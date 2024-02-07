<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
	<!-- Items common to XAmple and HermitCrab -->
	<xsl:include href="MorphTypeGuids.xsl"/>

	<xsl:key name="LexEntryInflTypeID" match="LexEntryInflType" use="@Id"/>
	<xsl:key name="PrefixSlotsID" match="/M3Dump/PartsOfSpeech/PartOfSpeech/AffixTemplates/MoInflAffixTemplate/PrefixSlots" use="@dst"/>

	<xsl:variable name="LexEntryInflTypeSlots" select="/M3Dump/LexEntryInflTypes/LexEntryInflType/Slots"/>
	<xsl:variable name="PartsOfSpeech" select="/M3Dump/PartsOfSpeech/PartOfSpeech"/>
	<!-- Old files have a single level of ParserParameters.  New output may have two levels (ParserParameters/ParserParameters). -->
	<xsl:variable name="XAmple" select="/M3Dump/ParserParameters//XAmple"/>
	<xsl:variable name="MoMorphTypes" select="/M3Dump/MoMorphTypes/MoMorphType"/>
	<xsl:variable name="MoEndoCompounds" select="/M3Dump/CompoundRules/MoEndoCompound"/>
	<xsl:variable name="MoExoCompounds" select="/M3Dump/CompoundRules/MoExoCompound"/>
	<xsl:variable name="MoAffixAllomorphs" select="/M3Dump/Lexicon/Allomorphs/MoAffixAllomorph"/>

	<!-- Parameters that can be set by user.  -->
	<xsl:param name="prmMaxInfixes">
		<xsl:choose>
			<xsl:when test="$XAmple/MaxInfixes">
				<xsl:value-of select="$XAmple/MaxInfixes"/>
			</xsl:when>
			<xsl:when test="$MoMorphTypes[Name='infix']">1</xsl:when>
			<xsl:otherwise>0</xsl:otherwise>
		</xsl:choose>
	</xsl:param>
	<xsl:param name="prmMaxNull">
		<xsl:choose>
			<xsl:when test="$XAmple/MaxNulls">
				<xsl:value-of select="$XAmple/MaxNulls"/>
			</xsl:when>
			<xsl:otherwise>1</xsl:otherwise>
		</xsl:choose>
	</xsl:param>
	<xsl:param name="prmMaxPrefixes">
		<xsl:choose>
			<xsl:when test="$XAmple/MaxPrefixes">
				<xsl:value-of select="$XAmple/MaxPrefixes"/>
			</xsl:when>
			<xsl:otherwise>5</xsl:otherwise>
		</xsl:choose>
	</xsl:param>
	<xsl:param name="prmMaxRoots">
		<xsl:choose>
			<xsl:when test="$XAmple/MaxRoots">
				<xsl:value-of select="$XAmple/MaxRoots"/>
			</xsl:when>
			<xsl:when test="$MoEndoCompounds | $MoExoCompounds">
				<xsl:choose>
					<xsl:when test="$MoEndoCompounds/Linker | $MoExoCompounds/Linker">3</xsl:when>
					<xsl:otherwise>2</xsl:otherwise>
				</xsl:choose>
			</xsl:when>
			<xsl:otherwise>1</xsl:otherwise>
		</xsl:choose>
	</xsl:param>
	<xsl:param name="prmMaxSuffixes">
		<xsl:choose>
			<xsl:when test="$XAmple/MaxSuffixes">
				<xsl:value-of select="$XAmple/MaxSuffixes"/>
			</xsl:when>
			<xsl:otherwise>5</xsl:otherwise>
		</xsl:choose>
	</xsl:param>
	<xsl:param name="prmMaxInterfixes">
		<xsl:choose>
			<xsl:when test="$XAmple/MaxInterfixes">
				<xsl:variable name="sValue" select="$XAmple/MaxInterfixes"/>
				<xsl:choose>
					<xsl:when test="$sValue!=0">
						<xsl:value-of select="$sValue"/>
					</xsl:when>
					<xsl:otherwise>
						<xsl:call-template name="DetermineMaxInterfixes"/>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:when>
			<xsl:otherwise>
				<xsl:call-template name="DetermineMaxInterfixes"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:param>
	<xsl:variable name="bSuffixingOnly">
		<xsl:choose>
			<xsl:when test="$prmMaxPrefixes=0 and $prmMaxInfixes=0">
				<xsl:text>Y</xsl:text>
			</xsl:when>
			<xsl:otherwise>
				<xsl:text>N</xsl:text>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:variable>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		DetermineMaxInterfixes
		Determine max interfixes value based on data
		Parameters: none
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="DetermineMaxInterfixes">
		<xsl:variable name="sPfxNfx" select="$MoMorphTypes[@Guid=$sPrefixingInterfix]/@Id"/>
		<xsl:variable name="sIfxNfx" select="$MoMorphTypes[@Guid=$sInfixingInterfix]/@Id"/>
		<xsl:variable name="sSfxNfx" select="$MoMorphTypes[@Guid=$sSuffixingInterfix]/@Id"/>
		<xsl:choose>
			<xsl:when test="$MoAffixAllomorphs[@MorphType=$sPfxNfx or @MorphType=$sIfxNfx or @MorphType=$sSfxNfx]">1</xsl:when>
			<xsl:otherwise>0</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		GetTopLevelPOSId
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="GetTopLevelPOSId">
		<xsl:param name="pos" select="."/>
		<xsl:variable name="subpos" select="$PartsOfSpeech/SubPossibilities[@dst=$pos/@Id]"/>
		<xsl:choose>
			<xsl:when test="$subpos">
				<xsl:call-template name="GetTopLevelPOSId">
					<xsl:with-param name="pos" select="$subpos/parent::PartOfSpeech"></xsl:with-param>
				</xsl:call-template>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="$pos/@Id"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
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
