<?xml version="1.0" encoding="UTF-8"?>
<!--This transform should be applied to the results of the
MagicToCellarStage2.xsl transform (bin\src\xmi\transforms\xmiTempOuts\xmi2cellar2.xml.)
Change TextPropBinary to Binary for *.cm purposes. FDO will use xmi2cellar2.xml.-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="xml" version="1.0" encoding="UTF-8"/>
	<xsl:template match="@sig[.='TextPropBinary']">
		<xsl:attribute name="sig">
			<xsl:text>Binary</xsl:text>
		</xsl:attribute>
	</xsl:template>
	<xsl:template match="/ | @* | node()">
		<xsl:copy>
			<xsl:apply-templates select="@* | node()"/>
		</xsl:copy>
	</xsl:template>
</xsl:stylesheet>
