<?xml version='1.0' encoding='utf-8'?>
<xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'>

<xsl:import href="copy.xsl" />

<xsl:output method='xml' version='1.0' indent='yes'/>

<xsl:strip-space elements="*" />

<xsl:key name="pages" match="widget[@class = 'GtkDialog']" use="@id" />

<xsl:key name="labels" match="child[widget/@class = 'GtkLabel']" use="../@id" />
<!--xsl:variable name="notebooklabel" select="child[widget/@id = 'kctidTabDlgslabel']" /-->

<xsl:template match="widget[@id = 'kctidTabDlgs']">
	<widget class="{@class}" id="{@id}">
		<xsl:apply-templates select="property" />
		<!-- General -->
		<xsl:call-template name="createNotebookPage">			<xsl:with-param name="dialog_name" select="'kridFmtGenDlg'" />			<xsl:with-param name="label_suffix" select="'Gen'" />			<xsl:with-param name="label_text" select="'General'" />		</xsl:call-template>		<!-- Font -->
		<xsl:call-template name="createNotebookPage">			<xsl:with-param name="dialog_name" select="'kridAfStyleFntDlg'" />			<xsl:with-param name="label_suffix" select="'Fnt'" />			<xsl:with-param name="label_text" select="'Font'" />		</xsl:call-template>		<!-- Paragraph -->
		<xsl:call-template name="createNotebookPage">			<xsl:with-param name="dialog_name" select="'kridFmtParaDlgRtl'" />			<xsl:with-param name="label_suffix" select="'Para'" />			<xsl:with-param name="label_text" select="'Paragraph'" />		</xsl:call-template>		<!-- Bullets and Numbering -->
		<xsl:call-template name="createNotebookPage">			<xsl:with-param name="dialog_name" select="'kridFmtBulNumDlg'" />			<xsl:with-param name="label_suffix" select="'BulNum'" />			<xsl:with-param name="label_text" select="'Bullets and Numbering'" />		</xsl:call-template>		<!-- Border -->
		<xsl:call-template name="createNotebookPage">			<xsl:with-param name="dialog_name" select="'kridFmtBdrDlgP'" />			<xsl:with-param name="label_suffix" select="'Bdr'" />			<xsl:with-param name="label_text" select="'Border'" />		</xsl:call-template>	</widget>
</xsl:template>

<xsl:template match="widget[@id = 'kridFmtGenDlg']">
</xsl:template>

<xsl:template match="widget[@id = 'kridAfStyleFntDlg']">
</xsl:template>

<!--xsl:template match="widget[@id = 'kridFmtParaDlgRtl']">
</xsl:template>

<xsl:template match="widget[@id = 'kridFmtBulNumDlg']">
</xsl:template>

<xsl:template match="widget[@id = 'kridFmtBdrDlgP']">
</xsl:template-->

<xsl:template name="createNotebookPage">
	<xsl:param name="dialog_name" select="'any dialog'" />	<xsl:param name="label_suffix" select="'Gen'" />	<xsl:param name="label_text" select="'General'" />		<xsl:copy-of select="key('pages', $dialog_name)/child" />
		<child>
			<widget class="GtkLabel">
				<xsl:attribute name="id">
					<xsl:value-of select="concat('label01', $label_suffix)" />
				</xsl:attribute>
				<xsl:for-each select="key('labels', 'kctidTabDlgs')/widget/property">
				<!--xsl:for-each select="$notebooklabel/widget/property"-->
					<xsl:choose>
						<xsl:when test="@name = 'label'">
							<property name="{@name}" translatable="{@translatable}">
								<xsl:value-of select="$label_text" />
							</property>
						</xsl:when>
						<xsl:otherwise>
							<xsl:copy-of select="." />
						</xsl:otherwise>
					</xsl:choose>
				</xsl:for-each>
			</widget>
			<xsl:copy-of select="key('labels', 'kctidTabDlgs')/packing" />
			<!--xsl:copy-of select="$notebooklabel/packing" /-->
		</child>
</xsl:template>

</xsl:stylesheet>
