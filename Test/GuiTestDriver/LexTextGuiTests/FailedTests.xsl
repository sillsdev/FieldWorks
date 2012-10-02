<?xml version="1.0" encoding="UTF-8"?>
<?xml-stylesheet type="text/xsl" href="FailedTests.xsl"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
xmlns:ms="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="ms">
<xsl:output method="html" encoding="UTF-8" indent="yes" />

<xsl:variable name="TestLogs" >
	<log file="abPres1.xlg"/>
	<log file="abPres2.xlg"/>
	<log file="abPres3.xlg"/>
	<log file="abPres4.xlg"/>
	<log file="abPres5.xlg"/>
	<log file="cipPart1.xlg"/>
	<log file="cipPart2.xlg"/>
	<log file="cipPart3.xlg"/>
	<log file="cipPart4.xlg"/>
	<log file="cipPart5.xlg"/>
	<log file="cipPart6.xlg"/>
	<log file="cipPart7.xlg"/>
	<log file="Demo.xlg"/>
	<log file="DemoD.xlg"/>
	<log file="DemoF.xlg"/>
	<log file="DemoG.xlg"/>
	<log file="DemoH.xlg"/>
	<log file="DemoI.xlg"/>
	<log file="DemoJ.xlg"/>
	<log file="DemoK.xlg"/>
	<log file="gmaVisitViews.xlg"/>
	<log file="gmCatEd.xlg"/>
	<log file="gmdAddDelNatClass.xlg"/>
	<log file="gmEFinsDel.xlg"/>
	<log file="gmFeaInsDel.xlg"/>
	<log file="gmhCategories.xlg"/>
	<log file="gmmCatBrowseFilter.xlg"/>
	<log file="gmPhonemes.xlg"/>
	<log file="gmrLT_2693.xlg"/>
	<log file="liaVisitViews.xlg"/>
	<log file="liFTinsDel.xlg"/>
	<log file="limAddDelItem.xlg"/>
	<log file="liqEditLists.xlg"/>
	<log file="lxaaVisitViews.xlg"/>
	<log file="lxabDataNav.xlg"/>
	<log file="lxacShowAllFields.xlg"/>
	<log file="lxBEentries.xlg"/>
	<log file="lxBkCopy.xlg"/>
	<log file="lxCatEntry.xlg"/>
	<log file="lxChangeStat.xlg"/>
	<log file="lxCkCopy.xlg"/>
	<log file="lxClassDict.xlg"/>
	<log file="lxConfDict.xlg"/>
	<log file="lxdBulkEdit.xlg"/>
	<log file="lxDictLex.xlg"/>
	<log file="lxeBulkReplace.xlg"/>
	<log file="lxfaRegExp.xlg"/>
	<log file="lxfREBulkReplace.xlg"/>
	<log file="lxgCombExp.xlg"/>
	<log file="lxShortcuts.xlg"/>
	<log file="lxuLT_2882.xlg"/>
	<log file="lxzFindExample.xlg"/>
	<log file="paaSounds.xlg"/>
	<log file="pabHyperlink.xlg"/>
	<log file="pmeVernacularWsSwap.xlg"/>
	<log file="pmgAddDelStyle.xlg"/>
	<log file="pmgEditFindUndo.xlg"/>
	<log file="pmgExtLink2Dn.xlg"/>
	<log file="pmgHelp.xlg"/>
	<log file="pmgLT_2602.xlg"/>
	<log file="pmgProp.xlg"/>
	<log file="pmgShortcuts.xlg"/>
	<log file="pmgStartTE.xlg"/>
	<log file="pmpNewProject.xlg"/>
	<log file="pmzImport.xlg"/>
	<log file="RestoreKalaba.xlg"/>
	<log file="txaVisitViews.xlg"/>
	<log file="txEdit.xlg"/>
	<log file="txSortLxGm.xlg"/>
	<log file="txtShortcuts.xlg"/>
	<log file="wdAnaStat.xlg"/>
	<log file="wdaVisitViews.xlg"/>
	<log file="wdbAnalysisCols.xlg"/>
	<log file="wdcAnalysisChecks.xlg"/>
	<log file="wdConcDic.xlg"/>
	<log file="wdInsDelGloss.xlg"/>
	<log file="wdShortcuts.xlg"/>
	<log file="wdSStat.xlg"/>
	<log file="wdwritSysDia.xlg"/>
	<log file="wdzAssignAnalysis.xlg"/>
</xsl:variable>
<xsl:variable name="AllLogs" select="document(ms:node-set($TestLogs)/log/@file)"/>
<xsl:variable name="FailedLogs" select="$AllLogs/gtdLog[assertion]"/>

<xsl:template match="/">
<html>
<head>
<title>Failed Tests</title>
</head>
<body>

 <h1>Failed Tests</h1>
 <table border="1">
   <xsl:apply-templates select="$FailedLogs"/>
 </table>

</body>
</html>
</xsl:template>

<xsl:template match="gtdLog">
  <xsl:variable name="fileName" select="substring-before(set-up/script/@name,'.')"/>
  <tr><td><a href="{$fileName}.xlg"><xsl:value-of select="$fileName"/></a></td></tr>
</xsl:template>


</xsl:stylesheet>
