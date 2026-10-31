"""Run only --probe-data on an owned, hash-pinned Native AOT probe artifact.

No GUI path, clipboard, keyboard, startup or OS permission operations.
"""
import argparse
import hashlib
from pathlib import Path
import subprocess
import re
import sys
import tempfile
import uuid
from verify_evidence import inspect_executable

BASE = Path(__file__).resolve().parent


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--executable", required=True, type=Path)
    parser.add_argument("--sha256", required=True)
    args = parser.parse_args()
    if sys.platform not in ("win32", "darwin"):
        raise ValueError("Use a validated Windows or macOS build host")
    executable = args.executable.resolve(strict=True)
    if not executable.is_relative_to((BASE / "bin" / "probe").resolve()):
        raise ValueError("Use an owned probe output")
    rid = "win-x64" if sys.platform == "win32" else "osx-arm64"
    expected_name = "SmartSnippets.UnoProbe.exe" if sys.platform == "win32" else "SmartSnippets.UnoProbe"
    data = executable.read_bytes()
    if executable.name != expected_name or hashlib.sha256(data).hexdigest() != args.sha256.lower():
        raise ValueError("Artifact identity check failed")
    if inspect_executable(data, rid):
        raise ValueError("Expected a native probe artifact")
    run = uuid.uuid4().hex
    flags = subprocess.CREATE_NO_WINDOW if sys.platform == "win32" else 0
    for stage, receipt in [
        ("seed", "probe_data_seed_ok migration=ok crud=ok import_export=ok failure_paths=ok"),
        ("verify", "probe_data_restart_ok persistence=ok privacy_fixture=ok cleanup=ok"),
    ]:
        result = subprocess.run([str(executable), "--probe-data", stage, run],
                                capture_output=True, text=True, timeout=30, creationflags=flags)
        if result.returncode or result.stdout != receipt + "\n" or result.stderr:
            # Payloads and private roots are never echoed; the synthetic profile
            # is retained on failure rather than deleted under a surviving owner.
            print("data_probe_failed stage=" + stage + " synthetic_run=" + run)
            if re.fullmatch(r"probe_data_failed stage=[a-z_]+ kind=[A-Za-z0-9]+ synthetic_profile_retained\s*", result.stderr):
                print(result.stderr.strip())
            return 1
        print(receipt.split()[0])
    if (Path(tempfile.gettempdir()) / ("SmartSnippets.Uno018-" + run)).exists():
        raise ValueError("Synthetic profile cleanup incomplete")
    print("data_probe_native_restart_passed gui=NOT_RUN")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (OSError, ValueError, subprocess.SubprocessError):
        print("Data probe could not complete; no GUI path was requested.", file=sys.stderr)
        sys.exit(1)
