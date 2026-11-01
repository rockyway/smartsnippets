# 042 — Rendered file-picker harness review BLOCK

Worker `f864bb8`, integrated as `a808e84`, implements a source-closed rendered
Import/Export/Cancel fixture. No native picker, UIA or file-picker execution has
occurred. All UI/native review flags remain false. Passing tests/builds do not
clear the following findings.

Review `3ca40b49-2c27-4fb9-a743-e33cb406949c` read the complete eight-file patch
and actual candidate file/parser/atomic-writer/ownership/harness boundaries.
Full report:
`C:/Users/tamtr/.pi/agent/sessions/--D--sources-demo-smartsnippets--/subagent-artifacts/3ca40b49-2c27-4fb9-a743-e33cb406949c_reviewer_output.md`.

## Verified design boundary

Actual rendered Import/Export invokes real `Microsoft.Windows.Storage.Pickers`,
real parser/store and atomic writer. Picker discovery is restricted to the
retained main's `GW_ENABLEDPOPUP`, direct main owner, same candidate PID/main
thread, exact `#32770` class and explicit unique control-ID schemas. Brokered,
other-thread or unknown providers must remain BLOCKED, with no desktop/title
search or PID adoption. No synthetic keyboard/mouse or production code edits
were added.

## Required single-batch fixes before any native picker run

1. **P1 foreground latch:** PowerShell `Guard` must latch first authenticated
   picker HWND and monotonic owned-foreground observation, matching Python. After
   owned main/picker gains foreground, original foreign foreground cannot become
   an allowed mutation context again. Reject replacement picker handles before
   SetValue/Invoke.
2. **P1 phase publication/close ordering/time budget:** publish `.active` and
   `.closed` markers atomically via exclusive temporary file, dispose, then Move;
   use `lexists` so dangling aliases are refused, not treated as absent. Tolerate
   hiding/destruction only for the previously authenticated exact picker handle,
   never an unknown/distinct HWND. Close the picker phase immediately after its
   authenticated close, before dismissing the main-window info dialog. Increase
   fixture phase wait to 25s for this scene (12s otherwise) to match bounded open,
   close and dialog action budgets.
3. **P1 files/permit/privacy:** reject hardlinks on prepared/imported/exported
   files and the scene permit; require exact permit length 25 before reading.
   Validate import/export bytes through existing single-link/reparse/size-capped
   owned-file helpers (4096/16384 caps). Reject dangling existing export entries.
   Add fixed tag and basename canaries (`picker-tag`, `picker-files`,
   `import.json`, `export.json`) to the diagnostic leakage scanner.
4. **P2 fold-in:** preserve all fixed `picker_*` refusal tokens in helper error
   allowlist; use existing exact main `ImportSnippetsButton` /
   `ExportSnippetsButton` AutomationIds instead of Name-only selection.

Add negative/race/latch/alias/timeout tests, rerun candidate Python and real
candidate/fixture tests sequentially, parse PowerShell, publish strict fixture
BuildOnly, and obtain a confirming bounded review. The main-thread/provider
schema remains a deliberately conservative runtime prerequisite, not a reason
to relax ownership or discover unowned dialogs.

## Checkpoint validation and disposition

After integration and separate native-mutation permit hardening (research 041):
436 Core, 616 Presentation, 276 candidate, 138 native-fixture and 131 candidate
Python tests pass. The file-picker worker also passed PowerShell parsing and a
strict BuildOnly publish. Native acceptance is **NOT RUN / review BLOCKED**.
No source gate was enabled for this new picker scene and no failure is hidden as
PASS. The subsequent fixes batch implements the requested hardening and tests;
independent confirming review remains outstanding, so this review stays BLOCKED.
Build-only and headless checks do not establish native picker behavior.
