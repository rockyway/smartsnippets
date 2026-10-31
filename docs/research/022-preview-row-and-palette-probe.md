# 022 — Programmatic preview-row and palette probe

Status: partial Plan 018 T3, not native pointer acceptance or full desktop feasibility.

## Native execution evidence

| Host | Build token | Native entry SHA256 | Lifecycle token | Preview-row token |
|---|---|---|---|---|
| Windows x64 | `e64d53f9fa254f95a6ddb6ac724849e2` | `5b646025d9a8117503a4692d305b7947a6ddf3a2fc2ef1b3d5b9eecb72bf9acf` | `ss-uno018-ui-c_1dwsi5` | `ss-uno018-ui-vbfy5s_2` |
| Apple Silicon macOS | `O6c1RB` | `2f07a92fee6b5c535f9aa7e62ce4e088572a05bc91ae82cdff388bc2de7db2c8` | `ss-uno018-ui-u4ft1um8` | `ss-uno018-ui-_tdlbeul` |

Both clean native collectors report BUILD_CHECKS_ONLY with no warnings/errors. Separately authorized, hash-pinned lifecycle and preview-row runs each observed owned windows, matched ordered receipts, and exited naturally with code zero and no stderr. No input synthesis or clipboard writes occurred. Windows windows remained on the bottom-left monitor.

## Actual wiring and negative boundaries

The new `--preview-row` runner scene selects `--preview-row` behind the existing explicit UI authorization and mutual-exclusion gate. It does not prepare foreground, post Enter, click the mouse, or use stdin handshakes. After main-window initialization, the real coordinator performs cloaked popup warmup. The new programmatic scene calls the **same row method** as the real ListView ItemClick handler; the method requires realized container/selected item/model identity and preview mode before calling real `PopupViewModel.InsertRowAsync`.

This reaches the real preview hide path. A fresh real SQLite store proves unchanged text, label, custom-label flag, folder, tags, timestamps and zero usage; forbidden insertion/clipboard/service boundaries count and throw. Clipboard epoch must remain unchanged. Native popup absence, managed close callbacks, private-profile removal and graceful process exit are required. The original native Enter scene remains separate and retains its held-input safety guards.

This verifies **programmatic shared-handler routing**, not physical mouse delivery, UI Automation invocation, or the ListView ItemClick event itself. Those are still unverified. Mac native Enter remains blocked by its reported held-input state; the programmatic row result is not a substitute PASS for that path.

## Palette resource assertion and real failure correction

The lifecycle probe now asserts actual opaque SolidColorBrush ARGB values from its own theme resource before and after switching to the opposite theme: Light `FFF4F4F4`, Dark `FF202020`. These are **probe-owned values**, not a claim of production palette parity or pixel-identical rendering across platforms.

An initial Windows runtime attempt failed the new brush-type assertion despite a clean Native AOT build (`ss-uno018-ui-0vfax5r2`). Diagnostic run `ss-uno018-ui-4nj9gtnu` showed the failed concrete-brush projection with fixed numeric theme/color diagnostics. Theme dictionary brushes are now constructed as real managed SolidColorBrush objects before XAML initialization, retaining the concrete Native AOT projection at the actual resource seam. The ThemeResource binding and exact color assertions were not removed or relaxed. Fresh lifecycle runs above pass on both hosts.

## Regression boundary

Both hosts pass Core 432, Presentation 599 and Python 159 tests (Windows one platform skip; macOS 29 skips). The separate, uncommitted T4 batch also passes 22 identity-policy/protocol tests on both hosts, using substituted native/lease boundaries; these do not establish native adapter runtime acceptance. Independent medium-effort argon review found no P0/P1 issues for this T3 checkpoint; its two documentation consistency notes were corrected. The current target-adapter integration remains a separate in-progress T4 batch. No production project/package/CI change or Plan 019 start is implied.
