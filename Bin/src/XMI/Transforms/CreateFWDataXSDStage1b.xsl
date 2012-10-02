<?xml version="1.0" encoding="UTF-8"?>
<!--This transform should be applied to the results of the CreateFWDataxsStage1.xsl transform.
This transform simply adds the element: ref Prop to StyleRules15 element and the Rules17 element because Prop is used to symbolically
represent styles in the XML output of a FieldWorks project.-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:fo="http://www.w3.org/1999/XSL/Format" xmlns:xs="http://www.w3.org/2001/XMLSchema">
	<xsl:output method="xml" version="1.0" encoding="UTF-8"/>
	<xsl:template match="/">
		<xsl:apply-templates/>
	</xsl:template>
	<xsl:template match="xs:schema">
		<xs:schema>
			<xsl:attribute name="elementFormDefault">qualified</xsl:attribute>
			<xsl:apply-templates/>
		</xs:schema>
	</xsl:template>
	<xsl:template match="xs:element[@name='Rules17']">
		<xs:element name="Rules17">
			<xs:complexType>
				<xs:choice minOccurs="0">
					<xs:element ref="Binary"/>
					<xs:element ref="Prop"/>
				</xs:choice>
			</xs:complexType>
		</xs:element>
	</xsl:template>
	<xsl:template match="xs:element[@name='StyleRules15']">
		<xs:element name="StyleRules15">
			<xs:complexType>
				<xs:choice minOccurs="0">
					<xs:element ref="Binary"/>
					<xs:element ref="Prop"/>
				</xs:choice>
			</xs:complexType>
		</xs:element>
	</xsl:template>
	<xsl:template match="@* | node()">
		<xsl:copy>
			<xsl:apply-templates select="@* | node()"/>
		</xsl:copy>
	</xsl:template>
</xsl:stylesheet>
