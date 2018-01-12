<?xml version="1.0"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl">
<xsl:output method="xml" version="1.0" encoding="utf-8" indent="yes" />
<!--
================================================================
DO NOT EDIT!!  This transform is automatically generated

Word Grammar Debugger
  This transform is applied repeatedly until a failure is found or the entire analysis succeeds

  Input:    XML output for a given sequence of morphemes
  Output: The tree result with failures embedded
			   (Note: each possible parse is within its own seq element)
================================================================

- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Preamble
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
<xsl:variable name="sSuccess">
<xsl:text>yes</xsl:text>
</xsl:variable>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Copy word  element
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
<xsl:template match="/word">
<xsl:copy>
<xsl:apply-templates />
</xsl:copy>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Copy form  element
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
<xsl:template match="/word/form">
<xsl:copy>
<xsl:apply-templates />
</xsl:copy>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Copy resultSoFar  element
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
<xsl:template match="/word/resultSoFar">
<xsl:copy-of select="." />
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
match a root
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
<xsl:template match="/word/seq/morph[@wordType='root']">
<xsl:call-template name="StemEqualsRoot" />
<xsl:call-template name="PartialEqualsRoot" />
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Partial = root production
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
<xsl:template name="PartialEqualsRoot">
<seq step="Try to build a Partial analysis node on a root where the category of the root is unknown (i.e. unmarked).">
<xsl:for-each select="preceding-sibling::*">
<xsl:copy-of select="." />
</xsl:for-each>
<partial>
<!--percolate these features-->
<xsl:attribute name="syncat">
<xsl:value-of select="stemMsa/@cat" />
</xsl:attribute>
<xsl:attribute name="syncatAbbr">
<xsl:value-of select="stemMsa/@catAbbr" />
</xsl:attribute>
<xsl:if test="stemMsa/@cat != '0'">
<failure>
<xsl:text>A root can only be a "Partial" when its category is unknown, but the category here is '</xsl:text>
<xsl:value-of select="stemMsa/@catAbbr" />
<xsl:text>'.</xsl:text>
</failure>
</xsl:if>
<xsl:copy-of select="." />
</partial>
<xsl:for-each select="following-sibling::*">
<xsl:copy-of select="." />
</xsl:for-each>
</seq>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Stem = root production
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
<xsl:template name="StemEqualsRoot">
<seq step="Try to build a Stem node on a root.">
<xsl:for-each select="preceding-sibling::*">
<xsl:copy-of select="." />
</xsl:for-each>
<stem>
<!--percolate these features-->
<xsl:attribute name="syncat">
<xsl:value-of select="stemMsa/@cat" />
</xsl:attribute>
<xsl:attribute name="syncatAbbr">
<xsl:value-of select="stemMsa/@catAbbr" />
</xsl:attribute>
<xsl:attribute name="inflClass">
<xsl:value-of select="stemMsa/@inflClass" />
</xsl:attribute>
<xsl:attribute name="inflClassAbbr">
<xsl:value-of select="stemMsa/@inflClassAbbr" />
</xsl:attribute>
<xsl:attribute name="requiresInfl">
<xsl:value-of select="stemMsa/@requiresInfl" />
</xsl:attribute>
<xsl:if test="stemMsa/@cat='0'">
<failure>A stem requires an overt category, but this root has an unmarked category.</failure>
</xsl:if>
<xsl:copy-of select="stemMsa/fs" />
<xsl:copy-of select="." />
<xsl:copy-of select="stemMsa/productivityRestriction" />
</stem>
<xsl:for-each select="following-sibling::*">
<xsl:copy-of select="." />
</xsl:for-each>
</seq>
</xsl:template>
<!--
	  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Other Stem productions
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template match="/word/seq[not(descendant::failure)]/stem">
<xsl:if test="preceding-sibling::morph[1][@wordType='derivPfx']">
<xsl:call-template name="DerivPfxStemProduction" />
</xsl:if>
<xsl:if test="preceding-sibling::morph[1][@wordType='derivCircumPfx'] and following-sibling::morph[1][@wordType='derivCircumSfx']">
<xsl:call-template name="DerivCircumPfxStemDerivCircumSfxProduction" />
</xsl:if>
<xsl:if test="following-sibling::morph[1][@wordType='derivSfx']">
<xsl:call-template name="StemDerivSfxProduction" />
</xsl:if>
<xsl:if test="following-sibling::stem[1]">
<xsl:call-template name="StemStemProductions" />
</xsl:if>
<xsl:if test="not(preceding-sibling::morph[@wordType='root']) and not(following-sibling::morph[@wordType='root'])">
<xsl:call-template name="InflectionalProductions" />
</xsl:if>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Partial productions
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Partial with circumfixes
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template match="/word/seq[not(descendant::failure)]/partial[preceding-sibling::morph[1][@wordType='circumPfx' or @wordType='derivCircumPfx'] and following-sibling::morph[1][@wordType='circumSfx' or @wordType='derivCircumSfx']]">
<seq step="Try to build a Partial node by surrounding another Partial node with an unclassified circumfix.">
<!--copy what's before the circumfix-prefix-->
<xsl:for-each select="preceding-sibling::*[position()&gt;1]">
<xsl:copy-of select="." />
</xsl:for-each>
<!--have a new partial-->
<partial>
<xsl:attribute name="syncat">
<xsl:value-of select="@syncat" />
</xsl:attribute>
<xsl:attribute name="syncatAbbr">
<xsl:value-of select="@syncatAbbr" />
</xsl:attribute>
<!-- copy circumfix-prefix-->
<xsl:copy-of select="preceding-sibling::morph[1]" />
<xsl:copy-of select="." />
<!-- copy circumfix-suffix-->
<xsl:copy-of select="following-sibling::morph[1]" />
</partial>
<!-- copy what's after the circumfix suffix -->
<xsl:for-each select="following-sibling::*[position()&gt;1]">
<xsl:copy-of select="." />
</xsl:for-each>
</seq>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Partial = ... circumPfx Full circumSfx ...
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CircumfixPartialOnFull">
<seq step="Try to build a Partial analysis node by surrounding a Full analysis node with an unclassified circumfix.">
<!-- constrain the circumfix -->
<xsl:variable name="sSynCat" select="@syncat" />
<xsl:variable name="sSynCatAbbr" select="@syncatAbbr" />
<xsl:for-each select="preceding-sibling::*[1]/morph/unclassMsa">
<!--apply constraints-->
<xsl:variable name="sCategoriesAreCompatible">
<xsl:call-template name="TestCompatibleCategories">
<xsl:with-param name="sFirstCat">
<xsl:value-of select="@fromCat" />
</xsl:with-param>
<xsl:with-param name="sSecondCat">
<xsl:value-of select="$sSynCat" />
</xsl:with-param>
</xsl:call-template>
</xsl:variable>
<xsl:variable name="sType" select="../@wordType" />
<xsl:if test="$sCategoriesAreCompatible != $sSuccess">
<xsl:variable name="sPreface">
<xsl:text>In attaching an unclassified circumfix: </xsl:text>
</xsl:variable>
<xsl:call-template name="ReportFailure">
<xsl:with-param name="sPreface">
<xsl:value-of select="$sPreface" />
</xsl:with-param>
<xsl:with-param name="sFirstItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemComponentAbbr" select="@fromCatAbbr" />
<xsl:with-param name="sFirstItemDescription">
<xsl:text>unclassified </xsl:text>
<xsl:value-of select="$sType" />
</xsl:with-param>
<xsl:with-param name="sFirstItemValue" select="../shortName" />
<xsl:with-param name="sSecondItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sSecondItemComponentAbbr" select="$sSynCatAbbr" />
<xsl:with-param name="sSecondItemDescription">
<xsl:text>stem</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:if>
</xsl:for-each>
<!--copy what's before the circumfix-prefix-->
<xsl:for-each select="preceding-sibling::*[position()&gt;1]">
<xsl:copy-of select="." />
</xsl:for-each>
<!--have a partial-->
<partial>
<xsl:attribute name="inflected">
<xsl:value-of select="@inflected" />
</xsl:attribute>
<xsl:attribute name="syncat">
<xsl:value-of select="@syncat" />
</xsl:attribute>
<xsl:attribute name="syncatAbbr">
<xsl:value-of select="@syncatAbbr" />
</xsl:attribute>
<!-- copy circumfix-prefix-->
<xsl:copy-of select="preceding-sibling::morph[1]" />
<!--copy the full-->
<xsl:copy-of select="." />
<!-- copy circumfix-suffix-->
<xsl:copy-of select="following-sibling::morph[1]" />
</partial>
<!-- copy what's after the circumfix suffix -->
<xsl:for-each select="following-sibling::*[position()&gt;1]">
<xsl:copy-of select="." />
</xsl:for-each>
</seq>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Partial with Generic affixes
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="GenericAffixPartial">
<seq step="Try to build a Partial analysis node on a Full analysis node with sequences of affixes not labeled as either derivational or inflectional (the sequence may also include derivational affixes).">
<xsl:variable name="GenericPrefixMorphs">
<xsl:call-template name="CountGenericPrefixes">
<xsl:with-param name="morph" select="preceding-sibling::*[1]" />
</xsl:call-template>
</xsl:variable>
<xsl:variable name="iGenericPrefixMorphs">
<xsl:value-of select="string-length($GenericPrefixMorphs)" />
</xsl:variable>
<xsl:variable name="GenericSuffixMorphs">
<xsl:call-template name="CountGenericSuffixes">
<xsl:with-param name="morph" select="following-sibling::*[1]" />
</xsl:call-template>
</xsl:variable>
<xsl:variable name="iGenericSuffixMorphs">
<xsl:value-of select="string-length($GenericSuffixMorphs)" />
</xsl:variable>
<!-- constrain any prefixes and any suffixes -->
<xsl:variable name="sSynCat" select="@syncat" />
<xsl:variable name="sSynCatAbbr" select="@syncatAbbr" />
<xsl:for-each select="../morph/unclassMsa ">
<!--apply constraints-->
<xsl:variable name="sCategoriesAreCompatible">
<xsl:call-template name="TestCompatibleCategories">
<xsl:with-param name="sFirstCat">
<xsl:value-of select="@fromCat" />
</xsl:with-param>
<xsl:with-param name="sSecondCat">
<xsl:value-of select="$sSynCat" />
</xsl:with-param>
</xsl:call-template>
</xsl:variable>
<xsl:variable name="sType" select="../@wordType" />
<xsl:if test="$sCategoriesAreCompatible != $sSuccess">
<xsl:variable name="sPreface">
<xsl:text>In attaching an unclassified </xsl:text>
<xsl:value-of select="$sType" />: </xsl:variable>
<xsl:call-template name="ReportFailure">
<xsl:with-param name="sPreface">
<xsl:value-of select="$sPreface" />
</xsl:with-param>
<xsl:with-param name="sFirstItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemComponentAbbr" select="@fromCatAbbr" />
<xsl:with-param name="sFirstItemDescription">
<xsl:text>unclassified </xsl:text>
<xsl:value-of select="$sType" />
</xsl:with-param>
<xsl:with-param name="sFirstItemValue" select="../shortName" />
<xsl:with-param name="sSecondItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sSecondItemComponentAbbr" select="$sSynCatAbbr" />
<xsl:with-param name="sSecondItemDescription">
<xsl:text>stem</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:if>
</xsl:for-each>
<!-- copy what's before the partial -->
<xsl:for-each select="preceding-sibling::*[position()&gt;$iGenericPrefixMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
<!--have a partial-->
<partial>
<xsl:attribute name="inflected">
<xsl:value-of select="@inflected" />
</xsl:attribute>
<xsl:attribute name="syncat">
<xsl:value-of select="@syncat" />
</xsl:attribute>
<xsl:attribute name="syncatAbbr">
<xsl:value-of select="@syncatAbbr" />
</xsl:attribute>
<!--copy any generic prefixes-->
<xsl:if test="$iGenericPrefixMorphs&gt; 0">
<xsl:for-each select="preceding-sibling::*[position()&lt;=$iGenericPrefixMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</xsl:if>
<!--copy the full-->
<xsl:copy-of select="." />
<!--copy any generic suffixes-->
<xsl:if test="$iGenericSuffixMorphs &gt; 0">
<xsl:for-each select="following-sibling::*[position()&lt;=$iGenericSuffixMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</xsl:if>
</partial>
<!-- copy what's after the partial -->
<xsl:for-each select="following-sibling::*[position()&gt;$iGenericSuffixMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</seq>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CountCompoundedRoots
	Params: morph - morpheme to check
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CountCompoundedRoots">
<xsl:param name="morph" />
<xsl:if test="$morph/@wordType = 'root' or $morph/@wordType = 'interfix'">
<xsl:call-template name="IndicateInflAffixSucceeded" />
<xsl:call-template name="CountCompoundedRoots">
<xsl:with-param name="morph" select="$morph/following-sibling::*[1]" />
</xsl:call-template>
</xsl:if>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CountGenericPrefixes
	Params: morph - morpheme to check
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CountGenericPrefixes">
<xsl:param name="morph" />
<xsl:if test="$morph/@wordType = 'prefix' or $morph/@wordType = 'derivPfx'">
<xsl:call-template name="IndicateInflAffixSucceeded" />
<xsl:call-template name="CountGenericPrefixes">
<xsl:with-param name="morph" select="$morph/preceding-sibling::*[1]" />
</xsl:call-template>
</xsl:if>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CountGenericSuffixes
	Params: morph - morpheme to check
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CountGenericSuffixes">
<xsl:param name="morph" />
<xsl:if test="$morph/@wordType = 'suffix' or $morph/@wordType = 'derivSfx'">
<xsl:call-template name="IndicateInflAffixSucceeded" />
<xsl:call-template name="CountGenericSuffixes">
<xsl:with-param name="morph" select="$morph/following-sibling::*[1]" />
</xsl:call-template>
</xsl:if>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Word = Full/Partial productions
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
<xsl:template match="/word/seq[not(descendant::failure)]/full[not(preceding-sibling::stem) and not(following-sibling::stem)] | /word/seq[not(descendant::failure)]/partial[not(preceding-sibling::stem) and not(following-sibling::stem)]">
<seq>
<xsl:attribute name="step">
<xsl:text>Try to build a Word analysis node on a </xsl:text>
<xsl:choose>
<xsl:when test="name()='partial'">
<xsl:text>Partial</xsl:text>
</xsl:when>
<xsl:otherwise>
<xsl:text>Full</xsl:text>
</xsl:otherwise>
</xsl:choose>
<xsl:text> analysis node.</xsl:text>
</xsl:attribute>
<xsl:for-each select="preceding-sibling::*">
<xsl:copy-of select="." />
</xsl:for-each>
<word>
<!--percolate these features-->
<xsl:attribute name="syncat">
<xsl:value-of select="@syncat" />
</xsl:attribute>
<xsl:attribute name="syncatAbbr">
<xsl:value-of select="@syncatAbbr" />
</xsl:attribute>
<xsl:if test="@requiresInfl='+' and @inflected='-'">
<failure>
<xsl:text>The category '</xsl:text>
<xsl:value-of select="@syncatAbbr" />
<xsl:text>' requires inflection, but there was no inflection.</xsl:text>
</failure>
</xsl:if>
<xsl:copy-of select="." />
</word>
<xsl:for-each select="following-sibling::*">
<xsl:copy-of select="." />
</xsl:for-each>
</seq>
<xsl:choose>
<xsl:when test="name()!='partial' and preceding-sibling::*[1][@wordType='circumPfx'] and following-sibling::*[1][@wordType='circumSfx']">
<xsl:call-template name="CircumfixPartialOnFull" />
</xsl:when>
<xsl:when test="name()!='partial' and preceding-sibling::*[1][@wordType='prefix'] or name()!='partial' and following-sibling::*[1][@wordType='suffix']">
<xsl:call-template name="GenericAffixPartial" />
</xsl:when>
<xsl:when test="name()='partial' and preceding-sibling::*[1][@wordType='prefix' or @wordType='derivPfx'] or name()='partial' and following-sibling::*[1][@wordType='suffix' or @wordType='derivSfx']">
<xsl:call-template name="GenericAffixPartial" />
</xsl:when>
<xsl:otherwise>
<xsl:if test="name()='partial'">
<!--    invoke inflectional templates -->
<xsl:call-template name="PartialInflectionalTemplate102POS81" />
<xsl:if test="@syncat!='0'">
<xsl:call-template name="PartialInflectionalTemplate102POS82" />
<xsl:call-template name="PartialInflectionalTemplate102POS85" />
<xsl:call-template name="PartialInflectionalTemplate102POS88" />
<xsl:call-template name="PartialInflectionalTemplate102POS91" />
<xsl:call-template name="PartialInflectionalTemplate102POS94" />
</xsl:if>
<xsl:call-template name="PartialInflectionalTemplate156POS153" />
<xsl:call-template name="PartialInflectionalTemplate166POS163" />
<xsl:call-template name="PartialInflectionalTemplate171POS168" />
<xsl:call-template name="PartialInflectionalTemplate175POS172" />
</xsl:if>
</xsl:otherwise>
</xsl:choose>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OrthographicWord = Word productions
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
<xsl:template match="/word/seq[not(descendant::failure)]/word">
<xsl:variable name="word" select="." />
<seq step="Try to build an Orthographic Word node on a Word, including any clitics.">
<xsl:variable name="ProcliticMorphs">
<xsl:call-template name="CountProclitics">
<xsl:with-param name="morph" select="preceding-sibling::*[1]" />
</xsl:call-template>
</xsl:variable>
<xsl:variable name="iProcliticMorphs">
<xsl:value-of select="string-length($ProcliticMorphs)" />
</xsl:variable>
<xsl:variable name="EncliticMorphs">
<xsl:call-template name="CountEnclitics">
<xsl:with-param name="morph" select="following-sibling::*[1]" />
</xsl:call-template>
</xsl:variable>
<xsl:variable name="iEncliticMorphs">
<xsl:value-of select="string-length($EncliticMorphs)" />
</xsl:variable>
<!-- copy what's before the orthographic word -->
<xsl:for-each select="preceding-sibling::*[position()&gt;$iProcliticMorphs]">
<xsl:copy-of select="." />
<failure>Only proclitics can be before a Word analysis node.</failure>
</xsl:for-each>
<orthographicWord>
<!--copy any proclitics-->
<xsl:if test="$iProcliticMorphs&gt; 0">
<xsl:for-each select="preceding-sibling::*[position()&lt;=$iProcliticMorphs]">
<xsl:copy-of select="." />
<!--apply constraints-->
<xsl:variable name="sCategoriesAreCompatible">
<xsl:call-template name="TestCliticFromCategories">
<xsl:with-param name="word" select="$word" />
</xsl:call-template>
</xsl:variable>
<xsl:if test="$sCategoriesAreCompatible != $sSuccess">
<xsl:call-template name="ReportCliticFailure">
<xsl:with-param name="sCliticType" select="'proclitic'" />
<xsl:with-param name="word" select="$word" />
</xsl:call-template>
</xsl:if>
</xsl:for-each>
</xsl:if>
<xsl:copy-of select="." />
<!--copy any enclitics-->
<xsl:if test="$iEncliticMorphs &gt; 0">
<xsl:for-each select="following-sibling::*[position()&lt;=$iEncliticMorphs]">
<xsl:copy-of select="." />
<!--apply constraints-->
<xsl:variable name="sCategoriesAreCompatible">
<xsl:call-template name="TestCliticFromCategories">
<xsl:with-param name="word" select="$word" />
</xsl:call-template>
</xsl:variable>
<xsl:if test="$sCategoriesAreCompatible != $sSuccess">
<xsl:call-template name="ReportCliticFailure">
<xsl:with-param name="sCliticType" select="'enclitic'" />
<xsl:with-param name="word" select="$word" />
</xsl:call-template>
</xsl:if>
</xsl:for-each>
</xsl:if>
</orthographicWord>
<!-- copy what's after the orthographic word -->
<xsl:for-each select="following-sibling::*[position()&gt;$iEncliticMorphs]">
<xsl:copy-of select="." />
<failure>Only enclitics can be after a Word analysis node.</failure>
</xsl:for-each>
</seq>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CountProclitics
	Params: morph - morpheme to check
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CountProclitics">
<xsl:param name="morph" />
<xsl:if test="$morph/@wordType = 'proclitic' or $morph/@wordType = 'clitic'">
<xsl:call-template name="IndicateInflAffixSucceeded" />
<xsl:call-template name="CountProclitics">
<xsl:with-param name="morph" select="$morph/preceding-sibling::*[1]" />
</xsl:call-template>
</xsl:if>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CountEnclitics
	Params: morph - morpheme to check
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CountEnclitics">
<xsl:param name="morph" />
<xsl:if test="$morph/@wordType = 'enclitic' or $morph/@wordType = 'clitic'">
<xsl:call-template name="IndicateInflAffixSucceeded" />
<xsl:call-template name="CountEnclitics">
<xsl:with-param name="morph" select="$morph/following-sibling::*[1]" />
</xsl:call-template>
</xsl:if>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ReportCliticFailure
	Params: sCliticType - type of clitic (proclitic, enclitic)
				   word - word node
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="ReportCliticFailure">
<xsl:param name="sCliticType" select="'proclitic'" />
<xsl:param name="word" />
<failure>
<xsl:text>In attaching a </xsl:text>
<xsl:value-of select="$sCliticType" />
<xsl:text>: The category (</xsl:text>
<xsl:value-of select="$word/@syncatAbbr" />
<xsl:text>) of the word is incompatible with any of the categories that the proclitic "</xsl:text>
<xsl:value-of select="shortName" />
<xsl:text>" must attach to (</xsl:text>
<xsl:for-each select="stemMsa/fromPartsOfSpeech">
<xsl:value-of select="@fromCatAbbr" />
<xsl:if test="position() != last()">
<xsl:text>, </xsl:text>
</xsl:if>
</xsl:for-each>
<xsl:text>).</xsl:text>
</failure>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
TestCliticFromCategories
	Params: word - word node
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="TestCliticFromCategories">
<xsl:param name="word" />
<xsl:choose>
<xsl:when test="count(stemMsa/fromPartsOfSpeech) &gt; 0">
<xsl:for-each select="stemMsa/fromPartsOfSpeech">
<xsl:call-template name="TestCompatibleCategories">
<xsl:with-param name="sFirstCat">
<xsl:value-of select="@fromCat" />
</xsl:with-param>
<xsl:with-param name="sSecondCat">
<xsl:value-of select="$word/@syncat" />
</xsl:with-param>
</xsl:call-template>
</xsl:for-each>
</xsl:when>
<xsl:otherwise>
<xsl:value-of select="$sSuccess" />
</xsl:otherwise>
</xsl:choose>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Ignore text template
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
<xsl:template match="alloform | gloss | citationForm | shortName | props | stemName | failure | inflClass | name | value" />
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ApplyDerivationalConstraints
	Params: derivMorph - derivational morpheme morph element
			sType - 'prefix' or 'suffix' or 'circumfix'
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
<xsl:template name="ApplyDerivationalConstraints">
<xsl:param name="derivMorph" />
<xsl:param name="sType">suffix</xsl:param>
<!--apply constraints-->
<xsl:variable name="sCategoriesAreCompatible">
<xsl:call-template name="TestCompatibleCategories">
<xsl:with-param name="sFirstCat">
<xsl:value-of select="$derivMorph/derivMsa/@fromCat" />
</xsl:with-param>
<xsl:with-param name="sSecondCat">
<xsl:value-of select="@syncat" />
</xsl:with-param>
</xsl:call-template>
</xsl:variable>
<xsl:variable name="sEnvCategoriesAreCompatible">
<xsl:call-template name="TestCompatibleCategories">
<xsl:with-param name="sFirstCat">
<xsl:value-of select="$derivMorph/envCat/@value" />
</xsl:with-param>
<xsl:with-param name="sSecondCat">
<xsl:value-of select="@syncat" />
</xsl:with-param>
</xsl:call-template>
</xsl:variable>
<xsl:variable name="sFromInflectionClassIsGood">
<xsl:if test="$derivMorph/derivMsa/@fromInflClass = '0' or @inflClass = $derivMorph/derivMsa/@fromInflClass">
<xsl:value-of select="$sSuccess" />
</xsl:if>
</xsl:variable>
<xsl:variable name="stem" select="." />
<xsl:variable name="sFromProductivityRestrictionsAreGood">
<xsl:choose>
<xsl:when test="$derivMorph/derivMsa/fromProductivityRestriction">
<xsl:variable name="sBadProductivityRestrictions">
<xsl:for-each select="$derivMorph/derivMsa/fromProductivityRestriction">
<xsl:variable name="sThisId" select="@id" />
<xsl:if test="not($stem/productivityRestriction[@id=$sThisId])">
<xsl:value-of select="$sThisId" />
<xsl:text>, </xsl:text>
</xsl:if>
</xsl:for-each>
</xsl:variable>
<xsl:choose>
<xsl:when test="string-length($sBadProductivityRestrictions) &gt; 0">
<xsl:value-of select="$sBadProductivityRestrictions" />
</xsl:when>
<xsl:otherwise>
<xsl:value-of select="$sSuccess" />
</xsl:otherwise>
</xsl:choose>
</xsl:when>
<xsl:otherwise>
<xsl:value-of select="$sSuccess" />
</xsl:otherwise>
</xsl:choose>
</xsl:variable>
<xsl:choose>
<xsl:when test="$sCategoriesAreCompatible = $sSuccess and $sFromInflectionClassIsGood = $sSuccess and $sEnvCategoriesAreCompatible = $sSuccess and $sFromProductivityRestrictionsAreGood = $sSuccess">
<!-- percolate these features -->
<xsl:attribute name="syncat">
<xsl:choose>
<xsl:when test="$derivMorph/derivMsa/@toCat=$derivMorph/derivMsa/@fromCat">
<xsl:value-of select="@syncat" />
</xsl:when>
<xsl:otherwise>
<xsl:value-of select="$derivMorph/derivMsa/@toCat" />
</xsl:otherwise>
</xsl:choose>
</xsl:attribute>
<xsl:attribute name="syncatAbbr">
<xsl:value-of select="$derivMorph/derivMsa/@toCatAbbr" />
</xsl:attribute>
<xsl:attribute name="inflClass">
<xsl:choose>
<xsl:when test="$derivMorph/derivMsa/@toInflClass != '0'">
<xsl:value-of select="$derivMorph/derivMsa/@toInflClass" />
</xsl:when>
<xsl:otherwise>
<xsl:value-of select="@inflClass" />
</xsl:otherwise>
</xsl:choose>
</xsl:attribute>
<xsl:attribute name="inflClassAbbr">
<xsl:choose>
<xsl:when test="$derivMorph/derivMsa/@toInflClass != '0'">
<xsl:value-of select="$derivMorph/derivMsa/@toInflClassAbbr" />
</xsl:when>
<xsl:otherwise>
<xsl:value-of select="@inflClassAbbr" />
</xsl:otherwise>
</xsl:choose>
</xsl:attribute>
<xsl:attribute name="requiresInfl">
<xsl:value-of select="$derivMorph/derivMsa/@requiresInfl" />
</xsl:attribute>
<xsl:attribute name="blocksInfl">
<xsl:text>-</xsl:text>
</xsl:attribute>
<!--Unify from features with stem-->
<xsl:variable name="UnificationOfStemAndDerivFrom">
<xsl:call-template name="UnifyTwoFeatureStructures">
<xsl:with-param name="FirstFS" select="fs" />
<xsl:with-param name="SecondFS" select="$derivMorph/derivMsa/fromFS" />
<xsl:with-param name="sTopLevelId" select="fs/@id" />
<xsl:with-param name="sRuleInfo">
<xsl:text>Attaching the derivational </xsl:text>
<xsl:value-of select="$sType" />
<xsl:text> (</xsl:text>
<xsl:value-of select="$derivMorph/shortName" />
<xsl:text>)</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstDescription" select="'stem'" />
<xsl:with-param name="sSecondDescription">
<xsl:text>derivational </xsl:text>
<xsl:value-of select="$sType" />
<xsl:text> (</xsl:text>
<xsl:value-of select="$derivMorph/shortName" />
<xsl:text>)</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:variable>
<xsl:choose>
<xsl:when test="msxsl:node-set($UnificationOfStemAndDerivFrom)/descendant::failure">
<xsl:copy-of select="$UnificationOfStemAndDerivFrom" />
</xsl:when>
<xsl:otherwise>
<!--Override those unified features with deriv to features-->
<xsl:variable name="PriorityUnion">
<xsl:call-template name="OverrideFirstFsWithSecondFs">
<xsl:with-param name="FirstFS" select="msxsl:node-set($UnificationOfStemAndDerivFrom)/fs" />
<xsl:with-param name="SecondFS" select="$derivMorph/derivMsa/toFS" />
</xsl:call-template>
</xsl:variable>
<xsl:if test="msxsl:node-set($PriorityUnion)/descendant::feature">
<xsl:copy-of select="$PriorityUnion" />
</xsl:if>
</xsl:otherwise>
</xsl:choose>
<xsl:choose>
<xsl:when test="$derivMorph/derivMsa/toProductivityRestriction">
<xsl:copy-of select="$derivMorph/derivMsa/toProductivityRestriction" />
</xsl:when>
<xsl:otherwise>
<xsl:copy-of select="stemMsa/productivityRestriction" />
</xsl:otherwise>
</xsl:choose>
<xsl:choose>
<xsl:when test="stemName">
<xsl:copy-of select="stemName" />
</xsl:when>
<xsl:when test="morph/stemName">
<xsl:copy-of select="morph/stemName" />
</xsl:when>
</xsl:choose>
</xsl:when>
<xsl:otherwise>
<xsl:variable name="sPreface">
<xsl:text>In attaching a derivational </xsl:text>
<xsl:value-of select="$sType" />: </xsl:variable>
<xsl:if test="$sCategoriesAreCompatible != $sSuccess">
<xsl:call-template name="ReportFailure">
<xsl:with-param name="sPreface">
<xsl:value-of select="$sPreface" />
</xsl:with-param>
<xsl:with-param name="sFirstItemComponent">
<xsl:text>from category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemComponentAbbr" select="$derivMorph/derivMsa/@fromCatAbbr" />
<xsl:with-param name="sFirstItemDescription">
<xsl:text>derivational </xsl:text>
<xsl:value-of select="$sType" />
</xsl:with-param>
<xsl:with-param name="sFirstItemValue" select="$derivMorph/shortName" />
<xsl:with-param name="sSecondItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sSecondItemComponentAbbr" select="@syncatAbbr" />
<xsl:with-param name="sSecondItemDescription">
<xsl:text>stem</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:if>
<xsl:if test="$sFromInflectionClassIsGood != $sSuccess">
<xsl:call-template name="ReportFailure">
<xsl:with-param name="sPreface">
<xsl:value-of select="$sPreface" />
</xsl:with-param>
<xsl:with-param name="sFirstItemComponent">
<xsl:text>from inflection class</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemComponentAbbr" select="$derivMorph/derivMsa/@fromInflClassAbbr" />
<xsl:with-param name="sFirstItemDescription">
<xsl:text>derivational </xsl:text>
<xsl:value-of select="$sType" />
</xsl:with-param>
<xsl:with-param name="sFirstItemValue" select="$derivMorph/shortName" />
<xsl:with-param name="sSecondItemComponent">
<xsl:text>inflection class</xsl:text>
</xsl:with-param>
<xsl:with-param name="sSecondItemComponentAbbr" select="@inflClassAbbr" />
<xsl:with-param name="sSecondItemDescription">
<xsl:text>stem</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:if>
<xsl:if test="$sEnvCategoriesAreCompatible != $sSuccess">
<xsl:call-template name="ReportFailure">
<xsl:with-param name="sPreface">
<xsl:value-of select="$sPreface" />
</xsl:with-param>
<xsl:with-param name="sFirstItemComponent">
<xsl:text>environment category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemComponentAbbr" select="$derivMorph/envCat/@value" />
<xsl:with-param name="sFirstItemDescription">
<xsl:text>derivational </xsl:text>
<xsl:value-of select="$sType" />
</xsl:with-param>
<xsl:with-param name="sFirstItemValue" select="$derivMorph/shortName" />
<xsl:with-param name="sSecondItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sSecondItemComponentAbbr" select="@syncatAbbr" />
<xsl:with-param name="sSecondItemDescription">
<xsl:text>stem</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:if>
<xsl:if test="$sFromProductivityRestrictionsAreGood  != $sSuccess">
<xsl:variable name="stem2" select="." />
<xsl:for-each select="$derivMorph/derivMsa/fromProductivityRestriction">
<xsl:variable name="sThisId" select="@id" />
<xsl:if test="not($stem2/productivityRestriction[@id=$sThisId])">
<xsl:call-template name="ReportFailure">
<xsl:with-param name="sPreface" select="$sPreface" />
<xsl:with-param name="sFirstItemComponent">
<xsl:text>from exception feature</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemComponentAbbr">
<xsl:value-of select="name" />
</xsl:with-param>
<xsl:with-param name="sFirstItemDescription">derivational <xsl:value-of select="$sType" />
</xsl:with-param>
<xsl:with-param name="sFirstItemValue" select="$derivMorph/shortName" />
<xsl:with-param name="sSecondItemComponent">
<xsl:text>exception features</xsl:text>
</xsl:with-param>
<xsl:with-param name="sSecondItemComponentAbbr" />
<xsl:with-param name="sSecondItemDescription"> stem</xsl:with-param>
</xsl:call-template>
</xsl:if>
</xsl:for-each>
</xsl:if>
</xsl:otherwise>
</xsl:choose>
<xsl:if test="$sType='prefix' or $sType='circumfix'">
<!-- copy derivational prefix-->
<xsl:copy-of select="preceding-sibling::morph[1]" />
</xsl:if>
<!-- copy right-hand side stem -->
<xsl:copy-of select="." />
<xsl:if test="$sType='suffix' or $sType='circumfix'">
<!-- copy derivational suffix -->
<xsl:copy-of select="following-sibling::morph[1]" />
</xsl:if>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DerivCircumPfxStemDerivCircumSfxProduction
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="DerivCircumPfxStemDerivCircumSfxProduction">
<seq step="Try to build a Stem node by prepending a derivational circumfix-prefix and, at the same time, appending a derivational circumfix-suffix to another Stem node.">
<!--copy what's before the derivational circumfix-prefix-->
<xsl:for-each select="preceding-sibling::*[position()&gt;1]">
<xsl:copy-of select="." />
</xsl:for-each>
<!--have a new stem-->
<stem>
<xsl:call-template name="ApplyDerivationalConstraints">
<xsl:with-param name="derivMorph" select="preceding-sibling::morph[1]" />
<xsl:with-param name="sType">
<xsl:text>circumfix</xsl:text>
</xsl:with-param>
</xsl:call-template>
</stem>
<!-- copy what's after the derivational circumfix-suffix -->
<xsl:for-each select="following-sibling::*[position()&gt;1]">
<xsl:copy-of select="." />
</xsl:for-each>
</seq>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DerivPfxStemProduction
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="DerivPfxStemProduction">
<seq step="Try to build a Stem node by prepending a derivational prefix to another Stem node.">
<!--copy what's before the derivational prefix-->
<xsl:for-each select="preceding-sibling::*[position()&gt;1]">
<xsl:copy-of select="." />
</xsl:for-each>
<!--have a new stem-->
<stem>
<xsl:call-template name="ApplyDerivationalConstraints">
<xsl:with-param name="derivMorph" select="preceding-sibling::morph[1]" />
<xsl:with-param name="sType">
<xsl:text>prefix</xsl:text>
</xsl:with-param>
</xsl:call-template>
</stem>
<!-- copy what's after the stem -->
<xsl:for-each select="following-sibling::*">
<xsl:copy-of select="." />
</xsl:for-each>
</seq>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
IndicateInflAffixSucceeded
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="IndicateInflAffixSucceeded">
<!--Output special code indicating affix succeeded.-->
<xsl:text>x</xsl:text>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
InflectionalProductions
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="InflectionalProductions">
<xsl:if test="not(preceding-sibling::stem) and not(following-sibling::stem)">
<xsl:call-template name="UninflectedStemProduction" />
</xsl:if>
<xsl:if test="not(preceding-sibling::stem) and not(following-sibling::stem)">
<xsl:call-template name="InflectionalTemplate102" />
</xsl:if>
<xsl:if test="not(preceding-sibling::stem) and not(following-sibling::stem)">
<xsl:call-template name="InflectionalTemplate156" />
</xsl:if>
<xsl:if test="not(preceding-sibling::stem) and not(following-sibling::stem)">
<xsl:call-template name="InflectionalTemplate166" />
</xsl:if>
<xsl:if test="not(preceding-sibling::stem) and not(following-sibling::stem)">
<xsl:call-template name="InflectionalTemplate171" />
</xsl:if>
<xsl:if test="not(preceding-sibling::stem) and not(following-sibling::stem)">
<xsl:call-template name="InflectionalTemplate175" />
</xsl:if>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ReportFailure
	Params: sPreface - descriptive blurb about where the failure occurred (compound rule, derivational affixes, etc.)
			sFirstItemComponent - component of the first item that is failing
			sFirstItemComponentAbbr - descriptive abbreviation/value of the component
			sFirstItemDescription - description of the first item
			sFirstItemValue - short name or other description of the first item
			sSecondItemComponent - component of the second item
				 sSecondItemComponentAbbr - descriptive abbreviation/value of the component
			sSecondItemDescription - description of the second item
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
<xsl:template name="ReportFailure">
<xsl:param name="sPreface" />
<xsl:param name="sFirstItemComponent" />
<xsl:param name="sFirstItemComponentAbbr" />
<xsl:param name="sFirstItemDescription" />
<xsl:param name="sFirstItemValue" />
<xsl:param name="sSecondItemComponent" />
<xsl:param name="sSecondItemComponentAbbr" />
<xsl:param name="sSecondItemDescription" />
<xsl:element name="failure">
<xsl:value-of select="$sPreface" />
<xsl:text>The </xsl:text>
<xsl:value-of select="$sFirstItemComponent" />
<xsl:text> (</xsl:text>
<xsl:value-of select="$sFirstItemComponentAbbr" />
<xsl:text>) of the </xsl:text>
<xsl:value-of select="$sFirstItemDescription" />
<xsl:text> "</xsl:text>
<xsl:value-of select="$sFirstItemValue" />
<xsl:text>" is incompatible with the </xsl:text>
<xsl:value-of select="$sSecondItemComponent" />
<xsl:if test="string-length($sSecondItemComponentAbbr) &gt; 0">
<xsl:text> (</xsl:text>
<xsl:value-of select="$sSecondItemComponentAbbr" />
<xsl:text>)</xsl:text>
</xsl:if>
<xsl:text> of the </xsl:text>
<xsl:value-of select="$sSecondItemDescription" />
<xsl:text>.</xsl:text>
</xsl:element>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ReportCompoundRuleFailure
	Params: sPreface - descriptive blurb about where the failure occurred (compound rule, derivational affixes, etc.)
			sFirstItemComponent - component of the first item that is failing
			sFirstItemComponentAbbr - descriptive abbreviation/value of the component
			sFirstItemDescription - description of the first item
			sFirstItemValue - stem node which failed
			sSecondItemComponent - component of the second item
				 sSecondItemComponentAbbr - descriptive abbreviation/value of the component
			sSecondItemDescription - description of the second item
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
<xsl:template name="ReportCompoundRuleFailure">
<xsl:param name="sPreface" />
<xsl:param name="sFirstItemComponent" />
<xsl:param name="sFirstItemComponentAbbr" />
<xsl:param name="sFirstItemDescription" />
<xsl:param name="sFirstItemValue" />
<xsl:param name="sSecondItemComponent" />
<xsl:param name="sSecondItemComponentAbbr" />
<xsl:param name="sSecondItemDescription" />
<xsl:element name="failure">
<content>
<xsl:value-of select="$sPreface" />
<xsl:text>The </xsl:text>
<xsl:value-of select="$sSecondItemComponent" />
<xsl:if test="string-length($sSecondItemComponentAbbr) &gt; 0">
<xsl:text> (</xsl:text>
<xsl:value-of select="$sSecondItemComponentAbbr" />
<xsl:text>)</xsl:text>
</xsl:if>
<xsl:text> of the </xsl:text>
<xsl:value-of select="$sSecondItemDescription" />
<xsl:text> is incompatible with the </xsl:text>
<xsl:value-of select="$sFirstItemComponent" />
<xsl:text> (</xsl:text>
<xsl:value-of select="$sFirstItemComponentAbbr" />
<xsl:text>) of the </xsl:text>
<xsl:value-of select="$sFirstItemDescription" />
<xsl:text>:</xsl:text>
</content>
<xsl:copy-of select="$sFirstItemValue" />
</xsl:element>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
StemDerivSfxProduction
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="StemDerivSfxProduction">
<seq step="Try to build a Stem node by appending a derivational suffix to another Stem node.">
<!--copy what's before the stem-->
<xsl:for-each select="preceding-sibling::*">
<xsl:copy-of select="." />
</xsl:for-each>
<!--have a new stem-->
<stem>
<xsl:call-template name="ApplyDerivationalConstraints">
<xsl:with-param name="derivMorph" select="following-sibling::morph[1]" />
<xsl:with-param name="sType">
<xsl:text>suffix</xsl:text>
</xsl:with-param>
</xsl:call-template>
</stem>
<!-- copy what's after the derivational suffix -->
<xsl:for-each select="following-sibling::*[position()&gt;1]">
<xsl:copy-of select="." />
</xsl:for-each>
</seq>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
StemStemProductions
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="StemStemProductions">
<xsl:call-template name="CompoundRule7132">
<xsl:with-param name="LeftMember" select="." />
<xsl:with-param name="RightMember" select="following-sibling::stem[1]" />
</xsl:call-template>
<xsl:call-template name="CompoundRule7136">
<xsl:with-param name="LeftMember" select="." />
<xsl:with-param name="RightMember" select="following-sibling::stem[1]" />
</xsl:call-template>
<xsl:call-template name="CompoundRule7141">
<xsl:with-param name="LeftMember" select="." />
<xsl:with-param name="RightMember" select="following-sibling::stem[1]" />
</xsl:call-template>
<xsl:call-template name="CompoundRule7146">
<xsl:with-param name="LeftMember" select="." />
<xsl:with-param name="RightMember" select="following-sibling::stem[1]" />
</xsl:call-template>
<xsl:call-template name="CompoundRule7150">
<xsl:with-param name="LeftMember" select="." />
<xsl:with-param name="RightMember" select="following-sibling::stem[1]" />
</xsl:call-template>
<xsl:call-template name="CompoundRule7154">
<xsl:with-param name="LeftMember" select="." />
<xsl:with-param name="RightMember" select="following-sibling::stem[1]" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CompoundRule7132
	Params: LeftMember - left stem
			RightMember - right stem
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CompoundRule7132">
<xsl:param name="LeftMember" />
<xsl:param name="RightMember" />
<seq step="Try to apply the Compound rule 'noun-adverb -&gt; intransitive verb, ic:2'.  The result will be a new Stem node.">
<!--copy what's before the left member stem -->
<xsl:for-each select="$LeftMember/preceding-sibling::*">
<xsl:copy-of select="." />
</xsl:for-each>
<!--have a new stem-->
<stem>
<xsl:variable name="sLeftCategoriesAreCompatible">
<xsl:call-template name="TestCompatibleCategories">
<xsl:with-param name="sFirstCat">81</xsl:with-param>
<xsl:with-param name="sSecondCat">
<xsl:value-of select="$LeftMember/@syncat" />
</xsl:with-param>
</xsl:call-template>
</xsl:variable>
<xsl:variable name="sRightCategoriesAreCompatible">
<xsl:call-template name="TestCompatibleCategories">
<xsl:with-param name="sFirstCat">73</xsl:with-param>
<xsl:with-param name="sSecondCat">
<xsl:value-of select="$RightMember/@syncat" />
</xsl:with-param>
</xsl:call-template>
</xsl:variable>
<xsl:choose>
<xsl:when test="$sLeftCategoriesAreCompatible = $sSuccess and $sRightCategoriesAreCompatible = $sSuccess">
<xsl:call-template name="ApplyCompoundRule">
<xsl:with-param name="LeftMember" select="$LeftMember" />
<xsl:with-param name="RightMember" select="$RightMember" />
<xsl:with-param name="sRuleHvo">7132</xsl:with-param>
</xsl:call-template>
</xsl:when>
<xsl:otherwise>
<xsl:if test="$sLeftCategoriesAreCompatible != $sSuccess">
<xsl:call-template name="ReportCompoundRuleFailure">
<xsl:with-param name="sPreface">In applying the compound rule "noun-adverb -&gt; intransitive verb, ic:2": </xsl:with-param>
<xsl:with-param name="sFirstItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemComponentAbbr" select="$LeftMember/@syncatAbbr" />
<xsl:with-param name="sFirstItemDescription">
<xsl:text>left-hand stem</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemValue" select="$LeftMember" />
<xsl:with-param name="sSecondItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sSecondItemComponentAbbr">N</xsl:with-param>
<xsl:with-param name="sSecondItemDescription">
<xsl:text>left-hand member of the compound rule</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:if>
<xsl:if test="$sRightCategoriesAreCompatible != $sSuccess">
<xsl:call-template name="ReportCompoundRuleFailure">
<xsl:with-param name="sPreface">In applying the compound rule "noun-adverb -&gt; intransitive verb, ic:2": </xsl:with-param>
<xsl:with-param name="sFirstItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemComponentAbbr" select="$RightMember/@syncatAbbr" />
<xsl:with-param name="sFirstItemDescription">
<xsl:text>right-hand stem</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemValue" select="$RightMember" />
<xsl:with-param name="sSecondItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sSecondItemComponentAbbr">Adv</xsl:with-param>
<xsl:with-param name="sSecondItemDescription">
<xsl:text>right-hand member of the compound rule</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:if>
</xsl:otherwise>
</xsl:choose>
<xsl:copy-of select="$LeftMember" />
<xsl:copy-of select="$RightMember" />
</stem>
<!-- copy what's after the right member stem -->
<xsl:for-each select="$RightMember/following-sibling::*">
<xsl:copy-of select="." />
</xsl:for-each>
</seq>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CompoundRule7136
	Params: LeftMember - left stem
			RightMember - right stem
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CompoundRule7136">
<xsl:param name="LeftMember" />
<xsl:param name="RightMember" />
<seq step="Try to apply the Compound rule 'adverb-noun --&gt; intransitive verb, ic:2'.  The result will be a new Stem node.">
<!--copy what's before the left member stem -->
<xsl:for-each select="$LeftMember/preceding-sibling::*">
<xsl:copy-of select="." />
</xsl:for-each>
<!--have a new stem-->
<stem>
<xsl:variable name="sLeftCategoriesAreCompatible">
<xsl:call-template name="TestCompatibleCategories">
<xsl:with-param name="sFirstCat">73</xsl:with-param>
<xsl:with-param name="sSecondCat">
<xsl:value-of select="$LeftMember/@syncat" />
</xsl:with-param>
</xsl:call-template>
</xsl:variable>
<xsl:variable name="sRightCategoriesAreCompatible">
<xsl:call-template name="TestCompatibleCategories">
<xsl:with-param name="sFirstCat">81</xsl:with-param>
<xsl:with-param name="sSecondCat">
<xsl:value-of select="$RightMember/@syncat" />
</xsl:with-param>
</xsl:call-template>
</xsl:variable>
<xsl:choose>
<xsl:when test="$sLeftCategoriesAreCompatible = $sSuccess and $sRightCategoriesAreCompatible = $sSuccess">
<xsl:call-template name="ApplyCompoundRule">
<xsl:with-param name="LeftMember" select="$LeftMember" />
<xsl:with-param name="RightMember" select="$RightMember" />
<xsl:with-param name="sRuleHvo">7136</xsl:with-param>
</xsl:call-template>
</xsl:when>
<xsl:otherwise>
<xsl:if test="$sLeftCategoriesAreCompatible != $sSuccess">
<xsl:call-template name="ReportCompoundRuleFailure">
<xsl:with-param name="sPreface">In applying the compound rule "adverb-noun --&gt; intransitive verb, ic:2": </xsl:with-param>
<xsl:with-param name="sFirstItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemComponentAbbr" select="$LeftMember/@syncatAbbr" />
<xsl:with-param name="sFirstItemDescription">
<xsl:text>left-hand stem</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemValue" select="$LeftMember" />
<xsl:with-param name="sSecondItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sSecondItemComponentAbbr">Adv</xsl:with-param>
<xsl:with-param name="sSecondItemDescription">
<xsl:text>left-hand member of the compound rule</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:if>
<xsl:if test="$sRightCategoriesAreCompatible != $sSuccess">
<xsl:call-template name="ReportCompoundRuleFailure">
<xsl:with-param name="sPreface">In applying the compound rule "adverb-noun --&gt; intransitive verb, ic:2": </xsl:with-param>
<xsl:with-param name="sFirstItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemComponentAbbr" select="$RightMember/@syncatAbbr" />
<xsl:with-param name="sFirstItemDescription">
<xsl:text>right-hand stem</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemValue" select="$RightMember" />
<xsl:with-param name="sSecondItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sSecondItemComponentAbbr">N</xsl:with-param>
<xsl:with-param name="sSecondItemDescription">
<xsl:text>right-hand member of the compound rule</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:if>
</xsl:otherwise>
</xsl:choose>
<xsl:copy-of select="$LeftMember" />
<xsl:copy-of select="$RightMember" />
</stem>
<!-- copy what's after the right member stem -->
<xsl:for-each select="$RightMember/following-sibling::*">
<xsl:copy-of select="." />
</xsl:for-each>
</seq>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CompoundRule7141
	Params: LeftMember - left stem
			RightMember - right stem
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CompoundRule7141">
<xsl:param name="LeftMember" />
<xsl:param name="RightMember" />
<seq step="Try to apply the Compound rule 'adj-noun with linker'.  The result will be a new Stem node.">
<!--copy what's before the left member stem -->
<xsl:for-each select="$LeftMember/preceding-sibling::*">
<xsl:copy-of select="." />
</xsl:for-each>
<!--have a new stem-->
<stem>
<xsl:variable name="sLeftCategoriesAreCompatible">
<xsl:call-template name="TestCompatibleCategories">
<xsl:with-param name="sFirstCat">70</xsl:with-param>
<xsl:with-param name="sSecondCat">
<xsl:value-of select="$LeftMember/@syncat" />
</xsl:with-param>
</xsl:call-template>
</xsl:variable>
<xsl:variable name="sRightCategoriesAreCompatible">
<xsl:call-template name="TestCompatibleCategories">
<xsl:with-param name="sFirstCat">81</xsl:with-param>
<xsl:with-param name="sSecondCat">
<xsl:value-of select="$RightMember/@syncat" />
</xsl:with-param>
</xsl:call-template>
</xsl:variable>
<xsl:choose>
<xsl:when test="$sLeftCategoriesAreCompatible = $sSuccess and $sRightCategoriesAreCompatible = $sSuccess">
<xsl:call-template name="ApplyCompoundRule">
<xsl:with-param name="LeftMember" select="$LeftMember" />
<xsl:with-param name="RightMember" select="$RightMember" />
<xsl:with-param name="sRuleHvo">7141</xsl:with-param>
</xsl:call-template>
</xsl:when>
<xsl:otherwise>
<xsl:if test="$sLeftCategoriesAreCompatible != $sSuccess">
<xsl:call-template name="ReportCompoundRuleFailure">
<xsl:with-param name="sPreface">In applying the compound rule "adj-noun with linker ": </xsl:with-param>
<xsl:with-param name="sFirstItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemComponentAbbr" select="$LeftMember/@syncatAbbr" />
<xsl:with-param name="sFirstItemDescription">
<xsl:text>left-hand stem</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemValue" select="$LeftMember" />
<xsl:with-param name="sSecondItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sSecondItemComponentAbbr">adj</xsl:with-param>
<xsl:with-param name="sSecondItemDescription">
<xsl:text>left-hand member of the compound rule</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:if>
<xsl:if test="$sRightCategoriesAreCompatible != $sSuccess">
<xsl:call-template name="ReportCompoundRuleFailure">
<xsl:with-param name="sPreface">In applying the compound rule "adj-noun with linker ": </xsl:with-param>
<xsl:with-param name="sFirstItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemComponentAbbr" select="$RightMember/@syncatAbbr" />
<xsl:with-param name="sFirstItemDescription">
<xsl:text>right-hand stem</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemValue" select="$RightMember" />
<xsl:with-param name="sSecondItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sSecondItemComponentAbbr">N</xsl:with-param>
<xsl:with-param name="sSecondItemDescription">
<xsl:text>right-hand member of the compound rule</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:if>
</xsl:otherwise>
</xsl:choose>
<xsl:copy-of select="$LeftMember" />
<xsl:copy-of select="$RightMember" />
</stem>
<!-- copy what's after the right member stem -->
<xsl:for-each select="$RightMember/following-sibling::*">
<xsl:copy-of select="." />
</xsl:for-each>
</seq>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CompoundRule7146
	Params: LeftMember - left stem
			RightMember - right stem
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CompoundRule7146">
<xsl:param name="LeftMember" />
<xsl:param name="RightMember" />
<seq step="Try to apply the Compound rule 'verb-adverb'.  The result will be a new Stem node.">
<!--copy what's before the left member stem -->
<xsl:for-each select="$LeftMember/preceding-sibling::*">
<xsl:copy-of select="." />
</xsl:for-each>
<!--have a new stem-->
<stem>
<xsl:variable name="sLeftCategoriesAreCompatible">
<xsl:call-template name="TestCompatibleCategories">
<xsl:with-param name="sFirstCat">146</xsl:with-param>
<xsl:with-param name="sSecondCat">
<xsl:value-of select="$LeftMember/@syncat" />
</xsl:with-param>
</xsl:call-template>
</xsl:variable>
<xsl:variable name="sRightCategoriesAreCompatible">
<xsl:call-template name="TestCompatibleCategories">
<xsl:with-param name="sFirstCat">73</xsl:with-param>
<xsl:with-param name="sSecondCat">
<xsl:value-of select="$RightMember/@syncat" />
</xsl:with-param>
</xsl:call-template>
</xsl:variable>
<xsl:choose>
<xsl:when test="$sLeftCategoriesAreCompatible = $sSuccess and $sRightCategoriesAreCompatible = $sSuccess">
<xsl:call-template name="ApplyCompoundRule">
<xsl:with-param name="LeftMember" select="$LeftMember" />
<xsl:with-param name="RightMember" select="$RightMember" />
<xsl:with-param name="sRuleHvo">7146</xsl:with-param>
</xsl:call-template>
</xsl:when>
<xsl:otherwise>
<xsl:if test="$sLeftCategoriesAreCompatible != $sSuccess">
<xsl:call-template name="ReportCompoundRuleFailure">
<xsl:with-param name="sPreface">In applying the compound rule "verb-adverb ": </xsl:with-param>
<xsl:with-param name="sFirstItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemComponentAbbr" select="$LeftMember/@syncatAbbr" />
<xsl:with-param name="sFirstItemDescription">
<xsl:text>left-hand stem</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemValue" select="$LeftMember" />
<xsl:with-param name="sSecondItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sSecondItemComponentAbbr">V</xsl:with-param>
<xsl:with-param name="sSecondItemDescription">
<xsl:text>left-hand member of the compound rule</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:if>
<xsl:if test="$sRightCategoriesAreCompatible != $sSuccess">
<xsl:call-template name="ReportCompoundRuleFailure">
<xsl:with-param name="sPreface">In applying the compound rule "verb-adverb ": </xsl:with-param>
<xsl:with-param name="sFirstItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemComponentAbbr" select="$RightMember/@syncatAbbr" />
<xsl:with-param name="sFirstItemDescription">
<xsl:text>right-hand stem</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemValue" select="$RightMember" />
<xsl:with-param name="sSecondItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sSecondItemComponentAbbr">Adv</xsl:with-param>
<xsl:with-param name="sSecondItemDescription">
<xsl:text>right-hand member of the compound rule</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:if>
</xsl:otherwise>
</xsl:choose>
<xsl:copy-of select="$LeftMember" />
<xsl:copy-of select="$RightMember" />
</stem>
<!-- copy what's after the right member stem -->
<xsl:for-each select="$RightMember/following-sibling::*">
<xsl:copy-of select="." />
</xsl:for-each>
</seq>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CompoundRule7150
	Params: LeftMember - left stem
			RightMember - right stem
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CompoundRule7150">
<xsl:param name="LeftMember" />
<xsl:param name="RightMember" />
<seq step="Try to apply the Compound rule 'noun-transitive verb'.  The result will be a new Stem node.">
<!--copy what's before the left member stem -->
<xsl:for-each select="$LeftMember/preceding-sibling::*">
<xsl:copy-of select="." />
</xsl:for-each>
<!--have a new stem-->
<stem>
<xsl:variable name="sLeftCategoriesAreCompatible">
<xsl:call-template name="TestCompatibleCategories">
<xsl:with-param name="sFirstCat">81</xsl:with-param>
<xsl:with-param name="sSecondCat">
<xsl:value-of select="$LeftMember/@syncat" />
</xsl:with-param>
</xsl:call-template>
</xsl:variable>
<xsl:variable name="sRightCategoriesAreCompatible">
<xsl:call-template name="TestCompatibleCategories">
<xsl:with-param name="sFirstCat">168</xsl:with-param>
<xsl:with-param name="sSecondCat">
<xsl:value-of select="$RightMember/@syncat" />
</xsl:with-param>
</xsl:call-template>
</xsl:variable>
<xsl:choose>
<xsl:when test="$sLeftCategoriesAreCompatible = $sSuccess and $sRightCategoriesAreCompatible = $sSuccess">
<xsl:call-template name="ApplyCompoundRule">
<xsl:with-param name="LeftMember" select="$LeftMember" />
<xsl:with-param name="RightMember" select="$RightMember" />
<xsl:with-param name="sRuleHvo">7150</xsl:with-param>
</xsl:call-template>
</xsl:when>
<xsl:otherwise>
<xsl:if test="$sLeftCategoriesAreCompatible != $sSuccess">
<xsl:call-template name="ReportCompoundRuleFailure">
<xsl:with-param name="sPreface">In applying the compound rule "noun-transitive verb ": </xsl:with-param>
<xsl:with-param name="sFirstItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemComponentAbbr" select="$LeftMember/@syncatAbbr" />
<xsl:with-param name="sFirstItemDescription">
<xsl:text>left-hand stem</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemValue" select="$LeftMember" />
<xsl:with-param name="sSecondItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sSecondItemComponentAbbr">N</xsl:with-param>
<xsl:with-param name="sSecondItemDescription">
<xsl:text>left-hand member of the compound rule</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:if>
<xsl:if test="$sRightCategoriesAreCompatible != $sSuccess">
<xsl:call-template name="ReportCompoundRuleFailure">
<xsl:with-param name="sPreface">In applying the compound rule "noun-transitive verb ": </xsl:with-param>
<xsl:with-param name="sFirstItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemComponentAbbr" select="$RightMember/@syncatAbbr" />
<xsl:with-param name="sFirstItemDescription">
<xsl:text>right-hand stem</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemValue" select="$RightMember" />
<xsl:with-param name="sSecondItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sSecondItemComponentAbbr">trans</xsl:with-param>
<xsl:with-param name="sSecondItemDescription">
<xsl:text>right-hand member of the compound rule</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:if>
</xsl:otherwise>
</xsl:choose>
<xsl:copy-of select="$LeftMember" />
<xsl:copy-of select="$RightMember" />
</stem>
<!-- copy what's after the right member stem -->
<xsl:for-each select="$RightMember/following-sibling::*">
<xsl:copy-of select="." />
</xsl:for-each>
</seq>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CompoundRule7154
	Params: LeftMember - left stem
			RightMember - right stem
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CompoundRule7154">
<xsl:param name="LeftMember" />
<xsl:param name="RightMember" />
<seq step="Try to apply the Compound rule 'noun-Intransitive verb'.  The result will be a new Stem node.">
<!--copy what's before the left member stem -->
<xsl:for-each select="$LeftMember/preceding-sibling::*">
<xsl:copy-of select="." />
</xsl:for-each>
<!--have a new stem-->
<stem>
<xsl:variable name="sLeftCategoriesAreCompatible">
<xsl:call-template name="TestCompatibleCategories">
<xsl:with-param name="sFirstCat">81</xsl:with-param>
<xsl:with-param name="sSecondCat">
<xsl:value-of select="$LeftMember/@syncat" />
</xsl:with-param>
</xsl:call-template>
</xsl:variable>
<xsl:variable name="sRightCategoriesAreCompatible">
<xsl:call-template name="TestCompatibleCategories">
<xsl:with-param name="sFirstCat">153</xsl:with-param>
<xsl:with-param name="sSecondCat">
<xsl:value-of select="$RightMember/@syncat" />
</xsl:with-param>
</xsl:call-template>
</xsl:variable>
<xsl:choose>
<xsl:when test="$sLeftCategoriesAreCompatible = $sSuccess and $sRightCategoriesAreCompatible = $sSuccess">
<xsl:call-template name="ApplyCompoundRule">
<xsl:with-param name="LeftMember" select="$LeftMember" />
<xsl:with-param name="RightMember" select="$RightMember" />
<xsl:with-param name="sRuleHvo">7154</xsl:with-param>
</xsl:call-template>
</xsl:when>
<xsl:otherwise>
<xsl:if test="$sLeftCategoriesAreCompatible != $sSuccess">
<xsl:call-template name="ReportCompoundRuleFailure">
<xsl:with-param name="sPreface">In applying the compound rule "noun-Intransitive verb ": </xsl:with-param>
<xsl:with-param name="sFirstItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemComponentAbbr" select="$LeftMember/@syncatAbbr" />
<xsl:with-param name="sFirstItemDescription">
<xsl:text>left-hand stem</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemValue" select="$LeftMember" />
<xsl:with-param name="sSecondItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sSecondItemComponentAbbr">N</xsl:with-param>
<xsl:with-param name="sSecondItemDescription">
<xsl:text>left-hand member of the compound rule</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:if>
<xsl:if test="$sRightCategoriesAreCompatible != $sSuccess">
<xsl:call-template name="ReportCompoundRuleFailure">
<xsl:with-param name="sPreface">In applying the compound rule "noun-Intransitive verb ": </xsl:with-param>
<xsl:with-param name="sFirstItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemComponentAbbr" select="$RightMember/@syncatAbbr" />
<xsl:with-param name="sFirstItemDescription">
<xsl:text>right-hand stem</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemValue" select="$RightMember" />
<xsl:with-param name="sSecondItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sSecondItemComponentAbbr">intrans</xsl:with-param>
<xsl:with-param name="sSecondItemDescription">
<xsl:text>right-hand member of the compound rule</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:if>
</xsl:otherwise>
</xsl:choose>
<xsl:copy-of select="$LeftMember" />
<xsl:copy-of select="$RightMember" />
</stem>
<!-- copy what's after the right member stem -->
<xsl:for-each select="$RightMember/following-sibling::*">
<xsl:copy-of select="." />
</xsl:for-each>
</seq>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ApplyCompoundRule
	Params: LeftMember - left stem
			RightMember - right stem
			sRuleHvo - hvo of the rule
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="ApplyCompoundRule">
<xsl:param name="LeftMember" />
<xsl:param name="RightMember" />
<xsl:param name="sRuleHvo" />
<xsl:choose>
<xsl:when test="$sRuleHvo='7132'">
<xsl:attribute name="syncat">153</xsl:attribute>
<xsl:attribute name="syncatAbbr"></xsl:attribute>
<xsl:attribute name="inflClass">179</xsl:attribute>
<xsl:attribute name="inflClassAbbr"></xsl:attribute>
<xsl:attribute name="requiresInfl">+</xsl:attribute>
<xsl:attribute name="blocksInfl">-</xsl:attribute>
</xsl:when>
<xsl:when test="$sRuleHvo='7136'">
<xsl:attribute name="syncat">153</xsl:attribute>
<xsl:attribute name="syncatAbbr"></xsl:attribute>
<xsl:attribute name="inflClass">179</xsl:attribute>
<xsl:attribute name="inflClassAbbr"></xsl:attribute>
<xsl:attribute name="requiresInfl">+</xsl:attribute>
<xsl:attribute name="blocksInfl">-</xsl:attribute>
</xsl:when>
<xsl:when test="$sRuleHvo='7141'">
<xsl:attribute name="syncat">
<xsl:value-of select="$RightMember/@syncat" />
</xsl:attribute>
<xsl:attribute name="syncatAbbr">
<xsl:value-of select="$RightMember/@syncatAbbr" />
</xsl:attribute>
<xsl:attribute name="inflClass">
<xsl:value-of select="$RightMember/@inflClass" />
</xsl:attribute>
<xsl:attribute name="inflClassAbbr">
<xsl:value-of select="$RightMember/@inflClassAbbr" />
</xsl:attribute>
<xsl:attribute name="requiresInfl">
<xsl:value-of select="$RightMember/@requiresInfl" />
</xsl:attribute>
<xsl:attribute name="blocksInfl">-</xsl:attribute>
<xsl:choose>
<xsl:when test="stemName">
<xsl:copy-of select="stemName" />
</xsl:when>
<xsl:when test="morph/stemName">
<xsl:copy-of select="morph/stemName" />
</xsl:when>
</xsl:choose>
</xsl:when>
<xsl:when test="$sRuleHvo='7146'">
<xsl:attribute name="syncat">
<xsl:value-of select="$LeftMember/@syncat" />
</xsl:attribute>
<xsl:attribute name="syncatAbbr">
<xsl:value-of select="$LeftMember/@syncatAbbr" />
</xsl:attribute>
<xsl:attribute name="inflClass">
<xsl:value-of select="$LeftMember/@inflClass" />
</xsl:attribute>
<xsl:attribute name="inflClassAbbr">
<xsl:value-of select="$LeftMember/@inflClassAbbr" />
</xsl:attribute>
<xsl:attribute name="requiresInfl">
<xsl:value-of select="$LeftMember/@requiresInfl" />
</xsl:attribute>
<xsl:attribute name="blocksInfl">-</xsl:attribute>
<xsl:choose>
<xsl:when test="stemName">
<xsl:copy-of select="stemName" />
</xsl:when>
<xsl:when test="morph/stemName">
<xsl:copy-of select="morph/stemName" />
</xsl:when>
</xsl:choose>
</xsl:when>
<xsl:when test="$sRuleHvo='7150'">
<xsl:attribute name="syncat">
<xsl:value-of select="$RightMember/@syncat" />
</xsl:attribute>
<xsl:attribute name="syncatAbbr">
<xsl:value-of select="$RightMember/@syncatAbbr" />
</xsl:attribute>
<xsl:attribute name="inflClass">
<xsl:value-of select="$RightMember/@inflClass" />
</xsl:attribute>
<xsl:attribute name="inflClassAbbr">
<xsl:value-of select="$RightMember/@inflClassAbbr" />
</xsl:attribute>
<xsl:attribute name="requiresInfl">
<xsl:value-of select="$RightMember/@requiresInfl" />
</xsl:attribute>
<xsl:attribute name="blocksInfl">-</xsl:attribute>
<xsl:choose>
<xsl:when test="stemName">
<xsl:copy-of select="stemName" />
</xsl:when>
<xsl:when test="morph/stemName">
<xsl:copy-of select="morph/stemName" />
</xsl:when>
</xsl:choose>
</xsl:when>
<xsl:when test="$sRuleHvo='7154'">
<xsl:attribute name="syncat">
<xsl:value-of select="$RightMember/@syncat" />
</xsl:attribute>
<xsl:attribute name="syncatAbbr">
<xsl:value-of select="$RightMember/@syncatAbbr" />
</xsl:attribute>
<xsl:attribute name="inflClass">
<xsl:value-of select="$RightMember/@inflClass" />
</xsl:attribute>
<xsl:attribute name="inflClassAbbr">
<xsl:value-of select="$RightMember/@inflClassAbbr" />
</xsl:attribute>
<xsl:attribute name="requiresInfl">
<xsl:value-of select="$RightMember/@requiresInfl" />
</xsl:attribute>
<xsl:attribute name="blocksInfl">-</xsl:attribute>
<xsl:choose>
<xsl:when test="stemName">
<xsl:copy-of select="stemName" />
</xsl:when>
<xsl:when test="morph/stemName">
<xsl:copy-of select="morph/stemName" />
</xsl:when>
</xsl:choose>
</xsl:when>
</xsl:choose>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
TestCompatibleCategories
	Params: firstCat
			secondCat
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
<xsl:template name="TestCompatibleCategories">
<xsl:param name="sFirstCat" />
<xsl:param name="sSecondCat" />
<xsl:choose>
<xsl:when test="not($sFirstCat) or $sFirstCat = '0' or $sFirstCat = '' or $sSecondCat = ''">
<xsl:value-of select="$sSuccess" />
</xsl:when>
<xsl:when test="$sFirstCat = $sSecondCat">
<xsl:value-of select="$sSuccess" />
</xsl:when>
<!-- build all the possible subcategory combinations here -->
<xsl:when test="$sFirstCat = '6'">
<xsl:if test="$sSecondCat = '7' or $sSecondCat = '10'">
<xsl:value-of select="$sSuccess" />
</xsl:if>
</xsl:when>
<xsl:when test="$sFirstCat = '21'">
<xsl:if test="$sSecondCat = '22' or $sSecondCat = '25' or $sSecondCat = '28' or $sSecondCat = '31' or $sSecondCat = '34' or $sSecondCat = '37' or $sSecondCat = '40' or $sSecondCat = '43' or $sSecondCat = '46' or $sSecondCat = '49' or $sSecondCat = '52' or $sSecondCat = '55' or $sSecondCat = '58' or $sSecondCat = '61' or $sSecondCat = '64'">
<xsl:value-of select="$sSuccess" />
</xsl:if>
</xsl:when>
<xsl:when test="$sFirstCat = '69'">
<xsl:if test="$sSecondCat = '70' or $sSecondCat = '73'">
<xsl:value-of select="$sSuccess" />
</xsl:if>
</xsl:when>
<xsl:when test="$sFirstCat = '81'">
<xsl:if test="$sSecondCat = '82' or $sSecondCat = '85' or $sSecondCat = '88' or $sSecondCat = '91' or $sSecondCat = '94'">
<xsl:value-of select="$sSuccess" />
</xsl:if>
</xsl:when>
<xsl:when test="$sFirstCat = '110'">
<xsl:if test="$sSecondCat = '111' or $sSecondCat = '114' or $sSecondCat = '117' or $sSecondCat = '120' or $sSecondCat = '123' or $sSecondCat = '126'">
<xsl:value-of select="$sSuccess" />
</xsl:if>
</xsl:when>
<xsl:when test="$sFirstCat = '131'">
<xsl:if test="$sSecondCat = '132' or $sSecondCat = '133' or $sSecondCat = '136' or $sSecondCat = '141'">
<xsl:value-of select="$sSuccess" />
</xsl:if>
</xsl:when>
<xsl:when test="$sFirstCat = '132'">
<xsl:if test="$sSecondCat = '133' or $sSecondCat = '136'">
<xsl:value-of select="$sSuccess" />
</xsl:if>
</xsl:when>
<xsl:when test="$sFirstCat = '146'">
<xsl:if test="$sSecondCat = '147' or $sSecondCat = '150' or $sSecondCat = '153' or $sSecondCat = '157' or $sSecondCat = '160' or $sSecondCat = '163' or $sSecondCat = '168' or $sSecondCat = '172'">
<xsl:value-of select="$sSuccess" />
</xsl:if>
</xsl:when>
</xsl:choose>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
UninflectedStemProduction
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="UninflectedStemProduction">
<seq step="Try to build a Full analysis node on an uninflected Stem node.">
<!--copy what's before the stem-->
<xsl:for-each select="preceding-sibling::*">
<xsl:copy-of select="." />
</xsl:for-each>
<!--have a full-->
<full>
<xsl:attribute name="syncat">
<xsl:value-of select="@syncat" />
</xsl:attribute>
<xsl:attribute name="syncatAbbr">
<xsl:value-of select="@syncatAbbr" />
</xsl:attribute>
<xsl:attribute name="requiresInfl">
<xsl:value-of select="@requiresInfl" />
</xsl:attribute>
<xsl:attribute name="inflected">
<xsl:text>-</xsl:text>
</xsl:attribute>
<xsl:if test="@blocksInflection='+'">
<failure>
<xsl:text>Tried to make the stem be uninflected, but the stem has been inflected via a non-final template.  Therefore, a derivatoinal affix or a compound rule must apply first.</xsl:text>
</failure>
</xsl:if>
<xsl:copy-of select="." />
</full>
<!-- copy what's after the stem -->
<xsl:for-each select="following-sibling::*">
<xsl:copy-of select="." />
</xsl:for-each>
</seq>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
InflectionalTemplate102
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="InflectionalTemplate102">
<seq step="Try to apply inflectional template 'Possessed noun'.  The result will be a Full analysis node.">
<xsl:variable name="PrefixSlotMorphs" />
<xsl:variable name="iPrefixSlotMorphs">0</xsl:variable>
<xsl:variable name="SuffixSlotMorphs">
<xsl:call-template name="InflectionalTemplate102SuffixSlot103">
<xsl:with-param name="morph" select="following-sibling::*[1]" />
</xsl:call-template>
</xsl:variable>
<xsl:variable name="iSuffixSlotMorphs">
<xsl:choose>
<xsl:when test="not(contains($SuffixSlotMorphs, ' '))">
<xsl:value-of select="string-length($SuffixSlotMorphs)" />
</xsl:when>
<xsl:otherwise>0</xsl:otherwise>
</xsl:choose>
</xsl:variable>
<!-- copy what's before the template -->
<xsl:for-each select="preceding-sibling::*[position()&gt;$iPrefixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
<!--have a full-->
<full>
<xsl:attribute name="syncat">
<xsl:value-of select="@syncat" />
</xsl:attribute>
<xsl:attribute name="syncatAbbr">
<xsl:value-of select="@syncatAbbr" />
</xsl:attribute>
<xsl:attribute name="requiresInfl">
<xsl:text>-</xsl:text>
</xsl:attribute>
<xsl:attribute name="inflected">
<xsl:text>+</xsl:text>
</xsl:attribute>
<!--check category of the stem-->
<xsl:variable name="sCategoriesAreCompatible">
<xsl:call-template name="TestCompatibleCategories">
<xsl:with-param name="sFirstCat">81</xsl:with-param>
<xsl:with-param name="sSecondCat">
<xsl:value-of select="@syncat" />
</xsl:with-param>
</xsl:call-template>
</xsl:variable>
<xsl:if test="$sCategoriesAreCompatible != $sSuccess">
<xsl:call-template name="ReportFailure">
<xsl:with-param name="sFirstItemComponent">
<xsl:text> category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemComponentAbbr">N</xsl:with-param>
<xsl:with-param name="sFirstItemDescription">
<xsl:text>inflectional template</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemValue">Possessed noun</xsl:with-param>
<xsl:with-param name="sSecondItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sSecondItemComponentAbbr" select="@syncatAbbr" />
<xsl:with-param name="sSecondItemDescription">
<xsl:text>stem</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:if>
<!--Check for productivity restrictions-->
<xsl:variable name="stem" select="." />
<xsl:if test="$iPrefixSlotMorphs &gt; 0">
<xsl:for-each select="preceding-sibling::*[position()&lt;=$iPrefixSlotMorphs]">
<xsl:call-template name="CheckInflectionalAffixProductivityRestrictions">
<xsl:with-param name="stemMorph" select="$stem" />
<xsl:with-param name="inflMorph" select="." />
<xsl:with-param name="sType" select="'prefix'" />
</xsl:call-template>
</xsl:for-each>
</xsl:if>
<xsl:if test="$iSuffixSlotMorphs &gt; 0">
<xsl:for-each select="following-sibling::*[position()&lt;=$iSuffixSlotMorphs]">
<xsl:call-template name="CheckInflectionalAffixProductivityRestrictions">
<xsl:with-param name="stemMorph" select="$stem" />
<xsl:with-param name="inflMorph" select="." />
<xsl:with-param name="sType" select="'suffix'" />
</xsl:call-template>
</xsl:for-each>
</xsl:if>
<!--Percolate features-->
<!--Check compatitbility of all inflection features-->
<xsl:variable name="InflFeaturesToPercolate">
<xsl:variable name="PrefixSlotsUnification">
<xsl:choose>
<xsl:when test="$iPrefixSlotMorphs &gt; 1">
<xsl:call-template name="UnifyPrefixSlots">
<xsl:with-param name="PreviousResult" />
<xsl:with-param name="PrefixSlot" select="preceding-sibling::*[1]" />
<xsl:with-param name="iRemaining" select="$iPrefixSlotMorphs - 1" />
<xsl:with-param name="sRuleInfo">The inflectional template named 'Possessed noun' for category 'noun'</xsl:with-param>
</xsl:call-template>
</xsl:when>
<xsl:when test="$iPrefixSlotMorphs=1">
<xsl:variable name="InflectionalPrefix" select="preceding-sibling::*[1]" />
<xsl:call-template name="UnifyTwoFeatureStructures">
<xsl:with-param name="FirstFS" select="fs" />
<xsl:with-param name="SecondFS" select="$InflectionalPrefix/inflMsa/fs" />
<xsl:with-param name="sTopLevelId" select="fs/@id" />
<xsl:with-param name="sRuleInfo">The inflectional template named 'Possessed noun' for category 'noun'</xsl:with-param>
<xsl:with-param name="sFirstDescription" select="'stem'" />
<xsl:with-param name="sSecondDescription">
<xsl:text>inflectional prefix (</xsl:text>
<xsl:value-of select="$InflectionalPrefix/shortName" />
<xsl:text>)</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:when>
</xsl:choose>
</xsl:variable>
<xsl:variable name="SuffixSlotsUnification">
<xsl:choose>
<xsl:when test="$iSuffixSlotMorphs &gt; 1">
<xsl:call-template name="UnifySuffixSlots">
<xsl:with-param name="PreviousResult" />
<xsl:with-param name="SuffixSlot" select="following-sibling::*[1]" />
<xsl:with-param name="iRemaining" select="$iSuffixSlotMorphs - 1" />
<xsl:with-param name="sRuleInfo">The inflectional template named 'Possessed noun' for category 'noun'</xsl:with-param>
</xsl:call-template>
</xsl:when>
<xsl:when test="$iSuffixSlotMorphs=1">
<xsl:variable name="InflectionalSuffix" select="following-sibling::*[1]" />
<xsl:call-template name="UnifyTwoFeatureStructures">
<xsl:with-param name="FirstFS" select="fs" />
<xsl:with-param name="SecondFS" select="$InflectionalSuffix/inflMsa/fs" />
<xsl:with-param name="sTopLevelId" select="fs/@id" />
<xsl:with-param name="sRuleInfo">The inflectional template named 'Possessed noun' for category 'noun'</xsl:with-param>
<xsl:with-param name="sFirstDescription" select="'stem'" />
<xsl:with-param name="sSecondDescription">
<xsl:text>inflectional suffix (</xsl:text>
<xsl:value-of select="$InflectionalSuffix/shortName" />
<xsl:text>)</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:when>
</xsl:choose>
</xsl:variable>
<xsl:variable name="PrefixSuffixSlotsUnification">
<xsl:choose>
<xsl:when test="string-length($PrefixSlotsUnification) &gt; 0 and string-length($SuffixSlotsUnification) &gt; 0">
<xsl:call-template name="UnifyTwoFeatureStructures">
<xsl:with-param name="FirstFS" select="msxsl:node-set($PrefixSlotsUnification)/fs" />
<xsl:with-param name="SecondFS" select="msxsl:node-set($SuffixSlotsUnification)/fs" />
<xsl:with-param name="sTopLevelId" select="msxsl:node-set($PrefixSlotsUnification)/fs/@id" />
<xsl:with-param name="sRuleInfo">The inflectional template named 'Possessed noun' for category 'noun'</xsl:with-param>
<xsl:with-param name="sFirstDescription">
<xsl:value-of select="'unification of all the prefix slots'" />
</xsl:with-param>
<xsl:with-param name="sSecondDescription">
<xsl:value-of select="'unification of all the suffix slots'" />
</xsl:with-param>
</xsl:call-template>
</xsl:when>
<xsl:when test="string-length($PrefixSlotsUnification) &gt; 0">
<xsl:copy-of select="$PrefixSlotsUnification" />
</xsl:when>
<xsl:when test="string-length($SuffixSlotsUnification) &gt; 0">
<xsl:copy-of select="$SuffixSlotsUnification" />
</xsl:when>
</xsl:choose>
</xsl:variable>
<xsl:call-template name="UnifyTwoFeatureStructures">
<xsl:with-param name="FirstFS" select="fs" />
<xsl:with-param name="SecondFS" select="msxsl:node-set($PrefixSuffixSlotsUnification)/fs" />
<xsl:with-param name="sTopLevelId" select="fs/@id" />
<xsl:with-param name="sRuleInfo">The inflectional template named 'Possessed noun' for category 'noun'</xsl:with-param>
<xsl:with-param name="sFirstDescription">
<xsl:value-of select="'stem'" />
</xsl:with-param>
<xsl:with-param name="sSecondDescription">
<xsl:value-of select="'unification of all the slots'" />
</xsl:with-param>
</xsl:call-template>
</xsl:variable>
<xsl:if test="msxsl:node-set($InflFeaturesToPercolate)/fs/descendant::feature">
<xsl:copy-of select="$InflFeaturesToPercolate" />
</xsl:if>
<xsl:variable name="StemNameConstraintResult">
<xsl:call-template name="CheckStemNames81">
<xsl:with-param name="stemName" select="stemName/@id" />
<xsl:with-param name="InflFeatures" select="msxsl:node-set($InflFeaturesToPercolate)" />
</xsl:call-template>
</xsl:variable>
<xsl:if test="msxsl:node-set($StemNameConstraintResult)/failure">
<xsl:copy-of select="msxsl:node-set($StemNameConstraintResult)" />
</xsl:if>
<xsl:if test="@blocksInflection!='-'">
<failure>The inflectional template named 'Possessed noun' for category 'noun' failed because the stem was built by a non-final template and there was no intervening derivation or compounding.</failure>
</xsl:if>
<xsl:if test="name()='partial' and @inflected='+'">
<failure>Partial inflectional template has already been inflected.</failure>
</xsl:if>
<!--copy any inflectional prefixes-->
<xsl:choose>
<xsl:when test="$iPrefixSlotMorphs &gt; 0">
<xsl:for-each select="preceding-sibling::*[position()&lt;=$iPrefixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</xsl:when>
<xsl:otherwise>
<!-- output any failure nodes -->
<xsl:copy-of select="$PrefixSlotMorphs" />
</xsl:otherwise>
</xsl:choose>
<!--copy the stem-->
<xsl:copy-of select="." />
<!--copy any inflectional suffixes-->
<xsl:choose>
<xsl:when test="$iSuffixSlotMorphs &gt; 0">
<xsl:for-each select="following-sibling::*[position()&lt;=$iSuffixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</xsl:when>
<xsl:otherwise>
<!-- output any failure nodes -->
<xsl:copy-of select="$SuffixSlotMorphs" />
</xsl:otherwise>
</xsl:choose>
</full>
<!-- copy what's after the template -->
<xsl:for-each select="following-sibling::*[position()&gt;$iSuffixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</seq>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
PartialInflectionalTemplate102POS81
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="PartialInflectionalTemplate102POS81">
<seq step="Try to apply inflectional template 'Possessed noun' on a Partial analysis node.  Apply it for category 'noun'. The result will be another Partial analysis node.">
<xsl:variable name="PrefixSlotMorphs" />
<xsl:variable name="iPrefixSlotMorphs">0</xsl:variable>
<xsl:variable name="SuffixSlotMorphs">
<xsl:call-template name="InflectionalTemplate102SuffixSlot103">
<xsl:with-param name="morph" select="following-sibling::*[1]" />
</xsl:call-template>
</xsl:variable>
<xsl:variable name="iSuffixSlotMorphs">
<xsl:choose>
<xsl:when test="not(contains($SuffixSlotMorphs, ' '))">
<xsl:value-of select="string-length($SuffixSlotMorphs)" />
</xsl:when>
<xsl:otherwise>0</xsl:otherwise>
</xsl:choose>
</xsl:variable>
<!-- copy what's before the template -->
<xsl:for-each select="preceding-sibling::*[position()&gt;$iPrefixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
<partial>
<xsl:attribute name="syncat">
<xsl:text>81</xsl:text>
</xsl:attribute>
<xsl:attribute name="syncatAbbr">
<xsl:text>N</xsl:text>
</xsl:attribute>
<xsl:attribute name="requiresInfl">
<xsl:text>-</xsl:text>
</xsl:attribute>
<xsl:attribute name="inflected">
<xsl:text>+</xsl:text>
</xsl:attribute>
<xsl:if test="@blocksInflection!='-'">
<failure>The inflectional template named 'Possessed noun' for category 'noun' failed because the stem was built by a non-final template and there was no intervening derivation or compounding.</failure>
</xsl:if>
<xsl:if test="name()='partial' and @inflected='+'">
<failure>Partial inflectional template has already been inflected.</failure>
</xsl:if>
<!--copy any inflectional prefixes-->
<xsl:choose>
<xsl:when test="$iPrefixSlotMorphs &gt; 0">
<xsl:for-each select="preceding-sibling::*[position()&lt;=$iPrefixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</xsl:when>
<xsl:otherwise>
<!-- output any failure nodes -->
<xsl:copy-of select="$PrefixSlotMorphs" />
</xsl:otherwise>
</xsl:choose>
<!--copy the stem-->
<xsl:copy-of select="." />
<!--copy any inflectional suffixes-->
<xsl:choose>
<xsl:when test="$iSuffixSlotMorphs &gt; 0">
<xsl:for-each select="following-sibling::*[position()&lt;=$iSuffixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</xsl:when>
<xsl:otherwise>
<!-- output any failure nodes -->
<xsl:copy-of select="$SuffixSlotMorphs" />
</xsl:otherwise>
</xsl:choose>
</partial>
<!-- copy what's after the template -->
<xsl:for-each select="following-sibling::*[position()&gt;$iSuffixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</seq>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
PartialInflectionalTemplate102POS82
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="PartialInflectionalTemplate102POS82">
<seq step="Try to apply inflectional template 'Possessed noun' on a Partial analysis node.  Apply it for category 'common noun'. The result will be another Partial analysis node.">
<xsl:variable name="PrefixSlotMorphs" />
<xsl:variable name="iPrefixSlotMorphs">0</xsl:variable>
<xsl:variable name="SuffixSlotMorphs">
<xsl:call-template name="InflectionalTemplate102SuffixSlot103">
<xsl:with-param name="morph" select="following-sibling::*[1]" />
</xsl:call-template>
</xsl:variable>
<xsl:variable name="iSuffixSlotMorphs">
<xsl:choose>
<xsl:when test="not(contains($SuffixSlotMorphs, ' '))">
<xsl:value-of select="string-length($SuffixSlotMorphs)" />
</xsl:when>
<xsl:otherwise>0</xsl:otherwise>
</xsl:choose>
</xsl:variable>
<!-- copy what's before the template -->
<xsl:for-each select="preceding-sibling::*[position()&gt;$iPrefixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
<partial>
<xsl:attribute name="syncat">
<xsl:text>82</xsl:text>
</xsl:attribute>
<xsl:attribute name="syncatAbbr">
<xsl:text>comm</xsl:text>
</xsl:attribute>
<xsl:attribute name="requiresInfl">
<xsl:text>-</xsl:text>
</xsl:attribute>
<xsl:attribute name="inflected">
<xsl:text>+</xsl:text>
</xsl:attribute>
<xsl:if test="@blocksInflection!='-'">
<failure>The inflectional template named 'Possessed noun' for category 'noun' failed because the stem was built by a non-final template and there was no intervening derivation or compounding.</failure>
</xsl:if>
<xsl:if test="name()='partial' and @inflected='+'">
<failure>Partial inflectional template has already been inflected.</failure>
</xsl:if>
<!--copy any inflectional prefixes-->
<xsl:choose>
<xsl:when test="$iPrefixSlotMorphs &gt; 0">
<xsl:for-each select="preceding-sibling::*[position()&lt;=$iPrefixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</xsl:when>
<xsl:otherwise>
<!-- output any failure nodes -->
<xsl:copy-of select="$PrefixSlotMorphs" />
</xsl:otherwise>
</xsl:choose>
<!--copy the stem-->
<xsl:copy-of select="." />
<!--copy any inflectional suffixes-->
<xsl:choose>
<xsl:when test="$iSuffixSlotMorphs &gt; 0">
<xsl:for-each select="following-sibling::*[position()&lt;=$iSuffixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</xsl:when>
<xsl:otherwise>
<!-- output any failure nodes -->
<xsl:copy-of select="$SuffixSlotMorphs" />
</xsl:otherwise>
</xsl:choose>
</partial>
<!-- copy what's after the template -->
<xsl:for-each select="following-sibling::*[position()&gt;$iSuffixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</seq>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
PartialInflectionalTemplate102POS85
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="PartialInflectionalTemplate102POS85">
<seq step="Try to apply inflectional template 'Possessed noun' on a Partial analysis node.  Apply it for category 'concrete noun'. The result will be another Partial analysis node.">
<xsl:variable name="PrefixSlotMorphs" />
<xsl:variable name="iPrefixSlotMorphs">0</xsl:variable>
<xsl:variable name="SuffixSlotMorphs">
<xsl:call-template name="InflectionalTemplate102SuffixSlot103">
<xsl:with-param name="morph" select="following-sibling::*[1]" />
</xsl:call-template>
</xsl:variable>
<xsl:variable name="iSuffixSlotMorphs">
<xsl:choose>
<xsl:when test="not(contains($SuffixSlotMorphs, ' '))">
<xsl:value-of select="string-length($SuffixSlotMorphs)" />
</xsl:when>
<xsl:otherwise>0</xsl:otherwise>
</xsl:choose>
</xsl:variable>
<!-- copy what's before the template -->
<xsl:for-each select="preceding-sibling::*[position()&gt;$iPrefixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
<partial>
<xsl:attribute name="syncat">
<xsl:text>85</xsl:text>
</xsl:attribute>
<xsl:attribute name="syncatAbbr">
<xsl:text>conc</xsl:text>
</xsl:attribute>
<xsl:attribute name="requiresInfl">
<xsl:text>-</xsl:text>
</xsl:attribute>
<xsl:attribute name="inflected">
<xsl:text>+</xsl:text>
</xsl:attribute>
<xsl:if test="@blocksInflection!='-'">
<failure>The inflectional template named 'Possessed noun' for category 'noun' failed because the stem was built by a non-final template and there was no intervening derivation or compounding.</failure>
</xsl:if>
<xsl:if test="name()='partial' and @inflected='+'">
<failure>Partial inflectional template has already been inflected.</failure>
</xsl:if>
<!--copy any inflectional prefixes-->
<xsl:choose>
<xsl:when test="$iPrefixSlotMorphs &gt; 0">
<xsl:for-each select="preceding-sibling::*[position()&lt;=$iPrefixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</xsl:when>
<xsl:otherwise>
<!-- output any failure nodes -->
<xsl:copy-of select="$PrefixSlotMorphs" />
</xsl:otherwise>
</xsl:choose>
<!--copy the stem-->
<xsl:copy-of select="." />
<!--copy any inflectional suffixes-->
<xsl:choose>
<xsl:when test="$iSuffixSlotMorphs &gt; 0">
<xsl:for-each select="following-sibling::*[position()&lt;=$iSuffixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</xsl:when>
<xsl:otherwise>
<!-- output any failure nodes -->
<xsl:copy-of select="$SuffixSlotMorphs" />
</xsl:otherwise>
</xsl:choose>
</partial>
<!-- copy what's after the template -->
<xsl:for-each select="following-sibling::*[position()&gt;$iSuffixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</seq>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
PartialInflectionalTemplate102POS88
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="PartialInflectionalTemplate102POS88">
<seq step="Try to apply inflectional template 'Possessed noun' on a Partial analysis node.  Apply it for category 'nominal'. The result will be another Partial analysis node.">
<xsl:variable name="PrefixSlotMorphs" />
<xsl:variable name="iPrefixSlotMorphs">0</xsl:variable>
<xsl:variable name="SuffixSlotMorphs">
<xsl:call-template name="InflectionalTemplate102SuffixSlot103">
<xsl:with-param name="morph" select="following-sibling::*[1]" />
</xsl:call-template>
</xsl:variable>
<xsl:variable name="iSuffixSlotMorphs">
<xsl:choose>
<xsl:when test="not(contains($SuffixSlotMorphs, ' '))">
<xsl:value-of select="string-length($SuffixSlotMorphs)" />
</xsl:when>
<xsl:otherwise>0</xsl:otherwise>
</xsl:choose>
</xsl:variable>
<!-- copy what's before the template -->
<xsl:for-each select="preceding-sibling::*[position()&gt;$iPrefixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
<partial>
<xsl:attribute name="syncat">
<xsl:text>88</xsl:text>
</xsl:attribute>
<xsl:attribute name="syncatAbbr">
<xsl:text>nom</xsl:text>
</xsl:attribute>
<xsl:attribute name="requiresInfl">
<xsl:text>-</xsl:text>
</xsl:attribute>
<xsl:attribute name="inflected">
<xsl:text>+</xsl:text>
</xsl:attribute>
<xsl:if test="@blocksInflection!='-'">
<failure>The inflectional template named 'Possessed noun' for category 'noun' failed because the stem was built by a non-final template and there was no intervening derivation or compounding.</failure>
</xsl:if>
<xsl:if test="name()='partial' and @inflected='+'">
<failure>Partial inflectional template has already been inflected.</failure>
</xsl:if>
<!--copy any inflectional prefixes-->
<xsl:choose>
<xsl:when test="$iPrefixSlotMorphs &gt; 0">
<xsl:for-each select="preceding-sibling::*[position()&lt;=$iPrefixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</xsl:when>
<xsl:otherwise>
<!-- output any failure nodes -->
<xsl:copy-of select="$PrefixSlotMorphs" />
</xsl:otherwise>
</xsl:choose>
<!--copy the stem-->
<xsl:copy-of select="." />
<!--copy any inflectional suffixes-->
<xsl:choose>
<xsl:when test="$iSuffixSlotMorphs &gt; 0">
<xsl:for-each select="following-sibling::*[position()&lt;=$iSuffixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</xsl:when>
<xsl:otherwise>
<!-- output any failure nodes -->
<xsl:copy-of select="$SuffixSlotMorphs" />
</xsl:otherwise>
</xsl:choose>
</partial>
<!-- copy what's after the template -->
<xsl:for-each select="following-sibling::*[position()&gt;$iSuffixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</seq>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
PartialInflectionalTemplate102POS91
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="PartialInflectionalTemplate102POS91">
<seq step="Try to apply inflectional template 'Possessed noun' on a Partial analysis node.  Apply it for category 'possessive noun'. The result will be another Partial analysis node.">
<xsl:variable name="PrefixSlotMorphs" />
<xsl:variable name="iPrefixSlotMorphs">0</xsl:variable>
<xsl:variable name="SuffixSlotMorphs">
<xsl:call-template name="InflectionalTemplate102SuffixSlot103">
<xsl:with-param name="morph" select="following-sibling::*[1]" />
</xsl:call-template>
</xsl:variable>
<xsl:variable name="iSuffixSlotMorphs">
<xsl:choose>
<xsl:when test="not(contains($SuffixSlotMorphs, ' '))">
<xsl:value-of select="string-length($SuffixSlotMorphs)" />
</xsl:when>
<xsl:otherwise>0</xsl:otherwise>
</xsl:choose>
</xsl:variable>
<!-- copy what's before the template -->
<xsl:for-each select="preceding-sibling::*[position()&gt;$iPrefixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
<partial>
<xsl:attribute name="syncat">
<xsl:text>91</xsl:text>
</xsl:attribute>
<xsl:attribute name="syncatAbbr">
<xsl:text>poss</xsl:text>
</xsl:attribute>
<xsl:attribute name="requiresInfl">
<xsl:text>-</xsl:text>
</xsl:attribute>
<xsl:attribute name="inflected">
<xsl:text>+</xsl:text>
</xsl:attribute>
<xsl:if test="@blocksInflection!='-'">
<failure>The inflectional template named 'Possessed noun' for category 'noun' failed because the stem was built by a non-final template and there was no intervening derivation or compounding.</failure>
</xsl:if>
<xsl:if test="name()='partial' and @inflected='+'">
<failure>Partial inflectional template has already been inflected.</failure>
</xsl:if>
<!--copy any inflectional prefixes-->
<xsl:choose>
<xsl:when test="$iPrefixSlotMorphs &gt; 0">
<xsl:for-each select="preceding-sibling::*[position()&lt;=$iPrefixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</xsl:when>
<xsl:otherwise>
<!-- output any failure nodes -->
<xsl:copy-of select="$PrefixSlotMorphs" />
</xsl:otherwise>
</xsl:choose>
<!--copy the stem-->
<xsl:copy-of select="." />
<!--copy any inflectional suffixes-->
<xsl:choose>
<xsl:when test="$iSuffixSlotMorphs &gt; 0">
<xsl:for-each select="following-sibling::*[position()&lt;=$iSuffixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</xsl:when>
<xsl:otherwise>
<!-- output any failure nodes -->
<xsl:copy-of select="$SuffixSlotMorphs" />
</xsl:otherwise>
</xsl:choose>
</partial>
<!-- copy what's after the template -->
<xsl:for-each select="following-sibling::*[position()&gt;$iSuffixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</seq>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
PartialInflectionalTemplate102POS94
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="PartialInflectionalTemplate102POS94">
<seq step="Try to apply inflectional template 'Possessed noun' on a Partial analysis node.  Apply it for category 'proper noun'. The result will be another Partial analysis node.">
<xsl:variable name="PrefixSlotMorphs" />
<xsl:variable name="iPrefixSlotMorphs">0</xsl:variable>
<xsl:variable name="SuffixSlotMorphs">
<xsl:call-template name="InflectionalTemplate102SuffixSlot103">
<xsl:with-param name="morph" select="following-sibling::*[1]" />
</xsl:call-template>
</xsl:variable>
<xsl:variable name="iSuffixSlotMorphs">
<xsl:choose>
<xsl:when test="not(contains($SuffixSlotMorphs, ' '))">
<xsl:value-of select="string-length($SuffixSlotMorphs)" />
</xsl:when>
<xsl:otherwise>0</xsl:otherwise>
</xsl:choose>
</xsl:variable>
<!-- copy what's before the template -->
<xsl:for-each select="preceding-sibling::*[position()&gt;$iPrefixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
<partial>
<xsl:attribute name="syncat">
<xsl:text>94</xsl:text>
</xsl:attribute>
<xsl:attribute name="syncatAbbr">
<xsl:text>prop</xsl:text>
</xsl:attribute>
<xsl:attribute name="requiresInfl">
<xsl:text>-</xsl:text>
</xsl:attribute>
<xsl:attribute name="inflected">
<xsl:text>+</xsl:text>
</xsl:attribute>
<xsl:if test="@blocksInflection!='-'">
<failure>The inflectional template named 'Possessed noun' for category 'noun' failed because the stem was built by a non-final template and there was no intervening derivation or compounding.</failure>
</xsl:if>
<xsl:if test="name()='partial' and @inflected='+'">
<failure>Partial inflectional template has already been inflected.</failure>
</xsl:if>
<!--copy any inflectional prefixes-->
<xsl:choose>
<xsl:when test="$iPrefixSlotMorphs &gt; 0">
<xsl:for-each select="preceding-sibling::*[position()&lt;=$iPrefixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</xsl:when>
<xsl:otherwise>
<!-- output any failure nodes -->
<xsl:copy-of select="$PrefixSlotMorphs" />
</xsl:otherwise>
</xsl:choose>
<!--copy the stem-->
<xsl:copy-of select="." />
<!--copy any inflectional suffixes-->
<xsl:choose>
<xsl:when test="$iSuffixSlotMorphs &gt; 0">
<xsl:for-each select="following-sibling::*[position()&lt;=$iSuffixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</xsl:when>
<xsl:otherwise>
<!-- output any failure nodes -->
<xsl:copy-of select="$SuffixSlotMorphs" />
</xsl:otherwise>
</xsl:choose>
</partial>
<!-- copy what's after the template -->
<xsl:for-each select="following-sibling::*[position()&gt;$iSuffixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</seq>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
InflectionalTemplate102SuffixSlot103
	Params: morph - morpheme to check
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="InflectionalTemplate102SuffixSlot103">
<xsl:param name="morph" />
<xsl:choose>
<xsl:when test="$morph/@wordType = '103'">
<!--Optional slot matches: if it is compatible, try next slot (if any) with next morph-->
<!--Check inflection class compatibility-->
<xsl:variable name="sStemInflClass">
<xsl:value-of select="@inflClass" />
</xsl:variable>
<xsl:choose>
<xsl:when test="$morph/inflClasses">
<xsl:choose>
<xsl:when test="$morph/inflClasses[inflClass/@hvo=$sStemInflClass] or $sStemInflClass=''">
<xsl:call-template name="IndicateInflAffixSucceeded" />
<xsl:call-template name="InflectionalTemplate102SuffixSlot103">
<xsl:with-param name="morph" select="$morph/following-sibling::morph[1]" />
</xsl:call-template>
</xsl:when>
<xsl:otherwise>
<failure>The inflectional template named 'Possessed noun' for category 'noun'<xsl:text> failed because in the </xsl:text>optional suffix<xsl:text> slot '</xsl:text>Possessor<xsl:text>', the inflection class of the stem (</xsl:text>
<xsl:value-of select="@inflClassAbbr" />
<xsl:text>) does not match any of the inflection classes of the inflectional affix (</xsl:text>
<xsl:value-of select="$morph/shortName" />
<xsl:text>).  The inflection class</xsl:text>
<xsl:choose>
<xsl:when test="count($morph/inflClasses/inflClass)=1">
<xsl:text> of this affix is: </xsl:text>
</xsl:when>
<xsl:otherwise>
<xsl:text>es of this affix are: </xsl:text>
</xsl:otherwise>
</xsl:choose>
<xsl:for-each select="$morph/inflClasses/inflClass">
<xsl:value-of select="@abbr" />
<xsl:if test="position() != last()">
<xsl:text>, </xsl:text>
</xsl:if>
</xsl:for-each>
<xsl:text>.</xsl:text>
</failure>
</xsl:otherwise>
</xsl:choose>
</xsl:when>
<xsl:otherwise>
<!--No inflection classes to check; indicate success and try next slot (if any)-->
<xsl:call-template name="IndicateInflAffixSucceeded" />
<xsl:call-template name="InflectionalTemplate102SuffixSlot103">
<xsl:with-param name="morph" select="$morph/following-sibling::morph[1]" />
</xsl:call-template>
</xsl:otherwise>
</xsl:choose>
</xsl:when>
<xsl:otherwise>
<!--Optional slot does not match: try next slot (if any) with this morph-->
<xsl:if test="$morph">
<xsl:call-template name="InflectionalTemplate102SuffixSlot103">
<xsl:with-param name="morph" select="$morph/following-sibling::morph[1]" />
</xsl:call-template>
</xsl:if>
</xsl:otherwise>
</xsl:choose>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
InflectionalTemplate156
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="InflectionalTemplate156">
<seq step="Try to apply inflectional template 'Intransitive verb'.  The result will be a Full analysis node.">
<xsl:variable name="PrefixSlotMorphs">
<xsl:call-template name="InflectionalTemplate156PrefixSlot181">
<xsl:with-param name="morph" select="preceding-sibling::*[1]" />
</xsl:call-template>
</xsl:variable>
<xsl:variable name="iPrefixSlotMorphs">
<xsl:choose>
<xsl:when test="not(contains($PrefixSlotMorphs, ' '))">
<xsl:value-of select="string-length($PrefixSlotMorphs)" />
</xsl:when>
<xsl:otherwise>0</xsl:otherwise>
</xsl:choose>
</xsl:variable>
<xsl:variable name="SuffixSlotMorphs">
<xsl:call-template name="InflectionalTemplate156SuffixSlot180">
<xsl:with-param name="morph" select="following-sibling::*[1]" />
</xsl:call-template>
</xsl:variable>
<xsl:variable name="iSuffixSlotMorphs">
<xsl:choose>
<xsl:when test="not(contains($SuffixSlotMorphs, ' '))">
<xsl:value-of select="string-length($SuffixSlotMorphs)" />
</xsl:when>
<xsl:otherwise>0</xsl:otherwise>
</xsl:choose>
</xsl:variable>
<!-- copy what's before the template -->
<xsl:for-each select="preceding-sibling::*[position()&gt;$iPrefixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
<!--have a full-->
<full>
<xsl:attribute name="syncat">
<xsl:value-of select="@syncat" />
</xsl:attribute>
<xsl:attribute name="syncatAbbr">
<xsl:value-of select="@syncatAbbr" />
</xsl:attribute>
<xsl:attribute name="requiresInfl">
<xsl:text>-</xsl:text>
</xsl:attribute>
<xsl:attribute name="inflected">
<xsl:text>+</xsl:text>
</xsl:attribute>
<!--check category of the stem-->
<xsl:variable name="sCategoriesAreCompatible">
<xsl:call-template name="TestCompatibleCategories">
<xsl:with-param name="sFirstCat">153</xsl:with-param>
<xsl:with-param name="sSecondCat">
<xsl:value-of select="@syncat" />
</xsl:with-param>
</xsl:call-template>
</xsl:variable>
<xsl:if test="$sCategoriesAreCompatible != $sSuccess">
<xsl:call-template name="ReportFailure">
<xsl:with-param name="sFirstItemComponent">
<xsl:text> category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemComponentAbbr">intrans</xsl:with-param>
<xsl:with-param name="sFirstItemDescription">
<xsl:text>inflectional template</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemValue">Intransitive verb</xsl:with-param>
<xsl:with-param name="sSecondItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sSecondItemComponentAbbr" select="@syncatAbbr" />
<xsl:with-param name="sSecondItemDescription">
<xsl:text>stem</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:if>
<!--Check for productivity restrictions-->
<xsl:variable name="stem" select="." />
<xsl:if test="$iPrefixSlotMorphs &gt; 0">
<xsl:for-each select="preceding-sibling::*[position()&lt;=$iPrefixSlotMorphs]">
<xsl:call-template name="CheckInflectionalAffixProductivityRestrictions">
<xsl:with-param name="stemMorph" select="$stem" />
<xsl:with-param name="inflMorph" select="." />
<xsl:with-param name="sType" select="'prefix'" />
</xsl:call-template>
</xsl:for-each>
</xsl:if>
<xsl:if test="$iSuffixSlotMorphs &gt; 0">
<xsl:for-each select="following-sibling::*[position()&lt;=$iSuffixSlotMorphs]">
<xsl:call-template name="CheckInflectionalAffixProductivityRestrictions">
<xsl:with-param name="stemMorph" select="$stem" />
<xsl:with-param name="inflMorph" select="." />
<xsl:with-param name="sType" select="'suffix'" />
</xsl:call-template>
</xsl:for-each>
</xsl:if>
<!--Percolate features-->
<!--Check compatitbility of all inflection features-->
<xsl:variable name="InflFeaturesToPercolate">
<xsl:variable name="PrefixSlotsUnification">
<xsl:choose>
<xsl:when test="$iPrefixSlotMorphs &gt; 1">
<xsl:call-template name="UnifyPrefixSlots">
<xsl:with-param name="PreviousResult" />
<xsl:with-param name="PrefixSlot" select="preceding-sibling::*[1]" />
<xsl:with-param name="iRemaining" select="$iPrefixSlotMorphs - 1" />
<xsl:with-param name="sRuleInfo">The inflectional template named 'Intransitive verb' for category 'intransitive verb'</xsl:with-param>
</xsl:call-template>
</xsl:when>
<xsl:when test="$iPrefixSlotMorphs=1">
<xsl:variable name="InflectionalPrefix" select="preceding-sibling::*[1]" />
<xsl:call-template name="UnifyTwoFeatureStructures">
<xsl:with-param name="FirstFS" select="fs" />
<xsl:with-param name="SecondFS" select="$InflectionalPrefix/inflMsa/fs" />
<xsl:with-param name="sTopLevelId" select="fs/@id" />
<xsl:with-param name="sRuleInfo">The inflectional template named 'Intransitive verb' for category 'intransitive verb'</xsl:with-param>
<xsl:with-param name="sFirstDescription" select="'stem'" />
<xsl:with-param name="sSecondDescription">
<xsl:text>inflectional prefix (</xsl:text>
<xsl:value-of select="$InflectionalPrefix/shortName" />
<xsl:text>)</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:when>
</xsl:choose>
</xsl:variable>
<xsl:variable name="SuffixSlotsUnification">
<xsl:choose>
<xsl:when test="$iSuffixSlotMorphs &gt; 1">
<xsl:call-template name="UnifySuffixSlots">
<xsl:with-param name="PreviousResult" />
<xsl:with-param name="SuffixSlot" select="following-sibling::*[1]" />
<xsl:with-param name="iRemaining" select="$iSuffixSlotMorphs - 1" />
<xsl:with-param name="sRuleInfo">The inflectional template named 'Intransitive verb' for category 'intransitive verb'</xsl:with-param>
</xsl:call-template>
</xsl:when>
<xsl:when test="$iSuffixSlotMorphs=1">
<xsl:variable name="InflectionalSuffix" select="following-sibling::*[1]" />
<xsl:call-template name="UnifyTwoFeatureStructures">
<xsl:with-param name="FirstFS" select="fs" />
<xsl:with-param name="SecondFS" select="$InflectionalSuffix/inflMsa/fs" />
<xsl:with-param name="sTopLevelId" select="fs/@id" />
<xsl:with-param name="sRuleInfo">The inflectional template named 'Intransitive verb' for category 'intransitive verb'</xsl:with-param>
<xsl:with-param name="sFirstDescription" select="'stem'" />
<xsl:with-param name="sSecondDescription">
<xsl:text>inflectional suffix (</xsl:text>
<xsl:value-of select="$InflectionalSuffix/shortName" />
<xsl:text>)</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:when>
</xsl:choose>
</xsl:variable>
<xsl:variable name="PrefixSuffixSlotsUnification">
<xsl:choose>
<xsl:when test="string-length($PrefixSlotsUnification) &gt; 0 and string-length($SuffixSlotsUnification) &gt; 0">
<xsl:call-template name="UnifyTwoFeatureStructures">
<xsl:with-param name="FirstFS" select="msxsl:node-set($PrefixSlotsUnification)/fs" />
<xsl:with-param name="SecondFS" select="msxsl:node-set($SuffixSlotsUnification)/fs" />
<xsl:with-param name="sTopLevelId" select="msxsl:node-set($PrefixSlotsUnification)/fs/@id" />
<xsl:with-param name="sRuleInfo">The inflectional template named 'Intransitive verb' for category 'intransitive verb'</xsl:with-param>
<xsl:with-param name="sFirstDescription">
<xsl:value-of select="'unification of all the prefix slots'" />
</xsl:with-param>
<xsl:with-param name="sSecondDescription">
<xsl:value-of select="'unification of all the suffix slots'" />
</xsl:with-param>
</xsl:call-template>
</xsl:when>
<xsl:when test="string-length($PrefixSlotsUnification) &gt; 0">
<xsl:copy-of select="$PrefixSlotsUnification" />
</xsl:when>
<xsl:when test="string-length($SuffixSlotsUnification) &gt; 0">
<xsl:copy-of select="$SuffixSlotsUnification" />
</xsl:when>
</xsl:choose>
</xsl:variable>
<xsl:call-template name="UnifyTwoFeatureStructures">
<xsl:with-param name="FirstFS" select="fs" />
<xsl:with-param name="SecondFS" select="msxsl:node-set($PrefixSuffixSlotsUnification)/fs" />
<xsl:with-param name="sTopLevelId" select="fs/@id" />
<xsl:with-param name="sRuleInfo">The inflectional template named 'Intransitive verb' for category 'intransitive verb'</xsl:with-param>
<xsl:with-param name="sFirstDescription">
<xsl:value-of select="'stem'" />
</xsl:with-param>
<xsl:with-param name="sSecondDescription">
<xsl:value-of select="'unification of all the slots'" />
</xsl:with-param>
</xsl:call-template>
</xsl:variable>
<xsl:if test="msxsl:node-set($InflFeaturesToPercolate)/fs/descendant::feature">
<xsl:copy-of select="$InflFeaturesToPercolate" />
</xsl:if>
<xsl:variable name="StemNameConstraintResult">
<xsl:call-template name="CheckStemNames153">
<xsl:with-param name="stemName" select="stemName/@id" />
<xsl:with-param name="InflFeatures" select="msxsl:node-set($InflFeaturesToPercolate)" />
</xsl:call-template>
</xsl:variable>
<xsl:if test="msxsl:node-set($StemNameConstraintResult)/failure">
<xsl:copy-of select="msxsl:node-set($StemNameConstraintResult)" />
</xsl:if>
<xsl:if test="@blocksInflection!='-'">
<failure>The inflectional template named 'Intransitive verb' for category 'intransitive verb' failed because the stem was built by a non-final template and there was no intervening derivation or compounding.</failure>
</xsl:if>
<xsl:if test="name()='partial' and @inflected='+'">
<failure>Partial inflectional template has already been inflected.</failure>
</xsl:if>
<!--copy any inflectional prefixes-->
<xsl:choose>
<xsl:when test="$iPrefixSlotMorphs &gt; 0">
<xsl:for-each select="preceding-sibling::*[position()&lt;=$iPrefixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</xsl:when>
<xsl:otherwise>
<!-- output any failure nodes -->
<xsl:copy-of select="$PrefixSlotMorphs" />
</xsl:otherwise>
</xsl:choose>
<!--copy the stem-->
<xsl:copy-of select="." />
<!--copy any inflectional suffixes-->
<xsl:choose>
<xsl:when test="$iSuffixSlotMorphs &gt; 0">
<xsl:for-each select="following-sibling::*[position()&lt;=$iSuffixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</xsl:when>
<xsl:otherwise>
<!-- output any failure nodes -->
<xsl:copy-of select="$SuffixSlotMorphs" />
</xsl:otherwise>
</xsl:choose>
</full>
<!-- copy what's after the template -->
<xsl:for-each select="following-sibling::*[position()&gt;$iSuffixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</seq>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
PartialInflectionalTemplate156POS153
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="PartialInflectionalTemplate156POS153">
<seq step="Try to apply inflectional template 'Intransitive verb' on a Partial analysis node.  Apply it for category 'intransitive verb'. The result will be another Partial analysis node.">
<xsl:variable name="PrefixSlotMorphs">
<xsl:call-template name="InflectionalTemplate156PrefixSlot181">
<xsl:with-param name="morph" select="preceding-sibling::*[1]" />
</xsl:call-template>
</xsl:variable>
<xsl:variable name="iPrefixSlotMorphs">
<xsl:choose>
<xsl:when test="not(contains($PrefixSlotMorphs, ' '))">
<xsl:value-of select="string-length($PrefixSlotMorphs)" />
</xsl:when>
<xsl:otherwise>0</xsl:otherwise>
</xsl:choose>
</xsl:variable>
<xsl:variable name="SuffixSlotMorphs">
<xsl:call-template name="InflectionalTemplate156SuffixSlot180">
<xsl:with-param name="morph" select="following-sibling::*[1]" />
</xsl:call-template>
</xsl:variable>
<xsl:variable name="iSuffixSlotMorphs">
<xsl:choose>
<xsl:when test="not(contains($SuffixSlotMorphs, ' '))">
<xsl:value-of select="string-length($SuffixSlotMorphs)" />
</xsl:when>
<xsl:otherwise>0</xsl:otherwise>
</xsl:choose>
</xsl:variable>
<!-- copy what's before the template -->
<xsl:for-each select="preceding-sibling::*[position()&gt;$iPrefixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
<partial>
<xsl:attribute name="syncat">
<xsl:text>153</xsl:text>
</xsl:attribute>
<xsl:attribute name="syncatAbbr">
<xsl:text>intrans</xsl:text>
</xsl:attribute>
<xsl:attribute name="requiresInfl">
<xsl:text>-</xsl:text>
</xsl:attribute>
<xsl:attribute name="inflected">
<xsl:text>+</xsl:text>
</xsl:attribute>
<xsl:if test="@blocksInflection!='-'">
<failure>The inflectional template named 'Intransitive verb' for category 'intransitive verb' failed because the stem was built by a non-final template and there was no intervening derivation or compounding.</failure>
</xsl:if>
<xsl:if test="name()='partial' and @inflected='+'">
<failure>Partial inflectional template has already been inflected.</failure>
</xsl:if>
<!--copy any inflectional prefixes-->
<xsl:choose>
<xsl:when test="$iPrefixSlotMorphs &gt; 0">
<xsl:for-each select="preceding-sibling::*[position()&lt;=$iPrefixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</xsl:when>
<xsl:otherwise>
<!-- output any failure nodes -->
<xsl:copy-of select="$PrefixSlotMorphs" />
</xsl:otherwise>
</xsl:choose>
<!--copy the stem-->
<xsl:copy-of select="." />
<!--copy any inflectional suffixes-->
<xsl:choose>
<xsl:when test="$iSuffixSlotMorphs &gt; 0">
<xsl:for-each select="following-sibling::*[position()&lt;=$iSuffixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</xsl:when>
<xsl:otherwise>
<!-- output any failure nodes -->
<xsl:copy-of select="$SuffixSlotMorphs" />
</xsl:otherwise>
</xsl:choose>
</partial>
<!-- copy what's after the template -->
<xsl:for-each select="following-sibling::*[position()&gt;$iSuffixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</seq>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
InflectionalTemplate156PrefixSlot181
	Params: morph - morpheme to check
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="InflectionalTemplate156PrefixSlot181">
<xsl:param name="morph" />
<xsl:choose>
<xsl:when test="$morph/@wordType = '181'">
<!--Optional slot matches: if it is compatible, try next slot (if any) with next morph-->
<!--Check inflection class compatibility-->
<xsl:variable name="sStemInflClass">
<xsl:value-of select="@inflClass" />
</xsl:variable>
<xsl:choose>
<xsl:when test="$morph/inflClasses">
<xsl:choose>
<xsl:when test="$morph/inflClasses[inflClass/@hvo=$sStemInflClass] or $sStemInflClass=''">
<xsl:call-template name="IndicateInflAffixSucceeded" />
</xsl:when>
<xsl:otherwise>
<failure>The inflectional template named 'Intransitive verb' for category 'intransitive verb'<xsl:text> failed because in the </xsl:text>optional prefix<xsl:text> slot '</xsl:text>Subject<xsl:text>', the inflection class of the stem (</xsl:text>
<xsl:value-of select="@inflClassAbbr" />
<xsl:text>) does not match any of the inflection classes of the inflectional affix (</xsl:text>
<xsl:value-of select="$morph/shortName" />
<xsl:text>).  The inflection class</xsl:text>
<xsl:choose>
<xsl:when test="count($morph/inflClasses/inflClass)=1">
<xsl:text> of this affix is: </xsl:text>
</xsl:when>
<xsl:otherwise>
<xsl:text>es of this affix are: </xsl:text>
</xsl:otherwise>
</xsl:choose>
<xsl:for-each select="$morph/inflClasses/inflClass">
<xsl:value-of select="@abbr" />
<xsl:if test="position() != last()">
<xsl:text>, </xsl:text>
</xsl:if>
</xsl:for-each>
<xsl:text>.</xsl:text>
</failure>
</xsl:otherwise>
</xsl:choose>
</xsl:when>
<xsl:otherwise>
<!--No inflection classes to check; indicate success and try next slot (if any)-->
<xsl:call-template name="IndicateInflAffixSucceeded" />
</xsl:otherwise>
</xsl:choose>
</xsl:when>
<xsl:otherwise>
<!--Optional slot does not match: try next slot (if any) with this morph-->
<xsl:if test="$morph" />
</xsl:otherwise>
</xsl:choose>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
InflectionalTemplate156SuffixSlot180
	Params: morph - morpheme to check
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="InflectionalTemplate156SuffixSlot180">
<xsl:param name="morph" />
<xsl:choose>
<xsl:when test="$morph/@wordType = '180'">
<!--Required slot matches: if it is compatible, try next slot (if any) with next morph-->
<!--Check inflection class compatibility-->
<xsl:variable name="sStemInflClass">
<xsl:value-of select="@inflClass" />
</xsl:variable>
<xsl:choose>
<xsl:when test="$morph/inflClasses">
<xsl:choose>
<xsl:when test="$morph/inflClasses[inflClass/@hvo=$sStemInflClass] or $sStemInflClass=''">
<xsl:call-template name="IndicateInflAffixSucceeded" />
</xsl:when>
<xsl:otherwise>
<failure>The inflectional template named 'Intransitive verb' for category 'intransitive verb'<xsl:text> failed because in the </xsl:text>required suffix<xsl:text> slot '</xsl:text>Tense<xsl:text>', the inflection class of the stem (</xsl:text>
<xsl:value-of select="@inflClassAbbr" />
<xsl:text>) does not match any of the inflection classes of the inflectional affix (</xsl:text>
<xsl:value-of select="$morph/shortName" />
<xsl:text>).  The inflection class</xsl:text>
<xsl:choose>
<xsl:when test="count($morph/inflClasses/inflClass)=1">
<xsl:text> of this affix is: </xsl:text>
</xsl:when>
<xsl:otherwise>
<xsl:text>es of this affix are: </xsl:text>
</xsl:otherwise>
</xsl:choose>
<xsl:for-each select="$morph/inflClasses/inflClass">
<xsl:value-of select="@abbr" />
<xsl:if test="position() != last()">
<xsl:text>, </xsl:text>
</xsl:if>
</xsl:for-each>
<xsl:text>.</xsl:text>
</failure>
</xsl:otherwise>
</xsl:choose>
</xsl:when>
<xsl:otherwise>
<!--No inflection classes to check; indicate success and try next slot (if any)-->
<xsl:call-template name="IndicateInflAffixSucceeded" />
</xsl:otherwise>
</xsl:choose>
</xsl:when>
<xsl:otherwise>
<!--Required slot does not match: give failure notice-->
<failure>The inflectional template named 'Intransitive verb' for category 'intransitive verb' failed because the required suffix slot 'Tense' was not found.</failure>
</xsl:otherwise>
</xsl:choose>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
InflectionalTemplate166
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="InflectionalTemplate166">
<seq step="Try to apply inflectional template 'Stative verb'.  The result will be a Full analysis node.">
<xsl:variable name="PrefixSlotMorphs">
<xsl:call-template name="InflectionalTemplate166PrefixSlot181">
<xsl:with-param name="morph" select="preceding-sibling::*[1]" />
</xsl:call-template>
</xsl:variable>
<xsl:variable name="iPrefixSlotMorphs">
<xsl:choose>
<xsl:when test="not(contains($PrefixSlotMorphs, ' '))">
<xsl:value-of select="string-length($PrefixSlotMorphs)" />
</xsl:when>
<xsl:otherwise>0</xsl:otherwise>
</xsl:choose>
</xsl:variable>
<xsl:variable name="SuffixSlotMorphs">
<xsl:call-template name="InflectionalTemplate166SuffixSlot167">
<xsl:with-param name="morph" select="following-sibling::*[1]" />
</xsl:call-template>
</xsl:variable>
<xsl:variable name="iSuffixSlotMorphs">
<xsl:choose>
<xsl:when test="not(contains($SuffixSlotMorphs, ' '))">
<xsl:value-of select="string-length($SuffixSlotMorphs)" />
</xsl:when>
<xsl:otherwise>0</xsl:otherwise>
</xsl:choose>
</xsl:variable>
<!-- copy what's before the template -->
<xsl:for-each select="preceding-sibling::*[position()&gt;$iPrefixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
<!--have a full-->
<full>
<xsl:attribute name="syncat">
<xsl:value-of select="@syncat" />
</xsl:attribute>
<xsl:attribute name="syncatAbbr">
<xsl:value-of select="@syncatAbbr" />
</xsl:attribute>
<xsl:attribute name="requiresInfl">
<xsl:text>-</xsl:text>
</xsl:attribute>
<xsl:attribute name="inflected">
<xsl:text>+</xsl:text>
</xsl:attribute>
<!--check category of the stem-->
<xsl:variable name="sCategoriesAreCompatible">
<xsl:call-template name="TestCompatibleCategories">
<xsl:with-param name="sFirstCat">163</xsl:with-param>
<xsl:with-param name="sSecondCat">
<xsl:value-of select="@syncat" />
</xsl:with-param>
</xsl:call-template>
</xsl:variable>
<xsl:if test="$sCategoriesAreCompatible != $sSuccess">
<xsl:call-template name="ReportFailure">
<xsl:with-param name="sFirstItemComponent">
<xsl:text> category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemComponentAbbr">sta</xsl:with-param>
<xsl:with-param name="sFirstItemDescription">
<xsl:text>inflectional template</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemValue">Stative verb</xsl:with-param>
<xsl:with-param name="sSecondItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sSecondItemComponentAbbr" select="@syncatAbbr" />
<xsl:with-param name="sSecondItemDescription">
<xsl:text>stem</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:if>
<!--Check for productivity restrictions-->
<xsl:variable name="stem" select="." />
<xsl:if test="$iPrefixSlotMorphs &gt; 0">
<xsl:for-each select="preceding-sibling::*[position()&lt;=$iPrefixSlotMorphs]">
<xsl:call-template name="CheckInflectionalAffixProductivityRestrictions">
<xsl:with-param name="stemMorph" select="$stem" />
<xsl:with-param name="inflMorph" select="." />
<xsl:with-param name="sType" select="'prefix'" />
</xsl:call-template>
</xsl:for-each>
</xsl:if>
<xsl:if test="$iSuffixSlotMorphs &gt; 0">
<xsl:for-each select="following-sibling::*[position()&lt;=$iSuffixSlotMorphs]">
<xsl:call-template name="CheckInflectionalAffixProductivityRestrictions">
<xsl:with-param name="stemMorph" select="$stem" />
<xsl:with-param name="inflMorph" select="." />
<xsl:with-param name="sType" select="'suffix'" />
</xsl:call-template>
</xsl:for-each>
</xsl:if>
<!--Percolate features-->
<!--Check compatitbility of all inflection features-->
<xsl:variable name="InflFeaturesToPercolate">
<xsl:variable name="PrefixSlotsUnification">
<xsl:choose>
<xsl:when test="$iPrefixSlotMorphs &gt; 1">
<xsl:call-template name="UnifyPrefixSlots">
<xsl:with-param name="PreviousResult" />
<xsl:with-param name="PrefixSlot" select="preceding-sibling::*[1]" />
<xsl:with-param name="iRemaining" select="$iPrefixSlotMorphs - 1" />
<xsl:with-param name="sRuleInfo">The inflectional template named 'Stative verb' for category 'stative'</xsl:with-param>
</xsl:call-template>
</xsl:when>
<xsl:when test="$iPrefixSlotMorphs=1">
<xsl:variable name="InflectionalPrefix" select="preceding-sibling::*[1]" />
<xsl:call-template name="UnifyTwoFeatureStructures">
<xsl:with-param name="FirstFS" select="fs" />
<xsl:with-param name="SecondFS" select="$InflectionalPrefix/inflMsa/fs" />
<xsl:with-param name="sTopLevelId" select="fs/@id" />
<xsl:with-param name="sRuleInfo">The inflectional template named 'Stative verb' for category 'stative'</xsl:with-param>
<xsl:with-param name="sFirstDescription" select="'stem'" />
<xsl:with-param name="sSecondDescription">
<xsl:text>inflectional prefix (</xsl:text>
<xsl:value-of select="$InflectionalPrefix/shortName" />
<xsl:text>)</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:when>
</xsl:choose>
</xsl:variable>
<xsl:variable name="SuffixSlotsUnification">
<xsl:choose>
<xsl:when test="$iSuffixSlotMorphs &gt; 1">
<xsl:call-template name="UnifySuffixSlots">
<xsl:with-param name="PreviousResult" />
<xsl:with-param name="SuffixSlot" select="following-sibling::*[1]" />
<xsl:with-param name="iRemaining" select="$iSuffixSlotMorphs - 1" />
<xsl:with-param name="sRuleInfo">The inflectional template named 'Stative verb' for category 'stative'</xsl:with-param>
</xsl:call-template>
</xsl:when>
<xsl:when test="$iSuffixSlotMorphs=1">
<xsl:variable name="InflectionalSuffix" select="following-sibling::*[1]" />
<xsl:call-template name="UnifyTwoFeatureStructures">
<xsl:with-param name="FirstFS" select="fs" />
<xsl:with-param name="SecondFS" select="$InflectionalSuffix/inflMsa/fs" />
<xsl:with-param name="sTopLevelId" select="fs/@id" />
<xsl:with-param name="sRuleInfo">The inflectional template named 'Stative verb' for category 'stative'</xsl:with-param>
<xsl:with-param name="sFirstDescription" select="'stem'" />
<xsl:with-param name="sSecondDescription">
<xsl:text>inflectional suffix (</xsl:text>
<xsl:value-of select="$InflectionalSuffix/shortName" />
<xsl:text>)</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:when>
</xsl:choose>
</xsl:variable>
<xsl:variable name="PrefixSuffixSlotsUnification">
<xsl:choose>
<xsl:when test="string-length($PrefixSlotsUnification) &gt; 0 and string-length($SuffixSlotsUnification) &gt; 0">
<xsl:call-template name="UnifyTwoFeatureStructures">
<xsl:with-param name="FirstFS" select="msxsl:node-set($PrefixSlotsUnification)/fs" />
<xsl:with-param name="SecondFS" select="msxsl:node-set($SuffixSlotsUnification)/fs" />
<xsl:with-param name="sTopLevelId" select="msxsl:node-set($PrefixSlotsUnification)/fs/@id" />
<xsl:with-param name="sRuleInfo">The inflectional template named 'Stative verb' for category 'stative'</xsl:with-param>
<xsl:with-param name="sFirstDescription">
<xsl:value-of select="'unification of all the prefix slots'" />
</xsl:with-param>
<xsl:with-param name="sSecondDescription">
<xsl:value-of select="'unification of all the suffix slots'" />
</xsl:with-param>
</xsl:call-template>
</xsl:when>
<xsl:when test="string-length($PrefixSlotsUnification) &gt; 0">
<xsl:copy-of select="$PrefixSlotsUnification" />
</xsl:when>
<xsl:when test="string-length($SuffixSlotsUnification) &gt; 0">
<xsl:copy-of select="$SuffixSlotsUnification" />
</xsl:when>
</xsl:choose>
</xsl:variable>
<xsl:call-template name="UnifyTwoFeatureStructures">
<xsl:with-param name="FirstFS" select="fs" />
<xsl:with-param name="SecondFS" select="msxsl:node-set($PrefixSuffixSlotsUnification)/fs" />
<xsl:with-param name="sTopLevelId" select="fs/@id" />
<xsl:with-param name="sRuleInfo">The inflectional template named 'Stative verb' for category 'stative'</xsl:with-param>
<xsl:with-param name="sFirstDescription">
<xsl:value-of select="'stem'" />
</xsl:with-param>
<xsl:with-param name="sSecondDescription">
<xsl:value-of select="'unification of all the slots'" />
</xsl:with-param>
</xsl:call-template>
</xsl:variable>
<xsl:if test="msxsl:node-set($InflFeaturesToPercolate)/fs/descendant::feature">
<xsl:copy-of select="$InflFeaturesToPercolate" />
</xsl:if>
<xsl:variable name="StemNameConstraintResult">
<xsl:call-template name="CheckStemNames163">
<xsl:with-param name="stemName" select="stemName/@id" />
<xsl:with-param name="InflFeatures" select="msxsl:node-set($InflFeaturesToPercolate)" />
</xsl:call-template>
</xsl:variable>
<xsl:if test="msxsl:node-set($StemNameConstraintResult)/failure">
<xsl:copy-of select="msxsl:node-set($StemNameConstraintResult)" />
</xsl:if>
<xsl:if test="@blocksInflection!='-'">
<failure>The inflectional template named 'Stative verb' for category 'stative' failed because the stem was built by a non-final template and there was no intervening derivation or compounding.</failure>
</xsl:if>
<xsl:if test="name()='partial' and @inflected='+'">
<failure>Partial inflectional template has already been inflected.</failure>
</xsl:if>
<!--copy any inflectional prefixes-->
<xsl:choose>
<xsl:when test="$iPrefixSlotMorphs &gt; 0">
<xsl:for-each select="preceding-sibling::*[position()&lt;=$iPrefixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</xsl:when>
<xsl:otherwise>
<!-- output any failure nodes -->
<xsl:copy-of select="$PrefixSlotMorphs" />
</xsl:otherwise>
</xsl:choose>
<!--copy the stem-->
<xsl:copy-of select="." />
<!--copy any inflectional suffixes-->
<xsl:choose>
<xsl:when test="$iSuffixSlotMorphs &gt; 0">
<xsl:for-each select="following-sibling::*[position()&lt;=$iSuffixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</xsl:when>
<xsl:otherwise>
<!-- output any failure nodes -->
<xsl:copy-of select="$SuffixSlotMorphs" />
</xsl:otherwise>
</xsl:choose>
</full>
<!-- copy what's after the template -->
<xsl:for-each select="following-sibling::*[position()&gt;$iSuffixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</seq>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
PartialInflectionalTemplate166POS163
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="PartialInflectionalTemplate166POS163">
<seq step="Try to apply inflectional template 'Stative verb' on a Partial analysis node.  Apply it for category 'stative'. The result will be another Partial analysis node.">
<xsl:variable name="PrefixSlotMorphs">
<xsl:call-template name="InflectionalTemplate166PrefixSlot181">
<xsl:with-param name="morph" select="preceding-sibling::*[1]" />
</xsl:call-template>
</xsl:variable>
<xsl:variable name="iPrefixSlotMorphs">
<xsl:choose>
<xsl:when test="not(contains($PrefixSlotMorphs, ' '))">
<xsl:value-of select="string-length($PrefixSlotMorphs)" />
</xsl:when>
<xsl:otherwise>0</xsl:otherwise>
</xsl:choose>
</xsl:variable>
<xsl:variable name="SuffixSlotMorphs">
<xsl:call-template name="InflectionalTemplate166SuffixSlot167">
<xsl:with-param name="morph" select="following-sibling::*[1]" />
</xsl:call-template>
</xsl:variable>
<xsl:variable name="iSuffixSlotMorphs">
<xsl:choose>
<xsl:when test="not(contains($SuffixSlotMorphs, ' '))">
<xsl:value-of select="string-length($SuffixSlotMorphs)" />
</xsl:when>
<xsl:otherwise>0</xsl:otherwise>
</xsl:choose>
</xsl:variable>
<!-- copy what's before the template -->
<xsl:for-each select="preceding-sibling::*[position()&gt;$iPrefixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
<partial>
<xsl:attribute name="syncat">
<xsl:text>163</xsl:text>
</xsl:attribute>
<xsl:attribute name="syncatAbbr">
<xsl:text>sta</xsl:text>
</xsl:attribute>
<xsl:attribute name="requiresInfl">
<xsl:text>-</xsl:text>
</xsl:attribute>
<xsl:attribute name="inflected">
<xsl:text>+</xsl:text>
</xsl:attribute>
<xsl:if test="@blocksInflection!='-'">
<failure>The inflectional template named 'Stative verb' for category 'stative' failed because the stem was built by a non-final template and there was no intervening derivation or compounding.</failure>
</xsl:if>
<xsl:if test="name()='partial' and @inflected='+'">
<failure>Partial inflectional template has already been inflected.</failure>
</xsl:if>
<!--copy any inflectional prefixes-->
<xsl:choose>
<xsl:when test="$iPrefixSlotMorphs &gt; 0">
<xsl:for-each select="preceding-sibling::*[position()&lt;=$iPrefixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</xsl:when>
<xsl:otherwise>
<!-- output any failure nodes -->
<xsl:copy-of select="$PrefixSlotMorphs" />
</xsl:otherwise>
</xsl:choose>
<!--copy the stem-->
<xsl:copy-of select="." />
<!--copy any inflectional suffixes-->
<xsl:choose>
<xsl:when test="$iSuffixSlotMorphs &gt; 0">
<xsl:for-each select="following-sibling::*[position()&lt;=$iSuffixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</xsl:when>
<xsl:otherwise>
<!-- output any failure nodes -->
<xsl:copy-of select="$SuffixSlotMorphs" />
</xsl:otherwise>
</xsl:choose>
</partial>
<!-- copy what's after the template -->
<xsl:for-each select="following-sibling::*[position()&gt;$iSuffixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</seq>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
InflectionalTemplate166PrefixSlot181
	Params: morph - morpheme to check
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="InflectionalTemplate166PrefixSlot181">
<xsl:param name="morph" />
<xsl:choose>
<xsl:when test="$morph/@wordType = '181'">
<!--Optional slot matches: if it is compatible, try next slot (if any) with next morph-->
<!--Check inflection class compatibility-->
<xsl:variable name="sStemInflClass">
<xsl:value-of select="@inflClass" />
</xsl:variable>
<xsl:choose>
<xsl:when test="$morph/inflClasses">
<xsl:choose>
<xsl:when test="$morph/inflClasses[inflClass/@hvo=$sStemInflClass] or $sStemInflClass=''">
<xsl:call-template name="IndicateInflAffixSucceeded" />
</xsl:when>
<xsl:otherwise>
<failure>The inflectional template named 'Stative verb' for category 'stative'<xsl:text> failed because in the </xsl:text>optional prefix<xsl:text> slot '</xsl:text>Subject<xsl:text>', the inflection class of the stem (</xsl:text>
<xsl:value-of select="@inflClassAbbr" />
<xsl:text>) does not match any of the inflection classes of the inflectional affix (</xsl:text>
<xsl:value-of select="$morph/shortName" />
<xsl:text>).  The inflection class</xsl:text>
<xsl:choose>
<xsl:when test="count($morph/inflClasses/inflClass)=1">
<xsl:text> of this affix is: </xsl:text>
</xsl:when>
<xsl:otherwise>
<xsl:text>es of this affix are: </xsl:text>
</xsl:otherwise>
</xsl:choose>
<xsl:for-each select="$morph/inflClasses/inflClass">
<xsl:value-of select="@abbr" />
<xsl:if test="position() != last()">
<xsl:text>, </xsl:text>
</xsl:if>
</xsl:for-each>
<xsl:text>.</xsl:text>
</failure>
</xsl:otherwise>
</xsl:choose>
</xsl:when>
<xsl:otherwise>
<!--No inflection classes to check; indicate success and try next slot (if any)-->
<xsl:call-template name="IndicateInflAffixSucceeded" />
</xsl:otherwise>
</xsl:choose>
</xsl:when>
<xsl:otherwise>
<!--Optional slot does not match: try next slot (if any) with this morph-->
<xsl:if test="$morph" />
</xsl:otherwise>
</xsl:choose>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
InflectionalTemplate166SuffixSlot167
	Params: morph - morpheme to check
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="InflectionalTemplate166SuffixSlot167">
<xsl:param name="morph" />
<xsl:choose>
<xsl:when test="$morph/@wordType = '167'">
<!--Required slot matches: if it is compatible, try next slot (if any) with next morph-->
<!--Check inflection class compatibility-->
<xsl:variable name="sStemInflClass">
<xsl:value-of select="@inflClass" />
</xsl:variable>
<xsl:choose>
<xsl:when test="$morph/inflClasses">
<xsl:choose>
<xsl:when test="$morph/inflClasses[inflClass/@hvo=$sStemInflClass] or $sStemInflClass=''">
<xsl:call-template name="IndicateInflAffixSucceeded" />
</xsl:when>
<xsl:otherwise>
<failure>The inflectional template named 'Stative verb' for category 'stative'<xsl:text> failed because in the </xsl:text>required suffix<xsl:text> slot '</xsl:text>Tense<xsl:text>', the inflection class of the stem (</xsl:text>
<xsl:value-of select="@inflClassAbbr" />
<xsl:text>) does not match any of the inflection classes of the inflectional affix (</xsl:text>
<xsl:value-of select="$morph/shortName" />
<xsl:text>).  The inflection class</xsl:text>
<xsl:choose>
<xsl:when test="count($morph/inflClasses/inflClass)=1">
<xsl:text> of this affix is: </xsl:text>
</xsl:when>
<xsl:otherwise>
<xsl:text>es of this affix are: </xsl:text>
</xsl:otherwise>
</xsl:choose>
<xsl:for-each select="$morph/inflClasses/inflClass">
<xsl:value-of select="@abbr" />
<xsl:if test="position() != last()">
<xsl:text>, </xsl:text>
</xsl:if>
</xsl:for-each>
<xsl:text>.</xsl:text>
</failure>
</xsl:otherwise>
</xsl:choose>
</xsl:when>
<xsl:otherwise>
<!--No inflection classes to check; indicate success and try next slot (if any)-->
<xsl:call-template name="IndicateInflAffixSucceeded" />
</xsl:otherwise>
</xsl:choose>
</xsl:when>
<xsl:otherwise>
<!--Required slot does not match: give failure notice-->
<failure>The inflectional template named 'Stative verb' for category 'stative' failed because the required suffix slot 'Tense' was not found.</failure>
</xsl:otherwise>
</xsl:choose>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
InflectionalTemplate171
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="InflectionalTemplate171">
<seq step="Try to apply inflectional template 'Transitive verb'.  The result will be a Full analysis node.">
<xsl:variable name="PrefixSlotMorphs">
<xsl:call-template name="InflectionalTemplate171PrefixSlot182">
<xsl:with-param name="morph" select="preceding-sibling::*[1]" />
</xsl:call-template>
</xsl:variable>
<xsl:variable name="iPrefixSlotMorphs">
<xsl:choose>
<xsl:when test="not(contains($PrefixSlotMorphs, ' '))">
<xsl:value-of select="string-length($PrefixSlotMorphs)" />
</xsl:when>
<xsl:otherwise>0</xsl:otherwise>
</xsl:choose>
</xsl:variable>
<xsl:variable name="SuffixSlotMorphs">
<xsl:call-template name="InflectionalTemplate171SuffixSlot180">
<xsl:with-param name="morph" select="following-sibling::*[1]" />
</xsl:call-template>
</xsl:variable>
<xsl:variable name="iSuffixSlotMorphs">
<xsl:choose>
<xsl:when test="not(contains($SuffixSlotMorphs, ' '))">
<xsl:value-of select="string-length($SuffixSlotMorphs)" />
</xsl:when>
<xsl:otherwise>0</xsl:otherwise>
</xsl:choose>
</xsl:variable>
<!-- copy what's before the template -->
<xsl:for-each select="preceding-sibling::*[position()&gt;$iPrefixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
<!--have a full-->
<full>
<xsl:attribute name="syncat">
<xsl:value-of select="@syncat" />
</xsl:attribute>
<xsl:attribute name="syncatAbbr">
<xsl:value-of select="@syncatAbbr" />
</xsl:attribute>
<xsl:attribute name="requiresInfl">
<xsl:text>-</xsl:text>
</xsl:attribute>
<xsl:attribute name="inflected">
<xsl:text>+</xsl:text>
</xsl:attribute>
<!--check category of the stem-->
<xsl:variable name="sCategoriesAreCompatible">
<xsl:call-template name="TestCompatibleCategories">
<xsl:with-param name="sFirstCat">168</xsl:with-param>
<xsl:with-param name="sSecondCat">
<xsl:value-of select="@syncat" />
</xsl:with-param>
</xsl:call-template>
</xsl:variable>
<xsl:if test="$sCategoriesAreCompatible != $sSuccess">
<xsl:call-template name="ReportFailure">
<xsl:with-param name="sFirstItemComponent">
<xsl:text> category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemComponentAbbr">trans</xsl:with-param>
<xsl:with-param name="sFirstItemDescription">
<xsl:text>inflectional template</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemValue">Transitive verb</xsl:with-param>
<xsl:with-param name="sSecondItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sSecondItemComponentAbbr" select="@syncatAbbr" />
<xsl:with-param name="sSecondItemDescription">
<xsl:text>stem</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:if>
<!--Check for productivity restrictions-->
<xsl:variable name="stem" select="." />
<xsl:if test="$iPrefixSlotMorphs &gt; 0">
<xsl:for-each select="preceding-sibling::*[position()&lt;=$iPrefixSlotMorphs]">
<xsl:call-template name="CheckInflectionalAffixProductivityRestrictions">
<xsl:with-param name="stemMorph" select="$stem" />
<xsl:with-param name="inflMorph" select="." />
<xsl:with-param name="sType" select="'prefix'" />
</xsl:call-template>
</xsl:for-each>
</xsl:if>
<xsl:if test="$iSuffixSlotMorphs &gt; 0">
<xsl:for-each select="following-sibling::*[position()&lt;=$iSuffixSlotMorphs]">
<xsl:call-template name="CheckInflectionalAffixProductivityRestrictions">
<xsl:with-param name="stemMorph" select="$stem" />
<xsl:with-param name="inflMorph" select="." />
<xsl:with-param name="sType" select="'suffix'" />
</xsl:call-template>
</xsl:for-each>
</xsl:if>
<!--Percolate features-->
<!--Check compatitbility of all inflection features-->
<xsl:variable name="InflFeaturesToPercolate">
<xsl:variable name="PrefixSlotsUnification">
<xsl:choose>
<xsl:when test="$iPrefixSlotMorphs &gt; 1">
<xsl:call-template name="UnifyPrefixSlots">
<xsl:with-param name="PreviousResult" />
<xsl:with-param name="PrefixSlot" select="preceding-sibling::*[1]" />
<xsl:with-param name="iRemaining" select="$iPrefixSlotMorphs - 1" />
<xsl:with-param name="sRuleInfo">The inflectional template named 'Transitive verb' for category 'transitive verb'</xsl:with-param>
</xsl:call-template>
</xsl:when>
<xsl:when test="$iPrefixSlotMorphs=1">
<xsl:variable name="InflectionalPrefix" select="preceding-sibling::*[1]" />
<xsl:call-template name="UnifyTwoFeatureStructures">
<xsl:with-param name="FirstFS" select="fs" />
<xsl:with-param name="SecondFS" select="$InflectionalPrefix/inflMsa/fs" />
<xsl:with-param name="sTopLevelId" select="fs/@id" />
<xsl:with-param name="sRuleInfo">The inflectional template named 'Transitive verb' for category 'transitive verb'</xsl:with-param>
<xsl:with-param name="sFirstDescription" select="'stem'" />
<xsl:with-param name="sSecondDescription">
<xsl:text>inflectional prefix (</xsl:text>
<xsl:value-of select="$InflectionalPrefix/shortName" />
<xsl:text>)</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:when>
</xsl:choose>
</xsl:variable>
<xsl:variable name="SuffixSlotsUnification">
<xsl:choose>
<xsl:when test="$iSuffixSlotMorphs &gt; 1">
<xsl:call-template name="UnifySuffixSlots">
<xsl:with-param name="PreviousResult" />
<xsl:with-param name="SuffixSlot" select="following-sibling::*[1]" />
<xsl:with-param name="iRemaining" select="$iSuffixSlotMorphs - 1" />
<xsl:with-param name="sRuleInfo">The inflectional template named 'Transitive verb' for category 'transitive verb'</xsl:with-param>
</xsl:call-template>
</xsl:when>
<xsl:when test="$iSuffixSlotMorphs=1">
<xsl:variable name="InflectionalSuffix" select="following-sibling::*[1]" />
<xsl:call-template name="UnifyTwoFeatureStructures">
<xsl:with-param name="FirstFS" select="fs" />
<xsl:with-param name="SecondFS" select="$InflectionalSuffix/inflMsa/fs" />
<xsl:with-param name="sTopLevelId" select="fs/@id" />
<xsl:with-param name="sRuleInfo">The inflectional template named 'Transitive verb' for category 'transitive verb'</xsl:with-param>
<xsl:with-param name="sFirstDescription" select="'stem'" />
<xsl:with-param name="sSecondDescription">
<xsl:text>inflectional suffix (</xsl:text>
<xsl:value-of select="$InflectionalSuffix/shortName" />
<xsl:text>)</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:when>
</xsl:choose>
</xsl:variable>
<xsl:variable name="PrefixSuffixSlotsUnification">
<xsl:choose>
<xsl:when test="string-length($PrefixSlotsUnification) &gt; 0 and string-length($SuffixSlotsUnification) &gt; 0">
<xsl:call-template name="UnifyTwoFeatureStructures">
<xsl:with-param name="FirstFS" select="msxsl:node-set($PrefixSlotsUnification)/fs" />
<xsl:with-param name="SecondFS" select="msxsl:node-set($SuffixSlotsUnification)/fs" />
<xsl:with-param name="sTopLevelId" select="msxsl:node-set($PrefixSlotsUnification)/fs/@id" />
<xsl:with-param name="sRuleInfo">The inflectional template named 'Transitive verb' for category 'transitive verb'</xsl:with-param>
<xsl:with-param name="sFirstDescription">
<xsl:value-of select="'unification of all the prefix slots'" />
</xsl:with-param>
<xsl:with-param name="sSecondDescription">
<xsl:value-of select="'unification of all the suffix slots'" />
</xsl:with-param>
</xsl:call-template>
</xsl:when>
<xsl:when test="string-length($PrefixSlotsUnification) &gt; 0">
<xsl:copy-of select="$PrefixSlotsUnification" />
</xsl:when>
<xsl:when test="string-length($SuffixSlotsUnification) &gt; 0">
<xsl:copy-of select="$SuffixSlotsUnification" />
</xsl:when>
</xsl:choose>
</xsl:variable>
<xsl:call-template name="UnifyTwoFeatureStructures">
<xsl:with-param name="FirstFS" select="fs" />
<xsl:with-param name="SecondFS" select="msxsl:node-set($PrefixSuffixSlotsUnification)/fs" />
<xsl:with-param name="sTopLevelId" select="fs/@id" />
<xsl:with-param name="sRuleInfo">The inflectional template named 'Transitive verb' for category 'transitive verb'</xsl:with-param>
<xsl:with-param name="sFirstDescription">
<xsl:value-of select="'stem'" />
</xsl:with-param>
<xsl:with-param name="sSecondDescription">
<xsl:value-of select="'unification of all the slots'" />
</xsl:with-param>
</xsl:call-template>
</xsl:variable>
<xsl:if test="msxsl:node-set($InflFeaturesToPercolate)/fs/descendant::feature">
<xsl:copy-of select="$InflFeaturesToPercolate" />
</xsl:if>
<xsl:variable name="StemNameConstraintResult">
<xsl:call-template name="CheckStemNames168">
<xsl:with-param name="stemName" select="stemName/@id" />
<xsl:with-param name="InflFeatures" select="msxsl:node-set($InflFeaturesToPercolate)" />
</xsl:call-template>
</xsl:variable>
<xsl:if test="msxsl:node-set($StemNameConstraintResult)/failure">
<xsl:copy-of select="msxsl:node-set($StemNameConstraintResult)" />
</xsl:if>
<xsl:if test="@blocksInflection!='-'">
<failure>The inflectional template named 'Transitive verb' for category 'transitive verb' failed because the stem was built by a non-final template and there was no intervening derivation or compounding.</failure>
</xsl:if>
<xsl:if test="name()='partial' and @inflected='+'">
<failure>Partial inflectional template has already been inflected.</failure>
</xsl:if>
<!--copy any inflectional prefixes-->
<xsl:choose>
<xsl:when test="$iPrefixSlotMorphs &gt; 0">
<xsl:for-each select="preceding-sibling::*[position()&lt;=$iPrefixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</xsl:when>
<xsl:otherwise>
<!-- output any failure nodes -->
<xsl:copy-of select="$PrefixSlotMorphs" />
</xsl:otherwise>
</xsl:choose>
<!--copy the stem-->
<xsl:copy-of select="." />
<!--copy any inflectional suffixes-->
<xsl:choose>
<xsl:when test="$iSuffixSlotMorphs &gt; 0">
<xsl:for-each select="following-sibling::*[position()&lt;=$iSuffixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</xsl:when>
<xsl:otherwise>
<!-- output any failure nodes -->
<xsl:copy-of select="$SuffixSlotMorphs" />
</xsl:otherwise>
</xsl:choose>
</full>
<!-- copy what's after the template -->
<xsl:for-each select="following-sibling::*[position()&gt;$iSuffixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</seq>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
PartialInflectionalTemplate171POS168
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="PartialInflectionalTemplate171POS168">
<seq step="Try to apply inflectional template 'Transitive verb' on a Partial analysis node.  Apply it for category 'transitive verb'. The result will be another Partial analysis node.">
<xsl:variable name="PrefixSlotMorphs">
<xsl:call-template name="InflectionalTemplate171PrefixSlot182">
<xsl:with-param name="morph" select="preceding-sibling::*[1]" />
</xsl:call-template>
</xsl:variable>
<xsl:variable name="iPrefixSlotMorphs">
<xsl:choose>
<xsl:when test="not(contains($PrefixSlotMorphs, ' '))">
<xsl:value-of select="string-length($PrefixSlotMorphs)" />
</xsl:when>
<xsl:otherwise>0</xsl:otherwise>
</xsl:choose>
</xsl:variable>
<xsl:variable name="SuffixSlotMorphs">
<xsl:call-template name="InflectionalTemplate171SuffixSlot180">
<xsl:with-param name="morph" select="following-sibling::*[1]" />
</xsl:call-template>
</xsl:variable>
<xsl:variable name="iSuffixSlotMorphs">
<xsl:choose>
<xsl:when test="not(contains($SuffixSlotMorphs, ' '))">
<xsl:value-of select="string-length($SuffixSlotMorphs)" />
</xsl:when>
<xsl:otherwise>0</xsl:otherwise>
</xsl:choose>
</xsl:variable>
<!-- copy what's before the template -->
<xsl:for-each select="preceding-sibling::*[position()&gt;$iPrefixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
<partial>
<xsl:attribute name="syncat">
<xsl:text>168</xsl:text>
</xsl:attribute>
<xsl:attribute name="syncatAbbr">
<xsl:text>trans</xsl:text>
</xsl:attribute>
<xsl:attribute name="requiresInfl">
<xsl:text>-</xsl:text>
</xsl:attribute>
<xsl:attribute name="inflected">
<xsl:text>+</xsl:text>
</xsl:attribute>
<xsl:if test="@blocksInflection!='-'">
<failure>The inflectional template named 'Transitive verb' for category 'transitive verb' failed because the stem was built by a non-final template and there was no intervening derivation or compounding.</failure>
</xsl:if>
<xsl:if test="name()='partial' and @inflected='+'">
<failure>Partial inflectional template has already been inflected.</failure>
</xsl:if>
<!--copy any inflectional prefixes-->
<xsl:choose>
<xsl:when test="$iPrefixSlotMorphs &gt; 0">
<xsl:for-each select="preceding-sibling::*[position()&lt;=$iPrefixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</xsl:when>
<xsl:otherwise>
<!-- output any failure nodes -->
<xsl:copy-of select="$PrefixSlotMorphs" />
</xsl:otherwise>
</xsl:choose>
<!--copy the stem-->
<xsl:copy-of select="." />
<!--copy any inflectional suffixes-->
<xsl:choose>
<xsl:when test="$iSuffixSlotMorphs &gt; 0">
<xsl:for-each select="following-sibling::*[position()&lt;=$iSuffixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</xsl:when>
<xsl:otherwise>
<!-- output any failure nodes -->
<xsl:copy-of select="$SuffixSlotMorphs" />
</xsl:otherwise>
</xsl:choose>
</partial>
<!-- copy what's after the template -->
<xsl:for-each select="following-sibling::*[position()&gt;$iSuffixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</seq>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
InflectionalTemplate171PrefixSlot182
	Params: morph - morpheme to check
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="InflectionalTemplate171PrefixSlot182">
<xsl:param name="morph" />
<xsl:choose>
<xsl:when test="$morph/@wordType = '182'">
<!--Required slot matches: if it is compatible, try next slot (if any) with next morph-->
<!--Check inflection class compatibility-->
<xsl:variable name="sStemInflClass">
<xsl:value-of select="@inflClass" />
</xsl:variable>
<xsl:choose>
<xsl:when test="$morph/inflClasses">
<xsl:choose>
<xsl:when test="$morph/inflClasses[inflClass/@hvo=$sStemInflClass] or $sStemInflClass=''">
<xsl:call-template name="IndicateInflAffixSucceeded" />
<xsl:call-template name="InflectionalTemplate171PrefixSlot181">
<xsl:with-param name="morph" select="$morph/preceding-sibling::morph[1]" />
</xsl:call-template>
</xsl:when>
<xsl:otherwise>
<failure>The inflectional template named 'Transitive verb' for category 'transitive verb'<xsl:text> failed because in the </xsl:text>required prefix<xsl:text> slot '</xsl:text>Object<xsl:text>', the inflection class of the stem (</xsl:text>
<xsl:value-of select="@inflClassAbbr" />
<xsl:text>) does not match any of the inflection classes of the inflectional affix (</xsl:text>
<xsl:value-of select="$morph/shortName" />
<xsl:text>).  The inflection class</xsl:text>
<xsl:choose>
<xsl:when test="count($morph/inflClasses/inflClass)=1">
<xsl:text> of this affix is: </xsl:text>
</xsl:when>
<xsl:otherwise>
<xsl:text>es of this affix are: </xsl:text>
</xsl:otherwise>
</xsl:choose>
<xsl:for-each select="$morph/inflClasses/inflClass">
<xsl:value-of select="@abbr" />
<xsl:if test="position() != last()">
<xsl:text>, </xsl:text>
</xsl:if>
</xsl:for-each>
<xsl:text>.</xsl:text>
</failure>
</xsl:otherwise>
</xsl:choose>
</xsl:when>
<xsl:otherwise>
<!--No inflection classes to check; indicate success and try next slot (if any)-->
<xsl:call-template name="IndicateInflAffixSucceeded" />
<xsl:call-template name="InflectionalTemplate171PrefixSlot181">
<xsl:with-param name="morph" select="$morph/preceding-sibling::morph[1]" />
</xsl:call-template>
</xsl:otherwise>
</xsl:choose>
</xsl:when>
<xsl:otherwise>
<!--Required slot does not match: give failure notice-->
<failure>The inflectional template named 'Transitive verb' for category 'transitive verb' failed because the required prefix slot 'Object' was not found.</failure>
</xsl:otherwise>
</xsl:choose>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
InflectionalTemplate171PrefixSlot181
	Params: morph - morpheme to check
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="InflectionalTemplate171PrefixSlot181">
<xsl:param name="morph" />
<xsl:choose>
<xsl:when test="$morph/@wordType = '181'">
<!--Optional slot matches: if it is compatible, try next slot (if any) with next morph-->
<!--Check inflection class compatibility-->
<xsl:variable name="sStemInflClass">
<xsl:value-of select="@inflClass" />
</xsl:variable>
<xsl:choose>
<xsl:when test="$morph/inflClasses">
<xsl:choose>
<xsl:when test="$morph/inflClasses[inflClass/@hvo=$sStemInflClass] or $sStemInflClass=''">
<xsl:call-template name="IndicateInflAffixSucceeded" />
</xsl:when>
<xsl:otherwise>
<failure>The inflectional template named 'Transitive verb' for category 'transitive verb'<xsl:text> failed because in the </xsl:text>optional prefix<xsl:text> slot '</xsl:text>Subject<xsl:text>', the inflection class of the stem (</xsl:text>
<xsl:value-of select="@inflClassAbbr" />
<xsl:text>) does not match any of the inflection classes of the inflectional affix (</xsl:text>
<xsl:value-of select="$morph/shortName" />
<xsl:text>).  The inflection class</xsl:text>
<xsl:choose>
<xsl:when test="count($morph/inflClasses/inflClass)=1">
<xsl:text> of this affix is: </xsl:text>
</xsl:when>
<xsl:otherwise>
<xsl:text>es of this affix are: </xsl:text>
</xsl:otherwise>
</xsl:choose>
<xsl:for-each select="$morph/inflClasses/inflClass">
<xsl:value-of select="@abbr" />
<xsl:if test="position() != last()">
<xsl:text>, </xsl:text>
</xsl:if>
</xsl:for-each>
<xsl:text>.</xsl:text>
</failure>
</xsl:otherwise>
</xsl:choose>
</xsl:when>
<xsl:otherwise>
<!--No inflection classes to check; indicate success and try next slot (if any)-->
<xsl:call-template name="IndicateInflAffixSucceeded" />
</xsl:otherwise>
</xsl:choose>
</xsl:when>
<xsl:otherwise>
<!--Optional slot does not match: try next slot (if any) with this morph-->
<xsl:if test="$morph" />
</xsl:otherwise>
</xsl:choose>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
InflectionalTemplate171SuffixSlot180
	Params: morph - morpheme to check
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="InflectionalTemplate171SuffixSlot180">
<xsl:param name="morph" />
<xsl:choose>
<xsl:when test="$morph/@wordType = '180'">
<!--Required slot matches: if it is compatible, try next slot (if any) with next morph-->
<!--Check inflection class compatibility-->
<xsl:variable name="sStemInflClass">
<xsl:value-of select="@inflClass" />
</xsl:variable>
<xsl:choose>
<xsl:when test="$morph/inflClasses">
<xsl:choose>
<xsl:when test="$morph/inflClasses[inflClass/@hvo=$sStemInflClass] or $sStemInflClass=''">
<xsl:call-template name="IndicateInflAffixSucceeded" />
</xsl:when>
<xsl:otherwise>
<failure>The inflectional template named 'Transitive verb' for category 'transitive verb'<xsl:text> failed because in the </xsl:text>required suffix<xsl:text> slot '</xsl:text>Tense<xsl:text>', the inflection class of the stem (</xsl:text>
<xsl:value-of select="@inflClassAbbr" />
<xsl:text>) does not match any of the inflection classes of the inflectional affix (</xsl:text>
<xsl:value-of select="$morph/shortName" />
<xsl:text>).  The inflection class</xsl:text>
<xsl:choose>
<xsl:when test="count($morph/inflClasses/inflClass)=1">
<xsl:text> of this affix is: </xsl:text>
</xsl:when>
<xsl:otherwise>
<xsl:text>es of this affix are: </xsl:text>
</xsl:otherwise>
</xsl:choose>
<xsl:for-each select="$morph/inflClasses/inflClass">
<xsl:value-of select="@abbr" />
<xsl:if test="position() != last()">
<xsl:text>, </xsl:text>
</xsl:if>
</xsl:for-each>
<xsl:text>.</xsl:text>
</failure>
</xsl:otherwise>
</xsl:choose>
</xsl:when>
<xsl:otherwise>
<!--No inflection classes to check; indicate success and try next slot (if any)-->
<xsl:call-template name="IndicateInflAffixSucceeded" />
</xsl:otherwise>
</xsl:choose>
</xsl:when>
<xsl:otherwise>
<!--Required slot does not match: give failure notice-->
<failure>The inflectional template named 'Transitive verb' for category 'transitive verb' failed because the required suffix slot 'Tense' was not found.</failure>
</xsl:otherwise>
</xsl:choose>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
InflectionalTemplate175
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="InflectionalTemplate175">
<seq step="Try to apply inflectional template 'Bitransitive verb'.  The result will be a Full analysis node.">
<xsl:variable name="PrefixSlotMorphs">
<xsl:call-template name="InflectionalTemplate175PrefixSlot182">
<xsl:with-param name="morph" select="preceding-sibling::*[1]" />
</xsl:call-template>
</xsl:variable>
<xsl:variable name="iPrefixSlotMorphs">
<xsl:choose>
<xsl:when test="not(contains($PrefixSlotMorphs, ' '))">
<xsl:value-of select="string-length($PrefixSlotMorphs)" />
</xsl:when>
<xsl:otherwise>0</xsl:otherwise>
</xsl:choose>
</xsl:variable>
<xsl:variable name="SuffixSlotMorphs">
<xsl:call-template name="InflectionalTemplate175SuffixSlot180">
<xsl:with-param name="morph" select="following-sibling::*[1]" />
</xsl:call-template>
</xsl:variable>
<xsl:variable name="iSuffixSlotMorphs">
<xsl:choose>
<xsl:when test="not(contains($SuffixSlotMorphs, ' '))">
<xsl:value-of select="string-length($SuffixSlotMorphs)" />
</xsl:when>
<xsl:otherwise>0</xsl:otherwise>
</xsl:choose>
</xsl:variable>
<!-- copy what's before the template -->
<xsl:for-each select="preceding-sibling::*[position()&gt;$iPrefixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
<!--have a full-->
<full>
<xsl:attribute name="syncat">
<xsl:value-of select="@syncat" />
</xsl:attribute>
<xsl:attribute name="syncatAbbr">
<xsl:value-of select="@syncatAbbr" />
</xsl:attribute>
<xsl:attribute name="requiresInfl">
<xsl:text>-</xsl:text>
</xsl:attribute>
<xsl:attribute name="inflected">
<xsl:text>+</xsl:text>
</xsl:attribute>
<!--check category of the stem-->
<xsl:variable name="sCategoriesAreCompatible">
<xsl:call-template name="TestCompatibleCategories">
<xsl:with-param name="sFirstCat">172</xsl:with-param>
<xsl:with-param name="sSecondCat">
<xsl:value-of select="@syncat" />
</xsl:with-param>
</xsl:call-template>
</xsl:variable>
<xsl:if test="$sCategoriesAreCompatible != $sSuccess">
<xsl:call-template name="ReportFailure">
<xsl:with-param name="sFirstItemComponent">
<xsl:text> category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemComponentAbbr">bitrans</xsl:with-param>
<xsl:with-param name="sFirstItemDescription">
<xsl:text>inflectional template</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemValue">Bitransitive verb</xsl:with-param>
<xsl:with-param name="sSecondItemComponent">
<xsl:text>category</xsl:text>
</xsl:with-param>
<xsl:with-param name="sSecondItemComponentAbbr" select="@syncatAbbr" />
<xsl:with-param name="sSecondItemDescription">
<xsl:text>stem</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:if>
<!--Check for productivity restrictions-->
<xsl:variable name="stem" select="." />
<xsl:if test="$iPrefixSlotMorphs &gt; 0">
<xsl:for-each select="preceding-sibling::*[position()&lt;=$iPrefixSlotMorphs]">
<xsl:call-template name="CheckInflectionalAffixProductivityRestrictions">
<xsl:with-param name="stemMorph" select="$stem" />
<xsl:with-param name="inflMorph" select="." />
<xsl:with-param name="sType" select="'prefix'" />
</xsl:call-template>
</xsl:for-each>
</xsl:if>
<xsl:if test="$iSuffixSlotMorphs &gt; 0">
<xsl:for-each select="following-sibling::*[position()&lt;=$iSuffixSlotMorphs]">
<xsl:call-template name="CheckInflectionalAffixProductivityRestrictions">
<xsl:with-param name="stemMorph" select="$stem" />
<xsl:with-param name="inflMorph" select="." />
<xsl:with-param name="sType" select="'suffix'" />
</xsl:call-template>
</xsl:for-each>
</xsl:if>
<!--Percolate features-->
<!--Check compatitbility of all inflection features-->
<xsl:variable name="InflFeaturesToPercolate">
<xsl:variable name="PrefixSlotsUnification">
<xsl:choose>
<xsl:when test="$iPrefixSlotMorphs &gt; 1">
<xsl:call-template name="UnifyPrefixSlots">
<xsl:with-param name="PreviousResult" />
<xsl:with-param name="PrefixSlot" select="preceding-sibling::*[1]" />
<xsl:with-param name="iRemaining" select="$iPrefixSlotMorphs - 1" />
<xsl:with-param name="sRuleInfo">The inflectional template named 'Bitransitive verb' for category 'bitransitive verb'</xsl:with-param>
</xsl:call-template>
</xsl:when>
<xsl:when test="$iPrefixSlotMorphs=1">
<xsl:variable name="InflectionalPrefix" select="preceding-sibling::*[1]" />
<xsl:call-template name="UnifyTwoFeatureStructures">
<xsl:with-param name="FirstFS" select="fs" />
<xsl:with-param name="SecondFS" select="$InflectionalPrefix/inflMsa/fs" />
<xsl:with-param name="sTopLevelId" select="fs/@id" />
<xsl:with-param name="sRuleInfo">The inflectional template named 'Bitransitive verb' for category 'bitransitive verb'</xsl:with-param>
<xsl:with-param name="sFirstDescription" select="'stem'" />
<xsl:with-param name="sSecondDescription">
<xsl:text>inflectional prefix (</xsl:text>
<xsl:value-of select="$InflectionalPrefix/shortName" />
<xsl:text>)</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:when>
</xsl:choose>
</xsl:variable>
<xsl:variable name="SuffixSlotsUnification">
<xsl:choose>
<xsl:when test="$iSuffixSlotMorphs &gt; 1">
<xsl:call-template name="UnifySuffixSlots">
<xsl:with-param name="PreviousResult" />
<xsl:with-param name="SuffixSlot" select="following-sibling::*[1]" />
<xsl:with-param name="iRemaining" select="$iSuffixSlotMorphs - 1" />
<xsl:with-param name="sRuleInfo">The inflectional template named 'Bitransitive verb' for category 'bitransitive verb'</xsl:with-param>
</xsl:call-template>
</xsl:when>
<xsl:when test="$iSuffixSlotMorphs=1">
<xsl:variable name="InflectionalSuffix" select="following-sibling::*[1]" />
<xsl:call-template name="UnifyTwoFeatureStructures">
<xsl:with-param name="FirstFS" select="fs" />
<xsl:with-param name="SecondFS" select="$InflectionalSuffix/inflMsa/fs" />
<xsl:with-param name="sTopLevelId" select="fs/@id" />
<xsl:with-param name="sRuleInfo">The inflectional template named 'Bitransitive verb' for category 'bitransitive verb'</xsl:with-param>
<xsl:with-param name="sFirstDescription" select="'stem'" />
<xsl:with-param name="sSecondDescription">
<xsl:text>inflectional suffix (</xsl:text>
<xsl:value-of select="$InflectionalSuffix/shortName" />
<xsl:text>)</xsl:text>
</xsl:with-param>
</xsl:call-template>
</xsl:when>
</xsl:choose>
</xsl:variable>
<xsl:variable name="PrefixSuffixSlotsUnification">
<xsl:choose>
<xsl:when test="string-length($PrefixSlotsUnification) &gt; 0 and string-length($SuffixSlotsUnification) &gt; 0">
<xsl:call-template name="UnifyTwoFeatureStructures">
<xsl:with-param name="FirstFS" select="msxsl:node-set($PrefixSlotsUnification)/fs" />
<xsl:with-param name="SecondFS" select="msxsl:node-set($SuffixSlotsUnification)/fs" />
<xsl:with-param name="sTopLevelId" select="msxsl:node-set($PrefixSlotsUnification)/fs/@id" />
<xsl:with-param name="sRuleInfo">The inflectional template named 'Bitransitive verb' for category 'bitransitive verb'</xsl:with-param>
<xsl:with-param name="sFirstDescription">
<xsl:value-of select="'unification of all the prefix slots'" />
</xsl:with-param>
<xsl:with-param name="sSecondDescription">
<xsl:value-of select="'unification of all the suffix slots'" />
</xsl:with-param>
</xsl:call-template>
</xsl:when>
<xsl:when test="string-length($PrefixSlotsUnification) &gt; 0">
<xsl:copy-of select="$PrefixSlotsUnification" />
</xsl:when>
<xsl:when test="string-length($SuffixSlotsUnification) &gt; 0">
<xsl:copy-of select="$SuffixSlotsUnification" />
</xsl:when>
</xsl:choose>
</xsl:variable>
<xsl:call-template name="UnifyTwoFeatureStructures">
<xsl:with-param name="FirstFS" select="fs" />
<xsl:with-param name="SecondFS" select="msxsl:node-set($PrefixSuffixSlotsUnification)/fs" />
<xsl:with-param name="sTopLevelId" select="fs/@id" />
<xsl:with-param name="sRuleInfo">The inflectional template named 'Bitransitive verb' for category 'bitransitive verb'</xsl:with-param>
<xsl:with-param name="sFirstDescription">
<xsl:value-of select="'stem'" />
</xsl:with-param>
<xsl:with-param name="sSecondDescription">
<xsl:value-of select="'unification of all the slots'" />
</xsl:with-param>
</xsl:call-template>
</xsl:variable>
<xsl:if test="msxsl:node-set($InflFeaturesToPercolate)/fs/descendant::feature">
<xsl:copy-of select="$InflFeaturesToPercolate" />
</xsl:if>
<xsl:variable name="StemNameConstraintResult">
<xsl:call-template name="CheckStemNames172">
<xsl:with-param name="stemName" select="stemName/@id" />
<xsl:with-param name="InflFeatures" select="msxsl:node-set($InflFeaturesToPercolate)" />
</xsl:call-template>
</xsl:variable>
<xsl:if test="msxsl:node-set($StemNameConstraintResult)/failure">
<xsl:copy-of select="msxsl:node-set($StemNameConstraintResult)" />
</xsl:if>
<xsl:if test="@blocksInflection!='-'">
<failure>The inflectional template named 'Bitransitive verb' for category 'bitransitive verb' failed because the stem was built by a non-final template and there was no intervening derivation or compounding.</failure>
</xsl:if>
<xsl:if test="name()='partial' and @inflected='+'">
<failure>Partial inflectional template has already been inflected.</failure>
</xsl:if>
<!--copy any inflectional prefixes-->
<xsl:choose>
<xsl:when test="$iPrefixSlotMorphs &gt; 0">
<xsl:for-each select="preceding-sibling::*[position()&lt;=$iPrefixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</xsl:when>
<xsl:otherwise>
<!-- output any failure nodes -->
<xsl:copy-of select="$PrefixSlotMorphs" />
</xsl:otherwise>
</xsl:choose>
<!--copy the stem-->
<xsl:copy-of select="." />
<!--copy any inflectional suffixes-->
<xsl:choose>
<xsl:when test="$iSuffixSlotMorphs &gt; 0">
<xsl:for-each select="following-sibling::*[position()&lt;=$iSuffixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</xsl:when>
<xsl:otherwise>
<!-- output any failure nodes -->
<xsl:copy-of select="$SuffixSlotMorphs" />
</xsl:otherwise>
</xsl:choose>
</full>
<!-- copy what's after the template -->
<xsl:for-each select="following-sibling::*[position()&gt;$iSuffixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</seq>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
PartialInflectionalTemplate175POS172
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="PartialInflectionalTemplate175POS172">
<seq step="Try to apply inflectional template 'Bitransitive verb' on a Partial analysis node.  Apply it for category 'bitransitive verb'. The result will be another Partial analysis node.">
<xsl:variable name="PrefixSlotMorphs">
<xsl:call-template name="InflectionalTemplate175PrefixSlot182">
<xsl:with-param name="morph" select="preceding-sibling::*[1]" />
</xsl:call-template>
</xsl:variable>
<xsl:variable name="iPrefixSlotMorphs">
<xsl:choose>
<xsl:when test="not(contains($PrefixSlotMorphs, ' '))">
<xsl:value-of select="string-length($PrefixSlotMorphs)" />
</xsl:when>
<xsl:otherwise>0</xsl:otherwise>
</xsl:choose>
</xsl:variable>
<xsl:variable name="SuffixSlotMorphs">
<xsl:call-template name="InflectionalTemplate175SuffixSlot180">
<xsl:with-param name="morph" select="following-sibling::*[1]" />
</xsl:call-template>
</xsl:variable>
<xsl:variable name="iSuffixSlotMorphs">
<xsl:choose>
<xsl:when test="not(contains($SuffixSlotMorphs, ' '))">
<xsl:value-of select="string-length($SuffixSlotMorphs)" />
</xsl:when>
<xsl:otherwise>0</xsl:otherwise>
</xsl:choose>
</xsl:variable>
<!-- copy what's before the template -->
<xsl:for-each select="preceding-sibling::*[position()&gt;$iPrefixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
<partial>
<xsl:attribute name="syncat">
<xsl:text>172</xsl:text>
</xsl:attribute>
<xsl:attribute name="syncatAbbr">
<xsl:text>bitrans</xsl:text>
</xsl:attribute>
<xsl:attribute name="requiresInfl">
<xsl:text>-</xsl:text>
</xsl:attribute>
<xsl:attribute name="inflected">
<xsl:text>+</xsl:text>
</xsl:attribute>
<xsl:if test="@blocksInflection!='-'">
<failure>The inflectional template named 'Bitransitive verb' for category 'bitransitive verb' failed because the stem was built by a non-final template and there was no intervening derivation or compounding.</failure>
</xsl:if>
<xsl:if test="name()='partial' and @inflected='+'">
<failure>Partial inflectional template has already been inflected.</failure>
</xsl:if>
<!--copy any inflectional prefixes-->
<xsl:choose>
<xsl:when test="$iPrefixSlotMorphs &gt; 0">
<xsl:for-each select="preceding-sibling::*[position()&lt;=$iPrefixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</xsl:when>
<xsl:otherwise>
<!-- output any failure nodes -->
<xsl:copy-of select="$PrefixSlotMorphs" />
</xsl:otherwise>
</xsl:choose>
<!--copy the stem-->
<xsl:copy-of select="." />
<!--copy any inflectional suffixes-->
<xsl:choose>
<xsl:when test="$iSuffixSlotMorphs &gt; 0">
<xsl:for-each select="following-sibling::*[position()&lt;=$iSuffixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</xsl:when>
<xsl:otherwise>
<!-- output any failure nodes -->
<xsl:copy-of select="$SuffixSlotMorphs" />
</xsl:otherwise>
</xsl:choose>
</partial>
<!-- copy what's after the template -->
<xsl:for-each select="following-sibling::*[position()&gt;$iSuffixSlotMorphs]">
<xsl:copy-of select="." />
</xsl:for-each>
</seq>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
InflectionalTemplate175PrefixSlot182
	Params: morph - morpheme to check
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="InflectionalTemplate175PrefixSlot182">
<xsl:param name="morph" />
<xsl:choose>
<xsl:when test="$morph/@wordType = '182'">
<!--Required slot matches: if it is compatible, try next slot (if any) with next morph-->
<!--Check inflection class compatibility-->
<xsl:variable name="sStemInflClass">
<xsl:value-of select="@inflClass" />
</xsl:variable>
<xsl:choose>
<xsl:when test="$morph/inflClasses">
<xsl:choose>
<xsl:when test="$morph/inflClasses[inflClass/@hvo=$sStemInflClass] or $sStemInflClass=''">
<xsl:call-template name="IndicateInflAffixSucceeded" />
<xsl:call-template name="InflectionalTemplate175PrefixSlot181">
<xsl:with-param name="morph" select="$morph/preceding-sibling::morph[1]" />
</xsl:call-template>
</xsl:when>
<xsl:otherwise>
<failure>The inflectional template named 'Bitransitive verb' for category 'bitransitive verb'<xsl:text> failed because in the </xsl:text>required prefix<xsl:text> slot '</xsl:text>Object<xsl:text>', the inflection class of the stem (</xsl:text>
<xsl:value-of select="@inflClassAbbr" />
<xsl:text>) does not match any of the inflection classes of the inflectional affix (</xsl:text>
<xsl:value-of select="$morph/shortName" />
<xsl:text>).  The inflection class</xsl:text>
<xsl:choose>
<xsl:when test="count($morph/inflClasses/inflClass)=1">
<xsl:text> of this affix is: </xsl:text>
</xsl:when>
<xsl:otherwise>
<xsl:text>es of this affix are: </xsl:text>
</xsl:otherwise>
</xsl:choose>
<xsl:for-each select="$morph/inflClasses/inflClass">
<xsl:value-of select="@abbr" />
<xsl:if test="position() != last()">
<xsl:text>, </xsl:text>
</xsl:if>
</xsl:for-each>
<xsl:text>.</xsl:text>
</failure>
</xsl:otherwise>
</xsl:choose>
</xsl:when>
<xsl:otherwise>
<!--No inflection classes to check; indicate success and try next slot (if any)-->
<xsl:call-template name="IndicateInflAffixSucceeded" />
<xsl:call-template name="InflectionalTemplate175PrefixSlot181">
<xsl:with-param name="morph" select="$morph/preceding-sibling::morph[1]" />
</xsl:call-template>
</xsl:otherwise>
</xsl:choose>
</xsl:when>
<xsl:otherwise>
<!--Required slot does not match: give failure notice-->
<failure>The inflectional template named 'Bitransitive verb' for category 'bitransitive verb' failed because the required prefix slot 'Object' was not found.</failure>
</xsl:otherwise>
</xsl:choose>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
InflectionalTemplate175PrefixSlot181
	Params: morph - morpheme to check
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="InflectionalTemplate175PrefixSlot181">
<xsl:param name="morph" />
<xsl:choose>
<xsl:when test="$morph/@wordType = '181'">
<!--Optional slot matches: if it is compatible, try next slot (if any) with next morph-->
<!--Check inflection class compatibility-->
<xsl:variable name="sStemInflClass">
<xsl:value-of select="@inflClass" />
</xsl:variable>
<xsl:choose>
<xsl:when test="$morph/inflClasses">
<xsl:choose>
<xsl:when test="$morph/inflClasses[inflClass/@hvo=$sStemInflClass] or $sStemInflClass=''">
<xsl:call-template name="IndicateInflAffixSucceeded" />
</xsl:when>
<xsl:otherwise>
<failure>The inflectional template named 'Bitransitive verb' for category 'bitransitive verb'<xsl:text> failed because in the </xsl:text>optional prefix<xsl:text> slot '</xsl:text>Subject<xsl:text>', the inflection class of the stem (</xsl:text>
<xsl:value-of select="@inflClassAbbr" />
<xsl:text>) does not match any of the inflection classes of the inflectional affix (</xsl:text>
<xsl:value-of select="$morph/shortName" />
<xsl:text>).  The inflection class</xsl:text>
<xsl:choose>
<xsl:when test="count($morph/inflClasses/inflClass)=1">
<xsl:text> of this affix is: </xsl:text>
</xsl:when>
<xsl:otherwise>
<xsl:text>es of this affix are: </xsl:text>
</xsl:otherwise>
</xsl:choose>
<xsl:for-each select="$morph/inflClasses/inflClass">
<xsl:value-of select="@abbr" />
<xsl:if test="position() != last()">
<xsl:text>, </xsl:text>
</xsl:if>
</xsl:for-each>
<xsl:text>.</xsl:text>
</failure>
</xsl:otherwise>
</xsl:choose>
</xsl:when>
<xsl:otherwise>
<!--No inflection classes to check; indicate success and try next slot (if any)-->
<xsl:call-template name="IndicateInflAffixSucceeded" />
</xsl:otherwise>
</xsl:choose>
</xsl:when>
<xsl:otherwise>
<!--Optional slot does not match: try next slot (if any) with this morph-->
<xsl:if test="$morph" />
</xsl:otherwise>
</xsl:choose>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
InflectionalTemplate175SuffixSlot180
	Params: morph - morpheme to check
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="InflectionalTemplate175SuffixSlot180">
<xsl:param name="morph" />
<xsl:choose>
<xsl:when test="$morph/@wordType = '180'">
<!--Required slot matches: if it is compatible, try next slot (if any) with next morph-->
<!--Check inflection class compatibility-->
<xsl:variable name="sStemInflClass">
<xsl:value-of select="@inflClass" />
</xsl:variable>
<xsl:choose>
<xsl:when test="$morph/inflClasses">
<xsl:choose>
<xsl:when test="$morph/inflClasses[inflClass/@hvo=$sStemInflClass] or $sStemInflClass=''">
<xsl:call-template name="IndicateInflAffixSucceeded" />
</xsl:when>
<xsl:otherwise>
<failure>The inflectional template named 'Bitransitive verb' for category 'bitransitive verb'<xsl:text> failed because in the </xsl:text>required suffix<xsl:text> slot '</xsl:text>Tense<xsl:text>', the inflection class of the stem (</xsl:text>
<xsl:value-of select="@inflClassAbbr" />
<xsl:text>) does not match any of the inflection classes of the inflectional affix (</xsl:text>
<xsl:value-of select="$morph/shortName" />
<xsl:text>).  The inflection class</xsl:text>
<xsl:choose>
<xsl:when test="count($morph/inflClasses/inflClass)=1">
<xsl:text> of this affix is: </xsl:text>
</xsl:when>
<xsl:otherwise>
<xsl:text>es of this affix are: </xsl:text>
</xsl:otherwise>
</xsl:choose>
<xsl:for-each select="$morph/inflClasses/inflClass">
<xsl:value-of select="@abbr" />
<xsl:if test="position() != last()">
<xsl:text>, </xsl:text>
</xsl:if>
</xsl:for-each>
<xsl:text>.</xsl:text>
</failure>
</xsl:otherwise>
</xsl:choose>
</xsl:when>
<xsl:otherwise>
<!--No inflection classes to check; indicate success and try next slot (if any)-->
<xsl:call-template name="IndicateInflAffixSucceeded" />
</xsl:otherwise>
</xsl:choose>
</xsl:when>
<xsl:otherwise>
<!--Required slot does not match: give failure notice-->
<failure>The inflectional template named 'Bitransitive verb' for category 'bitransitive verb' failed because the required suffix slot 'Tense' was not found.</failure>
</xsl:otherwise>
</xsl:choose>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames3
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames3">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames6
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames6">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames7
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames7">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames6">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames10
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames10">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames6">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames15
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames15">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames18
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames18">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames21
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames21">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames22
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames22">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames21">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames25
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames25">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames21">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames28
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames28">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames21">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames31
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames31">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames21">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames34
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames34">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames21">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames37
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames37">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames21">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames40
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames40">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames21">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames43
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames43">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames21">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames46
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames46">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames21">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames49
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames49">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames21">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames52
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames52">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames21">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames55
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames55">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames21">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames58
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames58">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames21">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames61
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames61">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames21">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames64
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames64">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames21">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames69
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames69">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames70
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames70">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames69">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames73
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames73">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames69">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames78
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames78">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames81
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames81">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames82
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames82">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames81">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames85
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames85">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames81">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames88
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames88">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames81">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames91
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames91">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames81">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames94
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames94">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames81">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames104
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames104">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames107
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames107">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames110
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames110">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames111
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames111">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames110">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames114
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames114">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames110">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames117
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames117">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames110">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames120
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames120">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames110">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames123
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames123">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames110">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames126
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames126">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames110">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames131
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames131">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames132
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames132">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames131">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames133
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames133">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames132">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames136
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames136">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames132">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames141
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames141">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames131">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames146
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames146">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames147
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames147">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames146">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames150
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames150">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames146">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames153
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames153">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames146">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames157
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames157">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames146">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames160
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames160">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames146">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames163
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames163">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames146">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames168
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames168">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames146">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckStemNames172
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
<xsl:template name="CheckStemNames172">
<xsl:param name="stemName" />
<xsl:param name="InflFeatures" />
<xsl:call-template name="CheckStemNames146">
<xsl:with-param name="stemName" select="$stemName" />
<xsl:with-param name="InflFeatures" select="$InflFeatures" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputFeatureValue
	Output the value of the given feature.  If it's atomic, output the atomic value otherwise copy the embedded  <fs>
		Parameters: value = value to output
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
<xsl:template name="OutputFeatureValue">
<xsl:param name="value" />
<xsl:choose>
<xsl:when test="$value/fs">
<xsl:copy-of select="$value/fs" />
</xsl:when>
<xsl:otherwise>
<xsl:value-of select="$value" />
</xsl:otherwise>
</xsl:choose>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputFeatureStructureAsText
	Show a feature structure
		Parameters: fs = feature structure to show
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
<xsl:template name="OutputFeatureStructureAsText">
<xsl:param name="fs" />
<xsl:text disable-output-escaping="yes">[</xsl:text>
<xsl:for-each select="$fs/feature">
<xsl:if test="position()!=1">
<xsl:text />
</xsl:if>
<xsl:value-of select="name" />
<xsl:text disable-output-escaping="yes">:</xsl:text>
<xsl:choose>
<xsl:when test="value/fs">
<xsl:for-each select="value/fs">
<xsl:call-template name="OutputFeatureStructureAsText">
<xsl:with-param name="fs" select="." />
</xsl:call-template>
</xsl:for-each>
</xsl:when>
<xsl:otherwise>
<xsl:value-of select="value" />
</xsl:otherwise>
</xsl:choose>
</xsl:for-each>
<xsl:text>]</xsl:text>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OverrideFirstFsWithSecondFs
	Perform priority union where feature2 overrides what's in feature1
		Parameters: FirstFS = first feature structure to be overriden
						   SecondFS = second feature structure which overrides first fs
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
<xsl:template name="OverrideFirstFsWithSecondFs">
<xsl:param name="FirstFS" />
<xsl:param name="SecondFS" />
<xsl:call-template name="UnifyTwoFeatureStructures">
<xsl:with-param name="FirstFS" select="$FirstFS" />
<xsl:with-param name="SecondFS" select="$SecondFS" />
<xsl:with-param name="sTopLevelId" select="$FirstFS/@id" />
<xsl:with-param name="bPerformPriorityUnion" select="'Y'" />
</xsl:call-template>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckInflectionalAffixProductivityRestrictions
	Check for valid productivity restrictions between the stem and an inflectional affix
		Parameters: stemMorph = stem morpheme
						   inlfMorph = inflectional affix
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
<xsl:template name="CheckInflectionalAffixProductivityRestrictions">
<xsl:param name="stemMorph" />
<xsl:param name="inflMorph" />
<xsl:param name="sType" />
<xsl:for-each select="$inflMorph/inflMsa/fromProductivityRestriction">
<xsl:variable name="sThisId" select="@id" />
<xsl:if test="not($stemMorph/productivityRestriction[@id=$sThisId])">
<xsl:call-template name="ReportFailure">
<xsl:with-param name="sFirstItemComponent">
<xsl:text>from exception feature</xsl:text>
</xsl:with-param>
<xsl:with-param name="sFirstItemComponentAbbr">
<xsl:value-of select="name" />
</xsl:with-param>
<xsl:with-param name="sFirstItemDescription">inflectional <xsl:value-of select="$sType" />
</xsl:with-param>
<xsl:with-param name="sFirstItemValue" select="$inflMorph/shortName" />
<xsl:with-param name="sSecondItemComponent">
<xsl:text>exception features</xsl:text>
</xsl:with-param>
<xsl:with-param name="sSecondItemComponentAbbr" />
<xsl:with-param name="sSecondItemDescription"> stem</xsl:with-param>
</xsl:call-template>
</xsl:if>
</xsl:for-each>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
UnifyPrefixSlots
	Perform the unification operation on prefix slots
	Is recursive; result is the  <fs> element which is is the unification of the feature structures of all the slots.
		Parameters: PreviousResult = feature structure of any previous unifications
						   PrefixSlot = morph for a prefix slot
						   iRemaining = count of prefix slots remaining to be unified
						   sRuleInfo = rule info
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
<xsl:template name="UnifyPrefixSlots">
<xsl:param name="PreviousResult" />
<xsl:param name="PrefixSlot" />
<xsl:param name="iRemaining" />
<xsl:param name="sRuleInfo" />
<xsl:variable name="CurrentUnification">
<xsl:call-template name="UnifyTwoFeatureStructures">
<xsl:with-param name="FirstFS" select="$PrefixSlot/inflMsa/fs" />
<xsl:with-param name="SecondFS" select="$PreviousResult" />
<xsl:with-param name="sTopLevelId" select="$PrefixSlot/inflMsa/fs/@id" />
<xsl:with-param name="sRuleInfo" select="$sRuleInfo" />
<xsl:with-param name="sFirstDescription">
<xsl:text>inflectional prefix (</xsl:text>
<xsl:value-of select="$PrefixSlot/shortName" />
<xsl:text>)</xsl:text>
</xsl:with-param>
<xsl:with-param name="sSecondDescription">
<xsl:value-of select="'inflectional prefixes closer to the stem'" />
</xsl:with-param>
</xsl:call-template>
</xsl:variable>
<xsl:choose>
<xsl:when test="$iRemaining &gt; 0">
<xsl:call-template name="UnifyPrefixSlots">
<xsl:with-param name="PreviousResult" select="msxsl:node-set($CurrentUnification)/fs" />
<xsl:with-param name="PrefixSlot" select="$PrefixSlot/preceding-sibling::*[1]" />
<xsl:with-param name="iRemaining" select="$iRemaining - 1" />
<xsl:with-param name="sRuleInfo" select="$sRuleInfo" />
</xsl:call-template>
</xsl:when>
<xsl:otherwise>
<xsl:copy-of select="$CurrentUnification" />
</xsl:otherwise>
</xsl:choose>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
UnifySuffixSlots
	Perform the unification operation on suffix slots
	Is recursive; result is the  <fs> element which is is the unification of the feature structures of all the slots.
		Parameters: PreviousResult = feature structure of any previous unifications
						   SuffixSlot = morph for a suffix slot
						   iRemaining = count of suffix slots remaining to be unified
						   sRuleInfo = rule info
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
<xsl:template name="UnifySuffixSlots">
<xsl:param name="PreviousResult" />
<xsl:param name="SuffixSlot" />
<xsl:param name="iRemaining" />
<xsl:param name="sRuleInfo" />
<xsl:variable name="CurrentUnification">
<xsl:call-template name="UnifyTwoFeatureStructures">
<xsl:with-param name="FirstFS" select="$SuffixSlot/inflMsa/fs" />
<xsl:with-param name="SecondFS" select="$PreviousResult" />
<xsl:with-param name="sTopLevelId" select="$SuffixSlot/inflMsa/fs/@id" />
<xsl:with-param name="sRuleInfo" select="$sRuleInfo" />
<xsl:with-param name="sFirstDescription">
<xsl:text>inflectional suffix (</xsl:text>
<xsl:value-of select="$SuffixSlot/shortName" />
<xsl:text>)</xsl:text>
</xsl:with-param>
<xsl:with-param name="sSecondDescription">
<xsl:value-of select="'inflectional suffixes closer to the stem'" />
</xsl:with-param>
</xsl:call-template>
</xsl:variable>
<xsl:choose>
<xsl:when test="$iRemaining &gt; 0">
<xsl:call-template name="UnifySuffixSlots">
<xsl:with-param name="PreviousResult" select="msxsl:node-set($CurrentUnification)/fs" />
<xsl:with-param name="SuffixSlot" select="$SuffixSlot/following-sibling::*[1]" />
<xsl:with-param name="iRemaining" select="$iRemaining - 1" />
<xsl:with-param name="sRuleInfo" select="$sRuleInfo" />
</xsl:call-template>
</xsl:when>
<xsl:otherwise>
<xsl:copy-of select="$CurrentUnification" />
</xsl:otherwise>
</xsl:choose>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
UnifyTwoFeatureStructures
	Perform the unification operation on two feature structures.
	The  <fs> element which is put into the output is the unification of the two feature structures.
		Parameters: FirstFS = first feature structure
						   SecondFS = second feature structure
						   bIsTopLevel = flag for creating new id
						   sTopLevelId = id of top level
						   sRuleInfo = preamble of failure message
						   sFirstDescription = description associated with first FS, used in failure message
						   sSecondDescription = description associated with second FS, used in failure message
						   bPerformPriorityUnion = flag whether to do priority union or not
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
<xsl:template name="UnifyTwoFeatureStructures">
<xsl:param name="FirstFS" />
<xsl:param name="SecondFS" />
<xsl:param name="bIsTopLevel">Y</xsl:param>
<xsl:param name="sTopLevelId" />
<xsl:param name="sRuleInfo" />
<xsl:param name="sFirstDescription" />
<xsl:param name="sSecondDescription" />
<xsl:param name="bPerformPriorityUnion">N</xsl:param>
<fs>
<xsl:if test="$bIsTopLevel='Y'">
<xsl:attribute name="id">
<xsl:choose>
<xsl:when test="$bPerformPriorityUnion='Y'">
<xsl:text>PriorityUnionOf(</xsl:text>
</xsl:when>
<xsl:otherwise>
<xsl:text>UnificationOf(</xsl:text>
</xsl:otherwise>
</xsl:choose>
<xsl:choose>
<xsl:when test="msxsl:node-set($FirstFS)/@id != ''">
<xsl:value-of select="$FirstFS/@id" />
</xsl:when>
<xsl:otherwise>Empty</xsl:otherwise>
</xsl:choose>
<xsl:text>and</xsl:text>
<xsl:choose>
<xsl:when test="msxsl:node-set($SecondFS)/@id != ''">
<xsl:value-of select="$SecondFS/@id" />
</xsl:when>
<xsl:otherwise>Empty</xsl:otherwise>
</xsl:choose>
<xsl:text>)</xsl:text>
</xsl:attribute>
</xsl:if>
<!--loop through the features of both feature structures at same time, sorted by name-->
<xsl:for-each select="msxsl:node-set($FirstFS)/feature | msxsl:node-set($SecondFS)/feature">
<xsl:sort select="name" />
<!--get name of this feature-->
<xsl:variable name="sName">
<xsl:value-of select="name" />
</xsl:variable>
<!--get this feature if it's in the first feature structure-->
<xsl:variable name="f1SameName" select="msxsl:node-set($FirstFS)/feature[name=$sName]" />
<!--get this feature if it's in the second feature structure -->
<xsl:variable name="f2SameName" select="msxsl:node-set($SecondFS)/feature[name=$sName]" />
<xsl:choose>
<xsl:when test="$f1SameName and $f2SameName">
<!--both feature1 and feature2 have this feature name-->
<xsl:if test="ancestor::fs[@id=$sTopLevelId]">
<!--only need to do this for the feature in the first feature structure-->
<feature>
<name>
<xsl:value-of select="$sName" />
</name>
<xsl:choose>
<xsl:when test="$f1SameName/value/fs and $f2SameName/value/fs">
<!--both have nested feature structure-->
<value>
<xsl:call-template name="UnifyTwoFeatureStructures">
<xsl:with-param name="FirstFS" select="$f1SameName/value/fs" />
<xsl:with-param name="SecondFS" select="$f2SameName/value/fs" />
<xsl:with-param name="bIsTopLevel">N</xsl:with-param>
<xsl:with-param name="sTopLevelId" select="$sTopLevelId" />
<xsl:with-param name="sFirstDescription" select="$sFirstDescription" />
<xsl:with-param name="sSecondDescription" select="$sSecondDescription" />
<xsl:with-param name="bPerformPriorityUnion" select="$bPerformPriorityUnion" />
<xsl:with-param name="sRuleInfo" select="$sRuleInfo" />
</xsl:call-template>
</value>
</xsl:when>
<xsl:when test="$f1SameName/value=$f2SameName/value">
<!--both features have the same value-->
<value>
<xsl:value-of select="$f1SameName/value" />
</value>
</xsl:when>
<xsl:otherwise>
<!--there's a value conflict-->
<xsl:choose>
<xsl:when test="$bPerformPriorityUnion='Y'">
<!--second feature wins-->
<xsl:copy-of select="$f2SameName/value" />
</xsl:when>
<xsl:otherwise>
<!--output failure element and the values-->
<xsl:copy-of select="$f1SameName/failure" />
<xsl:copy-of select="$f2SameName/failure" />
<xsl:if test="$f1SameName/value and $f2SameName/value">
<failure>
<xsl:value-of select="$sRuleInfo" /> failed because at least one inflection feature of the <xsl:value-of select="$sFirstDescription" /> is incompatible with the inflection features of the <xsl:value-of select="$sSecondDescription" />.  The incompatibility is for feature <xsl:value-of select="$f1SameName/name" />.  This feature for the <xsl:value-of select="$sFirstDescription" /> has a value of <xsl:value-of select="$f1SameName/value" /> but the corresponding feature for the <xsl:value-of select="$sSecondDescription" /> has a value of <xsl:value-of select="$f2SameName/value" />.</failure>
</xsl:if>
</xsl:otherwise>
</xsl:choose>
</xsl:otherwise>
</xsl:choose>
</feature>
</xsl:if>
</xsl:when>
<xsl:otherwise>
<!--only one of the features has this feature-->
<feature>
<name>
<xsl:value-of select="name" />
</name>
<value>
<xsl:call-template name="OutputFeatureValue">
<xsl:with-param name="value" select="value" />
</xsl:call-template>
</value>
</feature>
</xsl:otherwise>
</xsl:choose>
</xsl:for-each>
</fs>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
XSubsumesY
	Determine if feature X subsumes feature Y
		Parameters: X = feature to check
							 Y = feature to look in
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
<xsl:template name="XSubsumesY">
<xsl:param name="X" />
<xsl:param name="Y" />
<xsl:choose>
<xsl:when test="not($X)">
<xsl:text>Y</xsl:text>
</xsl:when>
<xsl:when test="not($Y)">
<xsl:text>N</xsl:text>
</xsl:when>
<xsl:otherwise>
<xsl:for-each select="msxsl:node-set($X)/feature | msxsl:node-set($Y)/feature">
<xsl:sort select="name" />
<xsl:variable name="sName">
<xsl:value-of select="name" />
</xsl:variable>
<xsl:variable name="f1SameName" select="msxsl:node-set($X)/feature[name=$sName]" />
<xsl:variable name="f2SameName" select="msxsl:node-set($Y)/feature[name=$sName]" />
<xsl:choose>
<xsl:when test="$f1SameName and $f2SameName">
<xsl:choose>
<xsl:when test="$f1SameName/value/fs and $f2SameName/value/fs">
<xsl:call-template name="XSubsumesY">
<xsl:with-param name="X" select="$f1SameName/value/fs" />
<xsl:with-param name="Y" select="$f2SameName/value/fs" />
</xsl:call-template>
</xsl:when>
<xsl:when test="$f1SameName/value=$f2SameName/value">
<xsl:text>Y</xsl:text>
</xsl:when>
<xsl:otherwise>
<xsl:text>N</xsl:text>
</xsl:otherwise>
</xsl:choose>
</xsl:when>
<xsl:when test="$f1SameName and not($f2SameName)">
<xsl:text>N</xsl:text>
</xsl:when>
<xsl:otherwise />
</xsl:choose>
</xsl:for-each>
</xsl:otherwise>
</xsl:choose>
</xsl:template>
</xsl:stylesheet>
