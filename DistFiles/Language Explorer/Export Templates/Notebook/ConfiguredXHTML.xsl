<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="xml" version="1.0" encoding="utf-8" indent="yes"/>

  <!--***************************************************************************************-->
  <!--* This stylesheet works on the output produced by FLEX specifically for XHTML export. *-->
  <!--***************************************************************************************-->

  <!-- strip out the writing system information, since only the stylesheet needs it. -->

  <xsl:template match="WritingSystemInfo"/>

  <!-- Strip all white space and leave it up to the stylesheet text elements below to put in appropriate spacing. -->

  <xsl:strip-space elements="*"/>
  <xsl:preserve-space elements="Run AUni Uni"/><!-- but these spaces are significant! -->

  <!-- insert a comment explaining why there's so little whitespace in the xhtml output. -->

  <xsl:template match="/html">
	<xsl:copy>
	  <xsl:copy-of select="@*"/><xsl:text>&#13;&#10;</xsl:text>
	  <xsl:comment>
There are no spaces or newlines between &lt;span&gt; elements in this file because
whitespace is significant.  We don't want extraneous spaces appearing in the
display/printout!
	  </xsl:comment><xsl:text>&#13;&#10;</xsl:text>
	  <xsl:apply-templates/>
	</xsl:copy>
  </xsl:template>

  <!-- insert the XHTML DOCTYPE declaration before the root element -->

  <xsl:template match="/">
	<xsl:text disable-output-escaping="yes">&#13;&#10;&lt;!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd"&gt;&#13;&#10;</xsl:text>
	<xsl:apply-templates/>
  </xsl:template>

  <!-- insert newlines after <head> and <body> -->

  <xsl:template match="head|body">
	<xsl:copy>
	  <xsl:copy-of select="@*"/><xsl:text>&#13;&#10;</xsl:text>
	  <xsl:apply-templates/>
	</xsl:copy><xsl:text>&#13;&#10;</xsl:text>
  </xsl:template>

  <!-- convert <RnGenericRec id="xyz"> to <div class="entry" id="xyz"> -->

  <xsl:template match="RnGenericRec">
	<div class="entry">
	  <xsl:copy-of select="@*"/><xsl:text>&#13;&#10;</xsl:text>
	  <xsl:apply-templates/><xsl:text>&#13;&#10;</xsl:text>
	</div><xsl:text>&#13;&#10;</xsl:text>
  </xsl:template>

  <!-- ignore some unwanted levels -->


