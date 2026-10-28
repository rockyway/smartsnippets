# Independent plan review 010 — round 2

**Verdict: changes required.** Reviewed the entire revised plan (1,955 lines), the first report, and relevant actual code/tests/workflow on `main`, still at `27f08ba`. Read `AGENTS.md` first. No builds/tests/publishes or repository edits were performed. This report is the only file written. “Resolved” below means resolved **in the proposed plan**, not implemented or experimentally verified.

Classification: **A** missed sites of the same change class; **B** weak oracle, with a wrong implementation that passes; **C** unsupported claim; **D** genuine design/code-fact defect. Plan line numbers refer to this revision.

## 1. Audit of the original 13 findings

| Original finding | Round-2 result against actual task text, not just §11 |
|---|---|
| 1. Native restore/text-only dedup | **User decision incorporated, implementation prescription still defective.** DedupeMode is carried by ImportBatch, native mapper sets FullRow, T5/T6/T7 exercise same-text/different-metadata rows, and the remaining collapse is explicitly documented. However, the recommended key representations can miscompare rows (R2 below). |
| 2. Exported files exceed import cap | **Resolved at policy/boundary level.** A2 appears in the real constant, reader, compact source-gen context, result type, messages, T7 real-file boundary tests and T9 no-write-call test. The 32 MB refusal is user-approved, not scope creep. Serialization responsiveness is still not covered; see improvements. |
| 3. Usage overflow | **Original missed-writer defect addressed, new preservation defect introduced.** SQL, store snapshot, reader and fake changes are all prescribed in §4.3.2 and T6; the display/sort consumers do not need saturation arithmetic. But the one-billion import clamp corrupts native restore after ordinary usage (R1). |
| 4. False “Nothing imported” after COMMIT | **Core issue resolved.** D9 handles individual observer exceptions; the stage table handles other in-commit failures with OutcomeUnknown. T6 real-store and T9 real-view tests cover acknowledgement and later subscribers. Busy ownership around unexpected-error dialogs remains inconsistent (R6). |
| 5. Guarded logs exception messages | **Resolved for the feature's actual call graph.** Guarded changes store-wide; new observer/import/export error logs pass null exception arguments; failure tests assert log-event presence before sentinel absence. AppLog's intentional general-purpose message behavior remains unchanged. |
| 6. Mid-read error mapping | **Expected I/O cases resolved:** first/mid-read and unauthorized failures are explicitly mapped/tested in T1/T9, including no store call and busy reset. The new total-reader wording contradicts the unexpected-error test (R4). |
| 7. Byte cap mistaken for time bound | **Honest conditional design now, not demonstrated performance.** Worker parsing, current-snapshot commit, dense/folder-dense/typical cases and stop/report policy are real additions. However, the advertised CI Release gate is not wired into any task that edits CI (R7). |
| 8. Run cannot produce I/O refusals / inaccessible helper tests | **Resolved.** Commit takes ParsedImport and owns no refusals; T1/T2/T9 own their actual boundaries. ValueRulesTests move to T3 and use public Parse; no friend assembly is needed. |
| 9. Triggers cannot prove zero calls | **Resolved.** T9 requires ImportCalls == 0 and ThrowOnImport. T6 now reserves triggers for no-write and rollback proofs. |
| 10. UI-thread-hop oracle | **Partially resolved.** Owner-thread recorder and suspending picker/read fakes are now explicit. The parse await itself is not deterministically suspended despite the stronger claim (R5). |
| 11. Finite Int64 numeric boundary | **Resolved.** Compare/clamp before cast and boundary/scientific/huge-finite tests cover the formerly unsafe class. The separate one-billion policy issue is R1, not a conversion bug. |
| 12. Impossible T11 grep | **Resolved.** Absence of the old exclusion and presence of the actual new wording are separate checks; T12 delegates to them. |
| 13. ScriptedDialogService declaration line | **Resolved:** line 6. A newly expanded insertion-test citation is off by two lines; see improvements. |

Thus §11 is mostly backed by task changes, but “every other finding is fixed” (lines 31–33) overstates the remaining oracle/gate issues.

## 2. Blocking and substantive findings

### R1 — D / High: D8 destroys native statistics even when no full-row collision exists

**Locations:** D8 (lines 158–161), shared Count rule (§4.3.5), T6 lines 1393–1400, AC4/AC7.

The original overflow fix required saturation across writers/readers. It did not require shrinking an otherwise representable native count to one billion. The revised task explicitly mandates this sequence:

1. Import native usage `1000000000`.
2. Record three successful uses; snapshot and reopened DB contain `1000000003`.
3. Export `1000000003`.
4. Restore into an empty library and expect **`1000000000`**.

That is an explicit test of **data loss**, not a lossless-backup oracle. The row need not duplicate any other row. The user's A1 exception concerns repeated full-row keys; A2 concerns file size. Neither approves discarding statistics of a single distinct native row. AC4 still says usage is kept; amended AC7 excepts repeated keys, not counts above one billion.

**Concrete failure:** a user imports an accepted at-cap row, pastes it just once, exports, and restores on a new machine. One recorded use vanishes. Larger valid counts, including existing rows at int.MaxValue, lose much more.

**Required:** preserve native counts through int.MaxValue and retain the proposed saturating writers, or obtain an explicit additional requirement amendment and change every preservation claim. If a lower cap is desired for third-party data, make it format-specific. The real-store chain test must expect exact native export/import preservation, not pin the loss.

**Whole-class check:** actual arithmetic sites are `SnippetRepository.cs:120`, `SnippetStore.cs:255`, `SnippetRepository.cs:136`, and `Fakes/FakeSnippetStore.cs:201`; the plan names all of them. `SnippetSort.cs:22` uses CompareTo (not subtraction), UiText's usage functions just format ints, popup sorts through SnippetSort, and `InsertionService.cs:126` is the sole production RecordUsage caller. No additional normal usage arithmetic consumer was found. A schema change is still unnecessary.

### R2 — D / High: both suggested full-row key shortcuts are unsafe without additional equality/encoding rules

**Location:** §4.3.5 lines 830–835: full-row equality is suggested “as a record struct or a string with `\u001F` separators.”

**Failure A — delimiter collision:** neither SnippetValidator nor the JSON rules forbid U+001F inside text/labels/tags. These two distinct rows serialize to the same naive separator-joined text/label prefix:

- text = `a\u001Fb`, label = `c`;
- text = `a`, label = `b\u001Fc`.

With identical folder/tags, their concatenated keys collide. The second legitimate row is silently skipped. The summary still balances, so the counts invariant cannot detect the loss. Similar collisions occur between tag elements or component boundaries.

**Failure B — collection reference equality:** a default record struct containing IReadOnlyList<string>/string[] does **not** automatically compare tag elements structurally. Separately materialized `["mail"]` lists can compare unequal. Reimporting the same native row then adds a duplicate even though every field value matches. Some existing FullRow tests may expose this variant, but the plan still offers it as an implementation approach.

**Required:** specify collision-free typed component equality with explicit ordinal, order-sensitive tag-sequence equality, or an unambiguous length-prefixed/escaped encoding. Require tests for embedded separators crossing text/label boundaries, separators inside tags, empty versus absent tags, and separately allocated equal tag arrays, both library-versus-file and in-file.

Also repair the §4.4 diagram (lines 981–982): it currently puts dedupe **before** labels/tags, whereas the authoritative FullRow rule requires resolved labels and normalized tags before key construction. Following the diagram literally can miss equivalent final rows. The implementer should not have to choose between conflicting prescribed sequences.

### R3 — D / Medium: T6 now depends on the T7 implementation, contradicting the mandatory task order

**Locations:** T6 lines 1393–1400; T7 starts line 1448; §§6/8 require T6 verification before T7.

T6's required usage-chain test calls NativeExport and asserts its output and reimport. But NativeExport.cs/NativeJsonContext.cs are first implemented in **T7**. T0 adds only contracts/results; it does not add the NativeExport implementation. Under the stated red-first/pass-each-task rules, a correct T6 implementation cannot complete its required tests before T7.

**Concrete failure:** implement T0–T6 in order. The new chain test references a nonexistent NativeExport type/method, or a stub that cannot satisfy its byte assertions. The standing instruction demands stopping, not moving the test or implementing T7 early.

**Required:** split the T6 chain at reopen and put the export/reimport extension in T7, or reorder/extract the export dependency explicitly. Fix the wrong usage expectation there as part of R1.

### R4 — D / Medium: reader's “only cancellation escapes” contract conflicts with T9's required unexpected-error scenario

**Locations:** Core API lines 539–540; T9 lines 1563–1565.

CappedReader's exact contract says **“no exception escapes except OperationCanceledException.”** T9 then requires an InvalidDataException from a fake stream to escape CappedReader to the relay wrapper, expressly because it is not mapped.

