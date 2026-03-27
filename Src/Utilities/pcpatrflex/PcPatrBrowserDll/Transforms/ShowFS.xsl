<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="html" version="1.0" encoding="UTF-8" indent="yes"/>
	<!--
================================================================
Format ANA interlinear
================================================================
Revision History is at the end of this file.

- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Preamble
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:param name="sFeatureColor" select="'black'"/>
	<xsl:param name="sIndexMarker" select="'$'"/>
	<xsl:param name="sIndexMarkerColor" select="'gray'"/>
	<xsl:param name="sValueColor" select="'navy'"/>
	<xsl:variable name="sIndexKey" select="'co'"/>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Main template
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="/Fs | /Lexfs">
		<html>
			<head>
				<meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
			</head>
			<body>
				<xsl:call-template name="DoFs"/>
			</body>
		</html>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoFs
	Output feature structure; is recursive
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="DoFs">
		<table cellpadding="0" cellspacing="0">
			<tr valign="top">
				<td>
					<xsl:if test="@id">
						<span>
							<xsl:attribute name="style">font-size='smaller';color=<xsl:value-of select="$sIndexMarkerColor"/></xsl:attribute>
							<xsl:value-of select="$sIndexMarker"/>
							<xsl:value-of select="substring-after(@id,$sIndexKey)"/>
						</span>
					</xsl:if>
				</td>
				<td style="padding-left='2'">
					<xsl:text> [ </xsl:text>
				</td>
				<td>
					<table cellpadding="0" cellspacing="0">
						<xsl:for-each select="F">
							<tr valign="top">
								<td style="padding-right='2'">
									<span>
										<xsl:attribute name="style">color=<xsl:value-of select="$sFeatureColor"/></xsl:attribute>
										<xsl:value-of select="@name"/>
									</span>
									<xsl:text disable-output-escaping="yes"> : </xsl:text>
								</td>
								<td>
									<xsl:choose>
										<xsl:when test="Fs">
											<xsl:for-each select="Fs">
												<xsl:call-template name="DoFs"/>
											</xsl:for-each>
										</xsl:when>
										<xsl:otherwise>
											<span>
												<xsl:attribute name="style">color=<xsl:value-of select="$sValueColor"/></xsl:attribute>
												<xsl:value-of select="Str"/>
											</span>
										</xsl:otherwise>
									</xsl:choose>
								</td>
							</tr>
						</xsl:for-each>
					</table>
				</td>
				<td valign="bottom">]</td>
			</tr>
		</table>
	</xsl:template>
</xsl:stylesheet>
<!--
================================================================
Revision History
- - - - - - - - - - - - - - - - - - -
26-Aug-2005	Andy Black	Initial Draft
================================================================
 -->
