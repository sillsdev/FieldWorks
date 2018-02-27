<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:sfm="output.xsl">
  <xsl:output method="xml" version="1.0" encoding="UTF-8" indent="yes"/>
  <!--
================================================================
Convert SFM Import result to an XSLT transform to perform step 2
  Input:    XML output from SFMImport tool
  Output: Step 1 XSLT
================================================================
Revision History is at the end of this file.

- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Preamble
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:namespace-alias stylesheet-prefix="sfm" result-prefix="xsl"/>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Main template
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
<!--   <xsl:template match="/">-->
  <xsl:template match="/database">
	<!-- output header info -->
	<sfm:stylesheet>
	  <xsl:attribute name="version">1.0</xsl:attribute>
	  <xsl:element name="xsl:output">
		<xsl:attribute name="method">xml</xsl:attribute>
		<xsl:attribute name="version">1.0</xsl:attribute>
		<xsl:attribute name="encoding">utf-8</xsl:attribute>
		<xsl:attribute name="indent">yes</xsl:attribute>
	  </xsl:element>
	  <xsl:comment>
================================================================
DO NOT EDIT!!  This transform is automatically generated

Produce Phase 2 XML of SFM Import

  Input:    XML output from SFM Import tool
  Output: An XSLT that produces Phase 2
			   (Note: each possible parse is within its own seq element)
================================================================

- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Preamble
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
</xsl:comment>
	  <sfm:template match="/database">
		<sfm:element name="dictionary">
		  <sfm:attribute name="affixMarker">
			<xsl:choose>
			  <xsl:when test="setting[@affixMarker]">
				<xsl:value-of select="setting/@affixMarker"/>
			  </xsl:when>
			  <xsl:otherwise><xsl:text>-</xsl:text></xsl:otherwise>
			</xsl:choose>
		  </sfm:attribute>
		  <sfm:for-each select="//entry">
			<entry>
			  <sfm:apply-templates/>
			</entry>
		  </sfm:for-each>
		</sfm:element>
	  </sfm:template>

	  <!-- Only process each field that has a meaning[@id] that isn't empty START -->
	  <xsl:for-each select="//fieldDescriptions/field/meaning[@id!='']">
		<sfm:template>
		  <xsl:attribute name="match"><xsl:value-of select="../@sfm"/></xsl:attribute>
		  <sfm:element>
			<xsl:attribute name="name"><xsl:value-of select="@id"/></xsl:attribute>
			<xsl:if test="../@xml:lang">
			  <sfm:attribute name="ws">
				<xsl:value-of select="../@xml:lang"/>
			  </sfm:attribute>
			</xsl:if>
			<xsl:if test="../@abbr">
			  <sfm:attribute name="abbr">
				<xsl:value-of select="../@abbr"/>
			  </sfm:attribute>
			</xsl:if>
			<sfm:apply-templates/>
		  </sfm:element>
		</sfm:template>
	  </xsl:for-each>
	  <!-- Only process each field that has a meaning[@id] that isn't empty END -->

	  <!-- Create a template that does nothing for fields that are missing the id START -->
	  <xsl:for-each select="//fieldDescriptions/field/meaning[@id='']">
		<sfm:template>
		  <xsl:attribute name="match"><xsl:value-of select="../@sfm"/></xsl:attribute>
		  <sfm:comment>The marker '<xsl:value-of select="../@sfm"/>' is ignored due to empty meaning@id. </sfm:comment>
			<xsl:attribute name="name"><xsl:value-of select="@id"/></xsl:attribute>
		</sfm:template>
	  </xsl:for-each>
	  <!-- Only process each field that has a meaning[@id] that isn't empty END -->

	  <xsl:for-each select="//inFieldMarkers/ifm">
		<sfm:template>
		  <xsl:attribute name="match"><xsl:value-of select="@element"/></xsl:attribute>
		  <sfm:element>
			<xsl:attribute name="name"><xsl:value-of select="@element"/></xsl:attribute>
			<xsl:if test="@xml:lang">
			  <sfm:attribute name="ws">
				<xsl:value-of select="@xml:lang"/>
			  </sfm:attribute>
			</xsl:if>
			<sfm:apply-templates/>
		  </sfm:element>
		</sfm:template>
	  </xsl:for-each>
<!--
	  <sfm:template match="sense | examp | func | subentry">
		<sfm:copy>
		  <sfm:apply-templates/>
		</sfm:copy>
	  </sfm:template>
-->
	  <sfm:template match="sense | examp ">
		<sfm:copy>
		  <sfm:apply-templates/>
		</sfm:copy>
	  </sfm:template>

	</sfm:stylesheet>
  </xsl:template>
</xsl:stylesheet>
<!--

  <xsl:template match="lf">
	<xsl:element name="rel">
this
	  <xsl:attribute name="type"><xsl:value-of select="."/></xsl:attribute>
	  <xsl:attribute name="wsa"><xsl:value-of select="//field[@sfm='lf']/@xml:lang"/></xsl:attribute>
instead of
	  <xsl:attribute name="ws">en</xsl:attribute>
	  <xsl:apply-templates />

	</xsl:element>

================================================================
Revision History
- - - - - - - - - - - - - - - - - - -
03-Mar-2005    Andy Black  Began working on Initial Draft
28-Mar-2005	   dlh - modifications for the Abbr attribute.
================================================================
 -->
