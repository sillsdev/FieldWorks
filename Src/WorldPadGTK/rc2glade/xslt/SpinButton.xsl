<?xml version='1.0' encoding='utf-8'?>
<xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'>

<xsl:import href="copy.xsl" />

<xsl:output method='xml' version='1.0' indent='yes'/>

<xsl:strip-space elements="*" />

<xsl:template match="child[widget/@class = 'GtkEntry']">
	<xsl:choose>
		<xsl:when test="following-sibling::child[1]/widget/@class = 'GtkSpinButton'">
			<xsl:variable name="spin-button" select="following-sibling::child[1]/widget" />
			<child>
				<widget class="GtkSpinButton">
					<xsl:attribute name="id">
						<xsl:value-of select="$spin-button/@id" />
					</xsl:attribute>
					<xsl:copy-of select="widget/property[@name = 'width_request']" />
					<xsl:copy-of select="widget/property[@name = 'height_request']" />
					<xsl:copy-of select="widget/property[@name = 'visible']" />
					<xsl:copy-of select="widget/property[@name = 'sensitive']" />
					<xsl:copy-of select="$spin-button/property[@name = 'climb_rate']" />
					<xsl:copy-of select="$spin-button/property[@name = 'digits']" />
					<xsl:copy-of select="$spin-button/property[@name = 'numeric']" />
					<xsl:copy-of select="$spin-button/property[@name = 'update_policy']" />
					<xsl:copy-of select="$spin-button/property[@name = 'snap_to_ticks']" />
					<xsl:copy-of select="$spin-button/property[@name = 'wrap']" />
					<xsl:copy-of select="$spin-button/property[@name = 'adjustment']" />
				</widget>
				<xsl:copy-of select="packing" />
			</child>
		</xsl:when>
		<xsl:otherwise>
			<child>
				<xsl:apply-templates select="widget" />
				<xsl:apply-templates select="packing" />
			</child>
		</xsl:otherwise>
	</xsl:choose>
</xsl:template>

<xsl:template match="child[widget/@class = 'GtkSpinButton']">
</xsl:template>

<xsl:template match="widget">
	<widget class="{@class}" id="{@id}">
		<xsl:apply-templates select="property" />
		<xsl:apply-templates select="child" />
	</widget>
</xsl:template>

</xsl:stylesheet>