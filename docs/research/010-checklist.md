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

- [ ] T5 implementation and task verification
- [ ] `Duplicate_OfLibrary_Skipped`
- [ ] `Duplicate_WithinBatch_FirstWins`
- [ ] `Duplicate_LineEndingsOnly_Skipped`
- [ ] `NotDuplicate_CaseDiffers`
- [ ] `NotDuplicate_TrailingSpaceDiffers`
- [ ] `NotDuplicate_TabVersusSpaces`
- [ ] `FullRow_SameTextDifferentFolder_BothPlanned`
- [ ] `FullRow_SameTextDifferentLabel_BothPlanned`
- [ ] `FullRow_SameTextDifferentTags_BothPlanned`
- [ ] `FullRow_SameTextDifferentTagOrder_BothPlanned`
- [ ] `FullRow_SameEverything_DifferentStats_SecondIsDuplicate`
- [ ] `FullRow_CrVsLf_SameMetadata_Duplicate`
- [ ] `FullRow_CrVsLf_DifferentLabel_BothPlanned`
- [ ] `FullRow_MatchesLibraryRow_BuiltFromStoredFolderAndTags`
- [ ] `FullRow_AutoLabelKeyUsesSuggestion`
- [ ] `FullRow_FolderCaseDiffers_SameKey_Duplicate`
- [ ] `TextOnly_SameTextDifferentFolder_SecondIsDuplicate`
- [ ] `FullRow_SeparatorInText_VersusSeparatorInLabel_NotDuplicate`
- [ ] `FullRow_SeparatorInsideTag_VersusTwoTags_NotDuplicate`
- [ ] `FullRow_NulAndControlCharsInComponents_NotDuplicate`
- [ ] `FullRow_EmptyTagsVersusOneTag_NotDuplicate`
- [ ] `FullRow_TagBoundaryShift_NotDuplicate`
- [ ] `FullRow_SeparatelyAllocatedEqualTags_Duplicate`
- [ ] `DuplicateCandidate_WithUnseenFolder_CreatesNoFolder`
- [ ] `RejectedCandidate_WithUnseenFolder_CreatesNoFolder`
- [ ] `AllDuplicates_WithNovelFolderNames_NoNewFolders`
- [ ] `NativeFolderNames_StillCreatedWhenNoCandidateUsesThem`
- [ ] `Folder_MatchesExistingCaseInsensitive_UsesExistingId`
- [ ] `Folder_NewTwiceDifferentCase_OneNewFolder_FirstSpelling`
- [ ] `Folder_Blank_Unfiled`
- [ ] `Folder_Whitespace_Unfiled`
- [ ] `Folder_Trimmed`
- [ ] `FolderNames_ProcessedFirst_SpellingWins`
- [ ] `FolderNames_ExistingNotRecreated`
- [ ] `Label_Given_KeptExactlyAsCustom`
- [ ] `Label_Missing_Suggested_FromFirstTag`
- [ ] `Label_Missing_Suggested_FromFirstLine`
- [ ] `Tags_CommaSplit_CaseDeduped`
- [ ] `Validation_Failure_CountedRejected`
- [ ] `Plan_DoesNotMutateInputs`
- [ ] `Counts_DuplicatesPlusPlannedPlusRejected_EqualCandidates`

## T6 — Store bulk import (AC4, AC5, AC6, AC3)

- [ ] T6 implementation and task verification
- [ ] `InsertImported_PersistsAllStats_Reload`
- [ ] `Insert_StillFreshStats`
- [ ] `RecordUsage_AtIntMax_Saturates`
- [ ] `Read_UsageAboveIntMax_ClampedToIntMax`
- [ ] `UsageImported_Paste_Sort_Reopen`
- [ ] `UsageAtIntMax_Paste_Saturates_Reopen`
- [ ] `UsageChain_Export_Reimport_Exact`
- [ ] `Import_SubscriberThrows_CommitReturned_RowsPersisted_LaterSubscribersRan`
- [ ] `Create_SubscriberThrows_ResultReturned`
- [ ] `Import_OneTransaction_OneEvent_KindImported_IdsAreNew`
- [ ] `Import_SnapshotsReplacedOnce_ContainNewRows`
- [ ] `Import_PersistsStats_ReloadEqualsCache`
- [ ] `Import_FoldersCreatedInSameTransaction`
- [ ] `Import_AllDuplicates_NoWrite_NoEvent`
- [ ] `Import_OnlyEmptyNativeFolder_WritesFolder_OneEvent`
- [ ] `Import_EditorRules_SameAsCreate`
- [ ] `Import_FullRowBatch_KeepsSameTextRowsWithDifferentMetadata`
- [ ] `Import_InjectedFailure_RollsBack_NoRows_NoFolders_NoEvent_SnapshotsSame`
- [ ] `Import_InjectedFailure_LogHasEventButNoMessage`
- [ ] `CreateSnippet_InjectedFailure_LogHasNoMessage`
- [ ] `Pipeline_EachFixture_CountsMatchIndependentTable_AndBalanced`
- [ ] `Pipeline_SameFileTwice_SecondImportsZero_NoEvent`
- [ ] `FakeSnippetStore_Import_MatchesRealStoreCounts`

