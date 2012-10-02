<?xml version='1.0' encoding='utf-8'?>
<xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'>

<xsl:import href="copy.xsl" />

<xsl:output method='xml' version='1.0' indent='yes'/>

<xsl:strip-space elements="*" />

<xsl:template match="widget[@id = 'kridDelAndChgStylesWarningDlg']">
	<widget class="{@class}" id="{@id}">
		<xsl:apply-templates select="property" />
		<child internal-child="vbox">			<widget class="GtkVBox" id="dialog-vbox2">				<property name="visible">True</property>				<property name="homogeneous">False</property>				<property name="spacing">0</property>				<child internal-child="action_area">					<widget class="GtkHButtonBox" id="dialog-action_area2">						<property name="visible">True</property>						<property name="layout_style">GTK_BUTTONBOX_END</property>	  <child>
		<widget class="GtkButton" id="kctidHelp">
		  <property name="visible">True</property>
		  <property name="can_default">True</property>
		  <property name="can_focus">True</property>
		  <property name="label">gtk-help</property>
		  <property name="use_stock">True</property>
		  <property name="relief">GTK_RELIEF_NORMAL</property>
		  <property name="focus_on_click">True</property>
		  <property name="response_id">-11</property>
		</widget>
	  </child>

	  <child>
		<widget class="GtkButton" id="kctidCancel">
		  <property name="visible">True</property>
		  <property name="can_default">True</property>
		  <property name="can_focus">True</property>
		  <property name="relief">GTK_RELIEF_NORMAL</property>
		  <property name="focus_on_click">True</property>
		  <property name="response_id">-9</property>

		  <child>
		<widget class="GtkAlignment" id="alignment5">
		  <property name="visible">True</property>
		  <property name="xalign">0.5</property>
		  <property name="yalign">0.5</property>
		  <property name="xscale">0</property>
		  <property name="yscale">0</property>
		  <property name="top_padding">0</property>
		  <property name="bottom_padding">0</property>
		  <property name="left_padding">0</property>
		  <property name="right_padding">0</property>

		  <child>
			<widget class="GtkHBox" id="hbox3">
			  <property name="visible">True</property>
			  <property name="homogeneous">False</property>
			  <property name="spacing">2</property>

			  <child>
			<widget class="GtkImage" id="image7">
			  <property name="visible">True</property>
			  <property name="stock">gtk-cancel</property>
			  <property name="icon_size">4</property>
			  <property name="xalign">0.5</property>
			  <property name="yalign">0.5</property>
			  <property name="xpad">0</property>
			  <property name="ypad">0</property>
			</widget>
			<packing>
			  <property name="padding">0</property>
			  <property name="expand">False</property>
			  <property name="fill">False</property>
			</packing>
			  </child>

			  <child>
			<widget class="GtkLabel" id="label9">
			  <property name="visible">True</property>
			  <property name="label" translatable="yes">_No</property>
			  <property name="use_underline">True</property>
			  <property name="use_markup">False</property>
			  <property name="justify">GTK_JUSTIFY_LEFT</property>
			  <property name="wrap">False</property>
			  <property name="selectable">False</property>
			  <property name="xalign">0.5</property>
			  <property name="yalign">0.5</property>
			  <property name="xpad">0</property>
			  <property name="ypad">0</property>
			  <property name="ellipsize">PANGO_ELLIPSIZE_NONE</property>
			  <property name="width_chars">-1</property>
			  <property name="single_line_mode">False</property>
			  <property name="angle">0</property>
			</widget>
			<packing>
			  <property name="padding">0</property>
			  <property name="expand">False</property>
			  <property name="fill">False</property>
			</packing>
			  </child>
			</widget>
		  </child>
		</widget>
		  </child>
		</widget>
	  </child>

	  <child>
		<widget class="GtkButton" id="kctidOk">
		  <property name="visible">True</property>
		  <property name="can_default">True</property>
		  <property name="has_default">True</property>
		  <property name="can_focus">True</property>
		  <property name="relief">GTK_RELIEF_NORMAL</property>
		  <property name="focus_on_click">True</property>
		  <property name="response_id">-8</property>

		  <child>
		<widget class="GtkAlignment" id="alignment6">
		  <property name="visible">True</property>
		  <property name="xalign">0.5</property>
		  <property name="yalign">0.5</property>
		  <property name="xscale">0</property>
		  <property name="yscale">0</property>
		  <property name="top_padding">0</property>
		  <property name="bottom_padding">0</property>
		  <property name="left_padding">0</property>
		  <property name="right_padding">0</property>

		  <child>
			<widget class="GtkHBox" id="hbox4">
			  <property name="visible">True</property>
			  <property name="homogeneous">False</property>
			  <property name="spacing">2</property>

			  <child>
			<widget class="GtkImage" id="image8">
			  <property name="visible">True</property>
			  <property name="stock">gtk-ok</property>
			  <property name="icon_size">4</property>
			  <property name="xalign">0.5</property>
			  <property name="yalign">0.5</property>
			  <property name="xpad">0</property>
			  <property name="ypad">0</property>
			</widget>
			<packing>
			  <property name="padding">0</property>
			  <property name="expand">False</property>
			  <property name="fill">False</property>
			</packing>
			  </child>

			  <child>
			<widget class="GtkLabel" id="label10">
			  <property name="visible">True</property>
			  <property name="label" translatable="yes">_Yes</property>
			  <property name="use_underline">True</property>
			  <property name="use_markup">False</property>
			  <property name="justify">GTK_JUSTIFY_LEFT</property>
			  <property name="wrap">False</property>
			  <property name="selectable">False</property>
			  <property name="xalign">0.5</property>
			  <property name="yalign">0.5</property>
			  <property name="xpad">0</property>
			  <property name="ypad">0</property>
			  <property name="ellipsize">PANGO_ELLIPSIZE_NONE</property>
			  <property name="width_chars">-1</property>
			  <property name="single_line_mode">False</property>
			  <property name="angle">0</property>
			</widget>
			<packing>
			  <property name="padding">0</property>
			  <property name="expand">False</property>
			  <property name="fill">False</property>
			</packing>
			  </child>
			</widget>
		  </child>
		</widget>
		  </child>
		</widget>
	  </child>
					</widget>					<packing>						<property name="padding">0</property>						<property name="expand">False</property>						<property name="fill">True</property>						<property name="pack_type">GTK_PACK_END</property>					</packing>				</child>
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
	<xsl:if test="not(ancestor::widget[@id = 'kridDelAndChgStylesWarningDlg'])">
		<xsl:copy-of select="." />
	</xsl:if>
</xsl:template>

<xsl:template match="child[widget/@id = 'kctidCancel']">
	<xsl:if test="not(ancestor::widget[@id = 'kridDelAndChgStylesWarningDlg'])">
		<xsl:copy-of select="." />
	</xsl:if>
</xsl:template>

<xsl:template match="child[widget/@id = 'kctidHelp']">
	<xsl:if test="not(ancestor::widget[@id = 'kridDelAndChgStylesWarningDlg'])">
		<xsl:copy-of select="." />
	</xsl:if>
</xsl:template>

</xsl:stylesheet>
