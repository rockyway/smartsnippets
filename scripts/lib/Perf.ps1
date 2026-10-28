# Pure helpers: no UI, profile or clipboard side effects when dot-sourced.
function Get-PerfEvidenceClassification([string]$Id, [int]$LibraryCount, [bool]$Ok) {
    $diagnostic=$LibraryCount -ne 10000 -and $Id -match '^P[012](?:\.|$)'
    [pscustomobject]@{
        Id=$(if ($diagnostic) { 'Diagnostic.'+$Id } else { $Id })
        Status=$(if (-not $Ok) { 'FAIL' } elseif ($diagnostic) { 'INFO' } else { 'PASS' })
        Prefix=$(if ($diagnostic) { 'NON-ACCEPTANCE (not 10k): ' } else { '' })
    }
}
function Get-TimingStatistics([double[]]$Samples) {
    if (-not $Samples.Count) { throw 'No timing samples' }
    $sorted = @($Samples | Sort-Object)
    $n = $sorted.Count
    $median = if ($n % 2) { $sorted[[int][Math]::Floor($n / 2)] } else { ($sorted[$n / 2 - 1] + $sorted[$n / 2]) / 2 }
    [pscustomobject]@{ Count = $n; Median = $median; P95 = $sorted[[int][Math]::Ceiling(.95 * $n) - 1]; Max = $sorted[-1] }
}
function ConvertTo-SqlText([string]$Text) { "'" + $Text.Replace("'", "''") + "'" }
function New-PerfLibrarySql([int]$Count = 10000, [int]$Seed = 42) {
    if ($Count -lt 10) { throw 'Performance library needs at least 10 snippets' }
    $rng = New-Object Random $Seed
    # 20 * 10 * 10 = 2,000 fixed English-like words, none containing planted terms.
    $words = @(foreach ($a in @('amber','cedar','river','stone','cloud','maple','silver','golden','meadow','forest','ocean','winter','summer','gentle','quiet','bright','swift','green','frost','sunny')) {
        foreach ($b in @('brook','field','wood','hill','vale','leaf','song','path','gate','well')) {
            foreach ($c in @('en','ing','er','ly','ful','ness','ed','est','ish','let')) { "$a$b$c" }
        }
    })
    $epoch = [long]1793145600000 # fixed 2026-10-28 UTC; fixture never depends on wall clock
    'PRAGMA foreign_keys=ON; BEGIN; DELETE FROM snippets; DELETE FROM folders;'
    for ($i = 1; $i -le 50; $i++) {
        $name = 'Perf folder {0:00}' -f $i
        "INSERT INTO folders VALUES($i,'$name','$($name.ToUpperInvariant())',$epoch,$epoch);"
    }
    $foldered = [int][Math]::Floor($Count * .9)
    $alphaCount = [int][Math]::Floor($Count * .1)
    $bothCount = [int][Math]::Floor($Count * .01)
    for ($i = 0; $i -lt $Count; $i++) {
        $kind = $i % 10
        $length = if ($kind -lt 6) { $rng.Next(20,161) } elseif ($kind -lt 9) { $rng.Next(200,951) } else { $rng.Next(1000,3901) }
        $lines = if ($kind -lt 6) { 1 } elseif ($kind -lt 9) { $rng.Next(2,9) } else { $rng.Next(10,41) }
        $builder = New-Object Text.StringBuilder
        while ($builder.Length -lt $length) { [void]$builder.Append($words[$rng.Next(2000)] + ' ') }
        $text = $builder.ToString().Substring(0,$length)
        # Replace spaces with line breaks without changing the length budget.
        $parts = $text.Split(' ')
        for ($j = 1; $j -lt $lines; $j++) { $at = [int][Math]::Floor($j * ($parts.Count - 1) / $lines); $parts[$at] += "`n" }
        $text = ($parts -join ' ').TrimEnd()
        if ($i % 20 -eq 0) { $text += ' café 世界 👋' }
        $custom = if ($i % 10 -lt 7) { 1 } else { 0 }
        $label = (@(for ($j = 0; $j -lt $rng.Next(2,4); $j++) { $words[$rng.Next(2000)] }) -join ' ')
        $tags = @(for ($j = 0; $j -lt $rng.Next(0,4); $j++) { 'tag{0:000}' -f $rng.Next(100) })
        # Tagless auto labels follow LabelSuggester's first-line rule.
        if (-not $custom) { $tags = @() }
        $alpha = $i -lt $alphaCount
        $bravo = $i -lt $bothCount -or ($i -ge $alphaCount -and $i -lt (2 * $alphaCount - $bothCount))
        foreach ($term in @('alpha','bravo')) {
            if (($term -eq 'alpha' -and $alpha) -or ($term -eq 'bravo' -and $bravo)) {
                if ($custom -and $i % 3 -eq 1) { $label += " $term" }
                elseif ($custom -and $i % 3 -eq 2) { $tags = @($term) + @($tags | Select-Object -First 2) }
                else { $text = "$term $text" }
            }
        }
        if (-not $custom) {
            $first = ($text -split "`n")[0].Trim()
            $elements = [Globalization.StringInfo]::ParseCombiningCharacters($first)
            $label = if ($elements.Count -gt 60) { $first.Substring(0,$elements[60]) } else { $first }
        }
        $tagText = (@($tags | Sort-Object -Unique) -join ',')
        $folder = if ($i -lt $foldered) { [string](1 + $i % 50) } else { 'NULL' }
        $used = if ($i % 10 -lt 9) { $rng.Next(0,6) } else { $rng.Next(6,501) }
        $created = $epoch - [long]$rng.Next(0,730) * 86400000
        $updated = $created + [long]($rng.NextDouble() * ($epoch - $created))
        $last = if ($used) { [string]($epoch - [long]$rng.Next(0,365) * 86400000) } else { 'NULL' }
        $id = $i + 1
        "INSERT INTO snippets VALUES($id,$(ConvertTo-SqlText $text),$(ConvertTo-SqlText $label),$custom,$folder,$(ConvertTo-SqlText $tagText),$created,$updated,$last,$used);"
    }
    'COMMIT;'
}
function Get-PerfSearchCountsSql {
    # Same case-insensitive substring AND semantics as the popup, across all searchable fields.
    @'
WITH hay AS (SELECT lower(s.text || ' ' || s.label || ' ' || s.tags || ' ' || coalesce(f.name,'')) AS h FROM snippets s LEFT JOIN folders f ON f.id=s.folder_id)
SELECT count(*),sum(instr(h,'alpha')>0),sum(instr(h,'bravo')>0),sum(instr(h,'alpha')>0 AND instr(h,'bravo')>0),sum(instr(h,'zzqxv')>0) FROM hay;
'@
}
function ConvertFrom-PopupSearchLog([string[]]$Lines, [string]$Layout, [int]$ExpectedCount) {
    $samples = @($Lines | Where-Object { $_ -match '\bpopup_search_ms ' })
    if ($samples.Count -ne 1) { throw "Expected exactly one search render sample, got $($samples.Count)" }
    $match = [regex]::Match($samples[0], '\bpopup_search_ms (\d+) layout=(flat|folder) results=(\d+)\s*$')
    if (-not $match.Success -or $match.Groups[2].Value -ine $Layout -or [int]$match.Groups[3].Value -ne $ExpectedCount) { throw 'Search sample has incorrect syntax, layout or result count' }
    [int]$match.Groups[1].Value
}
function ConvertFrom-PopupReadyLog([string[]]$Lines) {
    $samples = @($Lines | Where-Object { $_ -match '\bpopup_ready_ms ' })
    if ($samples.Count -ne 1) { throw "Expected exactly one ready sample, got $($samples.Count)" }
    $match = [regex]::Match($samples[0], '\bpopup_ready_ms (\d+)\s*$')
    if (-not $match.Success) { throw 'Malformed popup ready sample' }
    [int]$match.Groups[1].Value
}
function Test-LogPrivacy([string[]]$Lines, [string[]]$Secrets) {
    $text = $Lines -join "`n"
    foreach ($secret in $Secrets) { if ($secret -and $text.IndexOf($secret, [StringComparison]::OrdinalIgnoreCase) -ge 0) { return $false } }
    return $true
}
