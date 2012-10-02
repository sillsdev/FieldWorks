<?xml version='1.0' encoding='utf-8'?>
<xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'>

<xsl:import href="copy.xsl" />

<xsl:output method='xml' version='1.0' indent='yes'/>

<xsl:strip-space elements="*" />

<xsl:template match="widget[@id = 'kridDeleteWs']">
	<widget class="{@class}" id="{@id}">
		<xsl:apply-templates select="property" />
		<child internal-child="vbox">			<widget class="GtkVBox" id="dialog-vbox2">				<property name="visible">True</property>				<property name="homogeneous">False</property>				<property name="spacing">0</property>				<child internal-child="action_area">					<widget class="GtkHButtonBox" id="dialog-action_area2">						<property name="visible">True</property>						<property name="layout_style">GTK_BUTTONBOX_END</property>						<child>							<widget class="GtkButton" id="kctidCancel">								<property name="visible">True</property>								<property name="can_default">True</property>								<property name="can_focus">True</property>								<property name="label">gtk-cancel</property>								<property name="use_stock">True</property>								<property name="relief">GTK_RELIEF_NORMAL</property>								<property name="focus_on_click">True</property>								<property name="response_id">-6</property>							</widget>						</child>						<child>							<widget class="GtkButton" id="kctidOk">								<property name="visible">True</property>								<property name="can_default">True</property>								<property name="can_focus">True</property>								<property name="label">gtk-ok</property>								<property name="use_stock">True</property>								<property name="relief">GTK_RELIEF_NORMAL</property>								<property name="focus_on_click">True</property>								<property name="response_id">-5</property>							</widget>						</child>					</widget>					<packing>						<property name="padding">0</property>						<property name="expand">False</property>						<property name="fill">True</property>						<property name="pack_type">GTK_PACK_END</property>					</packing>				</child>
				<child>
					<widget class="{child/widget/@class}" id="{child/widget/@id}">
						<xsl:apply-templates select="child/widget/property" />
						<xsl:apply-templates select="child/widget/child" />
					</widget>
					<packing>	  					<property name="padding">0</property>	  					<property name="expand">True</property>	  					<property name="fill">True</property>					</packing>
				</child>			</widget>
		</child>
	</widget>
</xsl:template>

<xsl:template match="child[widget/@id = 'kctidOk']">
	<xsl:if test="not(ancestor::widget[@id = 'kridDeleteWs'])">
		<xsl:copy-of select="." />
	</xsl:if>
</xsl:template>

<xsl:template match="child[widget/@id = 'kctidCancel']">
	<xsl:if test="not(ancestor::widget[@id = 'kridDeleteWs'])">
		<xsl:copy-of select="." />
	</xsl:if>
</xsl:template>

</xsl:stylesheet>
