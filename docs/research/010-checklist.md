# 010 implementation checklist

Every task and explicitly named test from plan §6; unchecked means not verified.

## T0 — Shared contracts (AC1–AC8 enabler)

- [x] T0 implementation and task verification
- [x] `IsBalanced_TrueOnlyWhenPartsSumToEntries`

## T1 — Bounded read, encoding and file rules (AC8)

- [x] T1 implementation and task verification (58 Porting cases pass; historical red-first deviation retained in plan §10)
- [x] `MaxImportBytes_Is32MiB`
- [x] `MaxImportEntries_IsCalibratedValue`
- [x] `ExactlyCap_Accepted`
- [x] `CapPlusOne_TooLarge`
- [x] `TooLarge_StopsReadingAtCapPlusOne`
- [x] `LengthLies_StillCapped`
- [x] `ReadThrowsIOException_OnFirstRead_Unreadable`
- [x] `ReadThrowsIOException_AfterSomeBytes_Unreadable`
- [x] `ReadThrowsUnauthorized_Unreadable`
- [x] `ReadCancelled_PropagatesOperationCanceled`
- [x] `ReadThrowsInvalidData_Propagates`
- [x] `Utf8Bom_Stripped`
- [x] `Utf16LeBom_NotUtf8`
- [x] `Utf16BeBom_NotUtf8`
- [x] `Utf32Bom_NotUtf8`
- [x] `InvalidUtf8InsideString_NotUtf8`
- [x] `Empty_ReturnsEmptyBytes` (including empty-input parser hop)
- [x] `HasJsonExtension`
- [x] `ExportBaseName_IsInvariantDate`
- [x] `Write_NewFile_CreatedNoTempLeft`
- [x] `Write_ExistingFile_Replaced`
- [x] `OpenTempFails_TargetUntouched_NoTemp`
- [x] `WriteFailsMidway_TargetUntouched_TempDeleted`
- [x] `MoveFails_TargetUntouched_TempDeleted`
- [x] `TempDeleteFails_OriginalExceptionStillThrown`
- [x] `TempStreamDisposedBeforeMove`

## T2 — Parser: detection, version-first refusal, value rules (AC2, AC8)

- [x] T2 implementation and task verification
- [x] `Detect_NativeMarker_Native`
- [x] `Detect_FormatMemberOtherThanOurs_Unknown`
- [x] `Detect_SnippetsKey_TermFlow`
- [x] `Detect_CommandsKey_Rephlo`
- [x] `Detect_MappingsKey_InkSpoke`
- [x] `Detect_LowercaseMappings_Unknown`
- [x] `Detect_NoKnownKey_Unknown`
- [x] `Detect_RootArray_Unknown`
- [x] `Detect_SnippetsAndCommands_TermFlowWins`
- [x] `Version_TermFlow2_Unsupported_FoundVersion2`
- [x] `Version_TermFlowString1_Unsupported`
- [x] `Version_TermFlow1Point0_Unsupported`
- [x] `Version_TermFlowMissing_UnsupportedNoVersion`
- [x] `Version_Rephlo12_Unsupported`
- [x] `Version_RephloNumber13_Unsupported`
- [x] `Version_InkSpoke3_Unsupported`
- [x] `Version_Native2_Unsupported`
- [x] `Version_CheckedBeforeEntries`
- [x] `Entries_Missing_MissingEntries`
- [x] `Entries_NotArray_MissingEntries`
- [x] `Entries_AtLimit_Accepted`
- [x] `Entries_LimitPlusOne_TooManyEntries_WithCount`
- [x] `Entries_OverLimit_RefusedBeforeAnyEntryRead`
- [x] `Entries_OverLimit_AfterVersionCheck`
- [x] `NativeFolders_ZeroSnippets_OverLimit_Refused_WithCombinedCount`
- [x] `NativeFolders_AtCombinedLimit_Accepted` (2 cases)
- [x] `NativeFolders_CombinedLimitPlusOne_Refused`
- [x] `NativeFolders_MalformedElementsCountTowardLimit`
- [x] `NativeFolders_VersionCheckedBeforeCombinedLimit`
- [x] `InvalidJson_Syntax`
- [x] `InvalidJson_TrailingComma`
- [x] `InvalidJson_Comment`
- [x] `InvalidJson_TrailingGarbage`
- [x] `InvalidJson_DepthOver64`
- [x] `Parse_NeverThrows`

