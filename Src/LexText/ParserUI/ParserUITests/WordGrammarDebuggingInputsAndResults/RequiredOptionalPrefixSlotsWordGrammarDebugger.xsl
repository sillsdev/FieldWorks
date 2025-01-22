<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" msxsl:dummy="" exclude-result-prefixes="msxsl" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:msxsl="urn:schemas-microsoft-com:xslt">
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
    <xsl:call-template name="PartialEqualsRoots" />
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
Partial = Prefs* Roots Suffs* production
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="PartialEqualsRoots">
    <xsl:if test="not(preceding-sibling::morph[@wordType='root']) and following-sibling::morph[@wordType='root']">
      <seq step="Try to build a Partial analysis node on compound roots (when there are no compound rules).">
        <xsl:variable name="GenericPrefixMorphs">
          <xsl:call-template name="CountGenericPrefixes">
            <xsl:with-param name="morph" select="preceding-sibling::*[1]" />
          </xsl:call-template>
        </xsl:variable>
        <xsl:variable name="iGenericPrefixMorphs">
          <xsl:value-of select="string-length($GenericPrefixMorphs)" />
        </xsl:variable>
        <xsl:variable name="CompoundedRootMorphs">
          <xsl:call-template name="CountCompoundedRoots">
            <xsl:with-param name="morph" select="following-sibling::*[1]" />
          </xsl:call-template>
        </xsl:variable>
        <xsl:variable name="iCompoundedRootMorphs">
          <xsl:value-of select="string-length($CompoundedRootMorphs)" />
        </xsl:variable>
        <xsl:variable name="GenericSuffixMorphs">
          <xsl:call-template name="CountGenericSuffixes">
            <xsl:with-param name="morph" select="following-sibling::*[$iCompoundedRootMorphs]" />
          </xsl:call-template>
        </xsl:variable>
        <xsl:variable name="iGenericSuffixMorphs">
          <xsl:value-of select="string-length($GenericSuffixMorphs)" />
        </xsl:variable>
        <!-- copy what's before the partial -->
        <xsl:for-each select="preceding-sibling::*[position()&gt;$iGenericPrefixMorphs]">
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
          <!--copy any generic prefixes-->
          <xsl:if test="$iGenericPrefixMorphs&gt; 0">
            <xsl:for-each select="preceding-sibling::*[position()&lt;=$iGenericPrefixMorphs]">
              <xsl:copy-of select="." />
            </xsl:for-each>
          </xsl:if>
          <xsl:copy-of select="." />
          <xsl:copy-of select="following-sibling::*[position()&lt;=$iCompoundedRootMorphs]" />
          <!--copy any generic suffixes-->
          <xsl:if test="$iGenericSuffixMorphs &gt; 0">
            <xsl:for-each select="following-sibling::*[position()&gt;$iCompoundedRootMorphs and position()&lt;=$iCompoundedRootMorphs + $iGenericSuffixMorphs]">
              <xsl:copy-of select="." />
            </xsl:for-each>
          </xsl:if>
        </partial>
        <!-- copy what's after the partial -->
        <xsl:for-each select="following-sibling::*[position()&gt;$iCompoundedRootMorphs + $iGenericSuffixMorphs]">
          <xsl:copy-of select="." />
        </xsl:for-each>
      </seq>
    </xsl:if>
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
        <xsl:copy-of select="stemName" />
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
          <xsl:call-template name="PartialInflectionalTemplate9819POS5816" />
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
    <xsl:variable name="sFromProdRestrictAreGood">
      <xsl:choose>
        <xsl:when test="$derivMorph/derivMsa/fromProductivityRestriction">
          <xsl:variable name="sBadProdRestrict">
            <xsl:for-each select="$derivMorph/derivMsa/fromProductivityRestriction">
              <xsl:variable name="sThisId" select="@id" />
              <xsl:if test="not($stem/productivityRestriction[@id=$sThisId])">
                <xsl:value-of select="$sThisId" />
                <xsl:text>, </xsl:text>
              </xsl:if>
            </xsl:for-each>
          </xsl:variable>
          <xsl:choose>
            <xsl:when test="string-length($sBadProdRestrict) &gt; 0">
              <xsl:value-of select="$sBadProdRestrict" />
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
      <xsl:when test="$sCategoriesAreCompatible = $sSuccess and $sFromInflectionClassIsGood = $sSuccess and $sEnvCategoriesAreCompatible = $sSuccess and $sFromProdRestrictAreGood = $sSuccess">
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
        <xsl:call-template name="CheckAffixAllomorphFeatures">
          <xsl:with-param name="sAffix" select="'derivational'" />
          <xsl:with-param name="sAttachesTo" select="'stem'" />
          <xsl:with-param name="morph" select="$derivMorph" />
          <xsl:with-param name="stem" select="$stem" />
        </xsl:call-template>
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
        <xsl:if test="$sFromProdRestrictAreGood  != $sSuccess">
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
                <xsl:with-param name="sFirstItemDescription">derivational <xsl:value-of select="$sType" /></xsl:with-param>
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
      <xsl:call-template name="InflectionalTemplate9819" />
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
    <!-- stealth compounding...-->
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
      <xsl:when test="$sFirstCat = '7072'">
        <xsl:if test="$sSecondCat = '6990'">
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
            <xsl:text>Tried to make the stem be uninflected, but the stem has been inflected via a template that requires more derivation.  Therefore, a derivational affix or a compound rule must apply first.</xsl:text>
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
			InflectionalTemplate9819
			Params: none
			- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 -->
  <xsl:template name="InflectionalTemplate9819">
    <seq step="Try to apply inflectional template 'verb'.  The result will be a Full analysis node.">
      <xsl:variable name="PrefixSlotMorphs">
        <xsl:call-template name="InflectionalTemplate9819PrefixSlot4018-0">
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
        <xsl:call-template name="InflectionalTemplate9819SuffixSlot2431-0">
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
            <xsl:with-param name="sFirstCat">5816</xsl:with-param>
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
            <xsl:with-param name="sFirstItemComponentAbbr">v</xsl:with-param>
            <xsl:with-param name="sFirstItemDescription">
              <xsl:text>inflectional template</xsl:text>
            </xsl:with-param>
            <xsl:with-param name="sFirstItemValue">verb</xsl:with-param>
            <xsl:with-param name="sSecondItemComponent">
              <xsl:text>category</xsl:text>
            </xsl:with-param>
            <xsl:with-param name="sSecondItemComponentAbbr" select="@syncatAbbr" />
            <xsl:with-param name="sSecondItemDescription">
              <xsl:text>stem</xsl:text>
            </xsl:with-param>
          </xsl:call-template>
        </xsl:if>
        <xsl:variable name="stem" select="." />
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
                  <xsl:with-param name="sRuleInfo">The inflectional template named 'verb' for category 'Verb'</xsl:with-param>
                </xsl:call-template>
              </xsl:when>
              <xsl:when test="$iPrefixSlotMorphs=1">
                <xsl:variable name="InflectionalPrefix" select="preceding-sibling::*[1]" />
                <xsl:call-template name="UnifyTwoFeatureStructures">
                  <xsl:with-param name="FirstFS" select="fs" />
                  <xsl:with-param name="SecondFS" select="$InflectionalPrefix/inflMsa/fs" />
                  <xsl:with-param name="sTopLevelId" select="fs/@id" />
                  <xsl:with-param name="sRuleInfo">The inflectional template named 'verb' for category 'Verb'</xsl:with-param>
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
                  <xsl:with-param name="sRuleInfo">The inflectional template named 'verb' for category 'Verb'</xsl:with-param>
                </xsl:call-template>
              </xsl:when>
              <xsl:when test="$iSuffixSlotMorphs=1">
                <xsl:variable name="InflectionalSuffix" select="following-sibling::*[1]" />
                <xsl:call-template name="UnifyTwoFeatureStructures">
                  <xsl:with-param name="FirstFS" select="fs" />
                  <xsl:with-param name="SecondFS" select="$InflectionalSuffix/inflMsa/fs" />
                  <xsl:with-param name="sTopLevelId" select="fs/@id" />
                  <xsl:with-param name="sRuleInfo">The inflectional template named 'verb' for category 'Verb'</xsl:with-param>
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
                  <xsl:with-param name="sRuleInfo">The inflectional template named 'verb' for category 'Verb'</xsl:with-param>
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
            <xsl:with-param name="sRuleInfo">The inflectional template named 'verb' for category 'Verb'</xsl:with-param>
            <xsl:with-param name="sFirstDescription">
              <xsl:value-of select="'stem'" />
            </xsl:with-param>
            <xsl:with-param name="sSecondDescription">
              <xsl:value-of select="'unification of all the slots'" />
            </xsl:with-param>
          </xsl:call-template>
        </xsl:variable>
        <!--Check for productivity restrictions and affix allomorph features-->
        <xsl:if test="$iPrefixSlotMorphs &gt; 0">
          <xsl:for-each select="preceding-sibling::*[position()&lt;=$iPrefixSlotMorphs]">
            <xsl:call-template name="CheckInflectionalAffixProdRestrict">
              <xsl:with-param name="stemMorph" select="$stem" />
              <xsl:with-param name="inflMorph" select="." />
              <xsl:with-param name="sType" select="'prefix'" />
            </xsl:call-template>
            <xsl:call-template name="CheckAffixAllomorphFeatures">
              <xsl:with-param name="sAffix" select="'inflectional'" />
              <xsl:with-param name="sAttachesTo" select="'inflected form'" />
              <xsl:with-param name="morph" select="." />
              <xsl:with-param name="stem" select="msxsl:node-set($InflFeaturesToPercolate)" />
            </xsl:call-template>
          </xsl:for-each>
        </xsl:if>
        <xsl:if test="$iSuffixSlotMorphs &gt; 0">
          <xsl:for-each select="following-sibling::*[position()&lt;=$iSuffixSlotMorphs]">
            <xsl:call-template name="CheckInflectionalAffixProdRestrict">
              <xsl:with-param name="stemMorph" select="$stem" />
              <xsl:with-param name="inflMorph" select="." />
              <xsl:with-param name="sType" select="'suffix'" />
            </xsl:call-template>
            <xsl:call-template name="CheckAffixAllomorphFeatures">
              <xsl:with-param name="sAffix" select="'inflectional'" />
              <xsl:with-param name="sAttachesTo" select="'inflected form'" />
              <xsl:with-param name="morph" select="." />
              <xsl:with-param name="stem" select="msxsl:node-set($InflFeaturesToPercolate)" />
            </xsl:call-template>
          </xsl:for-each>
        </xsl:if>
        <xsl:if test="msxsl:node-set($InflFeaturesToPercolate)/fs/descendant::feature">
          <xsl:copy-of select="$InflFeaturesToPercolate" />
        </xsl:if>
        <xsl:variable name="StemNameConstraintResult">
          <xsl:call-template name="CheckStemNames5816">
            <xsl:with-param name="stemName" select="stemName/@id" />
            <xsl:with-param name="InflFeatures" select="msxsl:node-set($InflFeaturesToPercolate)" />
          </xsl:call-template>
        </xsl:variable>
        <xsl:if test="msxsl:node-set($StemNameConstraintResult)/failure">
          <xsl:copy-of select="msxsl:node-set($StemNameConstraintResult)" />
        </xsl:if>
        <xsl:if test="@blocksInflection!='-'">
          <failure>The inflectional template named 'verb' for category 'Verb' failed because the stem was built by a template that requires more derivation and there was no intervening derivation or compounding.</failure>
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
PartialInflectionalTemplate9819POS5816
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
  <xsl:template name="PartialInflectionalTemplate9819POS5816">
    <seq step="Try to apply inflectional template 'verb' on a Partial analysis node.  Apply it for category 'Verb'. The result will be another Partial analysis node.">
      <xsl:variable name="PrefixSlotMorphs">
        <xsl:call-template name="InflectionalTemplate9819PrefixSlot4018-0">
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
        <xsl:call-template name="InflectionalTemplate9819SuffixSlot2431-0">
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
          <xsl:text>5816</xsl:text>
        </xsl:attribute>
        <xsl:attribute name="syncatAbbr">
          <xsl:text>v</xsl:text>
        </xsl:attribute>
        <xsl:attribute name="requiresInfl">
          <xsl:text>-</xsl:text>
        </xsl:attribute>
        <xsl:attribute name="inflected">
          <xsl:text>+</xsl:text>
        </xsl:attribute>
        <xsl:if test="@blocksInflection!='-'">
          <failure>The inflectional template named 'verb' for category 'Verb' failed because the stem was built by a template that requires more derivation and there was no intervening derivation or compounding.</failure>
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
		 InflectionalTemplate9819PrefixSlot4018-0
	Params: morph - morpheme to check
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
  <xsl:template name="InflectionalTemplate9819PrefixSlot4018-0">
    <xsl:param name="morph" />
    <xsl:choose>
      <xsl:when test="$morph/@wordType = '4018'">
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
                <xsl:call-template name="InflectionalTemplate9819PrefixSlot8835-0">
                  <xsl:with-param name="morph" select="$morph/preceding-sibling::morph[1]" />
                </xsl:call-template>
              </xsl:when>
              <xsl:otherwise>
                <failure>The inflectional template named 'verb' for category 'Verb'<xsl:text> failed because in the </xsl:text>optional prefix<xsl:text> slot '</xsl:text>CAUS<xsl:text>', the inflection class of the stem (</xsl:text><xsl:value-of select="@inflClassAbbr" /><xsl:text>) does not match any of the inflection classes of the inflectional affix (</xsl:text><xsl:value-of select="$morph/shortName" /><xsl:text>).  The inflection class</xsl:text><xsl:choose><xsl:when test="count($morph/inflClasses/inflClass)=1"><xsl:text> of this affix is: </xsl:text></xsl:when><xsl:otherwise><xsl:text>es of this affix are: </xsl:text></xsl:otherwise></xsl:choose><xsl:for-each select="$morph/inflClasses/inflClass"><xsl:value-of select="@abbr" /><xsl:if test="position() != last()"><xsl:text>, </xsl:text></xsl:if></xsl:for-each><xsl:text>.</xsl:text></failure>
              </xsl:otherwise>
            </xsl:choose>
          </xsl:when>
          <xsl:otherwise>
            <!--No inflection classes to check; indicate success and try next slot (if any)-->
            <xsl:call-template name="IndicateInflAffixSucceeded" />
            <xsl:call-template name="InflectionalTemplate9819PrefixSlot8835-0">
              <xsl:with-param name="morph" select="$morph/preceding-sibling::morph[1]" />
            </xsl:call-template>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:when>
      <xsl:otherwise>
        <!--Optional slot does not match: try next slot (if any) with this morph-->
        <xsl:call-template name="InflectionalTemplate9819PrefixSlot8835-0">
          <xsl:with-param name="morph" select="$morph" />
        </xsl:call-template>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 InflectionalTemplate9819PrefixSlot8835-0
	Params: morph - morpheme to check
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
  <xsl:template name="InflectionalTemplate9819PrefixSlot8835-0">
    <xsl:param name="morph" />
    <xsl:choose>
      <xsl:when test="$morph/@wordType = '8835'">
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
                <failure>The inflectional template named 'verb' for category 'Verb'<xsl:text> failed because in the </xsl:text>required prefix<xsl:text> slot '</xsl:text>PERSNUMERG<xsl:text>', the inflection class of the stem (</xsl:text><xsl:value-of select="@inflClassAbbr" /><xsl:text>) does not match any of the inflection classes of the inflectional affix (</xsl:text><xsl:value-of select="$morph/shortName" /><xsl:text>).  The inflection class</xsl:text><xsl:choose><xsl:when test="count($morph/inflClasses/inflClass)=1"><xsl:text> of this affix is: </xsl:text></xsl:when><xsl:otherwise><xsl:text>es of this affix are: </xsl:text></xsl:otherwise></xsl:choose><xsl:for-each select="$morph/inflClasses/inflClass"><xsl:value-of select="@abbr" /><xsl:if test="position() != last()"><xsl:text>, </xsl:text></xsl:if></xsl:for-each><xsl:text>.</xsl:text></failure>
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
        <failure>The inflectional template named 'verb' for category 'Verb' failed because the required prefix slot 'PERSNUMERG' was not found.</failure>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 InflectionalTemplate9819SuffixSlot2431-0
	Params: morph - morpheme to check
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
  <xsl:template name="InflectionalTemplate9819SuffixSlot2431-0">
    <xsl:param name="morph" />
    <xsl:choose>
      <xsl:when test="$morph/@wordType = '2431'">
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
                <xsl:call-template name="InflectionalTemplate9819SuffixSlot904-0">
                  <xsl:with-param name="morph" select="$morph/following-sibling::morph[1]" />
                </xsl:call-template>
              </xsl:when>
              <xsl:otherwise>
                <failure>The inflectional template named 'verb' for category 'Verb'<xsl:text> failed because in the </xsl:text>optional suffix<xsl:text> slot '</xsl:text>PFV<xsl:text>', the inflection class of the stem (</xsl:text><xsl:value-of select="@inflClassAbbr" /><xsl:text>) does not match any of the inflection classes of the inflectional affix (</xsl:text><xsl:value-of select="$morph/shortName" /><xsl:text>).  The inflection class</xsl:text><xsl:choose><xsl:when test="count($morph/inflClasses/inflClass)=1"><xsl:text> of this affix is: </xsl:text></xsl:when><xsl:otherwise><xsl:text>es of this affix are: </xsl:text></xsl:otherwise></xsl:choose><xsl:for-each select="$morph/inflClasses/inflClass"><xsl:value-of select="@abbr" /><xsl:if test="position() != last()"><xsl:text>, </xsl:text></xsl:if></xsl:for-each><xsl:text>.</xsl:text></failure>
              </xsl:otherwise>
            </xsl:choose>
          </xsl:when>
          <xsl:otherwise>
            <!--No inflection classes to check; indicate success and try next slot (if any)-->
            <xsl:call-template name="IndicateInflAffixSucceeded" />
            <xsl:call-template name="InflectionalTemplate9819SuffixSlot904-0">
              <xsl:with-param name="morph" select="$morph/following-sibling::morph[1]" />
            </xsl:call-template>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:when>
      <xsl:otherwise>
        <!--Optional slot does not match: try next slot (if any) with this morph-->
        <xsl:call-template name="InflectionalTemplate9819SuffixSlot904-0">
          <xsl:with-param name="morph" select="$morph" />
        </xsl:call-template>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 InflectionalTemplate9819SuffixSlot904-0
	Params: morph - morpheme to check
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
  <xsl:template name="InflectionalTemplate9819SuffixSlot904-0">
    <xsl:param name="morph" />
    <xsl:choose>
      <xsl:when test="$morph/@wordType = '904'">
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
                <failure>The inflectional template named 'verb' for category 'Verb'<xsl:text> failed because in the </xsl:text>optional suffix<xsl:text> slot '</xsl:text>PERSNUMABS<xsl:text>', the inflection class of the stem (</xsl:text><xsl:value-of select="@inflClassAbbr" /><xsl:text>) does not match any of the inflection classes of the inflectional affix (</xsl:text><xsl:value-of select="$morph/shortName" /><xsl:text>).  The inflection class</xsl:text><xsl:choose><xsl:when test="count($morph/inflClasses/inflClass)=1"><xsl:text> of this affix is: </xsl:text></xsl:when><xsl:otherwise><xsl:text>es of this affix are: </xsl:text></xsl:otherwise></xsl:choose><xsl:for-each select="$morph/inflClasses/inflClass"><xsl:value-of select="@abbr" /><xsl:if test="position() != last()"><xsl:text>, </xsl:text></xsl:if></xsl:for-each><xsl:text>.</xsl:text></failure>
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
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  <!--
			- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
			CheckStemNames3072
			Params: none
			- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 -->
  <xsl:template name="CheckStemNames3072">
    <xsl:param name="stemName" />
    <xsl:param name="InflFeatures" />
  </xsl:template>
  <!--
			- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
			CheckStemNames7245
			Params: none
			- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 -->
  <xsl:template name="CheckStemNames7245">
    <xsl:param name="stemName" />
    <xsl:param name="InflFeatures" />
  </xsl:template>
  <!--
			- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
			CheckStemNames7072
			Params: none
			- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 -->
  <xsl:template name="CheckStemNames7072">
    <xsl:param name="stemName" />
    <xsl:param name="InflFeatures" />
  </xsl:template>
  <!--
			- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
			CheckStemNames6990
			Params: none
			- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 -->
  <xsl:template name="CheckStemNames6990">
    <xsl:param name="stemName" />
    <xsl:param name="InflFeatures" />
    <xsl:call-template name="CheckStemNames7072">
      <xsl:with-param name="stemName" select="$stemName" />
      <xsl:with-param name="InflFeatures" select="$InflFeatures" />
    </xsl:call-template>
  </xsl:template>
  <!--
			- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
			CheckStemNames5816
			Params: none
			- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 -->
  <xsl:template name="CheckStemNames5816">
    <xsl:param name="stemName" />
    <xsl:param name="InflFeatures" />
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
    <xsl:choose>
      <xsl:when test="$fs/feature">
        <xsl:text disable-output-escaping="yes">[</xsl:text>
        <xsl:for-each select="$fs/feature">
          <xsl:if test="position()!=1">
            <xsl:text disable-output-escaping="yes"> </xsl:text>
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
      </xsl:when>
      <xsl:otherwise>
        <xsl:text>(none)</xsl:text>
      </xsl:otherwise>
    </xsl:choose>
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
		 CheckInflectionalAffixProdRestrict
		 Check for valid productivity restrictions between the stem and an inflectional affix
		 Parameters: stemMorph = stem morpheme
		 inlfMorph = inflectional affix
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
  <xsl:template name="CheckInflectionalAffixProdRestrict">
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
          <xsl:with-param name="sFirstItemDescription">inflectional <xsl:value-of select="$sType" /></xsl:with-param>
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
		 CheckAffixAllomorphFeatures
		 Check for valid affix allomorph features
		 Parameters: stemMorph = stem morpheme
		 inlfMorph = inflectional affix
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  -->
  <xsl:template name="CheckAffixAllomorphFeatures">
    <xsl:param name="sAffix" select="'An'" />
    <xsl:param name="sAttachesTo" select="stem" />
    <xsl:param name="morph" />
    <xsl:param name="stem" />
    <xsl:variable name="affixAlloFeats" select="$morph/affixAlloFeats/fs" />
    <xsl:variable name="notAffixAlloFeats" select="$morph/affixAlloFeats/not" />
    <xsl:variable name="sAffixAlloFeatsSubsumeInflFeatures">
      <xsl:choose>
        <xsl:when test="$affixAlloFeats">
          <xsl:call-template name="XSubsumesY">
            <xsl:with-param name="X" select="msxsl:node-set($affixAlloFeats)" />
            <xsl:with-param name="Y" select="msxsl:node-set($stem)/fs" />
          </xsl:call-template>
        </xsl:when>
        <xsl:when test="$notAffixAlloFeats">
          <xsl:for-each select="$notAffixAlloFeats/fs">
            <xsl:call-template name="XSubsumesY">
              <xsl:with-param name="X" select="msxsl:node-set(.)" />
              <xsl:with-param name="Y" select="msxsl:node-set($stem)/fs" />
            </xsl:call-template>
          </xsl:for-each>
        </xsl:when>
        <xsl:otherwise>
          <xsl:text>Y</xsl:text>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:variable>
    <xsl:choose>
      <xsl:when test="contains($sAffixAlloFeatsSubsumeInflFeatures,'N') and $affixAlloFeats">
        <!--The affix itself had features but they do not match the features of what it attaches to.-->
        <failure>
          <xsl:text>The </xsl:text>
          <xsl:value-of select="$sAffix" />
          <xsl:text> affix allomorph '</xsl:text>
          <xsl:value-of select="$morph/shortName" />
          <xsl:text>' is conditioned to only occur when the </xsl:text>
          <xsl:value-of select="$sAttachesTo" />
          <xsl:text> it attaches to has certain features, but the </xsl:text>
          <xsl:value-of select="$sAttachesTo" />
          <xsl:text> does not have them.  The required features the affix must be inflected for are: </xsl:text>
          <xsl:for-each select="$affixAlloFeats">
            <xsl:call-template name="OutputFeatureStructureAsText">
              <xsl:with-param name="fs" select="." />
            </xsl:call-template>
          </xsl:for-each>
          <xsl:text>.  The inflected features for this </xsl:text>
          <xsl:value-of select="$sAttachesTo" />
          <xsl:text> are: </xsl:text>
          <xsl:for-each select="msxsl:node-set($stem)">
            <xsl:call-template name="OutputFeatureStructureAsText">
              <xsl:with-param name="fs" select="fs" />
            </xsl:call-template>
          </xsl:for-each>
          <xsl:text>.</xsl:text>
        </failure>
      </xsl:when>
      <xsl:when test="contains($sAffixAlloFeatsSubsumeInflFeatures,'Y') and $notAffixAlloFeats">
        <!--Other affixes in the entry had features, but this allomorph meets one of them, so it is a failure (the other allomorph should be used).-->
        <failure>
          <xsl:text>While the </xsl:text>
          <xsl:value-of select="$sAffix" />
          <xsl:text> affix allomorph '</xsl:text>
          <xsl:value-of select="$morph/shortName" />
          <xsl:text>' is not conditioned to occur when the </xsl:text>
          <xsl:value-of select="$sAttachesTo" />
          <xsl:text> it attaches to has certain features, there are other allomorphs in the entry that are so conditioned.  Thus, the </xsl:text>
          <xsl:value-of select="$sAttachesTo" />
          <xsl:text> must not be inflected for certain features, but it is.  The features the affix must not be inflected for are: </xsl:text>
          <xsl:for-each select="$notAffixAlloFeats/fs">
            <xsl:call-template name="OutputFeatureStructureAsText">
              <xsl:with-param name="fs" select="." />
            </xsl:call-template>
            <xsl:if test="position()!=last()">
              <xsl:text> and also </xsl:text>
            </xsl:if>
          </xsl:for-each>
          <xsl:text>.  The inflected features for this </xsl:text>
          <xsl:value-of select="$sAttachesTo" />
          <xsl:text> are: </xsl:text>
          <xsl:for-each select="msxsl:node-set($stem)">
            <xsl:call-template name="OutputFeatureStructureAsText">
              <xsl:with-param name="fs" select="fs" />
            </xsl:call-template>
          </xsl:for-each>
          <xsl:text>.</xsl:text>
        </failure>
      </xsl:when>
    </xsl:choose>
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
          <xsl:when test="failure">
            <!--only one of the features has this feature and there was a failure-->
            <feature>
              <name>
                <xsl:value-of select="name" />
              </name>
              <xsl:copy-of select="failure" />
            </feature>
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
        <xsl:variable name="sResult">
          <!--loop through the features of both feature structures at same time, sorted by name-->
          <xsl:for-each select="msxsl:node-set($X)/feature | msxsl:node-set($Y)/feature">
            <xsl:sort select="name" />
            <!--get name of this feature-->
            <xsl:variable name="sName">
              <xsl:value-of select="name" />
            </xsl:variable>
            <!--get this feature if it's in the first feature structure-->
            <xsl:variable name="f1SameName" select="msxsl:node-set($X)/feature[name=$sName]" />
            <!--get this feature if it's in the second feature structure-->
            <xsl:variable name="f2SameName" select="msxsl:node-set($Y)/feature[name=$sName]" />
            <xsl:choose>
              <xsl:when test="$f1SameName and $f2SameName">
                <!--both feature1 and feature2 have this feature name-->
                <xsl:choose>
                  <xsl:when test="$f1SameName/value/fs and $f2SameName/value/fs">
                    <!--both have nested feature structure-->
                    <xsl:call-template name="XSubsumesY">
                      <xsl:with-param name="X" select="$f1SameName/value/fs" />
                      <xsl:with-param name="Y" select="$f2SameName/value/fs" />
                    </xsl:call-template>
                  </xsl:when>
                  <xsl:when test="$f1SameName/value=$f2SameName/value">
                    <!--both features have the same value; is good-->
                    <xsl:text>Y</xsl:text>
                  </xsl:when>
                  <xsl:otherwise>
                    <!--there's a value conflict, so fail-->
                    <xsl:text>N</xsl:text>
                  </xsl:otherwise>
                </xsl:choose>
              </xsl:when>
              <xsl:when test="$f1SameName and not($f2SameName)">
                <!--X has it, but Y does not, so fail-->
                <xsl:text>N</xsl:text>
              </xsl:when>
              <xsl:otherwise>
                <!--only Y has this feature and that's OK.  Do nothing.-->
              </xsl:otherwise>
            </xsl:choose>
          </xsl:for-each>
        </xsl:variable>
        <xsl:choose>
          <xsl:when test="contains($sResult, 'N')">
            <xsl:text>N</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Y</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
</xsl:stylesheet>