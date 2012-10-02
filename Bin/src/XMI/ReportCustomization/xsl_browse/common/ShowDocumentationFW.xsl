<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<!--
Template shows element documentation
-->
	<xsl:template name="SHOW_DOCUMENTATION">
		<xsl:if test="$showDocumentation">
			<xsl:if test="$preformattedDocumentation">
				<!--  place documentation within PRE tag-->
				<xsl:text disable-output-escaping="yes">&lt;pre&gt;</xsl:text>
			</xsl:if>
			<xsl:choose>
				<xsl:when test="$documentationAsHTML">
					<xsl:call-template name="SHOW_TEXT_AS_HTML">
						<xsl:with-param name="value" select="Foundation.Core.ModelElement.comment/Foundation.Core.Comment/Foundation.Core.ModelElement.name"/>
					</xsl:call-template>
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="Foundation.Core.ModelElement.comment/Foundation.Core.Comment/Foundation.Core.ModelElement.name"/>
				</xsl:otherwise>
			</xsl:choose>
			<xsl:if test="$preformattedDocumentation">
				<!--  place documentation within PRE tag-->
				<xsl:text disable-output-escaping="yes">&lt;/pre&gt;</xsl:text>
			</xsl:if>
		</xsl:if>
	</xsl:template>
</xsl:stylesheet>
