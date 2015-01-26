<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="text" omit-xml-declaration="yes" encoding="UTF-8"/>

<!-- This stylesheet works on the output of ReversalIndex.xsl to produce a "standard format"
	 file. -->

<!-- Strip all white space and leave it up to the stylesheet text elements below to put in
	 appropriate spacing. -->

<xsl:strip-space elements="*"/>
<xsl:preserve-space elements="Run"/><!-- but these spaces are significant! -->


<!--Find entry and indicate with an \re field marker-->
<xsl:template match="ReversalIndexEntry">
	<xsl:if test="ReversalIndexEntry_ReversalForm/AUni/@ws">
		<!-- If this is not the very first entry, add a blank line before it. -->
		<xsl:if test="position()>1 or count(ancestor::ReversalIndexEntry)>0">
			<xsl:text>&#10;</xsl:text>
		</xsl:if>
		<xsl:text>\re_</xsl:text>
		<xsl:value-of select="ReversalIndexEntry_ReversalForm/AUni/@ws"/>
		<xsl:text>&#32;</xsl:text>
		<xsl:value-of select="ReversalIndexEntry_ReversalForm/AUni"/>
		<xsl:text>&#10;</xsl:text>
		<xsl:if test="count(ancestor::ReversalIndexEntry)>0">
			<xsl:text>\lev </xsl:text>
			<xsl:value-of select="count(ancestor::ReversalIndexEntry)"/>
			<xsl:text>&#10;</xsl:text>
		</xsl:if>
		<xsl:apply-templates/>
		<xsl:text>\-re</xsl:text>
		<xsl:text>_</xsl:text>
		<xsl:value-of select="ReversalIndexEntry_ReversalForm/AUni/@ws"/>
		<xsl:text>&#10;</xsl:text>
	</xsl:if>
</xsl:template>

<xsl:template match="ReversalIndexEntry_ReversalForm">
  <!-- Already output, nothing to do. -->
</xsl:template>


<!-- Write the alternate forms for the entry. -->

<xsl:template match="ReversalIndexEntry_RelatedForm">
  <xsl:for-each select="AUni">
	<xsl:text>\re</xsl:text>
	<xsl:text>_</xsl:text>
	<xsl:value-of select="@ws"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:value-of select="."/>
	<xsl:text>&#10;</xsl:text>
  </xsl:for-each>
</xsl:template>


<!-- Write the part of speech for the entry. -->

<xsl:template match="PartOfSpeech">
  <xsl:for-each select="Link">
	<xsl:text>\ps_</xsl:text>
	<xsl:value-of select="*/@ws"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:choose>
	  <xsl:when test="Alt/@name">
		<xsl:value-of select="Alt/@name"/>
	  </xsl:when>
	  <xsl:when test="Alt/@abbr">
		<xsl:value-of select="Alt/@abbr"/>
	  </xsl:when>
	</xsl:choose>
	<xsl:text>&#10;</xsl:text>
  </xsl:for-each>
</xsl:template>


<!-- process LexSenseLink_ReversalName, LexEntry_LexemeForm (or LexEntry_CitationForm) to extract the headword. -->
<xsl:template match="LexSenseLink_ReversalName">
	<xsl:text>\hw&#32;</xsl:text>
	<xsl:value-of select="*/text()"/>
	<xsl:value-of select="*/RefHomographNumber"/>
	<xsl:if test="*/RefSenseNumber">
		<xsl:text>&#32;</xsl:text>
		<xsl:value-of select="*/RefSenseNumber"/>
	</xsl:if>
	<xsl:text>&#10;</xsl:text>
</xsl:template>

<xsl:template match="LexEntry_Headword">
  <xsl:text>\hw</xsl:text>
  <xsl:text>_</xsl:text>
  <xsl:value-of select="MorphForm/AUni[1]/@ws"/>
  <xsl:text>&#32;</xsl:text>
  <xsl:value-of select="MorphForm/AUni[1]"/>
  <xsl:text>&#10;</xsl:text>
</xsl:template>
<xsl:template match="LexEntry_CitationForm">
  <xsl:text>\hw</xsl:text>
  <xsl:text>_</xsl:text>
  <xsl:value-of select="AUni[1]/@ws"/>
  <xsl:text>&#32;</xsl:text>
  <xsl:value-of select="AUni[1]"/>
  <xsl:text>&#10;</xsl:text>
</xsl:template>

