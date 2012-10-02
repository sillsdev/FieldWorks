REM --( This reverts selected XSL files back to MagicDraw.
REM --( This .bat expects to be run out of C:\Program Files\MagicDraw UML 5.5i\data\report\xsl_browse

RENAME common\Keys.xsl KeysFW.xsl
RENAME common\KeysMagic.xsl Keys.xsl

RENAME common\ShowDocumentation.xsl ShowDocumentationFW.xsl
RENAME common\ShowDocumentationMagic.xsl ShowDocumentation.xsl

RENAME reports\ClassReport.xsl ClassReportFW.xsl
RENAME reports\ClassReportMagic.xsl ClassReport.xsl