<?xml version='1.0' encoding='utf-8'?>
<xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'>

<xsl:import href="copy.xsl" />

<xsl:output method='xml' version='1.0' indent='yes'/>

<xsl:strip-space elements="*" />

<!--xsl:template match="widget[@id = 'kctidFmtBdrDlgDiag'][ancestor::widget[@id = 'kridFmtBdrDlgP']]"-->
<xsl:template match="widget[@id = 'kctidFmtBdrDlgDiag']">
	<widget class="GtkVBox" id="vbox1">
		<xsl:copy-of select="property[@name = 'width_request']" />
		<xsl:copy-of select="property[@name = 'height_request']" />
		<xsl:copy-of select="property[@name = 'visible']" />
		<property name="homogeneous">False</property>
		<property name="spacing">0</property>
		<child>
			<widget class="Custom" id="{@id}">
				<property name="visible">True</property>
				<!--property name="creation_function">SIL.FieldWorks.GtkCustomWidget.BorderWidget</property-->
				<property name="creation_function">SIL.FieldWorks.FwCoreDlgWidgets.BorderWidget, FwCoreDlgWidgets</property>
				<!--property name="string1">../../Output/Debug/FwCoreDlgWidgets.dll</property-->
				<property name="int1">0</property>
				<property name="int2">0</property>
				<property name="last_modification_time">Fri, 16 May 2008 19:23:32 GMT</property>
			</widget>
			<packing>
				<property name="padding">0</property>
				<property name="expand">True</property>
				<property name="fill">True</property>
			</packing>
		</child>
	</widget>
</xsl:template>

<!--xsl:template match="widget[@id = 'kctidFmtBdrDlgDiag'][ancestor::widget[@id = 'kridAfStyleDlg']]">
	<widget class="GtkDrawingArea" id="{@id}">
		<xsl:copy-of select="property[@name = 'width_request']" />
		<xsl:copy-of select="property[@name = 'height_request']" />
		<xsl:copy-of select="property[@name = 'visible']" />
		<signal name="expose_event" handler="on_kctidFmtBdrDlgDiag_expose_event" last_modification_time="Wed, 22 Nov 2006 23:12:17 GMT"/>
		<signal name="realize" handler="on_kctidFmtBdrDlgDiag_realize" last_modification_time="Wed, 22 Nov 2006 23:12:17 GMT"/>
	</widget>
</xsl:template-->

<xsl:template match="widget[@id = 'kctidFpPreview'][ancestor::widget[@id = 'kridFmtParaDlgRtl']]">
	<widget class="GtkVBox" id="vbox1">
		<xsl:copy-of select="property[@name = 'width_request']" />
		<xsl:copy-of select="property[@name = 'height_request']" />
		<xsl:copy-of select="property[@name = 'visible']" />
		<property name="homogeneous">False</property>
		<property name="spacing">0</property>
		<child>
			<widget class="Custom" id="{@id}">
				<property name="visible">True</property>
				<!--property name="creation_function">SIL.FieldWorks.GtkCustomWidget.ParaPreviewWidget</property-->
				<property name="creation_function">SIL.FieldWorks.FwCoreDlgWidgets.ParaPreviewWidget, FwCoreDlgWidgets</property>
				<!--property name="string1">../../Output/Debug/FwCoreDlgWidgets.dll</property-->
				<property name="int1">0</property>
				<property name="int2">0</property>
				<property name="last_modification_time">Fri, 16 May 2008 19:23:32 GMT</property>
			</widget>
			<packing>
				<property name="padding">0</property>
				<property name="expand">True</property>
				<property name="fill">True</property>
			</packing>
		</child>
	</widget>
</xsl:template>

<xsl:template match="widget[@id = 'kctidFbnPreview'][ancestor::widget[@id = 'kridFmtBulNumDlg']]">
	<widget class="GtkVBox" id="vbox1">
		<xsl:copy-of select="property[@name = 'width_request']" />
		<xsl:copy-of select="property[@name = 'height_request']" />
		<xsl:copy-of select="property[@name = 'visible']" />
		<property name="homogeneous">False</property>
		<property name="spacing">0</property>
		<child>
			<widget class="Custom" id="{@id}">
				<property name="visible">True</property>
				<!--property name="creation_function">SIL.FieldWorks.GtkCustomWidget.BulNumPreviewWidget</property-->
				<property name="creation_function">SIL.FieldWorks.FwCoreDlgWidgets.BulNumPreviewWidget, FwCoreDlgWidgets</property>
				<!--property name="string1">../../Output/Debug/FwCoreDlgWidgets.dll</property-->
				<property name="int1">0</property>
				<property name="int2">0</property>
				<property name="last_modification_time">Fri, 16 May 2008 19:23:32 GMT</property>
			</widget>
			<packing>
				<property name="padding">0</property>
				<property name="expand">True</property>
				<property name="fill">True</property>
			</packing>
		</child>
	</widget>
