<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0" xmlns:ms="urn:schemas-microsoft-com:xslt">
<xsl:output method="text" omit-xml-declaration="yes" encoding="UTF-8" indent="yes"/>
<xsl:param name="top" select="'false'"/>

<!--
This transform creates an HTML Help file Table Of Contents (TOC).
It turns each module heading into a text/sitemap object.
-->

<xsl:template match="/">
 <!-- create a list of files to parse in for this section -->
 <xsl:variable name="files">
  <xsl:call-template name="make-file-list"/>
 </xsl:variable>
 <!--xsl:for-each select="ms:node-set($files)/file">
  <xsl:value-of select="."/>
  <xsl:text>
  </xsl:text>
 </xsl:for-each-->
 <!-- read only unique files; the list has duplicates in it -->
 <xsl:variable name="content"  select="document(ms:node-set($files)/file[not(preceding-sibling::file=.)])"/>
 <!--xsl:for-each select="$content/*">
  <xsl:value-of select="@file"/>
  <xsl:text>
  </xsl:text>
 </xsl:for-each-->
 <xsl:choose>
 <xsl:when test="modularbook">
  <xsl:apply-templates select="modularbook/titlePage"/><xsl:text>
   &lt;ul>
	</xsl:text><xsl:apply-templates select="$content/*"/>
	<xsl:call-template name="TocItem">
	 <xsl:with-param name="ptitle" select="'Bug Patterns'"/>
	 <xsl:with-param name="plink" select="concat(substring-before(modularbook/@file,'\'),'\PatList.htm')"/>
	</xsl:call-template>
	<xsl:text>
   &lt;/ul>
 </xsl:text>
 </xsl:when>
 <xsl:otherwise>
  <xsl:variable name="title1" select="*/title"/>
  <xsl:call-template name="TocItem">
   <xsl:with-param name="ptitle" select="$title1"/>
   <xsl:with-param name="plink" select="concat(substring-before(*/@file,'.'),'.htm')"/>
  </xsl:call-template>
  <!--xsl:value-of select="count($content/*[$title1!=title])"/-->
  <xsl:if test="count($content/*[$title1!=title]) &gt; 1">
   <xsl:text>
  &lt;ul>
	</xsl:text><xsl:apply-templates select="$content/*[$title1!=title]"/>
	<xsl:text>
  &lt;/ul>
</xsl:text>
  </xsl:if>
 </xsl:otherwise>
 </xsl:choose>
</xsl:template>

<xsl:include href="ListFiles.xsl"/>

<xsl:template match="titlePage">
  <xsl:call-template name="TocItem">
   <xsl:with-param name="ptitle" select="title"/>
   <xsl:with-param name="plink">
	<xsl:value-of select="concat(substring-before(@file,'\'),'\')"/>
	<xsl:text>TestList.htm</xsl:text>
   </xsl:with-param>
  </xsl:call-template>
</xsl:template>

<xsl:template match="task|overview|test|fact|concept">
 <xsl:call-template name="TocItem">
  <xsl:with-param name="ptitle" select="title"/>
  <xsl:with-param name="plink" select="concat(substring-before(@file,'.'),'.htm')"/>
 </xsl:call-template>
</xsl:template>

<xsl:template name="TocItem">
<xsl:param name="ptitle" select="'*NO TITLE*'"/>
<xsl:param name="plink" select="TBD.htm"/>
 <xsl:text>
  &lt;li>&lt;object type="text/sitemap">
	&lt;param name="Name" value="</xsl:text><xsl:value-of select="$ptitle"/><xsl:text>"/>
	&lt;param name="Local" value="</xsl:text><xsl:value-of select="$plink"/><xsl:text>"/>
   &lt;/object>
  &lt;/li></xsl:text>
</xsl:template>

<xsl:template match="*"/>

</xsl:stylesheet>
