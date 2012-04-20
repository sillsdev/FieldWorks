<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
   <xsl:output method="text" version="1.0" encoding="UTF-8" indent="yes"/>
   <!--
================================================================
M3 to XAmple Analysis Data Control File mapper for Stage 1.
  Input:    XML output from M3ParserSvr which has been passed through CleanFWDump.xslt
  Output: XAmple Analysis Data Control file
================================================================
Revision History is at the end of this file.

- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Preamble
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <!-- Using keys instead of IDs (so no DTD or XSD required) -->
   <xsl:key name="LexSenseID" match="LexSense" use="@Id"/>
   <xsl:key name="MSAID" match="MorphoSyntaxAnalysis" use="@dst"/>
   <xsl:key name="POSID" match="PartOfSpeech" use="@Id"/>
	<!-- global variables (to improve efficiency) -->
	<xsl:variable name="LexEntries" select="/M3Dump/Lexicon/Entries/LexEntry"/>
	<xsl:variable name="MoStemMsas" select="/M3Dump/Lexicon/MorphoSyntaxAnalyses/MoStemMsa"/>
	<xsl:variable name="MoInflAffMsas" select="/M3Dump/Lexicon/MorphoSyntaxAnalyses/MoInflAffMsa"/>
	<xsl:variable name="MoDerivAffMsas" select="/M3Dump/Lexicon/MorphoSyntaxAnalyses/MoDerivAffMsa"/>
	<xsl:variable name="MoUnclassifiedAffixMsas" select="/M3Dump/Lexicon/MorphoSyntaxAnalyses/MoUnclassifiedAffixMsa"/>

	<!-- MoInflClass may be nested -->
	<xsl:variable name="MoInflClasses" select="/M3Dump/PartsOfSpeech/PartOfSpeech/InflectionClasses//MoInflClass"/>
	<xsl:variable name="MoStemNames" select="/M3Dump/PartsOfSpeech/PartOfSpeech/StemNames/MoStemName"/>
	<xsl:variable name="MoAlloAdhocProhibs" select="/M3Dump/AdhocCoProhibitions/MoAlloAdhocProhib"/>
	<xsl:variable name="MoMorphAdhocProhibs" select="/M3Dump/AdhocCoProhibitions/MoMorphAdhocProhib"/>

	<xsl:variable name="ProdRestricts" select="/M3Dump/ProdRestrict"/>
	<xsl:variable name="MoAffixAllomorphs" select="/M3Dump/Lexicon/Allomorphs/MoAffixAllomorph"/>
	<xsl:variable name="PartsOfSpeech" select="/M3Dump/PartsOfSpeech/PartOfSpeech"/>
	<xsl:variable name="MoEndoCompounds" select="/M3Dump/CompoundRules/MoEndoCompound"/>
	<xsl:variable name="MoExoCompounds" select="/M3Dump/CompoundRules/MoExoCompound"/>
	<xsl:variable name="MoMorphTypes" select="/M3Dump/MoMorphTypes/MoMorphType"/>
	<!-- Old files have a single level of ParserParameters.  New output may have two levels (ParserParameters/ParserParameters). -->
	<xsl:variable name="XAmple" select="/M3Dump/ParserParameters//XAmple"/>
	<!-- included stylesheets (i.e. things common to other style sheets) -->
	<xsl:include href="MorphTypeGuids.xsl"/>
   <xsl:include href="CalculateStemNamesUsedInLexicalEntries.xsl"/>
   <xsl:include href="XAmpleTemplateVariables.xsl"/>
   <!-- Parameters that can be set by user.  -->
   <xsl:param name="prmMaxInfixes">
	  <xsl:choose>
		 <xsl:when test="$XAmple/MaxInfixes">
			 <xsl:value-of select="$XAmple/MaxInfixes"/>
		 </xsl:when>
		 <xsl:when test="$MoMorphTypes[Name='infix']">1</xsl:when>
		 <xsl:otherwise>0</xsl:otherwise>
	  </xsl:choose>
   </xsl:param>
   <xsl:param name="prmMaxNull">
	  <xsl:choose>
		 <xsl:when test="$XAmple/MaxNulls">
			<xsl:value-of select="$XAmple/MaxNulls"/>
		 </xsl:when>
		 <xsl:otherwise>1</xsl:otherwise>
	  </xsl:choose>
   </xsl:param>
   <xsl:param name="prmMaxPrefixes">
	  <xsl:choose>
		 <xsl:when test="$XAmple/MaxPrefixes">
			<xsl:value-of select="$XAmple/MaxPrefixes"/>
		 </xsl:when>
		 <xsl:otherwise>5</xsl:otherwise>
	  </xsl:choose>
   </xsl:param>
   <xsl:param name="prmMaxRoots">
	  <xsl:choose>
		 <xsl:when test="$XAmple/MaxRoots">
			<xsl:value-of select="$XAmple/MaxRoots"/>
		 </xsl:when>
		 <xsl:when test="$MoEndoCompounds | $MoExoCompounds">
			<xsl:choose>
			   <xsl:when test="$MoEndoCompounds/Linker | $MoExoCompounds/Linker">3</xsl:when>
			   <xsl:otherwise>2</xsl:otherwise>
			</xsl:choose>
		 </xsl:when>
		 <xsl:otherwise>1</xsl:otherwise>
	  </xsl:choose>
   </xsl:param>
   <xsl:param name="prmMaxSuffixes">
	  <xsl:choose>
		 <xsl:when test="$XAmple/MaxSuffixes">
			<xsl:value-of select="$XAmple/MaxSuffixes"/>
		 </xsl:when>
		 <xsl:otherwise>5</xsl:otherwise>
	  </xsl:choose>
   </xsl:param>
   <xsl:param name="prmMaxInterfixes">
	  <xsl:choose>
		 <xsl:when test="$XAmple/MaxInterfixes">
			<xsl:variable name="sValue" select="$XAmple/MaxInterfixes"/>
			<xsl:choose>
			   <xsl:when test="$sValue!=0">
				  <xsl:value-of select="$sValue"/>
			   </xsl:when>
			   <xsl:otherwise>
				  <xsl:call-template name="DetermineMaxInterfixes"/>
			   </xsl:otherwise>
			</xsl:choose>
		 </xsl:when>
		 <xsl:otherwise>
			<xsl:call-template name="DetermineMaxInterfixes"/>
		 </xsl:otherwise>
	  </xsl:choose>
   </xsl:param>
   <xsl:param name="bMorphnameIsMsaId">y</xsl:param>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Main template
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template match="/">
\ca W Prt Linker
\cr W W
<xsl:if test="$MoEndoCompounds/Linker | $MoExoCompounds/Linker">
\cr W Linker
\cr Linker W
</xsl:if>
	  <!-- Need to create a list of all unique multi-inflection class combinations -->
	  <xsl:variable name="sAllMultiInflClasses">
		 <xsl:for-each select="$MoAffixAllomorphs[count(InflectionClasses)&gt;1]">
			<xsl:for-each select="InflectionClasses">
				<xsl:value-of select="$sInflClassAffix"/>
			   <xsl:value-of select="@dst"/>
			</xsl:for-each>
			<xsl:text>&#x20;</xsl:text>
		 </xsl:for-each>
	  </xsl:variable>
	  <xsl:variable name="sUniqueMultiInflClasses">
		 <xsl:call-template name="OutputUniqueStrings">
			<xsl:with-param name="sList" select="$sAllMultiInflClasses"/>
		 </xsl:call-template>
	  </xsl:variable>
	  <!-- now we need to count them so we get the maxprops parameter to be correct -->
	  <xsl:variable name="iCountUniqueMultiInflClasses">
		 <xsl:call-template name="CountInstancesOfStringInString">
			<xsl:with-param name="sString" select="$sUniqueMultiInflClasses"/>
			<xsl:with-param name="sStringToLookFor">
			   <xsl:text>&#x20;</xsl:text>
			</xsl:with-param>
		 </xsl:call-template>
	  </xsl:variable>
	  <!-- Need to create a list of all unique stem name combinations used in lexical entries (done in include above) -->
	  <!-- now we need to count them so we get the maxprops parameter to be correct -->
	  <xsl:variable name="iCountUniqueStemNamesUsedInLexicalEntries">
		 <xsl:call-template name="CountInstancesOfStringInString">
			<xsl:with-param name="sString" select="$sUniqueStemNamesUsedInLexicalEntries"/>
			<xsl:with-param name="sStringToLookFor">
			   <xsl:text>&#x20;</xsl:text>
			</xsl:with-param>
		 </xsl:call-template>
	  </xsl:variable>
	  <!-- Need to create a list of all MoAffixAllomorphs which are constrained by features  -->
	  <!-- now we need to count them so we get the maxprops parameter to be correct -->
	  <xsl:variable name="sAllosNotConditionedByFeatures">
		 <xsl:call-template name="AllosNotConditionedByFeats"/>
	  </xsl:variable>
	  <!-- to get  the count is a bit of a trick: first remove all non-spaces in the string and then count the spaces -->
	  <xsl:variable name="sAllosNotConditionedByFeaturesCount" select="translate($sAllosNotConditionedByFeatures, 'MSEnvFSNot0123456789', '')"/>
	  <xsl:variable name="iAllosNotConditionedByFeaturesCount" select="string-length($sAllosNotConditionedByFeaturesCount)"/>

