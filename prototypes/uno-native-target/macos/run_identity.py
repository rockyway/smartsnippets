"""Guarded owned-target AX identity experiment. No keyboard, mouse or clipboard."""
import argparse
import ctypes as C
from contextlib import suppress
import math
from pathlib import Path
import subprocess
import sys
import time
import uuid
from run_smoke import BASE, ready_pid_windows, validate_entry, private_directory


def require_quiet_focus(deadline):
    # Focus-only experiment, not event dispatch: do not reinterpret this chord/
    # pointer preflight as an exemption from the full held-key posting guard.
    cg = C.CDLL('/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics')
    cg.CGEventSourceSecondsSinceLastEventType.argtypes = [C.c_int, C.c_uint32]
    cg.CGEventSourceSecondsSinceLastEventType.restype = C.c_double
    cg.CGEventSourceKeyState.argtypes = [C.c_int, C.c_ushort]
    cg.CGEventSourceKeyState.restype = C.c_bool
    cg.CGEventSourceButtonState.argtypes = [C.c_int, C.c_uint32]
    cg.CGEventSourceButtonState.restype = C.c_bool
    until = min(deadline, time.monotonic() + 5)
    while time.monotonic() < until:
        idle = cg.CGEventSourceSecondsSinceLastEventType(0, 0xffffffff)
        if math.isfinite(idle) and idle >= 1.5 and not any(cg.CGEventSourceKeyState(0, key) for key in range(54, 64)) and not any(cg.CGEventSourceButtonState(0, button) for button in range(5)):
            return
        time.sleep(0.05)
    raise ValueError('Focus-only idle preflight refused')


def stop(process):
    if process is None:
        return
    if process.stdin is not None:
        with suppress(OSError):
            process.stdin.close()
    if process.poll() is None:
        process.terminate()
    try:
        process.wait(timeout=3)
    except subprocess.TimeoutExpired:
        process.kill()
        process.wait(timeout=3)
    except BaseException:
        if process.poll() is None:
            process.kill()
            process.wait(timeout=3)
        raise


def wait_lines(path, expected, children, deadline):
    while time.monotonic() < deadline:
        actual = path.read_text(errors='replace').splitlines()
        if actual == expected and all(child.poll() is None for child in children):
            return
        if actual != expected[:len(actual)] or any(child.poll() is not None for child in children):
            raise ValueError('Identity protocol refused')
        time.sleep(0.05)
    raise ValueError('Identity deadline exceeded')


def run(entry, digest, private):
    target = observer = None
    target_lines, observer_lines = [], []
    target_out, target_err = private / 'target.stdout', private / 'target.stderr'
    observer_out, observer_err = private / 'observer.stdout', private / 'observer.stderr'
    deadline = time.monotonic() + 30
    # Each native AX call is additionally configured for a 1s messaging timeout.
    # Parent observation is file-based; it never blocks reading a child's pipe.
    with target_out.open('wb') as out, target_err.open('wb') as err, observer_out.open('wb') as axout, observer_err.open('wb') as axerr:
        try:
            target = subprocess.Popen([str(entry), '--authorized-probe-target', str(uuid.uuid4())], stdin=subprocess.PIPE,
                                      stdout=out, stderr=err, cwd=entry.parent)
            while time.monotonic() < deadline and target.poll() is None:
                text = target_out.read_text(errors='replace')
                if ready_pid_windows(text, target.pid):
                    target_lines = text.splitlines()
                    break
                time.sleep(0.05)
            if len(target_lines) != 1:
                raise ValueError('Owned target readiness failed')
            observer = subprocess.Popen([sys.executable, str(BASE / 'ax_identity_observer.py'), '--pid', str(target.pid),
                                         '--executable', str(entry), '--sha256', digest], stdin=subprocess.PIPE,
                                        stdout=axout, stderr=axerr, cwd=BASE)
            observer_lines = ['ax_identity_ready']
            wait_lines(observer_out, observer_lines, [target, observer], deadline)
            for target_command, ax_command in [('focus_a', 'focus_a'), ('focus_b', 'focus_b'),
                                                ('focus_a', 'focus_a'), ('close_a', 'closed_a')]:
                if time.monotonic() >= deadline:
                    raise ValueError('Identity deadline exceeded')
                if target_command.startswith('focus_'):
                    require_quiet_focus(deadline)
                target.stdin.write((target_command + '\n').encode('ascii'))
                target.stdin.flush()
                target_lines.append('target_command name=' + target_command + ' ok=1')
                wait_lines(target_out, target_lines, [target, observer], deadline)
                observer.stdin.write((ax_command + '\n').encode('ascii'))
                observer.stdin.flush()
                observer_lines.append('ax_identity_verified name=' + ax_command)
                wait_lines(observer_out, observer_lines, [target, observer], deadline)
            observer.stdin.close()
            observer.wait(timeout=max(0.1, min(3, deadline - time.monotonic())))
            if observer.returncode != 0 or target_err.read_bytes() or observer_err.read_bytes():
                raise ValueError('Native identity verification failed')
            if observer_out.read_text().splitlines() != observer_lines or target_out.read_text().splitlines() != target_lines:
                raise ValueError('Unexpected trailing identity receipt')
        finally:
            # Both children are direct Popen-owned processes. Never kill by name.
            try:
                stop(observer)
            finally:
                stop(target)
    print('target_ax_identity=PASS same_pid_other_window=REJECTED closed_window=REJECTED owned_processes_stopped=true input=NONE clipboard=UNTOUCHED')
    return 0


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--executable', type=Path, required=True)
    parser.add_argument('--sha256', required=True)
    parser.add_argument('--authorized-screen-slot', action='store_true')
    args = parser.parse_args()
    if sys.platform != 'darwin' or not args.authorized_screen_slot:
        raise ValueError('Authorized Mac screen slot required')
    entry = validate_entry(args.executable, args.sha256, False)
    private = private_directory()
    print('private_evidence_token=' + private.name)
    return run(entry, args.sha256, private)


if __name__ == '__main__':
    try:
        sys.exit(main())
    except (OSError, ValueError, subprocess.SubprocessError) as error:
        print('target_ax_identity_failed kind=' + type(error).__name__, file=sys.stderr)
        sys.exit(1)
