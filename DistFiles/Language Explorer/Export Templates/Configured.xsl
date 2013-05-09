<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="xml" version="1.0" encoding="utf-8" indent="yes"/>

<!-- This stylesheet works on the output of Language Explorer to produce an XML file which is
	 somewhat more like the standard dump XML file in its treatment of cross-references. -->

<!-- Remove the LexSense_MorphoSyntaxAnalysis element level since Mo*Msa_PartOfSpeech is
	 enough for export purposes. -->

<xsl:template match="LexSense_MorphoSyntaxAnalysis">
  <xsl:apply-templates/>
</xsl:template>
<xsl:template match="MoMorphSynAnalysisLink">
  <xsl:apply-templates/>
</xsl:template>

<xsl:template match="LexEntry_Self">
  <xsl:apply-templates/>
</xsl:template>
<xsl:template match="LexEntry_Self/Paragraph">
  <xsl:apply-templates/>
</xsl:template>

<!-- Remove any residue from minor entries that aren't displayed -->
				<!-- Prevent empty LexEntry/LexEntry_Self nodes LT-13589
				These LexEntries are compound forms with "Show Subentry under" a component
				and "Show Minor Entry" checked. No main entry should be generated for these.
				Details: The LexEntry node is generated in ConfiguredExport.AddObjVecItems()
				while CollectorEnv.AddObjProp() emits tags for LexEntry_Self and
				/LexEntry_Self via ConfiguredExport.WriteFieldStartTag() and
				WriteFieldEndTag(). CollectorEnv.AddObjProp() calls XmlVc.Display() which
				rejects the content in ProcessPartRef() where sVisibility == never. -->
<xsl:template match="LexEntry">
	<xsl:choose>
	<xsl:when test="count(*) = 1 and LexEntry_Self[not(*)]"></xsl:when>
<!-- sometimes we get completely empty nodes for derived forms...omit them-->
	<xsl:when test="not(LexEntry_Self)"></xsl:when>
	<xsl:otherwise><xsl:copy><xsl:copy-of select="@*"/><xsl:apply-templates/></xsl:copy></xsl:otherwise>
	</xsl:choose>
</xsl:template>

<!-- Merge child $NameNode/AStr and $AbbrNode/AStr elements into a single Link element with
	 embedded Alt elements handling possible multilingual references. -->

<xsl:template name="ProcessLinkElement">
  <xsl:param name="AbbrNode"/>
  <xsl:param name="NameNode"/>
  <xsl:param name="AttrPrefix"/>
  <Link>
	<xsl:for-each select="$AbbrNode/AStr">
	  <xsl:variable name="wsAbbr">
		<xsl:value-of select="@ws"/>
	  </xsl:variable>
	  <Alt>
		<xsl:attribute name="ws">
		  <xsl:value-of select="@ws"/>
		</xsl:attribute>
		<xsl:attribute name="{$AttrPrefix}abbr">
		  <xsl:value-of select="Run"/>
		</xsl:attribute>
		<xsl:for-each select="$NameNode/AStr[@ws=$wsAbbr]">
		  <xsl:attribute name="{$AttrPrefix}name">
			<xsl:value-of select="Run"/>
		  </xsl:attribute>
		</xsl:for-each>
	  </Alt>
	</xsl:for-each>
	<xsl:for-each select="$NameNode/AStr">
	  <xsl:variable name="wsName">
		<xsl:value-of select="@ws"/>
	  </xsl:variable>
	  <xsl:if test="not($AbbrNode/AStr[@ws=$wsName])">
		<Alt>
		  <xsl:attribute name="ws">
			<xsl:value-of select="@ws"/>
		  </xsl:attribute>
		  <xsl:attribute name="{$AttrPrefix}name">
			<xsl:value-of select="Run"/>
		  </xsl:attribute>
		</Alt>
	  </xsl:if>
	</xsl:for-each>
  </Link>
</xsl:template>


<!-- Merge Mo*Msa_PartOfSpeech/CmPossibility_Abbreviation and
	 Mo*Msa_PartOfSpeech/CmPossibility_Name into Mo*Msa_PartOfSpeech/Link.  Do the same for
	 MoDerivAffMsa_FromPartOfSpeech and MoDerivAffMsa_ToPartOfSpeech. -->

