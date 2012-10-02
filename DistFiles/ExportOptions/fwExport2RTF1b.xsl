<?xml version="1.0" encoding="UTF-8"?>
<!--This XSL is the second of 4 in a series to export FieldWorks data to RTF.
FwExport2RTF1 - Strips extra information and outputs standard WorldPad format.
** FwExport2RTF1b - Creates unique integer ids for Styles and Fonts so that they can be more easily referenced when creating the RTF.
FwExport2RTF2a - Adds style information locally to the paragraph particularly for bulleted or numbered paragraphs to facilitate creation of RTF codes.
FwExport2RTF2b - Creates RTF codes.

It highly recommended that you use an XML viewer to review or edit these files in order to easily delineate commented code from actual code.
-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:silfw="http://fieldworks.sil.org/2002/silfw/Codes" exclude-result-prefixes="silfw">
	<xsl:strip-space elements="*"/>
	<xsl:preserve-space elements="Run"/>
	<silfw:file chain="FwExport2RTF2a.xsl"/>
	<!-- This transform simply strips out the element tags added to facilitate generic export. -->
	<xsl:output method="xml" version="1.0" encoding="UTF-8"/>
	<xsl:template match="/">
		<xsl:apply-templates/>
	</xsl:template>
	<xsl:template match="WpDoc">
		<xsl:text>&#13;</xsl:text>
		<WpDoc>
			<xsl:call-template name="BulletFonts"/>
			<xsl:apply-templates/>
			<xsl:text>&#13;</xsl:text>
		</WpDoc>
	</xsl:template>
	<xsl:template match="LgEncoding">
		<LgEncoding>
			<xsl:attribute name="serifFontNum"><xsl:value-of select="position()+100"/></xsl:attribute>
			<xsl:attribute name="sansSerifFontNum"><xsl:value-of select="position()+200"/></xsl:attribute>
			<xsl:attribute name="id"><xsl:value-of select="@id"/></xsl:attribute>
			<xsl:apply-templates/>
		</LgEncoding>
		<xsl:text>&#13;</xsl:text>
	</xsl:template>
	<xsl:template match="StStyle">
		<!-- include styles for paragraph types -->
		<StStyle>
			<xsl:attribute name="styleNum"><xsl:value-of select="position()-1"/></xsl:attribute>
			<xsl:if test="Type17/Integer[@val=0]">
				<xsl:attribute name="type"><xsl:text>s</xsl:text></xsl:attribute>
			</xsl:if>
			<xsl:if test="Type17/Integer[@val=1]">
				<xsl:attribute name="type"><xsl:text>cs</xsl:text></xsl:attribute>
			</xsl:if>
			<xsl:apply-templates/>
		</StStyle>
		<xsl:text>&#13;</xsl:text>
	</xsl:template>
	<xsl:template match="*">
		<xsl:text>&#13;</xsl:text>
		<xsl:copy>
			<xsl:apply-templates select="* | @* | text()"/>
		</xsl:copy>
		<xsl:text>&#13;</xsl:text>
	</xsl:template>
	<xsl:template match="@*">
		<xsl:copy>
			<xsl:apply-templates/>
		</xsl:copy>
	</xsl:template>
	<xsl:template match="text()">
		<xsl:copy/>
	</xsl:template>
	<xsl:template name="BulletFonts">
		<xsl:text>&#13;</xsl:text>
		<xsl:comment>The following element 'BulletFonts' is not part of the FWExport format or WorldPad format. It is added to assist in creating RTF.</xsl:comment>
		<xsl:text>&#13;</xsl:text>
		<BulletFonts>
			<xsl:text>&#13;</xsl:text>
			<xsl:for-each select="//BulNumFontInfo[not(@fontFamily=following::BulNumFontInfo/@fontFamily) and @fontFamily]">
				<BulletFont>
					<xsl:attribute name="bulletFontId"><xsl:value-of select="position() + 500"/></xsl:attribute>
					<xsl:attribute name="bulletFontName"><xsl:value-of select="@fontFamily"/></xsl:attribute>
				</BulletFont>
				<xsl:text>&#13;</xsl:text>
			</xsl:for-each>
		</BulletFonts>
		<xsl:text>&#13;</xsl:text>
	</xsl:template>
</xsl:stylesheet>