\maxnull <xsl:value-of select="$prmMaxNull"/>
	  <xsl:variable name="dPropCount">
		  <xsl:value-of select="2*count($PartsOfSpeech) + count($MoAffixAllomorphs/MsEnvFeatures/FsFeatStruc[descendant::FsClosedValue]) + $iAllosNotConditionedByFeaturesCount + 3*count($MoInflClasses) +
							number($iCountUniqueMultiInflClasses) + 3*count($ProdRestricts/CmPossibility) +  3*count($MoStemNames) +
							number($iCountUniqueStemNamesUsedInLexicalEntries) + 4"/>
	  </xsl:variable>
	  <!--    dPropCount = <xsl:value-of select="$dPropCount"/> -->
\maxprops <xsl:choose>
		 <xsl:when test="$dPropCount > 255">
			<xsl:value-of select="$dPropCount"/>
		 </xsl:when>
		 <xsl:otherwise>255</xsl:otherwise>
	  </xsl:choose>
\maxp <xsl:value-of select="$prmMaxPrefixes"/>
\maxi <xsl:value-of select="$prmMaxInfixes"/>
\maxs <xsl:value-of select="$prmMaxSuffixes"/>
\maxr <xsl:value-of select="$prmMaxRoots"/>
\maxn <xsl:value-of select="$prmMaxInterfixes"/>
	  <!--
