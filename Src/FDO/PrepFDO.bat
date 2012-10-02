REM Wrap Fieldworks dlls as .Net assemblies
REM And build the base DLLs, if needed
call %FWROOT%\bin\mkallCommon.bat %1 %2 %3 %4 %5 %6 %7 %8 %9

REM Generate the source code for classes
REM Fluffs up the zipped xml file
%FWROOT%\bin\FDOGenerate.bat
