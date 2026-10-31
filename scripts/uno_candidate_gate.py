"""Plan 019 build-only validator. Never launches binaries or accesses an app profile.

Checks local evidence, not provenance against a malicious build host. A successful
build check is never a native UI, clipboard, input, or release acceptance PASS.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import struct
import sys

ENTRY = "SmartSnippets.UnoCandidate.exe"
BUNDLE_MARKER = bytes.fromhex("8b1202b96a612038727b930214d7a03213f5b9e6efae3318ee3b2dce24b36aae")
HOST_MARKERS = (BUNDLE_MARKER, b"hostfxr_main_startupinfo", b"DotNetRuntimeContractDescriptor", b"coreclr_initialize")
RUNTIME_NAMES = {"coreclr.dll", "clrjit.dll", "hostfxr.dll", "hostpolicy.dll", "mono.dll", "monosgen-2.0.dll"}
TRUE_PROPERTIES = ("PublishAot", "PublishTrimmed", "EnableTrimAnalyzer", "EnableAotAnalyzer",
                   "TreatWarningsAsErrors", "IsAotCompatible", "DisableImplicitUnoPackages",
                   "WindowsAppSDKSelfContained", "RestorePackagesWithLockFile", "RestoreLockedMode")
FALSE_PROPERTIES = ("SuppressTrimAnalysisWarnings", "TrimmerSingleWarn", "PublishSingleFile")
EXACT_PROPERTIES = {
    "RuntimeIdentifier": "win-x64", "TargetFramework": "net10.0-windows10.0.26100",
    "WindowsPackageType": "None", "NETCoreSdkVersion": "10.0.403",
    "AssemblyName": "SmartSnippets.UnoCandidate", "ImplicitUsings": "disable",
    "SupportedOSPlatformVersion": "10.0.17763.0", "TargetPlatformMinVersion": "10.0.17763.0",
}


def inspect_pe(data):
    """Reject malformed/non-x64/managed entry images and known native JIT apphosts."""
    if len(data) < 64 or data[:2] != b"MZ":
        return ["invalid-pe"]
    offset = struct.unpack_from("<I", data, 0x3C)[0]
    if offset + 264 > len(data) or data[offset:offset + 4] != b"PE\0\0":
        return ["invalid-pe"]
    optional = offset + 24
    size = struct.unpack_from("<H", data, offset + 20)[0]
    if size < 240 or optional + size > len(data) or struct.unpack_from("<H", data, optional)[0] != 0x20B:
        return ["invalid-pe"]
    errors = []
    if struct.unpack_from("<H", data, offset + 4)[0] != 0x8664:
        errors.append("wrong-architecture")
    if struct.unpack_from("<I", data, optional + 108)[0] < 15:
        errors.append("invalid-pe")
    elif any(struct.unpack_from("<II", data, optional + 112 + 14 * 8)):
        errors.append("managed-entry")
    if any(marker in data for marker in HOST_MARKERS):
        errors.append("jit-host")
    return errors


def has_il_suppression(value):
    return any(token.upper().startswith("IL") or token.lower() in ("*", "all")
               for token in re.split(r"[;,\s]+", str(value)))


def validate_references(document):
    errors = []
    if not isinstance(document, dict) or set(document) != {"Core", "Presentation"}:
        return ["references:missing"]
    for name in ("Core", "Presentation"):
        record = document[name]
        if not isinstance(record, dict):
            errors.append("references:invalid-" + name)
            continue
        for phase in ("Authored", "BuildContext"):
            props = record.get(phase)
            if not isinstance(props, dict):
                errors.append("references:missing-" + phase + "-" + name)
                continue
            prefix = "references:" + name + ":" + phase + ":"
            for key in ("IsAotCompatible", "EnableTrimAnalyzer", "EnableAotAnalyzer"):
                if str(props.get(key, "")).lower() != "true":
                    errors.append(prefix + key)
            for key in ("RestorePackagesWithLockFile", "RestoreLockedMode"):
                if key not in props or str(props[key]).lower() == "true":
                    errors.append(prefix + key)
            if "NoWarn" not in props or has_il_suppression(props["NoWarn"]):
                errors.append(prefix + "NoWarn")
            if str(props.get("SuppressTrimAnalysisWarnings", "")).lower() == "true":
                errors.append(prefix + "SuppressTrimAnalysisWarnings")
            if str(props.get("TrimmerSingleWarn", "")).lower() == "true":
                errors.append(prefix + "TrimmerSingleWarn")
            # Root libraries intentionally retain their existing build policy; the
            # candidate's actual global build context enforces warnings-as-errors.
            if phase == "BuildContext" and str(props.get("TreatWarningsAsErrors", "")).lower() != "true":
                errors.append(prefix + "TreatWarningsAsErrors")
    return errors


def is_link(path):
    return path.is_symlink() or getattr(path, "is_junction", lambda: False)()


def validate(properties, log, root, started_ms):
    errors = []
    if not isinstance(properties, dict):
        return ["properties:invalid"]
    for name in TRUE_PROPERTIES:
        if str(properties.get(name, "")).lower() != "true":
            errors.append("property:" + name)
    for name in FALSE_PROPERTIES:
        if str(properties.get(name, "")).lower() != "false":
            errors.append("property:" + name)
    for name, value in EXACT_PROPERTIES.items():
        if properties.get(name) != value:
            errors.append("property:" + name)
    if not properties.get("MSBuildVersion"):
        errors.append("property:MSBuildVersion")
    if "NoWarn" not in properties:
        errors.append("property:NoWarn")
    if has_il_suppression(properties.get("NoWarn", "")):
        errors.append("property:NoWarn")
    for line in log.splitlines():
        if re.fullmatch(r"\s*0 (Warning|Error)\(s\)\s*", line, re.I):
            continue
        if re.search(r"\b(warning|error)\b|\bIL[23]\d{3}\b", line, re.I):
            errors.append("build:diagnostic")
            break
    if "Generating native code" not in log or not re.search(r"SmartSnippets\.Uno(?:Candidate)?\s*->\s*.+", log):
        errors.append("build:native-compiler-receipt-missing")
    if len(re.findall(r"^candidate_publish_exit=0\s*$", log, re.M)) != 1:
        errors.append("build:successful-exit-missing")
    try:
        if not isinstance(started_ms, int) or started_ms <= 0:
            return sorted(set(errors + ["build:invalid-start-time"]))
        if any(is_link(parent) for parent in (root, *root.parents)):
            return sorted(set(errors + ["artifact:link"]))
        files = list(root.rglob("*"))
        if any(is_link(path) for path in files):
            return sorted(set(errors + ["artifact:link"]))
        entry = root / ENTRY
        for path in (entry, root / "e_sqlite3.dll"):
            if not path.is_file():
                errors.append("artifact:missing-" + path.name)
                continue
            errors.extend("artifact:" + reason for reason in inspect_pe(path.read_bytes()))
        if entry.is_file() and entry.stat().st_mtime_ns // 1_000_000 < started_ms:
            errors.append("artifact:stale-entry")
        forbidden_entries = {"smartsnippets.dll", "smartsnippets.exe", "smartsnippets.app.exe", "smartsnippets.app.dll",
                             "smartsnippets.uno.dll", "smartsnippets.uno.exe", "smartsnippets.unocandidate.dll"}
        for path in files:
            name = path.name.lower()
            if name in forbidden_entries or name.endswith((".runtimeconfig.json", ".deps.json")):
                errors.append("artifact:managed-or-legacy-layout")
            if name in RUNTIME_NAMES or "libmonosgen" in name or "mono.framework" in name:
                errors.append("artifact:jit-runtime")
    except (OSError, ValueError, struct.error):
        errors.append("artifact:unreadable")
    return sorted(set(errors))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--properties", type=Path, required=True)
    parser.add_argument("--log", type=Path, required=True)
    parser.add_argument("--reference-properties", type=Path, required=True)
    parser.add_argument("--artifact-dir", type=Path, required=True)
    parser.add_argument("--started-utc-ms", type=int, required=True)
    args = parser.parse_args()
    try:
        document = json.loads(args.properties.read_text(encoding="utf-8-sig"))
        errors = validate(document.get("Properties", document), args.log.read_text(encoding="utf-8-sig"),
                          args.artifact_dir, args.started_utc_ms)
        errors.extend(validate_references(json.loads(args.reference_properties.read_text(encoding="utf-8-sig"))))
        errors = sorted(set(errors))
        digest = hashlib.sha256((args.artifact_dir / ENTRY).read_bytes()).hexdigest() if not errors else None
    except (OSError, ValueError, TypeError, AttributeError):
        errors, digest = ["evidence:invalid"], None
    # Do not echo paths, source logs or exception text. Private logs stay on host.
    print(json.dumps({"result": "REJECTED" if errors else "BUILD_CHECKS_ONLY", "errors": errors,
                      "sha256": digest, "nativeAcceptance": "NOT RUN"}))
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