InvalidDataException is not an IOException, despite its namespace. The two requirements cannot both hold: either the reader converts/suppresses this error, so the T9 expected wrapper branch is unreachable, or it propagates, breaking the total contract.

**Required:** narrow the contract to mapping the two expected I/O types while allowing unexpected exceptions to propagate, or choose a different reachable pre-commit fault seam. Then test actual reader ownership and the wrapper branch separately. Do not weaken a test to accept either behavior.

Reference checked: https://learn.microsoft.com/en-us/dotnet/api/system.io.invaliddataexception?view=net-10.0

### R5 — B / Medium: suspension after Task.Run is not guaranteed by suspending file reads

**Locations:** T9 lines 1585–1595; §11 finding-10 response.

The new test forces asynchronous picker/read completion and records the store/Changed thread. That is a real improvement. It does **not** ensure the Task.Run parse task remains incomplete when the view-model awaits it. A tiny fixture can finish on another worker before the await is evaluated.

**Wrong implementation that can pass:** retain normal awaits for picker/read, but use `await Task.Run(...).ConfigureAwait(false)` for parsing. If parsing has already completed, execution remains on the UI thread; both recorded-thread assertions pass. On a larger/slower file the same implementation commits from a worker. Therefore the claim “adds ConfigureAwait(false) ... after ... parse fails this test” is not deterministic.

**Required:** provide a controlled parse-execution/completion seam or equivalent deterministic scheduling arrangement that keeps the parse await pending until the owner yields and completes it from a worker. Assert store and Changed thread **outside** the isolated event handler. A Delay-based read fake tests the read hop, not the parse hop. The static Task.Run grep is useful audit evidence but not a substitute for the claimed test oracle.

### R6 — D / Medium: unexpected-error dialogs sit outside the stated busy lifetime

**Locations:** view-model contract lines 680–691; §4.4 `finally IsBusy = false` at line 1004; wrapper behavior lines 1007–1016 and 1533–1536.

ImportAsync resets IsBusy in finally. Unexpected exceptions propagate **out of ImportAsync** to Import(), which then shows the stage-specific error dialog. In C#, that finally runs before the wrapper catch. Thus the commands are re-enabled while the unexpected-error dialog is still open, contrary to “true from picker open to dialog close, for both commands.” The existing busy test holds only the successful InfoGate.

**Concrete failure:** a pre-commit unexpected stream exception reaches the wrapper; ImportAsync has already cleared IsBusy. While its error dialog is held open, a second ImportAsync/direct command invocation is accepted and can open a picker or change the shared stage field. Successful-path exclusivity tests all pass.

**Required:** one operation owner must hold busy through wrapper handling and dialog dismissal, while preserving the public direct-call guard and final reset on every exit. Add an unexpected-error ErrorGate test asserting both commands disabled and second invocation ignored until dismissal. Apply the same ownership rule to export.

### R7 — A / Medium: the advertised binding CI budget run has no CI implementation task

**Locations:** R2 line 1088 says **“Release build, CI Windows runner”**; T9/10/§8 give a Release command; no task edits `.github/workflows/ci.yml`.

**Actual workflow:** both test jobs invoke dotnet test without `-c Release`. The Windows publish step builds the app in Release, but does not rerun Presentation tests in Release. Consequently `workflow_dispatch` runs the relaxed DEBUG budget (10,000 ms), not the binding 2,000 ms budget.

**Concrete failure:** a 6-second commit passes all existing workflow steps, including AOT publishing. The stated CI 2-second gate has not run. A separate developer-PC run also does not satisfy the specifically chosen CI-runner reference environment.

**Required:** add the sequential Windows Release budget step and its file/task/verification entry, or explicitly choose a different binding environment and evidence procedure. The current manual command can be valid merge evidence on a chosen Windows machine, but it must not be called an already-wired CI runner gate.

**Related B oracle weakness:** budget tests currently only prescribe measuring Commit and asserting time. A density-dependent early return or a Changed subscriber that throws halfway through rebuilding can make the timed call fast. D9 swallows that error by design. Require expected imported counts, balanced summary, persisted rows, representative refreshed view contents and **no store_subscriber_failed event** for each budget case. Otherwise the timer can certify incomplete work.

### R8 — D / Medium: round-trip comparison still treats remapped folder ids as preserved fields

**Locations:** T7 lines 1461–1474: seed through CreateFolder/CreateSnippet, export all folders by name, then “Compare every field except id as multisets ... plus the folder name set.”

