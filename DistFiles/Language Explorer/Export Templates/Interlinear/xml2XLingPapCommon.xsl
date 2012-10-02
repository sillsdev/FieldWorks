<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
	<!-- This stylesheet contains the common components for XLingPap morpheme-aligned output  -->
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
phrase/item
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="phrase/item">
		<xsl:if test="item/@type!='segnum'">
			<xsl:copy-of select="."/>
		</xsl:if>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
word
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="word">
		<iword>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</iword>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
word/item
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="word/item">
		<xsl:copy-of select="."/>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
words
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="words">
		<words>
			<xsl:apply-templates/>
		</words>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
morph/item
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="morph/item[@type!='cf' and @type!='hn' and @type!='variantTypes']">
		<item>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</item>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
morph/item[@type='hn']
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<!-- suppress homograph numbers, so they don't occupy an extra line-->
	<xsl:template match="morph/item[@type='hn']"/>
	<!-- This mode occurs within the 'cf' item to display the homograph number from the following item.-->
	<xsl:template match="morph/item[@type='hn']" mode="hn">
		<xsl:apply-templates/>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
morph/item[@type='cf']
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="morph/item[@type='cf']">
		<item>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
			<xsl:variable name="homographNumber" select="following-sibling::item[@type='hn']"/>
			<xsl:if test="$homographNumber">
				<object type="tHomographNumber">
					<xsl:value-of select="$homographNumber"/>
				</object>
			</xsl:if>
			<xsl:variable name="variantTypes" select="following-sibling::item[@type='variantTypes']"/>
			<xsl:if test="$variantTypes">
				<object type="tVariantTypes">
					<xsl:value-of select="$variantTypes"/>
				</object>
			</xsl:if>
		</item>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		morph/item[@type='variantTypes']
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<!-- suppress variant types, so they don't occupy an extra line-->
	<xsl:template match="morph/item[@type='variantTypes']"/>
	<!--
	  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  interlinear-text/item
	  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
   -->
   <xsl:template match="interlinear-text/item">
<!-- ignore - is handled elsewhere -->
   </xsl:template>
	<xsl:include href="xml2XLingPapAllCommon.xsl"/>
</xsl:stylesheet>
