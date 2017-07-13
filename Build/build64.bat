setlocal

REM set an environment variable that we use in build.bat to set the correct visual studio environment
set arch=x64
REM call build.bat passing it the x64 platform
build.bat /p:Platform=x64 %*