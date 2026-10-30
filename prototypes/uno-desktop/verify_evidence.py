"""Fail-closed build checks for plan 018; no GUI launch or native execution.

This checks local build records, binary headers and payload layout, not provenance
against malicious inputs. BUILD_CHECKS_ONLY is not a PASS for any runtime gate.
"""
import argparse
import json
from pathlib import Path
import re
import struct
import sys


REQUIRED_TRUE = ("PublishAot", "PublishTrimmed", "EnableTrimAnalyzer", "EnableAotAnalyzer", "TreatWarningsAsErrors")
REQUIRED_FALSE = ("SuppressTrimAnalysisWarnings", "TrimmerSingleWarn")
METADATA = ("NETCoreSdkVersion", "MSBuildVersion", "UnoVersion", "TargetFramework")
# .NET bundle/apphost marker; header checks alone also accept native JIT hosts.
BUNDLE_MARKER = bytes.fromhex("8b1202b96a612038727b930214d7a03213f5b9e6efae3318ee3b2dce24b36aae")
HOST_MARKERS = (BUNDLE_MARKER, b"hostfxr_main_startupinfo", b"DotNetRuntimeContractDescriptor", b"coreclr_initialize")
JIT_NAMES = {"coreclr.dll", "clrjit.dll", "libcoreclr.dylib", "libclrjit.dylib",
             "libmonosgen-2.0.dylib", "libcoreclr.so", "libclrjit.so", "libmonosgen-2.0.so"}


def inspect_executable(data, rid, allowed_macho_types=(2,)):
    """Parse enough PE32+/Mach-O to reject wrong-arch and managed entry points."""
    errors = []
    if any(marker in data for marker in HOST_MARKERS):
        errors.append("artifact:jit-host")
    if rid == "win-x64":
        if len(data) < 64 or data[:2] != b"MZ":
            return ["artifact:invalid-pe"]
        offset = struct.unpack_from("<I", data, 0x3C)[0]
        if offset + 24 + 240 > len(data) or data[offset:offset + 4] != b"PE\0\0":
            return ["artifact:invalid-pe"]
        machine = struct.unpack_from("<H", data, offset + 4)[0]
        size = struct.unpack_from("<H", data, offset + 20)[0]
        optional = offset + 24
        if machine != 0x8664:
            errors.append("artifact:wrong-architecture")
        if size < 240 or optional + size > len(data) or struct.unpack_from("<H", data, optional)[0] != 0x20B:
            return errors + ["artifact:invalid-pe"]
        if struct.unpack_from("<I", data, optional + 108)[0] < 15:
            return errors + ["artifact:invalid-pe"]
        if any(struct.unpack_from("<II", data, optional + 112 + 14 * 8)):
            errors.append("artifact:managed-entry")
    elif rid == "osx-arm64":
        if len(data) < 32 or data[:4] != b"\xcf\xfa\xed\xfe":
            return ["artifact:invalid-macho"]
        cpu, _, filetype, count, size = struct.unpack_from("<IIIII", data, 4)
        if cpu != 0x0100000C:
            errors.append("artifact:wrong-architecture")
        if filetype not in allowed_macho_types or 32 + size > len(data) or count > size // 8:
            return errors + ["artifact:invalid-macho"]
        offset = 32
        # Embedded load commands can reveal a bundled/shared JIT host.
        for _ in range(count):
            if offset + 8 > 32 + size:
                return errors + ["artifact:invalid-macho"]
            command, length = struct.unpack_from("<II", data, offset)
            if length < 8 or offset + length > 32 + size:
                return errors + ["artifact:invalid-macho"]
            if command in (0xC, 0x18, 0x80000018, 0x8000001F):
                if length < 24:
                    return errors + ["artifact:invalid-macho"]
                name_offset = struct.unpack_from("<I", data, offset + 8)[0]
                if name_offset < 24 or name_offset >= length:
                    return errors + ["artifact:invalid-macho"]
                name = data[offset + name_offset:offset + length].split(b"\0")[0]
                if any(token in name.lower() for token in (b"coreclr", b"clrjit", b"monosgen")):
                    errors.append("artifact:jit-runtime")
            offset += length
        if offset != 32 + size:
            errors.append("artifact:invalid-macho")
    else:
        errors.append("metadata:unsupported-rid")
    return errors


