<?xml version="1.0" encoding="UTF-8"?>
<!-- #############################################################
# Name:        SemDomQuestDepthFirst.xsl
# Purpose:     Report of Semantic domain questions, depth first
#
# Author:      Greg Trihus <greg_trihus@sil.org>
#
# Created:     2014/08/29 from SemDomQs.xsl
# Copyright:   (c) 2012-2014 SIL International
# License:     <LGPL>
################################################################-->
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	version="1.0">

	<xsl:param name="ignoreLang">en</xsl:param>
	<xsl:param name="allQuestions">0</xsl:param>
	<xsl:param name="folderStart">folderEnd.xml</xsl:param>
	<xsl:variable name="folder" select="document($folderStart)/ul/li"/>

	<xsl:output encoding="UTF-8" method="xml"
		doctype-public="-//W3C//DTD HTML 4.01//EN"
		doctype-system="http://www.w3.org/TR/html4/strict.dtd"
		omit-xml-declaration="yes"/>

	<!-- Remove text (empty lines) at the root level. -->
	<xsl:strip-space elements="*"/>

	<xsl:template match="//List[Abbreviation/AUni='Sem']">
		<xsl:element name="html">
			<xsl:attribute name="lang">
				<xsl:value-of select=".//Run[@ws != $ignoreLang][1]/@ws"/>
			</xsl:attribute>
			<xsl:element name="head">
				<xsl:element name="title">
					<xsl:call-template name="Name"/>
				</xsl:element>
				<xsl:element name="style">
					<xsl:attribute name="type">text/css</xsl:attribute>
					<xsl:text>
h1 { font-family: Arial, sanserif; font-size: 4pt; margin: 0; page-break-before: always; }
h2 { font-family: Arial, sanserif; font-size: 16pt; margin: 0; margin-top: 18pt; page-break-after: avoid;}
.descr { font-family: "Times New Roman", serif; font-size: 12pt; margin: 0;}
.quest { font-family: "Times New Roman", serif; font-size: 12pt; margin: 0; margin-top: 4pt; page-break-after: avoid; }
.quest1 { font-family: "Times New Roman", serif; font-size: 12pt; margin: 0; margin-top: 4pt; }
.words { font-style: italic; font-family: "Times New Roman", serif; font-size: 12pt;
		 margin: 0; margin-left: 0.38in; text-indent: -0.13in;}