<xsl:template match="Paragraph"><xsl:apply-templates/></xsl:template>
<xsl:template match="StText"><xsl:apply-templates/></xsl:template>
<xsl:template match="StText_Paragraphs"><xsl:apply-templates/></xsl:template>
<xsl:template match="StTxtPara"><xsl:apply-templates/></xsl:template>
<xsl:template match="RnGenericRec_AnthroCodes"><xsl:apply-templates/></xsl:template>
<xsl:template match="RnGenericRec_Conclusions"><xsl:apply-templates/></xsl:template>
<xsl:template match="RnGenericRec_Confidence"><xsl:apply-templates/></xsl:template>
<xsl:template match="RnGenericRec_CounterEvidence"><xsl:apply-templates/></xsl:template>
<xsl:template match="RnGenericRec_Description"><xsl:apply-templates/></xsl:template>
<xsl:template match="RnGenericRec_Discussion"><xsl:apply-templates/></xsl:template>
<xsl:template match="RnGenericRec_ExternalMaterials"><xsl:apply-templates/></xsl:template>
<xsl:template match="RnGenericRec_FurtherQuestions"><xsl:apply-templates/></xsl:template>
<xsl:template match="RnGenericRec_Hypothesis"><xsl:apply-templates/></xsl:template>
<xsl:template match="RnGenericRec_Locations"><xsl:apply-templates/></xsl:template>
<xsl:template match="RnGenericRec_Participants"><xsl:apply-templates/></xsl:template>
<xsl:template match="RnGenericRec_PersonalNotes"><xsl:apply-templates/></xsl:template>
<xsl:template match="RnGenericRec_Researchers"><xsl:apply-templates/></xsl:template>
<xsl:template match="RnGenericRec_Restrictions"><xsl:apply-templates/></xsl:template>
<xsl:template match="RnGenericRec_SeeAlso"><xsl:apply-templates/></xsl:template>
<xsl:template match="RnGenericRec_Self"><xsl:apply-templates/></xsl:template>
<xsl:template match="RnGenericRec_Sources"><xsl:apply-templates/></xsl:template>
<xsl:template match="RnGenericRec_Status"><xsl:apply-templates/></xsl:template>
<xsl:template match="RnGenericRec_SubRecords"><xsl:apply-templates/></xsl:template>
<xsl:template match="RnGenericRec_SupersededBy"><xsl:apply-templates/></xsl:template>
<xsl:template match="RnGenericRec_SupportingEvidence"><xsl:apply-templates/></xsl:template>
<xsl:template match="RnGenericRec_TimeOfEvent"><xsl:apply-templates/></xsl:template>
<xsl:template match="RnGenericRec_Type"><xsl:apply-templates/></xsl:template>
<xsl:template match="RnRoledPartic_Participants"><xsl:apply-templates/></xsl:template>
<xsl:template match="RnRoledPartic_Role"><xsl:apply-templates/></xsl:template>

  <xsl:template match="Integer">
	<xsl:value-of select="@val"/>
  </xsl:template>

  <xsl:template match="LiteralString">
	<xsl:if test="Str/Run[1]/@editable='not' and Str/Run[1]/@namedStyle='Writing System Abbreviation'">
	  <span><xsl:attribute name="class">xlanguagetag</xsl:attribute><xsl:attribute name="lang"><xsl:value-of select="Str/Run[1]/@ws"/></xsl:attribute>
		<xsl:value-of select="Str/Run[1]"/>
	  </span>
	</xsl:if>
  </xsl:template>

  <!-- convert <RnGenericRec_Title> and other string valued elements -->

  <xsl:template match="RnGenericRec_Title|StTxtPara_Contents">
	<xsl:call-template name="ProcessString"></xsl:call-template>
  </xsl:template>


  <xsl:template match="CmPossibilityLink">
	<xsl:choose>
	  <xsl:when test="count(../CmPossibilityLink) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>

  <xsl:template match="CmSemanticDomainLink">
	<xsl:choose>
	  <xsl:when test="count(../CmSemanticDomainLink) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>

  <xsl:template match="CmAnthroItemLink">
	<xsl:choose>
	  <xsl:when test="count(../CmAnthroItemLink) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>

  <xsl:template match="CmPersonLink">
	<xsl:choose>
	  <xsl:when test="count(../CmPersonLink) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>

  <xsl:template match="CmLocationLink">
	<xsl:choose>
	  <xsl:when test="count(../CmLocationLink) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>

  <xsl:template match="RnGenericRecLink">
	<xsl:choose>
	  <xsl:when test="count(../RnGenericRecLink) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>

  <xsl:template match="RnRoledPartic">
	<xsl:choose>
	  <xsl:when test="count(../RnRoledPartic) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>

  <xsl:template match="CmTranslation">
	<xsl:choose>
	  <xsl:when test="count(../CmTranslation) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>


  <xsl:template match="CmPossibility_Abbreviation|CmPossibility_Name">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>

  <!-- convert <RnGenericRec_DateCreated> and <RnGenericRec_DateModified> -->

  <xsl:template match="RnGenericRec_DateCreated|RnGenericRec_DateModified">
	<xsl:call-template name="ProcessString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="RnGenericRec_DateOfEvent">
	<xsl:call-template name="ProcessString"></xsl:call-template>
  </xsl:template>


  <!-- ************************** -->
  <!-- * Process custom fields. * -->
  <!-- ************************** -->

  <xsl:template match="*[@userlabel]">
	<xsl:choose>
	  <xsl:when test="Str/Run">
		<xsl:call-template name="ProcessString"></xsl:call-template>
	  </xsl:when>
	  <xsl:when test="AStr/Run">
		<xsl:call-template name="ProcessMultiString"></xsl:call-template>
	  </xsl:when>
	  <xsl:otherwise>
		<xsl:apply-templates/>
	  </xsl:otherwise>
	</xsl:choose>
  </xsl:template>


  <!-- *************************************************************** -->
  <!-- * Some span classes need to be tagged to reflect their content. -->
  <!-- *************************************************************** -->

  <xsl:template match="span">
	<xsl:copy>
	  <xsl:copy-of select="@title"/>
	  <xsl:choose>
		<xsl:when test="*[@target]/*/AStr/Run">
		  <xsl:call-template name="SetAnalAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="*[@target]/*/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="*/AStr/Run">
		  <xsl:call-template name="SetAnalAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="*/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>

		<xsl:when test="LexEntry_LexemeForm/MoForm/Alternative/MoForm_Form/AStr/Run">
		  <xsl:call-template name="SetVernAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="LexEntry_LexemeForm/MoForm/Alternative/MoForm_Form/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:otherwise>
		  <xsl:copy-of select="@class"/>
		  <!--xsl:attribute name="dir"><xsl:value-of select="/html/WritingSystemInfo[@analTag='']/@dir"/></xsl:attribute-->
		</xsl:otherwise>
	  </xsl:choose>
	  <xsl:apply-templates/>
	</xsl:copy>
  </xsl:template>

  <xsl:template match="Alternative">
	<xsl:choose>
	  <xsl:when test="count(../Alternative) > 1">
		<span class="xitem"><xsl:attribute name="lang"><xsl:value-of select="@ws"/></xsl:attribute><xsl:apply-templates/></span>
	  </xsl:when>
	  <xsl:otherwise>
		<xsl:apply-templates/>
	  </xsl:otherwise>
	</xsl:choose>
  </xsl:template>


  <!-- ***************************************** -->
  <!-- * This is the basic default processing. * -->
  <!-- ***************************************** -->

  <xsl:template match="*">
	<xsl:copy>
	  <xsl:copy-of select="@*"/>
	  <xsl:apply-templates/>
	</xsl:copy>
  </xsl:template>


  <!-- ******************* -->
  <!-- * NAMED TEMPLATES * -->
  <!-- ******************* -->

  <!-- process content that consists of one or more <AStr> elements -->

  <xsl:template name="ProcessMultiString">
	<xsl:for-each select="AStr">
