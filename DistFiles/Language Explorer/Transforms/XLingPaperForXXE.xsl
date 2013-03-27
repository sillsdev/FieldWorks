<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
	<xsl:output method="xml" doctype-public="-//XMLmind//DTD XLingPap//EN"  doctype-system="XLingPap.dtd"/>
	<xsl:template match="@type[not(parent::object or parent::ol or parent::ul)]">
	<!-- remove incorrect type attribute -->
</xsl:template>
	<xsl:template match="@* | node()">
	<xsl:copy>
		<xsl:apply-templates select="@*"/>
		<xsl:apply-templates/>
	</xsl:copy>
	</xsl:template>
	<xsl:template match="xml-stylesheet">
		<!-- remove this -->
	</xsl:template>
</xsl:stylesheet>