## T3 — TermFlow and native mappers, value rules (AC1, AC3, AC4)

- [x] T3 implementation and task verification (46 cases pass; 44 red against mapper stubs)
- [x] `Date_Int_Kept`
- [x] `Date_1e400_Invalid`
- [x] `Date_Fraction_Invalid`
- [x] `Date_IntegralDouble_Kept`
- [x] `Date_Negative_Invalid`
- [x] `Date_String_Invalid`
- [x] `Date_Future_ClampedToNow`
- [x] `Date_LongMax_ClampedToNow`
- [x] `Date_TwoPow63_ClampedToNow`
- [x] `Date_TwoPow63Scientific_ClampedToNow`
- [x] `Date_1e300_ClampedToNow`
- [x] `Date_LongMin_Invalid`
- [x] `Date_Minus1e300_Invalid`
- [x] `Usage_Negative_Zero`
- [x] `Usage_Fraction_Zero`
- [x] `Usage_String_Zero`
- [x] `Usage_OneBillionAndThree_KeptExactly`
- [x] `Usage_IntMax_KeptExactly`
- [x] `Usage_IntMaxPlusOne_Saturated`
- [x] `Usage_LongMax_Saturated`
- [x] `Usage_TwoPow63_Saturated`
- [x] `Usage_TwoPow63Scientific_Saturated`
- [x] `Usage_1e300_Saturated`
- [x] `Usage_1e400_Zero`
- [x] `UpdatedBeforeCreated_SetToCreated`
- [x] `Text_LoneSurrogate_RejectedInvalidText`
- [x] `Label_LoneSurrogate_RejectedWrongFieldType`
- [x] `Null_IsAbsent`
- [x] `TermFlow_Fixture_PerEntryOutcomes`
- [x] `TermFlow_Fixture_StatsKept`
- [x] `TermFlow_Fixture_StatsDefaulted`
- [x] `TermFlow_IdIgnored_NotRequired`
- [x] `TermFlow_TextVerbatim_NoTrim_LineEndingsKept`
- [x] `TermFlow_LabelWrongType_Rejected`
- [x] `TermFlow_TagsElementNotString_Rejected`
- [x] `TermFlow_ExtraField_Tolerated`
- [x] `Native_Fixture_PerEntryOutcomes`
- [x] `Native_IsLabelCustomFalse_LabelNull`
- [x] `Native_BatchDedupeIsFullRow`
- [x] `TermFlow_BatchDedupeIsTextOnly`
- [x] `Native_FoldersList_IncludesEmptyFolder`
- [x] `Native_FolderElementMalformed_IgnoredNotCounted`

## T4 — Rephlo and InkSpoke mappers (AC1, AC3)

- [x] T4 implementation and task verification (20 cases pass; 18 red against stubs)
- [x] `Rephlo_Fixture_PerEntryOutcomes`
- [x] `Rephlo_FixedContentAppend_NeverAppended`
- [x] `Rephlo_Archived_SkippedBeforeTextCheck`
- [x] `Rephlo_StatsNull_Fresh`
- [x] `InkSpoke_Fixture_PerEntryOutcomes`
- [x] `InkSpoke_SecretInParams_Skipped`
- [x] `InkSpoke_SecretOnMapping_Skipped`
- [x] `InkSpoke_LowercaseTextKey_Imported`
- [x] `InkSpoke_UpperTextWinsOverLower`
- [x] `InkSpoke_EmptyParams_Unparsable`
- [x] `InkSpoke_ParamsArray_Unparsable`
- [x] `InkSpoke_ParameterizedCheckedBeforeParams`
- [x] `InkSpoke_MissingGroupId_Unfiled`
- [x] `InkSpoke_UnknownGroupId_Unfiled`
- [x] `InkSpoke_GroupsAbsent_AllUnfiled`
- [x] `InkSpoke_FavoritesVocabularyIgnored`
- [x] `InkSpoke_ActionTypeMissing_Rejected`

