<?xml version='1.0' encoding='utf-8'?>
<xsl:stylesheet version='1.0'
	xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
	xmlns:xsl='http://www.w3.org/1999/XSL/Transform'
	exclude-result-prefixes='xsi'>

<xsl:import href="copy.xsl" />

<xsl:output method='xml' version='1.0' indent='yes'/>

<xsl:strip-space elements="*"/>

<!-- Remove all attributes defined in the xsi namespace -->
<xsl:template match="@xsi:*"/>

<xsl:template match="widget[@id = 'kridAfFindDlg']"/>

<xsl:template match="widget[@id = 'kridAfReplaceDlg']"/>

<xsl:template match="widget[@id = 'kridDatePickDlg']"/>

<xsl:template match="widget[@id = 'kridDbCrawlerProgress']"/>

<xsl:template match="widget[@id = 'kridDelAndChgStylesWarningDlg']"/>

<xsl:template match="widget[@id = 'kridDeleteWs']"/>

<xsl:template match="widget[@id = 'kridDocDlg']"/>

<xsl:template match="widget[@id = 'kridEmptyReplaceDlg']"/>

<xsl:template match="widget[@id = 'kridFmtBdrDlgT']"/>

<xsl:template match="widget[@id = 'kridFmtParaDlg']"/>

<xsl:template match="widget[@id = 'kridNewWs']"/>

<xsl:template match="widget[@id = 'kridPrintCancelDlg']"/>

<xsl:template match="widget[@id = 'kridProgressDlg']"/>

<xsl:template match="widget[@id = 'kridProgressWithCancelDlg']"/>

<xsl:template match="widget[@id = 'kridRemFmtDlg']"/>

<xsl:template match="widget[@id = 'kridSavePlainTextDlg']"/>

<xsl:template match="widget[@id = 'kridShellDlg']"/>

</xsl:stylesheet>
