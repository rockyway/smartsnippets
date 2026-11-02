# 010 — Uno resident-memory measurement and tray trimming

Request: mem-01. Windows-native WinUI head retained; no renderer port.

## Measured decision

Nine isolated Native AOT variants, 200 synthetic snippets each, sequential S1–S6
runs on the granted non-primary DISPLAY1, physical bounds
(2560,-627)-(3760,1293). All runs used file commands, nonactivating previews,
throwaway `%TEMP%/ss-mem-01-*` profiles, and no clipboard/startup registration.
The initial DISPLAY29 grant was unusable; the operator explicitly granted
DISPLAY1 instead. A cloaked warmup initially failed the containment guard;
measurement-only warmup was corrected before the nine successful runs.

**Keep only delayed tray collection plus working-set trimming.** Do not change
normal GC configuration, retain/release main pages differently, or alter backdrop.

| State | Baseline WS MiB | Trim WS MiB | Baseline private MiB | Trim private MiB |
|---|---:|---:|---:|---:|
| S1 background, 20s | 205.55 | 11.70 | 170.48 | 174.91 |
| S2 main visible, 10s | 222.54 | 60.81 | 192.20 | 196.67 |
| S3 main hidden, 20s | 227.16 | 5.79 | 197.29 | 196.67 |
| S4 preview visible, 5s | 224.76 | 39.54 | 197.20 | 196.84 |
| S5 preview hidden, 20s | 224.38 | 8.57 | 197.10 | 192.88 |
| S6 hidden, another 60s | 224.17 | 10.46 | 196.72 | 192.50 |

Dedicated/shared GPU MiB are unchanged between baseline and trim:
8.08/39.83 at S1, 16.42/50.78 at S2–S3, 17.48/50.88 at S4–S6.
S6 resident working set falls 95.3%, but private bytes fall only 2.1%.
This is resident-page reclamation, **not** a 95% reduction in committed memory.
The user's original real-instance reference (152/130/119 MB) is not directly
comparable to this synthetic profile, display configuration and GPU counters.

- GC-only: S6 213.34 WS / 195.71 private; adding it to trim gives
  10.56 / 193.46, versus trim alone 10.46 / 192.50. Drop: no consistent
  incremental benefit, and background-GC/collection tradeoffs are unnecessary.
- Main UI release: S6 212.11 / 194.54 alone. With trim, 17.32 / 187.80:
  a small private saving, but higher warmed WS and higher dedicated GPU
  (20.41 vs 17.48), plus page reconstruction/subscription complexity. Drop.
- Backdrop/acrylic off: S6 213.50 / 195.78, GPU 17.48/52.13. No GPU saving;
  existing main/popup backgrounds are solid and menus already disable desktop
  acrylic. Report-only; drop.
- All candidates combined: S6 17.34 / 187.77, GPU 20.35/47.10. No advantage
  over trim alone for the primary resident-memory goal. GC+release and all
  pairwise combinations were also measured; none is promoted.

One pass per variant, no native latency benchmark: small differences are noisy.
All successful runs exercised main re-show after S6 and retained the same warm
popup/readiness path. Working-set trimming can cause subsequent page faults;
no claim of unchanged native show latency is made.

## Shipped implementation

`IdleMemoryTrimmer` polls on the UI dispatcher. After three continuous idle
seconds it requests one blocking compacting full GC (LOH CompactOnce), then
`SetProcessWorkingSetSizeEx(-1,-1)` using LibraryImport. It never releases UI,
VMs, popup surfaces or caches. Showing/busy work resets the delay. The native
boundary requires hidden main and popup plus a ready warm popup; the host
also excludes pending show/preview/capture, editor work, dirty main edits,
clipboard/insertion recovery, management operations and shutdown. Failed trims
are best-effort with a fixed content-free receipt; disposal stops/unsubscribes
the timer. No periodic forced collection during a single hidden epoch.

Measurement switches and profile/display overrides remain compile-time-only;
ordinary builds cannot access those environment/command paths. Measurement
builds disable the production timer so baseline and candidate switches remain
independent. Measurement trim delegates to the same native routine as production.

## Verification / safety

- Full Release suites: Core **436**, Presentation **617**, Uno **166** passed.
- First full Uno run caught the old all-source environment prohibition; the
  audit now checks product sources while the separate isolation test pins the
  exact Measurement directory's normal-build exclusion. No AOT gate relaxed.
- New tests exercise three-second cancellation/epochs, service failure/disposal,
  pending/visible previews, dirty editors, operations and host disposal.
- Final strict normal x64 AOT/SQLite publish: zero warnings,
  token `62b887cc749f44dcb64e382e40550d9f`, executable SHA256
  `70083326c104d00480e745f80bb3e187bcf327b00bcab0a0ecea386c1f333473`.
- Desktop work ended at 07:50:03Z, before the 07:53:40Z 35-minute cutoff.
  `SCREEN-DONE` was written immediately to the private report. Independent
  cleanup check: zero owned measurement processes and zero remaining
  `%TEMP%/ss-mem-01-*` directories. Real profile, clipboard and Run key untouched.
- No launch of the final product against the real profile; final service
  composition is headlessly tested and strict-AOT built, using the measured
  native trimming routine. No further desktop slot was used after SCREEN-DONE.

Private per-variant CSVs, full four-metric tables, executable hashes, commits and
cleanup evidence are in the operator's mem-01 scratchpad report. No push.