</xsl:template>

<xsl:template match="widget[@id = 'kctidFfdPreview'][ancestor::widget[@id = 'kridFmtFntDlg']]">
	<widget class="GtkVBox" id="vbox1">
		<xsl:copy-of select="property[@name = 'width_request']" />
		<xsl:copy-of select="property[@name = 'height_request']" />
		<xsl:copy-of select="property[@name = 'visible']" />
		<property name="homogeneous">False</property>
		<property name="spacing">0</property>
		<child>
			<widget class="Custom" id="{@id}">
				<property name="visible">True</property>
				<!--property name="creation_function">SIL.FieldWorks.GtkCustomWidget.FontPreviewWidget</property-->
				<property name="creation_function">SIL.FieldWorks.FwCoreDlgWidgets.FontPreviewWidget, FwCoreDlgWidgets</property>
				<!--property name="string1">../../Output/Debug/FwCoreDlgWidgets.dll</property-->
				<property name="int1">0</property>
				<property name="int2">0</property>
				<property name="last_modification_time">Fri, 16 May 2008 19:23:32 GMT</property>
			</widget>
			<packing>
				<property name="padding">0</property>
				<property name="expand">True</property>
				<property name="fill">True</property>
			</packing>
		</child>
	</widget>
</xsl:template>

<xsl:template match="widget[@class = 'GtkSpinButton'][ancestor::widget[@id = 'kridAfStyleDlg']]">
	<widget class="{@class}" id="{@id}">
		<xsl:copy-of select="property[@name = 'width_request']" />
		<xsl:copy-of select="property[@name = 'height_request']" />
		<xsl:copy-of select="property[@name = 'visible']" />
		<!--xsl:if test="@id = 'kctidFpSpSpIndBy' or @id = 'kctidFpSpLineSpaceAt'">
			<property name="sensitive">False</property>
		</xsl:if-->
		<property name="climb_rate">0.01</property>
		<!--xsl:choose>
			<xsl:when test="starts-with(@id, 'kctidFpSpInd') or @id = 'kctidFpSpSpIndBy'">
				<property name="digits">2</property>
			</xsl:when>
			<xsl:otherwise-->
				<property name="digits">0</property>
			<!--/xsl:otherwise>
		</xsl:choose-->
		<property name="numeric">True</property>
		<property name="update_policy">GTK_UPDATE_ALWAYS</property>
		<property name="snap_to_ticks">True</property>
		<property name="wrap">False</property>
		<!--xsl:choose>
			<xsl:when test="starts-with(@id, 'kctidFpSpInd') or @id = 'kctidFpSpSpIndBy'">
				<property name="adjustment">0 0 3 0.1 0.5 0</property>
			</xsl:when>
			<xsl:when test="@id = 'kctidFpSpLineSpaceAt'">
				<property name="adjustment">0 0 50 1 6 0</property>
			</xsl:when>
			<xsl:otherwise>
				<property name="adjustment">12 0 50 1 6 0</property>
			</xsl:otherwise>
		</xsl:choose-->
		<property name="adjustment">0 -100 100 1 6 0</property>
		<signal name="value_changed">
			<xsl:attribute name="handler">
				<xsl:value-of select="concat('on_', @id, '_value_changed')"/>
			</xsl:attribute>
		</signal>
	</widget>
</xsl:template>

