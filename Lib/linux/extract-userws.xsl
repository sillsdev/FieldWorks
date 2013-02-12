<?xml version="1.0" encoding="UTF-8"?>
<!-- Extract the UserWs value from the registry XML file. -->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="text" omit-xml-declaration="yes" indent="no"/>
	<xsl:strip-space elements="*"/>

	<xsl:template match="value[@name='UserWs']">
		<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="text()"/>

</xsl:stylesheet>
