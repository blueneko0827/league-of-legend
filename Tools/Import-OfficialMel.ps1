param(
    [Parameter(Mandatory = $true)][string]$Converter,
    [Parameter(Mandatory = $true)][string]$Source,
    [string]$DotNetRoot
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskOutputFolder = Join-Path $taskRoot 'Assets/SourceModels/MelOfficial'
$taskRawFolder = Join-Path $taskOutputFolder 'Raw'
New-Item -ItemType Directory -Path (Join-Path $taskRawFolder 'animations') -Force | Out-Null
$taskFiles = @('mel_base.skn', 'mel_base.skl', 'mel_base_tx_cm.tex', 'mel_base_dressmask_tx_cm.tex',
    'animations/idle1.anm', 'animations/run.anm', 'animations/attack1.anm')
$taskEntries = foreach ($taskName in $taskFiles) {
    $taskInput = Join-Path $Source $taskName
    $taskDestination = Join-Path $taskRawFolder $taskName
    Copy-Item -LiteralPath $taskInput -Destination $taskDestination -Force
    @{ file = $taskName; sha256 = (Get-FileHash -LiteralPath $taskInput -Algorithm SHA256).Hash.ToLowerInvariant() }
}
$taskOldRuntime = $env:DOTNET_ROOT
try {
    if ($DotNetRoot) { $env:DOTNET_ROOT = (Resolve-Path -LiteralPath $DotNetRoot).Path }
    & $Converter skn2gltf -m (Join-Path $taskRawFolder 'mel_base.skn') -s (Join-Path $taskRawFolder 'mel_base.skl') `
        -a (Join-Path $taskRawFolder 'animations') -g (Join-Path $taskOutputFolder 'MelOfficial.glb') `
        --materials Body Sleeve Dress `
        --textures (Join-Path $taskRawFolder 'mel_base_tx_cm.tex') (Join-Path $taskRawFolder 'mel_base_tx_cm.tex') (Join-Path $taskRawFolder 'mel_base_tx_cm.tex')
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo convertir el modelo oficial de Mel.' }
    @{ source_folder = $Source; files = $taskEntries; converter = 'lol2gltf';
       clips = @{ Idle = 'idle1'; Run = 'run'; Attack = 'attack1' };
       presentation = 'Original body, sleeve and dress geometry with the base color atlas. Dress mask preserved as source reference.' } |
        ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $taskOutputFolder 'Sources.json') -Encoding utf8
}
finally { $env:DOTNET_ROOT = $taskOldRuntime }
