"""Content-free scanner for synthetic canaries in owned candidate diagnostics.

This module performs no IO. The harness owns provenance, safe file reads and its
required flow receipts. CLEAN is a privacy scan result, never native acceptance.
Never pass operator clipboard contents as canaries or evidence.
"""
MAX_EVIDENCE_BYTES = 16 * 1024 * 1024


def signatures(canaries):
    values = tuple(canaries)
    if not values or len(values) > 64 or any(
            not isinstance(value, str) or not 8 <= len(value) <= 4096
            or '\x00' in value for value in values):
        raise ValueError('privacy_canaries_invalid')
    try:
        return tuple(dict.fromkeys(value.encode(encoding)
                                  for value in values
                                  for encoding in ('utf-8', 'utf-16-le', 'utf-16-be')))
    except UnicodeError:
        raise ValueError('privacy_canaries_invalid') from None


def scan_evidence(records, canaries):
    """Scan bounded byte records without returning data, names, offsets or paths."""
    patterns = signatures(canaries)
    values = tuple(records)
    if (not values or len(values) > 128 or any(not isinstance(value, bytes) for value in values)
            or sum(map(len, values)) > MAX_EVIDENCE_BYTES):
        raise ValueError('privacy_evidence_invalid')
    clean = not any(pattern in value for value in values for pattern in patterns)
    return {'privacy': 'CLEAN' if clean else 'LEAK_DETECTED',
            'records': len(values), 'nativeAcceptance': 'NOT_ASSERTED'}


def verify_negative_controls(canaries):
    """Exercise the same scanner with every supplied canary in each encoding."""
    values = tuple(canaries)
    signatures(values)  # Validate before running any controls.
    for value in values:
        for encoding in ('utf-8', 'utf-16-le', 'utf-16-be'):
            planted = b'fixed prefix\n' + value.encode(encoding) + b'\nfixed suffix'
            if scan_evidence((planted,), values)['privacy'] != 'LEAK_DETECTED':
                raise ValueError('privacy_negative_control_failed')
    return {'negativeControls': 'DETECTED', 'count': len(values) * 3}
