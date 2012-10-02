<?xml version="1.0" encoding="UTF-8"?>
<!-- This stylesheet converts older (pre April 1, 2003) FieldWorks XML project files to the new version due to changes made in LgWritingSystem-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="xml" version="1.0" encoding="UTF-8" doctype-system="FwDatabase.dtd"/>
	<xsl:template match="/">
		<xsl:apply-templates/>
	</xsl:template>
	<xsl:template match="LgWritingSystem">
		<LgWritingSystem>
			<xsl:apply-templates select="@*"/>
			<!--xsl:for-each select="*[name(.) != 'OldWritingSystems24']"-->
			<xsl:for-each select="*">
				<xsl:copy>
					<xsl:apply-templates/>
				</xsl:copy>
			</xsl:for-each>
			<xsl:for-each select="OldWritingSystems24/LgOldWritingSystem">
				<xsl:for-each select="Description25">
					<Description24>
						<xsl:apply-templates/>
					</Description24>
				</xsl:for-each>
				<xsl:for-each select="Code25">
					<Code24>
						<xsl:apply-templates/>
					</Code24>
				</xsl:for-each>
				<xsl:for-each select="Renderer25">
					<Renderer24>
						<xsl:apply-templates/>
					</Renderer24>
				</xsl:for-each>
				<xsl:for-each select="RendererInit25">
					<RendererInit24>
						<xsl:apply-templates/>
					</RendererInit24>
				</xsl:for-each>
				<xsl:for-each select="RendererType25">
					<RendererType24>
						<xsl:apply-templates/>
					</RendererType24>
				</xsl:for-each>
				<xsl:for-each select="DefaultMonospace25">
					<DefaultMonospace24>
						<xsl:apply-templates/>
					</DefaultMonospace24>
				</xsl:for-each>
				<xsl:for-each select="DefaultSansSerif25">
					<DefaultSansSerif24>
						<xsl:apply-templates/>
					</DefaultSansSerif24>
				</xsl:for-each>
				<xsl:for-each select="DefaultSerif25">
					<DefaultSerif24>
						<xsl:apply-templates/>
					</DefaultSerif24>
				</xsl:for-each>
				<xsl:for-each select="FontVariation25">
					<FontVariation24>
						<xsl:apply-templates/>
					</FontVariation24>
				</xsl:for-each>
				<xsl:for-each select="KeyboardType25">
					<KeyboardType24>
						<xsl:apply-templates/>
					</KeyboardType24>
				</xsl:for-each>
				<xsl:for-each select="LangId25">
					<LangId24>
						<xsl:apply-templates/>
					</LangId24>
				</xsl:for-each>
				<xsl:for-each select="RightToLeft25">
					<RightToLeft24>
						<xsl:apply-templates/>
					</RightToLeft24>
				</xsl:for-each>
				<!--xsl:for-each select="Locale25">
					<Locale24>
						<xsl:apply-templates/>
					</Locale24>
				</xsl:for-each-->
				<xsl:for-each select="CharPropOverrides25">
					<CharPropOverrides24>
						<xsl:apply-templates/>
					</CharPropOverrides24>
				</xsl:for-each>
				<xsl:for-each select="Collations25">
					<Collations24>
						<xsl:for-each select="LgCollation">
							<LgCollation>
								<xsl:if test="@id">
								<xsl:attribute name="id"><xsl:text>New</xsl:text><xsl:value-of select="@id"/></xsl:attribute>
								</xsl:if>
								<xsl:apply-templates select="node()"/>
							</LgCollation>
						</xsl:for-each>
					</Collations24>
				</xsl:for-each>
			</xsl:for-each>
		</LgWritingSystem>
	</xsl:template>
	<xsl:template match="@* | node()">
		<xsl:copy>
			<xsl:apply-templates select="@* | node()"/>
		</xsl:copy>
	</xsl:template>
</xsl:stylesheet>
