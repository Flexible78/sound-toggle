@echo off
setlocal
if not exist "%~dp0SoundToggle.exe" (
    echo SoundToggle.exe is missing. Extract all files from the ZIP first.
    pause
    exit /b 1
)
start "" /wait "%~dp0SoundToggle.exe" --install
if errorlevel 1 (
    echo Installation failed. See %%LOCALAPPDATA%%\SoundToggle\last-run.log.
    pause
    exit /b 1
)
exit /b 0