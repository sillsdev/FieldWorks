<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
	<!-- This stylesheet contains the common components for XLingPap output  -->

	<xsl:template match="interlinear-text">
	<xsl:variable name="sScriptureType">
		<xsl:variable name="sTitle" select="item[@type='title']"/>
		<xsl:variable name="iPreviousInterlinearTexts" select="count(preceding-sibling::interlinear-text)"/>
		<xsl:choose>
			<xsl:when test="substring($sTitle,string-length($sTitle)-6)='(Title)'">
<!--                <xsl:value-of select="$iPreviousInterlinearTexts"/>-->
				<!-- assume this is a Scripture title -->
				<xsl:text>Title</xsl:text>
			</xsl:when>
			<xsl:when test="substring($sTitle,string-length($sTitle)-8)='(Heading)'">
<!--                <xsl:value-of select="$iPreviousInterlinearTexts"/>-->
				<!-- assume this is a Scripture subtitle -->
				<xsl:text>Subtitle</xsl:text>
			</xsl:when>
			<xsl:when test="contains($sTitle,'Footnote(')">
<!--                <xsl:value-of select="$iPreviousInterlinearTexts"/>-->
				<!-- assume this is a Scripture footnote -->
				<xsl:text>Footnote</xsl:text>
			</xsl:when>
			<xsl:otherwise>
				<!-- it's something else, so use nothing -->
			</xsl:otherwise>
		</xsl:choose>
	</xsl:variable>
		<xsl:apply-templates>
			<xsl:with-param name="sScriptureType" select="$sScriptureType"/>
		</xsl:apply-templates>
	</xsl:template>
	<xsl:template match="paragraph">
		<xsl:param name="sScriptureType"/>
		<xsl:apply-templates>
			<xsl:with-param name="sScriptureType" select="$sScriptureType"/>
		</xsl:apply-templates>
	</xsl:template>
	<xsl:template match="paragraphs">
		<xsl:param name="sScriptureType"/>
		<xsl:apply-templates>
			<xsl:with-param name="sScriptureType" select="$sScriptureType"/>
		</xsl:apply-templates>
	</xsl:template>
	<xsl:template match="phrases">
		<xsl:param name="sScriptureType"/>
		<xsl:apply-templates>
			<xsl:with-param name="sScriptureType" select="$sScriptureType"/>
		</xsl:apply-templates>
	</xsl:template>
	<xsl:template match="phrase[count(item)=0]">
		<!-- skip these -->
	</xsl:template>
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
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		OutputLanguageElements
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="OutputLanguageElements">
		<xsl:for-each select="//interlinear-text[1]/languages/language">
			<xsl:variable name="sLangId" select="@lang"/>
			<xsl:if test="//item[@lang=$sLangId]">
				<language id="{@lang}" font-family="{@font}">
					<xsl:if test="@vernacular='true'">
						<xsl:attribute name="name">vernacular</xsl:attribute>
					</xsl:if>
				</language>
			</xsl:if>
		</xsl:for-each>

	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		OutputLevelContent
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="OutputLevelContent">
		<xsl:param name="sScriptureType"/>
		<xsl:if test="item[@type='segnum']">
			<xsl:choose>
				<xsl:when test="string-length($sScriptureType) &gt; 0">
					<xsl:value-of select="$sScriptureType"/>
					<xsl:if test="$sScriptureType='Footnote'">
						<xsl:text>.</xsl:text>
						<xsl:value-of select="item[@type='segnum']"/>
					</xsl:if>
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="item[@type='segnum']"/>
<!--                    <xsl:apply-templates select="item[@type='segnum']" mode="ReplaceColon"/>-->
				</xsl:otherwise>
			</xsl:choose>
		</xsl:if>
	</xsl:template>
	<!--<xsl:template match="text()[contains(.,':')]" mode="ReplaceColon">
		<xsl:value-of select="substring-before(.,':')"/>
		<xsl:text>___ReplaceWithColon___</xsl:text>
		<xsl:value-of select="substring-after(.,':')"/>
	</xsl:template>-->
	<xsl:include href="MorphTypeGuids.xsl"/>
</xsl:stylesheet>
