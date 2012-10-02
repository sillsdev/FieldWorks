<?xml version="1.0" encoding="UTF-8"?>
<!--This stylesheet adds information to LexicalDatabase/sub and LexicalDatabase/minor nodes so that in the HTML generation
they can display their major entry parents as a cross-reference.-->
<xsl:stylesheet version="1.0" xmlns:xhtml="http://www.w3.org/1999/xhtml" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" exclude-result-prefixes="#default">
	<xsl:output method="xml" version="1.0" encoding="UTF-8" omit-xml-declaration="yes"/>
	<xsl:template match="/">
		<xsl:apply-templates/>
	</xsl:template>
	<xsl:template match="xhtml:frame[@name='mainFrame']">
		<frame>
			<xsl:attribute name="src">
				<xsl:text>fieldworks_data_model.htm</xsl:text>
			</xsl:attribute>
			<xsl:apply-templates select="@scrolling | @name"/>
		</frame>
	</xsl:template>
	<xsl:template match="xhtml:frame[@name='browserBottom']">
		<frame>
			<xsl:attribute name="src">
				<xsl:text>browserBottom.htm</xsl:text>
			</xsl:attribute>
			<xsl:apply-templates select="@scrolling | @name"/>
		</frame>
	</xsl:template>
	<xsl:template match="@* | node()">
		<xsl:copy>
			<xsl:apply-templates select="@* | node()"/>
		</xsl:copy>
	</xsl:template>
</xsl:stylesheet>
