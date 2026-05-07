<?xml version="1.0" encoding="UTF-8"?>
<!-- transform to convert Allomorph Generator file from db version 3 to 4
-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="xml" version="1.0" encoding="UTF-8" indent="no"/>
	<xsl:template match="@* |  node()">
		<xsl:copy>
			<xsl:apply-templates select="@*"/>
			<xsl:apply-templates/>
		</xsl:copy>
	</xsl:template>
	<!--
	increment dbversion
	-->
	<xsl:template match="@dbVersion[.='3']">
		<xsl:attribute name="dbVersion">
			<xsl:text>4</xsl:text>
		</xsl:attribute>
	</xsl:template>
	<!--
	Change name from attribute to element
	-->
	<xsl:template match="Category">
		<Category active="{@active}" guid="{@guid}">
			<Name>
				<xsl:value-of select="@name"/>
			</Name>
		</Category>
	</xsl:template>
	<xsl:template match="Environment">
		<Environment active="{@active}" guid="{@guid}">
			<Name>
				<xsl:value-of select="@name"/>
			</Name>
		</Environment>
	</xsl:template>
	<xsl:template match="MorphType">
		<MorphType active="{@active}" guid="{@guid}">
			<Name>
				<xsl:value-of select="@name"/>
			</Name>
		</MorphType>
	</xsl:template>
	<xsl:template match="Replace">
		<Replace active="{@active}" guid="{@guid}" mode="{@mode}">
			<Name>
				<xsl:value-of select="@name"/>
			</Name>
			<xsl:apply-templates/>
		</Replace>
	</xsl:template>
	<xsl:template match="StemName">
		<StemName active="{@active}" guid="{@guid}">
			<Name>
				<xsl:value-of select="@name"/>
			</Name>
		</StemName>
	</xsl:template>
	<!--
	Remove ReplaceOps from Action
	-->
	<xsl:template match="ReplaceOps[parent::Action]"/>
	<!--
	Remove hard-coded writing system booleans from Replace
	-->
	<xsl:template match="@ach[parent::Replace]"/>
	<xsl:template match="@acl[parent::Replace]"/>
	<xsl:template match="@akh[parent::Replace]"/>
	<xsl:template match="@akl[parent::Replace]"/>
	<xsl:template match="@ame[parent::Replace]"/>
</xsl:stylesheet>