<!-- process LexSense_Gloss -->

<xsl:template match="LexSense_Gloss">
	<xsl:text>\gl</xsl:text>
	<xsl:text>_</xsl:text>
	<xsl:value-of select="AUni[1]/@ws"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:value-of select="AUni[1]"/>
	<xsl:text>&#10;</xsl:text>
</xsl:template>


<!-- process LexSense_Definition -->

<xsl:template match="LexSense_Definition">
	<xsl:text>\de</xsl:text>
	<xsl:text>_</xsl:text>
	<xsl:value-of select="AUni[1]/@ws"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:value-of select="AUni[1]"/>
	<xsl:text>&#10;</xsl:text>
</xsl:template>

<!-- process LexExampleSentence_Example -->
<xsl:template match="LexExampleSentence_Example">
	<xsl:text>\xv_</xsl:text>
	<xsl:value-of select="*/@ws"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:value-of select="*"/>
	<xsl:text>&#10;</xsl:text>
</xsl:template>

<!-- process CmTranslation_Translation -->
<xsl:template match="CmTranslation_Translation">
	<xsl:text>\x_</xsl:text>
	<xsl:value-of select="*/@ws"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:value-of select="*"/>
	<xsl:text>&#10;</xsl:text>
</xsl:template>

<!-- process LexSense_EncyclopedicInfo -->
<xsl:template match="LexSense_EncyclopedicInfo">
	<xsl:text>\e_</xsl:text>
	<xsl:value-of select="*/@ws"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:value-of select="*"/>
	<xsl:text>&#10;</xsl:text>
</xsl:template>

<!-- process LexSense_Restrictions -->
<xsl:template match="LexSense_Restrictions">
	<xsl:text>\rst_</xsl:text>
	<xsl:value-of select="*/@ws"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:value-of select="*"/>
	<xsl:text>&#10;</xsl:text>
</xsl:template>

<!-- process LexSense_AnthroNote -->
<xsl:template match="LexSense_AnthroNote">
	<xsl:text>\na_</xsl:text>
	<xsl:value-of select="*/@ws"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:value-of select="*"/>
	<xsl:text>&#10;</xsl:text>
</xsl:template>

<!-- process LexSense_GeneralNote -->
<xsl:template match="LexSense_GeneralNote">
	<xsl:text>\nt_</xsl:text>
	<xsl:value-of select="*/@ws"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:value-of select="*"/>
	<xsl:text>&#10;</xsl:text>
</xsl:template>

<!-- process LexSense_Bibliography -->
<xsl:template match="LexSense_Bibliography">
	<xsl:text>\nb_</xsl:text>
	<xsl:value-of select="*/@ws"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:value-of select="*"/>
	<xsl:text>&#10;</xsl:text>
</xsl:template>

<!-- process LexSense_DiscourseNote -->
<xsl:template match="LexSense_DiscourseNote">
	<xsl:text>\nd_</xsl:text>
	<xsl:value-of select="*/@ws"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:value-of select="*"/>
	<xsl:text>&#10;</xsl:text>
</xsl:template>

<!-- process LexSense_PhonologyNote -->
<xsl:template match="LexSense_PhonologyNote">
	<xsl:text>\np_</xsl:text>
	<xsl:value-of select="*/@ws"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:value-of select="*"/>
	<xsl:text>&#10;</xsl:text>
</xsl:template>

<!-- process LexSense_GrammarNote -->
<xsl:template match="LexSense_GrammarNote">
	<xsl:text>\ng_</xsl:text>
	<xsl:value-of select="*/@ws"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:value-of select="*"/>
	<xsl:text>&#10;</xsl:text>
</xsl:template>

<!-- process LexSense_SemanticsNote -->
<xsl:template match="LexSense_SemanticsNote">
	<xsl:text>\nm_</xsl:text>
	<xsl:value-of select="*/@ws"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:value-of select="*"/>
	<xsl:text>&#10;</xsl:text>
</xsl:template>

<!-- process LexSense_SocioLinguisticsNote -->
<xsl:template match="LexSense_SocioLinguisticsNote">
	<xsl:text>\ns_</xsl:text>
	<xsl:value-of select="*/@ws"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:value-of select="*"/>
	<xsl:text>&#10;</xsl:text>
</xsl:template>

