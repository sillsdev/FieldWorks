<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0" xmlns:ms="urn:schemas-microsoft-com:xslt">
<xsl:output method="text" omit-xml-declaration="yes" encoding="UTF-8" indent="yes"/>
<xsl:param name="top" select="'false'"/>

<!--
This transform creates an HTML Help file Index.
It turns each module index list item into a text/sitemap object.
They are sorted and when compiled by MS HTML Help Workshop, those of the same name are merged.
This transform need not merge them.
Later, other types of tags or attributes may be included in the index.
-->

<xsl:template match="/">
 <!-- create a list of files to parse in for this section -->
 <xsl:variable name="files">
  <xsl:call-template name="make-file-list"/>
 </xsl:variable>
 <!-- read only unique files; the list has duplicates in it -->
 <xsl:variable name="content"  select="document(ms:node-set($files)/file[not(preceding-sibling::file=.)])"/>
  <xsl:apply-templates select="$content/*//index/li">
   <xsl:sort select="."/>
  </xsl:apply-templates>
</xsl:template>

<xsl:include href="ListFiles.xsl"/>

<xsl:template match="index/li">
 <xsl:variable name="path" select="substring-before(../../@file,'\')"/>
 <xsl:call-template name="IndItem">
  <xsl:with-param name="pinditem" select="."/>
  <xsl:with-param name="ptitle" select="ancestor::*/title"/>
  <xsl:with-param name="plink" select="concat(substring-before(../../@file,'.'),'.htm')"/>
 </xsl:call-template>
</xsl:template>

<xsl:template name="IndItem">
<xsl:param name="pinditem" select="'*NO ITEM*'"/>
<xsl:param name="ptitle" select="'*NO TITLE*'"/>
<xsl:param name="plink" select="TBD.htm"/>
 <xsl:text>
 &lt;li>&lt;object type="text/sitemap">
   &lt;param name="Name" value="</xsl:text><xsl:value-of select="$pinditem"/><xsl:text>"/>
   &lt;param name="Name" value="</xsl:text><xsl:value-of select="$ptitle"/><xsl:text>"/>
   &lt;param name="Local" value="</xsl:text><xsl:value-of select="$plink"/><xsl:text>"/>
  &lt;/object>
 &lt;/li></xsl:text>
</xsl:template>

<xsl:template match="*"/>

</xsl:stylesheet>
