<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
	<xsl:param name="prmHCTraceLoadErrorFile" select="''"/>

	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		ShowAnyLoadErrors
		Show all load errors
		Parameters: none
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="ShowAnyLoadErrors">
		<xsl:if test="string-length($prmHCTraceLoadErrorFile) &gt; 0">
			<xsl:variable name="HCLoadErrors" select="document($prmHCTraceLoadErrorFile)/LoadErrors/LoadError"/>
			<xsl:if test="$HCLoadErrors">
				<div style="color:red">
					<p>
						<xsl:text>The following error</xsl:text>
						<xsl:choose>
							<xsl:when test="count($HCLoadErrors) &gt; 1">
								<xsl:text>s were</xsl:text>
							</xsl:when>
							<xsl:otherwise>
								<xsl:text> was</xsl:text>
							</xsl:otherwise>
						</xsl:choose>
						<xsl:text> found while the parser was loading the information getting ready to parse words.</xsl:text>
					</p>
					<ul>
						<xsl:for-each select="$HCLoadErrors">
							<xsl:choose>
								<xsl:when test="contains(.,'of rule') and contains(.,'mrule')">
									<xsl:variable name="sFormWithPlusSigns" select="substring-before(substring-after(.,'Failure to translate shape '),' of rule ')"/>
									<xsl:call-template name="ShowLoadErrorMessage">
										<xsl:with-param name="sForm" select="translate($sFormWithPlusSigns,'+','')"/>
										<xsl:with-param name="iCharNum">
											<xsl:variable name="iCharNum" select="substring-before(substring-after(.,'The missing phonetic shape is at or near character number '),':')"/>
											<xsl:choose>
												<xsl:when test="substring($sFormWithPlusSigns,2,1)='+'">
													<xsl:value-of select="number($iCharNum) - 1"/>
												</xsl:when>
												<xsl:otherwise>
													<xsl:value-of select="$iCharNum"/>
												</xsl:otherwise>
											</xsl:choose>
										</xsl:with-param>
										<xsl:with-param name="sFormPart" select="translate(substring-before(substring-after(.,': '),'.'),'+','')"/>
										<xsl:with-param name="sHvo">
											<xsl:variable name="sMruleHvoWithQuotes" select="substring-before(substring-after(.,' of rule '),' into a phonetic shape using character table ')"/>
											<xsl:value-of select="substring($sMruleHvoWithQuotes,7,string-length($sMruleHvoWithQuotes)-7)"/>
										</xsl:with-param>
									</xsl:call-template>
								</xsl:when>
								<xsl:when test="contains(.,' of lexical entry ')">
									<xsl:call-template name="ShowLoadErrorMessage">
										<xsl:with-param name="sForm" select="substring-before(substring-after(.,'Failure to translate shape '),' of lexical entry ')"/>
										<xsl:with-param name="iCharNum" select="substring-before(substring-after(.,'The missing phonetic shape is at or near character number '),':')"/>
										<xsl:with-param name="sFormPart" select="substring-before(substring-after(.,': '),'.')"/>
										<xsl:with-param name="sHvo">
											<xsl:variable name="sLexHvoWithQuotes" select="substring-before(substring-after(.,' of lexical entry '),' into a phonetic shape using character table ')"/>
											<xsl:value-of select="substring($sLexHvoWithQuotes,5,string-length($sLexHvoWithQuotes)-5)"/>
										</xsl:with-param>
									</xsl:call-template>
								</xsl:when>
								<xsl:otherwise>
									<!-- Do not expect any others to happen, but just in case, we show them in all their HC glory -->
									<xsl:value-of select="."/>
								</xsl:otherwise>
							</xsl:choose>
						</xsl:for-each>
					</ul>
				</div>
			</xsl:if>
		</xsl:if>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		ShowLoadErrorMessage
		Show a given load ereror message
		Parameters: none
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="ShowLoadErrorMessage">
		<xsl:param name="sForm"/>
		<xsl:param name="iCharNum"/>
		<xsl:param name="sFormPart"/>
		<xsl:param name="sHvo"/>
		<li>
			<xsl:text>Somewhere in the form </xsl:text>
			<xsl:value-of select="$sForm"/>
			<xsl:text>, beginning with character number </xsl:text>
			<xsl:value-of select="$iCharNum"/>
			<xsl:text> -- that is, in the part of the form </xsl:text>
			<xsl:value-of select="$sFormPart"/>
			<xsl:text> -- there is at least one undefined phoneme.  Please make sure all phonemes in the form have been defined.  The Hermit Crab parser will ignore this entry until it is fixed. </xsl:text>
			<span style="cursor:hand; text-decoration:underline">
				<xsl:attribute name="onclick">
					<xsl:text>JumpToToolBasedOnHvo(</xsl:text>
					<xsl:variable name="sMruleHvoWithQuotes" select="substring-before(substring-after(.,' of rule '),' into a phonetic shape using character table ')"/>
					<xsl:value-of select="$sHvo"/>
					<xsl:text>)</xsl:text>
				</xsl:attribute>
				<xsl:text>(Click here to see the lexical entry.)</xsl:text>
			</span>
		</li>
	</xsl:template>
</xsl:stylesheet>
