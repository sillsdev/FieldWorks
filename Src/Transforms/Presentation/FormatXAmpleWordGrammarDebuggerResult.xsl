<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
   <xsl:output method="html" version="4.0" encoding="UTF-8" indent="yes" media-type="text/html; charset=utf-8"/>
   <!--
================================================================
Format the resulting state of an application of the Word Grammar (for debugging)
================================================================
Revision History is at the end of this file.

- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Preamble
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <!-- Parameters -->
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
	  <xsl:text>10pt</xsl:text>
   </xsl:param>
   <xsl:param name="prmVernacularRTL">
	  <xsl:text>N</xsl:text>
   </xsl:param>
   <!-- Variables -->
   <xsl:variable name="sTryNextPass">
	  <xsl:text>Try the next pass</xsl:text>
   </xsl:variable>
   <xsl:variable name="sPreviousButtonText">
	  <xsl:text>Go back to the previous pass</xsl:text>
   </xsl:variable>
   <!-- Colors -->
   <xsl:variable name="sSuccessColor">
	  <xsl:text>green</xsl:text>
   </xsl:variable>
   <xsl:variable name="sFailureColor">
	  <xsl:text>red</xsl:text>
   </xsl:variable>
   <xsl:variable name="sWordFormColor">
	  <xsl:text>blue</xsl:text>
   </xsl:variable>
   <xsl:variable name="sGlossColor">
	  <xsl:text>green</xsl:text>
   </xsl:variable>
   <xsl:variable name="sCitationFormColor">
	  <xsl:text>maroon</xsl:text>
   </xsl:variable>
   <xsl:variable name="sMorphemeColor">
	  <xsl:text>blue</xsl:text>
   </xsl:variable>
   <xsl:variable name="sTypeColor">
	  <xsl:text>black</xsl:text>
   </xsl:variable>
   <xsl:variable name="sOtherInfoColor">
	  <xsl:text>brown</xsl:text>
   </xsl:variable>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Main template
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template match="/word">
	  <html>
		 <head>
			<xsl:call-template name="Script"/>
		 </head>
		 <body>
			<h1>
			   <xsl:text>Show why the Word Grammar failed for </xsl:text>
			   <span>
				  <xsl:attribute name="style">
					 <xsl:text>color:</xsl:text>
					 <xsl:value-of select="$sWordFormColor"/>
					 <xsl:text>; font-family:</xsl:text>
					 <xsl:value-of select="$prmVernacularFont"/>
				  </xsl:attribute>
				  <xsl:value-of select="translate(form,' ',' ')"/>
			   </span>
			   <xsl:text>.</xsl:text>
			</h1>
			<hr/>
			<xsl:choose>
			   <xsl:when test="//seq">
				  <xsl:call-template name="ShowSteps"/>
				  <hr/>
				  <input type="button" name="BBack" id="GoBack" style="width: 220px; height: 26px">
					 <xsl:attribute name="onclick">
						<xsl:text>ButtonGoBack()</xsl:text>
					 </xsl:attribute>
					 <xsl:attribute name="value">
						<xsl:value-of select="$sPreviousButtonText"/>
					 </xsl:attribute>
				  </input>
				  <hr/>
				  <xsl:call-template name="ShowMorphemes"/>
			   </xsl:when>
			   <xsl:otherwise>
				  <p>
					 <xsl:attribute name="style">
						<xsl:text>color:</xsl:text>
						<xsl:value-of select="$sSuccessColor"/>
					 </xsl:attribute>
					 <xsl:text>You have reached a completed, successful analysis.  </xsl:text>
				  </p>
				  <p>
					 <xsl:text>If this is not what you intended, please click the '</xsl:text>
					 <xsl:value-of select="$sPreviousButtonText"/>
					 <xsl:text>' button.</xsl:text>
				  </p>
				  <hr/>
				  <input type="button" name="BBack" id="GoBack" style="width: 220px; height: 26px">
					 <xsl:attribute name="onclick">
						<xsl:text>ButtonGoBack()</xsl:text>
					 </xsl:attribute>
					 <xsl:attribute name="value">
						<xsl:value-of select="$sPreviousButtonText"/>
					 </xsl:attribute>
				  </input>
			   </xsl:otherwise>
			</xsl:choose>
		 </body>
	  </html>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
