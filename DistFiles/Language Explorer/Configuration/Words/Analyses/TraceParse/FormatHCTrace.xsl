<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="html" version="4.0" encoding="UTF-8" indent="yes" media-type="text/html; charset=utf-8"/>
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
	<xsl:param name="prmHCTraceNoOutput" select="'*None*'"/>
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
						<xsl:value-of select="translate(@Form,'.',' ')"/>
					</span>
					<xsl:text>.</xsl:text>
				</h1>
				<xsl:call-template name="ResultSection"/>
				<xsl:call-template name="TraceSection"/>
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
		<xsl:variable name="analysisAffixes" select="$traceRoot/MorphologicalRuleAnalysisTrace[Output!=$prmHCTraceNoOutput and starts-with(MorphologicalRule/@id, 'mrule')]"/>
		<xsl:variable name="synthesisAffixes" select="$traceRoot/MorphologicalRuleSynthesisTrace[Output!=$prmHCTraceNoOutput and starts-with(MorphologicalRule/@id, 'mrule')]"/>
		<xsl:variable name="analysisCompoundRules" select="$traceRoot/MorphologicalRuleAnalysisTrace[Output!=$prmHCTraceNoOutput and starts-with(MorphologicalRule/@id, 'comp')]"/>
		<xsl:variable name="synthesisCompoundRules" select="$traceRoot/MorphologicalRuleSynthesisTrace[Output!=$prmHCTraceNoOutput and starts-with(MorphologicalRule/@id, 'comp')]"/>
		<xsl:variable name="synthesizedWords" select="$traceRoot/LexLookupTrace/WordSynthesisTrace"/>
		<xsl:variable name="phonologicalRules" select="$traceRoot[name()='PhonologicalRuleSynthesisTrace']"/>
		<xsl:variable name="parseNodes" select="$analysisAffixes | $synthesisAffixes | $analysisCompoundRules | $synthesisCompoundRules | $synthesizedWords | $phonologicalRules"/>
		<xsl:if test="$parseNodes">
			<xsl:text>Y</xsl:text>
		</xsl:if>
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
		<xsl:variable name="bSuccess">
			<xsl:for-each select="descendant::ReportSuccessTrace">
				<xsl:variable name="reportSuccessID" select="Result/@id"/>
				<xsl:if test="$reportSuccessID">
					<xsl:variable name="wordGrammarSuccess" select="/Wordform/WordGrammarTrace/WordGrammarAttempt[Id=$reportSuccessID]"/>
					<xsl:if test="$wordGrammarSuccess/@success='true'">
						<xsl:text>Y</xsl:text>
					</xsl:if>
				</xsl:if>
			</xsl:for-each>
		</xsl:variable>
		<xsl:choose>
			<xsl:when test="string-length($bSuccess) != 0">
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
			<xsl:when test="/Wordform/WfiAnalysis/Morphs">
				<p>
					<xsl:text>This word parsed successfully.  The following are the sequences of allomorphs that succeeded:</xsl:text>
				</p>
				<div>
					<xsl:if test="$prmVernacularRTL='Y'">
						<xsl:attribute name="style">direction:rtl; text-align:right</xsl:attribute>
					</xsl:if>
					<xsl:call-template name="ShowSuccessfulAnalyses"/>
				</div>
				<xsl:call-template name="UseShowDetailsButton"/>
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
				<xsl:call-template name="UseShowDetailsButton"/>
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
		<!-- HC trace currently does not report any failures, but we can show word grammar failures -->
		<xsl:variable name="reportSuccessID">
			<xsl:choose>
				<xsl:when test="name()='PhonologicalRuleSynthesisTrace'">
					<xsl:value-of select="following-sibling::ReportSuccessTrace[1]/Result/@id"/>
				</xsl:when>
				<xsl:when test="name()='MorphologicalRuleSynthesisTrace'">
					<xsl:value-of select="ReportSuccessTrace[1]/Result/@id"/>
				</xsl:when>
			</xsl:choose>
		</xsl:variable>
		<xsl:if test="$reportSuccessID">
			<xsl:variable name="wordGrammarSuccess" select="/Wordform/WordGrammarTrace/WordGrammarAttempt[Id=$reportSuccessID]"/>
			<xsl:variable name="reportFailure">
				<xsl:if test="$wordGrammarSuccess/@success='false'">
					<xsl:choose>
						<xsl:when test="$analysisPhonologicalRules">
							<xsl:if test="name()='PhonologicalRuleSynthesisTrace'">
								<xsl:text>Y</xsl:text>
							</xsl:if>
						</xsl:when>
						<xsl:otherwise>
							<xsl:text>Y</xsl:text>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:if>
			</xsl:variable>
			<xsl:if test="$reportFailure='Y'">
				<span style="unicode-bidi:embed">
					<xsl:attribute name="style">
						<xsl:text>color:</xsl:text>
						<xsl:value-of select="$sFailureColor"/>
						<xsl:text>; font-size:smaller</xsl:text>
					</xsl:attribute>
					<xsl:text>&#xa0;&#xa0;(Reason: </xsl:text>
					<xsl:text>Word grammar failed&#xa0;&#xa0;</xsl:text>
					<input type="button" value="Tell me more" name="BWGDetails" id="ShowWGDetailsButton" style="width: 100px; height: 26px">
						<xsl:attribute name="onclick">
							<xsl:text>ButtonShowWGDetails(</xsl:text>
							<xsl:choose>
								<xsl:when test="$wordGrammarSuccess/Id">"<xsl:value-of select="$wordGrammarSuccess/Id"/>"</xsl:when>
								<xsl:otherwise>
									<xsl:text>0</xsl:text>
								</xsl:otherwise>
							</xsl:choose>
							<xsl:text>)</xsl:text>
						</xsl:attribute>
					</input>
					<xsl:text>&#xa0;&#xa0;</xsl:text>
					<xsl:text>)</xsl:text>
				</span>
			</xsl:if>
		</xsl:if>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		ShowAnySuccess
		Show a success message if this is at end of word and a success
		Parameters: none
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="ShowAnySuccess">
		<xsl:variable name="reportSuccess">
			<xsl:variable name="reportSuccessID">
				<xsl:choose>
					<xsl:when test="name()='PhonologicalRuleSynthesisTrace'">
						<xsl:value-of select="following-sibling::ReportSuccessTrace[1]/Result/@id"/>
					</xsl:when>
					<xsl:when test="name()='MorphologicalRuleSynthesisTrace'">
						<xsl:value-of select="ReportSuccessTrace[1]/Result/@id"/>
					</xsl:when>
				</xsl:choose>
			</xsl:variable>
			<xsl:if test="$reportSuccessID">
				<xsl:variable name="wordGrammarSuccess" select="/Wordform/WordGrammarTrace/WordGrammarAttempt[Id=$reportSuccessID]"/>
				<xsl:if test="$wordGrammarSuccess/@success='true'">
					<xsl:choose>
						<xsl:when test="$analysisPhonologicalRules">
							<xsl:if test="name()='PhonologicalRuleSynthesisTrace'">
								<xsl:text>Y</xsl:text>
							</xsl:if>
						</xsl:when>
						<xsl:otherwise>
							<xsl:text>Y</xsl:text>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:if>
			</xsl:if>
		</xsl:variable>
		<xsl:if test="$reportSuccess='Y'">
			<span>
				<xsl:attribute name="style">
					<xsl:text>color:</xsl:text>
					<xsl:value-of select="$sSuccessColor"/>
					<xsl:text>; font-size:smaller;</xsl:text>
					<xsl:if test="$prmVernacularRTL='Y'">
						<xsl:text>direction:ltr</xsl:text>
					</xsl:if>
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
			</span>
		</xsl:if>
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
			<xsl:choose>
				<xsl:when test="name()='WordSynthesisTrace'">
					<xsl:call-template name="DetermineIfMoreToShow">
						<xsl:with-param name="traceRoot" select="."/>
					</xsl:call-template>
				</xsl:when>
				<xsl:when test="name()='MorphologicalRuleSynthesisTrace' and PhonologicalRuleSynthesisTrace">
					<xsl:call-template name="DetermineIfMoreToShow">
						<xsl:with-param name="traceRoot" select="PhonologicalRuleSynthesisTrace[1]"/>
					</xsl:call-template>
				</xsl:when>
				<xsl:when test="name()='PhonologicalRuleSynthesisTrace'">
					<!-- do nothing; we're done -->
				</xsl:when>
				<xsl:otherwise>
					<xsl:call-template name="DetermineIfMoreToShow">
						<xsl:with-param name="traceRoot" select="."/>
					</xsl:call-template>
				</xsl:otherwise>
			</xsl:choose>
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
					<xsl:text>color:</xsl:text>
					<xsl:call-template name="GetSuccessOrFailureColor"/>
					<xsl:if test="$prmVernacularRTL='Y'">
						<xsl:text>; text-align:right;</xsl:text>
					</xsl:if>
				</xsl:attribute>
				<xsl:attribute name="onclick">
					<xsl:text>JumpToToolBasedOnHvo(</xsl:text>
					<xsl:choose>
						<xsl:when test="name()='WordSynthesisTrace'">
							<xsl:value-of select="RootAllomorph/Morph/MoForm/@DbRef"/>
						</xsl:when>
						<xsl:when test="(name()='MorphologicalRuleAnalysisTrace' or name()='MorphologicalRuleSynthesisTrace') and starts-with(MorphologicalRule/@id, 'comp')">
							<xsl:choose>
								<xsl:when test="@id='compRight' or @id='compLeft'">
									<!--  this is a default rule, so there is nothing in the database that corresponds to it -->
									<xsl:text>0</xsl:text>
								</xsl:when>
								<xsl:when test="contains(MorphologicalRule/@id,'_')">
									<xsl:value-of select="substring-before(substring-after(MorphologicalRule/@id, 'comp'),'_')" />
								</xsl:when>
								<xsl:otherwise>
									<xsl:value-of select="substring-after(MorphologicalRule/@id, 'comp')" />
								</xsl:otherwise>
							</xsl:choose>
						</xsl:when>
						<xsl:otherwise>
							<xsl:value-of select="RuleAllomorph/Morph/MoForm/@DbRef"/>
						</xsl:otherwise>
					</xsl:choose>
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
							<xsl:when test="name()='WordSynthesisTrace'">
								<xsl:value-of select="RootAllomorph/Morph/alloform"/>
							</xsl:when>
							<xsl:when test="(name()='MorphologicalRuleAnalysisTrace' or name()='MorphologicalRuleSynthesisTrace') and starts-with(MorphologicalRule/@id, 'comp')">
							</xsl:when>
							<xsl:otherwise>
								<xsl:choose>
									<xsl:when test="count(RuleAllomorph/Morph/MoForm) > 1">
										<xsl:value-of select="RuleAllomorph/Morph/citationForm"/>
									</xsl:when>
									<xsl:otherwise>
										<xsl:value-of select="RuleAllomorph/Morph/alloform"/>
									</xsl:otherwise>
								</xsl:choose>
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
							<xsl:when test="name()='WordSynthesisTrace'">
								<xsl:value-of select="RootAllomorph/Morph/citationForm"/>
							</xsl:when>
							<xsl:when test="(name()='MorphologicalRuleAnalysisTrace' or name()='MorphologicalRuleSynthesisTrace') and starts-with(MorphologicalRule/@id, 'comp')">
								<xsl:text>Compound rule</xsl:text>
							</xsl:when>
							<xsl:otherwise>
								<xsl:value-of select="RuleAllomorph/Morph/citationForm"/>
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
							<xsl:when test="name()='WordSynthesisTrace'">
								<xsl:value-of select="RootAllomorph/Morph/gloss"/>
							</xsl:when>
							<xsl:when test="(name()='MorphologicalRuleAnalysisTrace' or name()='MorphologicalRuleSynthesisTrace') and starts-with(MorphologicalRule/@id, 'comp')">
								<xsl:value-of select="MorphologicalRule/Description"/>
							</xsl:when>
							<xsl:otherwise>
								<xsl:value-of select="RuleAllomorph/Morph/gloss"/>
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
							<xsl:when test="name()='WordSynthesisTrace'">
								<xsl:call-template name="ShowMsaInfo">
									<xsl:with-param name="morph" select="RootAllomorph/Morph/MSI"/>
								</xsl:call-template>
							</xsl:when>
							<xsl:when test="(name()='MorphologicalRuleAnalysisTrace' or name()='MorphologicalRuleSynthesisTrace') and starts-with(MorphologicalRule/@id, 'comp')">
							</xsl:when>
							<xsl:otherwise>
								<xsl:call-template name="ShowMsaInfo">
									<xsl:with-param name="morph" select="RuleAllomorph/Morph/MSI"/>
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
						<xsl:value-of select="gloss"/>
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
		ShowPhonologicalRules
		Show phonological rule applications
		Parameters: phonologicalRules - the rules to show
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="ShowPhonologicalRules">
		<xsl:param name="phonologicalRules"/>
		<xsl:variable name="fHaveBlocker">
			<xsl:choose>
				<xsl:when test="$phonologicalRules/PhonologicalRuleSynthesisRequiredPOSTrace">Y</xsl:when>
				<xsl:when test="$phonologicalRules/PhonologicalRuleSynthesisMPRFeaturesTrace">Y</xsl:when>
				<xsl:otherwise>N</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
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
				<xsl:if test="$fHaveBlocker='Y'">
					<th>Rule not applied because...</th>
				</xsl:if>
			</tr>
			<xsl:for-each select="$phonologicalRules">
				<tr>
					<th>
						<xsl:attribute name="onclick">
							<xsl:text>JumpToToolBasedOnHvo(</xsl:text>
							<xsl:value-of select="substring-after(PhonologicalRule/@id, 'prule')"/>
							<xsl:text>)</xsl:text>
						</xsl:attribute>
						<xsl:attribute name="onmousemove">
							<xsl:text>MouseMove()</xsl:text>
						</xsl:attribute>
						<xsl:choose>
							<xsl:when test="string-length(normalize-space(PhonologicalRule/Description)) &gt; 0">
								<xsl:choose>
									<xsl:when test="PhonologicalRuleSynthesisRequiredPOSTrace or PhonologicalRuleSynthesisMPRFeaturesTrace">
										<span style="color:gray">
											<xsl:value-of select="normalize-space(PhonologicalRule/Description)"/>
										</span>
									</xsl:when>
									<xsl:otherwise>
										<xsl:value-of select="normalize-space(PhonologicalRule/Description)"/>
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
					<xsl:if test="$fHaveBlocker='Y'">
						<td>
					<xsl:choose>
						<xsl:when test="PhonologicalRuleSynthesisRequiredPOSTrace">
							<xsl:text>The stem's category is </xsl:text>
							<xsl:value-of select="PhonologicalRuleSynthesisRequiredPOSTrace/PhonologicalRuleStemPOS/Description"/>
							<xsl:text>, but this rule only applies when the stem's category is </xsl:text>
							<xsl:for-each select="PhonologicalRuleSynthesisRequiredPOSTrace/PhonologicalRuleRequiredPOSes/PhonologicalRuleRequiredPOS">
								<xsl:value-of select="Description"/>
								<xsl:call-template name="OutputListPunctuation">
									<xsl:with-param name="sConjunction" select="' or '"/>
									<xsl:with-param name="sFinalPunctuation" select="'.'"/>
								</xsl:call-template>
							</xsl:for-each>
						</xsl:when>
						<xsl:otherwise>
							<xsl:choose>
								<xsl:when test="PhonologicalRuleSynthesisMPRFeaturesTrace">
									<xsl:choose>
										<xsl:when test="PhonologicalRuleSynthesisMPRFeaturesTrace/PhonologicalRuleMPRFeatures/PhonologicalRuleMPRFeature">
											<xsl:text>The stem has the following properties:</xsl:text>
											<xsl:for-each select="PhonologicalRuleSynthesisMPRFeaturesTrace/PhonologicalRuleMPRFeatures/PhonologicalRuleMPRFeature">
												<xsl:value-of select="Description"/>
												<xsl:call-template name="OutputListPunctuation">
													<xsl:with-param name="sConjunction" select="' and '"/>
													<xsl:with-param name="sFinalPunctuation" select="','"/>
												</xsl:call-template>
											</xsl:for-each>
										</xsl:when>
										<xsl:otherwise>
											<xsl:text>The stem does not have any special properties,</xsl:text>
										</xsl:otherwise>
									</xsl:choose>
									<xsl:choose>
										<xsl:when test="PhonologicalRuleSynthesisMPRFeaturesTrace/@type='required'">
											<xsl:text> but this rule only applies when the stem has the following properties: </xsl:text>
										</xsl:when>
										<xsl:otherwise>
											<xsl:text> but this rule only applies when the stem has none of the following properties: </xsl:text>
										</xsl:otherwise>
									</xsl:choose>
									<xsl:for-each select="PhonologicalRuleSynthesisMPRFeaturesTrace/PhonologicalRuleConstrainingMPRFeatrues/PhonologicalRuleMPRFeature">
										<xsl:value-of select="Description"/>
										<xsl:call-template name="OutputListPunctuation">
											<xsl:with-param name="sConjunction">
												<xsl:choose>
													<xsl:when test="../../@type='required'">
														<xsl:text> and </xsl:text>
													</xsl:when>
													<xsl:otherwise>
														<xsl:text> or </xsl:text>
													</xsl:otherwise>
												</xsl:choose>
											</xsl:with-param>
											<xsl:with-param name="sFinalPunctuation" select="'.'"/>
										</xsl:call-template>
									</xsl:for-each>
								</xsl:when>
								<xsl:otherwise>
									<xsl:text>&#xa0;</xsl:text>
								</xsl:otherwise>
							</xsl:choose>
						</xsl:otherwise>
					</xsl:choose>
						</td>
					</xsl:if>
				</tr>
			</xsl:for-each>
		</table>
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
			<xsl:for-each select="/Wordform/WfiAnalysis/Morphs">
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
								<!-- This is one of the null allomorphs we create when building the
									input for the parser in order to still get the Word Grammar to have something in any
									required slots in affix templates. -->
								<td valign="top">
									<xsl:call-template name="ShowMorphSuccess"/>
								</td>
							</xsl:if>
						</xsl:for-each>
					</tr>
				</table>
			</td>
		</tr>
	</xsl:template>

	<xsl:template name="ShowOutput">
		<xsl:if test="name()='MorphologicalRuleAnalysisTrace' or name()='MorphologicalRuleSynthesisTrace'">
			<span>
				<xsl:attribute name="style">
					<xsl:text>color:</xsl:text>
					<xsl:call-template name="GetSuccessOrFailureColor"/>
					<xsl:text>; font-size:smaller;</xsl:text>
					<xsl:if test="$prmVernacularRTL='Y'">
						<xsl:text>direction:ltr</xsl:text>
					</xsl:if>
				</xsl:attribute>
				<xsl:text>Output: </xsl:text>
				<xsl:value-of select="Output"/>
			</span>
		</xsl:if>
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

		<xsl:variable name="analysisAffixes" select="$traceRoot/MorphologicalRuleAnalysisTrace[Output!=$prmHCTraceNoOutput and starts-with(MorphologicalRule/@id, 'mrule')]"/>
		<xsl:variable name="synthesisAffixes" select="$traceRoot/MorphologicalRuleSynthesisTrace[Output!=$prmHCTraceNoOutput and starts-with(MorphologicalRule/@id, 'mrule')]"/>
		<xsl:variable name="analysisCompoundRules" select="$traceRoot/MorphologicalRuleAnalysisTrace[Output!=$prmHCTraceNoOutput and starts-with(MorphologicalRule/@id, 'comp')]"/>
		<xsl:variable name="synthesisCompoundRules" select="$traceRoot/MorphologicalRuleSynthesisTrace[Output!=$prmHCTraceNoOutput and starts-with(MorphologicalRule/@id, 'comp')]"/>
		<xsl:variable name="synthesizedWords" select="$traceRoot/LexLookupTrace/WordSynthesisTrace"/>
		<xsl:variable name="phonologicalRules" select="$traceRoot[name()='PhonologicalRuleSynthesisTrace']"/>

		<xsl:variable name="parseNodes" select="$analysisAffixes | $synthesisAffixes | $analysisCompoundRules | $synthesisCompoundRules | $synthesizedWords | $phonologicalRules"/>
		<xsl:if test="$parseNodes">
			<xsl:for-each select="$parseNodes">
				<xsl:variable name="lastTemplateTrace" select="(preceding-sibling::*[name()='TemplateAnalysisTraceIn' or name()='TemplateSynthesisTraceIn' or name()='TemplateAnalysisTraceOut' or name()='TemplateSynthesisTraceOut'])[position() = last()]"/>
				<xsl:variable name="template">
					<xsl:choose>
						<xsl:when test="name($lastTemplateTrace) = 'TemplateAnalysisTraceIn' or name($lastTemplateTrace) = 'TemplateSynthesisTraceIn'">
							<xsl:value-of select="$lastTemplateTrace/AffixTemplate/Description"/>
						</xsl:when>
						<xsl:when test="name($lastTemplateTrace) = 'TemplateAnalysisTraceOut' or name($lastTemplateTrace) = 'TemplateSynthesisTraceOut'">
							<xsl:text></xsl:text>
						</xsl:when>
						<xsl:otherwise>
							<xsl:value-of select="$curTemplate"/>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:variable>
				 <table border="0" style="cursor:pointer;">
					<tr>
						<td width="10"/>
						<td style="border:solid; border-width:thin;">
							<a>
								<xsl:call-template name="ShowIcon"/>
								<table cellpadding="0pt" cellspacing="0pt">
									<tr>
										<td valign="top">
											<xsl:choose>
												<xsl:when test="name()='PhonologicalRuleSynthesisTrace'">
													<xsl:variable name="synthesisPhonologicalRules" select=". | following-sibling::PhonologicalRuleSynthesisTrace"/>
													<xsl:if test="$synthesisPhonologicalRules">
														<xsl:text>Phonological rules applied:</xsl:text>
														<xsl:call-template name="ShowPhonologicalRules">
															<xsl:with-param name="phonologicalRules" select="$synthesisPhonologicalRules"/>
														</xsl:call-template>
													</xsl:if>
												</xsl:when>
												<xsl:otherwise>
													<xsl:call-template name="ShowMorph">
														<xsl:with-param name="curTemplate" select="normalize-space($template)"/>
													</xsl:call-template>
												</xsl:otherwise>
											</xsl:choose>
										</td>
										<td valign="top">
											<xsl:call-template name="ShowAnyFailure"/>
										</td>
									</tr>
								</table>
								<xsl:call-template name="ShowAnySuccess"/>
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
									<xsl:when test="name()='MorphologicalRuleSynthesisTrace' and PhonologicalRuleSynthesisTrace">
										<xsl:call-template name="ShowTracePath">
											<xsl:with-param name="traceRoot" select="."/>
											<xsl:with-param name="curTemplate" select="$template"/>
										</xsl:call-template>
										<xsl:call-template name="ShowTracePath">
											<xsl:with-param name="traceRoot" select="PhonologicalRuleSynthesisTrace[1]"/>
										</xsl:call-template>
									</xsl:when>
									<xsl:when test="name()='PhonologicalRuleSynthesisTrace'">
										<!-- do nothing; we're done -->
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
				<!-- HC does not offer reasons (yet)
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
				-->
				<xsl:text> does not have a path to a successful parse (i.e. this path failed to produce a successful parse). </xsl:text>
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
