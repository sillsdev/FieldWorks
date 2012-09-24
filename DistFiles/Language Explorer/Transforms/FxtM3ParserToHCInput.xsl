<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="xml" version="1.0" encoding="UTF-8" indent="yes" doctype-system="Language Explorer/Transforms/HermitCrabInput.dtd"/>

	<xsl:variable name="root" select="/M3Dump"/>
	<xsl:variable name="POSs" select="$root/PartsOfSpeech"/>
	<xsl:variable name="lexicon" select="$root/Lexicon"/>
	<xsl:variable name="allomorphs" select="$lexicon/Allomorphs"/>
	<xsl:variable name="analyses" select="$lexicon/MorphoSyntaxAnalyses"/>
	<xsl:variable name="entries" select="$lexicon/Entries"/>
	<xsl:variable name="phonData" select="$root/PhPhonData"/>
	<xsl:variable name="charTable" select="$phonData/PhonemeSets/PhPhonemeSet[1]"/>
	<xsl:variable name="phonRules" select="$phonData/PhonRules"/>
	<xsl:variable name="varNames">
		<xsl:text>αβγδεζηθικλμνξοπρστυφχψω</xsl:text>
	</xsl:variable>
	<xsl:variable name="wordBdryGuid">
		<xsl:text>7db635e0-9ef3-4167-a594-12551ed89aaa</xsl:text>
	</xsl:variable>
	<xsl:variable name="morphBdryGuid">
		<xsl:text>3bde17ce-e39a-4bae-8a5c-a8d96fd4cb56</xsl:text>
	</xsl:variable>
	<xsl:variable name="morphBdry" select="$charTable/BoundaryMarkers/PhBdryMarker[@Guid = $morphBdryGuid]"/>
	<xsl:variable name="wordBdry" select="$charTable/BoundaryMarkers/PhBdryMarker[@Guid = $wordBdryGuid]"/>
	<xsl:variable name="fNotOnClitics" select="/M3Dump/ParserParameters/ParserParameters/HC/NotOnClitics"/>

	<xsl:key name="AffixAlloId" match="MoAffixAllomorph" use="@Id"/>
	<xsl:key name="StemMsaId" match="MoStemMsa" use="@Id"/>
	<xsl:key name="MsaId" match="MoStemMsa | MoDerivAffMsa | MoInflAffMsa | MoUnclassifiedAffixMsa" use="@Id"/>
	<xsl:key name="MorphTypeId" match="MoMorphType" use="@Id"/>
	<xsl:key name="LexSenseId" match="LexSense" use="@Id"/>
	<xsl:key name="StemAlloId" match="MoStemAllomorph" use="@Id"/>
	<xsl:key name="AlloId" match="MoAffixAllomorph | MoStemAllomorph | MoAffixProcess" use="@Id"/>
	<xsl:key name="SlotId" match="MoInflAffixSlot" use="@Id"/>
	<xsl:key name="POSId" match="PartOfSpeech" use="@Id"/>
	<xsl:key name="NCAbbr" match="PhNCSegments | PhNCFeatures" use="Abbreviation"/>
	<xsl:key name="EnvId" match="PhEnvironment" use="@Id"/>
	<xsl:key name="PhonemeId" match="PhPhoneme" use="@Id"/>
	<xsl:key name="Rep" match="PhPhoneme" use="Codes/PhCode/Representation"/>
	<xsl:key name="FeatTypeId" match="FsFeatStrucType" use="@Id"/>
	<xsl:key name="FeatId" match="FsComplexFeature | FsClosedFeature" use="@Id"/>
	<xsl:key name="InflClassId" match="MoInflClass" use="@Id"/>
	<xsl:key name="FeatConstrId" match="PhFeatureConstraint" use="@Id"/>
	<xsl:key name="CtxtId" match="PhSimpleContextBdry | PhSimpleContextSeg | PhSimpleContextNC | PhIterationContext" use="@Id"/>
	<xsl:key name="BdryId" match="PhBdryMarker" use="@Id"/>
	<xsl:key name="AffixProcessId" match="MoAffixProcess" use="@Id"/>
	<xsl:key name="NCId" match="PhNCSegments | PhNCFeatures" use="@Id"/>
	<xsl:key name="TermUnitId" match="PhPhoneme | PhBdryMarker" use="@Id"/>
	<xsl:key name="PhonRuleFeatId" match="PhonRuleFeat" use="@Id"/>
	<xsl:key name="LexEntryId" match="LexEntry" use="@Id"/>
	<xsl:include href="MorphTypeGuids.xsl"/>
	<xsl:include href="XAmpleTemplateVariables.xsl"/>
	<xsl:include href="FxtM3ParserCommon.xsl"/>

	<xsl:template match="/M3Dump">
		<HermitCrabInput>
			<Language id="lang1">
				<Name>Language</Name>
				<!-- Parts of Speech -->
				<PartsOfSpeech>
					<!-- we need to add a id="pos0" for when the category is unknown -->
					<PartOfSpeech id="pos0">Unknown POS</PartOfSpeech>
					<xsl:apply-templates select="$POSs/PartOfSpeech"/>
				</PartsOfSpeech>
				<Strata id="strata">
					<!-- Strata -->
					<Stratum id="morphophonemic" cyclicity="noncyclic" phonologicalRuleOrder="linear" morphologicalRuleOrder="unordered">
						<xsl:attribute name="characterDefinitionTable">
							<xsl:text>table</xsl:text>
							<xsl:value-of select="$charTable/@Id"/>
						</xsl:attribute>
						<xsl:variable name="templates">
							<xsl:call-template name="TemplateIds"/>
						</xsl:variable>
						<xsl:if test="string-length(normalize-space($templates)) > 0">
							<xsl:attribute name="affixTemplates">
								<xsl:value-of select="normalize-space($templates)"/>
							</xsl:attribute>
						</xsl:if>
						<Name>morphophonemic</Name>
					</Stratum>
					<Stratum id="clitic" morphologicalRuleOrder="unordered">
						<xsl:attribute name="characterDefinitionTable">
							<xsl:text>table</xsl:text>
							<xsl:value-of select="$charTable/@Id"/>
						</xsl:attribute>
						<Name>clitic</Name>
					</Stratum>
					<Stratum id="surface">
						<xsl:attribute name="characterDefinitionTable">
							<xsl:text>table</xsl:text>
							<xsl:value-of select="$charTable/@Id"/>
						</xsl:attribute>
						<Name>surface</Name>
					</Stratum>
					<!-- Affix Templates -->
					<xsl:apply-templates select="$POSs/PartOfSpeech/AffixTemplates/MoInflAffixTemplate"/>
				</Strata>
				<!-- Lexicon -->
				<Lexicon>
					<xsl:call-template name="Lexicon"/>
					<Families/>
				</Lexicon>
				<!-- Phonological Feature System -->
				<xsl:apply-templates select="$root/PhFeatureSystem"/>
				<!-- Character Definition Table -->
				<xsl:apply-templates select="$charTable"/>
				<!-- Natural Classes -->
				<NaturalClasses>
					<FeatureNaturalClass id="ncAny">
						<Name>Any</Name>
					</FeatureNaturalClass>
					<xsl:apply-templates select="$phonData/NaturalClasses/PhNCSegments | $phonData/NaturalClasses/PhNCFeatures"/>
				</NaturalClasses>
				<!-- Phonological Rules -->
				<PhonologicalRules>
					<xsl:apply-templates select="$phonRules/PhRegularRule | $phonRules/PhMetathesisRule">
						<xsl:sort select="@ord" data-type="number" order="ascending"/>
					</xsl:apply-templates>
				</PhonologicalRules>
				<!-- Morphological Rules -->
				<xsl:variable name="compRules" select="CompoundRules/MoEndoCompound | CompoundRules/MoExoCompound"/>
				<MorphologicalRules id="mrs">
					<xsl:choose>
						<xsl:when test="count($compRules) = 0">
							<xsl:call-template name="DefaultCompoundRules"/>
						</xsl:when>
						<xsl:otherwise>
							<xsl:apply-templates select="$compRules"/>
						</xsl:otherwise>
					</xsl:choose>
					<xsl:call-template name="MorphologicalRules"/>
					<xsl:apply-templates select="/M3Dump/LexEntryInflTypes/LexEntryInflType/Slots"/>
				</MorphologicalRules>
				<!-- Morphological/Phonological Rule Features -->
				<xsl:variable name="inflClasses" select="$POSs/PartOfSpeech/InflectionClasses//MoInflClass"/>
				<xsl:variable name="prodRestricts" select="$root/ProdRestrict/CmPossibility"/>
				<xsl:variable name="lexEntryInflTypes" select="$root/LexEntryInflTypes/LexEntryInflType"/>
				<xsl:if test="count($inflClasses) != 0 or count($prodRestricts) != 0">
					<MorphologicalPhonologicalRuleFeatures>
						<xsl:apply-templates select="$inflClasses"/>
						<xsl:apply-templates select="$prodRestricts"/>
						<xsl:apply-templates select="$lexEntryInflTypes"/>
						<xsl:call-template name="MPRFeatureGroup">
							<xsl:with-param name="id" select="'inflClasses'"/>
							<xsl:with-param name="name" select="'Inflection Classes'"/>
							<xsl:with-param name="feats" select="$inflClasses"/>
							<xsl:with-param name="matchType" select="'any'"/>
						</xsl:call-template>
						<xsl:call-template name="MPRFeatureGroup">
							<xsl:with-param name="id" select="'prodRestricts'"/>
							<xsl:with-param name="name" select="'Exception Features'"/>
							<xsl:with-param name="feats" select="$prodRestricts"/>
							<xsl:with-param name="matchType" select="'all'"/>
						</xsl:call-template>
						<xsl:call-template name="MPRFeatureGroup">
							<xsl:with-param name="id" select="'lexEntryInflTypes'"/>
							<xsl:with-param name="name" select="'Irregularly Inflected Forms'"/>
							<xsl:with-param name="feats" select="$lexEntryInflTypes"/>
							<xsl:with-param name="matchType" select="'all'"/>
						</xsl:call-template>
					</MorphologicalPhonologicalRuleFeatures>
				</xsl:if>
				<!-- Head Features -->
				<xsl:apply-templates select="$root/FeatureSystem"/>
			</Language>
		</HermitCrabInput>
	</xsl:template>

	<xsl:template name="TemplateIds">
		<xsl:for-each select="$POSs/PartOfSpeech/AffixTemplates/MoInflAffixTemplate">
			<xsl:variable name="validSlot">
				<xsl:call-template name="HasValidSlot">
					<xsl:with-param name="template" select="."/>
				</xsl:call-template>
			</xsl:variable>
			<xsl:if test="string-length($validSlot) != 0">
				<xsl:text>temp</xsl:text>
				<xsl:value-of select="@Id"/>
				<xsl:if test="position() != last()">
					<xsl:text> </xsl:text>
				</xsl:if>
			</xsl:if>
		</xsl:for-each>
	</xsl:template>

	<!-- Parts of Speech -->

	<xsl:template match="PartOfSpeech">
		<PartOfSpeech>
			<xsl:attribute name="id">
				<xsl:text>pos</xsl:text>
				<xsl:value-of select="@Id"/>
			</xsl:attribute>
			<xsl:value-of select="Name"/>
		</PartOfSpeech>
	</xsl:template>

	<!-- Affix Templates -->

	<xsl:template match="MoInflAffixTemplate">
		<xsl:variable name="validSlot">
			<xsl:call-template name="HasValidSlot">
				<xsl:with-param name="template" select="."/>
			</xsl:call-template>
		</xsl:variable>
		<xsl:if test="string-length($validSlot) != 0">
			<AffixTemplate>
				<xsl:attribute name="id">
					<xsl:text>temp</xsl:text>
					<xsl:value-of select="@Id"/>
				</xsl:attribute>
				<xsl:attribute name="requiredPartsOfSpeech">
					<xsl:text>pos</xsl:text>
					<xsl:value-of select="../../@Id"/>
					<xsl:call-template name="POSIds">
						<xsl:with-param name="posId" select="../../@Id"/>
					</xsl:call-template>
				</xsl:attribute>
				<Name>
					<xsl:value-of select="Name"/>
				</Name>

				<xsl:apply-templates select="SuffixSlots">
					<xsl:sort select="@ord" data-type="number" order="ascending"/>
				</xsl:apply-templates>
				<xsl:apply-templates select="PrefixSlots">
					<xsl:sort select="@ord" data-type="number" order="descending"/>
				</xsl:apply-templates>
			</AffixTemplate>
		</xsl:if>
	</xsl:template>

	<xsl:template name="HasValidSlot">
		<xsl:param name="template"/>

		<xsl:for-each select="$template/SuffixSlots | $template/PrefixSlots">
			<xsl:variable name="rules">
				<xsl:call-template name="SlotRuleIds">
					<xsl:with-param name="slotId" select="@dst"/>
				</xsl:call-template>
			</xsl:variable>
			<xsl:if test="string-length(normalize-space($rules)) != 0">
				<xsl:text>Y</xsl:text>
			</xsl:if>
		</xsl:for-each>
	</xsl:template>

	<xsl:template match="PrefixSlots | SuffixSlots">
		<xsl:variable name="slotId" select="@dst"/>
		<xsl:variable name="rules">
			<xsl:call-template name="SlotRuleIds">
				<xsl:with-param name="slotId" select="$slotId"/>
			</xsl:call-template>
		</xsl:variable>
		<xsl:if test="string-length(normalize-space($rules)) != 0">
			<xsl:variable name="slot" select="key('SlotId', $slotId)"/>
			<Slot>
				<xsl:attribute name="id">
					<xsl:text>slot</xsl:text>
					<xsl:value-of select="../@Id"/>
					<xsl:text>_</xsl:text>
					<xsl:value-of select="@dst"/>
					<xsl:text>_</xsl:text>
					<xsl:variable name="slotKind" select="name()"/>
					<xsl:value-of select="count(preceding-sibling::*[name()=$slotKind])"/>
				</xsl:attribute>
				<xsl:attribute name="optional">
					<xsl:choose>
						<xsl:when test="$slot/@Optional = 'false'">
							<xsl:text>false</xsl:text>
						</xsl:when>
						<xsl:otherwise>
							<xsl:text>true</xsl:text>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:attribute>
				<xsl:attribute name="morphologicalRules">
					<xsl:value-of select="normalize-space($rules)"/>
				</xsl:attribute>
				<Name>
					<xsl:value-of select="$slot/Name"/>
				</Name>
			</Slot>
		</xsl:if>
	</xsl:template>

	<xsl:template name="SlotRuleIds">
		<xsl:param name="slotId"/>
		<xsl:variable name="sMsaSlots">
			<xsl:for-each select="$analyses/MoInflAffMsa[Slots/@dst = $slotId]">
				<xsl:variable name="ruleId" select="@Id"/>
				<xsl:variable name="entry" select="$entries/LexEntry/MorphoSyntaxAnalysis[@dst = $ruleId]/.."/>
				<xsl:variable name="valid">
					<xsl:call-template name="HasValidRuleForm">
						<xsl:with-param name="entry" select="$entry"/>
					</xsl:call-template>
				</xsl:variable>
				<xsl:if test="string-length($valid) != 0">
					<xsl:text>mrule</xsl:text>
					<xsl:value-of select="$ruleId"/>
					<xsl:if test="position() != last()">
						<xsl:text> </xsl:text>
					</xsl:if>
				</xsl:if>
			</xsl:for-each>
		</xsl:variable>
		<xsl:value-of select="$sMsaSlots"/>
		<xsl:variable name="slotOptionality" select="key('SlotId',$slotId)/@Optional"/>
		<xsl:if test="$slotOptionality='false'">
			<xsl:for-each select="$LexEntryInflTypeSlots[@dst = $slotId]">
				<xsl:if test="string-length($sMsaSlots) &gt; 0 or position() != 1">
					<xsl:text> </xsl:text>
				</xsl:if>
				<xsl:text>mrule</xsl:text>
				<xsl:value-of select="../@Id"/>
				<xsl:text>_</xsl:text>
				<xsl:value-of select="$slotId"/>
			</xsl:for-each>
		</xsl:if>
	</xsl:template>

	<!-- Lexicon and Lexical Entries -->

	<xsl:template name="Lexicon">
		<xsl:for-each select="$entries/LexEntry">
			<xsl:variable name="entry" select="."/>
			<xsl:variable name="valid">
				<xsl:call-template name="HasValidLexEntryForm">
					<xsl:with-param name="entry" select="$entry"/>
				</xsl:call-template>
			</xsl:variable>
			<xsl:if test="string-length($valid) != 0">
				<xsl:for-each select="$entry/MorphoSyntaxAnalysis">
					<xsl:variable name="msa" select="key('StemMsaId', @dst)"/>
					<xsl:if test="boolean($msa)">
						<xsl:call-template name="LexicalEntry">
							<xsl:with-param name="entry" select="$entry"/>
							<xsl:with-param name="msa" select="$msa"/>
						</xsl:call-template>
					</xsl:if>
				</xsl:for-each>

				<xsl:for-each select="$entry/LexEntryRef">
					<xsl:variable name="lexEntryRef" select="."/>
					<xsl:for-each select="ComponentLexeme">
						<xsl:variable name="componentLexEntry" select="key('LexEntryId',@dst)"/>
						<xsl:choose>
							<xsl:when test="$entry/Sense">
								<!-- do nothing special -->
							</xsl:when>
							<xsl:when test="$componentLexEntry">
								<xsl:for-each select="$componentLexEntry/MorphoSyntaxAnalysis">
									<xsl:variable name="stemMsa" select="key('StemMsaId',@dst)"/>
									<xsl:call-template name="LexicalEntryOfIrregularlyInflectedForm">
										<xsl:with-param name="lexEntryRef" select="$lexEntryRef"/>
										<xsl:with-param name="entry" select="$entry"/>
										<xsl:with-param name="componentLexEntry" select="$componentLexEntry"/>
										<xsl:with-param name="sVariantOfGloss" select="key('LexSenseId',$componentLexEntry/Sense[1]/@dst)/Gloss"/>
										<xsl:with-param name="msa" select="$stemMsa"/>
										<xsl:with-param name="lexEntryMsa" select="."/>
									</xsl:call-template>
								</xsl:for-each>
							</xsl:when>
							<xsl:otherwise>
								<!-- the component must refer to a sense -->
								<xsl:variable name="componentSense" select="key('LexSenseId',@dst)"/>
								<xsl:variable name="stemMsa" select="key('StemMsaId',$componentSense/@Msa)"/>
								<xsl:call-template name="LexicalEntryOfIrregularlyInflectedForm">
									<xsl:with-param name="lexEntryRef" select="$lexEntryRef"/>
									<xsl:with-param name="entry" select="$entry"/>
									<xsl:with-param name="componentLexEntry" select="$componentSense"/>
									<xsl:with-param name="sVariantOfGloss" select="$componentSense/Gloss"/>
									<xsl:with-param name="msa" select="$stemMsa"/>
								</xsl:call-template>
							</xsl:otherwise>
						</xsl:choose>
					</xsl:for-each>
				</xsl:for-each>


			</xsl:if>
		</xsl:for-each>
	</xsl:template>

	<xsl:template name="HasValidLexEntryForm">
		<xsl:param name="entry"/>

		<xsl:for-each select="$entry/LexemeForm | $entry/AlternateForms">
			<xsl:variable name="form" select="key('StemAlloId', @dst)"/>
			<xsl:variable name="valid">
				<xsl:call-template name="IsValidLexEntryForm">
					<xsl:with-param name="form" select="$form"/>
				</xsl:call-template>
			</xsl:variable>
			<xsl:if test="string-length($valid) != 0">
				<xsl:text>Y</xsl:text>
			</xsl:if>
		</xsl:for-each>
	</xsl:template>

	<xsl:template name="IsValidLexEntryForm">
		<xsl:param name="form"/>

		<xsl:if test="string-length(normalize-space($form/Form)) != 0 and $form/@IsAbstract = '0'">
			<xsl:variable name="morphType" select="key('MorphTypeId', $form/@MorphType)/@Guid"/>
			<xsl:if test="$morphType = $sRoot or $morphType = $sStem or $morphType = $sBoundRoot or $morphType = $sBoundStem or $morphType = $sPhrase or $morphType = $sClitic or $morphType = $sEnclitic or $morphType = $sProclitic or $morphType = $sParticle">
				<xsl:text>Y</xsl:text>
			</xsl:if>
		</xsl:if>
	</xsl:template>

	<xsl:template name="LexicalEntry">
		<xsl:param name="entry"/>
		<xsl:param name="msa"/>
		<xsl:variable name="morphType" select="key('MorphTypeId', $entry/LexemeForm/@MorphType)/@Guid"/>
		<LexicalEntry>
			<xsl:attribute name="id">
				<xsl:text>lex</xsl:text>
				<xsl:value-of select="$msa/@Id"/>
			</xsl:attribute>
			<xsl:call-template name="LexicalEntryPosStratumAttrs">
				<xsl:with-param name="msa" select="$msa"/>
				<xsl:with-param name="morphType" select="$morphType"/>
			</xsl:call-template>
			<xsl:variable name="inflClass">
				<xsl:call-template name="LexicalEntryInflClassValue">
					<xsl:with-param name="msa" select="$msa"/>
				</xsl:call-template>
			</xsl:variable>
			<xsl:variable name="mprFeats">
				<xsl:if test="string-length($inflClass) != 0">
					<xsl:text>mpr</xsl:text>
					<xsl:value-of select="$inflClass"/>
					<xsl:text> </xsl:text>
				</xsl:if>
				<xsl:for-each select="$msa/ProdRestrict">
					<xsl:text>mpr</xsl:text>
					<xsl:value-of select="@dst"/>
					<xsl:text> </xsl:text>
				</xsl:for-each>
			</xsl:variable>
			<xsl:if test="string-length(normalize-space($mprFeats)) != 0">
				<xsl:attribute name="ruleFeatures">
					<xsl:value-of select="normalize-space($mprFeats)"/>
				</xsl:attribute>
			</xsl:if>
			<xsl:variable name="headFeats" select="$msa/InflectionFeatures/FsFeatStruc[node()]"/>
			<xsl:call-template name="LexicalEntryAllomorphs">
				<xsl:with-param name="sStemName" select="$sStemName"/>
				<xsl:with-param name="entry" select="$entry"/>
				<xsl:with-param name="msa" select="$msa"/>
				<xsl:with-param name="morphType" select="$morphType"/>
				<xsl:with-param name="headFeats" select="$headFeats"/>
				<xsl:with-param name="inflClass" select="$inflClass"/>
			</xsl:call-template>
			<xsl:call-template name="Gloss">
				<xsl:with-param name="sense" select="key('LexSenseId', $entry/Sense[1]/@dst)"/>
				<xsl:with-param name="id">
					<xsl:value-of select="$msa/@Id"/>
					<xsl:text>_LEX</xsl:text>
				</xsl:with-param>
			</xsl:call-template>
			<xsl:if test="count($headFeats) != 0">
				<AssignedHeadFeatures>
					<xsl:apply-templates select="$headFeats" mode="morphosyntactic">
						<xsl:with-param name="id" select="$msa/@Id"/>
					</xsl:apply-templates>
				</AssignedHeadFeatures>
			</xsl:if>
			<xsl:call-template name="AdhocMorphRules">
				<xsl:with-param name="msaId" select="$msa/@Id"/>
			</xsl:call-template>
		</LexicalEntry>
	</xsl:template>

	<xsl:template name="LexicalEntryOfIrregularlyInflectedForm">
		<xsl:param name="entry"/>
		<xsl:param name="msa"/>
		<xsl:param name="lexEntryRef"/>
		<xsl:param name="componentLexEntry"/>
		<xsl:param name="sVariantOfGloss"/>
		<xsl:param name="lexEntryMsa"/>
		<xsl:variable name="morphType" select="key('MorphTypeId', $entry/LexemeForm/@MorphType)/@Guid"/>
		<xsl:if test="$lexEntryRef/LexEntryInflType">
			<LexicalEntry>
				<xsl:variable name="sIdOfIrregularlyInflectedFormEntry">
					<xsl:call-template name="IdOfIrregularlyInflectedFormEntry">
						<xsl:with-param name="lexEntry" select="$entry"/>
						<xsl:with-param name="lexEntryRef" select="$lexEntryRef"/>
						<xsl:with-param name="msa" select="$lexEntryMsa"/>
					</xsl:call-template>
				</xsl:variable>
				<xsl:attribute name="id">
					<xsl:text>lex</xsl:text>
					<xsl:value-of select="$sIdOfIrregularlyInflectedFormEntry"/>
				</xsl:attribute>
				<xsl:call-template name="LexicalEntryPosStratumAttrs">
					<xsl:with-param name="msa" select="$msa"/>
					<xsl:with-param name="morphType" select="$morphType"/>
				</xsl:call-template>
				<xsl:variable name="inflClass">
					<xsl:call-template name="LexicalEntryInflClassValue">
						<xsl:with-param name="msa" select="$msa"/>
					</xsl:call-template>
				</xsl:variable>
				<xsl:variable name="mprFeats">
					<xsl:if test="string-length($inflClass) != 0">
						<xsl:text>mpr</xsl:text>
						<xsl:value-of select="$inflClass"/>
						<xsl:text> </xsl:text>
					</xsl:if>
					<xsl:for-each select="$msa/ProdRestrict">
						<xsl:text>mpr</xsl:text>
						<xsl:value-of select="@dst"/>
						<xsl:text> </xsl:text>
					</xsl:for-each>
					<xsl:for-each select="$lexEntryRef/LexEntryInflType">
						<xsl:variable name="lexEnryInflType" select="key('LexEntryInflTypeID',@dst)"/>
						<xsl:if test="$lexEnryInflType">
							<xsl:text>mpr</xsl:text>
							<xsl:value-of select="$lexEnryInflType/@Id"/>
							<xsl:text> </xsl:text>
						</xsl:if>
					</xsl:for-each>
				</xsl:variable>
				<xsl:if test="string-length(normalize-space($mprFeats)) != 0">
					<xsl:attribute name="ruleFeatures">
						<xsl:value-of select="normalize-space($mprFeats)"/>
					</xsl:attribute>
				</xsl:if>
				<xsl:variable name="headFeats" select="$msa/InflectionFeatures/FsFeatStruc[node()]"/>
				<xsl:call-template name="LexicalEntryAllomorphs">
					<xsl:with-param name="sStemName" select="$sStemName"/>
					<xsl:with-param name="entry" select="$entry"/>
					<xsl:with-param name="msa" select="$msa"/>
					<xsl:with-param name="morphType" select="$morphType"/>
					<xsl:with-param name="headFeats" select="$headFeats"/>
					<xsl:with-param name="inflClass" select="$inflClass"/>
					<xsl:with-param name="sIdOfIrregularlyInflectedFormEntry" select="$sIdOfIrregularlyInflectedFormEntry"/>
				</xsl:call-template>
				<xsl:variable name="gloss">
					<xsl:call-template name="GlossOfIrregularlyInflectedForm">
						<xsl:with-param name="lexEntryRef" select="$lexEntryRef"/>
						<xsl:with-param name="sVariantOfGloss" select="$sVariantOfGloss"/>
					</xsl:call-template>
				</xsl:variable>
				<Gloss>
					<xsl:attribute name="id">
						<xsl:text>gl</xsl:text>
						<xsl:value-of select="$entry/@Id"/>
						<xsl:text>_</xsl:text>
						<xsl:value-of select="$componentLexEntry/@Id"/>
						<xsl:call-template name="AppendAnyMsaCountNumber">
							<xsl:with-param name="msa" select="$lexEntryMsa"/>
						</xsl:call-template>
						<xsl:text>_LEX</xsl:text>
					</xsl:attribute>
					<xsl:value-of select="$gloss"/>
				</Gloss>
				<xsl:if test="count($headFeats) != 0">
					<AssignedHeadFeatures>
						<xsl:variable name="sIrregIdToUse">
							<xsl:value-of select="$msa/@Id"/>
							<xsl:text>_</xsl:text>
							<xsl:value-of select="$entry/@Id"/>
						</xsl:variable>
						<xsl:apply-templates select="$headFeats" mode="morphosyntactic">
							<xsl:with-param name="id" select="$sIrregIdToUse"/>
						</xsl:apply-templates>
						<xsl:for-each select="$lexEntryRef/LexEntryInflType">
							<xsl:variable name="lexEnryInflType" select="key('LexEntryInflTypeID',@dst)"/>
							<xsl:if test="not($lexEnryInflType/Slots) and $lexEnryInflType/InflectionFeatures/FsFeatStruc">
								<xsl:apply-templates select="$headFeats" mode="morphosyntactic">
									<xsl:with-param name="id" select="$lexEnryInflType/InflectionFeatures/FsFeatStruc/@Id"/>
								</xsl:apply-templates>
							</xsl:if>
						</xsl:for-each>
					</AssignedHeadFeatures>
				</xsl:if>
				<xsl:call-template name="AdhocMorphRules">
					<xsl:with-param name="msaId" select="$msa/@Id"/>
				</xsl:call-template>
			</LexicalEntry>
		</xsl:if>
	</xsl:template>

	<xsl:template name="LexicalEntryAllomorphs">
		<xsl:param name="sStemName"/>
		<xsl:param name="entry"/>
		<xsl:param name="msa"/>
		<xsl:param name="morphType"/>
		<xsl:param name="headFeats"/>
		<xsl:param name="inflClass"/>
		<xsl:param name="sIdOfIrregularlyInflectedFormEntry" select="''"/>
		<xsl:variable name="allos" select="$entry/LexemeForm | $entry/AlternateForms"/>
		<xsl:variable name="stemAllos" select="$allomorphs/MoStemAllomorph[@Id=$allos/@dst and @IsAbstract='0' and @MorphType!=$sDiscontiguousPhrase and @StemName!='0']"/>
		<xsl:variable name="stemNameList">
			<xsl:for-each select="$stemAllos">
				<xsl:sort select="@StemName"/>
				<xsl:value-of select="$sStemName"/>
				<xsl:value-of select="@StemName"/>
				<xsl:text>;</xsl:text>
			</xsl:for-each>
		</xsl:variable>
		<xsl:variable name="allStemNames">
			<xsl:call-template name="OutputUniqueStrings">
				<xsl:with-param name="sList" select="$stemNameList"/>
			</xsl:call-template>
		</xsl:variable>
		<Allomorphs>
			<xsl:for-each select="$entry/AlternateForms">
				<xsl:sort select="@ord" data-type="number" order="ascending"/>
				<xsl:call-template name="StemAllomorph">
					<xsl:with-param name="form" select="key('StemAlloId', @dst)"/>
					<xsl:with-param name="msa" select="$msa"/>
					<xsl:with-param name="morphType" select="$morphType"/>
					<xsl:with-param name="headFeats" select="$headFeats"/>
					<xsl:with-param name="inflClass" select="$inflClass"/>
					<xsl:with-param name="allStemNames" select="$allStemNames"/>
					<xsl:with-param name="sIdOfIrregularlyInflectedFormEntry" select="$sIdOfIrregularlyInflectedFormEntry"/>
				</xsl:call-template>
			</xsl:for-each>
			<xsl:call-template name="StemAllomorph">
				<xsl:with-param name="form" select="key('StemAlloId', $entry/LexemeForm/@dst)"/>
				<xsl:with-param name="msa" select="$msa"/>
				<xsl:with-param name="morphType" select="$morphType"/>
				<xsl:with-param name="headFeats" select="$headFeats"/>
				<xsl:with-param name="inflClass" select="$inflClass"/>
				<xsl:with-param name="allStemNames" select="$allStemNames"/>
				<xsl:with-param name="sIdOfIrregularlyInflectedFormEntry" select="$sIdOfIrregularlyInflectedFormEntry"/>
			</xsl:call-template>
		</Allomorphs>
	</xsl:template>

	<xsl:template name="LexicalEntryInflClassValue">
		<xsl:param name="msa"/>
		<xsl:choose>
			<xsl:when test="$msa/@InflectionClass != '0'">
				<xsl:value-of select="$msa/@InflectionClass"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:call-template name="POSDefaultInflClass">
					<xsl:with-param name="pos" select="key('POSId', $msa/@PartOfSpeech)"/>
				</xsl:call-template>
			</xsl:otherwise>
		</xsl:choose>

	</xsl:template>

	<xsl:template name="LexicalEntryPosStratumAttrs">
		<xsl:param name="msa"/>
		<xsl:param name="morphType"/>
		<xsl:attribute name="partOfSpeech">
			<xsl:text>pos</xsl:text>
			<xsl:value-of select="$msa/@PartOfSpeech"/>
		</xsl:attribute>
		<xsl:attribute name="stratum">
			<xsl:choose>
				<xsl:when test="$morphType = $sClitic or $morphType = $sEnclitic or $morphType = $sProclitic or $morphType = $sParticle">
					<xsl:text>clitic</xsl:text>
				</xsl:when>
				<xsl:otherwise>
					<xsl:text>morphophonemic</xsl:text>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:attribute>
	</xsl:template>

	<xsl:template name="StemAllomorph">
		<xsl:param name="form"/>
		<xsl:param name="msa"/>
		<xsl:param name="morphType"/>
		<xsl:param name="headFeats"/>
		<xsl:param name="inflClass"/>
		<xsl:param name="allStemNames"/>
		<xsl:param name="sIdOfIrregularlyInflectedFormEntry"/>

		<xsl:variable name="valid">
			<xsl:call-template name="IsValidLexEntryForm">
				<xsl:with-param name="form" select="$form"/>
			</xsl:call-template>
		</xsl:variable>
		<xsl:if test="string-length($valid) != 0">
			<xsl:variable name="id">
				<xsl:value-of select="$msa/@Id"/>
				<xsl:text>_</xsl:text>
				<xsl:value-of select="$form/@Id"/>
			</xsl:variable>
			<Allomorph>
				<xsl:attribute name="id">
					<xsl:text>allo</xsl:text>
					<xsl:value-of select="$id"/>
				</xsl:attribute>
				<PhoneticShape>
					<xsl:call-template name="FormatForm">
						<xsl:with-param name="formStr" select="$form/Form"/>
					</xsl:call-template>
				</PhoneticShape>
				<xsl:variable name="envIds" select="$form/PhoneEnv/@dst"/>
				<xsl:variable name="envs" select="$phonData/Environments/PhEnvironment[@Id = $envIds]"/>
				<xsl:if test="count($envs) != 0">
					<RequiredEnvironments>
						<xsl:for-each select="$envs">
							<xsl:variable name="envStr" select="normalize-space(substring-after(@StringRepresentation, '/'))"/>
							<Environment>
								<xsl:variable name="left" select="normalize-space(substring-before($envStr, '_'))"/>
								<xsl:if test="string-length($left) != 0">
									<LeftEnvironment>
										<xsl:attribute name="id">
											<xsl:text>env</xsl:text>
											<xsl:value-of select="$id"/>
											<xsl:text>_</xsl:text>
											<xsl:value-of select="@Id"/>
											<xsl:text>_L_LEX</xsl:text>
										</xsl:attribute>
										<PhoneticTemplate>
											<xsl:if test="starts-with($left, '#')">
												<xsl:attribute name="initialBoundaryCondition">
													<xsl:text>true</xsl:text>
												</xsl:attribute>
											</xsl:if>
											<PhoneticSequence>
												<xsl:call-template name="ProcessEnv">
													<xsl:with-param name="env" select="$left"/>
												</xsl:call-template>
											</PhoneticSequence>
										</PhoneticTemplate>
									</LeftEnvironment>
								</xsl:if>
								<xsl:variable name="right" select="normalize-space(substring-after($envStr, '_'))"/>
								<xsl:if test="string-length($right) != 0">
									<RightEnvironment>
										<xsl:attribute name="id">
											<xsl:text>env</xsl:text>
											<xsl:value-of select="$id"/>
											<xsl:text>_</xsl:text>
											<xsl:value-of select="@Id"/>
											<xsl:text>_R_LEX</xsl:text>
										</xsl:attribute>
										<PhoneticTemplate>
											<xsl:if test="substring($right, string-length($right)) = '#'">
												<xsl:attribute name="finalBoundaryCondition">
													<xsl:text>true</xsl:text>
												</xsl:attribute>
											</xsl:if>
											<PhoneticSequence>
												<xsl:call-template name="ProcessEnv">
													<xsl:with-param name="env" select="$right"/>
												</xsl:call-template>
											</PhoneticSequence>
										</PhoneticTemplate>
									</RightEnvironment>
								</xsl:if>
							</Environment>
						</xsl:for-each>
					</RequiredEnvironments>
				</xsl:if>
				<xsl:call-template name="AdhocAlloRules">
					<xsl:with-param name="formId" select="$form/@Id"/>
				</xsl:call-template>
				<Properties>
					<Property name="WordCategory">
						<xsl:choose>
							<xsl:when test="$morphType = $sRoot or $morphType = $sStem or $morphType = $sBoundRoot or $morphType = $sBoundStem or $morphType = $sPhrase">
								<xsl:choose>
									<xsl:when test="string-length($msa/@Components) != 0">
										<xsl:text>Stem</xsl:text>
									</xsl:when>
									<xsl:otherwise>
										<xsl:text>root</xsl:text>
									</xsl:otherwise>
								</xsl:choose>
							</xsl:when>
							<xsl:otherwise>
								<xsl:text>clitic</xsl:text>
							</xsl:otherwise>
						</xsl:choose>
					</Property>
					<Property name="FormID">
						<xsl:value-of select="$form/@Id"/>
					</Property>
					<Property name="MsaID">
						<xsl:choose>
							<xsl:when test="string-length($sIdOfIrregularlyInflectedFormEntry) &gt; 0">
								<xsl:value-of select="$sIdOfIrregularlyInflectedFormEntry"/>
							</xsl:when>
							<xsl:otherwise>
								<xsl:value-of select="$msa/@Id"/>
							</xsl:otherwise>
						</xsl:choose>
					</Property>
					<Property name="FeatureDescriptors">
						<xsl:choose>
							<xsl:when test="$morphType = $sRoot or $morphType = $sStem or $morphType = $sBoundRoot or $morphType = $sBoundStem or $morphType = $sPhrase">
								<xsl:value-of select="$sRootPOS"/>
							</xsl:when>
							<xsl:otherwise>
								<xsl:value-of select="$sCliticPOS"/>
							</xsl:otherwise>
						</xsl:choose>
						<xsl:value-of select="$msa/@PartOfSpeech"/>
						<xsl:if test="count($headFeats) != 0">
							<xsl:text> </xsl:text>
							<xsl:value-of select="$sMSFS"/>
							<xsl:value-of select="$headFeats/@Id"/>
						</xsl:if>
						<xsl:if test="string-length($inflClass) != 0">
							<xsl:text> </xsl:text>
							<xsl:value-of select="$sInflClass"/>
							<xsl:value-of select="$inflClass"/>
						</xsl:if>
						<xsl:for-each select="$msa/ProdRestrict">
							<xsl:text> </xsl:text>
							<xsl:value-of select="$sExceptionFeature"/>
							<xsl:value-of select="@dst"/>
							<xsl:text>Plus</xsl:text>
						</xsl:for-each>
						<xsl:choose>
							<xsl:when test="$form/@StemName != '0'">
								<xsl:text> </xsl:text>
								<xsl:value-of select="$sStemName"/>
								<xsl:value-of select="$form/@StemName"/>
							</xsl:when>
							<xsl:when test="string-length($allStemNames) != 0">
								<xsl:text> Not</xsl:text>
								<xsl:value-of select="$allStemNames"/>
							</xsl:when>
						</xsl:choose>
					</Property>
				</Properties>
			</Allomorph>
		</xsl:if>
	</xsl:template>

	<!-- Morphosyntactic Features -->

	<xsl:template match="FeatureSystem">
		<HeadFeatures>
			<xsl:apply-templates select="Features/FsComplexFeature | Features/FsClosedFeature"/>
		</HeadFeatures>
	</xsl:template>

	<xsl:template match="FsClosedFeature">
		<FeatureDefinition>
			<xsl:attribute name="id">
				<xsl:text>fd</xsl:text>
				<xsl:value-of select="@Id"/>
			</xsl:attribute>
			<Feature>
				<xsl:attribute name="id">
					<xsl:text>feat</xsl:text>
					<xsl:value-of select="@Id"/>
				</xsl:attribute>
				<xsl:value-of select="Abbreviation"/>
			</Feature>
			<ValueList>
				<xsl:for-each select="Values/FsSymFeatVal">
					<Value>
						<xsl:attribute name="id">
							<xsl:text>val</xsl:text>
							<xsl:value-of select="@Id"/>
						</xsl:attribute>
						<xsl:value-of select="Abbreviation"/>
					</Value>
				</xsl:for-each>
			</ValueList>
		</FeatureDefinition>
	</xsl:template>

	<xsl:template match="FsComplexFeature">
		<FeatureDefinition>
			<xsl:attribute name="id">
				<xsl:text>fd</xsl:text>
				<xsl:value-of select="@Id"/>
			</xsl:attribute>
			<Feature>
				<xsl:attribute name="id">
					<xsl:text>feat</xsl:text>
					<xsl:value-of select="@Id"/>
				</xsl:attribute>
				<xsl:value-of select="Abbreviation"/>
			</Feature>
			<FeatureList>
				<xsl:variable name="type" select="key('FeatTypeId', Type/@dst)"/>
				<xsl:attribute name="features">
					<xsl:for-each select="$type/Features/Feature">
						<xsl:text>fd</xsl:text>
						<xsl:value-of select="@dst"/>
						<xsl:if test="position() != last()">
							<xsl:text> </xsl:text>
						</xsl:if>
					</xsl:for-each>
				</xsl:attribute>
			</FeatureList>
		</FeatureDefinition>
	</xsl:template>

	<xsl:template match="FsFeatStruc" mode="morphosyntactic">
		<xsl:param name="id"/>
		<xsl:apply-templates select="FsComplexValue | FsClosedValue" mode="morphosyntactic">
			<xsl:with-param name="id" select="$id"/>
		</xsl:apply-templates>
	</xsl:template>

	<xsl:template match="FsComplexValue" mode="morphosyntactic">
		<xsl:param name="id"/>
		<FeatureValueList>
			<xsl:attribute name="id">
				<xsl:text>vi</xsl:text>
				<xsl:value-of select="@Id"/>
				<xsl:if test="string-length($id) != 0">
					<xsl:text>_</xsl:text>
					<xsl:value-of select="$id"/>
				</xsl:if>
			</xsl:attribute>
			<xsl:attribute name="feature">
				<xsl:text>feat</xsl:text>
				<xsl:value-of select="@Feature"/>
			</xsl:attribute>
			<xsl:apply-templates select="FsFeatStruc[@Type != 0]" mode="morphosyntactic">
				<xsl:with-param name="id" select="$id"/>
			</xsl:apply-templates>
		</FeatureValueList>
	</xsl:template>

	<xsl:template match="FsClosedValue" mode="morphosyntactic">
		<xsl:param name="id"/>
		<FeatureValueList>
			<xsl:attribute name="id">
				<xsl:text>vi</xsl:text>
				<xsl:value-of select="@Id"/>
				<xsl:if test="string-length($id) != 0">
					<xsl:text>_</xsl:text>
					<xsl:value-of select="$id"/>
				</xsl:if>
			</xsl:attribute>
			<xsl:attribute name="feature">
				<xsl:text>feat</xsl:text>
				<xsl:value-of select="@Feature"/>
			</xsl:attribute>
			<xsl:attribute name="values">
				<xsl:text>val</xsl:text>
				<xsl:value-of select="@Value"/>
			</xsl:attribute>
		</FeatureValueList>
	</xsl:template>

	<!-- Phonological Features -->

	<xsl:template match="PhFeatureSystem">
		<PhonologicalFeatureSystem id="pfs">
			<xsl:apply-templates select="Features/FsClosedFeature"/>
		</PhonologicalFeatureSystem>
	</xsl:template>

	<xsl:template match="FsFeatStruc" mode="phonological">
		<xsl:apply-templates select="FsClosedValue" mode="phonological"/>
	</xsl:template>

	<xsl:template match="FsClosedValue" mode="phonological">
		<FeatureValuePair>
			<xsl:attribute name="feature">
				<xsl:text>feat</xsl:text>
				<xsl:value-of select="@Feature"/>
			</xsl:attribute>
			<xsl:attribute name="value">
				<xsl:text>val</xsl:text>
				<xsl:value-of select="@Value"/>
			</xsl:attribute>
		</FeatureValuePair>
	</xsl:template>

	<!-- Character Definition Table -->

	<xsl:template match="PhPhonemeSet">
		<CharacterDefinitionTable>
			<xsl:attribute name="id">
				<xsl:text>table</xsl:text>
				<xsl:value-of select="@Id"/>
			</xsl:attribute>
			<Name>
				<xsl:value-of select="Name"/>
			</Name>
			<Encoding>
				<xsl:text>encoding1</xsl:text>
			</Encoding>
			<xsl:apply-templates select="Phonemes"/>
			<xsl:apply-templates select="BoundaryMarkers"/>
		</CharacterDefinitionTable>
	</xsl:template>

	<xsl:template match="Phonemes">
		<SegmentDefinitions>
			<xsl:apply-templates select="PhPhoneme"/>
		</SegmentDefinitions>
	</xsl:template>

	<xsl:template match="PhPhoneme">
		<xsl:variable name="phoneme" select="."/>
		<xsl:for-each select="Codes/PhCode">
			<SegmentDefinition>
				<Representation>
					<xsl:attribute name="id">
						<xsl:call-template name="BuildRepresentationID">
							<xsl:with-param name="phoneme" select="$phoneme"/>
						</xsl:call-template>
					</xsl:attribute>
					<xsl:value-of select="normalize-space(Representation)"/>
				</Representation>
				<xsl:apply-templates select="$phoneme/PhonologicalFeatures/FsFeatStruc" mode="phonological"/>
			</SegmentDefinition>
		</xsl:for-each>
	</xsl:template>
	<xsl:template name="BuildRepresentationID">
		<xsl:param name="phoneme"/>
		<xsl:text>rep</xsl:text>
		<xsl:value-of select="$phoneme/@Id"/>
		<xsl:text>_</xsl:text>
		<xsl:value-of select="position()"/>
	</xsl:template>

	<xsl:template match="BoundaryMarkers">
		<BoundaryDefinitions>
			<xsl:apply-templates select="PhBdryMarker"/>
			<BoundarySymbol id="repNull">^0</BoundarySymbol>
			<BoundarySymbol id="repWordBdry">.</BoundarySymbol>
		</BoundaryDefinitions>
	</xsl:template>

	<xsl:template match="PhBdryMarker">
		<xsl:if test="@Guid != $wordBdryGuid">
			<BoundarySymbol>
				<xsl:attribute name="id">
					<xsl:text>rep</xsl:text>
					<xsl:value-of select="@Id"/>
				</xsl:attribute>
				<xsl:value-of select="Codes/PhCode[1]/Representation"/>
			</BoundarySymbol>
		</xsl:if>
	</xsl:template>

	<!-- Natural Classes -->

	<xsl:template match="PhNCSegments">
		<SegmentNaturalClass>
			<xsl:attribute name="id">
				<xsl:text>nc</xsl:text>
				<xsl:value-of select="@Id"/>
			</xsl:attribute>
			<Name>
				<xsl:value-of select="Abbreviation"/>
			</Name>
			<xsl:apply-templates select="Segments"/>
		</SegmentNaturalClass>
	</xsl:template>

	<xsl:template match="Segments">
		<xsl:variable name="phoneme" select="key('PhonemeId', @dst)"/>
		<xsl:for-each select="$phoneme/Codes/PhCode">
			<Segment>
				<xsl:attribute name="characterTable">
					<xsl:text>table</xsl:text>
					<xsl:value-of select="$charTable/@Id"/>
				</xsl:attribute>
				<xsl:attribute name="representation">
					<xsl:call-template name="BuildRepresentationID">
						<xsl:with-param name="phoneme" select="$phoneme"/>
					</xsl:call-template>
				</xsl:attribute>
			</Segment>
		</xsl:for-each>
	</xsl:template>

	<xsl:template match="PhNCFeatures">
		<FeatureNaturalClass>
			<xsl:attribute name="id">
				<xsl:text>nc</xsl:text>
				<xsl:value-of select="@Id"/>
			</xsl:attribute>
			<Name>
				<xsl:value-of select="Abbreviation"/>
			</Name>
			<xsl:apply-templates select="Features/FsFeatStruc" mode="phonological"/>
		</FeatureNaturalClass>
	</xsl:template>

	<!-- Morphological Rules -->

	<xsl:template name="DefaultCompoundRules">
		<CompoundingRule stratum="morphophonemic" id="compLeft">
			<Name>Default Left Head Compounding</Name>
			<CompoundSubrules>
				<CompoundSubruleStructure id="csubruleLeft">
					<HeadRecordStructure>
						<RequiredPhoneticInput>
							<PhoneticSequence id="pseqLeftHead">
								<xsl:call-template name="AnySeq"/>
							</PhoneticSequence>
						</RequiredPhoneticInput>
					</HeadRecordStructure>
					<NonHeadRecordStructure>
						<RequiredPhoneticInput>
							<PhoneticSequence id="pseqRightNonHead">
								<xsl:call-template name="AnySeq"/>
							</PhoneticSequence>
						</RequiredPhoneticInput>
					</NonHeadRecordStructure>
					<OutputSideRecordStructure>
						<MorphologicalPhoneticOutput id="mpoLeftHead">
							<CopyFromInput index="pseqLeftHead"/>
							<InsertSegments>
								<xsl:attribute name="characterTable">
									<xsl:text>table</xsl:text>
									<xsl:value-of select="$charTable/@Id"/>
								</xsl:attribute>
								<PhoneticShape>+</PhoneticShape>
							</InsertSegments>
							<CopyFromInput index="pseqRightNonHead"/>
						</MorphologicalPhoneticOutput>
					</OutputSideRecordStructure>
				</CompoundSubruleStructure>
			</CompoundSubrules>
		</CompoundingRule>
		<CompoundingRule stratum="morphophonemic" id="compRight">
			<Name>Default Right Head Compounding</Name>
			<CompoundSubrules>
				<CompoundSubruleStructure id="csubruleRight">
					<HeadRecordStructure>
						<RequiredPhoneticInput>
							<PhoneticSequence id="pseqRightHead">
								<xsl:call-template name="AnySeq"/>
							</PhoneticSequence>
						</RequiredPhoneticInput>
					</HeadRecordStructure>
					<NonHeadRecordStructure>
						<RequiredPhoneticInput>
							<PhoneticSequence id="pseqLeftNonHead">
								<xsl:call-template name="AnySeq"/>
							</PhoneticSequence>
						</RequiredPhoneticInput>
					</NonHeadRecordStructure>
					<OutputSideRecordStructure>
						<MorphologicalPhoneticOutput id="mpoRightHead">
							<CopyFromInput index="pseqLeftNonHead"/>
							<InsertSegments>
								<xsl:attribute name="characterTable">
									<xsl:text>table</xsl:text>
									<xsl:value-of select="$charTable/@Id"/>
								</xsl:attribute>
								<PhoneticShape>+</PhoneticShape>
							</InsertSegments>
							<CopyFromInput index="pseqRightHead"/>
						</MorphologicalPhoneticOutput>
					</OutputSideRecordStructure>
				</CompoundSubruleStructure>
			</CompoundSubrules>
		</CompoundingRule>
	</xsl:template>

	<xsl:template match="MoExoCompound">
		<xsl:variable name="leftMsa" select="key('StemMsaId', LeftMsa/@dst)"/>
		<xsl:variable name="rightMsa" select="key('StemMsaId', RightMsa/@dst)"/>
		<xsl:variable name="toMsa" select="key('StemMsaId', ToMsa/@dst)"/>
		<CompoundingRule stratum="morphophonemic">
			<xsl:attribute name="id">
				<xsl:text>comp</xsl:text>
				<xsl:value-of select="@Id"/>
				<xsl:text>_R</xsl:text>
			</xsl:attribute>
			<xsl:if test="$rightMsa/@PartOfSpeech != 0">
				<xsl:attribute name="headPartsOfSpeech">
					<xsl:text>pos</xsl:text>
					<xsl:value-of select="$rightMsa/@PartOfSpeech"/>
					<xsl:call-template name="POSIds">
						<xsl:with-param name="posId" select="$rightMsa/@PartOfSpeech"/>
					</xsl:call-template>
				</xsl:attribute>
			</xsl:if>
			<xsl:if test="$leftMsa/@PartOfSpeech != 0">
				<xsl:attribute name="nonheadPartsOfSpeech">
					<xsl:text>pos</xsl:text>
					<xsl:value-of select="$leftMsa/@PartOfSpeech"/>
					<xsl:call-template name="POSIds">
						<xsl:with-param name="posId" select="$leftMsa/@PartOfSpeech"/>
					</xsl:call-template>
				</xsl:attribute>
			</xsl:if>
			<xsl:if test="$toMsa/@PartOfSpeech != 0">
				<xsl:attribute name="outputPartOfSpeech">
					<xsl:text>pos</xsl:text>
					<xsl:value-of select="$toMsa/@PartOfSpeech"/>
				</xsl:attribute>
			</xsl:if>
			<Name>
				<xsl:value-of select="Name"/>
			</Name>
			<CompoundSubrules>
				<CompoundSubruleStructure>
					<xsl:attribute name="id">
						<xsl:text>csubrule</xsl:text>
						<xsl:value-of select="@Id"/>
						<xsl:text>_R</xsl:text>
					</xsl:attribute>
					<HeadRecordStructure>
						<RequiredPhoneticInput>
							<PhoneticSequence>
								<xsl:attribute name="id">
									<xsl:text>pseq</xsl:text>
									<xsl:value-of select="@Id"/>
									<xsl:text>_R_R</xsl:text>
								</xsl:attribute>
								<xsl:call-template name="AnySeq"/>
							</PhoneticSequence>
						</RequiredPhoneticInput>
					</HeadRecordStructure>
					<NonHeadRecordStructure>
						<RequiredPhoneticInput>
							<PhoneticSequence>
								<xsl:attribute name="id">
									<xsl:text>pseq</xsl:text>
									<xsl:value-of select="@Id"/>
									<xsl:text>_R_L</xsl:text>
								</xsl:attribute>
								<xsl:call-template name="AnySeq"/>
							</PhoneticSequence>
						</RequiredPhoneticInput>
					</NonHeadRecordStructure>
					<OutputSideRecordStructure>
						<xsl:if test="$toMsa/@InflectionClass != 0">
							<xsl:attribute name="MPRFeatures">
								<xsl:text>mpr</xsl:text>
								<xsl:value-of select="$toMsa/@InflectionClass"/>
							</xsl:attribute>
						</xsl:if>
						<MorphologicalPhoneticOutput>
							<xsl:attribute name="id">
								<xsl:text>mpo</xsl:text>
								<xsl:value-of select="@Id"/>
								<xsl:text>_R</xsl:text>
							</xsl:attribute>
							<CopyFromInput>
								<xsl:attribute name="index">
									<xsl:text>pseq</xsl:text>
									<xsl:value-of select="@Id"/>
									<xsl:text>_R_L</xsl:text>
								</xsl:attribute>
							</CopyFromInput>
							<InsertSegments>
								<xsl:attribute name="characterTable">
									<xsl:text>table</xsl:text>
									<xsl:value-of select="$charTable/@Id"/>
								</xsl:attribute>
								<PhoneticShape>+</PhoneticShape>
							</InsertSegments>
							<CopyFromInput>
								<xsl:attribute name="index">
									<xsl:text>pseq</xsl:text>
									<xsl:value-of select="@Id"/>
									<xsl:text>_R_R</xsl:text>
								</xsl:attribute>
							</CopyFromInput>
						</MorphologicalPhoneticOutput>
					</OutputSideRecordStructure>
				</CompoundSubruleStructure>
			</CompoundSubrules>
		</CompoundingRule>
		<CompoundingRule stratum="morphophonemic">
			<xsl:attribute name="id">
				<xsl:text>comp</xsl:text>
				<xsl:value-of select="@Id"/>
				<xsl:text>_L</xsl:text>
			</xsl:attribute>
			<xsl:if test="$rightMsa/@PartOfSpeech != 0">
				<xsl:attribute name="headPartsOfSpeech">
					<xsl:text>pos</xsl:text>
					<xsl:value-of select="$leftMsa/@PartOfSpeech"/>
					<xsl:call-template name="POSIds">
						<xsl:with-param name="posId" select="$leftMsa/@PartOfSpeech"/>
					</xsl:call-template>
				</xsl:attribute>
			</xsl:if>
			<xsl:if test="$leftMsa/@PartOfSpeech != 0">
				<xsl:attribute name="nonheadPartsOfSpeech">
					<xsl:text>pos</xsl:text>
					<xsl:value-of select="$rightMsa/@PartOfSpeech"/>
					<xsl:call-template name="POSIds">
						<xsl:with-param name="posId" select="$rightMsa/@PartOfSpeech"/>
					</xsl:call-template>
				</xsl:attribute>
			</xsl:if>
			<xsl:if test="$toMsa/@PartOfSpeech != 0">
				<xsl:attribute name="outputPartOfSpeech">
					<xsl:text>pos</xsl:text>
					<xsl:value-of select="$toMsa/@PartOfSpeech"/>
				</xsl:attribute>
			</xsl:if>
			<Name>
				<xsl:value-of select="Name"/>
			</Name>
			<CompoundSubrules>
				<CompoundSubruleStructure>
					<xsl:attribute name="id">
						<xsl:text>csubrule</xsl:text>
						<xsl:value-of select="@Id"/>
						<xsl:text>_L</xsl:text>
					</xsl:attribute>
					<HeadRecordStructure>
						<RequiredPhoneticInput>
							<PhoneticSequence>
								<xsl:attribute name="id">
									<xsl:text>pseq</xsl:text>
									<xsl:value-of select="@Id"/>
									<xsl:text>_L_L</xsl:text>
								</xsl:attribute>
								<xsl:call-template name="AnySeq"/>
							</PhoneticSequence>
						</RequiredPhoneticInput>
					</HeadRecordStructure>
					<NonHeadRecordStructure>
						<RequiredPhoneticInput>
							<PhoneticSequence>
								<xsl:attribute name="id">
									<xsl:text>pseq</xsl:text>
									<xsl:value-of select="@Id"/>
									<xsl:text>_L_R</xsl:text>
								</xsl:attribute>
								<xsl:call-template name="AnySeq"/>
							</PhoneticSequence>
						</RequiredPhoneticInput>
					</NonHeadRecordStructure>
					<OutputSideRecordStructure>
						<xsl:if test="$toMsa/@InflectionClass != 0">
							<xsl:attribute name="MPRFeatures">
								<xsl:text>mpr</xsl:text>
								<xsl:value-of select="$toMsa/@InflectionClass"/>
							</xsl:attribute>
						</xsl:if>
						<MorphologicalPhoneticOutput>
							<xsl:attribute name="id">
								<xsl:text>mpo</xsl:text>
								<xsl:value-of select="@Id"/>
								<xsl:text>_L</xsl:text>
							</xsl:attribute>
							<CopyFromInput>
								<xsl:attribute name="index">
									<xsl:text>pseq</xsl:text>
									<xsl:value-of select="@Id"/>
									<xsl:text>_L_L</xsl:text>
								</xsl:attribute>
							</CopyFromInput>
							<InsertSegments>
								<xsl:attribute name="characterTable">
									<xsl:text>table</xsl:text>
									<xsl:value-of select="$charTable/@Id"/>
								</xsl:attribute>
								<PhoneticShape>+</PhoneticShape>
							</InsertSegments>
							<CopyFromInput>
								<xsl:attribute name="index">
									<xsl:text>pseq</xsl:text>
									<xsl:value-of select="@Id"/>
									<xsl:text>_L_R</xsl:text>
								</xsl:attribute>
							</CopyFromInput>
						</MorphologicalPhoneticOutput>
					</OutputSideRecordStructure>
				</CompoundSubruleStructure>
			</CompoundSubrules>
		</CompoundingRule>
	</xsl:template>

	<xsl:template match="MoEndoCompound">
		<xsl:variable name="leftMsa" select="key('StemMsaId', LeftMsa/@dst)"/>
		<xsl:variable name="rightMsa" select="key('StemMsaId', RightMsa/@dst)"/>
		<xsl:variable name="overridingMsa" select="key('StemMsaId', OverridingMsa/@dst)"/>
		<CompoundingRule stratum="morphophonemic">
			<xsl:attribute name="id">
				<xsl:text>comp</xsl:text>
				<xsl:value-of select="@Id"/>
			</xsl:attribute>
			<xsl:choose>
				<xsl:when test="@HeadLast = 1">
					<xsl:if test="$rightMsa/@PartOfSpeech != 0">
						<xsl:attribute name="headPartsOfSpeech">
							<xsl:text>pos</xsl:text>
							<xsl:value-of select="$rightMsa/@PartOfSpeech"/>
							<xsl:call-template name="POSIds">
								<xsl:with-param name="posId" select="$rightMsa/@PartOfSpeech"/>
							</xsl:call-template>
						</xsl:attribute>
					</xsl:if>
				</xsl:when>
				<xsl:otherwise>
					<xsl:if test="$leftMsa/@PartOfSpeech != 0">
						<xsl:attribute name="headPartsOfSpeech">
							<xsl:text>pos</xsl:text>
							<xsl:value-of select="$leftMsa/@PartOfSpeech"/>
							<xsl:call-template name="POSIds">
								<xsl:with-param name="posId" select="$leftMsa/@PartOfSpeech"/>
							</xsl:call-template>
						</xsl:attribute>
					</xsl:if>
				</xsl:otherwise>
			</xsl:choose>
			<xsl:choose>
				<xsl:when test="@HeadLast = 1">
					<xsl:if test="$leftMsa/@PartOfSpeech != 0">
						<xsl:attribute name="nonheadPartsOfSpeech">
							<xsl:text>pos</xsl:text>
							<xsl:value-of select="$leftMsa/@PartOfSpeech"/>
							<xsl:call-template name="POSIds">
								<xsl:with-param name="posId" select="$leftMsa/@PartOfSpeech"/>
							</xsl:call-template>
						</xsl:attribute>
					</xsl:if>
				</xsl:when>
				<xsl:otherwise>
					<xsl:if test="$rightMsa/@PartOfSpeech != 0">
						<xsl:attribute name="nonheadPartsOfSpeech">
							<xsl:text>pos</xsl:text>
							<xsl:value-of select="$rightMsa/@PartOfSpeech"/>
							<xsl:call-template name="POSIds">
								<xsl:with-param name="posId" select="$rightMsa/@PartOfSpeech"/>
							</xsl:call-template>
						</xsl:attribute>
					</xsl:if>
				</xsl:otherwise>
			</xsl:choose>
			<xsl:if test="$overridingMsa/@PartOfSpeech != 0">
				<xsl:attribute name="outputPartOfSpeech">
					<xsl:text>pos</xsl:text>
					<xsl:value-of select="$overridingMsa/@PartOfSpeech"/>
				</xsl:attribute>
			</xsl:if>
			<Name>
				<xsl:value-of select="Name"/>
			</Name>
			<CompoundSubrules>
				<CompoundSubruleStructure>
					<xsl:attribute name="id">
						<xsl:text>csubrule</xsl:text>
						<xsl:value-of select="@Id"/>
					</xsl:attribute>
					<HeadRecordStructure>
						<RequiredPhoneticInput>
							<PhoneticSequence>
								<xsl:attribute name="id">
									<xsl:text>pseq</xsl:text>
									<xsl:value-of select="@Id"/>
									<xsl:text>_H</xsl:text>
								</xsl:attribute>
								<xsl:call-template name="AnySeq"/>
							</PhoneticSequence>
						</RequiredPhoneticInput>
					</HeadRecordStructure>
					<NonHeadRecordStructure>
						<RequiredPhoneticInput>
							<PhoneticSequence>
								<xsl:attribute name="id">
									<xsl:text>pseq</xsl:text>
									<xsl:value-of select="@Id"/>
									<xsl:text>_NH</xsl:text>
								</xsl:attribute>
								<xsl:call-template name="AnySeq"/>
							</PhoneticSequence>
						</RequiredPhoneticInput>
					</NonHeadRecordStructure>
					<OutputSideRecordStructure>
						<xsl:if test="$overridingMsa/@InflectionClass != 0">
							<xsl:attribute name="MPRFeatures">
								<xsl:text>mpr</xsl:text>
								<xsl:value-of select="$overridingMsa/@InflectionClass"/>
							</xsl:attribute>
						</xsl:if>
						<MorphologicalPhoneticOutput>
							<xsl:attribute name="id">
								<xsl:text>mpo</xsl:text>
								<xsl:value-of select="@Id"/>
							</xsl:attribute>
							<CopyFromInput>
								<xsl:attribute name="index">
									<xsl:text>pseq</xsl:text>
									<xsl:value-of select="@Id"/>
									<xsl:choose>
										<xsl:when test="@HeadLast = 1">
											<xsl:text>_NH</xsl:text>
										</xsl:when>
										<xsl:otherwise>
											<xsl:text>_H</xsl:text>
										</xsl:otherwise>
									</xsl:choose>
								</xsl:attribute>
							</CopyFromInput>
							<InsertSegments>
								<xsl:attribute name="characterTable">
									<xsl:text>table</xsl:text>
									<xsl:value-of select="$charTable/@Id"/>
								</xsl:attribute>
								<PhoneticShape>+</PhoneticShape>
							</InsertSegments>
							<CopyFromInput>
								<xsl:attribute name="index">
									<xsl:text>pseq</xsl:text>
									<xsl:value-of select="@Id"/>
									<xsl:choose>
										<xsl:when test="@HeadLast = 1">
											<xsl:text>_H</xsl:text>
										</xsl:when>
										<xsl:otherwise>
											<xsl:text>_NH</xsl:text>
										</xsl:otherwise>
									</xsl:choose>
								</xsl:attribute>
							</CopyFromInput>
						</MorphologicalPhoneticOutput>
					</OutputSideRecordStructure>
				</CompoundSubruleStructure>
			</CompoundSubrules>
		</CompoundingRule>
	</xsl:template>

	<xsl:template name="MorphologicalRules">
		<xsl:for-each select="$entries/LexEntry">
			<xsl:variable name="valid">
				<xsl:call-template name="HasValidRuleForm">
					<xsl:with-param name="entry" select="."/>
				</xsl:call-template>
			</xsl:variable>
			<xsl:if test="string-length($valid) != 0">
				<xsl:for-each select="MorphoSyntaxAnalysis">
					<xsl:apply-templates select="key('MsaId', @dst)">
						<xsl:with-param name="entry" select=".."/>
					</xsl:apply-templates>
				</xsl:for-each>
			</xsl:if>
		</xsl:for-each>
	</xsl:template>

	<xsl:template match="MoDerivAffMsa">
		<xsl:param name="entry"/>
		<MorphologicalRule stratum="morphophonemic">
			<xsl:attribute name="id">
				<xsl:text>mrule</xsl:text>
				<xsl:value-of select="@Id"/>
			</xsl:attribute>
			<xsl:if test="@FromPartOfSpeech != 0">
				<xsl:attribute name="requiredPartsOfSpeech">
					<xsl:text>pos</xsl:text>
					<xsl:value-of select="@FromPartOfSpeech"/>
					<xsl:call-template name="POSIds">
						<xsl:with-param name="posId" select="@FromPartOfSpeech"/>
					</xsl:call-template>
				</xsl:attribute>
			</xsl:if>
			<xsl:if test="@ToPartOfSpeech != 0">
				<xsl:attribute name="outputPartOfSpeech">
					<xsl:text>pos</xsl:text>
					<xsl:value-of select="@ToPartOfSpeech"/>
				</xsl:attribute>
			</xsl:if>
			<Name>
				<xsl:value-of select="key('AlloId', $entry/LexemeForm/@dst)/Form"/>
			</Name>
			<xsl:call-template name="Gloss">
				<xsl:with-param name="sense" select="key('LexSenseId', $entry/Sense[1]/@dst)"/>
				<xsl:with-param name="id" select="@Id"/>
			</xsl:call-template>

			<xsl:variable name="outHeadFeats" select="ToMsFeatures/FsFeatStruc[node()]"/>
			<xsl:variable name="reqHeadFeats" select="FromMsFeatures/FsFeatStruc[node()]"/>
			<xsl:call-template name="MorphologicalSubrules">
				<xsl:with-param name="entry" select="$entry"/>
				<xsl:with-param name="msa" select="."/>
				<xsl:with-param name="reqMPRFeats">
					<xsl:if test="@FromInflectionClass != 0">
						<xsl:text>mpr</xsl:text>
						<xsl:value-of select="@FromInflectionClass"/>
						<xsl:call-template name="InflClassIds">
							<xsl:with-param name="inflClass" select="key('InflClassId', @FromInflectionClass)"/>
						</xsl:call-template>
						<xsl:text> </xsl:text>
					</xsl:if>
					<xsl:for-each select="FromProdRestrict">
						<xsl:text>mpr</xsl:text>
						<xsl:value-of select="@dst"/>
						<xsl:text> </xsl:text>
					</xsl:for-each>
				</xsl:with-param>
				<xsl:with-param name="outMPRFeats">
					<xsl:if test="@ToInflectionClass != 0">
						<xsl:text>mpr</xsl:text>
						<xsl:value-of select="@ToInflectionClass"/>
						<xsl:text> </xsl:text>
					</xsl:if>
					<xsl:for-each select="ToProdRestrict">
						<xsl:text>mpr</xsl:text>
						<xsl:value-of select="@dst"/>
						<xsl:if test="position() != last()">
							<xsl:text> </xsl:text>
						</xsl:if>
					</xsl:for-each>
				</xsl:with-param>
				<xsl:with-param name="featDesc">
					<xsl:if test="@FromPartOfSpeech != 0">
						<xsl:value-of select="$sFromPOS"/>
						<xsl:value-of select="@FromPartOfSpeech"/>
						<xsl:text> </xsl:text>
					</xsl:if>
					<xsl:if test="@ToPartOfSpeech != 0">
						<xsl:value-of select="$sToPOS"/>
						<xsl:value-of select="@ToPartOfSpeech"/>
						<xsl:text> </xsl:text>
					</xsl:if>
					<xsl:if test="@FromInflectionClass != 0">
						<xsl:value-of select="$sFromInflClass"/>
						<xsl:value-of select="@FromInflectionClass"/>
						<xsl:text> </xsl:text>
					</xsl:if>
					<xsl:if test="@ToInflectionClass != 0">
						<xsl:value-of select="$sToInflClass"/>
						<xsl:value-of select="@ToInflectionClass"/>
						<xsl:text> </xsl:text>
					</xsl:if>
					<xsl:for-each select="FromProdRestrict">
						<xsl:value-of select="$sFromExceptionFeature"/>
						<xsl:value-of select="@dst"/>
						<xsl:text>Plus </xsl:text>
					</xsl:for-each>
					<xsl:for-each select="ToProdRestrict">
						<xsl:value-of select="$sToExceptionFeature"/>
						<xsl:value-of select="@dst"/>
						<xsl:text>Plus </xsl:text>
					</xsl:for-each>
					<xsl:if test="@FromStemName != '0'">
						<xsl:value-of select="$sStemName"/>
						<xsl:value-of select="@FromStemName"/>
						<xsl:text> </xsl:text>
					</xsl:if>
					<xsl:if test="count($reqHeadFeats) != 0">
						<xsl:value-of select="$sFromMSFS"/>
						<xsl:value-of select="$reqHeadFeats/@Id"/>
						<xsl:text> </xsl:text>
					</xsl:if>
					<xsl:if test="count($outHeadFeats) != 0">
						<xsl:value-of select="$sToMSFS"/>
						<xsl:value-of select="$outHeadFeats/@Id"/>
					</xsl:if>
				</xsl:with-param>
			</xsl:call-template>

			<xsl:if test="count($outHeadFeats) != 0">
				<OutputHeadFeatures>
					<xsl:apply-templates select="$outHeadFeats" mode="morphosyntactic"/>
				</OutputHeadFeatures>
			</xsl:if>
			<xsl:if test="count($reqHeadFeats) != 0">
				<RequiredHeadFeatures>
					<xsl:apply-templates select="$reqHeadFeats" mode="morphosyntactic"/>
				</RequiredHeadFeatures>
			</xsl:if>
			<xsl:call-template name="AdhocMorphRules">
				<xsl:with-param name="msaId" select="@Id"/>
			</xsl:call-template>
		</MorphologicalRule>
	</xsl:template>

	<xsl:template match="MoInflAffMsa">
		<xsl:param name="entry"/>
		<MorphologicalRule stratum="morphophonemic">
			<xsl:attribute name="id">
				<xsl:text>mrule</xsl:text>
				<xsl:value-of select="@Id"/>
			</xsl:attribute>
			<xsl:if test="@PartOfSpeech != 0">
				<xsl:attribute name="requiredPartsOfSpeech">
					<xsl:text>pos</xsl:text>
					<xsl:value-of select="@PartOfSpeech"/>
					<xsl:call-template name="POSIds">
						<xsl:with-param name="posId" select="@PartOfSpeech"/>
					</xsl:call-template>
				</xsl:attribute>
			</xsl:if>
			<Name>
				<xsl:value-of select="key('AlloId', $entry/LexemeForm/@dst)/Form"/>
			</Name>
			<xsl:call-template name="Gloss">
				<xsl:with-param name="sense" select="key('LexSenseId', $entry/Sense[1]/@dst)"/>
				<xsl:with-param name="id" select="@Id"/>
			</xsl:call-template>

			<xsl:variable name="realFeats" select="InflectionFeatures/FsFeatStruc[node()]"/>
			<xsl:variable name="stemName">
				<xsl:call-template name="InflAffMsaStemName">
					<xsl:with-param name="fs" select="$realFeats"/>
					<xsl:with-param name="pos" select="key('POSId', @PartOfSpeech)"/>
				</xsl:call-template>
			</xsl:variable>
			<xsl:variable name="alloIds" select="$entry/AlternateForms/@dst | $entry/LexemeForm/@dst"/>
			<xsl:variable name="allos" select="$allomorphs/MoAffixAllomorph[@Id = $alloIds and @MorphType != '0' and string-length(Form) != 0 and @IsAbstract = '0'] | $allomorphs/MoAffixProcess[@Id = $alloIds]"/>
			<xsl:variable name="useSubInflClass">
				<xsl:for-each select="$allos">
					<xsl:for-each select="InflectionClasses">
						<xsl:if test="name(key('InflClassId', @dst)/..) = 'Subclasses'">
							<xsl:text>N</xsl:text>
						</xsl:if>
					</xsl:for-each>
				</xsl:for-each>
				<xsl:text>Y</xsl:text>
			</xsl:variable>
			<xsl:call-template name="MorphologicalSubrules">
				<xsl:with-param name="entry" select="$entry"/>
				<xsl:with-param name="msa" select="."/>
				<xsl:with-param name="useSubInflClass" select="$useSubInflClass"/>
				<xsl:with-param name="reqMPRFeats">
					<xsl:for-each select="FromProdRestrict">
						<xsl:text>mpr</xsl:text>
						<xsl:value-of select="@dst"/>
						<xsl:text> </xsl:text>
					</xsl:for-each>
				</xsl:with-param>
				<xsl:with-param name="featDesc">
					<xsl:if test="@FromPartOfSpeech != 0">
						<xsl:value-of select="$sFromPOS"/>
						<xsl:value-of select="@PartOfSpeech"/>
						<xsl:text> </xsl:text>
					</xsl:if>
					<xsl:for-each select="FromProdRestrict">
						<xsl:value-of select="$sFromExceptionFeature"/>
						<xsl:value-of select="@dst"/>
						<xsl:text>Plus </xsl:text>
					</xsl:for-each>
					<xsl:if test="string-length(normalize-space($stemName)) != 0">
						<xsl:value-of select="normalize-space($stemName)"/>
						<xsl:text> </xsl:text>
					</xsl:if>
					<xsl:if test="count($realFeats) != 0">
						<xsl:value-of select="$sInflectionFS"/>
						<xsl:value-of select="$realFeats/@Id"/>
					</xsl:if>
				</xsl:with-param>
			</xsl:call-template>
			<xsl:if test="count($realFeats) != 0">
				<RequiredHeadFeatures>
					<xsl:apply-templates select="$realFeats" mode="morphosyntactic"/>
				</RequiredHeadFeatures>
			</xsl:if>
			<xsl:call-template name="AdhocMorphRules">
				<xsl:with-param name="msaId" select="@Id"/>
			</xsl:call-template>
		</MorphologicalRule>
	</xsl:template>

	<xsl:template name="InflAffMsaStemName">
		<xsl:param name="fs"/>
		<xsl:param name="pos"/>
		<xsl:for-each select="$pos/StemNames/MoStemName">
			<xsl:variable name="stemName" select="."/>
			<xsl:variable name="bHasFeatureInRegion">
				<xsl:for-each select="$fs/descendant::FsClosedValue">
					<xsl:variable name="sFeature" select="@Feature"/>
					<xsl:variable name="sValue" select="@Value"/>
					<xsl:if test="$stemName/Regions/descendant::FsClosedValue[@Feature=$sFeature and @Value=$sValue]">Y</xsl:if>
				</xsl:for-each>
			</xsl:variable>
			<xsl:if test="contains($bHasFeatureInRegion, 'Y')">
				<xsl:text> </xsl:text>
				<xsl:value-of select="$sStemName"/>
				<xsl:text>Affix</xsl:text>
				<xsl:value-of select="@Id"/>
			</xsl:if>
		</xsl:for-each>
		<!-- now check parent POS -->
		<xsl:for-each select="$POSs/PartOfSpeech/SubPossibilities[@dst = $pos/@Id]">
			<xsl:call-template name="InflAffMsaStemName">
				<xsl:with-param name="fs" select="$fs"/>
				<xsl:with-param name="pos" select=".."/>
			</xsl:call-template>
		</xsl:for-each>
	</xsl:template>

	<xsl:template match="MoStemMsa">
		<xsl:param name="entry"/>
		<MorphologicalRule stratum="clitic">
			<xsl:attribute name="id">
				<xsl:text>mrule</xsl:text>
				<xsl:value-of select="@Id"/>
			</xsl:attribute>
			<xsl:variable name="fromPOSs" select="FromPartsOfSpeech/FromPOS"/>
			<xsl:if test="count($fromPOSs) != 0">
				<xsl:attribute name="requiredPartsOfSpeech">
					<xsl:for-each select="$fromPOSs">
						<xsl:if test="position()!=1">
							<xsl:text>&#x20;</xsl:text>
						</xsl:if>
						<xsl:text>pos</xsl:text>
						<xsl:value-of select="@dst"/>
						<xsl:call-template name="POSIds">
							<xsl:with-param name="posId" select="@dst"/>
						</xsl:call-template>
					</xsl:for-each>
				</xsl:attribute>
			</xsl:if>
			<Name>
				<xsl:value-of select="key('StemAlloId', $entry/LexemeForm/@dst)/Form"/>
			</Name>
			<xsl:call-template name="Gloss">
				<xsl:with-param name="sense" select="key('LexSenseId', $entry/Sense[1]/@dst)"/>
				<xsl:with-param name="id">
					<xsl:value-of select="@Id"/>
					<xsl:text>_RULE</xsl:text>
				</xsl:with-param>
			</xsl:call-template>
			<xsl:call-template name="MorphologicalSubrules">
				<xsl:with-param name="entry" select="$entry"/>
				<xsl:with-param name="msa" select="."/>
				<xsl:with-param name="featDesc">
					<xsl:if test="count($fromPOSs) != 0">
						<xsl:for-each select="$fromPOSs">
							<xsl:value-of select="$sCliticFromPOS"/>
							<xsl:value-of select="@dst"/>
							<xsl:if test="position() != last()">
								<xsl:text> </xsl:text>
							</xsl:if>
						</xsl:for-each>
					</xsl:if>
					<xsl:variable name="headFeats" select="InflectionFeatures/FsFeatStruc[node()]"/>
					<xsl:if test="count($headFeats) != 0">
						<xsl:text> </xsl:text>
						<xsl:value-of select="$sMSFS"/>
						<xsl:value-of select="$headFeats/@Id"/>
					</xsl:if>
				</xsl:with-param>
			</xsl:call-template>
			<xsl:call-template name="AdhocMorphRules">
				<xsl:with-param name="msaId" select="@Id"/>
			</xsl:call-template>
		</MorphologicalRule>
	</xsl:template>

	<xsl:template match="MoUnclassifiedAffixMsa">
		<xsl:param name="entry"/>
		<MorphologicalRule stratum="morphophonemic">
			<xsl:attribute name="id">
				<xsl:text>mrule</xsl:text>
				<xsl:value-of select="@Id"/>
			</xsl:attribute>
			<xsl:if test="@PartOfSpeech != 0">
				<xsl:attribute name="requiredPartsOfSpeech">
					<xsl:text>pos</xsl:text>
					<xsl:value-of select="@PartOfSpeech"/>
					<xsl:call-template name="POSIds">
						<xsl:with-param name="posId" select="@PartOfSpeech"/>
					</xsl:call-template>
				</xsl:attribute>
			</xsl:if>
			<Name>
				<xsl:value-of select="key('AlloId', $entry/LexemeForm/@dst)/Form"/>
			</Name>
			<xsl:call-template name="Gloss">
				<xsl:with-param name="sense" select="key('LexSenseId', $entry/Sense[1]/@dst)"/>
				<xsl:with-param name="id" select="@Id"/>
			</xsl:call-template>
			<xsl:call-template name="MorphologicalSubrules">
				<xsl:with-param name="entry" select="$entry"/>
				<xsl:with-param name="msa" select="."/>
				<xsl:with-param name="featDesc">
					<xsl:if test="@PartOfSpeech != 0">
						<xsl:value-of select="$sFromPOS"/>
						<xsl:value-of select="@PartOfSpeech"/>
					</xsl:if>
				</xsl:with-param>
			</xsl:call-template>
			<xsl:call-template name="AdhocMorphRules">
				<xsl:with-param name="msaId" select="@Id"/>
			</xsl:call-template>
		</MorphologicalRule>
	</xsl:template>

	<xsl:template match="Slots">
		<xsl:variable name="slot" select="key('SlotId',@dst)"/>
		<xsl:if test="$slot/@Optional='false'">
			<!-- for each required slot, create a null entry so the template will still pass -->
			<xsl:variable name="sNullId">
				<xsl:value-of select="../@Id"/>
				<xsl:text>_</xsl:text>
				<xsl:value-of select="@dst"/>
			</xsl:variable>
			<MorphologicalRule stratum="morphophonemic">
				<xsl:attribute name="id">
					<xsl:text>mrule</xsl:text>
					<xsl:value-of select="$sNullId"/>
				</xsl:attribute>
				<xsl:attribute name="requiredPartsOfSpeech">
					<xsl:text>pos</xsl:text>
					<xsl:value-of select="$slot/../../@Id"/>
					<xsl:call-template name="POSIds">
						<xsl:with-param name="posId" select="@PartOfSpeech"/>
					</xsl:call-template>
				</xsl:attribute>
				<xsl:variable name="sNameAndGloss">
					<xsl:text>IrregInflForm</xsl:text>
					<xsl:value-of select="../@Id"/>
					<xsl:text>InSlot</xsl:text>
					<xsl:value-of select="@dst"/>
				</xsl:variable>
				<Name>
					<xsl:value-of select="$sNameAndGloss"/>
				</Name>
				<Gloss id="gl{$sNullId}_AutoGenerated">
					<xsl:value-of select="$sNameAndGloss"/>
				</Gloss>
				<xsl:variable name="sMorphBdryRep">
					<xsl:text>rep</xsl:text>
					<xsl:value-of select="$morphBdry/@Id"/>
				</xsl:variable>
				<xsl:variable name="fIsPrefix">
					<xsl:choose>
						<xsl:when test="key('PrefixSlotsID',@dst)">
							<xsl:text>Y</xsl:text>
						</xsl:when>
						<xsl:otherwise>
							<xsl:text>N</xsl:text>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:variable>
				<MorphologicalSubrules>
					<MorphologicalSubruleStructure id="msubrule{$sNullId}">
						<InputSideRecordStructure requiredMPRFeatures="mpr{../@Id}">
							<RequiredPhoneticInput>
								<PhoneticSequence id="pseq{$sNullId}">
									<OptionalSegmentSequence min="0" max="-1">
										<BoundaryMarker representation="repNull" characterTable="table{$charTable/@Id}"/>
										<BoundaryMarker characterTable="table{$charTable/@Id}" representation="{$sMorphBdryRep}"/>
									</OptionalSegmentSequence>
									<OptionalSegmentSequence min="0" max="-1">
										<SimpleContext naturalClass="ncAny"/>
									</OptionalSegmentSequence>
									<OptionalSegmentSequence min="0" max="-1">
										<BoundaryMarker characterTable="table{$charTable/@Id}" representation="{$sMorphBdryRep}"/>
										<BoundaryMarker representation="repNull" characterTable="table{$charTable/@Id}"/>
									</OptionalSegmentSequence>
								</PhoneticSequence>
							</RequiredPhoneticInput>
						</InputSideRecordStructure>
						<OutputSideRecordStructure>
							<MorphologicalPhoneticOutput id="mpo{$sNullId}">
								<CopyFromInput index="pseq{$sNullId}"/>
								<InsertSegments characterTable="table{$charTable/@Id}">
									<PhoneticShape>
										<xsl:choose>
											<xsl:when test="$fIsPrefix='Y'">
												<xsl:text>^0+</xsl:text>
											</xsl:when>
											<xsl:otherwise>
												<xsl:text>+^0</xsl:text>
											</xsl:otherwise>
										</xsl:choose>
									</PhoneticShape>
								</InsertSegments>
							</MorphologicalPhoneticOutput>
						</OutputSideRecordStructure>
						<Properties>
							<Property name="WordCategory">
								<xsl:value-of select="@dst"/>
							</Property>
							<Property name="FormID">
								<xsl:value-of select="../@Id"/>
							</Property>
							<Property name="MsaID">
								<xsl:value-of select="../@Id"/>
							</Property>
							<Property name="FeatureDescriptors">
								<xsl:value-of select="$sFromExceptionFeature"/>
								<xsl:value-of select="@dst"/>
								<xsl:text>Plus</xsl:text>
							</Property>
							<Property name="MorphType">
								<xsl:choose>
									<xsl:when test="$fIsPrefix='Y'">
										<xsl:value-of select="$sPrefix"/>
									</xsl:when>
									<xsl:otherwise>
										<xsl:value-of select="$sSuffix"/>
									</xsl:otherwise>
								</xsl:choose>
							</Property>
						</Properties>
					</MorphologicalSubruleStructure>
				</MorphologicalSubrules>
				<xsl:variable name="realFeats" select="../InflectionFeatures/FsFeatStruc[node()]"/>
				<xsl:if test="count($realFeats) != 0">
					<RequiredHeadFeatures>
						<xsl:apply-templates select="$realFeats" mode="morphosyntactic"/>
					</RequiredHeadFeatures>
				</xsl:if>
			</MorphologicalRule>
		</xsl:if>
	</xsl:template>

	<xsl:template name="MorphologicalSubrules">
		<xsl:param name="entry"/>
		<xsl:param name="msa"/>
		<xsl:param name="useSubInflClass"/>
		<xsl:param name="reqMPRFeats"/>
		<xsl:param name="outMPRFeats"/>
		<xsl:param name="featDesc"/>

		<xsl:variable name="allNotEnvFeats">
			<xsl:for-each select="$entry/AlternateForms | $entry/LexemeForm">
				<xsl:variable name="form" select="key('AlloId', @dst)"/>
				<xsl:variable name="valid">
					<xsl:call-template name="IsValidRuleForm">
						<xsl:with-param name="form" select="$form"/>
					</xsl:call-template>
				</xsl:variable>
				<xsl:if test="string-length($valid) != 0">
					<xsl:variable name="envFeats" select="$form/MsEnvFeatures/FsFeatStruc[node()]"/>
					<xsl:if test="count($envFeats) != 0">
						<xsl:text>Not</xsl:text>
						<xsl:value-of select="$envFeats/@Id"/>
					</xsl:if>
				</xsl:if>
			</xsl:for-each>
		</xsl:variable>
		<MorphologicalSubrules>
			<xsl:variable name="morphType" select="key('MorphTypeId', $entry/LexemeForm/@MorphType)/@Guid"/>
			<xsl:choose>
				<!-- Circumfix -->
				<xsl:when test="$morphType = $sCircumfix and not(boolean(key('AffixProcessId', $entry/LexemeForm/@dst)))">
					<xsl:for-each select="$entry/AlternateForms">
						<xsl:sort select="@ord" data-type="number" order="ascending"/>
						<xsl:variable name="prefix" select="key('AlloId', @dst)"/>
						<xsl:variable name="preMorphType" select="key('MorphTypeId', $prefix/@MorphType)/@Guid"/>
						<xsl:if test="$preMorphType = $sPrefix and string-length($prefix/Form) != 0 and $prefix/@IsAbstract = '0'">
							<xsl:variable name="preEnvs" select="$prefix/PhoneEnv"/>
							<xsl:variable name="preHasBlankEnv">
								<xsl:call-template name="HasBlankEnv">
									<xsl:with-param name="envs" select="$preEnvs"/>
								</xsl:call-template>
							</xsl:variable>
							<xsl:variable name="newReqMPRFeats">
								<xsl:call-template name="AddInflClasses">
									<xsl:with-param name="reqMPRFeats" select="$reqMPRFeats"/>
									<xsl:with-param name="form" select="$prefix"/>
								</xsl:call-template>
							</xsl:variable>
							<xsl:for-each select="$entry/AlternateForms">
								<xsl:sort select="@ord" data-type="number" order="ascending"/>
								<xsl:variable name="suffix" select="key('AlloId', @dst)"/>
								<xsl:variable name="sufMorphType" select="key('MorphTypeId', $suffix/@MorphType)/@Guid"/>
								<xsl:if test="$sufMorphType = $sSuffix and string-length($suffix/Form) != 0 and $suffix/@IsAbstract = '0'">
									<xsl:variable name="sufEnvs" select="$suffix/PhoneEnv"/>
									<xsl:variable name="sufHasBlankEnv">
										<xsl:call-template name="HasBlankEnv">
											<xsl:with-param name="envs" select="$sufEnvs"/>
										</xsl:call-template>
									</xsl:variable>
									<xsl:for-each select="$preEnvs">
										<xsl:if test="boolean(key('EnvId', @dst))">
											<xsl:call-template name="CircumfixSuffixSubrules">
												<xsl:with-param name="msa" select="$msa"/>
												<xsl:with-param name="prefix" select="$prefix"/>
												<xsl:with-param name="preEnv" select="key('EnvId', @dst)"/>
												<xsl:with-param name="suffix" select="$suffix"/>
												<xsl:with-param name="sufEnvs" select="$sufEnvs"/>
												<xsl:with-param name="sufHasBlankEnv" select="$sufHasBlankEnv"/>
												<xsl:with-param name="reqMPRFeats" select="$newReqMPRFeats"/>
												<xsl:with-param name="outMPRFeats" select="$outMPRFeats"/>
												<xsl:with-param name="featDesc" select="$featDesc"/>
												<xsl:with-param name="allNotEnvFeats" select="$allNotEnvFeats"/>
											</xsl:call-template>
										</xsl:if>
									</xsl:for-each>
									<xsl:if test="string-length($preHasBlankEnv) != 0">
										<xsl:call-template name="CircumfixSuffixSubrules">
											<xsl:with-param name="msa" select="$msa"/>
											<xsl:with-param name="prefix" select="$prefix"/>
											<xsl:with-param name="suffix" select="$suffix"/>
											<xsl:with-param name="sufEnvs" select="$sufEnvs"/>
											<xsl:with-param name="sufHasBlankEnv" select="$sufHasBlankEnv"/>
											<xsl:with-param name="reqMPRFeats" select="$newReqMPRFeats"/>
											<xsl:with-param name="outMPRFeats" select="$outMPRFeats"/>
											<xsl:with-param name="featDesc" select="$featDesc"/>
											<xsl:with-param name="allNotEnvFeats" select="$allNotEnvFeats"/>
										</xsl:call-template>
									</xsl:if>
								</xsl:if>
							</xsl:for-each>
						</xsl:if>
					</xsl:for-each>
				</xsl:when>
				<!-- Prefix, suffix, infix, proclitic, enclitic -->
				<xsl:otherwise>
					<xsl:for-each select="$entry/AlternateForms">
						<xsl:sort select="@ord" data-type="number" order="ascending"/>
						<xsl:call-template name="AffixSubrules">
							<xsl:with-param name="msa" select="$msa"/>
							<xsl:with-param name="form" select="key('AlloId', @dst)"/>
							<xsl:with-param name="useSubInflClass" select="$useSubInflClass"/>
							<xsl:with-param name="reqMPRFeats" select="$reqMPRFeats"/>
							<xsl:with-param name="outMPRFeats" select="$outMPRFeats"/>
							<xsl:with-param name="featDesc" select="$featDesc"/>
							<xsl:with-param name="allNotEnvFeats" select="$allNotEnvFeats"/>
						</xsl:call-template>
					</xsl:for-each>
					<xsl:call-template name="AffixSubrules">
						<xsl:with-param name="msa" select="$msa"/>
						<xsl:with-param name="form" select="key('AlloId', $entry/LexemeForm/@dst)"/>
						<xsl:with-param name="useSubInflClass" select="$useSubInflClass"/>
						<xsl:with-param name="reqMPRFeats" select="$reqMPRFeats"/>
						<xsl:with-param name="outMPRFeats" select="$outMPRFeats"/>
						<xsl:with-param name="featDesc" select="$featDesc"/>
						<xsl:with-param name="allNotEnvFeats" select="$allNotEnvFeats"/>
					</xsl:call-template>
				</xsl:otherwise>
			</xsl:choose>
		</MorphologicalSubrules>
	</xsl:template>

	<xsl:template name="AffixSubrules">
		<xsl:param name="msa"/>
		<xsl:param name="form"/>
		<xsl:param name="useSubInflClass"/>
		<xsl:param name="reqMPRFeats"/>
		<xsl:param name="outMPRFeats"/>
		<xsl:param name="featDesc"/>
		<xsl:param name="allNotEnvFeats"/>
		<xsl:variable name="valid">
			<xsl:call-template name="IsValidRuleForm">
				<xsl:with-param name="form" select="$form"/>
			</xsl:call-template>
		</xsl:variable>
		<xsl:if test="string-length($valid) != 0">
			<xsl:variable name="newReqMPRFeats">
				<xsl:call-template name="AddInflClasses">
					<xsl:with-param name="reqMPRFeats" select="$reqMPRFeats"/>
					<xsl:with-param name="form" select="$form"/>
					<xsl:with-param name="useSubInflClass" select="$useSubInflClass"/>
				</xsl:call-template>
			</xsl:variable>
			<xsl:choose>
				<xsl:when test="name($form) = 'MoAffixProcess'">
					<xsl:call-template name="AffixProcessSubruleStructure">
						<xsl:with-param name="msa" select="$msa"/>
						<xsl:with-param name="form" select="$form"/>
						<xsl:with-param name="reqMPRFeats" select="$newReqMPRFeats"/>
						<xsl:with-param name="outMPRFeats" select="$outMPRFeats"/>
						<xsl:with-param name="featDesc" select="$featDesc"/>
					</xsl:call-template>
				</xsl:when>
				<xsl:otherwise>
					<xsl:variable name="envs" select="$form/PhoneEnv | $form/Position"/>
					<xsl:variable name="hasBlankEnv">
						<xsl:call-template name="HasBlankEnv">
							<xsl:with-param name="envs" select="$envs"/>
						</xsl:call-template>
					</xsl:variable>
					<xsl:for-each select="$envs">
						<xsl:sort select="@ord" data-type="number" order="ascending"/>
						<xsl:if test="boolean(key('EnvId', @dst))">
							<xsl:call-template name="AffixSubruleStructure">
								<xsl:with-param name="msa" select="$msa"/>
								<xsl:with-param name="form1" select="$form"/>
								<xsl:with-param name="env1" select="key('EnvId', @dst)"/>
								<xsl:with-param name="reqMPRFeats" select="$newReqMPRFeats"/>
								<xsl:with-param name="outMPRFeats" select="$outMPRFeats"/>
								<xsl:with-param name="featDesc" select="$featDesc"/>
								<xsl:with-param name="allNotEnvFeats" select="$allNotEnvFeats"/>
							</xsl:call-template>
						</xsl:if>
					</xsl:for-each>
					<xsl:if test="string-length($hasBlankEnv) != 0">
						<xsl:call-template name="AffixSubruleStructure">
							<xsl:with-param name="msa" select="$msa"/>
							<xsl:with-param name="form1" select="$form"/>
							<xsl:with-param name="reqMPRFeats" select="$newReqMPRFeats"/>
							<xsl:with-param name="outMPRFeats" select="$outMPRFeats"/>
							<xsl:with-param name="featDesc" select="$featDesc"/>
							<xsl:with-param name="allNotEnvFeats" select="$allNotEnvFeats"/>
						</xsl:call-template>
					</xsl:if>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:if>
	</xsl:template>

	<xsl:template name="CircumfixSuffixSubrules">
		<xsl:param name="msa"/>
		<xsl:param name="prefix"/>
		<xsl:param name="preEnv"/>
		<xsl:param name="suffix"/>
		<xsl:param name="sufEnvs"/>
		<xsl:param name="sufHasBlankEnv"/>
		<xsl:param name="reqMPRFeats"/>
		<xsl:param name="outMPRFeats"/>
		<xsl:param name="featDesc"/>
		<xsl:param name="allNotEnvFeats"/>

		<xsl:for-each select="$sufEnvs">
			<xsl:if test="boolean(key('EnvId', @dst))">
				<xsl:call-template name="AffixSubruleStructure">
					<xsl:with-param name="msa" select="$msa"/>
					<xsl:with-param name="form1" select="$prefix"/>
					<xsl:with-param name="form2" select="$suffix"/>
					<xsl:with-param name="env1" select="$preEnv"/>
					<xsl:with-param name="env2" select="key('EnvId', @dst)"/>
					<xsl:with-param name="reqMPRFeats" select="$reqMPRFeats"/>
					<xsl:with-param name="outMPRFeats" select="$outMPRFeats"/>
					<xsl:with-param name="featDesc" select="$featDesc"/>
					<xsl:with-param name="allNotEnvFeats" select="$allNotEnvFeats"/>
				</xsl:call-template>
			</xsl:if>
		</xsl:for-each>
		<xsl:if test="string-length($sufHasBlankEnv) != 0">
			<xsl:call-template name="AffixSubruleStructure">
				<xsl:with-param name="msa" select="$msa"/>
				<xsl:with-param name="form1" select="$prefix"/>
				<xsl:with-param name="form2" select="$suffix"/>
				<xsl:with-param name="env1" select="$preEnv"/>
				<xsl:with-param name="reqMPRFeats" select="$reqMPRFeats"/>
				<xsl:with-param name="outMPRFeats" select="$outMPRFeats"/>
				<xsl:with-param name="featDesc" select="$featDesc"/>
				<xsl:with-param name="allNotEnvFeats" select="$allNotEnvFeats"/>
			</xsl:call-template>
		</xsl:if>
	</xsl:template>

	<xsl:template name="AddInflClasses">
		<xsl:param name="reqMPRFeats"/>
		<xsl:param name="form"/>
		<xsl:param name="useSubInflClass"/>
		<xsl:variable name="inflClasses" select="$form/InflectionClasses"/>
		<xsl:value-of select="$reqMPRFeats"/>
		<xsl:if test="string-length($useSubInflClass) != 0">
			<xsl:if test="string-length($reqMPRFeats) != 0 and count($inflClasses) != 0">
				<xsl:text> </xsl:text>
			</xsl:if>
			<xsl:for-each select="$inflClasses">
				<xsl:text>mpr</xsl:text>
				<xsl:value-of select="@dst"/>
				<xsl:if test="$useSubInflClass = 'Y'">
					<xsl:call-template name="InflClassIds">
						<xsl:with-param name="inflClass" select="key('InflClassId', @dst)"/>
					</xsl:call-template>
				</xsl:if>
				<xsl:if test="position() != last()">
					<xsl:text> </xsl:text>
				</xsl:if>
			</xsl:for-each>
		</xsl:if>
	</xsl:template>

	<xsl:template name="Gloss">
		<xsl:param name="sense"/>
		<xsl:param name="id"/>
		<Gloss>
			<xsl:attribute name="id">
				<xsl:text>gl</xsl:text>
				<xsl:value-of select="$id"/>
				<xsl:text>_</xsl:text>
				<xsl:value-of select="$sense/@Id"/>
			</xsl:attribute>
			<xsl:value-of select="$sense/Gloss"/>
		</Gloss>
	</xsl:template>

	<xsl:template name="POSIds">
		<xsl:param name="posId"/>
		<xsl:for-each select="key('POSId', $posId)/SubPossibilities">
			<xsl:text> pos</xsl:text>
			<xsl:value-of select="@dst"/>
			<xsl:call-template name="POSIds">
				<xsl:with-param name="posId" select="@dst"/>
			</xsl:call-template>
		</xsl:for-each>
	</xsl:template>

	<xsl:template name="InflClassIds">
		<xsl:param name="inflClass"/>
		<xsl:for-each select="$inflClass/Subclasses/MoInflClass">
			<xsl:text> mpr</xsl:text>
			<xsl:value-of select="@Id"/>
			<xsl:call-template name="InflClassIds">
				<xsl:with-param name="inflClass" select="."/>
			</xsl:call-template>
		</xsl:for-each>
	</xsl:template>

	<xsl:template name="POSDefaultInflClass">
		<xsl:param name="pos"/>
		<xsl:choose>
			<xsl:when test="$pos/@DefaultInflectionClass != '0'">
				<xsl:value-of select="$pos/@DefaultInflectionClass"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:for-each select="$POSs/PartOfSpeech/SubPossibilities[@dst = $pos/@Id]">
					<xsl:call-template name="POSDefaultInflClass">
						<xsl:with-param name="pos" select=".."/>
					</xsl:call-template>
				</xsl:for-each>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template name="HasValidRuleForm">
		<xsl:param name="entry"/>
		<xsl:variable name="lexMorphType" select="key('MorphTypeId', $entry/LexemeForm/@MorphType)/@Guid"/>
		<xsl:choose>
			<!-- Circumfix -->
			<xsl:when test="$lexMorphType = $sCircumfix and not(boolean(key('AffixProcessId', $entry/LexemeForm/@dst)))">
				<xsl:variable name="hasPrefix">
					<xsl:for-each select="$entry/AlternateForms">
						<xsl:variable name="form" select="key('AlloId', @dst)"/>
						<xsl:variable name="morphType" select="key('MorphTypeId', $form/@MorphType)/@Guid"/>
						<xsl:if test="$morphType = $sPrefix and string-length($form/Form) != 0 and $form/@IsAbstract = '0'">
							<xsl:text>Y</xsl:text>
						</xsl:if>
					</xsl:for-each>
				</xsl:variable>
				<xsl:variable name="hasSuffix">
					<xsl:for-each select="$entry/AlternateForms">
						<xsl:variable name="form" select="key('AlloId', @dst)"/>
						<xsl:variable name="morphType" select="key('MorphTypeId', $form/@MorphType)/@Guid"/>
						<xsl:if test="$morphType = $sSuffix and string-length($form/Form) != 0 and $form/@IsAbstract = '0'">
							<xsl:text>Y</xsl:text>
						</xsl:if>
					</xsl:for-each>
				</xsl:variable>
				<xsl:if test="string-length($hasPrefix) != 0 and string-length($hasSuffix) != 0">
					<xsl:text>Y</xsl:text>
				</xsl:if>
			</xsl:when>
			<!-- Prefix, suffix, infix, proclitic, enclitic -->
			<xsl:otherwise>
				<xsl:for-each select="$entry/LexemeForm | $entry/AlternateForms">
					<xsl:call-template name="IsValidRuleForm">
						<xsl:with-param name="form" select="key('AlloId', @dst)"/>
					</xsl:call-template>
				</xsl:for-each>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template name="IsValidRuleForm">
		<xsl:param name="form"/>
		<xsl:variable name="morphType" select="key('MorphTypeId', $form/@MorphType)/@Guid"/>
		<xsl:choose>
			<xsl:when test="name($form) = 'MoAffixProcess'">
				<xsl:if test="count($form/Input/*) > 1 or count($form/Output/*) > 1">
					<xsl:text>Y</xsl:text>
				</xsl:if>
			</xsl:when>
			<xsl:when test="($morphType = $sPrefix or $morphType = $sEnclitic or $morphType = $sInfix or $morphType = $sInfixingInterfix or $morphType = $sPrefix or $morphType = $sPrefixingInterfix or $morphType = $sProclitic or $morphType = $sSuffix or $morphType = $sSuffixingInterfix) and string-length(normalize-space($form/Form)) != 0 and $form/@IsAbstract = '0'">
				<xsl:choose>
					<xsl:when test="contains($form/Form, '[') and not(contains($form/Form, '[...]'))">
						<xsl:if test="$morphType = $sPrefix or $morphType = $sPrefixingInterfix or $morphType = $sProclitic or $morphType = $sSuffix or $morphType = $sSuffixingInterfix or $morphType = $sEnclitic">
							<xsl:for-each select="$form/PhoneEnv">
								<xsl:if test="boolean(key('EnvId', @dst))">
									<xsl:text>Y</xsl:text>
								</xsl:if>
							</xsl:for-each>
						</xsl:if>
					</xsl:when>
					<xsl:when test="$morphType = $sInfix or $morphType = $sInfixingInterfix">
						<xsl:for-each select="$form/Position">
							<xsl:if test="boolean(key('EnvId', @dst))">
								<xsl:text>Y</xsl:text>
							</xsl:if>
						</xsl:for-each>
					</xsl:when>
					<xsl:otherwise>
						<xsl:text>Y</xsl:text>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:when>
		</xsl:choose>
	</xsl:template>

	<xsl:template name="HasBlankEnv">
		<xsl:param name="envs"/>
		<xsl:choose>
			<xsl:when test="count($envs) != 0">
				<xsl:for-each select="$envs">
					<xsl:if test="not(boolean(key('EnvId', @dst)))">
						<xsl:text>Y</xsl:text>
					</xsl:if>
				</xsl:for-each>
			</xsl:when>
			<xsl:otherwise>
				<xsl:text>Y</xsl:text>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<!-- Allomorph-based Affix Subrules -->

	<xsl:template name="AffixSubruleStructure">
		<xsl:param name="msa"/>
		<xsl:param name="form1"/>
		<xsl:param name="form2"/>
		<xsl:param name="env1"/>
		<xsl:param name="env2"/>
		<xsl:param name="reqMPRFeats"/>
		<xsl:param name="outMPRFeats"/>
		<xsl:param name="featDesc"/>
		<xsl:param name="allNotEnvFeats"/>
		<xsl:variable name="morphType" select="key('MorphTypeId', $form1/@MorphType)/@Guid"/>
		<xsl:variable name="id">
			<xsl:value-of select="$msa/@Id"/>
			<xsl:text>_</xsl:text>
			<xsl:value-of select="$form1/@Id"/>
			<xsl:if test="boolean($form2)">
				<xsl:text>_</xsl:text>
				<xsl:value-of select="$form2/@Id"/>
			</xsl:if>
			<xsl:if test="boolean($env1)">
				<xsl:text>_</xsl:text>
				<xsl:value-of select="$env1/@Id"/>
			</xsl:if>
			<xsl:if test="boolean($env2)">
				<xsl:text>_</xsl:text>
				<xsl:value-of select="$env2/@Id"/>
			</xsl:if>
		</xsl:variable>
		<xsl:variable name="envStrRep">
			<xsl:if test="boolean($env1)">
				<xsl:value-of select="$env1/@StringRepresentation"/>
			</xsl:if>
		</xsl:variable>
		<MorphologicalSubruleStructure>
			<xsl:attribute name="id">
				<xsl:text>msubrule</xsl:text>
				<xsl:value-of select="$id"/>
			</xsl:attribute>
			<InputSideRecordStructure>
				<xsl:if test="string-length(normalize-space($reqMPRFeats)) != 0">
					<xsl:attribute name="requiredMPRFeatures">
						<xsl:value-of select="normalize-space($reqMPRFeats)"/>
					</xsl:attribute>
				</xsl:if>
				<RequiredPhoneticInput>
					<xsl:choose>
						<xsl:when test="contains($form1/Form, '[')">
							<xsl:call-template name="RedupLHS">
								<xsl:with-param name="id" select="$id"/>
								<xsl:with-param name="envStrRep" select="$envStrRep"/>
								<xsl:with-param name="morphType" select="$morphType"/>
								<xsl:with-param name="formStr" select="$form1/Form"/>
							</xsl:call-template>
						</xsl:when>
						<xsl:when test="boolean($form2)">
							<xsl:call-template name="CircumfixLHS">
								<xsl:with-param name="id" select="$id"/>
								<xsl:with-param name="preEnv" select="$env1"/>
								<xsl:with-param name="sufEnv" select="$env2"/>
							</xsl:call-template>
						</xsl:when>
						<xsl:when test="$morphType = $sInfix or $morphType = $sInfixingInterfix">
							<xsl:call-template name="InfixLHS">
								<xsl:with-param name="id" select="$id"/>
								<xsl:with-param name="env" select="$env1"/>
							</xsl:call-template>
						</xsl:when>
						<xsl:otherwise>
							<xsl:call-template name="PrefixSuffixLHS">
								<xsl:with-param name="id" select="$id"/>
								<xsl:with-param name="env" select="$env1"/>
								<xsl:with-param name="morphType" select="$morphType"/>
							</xsl:call-template>
						</xsl:otherwise>
					</xsl:choose>
				</RequiredPhoneticInput>
			</InputSideRecordStructure>
			<OutputSideRecordStructure>
				<xsl:if test="string-length(normalize-space($outMPRFeats)) != 0">
					<xsl:attribute name="MPRFeatures">
						<xsl:value-of select="normalize-space($outMPRFeats)"/>
					</xsl:attribute>
				</xsl:if>
				<xsl:if test="contains($form1/Form, '[...]')">
					<xsl:attribute name="redupMorphType">
						<xsl:choose>
							<xsl:when test="$morphType = $sPrefix">
								<xsl:text>prefix</xsl:text>
							</xsl:when>
							<xsl:when test="$morphType = $sSuffix">
								<xsl:text>suffix</xsl:text>
							</xsl:when>
							<xsl:otherwise>
								<xsl:text>implicit</xsl:text>
							</xsl:otherwise>
						</xsl:choose>
					</xsl:attribute>
				</xsl:if>
				<MorphologicalPhoneticOutput>
					<xsl:attribute name="id">
						<xsl:text>mpo</xsl:text>
						<xsl:value-of select="$id"/>
					</xsl:attribute>
					<xsl:choose>
						<xsl:when test="contains($form1/Form, '[')">
							<xsl:call-template name="RedupRHS">
								<xsl:with-param name="id" select="$id"/>
								<xsl:with-param name="morphType" select="$morphType"/>
								<xsl:with-param name="formStr" select="$form1/Form"/>
								<xsl:with-param name="envStrRep" select="$envStrRep"/>
							</xsl:call-template>
						</xsl:when>
						<xsl:when test="boolean($form2)">
							<xsl:call-template name="CircumfixRHS">
								<xsl:with-param name="id" select="$id"/>
								<xsl:with-param name="prefix" select="$form1"/>
								<xsl:with-param name="suffix" select="$form2"/>
							</xsl:call-template>
						</xsl:when>
						<xsl:when test="$morphType = $sInfix or $morphType = $sInfixingInterfix">
							<xsl:call-template name="InfixRHS">
								<xsl:with-param name="id" select="$id"/>
								<xsl:with-param name="formStr" select="$form1/Form"/>
							</xsl:call-template>
						</xsl:when>
						<xsl:otherwise>
							<xsl:call-template name="PrefixSuffixRHS">
								<xsl:with-param name="id" select="$id"/>
								<xsl:with-param name="morphType" select="$morphType"/>
								<xsl:with-param name="formStr" select="$form1/Form"/>
							</xsl:call-template>
						</xsl:otherwise>
					</xsl:choose>
				</MorphologicalPhoneticOutput>
			</OutputSideRecordStructure>
			<xsl:if test="not(boolean($form2)) and boolean($env1)">
				<xsl:variable name="envStr" select="normalize-space(substring-after($env1/@StringRepresentation, '/'))"/>
				<xsl:choose>
					<xsl:when test="$morphType = $sSuffix or $morphType = $sSuffixingInterfix or $morphType = $sEnclitic">
						<xsl:variable name="right" select="normalize-space(substring-after($envStr, '_'))"/>
						<xsl:if test="string-length($right) != 0">
							<RequiredEnvironments>
								<Environment>
									<RightEnvironment>
										<xsl:attribute name="id">
											<xsl:text>env</xsl:text>
											<xsl:value-of select="$id"/>
											<xsl:text>_R</xsl:text>
										</xsl:attribute>
										<PhoneticTemplate>
											<xsl:if test="substring($right, string-length($right)) = '#'">
												<xsl:attribute name="finalBoundaryCondition">
													<xsl:text>true</xsl:text>
												</xsl:attribute>
											</xsl:if>
											<PhoneticSequence>
												<xsl:call-template name="ProcessEnv">
													<xsl:with-param name="env" select="$right"/>
												</xsl:call-template>
											</PhoneticSequence>
										</PhoneticTemplate>
									</RightEnvironment>
								</Environment>
							</RequiredEnvironments>
						</xsl:if>
					</xsl:when>
					<xsl:when test="$morphType = $sPrefix or $morphType = $sPrefixingInterfix or $morphType = $sProclitic">
						<xsl:variable name="left" select="normalize-space(substring-before($envStr, '_'))"/>
						<xsl:if test="string-length($left) != 0">
							<RequiredEnvironments>
								<Environment>
									<LeftEnvironment>
										<xsl:attribute name="id">
											<xsl:text>env</xsl:text>
											<xsl:value-of select="$id"/>
											<xsl:text>_L</xsl:text>
										</xsl:attribute>
										<PhoneticTemplate>
											<xsl:if test="starts-with($left, '#')">
												<xsl:attribute name="initialBoundaryCondition">
													<xsl:text>true</xsl:text>
												</xsl:attribute>
											</xsl:if>
											<PhoneticSequence>
												<xsl:call-template name="ProcessEnv">
													<xsl:with-param name="env" select="$left"/>
												</xsl:call-template>
											</PhoneticSequence>
										</PhoneticTemplate>
									</LeftEnvironment>
								</Environment>
							</RequiredEnvironments>
						</xsl:if>
					</xsl:when>
				</xsl:choose>
			</xsl:if>
			<xsl:call-template name="AdhocAlloRules">
				<xsl:with-param name="id" select="$id"/>
				<xsl:with-param name="formId" select="$form1/@Id"/>
			</xsl:call-template>
			<Properties>
				<Property name="WordCategory">
					<xsl:choose>
						<xsl:when test="$morphType = $sEnclitic">
							<xsl:text>enclitic</xsl:text>
						</xsl:when>
						<xsl:when test="$morphType = $sProclitic">
							<xsl:text>proclitic</xsl:text>
						</xsl:when>
						<xsl:when test="not(boolean($form2)) and name($msa) = 'MoInflAffMsa'">
							<xsl:variable name="slots" select="$msa/Slots"/>
							<xsl:choose>
								<xsl:when test="count($slots) != 0">
									<xsl:for-each select="$slots">
										<xsl:value-of select="@dst"/>
										<xsl:if test="position() != last()">
											<xsl:text> </xsl:text>
										</xsl:if>
									</xsl:for-each>
								</xsl:when>
								<xsl:when test="$morphType = $sSuffix">
									<xsl:text>suffix</xsl:text>
								</xsl:when>
								<xsl:when test="$morphType = $sPrefix or $morphType = $sInfix">
									<xsl:text>prefix</xsl:text>
								</xsl:when>
								<xsl:otherwise>
									<xsl:text>interfix</xsl:text>
								</xsl:otherwise>
							</xsl:choose>
						</xsl:when>
						<xsl:when test="name($msa) = 'MoDerivAffMsa'">
							<xsl:choose>
								<xsl:when test="boolean($form2)">
									<xsl:text>derivCircumPfx derivCircumSfx</xsl:text>
								</xsl:when>
								<xsl:when test="$morphType = $sSuffix or $morphType = $sSuffixingInterfix">
									<xsl:text>derivSfx</xsl:text>
								</xsl:when>
								<xsl:when test="$morphType = $sPrefix or $morphType = $sPrefixingInterfix or $morphType = $sInfix or $morphType = $sInfixingInterfix">
									<xsl:text>derivPfx</xsl:text>
								</xsl:when>
							</xsl:choose>
						</xsl:when>
						<xsl:otherwise>
							<xsl:choose>
								<xsl:when test="boolean($form2)">
									<xsl:text>circumPfx circumSfx</xsl:text>
								</xsl:when>
								<xsl:when test="$morphType = $sSuffixingInterfix">
									<xsl:text>suffixinginterfix</xsl:text>
								</xsl:when>
								<xsl:when test="$morphType = $sPrefixingInterfix or $morphType = $sInfixingInterfix">
									<xsl:text>prefixinginterfix</xsl:text>
								</xsl:when>
								<xsl:when test="$morphType = $sSuffix">
									<xsl:text>suffix</xsl:text>
								</xsl:when>
								<xsl:when test="$morphType = $sPrefix or $morphType = $sInfix">
									<xsl:text>prefix</xsl:text>
								</xsl:when>
							</xsl:choose>
						</xsl:otherwise>
					</xsl:choose>
				</Property>
				<Property name="FormID">
					<xsl:value-of select="$form1/@Id"/>
					<xsl:if test="boolean($form2)">
						<xsl:text> </xsl:text>
						<xsl:value-of select="$form2/@Id"/>
					</xsl:if>
				</Property>
				<Property name="MsaID">
					<xsl:value-of select="$msa/@Id"/>
				</Property>
				<xsl:variable name="envFeats" select="$form1/MsEnvFeatures/FsFeatStruc[node()]"/>
				<xsl:if test="string-length(normalize-space($featDesc)) != 0 or count($envFeats) != 0 or string-length($allNotEnvFeats) != 0">
					<Property name="FeatureDescriptors">
						<xsl:value-of select="normalize-space($featDesc)"/>
						<xsl:choose>
							<xsl:when test="count($envFeats) != 0">
								<xsl:if test="string-length(normalize-space($featDesc)) != 0">
									<xsl:text> </xsl:text>
								</xsl:if>
								<xsl:value-of select="$sMSEnvFS"/>
								<xsl:value-of select="$envFeats/@Id"/>
							</xsl:when>
							<xsl:when test="string-length($allNotEnvFeats) != 0">
								<xsl:if test="string-length(normalize-space($featDesc)) != 0">
									<xsl:text> </xsl:text>
								</xsl:if>
								<xsl:value-of select="$sMSEnvFS"/>
								<xsl:value-of select="$allNotEnvFeats"/>
							</xsl:when>
						</xsl:choose>
					</Property>
				</xsl:if>
				<Property name="MorphType">
					<xsl:value-of select="$morphType"/>
				</Property>
			</Properties>
		</MorphologicalSubruleStructure>
	</xsl:template>

	<!-- Affix Process Subrules -->

	<xsl:template name="AffixProcessSubruleStructure">
		<xsl:param name="msa"/>
		<xsl:param name="form"/>
		<xsl:param name="reqMPRFeats"/>
		<xsl:param name="outMPRFeats"/>
		<xsl:param name="featDesc"/>
		<xsl:variable name="morphType" select="key('MorphTypeId', $form/@MorphType)/@Guid"/>
		<xsl:variable name="id">
			<xsl:value-of select="$msa/@Id"/>
			<xsl:text>_</xsl:text>
			<xsl:value-of select="$form/@Id"/>
		</xsl:variable>
		<MorphologicalSubruleStructure>
			<xsl:attribute name="id">
				<xsl:text>msubrule</xsl:text>
				<xsl:value-of select="$id"/>
			</xsl:attribute>
			<InputSideRecordStructure>
				<xsl:if test="string-length(normalize-space($reqMPRFeats)) != 0">
					<xsl:attribute name="requiredMPRFeatures">
						<xsl:value-of select="normalize-space($reqMPRFeats)"/>
					</xsl:attribute>
				</xsl:if>
				<RequiredPhoneticInput>
					<xsl:for-each select="$form/Input/*">
						<PhoneticSequence>
							<xsl:attribute name="id">
								<xsl:text>pseq</xsl:text>
								<xsl:value-of select="$id"/>
								<xsl:text>_</xsl:text>
								<xsl:value-of select="@Id"/>
							</xsl:attribute>
							<xsl:apply-templates select=".">
								<xsl:with-param name="id" select="$id"/>
							</xsl:apply-templates>
						</PhoneticSequence>
					</xsl:for-each>
				</RequiredPhoneticInput>
			</InputSideRecordStructure>
			<OutputSideRecordStructure>
				<xsl:if test="string-length(normalize-space($outMPRFeats)) != 0">
					<xsl:attribute name="MPRFeatures">
						<xsl:value-of select="normalize-space($outMPRFeats)"/>
					</xsl:attribute>
				</xsl:if>
				<xsl:attribute name="redupMorphType">
					<xsl:choose>
						<xsl:when test="$morphType = $sPrefix">
							<xsl:text>prefix</xsl:text>
						</xsl:when>
						<xsl:when test="$morphType = $sSuffix">
							<xsl:text>suffix</xsl:text>
						</xsl:when>
						<xsl:otherwise>
							<xsl:text>implicit</xsl:text>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:attribute>
				<MorphologicalPhoneticOutput>
					<xsl:attribute name="id">
						<xsl:text>mpo</xsl:text>
						<xsl:value-of select="$id"/>
					</xsl:attribute>
					<xsl:apply-templates select="$form/Output/*">
						<xsl:with-param name="id" select="$id"/>
					</xsl:apply-templates>
				</MorphologicalPhoneticOutput>
			</OutputSideRecordStructure>
			<Properties>
				<Property name="WordCategory">
					<xsl:choose>
						<xsl:when test="$morphType = $sEnclitic">
							<xsl:text>enclitic</xsl:text>
						</xsl:when>
						<xsl:when test="$morphType = $sProclitic">
							<xsl:text>proclitic</xsl:text>
						</xsl:when>
						<xsl:when test="name($msa) = 'MoInflAffMsa'">
							<xsl:variable name="slots" select="$msa/Slots"/>
							<xsl:choose>
								<xsl:when test="count($slots) != 0">
									<xsl:for-each select="$slots">
										<xsl:value-of select="@dst"/>
										<xsl:if test="position() != last()">
											<xsl:text> </xsl:text>
										</xsl:if>
									</xsl:for-each>
								</xsl:when>
								<xsl:when test="$morphType = $sSuffix">
									<xsl:text>suffix</xsl:text>
								</xsl:when>
								<xsl:when test="$morphType = $sCircumfix or $morphType = $sPrefix or $morphType = $sInfix">
									<xsl:text>prefix</xsl:text>
								</xsl:when>
								<xsl:otherwise>
									<xsl:text>interfix</xsl:text>
								</xsl:otherwise>
							</xsl:choose>
						</xsl:when>
						<xsl:when test="name($msa) = 'MoDerivAffMsa'">
							<xsl:choose>
								<xsl:when test="$morphType = $sSuffix or $morphType = $sSuffixingInterfix">
									<xsl:text>derivSfx</xsl:text>
								</xsl:when>
								<xsl:when test="$morphType = $sCircumfix or $morphType = $sPrefix or $morphType = $sPrefixingInterfix or $morphType = $sInfix or $morphType = $sInfixingInterfix">
									<xsl:text>derivPfx</xsl:text>
								</xsl:when>
							</xsl:choose>
						</xsl:when>
						<xsl:otherwise>
							<xsl:choose>
								<xsl:when test="$morphType = $sSuffixingInterfix">
									<xsl:text>suffixinginterfix</xsl:text>
								</xsl:when>
								<xsl:when test="$morphType = $sPrefixingInterfix or $morphType = $sInfixingInterfix">
									<xsl:text>prefixinginterfix</xsl:text>
								</xsl:when>
								<xsl:when test="$morphType = $sSuffix">
									<xsl:text>suffix</xsl:text>
								</xsl:when>
								<xsl:when test="$morphType = $sCircumfix or $morphType = $sPrefix or $morphType = $sInfix">
									<xsl:text>prefix</xsl:text>
								</xsl:when>
							</xsl:choose>
						</xsl:otherwise>
					</xsl:choose>
				</Property>
				<Property name="FormID">
					<xsl:value-of select="$form/@Id"/>
				</Property>
				<Property name="MsaID">
					<xsl:value-of select="$msa/@Id"/>
				</Property>
				<xsl:if test="string-length(normalize-space($featDesc)) != 0">
					<Property name="FeatureDescriptors">
						<xsl:value-of select="normalize-space($featDesc)"/>
					</Property>
				</xsl:if>
				<Property name="MorphType">
					<xsl:value-of select="$morphType"/>
				</Property>
			</Properties>
		</MorphologicalSubruleStructure>
	</xsl:template>

	<xsl:template match="MoCopyFromInput">
		<xsl:param name="id"/>

		<CopyFromInput>
			<xsl:attribute name="index">
				<xsl:text>pseq</xsl:text>
				<xsl:value-of select="$id"/>
				<xsl:text>_</xsl:text>
				<xsl:value-of select="Content/@dst"/>
			</xsl:attribute>
		</CopyFromInput>
	</xsl:template>

	<xsl:template match="MoInsertPhones">
		<InsertSegments>
			<xsl:attribute name="characterTable">
				<xsl:text>table</xsl:text>
				<xsl:value-of select="$charTable/@Id"/>
			</xsl:attribute>
			<PhoneticShape>
				<xsl:for-each select="Content">
					<xsl:sort select="@ord" data-type="number" order="ascending"/>
					<xsl:value-of select="key('TermUnitId', @dst)/Codes/PhCode[1]/Representation"/>
				</xsl:for-each>
			</PhoneticShape>
		</InsertSegments>
	</xsl:template>

	<xsl:template match="MoModifyFromInput">
		<xsl:param name="id"/>

		<ModifyFromInput>
			<xsl:attribute name="index">
				<xsl:text>pseq</xsl:text>
				<xsl:value-of select="$id"/>
				<xsl:text>_</xsl:text>
				<xsl:value-of select="Content/@dst"/>
			</xsl:attribute>
			<SimpleContext>
				<xsl:attribute name="naturalClass">
					<xsl:text>nc</xsl:text>
					<xsl:value-of select="Modification/@dst"/>
				</xsl:attribute>
			</SimpleContext>
		</ModifyFromInput>
	</xsl:template>

	<!-- Allomorph-based LHSs -->

	<xsl:template name="CircumfixLHS">
		<xsl:param name="id"/>
		<xsl:param name="preEnv"/>
		<xsl:param name="sufEnv"/>
		<PhoneticSequence>
			<xsl:attribute name="id">
				<xsl:text>pseq</xsl:text>
				<xsl:value-of select="$id"/>
			</xsl:attribute>
			<xsl:if test="boolean($preEnv)">
				<xsl:variable name="preEnvStr" select="normalize-space(substring-after($preEnv/@StringRepresentation, '/'))"/>
				<xsl:call-template name="ProcessEnv">
					<xsl:with-param name="env" select="normalize-space(substring-after($preEnvStr, '_'))"/>
				</xsl:call-template>
			</xsl:if>
			<xsl:call-template name="AnySeq"/>
			<xsl:if test="boolean($sufEnv)">
				<xsl:variable name="sufEnvStr" select="normalize-space(substring-after($sufEnv/@StringRepresentation, '/'))"/>
				<xsl:call-template name="ProcessEnv">
					<xsl:with-param name="env" select="normalize-space(substring-before($sufEnvStr, '_'))"/>
				</xsl:call-template>
			</xsl:if>
		</PhoneticSequence>
	</xsl:template>

	<xsl:template name="InfixLHS">
		<xsl:param name="id"/>
		<xsl:param name="env"/>
		<xsl:variable name="envStr" select="normalize-space(substring-after($env/@StringRepresentation, '/'))"/>
		<PhoneticSequence>
			<xsl:attribute name="id">
				<xsl:text>pseq</xsl:text>
				<xsl:value-of select="$id"/>
				<xsl:text>_L</xsl:text>
			</xsl:attribute>
			<xsl:variable name="left" select="normalize-space(substring-before($envStr, '_'))"/>
			<xsl:if test="not(starts-with($left, '#'))">
				<xsl:call-template name="AnySeq"/>
			</xsl:if>
			<xsl:call-template name="ProcessEnv">
				<xsl:with-param name="env" select="$left"/>
			</xsl:call-template>
		</PhoneticSequence>
		<PhoneticSequence>
			<xsl:attribute name="id">
				<xsl:text>pseq</xsl:text>
				<xsl:value-of select="$id"/>
				<xsl:text>_R</xsl:text>
			</xsl:attribute>
			<xsl:variable name="right" select="normalize-space(substring-after($envStr, '_'))"/>
			<xsl:call-template name="ProcessEnv">
				<xsl:with-param name="env" select="$right"/>
			</xsl:call-template>
			<xsl:if test="substring($right, string-length($right)) != '#'">
				<xsl:call-template name="AnySeq"/>
			</xsl:if>
		</PhoneticSequence>
	</xsl:template>

	<xsl:template name="RedupLHS">
		<xsl:param name="id"/>
		<xsl:param name="envStrRep"/>
		<xsl:param name="morphType"/>
		<xsl:param name="formStr"/>
		<xsl:choose>
			<xsl:when test="contains($formStr, '[...]')">
				<PhoneticSequence>
					<xsl:attribute name="id">
						<xsl:text>pseq</xsl:text>
						<xsl:value-of select="$id"/>
					</xsl:attribute>
					<xsl:call-template name="AnySeq"/>
				</PhoneticSequence>
			</xsl:when>
			<xsl:otherwise>
				<xsl:variable name="envStr" select="normalize-space(substring-after($envStrRep, '/'))"/>
				<xsl:choose>
					<xsl:when test="$morphType = $sSuffix or $morphType = $sSuffixingInterfix or $morphType = $sEnclitic">
						<xsl:variable name="left" select="normalize-space(substring-before($envStr, '_'))"/>
						<PhoneticSequence>
							<xsl:attribute name="id">
								<xsl:text>pseq</xsl:text>
								<xsl:value-of select="$id"/>
							</xsl:attribute>
							<xsl:call-template name="AnySeq"/>
						</PhoneticSequence>
						<xsl:call-template name="ProcessRedupEnv">
							<xsl:with-param name="envStr" select="$left"/>
							<xsl:with-param name="id" select="$id"/>
						</xsl:call-template>
					</xsl:when>
					<xsl:when test="$morphType = $sPrefix or $morphType = $sPrefixingInterfix or $morphType = $sProclitic">
						<xsl:variable name="right" select="normalize-space(substring-after($envStr, '_'))"/>
						<xsl:call-template name="ProcessRedupEnv">
							<xsl:with-param name="envStr" select="$right"/>
							<xsl:with-param name="id" select="$id"/>
						</xsl:call-template>
						<PhoneticSequence>
							<xsl:attribute name="id">
								<xsl:text>pseq</xsl:text>
								<xsl:value-of select="$id"/>
							</xsl:attribute>
							<xsl:call-template name="AnySeq"/>
						</PhoneticSequence>
					</xsl:when>
				</xsl:choose>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template name="ProcessRedupEnv">
		<xsl:param name="id"/>
		<xsl:param name="envStr"/>
		<xsl:if test="string-length($envStr) != 0">
			<xsl:variable name="char" select="substring($envStr, 1, 1)"/>
			<xsl:if test="$char = '['">
				<xsl:variable name="ncStr" select="substring-before(substring($envStr, 2), ']')"/>
				<xsl:variable name="ncAbbr" select="substring-before($ncStr, '^')"/>
				<xsl:variable name="ncIndex" select="substring-after($ncStr, '^')"/>
				<PhoneticSequence>
					<xsl:attribute name="id">
						<xsl:text>pseq</xsl:text>
						<xsl:value-of select="$id"/>
						<xsl:text>_</xsl:text>
						<xsl:value-of select="$ncAbbr"/>
						<xsl:text>_</xsl:text>
						<xsl:value-of select="$ncIndex"/>
					</xsl:attribute>
					<SimpleContext>
						<xsl:attribute name="naturalClass">
							<xsl:text>nc</xsl:text>
							<xsl:value-of select="key('NCAbbr', $ncAbbr)/@Id"/>
						</xsl:attribute>
					</SimpleContext>
				</PhoneticSequence>
				<xsl:call-template name="ProcessRedupEnv">
					<xsl:with-param name="id" select="$id"/>
					<xsl:with-param name="envStr" select="substring-after($envStr, ']')"/>
				</xsl:call-template>
			</xsl:if>
		</xsl:if>
	</xsl:template>

	<xsl:template name="PrefixSuffixLHS">
		<xsl:param name="id"/>
		<xsl:param name="env"/>
		<xsl:param name="morphType"/>
		<PhoneticSequence>
			<xsl:attribute name="id">
				<xsl:text>pseq</xsl:text>
				<xsl:value-of select="$id"/>
			</xsl:attribute>
			<xsl:choose>
				<xsl:when test="boolean($env)">
					<xsl:variable name="envStr" select="normalize-space(substring-after($env/@StringRepresentation, '/'))"/>
					<xsl:choose>
						<xsl:when test="$morphType = $sSuffix or $morphType = $sSuffixingInterfix or $morphType = $sEnclitic">
							<xsl:variable name="left" select="normalize-space(substring-before($envStr, '_'))"/>
							<xsl:if test="not(starts-with($left, '#'))">
								<xsl:call-template name="AnySeq"/>
							</xsl:if>
							<xsl:if test="string-length($left) != 0">
								<xsl:call-template name="ProcessEnv">
									<xsl:with-param name="env" select="$left"/>
								</xsl:call-template>
							</xsl:if>
						</xsl:when>
						<xsl:when test="$morphType = $sPrefix or $morphType = $sPrefixingInterfix or $morphType = $sProclitic">
							<xsl:variable name="right" select="normalize-space(substring-after($envStr, '_'))"/>
							<xsl:if test="string-length($right) != 0">
								<xsl:call-template name="ProcessEnv">
									<xsl:with-param name="env" select="$right"/>
								</xsl:call-template>
							</xsl:if>
							<xsl:if test="substring($right, string-length($right)) != '#'">
								<xsl:call-template name="AnySeq"/>
							</xsl:if>
						</xsl:when>
					</xsl:choose>
				</xsl:when>
				<xsl:otherwise>
					<xsl:call-template name="AnySeq"/>
				</xsl:otherwise>
			</xsl:choose>
		</PhoneticSequence>
	</xsl:template>

	<xsl:template name="ProcessEnv">
		<xsl:param name="env"/>
		<xsl:if test="string-length($env) != 0">
			<xsl:variable name="char" select="substring($env, 1, 1)"/>
			<xsl:choose>
				<xsl:when test="$char = '['">
					<SimpleContext>
						<xsl:attribute name="naturalClass">
							<xsl:text>nc</xsl:text>
							<xsl:value-of select="key('NCAbbr', substring-before(substring($env, 2), ']'))/@Id"/>
						</xsl:attribute>
					</SimpleContext>
					<xsl:call-template name="ProcessEnv">
						<xsl:with-param name="env" select="normalize-space(substring-after($env, ']'))"/>
					</xsl:call-template>
				</xsl:when>
				<xsl:when test="$char = '#'">
					<xsl:call-template name="ProcessEnv">
						<xsl:with-param name="env" select="normalize-space(substring($env, 2))"/>
					</xsl:call-template>
				</xsl:when>
				<xsl:when test="$char = '('">
					<OptionalSegmentSequence min="0" max="1">
						<xsl:call-template name="ProcessEnv">
							<xsl:with-param name="env" select="normalize-space(substring-before(substring($env, 2), ')'))"/>
						</xsl:call-template>
					</OptionalSegmentSequence>
					<xsl:call-template name="ProcessEnv">
						<xsl:with-param name="env" select="normalize-space(substring-after($env, ')'))"/>
					</xsl:call-template>
				</xsl:when>
				<xsl:otherwise>
					<xsl:variable name="tenv" select="translate($env, '[#( ', '[[[[')"/>
					<xsl:choose>
						<xsl:when test="contains($tenv, '[')">
							<xsl:call-template name="FindRep">
								<xsl:with-param name="env" select="$env"/>
								<xsl:with-param name="str" select="substring-before($tenv, '[')"/>
							</xsl:call-template>
						</xsl:when>
						<xsl:otherwise>
							<xsl:call-template name="FindRep">
								<xsl:with-param name="env" select="$env"/>
								<xsl:with-param name="str" select="$env"/>
							</xsl:call-template>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:if>
	</xsl:template>

	<xsl:template name="FindRep">
		<xsl:param name="env"/>
		<xsl:param name="str"/>

		<xsl:variable name="representation" select="key('Rep', $str)"/>
		<xsl:choose>
			<xsl:when test="string-length($str) = 0">
				<xsl:call-template name="ProcessEnv">
					<xsl:with-param name="env" select="normalize-space(substring($env, 2))"/>
				</xsl:call-template>
			</xsl:when>
			<xsl:when test="boolean($representation)">
				<Segment>
					<xsl:attribute name="characterTable">
						<xsl:text>table</xsl:text>
						<xsl:value-of select="$charTable/@Id"/>
					</xsl:attribute>
					<xsl:attribute name="representation">
						<xsl:text>rep</xsl:text>
						<xsl:value-of select="$representation/@Id"/>
						<xsl:text>_</xsl:text>
						<xsl:value-of select="count($representation/preceding-sibling::PhCode) + 1"/>
					</xsl:attribute>
				</Segment>
				<xsl:call-template name="ProcessEnv">
					<xsl:with-param name="env" select="normalize-space(substring($env, string-length($str) + 1))"/>
				</xsl:call-template>
			</xsl:when>
			<xsl:otherwise>
				<xsl:call-template name="FindRep">
					<xsl:with-param name="str" select="substring($str, 1, string-length($str) - 1)"/>
					<xsl:with-param name="env" select="$env"/>
				</xsl:call-template>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template name="AnySeq">
		<OptionalSegmentSequence min="0" max="-1">
			<BoundaryMarker representation="repNull">
				<xsl:attribute name="characterTable">
					<xsl:text>table</xsl:text>
					<xsl:value-of select="$charTable/@Id"/>
				</xsl:attribute>
			</BoundaryMarker>
			<BoundaryMarker>
				<xsl:attribute name="characterTable">
					<xsl:text>table</xsl:text>
					<xsl:value-of select="$charTable/@Id"/>
				</xsl:attribute>
				<xsl:attribute name="representation">
					<xsl:text>rep</xsl:text>
					<xsl:value-of select="$morphBdry/@Id"/>
				</xsl:attribute>
			</BoundaryMarker>
		</OptionalSegmentSequence>
		<OptionalSegmentSequence min="0" max="-1">
			<SimpleContext naturalClass="ncAny"/>
		</OptionalSegmentSequence>
		<OptionalSegmentSequence min="0" max="-1">
			<BoundaryMarker>
				<xsl:attribute name="characterTable">
					<xsl:text>table</xsl:text>
					<xsl:value-of select="$charTable/@Id"/>
				</xsl:attribute>
				<xsl:attribute name="representation">
					<xsl:text>rep</xsl:text>
					<xsl:value-of select="$morphBdry/@Id"/>
				</xsl:attribute>
			</BoundaryMarker>
			<BoundaryMarker representation="repNull">
				<xsl:attribute name="characterTable">
					<xsl:text>table</xsl:text>
					<xsl:value-of select="$charTable/@Id"/>
				</xsl:attribute>
			</BoundaryMarker>
		</OptionalSegmentSequence>
	</xsl:template>

	<!-- Allomorph-based RHSs -->

	<xsl:template name="CircumfixRHS">
		<xsl:param name="id"/>
		<xsl:param name="prefix"/>
		<xsl:param name="suffix"/>
		<InsertSegments>
			<xsl:attribute name="characterTable">
				<xsl:text>table</xsl:text>
				<xsl:value-of select="$charTable/@Id"/>
			</xsl:attribute>
			<PhoneticShape>
				<xsl:call-template name="FormatForm">
					<xsl:with-param name="formStr" select="$prefix/Form"/>
				</xsl:call-template>
				<xsl:text>+</xsl:text>
			</PhoneticShape>
		</InsertSegments>
		<CopyFromInput>
			<xsl:attribute name="index">
				<xsl:text>pseq</xsl:text>
				<xsl:value-of select="$id"/>
			</xsl:attribute>
		</CopyFromInput>
		<InsertSegments>
			<xsl:attribute name="characterTable">
				<xsl:text>table</xsl:text>
				<xsl:value-of select="$charTable/@Id"/>
			</xsl:attribute>
			<PhoneticShape>
				<xsl:text>+</xsl:text>
				<xsl:call-template name="FormatForm">
					<xsl:with-param name="formStr" select="$suffix/Form"/>
				</xsl:call-template>
			</PhoneticShape>
		</InsertSegments>
	</xsl:template>

	<xsl:template name="InfixRHS">
		<xsl:param name="id"/>
		<xsl:param name="formStr"/>
		<CopyFromInput>
			<xsl:attribute name="index">
				<xsl:text>pseq</xsl:text>
				<xsl:value-of select="$id"/>
				<xsl:text>_L</xsl:text>
			</xsl:attribute>
		</CopyFromInput>
		<InsertSegments>
			<xsl:attribute name="characterTable">
				<xsl:text>table</xsl:text>
				<xsl:value-of select="$charTable/@Id"/>
			</xsl:attribute>
			<PhoneticShape>
				<xsl:text>+</xsl:text>
				<xsl:call-template name="FormatForm">
					<xsl:with-param name="formStr" select="$formStr"/>
				</xsl:call-template>
				<xsl:text>+</xsl:text>
			</PhoneticShape>
		</InsertSegments>
		<CopyFromInput>
			<xsl:attribute name="index">
				<xsl:text>pseq</xsl:text>
				<xsl:value-of select="$id"/>
				<xsl:text>_R</xsl:text>
			</xsl:attribute>
		</CopyFromInput>
	</xsl:template>

	<xsl:template name="RedupRHS">
		<xsl:param name="id"/>
		<xsl:param name="morphType"/>
		<xsl:param name="formStr"/>
		<xsl:param name="envStrRep"/>
		<xsl:choose>
			<xsl:when test="contains($formStr, '[...]')">
				<CopyFromInput>
					<xsl:attribute name="index">
						<xsl:text>pseq</xsl:text>
						<xsl:value-of select="$id"/>
					</xsl:attribute>
				</CopyFromInput>
				<xsl:variable name="beforeStr" select="substring-before($formStr, '[')"/>
				<InsertSegments>
					<xsl:attribute name="characterTable">
						<xsl:text>table</xsl:text>
						<xsl:value-of select="$charTable/@Id"/>
					</xsl:attribute>
					<PhoneticShape>
						<xsl:text>+</xsl:text>
						<xsl:if test="string-length($beforeStr) != 0">
							<xsl:value-of select="$beforeStr"/>
						</xsl:if>
					</PhoneticShape>
				</InsertSegments>
				<CopyFromInput>
					<xsl:attribute name="index">
						<xsl:text>pseq</xsl:text>
						<xsl:value-of select="$id"/>
					</xsl:attribute>
				</CopyFromInput>
				<xsl:variable name="afterStr" select="substring-after($formStr, ']')"/>
				<xsl:if test="string-length($afterStr) != 0">
					<InsertSegments>
						<xsl:attribute name="characterTable">
							<xsl:text>table</xsl:text>
							<xsl:value-of select="$charTable/@Id"/>
						</xsl:attribute>
						<PhoneticShape>
							<xsl:value-of select="$afterStr"/>
						</PhoneticShape>
					</InsertSegments>
				</xsl:if>
			</xsl:when>
			<xsl:otherwise>
				<xsl:variable name="envStr" select="normalize-space(substring-after($envStrRep, '/'))"/>
				<xsl:if test="$morphType = $sPrefix or $morphType = $sPrefixingInterfix or $morphType = $sProclitic">
					<xsl:call-template name="ProcessRedupPattern">
						<xsl:with-param name="id" select="$id"/>
						<xsl:with-param name="formStr" select="normalize-space($formStr)"/>
					</xsl:call-template>
					<InsertSegments>
						<xsl:attribute name="characterTable">
							<xsl:text>table</xsl:text>
							<xsl:value-of select="$charTable/@Id"/>
						</xsl:attribute>
						<PhoneticShape>
							<xsl:text>+</xsl:text>
						</PhoneticShape>
					</InsertSegments>
					<xsl:variable name="right" select="normalize-space(substring-after($envStr, '_'))"/>
					<xsl:call-template name="ProcessRedupPattern">
						<xsl:with-param name="id" select="$id"/>
						<xsl:with-param name="formStr" select="$right"/>
					</xsl:call-template>
				</xsl:if>
				<CopyFromInput>
					<xsl:attribute name="index">
						<xsl:text>pseq</xsl:text>
						<xsl:value-of select="$id"/>
					</xsl:attribute>
				</CopyFromInput>
				<xsl:if test="$morphType = $sSuffix or $morphType = $sSuffixingInterfix or $morphType = $sEnclitic">
					<xsl:variable name="left" select="normalize-space(substring-before($envStr, '_'))"/>
					<xsl:call-template name="ProcessRedupPattern">
						<xsl:with-param name="id" select="$id"/>
						<xsl:with-param name="formStr" select="$left"/>
					</xsl:call-template>
					<InsertSegments>
						<xsl:attribute name="characterTable">
							<xsl:text>table</xsl:text>
							<xsl:value-of select="$charTable/@Id"/>
						</xsl:attribute>
						<PhoneticShape>
							<xsl:text>+</xsl:text>
						</PhoneticShape>
					</InsertSegments>
					<xsl:call-template name="ProcessRedupPattern">
						<xsl:with-param name="id" select="$id"/>
						<xsl:with-param name="formStr" select="normalize-space($formStr)"/>
					</xsl:call-template>
				</xsl:if>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template name="ProcessRedupPattern">
		<xsl:param name="id"/>
		<xsl:param name="formStr"/>
		<xsl:if test="string-length($formStr) != 0">
			<xsl:variable name="char" select="substring($formStr, 1, 1)"/>
			<xsl:choose>
				<xsl:when test="$char = '['">
					<xsl:variable name="ncStr" select="substring-before(substring($formStr, 2), ']')"/>
					<xsl:variable name="ncAbbr" select="substring-before($ncStr, '^')"/>
					<xsl:variable name="ncIndex" select="substring-after($ncStr, '^')"/>
					<CopyFromInput>
						<xsl:attribute name="index">
							<xsl:text>pseq</xsl:text>
							<xsl:value-of select="$id"/>
							<xsl:text>_</xsl:text>
							<xsl:value-of select="$ncAbbr"/>
							<xsl:text>_</xsl:text>
							<xsl:value-of select="$ncIndex"/>
						</xsl:attribute>
					</CopyFromInput>
					<xsl:call-template name="ProcessRedupPattern">
						<xsl:with-param name="id" select="$id"/>
						<xsl:with-param name="formStr" select="normalize-space(substring-after($formStr, ']'))"/>
					</xsl:call-template>
				</xsl:when>
				<xsl:otherwise>
					<InsertSegments>
						<xsl:attribute name="characterTable">
							<xsl:text>table</xsl:text>
							<xsl:value-of select="$charTable/@Id"/>
						</xsl:attribute>
						<PhoneticShape>
							<xsl:value-of select="$char"/>
						</PhoneticShape>
					</InsertSegments>
					<xsl:call-template name="ProcessRedupPattern">
						<xsl:with-param name="id" select="$id"/>
						<xsl:with-param name="formStr" select="normalize-space(substring($formStr, 2))"/>
					</xsl:call-template>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:if>
	</xsl:template>

	<xsl:template name="PrefixSuffixRHS">
		<xsl:param name="id"/>
		<xsl:param name="morphType"/>
		<xsl:param name="formStr"/>

		<xsl:if test="$morphType = $sPrefix or $morphType = $sPrefixingInterfix or $morphType = $sProclitic">
			<InsertSegments>
				<xsl:attribute name="characterTable">
					<xsl:text>table</xsl:text>
					<xsl:value-of select="$charTable/@Id"/>
				</xsl:attribute>
				<PhoneticShape>
					<xsl:call-template name="FormatForm">
						<xsl:with-param name="formStr" select="$formStr"/>
					</xsl:call-template>
					<xsl:text>+</xsl:text>
				</PhoneticShape>
			</InsertSegments>
		</xsl:if>
		<CopyFromInput>
			<xsl:attribute name="index">
				<xsl:text>pseq</xsl:text>
				<xsl:value-of select="$id"/>
			</xsl:attribute>
		</CopyFromInput>
		<xsl:if test="$morphType = $sSuffix or $morphType = $sSuffixingInterfix or $morphType = $sEnclitic">
			<InsertSegments>
				<xsl:attribute name="characterTable">
					<xsl:text>table</xsl:text>
					<xsl:value-of select="$charTable/@Id"/>
				</xsl:attribute>
				<PhoneticShape>
					<xsl:text>+</xsl:text>
					<xsl:call-template name="FormatForm">
						<xsl:with-param name="formStr" select="$formStr"/>
					</xsl:call-template>
				</PhoneticShape>
			</InsertSegments>
		</xsl:if>
	</xsl:template>

	<xsl:template name="FormatForm">
		<xsl:param name="formStr"/>

		<xsl:choose>
			<xsl:when test="$formStr = '*0' or $formStr = '&amp;0' or $formStr = '&#x2205;'">
				<xsl:text>^0</xsl:text>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="translate($formStr, ' ', '.')"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<!-- Adhoc Rules -->

	<xsl:template name="AdhocAlloRules">
		<xsl:param name="formId"/>

		<xsl:variable name="adhocAlloRules" select="$root/AdhocCoProhibitions/descendant-or-self::MoAlloAdhocProhib[FirstAllomorph/@dst = $formId]"/>
		<xsl:variable name="validAdhocRules">
			<xsl:call-template name="HasValidAdhocAlloRule">
				<xsl:with-param name="adhocAlloRules" select="$adhocAlloRules"/>
			</xsl:call-template>
		</xsl:variable>
		<xsl:if test="string-length($validAdhocRules) != 0">
			<ExcludedAllomorphCoOccurrences>
				<xsl:for-each select="$adhocAlloRules">
					<xsl:variable name="nextOrd">
						<xsl:choose>
							<xsl:when test="@Adjacency = 0 or @Adjacency = 2 or @Adjacency = 4">
								<xsl:value-of select="0"/>
							</xsl:when>
							<xsl:otherwise>
								<xsl:value-of select="count(RestOfAllos) - 1"/>
							</xsl:otherwise>
						</xsl:choose>
					</xsl:variable>
					<xsl:call-template name="AdhocAlloRule">
						<xsl:with-param name="adhocAlloRule" select="."/>
						<xsl:with-param name="ord" select="number($nextOrd)"/>
					</xsl:call-template>
				</xsl:for-each>
			</ExcludedAllomorphCoOccurrences>
		</xsl:if>
	</xsl:template>

	<xsl:template name="HasValidAdhocAlloRule">
		<xsl:param name="adhocAlloRules"/>

		<xsl:for-each select="$adhocAlloRules">
			<xsl:for-each select="RestOfAllos">
				<xsl:variable name="form" select="key('AlloId', @dst)"/>
				<xsl:choose>
					<xsl:when test="name($form) = 'MoStemAllomorph'">
						<xsl:variable name="valid">
							<xsl:call-template name="IsValidLexEntryForm">
								<xsl:with-param name="form" select="$form"/>
							</xsl:call-template>
						</xsl:variable>
						<xsl:if test="string-length($valid) != 0">
							<xsl:text>Y</xsl:text>
						</xsl:if>
					</xsl:when>
					<xsl:otherwise>
						<xsl:variable name="valid">
							<xsl:call-template name="IsValidRuleForm">
								<xsl:with-param name="form" select="$form"/>
							</xsl:call-template>
						</xsl:variable>
						<xsl:if test="string-length($valid) != 0">
							<xsl:text>Y</xsl:text>
						</xsl:if>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:for-each>
		</xsl:for-each>
	</xsl:template>

	<xsl:template name="AdhocAlloRule">
		<xsl:param name="adhocAlloRule"/>
		<xsl:param name="ord"/>
		<xsl:param name="allomorphIds"/>

		<xsl:variable name="adjacency" select="$adhocAlloRule/@Adjacency"/>
		<xsl:choose>
			<xsl:when test="$ord = count($adhocAlloRule/RestOfAllos) or $ord = -1">
				<xsl:if test="string-length($allomorphIds) != 0">
					<AllomorphCoOccurrence>
						<xsl:attribute name="allomorphs">
							<xsl:value-of select="$allomorphIds"/>
						</xsl:attribute>
						<xsl:attribute name="adjacency">
							<xsl:choose>
								<xsl:when test="$adjacency = 0">
									<xsl:text>anywhere</xsl:text>
								</xsl:when>
								<xsl:when test="$adjacency = 1">
									<xsl:text>somewhereToLeft</xsl:text>
								</xsl:when>
								<xsl:when test="$adjacency = 2">
									<xsl:text>somewhereToRight</xsl:text>
								</xsl:when>
								<xsl:when test="$adjacency = 3">
									<xsl:text>adjacentToLeft</xsl:text>
								</xsl:when>
								<xsl:when test="$adjacency = 4">
									<xsl:text>adjacentToRight</xsl:text>
								</xsl:when>
							</xsl:choose>
						</xsl:attribute>
					</AllomorphCoOccurrence>
				</xsl:if>
			</xsl:when>
			<xsl:otherwise>
				<xsl:variable name="formId" select="$adhocAlloRule/RestOfAllos[@ord = $ord]/@dst"/>
				<xsl:variable name="form" select="key('AlloId', $formId)"/>
				<xsl:variable name="entry" select="$entries/LexEntry[LexemeForm/@dst = $formId or AlternateForms/@dst = $formId]"/>
				<xsl:variable name="nextOrd">
					<xsl:choose>
						<xsl:when test="$adjacency = 0 or $adjacency = 2 or $adjacency = 4">
							<xsl:value-of select="$ord + 1"/>
						</xsl:when>
						<xsl:otherwise>
							<xsl:value-of select="$ord - 1"/>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:variable>
				<xsl:choose>
					<xsl:when test="name($form) = 'MoStemAllomorph'">
						<xsl:variable name="valid">
							<xsl:call-template name="IsValidLexEntryForm">
								<xsl:with-param name="form" select="$form"/>
							</xsl:call-template>
						</xsl:variable>
						<xsl:choose>
							<xsl:when test="string-length($valid) != 0">
								<xsl:for-each select="$entry/MorphoSyntaxAnalysis">
									<xsl:call-template name="AdhocAlloRule">
										<xsl:with-param name="adhocAlloRule" select="$adhocAlloRule"/>
										<xsl:with-param name="ord" select="number($nextOrd)"/>
										<xsl:with-param name="allomorphIds">
											<xsl:value-of select="$allomorphIds"/>
											<xsl:if test="string-length($allomorphIds) != 0">
												<xsl:text> </xsl:text>
											</xsl:if>
											<xsl:text>allo</xsl:text>
											<xsl:value-of select="@dst"/>
											<xsl:text>_</xsl:text>
											<xsl:value-of select="$form/@Id"/>
										</xsl:with-param>
									</xsl:call-template>
									<xsl:variable name="morphType" select="key('MorphTypeId', $entry/LexemeForm/@MorphType)/@Guid"/>
									<xsl:if test="$morphType = $sEnclitic or $morphType = $sProclitic">
										<xsl:call-template name="AdhocAlloRuleMRule">
											<xsl:with-param name="adhocAlloRule" select="$adhocAlloRule"/>
											<xsl:with-param name="nextOrd" select="$nextOrd"/>
											<xsl:with-param name="allomorphIds" select="$allomorphIds"/>
											<xsl:with-param name="form" select="$form"/>
											<xsl:with-param name="msaId" select="@dst"/>
										</xsl:call-template>
									</xsl:if>
								</xsl:for-each>
							</xsl:when>
							<xsl:otherwise>
								<xsl:call-template name="AdhocAlloRule">
									<xsl:with-param name="adhocAlloRule" select="$adhocAlloRule"/>
									<xsl:with-param name="ord" select="number($nextOrd)"/>
									<xsl:with-param name="allomorphIds" select="$allomorphIds"/>
								</xsl:call-template>
							</xsl:otherwise>
						</xsl:choose>
					</xsl:when>
					<xsl:otherwise>
						<xsl:variable name="valid">
							<xsl:call-template name="IsValidRuleForm">
								<xsl:with-param name="form" select="$form"/>
							</xsl:call-template>
						</xsl:variable>
						<xsl:choose>
							<xsl:when test="string-length($valid) != 0">
								<xsl:for-each select="$entry/MorphoSyntaxAnalysis">
									<xsl:call-template name="AdhocAlloRuleMRule">
										<xsl:with-param name="adhocAlloRule" select="$adhocAlloRule"/>
										<xsl:with-param name="nextOrd" select="$nextOrd"/>
										<xsl:with-param name="allomorphIds" select="$allomorphIds"/>
										<xsl:with-param name="form" select="$form"/>
										<xsl:with-param name="msaId" select="@dst"/>
									</xsl:call-template>
								</xsl:for-each>
							</xsl:when>
							<xsl:otherwise>
								<xsl:call-template name="AdhocAlloRule">
									<xsl:with-param name="adhocAlloRule" select="$adhocAlloRule"/>
									<xsl:with-param name="ord" select="number($nextOrd)"/>
									<xsl:with-param name="allomorphIds" select="$allomorphIds"/>
								</xsl:call-template>
							</xsl:otherwise>
						</xsl:choose>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template name="AdhocAlloRuleMRule">
		<xsl:param name="adhocAlloRule"/>
		<xsl:param name="nextOrd"/>
		<xsl:param name="allomorphIds"/>
		<xsl:param name="form"/>
		<xsl:param name="msaId"/>

		<xsl:variable name="envs" select="$form/PhoneEnv | $form/Position"/>
		<xsl:variable name="hasBlankEnv">
			<xsl:call-template name="HasBlankEnv">
				<xsl:with-param name="envs" select="$envs"/>
			</xsl:call-template>
		</xsl:variable>
		<xsl:for-each select="$envs">
			<xsl:call-template name="AdhocAlloRule">
				<xsl:with-param name="adhocAlloRule" select="$adhocAlloRule"/>
				<xsl:with-param name="ord" select="number($nextOrd)"/>
				<xsl:with-param name="allomorphIds">
					<xsl:value-of select="$allomorphIds"/>
					<xsl:if test="string-length($allomorphIds) != 0">
						<xsl:text> </xsl:text>
					</xsl:if>
					<xsl:text>msubrule</xsl:text>
					<xsl:value-of select="$msaId"/>
					<xsl:text>_</xsl:text>
					<xsl:value-of select="$form/@Id"/>
					<xsl:text>_</xsl:text>
					<xsl:value-of select="@dst"/>
				</xsl:with-param>
			</xsl:call-template>
		</xsl:for-each>
		<xsl:if test="string-length($hasBlankEnv) != 0">
			<xsl:call-template name="AdhocAlloRule">
				<xsl:with-param name="adhocAlloRule" select="$adhocAlloRule"/>
				<xsl:with-param name="ord" select="number($nextOrd)"/>
				<xsl:with-param name="allomorphIds">
					<xsl:value-of select="$allomorphIds"/>
					<xsl:if test="string-length($allomorphIds) != 0">
						<xsl:text> </xsl:text>
					</xsl:if>
					<xsl:text>msubrule</xsl:text>
					<xsl:value-of select="$msaId"/>
					<xsl:text>_</xsl:text>
					<xsl:value-of select="$form/@Id"/>
				</xsl:with-param>
			</xsl:call-template>
		</xsl:if>
	</xsl:template>

	<xsl:template name="AdhocMorphRules">
		<xsl:param name="msaId"/>

		<xsl:variable name="adhocMorphRules" select="$root/AdhocCoProhibitions/descendant-or-self::MoMorphAdhocProhib[FirstMorpheme/@dst = $msaId]"/>
		<xsl:variable name="validAdhocRules">
			<xsl:call-template name="HasValidAdhocMorphRule">
				<xsl:with-param name="adhocMorphRules" select="$adhocMorphRules"/>
			</xsl:call-template>
		</xsl:variable>
		<xsl:if test="string-length($validAdhocRules) != 0">
			<ExcludedMorphemeCoOccurrences>
				<xsl:for-each select="$adhocMorphRules">
					<xsl:variable name="nextOrd">
						<xsl:choose>
							<xsl:when test="@Adjacency = 0 or @Adjacency = 2 or @Adjacency = 4">
								<xsl:value-of select="0"/>
							</xsl:when>
							<xsl:otherwise>
								<xsl:value-of select="count(RestOfMorphs) - 1"/>
							</xsl:otherwise>
						</xsl:choose>
					</xsl:variable>
					<xsl:call-template name="AdhocMorphRule">
						<xsl:with-param name="adhocMorphRule" select="."/>
						<xsl:with-param name="ord" select="number($nextOrd)"/>
					</xsl:call-template>
				</xsl:for-each>
			</ExcludedMorphemeCoOccurrences>
		</xsl:if>
	</xsl:template>

	<xsl:template name="HasValidAdhocMorphRule">
		<xsl:param name="adhocMorphRules"/>

		<xsl:for-each select="$adhocMorphRules">
			<xsl:for-each select="RestOfMorphs">
				<xsl:variable name="msa" select="key('MsaId', @dst)"/>
				<xsl:variable name="entry" select="$entries/LexEntry[MorphoSyntaxAnalysis/@dst = $msa/@Id]"/>
				<xsl:choose>
					<xsl:when test="name($msa) = 'MoStemMsa'">
						<xsl:variable name="valid">
							<xsl:call-template name="HasValidLexEntryForm">
								<xsl:with-param name="entry" select="$entry"/>
							</xsl:call-template>
						</xsl:variable>
						<xsl:if test="string-length($valid) != 0">
							<xsl:text>Y</xsl:text>
						</xsl:if>
					</xsl:when>
					<xsl:otherwise>
						<xsl:variable name="valid">
							<xsl:call-template name="HasValidRuleForm">
								<xsl:with-param name="entry" select="$entry"/>
							</xsl:call-template>
						</xsl:variable>
						<xsl:if test="string-length($valid) != 0">
							<xsl:text>Y</xsl:text>
						</xsl:if>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:for-each>
		</xsl:for-each>
	</xsl:template>

	<xsl:template name="AdhocMorphRule">
		<xsl:param name="adhocMorphRule"/>
		<xsl:param name="ord"/>
		<xsl:param name="morphemeIds"/>

		<xsl:variable name="adjacency" select="$adhocMorphRule/@Adjacency"/>
		<xsl:choose>
			<xsl:when test="$ord = count($adhocMorphRule/RestOfMorphs) or $ord = -1">
				<xsl:if test="string-length($morphemeIds) != 0">
					<MorphemeCoOccurrence>
						<xsl:attribute name="morphemes">
							<xsl:value-of select="$morphemeIds"/>
						</xsl:attribute>
						<xsl:attribute name="adjacency">
							<xsl:choose>
								<xsl:when test="$adjacency = 0">
									<xsl:text>anywhere</xsl:text>
								</xsl:when>
								<xsl:when test="$adjacency = 1">
									<xsl:text>somewhereToLeft</xsl:text>
								</xsl:when>
								<xsl:when test="$adjacency = 2">
									<xsl:text>somewhereToRight</xsl:text>
								</xsl:when>
								<xsl:when test="$adjacency = 3">
									<xsl:text>adjacentToLeft</xsl:text>
								</xsl:when>
								<xsl:when test="$adjacency = 4">
									<xsl:text>adjacentToRight</xsl:text>
								</xsl:when>
							</xsl:choose>
						</xsl:attribute>
					</MorphemeCoOccurrence>
				</xsl:if>
			</xsl:when>
			<xsl:otherwise>
				<xsl:variable name="msaId" select="$adhocMorphRule/RestOfMorphs[@ord = $ord]/@dst"/>
				<xsl:variable name="msa" select="key('MsaId', $msaId)"/>
				<xsl:variable name="entry" select="$entries/LexEntry[MorphoSyntaxAnalysis/@dst = $msaId]"/>
				<xsl:variable name="nextOrd">
					<xsl:choose>
						<xsl:when test="$adjacency = 0 or $adjacency = 2 or $adjacency = 4">
							<xsl:value-of select="$ord + 1"/>
						</xsl:when>
						<xsl:otherwise>
							<xsl:value-of select="$ord - 1"/>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:variable>
				<xsl:choose>
					<xsl:when test="name($msa) = 'MoStemMsa'">
						<xsl:call-template name="AdhocMorphRule">
							<xsl:with-param name="adhocMorphRule" select="$adhocMorphRule"/>
							<xsl:with-param name="ord" select="number($nextOrd)"/>
							<xsl:with-param name="morphemeIds">
								<xsl:value-of select="$morphemeIds"/>
								<xsl:variable name="valid">
									<xsl:call-template name="HasValidLexEntryForm">
										<xsl:with-param name="entry" select="$entry"/>
									</xsl:call-template>
								</xsl:variable>
								<xsl:if test="string-length($valid) != 0">
									<xsl:if test="string-length($morphemeIds) != 0">
										<xsl:text> </xsl:text>
									</xsl:if>
									<xsl:text>lex</xsl:text>
									<xsl:value-of select="$msaId"/>
								</xsl:if>
							</xsl:with-param>
						</xsl:call-template>
						<xsl:variable name="morphType" select="key('MorphTypeId', $entry/LexemeForm/@MorphType)/@Guid"/>
						<xsl:if test="$morphType = $sEnclitic or $morphType = $sProclitic">
							<xsl:call-template name="AdhocMorphRule">
								<xsl:with-param name="adhocMorphRule" select="$adhocMorphRule"/>
								<xsl:with-param name="ord" select="number($nextOrd)"/>
								<xsl:with-param name="morphemeIds">
									<xsl:value-of select="$morphemeIds"/>
									<xsl:variable name="valid">
										<xsl:call-template name="HasValidRuleForm">
											<xsl:with-param name="entry" select="$entry"/>
										</xsl:call-template>
									</xsl:variable>
									<xsl:if test="string-length($valid) != 0">
										<xsl:if test="string-length($morphemeIds) != 0">
											<xsl:text> </xsl:text>
										</xsl:if>
										<xsl:text>mrule</xsl:text>
										<xsl:value-of select="$msaId"/>
									</xsl:if>
								</xsl:with-param>
							</xsl:call-template>
						</xsl:if>
					</xsl:when>
					<xsl:otherwise>
						<xsl:call-template name="AdhocMorphRule">
							<xsl:with-param name="adhocMorphRule" select="$adhocMorphRule"/>
							<xsl:with-param name="ord" select="number($nextOrd)"/>
							<xsl:with-param name="morphemeIds">
								<xsl:value-of select="$morphemeIds"/>
								<xsl:variable name="valid">
									<xsl:call-template name="HasValidRuleForm">
										<xsl:with-param name="entry" select="$entry"/>
									</xsl:call-template>
								</xsl:variable>
								<xsl:if test="string-length($valid) != 0">
									<xsl:if test="string-length($morphemeIds) != 0">
										<xsl:text> </xsl:text>
									</xsl:if>
									<xsl:text>mrule</xsl:text>
									<xsl:value-of select="$msaId"/>
								</xsl:if>
							</xsl:with-param>
						</xsl:call-template>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<!-- Morphological/Phonological Rule Features -->

	<xsl:template match="MoInflClass | ProdRestrict/CmPossibility | LexEntryInflType">
		<MorphologicalPhonologicalRuleFeature>
			<xsl:attribute name="id">
				<xsl:text>mpr</xsl:text>
				<xsl:value-of select="@Id"/>
			</xsl:attribute>
			<xsl:value-of select="Name"/>
		</MorphologicalPhonologicalRuleFeature>
	</xsl:template>

	<xsl:template name="MPRFeatureGroup">
		<xsl:param name="id"/>
		<xsl:param name="name"/>
		<xsl:param name="feats"/>
		<xsl:param name="matchType"/>
		<xsl:if test="count($feats) != 0">
			<MorphologicalPhonologicalRuleFeatureGroup>
				<xsl:attribute name="id">
					<xsl:value-of select="$id"/>
				</xsl:attribute>
				<xsl:attribute name="matchType">
					<xsl:value-of select="$matchType"/>
				</xsl:attribute>
				<xsl:attribute name="features">
					<xsl:for-each select="$feats">
						<xsl:text>mpr</xsl:text>
						<xsl:value-of select="@Id"/>
						<xsl:if test="position() != last()">
							<xsl:text> </xsl:text>
						</xsl:if>
					</xsl:for-each>
				</xsl:attribute>
				<Name>
					<xsl:value-of select="$name"/>
				</Name>
			</MorphologicalPhonologicalRuleFeatureGroup>
		</xsl:if>
	</xsl:template>

	<!-- Phonological Rules -->

	<xsl:template match="PhRegularRule">
		<xsl:if test="count(StrucDesc/*) > 0 or count(RightHandSides/PhSegRuleRHS/StrucChange/*) > 0">
			<PhonologicalRule>
				<xsl:attribute name="ruleStrata">
					<xsl:choose>
						<xsl:when test="$fNotOnClitics='false'">
							<xsl:text>morphophonemic clitic</xsl:text>
						</xsl:when>
						<xsl:otherwise>morphophonemic</xsl:otherwise>
					</xsl:choose>
				</xsl:attribute>
				<xsl:attribute name="id">
					<xsl:text>prule</xsl:text>
					<xsl:value-of select="@Id"/>
				</xsl:attribute>
				<xsl:attribute name="multipleApplicationOrder">
					<xsl:choose>
						<xsl:when test="@Direction = 0">
							<xsl:text>leftToRightIterative</xsl:text>
						</xsl:when>
						<xsl:when test="@Direction = 1">
							<xsl:text>rightToLeftIterative</xsl:text>
						</xsl:when>
						<xsl:when test="@Direction = 2">
							<xsl:text>simultaneous</xsl:text>
						</xsl:when>
					</xsl:choose>
				</xsl:attribute>
				<Name>
					<xsl:value-of select="Name"/>
				</Name>

				<xsl:variable name="constrs" select="FeatureConstraints"/>
				<xsl:if test="count($constrs) > 0">
					<VariableFeatures>
						<xsl:for-each select="$constrs">
							<xsl:sort select="@ord" data-type="number" order="ascending"/>
							<xsl:variable name="constr" select="key('FeatConstrId', @dst)"/>
							<VariableFeature>
								<xsl:attribute name="id">
									<xsl:text>var</xsl:text>
									<xsl:value-of select="@dst"/>
								</xsl:attribute>
								<xsl:attribute name="name">
									<xsl:value-of select="substring($varNames, @ord + 1, 1)"/>
								</xsl:attribute>
								<xsl:attribute name="phonologicalFeature">
									<xsl:text>feat</xsl:text>
									<xsl:value-of select="$constr/Feature/@dst"/>
								</xsl:attribute>
							</VariableFeature>
						</xsl:for-each>
					</VariableFeatures>
				</xsl:if>
				<PhoneticInputSequence>
					<xsl:attribute name="id">
						<xsl:text>pis</xsl:text>
						<xsl:value-of select="@Id"/>
					</xsl:attribute>
					<PhoneticSequence>
						<xsl:apply-templates select="StrucDesc/*"/>
					</PhoneticSequence>
				</PhoneticInputSequence>
				<PhonologicalSubrules>
					<xsl:apply-templates select="RightHandSides/PhSegRuleRHS"/>
				</PhonologicalSubrules>
			</PhonologicalRule>
		</xsl:if>
	</xsl:template>

	<xsl:template match="PhSegRuleRHS">
		<PhonologicalSubrule>
			<PhonologicalSubruleStructure>
				<xsl:if test="InputPOSes/RequiredPOS">
					<xsl:attribute name="requiredPartsOfSpeech">
						<xsl:for-each select="InputPOSes/RequiredPOS">
							<xsl:text>pos</xsl:text>
							<xsl:value-of select="@dst"/>
							<xsl:call-template name="POSIds">
								<xsl:with-param name="posId" select="@dst"/>
							</xsl:call-template>
							<xsl:if test="position() != last()">
								<xsl:text>&#x20;</xsl:text>
							</xsl:if>
						</xsl:for-each>
					</xsl:attribute>
				</xsl:if>
				<xsl:if test="ReqRuleFeats/RuleFeat">
					<xsl:attribute name="requiredMPRFeatures">
						<xsl:for-each select="ReqRuleFeats/RuleFeat">
							<xsl:text>mpr</xsl:text>
							<xsl:value-of select="key('PhonRuleFeatId',@dst)/Item/@itemRef"/>
							<xsl:if test="position()!=last()">
								<xsl:text>&#x20;</xsl:text>
							</xsl:if>
						</xsl:for-each>
					</xsl:attribute>
				</xsl:if>
				<xsl:if test="ExclRuleFeats/RuleFeat">
					<xsl:attribute name="excludedMPRFeatures">
						<xsl:for-each select="ExclRuleFeats/RuleFeat">
							<xsl:text>mpr</xsl:text>
							<xsl:value-of select="key('PhonRuleFeatId',@dst)/Item/@itemRef"/>
							<xsl:if test="position()!=last()">
								<xsl:text>&#x20;</xsl:text>
							</xsl:if>
						</xsl:for-each>
					</xsl:attribute>
				</xsl:if>
				<PhoneticOutput>
					<xsl:attribute name="id">
						<xsl:text>pout</xsl:text>
						<xsl:value-of select="@Id"/>
					</xsl:attribute>
					<PhoneticSequence>
						<xsl:apply-templates select="StrucChange/*"/>
					</PhoneticSequence>
				</PhoneticOutput>
				<xsl:variable name="leftCtxt" select="LeftContext/*"/>
				<xsl:variable name="rightCtxt" select="RightContext/*"/>
				<xsl:if test="count($leftCtxt) > 0 or count($rightCtxt) > 0">
					<Environment>
						<xsl:if test="count($leftCtxt) > 0">
							<LeftEnvironment>
								<xsl:attribute name="id">
									<xsl:text>lenv</xsl:text>
									<xsl:value-of select="@Id"/>
								</xsl:attribute>
								<PhoneticTemplate>
									<xsl:attribute name="initialBoundaryCondition">
										<xsl:call-template name="IsWordInitial">
											<xsl:with-param name="ctxts" select="$leftCtxt"/>
										</xsl:call-template>
									</xsl:attribute>
									<PhoneticSequence>
										<xsl:apply-templates select="$leftCtxt"/>
									</PhoneticSequence>
								</PhoneticTemplate>
							</LeftEnvironment>
						</xsl:if>
						<xsl:if test="count($rightCtxt) > 0">
							<RightEnvironment>
								<xsl:attribute name="id">
									<xsl:text>renv</xsl:text>
									<xsl:value-of select="@Id"/>
								</xsl:attribute>
								<PhoneticTemplate>
									<xsl:attribute name="finalBoundaryCondition">
										<xsl:call-template name="IsWordFinal">
											<xsl:with-param name="ctxts" select="$rightCtxt"/>
										</xsl:call-template>
									</xsl:attribute>
									<PhoneticSequence>
										<xsl:apply-templates select="$rightCtxt"/>
									</PhoneticSequence>
								</PhoneticTemplate>
							</RightEnvironment>
						</xsl:if>
					</Environment>
				</xsl:if>
			</PhonologicalSubruleStructure>
		</PhonologicalSubrule>
	</xsl:template>

	<xsl:template match="PhMetathesisRule">
		<xsl:variable name="strucChange" select="StrucChange"/>
		<xsl:variable name="leftEnv" select="substring-before($strucChange, ' ')"/>
		<xsl:variable name="leftEnvRemain" select="substring-after($strucChange, ' ')"/>
		<xsl:variable name="rightSwitch" select="substring-before($leftEnvRemain, ' ')"/>
		<xsl:variable name="rightSwitchRemain" select="substring-after($leftEnvRemain, ' ')"/>
		<xsl:variable name="middle">
			<xsl:choose>
				<xsl:when test="contains($rightSwitchRemain, ':')">
					<xsl:value-of select="substring-before($rightSwitchRemain, ':')"/>
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="substring-before($rightSwitchRemain, ' ')"/>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<xsl:variable name="middleRemain" select="substring-after($rightSwitchRemain, ' ')"/>
		<xsl:variable name="leftSwitch" select="substring-before($middleRemain, ' ')"/>
		<xsl:variable name="rightEnv" select="substring-after($middleRemain, ' ')"/>

		<xsl:if test="$leftSwitch != -1 and $rightSwitch != -1">
			<MetathesisRule>
				<xsl:attribute name="ruleStrata">
					<xsl:choose>
						<xsl:when test="$fNotOnClitics='false'">
							<xsl:text>morphophonemic clitic</xsl:text>
						</xsl:when>
						<xsl:otherwise>morphophonemic</xsl:otherwise>
					</xsl:choose>
				</xsl:attribute>
				<xsl:attribute name="id">
					<xsl:text>prule</xsl:text>
					<xsl:value-of select="@Id"/>
				</xsl:attribute>
				<xsl:attribute name="multipleApplicationOrder">
					<xsl:choose>
						<xsl:when test="@Direction = 0">
							<xsl:text>leftToRightIterative</xsl:text>
						</xsl:when>
						<xsl:when test="@Direction = 1">
							<xsl:text>rightToLeftIterative</xsl:text>
						</xsl:when>
						<xsl:when test="@Direction = 2">
							<xsl:text>simultaneous</xsl:text>
						</xsl:when>
					</xsl:choose>
				</xsl:attribute>
				<xsl:variable name="ctxts" select="StrucDesc/*"/>
				<xsl:variable name="strucChangeIds">
					<xsl:if test="$leftEnv != -1">
						<xsl:call-template name="StrucChangeIds">
							<xsl:with-param name="start" select="0"/>
							<xsl:with-param name="limit" select="$leftEnv + 1"/>
							<xsl:with-param name="ctxts" select="$ctxts"/>
						</xsl:call-template>
					</xsl:if>
					<xsl:if test="$rightSwitch != -1">
						<xsl:call-template name="StrucChangeIds">
							<xsl:with-param name="start" select="$rightSwitch"/>
							<xsl:with-param name="limit" select="$rightSwitch + 1"/>
							<xsl:with-param name="ctxts" select="$ctxts"/>
						</xsl:call-template>
					</xsl:if>
					<xsl:if test="$middle != -1">
						<xsl:call-template name="StrucChangeIds">
							<xsl:with-param name="start" select="$middle"/>
							<xsl:with-param name="limit" select="$middle + 1"/>
							<xsl:with-param name="ctxts" select="$ctxts"/>
						</xsl:call-template>
					</xsl:if>
					<xsl:if test="$leftSwitch != -1">
						<xsl:call-template name="StrucChangeIds">
							<xsl:with-param name="start" select="$leftSwitch"/>
							<xsl:with-param name="limit" select="$leftSwitch + 1"/>
							<xsl:with-param name="ctxts" select="$ctxts"/>
						</xsl:call-template>
					</xsl:if>
					<xsl:if test="$rightEnv != -1">
						<xsl:call-template name="StrucChangeIds">
							<xsl:with-param name="start" select="$rightEnv"/>
							<xsl:with-param name="limit" select="count($ctxts)"/>
							<xsl:with-param name="ctxts" select="$ctxts"/>
						</xsl:call-template>
					</xsl:if>
				</xsl:variable>
				<xsl:attribute name="structuralChange">
					<xsl:value-of select="normalize-space($strucChangeIds)"/>
				</xsl:attribute>
				<Name>
					<xsl:value-of select="Name"/>
				</Name>
				<StructuralDescription>
					<PhoneticTemplate>
						<xsl:attribute name="initialBoundaryCondition">
							<xsl:call-template name="IsWordInitial">
								<xsl:with-param name="ctxts" select="$ctxts"/>
							</xsl:call-template>
						</xsl:attribute>
						<xsl:attribute name="finalBoundaryCondition">
							<xsl:call-template name="IsWordFinal">
								<xsl:with-param name="ctxts" select="$ctxts"/>
							</xsl:call-template>
						</xsl:attribute>
						<PhoneticSequence>
							<xsl:apply-templates select="$ctxts"/>
						</PhoneticSequence>
					</PhoneticTemplate>
				</StructuralDescription>
			</MetathesisRule>
		</xsl:if>
	</xsl:template>

	<xsl:template name="StrucChangeIds">
		<xsl:param name="start"/>
		<xsl:param name="limit"/>
		<xsl:param name="ctxts"/>

		<xsl:for-each select="$ctxts[position() - 1 &gt;= $start and position() - 1 &lt; $limit]">
			<xsl:if test="name() != 'PhSimpleContextBdry' or @dst != $wordBdry/@Id">
				<xsl:text>ctxt</xsl:text>
				<xsl:value-of select="@Id"/>
				<xsl:text> </xsl:text>
			</xsl:if>
		</xsl:for-each>
	</xsl:template>

	<!-- Phonetic Sequence Contexts -->

	<xsl:template match="PhSimpleContextNC">
		<xsl:param name="id"/>

		<SimpleContext>
			<xsl:attribute name="id">
				<xsl:text>ctxt</xsl:text>
				<xsl:if test="string-length($id) != 0">
					<xsl:value-of select="$id"/>
					<xsl:text>_</xsl:text>
				</xsl:if>
				<xsl:value-of select="@Id"/>
			</xsl:attribute>
			<xsl:attribute name="naturalClass">
				<xsl:text>nc</xsl:text>
				<xsl:value-of select="@dst"/>
			</xsl:attribute>
			<xsl:variable name="constrs" select="PlusConstr | MinusConstr"/>
			<xsl:if test="count($constrs) > 0">
				<AlphaVariables>
					<xsl:for-each select="$constrs">
						<AlphaVariable>
							<xsl:attribute name="variableFeature">
								<xsl:text>var</xsl:text>
								<xsl:value-of select="@dst"/>
							</xsl:attribute>
							<xsl:attribute name="polarity">
								<xsl:choose>
									<xsl:when test="name() = 'PlusConstr'">
										<xsl:text>plus</xsl:text>
									</xsl:when>
									<xsl:otherwise>
										<xsl:text>minus</xsl:text>
									</xsl:otherwise>
								</xsl:choose>
							</xsl:attribute>
						</AlphaVariable>
					</xsl:for-each>
				</AlphaVariables>
			</xsl:if>
		</SimpleContext>
	</xsl:template>

	<xsl:template match="PhSimpleContextSeg">
		<xsl:param name="id"/>

		<Segment>
			<xsl:attribute name="id">
				<xsl:text>ctxt</xsl:text>
				<xsl:if test="string-length($id) != 0">
					<xsl:value-of select="$id"/>
					<xsl:text>_</xsl:text>
				</xsl:if>
				<xsl:value-of select="@Id"/>
			</xsl:attribute>
			<xsl:attribute name="characterTable">
				<xsl:text>table</xsl:text>
				<xsl:value-of select="$charTable/@Id"/>
			</xsl:attribute>
			<xsl:attribute name="representation">
				<xsl:variable name="representation" select="key('Rep', @dst)"/>
				<xsl:text>rep</xsl:text>
				<xsl:value-of select="@dst"/>
				<xsl:text>_</xsl:text>
				<xsl:value-of select="count($representation/preceding-sibling::PhCode) + 1"/>
			</xsl:attribute>
		</Segment>
	</xsl:template>

	<xsl:template match="PhSimpleContextBdry">
		<xsl:param name="id"/>

		<xsl:if test="@dst != $wordBdry/@Id">
			<BoundaryMarker>
				<xsl:attribute name="id">
					<xsl:text>ctxt</xsl:text>
					<xsl:if test="string-length($id) != 0">
						<xsl:value-of select="$id"/>
						<xsl:text>_</xsl:text>
					</xsl:if>
					<xsl:value-of select="@Id"/>
				</xsl:attribute>
				<xsl:attribute name="characterTable">
					<xsl:text>table</xsl:text>
					<xsl:value-of select="$charTable/@Id"/>
				</xsl:attribute>
				<xsl:attribute name="representation">
					<xsl:text>rep</xsl:text>
					<xsl:value-of select="@dst"/>
				</xsl:attribute>
			</BoundaryMarker>
		</xsl:if>
	</xsl:template>

	<xsl:template match="PhSequenceContext">
		<xsl:param name="id"/>

		<xsl:for-each select="Members">
			<xsl:sort select="@ord" data-type="number" order="ascending"/>
			<xsl:apply-templates select="key('CtxtId', @dst)">
				<xsl:with-param name="id" select="$id"/>
			</xsl:apply-templates>
		</xsl:for-each>
	</xsl:template>

	<xsl:template match="PhIterationContext">
		<xsl:param name="id"/>

		<OptionalSegmentSequence>
			<xsl:attribute name="id">
				<xsl:text>ctxt</xsl:text>
				<xsl:if test="string-length($id) != 0">
					<xsl:value-of select="$id"/>
					<xsl:text>_</xsl:text>
				</xsl:if>
				<xsl:value-of select="@Id"/>
			</xsl:attribute>
			<xsl:attribute name="min">
				<xsl:value-of select="@Minimum"/>
			</xsl:attribute>
			<xsl:attribute name="max">
				<xsl:value-of select="@Maximum"/>
			</xsl:attribute>
			<xsl:apply-templates select="key('CtxtId', Member/@dst)"/>
		</OptionalSegmentSequence>
	</xsl:template>

	<xsl:template match="PhVariable">
		<xsl:call-template name="AnySeq"/>
	</xsl:template>

	<xsl:template name="IsWordInitial">
		<xsl:param name="ctxts"/>
		<xsl:variable name="first" select="$ctxts[1]"/>
		<xsl:choose>
			<xsl:when test="name($first) = 'PhSimpleContextBdry'">
				<xsl:choose>
					<xsl:when test="$first/@dst = $wordBdry/@Id">
						<xsl:text>true</xsl:text>
					</xsl:when>
					<xsl:otherwise>
						<xsl:text>false</xsl:text>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:when>
			<xsl:when test="name($first) = 'PhSequenceContext'">
				<xsl:variable name="firstMember" select="key('CtxtId', $first/Members[1]/@dst)"/>
				<xsl:choose>
					<xsl:when test="name($firstMember) = 'PhSimpleContextBdry' and $firstMember/@dst = $wordBdry/@Id">
						<xsl:text>true</xsl:text>
					</xsl:when>
					<xsl:otherwise>
						<xsl:text>false</xsl:text>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:when>
			<xsl:otherwise>
				<xsl:text>false</xsl:text>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template name="IsWordFinal">
		<xsl:param name="ctxts"/>

		<xsl:variable name="last" select="$ctxts[position() = last()]"/>
		<xsl:choose>
			<xsl:when test="name($last) = 'PhSimpleContextBdry'">
				<xsl:choose>
					<xsl:when test="$last/@dst = $wordBdry/@Id">
						<xsl:text>true</xsl:text>
					</xsl:when>
					<xsl:otherwise>
						<xsl:text>false</xsl:text>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:when>
			<xsl:when test="name($last) = 'PhSequenceContext'">
				<xsl:variable name="lastMember" select="key('CtxtId', $last/Members[position() = last()]/@dst)"/>
				<xsl:choose>
					<xsl:when test="name($lastMember) = 'PhSimpleContextBdry' and $lastMember/@dst = $wordBdry/@Id">
						<xsl:text>true</xsl:text>
					</xsl:when>
					<xsl:otherwise>
						<xsl:text>false</xsl:text>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:when>
			<xsl:otherwise>
				<xsl:text>false</xsl:text>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template name="OutputUniqueStrings">
		<xsl:param name="sList"/>
		<xsl:variable name="sNewList" select="normalize-space($sList)"/>
		<xsl:variable name="sFirst" select="substring-before($sNewList,';')"/>
		<xsl:variable name="sRest" select="substring-after($sNewList,';')"/>
		<xsl:if test="not(contains($sRest,concat($sFirst,';')))">
			<xsl:value-of select="translate($sFirst,';','')"/>
		</xsl:if>
		<xsl:if test="$sRest">
			<xsl:call-template name="OutputUniqueStrings">
				<xsl:with-param name="sList" select="$sRest"/>
			</xsl:call-template>
		</xsl:if>
	</xsl:template>

</xsl:stylesheet>