morpheme classes
-->
	  <xsl:if test="$MoStemNames">
		 <xsl:text>
\mcl DerivAffix </xsl:text>
		 <xsl:variable name="derivAffixes" select="$MoDerivAffMsas[@FromPartOfSpeech!='0' and @ToPartOfSpeech!='0']"/>
		 <xsl:choose>
			<xsl:when test="$derivAffixes">
			   <xsl:for-each select="$MoDerivAffMsas[@FromPartOfSpeech!='0' and @ToPartOfSpeech!='0']">
				  <xsl:value-of select="@Id"/>
				  <xsl:text>&#x20;</xsl:text>
			   </xsl:for-each>
			</xsl:when>
			<xsl:otherwise>FAKE</xsl:otherwise>
		 </xsl:choose>
	  </xsl:if>
	  <!--
properties
-->
\mp Proclitic Enclitic
\mp <xsl:value-of select="$sRootPOS"/>0<xsl:for-each select="$PartsOfSpeech">
   <xsl:text>&#x20;</xsl:text>
   <xsl:value-of select="$sRootPOS"/><xsl:value-of select="@Id"/>
	  </xsl:for-each>
	  <xsl:for-each select="$MoInflClasses">
		 <xsl:text>&#x20;</xsl:text>
		 <xsl:value-of select="$sToInflClass"/><xsl:value-of select="@Id"/>
	  </xsl:for-each>
	  <xsl:for-each select="$ProdRestricts/CmPossibility">
		 <xsl:text>&#x20;</xsl:text>
		 <xsl:value-of select="$sExceptionFeature"/>
		 <xsl:value-of select="@Id"/>
		 <xsl:text>Plus </xsl:text>
		 <xsl:value-of select="$sFromExceptionFeature"/>
		 <xsl:value-of select="@Id"/>
		 <xsl:text>Plus </xsl:text>
		 <xsl:value-of select="$sToExceptionFeature"/>
		 <xsl:value-of select="@Id"/>
		 <xsl:text>Plus</xsl:text>
	  </xsl:for-each>
	   <xsl:for-each select="$MoStemNames">
		   <xsl:text>&#x20;</xsl:text>
		   <xsl:value-of select="$sStemName"/><xsl:text>Affix</xsl:text><xsl:value-of select="@Id"/>
	   </xsl:for-each>
\ap Bound <xsl:for-each select="$PartsOfSpeech">
   <xsl:text>&#x20;</xsl:text><xsl:value-of select="$sMSEnvPOS"/><xsl:value-of select="@Id"/>
	  </xsl:for-each>
	   <xsl:for-each select="$MoAffixAllomorphs/MsEnvFeatures/FsFeatStruc[descendant::FsClosedValue]"><xsl:text>&#x20;</xsl:text><xsl:value-of select="$sMSEnvFS"/><xsl:value-of select="@Id"/>
	  </xsl:for-each>
	  <xsl:value-of select="$sAllosNotConditionedByFeatures"/>

	  <!-- singleton inflection classes (from individual msas) -->
	  <xsl:for-each select="$MoInflClasses">
		 <xsl:text>&#x20;</xsl:text>
		 <xsl:value-of select="$sInflClass"/>
		 <xsl:value-of select="@Id"/>
		 <xsl:text>&#x20;</xsl:text>
		  <xsl:value-of select="$sInflClassAffix"/>
		 <xsl:value-of select="@Id"/>
	  </xsl:for-each>
	  <!-- combination inflection classes (from MoAffixAllomorph) -->
	  <xsl:text>&#x20;</xsl:text>
	  <xsl:value-of select="$sUniqueMultiInflClasses"/>
	  <xsl:for-each select="$MoStemNames">
		 <xsl:text>&#x20;</xsl:text>
		 <xsl:value-of select="$sStemName"/><xsl:value-of select="@Id"/>
	  </xsl:for-each>
	  <xsl:text>&#x20;</xsl:text>
	  <xsl:value-of select="$sUniqueStemNamesUsedInLexicalEntries"/>
	  <!--
