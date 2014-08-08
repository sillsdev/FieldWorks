<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="text" version="1.0" encoding="UTF-8" indent="yes"/>
	<!--
================================================================
M3 to XAmple Unified Dictionary File mapper for Stage 1.
  Input:    XML output from M3ParserSvr which has been passed through CleanFWDump.xslt
  Output: XAmple unified dictionary file
================================================================
Revision History is at the end of this file.

- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Preamble
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<!-- Parameters that can be set by user.  -->
	<!-- none now -->
	<!-- Using keys instead of IDs (so no DTD or XSD required) -->
	<xsl:key name="AffixAlloID" match="MoAffixAllomorph" use="@Id"/>
	<xsl:key name="AffixSlotsID" match="/M3Dump/PartsOfSpeech/PartOfSpeech/AffixSlots/MoInflAffixSlot" use="@Id"/>
	<xsl:key name="AlloID" match="MoAffixAllomorph | MoStemAllomorph" use="@Id"/>
	<xsl:key name="DerivMsaID" match="MoDerivAffMsa" use="@Id"/>
	<xsl:key name="InflClassID" match="MoInflClass" use="@Id"/>
	<xsl:key name="InflMsaID" match="MoInflAffMsa" use="@Id"/>
	<xsl:key name="UnclassifiedMsaID" match="MoUnclassifiedAffixMsa" use="@Id"/>
	<xsl:key name="LexEntryID" match="LexEntry" use="@Id"/>
	<xsl:key name="LexEntryInflTypeSlots" match="LexEntryInflType/Slots" use="@dst"/>
	<xsl:key name="LexSenseID" match="LexSense" use="@Id"/>
	<xsl:key name="MorphTypeID" match="MoMorphType" use="@Id"/>
	<xsl:key name="POSID" match="PartOfSpeech" use="@Id"/>
	<xsl:key name="PhEnvID" match="PhEnvironment" use="@Id"/>
	<xsl:key name="PhContextID" match="PhSimpleContextNC | PhSimpleContextSeg | PhSimpleContextBdry | PhIterationContext | PhSequenceContext" use="@Id"/>
	<xsl:key name="PhBdryID" match="PhBdryMarker" use="@Id"/>
	<xsl:key name="PhonemeID" match="PhPhoneme" use="@Id"/>
	<xsl:key name="StemAlloID" match="MoStemAllomorph" use="@Id"/>
	<xsl:key name="StemMsaID" match="MoStemMsa" use="@Id"/>
	<xsl:key name="ToMsaDst" match="ToMsa | OverridingMsa" use="@dst"/>
	<xsl:key name="NatClassAbbr" match="PhNCSegments/Abbreviation" use="."/>
	<!-- various global variables to make searching faster -->
	<xsl:variable name="AllAffixTemplates" select="MoInflAffixTemplate"/>
	<xsl:variable name="AllPrefixSlots" select="/M3Dump/PartsOfSpeech/PartOfSpeech/AffixTemplates/MoInflAffixTemplate/PrefixSlots"/>
	<xsl:variable name="AllSuffixSlots" select="/M3Dump/PartsOfSpeech/PartOfSpeech/AffixTemplates/MoInflAffixTemplate/SuffixSlots"/>
	<xsl:variable name="InflAffixSlots" select="/M3Dump/PartsOfSpeech/PartOfSpeech/AffixSlots/MoInflAffixSlot"/>
	<xsl:variable name="LexEntries" select="/M3Dump/Lexicon/Entries/LexEntry"/>
	<xsl:variable name="MoStemMsas" select="/M3Dump/Lexicon/MorphoSyntaxAnalyses/MoStemMsa"/>
	<xsl:variable name="MoInflAffMsas" select="/M3Dump/Lexicon/MorphoSyntaxAnalyses/MoInflAffMsa"/>
	<xsl:variable name="MoDerivAffMsas" select="/M3Dump/Lexicon/MorphoSyntaxAnalyses/MoDerivAffMsa"/>
	<xsl:variable name="MoUnclassifiedAffixMsas" select="/M3Dump/Lexicon/MorphoSyntaxAnalyses/MoUnclassifiedAffixMsa"/>
	<xsl:variable name="MoAffixAllomorphs" select="/M3Dump/Lexicon/Allomorphs/MoAffixAllomorph"/>
	<xsl:variable name="MoStemAllomorphs" select="/M3Dump/Lexicon/Allomorphs//MoStemAllomorph"/>
	<xsl:variable name="CompoundRules" select="/M3Dump/CompoundRules/MoEndoCompound | /M3Dump/CompoundRules/MoExoCompound"/>
	<xsl:variable name="PartsOfSpeech" select="/M3Dump/PartsOfSpeech/PartOfSpeech"/>
	<!-- included stylesheets (i.e. things common to other style sheets) -->
	<xsl:include href="MorphTypeGuids.xsl"/>
	<xsl:include href="XAmpleTemplateVariables.xsl"/>
	<xsl:include href="FxtM3ParserCommon.xsl"/>
	<!-- following is used for writing inflection class MECs -->
	<xsl:variable name="sExocentricToInflectionClasses">
		<!-- will use a delimeter to guarantee uniqueness of the class name -->
		<xsl:text>|</xsl:text>
		<xsl:for-each select="$MoStemMsas[key('ToMsaDst', @Id) and @InflectionClass!=0]">
			<xsl:value-of select="@InflectionClass"/>
			<!-- will use a delimeter to guarantee uniqueness of the class name -->
			<xsl:text>|</xsl:text>
		</xsl:for-each>
	</xsl:variable>
	<!-- following is a way to deal with iteration contexts -->
	<xsl:variable name="PhIters" select="/M3Dump/PhPhonData/PhIters"/>
	<!-- following is for full reduplication -->
	<xsl:variable name="sFullRedupPattern" select="'[...]'"/>
	<xsl:variable name="sPosIdDivider" select="'|'"/>

	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Main template
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="/">
		<xsl:apply-templates select="$LexEntries"/>
		<xsl:apply-templates select="$LexEntryInflTypeSlots"/>
		<!-- commented off since we're not using linkers right now
		<xsl:for-each select="//MoEndoCompound | //MoExoCompound">
			<xsl:if test="Linker[@dst!=0]">
				<xsl:variable name="sId">
					<xsl:value-of select="Linker/@dst"/>
				</xsl:variable>
\lx <xsl:value-of select="$sId"/>
\g <xsl:value-of select="$sId"/>
\c Linker
\wc Linker<xsl:for-each select="key('AffixAlloID',Linker/@dst)">
					<xsl:call-template name="AlloForm">
						<xsl:with-param name="bAffix" select="'Y'"/>
					</xsl:call-template>
				</xsl:for-each>
			</xsl:if>
		</xsl:for-each>
		-->
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		A new lexical entry
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template match="LexEntry">
		<!-- skip phrases -->
		<xsl:variable name="sLexemeFormTypeGuid" select="key('MorphTypeID', LexemeForm/@MorphType)/@Guid"/>
		<xsl:if test="LexemeForm/@MorphType='0' or $sLexemeFormTypeGuid!=$sDiscontiguousPhrase">
			<xsl:variable name="bIsAbstractOnly">
				<xsl:call-template name="IsAbstractOnly"/>
			</xsl:variable>
			<xsl:if test="$bIsAbstractOnly!='Y'">
				<xsl:variable name="lexEntry" select="."/>
				<xsl:choose>
					<xsl:when test="MorphoSyntaxAnalysis">
						<xsl:for-each select="MorphoSyntaxAnalysis">
							<xsl:variable name="thisMsa" select="@dst"/>
							<xsl:text>
\lx </xsl:text>
							<xsl:value-of select="$thisMsa"/>
							<xsl:apply-templates select="key('StemMsaID', $thisMsa) | key('InflMsaID', $thisMsa) | key('DerivMsaID', $thisMsa) | key('UnclassifiedMsaID', $thisMsa)">
								<xsl:with-param name="lexEntry" select="$lexEntry"/>
								<xsl:with-param name="allos" select="$lexEntry/AlternateForms | $lexEntry/LexemeForm"/>
								<xsl:with-param name="gloss" select="key('LexSenseID',../Sense[1]/@dst)/Gloss"/>
							</xsl:apply-templates>
							<!--            Ensure Final Newline-->
							<xsl:text>
&#x20;</xsl:text>
						</xsl:for-each>
					</xsl:when>
					<xsl:otherwise>
						<xsl:for-each select="LexEntryRef">
							<xsl:variable name="lexEntryRef" select="."/>
							<xsl:for-each select="ComponentLexeme">
								<xsl:variable name="componentLexEntry" select="key('LexEntryID',@dst)"/>
								<xsl:choose>
									<xsl:when test="$componentLexEntry">
										<xsl:for-each select="$componentLexEntry/MorphoSyntaxAnalysis">
											<xsl:variable name="stemMsa" select="key('StemMsaID',@dst)"/>
											<xsl:call-template name="OutputVariantEntry">
												<xsl:with-param name="lexEntryRef" select="$lexEntryRef"/>
												<xsl:with-param name="lexEntry" select="$lexEntry"/>
												<xsl:with-param name="componentLexEntry" select="$componentLexEntry"/>
												<xsl:with-param name="sVariantOfGloss" select="key('LexSenseID',$componentLexEntry/Sense[1]/@dst)/Gloss"/>
												<xsl:with-param name="stemMsa" select="$stemMsa"/>
											</xsl:call-template>
										</xsl:for-each>
									</xsl:when>
									<xsl:otherwise>
										<!-- the component must refer to a sense -->
										<xsl:variable name="componentSense" select="key('LexSenseID',@dst)"/>
										<xsl:variable name="stemMsa" select="key('StemMsaID',$componentSense/@Msa)"/>
										<xsl:call-template name="OutputVariantEntry">
											<xsl:with-param name="lexEntryRef" select="$lexEntryRef"/>
											<xsl:with-param name="lexEntry" select="$lexEntry"/>
											<xsl:with-param name="sVariantOfGloss" select="$componentSense/Gloss"/>
											<xsl:with-param name="stemMsa" select="$stemMsa"/>
										</xsl:call-template>
									</xsl:otherwise>
								</xsl:choose>
							</xsl:for-each>
						</xsl:for-each>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:if>
		</xsl:if>
	</xsl:template>
	<!--
	  MoUnclassifiedAffixMsa
   -->
	<xsl:template match="MoUnclassifiedAffixMsa">
		<xsl:param name="lexEntry"/>
		<xsl:param name="sCircumfix"/>
		<xsl:param name="gloss"/>
		<xsl:param name="allos"/>
		<xsl:variable name="unclassifiedMsa" select="."/>
		<xsl:variable name="sTypes">
			<xsl:call-template name="GetAffixMorphType">
				<xsl:with-param name="allos" select="$allos"/>
			</xsl:call-template>
		</xsl:variable>
		<xsl:choose>
			<xsl:when test="key('MorphTypeID', $lexEntry/LexemeForm/@MorphType)/@Guid=$sCircumfix">
				<!-- is a circumfix entry -->
				<xsl:if test="contains($sTypes, 'prefix')">
					<xsl:call-template name="DoUnclassifiedAffix">
						<xsl:with-param name="unclassifiedMsa" select="$unclassifiedMsa"/>
						<xsl:with-param name="gloss" select="$gloss"/>
						<xsl:with-param name="sTypes">prefix</xsl:with-param>
						<xsl:with-param name="bCircumfix">Y</xsl:with-param>
						<xsl:with-param name="lexEntry" select="$lexEntry"/>
						<xsl:with-param name="allos" select="$allos"/>
					</xsl:call-template>
					<xsl:call-template name="CircumfixMCCs">
						<xsl:with-param name="morphname" select="@dst"/>
					</xsl:call-template>
				</xsl:if>
				<xsl:if test="contains($sTypes, 'infix')">
					<xsl:if test="contains($sTypes,'prefix')">
						<!-- only need to output \lx if there was a prefix -->
						<xsl:text>

\lx </xsl:text>
						<xsl:value-of select="@Id"/>
					</xsl:if>
					<xsl:call-template name="DoUnclassifiedAffix">
						<xsl:with-param name="unclassifiedMsa" select="$unclassifiedMsa"/>
						<xsl:with-param name="gloss" select="$gloss"/>
						<xsl:with-param name="sTypes">infix</xsl:with-param>
						<xsl:with-param name="bCircumfix">Y</xsl:with-param>
						<xsl:with-param name="lexEntry" select="$lexEntry"/>
						<xsl:with-param name="allos" select="$allos"/>
					</xsl:call-template>
					<xsl:call-template name="CircumfixMCCs">
						<xsl:with-param name="morphname" select="@dst"/>
					</xsl:call-template>
					<xsl:text>
\fd Infix</xsl:text>
				</xsl:if>
				<xsl:if test="contains($sTypes, 'suffix')">
					<xsl:text>

