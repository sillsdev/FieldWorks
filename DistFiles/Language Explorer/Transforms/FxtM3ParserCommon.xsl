<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
<!-- Items common to XAmple and HermitCrab -->

	<xsl:key name="LexEntryInflTypeID" match="LexEntryInflType" use="@Id"/>
	<xsl:key name="PrefixSlotsID" match="/M3Dump/PartsOfSpeech/PartOfSpeech/AffixTemplates/MoInflAffixTemplate/PrefixSlots" use="@Id"/>

	<xsl:variable name="LexEntryInflTypeSlots" select="/M3Dump/LexEntryInflTypes/LexEntryInflType/Slots"/>

	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		GlossAddition
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="GlossAddition">
		<xsl:param name="addition"/>
		<xsl:choose>
			<xsl:when test="$addition='***'">
				<!-- output nothing -->
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="$addition"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		GlossOfIrregularlyInflectedForm
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="GlossOfIrregularlyInflectedForm">
		<xsl:param name="lexEntryRef"/>
		<xsl:param name="sVariantOfGloss"/>
		<xsl:for-each select="$lexEntryRef/LexEntryInflType">
			<xsl:variable name="lexEnryInflType" select="key('LexEntryInflTypeID',@dst)"/>
			<xsl:call-template name="GlossAddition">
				<xsl:with-param name="addition" select="$lexEnryInflType/GlossPrepend"/>
			</xsl:call-template>
		</xsl:for-each>
		<xsl:value-of select="$sVariantOfGloss"/>
		<xsl:for-each select="$lexEntryRef/LexEntryInflType">
			<xsl:variable name="lexEnryInflType" select="key('LexEntryInflTypeID',@dst)"/>
			<xsl:call-template name="GlossAddition">
				<xsl:with-param name="addition" select="$lexEnryInflType/GlossAppend"/>
			</xsl:call-template>
		</xsl:for-each>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		IdOfIrregularlyInflectedFormEntry
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="IdOfIrregularlyInflectedFormEntry">
		<xsl:param name="lexEntry"/>
		<xsl:param name="lexEntryRef"/>
		<xsl:value-of select="$lexEntry/@Id"/>
		<xsl:variable name="iPos" select="count($lexEntryRef/preceding-sibling::LexEntryRef)"/>
		<xsl:if test="$iPos &gt; 0">
			<xsl:text>.</xsl:text>
			<xsl:value-of select="$iPos"/>
		</xsl:if>
	</xsl:template>

</xsl:stylesheet>
