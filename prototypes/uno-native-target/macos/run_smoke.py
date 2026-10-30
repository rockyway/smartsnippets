"""Authorized, bounded two-window target startup only. No input or clipboard."""
import argparse
import hashlib
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
    match = re.fullmatch(r"target_ready pid=([0-9]+) a=([0-9]+) b=([0-9]+)\s*", text)
    return bool(match and int(match[1]) == pid and int(match[2]) > 0 and int(match[3]) > 0 and int(match[2]) != int(match[3]))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--executable", required=True, type=Path)
    parser.add_argument("--sha256", required=True)
    parser.add_argument("--authorized-screen-slot", action="store_true")
    args = parser.parse_args()
    if not args.authorized_screen_slot or sys.platform != "darwin":
        raise ValueError("Authorized Mac slot required")
    entry = args.executable.resolve(strict=True)
    if not entry.is_relative_to((BASE / "bin").resolve()) or entry.name != "SmartSnippets.Target018":
        raise ValueError("Unowned target image")
    if entry.parent.name != "MacOS" or entry.parents[1].name != "Contents" or entry.parents[2].name != "SmartSnippets.Target018.app":
        raise ValueError("Target bundle required")
    with (entry.parents[1] / "Info.plist").open("rb") as stream:
        info = plistlib.load(stream)
    if info.get("CFBundleIdentifier") != "org.smartsnippets.feasibility018.target" or info.get("CFBundleExecutable") != entry.name:
        raise ValueError("Target identity mismatch")
    data = entry.read_bytes()
    if hashlib.sha256(data).hexdigest() != args.sha256.lower() or inspect_executable(data, "osx-arm64"):
        raise ValueError("Native target hash/header mismatch")
    signature = subprocess.run(["/usr/bin/codesign", "--verify", "--strict", "--deep", str(entry.parents[2])],
                               capture_output=True, timeout=10)
    if signature.returncode:
        raise ValueError("Target signature refused")
    private = private_directory()
    print("private_evidence_token=" + private.name)
    process = None
    ready = False
    with (private / "stdout.log").open("wb") as stdout, (private / "stderr.log").open("wb") as stderr:
        try:
            process = subprocess.Popen([str(entry), "--authorized-probe-target", str(uuid.uuid4())],
                                       stdin=subprocess.DEVNULL, stdout=stdout, stderr=stderr, cwd=entry.parent)
            deadline = time.monotonic() + 15
            while time.monotonic() < deadline and process.poll() is None:
                output = (private / "stdout.log").read_text(errors="replace")
                if ready_pid_windows(output, process.pid):
                    try:
                        count = subprocess.run([sys.executable, str(PROBE / "run_ui_smoke.py"),
                                                "--window-count", str(process.pid)], capture_output=True,
                                               text=True, timeout=3, check=True)
                    except subprocess.TimeoutExpired:
                        break
                    if not count.stdout.strip().isdigit() or count.stderr.strip():
                        raise ValueError("Window observer failed")
                    if count.stdout.strip() == "2" and process.poll() is None:
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
    print("target_two_owned_windows=" + ("OBSERVED" if ready else "NOT_OBSERVED") +
          " owned_process_stopped=true input=NONE clipboard=UNTOUCHED")
    return 0 if ready else 1


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (OSError, ValueError, subprocess.SubprocessError):
        print("Owned target smoke refused; inspect private evidence.", file=sys.stderr)
        sys.exit(1)