\lx </xsl:text>
					<xsl:value-of select="@Id"/>
					<xsl:call-template name="DoUnclassifiedAffix">
						<xsl:with-param name="unclassifiedMsa" select="$unclassifiedMsa"/>
						<xsl:with-param name="gloss" select="$gloss"/>
						<xsl:with-param name="sTypes">suffix</xsl:with-param>
						<xsl:with-param name="bCircumfix">Y</xsl:with-param>
						<xsl:with-param name="lexEntry" select="$lexEntry"/>
						<xsl:with-param name="allos" select="$allos"/>
					</xsl:call-template>
					<xsl:call-template name="CircumfixMCCs">
						<xsl:with-param name="morphname" select="@dst"/>
					</xsl:call-template>
				</xsl:if>
			</xsl:when>
			<xsl:otherwise>
				<!-- non-circumfix entry -->
				<xsl:call-template name="DoUnclassifiedAffix">
					<xsl:with-param name="unclassifiedMsa" select="$unclassifiedMsa"/>
					<xsl:with-param name="gloss" select="$gloss"/>
					<xsl:with-param name="sTypes" select="$sTypes"/>
					<xsl:with-param name="lexEntry" select="$lexEntry"/>
					<xsl:with-param name="allos" select="$allos"/>
				</xsl:call-template>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
	  MoDerivAffMsa
   -->
	<xsl:template match="MoDerivAffMsa">
		<xsl:param name="lexEntry"/>
		<xsl:param name="gloss"/>
		<xsl:param name="allos"/>
		<xsl:variable name="derivMsa" select="."/>
		<xsl:variable name="sTypes">
			<xsl:call-template name="GetAffixMorphType">
				<xsl:with-param name="allos" select="$allos"/>
			</xsl:call-template>
		</xsl:variable>
		<xsl:choose>
			<xsl:when test="key('MorphTypeID', $lexEntry/LexemeForm/@MorphType)/@Guid=$sCircumfix">
				<!-- is a circumfix entry -->
				<xsl:if test="contains($sTypes, 'prefix')">
					<xsl:call-template name="DoDerivAffix">
						<xsl:with-param name="derivMsa" select="$derivMsa"/>
						<xsl:with-param name="gloss" select="$gloss"/>
						<xsl:with-param name="sTypes">prefix</xsl:with-param>
						<xsl:with-param name="bCircumfix">Y</xsl:with-param>
						<xsl:with-param name="lexEntry" select="$lexEntry"/>
						<xsl:with-param name="allos" select="$allos"/>
					</xsl:call-template>
					<xsl:call-template name="CircumfixMCCs">
						<xsl:with-param name="morphname" select="@dst"/>
					</xsl:call-template>
				</xsl:if>
				<xsl:if test="contains($sTypes, 'infix')">
					<xsl:if test="contains($sTypes,'prefix')">
						<!-- only need to output \lx if there was a prefix -->
						<xsl:text>

\lx </xsl:text>
						<xsl:value-of select="@Id"/>
					</xsl:if>
					<xsl:call-template name="DoDerivAffix">
						<xsl:with-param name="derivMsa" select="$derivMsa"/>
						<xsl:with-param name="gloss" select="$gloss"/>
						<xsl:with-param name="sTypes">infix</xsl:with-param>
						<xsl:with-param name="bCircumfix">Y</xsl:with-param>
						<xsl:with-param name="lexEntry" select="$lexEntry"/>
						<xsl:with-param name="allos" select="$allos"/>
					</xsl:call-template>
					<xsl:call-template name="CircumfixMCCs">
						<xsl:with-param name="morphname" select="@Id"/>
					</xsl:call-template>
					<xsl:text>
\fd Infix</xsl:text>
				</xsl:if>
				<xsl:if test="contains($sTypes, 'suffix')">
					<xsl:text>

\lx </xsl:text>
					<xsl:value-of select="@Id"/>
					<xsl:call-template name="DoDerivAffix">
						<xsl:with-param name="derivMsa" select="$derivMsa"/>
						<xsl:with-param name="gloss" select="$gloss"/>
						<xsl:with-param name="sTypes">suffix</xsl:with-param>
						<xsl:with-param name="bCircumfix">Y</xsl:with-param>
						<xsl:with-param name="lexEntry" select="$lexEntry"/>
						<xsl:with-param name="allos" select="$allos"/>
					</xsl:call-template>
					<xsl:call-template name="CircumfixMCCs">
						<xsl:with-param name="morphname" select="@Id"/>
					</xsl:call-template>
				</xsl:if>
			</xsl:when>
			<xsl:otherwise>
				<!-- non-circumfix entry -->
				<xsl:call-template name="DoDerivAffix">
					<xsl:with-param name="derivMsa" select="$derivMsa"/>
					<xsl:with-param name="gloss" select="$gloss"/>
					<xsl:with-param name="sTypes" select="$sTypes"/>
					<xsl:with-param name="lexEntry" select="$lexEntry"/>
					<xsl:with-param name="allos" select="$allos"/>
				</xsl:call-template>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
	  MoInflAffMsa
   -->
	<xsl:template match="MoInflAffMsa">
		<xsl:param name="lexEntry"/>
		<xsl:param name="gloss"/>
		<xsl:param name="allos"/>
		<xsl:variable name="inflMsa" select="."/>
		<xsl:variable name="ThisInflAffixSlot">
			<xsl:value-of select="$inflMsa/Slots/@dst"/>
		</xsl:variable>
		<xsl:variable name="Slot" select="$InflAffixSlots[@Id=$ThisInflAffixSlot]"/>
		<xsl:variable name="sTypes">
			<xsl:call-template name="GetAffixMorphType">
				<xsl:with-param name="allos" select="$allos"/>
			</xsl:call-template>
		</xsl:variable>
		<xsl:choose>
			<xsl:when test="key('MorphTypeID', $lexEntry/LexemeForm/@MorphType)/@Guid=$sCircumfix">
				<!-- is a circumfix entry -->
				<xsl:variable name="Slots" select="$inflMsa/Slots"/>
				<xsl:variable name="CircumfixPrefixSlots" select="$AllPrefixSlots[@dst=$Slots/@dst]"/>
				<xsl:variable name="PrefixSlots" select="$InflAffixSlots[@Id=$CircumfixPrefixSlots/@dst]"/>
				<xsl:variable name="CircumfixSuffixSlots" select="$AllSuffixSlots[@dst=$Slots/@dst]"/>
				<xsl:variable name="SuffixSlots" select="$InflAffixSlots[@Id=$CircumfixSuffixSlots/@dst]"/>
				<xsl:if test="contains($sTypes, 'prefix')">
					<xsl:call-template name="DoInflAffix">
						<xsl:with-param name="inflMsa" select="$inflMsa"/>
						<xsl:with-param name="gloss" select="$gloss"/>
						<xsl:with-param name="sTypes">prefix</xsl:with-param>
						<xsl:with-param name="Slot" select="$PrefixSlots[1]"/>
						<xsl:with-param name="lexEntry" select="$lexEntry"/>
						<xsl:with-param name="allos" select="$allos"/>
					</xsl:call-template>
					<xsl:call-template name="CircumfixMCCs">
						<xsl:with-param name="morphname" select="@Id"/>
					</xsl:call-template>
				</xsl:if>
				<xsl:if test="contains($sTypes, 'infix')">
					<xsl:if test="contains($sTypes,'prefix')">
						<!-- only need to output \lx if there was a prefix -->
						<xsl:text>

\lx </xsl:text>
						<xsl:value-of select="@Id"/>
					</xsl:if>
					<xsl:call-template name="DoInflAffix">
						<xsl:with-param name="inflMsa" select="$inflMsa"/>
						<xsl:with-param name="gloss" select="$gloss"/>
						<xsl:with-param name="sTypes">infix</xsl:with-param>
						<xsl:with-param name="Slot" select="$PrefixSlots[1]"/>
						<xsl:with-param name="lexEntry" select="$lexEntry"/>
						<xsl:with-param name="allos" select="$allos"/>
					</xsl:call-template>
					<xsl:call-template name="CircumfixMCCs">
						<xsl:with-param name="morphname" select="@Id"/>
					</xsl:call-template>
				</xsl:if>
				<xsl:if test="contains($sTypes, 'infixinfix')">
					<xsl:text>

\lx </xsl:text>
					<xsl:value-of select="@Id"/>
					<xsl:call-template name="DoInflAffix">
						<xsl:with-param name="inflMsa" select="$inflMsa"/>
						<xsl:with-param name="gloss" select="$gloss"/>
						<xsl:with-param name="sTypes">infix</xsl:with-param>
						<xsl:with-param name="Slot" select="$PrefixSlots[2]"/>
						<xsl:with-param name="lexEntry" select="$lexEntry"/>
						<xsl:with-param name="allos" select="$allos"/>
					</xsl:call-template>
					<xsl:call-template name="CircumfixMCCs">
						<xsl:with-param name="morphname" select="@Id"/>
					</xsl:call-template>
				</xsl:if>
				<xsl:if test="contains($sTypes, 'suffix')">
					<xsl:text>

\lx </xsl:text>
					<xsl:value-of select="@Id"/>
					<xsl:call-template name="DoInflAffix">
						<xsl:with-param name="inflMsa" select="$inflMsa"/>
						<xsl:with-param name="gloss" select="$gloss"/>
						<xsl:with-param name="sTypes">suffix</xsl:with-param>
						<xsl:with-param name="Slot" select="$SuffixSlots[1]"/>
						<xsl:with-param name="lexEntry" select="$lexEntry"/>
						<xsl:with-param name="allos" select="$allos"/>
					</xsl:call-template>
					<xsl:call-template name="CircumfixMCCs">
						<xsl:with-param name="morphname" select="@Id"/>
					</xsl:call-template>
				</xsl:if>
			</xsl:when>
			<xsl:otherwise>
				<!-- non-circumfix entry -->
				<xsl:call-template name="DoInflAffix">
					<xsl:with-param name="inflMsa" select="$inflMsa"/>
					<xsl:with-param name="gloss" select="$gloss"/>
					<xsl:with-param name="sTypes" select="$sTypes"/>
					<xsl:with-param name="Slot" select="$Slot"/>
					<xsl:with-param name="lexEntry" select="$lexEntry"/>
					<xsl:with-param name="allos" select="$allos"/>
				</xsl:call-template>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
		MoStemMsa
	-->
	<xsl:template match="MoStemMsa">
		<xsl:param name="lexEntry"/>
		<xsl:param name="stemMsa" select="."/>
		<xsl:param name="gloss"/>
		<xsl:param name="allos"/>
		<!-- A stemMsa can contain both a root and a clitic.
			In XAmple, we need to create distinct entries for these.  Clitics are like affixes. -->
		<xsl:variable name="sTypes">
			<xsl:for-each select="$allos">
				<xsl:variable name="sMorphType">
					<xsl:value-of select="key('MorphTypeID', key('StemAlloID',@dst)/@MorphType)/@Guid"/>
				</xsl:variable>
				<xsl:choose>
					<xsl:when test="$sMorphType=$sClitic">Clitic</xsl:when>
					<xsl:when test="$sMorphType=$sProclitic">Proclitic</xsl:when>
					<xsl:when test="$sMorphType=$sEnclitic">Enclitic</xsl:when>
					<xsl:when test="$sMorphType=$sRoot or $sMorphType=$sStem or $sMorphType=$sBoundRoot or $sMorphType=$sBoundStem or $sMorphType=$sPhrase">Root</xsl:when>
					<xsl:when test="$sMorphType=$sParticle">RootParticle</xsl:when>
				</xsl:choose>
			</xsl:for-each>
		</xsl:variable>
		<!--
			process any roots
		-->
		<xsl:if test="contains($sTypes,'Root')">
			<!-- both types use the same gloss -->
			<xsl:call-template name="Gloss">
				<xsl:with-param name="gloss" select="$gloss"/>
			</xsl:call-template>
			<xsl:text>
\c</xsl:text>
			<xsl:choose>
				<xsl:when test="contains($sTypes,'Particle')"> Prt</xsl:when>
				<xsl:otherwise> W</xsl:otherwise>
			</xsl:choose>
			<xsl:text>
