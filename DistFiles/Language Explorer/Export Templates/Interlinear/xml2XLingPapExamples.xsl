<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
	<xsl:output method="xml" encoding="UTF-8" indent="yes"/>
	<!-- This stylesheet transforms an interlinear text as output by FieldWorks into a form which conforms to the XLingPap DTD and can then be edited with the XMLmind XML Editor usingthe XLingPap configuration.  -->
	<!--
================================================================
FieldWorks Language Explorer interlinear XML to XLingPap mapper for Stage 1.
  Input:    XML output of FLEx interlinear, where morphemes in a word may be aligned in columns
  Output: XLingPap XML, where each interlinear is an example

================================================================
Revision History is at the end of this file.

- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Preamble
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Main template
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="document">
	   <!-- output dtd path -->
	   <xsl:text disable-output-escaping="yes">&#xa;&#x3c;!DOCTYPE lingPaper PUBLIC   "-//XMLmind//DTD XLingPap//EN" "XLingPap.dtd"&#x3e;&#xa;</xsl:text>
		<lingPaper automaticallywrapinterlinears="yes">
			<frontMatter>
				<title>
					<xsl:choose>
						<xsl:when test="//interlinear-text/item[@type='title']">
							<xsl:value-of select="//interlinear-text/item[@type='title']"/>
						</xsl:when>
						<xsl:otherwise>
							<xsl:text>FieldWorks Language Explorer Interlinear Export</xsl:text>
						</xsl:otherwise>
					</xsl:choose>
				</title>
				<author>
					<xsl:choose>
						<xsl:when test="//interlinear-text/item[@type='source']">
							<xsl:value-of select="//interlinear-text/item[@type='source']"/>
						</xsl:when>
						<xsl:otherwise>
							<xsl:text>[Insert author's name here]</xsl:text>
						</xsl:otherwise>
					</xsl:choose>
				</author>
			</frontMatter>
			<section1 id="s1">
				<secTitle>Interlinear text</secTitle>
				<xsl:apply-templates/>
			</section1>
			<backMatter>
				<endnotes/>
				<references/>
			</backMatter>
			<languages>
				<xsl:call-template name="OutputLanguageElements"/>
			</languages>
			<types>
				<xsl:call-template name="CommonTypes"/>
				<!--<type id="tHomographNumber" font-size="65%" cssSpecial="vertical-align:sub"
					xsl-foSpecial="baseline-shift='sub'"/>
				<type id="tVariantTypes" >
					<xsl:variable name="analysisLanguage" select="//language[not(@vernacular='true')][1]"/>
					<xsl:if test="$analysisLanguage">
						<xsl:attribute name="font-family">
							<xsl:value-of select="$analysisLanguage/@font"/>
						</xsl:attribute>
					</xsl:if>
				</type>-->
			</types>
		</lingPaper>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
morph
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="morph">
		<morph>
			<xsl:call-template name="GetMorphTypeFromGuid">
				<xsl:with-param name="sGuid" select="@guid"/>
			</xsl:call-template>
			<xsl:apply-templates/>
		</morph>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
morphemes
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="morphemes">
		<morphemes>
			<morphset>
				<xsl:apply-templates/>
			</morphset>
		</morphemes>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
phrase
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="phrase">
		<xsl:param name="sScriptureType"/>
		<xsl:variable name="sLevel">
			<xsl:call-template name="OutputLevelContent">
				<xsl:with-param name="sScriptureType" select="$sScriptureType"/>
			</xsl:call-template>
		</xsl:variable>
		<xsl:variable name="sThisTextId">
			<!-- we really need something from the DB or some such.  Am trying this in hopes it will be unique in most cases -->
			<xsl:for-each select="../phrase[position()=last()]">
				<!--                <xsl:if test="position()=last()">-->
				<xsl:value-of select="generate-id()"/>
				<!--                </xsl:if>-->
			</xsl:for-each>
		</xsl:variable>
		<p>
			<xsl:text>paragraph </xsl:text>
			<xsl:value-of select="$sLevel"/>
		</p>
		<example num="x{$sThisTextId}-{translate($sLevel,':','.')}">
			<interlinear>
				<phrase>
					<xsl:apply-templates/>
				</phrase>
			</interlinear>
		</example>
	</xsl:template>
	<xsl:include href="xml2XLingPapCommon.xsl"/>
	<xsl:template match="item[parent::phrase]">
		<xsl:choose>
			<xsl:when test="@type='txt'">
				<!-- what is this for?   -->
			</xsl:when>
			<xsl:when test="@type='gls'">
				<item type="gls" lang="{@lang}">
					<xsl:apply-templates/>
				</item>
			</xsl:when>
			<xsl:when test="@type='note'">
				<item type="note" lang="{@lang}">
					<xsl:apply-templates/>
				</item>
			</xsl:when>
			<xsl:when test="@type='lit' ">
				<!--  someday we'll have a literal translation element in XLingPaper -->
				<item type="gls" lang="{@lang}">
					<object type="tLiteralTranslation">
						<xsl:apply-templates/>
					</object>
				</item>
			</xsl:when>
		</xsl:choose>
	</xsl:template></xsl:stylesheet>
<!--
================================================================
Revision History
- - - - - - - - - - - - - - - - - - -
02-Aug-2006  Andy Black  Initial Draft
================================================================
-->
