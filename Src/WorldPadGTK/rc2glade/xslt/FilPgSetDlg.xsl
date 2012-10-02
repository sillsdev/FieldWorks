<?xml version='1.0' encoding='utf-8'?>
<xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'>

<xsl:import href="copy.xsl" />

<xsl:output method='xml' version='1.0' indent='yes'/>

<xsl:strip-space elements="*" />

<xsl:template match="widget[@id = 'kridFilPgSetDlg']">
	<widget class="{@class}" id="{@id}">
		<xsl:apply-templates select="property" />
		<signal name="close" handler="on_dialog_close" last_modification_time="Fri, 15 Sep 2006 20:52:53 GMT"/>
		<signal name="response" handler="on_dialog_response" last_modification_time="Fri, 15 Sep 2006 20:53:02 GMT"/>
		<signal name="delete_event" handler="on_dialog_delete_event" last_modification_time="Fri, 15 Sep 2006 20:53:39 GMT"/>
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
		  <signal name="clicked" handler="on_kctidHelp_clicked" last_modification_time="Thu, 07 Sep 2006 21:39:25 GMT"/>
		</widget>
	  </child>

	  <child>
		<widget class="GtkButton" id="kcidFilPgSetDef">
		  <property name="visible">True</property>
		  <property name="can_default">True</property>
		  <property name="can_focus">True</property>
		  <property name="label" translatable="yes">Defa_ult</property>
		  <property name="use_underline">True</property>
		  <property name="relief">GTK_RELIEF_NORMAL</property>
		  <property name="focus_on_click">True</property>
		  <property name="response_id">0</property>
		  <signal name="clicked" handler="on_kcidFilPgSetDef_clicked" last_modification_time="Thu, 07 Sep 2006 21:39:25 GMT"/>
		</widget>
	  </child>

	  <child>
		<widget class="GtkButton" id="kctidCancel">
		  <property name="visible">True</property>
		  <property name="can_default">True</property>
		  <property name="can_focus">True</property>
		  <property name="label">gtk-cancel</property>
		  <property name="use_stock">True</property>
		  <property name="relief">GTK_RELIEF_NORMAL</property>
		  <property name="focus_on_click">True</property>
		  <property name="response_id">-6</property>
		  <signal name="clicked" handler="on_kctidCancel_clicked" last_modification_time="Thu, 07 Sep 2006 21:39:25 GMT"/>
		</widget>
	  </child>

	  <child>
		<widget class="GtkButton" id="kctidOk">
		  <property name="visible">True</property>
		  <property name="can_default">True</property>
		  <property name="has_default">True</property>
		  <property name="can_focus">True</property>
		  <property name="label">gtk-ok</property>
		  <property name="use_stock">True</property>
		  <property name="relief">GTK_RELIEF_NORMAL</property>
		  <property name="focus_on_click">True</property>
		  <property name="response_id">-5</property>
		  <signal name="clicked" handler="on_kctidOk_clicked" last_modification_time="Thu, 07 Sep 2006 21:39:25 GMT"/>
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
	<xsl:if test="not(ancestor::widget[@id = 'kridFilPgSetDlg'])">
		<xsl:copy-of select="." />
	</xsl:if>
</xsl:template>

<xsl:template match="child[widget/@id = 'kctidCancel']">
	<xsl:if test="not(ancestor::widget[@id = 'kridFilPgSetDlg'])">
		<xsl:copy-of select="." />
	</xsl:if>
</xsl:template>

<xsl:template match="child[widget/@id = 'kcidFilPgSetDef']">
	<xsl:if test="not(ancestor::widget[@id = 'kridFilPgSetDlg'])">
		<xsl:copy-of select="." />
	</xsl:if>
</xsl:template>

<xsl:template match="child[widget/@id = 'kctidHelp']">
	<xsl:if test="not(ancestor::widget[@id = 'kridFilPgSetDlg'])">
		<xsl:copy-of select="." />
	</xsl:if>
</xsl:template>

<xsl:template match="widget[@class = 'GtkSpinButton'][ancestor::widget[@id = 'kridFilPgSetDlg']]">
	<widget class="{@class}" id="{@id}">
		<xsl:copy-of select="property[@name = 'width_request']" />
		<xsl:copy-of select="property[@name = 'height_request']" />
		<xsl:copy-of select="property[@name = 'visible']" />
		<xsl:copy-of select="property[@name = 'sensitive']" />
		<property name="climb_rate">0.01</property>
		<property name="digits">2</property>
		<property name="numeric">True</property>
		<property name="update_policy">GTK_UPDATE_ALWAYS</property>
		<property name="snap_to_ticks">True</property>
		<property name="wrap">False</property>
		<xsl:choose>
			<xsl:when test="@id = 'kcidFilPgSetMLS' or @id = 'kcidFilPgSetMRS'">
				<property name="adjustment">1.25 0.1 3 0.1 0.1 0</property>
			</xsl:when>
			<xsl:when test="@id = 'kcidFilPgSetMTS' or @id = 'kcidFilPgSetMBS'">
				<property name="adjustment">1 0.1 3 0.1 0.1 0</property>
			</xsl:when>
			<xsl:when test="@id = 'kcidFilPgSetEHS' or @id = 'kcidFilPgSetEFS'">
				<property name="adjustment">0.5 0.1 3 0.1 0.1 0</property>
			</xsl:when>
			<xsl:when test="@id = 'kcidFilPgSetWS'">
				<property name="adjustment">8.5 3 34 0.1 0.1 0</property>
			</xsl:when>
			<xsl:otherwise>
				<property name="adjustment">11 3 34 0.1 0.1 0</property>
			</xsl:otherwise>
		</xsl:choose>
		<signal name="value_changed">
			<xsl:attribute name="handler">
				<xsl:value-of select="concat('on_', @id, '_value_changed')"/>
			</xsl:attribute>
		</signal>
	</widget>
