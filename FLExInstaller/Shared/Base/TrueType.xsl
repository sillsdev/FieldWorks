<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="xml" version="1.0" encoding="UTF-8" indent="yes" />

	<xsl:template match="@*|node()">
		<xsl:copy>
			<xsl:apply-templates select="@*|node()" />
		</xsl:copy>
	</xsl:template>

	<!-- Insert TrueType="yes" after KeyPath="yes" (an attribute heat.exe adds to all File elements).
		This allows us to harvest fonts and install them so that Windows recognises them.
		A more principled approach was attempted, but heat's xsl is limited. -->
	<xsl:template match="@KeyPath[.='yes']">
		<xsl:attribute name="KeyPath">
			<xsl:value-of select="'yes'"/>
		</xsl:attribute>
		<xsl:attribute name="TrueType">
			<xsl:value-of select="'yes'"/>
		</xsl:attribute>
	</xsl:template>
</xsl:stylesheet>