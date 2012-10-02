<?xml version="1.0" encoding="UTF-8"?>
<!--This stylesheet is applied to the generated *.cmt (cmtemp) files so that they consistently appear for easier merges the
source code management system. It only resorts data. It does not change data.
The resulting sort places all num=0 classes first, by class name, then sorts remaining
classes first by level (starting at 0) and then by class name. Property names are
sorted within a class by num. -->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="xml" version="1.0" encoding="UTF-8" doctype-system="../../../bin/CellarModule.dtd"/>
	<xsl:template match="/">
		<xsl:apply-templates/>
	</xsl:template>
	<xsl:template match="CellarModule">
		<!--Copy the attributes of the CellarModule-->
		<CellarModule>
		<xsl:apply-templates select="@*"/>
		<!--Find all the classes that have an num of 0 and sort them by id name-->
		<xsl:for-each select="class[@num = 0]">
			<xsl:sort select="@id"/>
			<!--Copy any attributes of the Classes with an id of 0-->
			<xsl:text>&#xA;</xsl:text><class>
			<xsl:apply-templates select="@*"/>
			</class>
		</xsl:for-each>
		<!--Find all the classes that do not have a num of 0 and sort them by id name-->
		<xsl:for-each select="class[not(@num = 0)]">
			<xsl:sort select="@depth"/>
			<xsl:sort select="@id"/>
			<xsl:text>&#xA;</xsl:text><class>
			<!--Copy the attributes of the class-->
			<xsl:apply-templates select="@*"/>
			<!--Find all the properties of the class and sort them by num-->
			<xsl:for-each select="props">
				<xsl:text>&#xA;</xsl:text><props>
				<xsl:apply-templates select="../props/*">
					<xsl:sort select="@num" data-type="number"/>
				</xsl:apply-templates>
				</props>
			</xsl:for-each>
			</class>
		</xsl:for-each>
		</CellarModule>
	</xsl:template>
	<xsl:template match="props/*">
		<xsl:text>&#xA;</xsl:text><xsl:copy>
		<xsl:apply-templates select="@*"/>
		</xsl:copy>
	</xsl:template>
	<xsl:template match="@* | node()">
		<xsl:copy>
			<xsl:apply-templates select="@* | node()"/>
		</xsl:copy>
	</xsl:template>
</xsl:stylesheet>
