REM Creates an XDR schema file for FieldWorks data model to be used with SQLServer 2000.
REM Stage1 creates the elements and attribute types.
REM Stage2 is a hack because I didn't do stage1 right. It fixes the stage1 problem.
REM Larry Hayashi April 27, 2001.
%fwroot%\Bin\msxsl ..\XMITempOutputs\xmi2cellar3.xml sqlxdr.xsl -o ..\XMITempOutputs\sqlxdr.xdr
copy ..\XMITempOutputs\sqlxdr.xdr
REM Modifed Andy Black 03 October, 2001
