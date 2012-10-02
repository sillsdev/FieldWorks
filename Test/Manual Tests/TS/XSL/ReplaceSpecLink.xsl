<?xml version="1.0"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<xsl:output method="xml" encoding="UTF-8" indent="yes"/>

 <xsl:variable name="linkPreFix">
  <xsl:text>http://lsdev.sil.org/intranet/FwSpecs/</xsl:text>
  <!--xsl:text>C:/fw/FwSpecs/</xsl:text-->
 </xsl:variable>

  <xsl:template match="*|@*|processing-instruction()|text()|comment()">
   <xsl:choose>
   <xsl:when test="local-name()='link' and contains(@uri,'FwSpecs')">
	<xsl:variable name="uriC" select="translate(@uri,'\','/')"/>
	<link>
	 <xsl:attribute name="uri">
	  <xsl:value-of select="$linkPreFix"/>
	  <xsl:value-of select="substring-after($uriC,'FwSpecs/')"/>
	 </xsl:attribute>
	 <xsl:if test="@type">
	  <xsl:attribute name="type">
	   <xsl:value-of select="@type"/>
	  </xsl:attribute>
	 </xsl:if>
	 <xsl:if test="@title">
	  <xsl:attribute name="title">
	   <xsl:value-of select="@title"/>
	  </xsl:attribute>
	 </xsl:if>
	 <xsl:apply-templates/>
	</link>
   </xsl:when>
   <xsl:otherwise>
	<xsl:copy>
	 <xsl:apply-templates select="*|@*|processing-instruction()|text()|comment()"/>
	</xsl:copy>
   </xsl:otherwise>
   </xsl:choose>
  </xsl:template>

</xsl:stylesheet>
