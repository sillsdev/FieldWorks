<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="html" version="4.0" encoding="UTF-8" indent="yes" media-type="text/html; charset=utf-8"/>
	<xsl:include href="FormatCommon.xsl"/>
	<!--
================================================================
Format the xml returned from XAmple parse for user display.
================================================================
Revision History is at the end of this file.

- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Preamble
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<!-- Parameters -->
	<xsl:param name="prmIconPath">
		<xsl:text>file:///C:/fw/DistFiles/LexText/Configuration/Words/Analyses/TraceParse/</xsl:text>
	</xsl:param>
	<xsl:param name="prmAnalysisFont">
		<xsl:text>Times New Roman</xsl:text>
	</xsl:param>
	<xsl:param name="prmAnalysisFontSize">
		<xsl:text>10pt</xsl:text>
	</xsl:param>
	<xsl:param name="prmVernacularFont">
		<xsl:text>Times New Roman</xsl:text>
	</xsl:param>
	<xsl:param name="prmVernacularFontSize">
		<xsl:text>20pt</xsl:text>
	</xsl:param>
	<xsl:param name="prmVernacularRTL">
		<xsl:text>N</xsl:text>
	</xsl:param>
	<!-- Variables -->
	<!-- Colors -->
	<xsl:variable name="sSuccessColor">
		<xsl:text>green; font-weight:bold</xsl:text>
	</xsl:variable>
	<xsl:variable name="sFailureColor">
		<xsl:text>red</xsl:text>
	</xsl:variable>
	<xsl:variable name="sWordFormColor">
		<xsl:text>blue</xsl:text>
	</xsl:variable>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Main template
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="Wordform">
		<html>
			<head>
				<style type="text/css">
					.interblock {
						display: -moz-inline-box;
						display:
						inline-block;
						vertical-align: top;

					}</style>
				<xsl:call-template name="Script"/>
			</head>
			<body style="font-family:Times New Roman">
				<h1>
					<xsl:text>Parse of </xsl:text>
					<span>
						<xsl:attribute name="style">
							<xsl:text>color:</xsl:text>
							<xsl:value-of select="$sWordFormColor"/>
							<xsl:text>; font-family:</xsl:text>
							<xsl:value-of select="$prmVernacularFont"/>
						</xsl:attribute>
						<xsl:value-of select="translate(@Form,'.',' ')"/>
					</span>
					<xsl:text>.</xsl:text>
				</h1>
				<xsl:call-template name="ResultSection"/>
			</body>
		</html>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
GetAnalysisFont
	Output the analysis font information
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="GetAnalysisFont">
		<xsl:text>; font-family:</xsl:text>
		<xsl:value-of select="$prmAnalysisFont"/>
		<xsl:text>; font-size:</xsl:text>
		<xsl:value-of select="$prmAnalysisFontSize"/>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
