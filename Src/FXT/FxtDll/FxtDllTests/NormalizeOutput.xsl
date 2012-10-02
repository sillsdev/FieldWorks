<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="xml" version="1.0" encoding="UTF-8" indent="yes"/>
	<xsl:template match="/">
		<xsl:apply-templates>
			<xsl:sort select="@Id"/>
			<xsl:sort select="name(.)"/>
			<xsl:sort select="@ord"/>
			<xsl:sort select="@dst"/>
		</xsl:apply-templates>
	</xsl:template>
	<xsl:template match="*">
		<xsl:copy>
		<xsl:copy-of select="@*"/>
		<xsl:apply-templates>
			<xsl:sort select="@Id"/>
			<xsl:sort select="name(.)"/>
			<xsl:sort select="@ord"/>
			<xsl:sort select="@dst"/>
		</xsl:apply-templates>
		</xsl:copy>
	</xsl:template>
</xsl:stylesheet>
