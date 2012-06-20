<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:include href="ConfiguredXHTML.xsl"/>
  <xsl:output method="xml" version="1.0" encoding="utf-8" indent="yes"/>
  <!--***************************************************************************************-->
  <!--* This stylesheet works on the output produced by FLEX specifically for XHTML export. *-->
  <!--***************************************************************************************-->

  <!-- strip out the writing system information, since only the stylesheet needs it. -->

  <xsl:template match="WritingSystemInfo"/>

  <!-- Strip all white space and leave it up to the stylesheet text elements below to put in appropriate spacing. -->

  <xsl:strip-space elements="*"/>
  <xsl:preserve-space elements="Run"/><!-- but these spaces are significant! -->

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

  <!-- eliminate unneeded levels in the original markup -->

  <xsl:template match="ReversalIndexEntry_Hvo|ReversalIndexEntry_ReversalForm">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="ReversalIndexEntry_Self|LexSenseLink_VariantFormEntryBackRefs">
	<xsl:apply-templates/>
  </xsl:template>

  <!-- change ReversalIndexEntry into div with the proper class attribute value -->
  <xsl:template match="ReversalIndexEntry">
	<xsl:if test="parent::div[@class='letData']">
	  <div class="entry"><xsl:apply-templates/></div><xsl:text>&#13;&#10;</xsl:text>
	</xsl:if>
	<xsl:if test="parent::ReversalIndexEntry_Subentries">
	  <div class="subentry"><xsl:apply-templates/></div><xsl:text>&#13;&#10;</xsl:text>
	</xsl:if>
  </xsl:template>

  <!-- ****************************************** -->
  <!-- process multistring (AStr) owning elements -->
  <!-- ****************************************** -->

  <xsl:template match="ReversalIndexEntry_ReversalForm">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>

  <!-- process LexEntry_LexemeForm -->

  <xsl:template match="LexSenseLink_OwningEntry">
	 <xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="LexSenseLink_ReversalName">
	<xsl:if test="Str/Run">
	  <xsl:call-template name="ProcessString"></xsl:call-template>
	</xsl:if>
	<xsl:if test="AStr/Run">
	  <xsl:call-template name="ProcessMultiString"></xsl:call-template>
	</xsl:if><xsl:text>&#13;&#10;</xsl:text>
  </xsl:template>

  <xsl:template match="span[@class='headref']/LiteralString">
	<xsl:if test="Str/Run">
	  <xsl:call-template name="ProcessString"></xsl:call-template>
	</xsl:if>
	<xsl:if test="AStr/Run">
	  <xsl:call-template name="ProcessMultiString"></xsl:call-template>
	</xsl:if><xsl:text>&#13;&#10;</xsl:text>
  </xsl:template>

  <!-- ***************************************** -->
  <!-- * This is the basic default processing. * -->
  <!-- ***************************************** -->

<xsl:template match="_ReferringSenses">
	<xsl:apply-templates/>
</xsl:template>

  <xsl:template match="*">
	<xsl:copy>
	  <xsl:copy-of select="@*"/>
	  <xsl:apply-templates/>
	</xsl:copy>
  </xsl:template>


  <!-- ******************* -->
  <!-- * NAMED TEMPLATES * -->
  <!-- ******************* -->

<xsl:template name="DebugOutput">
  <xsl:param name="ws"/>
  <xsl:comment>ws="<xsl:value-of select="$ws"/>"</xsl:comment>
</xsl:template>

</xsl:stylesheet>
