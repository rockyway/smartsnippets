# Shared format validation for release.yml and the acceptance harness.
function Test-ReleaseTag([string]$Tag) {
    if ($Tag -cnotmatch '^v(\d+)\.(\d+)\.(\d+)(-[0-9A-Za-z.-]+)?$') { return $false }
    return $true
}
