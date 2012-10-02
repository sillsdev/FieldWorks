<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<xsl:output method="text" version="1.0" encoding="UTF-8" indent="yes"/>
	<xsl:template match="GAFAWSData/WordRecords">
		<xsl:for-each select="WordRecord">
			<xsl:for-each select="Prefixes">
				<xsl:for-each select="Affix">
					<xsl:value-of select="concat(@MIDREF, '-')"/>
				</xsl:for-each>
			</xsl:for-each>
			<xsl:for-each select="Stem">
				<xsl:value-of select="@MIDREF"/>
			</xsl:for-each>
			<xsl:for-each select="Suffixes">
				<xsl:for-each select="Affix">
					<xsl:value-of select="concat('-', @MIDREF)"/>
				</xsl:for-each>
			</xsl:for-each>
			<xsl:text>&#xa;</xsl:text>
		</xsl:for-each>
	</xsl:template>
</xsl:stylesheet>
