<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
   <xsl:output method="xml" version="1.0" encoding="UTF-8" indent="yes" doctype-system="eticGlossList.dtd"/>
   <!--
================================================================
Master Gloss List to Etic GLoss List mapper.
  Input:    Master Gloss List XML file
  Output: Etic Gloss List XML file
================================================================
Revision History is at the end of this file.

- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Preamble
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
   -->
   <xsl:key name="Unknowns" match="//Unknown" use="LangCode"/>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Main template (change name of top node; add DTD)
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template match="FeatureCatalog">
	  <eticGlossList>
		 <xsl:for-each select="@*">
			<xsl:copy/>
		 </xsl:for-each>
		 <xsl:apply-templates/>
	  </eticGlossList>
	  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Main copy template
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   </xsl:template>
   <xsl:template match="@*|*">
	  <xsl:copy>
		 <xsl:apply-templates select="@afterSeparator | @complexNameSeparator | @complexNameFirst"/>
		 <xsl:apply-templates select="@*"/>
		 <xsl:apply-templates/>
		 <xsl:if test="name(.)='item' and @type='value' or name(.)='item' and @type='xref'">
			<xsl:call-template name="CreateFS"/>
		 </xsl:if>
	  </xsl:copy>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Map element names
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template match="Name">
	  <term>
		 <xsl:attribute name="ws">
			<xsl:value-of select="../LangCode"/>
		 </xsl:attribute>
		 <xsl:value-of select="normalize-space(.)"/>
	  </term>
   </xsl:template>
   <xsl:template match="Abbreviation">
	  <abbrev>
		 <xsl:attribute name="ws">
			<xsl:value-of select="../LangCode"/>
		 </xsl:attribute>
		 <xsl:value-of select="normalize-space(.)"/>
	  </abbrev>
   </xsl:template>
   <xsl:template match="Description">
	  <xsl:if test="string-length(.) &gt; 0">
		 <def>
			<xsl:attribute name="ws">
			   <xsl:value-of select="../LangCode"/>
			</xsl:attribute>
			<xsl:value-of select="normalize-space(.)"/>
		 </def>
	  </xsl:if>
   </xsl:template>
   <xsl:template match="Citations">
	  <xsl:for-each select="Citation">
		 <citation>
			<xsl:value-of select="normalize-space(.)"/>
		 </citation>
	  </xsl:for-each>
   </xsl:template>
   <xsl:template match="Languages">
	  <xsl:for-each select="Language">
		 <xsl:apply-templates select="Abbreviation"/>
	  </xsl:for-each>
	  <xsl:for-each select="Language">
		 <xsl:apply-templates select="Name"/>
	  </xsl:for-each>
	  <xsl:for-each select="Language">
		 <xsl:apply-templates select="Description"/>
	  </xsl:for-each>
	  <xsl:for-each select="Language">
		 <xsl:apply-templates select="Citations"/>
	  </xsl:for-each>
   </xsl:template>
   <!--
   <xsl:template match="Language">
	  <xsl:apply-templates select="Abbreviation"/>
	  <xsl:apply-templates select="Name"/>
	  <xsl:apply-templates select="Description"/>
	  <xsl:apply-templates select="Citations"/>
   </xsl:template>
   -->
   <xsl:template match="LangCode"/>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Separator attr template (needed to get hidden ones to show)
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template match="@afterSeparator | @complexNameSeparator | @complexNameFirst">
	  <xsl:copy/>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Create unknown value

  Section 4, number 2 of 'Building the etic gloss list', G. Simons, 25 September 2002
	"Beneath each item that has a type value of "feature", add a final child node that represents an unknown value.
	It should have type="value", abbrev="?", term="Unknown feature-term". "
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template match="item[@type='feature']">
	  <xsl:copy>
		 <xsl:apply-templates select="@*"/>
		 <xsl:apply-templates/>
		 <xsl:variable name="sAbbrev">
			<xsl:text>?</xsl:text>
		 </xsl:variable>
		 <item>
			<xsl:attribute name="id">
			   <xsl:text>vUnknown</xsl:text>
			   <xsl:value-of select="@id"/>
			</xsl:attribute>
			<xsl:attribute name="type">
			   <xsl:text>value</xsl:text>
			</xsl:attribute>
			<xsl:for-each select="Languages/Language">
			   <abbrev ws="{LangCode}">
				  <xsl:value-of select="$sAbbrev"/>
			   </abbrev>
			</xsl:for-each>
			<xsl:for-each select="Languages/Language">
			   <term ws="{LangCode}">
				  <xsl:text>unknown </xsl:text>
				  <xsl:value-of select="normalize-space(Name)"/>
			   </term>
			</xsl:for-each>
			<xsl:call-template name="CreateFSUnknown">
			   <xsl:with-param name="sValue" select="$sAbbrev"/>
			</xsl:call-template>
		 </item>
	  </xsl:copy>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Embed complex nodes

  Section 4, number 3 of 'Building the etic gloss list', G. Simons, 25 September 2002
	"If a gloss item corresponds to a complex feature, create a subtree of proxy items under it that mirrors all the items in the subtree for the embedded feature structure type.
	(Proxies for the children of the embedded feature structure type become the children of the complex feature, and so on recursively.).
	If the carry attribute is "yes", put the term of the complex feature in parentheses and append it to the term for each feature value that is copied."
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template match="item[@type='complex']">
	  <item>
		 <xsl:attribute name="id">
			<xsl:value-of select="@id"/>
		 </xsl:attribute>
		 <xsl:attribute name="type">complex</xsl:attribute>
		 <xsl:for-each select="Languages/Language">
			   <xsl:apply-templates select="Abbreviation"/>
		 </xsl:for-each>
		 <xsl:for-each select="Languages/Language">
			   <xsl:apply-templates select="Name"/>
		 </xsl:for-each>
		 <xsl:for-each select="Languages/Language">
			   <xsl:apply-templates select="Description"/>
		 </xsl:for-each>
		 <xsl:variable name="sEmbed">
			<xsl:value-of select="@embed"/>
		 </xsl:variable>
		 <xsl:variable name="sTerm">
			<xsl:choose>
			   <!-- How do this for multiple languages??? -->
			   <xsl:when test="@carry='yes'">
				  <xsl:text> (</xsl:text>
				  <xsl:value-of select="Languages/Language[1]/Name"/>
				  <xsl:text>)</xsl:text>
			   </xsl:when>
			   <xsl:otherwise/>
			</xsl:choose>
		 </xsl:variable>
		 <xsl:call-template name="DoEachEmbeddedId">
			<xsl:with-param name="sEmbeddedIds" select="$sEmbed"/>
			<xsl:with-param name="sCarriedTerm" select="$sTerm"/>
			<xsl:with-param name="prmOrigItem" select="."/>
		 </xsl:call-template>
	  </item>
   </xsl:template>
   <!--  eat the following -->
   <xsl:template match="Unknowns"/>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
