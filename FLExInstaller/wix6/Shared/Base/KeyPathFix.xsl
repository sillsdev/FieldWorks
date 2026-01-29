<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:my="http://schema.infor.com/InforOAGIS/2">
	<xsl:output method="xml" version="1.0" encoding="UTF-8" indent="yes" />

	<xsl:template match="@*|node()">
		<xsl:copy>
			<xsl:apply-templates select="@*|node()"/>
		</xsl:copy>
	</xsl:template>

	<!-- Change KeyPath="yes" to KeyPath="no" in files harvested by heat.exe.
		This allows a later base installer to overwrite the higher versioned file with a lower versioned file.
		Patch files do NOT have this capability; this hack results in an error during the patch creation process -->
	<xsl:template match="@KeyPath[.='yes']">
		<xsl:attribute name="KeyPath">
			<xsl:value-of select="'no'"/>
		</xsl:attribute>
	</xsl:template>
</xsl:stylesheet>