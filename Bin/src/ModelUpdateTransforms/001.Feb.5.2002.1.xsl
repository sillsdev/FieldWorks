<?xml version="1.0" encoding="UTF-8"?>
<!-- Transform to handle the following change
February 5, 2002 L.S. Hayashi as per Hayashi
Moved Senses attribute from LexMajorEntry to LexEntry
which in the XML file is the same as
Replace PhoneEnviron5045 open and close elements with PhoneEnv5045-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="xml" version="1.0" encoding="UTF-8" doctype-system="FwDatabase.dtd"/>
	<xsl:template match="/">
		<xsl:apply-templates/>
	</xsl:template>
	<xsl:template match="PhoneEnviron5045">
		<PhoneEnv5045>
			<xsl:apply-templates/>
		</PhoneEnv5045>
	</xsl:template>
	<xsl:template match="@* | node()">
		<xsl:copy>
			<xsl:apply-templates select="@* | node()"/>
		</xsl:copy>
	</xsl:template>
</xsl:stylesheet>