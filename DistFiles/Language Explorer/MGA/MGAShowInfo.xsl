<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="html" version="1.0" encoding="UTF-8" indent="yes"/>
	<xsl:param name="sHelpFile" select="'Help.htm'"/>
	<xsl:template match="/">
		<html>
			<head>
				<title>Morphosyntactic Gloss Assistant</title>
			</head>
			<body bgcolor="lemonchiffon">
				<p>
					<b>
						<xsl:value-of select="/item/@term"/>
					</b>
				</p>
				<p>
					<xsl:choose>
						<xsl:when test="string-length(string(/item/def)) &gt; 0">
							<xsl:apply-templates select="/item/def"/>
						</xsl:when>
						<xsl:otherwise>
							<i>No definition available for this item.</i>
						</xsl:otherwise>
					</xsl:choose>
				</p>
				<xsl:if test="item/citation">
					<p>
						<i>References</i>
					</p>
					<ul>
						<xsl:for-each select="item/citation">
							<li>
								<xsl:apply-templates select="."/>
							</li>
						</xsl:for-each>
					</ul>
				</xsl:if>
<!--
Does not seem to navigate to it.  Will not use for now.  2006.08.15
				<p>For help on how to use the Morphosyntactic Gloss Assistant, click <a href="{$sHelpFile}">here</a>.</p>
				 -->
			</body>
		</html>
	</xsl:template>
	<xsl:template match="ul | li | p">
		<xsl:element name="{name()}">
			<xsl:apply-templates/>
		</xsl:element>
	</xsl:template>
	<xsl:template match="English">
		<i>
			<xsl:apply-templates/>
		</i>
	</xsl:template>
</xsl:stylesheet>