## T5 — Import planner (AC3, AC5)

- [x] T5 implementation and task verification (47 cases pass; 43 red against stub)
- [x] `Duplicate_OfLibrary_Skipped`
- [x] `Duplicate_WithinBatch_FirstWins`
- [x] `Duplicate_LineEndingsOnly_Skipped`
- [x] `NotDuplicate_CaseDiffers`
- [x] `NotDuplicate_TrailingSpaceDiffers`
- [x] `NotDuplicate_TabVersusSpaces`
- [x] `FullRow_SameTextDifferentFolder_BothPlanned`
- [x] `FullRow_SameTextDifferentLabel_BothPlanned`
- [x] `FullRow_SameTextDifferentTags_BothPlanned`
- [x] `FullRow_SameTextDifferentTagOrder_BothPlanned`
- [x] `FullRow_SameEverything_DifferentStats_SecondIsDuplicate`
- [x] `FullRow_CrVsLf_SameMetadata_Duplicate`
- [x] `FullRow_CrVsLf_DifferentLabel_BothPlanned`
- [x] `FullRow_MatchesLibraryRow_BuiltFromStoredFolderAndTags`
- [x] `FullRow_AutoLabelKeyUsesSuggestion`
- [x] `FullRow_FolderCaseDiffers_SameKey_Duplicate`
- [x] `TextOnly_SameTextDifferentFolder_SecondIsDuplicate`
- [x] `FullRow_SeparatorInText_VersusSeparatorInLabel_NotDuplicate`
- [x] `FullRow_SeparatorInsideTag_VersusTwoTags_NotDuplicate`
- [x] `FullRow_NulAndControlCharsInComponents_NotDuplicate`
- [x] `FullRow_EmptyTagsVersusOneTag_NotDuplicate`
- [x] `FullRow_TagBoundaryShift_NotDuplicate`
- [x] `FullRow_SeparatelyAllocatedEqualTags_Duplicate`
- [x] `DuplicateCandidate_WithUnseenFolder_CreatesNoFolder`
- [x] `RejectedCandidate_WithUnseenFolder_CreatesNoFolder`
- [x] `AllDuplicates_WithNovelFolderNames_NoNewFolders`
- [x] `NativeFolderNames_StillCreatedWhenNoCandidateUsesThem`
- [x] `Folder_MatchesExistingCaseInsensitive_UsesExistingId`
- [x] `Folder_NewTwiceDifferentCase_OneNewFolder_FirstSpelling`
- [x] `Folder_Blank_Unfiled`
- [x] `Folder_Whitespace_Unfiled`
- [x] `Folder_Trimmed`
- [x] `FolderNames_ProcessedFirst_SpellingWins`
- [x] `FolderNames_ExistingNotRecreated`
- [x] `Label_Given_KeptExactlyAsCustom`
- [x] `Label_Missing_Suggested_FromFirstTag`
- [x] `Label_Missing_Suggested_FromFirstLine`
- [x] `Tags_CommaSplit_CaseDeduped`
- [x] `Validation_Failure_CountedRejected`
- [x] `Plan_DoesNotMutateInputs`
- [x] `Counts_DuplicatesPlusPlannedPlusRejected_EqualCandidates`

## T6 — Store bulk import (AC4, AC5, AC6, AC3)

