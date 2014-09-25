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
	<xsl:param name="prmShowTrace">
		<xsl:text>true</xsl:text>
	</xsl:param>
	<!-- Variables -->
	<xsl:variable name="selectedMorphs" select="/Wordform/selectedMorphs"/>
	<xsl:variable name="analysisPhonologicalRules" select="/Wordform/Trace/WordAnalysisTrace/PhonologicalRuleAnalysisTrace"/>
	<xsl:variable name="bDoDebug" select="'N'"/>
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
				<xsl:call-template name="Script"/>
				<style type="text/css">
					.interblock {
						display: -moz-inline-box;
						display:
						inline-block;
						vertical-align: top;
					}</style>
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
						<xsl:value-of select="translate(@form,'.',' ')"/>
					</span>
					<xsl:text>.</xsl:text>
				</h1>
				<xsl:call-template name="ResultSection"/>
				<xsl:if test="$prmShowTrace = 'true'">
					<xsl:call-template name="TraceSection"/>
				</xsl:if>
			</body>
		</html>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		DetermineIfMoreToShow
		Figure out if the current item has nested nodes that need to be shown
		Parameters: none
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="DetermineIfMoreToShow">
		<xsl:param name="traceRoot"/>
		<xsl:choose>
			<xsl:when test="$traceRoot[name() = 'ParseCompleteTrace']">
				<!-- The parse is complete, so there is no more to show. -->
			</xsl:when>
			<xsl:otherwise>
				<xsl:variable name="analysisAffixes" select="$traceRoot/MorphologicalRuleAnalysisTrace[MorphologicalRule/@type = 'affix']"/>
				<xsl:variable name="synthesisAffixes" select="$traceRoot/MorphologicalRuleSynthesisTrace[MorphologicalRule/@type = 'affix']"/>
				<xsl:variable name="analysisCompoundRules" select="$traceRoot/MorphologicalRuleAnalysisTrace[MorphologicalRule/@type = 'compound']"/>
				<xsl:variable name="synthesisCompoundRules" select="$traceRoot/MorphologicalRuleSynthesisTrace[MorphologicalRule/@type = 'compound']"/>
				<xsl:variable name="synthesizedWords" select="$traceRoot/LexLookupTrace/WordSynthesisTrace"/>
				<xsl:variable name="parseCompleteTraces" select="$traceRoot/ParseCompleteTrace"/>
				<xsl:variable name="parseNodes" select="$analysisAffixes | $synthesisAffixes | $analysisCompoundRules | $synthesisCompoundRules | $synthesizedWords | $parseCompleteTraces"/>
				<xsl:if test="$parseNodes">
					<xsl:text>Y</xsl:text>
				</xsl:if>
			</xsl:otherwise>
		</xsl:choose>
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
		GetSuccessOrFailureColor
		Output the appropriate color
		Parameters: none
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="GetSuccessOrFailureColor">
		<xsl:choose>
			<xsl:when test="descendant-or-self::ParseCompleteTrace[@success = 'true']">
				<xsl:value-of select="$sSuccessColor"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="$sFailureColor"/>
			</xsl:otherwise>
		</xsl:choose>
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
		OutputListPunctuation
		display information at end of a list for each member of that list
		Parameters: none
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="OutputListPunctuation">
		<xsl:param name="sConjunction" select="' and '"/>
		<xsl:param name="sFinalPunctuation" select="'.'"/>
		<xsl:choose>
			<xsl:when test="position()='1' and position()=last()-1">
				<xsl:value-of select="$sConjunction"/>
			</xsl:when>
			<xsl:when test="position()=last()-1">
				<xsl:text>,</xsl:text>
				<xsl:value-of select="$sConjunction"/>
			</xsl:when>
			<xsl:when test="position()=last()">
				<xsl:value-of select="$sFinalPunctuation"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:text>, </xsl:text>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>    <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ResultSection
	Output the Results section
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="ResultSection">
		<h2>Result</h2>
		<xsl:choose>
			<xsl:when test="/Wordform/Analysis">
				<p>
					<xsl:text>This word parsed successfully.  The following are the sequences of allomorphs that succeeded:</xsl:text>
				</p>
				<div>
					<xsl:if test="$prmVernacularRTL='Y'">
						<xsl:attribute name="style">direction:rtl; text-align:right</xsl:attribute>
					</xsl:if>
					<xsl:call-template name="ShowSuccessfulAnalyses"/>
				</div>
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
				<xsl:if test="/Wordform/Error">
					<p>
						<span>
							<xsl:attribute name="style">
								<xsl:text>font-size:larger; color:</xsl:text>
								<xsl:value-of select="$sFailureColor"/>
							</xsl:attribute>
							<xsl:text>An error was detected!  </xsl:text>
							<xsl:value-of select="/Wordform/Error"/>
						</span>
					</p>
				</xsl:if>
			</xsl:otherwise>
		</xsl:choose>
		<xsl:call-template name="ShowAnyLoadErrors"/>
		<xsl:call-template name="ShowAnyDataIssues"/>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ShowMsaInfo
	Show the information associated with the msa
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="ShowMsaInfo">
		<xsl:param name="msa"/>
		<xsl:for-each select="$msa">
			<xsl:variable name="form" select=".."/>
			<span>
				<xsl:attribute name="style">
					<xsl:text>; font-size:smaller</xsl:text>
				</xsl:attribute>
				<xsl:choose>
					<xsl:when test="@type = 'infl'">
						<xsl:text>Category = </xsl:text>
						<span>
							<xsl:attribute name="style">
								<xsl:call-template name="GetAnalysisFont"/>
							</xsl:attribute>
							<xsl:choose>
								<xsl:when test="Category">
									<xsl:value-of select="Category"/>
								</xsl:when>
								<xsl:otherwise>
									<xsl:text>??</xsl:text>
								</xsl:otherwise>
							</xsl:choose>
						</span>
						<xsl:choose>
							<xsl:when test="Slot">
								<xsl:text>; Slot = </xsl:text>
								<xsl:if test="Slot/@optional='true'">
									<xsl:text>(</xsl:text>
								</xsl:if>
								<span>
									<xsl:attribute name="style">
										<xsl:call-template name="GetAnalysisFont"/>
									</xsl:attribute>
									<xsl:value-of select="Slot"/>
								</span>
								<xsl:if test="Slot/@optional='true'">
									<xsl:text>)</xsl:text>
								</xsl:if>
							</xsl:when>
							<xsl:otherwise>
								<xsl:text>; Unspecified slot or category</xsl:text>
							</xsl:otherwise>
						</xsl:choose>
					</xsl:when>
					<xsl:when test="@type ='deriv'">
						<xsl:text>From category = </xsl:text>
						<span>
							<xsl:attribute name="style">
								<xsl:call-template name="GetAnalysisFont"/>
							</xsl:attribute>
							<xsl:choose>
								<xsl:when test="FromCategory">
									<xsl:value-of select="FromCategory"/>
								</xsl:when>
								<xsl:otherwise>
									<xsl:text>??</xsl:text>
								</xsl:otherwise>
							</xsl:choose>
						</span>
						<xsl:text>; To category = </xsl:text>
						<span>
							<xsl:attribute name="style">
								<xsl:call-template name="GetAnalysisFont"/>
							</xsl:attribute>
							<xsl:choose>
								<xsl:when test="ToCategory">
									<xsl:value-of select="ToCategory"/>
								</xsl:when>
								<xsl:otherwise>
									<xsl:text>??</xsl:text>
								</xsl:otherwise>
							</xsl:choose>
						</span>
						<xsl:if test="ToInflClass">
							<xsl:text>; To inflection class = </xsl:text>
							<span>
								<xsl:attribute name="style">
									<xsl:call-template name="GetAnalysisFont"/>
								</xsl:attribute>
								<xsl:value-of select="ToInflClass"/>
							</span>
						</xsl:if>
					</xsl:when>
					<xsl:when test="@type = 'unclass'">
						<xsl:text>unclassified affix</xsl:text>
						<xsl:if test="Category">
							<span>
								<xsl:attribute name="style">
									<xsl:call-template name="GetAnalysisFont"/>
								</xsl:attribute>
								<text> </text>
								<xsl:value-of select="Category"/>
							</span>
						</xsl:if>
					</xsl:when>
					<xsl:when test="$form/@type = 'proclitic' or $form/@type = 'enclitic'">
						<xsl:value-of select="$form/@wordType"/>
						<xsl:if test="stemMsa/@cat!=0">
							<xsl:text>; Category = </xsl:text>
							<span>
								<xsl:attribute name="style">
									<xsl:call-template name="GetAnalysisFont"/>
								</xsl:attribute>
								<xsl:choose>
									<xsl:when test="Category">
										<xsl:value-of select="Category"/>
									</xsl:when>
									<xsl:otherwise>
										<xsl:text>??</xsl:text>
									</xsl:otherwise>
								</xsl:choose>
								<xsl:text>; Attaches to: </xsl:text>
								<xsl:choose>
									<xsl:when test="FromCategories">
										<xsl:for-each select="FromCategories/Category">
											<xsl:if test="position() &gt; 1">
												<xsl:text>, </xsl:text>
												<xsl:if test="position() = last()">
													<xsl:text>or </xsl:text>
												</xsl:if>
											</xsl:if>
											<span>
												<xsl:attribute name="style">
													<xsl:call-template name="GetAnalysisFont"/>
												</xsl:attribute>
												<xsl:value-of select="."/>
											</span>
										</xsl:for-each>
									</xsl:when>
									<xsl:otherwise>
										<xsl:text>Any category</xsl:text>
									</xsl:otherwise>
								</xsl:choose>
							</span>
						</xsl:if>
					</xsl:when>
					<xsl:when test="$form/@type = 'clitic'">
						<xsl:text>clitic</xsl:text>
						<xsl:if test="Category">
							<xsl:text>; Category = </xsl:text>
							<span>
								<xsl:attribute name="style">
									<xsl:call-template name="GetAnalysisFont"/>
								</xsl:attribute>
								<xsl:value-of select="Category"/>
							</span>
						</xsl:if>
					</xsl:when>
					<xsl:otherwise>
						<!-- stem or root -->
						<xsl:text>Category = </xsl:text>
						<span>
							<xsl:attribute name="style">
								<xsl:call-template name="GetAnalysisFont"/>
							</xsl:attribute>
							<xsl:choose>
								<xsl:when test="Category">
									<xsl:value-of select="Category"/>
								</xsl:when>
								<xsl:otherwise>
									<xsl:text>??</xsl:text>
								</xsl:otherwise>
							</xsl:choose>
						</span>
						<xsl:if test="InflClass">
							<xsl:text>; Inflection class = </xsl:text>
							<span>
								<xsl:attribute name="style">
									<xsl:call-template name="GetAnalysisFont"/>
								</xsl:attribute>
								<xsl:value-of select="InflClass"/>
							</span>
						</xsl:if>
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
	function ButtonShowWGDetails(nodeId)
	{
		window.external.ShowWordGrammarDetail(nodeId);
	}
	function JumpToToolBasedOnHvo(hvo)
	{
		window.external.JumpToToolBasedOnHvo(hvo);
	}
	function MouseMove()
	{
		window.external.MouseMove();
	}
	function ButtonShowDetails()
	{
	if (TraceSection.style.display == 'none')
	{
	  TraceSection.style.display = 'block';
	  ShowDetailsButton.value = "Hide Details";
	}
	else
	{
	  TraceSection.style.display = 'none';
	  ShowDetailsButton.value = "Show Details";
	}
	}
	// Center the mouse position in the browser
	function CenterNodeInBrowser(node)
	{
	var posx = 0;
	var posy = 0;
	if (!e) var e = window.event;
	if (e.pageX || e.pageY)
	{
		posx = e.pageX;
		posy = e.pageY;
	}
	else if (e.clientX || e.clientY)
	{
		posx = e.clientX + document.body.scrollLeft;
		posy = e.clientY + document.body.scrollTop;
	}
	// posx and posy contain the mouse position relative to the document
	curY = findPosY(node);
	offset = document.body.clientHeight/2;
	window.scrollTo(0, curY-offset); // scroll to about the middle if possible
	}
	// findPosX() and findPosY() are from http://www.quirksmode.org/js/findpos.html
	function findPosX(obj)
{
	var curleft = 0;
	if (obj.offsetParent)
	{
		while (obj.offsetParent)
		{
			curleft += obj.offsetLeft
			obj = obj.offsetParent;
		}
	}
	else if (obj.x)
		curleft += obj.x;
	return curleft;
}

