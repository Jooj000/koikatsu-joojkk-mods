# Builds the release DLLs and makes one zip per mod in dist\release, laid out to be unpacked over the game's folder
# Usage:  powershell -ExecutionPolicy Bypass -File tools\package_release.ps1 [-Mods KK_CampusLife,...]
param([string[]]$Mods = @('KK_CampusLife', 'KK_SchoolOptimizer', 'KK_HMaleWardrobe', 'KK_CampusSaves'))

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
& (Join-Path $root 'build.ps1')
if ($Mods -contains 'KK_CampusSaves') { & (Join-Path $root 'build.ps1') -Only KK_CampusSaves }
$out = Join-Path $root 'dist\release'
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory -Force $out | Out-Null

foreach ($mod in $Mods) {
  $plugin = Get-ChildItem (Join-Path $root "src\$mod") -Filter '*Plugin.cs' | Select-Object -First 1
  $version = ([regex]::Match((Get-Content $plugin.FullName -Raw), 'Version = "([0-9.]+)"')).Groups[1].Value
  if (-not $version) { throw "No version in $($plugin.Name)" }
  $stage = Join-Path $out "stage_$mod"
  $dest = Join-Path $stage 'BepInEx\plugins\Joojkk'
  New-Item -ItemType Directory -Force $dest | Out-Null
  Copy-Item (Join-Path $root "dist\$mod.dll") $dest
  if ($mod -eq 'KK_CampusLife') {
    New-Item -ItemType Directory -Force (Join-Path $dest 'backgrounds') | Out-Null
    Copy-Item (Join-Path $root 'data\backgrounds\*.jpg') (Join-Path $dest 'backgrounds')
  }
  Copy-Item (Join-Path $root "src\$mod\README.md"), (Join-Path $root "src\$mod\CHANGELOG.md") $stage
  $zip = Join-Path $out "$mod-$version.zip"
  Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip
  Remove-Item $stage -Recurse -Force
  Write-Host ("{0}  {1:0.0} MB" -f (Split-Path -Leaf $zip), ((Get-Item $zip).Length / 1MB))
}
