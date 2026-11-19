# Takes the newest Hydrogen answer from Delegation/Hydrogen/Inbox (two ```xml blocks: EN defs, RU DefInjected),
# applies the known mechanical fixes, audits references and gate flags, and stages it under Delegation/StoryContent/<Folder>.
# Usage: ./Tools/stage_hydrogen_story.ps1 -Folder Act5a -Name Story_Act5_Part1 -Prefix TMM_Story_A5_
param([Parameter(Mandatory = $true)][string]$Folder, [Parameter(Mandatory = $true)][string]$Name, [Parameter(Mandatory = $true)][string]$Prefix)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$f = Get-ChildItem Delegation\Hydrogen\Inbox -File | Where-Object Name -ne 'README.md' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (!$f) { throw 'Inbox is empty' }
"file: $($f.Name)"
$src = [IO.File]::ReadAllText($f.FullName, [Text.Encoding]::UTF8)
$blocks = [regex]::Matches($src, '(?s)```xml\s*(.*?)```')
if ($blocks.Count -lt 2) { throw "expected 2 xml blocks, got $($blocks.Count)" }
$en = $blocks[0].Groups[1].Value; $ru = $blocks[1].Groups[1].Value
$en = [regex]::Replace($en, '(<li>[^<>]*?)<(=?\s*-?\d)', '$1&lt;$2')
$en = [regex]::Replace($en, 'Class="Story(Effect|Stage)_', 'Class="TurboMegaMod.Story$1_')
$en = $en.Replace('>TMM_MinutemenFaction<', '>TMM_MinutemenHuntersFaction<').Replace('TMM_Hero_brian_virgil', 'TMM_Hero_virgil').Replace('TMM_Hero_mr_kendricks', 'TMM_Hero_kendricks')
$ru = [regex]::Replace($ru, '(outcomes\.\d+\.)outcomes\.', '$1')  # recurring Hydrogen typo in RU closing tags
$d = New-Object Xml.XmlDocument; $d.LoadXml($en)
$r = New-Object Xml.XmlDocument; $r.LoadXml($ru)
$qs = $d.SelectNodes('/Defs/TurboMegaMod.StoryQuestDef')
"quests: $($qs.Count)"
$knownF = Get-ChildItem Defs\FactionDefs -Recurse -Filter *.xml | Select-String -Pattern '<defName>(\w+)</defName>' | ForEach-Object { $_.Matches[0].Groups[1].Value }
$facs = $d.SelectNodes('//faction|//enemyFaction') | ForEach-Object InnerText | Sort-Object -Unique
"bad factions: " + (($facs | Where-Object { $knownF -notcontains $_ }) -join ', ')
$knownH = Select-String Defs\HeroDefs\*.xml -Pattern '<defName>(\w+)</defName>' | ForEach-Object { $_.Matches[0].Groups[1].Value }
$refs = [regex]::Matches($en, 'TMM_Hero_\w+') | ForEach-Object Value | Sort-Object -Unique
"bad heroes: " + (($refs | Where-Object { $knownH -notcontains $_ }) -join ', ')
"things: " + (($d.SelectNodes('//thing') | ForEach-Object InnerText | Sort-Object -Unique) -join ', ')
"classes: " + (($d.SelectNodes('//*[@Class]') | ForEach-Object { $_.Class.Replace('TurboMegaMod.', '') } | Sort-Object -Unique) -join ', ')
$badRu = $r.SelectNodes('/LanguageData/*') | Where-Object { $_.LocalName -notlike "$Prefix*" }
"ru keys: $($r.SelectNodes('/LanguageData/*').Count); wrong prefix: $(@($badRu).Count)"
# Gate flags (used in requires/anyOf) that are set only inside stage outcomes without covering every outcome of that stage.
$all = @{ 'Site' = @('success', 'timeout', 'failed'); 'ColonyRaid' = @('success', 'timeout'); 'HotelVisit' = @('success', 'failed', 'noHotel'); 'Deliver' = @('success', 'timeout') }
$gates = $d.SelectNodes('//requires/li|//anyOf/li') | ForEach-Object { ($_.InnerText.TrimStart('!') -split '[=!<>:]')[0] } | Where-Object { $_ -like 'story.*' } | Sort-Object -Unique
foreach ($g in $gates) {
    $setters = @($d.SelectNodes("//li[flag='$g']"))
    if ($setters.Count -eq 0) { continue }
    $safe = $setters | Where-Object { $_.ParentNode.LocalName -eq 'onComplete' -or $_.ParentNode.ParentNode.ParentNode.LocalName -eq 'choices' }
    if (@($safe).Count -gt 0) { continue }
    foreach ($s in $setters) {
        $stage = $s.ParentNode.ParentNode.ParentNode.ParentNode
        $kind = $stage.Class -replace '^TurboMegaMod.StoryStage_', ''
        if (!$all.ContainsKey($kind)) { continue }
        $have = $stage.SelectNodes("outcomes/li[effects/li[flag='$g']]") | ForEach-Object outcome
        $missing = $all[$kind] | Where-Object { $have -notcontains $_ }
        if ($missing) { $q = $stage.ParentNode.ParentNode; "GATE RISK: $g in $($q.defName) ($kind) missing outcomes: $($missing -join ',')" }
    }
}
$out = "Delegation\StoryContent\$Folder"
New-Item -ItemType Directory -Force $out | Out-Null
$u = New-Object Text.UTF8Encoding $false
[IO.File]::WriteAllText((Join-Path $root "$out\$Name.xml"), $en.Trim(), $u)
[IO.File]::WriteAllText((Join-Path $root "$out\$Name.ru.xml"), $ru.Trim(), $u)
Move-Item $f.FullName (Join-Path $root "$out\hydrogen-raw.md") -Force
"staged: $out"
