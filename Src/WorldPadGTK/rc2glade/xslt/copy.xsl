<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'>

<xsl:template match="/*">
	<!-- Avoidance of <xsl:copy> prevents automatic copying of namespace nodes -->
	<xsl:element name="{name()}">
		<xsl:apply-templates select="@*|node()" />
	</xsl:element>
</xsl:template>

<xsl:template match="node()|@*">
	<xsl:copy>
		<xsl:apply-templates select="@*|node()" />
	</xsl:copy>
</xsl:template>

</xsl:stylesheet>