\wc </xsl:text>
			<xsl:if test="$stemMsa/@Components">Stem</xsl:if>
			<xsl:if test="not($stemMsa/@Components)">root</xsl:if>
			<xsl:call-template name="DoStemMsaProperties">
				<xsl:with-param name="stemMsa" select="$stemMsa"/>
				<xsl:with-param name="allos" select="$allos"/>
			</xsl:call-template>
			<xsl:call-template name="DoStemMsaStemNamesAndAllos">
				<xsl:with-param name="allos" select="$allos"/>
			</xsl:call-template>
		</xsl:if>
		<!--
			process any clitics
		 -->
		<xsl:if test="contains($sTypes, 'Proclitic')">
			<xsl:call-template name="CliticAsAffix">
				<xsl:with-param name="sTypes">
					<xsl:value-of select="$sTypes"/>
				</xsl:with-param>
				<xsl:with-param name="sCliticType">Proclitic</xsl:with-param>
				<xsl:with-param name="sCliticTypeGuid">
					<xsl:value-of select="$sProclitic"/>
				</xsl:with-param>
				<xsl:with-param name="gloss" select="$gloss"/>
				<xsl:with-param name="stemMsa" select="$stemMsa"/>
				<xsl:with-param name="lexEntry" select="$lexEntry"/>
				<xsl:with-param name="allos" select="$allos"/>
			</xsl:call-template>
			<xsl:call-template name="CliticAsRoot">
				<xsl:with-param name="sTypes">
					<xsl:value-of select="$sTypes"/>
				</xsl:with-param>
				<xsl:with-param name="sCliticTypeGuid">
					<xsl:value-of select="$sProclitic"/>
				</xsl:with-param>
				<xsl:with-param name="gloss" select="$gloss"/>
				<xsl:with-param name="stemMsa" select="$stemMsa"/>
				<xsl:with-param name="bOutputLxField" select="'Y'"/>
				<xsl:with-param name="lexEntry" select="$lexEntry"/>
				<xsl:with-param name="allos" select="$allos"/>
			</xsl:call-template>
		</xsl:if>
		<xsl:if test="contains($sTypes,'Enclitic')">
			<xsl:call-template name="CliticAsAffix">
				<xsl:with-param name="sTypes">
					<xsl:value-of select="$sTypes"/>
				</xsl:with-param>
				<xsl:with-param name="sCliticType">Enclitic</xsl:with-param>
				<xsl:with-param name="sCliticTypeGuid">
					<xsl:value-of select="$sEnclitic"/>
				</xsl:with-param>
				<xsl:with-param name="gloss" select="$gloss"/>
				<xsl:with-param name="stemMsa" select="$stemMsa"/>
				<xsl:with-param name="lexEntry" select="$lexEntry"/>
				<xsl:with-param name="allos" select="$allos"/>
			</xsl:call-template>
			<xsl:call-template name="CliticAsRoot">
				<xsl:with-param name="sTypes">
					<xsl:value-of select="$sTypes"/>
				</xsl:with-param>
				<xsl:with-param name="sCliticTypeGuid">
					<xsl:value-of select="$sEnclitic"/>
				</xsl:with-param>
				<xsl:with-param name="gloss" select="$gloss"/>
				<xsl:with-param name="stemMsa" select="$stemMsa"/>
				<xsl:with-param name="bOutputLxField" select="'Y'"/>
				<xsl:with-param name="lexEntry" select="$lexEntry"/>
				<xsl:with-param name="allos" select="$allos"/>
			</xsl:call-template>
		</xsl:if>
		<xsl:if test="contains($sTypes,'Clitic')">
			<xsl:call-template name="CliticAsRoot">
				<xsl:with-param name="sTypes">
					<xsl:value-of select="$sTypes"/>
				</xsl:with-param>
				<xsl:with-param name="sCliticTypeGuid">
					<xsl:value-of select="$sClitic"/>
				</xsl:with-param>
				<xsl:with-param name="gloss" select="$gloss"/>
				<xsl:with-param name="stemMsa" select="$stemMsa"/>
				<xsl:with-param name="lexEntry" select="$lexEntry"/>
				<xsl:with-param name="allos" select="$allos"/>
			</xsl:call-template>
		</xsl:if>
	</xsl:template>
	<!--
		Slots
	-->
	<xsl:template match="Slots">
		<xsl:variable name="slot" select="key('AffixSlotsID',@dst)"/>
		<xsl:if test="$slot/@Optional='false'">
			<!-- for each required slot, create a null entry so the template will still pass -->
			<xsl:text>

\lx </xsl:text>
			<xsl:value-of select="../@Id"/>
			<xsl:variable name="sGloss">
				<xsl:value-of select="$sIrregularlyInflectedFormInSlot"/>
				<xsl:value-of select="@dst"/>
			</xsl:variable>
			<xsl:call-template name="Gloss">
				<xsl:with-param name="gloss" select="$sGloss"/>
			</xsl:call-template>
			<xsl:text>
\c W/W
\wc </xsl:text>
			<xsl:value-of select="@dst"/>
			<xsl:text>
\a 0 {</xsl:text>
			<xsl:value-of select="../@Id"/>
			<xsl:text>}
\eType </xsl:text>
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
			<xsl:choose>
				<xsl:when test="$fIsPrefix='Y'">
					<xsl:text>prefix</xsl:text>
				</xsl:when>
				<xsl:otherwise>
					<xsl:text>suffix</xsl:text>
				</xsl:otherwise>
			</xsl:choose>
			<xsl:if test="../InflectionFeatures/FsFeatStruc">
				<xsl:text>
\fd </xsl:text>
				<xsl:value-of select="$sMSFS"/>
				<xsl:value-of select="../InflectionFeatures/FsFeatStruc/@Id"/>
			</xsl:if>
			<xsl:text>
\mcc </xsl:text>
			<xsl:value-of select="../@Id"/>
			<xsl:choose>
				<xsl:when test="$fIsPrefix='Y'">
					<xsl:text> +/ _ ... {</xsl:text>
					<xsl:value-of select="$sIrregularlyInflectedForm"/>
					<xsl:value-of select="../@Id"/>
					<xsl:text>}</xsl:text>
				</xsl:when>
				<xsl:otherwise>
					<xsl:text> +/ {</xsl:text>
					<xsl:value-of select="$sIrregularlyInflectedForm"/>
					<xsl:value-of select="../@Id"/>
					<xsl:text>} ... _ </xsl:text>
				</xsl:otherwise>
			</xsl:choose>
			<xsl:text>&#xa;</xsl:text>
		</xsl:if>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
AffixType
	Output affix type field contents
		Parameters: sTypes
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="AffixType">
		<xsl:param name="sTypes"/>
		<xsl:param name="allos"/>
\eType<xsl:if test="contains($sTypes,'prefix') or contains($sTypes,'proclitic')"> prefix</xsl:if>
		<xsl:if test="contains($sTypes,'suffix') or contains($sTypes,'enclitic')"> suffix</xsl:if>
		<xsl:if test="contains($sTypes,'prefixing interfix') or contains($sTypes,'suffixing interfix')"> nterfix</xsl:if>
		<xsl:if test="contains($sTypes,'infix')"> infix<!-- following assumes an infix occurs in prefix and/or root;  not always the case... -->
			<xsl:if test="contains($sTypes,'infixing interfix')"> nterfix</xsl:if>
\loc prefix root<xsl:for-each select="$allos">
				<xsl:for-each select="key('AffixAlloID',@dst)/Position">
					<xsl:for-each select="key('PhEnvID', @dst)">
						<xsl:call-template name="PhEnvironment"/>
					</xsl:for-each>
					<xsl:if test="position() != last()">
						<!-- force a newline in case there are any comments in the output -->
						<xsl:text>&#x20;
</xsl:text>
					</xsl:if>
				</xsl:for-each>
			</xsl:for-each>
		</xsl:if>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
AlloEnvironment
	Output an environmental context
		(section 1.4	"Using PhEnvironments to build SECs"  in design doc)
		Parameters: context = context to format
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="AlloEnvironment">
		<xsl:param name="context"/>
		<xsl:for-each select="key('PhContextID', $context)">
			<!-- doing <xsl:value-of select="@Id"/> <xsl:value-of select="name(.)"/> -->
			<xsl:choose>
				<xsl:when test="name()='PhSequenceContext'">
					<xsl:for-each select="Members">
						<xsl:sort select="@ord"/>
						<xsl:call-template name="AlloEnvironment">
							<xsl:with-param name="context" select="@dst"/>
						</xsl:call-template>
					</xsl:for-each>
				</xsl:when>
				<xsl:when test="name()='PhSimpleContextSeg'">
					<xsl:text>&#x20;</xsl:text>
					<xsl:value-of select="key('PhonemeID', @dst)/Codes/PhCode/Representation"/>
				</xsl:when>
				<xsl:when test="name()='PhSimpleContextNC'">
					<xsl:text> [</xsl:text>
					<xsl:value-of select="@dst"/>
					<xsl:text>]</xsl:text>
				</xsl:when>
				<xsl:when test="name()='PhSimpleContextBdry' and key('PhBdryID', @dst)/Name='word'">
					<xsl:text>#</xsl:text>
				</xsl:when>
				<xsl:when test="name()='PhIterationContext'">
					<xsl:variable name="min">
						<xsl:value-of select="number(@Minimum)"/>
					</xsl:variable>
					<xsl:variable name="max">
						<xsl:value-of select="number(@Maximum)"/>
					</xsl:variable>
					<xsl:variable name="iterContext" select="."/>
					<xsl:for-each select="$PhIters">
						<xsl:variable name="count">
							<xsl:number/>
						</xsl:variable>
						<xsl:if test="number($count) &lt;= $max">
							<xsl:text>&#x20;</xsl:text>
							<xsl:if test="number($count) &gt; $min">(</xsl:if>
							<xsl:call-template name="AlloEnvironment">
								<xsl:with-param name="context" select="$iterContext/Member/@dst"/>
							</xsl:call-template>
							<xsl:if test="number($count) &gt; $min">)</xsl:if>
						</xsl:if>
					</xsl:for-each>
				</xsl:when>
			</xsl:choose>
		</xsl:for-each>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
AlloForm
	Output allomorph forms (i.e. beginning of an \a field)
		Parameters: bAffix = flag for whether its an affix or not
							  lexEntry = lexical entry of this allomorph
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="AlloForm">
		<xsl:param name="bAffix"/>
		<xsl:param name="sTypes"/>
		<xsl:param name="sStemNamesUsed"/>
		<xsl:param name="sGuid"/>
		<xsl:param name="sAllosNotConditionedByFeatures"/>
