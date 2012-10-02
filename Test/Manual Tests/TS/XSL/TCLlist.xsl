<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0" xmlns:ms="urn:schemas-microsoft-com:xslt">
<xsl:output method="html" encoding="UTF-8" indent="yes"/>
<xsl:param name="target">xml</xsl:param>

<xsl:template match="/modularbook">

<html>
<head>
<title><xsl:value-of select="titlePage/title"/></title>
</head>

<body>

<xsl:apply-templates select="titlePage/title"/>

<H1 align="center">Test Script Summary</H1>

<table border="1">
 <tr><th>Number</th><th>Script</th><th>Test Cases</th><th>Steps</th><th>Bugs</th><th>To Dos</th><th>Status</th><th>Author</th><th>Last Update</th></tr>

 <xsl:apply-templates select="document(include-test)">
  <xsl:sort select="test/@num"/>
 </xsl:apply-templates>

 <xsl:variable name="test-tree">
  <root>
   <xsl:copy-of select="document(include-test)"/>
  </root>
 </xsl:variable>
 <xsl:call-template name="StatsRow">
  <xsl:with-param name="tests" select="$test-tree"/>
 </xsl:call-template>

</table>

</body>
</html>
</xsl:template>

<xsl:template name="StatsRow">
<xsl:param name="tests"/>
 <tr>
  <td><xsl:value-of select="count(ms:node-set($tests)//test)"/></td>
  <th>Totals</th>
  <td align="center"><xsl:value-of select="count(ms:node-set($tests)//case)"/></td>
  <td align="center"><xsl:value-of select="count(ms:node-set($tests)//step)"/></td>
  <td align="center"><xsl:value-of select="count(ms:node-set($tests)//p[@name='bug'])"/></td>
  <td align="center"><xsl:value-of select="count(ms:node-set($tests)//p[@name='todo'])"/></td>
  <td align="center"><xsl:value-of select="count(ms:node-set($tests)//test/@ready)"/></td>
  <td></td><td></td>
 </tr>
</xsl:template>

<xsl:template match="titlePage/title">
 <h1 align="center"><xsl:value-of select="."/></h1>
</xsl:template>

<xsl:template match="test">
 <tr>
  <th align="left"><xsl:value-of select="@num"/></th>
  <th align="left">
   <a>
	<xsl:attribute name="href">
	 <xsl:text>..\</xsl:text><xsl:value-of select="substring-before(@file,'.')"/>
	 <xsl:text>.</xsl:text>
	 <xsl:choose>
	 <xsl:when test="'html'=$target">
	  <xsl:text>htm</xsl:text>
	 </xsl:when>
	 <xsl:otherwise>
	  <xsl:value-of select="substring-after(@file,'.')"/>
	 </xsl:otherwise>
	 </xsl:choose>
	</xsl:attribute>
	<xsl:apply-templates select="title"/>
   </a>
   <xsl:text> (</xsl:text>
   <xsl:value-of select="@id"/>
   <xsl:text>)</xsl:text>
  </th>
  <td align="center"><xsl:value-of select="count(.//case)"/></td>
  <td align="center"><xsl:value-of select="count(.//step)"/></td>
  <td align="center"><xsl:value-of select="count(.//p[@name='bug'])"/></td>
  <td align="center"><xsl:value-of select="count(.//p[@name='todo'])"/></td>
  <td align="center">
   <xsl:choose>
   <xsl:when test="@ready">
	<xsl:value-of select="@ready"/>
   </xsl:when>
   <xsl:otherwise>
	<xsl:text>Use</xsl:text>
   </xsl:otherwise>
   </xsl:choose>
  </td>
  <td align="center"><xsl:value-of select=".//update[1]/@author"/></td>
  <td align="center"><xsl:value-of select=".//update[last()]/@author"/></td>
 </tr>
</xsl:template>

<xsl:template match="test/title">
 <xsl:value-of select="."/>
</xsl:template>

</xsl:stylesheet>