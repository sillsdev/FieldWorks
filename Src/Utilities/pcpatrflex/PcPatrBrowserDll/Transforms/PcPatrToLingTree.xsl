<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="xml" version="1.0" encoding="UTF-8" indent="yes"/>
	<!--
================================================================
Test unification via XSLT
  Input:    Pairs of tests to apply
  Output: should be identical to input
================================================================
Revision History is at the end of this file.

- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Preamble
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:param name="sGlossFont">Times New Roman</xsl:param>
	<xsl:param name="sGlossFontSize">12</xsl:param>
	<xsl:param name="sGlossFontColor">Green</xsl:param>
	<xsl:param name="sLexFont">Courier New</xsl:param>
	<xsl:param name="sLexFontSize">11</xsl:param>
	<xsl:param name="sLexFontColor">Blue</xsl:param>
	<xsl:param name="sNTFont">Times New Roman</xsl:param>
	<xsl:param name="sNTFontSize">14</xsl:param>
	<xsl:param name="sNTFontColor">Black</xsl:param>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Main template
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="/">
		<LingTree>
			<Parameters>
				<Layout>
					<VerticalGap>300</VerticalGap>
					<HorizontalGap>300</HorizontalGap>
					<InitialXCoord>100</InitialXCoord>
					<InitialYCoord>300</InitialYCoord>
					<HorizontalOffset>5529</HorizontalOffset>
					<LexGlossGapAdjustment>10</LexGlossGapAdjustment>
					<ShowFlatView>false</ShowFlatView>
				</Layout>
				<Fonts>
					<Gloss>
						<GlossFontFace>
							<xsl:value-of select="$sGlossFont"/>
						</GlossFontFace>
						<GlossFontSize>
							<xsl:value-of select="$sGlossFontSize"/>
						</GlossFontSize>
						<GlossFontStyle>Regular</GlossFontStyle>
						<GlossColorArgb>-16744447</GlossColorArgb>
						<GlossColorName>
							<xsl:value-of select="$sGlossFontColor"/>
						</GlossColorName>
					</Gloss>
					<Lex>
						<LexFontFace>
							<xsl:value-of select="$sLexFont"/>
						</LexFontFace>
						<LexFontSize>
							<xsl:value-of select="$sLexFontSize"/>
						</LexFontSize>
						<LexFontStyle>Regular</LexFontStyle>
						<LexColorArgb>-16776961</LexColorArgb>
						<LexColorName>
							<xsl:value-of select="$sLexFontColor"/>
						</LexColorName>
					</Lex>
					<NT>
						<NTFontFace>
							<xsl:value-of select="$sNTFont"/>
						</NTFontFace>
						<NTFontSize>
							<xsl:value-of select="$sNTFontSize"/>
						</NTFontSize>
						<NTFontStyle>Regular</NTFontStyle>
						<NTColorArgb>-16777216</NTColorArgb>
						<NTColorName>
							<xsl:value-of select="$sNTFontColor"/>
						</NTColorName>
					</NT>
					<Lines>
						<LineWidth>15</LineWidth>
						<LinesColorArgb>-16777216</LinesColorArgb>
						<LinesColorName>Black</LinesColorName>
					</Lines>
					<Background>
						<BackgroundColorArgb>-1</BackgroundColorArgb>
						<BackgroundColorName>White</BackgroundColorName>
					</Background>
				</Fonts>
			</Parameters>
			<TreeDescription>
				<xsl:apply-templates/>
			</TreeDescription>
		</LingTree>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Node
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="Node">
		<node type="nonterminal">
			<xsl:attribute name="id">
				<xsl:value-of select="@id"/>
			</xsl:attribute>
			<label>
				<xsl:value-of select="@cat"/>
				<xsl:if test="@all='true'">+</xsl:if>
				<xsl:if test="@fail='true'">-</xsl:if>
			</label>
			<xsl:apply-templates/>
		</node>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Leaf
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="Leaf">
		<node type="nonterminal">
			<xsl:attribute name="id">
				<xsl:value-of select="@id"/>
			</xsl:attribute>
			<label>
				<xsl:value-of select="@cat"/>
			</label>
			<node type="lex">
				<xsl:attribute name="id">
					<xsl:value-of select="@id"/>lex</xsl:attribute>
				<label>
					<xsl:choose>
						<xsl:when test="Fs/F[@name='decomposition']/Str">
							<xsl:value-of select="Fs/F[@name='decomposition']/Str"/>
						</xsl:when>
						<xsl:otherwise>
							<xsl:value-of select="Str"/>
						</xsl:otherwise>
					</xsl:choose>
				</label>
				<node type="gloss">
					<xsl:attribute name="id">
						<xsl:value-of select="@id"/>gloss</xsl:attribute>
					<label>
						<xsl:choose>
							<xsl:when test="Fs/F[@name='gloss']/Str">
								<xsl:value-of select="Fs/F[@name='gloss']/Str"/>
							</xsl:when>
							<xsl:otherwise>
								<xsl:value-of select="@gloss"/>
							</xsl:otherwise>
						</xsl:choose>
					</label>
				</node>
			</node>
		</node>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Elements to ignore
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="Fs | F | Str | Lexfs"/>
</xsl:stylesheet>
<!--
================================================================
Revision History
- - - - - - - - - - - - - - - - - - -
15-Nov-2005    Andy Black    Increase InitialYCoord value so the Parse x of y does not conflict with tree
21-Sep-2005    Andy Black    Add plus sign to nodes with @all='true'
08-Dec-2004    Andy Black  Began working on Initial Draft
================================================================
 -->
