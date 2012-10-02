<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:fo="http://www.w3.org/1999/XSL/Format">
	<xsl:output method="xml" version="1.0" encoding="UTF-8"/>
	<xsl:template match="/">
		<html>
			<meta http-equiv="Content-Type" content="text/html; charset=windows-1252"/>
			<meta name="GENERATOR" content="Microsoft FrontPage 4.0"/>
			<meta name="ProgId" content="FrontPage.Editor.Document"/>
			<title>New Page 1</title>
			<style>
				<xsl:comment>
.ClassName   { font-size: 8pt }
.AttributeName { font-size: 8pt; text-align: Left; line-height: 100%; margin-left: 20;
			   margin-top: 0; margin-bottom: 0 }
</xsl:comment>
			</style>
			<body>
				<p>Numbers for Classes used in the following modules:</p>
				<xsl:apply-templates select="//CellarModule">
					<xsl:sort data-type="number" select="@num"/>
				</xsl:apply-templates>
				<xsl:apply-templates select="//class">
					<xsl:sort select="@id"/>
				</xsl:apply-templates>
			</body>
		</html>
	</xsl:template>
	<xsl:template match="//CellarModule">
		<ul>
			<li>
				<xsl:value-of select="@num"/>
				<xsl:text>&#32;</xsl:text>
				<b>
					<xsl:value-of select="@id"/>
				</b>
				<xsl:text>&#32;</xsl:text>
				<xsl:for-each select="class">
					<xsl:sort data-type="number" select="@num"/>
					<xsl:value-of select="@num"/>
					<xsl:text>,&#32;</xsl:text>
				</xsl:for-each>
			</li>
		</ul>
	</xsl:template>
	<xsl:template match="//class">
		<p class="ClassName">
			<b>
				<a target="_top">
					<xsl:attribute name="name">
						<xsl:value-of select="@id"/>
					</xsl:attribute>
				</a>
				<xsl:value-of select="@id"/>
			</b>
			<xsl:text>&#32;</xsl:text>
			<xsl:value-of select="@num"/>
			<xsl:text>&#32;Mod:</xsl:text>
			<xsl:value-of select="../@num"/>
		</p>
		<xsl:text>&#32;</xsl:text>
		<xsl:for-each select="props/*">
			<xsl:sort data-type="number" select="@num"/>
			<p class="AttributeName">
				<xsl:value-of select="@num"/>
				<xsl:text>&#32;</xsl:text>
				<b>
					<xsl:value-of select="@id"/>
				</b>
				<xsl:value-of select="@sig"/>
			</p>
		</xsl:for-each>
		<xsl:text>&#13;</xsl:text>
	</xsl:template>
</xsl:stylesheet>
