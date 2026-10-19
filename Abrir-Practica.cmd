@echo off
setlocal
set "PRACTICE_UNITY=%ProgramFiles%\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe"
if not exist "%PRACTICE_UNITY%" (
  echo Instala Unity 6000.6.5f1 o abre esta carpeta desde Unity Hub.
  pause
  exit /b 1
)
start "" "%PRACTICE_UNITY%" -projectPath "%~dp0."