- [x] T6 implementation and task verification (Core 387; Presentation 335; Import filter 115 pass; export-half box belongs to T7)
- [x] `InsertImported_PersistsAllStats_Reload`
- [x] `Insert_StillFreshStats`
- [x] `RecordUsage_AtIntMax_Saturates`
- [x] `Read_UsageAboveIntMax_ClampedToIntMax`
- [x] `UsageImported_Paste_Sort_Reopen`
- [x] `UsageAtIntMax_Paste_Saturates_Reopen`
- [x] `UsageChain_Export_Reimport_Exact`
- [x] `Import_SubscriberThrows_CommitReturned_RowsPersisted_LaterSubscribersRan`
- [x] `Create_SubscriberThrows_ResultReturned`
- [x] `Import_OneTransaction_OneEvent_KindImported_IdsAreNew`
- [x] `Import_SnapshotsReplacedOnce_ContainNewRows`
- [x] `Import_PersistsStats_ReloadEqualsCache`
- [x] `Import_FoldersCreatedInSameTransaction`
- [x] `Import_AllDuplicates_NoWrite_NoEvent`
- [x] `Import_OnlyEmptyNativeFolder_WritesFolder_OneEvent`
- [x] `Import_EditorRules_SameAsCreate`
- [x] `Import_FullRowBatch_KeepsSameTextRowsWithDifferentMetadata`
- [x] `Import_InjectedFailure_RollsBack_NoRows_NoFolders_NoEvent_SnapshotsSame`
- [x] `Import_InjectedFailure_LogHasEventButNoMessage`
- [x] `CreateSnippet_InjectedFailure_LogHasNoMessage`
- [x] `Pipeline_EachFixture_CountsMatchIndependentTable_AndBalanced`
- [x] `Pipeline_SameFileTwice_SecondImportsZero_NoEvent`
- [x] `FakeSnippetStore_Import_MatchesRealStoreCounts`

## T7 — Native export (AC7)

- [x] T7 implementation and task verification (27 filtered cases pass; AOT remains final gate)
- [x] `Export_EnvelopeShape_MarkerVersionArrays`
- [x] `Export_IsCompact`
- [x] `Export_EmptyLibrary_ValidEmptyArrays`
- [x] `Export_UnfiledFolderNull_NeverUsedLastUsedNull`
- [x] `Export_DatesAreEpochMs`
- [x] `Export_NoBom`
- [x] `Export_LoneSurrogate_WrittenAsReplacementChar`
- [x] `RoundTrip_ExportThenImportIntoEmpty_ReproducesEverything`
- [x] `RoundTrip_FullKeyRepeat_CollapsesToFirst`
- [x] `RoundTrip_FolderIdsDiffer_ProjectionStillEqual`
- [x] `UsageChain_Export_Reimport_Exact`
- [x] `RoundTrip_ReimportIntoSameLibrary_ZeroNew_NoEvent`
- [x] `RoundTrip_ReimportIntoTarget_ZeroNew`
- [x] `RoundTrip_ParserDetectsOwnOutputAsNative`
- [x] `ExportImport_ExactlyAtCap_ExportsAndImports`
- [x] `ExportImport_OneByteOver_RefusedAtExport_NoFile`
- [x] `ExportImport_EscapingExpansion_RefusedAtExport`
- [x] `Export_Oversized_StopsAtBound` (also protected by allocation-free preflight after review)
- [x] `Export_SingleOversizedToken_PreflightRefusesWithoutLargeAllocations` (red 1,610,616,384 bytes; green 48 bytes)
- [x] `Export_PreflightCoversEveryEscapedStringField` (5 cases)
- [x] `Export_PreflightSumsStringPayloads`
- [x] `Export_TooManySnippets_RefusedWithoutSerializing`
- [x] `Export_AtEntryLimit_Exports`
- [x] `Export_TooManyEmptyFolders_RefusedBeforeSerialization`
- [x] `Export_CombinedLimitPlusOne_Refused`
- [x] `Export_EmptyFoldersAtLimit_ExportsAndImports`
- [x] `Export_CombinedEntryLimit_ExportsAndImports`
- [x] `ExportImport_EscapingNearCap_ImportsBack`

## T8 — Porting messages (AC2, AC8, AC9)

