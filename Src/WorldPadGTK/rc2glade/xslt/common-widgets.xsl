<?xml version='1.0' encoding='utf-8'?>
<xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'>

<xsl:import href="copy.xsl" />

<xsl:output method="xml" version="1.0" standalone="no" indent="yes"
	doctype-system="http://glade.gnome.org/glade-2.0.dtd"/>

<xsl:template match="widget[@id = 'kridFmtBdrDlgP']//widget[@id = 'fixed00']">
	<widget class="{@class}" id="borderWidgets">
		<xsl:apply-templates/>
	</widget>
</xsl:template>

<xsl:template match="widget[@id = 'kridFmtBulNumDlg']//widget[@id = 'fixed00']">
	<widget class="{@class}" id="bulNumWidgets">
		<xsl:apply-templates/>
	</widget>
</xsl:template>

<!--xsl:template match="widget[@id = 'kridFmtFntDlg']//widget[@id = 'fixed00']">
	<widget class="{@class}" id="fontWidgets">
		<xsl:apply-templates/>
	</widget>
</xsl:template-->

<xsl:template match="widget[@id = 'kridFmtParaDlgRtl']//widget[@id = 'fixed00']">
	<widget class="{@class}" id="paraWidgets">
		<xsl:apply-templates/>
	</widget>
</xsl:template>

<xsl:template match="widget[@id = 'kctidTabDlgs']/child">
	<child>
		<xsl:choose>
			<!--xsl:when test="count(preceding-sibling::child) = 2">
				<xsl:call-template name="create-widget-site">
					<xsl:with-param name="site-name" select="'fontTabSite'"/>
				</xsl:call-template>
			</xsl:when-->
			<xsl:when test="count(preceding-sibling::child) = 4">
				<xsl:call-template name="create-widget-site">
					<xsl:with-param name="site-name" select="'paraTabSite'"/>
				</xsl:call-template>
			</xsl:when>
			<xsl:when test="count(preceding-sibling::child) = 6">
				<xsl:call-template name="create-widget-site">
					<xsl:with-param name="site-name" select="'bulNumTabSite'"/>
				</xsl:call-template>
			</xsl:when>
			<xsl:when test="count(preceding-sibling::child) = 8">
				<xsl:call-template name="create-widget-site">
					<xsl:with-param name="site-name" select="'borderTabSite'"/>
				</xsl:call-template>
			</xsl:when>
			<xsl:otherwise>
				<xsl:apply-templates/>
			</xsl:otherwise>
		</xsl:choose>
	</child>
</xsl:template>

<xsl:template name="create-widget-site">
<xsl:param name="site-name"/>
	<widget class="GtkVBox" id="{$site-name}">
		<property name="visible">True</property>
		<property name="homogeneous">False</property>
		<property name="spacing">0</property>
		<child>
			<placeholder/>
		</child>
	</widget>
	<packing>
		<property name="tab_expand">False</property>
		<property name="tab_fill">True</property>
	</packing>
</xsl:template>

</xsl:stylesheet>
