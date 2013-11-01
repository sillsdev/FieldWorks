<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
 xmlns:msxslt="urn:schemas-microsoft-com:xslt"
 xmlns:saxon="http://icl.com/saxon"
 xmlns:exsl="http://exslt.org/common"
 exclude-result-prefixes="exsl saxon msxslt">
	<xsl:output method="xml" version="1.0" encoding="utf-8" indent="yes"/>
	<!--
================================================================
M3 to Morphological Sketch mapper for Stage 1.
  Input:    XML output from M3SketchSvr which has been passed through CleanFWDump.xslt
  Output: Morphological Sketch file

================================================================
Revision History is at the end of this file.

- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Preamble
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<!-- Using keys instead of IDs (so no DTD or XSD required) -->
	<xsl:key name="AdhocGroup" match="MoAdhocProhibGr/Members" use="@dst"/>
	<xsl:key name="BearableFeaturesID" match="FsClosedFeature" use="@Id"/>
	<xsl:key name="BoundaryMarkersID" match="PhBdryMarker" use="@Id"/>
	<xsl:key name="ContextsID" match="PhSequenceContext | PhIterationContext | PhSimpleContextBdry | PhSimpleContextSeg | PhSimpleContextNC" use="@Id"/>
	<xsl:key name="FsClosedFeaturesID" match="FsClosedFeature" use="@Id"/>
	<xsl:key name="FsComplexFeaturesID" match="FsComplexFeature" use="@Id"/>
	<xsl:key name="FsFeatStrucsID" match="FsFeatStruc" use="@Id"/>
	<xsl:key name="FsSymFeatValsID" match="FsSymFeatVal" use="@Id"/>
	<xsl:key name="ExceptionFeaturesID" match="ProdRestrict/CmPossibility" use="@Id"/>
	<xsl:key name="InflectableFeatsID" match="FsClosedFeature | FsComplexFeature" use="@Id"/>
	<xsl:key name="InflectionClassesID" match="MoInflClass" use="@Id"/>
	<xsl:key name="InflAffixMsaID" match="MoInflAffMsa" use="@Id"/>
	<xsl:key name="LexAllos" match="LexEntry/AlternateForms | LexEntry/LexemeForm" use="@dst"/>
	<xsl:key name="LexEntryMsa" match="LexEntry/MorphoSyntaxAnalysis" use="@dst"/>
	<xsl:key name="LexEntrySense" match="LexEntry/Sense" use="@dst"/>
	<xsl:key name="MoFormsID" match="MoAffixAllomorph |  MoStemAllomorph" use="@Id"/>
	<xsl:key name="MorphTypeID" match="MoMorphType" use="@Id"/>
	<xsl:key name="MsaID" match="MoStemMsa | MoInflAffMsa | MoDerivAffMsa | MoUnclassifiedAffixMsa " use="@Id"/>
	<xsl:key name="NaturalClassesID" match="PhNCSegments | PhNCFeatures" use="@Id"/>
	<xsl:key name="POSID" match="PartOfSpeech" use="@Id"/>
	<xsl:key name="PhEnvironmentsID" match="PhEnvironment" use="@Id"/>
	<xsl:key name="PhFeatureConstraintsID" match="PhFeatureConstraint" use="@Id"/>
	<xsl:key name="PhonemesID" match="PhPhoneme" use="@Id"/>
	<xsl:key name="PhonRuleFeatsID" match="PhonRuleFeat" use="@Id"/>
	<xsl:key name="SensesID" match="LexSense" use="@Id"/>
	<xsl:key name="SensesMsa" match="LexSense" use="@Msa"/>
	<xsl:key name="SlotsID" match="MoInflAffixSlot" use="@Id"/>
	<xsl:key name="StemNamesID" match="MoStemName" use="@Id"/>
	<!-- Using a parameter to set language of sketch; default is English (en)  -->
	<xsl:param name="sSketchLangICULocale">
		<xsl:text>en</xsl:text>
	</xsl:param>
	<!-- Using a parameter to set maximum number of morphemes to show in each subsection of the appendices.  Default is to show all.  -->
	<xsl:param name="prmIMaxMorphsInAppendices">-1</xsl:param>
	<!-- Using a parameter to set  the analysis language's word(s) for the maximum number of morphemes to show in each subsection of the appendices.  Default is to show all.  -->
	<xsl:param name="prmSMaxMorphsInAppendices"/>
	<!-- using a special parameter for date/time tag -->
	<xsl:param name="prmSDateTime"/>
	<!-- param for unclassified or "Not Sure" msa cases -->
	<xsl:param name="prmSUnclassifiedTextToUse">
		<xsl:text>???</xsl:text>
	</xsl:param>
	<!-- need to set vernacular font size, especially for non-roman scripts -->
	<xsl:param name="prmVernacularFontSize"/>
	<!-- need to set analysis/gloss font size, especially for non-roman scripts -->
	<xsl:param name="prmGlossFontSize"/>
	<!--		<xsl:text>740664001</xsl:text>-->
	<xsl:variable name="bVernRightToLeft" select="/M3Dump/LangProject/VernWss/WritingSystem/@RightToLeft"/>
	<xsl:variable name="bGlossRightToLeft" select="/M3Dump/LangProject/AnalysisWss/WritingSystem/@RightToLeft"/>
	<xsl:param name="sWordWorksTransformPath">
		<xsl:text>C:/fw/DistFiles/Language Explorer/Transforms</xsl:text>
	</xsl:param>
	<xsl:variable name="sEnvironmentPositionTag" select="'.Position'"/>
	<xsl:variable name="PartsOfSpeech" select="/M3Dump/PartsOfSpeech"/>
	<xsl:variable name="POSes" select="$PartsOfSpeech//PartOfSpeech"/>
	<xsl:variable name="toplevelPOSes" select="$PartsOfSpeech/PartOfSpeech[not($POSes/SubPossibilities/@dst=@Id)]"/>
	<xsl:variable name="MoInflAffixTemplates" select="$POSes/AffixTemplates/MoInflAffixTemplate"/>
	<xsl:variable name="MorphoSyntaxAnalyses" select="/M3Dump/Lexicon/MorphoSyntaxAnalyses"/>
	<xsl:variable name="LexEntries" select="/M3Dump/Lexicon/Entries/LexEntry"/>
	<xsl:variable name="MoMorphTypes" select="/M3Dump/MoMorphTypes/MoMorphType"/>
	<xsl:variable name="MoStemAllomorphs" select="/M3Dump/Lexicon/Allomorphs/MoStemAllomorph"/>
	<xsl:variable name="MoAffixAllomorphs" select="/M3Dump/Lexicon/Allomorphs/MoAffixAllomorph"/>
	<xsl:variable name="PhoneEnvs" select="/M3Dump/Lexicon/Allomorphs//PhoneEnv"/>
	<xsl:variable name="Positions" select="/M3Dump/Lexicon/Allomorphs/MoAffixAllomorph/Position"/>
	<xsl:variable name="MoInflAffMsas" select="$MorphoSyntaxAnalyses/MoInflAffMsa"/>
	<xsl:variable name="MoDerivAffMsas" select="$MorphoSyntaxAnalyses/MoDerivAffMsa"/>
	<xsl:variable name="MoInflClasses" select="$POSes/InflectionClasses//MoInflClass"/>
	<xsl:variable name="MoStemNames" select="$POSes/StemNames/MoStemName"/>
	<xsl:variable name="MoAdhocProhibGrs" select="/M3Dump/MoMorphData/AdhocCoProhibitions//MoAdhocProhibGr"/>
	<xsl:variable name="MoMorphAdhocProhibs" select="/M3Dump/MoMorphData/AdhocCoProhibitions/MoMorphAdhocProhib"/>
	<xsl:variable name="MoAlloAdhocProhibs" select="/M3Dump/MoMorphData/AdhocCoProhibitions/MoAlloAdhocProhib"/>
	<xsl:variable name="FsFeatStrucTypes" select="/M3Dump//Types/FsFeatStrucType[Name!='Phon']"/>
	<xsl:variable name="ProdRestricts" select="/M3Dump//ProdRestrict"/>
	<xsl:variable name="InflectionClasses" select="/M3Dump//InflectionClasses"/>
	<xsl:variable name="PhEnvironments" select="/M3Dump/PhPhonData/Environments/PhEnvironment"/>
	<xsl:variable name="PhPhonemes" select="/M3Dump/PhPhonData/PhonemeSets/PhPhonemeSet/Phonemes/PhPhoneme"/>
	<!-- variable used to tell if the Feature System section should be included or not.  May need to add check for other types of features. -->
	<xsl:variable name="FeatureSystem" select="/M3Dump/FeatureSystem/Features/FsClosedFeature | /M3Dump/FeatureSystem/Features/FsComplexFeature"/>
	<!-- variable used to tell if the Phonological Rules section should be included or not. -->
	<xsl:variable name="PhonologicalRules" select="/M3Dump/PhPhonData/PhonRules/PhRegularRule | //PhonRules/PhMetathesisRule"/>
	<!-- variable used to tell if the Phonological Feature System section should be included or not. -->
	<xsl:variable name="PhonologicalFeatureSystem" select="/M3Dump/PhFeatureSystem/Features/FsClosedFeature"/>
	<!-- variable used to tell if the exception "features" section should be included or not. -->
	<xsl:variable name="ProdRestrict" select="$ProdRestricts/CmPossibility"/>
	<!-- included stylesheets (i.e. things common to other style sheets) -->
	<xsl:include href="MorphTypeGuids.xsl"/>
	<xsl:include href="BoundaryMarkerGuids.xsl"/>
	<xsl:variable name="sEmptyContent" select="'***'"/>
	<!-- variables used to tell if the residue section should be included or not -->
	<xsl:variable name="UnmarkedAffixes" select="$LexEntries[key('MsaID',MorphoSyntaxAnalysis/@dst)[name()='MoUnclassifiedAffixMsa']]"/>
	<xsl:variable name="UnderspecifiedDerivationalAffixes" select="$LexEntries[key('MsaID',MorphoSyntaxAnalysis/@dst)[name()='MoDerivAffMsa' and @FromPartOfSpeech='0' or name()='MoDerivAffMsa' and @ToPartOfSpeech='0']]"/>
	<xsl:variable name="UnderspecifiedInflectionalAffixes" select="$LexEntries[key('MsaID',MorphoSyntaxAnalysis/@dst)[name()='MoInflAffMsa' and not(@PartOfSpeech) or name()='MoInflAffMsa' and not(Slots)]]"/>
	<xsl:variable name="UnmarkedStems" select="$LexEntries[key('MsaID',MorphoSyntaxAnalysis/@dst)[name()='MoStemMsa' and @PartOfSpeech='0'] and not(
	   key('MorphTypeID',LexemeForm/@MorphType)/@Guid=$sEnclitic or
	   key('MorphTypeID',LexemeForm/@MorphType)/@Guid=$sProclitic or
	   key('MorphTypeID',LexemeForm/@MorphType)/@Guid=$sPhrase or
	   key('MorphTypeID',LexemeForm/@MorphType)/@Guid=$sDiscontiguousPhrase)]"/>
	<xsl:variable name="sUnusedExceptionFeatures">
		<xsl:call-template name="CalculateUnusedExceptionFeatures"/>
	</xsl:variable>
	<xsl:variable name="sUnusedBearableFeatures">
		<!-- not for now
			<xsl:call-template name="CalculateUnusedBearableFeatures"/>
			-->
	</xsl:variable>
	<xsl:variable name="sUnusedInflectionClassesStems">
		<xsl:call-template name="CalculateUnusedInflectionClassesStems"/>
	</xsl:variable>
	<xsl:variable name="sUnusedInflectionClassesAffixes">
		<xsl:call-template name="CalculateUnusedInflectionClassesAffixes"/>
	</xsl:variable>
	<xsl:variable name="sUnusedStemNamesStems">
		<xsl:call-template name="CalculateUnusedStemNamesStems"/>
	</xsl:variable>
	<xsl:variable name="sStemNamesWithoutFeatureSets">
		<xsl:call-template name="CalculateStemNamesWithoutFeatureSets"/>
	</xsl:variable>
	<xsl:variable name="sUnusedStemNamesAffixes">
		<xsl:call-template name="CalculateUnusedStemNamesAffixes"/>
	</xsl:variable>
	<xsl:variable name="sUnusedSlots">
		<xsl:call-template name="CalculateUnusedSlots"/>
	</xsl:variable>
	<xsl:variable name="ShowResidue" select="$MoMorphAdhocProhibs | $MoAlloAdhocProhibs | $MoAdhocProhibGrs | $UnmarkedAffixes | $UnderspecifiedDerivationalAffixes | $UnderspecifiedInflectionalAffixes | $UnmarkedStems or string-length($sUnusedExceptionFeatures) &gt; 0 or string-length($sUnusedBearableFeatures) &gt; 0 or string-length($sUnusedInflectionClassesStems) &gt; 0 or string-length($sUnusedInflectionClassesAffixes) &gt; 0 or string-length($sUnusedStemNamesStems) &gt; 0 or string-length($sUnusedStemNamesAffixes) &gt; 0 or string-length($sStemNamesWithoutFeatureSets) &gt; 0 or string-length($sUnusedSlots) &gt; 0"/>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Main template
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="/M3Dump">
		<xsl:variable name="sLangName">
			<xsl:choose>
				<xsl:when test="LangProject/Name">
					<!-- NB: this assumes that every project name begins with the Ethnologue code followed by a dash.  Do they?  -->
					<!-- no					<xsl:value-of select="substring(LangProject/CmProject/Name, 5)"/> -->
					<xsl:value-of select="LangProject/Name"/>
				</xsl:when>
				<xsl:otherwise>
					<xsl:text>Unknown Language</xsl:text>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<xsl:processing-instruction name="xml-stylesheet">
			<xsl:text>type="text/xsl" href="</xsl:text>
			<xsl:value-of select="$sWordWorksTransformPath"/>
			<xsl:text>/XLingPap1.xsl"</xsl:text>
		</xsl:processing-instruction>
		<!-- output dtd path -->
		<xsl:text disable-output-escaping="yes">&#xa;&#x3c;!DOCTYPE lingPaper SYSTEM "file://</xsl:text>
		<xsl:value-of select="$sWordWorksTransformPath"/>
		<xsl:text disable-output-escaping="yes">/XLingPap.dtd"&#x3e;&#xa;</xsl:text>
		<lingPaper version="1.12.0">
			<frontMatter>
				<title>
					<xsl:text>A Sketch of </xsl:text>
					<xsl:value-of select="$sLangName"/>
					<xsl:text> Morphology</xsl:text>
				</title>
				<author>
					<!-- ### LangProj fix needed here -->
					<xsl:text>[Insert investigators' names here]</xsl:text>
					<xsl:value-of select="@investigators"/>
				</author>
				<affiliation>[Insert affiliation here]</affiliation>
				<date>
					<!-- ### LangProj fix needed here -->
					<!--          <xsl:value-of select="@revisionDate"/> -->
					<xsl:value-of select="$prmSDateTime"/>
				</date>
				<contents showLevel="1"/>
			</frontMatter>
			<section1 id="sIntro" type="tH1">
				<secTitle>Introduction</secTitle>
				<p>
					<xsl:value-of select="$sLangName"/>
					<xsl:text> is a language spoken in </xsl:text>
					<xsl:choose>
						<xsl:when test="string-length(LangProject/MainCountry) > 0 and LangProject/MainCountry!=$sEmptyContent">
							<xsl:value-of select="LangProject/MainCountry"/>
						</xsl:when>
						<xsl:otherwise>
							<xsl:text>[insert country here]</xsl:text>
						</xsl:otherwise>
					</xsl:choose>
					<xsl:text>. This paper gives a preliminary sketch of </xsl:text>
					<xsl:value-of select="$sLangName"/>
					<xsl:text> morphology following a basic item-and-arrangement model. The sketch covers the following topics:</xsl:text>
				</p>
				<ul>
					<li>
						Phonemes in section <sectionRef sec="sPhonemes"/>.
					</li>
					<li>
						<xsl:text>Morpheme types in section </xsl:text>
						<sectionRef sec="sTypes"/>
						<xsl:text>.</xsl:text>
					</li>
					<li>
						<xsl:text>Word categories in section </xsl:text>
						<sectionRef sec="sCategories"/>
						<xsl:text>.</xsl:text>
					</li>
					<li>
						<xsl:text>Inflection in section </xsl:text>
						<sectionRef sec="sInflection"/>
						<xsl:text>.</xsl:text>
					</li>
					<li>
						<xsl:text>Derivation in section </xsl:text>
						<sectionRef sec="sDerivation"/>
						<xsl:text>.</xsl:text>
					</li>
					<li>
						<xsl:text>Clitics in section </xsl:text>
						<sectionRef sec="sClitics"/>
						<xsl:text>.</xsl:text>
					</li>
					<li>
						<xsl:text>Compounding in section </xsl:text>
						<sectionRef sec="sCompounding"/>
						<xsl:text>.</xsl:text>
					</li>
					<xsl:if test="$FeatureSystem">
						<li>
							<xsl:text>Morpho-syntactic feature system in section </xsl:text>
							<sectionRef sec="sMSFeatureSystem"/>
							<xsl:text>.</xsl:text>
						</li>
					</xsl:if>
					<li>
						<xsl:text>Allomorphy in section </xsl:text>
						<sectionRef sec="sAllomorphy"/>
						<xsl:text>.</xsl:text>
					</li>
					<li>
						<xsl:text>Natural classes in section </xsl:text>
						<sectionRef sec="sNaturalClasses"/>
						<xsl:text>.</xsl:text>
					</li>
					<xsl:if test="$PhonologicalRules">
						<li>
							<xsl:text>Phonological rules in section </xsl:text>
							<sectionRef sec="sPhonologicalRules"/>
							<xsl:text>.</xsl:text>
						</li>
					</xsl:if>
					<xsl:if test="$PhonologicalFeatureSystem">
						<li>
							<xsl:text>Phonological feature system in section </xsl:text>
							<sectionRef sec="sPhFeatureSystem"/>
							<xsl:text>.</xsl:text>
						</li>
					</xsl:if>
					<xsl:if test="$ShowResidue">
						<li>
							<xsl:text>Residue in section </xsl:text>
							<sectionRef sec="sResidue"/>
							<xsl:text>.</xsl:text>
						</li>
					</xsl:if>
				</ul>
				<p>
					<xsl:text>There also are two appendices.  The first, Appendix </xsl:text>
					<appendixRef app="aMorphsByType"/>
					<xsl:text>, lists morphemes arranged by morpheme type.  The second, Appendix </xsl:text>
					<appendixRef app="aMorphsByCat"/>
					<xsl:text>, lists morphemes arranged by lexical category.</xsl:text>
				</p>
			</section1>
			<section1 id="sPhonemes" type="tH1">
				<secTitle>Phonemes</secTitle>
				<xsl:variable name="iPhonemeCount" select="count($PhPhonemes)"/>
				<xsl:choose>
					<xsl:when test="$iPhonemeCount = 0">
						<p>
							<xsl:text>There are no phonemes in this analysis of </xsl:text>
							<xsl:value-of select="$sLangName"/>
							<xsl:text>(!).</xsl:text>
						</p>
					</xsl:when>
					<xsl:otherwise>
						<xsl:if test="$PhPhonemes/Codes[count(PhCode)=0]">
							<p>
								<xsl:text>WARNING: there is at least one phoneme which does not have a representation.  This should be corrected.</xsl:text>
								<table type="tLeftOffset" border="1">
									<tr>
										<th>Name</th>
										<th>Description</th>
									</tr>
									<xsl:for-each select="$PhPhonemes[Codes[count(PhCode)=0]]">
										<tr>
											<td>
												<langData lang="lVern">
													<xsl:call-template name="OutputPotentiallyBlankItemInTable">
														<xsl:with-param name="item" select="Name"/>
													</xsl:call-template>
												</langData>
											</td>
											<td>
												<xsl:call-template name="OutputPotentiallyBlankItemInTable">
													<xsl:with-param name="item" select="Description"/>
												</xsl:call-template>
											</td>
										</tr>
									</xsl:for-each>
								</table>
							</p>
						</xsl:if>
						<xsl:choose>
							<xsl:when test="$PhonologicalFeatureSystem">
								<p>
									<xsl:call-template name="OutputPhonemeCount">
										<xsl:with-param name="sLangName" select="$sLangName"/>
										<xsl:with-param name="iPhonemeCount" select="$iPhonemeCount"/>
									</xsl:call-template>
									<xsl:text>.</xsl:text>
								</p>
								<section2 id="sPhonemesPhonemeFeatureChart" type="tH2">
									<secTitle>Phoneme-Feature Matrix</secTitle>
									<p>The following matrix shows the features associated with each phoneme in this analysis of <xsl:value-of select="$sLangName"/>.</p>
									<xsl:call-template name="OutputPhonemeFeatureMatrix"/>
								</section2>
								<section2 id="sPhonemesPhonemeTable" type="tH2">
									<secTitle>Phoneme Table</secTitle>
									<p>
										<xsl:text>This analysis of </xsl:text>
										<xsl:value-of select="$sLangName"/>
										<xsl:text> posits the following phonemes (the first column shows the orthographic representations):</xsl:text>
									</p>
									<xsl:call-template name="OutputPhonemeTable"/>
								</section2>
							</xsl:when>
							<xsl:otherwise>
								<p>
									<xsl:call-template name="OutputPhonemeCount">
										<xsl:with-param name="sLangName" select="$sLangName"/>
										<xsl:with-param name="iPhonemeCount" select="$iPhonemeCount"/>
									</xsl:call-template>
									<xsl:text> as shown in the following table (the first column shows the orthographic representations):</xsl:text>
								</p>
								<xsl:call-template name="OutputPhonemeTable"/>
							</xsl:otherwise>
						</xsl:choose>
					</xsl:otherwise>
				</xsl:choose>
			</section1>
			<section1 id="sTypes" type="tH1">
				<secTitle>Morpheme types</secTitle>
				<p>
					<xsl:text>Words in this analysis of </xsl:text>
					<xsl:value-of select="$sLangName"/>
					<xsl:text> are formed from morphemes of </xsl:text>
					<!-- The following is a trick (I could not figure out any other way to do it)
				It sorts the allomorphs by morph type and, when a new type is found, it outputs a single character into the variable (it happens to use "x", but any character would do).
				There is therefore one character in the variable for each unique morph type.
				We can then output the string length of the variable to give the count of morph types actually used.
				-->
					<xsl:variable name="sMorphTypeCounter">
						<xsl:for-each select="$MoStemAllomorphs | $MoAffixAllomorphs">
							<xsl:sort select="@MorphType"/>
							<xsl:if test="@MorphType !='0' and not(@MorphType=preceding-sibling::*/@MorphType)">
								<xsl:text>x</xsl:text>
							</xsl:if>
						</xsl:for-each>
					</xsl:variable>
					<xsl:value-of select="string-length($sMorphTypeCounter)"/>
					<!-- end of trick -->
					<xsl:text> types. The following table lists the types along with a count of how many instances are in the lexicon.  Appendix </xsl:text>
					<xsl:element name="appendixRef">
						<xsl:attribute name="app">
							<xsl:text>aMorphsByType</xsl:text>
						</xsl:attribute>
					</xsl:element>
					<xsl:text> lists some or all of these.</xsl:text>
				</p>
				<table type="tLeftOffset" border="1">
					<tr>
						<th align="right">Count</th>
						<th>Name</th>
						<th>Description</th>
						<th>Appendix</th>
					</tr>
					<xsl:for-each select="$MoMorphTypes">
						<xsl:sort select="Name"/>
						<xsl:variable name="idType">
							<xsl:value-of select="@Id"/>
						</xsl:variable>
						<xsl:variable name="sName">
							<xsl:value-of select="Name"/>
						</xsl:variable>
						<xsl:variable name="iCount">
							<xsl:value-of select="NumberOfLexEntries"/>
						</xsl:variable>
						<xsl:if test="$iCount != 0">
							<tr>
								<td align="right" width="5%">
									<xsl:value-of select="$iCount"/>
									<xsl:text disable-output-escaping="yes">&amp;nbsp;</xsl:text>
								</td>
								<td width="10%">
									<xsl:value-of select="$sName"/>
									<xsl:text disable-output-escaping="yes">&amp;nbsp;</xsl:text>
									<xsl:text disable-output-escaping="yes">&amp;nbsp;&amp;nbsp;</xsl:text>
								</td>
								<td width="75%">
									<xsl:value-of select="Description"/>
								</td>
								<td width="10%">
									<appendixRef>
										<xsl:attribute name="app">
											<xsl:text>aMorphsByType.</xsl:text>
											<xsl:value-of select="$idType"/>
										</xsl:attribute>
									</appendixRef>
								</td>
							</tr>
						</xsl:if>
					</xsl:for-each>
				</table>
			</section1>
			<section1 id="sCategories" type="tH1">
				<secTitle>Word categories</secTitle>
				<p>
					<xsl:text>In this analysis of  </xsl:text>
					<xsl:value-of select="$sLangName"/>
					<xsl:text> there are </xsl:text>
					<xsl:variable name="iSubcats" select="count($POSes/PartOfSpeech)"/>
					<xsl:value-of select="count($POSes) - $iSubcats"/>
					<xsl:if test="$iSubcats > 0">
						<xsl:text> major</xsl:text>
					</xsl:if>
					<xsl:text> syntactic categories for words.  </xsl:text>
					<xsl:if test="$iSubcats > 0">
						<xsl:text>Some of these in turn have subcategories. </xsl:text>
					</xsl:if>
					<xsl:text>The following is a complete list of the categories </xsl:text>
					<xsl:if test="$iSubcats > 0">
						<xsl:text>and subcategories </xsl:text>
					</xsl:if>
					<xsl:text>that are posited (along with a count of how many instances of each are found in the lexicon; some or all of these are in the appendix).</xsl:text>
				</p>
				<ul>
					<xsl:for-each select="$PartsOfSpeech/PartOfSpeech">
						<xsl:sort select="Name"/>
						<xsl:variable name="idPOS">
							<xsl:value-of select="@Id"/>
						</xsl:variable>
						<xsl:call-template name="ProcessPOS">
							<xsl:with-param name="pos" select="."/>
						</xsl:call-template>
					</xsl:for-each>
				</ul>
				<p>
					<xsl:text>The categories are defined as follows (the category's abbreviation is shown within square brackets): </xsl:text>
				</p>
				<xsl:for-each select="$PartsOfSpeech/PartOfSpeech">
					<xsl:sort select="Name"/>
					<xsl:variable name="idPOS">
						<xsl:value-of select="@Id"/>
					</xsl:variable>
					<xsl:call-template name="DefinePOS">
						<xsl:with-param name="pos" select="."/>
					</xsl:call-template>
				</xsl:for-each>
			</section1>
			<section1 id="sInflection" type="tH1">
				<secTitle>Inflection</secTitle>
				<xsl:if test="count($MoInflAffixTemplates[count(PrefixSlots)+count(SuffixSlots)=0]) > 0">
					<p>
						<xsl:text>WARNING: There are one or more templates that do not have any slots!  A template without any slots does not make any sense.  You should either remove the template or add slots to it.  The following lists the templates which do not have a slot:</xsl:text>
					</p>
					<table type="tLeftOffset" border="1">
						<tr>
							<th>Category</th>
							<th>Template Name</th>
						</tr>
						<xsl:for-each select="$MoInflAffixTemplates[count(PrefixSlots)=0 and count(SuffixSlots)=0]">
							<tr>
								<td>
									<xsl:value-of select="../../Name"/>
								</td>
								<td>
									<xsl:value-of select="Name"/>
								</td>
							</tr>
						</xsl:for-each>
					</table>
				</xsl:if>
				<xsl:choose>
					<xsl:when test="count($MoInflAffixTemplates) = 0">
						<p>
							<xsl:text>There is no inflection in this analysis of </xsl:text>
							<xsl:value-of select="$sLangName"/>
							<xsl:text>.</xsl:text>
						</p>
					</xsl:when>
					<xsl:when test="count($POSes[descendant-or-self::MoInflAffixTemplate]) = 1">
						<p>
							<xsl:text>The only word category that is inflected in this analysis of </xsl:text>
							<xsl:value-of select="$sLangName"/>
							<xsl:text> is </xsl:text>
							<xsl:value-of select="$POSes[descendant-or-self::MoInflAffixTemplate]/Name"/>
							<xsl:text>.</xsl:text>
						</p>
					</xsl:when>
					<xsl:otherwise>
						<p>
							<xsl:text>In this analysis of </xsl:text>
							<xsl:value-of select="$sLangName"/>
							<xsl:text> the following word categories are inflected: </xsl:text>
						</p>
						<dl>
							<xsl:for-each select="$toplevelPOSes">
								<!--                                <xsl:for-each select="$PartsOfSpeech/PartOfSpeech[descendant-or-self::MoInflAffixTemplate]">-->
								<xsl:sort select="Name"/>
								<xsl:variable name="idPOS">
									<xsl:value-of select="@Id"/>
								</xsl:variable>
								<xsl:variable name="bDescendantOrSelfHasTemplate">
									<xsl:call-template name="bPOSContainsInflTemplate">
										<xsl:with-param name="pos" select="."/>
									</xsl:call-template>
								</xsl:variable>
								<xsl:if test="contains($bDescendantOrSelfHasTemplate,'1')">
									<dd>
										<xsl:call-template name="Capitalize">
											<xsl:with-param name="sStr">
												<xsl:value-of select="Name"/>
											</xsl:with-param>
										</xsl:call-template>
										<xsl:text> (</xsl:text>
										<sectionRef>
											<xsl:attribute name="sec">
												<xsl:text>sInfl.</xsl:text>
												<xsl:value-of select="@Id"/>
											</xsl:attribute>
										</sectionRef>
										<xsl:text>)</xsl:text>
									</dd>
								</xsl:if>
							</xsl:for-each>
						</dl>
					</xsl:otherwise>
				</xsl:choose>
				<xsl:if test="$POSes/AffixSlots/MoInflAffixSlot/@Optional = 'true'">
					<p>In the inflectional templates expressed below, parentheses indicate that a slot is optional.</p>
				</xsl:if>
				<xsl:for-each select="$toplevelPOSes">
					<xsl:sort select="Name"/>
					<xsl:variable name="bDescendantOrSelfHasTemplate">
						<xsl:call-template name="bPOSContainsInflTemplate">
							<xsl:with-param name="pos" select="."/>
						</xsl:call-template>
					</xsl:variable>
					<xsl:if test="contains($bDescendantOrSelfHasTemplate,'1')">
						<section2 type="tH2">
							<xsl:attribute name="id">
								<xsl:text>sInfl.</xsl:text>
								<xsl:value-of select="@Id"/>
							</xsl:attribute>
							<xsl:call-template name="ProcessInflectionForPOS">
								<xsl:with-param name="pos" select="."/>
							</xsl:call-template>
						</section2>
					</xsl:if>
				</xsl:for-each>
			</section1>
			<section1 id="sDerivation" type="tH1">
				<secTitle>Derivation</secTitle>
				<xsl:variable name="iCountDerivAffixes">
					<xsl:value-of select="count($LexEntries[key('MsaID',MorphoSyntaxAnalysis/@dst)[@ToPartOfSpeech!='0']])"/>
				</xsl:variable>
				<xsl:choose>
					<xsl:when test="$iCountDerivAffixes = '0'">
						<p>
							<xsl:text>There are no derivational affixes in this analysis of </xsl:text>
							<xsl:value-of select="$sLangName"/>
							<xsl:text>.</xsl:text>
						</p>
					</xsl:when>
					<xsl:otherwise>
						<p>
							<xsl:text>The lexicon currently contains </xsl:text>
							<xsl:value-of select="$iCountDerivAffixes"/>
							<xsl:text> derivational affix</xsl:text>
							<xsl:if test="$iCountDerivAffixes &gt; '1'">es</xsl:if>
							<xsl:text>.  A number in the table below indicates the number of derivational affixes that attach to a stem of the syntactic category named in the row label to the left and produce a stem of the syntactic category named in the column label above it. Click on the number to see a list of the actual affixes.  (Note that it is possible for a derivational affix to have more than one mapping so the sum of the numbers in the table may be greater than the number of derivational affixes in the lexicon.)</xsl:text>
						</p>
						<table border="1" type="tDerivationChart">
							<tr>
								<td/>
								<xsl:for-each select="$POSes">
									<xsl:sort select="Abbreviation"/>
									<xsl:variable name="toCat">
										<xsl:value-of select="@Id"/>
									</xsl:variable>
									<xsl:if test="$MoDerivAffMsas[@ToPartOfSpeech=$toCat]">
										<td type="tDerivHeader">
											<xsl:call-template name="CreatePOSAbbrAsRef">
												<xsl:with-param name="idPOS" select="$toCat"/>
											</xsl:call-template>
										</td>
									</xsl:if>
								</xsl:for-each>
							</tr>
							<xsl:for-each select="$POSes">
								<xsl:sort select="Abbreviation"/>
								<xsl:variable name="fromCat">
									<xsl:value-of select="@Id"/>
								</xsl:variable>
								<xsl:if test="$MoDerivAffMsas[@FromPartOfSpeech=$fromCat]">
									<tr>
										<td type="tDerivHeader">
											<xsl:call-template name="CreatePOSAbbrAsRef">
												<xsl:with-param name="idPOS" select="$fromCat"/>
											</xsl:call-template>
										</td>
										<xsl:for-each select="$POSes">
											<xsl:sort select="Abbreviation"/>
											<xsl:variable name="toCat">
												<xsl:value-of select="@Id"/>
											</xsl:variable>
											<xsl:if test="$MoDerivAffMsas[@ToPartOfSpeech=$toCat]">
												<xsl:variable name="derivCount">
													<xsl:value-of select="count($MorphoSyntaxAnalyses/MoDerivAffMsa[@FromPartOfSpeech=$fromCat and @ToPartOfSpeech=$toCat])"/>
												</xsl:variable>
												<td align="center">
													<xsl:choose>
														<xsl:when test="$derivCount > 0">
															<genericRef>
																<xsl:attribute name="gref">
																	<xsl:call-template name="CreateFromCatToCatRef">
																		<xsl:with-param name="fromCat" select="$fromCat"/>
																		<xsl:with-param name="toCat" select="$toCat"/>
																	</xsl:call-template>
																</xsl:attribute>
																<xsl:value-of select="$derivCount"/>
															</genericRef>
														</xsl:when>
														<xsl:otherwise>
															<xsl:text disable-output-escaping="yes">&amp;nbsp;</xsl:text>
														</xsl:otherwise>
													</xsl:choose>
												</td>
											</xsl:if>
										</xsl:for-each>
									</tr>
								</xsl:if>
							</xsl:for-each>
						</table>
						<p>
							<xsl:text>The following are the derivational affixes in this analysis of </xsl:text>
							<xsl:value-of select="$sLangName"/>
							<xsl:text>:</xsl:text>
						</p>
						<xsl:for-each select="$POSes">
							<xsl:sort select="Abbreviation"/>
							<xsl:variable name="fromCat">
								<xsl:value-of select="@Id"/>
							</xsl:variable>
							<xsl:variable name="fromAbbr">
								<xsl:value-of select="Abbreviation"/>
							</xsl:variable>
							<xsl:for-each select="$POSes">
								<xsl:sort select="Abbreviation"/>
								<xsl:variable name="toCat">
									<xsl:value-of select="@Id"/>
								</xsl:variable>
								<xsl:variable name="toAbbr">
									<xsl:value-of select="Abbreviation"/>
								</xsl:variable>
								<xsl:if test="$MoDerivAffMsas[@FromPartOfSpeech=$fromCat and @ToPartOfSpeech=$toCat]">
									<section2 type="tH2">
										<xsl:attribute name="id">
											<xsl:call-template name="CreateFromCatToCatRef">
												<xsl:with-param name="fromCat" select="$fromCat"/>
												<xsl:with-param name="toCat" select="$toCat"/>
											</xsl:call-template>
										</xsl:attribute>
										<secTitle>
											<xsl:text>From </xsl:text>
											<genericRef>
												<xsl:attribute name="gref">
													<xsl:text>sCat.</xsl:text>
													<xsl:value-of select="$fromCat"/>
												</xsl:attribute>
												<xsl:value-of select="$fromAbbr"/>
											</genericRef>
											<xsl:text> to </xsl:text>
											<genericRef>
												<xsl:attribute name="gref">
													<xsl:text>sCat.</xsl:text>
													<xsl:value-of select="$toCat"/>
												</xsl:attribute>
												<xsl:value-of select="$toAbbr"/>
											</genericRef>
										</secTitle>
										<table type="tLeftOffset">
											<xsl:variable name="derivs" select="$MorphoSyntaxAnalyses/MoDerivAffMsa[@FromPartOfSpeech=$fromCat and @ToPartOfSpeech=$toCat]"/>
											<xsl:variable name="fromInflClasses" select="$derivs[@FromInflectionClass!='0']"/>
											<xsl:variable name="toInflClasses" select="$derivs[@ToInflectionClass!='0']"/>
											<xsl:variable name="fromInflFeats" select="$derivs/FromMsFeatures/FsFeatStruc[descendant-or-self::FsClosedValue]"/>
											<xsl:variable name="toInflFeats" select="$derivs/ToMsFeatures/FsFeatStruc[descendant-or-self::FsClosedValue]"/>
											<xsl:variable name="fromExceptionFeatures" select="$derivs[FromProdRestrict]"/>
											<xsl:variable name="toExceptionFeatures" select="$derivs[ToProdRestrict]"/>
											<xsl:variable name="stemNames" select="$derivs[@FromStemName!='0']"/>
											<xsl:if test="$fromInflClasses or $toInflClasses or $fromExceptionFeatures or $toExceptionFeatures or $stemNames">
												<xsl:attribute name="border">1</xsl:attribute>
												<tr>
													<xsl:choose>
														<xsl:when test="$bVernRightToLeft='1'">
															<xsl:if test="$toExceptionFeatures">
																<th>
																	<xsl:text>To </xsl:text>
																	<xsl:call-template name="OutputProductivityRestrictionLabel">
																		<xsl:with-param name="sFinal" select="'s'"/>
																	</xsl:call-template>
																</th>
															</xsl:if>
															<xsl:if test="$toInflFeats">
																<th>To inflection features</th>
															</xsl:if>
															<xsl:if test="$toInflClasses">
																<th>To inflection class</th>
															</xsl:if>
															<xsl:if test="$fromExceptionFeatures">
																<th>
																	<xsl:text>From </xsl:text>
																	<xsl:call-template name="OutputProductivityRestrictionLabel">
																		<xsl:with-param name="sFinal" select="'s'"/>
																	</xsl:call-template>
																</th>
															</xsl:if>
															<xsl:if test="$stemNames">
																<th>From Stem Name</th>
															</xsl:if>
															<xsl:if test="$fromInflFeats">
																<th>From inflection features</th>
															</xsl:if>
															<xsl:if test="$fromInflClasses">
																<th>From inflection class</th>
															</xsl:if>
															<th>Definition</th>
															<th>Gloss</th>
															<th>Citation form</th>
														</xsl:when>
														<xsl:otherwise>
															<th>Citation form</th>
															<th>Gloss</th>
															<th>Definition</th>
															<xsl:if test="$fromInflClasses">
																<th>From inflection class</th>
															</xsl:if>
															<xsl:if test="$fromInflFeats">
																<th>From inflection features</th>
															</xsl:if>
															<xsl:if test="$stemNames">
																<th>From Stem Name</th>
															</xsl:if>
															<xsl:if test="$fromExceptionFeatures">
																<th>
																	<xsl:text>From </xsl:text>
																	<xsl:call-template name="OutputProductivityRestrictionLabel">
																		<xsl:with-param name="sFinal" select="'s'"/>
																	</xsl:call-template>
																</th>
															</xsl:if>
															<xsl:if test="$toInflClasses">
																<th>To inflection class</th>
															</xsl:if>
															<xsl:if test="$toInflFeats">
																<th>To inflection features</th>
															</xsl:if>
															<xsl:if test="$toExceptionFeatures">
																<th>
																	<xsl:text>To </xsl:text>
																	<xsl:call-template name="OutputProductivityRestrictionLabel">
																		<xsl:with-param name="sFinal" select="'s'"/>
																	</xsl:call-template>
																</th>
															</xsl:if>
														</xsl:otherwise>
													</xsl:choose>
												</tr>
											</xsl:if>
											<xsl:for-each select="$derivs">
												<xsl:variable name="id" select="@Id"/>
												<xsl:variable name="LexEntry" select="$LexEntries[MorphoSyntaxAnalysis/@dst=$id]"/>
												<tr>
													<xsl:choose>
														<xsl:when test="$bVernRightToLeft='1'">
															<xsl:if test="$toExceptionFeatures">
																<xsl:call-template name="OutputExceptionFeatures">
																	<xsl:with-param name="features" select="ToProdRestrict"/>
																</xsl:call-template>
															</xsl:if>
															<xsl:if test="$toInflFeats">
																<xsl:call-template name="OutputInflectionFeatures">
																	<xsl:with-param name="fs" select="ToMsFeatures/FsFeatStruc"/>
																</xsl:call-template>
															</xsl:if>
															<xsl:if test="$toInflClasses">
																<xsl:call-template name="OutputInflectionClass">
																	<xsl:with-param name="class" select="@ToInflectionClass"/>
																</xsl:call-template>
															</xsl:if>
															<xsl:if test="$fromExceptionFeatures">
																<xsl:call-template name="OutputExceptionFeatures">
																	<xsl:with-param name="features" select="FromProdRestrict"/>
																</xsl:call-template>
															</xsl:if>
															<xsl:if test="$stemNames">
																<xsl:call-template name="OutputStemName">
																	<xsl:with-param name="sn" select="@FromStemName"/>
																</xsl:call-template>
															</xsl:if>
															<xsl:if test="$fromInflFeats">
																<xsl:call-template name="OutputInflectionFeatures">
																	<xsl:with-param name="fs" select="FromMsFeatures/FsFeatStruc"/>
																</xsl:call-template>
															</xsl:if>
															<xsl:if test="$fromInflClasses">
																<xsl:call-template name="OutputInflectionClass">
																	<xsl:with-param name="class" select="@FromInflectionClass"/>
																</xsl:call-template>
															</xsl:if>
															<xsl:call-template name="GetDefinition">
																<xsl:with-param name="Sense" select="key('SensesMsa',@Id)"/>
															</xsl:call-template>
															<xsl:call-template name="GetGloss">
																<xsl:with-param name="Sense" select="key('SensesMsa',@Id)"/>
															</xsl:call-template>
															<xsl:call-template name="GetCitationForm">
																<xsl:with-param name="LexEntry" select="$LexEntry"/>
															</xsl:call-template>
														</xsl:when>
														<xsl:otherwise>
															<xsl:call-template name="GetCitationForm">
																<xsl:with-param name="LexEntry" select="$LexEntry"/>
															</xsl:call-template>
															<xsl:call-template name="GetGloss">
																<xsl:with-param name="Sense" select="key('SensesMsa',@Id)"/>
															</xsl:call-template>
															<xsl:call-template name="GetDefinition">
																<xsl:with-param name="Sense" select="key('SensesMsa',@Id)"/>
															</xsl:call-template>
															<xsl:if test="$fromInflClasses">
																<xsl:call-template name="OutputInflectionClass">
																	<xsl:with-param name="class" select="@FromInflectionClass"/>
																</xsl:call-template>
															</xsl:if>
															<xsl:if test="$fromInflFeats">
																<xsl:call-template name="OutputInflectionFeatures">
																	<xsl:with-param name="fs" select="FromMsFeatures/FsFeatStruc"/>
																</xsl:call-template>
															</xsl:if>
															<xsl:if test="$stemNames">
																<xsl:call-template name="OutputStemName">
																	<xsl:with-param name="sn" select="@FromStemName"/>
																</xsl:call-template>
															</xsl:if>
															<xsl:if test="$fromExceptionFeatures">
																<xsl:call-template name="OutputExceptionFeatures">
																	<xsl:with-param name="features" select="FromProdRestrict"/>
																</xsl:call-template>
															</xsl:if>
															<xsl:if test="$toInflClasses">
																<xsl:call-template name="OutputInflectionClass">
																	<xsl:with-param name="class" select="@ToInflectionClass"/>
																</xsl:call-template>
															</xsl:if>
															<xsl:if test="$toInflFeats">
																<xsl:call-template name="OutputInflectionFeatures">
																	<xsl:with-param name="fs" select="ToMsFeatures/FsFeatStruc"/>
																</xsl:call-template>
															</xsl:if>
															<xsl:if test="$toExceptionFeatures">
																<xsl:call-template name="OutputExceptionFeatures">
																	<xsl:with-param name="features" select="ToProdRestrict"/>
																</xsl:call-template>
															</xsl:if>
														</xsl:otherwise>
													</xsl:choose>
												</tr>
											</xsl:for-each>
										</table>
									</section2>
								</xsl:if>
							</xsl:for-each>
						</xsl:for-each>
					</xsl:otherwise>
				</xsl:choose>
			</section1>
			<section1 id="sClitics" type="tH1">
				<secTitle>Clitics</secTitle>
				<xsl:variable name="sCliticMorphTypeId" select="$MoMorphTypes[@Guid=$sClitic]/@Id"/>
				<xsl:variable name="sEncliticMorphTypeId" select="$MoMorphTypes[@Guid=$sEnclitic]/@Id"/>
				<xsl:variable name="sProcliticMorphTypeId" select="$MoMorphTypes[@Guid=$sProclitic]/@Id"/>
				<xsl:variable name="Clitics" select="$LexEntries[key('MoFormsID',LexemeForm/@dst)[@MorphType=$sCliticMorphTypeId and @IsAbstract='0' or @MorphType=$sEncliticMorphTypeId and @IsAbstract='0' or @MorphType=$sProcliticMorphTypeId and @IsAbstract='0'] or key('MoFormsID',AlternateForms/@dst)[@MorphType=$sCliticMorphTypeId and @IsAbstract='0' or @MorphType=$sEncliticMorphTypeId and @IsAbstract='0' or @MorphType=$sProcliticMorphTypeId and @IsAbstract='0']]"/>
				<xsl:variable name="iCount" select="count($Clitics)"/>
				<p>
					<xsl:text>In this analysis of  </xsl:text>
					<xsl:value-of select="$sLangName"/>
					<xsl:text> there </xsl:text>
					<xsl:choose>
						<xsl:when test="$iCount=0">
							<xsl:text>are no clitics.</xsl:text>
						</xsl:when>
						<xsl:when test="$iCount=1">
							<xsl:text>is one clitic.</xsl:text>
						</xsl:when>
						<xsl:otherwise>
							<xsl:text>are </xsl:text>
							<xsl:value-of select="$iCount"/>
							<xsl:text> clitics.</xsl:text>
						</xsl:otherwise>
					</xsl:choose>
				</p>
				<xsl:if test="$iCount &gt; 0">
					<table type="tLeftOffset" border="1">
						<tr>
							<xsl:choose>
								<xsl:when test="$bVernRightToLeft='1'">
									<th>Attaches to:</th>
									<th>Category</th>
									<th>Definition</th>
									<th>Gloss</th>
									<th>Form</th>
								</xsl:when>
								<xsl:otherwise>
									<th>Form</th>
									<th>Gloss</th>
									<th>Definition</th>
									<th>Category</th>
									<th>Attaches to:</th>
								</xsl:otherwise>
							</xsl:choose>
						</tr>
						<xsl:for-each select="$Clitics">
							<xsl:sort select="CitationForm"/>
							<xsl:sort select="LexemeForm"/>
							<xsl:variable name="lexEntry" select="."/>
							<xsl:variable name="entryMsas" select="key('MsaID',MorphoSyntaxAnalysis/@dst)"/>
							<xsl:call-template name="OutputCliticWithPOS">
								<xsl:with-param name="lexEntry" select="$lexEntry"/>
								<xsl:with-param name="entryMsas" select="$entryMsas"/>
							</xsl:call-template>
						</xsl:for-each>
					</table>
				</xsl:if>
			</section1>
			<section1 id="sCompounding" type="tH1">
				<secTitle>Compounding</secTitle>
				<xsl:variable name="CompoundRules" select="/M3Dump/MoMorphData/CompoundRules/MoEndoCompound | /M3Dump/MoMorphData/CompoundRules/MoExoCompound"/>
				<p>
					<xsl:text>In this analysis of  </xsl:text>
					<xsl:value-of select="$sLangName"/>
					<xsl:text> there </xsl:text>
					<xsl:choose>
						<xsl:when test="count($CompoundRules)=0">
							<xsl:text>is no compounding.</xsl:text>
						</xsl:when>
						<xsl:when test="count($CompoundRules)=1">
							<xsl:text>is one form of compounding.</xsl:text>
						</xsl:when>
						<xsl:otherwise>
							<xsl:text>are </xsl:text>
							<xsl:value-of select="count($CompoundRules)"/>
							<xsl:text> forms of compounding.</xsl:text>
						</xsl:otherwise>
					</xsl:choose>
				</p>
				<xsl:for-each select="$CompoundRules">
					<xsl:sort select="Name"/>
					<section2 type="tH2">
						<xsl:attribute name="id">
							<xsl:text>compound.</xsl:text>
							<xsl:value-of select="@Id"/>
						</xsl:attribute>
						<secTitle>
							<xsl:call-template name="Capitalize">
								<xsl:with-param name="sStr">
									<xsl:value-of select="Name"/>
								</xsl:with-param>
							</xsl:call-template>
						</secTitle>
						<p>
							<xsl:choose>
								<xsl:when test="string-length(Description) &gt; 0 and Description!=$sEmptyContent">
									<xsl:call-template name="Capitalize">
										<xsl:with-param name="sStr">
											<xsl:value-of select="Description"/>
										</xsl:with-param>
									</xsl:call-template>
								</xsl:when>
								<xsl:otherwise>
									<xsl:text>This compound rule does not have a description.  Please add one explaining the nature of this compound rule.</xsl:text>
								</xsl:otherwise>
							</xsl:choose>
							<xsl:text disable-output-escaping="yes">&amp;nbsp;&amp;nbsp;</xsl:text>
							<xsl:text>A compound is formed by combining a stem of the category on the left with a stem of the category on the right:</xsl:text>
						</p>
						<xsl:variable name="idLeftMsaPOS" select="key('MsaID',LeftMsa/@dst)/@PartOfSpeech"/>
						<xsl:variable name="idRightMsaPOS" select="key('MsaID',RightMsa/@dst)/@PartOfSpeech"/>
						<xsl:choose>
							<xsl:when test="$idLeftMsaPOS!=0 and $idRightMsaPOS!=0">
								<chart type="tLeftOffset">
									<genericRef>
										<xsl:attribute name="gref">
											<xsl:text>sCat.</xsl:text>
											<xsl:value-of select="$idLeftMsaPOS"/>
										</xsl:attribute>
										<xsl:value-of select="key('POSID',$idLeftMsaPOS)/Abbreviation"/>
									</genericRef>
									<xsl:text disable-output-escaping="yes">&amp;nbsp;&amp;nbsp;</xsl:text>
									<genericRef>
										<xsl:attribute name="gref">
											<xsl:text>sCat.</xsl:text>
											<xsl:value-of select="$idRightMsaPOS"/>
										</xsl:attribute>
										<xsl:value-of select="key('POSID',$idRightMsaPOS)/Abbreviation"/>
									</genericRef>
								</chart>
							</xsl:when>
							<xsl:otherwise>
								<xsl:if test="$idLeftMsaPOS=0">
									<p>The left category has not been specified!  This means that this rule will never be successfully applied by the Parser.  You should specify a category.</p>
								</xsl:if>
								<xsl:if test="$idRightMsaPOS=0">
									<p>The right category has not been specified!  This means that this rule will never be successfully applied by the Parser.  You should specify a category.</p>
								</xsl:if>
							</xsl:otherwise>
						</xsl:choose>
						<xsl:if test="Linker/@dst!=0">
							<p>
								<xsl:text>This rule also has a linker 'form' which appears between the parts of the compound.  This form is: </xsl:text>
								<langData lang="lVern">
									<xsl:value-of select="key('MoFormsID',Linker/@dst)/Form"/>
								</langData>
								<xsl:text>.</xsl:text>
							</p>
						</xsl:if>
						<xsl:choose>
							<xsl:when test="@HeadLast">
								<p>
									<xsl:choose>
										<xsl:when test="@HeadLast = '0'">
											<xsl:call-template name="OutputHeadedCompoundRuleInfo">
												<xsl:with-param name="sHead">left</xsl:with-param>
												<xsl:with-param name="pos" select="$idLeftMsaPOS"/>
												<xsl:with-param name="features" select="key('MsaID',LeftMsa/@dst)/ExceptionFeatures/FsFeatStruc"/>
											</xsl:call-template>
										</xsl:when>
										<xsl:otherwise>
											<xsl:call-template name="OutputHeadedCompoundRuleInfo">
												<xsl:with-param name="sHead">right</xsl:with-param>
												<xsl:with-param name="pos" select="$idRightMsaPOS"/>
												<xsl:with-param name="features" select="key('MsaID',RightMsa/@dst)/ExceptionFeatures/FsFeatStruc"/>
											</xsl:call-template>
										</xsl:otherwise>
									</xsl:choose>
								</p>
							</xsl:when>
							<xsl:when test="ToMsa">
								<xsl:variable name="idToMsaPOS" select="key('MsaID',ToMsa/@dst)/@PartOfSpeech"/>
								<p>
									<xsl:choose>
										<xsl:when test="$idToMsaPOS!=0">
											<xsl:text>This is a non-headed construction; the resulting compound has the category of </xsl:text>
											<genericRef>
												<xsl:attribute name="gref">
													<xsl:text>sCat.</xsl:text>
													<xsl:value-of select="$idToMsaPOS"/>
												</xsl:attribute>
												<xsl:value-of select="key('POSID',$idToMsaPOS)/Abbreviation"/>
											</genericRef>
											<xsl:variable name="idToMsaInflClass" select="key('MsaID',ToMsa/@dst)/@InflectionClass"/>
											<xsl:if test="$idToMsaInflClass!=0">
												<xsl:text> and has an inflection class of </xsl:text>
												<xsl:value-of select="$MoInflClasses[@Id=$idToMsaInflClass]/Name"/>
											</xsl:if>
											<xsl:text>.</xsl:text>
										</xsl:when>
										<xsl:otherwise>
											<xsl:text>This is a non-headed construction, but the category of the resulting compound has not been specifed!  You should specify a category.</xsl:text>
										</xsl:otherwise>
									</xsl:choose>
									<xsl:call-template name="OutputExceptionFeaturesOfCompoundRule">
										<xsl:with-param name="features" select="key('MsaID', ToMsa/@dst)/ExceptionFeatures/FsFeatStruc"/>
									</xsl:call-template>
								</p>
							</xsl:when>
						</xsl:choose>
					</section2>
				</xsl:for-each>
			</section1>
			<xsl:if test="$FeatureSystem">
				<xsl:call-template name="OutputFeatureSystem">
					<xsl:with-param name="FeatureSystem" select="$FeatureSystem"/>
					<xsl:with-param name="sLangName" select="$sLangName"/>
					<xsl:with-param name="sKindId" select="'MS'"/>
					<xsl:with-param name="sKind" select="'morpho-syntactic'"/>
				</xsl:call-template>
			</xsl:if>
			<xsl:if test="$ProdRestrict">
				<section1 id="sProdRestrict" type="tH1">
					<secTitle>
						Exception <xsl:text disable-output-escaping="yes">&amp;ldquo;Features&amp;rdquo;</xsl:text>
					</secTitle>
					<p>
						<xsl:text>This analysis of </xsl:text>
						<xsl:value-of select="$sLangName"/>
						<xsl:text> has the following </xsl:text>
						<xsl:call-template name="OutputProductivityRestrictionLabel">
							<xsl:with-param name="sFinal" select="'s.'"/>
						</xsl:call-template>
						<xsl:text>   These are used to restrict the productivity of certain affixes so that they only co-occur with stems that are tagged with the same set of </xsl:text>
						<xsl:call-template name="OutputProductivityRestrictionLabel">
							<xsl:with-param name="sFinal" select="'s.'"/>
						</xsl:call-template>
					</p>
					<xsl:for-each select="$ProdRestrict">
						<xsl:sort select="Name"/>
						<xsl:variable name="prodRest" select="@Id"/>
						<section2 type="tH2">
							<xsl:attribute name="id">
								<xsl:text>sProductivityRestriction</xsl:text>
								<xsl:value-of select="@Id"/>
							</xsl:attribute>
							<secTitle>
								<xsl:value-of select="Name"/>
							</secTitle>
							<p>
								<xsl:choose>
									<xsl:when test="string-length(Description) &gt; 0 and Description!=$sEmptyContent">
										<xsl:value-of select="Description"/>
									</xsl:when>
									<xsl:otherwise>
										<xsl:text>There is no description for this </xsl:text>
										<xsl:call-template name="OutputProductivityRestrictionLabel"/>
										<xsl:text> yet.</xsl:text>
									</xsl:otherwise>
								</xsl:choose>
							</p>
							<xsl:variable name="inflAffixes" select="$MoInflAffMsas[FromProdRestrict/@dst=$prodRest]"/>
							<xsl:if test="$inflAffixes">
								<p>
									<xsl:text>The following inflectional affixes are marked with this </xsl:text>
									<xsl:call-template name="OutputProductivityRestrictionLabel">
										<xsl:with-param name="sFinal" select="':'"/>
									</xsl:call-template>
								</p>
								<table type="tLeftOffset">
									<xsl:for-each select="$inflAffixes">
										<xsl:sort select="key('LexEntryMsa', @Id)/../CitationForm"/>
										<xsl:variable name="InflAffixMsa" select="@Id"/>
										<xsl:variable name="LexEntry" select="$LexEntries[MorphoSyntaxAnalysis/@dst=$InflAffixMsa]"/>
										<tr>
											<xsl:call-template name="OutputInflAffixLexEntry">
												<xsl:with-param name="LexEntry" select="$LexEntry"/>
												<xsl:with-param name="msa" select="../@Id"/>
											</xsl:call-template>
										</tr>
									</xsl:for-each>
								</table>
							</xsl:if>
							<xsl:variable name="derivAffixes" select="$MoDerivAffMsas[FromProdRestrict/@dst=$prodRest] | $MoDerivAffMsas[ToProdRestrict/@dst=$prodRest]"/>
							<xsl:if test="$derivAffixes">
								<p>
									The following derivational affixes are marked with this <xsl:call-template name="OutputProductivityRestrictionLabel">
										<xsl:with-param name="sFinal" select="':'"/>
									</xsl:call-template>
								</p>
								<table type="tLeftOffset" border="1">
									<tr>
										<th>Form</th>
										<th>Gloss</th>
										<th>
											Used as From <xsl:call-template name="OutputProductivityRestrictionLabel"/>
										</th>
										<th>
											Used as To <xsl:call-template name="OutputProductivityRestrictionLabel"/>
										</th>
									</tr>
									<xsl:for-each select="$derivAffixes">
										<xsl:sort select="key('LexEntryMsa', @Id)/../CitationForm"/>
										<xsl:variable name="DerivAffixMsa" select="@Id"/>
										<xsl:variable name="LexEntry" select="$LexEntries[MorphoSyntaxAnalysis/@dst=$DerivAffixMsa]"/>
										<tr>
											<!-- following also works here -->
											<xsl:call-template name="OutputInflAffixLexEntry">
												<xsl:with-param name="LexEntry" select="$LexEntry"/>
												<xsl:with-param name="msa" select="../@Id"/>
											</xsl:call-template>
											<td align="center">
												<xsl:choose>
													<xsl:when test="FromProdRestrict/@dst=$prodRest">
														<xsl:text>Yes</xsl:text>
													</xsl:when>
													<xsl:otherwise>
														<xsl:text disable-output-escaping="yes">No</xsl:text>
													</xsl:otherwise>
												</xsl:choose>
											</td>
											<td align="center">
												<xsl:choose>
													<xsl:when test="ToProdRestrict/@dst=$prodRest">
														<xsl:text>Yes</xsl:text>
													</xsl:when>
													<xsl:otherwise>
														<xsl:text disable-output-escaping="yes">No</xsl:text>
													</xsl:otherwise>
												</xsl:choose>
											</td>
										</tr>
									</xsl:for-each>
								</table>
							</xsl:if>
							<xsl:if test="not($inflAffixes) and not($derivAffixes)">
								<p>
									No affixes are marked with this <xsl:call-template name="OutputProductivityRestrictionLabel">
										<xsl:with-param name="sFinal" select="'.'"/>
									</xsl:call-template>
								</p>
							</xsl:if>
							<xsl:variable name="stems" select="$MorphoSyntaxAnalyses/MoStemMsa[ProdRestrict/@dst=$prodRest]"/>
							<xsl:choose>
								<xsl:when test="$stems">
									<xsl:variable name="iCount" select="count($stems)"/>
									<p>
										The following stems are marked with this <xsl:call-template name="OutputProductivityRestrictionLabel">
											<xsl:with-param name="sFinal" select="'.'"/>
										</xsl:call-template>
										<xsl:text>&#x20;</xsl:text>
										<xsl:call-template name="AppendixSubsectionBlurb">
											<xsl:with-param name="iCount" select="$iCount"/>
											<xsl:with-param name="sSubsection"/>
										</xsl:call-template>
									</p>
									<table type="tLeftOffset">
										<xsl:for-each select="$stems">
											<xsl:sort select="key('LexEntryMsa', @Id)/../CitationForm"/>
											<xsl:if test="$prmIMaxMorphsInAppendices = '-1' or position() &lt;= $prmIMaxMorphsInAppendices">
												<xsl:variable name="StemMsa" select="@Id"/>
												<xsl:variable name="LexEntry" select="$LexEntries[MorphoSyntaxAnalysis/@dst=$StemMsa]"/>
												<tr>
													<!-- following also works here -->
													<xsl:call-template name="OutputInflAffixLexEntry">
														<xsl:with-param name="LexEntry" select="$LexEntry"/>
														<xsl:with-param name="msa" select="../@Id"/>
													</xsl:call-template>
												</tr>
											</xsl:if>
										</xsl:for-each>
									</table>
								</xsl:when>
								<xsl:otherwise>
									<p>
										No stems are marked with this <xsl:call-template name="OutputProductivityRestrictionLabel">
											<xsl:with-param name="sFinal" select="'.'"/>
										</xsl:call-template>
									</p>
								</xsl:otherwise>
							</xsl:choose>
						</section2>
					</xsl:for-each>
				</section1>
			</xsl:if>
			<section1 id="sAllomorphy" type="tH1">
				<secTitle>Allomorphy</secTitle>
				<xsl:variable name="affixAlloFeats" select="$MoAffixAllomorphs[MsEnvFeatures[FsFeatStruc/FsComplexValue or FsFeatStruc/FsClosedValue]]"/>
				<xsl:choose>
					<xsl:when test="not($PhEnvironments) and not($MoInflClasses) and not($MoStemNames) and not($affixAlloFeats)">
						<p>
							<xsl:text>This analysis of </xsl:text>
							<xsl:value-of select="$sLangName"/>
							<xsl:text> has no phonological conditioning of allomorphs.</xsl:text>
						</p>
					</xsl:when>
					<xsl:otherwise>
						<p>
							<xsl:text>This analysis of </xsl:text>
							<xsl:value-of select="$sLangName"/>
							<xsl:text> has phonological conditioning of allomorphs.</xsl:text>
						</p>
						<section2 id="sAllomorphyEnvs" type="tH2">
							<secTitle>Phonological Environments</secTitle>
							<xsl:variable name="iCount" select="count($PhEnvironments)"/>
							<xsl:choose>
								<xsl:when test="$iCount=0">
									<p>
										<xsl:text>No phonological environments have been defined in this analysis of </xsl:text>
										<xsl:value-of select="$sLangName"/>.
		</p>
								</xsl:when>
								<xsl:otherwise>
									<p>
										<xsl:text>The following is a complete list of the phonological environments that condition allomorphs in this analysis: </xsl:text>
									</p>
									<table type="tLeftOffset" border="1">
										<tr>
											<th>Representation</th>
											<th>Name</th>
											<th>Description</th>
											<th>Count</th>
										</tr>
										<xsl:for-each select="$PhEnvironments">
											<xsl:sort select="@StringRepresentation"/>
											<xsl:sort select="Name"/>
											<xsl:variable name="env" select="@Id"/>
											<xsl:variable name="count" select="count($PhoneEnvs[@dst=$env])"/>
											<xsl:if test="$count != 0">
												<tr>
													<td>
														<xsl:if test="$bVernRightToLeft='1'">
															<xsl:attribute name="type">tRtl</xsl:attribute>
														</xsl:if>
														<genericTarget id="Environment.{@Id}"/>
														<xsl:call-template name="OutputStringRepresentation"/>
													</td>
													<td>
														<xsl:call-template name="OutputPotentiallyBlankItemInTable">
															<xsl:with-param name="item" select="Name"/>
														</xsl:call-template>
													</td>
													<td>
														<xsl:call-template name="OutputPotentiallyBlankItemInTable">
															<xsl:with-param name="item" select="Description"/>
														</xsl:call-template>
													</td>
													<td>
														<xsl:value-of select="$count"/>
														<xsl:if test="$count=1">
															<xsl:text> instance</xsl:text>
														</xsl:if>
														<xsl:if test="$count != 1">
															<xsl:text> instances</xsl:text>
														</xsl:if>
													</td>
												</tr>
											</xsl:if>
										</xsl:for-each>
									</table>
									<xsl:if test="$Positions">
										<p>
											<xsl:text>The following is a complete list of the phonological environments that condition infix positioning in this analysis: </xsl:text>
										</p>
										<table type="tLeftOffset" border="1">
											<tr>
												<th>Representation</th>
												<th>Name</th>
												<th>Description</th>
												<th>Count</th>
											</tr>
											<xsl:for-each select="$PhEnvironments">
												<xsl:sort select="@StringRepresentation"/>
												<xsl:sort select="Name"/>
												<xsl:variable name="env" select="@Id"/>
												<xsl:variable name="count" select="count($Positions[@dst=$env])"/>
												<xsl:if test="$count!=0">
													<tr>
														<td>
															<xsl:if test="$bVernRightToLeft='1'">
																<xsl:attribute name="type">tRtl</xsl:attribute>
															</xsl:if>
															<genericTarget id="Environment.{@Id}{$sEnvironmentPositionTag}"/>
															<xsl:call-template name="OutputStringRepresentation"/>
														</td>
														<td>
															<xsl:call-template name="OutputPotentiallyBlankItemInTable">
																<xsl:with-param name="item" select="Name"/>
															</xsl:call-template>
														</td>
														<td>
															<xsl:call-template name="OutputPotentiallyBlankItemInTable">
																<xsl:with-param name="item" select="Description"/>
															</xsl:call-template>
														</td>
														<td>
															<xsl:value-of select="$count"/>
															<xsl:if test="$count=1">
																<xsl:text> instance</xsl:text>
															</xsl:if>
															<xsl:if test="$count != 1">
																<xsl:text> instances</xsl:text>
															</xsl:if>
														</td>
													</tr>
												</xsl:if>
											</xsl:for-each>
										</table>
									</xsl:if>
									<xsl:variable name="bAreUnusedEnvironments">
										<xsl:for-each select="$PhEnvironments">
											<xsl:variable name="env" select="@Id"/>
											<xsl:variable name="countAllo" select="count($PhoneEnvs[@dst=$env])"/>
											<xsl:variable name="countInfix" select="count($Positions[@dst=$env])"/>
											<xsl:if test="$countAllo = 0 and $countInfix = 0">Y</xsl:if>
										</xsl:for-each>
									</xsl:variable>
									<xsl:if test="$bAreUnusedEnvironments!=''">
										<p>
											<xsl:text>The following is a complete list of the phonological environments that are not being used: </xsl:text>
										</p>
										<table type="tLeftOffset" border="1">
											<tr>
												<th>Representation</th>
												<th>Name</th>
												<th>Description</th>
												<th>Count</th>
											</tr>
											<xsl:for-each select="$PhEnvironments">
												<xsl:sort select="Name"/>
												<xsl:sort select="@StringRepresentation"/>
												<xsl:variable name="env" select="@Id"/>
												<xsl:variable name="countAllo" select="count($PhoneEnvs[@dst=$env])"/>
												<xsl:variable name="countInfix" select="count($Positions[@dst=$env])"/>
												<xsl:if test="$countAllo = 0 and $countInfix = 0">
													<tr>
														<td>
															<xsl:if test="$bVernRightToLeft='1'">
																<xsl:attribute name="type">tRtl</xsl:attribute>
															</xsl:if>
															<genericTarget id="Environment.{@Id}"/>
															<xsl:call-template name="OutputStringRepresentation"/>
														</td>
														<td>
															<xsl:call-template name="OutputPotentiallyBlankItemInTable">
																<xsl:with-param name="item" select="Name"/>
															</xsl:call-template>
														</td>
														<td>
															<xsl:call-template name="OutputPotentiallyBlankItemInTable">
																<xsl:with-param name="item" select="Description"/>
															</xsl:call-template>
														</td>
														<td>
															<xsl:text>0 instances</xsl:text>
														</td>
													</tr>
												</xsl:if>
											</xsl:for-each>
										</table>
									</xsl:if>
								</xsl:otherwise>
							</xsl:choose>
						</section2>
						<xsl:if test="$MoInflClasses">
							<section2 id="sAllomorphyInflClasses" type="tH2">
								<secTitle>Inflection Classes</secTitle>
								<p>
									<xsl:text>This analysis of </xsl:text>
									<xsl:value-of select="$sLangName"/>
									<xsl:if test="$PhEnvironments">
										<xsl:text> also </xsl:text>
									</xsl:if>
									<xsl:text> has allomorphy that is lexically conditioned by inflection class. </xsl:text>
								</p>
								<xsl:for-each select="$POSes[InflectionClasses/MoInflClass]">
									<p>
										<xsl:text>The category </xsl:text>
										<genericRef>
											<xsl:attribute name="gref">
												<xsl:text>sCat.</xsl:text>
												<xsl:value-of select="@Id"/>
											</xsl:attribute>
											<xsl:call-template name="Capitalize">
												<xsl:with-param name="sStr">
													<xsl:value-of select="Name"/>
												</xsl:with-param>
											</xsl:call-template>
										</genericRef>
										<xsl:text> has the inflection classes shown in the following table.  </xsl:text>
										<xsl:choose>
											<xsl:when test="@DefaultInflectionClass!='0'">
												<xsl:text>The default inflection class is </xsl:text>
												<xsl:variable name="sDefault" select="@DefaultInflectionClass"/>
												<xsl:call-template name="Capitalize">
													<xsl:with-param name="sStr">
														<xsl:value-of select="$MoInflClasses[@Id=$sDefault]/Name"/>
													</xsl:with-param>
												</xsl:call-template>
												<xsl:text>.</xsl:text>
											</xsl:when>
											<xsl:otherwise>
												<xsl:text>There is no default inflection class for this category.</xsl:text>
											</xsl:otherwise>
										</xsl:choose>
									</p>
									<table type="tLeftOffset" border="1">
										<tr>
											<th>Name</th>
											<th>Description</th>
											<th>Stem count</th>
											<th>Affix count</th>
										</tr>
										<xsl:for-each select="InflectionClasses/MoInflClass">
											<xsl:sort select="Name"/>
											<xsl:call-template name="OutputInflectionClassWithStemAffixCounts"/>
											<xsl:call-template name="OutputInflectionSubclassesWithStemAffixCounts">
												<xsl:with-param name="iDepth" select="number(1)"/>
											</xsl:call-template>
										</xsl:for-each>
									</table>
								</xsl:for-each>
							</section2>
						</xsl:if>
						<xsl:if test="$MoStemNames">
							<section2 id="sAllomorphyStemNames" type="tH2">
								<secTitle>Stem Names</secTitle>
								<p>
									<xsl:text>This analysis of </xsl:text>
									<xsl:value-of select="$sLangName"/>
									<xsl:if test="$PhEnvironments or $MoInflClasses">
										<xsl:text> also </xsl:text>
									</xsl:if>
									<xsl:text> has stem allomorphy that is conditioned by one or more sets of inflection features.  Each stem name has one or more sets of inflection features associated with it.  When a stem allomorph is tagged with a stem name, then the word containing that stem allomorph must have the inflection features contained in at least one of the feature sets of that stem name.</xsl:text>
								</p>
								<xsl:for-each select="$POSes[StemNames/MoStemName]">
									<p>
										<xsl:variable name="cStemNameCount" select="count(StemNames/MoStemName)"/>
										<xsl:text>The category </xsl:text>
										<genericRef>
											<xsl:attribute name="gref">
												<xsl:text>sCat.</xsl:text>
												<xsl:value-of select="@Id"/>
											</xsl:attribute>
											<xsl:call-template name="Capitalize">
												<xsl:with-param name="sStr">
													<xsl:value-of select="Name"/>
												</xsl:with-param>
											</xsl:call-template>
										</genericRef>
										<xsl:text> has the stem name</xsl:text>
										<xsl:if test="count(StemNames/MoStemName) &gt; 1">s</xsl:if>
										<xsl:text> shown in the following table.  </xsl:text>
										<xsl:if test="SubPossibilities">
											<xsl:call-template name="OutputListOfSubcategories">
												<xsl:with-param name="sText1">
													<xsl:text>  Any stem names shown here are valid for not only the </xsl:text>
												</xsl:with-param>
												<xsl:with-param name="pos" select="."/>
											</xsl:call-template>
										</xsl:if>
									</p>
									<table type="tLeftOffset" border="1">
										<tr>
											<th>Name</th>
											<th>Description</th>
											<th>Feature Sets</th>
											<th>Stem count</th>
										</tr>
										<xsl:for-each select="StemNames/MoStemName">
											<xsl:sort select="Name"/>
											<tr>
												<td>
													<xsl:call-template name="Capitalize">
														<xsl:with-param name="sStr">
															<xsl:value-of select="Name"/>
														</xsl:with-param>
													</xsl:call-template>
												</td>
												<td>
													<xsl:call-template name="OutputPotentiallyBlankItemInTable">
														<xsl:with-param name="item" select="Description"/>
													</xsl:call-template>
												</td>
												<xsl:variable name="cRegionCount" select="count(Regions/FsFeatStruc)"/>
												<td>
													<xsl:choose>
														<xsl:when test="$cRegionCount &gt; 1">
															<table>
																<xsl:for-each select="Regions/FsFeatStruc">
																	<tr>
																		<td>
																			<xsl:choose>
																				<xsl:when test="position() = 1">
																					<xsl:text>&#xa0;</xsl:text>
																				</xsl:when>
																				<xsl:otherwise>or</xsl:otherwise>
																			</xsl:choose>
																		</td>
																		<td>
																			<xsl:call-template name="OutputFeatureStructure">
																				<xsl:with-param name="fs" select="."/>
																			</xsl:call-template>
																		</td>
																	</tr>
																</xsl:for-each>
															</table>
														</xsl:when>
														<xsl:when test="$cRegionCount=1">
															<xsl:call-template name="OutputFeatureStructure">
																<xsl:with-param name="fs" select="Regions/FsFeatStruc"/>
															</xsl:call-template>
														</xsl:when>
														<xsl:otherwise>
															<object type="tWarning">No feature set has been specified!  This should be fixed.</object>
														</xsl:otherwise>
													</xsl:choose>
												</td>
												<td>
													<xsl:variable name="stemName" select="@Id"/>
													<xsl:variable name="count" select="count($MoStemAllomorphs[@StemName=$stemName])"/>
													<xsl:text disable-output-escaping="yes">&amp;nbsp;</xsl:text>
													<xsl:value-of select="$count"/>
													<xsl:text> stem allomorph</xsl:text>
													<xsl:if test="$count != 1">
														<xsl:text>s</xsl:text>
													</xsl:if>
												</td>
											</tr>
										</xsl:for-each>
									</table>
								</xsl:for-each>
							</section2>
						</xsl:if>
						<xsl:if test="$affixAlloFeats">
							<section2 id="sAllomorphyAffixAlloFeats" type="tH2">
								<secTitle>Affix Allomorphy Conditioned by Inflectional Features</secTitle>
								<p>
									<xsl:text>This analysis of </xsl:text>
									<xsl:value-of select="$sLangName"/>
									<xsl:if test="$PhEnvironments or $MoInflClasses or $MoStemNames">
										<xsl:text> also </xsl:text>
									</xsl:if>
									<xsl:text> has affix allomorphy that is conditioned by one or more inflection features.  The allomorph is only licit when the stem it attaches to or the inflectional affix template it is a part of bears these required features.</xsl:text>
								</p>
								<table type="tInflClasses" border="1">
									<tr>
										<th>Affix</th>
										<th>Entry</th>
										<th>Required Features</th>
									</tr>
									<xsl:for-each select="$affixAlloFeats">
										<xsl:sort select="Form"/>
										<tr>
											<td>
												<xsl:call-template name="OutputAllomorphAndGloss">
													<xsl:with-param name="Allo" select="@Id"/>
												</xsl:call-template>
											</td>
											<td>
												<xsl:variable name="thisAllo" select="@Id"/>
												<xsl:call-template name="OutputCitationAndGloss">
													<xsl:with-param name="LexEntry" select="key('LexAllos',@Id)/.."/>
												</xsl:call-template>
											</td>
											<td>
												<xsl:call-template name="OutputFeatureStructure">
													<xsl:with-param name="fs" select="MsEnvFeatures/FsFeatStruc"/>
												</xsl:call-template>
											</td>
										</tr>
									</xsl:for-each>
								</table>
							</section2>
						</xsl:if>
					</xsl:otherwise>
				</xsl:choose>
			</section1>
			<section1 id="sNaturalClasses" type="tH1">
				<secTitle>Natural Classes</secTitle>
				<xsl:variable name="NCSegments" select="/M3Dump/PhPhonData/NaturalClasses/PhNCSegments"/>
				<xsl:variable name="NCFeatures" select="/M3Dump/PhPhonData/NaturalClasses/PhNCFeatures[Name[not(contains(.,'Created automatically'))]]"/>
				<xsl:variable name="iCountSegments" select="count($NCSegments)"/>
				<xsl:variable name="iCountFeatures" select="count($NCFeatures)"/>
				<xsl:variable name="iCount" select="$iCountSegments + $iCountFeatures"/>
				<xsl:choose>
					<xsl:when test="$iCount=0">
						<p>No natural classes have been defined in this analysis of <xsl:value-of select="$sLangName"/>.</p>
					</xsl:when>
					<xsl:otherwise>
						<p>The following natural classes have been defined in this analysis of  <xsl:value-of select="$sLangName"/>.</p>
						<table type="tLeftOffset" border="1">
							<tr>
								<th>Class</th>
								<th>
									<xsl:choose>
										<xsl:when test="$iCountSegments != 0 and $iCountFeatures != 0">
											<xsl:text>Phonemes/</xsl:text>
											<br/>
											<xsl:text>Features</xsl:text>
										</xsl:when>
										<xsl:when test="$iCountSegments = 0 and $iCountFeatures != 0">
											<xsl:text>Features</xsl:text>
										</xsl:when>
										<xsl:otherwise>
											<xsl:text>Phonemes</xsl:text>
										</xsl:otherwise>
									</xsl:choose>
								</th>
								<th>Class Name</th>
								<th>Class Description</th>
							</tr>
							<xsl:for-each select="$NCSegments | $NCFeatures">
								<xsl:sort select="Abbreviation"/>
								<tr>
									<td>
										<xsl:call-template name="OutputPotentiallyBlankItemInTable">
											<xsl:with-param name="item" select="Abbreviation"/>
										</xsl:call-template>
									</td>
									<td>
										<xsl:choose>
											<xsl:when test="name()='PhNCFeatures'">
												<xsl:call-template name="OutputPhonologicalFeatureStructure">
													<xsl:with-param name="fs" select="Features/FsFeatStruc"/>
												</xsl:call-template>
											</xsl:when>
											<xsl:otherwise>
												<xsl:if test="$bVernRightToLeft='1'">
													<xsl:attribute name="type">tRtl</xsl:attribute>
												</xsl:if>
												<xsl:call-template name="ProcessSegments">
													<xsl:with-param name="Segments" select="Segments"/>
												</xsl:call-template>
											</xsl:otherwise>
										</xsl:choose>
									</td>
									<td>
										<xsl:call-template name="OutputPotentiallyBlankItemInTable">
											<xsl:with-param name="item" select="Name"/>
										</xsl:call-template>
									</td>
									<td>
										<xsl:call-template name="OutputPotentiallyBlankItemInTable">
											<xsl:with-param name="item" select="Description"/>
										</xsl:call-template>
									</td>
								</tr>
							</xsl:for-each>
						</table>
					</xsl:otherwise>
				</xsl:choose>
			</section1>
			<xsl:if test="$PhonologicalRules">
				<section1 id="sPhonologicalRules" type="tH1">
					<secTitle>Phonological Rules</secTitle>
					<p>
						<xsl:text>This analysis of </xsl:text>
						<xsl:value-of select="$sLangName"/>
						<xsl:text> has the following phonological rule</xsl:text>
						<xsl:if test="count($PhonologicalRules) &gt; 1">
							<xsl:text>s</xsl:text>
						</xsl:if>
						<xsl:text>.</xsl:text>
						<xsl:if test="count($PhonologicalRules) &gt; 1">
							<xsl:text>  They are applied in the order given.</xsl:text>
						</xsl:if>
					</p>
					<xsl:for-each select="$PhonologicalRules">
						<section2 id="sPhonologicalRules.{position()}" type="tH2">
							<secTitle>
								<xsl:choose>
									<xsl:when test="string-length(normalize-space(Name)) &gt; 0 and Name!=$sEmptyContent">
										<xsl:value-of select="normalize-space(Name)"/>
									</xsl:when>
									<xsl:otherwise>
										<xsl:text>(This rule has not been given a name yet.)</xsl:text>
									</xsl:otherwise>
								</xsl:choose>
							</secTitle>
							<xsl:choose>
								<xsl:when test="name()='PhMetathesisRule'">
									<xsl:call-template name="OutputMetathesisRule"/>
								</xsl:when>
								<xsl:otherwise>
									<xsl:call-template name="OutputRegularPhonologicalRule"/>
								</xsl:otherwise>
							</xsl:choose>
							<p>
								<xsl:choose>
									<xsl:when test="string-length(normalize-space(Description)) &gt; 0 and Description!=$sEmptyContent">
										<xsl:value-of select="normalize-space(Description)"/>
									</xsl:when>
									<xsl:otherwise>
										<xsl:text>(This rule has not been given a description yet.  Ideally, the description describes in words what the effect of the rule is.  The idea is that even if someone does not understand the notations of the rule, they will still be able to understand what the rule does.)</xsl:text>
									</xsl:otherwise>
								</xsl:choose>
								<xsl:text>  This rule </xsl:text>
								<xsl:choose>
									<xsl:when test="@Direction='0'">
										<xsl:text>is applied left-to-right, in an iterative manner.</xsl:text>
									</xsl:when>
									<xsl:when test="@Direction='1'">
										<xsl:text>is applied right-to-left, in an iterative manner.</xsl:text>
									</xsl:when>
									<xsl:otherwise>
										<xsl:text>has simultaneous application.</xsl:text>
									</xsl:otherwise>
								</xsl:choose>
								<xsl:variable name="requiredCategories" select="RightHandSides/PhSegRuleRHS/InputPOSes/RequiredPOS"/>
								<xsl:variable name="requiredRuleFeats" select="RightHandSides/PhSegRuleRHS/ReqRuleFeats/RuleFeat"/>
								<xsl:variable name="excludedRuleFeats" select="RightHandSides/PhSegRuleRHS/ExclRuleFeats/RuleFeat"/>
								<xsl:if test="$requiredCategories or $requiredRuleFeats or $excludedRuleFeats">
									<xsl:text>  It also only applies </xsl:text>
									<xsl:if test="$requiredCategories">
										<xsl:text>when the category of the stem is </xsl:text>
									   <xsl:call-template name="OutputPhonologicalRuleRequiredPOSes">
										   <xsl:with-param name="requiredCategories" select="$requiredCategories"/>
									   </xsl:call-template>
									</xsl:if>
									<xsl:if test="$requiredRuleFeats">
										<xsl:if test="$requiredCategories">
											<xsl:text>; </xsl:text>
											<xsl:if test="not($excludedRuleFeats)">
												<xsl:text>and </xsl:text>
											</xsl:if>
										</xsl:if>
										<xsl:text>when the stem has all of the following properties: </xsl:text>
										<xsl:for-each select="$requiredRuleFeats">
											<xsl:value-of select="key('PhonRuleFeatsID', @dst)/Name"/>
											<xsl:call-template name="OutputListPunctuation">
												<xsl:with-param name="sFinalPunctuation" select="''"/>
											</xsl:call-template>
										</xsl:for-each>
									</xsl:if>
									<xsl:if test="$excludedRuleFeats">
										<xsl:if test="$requiredCategories or $requiredRuleFeats">
											<xsl:text>; and </xsl:text>
										</xsl:if>
										<xsl:text>when the stem has none of the following properties: </xsl:text>
										<xsl:for-each select="$excludedRuleFeats">
											<xsl:value-of select="key('PhonRuleFeatsID', @dst)/Name"/>
											<xsl:call-template name="OutputListPunctuation">
												<xsl:with-param name="sConjunction" select="' or '"/>
												<xsl:with-param name="sFinalPunctuation" select="''"/>
											</xsl:call-template>
										</xsl:for-each>
									</xsl:if>
									<xsl:text>.  </xsl:text>
								 </xsl:if>
							</p>
						</section2>
					</xsl:for-each>
				</section1>
			</xsl:if>
			<xsl:if test="$PhonologicalFeatureSystem">
				<xsl:call-template name="OutputFeatureSystem">
					<xsl:with-param name="FeatureSystem" select="$PhonologicalFeatureSystem"/>
					<xsl:with-param name="sLangName" select="$sLangName"/>
					<xsl:with-param name="sKindId" select="'Ph'"/>
					<xsl:with-param name="sKind" select="'phonological'"/>
					<xsl:with-param name="bShowTypes" select="'N'"/>
				</xsl:call-template>
			</xsl:if>
			<xsl:if test="$ShowResidue">
				<section1 id="sResidue" type="tH1">
					<secTitle>Residue</secTitle>
					<xsl:if test="$UnderspecifiedDerivationalAffixes">
						<section2 id="sResidueUnderspecifiedDerivationalAffixes" type="tH2">
							<secTitle>Underspecified Derivational Affixes</secTitle>
							<p>
								<xsl:text>This analysis of </xsl:text>
								<xsl:value-of select="$sLangName"/> has the following underspecified derivational affix<xsl:if test="count($UnderspecifiedDerivationalAffixes) != '1'">es</xsl:if>
								<xsl:text disable-output-escaping="yes">.  Underspecified derivational affixes are those affixes which have been classified as being derivational yet do not have a &amp;ldquo;from category&amp;rdquo;, a &amp;ldquo;to category&amp;rdquo;, or both.  If the &amp;ldquo;from category&amp;rdquo; is not specified, then the affix can attach to a stem of any category.  If the &amp;ldquo;to category&amp;rdquo; is not specified, then the category of the stem to which the affix attaches will be used as the category of the resulting stem.</xsl:text>
							</p>
							<table type="tLeftOffset" border="1">
								<tr>
									<th>Form</th>
									<th>Gloss</th>
									<th>From Category</th>
									<th>To Category</th>
								</tr>
								<xsl:for-each select="$UnderspecifiedDerivationalAffixes">
									<xsl:sort select="CitationForm"/>
									<xsl:sort select="LexemeForm"/>
									<xsl:variable name="lexEntry" select="."/>
									<xsl:variable name="problemMsas" select="key('MsaID',MorphoSyntaxAnalysis/@dst)[name()='MoDerivAffMsa' and @FromPartOfSpeech='0' or name()='MoDerivAffMsa' and @ToPartOfSpeech='0']"/>
									<xsl:for-each select="$problemMsas">
										<tr>
											<xsl:if test="position()=1">
												<xsl:call-template name="GetCitationForm">
													<xsl:with-param name="LexEntry" select="$lexEntry"/>
													<xsl:with-param name="iRowSpan" select="count($problemMsas)"/>
												</xsl:call-template>
											</xsl:if>
											<td>
												<xsl:text> '</xsl:text>
												<xsl:value-of select="key('SensesMsa',@Id)/Gloss"/>
												<xsl:text>'</xsl:text>
											</td>
											<td>
												<xsl:call-template name="OutputPOSAsNameWithRef">
													<xsl:with-param name="sPOSId" select="@FromPartOfSpeech"/>
												</xsl:call-template>
											</td>
											<td>
												<xsl:call-template name="OutputPOSAsNameWithRef">
													<xsl:with-param name="sPOSId" select="@ToPartOfSpeech"/>
												</xsl:call-template>
											</td>
										</tr>
									</xsl:for-each>
								</xsl:for-each>
							</table>
						</section2>
					</xsl:if>
					<xsl:if test="$UnderspecifiedInflectionalAffixes">
						<section2 id="sResidueUnderspecifiedInflectionalAffixes" type="tH2">
							<secTitle>Underspecified Inflectional Affixes</secTitle>
							<p>
								<xsl:text>This analysis of </xsl:text>
								<xsl:value-of select="$sLangName"/> has the following underspecified inflectional affix<xsl:if test="count($UnderspecifiedInflectionalAffixes) != '1'">es</xsl:if>
								<xsl:text disable-output-escaping="yes">.  Underspecified inflectional affixes are those affixes which have been classified as being inflectional yet do not have a specified category or, if a category is specified, do not have a specified slot for that category.  If the category is not specified, then the affix can attach to any stem.  If the category is specified, but the slot is not specified, then the affix can attach to any stem of that category.</xsl:text>
							</p>
							<table type="tLeftOffset" border="1">
								<tr>
									<th>Form</th>
									<th>Gloss</th>
									<th>Category</th>
									<th>Slot</th>
								</tr>
								<xsl:for-each select="$UnderspecifiedInflectionalAffixes">
									<xsl:sort select="CitationForm"/>
									<xsl:sort select="LexemeForm"/>
									<xsl:variable name="lexEntry" select="."/>
									<xsl:variable name="problemMsas" select="key('MsaID',MorphoSyntaxAnalysis/@dst)[name()='MoInflAffMsa' and not(@PartOfSpeech) or name()='MoInflAffMsa' and not(Slots)]"/>
									<xsl:for-each select="$problemMsas">
										<tr>
											<xsl:if test="position()=1">
												<xsl:call-template name="GetCitationForm">
													<xsl:with-param name="LexEntry" select="$lexEntry"/>
													<xsl:with-param name="iRowSpan" select="count($problemMsas)"/>
												</xsl:call-template>
											</xsl:if>
											<td>
												<xsl:text> '</xsl:text>
												<xsl:value-of select="key('SensesMsa',@Id)/Gloss"/>
												<xsl:text>'</xsl:text>
											</td>
											<td>
												<xsl:call-template name="OutputPOSAsNameWithRef">
													<xsl:with-param name="sPOSId" select="@PartOfSpeech"/>
												</xsl:call-template>
											</td>
											<td>
												<xsl:text>Unknown slot</xsl:text>
											</td>
										</tr>
									</xsl:for-each>
								</xsl:for-each>
							</table>
						</section2>
					</xsl:if>
					<xsl:if test="$UnmarkedAffixes">
						<section2 id="sResidueUnmarkedAffixes" type="tH2">
							<secTitle>Unclassified Affixes</secTitle>
							<p>
								<xsl:text>This analysis of </xsl:text>
								<xsl:value-of select="$sLangName"/> has the following unclassified affix<xsl:if test="count($UnmarkedAffixes) != '1'">es</xsl:if>.  (Unclassified affixes are those which have not yet been classified as being either inflectional or derivational.)  Any categories which the affix can attach to are shown.  If none are shown, then the affix can attach to any category.
							</p>
							<table type="tLeftOffset">
								<xsl:for-each select="$UnmarkedAffixes">
									<xsl:sort select="CitationForm"/>
									<xsl:sort select="LexemeForm"/>
									<tr>
										<xsl:choose>
											<xsl:when test="$bVernRightToLeft='1'">
												<td>
													<table cellpadding="0" cellspacing="0">
														<xsl:variable name="affix" select="."/>
														<xsl:for-each select="$POSes">
															<xsl:sort select="Abbreviation"/>
															<xsl:variable name="cat">
																<xsl:value-of select="@Id"/>
															</xsl:variable>
															<xsl:for-each select="key('MsaID',$affix/MorphoSyntaxAnalysis/@dst)[name()='MoUnclassifiedAffixMsa' and @PartOfSpeech=$cat]">
																<tr>
																	<td>
																		<xsl:for-each select="key('POSID',$cat)">
																			<genericRef>
																				<xsl:attribute name="gref">
																					<xsl:text>sCat.</xsl:text>
																					<xsl:value-of select="$cat"/>
																				</xsl:attribute>
																				<xsl:value-of select="Name"/>
																			</genericRef>
																		</xsl:for-each>
																	</td>
																</tr>
															</xsl:for-each>
														</xsl:for-each>
													</table>
												</td>
												<td>
													<xsl:text> '</xsl:text>
													<xsl:value-of select="key('SensesID',Sense/@dst)/Gloss"/>
													<xsl:text>'</xsl:text>
												</td>
												<xsl:call-template name="GetCitationForm">
													<xsl:with-param name="LexEntry" select="."/>
												</xsl:call-template>
											</xsl:when>
											<xsl:otherwise>
												<xsl:call-template name="GetCitationForm">
													<xsl:with-param name="LexEntry" select="."/>
												</xsl:call-template>
												<td>
													<xsl:text> '</xsl:text>
													<xsl:value-of select="key('SensesID',Sense/@dst)/Gloss"/>
													<xsl:text>'</xsl:text>
												</td>
												<td>
													<table cellpadding="0" cellspacing="0">
														<xsl:variable name="affix" select="."/>
														<xsl:for-each select="$POSes">
															<xsl:sort select="Abbreviation"/>
															<xsl:variable name="cat">
																<xsl:value-of select="@Id"/>
															</xsl:variable>
															<xsl:for-each select="key('MsaID',$affix/MorphoSyntaxAnalysis/@dst)[name()='MoUnclassifiedAffixMsa' and @PartOfSpeech=$cat]">
																<tr>
																	<td>
																		<xsl:for-each select="key('POSID',$cat)">
																			<genericRef>
																				<xsl:attribute name="gref">
																					<xsl:text>sCat.</xsl:text>
																					<xsl:value-of select="$cat"/>
																				</xsl:attribute>
																				<xsl:value-of select="Name"/>
																			</genericRef>
																		</xsl:for-each>
																	</td>
																</tr>
															</xsl:for-each>
														</xsl:for-each>
													</table>
												</td>
											</xsl:otherwise>
										</xsl:choose>
									</tr>
								</xsl:for-each>
							</table>
						</section2>
					</xsl:if>
					<xsl:if test="$UnmarkedStems">
						<section2 id="sResidueUnmarkedStems" type="tH2">
							<xsl:variable name="iCount" select="count($UnmarkedStems)"/>
							<secTitle>Unmarked Stems</secTitle>
							<p>
								<xsl:text>This analysis of </xsl:text>
								<xsl:value-of select="$sLangName"/>
								<xsl:text> has the following </xsl:text>
								<xsl:choose>
									<xsl:when test="$iCount &gt; 9">
										<xsl:value-of select="$iCount"/>
										<xsl:text> stems</xsl:text>
									</xsl:when>
									<xsl:otherwise>
										<xsl:text> stem</xsl:text>
										<xsl:if test="$iCount != '1'">s</xsl:if>
									</xsl:otherwise>
								</xsl:choose>
								<xsl:text> which </xsl:text>
								<xsl:choose>
									<xsl:when test="$iCount != '1'">have</xsl:when>
									<xsl:otherwise>has</xsl:otherwise>
								</xsl:choose>
								<xsl:text> not yet been marked for category.</xsl:text>
							</p>
							<table type="tLeftOffset">
								<xsl:for-each select="$UnmarkedStems">
									<xsl:sort select="CitationForm"/>
									<tr>
										<xsl:choose>
											<xsl:when test="$bVernRightToLeft='1'">
												<td>
													<xsl:text> '</xsl:text>
													<xsl:value-of select="key('SensesID',Sense/@dst)/Gloss"/>
													<xsl:text>'</xsl:text>
												</td>
												<xsl:call-template name="GetCitationForm">
													<xsl:with-param name="LexEntry" select="."/>
												</xsl:call-template>
											</xsl:when>
											<xsl:otherwise>
												<xsl:call-template name="GetCitationForm">
													<xsl:with-param name="LexEntry" select="."/>
												</xsl:call-template>
												<td>
													<xsl:text> '</xsl:text>
													<xsl:value-of select="key('SensesID',Sense/@dst)/Gloss"/>
													<xsl:text>'</xsl:text>
												</td>
											</xsl:otherwise>
										</xsl:choose>
									</tr>
								</xsl:for-each>
							</table>
						</section2>
					</xsl:if>
					<xsl:if test="$MoMorphAdhocProhibs | $MoAlloAdhocProhibs | $MoAdhocProhibGrs">
						<section2 id="sResidueAdHocs" type="tH2">
							<secTitle>Ad hoc constraints</secTitle>
							<p>The following sets of morphemes or allomorphs never co-occur in the same wordform, but the morphological description given above does not yet offer an explanation.  They are listed as follows:</p>
							<xsl:variable name="sGroupedAdHocs">
								<xsl:text>Grouped ad hoc sequences</xsl:text>
							</xsl:variable>
							<xsl:variable name="sMorphemeAdHocs">
								<xsl:text>Morpheme ad hoc sequences</xsl:text>
							</xsl:variable>
							<xsl:variable name="sAllomorphAdHocs">
								<xsl:text>Allomorph ad hoc sequences</xsl:text>
							</xsl:variable>
							<ul>
								<xsl:if test="$MoAdhocProhibGrs">
									<li>
										<genericRef gref="sResidueGroup">
											<xsl:value-of select="$sGroupedAdHocs"/>
										</genericRef>
									</li>
								</xsl:if>
								<xsl:if test="$MoMorphAdhocProhibs[not(ancestor::MoAdhocProhibGr)]">
									<li>
										<genericRef gref="sResidueMorphemes">
											<xsl:value-of select="$sMorphemeAdHocs"/>
										</genericRef>
									</li>
								</xsl:if>
								<xsl:if test="$MoAlloAdhocProhibs[not(ancestor::MoAdhocProhibGr)]">
									<li>
										<genericRef gref="sResidueAllomorphs">
											<xsl:value-of select="$sAllomorphAdHocs"/>
										</genericRef>
									</li>
								</xsl:if>
							</ul>
							<xsl:if test="$MoAdhocProhibGrs">
								<section3 id="sResidueGroup">
									<secTitle>
										<xsl:value-of select="$sGroupedAdHocs"/>
									</secTitle>
									<p>The following sets of ad hoc constraints are grouped together with some kind of common factor.</p>
									<xsl:for-each select="$MoAdhocProhibGrs">
										<xsl:sort select="Name"/>
										<section4>
											<xsl:attribute name="id">
												<xsl:text>sResidueGroup</xsl:text>
												<xsl:value-of select="position()"/>
											</xsl:attribute>
											<secTitle>
												<xsl:choose>
													<xsl:when test="Name">
														<xsl:value-of select="Name"/>
													</xsl:when>
													<xsl:otherwise>Nameless group</xsl:otherwise>
												</xsl:choose>
											</secTitle>
											<xsl:if test="Description">
												<p>
													<xsl:value-of select="Description"/>
												</p>
											</xsl:if>
											<table type="tLeftOffset" border="1">
												<tr>
													<th>Key item</th>
													<th>Cannot occur</th>
													<th>Other items</th>
												</tr>
												<xsl:for-each select="MoMorphAdhocProhib | MoAlloAdhocProhib">
													<xsl:sort select="FirstMorpheme/@dst | FirstAllomorph/@dst"/>
													<xsl:variable name="bIsMorpheme">
														<xsl:choose>
															<xsl:when test="name()='MoMorphAdhocProhib'">Y</xsl:when>
															<xsl:otherwise>N</xsl:otherwise>
														</xsl:choose>
													</xsl:variable>
													<tr>
														<td>
															<xsl:choose>
																<xsl:when test="$bIsMorpheme='Y'">
																	<xsl:call-template name="OutputFirstMorphemeAdhocMember"/>
																</xsl:when>
																<xsl:otherwise>
																	<xsl:call-template name="OutputFirstAllomorphAdhocMember"/>
																</xsl:otherwise>
															</xsl:choose>
														</td>
														<td valign="middle">
															<xsl:call-template name="OutputAdjacencyValue"/>
														</td>
														<td>
															<xsl:choose>
																<xsl:when test="$bIsMorpheme='Y'">
																	<xsl:call-template name="OutputRestMorphemeAdhocMembers"/>
																</xsl:when>
																<xsl:otherwise>
																	<xsl:call-template name="OutputRestAllomorphAdhocMembers"/>
																</xsl:otherwise>
															</xsl:choose>
														</td>
													</tr>
												</xsl:for-each>
											</table>
										</section4>
									</xsl:for-each>
								</section3>
							</xsl:if>
							<xsl:if test="$MoMorphAdhocProhibs[not(ancestor::MoAdhocProhibGr)]">
								<section3 id="sResidueMorphemes">
									<secTitle>
										<xsl:value-of select="$sMorphemeAdHocs"/>
									</secTitle>
									<p>The following table delineates the sets of morphemes which may not co-occur:</p>
									<table type="tLeftOffset" border="1">
										<tr>
											<th>Key morpheme</th>
											<th>Cannot occur</th>
											<th>Other morphemes</th>
										</tr>
										<xsl:for-each select="$MoMorphAdhocProhibs[not(key('AdhocGroup',@Id))]">
											<xsl:sort select="FirstMorpheme/@dst"/>
											<tr>
												<td>
													<xsl:call-template name="OutputFirstMorphemeAdhocMember"/>
												</td>
												<td valign="middle">
													<xsl:call-template name="OutputAdjacencyValue"/>
												</td>
												<td>
													<xsl:call-template name="OutputRestMorphemeAdhocMembers"/>
												</td>
											</tr>
										</xsl:for-each>
									</table>
								</section3>
							</xsl:if>
							<xsl:if test="$MoAlloAdhocProhibs[not(ancestor::MoAdhocProhibGr)]">
								<section3 id="sResidueAllomorphs">
									<secTitle>
										<xsl:value-of select="$sAllomorphAdHocs"/>
									</secTitle>
									<p>The following table delineates the sets of allomorph/morpheme pairs which may not co-occur:</p>
									<table type="tLeftOffset" border="1">
										<tr>
											<th>Key allomorph/morpheme</th>
											<th>Cannot occur</th>
											<th>Other allomorph/morphemes</th>
										</tr>
										<xsl:for-each select="$MoAlloAdhocProhibs[not(key('AdhocGroup',@Id))]">
											<xsl:sort select="FirstAllomorph/@dst"/>
											<tr>
												<td>
													<xsl:call-template name="OutputFirstAllomorphAdhocMember"/>
												</td>
												<td valign="middle">
													<xsl:call-template name="OutputAdjacencyValue"/>
												</td>
												<td>
													<xsl:call-template name="OutputRestAllomorphAdhocMembers"/>
												</td>
											</tr>
										</xsl:for-each>
									</table>
								</section3>
							</xsl:if>
						</section2>
					</xsl:if>
					<xsl:if test="string-length($sUnusedSlots) &gt; 0">
						<section2 id="sResidueSlots" type="tH2">
							<secTitle>Unused Affix Slots</secTitle>
							<p>
								<xsl:text>This analysis of </xsl:text>
								<xsl:value-of select="$sLangName"/>
								<xsl:text> has the following affix slot</xsl:text>
								<xsl:variable name="sPlural">
									<xsl:if test="string-length(substring-after($sUnusedSlots,',')) &gt; 0">
										<xsl:text>s</xsl:text>
									</xsl:if>
								</xsl:variable>
								<xsl:value-of select="$sPlural"/>
								<xsl:text> which </xsl:text>
								<xsl:choose>
									<xsl:when test="contains($sPlural,'s')">have</xsl:when>
									<xsl:otherwise>has</xsl:otherwise>
								</xsl:choose>
								<xsl:text> been defined but </xsl:text>
								<xsl:choose>
									<xsl:when test="contains($sPlural,'s')">are</xsl:when>
									<xsl:otherwise>is</xsl:otherwise>
								</xsl:choose>
								<xsl:text> never used in any affix template within the category hierarchy.  </xsl:text>
							</p>
							<table type="tLeftOffset" border="1">
								<tr>
									<th>Category</th>
									<th>Unused Slot</th>
								</tr>
								<xsl:call-template name="OutputUnusedSlots">
									<xsl:with-param name="sUnusedSlots" select="$sUnusedSlots"/>
								</xsl:call-template>
							</table>
						</section2>
					</xsl:if>
					<xsl:if test="string-length($sUnusedExceptionFeatures) &gt; 0">
						<section2 id="sResidueExceptionFeatures" type="tH2">
							<secTitle>
								Unused Exception <xsl:text disable-output-escaping="yes">&amp;ldquo;Features&amp;rdquo;</xsl:text>
							</secTitle>
							<p>
								<xsl:text>This analysis of </xsl:text>
								<xsl:value-of select="$sLangName"/>
								<xsl:text> has the following </xsl:text>
								<xsl:call-template name="OutputProductivityRestrictionLabel">
									<xsl:with-param name="sFinal" select="'s'"/>
								</xsl:call-template>
								<xsl:text> which have been defined but are never used in any lexical entry.  </xsl:text>
								<xsl:text disable-output-escaping="yes">(Exception &amp;ldquo;features&amp;rdquo; are typically used to constrain certain affixes to only occur on specially marked stems; that is, they restrict the productivity of these affixes.)</xsl:text>
							</p>
							<table type="tLeftOffset">
								<xsl:call-template name="OutputUnusedItems">
									<xsl:with-param name="sUnusedItems" select="$sUnusedExceptionFeatures"/>
									<xsl:with-param name="sKeyName" select="'ExceptionFeaturesID'"/>
								</xsl:call-template>
							</table>
							<!-- Not for now
							<xsl:if test="string-length($sUnusedBearableFeatures) &gt; 0">
								<p>
									<xsl:value-of select="$sLangName"/>
									<xsl:text> has the following categories which have been marked as having exception features, but some of those exception features are never used.  </xsl:text>
									<xsl:if test="string-length($sUnusedExceptionFeatures) = 0">
										<xsl:value-of select="$sExceptionFeatureExplanation"/>
									</xsl:if>
								</p>
								<table type="tLeftOffset">
									<tr>
										<th>Category</th>
										<th>Unused exception feature</th>
									</tr>
									<xsl:call-template name="OutputUnusedBearableFeatures">
										<xsl:with-param name="sUnusedBearableFeatures" select="$sUnusedBearableFeatures"/>
									</xsl:call-template>
								</table>
							</xsl:if>
							-->
						</section2>
					</xsl:if>
					<xsl:if test="string-length($sUnusedInflectionClassesStems) &gt; 0 or string-length($sUnusedInflectionClassesAffixes) &gt; 0">
						<section2 id="sResidueInflectionClasses" type="tH2">
							<secTitle>Unused Inflection Classes</secTitle>
							<xsl:if test="string-length($sUnusedInflectionClassesStems) &gt; 0">
								<p>
									<xsl:text>This analysis of </xsl:text>
									<xsl:value-of select="$sLangName"/>
									<xsl:text> has the following inflection classes which have been defined but are never assigned to any stem.  </xsl:text>
								</p>
								<table type="tLeftOffset">
									<xsl:call-template name="OutputUnusedItems">
										<xsl:with-param name="sUnusedItems" select="$sUnusedInflectionClassesStems"/>
										<xsl:with-param name="sKeyName" select="'InflectionClassesID'"/>
									</xsl:call-template>
								</table>
							</xsl:if>
							<xsl:if test="string-length($sUnusedInflectionClassesAffixes) &gt; 0">
								<p>
									<xsl:text>This analysis of </xsl:text>
									<xsl:value-of select="$sLangName"/>
									<xsl:text> has the following inflection classes which have been defined but are never assigned to any affix.  </xsl:text>
								</p>
								<table type="tLeftOffset">
									<xsl:call-template name="OutputUnusedItems">
										<xsl:with-param name="sUnusedItems" select="$sUnusedInflectionClassesAffixes"/>
										<xsl:with-param name="sKeyName" select="'InflectionClassesID'"/>
									</xsl:call-template>
								</table>
							</xsl:if>
							<p>See section <sectionRef sec="sAllomorphyInflClasses"/> for more information on these inflection classes.</p>
						</section2>
					</xsl:if>
					<xsl:if test="string-length($sUnusedStemNamesStems) &gt; 0 or string-length($sUnusedStemNamesAffixes) &gt; 0 or string-length($sStemNamesWithoutFeatureSets) &gt; 0">
						<section2 id="sResidueStemNames" type="tH2">
							<secTitle>Stem Names</secTitle>
							<xsl:if test="string-length($sUnusedStemNamesStems) &gt; 0">
								<p>
									<xsl:text>This analysis of </xsl:text>
									<xsl:value-of select="$sLangName"/>
									<xsl:text> has the following stem names which have been defined but are never used in any lexical entry.</xsl:text>
								</p>
								<table type="tLeftOffset">
									<xsl:call-template name="OutputUnusedItems">
										<xsl:with-param name="sUnusedItems" select="$sUnusedStemNamesStems"/>
										<xsl:with-param name="sKeyName" select="'StemNamesID'"/>
									</xsl:call-template>
								</table>
							</xsl:if>
							<xsl:if test="string-length($sUnusedStemNamesAffixes) &gt; 0">
								<p>
									<xsl:text>This analysis of </xsl:text>
									<xsl:value-of select="$sLangName"/>
									<xsl:text> has the following stem names which have features for which there are no inflectional affixes which bear the feature and </xsl:text>
									<xsl:text>for which there are no derivationial affixes whose resulting stem bears the feature.</xsl:text>
								</p>
								<table type="tLeftOffset" border="1">
									<tr>
										<th>Stem Name</th>
										<th>Features</th>
									</tr>
									<xsl:call-template name="OutputUnusedStemNameFeatures">
										<xsl:with-param name="sUnusedItems" select="$sUnusedStemNamesAffixes"/>
									</xsl:call-template>
								</table>
							</xsl:if>
							<xsl:if test="string-length($sStemNamesWithoutFeatureSets) &gt; 0">
								<p>
									<xsl:text>This analysis of </xsl:text>
									<xsl:value-of select="$sLangName"/>
									<xsl:text> has the following stem names which do not have any feature sets defined.</xsl:text>
								</p>
								<table type="tLeftOffset">
									<xsl:call-template name="OutputUnusedItems">
										<xsl:with-param name="sUnusedItems" select="$sStemNamesWithoutFeatureSets"/>
										<xsl:with-param name="sKeyName" select="'StemNamesID'"/>
									</xsl:call-template>
								</table>
							</xsl:if>
							<p>See section <sectionRef sec="sAllomorphyStemNames"/> for more information on these stem names.</p>
						</section2>
					</xsl:if>
				</section1>
			</xsl:if>
			<backMatter>
				<xsl:call-template name="MorphsByMorphTypeAppendix">
					<xsl:with-param name="sLangName">
						<xsl:value-of select="$sLangName"/>
					</xsl:with-param>
				</xsl:call-template>
				<xsl:call-template name="MorphsByCatAppendix">
					<xsl:with-param name="sLangName">
						<xsl:value-of select="$sLangName"/>
					</xsl:with-param>
				</xsl:call-template>
			</backMatter>
			<languages>
				<language id="lVern" name="vernacular">
					<xsl:if test="$bVernRightToLeft='1'">
						<xsl:attribute name="rtl">yes</xsl:attribute>
					</xsl:if>
					<xsl:if test="$prmVernacularFontSize">
						<xsl:attribute name="font-size">
							<xsl:value-of select="$prmVernacularFontSize"/>
						</xsl:attribute>
					</xsl:if>
					<xsl:attribute name="font-family">
						<xsl:value-of select="/M3Dump/LangProject/VernWss/WritingSystem/DefaultFont"/>
					</xsl:attribute>
					<xsl:attribute name="font-weight">bold</xsl:attribute>
					<xsl:attribute name="color">blue</xsl:attribute>
				</language>
				<language id="lGloss" name="gloss">
					<xsl:if test="$bGlossRightToLeft='1'">
						<xsl:attribute name="rtl">yes</xsl:attribute>
					</xsl:if>
					<xsl:if test="$prmGlossFontSize">
						<xsl:attribute name="font-size">
							<xsl:value-of select="$prmGlossFontSize"/>
						</xsl:attribute>
					</xsl:if>
					<xsl:attribute name="font-family">
						<xsl:value-of select="/M3Dump/LangProject/AnalysisWss/WritingSystem/DefaultFont"/>
					</xsl:attribute>
				</language>
				<language id="lIPA" name="IPA">
					<xsl:attribute name="font-family">
						<xsl:text>Charis SIL</xsl:text>
					</xsl:attribute>
				</language>
			</languages>
			<xsl:call-template name="DoTypes"/>
		</lingPaper>
	</xsl:template>
	<xsl:template name="OutputPhonologicalRuleRequiredPOSes">
		<xsl:param name="requiredCategories"/>
		<xsl:for-each select="key('POSID',$requiredCategories/@dst)">
			<xsl:if test="position()=last() and count($requiredCategories) &gt; 1">
				<xsl:text>or </xsl:text>
			</xsl:if>
			<genericRef>
				<xsl:attribute name="gref">
					<xsl:text>sCat.</xsl:text>
					<xsl:value-of select="@Id"/>
				</xsl:attribute>
				<xsl:value-of select="Name"/>
			</genericRef>
			<xsl:variable name="subcats">
				<xsl:call-template name="GetAnyNestedCategoriesForListing">
					<xsl:with-param name="pos" select="."/>
				</xsl:call-template>
			</xsl:variable>
			<xsl:choose>
				<xsl:when test="function-available('saxon:node-set')">
					<xsl:if test="saxon:node-set($subcats)/subcat">
						<xsl:text> or its </xsl:text>
						<xsl:choose>
							<xsl:when test="saxon:node-set($subcats)[count(subcat) &gt; 1]">subcategories: </xsl:when>
							<xsl:otherwise>subcategory </xsl:otherwise>
						</xsl:choose>
						<xsl:for-each select="saxon:node-set($subcats)/subcat">
							<xsl:sort select="name"/>
							<xsl:call-template name="OutputSubcategoryNameAsList">
								<xsl:with-param name="sConjunction" select="' or '"/>
								<xsl:with-param name="sFinalPunctuation" select="''"/>
							</xsl:call-template>
						</xsl:for-each>
					</xsl:if>
				</xsl:when>
				<xsl:otherwise>
					<xsl:if test="msxslt:node-set($subcats)/subcat">
						<xsl:text> or  its </xsl:text>
						<xsl:choose>
							<xsl:when test="msxslt:node-set($subcats)[count(subcat) &gt; 1]">subcategories: </xsl:when>
							<xsl:otherwise>subcategory </xsl:otherwise>
						</xsl:choose>
						<xsl:for-each select="msxslt:node-set($subcats)/subcat">
							<xsl:sort select="name"/>
							<xsl:call-template name="OutputSubcategoryNameAsList">
								<xsl:with-param name="sConjunction" select="' or '"/>
								<xsl:with-param name="sFinalPunctuation" select="''"/>
							</xsl:call-template>
						</xsl:for-each>
					</xsl:if>
				</xsl:otherwise>
			</xsl:choose>
			<xsl:if test="position()!=last()">
				<xsl:choose>
					<xsl:when test="count($requiredCategories) &gt; 2">
						<xsl:text>, </xsl:text>
					</xsl:when>
					<xsl:otherwise>
						<xsl:text>&#x20;</xsl:text>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:if>
		</xsl:for-each>

	</xsl:template>
	<xsl:template name="OutputPhonemeCount">
		<xsl:param name="sLangName"/>
		<xsl:param name="iPhonemeCount"/>
		<xsl:value-of select="$sLangName"/>
		<xsl:text> has </xsl:text>
		<xsl:value-of select="$iPhonemeCount"/>
		<xsl:text> phoneme</xsl:text>
		<xsl:if test="$iPhonemeCount &gt; 1">
			<xsl:text>s</xsl:text>
		</xsl:if>
	</xsl:template>
	<xsl:template name="OutputPhonemeFeatureMatrix">
		<table type="tLeftOffset" border="1">
			<tr>
				<th>&#xa0;</th>
				<xsl:for-each select="$PhonologicalFeatureSystem">
					<xsl:sort select="Abbreviation"/>
					<th>
						<genericRef gref="sFeature.{@Id}">
							<xsl:value-of select="Abbreviation"/>
						</genericRef>
					</th>
				</xsl:for-each>
			</tr>
			<xsl:for-each select="$PhPhonemes">
				<xsl:sort select="Name"/>
				<xsl:variable name="phoneme" select="."/>
				<tr>
					<th>
						<genericRef gref="gPhoneme.{@Id}.{Codes/PhCode[1]/@Id}">
							<langData lang="lVern">
								<xsl:value-of select="Name"/>
							</langData>
						</genericRef>
					</th>
					<xsl:for-each select="$PhonologicalFeatureSystem">
						<xsl:sort select="Abbreviation"/>
						<xsl:variable name="sFeatureID" select="@Id"/>
						<td align="center">
							<xsl:variable name="columnsFeature" select="$phoneme/PhonologicalFeatures/FsFeatStruc/FsClosedValue[@Feature=$sFeatureID]"/>
							<xsl:choose>
								<xsl:when test="$columnsFeature">
									<xsl:value-of select="key('FsSymFeatValsID', $columnsFeature/@Value)/Abbreviation"/>
								</xsl:when>
								<xsl:otherwise>
									<xsl:text>&#xa0;</xsl:text>
								</xsl:otherwise>
							</xsl:choose>
						</td>
					</xsl:for-each>
				</tr>
			</xsl:for-each>
		</table>
	</xsl:template>
	<xsl:template name="OutputPhonemeTable">
		<xsl:variable name="bShowNames">
			<xsl:choose>
				<xsl:when test="$PhPhonemes/Codes[count(PhCode)>1]">Y</xsl:when>
				<xsl:otherwise>N</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<xsl:variable name="bShowBasicIPASymbol">
			<xsl:choose>
				<xsl:when test="$PhPhonemes[string-length(normalize-space(BasicIPASymbol)) &gt; 0 and BasicIPASymbol!=$sEmptyContent]">Y</xsl:when>
				<xsl:otherwise>N</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<xsl:variable name="bShowFeatures">
			<xsl:choose>
				<xsl:when test="$PhPhonemes/PhonologicalFeatures/FsFeatStruc[count(FsClosedValue) &gt; 0]">Y</xsl:when>
				<xsl:otherwise>N</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<table border="1" type="tLeftOffset">
			<tr>
				<th>Representation</th>
				<xsl:if test="$bShowBasicIPASymbol='Y'">
					<th>Basic IPA Symbol</th>
				</xsl:if>
				<xsl:if test="$bShowNames='Y'">
					<th>Name</th>
				</xsl:if>
				<th>Description</th>
				<xsl:if test="$bShowFeatures='Y'">
					<th>Phonological Features</th>
				</xsl:if>
			</tr>
			<xsl:for-each select="/M3Dump/PhPhonData/PhonemeSets/PhPhonemeSet//Codes/PhCode/Representation">
				<xsl:sort select="."/>
				<xsl:variable name="phoneme" select="ancestor::PhPhoneme"/>
				<xsl:if test="$phoneme">
					<tr>
						<td>
							<genericTarget id="gPhoneme.{$phoneme/@Id}.{../@Id}"/>
							<langData lang="lVern">
								<xsl:value-of select="."/>
							</langData>
						</td>
						<xsl:if test="$bShowBasicIPASymbol='Y'">
							<td>
								<langData lang="lIPA">
									<xsl:choose>
										<xsl:when test="string-length(normalize-space($phoneme/BasicIPASymbol)) &gt; 0 and $phoneme/BasicIPASymbol!=$sEmptyContent">
											<xsl:value-of select="$phoneme/BasicIPASymbol"/>
										</xsl:when>
										<xsl:otherwise>
											<xsl:text>&#xa0;</xsl:text>
										</xsl:otherwise>
									</xsl:choose>
								</langData>
							</td>
						</xsl:if>
						<xsl:if test="$bShowNames='Y'">
							<td>
								<langData lang="lVern">
									<xsl:value-of select="$phoneme/Name"/>
								</langData>
							</td>
						</xsl:if>
						<td>
							<xsl:value-of select="$phoneme/Description"/>
						</td>
						<xsl:if test="$bShowFeatures='Y'">
							<td>
								<xsl:choose>
									<xsl:when test="count($phoneme/PhonologicalFeatures/FsFeatStruc/FsClosedValue) &gt; 0">
										<xsl:call-template name="OutputPhonologicalFeatureStructure">
											<xsl:with-param name="fs" select="$phoneme/PhonologicalFeatures/FsFeatStruc"/>
										</xsl:call-template>
									</xsl:when>
									<xsl:otherwise>
										<xsl:text>&#xa0;</xsl:text>
									</xsl:otherwise>
								</xsl:choose>
							</td>
						</xsl:if>
					</tr>
				</xsl:if>
			</xsl:for-each>
		</table>
	</xsl:template>
	<xsl:template match="PhIterationContext">
		<table>
			<tr>
				<td valign="middle">
					<xsl:apply-templates select="key('ContextsID', Member/@dst)"/>
				</td>
				<td>
					<table cellspacing="0pt" cellpadding="0pt">
						<tr>
							<td>
								<xsl:if test="@Maximum!=0">
									<object type="tSub">
										<xsl:choose>
											<xsl:when test="@Maximum=-1">
												<object type="tPhonRuleInfinity"/>
											</xsl:when>
											<xsl:otherwise>
												<xsl:value-of select="@Maximum"/>
											</xsl:otherwise>
										</xsl:choose>
									</object>
								</xsl:if>
							</td>
						</tr>
						<tr>
							<td>
								<object type="tSub">
									<xsl:value-of select="@Minimum"/>
								</object>
							</td>
						</tr>
					</table>
				</td>
			</tr>
		</table>
	</xsl:template>
	<xsl:template match="PhSequenceContext">
		<table>
			<tr>
				<xsl:for-each select="Members">
					<td valign="middle">
						<xsl:apply-templates select="key('ContextsID', @dst)"/>
					</td>
				</xsl:for-each>
			</tr>
		</table>
	</xsl:template>
	<xsl:template match="PhSimpleContextBdry">
		<xsl:variable name="marker" select="key('BoundaryMarkersID', @dst)"/>
		<xsl:choose>
			<xsl:when test="$marker/@Guid=$sMorphemeBoundary">
				<xsl:text>+</xsl:text>
			</xsl:when>
			<xsl:when test="$marker/@Guid=$sWordBoundary">#</xsl:when>
		</xsl:choose>
	</xsl:template>
	<xsl:template match="PhSimpleContextNC">
		<xsl:variable name="class" select="key('NaturalClassesID', @dst)"/>
		<xsl:choose>
			<xsl:when test="name($class)='PhNCSegments'">
				<xsl:text>[</xsl:text>
				<xsl:value-of select="$class/Abbreviation"/>
				<xsl:text>]</xsl:text>
			</xsl:when>
			<xsl:when test="not(PlusConstr) and not(MinusConstr) and $class/Abbreviation='C' or not(PlusConstr) and not(MinusConstr) and $class/Abbreviation='V'">
				<xsl:value-of select="$class/Abbreviation"/>
			</xsl:when>
			<xsl:otherwise>
				<table cellpadding="1pt" cellspacing="0pt">
					<!--
			   We have to get the feature name to sort whether it comes from the feature or from some constraints.
			   In addition, we have to have the alpha variables be the same between this element and its sisters.
			   Since I'm not aware of any other way to determine the order of the sort, we use a string to indicate
			   when we have a feature (f) or one of the two constraints (c).  We then count the number of c's to
			   figure out what the alpha should be.
			   -->
					<!-- Create the string -->
					<xsl:variable name="sSortedItemKinds">
						<xsl:for-each select="$class/Features/FsFeatStruc/FsClosedValue | PlusConstr | MinusConstr">
							<xsl:sort select="key('FsClosedFeaturesID', @Feature)/Name | key('FsClosedFeaturesID', key('PhFeatureConstraintsID', @dst)/@Feature)/Name"/>
							<xsl:choose>
								<xsl:when test="name()='FsClosedValue'">
									<xsl:text>f</xsl:text>
								</xsl:when>
								<xsl:otherwise>
									<xsl:text>c</xsl:text>
								</xsl:otherwise>
							</xsl:choose>
						</xsl:for-each>
					</xsl:variable>
					<xsl:for-each select="$class/Features/FsFeatStruc/FsClosedValue | PlusConstr | MinusConstr">
						<xsl:sort select="key('FsClosedFeaturesID', @Feature)/Name | key('FsClosedFeaturesID', key('PhFeatureConstraintsID', @dst)/@Feature)/Name"/>
						<tr>
							<xsl:choose>
								<xsl:when test="name()='FsClosedValue'">
									<xsl:call-template name="OutputPhonologicalFeatureValuePair">
										<xsl:with-param name="iPosition" select="position()"/>
										<xsl:with-param name="sValue" select="key('FsSymFeatValsID', @Value)/Abbreviation"/>
										<xsl:with-param name="sFeatureName" select="key('FsClosedFeaturesID', @Feature)/Name"/>
										<xsl:with-param name="sFeatureId" select="@Feature"/>
									</xsl:call-template>
								</xsl:when>
								<xsl:otherwise>
									<!-- Count the number of c's in the string up to this point. -->
									<xsl:variable name="sItemKindsToThisPoint" select="substring($sSortedItemKinds,1,position())"/>
									<xsl:variable name="sConstraintItemsToThisPoint" select="translate($sItemKindsToThisPoint, 'f', '')"/>
									<xsl:variable name="iConstraintPosition" select="string-length($sConstraintItemsToThisPoint) - 1"/>
									<xsl:variable name="constraint" select="key('PhFeatureConstraintsID', @dst)"/>
									<xsl:choose>
										<xsl:when test="name()='PlusConstr'">
											<xsl:call-template name="OutputPhonologicalFeatureConstraint">
												<xsl:with-param name="feature" select="key('FsClosedFeaturesID', $constraint/Feature/@dst)"/>
												<xsl:with-param name="ord" select="$iConstraintPosition"/>
											</xsl:call-template>
										</xsl:when>
										<xsl:when test="name()='MinusConstr'">
											<xsl:call-template name="OutputPhonologicalFeatureConstraint">
												<xsl:with-param name="feature" select="key('FsClosedFeaturesID', $constraint/Feature/@dst)"/>
												<xsl:with-param name="ord" select="$iConstraintPosition"/>
												<xsl:with-param name="bIsMinus" select="'Y'"/>
											</xsl:call-template>
										</xsl:when>
									</xsl:choose>
								</xsl:otherwise>
							</xsl:choose>
						</tr>
					</xsl:for-each>
				</table>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<xsl:template match="PhSimpleContextSeg">
		<xsl:value-of select="key('PhonemesID', @dst)/Name"/>
	</xsl:template>
	<xsl:template name="OutputMetathesisRule">
		<xsl:variable name="iLeftEnvironmentIndex" select="substring-before(StrucChange, ' ') + 1"/>
		<xsl:variable name="iRightTargetIndex" select="substring-before(substring-after(StrucChange, ' '),' ') + 1"/>
		<xsl:variable name="sMorphBoundary" select="substring-before(substring-after(substring-after(StrucChange, ' '),' '), ' ')"/>
		<xsl:variable name="iLeftTargetIndex" select="substring-before(substring-after(substring-after(substring-after(StrucChange, ' '),' '), ' '), ' ') + 1"/>
		<xsl:variable name="iRightEnvironmentIndex" select="substring-after(substring-after(substring-after(substring-after(StrucChange, ' '),' '), ' '), ' ') + 1"/>
		<table border="1" type="tLeftOffset">
			<tr>
				<td>&#xa0;</td>
				<td align="center">Left environment</td>
				<td align="center" colspan="2">
					<xsl:text>Switch these items</xsl:text>
				</td>
				<td align="center">Right environment</td>
			</tr>
			<tr>
				<td align="left" valign="middle">Input</td>
				<td align="center" valign="middle">
					<xsl:call-template name="OutputMetathesisRuleLeftEnvironment">
						<xsl:with-param name="iLeftEnvironmentIndex" select="$iLeftEnvironmentIndex"/>
					</xsl:call-template>
				</td>
				<td align="center" valign="middle">
					<table>
						<tr>
							<td>
								<xsl:call-template name="OutputPhonologicalRuleItem">
									<xsl:with-param name="item" select="StrucDesc/child::*[$iLeftTargetIndex]"/>
								</xsl:call-template>
							</td>
							<xsl:if test="contains($sMorphBoundary,':L')">
								<td align="center" valign="bottom">
									<xsl:text>+</xsl:text>
								</td>
							</xsl:if>
						</tr>
					</table>
				</td>
				<td align="center" valign="middle">
					<table>
						<tr>
							<xsl:if test="contains($sMorphBoundary,':R')">
								<td align="center" valign="bottom">
									<xsl:text>+</xsl:text>
								</td>
							</xsl:if>
							<td>
								<xsl:call-template name="OutputPhonologicalRuleItem">
									<xsl:with-param name="item" select="StrucDesc/child::*[$iRightTargetIndex]"/>
								</xsl:call-template>
							</td>
						</tr>
					</table>
				</td>
				<td align="center" valign="middle">
					<xsl:call-template name="OutputMetathesisRuleRightEnvironment">
						<xsl:with-param name="iRightEnvironmentIndex" select="$iRightEnvironmentIndex"/>
					</xsl:call-template>
				</td>
			</tr>
			<tr>
				<td align="left" valign="middle">Result</td>
				<td align="center" valign="middle">
					<xsl:call-template name="OutputMetathesisRuleLeftEnvironment">
						<xsl:with-param name="iLeftEnvironmentIndex" select="$iLeftEnvironmentIndex"/>
					</xsl:call-template>
				</td>
				<td align="center" valign="middle">
					<table>
						<tr>
							<td>
								<xsl:call-template name="OutputPhonologicalRuleItem">
									<xsl:with-param name="item" select="StrucDesc/child::*[$iRightTargetIndex]"/>
								</xsl:call-template>
							</td>
							<xsl:if test="contains($sMorphBoundary,':L')">
								<td align="center" valign="bottom">
									<xsl:text>+</xsl:text>
								</td>
							</xsl:if>
						</tr>
					</table>
				</td>
				<td align="center" valign="middle">
					<table>
						<tr>
							<xsl:if test="contains($sMorphBoundary,':R')">
								<td align="center" valign="bottom">
									<xsl:text>+</xsl:text>
								</td>
							</xsl:if>
							<td>
								<xsl:call-template name="OutputPhonologicalRuleItem">
									<xsl:with-param name="item" select="StrucDesc/child::*[$iLeftTargetIndex]"/>
								</xsl:call-template>
							</td>
						</tr>
					</table>
				</td>
				<td align="center" valign="middle">
					<xsl:call-template name="OutputMetathesisRuleRightEnvironment">
						<xsl:with-param name="iRightEnvironmentIndex" select="$iRightEnvironmentIndex"/>
					</xsl:call-template>
				</td>
			</tr>
		</table>
	</xsl:template>
	<xsl:template name="OutputMetathesisRuleRightEnvironment">
		<xsl:param name="iRightEnvironmentIndex"/>
		<xsl:choose>
			<xsl:when test="$iRightEnvironmentIndex=0">
				<xsl:text>&#xa0;</xsl:text>
			</xsl:when>
			<xsl:otherwise>
				<xsl:variable name="contents" select="StrucDesc/child::*[position() &gt;= $iRightEnvironmentIndex]"/>
				<xsl:call-template name="OutputMetathesisRuleEnvironment">
					<xsl:with-param name="contents" select="$contents"/>
				</xsl:call-template>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<xsl:template name="OutputMetathesisRuleLeftEnvironment">
		<xsl:param name="iLeftEnvironmentIndex"/>
		<xsl:choose>
			<xsl:when test="$iLeftEnvironmentIndex=0">
				<xsl:text>&#xa0;</xsl:text>
			</xsl:when>
			<xsl:otherwise>
				<xsl:variable name="contents" select="StrucDesc/child::*[position() &lt;= $iLeftEnvironmentIndex]"/>
				<xsl:call-template name="OutputMetathesisRuleEnvironment">
					<xsl:with-param name="contents" select="$contents"/>
				</xsl:call-template>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<xsl:template name="OutputMetathesisRuleEnvironment">
		<xsl:param name="contents"/>
		<xsl:choose>
			<xsl:when test="$contents">
				<table>
					<tr>
						<xsl:for-each select="$contents">
							<td valign="middle">
								<xsl:call-template name="OutputPhonologicalRuleItem">
									<xsl:with-param name="item" select="."/>
								</xsl:call-template>
							</td>
						</xsl:for-each>
					</tr>
				</table>
			</xsl:when>
			<xsl:otherwise>
				<xsl:text>&#xa0;</xsl:text>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<xsl:template name="OutputFeatureSystem">
		<xsl:param name="sKind"/>
		<xsl:param name="sKindId"/>
		<xsl:param name="FeatureSystem"/>
		<xsl:param name="sLangName"/>
		<xsl:param name="bShowTypes" select="'Y'"/>
		<xsl:variable name="sKindCapitalized">
			<xsl:call-template name="Capitalize">
				<xsl:with-param name="sStr">
					<xsl:value-of select="$sKind"/>
				</xsl:with-param>
			</xsl:call-template>
		</xsl:variable>
		<section1 id="s{$sKindId}FeatureSystem" type="tH1">
			<secTitle>
				<xsl:value-of select="$sKindCapitalized"/>
				<xsl:text> Feature System</xsl:text>
			</secTitle>
			<xsl:if test="$bShowTypes='Y'">
				<p>
					<xsl:value-of select="$sLangName"/>
					<xsl:text> has a </xsl:text>
					<xsl:value-of select="$sKind"/>
					<xsl:text> feature system with the </xsl:text>
					<xsl:text>feature structure types listed in section </xsl:text>
					<sectionRef sec="s{$sKindId}FeatureSystemTypes"/>
					<xsl:text> and the </xsl:text>
					<xsl:text>features given in section </xsl:text>
					<sectionRef sec="s{$sKindId}FeatureSystemFeatures"/>
					<xsl:text>.</xsl:text>
				</p>
				<section2 id="s{$sKindId}FeatureSystemTypes" type="tH2">
					<secTitle><xsl:value-of select="$sKindCapitalized"/> Feature Structure Types</secTitle>
					<p>
						<xsl:value-of select="$sLangName"/>
						<xsl:text> has a feature system with the following feature structure type</xsl:text>
						<xsl:if test="count($FsFeatStrucTypes) &gt; 1">
							<xsl:text>s</xsl:text>
						</xsl:if>
						<xsl:text>:</xsl:text>
					</p>
					<xsl:for-each select="$FsFeatStrucTypes">
						<xsl:sort select="Name"/>
						<section3>
							<xsl:attribute name="id">
								<xsl:text>sFeatureType.</xsl:text>
								<xsl:value-of select="@Id"/>
							</xsl:attribute>
							<secTitle>
								<xsl:call-template name="Capitalize">
									<xsl:with-param name="sStr">
										<xsl:value-of select="Name"/>
									</xsl:with-param>
								</xsl:call-template>
							</secTitle>
							<p>
								<xsl:choose>
									<xsl:when test="string-length(Description) = 0 or Description=$sEmptyContent">
										<xsl:text>There is no description yet.</xsl:text>
									</xsl:when>
									<xsl:otherwise>
										<xsl:value-of select="Description"/>
									</xsl:otherwise>
								</xsl:choose>
								<xsl:text> It has the following features:</xsl:text>
							</p>
							<table type="tLeftOffset" border="1">
								<tr>
									<th>Name</th>
									<th>Description</th>
								</tr>
								<xsl:for-each select="Features/Feature">
									<xsl:variable name="feature" select="key('InflectableFeatsID', @dst)"/>
									<tr>
										<td>
											<genericRef>
												<xsl:attribute name="gref">
													<xsl:text>sFeature.</xsl:text>
													<xsl:value-of select="@dst"/>
												</xsl:attribute>
												<xsl:value-of select="$feature/Name"/>
											</genericRef>
										</td>
										<td>
											<xsl:choose>
												<xsl:when test="string-length($feature/Description) = 0 or $feature/Description=$sEmptyContent">
													<xsl:text>There is no description yet.</xsl:text>
												</xsl:when>
												<xsl:otherwise>
													<xsl:value-of select="$feature/Description"/>
												</xsl:otherwise>
											</xsl:choose>
										</td>
									</tr>
								</xsl:for-each>
							</table>
						</section3>
					</xsl:for-each>
				</section2>
			</xsl:if>
			<xsl:choose>
				<xsl:when test="$bShowTypes='Y'">
					<section2 id="s{$sKindId}FeatureSystemFeatures" type="tH2">
						<secTitle><xsl:value-of select="$sKindCapitalized"/> Features</secTitle>
						<p>
							<xsl:value-of select="$sLangName"/>
							<xsl:text> has a </xsl:text>
							<xsl:value-of select="$sKind"/>
							<xsl:text> feature system with the following features:</xsl:text>
						</p>
						<xsl:for-each select="$FeatureSystem">
							<xsl:sort select="."/>
							<section3>
								<xsl:call-template name="OutputFeatures"/>
							</section3>
						</xsl:for-each>
					</section2>
				</xsl:when>
				<xsl:otherwise>
					<p>
						<xsl:value-of select="$sLangName"/>
						<xsl:text> has a </xsl:text>
						<xsl:value-of select="$sKind"/>
						<xsl:text> feature system with the following features:</xsl:text>
					</p>
					<xsl:for-each select="$FeatureSystem">
						<xsl:sort select="."/>
						<section2>
							<xsl:call-template name="OutputFeatures"/>
						</section2>
					</xsl:for-each>
				</xsl:otherwise>
			</xsl:choose>
		</section1>
	</xsl:template>
	<xsl:template name="OutputFeatures">
		<xsl:attribute name="id">
			<xsl:text>sFeature.</xsl:text>
			<xsl:value-of select="@Id"/>
		</xsl:attribute>
		<xsl:attribute name="type">tH2</xsl:attribute>
		<secTitle>
			<xsl:call-template name="Capitalize">
				<xsl:with-param name="sStr">
					<xsl:value-of select="Name"/>
				</xsl:with-param>
			</xsl:call-template>
		</secTitle>
		<p>
			<xsl:choose>
				<xsl:when test="string-length(Description) = 0 or Description=$sEmptyContent">
					<xsl:text>There is no description yet.</xsl:text>
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="Description"/>
				</xsl:otherwise>
			</xsl:choose>
			<xsl:text> It has the following possible values:</xsl:text>
		</p>
		<table type="tLeftOffset" border="1">
			<tr>
				<th>Name</th>
				<th>Abbreviation</th>
				<th>Description</th>
			</tr>
			<xsl:choose>
				<xsl:when test="name()='FsClosedFeature'">
					<xsl:for-each select="Values/FsSymFeatVal">
						<tr>
							<td>
								<xsl:call-template name="OutputPotentiallyBlankItemInTable">
									<xsl:with-param name="item" select="Name"/>
								</xsl:call-template>
							</td>
							<td>
								<xsl:call-template name="OutputPotentiallyBlankItemInTable">
									<xsl:with-param name="item" select="Abbreviation"/>
								</xsl:call-template>
							</td>
							<td>
								<xsl:call-template name="OutputPotentiallyBlankItemInTable">
									<xsl:with-param name="item" select="Description"/>
								</xsl:call-template>
							</td>
						</tr>
					</xsl:for-each>
				</xsl:when>
				<xsl:when test="name()='FsComplexFeature'">
					<xsl:variable name="sType" select="Type/@dst"/>
					<xsl:for-each select="$FsFeatStrucTypes[@Id=$sType]/Features/Feature">
						<xsl:variable name="feature" select="key('InflectableFeatsID', @dst)"/>
						<tr>
							<td>
								<genericRef>
									<xsl:attribute name="gref">
										<xsl:text>sFeature.</xsl:text>
										<xsl:value-of select="@dst"/>
									</xsl:attribute>
									<xsl:value-of select="$feature/Name"/>
								</genericRef>
							</td>
							<td>
								<xsl:call-template name="OutputPotentiallyBlankItemInTable">
									<xsl:with-param name="item" select="Abbreviation"/>
								</xsl:call-template>
							</td>
							<td>
								<xsl:call-template name="OutputPotentiallyBlankItemInTable">
									<xsl:with-param name="item" select="$feature/Description"/>
								</xsl:call-template>
							</td>
						</tr>
					</xsl:for-each>
				</xsl:when>
			</xsl:choose>
		</table>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		OutputInflectionFeatures
		display information for inflection features
		Parameters: fs = inflection features to show
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="OutputInflectionFeatures">
		<xsl:param name="fs"/>
		<td>
			<xsl:choose>
				<xsl:when test="$fs/descendant-or-self::FsClosedValue">
					<table cellpadding="0" cellspacing="0">
						<tr>
							<td>
								<xsl:call-template name="OutputFeatureStructure">
									<xsl:with-param name="fs" select="$fs"/>
								</xsl:call-template>
							</td>
						</tr>
					</table>
				</xsl:when>
				<xsl:otherwise>
					<xsl:text disable-output-escaping="yes">&amp;nbsp;</xsl:text>
				</xsl:otherwise>
			</xsl:choose>
		</td>
	</xsl:template>
	<!--
	  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  OutputInflectionSubclassesWithStemAffixCounts
	  Parameters: iDepth = the depth of nesting of subclasses
	  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
   -->
	<xsl:template name="OutputInflectionSubclassesWithStemAffixCounts">
		<xsl:param name="iDepth"/>
		<xsl:variable name="iNewDepth" select="$iDepth + 1"/>
		<xsl:for-each select="Subclasses/MoInflClass">
			<xsl:sort select="Name"/>
			<xsl:call-template name="OutputInflectionClassWithStemAffixCounts">
				<xsl:with-param name="iDepth" select="$iNewDepth"/>
			</xsl:call-template>
			<xsl:call-template name="OutputInflectionSubclassesWithStemAffixCounts">
				<xsl:with-param name="iDepth" select="$iNewDepth"/>
			</xsl:call-template>
		</xsl:for-each>
	</xsl:template>
	<!--
	  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  OutputInflectionClassWithStemAffixCounts
	  Parameters: iDepth = the depth of nesting of subclasses
	  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
   -->
	<xsl:template name="OutputInflectionClassWithStemAffixCounts">
		<xsl:param name="iDepth" select="number(0)"/>
		<tr>
			<td>
				<genericTarget id="InflectionClass.{@Id}"/>
				<xsl:choose>
					<xsl:when test="$iDepth=1">&#xa0;&#xa0;</xsl:when>
					<xsl:when test="$iDepth=2">&#xa0;&#xa0;&#xa0;&#xa0;</xsl:when>
					<xsl:when test="$iDepth=3">&#xa0;&#xa0;&#xa0;&#xa0;&#xa0;&#xa0;</xsl:when>
					<xsl:when test="$iDepth=4">&#xa0;&#xa0;&#xa0;&#xa0;&#xa0;&#xa0;&#xa0;&#xa0;</xsl:when>
					<xsl:when test="$iDepth=5">&#xa0;&#xa0;&#xa0;&#xa0;&#xa0;&#xa0;&#xa0;&#xa0;&#xa0;&#xa0;</xsl:when>
				</xsl:choose>
				<xsl:call-template name="Capitalize">
					<xsl:with-param name="sStr">
						<xsl:value-of select="Name"/>
					</xsl:with-param>
				</xsl:call-template>
			</td>
			<td>
				<xsl:value-of select="Description"/>
			</td>
			<xsl:variable name="inflClass" select="@Id"/>
			<td>
				<xsl:variable name="count" select="count($MorphoSyntaxAnalyses/MoStemMsa[@InflectionClass=$inflClass])"/>
				<xsl:text disable-output-escaping="yes">&amp;nbsp;</xsl:text>
				<xsl:value-of select="$count"/>
				<xsl:if test="$count=1">
					<xsl:text> stem</xsl:text>
				</xsl:if>
				<xsl:if test="$count != 1">
					<xsl:text> stems</xsl:text>
				</xsl:if>
			</td>
			<td>
				<xsl:variable name="count2" select="count($InflectionClasses[@dst=$inflClass])"/>
				<xsl:value-of select="$count2"/>
				<xsl:if test="$count2=1">
					<xsl:text> affix</xsl:text>
				</xsl:if>
				<xsl:if test="$count2 != 1">
					<xsl:text> affixes</xsl:text>
				</xsl:if>
			</td>
		</tr>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
AppendixIntro
	Display introduction to an appendix
		Parameters: sType = the manner inwhich the morphemes are arranged
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="AppendixIntro">
		<xsl:param name="sType"/>
		<p>
			<xsl:text>This appendix lists morphemes by </xsl:text>
			<xsl:value-of select="$sType"/>
			<xsl:text>.</xsl:text>
			<xsl:if test="$prmIMaxMorphsInAppendices != '-1'">
				<xsl:text>  Only the first </xsl:text>
				<xsl:value-of select="$prmSMaxMorphsInAppendices"/>
				<xsl:text> morphemes will be listed for each </xsl:text>
				<xsl:value-of select="$sType"/>
				<xsl:text>.</xsl:text>
			</xsl:if>
		</p>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
AppendixSubsectionBlurb
	Display introduction to a subscetion of an appendix
		Parameters: iCount = the number of items it is possible to show in the subsection
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="AppendixSubsectionBlurb">
		<xsl:param name="sSubsection" select="'subsection'"/>
		<xsl:param name="iCount"/>
		<xsl:text>This </xsl:text>
		<xsl:value-of select="$sSubsection"/>
		<xsl:text> lists </xsl:text>
		<xsl:choose>
			<xsl:when test="$prmIMaxMorphsInAppendices = '-1' or $iCount &lt;= $prmIMaxMorphsInAppendices">
				<xsl:text>all the</xsl:text>
			</xsl:when>
			<xsl:otherwise>
				<xsl:text>the first </xsl:text>
				<xsl:value-of select="$prmSMaxMorphsInAppendices"/>
			</xsl:otherwise>
		</xsl:choose>
		<xsl:text> instances.</xsl:text>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
bPOSContainsInflAffixSlot
	Want to get a nodeset/tree fragment of all MoInflAffixSlot that belong to this POS or any of its subpossibilitiesdisplay part of speech and all sub parts of speech
		Parameters: pos = current PartOfSpeech nodeset
							idSlot = Id attr of MoInflAffixSlot to look for
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="bSlotIsUsedInSomeTemplateOfThisPOS">
		<xsl:param name="pos" select="."/>
		<xsl:param name="idSlot"/>
		<xsl:if test="$pos/descendant::SuffixSlots[@dst=$idSlot] or $pos/descendant::PrefixSlots[@dst=$idSlot]">
			<xsl:text>1</xsl:text>
		</xsl:if>
		<xsl:for-each select="$pos/SubPossibilities">
			<xsl:call-template name="bSlotIsUsedInSomeTemplateOfThisPOS">
				<xsl:with-param name="pos" select="key('POSID',@dst)"/>
				<xsl:with-param name="idSlot" select="$idSlot"/>
			</xsl:call-template>
		</xsl:for-each>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		bPOSContainsInflTemplate
		Determine if this POS or one of its descendents has an inflectional affix template
		Parameters: pos = current PartOfSpeech nodeset
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="bPOSContainsInflTemplate">
		<xsl:param name="pos" select="."/>
		<xsl:if test="$pos/descendant::MoInflAffixTemplate">
			<xsl:text>1</xsl:text>
		</xsl:if>
		<xsl:for-each select="$pos/SubPossibilities">
			<xsl:call-template name="bPOSContainsInflTemplate">
				<xsl:with-param name="pos" select="key('POSID',@dst)"/>
			</xsl:call-template>
		</xsl:for-each>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
bPosMatchesOrIsSubCat
	See if posToMatch matches pos or any of the subcategories of pos
		Parameters: pos = id of current PartOfSpeech nodeset
							 posToMatch = Id of POS to look for
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="bPosMatchesOrIsSubCat">
		<xsl:param name="posToMatch"/>
		<xsl:param name="pos"/>
		<xsl:choose>
			<xsl:when test="$pos=$posToMatch">Y</xsl:when>
			<xsl:otherwise>
				<xsl:variable name="thisPos" select="key('POSID', $pos)"/>
				<xsl:for-each select="$thisPos/PartOfSpeech">
					<xsl:call-template name="bPosMatchesOrIsSubCat">
						<xsl:with-param name="pos">
							<xsl:value-of select="@Id"/>
						</xsl:with-param>
						<xsl:with-param name="posToMatch">
							<xsl:value-of select="$posToMatch"/>
						</xsl:with-param>
					</xsl:call-template>
				</xsl:for-each>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		CalculateStemNamesWithoutFeatureSets
		output ids of any stem names which do not have any feature sets
		Parameters: none
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="CalculateStemNamesWithoutFeatureSets">
		<xsl:for-each select="$MoStemNames">
			<xsl:sort select="Name"/>
			<xsl:variable name="idStemName">
				<xsl:value-of select="@Id"/>
			</xsl:variable>
			<xsl:if test="not(Regions/FsFeatStruc)">
				<xsl:value-of select="$idStemName"/>
				<xsl:text>,</xsl:text>
			</xsl:if>
		</xsl:for-each>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CalculateUnusedBearableFeatures
	output ids of any unused bearable features for any POSes
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="CalculateUnusedBearableFeatures">
		<xsl:for-each select="$POSes/BearableFeatures/BearableFeature">
			<xsl:sort select="../../Name"/>
			<xsl:variable name="pos">
				<xsl:value-of select="../../@Id"/>
			</xsl:variable>
			<xsl:variable name="idFeature">
				<xsl:value-of select="@dst"/>
			</xsl:variable>
			<xsl:variable name="sPosFeature">
				<xsl:value-of select="$pos"/>
				<xsl:text>,</xsl:text>
				<xsl:value-of select="$idFeature"/>
				<xsl:text>;</xsl:text>
			</xsl:variable>
			<xsl:variable name="values" select="//FsFeatStruc/FsClosedValue[@Feature=$idFeature] | //FsFeatStruc/FsNegatedValue[@Feature=$idFeature]"/>
			<xsl:choose>
				<xsl:when test="count($values)=0">
					<!-- there are none at all; remember POS id and bearable feature dst -->
					<xsl:value-of select="$sPosFeature"/>
				</xsl:when>
				<xsl:otherwise>
					<!-- there are some, but are they for this POS or one of its subcategories? -->
					<xsl:variable name="bUsed">
						<xsl:for-each select="$values">
							<xsl:choose>
								<xsl:when test="ancestor::MoStemMsa">
									<xsl:call-template name="bPosMatchesOrIsSubCat">
										<xsl:with-param name="pos" select="$pos"/>
										<xsl:with-param name="posToMatch">
											<xsl:value-of select="key('POSID', ancestor::MoStemMsa/@PartOfSpeech)/@Id"/>
										</xsl:with-param>
									</xsl:call-template>
								</xsl:when>
								<xsl:when test="ancestor::FromExceptionFeatures">
									<xsl:variable name="posToMatch">
										<xsl:choose>
											<xsl:when test="ancestor::MoDerivAffMsa">
												<xsl:value-of select="ancestor::MoDerivAffMsa/@FromPartOfSpeech"/>
											</xsl:when>
											<xsl:otherwise>
												<!-- MoInflAffMsa -->
												<xsl:value-of select="@PartOfSpeech"/>
											</xsl:otherwise>
										</xsl:choose>
									</xsl:variable>
									<xsl:call-template name="bPosMatchesOrIsSubCat">
										<xsl:with-param name="pos" select="$pos"/>
										<xsl:with-param name="posToMatch" select="$posToMatch"/>
									</xsl:call-template>
								</xsl:when>
								<xsl:when test="ancestor::ToExceptionFeatures and ancestor::MoDerivAffMsa">
									<xsl:call-template name="bPosMatchesOrIsSubCat">
										<xsl:with-param name="pos" select="$pos"/>
										<xsl:with-param name="posToMatch">
											<xsl:value-of select="ancestor::MoDerivAffMsa/@ToPartOfSpeech"/>
										</xsl:with-param>
									</xsl:call-template>
								</xsl:when>
								<xsl:when test="ancestor::MoEndoCompound">
									<xsl:variable name="idMsa">
										<xsl:choose>
											<xsl:when test="ancestor::MoEndoCompound/@HeadLast='0'">
												<xsl:value-of select="ancestor::MoEndoCompound/LeftMsa/@dst"/>
											</xsl:when>
											<xsl:otherwise>
												<xsl:value-of select="ancestor::MoEndoCompound/RightMsa/@dst"/>
											</xsl:otherwise>
										</xsl:choose>
									</xsl:variable>
									<xsl:variable name="sHab" select="key('MsaID', $idMsa)"/>
									<xsl:variable name="shab2">
										<xsl:value-of select="key('POSID', $sHab)/@Id"/>
									</xsl:variable>
									<xsl:call-template name="bPosMatchesOrIsSubCat">
										<xsl:with-param name="pos" select="$pos"/>
										<xsl:with-param name="posToMatch">
											<xsl:value-of select="key('POSID', key('MsaID', $idMsa)/@PartOfSpeech)/@Id"/>
										</xsl:with-param>
									</xsl:call-template>
								</xsl:when>
							</xsl:choose>
						</xsl:for-each>
					</xsl:variable>
					<xsl:if test="string-length($bUsed)=0">
						<!-- none used by this POS; remember POS id and bearable feature dst -->
						<xsl:value-of select="$sPosFeature"/>
					</xsl:if>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:for-each>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CalculateUnusedExceptionFeatures
	output ids of any unused exception features
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="CalculateUnusedExceptionFeatures">
		<xsl:for-each select="$ProdRestricts/CmPossibility">
			<xsl:sort select="Name"/>
			<xsl:variable name="idEF">
				<xsl:value-of select="@Id"/>
			</xsl:variable>
			<xsl:if test="count($ProdRestricts[@dst=$idEF]) + count(//FromProdRestrict[@dst=$idEF]) + count(//ToProdRestrict[@dst=$idEF])=0">
				<xsl:value-of select="$idEF"/>
				<xsl:text>,</xsl:text>
			</xsl:if>
		</xsl:for-each>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CalculateUnusedInflectionClassesAffixes
	output ids of any unused inflection classes in affixes
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="CalculateUnusedInflectionClassesAffixes">
		<xsl:for-each select="$MoInflClasses">
			<xsl:sort select="Name"/>
			<xsl:variable name="idInflClass">
				<xsl:value-of select="@Id"/>
			</xsl:variable>
			<xsl:if test="count($InflectionClasses[@dst=$idInflClass])=0">
				<xsl:value-of select="$idInflClass"/>
				<xsl:text>,</xsl:text>
			</xsl:if>
		</xsl:for-each>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CalculateUnusedInflectionClassesStems
	output ids of any unused inflection classes in stems
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="CalculateUnusedInflectionClassesStems">
		<xsl:for-each select="$MoInflClasses">
			<xsl:sort select="Name"/>
			<xsl:variable name="idInflClass">
				<xsl:value-of select="@Id"/>
			</xsl:variable>
			<xsl:if test="count($MorphoSyntaxAnalyses/MoStemMsa[@InflectionClass=$idInflClass]) + count($MoDerivAffMsas[@ToInflectionClass=$idInflClass])=0">
				<xsl:value-of select="$idInflClass"/>
				<xsl:text>,</xsl:text>
			</xsl:if>
		</xsl:for-each>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		CalculateUnusedSlots
		output ids of any slots which have no templates referring to them (within the appropriate hierarchy)
		Parameters: none
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="CalculateUnusedSlots">
		<xsl:for-each select="$POSes/AffixSlots/MoInflAffixSlot">
			<xsl:variable name="idSlot">
				<xsl:value-of select="@Id"/>
			</xsl:variable>
			<xsl:variable name="bSlotIsUsedInSomeTemplateOfThisPOS">
				<xsl:call-template name="bSlotIsUsedInSomeTemplateOfThisPOS">
					<xsl:with-param name="pos" select="../.."/>
					<xsl:with-param name="idSlot" select="$idSlot"/>
				</xsl:call-template>
			</xsl:variable>
			<xsl:if test="not(contains($bSlotIsUsedInSomeTemplateOfThisPOS,'1'))">
				<xsl:value-of select="$idSlot"/>
				<xsl:text>,</xsl:text>
			</xsl:if>
		</xsl:for-each>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CalculateUnusedStemNamesAffixes
	output ids of any stem names which have no inflectional affixes bearing the their features
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="CalculateUnusedStemNamesAffixes">
		<xsl:for-each select="$MoStemNames">
			<xsl:sort select="Name"/>
			<xsl:variable name="idStemName">
				<xsl:value-of select="@Id"/>
			</xsl:variable>
			<xsl:for-each select="descendant::FsClosedValue">
				<xsl:variable name="snFeature" select="@Feature"/>
				<xsl:variable name="snValue" select="@Value"/>
				<xsl:if test="not($MoInflAffMsas/InflectionFeatures/descendant::FsClosedValue[@Feature=$snFeature and @Value=$snValue]) and not($MoDerivAffMsas/ToMsFeatures/descendant::FsClosedValue[@Feature=$snFeature and @Value=$snValue])">
					<xsl:value-of select="$idStemName"/>
					<xsl:text>:</xsl:text>
					<xsl:value-of select="ancestor::FsFeatStruc[parent::Regions]/@Id"/>
					<xsl:text>,</xsl:text>
				</xsl:if>
			</xsl:for-each>
		</xsl:for-each>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CalculateUnusedStemNamesStems
	output ids of any stem names which are unused by stem allomorphs
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="CalculateUnusedStemNamesStems">
		<xsl:for-each select="$MoStemNames">
			<xsl:sort select="Name"/>
			<xsl:variable name="idStemName">
				<xsl:value-of select="@Id"/>
			</xsl:variable>
			<xsl:if test="count($MoStemAllomorphs[@StemName=$idStemName])=0">
				<xsl:value-of select="$idStemName"/>
				<xsl:text>,</xsl:text>
			</xsl:if>
		</xsl:for-each>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Capitalize
	Ensure the first letter of a string is capitalized
		Parameters: sStr = string to capitalize
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="Capitalize">
		<xsl:param name="sStr"/>
		<xsl:value-of select="translate(substring($sStr,1,1),'abcdefghijklmnopqrstuvwxyz','ABCDEFGHIJKLMNOPQRSTUVWXYZ')"/>
		<xsl:value-of select="substring($sStr,2)"/>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CreateFromCatToCatRef
	Create an id or idref for a from-to category pair
		Parameters: fromCat = from category
							 toCat     = to category
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="CreateFromCatToCatRef">
		<xsl:param name="fromCat"/>
		<xsl:param name="toCat"/>
		<xsl:text>sDeriv.</xsl:text>
		<xsl:value-of select="$fromCat"/>
		<xsl:text>.to.</xsl:text>
		<xsl:value-of select="$toCat"/>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CreatePOSAbbrAsRef
	Create a POS abbreviatoin as a reference
		Parameters: idPOS = ID of the part of speech
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="CreatePOSAbbrAsRef">
		<xsl:param name="idPOS"/>
		<genericRef>
			<xsl:attribute name="gref">
				<xsl:text>sCat.</xsl:text>
				<xsl:value-of select="$idPOS"/>
			</xsl:attribute>
			<xsl:value-of select="Abbreviation"/>
		</genericRef>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DefinePOS
	display definition of part of speech and all sub parts of speech
		Parameters: pos = current PartOfSpeech nodeset
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="DefinePOS">
		<xsl:param name="pos" select="."/>
		<xsl:variable name="sPOSName">
			<xsl:value-of select="$pos/Name"/>
		</xsl:variable>
		<xsl:variable name="idPOS">
			<xsl:value-of select="$pos/@Id"/>
		</xsl:variable>
		<section2 type="tH2">
			<xsl:attribute name="id">
				<xsl:text>sCat.</xsl:text>
				<xsl:value-of select="$idPOS"/>
			</xsl:attribute>
			<secTitle>
				<xsl:call-template name="Capitalize">
					<xsl:with-param name="sStr">
						<xsl:value-of select="$sPOSName"/>
					</xsl:with-param>
				</xsl:call-template>
				<xsl:text> [</xsl:text>
				<xsl:value-of select="Abbreviation"/>
				<xsl:text>]  </xsl:text>
			</secTitle>
			<p>
				<xsl:choose>
					<xsl:when test="string-length(Description) &gt; 0 and Description!=$sEmptyContent">
						<xsl:call-template name="Capitalize">
							<xsl:with-param name="sStr">
								<xsl:value-of select="Description"/>
							</xsl:with-param>
						</xsl:call-template>
					</xsl:when>
					<xsl:otherwise>(This category does not yet have a description.)</xsl:otherwise>
				</xsl:choose>
			</p>
			<xsl:variable name="iCountTemplates" select="count($pos/AffixTemplates/MoInflAffixTemplate)"/>
			<xsl:if test="$iCountTemplates &gt; 0">
				<p>
					<xsl:text>  The </xsl:text>
					<xsl:value-of select="$sPOSName"/>
					<xsl:text> category has </xsl:text>
					<xsl:value-of select="$iCountTemplates"/>
					<xsl:text> inflectional template</xsl:text>
					<xsl:if test="$iCountTemplates &gt; '1'">
						<xsl:text>s</xsl:text>
					</xsl:if>
					<xsl:text>:  </xsl:text>
				</p>
				<table type="tLeftOffset">
					<xsl:for-each select="$pos/AffixTemplates/MoInflAffixTemplate">
						<xsl:sort select="Name"/>
						<tr>
							<td>
								<genericRef>
									<xsl:attribute name="gref">
										<xsl:text>sInflTemplate.</xsl:text>
										<xsl:value-of select="@Id"/>
									</xsl:attribute>
									<xsl:call-template name="OutputInflectionalTemplateName"/>
								</genericRef>
							</td>
						</tr>
					</xsl:for-each>
				</table>
				<xsl:variable name="iSubcatCount" select="count($pos/descendant::SubPossibilities)"/>
				<xsl:if test="$iSubcatCount &gt; 0">
					<p>
						<xsl:choose>
							<xsl:when test="$iCountTemplates &gt; '1'">
								<xsl:text>  These templates are </xsl:text>
							</xsl:when>
							<xsl:otherwise>
								<xsl:text>  This template is </xsl:text>
							</xsl:otherwise>
						</xsl:choose>
						<xsl:text>valid for not only this category, but also </xsl:text>
						<xsl:choose>
							<xsl:when test="$iSubcatCount > 1">
								<xsl:text>all of its subcategories: </xsl:text>
							</xsl:when>
							<xsl:otherwise>
								<xsl:text>its subcategory: </xsl:text>
							</xsl:otherwise>
						</xsl:choose>
						<xsl:call-template name="HandleOutputtingListOfSubcatNames">
							<xsl:with-param name="pos" select="$pos"/>
						</xsl:call-template>
					</p>
				</xsl:if>
			</xsl:if>
			<xsl:if test="$pos/InflectionClasses/MoInflClass">
				<p>
					<xsl:variable name="iCountInflectionClasses" select="count($pos/InflectionClasses/MoInflClass)"/>
					<xsl:text>  The </xsl:text>
					<xsl:value-of select="$sPOSName"/>
					<xsl:text> category has </xsl:text>
					<xsl:value-of select="$iCountInflectionClasses"/>
					<xsl:text> inflection class</xsl:text>
					<xsl:if test="$iCountInflectionClasses &gt; '1'">
						<xsl:text>es</xsl:text>
					</xsl:if>
					<xsl:text>:  </xsl:text>
					<xsl:for-each select="$pos/InflectionClasses/MoInflClass">
						<genericRef>
							<xsl:attribute name="gref">
								<xsl:text>InflectionClass.</xsl:text>
								<xsl:value-of select="@Id"/>
							</xsl:attribute>
							<xsl:value-of select="Name"/>
						</genericRef>
						<xsl:call-template name="OutputListPunctuation"/>
					</xsl:for-each>
					<!-- @#@ put reference to features here -->
				</p>
			</xsl:if>
			<xsl:if test="$pos/InflectableFeats[InflectableFeature]">
				<p>
					<xsl:variable name="iCountInflectableFeats" select="count($pos/InflectableFeats/InflectableFeature)"/>
					<xsl:text>  The </xsl:text>
					<xsl:value-of select="$sPOSName"/>
					<xsl:text> category has </xsl:text>
					<xsl:value-of select="$iCountInflectableFeats"/>
					<xsl:text> inflectable feature</xsl:text>
					<xsl:if test="$iCountInflectableFeats &gt; '1'">
						<xsl:text>s</xsl:text>
					</xsl:if>
					<xsl:text>:  </xsl:text>
					<xsl:for-each select="$pos/InflectableFeats/InflectableFeature">
						<xsl:variable name="feature" select="key('InflectableFeatsID', @dst)"/>
						<genericRef>
							<xsl:attribute name="gref">
								<xsl:text>sFeature.</xsl:text>
								<xsl:value-of select="@dst"/>
							</xsl:attribute>
							<xsl:value-of select="$feature/Name"/>
						</genericRef>
						<xsl:call-template name="OutputListPunctuation"/>
					</xsl:for-each>
					<!-- @#@ put reference to features here -->
				</p>
			</xsl:if>
			<!--
			<xsl:variable name="stemNames" select="$pos/StemNames/MoStemName"/>
			<xsl:if test="$stemNames">
				<p>
					<xsl:variable name="iCountStemNames" select="count($stemNames)"/>
					<xsl:text>  The </xsl:text>
					<xsl:value-of select="$sPOSName"/>
					<xsl:text> category has </xsl:text>
					<xsl:value-of select="$iCountStemNames"/>
					<xsl:text> stem name</xsl:text>
					<xsl:if test="$iCountStemNames &gt; '1'">
						<xsl:text>s</xsl:text>
					</xsl:if>
					<xsl:text>:  </xsl:text>
					<xsl:for-each select="$stemNames">
						<genericRef>
							<xsl:attribute name="gref">
								<xsl:text>sStemName.</xsl:text>
								<xsl:value-of select="@Id"/>
							</xsl:attribute>
							<xsl:value-of select="Name"/>
						</genericRef>
						<xsl:call-template name="OutputListPunctuation"/>
					</xsl:for-each>
				</p>
			</xsl:if>
			-->
			<p>
				<xsl:text>  (See instances from the lexicon in appendix </xsl:text>
				<appendixRef>
					<xsl:attribute name="app">
						<xsl:text>aMorphsByCat.</xsl:text>
						<xsl:value-of select="$idPOS"/>
					</xsl:attribute>
				</appendixRef>
				<xsl:text>.)</xsl:text>
			</p>
		</section2>
		<xsl:for-each select="$pos/PartOfSpeech">
			<xsl:call-template name="DefinePOS">
				<xsl:with-param name="pos" select="."/>
			</xsl:call-template>
		</xsl:for-each>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoTypes
	output types section
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="DoTypes">
		<types>
			<type id="tRtl" cssSpecial="direction:rtl; text-align:right;"/>
			<type id="tH1" font-size="large"/>
			<type id="tH2" font-size="medium"/>
			<type id="tLeftOffset" cssSpecial="margin-left: .25in;"/>
			<type id="tSmaller" font-size="smaller"/>
			<type id="tCitationForm" font-style="italic" font-weight="bold" color="blue"/>
			<type id="tFromToCat" font-weight="bold"/>
			<type id="tNcChart" font-style="italic"/>
			<type id="tSlot" font-weight="bold"/>
			<type id="tSub" cssSpecial="vertical-align: sub;" font-size="65%"/>
			<type id="tSuper" cssSpecial="vertical-align: super;" font-size="65%"/>
			<type id="tDerivationChart" cssSpecial="padding:6; text-align:center; margin-left: .25in;"/>
			<type id="tDerivHeader" font-weight="bold"/>
			<type id="tInflClasses" cssSpecial="padding:4; border:medium none; margin-left: .25in;"/>
			<type id="tNaturalClasses" cssSpecial="padding:4; border:medium none; 	margin-left: .25in;"/>
			<type id="tResidue" cssSpecial="margin-left: .25in; padding:4;"/>
			<type id="tResidueCol" cssSpecial="width:20;"/>
			<type id="tWarning" color="red" font-style="italic"/>
			<type id="tFSBrackets" font-family="Charis SIL" color="black"/>
			<type id="tAlphaVariable" font-family="Charis SIL" color="black"/>
			<type id="tFSCell" cssSpecial="padding-top:0pt; padding-bottom:0pt; "/>
			<type id="tEmbeddedTable" cssSpecial="padding:0pt; margin:0pt; "/>
			<type id="tPhonRuleNullSet" font-family="Charis SIL" color="black" after="&#x2205;"/>
			<type id="tPhonRuleInfinity" font-family="Charis SIL" color="black" after="&#x221e;"/>
			<type id="tIterationCell" cssSpecial="padding-left:0pt; "/>
		</types>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		GetAnyInflSlotsInHigherPOSesForListing
		Output Id and Name for each inflectional affix template in this or any nested PartOfSpeech
		Parameters: pos = current PartOfSpeech nodeset
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="GetAnyInflSlotsInHigherPOSesForListing">
		<xsl:param name="pos" select="."/>
		<xsl:for-each select="$pos/descendant::MoInflAffixSlot">
			<affixslot>
				<id>
					<xsl:value-of select="@Id"/>
				</id>
				<name>
					<xsl:value-of select="Name"/>
				</name>
			</affixslot>
		</xsl:for-each>
		<xsl:for-each select="$POSes[SubPossibilities[@dst=$pos/@Id]]">
			<xsl:call-template name="GetAnyInflSlotsInHigherPOSesForListing">
				<xsl:with-param name="pos" select="key('POSID',@Id)"/>
			</xsl:call-template>
		</xsl:for-each>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		GetAnyNestedCategoriesForListing
		Output Id and Name for each subcategory in this or any nested PartOfSpeech
		Parameters: pos = current PartOfSpeech nodeset
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="GetAnyNestedCategoriesForListing">
		<xsl:param name="pos" select="."/>
		<xsl:for-each select="$pos/SubPossibilities">
			<xsl:for-each select="key('POSID',@dst)">
				<subcat>
					<id>
						<xsl:value-of select="@Id"/>
					</id>
					<name>
						<xsl:value-of select="Name"/>
					</name>
				</subcat>
			</xsl:for-each>
			<xsl:call-template name="GetAnyNestedCategoriesForListing">
				<xsl:with-param name="pos" select="key('POSID',@dst)"/>
			</xsl:call-template>
		</xsl:for-each>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		GetAnyNestedInflTemplatesForListing
		Output Id and Name for each inflectional affix template in this or any nested PartOfSpeech
		Parameters: pos = current PartOfSpeech nodeset
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="GetAnyNestedInflTemplatesForListing">
		<xsl:param name="pos" select="."/>
		<xsl:if test="$pos/descendant::MoInflAffixTemplate">
			<subcat>
				<id>
					<xsl:value-of select="$pos/@Id"/>
				</id>
				<name>
					<xsl:value-of select="$pos/Name"/>
				</name>
			</subcat>
		</xsl:if>
		<xsl:for-each select="$pos/SubPossibilities">
			<xsl:call-template name="GetAnyNestedInflTemplatesForListing">
				<xsl:with-param name="pos" select="key('POSID',@dst)"/>
			</xsl:call-template>
		</xsl:for-each>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
GetCitationForm
	display an entry's citation form as a cell in a table
		Parameters: LexEntry = nodeset of LexEntry
					iRowSpan = number of rows to span
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="GetCitationForm">
		<xsl:param name="LexEntry"/>
		<xsl:param name="iRowSpan" select="'0'"/>
		<td>
			<xsl:if test="$iRowSpan!=0">
				<xsl:attribute name="rowspan">
					<xsl:value-of select="$iRowSpan"/>
				</xsl:attribute>
			</xsl:if>
			<xsl:if test="$bVernRightToLeft='1'">
				<xsl:attribute name="type">tRtl</xsl:attribute>
			</xsl:if>
			<langData lang="lVern">
				<xsl:choose>
					<xsl:when test="string-length($LexEntry/CitationForm) &gt; 0">
						<xsl:value-of select="$LexEntry/CitationForm"/>
					</xsl:when>
					<xsl:when test="$LexEntry/LexemeForm/@dst">
						<xsl:value-of select="key('MoFormsID',$LexEntry/LexemeForm/@dst)/Form"/>
					</xsl:when>
					<xsl:when test="$LexEntry/AlternateForms">
						<xsl:value-of select="key('MoFormsID',$LexEntry/AlternateForms[@ord='1']/@dst)/Form"/>
					</xsl:when>
					<xsl:otherwise>
						<xsl:text>There is no form!</xsl:text>
					</xsl:otherwise>
				</xsl:choose>
			</langData>
			<xsl:text disable-output-escaping="yes">&amp;nbsp;</xsl:text>
		</td>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		GetDefinition
		display a sense's definition as a cell in a table
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="GetDefinition">
		<xsl:param name="Sense"/>
		<xsl:variable name="definition" select="$Sense/Definition"/>
		<td>
			<xsl:if test="$bGlossRightToLeft='1'">
				<xsl:attribute name="type">tRtl</xsl:attribute>
			</xsl:if>
			<xsl:choose>
				<xsl:when test="string-length(normalize-space($definition)) &gt; 0 and $definition!=$sEmptyContent">
					<!--                    <xsl:text>  '</xsl:text>-->
					<gloss lang="lGloss">
						<xsl:value-of select="$Sense/Definition"/>
					</gloss>
					<!--                    <xsl:text>'</xsl:text>-->
				</xsl:when>
				<xsl:otherwise>
					<xsl:text>The definition is missing.  Please add it.</xsl:text>
				</xsl:otherwise>
			</xsl:choose>
		</td>
	</xsl:template>
	<!--
	  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  GetGloss
	  display a sense's gloss as a cell in a table
	  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
   -->
	<xsl:template name="GetGloss">
		<xsl:param name="Sense"/>
		<td>
			<xsl:if test="$bGlossRightToLeft='1'">
				<xsl:attribute name="type">tRtl</xsl:attribute>
			</xsl:if>
			<xsl:text>  '</xsl:text>
			<gloss lang="lGloss">
				<xsl:if test="$Sense/Gloss!=$sEmptyContent">
					<xsl:value-of select="$Sense/Gloss"/>
				</xsl:if>
			</gloss>
			<xsl:text>'</xsl:text>
		</td>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
GetLexCountOfPOS
	obtain count of lexical item for a given PartOfSpeech
		Parameters: idPOS = id of the PartOfSpeech
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="GetLexCountOfPOS">
		<xsl:param name="idPOS"/>
		<xsl:value-of select="NumberOfLexEntries"/>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
GetMSAInfo
	display an msa's info
		Parameters: msa = msa to show info for
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="GetMSAInfo">
		<xsl:param name="msa"/>
		<xsl:choose>
			<xsl:when test="name($msa)='MoStemMsa'">
				<xsl:call-template name="GetPOSAbbr">
					<xsl:with-param name="posid" select="$msa/@PartOfSpeech"/>
				</xsl:call-template>
			</xsl:when>
			<xsl:when test="name($msa)='MoDerivAffMsa'">
				<xsl:call-template name="GetPOSAbbr">
					<xsl:with-param name="posid" select="$msa/@FromPartOfSpeech"/>
				</xsl:call-template>
				<xsl:text>&gt;</xsl:text>
				<xsl:call-template name="GetPOSAbbr">
					<xsl:with-param name="posid" select="$msa/@ToPartOfSpeech"/>
				</xsl:call-template>
			</xsl:when>
			<xsl:when test="name($msa)='MoInflAffMsa'">
				<xsl:call-template name="GetPOSAbbr">
					<xsl:with-param name="posid" select="$msa/@PartOfSpeech"/>
				</xsl:call-template>
				<xsl:text>:</xsl:text>
				<xsl:choose>
					<xsl:when test="count($msa/Slots)=0">
						<xsl:text>???</xsl:text>
					</xsl:when>
					<xsl:otherwise>
						<xsl:for-each select="$msa/Slots">
							<xsl:call-template name="GetSlotName">
								<xsl:with-param name="slotid" select="@dst"/>
							</xsl:call-template>
							<xsl:if test="position()!=last()">
								<xsl:text>/</xsl:text>
							</xsl:if>
						</xsl:for-each>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:when>
			<xsl:when test="name($msa)='MoUnclassifiedAffixMsa'">
				<xsl:text>Unclassified affix</xsl:text>
				<xsl:if test="$msa/@PartOfSpeech!='0'">
					<xsl:text> (</xsl:text>
					<xsl:call-template name="GetPOSAbbr">
						<xsl:with-param name="posid" select="$msa/@PartOfSpeech"/>
					</xsl:call-template>
					<xsl:text>)</xsl:text>
				</xsl:if>
			</xsl:when>
			<xsl:otherwise>
				<xsl:text>There is no function!</xsl:text>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
GetPOSAbbr
	display the abbreviation of a POS
		Parameters: posid = id of the POS
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="GetPOSAbbr">
		<xsl:param name="posid"/>
		<xsl:choose>
			<xsl:when test="$posid='0'">
				<xsl:value-of select="$prmSUnclassifiedTextToUse"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="key('POSID',$posid)/Abbreviation"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
GetSlotName
	display the slot name, including parentheses for optionality
		Parameters: slotid = id of the slot
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="GetSlotName">
		<xsl:param name="slotid"/>
		<xsl:variable name="Slot" select="key('SlotsID', $slotid)"/>
		<xsl:variable name="sSlotName">
			<xsl:value-of select="$Slot/Name"/>
		</xsl:variable>
		<xsl:variable name="bOptional">
			<xsl:value-of select="$Slot/@Optional"/>
		</xsl:variable>
		<xsl:if test="$bOptional='true'">
			<xsl:text>(</xsl:text>
		</xsl:if>
		<xsl:call-template name="Capitalize">
			<xsl:with-param name="sStr">
				<xsl:value-of select="$sSlotName"/>
			</xsl:with-param>
		</xsl:call-template>
		<xsl:if test="$bOptional='true'">
			<xsl:text>)</xsl:text>
		</xsl:if>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		HandleOutputtingListOfSubcatNames
		get and display list of subcategory names
		Parameters: pos = current PartOfSpeech
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="HandleOutputtingListOfSubcatNames">
		<xsl:param name="pos"/>
		<xsl:variable name="subcatItems">
			<xsl:call-template name="GetAnyNestedCategoriesForListing">
				<xsl:with-param name="pos" select="$pos"/>
			</xsl:call-template>
		</xsl:variable>
		<xsl:choose>
			<xsl:when test="function-available('exsl:node-set')">
				<xsl:for-each select="exsl:node-set($subcatItems)/subcat">
					<xsl:sort select="name"/>
					<xsl:call-template name="OutputSubcategoryNameAsList"/>
				</xsl:for-each>
			</xsl:when>
			<xsl:when test="function-available('saxon:node-set')">
				<xsl:for-each select="saxon:node-set($subcatItems)/subcat">
					<xsl:sort select="name"/>
					<xsl:call-template name="OutputSubcategoryNameAsList"/>
				</xsl:for-each>
			</xsl:when>
			<xsl:otherwise>
				<xsl:for-each select="msxslt:node-set($subcatItems)/subcat">
					<xsl:sort select="name"/>
					<xsl:call-template name="OutputSubcategoryNameAsList"/>
				</xsl:for-each>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
MorphsByCatAppendix
	display appendix listing morphemes by category
		Parameters: sLangName = language name
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="MorphsByCatAppendix">
		<xsl:param name="sLangName"/>
		<appendix id="aMorphsByCat">
			<secTitle>
				<xsl:value-of select="$sLangName"/>
				<xsl:text> morphemes by category</xsl:text>
			</secTitle>
			<xsl:call-template name="AppendixIntro">
				<xsl:with-param name="sType">category</xsl:with-param>
			</xsl:call-template>
			<ul>
				<xsl:for-each select="$POSes">
					<xsl:sort select="Name"/>
					<xsl:variable name="idPOS">
						<xsl:value-of select="@Id"/>
					</xsl:variable>
					<xsl:call-template name="ProcessPOSLex">
						<xsl:with-param name="pos" select="."/>
					</xsl:call-template>
				</xsl:for-each>
			</ul>
			<xsl:for-each select="$POSes">
				<xsl:sort select="Name"/>
				<xsl:variable name="Cat">
					<xsl:value-of select="@Id"/>
				</xsl:variable>
				<section2 type="tH2">
					<xsl:attribute name="id">
						<xsl:text>aMorphsByCat.</xsl:text>
						<xsl:value-of select="@Id"/>
					</xsl:attribute>
					<secTitle>
						<xsl:call-template name="Capitalize">
							<xsl:with-param name="sStr">
								<xsl:value-of select="Name"/>
							</xsl:with-param>
						</xsl:call-template>
					</secTitle>
					<xsl:variable name="iCatCount" select="count($LexEntries[key('MsaID', MorphoSyntaxAnalysis/@dst)/@PartOfSpeech=$Cat])"/>
					<xsl:if test="$iCatCount >0">
						<p>
							<xsl:call-template name="AppendixSubsectionBlurb">
								<xsl:with-param name="iCount" select="$iCatCount"/>
							</xsl:call-template>
						</p>
						<table type="tLeftOffset">
							<xsl:for-each select="$LexEntries[key('MsaID', MorphoSyntaxAnalysis/@dst)/@PartOfSpeech=$Cat]">
								<xsl:sort select="CitationForm"/>
								<xsl:call-template name="OutputLexEntryInTable">
									<xsl:with-param name="msa" select="MorphoSyntaxAnalysis[key('MsaID', @dst)/@PartOfSpeech=$Cat]"/>
								</xsl:call-template>
							</xsl:for-each>
						</table>
					</xsl:if>
				</section2>
			</xsl:for-each>
		</appendix>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
MorphsByMorphTypeAppendix
	display appendix listing morphemes by category
		Parameters: sLangName = language name
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="MorphsByMorphTypeAppendix">
		<xsl:param name="sLangName"/>
		<appendix id="aMorphsByType">
			<secTitle>
				<xsl:value-of select="$sLangName"/>
				<xsl:text> morphemes by type</xsl:text>
			</secTitle>
			<xsl:call-template name="AppendixIntro">
				<xsl:with-param name="sType">morphological type</xsl:with-param>
			</xsl:call-template>
			<!-- do we need this? -->
			<ul>
				<xsl:for-each select="$MoMorphTypes">
					<xsl:sort select="Name"/>
					<xsl:variable name="Type">
						<xsl:value-of select="@Id"/>
					</xsl:variable>
					<xsl:variable name="iCount">
						<xsl:value-of select="NumberOfLexEntries"/>
					</xsl:variable>
					<xsl:if test="$iCount!=0">
						<li>
							<genericRef>
								<xsl:attribute name="gref">
									<xsl:text>aMorphsByType.</xsl:text>
									<xsl:value-of select="@Id"/>
								</xsl:attribute>
								<xsl:call-template name="Capitalize">
									<xsl:with-param name="sStr">
										<xsl:value-of select="Name"/>
									</xsl:with-param>
								</xsl:call-template>
							</genericRef>
							<xsl:text> (</xsl:text>
							<xsl:value-of select="$iCount"/>
							<xsl:text>).</xsl:text>
						</li>
					</xsl:if>
				</xsl:for-each>
			</ul>
			<xsl:for-each select="$MoMorphTypes">
				<xsl:sort select="Name"/>
				<xsl:variable name="Type">
					<xsl:value-of select="@Id"/>
				</xsl:variable>
				<xsl:variable name="iCount">
					<xsl:value-of select="NumberOfLexEntries"/>
				</xsl:variable>
				<xsl:if test="$iCount!=0">
					<section2 type="tH2">
						<xsl:attribute name="id">
							<xsl:text>aMorphsByType.</xsl:text>
							<xsl:value-of select="@Id"/>
						</xsl:attribute>
						<secTitle>
							<xsl:call-template name="Capitalize">
								<xsl:with-param name="sStr">
									<xsl:value-of select="Name"/>
								</xsl:with-param>
							</xsl:call-template>
						</secTitle>
						<xsl:variable name="allos" select="$MoAffixAllomorphs[@MorphType=$Type and @IsAbstract='0' or @MorphType=$Type and key('MorphTypeID',@MorphType)/@Guid=$sCircumfix] | $MoStemAllomorphs[@MorphType=$Type and @IsAbstract='0']"/>
						<xsl:if test="$allos">
							<p>
								<xsl:call-template name="AppendixSubsectionBlurb">
									<xsl:with-param name="iCount" select="count($allos)"/>
								</xsl:call-template>
							</p>
							<table type="tLeftOffset">
								<!-- find all LexEntries that have allomorphs with the $Type.  -->
								<xsl:choose>
									<xsl:when test="@Guid=$sCircumfix">
										<!-- special case for circumfixes -->
										<xsl:for-each select="$LexEntries[key('MoFormsID',LexemeForm/@dst)[@MorphType=$Type]]">
											<xsl:sort select="CitationForm"/>
											<xsl:call-template name="OutputLexEntryInTable"/>
										</xsl:for-each>
									</xsl:when>
									<xsl:when test="@Guid=$sPrefix or @Guid=$sInfix or @Guid=$sSuffix">
										<!-- special case for prefix, infix, and suffix when there are circumfixes -->
										<!-- Circumfix -->
										<xsl:for-each select="$LexEntries[key('MoFormsID',AlternateForms/@dst)[@MorphType=$Type and @IsAbstract='0']  and not(key('MorphTypeID',key('MoFormsID',LexemeForm/@dst)/@MorphType)/@Guid=$sCircumfix) or key('MoFormsID',LexemeForm/@dst)[@MorphType=$Type and @IsAbstract='0']]">
											<!--                                    <xsl:for-each select="$LexEntries[key('MoFormsID',AlternateForms/@dst)[@MorphType=$Type and @IsAbstract='0']  and not(key('MorphTypeID',LexemeForm/@dst)/@Guid=$sCircumfix) or key('MoFormsID',LexemeForm/@dst)[@MorphType=$Type and @IsAbstract='0'] and not(key('MorphTypeID',LexemeForm/@dst)/@Guid=$sCircumfix)]"> -->
											<xsl:sort select="CitationForm"/>
											<xsl:call-template name="OutputLexEntryInTable"/>
										</xsl:for-each>
									</xsl:when>
									<xsl:otherwise>
										<!-- normal case -->
										<xsl:for-each select="$LexEntries[key('MoFormsID',LexemeForm/@dst)[@MorphType=$Type and @IsAbstract='0'] or key('MoFormsID',AlternateForms/@dst)[@MorphType=$Type and @IsAbstract='0']]">
											<xsl:sort select="CitationForm"/>
											<xsl:call-template name="OutputLexEntryInTable"/>
										</xsl:for-each>
									</xsl:otherwise>
								</xsl:choose>
							</table>
						</xsl:if>
					</section2>
				</xsl:if>
			</xsl:for-each>
		</appendix>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputAdjacencyValue
	display information for an adjacnecy value
		Parameters: Fillers = none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputAdjacencyValue">
		<xsl:choose>
			<xsl:when test="@Adjacency='0'">
				<xsl:text>Anywhere around</xsl:text>
			</xsl:when>
			<!-- N.B. the left/right values here are the opposite of how we interpret it for the parser; but this is clearer for the user -->
			<xsl:when test="@Adjacency='1'">
				<xsl:text>Somewhere after</xsl:text>
			</xsl:when>
			<xsl:when test="@Adjacency='2'">
				<xsl:text>Somewhere before</xsl:text>
			</xsl:when>
			<xsl:when test="@Adjacency='3'">
				<xsl:text>Adjacent after</xsl:text>
			</xsl:when>
			<xsl:when test="@Adjacency='4'">
				<xsl:text>Adjacent before</xsl:text>
			</xsl:when>
			<xsl:otherwise>
				<xsl:text>Undefined!</xsl:text>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputAllomorphAndGloss
	display allomorph form and gloss in vertical tabular format
		Parameters: LexEntry = LexEntry to use
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputAllomorphAndGloss">
		<xsl:param name="LexEntry"/>
		<xsl:param name="Allo"/>
		<table>
			<tr>
				<td>
					<xsl:if test="$bVernRightToLeft='1'">
						<xsl:attribute name="type">tRtl</xsl:attribute>
					</xsl:if>
					<langData lang="lVern">
						<xsl:value-of select="key('MoFormsID',$Allo)/Form"/>
					</langData>
				</td>
			</tr>
			<tr>
				<xsl:choose>
					<xsl:when test="$LexEntry">
						<xsl:call-template name="GetGloss">
							<xsl:with-param name="Sense" select="key('SensesID',$LexEntry/Sense/@dst)"/>
						</xsl:call-template>
					</xsl:when>
					<xsl:otherwise>
						<td>&#xa0;</td>
					</xsl:otherwise>
				</xsl:choose>
			</tr>
		</table>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputCitationAndGloss
	display citation form and gloss in vertical tabular format
		Parameters: LexEntry = LexEntry to use
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputCitationAndGloss">
		<xsl:param name="LexEntry"/>
		<table>
			<tr>
				<xsl:call-template name="GetCitationForm">
					<xsl:with-param name="LexEntry" select="$LexEntry"/>
				</xsl:call-template>
			</tr>
			<tr>
				<xsl:call-template name="GetGloss">
					<xsl:with-param name="Sense" select="key('SensesID',$LexEntry/Sense/@dst)"/>
				</xsl:call-template>
			</tr>
		</table>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputCitationAndGlossAndMSAInfo
	display citation form and gloss in vertical tabular format
		Parameters: LexEntry = LexEntry to use
							 msa = msa to use
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputCitationAndGlossAndMSAInfo">
		<xsl:param name="LexEntry"/>
		<xsl:param name="msa"/>
		<table>
			<tr>
				<xsl:call-template name="GetCitationForm">
					<xsl:with-param name="LexEntry" select="$LexEntry"/>
				</xsl:call-template>
			</tr>
			<tr>
				<xsl:call-template name="GetGloss">
					<xsl:with-param name="Sense" select="key('SensesMsa',$msa/@Id)"/>
				</xsl:call-template>
			</tr>
			<tr>
				<td>
					<xsl:call-template name="GetMSAInfo">
						<xsl:with-param name="msa" select="$msa"/>
					</xsl:call-template>
				</td>
			</tr>
		</table>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputCliticFromPOSes
	display from POSes info for a clitic
		Parameters: LexEntry = LexEntry to use
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputCliticFromPOSes">
		<xsl:param name="lexEntry"/>
		<xsl:choose>
			<xsl:when test="FromPartsOfSpeech/FromPOS">
				<table cellpadding="0" cellspacing="0">
					<xsl:for-each select="FromPartsOfSpeech/FromPOS">
						<tr>
							<td>
								<xsl:call-template name="OutputPOSAsNameWithRef">
									<xsl:with-param name="sPOSId" select="@dst"/>
									<xsl:with-param name="sUnknown" select="'Any category'"/>
								</xsl:call-template>
							</td>
						</tr>
					</xsl:for-each>
				</table>
			</xsl:when>
			<xsl:when test="key('MorphTypeID',$lexEntry/LexemeForm/@MorphType)/@Guid=$sClitic">
				<xsl:text disable-output-escaping="yes">not applicable</xsl:text>
			</xsl:when>
			<xsl:otherwise>
				<xsl:text disable-output-escaping="yes">Any category</xsl:text>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputCliticWithPOS
	display a clitic that has at least one POS
		Parameters: LexEntry = LexEntry to use
							 entryMsas = set of MSAs in entry
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputCliticWithPOS">
		<xsl:param name="lexEntry"/>
		<xsl:param name="entryMsas"/>
		<xsl:for-each select="$entryMsas">
			<tr>
				<xsl:choose>
					<xsl:when test="$bVernRightToLeft='1'">
						<td>
							<xsl:call-template name="OutputCliticFromPOSes">
								<xsl:with-param name="lexEntry" select="$lexEntry"/>
							</xsl:call-template>
						</td>
						<td>
							<xsl:call-template name="OutputPOSAsNameWithRef">
								<xsl:with-param name="sPOSId" select="@PartOfSpeech"/>
								<xsl:with-param name="sUnknown" select="'Any category'"/>
							</xsl:call-template>
						</td>
						<xsl:call-template name="GetDefinition">
							<xsl:with-param name="Sense" select="key('SensesMsa',@Id)"/>
						</xsl:call-template>
						<xsl:call-template name="GetGloss">
							<xsl:with-param name="Sense" select="key('SensesMsa',@Id)"/>
						</xsl:call-template>
						<xsl:if test="position()=1">
							<xsl:call-template name="GetCitationForm">
								<xsl:with-param name="LexEntry" select="$lexEntry"/>
								<xsl:with-param name="iRowSpan" select="count($entryMsas)"/>
							</xsl:call-template>
						</xsl:if>
					</xsl:when>
					<xsl:otherwise>
						<xsl:if test="position()=1">
							<xsl:call-template name="GetCitationForm">
								<xsl:with-param name="LexEntry" select="$lexEntry"/>
								<xsl:with-param name="iRowSpan" select="count($entryMsas)"/>
							</xsl:call-template>
						</xsl:if>
						<xsl:call-template name="GetGloss">
							<xsl:with-param name="Sense" select="key('SensesMsa',@Id)"/>
						</xsl:call-template>
						<xsl:call-template name="GetDefinition">
							<xsl:with-param name="Sense" select="key('SensesMsa',@Id)"/>
						</xsl:call-template>
						<td>
							<xsl:call-template name="OutputPOSAsNameWithRef">
								<xsl:with-param name="sPOSId" select="@PartOfSpeech"/>
								<xsl:with-param name="sUnknown" select="'Not sure'"/>
							</xsl:call-template>
						</td>
						<td>
							<xsl:call-template name="OutputCliticFromPOSes">
								<xsl:with-param name="lexEntry" select="$lexEntry"/>
							</xsl:call-template>
						</td>
					</xsl:otherwise>
				</xsl:choose>
			</tr>
		</xsl:for-each>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputFirstAllomorphAdhocMember
	display information for the first member of an allomorph adhoc sequence
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputFirstAllomorphAdhocMember">
		<xsl:variable name="dst" select="FirstAllomorph/@dst"/>
		<xsl:variable name="LexEntry" select="$LexEntries[AlternateForms/@dst=$dst or LexemeForm/@dst=$dst]"/>
		<xsl:call-template name="OutputAllomorphAndGloss">
			<xsl:with-param name="LexEntry" select="$LexEntry"/>
			<xsl:with-param name="Allo" select="$dst"/>
		</xsl:call-template>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputFirstMorphemeAdhocMember
	display information for the first member of a morpheme adhoc sequence
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputFirstMorphemeAdhocMember">
		<xsl:variable name="dst" select="FirstMorpheme/@dst"/>
		<xsl:variable name="LexEntry" select="$LexEntries[MorphoSyntaxAnalysis/@dst=$dst]"/>
		<xsl:call-template name="OutputCitationAndGlossAndMSAInfo">
			<xsl:with-param name="LexEntry" select="$LexEntry"/>
			<xsl:with-param name="msa" select="key('MsaID',$dst)"/>
		</xsl:call-template>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputHeadedCompoundRuleInfo
	display information for a headed compound rule
		Parameters: sHead = 'left' or 'right'
							  pos = id of the part of speech of the head
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputHeadedCompoundRuleInfo">
		<xsl:param name="sHead"/>
		<xsl:param name="pos"/>
		<xsl:param name="features"/>
		<xsl:text>This is a headed  construction in which the </xsl:text>
		<xsl:value-of select="$sHead"/>
		<xsl:text>-hand stem is the head. </xsl:text>
		<xsl:choose>
			<xsl:when test="$pos!=0">
				<xsl:variable name="overridingMsa" select="key('MsaID',OverridingMsa/@dst)"/>
				<xsl:variable name="overridingPOS" select="$overridingMsa/@PartOfSpeech"/>
				<xsl:choose>
					<xsl:when test="$overridingMsa and $overridingPOS!=0">
						<xsl:text>The head, however, is overriden for category and the resulting category is  </xsl:text>
						<genericRef>
							<xsl:attribute name="gref">
								<xsl:text>sCat.</xsl:text>
								<xsl:value-of select="$overridingPOS"/>
							</xsl:attribute>
							<xsl:value-of select="key('POSID',$overridingPOS)/Abbreviation"/>
						</genericRef>
						<xsl:variable name="idToMsaInflClass" select="$overridingMsa/@InflectionClass"/>
						<xsl:if test="$idToMsaInflClass!=0">
							<xsl:text> and has an inflection class of </xsl:text>
							<xsl:value-of select="$MoInflClasses[@Id=$idToMsaInflClass]/Name"/>
						</xsl:if>

						<xsl:text>.</xsl:text>
						<xsl:call-template name="OutputExceptionFeaturesOfCompoundRule">
							<xsl:with-param name="features" select="$features"/>
						</xsl:call-template>
					</xsl:when>
					<xsl:otherwise>
						<xsl:text>Thus the resulting compound has the category of </xsl:text>
						<genericRef>
							<xsl:attribute name="gref">
								<xsl:text>sCat.</xsl:text>
								<xsl:value-of select="$pos"/>
							</xsl:attribute>
							<xsl:value-of select="key('POSID',$pos)/Abbreviation"/>
						</genericRef>
						<xsl:text>.</xsl:text>
						<xsl:call-template name="OutputExceptionFeaturesOfCompoundRule">
							<xsl:with-param name="features" select="$features"/>
						</xsl:call-template>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:when>
			<xsl:otherwise>
				<xsl:text>The category of the head, however, has not been specified!  You should specify the category.</xsl:text>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputInflectionalAffixTemplate
	display information for an inflectional affix template
		Parameters: iSubcatLevel = subcat nesting level
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputInflectionalAffixTemplate">
		<xsl:param name="iSubcatLevel">0</xsl:param>
		<xsl:choose>
			<xsl:when test="$iSubcatLevel &lt; 3">
				<secTitle>
					<xsl:call-template name="OutputInflectionalTemplateName"/>
				</secTitle>
			</xsl:when>
			<xsl:otherwise>
				<p>
					<xsl:call-template name="OutputInflectionalTemplateName"/>
				</p>
			</xsl:otherwise>
		</xsl:choose>
		<p>
			<xsl:choose>
				<xsl:when test="string-length(Description) &gt; 0 and Description!=$sEmptyContent">
					<xsl:value-of select="Description"/>
				</xsl:when>
				<xsl:otherwise>
					<xsl:text>This inflectional template for </xsl:text>
					<xsl:value-of select="../../Name"/>
					<xsl:choose>
						<xsl:when test="count(PrefixSlots | SuffixSlots) &gt; 0">
							<xsl:text> has the following slot</xsl:text>
							<xsl:if test="count(PrefixSlots | SuffixSlots) &gt; 1">
								<xsl:text>s </xsl:text>
							</xsl:if>
							<xsl:if test="PrefixSlots">
								<xsl:text> before </xsl:text>
							</xsl:if>
							<xsl:if test="PrefixSlots and SuffixSlots">
								<xsl:text>and </xsl:text>
							</xsl:if>
							<xsl:if test="SuffixSlots">
								<xsl:text> after </xsl:text>
							</xsl:if>
							<xsl:text> the stem.</xsl:text>
						</xsl:when>
						<xsl:otherwise> does not have any slots!  You should either add some or remove the template.</xsl:otherwise>
					</xsl:choose>
				</xsl:otherwise>
			</xsl:choose>
			<xsl:if test="../../PartOfSpeech">
				<xsl:call-template name="OutputListOfSubcategories">
					<xsl:with-param name="sText1">
						<xsl:text>  This template is valid for not only the </xsl:text>
					</xsl:with-param>
					<xsl:with-param name="pos" select="../.."/>
				</xsl:call-template>
			</xsl:if>
			<xsl:if test="@Final='0'">
				<xsl:text>  This template is a non-final template.  That is, when it applies, it does not yet make a well-formed word.  It requires a derivational affix to change its category and then the resulting category may have an inflectional template to complete it.</xsl:text>
			</xsl:if>
		</p>
		<table border="1" type="tLeftOffset">
			<xsl:choose>
				<xsl:when test="$bVernRightToLeft='1'">
					<tr>
						<xsl:call-template name="ProcessSlotNames">
							<xsl:with-param name="Slots" select="SuffixSlots"/>
						</xsl:call-template>
						<th>Stem</th>
						<xsl:call-template name="ProcessSlotNames">
							<xsl:with-param name="Slots" select="PrefixSlots"/>
						</xsl:call-template>
					</tr>
					<tr>
						<xsl:call-template name="ProcessSlotFillers">
							<xsl:with-param name="Slots" select="SuffixSlots"/>
						</xsl:call-template>
						<td>
							<xsl:text disable-output-escaping="yes">&amp;nbsp;</xsl:text>
						</td>
						<xsl:call-template name="ProcessSlotFillers">
							<xsl:with-param name="Slots" select="PrefixSlots"/>
						</xsl:call-template>
					</tr>
				</xsl:when>
				<xsl:otherwise>
					<tr>
						<xsl:call-template name="ProcessSlotNames">
							<xsl:with-param name="Slots" select="PrefixSlots"/>
						</xsl:call-template>
						<th>Stem</th>
						<xsl:call-template name="ProcessSlotNames">
							<xsl:with-param name="Slots" select="SuffixSlots"/>
						</xsl:call-template>
					</tr>
					<tr>
						<xsl:call-template name="ProcessSlotFillers">
							<xsl:with-param name="Slots" select="PrefixSlots"/>
						</xsl:call-template>
						<td>
							<xsl:text disable-output-escaping="yes">&amp;nbsp;</xsl:text>
						</td>
						<xsl:call-template name="ProcessSlotFillers">
							<xsl:with-param name="Slots" select="SuffixSlots"/>
						</xsl:call-template>
					</tr>
				</xsl:otherwise>
			</xsl:choose>
		</table>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		OutputInflectionalSlotAsList
		display information for the name of an inflectional slot in list format
		Parameters: none
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="OutputInflectionalSlotAsList">
		<genericRef>
			<xsl:attribute name="gref">
				<xsl:text>sInflSlotFillers.</xsl:text>
				<xsl:value-of select="id"/>
			</xsl:attribute>
			<xsl:value-of select="name"/>
		</genericRef>
		<xsl:call-template name="OutputListPunctuation"/>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputInflectionalTemplateName
	display information for the name of an inflectional template
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputInflectionalTemplateName">
		<xsl:choose>
			<xsl:when test="string-length(Name) &gt; 1 and Name!=$sEmptyContent">
				<xsl:call-template name="Capitalize">
					<xsl:with-param name="sStr">
						<xsl:value-of select="Name"/>
					</xsl:with-param>
				</xsl:call-template>
			</xsl:when>
			<xsl:otherwise>
				<xsl:text>(This template has not been given a name yet.)</xsl:text>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		OutputInflectionalTemplateNameAsList
		display the name of an inflectional template in a list format
		Parameters: none
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="OutputInflectionalTemplateNameAsList">
		<genericRef>
			<xsl:attribute name="gref">
				<xsl:text>sInflTemplate.</xsl:text>
				<xsl:value-of select="id"/>
			</xsl:attribute>
			<xsl:value-of select="name"/>
		</genericRef>
		<xsl:call-template name="OutputListPunctuation"/>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputLexEntryInTable
	display information for a lex entry
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputLexEntryInTable">
		<xsl:param name="msa"/>
		<xsl:if test="$prmIMaxMorphsInAppendices = '-1' or position() &lt;= $prmIMaxMorphsInAppendices">
			<tr>
				<xsl:choose>
					<xsl:when test="$bVernRightToLeft='1'">
						<xsl:choose>
							<xsl:when test="$msa">
								<xsl:call-template name="GetGloss">
									<xsl:with-param name="Sense" select="key('SensesMsa',$msa/@dst)"/>
								</xsl:call-template>
							</xsl:when>
							<xsl:otherwise>
								<xsl:call-template name="GetGloss">
									<xsl:with-param name="Sense" select="key('SensesID',./Sense/@dst)"/>
								</xsl:call-template>
							</xsl:otherwise>
						</xsl:choose>
						<xsl:call-template name="GetCitationForm">
							<xsl:with-param name="LexEntry" select="."/>
						</xsl:call-template>
					</xsl:when>
					<xsl:otherwise>
						<xsl:call-template name="GetCitationForm">
							<xsl:with-param name="LexEntry" select="."/>
						</xsl:call-template>
						<xsl:choose>
							<xsl:when test="$msa">
								<xsl:call-template name="GetGloss">
									<xsl:with-param name="Sense" select="key('SensesMsa',$msa/@dst)"/>
								</xsl:call-template>
							</xsl:when>
							<xsl:otherwise>
								<xsl:call-template name="GetGloss">
									<xsl:with-param name="Sense" select="key('SensesID',./Sense/@dst)"/>
								</xsl:call-template>
							</xsl:otherwise>
						</xsl:choose>
					</xsl:otherwise>
				</xsl:choose>
			</tr>
		</xsl:if>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputListPunctuation
	display information at end of a list for each member of that list
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputListPunctuation">
		<xsl:param name="sConjunction" select="' and '"/>
		<xsl:param name="sFinalPunctuation" select="'.'"/>
		<xsl:choose>
			<xsl:when test="position()='1' and position()=last()-1">
				<xsl:value-of select="$sConjunction"/>
			</xsl:when>
			<xsl:when test="position()=last()-1">
				<xsl:text>,</xsl:text>
				<xsl:value-of select="$sConjunction"/>
			</xsl:when>
			<xsl:when test="position()=last()">
				<xsl:value-of select="$sFinalPunctuation"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:text>, </xsl:text>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputProductivityRestrictionLabel
	display label used for Productivity restriction
		Parameters: sFinal - ending string (for plural and punctuation)
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputProductivityRestrictionLabel">
		<xsl:param name="sFinal"/>
		<xsl:text disable-output-escaping="yes">exception &amp;ldquo;feature</xsl:text>
		<xsl:value-of select="$sFinal"/>
		<xsl:text disable-output-escaping="yes">&amp;rdquo;</xsl:text>
	</xsl:template>
	<xsl:template name="OutputRegularPhonologicalRule">
		<table>
			<tr>
				<td valign="middle">
					<xsl:variable name="structuralDescription" select="StrucDesc/*"/>
					<xsl:choose>
						<xsl:when test="$structuralDescription">
							<xsl:for-each select="$structuralDescription">
								<xsl:call-template name="OutputPhonologicalRuleItem">
									<xsl:with-param name="item" select="."/>
									<xsl:with-param name="bCanUseNullSet" select="'Y'"/>
								</xsl:call-template>
							</xsl:for-each>
						</xsl:when>
						<xsl:otherwise>
							<object type="tPhonRuleNullSet"/>
						</xsl:otherwise>
					</xsl:choose>
				</td>
				<td valign="middle">---&gt;</td>
				<td valign="middle">
					<xsl:variable name="structuralChange" select="RightHandSides/PhSegRuleRHS/StrucChange/*"/>
					<xsl:choose>
						<xsl:when test="$structuralChange">
							<xsl:for-each select="$structuralChange">
								<xsl:call-template name="OutputPhonologicalRuleItem">
									<xsl:with-param name="item" select="."/>
									<xsl:with-param name="bCanUseNullSet" select="'Y'"/>
								</xsl:call-template>
							</xsl:for-each>
						</xsl:when>
						<xsl:otherwise>
							<object type="tPhonRuleNullSet"/>
						</xsl:otherwise>
					</xsl:choose>
				</td>
				<td valign="middle">/</td>
				<td valign="middle">
					<xsl:for-each select="RightHandSides/PhSegRuleRHS/LeftContext/*">
						<xsl:call-template name="OutputPhonologicalRuleItem">
							<xsl:with-param name="item" select="."/>
						</xsl:call-template>
					</xsl:for-each>
				</td>
				<td valign="middle">___</td>
				<td valign="middle">
					<xsl:for-each select="RightHandSides/PhSegRuleRHS/RightContext/*">
						<xsl:call-template name="OutputPhonologicalRuleItem">
							<xsl:with-param name="item" select="."/>
						</xsl:call-template>
					</xsl:for-each>
				</td>
			</tr>
		</table>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputRestAllomorphAdhocMembers
	display information for the non-first members of an allomorph adhoc sequence
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputRestAllomorphAdhocMembers">
		<xsl:for-each select="RestOfAllos">
			<xsl:variable name="LexEntry" select="key('LexAllos',@dst)/.."/>
			<xsl:call-template name="OutputAllomorphAndGloss">
				<xsl:with-param name="LexEntry" select="$LexEntry"/>
				<xsl:with-param name="Allo" select="@dst"/>
			</xsl:call-template>
		</xsl:for-each>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputRestMorphemeAdhocMembers
	display information for the non-first members of a morpheme adhoc sequence
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputRestMorphemeAdhocMembers">
		<xsl:for-each select="RestOfMorphs">
			<xsl:variable name="dst" select="@dst"/>
			<xsl:variable name="LexEntry" select="$LexEntries[MorphoSyntaxAnalysis/@dst=$dst]"/>
			<xsl:call-template name="OutputCitationAndGlossAndMSAInfo">
				<xsl:with-param name="LexEntry" select="$LexEntry"/>
				<xsl:with-param name="msa" select="key('MsaID',$dst)"/>
			</xsl:call-template>
		</xsl:for-each>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputExceptionFeatures
	display information for exception features
		Parameters: features = exception features to show
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputExceptionFeatures">
		<xsl:param name="features"/>
		<td>
			<xsl:choose>
				<xsl:when test="$features">
					<table cellpadding="0" cellspacing="0">
						<xsl:for-each select="$features">
							<tr>
								<td>
									<xsl:value-of select="key('ExceptionFeaturesID', @dst)/Name"/>
								</td>
							</tr>
						</xsl:for-each>
					</table>
				</xsl:when>
				<xsl:otherwise>
					<xsl:text disable-output-escaping="yes">&amp;nbsp;</xsl:text>
				</xsl:otherwise>
			</xsl:choose>
		</td>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputExceptionFeaturesOfCompoundRule
	display information for exception features
		Parameters: features = exception features to show
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputExceptionFeaturesOfCompoundRule">
		<xsl:param name="features"/>
		<xsl:if test="$features">
			<xsl:text>  It also has the following exception feature</xsl:text>
			<xsl:variable name="iCount" select="count($features/FsClosedValue | $features/FsNegatedValue)"/>
			<xsl:choose>
				<xsl:when test="$iCount=1">:</xsl:when>
				<xsl:otherwise>s:</xsl:otherwise>
			</xsl:choose>
			<xsl:for-each select="$features/FsClosedValue | $features/FsNegatedValue">
				<xsl:text disable-output-escaping="yes">&amp;nbsp;</xsl:text>
				<xsl:value-of select="key('BearableFeaturesID', @Feature)/Name"/>
				<xsl:choose>
					<xsl:when test="position()=last()">.</xsl:when>
					<xsl:otherwise>,</xsl:otherwise>
				</xsl:choose>
			</xsl:for-each>
		</xsl:if>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputFeatureStructure
	display information for morphosyntactic features
		Parameters: fs = feature structure to show
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputFeatureStructure">
		<xsl:param name="fs"/>
		<xsl:if test="$fs">
			<xsl:text>[</xsl:text>
			<xsl:if test="$fs/FsClosedValue">
				<xsl:for-each select="$fs/FsClosedValue">
					<xsl:if test="position() !=1">
						<xsl:text>; </xsl:text>
					</xsl:if>
					<xsl:value-of select="normalize-space(key('FsClosedFeaturesID', @Feature)/Name)"/>
					<xsl:text>:</xsl:text>
					<xsl:value-of select="normalize-space(key('FsSymFeatValsID', @Value)/Name)"/>
				</xsl:for-each>
			</xsl:if>
			<xsl:if test="$fs/FsComplexValue">
				<xsl:if test="$fs/FsClosedValue">
					<br/>
					<xsl:text disable-output-escaping="yes">&amp;nbsp;</xsl:text>
				</xsl:if>
				<xsl:for-each select="$fs/FsComplexValue">
					<xsl:value-of select="normalize-space(key('FsComplexFeaturesID', @Feature)/Name)"/>
					<xsl:text>:</xsl:text>
					<xsl:call-template name="OutputFeatureStructure">
						<xsl:with-param name="fs" select="FsFeatStruc"/>
					</xsl:call-template>
					<xsl:if test="position()!=last()">
						<br/>
						<xsl:text disable-output-escaping="yes">&amp;nbsp;</xsl:text>
					</xsl:if>
				</xsl:for-each>
			</xsl:if>
			<xsl:text>]</xsl:text>
		</xsl:if>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputInflAffixLexEntry
	display lex entry info for an inflectional affix msa
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputInflAffixLexEntry">
		<xsl:param name="LexEntry"/>
		<xsl:param name="msa"/>
		<xsl:param name="bShowDefinition" select="'N'"/>
		<xsl:variable name="sense" select="key('SensesMsa', $msa)"/>
		<xsl:choose>
			<xsl:when test="$bVernRightToLeft='1'">
				<xsl:choose>
					<xsl:when test="$sense">
						<xsl:if test="$bShowDefinition='Y'">
							<xsl:call-template name="GetDefinition">
								<xsl:with-param name="Sense" select="$sense"/>
							</xsl:call-template>
						</xsl:if>
						<xsl:call-template name="GetGloss">
							<xsl:with-param name="Sense" select="$sense"/>
						</xsl:call-template>
					</xsl:when>
					<xsl:otherwise>
						<xsl:if test="$bShowDefinition='Y'">
							<xsl:call-template name="GetDefinition">
								<xsl:with-param name="Sense" select="key('SensesID',$LexEntry/Sense/@dst)"/>
							</xsl:call-template>
						</xsl:if>
						<xsl:call-template name="GetGloss">
							<xsl:with-param name="Sense" select="key('SensesID',$LexEntry/Sense/@dst)"/>
						</xsl:call-template>
					</xsl:otherwise>
				</xsl:choose>
				<xsl:call-template name="GetCitationForm">
					<xsl:with-param name="LexEntry" select="$LexEntry"/>
				</xsl:call-template>
			</xsl:when>
			<xsl:otherwise>
				<xsl:call-template name="GetCitationForm">
					<xsl:with-param name="LexEntry" select="$LexEntry"/>
				</xsl:call-template>
				<xsl:choose>
					<xsl:when test="$sense">
						<xsl:call-template name="GetGloss">
							<xsl:with-param name="Sense" select="$sense"/>
						</xsl:call-template>
						<xsl:if test="$bShowDefinition='Y'">
							<xsl:call-template name="GetDefinition">
								<xsl:with-param name="Sense" select="$sense"/>
							</xsl:call-template>
						</xsl:if>
					</xsl:when>
					<xsl:otherwise>
						<xsl:call-template name="GetGloss">
							<xsl:with-param name="Sense" select="key('SensesID',$LexEntry/Sense/@dst)"/>
						</xsl:call-template>
						<xsl:if test="$bShowDefinition='Y'">
							<xsl:call-template name="GetDefinition">
								<xsl:with-param name="Sense" select="key('SensesID',$LexEntry/Sense/@dst)"/>
							</xsl:call-template>
						</xsl:if>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputInflectionClass
	display information for an inflection class in a table
		Parameters: class = id of inflection class to show
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputInflectionClass">
		<xsl:param name="class"/>
		<td>
			<xsl:choose>
				<xsl:when test="$class != 0">
					<xsl:value-of select="key('InflectionClassesID',$class)/Name"/>
				</xsl:when>
				<xsl:otherwise>
					<xsl:text disable-output-escaping="yes">&amp;nbsp;</xsl:text>
				</xsl:otherwise>
			</xsl:choose>
		</td>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		OutputInflectionClassAbbreviation
		display information for an inflection class in a table
		Parameters: class = id of inflection class to show
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="OutputInflectionClassAbbreviation">
		<xsl:param name="class"/>
		<xsl:choose>
			<xsl:when test="$class != 0">
				<genericRef gref="InflectionClass.{$class}">
					<xsl:value-of select="key('InflectionClassesID',$class)/Abbreviation"/>
				</genericRef>
			</xsl:when>
			<xsl:otherwise>
				<xsl:text disable-output-escaping="yes">&amp;nbsp;</xsl:text>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputListOfSubcategories
	display list of subcategories
		Parameters: pos = PartOfSpeech to use
							 sText = text blurb
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputListOfSubcategories">
		<xsl:param name="pos" select="."/>
		<xsl:param name="sText1"/>
		<xsl:param name="sText2" select="' category, but also '"/>
		<xsl:param name="sText3plural" select="'all of its subcategories: '"/>
		<xsl:param name="sText3singular" select="'its subcategory: '"/>
		<xsl:variable name="subcatItems">
			<xsl:call-template name="GetAnyNestedCategoriesForListing">
				<xsl:with-param name="pos" select="$pos"/>
			</xsl:call-template>
		</xsl:variable>
		<xsl:variable name="iSubcatCount">
			<xsl:choose>
				<xsl:when test="function-available('exsl:node-set')">
					<xsl:value-of select="count(exsl:node-set($subcatItems)/subcat)"/>
				</xsl:when>
				<xsl:when test="function-available('saxon:node-set')">
					<xsl:value-of select="count(saxon:node-set($subcatItems)/subcat)"/>
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="count(msxslt:node-set($subcatItems)/subcat)"/>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<xsl:value-of select="$sText1"/>
		<genericRef>
			<xsl:attribute name="gref">
				<xsl:text>sCat.</xsl:text>
				<xsl:value-of select="$pos/@Id"/>
			</xsl:attribute>
			<xsl:value-of select="$pos/Name"/>
		</genericRef>
		<xsl:value-of select="$sText2"/>
		<xsl:choose>
			<xsl:when test="$iSubcatCount > 1">
				<xsl:value-of select="$sText3plural"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="$sText3singular"/>
			</xsl:otherwise>
		</xsl:choose>
		<xsl:call-template name="HandleOutputtingListOfSubcatNames">
			<xsl:with-param name="pos" select="$pos"/>
		</xsl:call-template>
	</xsl:template>
	<!--
	  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  OutputPhonologicalFeatureConstraint
	  display information for phonological features
	  Parameters: fs = feature structure to show
	  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
   -->
	<xsl:template name="OutputPhonologicalFeatureConstraint">
		<xsl:param name="feature"/>
		<xsl:param name="ord"/>
		<xsl:param name="bIsMinus" select="'N'"/>
		<xsl:if test="$feature">
			<xsl:call-template name="OutputPhonologicalFeatureValuePair">
				<xsl:with-param name="iPosition" select="position()"/>
				<xsl:with-param name="sValue">
					<xsl:if test="$bIsMinus='Y'">
						<xsl:text>-</xsl:text>
					</xsl:if>
					<object type="tAlphaVariable">
						<xsl:choose>
							<xsl:when test="@ord &gt;= 0 and @ord &lt; 24">
								<xsl:value-of select="substring('αβγδεζηθικλμνξοπρστυφχψω', $ord + 1, 1)"/>
							</xsl:when>
							<xsl:otherwise>XXX</xsl:otherwise>
						</xsl:choose>
					</object>
				</xsl:with-param>
				<xsl:with-param name="sFeatureName" select="$feature/Name"/>
				<xsl:with-param name="sFeatureId" select="$feature/@Id"/>
			</xsl:call-template>
		</xsl:if>
	</xsl:template>
	<!--
	  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  OutputPhonologicalFeatureStructure
	  display information for phonological features
	  Parameters: fs = feature structure to show
	  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
   -->
	<xsl:template name="OutputPhonologicalFeatureStructure">
		<xsl:param name="fs"/>
		<xsl:if test="$fs">
			<table cellspacing="0pt" cellpadding="1pt">
				<xsl:for-each select="$fs/FsClosedValue">
					<xsl:sort select="key('FsClosedFeaturesID', @Feature)/Name"/>
					<tr>
						<xsl:call-template name="OutputPhonologicalFeatureValuePair">
							<xsl:with-param name="iPosition" select="position()"/>
							<xsl:with-param name="sValue" select="key('FsSymFeatValsID', @Value)/Abbreviation"/>
							<xsl:with-param name="sFeatureName" select="key('FsClosedFeaturesID', @Feature)/Name"/>
							<xsl:with-param name="sFeatureId" select="@Feature"/>
						</xsl:call-template>
					</tr>
				</xsl:for-each>
			</table>
		</xsl:if>
	</xsl:template>
	<xsl:template name="OutputPhonologicalFeatureValuePair">
		<xsl:param name="iPosition"/>
		<xsl:param name="sValue"/>
		<xsl:param name="sFeatureName"/>
		<xsl:param name="sFeatureId"/>
		<td valign="middle" type="tFSCell">
			<xsl:choose>
				<xsl:when test="position()=1 and position()=last()">
					<object type="tFSBrackets">[</object>
				</xsl:when>
				<xsl:when test="position()=1">
					<object type="tFSBrackets">&#x23a1;</object>
				</xsl:when>
				<xsl:when test="position()=last()">
					<object type="tFSBrackets">&#x23a3;</object>
				</xsl:when>
				<xsl:otherwise>
					<object type="tFSBrackets">&#x23a2;</object>
				</xsl:otherwise>
			</xsl:choose>
		</td>
		<td valign="middle" type="tFSCell" align="right">
			<xsl:value-of select="$sValue"/>
		</td>
		<td valign="middle" type="tFSCell">
			<xsl:text>:</xsl:text>
		</td>
		<td valign="middle" type="tFSCell">
			<genericRef gref="sFeature.{$sFeatureId}">
				<xsl:value-of select="$sFeatureName"/>
			</genericRef>
		</td>
		<td valign="middle" type="tFSCell">
			<xsl:choose>
				<xsl:when test="position()=1 and position()=last()">
					<object type="tFSBrackets">]</object>
				</xsl:when>
				<xsl:when test="position()=1">
					<object type="tFSBrackets">&#x23a4;</object>
				</xsl:when>
				<xsl:when test="position()=last()">
					<object type="tFSBrackets">&#x23a6;</object>
				</xsl:when>
				<xsl:otherwise>
					<object type="tFSBrackets">&#x23a5;</object>
				</xsl:otherwise>
			</xsl:choose>
		</td>
	</xsl:template>
	<!--
	  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  OutputPhonologicalRuleItem
	  display information for phonological rule item (structural description, structural change, or environment)
	  Parameters: item = item to show
	  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
   -->
	<xsl:template name="OutputPhonologicalRuleItem">
		<xsl:param name="item"/>
		<xsl:param name="bCanUseNullSet" select="'N'"/>
		<xsl:choose>
			<xsl:when test="not($item)">
				<xsl:choose>
					<xsl:when test="$bCanUseNullSet='Y'">
						<object type="tPhonRuleNullSet"/>
					</xsl:when>
					<xsl:otherwise>
						<!-- nothing to show; leave it blank (but use a non-breaking space so it looks OK if there are borders -->
						<xsl:text>&#xa0;</xsl:text>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:when>
			<xsl:otherwise>
				<xsl:apply-templates select="$item"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputPotentiallyBlankItemInTable
	display item in a table
		Parameters: item = item to show
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputPotentiallyBlankItemInTable">
		<xsl:param name="item"/>
		<xsl:choose>
			<xsl:when test="string-length(normalize-space($item)) &gt; 0 and $item!=$sEmptyContent">
				<xsl:value-of select="$item"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:text disable-output-escaping="yes">&amp;nbsp;</xsl:text>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputPOSAsNameWithRef
	display name of POS if valid and mark it as a reference
		Parameters: sPOSId = id of POS to show
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputPOSAsNameWithRef">
		<xsl:param name="sPOSId"/>
		<xsl:param name="sUnknown" select="'Unknown category'"/>
		<xsl:choose>
			<xsl:when test="$sPOSId='0' or not($sPOSId)">
				<xsl:value-of select="$sUnknown"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:for-each select="key('POSID',$sPOSId)">
					<genericRef>
						<xsl:attribute name="gref">
							<xsl:text>sCat.</xsl:text>
							<xsl:value-of select="$sPOSId"/>
						</xsl:attribute>
						<xsl:value-of select="Name"/>
					</genericRef>
				</xsl:for-each>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputSlotsAndFillersForPOS
	display information for slots and their fillers of a POS
		Parameters: pos = the POS
						   iSubcatLevel = subcat nesting level
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputSlotsAndFillersForPOS">
		<xsl:param name="pos"/>
		<xsl:param name="iSubcatLevel">0</xsl:param>
		<xsl:variable name="sPOSId" select="$pos/@Id"/>
		<xsl:variable name="sPOSName">
			<xsl:value-of select="$pos/Name"/>
		</xsl:variable>
		<xsl:choose>
			<xsl:when test="$iSubcatLevel&lt;4">
				<secTitle>
					<xsl:value-of select="$sPOSName"/>
					<xsl:text> Slots and Fillers</xsl:text>
				</secTitle>
			</xsl:when>
			<xsl:otherwise>
				<p>
					<xsl:value-of select="$sPOSName"/>
					<xsl:text> Slots and Fillers</xsl:text>
				</p>
			</xsl:otherwise>
		</xsl:choose>
		<xsl:variable name="slots" select="AffixSlots/MoInflAffixSlot"/>
		<xsl:choose>
			<xsl:when test="not($slots)">
				<p>
					<xsl:text>The category </xsl:text>
					<xsl:value-of select="$sPOSName"/>
					<xsl:text> does not define any slots.</xsl:text>
					<xsl:if test="$pos/AffixTemplates/MoInflAffixTemplate">
						<!--                        <xsl:variable name="higherSlots" select="ancestor::PartOfSpeech/AffixSlots/MoInflAffixSlot"/>-->
						<xsl:variable name="higherSlots">
							<xsl:for-each select="$POSes[SubPossibilities[@dst=$pos/@Id]]">
								<xsl:call-template name="GetAnyInflSlotsInHigherPOSesForListing">
									<xsl:with-param name="pos" select="."/>
								</xsl:call-template>
							</xsl:for-each>
						</xsl:variable>
						<xsl:text>  Its templates, however, may use </xsl:text>
						<xsl:choose>
							<xsl:when test="function-available('exsl:node-set')">
								<xsl:choose>
									<xsl:when test="exsl:node-set($higherSlots)[count(affixslot) &gt; 1]">
										<xsl:text>any of these slots: </xsl:text>
									</xsl:when>
									<xsl:otherwise>
										<xsl:text>the slot </xsl:text>
									</xsl:otherwise>
								</xsl:choose>
								<xsl:for-each select="exsl:node-set($higherSlots)/affixslot">
									<xsl:sort select="name"/>
									<xsl:call-template name="OutputInflectionalSlotAsList"/>
								</xsl:for-each>
							</xsl:when>
							<xsl:when test="function-available('saxon:node-set')">
								<xsl:choose>
									<xsl:when test="saxon:node-set($higherSlots)[count(affixslot) &gt; 1]">
										<xsl:text>any of these slots: </xsl:text>
									</xsl:when>
									<xsl:otherwise>
										<xsl:text>the slot </xsl:text>
									</xsl:otherwise>
								</xsl:choose>
								<xsl:for-each select="saxon:node-set($higherSlots)/affixslot">
									<xsl:sort select="name"/>
									<xsl:call-template name="OutputInflectionalSlotAsList"/>
								</xsl:for-each>
							</xsl:when>
							<xsl:otherwise>
								<xsl:choose>
									<xsl:when test="msxslt:node-set($higherSlots)[count(affixslot) &gt; 1]">
										<xsl:text>any of these slots: </xsl:text>
									</xsl:when>
									<xsl:otherwise>
										<xsl:text>the slot </xsl:text>
									</xsl:otherwise>
								</xsl:choose>
								<xsl:for-each select="msxslt:node-set($higherSlots)/affixslot">
									<xsl:sort select="name"/>
									<xsl:call-template name="OutputInflectionalSlotAsList"/>
								</xsl:for-each>
							</xsl:otherwise>
						</xsl:choose>
					</xsl:if>
				</p>
			</xsl:when>
			<xsl:otherwise>
				<p>
					<xsl:text>The following is a listing of the fillers of the slot</xsl:text>
					<xsl:if test="count($slots) &gt; 1">s</xsl:if>
					<xsl:text> involved in </xsl:text>
					<xsl:value-of select="$sPOSName"/>
					<xsl:text> inflection.</xsl:text>
					<!--                    <xsl:variable name="subTemplates" select="$pos/descendant::PartOfSpeech/AffixTemplates/MoInlfAffixTemplate"/>-->
					<xsl:variable name="subTemplates">
						<xsl:for-each select="SubPossibilities">
							<xsl:call-template name="bPOSContainsInflTemplate">
								<xsl:with-param name="pos" select="key('POSID',@dst)"/>
							</xsl:call-template>
						</xsl:for-each>
					</xsl:variable>
					<xsl:if test="contains($subTemplates,'1')">
						<xsl:choose>
							<xsl:when test="count($slots) &gt; 1">These slots are</xsl:when>
							<xsl:otherwise>This slot is</xsl:otherwise>
						</xsl:choose>
						<xsl:text> also valid for the template</xsl:text>
						<xsl:if test="string-length($subTemplates) &gt; 1">s</xsl:if>
						<xsl:text> defined by </xsl:text>
						<!--                        <xsl:variable name="subcats" select="$pos/descendant::PartOfSpeech[AffixTemplates/MoInlfAffixTemplate]"/>-->
						<xsl:variable name="subcats">
							<xsl:for-each select="SubPossibilities">
								<xsl:call-template name="GetAnyNestedInflTemplatesForListing">
									<xsl:with-param name="pos" select="key('POSID',@dst)"/>
								</xsl:call-template>
							</xsl:for-each>
						</xsl:variable>
						<xsl:choose>
							<xsl:when test="function-available('exsl:node-set')">
								<xsl:choose>
									<xsl:when test="exsl:node-set($subcats)[count(subcat) &gt; 1]">these subcategories: </xsl:when>
									<xsl:otherwise>the subcategory </xsl:otherwise>
								</xsl:choose>
								<xsl:for-each select="exsl:node-set($subcats)/subcat">
									<xsl:sort select="name"/>
									<xsl:call-template name="OutputSubcategoryNameAsList"/>
								</xsl:for-each>
							</xsl:when>
							<xsl:when test="function-available('saxon:node-set')">
								<xsl:choose>
									<xsl:when test="saxon:node-set($subcats)[count(subcat) &gt; 1]">these subcategories: </xsl:when>
									<xsl:otherwise>the subcategory </xsl:otherwise>
								</xsl:choose>
								<xsl:for-each select="saxon:node-set($subcats)/subcat">
									<xsl:sort select="name"/>
									<xsl:call-template name="OutputSubcategoryNameAsList"/>
								</xsl:for-each>
							</xsl:when>
							<xsl:otherwise>
								<xsl:choose>
									<xsl:when test="msxslt:node-set($subcats)[count(subcat) &gt; 1]">these subcategories: </xsl:when>
									<xsl:otherwise>the subcategory </xsl:otherwise>
								</xsl:choose>
								<xsl:for-each select="msxslt:node-set($subcats)/subcat">
									<xsl:sort select="name"/>
									<xsl:call-template name="OutputSubcategoryNameAsList"/>
								</xsl:for-each>
							</xsl:otherwise>
						</xsl:choose>
					</xsl:if>
				</p>
				<xsl:for-each select="$slots">
					<xsl:sort select="Name"/>
					<xsl:choose>
						<xsl:when test="$iSubcatLevel=0">
							<section4>
								<xsl:attribute name="id">
									<xsl:text>sInflSlotFillers.</xsl:text>
									<xsl:value-of select="@Id"/>
								</xsl:attribute>
								<secTitle>
									<xsl:call-template name="Capitalize">
										<xsl:with-param name="sStr">
											<xsl:value-of select="Name"/>
										</xsl:with-param>
									</xsl:call-template>
								</secTitle>
								<p>
									<xsl:choose>
										<xsl:when test="string-length(Description) &gt; 0 and Description!=$sEmptyContent">
											<xsl:value-of select="Description"/>
										</xsl:when>
										<xsl:otherwise>
											<xsl:text>These are the morphemes in the </xsl:text>
											<xsl:value-of select="Name"/>
											<xsl:text> slot.</xsl:text>
										</xsl:otherwise>
									</xsl:choose>
								</p>
								<xsl:call-template name="ProcessFillers">
									<xsl:with-param name="Slot" select="@Id"/>
								</xsl:call-template>
							</section4>
						</xsl:when>
						<xsl:when test="$iSubcatLevel=1">
							<section5>
								<xsl:attribute name="id">
									<xsl:text>sInflSlotFillers.</xsl:text>
									<xsl:value-of select="@Id"/>
								</xsl:attribute>
								<secTitle>
									<xsl:call-template name="Capitalize">
										<xsl:with-param name="sStr">
											<xsl:value-of select="Name"/>
										</xsl:with-param>
									</xsl:call-template>
								</secTitle>
								<p>
									<xsl:choose>
										<xsl:when test="string-length(Description) &gt; 0 and Description!=$sEmptyContent">
											<xsl:value-of select="Description"/>
										</xsl:when>
										<xsl:otherwise>
											<xsl:text>These are the morphemes in the </xsl:text>
											<xsl:value-of select="Name"/>
											<xsl:text> slot.</xsl:text>
										</xsl:otherwise>
									</xsl:choose>
								</p>
								<xsl:call-template name="ProcessFillers">
									<xsl:with-param name="Slot" select="@Id"/>
								</xsl:call-template>
							</section5>
						</xsl:when>
						<xsl:when test="$iSubcatLevel=2">
							<section6>
								<xsl:attribute name="id">
									<xsl:text>sInflSlotFillers.</xsl:text>
									<xsl:value-of select="@Id"/>
								</xsl:attribute>
								<secTitle>
									<xsl:call-template name="Capitalize">
										<xsl:with-param name="sStr">
											<xsl:value-of select="Name"/>
										</xsl:with-param>
									</xsl:call-template>
								</secTitle>
								<p>
									<xsl:choose>
										<xsl:when test="string-length(Description) &gt; 0 and Description!=$sEmptyContent">
											<xsl:value-of select="Description"/>
										</xsl:when>
										<xsl:otherwise>
											These are the morphemes in the <xsl:value-of select="Name"/> slot.
										</xsl:otherwise>
									</xsl:choose>
								</p>
								<xsl:call-template name="ProcessFillers">
									<xsl:with-param name="Slot" select="@Id"/>
								</xsl:call-template>
							</section6>
						</xsl:when>
						<xsl:otherwise>
							<ul>
								<li>
									<xsl:attribute name="id">
										<xsl:text>sInflSlotFillers.</xsl:text>
										<xsl:value-of select="@Id"/>
									</xsl:attribute>
									<p>
										<xsl:call-template name="Capitalize">
											<xsl:with-param name="sStr">
												<xsl:value-of select="Name"/>
											</xsl:with-param>
										</xsl:call-template>
									</p>
									<p>
										<xsl:choose>
											<xsl:when test="string-length(Description) &gt; 0 and Description!=$sEmptyContent">
												<xsl:value-of select="Description"/>
											</xsl:when>
											<xsl:otherwise>
												These are the morphemes in the <xsl:value-of select="Name"/> slot.
											</xsl:otherwise>
										</xsl:choose>
									</p>
									<xsl:call-template name="ProcessFillers">
										<xsl:with-param name="Slot" select="@Id"/>
									</xsl:call-template>
								</li>
							</ul>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:for-each>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
						- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
						OutputSubcategoryNameAsList
						display information for a subcategory as part of a list
						Parameters: none
						- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
					-->
	<xsl:template name="OutputSubcategoryNameAsList">
		<xsl:param name="sConjunction" select="' and '"/>
		<xsl:param name="sFinalPunctuation" select="'.'"/>
		<genericRef>
			<xsl:attribute name="gref">
				<xsl:text>sCat.</xsl:text>
				<xsl:value-of select="id"/>
			</xsl:attribute>
			<xsl:value-of select="name"/>
		</genericRef>
		<xsl:call-template name="OutputListPunctuation">
			<xsl:with-param name="sConjunction" select="$sConjunction"/>
			<xsl:with-param name="sFinalPunctuation" select="$sFinalPunctuation"/>
		</xsl:call-template>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputStemName
	display information for a stem name in a table
		Parameters: sn = id of stem name to show
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputStemName">
		<xsl:param name="sn"/>
		<td>
			<xsl:choose>
				<xsl:when test="$sn != 0">
					<xsl:value-of select="key('StemNamesID',$sn)/Name"/>
				</xsl:when>
				<xsl:otherwise>
					<xsl:text disable-output-escaping="yes">&amp;nbsp;</xsl:text>
				</xsl:otherwise>
			</xsl:choose>
		</td>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputStemNameFeature
	display information for a stem name and a feature in a table
		Parameters: sUnusedItems = list of stem names and features to show
							  fs = feature structure
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputStemNameFeature">
		<xsl:param name="sUnusedItems"/>
		<xsl:param name="fs"/>
		<tr>
			<td>
				<xsl:value-of select="key('StemNamesID', substring-before($sUnusedItems, ':'))/Name"/>
			</td>
			<td>
				<xsl:call-template name="OutputFeatureStructure">
					<xsl:with-param name="fs" select="$fs"/>
				</xsl:call-template>
			</td>
		</tr>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputStringRepresentation
	display information for an environment's string representation
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputStringRepresentation">
		<langData lang="lVern">
			<xsl:value-of select="@StringRepresentation"/>
		</langData>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputUnusedBearableFeatures
	display information for unused bearable features
		Parameters: sUnusedBearableFeatures = string containing list of category,FsClosedFeature ids to show
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputUnusedBearableFeatures">
		<xsl:param name="sUnusedBearableFeatures"/>
		<xsl:if test="string-length($sUnusedBearableFeatures) &gt; 0">
			<tr>
				<td>
					<xsl:value-of select="key('POSID', substring-before($sUnusedBearableFeatures, ','))/Name"/>
				</td>
				<td>
					<xsl:value-of select="key('BearableFeaturesID', substring-after(substring-before($sUnusedBearableFeatures, ';'), ','))/Name"/>
				</td>
			</tr>
			<xsl:call-template name="OutputUnusedBearableFeatures">
				<xsl:with-param name="sUnusedBearableFeatures" select="substring-after($sUnusedBearableFeatures, ';')"/>
			</xsl:call-template>
		</xsl:if>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputUnusedItem
	display information for unused items
		Parameters: sUnusedItems = string containing list of ids to show
							  sKeyName = name of key
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputUnusedItems">
		<xsl:param name="sUnusedItems"/>
		<xsl:param name="sKeyName"/>
		<xsl:if test="string-length($sUnusedItems) &gt; 0">
			<tr>
				<td>
					<xsl:value-of select="key($sKeyName, substring-before($sUnusedItems, ','))/Name"/>
				</td>
			</tr>
			<xsl:call-template name="OutputUnusedItems">
				<xsl:with-param name="sUnusedItems" select="substring-after($sUnusedItems, ',')"/>
				<xsl:with-param name="sKeyName" select="$sKeyName"/>
			</xsl:call-template>
		</xsl:if>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputUnusedStemNameFeatures
	display information for unused stem named features
		Parameters: sUnusedItems = string containing list of ids to show
							  sKeyName = name of key
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputUnusedStemNameFeatures">
		<xsl:param name="sUnusedItems"/>
		<xsl:param name="lastFs"/>
		<xsl:if test="string-length($sUnusedItems) &gt; 0">
			<xsl:variable name="fs" select="key('FsFeatStrucsID', substring-before(substring-after($sUnusedItems, ':'), ','))"/>
			<xsl:choose>
				<xsl:when test="$lastFs">
					<xsl:if test="$fs/@Id != $lastFs/@Id">
						<xsl:call-template name="OutputStemNameFeature">
							<xsl:with-param name="sUnusedItems" select="$sUnusedItems"/>
							<xsl:with-param name="fs" select="$fs"/>
						</xsl:call-template>
					</xsl:if>
				</xsl:when>
				<xsl:otherwise>
					<xsl:call-template name="OutputStemNameFeature">
						<xsl:with-param name="sUnusedItems" select="$sUnusedItems"/>
						<xsl:with-param name="fs" select="$fs"/>
					</xsl:call-template>
				</xsl:otherwise>
			</xsl:choose>
			<xsl:call-template name="OutputUnusedStemNameFeatures">
				<xsl:with-param name="sUnusedItems" select="substring-after($sUnusedItems, ',')"/>
				<xsl:with-param name="lastFs" select="$fs"/>
			</xsl:call-template>
		</xsl:if>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		OutputUnusedSlots
		display information for unused items
		Parameters: sUnusedSlots = string containing list of ids to show
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="OutputUnusedSlots">
		<xsl:param name="sUnusedSlots"/>
		<xsl:if test="string-length($sUnusedSlots) &gt; 0">
			<xsl:variable name="slot" select="key('SlotsID',substring-before($sUnusedSlots, ','))"/>
			<xsl:for-each select="$slot">
				<tr>
					<td>
						<xsl:call-template name="OutputPotentiallyBlankItemInTable">
							<xsl:with-param name="item" select="../../Name"/>
						</xsl:call-template>
					</td>
					<td>
						<xsl:value-of select="$slot/Name"/>
					</td>
				</tr>
			</xsl:for-each>
			<xsl:call-template name="OutputUnusedSlots">
				<xsl:with-param name="sUnusedSlots" select="substring-after($sUnusedSlots, ',')"/>
			</xsl:call-template>
		</xsl:if>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ProcessFillers
	display information for an inflectional affix slot's filler
		Parameters: Fillers = nodeset of fillers
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="ProcessFillers">
		<xsl:param name="Slot"/>
		<!-- The following is a bit of a trick.
			If one of the MSAs has a productivity restriction, the variable will contain an R.
			If one of the MSAs has an inflection feature, the variable will contain an F.
			If a lexical entry has at least one allomorph, the variable will contain an A.
		-->
		<xsl:variable name="someEntryHasAllomorphy">
			<xsl:for-each select="$MoInflAffMsas/Slots[@dst=$Slot]">
				<xsl:sort select="key('LexEntryMsa', ../@Id)/../CitationForm"/>
				<xsl:variable name="InflAffixMsa" select="../@Id"/>
				<xsl:variable name="LexEntry" select="$LexEntries[MorphoSyntaxAnalysis/@dst=$InflAffixMsa]"/>
				<xsl:if test="key('InflAffixMsaID',$InflAffixMsa)/FromProdRestrict">R</xsl:if>
				<xsl:if test="key('InflAffixMsaID',$InflAffixMsa)/InflectionFeatures/FsFeatStruc/descendant-or-self::FsClosedValue">F</xsl:if>
				<xsl:for-each select="$LexEntry">
					<xsl:if test="AlternateForms">A</xsl:if>
				</xsl:for-each>
			</xsl:for-each>
		</xsl:variable>
		<xsl:variable name="bDoProdRestricts">
			<xsl:choose>
				<xsl:when test="contains($someEntryHasAllomorphy, 'R')">Y</xsl:when>
				<xsl:otherwise>N</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<xsl:variable name="bDoInflFeats">
			<xsl:choose>
				<xsl:when test="contains($someEntryHasAllomorphy, 'F')">Y</xsl:when>
				<xsl:otherwise>N</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<table type="tLeftOffset">
			<xsl:if test="$bDoProdRestricts='Y' or $bDoInflFeats='Y' or contains($someEntryHasAllomorphy, 'A')">
				<xsl:attribute name="border">1</xsl:attribute>
			</xsl:if>
			<tr>
				<xsl:choose>
					<xsl:when test="$bVernRightToLeft='1'">
						<xsl:if test="contains($someEntryHasAllomorphy,'A')">
							<th align="center">Allomorphy</th>
						</xsl:if>
						<xsl:if test="$bDoProdRestricts='Y'">
							<th>Exception "Features"</th>
						</xsl:if>
						<xsl:if test="$bDoInflFeats='Y'">
							<th>Inflection Features</th>
						</xsl:if>
						<th>Definition</th>
						<th>Gloss</th>
						<th>Form</th>
					</xsl:when>
					<xsl:otherwise>
						<th>Form</th>
						<th>Gloss</th>
						<th>Definition</th>
						<xsl:if test="$bDoInflFeats='Y'">
							<th>Inflection Features</th>
						</xsl:if>
						<xsl:if test="$bDoProdRestricts='Y'">
							<th>Exception "Features"</th>
						</xsl:if>
						<xsl:if test="contains($someEntryHasAllomorphy,'A')">
							<th align="center">Allomorphy</th>
						</xsl:if>
					</xsl:otherwise>
				</xsl:choose>
			</tr>
			<xsl:for-each select="$MoInflAffMsas/Slots[@dst=$Slot]">
				<xsl:sort select="key('LexEntryMsa', ../@Id)/../CitationForm"/>
				<xsl:variable name="InflAffixMsa" select="../@Id"/>
				<xsl:variable name="LexEntry" select="$LexEntries[MorphoSyntaxAnalysis/@dst=$InflAffixMsa]"/>
				<tr>
					<!-- The following is a bit of a trick.
						If any form has inflection class info, the variable will contain an I.
						If any form has an environment, the variable will contain an E.
						If any form has an infix position environment, the variable will contain a P.
					-->
					<xsl:variable name="thisEntryHasAllomorphy">
						<xsl:for-each select="$LexEntry/LexemeForm | $LexEntry/AlternateForms">
							<xsl:variable name="affixform" select="key('MoFormsID',@dst)"/>
							<xsl:if test="$affixform/InflectionClasses">I</xsl:if>
							<xsl:if test="$affixform/PhoneEnv">E</xsl:if>
							<xsl:if test="$affixform/Position">P</xsl:if>
						</xsl:for-each>
					</xsl:variable>
					<xsl:choose>
						<xsl:when test="$bVernRightToLeft='1'">
							<xsl:if test="contains($someEntryHasAllomorphy,'A')">
								<td>
									<xsl:choose>
										<xsl:when test="$LexEntry/AlternateForms">
											<table border="1" type="tEmbeddedTable">
												<tr>
													<xsl:if test="contains($thisEntryHasAllomorphy, 'P')">
														<th>Infix Positions</th>
													</xsl:if>
													<xsl:if test="contains($thisEntryHasAllomorphy, 'E')">
														<th>Environments</th>
													</xsl:if>
													<xsl:if test="contains($thisEntryHasAllomorphy, 'I')">
														<th>Inflection Classes</th>
													</xsl:if>
													<th>Allomorph</th>
												</tr>
												<xsl:call-template name="OutputAllomorphyInfo">
													<xsl:with-param name="form" select="key('MoFormsID', $LexEntry/LexemeForm/@dst)"/>
													<xsl:with-param name="thisEntryHasAllomorphy" select="$thisEntryHasAllomorphy"/>
												</xsl:call-template>
												<xsl:for-each select="$LexEntry/AlternateForms">
													<xsl:call-template name="OutputAllomorphyInfo">
														<xsl:with-param name="form" select="key('MoFormsID', @dst)"/>
														<xsl:with-param name="thisEntryHasAllomorphy" select="$thisEntryHasAllomorphy"/>
													</xsl:call-template>
												</xsl:for-each>
											</table>
										</xsl:when>
										<xsl:otherwise>
											<xsl:text disable-output-escaping="yes">&amp;nbsp;</xsl:text>
										</xsl:otherwise>
									</xsl:choose>
								</td>
							</xsl:if>
							<xsl:if test="$bDoProdRestricts='Y'">
								<xsl:call-template name="OutputExceptionFeatures">
									<xsl:with-param name="features" select="key('InflAffixMsaID',$InflAffixMsa)/FromProdRestrict"/>
								</xsl:call-template>
							</xsl:if>
							<xsl:if test="$bDoInflFeats='Y'">
								<xsl:call-template name="OutputInflectionFeatures">
									<xsl:with-param name="fs" select="key('InflAffixMsaID',$InflAffixMsa)/InflectionFeatures/FsFeatStruc"/>
								</xsl:call-template>
							</xsl:if>
							<xsl:call-template name="OutputInflAffixLexEntry">
								<xsl:with-param name="LexEntry" select="$LexEntry"/>
								<xsl:with-param name="msa" select="../@Id"/>
								<xsl:with-param name="bShowDefinition" select="'Y'"/>
							</xsl:call-template>
						</xsl:when>
						<xsl:otherwise>
							<xsl:call-template name="OutputInflAffixLexEntry">
								<xsl:with-param name="LexEntry" select="$LexEntry"/>
								<xsl:with-param name="msa" select="../@Id"/>
								<xsl:with-param name="bShowDefinition" select="'Y'"/>
							</xsl:call-template>
							<xsl:if test="$bDoInflFeats='Y'">
								<xsl:call-template name="OutputInflectionFeatures">
									<xsl:with-param name="fs" select="key('InflAffixMsaID',$InflAffixMsa)/InflectionFeatures/FsFeatStruc"/>
								</xsl:call-template>
							</xsl:if>
							<xsl:if test="$bDoProdRestricts='Y'">
								<xsl:call-template name="OutputExceptionFeatures">
									<xsl:with-param name="features" select="key('InflAffixMsaID',$InflAffixMsa)/FromProdRestrict"/>
								</xsl:call-template>
							</xsl:if>
							<xsl:if test="contains($someEntryHasAllomorphy,'A')">
								<td>
									<xsl:choose>
										<xsl:when test="$LexEntry/AlternateForms">
											<table border="1" type="tEmbeddedTable">
												<tr>
													<th>Allomorph</th>
													<xsl:if test="contains($thisEntryHasAllomorphy, 'I')">
														<th>Inflection Classes</th>
													</xsl:if>
													<xsl:if test="contains($thisEntryHasAllomorphy, 'E')">
														<th>Environments</th>
													</xsl:if>
													<xsl:if test="contains($thisEntryHasAllomorphy, 'P')">
														<th>Infix Positions</th>
													</xsl:if>
												</tr>
												<xsl:call-template name="OutputAllomorphyInfo">
													<xsl:with-param name="form" select="key('MoFormsID', $LexEntry/LexemeForm/@dst)"/>
													<xsl:with-param name="thisEntryHasAllomorphy" select="$thisEntryHasAllomorphy"/>
												</xsl:call-template>
												<xsl:for-each select="$LexEntry/AlternateForms">
													<xsl:call-template name="OutputAllomorphyInfo">
														<xsl:with-param name="form" select="key('MoFormsID', @dst)"/>
														<xsl:with-param name="thisEntryHasAllomorphy" select="$thisEntryHasAllomorphy"/>
													</xsl:call-template>
												</xsl:for-each>
											</table>
										</xsl:when>
										<xsl:otherwise>
											<xsl:text disable-output-escaping="yes">&amp;nbsp;</xsl:text>
										</xsl:otherwise>
									</xsl:choose>
								</td>
							</xsl:if>
						</xsl:otherwise>
					</xsl:choose>
				</tr>
			</xsl:for-each>
		</table>
	</xsl:template>
	<xsl:template name="OutputAllomorphyInfo">
		<xsl:param name="form"/>
		<xsl:param name="thisEntryHasAllomorphy"/>
		<xsl:if test="not($form/@IsAbstract='1') or $PhonologicalRules">
			<tr>
				<xsl:choose>
					<xsl:when test="$bVernRightToLeft='1'">
						<xsl:if test="contains($thisEntryHasAllomorphy, 'P')">
							<td>
								<xsl:call-template name="OutputListOfEnvironments">
									<xsl:with-param name="environments" select="$form/Position"/>
									<xsl:with-param name="sPosition" select="$sEnvironmentPositionTag"/>
								</xsl:call-template>
							</td>
						</xsl:if>
						<xsl:if test="contains($thisEntryHasAllomorphy, 'E')">
							<td>
								<xsl:call-template name="OutputListOfEnvironments">
									<xsl:with-param name="environments" select="$form/PhoneEnv"/>
								</xsl:call-template>
							</td>
						</xsl:if>
						<xsl:if test="contains($thisEntryHasAllomorphy, 'I')">
							<td>
								<xsl:variable name="inflClasses" select="$form/InflectionClasses"/>
								<xsl:choose>
									<xsl:when test="$inflClasses">
										<xsl:for-each select="$inflClasses">
											<xsl:call-template name="OutputInflectionClassAbbreviation">
												<xsl:with-param name="class" select="@dst"/>
											</xsl:call-template>
											<xsl:if test="not(position()=last())">
												<xsl:text>, </xsl:text>
											</xsl:if>
										</xsl:for-each>
									</xsl:when>
									<xsl:otherwise>
										<xsl:text disable-output-escaping="yes">&amp;nbsp;</xsl:text>
									</xsl:otherwise>
								</xsl:choose>
							</td>
						</xsl:if>
						<td type="tRtl">
							<langData lang="lVern">
								<xsl:value-of select="$form/Form"/>
							</langData>
						</td>
					</xsl:when>
					<xsl:otherwise>
						<td>
							<langData lang="lVern">
								<xsl:value-of select="$form/Form"/>
							</langData>
						</td>
						<xsl:if test="contains($thisEntryHasAllomorphy, 'I')">
							<td>
								<xsl:variable name="inflClasses" select="$form/InflectionClasses"/>
								<xsl:choose>
									<xsl:when test="$inflClasses">
										<xsl:for-each select="$inflClasses">
											<xsl:call-template name="OutputInflectionClassAbbreviation">
												<xsl:with-param name="class" select="@dst"/>
											</xsl:call-template>
											<xsl:if test="not(position()=last())">
												<xsl:text>, </xsl:text>
											</xsl:if>
										</xsl:for-each>
									</xsl:when>
									<xsl:otherwise>
										<xsl:text disable-output-escaping="yes">&amp;nbsp;</xsl:text>
									</xsl:otherwise>
								</xsl:choose>
							</td>
						</xsl:if>
						<xsl:if test="contains($thisEntryHasAllomorphy, 'E')">
							<td>
								<xsl:call-template name="OutputListOfEnvironments">
									<xsl:with-param name="environments" select="$form/PhoneEnv"/>
								</xsl:call-template>
							</td>
						</xsl:if>
						<xsl:if test="contains($thisEntryHasAllomorphy, 'P')">
							<td>
								<xsl:call-template name="OutputListOfEnvironments">
									<xsl:with-param name="environments" select="$form/Position"/>
									<xsl:with-param name="sPosition" select="$sEnvironmentPositionTag"/>
								</xsl:call-template>
							</td>
						</xsl:if>
					</xsl:otherwise>
				</xsl:choose>
			</tr>
		</xsl:if>
	</xsl:template>
	<xsl:template name="OutputListOfEnvironments">
		<xsl:param name="environments"/>
		<xsl:param name="sPosition"/>
		<xsl:choose>
			<xsl:when test="$environments">
				<xsl:for-each select="$environments">
					<xsl:for-each select="key('PhEnvironmentsID', @dst)">
						<xsl:if test="$bVernRightToLeft='1'">
							<xsl:attribute name="type">tRtl</xsl:attribute>
						</xsl:if>
						<genericRef gref="Environment.{@Id}{$sPosition}">
							<xsl:call-template name="OutputStringRepresentation"/>
						</genericRef>
					</xsl:for-each>
					<xsl:if test="not(position()=last())">
						<xsl:text>, </xsl:text>
					</xsl:if>
				</xsl:for-each>
			</xsl:when>
			<xsl:otherwise>
				<xsl:text disable-output-escaping="yes">&amp;nbsp;</xsl:text>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ProcessInflAffixTemplatesForPOS
	display inflectional affix templates for a part of speech and all sub parts of speech
		Parameters: pos = current PartOfSpeech nodeset
						   iSubcatLevel = subcat nesting level
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="ProcessInflAffixTemplatesForPOS">
		<xsl:param name="pos" select="."/>
		<xsl:param name="iSubcatLevel"/>
		<xsl:variable name="sPOSName">
			<xsl:value-of select="$pos/Name"/>
		</xsl:variable>
		<xsl:choose>
			<xsl:when test="$iSubcatLevel&lt;4">
				<secTitle>
					<xsl:value-of select="$sPOSName"/>
					<xsl:text> Templates</xsl:text>
				</secTitle>
			</xsl:when>
			<xsl:otherwise>
				<xsl:attribute name="id">
					<xsl:text>sInflTemplate.</xsl:text>
					<xsl:value-of select="$pos/@Id"/>
				</xsl:attribute>
				<xsl:value-of select="$sPOSName"/>
				<xsl:text> Templates</xsl:text>
			</xsl:otherwise>
		</xsl:choose>
		<xsl:choose>
			<xsl:when test="not($pos/AffixTemplates/MoInflAffixTemplate)">
				<!--                <xsl:variable name="subcats" select="$pos/descendant::PartOfSpeech[AffixTemplates/MoInflAffixTemplate]"/>-->
				<xsl:variable name="subcats">
					<xsl:for-each select="SubPossibilities">
						<xsl:call-template name="bPOSContainsInflTemplate">
							<xsl:with-param name="pos" select="key('POSID',@dst)"/>
						</xsl:call-template>
					</xsl:for-each>
				</xsl:variable>
				<p>
					<xsl:text>There are no inflectional templates for </xsl:text>
					<xsl:value-of select="$sPOSName"/>
					<xsl:choose>
						<xsl:when test="contains($subcats, '1')">
							<xsl:text>, but there are for its subcategor</xsl:text>
							<xsl:choose>
								<xsl:when test="string-length($subcats)&gt;1">
									<xsl:text>ies: </xsl:text>
								</xsl:when>
								<xsl:otherwise>
									<xsl:text>y: </xsl:text>
								</xsl:otherwise>
							</xsl:choose>
							<xsl:variable name="subcatItems">
								<xsl:call-template name="GetAnyNestedInflTemplatesForListing">
									<xsl:with-param name="pos" select="."/>
								</xsl:call-template>
							</xsl:variable>
							<xsl:choose>
								<xsl:when test="function-available('exsl:node-set')">
									<xsl:for-each select="exsl:node-set($subcatItems)/subcat">
										<xsl:sort select="name"/>
										<xsl:call-template name="OutputInflectionalTemplateNameAsList"/>
									</xsl:for-each>
								</xsl:when>
								<xsl:when test="function-available('saxon:node-set')">
									<xsl:for-each select="saxon:node-set($subcatItems)/subcat">
										<xsl:sort select="name"/>
										<xsl:call-template name="OutputInflectionalTemplateNameAsList"/>
									</xsl:for-each>
								</xsl:when>
								<xsl:otherwise>
									<xsl:for-each select="msxslt:node-set($subcatItems)/subcat">
										<xsl:sort select="name"/>
										<xsl:call-template name="OutputInflectionalTemplateNameAsList"/>
									</xsl:for-each>
								</xsl:otherwise>
							</xsl:choose>
						</xsl:when>
						<xsl:otherwise>
							<xsl:text>.</xsl:text>
						</xsl:otherwise>
					</xsl:choose>
				</p>
			</xsl:when>
			<xsl:otherwise>
				<p>
					<xsl:text>The category </xsl:text>
					<xsl:value-of select="$sPOSName"/>
					<xsl:text> has the following template</xsl:text>
					<xsl:if test="count($pos/AffixTemplates/MoInflAffixTemplate)!='1'">
						<xsl:text>s</xsl:text>
					</xsl:if>
					<xsl:text>.</xsl:text>
				</p>
				<xsl:for-each select="$pos/AffixTemplates/MoInflAffixTemplate">
					<xsl:sort select="Name"/>
					<xsl:choose>
						<xsl:when test="$iSubcatLevel=0">
							<section4>
								<xsl:attribute name="id">
									<xsl:text>sInflTemplate.</xsl:text>
									<xsl:value-of select="@Id"/>
								</xsl:attribute>
								<xsl:call-template name="OutputInflectionalAffixTemplate">
									<xsl:with-param name="iSubcatLevel" select="$iSubcatLevel"/>
								</xsl:call-template>
							</section4>
						</xsl:when>
						<xsl:when test="$iSubcatLevel=1">
							<section5>
								<xsl:attribute name="id">
									<xsl:text>sInflTemplate.</xsl:text>
									<xsl:value-of select="@Id"/>
								</xsl:attribute>
								<xsl:call-template name="OutputInflectionalAffixTemplate">
									<xsl:with-param name="iSubcatLevel" select="$iSubcatLevel"/>
								</xsl:call-template>
							</section5>
						</xsl:when>
						<xsl:when test="$iSubcatLevel=2">
							<section6>
								<xsl:attribute name="id">
									<xsl:text>sInflTemplate.</xsl:text>
									<xsl:value-of select="@Id"/>
								</xsl:attribute>
								<xsl:call-template name="OutputInflectionalAffixTemplate">
									<xsl:with-param name="iSubcatLevel" select="$iSubcatLevel"/>
								</xsl:call-template>
							</section6>
						</xsl:when>
						<xsl:otherwise>
							<ul>
								<li>
									<xsl:attribute name="id">
										<xsl:text>sInflTemplate.</xsl:text>
										<xsl:value-of select="@Id"/>
									</xsl:attribute>
									<xsl:call-template name="OutputInflectionalAffixTemplate">
										<xsl:with-param name="iSubcatLevel" select="$iSubcatLevel"/>
									</xsl:call-template>
								</li>
							</ul>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:for-each>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ProcessInflectionForPOS
	display inflectional affix templates, slots and their fillers for a part of speech and all of its sub parts of speech
		Parameters: pos = current PartOfSpeech nodeset
						   iSubcatLevel = subcat nesting level
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="ProcessInflectionForPOS">
		<xsl:param name="pos"/>
		<xsl:param name="iSubcatLevel">0</xsl:param>
		<xsl:variable name="sPOSName">
			<xsl:value-of select="$pos/Name"/>
		</xsl:variable>
		<xsl:variable name="sPOSId" select="$pos/@Id"/>
		<secTitle>
			<xsl:call-template name="Capitalize">
				<xsl:with-param name="sStr">
					<xsl:value-of select="$sPOSName"/>
				</xsl:with-param>
			</xsl:call-template>
			<xsl:text> inflection</xsl:text>
		</secTitle>
		<p>
			<xsl:text>This section lists all inflectional templates and slots for the </xsl:text>
			<xsl:value-of select="$sPOSName"/>
			<xsl:text> category</xsl:text>
			<xsl:variable name="bDescendantOrSelfHasTemplate">
				<xsl:call-template name="bPOSContainsInflTemplate">
					<xsl:with-param name="pos" select="$pos"/>
				</xsl:call-template>
			</xsl:variable>
			<xsl:if test="contains($bDescendantOrSelfHasTemplate,'1')">
				<xsl:text> and its subcategories</xsl:text>
			</xsl:if>
			<xsl:text>.</xsl:text>
		</p>
		<!-- show templates and any slots and filters for this POS -->
		<xsl:choose>
			<xsl:when test="$iSubcatLevel=0">
				<section3>
					<xsl:attribute name="id">
						<xsl:text>sInflTemplate.</xsl:text>
						<xsl:value-of select="$sPOSId"/>
					</xsl:attribute>
					<xsl:call-template name="ProcessInflAffixTemplatesForPOS">
						<xsl:with-param name="pos" select="."/>
						<xsl:with-param name="iSubcatLevel" select="$iSubcatLevel"/>
					</xsl:call-template>
				</section3>
				<section3>
					<xsl:attribute name="id">
						<xsl:text>sInflFillers.</xsl:text>
						<xsl:value-of select="$sPOSId"/>
					</xsl:attribute>
					<xsl:call-template name="OutputSlotsAndFillersForPOS">
						<xsl:with-param name="pos" select="."/>
						<xsl:with-param name="iSubcatLevel" select="$iSubcatLevel"/>
					</xsl:call-template>
				</section3>
			</xsl:when>
			<xsl:when test="$iSubcatLevel=1">
				<section4>
					<xsl:attribute name="id">
						<xsl:text>sInflTemplate.</xsl:text>
						<xsl:value-of select="$sPOSId"/>
					</xsl:attribute>
					<xsl:call-template name="ProcessInflAffixTemplatesForPOS">
						<xsl:with-param name="pos" select="."/>
						<xsl:with-param name="iSubcatLevel" select="$iSubcatLevel"/>
					</xsl:call-template>
				</section4>
				<section4>
					<xsl:attribute name="id">
						<xsl:text>sInflFillers.</xsl:text>
						<xsl:value-of select="$sPOSId"/>
					</xsl:attribute>
					<xsl:call-template name="OutputSlotsAndFillersForPOS">
						<xsl:with-param name="pos" select="."/>
						<xsl:with-param name="iSubcatLevel" select="$iSubcatLevel"/>
					</xsl:call-template>
				</section4>
			</xsl:when>
			<xsl:when test="$iSubcatLevel=2">
				<section5>
					<xsl:attribute name="id">
						<xsl:text>sInflTemplate.</xsl:text>
						<xsl:value-of select="$sPOSId"/>
					</xsl:attribute>
					<xsl:call-template name="ProcessInflAffixTemplatesForPOS">
						<xsl:with-param name="pos" select="."/>
						<xsl:with-param name="iSubcatLevel" select="$iSubcatLevel"/>
					</xsl:call-template>
				</section5>
				<section5>
					<xsl:attribute name="id">
						<xsl:text>sInflFillers.</xsl:text>
						<xsl:value-of select="$sPOSId"/>
					</xsl:attribute>
					<xsl:call-template name="OutputSlotsAndFillersForPOS">
						<xsl:with-param name="pos" select="."/>
						<xsl:with-param name="iSubcatLevel" select="$iSubcatLevel"/>
					</xsl:call-template>
				</section5>
			</xsl:when>
			<xsl:when test="$iSubcatLevel=3">
				<section6>
					<xsl:attribute name="id">
						<xsl:text>sInflTemplate.</xsl:text>
						<xsl:value-of select="$sPOSId"/>
					</xsl:attribute>
					<xsl:call-template name="ProcessInflAffixTemplatesForPOS">
						<xsl:with-param name="pos" select="."/>
						<xsl:with-param name="iSubcatLevel" select="$iSubcatLevel"/>
					</xsl:call-template>
				</section6>
				<section6>
					<xsl:attribute name="id">
						<xsl:text>sInflFillers.</xsl:text>
						<xsl:value-of select="$sPOSId"/>
					</xsl:attribute>
					<xsl:call-template name="OutputSlotsAndFillersForPOS">
						<xsl:with-param name="pos" select="."/>
						<xsl:with-param name="iSubcatLevel" select="$iSubcatLevel"/>
					</xsl:call-template>
				</section6>
			</xsl:when>
			<xsl:otherwise>
				<ul>
					<li>
						<xsl:call-template name="ProcessInflAffixTemplatesForPOS">
							<xsl:with-param name="pos" select="."/>
							<xsl:with-param name="iSubcatLevel" select="$iSubcatLevel"/>
						</xsl:call-template>
					</li>
					<li>
						<xsl:attribute name="id">
							<xsl:text>sInflFillers.</xsl:text>
							<xsl:value-of select="$sPOSId"/>
						</xsl:attribute>
						<xsl:call-template name="OutputSlotsAndFillersForPOS">
							<xsl:with-param name="pos" select="."/>
							<xsl:with-param name="iSubcatLevel" select="$iSubcatLevel"/>
						</xsl:call-template>
					</li>
				</ul>
			</xsl:otherwise>
		</xsl:choose>
		<!-- show inflectional info for all sub parts of speech -->
		<xsl:for-each select="$pos/SubPossibilities">
			<xsl:for-each select="key('POSID',@dst)">
				<xsl:choose>
					<xsl:when test="$iSubcatLevel=0">
						<section3>
							<xsl:attribute name="id">
								<xsl:text>sInflSubcats.</xsl:text>
								<xsl:value-of select="@Id"/>
							</xsl:attribute>
							<xsl:call-template name="ProcessInflectionForPOS">
								<xsl:with-param name="pos" select="."/>
								<xsl:with-param name="iSubcatLevel">1</xsl:with-param>
							</xsl:call-template>
						</section3>
					</xsl:when>
					<xsl:when test="$iSubcatLevel=1">
						<section4>
							<xsl:attribute name="id">
								<xsl:text>sInflSubcats.</xsl:text>
								<xsl:value-of select="@Id"/>
							</xsl:attribute>
							<xsl:call-template name="ProcessInflectionForPOS">
								<xsl:with-param name="pos" select="."/>
								<xsl:with-param name="iSubcatLevel">2</xsl:with-param>
							</xsl:call-template>
						</section4>
					</xsl:when>
					<xsl:when test="$iSubcatLevel=2">
						<section5>
							<xsl:attribute name="id">
								<xsl:text>sInflSubcats.</xsl:text>
								<xsl:value-of select="@Id"/>
							</xsl:attribute>
							<xsl:call-template name="ProcessInflectionForPOS">
								<xsl:with-param name="pos" select="."/>
								<xsl:with-param name="iSubcatLevel">3</xsl:with-param>
							</xsl:call-template>
						</section5>
					</xsl:when>
					<xsl:when test="$iSubcatLevel=3">
						<section6>
							<xsl:attribute name="id">
								<xsl:text>sInflSubcats.</xsl:text>
								<xsl:value-of select="@Id"/>
							</xsl:attribute>
							<xsl:call-template name="ProcessInflectionForPOS">
								<xsl:with-param name="pos" select="."/>
								<xsl:with-param name="iSubcatLevel">4</xsl:with-param>
							</xsl:call-template>
						</section6>
					</xsl:when>
					<xsl:otherwise>
						<ul>
							<li>
								<genericRef>
									<xsl:attribute name="gref">
										<xsl:text>sInflSubcats.</xsl:text>
										<xsl:value-of select="@Id"/>
									</xsl:attribute>
								</genericRef>
								<xsl:call-template name="ProcessInflectionForPOS">
									<xsl:with-param name="pos" select="."/>
									<xsl:with-param name="iSubcatLevel">5</xsl:with-param>
								</xsl:call-template>
							</li>
						</ul>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:for-each>
		</xsl:for-each>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ProcessPOS
	display part of speech and all sub parts of speech
		Parameters: pos = current PartOfSpeech nodeset
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="ProcessPOS">
		<xsl:param name="pos" select="."/>
		<xsl:variable name="sPOSName">
			<xsl:value-of select="$pos/Name"/>
		</xsl:variable>
		<xsl:variable name="idPOS">
			<xsl:value-of select="$pos/@Id"/>
		</xsl:variable>
		<li>
			<genericRef>
				<xsl:attribute name="gref">
					<xsl:text>sCat.</xsl:text>
					<xsl:value-of select="$idPOS"/>
				</xsl:attribute>
				<xsl:call-template name="Capitalize">
					<xsl:with-param name="sStr">
						<xsl:value-of select="$sPOSName"/>
					</xsl:with-param>
				</xsl:call-template>
			</genericRef>
			<xsl:variable name="iCount">
				<xsl:call-template name="GetLexCountOfPOS">
					<xsl:with-param name="idPOS" select="$idPOS"/>
				</xsl:call-template>
			</xsl:variable>
			<xsl:if test="$iCount != '0' or $pos/PartOfSpeech">
				<xsl:text> (</xsl:text>
				<xsl:value-of select="$iCount"/>
				<xsl:text>)</xsl:text>
			</xsl:if>
			<xsl:for-each select="$pos/PartOfSpeech">
				<ul>
					<xsl:call-template name="ProcessPOS">
						<xsl:with-param name="pos" select="."/>
					</xsl:call-template>
				</ul>
			</xsl:for-each>
		</li>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ProcessPOSLex
	display part of speech and all sub parts of speech
		Parameters: pos = current PartOfSpeech nodeset
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="ProcessPOSLex">
		<xsl:param name="pos" select="."/>
		<xsl:variable name="sPOSName">
			<xsl:value-of select="$pos/Name"/>
		</xsl:variable>
		<xsl:variable name="idPOS">
			<xsl:value-of select="$pos/@Id"/>
		</xsl:variable>
		<li>
			<xsl:variable name="iCount">
				<xsl:call-template name="GetLexCountOfPOS">
					<xsl:with-param name="idPOS" select="$idPOS"/>
				</xsl:call-template>
			</xsl:variable>
			<xsl:choose>
				<xsl:when test="$iCount=0">
					<xsl:call-template name="Capitalize">
						<xsl:with-param name="sStr">
							<xsl:value-of select="$sPOSName"/>
						</xsl:with-param>
					</xsl:call-template>
				</xsl:when>
				<xsl:otherwise>
					<genericRef>
						<xsl:attribute name="gref">
							<xsl:text>aMorphsByCat.</xsl:text>
							<xsl:value-of select="$idPOS"/>
						</xsl:attribute>
						<xsl:call-template name="Capitalize">
							<xsl:with-param name="sStr">
								<xsl:value-of select="$sPOSName"/>
							</xsl:with-param>
						</xsl:call-template>
					</genericRef>
					<xsl:text> (</xsl:text>
					<xsl:value-of select="$iCount"/>
					<xsl:text>).</xsl:text>
				</xsl:otherwise>
			</xsl:choose>
		</li>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ProcessSlotFillers
	display slot fillers within an inflectional template
		Parameters: Slots = nodeset of slots
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="ProcessSlotFillers">
		<xsl:param name="Slots"/>
		<xsl:choose>
			<xsl:when test="$bVernRightToLeft='1'">
				<xsl:for-each select="$Slots">
					<xsl:sort select="@ord" order="descending"/>
					<xsl:call-template name="ProcessSlotFiller"/>
				</xsl:for-each>
			</xsl:when>
			<xsl:otherwise>
				<xsl:for-each select="$Slots">
					<xsl:sort select="@ord"/>
					<xsl:call-template name="ProcessSlotFiller"/>
				</xsl:for-each>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ProcessSlotFiller
	display fillers within an inflectional template slot
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="ProcessSlotFiller">
		<xsl:variable name="Slot" select="key('SlotsID', @dst)"/>
		<td>
			<table>
				<xsl:for-each select="$MoInflAffMsas/Slots[@dst=$Slot/@Id]">
					<xsl:sort select="key('LexEntryMsa', ../@Id)/../CitationForm"/>
					<xsl:variable name="InflAffixMsa" select="../@Id"/>
					<xsl:variable name="LexEntry" select="$LexEntries[MorphoSyntaxAnalysis/@dst=$InflAffixMsa]"/>
					<tr>
						<xsl:call-template name="OutputInflAffixLexEntry">
							<xsl:with-param name="LexEntry" select="$LexEntry"/>
							<xsl:with-param name="msa" select="../@Id"/>
						</xsl:call-template>
					</tr>
				</xsl:for-each>
			</table>
		</td>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ProcessSlotName
	display a given slot name within an inflectional template
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="ProcessSlotName">
		<xsl:variable name="Slot" select="key('SlotsID', @dst)"/>
		<xsl:variable name="sSlotName">
			<xsl:value-of select="$Slot/Name"/>
		</xsl:variable>
		<th>
			<xsl:variable name="bOptional">
				<xsl:value-of select="$Slot/@Optional"/>
			</xsl:variable>
			<xsl:if test="$bOptional='true'">
				<xsl:text>(</xsl:text>
			</xsl:if>
			<genericRef>
				<xsl:attribute name="gref">
					<xsl:text>sInflSlotFillers.</xsl:text>
					<xsl:value-of select="@dst"/>
				</xsl:attribute>
				<xsl:call-template name="Capitalize">
					<xsl:with-param name="sStr">
						<xsl:value-of select="$sSlotName"/>
					</xsl:with-param>
				</xsl:call-template>
			</genericRef>
			<xsl:if test="$bOptional='true'">
				<xsl:text>)</xsl:text>
			</xsl:if>
		</th>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ProcessSlotNames
	display slot names within an inflectional template
		Parameters: Slots = nodeset of slots
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="ProcessSlotNames">
		<xsl:param name="Slots"/>
		<xsl:choose>
			<xsl:when test="$bVernRightToLeft='1'">
				<xsl:for-each select="$Slots">
					<xsl:sort select="@ord" order="descending"/>
					<xsl:call-template name="ProcessSlotName"/>
				</xsl:for-each>
			</xsl:when>
			<xsl:otherwise>
				<xsl:for-each select="$Slots">
					<xsl:sort select="@ord"/>
					<xsl:call-template name="ProcessSlotName"/>
				</xsl:for-each>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ProcessSegments
	display segments belonging to a class
		Parameters: Segments = nodeset of segments
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="ProcessSegments">
		<xsl:param name="Segments"/>
		<xsl:for-each select="$Segments">
			<xsl:sort select="key('PhonemesID', @dst)/Name"/>
			<xsl:variable name="Segment" select="key('PhonemesID', @dst)"/>
			<xsl:text disable-output-escaping="yes">&amp;nbsp;&amp;nbsp;</xsl:text>
			<langData lang="lVern">
				<xsl:value-of select="$Segment/Name"/>
			</langData>
			<xsl:if test="not(position()=last())">
				<xsl:text>,</xsl:text>
			</xsl:if>
		</xsl:for-each>
	</xsl:template>
</xsl:stylesheet>
