<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
	<!-- This stylesheet contains the common components for XLingPap output  -->
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		CommonTypes
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="CommonTypes">
		<type id="tHomographNumber" font-size="65%" cssSpecial="vertical-align:sub" xsl-foSpecial="baseline-shift='sub'"/>
		<type id="tVariantTypes" >
			<xsl:variable name="analysisLanguage" select="//language[not(@vernacular='true')][1]"/>
			<xsl:if test="$analysisLanguage">
				<xsl:attribute name="font-family">
					<xsl:value-of select="$analysisLanguage/@font"/>
				</xsl:attribute>
			</xsl:if>
		</type>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		GetMorphTypeFromGuid
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="GetMorphTypeFromGuid">
		<xsl:param name="sGuid"/>
		<xsl:attribute name="type">
		<xsl:choose>
			<xsl:when test="$sGuid=$sBoundRoot">
				<xsl:text>boundroot</xsl:text>
			</xsl:when>
			<xsl:when test="$sGuid=$sBoundStem">
				<xsl:text>boundstem</xsl:text>
			</xsl:when>
			<xsl:when test="$sGuid=$sRoot">
				<xsl:text>root</xsl:text>
			</xsl:when>
			<xsl:when test="$sGuid=$sStem">
				<xsl:text>stem</xsl:text>
			</xsl:when>
			<xsl:when test="$sGuid=$sCircumfix">
				<xsl:text>circumfix</xsl:text>
			</xsl:when>
			<xsl:when test="$sGuid=$sClitic">
				<xsl:text>clitic</xsl:text>
			</xsl:when>
			<xsl:when test="$sGuid=$sDiscontiguousPhrase">
				<xsl:text>discontiguousphrase</xsl:text>
			</xsl:when>
			<xsl:when test="$sGuid=$sEnclitic">
				<xsl:text>enclitic</xsl:text>
			</xsl:when>
			<xsl:when test="$sGuid=$sInfix or $sGuid=$sInfixingInterfix">
				<xsl:text>infix</xsl:text>
			</xsl:when>
			<xsl:when test="$sGuid=$sParticle">
				<xsl:text>particle</xsl:text>
			</xsl:when>
			<xsl:when test="$sGuid=$sPhrase">
				<xsl:text>phrase</xsl:text>
			</xsl:when>
			<xsl:when test="$sGuid=$sPrefix or $sGuid=$sPrefixingInterfix">
				<xsl:text>prefix</xsl:text>
			</xsl:when>
			<xsl:when test="$sGuid=$sProclitic">
				<xsl:text>proclitic</xsl:text>
			</xsl:when>
			<xsl:when test="$sGuid=$sSuffix or $sGuid=$sSuffixingInterfix">
				<xsl:text>suffix</xsl:text>
			</xsl:when>
			<xsl:otherwise>
				<xsl:text>UnknownType</xsl:text>
			</xsl:otherwise>
		</xsl:choose>
		</xsl:attribute>
	</xsl:template>
	<xsl:include href="MorphTypeGuids.xsl"/>
</xsl:stylesheet>
