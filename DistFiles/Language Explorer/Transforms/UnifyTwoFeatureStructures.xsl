<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="xml" version="1.0" encoding="UTF-8" indent="yes"/>
	<!--
================================================================
XSLT template to unify two feature structures.
================================================================
Revision History is at the end of this file.

The expected format of the feature structures follow this DTD:

<!ELEMENT fs (feature*)>
<!ATTLIST fs
	id CDATA #IMPLIED
>
<!ELEMENT feature (name, value)>
<!ELEMENT name (#PCDATA)>
<!ELEMENT value (#PCDATA | fs)*>
-->
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputFeatureValue
	Output the value of the given feature.  If it's atomic, output the atomic value otherwise copy the embedded <fs>
		Parameters: value = value to output
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputFeatureValue">
		<xsl:param name="value"/>
		<xsl:choose>
			<xsl:when test="$value/fs">
				<xsl:copy-of select="$value/fs"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="$value"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
UnifyTwoFeatureStructures
	Perform the unification operation on two feature structures.
	The  <fs> element which is put into the output is the unification of the two feature structures.
		Parameters: firstFS = first feature structure
							 secondFS = second feature structure
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="UnifyTwoFeatureStructures">
		<xsl:param name="firstFS"/>
		<xsl:param name="secondFS"/>
		<xsl:param name="bIsTopLevel">Y</xsl:param>
		<xsl:param name="sTopLevelId"/>
		<fs>
			<xsl:if test="$bIsTopLevel='Y'">
				<xsl:attribute name="id"><xsl:value-of select="$firstFS/@id"/><xsl:text>and</xsl:text><xsl:value-of select="$secondFS/@id"/><xsl:text>Unified</xsl:text></xsl:attribute>
			</xsl:if>
			<!-- loop through the features of both feature structures at same time, sorted by name -->
			<xsl:for-each select="$firstFS/feature | $secondFS/feature">
				<xsl:sort select="name"/>
				<!-- get name of this feature -->
				<xsl:variable name="sName">
					<xsl:value-of select="name"/>
				</xsl:variable>
				<!-- get this feature if it's in the first feature structure -->
				<xsl:variable name="f1SameName" select="$firstFS/feature[name=$sName]"/>
				<!-- get this feature if it's in the second feature structure  -->
				<xsl:variable name="f2SameName" select="$secondFS/feature[name=$sName]"/>
				<xsl:choose>
					<xsl:when test="$f1SameName and $f2SameName">
						<!-- both feature1 and feature2 have this feature name -->
						<xsl:if test="ancestor::fs[@id=$sTopLevelId]">
							<!-- only need to do this for the feature in the first feature structure -->
							<feature>
								<name>
									<xsl:value-of select="$sName"/>
								</name>
								<xsl:choose>
									<xsl:when test="$f1SameName/value/fs and $f2SameName/value/fs">
										<!-- both have nested feature structure -->
										<value>
											<xsl:call-template name="UnifyTwoFeatureStructures">
												<xsl:with-param name="firstFS" select="$f1SameName/value/fs"/>
												<xsl:with-param name="secondFS" select="$f2SameName/value/fs"/>
												<xsl:with-param name="bIsTopLevel">N</xsl:with-param>
												<xsl:with-param name="sTopLevelId" select="$sTopLevelId"/>
											</xsl:call-template>
										</value>
									</xsl:when>
									<xsl:when test="$f1SameName/value=$f2SameName/value">
										<!-- both features have the same value -->
										<value>
											<xsl:value-of select="$f1SameName/value"/>
										</value>
									</xsl:when>
									<xsl:otherwise>
										<!-- there's a value conflict; output failure element and the values -->
										<failure>
											<value>
												<xsl:call-template name="OutputFeatureValue">
													<xsl:with-param name="value" select="$f1SameName/value"/>
												</xsl:call-template>
											</value>
											<value>
												<xsl:call-template name="OutputFeatureValue">
													<xsl:with-param name="value" select="$f2SameName/value"/>
												</xsl:call-template>
											</value>
										</failure>
									</xsl:otherwise>
								</xsl:choose>
							</feature>
						</xsl:if>
					</xsl:when>
					<xsl:otherwise>
						<!-- only one of the features has this feature -->
						<feature>
							<name>
								<xsl:value-of select="name"/>
							</name>
							<value>
								<xsl:call-template name="OutputFeatureValue">
									<xsl:with-param name="value" select="value"/>
								</xsl:call-template>
							</value>
						</feature>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:for-each>
		</fs>
	</xsl:template>
</xsl:stylesheet>
<!--
================================================================
Revision History
- - - - - - - - - - - - - - - - - - -
17-Jun-2005    Andy Black  Initial draft
================================================================
 -->