function findPosY(obj)
{
	var curtop = 0;
	if (obj.offsetParent)
	{
		while (obj.offsetParent)
		{
			curtop += obj.offsetTop
			obj = obj.offsetParent;
		}
	}
	else if (obj.y)
		curtop += obj.y;
	return curtop;
}

// nextSibling function that skips over textNodes.
function NextNonTextSibling(node)
{
	while(node.nextSibling.nodeName == "#text")
		node = node.nextSibling;

	return node.nextSibling;
}

// This script based on the one given in http://www.codeproject.com/jscript/dhtml_treeview.asp.
function Toggle(node, path, imgOffset)
{

	Images = new Array('beginminus.gif', 'beginplus.gif', 'lastminus.gif', 'lastplus.gif', 'minus.gif', 'plus.gif', 'singleminus.gif', 'singleplus.gif',
										 'beginminusRTL.gif', 'beginplusRTL.gif', 'lastminusRTL.gif', 'lastplusRTL.gif', 'minusRTL.gif', 'plusRTL.gif', 'singleminusRTL.gif', 'singleplusRTL.gif');
	// Unfold the branch if it isn't visible

	if (NextNonTextSibling(node).style.display == 'none')
	{
		// Change the image (if there is an image)
		if (node.childNodes.length > 0)
		{
			if (node.childNodes.item(0).nodeName == "IMG")
			{
				var str = node.childNodes.item(0).src;
				var pos = str.indexOf(Images[1 + imgOffset]); // beginplus.gif
				if (pos >= 0)
				{
					node.childNodes.item(0).src = path + Images[0 + imgOffset]; // "beginminus.gif";
				}
				else
				{
					pos = str.indexOf(Images[7 + imgOffset]); // "singleplus.gif");
					if (pos >= 0)
					{
						node.childNodes.item(0).src = path + Images[6 + imgOffset]; // "singleminus.gif";
					}
					else
					{
						pos = str.indexOf(Images[3 + imgOffset]); // "lastplus.gif");
						if (pos >= 0)
						{
							node.childNodes.item(0).src = path + Images[2 + imgOffset]; // "lastminus.gif";
						}
						else
						{
							node.childNodes.item(0).src = path + Images[4 + imgOffset]; // "minus.gif";
						}
					}
				}
			}
		}
		NextNonTextSibling(node).style.display = 'block';
		CenterNodeInBrowser(node);
	}
	// Collapse the branch if it IS visible
	else
	{
		// Change the image (if there is an image)
		if (node.childNodes.length > 0)
		{
			if (node.childNodes.item(0).nodeName == "IMG")
				var str = node.childNodes.item(0).src;
				var pos = str.indexOf(Images[0 + imgOffset]); // "beginminus.gif");
				if (pos >= 0)
				{
					node.childNodes.item(0).src = path + Images[1 + imgOffset]; // "beginplus.gif";
				}
				else
				{
					pos = str.indexOf(Images[6 + imgOffset]); // "singleminus.gif");
					if (pos >= 0)
					{
						node.childNodes.item(0).src = path + Images[7 + imgOffset]; // "singleplus.gif";
					}
					else
					{
						pos = str.indexOf(Images[2 + imgOffset]); // "lastminus.gif");
						if (pos >= 0)
						{
							node.childNodes.item(0).src = path + Images[3 + imgOffset]; // "lastplus.gif";
						}
						else
						{
							node.childNodes.item(0).src = path + Images[5 + imgOffset]; // "plus.gif";
						}
					}
				}
	}
	NextNonTextSibling(node).style.display = 'none';
}
}
			</xsl:text>
		</script>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		ShowAnyFailure
		Show any test failure message; reword message as appropriate
		Parameters: none
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="ShowAnyFailure">
		<xsl:variable name="moreToShow">
			<xsl:call-template name="DetermineIfMoreToShow">
				<xsl:with-param name="traceRoot" select="."/>
			</xsl:call-template>
		</xsl:variable>
		<xsl:choose>
			<xsl:when test="name() = 'MorphologicalRuleSynthesisTrace' and not(FailureReason) and $moreToShow != 'Y'">
				<span style="unicode-bidi:embed">
					<xsl:attribute name="style">
						<xsl:text>color:</xsl:text>
						<xsl:value-of select="$sFailureColor"/>
						<xsl:text>; font-size:smaller</xsl:text>
					</xsl:attribute>
					<xsl:text>&#xa0;&#xa0;(Reason: This is a duplicate parse and has been pruned.)</xsl:text>
				</span>
			</xsl:when>
			<xsl:otherwise>
				<xsl:for-each select="FailureReason">
					<span style="unicode-bidi:embed">
						<xsl:attribute name="style">
							<xsl:text>color:</xsl:text>
							<xsl:value-of select="$sFailureColor"/>
							<xsl:text>; font-size:smaller</xsl:text>
						</xsl:attribute>
						<xsl:text>&#xa0;&#xa0;(Reason: </xsl:text>
						<xsl:choose>
							<xsl:when test="@type = 'adhocProhibitionRule'">
								<xsl:text>Ad-hoc prohibition rule failed.  The </xsl:text>
								<xsl:value-of select="translate(RuleType,'AM','am')"/>
								<xsl:text>, </xsl:text>
								<xsl:if test="Allomorph">
									<span>
										<xsl:attribute name="style">
											<xsl:text>cursor:pointer</xsl:text>
											<xsl:call-template name="GetVernacularFont"/>
										</xsl:attribute>
										<xsl:attribute name="onclick">
											<xsl:text>JumpToToolBasedOnHvo(</xsl:text>
											<xsl:value-of select="Allomorph/@id"/>
											<xsl:text>)</xsl:text>
										</xsl:attribute>
										<xsl:attribute name="onmousemove">
											<xsl:text>MouseMove()</xsl:text>
										</xsl:attribute>
										<xsl:value-of select="Allomorph/LongName"/>
									</span>
								</xsl:if>
								<xsl:if test="Morpheme">
									<span style="cursor:pointer;">
										<xsl:attribute name="onclick">
											<xsl:text>JumpToToolBasedOnHvo(</xsl:text>
											<xsl:value-of select="KeyMorpheme/@id"/>
											<xsl:text>)</xsl:text>
										</xsl:attribute>
										<xsl:attribute name="onmousemove">
											<xsl:text>MouseMove()</xsl:text>
										</xsl:attribute>
										<xsl:value-of select="Morpheme/Gloss"/>
									</span>
								</xsl:if>
								<xsl:text>, cannot occur </xsl:text>
								<xsl:choose>
									<xsl:when test="Adjacency='AdjacentToRight'">
										<xsl:text>adjacent before</xsl:text>
									</xsl:when>
									<xsl:when test="Adjacency='AdjacentToLeft'">
										<xsl:text>adjacent after</xsl:text>
									</xsl:when>
									<xsl:when test="Adjacency='SomewhereToRight'">
										<xsl:text>somewhere before</xsl:text>
									</xsl:when>
									<xsl:when test="Adjacency='SomewhereToLeft'">
										<xsl:text>somewhere after</xsl:text>
									</xsl:when>
									<xsl:otherwise>
										<xsl:text>anywhere around</xsl:text>
									</xsl:otherwise>
								</xsl:choose>
								<xsl:variable name="others" select="Others/Morpheme | Others/Allomorph"/>
								<xsl:choose>
									<xsl:when test="count($others) &gt; 1">
										<xsl:text> these items: </xsl:text>
									</xsl:when>
									<xsl:otherwise>
										<xsl:text> this item: </xsl:text>
									</xsl:otherwise>
								</xsl:choose>
								<xsl:for-each select="Others/Morpheme">
									<span style="cursor:pointer;">
										<xsl:attribute name="onclick">
											<xsl:text>JumpToToolBasedOnHvo(</xsl:text>
											<xsl:value-of select="@id"/>
											<xsl:text>)</xsl:text>
										</xsl:attribute>
										<xsl:attribute name="onmousemove">
											<xsl:text>MouseMove()</xsl:text>
										</xsl:attribute>
										<xsl:value-of select="Gloss"/>
									</span>
									<xsl:if test="count(following-sibling::*)!=0">
										<xsl:text>, </xsl:text>
									</xsl:if>
								</xsl:for-each>
								<xsl:for-each select="Others/Allomorph">
									<span>
										<xsl:attribute name="style">
											<xsl:text>cursor:pointer</xsl:text>
											<xsl:call-template name="GetVernacularFont"/>
										</xsl:attribute>
										<xsl:attribute name="onclick">
											<xsl:text>JumpToToolBasedOnHvo(</xsl:text>
											<xsl:value-of select="@id"/>
											<xsl:text>)</xsl:text>
										</xsl:attribute>
										<xsl:attribute name="onmousemove">
											<xsl:text>MouseMove()</xsl:text>
										</xsl:attribute>
										<xsl:value-of select="LongName"/>
									</span>
									<xsl:if test="count(following-sibling::*)!=0">
										<xsl:text>, </xsl:text>
									</xsl:if>
								</xsl:for-each>
								<xsl:text>.</xsl:text>
							</xsl:when>
							<xsl:when test="@type = 'formMismatch'">
								<xsl:text>The synthesized surface form does not match the input word.</xsl:text>
							</xsl:when>
							<xsl:when test="@type = 'boundStem'">
								<xsl:text>A bound stem or root was found completely by itself. These must have at least one other morpheme present.</xsl:text>
							</xsl:when>
							<xsl:when test="@type = 'disjunctiveAllomorph'">
								<xsl:text>The valid parse '</xsl:text>
								<span>
									<xsl:attribute name="style">
										<xsl:call-template name="GetVernacularFont"/>
									</xsl:attribute>
									<xsl:value-of select="Word"/>
								</span>
								<xsl:text>' takes precedence over this parse.</xsl:text>
							</xsl:when>
							<xsl:when test="@type = 'affixInflFeats'">
								<xsl:text>The parse's inflection features '</xsl:text>
								<xsl:value-of select="InflFeatures"/>
								<xsl:text>' conflict with the following features required by the allomorph '</xsl:text>
								<span>
									<xsl:attribute name="style">
										<xsl:text>cursor:pointer</xsl:text>
										<xsl:call-template name="GetVernacularFont"/>
									</xsl:attribute>
									<xsl:attribute name="onclick">
										<xsl:text>JumpToToolBasedOnHvo(</xsl:text>
										<xsl:value-of select="Allomorph/@id"/>
										<xsl:text>)</xsl:text>
									</xsl:attribute>
									<xsl:attribute name="onmousemove">
										<xsl:text>MouseMove()</xsl:text>
									</xsl:attribute>
									<xsl:value-of select="Allomorph/LongName"/>
								</span>
								<xsl:text>': </xsl:text>
								<xsl:value-of select="RequiredInflFeatures"/>
								<xsl:text>.</xsl:text>
							</xsl:when>
							<xsl:when test="@type = 'stemName'">
								<xsl:text>The allomorph '</xsl:text>
								<span>
									<xsl:attribute name="style">
										<xsl:text>cursor:pointer</xsl:text>
										<xsl:call-template name="GetVernacularFont"/>
									</xsl:attribute>
									<xsl:attribute name="onclick">
										<xsl:text>JumpToToolBasedOnHvo(</xsl:text>
										<xsl:value-of select="Allomorph/@id"/>
										<xsl:text>)</xsl:text>
									</xsl:attribute>
									<xsl:attribute name="onmousemove">
										<xsl:text>MouseMove()</xsl:text>
									</xsl:attribute>
									<xsl:value-of select="Allomorph/LongName"/>
								</span>
								<xsl:text>' has a stem name of '</xsl:text>
								<xsl:value-of select="StemName"/>
								<xsl:text>', therefore it requires some inflectional affixes with inflection features for that stem name, but there aren't any such inflectional affixes.</xsl:text>
							</xsl:when>
							<xsl:when test="@type = 'environment'">
								<xsl:choose>
									<xsl:when test="Allomorph">
										<xsl:text>Environment incorrect for allomorph '</xsl:text>
										<span>
											<xsl:attribute name="style">
												<xsl:text>cursor:pointer</xsl:text>
												<xsl:call-template name="GetVernacularFont"/>
											</xsl:attribute>
											<xsl:attribute name="onclick">
												<xsl:text>JumpToToolBasedOnHvo(</xsl:text>
												<xsl:value-of select="Allomorph/@id"/>
												<xsl:text>)</xsl:text>
											</xsl:attribute>
											<xsl:attribute name="onmousemove">
												<xsl:text>MouseMove()</xsl:text>
											</xsl:attribute>
											<xsl:value-of select="Allomorph/LongName"/>
										</span>
										<xsl:text>': </xsl:text>
									</xsl:when>
									<xsl:otherwise>
										<xsl:text>Environment incorrect: </xsl:text>
									</xsl:otherwise>
								</xsl:choose>
								<xsl:for-each select="Environment">
									<span>
										<xsl:attribute name="style">
											<xsl:call-template name="GetVernacularFont"/>
										</xsl:attribute>
										<xsl:value-of select="."/>
									</span>
									<xsl:if test="count(following-sibling::*)!=0">
										<xsl:text>, </xsl:text>
									</xsl:if>
								</xsl:for-each>
								<xsl:text>.</xsl:text>
							</xsl:when>
							<xsl:when test="@type = 'affixProcess'">
								<xsl:text>The synthesized form does not match the input side of this affix process rule.</xsl:text>
							</xsl:when>
							<xsl:when test="@type = 'inflFeats'">
								<xsl:text>The parse's inflection features '</xsl:text>
								<xsl:value-of select="InflFeatures"/>
								<xsl:text>' conflict with the following required features: </xsl:text>
								<xsl:value-of select="RequiredInflFeatures"/>
								<xsl:text>.</xsl:text>
							</xsl:when>
							<xsl:when test="@type = 'fromStemName'">
								<xsl:text>The stem/root does not have the stem name '</xsl:text>
								<xsl:value-of select="StemName"/>
								<xsl:text>'.</xsl:text>
							</xsl:when>
							<xsl:when test="@type = 'mprFeatures'">
								<xsl:call-template name="ShowMprFeatureFailure">
									<xsl:with-param name="reason" select="."/>
								</xsl:call-template>
							</xsl:when>
							<xsl:when test="@type = 'requiredInflType'">
								<xsl:text>This null affix can only attach to an irregulary inflected form.</xsl:text>
							</xsl:when>
							<xsl:when test="@type = 'excludedInflType'">
								<xsl:text>This affix cannot attach to an irregularly inflected form.</xsl:text>
							</xsl:when>
							<xsl:when test="@type = 'partialParse'">
								<xsl:text>This parse does not include all analyzed morphemes.</xsl:text>
							</xsl:when>
							<xsl:when test="@type = 'nonFinalTemplate'">
								<xsl:text>Further derivation is required after a non-final template.</xsl:text>
							</xsl:when>
						</xsl:choose>
						<xsl:text>)</xsl:text>
					</span>
				</xsl:for-each>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		ShowIcon
		Show the "tree" icon
		Parameters: none
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="ShowIcon">
		<xsl:variable name="bUsePlus">
			<xsl:call-template name="DetermineIfMoreToShow">
				<xsl:with-param name="traceRoot" select="."/>
			</xsl:call-template>
		</xsl:variable>
		<xsl:variable name="sIcon">
			<xsl:choose>
				<xsl:when test="$bUsePlus!='Y'">
					<xsl:choose>
						<xsl:when test="position()=1 and position()=last()">
							<xsl:text>singleminus</xsl:text>
						</xsl:when>
						<xsl:when test="position()=1">
							<xsl:text>beginminus</xsl:text>
						</xsl:when>
						<xsl:when test="position()=last()">
							<xsl:text>lastminus</xsl:text>
						</xsl:when>
						<xsl:otherwise>
							<xsl:text>minus</xsl:text>
						</xsl:otherwise>
					</xsl:choose>
					<xsl:if test="$prmVernacularRTL='Y'">
						<xsl:text>RTL</xsl:text>
					</xsl:if>
					<xsl:text>.gif</xsl:text>
				</xsl:when>
				<xsl:otherwise>
					<xsl:choose>
						<xsl:when test="position()=1 and position()=last()">
							<xsl:text>singleplus</xsl:text>
						</xsl:when>
						<xsl:when test="position()=1">
							<xsl:text>beginplus</xsl:text>
						</xsl:when>
						<xsl:when test="position()=last()">
							<xsl:text>lastplus</xsl:text>
						</xsl:when>
						<xsl:otherwise>
							<xsl:text>plus</xsl:text>
						</xsl:otherwise>
					</xsl:choose>
					<xsl:if test="$prmVernacularRTL='Y'">
						<xsl:text>RTL</xsl:text>
					</xsl:if>
					<xsl:text>.gif</xsl:text>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<img>
			<xsl:attribute name="src">
				<xsl:value-of select="$prmIconPath"/>
				<xsl:text>/</xsl:text>
				<xsl:value-of select="$sIcon"/>
			</xsl:attribute>
			<xsl:attribute name="onclick">
				<xsl:text>Toggle(this.parentNode, "</xsl:text>
				<xsl:value-of select="$prmIconPath"/>
				<xsl:text>",</xsl:text>
				<xsl:choose>
					<xsl:when test="$prmVernacularRTL='Y'">
						<xsl:text> 8)</xsl:text>
					</xsl:when>
					<xsl:otherwise>
						<xsl:text> 0)</xsl:text>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:attribute>
		</img>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ShowMorph
	Show the morpheme information
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="ShowMorph">
		<xsl:param name="curTemplate"/>
		<span class="interblock">
			<table cellpadding="0" cellspacing="0">
				<xsl:attribute name="style">
					<xsl:text>cursor:pointer;color:</xsl:text>
					<xsl:call-template name="GetSuccessOrFailureColor"/>
					<xsl:if test="$prmVernacularRTL='Y'">
						<xsl:text>; text-align:right;</xsl:text>
					</xsl:if>
				</xsl:attribute>
				<xsl:attribute name="onclick">
					<xsl:text>JumpToToolBasedOnHvo(</xsl:text>
					<xsl:value-of select="Allomorph/@id"/>
					<xsl:text>)</xsl:text>
				</xsl:attribute>
				<xsl:attribute name="onmousemove">
					<xsl:text>MouseMove()</xsl:text>
				</xsl:attribute>
				<xsl:if test="name()='WordSynthesisTrace'">
					<tr>
						<td>Have found an analysis; now working on synthesizing that analysis.</td>
					</tr>
				</xsl:if>
				<tr>
					<td>
						<xsl:attribute name="style">
							<xsl:if test="$prmVernacularRTL='Y'">
								<xsl:text>direction:rtl</xsl:text>
							</xsl:if>
							<xsl:call-template name="GetVernacularFont"/>
						</xsl:attribute>
						<xsl:choose>
							<xsl:when test="MorphologicalRule/@type = 'compound'">
							</xsl:when>
							<xsl:otherwise>
								<xsl:value-of select="Allomorph/Form"/>
							</xsl:otherwise>
						</xsl:choose>
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
						<!-- citationForm -->
						<xsl:choose>
							<xsl:when test="MorphologicalRule/@type = 'compound'">
								<xsl:text>Compound rule</xsl:text>
							</xsl:when>
							<xsl:otherwise>
								<xsl:value-of select="Allomorph/Morpheme/HeadWord"/>
							</xsl:otherwise>
						</xsl:choose>
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
						<xsl:choose>
							<xsl:when test="MorphologicalRule/@type = 'compound'">
								<xsl:value-of select="MorphologicalRule"/>
							</xsl:when>
							<xsl:otherwise>
								<xsl:value-of select="Allomorph/Morpheme/Gloss"/>
							</xsl:otherwise>
						</xsl:choose>
					</td>
				</tr>
				<tr>
					<td>
						<xsl:if test="$prmVernacularRTL='Y'">
							<xsl:attribute name="style">direction:ltr</xsl:attribute>
						</xsl:if>
						<xsl:choose>
							<xsl:when test="MorphologicalRule/@type = 'compound'">
								<!-- Compound rules are not morphemes -->
							</xsl:when>
							<xsl:otherwise>
								<xsl:call-template name="ShowMsaInfo">
									<xsl:with-param name="msa" select="Allomorph/Morpheme"/>
								</xsl:call-template>
							</xsl:otherwise>
						</xsl:choose>
					</td>
				</tr>
				<xsl:if test="name()='MorphologicalRuleAnalysisTrace' or name()='MorphologicalRuleSynthesisTrace'">
					<xsl:if test="$curTemplate">
						<tr>
							<td>
								<xsl:attribute name="style">
									<xsl:if test="$prmVernacularRTL='Y'">
										<xsl:text>direction:rtl</xsl:text>
									</xsl:if>
									<xsl:text>; font-size:smaller</xsl:text>
								</xsl:attribute>
								<xsl:text>Template = </xsl:text>
								<span>
									<xsl:attribute name="style">
										<xsl:call-template name="GetAnalysisFont"/>
									</xsl:attribute>
									<xsl:value-of select="$curTemplate"/>
								</span>
							</td>
						</tr>
					</xsl:if>
					<tr>
						<td>
							<xsl:attribute name="style">
								<xsl:if test="$prmVernacularRTL='Y'">
									<xsl:text>direction:rtl</xsl:text>
								</xsl:if>
								<xsl:text>; font-size:smaller</xsl:text>
							</xsl:attribute>
							<xsl:text>Output = </xsl:text>
							<span>
								<xsl:attribute name="style">
									<xsl:call-template name="GetVernacularFont"/>
								</xsl:attribute>
								<xsl:value-of select="Output"/>
							</span>
						</td>
					</tr>
				</xsl:if>
			</table>
		</span>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		ShowMorphSuccess
		Show the morpheme information
		Parameters: none
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="ShowMorphSuccess">
		<span class="interblock">
			<table cellpadding="0" cellspacing="0">
				<xsl:attribute name="style">
					<xsl:text>cursor:pointer;color:</xsl:text>
					<xsl:value-of select="$sSuccessColor"/>
					<xsl:if test="$prmVernacularRTL='Y'">
						<xsl:text>; text-align:right;</xsl:text>
					</xsl:if>
				</xsl:attribute>
				<xsl:attribute name="onclick">
					<xsl:text>JumpToToolBasedOnHvo(</xsl:text>
					<xsl:value-of select="@id"/>
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
						<xsl:value-of select="Form"/>
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
						<xsl:value-of select="Morpheme/HeadWord"/>
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
						<xsl:choose>
							<xsl:when test="string-length(Morpheme/Gloss) &gt; 0">
								<xsl:value-of select="Morpheme/Gloss"/>
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
							<xsl:with-param name="msa" select="Morpheme"/>
						</xsl:call-template>
					</td>
				</tr>
			</table>
		</span>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		ShowPhonologicalRules
		Show phonological rule applications
		Parameters: phonologicalRules - the rules to show
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="ShowPhonologicalRules">
		<xsl:param name="phonologicalRules"/>
		<table border="1">
			<tr>
				<th>Input</th>
				<td>
					<xsl:attribute name="style">
						<xsl:if test="$prmVernacularRTL='Y'">
							<xsl:text>direction:rtl</xsl:text>
						</xsl:if>
						<xsl:call-template name="GetVernacularFont"/>
					</xsl:attribute>
					<xsl:value-of select="$phonologicalRules[1]/Input"/>
				</td>
				<xsl:if test="$phonologicalRules/FailureReason">
					<th>Rule not applied because...</th>
				</xsl:if>
			</tr>
			<xsl:for-each select="$phonologicalRules">
				<tr>
					<th style="cursor:pointer;">
						<xsl:attribute name="onclick">
							<xsl:text>JumpToToolBasedOnHvo(</xsl:text>
							<xsl:value-of select="PhonologicalRule/@id"/>
							<xsl:text>)</xsl:text>
						</xsl:attribute>
						<xsl:attribute name="onmousemove">
							<xsl:text>MouseMove()</xsl:text>
						</xsl:attribute>
						<xsl:choose>
							<xsl:when test="string-length(normalize-space(PhonologicalRule)) &gt; 0">
								<xsl:choose>
									<xsl:when test="FailureReason">
										<span style="color:gray">
											<xsl:value-of select="normalize-space(PhonologicalRule)"/>
										</span>
									</xsl:when>
									<xsl:otherwise>
										<xsl:value-of select="normalize-space(PhonologicalRule)"/>
									</xsl:otherwise>
								</xsl:choose>
							</xsl:when>
							<xsl:otherwise>
								<xsl:text>Rule </xsl:text>
								<xsl:value-of select="count($phonologicalRules) - position() + 1"/>
							</xsl:otherwise>
						</xsl:choose>
					</th>
					<td>
						<xsl:attribute name="style">
							<xsl:if test="$prmVernacularRTL='Y'">
								<xsl:text>direction:rtl</xsl:text>
							</xsl:if>
							<xsl:call-template name="GetVernacularFont"/>
						</xsl:attribute>
						<xsl:value-of select="Output"/>
					</td>
					<xsl:if test="$phonologicalRules/FailureReason">
						<td>
							<xsl:choose>
								<xsl:when test="FailureReason/@type = 'category'">
									<xsl:text>The stem's category is </xsl:text>
									<xsl:value-of select="FailureReason/Category"/>
									<xsl:text>, but this rule only applies when the stem's category is </xsl:text>
									<xsl:for-each select="FailureReason/RequiredCategories/Category">
										<xsl:value-of select="."/>
										<xsl:call-template name="OutputListPunctuation">
											<xsl:with-param name="sConjunction" select="' or '"/>
											<xsl:with-param name="sFinalPunctuation" select="'.'"/>
										</xsl:call-template>
									</xsl:for-each>
								</xsl:when>
								<xsl:when test="FailureReason/@type = 'mprFeatures'">
									<xsl:call-template name="ShowMprFeatureFailure">
										<xsl:with-param name="reason" select="FailureReason"/>
									</xsl:call-template>
								</xsl:when>
							</xsl:choose>
						</td>
					</xsl:if>
				</tr>
			</xsl:for-each>
		</table>
	</xsl:template>

	<xsl:template name="ShowMprFeatureFailure">
		<xsl:param name="reason"/>
		<xsl:variable name="featureTypeStr">
			<xsl:choose>
				<xsl:when test="$reason/Group = 'inflClasses'">
					<xsl:text>inflection classes</xsl:text>
				</xsl:when>
				<xsl:when test="$reason/Group = 'exceptionFeatures'">
					<xsl:text>exception features</xsl:text>
				</xsl:when>
			</xsl:choose>
		</xsl:variable>
		<xsl:choose>
			<xsl:when test="$reason/MprFeatures/MprFeature">
				<xsl:text>The stem has the following </xsl:text>
				<xsl:value-of select="$featureTypeStr"/>
				<xsl:text>: </xsl:text>
				<xsl:for-each select="FailureReason/MprFeatures/MprFeature">
					<xsl:value-of select="."/>
					<xsl:call-template name="OutputListPunctuation">
						<xsl:with-param name="sConjunction" select="' and '"/>
						<xsl:with-param name="sFinalPunctuation" select="','"/>
					</xsl:call-template>
				</xsl:for-each>
			</xsl:when>
			<xsl:otherwise>
				<xsl:text>The stem does not have any </xsl:text>
				<xsl:value-of select="$featureTypeStr"/>
				<xsl:text>,</xsl:text>
			</xsl:otherwise>
		</xsl:choose>
		<xsl:choose>
			<xsl:when test="$reason/MatchType = 'required'">
				<xsl:text> but this rule only applies when the stem has the following </xsl:text>
			</xsl:when>
			<xsl:when test="$reason/MatchType = 'excluded'">
				<xsl:text> but this rule only applies when the stem has none of the following </xsl:text>
			</xsl:when>
		</xsl:choose>
		<xsl:value-of select="$featureTypeStr"/>
		<xsl:text>: </xsl:text>
		<xsl:for-each select="$reason/ConstrainingMprFeatrues/MprFeature">
			<xsl:value-of select="."/>
			<xsl:call-template name="OutputListPunctuation">
				<xsl:with-param name="sConjunction">
					<xsl:choose>
						<xsl:when test="$reason/MatchType = 'required'">
							<xsl:text> and </xsl:text>
						</xsl:when>
						<xsl:when test="$reason/MatchType = 'excluded'">
							<xsl:text> or </xsl:text>
						</xsl:when>
					</xsl:choose>
				</xsl:with-param>
				<xsl:with-param name="sFinalPunctuation" select="'.'"/>
			</xsl:call-template>
		</xsl:for-each>
	</xsl:template>

	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		ShowSuccessfulAnalyses
		Show all analyses that succeeded
		Parameters: none
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="ShowSuccessfulAnalyses">
		<table border="0">
			<xsl:for-each select="/Wordform/Analysis">
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
							<td valign="top">
								<xsl:call-template name="ShowMorphSuccess"/>
							</td>
						</xsl:for-each>
					</tr>
				</table>
			</td>
		</tr>
	</xsl:template>

	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		ShowTracePath
		Recursively show the paths that the trace took
		Parameters: parseNodes - current set of parseNodes at this level
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="ShowTracePath">
		<xsl:param name="traceRoot"/>
		<xsl:param name="curTemplate"/>

		<xsl:variable name="analysisAffixes" select="$traceRoot/MorphologicalRuleAnalysisTrace[MorphologicalRule/@type = 'affix']"/>
		<xsl:variable name="synthesisAffixes" select="$traceRoot/MorphologicalRuleSynthesisTrace[MorphologicalRule/@type = 'affix']"/>
		<xsl:variable name="analysisCompoundRules" select="$traceRoot/MorphologicalRuleAnalysisTrace[MorphologicalRule/@type = 'compound']"/>
		<xsl:variable name="synthesisCompoundRules" select="$traceRoot/MorphologicalRuleSynthesisTrace[MorphologicalRule/@type = 'compound']"/>
		<xsl:variable name="synthesizedWords" select="$traceRoot/LexLookupTrace/WordSynthesisTrace"/>
		<xsl:variable name="parseCompleteTraces" select="$traceRoot/ParseCompleteTrace"/>

		<xsl:variable name="parseNodes" select="$analysisAffixes | $synthesisAffixes | $analysisCompoundRules | $synthesisCompoundRules | $synthesizedWords | $parseCompleteTraces"/>
		<xsl:if test="$parseNodes">
			<xsl:for-each select="$parseNodes">
				<xsl:variable name="lastTemplateTrace" select="(preceding-sibling::*[name()='TemplateAnalysisTraceIn' or name()='TemplateSynthesisTraceIn' or name()='TemplateAnalysisTraceOut' or name()='TemplateSynthesisTraceOut'])[position() = last()]"/>
				<xsl:variable name="template">
					<xsl:choose>
						<xsl:when test="name($lastTemplateTrace) = 'TemplateAnalysisTraceIn' or name($lastTemplateTrace) = 'TemplateSynthesisTraceIn'">
							<xsl:value-of select="$lastTemplateTrace/AffixTemplate"/>
						</xsl:when>
						<xsl:when test="name($lastTemplateTrace) = 'TemplateAnalysisTraceOut' or name($lastTemplateTrace) = 'TemplateSynthesisTraceOut'">
							<xsl:text></xsl:text>
						</xsl:when>
						<xsl:otherwise>
							<xsl:value-of select="$curTemplate"/>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:variable>
				 <table border="0">
					<tr>
						<td width="10"/>
						<td style="border:solid; border-width:thin;">
							<a>
								<xsl:call-template name="ShowIcon"/>
								<table cellpadding="0pt" cellspacing="0pt">
									<xsl:choose>
										<xsl:when test="name() = 'ParseCompleteTrace'">
											<xsl:if test="../PhonologicalRuleSynthesisTrace">
												<tr>
													<td valign="top" colspan="2">
														<xsl:attribute name="style">
															<xsl:text>color:</xsl:text>
															<xsl:call-template name="GetSuccessOrFailureColor"/>
														</xsl:attribute>
														<xsl:text>Phonological rules applied:</xsl:text>
														<xsl:call-template name="ShowPhonologicalRules">
															<xsl:with-param name="phonologicalRules" select="../PhonologicalRuleSynthesisTrace"/>
														</xsl:call-template>
													</td>
												</tr>
											</xsl:if>
											<tr>
												<td valign="top" colspan="2">
													<xsl:attribute name="style">
														<xsl:text>color:</xsl:text>
														<xsl:call-template name="GetSuccessOrFailureColor"/>
													</xsl:attribute>
													<xsl:text>Parse completed.</xsl:text>
												</td>
											</tr>
											<tr>
												<td valign="top">
													<span class="interblock">
														<table cellpadding="0" cellspacing="0">
															<xsl:attribute name="style">
																<xsl:text>color:</xsl:text>
																<xsl:call-template name="GetSuccessOrFailureColor"/>
																<xsl:if test="$prmVernacularRTL='Y'">
																	<xsl:text>; text-align:right;</xsl:text>
																</xsl:if>
															</xsl:attribute>
															<tr>
																<td>
																	<xsl:attribute name="style">
																		<xsl:if test="$prmVernacularRTL='Y'">
																			<xsl:text>direction:rtl</xsl:text>
																		</xsl:if>
																		<xsl:text>; font-size:smaller</xsl:text>
																	</xsl:attribute>
																	<xsl:text>Result = </xsl:text>
																	<span>
																		<xsl:attribute name="style">
																			<xsl:call-template name="GetVernacularFont"/>
																		</xsl:attribute>
																		<xsl:value-of select="Result"/>
																	</span>
																</td>
															</tr>
															<xsl:if test="@success = 'true'">
																<tr>
																	<td>
																		<xsl:attribute name="style">
																			<xsl:if test="$prmVernacularRTL='Y'">
																				<xsl:text>direction:ltr</xsl:text>
																			</xsl:if>
																			<xsl:text>; font-size:smaller</xsl:text>
																		</xsl:attribute>
																		<xsl:choose>
																			<xsl:when test="$prmVernacularRTL='Y'">
																				<!-- NB: no exclamation mark when right-to-left because it comes out at the wrong spot -->
																				<xsl:text>(Parse succeeded)&#xa0;&#xa0;</xsl:text>
																			</xsl:when>
																			<xsl:otherwise>
																				<xsl:text>&#xa0;&#xa0;(Parse succeeded!)</xsl:text>
																			</xsl:otherwise>
																		</xsl:choose>
																	</td>
																</tr>
															</xsl:if>
														</table>
													</span>
												</td>
												<td valign="top">
													<xsl:call-template name="ShowAnyFailure"/>
												</td>
											</tr>
										</xsl:when>
										<xsl:otherwise>
											<tr>
												<td valign="top">
													<xsl:call-template name="ShowMorph">
														<xsl:with-param name="curTemplate" select="normalize-space($template)"/>
													</xsl:call-template>
												</td>
												<td valign="top">
													<xsl:call-template name="ShowAnyFailure"/>
												</td>
											</tr>
										</xsl:otherwise>
									</xsl:choose>
								</table>
							</a>
							<div>
								<xsl:attribute name="style">
									<xsl:choose>
										<xsl:when test="$selectedMorphs">
											<xsl:text>display:block</xsl:text>
										</xsl:when>
										<xsl:otherwise>
											<xsl:text>display:none</xsl:text>
										</xsl:otherwise>
									</xsl:choose>
								</xsl:attribute>
								<xsl:choose>
									<xsl:when test="name()='WordSynthesisTrace'">
										<xsl:call-template name="ShowTracePath">
											<xsl:with-param name="traceRoot" select="."/>
										</xsl:call-template>
									</xsl:when>
									<xsl:when test="name() = 'ParseCompleteTrace'">
										<!-- The parse is complete, so there are no more records. -->
									</xsl:when>
									<xsl:otherwise>
										<xsl:call-template name="ShowTracePath">
											<xsl:with-param name="traceRoot" select="."/>
											<xsl:with-param name="curTemplate" select="$template"/>
										</xsl:call-template>
									</xsl:otherwise>
								</xsl:choose>
							</div>
						</td>
					</tr>
				</table>
			</xsl:for-each>
		</xsl:if>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		TraceSection
		Output the trace section
		Parameters: none
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="TraceSection">
		<xsl:call-template name="UseShowDetailsButton"/>
		<div id="TraceSection" style="display:block">
			<h2>Parsing Details</h2>
			<p>
				<xsl:text>In the following, any item shown in </xsl:text>
				<span>
					<xsl:attribute name="style">
						<xsl:text>color:</xsl:text>
						<xsl:value-of select="$sSuccessColor"/>
					</xsl:attribute>
					<xsl:text>green</xsl:text>
				</span>
				<xsl:text> has a path to a successful parse.  Any item shown in </xsl:text>
				<span>
					<xsl:attribute name="style">
						<xsl:text>color:</xsl:text>
						<xsl:value-of select="$sFailureColor"/>
					</xsl:attribute>
					<xsl:text>red</xsl:text>
				</span>
				<xsl:text> does not have a path to a successful parse (i.e. this path failed to produce a successful parse).  The reason for the failure is shown at the end of a line as </xsl:text>
				<span>
					<xsl:attribute name="style">
						<xsl:text>color:</xsl:text>
						<xsl:value-of select="$sFailureColor"/>
						<xsl:text>; font-size:smaller</xsl:text>
					</xsl:attribute>
					<xsl:text>(Reason: XXX)</xsl:text>
				</span>
				<xsl:text>, where XXX is the reason.  Sometimes you have to follow a path to find a reason. </xsl:text>
			</p>
			<p>
				<xsl:text>This particular parser works as follows:</xsl:text>
				<ol>
					<li>It "unapplies" any phonological rules.
						That is, it tries the phonological rules in reverse order.
						At such each step, the information below shows you a pattern of the possible phonemes that the word may have started with.
						This pattern is something like the regular expression patterns you can define in Filters.</li>
					<li>It sees if the entire word form matches a root or stem.</li>
					<li>It tries to peel off affixes from either the front or the back of the word.</li>
					<li>It tries to apply any compound rules.</li>
					<li>It tries to look the root or stem up in the lexicon.</li>
					<li>When it has found a possible analysis, it then builds the word in a generative fashion, from the root out.</li>
					<li>Then it applies any phonological rules in their generative order.</li>
					<li>If the result matches the original word form, then the parse is declared to be successful.</li>
				</ol>
			</p>
			<p>Click on the box by a morpheme to follow a path.  When the mouse icon turns to the hand symbol, you can click to bring up that item in Language Explorer.</p>
			<xsl:if test="$selectedMorphs">
				<div>
					<p>NOTE: The information shown below is limited to the morphemes you selected above.</p>
				</div>
			</xsl:if>
			<div>
				<xsl:if test="$prmVernacularRTL='Y'">
					<xsl:attribute name="style">direction:rtl; text-align:right</xsl:attribute>
				</xsl:if>
				<xsl:if test="$analysisPhonologicalRules">
					<div>
						<p>Phonological rules 'unapplied' (i.e. in reverse order):</p>
						<xsl:call-template name="ShowPhonologicalRules">
							<xsl:with-param name="phonologicalRules" select="$analysisPhonologicalRules"/>
						</xsl:call-template>
					</div>
				</xsl:if>
				<table>
					<tr>
						<td>
							<xsl:call-template name="ShowTracePath">
								<xsl:with-param name="traceRoot" select="/Wordform/Trace/WordAnalysisTrace"/>
							</xsl:call-template>
						</td>
					</tr>
				</table>
			</div>
		</div>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		UseShowDetailsButton
		Output the "Show Detals" button
		Parameters: none
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="UseShowDetailsButton">
		<p style="margin-left:.5in">
			<input type="button" value="Hide Details" name="BDetails" id="ShowDetailsButton" onclick="ButtonShowDetails()" style="width: 88px; height: 24px"/>
		</p>
	</xsl:template>
</xsl:stylesheet>
