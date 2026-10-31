"""Native cross-process PRIVATE-board recovery. General contents never read/written."""
import argparse
from contextlib import suppress
from pathlib import Path
import subprocess
import sys
import time
import uuid
from run_general_snapshot_probe import validate_entry
from run_ui_smoke import private_directory

GUARD_READY = 'private_watchdog_guardian_ready snapshot=MEMORY_ONLY'
SWAPPED = 'private_watchdog_swapped_observed snapshot=MEMORY_ONLY'
GUARD_COMPLETE = 'private_watchdog_guardian_complete general_epoch=UNCHANGED general_content_reads=0 general_writes=0'
WORK_READY = 'private_watchdog_worker_ready'
WORK_COMPLETE = 'private_watchdog_worker_complete general_epoch=UNCHANGED general_content_reads=0 general_writes=0'
CASES = ('normal', 'worker_killed', 'supervisor_eof', 'changed_owner')


def send(process, command):
    process.stdin.write((command + '\n').encode('ascii'))
    process.stdin.flush()


def wait_lines(path, expected, children, deadline):
    while time.monotonic() < deadline:
        lines = path.read_text(errors='replace').splitlines()
        if lines == expected and all(child.poll() is None for child in children):
            return
        if lines != expected[:len(lines)] or any(child.poll() is not None for child in children):
            raise ValueError('Private watchdog protocol failed')
        time.sleep(0.05)
    raise ValueError('Private watchdog deadline exceeded')


def cleanup(process):
    if process is None:
        return
    if process.stdin is not None:
        with suppress(OSError):
            process.stdin.close()
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


def run_case(entry, case, private):
    if case not in CASES:
        raise ValueError('Unknown private watchdog case')
    run = uuid.uuid4().hex
    guardian = worker = None
    gout, gerr = private / 'guardian.stdout', private / 'guardian.stderr'
    wout, werr = private / 'worker.stdout', private / 'worker.stderr'
    deadline = time.monotonic() + 20
    guard_lines, work_lines = [GUARD_READY], [WORK_READY]
    with gout.open('wb') as go, gerr.open('wb') as ge, wout.open('wb') as wo, werr.open('wb') as we:
        try:
            guardian = subprocess.Popen([str(entry), '--probe-private-watchdog-owner', '--authorized-probe-clipboard', run],
                                        stdin=subprocess.PIPE, stdout=go, stderr=ge, cwd=entry.parent)
            wait_lines(gout, guard_lines, [guardian], deadline)
            worker = subprocess.Popen([str(entry), '--probe-private-watchdog-worker', '--authorized-probe-clipboard', run],
                                      stdin=subprocess.PIPE, stdout=wo, stderr=we, cwd=entry.parent)
            wait_lines(wout, work_lines, [guardian, worker], deadline)
            send(worker, 'swap')
            work_lines.append('private_watchdog_worker_swapped')
            wait_lines(wout, work_lines, [guardian, worker], deadline)
            send(guardian, 'capture_swapped')
            guard_lines.append(SWAPPED)
            wait_lines(gout, guard_lines, [guardian, worker], deadline)
            if case == 'changed_owner':
                send(worker, 'new_owner')
                work_lines.append('private_watchdog_worker_new_owner')
                wait_lines(wout, work_lines, [guardian, worker], deadline)
            if case == 'normal':
                worker.stdin.close()
                worker.wait(timeout=min(3, max(0.1, deadline - time.monotonic())))
                if worker.returncode != 0:
                    raise ValueError('Private worker natural exit failed')
                work_lines.append(WORK_COMPLETE)
            else:
                # Deliberate hard abort of this Popen-owned mutator only. The
                # independent native guardian retains the original in memory.
                worker.kill()
                worker.wait(timeout=3)
                if worker.returncode == 0:
                    raise ValueError('Private worker abort was not observed')
            if case == 'supervisor_eof':
                guardian.stdin.close()
                guard_lines.append('private_watchdog_eof_restored representations=EXACT')
            else:
                command = 'refuse_changed' if case == 'changed_owner' else 'restore'
                send(guardian, command)
                guard_lines.append('private_watchdog_changed_owner_preserved mutation=NONE' if case == 'changed_owner' else
                                   'private_watchdog_restored representations=EXACT')
                wait_lines(gout, guard_lines, [guardian], deadline)
                send(guardian, 'finish')
                guardian.stdin.close()
            guardian.wait(timeout=min(5, max(0.1, deadline - time.monotonic())))
            guard_lines.append(GUARD_COMPLETE)
            if guardian.returncode != 0 or gerr.read_bytes() or werr.read_bytes():
                raise ValueError('Private recovery native verification failed')
            if gout.read_text().splitlines() != guard_lines or wout.read_text().splitlines() != work_lines:
                raise ValueError('Private recovery receipts incomplete')
        finally:
            try:
                cleanup(worker)
            finally:
                cleanup(guardian)
    print('private_watchdog_case=' + case + ' result=PASS original_snapshot=MEMORY_ONLY scope=PRIVATE_ONLY')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--executable', type=Path, required=True)
    parser.add_argument('--sha256', required=True)
    parser.add_argument('--authorized-private-watchdog', action='store_true')
    args = parser.parse_args()
    if sys.platform != 'darwin' or not args.authorized_private_watchdog:
        raise ValueError('Authorized Mac private watchdog required')
    entry = validate_entry(args.executable, args.sha256)
    private = private_directory()
    print('private_evidence_token=' + private.name)
    for case in CASES:
        folder = private / case
        folder.mkdir(mode=0o700)
        run_case(entry, case, folder)
    print('private_watchdog_result=PASS scope=PRIVATE_ONLY general_contents=UNTOUCHED owned_processes_stopped=true')
    return 0


if __name__ == '__main__':
    try:
        sys.exit(main())
    except (OSError, ValueError, subprocess.SubprocessError) as error:
        print('private_watchdog_failed kind=' + type(error).__name__, file=sys.stderr)
        sys.exit(1)