AncestorType
	output type attribute based on ancestor's value
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="AncestorType">
	  <xsl:variable name="sAncestorType">
		 <xsl:value-of select="ancestor::item[@type='fsType']/@id"/>
	  </xsl:variable>
	  <xsl:variable name="sType">
		 <xsl:choose>
			<xsl:when test="string-length($sAncestorType) != 0">
			   <xsl:value-of select="$sAncestorType"/>
			</xsl:when>
			<xsl:otherwise>Infl</xsl:otherwise>
		 </xsl:choose>
	  </xsl:variable>
	  <xsl:attribute name="type">
		 <xsl:value-of select="$sType"/>
	  </xsl:attribute>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CreateComplexFS
	Create a FS for a complex value node
		Parameters: prmOrigItem - the original complex item
							sUnknown    - "vUnknown" for unknown values, empty otherwise
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="CreateComplexFS">
	  <xsl:param name="prmOrigItem"/>
	  <xsl:param name="sUnknown"/>
	  <xsl:variable name="sId" select="@id"/>
	  <xsl:for-each select="$prmOrigItem">
		 <fs>
			<xsl:call-template name="AncestorType"/>
			<f>
			   <xsl:attribute name="name">
				  <xsl:value-of select="Languages/Language[1]/Abbreviation"/>
			   </xsl:attribute>
			   <fs>
				  <xsl:for-each select="id($sId)">
					 <xsl:for-each select="$prmOrigItem">
						<xsl:call-template name="AncestorType"/>
					 </xsl:for-each>
					 <f>
						<xsl:choose>
						   <xsl:when test="string-length($sUnknown)!=0">
							  <xsl:attribute name="name">
								 <xsl:value-of select="Languages/Language[1]/Abbreviation"/>
							  </xsl:attribute>
							  <unknown/>
						   </xsl:when>
						   <xsl:otherwise>
							  <xsl:attribute name="name">
								 <xsl:value-of select="ancestor::item[@type='feature']/Languages/Language[1]/Abbreviation"/>
							  </xsl:attribute>
							  <sym>
								 <xsl:attribute name="value">
									<xsl:value-of select="Languages/Language[1]/Abbreviation"/>
								 </xsl:attribute>
							  </sym>
						   </xsl:otherwise>
						</xsl:choose>
					 </f>
				  </xsl:for-each>
			   </fs>
			</f>
		 </fs>
	  </xsl:for-each>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CreateFS
	Create a FS for a value node
		Parameters: none

  Section 4, number 4a of 'Building the etic gloss list', G. Simons, 25 September 2002
		"Add a feature structure fragment to each leaf node of the etic gloss tree as follows:
				  a. If there is no complex feature involved, the path from root to leaf should pass through one each of items with type values of fsType, feature,
					  and value (and in that order from top to bottom).
					  Take the abbreviation from each gloss item to construct a feature structure of the following form:
<fs type="type-abbrev">
   <f name="feature-abbrev">
		 <sym value="value-abbrev"/>
			</f></fs>"
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="CreateFS">
	  <fs>
		 <xsl:attribute name="id">
			<xsl:value-of select="@id"/>
			<xsl:text>FS</xsl:text>
		 </xsl:attribute>
		 <xsl:call-template name="AncestorType"/>
		 <f>
			<xsl:attribute name="name">
			   <xsl:value-of select="ancestor::item[@type='feature']/Languages/Language[1]/Abbreviation"/>
			</xsl:attribute>
			<sym>
			   <xsl:attribute name="value">
				  <xsl:value-of select="Languages/Language[1]/Abbreviation"/>
			   </xsl:attribute>
			</sym>
		 </f>
	  </fs>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CreateFSUnknown
	Create a FS for a generated unknown value node
		Parameters: sValue
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="CreateFSUnknown">
	  <xsl:param name="sValue"/>
	  <fs>
		 <xsl:attribute name="id">
			<xsl:text>vUnknown</xsl:text>
			<xsl:value-of select="@id"/>
			<xsl:text>FS</xsl:text>
		 </xsl:attribute>
		 <xsl:call-template name="AncestorType"/>
		 <f>
			<xsl:attribute name="name">
			   <xsl:value-of select="Languages/Language[1]/Abbreviation"/>
			</xsl:attribute>
			<sym>
			   <xsl:attribute name="value">
				  <xsl:value-of select="$sValue"/>
			   </xsl:attribute>
			</sym>
		 </f>
	  </fs>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoEachEmbeddedId
	Process a space-separated list of IDREFS; for each one, mirror its contents
		Parameters: sEmbeddedIds - setof IDREFS to process
							 sCarriedTerm - string of carried term from complex feature
							 prmOrigItem - the original complex item
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="DoEachEmbeddedId">
	  <xsl:param name="sEmbeddedIds"/>
	  <xsl:param name="sCarriedTerm"/>
	  <xsl:param name="prmOrigItem"/>
	  <xsl:variable name="sNewList" select="concat(normalize-space($sEmbeddedIds),' ')"/>
	  <xsl:variable name="sFirst" select="substring-before($sNewList,' ')"/>
	  <xsl:variable name="sRest" select="substring-after($sNewList,' ')"/>
	  <xsl:if test="string-length($sFirst) != 0">
		 <xsl:call-template name="MirrorEmbedded">
			<xsl:with-param name="prmItems" select="//item[@id=$sFirst]"/>
			<xsl:with-param name="sCarriedTerm" select="$sCarriedTerm"/>
			<xsl:with-param name="prmOrigItem" select="$prmOrigItem"/>
		 </xsl:call-template>
		 <xsl:if test="string-length($sRest) != 0">
			<xsl:call-template name="DoEachEmbeddedId">
			   <xsl:with-param name="sEmbeddedIds" select="$sRest"/>
			   <xsl:with-param name="sCarriedTerm" select="$sCarriedTerm"/>
			   <xsl:with-param name="prmOrigItem" select="$prmOrigItem"/>
			</xsl:call-template>
		 </xsl:if>
	  </xsl:if>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
