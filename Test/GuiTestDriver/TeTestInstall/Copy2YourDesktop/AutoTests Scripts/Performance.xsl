<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
xmlns:ms="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="ms">
<xsl:output method="xml" encoding="UTF-8" indent="yes" standalone="yes" />


<xsl:template match="performance">
  <html>
  <head>
	 <title>Performance of TE</title>
  </head>
  <body>
	<h1>Performance of TE</h1>
	<table border="1">
	  <tr><th>Action</th><th>Expected</th><th>Time</th><th>Date</th><th>Ver</th></tr>
	  <xsl:apply-templates select="action"/>
	</table>
	<table border="1">
	  <tr><th>Total</th><th>Date</th></tr>
	  <xsl:for-each select="action[2]/time">
		<xsl:variable name="pos" select="position()"/>
		<xsl:variable name="date" select="@date"/>
		<tr>
		  <td><xsl:value-of select="sum(//action/time[@date = $date]/@ellapsed) div 1000"/></td>
		  <td><xsl:value-of select="@date"/></td>
		</tr>
	  </xsl:for-each>
	</table>
  </body>
  </html>
</xsl:template>

<xsl:template match="action">
  <tr>
	<td>
	  <xsl:value-of select="@desc"/>
	</td>
	<td style="text-align:right">
	  <xsl:value-of select="time/@expected div 1000"/>
	</td>
	<td style="text-align:right">
	  <xsl:value-of select="format-number(time/@ellapsed div 1000, '#.000')"/>
	</td>
	<td>
	  <xsl:value-of select="time/@date"/>
	</td>
	<td>
	  <xsl:value-of select="time/@ver"/>
	</td>
  </tr>
  <xsl:apply-templates select="time[position() > 1]"/>
</xsl:template>

<xsl:template match="time">
  <tr>
	<td></td>
	<td></td>
	<td style="text-align:right">
	  <xsl:value-of select="format-number(@ellapsed div 1000, '#.000')"/>
	</td>
	<td>
	  <xsl:value-of select="@date"/>
	</td>
	<td>
	  <xsl:value-of select="@ver"/>
	</td>
  </tr>
</xsl:template>

<xsl:template match="app">
	<xsl:value-of select="@name"/>
</xsl:template>

<xsl:template match="*"/>

</xsl:stylesheet>

<!--performance>
  <action desc="stuff" time="6000" expected="8000">
	<app name="App title" />
  </action>
</performance-->