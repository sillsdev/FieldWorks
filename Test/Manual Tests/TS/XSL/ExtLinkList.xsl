<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
<xsl:output method="html" encoding="UTF-8"/>
<xsl:template match="/modularbook">
 <html>
  <head>
   <title>External Links from <xsl:value-of select="titlePage/title"/></title>
  </head>
  <body>
   <H1 align="center">External Links from <xsl:value-of select="titlePage/title"/></H1>
   <table border="1" width="100%">
	<tr><th>File</th><th>Link Text</th><th>Link URI</th></tr>
	<xsl:apply-templates select="//link[@type='doc']"/>
   </table>
  </body>
 </html>
</xsl:template>

<xsl:template match="link">
 <tr>
  <td><xsl:value-of select="ancestor::test/@file"/></td>
  <td><xsl:value-of select="."/></td>
  <td><xsl:value-of select="@uri"/></td>
 </tr>
</xsl:template>

</xsl:stylesheet>