## T7 — Native export (AC7)

- [ ] T7 implementation and task verification
- [ ] `Export_EnvelopeShape_MarkerVersionArrays`
- [ ] `Export_IsCompact`
- [ ] `Export_EmptyLibrary_ValidEmptyArrays`
- [ ] `Export_UnfiledFolderNull_NeverUsedLastUsedNull`
- [ ] `Export_DatesAreEpochMs`
- [ ] `Export_NoBom`
- [ ] `Export_LoneSurrogate_WrittenAsReplacementChar`
- [ ] `RoundTrip_ExportThenImportIntoEmpty_ReproducesEverything`
- [ ] `RoundTrip_FullKeyRepeat_CollapsesToFirst`
- [ ] `RoundTrip_FolderIdsDiffer_ProjectionStillEqual`
- [ ] `UsageChain_Export_Reimport_Exact`
- [ ] `RoundTrip_ReimportIntoSameLibrary_ZeroNew_NoEvent`
- [ ] `RoundTrip_ReimportIntoTarget_ZeroNew`
- [ ] `RoundTrip_ParserDetectsOwnOutputAsNative`
- [ ] `ExportImport_ExactlyAtCap_ExportsAndImports`
- [ ] `ExportImport_OneByteOver_RefusedAtExport_NoFile`
- [ ] `ExportImport_EscapingExpansion_RefusedAtExport`
- [ ] `Export_Oversized_StopsAtBound`
- [ ] `Export_TooManySnippets_RefusedWithoutSerializing`
- [ ] `Export_AtEntryLimit_Exports`
- [ ] `ExportImport_EscapingNearCap_ImportsBack`

## T8 — Porting messages (AC2, AC8, AC9)

- [ ] T8 implementation and task verification
- [ ] `EveryRefusal_HasMessage_EndingNothingWasImported`
- [ ] `RefusalMessages_PairwiseDistinct`
- [ ] `UnsupportedVersion_NamesFormatFoundAndSupported`
- [ ] `MissingEntries_NamesListPerFormat`
- [ ] `Summary_FourLines_Pluralization`
- [ ] `Summary_FormatNames`
- [ ] `ExportComplete_Pluralization`
- [ ] `ButtonNames_AreDistinctFromNewSnippet`

## T9 — View-model, dialog contract, page wiring (AC2, AC3, AC8, AC9 enabler)

