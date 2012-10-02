<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="xml" version="1.0" encoding="UTF-8" indent="yes"/>
	<!--
================================================================
Create initial set of Categories (POS) to be included with NewLangProj.xml at DistFiles\Templates\POS.xml
  Input:    A category organization that conforms to POSes.dtd
  Output: PartsOfSpeech6001 element and all its subelements

================================================================
Revision History is at the end of this file.

- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Main template
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="/">
		<PartsOfSpeech6001>
			<CmPossibilityList>
				<Depth8>
					<Integer val="127"/>
				</Depth8>
				<IsSorted8>
					<Boolean val="true"/>
				</IsSorted8>
				<UseExtendedFields8>
					<Boolean val="true"/>
				</UseExtendedFields8>
				<ItemClsid8>
					<Integer val="5049"/>
				</ItemClsid8>
				<WsSelector8>
					<Integer val="-3"/>
				</WsSelector8>
				<Name5>
					<AUni ws="es">Categorías Gramáticas</AUni>
					<AUni ws="en">Parts Of Speech</AUni>
					<AUni ws="fr">Parties du Discours</AUni>
				</Name5>
				<Abbreviation8>
					<AUni ws="en">Pos</AUni>
				</Abbreviation8>
				<Possibilities8>
					<xsl:for-each select="/Categories/Category[descendant-or-self::Category[@use4newlanguageprojects='yes']]">
						<xsl:call-template name="DoPOSContent"/>
					</xsl:for-each>
				</Possibilities8>
			</CmPossibilityList>
		</PartsOfSpeech6001>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoPOSContent
	create the content for a POS and all subcategories
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="DoPOSContent">
		<PartOfSpeech>
			<ForeColor7>
				<Integer val="6303632"/>
			</ForeColor7>
			<BackColor7>
				<Integer val="-1073741824"/>
			</BackColor7>
			<UnderColor7>
				<Integer val="-1073741824"/>
			</UnderColor7>
			<UnderStyle7>
				<Integer val="1"/>
			</UnderStyle7>
			<Name7>
				<xsl:for-each select="Languages/Language">
					<AUni>
						<xsl:attribute name="ws">
							<xsl:value-of select="LangCode"/>
						</xsl:attribute>
						<xsl:value-of select="normalize-space(Name)"/>
					</AUni>
				</xsl:for-each>
			</Name7>
			<Abbreviation7>
				<xsl:for-each select="Languages/Language">
					<AUni>
						<xsl:attribute name="ws">
							<xsl:value-of select="LangCode"/>
						</xsl:attribute>
						<xsl:value-of select="normalize-space(Abbreviation)"/>
					</AUni>
				</xsl:for-each>
			</Abbreviation7>
			<Description7>
				<xsl:for-each select="Languages/Language">
					<AStr>
						<xsl:attribute name="ws">
							<xsl:value-of select="LangCode"/>
						</xsl:attribute>
					<Run>
						<xsl:attribute name="ws">
							<xsl:value-of select="LangCode"/>
						</xsl:attribute>
						<xsl:value-of select="normalize-space(Description)"/>
					</Run>
					</AStr>
				</xsl:for-each>
			</Description7>
			<CatalogSourceId5049>
				<Uni>
					<xsl:value-of select="@id"/>
				</Uni>
			</CatalogSourceId5049>
			<xsl:for-each select="descendant::Category[@use4newlanguageprojects='yes']">
				<SubPossibilities7>
					<xsl:call-template name="DoPOSContent"/>
				</SubPossibilities7>
			</xsl:for-each>
		</PartOfSpeech>
	</xsl:template>
</xsl:stylesheet>
<!--
================================================================
Revision History
- - - - - - - - - - - - - - - - - - -
17-Mar-2006    Andy Black    Initial Version
================================================================
 -->
