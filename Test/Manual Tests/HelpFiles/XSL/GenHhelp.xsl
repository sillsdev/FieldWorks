<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0"
 xmlns:ms="urn:schemas-microsoft-com:xslt">
<xsl:param name="srcdir">default1\</xsl:param>
<xsl:param name="srcmbk">default2\</xsl:param>
<xsl:output method="text" encoding="UTF-8"/>
<!--
This transform creates a batch file that generates an HTML Help project from modular book xml files.
-->
<xsl:variable name="dir" select = "substring-before(substring-after(substring-after(substring-after($srcdir,'\'),'\'),'\'),'\')" />

<xsl:template match="/modularbook|/task|/overview">
<xsl:choose>
<xsl:when test="include-test">
 <xsl:apply-templates select="include-test[not(contains(.,'/') or contains(.,'\'))]"/>
</xsl:when>
<xsl:otherwise>
 <xsl:variable name="files">
  <!-- only task or overview modules are searched this way so include it in the list -->
  <file><xsl:value-of select="$srcmbk"/></file>
  <xsl:apply-templates select="//link|//step[@uri]"/>
 </xsl:variable>
 <xsl:text>@echo off
 rem Transform each xml page into an html help topic page
</xsl:text>
  <!-- apply to only unique file names as the list has duplicates in it -->
  <xsl:apply-templates select="ms:node-set($files)/file[not(preceding-sibling::file=.)]"/>
</xsl:otherwise>
</xsl:choose>
<xsl:text>
</xsl:text>

<xsl:text>
rem Use transforms to create a table of contents from the module titles
</xsl:text>
<xsl:value-of select = "concat('..\..\User\Tools\msxsl.exe ',$srcdir,$srcmbk,' ..\XSL\HelpTOC.xsl -o all.hhc')" />
<xsl:text>
rem Use transforms to create an index from the module index tag lists
</xsl:text>
<xsl:value-of select = "concat('..\..\User\Tools\msxsl.exe ',$srcdir,$srcmbk,' ..\XSL\HelpIndex.xsl -o all.hhk')" />
<xsl:text>
rem Use transforms to create a MS HTML Help Workshop project file from the module file attributes
</xsl:text>
<xsl:value-of select = "concat('..\..\User\Tools\msxsl.exe ',$srcdir,$srcmbk,' ..\XSL\HelpProj.xsl -o all.hhp')" />
<xsl:text>
Rem To create the helpfile from these files:
Rem Concatenate the Proj, TOC and Ind files to each other (intelligently) to get full Proj, TOC and Ind files.
Rem Run MS HTML Help Workshop and compile.
</xsl:text>
</xsl:template>

<xsl:template match="file|include-test">
 <xsl:variable name="base" select="substring-before(.,'.')"/>
 <xsl:value-of select="concat('call ..\XSL\GenHTML ',$srcdir,' ',$base,' ..\',$dir,'\',$base,'.htm')"/>
<xsl:text>
</xsl:text>
</xsl:template>

<!-- create a "file" element for each local link target-->
<xsl:template match="link|step">
 <xsl:if test="not(contains(@uri,'/') or contains(@uri,'\')) and contains(@uri,'.xml')">
  <file><xsl:value-of select="@uri"/></file>
  <xsl:apply-templates select="document(@uri)//link|document(@uri)//step[@uri]"/>
 </xsl:if>
</xsl:template>

<xsl:template match="titlePage"/>

</xsl:stylesheet>