The real Snippet record contains **FolderId** (`Domain/Snippet.cs:8`), and folders are exported in **name order**, not creation-id order. New ids on import are expressly required. Comparing every Snippet field except its Id can therefore reject a correct implementation.

**Concrete failure:** create Work first (folder id 1), Home second (id 2), Empty third (id 3). Export order is Empty, Home, Work. An empty target creates them in that order, making Work id 3. A Work snippet's membership is correct, but its FolderId is now 3 rather than 1. The proposed literal comparison fails despite a correct round trip.

**Required:** define the comparison projection explicitly: folder **name** joined through each store, not FolderId; snippet id excluded; tags compared as ordered sequences; the remaining semantic fields retained as a multiset. Keep the independent folder-name set to check empty folders. This is an oracle correction, not a request to preserve file ids.

## 3. Design checks and remaining improvements

### Worker parsing, snapshots, reentrancy and cancellation

The intended design has **no stale-library-snapshot race**: Task.Run receives only bytes/now; ImportPlanner reads `_snippets`/`_folders` inside the synchronous UI-owned ImportSnippets call. Editing while parsing is allowed, and the commit sees the latest saved library. No await belongs between that snapshot, dedupe and transaction. The initial wording “snapshot at import start” should explicitly mean **commit start**, not picker/parse start.

**B / Medium improvement:** there is no required test that mutates the saved library while a read/parse is suspended. An implementation that caches/filter-dedupes against a pre-parse snapshot can pass the current static-library tests. Add controlled interleaving cases: create a matching snippet during parse (it must be skipped), delete a matching row during parse (it must now import), and rename/create a matching folder during parse (resolve the current folder names/ids). Assert balanced counts and one/no event as appropriate. This protects the newly introduced async interval rather than merely testing that a preexisting dirty editor remains dirty.

Normal second-click handling is sound if IsBusy is set before the first await and remains owned by the whole operation; R6 covers its exceptional-path hole. Picker cancellation remains a no-op. There is no user-facing cancellation token/cancel operation after picking, so the plan should not imply parse/commit can be cancelled. CappedReader's cancellation behavior can be tested independently. Do not move store access onto Task.Run to “fix” responsiveness.

### D9: behavior, tests, reflection and thread assertion

- Typed per-subscriber invocation and catch/log/continue intentionally change **all** store mutation semantics. That is defensible hardening, provided it is approved as such. The actual current tests inspected record events externally; no existing expected subscriber-exception propagation dependency was found. Runtime test success is not established by this review.
- The existing Debug UI-thread assertion is called **before** Raise (`SnippetStore.cs:239–241`, similarly other writes). Isolating handlers does not catch or remove it. However, in Release a wrong-thread commit could now produce logged/swallowed view exceptions instead of an obvious caller failure. Explicit thread oracles remain essential.
- **C / Low:** the diagnostic expression `handler.Method.DeclaringType?.Name` (line 662) obtains reflection MethodInfo metadata, contrary to the literal “no reflection” instruction. It is not reflection-based delegate invocation, and I am **not** claiming it necessarily causes an AOT warning. Prefer a stable ordinal or safe known subscriber identifier, or explicitly approve this narrow metadata exception. Do not introduce DynamicInvoke.
- Because D9 can hide a broken view, tests should assert recorded values outside handlers and verify normal successful imports/budgets produce no subscriber-failure logs. The “always returns after COMMIT” wording is also too absolute: the stage table correctly admits Publish can throw. Say subscriber failure alone cannot escape, rather than promising every post-commit allocation/publication always succeeds.

### D10: all relevant logging sites

The revised Guarded change covers every store operation that reuses it, not just import. New import/export error logs and the D9 handler log explicitly pass null exception objects. Existing consumer paths relevant to Changed contain no required new message logging. AppHost/Migrator/navigation/clipboard/startup error logs still pass exceptions, but they are unrelated general-purpose callers; D10 does not require rewriting the whole logging system. AppHost's StoreException outer messages are fixed/user-safe, and AppLog does not recursively print inner exceptions. No additional direct feature log leak was identified. Keep failure-stage/path sentinels and presence assertions as prescribed.

### Compact export and atomic writer

The proposed compact context, exact escaped-byte size check, same-directory temp file and File.Move(overwrite:true) form a coherent local same-volume replacement design. A target opened without delete sharing should cause rename failure and preserve the original. The helper's separation and injected failure tests address the first review's missing non-WinUI I/O surface. Specify disposal/closure of the temp stream **before** the move (essential on Windows); the real Windows success test should enforce it.

