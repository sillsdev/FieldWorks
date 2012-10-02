<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="xml" version="1.0" encoding="UTF-8" indent="yes"/>
  <!--
================================================================
Test unification via XSLT
  Input:    Pairs of tests to apply
  Output: should be identical to input
================================================================
Revision History is at the end of this file.

- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Preamble
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
<xsl:include href="../../../../DistFiles/Language Explorer/Transforms/UnifyTwoFeatureStructures.xsl"/>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Main template
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="/">
		<listOfFeatureStructurePairsToUnify>
			<xsl:for-each select="//section">
				<section>
					<title>
						<xsl:value-of select="title"/>
					</title>
					<xsl:for-each select="pair">
						<pair>
							<xsl:copy-of select="@*"/>
							<xsl:copy-of select="feature1"/>
							<xsl:copy-of select="feature2"/>
							<featureExpectedResult>
								<xsl:call-template name="UnifyTwoFeatureStructures">
									<xsl:with-param name="firstFS" select="feature1/fs"/>
									<xsl:with-param name="secondFS" select="feature2/fs"/>
									<xsl:with-param name="sTopLevelId" select="feature1/fs/@id"/>
								</xsl:call-template>
							</featureExpectedResult>
						</pair>
					</xsl:for-each>
				</section>
			</xsl:for-each>
		</listOfFeatureStructurePairsToUnify>
	</xsl:template>
</xsl:stylesheet>
<!--
================================================================
Revision History
- - - - - - - - - - - - - - - - - - -
17-Jun-2005    Andy Black  Initial draft
================================================================
 -->
