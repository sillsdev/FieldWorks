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
OutputLineOfWrdElementsFromMorphs
	Output a sequence of <wrd/> elements based on <morph/> elements
		Parameters: sType = type of item to use
							  bAddHyphen = flag whether to add hyphen between morphs
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="OutputLineOfWrdElementsFromMorphs">
	  <xsl:param name="sType"/>
	  <xsl:param name="bAddHyphen"/>
	  <xsl:if test="words/word[morphemes/morph/item[@type=$sType]]">
		 <line>
			<xsl:for-each select="words/word">
			   <xsl:choose>
				  <xsl:when test="morphemes[morph/item[@type=$sType]]">
					 <xsl:for-each select="morphemes[morph/item[@type=$sType]]">
						<wrd>
						   <xsl:choose>
							  <xsl:when test="key('Language',morph/item[@type=$sType]/@lang)/@vernacular='true'">
								 <langData lang="{morph/item[@type=$sType]/@lang}">
									<xsl:call-template name="OutputMorphs">
									   <xsl:with-param name="sType" select="$sType"/>
									   <xsl:with-param name="bAddHyphen" select="$bAddHyphen"/>
									</xsl:call-template>
								 </langData>
							  </xsl:when>
							  <xsl:when test="$sType='gls'">
								 <gloss lang="{morph/item[@type=$sType]/@lang}">
									<xsl:call-template name="OutputMorphs">
									   <xsl:with-param name="sType" select="$sType"/>
									   <xsl:with-param name="bAddHyphen" select="$bAddHyphen"/>
									   <xsl:with-param name="bIsGloss" select="'Y'"/>
									</xsl:call-template>
								 </gloss>
							  </xsl:when>
							  <xsl:when test="$sType='msa'">
								 <xsl:attribute name="lang">
									<xsl:value-of select="morph/item[@type=$sType]/@lang"/>
								 </xsl:attribute>
								 <object type="tGrammaticalGloss">
									<xsl:call-template name="OutputMorphs">
									   <xsl:with-param name="sType" select="$sType"/>
									   <xsl:with-param name="bAddHyphen" select="$bAddHyphen"/>
									</xsl:call-template>
								 </object>
							  </xsl:when>
							  <xsl:otherwise>
								 <xsl:attribute name="lang">
									<xsl:value-of select="morph/item[@type=$sType]/@lang"/>
								 </xsl:attribute>
								 <xsl:call-template name="OutputMorphs">
									<xsl:with-param name="sType" select="$sType"/>
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
							  <langData lang="{item[1]/@lang}">
								 <xsl:text>***</xsl:text>
							  </langData>
						   </xsl:when>
						   <xsl:when test="$sType='gls'">
							  <gloss lang="{//language[@lang!='' and not(@vernacular='true')]/@lang}">
								 <xsl:text>***</xsl:text>
							  </gloss>
						   </xsl:when>
						   <xsl:when test="$sType='msa'">
							  <xsl:attribute name="lang">
								 <xsl:value-of select="//language[@lang!='' and not(@vernacular='true')]/@lang"/>
							  </xsl:attribute>
							  <object type="tGrammaticalGloss">
								 <xsl:text>***</xsl:text>
							  </object>
						   </xsl:when>
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
	  <xsl:if test="words/word/item[@type=$sType]">
		 <line>
			<xsl:for-each select="words/word">
			   <xsl:choose>
				  <xsl:when test="item[@type=$sType]">
					 <wrd>
						<xsl:choose>
						   <xsl:when test="$sType='gls'">
							  <gloss lang="{item[@type=$sType]/@lang}">
								 <xsl:value-of select="normalize-space(item[@type=$sType])"/>
							  </gloss>
						   </xsl:when>
						   <xsl:when test="$sType='pos'">
							  <xsl:attribute name="lang">
								 <xsl:value-of select="item[@type=$sType]/@lang"/>
							  </xsl:attribute>
							  <object type="tWordPos">
								 <xsl:value-of select="normalize-space(item[@type=$sType])"/>
							  </object>
						   </xsl:when>
						   <xsl:otherwise>
							  <xsl:attribute name="lang">
								 <xsl:value-of select="item[@type=$sType]/@lang"/>
							  </xsl:attribute>
							  <xsl:value-of select="normalize-space(item[@type=$sType])"/>
						   </xsl:otherwise>
						</xsl:choose>
					 </wrd>
				  </xsl:when>
				  <xsl:when test="item[@type='punct']">
					 <!-- do nothing -->
				  </xsl:when>
				  <xsl:otherwise>
					 <wrd>
						<xsl:choose>
						   <xsl:when test="$sType='gls'">
							  <gloss lang="{//language[@lang!='' and not(@vernacular='true')]/@lang}">
								 <xsl:text>***</xsl:text>
							  </gloss>
						   </xsl:when>
						   <xsl:when test="$sType='pos'">
							  <xsl:attribute name="lang">
								 <xsl:value-of select="//language[@lang!='' and not(@vernacular='true')]/@lang"/>
							  </xsl:attribute>
							  <object type="tWordPos">
								 <xsl:text>***</xsl:text>
							  </object>
						   </xsl:when>
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
OutputMorphs
	Output a sequence of  morphs
		Parameters: sType = type of item to use
							  bAddHyphen = flag whether to add hyphen between morphs
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="OutputMorphs">
	  <xsl:param name="sType"/>
	  <xsl:param name="bAddHyphen"/>
	  <xsl:param name="bIsGloss" select="'N'"/>
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
				  <xsl:if test="$previousMorphType=$sStem or $previousMorphType=$sRoot or $previousMorphType=$sBoundStem or $previousMorphType=$sBoundRoot or $previousMorphType=$sEnclitic or $previousMorphType=$sSuffixingInterfix or $previousMorphType=$sSuffix">
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
			   <object type="tGrammaticalGloss">
				  <xsl:choose>
					 <xsl:when test="@guid=$sEnclitic">
						<xsl:value-of select="substring-after(normalize-space(item[@type=$sType]), '=')"/>
					 </xsl:when>
					 <xsl:when test="@guid=$sProclitic">
						<xsl:value-of select="substring-before(normalize-space(item[@type=$sType]), '=')"/>
					 </xsl:when>
					 <xsl:otherwise>
						<xsl:value-of select="normalize-space(item[@type=$sType])"/>
					 </xsl:otherwise>
				  </xsl:choose>
			   </object>
			   <xsl:if test="@guid=$sProclitic">
				  <xsl:text>=</xsl:text>
			   </xsl:if>
			</xsl:when>
			<xsl:otherwise>
			   <xsl:value-of select="normalize-space(item[@type=$sType])"/>
			   <xsl:if test="$sType='gls'">
				  <xsl:variable name="sGlossAppend" select="normalize-space(item[@type='glsAppend'])"/>
				  <xsl:if test="string-length($sGlossAppend) &gt; 0">
					 <object type="tGrammaticalGloss">
						<xsl:value-of select="$sGlossAppend"/>
					 </object>
				  </xsl:if>
			   </xsl:if>
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
