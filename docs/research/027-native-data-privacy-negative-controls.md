# 027 — Native data privacy negative controls

Status: partial Plan 018 T6 hardening, cleared in consolidated milestone review. Not full privacy/recovery acceptance.

## Actual native execution

| Host | Native build | Entry SHA256 |
|---|---|---|
| Windows x64 | `652b0b83af884efeac60784701b7b508` | `9dadde697f91393a27db161c5ac0a598c0a23eb4d6c8861cc17e355dd43fc510` |
| Apple Silicon macOS | `OtxGHG` | `895d86030397fd1de2251d763946c5e0a74a6cfaef744e45bd67faa9b2712dfb` |

Both strict native collectors report zero warnings/errors and BUILD_CHECKS_ONLY. Separately, each hash-pinned `run_data_probe.py` invocation passes real seed and independent restart verification, ending `data_probe_native_restart_passed gui=NOT_RUN`. No GUI, input or clipboard path is run.

## Wrong-implementation controls

DataProbe's actual AppLog scan now uses one pure policy for the real log and in-memory planted leak controls. The real trace must contain its expected event and none of its synthetic body/label/tag/folder canaries. Each planted canary must fail that same scanner; an always-success implementation cannot satisfy native acceptance. Empty input also fails. Planted strings are never written to logs or process output.

The native-data runner now accepts exactly one full stage-specific receipt plus its newline and empty stderr. Prefix-only success, duplicate/extra/truncated receipts, wrong-stage receipts, nonzero status and any stderr fail without echoing the supplied private payload. Real subprocess text-mode newline normalization supports Windows CRLF and macOS LF without loosening the receipt contract.

The Python desktop suite runs 181 tests on each host (Windows one skip, macOS 38 skips). Tests include fabricated leaked payloads and runner rejection controls. Parent code inspection, native regressions and consolidated security review are complete; no privacy issues were found in this bounded change.

## Limits

These canaries cover the exercised real data trace, not window-title logging, arbitrary Unicode escaping/encoding, every exception path or operator clipboard payloads. Strict stdout contracts constrain those particular data processes; they are not a universal log scrubber. Full T6 host manifests, native recovery/error matrices and end-to-end wiring evidence remain open.
