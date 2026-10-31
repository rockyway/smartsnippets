"""Negative decision-report controls; these do not verify binaries/devices."""
import contextlib
import copy
import io
import json
from pathlib import Path
import sys
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import verify_gate_report as gate


def unresolved():
    return {"schemaVersion": 1, "rows": [
        {"platform": platform, "gate": name, "status": "NOT_RUN", "risks": ["not_run"]}
        for platform in gate.PLATFORMS for name in gate.GATES]}


class GateReportTests(unittest.TestCase):
    def test_UnresolvedReportIsNotArtifactPass(self):
        document = unresolved()
        self.assertEqual([], gate.validate(document))
        document["rows"][0].update(status="BLOCKED", risks=["build_checks_only"])
        with patch("sys.stdin", io.StringIO(json.dumps(document))), contextlib.redirect_stdout(io.StringIO()) as output:
            self.assertEqual(0, gate.main())
        result = json.loads(output.getvalue())
        self.assertEqual("UNRESOLVED_REPORT_ONLY", result["result"])
        self.assertEqual("NOT_RUN", result["artifactVerification"])

    def test_EvidenceGateRejectsNotRunAsPass(self):
        for platform in ("windows", "macos"):
            for basis in ("not_run", "toolchain_unavailable", "build_checks_only"):
                with self.subTest(platform=platform, basis=basis):
                    document = unresolved()
                    row = next(row for row in document["rows"] if row["platform"] == platform)
                    row.update(status="PASS", risks=[basis])
                    self.assertIn("row:unsupported-status", gate.validate(document))

    def test_MobileRiskEvidenceRejectsUnverifiedArtifacts(self):
        for platform in ("android", "ios"):
            for basis in ("docs_only", "prerequisite_only", "management_only", "Mono AOT", "JIT",
                          "NoWarn=XA1040", "NoWarn=IL2026;IL3050", "EnablePreviewFeatures=true",
                          "paired_artifacts_verified", "physical_device_verified"):
                with self.subTest(platform=platform, basis=basis):
                    document = unresolved()
                    row = next(row for row in document["rows"] if row["platform"] == platform)
                    row.update(status="PASS", risks=[basis])
                    self.assertIn("row:unsupported-status", gate.validate(document))
                    # Even plausible allowlisted risks cannot launder a PASS.
                    row["risks"] = ["device_unverified"]
                    self.assertIn("row:unsupported-status", gate.validate(document))

    def test_RejectsMalformedAndIncompleteMatrices(self):
        controls = [None, [], {}, {"schemaVersion": True, "rows": []}]
        for change in (lambda d: d["rows"].pop(),
                       lambda d: d["rows"].append(copy.deepcopy(d["rows"][0])),
                       lambda d: d["rows"][0].update(risks=[]),
                       lambda d: d["rows"][0].update(risks=["not_run", "not_run"]),
                       lambda d: d["rows"][0].update(platform=[]),
                       lambda d: d["rows"][0].update(status={}),
                       lambda d: d["rows"][0].update(risks=[{}]),
                       lambda d: d["rows"][0].update(evidencePath="private-sentinel")):
            document = unresolved()
            change(document)
            controls.append(document)
        for document in controls:
            with self.subTest(document=document):
                errors = gate.validate(document)
                self.assertTrue(errors)
                self.assertNotIn("private-sentinel", str(errors))

    def test_CliRejectsInvalidJsonAndDuplicateKeysWithoutContent(self):
        for source in ('private-sentinel', '{"schemaVersion":1,"schemaVersion":1,"rows":[]}'):
            with patch("sys.stdin", io.StringIO(source)), contextlib.redirect_stdout(io.StringIO()) as output:
                self.assertEqual(1, gate.main())
            self.assertEqual(["input:invalid"], json.loads(output.getvalue())["errors"])
            self.assertNotIn("private-sentinel", output.getvalue())


if __name__ == "__main__":
    unittest.main()