<!--
	  <xsl:choose>
		<xsl:when test="not(@ws=../AStr[1]/@ws)">
		  <span><xsl:attribute name="lang"><xsl:value-of select="@ws"/></xsl:attribute></span>
		  <xsl:call-template name="ProcessRuns"/>
		</xsl:when>
		<xsl:otherwise>
		  <xsl:call-template name="ProcessRuns"/>
		</xsl:otherwise>
	  </xsl:choose>
-->
	  <xsl:call-template name="ProcessRuns"/>
	</xsl:for-each>
	<xsl:for-each select="Alternative">
	  <xsl:for-each select="AStr">
		<span class="xitem"><xsl:attribute name="lang"><xsl:value-of select="@ws"/></xsl:attribute>
		  <xsl:call-template name="ProcessRuns"/>
		</span>
	  </xsl:for-each>
	</xsl:for-each>
  </xsl:template>


  <!-- Process the <Run> elements inside <Str> or <AStr> elements. -->

  <xsl:template name="ProcessRuns">
	<xsl:for-each select="Run">
	  <xsl:variable name="oldStyle">
		  <xsl:if test="preceding-sibling::Run[1]/@namedStyle">
			<xsl:value-of select="preceding-sibling::Run[1]/@namedStyle"/>
		  </xsl:if>
		  <xsl:if test="not(preceding-sibling::Run[1]/@namedStyle)">
			<xsl:text>&#32;</xsl:text>
		  </xsl:if>
	  </xsl:variable>
	  <xsl:variable name="newStyle">
		<xsl:choose>
		  <xsl:when test="@namedStyle">
			<xsl:value-of select="@namedStyle"/>
		  </xsl:when>
		  <xsl:otherwise>
			<xsl:text>&#32;</xsl:text>
		  </xsl:otherwise>
		</xsl:choose>
	  </xsl:variable>
	  <xsl:choose>
		<xsl:when test="position()=1">
		  <xsl:text disable-output-escaping="yes">&lt;span lang="</xsl:text>
		  <xsl:value-of select="@ws"/>
		  <xsl:if test="@namedStyle">
			<xsl:text>" class="</xsl:text><xsl:value-of select="@namedStyle"/>
		  </xsl:if>
		  <xsl:text disable-output-escaping="yes">"&gt;</xsl:text>
		</xsl:when>
		<xsl:when test="@ws != preceding-sibling::Run[1]/@ws">
		  <xsl:text disable-output-escaping="yes">&lt;/span&gt;&lt;span lang="</xsl:text>
		  <xsl:value-of select="@ws"/>
		  <xsl:if test="@namedStyle">
			<xsl:text>" class="</xsl:text><xsl:value-of select="@namedStyle"/>
		  </xsl:if>
		  <xsl:text disable-output-escaping="yes">"&gt;</xsl:text>
		</xsl:when>
		<xsl:when test="$newStyle != $oldStyle">
		  <xsl:text disable-output-escaping="yes">&lt;/span&gt;&lt;span lang="</xsl:text>
		  <xsl:value-of select="@ws"/>
		  <xsl:if test="@namedStyle">
			<xsl:text>" class="</xsl:text><xsl:value-of select="@namedStyle"/>
		  </xsl:if>
		  <xsl:text disable-output-escaping="yes">"&gt;</xsl:text>
		</xsl:when>
	  </xsl:choose>
	  <xsl:value-of select="."/>
	  <xsl:if test="position()=last()">
		<xsl:text disable-output-escaping="yes">&lt;/span&gt;</xsl:text>
	  </xsl:if>
	</xsl:for-each>
  </xsl:template>

  <!-- process content that consists of one or more <AUni> elements -->

  <xsl:template name="ProcessMultiUnicode">
	<xsl:for-each select="AUni">
	  <xsl:value-of select="."/>
	</xsl:for-each>
	<xsl:for-each select="Alternative">
	  <xsl:for-each select="AUni">
		<span class="xitem"><xsl:attribute name="lang"><xsl:value-of select="@ws"/></xsl:attribute>
		  <xsl:value-of select="."/>
		</span>
	  </xsl:for-each>
	</xsl:for-each>
  </xsl:template>


  <!-- Process content that consists of a <Str> element. -->
  <!-- This converts each Run into a span if the ws or namedStyle changes. -->

  <xsl:template name="ProcessString">
	<xsl:for-each select="Str">
	  <xsl:call-template name="ProcessRuns"/>
	</xsl:for-each>
  </xsl:template>

  <!-- convert all \ characters to / -->

  <xsl:template name="FixSlashes">
	<xsl:param name="text"/>
	<xsl:choose>
	  <xsl:when test="contains($text, '\')">
		<xsl:value-of select="substring-before($text, '\')"/>
		<xsl:value-of select="'/'"/>
		<xsl:call-template name="FixSlashes">
		  <xsl:with-param name="text" select="substring-after($text, '\')"/>
		</xsl:call-template>
	  </xsl:when>
	  <xsl:otherwise>
		<xsl:value-of select="$text"/>
	  </xsl:otherwise>
	</xsl:choose>
  </xsl:template>

  <!-- tag the class name for a vernacular language -->

  <xsl:template name="SetVernAttrs">
	<xsl:param name="Class"/>
	<xsl:param name="Lang"/>
	<xsl:attribute name="class">
	  <xsl:value-of select="$Class"/>
	  <xsl:value-of select="/html/WritingSystemInfo[@lang=$Lang]/@vernTag"/>
	</xsl:attribute>
	<xsl:attribute name="lang"><xsl:value-of select="$Lang"/></xsl:attribute>
	<!--xsl:attribute name="dir"><xsl:value-of select="/html/WritingSystemInfo[@lang=$Lang]/@dir"/></xsl:attribute-->
  </xsl:template>

  <!-- tag the class name for an analysis language -->

  <xsl:template name="SetAnalAttrs">
	<xsl:param name="Class"/>
	<xsl:param name="Lang"/>
	<xsl:attribute name="class">
	  <xsl:value-of select="$Class"/>
	  <xsl:value-of select="/html/WritingSystemInfo[@lang=$Lang]/@analTag"/>
	</xsl:attribute>
	<xsl:attribute name="lang"><xsl:value-of select="$Lang"/></xsl:attribute>
	<!--xsl:attribute name="dir"><xsl:value-of select="/html/WritingSystemInfo[@lang=$Lang]/@dir"/></xsl:attribute-->
  </xsl:template>

</xsl:stylesheet>
