<?xml version="1.0" encoding="UTF-8"?>
<!-- #############################################################
	# Name:		SemDomQs.xsl
	# Purpose:	 Report of Semantic domain questions
	#
	# Author:	  Greg Trihus <greg_trihus@sil.org>
	#
	# Created:	 2012/01/25
	#			  2012/11/21 gt - control page breaks
	#			  2012/12/03 gt - format changes proposed by Kevin Warfel
	#			  2012/12/10 gt - allow other languages to be ignored
	#			  2012/12/10 gt - fix spelling of margin-top
	#			  2012/12/10 gt - make folderStart work for en too.
	#			  2012/12/11 gt - allow output of all questions
	#			  2013/01/09 gt - put en descr in red if not translated
	#							- use different style for quest if no examples
	#			  2013/05/28 jt - ensure only one abbreviation output (in correct WS)
	#							- integrated into git; recommend we stop tracking changes here
	# Copyright:   (c) 2012-2013 SIL International
	# Licence:	 <LPGL>
	################################################################-->
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	version="1.0">

	<xsl:param name="ignoreLang">en</xsl:param>
	<xsl:param name="allQuestions">0</xsl:param>
	<xsl:variable name="folder" select="document('folderStart.xml')/ul/li/text()"/>

	<xsl:output encoding="UTF-8" method="html"
		doctype-public="-//W3C//DTD HTML 4.01//EN"
		doctype-system="http://www.w3.org/TR/html4/strict.dtd"/>

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
h1 { font-family: Arial, sanserif; font-size: 16pt; margin: 0; page-break-before: always;}
h2 { font-family: Arial, sanserif; font-size: 16pt; margin: 0; page-break-after: avoid;}
.descr { font-family: "Times New Roman", serif; font-size: 12pt; margin: 0;}
.quest { font-family: "Times New Roman", serif; font-size: 12pt; margin: 0; margin-top: 4pt; page-break-after: avoid; }
.quest1 { font-family: "Times New Roman", serif; font-size: 12pt; margin: 0; margin-top: 4pt; }
.words { font-style: italic; font-family: "Times New Roman", serif; font-size: 12pt;
		 margin: 0; margin-left: 0.38in; text-indent: -0.13in; display: none;}
.english {color: red;}
					</xsl:text>
				</xsl:element>
			</xsl:element>
			<xsl:element name="body">
				<xsl:apply-templates/>
			</xsl:element>
		</xsl:element>
	</xsl:template>

	<xsl:template match="CmSemanticDomain">
		<xsl:if test="count(.//Run[@ws != $ignoreLang]/text()) > 0 or count(.//AUni[@ws != $ignoreLang]/text())> 0 or $allQuestions != 0">
			<xsl:choose>
				<xsl:when test="Abbreviation/AUni[@ws = 'en']/text() = $folder">
					<xsl:element name="h1">
						<xsl:attribute name="lang">
							<xsl:value-of select="Name//AUni[@ws != $ignoreLang]/@ws"/>
						</xsl:attribute>
						<xsl:value-of select="Abbreviation/AUni[1]"/>
						<xsl:text> </xsl:text>
						<xsl:call-template name="Name"/>
					</xsl:element>
				</xsl:when>
				<xsl:otherwise>
					<xsl:element name="h2">
						<xsl:attribute name="lang">
							<xsl:value-of select="Name//AUni[@ws != $ignoreLang]/@ws"/>
						</xsl:attribute>
						<xsl:value-of select="Abbreviation/AUni[1]"/>
						<xsl:text> </xsl:text>
						<xsl:call-template name="Name"/>
					</xsl:element>
				</xsl:otherwise>
			</xsl:choose>
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
								<xsl:attribute name="class">english</xsl:attribute>
								<xsl:attribute name="lang">en</xsl:attribute>
								<xsl:value-of select=".//Run"/>
							</xsl:element>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:element>
			</xsl:if>
			<xsl:apply-templates/>
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
							<xsl:attribute name="class">english</xsl:attribute>
							<xsl:attribute name="lang">en</xsl:attribute>
							<xsl:value-of select="Question/AUni"/>
						</xsl:element>
					</xsl:otherwise>
				</xsl:choose>
				<xsl:text> </xsl:text>
				<xsl:if test="count(ExampleWords//AUni[@ws != $ignoreLang]/text()) > 0">
					<xsl:element name="p">
						<xsl:attribute name="class">words</xsl:attribute>
						<xsl:attribute name="lang">
							<xsl:value-of select="ExampleWords//AUni[@ws != $ignoreLang]/@ws"/>
						</xsl:attribute>
						<!-- See: http://stackoverflow.com/questions/1068647/how-do-i-create-tab-indenting-in-html -->
						<xsl:attribute name="style">white-space: pre;</xsl:attribute>
						<xsl:text>&#x2022;&#x09;</xsl:text>
						<xsl:value-of select="ExampleWords/AUni[@ws!=$ignoreLang]"/>
					</xsl:element>
				</xsl:if>
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
					<xsl:attribute name="class">english</xsl:attribute>
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