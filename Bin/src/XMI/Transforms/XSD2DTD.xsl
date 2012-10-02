<?xml version="1.0" encoding="UTF-8"?>
<!--
May 1, 2003 Larry Hayashi:
This converts FwDatabase.xsd to FwDatabase.dtd
This used to be done through a conversion utility in XMLSpy but
I decided to build a transform to do this as it is faster and it doesn't
require spending money on XMLSpy-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:fo="http://www.w3.org/1999/XSL/Format" xmlns:xs="http://www.w3.org/2001/XMLSchema">
	<xsl:output encoding="UTF-8" method="text"/>
	<xsl:strip-space elements="*"/>
	<xsl:template match="/">
		<xsl:text>&#60;?xml version="1.0" encoding="UTF-8"?&#62;&#13;</xsl:text>
		<xsl:apply-templates/>
	</xsl:template>
	<xsl:template match="xs:element">
		<xsl:text>&#60;!ELEMENT </xsl:text>
		<xsl:value-of select="@name"/>
		<xsl:text>&#32;</xsl:text>
		<xsl:if test="@type">
			<xsl:choose>
				<xsl:when test="@type='xs:string'">
					<xsl:text>(#PCDATA)&#62;&#13;</xsl:text>
				</xsl:when>
			</xsl:choose>
		</xsl:if>
		<!--Not sure why we need to do this. Something to do with CmObject being the superclass of everything else.-->
		<xsl:if test="@name='CmObject'">
			<xsl:text>(#PCDATA)&#62;&#13;</xsl:text>
		</xsl:if>
		<xsl:apply-templates/>
	</xsl:template>
	<xsl:template match="xs:complexType">
		<xsl:apply-templates select="xs:sequence | xs:choice | xs:simpleContent"/>
		<xsl:if test="not(xs:sequence | xs:choice | xs:simpleContent)">
			<xsl:text>EMPTY&#62;&#13;</xsl:text>
		</xsl:if>
		<xsl:if test="xs:attribute">
			<xsl:text>&#60;!ATTLIST </xsl:text>
			<xsl:value-of select="../@name"/>
			<xsl:text>&#13;</xsl:text>
			<xsl:apply-templates select="xs:attribute[@type] | xs:attribute[xs:simpleType]"/>
			<xsl:text>&#62;&#13;</xsl:text>
		</xsl:if>
	</xsl:template>
	<xsl:template match="xs:sequence">
		<xsl:text>(</xsl:text>
		<xsl:value-of select="xs:element/@ref"/>
		<xsl:text>)</xsl:text>
		<xsl:choose>
			<xsl:when test="@maxOccurs='unbounded'">
				<xsl:text>*</xsl:text>
			</xsl:when>
			<xsl:otherwise>
				<xsl:text>?</xsl:text>
			</xsl:otherwise>
		</xsl:choose>
		<xsl:text>&#62;&#13;</xsl:text>
	</xsl:template>
	<xsl:template match="xs:choice">
		<xsl:if test="xs:element">
			<xsl:text>(</xsl:text>
			<xsl:for-each select="xs:element">
				<xsl:value-of select="@ref"/>
				<xsl:if test="position()!=last()">
					<xsl:text> | </xsl:text>
				</xsl:if>
			</xsl:for-each>
			<xsl:text>)</xsl:text>
			<xsl:choose>
				<xsl:when test="@maxOccurs='unbounded'">
					<xsl:text>*</xsl:text>
				</xsl:when>
				<xsl:otherwise>
					<xsl:text>?</xsl:text>
				</xsl:otherwise>
			</xsl:choose>
			<xsl:text>&#62;&#13;</xsl:text>
		</xsl:if>
	</xsl:template>
	<xsl:template match="xs:simpleContent">
		<xsl:apply-templates select="xs:extension"/>
	</xsl:template>
	<xsl:template match="xs:extension[@base='xs:string']">
		<xsl:text>(#PCDATA)&#62;&#13;</xsl:text>
		<xsl:text>&#60;!ATTLIST </xsl:text>
		<xsl:value-of select="../../../@name"/>
		<xsl:text>&#13;</xsl:text>
		<xsl:apply-templates select="xs:attribute[@type] | xs:attribute[xs:simpleType]"/>
		<xsl:text>&#62;&#13;</xsl:text>
	</xsl:template>
	<xsl:template match="xs:attribute[@type]">
		<xsl:text>&#9;</xsl:text>
		<xsl:value-of select="@name"/>
		<xsl:text>&#32;</xsl:text>
		<xsl:choose>
			<xsl:when test="@type='xs:string'">
				<xsl:text>CDATA&#32;</xsl:text>
			</xsl:when>
			<xsl:when test="@type='xs:ID'">
				<xsl:text>ID&#32;</xsl:text>
			</xsl:when>
			<xsl:when test="@type='xs:IDREFS'">
				<xsl:text>IDREFS&#32;</xsl:text>
			</xsl:when>
			<xsl:when test="@type='xs:IDREF'">
				<xsl:text>IDREF&#32;</xsl:text>
			</xsl:when>
		</xsl:choose>
		<xsl:choose>
			<xsl:when test="@use='required'">
				<xsl:text>#REQUIRED</xsl:text>
			</xsl:when>
			<xsl:otherwise>
				<xsl:text>#IMPLIED</xsl:text>
			</xsl:otherwise>
		</xsl:choose>
		<xsl:text>&#13;</xsl:text>
	</xsl:template>
	<xsl:template match="xs:attribute[xs:simpleType]">
		<!--xsl:text>&#60;!ATTLIST </xsl:text>
		<xsl:value-of select="../../@name"/>
		<xsl:text>&#13;</xsl:text-->
		<xsl:text>&#9;</xsl:text>
		<xsl:value-of select="@name"/>
		<xsl:text>&#32;(</xsl:text>
		<xsl:for-each select="xs:simpleType/xs:restriction/xs:enumeration">
			<xsl:value-of select="@value"/>
			<xsl:if test="position()!=last()">
				<xsl:text> | </xsl:text>
			</xsl:if>
		</xsl:for-each>
		<xsl:text>)&#32;</xsl:text>
		<xsl:choose>
			<xsl:when test="@use='required'">
				<xsl:text>#REQUIRED</xsl:text>
			</xsl:when>
			<xsl:otherwise>
				<xsl:text>#IMPLIED</xsl:text>
			</xsl:otherwise>
		</xsl:choose>
		<xsl:text>&#13;</xsl:text>
	</xsl:template>
</xsl:stylesheet>
