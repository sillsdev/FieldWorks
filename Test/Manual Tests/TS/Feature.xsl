<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
<xsl:output method="html" encoding="UTF-8" indent="yes"/>

<xsl:template match="/features">
 <html>
  <head>
   <title><xsl:value-of select="@name"/></title>
  </head>
  <body style="background: GOLDENROD">
   <div style="color: maroon">
   <h1><xsl:value-of select="@name"/></h1>
   <h2>from <a href="{@src}"><xsl:value-of select="@src"/></a></h2>
   <table border="1" style="background: KHAKI">
	<tr>
	 <th>Number</th>
	 <th>SL</th>
	 <th>Object</th>
	 <th>Name</th>
	</tr>
	<xsl:apply-templates select="f"/>
   </table>
   <xsl:apply-templates select="key"/>
   </div>
  </body>
 </html>
</xsl:template>

<xsl:template match="f">
	<tr>
	 <td>
	  <xsl:choose>
	  <xsl:when test="not(@id='')">
	   <xsl:choose>
	   <xsl:when test="not(''=@test)">
		<a href="{@test}" target="_new"><xsl:value-of select="document(@test)/test/@num"/></a>
	   </xsl:when>
	   <xsl:otherwise>
		<xsl:value-of select="@id"/>
	   </xsl:otherwise>
	   </xsl:choose>
	  </xsl:when>
	  <xsl:otherwise>
	   <xsl:text disable-output-escaping="yes">&amp;nbsp;</xsl:text>
	  </xsl:otherwise>
	  </xsl:choose>
	 </td>
	 <td>
	  <xsl:value-of select="//key[@name='Support Level']/k[current()/@sl=@sym]/@short" />
	 </td>
	 <td>
	  <xsl:variable name="image" select="//key[@name='Objects']/k[current()/@obj=@sym]/@short" />
	  <xsl:choose>
	  <xsl:when test="$image!=''">
	   <xsl:value-of select="$image" />
	  </xsl:when>
	  <xsl:otherwise>
	   <xsl:value-of select="//key[@name='Objects']/k[current()/@obj=@sym]"/>
	  </xsl:otherwise>
	  </xsl:choose>
	 </td>
	 <td>
	  <xsl:choose>
	  <xsl:when test="not(''=@src)">
	   <a href="{@src}" target="_new"><xsl:value-of select="@name"/></a>
	  </xsl:when>
	  <xsl:otherwise>
	   <xsl:value-of select="@name"/>
	  </xsl:otherwise>
	  </xsl:choose>
	 </td>
	</tr>
</xsl:template>

<xsl:template match="key">
 <h2><xsl:value-of select="@name"/></h2>
 <table style="background: GOLD">
  <tr><th>Symbol</th><th>Description</th></tr>
  <xsl:apply-templates/>
 </table>
</xsl:template>

<xsl:template match="k">
 <tr>
  <td><xsl:value-of select="@sym"/></td>
  <td><xsl:value-of select="."/></td>
 </tr>
</xsl:template>

</xsl:stylesheet>
