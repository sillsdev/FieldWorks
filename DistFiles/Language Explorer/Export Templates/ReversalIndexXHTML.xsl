<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="xml" version="1.0" encoding="utf-8" indent="yes"/>

  <!--***************************************************************************************-->
  <!--* This stylesheet works on the output produced by FLEX specifically for XHTML export. *-->
  <!--***************************************************************************************-->

  <!-- strip out the writing system information, since only the stylesheet needs it. -->

  <xsl:template match="WritingSystemInfo"/>

  <!-- Strip all white space and leave it up to the stylesheet text elements below to put in appropriate spacing. -->

  <xsl:strip-space elements="*"/>
  <xsl:preserve-space elements="Run"/><!-- but these spaces are significant! -->

  <!-- insert a comment explaining why there's so little whitespace in the xhtml output. -->

  <xsl:template match="/html">
	<xsl:copy>
	  <xsl:copy-of select="@*"/><xsl:text>&#13;&#10;</xsl:text>
	  <xsl:comment>
There are no spaces or newlines between &lt;span&gt; elements in this file because
whitespace is significant.  We don't want extraneous spaces appearing in the
display/printout!
	  </xsl:comment><xsl:text>&#13;&#10;</xsl:text>
	  <xsl:apply-templates/>
	</xsl:copy>
  </xsl:template>

  <!-- insert the XHTML DOCTYPE declaration before the root element -->

  <xsl:template match="/">
	<xsl:text disable-output-escaping="yes">&#13;&#10;&lt;!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd"&gt;&#13;&#10;</xsl:text>
	<xsl:apply-templates/>
  </xsl:template>

  <!-- insert newlines after <head>, <body>, and <div> -->

  <xsl:template match="head|body|div">
	<xsl:copy>
	  <xsl:copy-of select="@*"/><xsl:text>&#13;&#10;</xsl:text>
	  <xsl:apply-templates/>
	</xsl:copy><xsl:text>&#13;&#10;</xsl:text>
  </xsl:template>


  <!-- eliminate unneeded levels in the original markup -->

  <xsl:template match="Paragraph|ReversalIndexEntry_Hvo|ReversalIndexEntry_ReversalForm">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="_ReferringSenses|ReversalIndexEntry_ReferringSenses|LexReference_Targets">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="LexSenseLink_OwningEntry|LexSense_MorphoSyntaxAnalysis">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="LexSense_SemanticDomains|ReversalIndexEntry_PartOfSpeech">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="LexSense_Examples|LexExampleSentence_Translations">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="_MinimalLexReferences|ReversalIndexEntry_Subentries">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="_ComplexFormEntryBackRefs|LexEntryRef_ComplexEntryTypes">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="LexEntryRefLink_OwningEntry|LexEntry_EntryRefs|_VariantFormEntryBackRefs">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="LexEntryRef_VariantEntryTypes|LexEntryRef_ComponentLexemes">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="LexEntry_Pronunciations|LexPronunciation|LexEntryRef|LexSense_SenseType">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="LexSenseLink_LexSenseReferences|LexSense_DomainTypes|LexSense_UsageTypes">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="MoInflAffMsa_Slots|LexEntry_Etymology|CmTranslation_Type">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="LexSense_Senses|LexSense_Status|LexSense_AnthroCodes">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="LexEntry_AlternateForms|MoForm_MorphType|MoMorphTypeLink">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="MoStemAllomorph_PhoneEnv|MoAffixAllomorph_PhoneEnv">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="LexSenseLink_ComplexFormEntryBackRefs|LexPronunciation_Location">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="LexSense_ComplexFormEntryBackRefs|LexSense_LexSenseReferences">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="ReversalIndexEntry_Self|LexSenseLink_VariantFormEntryBackRefs">
	<xsl:apply-templates/>
  </xsl:template>


  <!-- Insert xitem elements as needed -->

  <xsl:template match="MoStemAllomorph">
	<xsl:choose>
	  <xsl:when test="count(../MoStemAllomorph) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>
  <xsl:template match="MoAffixAllomorph">
	<xsl:choose>
	  <xsl:when test="count(../MoAffixAllomorph) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>
  <xsl:template match="CmPossibilityLink">
	<xsl:choose>
	  <xsl:when test="count(../CmPossibilityLink) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>
  <xsl:template match="CmSemanticDomainLink">
	<xsl:choose>
	  <xsl:when test="count(../CmSemanticDomainLink) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>
  <xsl:template match="LexEntryLink">
	<xsl:choose>
	  <xsl:when test="count(../LexEntryLink)+count(../LexSenseLink) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>
  <xsl:template match="LexEntryTypeLink">
	<xsl:choose>
	  <xsl:when test="count(../LexEntryTypeLink) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>
  <xsl:template match="LexRefTypeLink">
	<xsl:choose>
	  <xsl:when test="count(../LexRefTypeLink) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>
  <xsl:template match="LexReferenceLink">
	<xsl:choose>
	  <xsl:when test="count(../LexReferenceLink) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>
  <xsl:template match="MoMorphSynAnalysisLink">
	<xsl:choose>
	  <xsl:when test="count(../MoMorphSynAnalysisLink) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>
  <xsl:template match="PartOfSpeechLink">
	<xsl:choose>
	  <xsl:when test="count(../PartOfSpeechLink) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>
  <xsl:template match="LexEntryRefLink">
	<xsl:choose>
	  <xsl:when test="count(../LexEntryRefLink) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>
  <xsl:template match="LexExampleSentence">
	<xsl:choose>
	  <xsl:when test="count(../LexExampleSentence) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>
  <xsl:template match="CmTranslation">
	<xsl:choose>
	  <xsl:when test="count(../CmTranslation) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>
  <xsl:template match="MoInflAffixSlotLink">
	<xsl:choose>
	  <xsl:when test="count(../MoInflAffixSlotLink) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>
  <xsl:template match="LexEtymology">
	<xsl:choose>
	  <xsl:when test="count(../LexEtymology) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>
  <xsl:template match="CmAnthroItemLink">
	<xsl:choose>
	  <xsl:when test="count(../CmAnthroItemLink) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>
  <xsl:template match="PhEnvironmentLink">
	<xsl:choose>
	  <xsl:when test="count(../PhEnvironmentLink) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>
  <xsl:template match="CmLocationLink">
	<xsl:choose>
	  <xsl:when test="count(../CmLocationLink) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>


  <!-- eliminate elements that are processed completely by ancestors -->

  <xsl:template match="LexEntry_HomographNumber"/>


  <!-- change ReversalIndexEntry into div with the proper class attribute value -->

  <xsl:template match="ReversalIndexEntry">
	<xsl:if test="parent::div[@class='letData']">
	  <div class="entry"><xsl:apply-templates/></div><xsl:text>&#13;&#10;</xsl:text>
	</xsl:if>
	<xsl:if test="parent::ReversalIndexEntry_Subentries">
	  <div class="subentry"><xsl:apply-templates/></div><xsl:text>&#13;&#10;</xsl:text>
	</xsl:if>
  </xsl:template>

  <!-- change LexSenseLink into span (or xitem) with the proper attribute values -->
  <!-- I'm not sure whether the id values are needed for senses, but they must be unique.  See FWR-3255. -->

  <xsl:template match="LexSenseLink">
	<xsl:choose>
	  <xsl:when test="parent::_ReferringSenses">
		<xsl:call-template name="ProcessSense">
		  <xsl:with-param name="id">
		<xsl:value-of select="ancestor::ReversalIndexEntry/@id"/><xsl:text>_</xsl:text><xsl:value-of select="@target"/>
	  </xsl:with-param>
		</xsl:call-template>
	  </xsl:when>
	  <xsl:when test="parent::ReversalIndexEntry_ReferringSenses">
		<xsl:call-template name="ProcessSense">
		  <xsl:with-param name="id">
		<xsl:value-of select="ancestor::ReversalIndexEntry/@id"/><xsl:text>_</xsl:text><xsl:value-of select="@target"/>
	  </xsl:with-param>
		</xsl:call-template>
	  </xsl:when>
	  <xsl:when test="count(../LexEntryLink)+count(../LexSenseLink) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>

  <!-- change LexSense into span with the proper attribute values -->

  <xsl:template match="LexSense">
		<xsl:call-template name="ProcessSense">
		  <xsl:with-param name="id" select="@id"/>
		</xsl:call-template>
  </xsl:template>

  <!-- ****************************************** -->
  <!-- process multistring (AStr) owning elements -->
  <!-- ****************************************** -->

  <xsl:template match="ReversalIndexEntry_ReversalForm">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="MoMorphSynAnalysisLink_MLPartOfSpeech">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexSense_Definition">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="CmPossibility_Abbreviation">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="CmPossibility_Name">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexSense_Gloss">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexExampleSentence_Example">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="CmTranslation_Translation">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexEntryLink_MLHeadWord">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexEntryType_ReverseAbbr">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="_MLHeadWord">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexPronunciation_Form">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexSense_EncyclopedicInfo">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexSense_Restrictions">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexRefType_ReverseAbbreviation">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexRefType_ReverseName">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexSenseLink_MLOwnerOutlineName">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexEntry_CitationForm">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="MoInflAffixSlot_Name">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexEtymology_Form">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexEtymology_Gloss">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexEtymology_Comment">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="MoMorphSynAnalysisLink_MLInflectionClass">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexEntryRef_Summary">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexSense_AnthroNote">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexSense_Bibliography">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexSense_DiscourseNote">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexSense_PhonologyNote">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexSense_SemanticsNote">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexSense_SocioLinguisticsNote">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexSense_GeneralNote">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexEntry_Bibliography">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexEntry_Comment">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexEntry_LiteralMeaning">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="MoForm_Form">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexEntry_SummaryDefinition">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexSenseLink_ReversalName">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>


  <!-- ******************************************* -->
  <!-- process multiunicode (AUni) owning elements -->
  <!-- ******************************************* -->

  <xsl:template match="LexEtymology_Source">
	<xsl:call-template name="ProcessMultiUnicode"></xsl:call-template>
  </xsl:template>
  <xsl:template match="MoMorphType_Prefix">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="MoMorphType_Postfix">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>


  <!-- ************************************ -->
  <!-- process string (Str) owning elements -->
  <!-- ************************************ -->

  <xsl:template match="LexSense_ScientificName">
	<xsl:call-template name="ProcessString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexEntry_DateCreated">
	<xsl:call-template name="ProcessString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexEntry_DateModified">
	<xsl:call-template name="ProcessString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="MoMorphSynAnalysisLink_FeaturesTSS">
	<xsl:call-template name="ProcessString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="MoMorphSynAnalysisLink_ExceptionFeaturesTSS">
	<xsl:call-template name="ProcessString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexPronunciation_CVPattern">
	<xsl:call-template name="ProcessString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexPronunciation_Tone">
	<xsl:call-template name="ProcessString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexSense_Source">
	<xsl:call-template name="ProcessString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="PhEnvironment_StringRepresentation">
	<xsl:call-template name="ProcessString"></xsl:call-template>
  </xsl:template>



  <xsl:template match="LiteralString">
	<xsl:if test="Str/Run/@fontsize and Str/Run/@editable='not'">
	  <span><xsl:attribute name="class">xlanguagetag</xsl:attribute><xsl:attribute name="lang"><xsl:value-of select="Str/Run/@ws"/></xsl:attribute>
		<xsl:value-of select="Str/Run"/>
	  </span>
	</xsl:if>
  </xsl:template>

  <!-- ************************** -->
  <!-- * Process custom fields. * -->
  <!-- ************************** -->

  <xsl:template match="*[@userlabel]">
	<xsl:if test="Str/Run">
	  <xsl:call-template name="ProcessString"></xsl:call-template>
	</xsl:if>
	<xsl:if test="AStr/Run">
	  <xsl:call-template name="ProcessMultiString"></xsl:call-template>
	</xsl:if>
  </xsl:template>



  <!-- process LexEntry_LexemeForm -->

  <xsl:template match="LexEntry_LexemeForm">
	<xsl:choose>
	  <xsl:when test="not(MoForm/*)">
	  </xsl:when>
	  <xsl:when test="MoForm/MoForm_MorphType/MoMorphTypeLink">
	  </xsl:when>
	  <xsl:when test="preceding-sibling::LexEntry_LexemeForm[1]/MoForm and following-sibling::LexEntry_LexemeForm[1]/MoForm">
		<xsl:choose>
		  <xsl:when test="MoForm/Alternative/MoForm_Form/AStr">
			<xsl:for-each select="MoForm/Alternative">
			  <span class="xitem"><xsl:attribute name="lang"><xsl:value-of select="@ws"/></xsl:attribute>
				<xsl:value-of select="../../../LexEntry_LexemeForm/MoForm/MoForm_MorphType/MoMorphTypeLink/MoMorphType_Prefix/AUni"/>
				<xsl:value-of select="MoForm_Form/AStr/Run"/>
				<xsl:value-of select="../../../LexEntry_LexemeForm/MoForm/MoForm_MorphType/MoMorphTypeLink/MoMorphType_Postfix/AUni"/>
				<xsl:if test="../../../LexEntry_HomographNumber/Integer/@val">
				  <span class="xhomographnumber"><xsl:attribute name="lang"><xsl:value-of select="/html/WritingSystemInfo[@analTag='']/@lang"/></xsl:attribute><xsl:value-of select="../../../LexEntry_HomographNumber/Integer/@val"/></span>
				</xsl:if>
			  </span>
			</xsl:for-each>
		  </xsl:when>
		  <xsl:otherwise>
			<xsl:value-of select="../LexEntry_LexemeForm/MoForm/MoForm_MorphType/MoMorphTypeLink/MoMorphType_Prefix/AUni"/>
			<xsl:value-of select="MoForm/MoForm_Form/AStr/Run"/>
			<xsl:value-of select="../LexEntry_LexemeForm/MoForm/MoForm_MorphType/MoMorphTypeLink/MoMorphType_Postfix/AUni"/>
			<xsl:if test="../LexEntry_HomographNumber/Integer/@val">
			  <span class="xhomographnumber"><xsl:attribute name="lang"><xsl:value-of select="/html/WritingSystemInfo[@analTag='']/@lang"/></xsl:attribute><xsl:value-of select="../LexEntry_HomographNumber/Integer/@val"/></span>
			</xsl:if>
		  </xsl:otherwise>
		</xsl:choose>
	  </xsl:when>
	  <xsl:when test="MoForm/span/MoForm_Form">
		<xsl:for-each select="MoForm">
		  <xsl:apply-templates/>
		</xsl:for-each>
	  </xsl:when>
	  <xsl:otherwise>
		<xsl:for-each select="MoForm/MoForm_Form">
		  <xsl:call-template name="ProcessMultiString"></xsl:call-template>
		</xsl:for-each>
	  </xsl:otherwise>
	</xsl:choose>
  </xsl:template>


  <!-- *************************************************************** -->
  <!-- * Some span classes need to be tagged to reflect their content. -->
  <!-- *************************************************************** -->

  <xsl:template match="span">
	<xsl:copy>
	  <xsl:copy-of select="@title"/>
	  <xsl:choose>
		<xsl:when test="ReversalIndexEntry_ReversalForm/AStr/Run">
		  <xsl:call-template name="SetAnalAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="ReversalIndexEntry_ReversalForm//AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="LexEntry_LexemeForm/MoForm/MoForm_Form/AStr/Run">
		  <xsl:call-template name="SetVernAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="LexEntry_LexemeForm/MoForm/MoForm_Form/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="LexEntry_CitationForm/AStr/Run">
		  <xsl:call-template name="SetVernAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="LexEntry_CitationForm/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="LexSenseLink_MLOwnerOutlineName/AStr/Run">
		  <xsl:call-template name="SetVernAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="LexSenseLink_MLOwnerOutlineName/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="LexEntryLink_MLHeadWord/AStr/Run">
		  <xsl:call-template name="SetVernAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="LexEntryLink_MLHeadWord/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="_MLHeadWord/AStr/Run">
		  <xsl:call-template name="SetVernAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="_MLHeadWord/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="LexEtymology_Form/AStr/Run">
		  <xsl:call-template name="SetVernAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="LexEtymology_Form/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="LexEtymology_Gloss/AStr/Run">
		  <xsl:call-template name="SetAnalAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="LexEtymology_Gloss/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="LexEtymology_Comment/AStr/Run">
		  <xsl:call-template name="SetAnalAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="LexEtymology_Comment/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<!--xsl:when test="LexEtymology_Source/AUni">
		  <xsl:call-template name="SetVernAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="LexEtymology_Source/AUni[1]/@ws"/></xsl:call-template>
		</xsl:when-->
		<xsl:when test="LexExampleSentence_Example/AStr/Run">
		  <xsl:call-template name="SetVernAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="LexExampleSentence_Example/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="MoForm_Form/AStr/Run">
		  <xsl:call-template name="SetVernAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="MoForm_Form/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="LexPronunciation_Form/AStr/Run">
		  <xsl:call-template name="SetPronAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="LexPronunciation_Form/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="LexSense_ScientificName/Str/Run">
		  <xsl:call-template name="SetAnalAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="LexSense_ScientificName/Str/Run/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="*[@target]/*/AStr/Run">
		  <xsl:call-template name="SetAnalAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="*[@target]/*/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="*/AStr/Run">
		  <xsl:call-template name="SetAnalAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="*/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>

		<xsl:when test="LexEntry_LexemeForm/MoForm/Alternative/MoForm_Form/AStr/Run">
		  <xsl:call-template name="SetVernAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="LexEntry_LexemeForm/MoForm/Alternative/MoForm_Form/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="Alternative/LexEntry_CitationForm/AStr/Run">
		  <xsl:call-template name="SetVernAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="Alternative/LexEntry_CitationForm/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="Alternative/LexSenseLink_MLOwnerOutlineName/AStr/Run">
		  <xsl:call-template name="SetVernAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="Alternative/LexSenseLink_MLOwnerOutlineName/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="Alternative/LexEntryLink_MLHeadWord/AStr/Run">
		  <xsl:call-template name="SetVernAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="Alternative/LexEntryLink_MLHeadWord/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="Alternative/LexEtymology_Form/AStr/Run">
		  <xsl:call-template name="SetVernAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="Alternative/LexEtymology_Form/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="Alternative/LexExampleSentence_Example/AStr/Run">
		  <xsl:call-template name="SetVernAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="Alternative/LexExampleSentence_Example/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="Alternative/MoForm_Form/AStr/Run">
		  <xsl:call-template name="SetVernAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="Alternative/MoForm_Form/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="Alternative/LexPronunciation_Form/AStr/Run">
		  <xsl:call-template name="SetPronAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="Alternative/LexPronunciation_Form/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="MoMorphSynAnalysisLink_MLPartOfSpeech/AStr/Run">
		  <xsl:call-template name="SetAnalAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="MoMorphSynAnalysisLink_MLPartOfSpeech/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="LexSense_Definition/AStr/Run">
		  <xsl:call-template name="SetAnalAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="LexSense_Definition/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="LexRefTypeLink/CmPossibility_Abbreviation/AStr/Run">
		  <xsl:call-template name="SetAnalAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="LexRefTypeLink/CmPossibility_Abbreviation/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="CmPossibility_Abbreviation/AStr/Run">
		  <xsl:call-template name="SetAnalAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="CmPossibility_Abbreviation/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="CmPossibility_Name/AStr/Run">
		  <xsl:call-template name="SetAnalAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="CmPossibility_Name/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="LexSense_Gloss/AStr/Run">
		  <xsl:call-template name="SetAnalAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="LexSense_Gloss/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="CmTranslation_Translation/AStr/Run">
		  <xsl:call-template name="SetAnalAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="CmTranslation_Translation/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="LexEntryRef_VariantEntryTypes/LexEntryTypeLink/LexEntryType_ReverseAbbr/AStr/Run">
		  <xsl:call-template name="SetAnalAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="LexEntryRef_VariantEntryTypes/LexEntryTypeLink/LexEntryType_ReverseAbbr/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="LexEntryRef_ComplexEntryTypes/LexEntryTypeLink/LexEntryType_ReverseAbbr/AStr/Run">
		  <xsl:call-template name="SetAnalAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="LexEntryRef_ComplexEntryTypes/LexEntryTypeLink/LexEntryType_ReverseAbbr/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
