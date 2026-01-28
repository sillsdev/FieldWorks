@echo off
REM Delegate to PowerShell wrapper which handles heredocs and fallbacks.
setlocal
set PSWRAPPER=%~dp0py.ps1

where powershell >nul 2>&1
if %errorlevel%==0 (
  powershell -NoProfile -ExecutionPolicy Bypass -File "%PSWRAPPER%" -- %*
  exit /b %errorlevel%
)

where pwsh >nul 2>&1
if %errorlevel%==0 (
  pwsh -NoProfile -ExecutionPolicy Bypass -File "%PSWRAPPER%" -- %*
  exit /b %errorlevel%
)

echo "No PowerShell runtime found to run py wrapper. Install PowerShell or run python directly."
exit /b 1