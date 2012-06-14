<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="text" omit-xml-declaration="yes" encoding="UTF-8"/>

<!-- This stylesheet works on the output of Configured.xsl to produce a "standard format" file
	 somewhat like MDF. -->

<!-- Strip all white space and leave it up to the stylesheet text elements below to put in
	 appropriate spacing. -->

<xsl:strip-space elements="*"/>
<xsl:preserve-space elements="Run"/><!-- but these spaces are significant! -->


<!--Find entry and indicate with an \lx field marker-->

<xsl:template match="/ExportedDictionary/LexEntry">
  <xsl:choose>
	<!-- If this is not the very first entry, add a blank line before it. -->
	<xsl:when test="position()>1">
	<xsl:text>&#13;&#10;</xsl:text>
	</xsl:when>
	<xsl:otherwise>
	<!-- Otherwise, is there anything useful to write? -->
	</xsl:otherwise>
  </xsl:choose>
  <xsl:text>\lx&#32;</xsl:text>
  <xsl:value-of select="LexEntry_HeadWord[1]/AStr/Run"/>
  <xsl:text>&#13;&#10;</xsl:text>

  <xsl:if test="LexEntry_HomographNumber/Integer/@val">
	<xsl:text>\hm&#32;</xsl:text>
	<xsl:value-of select="LexEntry_HomographNumber/Integer/@val"/>
	<xsl:text>&#13;&#10;</xsl:text>
  </xsl:if>

  <xsl:apply-templates/>
</xsl:template>


<!-- already handled elsewhere, so ignore contents. -->

<xsl:template match="LexEntry_HeadWord"/>
<xsl:template match="LexEntry_HomographNumber"/>


<!-- Output the primary lexeme form(s) and morphtype. -->

<xsl:template match="LexEntry_LexemeForm">
  <xsl:for-each select="MoForm/MoForm_Form/AStr">
	<xsl:text>\lx</xsl:text>
	<xsl:text>_</xsl:text>
	<xsl:value-of select="@ws"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:value-of select="Run"/>
	<xsl:text>&#13;&#10;</xsl:text>
  </xsl:for-each>
</xsl:template>


