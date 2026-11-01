"""Shared strict allowlists for the content-free UI helper failure protocol."""
import re
from types import MappingProxyType


HELPER_FAILURE_ALLOWLISTS = MappingProxyType({
    'stages': frozenset((
        'bootstrap', 'ready', 'picker_import_cancel', 'picker_import', 'picker_export_cancel', 'picker_export',
        'settings_theme', 'settings_size', 'folder_create', 'folder_rename', 'folder_delete', 'new', 'edit_saved',
        'save', 'edit_dirty', 'dirty_leave', 'cancel', 'dirty_save_leave', 'dirty_save', 'discard_edit',
        'dirty_discard_leave', 'dirty_discard', 'finish')),
    'reasons': frozenset((
        'sta', 'root', 'input', 'window', 'identity', 'artifact', 'desktop', 'desktop_close', 'foreground',
        'uia_owner', 'schema', 'focus', 'value', 'timeout', 'receipt', 'control_timeout', 'actions', 'clipboard',
        'selection', 'picker_schema_missing', 'picker_schema_multiple', 'picker_schema_type', 'picker_identity',
        'picker_replaced', 'picker_changed', 'picker_schema', 'picker_control_owner', 'picker_control_class',
        'picker_phase', 'picker_timeout', 'picker_uia_owner', 'picker_alias', 'picker_existing_export',
        'picker_input', 'picker_combo_chain', 'picker_value', 'picker_close_timeout', 'picker_actions', 'operation')),
    'operationClasses': frozenset((
        'element_not_available', 'element_not_enabled', 'invalid_operation', 'com', 'win32', 'timeout',
        'argument', 'null_reference', 'unauthorized', 'io', 'other')),
    'pickerSteps': frozenset((
        'not_applicable', 'start', 'invoke', 'wait_dialog', 'guard', 'uia_owner', 'path_ready', 'control_ready',
        'commit_ready', 'value_set', 'cancel_invoke', 'picker_invoke', 'close_wait', 'actions_validated', 'other')),
})

HELPER_FAILURE_PATTERN = re.compile(
    rb'ui_helper_failed stage=([a-z][a-z0-9_]*) reason=([a-z][a-z0-9_]*) '
    rb'operationClass=([a-z][a-z0-9_]{0,63}) pickerStep=([a-z][a-z0-9_]{0,63})'
    rb'(?: hresult=0x([0-9A-F]{8}))?\r?\n\Z')
