"""Offline plan 018 decision-report validation, NOT binary artifact verification.

Schema v1 (exact keys): {"schemaVersion": 1, "rows": [
  {"platform": "windows|macos|android|ios", "gate": "FG1|...|FG6",
   "status": "BLOCKED|NOT_RUN", "risks": [allowlisted risk codes]}]}.
Exactly one row for every platform/gate pair is required (24 rows). NOT_RUN
is the machine spelling of the plan's NOT RUN. Risks must be nonempty/unique.
No paths, free text, receipts or claimed artifact properties are accepted.

No PASS is supported: verify_evidence.py checks actual desktop build artifacts
but returns BUILD_CHECKS_ONLY, never runtime acceptance. There is no paired
mobile artifact/physical-device validator. Even a self-asserted verified receipt
cannot change that. Acceptance here means only an honest unresolved decision
report, not gate completion, owner approval, provenance or actual binary PASS.
"""
import json
import sys

PLATFORMS = ("windows", "macos", "android", "ios")
GATES = tuple("FG" + str(number) for number in range(1, 7))
RISKS = frozenset({"not_run", "toolchain_unavailable", "build_checks_only",
                   "runtime_unverified", "device_unverified", "keyboard_unverified",
                   "production_runtime_blocked", "privacy_unverified",
                   "recovery_unverified", "owner_decision_required"})


def validate(document):
    """Return content-free errors; never read files or trust declared verification."""
    if (type(document) is not dict or set(document) != {"schemaVersion", "rows"}
            or type(document["schemaVersion"]) is not int or document["schemaVersion"] != 1
            or type(document["rows"]) is not list):
        return ["report:invalid-schema"]
    errors = set()
    seen = set()
    for row in document["rows"]:
        if type(row) is not dict or set(row) != {"platform", "gate", "status", "risks"}:
            errors.add("row:invalid-schema")
            continue
        platform, gate = row["platform"], row["gate"]
        if type(platform) is not str or platform not in PLATFORMS or type(gate) is not str or gate not in GATES:
            errors.add("row:invalid-target")
            continue
        key = (platform, gate)
        if key in seen:
            errors.add("row:duplicate")
        seen.add(key)
        if type(row["status"]) is not str or row["status"] not in ("BLOCKED", "NOT_RUN"):
            errors.add("row:unsupported-status")
        risks = row["risks"]
        if (type(risks) is not list or not risks
                or any(type(risk) is not str or risk not in RISKS for risk in risks)):
            errors.add("row:invalid-risks")
        elif len(set(risks)) != len(risks):
            errors.add("row:duplicate-risk")
    if seen != {(platform, gate) for platform in PLATFORMS for gate in GATES}:
        errors.add("report:incomplete-matrix")
    return sorted(errors)


def _unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError()
        result[key] = value
    return result


def main():
    """Read JSON from stdin only; emit no input content or exception payloads."""
    try:
        document = json.load(sys.stdin, object_pairs_hook=_unique_object)
        errors = validate(document)
    except (ValueError, TypeError, OSError, RecursionError):
        errors = ["input:invalid"]
    print(json.dumps({"result": "REJECTED" if errors else "UNRESOLVED_REPORT_ONLY",
                      "errors": errors, "artifactVerification": "NOT_RUN"}))
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
