<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="xml" version="1.0" encoding="UTF-8" indent="yes"/>
	<!--
	Main Template
	-->
	<xsl:template match="//Class">
		<p>Class: <xsl:value-of select="@name"/>
		</p>
		<xsl:apply-templates select="Field"/>
	</xsl:template>
	<xsl:template match="Field">
		<html>
			<head>
				<title>Modify Markers Help</title>
			</head>
			<body bgcolor="lemonchiffon">
				<div>
					<xsl:call-template name="OutputLine">
						<xsl:with-param name="sLabel" select="'About'"/>
						<xsl:with-param name="Content" select="@uiname"/>
					</xsl:call-template>
					<xsl:call-template name="OutputLine">
						<xsl:with-param name="sLabel" select="'When to use'"/>
						<xsl:with-param name="Content" select="Help/Usage"/>
					</xsl:call-template>
					<xsl:call-template name="OutputLine">
						<xsl:with-param name="sLabel" select="'Additional Settings'"/>
						<xsl:with-param name="Content" select="Help/Settings"/>
					</xsl:call-template>
					<xsl:call-template name="OutputLine">
						<xsl:with-param name="sLabel" select="'Interprets character mapping'"/>
						<xsl:with-param name="Content" select="Help/Mapping"/>
					</xsl:call-template>
					<xsl:call-template name="OutputLine">
						<xsl:with-param name="sLabel" select="'Allows multiple SFM fields'"/>
						<xsl:with-param name="Content" select="Help/Appends"/>
					</xsl:call-template>
					<xsl:call-template name="OutputLine">
						<xsl:with-param name="sLabel" select="'Uses list item'"/>
						<xsl:with-param name="Content" select="Help/List"/>
					</xsl:call-template>
					<xsl:call-template name="OutputLine">
						<xsl:with-param name="sLabel" select="'Allows an equivalent field for each language'"/>
						<xsl:with-param name="Content" select="Help/Multilingual"/>
					</xsl:call-template>
					<xsl:call-template name="OutputList">
						<xsl:with-param name="sLabel" select="'Example(s)'"/>
						<xsl:with-param name="Content" select="Help/Examples"/>
					</xsl:call-template>
					<xsl:call-template name="OutputLine">
						<xsl:with-param name="sLabel" select="'Related fields'"/>
						<xsl:with-param name="Content" select="Help/RelatedFields"/>
					</xsl:call-template>
					<xsl:call-template name="OutputBulletedList">
						<xsl:with-param name="sLabel" select="'Limitations'"/>
						<xsl:with-param name="Content" select="Help/Limitations"/>
					</xsl:call-template>
					<xsl:call-template name="OutputBulletedList">
						<xsl:with-param name="sLabel" select="'Extra things that will happen'"/>
						<xsl:with-param name="Content" select="Help/Extras"/>
					</xsl:call-template>
				</div>
			</body>
		</html>
	</xsl:template>
	<!--
	OutputBulletedList
	-->
	<xsl:template name="OutputBulletedList">
		<xsl:param name="sLabel"/>
		<xsl:param name="Content"/>
		<xsl:if test="$Content">
			<xsl:call-template name="OutputLabel">
				<xsl:with-param name="sLabel" select="$sLabel"/>
			</xsl:call-template>
			<br/>
			<ul style="margin-top:0pt; margin-bottom:0pt">
				<xsl:for-each select="$Content/*">
					<li>
						<xsl:value-of select="."/>
					</li>
				</xsl:for-each>
			</ul>
		</xsl:if>
	</xsl:template>
	<!--
	OutputLabel
	-->
	<xsl:template name="OutputLabel">
		<xsl:param name="sLabel"/>
		<b>
			<xsl:value-of select="$sLabel"/>
		</b>
		<xsl:text>: </xsl:text>
	</xsl:template>
	<!--
	OutputLine
	-->
	<xsl:template name="OutputLine">
		<xsl:param name="sLabel"/>
		<xsl:param name="Content"/>
		<xsl:if test="$Content">
			<xsl:call-template name="OutputLabel">
				<xsl:with-param name="sLabel" select="$sLabel"/>
			</xsl:call-template>
			<xsl:value-of select="$Content"/>
			<br/>
		</xsl:if>
	</xsl:template>
	<!--
	OutputList
	-->
	<xsl:template name="OutputList">
		<xsl:param name="sLabel"/>
		<xsl:param name="Content"/>
		<xsl:if test="$Content">
			<xsl:call-template name="OutputLabel">
				<xsl:with-param name="sLabel" select="$sLabel"/>
			</xsl:call-template>
			<br/>
			<div style="margin-left:0.25in; margin-top:0pt; margin-bottom:0pt">
				<xsl:for-each select="$Content/*">
					<xsl:value-of select="."/>
					<br/>
				</xsl:for-each>
			</div>
		</xsl:if>
	</xsl:template>
</xsl:stylesheet>
