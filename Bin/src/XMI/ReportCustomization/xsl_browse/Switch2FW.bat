REM --( This installs the XSL files into MagicDraw so the FW doc comes out as expected.
REM --( This .bat expects to be run out of C:\Program Files\MagicDraw UML 5.5i\data\report\xsl_browse

RENAME common\Keys.xsl KeysMagic.xsl
RENAME common\KeysFW.xsl Keys.xsl

RENAME common\ShowDocumentation.xsl ShowDocumentationMagic.xsl
RENAME common\ShowDocumentationFW.xsl ShowDocumentation.xsl

RENAME reports\ClassReport.xsl ClassReportMagic.xsl
RENAME reports\ClassReportFW.xsl ClassReport.xsl