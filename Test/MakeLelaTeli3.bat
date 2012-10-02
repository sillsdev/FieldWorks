@echo off
echo Recreating Lela-Teli 3 database...
REM This will fail if you haven't run InitMsde.bat once after MSDE installation.
pushd
cd ..\bld
..\bin\nant\bin\nant LelaTeli3-nodep
popd
echo ************ Done! ****************
