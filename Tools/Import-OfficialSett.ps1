param(
    [Parameter(Mandatory = $true)][string]$Converter,
    [Parameter(Mandatory = $true)][string]$Source,
    [string]$DotNetRoot
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskOutputFolder = Join-Path $taskRoot 'Assets/SourceModels/SettOfficial'
$taskRawFolder = Join-Path $taskOutputFolder 'Raw'
New-Item -ItemType Directory -Path (Join-Path $taskRawFolder 'animations') -Force | Out-Null
$taskFiles = @('sett.skn', 'sett.skl', 'sett_base_tx_cm.tex', 'sett_base_mom_tx_cm.tex', 'sett_base_wolverine_tx_cm.tex',
    'animations/sett_idle.anm', 'animations/sett_run.anm', 'animations/sett_attack1_start.anm', 'animations/sett_attack1.anm')
$taskEntries = foreach ($taskName in $taskFiles) {
    $taskInput = Join-Path $Source $taskName
    $taskDestination = Join-Path $taskRawFolder $taskName
    Copy-Item -LiteralPath $taskInput -Destination $taskDestination -Force
    @{ file = $taskName; sha256 = (Get-FileHash -LiteralPath $taskInput -Algorithm SHA256).Hash.ToLowerInvariant() }
}
$taskOldRuntime = $env:DOTNET_ROOT
try {
    if ($DotNetRoot) { $env:DOTNET_ROOT = (Resolve-Path -LiteralPath $DotNetRoot).Path }
    & $Converter skn2gltf -m (Join-Path $taskRawFolder 'sett.skn') -s (Join-Path $taskRawFolder 'sett.skl') `
        -a (Join-Path $taskRawFolder 'animations') -g (Join-Path $taskOutputFolder 'SettOfficial.glb') `
        --materials Sett_Base_Mat Joke Wolverine `
        --textures (Join-Path $taskRawFolder 'sett_base_tx_cm.tex') (Join-Path $taskRawFolder 'sett_base_mom_tx_cm.tex') (Join-Path $taskRawFolder 'sett_base_wolverine_tx_cm.tex')
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo convertir el modelo oficial de Sett.' }
    @{ source_folder = $Source; files = $taskEntries; converter = 'lol2gltf';
       clips = @{ Idle = 'sett_idle'; Run = 'sett_run'; AttackStart = 'sett_attack1_start'; Attack = 'sett_attack1' };
       presentation = 'Official base body and atlas. Joke and Wolverine source geometry preserved but hidden in the normal-form prefab.' } |
        ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $taskOutputFolder 'Sources.json') -Encoding utf8
}
finally { $env:DOTNET_ROOT = $taskOldRuntime }
