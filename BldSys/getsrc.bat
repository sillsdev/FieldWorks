rem %1 is the target drive, %2 is the target directory where source should go. It should
rem already exist.

rem Change to the target drive
%1:

rem ********************************************************************************************
rem Delete the existing files and recreate the directory.
rem ********************************************************************************************
cd\
if exist %2 rmdir /s /q %2
mkdir %2
cd %2

rem ********************************************************************************************
rem Get code from Visual Source Safe.
rem ********************************************************************************************
SS.exe cp $/
SS.exe get * -R -I-Y

cd ..
