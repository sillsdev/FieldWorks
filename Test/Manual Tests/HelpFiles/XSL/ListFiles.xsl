<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">

<!-- Creates a list of files linked in the same directory via "link" or "include-test" elements to the modularbook, task or overview in the current context.
Call-template make-file-list in a variable element and process the variable using ms:node-set().
xmlns:ms="urn:schemas-microsoft-com:xslt" must be included in the stylesheet start tag.

example;
 </!/-/- create a list of files to parse in for this section -/-/>
 <xsl:variable name="files">
  <xsl:call-template name="make-file-list"/>
 </xsl:variable>
 </!/-/- read only unique files; the list has duplicates in it -/-/>
 <xsl:variable name="content"  select="document(ms:node-set($files)/file[not(preceding-sibling::file=.)])"/>

<xsl:include href="ListFiles.xsl"/>
-->

<xsl:template name="make-file-list">
 <xsl:choose>
 <xsl:when test="//include-test">
  <xsl:apply-templates select="//include-test[not(contains(.,'/') or contains(.,'\'))]"/>
 </xsl:when>
 <xsl:otherwise>
  <!-- only task or overview modules are searched this way. Include it in the list too -->
  <file><xsl:value-of select="(substring-after(@file,'\'))"/></file>
  <xsl:apply-templates select="//link|//step[@uri]"/>
 </xsl:otherwise>
 </xsl:choose>
</xsl:template>

<!-- create a "file" element for each local link target-->
<xsl:template match="include-test">
 <file><xsl:value-of select="."/></file>
</xsl:template>

<!-- create a "file" element for each local link target-->
<xsl:template match="link|step">
 <xsl:if test="not(contains(@uri,'/') or contains(@uri,'\')) and contains(@uri,'.xml')">
  <file><xsl:value-of select="@uri"/></file>
  <xsl:apply-templates select="document(@uri)//link|document(@uri)//step[@uri]"/>
 </xsl:if>
</xsl:template>

</xsl:stylesheet>
