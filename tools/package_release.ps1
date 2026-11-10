# Builds the release DLLs and makes one zip per mod in dist\release. Each zip holds only BepInEx\plugins\Joojkk\<mod>.dll,
# so it is unpacked straight over the game's folder. Publishes nothing.
# Usage:  powershell -ExecutionPolicy Bypass -File tools\package_release.ps1 [-Mods KK_CampusLife,...]
param([string[]]$Mods = @('KK_CampusLife', 'KK_SchoolOptimizer', 'KK_HMaleWardrobe', 'KK_CampusSaves'))

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
& (Join-Path $root 'build.ps1')
if ($Mods -contains 'KK_CampusSaves') { & (Join-Path $root 'build.ps1') -Only KK_CampusSaves }
$out = Join-Path $root 'dist\release'
New-Item -ItemType Directory -Force $out | Out-Null
Get-ChildItem $out | Remove-Item -Recurse -Force

foreach ($mod in $Mods) {
  $plugin = Get-ChildItem (Join-Path $root "src\$mod") -Filter '*Plugin.cs' | Select-Object -First 1
  $version = ([regex]::Match((Get-Content $plugin.FullName -Raw), 'Version = "([0-9.]+)"')).Groups[1].Value
  if (-not $version) { throw "No version in $($plugin.Name)" }
  # Everything the mod needs (Campus Life's backgrounds, map photos and staff titles too) is inside the DLL. The entry
  # is written with forward slashes (Compress-Archive on Windows PowerShell writes backslashes, which some unzip tools
  # take as part of the file name).
  $zip = Join-Path $out "$mod-$version.zip"
  $archive = [System.IO.Compression.ZipFile]::Open($zip, [System.IO.Compression.ZipArchiveMode]::Create)
  try { [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, (Join-Path $root "dist\$mod.dll"), "BepInEx/plugins/Joojkk/$mod.dll", [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null }
  finally { $archive.Dispose() }
  Write-Host ("{0}  {1:0.0} MB" -f (Split-Path -Leaf $zip), ((Get-Item $zip).Length / 1MB))
}
