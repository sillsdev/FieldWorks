<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
 <xsl:output method="html" encoding="utf-16" version="4.0"/>

 <xsl:template match="/">
 here is some output
		<xsl:for-each select=".//WfiWordform">
			<p>Wordform id is <xsl:value-of select="@Id"/></p>
		</xsl:for-each>
  </xsl:template>
</xsl:stylesheet>