**C / Low:** ISnippetFileService still promises the temp file “is deleted” on every failure, whereas AtomicFileWriter explicitly promises only best effort and tests deletion failure. The helper's wording is realistic; align the outer contract and risk table. A cleanup-permission error must preserve the original operation exception, but cannot guarantee no temp remains.

**A / Medium improvement:** only import parsing moved off the UI thread. NativeExport.Write still serializes the **whole** library synchronously before its size refusal, including an arbitrarily large library. The 32 MB cap bounds accepted output, not work done before rejecting oversized output. An oversized backup can still freeze the UI or allocate very large buffers before producing its promised message. Consider serializing captured immutable snapshots off-thread and/or counting/bounding the output writer; at minimum add export-stage responsiveness/oversize evidence. The current commit-only budget measures neither export serialization nor import UTF-8 validation.

T9 also tells **both** relay wrappers to apply the import-only Failure stages table (lines 1533–1536). Define export's own before-write/in-write/after-write error outcomes and a summary-dialog-throws-after-success test, so it cannot claim the old target was unchanged after a successful move or show import wording for an export error.

### A1 edge cases and counts invariant

Apart from R2, the intended equality is well specified: ordinal labels, folder-name rule keys, ordered normalized tags; library keys use joined folder names, not ids; stats/custom-label flag excluded intentionally. Auto-label resolution and comma/tag normalization must precede key construction. Explicit native folders remain the documented no-incidental-folder exception.

Parser candidate/skipped/rejected partition plus planned/duplicate/rejected partition still provides a sound summary accounting model. Full-row changes do not inherently break it. **Balanced counts do not prove correct deduplication**: a false collision simply increases duplicates instead of imported while preserving the sum. R2's adversarial equality tests are therefore needed in addition to balance checks.

### Scope, tasks, criteria and references

- All AC1–AC10 still have real task mappings. No criterion is wholly taskless. T0–T9 provide verification/test lists; T10 uses Windows compilation/runbook evidence; T11/T12 are legitimate document/audit tasks. R3 is the material dependency/order defect.
- A1/A2 implement the user's stated decisions. Store-wide saturation, observer isolation and privacy hardening are broader than adding buttons but are directly motivated and identified in §8. Do not describe them as inert: **C / Low**, §8 still says “T0–T9 are inert without T10 (no button),” which is false for D8–D10. Those change existing behavior even without any new picker surface.
- D8's one-billion **native** truncation is an additional requirement change, not a consequence of A1/A2 or saturation. The header's suggestion that all other decisions were already confirmed sound by review should not imply prior approval of newly added D8–D10.
- D1/D5/D7 remain explicit G2 decisions. Q1 is an honest unresolved performance gate: approving a measurement-first implementation need not assert the dense 32 MB case will meet 2 seconds. No performance result is claimed here.
- The corrected ScriptedDialogService.cs:6 citation is accurate. New usage-consumer lines (`SnippetSort:22`, UiText:21/32, RowVM:19, EditorVM:520, InsertionService:126) match actual code.
- **C / Low:** §3 line 274 cites forced suspension at InsertionServiceTests.cs:240. It is actually **line 242**; recorded-thread capture is at 240–241 and assertion at 246.
- No newly cited pre-existing API was found nonexistent. New Commit, AtomicFileWriter overloads, DedupeMode and NativeExportResult are proposed additions, not claims that they already exist. The 1.8 picker choice is unchanged from the prior verified review; no newly introduced elevation defect was found. No migration/schema change is needed.
- The three real-sample count claims remain unaffected by A1/A2: all are third-party formats, and all are far below the new cap. The first review's independent 13 / 5 / 34+11, and combined InkSpoke 32+2+11, remain applicable.

## IS THIS PLAN READY TO APPROVE?

**No. Blockers:** native usage truncation contradicts preserved-stat backup; the full-row key prescription permits false collisions/reference equality; T6 depends on unimplemented T7; reader/test exception contracts conflict; the parse-hop test is not deterministic; busy ownership misses wrapper error dialogs; and the stated CI Release gate lacks a CI task. Correct the folder-id round-trip oracle as well.

**Improvements:** lock the async snapshot race with controlled interleaving tests; strengthen budget work-completion checks; clarify D9 diagnostics/remaining failures, export error stages and responsiveness, temp cleanup wording, and the few stale claims/citations. The revision genuinely resolves most original findings, but §11 is not yet sufficient evidence that the whole classes are closed.

REVIEW_COMPLETE