def validate(properties, build_log, artifact_dir, executable, rid):
    errors = []
    for name in REQUIRED_TRUE:
        if str(properties.get(name, "")).lower() != "true":
            errors.append("property:" + name)
    for name in REQUIRED_FALSE:
        if str(properties.get(name, "")).lower() != "false":
            errors.append("property:" + name)
    for name in METADATA:
        if not properties.get(name):
            errors.append("metadata:" + name)
    if properties.get("RuntimeIdentifier") != rid:
        errors.append("metadata:RuntimeIdentifier")
    if str(properties.get("PublishSingleFile", "")).lower() not in ("", "false"):
        errors.append("property:PublishSingleFile")
    tfm = properties.get("TargetFramework", "")
    if rid == "win-x64" and (not tfm.startswith("net10.0-windows") or not properties.get("WinAppSdkVersion")):
        errors.append("metadata:windows-native-head")
    if rid == "osx-arm64" and tfm != "net10.0-desktop":
        errors.append("metadata:macos-desktop-head")
    if rid == "win-x64" and properties.get("WindowsPackageType") != "None":
        errors.append("property:WindowsPackageType")
    if any(token.strip().upper().startswith("IL") or token.strip().lower() in ("*", "all")
           for token in re.split(r"[;,\s]+", properties.get("NoWarn", ""))):
        errors.append("property:NoWarn")
    # Reject all warnings, including SDK warnings outside the IL2/IL3 families.
    for line in build_log.splitlines():
        if re.fullmatch(r"\s*0 (Warning|Error)\(s\)\s*", line, re.I):
            continue
        if re.search(r"\b(warning|error)\b|\bIL[23]\d{3}\b", line, re.I):
            errors.append("build:diagnostic")
            break
    if "Generating native code" not in build_log or not re.search(r"UnoProbe\s*->\s*.+", build_log):
        errors.append("build:no-publish-receipt")
    if not re.search(r"^probe_publish_exit=0\s*$", build_log, re.M):
        errors.append("build:no-successful-exit")
    try:
        root = Path(artifact_dir).resolve(strict=True)
        entry = Path(executable).resolve(strict=True)
        if not entry.is_relative_to(root):
            errors.append("artifact:outside-output")
        else:
            errors.extend(inspect_executable(entry.read_bytes(), rid))
        payloads = list(root.rglob("*"))
        if any(file.name.lower() == entry.stem.lower() + ".dll" or
               file.name.lower().endswith((".runtimeconfig.json", ".deps.json")) for file in payloads):
            errors.append("artifact:managed-apphost-layout")
        if any(file.name.lower() in JIT_NAMES for file in payloads):
            errors.append("artifact:jit-runtime")
        sqlite_name = "e_sqlite3.dll" if rid == "win-x64" else "libe_sqlite3.dylib"
        sqlite_files = [file for file in payloads if file.is_file() and file.name.lower() == sqlite_name]
        if not sqlite_files:
            errors.append("artifact:sqlite-missing")
        elif any(inspect_executable(file.read_bytes(), rid, allowed_macho_types=(6,)) for file in sqlite_files):
            errors.append("artifact:sqlite-invalid")
    except (OSError, ValueError):
        errors.append("artifact:missing-or-unreadable")
    return sorted(set(errors))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--properties", required=True, type=Path)
    parser.add_argument("--log", required=True, type=Path)
    parser.add_argument("--artifact-dir", required=True, type=Path)
    parser.add_argument("--executable", required=True, type=Path)
    parser.add_argument("--rid", required=True, choices=("win-x64", "osx-arm64"))
    args = parser.parse_args()
    try:
        document = json.loads(args.properties.read_text(encoding="utf-8-sig"))
        errors = validate(document.get("Properties", document), args.log.read_text(encoding="utf-8-sig"),
                          args.artifact_dir, args.executable, args.rid)
    except (OSError, ValueError, TypeError, AttributeError):
        errors = ["input:invalid"]
    # Do not print source log text, usernames, file paths or exception payloads.
    print(json.dumps({"result": "REJECTED" if errors else "BUILD_CHECKS_ONLY", "errors": errors,
                      "runtimeGates": "NOT RUN"}))
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