GetVernacularFont
	Output the vernacular font information
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="GetVernacularFont">
		<xsl:text>; font-family:</xsl:text>
		<xsl:value-of select="$prmVernacularFont"/>
		<xsl:text>; font-size:</xsl:text>
		<xsl:value-of select="$prmVernacularFontSize"/>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ResultSection
	Output the Results section
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="ResultSection">
		<h2>Result</h2>
		<xsl:choose>
			<xsl:when test="//Morphs">
				<p>
					<xsl:text>This word parsed successfully.  The following are the sequences of allomorphs that succeeded:</xsl:text>
				</p>
				<div>
					<xsl:if test="$prmVernacularRTL='Y'">
						<xsl:attribute name="style">direction:rtl; text-align:right</xsl:attribute>
					</xsl:if>
					<xsl:call-template name="ShowSuccessfulAnalyses"/>
				</div>
				<xsl:if test="//morph[@type='ifx']">
					<p>Note: when there are infixes, sometimes a successful parse will be shown more
						than once.</p>
				</xsl:if>
				<xsl:call-template name="ShowAnyLoadErrors"/>
			</xsl:when>
			<xsl:otherwise>
				<p>
					<span>
						<xsl:attribute name="style">
							<xsl:text>color:</xsl:text>
							<xsl:value-of select="$sFailureColor"/>
						</xsl:attribute>
						<xsl:text>This word failed to parse successfully.</xsl:text>
					</span>
				</p>
				<xsl:if test="//Error">
					<p>
						<span>
							<xsl:attribute name="style">
								<xsl:text>font-size:larger; color:</xsl:text>
								<xsl:value-of select="$sFailureColor"/>
							</xsl:attribute>
							<xsl:text>An error was detected!  </xsl:text>
							<xsl:value-of select="//Error"/>
						</span>
					</p>
				</xsl:if>
				<xsl:call-template name="ShowAnyLoadErrors"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ShowMsaInfo
	Show the information associated with the msa
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="ShowMsaInfo">
		<xsl:param name="morph"/>
		<xsl:for-each select="$morph">
			<xsl:variable name="form" select="../MoForm"/>
			<span>
				<xsl:attribute name="style">
					<xsl:text>; font-size:smaller</xsl:text>
				</xsl:attribute>
				<xsl:choose>
					<xsl:when test="$form/@wordType='root'">
						<xsl:text>Category = </xsl:text>
						<span>
							<xsl:attribute name="style">
								<xsl:call-template name="GetAnalysisFont"/>
							</xsl:attribute>
							<xsl:value-of select="stemMsa/@catAbbr"/>
						</span>
						<xsl:if test="stemMsa/@inflClassAbbr != ''">
							<xsl:text>; Inflection class = </xsl:text>
							<span>
								<xsl:attribute name="style">
									<xsl:call-template name="GetAnalysisFont"/>
								</xsl:attribute>
								<xsl:value-of select="stemMsa/@inflClassAbbr"/>
							</span>
						</xsl:if>
					</xsl:when>
					<xsl:when test="contains($form/@wordType,'deriv')">
						<xsl:text>From category = </xsl:text>
						<span>
							<xsl:attribute name="style">
								<xsl:call-template name="GetAnalysisFont"/>
							</xsl:attribute>
							<xsl:value-of select="derivMsa/@fromCatAbbr"/>
						</span>
						<xsl:text>; To category = </xsl:text>
						<span>
							<xsl:attribute name="style">
								<xsl:call-template name="GetAnalysisFont"/>
							</xsl:attribute>
							<xsl:value-of select="derivMsa/@toCatAbbr"/>
						</span>
						<xsl:if test="derivMsa/@toInflClassAbbr!=''">
							<xsl:text>; To inflection class = </xsl:text>
							<span>
								<xsl:attribute name="style">
									<xsl:call-template name="GetAnalysisFont"/>
								</xsl:attribute>
								<xsl:value-of select="derivMsa/@toInflClassAbbr"/>
							</span>
						</xsl:if>
					</xsl:when>
					<xsl:when test="$form/@wordType='prefix' or $form/@wordType='suffix'">
						<xsl:text>unclassified affix</xsl:text>
						<xsl:if test="unclassMsa/@fromCatAbbr">
							<span>
								<xsl:attribute name="style">
									<xsl:call-template name="GetAnalysisFont"/>
								</xsl:attribute>
								<xsl:value-of select="unclassMsa/@fromCatAbbr"/>
							</span>
						</xsl:if>
					</xsl:when>
					<xsl:when test="$form/@wordType='proclitic' or $form/@wordType='enclitic'">
						<xsl:value-of select="$form/@wordType"/>
						<xsl:if test="stemMsa/@cat!=0">
							<xsl:text>; Category = </xsl:text>
							<span>
								<xsl:attribute name="style">
									<xsl:call-template name="GetAnalysisFont"/>
								</xsl:attribute>
								<xsl:value-of select="stemMsa/@catAbbr"/>
								<xsl:text>; Attaches to: </xsl:text>
								<xsl:variable name="fromPOSes" select="stemMsa/fromPartsOfSpeech"/>
								<xsl:choose>
									<xsl:when test="$fromPOSes">
										<xsl:for-each select="$fromPOSes">
											<xsl:if test="position() &gt; 1">
												<xsl:text>, </xsl:text>
												<xsl:if test="position() = last()">
													<xsl:text>or </xsl:text>
												</xsl:if>
											</xsl:if>
											<xsl:value-of select="@fromCatAbbr"/>
										</xsl:for-each>
									</xsl:when>
									<xsl:otherwise>
										<xsl:text>Any category</xsl:text>
									</xsl:otherwise>
								</xsl:choose>
							</span>
						</xsl:if>
					</xsl:when>
					<xsl:when test="$form/@wordType='clitic'">
						<xsl:text>clitic</xsl:text>
						<xsl:if test="stemMsa/@cat!=0">
							<xsl:text>; Category = </xsl:text>
							<span>
								<xsl:attribute name="style">
									<xsl:call-template name="GetAnalysisFont"/>
								</xsl:attribute>
								<xsl:value-of select="stemMsa/@catAbbr"/>
							</span>
						</xsl:if>
					</xsl:when>
					<xsl:otherwise>
						<!-- an inflectional affix -->
						<xsl:text>Category = </xsl:text>
						<span>
							<xsl:attribute name="style">
								<xsl:call-template name="GetAnalysisFont"/>
							</xsl:attribute>
							<xsl:value-of select="inflMsa/@catAbbr"/>
						</span>
						<xsl:text>; Slot = </xsl:text>
						<xsl:choose>
							<xsl:when test="inflMsa/@slotAbbr!='??'">
								<xsl:if test="inflMsa/@slotOptional='true'">
									<xsl:text>(</xsl:text>
								</xsl:if>
								<span>
									<xsl:attribute name="style">
										<xsl:call-template name="GetAnalysisFont"/>
									</xsl:attribute>
									<xsl:value-of select="inflMsa/@slotAbbr"/>
								</span>
								<xsl:if test="inflMsa/@slotOptional='true'">
									<xsl:text>)</xsl:text>
								</xsl:if>
							</xsl:when>
							<xsl:otherwise>
								<xsl:text>; Unspecified slot or category</xsl:text>
							</xsl:otherwise>
						</xsl:choose>
					</xsl:otherwise>
				</xsl:choose>
			</span>
		</xsl:for-each>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		Script
		Output the JavaScript script to handle dynamic "tree"
		Parameters: none
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="Script">
		<script language="JavaScript" id="clientEventHandlersJS">
			<xsl:text>
			function JumpToToolBasedOnHvo(hvo)
			{
			window.external.JumpToToolBasedOnHvo(hvo);
			}
			function MouseMove()
			{
			window.external.MouseMove();
			}
			</xsl:text>
		</script>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ShowMorph
	Show the morpheme information
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="ShowMorph">
		<span class="interblock">
			<table cellpadding="0" cellspacing="0">
				<xsl:attribute name="style">
					<xsl:text>color:</xsl:text>
					<xsl:value-of select="$sSuccessColor"/>
					<xsl:if test="$prmVernacularRTL='Y'">
						<xsl:text>; text-align:right;</xsl:text>
					</xsl:if>
				</xsl:attribute>
				<xsl:attribute name="onclick">
					<xsl:text>JumpToToolBasedOnHvo(</xsl:text>
					<xsl:value-of select="MoForm/@DbRef"/>
					<xsl:text>)</xsl:text>
				</xsl:attribute>
				<xsl:attribute name="onmousemove">
					<xsl:text>MouseMove()</xsl:text>
				</xsl:attribute>
				<tr>
					<td>
						<xsl:attribute name="style">
							<xsl:if test="$prmVernacularRTL='Y'">
								<xsl:text>direction:rtl</xsl:text>
							</xsl:if>
							<xsl:call-template name="GetVernacularFont"/>
						</xsl:attribute>
						<xsl:value-of select="alloform"/>
					</td>
				</tr>
				<tr>
					<td>
						<xsl:attribute name="style">
							<xsl:if test="$prmVernacularRTL='Y'">
								<xsl:text>direction:rtl</xsl:text>
							</xsl:if>
							<xsl:call-template name="GetVernacularFont"/>
						</xsl:attribute>
						<xsl:value-of select="citationForm"/>
					</td>
				</tr>
				<tr>
					<td>
						<xsl:attribute name="style">
							<xsl:if test="$prmVernacularRTL='Y'">
								<xsl:text>direction:ltr</xsl:text>
							</xsl:if>
							<xsl:call-template name="GetAnalysisFont"/>
						</xsl:attribute>
						<xsl:variable name="sGloss" select="gloss"/>
						<xsl:choose>
							<xsl:when test="string-length($sGloss) &gt; 0">
								<xsl:value-of select="$sGloss"/>
							</xsl:when>
							<xsl:otherwise>
								<xsl:text>&#xa0;</xsl:text>
							</xsl:otherwise>
						</xsl:choose>
					</td>
				</tr>
				<tr>
					<td>
						<xsl:if test="$prmVernacularRTL='Y'">
							<xsl:attribute name="style">direction:ltr</xsl:attribute>
						</xsl:if>
						<xsl:call-template name="ShowMsaInfo">
							<xsl:with-param name="morph" select="MSI"/>
						</xsl:call-template>
					</td>
				</tr>
			</table>
		</span>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ShowSuccessfulAnalyses
	Show all analyses that succeeded
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="ShowSuccessfulAnalyses">
		<table border="0" style="cursor:pointer;">
			<xsl:for-each select="//WfiAnalysis/Morphs">
				<xsl:call-template name="ShowSuccessfulAnalysis"/>
			</xsl:for-each>
		</table>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ShowSuccessfulAnalysis
	Show a successful analysis
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="ShowSuccessfulAnalysis">
		<tr>
			<td>
				<table border="1">
					<tr>
						<!-- Note: do not need to do any re-ordering for RTL; the browser does it. -->
						<xsl:for-each select="Morph">
							<xsl:if test="not(lexEntryInflType)">
								<!-- when we have only two child nodes, it is a case of being one of the null allomorphs we create when building the
									   input for the parser in order to still get the Word Grammar to have something in any
									   required slots in affix templates. -->
								<td valign="top">
									<xsl:call-template name="ShowMorph"/>
								</td>
							</xsl:if>
						</xsl:for-each>
					</tr>
				</table>
			</td>
		</tr>
	</xsl:template>
</xsl:stylesheet>
