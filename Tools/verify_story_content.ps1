# Story content linter: checks every StoryQuestDef XML under the given Defs folders (default: Defs/StoryDefs).
# Exit code 1 on errors. Usage: ./Tools/verify_story_content.ps1 [-Defs Defs/StoryDefs,Delegation/StoryContent/Factions/Defs/StoryDefs] [-Ru <folders>]
param([string[]]$Defs = @('Defs/StoryDefs'), [string[]]$Ru = @('Languages/Russian/DefInjected/TurboMegaMod.StoryQuestDef'))
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$errors = New-Object System.Collections.Generic.List[string]
$warns = New-Object System.Collections.Generic.List[string]
$heroes = @{}; Select-String Defs\HeroDefs\*.xml -Pattern '<defName>(\w+)</defName>' | ForEach-Object { $heroes[$_.Matches[0].Groups[1].Value] = 1 }
$factions = @{}; Get-ChildItem Defs\FactionDefs -Recurse -Filter *.xml | Select-String -Pattern '<defName>(\w+)</defName>' | ForEach-Object { $factions[$_.Matches[0].Groups[1].Value] = 1 }
$quests = @{}; $docs = @()
foreach ($dir in $Defs) { foreach ($f in Get-ChildItem $dir -Filter *.xml -ErrorAction SilentlyContinue) {
    $d = New-Object Xml.XmlDocument
    try { $d.LoadXml([IO.File]::ReadAllText($f.FullName)) } catch { $errors.Add("$($f.Name): XML: $($_.Exception.Message)"); continue }
    foreach ($q in $d.SelectNodes('/Defs/TurboMegaMod.StoryQuestDef')) { if ($quests.ContainsKey($q.defName)) { $errors.Add("duplicate defName $($q.defName)") }; $quests[$q.defName] = $q }
    $docs += $d } }
$ruKeys = @{}
foreach ($dir in $Ru) { foreach ($f in Get-ChildItem $dir -Filter *.xml -ErrorAction SilentlyContinue) {
    $r = New-Object Xml.XmlDocument; try { $r.LoadXml([IO.File]::ReadAllText($f.FullName)) } catch { $errors.Add("RU $($f.Name): XML: $($_.Exception.Message)"); continue }
    foreach ($n in $r.SelectNodes('/LanguageData/*')) { $ruKeys[$n.LocalName] = 1 } } }