BracketItem
	Show brackets around an item
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="BracketItem">
	  <xsl:param name="sName"/>
	  <table cellpadding="0" cellspacing="0">
		 <tr valign="top">
			<td>
			   <xsl:text>&#xa0;[</xsl:text>
			   <span style="vertical-align:sub">
				  <xsl:value-of select="$sName"/>
			   </span>
			   <xsl:text>&#xa0;</xsl:text>
			</td>
			<xsl:for-each select="*">
			   <xsl:call-template name="ShowItem"/>
			</xsl:for-each>
			<td>
			   <xsl:text>&#xa0;</xsl:text>
			   <span style="vertical-align:sub">
				  <xsl:value-of select="$sName"/>
			   </span>
			   <xsl:text>]&#xa0;</xsl:text>
			</td>
		 </tr>
	  </table>
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
Script
	Output the JavaScript script to handle dynamic "tree"
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="Script">
	  <script language="JavaScript" id="clientEventHandlersJS">
		 <xsl:text>
	function ButtonTryNextPass(nodeId)
	{
	  window.external.TryWordGrammarAgain(nodeId);
	}

	function ButtonGoBack()
	{
	  window.external.GoToPreviousWordGrammarPage();
	}

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
ShowFeatures
	Show the allomorph form, the gloss, and citation form of a morpheme
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="ShowFeatures">
	  <xsl:param name="fs"/>
	  <table cellpadding="0" cellspacing="0">
		 <xsl:attribute name="style">
			<xsl:text>color:</xsl:text>
			<xsl:value-of select="$sOtherInfoColor"/>
		 </xsl:attribute>
		 <tr valign="top">
			<td>
			   <xsl:text disable-output-escaping="yes"> [ </xsl:text>
			</td>
			<td>
			   <table cellpadding="0" cellspacing="0">
				  <xsl:attribute name="style">
					 <xsl:text>color:</xsl:text>
					 <xsl:value-of select="$sOtherInfoColor"/>
				  </xsl:attribute>
				  <xsl:for-each select="$fs/feature">
					 <tr valign="top">
						<td>
						   <xsl:value-of select="name"/>
						   <xsl:text disable-output-escaping="yes"> : </xsl:text>
						</td>
						<td>
						   <xsl:choose>
							  <xsl:when test="value/fs">
								 <xsl:for-each select="value/fs">
									<xsl:call-template name="ShowFeatures">
									   <xsl:with-param name="fs" select="."/>
									</xsl:call-template>
								 </xsl:for-each>
							  </xsl:when>
							  <xsl:otherwise>
								 <xsl:value-of select="value"/>
							  </xsl:otherwise>
						   </xsl:choose>
						</td>
					 </tr>
				  </xsl:for-each>
			   </table>
			</td>
			<td valign="bottom">]</td>
		 </tr>
	  </table>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ShowFormGlossCitation
	Show the allomorph form, the gloss, and citation form of a morpheme
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="ShowFormGlossCitation">
	  <table cellpadding="0" cellspacing="0">
		 <xsl:if test="$prmVernacularRTL='Y'">
			<xsl:attribute name="style">
			   <xsl:text> direction:rtl; text-align:right</xsl:text>
			</xsl:attribute>
		 </xsl:if>
		 <xsl:call-template name="CreateJumpToAttributes">
			<xsl:with-param name="hvo" select="@alloid"/>
		 </xsl:call-template>
		 <tr>
			<td>
			   <xsl:attribute name="style">
				  <xsl:text>color:</xsl:text>
				  <xsl:value-of select="$sMorphemeColor"/>
				  <xsl:call-template name="GetVernacularFont"/>
				  <xsl:if test="$prmVernacularRTL='Y'">
					 <xsl:text>; direction:rtl; </xsl:text>
				  </xsl:if>
			   </xsl:attribute>
			   <xsl:text>&#xa0;</xsl:text>
			   <xsl:value-of select="alloform"/>
			</td>
		 </tr>
		 <tr>
			<td>
			   <xsl:attribute name="style">
				  <xsl:text>color:</xsl:text>
				  <xsl:value-of select="$sGlossColor"/>
				  <xsl:call-template name="GetAnalysisFont"/>
			   </xsl:attribute>
			   <xsl:text>&#xa0;</xsl:text>
			   <xsl:value-of select="gloss"/>
			</td>
		 </tr>
		 <tr>
			<td>
			   <xsl:attribute name="style">
				  <xsl:text>color:</xsl:text>
				  <xsl:value-of select="$sCitationFormColor"/>
				  <xsl:call-template name="GetVernacularFont"/>
				  <xsl:if test="$prmVernacularRTL='Y'">
					 <xsl:text>; direction:rtl; </xsl:text>
				  </xsl:if>
			   </xsl:attribute>
			   <xsl:text>&#xa0;</xsl:text>
			   <xsl:value-of select="citationForm"/>
			</td>
		 </tr>
	  </table>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ShowItem
	Show an item in a parse
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="ShowItem">
	  <td>
		 <xsl:choose>
			<!--
	  <xsl:when test="name()='full'">
		<xsl:call-template name="BracketItem">
		  <xsl:with-param name="sName">Full</xsl:with-param>
		</xsl:call-template>
	  </xsl:when>
	  -->
			<xsl:when test="name()='morph'">
			   <xsl:call-template name="ShowFormGlossCitation"/>
			   <xsl:text>&#xa0;</xsl:text>
			</xsl:when>
			<xsl:when test="name()='fs' or name()='feature' or name()='name' or name()='value' or name()='productivityRestriction' or name()='fromProductivityRestriction' or name()='toProductivityRestriction' or name()='stemName'"/>
			<xsl:otherwise>
			   <xsl:call-template name="BracketItem">
				  <xsl:with-param name="sName">
					 <xsl:value-of select="translate(substring(name(), 1, 1), 'abcdefghijklmnopqrstuvwxyz', 'ABCDEFGHIJKLMNOPQRSTUVWXYZ')"/>
					 <xsl:value-of select="substring(name(), 2)"/>
				  </xsl:with-param>
			   </xsl:call-template>
			</xsl:otherwise>
		 </xsl:choose>
	  </td>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ShowMorphemes
	Show the information about the morphemes used
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="ShowMorphemes">
	  <p>These are the morphemes used:</p>
	  <table border="1" cellpadding="5" style="margin-left: 0.25in">
		 <xsl:choose>
			<xsl:when test="$prmVernacularRTL='Y'">
			   <tr>
				  <th>Other info</th>
				  <th>type</th>
				  <th>Allomorph</th>
			   </tr>
			   <xsl:for-each select="/word/seq[1]/descendant::morph">
				  <tr>
					 <xsl:call-template name="ShowOtherInfo"/>
					 <xsl:call-template name="ShowType"/>
					 <td style="direction:rtl; text-align:right">
						<xsl:call-template name="ShowFormGlossCitation"/>
					 </td>
				  </tr>
			   </xsl:for-each>
			</xsl:when>
			<xsl:otherwise>
			   <tr>
				  <th>Allomorph</th>
				  <th>type</th>
				  <th>Other info</th>
			   </tr>
			   <xsl:for-each select="/word/seq[1]/descendant::morph">
				  <tr>
					 <td>
						<xsl:call-template name="ShowFormGlossCitation"/>
					 </td>
					 <xsl:call-template name="ShowType"/>
					 <xsl:call-template name="ShowOtherInfo"/>
				  </tr>
			   </xsl:for-each>
			</xsl:otherwise>
		 </xsl:choose>
	  </table>
	  <br/>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ShowOtherInfo
	Show the other info
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="ShowOtherInfo">
	  <td>
		 <xsl:attribute name="style">
			<xsl:text>color:</xsl:text>
			<xsl:value-of select="$sOtherInfoColor"/>
		 </xsl:attribute>
		 <xsl:call-template name="CreateJumpToAttributes">
			<xsl:with-param name="hvo" select="@morphname"/>
		 </xsl:call-template>
		 <xsl:choose>
			<xsl:when test="@wordType='root'">
			   <table cellpadding="0" cellspacing="0">
				  <xsl:attribute name="style">
					 <xsl:text>color:</xsl:text>
					 <xsl:value-of select="$sOtherInfoColor"/>
				  </xsl:attribute>
				  <tr>
					 <td>
						<xsl:text>Category = </xsl:text>
						<xsl:value-of select="stemMsa/@catAbbr"/>
					 </td>
				  </tr>
				  <xsl:if test="stemMsa/@inflClassAbbr != ''">
					 <tr>
						<td>
						   <xsl:text>Inflection class = </xsl:text>
						   <xsl:value-of select="stemMsa/@inflClassAbbr"/>
						</td>
					 </tr>
				  </xsl:if>
				  <xsl:if test="stemName != ''">
					 <tr>
						<td>
						   <xsl:text>Stem name = </xsl:text>
						   <xsl:value-of select="stemName"/>
						</td>
					 </tr>
				  </xsl:if>
				  <xsl:if test="stemMsa/fs">
					 <tr>
						<td>
						   <xsl:call-template name="ShowFeatures">
							  <xsl:with-param name="fs" select="stemMsa/fs"/>
						   </xsl:call-template>
						</td>
					 </tr>
				  </xsl:if>
				  <xsl:if test="stemMsa/productivityRestriction">
					 <tr>
						<td>
						   <xsl:call-template name="ShowProdRestrict">
							  <xsl:with-param name="pr" select="stemMsa/productivityRestriction"/>
						   </xsl:call-template>
						</td>
					 </tr>
				  </xsl:if>
			   </table>
			</xsl:when>
			<xsl:when test="@wordType='derivPfx' or @wordType='derivSfx' or @wordType='derivCircumPfx' or @wordType='derivCircumSfx'">
			   <table cellpadding="0" cellspacing="0">
				  <xsl:attribute name="style">
					 <xsl:text>color:</xsl:text>
					 <xsl:value-of select="$sOtherInfoColor"/>
				  </xsl:attribute>
				  <tr>
					 <td>
						<xsl:text>From category = </xsl:text>
						<xsl:value-of select="derivMsa/@fromCatAbbr"/>
					 </td>
				  </tr>
				  <xsl:if test="derivMsa/fromFS">
					 <tr>
						<td>
						   <xsl:call-template name="ShowFeatures">
							  <xsl:with-param name="fs" select="derivMsa/fromFS"/>
						   </xsl:call-template>
						</td>
					 </tr>
				  </xsl:if>
				  <xsl:if test="derivMsa/fromProductivityRestriction">
					 <tr>
						<td>
						   <xsl:call-template name="ShowProdRestrict">
							  <xsl:with-param name="sPreface" select="'From '"/>
							  <xsl:with-param name="pr" select="derivMsa/fromProductivityRestriction"/>
						   </xsl:call-template>
						</td>
					 </tr>
				  </xsl:if>
				  <tr>
					 <td>
						<xsl:text>To category = </xsl:text>
						<xsl:value-of select="derivMsa/@toCatAbbr"/>
					 </td>
				  </tr>
				  <xsl:if test="derivMsa/@toInflClassAbbr!=''">
					 <tr>
						<td>
						   <xsl:text>To inflection class = </xsl:text>
						   <xsl:value-of select="derivMsa/@toInflClassAbbr"/>
						</td>
					 </tr>
				  </xsl:if>
				  <xsl:if test="derivMsa/toFS">
					 <tr>
						<td>
						   <xsl:call-template name="ShowFeatures">
							  <xsl:with-param name="fs" select="derivMsa/toFS"/>
						   </xsl:call-template>
						</td>
					 </tr>
				  </xsl:if>
				  <xsl:if test="derivMsa/toProductivityRestriction">
					 <tr>
						<td>
						   <xsl:call-template name="ShowProdRestrict">
							  <xsl:with-param name="sPreface" select="'To '"/>
							  <xsl:with-param name="pr" select="derivMsa/toProductivityRestriction"/>
						   </xsl:call-template>
						</td>
					 </tr>
				  </xsl:if>
				  <xsl:if test="affixAlloFeats/fs">
					 <xsl:call-template name="ShowRequiredFeatures"/>
				  </xsl:if>
			   </table>
			</xsl:when>
			<xsl:when test="@wordType='prefix' or @wordType='suffix' or @wordType='circumPfx' or @wordType='circumSfx'">
			   <xsl:text>unclassified affix</xsl:text>
			</xsl:when>
			<xsl:when test="@wordType='proclitic' or @wordType='enclitic'">
			   <xsl:text>clitic</xsl:text>
			   <table cellpadding="0" cellspacing="0">
				  <xsl:attribute name="style">
					 <xsl:text>color:</xsl:text>
					 <xsl:value-of select="$sOtherInfoColor"/>
				  </xsl:attribute>
				  <tr>
					 <td>
						<xsl:if test="stemMsa/@cat!=0">
						   <xsl:text>Category = </xsl:text>
						   <xsl:value-of select="stemMsa/@catAbbr"/>
						</xsl:if>
					 </td>
				  </tr>
				  <tr>
					 <td>
						<xsl:text>Attaches to: </xsl:text>
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
					 </td>
				  </tr>
				  <xsl:if test="stemMsa/fs">
					 <tr>
						<td>
						   <xsl:call-template name="ShowFeatures">
							  <xsl:with-param name="fs" select="stemMsa/fs"/>
						   </xsl:call-template>
						</td>
					 </tr>
				  </xsl:if>
			   </table>
			</xsl:when>
			<xsl:otherwise>
			   <!-- an inflectional affix -->
			   <table>
				  <tr>
					 <td>
						<xsl:text>Category = </xsl:text>
						<xsl:value-of select="inflMsa/@catAbbr"/>
						<xsl:text>; Slot = </xsl:text>
						<xsl:choose>
						   <xsl:when test="inflMsa/@slotAbbr!='??'">
							  <xsl:if test="inflMsa/@slotOptional='true'">
								 <xsl:text>(</xsl:text>
							  </xsl:if>
							  <xsl:value-of select="inflMsa/@slotAbbr"/>
							  <xsl:if test="inflMsa/@slotOptional='true'">
								 <xsl:text>)</xsl:text>
							  </xsl:if>
						   </xsl:when>
						   <xsl:otherwise>
							  <xsl:text>Unspecified slot or category</xsl:text>
						   </xsl:otherwise>
						</xsl:choose>
					 </td>
				  </tr>
				  <xsl:if test="inflMsa/fs">
					 <tr>
						<td>
						   <xsl:call-template name="ShowFeatures">
							  <xsl:with-param name="fs" select="inflMsa/fs"/>
						   </xsl:call-template>
						</td>
					 </tr>
				  </xsl:if>
				  <xsl:if test="inflMsa/fromProductivityRestriction">
					 <tr>
						<td>
						   <xsl:call-template name="ShowProdRestrict">
							  <xsl:with-param name="sPreface" select="'From '"/>
							  <xsl:with-param name="pr" select="inflMsa/fromProductivityRestriction"/>
						   </xsl:call-template>
						</td>
					 </tr>
				  </xsl:if>
				  <xsl:if test="affixAlloFeats/fs">
					 <xsl:call-template name="ShowRequiredFeatures"/>
				  </xsl:if>
			   </table>
			</xsl:otherwise>
		 </xsl:choose>
	  </td>
   </xsl:template>
   <xsl:template name="ShowRequiredFeatures">
	  <tr>
		 <td>
			<span>Required Features: </span>
			<xsl:call-template name="ShowFeatures">
			   <xsl:with-param name="fs" select="affixAlloFeats/fs"/>
			</xsl:call-template>
		 </td>
	  </tr>
   </xsl:template>
   <!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		CreateJumpToAttributes
		Create attributes needed for jump ablity in FLEx
		Parameters: hvo - the element containing the hvo to jump to
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
   <xsl:template name="CreateJumpToAttributes">
	  <xsl:param name="hvo"/>
	  <xsl:attribute name="onclick">
		 <xsl:text>JumpToToolBasedOnHvo(</xsl:text>
		 <xsl:value-of select="$hvo"/>
		 <xsl:text>)</xsl:text>
	  </xsl:attribute>
	  <xsl:attribute name="onmousemove">
		 <xsl:text>MouseMove()</xsl:text>
	  </xsl:attribute>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ShowProdRestrict
	Show the the steps tried
		Parameters: sPreface - any words before "exception"
							 pr - set of productivity restrictions
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="ShowProdRestrict">
	  <xsl:param name="sPreface"/>
	  <xsl:param name="pr"/>
	  <xsl:value-of select="$sPreface"/>
	  <xsl:text disable-output-escaping="yes"> Exception &amp;ldquo;features&amp;rdquo;:</xsl:text>
	  <table>
		 <xsl:attribute name="style">
			<xsl:text>color:</xsl:text>
			<xsl:value-of select="$sOtherInfoColor"/>
		 </xsl:attribute>
		 <xsl:for-each select="$pr">
			<tr>
			   <td>
				  <xsl:value-of select="name"/>
			   </td>
			</tr>
		 </xsl:for-each>
	  </table>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ShowSteps
	Show the the steps tried
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="ShowSteps">
	  <p>
		 <xsl:text>The Word Grammar tries to build a word in a number of ways in various kinds of steps.  This tool shows what happens at a particular pass.  It shows all the steps that the Word Grammar tried at this pass.  Look for a step that would be reasonable given your understanding of how the wordform should be parsed.  If such a step is shown as succeeding, click the '</xsl:text>
		 <xsl:value-of select="$sTryNextPass"/>
		 <xsl:text>' button to see the next pass.  If such a step failed, read the description of the failure(s) and try to figure out how to correct it.  [Eventually we plan to have some help to aid you in doing this, but for now you'll need to make a note of the problem, close this dialog box, and then go to the appropriate area and try to fix the problem.]</xsl:text>
	  </p>
	  <hr/>
	  <xsl:choose>
		 <xsl:when test="//resultSoFar">
			<p>At this point, the Word Grammar has built the following:</p>
			<div style="margin-left:.25in">
			   <table>
				  <tr valign="top">
					 <xsl:choose>
						<xsl:when test="$prmVernacularRTL='Y'">
						   <xsl:for-each select="//resultSoFar/*">
							  <xsl:sort select="position()" order="descending"/>
							  <xsl:call-template name="ShowItem"/>
						   </xsl:for-each>
						</xsl:when>
						<xsl:otherwise>
						   <xsl:for-each select="//resultSoFar/*">
							  <xsl:call-template name="ShowItem"/>
						   </xsl:for-each>
						</xsl:otherwise>
					 </xsl:choose>
				  </tr>
			   </table>
			</div>
		 </xsl:when>
		 <xsl:otherwise>
			<p>Let's imagine that the Word Grammar has not started yet. That is, the Word
					Grammar has not built anything yet. There's just the sequence of morphemes as
					shown.</p>
			<div style="margin-left:.25in">
			   <table>
				  <tr valign="top">
					 <xsl:choose>
						<xsl:when test="$prmVernacularRTL='Y'">
						   <xsl:for-each select="/word/seq[1]/descendant::morph">
							  <xsl:sort select="position()" order="descending"/>
							  <xsl:call-template name="ShowItem"/>
						   </xsl:for-each>
						</xsl:when>
						<xsl:otherwise>
						   <xsl:for-each select="/word/seq[1]/descendant::morph">
							  <xsl:call-template name="ShowItem"/>
						   </xsl:for-each>
						</xsl:otherwise>
					 </xsl:choose>
				  </tr>
			   </table>
			</div>
		 </xsl:otherwise>
	  </xsl:choose>
	  <hr/>
	  <p>
		 <xsl:text>The Word Grammar tried the following set of steps.  If a step succeeded, its result is shown in </xsl:text>
		 <span>
			<xsl:attribute name="style">
			   <xsl:text>color:</xsl:text>
			   <xsl:value-of select="$sSuccessColor"/>
			</xsl:attribute>
			<xsl:text>green</xsl:text>
		 </span>
		 <xsl:text>.  If a step failed, then each error condition found is shown in </xsl:text>
		 <span>
			<xsl:attribute name="style">
			   <xsl:text>color:</xsl:text>
			   <xsl:value-of select="$sFailureColor"/>
			</xsl:attribute>
			<xsl:text>red</xsl:text>
		 </span>
		 <xsl:text>.</xsl:text>
	  </p>
	  <ul>
		 <xsl:for-each select="//seq">
			<li>
			   <xsl:for-each select="descendant-or-self::warning">
				  <span style="font-size:larger; font-weight:bold; color:navy">WARNING:  <xsl:value-of select="."/></span>
				  <br/>
			   </xsl:for-each>
			   <xsl:choose>
				  <xsl:when test="descendant::failure">
					 <xsl:value-of select="@step"/>
					 <ul style="margin-top:.5mm;margin-bottom:.5mm">
						<xsl:for-each select="descendant::failure">
						   <li>
							  <xsl:attribute name="style">
								 <xsl:text>color:</xsl:text>
								 <xsl:value-of select="$sFailureColor"/>
								 <xsl:text/>
							  </xsl:attribute>
							  <xsl:choose>
								 <xsl:when test="content">
									<xsl:value-of select="content"/>
									<xsl:for-each select="stem">
									   <table cellpadding="0" cellspacing="0" style="margin-left:0.25in">
										  <tr>
											 <xsl:call-template name="ShowItem"/>
										  </tr>
									   </table>
									</xsl:for-each>
								 </xsl:when>
								 <xsl:otherwise>
									<!-- this is not right; we need to have the xml tell us what and where - could be many within a given failure message
												<xsl:call-template name="CreateJumpToAttributes">
													<xsl:with-param name="hvo" select="../@syncat"/>
												</xsl:call-template>
												-->
									<xsl:value-of select="."/>
								 </xsl:otherwise>
							  </xsl:choose>
						   </li>
						</xsl:for-each>
					 </ul>
				  </xsl:when>
				  <xsl:otherwise>
					 <xsl:value-of select="@step"/>
					 <ul style="margin-top:.5mm;margin-bottom:.5mm">
						<li>
						   <xsl:attribute name="style">
							  <xsl:text>color:</xsl:text>
							  <xsl:value-of select="$sSuccessColor"/>
							  <xsl:text/>
						   </xsl:attribute>
						   <xsl:text>This succeeded.&#xa0;&#xa0;</xsl:text>
						   <input type="button" name="BWGDetails" id="TryNextPassButton" style="width: 120px; height: 26px">
							  <xsl:attribute name="onclick">
								 <xsl:text>ButtonTryNextPass(</xsl:text>"<xsl:value-of select="position()"/>"<xsl:text>)</xsl:text>
							  </xsl:attribute>
							  <xsl:attribute name="value">
								 <xsl:value-of select="$sTryNextPass"/>
							  </xsl:attribute>
						   </input>
						</li>
					 </ul>
				  </xsl:otherwise>
			   </xsl:choose>
			</li>
		 </xsl:for-each>
	  </ul>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ShowType
	Show the the type info
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="ShowType">
	  <td align="center">
		 <xsl:attribute name="style">
			<xsl:text>color:</xsl:text>
			<xsl:value-of select="$sTypeColor"/>
		 </xsl:attribute>
		 <xsl:choose>
			<xsl:when test="number(@wordType)>0">
			   <xsl:text>inflectional affix</xsl:text>
			</xsl:when>
			<xsl:when test="@wordType='derivPfx'">
			   <xsl:text>derivational prefix</xsl:text>
			</xsl:when>
			<xsl:when test="@wordType='derivSfx'">
			   <xsl:text>derivational suffix</xsl:text>
			</xsl:when>
			<xsl:when test="@wordType='derivCircumPfx'">
			   <xsl:text>derivational circumfix (left member)</xsl:text>
			</xsl:when>
			<xsl:when test="@wordType='derivCircumSfx'">
			   <xsl:text>derivational circumfix (right member)</xsl:text>
			</xsl:when>
			<xsl:otherwise>
			   <xsl:value-of select="@wordType"/>
			</xsl:otherwise>
		 </xsl:choose>
	  </td>
   </xsl:template>
</xsl:stylesheet>
<!--
================================================================
Revision History
- - - - - - - - - - - - - - - - - - -
30-Jan-2006  Andy Black  Add productivity restrictions
02-Nov-2005  Andy Black  Add msa info to clitics.
23-May-2005	Andy Black	Improve wording in failure instructions.
20-May-2005	Andy Black	Use new multi-part alloform info
26-Apr-2005	Andy Black	Changed "unmarked affix" to "unclassified affix"
12-Apr-2005	Andy Black	Use window.external to hook JavaScript calls to C# code
03-Feb-2005	Andy Black	Add vernacular font.
01-Feb-2005	Andy Black	Initial Draft
================================================================
-->
