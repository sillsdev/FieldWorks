<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
	<!-- This stylesheet contains the common components for XLingPap word-aligned output  -->
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Elements to ignore or are handled elsewhere
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match=" item"/>
	<!--
	  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  OutputFreeWithAnyNotes
	  Output a free element and include any notes it might have
	  Parameters: none
	  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
   -->
	<xsl:template name="OutputFreeWithAnyNotes">
		<xsl:variable name="sThisTextId" select="substring-before(ancestor::interlinear-text/@guid,'-')"/>
		<free>
			<xsl:call-template name="GetFreeLangAttribute"/>
			<xsl:apply-templates/>
			<xsl:variable name="sLang" select="@lang"/>
			<xsl:if test="preceding-sibling::item[@type='note' and @lang=$sLang] or following-sibling::item[@type='note' and @lang=$sLang]">
				<xsl:for-each select="preceding-sibling::item[@type='note' and @lang=$sLang] | following-sibling::item[@type='note' and @lang=$sLang]">
					<xsl:variable name="sEndnoteNumber">
						<xsl:number level="any" count="item[@type='note']" format="1"/>
					</xsl:variable>
					<endnote id="n{$sThisTextId}.{$sEndnoteNumber}">
						<p>
							<xsl:apply-templates/>
						</p>
					</endnote>
				</xsl:for-each>
			</xsl:if>
		</free>
	</xsl:template>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		OutputGlossWithPrependOrAppend
		Output a gloss with any prepended or appended material
		Parameters: sType = type of item to use
		sLang = language id currently being used
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="OutputGlossWithPrependOrAppend">
		<xsl:param name="sType"/>
		<xsl:param name="sLang"/>
		<xsl:variable name="sGlossPrepend" select="normalize-space(item[@type='glsPrepend' and @lang=$sLang])"/>
		<xsl:if test="string-length($sGlossPrepend) &gt; 0">
			<xsl:value-of select="$sGlossPrepend"/>
		</xsl:if>
		<xsl:value-of select="normalize-space(item[@type=$sType and @lang=$sLang])"/>
		<xsl:variable name="sGlossAppend" select="normalize-space(item[@type='glsAppend' and @lang=$sLang])"/>
		<xsl:if test="string-length($sGlossAppend) &gt; 0">
			<xsl:value-of select="$sGlossAppend"/>
		</xsl:if>
	</xsl:template>
	<!--
	  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  OutputInterlinearContent
	  Output the content of an interlinear portion
	  Parameters: none
	  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
   -->
	<xsl:template name="OutputInterlinearContent">
		<!-- 			<xsl:for-each select="words/word[item/@type='txt' or morphemes][1]/descendant-or-self::item">
		-->
		<lineGroup>
			<xsl:variable name="iPunct" select="count(words/word[item[@type='punct']])"/>
			<xsl:variable name="iWord" select="count(words/word)"/>
			<xsl:choose>
				<xsl:when test="$iPunct = $iWord">
					<!-- every word is punctuation; still create a line using each word's language -->
					<line>
						<xsl:for-each select="parent::words/word">
							<wrd lang="{item/@lang}-baseline">
								<xsl:value-of select="item"/>
							</wrd>
						</xsl:for-each>
					</line>
				</xsl:when>
				<xsl:otherwise>
					<!-- this is the case before handling punctuation only lines -->
					<xsl:for-each select="words/word[item/@type='txt' or morphemes][1]/descendant-or-self::item | words/word/morphemes/morph[1][not(item)]">
						<xsl:variable name="sLang" select="@lang"/>
						<xsl:choose>
							<xsl:when test="parent::word">
								<xsl:if test="@type='txt' or @type='punct'">
									<!-- word -->
									<line>
										<!-- word -->
										<xsl:for-each select="ancestor::words/word/item[@type='txt' and @lang=$sLang]">
											<wrd>
												<langData>
													<xsl:variable name="iBaselineSiblingsCount" select="count(preceding-sibling::item[@type='txt'])"/>
													<xsl:call-template name="GetWordLangAttribute"/>
													<xsl:if test="$iBaselineSiblingsCount=0">
														<!-- prepend any initial punctuation only to the first line -->
														<xsl:for-each select="../preceding-sibling::word[1]/item[@type='punct']">
															<xsl:choose>
																<xsl:when test="count(../preceding-sibling::word)=0">
																	<!-- it's the first word item -->
																	<xsl:value-of select="normalize-space(.)"/>
																</xsl:when>
																<xsl:when test="../preceding-sibling::word[1][item/@type='punct']">
																	<!-- there are other punct items before it -->
																	<xsl:variable name="iPreviousTextItem" select="count(../preceding-sibling::word[item/@type='txt'])"/>
																	<xsl:choose>
																		<xsl:when test="$iPreviousTextItem=0">
																			<!-- everything before is punctuation; prepend them all -->
																			<xsl:for-each select="../preceding-sibling::word[item/@type='punct']">
																				<xsl:value-of select="normalize-space(.)"/>
																			</xsl:for-each>
																			<!-- include this one, too -->
																			<xsl:value-of select="normalize-space(.)"/>
																		</xsl:when>
																		<xsl:otherwise>
																			<!-- assume only the last one is preceding punct -->
																			<xsl:value-of select="normalize-space(.)"/>
																		</xsl:otherwise>
																	</xsl:choose>
																</xsl:when>
																<xsl:when test="contains(.,'(') or contains(.,'[') or contains(.,'{') or contains(.,'“') or contains(.,'‘')">
																	<!-- there are other preceding word items; look for preceding punctuation N.B. may well need to look for characters, too -->
																	<xsl:value-of select="normalize-space(.)"/>
																</xsl:when>
															</xsl:choose>
														</xsl:for-each>
													</xsl:if>
													<!-- output the word -->
													<xsl:value-of select="normalize-space(.)"/>
													<xsl:if test="$iBaselineSiblingsCount=0">
														<!-- append any following punctuation only to the first line -->
														<xsl:if test="../following-sibling::word[1]/item[@type='punct']">
															<xsl:variable name="iFollowingTextItem" select="count(../following-sibling::word[item/@type='txt'])"/>
															<xsl:choose>
																<xsl:when test="$iFollowingTextItem=0">
																	<!-- everything after is punctuation; append them all -->
																	<xsl:for-each select="../following-sibling::word[item/@type='punct']">
																		<xsl:value-of select="normalize-space(translate(.,'§',''))"/>
																	</xsl:for-each>
																</xsl:when>
																<xsl:otherwise>
																	<xsl:for-each select="../following-sibling::word[1]/item[@type='punct']">
																		<xsl:if test="not(contains(.,'(') or contains(.,'[') or contains(.,'{') or contains(.,'“') or contains(.,'‘'))">
																			<!-- skip any preceding punctuation N.B. may well need to look for characters, too -->
																			<xsl:value-of select="normalize-space(translate(.,'§',''))"/>
																		</xsl:if>
																	</xsl:for-each>
																	<!-- check for a second consecutive punctuation item -->
																	<xsl:for-each select="../following-sibling::word[2]/item[@type='punct']">
																		<xsl:if test="not(contains(.,'(') or contains(.,'[') or contains(.,'{') or contains(.,'“') or contains(.,'‘'))">
																			<!-- skip any preceding punctuation N.B. may well need to look for characters, too -->
																			<xsl:value-of select="normalize-space(translate(.,'§',''))"/>
																		</xsl:if>
																	</xsl:for-each>
																</xsl:otherwise>
															</xsl:choose>
														</xsl:if>
													</xsl:if>
												</langData>
											</wrd>
										</xsl:for-each>
									</line>
								</xsl:if>
								<!-- word gloss -->
								<xsl:call-template name="OutputLineOfWrdElementsFromWord">
									<xsl:with-param name="sType" select="'gls'"/>
									<xsl:with-param name="sLang" select="$sLang"/>
								</xsl:call-template>
								<!-- word cat -->
								<xsl:call-template name="OutputLineOfWrdElementsFromWord">
									<xsl:with-param name="sType" select="'pos'"/>
									<xsl:with-param name="sLang" select="$sLang"/>
								</xsl:call-template>
							</xsl:when>
							<xsl:when test="name()='morph'">
								<!-- first word does not have an analysis -->
								<xsl:if test="not(ancestor::word[preceding-sibling::word/morphemes/morph/item])">
									<!-- avoid some duplications -->
									<xsl:variable name="nextWord" select="ancestor::word/following-sibling::word[1]"/>
									<xsl:choose>
										<xsl:when test="not($nextWord/morphemes)">
											<!-- very rare case: the next word does not even have blank analysis items
												find the next word which has filled in analysis items -->
											<xsl:for-each select="ancestor::word/following-sibling::word[morphemes/morph/item][1]/morphemes/morph/item">
												<xsl:variable name="sLang2" select="@lang"/>
												<xsl:call-template name="ProcessMorphItem">
													<xsl:with-param name="sLang" select="$sLang2"/>
												</xsl:call-template>
											</xsl:for-each>
										</xsl:when>
										<xsl:otherwise>
											<xsl:for-each select="ancestor::word/following-sibling::word[1]/morphemes/morph/item">
												<xsl:variable name="sLang2" select="@lang"/>
												<xsl:call-template name="ProcessMorphItem">
													<xsl:with-param name="sLang" select="$sLang2"/>
												</xsl:call-template>
											</xsl:for-each>
										</xsl:otherwise>
									</xsl:choose>
								</xsl:if>
							</xsl:when>
							<xsl:when test="parent::morph[count(preceding-sibling::morph)=0]">
								<xsl:call-template name="ProcessMorphItem">
									<xsl:with-param name="sLang" select="$sLang"/>
								</xsl:call-template>
							</xsl:when>
						</xsl:choose>
					</xsl:for-each>
				</xsl:otherwise>
			</xsl:choose>
		</lineGroup>
		<xsl:for-each select="item">
			<xsl:choose>
				<xsl:when test="@type='txt'">
					<!-- what is this for?   -->
				</xsl:when>
				<xsl:when test="@type='gls'">
					<xsl:call-template name="OutputFreeWithAnyNotes"/>
				</xsl:when>
				<xsl:when test="@type='lit' ">
					<!--  someday we'll have a literal translation element in XLingPaper -->
					<free>
						<xsl:call-template name="GetFreeLangAttribute"/>
						<object type="tLiteralTranslation">
							<xsl:apply-templates/>
						</object>
					</free>
				</xsl:when>
			</xsl:choose>
		</xsl:for-each>
	</xsl:template>
	<xsl:template name="ProcessMorphItem">
		<xsl:param name="sLang"/>
		<!-- morphemes -->
		<xsl:call-template name="OutputLineOfWrdElementsFromMorphs">
			<xsl:with-param name="sType" select="'txt'"/>
			<xsl:with-param name="sLang" select="$sLang"/>
		</xsl:call-template>
		<!-- Lex Entries -->
		<xsl:call-template name="OutputLineOfWrdElementsFromMorphs">
			<xsl:with-param name="sType" select="'cf'"/>
			<xsl:with-param name="sLang" select="$sLang"/>
		</xsl:call-template>
		<!-- Gloss -->
		<xsl:call-template name="OutputLineOfWrdElementsFromMorphs">
			<xsl:with-param name="sType" select="'gls'"/>
			<xsl:with-param name="sLang" select="$sLang"/>
			<xsl:with-param name="bAddHyphen" select="'Y'"/>
		</xsl:call-template>
		<!-- msa -->
		<xsl:call-template name="OutputLineOfWrdElementsFromMorphs">
			<xsl:with-param name="sType" select="'msa'"/>
			<xsl:with-param name="sLang" select="$sLang"/>
			<xsl:with-param name="bAddHyphen" select="'Y'"/>
		</xsl:call-template>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputLineOfWrdElementsFromMorphs
	Output a sequence of <wrd/> elements based on <morph/> elements
		Parameters: sType = type of item to use
							  sLang = language id currently being used
							  bAddHyphen = flag whether to add hyphen between morphs
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputLineOfWrdElementsFromMorphs">
		<xsl:param name="sType"/>
		<xsl:param name="sLang"/>
		<xsl:param name="bAddHyphen"/>
		<xsl:if test="@type=$sType and @lang=$sLang">
			<line>
				<xsl:for-each select="ancestor::words/word">
					<xsl:choose>
						<xsl:when test="morphemes[morph/item[@type=$sType and @lang=$sLang]]">
							<xsl:for-each select="morphemes[morph/item[@type=$sType and @lang=$sLang]]">
								<wrd>
									<xsl:variable name="vernacularLanguageMissingInPhase1">
										<xsl:variable name="sBeforeLastHyphenId">
											<xsl:call-template name="GetPortionBeforeLastHyphen">
												<xsl:with-param name="pText" select="$sLang"/>
											</xsl:call-template>
										</xsl:variable>
										<xsl:variable name="sBeforeFirstHyphenId">
											<xsl:value-of select="substring-before($sLang,'-')"/>
										</xsl:variable>
											<xsl:choose>
												<xsl:when test="//language[@lang=$sBeforeFirstHyphenId]/@vernacular='true'">
													<xsl:text>Y</xsl:text>
												</xsl:when>
												<xsl:when test="//language[@lang=$sBeforeLastHyphenId]/@vernacular='true'">
													<xsl:text>Y</xsl:text>
												</xsl:when>
												<xsl:otherwise>
													<xsl:text>N</xsl:text>
												</xsl:otherwise>
											</xsl:choose>
									</xsl:variable>
									<xsl:choose>
										<xsl:when test="key('Language',morph/item[@type=$sType and @lang=$sLang]/@lang)/@vernacular='true' or $vernacularLanguageMissingInPhase1='Y'">
											<langData>
												<xsl:for-each select="morph/item[@type=$sType and @lang=$sLang]">
													<xsl:call-template name="GetMorphLangAttribute"/>
												</xsl:for-each>
												<xsl:call-template name="OutputMorphs">
													<xsl:with-param name="sType" select="$sType"/>
													<xsl:with-param name="sLang" select="$sLang"/>
													<xsl:with-param name="bAddHyphen" select="$bAddHyphen"/>
												</xsl:call-template>
											</langData>
										</xsl:when>
										<xsl:when test="$sType='gls'">
											<gloss>
												<xsl:for-each select="morph/item[@type=$sType and @lang=$sLang]">
													<xsl:call-template name="GetMorphLangAttribute"/>
												</xsl:for-each>
												<xsl:call-template name="OutputMorphs">
													<xsl:with-param name="sType" select="$sType"/>
													<xsl:with-param name="sLang" select="$sLang"/>
													<xsl:with-param name="bAddHyphen" select="$bAddHyphen"/>
													<xsl:with-param name="bIsGloss" select="'Y'"/>
												</xsl:call-template>
											</gloss>
										</xsl:when>
										<xsl:when test="$sType='msa'">
											<gloss>
												<xsl:for-each select="morph/item[@type=$sType and @lang=$sLang]">
													<xsl:call-template name="GetMorphLangAttribute"/>
												</xsl:for-each>
												<!--												<object type="tGrammaticalGloss">-->
												<xsl:call-template name="OutputMorphs">
													<xsl:with-param name="sType" select="$sType"/>
													<xsl:with-param name="sLang" select="$sLang"/>
													<xsl:with-param name="bAddHyphen" select="$bAddHyphen"/>
												</xsl:call-template>
												<!--												</object>-->
											</gloss>
										</xsl:when>
										<xsl:otherwise>
											<xsl:attribute name="lang">
												<xsl:value-of select="$sLang"/>
											</xsl:attribute>
											<xsl:call-template name="OutputMorphs">
												<xsl:with-param name="sType" select="$sType"/>
												<xsl:with-param name="sLang" select="$sLang"/>
												<xsl:with-param name="bAddHyphen" select="$bAddHyphen"/>
											</xsl:call-template>
										</xsl:otherwise>
									</xsl:choose>
								</wrd>
							</xsl:for-each>
						</xsl:when>
						<xsl:when test="item[@type='punct']">
							<!-- do nothing -->
						</xsl:when>
						<xsl:otherwise>
							<wrd>
								<xsl:choose>
									<xsl:when test="$sType='cf' or $sType='txt'">
										<langData>
											<xsl:call-template name="GetMorphLangAttribute">
												<xsl:with-param name="sLang" select="$sLang"/>
												<xsl:with-param name="sType" select="$sType"/>
											</xsl:call-template>
											<xsl:text>***</xsl:text>
										</langData>
									</xsl:when>
									<xsl:when test="$sType='gls' or $sType='msa'">
										<gloss>
											<xsl:call-template name="GetMorphLangAttribute">
												<xsl:with-param name="sLang" select="$sLang"/>
												<xsl:with-param name="sType" select="$sType"/>
											</xsl:call-template>
											<xsl:text>***</xsl:text>
										</gloss>
									</xsl:when>
									<!--<xsl:when test="$sType='msa'">
										<xsl:for-each select="morph/item[@type=$sType and @lang=$sLang]">
											<xsl:call-template name="GetMorphLangAttribute"/>
										</xsl:for-each>
										<!-\-										<object type="tGrammaticalGloss">-\->
										<xsl:text>***</xsl:text>
										<!-\-										</object>-\->
									</xsl:when>-->
									<xsl:otherwise>
										<xsl:text>***</xsl:text>
									</xsl:otherwise>
								</xsl:choose>
							</wrd>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:for-each>
			</line>
		</xsl:if>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputLineOfWrdElementsFromWord
	Output a sequence of <wrd/> elements based on <word/> elements
		Parameters: sType = type of item to use
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputLineOfWrdElementsFromWord">
		<xsl:param name="sType"/>
		<xsl:param name="sLang"/>
		<xsl:if test="@type=$sType and @lang=$sLang">
			<line>
				<xsl:for-each select="ancestor::words/word/item[@type=$sType and @lang=$sLang]">
					<xsl:choose>
						<xsl:when test="@type=$sType">
							<wrd>
								<gloss>
									<xsl:call-template name="GetWordLangAttribute"/>
									<xsl:value-of select="normalize-space(.)"/>
								</gloss>
							</wrd>
						</xsl:when>
						<xsl:when test="@type='punct'">
							<!-- do nothing -->
						</xsl:when>
						<xsl:otherwise>
							<wrd>
								<gloss>
									<xsl:call-template name="GetWordLangAttribute"/>
									<xsl:text>***</xsl:text>
								</gloss>
							</wrd>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:for-each>
			</line>
		</xsl:if>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputMorphs
	Output a sequence of  morphs
		Parameters: sType = type of item to use
							  bAddHyphen = flag whether to add hyphen between morphs
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="OutputMorphs">
		<xsl:param name="sType"/>
		<xsl:param name="sLang"/>
		<xsl:param name="bAddHyphen"/>
		<xsl:param name="bIsGloss" select="'N'"/>
		<xsl:variable name="iCountOfMorphs" select="count(morph)"/>
		<xsl:for-each select="morph">
			<xsl:if test="position()!=1">
				<xsl:choose>
					<xsl:when test="$bAddHyphen='Y'">
						<xsl:if test="preceding-sibling::morph[1]/@guid!=$sProclitic and @guid!=$sEnclitic">
							<!-- proclitics and enclitics already have an equal sign, so do not add a hyphen -->
							<xsl:value-of select="$sHyphen"/>
						</xsl:if>
					</xsl:when>
					<xsl:when test="@guid=$sStem or @guid=$sRoot or @guid=$sBoundStem or @guid=$sBoundRoot ">
						<xsl:variable name="previousMorphType" select="preceding-sibling::*[1]/@guid"/>
						<xsl:if
							test="$previousMorphType=$sStem or $previousMorphType=$sRoot or $previousMorphType=$sBoundStem or $previousMorphType=$sBoundRoot or $previousMorphType=$sEnclitic or $previousMorphType=$sSuffixingInterfix or $previousMorphType=$sSuffix">
							<xsl:value-of select="$sHyphen"/>
						</xsl:if>
					</xsl:when>
				</xsl:choose>
			</xsl:if>
			<xsl:choose>
				<xsl:when test="$bIsGloss='Y' and @guid!=$sBoundRoot and @guid!=$sBoundStem and @guid!=$sRoot and @guid!=$sStem and @guid!=$sPhrase and @guid!=$sDiscontiguousPhrase">
					<xsl:if test="@guid=$sEnclitic">
						<xsl:text>=</xsl:text>
					</xsl:if>
					<!--<object type="tGrammaticalGloss">-->
					<xsl:choose>
						<xsl:when test="@guid=$sEnclitic">
							<xsl:value-of select="substring-after(normalize-space(item[@type=$sType and @lang=$sLang]), '=')"/>
						</xsl:when>
						<xsl:when test="@guid=$sProclitic">
							<xsl:value-of select="substring-before(normalize-space(item[@type=$sType and @lang=$sLang]), '=')"/>
						</xsl:when>
						<xsl:otherwise>
							<xsl:call-template name="OutputGlossWithPrependOrAppend">
								<xsl:with-param name="sType" select="$sType"/>
								<xsl:with-param name="sLang" select="$sLang"/>
							</xsl:call-template>
						</xsl:otherwise>
					</xsl:choose>
					<!--					</object>-->
					<xsl:if test="@guid=$sProclitic">
						<xsl:text>=</xsl:text>
					</xsl:if>
				</xsl:when>
				<xsl:when test="$iCountOfMorphs &gt; 1 and $sType='msa' and @guid!=$sEnclitic or $iCountOfMorphs &gt; 1 and $sType='msa' and @guid!=$sProclitic">
					<xsl:if test="@guid=$sEnclitic">
						<xsl:text>=</xsl:text>
					</xsl:if>
					<xsl:value-of select="normalize-space(item[@type=$sType and @lang=$sLang])"/>
					<xsl:if test="@guid=$sProclitic">
						<xsl:text>=</xsl:text>
					</xsl:if>
				</xsl:when>
				<xsl:otherwise>
					<xsl:choose>
						<xsl:when test="$sType='gls'">
							<xsl:call-template name="OutputGlossWithPrependOrAppend">
								<xsl:with-param name="sType" select="$sType"/>
								<xsl:with-param name="sLang" select="$sLang"/>
							</xsl:call-template>
						</xsl:when>
						<xsl:otherwise>
							<xsl:value-of select="normalize-space(item[@type=$sType and @lang=$sLang])"/>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:otherwise>
			</xsl:choose>
			<xsl:if test="$sType='cf'">
				<xsl:variable name="homographNumber" select="item[@type='hn']"/>
				<xsl:if test="$homographNumber">
					<object type="tHomographNumber">
						<xsl:value-of select="$homographNumber"/>
					</object>
				</xsl:if>
				<xsl:variable name="variantTypes" select="item[@type='variantTypes']"/>
				<xsl:if test="$variantTypes">
					<object type="tVariantTypes">
						<xsl:value-of select="$variantTypes"/>
					</object>
				</xsl:if>
			</xsl:if>
		</xsl:for-each>
	</xsl:template>
	<xsl:include href="xml2XLingPapAllCommon.xsl"/>
</xsl:stylesheet>
