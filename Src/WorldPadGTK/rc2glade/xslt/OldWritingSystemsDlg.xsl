<?xml version='1.0' encoding='utf-8'?>
<xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'>

<xsl:import href="copy.xsl" />

<xsl:output method='xml' version='1.0' indent='yes'/>

<xsl:strip-space elements="*" />

<xsl:template match="widget[@class = 'GtkEntry'][ancestor::widget[@id = 'kridOldWritingSystemsDlg']]">
	<widget class="{@class}" id="{@id}">
		<xsl:apply-templates select="property" />
		<signal name="changed">
			<xsl:attribute name="handler">
				<xsl:value-of select="concat('on_', @id, '_changed')"/>
			</xsl:attribute>
		</signal>
	</widget>
</xsl:template>

</xsl:stylesheet>