\a <xsl:choose>
			<xsl:when test="not(name()='MoStemAllomorph' or name()='MoAffixAllomorph')">
				<!-- should never happen -->
				<xsl:value-of select="name()"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:variable name="sOrigForm" select="normalize-space(Form)"/>
				<xsl:variable name="sForm">
					<xsl:choose>
						<xsl:when test="contains($sOrigForm, $sFullRedupPattern)">
							<!-- special case for full reduplication; we use [...] (like Shoebox/Toolbox does, but AMPLE uses <...> -->
							<xsl:variable name="sBefore" select="substring-before($sOrigForm, $sFullRedupPattern)"/>
							<xsl:variable name="sAfter" select="substring-after($sOrigForm, $sFullRedupPattern)"/>
							<xsl:value-of select="$sBefore"/>
							<xsl:text>&lt;...&gt;</xsl:text>
							<xsl:value-of select="$sAfter"/>
						</xsl:when>
						<xsl:otherwise>
							<!-- strip any leading or traillng whitespace and convert all internal whitespace to a single space; then
			  convert any internal spaces to periods.  The latter will cause it to fail to analyze, but avoids an error message  -->
							<xsl:value-of select="translate(normalize-space(Form), ' ', '.')"/>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:variable>
				<xsl:choose>
					<!-- allow various ways to indicate a null: ^0, *0, &0, or the Unicode empty set character -->
					<xsl:when test="$sForm='^0' or $sForm='&amp;0' or $sForm='*0' or $sForm='∅'">0</xsl:when>
					<xsl:otherwise>
						<xsl:choose>
							<xsl:when test="contains($sForm,'[')">
								<xsl:call-template name="NatClassStringToHvo">
									<xsl:with-param name="sIn">
										<xsl:value-of select="$sForm"/>
									</xsl:with-param>
								</xsl:call-template>
							</xsl:when>
							<xsl:otherwise>
								<xsl:value-of select="$sForm"/>
							</xsl:otherwise>
						</xsl:choose>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:otherwise>
		</xsl:choose>
		<xsl:text> {</xsl:text>
		<xsl:value-of select="@Id"/>
		<xsl:text>}</xsl:text>
		<xsl:choose>
			<xsl:when test="$bAffix='Y'">
				<xsl:call-template name="MSEnvFS">
					<xsl:with-param name="sAllosNotConditionedByFeatures" select="$sAllosNotConditionedByFeatures"/>
				</xsl:call-template>
				<xsl:call-template name="MSEnvPos"/>
				<xsl:call-template name="InflClass">
					<xsl:with-param name="sTypes" select="$sTypes"/>
				</xsl:call-template>
			</xsl:when>
			<xsl:otherwise>
				<xsl:if test="$sGuid=$sBoundRoot or $sGuid=$sBoundStem">
					<xsl:text> Bound</xsl:text>
				</xsl:if>
				<xsl:choose>
					<xsl:when test="@StemName!=0">
						<xsl:variable name="sStemName">
							<xsl:text>StemName</xsl:text>
							<xsl:value-of select="@StemName"/>
						</xsl:variable>
						<!-- output allomorph property -->
						<xsl:text>&#x20;</xsl:text>
						<xsl:value-of select="$sStemName"/>
						<xsl:variable name="sStemNameAffix">
							<xsl:text>StemNameAffix</xsl:text>
							<xsl:value-of select="@StemName"/>
						</xsl:variable>
						<!-- output two MECs for stem name -->
						<xsl:text> +/ _ ... {</xsl:text>
						<xsl:value-of select="$sStemNameAffix"/>
						<xsl:text>} +/ {</xsl:text>
						<xsl:value-of select="$sStemNameAffix"/>
						<xsl:text>} ... _ </xsl:text>
						<!-- allow possibility of derivational affix or root compounding -->
						<xsl:text> +/ _ ... [DerivAffix] +/ [DerivAffix] ... _ +/ _ ... {root} +/ {root} ... _</xsl:text>
					</xsl:when>
					<xsl:when test="string-length($sStemNamesUsed) &gt; 0">
						<!-- output allomorph property -->
						<xsl:text> Not</xsl:text>
						<xsl:value-of select="$sStemNamesUsed"/>
					</xsl:when>
				</xsl:choose>
			</xsl:otherwise>
		</xsl:choose>
		<xsl:for-each select="./PhoneEnviron | ./PhoneEnv">
			<xsl:for-each select="key('PhEnvID', @dst)">
				<xsl:call-template name="PhEnvironment"/>
			</xsl:for-each>
		</xsl:for-each>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CircumfixMCCs
	Output mccs for a circumfix
		Parameters: morphname = morphname to use in mccs
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="CircumfixMCCs">
		<xsl:param name="morphname"/>
\mcc <xsl:value-of select="$morphname"/> +/ _ ... <xsl:value-of select="$morphname"/> +/ <xsl:value-of select="$morphname"/> ... _
</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CliticAsAffix
	Handle a proclitic or enclitc as an affix entry
		Parameters: sTypes = string of types in the lex entry
							 sCliticType = 'Proclitic' or 'Enclitic'
							 sCliticTypeGuid = GUID of the clitic type
							 gloss = gloss node of lex entry
							 stemMsa = stem's msa
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="CliticAsAffix">
		<xsl:param name="sTypes"/>
		<xsl:param name="sCliticType"/>
		<xsl:param name="sCliticTypeGuid"/>
		<xsl:param name="gloss"/>
		<xsl:param name="stemMsa"/>
		<xsl:param name="lexEntry"/>
		<xsl:param name="allos"/>
		<xsl:if test="string-length($sTypes) &gt; string-length($sCliticType)">
			<!-- Need to create another record; add space -->

\lx <xsl:value-of select="@Id"/>
		</xsl:if>
		<xsl:call-template name="Gloss">
			<xsl:with-param name="gloss" select="$gloss"/>
		</xsl:call-template>
\c W/W
\wc <xsl:choose>
			<xsl:when test="$sCliticType='Proclitic'">proclitic</xsl:when>
			<xsl:otherwise>enclitic</xsl:otherwise>
		</xsl:choose>
\eType <xsl:choose>
			<xsl:when test="$sCliticType='Proclitic'">prefix</xsl:when>
			<xsl:otherwise>suffix</xsl:otherwise>
		</xsl:choose>
\o <xsl:if test="$sCliticType='Proclitic'">
			<xsl:text>-</xsl:text>
		</xsl:if>
		<xsl:text>32000</xsl:text>
		<xsl:for-each select="$allos">
			<xsl:variable name="currentAllomorph" select="."/>
			<xsl:variable name="thisAllomorph" select="key('StemAlloID',@dst)"/>
			<xsl:for-each select="$thisAllomorph[@IsAbstract='0']">
				<xsl:variable name="sMorphType">
					<xsl:value-of select="key('MorphTypeID', @MorphType)/@Guid"/>
				</xsl:variable>
				<xsl:if test="$sMorphType=$sCliticTypeGuid">
					<xsl:call-template name="AlloForm">
						<xsl:with-param name="bAffix" select="'N'"/>
					</xsl:call-template>
					<xsl:for-each select="$currentAllomorph">
						<xsl:call-template name="GenerateAnyNegSECs">
							<xsl:with-param name="currentAllomorph" select="$thisAllomorph"/>
							<xsl:with-param name="sType" select="$sMorphType"/>
						</xsl:call-template>
					</xsl:for-each>
				</xsl:if>
			</xsl:for-each>
		</xsl:for-each>
\mp <xsl:value-of select="$sCliticType"/>
		<xsl:if test="$stemMsa/InflectionFeatures/FsFeatStruc">
\fd <xsl:value-of select="$sMSFS"/><xsl:value-of select="$stemMsa/InflectionFeatures/FsFeatStruc/@Id"/>
		</xsl:if>
		<xsl:variable name="FromPartsOfSpeech" select="$stemMsa/FromPartsOfSpeech/FromPOS"/>
		<xsl:if test="$FromPartsOfSpeech">
\fd <xsl:for-each select="$FromPartsOfSpeech">
				<xsl:value-of select="$sCliticFromPOS"/>
				<xsl:value-of select="@dst"/>
			</xsl:for-each>
		</xsl:if>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CliticAsRoot
	Handle a proclitic or enclitc as an affix entry
		Parameters: sTypes = string of types in the lex entry
							 sCliticType = 'Proclitic' or 'Enclitic'
							 sCliticTypeGuid = GUID of the clitic type
							 gloss = gloss node of lex entry
							 stemMsa = stem's msa
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="CliticAsRoot">
		<xsl:param name="sTypes"/>
		<xsl:param name="sCliticTypeGuid"/>
		<xsl:param name="gloss"/>
		<xsl:param name="stemMsa"/>
		<xsl:param name="bOutputLxField" select="'N'"/>
		<xsl:param name="lexEntry"/>
		<xsl:param name="allos"/>
		<xsl:if test="$bOutputLxField='Y'">
			<!-- Need to create another record; add space -->

\lx <xsl:value-of select="@Id"/>
		</xsl:if>
		<xsl:call-template name="Gloss">
			<xsl:with-param name="gloss" select="$gloss"/>
		</xsl:call-template>
\c Prt
\wc clitic<xsl:for-each select="$allos">
			<xsl:variable name="currentAllomorph" select="."/>
			<xsl:variable name="thisAllomorph" select="key('StemAlloID',@dst)"/>
			<xsl:for-each select="$thisAllomorph[@IsAbstract='0']">
				<xsl:variable name="sMorphType">
					<xsl:value-of select="key('MorphTypeID', @MorphType)/@Guid"/>
				</xsl:variable>
				<xsl:if test="$sMorphType=$sCliticTypeGuid">
					<xsl:call-template name="AlloForm">
						<xsl:with-param name="bAffix" select="'N'"/>
					</xsl:call-template>
					<xsl:for-each select="$currentAllomorph">
						<xsl:call-template name="GenerateAnyNegSECs">
							<xsl:with-param name="currentAllomorph" select="$thisAllomorph"/>
							<xsl:with-param name="sType" select="$sMorphType"/>
						</xsl:call-template>
					</xsl:for-each>
				</xsl:if>
			</xsl:for-each>
		</xsl:for-each>
\fd <xsl:value-of select="$sCliticPOS"/><xsl:value-of select="$stemMsa/@PartOfSpeech"/>
		<xsl:if test="$stemMsa/InflectionFeatures/FsFeatStruc">
\fd <xsl:value-of select="$sMSFS"/><xsl:value-of select="$stemMsa/InflectionFeatures/FsFeatStruc/@Id"/>
		</xsl:if>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		DetermineIfCompoundRuleCouldCauseOrderClassProblem
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="DetermineIfCompoundRuleCouldCauseOrderClassProblem">
		<xsl:if test="$CompoundRules">
			<!-- check if any compound rules allow same categories as a non-final template -->
			<xsl:variable name="slotId" select="@Id"/>
			<xsl:variable name="nonFinalTemplatesUsingThisSlot" select="$AllAffixTemplates[PrefixSlots/@dst=$slotId or SuffixSlots/@dst=$slotId][@Final='false']"/>
			<xsl:if test="$nonFinalTemplatesUsingThisSlot">
				<xsl:variable name="sNonFinalTemplatePOSes">
					<xsl:value-of select="$sPosIdDivider"/>
					<xsl:for-each select="$nonFinalTemplatesUsingThisSlot">
						<xsl:call-template name="GetPOSIdAndAnyNestedPOSIds">
							<xsl:with-param name="pos" select="../.."/>
						</xsl:call-template>
					</xsl:for-each>
					<xsl:value-of select="$sPosIdDivider"/>
				</xsl:variable>
				<xsl:for-each select="$CompoundRules">
					<xsl:variable name="sLeftMatches">
						<xsl:call-template name="GetAnyMatchingPOSIdOrAnyMatchingNestedPOSIds">
							<xsl:with-param name="pos" select="key('POSID',key('StemMsaID',LeftMsa/@dst)/@PartOfSpeech)"/>
							<xsl:with-param name="sListToLookIn" select="$sNonFinalTemplatePOSes"/>
						</xsl:call-template>
					</xsl:variable>
					<xsl:choose>
						<xsl:when test="contains($sLeftMatches,'Y')">
							<xsl:value-of select="$sLeftMatches"/>
						</xsl:when>
						<xsl:otherwise>
							<xsl:call-template name="GetAnyMatchingPOSIdOrAnyMatchingNestedPOSIds">
								<xsl:with-param name="pos" select="key('POSID',key('StemMsaID',RightMsa/@dst)/@PartOfSpeech)"/>
								<xsl:with-param name="sListToLookIn" select="$sNonFinalTemplatePOSes"/>
							</xsl:call-template>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:for-each>
			</xsl:if>
		</xsl:if>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoAffixAllomorphs
	Generate all affix allomorphs
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="DoAffixAllomorphs">
		<xsl:param name="sTypes">x</xsl:param>
		<xsl:param name="lexEntry"/>
		<xsl:variable name="sType">
			<xsl:choose>
				<xsl:when test="contains($sTypes,'interfix')">
					<xsl:choose>
						<xsl:when test="contains($sTypes,'suffix')">
							<xsl:value-of select="$sSuffixingInterfix"/>
						</xsl:when>
						<!-- for both prefix and infix (although we don't know for sure that infixes will be prefixes... -->
						<xsl:otherwise>
							<xsl:value-of select="$sPrefixingInterfix"/>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:when>
				<xsl:when test="contains($sTypes,'prefix')">
					<xsl:value-of select="$sPrefix"/>
				</xsl:when>
				<xsl:when test="contains($sTypes,'infix')">
					<xsl:value-of select="$sInfix"/>
				</xsl:when>
				<xsl:when test="contains($sTypes,'suffix')">
					<xsl:value-of select="$sSuffix"/>
				</xsl:when>
			</xsl:choose>
		</xsl:variable>
		<xsl:variable name="allomorphs" select="$lexEntry/AlternateForms[key('MorphTypeID', key('AffixAlloID',@dst)/@MorphType)/@Guid=$sType] | $lexEntry/LexemeForm[key('MorphTypeID', key('AffixAlloID',@dst)/@MorphType)/@Guid=$sType]"/>
		<xsl:variable name="allosConditionedByFeatures" select="$MoAffixAllomorphs[@Id=$allomorphs/@dst and MsEnvFeatures/FsFeatStruc[descendant::FsClosedValue]]"/>
		<xsl:variable name="sAllosNotConditionedByFeatures">
			<xsl:if test="count($allosConditionedByFeatures) &gt; 0">
				<xsl:text> MSEnvFS</xsl:text>
				<xsl:for-each select="$allosConditionedByFeatures">
					<xsl:text>Not</xsl:text>
					<xsl:value-of select="MsEnvFeatures/FsFeatStruc/@Id"/>
				</xsl:for-each>
			</xsl:if>
		</xsl:variable>
		<xsl:for-each select="$allomorphs">
			<!-- When a lexical entry has both alternate forms and a lexeme form, we want to order the lexeme form last.
		Since "LexemeForm" comes after "AlternateForms" alphabetically, we sort by the element name to get this effect.
		-->
			<xsl:sort select="name()"/>
			<xsl:variable name="thisAllomorph" select="key('AffixAlloID',@dst)"/>
			<xsl:for-each select="$thisAllomorph[@IsAbstract='0']">
				<xsl:call-template name="AlloForm">
					<xsl:with-param name="bAffix" select="'Y'"/>
					<xsl:with-param name="sTypes" select="$sTypes"/>
					<xsl:with-param name="sAllosNotConditionedByFeatures" select="$sAllosNotConditionedByFeatures"/>
				</xsl:call-template>
			</xsl:for-each>
			<xsl:if test="$thisAllomorph[@IsAbstract='0']">
				<xsl:call-template name="GenerateAnyNegSECs">
					<xsl:with-param name="currentAllomorph" select="$thisAllomorph"/>
					<xsl:with-param name="sType" select="$sType"/>
				</xsl:call-template>
			</xsl:if>
		</xsl:for-each>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoDerivAffix
	Generate a derivational affix
		Parameters: derivMsa = msa node
							 gloss = gloss node
							 sTypes = morpheme types in this entry
							 bCircumfix = flag for if it's a circumfix entry
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="DoDerivAffix">
		<xsl:param name="derivMsa"/>
		<xsl:param name="gloss"/>
		<xsl:param name="sTypes"/>
		<xsl:param name="bCircumfix"/>
		<xsl:param name="lexEntry"/>
		<xsl:param name="allos"/>
		<xsl:call-template name="Gloss">
			<xsl:with-param name="gloss" select="$gloss"/>
		</xsl:call-template>
\c W/W
\o <xsl:if test="contains($sTypes, 'prefix')">
			<xsl:text>-</xsl:text>
		</xsl:if>
		<xsl:text>1</xsl:text>
\wc <xsl:choose>
			<xsl:when test="$bCircumfix='Y'">
				<xsl:choose>
					<xsl:when test="contains($sTypes,'prefix')">derivCircumPfx</xsl:when>
					<xsl:when test="contains($sTypes,'suffix')">derivCircumSfx</xsl:when>
					<!-- following is a guess - we really don't know, do we?? -->
					<xsl:when test="contains($sTypes,'infix')">derivCircumPfx</xsl:when>
				</xsl:choose>
			</xsl:when>
			<xsl:otherwise>
				<xsl:choose>
					<xsl:when test="contains($sTypes,'prefix')">derivPfx</xsl:when>
					<xsl:when test="contains($sTypes,'suffix')">derivSfx</xsl:when>
					<!-- following is a guess - we really don't know, do we?? -->
					<xsl:when test="contains($sTypes,'infix')">derivPfx</xsl:when>
					<xsl:when test="contains($sTypes,'proclitic')">proclitic</xsl:when>
					<xsl:when test="contains($sTypes,'enclitic')">enclitic</xsl:when>
				</xsl:choose>
			</xsl:otherwise>
		</xsl:choose>
		<xsl:call-template name="DoAffixAllomorphs">
			<xsl:with-param name="lexEntry" select="$lexEntry"/>
			<xsl:with-param name="sTypes" select="$sTypes"/>
		</xsl:call-template>
		<xsl:call-template name="AffixType">
			<xsl:with-param name="sTypes" select="$sTypes"/>
			<xsl:with-param name="allos" select="$allos"/>
		</xsl:call-template>
		<xsl:if test="$derivMsa/@FromPartOfSpeech!='0'">
\fd <xsl:value-of select="$sFromPOS"/><xsl:value-of select="$derivMsa/@FromPartOfSpeech"/>
		</xsl:if>
		<xsl:if test="$derivMsa/@ToPartOfSpeech!='0' and $derivMsa/@ToPartOfSpeech!=$derivMsa/@FromPartOfSpeech">
\fd <xsl:value-of select="$sToPOS"/><xsl:value-of select="$derivMsa/@ToPartOfSpeech"/>
		</xsl:if>
		<xsl:if test="$derivMsa/@FromInflectionClass!='0'">
\fd <xsl:value-of select="$sFromInflClass"/><xsl:value-of select="$derivMsa/@FromInflectionClass"/>
		</xsl:if>
		<xsl:if test="$derivMsa/@ToInflectionClass!='0'">
\mp <xsl:value-of select="$sToInflClass"/><xsl:value-of select="$derivMsa/@ToInflectionClass"/>
		</xsl:if>
		<xsl:if test="$derivMsa/FromMsFeatures/FsFeatStruc">
\fd <xsl:value-of select="$sFromMSFS"/><xsl:value-of select="$derivMsa/FromMsFeatures/FsFeatStruc/@Id"/>
		</xsl:if>
		<xsl:if test="$derivMsa/ToMsFeatures/FsFeatStruc">
\fd <xsl:value-of select="$sToMSFS"/><xsl:value-of select="$derivMsa/ToMsFeatures/FsFeatStruc/@Id"/>
		</xsl:if>
		<xsl:for-each select="$derivMsa/FromProdRestrict">
			<xsl:call-template name="OutputExcpFeats">
				<xsl:with-param name="sTypes" select="$sTypes"/>
				<xsl:with-param name="sValue" select="$derivMsa/@Id"/>
			</xsl:call-template>
		</xsl:for-each>
		<xsl:for-each select="$derivMsa/ToProdRestrict">
\mp <xsl:value-of select="$sToExceptionFeature"/><xsl:value-of select="@dst"/>Plus</xsl:for-each>
		<xsl:if test="$derivMsa/@FromStemName!=0">
			<xsl:variable name="sFromStemName">
				<xsl:text>StemName</xsl:text>
				<xsl:value-of select="$derivMsa/@FromStemName"/>
			</xsl:variable>
\mp <xsl:text>StemNameAffix</xsl:text><xsl:value-of select="$derivMsa/@FromStemName"/>
\mcc <xsl:value-of select="$derivMsa/@Id"/>
			<xsl:text> +/ {</xsl:text>
			<xsl:value-of select="$sFromStemName"/>
			<xsl:text>} ... _ +/ _ ... {</xsl:text>
			<xsl:value-of select="$sFromStemName"/>
			<xsl:text>}</xsl:text>
		</xsl:if>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		DoInflAffix
		Generate an inflectional affix
		Parameters: inflMsa = msa node
		gloss = gloss node
		sTypes = morpheme types in this entry
		Slot = slot in template
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="DoInflAffix">
		<xsl:param name="inflMsa"/>
		<xsl:param name="gloss"/>
		<xsl:param name="sTypes"/>
		<xsl:param name="Slot"/>
		<xsl:param name="lexEntry"/>
		<xsl:param name="allos"/>
		<xsl:call-template name="Gloss">
			<xsl:with-param name="gloss" select="$gloss"/>
		</xsl:call-template>
		<xsl:text>
\c W/W
\o </xsl:text>
		<xsl:choose>
			<xsl:when test="$Slot">
				<!-- is really an inflectional affix -->
				<xsl:variable name="sSign">
					<xsl:if test="contains($sTypes, 'prefix') or contains($sTypes, 'infix')">
						<xsl:text>-</xsl:text>
					</xsl:if>
				</xsl:variable>
				<xsl:for-each select="$Slot">
					<xsl:variable name="bCompoundRuleCouldCauseOrderClassProblem">
						<xsl:call-template name="DetermineIfCompoundRuleCouldCauseOrderClassProblem"/>
					</xsl:variable>
					<xsl:variable name="sMinOrderClass">
						<xsl:choose>
							<xsl:when test="contains($bCompoundRuleCouldCauseOrderClassProblem,'Y')">
								<xsl:text>0</xsl:text>
							</xsl:when>
							<xsl:when test="contains($sTypes, 'prefix') or contains($sTypes, 'infix')">
								<!-- prefix orderclass is negative so need to flip -->
								<xsl:value-of select="orderclass/maxValue"/>
							</xsl:when>
							<xsl:otherwise>
								<xsl:value-of select="orderclass/minValue"/>
							</xsl:otherwise>
						</xsl:choose>
					</xsl:variable>
					<xsl:variable name="sMaxOrderClass">
						<xsl:choose>
							<xsl:when test="contains($bCompoundRuleCouldCauseOrderClassProblem,'Y')">
								<xsl:text>0</xsl:text>
							</xsl:when>
							<!-- prefix orderclass is negative so need to flip -->
							<xsl:when test="contains($sTypes, 'prefix') or contains($sTypes, 'infix')">
								<xsl:value-of select="orderclass/minValue"/>
							</xsl:when>
							<xsl:otherwise>
								<xsl:value-of select="orderclass/maxValue"/>
							</xsl:otherwise>
						</xsl:choose>
					</xsl:variable>
					<xsl:call-template name="OutputOrderClass">
						<xsl:with-param name="sSign">
							<xsl:value-of select="$sSign"/>
						</xsl:with-param>
						<xsl:with-param name="sValue">
							<xsl:value-of select="$sMinOrderClass"/>
						</xsl:with-param>
					</xsl:call-template>
					<xsl:call-template name="OutputOrderClass">
						<xsl:with-param name="sSign">
							<xsl:value-of select="$sSign"/>
						</xsl:with-param>
						<xsl:with-param name="sValue">
							<xsl:value-of select="$sMaxOrderClass"/>
						</xsl:with-param>
					</xsl:call-template>
				</xsl:for-each>
			</xsl:when>
			<xsl:otherwise>
				<!-- user has marked as inflectional, but it does not appear in any template; treat as unmarked -->
				<xsl:call-template name="OutputUnclassifiedOrderClass">
					<xsl:with-param name="sTypes" select="$sTypes"/>
				</xsl:call-template>
			</xsl:otherwise>
		</xsl:choose>
		<xsl:text>
\wc </xsl:text>
		<!-- slot name -->
		<xsl:choose>
			<xsl:when test="$Slot">
				<!-- is really an inflectional affix -->
				<xsl:for-each select="$Slot">
					<xsl:value-of select="@Id"/>
					<xsl:variable name="slotsInLexEntryInflType" select="key('LexEntryInflTypeSlots',@Id)"/>
					<xsl:if test="$slotsInLexEntryInflType">
						<xsl:for-each select="$slotsInLexEntryInflType">
							<xsl:text>
\mcc </xsl:text>
							<xsl:value-of select="$inflMsa/@Id"/>
							<xsl:text> +/ {</xsl:text>
							<xsl:value-of select="$sIrregularlyInflectedForm"/>
							<xsl:value-of select="../@Id"/>
							<xsl:text>} ... ~_</xsl:text>
						</xsl:for-each>
					</xsl:if>
				</xsl:for-each>
			</xsl:when>
			<xsl:otherwise>
				<!-- user has marked as inflectional, but it does not appear in any template; treat as unmarked -->
				<xsl:if test="contains($sTypes,'prefix')"> prefix</xsl:if>
				<xsl:if test="contains($sTypes,'suffix')"> suffix</xsl:if>
				<!-- following is a guess - we really don't know, do we?? -->
				<xsl:if test="contains($sTypes,'infix')"> prefix</xsl:if>
				<xsl:if test="contains($sTypes,'interfix')"> interfix</xsl:if>
				<xsl:if test="$inflMsa/@PartOfSpeech!='0'">
\fd <xsl:value-of select="$sFromPOS"/><xsl:value-of select="$inflMsa/@PartOfSpeech"/>
				</xsl:if>
			</xsl:otherwise>
		</xsl:choose>
		<xsl:if test="$inflMsa/InflectionFeatures/FsFeatStruc">
\fd <xsl:value-of select="$sInflectionFS"/><xsl:value-of select="$inflMsa/InflectionFeatures/FsFeatStruc/@Id"/>
		</xsl:if>
		<xsl:for-each select="$inflMsa/FromProdRestrict">
			<xsl:call-template name="OutputExcpFeats">
				<xsl:with-param name="sTypes" select="$sTypes"/>
				<xsl:with-param name="sValue" select="$inflMsa/@Id"/>
			</xsl:call-template>
		</xsl:for-each>
		<xsl:call-template name="DoAffixAllomorphs">
			<xsl:with-param name="lexEntry" select="$lexEntry"/>
			<xsl:with-param name="sTypes" select="$sTypes"/>
		</xsl:call-template>
		<xsl:call-template name="AffixType">
			<xsl:with-param name="sTypes" select="$sTypes"/>
			<xsl:with-param name="allos" select="$allos"/>
		</xsl:call-template>
		<xsl:call-template name="DoInflAffixMsaStemName">
			<xsl:with-param name="fs" select="$inflMsa/InflectionFeatures/FsFeatStruc"/>
			<xsl:with-param name="posid" select="$inflMsa/@PartOfSpeech"/>
		</xsl:call-template>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoInflAffixMsaStemName
	Process stem name information for an inflectional affix msa; recurses up POS chain
		Parameters: fs = feature structure of the inflectional affix msa
							 posid = Part of Speech id
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="DoInflAffixMsaStemName">
		<xsl:param name="fs"/>
		<xsl:param name="posid"/>
		<xsl:for-each select="key('POSID', $posid)">
			<xsl:for-each select="StemNames/MoStemName">
				<xsl:variable name="stemName" select="."/>
				<xsl:variable name="bHasFeatureInRegion">
					<xsl:for-each select="$fs/descendant::FsClosedValue">
						<xsl:variable name="sFeature" select="@Feature"/>
						<xsl:variable name="sValue" select="@Value"/>
						<xsl:if test="$stemName/Regions/descendant::FsClosedValue[@Feature=$sFeature and @Value=$sValue]">Y</xsl:if>
					</xsl:for-each>
				</xsl:variable>
				<xsl:if test="contains($bHasFeatureInRegion,'Y')">
\mp <xsl:value-of select="$sStemName"/>
					<xsl:text>Affix</xsl:text>
					<xsl:value-of select="@Id"/>
				</xsl:if>
			</xsl:for-each>
			<!-- now check parent POS -->
			<xsl:for-each select="$PartsOfSpeech/SubPossibilities[@dst=$posid]">
				<xsl:call-template name="DoInflAffixMsaStemName">
					<xsl:with-param name="fs" select="$fs"/>
					<xsl:with-param name="posid" select="../@Id"/>
				</xsl:call-template>
			</xsl:for-each>
		</xsl:for-each>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		DoStemMsaProperties
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="DoStemMsaProperties">
		<xsl:param name="stemMsa"/>
		<xsl:param name="allos"/>
		<xsl:if test="$stemMsa/@PartOfSpeech">
			<xsl:text>
\mp </xsl:text>
			<xsl:value-of select="$sRootPOS"/>
			<xsl:value-of select="$stemMsa/@PartOfSpeech"/>
		</xsl:if>
		<xsl:if test="$stemMsa/@InflectionClass!='0'">
			<xsl:text>
\mp </xsl:text>
			<xsl:value-of select="$sInflClass"/>
			<xsl:value-of select="$stemMsa/@InflectionClass"/>
		</xsl:if>
		<xsl:if test="$stemMsa/@InflectionClass='0'">
			<!-- need to look at ancestor POSes, too -->
			<xsl:variable name="default">
				<xsl:call-template name="NestedPOSDefaultInflectionClass">
					<xsl:with-param name="pos" select="key('POSID',$stemMsa/@PartOfSpeech)"/>
				</xsl:call-template>
			</xsl:variable>
			<xsl:if test="$default!=''">
				<xsl:text>
\mp </xsl:text>
				<xsl:value-of select="$sInflClass"/>
				<xsl:value-of select="$default"/>
			</xsl:if>
		</xsl:if>
		<xsl:if test="$stemMsa/InflectionFeatures/FsFeatStruc">
			<xsl:text>
\fd </xsl:text>
			<xsl:value-of select="$sMSFS"/>
			<xsl:value-of select="$stemMsa/InflectionFeatures/FsFeatStruc/@Id"/>
		</xsl:if>
		<xsl:for-each select="$stemMsa/ProdRestrict">
			<xsl:text>
\mp </xsl:text>
			<xsl:value-of select="$sExceptionFeature"/>
			<xsl:value-of select="@dst"/>Plus</xsl:for-each>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		DoStemMsaStemNamesAndAllos
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="DoStemMsaStemNamesAndAllos">
		<xsl:param name="allos"/>
		<!-- need lex entry for stem name processing -->
		<!-- collect stem names used so we can output any default stem name allomorph properties -->
		<xsl:variable name="stemallos" select="key('StemAlloID', $allos/@dst)[@IsAbstract='0' and @MorphType!=$sDiscontiguousPhrase and @StemName!='0']"/>
		<xsl:variable name="sStemNamesUsedAll">
			<xsl:for-each select="$stemallos">
				<xsl:sort select="@StemName"/>
				<xsl:text>StemName</xsl:text>
				<xsl:value-of select="@StemName"/>
				<xsl:text>;</xsl:text>
			</xsl:for-each>
		</xsl:variable>
		<xsl:variable name="sStemNamesUsed">
			<xsl:call-template name="OutputUniqueStrings">
				<xsl:with-param name="sList" select="$sStemNamesUsedAll"/>
			</xsl:call-template>
		</xsl:variable>

		<xsl:for-each select="$allos">
			<xsl:variable name="thisAllomorph" select="."/>
			<xsl:for-each select="key('StemAlloID',@dst)[@IsAbstract='0']">
				<xsl:variable name="sMorphType">
					<xsl:value-of select="key('MorphTypeID', @MorphType)/@Guid"/>
				</xsl:variable>
				<xsl:if test="$sMorphType=$sRoot or $sMorphType=$sStem or $sMorphType=$sBoundRoot or $sMorphType=$sBoundStem or $sMorphType=$sParticle or $sMorphType=$sPhrase">
					<xsl:call-template name="AlloForm">
						<xsl:with-param name="bAffix" select="'N'"/>
						<xsl:with-param name="sStemNamesUsed" select="$sStemNamesUsed"/>
						<xsl:with-param name="sGuid" select="$sMorphType"/>
					</xsl:call-template>
					<xsl:for-each select="$thisAllomorph">
						<xsl:call-template name="GenerateAnyNegSECs">
							<xsl:with-param name="currentAllomorph" select="."/>
							<xsl:with-param name="sType" select="$sMorphType"/>
						</xsl:call-template>
					</xsl:for-each>
				</xsl:if>
			</xsl:for-each>
		</xsl:for-each>

	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoUnclassifiedAffix
	Generate a derivational affix
		Parameters: unclassifiedMsa = msa node
							 gloss = gloss node
							 sTypes = morpheme types in this entry
							 bCircumfix = flag for if it's a circumfix entry
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="DoUnclassifiedAffix">
		<xsl:param name="unclassifiedMsa"/>
		<xsl:param name="gloss"/>
		<xsl:param name="sTypes"/>
		<xsl:param name="bCircumfix"/>
		<xsl:param name="lexEntry"/>
		<xsl:param name="allos"/>
		<xsl:call-template name="Gloss">
			<xsl:with-param name="gloss" select="$gloss"/>
		</xsl:call-template>
\c W/W
\o <xsl:call-template name="OutputUnclassifiedOrderClass">
			<xsl:with-param name="sTypes" select="$sTypes"/>
		</xsl:call-template>
\wc <xsl:choose>
			<xsl:when test="contains($sTypes,'interfix')">
				<xsl:choose>
					<xsl:when test="contains($sTypes,'suffix')"> suffixinginterfix</xsl:when>
					<!-- for both prefix and infix (although we don't know for sure that infixes will be prefixes... -->
					<xsl:otherwise> prefixinginterfix</xsl:otherwise>
				</xsl:choose>
			</xsl:when>
			<xsl:when test="$bCircumfix='Y'">
				<xsl:choose>
					<xsl:when test="contains($sTypes,'suffix')">circumSfx</xsl:when>
					<!-- for both prefix and infix (although we don't know for sure that infixes will be prefixes... -->
					<xsl:otherwise>circumPfx</xsl:otherwise>
				</xsl:choose>
			</xsl:when>
			<xsl:when test="contains($sTypes,'suffix')">suffix</xsl:when>
			<!-- for both prefix and infix (although we don't know for sure that infixes will be prefixes... -->
			<xsl:otherwise>prefix</xsl:otherwise>
		</xsl:choose>
		<xsl:call-template name="DoAffixAllomorphs">
			<xsl:with-param name="lexEntry" select="$lexEntry"/>
			<xsl:with-param name="sTypes" select="$sTypes"/>
		</xsl:call-template>
		<xsl:call-template name="AffixType">
			<xsl:with-param name="sTypes" select="$sTypes"/>
			<xsl:with-param name="allos" select="$allos"/>
		</xsl:call-template>
		<xsl:if test="$unclassifiedMsa/@PartOfSpeech!='0'">
\fd <xsl:value-of select="$sFromPOS"/><xsl:value-of select="$unclassifiedMsa/@PartOfSpeech"/>
		</xsl:if>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
GenerateAnyNegSECs
	Generate any Negative SECs needed for the allomorphs
	It assumes that the current context is the LexEntry.
	Note that we need to match inflection classes and stem names, too, when they exist;  i.e. only produce negative SECs for an allomorph with an
	  inflection class/stem name when there are previous allomorphs (having environments)	that also have the same inflection class/stem name.
	Further, only look at previous allomorphs that have similar morphtypes; e.g. for a circumfix with both a prefix and a suffix,
	  don't put athe prefix's negated environment on the suiffix. A similar case exists for an entry having both a root and a proclitic: these get split into
	  separate XAmple entries and are conditioned independently (by environments)
		Parameters: currentAllomorph - the MoAffixAllomorph or MoStemAllomorph to receive any Negative SECs
								sType - the morph type of the allomorph being processed
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="GenerateAnyNegSECs">
		<xsl:param name="currentAllomorph"/>
		<xsl:param name="sType"/>
		<xsl:variable name="sInflClass" select="key('AffixAlloID',@dst)/InflectionClasses/@dst"/>
		<xsl:variable name="sStemName" select="key('StemAlloID',@dst)/@StemName"/>
		<xsl:variable name="environsOfCurrentAllomorph" select="$currentAllomorph/PhoneEnv"/>
		<xsl:for-each select="preceding-sibling::*[name()='AlternateForms' or name()='LexemeForm']">
			<xsl:variable name="sThisMorphType" select="key('MorphTypeID', key('AlloID',@dst)/@MorphType)/@Guid"/>
			<xsl:variable name="bTypesAreCompatible">
				<xsl:call-template name="MorphTypesAreCompatible">
					<xsl:with-param name="sMorphType1" select="$sType"/>
					<xsl:with-param name="sMorphType2" select="$sThisMorphType"/>
				</xsl:call-template>
			</xsl:variable>
			<xsl:if test="$bTypesAreCompatible='Y'">
				<xsl:choose>
					<xsl:when test="string-length($sInflClass) > 0">
						<xsl:for-each select="key('AffixAlloID',@dst)">
							<xsl:if test="InflectionClasses/@dst = $sInflClass">
								<xsl:call-template name="GenNegSECs">
									<xsl:with-param name="environsToSkip" select="$environsOfCurrentAllomorph"/>
								</xsl:call-template>
							</xsl:if>
						</xsl:for-each>
					</xsl:when>
					<xsl:when test="string-length($sStemName) > 0">
						<xsl:for-each select="key('StemAlloID',@dst)">
							<xsl:if test="@StemName = $sStemName">
								<xsl:call-template name="GenNegSECs">
									<xsl:with-param name="environsToSkip" select="$environsOfCurrentAllomorph"/>
								</xsl:call-template>
							</xsl:if>
						</xsl:for-each>
					</xsl:when>
					<xsl:otherwise>
						<xsl:for-each select="key('AlloID',@dst)">
							<xsl:call-template name="GenNegSECs">
								<xsl:with-param name="environsToSkip" select="$environsOfCurrentAllomorph"/>
							</xsl:call-template>
						</xsl:for-each>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:if>
		</xsl:for-each>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
GenNegSECs
	Generate a set of Negative SECs needed for the allomorphs
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="GenNegSECs">
		<xsl:param name="environsToSkip"/>
		<xsl:for-each select="./PhoneEnviron | ./PhoneEnv">
			<xsl:variable name="sThisEnviron">
				<xsl:value-of select="@dst"/>
			</xsl:variable>
			<xsl:if test="not($environsToSkip[@dst=$sThisEnviron])">
				<xsl:for-each select="key('PhEnvID', @dst)">
					<xsl:if test="not(contains(@StringRepresentation, '^'))">
						<!-- following forces a new line which is essential - without it, XAmple won't read the NegSEC -->
						<xsl:text> &#x20;
&#x20;
</xsl:text>
						<xsl:call-template name="PhEnvironment">
							<xsl:with-param name="bNegate">Y</xsl:with-param>
						</xsl:call-template>
					</xsl:if>
				</xsl:for-each>
			</xsl:if>
		</xsl:for-each>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
GetAffixMorphType
	Get morph type of an affix
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="GetAffixMorphType">
		<xsl:param name="allos"/>
		<xsl:for-each select="$allos">
			<xsl:variable name="sMorphType">
				<xsl:value-of select="key('MorphTypeID', key('AffixAlloID',@dst)/@MorphType)/@Guid"/>
			</xsl:variable>
			<xsl:choose>
				<xsl:when test="$sMorphType=$sInfix">infix</xsl:when>
				<xsl:when test="$sMorphType=$sInfixingInterfix">infixing interfix</xsl:when>
				<xsl:when test="$sMorphType=$sPrefix">prefix</xsl:when>
				<xsl:when test="$sMorphType=$sPrefixingInterfix">prefixing interfix</xsl:when>
				<xsl:when test="$sMorphType=$sSuffix">suffix</xsl:when>
				<xsl:when test="$sMorphType=$sSuffixingInterfix">suffixing interfix</xsl:when>
			</xsl:choose>
		</xsl:for-each>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
IsAbstractOnly
	Determine if the lexentry only has abstract forms.  If so, no need to output anything
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="IsAbstractOnly">
		<xsl:if test="key('StemAlloID', LexemeForm/@dst)/@IsAbstract='1' or key('AffixAlloID', LexemeForm/@dst)/@IsAbstract='1'">
			<xsl:for-each select="AlternateForms">
				<xsl:if test="key('StemAlloID', @dst)/@IsAbstract='0' or key('AffixAlloID', @dst)/@IsAbstract='0'">
					<xsl:text>N</xsl:text>
				</xsl:if>
			</xsl:for-each>
			<xsl:text>Y</xsl:text>
		</xsl:if>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		MorphTypesAreCompatible
		Determine if two morph types are compatible
		Parameters: two morph types to compare (assumes are guids)
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="MorphTypesAreCompatible">
		<xsl:param name="sMorphType1"/>
		<xsl:param name="sMorphType2"/>
		<xsl:choose>
			<xsl:when test="$sMorphType1=$sRoot or $sMorphType1=$sStem or $sMorphType1=$sBoundRoot or $sMorphType1=$sBoundStem or $sMorphType1=$sParticle">
				<xsl:if test="$sMorphType2=$sRoot or $sMorphType2=$sStem or $sMorphType2=$sBoundRoot or $sMorphType2=$sBoundStem or $sMorphType2=$sParticle">
					<xsl:text>Y</xsl:text>
				</xsl:if>
			</xsl:when>
			<xsl:when test="$sMorphType1=$sInfix or $sMorphType1=$sInfixingInterfix">
				<xsl:if test="$sMorphType2=$sInfix or $sMorphType2=$sInfixingInterfix">
					<xsl:text>Y</xsl:text>
				</xsl:if>
			</xsl:when>
			<xsl:when test="$sMorphType1=$sPrefix or $sMorphType1=$sPrefixingInterfix">
				<xsl:if test="$sMorphType2=$sPrefix or $sMorphType2=$sPrefixingInterfix">
					<xsl:text>Y</xsl:text>
				</xsl:if>
			</xsl:when>
			<xsl:when test="$sMorphType1=$sSuffix or $sMorphType1=$sSuffixingInterfix">
				<xsl:if test="$sMorphType2=$sSuffix or $sMorphType2=$sSuffixingInterfix">
					<xsl:text>Y</xsl:text>
				</xsl:if>
			</xsl:when>
			<xsl:otherwise>
				<xsl:if test="$sMorphType1=$sMorphType2">
					<xsl:text>Y</xsl:text>
				</xsl:if>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
NatClassStringToHvo
	Convert natural class abbreviations to their corresponding hvo number (we use the hvo in \scl)
	Is recursive (looking for consecutive classes)
		Parameters: sIn - the string representation of the environment
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="NatClassStringToHvo">
		<xsl:param name="sIn"/>
		<xsl:variable name="sNewList" select="concat(normalize-space($sIn),' ')"/>
		<xsl:variable name="sFirst" select="substring-before($sNewList,'[')"/>
		<xsl:variable name="sRest" select="substring-after($sNewList,']')"/>
		<xsl:variable name="sAbbr" select="substring($sNewList, string-length($sFirst)+2, string-length($sNewList)-string-length($sRest) - string-length($sFirst) - 2)"/>
		<!--	 debug output:
NewList = <xsl:value-of select="$sNewList"/>
First = <xsl:value-of select="$sFirst"/>
Rest = <xsl:value-of select="$sRest"/>
Name = <xsl:value-of select="$sAbbr"/>
-->
		<xsl:choose>
			<xsl:when test="$sAbbr and contains($sNewList,'[')">
				<xsl:value-of select="$sFirst"/>
				<xsl:text>[</xsl:text>
				<xsl:choose>
					<xsl:when test="contains($sAbbr,'^')">
						<!-- is reduplication pattern -->
						<xsl:variable name="sBeforeCaret" select="substring-before($sAbbr,'^')"/>
						<xsl:variable name="sAfterCaret" select="substring-after($sAbbr,'^')"/>
						<xsl:for-each select="key('NatClassAbbr', $sBeforeCaret)">
							<xsl:value-of select="../@Id"/>
						</xsl:for-each>
						<xsl:text>^</xsl:text>
						<xsl:value-of select="$sAfterCaret"/>
					</xsl:when>
				</xsl:choose>
				<xsl:for-each select="key('NatClassAbbr', $sAbbr)">
					<xsl:if test="position()=1">
						<!-- only use the first one; if the user has more than one with same abbreviation, we only use the first -->
						<xsl:value-of select="../@Id"/>
					</xsl:if>
				</xsl:for-each>
				<xsl:text>]</xsl:text>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="$sNewList"/>
			</xsl:otherwise>
		</xsl:choose>
		<xsl:if test="$sRest">
			<xsl:call-template name="NatClassStringToHvo">
				<xsl:with-param name="sIn" select="$sRest"/>
			</xsl:call-template>
		</xsl:if>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
NestedPOSDefaultInflectionClass
	Perform a recursive "ascent" through nested PartOfSpeech elements, looking for a non-zero default inflection class
		Parameters: pos = key for current PartOfSpeech
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<!-- recursive pass through POS -->
	<xsl:template name="NestedPOSDefaultInflectionClass">
		<xsl:param name="pos" select="."/>
		<xsl:choose>
			<xsl:when test="$pos/@DefaultInflectionClass!='0'">
				<xsl:value-of select="$pos/@DefaultInflectionClass"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:for-each select="$PartsOfSpeech/SubPossibilities[@dst=$pos/@Id]">
					<xsl:call-template name="NestedPOSDefaultInflectionClass">
						<xsl:with-param name="pos" select=".."/>
					</xsl:call-template>
				</xsl:for-each>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputExcpFeats
	Output exception feature info
		Parameters: sTypes: type of affix
							 sValue: affix morphname
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputExcpFeats">
		<xsl:param name="sTypes"/>
		<xsl:param name="sValue"/>
\mp <xsl:value-of select="$sFromExceptionFeature"/><xsl:value-of select="@dst"/>Plus
\mcc <xsl:value-of select="$sValue"/>
		<xsl:call-template name="OutputAffixMECorMCC">
			<xsl:with-param name="sTypes" select="$sTypes"/>
			<xsl:with-param name="sPreface" select="'ExcpFeat'"/>
			<xsl:with-param name="sValue" select="@dst"/>
			<xsl:with-param name="sPostscript" select="'Plus'"/>
		</xsl:call-template>
		<xsl:call-template name="OutputAffixMECorMCC">
			<xsl:with-param name="sTypes" select="$sTypes"/>
			<xsl:with-param name="sPreface" select="'ToExcpFeat'"/>
			<xsl:with-param name="sValue" select="@dst"/>
			<xsl:with-param name="sPostscript" select="'Plus'"/>
		</xsl:call-template>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
PhEnvironment
	Output the PhEnvironment
		Parameters: bNegate : 'Y' means output as an "Allomorphs Never Co-occur Constraint"
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="PhEnvironment">
		<xsl:param name="bNegate">N</xsl:param>
		<xsl:choose>
			<xsl:when test="@StringRepresentation!=''">
				<xsl:variable name="sNormalizedOrgRep">
					<xsl:if test="$bNegate = 'Y'">
						<xsl:text>~</xsl:text>
					</xsl:if>
					<xsl:value-of select="normalize-space(@StringRepresentation)"/>
				</xsl:variable>
				<xsl:text>&#x20;</xsl:text>
				<xsl:variable name="sRep">
					<xsl:call-template name="NatClassStringToHvo">
						<xsl:with-param name="sIn">
							<xsl:value-of select="$sNormalizedOrgRep"/>
						</xsl:with-param>
					</xsl:call-template>
				</xsl:variable>
				<xsl:value-of select="$sRep"/>
				<!-- keep original for testing purposes -->
				<xsl:text> | </xsl:text>
				<xsl:value-of select="$sNormalizedOrgRep"/>
				<xsl:text>&#x20;
		</xsl:text>
			</xsl:when>
			<xsl:otherwise>
				<xsl:text> / </xsl:text>
				<xsl:if test="@LeftContext!='0'">
					<xsl:call-template name="AlloEnvironment">
						<xsl:with-param name="context" select="@LeftContext"/>
					</xsl:call-template>
				</xsl:if> _<xsl:if test="@RightContext!='0'">
					<xsl:call-template name="AlloEnvironment">
						<xsl:with-param name="context" select="@RightContext"/>
					</xsl:call-template>
				</xsl:if>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		GetPOSIdAndAnyNestedPOSIds
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="GetAnyMatchingPOSIdOrAnyMatchingNestedPOSIds">
		<xsl:param name="pos" select="."/>
		<xsl:param name="sListToLookIn"/>
		<xsl:variable name="sPatternToMatch">
			<xsl:value-of select="$sPosIdDivider"/>
			<xsl:value-of select="$pos/@Id"/>
			<xsl:value-of select="$sPosIdDivider"/>
		</xsl:variable>
		<xsl:choose>
			<xsl:when test="contains($sListToLookIn,$sPatternToMatch) and string-length($sPatternToMatch) &gt; 2">
				<xsl:text>Y</xsl:text>
			</xsl:when>
			<xsl:otherwise>
				<xsl:for-each select="$pos/SubPossibilities">
					<xsl:call-template name="GetAnyMatchingPOSIdOrAnyMatchingNestedPOSIds">
						<xsl:with-param name="pos" select="key('POSID',@dst)"/>
						<xsl:with-param name="sListToLookIn" select="$sListToLookIn"/>
					</xsl:call-template>
				</xsl:for-each>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
	- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	GetPOSIdAndAnyNestedPOSIds
	- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="GetPOSIdAndAnyNestedPOSIds">
		<xsl:param name="pos" select="."/>
		<xsl:value-of select="$pos/@Id"/>
		<xsl:value-of select="$sPosIdDivider"/>
		<xsl:for-each select="$pos/SubPossibilities">
			<xsl:call-template name="GetPOSIdAndAnyNestedPOSIds">
				<xsl:with-param name="pos" select="key('POSID',@dst)"/>
			</xsl:call-template>
		</xsl:for-each>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Gloss
	Output the Gloss
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="Gloss">
		<xsl:param name="gloss"/>
\g <xsl:if test="$gloss">
			<!-- Use glosses for testing purposes for now -->
			<xsl:value-of select="$gloss"/>
		</xsl:if>
		<xsl:if test="not($gloss)">
			<xsl:value-of select="MorphoSyntaxAnalysis/@dst"/>
		</xsl:if>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
InflClass
	Output the InflClass allomorph property if needed
		Parameters: STypes - the type of the affix
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="InflClass">
		<xsl:param name="sTypes"/>
		<xsl:choose>
			<xsl:when test="InflectionClasses">
				<xsl:variable name="sInflectonClassesOfOtherFormsInEntry">
					<xsl:variable name="sThisEntryId" select="@Id"/>
					<xsl:variable name="lexEntry" select="$LexEntries[LexemeForm/@dst=$sThisEntryId or AlternateForms/@dst=$sThisEntryId]"/>
					<xsl:for-each select="$lexEntry/LexemeForm[@dst!=$sThisEntryId] | $lexEntry/AlternateForms[@dst!=$sThisEntryId]">
						<xsl:variable name="affixForm" select="key('AffixAlloID',@dst)"/>
						<xsl:for-each select="$affixForm/InflectionClasses">
							<xsl:text>|</xsl:text>
							<xsl:value-of select="@dst"/>
							<xsl:text>|</xsl:text>
						</xsl:for-each>
					</xsl:for-each>
				</xsl:variable>
				<!-- output a space before the template name/allomorph property;
				   we use no space between multiple inflection classes to make a singleton, combo
				   allomorph property/template name -->
				<xsl:text>&#x20;</xsl:text>
				<xsl:for-each select="InflectionClasses">
					<xsl:value-of select="$sInflClassAffix"/>
					<xsl:value-of select="@dst"/>
				</xsl:for-each>
				<xsl:for-each select="InflectionClasses">
					<xsl:variable name="sThisInflClass">
						<xsl:text>|</xsl:text>
						<xsl:value-of select="@dst"/>
						<xsl:text>|</xsl:text>
					</xsl:variable>
					<!-- We create an MEC to check for the inflection class property: if it's not there somewhere,
					then something is not right.  However, if there's an exocentric compound rule that produces
					a stem having the inflection class or if there is an endocentric compound rule whose overridding
					MSA has an inflection class, then we cannot use the MEC to check.  We have to wait
					for the word grammar to check.
					By the way, we still need the word grammar because the MEC cannot test if the inflection
					class is appearing in the correct stem; it also will be met if there is a prefix and a suffix both of
					which are marked with the inflection class -->
					<xsl:if test="not(contains($sExocentricToInflectionClasses,$sThisInflClass))">
						<xsl:call-template name="OutputAffixMECorMCC">
							<xsl:with-param name="sTypes" select="$sTypes"/>
							<xsl:with-param name="sPreface" select="'InflClass'"/>
							<xsl:with-param name="sValue" select="@dst"/>
							<xsl:with-param name="sExoToInflClasses" select="$sExocentricToInflectionClasses"/>
						</xsl:call-template>
						<xsl:call-template name="OutputAffixMECorMCC">
							<xsl:with-param name="sTypes" select="$sTypes"/>
							<xsl:with-param name="sPreface" select="'ToInflClass'"/>
							<xsl:with-param name="sValue" select="@dst"/>
						</xsl:call-template>
						<!-- Also create MECs for all appropriate subclasses of this inflection class because this affix can attach to
					   any stem with this inflection class or any of its subclasses unless some other allomorph in the entry is tagged
					   for that inflection class.  In the latter case, we do not create any MECs for those inflection classes and their subclasses. -->
						<xsl:call-template name="OutputInflectionSubclassAffixMECs">
							<xsl:with-param name="sTypes" select="$sTypes"/>
							<xsl:with-param name="class" select="key('InflClassID',@dst)/Subclasses/MoInflClass"/>
							<xsl:with-param name="sInflectonClassesOfOtherFormsInEntry" select="$sInflectonClassesOfOtherFormsInEntry"/>
						</xsl:call-template>
					</xsl:if>
				</xsl:for-each>
			</xsl:when>
			<!-- The following is an attempt at allowing the final allomorph to default to the inflection class not already used.
	   There are two challenges with this:
		  1. Need to also consider inflection classes higher up in the POS hierarchy
		  2. When we generate any NegSECs, we need to only consider environments for those allomorphs which have the
			  same inflection class as this one will be.
		All this is a lot of work for something that we may not really need.

	  <xsl:otherwise>
		<xsl:variable name="CurrentAllomorphs" select="@Id"/>
		<xsl:for-each select="//Allomorphs[@dst = $CurrentAllomorphs]">
		  <xsl:if test="count(following-sibling::*[name()='Allomorphs']) = 0 and count(preceding-sibling::*[name()='Allomorphs']) > 0">
			<xsl:variable name="this" select="."/>
			<xsl:variable name="InflClassList" select="//InflectionClasses[../@Id = $this/preceding-sibling::*/@dst]"/>
			<xsl:for-each select="//MoInflClass[not(@Id = $InflClassList/@dst) and (preceding-sibling::*[@Id = $InflClassList/@dst] or following-sibling::*[@Id = $InflClassList/@dst])]">
			  <xsl:text> InflClass</xsl:text>
			  <xsl:value-of select="@Id"/>
			</xsl:for-each>
		  </xsl:if>
		</xsl:for-each>
	  </xsl:otherwise>
	  -->
		</xsl:choose>
	</xsl:template>
	<!--
	  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  OutputInflectionSubclassAffixMECs
	  Output MECs for any inflection subclasses as affix property; is recursive on subclasses
	  Parameters: sTypes - type of affix
	  class - inflection subclass
	  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
   -->
	<xsl:template name="OutputInflectionSubclassAffixMECs">
		<xsl:param name="sTypes"/>
		<xsl:param name="class"/>
		<xsl:param name="sInflectonClassesOfOtherFormsInEntry"/>
		<xsl:for-each select="$class">
			<xsl:variable name="sThisInflClass">
				<xsl:text>|</xsl:text>
				<xsl:value-of select="@Id"/>
				<xsl:text>|</xsl:text>
			</xsl:variable>
			<xsl:if test="not(contains($sInflectonClassesOfOtherFormsInEntry,$sThisInflClass))">
				<xsl:call-template name="OutputAffixMECorMCC">
					<xsl:with-param name="sTypes" select="$sTypes"/>
					<xsl:with-param name="sPreface" select="'InflClass'"/>
					<xsl:with-param name="sValue" select="@Id"/>
				</xsl:call-template>
				<xsl:call-template name="OutputAffixMECorMCC">
					<xsl:with-param name="sTypes" select="$sTypes"/>
					<xsl:with-param name="sPreface" select="'ToInflClass'"/>
					<xsl:with-param name="sValue" select="@Id"/>
				</xsl:call-template>
				<xsl:call-template name="OutputInflectionSubclassAffixMECs">
					<xsl:with-param name="sTypes" select="$sTypes"/>
					<xsl:with-param name="class" select="Subclasses/MoInflClass"/>
					<xsl:with-param name="sInflectonClassesOfOtherFormsInEntry" select="$sInflectonClassesOfOtherFormsInEntry"/>
				</xsl:call-template>
			</xsl:if>
		</xsl:for-each>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		OutputVariantEntry
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="OutputVariantEntry">
		<xsl:param name="lexEntryRef"/>
		<xsl:param name="lexEntry"/>
		<xsl:param name="sVariantOfGloss"/>
		<xsl:param name="stemMsa"/>
		<xsl:if test="$lexEntryRef/LexEntryType">
			<xsl:text>

\lx </xsl:text>
			<xsl:call-template name="IdOfVariantEntry">
				<xsl:with-param name="lexEntry" select="$lexEntry"/>
				<xsl:with-param name="lexEntryRef" select="$lexEntryRef"/>
			</xsl:call-template>
			<xsl:variable name="gloss">
				<xsl:call-template name="GlossOfVariant">
					<xsl:with-param name="lexEntryRef" select="$lexEntryRef"/>
					<xsl:with-param name="sVariantOfGloss" select="$sVariantOfGloss"/>
				</xsl:call-template>
			</xsl:variable>
			<xsl:call-template name="Gloss">
				<xsl:with-param name="gloss" select="$gloss"/>
			</xsl:call-template>
			<xsl:text>
\c W
\wc root</xsl:text>
			<xsl:variable name="allos" select="$lexEntry/AlternateForms | $lexEntry/LexemeForm"/>
			<xsl:call-template name="DoStemMsaProperties">
				<xsl:with-param name="stemMsa" select="$stemMsa"/>
				<xsl:with-param name="allos" select="$allos"/>
			</xsl:call-template>
			<xsl:call-template name="DoStemMsaStemNamesAndAllos">
				<xsl:with-param name="allos" select="$allos"/>
			</xsl:call-template>
			<xsl:for-each select="$lexEntryRef/LexEntryType">
				<xsl:variable name="lexEntryInflType" select="key('LexEntryInflTypeID',@dst)"/>
				<xsl:if test="$lexEntryInflType">
					<xsl:if test="$lexEntryInflType/InflectionFeatures/FsFeatStruc">
						<xsl:text>
\fd </xsl:text>
						<xsl:value-of select="$sMSFS"/>
						<xsl:value-of select="$lexEntryInflType/InflectionFeatures/FsFeatStruc/@Id"/>
					</xsl:if>
					<xsl:text>
\mp </xsl:text>
					<xsl:value-of select="$sIrregularlyInflectedForm"/>
					<xsl:value-of select="$lexEntryInflType/@Id"/>
				</xsl:if>
			</xsl:for-each>
			<xsl:text>&#xa;</xsl:text>
		</xsl:if>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
MSEnvFS
	Output the MSEnvFS allomorph property if needed
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="MSEnvFS">
		<xsl:param name="sAllosNotConditionedByFeatures"/>
		<xsl:choose>
			<xsl:when test="MsEnvFeatures/FsFeatStruc[descendant::FsClosedValue]">
				<xsl:text> MSEnvFS</xsl:text>
				<xsl:value-of select="MsEnvFeatures/FsFeatStruc/@Id"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="$sAllosNotConditionedByFeatures"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
MSEnvPos
	Output the MSEnvPos allomorph property if needed
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="MSEnvPos">
		<xsl:if test="@msEnvPos"> MSEnvPos<xsl:value-of select="@msEnvPos"/>
		</xsl:if>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputAffixMECorMCC
	Output an MEC (or MCC) for an affix property
		Parameters: sTypes - type of affix
							 sPreface - first part of "morphname"
							 sValue - main part of "morphname"
							 sPostscript - last part of "morphname"
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputAffixMECorMCC">
		<xsl:param name="sTypes"/>
		<xsl:param name="sPreface"/>
		<xsl:param name="sValue"/>
		<xsl:param name="sPostscript"/>
		<xsl:text> +/ </xsl:text>
		<xsl:choose>
			<xsl:when test="contains($sTypes, 'suffix')">
				<xsl:text>{</xsl:text>
				<xsl:value-of select="$sPreface"/>
				<xsl:value-of select="$sValue"/>
				<xsl:value-of select="$sPostscript"/>
				<xsl:text>}</xsl:text>
				<xsl:text> ... _</xsl:text>
			</xsl:when>
			<xsl:otherwise>
				<xsl:text>_ ... </xsl:text>
				<xsl:text>{</xsl:text>
				<xsl:value-of select="$sPreface"/>
				<xsl:value-of select="$sValue"/>
				<xsl:value-of select="$sPostscript"/>
				<xsl:text>}</xsl:text>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputOrderClass
	Output an orderclass value (based on GAFAWS output)
		Parameters: sSign = sign of the value
							sValue = the value itself
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputOrderClass">
		<xsl:param name="sSign"/>
		<xsl:param name="sValue"/>
		<xsl:choose>
			<xsl:when test="$sValue='0'">
				<xsl:text>0</xsl:text>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="$sSign"/>
				<xsl:value-of select="string(number(substring($sValue,3)) + 1)"/>
			</xsl:otherwise>
		</xsl:choose>
		<xsl:text>&#x20;</xsl:text>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputUnclassifiedOrderClass
	Output orderclass for unclassifed affix
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputUnclassifiedOrderClass">
		<xsl:param name="sTypes"/>
		<xsl:choose>
			<xsl:when test="contains($sTypes, 'prefix') or contains($sTypes, 'infix')">
				<!-- prefix orderclass is negative so need (absolute) largest as minimum -->
				<xsl:text>-31999 -1</xsl:text>
			</xsl:when>
			<xsl:otherwise>
				<xsl:text>1 31999</xsl:text>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		OutputUniqueStrings
		Routine to output a list of strings, only using those strings which are unique
		Parameters: sList = list of strings to look at, in determining which are unique
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
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
	<!--
		================================================================
		Revision History
		- - - - - - - - - - - - - - - - - - -
		27-Mar-2012     Steve McConnel  Tweak for effiency in libxslt based processing.
		21-Feb-2006	    Andy Black	Use MECs for inflection classes and MCC for exception features
		09-Feb-2006	    Andy Black	Allow ^0 and &0 for null as well as *0 and the empty set character
		06-Feb-2006	    Andy Black	Allow for full reduplication
		21-Nov-2005	    Andy Black	Do not output a record if all allomorphs are marked as abstract
		03-Nov-2005	    Andy Black	Fix typo in suffixing interfix.
		23-Sep-2005	    Andy Black	Have derivational affixes ignore the tocat if tocat = from cat.
		16-Aug-2005	    Andy Black	Allow for inflection features
		09-Aug-2005	    Andy Black	Fix LT-2270 Inflectional affixes which have a category but no slot are being treated like an unclassifed affix without a category
		07-Jul-2005	           Andy Black	Skip an NEC if the current allomorph already has the environment
		09-Jun-2005	    Andy Black	Modify for new way of doing LexEntrys
		27-May-2005	    Andy Black	Handle exception features.
		27-Apr-2005	    Andy Black	Modify to allow an allomorph to belong to more than one inflection class
		22-Apr-2005	    Andy Black	Changes for MoUnclassifedAffixMsa
		11-Apr-2005	    Andy Black	Undo last change - not needed
		07-Apr-2005	    Andy Black	Do not output an ill-formed allomoprh environment
		21-Jan-2005	    Andy Black	If an allomorph has internal whitespace, turn it into periods (to avoid error msg)
		22-Nov-2004	    Andy Black	Use natural class abbrevations
		13-Aug-2004	    Andy Black	Force nl after every SEC to avoid having two appear on same line.
		02-Jul-2004	    Andy Black	Fix Linker to use hvo instead of the word "Linker"
		17-Mar-2004	Andy Black	Pass two on order class: use results ofGAFAWS to assign inflectional orderclasses; note that it is possible to get multiple sets; only the first will be used
		11-Mar-2004	Andy Black	Pass one on order class: unmarked are all 0, derivational are all -1/1, inflectional are all -2/2 and clitics
		are all -32000/32000.
		We allow multiple instances of the same orderclass to occur.  We also allow derivational affixes to occur
		outside of inflectional affixes.
		08-Mar-2004	Andy Black	Change way we handle null allomorphs: now we allow either "*0" or Unicode 2205 (∅), the empty set symbol
		22-Dec-2003	Andy Black	Handle producing NegSECs for allomorphs
		12-Nov-2003	Andy Black	Modify for FXT output via DistFiles\WW\FXTs\M3Paser.fxt
		29-Oct-2003	 Andy Black 	Reflect recent WritingSystem change
		02-Oct-2003	 Andy Black	Add linker in compound rules
		15-Aug-2003 Andy Black Fix infix location environment processing
		17-Jul-2003	Andy Black	allow use of reduplication patterns in allomorph fields; output strings in SECs
		11-Jul-2003	Andy Black	output of string representations need to use hvos instead of class names
		10-Jul-2003	Andy Black	Make changes per model changes; fix output of string representations
		18 Jun-2003	Randy Regnier	Fix bug in case of LeftContext and RightContext.
		16 Jun-2003	Randy Regnier	Use StringRepresentation, if available.
		23-May-2003	John Hatton Changed all 'enc' to 'ws'
		24-Jan-2003      Andy Black  Create distinct \lx entreis for each MorphoSyntaxAnalyses within a LexEntry
		03-Dec-2002      Andy Black  Change Msi to Msa
		08-Feb-2002	Andy Black	First pass for flat XML format
		(Note: do Allomorphs for loop many times - see if can collapse it for efficiency)
		07-Feb-2002	Andy Black	Add infix position processing (i.e. produce \loc field)
		01-Feb-2002	Andy Black	Fix name change in MoAffixAllomorph (<PhoneEnviron> changed to <PhoneEnv>)
		15-Jan-2002	Andy Black	Revise \wc to look for English encoding (sort of...)
		14-Jan-2002	Andy Black	Rework inflectional affix \wc fields to only include the "slot" text if the affix occurs in more
		than one slot
		10-Jan-2002	Andy Black	Add glosses.
		Revise AffixType to look for English encoding (sort of...)
		04-Jan-2002	Andy Black	For MoInflectionalAffixMsi handling, added "slot" to \wc field and used correct Id value.
		Added \fd MSEnvFS info.
		Fixed MSEnvFS named template to work.
		Output 0 (zero) for allomorphs lacking a Form element.
		14-Dec-2001	Andy Black	Got AlloEnvironment to work per new data
		Added main template to avoid extra blank lines in output
		Removed FsFeatStruc match since it is no longer needed
		13-Dec-2001	Andy Black	Modify to apply to XML output from M3ParserSvr which
		has been passed through CleanFWDump.xslt
		(other than the sections demarcated by @@@)
		05-Sep-2001	Andy Black	Modified AlloEnvironment to conform to new way of handling PhPhonContext items.
		25-May-2001	Andy Black	Reworked morphtype processing to improve efficiency
		24-May-2001	Andy Black	Removed addition of "OR" for \wc cases where an inflectional affix has more than one slot (for
		efficiency reasons)
		Reworked slot name calculation to improve efficiency.
		26-Apr-2001	Andy Black	Initial Draft
		================================================================
	-->
</xsl:stylesheet>
