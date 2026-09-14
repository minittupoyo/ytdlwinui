@echo off
setlocal

where powershell.exe >nul 2>&1
if errorlevel 1 (
  echo [ERROR] Windows PowerShell could not be found.
  if not defined YTDLGUI_INSTALLER_NO_PAUSE pause
  exit /b 1
)

echo Installing yt-dlp, Deno, FFmpeg, and ffprobe...
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-Tools.ps1" %*
set "INSTALL_EXIT_CODE=%ERRORLEVEL%"

if not "%INSTALL_EXIT_CODE%"=="0" (
  echo.
  echo [ERROR] Tool installation failed. See the message above.
  if not defined YTDLGUI_INSTALLER_NO_PAUSE pause
  exit /b %INSTALL_EXIT_CODE%
)

echo.
echo Installation completed successfully.
if not defined YTDLGUI_INSTALLER_NO_PAUSE pause
exit /b 0