<xsl:template name="ProcessPartOfSpeech" match="MoStemMsa_PartOfSpeech">
  <xsl:copy>
	<xsl:copy-of select="@*"/>
	<xsl:call-template name="ProcessLinkElement">
	  <xsl:with-param name="AbbrNode" select="PartOfSpeechLink/CmPossibility_Abbreviation"/>
	  <xsl:with-param name="NameNode" select="PartOfSpeechLink/CmPossibility_Name"/>
	  <xsl:with-param name="AttrPrefix"></xsl:with-param>
	</xsl:call-template>
  </xsl:copy>
</xsl:template>
<xsl:template match="MoInflAffMsa_PartOfSpeech">
  <xsl:call-template name="ProcessPartOfSpeech"/>
</xsl:template>
<xsl:template match="MoUnclassifiedAffixMsa_PartOfSpeech">
  <xsl:call-template name="ProcessPartOfSpeech"/>
</xsl:template>
<xsl:template match="MoDerivStepMsa_PartOfSpeech">
  <xsl:call-template name="ProcessPartOfSpeech"/>
</xsl:template>
<xsl:template match="MoDerivAffMsa_FromPartOfSpeech">
  <xsl:call-template name="ProcessPartOfSpeech"/>
</xsl:template>
<xsl:template match="MoDerivAffMsa_ToPartOfSpeech">
  <xsl:call-template name="ProcessPartOfSpeech"/>
</xsl:template>


<!-- Change the LexEntryLink element into a Link element. -->

<xsl:template match="LexEntryLink">
  <Link>
	<xsl:copy-of select="@*"/>
	<xsl:choose>
	  <xsl:when test="LexEntryLink_MLHeadWord/AStr">
		<xsl:for-each select="LexEntryLink_MLHeadWord/AStr">
		  <Alt>
			<xsl:attribute name="ws">
			  <xsl:value-of select="@ws"/>
			</xsl:attribute>
			<xsl:attribute name="entry">
			  <xsl:for-each select="Run">
				<xsl:value-of select="."/>
			  </xsl:for-each>
			</xsl:attribute>
		  </Alt>
		</xsl:for-each>
		<xsl:for-each select="LexEntry_SummaryDefinition">
		  <xsl:copy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		  </xsl:copy>
		</xsl:for-each>
	  </xsl:when>
	</xsl:choose>
  </Link>
</xsl:template>



<!-- Change the LexSenseLink element into a Link element. -->

<xsl:template match="LexSenseLink">
  <Link>
	<xsl:copy-of select="@*"/>
	<xsl:for-each select="LexSenseLink_MLOwnerOutlineName/AStr">
	  <Alt>
		<xsl:attribute name="ws">
		  <xsl:value-of select="@ws"/>
		</xsl:attribute>
		<xsl:attribute name="sense">
		  <xsl:for-each select="Run">
			<xsl:value-of select="."/>
		  </xsl:for-each>
		</xsl:attribute>
	  </Alt>
	</xsl:for-each>
  </Link>
</xsl:template>


<!-- Rename the LexRefTypeLink element to LexReferenceLink_Type, since the former is a
	 class, and we really want a field at this point.  Also convert its contents to a Link
	 element. -->

<xsl:template match="LexRefTypeLink">
  <LexReferenceLink_Type>
	<xsl:choose>
	  <xsl:when test="CmPossibility_Abbreviation or CmPossibility_Name">
		<xsl:call-template name="ProcessLinkElement">
		  <xsl:with-param name="AbbrNode" select="CmPossibility_Abbreviation"/>
		  <xsl:with-param name="NameNode" select="CmPossibility_Name"/>
		  <xsl:with-param name="AttrPrefix"></xsl:with-param>
		</xsl:call-template>
	  </xsl:when>
	  <xsl:when test="LexRefType_ReverseAbbreviation or LexRefType_ReverseName">
		<xsl:call-template name="ProcessLinkElement">
		  <xsl:with-param name="AbbrNode" select="LexRefType_ReverseAbbreviation"/>
		  <xsl:with-param name="NameNode" select="LexRefType_ReverseName"/>
		  <xsl:with-param name="AttrPrefix">rev</xsl:with-param>
		</xsl:call-template>
	  </xsl:when>
	</xsl:choose>
  </LexReferenceLink_Type>
