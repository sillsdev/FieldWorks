<?xml version="1.0" encoding="UTF-8"?>
<!-- edited with XML Spy v4.4 U (http://www.xmlspy.com) by Larr Hayashi (private) -->
<!--This XSL is the third of 4 in a series to export FieldWorks data to RTF.
FwExport2RTF1 - Strips extra information and outputs standard WorldPad format.
FwExport2RTF1b - Creates unique integer ids for Styles and Fonts so that they can be more easily referenced when creating the RTF.
** FwExport2RTF2a - Adds style information locally to the paragraph particularly for bulleted or numbered paragraphs to facilitate creation of RTF codes.
FwExport2RTF2b - Creates RTF codes.

It highly recommended that you use an XML viewer to review or edit these files in order to easily delineate commented code from actual code.
-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:silfw="http://fieldworks.sil.org/2002/silfw/Codes" exclude-result-prefixes="silfw">
	<silfw:file chain="FwExport2RTF2b.xsl"/>
	<xsl:key name="ParagraphStyles" match="//StStyle" use="Name17/Uni"/>
	<xsl:key name="BulletedParagraphStyles" match="//StStyle[Rules17/Prop/@bulNumScheme]" use="Name17/Uni"/>
	<!-- This transform simply strips out the element tags added to facilitate generic export. -->
	<xsl:output method="xml" version="1.0" encoding="UTF-8"/>
	<xsl:template match="/">
		<xsl:apply-templates/>
	</xsl:template>
	<!--bulNumFontInfo CDATA #IMPLIED
	bulNumScheme CDATA #IMPLIED
	bulNumStartAt CDATA #IMPLIED
	bulNumTxtAft CDATA #IMPLIED
	bulNumTxtBef CDATA #IMPLIED-->
	<!--We want to add bullet or number style information locally to paragraphs for those paragraphs that have local bullet information or are of a style with bullet information.-->
	<xsl:template match="StyleRules15/Prop[BulNumFontInfo | @bulNumScheme | @bulNumStartAt | @bulNumTxtAft | @bulNumTxtBef | key('BulletedParagraphStyles', @namedStyle)]">
		<Prop>
			<xsl:variable name="LocalBulNumScheme" select="@bulNumScheme"/>
			<xsl:variable name="LocalBulNumStartAt" select="@bulNumStartAt"/>
			<xsl:variable name="LocalBulNumTxtAft" select="@bulNumTxtAft"/>
			<xsl:variable name="LocalBulNumTxtBef" select="@bulNumTxtBef"/>
			<xsl:variable name="LocalFirstIndent" select="@firstIndent"/>
			<xsl:variable name="LocalLeadingIndent" select="@leadingIndent"/>
			<!--BulNumFontInfo for the local paragraph  if any-->
			<xsl:variable name="LocalBNFIBackcolor" select="BulNumFontInfo/@backcolor"/>
			<xsl:variable name="LocalBNFIBold" select="BulNumFontInfo/@bold"/>
			<xsl:variable name="LocalBNFIFontsize" select="BulNumFontInfo/@fontsize"/>
			<xsl:variable name="LocalBNFIForecolor" select="BulNumFontInfo/@forecolor"/>
			<xsl:variable name="LocalBNFIItalic" select="BulNumFontInfo/@italic"/>
			<xsl:variable name="LocalBNFIOffset" select="BulNumFontInfo/@offset"/>
			<xsl:variable name="LocalBNFISuperscript" select="BulNumFontInfo/@superscript"/>
			<xsl:variable name="LocalBNFIUndercolor" select="BulNumFontInfo/@undercolor"/>
			<xsl:variable name="LocalBNFIUnderline" select="BulNumFontInfo/@underline"/>
			<xsl:variable name="LocalBNFIFontFamily" select="BulNumFontInfo/@fontFamily"/>
			<xsl:if test="@namedStyle">
				<xsl:attribute name="namedStyle"><xsl:value-of select="@namedStyle"/></xsl:attribute>
			</xsl:if>
			<xsl:for-each select="key('ParagraphStyles', @namedStyle)">
				<xsl:for-each select="Rules17/Prop">
					<xsl:variable name="StyleBulNumScheme" select="@bulNumScheme"/>
					<xsl:variable name="StyleBulNumStartAt" select="@bulNumStartAt"/>
					<xsl:variable name="StyleBulNumTxtAft" select="@bulNumTxtAft"/>
					<xsl:variable name="StyleBulNumTxtBef" select="@bulNumTxtBef"/>
					<xsl:variable name="StyleFirstIndent" select="@firstIndent"/>
					<xsl:variable name="StyleLeadingIndent" select="@leadingIndent"/>
					<!--BulNumFontInfo for the style if any-->
					<xsl:variable name="StyleBNFIBackcolor" select="BulNumFontInfo/@backcolor"/>
					<xsl:variable name="StyleBNFIBold" select="BulNumFontInfo/@bold"/>
					<xsl:variable name="StyleBNFIFontsize" select="BulNumFontInfo/@fontsize"/>
					<xsl:variable name="StyleBNFIForecolor" select="BulNumFontInfo/@forecolor"/>
					<xsl:variable name="StyleBNFIItalic" select="BulNumFontInfo/@italic"/>
					<xsl:variable name="StyleBNFIOffset" select="BulNumFontInfo/@offset"/>
					<xsl:variable name="StyleBNFISuperscript" select="BulNumFontInfo/@superscript"/>
					<xsl:variable name="StyleBNFIUndercolor" select="BulNumFontInfo/@undercolor"/>
					<xsl:variable name="StyleBNFIUnderline" select="BulNumFontInfo/@underline"/>
					<xsl:variable name="StyleBNFIFontFamily" select="BulNumFontInfo/@fontFamily"/>
					<!--xsl:if test="@bulNumScheme">
						<xsl:if test="$LocalBulNumScheme !=''">
							<xsl:attribute name="bulNumScheme"><xsl:value-of select="$LocalBulNumScheme"/></xsl:attribute>
						</xsl:if>
						<xsl:attribute name="bulNumScheme"><xsl:value-of select="@bulNumScheme"/></xsl:attribute>
					</xsl:if-->
					<xsl:choose>
						<!--When the LocalBulNumScheme isn't empty, use it as the attribute for the paragraph. Local overrides style.-->
						<xsl:when test="$LocalBulNumScheme !=''">
							<xsl:attribute name="bulNumScheme"><xsl:value-of select="$LocalBulNumScheme"/></xsl:attribute>
						</xsl:when>
						<!--When LocalBulNumScheme is empty, then check the StyleBulNumScheme-->
						<xsl:otherwise>
							<xsl:choose>
								<!--If StyleBulNumScheme isn't empty then use it.-->
								<xsl:when test="$StyleBulNumScheme !=''">
									<xsl:attribute name="bulNumScheme"><xsl:value-of select="$StyleBulNumScheme"/></xsl:attribute>
								</xsl:when>
								<!--Otherwise don't do anything.-->
							</xsl:choose>
						</xsl:otherwise>
					</xsl:choose>
					<xsl:choose>
						<!--When the LocalBulNumStartAt isn't empty, use it as the attribute for the paragraph. Local overrides style.-->
						<xsl:when test="$LocalBulNumStartAt !=''">
							<xsl:attribute name="bulNumStartAt"><xsl:value-of select="$LocalBulNumStartAt"/></xsl:attribute>
						</xsl:when>
						<!--When LocalBulNumStartAt is empty, then check the StyleBulNumStartAt-->
						<xsl:otherwise>
							<xsl:choose>
								<!--If StyleBulNumStartAt isn't empty then use it.-->
								<xsl:when test="$StyleBulNumStartAt !=''">
									<xsl:attribute name="bulNumStartAt"><xsl:value-of select="$StyleBulNumStartAt"/></xsl:attribute>
								</xsl:when>
								<!--Otherwise don't do anything.-->
							</xsl:choose>
						</xsl:otherwise>
					</xsl:choose>
					<xsl:choose>
						<!--When the LocalBulNumTxtBef isn't empty, use it as the attribute for the paragraph. Local overrides style.-->
						<xsl:when test="$LocalBulNumTxtBef !=''">
							<xsl:attribute name="bulNumTxtBef"><xsl:value-of select="$LocalBulNumTxtBef"/></xsl:attribute>
						</xsl:when>
						<!--When LocalBulNumTxtBef is empty, then check the StyleBulNumTxtBef-->
						<xsl:otherwise>
							<xsl:choose>
								<!--If StyleBulNumTxtBef isn't empty then use it.-->
								<xsl:when test="$StyleBulNumTxtBef !=''">
									<xsl:attribute name="bulNumTxtBef"><xsl:value-of select="$StyleBulNumTxtBef"/></xsl:attribute>
								</xsl:when>
								<!--Otherwise don't do anything.-->
							</xsl:choose>
						</xsl:otherwise>
					</xsl:choose>
					<xsl:choose>
						<!--When the LocalBulNumTxtAft isn't empty, use it as the attribute for the paragraph. Local overrides style.-->
						<xsl:when test="$LocalBulNumTxtAft !=''">
							<xsl:attribute name="bulNumTxtAft"><xsl:value-of select="$LocalBulNumTxtAft"/></xsl:attribute>
						</xsl:when>
						<!--When LocalBulNumTxtAft is empty, then check the StyleBulNumTxtAft-->
						<xsl:otherwise>
							<xsl:choose>
								<!--If StyleBulNumTxtAft isn't empty then use it.-->
								<xsl:when test="$StyleBulNumTxtAft !=''">
									<xsl:attribute name="bulNumTxtAft"><xsl:value-of select="$StyleBulNumTxtAft"/></xsl:attribute>
								</xsl:when>
								<!--Otherwise don't do anything.-->
							</xsl:choose>
						</xsl:otherwise>
					</xsl:choose>
					<xsl:choose>
						<!--When the LocalLeadingIndent isn't empty, use it as the attribute for the paragraph. Local overrides style.-->
						<xsl:when test="$LocalLeadingIndent !=''">
							<xsl:attribute name="leadingIndent"><xsl:value-of select="$LocalLeadingIndent"/></xsl:attribute>
						</xsl:when>
						<!--When LocalLeadingIndent is empty, then check the StyleLeadingIndent-->
						<xsl:otherwise>
							<xsl:choose>
								<!--If StyleLeadingIndent isn't empty then use it.-->
								<xsl:when test="$StyleLeadingIndent !=''">
									<xsl:attribute name="leadingIndent"><xsl:value-of select="$StyleLeadingIndent"/></xsl:attribute>
								</xsl:when>
								<!--Otherwise don't do anything.-->
							</xsl:choose>
						</xsl:otherwise>
					</xsl:choose>
					<xsl:choose>
						<!--When the LocalFirstIndent isn't empty, use it as the attribute for the paragraph. Local overrides style.-->
						<xsl:when test="$LocalFirstIndent !=''">
							<xsl:attribute name="firstIndent"><xsl:value-of select="$LocalFirstIndent"/></xsl:attribute>
						</xsl:when>
						<!--When LocalFirstIndent is empty, then check the StyleFirstIndent-->
						<xsl:otherwise>
							<xsl:choose>
								<!--If StyleFirstIndent isn't empty then use it.-->
								<xsl:when test="$StyleFirstIndent !=''">
									<xsl:attribute name="firstIndent"><xsl:value-of select="$StyleFirstIndent"/></xsl:attribute>
								</xsl:when>
								<!--Otherwise don't do anything.-->
							</xsl:choose>
						</xsl:otherwise>
					</xsl:choose>
					<BulNumFontInfo>
						<xsl:choose>
							<!--When the LocalBulNumFontInfoBackcolor isn't empty, use it as the attribute for the paragraph. Local overrides style.-->
							<xsl:when test="$LocalBNFIBackcolor!=''">
								<xsl:attribute name="backcolor"><xsl:value-of select="$LocalBNFIBackcolor"/></xsl:attribute>
							</xsl:when>
							<!--When LocalBNFIBackcolor is empty, then check the StyleBNFIBackcolor-->
							<xsl:otherwise>
								<xsl:choose>
									<!--If StyleBNFIBackcolor isn't empty then use it.-->
									<xsl:when test="$StyleBNFIBackcolor !=''">
										<xsl:attribute name="backcolor"><xsl:value-of select="$StyleBNFIBackcolor"/></xsl:attribute>
									</xsl:when>
									<!--Otherwise don't do anything.-->
								</xsl:choose>
							</xsl:otherwise>
						</xsl:choose>
						<xsl:choose>
							<!--When the LocalBulNumFontInfoBold isn't empty, use it as the attribute for the paragraph. Local overrides style.-->
							<xsl:when test="$LocalBNFIBold!=''">
								<xsl:attribute name="bold"><xsl:value-of select="$LocalBNFIBold"/></xsl:attribute>
							</xsl:when>
							<!--When LocalBNFIBold is empty, then check the StyleBNFIBold-->
							<xsl:otherwise>
								<xsl:choose>
									<!--If StyleBNFIBold isn't empty then use it.-->
									<xsl:when test="$StyleBNFIBold !=''">
										<xsl:attribute name="bold"><xsl:value-of select="$StyleBNFIBold"/></xsl:attribute>
									</xsl:when>
									<!--Otherwise don't do anything.-->
								</xsl:choose>
							</xsl:otherwise>
						</xsl:choose>
						<xsl:choose>
							<!--When the LocalBulNumFontInfoFontsize isn't empty, use it as the attribute for the paragraph. Local overrides style.-->
							<xsl:when test="$LocalBNFIFontsize!=''">
								<xsl:attribute name="fontsize"><xsl:value-of select="$LocalBNFIFontsize"/></xsl:attribute>
							</xsl:when>
							<!--When LocalBNFIFontsize is empty, then check the StyleBNFIFontsize-->
							<xsl:otherwise>
								<xsl:choose>
									<!--If StyleBNFIFontsize isn't empty then use it.-->
									<xsl:when test="$StyleBNFIFontsize !=''">
										<xsl:attribute name="fontsize"><xsl:value-of select="$StyleBNFIFontsize"/></xsl:attribute>
									</xsl:when>
									<!--Otherwise don't do anything.-->
								</xsl:choose>
							</xsl:otherwise>
						</xsl:choose>
						<xsl:choose>
							<!--When the LocalBulNumFontInfoForecolor isn't empty, use it as the attribute for the paragraph. Local overrides style.-->
							<xsl:when test="$LocalBNFIForecolor!=''">
								<xsl:attribute name="forecolor"><xsl:value-of select="$LocalBNFIForecolor"/></xsl:attribute>
							</xsl:when>
							<!--When LocalBNFIForecolor is empty, then check the StyleBNFIForecolor-->
							<xsl:otherwise>
								<xsl:choose>
									<!--If StyleBNFIForecolor isn't empty then use it.-->
									<xsl:when test="$StyleBNFIForecolor !=''">
										<xsl:attribute name="forecolor"><xsl:value-of select="$StyleBNFIForecolor"/></xsl:attribute>
									</xsl:when>
									<!--Otherwise don't do anything.-->
								</xsl:choose>
							</xsl:otherwise>
						</xsl:choose>
						<xsl:choose>
							<!--When the LocalBulNumFontInfoItalic isn't empty, use it as the attribute for the paragraph. Local overrides style.-->
							<xsl:when test="$LocalBNFIItalic!=''">
								<xsl:attribute name="italic"><xsl:value-of select="$LocalBNFIItalic"/></xsl:attribute>
							</xsl:when>
							<!--When LocalBNFIItalic is empty, then check the StyleBNFIItalic-->
							<xsl:otherwise>
								<xsl:choose>
									<!--If StyleBNFIItalic isn't empty then use it.-->
									<xsl:when test="$StyleBNFIItalic !=''">
										<xsl:attribute name="italic"><xsl:value-of select="$StyleBNFIItalic"/></xsl:attribute>
									</xsl:when>
									<!--Otherwise don't do anything.-->
								</xsl:choose>
							</xsl:otherwise>
						</xsl:choose>
						<xsl:choose>
							<!--When the LocalBulNumFontInfoOffset isn't empty, use it as the attribute for the paragraph. Local overrides style.-->
							<xsl:when test="$LocalBNFIOffset!=''">
								<xsl:attribute name="offset"><xsl:value-of select="$LocalBNFIOffset"/></xsl:attribute>
							</xsl:when>
							<!--When LocalBNFIOffset is empty, then check the StyleBNFIOffset-->
							<xsl:otherwise>
								<xsl:choose>
									<!--If StyleBNFIOffset isn't empty then use it.-->
									<xsl:when test="$StyleBNFIOffset !=''">
										<xsl:attribute name="offset"><xsl:value-of select="$StyleBNFIOffset"/></xsl:attribute>
									</xsl:when>
									<!--Otherwise don't do anything.-->
								</xsl:choose>
							</xsl:otherwise>
						</xsl:choose>
						<xsl:choose>
							<!--When the LocalBulNumFontInfoFontFamily isn't empty, use it as the attribute for the paragraph. Local overrides style.-->
							<xsl:when test="$LocalBNFIFontFamily!=''">
								<xsl:attribute name="fontFamily"><xsl:value-of select="$LocalBNFIFontFamily"/></xsl:attribute>
							</xsl:when>
							<!--When LocalBNFIFontFamily is empty, then check the StyleBNFIFontFamily-->
							<xsl:otherwise>
								<xsl:choose>
									<!--If StyleBNFIFontFamily isn't empty then use it.-->
									<xsl:when test="$StyleBNFIFontFamily !=''">
										<xsl:attribute name="fontFamily"><xsl:value-of select="$StyleBNFIFontFamily"/></xsl:attribute>
									</xsl:when>
									<!--Otherwise don't do anything.-->
								</xsl:choose>
							</xsl:otherwise>
						</xsl:choose>
					</BulNumFontInfo>
				</xsl:for-each>
			</xsl:for-each>
			<!--			ATTLIST BulNumFontInfo
	backcolor CDATA #IMPLIED - No equivalent?
	bold (on | off | invert) #IMPLIED \pnb
	fontsize CDATA #IMPLIED \pnfsN (in half-points)
	forecolor CDATA #IMPLIED \pncfN
	italic (on | off | invert) #IMPLIED \pni
	offset CDATA #IMPLIED
	superscript (off | sub | super) #IMPLIED  Not currently used on bullets.
	undercolor CDATA #IMPLIED - No RTF equivalent.
	underline (none | dotted | dashed | single | double | squiggle) #IMPLIED  \pnulnone \pnuld  \pnuldash \pnul \pnuldb \pnulwave
	fontFamily CDATA #IMPLIED \pnf

	<Rules17>
				<Prop bulNumScheme="10" bulNumStartAt="1">
					<BulNumFontInfo backcolor="red" bold="invert" forecolor="black" italic="invert" offset="0mpt" undercolor="00ffc000" underline="single" fontFamily="Algerian"/>
				</Prop>
-->
		</Prop>
	</xsl:template>
	<xsl:template match="StyleRules15/Prop/@bulNumScheme"/>
	<xsl:template match="StyleRules15/Prop/@bulNumStartAt"/>
	<xsl:template match="StyleRules15/Prop/@bulNumTxtAft"/>
	<xsl:template match="StyleRules15/Prop/@bulNumTxtBef"/>
	<xsl:template match="StyleRules15/Prop/@leadingIndent"/>
	<xsl:template match="StyleRules15/Prop/@firstIndent"/>
	<xsl:template match="StyleRules15/Prop/@firstIndent"/>
	<xsl:template match="StyleRules15/Prop/BulNumFontInfo/@backcolor"/>
	<xsl:template match="StyleRules15/Prop/BulNumFontInfo/@bold"/>
	<xsl:template match="StyleRules15/Prop/BulNumFontInfo/@fontsize"/>
	<xsl:template match="StyleRules15/Prop/BulNumFontInfo/@forecolor"/>
	<xsl:template match="StyleRules15/Prop/BulNumFontInfo/@italict"/>
	<xsl:template match="StyleRules15/Prop/BulNumFontInfo/@offset"/>
	<xsl:template match="StyleRules15/Prop/BulNumFontInfo/@superscript"/>
	<xsl:template match="StyleRules15/Prop/BulNumFontInfo/@undercolor"/>
	<xsl:template match="StyleRules15/Prop/BulNumFontInfo/@underline"/>
	<xsl:template match="StyleRules15/Prop/BulNumFontInfo/@fontFamily"/>
	<xsl:template match="@* | node()">
		<xsl:copy>
			<xsl:apply-templates select="@* | node()"/>
		</xsl:copy>
	</xsl:template>
</xsl:stylesheet>