<!--
LexSense_EncyclopedicInfo/AStr/Run
LexSense_Restrictions/AStr/Run
LexRefTypeLink/LexRefType_ReverseAbbreviation/AStr/Run
LexRefTypeLink/LexRefType_ReverseName/AStr/Run
MoInflAffixSlot_Name/AStr/Run
-->
		<xsl:when test="Alternative/*[@target]/*/AStr/Run">
		  <xsl:call-template name="SetAnalAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="Alternative/*[@target]/*/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="Alternative/*/AStr/Run">
		  <xsl:call-template name="SetAnalAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="Alternative/*/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:otherwise>
		  <xsl:copy-of select="@class"/>
		  <!--xsl:attribute name="dir"><xsl:value-of select="/html/WritingSystemInfo[@analTag='']/@dir"/></xsl:attribute-->
		</xsl:otherwise>
	  </xsl:choose>
	  <xsl:apply-templates/>
	</xsl:copy>
  </xsl:template>

  <xsl:template match="Alternative">
	<xsl:choose>
	  <xsl:when test="count(../Alternative) > 1">
		<span class="xitem"><xsl:attribute name="lang"><xsl:value-of select="@ws"/></xsl:attribute><xsl:apply-templates/></span>
	  </xsl:when>
	  <xsl:otherwise>
		<xsl:apply-templates/>
	  </xsl:otherwise>
	</xsl:choose>
  </xsl:template>


  <!-- ***************************************** -->
  <!-- * This is the basic default processing. * -->
  <!-- ***************************************** -->

  <xsl:template match="*">
	<xsl:copy>
	  <xsl:copy-of select="@*"/>
	  <xsl:apply-templates/>
	</xsl:copy>
  </xsl:template>


  <!-- ******************* -->
  <!-- * NAMED TEMPLATES * -->
  <!-- ******************* -->

<xsl:template name="DebugOutput">
  <xsl:param name="ws"/>
  <xsl:comment>ws="<xsl:value-of select="$ws"/>"</xsl:comment>
</xsl:template>

  <!-- process content of a LexSense (which may be disguised as a LexSenseLink) -->

  <xsl:template name="ProcessSense">
	<xsl:param name="id"/>
	<span class="sense">
	  <xsl:attribute name="id"><xsl:value-of select="$id"/></xsl:attribute>
	  <!-- if an entry is numbered -->
	  <xsl:if test="preceding-sibling::LiteralString[1]/Str/Run[@bold='on']">
		<span class="xsensenumber"><xsl:value-of select="preceding-sibling::LiteralString[1]/Str/Run[@bold='on']"/></span>
	  </xsl:if>
	  <xsl:apply-templates/>
	</span>
  </xsl:template>

  <!-- process content that consists of one or more <AStr> elements -->
  <!-- TODO: handle styles as well as writing systems in the individual runs? -->

  <xsl:template name="ProcessMultiString">
	<xsl:for-each select="AStr">
	  <xsl:choose>
		<xsl:when test="not(@ws=../AStr[1]/@ws)">
		  <span><xsl:attribute name="lang"><xsl:value-of select="@ws"/></xsl:attribute>
		  <xsl:call-template name="ProcessAStrRuns"/>
		  </span>
		</xsl:when>
		<xsl:otherwise>
		  <xsl:call-template name="ProcessAStrRuns"/>
		</xsl:otherwise>
	  </xsl:choose>
	</xsl:for-each>
	<xsl:for-each select="Alternative">
	  <xsl:for-each select="AStr">
		<span class="xitem"><xsl:attribute name="lang"><xsl:value-of select="@ws"/></xsl:attribute>
		  <xsl:call-template name="ProcessAStrRuns"/>
		</span>
	  </xsl:for-each>
	</xsl:for-each>
  </xsl:template>


  <!-- process the <Run> elements inside an <AStr> element -->
  <!-- TODO: handle styles as well as writing systems in the individual runs? -->

  <xsl:template name="ProcessAStrRuns">
	<xsl:for-each select="Run">
	  <xsl:choose>
		<xsl:when test="not(@ws=../@ws)">
			<xsl:choose>
				<xsl:when test="@superscript='sub'">
					<span class="xhomographnumber"><xsl:attribute name="lang"><xsl:value-of select="@ws"/></xsl:attribute><xsl:value-of select="."/></span>
				</xsl:when>
				<xsl:otherwise>
					<span><xsl:attribute name="lang"><xsl:value-of select="@ws"/></xsl:attribute><xsl:value-of select="."/></span>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:when>
		<xsl:otherwise>
			<xsl:choose>
				<xsl:when test="@superscript='sub'">
					<span class="xhomographnumber"><xsl:value-of select="."/></span>
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="."/>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:otherwise>
	  </xsl:choose>
	</xsl:for-each>
  </xsl:template>

  <!-- process content that consists of one or more <AUni> elements -->

  <xsl:template name="ProcessMultiUnicode">
	<xsl:for-each select="AUni">
	  <xsl:value-of select="."/>
	</xsl:for-each>
	<xsl:for-each select="Alternative">
	  <xsl:for-each select="AUni">
		<span class="xitem"><xsl:attribute name="lang"><xsl:value-of select="@ws"/></xsl:attribute>
		  <xsl:value-of select="."/>
		</span>
	  </xsl:for-each>
	</xsl:for-each>
  </xsl:template>


  <!-- process content that consists of a <Str> element -->

  <xsl:template name="ProcessString">
	<!-- TODO: translate individual Runs to spans? -->
	<xsl:for-each select="Str/Run">
	  <xsl:value-of select="."/>
	</xsl:for-each>
  </xsl:template>


  <!-- convert all \ characters to / -->

  <xsl:template name="FixSlashes">
	<xsl:param name="text"/>
	<xsl:choose>
	  <xsl:when test="contains($text, '\')">
		<xsl:value-of select="substring-before($text, '\')"/>
		<xsl:value-of select="'/'"/>
		<xsl:call-template name="FixSlashes">
		  <xsl:with-param name="text" select="substring-after($text, '\')"/>
		</xsl:call-template>
	  </xsl:when>
	  <xsl:otherwise>
		<xsl:value-of select="$text"/>
	  </xsl:otherwise>
	</xsl:choose>
  </xsl:template>

  <!-- tag the class name for a vernacular language -->

  <xsl:template name="SetVernAttrs">
	<xsl:param name="Class"/>
	<xsl:param name="Lang"/>
	<xsl:attribute name="class">
	  <xsl:value-of select="$Class"/>
	  <xsl:value-of select="/html/WritingSystemInfo[@lang=$Lang]/@vernTag"/>
	</xsl:attribute>
	<xsl:attribute name="lang"><xsl:value-of select="$Lang"/></xsl:attribute>
	<!--xsl:attribute name="dir"><xsl:value-of select="/html/WritingSystemInfo[@lang=$Lang]/@dir"/></xsl:attribute-->
  </xsl:template>

  <!-- tag the class name for an analysis language -->

  <xsl:template name="SetAnalAttrs">
	<xsl:param name="Class"/>
	<xsl:param name="Lang"/>
	<xsl:attribute name="class">
	  <xsl:value-of select="$Class"/>
	  <xsl:value-of select="/html/WritingSystemInfo[@lang=$Lang]/@analTag"/>
	</xsl:attribute>
	<xsl:attribute name="lang"><xsl:value-of select="$Lang"/></xsl:attribute>
	<!--xsl:attribute name="dir"><xsl:value-of select="/html/WritingSystemInfo[@lang=$Lang]/@dir"/></xsl:attribute-->
  </xsl:template>

  <!-- tag the class name for a pronunciation language -->

  <xsl:template name="SetPronAttrs">
	<xsl:param name="Class"/>
	<xsl:param name="Lang"/>
	<xsl:attribute name="class">
	  <xsl:value-of select="$Class"/>
	  <xsl:value-of select="/html/WritingSystemInfo[@lang=$Lang]/@pronTag"/>
	</xsl:attribute>
	<xsl:attribute name="lang"><xsl:value-of select="$Lang"/></xsl:attribute>
	<!--xsl:attribute name="dir"><xsl:value-of select="/html/WritingSystemInfo[@lang=$Lang]/@dir"/></xsl:attribute-->
  </xsl:template>

</xsl:stylesheet>
