<?xml version="1.0" encoding="UTF-8"?>
<!--This XSL is the first of 4 in a series to export FieldWorks data to RTF.
** FwExport2RTF1 - Strips extra information and outputs standard WorldPad format.
FwExport2RTF1b - Creates unique integer ids for Styles and Fonts so that they can be more easily referenced when creating the RTF.
FwExport2RTF2a - Adds style information locally to the paragraph particularly for bulleted or numbered paragraphs to facilitate creation of RTF codes.
FwExport2RTF2b - Creates RTF codes.

It highly recommended that you use an XML viewer to review or edit these files in order to easily delineate commented code from actual code.
-->
<xsl:stylesheet version="1.0"
 xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
 xmlns:silfw="http://fieldworks.sil.org/2002/silfw/Codes"
 exclude-result-prefixes="silfw">
<silfw:file
 title="Rich Text Format - MS Word (*.rtf)"
 outputext="rtf"
 description="Converts FwExport format to WorldPad format"
 views="document"
 chain="FwExport2RTF1b.xsl"
/>
<!-- This transform simply strips out the element tags added to facilitate generic export. -->
<xsl:output method="xml" version="1.0" encoding="UTF-8"/>
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
