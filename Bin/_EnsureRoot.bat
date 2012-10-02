if not "%FWROOT%"=="" goto LSetBuildRoot

if exist "%0\..\_setroot.bat" goto LCall

@echo Creating _setroot.bat will fail if this build was kicked off from within
@echo VisualStudio. You may need to edit _setroot.bat manually or create it by
@echo deleting it and running _EnsureRoot.bat from the command prompt.
%0\..\here.exe ".." "set FWROOT=" > %0\..\_setroot.bat

:LCall
call %0\..\_setroot.bat

:LSetBuildRoot
set BUILD_ROOT=%FWROOT%

rem ***** set BUILD_SRC if not already set (BUILD_SRC is used for .NET projects)
if "%BUILD_SRC%"=="" set BUILD_SRC=%BUILD_ROOT%\Src
