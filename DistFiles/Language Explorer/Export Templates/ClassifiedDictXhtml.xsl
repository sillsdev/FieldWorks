<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:include href="ConfiguredXHTML.xsl"/>
  <xsl:output method="xml" version="1.0" encoding="utf-8" indent="yes"/>
	<!--***************************************************************************************-->
	<!--* This stylesheet works on the output produced by FLEX specifically for XHTML export.*-->
	<!--* It modifies the stylesheet used for regular dictionaries. Many elements are the same. *-->
	<!--***************************************************************************************-->

  <!-- eliminate unneeded levels in the original markup -->
  <xsl:template match="CmSemanticDomain|CmSemanticDomain_ReferringSenses">
	<xsl:apply-templates/>
  </xsl:template>

  <!-- Preserve paragraphs which are under CmSemanticDomain_ReferringSenses/LexSenseLink as divs. These will be senses configured as paragraphs
  with a (possibly custom) style. The code which generates our stylesheet replaces spaces in style names with underscore,
  as required for CSS, so we do the same here.
  Similarly preserve the paragraphs for the main semantic domains.-->
	<xsl:template match="CmSemanticDomain_ReferringSenses/LexSenseLink/Paragraph|CmSemanticDomain/Paragraph">
		<div>
			<xsl:attribute name="class"><xsl:value-of select="translate(@style, ' ', '_')"/></xsl:attribute>
			<xsl:apply-templates/>
		</div>
	</xsl:template>

  <!-- ****************************************** -->
  <!-- process multistring (AStr) owning elements -->
  <!-- ****************************************** -->

  <!-- process LexEntry_LexemeForm -->


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

<!-- This template will avoid the main entries coming out as hot links -->
<xsl:template match="CmSemanticDomain_ReferringSenses//LexSenseLink">
	<xsl:apply-templates/>
</xsl:template>

  <xsl:template match="*">
	<xsl:copy>
	  <xsl:copy-of select="@*"/>
	  <xsl:apply-templates/>
	</xsl:copy>
  </xsl:template>

</xsl:stylesheet>
