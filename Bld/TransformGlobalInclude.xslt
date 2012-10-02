<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
							xmlns:fw="http://fieldworks.sil.org/nant/fwnant.xsd"
							xmlns="http://fieldworks.sil.org/nant/fwnant.xsd"
						exclude-result-prefixes="fw" >
	<xsl:output method="xml" version="1.0" encoding="UTF-8" indent="yes"/>
	<xsl:template match="fw:include">
		<xsl:comment>Don't modify - this file gets generated from GlobalInclude.xml</xsl:comment>
		<project name="GlobalInclude" default="all">
			<xsl:apply-templates/>
		</project>
	</xsl:template>

	<xsl:template match="fw:pre-init">
		<xsl:copy-of select="fw:property"/>
	</xsl:template>
</xsl:stylesheet>