</xsl:template>


<!-- Rename MoStemAllomorph and MoAffixAllomorph to Allomorph. -->

<xsl:template match="MoStemAllomorph">
  <Allomorph>
	<xsl:apply-templates/>
  </Allomorph>
</xsl:template>

<xsl:template match="MoAffixAllomorph">
  <Allomorph>
	<xsl:apply-templates/>
  </Allomorph>
</xsl:template>


<!-- Rename MoStemAllomorph_Form and MoAffixAllomorph_Form to Allomorph_Form. -->

<!--xsl:template match="MoStemAllomorph_Form">
  <Allomorph_Form>
	<xsl:apply-templates/>
  </Allomorph_Form>
</xsl:template>

<xsl:template match="MoAffixAllomorph_Form">
  <Allomorph_Form>
	<xsl:apply-templates/>
  </Allomorph_Form>
</xsl:template-->


<!-- Rename MoStemAllomorph_MorphType and MoAffixAllomorph_MorphType to Allomorph_MorphType. -->

<!--xsl:template match="MoStemAllomorph_MorphType">
  <Allomorph_MorphType>
	<xsl:apply-templates/>
  </Allomorph_MorphType>
</xsl:template>

<xsl:template match="MoAffixAllomorph_MorphType">
  <Allomorph_MorphType>
	<xsl:apply-templates/>
  </Allomorph_MorphType>
</xsl:template-->


<!-- Rename LexEntry_MinimalLexReferences to LexEntry_LexReferences. -->

<xsl:template match="LexEntry_MinimalLexReferences">
  <LexEntry_LexReferences>
	<xsl:apply-templates/>
  </LexEntry_LexReferences>
</xsl:template>


<!-- Remove the Paragraph element level. -->

<xsl:template match="LexEntry/Paragraph">
  <xsl:apply-templates/>
</xsl:template>
<xsl:template match="CmPicture/Paragraph">
  <xsl:apply-templates/>
</xsl:template>

<!-- Remove the LiteralString elements (including their contents). -->

<xsl:template match="LiteralString">
</xsl:template>


<!-- Remove the target attribute from a LexReferenceLink element. -->

<xsl:template match="LexReferenceLink">
  <xsl:copy>
	<xsl:copy-of select="@*[name() != 'target']"/>
	<xsl:apply-templates/>
  </xsl:copy>
</xsl:template>

<!--xsl:template match="LexSenseLink_MLOwnerOutlineName">
  <Link>
	<xsl:attribute name="target">
	  <xsl:value-of select="../@target"/>
	</xsl:attribute>
	<xsl:attribute name="ws">
	  <xsl:value-of select="AStr/@ws"/>
	</xsl:attribute>
	<xsl:attribute name="sense">
	  <xsl:for-each select="AStr/Run">
		<xsl:value-of select="."/>
	  </xsl:for-each>
	</xsl:attribute>
  </Link>
</xsl:template-->


<!-- Turn the CmPossibilityLink element into a Link element. -->

<xsl:template match="CmPossibilityLink">
  <xsl:call-template name="ProcessLinkElement">
	<xsl:with-param name="AbbrNode" select="CmPossibility_Abbreviation"/>
	<xsl:with-param name="NameNode" select="CmPossibility_Name"/>
	<xsl:with-param name="AttrPrefix"></xsl:with-param>
  </xsl:call-template>
</xsl:template>


<!-- Turn the CmSemanticDomainLink element into a Link element. -->

<xsl:template match="CmSemanticDomainLink">
  <xsl:call-template name="ProcessLinkElement">
	<xsl:with-param name="AbbrNode" select="CmPossibility_Abbreviation"/>
	<xsl:with-param name="NameNode" select="CmPossibility_Name"/>
	<xsl:with-param name="AttrPrefix"></xsl:with-param>
  </xsl:call-template>
</xsl:template>


<!-- Turn the CmAnthroItemLink element into a Link element. -->

