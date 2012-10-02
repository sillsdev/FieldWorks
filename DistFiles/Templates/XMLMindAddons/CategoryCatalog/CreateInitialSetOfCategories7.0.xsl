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
		<LangProject>
		<PartsOfSpeech>
			<CmPossibilityList>
				<Depth>
					<Integer val="127"/>
				</Depth>
				<IsSorted>
					<Boolean val="true"/>
				</IsSorted>
				<UseExtendedFields>
					<Boolean val="true"/>
				</UseExtendedFields>
				<ItemClsid>
					<Integer val="5049"/>
				</ItemClsid>
				<WsSelector>
					<Integer val="-3"/>
				</WsSelector>
				<Name>
					<AUni ws="es">Categorías Gramáticas</AUni>
					<AUni ws="en">Parts Of Speech</AUni>
					<AUni ws="fr">Parties du Discours</AUni>
				</Name>
				<Abbreviation>
					<AUni ws="en">Pos</AUni>
				</Abbreviation>
				<Possibilities>
					<xsl:for-each select="/Categories/Category[descendant-or-self::Category[@use4newlanguageprojects='yes']]">
						<xsl:call-template name="DoPOSContent"/>
					</xsl:for-each>
				</Possibilities>
			</CmPossibilityList>
		</PartsOfSpeech>
		</LangProject>
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
			<ForeColor>
				<Integer val="6303632"/>
			</ForeColor>
			<BackColor>
				<Integer val="-1073741824"/>
			</BackColor>
			<UnderColor>
				<Integer val="-1073741824"/>
			</UnderColor>
			<UnderStyle>
				<Integer val="1"/>
			</UnderStyle>
			<Name>
				<xsl:for-each select="Languages/Language">
					<AUni>
						<xsl:attribute name="ws">
							<xsl:value-of select="LangCode"/>
						</xsl:attribute>
						<xsl:value-of select="normalize-space(Name)"/>
					</AUni>
				</xsl:for-each>
			</Name>
			<Abbreviation>
				<xsl:for-each select="Languages/Language">
					<AUni>
						<xsl:attribute name="ws">
							<xsl:value-of select="LangCode"/>
						</xsl:attribute>
						<xsl:value-of select="normalize-space(Abbreviation)"/>
					</AUni>
				</xsl:for-each>
			</Abbreviation>
			<Description>
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
			</Description>
			<CatalogSourceId>
				<Uni>
					<xsl:value-of select="@id"/>
				</Uni>
			</CatalogSourceId>
			<xsl:for-each select="descendant::Category[@use4newlanguageprojects='yes']">
				<SubPossibilities>
					<xsl:call-template name="DoPOSContent"/>
				</SubPossibilities>
			</xsl:for-each>
		</PartOfSpeech>
	</xsl:template>
</xsl:stylesheet>
<!--
================================================================
Revision History
- - - - - - - - - - - - - - - - - - -
12-Apr-2010    Andy Black    Initial Version
================================================================
 -->