- [ ] T9 implementation and task verification
- [ ] `Import_Cancelled_NoRead_NoStoreCall_NoDialog_LogsCancelled`
- [ ] `Import_NotJsonPath_Refused_NoOpen`
- [ ] `Import_OpenThrowsIOException_Unreadable`
- [ ] `Import_OpenThrowsUnauthorized_Unreadable`
- [ ] `Import_ReadThrowsOnFirstRead_Unreadable`
- [ ] `Import_ReadThrowsAfterSomeBytes_Unreadable`
- [ ] `Import_TooLarge_Refused`
- [ ] `Import_NotUtf8_Refused`
- [ ] `Import_InvalidJson_Refused`
- [ ] `Import_UnknownFormat_Refused`
- [ ] `Import_UnsupportedVersion_Refused_MessageNamesVersion`
- [ ] `Import_MissingEntries_Refused`
- [ ] `Import_TooManyEntries_Refused_MessageNamesCountAndLimit`
- [ ] `Import_Success_ShowsInfoWithSummary_TitleComplete`
- [ ] `Import_ZeroImported_TitleNothingImported`
- [ ] `Import_StoreException_ShowsStoreFailed_NoInfo`
- [ ] `Import_UnexpectedReadException_BeforeCommit_NothingImportedMessage`
- [ ] `ReadThrowsInvalidData_Propagates`
- [ ] `Import_ParseWorkerThrows_BeforeCommit_NothingImportedMessage`
- [ ] `Import_StoreThrowsNonStoreException_InCommit_OutcomeUnknownMessage_NeverNothingImported`
- [ ] `Import_SummaryDialogThrows_AfterCommit_NoErrorDialog_Logged`
- [ ] `Import_SubscriberThrows_SummaryStillShown`
- [ ] `Import_Busy_DuringSummaryDialog`
- [ ] `Import_Busy_DuringRefusalDialog`
- [ ] `Import_Busy_DuringUnexpectedErrorDialog`
- [ ] `Import_Busy_DuringStoreFailureDialog`
- [ ] `Export_Busy_DuringUnexpectedErrorDialog`
- [ ] `Export_Busy_DuringSuccessDialog`
- [ ] `Import_MatchingSnippetCreatedDuringParse_IsSkippedAsDuplicate`
- [ ] `Import_MatchingSnippetDeletedDuringParse_IsImported`
- [ ] `Import_FolderRenamedDuringParse_ResolvesToCurrentFolder`
- [ ] `Import_FolderCreatedDuringParse_Reused`
- [ ] `Import_WhileEditorDirty_EditorKeepsEdits_NewFolderInChoices`
- [ ] `Page_FilesNull_CommandsDisabled`
- [ ] `Page_PortingSharesStoreAndDialogs`
- [ ] `Export_Cancelled_NoWrite`
- [ ] `Export_NotJson_Refused_NoWrite`
- [ ] `Export_Success_WritesNativeBytes_ShowsCountsAndFileName`
- [ ] `Export_WriteThrows_ShowsWriteFailed`
- [ ] `Export_TooLarge_Refused_NoWriteCall`
- [ ] `Export_TooManySnippets_Refused_NoWriteCall`
- [ ] `Export_UnexpectedSerializeException_BeforeWrite_NoFileWrittenMessage`
- [ ] `Export_NonIoWriteException_InWrite_OutcomeUnknownMessage`
- [ ] `Export_SuccessDialogThrows_AfterWrite_NoErrorDialog_Logged`
- [ ] `Export_SuggestedBaseName_HasDateNoExtension`
- [ ] `Logs_SuccessAndFailure_NoPathNoFileNameNoText`
- [ ] `Import_StoreCallAndChanged_OnOwnerThread_AfterSuspendedReadAndHeldParse`
- [ ] `Import_ParseCompletesOnWorker_StillCommitsOnOwner`
- [ ] `Export_SerializeHeld_WriteAndDialogOnOwner`
- [ ] `PortingWorkers_Create_RunsOffCallerThread`
- [ ] `Integration_ImportTermFlow_ListFindsImportedTextFolderAndTag`
- [ ] `Integration_ImportTermFlow_PopupSearchAndSortsReflectImport`
- [ ] `Integration_ImportNative_FoldersPageShowsNewFolderWithCount`
- [ ] `Integration_InjectedFailure_NoInfoDialog_LibraryUnchanged`
- [ ] `Integration_ThrowingSubscriber_RowsAcknowledged_OtherViewsRefreshed`
- [ ] `Budget_AtLimit_Dense_CommitUnderThreshold`
- [ ] `Budget_AtLimit_FolderDense_CommitUnderThreshold`
- [ ] `Budget_TypicalAtCap_CommitUnderThreshold`
- [ ] `Budget_Calibrate`

## T10 — App: pickers, file I/O, dialog, buttons, wiring (AC9, AC10)

- [ ] T10 implementation and task verification
- [ ] `MaxImportEntries_IsCalibratedValue`

## T11 — Requirement and master-plan update (scope change)

- [ ] T11 implementation and task verification

## T12 — Wiring audit (L)

- [ ] T12 implementation and task verification

## Manual and final gates

- [ ] Runbook §7.3 step 1
- [ ] Runbook §7.3 step 2
- [ ] Runbook §7.3 step 3
- [ ] Runbook §7.3 step 4
- [ ] Runbook §7.3 step 5
- [ ] Runbook §7.3 step 6 (a–f)
- [ ] Runbook §7.3 step 7
- [ ] Runbook §7.3 step 8
- [ ] Runbook §7.3 step 9a
- [ ] Runbook §7.3 step 9b
- [ ] Runbook §7.3 step 10
- [ ] Runbook §7.3 step 11
- [ ] Runbook §7.3 step 12
- [ ] Runbook §7.3 step 13
- [ ] Runbook §7.3 step 14 (automated evidence)
- [ ] Windows solution build, 0 warnings
- [ ] Core tests (sequential)
- [ ] Presentation tests (sequential)
- [ ] scripts/publish-aot.ps1, no IL warnings
- [ ] T10 budget calibration and Release budget gate
- [ ] T11 documentation verification
- [ ] T12 wiring, privacy, fixture and runtime audits
