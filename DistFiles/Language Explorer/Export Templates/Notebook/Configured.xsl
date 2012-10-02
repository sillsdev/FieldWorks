<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="xml" version="1.0" encoding="utf-8" indent="yes"/>

<!-- This stylesheet works on the output of Language Explorer/Notebook to produce an XML file
	 which is somewhat more like the standard dump XML file in its
	 treatment of cross-references.
 -->

<!--
	Strip all white space and leave it up to the stylesheet text elements below to put in
	appropriate spacing.
 -->

<xsl:strip-space elements="*"/>
<xsl:preserve-space elements="Run AUni Uni"/><!-- but these spaces are significant! -->

<!-- This is the basic default processing. -->

<xsl:template match="*">
  <xsl:copy>
	<xsl:copy-of select="@*"/>
	<xsl:text>&#13;&#10;</xsl:text>
	<xsl:apply-templates/>
  </xsl:copy><xsl:text>&#13;&#10;</xsl:text>
</xsl:template>

<!-- Insert a newline before the top element. -->

<xsl:template match="ExportedNotebook">
  <xsl:text>&#13;&#10;</xsl:text>
  <xsl:copy>
	<xsl:copy-of select="@*"/>
	<xsl:text>&#13;&#10;</xsl:text>
	<xsl:apply-templates/>
  </xsl:copy><xsl:text>&#13;&#10;</xsl:text>
</xsl:template>

<!-- Preserve the space inside a Run element. -->

<xsl:template match="Run|AUni|Uni">
  <xsl:copy>
	<xsl:copy-of select="@*"/>
	<xsl:apply-templates/>
  </xsl:copy><xsl:text>&#13;&#10;</xsl:text>
</xsl:template>

<!-- Remove the Paragraph element level. -->

<xsl:template match="Paragraph">
  <xsl:apply-templates/>
</xsl:template>

<!-- Remove the LiteralString elements (including their contents). -->

<xsl:template match="LiteralString">
</xsl:template>

<!-- Process the various link elements. -->

<xsl:template match="CmAnthroItemLink|CmPersonLink|CmLocationLink|CmPossibilityLink">
	<xsl:call-template name="ProcessPossibilityLink">
	  <xsl:with-param name="AbbrNode" select="CmPossibility_Abbreviation"/>
	  <xsl:with-param name="NameNode" select="CmPossibility_Name"/>
	  <xsl:with-param name="AttrPrefix"></xsl:with-param>
	</xsl:call-template>
</xsl:template>

<!-- Merge child $NameNode/AStr and $AbbrNode/AStr elements into a single Link element with
	 embedded Alt elements handling possible multilingual references. -->

<xsl:template name="ProcessPossibilityLink">
  <xsl:param name="AbbrNode"/>
  <xsl:param name="NameNode"/>
  <xsl:param name="AttrPrefix"/>
  <Link><xsl:text>&#13;&#10;</xsl:text>
	<xsl:for-each select="$AbbrNode/AStr">
	  <xsl:variable name="wsAbbr">
		<xsl:value-of select="@ws"/>
	  </xsl:variable>
	  <Alt>
		<xsl:attribute name="ws">
		  <xsl:value-of select="@ws"/>
		</xsl:attribute>
		<xsl:attribute name="{$AttrPrefix}abbr">
		  <xsl:value-of select="Run"/>
		</xsl:attribute>
		<xsl:for-each select="$NameNode/AStr[@ws=$wsAbbr]">
		  <xsl:attribute name="{$AttrPrefix}name">
			<xsl:value-of select="Run"/>
		  </xsl:attribute>
		</xsl:for-each>
	  </Alt><xsl:text>&#13;&#10;</xsl:text>
	</xsl:for-each>
	<xsl:for-each select="$NameNode/AStr">
	  <xsl:variable name="wsName">
		<xsl:value-of select="@ws"/>
	  </xsl:variable>
	  <xsl:if test="not($AbbrNode/AStr[@ws=$wsName])">
		<Alt>
		  <xsl:attribute name="ws">
			<xsl:value-of select="@ws"/>
		  </xsl:attribute>
		  <xsl:attribute name="{$AttrPrefix}name">
			<xsl:value-of select="Run"/>
		  </xsl:attribute>
		</Alt><xsl:text>&#13;&#10;</xsl:text>
	  </xsl:if>
	</xsl:for-each>
  </Link><xsl:text>&#13;&#10;</xsl:text>
</xsl:template>

</xsl:stylesheet>
