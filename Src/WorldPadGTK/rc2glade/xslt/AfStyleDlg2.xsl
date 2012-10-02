<?xml version='1.0' encoding='utf-8'?>
<xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'>

<xsl:import href="copy.xsl" />

<xsl:output method='xml' version='1.0' indent='yes'/>

<xsl:strip-space elements="*" />

<!--xsl:key name="helpbutton" match="widget[child/widget/child/widget/@id = 'kctidHelp']" use="@id" />
<xsl:key name="applybutton" match="widget[child/widget/child/widget/@id = 'kctidClose']" use="@id" />
<xsl:key name="cancelbutton" match="widget[child/widget/child/widget/@id = 'kctidCancel']" use="@id" />
<xsl:key name="okbutton" match="widget[child/widget/child/widget/@id = 'kctidOk']" use="@id" /-->

<xsl:template match="widget[@id = 'kridAfStyleDlg']">
	<widget class="{@class}" id="{@id}">
		<xsl:apply-templates select="property" />
		<signal name="close" handler="on_dialog_close" last_modification_time="Fri, 15 Sep 2006 20:52:53 GMT"/>
		<signal name="response" handler="on_dialog_response" last_modification_time="Fri, 15 Sep 2006 20:53:02 GMT"/>
		<signal name="delete_event" handler="on_dialog_delete_event" last_modification_time="Fri, 15 Sep 2006 20:53:39 GMT"/>
		<child internal-child="vbox">			<widget class="GtkVBox" id="dialog-vbox2">				<property name="visible">True</property>				<property name="homogeneous">False</property>				<property name="spacing">0</property>				<child internal-child="action_area">					<widget class="GtkHButtonBox" id="dialog-action_area2">						<property name="visible">True</property>						<property name="layout_style">GTK_BUTTONBOX_END</property>						<child>							<widget class="GtkButton" id="kctidHelp">								<property name="visible">True</property>								<property name="can_default">True</property>								<!--property name="has_default">
									<xsl:value-of select="key('helpbutton', 'kridAfStyleDlg')/child/widget/child/widget/property[@name = 'has_default']" />
								</property-->								<property name="can_focus">True</property>								<property name="label">gtk-help</property>								<property name="use_stock">True</property>								<property name="relief">GTK_RELIEF_NORMAL</property>								<property name="focus_on_click">True</property>								<property name="response_id">-11</property>								<signal name="clicked" handler="on_kctidHelp_clicked" last_modification_time="Thu, 07 Sep 2006 21:39:25 GMT"/>
							</widget>						</child>						<child>							<widget class="GtkButton" id="kctidClose">								<property name="visible">True</property>								<property name="can_default">True</property>								<!--property name="has_default">
									<xsl:value-of select="key('applybutton', 'kridAfStyleDlg')/child/widget/child/widget/property[@name = 'has_default']" />
								</property-->								<property name="can_focus">True</property>								<property name="label">gtk-apply</property>								<property name="use_stock">True</property>								<property name="relief">GTK_RELIEF_NORMAL</property>								<property name="focus_on_click">True</property>								<property name="response_id">-10</property>								<signal name="clicked" handler="on_kctidClose_clicked" last_modification_time="Thu, 07 Sep 2006 21:39:25 GMT"/>
							</widget>						</child>						<child>							<widget class="GtkButton" id="kctidCancel">								<property name="visible">True</property>								<property name="can_default">True</property>								<!--property name="has_default">
									<xsl:value-of select="key('cancelbutton', 'kridAfStyleDlg')/child/widget/child/widget/property[@name = 'has_default']" />
								</property-->								<property name="can_focus">True</property>								<property name="label">gtk-cancel</property>								<property name="use_stock">True</property>								<property name="relief">GTK_RELIEF_NORMAL</property>								<property name="focus_on_click">True</property>								<property name="response_id">-6</property>								<signal name="clicked" handler="on_kctidCancel_clicked" last_modification_time="Thu, 07 Sep 2006 21:39:25 GMT"/>
							</widget>						</child>						<child>							<widget class="GtkButton" id="kctidOk">								<property name="visible">True</property>								<property name="can_default">True</property>								<!--property name="has_default">
									<xsl:value-of select="key('okbutton', 'kridAfStyleDlg')/child/widget/child/widget/property[@name = 'has_default']" />
								</property-->								<property name="can_focus">True</property>								<property name="label">gtk-ok</property>								<property name="use_stock">True</property>								<property name="relief">GTK_RELIEF_NORMAL</property>								<property name="focus_on_click">True</property>								<property name="response_id">-5</property>								<signal name="clicked" handler="on_kctidOk_clicked" last_modification_time="Thu, 07 Sep 2006 21:39:25 GMT"/>
							</widget>						</child>					</widget>					<packing>						<property name="padding">0</property>						<property name="expand">False</property>						<property name="fill">True</property>						<property name="pack_type">GTK_PACK_END</property>					</packing>				</child>
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

<xsl:template match="child[widget/@id = 'kctidClose']">
	<xsl:if test="not(ancestor::widget[@id = 'kridAfStyleDlg'])">
		<xsl:copy-of select="." />
	</xsl:if>
</xsl:template>

<xsl:template match="child[widget/@id = 'kctidOk']">
	<xsl:if test="not(ancestor::widget[@id = 'kridAfStyleDlg'])">
		<xsl:copy-of select="." />
	</xsl:if>
</xsl:template>

<xsl:template match="child[widget/@id = 'kctidCancel']">
	<xsl:if test="not(ancestor::widget[@id = 'kridAfStyleDlg'])">
		<xsl:copy-of select="." />
	</xsl:if>
</xsl:template>

<xsl:template match="child[widget/@id = 'kctidHelp']">
	<xsl:if test="not(ancestor::widget[@id = 'kridAfStyleDlg'])">
		<xsl:copy-of select="." />
	</xsl:if>
</xsl:template>

<xsl:template match="widget[@id = 'kctidFbnRbBullet']">
	<xsl:choose>
		<xsl:when test="ancestor::widget[@id = 'kridAfStyleDlg']">
			<widget class="{@class}" id="{@id}">
				<xsl:apply-templates select="property" />
				<property name="group">
					<xsl:text>kctidFbnRbNotAList</xsl:text>
				</property>
			</widget>
		</xsl:when>
		<xsl:otherwise>
			<xsl:copy-of select="." />
		</xsl:otherwise>
	</xsl:choose>
</xsl:template>

<xsl:template match="widget[@id = 'kctidFbnRbNumber']">
	<xsl:choose>
		<xsl:when test="ancestor::widget[@id = 'kridAfStyleDlg']">
			<widget class="{@class}" id="{@id}">
				<xsl:apply-templates select="property" />
				<property name="group">
					<xsl:text>kctidFbnRbNotAList</xsl:text>
				</property>
			</widget>
		</xsl:when>
		<xsl:otherwise>
			<xsl:copy-of select="." />
		</xsl:otherwise>
	</xsl:choose>
</xsl:template>

<xsl:template match="widget[@id = 'kctidFbnRbUnspecified']">
	<xsl:choose>
		<xsl:when test="ancestor::widget[@id = 'kridAfStyleDlg']">
			<widget class="{@class}" id="{@id}">
				<xsl:apply-templates select="property" />
				<property name="group">
					<xsl:text>kctidFbnRbNotAList</xsl:text>
				</property>
			</widget>
		</xsl:when>
		<xsl:otherwise>
			<xsl:copy-of select="." />
		</xsl:otherwise>
	</xsl:choose>
</xsl:template>

</xsl:stylesheet>