<xsl:template match="CmAnthroItemLink">
  <xsl:call-template name="ProcessLinkElement">
	<xsl:with-param name="AbbrNode" select="CmPossibility_Abbreviation"/>
	<xsl:with-param name="NameNode" select="CmPossibility_Name"/>
	<xsl:with-param name="AttrPrefix"></xsl:with-param>
  </xsl:call-template>
</xsl:template>


<!-- Turn the CmLocationLink element into a Link element. -->

<xsl:template match="CmLocationLink">
  <xsl:call-template name="ProcessLinkElement">
	<xsl:with-param name="AbbrNode" select="CmPossibility_Abbreviation"/>
	<xsl:with-param name="NameNode" select="CmPossibility_Name"/>
	<xsl:with-param name="AttrPrefix"></xsl:with-param>
  </xsl:call-template>
</xsl:template>


<!-- Add the sense number to LexSense elements. -->

<xsl:template match="LexSense">
  <xsl:copy>
	<xsl:attribute name="number"><xsl:number format="1.1.1" level="multiple" count="LexSense"/></xsl:attribute>
	<xsl:copy-of select="@*"/>
	<xsl:apply-templates/>
  </xsl:copy>
</xsl:template>


<!-- Change the contents of LexSense_SenseType into a Link element. -->

<xsl:template match="LexSense_SenseType">
  <xsl:copy>
	<xsl:copy-of select="@*"/>
	<xsl:call-template name="ProcessLinkElement">
	  <xsl:with-param name="AbbrNode" select="CmPossibilityLink/CmPossibility_Abbreviation"/>
	  <xsl:with-param name="NameNode" select="CmPossibilityLink/CmPossibility_Name"/>
	  <xsl:with-param name="AttrPrefix"></xsl:with-param>
	</xsl:call-template>
  </xsl:copy>
</xsl:template>

<!-- Change the contents of CmTranslation_Type into a Link element. -->

<xsl:template match="CmTranslation_Type">
  <xsl:copy>
	<xsl:copy-of select="@*"/>
	<xsl:call-template name="ProcessLinkElement">
	  <xsl:with-param name="AbbrNode" select="CmPossibilityLink/CmPossibility_Abbreviation"/>
	  <xsl:with-param name="NameNode" select="CmPossibilityLink/CmPossibility_Name"/>
	  <xsl:with-param name="AttrPrefix"></xsl:with-param>
	</xsl:call-template>
  </xsl:copy>
</xsl:template>


<!-- Change the contents of LexSense_Status into a Link element. -->

<xsl:template match="LexSense_Status">
  <xsl:copy>
	<xsl:copy-of select="@*"/>
	<xsl:call-template name="ProcessLinkElement">
	  <xsl:with-param name="AbbrNode" select="CmPossibilityLink/CmPossibility_Abbreviation"/>
	  <xsl:with-param name="NameNode" select="CmPossibilityLink/CmPossibility_Name"/>
	  <xsl:with-param name="AttrPrefix"></xsl:with-param>
	</xsl:call-template>
  </xsl:copy>
</xsl:template>


<!-- Transform the following structure: (no Citation Form)

	<LexEntry_LexemeForm>
		<MoForm>
			<MoForm_MorphType>
				<MoMorphTypeLink>
					<MoMorphType_Prefix>
						<AUni ws="en">-</AUni>
					</MoMorphType_Prefix>
				</MoMorphTypeLink>
			</MoForm_MorphType>
		</MoForm>
	</LexEntry_LexemeForm>
	<LexEntry_LexemeForm>
		<MoForm>
			<MoForm_Form>
				<AStr ws="seh"><Run ws="seh">banku</Run></AStr>
			</MoForm_Form>
		</MoForm>
	</LexEntry_LexemeForm>
	<LexEntry_LexemeForm>
		<MoForm>
			<MoForm_MorphType>
				<MoMorphTypeLink>
					<MoMorphType_Postfix>
						<AUni ws="en">=</AUni>
					</MoMorphType_Postfix>
				</MoMorphTypeLink>
			</MoForm_MorphType>
		</MoForm>
	</LexEntry_LexemeForm>

	into:

	<LexEntry_HeadWord>
		<MoForm>
			<MoForm_TypePrefix><AUni ws="en">-</AUni></MoForm_TypePrefix>
			<MoForm_Form>
				<AStr ws="seh"><Run ws="seh">banku</Run></AStr>
			</MoForm_Form>
			<MoForm_TypePostfix><AUni ws="en">=</AUni></MoForm_TypePostfix>
		</MoForm>
	</LexEntry_HeadWord>