<!-- process LexSense_ScientificName -->
<xsl:template match="LexSense_ScientificName">
	<xsl:text>\sc_</xsl:text>
	<xsl:value-of select="*/@ws"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:value-of select="*"/>
	<xsl:text>&#10;</xsl:text>
</xsl:template>

<!-- process LexSense_Source -->
<xsl:template match="LexSense_Source">
	<xsl:text>\s_</xsl:text>
	<xsl:value-of select="*/@ws"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:value-of select="*"/>
	<xsl:text>&#10;</xsl:text>
</xsl:template>

<!-- process LexSense_SemanticDomains -->
<xsl:template match="LexSense_SemanticDomains">
	<xsl:for-each select=".//Alt">
		<xsl:text>\is_</xsl:text>
		<xsl:value-of select="@ws"/>
		<xsl:text>&#32;</xsl:text>
		<xsl:value-of select="@abbr"/>
		<xsl:text>&#10;</xsl:text>
		<xsl:text>\sd_</xsl:text>
		<xsl:value-of select="@ws"/>
		<xsl:text>&#32;</xsl:text>
		<xsl:value-of select="@name"/>
		<xsl:text>&#10;</xsl:text>
	</xsl:for-each>
</xsl:template>

<!-- process LexSense_AnthroCodes -->
<xsl:template match="LexSense_AnthroCodes">
	<xsl:for-each select="CmAnthroItemLink">
		<xsl:text>\ia_</xsl:text>
		<xsl:value-of select="CmPossibility_Abbreviation//@ws"/>
		<xsl:text>&#32;</xsl:text>
		<xsl:value-of select="CmPossibility_Abbreviation//text()"/>
		<xsl:text>&#10;</xsl:text>
		<xsl:text>\ac_</xsl:text>
		<xsl:value-of select="CmPossibility_Name//@ws"/>
		<xsl:text>&#32;</xsl:text>
		<xsl:value-of select="CmPossibility_Name//text()"/>
		<xsl:text>&#10;</xsl:text>
	</xsl:for-each>
</xsl:template>

<!-- process LexSense_DomainTypes -->
<xsl:template match="LexSense_DomainTypes">
	<xsl:for-each select=".//Alt">
		<xsl:text>\it_</xsl:text>
		<xsl:value-of select="@ws"/>
		<xsl:text>&#32;</xsl:text>
		<xsl:value-of select="@abbr"/>
		<xsl:text>&#10;</xsl:text>
		<xsl:text>\td_</xsl:text>
		<xsl:value-of select="@ws"/>
		<xsl:text>&#32;</xsl:text>
		<xsl:value-of select="@name"/>
		<xsl:text>&#10;</xsl:text>
	</xsl:for-each>
</xsl:template>

<!-- process LexSense_UsageTypes -->
<xsl:template match="LexSense_UsageTypes">
	<xsl:for-each select=".//Alt">
		<xsl:text>\iu_</xsl:text>
		<xsl:value-of select="@ws"/>
		<xsl:text>&#32;</xsl:text>
		<xsl:value-of select="@abbr"/>
		<xsl:text>&#10;</xsl:text>
		<xsl:text>\ud_</xsl:text>
		<xsl:value-of select="@ws"/>
		<xsl:text>&#32;</xsl:text>
		<xsl:value-of select="@name"/>
		<xsl:text>&#10;</xsl:text>
	</xsl:for-each>
</xsl:template>

<!-- process LexSense_Status -->
<xsl:template match="LexSense_Status">
	<xsl:text>\st_</xsl:text>
	<xsl:value-of select=".//@ws"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:value-of select=".//@name"/>
	<xsl:text>&#10;</xsl:text>
</xsl:template>

<!-- process LexEntry_Bibliography -->
<xsl:template match="LexEntry_Bibliography">
	<xsl:text>\bi_</xsl:text>
	<xsl:value-of select="*/@ws"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:value-of select="*"/>
	<xsl:text>&#10;</xsl:text>
</xsl:template>

<!-- process LexEntry_LiteralMeaning -->
<xsl:template match="LexEntry_LiteralMeaning">
	<xsl:text>\lit_</xsl:text>
	<xsl:value-of select="*/@ws"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:value-of select="*"/>
	<xsl:text>&#10;</xsl:text>
</xsl:template>

<!-- This is the basic default processing: throw it away! -->

<xsl:template match="AUni|Uni|Run"/>

<xsl:template match="*">
  <xsl:apply-templates/>
</xsl:template>

</xsl:stylesheet>
