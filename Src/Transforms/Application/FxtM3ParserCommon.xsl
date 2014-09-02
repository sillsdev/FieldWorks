<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
	<!-- Items common to XAmple and HermitCrab -->

	<xsl:key name="LexEntryInflTypeID" match="LexEntryInflType" use="@Id"/>
	<xsl:key name="PrefixSlotsID" match="/M3Dump/PartsOfSpeech/PartOfSpeech/AffixTemplates/MoInflAffixTemplate/PrefixSlots" use="@dst"/>

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
	<xsl:template name="GlossOfVariant">
		<xsl:param name="lexEntryRef"/>
		<xsl:param name="sVariantOfGloss"/>
		<xsl:for-each select="$lexEntryRef/LexEntryType">
			<xsl:variable name="lexEntryInflType" select="key('LexEntryInflTypeID',@dst)"/>
			<xsl:if test="$lexEntryInflType">
				<xsl:call-template name="GlossAddition">
					<xsl:with-param name="addition" select="$lexEntryInflType/GlossPrepend"/>
				</xsl:call-template>
			</xsl:if>
		</xsl:for-each>
		<xsl:value-of select="$sVariantOfGloss"/>
		<xsl:for-each select="$lexEntryRef/LexEntryType">
			<xsl:variable name="lexEntryInflType" select="key('LexEntryInflTypeID',@dst)"/>
			<xsl:if test="$lexEntryInflType">
				<xsl:call-template name="GlossAddition">
					<xsl:with-param name="addition" select="$lexEntryInflType/GlossAppend"/>
				</xsl:call-template>
			</xsl:if>
		</xsl:for-each>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		IdOfVariantEntry
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="IdOfVariantEntry">
		<xsl:param name="lexEntry"/>
		<xsl:param name="lexEntryRef"/>
		<xsl:param name="msa"/>
		<xsl:value-of select="$lexEntry/@Id"/>
		<xsl:variable name="iPos" select="count($lexEntryRef/preceding-sibling::LexEntryRef)"/>
		<xsl:if test="$iPos &gt; 0">
			<xsl:text>.</xsl:text>
			<!-- Subtracting 1 to account for 0-indexing in C# -->
			<xsl:value-of select="$iPos - 1"/>
		</xsl:if>
		<xsl:call-template name="AppendAnyMsaCountNumber">
			<xsl:with-param name="msa" select="$msa"/>
		</xsl:call-template>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		AppendAnyMsaCountNumber
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="AppendAnyMsaCountNumber">
		<xsl:param name="msa"/>
		<xsl:if test="$msa">
			<xsl:variable name="iMsaPos" select="count($msa/preceding-sibling::MorphoSyntaxAnalysis)"/>
			<xsl:if test="$iMsaPos &gt; 0">
				<xsl:text>.</xsl:text>
				<!-- Subtracting 1 to account for 0-indexing in C# -->
				<xsl:value-of select="$iMsaPos - 1"/>
			</xsl:if>
		</xsl:if>
	</xsl:template>

</xsl:stylesheet>
