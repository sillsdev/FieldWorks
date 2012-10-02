<?xml version="1.0" encoding="UTF-8"?>
<!--Creates an XDR file for use with SQL Server 8
Input: xmi2cellar3.xml file (this file is generated from the FieldWorks.xmi file after Magic2CellarStageX are applied).
Created by John Hatton and Larry Hayashi
April 12, 2001-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:fo="http://www.w3.org/1999/XSL/Format" xmlns="urn:schemas-microsoft-com:xml-data" xmlns:dt="urn:schemas-microsoft-com:datatypes" xmlns:sql="urn:schemas-microsoft-com:xml-sql">
	<xsl:output method="xml" version="1.0" encoding="UTF-8"/>
	<xsl:template match="/">
		<Schema xmlns="urn:schemas-microsoft-com:xml-data" xmlns:dt="urn:schemas-microsoft-com:datatypes" xmlns:sql="urn:schemas-microsoft-com:xml-sql" name="Schema" sql:id="MyMappingSchema" sql:is-mapping-schema="1">
			<xsl:for-each select="EntireModel/CellarModule/class[@abstract='false']">
				<ElementType content="mixed" model="closed" order="many">
					<xsl:attribute name="name">
						<xsl:value-of select="@id"/>
					</xsl:attribute>
					<xsl:apply-templates select="props/basic"/>
					<xsl:apply-templates select="props/rel"/>
					<xsl:apply-templates select="props/owning">
						<xsl:with-param name="SubClassName" select="@id"/>
					</xsl:apply-templates>
					<xsl:call-template name="GetAttributesOfSuperClass"/>
				</ElementType>
			</xsl:for-each>
		</Schema>
	</xsl:template>
	<xsl:template name="GetAttributesOfSuperClass">
		<xsl:variable name="SubClassNameVar" select="@id"/>
		<xsl:choose>
			<xsl:when test="@base='CmObject'">
				<!--Here we may need to add custom capabilities to each class-->
			</xsl:when>
			<xsl:otherwise>
				<xsl:variable name="SuperClassBase" select="@base"/>
				<xsl:for-each select="//class[@id=$SuperClassBase]">
					<xsl:apply-templates select="props/basic"/>
					<xsl:apply-templates select="props/rel"/>
					<xsl:apply-templates select="props/owning">
						<xsl:with-param name="SubClassName" select="$SubClassNameVar"/>
					</xsl:apply-templates>
					<xsl:call-template name="GetAttributesOfSuperClass"/>
				</xsl:for-each>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<xsl:template match="props/basic">
		<AttributeType>
			<xsl:attribute name="name">
				<xsl:value-of select="@id"/>
			</xsl:attribute>
			<xsl:attribute name="dt:type">
				<xsl:choose>
					<xsl:when test="@sig[.='Integer']">i4</xsl:when>
					<xsl:when test="@sig[.='Boolean']">boolean</xsl:when>
					<xsl:when test="@sig[.='String'] or @sig[.='String'] or @sig[.='MultiString'] or @sig[.='Text'] or @sig[.='MultiText'] or @sig[.='MultiUnicode'] or @sig[.='Unicode']">string</xsl:when>
					<!--We haven't accounted for all basic signature types yet but for now we will try and use string-->
					<xsl:otherwise>string</xsl:otherwise>
				</xsl:choose>
			</xsl:attribute>
		</AttributeType>
		<attribute>
			<xsl:attribute name="type">
				<xsl:value-of select="@id"/>
			</xsl:attribute>
			<xsl:choose>
				<xsl:when test="@sig[.='String'] or @sig[.='String'] or @sig[.='MultiString'] or @sig[.='Text'] or @sig[.='MultiText']">
					<xsl:attribute name="sql:relation">
						<xsl:value-of select="../../@id"/>
						<xsl:text>_</xsl:text>
						<xsl:value-of select="@id"/>
					</xsl:attribute>
					<xsl:attribute name="sql:field">
						<xsl:text>txt</xsl:text>
					</xsl:attribute>
					<sql:relationship>
						<xsl:attribute name="key-relation">
							<xsl:value-of select="../../@id"/>
						</xsl:attribute>
						<xsl:attribute name="key">
							<xsl:text>id</xsl:text>
						</xsl:attribute>
						<xsl:attribute name="foreign-key">
							<xsl:text>obj</xsl:text>
						</xsl:attribute>
						<xsl:attribute name="foreign-relation">
							<xsl:value-of select="../../@id"/>_<xsl:value-of select="@id"/>
						</xsl:attribute>
					</sql:relationship>
				</xsl:when>
				<xsl:otherwise>
					<xsl:attribute name="sql:field">
						<xsl:value-of select="@id"/>
					</xsl:attribute>
				</xsl:otherwise>
			</xsl:choose>
		</attribute>
	</xsl:template>
	<xsl:template match="props/rel">
		<AttributeType>
			<xsl:attribute name="name">
				<xsl:value-of select="@id"/>
			</xsl:attribute>
			<xsl:attribute name="dt:type">
				<xsl:text>i4</xsl:text>
			</xsl:attribute>
		</AttributeType>
		<attribute>
			<xsl:attribute name="type">
				<xsl:value-of select="@id"/>
			</xsl:attribute>
			<xsl:attribute name="sql:field">
				<xsl:value-of select="@id"/>
			</xsl:attribute>
		</attribute>
	</xsl:template>
	<xsl:template match="props/owning">
		<xsl:param name="SubClassName"/>
		<xsl:variable name="sig" select="@sig"/>
		<!--We look for the class that is the signature of the owning property (there will only be one)-->
		<ElementType>
			<xsl:attribute name="name">
				<xsl:value-of select="$SubClassName"/>
				<xsl:text>_</xsl:text>
				<xsl:value-of select="@id"/>
			</xsl:attribute>
			<xsl:for-each select="//class[@id=$sig]">
				<!--We want to find out two things - a. is this class concrete, if yes then it should be listed.
			b. does this class have subclasses, if yes, go out and get the subclasses because they are possible
			signature classes for this owning property. It is also possible that those subclasses in turn have subclasses.-->
				<xsl:call-template name="GetSubClasses"/>
			</xsl:for-each>
		</ElementType>
	</xsl:template>
	<xsl:template name="GetSubClasses">
		<xsl:variable name="classID" select="@id"/>
		<xsl:if test="@abstract='false'">
			<element>
				<xsl:attribute name="type">
					<xsl:value-of select="@id"/>
				</xsl:attribute>
			</element>
		</xsl:if>
		<xsl:for-each select="//class[@base=$classID]">
			<xsl:call-template name="GetSubClasses"/>
		</xsl:for-each>
	</xsl:template>
</xsl:stylesheet>
