<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:sfm="output.xsl" xmlns:msxsl="urn:schemas-microsoft-com:xslt" xmlns:user="urn:my-scripts">
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
	  <!--
	  <xsl:element name="xsl:strip-space">
		<xsl:attribute name="elements">Variant Example Subentry Entry Sense</xsl:attribute>
	  </xsl:element>
-->
<!-- Preserve all the spaces in the input sfm fields. -->
<!-- xsl:preserve-space elements="lx gl ex "/-->
	  <xsl:element name="xsl:preserve-space">
		<xsl:attribute name="elements">
		  <xsl:for-each select="/database/fieldDescriptions/field">
			<xsl:choose>
			  <xsl:when test="@autoSfm">
				<xsl:value-of select="@autoSfm"/>
			  </xsl:when>
			  <xsl:otherwise>
				<xsl:value-of select="@sfm"/>
			  </xsl:otherwise>
			</xsl:choose>
			<xsl:text>&#32;</xsl:text>
		  </xsl:for-each>
		</xsl:attribute>
	  </xsl:element>

	  <xsl:comment>
		================================================================
		DO NOT EDIT!!        This transform is automatically generated

		Produce Phase 2 XML of SFM Import
		Input: XML output from SFM Import tool
		Output: An XSLT that produces Phase 2 (Note: each possible parse is within its own seq element)
		================================================================
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		Preamble
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  </xsl:comment>


	  <sfm:template match="/database">
		<!-- sfm:element name="database" -->
		  <!-- added another level of element so that custom fields can be a child of it -->
		  <sfm:element name="dictionary">
			<sfm:for-each select="//Entry">
			  <sfm:element name="Entry">
				<sfm:attribute name="guid">
				  <xsl:text disable-output-escaping="yes">&lt;xsl:text&gt;I&lt;/xsl:text&gt;&lt;xsl:value-of select="generate-id()"/&gt;</xsl:text>
				</sfm:attribute>
				<sfm:apply-templates/>
			  </sfm:element>
			</sfm:for-each>
		  <!-- /sfm:element -->
		  <!-- Now add the CustomFieldDescirptions section 06-08 -->
		  <xsl:if test="//CustomFieldDescriptions/CustomField">
			<sfm:element name="CustomFields">
			  <sfm:for-each select="//CustomField">
					 <sfm:copy-of select="."/>
			  </sfm:for-each>
			</sfm:element>
		  </xsl:if>
		</sfm:element>
	  </sfm:template>

	  <!-- Only process each field that has a meaning[@id] that isn't empty START -->
	  <!-- xsl:for-each select="//fieldDescriptions/field/meaning[@id!='']" -->
	  <xsl:for-each select="//field/meaning[@id!='']">
		  <sfm:template>
		  <!-- Determine what the match value should be. If autofield include the class name. -->
		  <xsl:variable name="safeSfm">
			<xsl:choose>
			  <xsl:when test="../@autoSfm">
				<xsl:value-of select="../@autoSfm"/>
			  </xsl:when>
			  <xsl:otherwise>
				<xsl:value-of select="../@sfm"/>
			  </xsl:otherwise>
			</xsl:choose>
		  </xsl:variable>
		  <xsl:variable name="matchName">
			<xsl:choose>
			  <xsl:when test="../@autoImportClassName">
				<xsl:value-of select="../@autoImportClassName"/>
				<xsl:text>/</xsl:text>
				<!--                  <xsl:value-of select="../@sfm"/> -->
				<xsl:value-of select="$safeSfm"/>
			  </xsl:when>
			  <xsl:otherwise>
				<xsl:value-of select="$safeSfm"/>
			  </xsl:otherwise>
			  <!--                <xsl:otherwise><xsl:value-of select="../@sfm"/></xsl:otherwise> -->
			</xsl:choose>
		  </xsl:variable>
		  <!-- Special processing for autofields -->
		  <!-- Now regular fields -->
		  <xsl:attribute name="match">
			<xsl:value-of select="$matchName"/>
		  </xsl:attribute>

		  <sfm:element>
			<xsl:choose>
			  <xsl:when test="ancestor::CustomField">
				<xsl:attribute name="name">Custom</xsl:attribute>
			  </xsl:when>
			  <xsl:otherwise>
				<xsl:attribute name="name"><xsl:value-of select="@id"/></xsl:attribute>
			  </xsl:otherwise>
			</xsl:choose>

			<!-- Add any additional needed attributes for the custom field here -->
			<xsl:if test="ancestor::CustomField">
			  <sfm:attribute name="fwid"><xsl:value-of select="@id"/></sfm:attribute>
			  <sfm:attribute name="type"><xsl:value-of select="../@type"/></sfm:attribute>
			  <sfm:attribute name="sortKey"><xsl:value-of select="@id"/>_<xsl:value-of select="../@xml:lang"/></sfm:attribute>
			</xsl:if>


			<!-- xsl:attribute name="name">
			  <xsl:choose>
				<xsl:when test="ancestor::CustomField">
				  <xsl:text>Custom</xsl:text>
				</xsl:when>
				<xsl:otherwise>
				  <xsl:value-of select="@id"/>
				</xsl:otherwise>
			  </xsl:choose>
			</xsl:attribute -->
			<!-- guid attribute on child elements if desired at some point -->
			<!-- sfm:attribute name="guid">
				  <xsl:text disable-output-escaping="yes">&lt;xsl:text&gt;I&lt;/xsl:text&gt;&lt;xsl:value-of select="generate-id()"/&gt;</xsl:text>
			  </sfm:attribute -->

			<xsl:if test="../@sfm">
			  <sfm:attribute name="sfm">
				<!-- Here we don't want to put out the auto-generated sfm, use the orig even if invalid -->
				<xsl:value-of select="../@sfm"/>
				<!--                <xsl:value-of select="$safeSfm"/> -->
			  </sfm:attribute>
			</xsl:if>
			<xsl:if test="@func">
			  <sfm:attribute name="func">
				<xsl:value-of select="@func"/>
			  </sfm:attribute>
			</xsl:if>
			<xsl:if test="@funcWS">
			  <sfm:attribute name="funcWS">
				<xsl:value-of select="@funcWS"/>
			  </sfm:attribute>
			</xsl:if>
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
			<sfm:if test="@morphTypeWs">
			  <sfm:attribute name="morphTypeWs">
				<sfm:value-of select="@morphTypeWs"/>
			  </sfm:attribute>
			</sfm:if>
			<sfm:if test="@morphType">
			  <sfm:attribute name="morphType">
				<sfm:value-of select="@morphType"/>
			  </sfm:attribute>
			</sfm:if>
			<sfm:if test="@allomorphClass">
			  <sfm:attribute name="allomorphClass">
				<sfm:value-of select="@allomorphClass"/>
			  </sfm:attribute>
			</sfm:if>
			<sfm:if test="@varTypeWs">
			  <sfm:attribute name="varTypeWs">
				<sfm:value-of select="@varTypeWs"/>
			  </sfm:attribute>
			</sfm:if>
			<sfm:if test="@varType">
			  <sfm:attribute name="varType">
				<sfm:value-of select="@varType"/>
			  </sfm:attribute>
			</sfm:if>
			<sfm:if test="@subTypeWs">
			  <sfm:attribute name="subTypeWs">
				<sfm:value-of select="@subTypeWs"/>
			  </sfm:attribute>
			</sfm:if>
			<sfm:if test="@subType">
			  <sfm:attribute name="subType">
				<sfm:value-of select="@subType"/>
			  </sfm:attribute>
			</sfm:if>

			<sfm:apply-templates/>
		  </sfm:element>
		</sfm:template>
	  </xsl:for-each>
	  <!-- Only process each field that has a meaning[@id] that isn't empty END -->

	  <!-- Create a template that does nothing for fields that are missing the id START -->
	  <xsl:for-each select="//fieldDescriptions/field/meaning[@id='']">
		<sfm:template>

		  <!-- Determine what the match value should be. If autofield include the class name. -->
		  <xsl:variable name="safeSfm">
			<xsl:choose>
			  <xsl:when test="../@autoSfm">
				<xsl:value-of select="../@autoSfm"/>
			  </xsl:when>
			  <xsl:otherwise>
				<xsl:value-of select="../@sfm"/>
			  </xsl:otherwise>
			</xsl:choose>
		  </xsl:variable>
		  <xsl:variable name="matchName">
			<xsl:choose>
			  <xsl:when test="../@autoImportClassName">
				<xsl:value-of select="../@autoImportClassName"/>
				<xsl:text>/</xsl:text>
				<xsl:value-of select="$safeSfm"/>
			  </xsl:when>
			  <xsl:otherwise>
				<xsl:value-of select="$safeSfm"/>
			  </xsl:otherwise>
			</xsl:choose>
		  </xsl:variable>
		  <!-- Special processing for autofields -->
		  <!-- Now regular fields -->
		  <xsl:attribute name="match">
			<xsl:value-of select="$matchName"/>
		  </xsl:attribute>
		  <!--          <xsl:attribute name="match"><xsl:value-of select="../@sfm"/></xsl:attribute> -->
		  <!-- sfm:comment>The marker '<xsl:value-of select="../@sfm"/>' is ignored due to empty meaning@id. </sfm:comment -->

		  <xsl:if test="@id and not(@id='')">
			<xsl:attribute name="name">
			  <xsl:value-of select="@id"/>
			</xsl:attribute>
		  </xsl:if>
		  <sfm:comment>The marker '<xsl:value-of select="../@sfm"/>' is ignored due to empty
			meaning@id. </sfm:comment>
		</sfm:template>
	  </xsl:for-each>
	  <!-- Only process each field that has a meaning[@id] that isn't empty END -->

	  <xsl:for-each select="//inFieldMarkers/ifm">
		<sfm:template>
		  <xsl:attribute name="match">
			<xsl:value-of select="@element"/>
		  </xsl:attribute>
		  <sfm:element>
			<!--            <xsl:attribute name="name"><xsl:value-of select="@element"/></xsl:attribute> -->
			<xsl:attribute name="name">InFieldMarker</xsl:attribute>
			<sfm:attribute name="beginMarker">
			  <xsl:value-of select="@begin"/>
			</sfm:attribute>
			<xsl:if test="@end">
			  <sfm:attribute name="endMarker">
				<xsl:value-of select="@end"/>
			  </sfm:attribute>
			</xsl:if>
			<xsl:if test="@xml:lang">
			  <sfm:attribute name="ws">
				<xsl:value-of select="@xml:lang"/>
			  </sfm:attribute>
			</xsl:if>
			<xsl:if test="@lang">
			  <sfm:attribute name="ws">
				<xsl:value-of select="@lang"/>
			  </sfm:attribute>
			</xsl:if>
			<xsl:if test="@style">
			  <sfm:attribute name="style">
				<xsl:value-of select="@style"/>
			  </sfm:attribute>
			</xsl:if>
			<xsl:if test="@ignore">
			  <sfm:attribute name="ignore">
				<xsl:value-of select="@ignore"/>
			  </sfm:attribute>
			</xsl:if>


			<sfm:apply-templates/>
		  </sfm:element>
		</sfm:template>
	  </xsl:for-each>

	  <sfm:template
		match=" Sense | Example | Function | Subentry | Variant | Etymology | Picture | Pronunciation | SemanticDomain">
		<xsl:comment> Only elements that have content are copied and propigated through the xsl's.
		  All high level Elements are now 'tagged' with a GUID for future identification and
		  reference. </xsl:comment>
		<sfm:if test="@* or ''!=.">
		  <sfm:copy>
			<sfm:attribute name="guid">
			  <xsl:text disable-output-escaping="yes">&lt;xsl:text&gt;I&lt;/xsl:text&gt;&lt;xsl:value-of select="generate-id()"/&gt;</xsl:text>
			</sfm:attribute>
			<sfm:apply-templates/>
		  </sfm:copy>
		</sfm:if>
	  </sfm:template>

	  <!-- New Code START  -->
	  <!-- Match with each Sense element that doesn't have a 'ps' element -->
	  <sfm:template match="Sense[not(ps)]">

		<!-- At a minimum we will copy this element, so start with the element name -->
		<sfm:element name="Sense">
		  <sfm:if test="@* or ''!=.">
			<sfm:attribute name="guid">
			  <xsl:text disable-output-escaping="yes">&lt;xsl:text&gt;I&lt;/xsl:text&gt;&lt;xsl:value-of select="generate-id()"/&gt;</xsl:text>
			</sfm:attribute>

		  <!--
			Look for a preceding Sense element with a ps element that is at the same level as this node.
		  -->
		  <sfm:choose>
			<sfm:when test="parent::Entry">
			  <sfm:if test="preceding-sibling::Sense/ps">
				<sfm:variable name="withPS" select="preceding-sibling::Sense/ps"/>
				<sfm:comment>** copied 'ps'  **</sfm:comment>
				<sfm:element name="eires"><sfm:attribute name="ws">en</sfm:attribute>Review the Grammatical Info of senses.</sfm:element>
				<!--sfm:copy-of select="$withPS[count($withPS)]"/-->
				<sfm:apply-templates select="$withPS[count($withPS)]"/>
			  </sfm:if>
			</sfm:when>
		  </sfm:choose>
		   <sfm:apply-templates select="*|@*|text()|comment()"/>
		  </sfm:if>
		</sfm:element>
	  </sfm:template>
	  <!-- END -->

	</sfm:stylesheet>
  </xsl:template>
</xsl:stylesheet>
<!--

================================================================
Revision History
- - - - - - - - - - - - - - - - - - -
03-Mar-2005    Andy Black  Began working on Initial Draft
28-Mar-2005	   dlh - modifications for the Abbr attribute.
20-Jun-2005	   dlh - modifications new class names for 'Entry',
				 'Sense' and 'Example'.
17-Aug-2005    dlh - modifications for the auto fields.
16-Nov-2005	   dlh - adding restriction that elements have to have content to be passed on.
================================================================
-->
