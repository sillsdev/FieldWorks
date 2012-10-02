<?xml version="1.0" encoding="UTF-8"?>
<!-- continued from ...1.xsl of this series.
It also means changing entries that have any of the above attributes into subentries which is done ....2.xsl of this series.-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="xml" version="1.0" encoding="UTF-8" doctype-system="FwDatabase.dtd"/>
	<xsl:template match="/">
		<xsl:apply-templates/>
	</xsl:template>
	<xsl:template match="LexMajorEntry[MainEntriesOrSenses5007 or SubentryType5007 or LiteralMeaning5007 or IsBodyWithHeadword5007]">
		<LexSubentry>
			<!--I am not sure why this is needed but apply-templates misses copying the attributes on the element-->
			<xsl:for-each select="@*">
				<xsl:copy/>
			</xsl:for-each>
			<!--Resume normal apply-templates here for all other elements and their attributes-->
			<xsl:apply-templates/>
		</LexSubentry>
	</xsl:template>
	<xsl:template match="@* | node()">
		<xsl:copy>
			<xsl:apply-templates select="@* | node()"/>
		</xsl:copy>
	</xsl:template>
</xsl:stylesheet>
