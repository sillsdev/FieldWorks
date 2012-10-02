REM This batch files creates replacement html help files
REM from the MagicDraw FieldWorks.xml file
REM on the machine of the FieldWorks Model maintainer.
call ..\..\..\_EnsureRoot.bat
REM %fwroot%\Bin\saxon\saxon.exe %fwroot%\Bin\src\XMI\FieldWorks.xml %fwroot%\Bin\src\XMI\Transforms\CreateReplaceHelp.xsl
set classpath=%fwroot%\Bin\saxon\saxon.jar
java com.icl.saxon.StyleSheet %fwroot%\Bin\src\XMI\FieldWorks.xml %fwroot%\Bin\src\XMI\Transforms\CreateReplaceHelp.xsl
copy /Y %fwroot%\Bin\src\XMI\ReportCustomization\ReplaceHelpExtraFiles\*.* %fwroot%"\Bin\src\XMI\ReportCustomization\chm generation"
copy /Y %fwroot%\Bin\src\XMI\Transforms\XMITempOutputs\ReplaceHelp\*.* %fwroot%"\Bin\src\XMI\ReportCustomization\chm generation"
pause
