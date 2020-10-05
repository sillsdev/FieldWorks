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
	<!-- Parameters that can be set by user.  -->
	<!-- Using MGAMaster until can get real thing -->
	<xsl:param name="prmCellarFile">MGAMaster.xml</xsl:param>
	<xsl:param name="prmEmeldFile">nonExistantRightNow</xsl:param>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Main template (change name of top node; add DTD)
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="masterGlossList">
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
Separator attr template (needed to get hidden ones to show)
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="@afterSeparator | @complexNameSeparator | @complexNameFirst">
		<xsl:copy/>
	</xsl:template>
	<!-- ### -->
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Include cellar info Template

  Section 4, number 1a of 'Building the etic gloss list', G. Simons, 25 September 2002
	  "Pull in definitions and other documentation as needed from the LinguaLinks glossary or the EMELD ontology."
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="@cellar">
		<xsl:variable name="sCellarId">
			<xsl:value-of select="."/>
		</xsl:variable>
		<!-- Following is temporary until we get the real thing -->
		<xsl:variable name="nItem" select="document($prmCellarFile)//item[cellar[@id=$sCellarId]]"/>
		<abbrev ws="en">
			<xsl:value-of select="../abbrev"/>
		</abbrev>
		<term ws="en">
			<xsl:value-of select="../term"/>
		</term>
		<xsl:copy-of select="$nItem/def"/>
		<xsl:copy-of select="$nItem/citation"/>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
abbrev Template

  Sometimes need to skip abbrev because it's done elsewhere
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="abbrev">
		<xsl:if test="not(../@cellar)">
			<abbrev ws="en">
				<xsl:value-of select="."/>
			</abbrev>
		</xsl:if>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
term Template

  Sometimes need to skip term because it's done elsewhere
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="term">
		<xsl:if test="not(../@cellar)">
			<term ws="en">
				<xsl:value-of select="."/>
			</term>
		</xsl:if>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Include EMELD info Template

  Section 4, number 1b of 'Building the etic gloss list', G. Simons, 25 September 2002:
	  "Pull in definitions and other documentation as needed from the LinguaLinks glossary or the EMELD ontology."
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="@emeld">
		<xsl:variable name="sEmeldId">
			<xsl:value-of select="."/>
		</xsl:variable>
		<!--
	Do not know what form it really is yet
	<xsl:variable name="nItem" select="document($prmCellarFile)//item[cellar[@id=$sCellarId]]"/>
	<xsl:copy-of select="$nItem/def"/>
	<xsl:copy-of select="$nItem/citation"/>
	-->
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Create unknown value

  Section 4, number 2 of 'Building the etic gloss list', G. Simons, 25 September 2002
	"Beneath each item that has a type value of "feature", add a final child node that represents an unknown value. It should have type="value", abbrev="?", term="Unknown feature-term". "
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="item[@type='feature']">
		<xsl:copy>
			<xsl:apply-templates select="@*"/>
			<!--
			<xsl:for-each select="*">
				<xsl:sort select="@term"/>
				<xsl:copy>
					<xsl:apply-templates select="@*"/>
					-->
			<xsl:apply-templates/>
			<!--
				</xsl:copy>
			</xsl:for-each>
			-->
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
				<abbrev ws="en">
					<xsl:value-of select="$sAbbrev"/>
				</abbrev>
				<term ws="en">
					<xsl:text>unknown </xsl:text>
					<xsl:value-of select="term"/>
				</term>
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
	"If a gloss item corresponds to a complex feature, create a subtree of proxy items under it that mirrors all the items in the subtree for the embedded feature structure type. (Proxies for the children of the embedded feature structure type become the children of the complex feature, and so on recursively.). If the carry attribute is "yes", put the term of the complex feature in parentheses and append it to the term for each feature value that is copied."
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="item[@type='complex']">
		<item>
			<xsl:attribute name="id">
				<xsl:value-of select="@id"/>
			</xsl:attribute>
			<xsl:attribute name="type">complex</xsl:attribute>
			<abbrev ws="en">
				<xsl:value-of select="abbrev"/>
			</abbrev>
			<term ws="en">
				<xsl:value-of select="term"/>
			</term>
			<xsl:variable name="sEmbed">
				<xsl:value-of select="@embed"/>
			</xsl:variable>
			<xsl:variable name="sTerm">
				<xsl:choose>
					<xsl:when test="@carry='yes'">
						<xsl:text> (</xsl:text>
						<xsl:value-of select="term"/>
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
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
AncestorType
	output type attribute based on ancestor's value
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="AncestorType">
		<xsl:variable name="sAncestorType">
			<xsl:value-of select="ancestor::item[@type='fsType']/abbrev"/>
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
						<xsl:value-of select="abbrev"/>
					</xsl:attribute>
					<fs>
						<xsl:for-each select="id($sId)">
							<xsl:call-template name="AncestorType"/>
							<f>
								<xsl:choose>
									<xsl:when test="string-length($sUnknown)!=0">
										<xsl:attribute name="name">
											<xsl:value-of select="abbrev"/>
										</xsl:attribute>
										<unknown/>
									</xsl:when>
									<xsl:otherwise>
										<xsl:attribute name="name">
											<xsl:value-of select="ancestor::item[@type='feature']/abbrev"/>
										</xsl:attribute>
										<sym>
											<xsl:attribute name="value">
												<xsl:value-of select="abbrev"/>
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
				  a. If there is no complex feature involved, the path from root to leaf should pass through one each of items with type values of fsType, feature, and value (and in that order from top to bottom). Take the abbreviation from each gloss item to construct a feature structure of the following form:
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
					<xsl:value-of select="ancestor::item[@type='feature']/abbrev"/>
				</xsl:attribute>
				<sym>
					<xsl:attribute name="value">
						<xsl:value-of select="abbrev"/>
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
					<xsl:value-of select="abbrev"/>
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
					<abbrev ws="en"/>
					<term ws="en">
						<xsl:value-of select="term"/>
						<xsl:if test="@type='value'">
							<xsl:value-of select="$sCarriedTerm"/>
						</xsl:if>
					</term>
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
							<abbrev ws="en"/>
							<term ws="en">
								<xsl:text>unknown </xsl:text>
								<xsl:value-of select="term"/>
							</term>
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
16-Nov-2005     Andy Black  Output fs for xrefs, too
14-Nov-2002     Andy Black  Handle separator attrs
08-Nov-2002	Andy Black	Initial Draft
================================================================
 -->
