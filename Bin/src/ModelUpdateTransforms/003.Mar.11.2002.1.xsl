<?xml version="1.0" encoding="UTF-8"?>
<!-- Transform to handle the following change
Deleted MorphologicalData: PhoneEnvs (redundant with attributes on PhonologicalData)
Changed FsDisjunctiveValue: Value to Values
Added subclass LexSubentry (sub of LexMajorEntry) num=7
Moved LexMajorEntry: MainEntriesOrSenses to LexSubentry
Moved LexMajorEntry: SubentryType to LexSubentry
Moved LexMajorEntry: LiteralMeaning to LexSubentry
Moved LexMajorEntry: IsBodyWithHeadword to LexSubentry
which in the XML file is the same as
PhoneEnvs does not need to be dealth with as we don't have an data that uses it yet.
Replace Value2006 to Values2006
Replace MainEntriesOrSenses5008 to MainEntriesOrSenses5007
Replace SubentryType5008 to SubentryType5007
Replace LiteralMeaning5008 to LiteralMeaning5007
Replace IsBodyWithHeadword5008 to IsBodyWithHeadword5007
It also means changing entries that have any of the above attributes into subentries which is done ....2.xsl of this series.-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="xml" version="1.0" encoding="UTF-8" doctype-system="FwDatabase.dtd"/>
	<xsl:template match="/">
		<xsl:apply-templates/>
	</xsl:template>
	<xsl:template match="Value2006">
		<Values2006>
			<xsl:apply-templates/>
		</Values2006>
	</xsl:template>
	<xsl:template match="MainEntriesOrSenses5008">
		<MainEntriesOrSenses5007>
			<xsl:apply-templates/>
		</MainEntriesOrSenses5007>
	</xsl:template>
	<xsl:template match="SubentryType5008">
		<SubentryType5007>
			<xsl:apply-templates/>
		</SubentryType5007>
	</xsl:template>
<xsl:template match="LiteralMeaning5008">
		<LiteralMeaning5007>
			<xsl:apply-templates/>
		</LiteralMeaning5007>
	</xsl:template>
<xsl:template match="IsBodyWithHeadword5008">
		<IsBodyWithHeadword5007>
			<xsl:apply-templates/>
		</IsBodyWithHeadword5007>
	</xsl:template>
	<xsl:template match="@* | node()">
		<xsl:copy>
			<xsl:apply-templates select="@* | node()"/>
		</xsl:copy>
	</xsl:template>
</xsl:stylesheet>