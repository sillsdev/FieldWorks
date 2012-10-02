<?xml version="1.0" encoding="UTF-8" ?>
<!-- This file is a placeholder to illustrate how the migration scripts are named, -->
<!-- and how the version number is updated. -->
<!-- Since version 1.1.1 is the first one to be released, this should never be used (except in tests)! -->
<xsl:stylesheet version="2.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	xmlns:o="http://www.wycliffe.net/scripture/namespace/version_1.1.0">
	<xsl:output method="xml" version="1.0" encoding="UTF-8" indent="yes"/>
	<xsl:preserve-space elements="tr bt q otPassage emphasis nameOfGod wordsOfJesus"/><!-- all elements corresponding to char styles? -->

	<!-- update the places where the version number appears -->

	<!-- @version doesn't exist, but might instead of the version in the xmlns value-->

	<xsl:template match="o:oxes/@version">
		<xsl:attribute name="version">1.1.1</xsl:attribute>
	</xsl:template>

	<xsl:template match="o:oxesText/@type">
		<xsl:variable name="val" select="."/>
		<xsl:variable name="ver">1.1.1</xsl:variable>
		<xsl:attribute name="type">
			<xsl:value-of select="substring-before($val, '1.1.0')"/>
			<xsl:value-of select="$ver"/>
		</xsl:attribute>
	</xsl:template>

	<!-- This is the basic default processing.  It updates the namespace along the way.-->

	<xsl:template match="*">
		<xsl:element name="{local-name()}"
			namespace="http://www.wycliffe.net/scripture/namespace/version_1.1.1">
			<xsl:apply-templates select="@*|*|text()"/>
		</xsl:element>
	</xsl:template>
	<xsl:template match="@*">
		<xsl:copy/>
	</xsl:template>
	<xsl:template match="text()">
		<xsl:copy/>
	</xsl:template>
</xsl:stylesheet>