- [x] T8 implementation and task verification (18 cases pass; 17 red against message stubs)
- [x] `EveryRefusal_HasMessage_EndingNothingWasImported`
- [x] `RefusalMessages_PairwiseDistinct`
- [x] `UnsupportedVersion_NamesFormatFoundAndSupported`
- [x] `MissingEntries_NamesListPerFormat`
- [x] `Summary_FourLines_Pluralization`
- [x] `Summary_FormatNames`
- [x] `ExportComplete_Pluralization`
- [x] `ButtonNames_AreDistinctFromNewSnippet`

## T9 — View-model, dialog contract, page wiring (AC2, AC3, AC8, AC9 enabler)

- [x] T9 implementation and task verification (78 filtered cases pass; calibration executes in T10)
- [x] `Import_Cancelled_NoRead_NoStoreCall_NoDialog_LogsCancelled`
- [x] `Import_NotJsonPath_Refused_NoOpen`
- [x] `Import_OpenThrowsIOException_Unreadable`
- [x] `Import_OpenThrowsUnauthorized_Unreadable`
- [x] `Import_ReadThrowsOnFirstRead_Unreadable`
- [x] `Import_ReadThrowsAfterSomeBytes_Unreadable`
- [x] `Import_TooLarge_Refused`
- [x] `Import_NotUtf8_Refused`
- [x] `Import_InvalidJson_Refused`
- [x] `Import_UnknownFormat_Refused`
- [x] `Import_UnsupportedVersion_Refused_MessageNamesVersion`
- [x] `Import_MissingEntries_Refused`
- [x] `Import_TooManyEntries_Refused_MessageNamesCountAndLimit`
- [x] `Import_Success_ShowsInfoWithSummary_TitleComplete`
- [x] `Import_ZeroImported_TitleNothingImported`
- [x] `Import_StoreException_ShowsStoreFailed_NoInfo`
- [x] `Import_UnexpectedReadException_BeforeCommit_NothingImportedMessage`
- [x] `ReadThrowsInvalidData_Propagates` (Core T1 test)
- [x] `Import_ParseWorkerThrows_BeforeCommit_NothingImportedMessage`
- [x] `Import_StoreThrowsNonStoreException_InCommit_OutcomeUnknownMessage_NeverNothingImported`
- [x] `Import_SummaryDialogThrows_AfterCommit_NoErrorDialog_Logged`
- [x] `Import_SubscriberThrows_SummaryStillShown`
- [x] `Import_Busy_DuringSummaryDialog`
- [x] `Import_Busy_DuringRefusalDialog`
- [x] `Import_Busy_DuringUnexpectedErrorDialog`
- [x] `Import_Busy_DuringStoreFailureDialog`
- [x] `Export_Busy_DuringUnexpectedErrorDialog`
- [x] `Export_Busy_DuringSuccessDialog`
- [x] `Import_MatchingSnippetCreatedDuringParse_IsSkippedAsDuplicate`
- [x] `Import_MatchingSnippetDeletedDuringParse_IsImported`
- [x] `Import_FolderRenamedDuringParse_ResolvesToCurrentFolder`
- [x] `Import_FolderCreatedDuringParse_Reused`
- [x] `Import_WhileEditorDirty_EditorKeepsEdits_NewFolderInChoices`
- [x] `Page_FilesNull_CommandsDisabled`
- [x] `Page_PortingSharesStoreAndDialogs`
- [x] `Export_Cancelled_NoWrite`
- [x] `Export_NotJson_Refused_NoWrite`
- [x] `Export_Success_WritesNativeBytes_ShowsCountsAndFileName`
- [x] `Export_WriteThrows_ShowsWriteFailed`
- [x] `Export_TooLarge_Refused_NoWriteCall`
- [x] `Export_TooManySnippets_Refused_NoWriteCall`
- [x] `Export_TooManyEmptyFolders_Refused_NoWriteCall`
- [x] `Import_TooManyEmptyFolders_Refused_MessageNamesCombinedCount`
- [x] `Export_UnexpectedSerializeException_BeforeWrite_NoFileWrittenMessage`
- [x] `Export_NonIoWriteException_InWrite_OutcomeUnknownMessage`
- [x] `Export_SuccessDialogThrows_AfterWrite_NoErrorDialog_Logged`
- [x] `Export_SuggestedBaseName_HasDateNoExtension`
- [x] `Logs_SuccessAndFailure_NoPathNoFileNameNoText`
- [x] `Import_StoreCallAndChanged_OnOwnerThread_AfterSuspendedReadAndHeldParse`
- [x] `Import_ParseCompletesOnWorker_StillCommitsOnOwner`
- [x] `Export_SerializeHeld_WriteAndDialogOnOwner`
- [x] `PortingWorkers_Create_RunsOffCallerThread`
- [x] `Integration_ImportTermFlow_ListFindsImportedTextFolderAndTag`
- [x] `Integration_ImportTermFlow_PopupSearchAndSortsReflectImport`
- [x] `Integration_ImportNative_FoldersPageShowsNewFolderWithCount`
- [x] `Integration_InjectedFailure_NoInfoDialog_LibraryUnchanged`
- [x] `Integration_ThrowingSubscriber_RowsAcknowledged_OtherViewsRefreshed`
- [x] `Budget_AtLimit_Dense_CommitUnderThreshold`
- [x] `Budget_AtLimit_FolderDense_CommitUnderThreshold`
- [x] `Budget_TypicalAtCap_CommitUnderThreshold`
- [x] `Budget_AtLimit_EmptyNativeFolders_CommitUnderThreshold` (local Release 339.15 ms)
- [x] `Budget_Calibrate` (local Windows Release opt-in run, 1 passed; measurements in plan §10)

