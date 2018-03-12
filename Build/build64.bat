setlocal

REM set an environment variable that we use in build.bat to set the correct visual studio environment
set arch=x64
REM the corresponding x64 SDK Tools directory does not provide resgen.exe, which is needed for localization.
set PATH=C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools;%PATH%
REM call build.bat passing it the x64 platform
build.bat /p:Platform=x64 %*