# flags that are set anywhere
$setFlags = @{ 'act' = 1; 'holodisk' = 1; 'arcPoints' = 1; 'zeroSynth.trust' = 1 }
foreach ($q in $quests.Values) { foreach ($s in $q.SelectNodes('.//li[flag]')) { $setFlags[$s.flag] = 1 } }
$outcomesOf = @{ 'Site' = @('success', 'timeout', 'failed'); 'ColonyRaid' = @('success', 'timeout'); 'HotelVisit' = @('success', 'failed', 'noHotel'); 'Deliver' = @('success', 'timeout') }
function Check-Cond($qname, $c) {
    $x = $c.Trim().TrimStart('!')
    if ($x -match '^(done|active):(.+)$') { if (!$quests.ContainsKey($Matches[2].Trim())) { $errors.Add("${qname}: condition '$c' refers to unknown quest") }; return }
    if ($x -match '^heroAlive:(.+)$') { if (!$heroes.ContainsKey($Matches[1].Trim())) { $errors.Add("${qname}: unknown hero in '$c'") }; return }
    if ($x -match '^factionExists:(.+)$') { if (!$factions.ContainsKey($Matches[1].Trim())) { $errors.Add("${qname}: unknown faction in '$c'") }; return }
    $flag = ($x -split '>=|<=|!=|==|=|<|>')[0].Trim()
    if (!$setFlags.ContainsKey($flag) -and $flag -notlike 'choice.*' -and $flag -notlike 'defector.*' -and $flag -notlike 'q.*' -and $flag -notlike 'arc.*' -and $flag -notlike 'ponr*') { $warns.Add("${qname}: flag '$flag' is tested but never set") }
}
foreach ($q in $quests.Values) {
    $n = $q.defName
    foreach ($c in $q.SelectNodes('requires/li|anyOf/li')) { Check-Cond $n $c.InnerText }
    foreach ($c in $q.SelectNodes('.//choices/li/requires/li|.//choices/li/anyOf/li')) { Check-Cond $n $c.InnerText }
    foreach ($h in $q.SelectNodes('giver|fallbackGiver|.//hero')) { if (!$heroes.ContainsKey($h.InnerText)) { $errors.Add("${n}: unknown hero $($h.InnerText)") } }
    foreach ($fct in $q.SelectNodes('faction|.//enemyFaction|.//li/faction')) { if ($fct.InnerText -and !$factions.ContainsKey($fct.InnerText)) { $errors.Add("${n}: unknown faction $($fct.InnerText)") } }
    if (!$q.giver -and $q.OuterXml.Contains('{GIVER}') -and !$q.faction) { $warns.Add("${n}: {GIVER} used without giver or faction") }
    if ($q.autoStart -ne 'true' -and !$q.letterText) { $errors.Add("${n}: offered quest without letterText") }
    $i = 0
    foreach ($st in $q.SelectNodes('stages/li')) {
        $kind = ($st.Class -replace '^TurboMegaMod.StoryStage_', '')
        if ($kind -eq 'Choice') { $cnt = $st.SelectNodes('choices/li').Count; if ($cnt -lt 1 -or $cnt -gt 12) { $errors.Add("${n} stage ${i}: $cnt choices") } }
        foreach ($field in 'label', 'text') { if ($st.SelectSingleNode($field) -and !$ruKeys.ContainsKey("$n.stages.$i.$field")) { $warns.Add("${n}: no RU for stages.$i.$field") } }
        $j = 0; foreach ($ch in $st.SelectNodes('choices/li')) { foreach ($field in 'label', 'resultText') { if ($ch.SelectSingleNode($field) -and !$ruKeys.ContainsKey("$n.stages.$i.choices.$j.$field")) { $warns.Add("${n}: no RU for stages.$i.choices.$j.$field") } }; $j++ }
        $i++
    }
    foreach ($field in 'label', 'letterLabel', 'letterText') { if ($q.SelectSingleNode($field) -and !$ruKeys.ContainsKey("$n.$field")) { $warns.Add("${n}: no RU for $field") } }
}
# gate flags set only inside some outcomes of a stage
foreach ($q in $quests.Values) { foreach ($c in $q.SelectNodes('requires/li|anyOf/li')) {
    $g = ($c.InnerText.Trim().TrimStart('!') -split '>=|<=|!=|==|=|<|>|:')[0].Trim(); if ($g -notlike 'story.*') { continue }
    $setters = @(); foreach ($qq in $quests.Values) { $setters += @($qq.SelectNodes(".//li[flag='$g']")) }
    if (@($setters | Where-Object { $_.ParentNode.LocalName -eq 'onComplete' -or $_.ParentNode.ParentNode.ParentNode.LocalName -eq 'choices' }).Count -gt 0) { continue }
    foreach ($s in $setters) { $stage = $s.ParentNode.ParentNode.ParentNode.ParentNode; $kind = ($stage.Class -replace '^TurboMegaMod.StoryStage_', ''); if (!$outcomesOf.ContainsKey($kind)) { continue }
        $have = $stage.SelectNodes("outcomes/li[effects/li[flag='$g']]") | ForEach-Object outcome
        $miss = $outcomesOf[$kind] | Where-Object { $have -notcontains $_ }
        if ($miss -and $c.InnerText -notmatch '^!') { $warns.Add("gate $g (used by $($q.defName)) not set on outcomes: $($miss -join ',')") } } } }
"quests: $($quests.Count), RU keys: $($ruKeys.Count), errors: $($errors.Count), warnings: $($warns.Count)"
$errors | Sort-Object -Unique | ForEach-Object { "ERROR  $_" }
$warns | Sort-Object -Unique | Select-Object -First 60 | ForEach-Object { "WARN   $_" }
if ($errors.Count -gt 0) { exit 1 }
