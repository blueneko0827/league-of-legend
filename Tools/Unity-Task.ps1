param(
    [Parameter(Mandatory=$true)][ValidateSet('Open','Test','Build')][string]$Task,
    [string]$UnityPath
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
if (-not $UnityPath) {
    $taskEditorRoot = Join-Path $env:ProgramFiles 'Unity\Hub\Editor'
    $taskVersionText = Get-Content -LiteralPath (Join-Path $taskRoot 'ProjectSettings\ProjectVersion.txt') -Raw
    $taskVersionMatch = [regex]::Match($taskVersionText, 'm_EditorVersion:\s*(\S+)')
    $UnityPath = Join-Path $taskEditorRoot ($taskVersionMatch.Groups[1].Value + '\Editor\Unity.exe')
    if (-not (Test-Path -LiteralPath $UnityPath)) {
        $taskInstalled = Get-ChildItem -LiteralPath $taskEditorRoot -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -match '^\d+\.\d+\.\d+[abfp]\d+$' } |
            Sort-Object { [version]($_.Name -replace '[abfp].*$', '') } -Descending |
            Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'Editor\Unity.exe') } |
            Select-Object -First 1
        if ($taskInstalled) { $UnityPath = Join-Path $taskInstalled.FullName 'Editor\Unity.exe' }
    }
}
if (-not (Test-Path -LiteralPath $UnityPath)) { throw 'No se encontró Unity. Pasá -UnityPath con la ubicación del editor.' }
if ($Task -eq 'Open') {
    & $UnityPath -projectPath $taskRoot
    exit
}
$taskResults = Join-Path $taskRoot 'TestResults'
New-Item -ItemType Directory -Path $taskResults -Force | Out-Null
if ($Task -eq 'Test') {
    & $UnityPath -batchmode -projectPath $taskRoot -runTests -testPlatform PlayMode -testResults (Join-Path $taskResults 'playmode.xml') -logFile (Join-Path $taskResults 'playmode.log') | Out-Null
} else {
    & $UnityPath -batchmode -quit -projectPath $taskRoot -executeMethod MidLane.Editor.ProjectSetup.BuildWindows -logFile (Join-Path $taskResults 'build.log') | Out-Null
}
# Unity.exe is a GUI executable: piping to Out-Null makes PowerShell wait for exit.
if ($LASTEXITCODE -ne 0) { throw "Unity terminó con error. Revisá $taskResults" }