<xsl:template match="widget[@class = 'GtkSpinButton'][ancestor::widget[@id = 'kridFmtParaDlgRtl']]">
	<widget class="{@class}" id="{@id}">
		<xsl:copy-of select="property[@name = 'width_request']" />
		<xsl:copy-of select="property[@name = 'height_request']" />
		<xsl:copy-of select="property[@name = 'visible']" />
		<xsl:if test="@id = 'kctidFpSpSpIndBy' or @id = 'kctidFpSpLineSpaceAt'">
			<property name="sensitive">False</property>
		</xsl:if>
		<property name="climb_rate">0.01</property>
		<xsl:choose>
			<xsl:when test="starts-with(@id, 'kctidFpSpInd') or @id = 'kctidFpSpSpIndBy'">
				<property name="digits">2</property>
			</xsl:when>
			<xsl:otherwise>
				<property name="digits">0</property>
			</xsl:otherwise>
		</xsl:choose>
		<property name="numeric">True</property>
		<property name="update_policy">GTK_UPDATE_ALWAYS</property>
		<property name="snap_to_ticks">True</property>
		<property name="wrap">False</property>
		<xsl:choose>
			<xsl:when test="starts-with(@id, 'kctidFpSpInd') or @id = 'kctidFpSpSpIndBy'">
				<property name="adjustment">0 0 3 0.1 0.5 0</property>
			</xsl:when>
			<xsl:when test="@id = 'kctidFpSpLineSpaceAt'">
				<property name="adjustment">0 0 50 1 6 0</property>
			</xsl:when>
			<xsl:otherwise>
				<property name="adjustment">12 0 50 1 6 0</property>
			</xsl:otherwise>
		</xsl:choose>
		<signal name="value_changed">
			<xsl:attribute name="handler">
				<xsl:value-of select="concat('on_', @id, '_value_changed')"/>
			</xsl:attribute>
		</signal>
	</widget>
</xsl:template>

<xsl:template match="widget[@class = 'GtkSpinButton'][ancestor::widget[@id = 'kridFmtBulNumDlg']]">
	<widget class="{@class}" id="{@id}">
		<xsl:copy-of select="property[@name = 'width_request']" />
		<xsl:copy-of select="property[@name = 'height_request']" />
		<xsl:copy-of select="property[@name = 'visible']" />
		<!--xsl:if test="@id = 'kctidFpSpSpIndBy' or @id = 'kctidFpSpLineSpaceAt'">
			<property name="sensitive">False</property>
		</xsl:if-->
		<property name="sensitive">False</property>
		<property name="climb_rate">0.01</property>
		<!--xsl:choose>
			<xsl:when test="starts-with(@id, 'kctidFpSpInd') or @id = 'kctidFpSpSpIndBy'">
				<property name="digits">2</property>
			</xsl:when>
			<xsl:otherwise-->
				<property name="digits">0</property>
			<!--/xsl:otherwise>
		</xsl:choose-->
		<property name="numeric">True</property>
		<property name="update_policy">GTK_UPDATE_ALWAYS</property>
		<property name="snap_to_ticks">True</property>
		<property name="wrap">False</property>
		<!--xsl:choose>
			<xsl:when test="starts-with(@id, 'kctidFpSpInd') or @id = 'kctidFpSpSpIndBy'">
				<property name="adjustment">0 0 3 0.1 0.5 0</property>
			</xsl:when>
			<xsl:when test="@id = 'kctidFpSpLineSpaceAt'">
				<property name="adjustment">0 0 50 1 6 0</property>
			</xsl:when>
			<xsl:otherwise>
				<property name="adjustment">12 0 50 1 6 0</property>
			</xsl:otherwise>
		</xsl:choose-->
		<property name="adjustment">0 0 100 1 10 0</property>
		<signal name="value_changed">
			<xsl:attribute name="handler">
				<xsl:value-of select="concat('on_', @id, '_value_changed')"/>
			</xsl:attribute>
		</signal>
	</widget>
</xsl:template>

<xsl:template match="widget[@class = 'GtkColorButton'][ancestor::widget[@id = 'kridAfStyleDlg']]">
	<xsl:call-template name="add-colorbutton-handler"/>
</xsl:template>

<xsl:template match="widget[@class = 'GtkColorButton'][ancestor::widget[@id = 'kridFmtParaDlgRtl']]">
	<xsl:call-template name="add-colorbutton-handler"/>
</xsl:template>

<xsl:template match="widget[@class = 'GtkColorButton'][ancestor::widget[@id = 'kridFmtBdrDlgP']]">
	<xsl:call-template name="add-colorbutton-handler"/>
</xsl:template>

<xsl:template name="add-colorbutton-handler">
	<widget class="{@class}" id="{@id}">
		<xsl:apply-templates select="property" />
		<signal name="color_set">
			<xsl:attribute name="handler">
				<xsl:value-of select="concat('on_', @id, '_color_set')"/>
			</xsl:attribute>
		</signal>
	</widget>
</xsl:template>

<xsl:template match="widget[@class = 'GtkComboBox'][ancestor::widget[@id = 'kridAfStyleDlg']]">
	<xsl:call-template name="add-combobox-handler"/>
</xsl:template>

<xsl:template match="widget[@class = 'GtkComboBox'][ancestor::widget[@id = 'kridFmtParaDlgRtl']]">
	<xsl:call-template name="add-combobox-handler"/>
</xsl:template>

