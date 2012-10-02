<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="xml" version="1.0" encoding="UTF-8" indent="yes" doctype-system="file://%SystemDrive%/Documents and Settings/%USERNAME%/Application Data/XMLmind/XMLEditor/addon/FeatureCatalog/FeatureCatalog.dtd" doctype-public="-//XMLmind//DTD FeatureCatalog//EN"/>
	<xsl:param name="sLangCode">XXX</xsl:param>
	<xsl:template match="node()|@*">
		<xsl:choose>
			<xsl:when test="name()='Language' and LangCode=$sLangCode"/>
			<xsl:otherwise>
				<xsl:copy>
					<xsl:apply-templates select="@*"/>
					<xsl:apply-templates/>
				</xsl:copy>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
</xsl:stylesheet>