-->

<xsl:template match="LexEntry_LexemeForm">
  <xsl:choose>
	<xsl:when test="not(MoForm/*)">
	</xsl:when>
	<xsl:when test="MoForm/MoForm_MorphType/MoMorphTypeLink">
	</xsl:when>
	<xsl:when test="preceding-sibling::LexEntry_LexemeForm[1]/MoForm and following-sibling::LexEntry_LexemeForm[1]/MoForm">
	  <LexEntry_HeadWord>
		<MoForm>
		  <xsl:if test="not(../LexEntry_LexemeForm/MoForm/MoForm_MorphType/MoMorphTypeLink/MoMorphType_Prefix/AUni='')">
			<MoForm_TypePrefix>
			  <xsl:copy-of select="../LexEntry_LexemeForm/MoForm/MoForm_MorphType/MoMorphTypeLink/MoMorphType_Prefix/AUni"/>
			</MoForm_TypePrefix>
		  </xsl:if>
		  <xsl:copy-of select="../LexEntry_LexemeForm/MoForm/MoForm_Form"/>
		  <xsl:if test="not(../LexEntry_LexemeForm/MoForm/MoForm_MorphType/MoMorphTypeLink/MoMorphType_Postfix/AUni='')">
			<MoForm_TypePostfix>
			  <xsl:copy-of select="../LexEntry_LexemeForm/MoForm/MoForm_MorphType/MoMorphTypeLink/MoMorphType_Postfix/AUni"/>
			</MoForm_TypePostfix>
		  </xsl:if>
		</MoForm>
	  </LexEntry_HeadWord>
	</xsl:when>
	<xsl:otherwise>
	  <xsl:copy>
		<xsl:apply-templates/>
	  </xsl:copy>
	</xsl:otherwise>
  </xsl:choose>
</xsl:template>

<!-- transform the following structure: (with Citation Form)

	<LexEntry_LexemeForm>
		<MoForm>
			<MoForm_MorphType>
				<MoMorphTypeLink>
					<MoMorphType_Prefix>
						<AUni ws="en">-</AUni>
					</MoMorphType_Prefix>
				</MoMorphTypeLink>
			</MoForm_MorphType>
		</MoForm>
	</LexEntry_LexemeForm>
	<LexEntry_CitationForm>
		<AStr ws="seh">
		  <Run ws="seh">banku</Run>
		</AStr>
	</LexEntry_CitationForm>
	<LexEntry_LexemeForm>
		<MoForm>
			<MoForm_MorphType>
				<MoMorphTypeLink>
					<MoMorphType_Postfix>
						<AUni ws="en">=</AUni>
					</MoMorphType_Postfix>
				</MoMorphTypeLink>
			</MoForm_MorphType>
		</MoForm>
	</LexEntry_LexemeForm>

	into:

	<LexEntry_HeadWord>
		<MoForm>
			<MoForm_TypePrefix><AUni ws="en">-</AUni></MoForm_TypePrefix>
			<MoForm_Form>
				<AStr ws="seh"><Run ws="seh">banku</Run></AStr>
			</MoForm_Form>
			<MoForm_TypePostfix><AUni ws="en">=</AUni></MoForm_TypePostfix>
		</MoForm>
	</LexEntry_HeadWord>

-->

