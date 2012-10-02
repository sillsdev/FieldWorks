<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
<xsl:output method="html" encoding="UTF-8" indent="yes"/>
<xsl:param name="target">xml</xsl:param>

<xsl:template match="/">
 <xsl:processing-instruction name="xml-stylesheet">
  <xsl:text>type="text/xsl" </xsl:text>
  <xsl:text>href="..\..\XSL\HelpFile.xsl"</xsl:text>
 </xsl:processing-instruction>
 <xsl:text>

</xsl:text>
 <xsl:apply-templates select="*|@*|text()|comment()"/>
</xsl:template>

<xsl:template match="*|@*|processing-instruction()|text()|comment()">
 <xsl:copy>
  <xsl:apply-templates select="*|@*|processing-instruction()|text()|comment()"/>
 </xsl:copy>
</xsl:template>

</xsl:stylesheet>