@if "%_echo%"=="" echo off
if "%1"=="" goto usage
rem ************************ Check for the standard system databases. ************************
if "%1"=="master" goto badjoke
if "%1"=="MASTER" goto badjoke
if "%1"=="Master" goto badjoke
if "%1"=="model" goto badjoke
if "%1"=="MODEL" goto badjoke
if "%1"=="Model" goto badjoke
if "%1"=="msdb" goto badjoke
if "%1"=="MSDB" goto badjoke
if "%1"=="Msdb" goto badjoke
if "%1"=="tempdb" goto badjoke
if "%1"=="TEMPDB" goto badjoke
if "%1"=="Tempdb" goto badjoke
if "%1"=="northwind" goto badjoke
if "%1"=="NORTHWIND" goto badjoke
if "%1"=="Northwind" goto badjoke
if "%1"=="pubs" goto badjoke
if "%1"=="PUBS" goto badjoke
if "%1"=="Pubs" goto badjoke

osql /U sa /E /n /b /Q "USE %1" >nul
if errorlevel 1 goto nosuch

@if "%_echo%"=="" echo on
osql /U sa /E /n /b /Q "DROP DATABASE %1" >nul
@if "%_echo%"=="" echo off
goto done

:nosuch
echo Database "%1" does not exist!
goto done

:badjoke
echo Do not delete standard system databases!
goto done

:usage
echo Usage: DeleteDb.bat DatabaseName
:done
