# Fixes known mechanical issues in staged story XML in place and audits references and gates.
# Usage: ./Tools/audit_story_xml.ps1 -Path Delegation/StoryContent/Factions
param([Parameter(Mandatory = $true)][string]$Path)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$u = New-Object Text.UTF8Encoding $false
$knownF = Get-ChildItem Defs\FactionDefs -Recurse -Filter *.xml | Select-String -Pattern '<defName>(\w+)</defName>' | ForEach-Object { $_.Matches[0].Groups[1].Value }
$knownH = Select-String Defs\HeroDefs\*.xml -Pattern '<defName>(\w+)</defName>' | ForEach-Object { $_.Matches[0].Groups[1].Value }
$all = @{ 'Site' = @('success', 'timeout', 'failed'); 'ColonyRaid' = @('success', 'timeout'); 'HotelVisit' = @('success', 'failed', 'noHotel'); 'Deliver' = @('success', 'timeout', 'failed') }
foreach ($file in Get-ChildItem (Join-Path $Path 'Defs\StoryDefs') -Filter *.xml) {
    $t = [IO.File]::ReadAllText($file.FullName, [Text.Encoding]::UTF8)
    $t = [regex]::Replace($t, '(<li>[^<>]*?)<(=?\s*-?\d)', '$1&lt;$2')
    $t = [regex]::Replace($t, 'Class="Story(Effect|Stage)_', 'Class="TurboMegaMod.Story$1_')
    $t = $t.Replace('>TMM_MinutemenFaction<', '>TMM_MinutemenHuntersFaction<').Replace('TMM_Hero_brian_virgil', 'TMM_Hero_virgil').Replace('TMM_Hero_mr_kendricks', 'TMM_Hero_kendricks').Replace('TMM_Hero_charles_curling', 'TMM_Hero_dr_curling').Replace('TMM_Hero_fawkes', 'TMM_Hero_fox')
    [IO.File]::WriteAllText($file.FullName, $t, $u)
    $d = New-Object Xml.XmlDocument; $d.LoadXml($t)
    $qs = $d.SelectNodes('/Defs/TurboMegaMod.StoryQuestDef')
    $facs = $d.SelectNodes('//faction|//enemyFaction') | ForEach-Object InnerText | Sort-Object -Unique
    $badF = $facs | Where-Object { $knownF -notcontains $_ }
    $badH = [regex]::Matches($t, 'TMM_Hero_\w+') | ForEach-Object Value | Sort-Object -Unique | Where-Object { $knownH -notcontains $_ }
    $arc = $d.SelectNodes("//onComplete/li[flag='arcPoints']").Count
    $risks = @()
    $gates = $d.SelectNodes('//requires/li|//anyOf/li') | ForEach-Object { ($_.InnerText.TrimStart('!') -split '[=!<>:]')[0] } | Where-Object { $_ -like 'story.*' } | Sort-Object -Unique
    foreach ($g in $gates) {
        $setters = @($d.SelectNodes("//li[flag='$g']"))
        if ($setters.Count -eq 0) { continue }
        if (@($setters | Where-Object { $_.ParentNode.LocalName -eq 'onComplete' -or $_.ParentNode.ParentNode.ParentNode.LocalName -eq 'choices' }).Count -gt 0) { continue }
        foreach ($s in $setters) {
            $stage = $s.ParentNode.ParentNode.ParentNode.ParentNode
            $kind = $stage.Class -replace '^TurboMegaMod.StoryStage_', ''
            if (!$all.ContainsKey($kind)) { continue }
            $have = $stage.SelectNodes("outcomes/li[effects/li[flag='$g']]") | ForEach-Object outcome
            $missing = $all[$kind] | Where-Object { $have -notcontains $_ }
            if ($missing) { $risks += "$g in $($stage.ParentNode.ParentNode.defName) ($kind) missing: $($missing -join ',')" }
        }
    }
    $ru = Join-Path $Path ("Languages\Russian\DefInjected\TurboMegaMod.StoryQuestDef\" + $file.Name)
    $ruN = 0
    if (Test-Path $ru) {
        $rt = [regex]::Replace([IO.File]::ReadAllText($ru, [Text.Encoding]::UTF8), '(outcomes\.\d+\.)outcomes\.', '$1')
        [IO.File]::WriteAllText($ru, $rt, $u)
        $r = New-Object Xml.XmlDocument; $r.LoadXml($rt); $ruN = $r.SelectNodes('/LanguageData/*').Count
    }
    "{0}: quests {1}, arcPoints {2}, ru keys {3}, bad factions [{4}], bad heroes [{5}]" -f $file.Name, $qs.Count, $arc, $ruN, ($badF -join ','), ($badH -join ',')
    $risks | ForEach-Object { "   GATE RISK: $_" }
}
