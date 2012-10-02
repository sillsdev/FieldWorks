<?xml version="1.0" encoding="UTF-8"?>
<!-- Transform to handle the following change
February 7, 2002 L.S. Hayashi as per Hayashi
Renamed MoStemAllomorph: PhoneEnviron to PhoneEnv for consistency.
which in the XML file is the same as
Replace Senses5008 open and close elements with Senses5002-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="xml" version="1.0" encoding="UTF-8" doctype-system="FwDatabase.dtd"/>
	<xsl:template match="/">
		<xsl:apply-templates/>
	</xsl:template>
	<xsl:template match="Senses5008">
		<Senses5002>
			<xsl:apply-templates/>
		</Senses5002>
	</xsl:template>
	<xsl:template match="@* | node()">
		<xsl:copy>
			<xsl:apply-templates select="@* | node()"/>
		</xsl:copy>
	</xsl:template>
</xsl:stylesheet>