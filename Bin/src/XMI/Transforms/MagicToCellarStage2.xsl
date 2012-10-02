<?xml version="1.0" encoding="UTF-8"?>
<!--This transform should be applied to the results of the
MagicToCellarStage1.xsl transform (bin\src\xmi\transforms\xmiTempOuts\xmi2cellar1.xml.)
Below will order the superclasses before the subclasses so that the code generator
can build classes in the correct order.-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="xml" version="1.0" encoding="UTF-8"/>
	<xsl:template match="/">
		<EntireModel>
		<xsl:apply-templates/>
		</EntireModel>
	</xsl:template>
	<xsl:template match="CellarModule">
		<CellarModule>
			<xsl:copy-of select="@*"/>
			<xsl:text>&#xA;</xsl:text><xsl:for-each select="class">
				<xsl:sort select="@depth" order="ascending"/>
				<xsl:copy>
					<!--copy node being visited-->
					<xsl:copy-of select="@*"/>
					<!--copy of all attributes-->
					<xsl:text>&#xA;</xsl:text><props><xsl:text>&#xA;</xsl:text><xsl:apply-templates/></props><xsl:text>&#xA;</xsl:text>
					<!--process the children-->
				</xsl:copy><xsl:text>&#xA;</xsl:text>
				<!-- process the children of form element in source tree -->
				<!--<xsl:apply-templates/>-->
			</xsl:for-each>
		</CellarModule><xsl:text>&#xA;</xsl:text>
	</xsl:template>
	<xsl:template match="class/props/*">
		<xsl:copy>
			<!--copy node being visited-->
			<xsl:copy-of select="@*"/>
			<!--copy of all attributes-->
			<xsl:apply-templates/>
			<!--process the children-->
		</xsl:copy><xsl:text>&#xA;</xsl:text>
	</xsl:template>
</xsl:stylesheet>
