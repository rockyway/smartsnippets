"""Bounded Native AOT private-pasteboard checks. Never reads/mutates General contents."""
import argparse
import hashlib
from pathlib import Path
import plistlib
import subprocess
import sys
from run_ui_smoke import private_directory
from verify_evidence import inspect_executable

BASE = Path(__file__).resolve().parent
RECEIPTS = [
    "private_pasteboard_multi_item_roundtrip=PASS representations=EXACT items=4",
    "private_pasteboard_empty_roundtrip=PASS",
    "private_pasteboard_post_clear_recovery=PASS",
    "private_pasteboard_capture_owner_changed=PASS mutation=NONE",
    "private_pasteboard_unknown_refused=PASS mutation=NONE",
    "private_pasteboard_cap_refused=PASS mutation=NONE",
    "private_pasteboard_unreadable_refused=PASS mutation=NONE",
    "private_pasteboard_new_owner_preserved=PASS mutation=NONE",
    "private_pasteboard_complete general_epoch=UNCHANGED general_content_reads=0 general_writes=0 input=NONE",
]


def completed(stdout, stderr, code):
    return code == 0 and not stderr.strip() and stdout.splitlines() == RECEIPTS


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--executable", required=True, type=Path)
    parser.add_argument("--sha256", required=True)
    args = parser.parse_args()
    if sys.platform != "darwin":
        raise ValueError("Apple Silicon build host required")
    entry = args.executable.resolve(strict=True)
    if not entry.is_relative_to((BASE / "bin/probe").resolve()) or entry.name != "SmartSnippets.UnoProbe":
        raise ValueError("Owned native probe required")
    if entry.parent.name != "MacOS" or entry.parents[1].name != "Contents" or entry.parents[2].name != "SmartSnippets.UnoProbe.app":
        raise ValueError("Isolated bundle required")
    with (entry.parents[1] / "Info.plist").open("rb") as stream:
        info = plistlib.load(stream)
    if info.get("CFBundleIdentifier") != "org.smartsnippets.feasibility018" or info.get("CFBundleExecutable") != entry.name:
        raise ValueError("Bundle identity mismatch")
    data = entry.read_bytes()
    if hashlib.sha256(data).hexdigest() != args.sha256.lower() or inspect_executable(data, "osx-arm64"):
        raise ValueError("Native artifact identity mismatch")
    private = private_directory()
    print("private_evidence_token=" + private.name)
    code = None
    with (private / "stdout.log").open("wb") as stdout, (private / "stderr.log").open("wb") as stderr:
        try:
            result = subprocess.run([str(entry), "--probe-private-pasteboard"], stdin=subprocess.DEVNULL,
                                    stdout=stdout, stderr=stderr, cwd=entry.parent, timeout=20)
            code = result.returncode
        except subprocess.TimeoutExpired:
            # subprocess.run kills and reaps only its owned child on timeout.
            pass
    passed = completed((private / "stdout.log").read_text(errors="replace"),
                       (private / "stderr.log").read_text(errors="replace"), code)
    print("private_pasteboard_result=" + ("PASS" if passed else "FAIL") + " scope=PRIVATE_ONLY owned_process_stopped=true")
    return 0 if passed else 1


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (OSError, ValueError, subprocess.SubprocessError):
        print("Private pasteboard probe refused; no General content access requested.", file=sys.stderr)
        sys.exit(1)