## T10 — App: pickers, file I/O, dialog, buttons, wiring (AC9, AC10)

- [ ] T10 implementation and task verification (App code, build and local Release budgets done; binding CI blocked by no remote branch/no push; manual runbook PENDING-USER)
- [x] `MaxImportEntries_IsCalibratedValue` (50,000; final Core run verifies)

## T11 — Requirement and master-plan update (scope change)

- [x] T11 implementation and task verification (rebased onto local main; all six document greps verified)

## T12 — Wiring audit (L)

- [x] T12 static implementation and task verification (actual source/privacy/fixture/docs audit in docs/research/010-wiring-audit.md; runtime runbook evidence PENDING-USER)

## Manual and final gates

- [ ] Runbook §7.3 step 1 — PENDING-USER
- [ ] Runbook §7.3 step 2 — PENDING-USER
- [ ] Runbook §7.3 step 3 — PENDING-USER
- [ ] Runbook §7.3 step 4 — PENDING-USER
- [ ] Runbook §7.3 step 5 — PENDING-USER
- [ ] Runbook §7.3 step 6 (a–f) — PENDING-USER
- [ ] Runbook §7.3 step 7 — PENDING-USER
- [ ] Runbook §7.3 step 8 — PENDING-USER
- [ ] Runbook §7.3 step 9a — PENDING-USER
- [ ] Runbook §7.3 step 9b — PENDING-USER
- [ ] Runbook §7.3 step 10 — PENDING-USER
- [ ] Runbook §7.3 step 11 — PENDING-USER
- [ ] Runbook §7.3 step 12 — PENDING-USER
- [ ] Runbook §7.3 step 13 — PENDING-USER
- [x] Runbook §7.3 step 14 (automated evidence)
- [x] Windows solution build, 0 warnings
- [x] Core tests (sequential): 409 passed
- [x] Presentation tests (sequential): 413 passed
- [x] scripts/publish-aot.ps1, no IL warnings (exit 0; no warnings at all)
- [ ] T10 binding CI budget calibration and Release budget gate (local Windows calibration + 3 Release budgets passed; CI blocked: remote feature branch absent, user prohibits push)
- [x] T11 documentation verification
- [x] T12 wiring, privacy and fixture source audits
- [ ] T12 runtime audit (porting_ready / real-sample import_done) — PENDING-USER, runbook step 1
