<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:include href="ConfiguredXHTML.xsl"/>
  <xsl:output method="xml" version="1.0" encoding="utf-8" indent="yes"/>
  <!--***************************************************************************************-->
  <!--* This stylesheet works on the output produced by FLEX specifically for XHTML export. *-->
  <!--***************************************************************************************-->

  <!-- eliminate unneeded levels in the original markup -->
  <xsl:template match="ReversalIndexEntry_Hvo">
	<xsl:apply-templates/>
  </xsl:template>

  <!-- override the div subentries from the ConfiguredXHTML so that sub entries of the reversals aren't eaten -->
  <xsl:template match="div[@class='subentries']">
	<xsl:copy>
	  <xsl:copy-of select="@*"/>
		<xsl:if test="not(@class='letter')"><xsl:text>&#13;&#10;</xsl:text></xsl:if>
	  <xsl:apply-templates/>
	</xsl:copy><xsl:text>&#13;&#10;</xsl:text>
  </xsl:template>

  <xsl:template match="ReversalIndexEntry_Self|LexSenseLink_VariantFormEntryBackRefs">
	<xsl:apply-templates/>
  </xsl:template>

  <!-- change ReversalIndexEntry into div with the proper class attribute value -->
  <xsl:template match="ReversalIndexEntry">
	<xsl:choose>
		<xsl:when test="parent::div[@class='letData']">
		  <div class="entry"><xsl:apply-templates/></div>
		</xsl:when>
		<xsl:when test="ancestor::ReversalIndexEntry_Subentries">
		  <div class="subentry"><xsl:apply-templates/></div>
		</xsl:when>
		<xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
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

  <!-- ***************************************** -->
  <!-- * This is the basic default processing. * -->
  <!-- ***************************************** -->

<xsl:template match="_ReferringSenses">
	<xsl:apply-templates/>
</xsl:template>

<!-- This template will avoid the anchor links being put in -->
<xsl:template match="_ReferringSenses//LexSenseLink|ReversalIndexEntry_ReferringSenses//LexSenseLink">
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
