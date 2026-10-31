"""Authorized bounded ephemeral registration only; no key posting or GUI mode."""
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
    "shortcut_registration_started",
    "shortcut_registration_acquired",
    "shortcut_registration_released",
    "shortcut_registration_reacquired",
    "shortcut_registration_complete cleanup=VERIFIED foreground_token=UNCHANGED input=NONE delivery=NOT_RUN",
]


def completed(stdout, stderr, code):
    return code == 0 and not stderr.strip() and stdout.splitlines() == RECEIPTS


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--executable", required=True, type=Path)
    parser.add_argument("--sha256", required=True)
    parser.add_argument("--authorized-screen-slot", action="store_true")
    args = parser.parse_args()
    if not args.authorized_screen_slot or sys.platform not in ("darwin", "win32"):
        raise ValueError("Authorized native desktop slot required")
    windows = sys.platform == "win32"
    entry = args.executable.resolve(strict=True)
    expected = "SmartSnippets.UnoProbe.exe" if windows else "SmartSnippets.UnoProbe"
    if not entry.is_relative_to((BASE / "bin/probe").resolve()) or entry.name != expected:
        raise ValueError("Owned native probe required")
    if not windows:
        if entry.parent.name != "MacOS" or entry.parents[1].name != "Contents" or entry.parents[2].name != "SmartSnippets.UnoProbe.app":
            raise ValueError("Isolated bundle required")
        with (entry.parents[1] / "Info.plist").open("rb") as stream:
            info = plistlib.load(stream)
        if info.get("CFBundleIdentifier") != "org.smartsnippets.feasibility018" or info.get("CFBundleExecutable") != entry.name:
            raise ValueError("Bundle identity mismatch")
    data = entry.read_bytes()
    if hashlib.sha256(data).hexdigest() != args.sha256.lower() or inspect_executable(data, "win-x64" if windows else "osx-arm64"):
        raise ValueError("Native artifact identity mismatch")
    private = private_directory()
    print("private_evidence_token=" + private.name)
    code = None
    with (private / "stdout.log").open("wb") as stdout, (private / "stderr.log").open("wb") as stderr:
        try:
            result = subprocess.run([str(entry), "--probe-shortcut-register", "--authorized-probe-shortcut"],
                                    stdin=subprocess.DEVNULL, stdout=stdout, stderr=stderr, cwd=entry.parent,
                                    timeout=20, creationflags=subprocess.CREATE_NO_WINDOW if windows else 0)
            code = result.returncode
        except subprocess.TimeoutExpired:
            # run() kills/reaps its own child; OS registrations are process-owned.
            pass
    passed = completed((private / "stdout.log").read_text(errors="replace"),
                       (private / "stderr.log").read_text(errors="replace"), code)
    print("shortcut_registration_result=" + ("PASS" if passed else "FAIL") +
          " owned_process_stopped=true input=NONE delivery=NOT_RUN")
    return 0 if passed else 1


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (OSError, ValueError, subprocess.SubprocessError):
        print("Shortcut registration probe refused; inspect private evidence.", file=sys.stderr)
        sys.exit(1)
