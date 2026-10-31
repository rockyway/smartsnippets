"""Bounded READ-ONLY General snapshot observation. No mutation/restore acceptance."""
import argparse
import hashlib
from pathlib import Path
import plistlib
import subprocess
import sys
from run_ui_smoke import private_directory
from verify_evidence import inspect_executable

BASE = Path(__file__).resolve().parent
REASONS = {
    'COMPLETE': {'complete'},
    'BUSY': {'owner_changed'},
    'REFUSED': {'permission', 'missing_items', 'item_limit', 'empty_with_types', 'missing_item',
                'missing_types', 'format_limit', 'missing_type', 'unsupported_type', 'duplicate_type',
                'unreadable_data', 'byte_cap', 'missing_bytes'},
}
FOOTER = 'general_snapshot_complete scope=READ_ONLY restore=NOT_RUN'


def observed(stdout, stderr, code):
    if code != 0 or stderr.strip():
        return None
    lines = stdout.splitlines()
    if len(lines) != 2 or lines[1] != FOOTER:
        return None
    for status, reasons in REASONS.items():
        for reason in reasons:
            if lines[0] == f'general_snapshot status={status} reason={reason} secret_buffers=ZEROED mutation=NONE input=NONE':
                return status
    return None


def validate_entry(entry, digest):
    entry = entry.resolve(strict=True)
    if not entry.is_relative_to((BASE / 'bin/probe').resolve()) or entry.name != 'SmartSnippets.UnoProbe':
        raise ValueError('Owned native probe required')
    if entry.parent.name != 'MacOS' or entry.parents[1].name != 'Contents' or entry.parents[2].name != 'SmartSnippets.UnoProbe.app':
        raise ValueError('Isolated bundle required')
    with (entry.parents[1] / 'Info.plist').open('rb') as stream:
        info = plistlib.load(stream)
    if info.get('CFBundleIdentifier') != 'org.smartsnippets.feasibility018' or info.get('CFBundleExecutable') != entry.name:
        raise ValueError('Bundle identity mismatch')
    data = entry.read_bytes()
    if hashlib.sha256(data).hexdigest() != digest.lower() or inspect_executable(data, 'osx-arm64'):
        raise ValueError('Native artifact identity mismatch')
    return entry


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--executable', type=Path, required=True)
    parser.add_argument('--sha256', required=True)
    parser.add_argument('--authorized-clipboard-read', action='store_true')
    args = parser.parse_args()
    if sys.platform != 'darwin' or not args.authorized_clipboard_read:
        raise ValueError('Authorized Mac clipboard read required')
    entry = validate_entry(args.executable, args.sha256)
    private = private_directory()
    print('private_evidence_token=' + private.name)
    code = None
    with (private / 'stdout.log').open('wb') as stdout, (private / 'stderr.log').open('wb') as stderr:
        try:
            result = subprocess.run([str(entry), '--probe-general-snapshot', '--authorized-probe-clipboard'],
                                    stdin=subprocess.DEVNULL, stdout=stdout, stderr=stderr, cwd=entry.parent, timeout=20)
            code = result.returncode
        except subprocess.TimeoutExpired:
            # No mutation occurred; run() kills/reaps its own blocked reader.
            pass
    status = observed((private / 'stdout.log').read_text(errors='replace'),
                      (private / 'stderr.log').read_text(errors='replace'), code)
    print('general_snapshot_observation=' + (status or 'FAILED') +
          ' scope=READ_ONLY mutation=NONE restore=NOT_RUN owned_process_stopped=true')
    return 0 if status else 1


if __name__ == '__main__':
    try:
        sys.exit(main())
    except (OSError, ValueError, subprocess.SubprocessError):
        print('General snapshot observation refused; inspect private evidence.', file=sys.stderr)
        sys.exit(1)
