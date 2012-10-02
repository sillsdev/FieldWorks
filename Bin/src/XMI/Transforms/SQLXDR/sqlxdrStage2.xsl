<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:myns="urn:schemas-microsoft-com:xml-data">
	<xsl:output method="xml" version="1.0" encoding="UTF-8"/>
	<!--xsl:namespace-alias stylesheet-prefix="myns" result-prefix="#default"/-->
	<xsl:template match="/">
		<xsl:apply-templates/>
	</xsl:template>
	<xsl:template match="myns:Schema">
		<Schema xmlns="urn:schemas-microsoft-com:xml-data" xmlns:dt="urn:schemas-microsoft-com:datatypes">
			<xsl:for-each select="@*">
				<xsl:copy/>
			</xsl:for-each>
			<xsl:for-each select="myns:ElementType">
				<ElementType>
					<xsl:for-each select="@*">
						<xsl:copy/>
					</xsl:for-each>
					<xsl:for-each select="myns:element">
						<element>
							<xsl:for-each select="@*">
								<xsl:copy/>
							</xsl:for-each>
						</element>
					</xsl:for-each>
					<xsl:for-each select="myns:AttributeType">
						<AttributeType>
							<xsl:for-each select="@*">
								<xsl:copy/>
							</xsl:for-each>
						</AttributeType>
					</xsl:for-each>
					<xsl:for-each select="myns:attribute">
						<attribute>
							<xsl:for-each select="@*">
								<xsl:copy/>
							</xsl:for-each>
						</attribute>
					</xsl:for-each>
				</ElementType>
			</xsl:for-each>
			<xsl:for-each select="myns:ElementType/myns:ElementType">
				<ElementType>
					<xsl:for-each select="@*">
						<xsl:copy/>
					</xsl:for-each>
					<xsl:for-each select="myns:element">
						<element>
							<xsl:for-each select="@*">
								<xsl:copy/>
							</xsl:for-each>
						</element>
					</xsl:for-each>
					<xsl:for-each select="myns:AttributeType">
						<AttributeType>
							<xsl:for-each select="@*">
								<xsl:copy/>
							</xsl:for-each>
						</AttributeType>
					</xsl:for-each>
					<xsl:for-each select="myns:attribute">
						<attribute>
							<xsl:for-each select="@*">
								<xsl:copy/>
							</xsl:for-each>
						</attribute>
					</xsl:for-each>
				</ElementType>
			</xsl:for-each>
		</Schema>
	</xsl:template>
</xsl:stylesheet>