MirrorEmbedded
	Recursively mirrors an embedded set of nodes, marking them as "proxy"
		Parameters: prmItems - set of nodes to consider
							 sCarriedTerm - string of carried term from complex feature
							 prmOrigItem - the original complex item
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="MirrorEmbedded">
	  <xsl:param name="prmItems"/>
	  <xsl:param name="sCarriedTerm"/>
	  <xsl:param name="prmOrigItem"/>
	  <xsl:for-each select="$prmItems">
		 <xsl:if test="name(.)='item'">
			<item>
			   <xsl:attribute name="status">
				  <xsl:text>proxy</xsl:text>
			   </xsl:attribute>
			   <xsl:attribute name="target">
				  <xsl:value-of select="@id"/>
			   </xsl:attribute>
			   <xsl:for-each select="Languages/Language">
				  <xsl:apply-templates select="Abbreviation"/>
			   </xsl:for-each>
			   <xsl:for-each select="Languages/Language">
				  <term ws="{LangCode}">
					 <xsl:value-of select="normalize-space(Name)"/>
					 <xsl:if test="../../@type='value'">
						<xsl:value-of select="$sCarriedTerm"/>
					 </xsl:if>
				  </term>
			   </xsl:for-each>
			   <xsl:for-each select="Languages/Language">
				  <xsl:apply-templates select="Description"/>
			   </xsl:for-each>
			   <xsl:if test="@type='value'">
				  <!-- now do FS for a value -->
				  <xsl:call-template name="CreateComplexFS">
					 <xsl:with-param name="prmOrigItem" select="$prmOrigItem"/>
				  </xsl:call-template>
			   </xsl:if>
			   <xsl:call-template name="MirrorEmbedded">
				  <xsl:with-param name="prmItems" select="*"/>
				  <xsl:with-param name="sCarriedTerm" select="$sCarriedTerm"/>
				  <xsl:with-param name="prmOrigItem" select="$prmOrigItem"/>
			   </xsl:call-template>
			   <xsl:if test="@type='feature'">
				  <item>
					 <xsl:attribute name="status">
						<xsl:text>proxy</xsl:text>
					 </xsl:attribute>
					 <xsl:attribute name="target">
						<xsl:text>vUnknown</xsl:text>
						<xsl:value-of select="@id"/>
					 </xsl:attribute>
					 <xsl:for-each select="Languages/Language">
						<xsl:apply-templates select="Abbreviation"/>
					 </xsl:for-each>
					 <xsl:for-each select="Languages/Language">
						<xsl:variable name="unknown" select="key('Unknowns',LangCode)"/>
						<term ws="{LangCode}">
						   <xsl:if test="$unknown/Position/@appearance='before'">
							  <xsl:value-of select=" normalize-space($unknown/Name)"/>
							  <xsl:text>&#x20;</xsl:text>
						   </xsl:if>
						   <xsl:value-of select=" normalize-space(Name)"/>
						   <xsl:if test="$unknown/Position/@appearance='after'">
							  <xsl:value-of select=" normalize-space($unknown/Name)"/>
							  <xsl:text>&#x20;</xsl:text>
						   </xsl:if>
						</term>
					 </xsl:for-each>
					 <xsl:for-each select="Languages/Language">
						<xsl:apply-templates select="Description"/>
					 </xsl:for-each>
					 <!-- now do FS for an unknown value -->
					 <xsl:call-template name="CreateComplexFS">
						<xsl:with-param name="prmOrigItem" select="$prmOrigItem"/>
						<xsl:with-param name="sUnknown">
						   <xsl:text>vUnknown</xsl:text>
						</xsl:with-param>
					 </xsl:call-template>
				  </item>
			   </xsl:if>
			</item>
		 </xsl:if>
	  </xsl:for-each>
   </xsl:template>
</xsl:stylesheet>
<!--
================================================================
Revision History
- - - - - - - - - - - - - - - - - - -
17-Apr-2006	Andy Black	Initial Draft
================================================================
-->
