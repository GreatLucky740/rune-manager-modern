param(
    [Parameter(Mandatory=$true)][string]$Json,
    [Parameter(Mandatory=$true)][string]$Data,
    [Parameter(Mandatory=$true)][string]$Out
)
$ErrorActionPreference = 'Stop'
$db = Get-Content -LiteralPath $Data -Raw -Encoding UTF8 | ConvertFrom-Json
$game = Get-Content -LiteralPath $Json -Raw -Encoding UTF8 | ConvertFrom-Json

function PropValue($obj, [string]$name, $default = 0) {
    if ($null -eq $obj) { return $default }
    $p = $obj.PSObject.Properties[$name]
    if ($null -eq $p) { return $default }
    return $p.Value
}
function StatName([int]$code) { return [string](PropValue $db.stat_names ([string]$code) ([string]$code)) }
function UnitName([int64]$id) { return [string](PropValue $db.unit_names ([string]$id) ([string]$id)) }
function FlatMatch([string]$primary, [string]$preferred) {
    if ($primary -eq 'HP flat') { return $preferred -eq 'HP' }
    if ($primary -eq 'ATK flat') { return $preferred -eq 'Attack' }
    if ($primary -eq 'DEF flat') { return $preferred -eq 'Defense' }
    return $false
}
function Evaluate($artifact, $profile, $subs) {
    $isElement = [int]$artifact.type -eq 1
    $weights = if ($isElement) { $profile.weights_element } else { $profile.weights_type }
    $preferred = if ($isElement) { [string]$profile.preferred_flat_element } else { [string]$profile.preferred_flat_type }
    $total = 0.0; $contrib = @(); $present = @{}
    foreach ($s in $subs) {
        $avg = [double](PropValue (PropValue $db.rolls $s.name $null) 'avg' 1)
        if ($avg -eq 0) { $avg = 1 }
        $w = [double](PropValue $weights $s.name 0)
        $c = ([double]$s.value / $avg) * $w
        $total += $c; $present[$s.name] = $true
        $contrib += [pscustomobject]@{ name=$s.name; value=$s.value; c=$c }
    }
    $pri = $artifact.pri_effect
    if (FlatMatch (StatName ([int]$pri[0])) $preferred) { $total += [double]$db.config.main_bonus }
    $source = $contrib | Sort-Object c | Select-Object -First 1
    if ($null -eq $source) { $source = [pscustomobject]@{name='-';c=0} }
    $best = $null
    foreach ($wp in $weights.PSObject.Properties) {
        $name = [string]$wp.Name
        if ($present.ContainsKey($name)) { continue }
        $roll = PropValue $db.rolls $name $null
        if ($null -eq $roll) { continue }
        $gain = (([double]$roll.max / [double]$roll.avg) * [double]$wp.Value) - [double]$source.c
        if ($null -eq $best -or $gain -gt $best.gain) { $best = [pscustomobject]@{name=$name;max=$roll.max;gain=$gain} }
    }
    $potential = $total
    $reco = 'Deja optimal'
    if ($null -ne $best -and $best.gain -gt 0.05) { $potential += $best.gain; $reco = "$($source.name) -> $($best.name) $($best.max)" }
    return [pscustomobject]@{ potential=($potential/[double]$db.config.divisor); reco=$reco }
}

$owners = @{}
foreach ($u in @($game.unit_list)) { $owners[[string]$u.unit_id] = UnitName ([int64]$u.unit_master_id) }
$profilesBy = @{}
foreach ($p in @($db.profiles)) {
    foreach ($key in @("E$([int]$p.element_id)", "S$([int]$p.style_id)")) {
        if (-not $profilesBy.ContainsKey($key)) { $profilesBy[$key] = New-Object System.Collections.ArrayList }
        [void]$profilesBy[$key].Add($p)
    }
}
$all = New-Object System.Collections.ArrayList
foreach ($a in @($game.artifacts)) { [void]$all.Add($a) }
foreach ($u in @($game.unit_list)) { foreach ($a in @($u.artifacts)) { [void]$all.Add($a) } }
$seen = @{}; $rows = @()
$elementNames = @{'1'='Eau';'2'='Feu';'3'='Vent';'4'='Lumiere';'5'='Tenebres';'98'='Intangible'}
$styleNames = @{'1'='Attaque';'2'='Defense';'3'='PV';'4'='Support'}
foreach ($a in $all) {
    $rid = [string]$a.rid; if ($seen.ContainsKey($rid)) { continue }; $seen[$rid]=$true
    $subs = @()
    foreach ($s in @($a.sec_effects)) { $subs += [pscustomobject]@{name=(StatName ([int]$s[0]));value=[double]$s[1];converted=([int]$s[4] -ne 0)} }
    $profileKey = if ([int]$a.type -eq 1) { "E$([int]$a.attribute)" } else { "S$([int]$a.unit_style)" }
    $candidates = @($profilesBy[$profileKey])
    $bestEval = $null; $bestProfile = $null
    foreach ($p in $candidates) { $e=Evaluate $a $p $subs; if ($null -eq $bestEval -or $e.potential -gt $bestEval.potential) {$bestEval=$e;$bestProfile=$p} }
    if ($null -eq $bestEval) { $bestEval=[pscustomobject]@{potential=0;reco='Aucune donnee SWLens'}; $bestProfile=[pscustomobject]@{preset='';mode=''} }
    $action = if ($bestEval.potential -ge [double]$db.config.keep) {'Keep'} elseif ($bestEval.potential -ge [double]$db.config.review) {'Review'} else {'Sell'}
    $restriction = if ([int]$a.type -eq 1) {$elementNames[[string]$a.attribute]} else {$styleNames[[string]$a.unit_style]}
    $category = if ([int]$a.type -eq 1) {'Element'} else {'Type'}
    $rarity = if ([int]$a.natural_rank -eq 5) {'Legendaire'} else {'Heroique'}
    $label = "$rarity $category $restriction +$($a.level)"
    $primary = "$(StatName ([int]$a.pri_effect[0])) +$($a.pri_effect[1])"
    $st = @('','','','')
    for($i=0;$i -lt [Math]::Min(4,$subs.Count);$i++){ $mark=if($subs[$i].converted){'[C] '}else{''};$st[$i]="$mark$($subs[$i].name) +$($subs[$i].value)" }
    $owner = PropValue $owners ([string]$a.occupied_id) ''
    $locked = if ([int]$a.locked -ne 0) {'Oui'} else {'Non'}
    $rows += ,@($label,$rid,$category,$restriction,$rarity,[int]$a.level,$primary,$st[0],$st[1],$st[2],$st[3],$action,[Math]::Round($bestEval.potential,3),$bestEval.reco,$bestProfile.preset,$bestProfile.mode,$owner,$locked)
}
$rows = $rows | Sort-Object { [double]$_[12] } -Descending
$lines = foreach($row in $rows){ ($row | ForEach-Object { ([string]$_).Replace("`t",' ').Replace("`r",' ').Replace("`n",' ') }) -join "`t" }
[IO.File]::WriteAllLines($Out,$lines,(New-Object Text.UTF8Encoding($false)))
Write-Output $rows.Count