<xsl:template match="LexEntry_CitationForm">
  <xsl:choose>
	<xsl:when test="preceding-sibling::LexEntry_LexemeForm[1]/MoForm and following-sibling::LexEntry_LexemeForm[1]/MoForm">
	  <LexEntry_HeadWord>
		<MoForm>
		  <xsl:if test="not(../LexEntry_LexemeForm/MoForm/MoForm_MorphType/MoMorphTypeLink/MoMorphType_Prefix/AUni='')">
			<MoForm_TypePrefix><xsl:copy-of select="../LexEntry_LexemeForm/MoForm/MoForm_MorphType/MoMorphTypeLink/MoMorphType_Prefix/AUni"/></MoForm_TypePrefix>
		  </xsl:if>
		  <MoForm_Form>
		  <xsl:apply-templates/>
		  </MoForm_Form>
		  <xsl:if test="not(../LexEntry_LexemeForm/MoForm/MoForm_MorphType/MoMorphTypeLink/MoMorphType_Postfix/AUni='')">
			<MoForm_TypePostfix><xsl:copy-of select="../LexEntry_LexemeForm/MoForm/MoForm_MorphType/MoMorphTypeLink/MoMorphType_Postfix/AUni"/></MoForm_TypePostfix>
		  </xsl:if>
		</MoForm>
	  </LexEntry_HeadWord>
	</xsl:when>
	<xsl:otherwise>
	  <xsl:copy>
		<xsl:apply-templates/>
	  </xsl:copy>
	</xsl:otherwise>
  </xsl:choose>
</xsl:template>


<!-- Simplify the CmFileLink element inside the CmPicture_PictureFile -->

<xsl:template match="CmPicture_PictureFile">
  <xsl:if test="not(CmFileLink/CmFile_InternalPath/AUni='')">
	<CmPicture_InternalFilePath>
	  <xsl:value-of select="CmFileLink/CmFile_InternalPath/AUni"/>
	</CmPicture_InternalFilePath>
  </xsl:if>
</xsl:template>


<!-- Simplify LexEntryRef by removing unneeded id attribute. -->

<xsl:template match="LexEntryRef">
  <xsl:if test="child::*">
	<xsl:copy>
	  <xsl:apply-templates/>
	</xsl:copy>
  </xsl:if>
</xsl:template>

<!-- Simplify LexEntryRef_ComplexEntryTypes and LexEntryRef_VariantEntryTypes -->

<xsl:template match="LexEntryRef_ComplexEntryTypes|LexEntryRef_VariantEntryTypes">
  <xsl:if test="child::*">
	<xsl:copy>
	  <xsl:copy-of select="@*"/>
	  <xsl:for-each select="LexEntryTypeLink">
	  <xsl:choose>
		<xsl:when test="CmPossibility_Abbreviation or CmPossibility_Name">
		  <xsl:call-template name="ProcessLinkElement">
			<xsl:with-param name="AbbrNode" select="CmPossibility_Abbreviation"/>
			<xsl:with-param name="NameNode" select="CmPossibility_Name"/>
			<xsl:with-param name="AttrPrefix"></xsl:with-param>
		  </xsl:call-template>
		</xsl:when>
		<xsl:when test="LexEntryType_ReverseAbbr or LexEntryType_ReverseName">
		  <xsl:call-template name="ProcessLinkElement">
			<xsl:with-param name="AbbrNode" select="LexEntryType_ReverseAbbr"/>
			<xsl:with-param name="NameNode" select="LexEntryType_ReverseName"/>
			<xsl:with-param name="AttrPrefix">rev</xsl:with-param>
		  </xsl:call-template>
		</xsl:when>
	  </xsl:choose>
	  </xsl:for-each>
	</xsl:copy>
  </xsl:if>
</xsl:template>

<!-- convert _MLHeadWord to LexEntry_HeadWord -->

<xsl:template match="_MLHeadWord">
  <LexEntry_HeadWord>
	<xsl:apply-templates/>
  </LexEntry_HeadWord>
</xsl:template>

<!-- Throw away empty LexEntryRefLink_OwningEntry and _ComplexFormEntryBackRefs elements. -->

<xsl:template match="LexEntryRefLink_OwningEntry|_ComplexFormEntryBackRefs">
  <xsl:if test="child::*">
	<xsl:copy>
	  <xsl:copy-of select="@*"/>
	  <xsl:apply-templates/>
	</xsl:copy>
  </xsl:if>
</xsl:template>


<!-- Copy over any Custom fields. -->
<xsl:template match="*[@userlabel]">
	<xsl:copy>
		<xsl:copy-of select="@*"/>
		<xsl:apply-templates/>
	</xsl:copy>
</xsl:template>

<!-- This is the basic default processing. -->

<xsl:template match="*">
  <xsl:copy>
	<xsl:copy-of select="@*"/>
	<xsl:apply-templates/>
  </xsl:copy>
</xsl:template>

</xsl:stylesheet>