<xsl:template match="widget[@class = 'GtkComboBox'][ancestor::widget[@id = 'kridFmtBulNumDlg']]">
	<xsl:call-template name="add-combobox-handler">
		<xsl:with-param name="sensitive" select="'False'"/>
	</xsl:call-template>
</xsl:template>

<xsl:template match="widget[@class = 'GtkComboBox'][ancestor::widget[@id = 'kridFmtBdrDlgP']]">
	<xsl:call-template name="add-combobox-handler"/>
</xsl:template>

<xsl:template name="add-combobox-handler">
  <xsl:param name="sensitive" select="'True'"/>
	<widget class="{@class}" id="{@id}">
		<xsl:apply-templates select="property" />
		<property name="sensitive">
			<xsl:value-of select="$sensitive"/>
		</property>
		<signal name="changed">
			<xsl:attribute name="handler">
				<xsl:value-of select="concat('on_', @id, '_changed')"/>
			</xsl:attribute>
		</signal>
	</widget>
</xsl:template>

<xsl:template match="widget[@class = 'GtkCheckButton'][ancestor::widget[@id = 'kridAfStyleDlg']]">
	<xsl:call-template name="add-checkbutton-handler"/>
</xsl:template>

<xsl:template match="widget[@class = 'GtkCheckButton'][ancestor::widget[@id = 'kridFmtBulNumDlg']]">
	<xsl:call-template name="add-checkbutton-handler">
		<xsl:with-param name="sensitive" select="'False'"/>
	</xsl:call-template>
</xsl:template>

<xsl:template name="add-checkbutton-handler">
  <xsl:param name="sensitive" select="'True'"/>
	<widget class="{@class}" id="{@id}">
		<xsl:apply-templates select="property" />
		<property name="sensitive">
			<xsl:value-of select="$sensitive"/>
		</property>
		<signal name="toggled">
			<xsl:attribute name="handler">
				<xsl:value-of select="concat('on_', @id, '_toggled')"/>
			</xsl:attribute>
		</signal>
	</widget>
</xsl:template>

<xsl:template match="widget[@id = 'kctidFfdFeatures']">
	<widget class="{@class}" id="{@id}">
		<xsl:apply-templates select="property" />
		<property name="sensitive">False</property>
		<signal name="clicked">
			<xsl:attribute name="handler">
				<xsl:value-of select="concat('on_', @id, '_clicked')"/>
			</xsl:attribute>
		</signal>
	</widget>
</xsl:template>

<xsl:template match="widget[@id = 'kctidFbnPbFont']">
	<widget class="{@class}" id="{@id}">
		<xsl:apply-templates select="property" />
		<property name="sensitive">False</property>
		<signal name="clicked">
			<xsl:attribute name="handler">
				<xsl:value-of select="concat('on_', @id, '_clicked')"/>
			</xsl:attribute>
		</signal>
	</widget>
</xsl:template>

<xsl:template match="widget[@id = 'kctidFmtBdrDlgNoneP']">
	<widget class="GtkCheckButton" id="{@id}">
		<xsl:copy-of select="property[@name = 'width_request']" />
		<xsl:copy-of select="property[@name = 'height_request']" />
		<xsl:copy-of select="property[@name = 'visible']" />
		<property name="can_focus">True</property>
		<property name="relief">GTK_RELIEF_NORMAL</property>
		<property name="focus_on_click">True</property>
		<property name="active">True</property>
		<property name="inconsistent">False</property>
		<property name="draw_indicator">False</property>
		<signal name="toggled" handler="on_kctidFmtBdrDlgNoneP_toggled" last_modification_time="Wed, 22 Nov 2006 23:13:36 GMT"/>
		<child>
			<widget class="GtkImage" id="image1">
				<property name="visible">True</property>
				<property name="pixbuf">FmtBdrDlgNoneP.bmp</property>
				<property name="xalign">0.5</property>
				<property name="yalign">0.5</property>
				<property name="xpad">0</property>
				<property name="ypad">0</property>
			</widget>
		</child>
	</widget>
</xsl:template>

