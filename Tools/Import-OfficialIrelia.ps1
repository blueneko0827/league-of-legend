param(
    [Parameter(Mandatory = $true)][string]$Converter,
    [string]$DotNetRoot
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskSource = Join-Path $taskRoot 'Assets/SourceModels/IreliaOfficial/Raw'
$taskOutput = Join-Path $taskRoot 'Assets/SourceModels/IreliaOfficial/IreliaOfficial.glb'
$taskConverter = (Resolve-Path -LiteralPath $Converter).Path
$taskOldRuntime = $env:DOTNET_ROOT
try {
    if ($DotNetRoot) { $env:DOTNET_ROOT = (Resolve-Path -LiteralPath $DotNetRoot).Path }
    & $taskConverter skn2gltf `
        -m (Join-Path $taskSource 'irelia_base.skn') `
        -s (Join-Path $taskSource 'irelia_base.skl') `
        -a (Join-Path $taskSource 'animations') `
        -g $taskOutput `
        --materials Irelia_Skin05_Mat Irelia_Base_Mat blades `
        --textures (Join-Path $taskSource 'irelia_base_tx_cm.tex') (Join-Path $taskSource 'irelia_base_tx_cm.tex') (Join-Path $taskSource 'irelia_base_blades_tx_cm.tex')
    if ($LASTEXITCODE -ne 0) { throw "lol2gltf failed with exit code $LASTEXITCODE" }
    $taskBytes = [System.IO.File]::ReadAllBytes($taskOutput)
    if ($taskBytes.Length -lt 20 -or [System.Text.Encoding]::ASCII.GetString($taskBytes, 0, 4) -ne 'glTF') { throw 'The converter did not generate a valid GLB.' }
    Write-Output 'GLB generated. In Unity, use MidLane > Reimportar Irelia oficial to bake the native assets.'
}
finally { $env:DOTNET_ROOT = $taskOldRuntime }
