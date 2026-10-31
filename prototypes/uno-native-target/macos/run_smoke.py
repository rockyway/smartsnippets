"""Authorized, bounded two-window target startup only. No input or clipboard."""
import argparse
import hashlib
import json
from pathlib import Path
import plistlib
import re
import subprocess
import sys
import time
import uuid

BASE = Path(__file__).resolve().parent
PROBE = BASE.parents[1] / "uno-desktop"
sys.path.insert(0, str(PROBE))
from run_ui_smoke import private_directory
from verify_evidence import inspect_executable


def ready_pid_windows(text, pid):
    match = re.fullmatch(r"target_ready pid=([0-9]+) a=([0-9]+) b=([0-9]+)(?: edit_a=([0-9]+) edit_b=([0-9]+))?\s*", text)
    return bool(match and int(match[1]) == pid and int(match[2]) > 0 and int(match[3]) > 0 and int(match[2]) != int(match[3]) and
                (match[4] is None or len({int(match[n]) for n in (2, 3, 4, 5)}) == 4 and int(match[4]) > 0 and int(match[5]) > 0))


CONTROL_RECEIPTS = [
    "target_command name=empty_a ok=1",
    "target_command name=empty_b ok=1",
    "target_command name=close_a ok=1",
]


def observed_ids(output):
    try:
        values = json.loads(output)
        if not isinstance(values, list) or any(type(v) is not int or v <= 0 for v in values) or len(set(values)) != len(values):
            raise ValueError()
        return set(values)
    except (ValueError, TypeError):
        raise ValueError("Window observer failed") from None


def control_completed(text, pid):
    lines = text.splitlines()
    return len(lines) == 4 and ready_pid_windows(lines[0], pid) and lines[1:] == CONTROL_RECEIPTS


def validate_entry(entry, digest, windows):
    base = BASE.parent / "windows" if windows else BASE
    expected = "SmartSnippets.Target018.exe" if windows else "SmartSnippets.Target018"
    entry = entry.resolve(strict=True)
    if not entry.is_relative_to((base / "bin").resolve()) or entry.name != expected:
        raise ValueError("Unowned target image")
    if not windows:
        if entry.parent.name != "MacOS" or entry.parents[1].name != "Contents" or entry.parents[2].name != "SmartSnippets.Target018.app":
            raise ValueError("Target bundle required")
        with (entry.parents[1] / "Info.plist").open("rb") as stream:
            info = plistlib.load(stream)
        if info.get("CFBundleIdentifier") != "org.smartsnippets.feasibility018.target" or info.get("CFBundleExecutable") != entry.name:
            raise ValueError("Target identity mismatch")
    data = entry.read_bytes()
    if hashlib.sha256(data).hexdigest() != digest.lower() or inspect_executable(data, "win-x64" if windows else "osx-arm64"):
        raise ValueError("Native target hash/header mismatch")
    if windows:
        # A console-subsystem head can unexpectedly allocate/activate a console.
        pe = int.from_bytes(data[0x3c:0x40], "little")
        if int.from_bytes(data[pe + 24 + 68:pe + 24 + 70], "little") != 2:
            raise ValueError("GUI subsystem target required")
    else:
        signature = subprocess.run(["/usr/bin/codesign", "--verify", "--strict", "--deep", str(entry.parents[2])],
                                   capture_output=True, timeout=10)
        if signature.returncode:
            raise ValueError("Target signature refused")
    return entry


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--executable", required=True, type=Path)
    parser.add_argument("--sha256", required=True)
    parser.add_argument("--authorized-screen-slot", action="store_true")
    parser.add_argument("--empty-close", action="store_true", help="Check empty owned editors and close owned window A through native callbacks")
    args = parser.parse_args()
    if not args.authorized_screen_slot or sys.platform not in ("darwin", "win32"):
        raise ValueError("Authorized desktop slot required")
    windows = sys.platform == "win32"
    entry = validate_entry(args.executable, args.sha256, windows)
    private = private_directory()
    print("private_evidence_token=" + private.name)
    process = None
    ready = False
    requested = False
    with (private / "stdout.log").open("wb") as stdout, (private / "stderr.log").open("wb") as stderr:
        try:
            process = subprocess.Popen([str(entry), "--authorized-probe-target", str(uuid.uuid4())],
                                       stdin=subprocess.PIPE if args.empty_close else subprocess.DEVNULL, stdout=stdout, stderr=stderr, cwd=entry.parent,
                                       creationflags=subprocess.CREATE_NO_WINDOW if windows else 0)
            deadline = time.monotonic() + 15
            while time.monotonic() < deadline and process.poll() is None:
                output = (private / "stdout.log").read_text(errors="replace")
                valid = control_completed(output, process.pid) if requested else ready_pid_windows(output, process.pid)
                if valid:
                    try:
                        count = subprocess.run([sys.executable, str(PROBE / "run_ui_smoke.py"),
                                                "--window-ids", str(process.pid)], capture_output=True,
                                               text=True, timeout=3, check=True,
                                               creationflags=subprocess.CREATE_NO_WINDOW if windows else 0)
                    except subprocess.TimeoutExpired:
                        break
                    if count.stderr.strip():
                        raise ValueError("Window observer failed")
                    ids = observed_ids(count.stdout)
                    header = output.splitlines()[0]
                    identity = re.fullmatch(r"target_ready pid=[0-9]+ a=([0-9]+) b=([0-9]+)(?: edit_a=[0-9]+ edit_b=[0-9]+)?", header)
                    if identity is None:
                        raise ValueError("Target readiness failed")
                    expected_ids = {int(identity[2])} if requested else {int(identity[1]), int(identity[2])}
                    if ids == expected_ids and process.poll() is None:
                        if args.empty_close and not requested:
                            process.stdin.write(b"empty_a\nempty_b\nclose_a\n")
                            process.stdin.flush()
                            process.stdin.close()
                            requested = True
                        else:
                            ready = True
                            break
                time.sleep(0.1)
        finally:
            if process is not None:
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
    ready = ready and not (private / "stderr.log").read_bytes().strip()
    print(("target_owned_empty_close=" if args.empty_close else "target_two_owned_windows=") + ("OBSERVED" if ready else "NOT_OBSERVED") +
          " owned_process_stopped=true input=NONE clipboard=UNTOUCHED")
    return 0 if ready else 1


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (OSError, ValueError, subprocess.SubprocessError):
        print("Owned target smoke refused; inspect private evidence.", file=sys.stderr)
        sys.exit(1)
