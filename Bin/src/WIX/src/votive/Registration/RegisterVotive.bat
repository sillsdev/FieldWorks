::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
:: RegisterVotive.bat
:: -------------------
:: Preprocesses all of the various support files for working with Votive in a development
:: environment and then registers Votive with Visual Studio 2003 and 2005.
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

@echo off
setlocal

:: GUIDs
set PACKAGE_GUID={B0AB1F0F-7B08-47FD-8E7C-A5C0EC855568}
set PROJECT_GUID={A49CE20D-CE64-4A08-9F24-92A6443D6699}
set XML_EDITOR_GUID_2003={C76D83F8-A489-11D0-8195-00A0C91BBEE3}
set XML_EDITOR_GUID_2005={fa3cd31e-987b-443a-9b81-186104e8dac1}

:: Toools declarations
set VOTIVE_PREPROCESSOR=%~dp0..\..\..\Release\debug\VotivePP.exe

:: Version flags
set REGISTER_2003=1
set REGISTER_2005=1
set DEVENVPATH_2003=
set DEVENVPATH_2005=

:: See which versions of the experimental hive are registered.
reg query HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\7.1Exp\Setup\VS /v EnvironmentPath > NUL 2> NUL
if not %errorlevel% == 0 (
	echo VSIP SDK for Visual Studio 2003 is not installed. Skipping registration for VS 2003.
	set REGISTER_2003=0
)
reg query HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\8.0Exp\Setup\VS /v EnvironmentPath > NUL 2> NUL
if not %errorlevel% == 0 (
	echo VS SDK for Visual Studio 2005 is not installed. Skipping registration for VS 2005.
	set REGISTER_2005=0
)

:: Try to get the paths to the Visual Studio devenv.exe
if %REGISTER_2003%==1 (
	for /f "tokens=3*" %%a in ('reg query HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\7.1Exp\Setup\VS /v EnvironmentPath ^| find "EnvironmentPath"') do set DEVENVPATH_2003=%%a %%b
)
if %REGISTER_2005%==1 (
	for /f "tokens=3*" %%a in ('reg query HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\8.0Exp\Setup\VS /v EnvironmentPath ^| find "EnvironmentPath"') do set DEVENVPATH_2005=%%a %%b
)

:: Directories
set SCONCE_TARGETDIR=%~dp0..\..\sconce\bin\Debug\
set SCONCE_TARGETPATH=%SCONCE_TARGETDIR%sconce.dll
set VOTIVE_TARGETDIR=%~dp0..\Core\bin\Debug\
set VOTIVE_TARGETPATH=%VOTIVE_TARGETDIR%votive.dll
set WIXTOOLSDIR=%~dp0..\..\..\Release\debug\
set TEMPLATESDIR=%~dp0..\Templates\

:: Write the .reg files and use reg.exe to import the files
set REG_FILE=%~dp0Register.reg
if %REGISTER_2003%==1 (
	echo Writing package registry settings for Visual Studio 2003...
	"%VOTIVE_PREPROCESSOR%" -bs "%~dp0Register.reg.pp" "%REG_FILE%" DLLPATH="%VOTIVE_TARGETPATH%" DLLDIR="%VOTIVE_TARGETDIR%\" SCONCEPATH="%SCONCE_TARGETPATH%" TEMPLATESDIR="%TEMPLATESDIR%\" WIXTOOLSDIR="%WIXTOOLSDIR%\" DEVENVPATH="%DEVENVPATH_2003%" PACKAGE_GUID=%PACKAGE_GUID% PROJECT_GUID=%PROJECT_GUID% XML_EDITOR_GUID=%XML_EDITOR_GUID_2003% VS_VERSION=7.1 VS_VERSION_YEAR=2003
	reg import "%REG_FILE%"
	if exist "%REG_FILE%" del /q /f "%REG_FILE%"
	echo Registering Votive with Visual Studio 2003 Exp...
	echo "%DEVENVPATH_2003%" /setup /rootsuffix Exp
	"%DEVENVPATH_2003%" /setup /rootsuffix Exp
	echo.
)
if %REGISTER_2005%==1 (
	echo Writing package registry settings for Visual Studio 2005...
	"%VOTIVE_PREPROCESSOR%" -bs "%~dp0Register.reg.pp" "%REG_FILE%" DLLPATH="%VOTIVE_TARGETPATH%" DLLDIR="%VOTIVE_TARGETDIR%\" SCONCEPATH="%SCONCE_TARGETPATH%" TEMPLATESDIR="%TEMPLATESDIR%\" WIXTOOLSDIR="%WIXTOOLSDIR%\" DEVENVPATH="%DEVENVPATH_2005%" PACKAGE_GUID=%PACKAGE_GUID% PROJECT_GUID=%PROJECT_GUID% XML_EDITOR_GUID=%XML_EDITOR_GUID_2005% VS_VERSION=8.0 VS_VERSION_YEAR=2005
	reg import "%REG_FILE%"
	if exist "%REG_FILE%" del /q /f "%REG_FILE%"
	echo Registering Votive with Visual Studio 2005 Exp...
	echo "%DEVENVPATH_2005%" /setup /rootsuffix Exp
	"%DEVENVPATH_2005%" /setup /rootsuffix Exp
	echo.
)

:End
endlocal
exit /b 0
