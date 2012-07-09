<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="html" version="4.0" encoding="UTF-8" indent="yes" media-type="text/html; charset=utf-8"/>
	<!--
================================================================
Format the xml returned from XAmple for user display.
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
	<!-- Test Names -->
	<xsl:variable name="sANCC_FT">
		<xsl:text>ANCC_FT</xsl:text>
	</xsl:variable>
	<xsl:variable name="sBoundStemRoot">
		<xsl:text>BoundStemOrRoot_FT</xsl:text>
	</xsl:variable>
	<xsl:variable name="sCategory">
		<xsl:text>Category</xsl:text>
	</xsl:variable>
	<xsl:variable name="sInterfixType_ST">
		<xsl:text>InterfixType_ST</xsl:text>
	</xsl:variable>
	<xsl:variable name="sMCC_FT">
		<xsl:text>MCC_FT</xsl:text>
	</xsl:variable>
	<xsl:variable name="sMEC_FT">
		<xsl:text>MEC_FT</xsl:text>
	</xsl:variable>
	<xsl:variable name="sOrderFinal_FT">
		<xsl:text>OrderFinal_FT</xsl:text>
	</xsl:variable>
	<xsl:variable name="sOrderIfx_ST">
		<xsl:text>OrderIfx_ST</xsl:text>
	</xsl:variable>
	<xsl:variable name="sOrderPfx_ST">
		<xsl:text>OrderPfx_ST</xsl:text>
	</xsl:variable>
	<xsl:variable name="sOrderSfx_ST">
		<xsl:text>OrderSfx_ST</xsl:text>
	</xsl:variable>
	<xsl:variable name="sROOTS_ST">
		<xsl:text>ROOTS_ST</xsl:text>
	</xsl:variable>
	<xsl:variable name="sSEC_ST">
		<xsl:text>SEC_ST:</xsl:text>
	</xsl:variable>
	<xsl:variable name="sInfixEnvironment">
		<xsl:text>InfixEnvironment</xsl:text>
	</xsl:variable>
	<xsl:variable name="sInfixType">
		<xsl:text>InfixType</xsl:text>
	</xsl:variable>
	<xsl:variable name="selectedMorphs" select="//selectedMorphs"/>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Main template
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="AmpleTrace">
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
						<xsl:value-of select="translate(//form,'.',' ')"/>
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
ConvertToAdHocCoprohibition
	Create proper wording for an adhoc coprohibition based on an MCC failure message
		Parameters: sTestName - XAmple test name
							 sType - type of adhoc coprohibition (morpheme or allomorph)
							 sEnvironmentMarker - +/ or -/
							 sEnvironmentBar - ~_ or _
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="ConvertToAdHocCoprohibition">
		<xsl:param name="sTestName">
			<xsl:value-of select="$sMCC_FT"/>
		</xsl:param>
		<xsl:param name="sType">
			<xsl:text>Morpheme</xsl:text>
		</xsl:param>
		<xsl:param name="sEnvironmentMarker">
			<xsl:text>+/</xsl:text>
		</xsl:param>
		<xsl:param name="sEnvironmentBar">
			<xsl:text>~_</xsl:text>
		</xsl:param>
		<xsl:variable name="sKeyItem">
			<xsl:value-of select="substring-before(substring-after(@test,concat($sTestName,':')), $sEnvironmentMarker)"/>
		</xsl:variable>
		<xsl:variable name="sRestContent">
			<xsl:value-of select="substring-after(@test, $sEnvironmentMarker)"/>
		</xsl:variable>
		<xsl:variable name="sAdjacency">
			<xsl:text>&#32;</xsl:text>
			<xsl:choose>
				<xsl:when test="contains(substring-before($sRestContent, $sEnvironmentBar), '...')">
					<xsl:text>somewhere after (or maybe anywhere around)</xsl:text>
				</xsl:when>
				<xsl:when test="contains(substring-after($sRestContent, $sEnvironmentBar), '...')">
					<xsl:text>somewhere before (or maybe anywhere around)</xsl:text>
				</xsl:when>
				<xsl:when test="string-length(substring-before($sRestContent, $sEnvironmentBar)) &gt; 1">
					<xsl:text>adjacent after</xsl:text>
				</xsl:when>
				<xsl:when test="string-length(substring-after($sRestContent, $sEnvironmentBar)) &gt; 1">
					<xsl:text>adjacent before</xsl:text>
				</xsl:when>
			</xsl:choose>
			<xsl:text>&#32;</xsl:text>
		</xsl:variable>
		<xsl:variable name="sOtherItems">
			<xsl:value-of select="substring-before($sRestContent, $sEnvironmentBar)"/>
			<xsl:value-of select="substring-after($sRestContent, $sEnvironmentBar)"/>
		</xsl:variable>
		<xsl:value-of select="$sType"/>
		<xsl:text> adhoc rule found: </xsl:text>
		<u>
			<!-- not the best way, but will do for now -->
			<xsl:if test="contains($sKeyItem, ':')">
				<xsl:attribute name="style">
					<xsl:call-template name="GetVernacularFont"/>
				</xsl:attribute>
			</xsl:if>
			<xsl:value-of select="$sKeyItem"/>
		</u>
		<xsl:text> cannot occur </xsl:text>
		<xsl:value-of select="$sAdjacency"/>
		<u>
			<!-- not the best way, but will do for now -->
			<xsl:if test="contains($sKeyItem, ':')">
				<xsl:attribute name="style">
					<xsl:call-template name="GetVernacularFont"/>
				</xsl:attribute>
			</xsl:if>
			<xsl:value-of select="translate($sOtherItems, '...', '')"/>
		</u>
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
OutputExceptionFeatureReason
	Output the reason for an MCC failure for exception features
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputExceptionFeatureReason">
		<xsl:variable name="sMorphId" select="substring-before(substring-after(substring-after(@test,':'), '_'), '_')"/>
		<xsl:variable name="morph" select="//morph[@morphname=$sMorphId]"/>
		<xsl:text>The affix '</xsl:text>
		<xsl:value-of select="$morph/shortName"/>
		<xsl:text>' requires a stem with an exception 'feature' of </xsl:text>
		<xsl:for-each select="$morph[1]/descendant::fromProductivityRestriction">
			<xsl:if test="position()!=1">
				<xsl:text> and </xsl:text>
			</xsl:if>
			<xsl:text> '</xsl:text>
			<xsl:value-of select="name"/>
			<xsl:text>'</xsl:text>
		</xsl:for-each>
		<xsl:text>, but there weren't any such stems.</xsl:text>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputInflectionClassReason
	Output the reason for an MEC failure of inflection class
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputInflectionClassReason">
		<xsl:variable name="sMorphId" select="substring-before(substring-after(substring-after(@test,':'), '_'), '_')"/>
		<xsl:variable name="morph" select="//morph[@morphname=$sMorphId]"/>
		<xsl:text>The affix '</xsl:text>
		<span>
			<xsl:attribute name="style">
				<xsl:call-template name="GetVernacularFont"/>
			</xsl:attribute>
			<xsl:value-of select="$morph/shortName"/>
		</span>
		<xsl:text>' requires a stem with an inflection class of </xsl:text>
		<xsl:for-each select="$morph[1]/inflectionClass">
			<xsl:if test="position()!=1">
				<xsl:text> or </xsl:text>
			</xsl:if>
			<xsl:value-of select="@abbr"/>
		</xsl:for-each>
		<xsl:text>, but there weren't any such stems.</xsl:text>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputStemNameReason
	Output the reason for an MEC failure of stem name
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputStemNameReason">
		<xsl:variable name="sMorphId" select="substring-before(substring-after(substring-after(@test,':'), '_'), '_')"/>
		<xsl:variable name="morph" select="//morph[@morphname=$sMorphId]"/>
		<xsl:text>The allomorph '</xsl:text>
		<span>
			<xsl:attribute name="style">
				<xsl:call-template name="GetVernacularFont"/>
			</xsl:attribute>
			<xsl:value-of select="$morph/alloform"/>
		</span>
		<xsl:text>' of the entry '</xsl:text>
		<span>
			<xsl:attribute name="style">
				<xsl:call-template name="GetVernacularFont"/>
			</xsl:attribute>
			<xsl:value-of select="$morph/shortName"/>
		</span>
		<xsl:text>' has a stem name of '</xsl:text>
		<xsl:value-of select="$morph/stemName"/>
		<xsl:text>.'  Therefore it requires some inflectional affixes with inflection features for that stem name, but there weren't any such inflectional affixes.</xsl:text>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		OutputStemNameDerivAffixReason
		Output the reason for an MEC failure of stem name
		Parameters: none
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="OutputStemNameDerivAffixReason">
		<xsl:variable name="sMorphId" select="substring-before(substring-after(substring-after(@test,':'), '_'), '_')"/>
		<xsl:variable name="morph" select="//morph[@morphname=$sMorphId]"/>
		<xsl:text>The entry '</xsl:text>
		<span>
			<xsl:attribute name="style">
				<xsl:call-template name="GetVernacularFont"/>
			</xsl:attribute>
			<xsl:value-of select="$morph/shortName"/>
		</span>
		<xsl:text>' has a from stem name of '</xsl:text>
		<xsl:value-of select="$morph/stemNameAffix"/>
		<xsl:text>.'  Therefore it requires a stem that has benn marked for that stem name, but there was no such stem.</xsl:text>
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
			<xsl:when test="//success">
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
				<xsl:call-template name="UseShowDetailsButton"/>
			</xsl:when>
			<xsl:when test="count(//trace/parseNode/leftover)='1'">
				<p>
					<span>
						<xsl:attribute name="style">
							<xsl:text>color:</xsl:text>
							<xsl:value-of select="$sFailureColor"/>
						</xsl:attribute>
						<xsl:text>This word failed to parse successfully because there are no prefix, root, or infix allomorphs that match the beginning of the word.</xsl:text>
					</span>
				</p>
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
				<xsl:call-template name="UseShowDetailsButton"/>
			</xsl:otherwise>
		</xsl:choose>
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
		<xsl:for-each select="failure | continuation/parseNode[endOfWord]/failure | leftover | maxReached">
			<span style="unicode-bidi:embed">
				<xsl:attribute name="style">
					<xsl:text>color:</xsl:text>
					<xsl:value-of select="$sFailureColor"/>
					<xsl:text>; font-size:smaller</xsl:text>
				</xsl:attribute>
				<xsl:text>&#xa0;&#xa0;(Reason: </xsl:text>
				<xsl:choose>
					<xsl:when test="contains(@test,$sSEC_ST)">
						<xsl:text>Environment incorrect:</xsl:text>
						<span>
							<xsl:attribute name="style">
								<xsl:call-template name="GetVernacularFont"/>
							</xsl:attribute>
							<xsl:value-of select="substring-after(@test,$sSEC_ST)"/>
						</span>
					</xsl:when>
					<xsl:when test="contains(@test,'PC-PATR word parse')">
						<xsl:text>Word grammar failed&#xa0;&#xa0;</xsl:text>
						<input type="button" value="Tell me more" name="BWGDetails" id="ShowWGDetailsButton" style="width: 100px; height: 26px">
							<xsl:attribute name="onclick">
								<xsl:text>ButtonShowWGDetails(</xsl:text>
								<xsl:choose>
									<xsl:when test="@id">"<xsl:value-of select="@id"/>"</xsl:when>
									<xsl:otherwise>
										<xsl:text>0</xsl:text>
									</xsl:otherwise>
								</xsl:choose>
								<xsl:text>)</xsl:text>
							</xsl:attribute>
						</input>
						<xsl:text>&#xa0;&#xa0;</xsl:text>
					</xsl:when>
					<xsl:when test="contains(@test,$sANCC_FT)">
						<xsl:call-template name="ConvertToAdHocCoprohibition">
							<xsl:with-param name="sTestName">
								<xsl:value-of select="$sANCC_FT"/>
							</xsl:with-param>
							<xsl:with-param name="sType">
								<xsl:text>Allomorph</xsl:text>
							</xsl:with-param>
							<xsl:with-param name="sEnvironmentMarker">
								<xsl:text>-/</xsl:text>
							</xsl:with-param>
							<xsl:with-param name="sEnvironmentBar">
								<xsl:text>_</xsl:text>
							</xsl:with-param>
						</xsl:call-template>
					</xsl:when>
					<xsl:when test="contains(@test,$sMCC_FT)">
						<xsl:choose>
							<xsl:when test="contains(@test,'ExcpFeat')">
								<xsl:call-template name="OutputExceptionFeatureReason"/>
							</xsl:when>
							<xsl:when test="contains(@test,'StemName')">
								<xsl:call-template name="OutputStemNameDerivAffixReason"/>
							</xsl:when>
							<xsl:when test="contains(@test,'IrregInflForm')">
								<xsl:text>There was at least one automatically generated null affix for an irregularly inflected form, but the stem was not for that irregularly inflected form.</xsl:text>
							</xsl:when>
							<xsl:when test="not(contains(@test,'~_'))">
								<!-- is circumfix -->
								<xsl:text>Only found one member of circumfix (</xsl:text>
								<xsl:value-of select="substring-before(substring-after(@test,':  '), '   +/')"/>
								<xsl:text>).  Both the left and the right members of a circumfix must be present.</xsl:text>
							</xsl:when>
							<xsl:otherwise>
								<xsl:call-template name="ConvertToAdHocCoprohibition">
									<xsl:with-param name="sTestName">
										<xsl:value-of select="$sMCC_FT"/>
									</xsl:with-param>
									<xsl:with-param name="sType">
										<xsl:text>Morpheme</xsl:text>
									</xsl:with-param>
									<xsl:with-param name="sEnvironmentMarker">
										<xsl:text>+/</xsl:text>
									</xsl:with-param>
									<xsl:with-param name="sEnvironmentBar">
										<xsl:text>~_</xsl:text>
									</xsl:with-param>
								</xsl:call-template>
							</xsl:otherwise>
						</xsl:choose>
					</xsl:when>
					<xsl:when test="contains(@test,$sMEC_FT)">
						<xsl:choose>
							<xsl:when test="contains(@test,'StemName')">
								<xsl:call-template name="OutputStemNameReason"/>
							</xsl:when>
							<xsl:otherwise>
								<xsl:call-template name="OutputInflectionClassReason"/>
							</xsl:otherwise>
						</xsl:choose>
					</xsl:when>
					<xsl:when test="contains(@test,$sCategory)">
						<xsl:value-of select="concat('A particle must stand alone or a compound linker was not in a compound', substring-after(@test,$sCategory))"/>
					</xsl:when>
					<xsl:when test="contains(@test,$sInterfixType_ST)">
						<xsl:text>A prefixing interfix was found before a suffixing interfix.  Prefixing interfixes may only follow suffixing interfixes.</xsl:text>
					</xsl:when>
					<xsl:when test="contains(@test,$sOrderFinal_FT)">
						<xsl:value-of select="concat('Affix ordering was incorrect somewhere in the word', substring-after(@test,$sOrderFinal_FT))"/>
					</xsl:when>
					<xsl:when test="contains(@test,$sOrderIfx_ST)">
						<xsl:value-of select="concat('Affix ordering was incorrect for a prefix/infix pair', substring-after(@test,$sOrderIfx_ST))"/>
					</xsl:when>
					<xsl:when test="contains(@test,$sOrderPfx_ST)">
						<xsl:value-of select="concat('Affix ordering was incorrect for a prefix/infix pair', substring-after(@test,$sOrderPfx_ST))"/>
					</xsl:when>
					<xsl:when test="contains(@test,$sOrderSfx_ST)">
						<xsl:value-of select="concat('Affix ordering was incorrect for a suffix pair', substring-after(@test,$sOrderSfx_ST))"/>
					</xsl:when>
					<xsl:when test="contains(@test,$sROOTS_ST)">
						<xsl:value-of select="concat('A compound linker was not properly in a compound or there was an attempt to compound using a particle or clitic', substring-after(@test,$sROOTS_ST))"/>
					</xsl:when>
					<xsl:when test="contains(@test,$sBoundStemRoot)">
						<xsl:text>A bound stem or root was found completely by itself.  These must have at least one other morpheme present.</xsl:text>
					</xsl:when>
					<xsl:when test="name()='leftover'">
						<xsl:text>No other matching allomorphs could be found</xsl:text>
					</xsl:when>
					<xsl:when test="name()='maxReached'">
						<xsl:text>The maximum number of allowed </xsl:text>
						<xsl:value-of select="."/>
						<xsl:text> was found, so the parser quit looking for more </xsl:text>
						<xsl:value-of select="."/>
						<xsl:text>.&#xa0;&#xa0;You may need to edit the Parser Parameters.</xsl:text>
					</xsl:when>
					<xsl:when test="contains(@test,$sInfixEnvironment)">
						<xsl:value-of select="concat('Infix position environment incorrect', substring-after(@test,$sInfixEnvironment))"/>
					</xsl:when>
					<xsl:when test="contains(@test,$sInfixType)">
						<xsl:value-of select="concat('Either there are no positions specified in the lexicon for this infix or the insertion type is incorrect', substring-after(@test,$sInfixEnvironment))"/>
					</xsl:when>
				</xsl:choose>
				<xsl:text>)</xsl:text>
			</span>
		</xsl:for-each>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ShowAnySuccess
	Show a success message if this is at end of word and a success
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="ShowAnySuccess">
		<xsl:for-each select="success | continuation/parseNode[endOfWord]/success">
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
		</xsl:for-each>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ShowIcon
	Show the "tree" icon
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="ShowIcon">
		<xsl:variable name="sIcon">
			<xsl:choose>
				<xsl:when test="failure or leftover or maxReached or $selectedMorphs or count(continuation/parseNode) = 1 and continuation/parseNode[endOfWord]">
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
ShowMsaInfo
	Show the information associated with the msa
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="ShowMsaInfo">
		<xsl:param name="morph"/>
		<xsl:for-each select="$morph">
				<span>
					<xsl:attribute name="style">
						<xsl:text>; font-size:smaller</xsl:text>
					</xsl:attribute>
					<xsl:choose>
						<xsl:when test="@wordType='root'">
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
						<xsl:when test="contains(@wordType,'deriv')">
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
						<xsl:when test="@wordType='prefix' or @wordType='suffix'">
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
						<xsl:when test="@wordType='proclitic' or @wordType='enclitic'">
							<xsl:value-of select="@wordType"/>
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
						<xsl:when test="@wordType='clitic'">
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
ShowMorph
	Show the morpheme information
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="ShowMorph">
		<xsl:variable name="sColor">
			<xsl:choose>
				<xsl:when test="descendant::success">
					<xsl:value-of select="$sSuccessColor"/>
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="$sFailureColor"/>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<span class="interblock">
			<table cellpadding="0" cellspacing="0">
				<xsl:attribute name="style">
					<xsl:text>color:</xsl:text>
					<xsl:value-of select="$sColor"/>
					<xsl:if test="$prmVernacularRTL='Y'">
						<xsl:text>; text-align:right;</xsl:text>
					</xsl:if>
				</xsl:attribute>
				<xsl:attribute name="onclick">
					<xsl:text>JumpToToolBasedOnHvo(</xsl:text>
					<xsl:value-of select="morph/@alloid"/>
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
						<xsl:value-of select="morph/alloform"/>
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
						<xsl:value-of select="morph/citationForm"/>
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
						<xsl:variable name="sGloss" select="morph/gloss"/>
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
							<xsl:with-param name="morph" select="morph"/>
						</xsl:call-template>
					</td>
				</tr>
			</table>
		</span>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ShowPreviousMorph
	A recursive routine to back up the result tree to the previous morpheme
		Parameters: current - the current parseNode
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="ShowPreviousMorph">
		<xsl:param name="current"/>
		<xsl:if test="name($current)='parseNode'">
			<xsl:call-template name="ShowPreviousMorph">
				<xsl:with-param name="current" select="$current/../.."/>
			</xsl:call-template>
		</xsl:if>
		<xsl:for-each select="$current[morph]">
			<xsl:if test="not(morph/lexEntryInflType)">
				<!-- This is one of the null allomorphs we create when building the
					input for the parser in order to still get the Word Grammar to have something in any
					required slots in affix templates. -->
				<td valign="top">
					<xsl:call-template name="ShowMorph"/>
				</td>
			</xsl:if>
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
		<table border="0" style="cursor:pointer;">
			<xsl:for-each select="//success">
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
						<!-- unsuccessful attempt at adding labels
						<td>
							<table>
								<tr>
									<td>Morphemes</td>
								</tr>
								<tr>
									<td>Lex. Entries</td>
								</tr>
								<tr>
									<td>&#xa0;Gloss</td>
								</tr>
								<tr>
									<td>&#xa0;Gram. Info</td>
								</tr>
							</table>
						</td>
							-->
						<xsl:call-template name="ShowPreviousMorph">
							<xsl:with-param name="current" select=".."/>
						</xsl:call-template>
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
		<xsl:param name="parseNodes"/>
		<xsl:if test="$parseNodes">
			<xsl:for-each select="$parseNodes/parseNode[morph | leftover | maxReached]">
				<table border="0" style="cursor:pointer;">
					<tr>
						<td width="10"/>
						<td style="border:solid; border-width:thin;">
							<a>
								<xsl:call-template name="ShowIcon"/>
								<table cellpadding="0pt" cellspacing="0pt">
									<tr>
										<td valign="top">
											<xsl:call-template name="ShowMorph"/>
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
								<xsl:call-template name="ShowTracePath">
									<xsl:with-param name="parseNodes" select="continuation"/>
								</xsl:call-template>
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
				<table>
					<tr>
						<td>
							<xsl:call-template name="ShowTracePath">
								<xsl:with-param name="parseNodes" select="//trace"/>
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
