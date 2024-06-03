<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
	<xsl:output method="xml" encoding="UTF-8" indent="yes"/>
	<!-- This stylesheet transforms a discourse chart as output by FieldWorks into a form which conforms to the XLingPap DTD and can then be edited with the XMLmind XML Editor using the XLingPap configuration.  -->
	<!--
================================================================
FieldWorks Language Explorer discourse chart XML to XLingPap mapper for Stage 1.
  Input:    XML output of FLEx discourse chart
  Output: XLingPaper XML
================================================================

- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Preamble
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:key name="Language" match="language" use="@lang"/>
	<xsl:param name="sHyphen" select="'-'"/>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Main template
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="document">
		<!-- output dtd path -->
		<xsl:text disable-output-escaping="yes">&#xa;&#x3c;!DOCTYPE lingPaper PUBLIC   "-//XMLmind//DTD XLingPap//EN" "XLingPap.dtd"&#x3e;&#xa;</xsl:text>
		<lingPaper>
			<frontMatter>
				<title>
					<xsl:choose>
						<xsl:when test="//interlinear-text/item[@type='title']">
							<xsl:value-of select="//interlinear-text/item[@type='title']"/>
						</xsl:when>
						<xsl:otherwise>
							<xsl:text>FieldWorks Language Explorer Discourse Chart Export</xsl:text>
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
				<secTitle>Discourse Chart</secTitle>
				<xsl:apply-templates/>
			</section1>
			<backMatter>
				<endnotes/>
				<references/>
			</backMatter>
			<languages>
				<xsl:for-each select="//language">
					<xsl:variable name="sLangId" select="@lang"/>
					<xsl:if test="//@lang=$sLangId">
						<language id="{@lang}" font-family="{@font}">
							<xsl:if test="@vernacular='true'">
								<xsl:attribute name="name">vernacular</xsl:attribute>
							</xsl:if>
						</language>
					</xsl:if>
				</xsl:for-each>
			</languages>
			<types>
				<type id="tDependent" color="#0000FF"/>
				<type id="tGramInfo" color="#FF9900"/>
				<type id="tMoved" color="#FF0000"/>
				<type id="tMovedMkr" color="#FF8080"/>
				<type id="tSong" color="#993366"/>
				<type id="tSpeech" color="#008000"/>
			</types>
		</lingPaper>
	</xsl:template>
	<!--
		cell
