[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GameDir,

    [string]$LegacyLibDir,

    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($LegacyLibDir)) {
    $LegacyLibDir = Join-Path (Split-Path $repoRoot -Parent) 'build\Plugin'
}
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot 'output\release'
}

$gameRoot = [System.IO.Path]::GetFullPath($GameDir)
$legacyRoot = [System.IO.Path]::GetFullPath($LegacyLibDir)
$stageRoot = [System.IO.Path]::GetFullPath($OutputDirectory)
$project = Join-Path $repoRoot 'KKCharaStudioVRPlugin\KKCharaStudioVRPlugin.csproj'
$noVrLauncher = Join-Path $repoRoot 'launchers\StartCharaStudioNoVR.bat'
$vrLauncher = Join-Path $repoRoot 'launchers\StartCharaStudioVR.bat'

if (-not (Test-Path -LiteralPath (Join-Path $gameRoot 'CharaStudio_Data\Managed\Assembly-CSharp.dll'))) {
    throw "GameDir does not point to a complete Koikatu/CharaStudio installation: $gameRoot"
}
if (-not (Test-Path -LiteralPath (Join-Path $legacyRoot '0Harmony_BepInEx4.dll'))) {
    throw "LegacyLibDir is missing 0Harmony_BepInEx4.dll: $legacyRoot"
}
if (-not (Test-Path -LiteralPath $noVrLauncher)) {
    throw "The NoVR launcher is missing: $noVrLauncher"
}
if (-not (Test-Path -LiteralPath $vrLauncher)) {
    throw "The VR launcher is missing: $vrLauncher"
}

$msbuild = $null
$vswhere = Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Microsoft Visual Studio\Installer\vswhere.exe'
if (Test-Path -LiteralPath $vswhere) {
    $candidate = & $vswhere -latest -products * `
        -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 `
        -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
    if ($candidate) {
        $msbuild = $candidate.Trim()
    }
}
if (-not $msbuild) {
    $command = Get-Command MSBuild.exe -ErrorAction SilentlyContinue
    if ($command) {
        $msbuild = $command.Source
    }
}
if (-not $msbuild) {
    throw 'Visual Studio 2022 MSBuild with the Desktop development with C++ workload is required.'
}

& $msbuild $project /nologo /m /restore /t:Rebuild `
    /p:Configuration=Release `
    /p:KKGameDir="$gameRoot" `
    /p:LegacyLibDir="$legacyRoot" `
    /p:BuildNativeReShadeBridge=true
if ($LASTEXITCODE -ne 0) {
    throw "Release build failed with exit code $LASTEXITCODE."
}

$managedOutput = Join-Path $repoRoot 'KKCharaStudioVRPlugin\bin\Release\net35'
$pluginDll = Join-Path $managedOutput 'KKCharaStudioVRPlugin.dll'
$bridge = Join-Path $managedOutput 'KKVRReShadeBridge.addon64'
if (-not (Test-Path -LiteralPath $pluginDll)) {
    throw "Managed plug-in output is missing: $pluginDll"
}
if (-not (Test-Path -LiteralPath $bridge)) {
    throw "Native ReShade bridge output is missing: $bridge"
}

$pluginStage = Join-Path $stageRoot 'BepInEx'
New-Item -ItemType Directory -Force -Path $pluginStage | Out-Null
New-Item -ItemType Directory -Force -Path $stageRoot | Out-Null
Copy-Item -LiteralPath $pluginDll -Destination $pluginStage -Force
Copy-Item -LiteralPath $bridge -Destination $stageRoot -Force
Copy-Item -LiteralPath $noVrLauncher -Destination $stageRoot -Force
Copy-Item -LiteralPath $vrLauncher -Destination $stageRoot -Force

Write-Host "Release staged at: $stageRoot"
Write-Host "  BepInEx plug-in: $(Join-Path $pluginStage 'KKCharaStudioVRPlugin.dll')"
Write-Host "  ReShade add-on:  $(Join-Path $stageRoot 'KKVRReShadeBridge.addon64')"
Write-Host "  NoVR launcher:   $(Join-Path $stageRoot 'StartCharaStudioNoVR.bat')"
Write-Host "  VR launcher:     $(Join-Path $stageRoot 'StartCharaStudioVR.bat')"
