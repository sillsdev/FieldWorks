<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="xml" version="1.0" encoding="UTF-8" indent="yes" doctype-system="file://%SystemDrive%//Documents and Settings/%USERNAME%/Application Data/XMLmind/XMLEditor/addon/POSes/POSes.dtd" doctype-public="-//XMLmind//DTD POSes//EN"/>
	<xsl:param name="sLangCode">es</xsl:param>
	<xsl:template match="@* | node()">
<!--    name = <xsl:value-of select="name()"/> -->
		<xsl:copy>
			<xsl:apply-templates select="@*"/>
			<xsl:apply-templates/>
		</xsl:copy>
	<xsl:if test="name()='Language' and position()=last()">
	<Language>
				<LangCode><xsl:value-of select="$sLangCode"/></LangCode>
				<Name></Name>
				<Abbreviation></Abbreviation>
				<Description></Description>
				<Citations>
				</Citations>
	</Language>
	</xsl:if>
	</xsl:template>
</xsl:stylesheet>