-->
	<xsl:template match="cell">
		<td>
			<xsl:if test="@cols &gt; 1">
				<xsl:attribute name="colspan">
					<xsl:value-of select="@cols"/>
				</xsl:attribute>
			</xsl:if>
			<xsl:apply-templates select="*[name()!='glosses']"/>
		</td>
	</xsl:template>
	<xsl:template match="cell" mode="title">
		<th>
			<xsl:if test="@cols &gt; 1">
				<xsl:attribute name="colspan">
					<xsl:value-of select="@cols"/>
				</xsl:attribute>
			</xsl:if>
			<xsl:apply-templates/>
		</th>
	</xsl:template>
	<!--
		chart
	-->
	<xsl:template match="chart">
		<table border="1">
			<xsl:apply-templates/>
		</table>
	</xsl:template>
	<!--
		gloss
	-->
	<xsl:template match="gloss">
		<gloss lang="{@lang}">
			<xsl:if test="count(preceding-sibling::*) &gt; 0">
				<xsl:text>&#x20;</xsl:text>
			</xsl:if>
			<xsl:apply-templates/>
		</gloss>
	</xsl:template>
	<!--
		glosses
	-->
	<xsl:template match="glosses">
		<xsl:apply-templates/>
	</xsl:template>
	<!--
		listRef
	-->
	<xsl:template match="listRef">
		<gloss lang="{@lang}">
			<xsl:text>&#x20;</xsl:text>
			<object type="tGramInfo">
				<xsl:apply-templates/>
			</object>
		</gloss>
	</xsl:template>
	<!--
		lit
	-->
	<xsl:template match="lit">
		<xsl:if test="@noSpaceBefore='false'">
			<xsl:text>&#x20;</xsl:text>
		</xsl:if>
		<xsl:choose>
			<xsl:when test="ancestor::row[@type='speech']">
				<object type="tSpeech">
					<xsl:apply-templates/>
				</object>
			</xsl:when>
			<xsl:when test="ancestor::row[@type='song']">
				<object type="tSong">
					<xsl:apply-templates/>
				</object>
			</xsl:when>
			<xsl:when test="ancestor::row[@type='dependent']">
				<object type="tDependent">
					<xsl:apply-templates/>
				</object>
			</xsl:when>
			<xsl:otherwise>
				<xsl:apply-templates/>
			</xsl:otherwise>
		</xsl:choose>
		<xsl:if test="@noSpaceAfter='false'">
			<xsl:text>&#x20;</xsl:text>
		</xsl:if>
	</xsl:template>

	<xsl:template match="lit[following-sibling::*[1][name()='listRef'] or preceding-sibling::*[1][name()='listRef']]">
		<xsl:if test="@noSpaceBefore='false' or preceding-sibling::*[1][name()='word']">
			<xsl:text>&#x20;</xsl:text>
		</xsl:if>
		<object type="tGramInfo">
			<xsl:apply-templates/>
		</object>
		<xsl:if test="@noSpaceAfter='false'">
			<xsl:text>&#x20;</xsl:text>
		</xsl:if>
	</xsl:template>

	<xsl:template match="lit[following-sibling::word]">
		<langData lang="{@lang}">
			<xsl:if test="count(preceding-sibling::*) &gt; 0">
				<xsl:text>&#x20;</xsl:text>
			</xsl:if>
			<xsl:choose>
				<xsl:when test="ancestor::row[@type='speech']">
					<object type="tSpeech">
						<xsl:apply-templates/>
					</object>
				</xsl:when>
				<xsl:when test="ancestor::row[@type='song']">
					<object type="tSong">
						<xsl:apply-templates/>
					</object>
				</xsl:when>
				<xsl:when test="ancestor::row[@type='dependent']">
					<object type="tDependent">
						<xsl:apply-templates/>
					</object>
				</xsl:when>
				<xsl:otherwise>
					<xsl:apply-templates/>
				</xsl:otherwise>
			</xsl:choose>
		</langData>
	</xsl:template>


	<!--
		main
	-->
	<xsl:template match="main">
		<xsl:choose>
			<xsl:when test="string-length(.) = 0">
				<!-- this one is empty; output a non-breaking space into this table cell -->
				<xsl:text>&#xa0;</xsl:text>
			</xsl:when>
			<xsl:when test="following-sibling::glosses">
				<interlinear>
					<lineGroup>
						<line>
							<wrd>
								<xsl:apply-templates select="lit | word | listRef | moveMkr"/>
							</wrd>
						</line>
						<line>
							<wrd>
								<xsl:apply-templates select="following-sibling::glosses"/>
							</wrd>
						</line>
					</lineGroup>
				</interlinear>
			</xsl:when>
			<xsl:otherwise>
				<xsl:apply-templates/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
		moveMkr
	-->
	<xsl:template match="moveMkr">
		<object type="tMovedMkr">
			<xsl:choose>
				<xsl:when test=".='Preposed'">
					<xsl:text>&lt;&lt; </xsl:text>
				</xsl:when>
				<xsl:otherwise>
					<xsl:text> &gt;&gt;</xsl:text>
				</xsl:otherwise>
			</xsl:choose>
		</object>
	</xsl:template>
	<!--
		row
	-->
	<xsl:template match="row">
		<tr>
			<xsl:choose>
				<xsl:when test="starts-with(@type,'title')">
					<xsl:apply-templates mode="title"/>
				</xsl:when>
				<xsl:otherwise>
					<xsl:apply-templates/>
				</xsl:otherwise>
			</xsl:choose>
		</tr>
	</xsl:template>
	<!--
		rownum
	-->
	<xsl:template match="rownum">
		<xsl:apply-templates/>
	</xsl:template>
	<!--
		word
	-->
	<xsl:template match="word">
		<langData lang="{@lang}">
			<xsl:if test="count(preceding-sibling::*) &gt; 0">
				<xsl:text>&#x20;</xsl:text>
			</xsl:if>
			<xsl:choose>
				<xsl:when test="ancestor::row[@type='speech']">
					<object type="tSpeech">
						<xsl:apply-templates/>
					</object>
				</xsl:when>
				<xsl:when test="ancestor::row[@type='song']">
					<object type="tSong">
						<xsl:apply-templates/>
					</object>
				</xsl:when>
				<xsl:when test="ancestor::row[@type='dependent']">
					<object type="tDependent">
						<xsl:apply-templates/>
					</object>
				</xsl:when>
				<xsl:when test="@moved='true'">
					<object type="tMoved">
						<xsl:apply-templates/>
					</object>
				</xsl:when>
				<xsl:otherwise>
					<xsl:apply-templates/>
				</xsl:otherwise>
			</xsl:choose>
		</langData>
	</xsl:template>
</xsl:stylesheet>
