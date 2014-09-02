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
							<xsl:variable name="soFarAfter">The phoneme(s) found so far are '</xsl:variable>
							<xsl:variable name="soFarBefore">'.</xsl:variable>
							<xsl:variable name="iCharNum" select="substring-before(substring-after(.,'The missing phonetic shape is at or near character number '),':')"/>
							<xsl:variable name="sPhonemesFoundSoFar" select="substring-before(substring-after(., $soFarAfter), $soFarBefore)"/>
							<xsl:variable name="wholeAfter">Failure to translate shape '</xsl:variable>
							<xsl:variable name="partAfter">: '</xsl:variable>
							<xsl:variable name="partBefore">'.</xsl:variable>
							<xsl:choose>
								<xsl:when test="contains(.,'of rule') and contains(.,'mrule')">
									<xsl:variable name="wholeBefore">' of rule </xsl:variable>
									<xsl:variable name="sFormWithPlusSigns" select="substring-before(substring-after(., $wholeAfter), $wholeBefore)"/>
									<xsl:call-template name="ShowLoadErrorMessage">
										<xsl:with-param name="sForm" select="translate($sFormWithPlusSigns,'+','')"/>
										<xsl:with-param name="iCharNum">
											<xsl:choose>
												<xsl:when test="substring($sFormWithPlusSigns,2,1)='+'">
													<xsl:value-of select="number($iCharNum) - 1"/>
												</xsl:when>
												<xsl:otherwise>
													<xsl:value-of select="$iCharNum"/>
												</xsl:otherwise>
											</xsl:choose>
										</xsl:with-param>
										<xsl:with-param name="sFormPart" select="translate(substring-before(substring-after(., $partAfter), $partBefore),'+','')"/>
										<xsl:with-param name="sPhonemesFoundSoFar" select="$sPhonemesFoundSoFar"/>
										<xsl:with-param name="sHvo">
											<xsl:variable name="sMruleHvoWithQuotes" select="substring-before(substring-after(.,' of rule '),' into a phonetic shape using character table ')"/>
											<xsl:value-of select="substring($sMruleHvoWithQuotes,7,string-length($sMruleHvoWithQuotes)-7)"/>
										</xsl:with-param>
									</xsl:call-template>
								</xsl:when>
								<xsl:when test="contains(.,' of lexical entry ')">
									<xsl:variable name="wholeBefore">' of lexical entry </xsl:variable>
									<xsl:call-template name="ShowLoadErrorMessage">
										<xsl:with-param name="sForm" select="substring-before(substring-after(., $wholeAfter), $wholeBefore)"/>
										<xsl:with-param name="iCharNum" select="$iCharNum"/>
										<xsl:with-param name="sFormPart" select="substring-before(substring-after(., $partAfter), $partBefore)"/>
										<xsl:with-param name="sPhonemesFoundSoFar" select="$sPhonemesFoundSoFar"/>
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
		<xsl:param name="sPhonemesFoundSoFar"/>
		<xsl:param name="sHvo"/>
		<li>
			<xsl:text>There is at least one undefined phoneme in the form "</xsl:text>
			<xsl:value-of select="$sForm"/>
			<xsl:text>".  The following phonemes were parsed: "</xsl:text>
			<xsl:value-of select="$sPhonemesFoundSoFar"/>
			<xsl:text>". The problem begins with character/diacritic number </xsl:text>
			<xsl:value-of select="$iCharNum"/>
			<xsl:text> -- that is, in the part of the form "</xsl:text>
			<xsl:value-of select="$sFormPart"/>
			<xsl:text>". Please make sure all phonemes in the form have been defined.  The Hermit Crab parser will ignore this entry until it is fixed.</xsl:text>
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
	<xsl:template name="ShowAnyDataIssues">
		<xsl:variable name="issues" select="DataIssues/NatClassPhonemeMismatch"/>
		<xsl:if test="count($issues)&gt;0">
		<div style="color:red">
			<p>
				<xsl:text>The following data issue</xsl:text>
				<xsl:choose>
					<xsl:when test="count($issues) &gt; 1">
						<xsl:text>s were</xsl:text>
					</xsl:when>
					<xsl:otherwise>
						<xsl:text> was</xsl:text>
					</xsl:otherwise>
				</xsl:choose>
				<xsl:text> found that may affect how the parser works.  When the Hermit Crab parser uses a natural class during its synthesis process, the natural class will use the phonological features which are the intersection of the features of all the phonemes in the class while trying to see if a segment matches the natural class.  The implied phonological features are shown for each class below and mean that it will match any of the predicted phonemes shown.  (If the implied features field is blank, then it will match *all* phonemes.)  For each of the natural classes shown below, the set of predicted phonemes is not the same as the set of actual  phonemes.  You will need to rework your phonological feature system and the assignment of these features to phonemes to make it be correct.</xsl:text>
			</p>
<!--			<ul>-->
				<xsl:for-each select="$issues">
<!--					<li>-->
					<table>
							<tr valign="top">
								<td>
									<table>
										<tr style="color:red">
											<td>
												<xsl:value-of select="ClassName"/>
											</td>
										</tr>
										<tr style="color:red">
											<td>
												<xsl:text>[</xsl:text>
												<xsl:value-of select="ClassAbbeviation"/>
												<xsl:text>]</xsl:text>
											</td>
										</tr>
									</table>
								</td>
								<td>
									<table>
										<tr style="color:red">
											<td>Implied Features</td>
											<td>
												<xsl:value-of select="ImpliedPhonologicalFeatures"/>
											</td>
										</tr>
										<tr style="color:red">
											<td>Predicted Phonemes</td>
											<td>
												<xsl:value-of select="PredictedPhonemes"/>
											</td>
										</tr>
										<tr style="color:red">
											<td>Actual Phonemes</td>
											<td>
												<xsl:value-of select="ActualPhonemes"/>
											</td>
										</tr>
									</table>
								</td>
							</tr>
						</table>
<!--					</li>-->
				</xsl:for-each>
<!--			</ul>-->
			</div>
		</xsl:if>
	</xsl:template>
</xsl:stylesheet>
