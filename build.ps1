# Builds KK_CampusLife.dll, KK_HMaleWardrobe.dll and KK_SchoolOptimizer.dll with the Roslyn csc from VS 2019.
# Usage:  powershell -ExecutionPolicy Bypass -File build.ps1 [-Install]
#   -Install  copies the DLLs into the game's BepInEx\plugins\Joojkk\ folder (each mod is one DLL).
#   -Only     builds just that plugin (KK_CampusLife, KK_HMaleWardrobe, KK_SchoolOptimizer or KK_CampusSaves). A plugin
#             whose src folder is missing (a single-mod repository) is skipped.
#   -Game     the Koikatsu folder (with Koikatu_Data and BepInEx);  -Csc  the Roslyn csc.exe to use.
param([switch]$Install, [switch]$Dev, [string]$Only = '',
      [string]$Game = 'E:\SteamLibrary\steamapps\settings\KK',
      [string]$Csc = 'C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\Roslyn\csc.exe')

$ErrorActionPreference = 'Stop'
$root    = Split-Path -Parent $MyInvocation.MyCommand.Path
$game    = $Game
$csc     = $Csc
$managed = Join-Path $game 'Koikatu_Data\Managed'
$core    = Join-Path $game 'BepInEx\core'
$plugins = Join-Path $game 'BepInEx\plugins'
$out     = Join-Path $root 'dist'
New-Item -ItemType Directory -Force $out | Out-Null

$refs = @(
  "$managed\mscorlib.dll", "$managed\System.dll", "$managed\System.Core.dll", "$managed\System.Xml.dll",
  "$managed\UnityEngine.dll", "$managed\UnityEngine.UI.dll",
  "$managed\Assembly-CSharp.dll", "$managed\Assembly-CSharp-firstpass.dll",
  "$managed\TextMeshPro-1.0.55.56.0b12.dll",
  "$core\BepInEx.dll", "$core\0Harmony.dll",
  "$plugins\KKAPI.dll", "$plugins\KK_BepisPlugins\ExtensibleSaveFormat.dll", "$plugins\KK_BepisPlugins\Sideloader.dll"
) | ForEach-Object { "-r:$_" }

# BepInEx rejects config sections/keys containing = \n \t \ " ' [ ] and the plugin then
# fails in Awake. Check every B(c, section, "key", ...) and Config.Bind(section, "key", ...).
$bad = @()
Get-ChildItem -Recurse -Filter *.cs (Join-Path $root 'src') | ForEach-Object {
  $text = [IO.File]::ReadAllText($_.FullName)
  foreach ($m in [regex]::Matches($text, '(?:\bB\(c,\s*[^,]+|Config\.Bind\([^,]+),\s*"((?:[^"\\]|\\.)*)"')) {
    if ($m.Groups[1].Value -match "[=\t\\""'\[\]]") { $bad += "$($_.Name): $($m.Groups[1].Value)" }
  }
}
if ($bad.Count -gt 0) { $bad | ForEach-Object { Write-Host "Invalid config key: $_" }; throw 'Config key check failed' }

function Build($name) {
  $src = Get-ChildItem -Recurse -Filter *.cs (Join-Path $root "src\$name") | ForEach-Object { $_.FullName }
  $dll = Join-Path $out "$name.dll"
  $cscArgs = @('-nologo', '-noconfig', '-nostdlib+', '-target:library', '-optimize+', '-debug-',
            '-langversion:7.3', '-warn:4', '-nowarn:1591,0649,0169,0414,0618',
            "-out:$dll") + $refs + $src
  if ($Dev) { $cscArgs += '-define:DEBUG_TOOLS' }
  # The procedural dialogue bank (data\dialogue\*.dlg) is built into the DLL; a file of the same name in
  # plugins\Joojkk\dialogue replaces it (for trying lines without building).
  $dlgDir = Join-Path $root "data\dialogue"
  if ($name -eq 'KK_CampusLife' -and (Test-Path $dlgDir)) {
    Get-ChildItem $dlgDir -Filter *.dlg | ForEach-Object { $cscArgs += "-resource:$($_.FullName),CampusLife.dialogue.$($_.Name)" }
  }
  # The default activity spots (placed with the Alt+F7 window), built into the DLL.
  $spotsDefault = Join-Path $root "data\spots_default.txt"
  if ($name -eq 'KK_CampusLife' -and (Test-Path $spotsDefault)) { $cscArgs += "-resource:$spotsDefault,CampusLife.spots_default.txt" }
  # The painted event backgrounds, the loading screen's map photos and the staff-title translation, so the mod is a
  # single DLL. A file of the same name in a folder next to the DLL replaces the built-in one.
  if ($name -eq 'KK_CampusLife') {
    foreach ($pair in @(@('backgrounds', '*.jpg'), @('mapshots', '*.jpg'), @('translation', '*.txt'))) {
      $dir = Join-Path $root "data\$($pair[0])"
      if (Test-Path $dir) { Get-ChildItem $dir -Filter $pair[1] | ForEach-Object { $cscArgs += "-resource:$($_.FullName),CampusLife.$($pair[0]).$($_.Name)" } }
    }
  }
  & $csc @cscArgs
  if ($LASTEXITCODE -ne 0) { throw "Build failed: $name" }
  Write-Host "Built $dll"
}

# KK_CampusSaves is built only by name (-Only KK_CampusSaves).
$names = @('KK_CampusLife', 'KK_HMaleWardrobe', 'KK_SchoolOptimizer', 'KK_CampusSaves') | Where-Object { (Test-Path (Join-Path $root "src\$_")) -and (($Only -eq '' -and $_ -ne 'KK_CampusSaves') -or $Only -eq $_) }
foreach ($n in $names) { Build $n }

if ($Install) {
  $dest = Join-Path $plugins 'Joojkk'
  New-Item -ItemType Directory -Force $dest | Out-Null
  foreach ($n in $names) { Copy-Item (Join-Path $out "$n.dll") $dest -Force }
  Write-Host "Installed to $dest"
}
