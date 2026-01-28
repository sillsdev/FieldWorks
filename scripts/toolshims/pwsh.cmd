@echo off
:: Shim for `pwsh` - prefer PowerShell Core, fall back to Windows PowerShell
where pwsh >nul 2>&1
if %errorlevel%==0 (
  pwsh %*
  exit /b %errorlevel%
)
where powershell >nul 2>&1
if %errorlevel%==0 (
  powershell %*
  exit /b %errorlevel%
)


exit /b 1echo "No PowerShell (pwsh or powershell) found on PATH."