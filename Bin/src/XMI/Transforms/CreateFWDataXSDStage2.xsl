<?xml version="1.0" encoding="UTF-8"?>
<!--This transform should be applied to the results of the CreateFWDataxsStage1.xsl transform.-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:fo="http://www.w3.org/1999/XSL/Format" xmlns:xs="http://www.w3.org/2001/XMLSchema">
	<xsl:output method="xml" version="1.0" encoding="UTF-8"/>
	<xsl:template match="/">
		<xsl:apply-templates/>
	</xsl:template>
	<xsl:template match="xs:schema">
		<xs:schema>
			<xsl:attribute name="elementFormDefault">qualified</xsl:attribute><xsl:text>&#xA;</xsl:text>
			<xsl:for-each select="xs:element">
				<xsl:sort select="@name" order="ascending"/>
				<xs:element>
					<xsl:copy-of select="@*"/>
					<xsl:apply-templates/>
				</xs:element><xsl:text>&#xA;</xsl:text>
			</xsl:for-each>
		</xs:schema>
	</xsl:template>
	<xsl:template match="xs:choice">
		<xs:choice>
			<xsl:copy-of select="@*"/>
			<xsl:for-each select="xs:element">
				<xsl:sort select="@ref"/>
				<xs:element>
					<xsl:copy-of select="@*"/>
					<xsl:apply-templates/>
				</xs:element><xsl:text>&#xA;</xsl:text>
			</xsl:for-each>
		</xs:choice><xsl:text>&#xA;</xsl:text>
	</xsl:template>
	<xsl:template match="xs:restriction">
		<xs:restriction>
			<xsl:copy-of select="@*"/>
			<!--xsl:for-each select="xs:enumeration"-->
			<!--xsl:sort select="@value"/-->
			<!--xs:enumeration>
					<xsl:copy-of select="@*"/>
					<xsl:apply-templates/>
				</xs:enumeration-->
			<!--/xsl:for-each-->
			<xsl:apply-templates>
				<xsl:sort select="@value"/>
			</xsl:apply-templates>
		</xs:restriction><xsl:text>&#xA;</xsl:text>
	</xsl:template>
	<xsl:template match="node()">
		<xsl:copy>
			<xsl:apply-templates select="@* | node()"/>
		</xsl:copy><xsl:text>&#xA;</xsl:text>
	</xsl:template>
	<xsl:template match="@*">
		<xsl:copy>
			<xsl:apply-templates select="@* | node()"/>
		</xsl:copy>
	</xsl:template>
</xsl:stylesheet>
