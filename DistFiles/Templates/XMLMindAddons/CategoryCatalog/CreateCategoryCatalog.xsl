<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="xml" version="1.0" encoding="UTF-8" indent="yes"/>
	<!--
================================================================
Create a Category Catalog to be stored as DistFiles\Templates\GOLDEtic.xml
  Input:    A category organization that conforms to POSes.dtd
  Output: eticPOSList used as the Category Catalog

================================================================
Revision History is at the end of this file.

- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Main template
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="/">
		<eticPOSList xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#" xmlns:rdfs="http://www.w3.org/2000/01/rdf-schema#" xmlns:owl="http://www.w3.org/2002/07/owl#">
			<source>This was originally based on the 2005-06-03 GOLD xml version at <link>http://www.linguistics-ontology.org/ns/gold/0.2/gold-pos.owl</link>, but has been modified by hand.</source>
			<xsl:for-each select="Categories/Category">
				<xsl:call-template name="DoPOSContent"/>
			</xsl:for-each>
		</eticPOSList>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoPOSContent
	create the content for a POS and all subcategories
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="DoPOSContent">
		<item type="category">
			<xsl:attribute name="id">
				<xsl:value-of select="@id"/>
			</xsl:attribute>
			<xsl:for-each select="Languages/Language">
				<abbrev>
					<xsl:attribute name="ws">
						<xsl:value-of select="LangCode"/>
					</xsl:attribute>
					<xsl:value-of select="normalize-space(Abbreviation)"/>
				</abbrev>
			</xsl:for-each>
			<xsl:for-each select="Languages/Language">
				<term>
					<xsl:attribute name="ws">
						<xsl:value-of select="LangCode"/>
					</xsl:attribute>
					<xsl:value-of select="normalize-space(Name)"/>
				</term>
			</xsl:for-each>
			<xsl:for-each select="Languages/Language">
				<def>
					<xsl:attribute name="ws">
						<xsl:value-of select="LangCode"/>
					</xsl:attribute>
					<xsl:value-of select="normalize-space(Description)"/>
				</def>
			</xsl:for-each>
			<xsl:for-each select="Languages/Language">
				<xsl:for-each select="Citations/Citation[string-length(.) &gt; 0]">
					<citation>
						<xsl:attribute name="ws">
							<xsl:value-of select="../../LangCode"/>
						</xsl:attribute>
						<xsl:value-of select="."/>
					</citation>
				</xsl:for-each>
			</xsl:for-each>
			<xsl:for-each select="Category">
				<xsl:call-template name="DoPOSContent"/>
			</xsl:for-each>
		</item>
	</xsl:template>
</xsl:stylesheet>
<!--
================================================================
Revision History
- - - - - - - - - - - - - - - - - - -
17-Mar-2006    Andy Black    Initial Version
================================================================
 -->