</xsl:template>

<xsl:template match="widget[@id = 'kcidFilPgSetSize'][ancestor::widget[@id = 'kridFilPgSetDlg']]">
	<widget class="{@class}" id="{@id}">
		<xsl:apply-templates select="property" />
		<signal name="changed">
			<xsl:attribute name="handler">
				<xsl:value-of select="concat('on_', @id, '_changed')"/>
			</xsl:attribute>
		</signal>
	</widget>
</xsl:template>

<xsl:template match="widget[@id = 'kcidFilPgSetPort'][ancestor::widget[@id = 'kridFilPgSetDlg']]">
	<widget class="{@class}" id="{@id}">
		<xsl:apply-templates select="property" />
		<signal name="changed">
			<xsl:attribute name="handler">
				<xsl:value-of select="concat('on_', @id, '_toggled')"/>
			</xsl:attribute>
		</signal>
	</widget>
</xsl:template>

<xsl:template match="widget[@id = 'kcidFilPgSetLand']">
	<xsl:choose>
		<xsl:when test="ancestor::widget[@id = 'kridFilPgSetDlg']">
			<widget class="{@class}" id="{@id}">
				<xsl:apply-templates select="property" />
				<property name="group">
					<xsl:text>kcidFilPgSetPort</xsl:text>
				</property>
			</widget>
		</xsl:when>
		<xsl:otherwise>
			<xsl:copy-of select="." />
		</xsl:otherwise>
	</xsl:choose>
</xsl:template>

<xsl:template match="widget[@id = 'kridFilPgSetDlg']//widget[@class = 'GtkImage'][@id = 'kridFilPgSetPort']">
	<widget class="{@class}" id="{@id}">
		<property name="pixbuf">
			<xsl:text>FilPgSetDlgPort.bmp</xsl:text>
		</property>
		<xsl:apply-templates select="property" />
	</widget>
</xsl:template>

<xsl:template match="widget[@id = 'kridFilPgSetDlg']//widget[@class = 'GtkImage'][@id = 'IDC_STATIC']">
	<widget class="{@class}" id="{@id}">
		<property name="pixbuf">
			<xsl:text>FilPgSetDlgLand.bmp</xsl:text>
		</property>
		<xsl:apply-templates select="property" />
	</widget>
</xsl:template>

<xsl:template match="widget[@id = 'kcidFilPgSetFont']//widget[@class = 'GtkImage']">
	<widget class="{@class}" id="{@id}">
		<property name="pixbuf">
			<xsl:text>FilPgSetDlgFont.bmp</xsl:text>
		</property>
		<xsl:apply-templates select="property" />
	</widget>
</xsl:template>

<xsl:template match="widget[@id = 'kcidFilPgSetPgNum']//widget[@class = 'GtkImage']">
	<widget class="{@class}" id="{@id}">
		<property name="pixbuf">
			<xsl:text>FilPgSetDlgPgNum.bmp</xsl:text>
		</property>
		<xsl:apply-templates select="property" />
	</widget>
</xsl:template>

<xsl:template match="widget[@id = 'kcidFilPgSetTotPg']//widget[@class = 'GtkImage']">
	<widget class="{@class}" id="{@id}">
		<property name="pixbuf">
			<xsl:text>FilPgSetDlgTotPg.bmp</xsl:text>
		</property>
		<xsl:apply-templates select="property" />
	</widget>
</xsl:template>

<xsl:template match="widget[@id = 'kcidFilPgSetDate']//widget[@class = 'GtkImage']">
	<widget class="{@class}" id="{@id}">
		<property name="pixbuf">
			<xsl:text>FilPgSetDlgDate.bmp</xsl:text>
		</property>
		<xsl:apply-templates select="property" />
	</widget>
</xsl:template>

<xsl:template match="widget[@id = 'kcidFilPgSetTime']//widget[@class = 'GtkImage']">
	<widget class="{@class}" id="{@id}">
		<property name="pixbuf">
			<xsl:text>FilPgSetDlgTime.bmp</xsl:text>
		</property>
		<xsl:apply-templates select="property" />
	</widget>
</xsl:template>

<xsl:template match="widget[@id = 'kcidFilPgSetTitle']//widget[@class = 'GtkImage']">
	<widget class="{@class}" id="{@id}">
		<property name="pixbuf">
			<xsl:text>FilPgSetDlgTit.bmp</xsl:text>
		</property>
		<xsl:apply-templates select="property" />
	</widget>
</xsl:template>

</xsl:stylesheet>
