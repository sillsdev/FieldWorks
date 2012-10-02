<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

<xsl:import href="copy.xsl" />

<xsl:output method="xml" version="1.0" standalone="no" indent="yes"
	doctype-system="http://glade.gnome.org/glade-2.0.dtd"/>

<!--xsl:strip-space elements="*" /-->

<xsl:template match="/">
	<glade-interface>
		<xsl:for-each select="/merger/def">
			<xsl:apply-templates select="document(@filename)/glade-interface/widget" />
		</xsl:for-each>
	</glade-interface>
</xsl:template>

<xsl:template match="widget[@id = 'window1']">
</xsl:template>

</xsl:stylesheet>