If an msa has a component attr, we want the only analysis to be the combined one; that is we want
to disallow the component analysis itself.  we'll use an \mcc to do that.
-->
	  <xsl:for-each select="$MoStemMsas[Components] | $MoDerivAffMsas[Components] | $MoInflAffMsas[Components] | $MoUnclassifiedAffixMsas[Components]">
		 <xsl:for-each select="Components">
			<xsl:variable name="Value">
			   <xsl:choose>
				  <xsl:when test="$bMorphnameIsMsaId!='y'">
					 <xsl:value-of select="key('LexSenseID', key('MSAID',@dst)/../Senses/@dst)/Gloss"/>
				  </xsl:when>
				  <xsl:otherwise>
					 <xsl:value-of select="@dst"/>
				  </xsl:otherwise>
			   </xsl:choose>
			</xsl:variable>
			<xsl:choose>
			   <xsl:when test="@ord='0'">
				  <xsl:text>
\mcc </xsl:text>
				  <xsl:value-of select="$Value"/>
				  <xsl:text> +/ ~_</xsl:text>
			   </xsl:when>
			   <xsl:otherwise>
				  <xsl:text>&#x20;</xsl:text>
				  <xsl:value-of select="$Value"/>
			   </xsl:otherwise>
			</xsl:choose>
		 </xsl:for-each>
	  </xsl:for-each>
	  <!--
Morpheme ad hoc constraints
	for the various values of Adjacency, produce:
		Anywhere:                XXX +/ ZZZ ... YYY ... _
										  XXX +/                         _ ... YYY ... ZZZ
		Somewhere to left:    XXX +/ ZZZ ... YYY ... _
		Somewhere to right:  XXX +/                         _ ... YYY ... ZZZ
		Adjacent to left:          XXX +/ ZZZ YYY _
		Adjacent to right:        XXX +/                 _ YYY ZZZ
-->
	  <xsl:for-each select="$MoMorphAdhocProhibs">
		 <xsl:variable name="iAdjacency">
			<xsl:value-of select="@Adjacency"/>
		 </xsl:variable>
		 <xsl:variable name="sEllipsis">
			<xsl:call-template name="SetEllipsisValue">
			   <xsl:with-param name="iAdjacency">
				  <xsl:value-of select="$iAdjacency"/>
			   </xsl:with-param>
			</xsl:call-template>
		 </xsl:variable>
		 <xsl:variable name="FirstMorph">
			<xsl:value-of select="FirstMorpheme/@dst"/>
		 </xsl:variable>
		 <xsl:variable name="sBeginning">
			<xsl:variable name="FirstSense" select="key('LexSenseID', key('MSAID',$FirstMorph)/../Senses/@dst)/Gloss"/>
			<xsl:text>
\mcc </xsl:text>
			<xsl:choose>
			   <xsl:when test="$bMorphnameIsMsaId='y'">
				  <xsl:value-of select="$FirstMorph"/>
			   </xsl:when>
			   <xsl:otherwise>
				  <xsl:value-of select="$FirstSense"/>
			   </xsl:otherwise>
			</xsl:choose>
			<xsl:text> +/ </xsl:text>
		 </xsl:variable>
		 <xsl:value-of select="$sBeginning"/>
		 <xsl:if test="$iAdjacency='0' or $iAdjacency='1' or $iAdjacency='3'">
			<!-- anywhere or (somewhere or anywhere to the left) -->
			<xsl:for-each select="RestOfMorphs">
			   <xsl:sort select="position()" order="descending"/>
			   <xsl:variable name="NextSense" select="key('LexSenseID', key('MSAID',@dst)/../Senses/@dst)/Gloss"/>
			   <xsl:choose>
				  <xsl:when test="$bMorphnameIsMsaId='y'">
					 <xsl:value-of select="@dst"/>
				  </xsl:when>
				  <xsl:otherwise>
					 <xsl:value-of select="$NextSense"/>
				  </xsl:otherwise>
			   </xsl:choose>
			   <xsl:value-of select="$sEllipsis"/>
			</xsl:for-each>
			<xsl:text> ~_ </xsl:text>
		 </xsl:if>
		 <xsl:if test="$iAdjacency='0' or $iAdjacency='2' or $iAdjacency='4'">
			<!-- anywhere or (somewhere or anywhere to the right) -->
			<xsl:if test="$iAdjacency='0'">
			   <xsl:value-of select="$sBeginning"/>
			</xsl:if>
			<xsl:text> ~_ </xsl:text>
			<xsl:for-each select="RestOfMorphs">
			   <xsl:value-of select="$sEllipsis"/>
			   <xsl:variable name="NextSense" select="key('LexSenseID', key('MSAID',@dst)/../Senses/@dst)/Gloss"/>
			   <xsl:choose>
				  <xsl:when test="$bMorphnameIsMsaId='y'">
					 <xsl:value-of select="@dst"/>
				  </xsl:when>
				  <xsl:otherwise>
					 <xsl:value-of select="$NextSense"/>
				  </xsl:otherwise>
			   </xsl:choose>
			</xsl:for-each>
		 </xsl:if>
	  </xsl:for-each>
	  <!--
