# Popup polish checklist

- [x] Read AGENTS.md, plan 006 popup design and user screenshot.
- [x] Design and validation gate documented before implementation.
- [x] T1: shared neutral/accent list states, full-width rows (static only).
- [x] T2: thin neutral rounded popup border (static only).
- [x] T3: explicit main-window foreground activation; sweep callers (static only).
- [x] T4: empty-space search focus and cursor-position menus at both sites (static only).
- [x] T5: in-popup Edit, preserved target/interlocks/preview, tests added and passed in authorized round; old route removed/host contract repurposed.
- [x] Static review, committed sources; BUILD_REQUEST polish-01 prepared for append after commit.
- [x] REVIEW D: remove main-window AttachThreadInput fallback; ShowWindow/direct foreground, brief topmost toggle, flash on denial; correct safety wording (static only).
- [x] REVIEW A: shared Browse count inherits container foreground; no ComboBox style/state changes; design claim scoped to repaired templates (static only).
- [x] REVIEW B: A/B/C multi-row edit/save/cache/cancel oracles; edit non-first row, verify loaded subject, unchanged neighbours, selected kind/ID/label and complete insertion target/options (passed in authorized Presentation suite).
- [x] BUILD GO polish-01 15 received; C: mirror of c8cefce verified against 330 tracked non-excluded files; build -m:4 passed (0 warnings/errors), Core 426/426, Presentation 488/488; one AOT publish passed (0 warning/error diagnostics, native exe/SQLite present, no managed App DLL).
- [x] BUILD_FINISHED polish-01 appended; final report records results and outstanding interactive QA.
- [ ] PENDING-USER: authorized desktop QA for light/dark/high contrast, DPI/corners, full-width/blank-area hit testing, menus/focus, minimized/occluded foreground activation, real original-target insertion.

Build evidence: C:\temp\polish-build.log, C:\temp\polish-core-tests.log, C:\temp\polish-presentation-tests.log, C:\temp\ss-stab-build\publish.log. No app launch, push, merge or tag. Documentation-only validation update after tested code commit c8cefce.
