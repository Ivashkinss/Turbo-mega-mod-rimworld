# Builds the player-only "release" branch from a commit of main (default HEAD) without touching the
# working tree or the main index. The README download button points at
# https://github.com/Ivashkinss/Turbo-mega-mod-rimworld/archive/refs/heads/release.zip
# Usage: ./Tools/publish_release_branch.ps1 -Version 0.10 [-Push]
param([Parameter(Mandatory = $true)][string]$Version, [string]$Commit = 'HEAD', [switch]$Push)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$keep = @('About', 'Assemblies', 'Components', 'Defs', 'Languages', 'Patches', 'Textures', 'ThirdParty', 'LoadFolders.xml')
$sha = (git rev-parse $Commit).Trim()
[xml]$about = git show "${sha}:About/About.xml"
if ($about.ModMetaData.modVersion -ne $Version) { throw "About.xml modVersion at $sha is '$($about.ModMetaData.modVersion)', expected $Version" }

$install = @"
Turbo mega mod $Version (RimWorld 1.7)

1. Распакуйте эту папку в папку Mods игры, например:
   C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods
   Внутри папки мода должны лежать About, Defs, Textures и другие.
2. Отключите все остальные моды: Harmony, HugsLib, VEF и прочее уже встроено.
   Официальные DLC оставьте включёнными. Turbo mega mod — последним.
3. Начните новую игру.

Баги и идеи: https://vk.ru/tvoydubrovski
Поддержать автора: https://music.yandex.ru/artist/22932524 (Твой Дубровский)
Страница мода: https://github.com/Ivashkinss/Turbo-mega-mod-rimworld
"@
$tmpFile = Join-Path $env:TEMP ('tmm-install-' + [guid]::NewGuid() + '.txt')
[IO.File]::WriteAllText($tmpFile, $install.Replace("`n", "`r`n").Replace("`r`r`n", "`r`n"), (New-Object Text.UTF8Encoding $true))

$index = Join-Path $env:TEMP ('tmm-release-index-' + [guid]::NewGuid())
$oldIndex = $env:GIT_INDEX_FILE
try {
    $env:GIT_INDEX_FILE = $index
    git read-tree $sha
    if ($LASTEXITCODE -ne 0) { throw 'read-tree failed' }
    foreach ($top in (git ls-tree --name-only $sha)) {
        if ($keep -notcontains $top) { git rm -r --cached -q --ignore-unmatch -- $top | Out-Null }
    }
    $blob = (git hash-object -w $tmpFile).Trim()
    git update-index --add --cacheinfo "100644,$blob,INSTALL.txt"
    $tree = (git write-tree).Trim()
}
finally {
    $env:GIT_INDEX_FILE = $oldIndex
    Remove-Item $index, $tmpFile -ErrorAction SilentlyContinue
}

$parentArgs = @()
git rev-parse -q --verify refs/heads/release *> $null
if ($LASTEXITCODE -eq 0) { $parentArgs = @('-p', (git rev-parse refs/heads/release).Trim()) }
$env:GIT_AUTHOR_NAME = 'Ivashkinss'; $env:GIT_AUTHOR_EMAIL = '166906583+Ivashkinss@users.noreply.github.com'
$env:GIT_COMMITTER_NAME = $env:GIT_AUTHOR_NAME; $env:GIT_COMMITTER_EMAIL = $env:GIT_AUTHOR_EMAIL
$short = $sha.Substring(0, 7)
$commitArgs = @('commit-tree', $tree) + $parentArgs + @('-m', "Turbo mega mod $Version (main $short)")
$new = (& git @commitArgs).Trim()
git update-ref refs/heads/release $new
git tag -f "v$Version" $new | Out-Null
"release -> $new (v$Version from main $short)"
git ls-tree --name-only $new
if ($Push) {
    # git reports progress on stderr; judge success by the exit code only.
    $ErrorActionPreference = 'Continue'
    git push -q origin refs/heads/release:refs/heads/release 2>$null
    if ($LASTEXITCODE -ne 0) { throw 'push of release branch failed' }
    git push -q origin "refs/tags/v${Version}:refs/tags/v$Version" 2>$null
    if ($LASTEXITCODE -ne 0) { throw "push of tag v$Version failed" }
    'pushed release and tag'
}