Allomorph ad hoc constraints
	for the various values of Adjacency, produce:
		Anywhere:                XXX -/ ZZZ ... YYY ... _
										  XXX -/                         _ ... YYY ... ZZZ
		Somewhere to left:    XXX -/ ZZZ ... YYY ... _
		Somewhere to right:  XXX -/                         _ ... YYY ... ZZZ
		Adjacent to left:          XXX -/ ZZZ YYY _
		Adjacent to right:        XXX -/                 _ YYY ZZZ
-->
	  <xsl:for-each select="$MoAlloAdhocProhibs">
		 <xsl:variable name="iAdjacency">
			<xsl:value-of select="@Adjacency"/>
		 </xsl:variable>
		 <xsl:variable name="sEllipsis">
			<xsl:call-template name="SetEllipsisValue">
			   <xsl:with-param name="iAdjacency">
				  <xsl:value-of select="$iAdjacency"/>
			   </xsl:with-param>
			</xsl:call-template>
		 </xsl:variable>
		 <xsl:variable name="sBeginning">
			<xsl:text>
\ancc </xsl:text>
			<xsl:value-of select="FirstAllomorph/@dst"/>
			<!--				<xsl:text> -/ </xsl:text> -->
			<xsl:text> / </xsl:text>
		 </xsl:variable>
		 <xsl:value-of select="$sBeginning"/>
		 <xsl:if test="$iAdjacency='0' or $iAdjacency='1' or $iAdjacency='3'">
			<!-- anywhere or (somewhere or anywhere to the left) -->
			<xsl:for-each select="RestOfAllos">
			   <xsl:sort select="position()" order="descending"/>
			   <xsl:value-of select="@dst"/>
			   <xsl:value-of select="$sEllipsis"/>
			</xsl:for-each>
			<xsl:text>_</xsl:text>
		 </xsl:if>
		 <xsl:if test="$iAdjacency='0' or $iAdjacency='2' or $iAdjacency='4'">
			<!-- anywhere or (somewhere or anywhere to the right) -->
			<xsl:if test="$iAdjacency='0'">
			   <xsl:value-of select="$sBeginning"/>
			</xsl:if>
			<xsl:text>_</xsl:text>
			<xsl:for-each select="RestOfAllos">
			   <xsl:value-of select="$sEllipsis"/>
			   <xsl:value-of select="@dst"/>
			</xsl:for-each>
		 </xsl:if>
	  </xsl:for-each>
	  <!--