.pagetext { font-size: 4pt; display: none; }
					</xsl:text>
				</xsl:element>
			</xsl:element>
			<xsl:element name="body">
				<xsl:apply-templates/>
			</xsl:element>
		</xsl:element>
	</xsl:template>

	<xsl:template match="CmSemanticDomain">
		<xsl:param name="doTitle" select="false()"/>
		<xsl:if test="count(.//Run[@ws != $ignoreLang]/text()) > 0 or count(.//AUni[@ws != $ignoreLang]/text())> 0 or $allQuestions != 0">
			<xsl:if test="not($doTitle)">
				<xsl:apply-templates select="SubPossibilities"/>
			</xsl:if>
			<xsl:if test="not(Abbreviation/AUni[@ws = 'en']/text() = $folder/@title) or $doTitle">
				<xsl:element name="h2">
					<xsl:attribute name="lang">
						<xsl:value-of select="Name//AUni[@ws != $ignoreLang]/@ws"/>
					</xsl:attribute>
					<xsl:value-of select="Abbreviation"/>
					<xsl:text> </xsl:text>
					<xsl:call-template name="Name"/>
				</xsl:element>
				<xsl:if test="count(Description//Run[@ws!=$ignoreLang]/text()) > 0 or $allQuestions != 0">
					<xsl:element name="p">
						<xsl:attribute name="class">descr</xsl:attribute>
						<xsl:choose>
							<xsl:when test="count(Description//Run[@ws!=$ignoreLang]/text()) > 0">
								<xsl:attribute name="lang">
									<xsl:value-of select=".//Run[@ws!=$ignoreLang]/@ws"/>
								</xsl:attribute>
								<xsl:value-of select="Description//Run[@ws!=$ignoreLang]"/>
							</xsl:when>
							<xsl:otherwise>
								<xsl:element name="span">
									<xsl:attribute name="style">color: red;</xsl:attribute>
									<xsl:attribute name="lang">en</xsl:attribute>
									<xsl:value-of select=".//Run"/>
								</xsl:element>
							</xsl:otherwise>
						</xsl:choose>
					</xsl:element>
				</xsl:if>
				<xsl:apply-templates select="node()[local-name() != 'SubPossibilities']"/>
				<xsl:if test="Abbreviation/AUni[@ws = 'en']/text() = $folder/text()">
					<xsl:variable name="myAbbreviation" select="Abbreviation/AUni[@ws = 'en']/text()"/>
					<xsl:variable name="myTitle" select="$folder[text() = $myAbbreviation]/@title"/>
					<xsl:apply-templates select="//CmSemanticDomain[Abbreviation/AUni[@ws = 'en']/text() = $myTitle]">
						<xsl:with-param name="doTitle" select="true()"/>
					</xsl:apply-templates>
					<xsl:if test="not($doTitle)">
						<xsl:element name="h1">
							<xsl:element name="span">
								<xsl:attribute name="class">pagetext</xsl:attribute>
								<xsl:text>Page</xsl:text>
							</xsl:element>
						</xsl:element>
					</xsl:if>
				</xsl:if>
			</xsl:if>
		</xsl:if>
	</xsl:template>

	<xsl:template match="CmDomainQ">
		<xsl:if test="count(.//AUni[@ws != $ignoreLang]/text()) > 0 or $allQuestions != 0">
			<xsl:element name="p">
				<xsl:choose>
					<xsl:when test="count(ExampleWords//AUni[@ws != $ignoreLang]/text()) > 0">
						<xsl:attribute name="class">quest</xsl:attribute>
					</xsl:when>
					<xsl:otherwise>
						<xsl:attribute name="class">quest1</xsl:attribute>
					</xsl:otherwise>
				</xsl:choose>
				<xsl:choose>
					<xsl:when test="count(Question/AUni[@ws!=$ignoreLang]/text()) > 0">
						<xsl:attribute name="lang">
							<xsl:value-of select="Question/AUni[@ws!=$ignoreLang]/@ws"/>
						</xsl:attribute>
						<xsl:value-of select="Question/AUni[@ws!=$ignoreLang]"/>
					</xsl:when>
					<xsl:otherwise>
						<xsl:element name="span">
							<xsl:attribute name="style">color: red;</xsl:attribute>
							<xsl:attribute name="lang">en</xsl:attribute>
							<xsl:value-of select="Question"/>
						</xsl:element>
					</xsl:otherwise>
				</xsl:choose>
				<xsl:text> </xsl:text>
				<xsl:choose>
					<xsl:when test="count(ExampleWords//AUni[@ws != $ignoreLang]/text()) > 0">
						<xsl:element name="p">
							<xsl:attribute name="class">words</xsl:attribute>
							<xsl:attribute name="lang">
								<xsl:value-of select="ExampleWords//AUni[@ws != $ignoreLang]/@ws"/>
							</xsl:attribute>
							<!-- See: http://stackoverflow.com/questions/1068647/how-do-i-create-tab-indenting-in-html -->
							<xsl:attribute name="style">white-space: pre;</xsl:attribute>
							<xsl:text disable-output-escaping="yes"><![CDATA[&#x2022;&#x09;]]></xsl:text>
							<xsl:value-of select="ExampleWords/AUni[@ws!=$ignoreLang]"/>
						</xsl:element>
					</xsl:when>
					<xsl:when test="$allQuestions != 0">
						<xsl:element name="p">
							<xsl:attribute name="class">words</xsl:attribute>
							<xsl:attribute name="lang">en</xsl:attribute>
							<xsl:attribute name="style">color: red; white-space: pre;</xsl:attribute>
							<xsl:text disable-output-escaping="yes"><![CDATA[&#x2022;&#x09;]]></xsl:text>
						  <xsl:value-of select="ExampleWords"/>
						</xsl:element>
					</xsl:when>
				</xsl:choose>
			</xsl:element>
		</xsl:if>
	</xsl:template>

	<xsl:template name="Name">
		<xsl:choose>
			<xsl:when test="count(Name/AUni[@ws != $ignoreLang]/text()) > 0">
				<xsl:value-of select="Name/AUni[@ws != $ignoreLang]"/>
			</xsl:when>
			<xsl:when test="count(Name/AUni[@ws = $ignoreLang]/text()) > 0">
				<xsl:element name="span">
					<xsl:attribute name="style">color: red;</xsl:attribute>
					<xsl:value-of select="Name/AUni[@ws = $ignoreLang]"/>
				</xsl:element>
			</xsl:when>
			<xsl:otherwise>
				<xsl:text>Semantic Domains</xsl:text>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template match="AStr | AUni"/>

	<xsl:template match="Abbreviation |
		Description | ExampleSentences | ExampleWords | List[not(Abbreviation/AUni = 'Sem')] | Name | Possibilities |
		Question | Questions | Run | SubPossibilities">
		<xsl:apply-templates/>
	</xsl:template>
</xsl:stylesheet>
