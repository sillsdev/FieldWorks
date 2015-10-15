<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
	<!-- This stylesheet contains the common components for XLingPaper output  -->

	<xsl:key name="ItemsLang" match="//item" use="@lang"/>

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
		<type id="tLiteralTranslation"/>
		<type id="tVariantTypes">
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
		GetFreeLangAttribute
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="GetFreeLangAttribute">
		<xsl:param name="sAttr" select="'lang'"/>
		<xsl:attribute name="{$sAttr}">
			<xsl:value-of select="@lang"/>
			<xsl:text>-</xsl:text>
			<xsl:choose>
				<xsl:when test="@type='gls'">
					<xsl:text>free</xsl:text>
				</xsl:when>
				<xsl:otherwise>
					<xsl:choose>
						<xsl:when test="@type='lit'">
							<xsl:text>literal</xsl:text>
						</xsl:when>
						<xsl:otherwise>
							<xsl:text>note</xsl:text>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:attribute>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		GetMorphLangAttribute
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="GetMorphLangAttribute">
		<xsl:param name="sLang" select="@lang"/>
		<xsl:param name="sType" select="@type"/>
		<xsl:attribute name="lang">
			<xsl:value-of select="$sLang"/>
			<xsl:text>-</xsl:text>
			<xsl:choose>
				<xsl:when test="$sType='txt'">
					<xsl:text>morpheme</xsl:text>
				</xsl:when>
				<xsl:when test="$sType='cf'">
					<xsl:text>lexEntry</xsl:text>
				</xsl:when>
				<xsl:when test="$sType='gls'">
					<xsl:text>lexGloss</xsl:text>
				</xsl:when>
				<xsl:when test="$sType='msa'">
					<xsl:text>gramInfo</xsl:text>
				</xsl:when>
			</xsl:choose>
		</xsl:attribute>
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
		GetWordLangAttribute
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="GetWordLangAttribute">
		<xsl:param name="sAttr" select="'lang'"/>
		<xsl:attribute name="{$sAttr}">
			<xsl:value-of select="@lang"/>
			<xsl:text>-</xsl:text>
			<xsl:choose>
				<xsl:when test="following-sibling::morphemes or @type='punct' or @type='txt' and count(../*)=1">
					<xsl:text>baseline</xsl:text>
				</xsl:when>
				<xsl:otherwise>
					<xsl:choose>
						<xsl:when test="@type='pos'">
							<xsl:text>wordCategory</xsl:text>
						</xsl:when>
						<xsl:otherwise>
							<xsl:text>wordGloss</xsl:text>
						</xsl:otherwise>
					</xsl:choose>
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
		<xsl:variable name="firstMorphemes" select="//paragraphs/paragraph[1]/phrases[1]/phrase[1]/words[1]/word[1]/morphemes/morph[1]/item"/>
		<xsl:for-each select="//interlinear-text[1]/languages/language">
			<xsl:variable name="sLangId" select="@lang"/>
			<xsl:if test="key('ItemsLang',$sLangId)">
				<xsl:variable name="sFont" select="@font"/>
				<xsl:if test="key('ItemsLang',$sLangId)[parent::phrase and @type='gls']">
					<language font-family="{$sFont}" id="{$sLangId}-free"/>
				</xsl:if>
				<xsl:if test="key('ItemsLang',$sLangId)[parent::phrase and @type='lit']">
					<language font-family="{$sFont}" id="{$sLangId}-literal"/>
				</xsl:if>
				<xsl:if test="key('ItemsLang',$sLangId)[parent::phrase and @type='note']">
					<language font-family="{$sFont}" id="{$sLangId}-note">
						<!--<xsl:if test="@vernacular='true'">
							<xsl:attribute name="name">vernacular</xsl:attribute>
						</xsl:if>-->
					</language>
				</xsl:if>
				<xsl:if test="key('ItemsLang',$sLangId)[parent::word and following-sibling::morphemes]">
					<language font-family="{$sFont}" id="{$sLangId}-baseline">
						<!--<xsl:if test="@vernacular='true'">
							<xsl:attribute name="name">vernacular</xsl:attribute>
						</xsl:if>-->
					</language>
				</xsl:if>
				<xsl:if test="key('ItemsLang',$sLangId)[parent::word and @type='pos']">
					<language font-family="{$sFont}" id="{$sLangId}-wordCategory"/>
				</xsl:if>
				<xsl:if test="key('ItemsLang',$sLangId)[parent::word and @type='gls']">
					<language font-family="{$sFont}" id="{$sLangId}-wordGloss"/>
				</xsl:if>
				<xsl:if test="key('ItemsLang',$sLangId)[parent::morph and @type='txt']">
					<language font-family="{$sFont}" id="{$sLangId}-morpheme">
						<!--<xsl:if test="@vernacular='true'">
							<xsl:attribute name="name">vernacular</xsl:attribute>
						</xsl:if>-->
					</language>
				</xsl:if>
				<xsl:if test="key('ItemsLang',$sLangId)[parent::morph and @type='cf']">
					<language font-family="{$sFont}" id="{$sLangId}-lexEntry">
						<!--<xsl:if test="@vernacular='true'">
							<xsl:attribute name="name">vernacular</xsl:attribute>
						</xsl:if>-->
					</language>
				</xsl:if>
				<xsl:if test="key('ItemsLang',$sLangId)[parent::morph and @type='gls']">
					<language font-family="{$sFont}" id="{$sLangId}-lexGloss">
						<!--<xsl:if test="@vernacular='true'">
							<xsl:attribute name="name">vernacular</xsl:attribute>
						</xsl:if>-->
					</language>
				</xsl:if>
				<xsl:if test="key('ItemsLang',$sLangId)[parent::morph and @type='msa']">
					<language font-family="{$sFont}" id="{$sLangId}-gramInfo">
						<!--<xsl:if test="@vernacular='true'">
							<xsl:attribute name="name">vernacular</xsl:attribute>
						</xsl:if>-->
					</language>
				</xsl:if>
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
