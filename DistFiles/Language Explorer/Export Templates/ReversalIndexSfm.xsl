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
  <!-- If this is not the very first entry, add a blank line before it. -->
  <xsl:if test="position()>1 or count(ancestor::ReversalIndexEntry)>0">
	<xsl:text>&#13;&#10;</xsl:text>
  </xsl:if>
  <xsl:text>\re</xsl:text>
  <xsl:text>_</xsl:text>
  <xsl:value-of select="ReversalIndexEntry_ReversalForm/AUni/@ws"/>
  <xsl:text>&#32;</xsl:text>
  <xsl:value-of select="ReversalIndexEntry_ReversalForm/AUni"/>
  <xsl:text>&#13;&#10;</xsl:text>
  <xsl:if test="count(ancestor::ReversalIndexEntry)>0">
	<xsl:text>\lev </xsl:text>
	<xsl:value-of select="count(ancestor::ReversalIndexEntry)"/>
	<xsl:text>&#13;&#10;</xsl:text>
  </xsl:if>
  <xsl:apply-templates/>
  <xsl:text>\-re</xsl:text>
  <xsl:text>_</xsl:text>
  <xsl:value-of select="ReversalIndexEntry_ReversalForm/AUni/@ws"/>
  <xsl:text>&#13;&#10;</xsl:text>
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
	<xsl:text>&#13;&#10;</xsl:text>
  </xsl:for-each>
</xsl:template>


<!-- Write the part of speech for the entry. -->

<xsl:template match="ReversalIndexEntry_PartOfSpeech">
  <xsl:for-each select="Link">
	<xsl:text>\ps&#32;</xsl:text>
	<xsl:choose>
	  <xsl:when test="Alt/@name">
		<xsl:value-of select="Alt/@name"/>
	  </xsl:when>
	  <xsl:when test="Alt/@abbr">
		<xsl:value-of select="Alt/@abbr"/>
	  </xsl:when>
	</xsl:choose>
	<xsl:text>&#13;&#10;</xsl:text>
  </xsl:for-each>
</xsl:template>


<!-- Write the referring senses for the entry. -->

<xsl:template match="LexSense">
  <xsl:choose>
	<xsl:when test="parent::ReversalIndexEntry_ReferringSenses or parent::LexSense_Senses">
	  <xsl:text>\sn&#32;</xsl:text>
	  <xsl:number level="multiple" format="1.1" count="LexSense"/>
	  <xsl:text>&#13;&#10;</xsl:text>
	</xsl:when>
	<xsl:otherwise>
	</xsl:otherwise>
  </xsl:choose>
  <xsl:apply-templates/>
</xsl:template>


<!-- process LexEntry_LexemeForm (or LexEntry_CitationForm) to extract the headword. -->

<xsl:template match="LexEntry_Headword">
  <xsl:text>\hw</xsl:text>
  <xsl:text>_</xsl:text>
  <xsl:value-of select="MorphForm/AUni[1]/@ws"/>
  <xsl:text>&#32;</xsl:text>
  <xsl:value-of select="MorphForm/AUni[1]"/>
  <xsl:text>&#13;&#10;</xsl:text>
</xsl:template>
<xsl:template match="LexEntry_CitationForm">
  <xsl:text>\hw</xsl:text>
  <xsl:text>_</xsl:text>
  <xsl:value-of select="AUni[1]/@ws"/>
  <xsl:text>&#32;</xsl:text>
  <xsl:value-of select="AUni[1]"/>
  <xsl:text>&#13;&#10;</xsl:text>
</xsl:template>

<!-- process LexSense_Gloss -->

<xsl:template match="LexSense_Gloss">
  <xsl:if test="not(preceding-sibling::LexSense_Gloss)">
	<xsl:text>\gl</xsl:text>
	<xsl:text>_</xsl:text>
	<xsl:value-of select="AUni[1]/@ws"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:value-of select="AUni[1]"/>
	<xsl:text>&#13;&#10;</xsl:text>
  </xsl:if>
</xsl:template>


<!-- process LexSense_Definition -->

<xsl:template match="LexSense_Definition">
  <xsl:if test="not(preceding-sibling::LexSense_Definition)">
	<xsl:text>\de</xsl:text>
	<xsl:text>_</xsl:text>
	<xsl:value-of select="AUni[1]/@ws"/>
	<xsl:text>&#32;</xsl:text>
	<xsl:value-of select="AUni[1]"/>
	<xsl:text>&#13;&#10;</xsl:text>
  </xsl:if>
</xsl:template>


<!-- This is the basic default processing: throw it away! -->

<xsl:template match="AUni|Uni|Run"/>

<xsl:template match="*">
  <xsl:apply-templates/>
</xsl:template>

</xsl:stylesheet>