<!-- Output the pronunciation(s) (JohnT: added cv, tn, pln, pla..subitems don't repeat, but wanted to keep this self-contained
since I don't understand the full design of this)-->

<xsl:template match="LexEntry_Pronunciations">
	<xsl:for-each select="LexPronunciation">
		<xsl:for-each select="LexPronunciation_Form/AStr">
			<xsl:text>\ph</xsl:text>
			<xsl:text>_</xsl:text>
			<xsl:value-of select="@ws"/>
			<xsl:text>&#32;</xsl:text>
			<xsl:value-of select="Run"/>
			<xsl:text>&#13;&#10;</xsl:text>
		</xsl:for-each>
		<xsl:for-each select="LexPronunciation_CVPattern/Str/Run">
			<xsl:text>\cv</xsl:text>
			<xsl:text>&#32;</xsl:text>
			<xsl:value-of select="."/>
			<xsl:text>&#13;&#10;</xsl:text>
		</xsl:for-each>
		<xsl:for-each select="LexPronunciation_Tone/Str/Run">
			<xsl:text>\tn</xsl:text>
			<xsl:text>&#32;</xsl:text>
			<xsl:value-of select="."/>
			<xsl:text>&#13;&#10;</xsl:text>
		</xsl:for-each>
		<xsl:for-each select="LexPronunciation_Location/Link/Alt[@abbr]">
			<xsl:text>\pla</xsl:text>
			<xsl:text>&#32;</xsl:text>
			<xsl:value-of select="@abbr"/>
			<xsl:text>&#13;&#10;</xsl:text>
		</xsl:for-each>
		<xsl:for-each select="LexPronunciation_Location/Link/Alt[@name]">
			<xsl:text>\pln</xsl:text>
			<xsl:text>&#32;</xsl:text>
			<xsl:value-of select="@name"/>
			<xsl:text>&#13;&#10;</xsl:text>
		</xsl:for-each>
	</xsl:for-each>
</xsl:template>


<!-- Output the alternate forms and morphtypes (not part of standard MDF). -->

<xsl:template match="LexEntry_AlternateForms">
  <xsl:for-each select="Allomorph/MoForm_Form/AStr">
	<xsl:text>\af</xsl:text>
	<xsl:text>_</xsl:text>
	<xsl:value-of select="@ws"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:value-of select="Run"/><!-- assumes a single Run -->
	<xsl:text>&#13;&#10;</xsl:text>
  </xsl:for-each>
  <xsl:choose>
	<xsl:when test="Allomorph/MoForm_MorphType/Link/Alt/@name">
	  <xsl:text>\mtaf&#32;</xsl:text>
	  <xsl:value-of select="Allomorph/MoForm_MorphType/Link/Alt/@name"/>
	  <xsl:text>&#13;&#10;</xsl:text>
	</xsl:when>
	<xsl:when test="Allomorph/MoForm_MorphType/Link/Alt/@abbr">
	  <xsl:text>\mtaf&#32;</xsl:text>
	  <xsl:value-of select="Allomorph/MoForm_MorphType/Link/Alt/@abbr"/>
	  <xsl:text>&#13;&#10;</xsl:text>
	</xsl:when>
  </xsl:choose>
</xsl:template>


<!-- Output the citation form(s). -->

<xsl:template match="LexEntry_CitationForm">
  <xsl:for-each select="AStr">
	<xsl:text>\lc</xsl:text>
	<xsl:text>_</xsl:text>
	<xsl:value-of select="@ws"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:value-of select="Run"/><!-- assumes a single Run -->
	<xsl:text>&#13;&#10;</xsl:text>
  </xsl:for-each>
</xsl:template>


<!-- Output the sense number for each LexSense. -->

<xsl:template match="LexSense">
  <xsl:choose>
	<xsl:when test="LexSense_Pictures">
	  <xsl:text>\pcsn&#32;</xsl:text>
	  <xsl:number format="1.1.1" level="multiple" count="LexSense"/>
	  <xsl:text>&#13;&#10;</xsl:text>
	</xsl:when>
	<xsl:when test="ancestor::LexEntry_Senses//LexSense_Pictures"/>
	<xsl:otherwise>
	  <xsl:text>\sn&#32;</xsl:text>
	  <xsl:number format="1.1.1" level="multiple" count="LexSense"/>
	  <xsl:text>&#13;&#10;</xsl:text>
	</xsl:otherwise>
  </xsl:choose>
  <xsl:apply-templates/>
</xsl:template>

<!-- already handled above, so ignore contents. -->

<xsl:template match="MoStemMsa_PartOfSpeech"/>
<xsl:template match="FsFeatStruc"/>
<xsl:template match="MoInflAffMsa_PartOfSpeech"/>
<xsl:template match="MoDerivStepMsa_PartOfSpeech"/>
<xsl:template match="MoUnclassifiedAffixMsa_PartOfSpeech"/>
<xsl:template match="MoDerivAffMsa_FromPartOfSpeech"/>
<xsl:template match="MoDerivAffMsa_ToPartOfSpeech"/>


<!-- Output the Part of Speech for each LexSense -->

<xsl:template match="MoMorphSynAnalysisLink_MLPartOfSpeech">
  <xsl:for-each select="AStr">
	<xsl:text>\ps</xsl:text>
	<xsl:text>_</xsl:text>
	<xsl:value-of select="@ws"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:value-of select="Run"/>
	<xsl:text>&#13;&#10;</xsl:text>
  </xsl:for-each>
</xsl:template>


<!-- Output the Affix Slot for the Part of Speech -->

<xsl:template match="MoInflAffMsa_Slots">
  <xsl:for-each select="MoInflAffixSlotLink/MoInflAffixSlot_Name/AStr">
	<xsl:text>\psl</xsl:text>
	<xsl:text>_</xsl:text>
	<xsl:value-of select="@ws"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:value-of select="Run"/>
	<xsl:text>&#13;&#10;</xsl:text>
  </xsl:for-each>
</xsl:template>


<!-- Output the Features for the Part of Speech -->

<xsl:template match="MoMorphSynAnalysisLink_FeaturesTSS">
  <xsl:text>\psft&#32;</xsl:text>
  <xsl:value-of select="Str/Run"/>
  <xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output the Exception Features for the Part of Speech -->

<xsl:template match="MoMorphSynAnalysisLink_ExceptionFeaturesTSS">
  <xsl:text>\psex&#32;</xsl:text>
  <xsl:value-of select="Str/Run"/>
  <xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output the Inflection Class -->

<xsl:template match="MoMorphSynAnalysisLink_MLInflectionClass">
  <xsl:for-each select="AStr">
	<xsl:text>\psic</xsl:text>
	<xsl:text>_</xsl:text>
	<xsl:value-of select="@ws"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:value-of select="Run"/>
	<xsl:text>&#13;&#10;</xsl:text>
  </xsl:for-each>
</xsl:template>


<!-- Output the gloss. -->

<xsl:template match="LexSense_Gloss">
  <xsl:text>\g</xsl:text>
  <xsl:text>_</xsl:text>
  <xsl:value-of select="AStr/@ws"/>
  <xsl:text>&#32;</xsl:text>
  <xsl:value-of select="AStr/Run"/>
  <xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output the definition. -->

<xsl:template match="LexSense_Definition">
  <xsl:text>\d</xsl:text>
  <xsl:text>_</xsl:text>
  <xsl:value-of select="AStr/@ws"/>
  <xsl:text>&#32;</xsl:text>
  <xsl:value-of select="AStr/Run"/>
  <xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output the semantic domains. -->

<xsl:template match="LexSense_SemanticDomains">
  <xsl:for-each select="Link/Alt">
	<xsl:if test="@abbr">
	  <xsl:text>\is</xsl:text>
	  <xsl:text>_</xsl:text>
	  <xsl:value-of select="@ws"/>
	  <xsl:text>&#32;</xsl:text>
	  <xsl:value-of select="@abbr"/>
	  <xsl:text>&#13;&#10;</xsl:text>
	</xsl:if>
	<xsl:if test="@name">
	  <xsl:text>\sd</xsl:text>
	  <xsl:text>_</xsl:text>
	  <xsl:value-of select="@ws"/>
	  <xsl:text>&#32;</xsl:text>
	  <xsl:value-of select="@name"/>
	  <xsl:text>&#13;&#10;</xsl:text>
	</xsl:if>
  </xsl:for-each>
</xsl:template>


<!-- Output the anthropology codes. -->

<xsl:template match="LexSense_AnthroCodes">
  <xsl:for-each select="Link/Alt">
	<xsl:if test="@abbr">
	  <xsl:text>\iac</xsl:text>
	  <xsl:text>_</xsl:text>
	  <xsl:value-of select="@ws"/>
	  <xsl:text>&#32;</xsl:text>
	  <xsl:value-of select="@abbr"/>
	  <xsl:text>&#13;&#10;</xsl:text>
	</xsl:if>
	<xsl:if test="@name">
	  <xsl:text>\ac</xsl:text>
	  <xsl:text>_</xsl:text>
	  <xsl:value-of select="@ws"/>
	  <xsl:text>&#32;</xsl:text>
	  <xsl:value-of select="@name"/>
	  <xsl:text>&#13;&#10;</xsl:text>
	</xsl:if>
  </xsl:for-each>
</xsl:template>


<!-- Output the domain types. -->

<xsl:template match="LexSense_DomainTypes">
  <xsl:for-each select="Link/Alt">
	<xsl:choose>
	  <xsl:when test="@name">
		<xsl:text>\do</xsl:text>
		<xsl:text>_</xsl:text>
		<xsl:value-of select="@ws"/>
		<xsl:text>&#32;</xsl:text>
		<xsl:value-of select="@name"/>
		<xsl:text>&#13;&#10;</xsl:text>
	  </xsl:when>
	  <xsl:when test="@abbr">
		<xsl:text>\do</xsl:text>
		<xsl:text>_</xsl:text>
		<xsl:value-of select="@ws"/>
		<xsl:text>&#32;</xsl:text>
		<xsl:value-of select="@abbr"/>
		<xsl:text>&#13;&#10;</xsl:text>
	  </xsl:when>
	</xsl:choose>
  </xsl:for-each>
</xsl:template>


<!-- Output the usage types. -->

<xsl:template match="LexSense_UsageTypes">
  <xsl:for-each select="Link/Alt">
	<xsl:choose>
	  <xsl:when test="@name">
		<xsl:text>\u</xsl:text>
		<xsl:text>_</xsl:text>
		<xsl:value-of select="@ws"/>
		<xsl:text>&#32;</xsl:text>
		<xsl:value-of select="@name"/>
		<xsl:text>&#13;&#10;</xsl:text>
	  </xsl:when>
	  <xsl:when test="@abbr">
		<xsl:text>\u</xsl:text>
		<xsl:text>_</xsl:text>
		<xsl:value-of select="@ws"/>
		<xsl:text>&#32;</xsl:text>
		<xsl:value-of select="@abbr"/>
		<xsl:text>&#13;&#10;</xsl:text>
	  </xsl:when>
	</xsl:choose>
  </xsl:for-each>
</xsl:template>


<!-- Output the status. -->

<xsl:template match="LexSense_Status">
  <xsl:for-each select="Link/Alt">
	<xsl:choose>
	  <xsl:when test="@name">
		<xsl:text>\st</xsl:text>
		<xsl:text>_</xsl:text>
		<xsl:value-of select="@ws"/>
		<xsl:text>&#32;</xsl:text>
		<xsl:value-of select="@name"/>
		<xsl:text>&#13;&#10;</xsl:text>
	  </xsl:when>
	  <xsl:when test="@abbr">
		<xsl:text>\st</xsl:text>
		<xsl:text>_</xsl:text>
		<xsl:value-of select="@ws"/>
		<xsl:text>&#32;</xsl:text>
		<xsl:value-of select="@abbr"/>
		<xsl:text>&#13;&#10;</xsl:text>
	  </xsl:when>
	</xsl:choose>
  </xsl:for-each>
</xsl:template>



<!-- Output a vernacular example sentence. -->

<xsl:template match="LexExampleSentence_Example">
  <xsl:text>\x</xsl:text>
  <xsl:text>_</xsl:text>
  <xsl:value-of select="AStr/@ws"/>
  <xsl:text>&#32;</xsl:text>
  <xsl:value-of select="AStr/Run"/>
  <xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output a translation of an example sentence. -->

<xsl:template match="CmTranslation_Translation">
  <xsl:text>\x</xsl:text>
  <xsl:text>_</xsl:text>
  <xsl:value-of select="AStr/@ws"/>
  <xsl:text>&#32;</xsl:text>
  <xsl:value-of select="AStr/Run"/>
  <xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output the encyclopedic information. -->

<xsl:template match="LexSense_EncyclopedicInfo">
  <xsl:text>\e</xsl:text>
  <xsl:text>_</xsl:text>
  <xsl:value-of select="AStr/@ws"/>
  <xsl:text>&#32;</xsl:text>
  <xsl:value-of select="AStr/Run"/>
  <xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output the general note. -->

<xsl:template match="LexSense_GeneralNote">
  <xsl:text>\nt</xsl:text>
  <xsl:text>_</xsl:text>
  <xsl:value-of select="AStr/@ws"/>
  <xsl:text>&#32;</xsl:text>
  <xsl:value-of select="AStr/Run"/>
  <xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output the grammar note. -->

<xsl:template match="LexSense_GrammarNote">
  <xsl:text>\ng</xsl:text>
  <xsl:text>_</xsl:text>
  <xsl:value-of select="AStr/@ws"/>
  <xsl:text>&#32;</xsl:text>
  <xsl:value-of select="AStr/Run"/>
  <xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output the restrictions. -->

<xsl:template match="LexSense_Restrictions">
  <xsl:text>\rst</xsl:text>
  <xsl:text>_</xsl:text>
  <xsl:value-of select="AStr/@ws"/>
  <xsl:text>&#32;</xsl:text>
  <xsl:value-of select="AStr/Run"/>
  <xsl:text>&#13;&#10;</xsl:text>
</xsl:template>

<!-- Output the anthropology notes. -->

<xsl:template match="LexSense_AnthroNote">
  <xsl:text>\na</xsl:text>
  <xsl:text>_</xsl:text>
  <xsl:value-of select="AStr/@ws"/>
  <xsl:text>&#32;</xsl:text>
  <xsl:value-of select="AStr/Run"/>
  <xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output the bibliography information. -->

<xsl:template match="LexEntry_Bibliography">
  <xsl:text>\bb</xsl:text>
  <xsl:text>_</xsl:text>
  <xsl:value-of select="AStr/@ws"/>
  <xsl:text>&#32;</xsl:text>
  <xsl:value-of select="AStr/Run"/>
  <xsl:text>&#13;&#10;</xsl:text>
</xsl:template>
<xsl:template match="LexSense_Bibliography">
  <xsl:text>\bbs</xsl:text>
  <xsl:text>_</xsl:text>
  <xsl:value-of select="AStr/@ws"/>
  <xsl:text>&#32;</xsl:text>
  <xsl:value-of select="AStr/Run"/>
  <xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output the discourse notes. -->

<xsl:template match="LexSense_DiscourseNote">
  <xsl:text>\nd</xsl:text>
  <xsl:text>_</xsl:text>
  <xsl:value-of select="AStr/@ws"/>
  <xsl:text>&#32;</xsl:text>
  <xsl:value-of select="AStr/Run"/>
  <xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output the phonology notes. -->

<xsl:template match="LexSense_PhonologyNote">
  <xsl:text>\np</xsl:text>
  <xsl:text>_</xsl:text>
  <xsl:value-of select="AStr/@ws"/>
  <xsl:text>&#32;</xsl:text>
  <xsl:value-of select="AStr/Run"/>
  <xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output the semantics notes. -->

<xsl:template match="LexSense_SemanticsNote">
  <xsl:text>\nm</xsl:text>
  <xsl:text>_</xsl:text>
  <xsl:value-of select="AStr/@ws"/>
  <xsl:text>&#32;</xsl:text>
  <xsl:value-of select="AStr/Run"/>
  <xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output the sociolinguistics notes. -->

<xsl:template match="LexSense_SocioLinguisticsNote">
  <xsl:text>\ns</xsl:text>
  <xsl:text>_</xsl:text>
  <xsl:value-of select="AStr/@ws"/>
  <xsl:text>&#32;</xsl:text>
  <xsl:value-of select="AStr/Run"/>
  <xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output the literal meaning. -->

<xsl:template match="LexEntry_LiteralMeaning">
  <xsl:text>\lt</xsl:text>
  <xsl:text>_</xsl:text>
  <xsl:value-of select="AStr/@ws"/>
  <xsl:text>&#32;</xsl:text>
  <xsl:value-of select="AStr/Run"/>
  <xsl:text>&#13;&#10;</xsl:text>
</xsl:template>

<!-- Output a comment (note). -->

<xsl:template match="LexEntry_Comment">
  <xsl:text>\co</xsl:text>
  <xsl:text>_</xsl:text>
  <xsl:value-of select="AStr/@ws"/>
  <xsl:text>&#32;</xsl:text>
  <xsl:value-of select="AStr/Run"/>
  <xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output the scientific name. -->

<xsl:template match="LexSense_ScientificName">
  <xsl:text>\sc&#32;</xsl:text>
  <xsl:value-of select="Str/Run"/>
  <xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output the source information. -->

<xsl:template match="LexSense_Source">
  <xsl:text>\so&#32;</xsl:text>
  <xsl:value-of select="Str/Run"/>
  <xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output the lexical references. -->

<xsl:template match="LexReferenceLink">
  <xsl:choose>
	<xsl:when test="LexReferenceLink_Type/Link/Alt/@abbr">
	  <xsl:text>\lf</xsl:text>
	  <!--xsl:value-of select="LexReferenceLink_Type/Link/Alt/@ws"/-->
	  <xsl:text>&#32;</xsl:text>
	  <xsl:value-of select="LexReferenceLink_Type/Link/Alt/@abbr"/>
	  <xsl:text>&#13;&#10;</xsl:text>
	</xsl:when>
	<xsl:when test="LexReferenceLink_Type/Link/Alt/@name">
	  <xsl:text>\lf</xsl:text>
	  <!--xsl:value-of select="LexReferenceLink_Type/Link/Alt/@ws"/-->
	  <xsl:text>&#32;</xsl:text>
	  <xsl:value-of select="LexReferenceLink_Type/Link/Alt/@name"/>
	  <xsl:text>&#13;&#10;</xsl:text>
	</xsl:when>
	<xsl:when test="LexReferenceLink_Type/Link/Alt/@revabbr">
	  <xsl:text>\lf</xsl:text>
	  <!--xsl:value-of select="LexReferenceLink_Type/Link/Alt/@ws"/-->
	  <xsl:text>&#32;</xsl:text>
	  <xsl:value-of select="LexReferenceLink_Type/Link/Alt/@revabbr"/>
	  <xsl:text>&#13;&#10;</xsl:text>
	</xsl:when>
	<xsl:when test="LexReferenceLink_Type/Link/Alt/@revname">
	  <xsl:text>\lf</xsl:text>
	  <!--xsl:value-of select="LexReferenceLink_Type/Link/Alt/@ws"/-->
	  <xsl:text>&#32;</xsl:text>
	  <xsl:value-of select="LexReferenceLink_Type/Link/Alt/@revname"/>
	  <xsl:text>&#13;&#10;</xsl:text>
	</xsl:when>
  </xsl:choose>
  <xsl:for-each select="LexReference_Targets/Link">
	<xsl:choose>
	  <xsl:when test="Alt/@entry">
		<xsl:text>\lv</xsl:text>
		<!--xsl:value-of select="Alt/@ws"/-->
		<xsl:text>&#32;</xsl:text>
		<xsl:value-of select="Alt/@entry"/>
		<xsl:text>&#13;&#10;</xsl:text>
	  </xsl:when>
	  <xsl:when test="Alt/@sense">
		<xsl:text>\lv</xsl:text>
		<!--xsl:value-of select="Alt/@ws"/-->
		<xsl:text>&#32;</xsl:text>
		<xsl:value-of select="Alt/@sense"/>
		<xsl:text>&#13;&#10;</xsl:text>
	  </xsl:when>
	</xsl:choose>
  </xsl:for-each>
</xsl:template>


<!-- Output the picture file path and caption -->

<xsl:template match="CmPicture|CmPictureLink">
  <xsl:choose>
	<xsl:when test="CmPicture_OriginalFilePath">
	  <xsl:text>\pc&#32;</xsl:text>
	  <xsl:value-of select="CmPicture_OriginalFilePath"/>
	  <xsl:text>&#13;&#10;</xsl:text>
	</xsl:when>
	<xsl:when test="CmPicture_InternalFilePath">
	  <xsl:text>\pc&#32;</xsl:text>
	  <xsl:value-of select="CmPicture_InternalFilePath"/>
	  <xsl:text>&#13;&#10;</xsl:text>
	</xsl:when>
  </xsl:choose>
  <xsl:apply-templates/>
</xsl:template>
<xsl:template match="CmPicture_InternalFilePath"/>
<xsl:template match="CmPicture_OriginalFilePath"/>
<xsl:template match="CmPicture_Caption">
  <xsl:text>\pc</xsl:text>
  <xsl:text>_</xsl:text>
  <xsl:value-of select="AStr/@ws"/>
  <xsl:text>&#32;</xsl:text>
  <xsl:if test="../CmPictureLink_SenseNumberTSS">
	<xsl:value-of select="../CmPictureLink_SenseNumberTSS/Str/Run"/>
	<xsl:text>&#32;</xsl:text>
  </xsl:if>
  <xsl:value-of select="AStr/Run"/>
  <xsl:text>&#13;&#10;</xsl:text>
</xsl:template>
<xsl:template match="_PicturesOfSenses">
  <xsl:apply-templates/>
</xsl:template>


<!-- Ignore the Sense number output with the picture caption -->

<xsl:template match="CmPicture_SenseNumberTSS"/>
<xsl:template match="CmPictureLink_SenseNumberTSS"/>


<!-- Output the date modified. -->

<xsl:template match="LexEntry_DateModified">
  <xsl:text>\dt&#32;</xsl:text>
  <xsl:value-of select="Str/Run"/>
  <xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output the date created. -->

<xsl:template match="LexEntry_DateCreated">
  <xsl:text>\dtc&#32;</xsl:text>
  <xsl:value-of select="Str/Run"/>
  <xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output the Etymology Form -->

<xsl:template match="LexEtymology_Form">
  <xsl:for-each select="AStr">
	<xsl:text>\et_</xsl:text>
	<xsl:value-of select="@ws"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:value-of select="Run"/>
	<xsl:text>&#13;&#10;</xsl:text>
  </xsl:for-each>
</xsl:template>


<!-- Output the Etymology Gloss -->

<xsl:template match="LexEtymology_Gloss">
  <xsl:for-each select="AStr">
	<xsl:text>\eg_</xsl:text>
	<xsl:value-of select="@ws"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:value-of select="Run"/>
	<xsl:text>&#13;&#10;</xsl:text>
  </xsl:for-each>
</xsl:template>


<!-- Output the Etymology Comment -->

<xsl:template match="LexEtymology_Comment">
  <xsl:for-each select="AStr">
	<xsl:text>\ec_</xsl:text>
	<xsl:value-of select="@ws"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:value-of select="Run"/>
	<xsl:text>&#13;&#10;</xsl:text>
  </xsl:for-each>
</xsl:template>

<!-- Output the Etymology Source -->

<xsl:template match="LexEtymology_Source">
	<xsl:for-each select="AUni">
		<xsl:text>\es_</xsl:text>
		<xsl:value-of select="@ws"/>
		<xsl:text>&#32;</xsl:text>
		<xsl:value-of select="."/>
		<xsl:text>&#13;&#10;</xsl:text>
	</xsl:for-each>
</xsl:template>

<!-- Process subentries of this entry. -->

<xsl:template match="_AllComplexFormEntryBackRefs|_ComplexFormEntryBackRefs">
  <xsl:for-each select="LexEntryRefLink">
	<xsl:for-each select="LexEntryRefLink_OwningEntry/Link/Alt">
	  <xsl:text>\se</xsl:text>
	  <!--xsl:value-of select="@ws"/-->
	  <xsl:text>&#32;</xsl:text>
	  <xsl:value-of select="@entry"/>
	  <xsl:text>&#13;&#10;</xsl:text>
	</xsl:for-each>
	<xsl:for-each select="LexEntryRef_ComplexEntryTypes/Link/Alt">
	  <xsl:text>\cet</xsl:text>
	  <!--xsl:value-of select="@ws"/-->
	  <xsl:text>&#32;</xsl:text>
	  <xsl:value-of select="@revabbr"/>
	  <xsl:text>&#13;&#10;</xsl:text>
	</xsl:for-each>
	<xsl:for-each select="LexEntryRef_Summary/AStr">
	  <xsl:call-template name="ProcessAstr">
		<xsl:with-param name="Tag" select="'s'"/>
	  </xsl:call-template>
	</xsl:for-each>
	<xsl:for-each select="LexEntryRefLink_OwningEntry/LexEntry_SummaryDefinition/AStr">
	  <xsl:call-template name="ProcessAstr">
		<xsl:with-param name="Tag" select="'s'"/>
	  </xsl:call-template>
	</xsl:for-each>
	<xsl:for-each select="Paragraph/LexEntryRefLink_OwningEntry">
	  <xsl:call-template name="ProcessSubentry"/>
	</xsl:for-each>
  </xsl:for-each>
</xsl:template>

<xsl:template match="_ComplexFormEntryRefs">
	<xsl:text>\cet</xsl:text>
	<xsl:for-each select="LexEntryRefLink/LexEntryRef_ComplexEntryTypes/Link/Alt">
	  <xsl:text>&#32;</xsl:text>
	  <xsl:value-of select="@revabbr"/>
	  <xsl:if test="position() != last()">
			<xsl:text>;</xsl:text>
	  </xsl:if>
	</xsl:for-each>
	<xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Process variants of this entry. -->

<xsl:template match="_VariantFormEntryBackRefs">
  <xsl:for-each select="LexEntryRefLink">
	<xsl:for-each select="LexEntryRefLink_OwningEntry/Link/Alt">
	  <xsl:text>\va_</xsl:text>
	  <xsl:value-of select="@ws"/>
	  <xsl:text>&#32;</xsl:text>
	  <xsl:value-of select="@entry"/>
	  <xsl:text>&#13;&#10;</xsl:text>
	</xsl:for-each>
	<xsl:text>\vet</xsl:text>
	<xsl:for-each select="LexEntryRef_VariantEntryTypes/Link/Alt">
	  <xsl:text>&#32;</xsl:text>
	  <xsl:value-of select="@revabbr"/>
	  <xsl:if test="position() != last()">
			<xsl:text>;</xsl:text>
	  </xsl:if>
	</xsl:for-each>
	<xsl:text>&#13;&#10;</xsl:text>
	<xsl:for-each select="LexEntryRef_Summary/AStr">
	  <xsl:call-template name="ProcessAstr">
		<xsl:with-param name="Tag" select="'v'"/>
	  </xsl:call-template>
	</xsl:for-each>
	<xsl:for-each select="LexEntryRefLink_OwningEntry/LexEntry_SummaryDefinition/AStr">
	  <xsl:call-template name="ProcessAstr">
		<xsl:with-param name="Tag" select="'v'"/>
	  </xsl:call-template>
	</xsl:for-each>
  </xsl:for-each>
</xsl:template>


<!-- Process main entries for this variant (which is a minor entry). -->

<xsl:template match="_VisibleEntryRefs|_VisibleVariantEntryRefs">
  <xsl:for-each select="LexEntryRefLink">
	<xsl:for-each select="LexEntryRef_VariantEntryTypes/Link/Alt">
	  <xsl:text>\vt</xsl:text>
	  <xsl:text>&#32;</xsl:text>
	  <xsl:value-of select="@abbr"/>
	  <xsl:if test="position() != last()">
		   <xsl:text>;</xsl:text>
	  </xsl:if>
	  <xsl:text>&#13;&#10;</xsl:text>
	</xsl:for-each>
	<xsl:for-each select="LexEntryRef_ComplexEntryTypes/Link/Alt">
	  <xsl:text>\ct</xsl:text>
	  <xsl:text>&#32;</xsl:text>
	  <xsl:value-of select="@abbr"/>
	  <xsl:if test="position() != last()">
			<xsl:text>;</xsl:text>
	  </xsl:if>
	  <xsl:text>&#13;&#10;</xsl:text>
	</xsl:for-each>
	<xsl:for-each select="LexEntryRef_PrimaryLexemes/Link/Alt|LexEntryRef_ComponentLexemes/Link/Alt">
	  <xsl:text>\mn</xsl:text>
	  <xsl:text>&#32;</xsl:text>
	  <xsl:value-of select="@entry|@sense"/>
	  <xsl:text>&#13;&#10;</xsl:text>
	</xsl:for-each>
	<xsl:for-each select="LexEntry_SummaryDefinition/AStr">
	  <xsl:call-template name="ProcessAstr">
		<xsl:with-param name="Tag" select="'su'"/>
	  </xsl:call-template>
	</xsl:for-each>
	<xsl:for-each select="LexEntryRef_PrimaryLexemes/Link/LexEntry_SummaryDefinition/AStr|
	LexEntryRef_ComponentLexemes/Link/LexEntry_SummaryDefinition/AStr">
	  <xsl:call-template name="ProcessAstr">
		<xsl:with-param name="Tag" select="'su'"/>
	  </xsl:call-template>
	</xsl:for-each>
  </xsl:for-each>
</xsl:template>


<!-- process summary definition for a minor entry -->

<xsl:template match="LexEntry_SummaryDefinition">
	<xsl:for-each select="AStr">
	  <xsl:call-template name="ProcessAstr">
		<xsl:with-param name="Tag" select="'su'"/>
	  </xsl:call-template>
	</xsl:for-each>
</xsl:template>

<!-- Process main entries for this complex form/variant (which is a main entry). -->

<xsl:template match="LexEntry_EntryRefs">
  <xsl:for-each select="LexEntryRef">
	<xsl:if test="LexEntryRef_ComplexEntryTypes/Link/Alt">
	  <xsl:text>\ct</xsl:text>
	  <xsl:for-each select="LexEntryRef_ComplexEntryTypes/Link/Alt">
		<xsl:text>&#32;</xsl:text>
		<xsl:value-of select="@abbr"/>
		<xsl:if test="position() != last()">
			  <xsl:text>;</xsl:text>
		</xsl:if>
	  </xsl:for-each>
	  <xsl:text>&#13;&#10;</xsl:text>
	</xsl:if>
	<xsl:if test="LexEntryRef_VariantEntryTypes/Link/Alt">
	  <xsl:text>\vt</xsl:text>
	  <xsl:for-each select="LexEntryRef_VariantEntryTypes/Link/Alt">
		<xsl:text>&#32;</xsl:text>
		<xsl:value-of select="@abbr"/>
		<xsl:if test="position() != last()">
			<xsl:text>;</xsl:text>
		</xsl:if>
	  </xsl:for-each>
	  <xsl:text>&#13;&#10;</xsl:text>
	</xsl:if>
	<xsl:if test="not(LexEntryRef_ComplexEntryTypes/Link/Alt) and not(LexEntryRef_VariantEntryTypes/Link/Alt)">
	  <xsl:text>\vet&#13;&#10;</xsl:text>
	</xsl:if>
	<xsl:for-each select="LexEntryRef_ComponentLexemes/Link">
	  <xsl:for-each select="Alt">
		<xsl:text>\mn</xsl:text>
		<xsl:text>&#32;</xsl:text>
		<xsl:value-of select="@entry|@sense"/>
		<xsl:text>&#13;&#10;</xsl:text>
	  </xsl:for-each>
	  <xsl:for-each select="LexEntry_SummaryDefinition/AStr">
		<xsl:call-template name="ProcessAstr">
		  <xsl:with-param name="Tag" select="'ms'"/>
		</xsl:call-template>
	  </xsl:for-each>
	</xsl:for-each>
	<xsl:for-each select="LexEntryRef_Summary/AStr">
	  <xsl:call-template name="ProcessAstr">
		<xsl:with-param name="Tag" select="'su'"/>
	  </xsl:call-template>
	</xsl:for-each>
  </xsl:for-each>
</xsl:template>


<!-- Handle an AStr element generically. -->

<xsl:template name="ProcessAstr">
  <xsl:param name="Tag"/>
  <xsl:text>\</xsl:text>
  <xsl:value-of select="$Tag"/>
  <xsl:text>_</xsl:text>
  <xsl:value-of select="@ws"/>
  <xsl:text>&#32;</xsl:text>
  <xsl:value-of select="Run"/>
  <xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Process a subentry paragraph -->

<xsl:template name="ProcessSubentry">
  <xsl:text>\se&#32;</xsl:text>
  <xsl:value-of select="LexEntry_HeadWord[1]/MoForm/MoForm_TypePrefix/AUni"/>
  <xsl:value-of select="LexEntry_HeadWord[1]/MoForm/MoForm_Form/AStr/Run"/>
  <xsl:value-of select="LexEntry_HeadWord[1]/MoForm/MoForm_TypePostfix/AUni"/>
  <xsl:text>&#13;&#10;</xsl:text>
  <xsl:if test="LexEntry_HomographNumber/Integer/@val">
	<xsl:text>\hm&#32;</xsl:text>
	<xsl:value-of select="LexEntry_HomographNumber/Integer/@val"/>
	<xsl:text>&#13;&#10;</xsl:text>
  </xsl:if>
  <xsl:apply-templates/>
</xsl:template>

<!-- Handle a custom field. -->

<xsl:template match="*[@userlabel]">
	<xsl:text>\</xsl:text>
	<xsl:value-of select="substring-after(name(),'_')"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:apply-templates/>
	<xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


</xsl:stylesheet>
