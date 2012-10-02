<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0" xmlns:ms="urn:schemas-microsoft-com:xslt">
<xsl:output method="text" omit-xml-declaration="yes" encoding="UTF-8" indent="yes"/>
<xsl:param name="top" select="'false'"/>

<!--
This transform creates an HTML Help project file (*.hhp).
-->

<xsl:template match="/">
 <!-- create a list of files to parse in for this section -->
 <xsl:variable name="files">
  <xsl:call-template name="make-file-list"/>
 </xsl:variable>
 <!-- read only unique files; the list has duplicates in it -->
 <xsl:variable name="content" select="document(ms:node-set($files)/file[not(preceding-sibling::file=.)])"/>
 <xsl:if test="'true'=$top">
  <xsl:variable name="proj" select="'TestHelp'"/>
  <xsl:text>
[OPTIONS]
Compatibility=1.1 or later
</xsl:text>
  <xsl:value-of select = "concat('Compiled file=',$proj,'.chm')" />
  <xsl:text>
</xsl:text>
  <xsl:value-of select = "concat('Contents file=',$proj,'.hhc')" />
  <xsl:text>
</xsl:text>
  <xsl:value-of select = "'Default topic=TitlePage.htm'" />
<xsl:text>
Display compile progress=No
Full-text search=Yes
</xsl:text>
  <xsl:value-of select = "concat('Index file=',$proj,'.hhk')" />
  <xsl:text>
Language=0x409 English (United States)
Title=</xsl:text><xsl:value-of select = "titlePage/title"/>
  <xsl:text>

[FILES]
TitlePage.htm</xsl:text>
 </xsl:if>
 <xsl:if test="modularbook">
  <xsl:variable name="path" select="substring-before(modularbook/@file,'\')"/>
  <xsl:text>
</xsl:text>
  <xsl:value-of select="concat($path,'\TestList.htm')"/>
  <xsl:text>
</xsl:text>
  <xsl:value-of select="concat($path,'\PatList.htm')"/>
 </xsl:if>
 <xsl:apply-templates select="$content/*"/>
</xsl:template>

<xsl:include href="ListFiles.xsl"/>

<xsl:template match="overview | task | concept | structure | fact | process | principle | story | training | test">
<xsl:text>
</xsl:text>
 <xsl:value-of select ="concat(substring-before (@file,'.'),'.htm')"/>
</xsl:template>

<xsl:template match="*"/>

</xsl:stylesheet>
