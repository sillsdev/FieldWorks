::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
:: make.bat
:: --------
:: Builds the WiX toolset using NAnt.
::
:: Copyright (c) Microsoft Corporation.  All rights reserved.
::
:: The use and distribution terms for this software are covered by the
:: Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
:: which can be found in the file CPL.TXT at the root of this distribution.
:: By using this software in any fashion, you are agreeing to be bound by
:: the terms of this license.
::
:: You must not remove this notice, or any other, from this software.
::
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
::
:: In order to fully build WiX, you must have the following Frameworks and
:: SDKs installed:
::    * NAnt version 0.85 rc3 or higher
::    * .NET Framework 1.1 and SDK
::    * .NET Framework 2.0 (SDK is optional)
::    * PlatformSDK (version 3790.1830 or higher)
::    *   Core SDK
::    *   Web Workshop (IE) SDK
::    *   Internet Information Server (IIS) SDK
::    *   Microsoft Data Access Services (MDAC) SDK
::    *   Microsoft Windows Installer SDK
::    * One of the following Visual Studio 2005 Editions:
::    *   Visual C++ Express Edition
::    *   Professional or higher with Visual C++ installed
::    *   (To install Votive on Visual Studio 2005, you must have the Standard
::        edition or higher)
::    * HTML Help SDK 1.4 or higher
::
:: To build Sconce and Votive, you must have the following SDKs installed:
::    * Visual Studio Partner Integration Program (VSIP) SDK 2003
::    * VSIP SDK 2003 Extras
::      Both are available at http://msdn.microsoft.com/vstudio/partners/default.aspx
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

@echo off
setlocal

:: Cache some environment variables.
set ROOT=%~dp0

:: Set the default arguments
set FLAVOR=debug
set ACTION=
set VERBOSE=

:ParseArgs
:: Parse the incoming arguments
if /i "%1"==""      goto Build
if /i "%1"=="-?"    goto Syntax
if /i "%1"=="-h"    goto Syntax
if /i "%1"=="-help" goto Syntax
if /i "%1"=="-v"    (set VERBOSE=-v)   & shift & goto ParseArgs
if /i "%1"=="debug" (set FLAVOR=debug) & shift & goto ParseArgs
if /i "%1"=="ship"  (set FLAVOR=ship)  & shift & goto ParseArgs
if /i "%1"=="build" (set ACTION=build) & shift & goto ParseArgs
if /i "%1"=="full"  (set ACTION=build) & shift & goto ParseArgs
if /i "%1"=="clean" (set ACTION=clean) & shift & goto ParseArgs
if /i "%1"=="inc"   (set ACTION=)      & shift & goto ParseArgs
goto Error

:Build
pushd %ROOT%

nant.exe -buildfile:"%ROOT%\wix.build" %ACTION% -D:dir.root=%ROOT% -D:flavor=%FLAVOR% %VERBOSE%

popd
goto End

:Error
echo Invalid command line parameter: %1
echo.

:Syntax
echo %~nx0 [-?] [-v] [debug or ship] [clean or build or full or inc]
echo.
echo   -?    : this help
echo   -v    : verbose messages
echo   debug : builds a debug version of the WiX toolset (default)
echo   ship  : builds a release (ship) version of the WiX toolset
echo   clean : cleans the build
echo   build : does a full rebuild
echo   full  : same as build
echo   inc   : does an incremental rebuild (default)
echo.
echo.

:End
endlocal