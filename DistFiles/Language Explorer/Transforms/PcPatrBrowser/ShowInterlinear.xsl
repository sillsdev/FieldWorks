<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="html" version="4.0" encoding="UTF-8" indent="yes"/>
	<!--
================================================================
Format ANA interlinear
================================================================
Revision History is at the end of this file.

- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Preamble
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:param name="sDecompSeparationCharacter" select="'-'"/>
	<!-- colors -->
	<xsl:param name="sCategoryColor" select="'red'"/>
	<xsl:param name="sDecompColor" select="'blue'"/>
	<xsl:param name="sFeatDescColor" select="'maroon'"/>
	<xsl:param name="sGlossColor" select="'green'"/>
	<xsl:param name="sUnderFormColor" select="'navy'"/>
	<!-- fonts -->
	<xsl:param name="sCategoryFont" select="'Times New Roman'"/>
	<xsl:param name="sDecompFont" select="'Courier New'"/>
	<xsl:param name="sFeatDescFont" select="'Times New Roman'"/>
	<xsl:param name="sFormatFont" select="'Times New Roman'"/>
	<xsl:param name="sGlossFont" select="'Courier New'"/>
	<xsl:param name="sOrigWordFont" select="'Courier New'"/>
	<xsl:param name="sUnderFormFont" select="'Courier New'"/>
	<!-- text message -->
	<xsl:param name="sTextMessage" select="'Sentence 1 of 33'"/>
	<xsl:param name="bParseAccepted" select="'N'"/>
	<xsl:param name="bRightToLeft" select="'Y'"/>

	<xsl:variable name="bDoUnderlyingForm" select="//UnderForm"/>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Main template
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="/Input">
		<html>
			<head>
				<meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
			</head>
			<body>
				<table>
					<tr>
						<td valign="top">
							<table border="0">
								<tr>
									<td>
										<xsl:attribute name="style">
											<xsl:text>font-size=75%</xsl:text>
											<xsl:if test="$bParseAccepted='Y'">
												<xsl:text>; background-color:yellow</xsl:text>
											</xsl:if>
										</xsl:attribute>
										<xsl:value-of select="$sTextMessage"/>
									</td>
								</tr>
							</table>
						</td>
						<td>
							<table border="1">
								<tr>
									<xsl:choose>
										<xsl:when test="$bRightToLeft='Y'">
											<xsl:for-each select="Word">
												<xsl:sort select="position()" order="descending" data-type="number"/>
												<xsl:call-template name="DoWord"/>
											</xsl:for-each>
										</xsl:when>
										<xsl:otherwise>
											<xsl:for-each select="Word">
												<xsl:call-template name="DoWord"/>
											</xsl:for-each>
										</xsl:otherwise>
									</xsl:choose>
								</tr>
							</table>
						</td>
					</tr>
					<tr>
						<td/>
						<td>
						<xsl:attribute name="style"><xsl:text>font-family:</xsl:text>
					<xsl:value-of select="$sFormatFont"/>
						</xsl:attribute>
							<xsl:value-of select="Word[1]/Format"/>
						</td>
					</tr>
				</table>
			</body>
		</html>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoCategoryInfo
	Output category info
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="DoCategoryInfo">
		<tr>
			<td>
				<xsl:attribute name="style">color=<xsl:value-of select="$sCategoryColor"/>
					<xsl:text>;font-family:</xsl:text>
					<xsl:value-of select="$sCategoryFont"/>
				</xsl:attribute>
				<xsl:for-each select="WordParse">
					<xsl:for-each select="Morphs/*">
						<xsl:value-of select="MorphCat"/>
						<xsl:call-template name="DoSeparationCharacter"/>
					</xsl:for-each>
					<xsl:call-template name="DoParseSeparator"/>
				</xsl:for-each>
			</td>
		</tr>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoDecompInfo
	Output decomposition info
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="DoDecompInfo">
		<tr>
			<td>
				<xsl:attribute name="style">color=<xsl:value-of select="$sDecompColor"/>
					<xsl:text>;font-family:</xsl:text>
					<xsl:value-of select="$sDecompFont"/>
				</xsl:attribute>
				<xsl:for-each select="WordParse">
					<xsl:for-each select="Morphs/*">
						<xsl:value-of select="Decomp"/>
						<xsl:call-template name="DoSeparationCharacter"/>
					</xsl:for-each>
					<xsl:call-template name="DoParseSeparator"/>
				</xsl:for-each>
			</td>
		</tr>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoFeatDescInfo
	Output feature descriptor info
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="DoFeatDescInfo">
		<tr>
			<td>
				<xsl:attribute name="style">color=<xsl:value-of select="$sFeatDescColor"/>
					<xsl:text>;font-family:</xsl:text>
					<xsl:value-of select="$sFeatDescFont"/>
				</xsl:attribute>
				<xsl:for-each select="WordParse">
					<xsl:for-each select="Morphs/*">
						<xsl:value-of select="FeatDesc"/>
						<xsl:call-template name="DoSeparationCharacter"/>
					</xsl:for-each>
					<xsl:call-template name="DoParseSeparator"/>
				</xsl:for-each>
			</td>
		</tr>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoGlossInfo
	Output gloss info
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="DoGlossInfo">
		<tr>
			<td>
				<xsl:attribute name="style">color=<xsl:value-of select="$sGlossColor"/>
					<xsl:text>;font-family:</xsl:text>
					<xsl:value-of select="$sGlossFont"/>
				</xsl:attribute>
				<xsl:for-each select="WordParse">
					<xsl:for-each select="Morphs/*">
						<xsl:value-of select="Morph"/>
						<xsl:call-template name="DoSeparationCharacter"/>
					</xsl:for-each>
					<xsl:call-template name="DoParseSeparator"/>
				</xsl:for-each>
			</td>
		</tr>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoParseSeparator
	Output any needed separation character between parses
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="DoParseSeparator">
		<xsl:if test="position()!=last()">
			<xsl:text>%</xsl:text>
		</xsl:if>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoSeparationCharacter
	Output any needed separation character
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="DoSeparationCharacter">
		<xsl:if test="position()!=last()">
			<xsl:value-of select="$sDecompSeparationCharacter"/>
		</xsl:if>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoUnderlyingFormInfo
	Output gloss info
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="DoUnderlyingFormInfo">
		<xsl:if test="$bDoUnderlyingForm">
			<tr>
				<td>
					<xsl:attribute name="style">color=<xsl:value-of select="$sUnderFormColor"/>
						<xsl:text>;font-family:</xsl:text>
						<xsl:value-of select="$sUnderFormFont"/>
					</xsl:attribute>
					<xsl:for-each select="WordParse">
						<xsl:for-each select="Morphs/*">
							<xsl:value-of select="UnderForm"/>
							<xsl:call-template name="DoSeparationCharacter"/>
						</xsl:for-each>
						<xsl:call-template name="DoParseSeparator"/>
					</xsl:for-each>
				</td>
			</tr>
		</xsl:if>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoWord
	Output gloss info
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="DoWord">
		<td valign="top">
			<table>
			<xsl:if test="$bRightToLeft='Y'">
			<xsl:attribute name="align"><xsl:text>right</xsl:text></xsl:attribute>
			<xsl:attribute name="style"><xsl:text>direction:rtl</xsl:text></xsl:attribute>
			</xsl:if>
				<tr>
					<td>
						<span>
							<xsl:attribute name="style">
								<xsl:text>font-weight='bold';font-family:</xsl:text>
								<xsl:value-of select="$sOrigWordFont"/>
							</xsl:attribute>
							<xsl:value-of select="OrigWord"/>
						</span>
						<xsl:value-of select="NonAlpha"/>
					</td>
				</tr>
				<tr>
					<td>
						<table cellpadding="0" cellspacing="0">
							<tr>
								<td valign="top" style="padding-right='2pt'">
									<xsl:variable name="iCountParses" select="count(WordParse)"/>
									<xsl:if test="$iCountParses &gt; 1">
										<xsl:value-of select="$iCountParses"/>
									</xsl:if>
								</td>
								<td valign="top">
									<table cellpadding="0" cellspacing="0">
										<xsl:call-template name="DoDecompInfo"/>
										<xsl:call-template name="DoGlossInfo"/>
										<xsl:call-template name="DoUnderlyingFormInfo"/>
										<xsl:call-template name="DoCategoryInfo"/>
										<xsl:call-template name="DoFeatDescInfo"/>
									</table>
								</td>
							</tr>
						</table>
					</td>
				</tr>
			</table>
		</td>
	</xsl:template>
</xsl:stylesheet>
<!--
================================================================
Revision History
- - - - - - - - - - - - - - - - - - -
22-Aug-2018    Andy Black    Only show underlying form if needed; add more space between count and items
21-Sep-2005    Andy Black    Use RTL flow and right-justify;
												  fix ordering for RTL to sort correctly when more than 9 words
26-Aug-2005	Andy Black	Initial Draft
================================================================
 -->