String Classes (which correspond to natural classes
-->
	  <xsl:for-each select="/M3Dump/PhPhonData/NaturalClasses/PhNCSegments">
\scl <xsl:value-of select="@Id"/>
		 <xsl:text> | </xsl:text>
		 <xsl:value-of select="Abbreviation"/>
		 <!--      <xsl:text>&#x0a;&#x0a;</xsl:text> Don't use &#x0a; - it is just a \n, but on Windows need \r\n -->
		 <!-- use of space (&#x20;) is to keep XmlSpy from reformatting the whitespace -->
		 <xsl:text>
&#x20;
</xsl:text>
		 <xsl:for-each select="Segments">
			<xsl:text>&#x20;</xsl:text>
			<xsl:variable name="Code">
			   <xsl:value-of select="@dst"/>
			</xsl:variable>
			<xsl:for-each select="/M3Dump/PhPhonData/PhonemeSets/PhPhonemeSet/Phonemes/PhPhoneme[@Id=$Code]">
			   <xsl:for-each select="Codes/PhCode">
				  <xsl:value-of select="Representation"/>
				  <xsl:if test="position()!=last()">
					 <xsl:text>&#x20;</xsl:text>
				  </xsl:if>
			   </xsl:for-each>
			</xsl:for-each>
		 </xsl:for-each>
	  </xsl:for-each>
	  <!--
User tests
-->
\pt SEC_ST
\pt OrderPfx_ST
	(    (left orderclassmin &lt; current orderclassmin)
	AND (left orderclassmax &lt; current orderclassmax) )
	OR (current orderclass = 0)
	OR ((current orderclass = -1) AND (left orderclass = -1))
OR ((current orderclass = -32000) AND (left orderclass = -32000))
OR ((current orderclassmin = -31999) AND (current orderclassmax = -1))
OR ((left orderclassmin = -31999) AND (left orderclassmax = -1))
OR ((left orderclass = -1) AND (current orderclass ~= -32000)) | allow derivation outside inflection, but not outside clitics
<!-- following needed to avoid affixes appearing on particles -->
\pt Category (left tocategory is current fromcategory)
\it SEC_ST
\it OrderIfx_ST
	(    (left orderclassmin &lt; current orderclassmin)
	AND (left orderclassmax &lt; current orderclassmax) )
	OR (current orderclass = 0)
	OR ((current orderclass = -1) AND (left orderclass = -1))
OR ((current orderclass = -32000) AND (left orderclass = -32000))
OR ((current orderclassmin = -31999) AND (current orderclassmax = -1))
OR ((left orderclassmin = -31999) AND (left orderclassmax = -1))
OR ((left orderclass = -1) AND (current orderclass ~= -32000)) | allow derivation outside inflection, but not outside clitics
OR ((current orderclass = 1) AND (left orderclass ~= 32000)) | allow derivation outside inflection, but not outside clitics
\it Category
\nt InterfixType_ST
	 NOT (    (left    type is interfixprefix)
		  AND (current type is interfixsuffix)
		 )
\rt SEC_ST
\co only proclitics can occur left of a particle
\rt RootCategory_ST
  IF (current tocategory is Prt)
THEN (left property is Proclitic)
<!--
\rt Category<xsl:if test="$prmMaxRoots>1 and $MoEndoCompounds or $MoExoCompounds">
-->
	  <!-- compound rules -->
	  <!-- Is too restrictive: can have root1 root2 suf1 suf2 ... sufn, where sufn's tocat is the correct category of the right member of the compound.  This approach here will rule this out if root2 is not the correct category of the right member.
\rt Compound
IF (    (left type is root)
	AND (current type is root)
   )
THEN
   (<xsl:for-each select="$MoEndoCompounds | $MoExoCompounds">
		<xsl:variable name="LeftPOS" select="LeftMsa/MoStemMsa/@PartOfSpeech"/>
		<xsl:variable name="RightPOS" select="RightMsa/MoStemMsa/@PartOfSpeech"/>
		<xsl:if test="position()=1">
		  <xsl:text>&#x20;&#x20;&#x20;</xsl:text>
		</xsl:if>
		<xsl:if test="position()!=1">    OR </xsl:if>(    (   (left property is RootPOS<xsl:value-of select="$LeftPOS"/>)
	  <xsl:call-template name="NestedPOSids">
		  <xsl:with-param name="pos">
			<xsl:value-of select="$LeftPOS"/>
		  </xsl:with-param>
		  <xsl:with-param name="bLeft" select="'Y'"/>
		</xsl:call-template>      )
		AND (   (current property is RootPOS<xsl:value-of select="$RightPOS"/>)
	  <xsl:call-template name="NestedPOSids">
		  <xsl:with-param name="pos">
			<xsl:value-of select="$RightPOS"/>
		  </xsl:with-param>
		  <xsl:with-param name="bLeft" select="'N'"/>
		</xsl:call-template>      )
	   )
</xsl:for-each>   ) -->
	  <!--		</xsl:if> -->
\st SEC_ST
\st OrderSfx_ST
(    (left orderclassmin &lt; current orderclassmin)
AND (left orderclassmax &lt; current orderclassmax) )
OR (current orderclass = 0)
OR ((current orderclass = 1) AND (left orderclass = 1))
OR ((current orderclass = 32000) AND (left orderclass = 32000))
OR ((current orderclassmin = 1) AND (current orderclassmax = 31999))
OR ((left orderclassmin = 1) AND (left orderclassmax = 31999))
OR ((current orderclass = 1) AND (left orderclass ~= 32000)) | allow derivation outside inflection, but not outside clitics
\st SuffixCategory_ST
   (left tocategory is current fromcategory)
OR
   | only enclitics can go on particles
   (  IF (left tocategory is Prt)
	THEN (current property is Enclitic)
   )
\ft OrderFinal_FT
IF   (    (current orderclass = 0)
	  AND (NOT (current type is root))
	  AND (FOR_SOME_LEFT  (LEFT  orderclass ~= 0))
	  AND (FOR_SOME_RIGHT (RIGHT orderclass ~= 0))
	 )
THEN (   (LEFT orderclass &lt;= RIGHT orderclass)
	  OR (    (LEFT  orderclass = -1)
		  AND (RIGHT orderclass ~= -32000)
		  )
	  OR (    (RIGHT orderclass = 1)
		  AND (LEFT  orderclass ~= 32000)
		  )
	 )
\ft BoundStemOrRoot_FT
IF   (current property is Bound)
THEN (NOT (    (current type is initial)
		   AND (current type is final))
	 )
\ft MCC_FT
<!-- For efficiency purposes, we want the minimal output -->
\patr TreeStyle none
\patr ShowGlosses Off
\patr ShowFeatures On
</xsl:template>
   <!--
	  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  AllosNotConditionedByFeats
	  Routine to create strings of "Not" values for entries having allos conditioned by features and allos not so conditioned
	  Parameters: none
	  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
   -->
   <xsl:template name="AllosNotConditionedByFeats">
	  <xsl:for-each select="$LexEntries">
		 <xsl:variable name="allomorphs" select="AlternateForms | LexemeForm"/>
		  <xsl:variable name="allosConditionedByFeatures" select="$MoAffixAllomorphs[@Id=$allomorphs/@dst and MsEnvFeatures/FsFeatStruc[descendant::FsClosedValue]]"/>
		 <xsl:if test="count($allosConditionedByFeatures) &gt; 0">
			<xsl:text>&#x20;</xsl:text>
			<xsl:value-of select="$sMSEnvFS"/>
			<xsl:for-each select="$allosConditionedByFeatures">
			   <xsl:text>Not</xsl:text>
			   <xsl:value-of select="MsEnvFeatures/FsFeatStruc/@Id"/>
			</xsl:for-each>
		 </xsl:if>
	  </xsl:for-each>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CountInstancesOfStringInString
	Routine to count instances of one string in another
		Parameters: sString = string to search
							 sStringToLookFor = string to find in sString
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="CountInstancesOfStringInString">
	  <xsl:param name="sString"/>
	  <xsl:param name="sStringToLookFor"/>
	  <!-- The following is a trick.
				It outputs a single character into the variable every time the string to be searched for is found  (it happens to use "x", but any character would do).
				There is therefore one character in the variable for each instance of the string to be searched for.
				We can then output the string length of the variable to give the count.
				-->
	  <xsl:variable name="sCountHolder">
		 <xsl:call-template name="OutputXIfStringInString">
			<xsl:with-param name="sString" select="$sString"/>
			<xsl:with-param name="sStringToLookFor" select="$sStringToLookFor"/>
		 </xsl:call-template>
	  </xsl:variable>
	  <xsl:value-of select="string-length($sCountHolder)"/>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DetermineMaxInterfixes
	Determine max interfixes value based on data
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="DetermineMaxInterfixes">
	  <xsl:variable name="sPfxNfx" select="$MoMorphTypes[@Guid=$sPrefixingInterfix]/@Id"/>
	  <xsl:variable name="sIfxNfx" select="$MoMorphTypes[@Guid=$sInfixingInterfix]/@Id"/>
	  <xsl:variable name="sSfxNfx" select="$MoMorphTypes[@Guid=$sSuffixingInterfix]/@Id"/>
	  <xsl:choose>
		 <xsl:when test="$MoAffixAllomorphs[@MorphType=$sPfxNfx or @MorphType=$sIfxNfx or @MorphType=$sSfxNfx]">1</xsl:when>
		 <xsl:otherwise>0</xsl:otherwise>
	  </xsl:choose>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
NestedPOSids
	Recursive pass through PartOfSpeech to add additional clauses to compound
	 rule test for nested PartOfSpeech.
		Parameters: pos = key/id for current PartOfSpeech
						 bLeft = flag for whether the condition is 'left' or 'current'
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="NestedPOSids">
	  <xsl:param name="pos" select="."/>
	  <xsl:param name="bLeft">Y</xsl:param>
	  <xsl:for-each select="key('POSID',$pos)/SubPossibilities">       OR (<xsl:if test="$bLeft='Y'">left</xsl:if>
		 <xsl:if test="$bLeft!='Y'">current</xsl:if> property is RootPOS<xsl:value-of select="./@dst"/>)
	  <xsl:call-template name="NestedPOSids">
			<xsl:with-param name="pos" select="./@dst"/>
			<xsl:with-param name="bLeft" select="$bLeft"/>
		 </xsl:call-template>
	  </xsl:for-each>
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
	   <!-- make sure there's a trailing space -->
	  <xsl:variable name="sNewList" select="concat(normalize-space($sList),' ')"/>
	  <xsl:variable name="sFirst" select="substring-before($sNewList,' ')"/>
	  <xsl:variable name="sRest" select="substring-after($sNewList,' ')"/>
	   <!-- need to compare entire string, so when looking, append a space before and after; the rest variable also needs to be sure
			 to have an initial space or we can skip one when it occurs more than once in a row. -->
	  <xsl:if test="string-length($sFirst) &gt; 0 and not(contains(concat(' ',$sRest),concat(' ',concat($sFirst,' '))))">
		 <xsl:value-of select="$sFirst"/>
		 <xsl:text>&#x20;</xsl:text>
	  </xsl:if>
	  <xsl:if test="$sRest">
		 <xsl:call-template name="OutputUniqueStrings">
			<xsl:with-param name="sList" select="$sRest"/>
		 </xsl:call-template>
	  </xsl:if>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputXIfStringInString
	Routine to output an x everytime a character is found in a string
		Parameters: sString = string to search
							 sStringToLookFor = string to find in sString
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="OutputXIfStringInString">
	  <xsl:param name="sString"/>
	  <xsl:param name="sStringToLookFor"/>
	  <xsl:if test="contains($sString, $sStringToLookFor)">
		 <xsl:text>x</xsl:text>
		 <xsl:call-template name="OutputXIfStringInString">
			<xsl:with-param name="sString" select="substring-after($sString, $sStringToLookFor)"/>
			<xsl:with-param name="sStringToLookFor" select="$sStringToLookFor"/>
		 </xsl:call-template>
	  </xsl:if>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
SetEllipsisValue
	Sets content of ellipsis string in a MoAdhocProhib item
		Parameters: iAdjacency - value of adjacency parameter
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="SetEllipsisValue">
	  <xsl:param name="iAdjacency">0</xsl:param>
	  <!-- following assumes the values of iAdjacency are:
	  0 = Anywhere
	  1 = Somewhere to left
	  2 = Somewhere to right
	  3 = Adjacent to left
	  4 = Adjacent to right
	  -->
	  <xsl:choose>
		 <xsl:when test="$iAdjacency='0' or $iAdjacency='1' or $iAdjacency='2'">
			<xsl:text> ... </xsl:text>
		 </xsl:when>
		 <xsl:when test="$iAdjacency='3' or $iAdjacency='4'">
			<xsl:text>&#x20;</xsl:text>
		 </xsl:when>
	  </xsl:choose>
   </xsl:template>
</xsl:stylesheet>
<!--
================================================================
Revision History
- - - - - - - - - - - - - - - - - - -
27-Mar-2012     Steve McConnel  Tweak for effiency in libxslt based processing.
21-Apr-2006	    Andy Black	Output all representations in a natural class, not just the first one.
21-Feb-2006	    Andy Black	Add inflection class and productivity restrictions to properties; remove feature structures
09-Dec-2005	    Andy Black	Use minimum output patr options (for efficiency)
16-Aug-2005	    Andy Black	Allow for inflection features
24-May-2005	 Andy Black	Improve calculation of count of  properties.
27-Apr-2005	 Andy Black	Modify to allow an allomorph to belong to more than one inflection class
20-Apr-2005	 Andy Black	Modify to include MoUnclassifiedAffixMsa
24-Feb-2005	 Andy Black	Add interfix successor test
18-Feb-2005	 Andy Black	Add interfixes
21-Jan-2005	 Andy Black	Orderclass needed to allow for an unmarked prefix between marked prefixes
22-Nov-2004	Andy Black	Use natural class abbrevations
							Add RootPOS0 as a morpheme property (to handle stemMSAs without a POS).
07-Jul-2004	       Andy Black	Fix per model change whre adhoc coprohibs now use "first/rest"
11-Mar-2004	Andy Black	Allow consecutive orderclasses to be equal
							We allow multiple instances of the same orderclass to occur.  We also allow derivational affixes to occur
							outside of inflectional affixes.
06-Feb-2004	Andy Black	Change ANCC from "-/" to "/"
19-Dec-2003	Andy Black	Change ANCC from "~/" to "-/"
12-Nov-2003	Andy Black	Modify for FXT output via DistFiles\WW\FXTs\M3Paser.fxt
29-Oct-2003		Andy Black Reflect recent WritingSystem change
02-Oct-2003		Andy Black	Add linker in compound rules
18-Aug-2003	Andy Black	Change \maxprops to calculate number rather than use a parameter
15-Aug-2003	Andy Black	Fix max infix code to use ws instead of enc
12-Aug-2003	Andy Black	Disable \rt Compound test: it is too restrictive (doesn't account for derivational suffixes
							appearing on the right-hand member)
10-Jul-2003		Andy Black	Make changes per model changes
03-Dec-2002      Andy Black  Change Msi to Msa
06-Sep-2002      Andy Black  Use parameter for whether to output gloss or MSI ID as morphname
19-Mar-2002     Andy Black  Added Components to MSIs
												Added Adjacency to adhoc coprohibitions
												Added MoAlloAdhocProhib
08-Feb-2002	Andy Black	Modify to handle "flat" xml format
05-Feb-2002	Andy Black	Refine compound root checking to look for MoEndoCompound or 										MoExoCompound
							Fix \maxi calculation
28-Jan-2002	Andy Black	Make \maxi be 1 if there are infixes defined in MoMorphType
14-Jan-2002	Andy Black	Allow glosses in Adhoc constraints
13-Dec-2001	Andy Black	Modify to apply to XML output from M3ParserSvr which
								has been passed through CleanFWDump.xslt
26-Apr-2001	Andy Black	Initial Draft
================================================================
-->
