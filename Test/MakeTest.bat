@echo off
echo Recreating TestLangProj database...
REM This will fail if you haven't run InitMsde.bat once after MSDE installation.
pushd
cd ..\bld
..\bin\nant\bin\nant TestLangProj-nodep
popd
echo ************ Done! ****************