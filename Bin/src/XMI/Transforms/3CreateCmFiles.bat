echo.
echo Setting read-only attribute on current .cm files to false
echo (regardless of whether or not you have them checked out)...
echo.
attrib -r ..\..\..\..\src\cellar\xml\cellar.cm
attrib -r ..\..\..\..\src\featsys\xml\featsys.cm
attrib -r ..\..\..\..\src\langproj\xml\langproj.cm
attrib -r ..\..\..\..\src\ling\xml\ling.cm
attrib -r ..\..\..\..\src\notebk\xml\notebk.cm
attrib -r ..\..\..\..\src\scripture\xml\scripture.cm
echo.
echo Creating separate .cmt (temporary) files from temporary xmi2cellar3.xml...
echo.
echo Generating cellar.cmt
%fwroot%\Bin\msxsl XMITempOutputs\xmi2cellar3.xml CreateCmFilesCellar.xsl -xe -o XMITempOutputs\cellar.cmt
echo Generating featsys.cmt
%fwroot%\Bin\msxsl XMITempOutputs\xmi2cellar3.xml CreateCmFilesFeatSys.xsl -xe -o XMITempOutputs\featsys.cmt
echo Generating langproj.cmt
%fwroot%\Bin\msxsl XMITempOutputs\xmi2cellar3.xml CreateCmFilesLangProj.xsl -xe -o XMITempOutputs\langproj.cmt
echo Generating ling.cmt
%fwroot%\Bin\msxsl XMITempOutputs\xmi2cellar3.xml CreateCmFilesLing.xsl -xe -o XMITempOutputs\ling.cmt
echo Generating notebk.cmt
%fwroot%\Bin\msxsl XMITempOutputs\xmi2cellar3.xml CreateCmFilesNotebk.xsl -xe -o XMITempOutputs\notebk.cmt
echo Generating scripture.cmt
%fwroot%\Bin\msxsl XMITempOutputs\xmi2cellar3.xml CreateCmFilesScripture.xsl -xe -o XMITempOutputs\scripture.cmt
echo.
echo.
echo Transforming and sorting each .cm file's classes and attributes...
echo.
echo Generating cellar.cm
%fwroot%\Bin\msxsl XMITempOutputs\cellar.cmt CreateSortCms.xsl -xe -o ..\..\..\..\src\cellar\xml\cellar.cm
echo Generating featsys.cm
%fwroot%\Bin\msxsl XMITempOutputs\featsys.cmt CreateSortCms.xsl -xe -o ..\..\..\..\src\featsys\xml\featsys.cm
echo Generating langproj.cm
%fwroot%\Bin\msxsl XMITempOutputs\langproj.cmt CreateSortCms.xsl -xe -o ..\..\..\..\src\langproj\xml\langproj.cm
echo Generating ling.cm
%fwroot%\Bin\msxsl XMITempOutputs\ling.cmt CreateSortCms.xsl -xe -o ..\..\..\..\src\ling\xml\ling.cm
echo Generating notebk.cm
%fwroot%\Bin\msxsl XMITempOutputs\notebk.cmt CreateSortCms.xsl -xe -o ..\..\..\..\src\notebk\xml\notebk.cm
echo Generating scripture.cm
%fwroot%\Bin\msxsl XMITempOutputs\scripture.cmt CreateSortCms.xsl -xe -o ..\..\..\..\src\scripture\xml\scripture.cm
