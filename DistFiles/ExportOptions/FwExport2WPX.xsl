<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0"
 xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
 xmlns:silfw="http://fieldworks.sil.org/2002/silfw/Codes"
 exclude-result-prefixes="silfw">
<silfw:file
 title="XML Format - WorldPad (*.wpx)"
 outputext="wpx"
 description="Converts FwExport format to WorldPad format"
 views="document"
/>
<!-- This transform simply strips out the element tags added to facilitate generic export. -->
<xsl:output method="xml" version="1.0" encoding="UTF-8" doctype-system="WorldPad.dtd"/>
<xsl:template match="/">
<xsl:apply-templates/>
</xsl:template>
<xsl:template match="Item">
<xsl:apply-templates/>
</xsl:template>
<xsl:template match="Field">
<xsl:apply-templates/>
</xsl:template>
<xsl:template match="Entry">
<xsl:apply-templates/>
</xsl:template>
<xsl:template match="@* | node()">
<xsl:copy>
<xsl:apply-templates select="@* | node()"/>
</xsl:copy>
</xsl:template>
</xsl:stylesheet>
