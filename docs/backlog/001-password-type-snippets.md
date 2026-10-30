# 001 — Password-type snippets and masked previews

**Status:** Backlog — requirement captured, not designed or approved for implementation
**Requested:** 2026-10-29
**Related work:** [012 — Shared snippet hover details](../plan/012-shared-snippet-hover-details.md)

## Request

Introduce an explicit password/sensitive-content snippet type. Its content must be masked in normal previews and hover surfaces rather than displayed as plaintext. The user requested this as separate future work while approving actual-content hover for current ordinary snippets.

## Current safety boundary

The domain has no password/sensitive type. All existing snippets remain ordinary text and may be shown unmasked on hover. This backlog item provides **no masking, encryption or password-manager guarantee**. Do not infer sensitivity from text, tags or folder names.

## Required discovery before planning implementation

- Define how users mark/unmark a sensitive snippet, treatment of existing snippets, and whether reveal is ever allowed; confirm authentication, timeout and re-masking expectations.
- Specify masked behavior across hotkey/main lists, hover, editors, auto-generated labels, search results, accessibility/automation, notifications and any future preview surface.
- Decide persistence/migration and threat model: masking alone is not encryption or secure storage. Define whether encryption/key management is required and who can decrypt.
- Define clipboard/insertion/clipboard-restoration behavior, usage recording and exposure to clipboard history or other processes.
- Define safe import/export/backup behavior and compatibility with formats that identify secret entries; do not silently weaken existing secret-import exclusions.
- Audit logging, diagnostics and crash-report exposure; never treat hidden UI as protection against plaintext in logs, storage or exports.
- Specify tests proving masked surfaces never accidentally fall back to plaintext, including live edits, recycling, cache refresh, restart and accessibility paths.

## Proposed completion criteria (must be confirmed in a separate brief)

A separately approved design defines the sensitive type and its security boundaries; all normal content-preview/hover paths display masked data for that type; any explicit reveal, storage, clipboard and export behavior follows that design. Ordinary snippets retain their existing behavior. No implementation is included in plan 012.
