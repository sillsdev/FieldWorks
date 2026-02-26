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
								<xsl:when test="@type = 'invalid-shape'">
									<xsl:call-template name="ShowInvalidShapeErrorMessage">
										<xsl:with-param name="sForm" select="translate(Form, '+', '')"/>
										<xsl:with-param name="iCharNum" select="Position + 1"/>
										<xsl:with-param name="sFormPart" select="translate(substring(Form, Position + 1), '+', '')"/>
										<xsl:with-param name="sPhonemesFoundSoFar" select="translate(substring(Form, 1, Position), '+', '')"/>
										<xsl:with-param name="sHvo" select="Hvo"/>
									</xsl:call-template>
								</xsl:when>
								<xsl:when test="@type = 'invalid-phoneme'">
									<li>
										<xsl:text>The phoneme "</xsl:text>
										<xsl:value-of select="Name" />
										<xsl:text>" does not have a grapheme defined for the default vernacular writing system. </xsl:text>
										<span style="cursor:pointer; text-decoration:underline">
											<xsl:attribute name="id">
												<xsl:value-of select="Hvo"/>
											</xsl:attribute>
											<xsl:text>(Click here to see the phoneme.)</xsl:text>
										</span>
									</li>
								</xsl:when>
								<xsl:when test="@type = 'duplicate-grapheme'">
									<li>
										<xsl:text>The phoneme "</xsl:text>
										<xsl:value-of select="Name" />
										<xsl:text>" has the same grapheme as another phoneme. </xsl:text>
										<span style="cursor:pointer; text-decoration:underline">
											<xsl:attribute name="id">
												<xsl:value-of select="Hvo"/>
											</xsl:attribute>
											<xsl:text>(Click here to see the phoneme.)</xsl:text>
										</span>
									</li>
								</xsl:when>
								<xsl:when test="@type = 'invalid-affix-process'">
									<li>
										<xsl:text>The </xsl:text>
										<xsl:choose>
											<xsl:when test="InvalidLhs = 'True'">
												<xsl:text>left hand side</xsl:text>
											</xsl:when>
											<xsl:otherwise>
												<xsl:text>right hand side</xsl:text>
											</xsl:otherwise>
										</xsl:choose>
										<xsl:text> of the affix process rule "</xsl:text>
										<xsl:value-of select="Form" />
										<xsl:text>" contains an invalid natural class or phoneme. </xsl:text>
										<span style="cursor:pointer; text-decoration:underline">
											<xsl:attribute name="id">
												<xsl:value-of select="Hvo"/>
											</xsl:attribute>
											<xsl:text>(Click here to see the entry.)</xsl:text>
										</span>
									</li>
								</xsl:when>
								<xsl:when test="@type = 'invalid-environment'">
									<li>
										<xsl:text>The environment "</xsl:text>
										<xsl:value-of select="Env" />
										<xsl:text>" in allomorph "</xsl:text>
										<xsl:value-of select="Form" />
										<xsl:text>" is invalid. The environment will be ignored. Reason: </xsl:text>
										<xsl:value-of select="Reason" />
										<xsl:text> </xsl:text>
										<span style="cursor:pointer; text-decoration:underline">
											<xsl:attribute name="id">
												<xsl:value-of select="Hvo"/>
											</xsl:attribute>
											<xsl:text>(Click here to see the entry.)</xsl:text>
										</span>
									</li>
								</xsl:when>
								<xsl:when test="@type = 'invalid-redup-form'">
									<li>
										<xsl:text>The reduplication form "</xsl:text>
										<xsl:value-of select="Form" />
										<xsl:text>" is invalid. Reason: </xsl:text>
										<xsl:value-of select="Reason" />
										<xsl:text> </xsl:text>
										<span style="cursor:pointer; text-decoration:underline">
											<xsl:attribute name="id">
												<xsl:value-of select="Hvo"/>
											</xsl:attribute>
											<xsl:text>(Click here to see the entry.)</xsl:text>
										</span>
									</li>
								</xsl:when>
								<xsl:otherwise>
									<!-- Do not expect any others to happen, but just in case, we show them in all their HC glory -->
									<li><xsl:value-of select="."/></li>
								</xsl:otherwise>
							</xsl:choose>
						</xsl:for-each>
					</ul>
				</div>
			</xsl:if>
		</xsl:if>
	</xsl:template>

	<xsl:template name="ShowInvalidShapeErrorMessage">
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
			<xsl:text>". Please make sure all phonemes in the form have been defined. The Hermit Crab parser will ignore this entry until it is fixed. </xsl:text>
			<span style="cursor:pointer; text-decoration:underline">
				<xsl:attribute name="id">
					<xsl:value-of select="$sHvo"/>
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
				<xsl:text> found that may affect how the synthesis works.  When the Hermit Crab synthesize uses a natural class during its synthesis process, the natural class will use the phonological features which are the intersection of the features of all the phonemes in the class while trying to see if a segment matches the natural class.  The implied phonological features are shown for each class below and mean that it will match any of the predicted phonemes shown.  (If the implied features field is blank, then it will match *all* phonemes.)  For each of the natural classes shown below, the set of predicted phonemes is not the same as the set of actual  phonemes.  You will need to rework your phonological feature system and the assignment of these features to phonemes to make it be correct.</xsl:text>
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
