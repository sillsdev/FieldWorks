@echo off
echo Recreating Lela-Teli 2 database...
REM This will fail if you haven't run InitMsde.bat once after MSDE installation.
pushd
cd ..\bld
..\bin\nant\bin\nant LelaTeli2-nodep
popd
echo ************ Done! ****************