<xsl:template match="widget[@id = 'kctidFmtBdrDlgAll']">
	<widget class="GtkCheckButton" id="{@id}">
		<xsl:copy-of select="property[@name = 'width_request']" />
		<xsl:copy-of select="property[@name = 'height_request']" />
		<xsl:copy-of select="property[@name = 'visible']" />
		<property name="can_focus">True</property>
		<property name="relief">GTK_RELIEF_NORMAL</property>
		<property name="focus_on_click">True</property>
		<property name="active">False</property>
		<property name="inconsistent">False</property>
		<property name="draw_indicator">False</property>
		<signal name="toggled" handler="on_kctidFmtBdrDlgAll_toggled" last_modification_time="Wed, 22 Nov 2006 23:13:36 GMT"/>
		<child>
			<widget class="GtkImage" id="image1">
				<property name="visible">True</property>
				<property name="pixbuf">FmtBdrDlgAll.bmp</property>
				<property name="xalign">0.5</property>
				<property name="yalign">0.5</property>
				<property name="xpad">0</property>
				<property name="ypad">0</property>
			</widget>
		</child>
	</widget>
</xsl:template>

<xsl:template match="child[widget/@id = 'kctidFmtBdrDlgLeading']">
	<child>
		<widget class="{widget/@class}" id="{widget/@id}">
			<xsl:for-each select="widget/property">
				<xsl:choose>
					<xsl:when test="@name = 'width_request'">
						<property name="{@name}">50</property>
					</xsl:when>
					<xsl:otherwise>
						<xsl:copy-of select="." />
					</xsl:otherwise>
				</xsl:choose>
			</xsl:for-each>
			<signal name="toggled" handler="on_checkLTRB_toggled" last_modification_time="Thu, 16 Nov 2006 21:49:43 GMT"/>
		</widget>
		<packing>
			<xsl:for-each select="packing/property">
				<xsl:choose>
					<xsl:when test="@name = 'x'">
						<property name="{@name}">176</property>
					</xsl:when>
					<xsl:otherwise>
						<xsl:copy-of select="." />
					</xsl:otherwise>
				</xsl:choose>
			</xsl:for-each>
		</packing>
	</child>
</xsl:template>

<xsl:template match="widget[@id = 'kctidFmtBdrDlgTop']">
	<widget class="{@class}" id="{@id}">
		<xsl:apply-templates select="property" />
		<signal name="toggled" handler="on_checkLTRB_toggled" last_modification_time="Thu, 16 Nov 2006 21:49:43 GMT"/>
	</widget>
</xsl:template>

<xsl:template match="widget[@id = 'kctidFmtBdrDlgTrailing']">
	<widget class="{@class}" id="{@id}">
		<xsl:apply-templates select="property" />
		<signal name="toggled" handler="on_checkLTRB_toggled" last_modification_time="Thu, 16 Nov 2006 21:49:43 GMT"/>
	</widget>
</xsl:template>

<xsl:template match="widget[@id = 'kctidFmtBdrDlgBottom']">
	<widget class="{@class}" id="{@id}">
		<xsl:apply-templates select="property" />
		<signal name="toggled" handler="on_checkLTRB_toggled" last_modification_time="Thu, 16 Nov 2006 21:49:43 GMT"/>
	</widget>
</xsl:template>

<!--xsl:template match="widget[@id = 'kctidColor'][ancestor::widget[@id = 'kridFmtBdrDlgP']]"-->
<!--xsl:template match="widget[@id = 'kctidColor']">
	<widget class="{@class}" id="{@id}">
		<xsl:apply-templates select="property" />
		<signal name="color_set" handler="on_kctidColor_color_set" last_modification_time="Thu, 16 Nov 2006 21:49:43 GMT"/>
	</widget>
</xsl:template-->

<!--xsl:template match="widget[@id = 'kctidFmtBdrDlgWidth'][ancestor::widget[@id = 'kridFmtBdrDlgP']]"-->
<!--xsl:template match="widget[@id = 'kctidFmtBdrDlgWidth']">
	<widget class="{@class}" id="{@id}">
		<xsl:apply-templates select="property" />
		<signal name="changed" handler="on_kctidFmtBdrDlgWidth_changed" last_modification_time="Thu, 16 Nov 2006 21:49:43 GMT"/>
	</widget>
</xsl:template-->

<xsl:template match="widget[@id = 'IDC_STATIC']">
	<widget class="{@class}" id="{@id}">
		<xsl:for-each select="property">
			<xsl:if test="@name = 'label'">
				<xsl:choose>
					<xsl:when test=". = '_None'">
						<property name="mnemonic_widget">kctidFmtBdrDlgNoneP</property>
					</xsl:when>
					<xsl:when test=". = 'A_ll'">
						<property name="mnemonic_widget">kctidFmtBdrDlgAll</property>
					</xsl:when>
				</xsl:choose>
			</xsl:if>
			<xsl:copy-of select="." />
		</xsl:for-each>
		<xsl:apply-templates select="child" />
	</widget>
</xsl:template>

</xsl:stylesheet>
