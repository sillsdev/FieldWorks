<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
   <!-- Clitics -->
   <xsl:variable name="sCliticPOS" select="'CliticPOS'"/>
   <!-- CFP = CliticFromPOS, but PC-PATR has an 80 character limit for template names, so we're using a shorter version -->
   <xsl:variable name="sCliticFromPOS" select="'CFP'"/>
   <!-- Exception features -->
   <xsl:variable name="sDefaultExceptionFeatures" select="'DefaultExcpFeatures'"/>
   <xsl:variable name="sExceptionFeature" select="'ExcpFeat'"/>
   <xsl:variable name="sFromExceptionFeature" select="'FromExcpFeat'"/>
   <xsl:variable name="sToExceptionFeature" select="'ToExcpFeat'"/>
<!-- Inflection class -->
   <xsl:variable name="sInflClass" select="'InflClass'"/>
   <xsl:variable name="sFromInflClass" select="'FromInflClass'"/>
   <xsl:variable name="sToInflClass" select="'ToInflClass'"/>
   <!-- Part of Speech -->
   <xsl:variable name="sFromPOS" select="'FromPOS'"/>
   <xsl:variable name="sToPOS" select="'ToPOS'"/>
   <xsl:variable name="sStemName" select="'StemName'"/>
   <xsl:variable name="sRootPOS" select="'RootPOS'"/>
   <!-- morpho syntactic environments and features -->
   <xsl:variable name="sMSEnvPOS" select="'MSEnvPOS'"/>
   <xsl:variable name="sMSEnvFS" select="'MSEnvFS'"/>
   <xsl:variable name="sMSFS" select="'MSFS'"/>
   <xsl:variable name="sFromMSFS" select="'FromMSFS'"/>
   <xsl:variable name="sToMSFS" select="'ToMSFS'"/>
   <xsl:variable name="sInflectionFS" select="'InflectionFS'"/>
   <xsl:variable name="sIrregularlyInflectedForm" select="'IrregInflForm'"/>
	<!-- ICA = InflClassAffix (inflectional class affix), but we need to avoid PC-PATR's 80 character limit -->
	<xsl:variable name="sInflClassAffix" select="'ICA'"/>
</xsl:stylesheet>
