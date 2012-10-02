@echo off

REM   This batch program builds the Encoding Converters installer.

REM   Then the Wise source file (.wsi) is copied to the BuildSpace subfolder, compiled,
REM   and the resulting files moved to the original folder. The compile log is renamed to
REM   reflect the source file name.

REM   This program is designed to be run from any directory.

REM   Note on batch file parameters:
REM   Check out http://www.microsoft.com/resources/documentation/windows/xp/all/proddocs/en-us/percent.mspx
REM   Paramters used in this file:
REM   %~p0  - The directory path of this file.

echo Copying EncodingConverters.wsi to BuildSpace
copy "%~p0EncodingConverters.wsi" "%~p0BuildSpace"
rem remove read-only attribute:
attrib -R "%~p0BuildSpace\EncodingConverters.wsi"
echo Compiling SIL Converters and Repository Installer
"C:\Program Files\Wise for Windows Installer\Wfwi.exe" "%~p0BuildSpace\EncodingConverters.wsi" /c /s
type "%~p0BuildSpace\compile.log"
copy "%~p0BuildSpace\compile.log" "%~p0BuildSpace\compile [EncodingConverters].log"
echo Moving compiled EncodingConverters.msi, .exe and .cab files back to original folder
move "%~p0BuildSpace\*.msi" "%~p0"
move "%~p0BuildSpace\*.cab" "%~p0"
move "%~p0BuildSpace\*.exe" "%~p0"
echo Deleting temp copy of EncodingConverters.wsi
del "%~p0BuildSpace\EncodingConverters.wsi"
