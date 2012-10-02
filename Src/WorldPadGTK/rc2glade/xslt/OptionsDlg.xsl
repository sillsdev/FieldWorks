<?xml version='1.0' encoding='utf-8'?>
<xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'>

<xsl:import href="copy.xsl" />

<xsl:output method='xml' version='1.0' indent='yes'/>

<xsl:strip-space elements="*" />

<xsl:template match="widget[@id = 'kctidArrLog']">
	<xsl:choose>
		<xsl:when test="ancestor::widget[@id = 'kridOptionsDlg']">
			<widget class="{@class}" id="{@id}">
				<xsl:apply-templates select="property" />
				<property name="group">
					<xsl:text>kctidArrVis</xsl:text>
				</property>
			</widget>
		</xsl:when>
		<xsl:otherwise>
			<xsl:copy-of select="." />
		</xsl:otherwise>
	</xsl:choose>
</xsl:template>

</xsl:stylesheet>
