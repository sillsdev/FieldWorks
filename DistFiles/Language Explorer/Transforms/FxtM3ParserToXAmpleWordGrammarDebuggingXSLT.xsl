<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:wgd="output.xsl"
 xmlns:msxsl="urn:schemas-microsoft-com:xslt"
 xmlns:saxon="http://icl.com/saxon"
 xmlns:exsl="http://exslt.org/common"
 exclude-result-prefixes="exsl saxon msxsl">
   <xsl:output method="xml" version="1.0" encoding="utf-8" indent="yes"/>
   <!--
================================================================
Convert M3 FXT Parser dump to a Word Grammar Debugging XSLT
  Input:    XML output from FXT for parser
  Output: Word Grammar Debugging XSLT
================================================================
Revision History is at the end of this file.

- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Preamble
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:namespace-alias stylesheet-prefix="wgd" result-prefix="xsl"/>
   <!-- Using keys instead of IDs (so no DTD or XSD required) -->
   <xsl:key name="FeatureID" match="//FsComplexFeature | //FsClosedFeature" use="@Id"/>
   <xsl:key name="POSID" match="//PartOfSpeech" use="@Id"/>
   <xsl:key name="SlotID" match="//MoInflAffixSlot" use="@Id"/>
   <xsl:key name="StemMsaID" match="//MoStemMsa" use="@Id"/>
   <xsl:key name="StemNameID" match="//MoStemName" use="@Id"/>
   <xsl:key name="ValueID" match="//FsSymFeatVal" use="@Id"/>
   <xsl:key name="MoStemAllomorph_id" match="//MoStemAllomorph" use="@Id"/>
   <!--
	Global variables
	-->
   <xsl:variable name="sDiscontiguousPhrase">
	  <xsl:text>0cc8c35a-cee9-434d-be58-5d29130fba5b</xsl:text>
   </xsl:variable>
   <xsl:variable name="sEnclitic">
	  <xsl:text>d7f713e1-e8cf-11d3-9764-00c04f186933</xsl:text>
   </xsl:variable>
   <xsl:variable name="sInfix">
	  <xsl:text>d7f713da-e8cf-11d3-9764-00c04f186933</xsl:text>
   </xsl:variable>
   <xsl:variable name="sInfixingInterfix">
	  <xsl:text>18d9b1c3-b5b6-4c07-b92c-2fe1d2281bd4</xsl:text>
   </xsl:variable>
   <xsl:variable name="sParticle">
	  <xsl:text>56db04bf-3d58-44cc-b292-4c8aa68538f4</xsl:text>
   </xsl:variable>
   <xsl:variable name="sPhrase">
	  <xsl:text>a23b6faa-1052-4f4d-984b-4b338bdaf95f</xsl:text>
   </xsl:variable>
	<xsl:variable name="affixTemplates" select="//MoInflAffixTemplate"/>
	<xsl:variable name="compoundRules" select="//CompoundRules[MoExoCompound | MoEndoCompound]"/>
	<xsl:variable name="compounds" select="//MoExoCompound | //MoEndoCompound"/>
	<xsl:variable name="lexEntries" select="//LexEntry"/>
	<xsl:variable name="partsOfSpeech" select="//PartOfSpeech"/>
	<xsl:variable name="stemAllomorphs" select="//MoStemAllomorph"/>
	<!-- Need to create a list of all unique stem name combinations used in lexical entries -->
   <xsl:variable name="sAllStemNamesUsedInLexicalEntries">
	  <xsl:for-each select="$lexEntries">
		 <xsl:variable name="allos" select="AlternateForms | LexemeForm"/>
		 <!-- collect stem names used so we can output any default stem name allomorph properties -->
		 <xsl:variable name="sStemNamesUsed">
			<xsl:for-each select="key('MoStemAllomorph_id', $allos/@dst)[@StemName!='0' and @IsAbstract='0' and @MorphType!=$sDiscontiguousPhrase and @MorphType!=$sPhrase]">
			   <xsl:sort select="@StemName"/>
			   <xsl:variable name="sn" select="@StemName"/>
			   <xsl:if test="position()=1 or not(preceding-sibling::*/@StemName=$sn)">
				  <xsl:text>StemName</xsl:text>
				  <xsl:value-of select="@StemName"/>
			   </xsl:if>
			</xsl:for-each>
		 </xsl:variable>
		 <xsl:if test="string-length($sStemNamesUsed) &gt; 0">
			<xsl:text>Not</xsl:text>
			<xsl:value-of select="$sStemNamesUsed"/>
			<xsl:text>&#x20;</xsl:text>
		 </xsl:if>
	  </xsl:for-each>
   </xsl:variable>
   <xsl:variable name="sUniqueStemNamesUsedInLexicalEntries">
	  <xsl:call-template name="OutputUniqueStrings">
		 <xsl:with-param name="sList" select="$sAllStemNamesUsedInLexicalEntries"/>
	  </xsl:call-template>
   </xsl:variable>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Main template
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template match="/">
	  <!-- output header info -->
	  <xsl:element name="xsl:stylesheet">
		 <xsl:attribute name="version">1.0</xsl:attribute>
		 <xsl:choose>
		   <xsl:when test="function-available('exsl:node-set')">
			 <xsl:attribute name="auto-ns1:exsl" namespace="http://exslt.org/common"/>
		   </xsl:when>
		   <xsl:when test="function-available('saxon:node-set')">
			 <xsl:attribute name="auto-ns1:saxon" namespace="http://icl.com/saxon"/>
		   </xsl:when>
		   <xsl:otherwise>
		 <xsl:attribute name="msxsl" namespace="urn:schemas-microsoft-com:xslt"/>
		   </xsl:otherwise>
		 </xsl:choose>
		 <xsl:element name="xsl:output">
			<xsl:attribute name="method">xml</xsl:attribute>
			<xsl:attribute name="version">1.0</xsl:attribute>
			<xsl:attribute name="encoding">utf-8</xsl:attribute>
			<xsl:attribute name="indent">yes</xsl:attribute>
		 </xsl:element>
		 <xsl:comment>
================================================================
DO NOT EDIT!!  This transform is automatically generated

Word Grammar Debugger
  This transform is applied repeatedly until a failure is found or the entire analysis succeeds

  Input:  XML output for a given sequence of morphemes
  Output: The tree result with failures embedded
			   (Note: each possible parse is within its own seq element)
================================================================

- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Preamble
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
</xsl:comment>
		 <xsl:element name="xsl:variable">
			<xsl:attribute name="name">
			   <xsl:text>sSuccess</xsl:text>
			</xsl:attribute>
			<xsl:element name="xsl:text">yes</xsl:element>
		 </xsl:element>
		 <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Copy word  element
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
		 <xsl:comment>
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Copy word  element
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
</xsl:comment>
		 <xsl:element name="xsl:template">
			<xsl:attribute name="match">
			   <xsl:text>/word</xsl:text>
			</xsl:attribute>
			<xsl:element name="xsl:copy">
			   <xsl:element name="xsl:apply-templates"/>
			</xsl:element>
		 </xsl:element>
		 <xsl:comment>
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Copy form  element
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
</xsl:comment>
		 <xsl:element name="xsl:template">
			<xsl:attribute name="match">
			   <xsl:text>/word/form</xsl:text>
			</xsl:attribute>
			<xsl:element name="xsl:copy">
			   <xsl:element name="xsl:apply-templates"/>
			</xsl:element>
		 </xsl:element>
		 <xsl:comment>
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Copy resultSoFar  element
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
</xsl:comment>
		 <xsl:element name="xsl:template">
			<xsl:attribute name="match">
			   <xsl:text>/word/resultSoFar</xsl:text>
			</xsl:attribute>
			<xsl:element name="xsl:copy-of">
			   <xsl:attribute name="select">
				  <xsl:text>.</xsl:text>
			   </xsl:attribute>
			</xsl:element>
		 </xsl:element>
		 <xsl:comment>
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
match a root
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
</xsl:comment>
		 <xsl:element name="xsl:template">
			<xsl:attribute name="match">
			   <xsl:text>/word/seq/morph[@wordType='root']</xsl:text>
			</xsl:attribute>
			<xsl:element name="xsl:call-template">
			   <xsl:attribute name="name">StemEqualsRoot</xsl:attribute>
			</xsl:element>
			<xsl:element name="xsl:call-template">
			   <xsl:attribute name="name">PartialEqualsRoot</xsl:attribute>
			</xsl:element>
			<xsl:if test="not($compoundRules)">
			   <xsl:element name="xsl:call-template">
				  <xsl:attribute name="name">PartialEqualsRoots</xsl:attribute>
			   </xsl:element>
			</xsl:if>
		 </xsl:element>
		 <xsl:comment>
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Partial = root production
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
</xsl:comment>
		 <xsl:element name="xsl:template">
			<xsl:attribute name="name">
			   <xsl:text>PartialEqualsRoot</xsl:text>
			</xsl:attribute>
			<seq step="Try to build a Partial analysis node on a root where the category of the root is unknown (i.e. unmarked).">
			   <xsl:element name="xsl:for-each">
				  <xsl:attribute name="select">
					 <xsl:text>preceding-sibling::*</xsl:text>
				  </xsl:attribute>
				  <xsl:element name="xsl:copy-of">
					 <xsl:attribute name="select">
						<xsl:text>.</xsl:text>
					 </xsl:attribute>
				  </xsl:element>
			   </xsl:element>
			   <partial>
				  <xsl:comment>percolate these features</xsl:comment>
				  <xsl:element name="xsl:attribute">
					 <xsl:attribute name="name">syncat</xsl:attribute>
					 <xsl:element name="xsl:value-of">
						<xsl:attribute name="select">stemMsa/@cat</xsl:attribute>
					 </xsl:element>
				  </xsl:element>
				  <xsl:element name="xsl:attribute">
					 <xsl:attribute name="name">syncatAbbr</xsl:attribute>
					 <xsl:element name="xsl:value-of">
						<xsl:attribute name="select">stemMsa/@catAbbr</xsl:attribute>
					 </xsl:element>
				  </xsl:element>
				  <xsl:element name="xsl:if">
					 <xsl:attribute name="test">stemMsa/@cat != '0'</xsl:attribute>
					 <failure>
						<xsl:element name="xsl:text">A root can only be a "Partial" when its category is unknown, but the category here is '</xsl:element>
						<xsl:element name="xsl:value-of">
						   <xsl:attribute name="select">stemMsa/@catAbbr</xsl:attribute>
						</xsl:element>
						<xsl:element name="xsl:text">'.</xsl:element>
					 </failure>
				  </xsl:element>
				  <xsl:element name="xsl:copy-of">
					 <xsl:attribute name="select">
						<xsl:text>.</xsl:text>
					 </xsl:attribute>
				  </xsl:element>
			   </partial>
			   <xsl:element name="xsl:for-each">
				  <xsl:attribute name="select">following-sibling::*</xsl:attribute>
				  <xsl:element name="xsl:copy-of">
					 <xsl:attribute name="select">
						<xsl:text>.</xsl:text>
					 </xsl:attribute>
				  </xsl:element>
			   </xsl:element>
			</seq>
		 </xsl:element>
		 <xsl:if test="not($compoundRules)">
			<xsl:comment>
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Partial = Prefs* Roots Suffs* production
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
</xsl:comment>
			<xsl:element name="xsl:template">
			   <xsl:attribute name="name">
				  <xsl:text>PartialEqualsRoots</xsl:text>
			   </xsl:attribute>
			   <xsl:element name="xsl:if">
				  <xsl:attribute name="test">
					 <xsl:text>not(preceding-sibling::morph[@wordType='root']) and following-sibling::morph[@wordType='root']</xsl:text>
				  </xsl:attribute>
				  <seq step="Try to build a Partial analysis node on compound roots (when there are no compound rules).">
					 <xsl:element name="xsl:variable">
						<xsl:attribute name="name">GenericPrefixMorphs</xsl:attribute>
						<xsl:element name="xsl:call-template">
						   <xsl:attribute name="name">CountGenericPrefixes</xsl:attribute>
						   <xsl:element name="xsl:with-param">
							  <xsl:attribute name="name">morph</xsl:attribute>
							  <xsl:attribute name="select">preceding-sibling::*[1]</xsl:attribute>
						   </xsl:element>
						</xsl:element>
					 </xsl:element>
					 <xsl:element name="xsl:variable">
						<xsl:attribute name="name">iGenericPrefixMorphs</xsl:attribute>
						<xsl:element name="xsl:value-of">
						   <xsl:attribute name="select">string-length($GenericPrefixMorphs)</xsl:attribute>
						</xsl:element>
					 </xsl:element>
					 <xsl:element name="xsl:variable">
						<xsl:attribute name="name">CompoundedRootMorphs</xsl:attribute>
						<xsl:element name="xsl:call-template">
						   <xsl:attribute name="name">CountCompoundedRoots</xsl:attribute>
						   <xsl:element name="xsl:with-param">
							  <xsl:attribute name="name">morph</xsl:attribute>
							  <xsl:attribute name="select">following-sibling::*[1]</xsl:attribute>
						   </xsl:element>
						</xsl:element>
					 </xsl:element>
					 <xsl:element name="xsl:variable">
						<xsl:attribute name="name">iCompoundedRootMorphs</xsl:attribute>
						<xsl:element name="xsl:value-of">
						   <xsl:attribute name="select">string-length($CompoundedRootMorphs)</xsl:attribute>
						</xsl:element>
					 </xsl:element>
					 <xsl:element name="xsl:variable">
						<xsl:attribute name="name">GenericSuffixMorphs</xsl:attribute>
						<xsl:element name="xsl:call-template">
						   <xsl:attribute name="name">CountGenericSuffixes</xsl:attribute>
						   <xsl:element name="xsl:with-param">
							  <xsl:attribute name="name">morph</xsl:attribute>
							  <xsl:attribute name="select">following-sibling::*[$iCompoundedRootMorphs]</xsl:attribute>
						   </xsl:element>
						</xsl:element>
					 </xsl:element>
					 <xsl:element name="xsl:variable">
						<xsl:attribute name="name">iGenericSuffixMorphs</xsl:attribute>
						<xsl:element name="xsl:value-of">
						   <xsl:attribute name="select">string-length($GenericSuffixMorphs)</xsl:attribute>
						</xsl:element>
					 </xsl:element>
					 <xsl:comment> copy what's before the partial </xsl:comment>
					 <xsl:element name="xsl:for-each">
						<xsl:attribute name="select">preceding-sibling::*[position()&gt;$iGenericPrefixMorphs]</xsl:attribute>
						<xsl:element name="xsl:copy-of">
						   <xsl:attribute name="select">.</xsl:attribute>
						</xsl:element>
					 </xsl:element>
					 <partial>
						<xsl:comment>percolate these features</xsl:comment>
						<xsl:element name="xsl:attribute">
						   <xsl:attribute name="name">syncat</xsl:attribute>
						   <xsl:element name="xsl:value-of">
							  <xsl:attribute name="select">stemMsa/@cat</xsl:attribute>
						   </xsl:element>
						</xsl:element>
						<xsl:element name="xsl:attribute">
						   <xsl:attribute name="name">syncatAbbr</xsl:attribute>
						   <xsl:element name="xsl:value-of">
							  <xsl:attribute name="select">stemMsa/@catAbbr</xsl:attribute>
						   </xsl:element>
						</xsl:element>
						<xsl:comment>copy any generic prefixes</xsl:comment>
						<xsl:element name="xsl:if">
						   <xsl:attribute name="test">$iGenericPrefixMorphs&gt; 0</xsl:attribute>
						   <xsl:element name="xsl:for-each">
							  <xsl:attribute name="select">preceding-sibling::*[position()&lt;=$iGenericPrefixMorphs]</xsl:attribute>
							  <xsl:element name="xsl:copy-of">
								 <xsl:attribute name="select">.</xsl:attribute>
							  </xsl:element>
						   </xsl:element>
						</xsl:element>
						<xsl:element name="xsl:copy-of">
						   <xsl:attribute name="select">
							  <xsl:text>.</xsl:text>
						   </xsl:attribute>
						</xsl:element>
						<xsl:element name="xsl:copy-of">
						   <xsl:attribute name="select">
							  <xsl:text>following-sibling::*[position()&lt;=$iCompoundedRootMorphs]</xsl:text>
						   </xsl:attribute>
						</xsl:element>
						<xsl:comment>copy any generic suffixes</xsl:comment>
						<xsl:element name="xsl:if">
						   <xsl:attribute name="test">$iGenericSuffixMorphs &gt; 0</xsl:attribute>
						   <xsl:element name="xsl:for-each">
							  <xsl:attribute name="select">following-sibling::*[position()&gt;$iCompoundedRootMorphs and position()&lt;=$iCompoundedRootMorphs + $iGenericSuffixMorphs]</xsl:attribute>
							  <xsl:element name="xsl:copy-of">
								 <xsl:attribute name="select">.</xsl:attribute>
							  </xsl:element>
						   </xsl:element>
						</xsl:element>
					 </partial>
					 <xsl:comment> copy what's after the partial </xsl:comment>
					 <xsl:element name="xsl:for-each">
						<xsl:attribute name="select">following-sibling::*[position()&gt;$iCompoundedRootMorphs + $iGenericSuffixMorphs]</xsl:attribute>
						<xsl:element name="xsl:copy-of">
						   <xsl:attribute name="select">.</xsl:attribute>
						</xsl:element>
					 </xsl:element>
				  </seq>
			   </xsl:element>
			</xsl:element>
		 </xsl:if>
		 <xsl:comment>
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Stem = root production
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
</xsl:comment>
		 <xsl:element name="xsl:template">
			<xsl:attribute name="name">
			   <xsl:text>StemEqualsRoot</xsl:text>
			</xsl:attribute>
			<seq step="Try to build a Stem node on a root.">
			   <xsl:element name="xsl:for-each">
				  <xsl:attribute name="select">
					 <xsl:text>preceding-sibling::*</xsl:text>
				  </xsl:attribute>
				  <xsl:element name="xsl:copy-of">
					 <xsl:attribute name="select">
						<xsl:text>.</xsl:text>
					 </xsl:attribute>
				  </xsl:element>
			   </xsl:element>
			   <stem>
				  <xsl:comment>percolate these features</xsl:comment>
				  <xsl:element name="xsl:attribute">
					 <xsl:attribute name="name">syncat</xsl:attribute>
					 <xsl:element name="xsl:value-of">
						<xsl:attribute name="select">stemMsa/@cat</xsl:attribute>
					 </xsl:element>
				  </xsl:element>
				  <xsl:element name="xsl:attribute">
					 <xsl:attribute name="name">syncatAbbr</xsl:attribute>
					 <xsl:element name="xsl:value-of">
						<xsl:attribute name="select">stemMsa/@catAbbr</xsl:attribute>
					 </xsl:element>
				  </xsl:element>
				  <xsl:element name="xsl:attribute">
					 <xsl:attribute name="name">inflClass</xsl:attribute>
					 <xsl:element name="xsl:value-of">
						<xsl:attribute name="select">stemMsa/@inflClass</xsl:attribute>
					 </xsl:element>
				  </xsl:element>
				  <xsl:element name="xsl:attribute">
					 <xsl:attribute name="name">inflClassAbbr</xsl:attribute>
					 <xsl:element name="xsl:value-of">
						<xsl:attribute name="select">stemMsa/@inflClassAbbr</xsl:attribute>
					 </xsl:element>
				  </xsl:element>
				  <xsl:element name="xsl:attribute">
					 <xsl:attribute name="name">requiresInfl</xsl:attribute>
					 <xsl:element name="xsl:value-of">
						<xsl:attribute name="select">stemMsa/@requiresInfl</xsl:attribute>
					 </xsl:element>
				  </xsl:element>
				  <xsl:element name="xsl:if">
					 <xsl:attribute name="test">stemMsa/@cat='0'</xsl:attribute>
					 <failure>A stem requires an overt category, but this root has an unmarked category.</failure>
				  </xsl:element>
				  <xsl:element name="xsl:copy-of">
					 <xsl:attribute name="select">
						<xsl:text>stemMsa/fs</xsl:text>
					 </xsl:attribute>
				  </xsl:element>
				  <xsl:element name="xsl:copy-of">
					 <xsl:attribute name="select">
						<xsl:text>.</xsl:text>
					 </xsl:attribute>
				  </xsl:element>
				  <wgd:copy-of select="stemMsa/productivityRestriction"/>
				  <wgd:copy-of select="stemName"/>
			   </stem>
			   <xsl:element name="xsl:for-each">
				  <xsl:attribute name="select">following-sibling::*</xsl:attribute>
				  <xsl:element name="xsl:copy-of">
					 <xsl:attribute name="select">
						<xsl:text>.</xsl:text>
					 </xsl:attribute>
				  </xsl:element>
			   </xsl:element>
			</seq>
		 </xsl:element>
		 <xsl:comment>
	  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Other Stem productions
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  </xsl:comment>
		 <xsl:element name="xsl:template">
			<xsl:attribute name="match">/word/seq[not(descendant::failure)]/stem</xsl:attribute>
			<xsl:element name="xsl:if">
			   <xsl:attribute name="test">preceding-sibling::morph[1][@wordType='derivPfx']</xsl:attribute>
			   <xsl:element name="xsl:call-template">
				  <xsl:attribute name="name">DerivPfxStemProduction</xsl:attribute>
			   </xsl:element>
			</xsl:element>
			<xsl:element name="xsl:if">
			   <xsl:attribute name="test">preceding-sibling::morph[1][@wordType='derivCircumPfx'] and following-sibling::morph[1][@wordType='derivCircumSfx']</xsl:attribute>
			   <xsl:element name="xsl:call-template">
				  <xsl:attribute name="name">DerivCircumPfxStemDerivCircumSfxProduction</xsl:attribute>
			   </xsl:element>
			</xsl:element>
			<xsl:element name="xsl:if">
			   <xsl:attribute name="test">following-sibling::morph[1][@wordType='derivSfx']</xsl:attribute>
			   <xsl:element name="xsl:call-template">
				  <xsl:attribute name="name">StemDerivSfxProduction</xsl:attribute>
			   </xsl:element>
			</xsl:element>
			<xsl:element name="xsl:if">
			   <xsl:attribute name="test">following-sibling::stem[1]</xsl:attribute>
			   <xsl:element name="xsl:call-template">
				  <xsl:attribute name="name">StemStemProductions</xsl:attribute>
			   </xsl:element>
			</xsl:element>
			<xsl:element name="xsl:if">
			   <xsl:attribute name="test">not(preceding-sibling::morph[@wordType='root']) and not(following-sibling::morph[@wordType='root'])</xsl:attribute>
			   <xsl:element name="xsl:call-template">
				  <xsl:attribute name="name">InflectionalProductions</xsl:attribute>
			   </xsl:element>
			</xsl:element>
		 </xsl:element>
		 <xsl:comment>
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Partial productions
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
</xsl:comment>
		 <xsl:comment>
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Partial with circumfixes
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  </xsl:comment>
		 <xsl:element name="xsl:template">
			<xsl:attribute name="match">/word/seq[not(descendant::failure)]/partial[preceding-sibling::morph[1][@wordType='circumPfx' or @wordType='derivCircumPfx'] and following-sibling::morph[1][@wordType='circumSfx' or @wordType='derivCircumSfx']]</xsl:attribute>
			<seq step="Try to build a Partial node by surrounding another Partial node with an unclassified circumfix.">
			   <xsl:comment>copy what's before the circumfix-prefix</xsl:comment>
			   <xsl:element name="xsl:for-each">
				  <xsl:attribute name="select">preceding-sibling::*[position()&gt;1]</xsl:attribute>
				  <xsl:element name="xsl:copy-of">
					 <xsl:attribute name="select">.</xsl:attribute>
				  </xsl:element>
			   </xsl:element>
			   <xsl:comment>have a new partial</xsl:comment>
			   <partial>
				  <xsl:element name="xsl:attribute">
					 <xsl:attribute name="name">syncat</xsl:attribute>
					 <xsl:element name="xsl:value-of">
						<xsl:attribute name="select">@syncat</xsl:attribute>
					 </xsl:element>
				  </xsl:element>
				  <xsl:element name="xsl:attribute">
					 <xsl:attribute name="name">syncatAbbr</xsl:attribute>
					 <xsl:element name="xsl:value-of">
						<xsl:attribute name="select">@syncatAbbr</xsl:attribute>
					 </xsl:element>
				  </xsl:element>
				  <xsl:comment> copy circumfix-prefix</xsl:comment>
				  <xsl:element name="xsl:copy-of">
					 <xsl:attribute name="select">preceding-sibling::morph[1]</xsl:attribute>
				  </xsl:element>
				  <xsl:element name="xsl:copy-of">
					 <xsl:attribute name="select">.</xsl:attribute>
				  </xsl:element>
				  <xsl:comment> copy circumfix-suffix</xsl:comment>
				  <xsl:element name="xsl:copy-of">
					 <xsl:attribute name="select">following-sibling::morph[1]</xsl:attribute>
				  </xsl:element>
			   </partial>
			   <xsl:comment> copy what's after the circumfix suffix </xsl:comment>
			   <xsl:element name="xsl:for-each">
				  <xsl:attribute name="select">following-sibling::*[position()&gt;1]</xsl:attribute>
				  <xsl:element name="xsl:copy-of">
					 <xsl:attribute name="select">.</xsl:attribute>
				  </xsl:element>
			   </xsl:element>
			</seq>
		 </xsl:element>
		 <xsl:comment>
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Partial = ... circumPfx Full circumSfx ...
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  </xsl:comment>
		 <xsl:element name="xsl:template">
			<xsl:attribute name="name">CircumfixPartialOnFull</xsl:attribute>
			<seq step="Try to build a Partial analysis node by surrounding a Full analysis node with an unclassified circumfix.">
			   <xsl:comment> constrain the circumfix </xsl:comment>
			   <xsl:element name="xsl:variable">
				  <xsl:attribute name="name">sSynCat</xsl:attribute>
				  <xsl:attribute name="select">@syncat</xsl:attribute>
			   </xsl:element>
			   <xsl:element name="xsl:variable">
				  <xsl:attribute name="name">sSynCatAbbr</xsl:attribute>
				  <xsl:attribute name="select">@syncatAbbr</xsl:attribute>
			   </xsl:element>
			   <xsl:element name="xsl:for-each">
				  <xsl:attribute name="select">preceding-sibling::*[1]/morph/unclassMsa</xsl:attribute>
				  <xsl:comment>apply constraints</xsl:comment>
				  <xsl:element name="xsl:variable">
					 <xsl:attribute name="name">sCategoriesAreCompatible</xsl:attribute>
					 <xsl:element name="xsl:call-template">
						<xsl:attribute name="name">TestCompatibleCategories</xsl:attribute>
						<xsl:element name="xsl:with-param">
						   <xsl:attribute name="name">sFirstCat</xsl:attribute>
						   <xsl:element name="xsl:value-of">
							  <xsl:attribute name="select">@fromCat</xsl:attribute>
						   </xsl:element>
						</xsl:element>
						<xsl:element name="xsl:with-param">
						   <xsl:attribute name="name">sSecondCat</xsl:attribute>
						   <xsl:element name="xsl:value-of">
							  <xsl:attribute name="select">$sSynCat</xsl:attribute>
						   </xsl:element>
						</xsl:element>
					 </xsl:element>
				  </xsl:element>
				  <xsl:element name="xsl:variable">
					 <xsl:attribute name="name">sType</xsl:attribute>
					 <xsl:attribute name="select">../@wordType</xsl:attribute>
				  </xsl:element>
				  <xsl:element name="xsl:if">
					 <xsl:attribute name="test">$sCategoriesAreCompatible != $sSuccess</xsl:attribute>
					 <xsl:element name="xsl:variable">
						<xsl:attribute name="name">sPreface</xsl:attribute>
						<xsl:element name="xsl:text">In attaching an unclassified circumfix: </xsl:element>
					 </xsl:element>
					 <xsl:element name="xsl:call-template">
						<xsl:attribute name="name">ReportFailure</xsl:attribute>
						<xsl:element name="xsl:with-param">
						   <xsl:attribute name="name">sPreface</xsl:attribute>
						   <xsl:element name="xsl:value-of">
							  <xsl:attribute name="select">$sPreface</xsl:attribute>
						   </xsl:element>
						</xsl:element>
						<xsl:element name="xsl:with-param">
						   <xsl:attribute name="name">sFirstItemComponent</xsl:attribute>
						   <xsl:element name="xsl:text">category</xsl:element>
						</xsl:element>
						<xsl:element name="xsl:with-param">
						   <xsl:attribute name="name">sFirstItemComponentAbbr</xsl:attribute>
						   <xsl:attribute name="select">@fromCatAbbr</xsl:attribute>
						</xsl:element>
						<xsl:element name="xsl:with-param">
						   <xsl:attribute name="name">sFirstItemDescription</xsl:attribute>
						   <xsl:element name="xsl:text">unclassified </xsl:element>
						   <xsl:element name="xsl:value-of">
							  <xsl:attribute name="select">$sType</xsl:attribute>
						   </xsl:element>
						</xsl:element>
						<xsl:element name="xsl:with-param">
						   <xsl:attribute name="name">sFirstItemValue</xsl:attribute>
						   <xsl:attribute name="select">../shortName</xsl:attribute>
						</xsl:element>
						<xsl:element name="xsl:with-param">
						   <xsl:attribute name="name">sSecondItemComponent</xsl:attribute>
						   <xsl:element name="xsl:text">category</xsl:element>
						</xsl:element>
						<xsl:element name="xsl:with-param">
						   <xsl:attribute name="name">sSecondItemComponentAbbr</xsl:attribute>
						   <xsl:attribute name="select">$sSynCatAbbr</xsl:attribute>
						</xsl:element>
						<xsl:element name="xsl:with-param">
						   <xsl:attribute name="name">sSecondItemDescription</xsl:attribute>
						   <xsl:element name="xsl:text">stem</xsl:element>
						</xsl:element>
					 </xsl:element>
				  </xsl:element>
			   </xsl:element>
			   <xsl:comment>copy what's before the circumfix-prefix</xsl:comment>
			   <xsl:element name="xsl:for-each">
				  <xsl:attribute name="select">preceding-sibling::*[position()&gt;1]</xsl:attribute>
				  <xsl:element name="xsl:copy-of">
					 <xsl:attribute name="select">.</xsl:attribute>
				  </xsl:element>
			   </xsl:element>
			   <xsl:comment>have a partial</xsl:comment>
			   <partial>
				  <xsl:element name="xsl:attribute">
					 <xsl:attribute name="name">inflected</xsl:attribute>
					 <xsl:element name="xsl:value-of">
						<xsl:attribute name="select">@inflected</xsl:attribute>
					 </xsl:element>
				  </xsl:element>
				  <xsl:element name="xsl:attribute">
					 <xsl:attribute name="name">syncat</xsl:attribute>
					 <xsl:element name="xsl:value-of">
						<xsl:attribute name="select">@syncat</xsl:attribute>
					 </xsl:element>
				  </xsl:element>
				  <xsl:element name="xsl:attribute">
					 <xsl:attribute name="name">syncatAbbr</xsl:attribute>
					 <xsl:element name="xsl:value-of">
						<xsl:attribute name="select">@syncatAbbr</xsl:attribute>
					 </xsl:element>
				  </xsl:element>
				  <xsl:comment> copy circumfix-prefix</xsl:comment>
				  <xsl:element name="xsl:copy-of">
					 <xsl:attribute name="select">preceding-sibling::morph[1]</xsl:attribute>
				  </xsl:element>
				  <xsl:comment>copy the full</xsl:comment>
				  <xsl:element name="xsl:copy-of">
					 <xsl:attribute name="select">.</xsl:attribute>
				  </xsl:element>
				  <xsl:comment> copy circumfix-suffix</xsl:comment>
				  <xsl:element name="xsl:copy-of">
					 <xsl:attribute name="select">following-sibling::morph[1]</xsl:attribute>
				  </xsl:element>
			   </partial>
			   <xsl:comment> copy what's after the circumfix suffix </xsl:comment>
			   <xsl:element name="xsl:for-each">
				  <xsl:attribute name="select">following-sibling::*[position()&gt;1]</xsl:attribute>
				  <xsl:element name="xsl:copy-of">
					 <xsl:attribute name="select">.</xsl:attribute>
				  </xsl:element>
			   </xsl:element>
			</seq>
		 </xsl:element>
		 <xsl:comment>
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Partial with Generic affixes
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  </xsl:comment>
		 <xsl:element name="xsl:template">
			<xsl:attribute name="name">GenericAffixPartial</xsl:attribute>
			<seq step="Try to build a Partial analysis node on a Full analysis node with sequences of affixes not labeled as either derivational or inflectional (the sequence may also include derivational affixes).">
			   <xsl:element name="xsl:variable">
				  <xsl:attribute name="name">GenericPrefixMorphs</xsl:attribute>
				  <xsl:element name="xsl:call-template">
					 <xsl:attribute name="name">CountGenericPrefixes</xsl:attribute>
					 <xsl:element name="xsl:with-param">
						<xsl:attribute name="name">morph</xsl:attribute>
						<xsl:attribute name="select">preceding-sibling::*[1]</xsl:attribute>
					 </xsl:element>
				  </xsl:element>
			   </xsl:element>
			   <xsl:element name="xsl:variable">
				  <xsl:attribute name="name">iGenericPrefixMorphs</xsl:attribute>
				  <xsl:element name="xsl:value-of">
					 <xsl:attribute name="select">string-length($GenericPrefixMorphs)</xsl:attribute>
				  </xsl:element>
			   </xsl:element>
			   <xsl:element name="xsl:variable">
				  <xsl:attribute name="name">GenericSuffixMorphs</xsl:attribute>
				  <xsl:element name="xsl:call-template">
					 <xsl:attribute name="name">CountGenericSuffixes</xsl:attribute>
					 <xsl:element name="xsl:with-param">
						<xsl:attribute name="name">morph</xsl:attribute>
						<xsl:attribute name="select">following-sibling::*[1]</xsl:attribute>
					 </xsl:element>
				  </xsl:element>
			   </xsl:element>
			   <xsl:element name="xsl:variable">
				  <xsl:attribute name="name">iGenericSuffixMorphs</xsl:attribute>
				  <xsl:element name="xsl:value-of">
					 <xsl:attribute name="select">string-length($GenericSuffixMorphs)</xsl:attribute>
				  </xsl:element>
			   </xsl:element>
			   <xsl:comment> constrain any prefixes and any suffixes </xsl:comment>
			   <xsl:element name="xsl:variable">
				  <xsl:attribute name="name">sSynCat</xsl:attribute>
				  <xsl:attribute name="select">@syncat</xsl:attribute>
			   </xsl:element>
			   <xsl:element name="xsl:variable">
				  <xsl:attribute name="name">sSynCatAbbr</xsl:attribute>
				  <xsl:attribute name="select">@syncatAbbr</xsl:attribute>
			   </xsl:element>
			   <xsl:element name="xsl:for-each">
				  <xsl:attribute name="select">../morph/unclassMsa </xsl:attribute>
				  <xsl:comment>apply constraints</xsl:comment>
				  <xsl:element name="xsl:variable">
					 <xsl:attribute name="name">sCategoriesAreCompatible</xsl:attribute>
					 <xsl:element name="xsl:call-template">
						<xsl:attribute name="name">TestCompatibleCategories</xsl:attribute>
						<xsl:element name="xsl:with-param">
						   <xsl:attribute name="name">sFirstCat</xsl:attribute>
						   <xsl:element name="xsl:value-of">
							  <xsl:attribute name="select">@fromCat</xsl:attribute>
						   </xsl:element>
						</xsl:element>
						<xsl:element name="xsl:with-param">
						   <xsl:attribute name="name">sSecondCat</xsl:attribute>
						   <xsl:element name="xsl:value-of">
							  <xsl:attribute name="select">$sSynCat</xsl:attribute>
						   </xsl:element>
						</xsl:element>
					 </xsl:element>
				  </xsl:element>
				  <xsl:element name="xsl:variable">
					 <xsl:attribute name="name">sType</xsl:attribute>
					 <xsl:attribute name="select">../@wordType</xsl:attribute>
				  </xsl:element>
				  <xsl:element name="xsl:if">
					 <xsl:attribute name="test">$sCategoriesAreCompatible != $sSuccess</xsl:attribute>
					 <xsl:element name="xsl:variable">
						<xsl:attribute name="name">sPreface</xsl:attribute>
						<xsl:element name="xsl:text">In attaching an unclassified </xsl:element>
						<xsl:element name="xsl:value-of">
						   <xsl:attribute name="select">$sType</xsl:attribute>
						</xsl:element>: </xsl:element>
					 <xsl:element name="xsl:call-template">
						<xsl:attribute name="name">ReportFailure</xsl:attribute>
						<xsl:element name="xsl:with-param">
						   <xsl:attribute name="name">sPreface</xsl:attribute>
						   <xsl:element name="xsl:value-of">
							  <xsl:attribute name="select">$sPreface</xsl:attribute>
						   </xsl:element>
						</xsl:element>
						<xsl:element name="xsl:with-param">
						   <xsl:attribute name="name">sFirstItemComponent</xsl:attribute>
						   <xsl:element name="xsl:text">category</xsl:element>
						</xsl:element>
						<xsl:element name="xsl:with-param">
						   <xsl:attribute name="name">sFirstItemComponentAbbr</xsl:attribute>
						   <xsl:attribute name="select">@fromCatAbbr</xsl:attribute>
						</xsl:element>
						<xsl:element name="xsl:with-param">
						   <xsl:attribute name="name">sFirstItemDescription</xsl:attribute>
						   <xsl:element name="xsl:text">unclassified </xsl:element>
						   <xsl:element name="xsl:value-of">
							  <xsl:attribute name="select">$sType</xsl:attribute>
						   </xsl:element>
						</xsl:element>
						<xsl:element name="xsl:with-param">
						   <xsl:attribute name="name">sFirstItemValue</xsl:attribute>
						   <xsl:attribute name="select">../shortName</xsl:attribute>
						</xsl:element>
						<xsl:element name="xsl:with-param">
						   <xsl:attribute name="name">sSecondItemComponent</xsl:attribute>
						   <xsl:element name="xsl:text">category</xsl:element>
						</xsl:element>
						<xsl:element name="xsl:with-param">
						   <xsl:attribute name="name">sSecondItemComponentAbbr</xsl:attribute>
						   <xsl:attribute name="select">$sSynCatAbbr</xsl:attribute>
						</xsl:element>
						<xsl:element name="xsl:with-param">
						   <xsl:attribute name="name">sSecondItemDescription</xsl:attribute>
						   <xsl:element name="xsl:text">stem</xsl:element>
						</xsl:element>
					 </xsl:element>
				  </xsl:element>
			   </xsl:element>
			   <xsl:comment> copy what's before the partial </xsl:comment>
			   <xsl:element name="xsl:for-each">
				  <xsl:attribute name="select">preceding-sibling::*[position()&gt;$iGenericPrefixMorphs]</xsl:attribute>
				  <xsl:element name="xsl:copy-of">
					 <xsl:attribute name="select">.</xsl:attribute>
				  </xsl:element>
			   </xsl:element>
			   <xsl:comment>have a partial</xsl:comment>
			   <partial>
				  <xsl:element name="xsl:attribute">
					 <xsl:attribute name="name">inflected</xsl:attribute>
					 <xsl:element name="xsl:value-of">
						<xsl:attribute name="select">@inflected</xsl:attribute>
					 </xsl:element>
				  </xsl:element>
				  <xsl:element name="xsl:attribute">
					 <xsl:attribute name="name">syncat</xsl:attribute>
					 <xsl:element name="xsl:value-of">
						<xsl:attribute name="select">@syncat</xsl:attribute>
					 </xsl:element>
				  </xsl:element>
				  <xsl:element name="xsl:attribute">
					 <xsl:attribute name="name">syncatAbbr</xsl:attribute>
					 <xsl:element name="xsl:value-of">
						<xsl:attribute name="select">@syncatAbbr</xsl:attribute>
					 </xsl:element>
				  </xsl:element>
				  <xsl:comment>copy any generic prefixes</xsl:comment>
				  <xsl:element name="xsl:if">
					 <xsl:attribute name="test">$iGenericPrefixMorphs&gt; 0</xsl:attribute>
					 <xsl:element name="xsl:for-each">
						<xsl:attribute name="select">preceding-sibling::*[position()&lt;=$iGenericPrefixMorphs]</xsl:attribute>
						<xsl:element name="xsl:copy-of">
						   <xsl:attribute name="select">.</xsl:attribute>
						</xsl:element>
					 </xsl:element>
				  </xsl:element>
				  <xsl:comment>copy the full</xsl:comment>
				  <xsl:element name="xsl:copy-of">
					 <xsl:attribute name="select">.</xsl:attribute>
				  </xsl:element>
				  <xsl:comment>copy any generic suffixes</xsl:comment>
				  <xsl:element name="xsl:if">
					 <xsl:attribute name="test">$iGenericSuffixMorphs &gt; 0</xsl:attribute>
					 <xsl:element name="xsl:for-each">
						<xsl:attribute name="select">following-sibling::*[position()&lt;=$iGenericSuffixMorphs]</xsl:attribute>
						<xsl:element name="xsl:copy-of">
						   <xsl:attribute name="select">.</xsl:attribute>
						</xsl:element>
					 </xsl:element>
				  </xsl:element>
			   </partial>
			   <xsl:comment> copy what's after the partial </xsl:comment>
			   <xsl:element name="xsl:for-each">
				  <xsl:attribute name="select">following-sibling::*[position()&gt;$iGenericSuffixMorphs]</xsl:attribute>
				  <xsl:element name="xsl:copy-of">
					 <xsl:attribute name="select">.</xsl:attribute>
				  </xsl:element>
			   </xsl:element>
			</seq>
		 </xsl:element>
		 <xsl:comment>
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CountCompoundedRoots
	Params: morph - morpheme to check
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  </xsl:comment>
		 <xsl:element name="xsl:template">
			<xsl:attribute name="name">CountCompoundedRoots</xsl:attribute>
			<xsl:element name="xsl:param">
			   <xsl:attribute name="name">morph</xsl:attribute>
			</xsl:element>
			<xsl:element name="xsl:if">
			   <xsl:attribute name="test">$morph/@wordType = 'root' or $morph/@wordType = 'interfix'</xsl:attribute>
			   <xsl:element name="xsl:call-template">
				  <xsl:attribute name="name">IndicateInflAffixSucceeded</xsl:attribute>
			   </xsl:element>
			   <xsl:element name="xsl:call-template">
				  <xsl:attribute name="name">CountCompoundedRoots</xsl:attribute>
				  <xsl:element name="xsl:with-param">
					 <xsl:attribute name="name">morph</xsl:attribute>
					 <xsl:attribute name="select">$morph/following-sibling::*[1]</xsl:attribute>
				  </xsl:element>
			   </xsl:element>
			</xsl:element>
		 </xsl:element>
		 <xsl:comment>
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CountGenericPrefixes
	Params: morph - morpheme to check
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  </xsl:comment>
		 <xsl:element name="xsl:template">
			<xsl:attribute name="name">CountGenericPrefixes</xsl:attribute>
			<xsl:element name="xsl:param">
			   <xsl:attribute name="name">morph</xsl:attribute>
			</xsl:element>
			<xsl:element name="xsl:if">
			   <xsl:attribute name="test">$morph/@wordType = 'prefix' or $morph/@wordType = 'derivPfx'</xsl:attribute>
			   <xsl:element name="xsl:call-template">
				  <xsl:attribute name="name">IndicateInflAffixSucceeded</xsl:attribute>
			   </xsl:element>
			   <xsl:element name="xsl:call-template">
				  <xsl:attribute name="name">CountGenericPrefixes</xsl:attribute>
				  <xsl:element name="xsl:with-param">
					 <xsl:attribute name="name">morph</xsl:attribute>
					 <xsl:attribute name="select">$morph/preceding-sibling::*[1]</xsl:attribute>
				  </xsl:element>
			   </xsl:element>
			</xsl:element>
		 </xsl:element>
		 <xsl:comment>
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CountGenericSuffixes
	Params: morph - morpheme to check
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  </xsl:comment>
		 <xsl:element name="xsl:template">
			<xsl:attribute name="name">CountGenericSuffixes</xsl:attribute>
			<xsl:element name="xsl:param">
			   <xsl:attribute name="name">morph</xsl:attribute>
			</xsl:element>
			<xsl:element name="xsl:if">
			   <xsl:attribute name="test">$morph/@wordType = 'suffix' or $morph/@wordType = 'derivSfx'</xsl:attribute>
			   <xsl:element name="xsl:call-template">
				  <xsl:attribute name="name">IndicateInflAffixSucceeded</xsl:attribute>
			   </xsl:element>
			   <xsl:element name="xsl:call-template">
				  <xsl:attribute name="name">CountGenericSuffixes</xsl:attribute>
				  <xsl:element name="xsl:with-param">
					 <xsl:attribute name="name">morph</xsl:attribute>
					 <xsl:attribute name="select">$morph/following-sibling::*[1]</xsl:attribute>
				  </xsl:element>
			   </xsl:element>
			</xsl:element>
		 </xsl:element>
		 <xsl:comment>
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Word = Full/Partial productions
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
</xsl:comment>
		 <xsl:element name="xsl:template">
			<xsl:attribute name="match">
			   <xsl:text>/word/seq[not(descendant::failure)]/full[not(preceding-sibling::stem) and not(following-sibling::stem)] | /word/seq[not(descendant::failure)]/partial[not(preceding-sibling::stem) and not(following-sibling::stem)]</xsl:text>
			</xsl:attribute>
			<seq>
			   <xsl:element name="xsl:attribute">
				  <xsl:attribute name="name">step</xsl:attribute>
				  <xsl:element name="xsl:text">Try to build a Word analysis node on a </xsl:element>
				  <xsl:element name="xsl:choose">
					 <xsl:element name="xsl:when">
						<xsl:attribute name="test">name()='partial'</xsl:attribute>
						<xsl:element name="xsl:text">Partial</xsl:element>
					 </xsl:element>
					 <xsl:element name="xsl:otherwise">
						<xsl:element name="xsl:text">Full</xsl:element>
					 </xsl:element>
				  </xsl:element>
				  <xsl:element name="xsl:text"> analysis node.</xsl:element>
			   </xsl:element>
			   <xsl:element name="xsl:for-each">
				  <xsl:attribute name="select">
					 <xsl:text>preceding-sibling::*</xsl:text>
				  </xsl:attribute>
				  <xsl:element name="xsl:copy-of">
					 <xsl:attribute name="select">
						<xsl:text>.</xsl:text>
					 </xsl:attribute>
				  </xsl:element>
			   </xsl:element>
			   <word>
				  <xsl:comment>percolate these features</xsl:comment>
				  <xsl:element name="xsl:attribute">
					 <xsl:attribute name="name">syncat</xsl:attribute>
					 <xsl:element name="xsl:value-of">
						<xsl:attribute name="select">@syncat</xsl:attribute>
					 </xsl:element>
				  </xsl:element>
				  <xsl:element name="xsl:attribute">
					 <xsl:attribute name="name">syncatAbbr</xsl:attribute>
					 <xsl:element name="xsl:value-of">
						<xsl:attribute name="select">@syncatAbbr</xsl:attribute>
					 </xsl:element>
				  </xsl:element>
				  <xsl:element name="xsl:if">
					 <xsl:attribute name="test">@requiresInfl='+' and @inflected='-'</xsl:attribute>
					 <failure>
						<xsl:element name="xsl:text">The category '</xsl:element>
						<xsl:element name="xsl:value-of">
						   <xsl:attribute name="select">@syncatAbbr</xsl:attribute>
						</xsl:element>
						<xsl:element name="xsl:text">' requires inflection, but there was no inflection.</xsl:element>
					 </failure>
				  </xsl:element>
				  <xsl:element name="xsl:copy-of">
					 <xsl:attribute name="select">
						<xsl:text>.</xsl:text>
					 </xsl:attribute>
				  </xsl:element>
			   </word>
			   <xsl:element name="xsl:for-each">
				  <xsl:attribute name="select">following-sibling::*</xsl:attribute>
				  <xsl:element name="xsl:copy-of">
					 <xsl:attribute name="select">
						<xsl:text>.</xsl:text>
					 </xsl:attribute>
				  </xsl:element>
			   </xsl:element>
			</seq>
			<xsl:element name="xsl:choose">
			   <xsl:element name="xsl:when">
				  <xsl:attribute name="test">name()!='partial' and preceding-sibling::*[1][@wordType='circumPfx'] and following-sibling::*[1][@wordType='circumSfx']</xsl:attribute>
				  <xsl:element name="xsl:call-template">
					 <xsl:attribute name="name">CircumfixPartialOnFull</xsl:attribute>
				  </xsl:element>
			   </xsl:element>
			   <xsl:element name="xsl:when">
				  <xsl:attribute name="test">name()!='partial' and preceding-sibling::*[1][@wordType='prefix'] or name()!='partial' and following-sibling::*[1][@wordType='suffix']</xsl:attribute>
				  <xsl:element name="xsl:call-template">
					 <xsl:attribute name="name">GenericAffixPartial</xsl:attribute>
				  </xsl:element>
			   </xsl:element>
			   <xsl:element name="xsl:when">
				  <xsl:attribute name="test">name()='partial' and preceding-sibling::*[1][@wordType='prefix' or @wordType='derivPfx'] or name()='partial' and following-sibling::*[1][@wordType='suffix' or @wordType='derivSfx']</xsl:attribute>
				  <xsl:element name="xsl:call-template">
					 <xsl:attribute name="name">GenericAffixPartial</xsl:attribute>
				  </xsl:element>
			   </xsl:element>
			   <xsl:element name="xsl:otherwise">
				  <xsl:element name="xsl:if">
					 <xsl:attribute name="test">name()='partial'</xsl:attribute>
					 <xsl:comment>    invoke inflectional templates </xsl:comment>
					 <xsl:for-each select="$affixTemplates">
						<xsl:call-template name="CreateInvocationsOfPartialInflectionalTemplate">
						   <xsl:with-param name="sTemplate">
							  <xsl:value-of select="@Id"/>
						   </xsl:with-param>
						   <xsl:with-param name="pos" select="../.."/>
						</xsl:call-template>
					 </xsl:for-each>
				  </xsl:element>
			   </xsl:element>
			</xsl:element>
		 </xsl:element>
		 <xsl:comment>
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OrthographicWord = Word productions
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
</xsl:comment>
		 <xsl:element name="xsl:template">
			<xsl:attribute name="match">
			   <xsl:text>/word/seq[not(descendant::failure)]/word</xsl:text>
			</xsl:attribute>
			<wgd:variable name="word" select="."/>
			<seq step="Try to build an Orthographic Word node on a Word, including any clitics.">
			   <xsl:element name="xsl:variable">
				  <xsl:attribute name="name">ProcliticMorphs</xsl:attribute>
				  <xsl:element name="xsl:call-template">
					 <xsl:attribute name="name">CountProclitics</xsl:attribute>
					 <xsl:element name="xsl:with-param">
						<xsl:attribute name="name">morph</xsl:attribute>
						<xsl:attribute name="select">preceding-sibling::*[1]</xsl:attribute>
					 </xsl:element>
				  </xsl:element>
			   </xsl:element>
			   <xsl:element name="xsl:variable">
				  <xsl:attribute name="name">iProcliticMorphs</xsl:attribute>
				  <xsl:element name="xsl:value-of">
					 <xsl:attribute name="select">string-length($ProcliticMorphs)</xsl:attribute>
				  </xsl:element>
			   </xsl:element>
			   <xsl:element name="xsl:variable">
				  <xsl:attribute name="name">EncliticMorphs</xsl:attribute>
				  <xsl:element name="xsl:call-template">
					 <xsl:attribute name="name">CountEnclitics</xsl:attribute>
					 <xsl:element name="xsl:with-param">
						<xsl:attribute name="name">morph</xsl:attribute>
						<xsl:attribute name="select">following-sibling::*[1]</xsl:attribute>
					 </xsl:element>
				  </xsl:element>
			   </xsl:element>
			   <xsl:element name="xsl:variable">
				  <xsl:attribute name="name">iEncliticMorphs</xsl:attribute>
				  <xsl:element name="xsl:value-of">
					 <xsl:attribute name="select">string-length($EncliticMorphs)</xsl:attribute>
				  </xsl:element>
			   </xsl:element>
			   <xsl:comment> copy what's before the orthographic word </xsl:comment>
			   <xsl:element name="xsl:for-each">
				  <xsl:attribute name="select">preceding-sibling::*[position()&gt;$iProcliticMorphs]</xsl:attribute>
				  <xsl:element name="xsl:copy-of">
					 <xsl:attribute name="select">.</xsl:attribute>
				  </xsl:element>
				  <failure>Only proclitics can be before a Word analysis node.</failure>
			   </xsl:element>
			   <orthographicWord>
				  <xsl:comment>copy any proclitics</xsl:comment>
				  <xsl:element name="xsl:if">
					 <xsl:attribute name="test">$iProcliticMorphs&gt; 0</xsl:attribute>
					 <xsl:element name="xsl:for-each">
						<xsl:attribute name="select">preceding-sibling::*[position()&lt;=$iProcliticMorphs]</xsl:attribute>
						<xsl:element name="xsl:copy-of">
						   <xsl:attribute name="select">.</xsl:attribute>
						</xsl:element>
						<xsl:comment>apply constraints</xsl:comment>
						<xsl:element name="xsl:variable">
						   <xsl:attribute name="name">sCategoriesAreCompatible</xsl:attribute>
						   <wgd:call-template name="TestCliticFromCategories">
							  <wgd:with-param name="word" select="$word"/>
						   </wgd:call-template>
						</xsl:element>
						<wgd:if test="$sCategoriesAreCompatible != $sSuccess">
						   <wgd:call-template name="ReportCliticFailure">
							  <wgd:with-param name="sCliticType" select="'proclitic'"/>
							  <wgd:with-param name="word" select="$word"/>
						   </wgd:call-template>
						</wgd:if>
					 </xsl:element>
				  </xsl:element>
				  <xsl:element name="xsl:copy-of">
					 <xsl:attribute name="select">
						<xsl:text>.</xsl:text>
					 </xsl:attribute>
				  </xsl:element>
				  <xsl:comment>copy any enclitics</xsl:comment>
				  <xsl:element name="xsl:if">
					 <xsl:attribute name="test">$iEncliticMorphs &gt; 0</xsl:attribute>
					 <xsl:element name="xsl:for-each">
						<xsl:attribute name="select">following-sibling::*[position()&lt;=$iEncliticMorphs]</xsl:attribute>
						<xsl:element name="xsl:copy-of">
						   <xsl:attribute name="select">.</xsl:attribute>
						</xsl:element>
						<xsl:comment>apply constraints</xsl:comment>
						<xsl:element name="xsl:variable">
						   <xsl:attribute name="name">sCategoriesAreCompatible</xsl:attribute>
						   <wgd:call-template name="TestCliticFromCategories">
							  <wgd:with-param name="word" select="$word"/>
						   </wgd:call-template>
						</xsl:element>
						<wgd:if test="$sCategoriesAreCompatible != $sSuccess">
						   <wgd:call-template name="ReportCliticFailure">
							  <wgd:with-param name="sCliticType" select="'enclitic'"/>
							  <wgd:with-param name="word" select="$word"/>
						   </wgd:call-template>
						</wgd:if>
					 </xsl:element>
				  </xsl:element>
			   </orthographicWord>
			   <xsl:comment> copy what's after the orthographic word </xsl:comment>
			   <xsl:element name="xsl:for-each">
				  <xsl:attribute name="select">following-sibling::*[position()&gt;$iEncliticMorphs]</xsl:attribute>
				  <xsl:element name="xsl:copy-of">
					 <xsl:attribute name="select">.</xsl:attribute>
				  </xsl:element>
				  <failure>Only enclitics can be after a Word analysis node.</failure>
			   </xsl:element>
			</seq>
		 </xsl:element>
		 <xsl:call-template name="CountProcliticsNamedTemplate"/>
		 <xsl:call-template name="CountEncliticsNamedTemplate"/>
		 <xsl:call-template name="ReportCliticFailureNamedTemplate"/>
		 <xsl:call-template name="TestCliticFromCategoriesNamedTemplate"/>
		 <xsl:comment>
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Ignore text template
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
</xsl:comment>
		 <xsl:element name="xsl:template">
			<xsl:attribute name="match">alloform | gloss | citationForm | shortName | props | stemName | failure | inflClass | name | value</xsl:attribute>
		 </xsl:element>
		 <xsl:call-template name="ApplyDerivationalConstraintsNamedTemplate"/>
		 <xsl:call-template name="DerivCircumPfxStemDerivCircumSfxProductionNamedTemplate"/>
		 <xsl:call-template name="DerivPfxStemProductionNamedTemplate"/>
		 <xsl:call-template name="IndicateInflAffixSucceededNamedTemplate"/>
		 <xsl:call-template name="InflectionalProductionsNamedTemplate"/>
		 <xsl:call-template name="ReportFailureNamedTemplate"/>
		 <xsl:call-template name="ReportCompoundRuleFailureNamedTemplate"/>
		 <xsl:call-template name="StemDerivSfxProductionNamedTemplate"/>
		 <xsl:call-template name="StemStemProductionsNamedTemplate"/>
		 <xsl:call-template name="CompoundRuleNamedTemplates"/>
		 <xsl:call-template name="TestCompatibleCategoriesNamedTemplate"/>
		 <xsl:call-template name="UninflectedStemProductionNamedTemplate"/>
		 <xsl:call-template name="InflectionalTemplateNamedTemplates"/>
		 <xsl:call-template name="CheckStemNamesNamedTemplates"/>
		 <xsl:call-template name="OutputFeatureValueNamedTemplate"/>
		 <xsl:call-template name="OutputFeatureStructureAsTextNamedTemplate"/>
		 <xsl:call-template name="OverrideFirstFsWithSecondFsNamedTemplate"/>
		 <xsl:call-template name="CheckInflectionalAffixProdRestrictNamedTemplate"/>
		 <xsl:call-template name="CheckAffixAllomorphFeaturesNamedTemplate"/>
		 <xsl:call-template name="UnifyPrefixSlotsNamedTemplate"/>
		 <xsl:call-template name="UnifySuffixSlotsNamedTemplate"/>
		 <xsl:call-template name="UnifyTwoFeatureStructuresNamedTemplate"/>
		 <xsl:call-template name="XSubsumesYNamedTemplate"/>
	  </xsl:element>
   </xsl:template>
   <xsl:template name="CountProcliticsNamedTemplate">
	  <xsl:comment>
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 CountProclitics
		 Params: morph - morpheme to check
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  </xsl:comment>
	  <xsl:element name="xsl:template">
		 <xsl:attribute name="name">CountProclitics</xsl:attribute>
		 <xsl:element name="xsl:param">
			<xsl:attribute name="name">morph</xsl:attribute>
		 </xsl:element>
		 <xsl:element name="xsl:if">
			<xsl:attribute name="test">$morph/@wordType = 'proclitic' or $morph/@wordType = 'clitic'</xsl:attribute>
			<xsl:element name="xsl:call-template">
			   <xsl:attribute name="name">IndicateInflAffixSucceeded</xsl:attribute>
			</xsl:element>
			<xsl:element name="xsl:call-template">
			   <xsl:attribute name="name">CountProclitics</xsl:attribute>
			   <xsl:element name="xsl:with-param">
				  <xsl:attribute name="name">morph</xsl:attribute>
				  <xsl:attribute name="select">$morph/preceding-sibling::*[1]</xsl:attribute>
			   </xsl:element>
			</xsl:element>
		 </xsl:element>
	  </xsl:element>
   </xsl:template>
   <xsl:template name="CountEncliticsNamedTemplate">
	  <xsl:comment>
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 CountEnclitics
		 Params: morph - morpheme to check
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  </xsl:comment>
	  <xsl:element name="xsl:template">
		 <xsl:attribute name="name">CountEnclitics</xsl:attribute>
		 <xsl:element name="xsl:param">
			<xsl:attribute name="name">morph</xsl:attribute>
		 </xsl:element>
		 <xsl:element name="xsl:if">
			<xsl:attribute name="test">$morph/@wordType = 'enclitic' or $morph/@wordType = 'clitic'</xsl:attribute>
			<xsl:element name="xsl:call-template">
			   <xsl:attribute name="name">IndicateInflAffixSucceeded</xsl:attribute>
			</xsl:element>
			<xsl:element name="xsl:call-template">
			   <xsl:attribute name="name">CountEnclitics</xsl:attribute>
			   <xsl:element name="xsl:with-param">
				  <xsl:attribute name="name">morph</xsl:attribute>
				  <xsl:attribute name="select">$morph/following-sibling::*[1]</xsl:attribute>
			   </xsl:element>
			</xsl:element>
		 </xsl:element>
	  </xsl:element>
   </xsl:template>
   <xsl:template name="ReportCliticFailureNamedTemplate">
	  <xsl:comment>
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 ReportCliticFailure
		 Params: sCliticType - type of clitic (proclitic, enclitic)
		 word - word node
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  </xsl:comment>
	  <wgd:template name="ReportCliticFailure">
		 <wgd:param name="sCliticType" select="'proclitic'"/>
		 <wgd:param name="word"/>
		 <failure>
			<wgd:text>In attaching a </wgd:text>
			<wgd:value-of select="$sCliticType"/>
			<wgd:text>: The category (</wgd:text>
			<wgd:value-of select="$word/@syncatAbbr"/>
			<wgd:text>) of the word is incompatible with any of the categories that the proclitic "</wgd:text>
			<wgd:value-of select="shortName"/>
			<wgd:text>" must attach to (</wgd:text>
			<wgd:for-each select="stemMsa/fromPartsOfSpeech">
			   <wgd:value-of select="@fromCatAbbr"/>
			   <wgd:if test="position() != last()">
				  <wgd:text>, </wgd:text>
			   </wgd:if>
			</wgd:for-each>
			<wgd:text>).</wgd:text>
		 </failure>
	  </wgd:template>
   </xsl:template>
   <xsl:template name="TestCliticFromCategoriesNamedTemplate">
	  <xsl:comment>
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 TestCliticFromCategories
		 Params: word - word node
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  </xsl:comment>
	  <wgd:template name="TestCliticFromCategories">
		 <wgd:param name="word"/>
		 <wgd:choose>
			<wgd:when test="count(stemMsa/fromPartsOfSpeech) &gt; 0">
			   <wgd:for-each select="stemMsa/fromPartsOfSpeech">
				  <wgd:call-template name="TestCompatibleCategories">
					 <wgd:with-param name="sFirstCat">
						<wgd:value-of select="@fromCat"/>
					 </wgd:with-param>
					 <wgd:with-param name="sSecondCat">
						<wgd:value-of select="$word/@syncat"/>
					 </wgd:with-param>
				  </wgd:call-template>
			   </wgd:for-each>
			</wgd:when>
			<wgd:otherwise>
			   <wgd:value-of select="$sSuccess"/>
			</wgd:otherwise>
		 </wgd:choose>
	  </wgd:template>
   </xsl:template>
   <xsl:template name="XSubsumesYNamedTemplate">
	  <xsl:comment>
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 XSubsumesY
		 Determine if feature X subsumes feature Y
		 Parameters: X = feature to check
		 Y = feature to look in
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  </xsl:comment>
	  <wgd:template name="XSubsumesY">
		 <wgd:param name="X"/>
		 <wgd:param name="Y"/>
		 <wgd:choose>
			<wgd:when test="not($X)">
			   <wgd:text>Y</wgd:text>
			</wgd:when>
			<wgd:when test="not($Y)">
			   <wgd:text>N</wgd:text>
			</wgd:when>
			<wgd:otherwise>
			   <wgd:variable name="sResult">
				  <xsl:comment>loop through the features of both feature structures at same time, sorted by name</xsl:comment>
				  <wgd:for-each select="auto-ns1:node-set($X)/feature | auto-ns1:node-set($Y)/feature">
					 <wgd:sort select="name"/>
					 <xsl:comment>get name of this feature</xsl:comment>
					 <wgd:variable name="sName">
						<wgd:value-of select="name"/>
					 </wgd:variable>
					 <xsl:comment>get this feature if it's in the first feature structure</xsl:comment>
					 <wgd:variable name="f1SameName" select="auto-ns1:node-set($X)/feature[name=$sName]"/>
					 <xsl:comment>get this feature if it's in the second feature structure</xsl:comment>
					 <wgd:variable name="f2SameName" select="auto-ns1:node-set($Y)/feature[name=$sName]"/>
					 <wgd:choose>
						<wgd:when test="$f1SameName and $f2SameName">
						   <xsl:comment>both feature1 and feature2 have this feature name</xsl:comment>
						   <wgd:choose>
							  <wgd:when test="$f1SameName/value/fs and $f2SameName/value/fs">
								 <xsl:comment>both have nested feature structure</xsl:comment>
								 <wgd:call-template name="XSubsumesY">
									<wgd:with-param name="X" select="$f1SameName/value/fs"/>
									<wgd:with-param name="Y" select="$f2SameName/value/fs"/>
								 </wgd:call-template>
							  </wgd:when>
							  <wgd:when test="$f1SameName/value=$f2SameName/value">
								 <xsl:comment>both features have the same value; is good</xsl:comment>
								 <wgd:text>Y</wgd:text>
							  </wgd:when>
							  <wgd:otherwise>
								 <xsl:comment>there's a value conflict, so fail</xsl:comment>
								 <wgd:text>N</wgd:text>
							  </wgd:otherwise>
						   </wgd:choose>
						</wgd:when>
						<wgd:when test="$f1SameName and not($f2SameName)">
						   <xsl:comment>X has it, but Y does not, so fail</xsl:comment>
						   <wgd:text>N</wgd:text>
						</wgd:when>
						<wgd:otherwise>
						   <xsl:comment>only Y has this feature and that's OK.  Do nothing.</xsl:comment>
						</wgd:otherwise>
					 </wgd:choose>
				  </wgd:for-each>
			   </wgd:variable>
			   <wgd:choose>
				  <wgd:when test="contains($sResult, 'N')">
					 <wgd:text>N</wgd:text>
				  </wgd:when>
				  <wgd:otherwise>
					 <wgd:text>Y</wgd:text>
				  </wgd:otherwise>
			   </wgd:choose>
			</wgd:otherwise>
		 </wgd:choose>
	  </wgd:template>
   </xsl:template>
   <xsl:template name="UnifyTwoFeatureStructuresNamedTemplate">
	  <xsl:comment>
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 UnifyTwoFeatureStructures
		 Perform the unification operation on two feature structures.
		 The  &lt;fs&gt; element which is put into the output is the unification of the two feature structures.
		 Parameters: FirstFS = first feature structure
		 SecondFS = second feature structure
		 bIsTopLevel = flag for creating new id
		 sTopLevelId = id of top level
		 sRuleInfo = preamble of failure message
		 sFirstDescription = description associated with first FS, used in failure message
		 sSecondDescription = description associated with second FS, used in failure message
		 bPerformPriorityUnion = flag whether to do priority union or not
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  </xsl:comment>
	  <wgd:template name="UnifyTwoFeatureStructures">
		 <wgd:param name="FirstFS"/>
		 <wgd:param name="SecondFS"/>
		 <wgd:param name="bIsTopLevel">Y</wgd:param>
		 <wgd:param name="sTopLevelId"/>
		 <wgd:param name="sRuleInfo"/>
		 <wgd:param name="sFirstDescription"/>
		 <wgd:param name="sSecondDescription"/>
		 <wgd:param name="bPerformPriorityUnion">N</wgd:param>
		 <fs>
			<wgd:if test="$bIsTopLevel='Y'">
			   <wgd:attribute name="id">
				  <wgd:choose>
					 <wgd:when test="$bPerformPriorityUnion='Y'">
						<wgd:text>PriorityUnionOf(</wgd:text>
					 </wgd:when>
					 <wgd:otherwise>
						<wgd:text>UnificationOf(</wgd:text>
					 </wgd:otherwise>
				  </wgd:choose>
				  <wgd:choose>
					 <wgd:when test="auto-ns1:node-set($FirstFS)/@id != ''">
						<wgd:value-of select="$FirstFS/@id"/>
					 </wgd:when>
					 <wgd:otherwise>Empty</wgd:otherwise>
				  </wgd:choose>
				  <wgd:text>and</wgd:text>
				  <wgd:choose>
					 <wgd:when test="auto-ns1:node-set($SecondFS)/@id != ''">
						<wgd:value-of select="$SecondFS/@id"/>
					 </wgd:when>
					 <wgd:otherwise>Empty</wgd:otherwise>
				  </wgd:choose>
				  <wgd:text>)</wgd:text>
			   </wgd:attribute>
			</wgd:if>
			<xsl:comment>loop through the features of both feature structures at same time, sorted by name</xsl:comment>
			<wgd:for-each select="auto-ns1:node-set($FirstFS)/feature | auto-ns1:node-set($SecondFS)/feature">
			   <wgd:sort select="name"/>
			   <xsl:comment>get name of this feature</xsl:comment>
			   <wgd:variable name="sName">
				  <wgd:value-of select="name"/>
			   </wgd:variable>
			   <xsl:comment>get this feature if it's in the first feature structure</xsl:comment>
			   <wgd:variable name="f1SameName" select="auto-ns1:node-set($FirstFS)/feature[name=$sName]"/>
			   <xsl:comment>get this feature if it's in the second feature structure </xsl:comment>
			   <wgd:variable name="f2SameName" select="auto-ns1:node-set($SecondFS)/feature[name=$sName]"/>
			   <wgd:choose>
				  <wgd:when test="$f1SameName and $f2SameName">
					 <xsl:comment>both feature1 and feature2 have this feature name</xsl:comment>
					 <wgd:if test="ancestor::fs[@id=$sTopLevelId]">
						<xsl:comment>only need to do this for the feature in the first feature structure</xsl:comment>
						<feature>
						   <name>
							  <wgd:value-of select="$sName"/>
						   </name>
						   <wgd:choose>
							  <wgd:when test="$f1SameName/value/fs and $f2SameName/value/fs">
								 <xsl:comment>both have nested feature structure</xsl:comment>
								 <value>
									<wgd:call-template name="UnifyTwoFeatureStructures">
									   <wgd:with-param name="FirstFS" select="$f1SameName/value/fs"/>
									   <wgd:with-param name="SecondFS" select="$f2SameName/value/fs"/>
									   <wgd:with-param name="bIsTopLevel">N</wgd:with-param>
									   <wgd:with-param name="sTopLevelId" select="$sTopLevelId"/>
									   <wgd:with-param name="sFirstDescription" select="$sFirstDescription"/>
									   <wgd:with-param name="sSecondDescription" select="$sSecondDescription"/>
									   <wgd:with-param name="bPerformPriorityUnion" select="$bPerformPriorityUnion"/>
									   <wgd:with-param name="sRuleInfo" select="$sRuleInfo"/>
									</wgd:call-template>
								 </value>
							  </wgd:when>
							  <wgd:when test="$f1SameName/value=$f2SameName/value">
								 <xsl:comment>both features have the same value</xsl:comment>
								 <value>
									<wgd:value-of select="$f1SameName/value"/>
								 </value>
							  </wgd:when>
							  <wgd:otherwise>
								 <xsl:comment>there's a value conflict</xsl:comment>
								 <wgd:choose>
									<wgd:when test="$bPerformPriorityUnion='Y'">
									   <xsl:comment>second feature wins</xsl:comment>
									   <wgd:copy-of select="$f2SameName/value"/>
									</wgd:when>
									<wgd:otherwise>
									   <xsl:comment>output failure element and the values</xsl:comment>
									   <wgd:copy-of select="$f1SameName/failure"/>
									   <wgd:copy-of select="$f2SameName/failure"/>
									   <wgd:if test="$f1SameName/value and $f2SameName/value">
										  <failure>
											 <wgd:value-of select="$sRuleInfo"/>
											 <xsl:text> failed because at least one inflection feature of the </xsl:text>
											 <wgd:value-of select="$sFirstDescription"/>
											 <xsl:text> is incompatible with the inflection features of the </xsl:text>
											 <wgd:value-of select="$sSecondDescription"/>
											 <xsl:text>.  The incompatibility is for feature </xsl:text>
											 <wgd:value-of select="$f1SameName/name"/>
											 <xsl:text>.  This feature for the </xsl:text>
											 <wgd:value-of select="$sFirstDescription"/>
											 <xsl:text> has a value of </xsl:text>
											 <wgd:value-of select="$f1SameName/value"/>
											 <xsl:text> but the corresponding feature for the </xsl:text>
											 <wgd:value-of select="$sSecondDescription"/>
											 <xsl:text> has a value of </xsl:text>
											 <wgd:value-of select="$f2SameName/value"/>
											 <xsl:text>.</xsl:text>
										  </failure>
									   </wgd:if>
									</wgd:otherwise>
								 </wgd:choose>
							  </wgd:otherwise>
						   </wgd:choose>
						</feature>
					 </wgd:if>
				  </wgd:when>
				  <wgd:otherwise>
					 <xsl:comment>only one of the features has this feature</xsl:comment>
					 <feature>
						<name>
						   <wgd:value-of select="name"/>
						</name>
						<value>
						   <wgd:call-template name="OutputFeatureValue">
							  <wgd:with-param name="value" select="value"/>
						   </wgd:call-template>
						</value>
					 </feature>
				  </wgd:otherwise>
			   </wgd:choose>
			</wgd:for-each>
		 </fs>
	  </wgd:template>
   </xsl:template>
   <xsl:template name="UnifySuffixSlotsNamedTemplate">
	  <xsl:comment>
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 UnifySuffixSlots
		 Perform the unification operation on suffix slots
		 Is recursive; result is the  &lt;fs&gt; element which is is the unification of the feature structures of all the slots.
		 Parameters: PreviousResult = feature structure of any previous unifications
		 SuffixSlot = morph for a suffix slot
		 iRemaining = count of suffix slots remaining to be unified
		 sRuleInfo = rule info
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  </xsl:comment>
	  <wgd:template name="UnifySuffixSlots">
		 <wgd:param name="PreviousResult"/>
		 <wgd:param name="SuffixSlot"/>
		 <wgd:param name="iRemaining"/>
		 <wgd:param name="sRuleInfo"/>
		 <wgd:variable name="CurrentUnification">
			<wgd:call-template name="UnifyTwoFeatureStructures">
			   <wgd:with-param name="FirstFS" select="$SuffixSlot/inflMsa/fs"/>
			   <wgd:with-param name="SecondFS" select="$PreviousResult"/>
			   <wgd:with-param name="sTopLevelId" select="$SuffixSlot/inflMsa/fs/@id"/>
			   <wgd:with-param name="sRuleInfo" select="$sRuleInfo"/>
			   <wgd:with-param name="sFirstDescription">
				  <wgd:text>inflectional suffix (</wgd:text>
				  <wgd:value-of select="$SuffixSlot/shortName"/>
				  <wgd:text>)</wgd:text>
			   </wgd:with-param>
			   <wgd:with-param name="sSecondDescription">
				  <wgd:value-of select="'inflectional suffixes closer to the stem'"/>
			   </wgd:with-param>
			</wgd:call-template>
		 </wgd:variable>
		 <wgd:choose>
			<wgd:when test="$iRemaining &gt; 0">
			   <wgd:call-template name="UnifySuffixSlots">
				  <wgd:with-param name="PreviousResult" select="auto-ns1:node-set($CurrentUnification)/fs"/>
				  <wgd:with-param name="SuffixSlot" select="$SuffixSlot/following-sibling::*[1]"/>
				  <wgd:with-param name="iRemaining" select="$iRemaining - 1"/>
				  <wgd:with-param name="sRuleInfo" select="$sRuleInfo"/>
			   </wgd:call-template>
			</wgd:when>
			<wgd:otherwise>
			   <wgd:copy-of select="$CurrentUnification"/>
			</wgd:otherwise>
		 </wgd:choose>
	  </wgd:template>
   </xsl:template>
   <xsl:template name="UnifyPrefixSlotsNamedTemplate">
	  <xsl:comment>
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 UnifyPrefixSlots
		 Perform the unification operation on prefix slots
		 Is recursive; result is the  &lt;fs&gt; element which is is the unification of the feature structures of all the slots.
		 Parameters: PreviousResult = feature structure of any previous unifications
		 PrefixSlot = morph for a prefix slot
		 iRemaining = count of prefix slots remaining to be unified
		 sRuleInfo = rule info
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  </xsl:comment>
	  <wgd:template name="UnifyPrefixSlots">
		 <wgd:param name="PreviousResult"/>
		 <wgd:param name="PrefixSlot"/>
		 <wgd:param name="iRemaining"/>
		 <wgd:param name="sRuleInfo"/>
		 <wgd:variable name="CurrentUnification">
			<wgd:call-template name="UnifyTwoFeatureStructures">
			   <wgd:with-param name="FirstFS" select="$PrefixSlot/inflMsa/fs"/>
			   <wgd:with-param name="SecondFS" select="$PreviousResult"/>
			   <wgd:with-param name="sTopLevelId" select="$PrefixSlot/inflMsa/fs/@id"/>
			   <wgd:with-param name="sRuleInfo" select="$sRuleInfo"/>
			   <wgd:with-param name="sFirstDescription">
				  <wgd:text>inflectional prefix (</wgd:text>
				  <wgd:value-of select="$PrefixSlot/shortName"/>
				  <wgd:text>)</wgd:text>
			   </wgd:with-param>
			   <wgd:with-param name="sSecondDescription">
				  <wgd:value-of select="'inflectional prefixes closer to the stem'"/>
			   </wgd:with-param>
			</wgd:call-template>
		 </wgd:variable>
		 <wgd:choose>
			<wgd:when test="$iRemaining &gt; 0">
			   <wgd:call-template name="UnifyPrefixSlots">
				  <wgd:with-param name="PreviousResult" select="auto-ns1:node-set($CurrentUnification)/fs"/>
				  <wgd:with-param name="PrefixSlot" select="$PrefixSlot/preceding-sibling::*[1]"/>
				  <wgd:with-param name="iRemaining" select="$iRemaining - 1"/>
				  <wgd:with-param name="sRuleInfo" select="$sRuleInfo"/>
			   </wgd:call-template>
			</wgd:when>
			<wgd:otherwise>
			   <wgd:copy-of select="$CurrentUnification"/>
			</wgd:otherwise>
		 </wgd:choose>
	  </wgd:template>
   </xsl:template>
   <xsl:template name="CheckInflectionalAffixProdRestrictNamedTemplate">
	  <xsl:comment>
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 CheckInflectionalAffixProdRestrict
		 Check for valid productivity restrictions between the stem and an inflectional affix
		 Parameters: stemMorph = stem morpheme
		 inlfMorph = inflectional affix
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  </xsl:comment>
	  <wgd:template name="CheckInflectionalAffixProdRestrict">
		 <wgd:param name="stemMorph"/>
		 <wgd:param name="inflMorph"/>
		 <wgd:param name="sType"/>
		 <wgd:for-each select="$inflMorph/inflMsa/fromProductivityRestriction">
			<wgd:variable name="sThisId" select="@id"/>
			<wgd:if test="not($stemMorph/productivityRestriction[@id=$sThisId])">
			   <wgd:call-template name="ReportFailure">
				  <!-- <wgd:with-param name="sPreface" select="$sPreface"/> -->
				  <wgd:with-param name="sFirstItemComponent">
					 <wgd:text>from exception feature</wgd:text>
				  </wgd:with-param>
				  <wgd:with-param name="sFirstItemComponentAbbr">
					 <wgd:value-of select="name"/>
				  </wgd:with-param>
				  <wgd:with-param name="sFirstItemDescription">inflectional <wgd:value-of select="$sType"/>
				  </wgd:with-param>
				  <wgd:with-param name="sFirstItemValue" select="$inflMorph/shortName"/>
				  <wgd:with-param name="sSecondItemComponent">
					 <wgd:text>exception features</wgd:text>
				  </wgd:with-param>
				  <wgd:with-param name="sSecondItemComponentAbbr"/>
				  <wgd:with-param name="sSecondItemDescription"> stem</wgd:with-param>
			   </wgd:call-template>
			</wgd:if>
		 </wgd:for-each>
	  </wgd:template>
   </xsl:template>
   <xsl:template name="OverrideFirstFsWithSecondFsNamedTemplate">
	  <xsl:comment>
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 OverrideFirstFsWithSecondFs
		 Perform priority union where feature2 overrides what's in feature1
		 Parameters: FirstFS = first feature structure to be overriden
		 SecondFS = second feature structure which overrides first fs
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  </xsl:comment>
	  <wgd:template name="OverrideFirstFsWithSecondFs">
		 <wgd:param name="FirstFS"/>
		 <wgd:param name="SecondFS"/>
		 <wgd:call-template name="UnifyTwoFeatureStructures">
			<wgd:with-param name="FirstFS" select="$FirstFS"/>
			<wgd:with-param name="SecondFS" select="$SecondFS"/>
			<wgd:with-param name="sTopLevelId" select="$FirstFS/@id"/>
			<wgd:with-param name="bPerformPriorityUnion" select="'Y'"/>
		 </wgd:call-template>
	  </wgd:template>
   </xsl:template>
   <xsl:template name="OutputFeatureStructureAsTextNamedTemplate">
	  <xsl:comment>
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 OutputFeatureStructureAsText
		 Show a feature structure
		 Parameters: fs = feature structure to show
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  </xsl:comment>
	  <wgd:template name="OutputFeatureStructureAsText">
		 <wgd:param name="fs"/>
		 <wgd:choose>
			<wgd:when test="$fs/feature">
			   <wgd:text disable-output-escaping="yes">[</wgd:text>
			   <wgd:for-each select="$fs/feature">
				  <wgd:if test="position()!=1">
					 <wgd:text disable-output-escaping="yes">&#xa0;</wgd:text>
				  </wgd:if>
				  <wgd:value-of select="name"/>
				  <wgd:text disable-output-escaping="yes">:</wgd:text>
				  <wgd:choose>
					 <wgd:when test="value/fs">
						<wgd:for-each select="value/fs">
						   <wgd:call-template name="OutputFeatureStructureAsText">
							  <wgd:with-param name="fs" select="."/>
						   </wgd:call-template>
						</wgd:for-each>
					 </wgd:when>
					 <wgd:otherwise>
						<wgd:value-of select="value"/>
					 </wgd:otherwise>
				  </wgd:choose>
			   </wgd:for-each>
			   <wgd:text>]</wgd:text>
			</wgd:when>
			<wgd:otherwise>
			   <wgd:text>(none)</wgd:text>
			</wgd:otherwise>
		 </wgd:choose>
	  </wgd:template>
   </xsl:template>
   <xsl:template name="OutputFeatureValueNamedTemplate">
	  <xsl:comment>
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 OutputFeatureValue
		 Output the value of the given feature.  If it's atomic, output the atomic value otherwise copy the embedded  &lt;fs&gt;
		 Parameters: value = value to output
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  </xsl:comment>
	  <wgd:template name="OutputFeatureValue">
		 <wgd:param name="value"/>
		 <wgd:choose>
			<wgd:when test="$value/fs">
			   <wgd:copy-of select="$value/fs"/>
			</wgd:when>
			<wgd:otherwise>
			   <wgd:value-of select="$value"/>
			</wgd:otherwise>
		 </wgd:choose>
	  </wgd:template>
   </xsl:template>
   <xsl:template name="CheckStemNamesNamedTemplates">
	  <xsl:for-each select="$partsOfSpeech">
		 <xsl:variable name="pos" select="."/>
		 <xsl:comment>
			- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
			CheckStemNames<xsl:value-of select="@Id"/>
			Params: none
			- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 </xsl:comment>
		 <xsl:element name="xsl:template">
			<xsl:attribute name="name">
			   <xsl:text>CheckStemNames</xsl:text>
			   <xsl:value-of select="@Id"/>
			</xsl:attribute>
			<wgd:param name="stemName"/>
			<wgd:param name="InflFeatures"/>
			<xsl:variable name="stemnames" select="StemNames/MoStemName"/>
			<xsl:choose>
			   <xsl:when test="$stemnames">
				  <wgd:choose>
					 <xsl:for-each select="$stemnames">
						<wgd:when>
						   <xsl:attribute name="test">
							  <xsl:text>$stemName='</xsl:text>
							  <xsl:value-of select="@Id"/>
							  <xsl:text>'</xsl:text>
						   </xsl:attribute>
						   <!-- check each fs in regions for subsumption -->
						   <xsl:variable name="regions" select="Regions/FsFeatStruc[descendant::FsClosedValue]"/>
						   <wgd:variable name="sStemNameSubsumesRegionFs">
							  <xsl:choose>
								 <xsl:when test="$regions">
									<xsl:for-each select="$regions">
									   <xsl:variable name="regionFsWithId">
										  <xsl:text>regionFs</xsl:text>
										  <xsl:value-of select="@Id"/>
									   </xsl:variable>
									   <wgd:variable name="{$regionFsWithId}">
										  <xsl:call-template name="OutputFsFeatStrucAsXml"/>
									   </wgd:variable>
									   <wgd:call-template name="XSubsumesY">
										  <wgd:with-param name="X" select="auto-ns1:node-set(${$regionFsWithId})/fs"/>
										  <wgd:with-param name="Y" select="auto-ns1:node-set($InflFeatures)/fs"/>
									   </wgd:call-template>
									</xsl:for-each>
								 </xsl:when>
								 <xsl:otherwise>
									<!-- no features in the regions, so it always will subsume -->
									<xsl:text>Y</xsl:text>
								 </xsl:otherwise>
							  </xsl:choose>
						   </wgd:variable>
						   <wgd:if test="not(contains($sStemNameSubsumesRegionFs,'Y'))">
							  <!-- no feature structure in regions succeeded, so is a failure -->
							  <failure>
								 <xsl:text>A stem allomorph belongs to Stem Name '</xsl:text>
								 <xsl:value-of select="Name"/>
								 <xsl:text>' so the word must be inflected for certain features, but it is not.  The </xsl:text>
								 <xsl:choose>
									<xsl:when test="count($regions) = 1">
									   <xsl:text>required feature set it must be inflected for is: </xsl:text>
									</xsl:when>
									<xsl:otherwise>
									   <xsl:text>possible feature sets it must be inflected for are: </xsl:text>
									</xsl:otherwise>
								 </xsl:choose>
								 <xsl:for-each select="$regions">
									<xsl:if test="position()!=1">
									   <xsl:text> or </xsl:text>
									</xsl:if>
									<xsl:call-template name="OutputFsFeatStrucAsText"/>
								 </xsl:for-each>
								 <xsl:text>.  The inflected features for this word are: </xsl:text>
								 <wgd:for-each select="auto-ns1:node-set($InflFeatures)">
									<wgd:call-template name="OutputFeatureStructureAsText">
									   <wgd:with-param name="fs" select="fs"/>
									</wgd:call-template>
								 </wgd:for-each>
								 <xsl:text>.</xsl:text>
							  </failure>
						   </wgd:if>
						</wgd:when>
					 </xsl:for-each>
					 <xsl:call-template name="OutputElsewhereChecksForStemNamesUsedInLexicalEntries">
						<xsl:with-param name="sList" select="$sUniqueStemNamesUsedInLexicalEntries"/>
						<xsl:with-param name="pos" select="$pos"/>
					 </xsl:call-template>
					 <wgd:otherwise>
						<!-- check for stem names in any POSes higher up -->
						<xsl:call-template name="CreateStemNameCheckForSuperPOS"/>
					 </wgd:otherwise>
				  </wgd:choose>
			   </xsl:when>
			   <xsl:otherwise>
				  <!-- check for stem names in any POSes higher up -->
				  <xsl:call-template name="CreateStemNameCheckForSuperPOS"/>
			   </xsl:otherwise>
			</xsl:choose>
		 </xsl:element>
	  </xsl:for-each>
   </xsl:template>
   <xsl:template name="InflectionalTemplateNamedTemplates">
	  <xsl:for-each select="$affixTemplates">
		 <xsl:comment>
			- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
			InflectionalTemplate<xsl:value-of select="@Id"/>
			Params: none
			- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 </xsl:comment>
		 <xsl:element name="xsl:template">
			<xsl:attribute name="name">
			   <xsl:text>InflectionalTemplate</xsl:text>
			   <xsl:value-of select="@Id"/>
			</xsl:attribute>
			<seq>
			   <xsl:attribute name="step">
				  <xsl:text>Try to apply inflectional template '</xsl:text>
				  <xsl:value-of select="Name"/>
				  <xsl:text>'.  The result will be a </xsl:text>
				  <xsl:choose>
					 <xsl:when test="@Final='false'">Stem</xsl:when>
					 <xsl:otherwise>Full</xsl:otherwise>
				  </xsl:choose>
				  <xsl:text> analysis node.</xsl:text>
			   </xsl:attribute>
			   <xsl:call-template name="CreateInflectionalTemplateInnards">
				  <xsl:with-param name="template" select="."/>
				  <xsl:with-param name="pos" select="../.."/>
				  <xsl:with-param name="bPartial" select="'N'"/>
			   </xsl:call-template>
			</seq>
		 </xsl:element>
		 <xsl:call-template name="CreatePartialInflectionalTemplate">
			<xsl:with-param name="template" select="."/>
			<xsl:with-param name="pos" select="../.."/>
		 </xsl:call-template>
		 <xsl:variable name="iTemplateId" select="@Id"/>
		 <xsl:for-each select="PrefixSlots">
			<xsl:sort select="position()" order="descending" data-type="number"/>
			<xsl:variable name="thisSlot" select="@dst"/>
			<xsl:call-template name="CreateInflectionalTemplateSlotTemplate">
			   <xsl:with-param name="sType">prefix</xsl:with-param>
			   <xsl:with-param name="iTemplateId">
				  <xsl:value-of select="$iTemplateId"/>
			   </xsl:with-param>
			</xsl:call-template>
		 </xsl:for-each>
		 <xsl:for-each select="SuffixSlots">
			<xsl:variable name="thisSlot" select="@dst"/>
			<xsl:call-template name="CreateInflectionalTemplateSlotTemplate">
			   <xsl:with-param name="sType">suffix</xsl:with-param>
			   <xsl:with-param name="iTemplateId">
				  <xsl:value-of select="$iTemplateId"/>
			   </xsl:with-param>
			</xsl:call-template>
		 </xsl:for-each>
	  </xsl:for-each>
   </xsl:template>
   <xsl:template name="UninflectedStemProductionNamedTemplate">
	  <xsl:comment>
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 UninflectedStemProduction
		 Params: none
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  </xsl:comment>
	  <xsl:element name="xsl:template">
		 <xsl:attribute name="name">UninflectedStemProduction</xsl:attribute>
		 <seq step="Try to build a Full analysis node on an uninflected Stem node.">
			<xsl:comment>copy what's before the stem</xsl:comment>
			<xsl:element name="xsl:for-each">
			   <xsl:attribute name="select">preceding-sibling::*</xsl:attribute>
			   <xsl:element name="xsl:copy-of">
				  <xsl:attribute name="select">.</xsl:attribute>
			   </xsl:element>
			</xsl:element>
			<xsl:comment>have a full</xsl:comment>
			<full>
			   <xsl:element name="xsl:attribute">
				  <xsl:attribute name="name">syncat</xsl:attribute>
				  <xsl:element name="xsl:value-of">
					 <xsl:attribute name="select">@syncat</xsl:attribute>
				  </xsl:element>
			   </xsl:element>
			   <xsl:element name="xsl:attribute">
				  <xsl:attribute name="name">syncatAbbr</xsl:attribute>
				  <xsl:element name="xsl:value-of">
					 <xsl:attribute name="select">@syncatAbbr</xsl:attribute>
				  </xsl:element>
			   </xsl:element>
			   <xsl:element name="xsl:attribute">
				  <xsl:attribute name="name">requiresInfl</xsl:attribute>
				  <xsl:element name="xsl:value-of">
					 <xsl:attribute name="select">@requiresInfl</xsl:attribute>
				  </xsl:element>
			   </xsl:element>
			   <xsl:element name="xsl:attribute">
				  <xsl:attribute name="name">inflected</xsl:attribute>
				  <xsl:element name="xsl:text">-</xsl:element>
			   </xsl:element>
			   <!-- Hmm, the word grammar allows this - maybe for unmarked affixes??
				  <xsl:element name="xsl:if">
				  <xsl:attribute name="test">@requiresInfl='+'</xsl:attribute>
				  <failure>
				  <xsl:element name="xsl:text">Tried to make the stem be uninflected, but the stem's category (</xsl:element>
				  <xsl:element name="xsl:value-of">
				  <xsl:attribute name="select">@syncatAbbr</xsl:attribute>
				  </xsl:element>
				  <xsl:element name="xsl:text">) requires inflection.</xsl:element>
				  </failure>
				  </xsl:element>
			   -->
			   <xsl:element name="xsl:if">
				  <xsl:attribute name="test">@blocksInflection='+'</xsl:attribute>
				  <failure>
					 <xsl:element name="xsl:text">Tried to make the stem be uninflected, but the stem has been inflected via a template that requires more derivation.  Therefore, a derivational affix or a compound rule must apply first.</xsl:element>
				  </failure>
			   </xsl:element>
			   <xsl:element name="xsl:copy-of">
				  <xsl:attribute name="select">.</xsl:attribute>
			   </xsl:element>
			</full>
			<xsl:comment> copy what's after the stem </xsl:comment>
			<xsl:element name="xsl:for-each">
			   <xsl:attribute name="select">following-sibling::*</xsl:attribute>
			   <xsl:element name="xsl:copy-of">
				  <xsl:attribute name="select">.</xsl:attribute>
			   </xsl:element>
			</xsl:element>
		 </seq>
	  </xsl:element>
   </xsl:template>
   <xsl:template name="TestCompatibleCategoriesNamedTemplate">
	  <xsl:comment>
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 TestCompatibleCategories
		 Params: firstCat
		 secondCat
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  </xsl:comment>
	  <xsl:element name="xsl:template">
		 <xsl:attribute name="name">TestCompatibleCategories</xsl:attribute>
		 <xsl:element name="xsl:param">
			<xsl:attribute name="name">sFirstCat</xsl:attribute>
		 </xsl:element>
		 <xsl:element name="xsl:param">
			<xsl:attribute name="name">sSecondCat</xsl:attribute>
		 </xsl:element>
		 <xsl:element name="xsl:choose">
			<xsl:element name="xsl:when">
			   <xsl:attribute name="test">not($sFirstCat) or $sFirstCat = '0' or $sFirstCat = '' or $sSecondCat = ''</xsl:attribute>
			   <xsl:element name="xsl:value-of">
				  <xsl:attribute name="select">$sSuccess</xsl:attribute>
			   </xsl:element>
			</xsl:element>
			<xsl:element name="xsl:when">
			   <xsl:attribute name="test">$sFirstCat = $sSecondCat</xsl:attribute>
			   <xsl:element name="xsl:value-of">
				  <xsl:attribute name="select">$sSuccess</xsl:attribute>
			   </xsl:element>
			</xsl:element>
			<xsl:comment> build all the possible subcategory combinations here </xsl:comment>
			<xsl:for-each select="$partsOfSpeech">
			   <xsl:if test="SubPossibilities">
				  <xsl:element name="xsl:when">
					 <xsl:attribute name="test">
						<xsl:text>$sFirstCat = '</xsl:text>
						<xsl:value-of select="@Id"/>
						<xsl:text>'</xsl:text>
					 </xsl:attribute>
					 <xsl:variable name="sTestContents">
						<xsl:call-template name="NestedPOSids">
						   <xsl:with-param name="pos" select="@Id"/>
						</xsl:call-template>
					 </xsl:variable>
					 <xsl:element name="xsl:if">
						<xsl:attribute name="test">
						   <xsl:value-of select="$sTestContents"/>
						</xsl:attribute>
						<xsl:element name="xsl:value-of">
						   <xsl:attribute name="select">
							  <xsl:text>$sSuccess</xsl:text>
						   </xsl:attribute>
						</xsl:element>
					 </xsl:element>
				  </xsl:element>
			   </xsl:if>
			</xsl:for-each>
		 </xsl:element>
	  </xsl:element>
   </xsl:template>
   <xsl:template name="CompoundRuleNamedTemplates">
	  <xsl:if test="$compoundRules">
		 <xsl:for-each select="$compounds">
			<xsl:variable name="LeftMsa" select="key('StemMsaID',LeftMsa/@dst)"/>
			<xsl:variable name="RightMsa" select="key('StemMsaID',RightMsa/@dst)"/>
			<xsl:variable name="LeftPartOfSpeech" select="$LeftMsa/@PartOfSpeech"/>
			<xsl:variable name="RightPartOfSpeech" select="$RightMsa/@PartOfSpeech"/>
			<xsl:comment>
			   - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
			   CompoundRule<xsl:value-of select="@Id"/>
			   Params: LeftMember - left stem
			   RightMember - right stem
			   - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
			</xsl:comment>
			<xsl:element name="xsl:template">
			   <xsl:attribute name="name">
				  <xsl:text>CompoundRule</xsl:text>
				  <xsl:value-of select="@Id"/>
			   </xsl:attribute>
			   <xsl:element name="xsl:param">
				  <xsl:attribute name="name">LeftMember</xsl:attribute>
			   </xsl:element>
			   <xsl:element name="xsl:param">
				  <xsl:attribute name="name">RightMember</xsl:attribute>
			   </xsl:element>
			   <seq>
				  <xsl:attribute name="step">
					 <xsl:text>Try to apply the Compound rule '</xsl:text>
					 <xsl:value-of select="normalize-space(Name)"/>
					 <xsl:text>'.  The result will be a new Stem node.</xsl:text>
				  </xsl:attribute>
				  <xsl:comment>copy what's before the left member stem </xsl:comment>
				  <xsl:element name="xsl:for-each">
					 <xsl:attribute name="select">$LeftMember/preceding-sibling::*</xsl:attribute>
					 <xsl:element name="xsl:copy-of">
						<xsl:attribute name="select">.</xsl:attribute>
					 </xsl:element>
				  </xsl:element>
				  <xsl:comment>have a new stem</xsl:comment>
				  <stem>
					 <xsl:element name="xsl:variable">
						<xsl:attribute name="name">sLeftCategoriesAreCompatible</xsl:attribute>
						<xsl:element name="xsl:call-template">
						   <xsl:attribute name="name">TestCompatibleCategories</xsl:attribute>
						   <xsl:element name="xsl:with-param">
							  <xsl:attribute name="name">sFirstCat</xsl:attribute>
							  <xsl:value-of select="$LeftPartOfSpeech"/>
						   </xsl:element>
						   <xsl:element name="xsl:with-param">
							  <xsl:attribute name="name">sSecondCat</xsl:attribute>
							  <xsl:element name="xsl:value-of">
								 <xsl:attribute name="select">$LeftMember/@syncat</xsl:attribute>
							  </xsl:element>
						   </xsl:element>
						</xsl:element>
					 </xsl:element>
					 <xsl:element name="xsl:variable">
						<xsl:attribute name="name">sRightCategoriesAreCompatible</xsl:attribute>
						<xsl:element name="xsl:call-template">
						   <xsl:attribute name="name">TestCompatibleCategories</xsl:attribute>
						   <xsl:element name="xsl:with-param">
							  <xsl:attribute name="name">sFirstCat</xsl:attribute>
							  <xsl:value-of select="$RightPartOfSpeech"/>
						   </xsl:element>
						   <xsl:element name="xsl:with-param">
							  <xsl:attribute name="name">sSecondCat</xsl:attribute>
							  <xsl:element name="xsl:value-of">
								 <xsl:attribute name="select">$RightMember/@syncat</xsl:attribute>
							  </xsl:element>
						   </xsl:element>
						</xsl:element>
					 </xsl:element>
					 <xsl:element name="xsl:choose">
						<xsl:element name="xsl:when">
						   <xsl:attribute name="test">$sLeftCategoriesAreCompatible = $sSuccess and $sRightCategoriesAreCompatible = $sSuccess</xsl:attribute>
						   <xsl:element name="xsl:call-template">
							  <xsl:attribute name="name">ApplyCompoundRule</xsl:attribute>
							  <xsl:element name="xsl:with-param">
								 <xsl:attribute name="name">LeftMember</xsl:attribute>
								 <xsl:attribute name="select">$LeftMember</xsl:attribute>
							  </xsl:element>
							  <xsl:element name="xsl:with-param">
								 <xsl:attribute name="name">RightMember</xsl:attribute>
								 <xsl:attribute name="select">$RightMember</xsl:attribute>
							  </xsl:element>
							  <xsl:element name="xsl:with-param">
								 <xsl:attribute name="name">sRuleHvo</xsl:attribute>
								 <xsl:value-of select="@Id"/>
							  </xsl:element>
						   </xsl:element>
						</xsl:element>
						<xsl:element name="xsl:otherwise">
						   <xsl:element name="xsl:if">
							  <xsl:attribute name="test">$sLeftCategoriesAreCompatible != $sSuccess</xsl:attribute>
							  <xsl:element name="xsl:call-template">
								 <xsl:attribute name="name">ReportCompoundRuleFailure</xsl:attribute>
								 <xsl:element name="xsl:with-param">
									<xsl:attribute name="name">sPreface</xsl:attribute>
									<xsl:text>In applying the compound rule "</xsl:text>
									<xsl:value-of select="Name"/>
									<xsl:text>": </xsl:text>
								 </xsl:element>
								 <xsl:element name="xsl:with-param">
									<xsl:attribute name="name">sFirstItemComponent</xsl:attribute>
									<xsl:element name="xsl:text">category</xsl:element>
								 </xsl:element>
								 <xsl:element name="xsl:with-param">
									<xsl:attribute name="name">sFirstItemComponentAbbr</xsl:attribute>
									<xsl:attribute name="select">$LeftMember/@syncatAbbr</xsl:attribute>
								 </xsl:element>
								 <xsl:element name="xsl:with-param">
									<xsl:attribute name="name">sFirstItemDescription</xsl:attribute>
									<xsl:element name="xsl:text">left-hand stem</xsl:element>
								 </xsl:element>
								 <xsl:element name="xsl:with-param">
									<xsl:attribute name="name">sFirstItemValue</xsl:attribute>
									<xsl:attribute name="select">$LeftMember</xsl:attribute>
								 </xsl:element>
								 <xsl:element name="xsl:with-param">
									<xsl:attribute name="name">sSecondItemComponent</xsl:attribute>
									<xsl:element name="xsl:text">category</xsl:element>
								 </xsl:element>
								 <xsl:element name="xsl:with-param">
									<xsl:attribute name="name">sSecondItemComponentAbbr</xsl:attribute>
									<xsl:value-of select="key('POSID',$LeftPartOfSpeech)/Abbreviation"/>
								 </xsl:element>
								 <xsl:element name="xsl:with-param">
									<xsl:attribute name="name">sSecondItemDescription</xsl:attribute>
									<xsl:element name="xsl:text">left-hand member of the compound rule</xsl:element>
								 </xsl:element>
							  </xsl:element>
						   </xsl:element>
						   <xsl:element name="xsl:if">
							  <xsl:attribute name="test">$sRightCategoriesAreCompatible != $sSuccess</xsl:attribute>
							  <xsl:element name="xsl:call-template">
								 <xsl:attribute name="name">ReportCompoundRuleFailure</xsl:attribute>
								 <xsl:element name="xsl:with-param">
									<xsl:attribute name="name">sPreface</xsl:attribute>
									<xsl:text>In applying the compound rule "</xsl:text>
									<xsl:value-of select="Name"/>
									<xsl:text>": </xsl:text>
								 </xsl:element>
								 <xsl:element name="xsl:with-param">
									<xsl:attribute name="name">sFirstItemComponent</xsl:attribute>
									<xsl:element name="xsl:text">category</xsl:element>
								 </xsl:element>
								 <xsl:element name="xsl:with-param">
									<xsl:attribute name="name">sFirstItemComponentAbbr</xsl:attribute>
									<xsl:attribute name="select">$RightMember/@syncatAbbr</xsl:attribute>
								 </xsl:element>
								 <xsl:element name="xsl:with-param">
									<xsl:attribute name="name">sFirstItemDescription</xsl:attribute>
									<xsl:element name="xsl:text">right-hand stem</xsl:element>
								 </xsl:element>
								 <xsl:element name="xsl:with-param">
									<xsl:attribute name="name">sFirstItemValue</xsl:attribute>
									<xsl:attribute name="select">$RightMember</xsl:attribute>
								 </xsl:element>
								 <xsl:element name="xsl:with-param">
									<xsl:attribute name="name">sSecondItemComponent</xsl:attribute>
									<xsl:element name="xsl:text">category</xsl:element>
								 </xsl:element>
								 <xsl:element name="xsl:with-param">
									<xsl:attribute name="name">sSecondItemComponentAbbr</xsl:attribute>
									<xsl:value-of select="key('POSID',$RightPartOfSpeech)/Abbreviation"/>
								 </xsl:element>
								 <xsl:element name="xsl:with-param">
									<xsl:attribute name="name">sSecondItemDescription</xsl:attribute>
									<xsl:element name="xsl:text">right-hand member of the compound rule</xsl:element>
								 </xsl:element>
							  </xsl:element>
						   </xsl:element>
						</xsl:element>
					 </xsl:element>
					 <xsl:element name="xsl:copy-of">
						<xsl:attribute name="select">$LeftMember</xsl:attribute>
					 </xsl:element>
					 <xsl:element name="xsl:copy-of">
						<xsl:attribute name="select">$RightMember</xsl:attribute>
					 </xsl:element>
				  </stem>
				  <xsl:comment> copy what's after the right member stem </xsl:comment>
				  <xsl:element name="xsl:for-each">
					 <xsl:attribute name="select">$RightMember/following-sibling::*</xsl:attribute>
					 <xsl:element name="xsl:copy-of">
						<xsl:attribute name="select">.</xsl:attribute>
					 </xsl:element>
				  </xsl:element>
			   </seq>
			</xsl:element>
		 </xsl:for-each>
		 <xsl:comment>
			- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
			ApplyCompoundRule
			Params: LeftMember - left stem
			RightMember - right stem
			sRuleHvo - hvo of the rule
			- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 </xsl:comment>
		 <xsl:element name="xsl:template">
			<xsl:attribute name="name">ApplyCompoundRule</xsl:attribute>
			<xsl:element name="xsl:param">
			   <xsl:attribute name="name">LeftMember</xsl:attribute>
			</xsl:element>
			<xsl:element name="xsl:param">
			   <xsl:attribute name="name">RightMember</xsl:attribute>
			</xsl:element>
			<xsl:element name="xsl:param">
			   <xsl:attribute name="name">sRuleHvo</xsl:attribute>
			</xsl:element>
			<xsl:element name="xsl:choose">
			   <xsl:for-each select="$compounds">
				  <xsl:variable name="LeftMsa" select="key('StemMsaID',LeftMsa/@dst)"/>
				  <xsl:variable name="RightMsa" select="key('StemMsaID',RightMsa/@dst)"/>
				  <xsl:variable name="OverridingMsa" select="key('StemMsaID',OverridingMsa/@dst)"/>
				  <xsl:variable name="ToMsa" select="key('StemMsaID',ToMsa/@dst)"/>
				  <xsl:variable name="LeftPartOfSpeech" select="$LeftMsa/@PartOfSpeech"/>
				  <xsl:variable name="RightPartOfSpeech" select="$RightMsa/@PartOfSpeech"/>
				  <xsl:variable name="ToPartOfSpeech" select="$ToMsa/@PartOfSpeech"/>
				  <xsl:variable name="ToInflectionClass" select="$ToMsa/@InflectionClass"/>
				  <xsl:element name="xsl:when">
					 <xsl:attribute name="test">
						<xsl:text>$sRuleHvo='</xsl:text>
						<xsl:value-of select="@Id"/>
						<xsl:text>'</xsl:text>
					 </xsl:attribute>
					 <xsl:choose>
						<xsl:when test="name()='MoExoCompound'">
						   <xsl:element name="xsl:attribute">
							  <xsl:attribute name="name">syncat</xsl:attribute>
							  <xsl:value-of select="$ToPartOfSpeech"/>
						   </xsl:element>
						   <xsl:element name="xsl:attribute">
							  <xsl:attribute name="name">syncatAbbr</xsl:attribute>
							  <xsl:value-of select="$ToPartOfSpeech/Abbreviation"/>
						   </xsl:element>
						   <xsl:element name="xsl:attribute">
							  <xsl:attribute name="name">inflClass</xsl:attribute>
							  <xsl:value-of select="$ToInflectionClass"/>
						   </xsl:element>
						   <xsl:element name="xsl:attribute">
							  <xsl:attribute name="name">inflClassAbbr</xsl:attribute>
							  <xsl:value-of select="$ToInflectionClass/Abbreviation"/>
						   </xsl:element>
						   <xsl:variable name="sHasInflTemplate">
							  <xsl:call-template name="SuperPOSHasTemplate">
								 <xsl:with-param name="pos" select="$ToPartOfSpeech"/>
							  </xsl:call-template>
						   </xsl:variable>
						   <xsl:choose>
							  <xsl:when test="$sHasInflTemplate='Y'">
								 <xsl:element name="xsl:attribute">
									<xsl:attribute name="name">requiresInfl</xsl:attribute>+</xsl:element>
							  </xsl:when>
							  <xsl:otherwise>
								 <xsl:element name="xsl:attribute">
									<xsl:attribute name="name">requiresInfl</xsl:attribute>-</xsl:element>
							  </xsl:otherwise>
						   </xsl:choose>
						   <xsl:element name="xsl:attribute">
							  <xsl:attribute name="name">blocksInfl</xsl:attribute>-</xsl:element>
						</xsl:when>
						<xsl:otherwise>
						   <!-- enodocentric rule -->
						   <xsl:choose>
							  <xsl:when test="@HeadLast='1'">
								 <xsl:call-template name="PercolateHeadedCompoundInfo">
									<xsl:with-param name="Member">$RightMember</xsl:with-param>
									<xsl:with-param name="Override" select="$OverridingMsa"/>
								 </xsl:call-template>
							  </xsl:when>
							  <xsl:otherwise>
								 <xsl:call-template name="PercolateHeadedCompoundInfo">
									<xsl:with-param name="Member">$LeftMember</xsl:with-param>
									<xsl:with-param name="Override" select="$OverridingMsa"/>
								 </xsl:call-template>
							  </xsl:otherwise>
						   </xsl:choose>
						</xsl:otherwise>
					 </xsl:choose>
				  </xsl:element>
			   </xsl:for-each>
			</xsl:element>
		 </xsl:element>
	  </xsl:if>
   </xsl:template>
   <xsl:template name="StemStemProductionsNamedTemplate">
	  <xsl:comment>
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 StemStemProductions
		 Params: none
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  </xsl:comment>
	  <xsl:element name="xsl:template">
		 <xsl:attribute name="name">StemStemProductions</xsl:attribute>
		 <xsl:choose>
			<xsl:when test="$compoundRules">
			   <xsl:for-each select="$compounds">
				  <xsl:element name="xsl:call-template">
					 <xsl:attribute name="name">
						<xsl:text>CompoundRule</xsl:text>
						<xsl:value-of select="@Id"/>
					 </xsl:attribute>
					 <xsl:element name="xsl:with-param">
						<xsl:attribute name="name">LeftMember</xsl:attribute>
						<xsl:attribute name="select">.</xsl:attribute>
					 </xsl:element>
					 <xsl:element name="xsl:with-param">
						<xsl:attribute name="name">RightMember</xsl:attribute>
						<xsl:attribute name="select">following-sibling::stem[1]</xsl:attribute>
					 </xsl:element>
				  </xsl:element>
			   </xsl:for-each>
			</xsl:when>
			<xsl:otherwise>
			   <xsl:comment> stealth compounding...</xsl:comment>
			</xsl:otherwise>
		 </xsl:choose>
	  </xsl:element>
   </xsl:template>
   <xsl:template name="ApplyDerivationalConstraintsNamedTemplate">
	  <xsl:comment>
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 ApplyDerivationalConstraints
		 Params: derivMorph - derivational morpheme morph element
		 sType - 'prefix' or 'suffix' or 'circumfix'
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  </xsl:comment>
	  <xsl:element name="xsl:template">
		 <xsl:attribute name="name">ApplyDerivationalConstraints</xsl:attribute>
		 <xsl:element name="xsl:param">
			<xsl:attribute name="name">derivMorph</xsl:attribute>
		 </xsl:element>
		 <xsl:element name="xsl:param">
			<xsl:attribute name="name">sType</xsl:attribute>suffix</xsl:element>
		 <xsl:comment>apply constraints</xsl:comment>
		 <xsl:element name="xsl:variable">
			<xsl:attribute name="name">sCategoriesAreCompatible</xsl:attribute>
			<xsl:element name="xsl:call-template">
			   <xsl:attribute name="name">TestCompatibleCategories</xsl:attribute>
			   <xsl:element name="xsl:with-param">
				  <xsl:attribute name="name">sFirstCat</xsl:attribute>
				  <xsl:element name="xsl:value-of">
					 <xsl:attribute name="select">$derivMorph/derivMsa/@fromCat</xsl:attribute>
				  </xsl:element>
			   </xsl:element>
			   <xsl:element name="xsl:with-param">
				  <xsl:attribute name="name">sSecondCat</xsl:attribute>
				  <xsl:element name="xsl:value-of">
					 <xsl:attribute name="select">@syncat</xsl:attribute>
				  </xsl:element>
			   </xsl:element>
			</xsl:element>
		 </xsl:element>
		 <xsl:element name="xsl:variable">
			<xsl:attribute name="name">sEnvCategoriesAreCompatible</xsl:attribute>
			<xsl:element name="xsl:call-template">
			   <xsl:attribute name="name">TestCompatibleCategories</xsl:attribute>
			   <xsl:element name="xsl:with-param">
				  <xsl:attribute name="name">sFirstCat</xsl:attribute>
				  <xsl:element name="xsl:value-of">
					 <xsl:attribute name="select">$derivMorph/envCat/@value</xsl:attribute>
				  </xsl:element>
			   </xsl:element>
			   <xsl:element name="xsl:with-param">
				  <xsl:attribute name="name">sSecondCat</xsl:attribute>
				  <xsl:element name="xsl:value-of">
					 <xsl:attribute name="select">@syncat</xsl:attribute>
				  </xsl:element>
			   </xsl:element>
			</xsl:element>
		 </xsl:element>
		 <xsl:element name="xsl:variable">
			<xsl:attribute name="name">sFromInflectionClassIsGood</xsl:attribute>
			<xsl:element name="xsl:if">
			   <xsl:attribute name="test">$derivMorph/derivMsa/@fromInflClass = '0' or @inflClass = $derivMorph/derivMsa/@fromInflClass</xsl:attribute>
			   <xsl:element name="xsl:value-of">
				  <xsl:attribute name="select">$sSuccess</xsl:attribute>
			   </xsl:element>
			</xsl:element>
		 </xsl:element>
		 <wgd:variable name="stem" select="."/>
		 <wgd:variable name="sFromProdRestrictAreGood">
			<wgd:choose>
			   <wgd:when test="$derivMorph/derivMsa/fromProductivityRestriction">
				  <wgd:variable name="sBadProdRestrict">
					 <wgd:for-each select="$derivMorph/derivMsa/fromProductivityRestriction">
						<wgd:variable name="sThisId" select="@id"/>
						<wgd:if test="not($stem/productivityRestriction[@id=$sThisId])">
						   <wgd:value-of select="$sThisId"/>
						   <wgd:text>, </wgd:text>
						</wgd:if>
					 </wgd:for-each>
				  </wgd:variable>
				  <wgd:choose>
					 <wgd:when test="string-length($sBadProdRestrict) &gt; 0">
						<wgd:value-of select="$sBadProdRestrict"/>
					 </wgd:when>
					 <wgd:otherwise>
						<wgd:value-of select="$sSuccess"/>
					 </wgd:otherwise>
				  </wgd:choose>
			   </wgd:when>
			   <wgd:otherwise>
				  <wgd:value-of select="$sSuccess"/>
			   </wgd:otherwise>
			</wgd:choose>
		 </wgd:variable>
		 <xsl:element name="xsl:choose">
			<xsl:element name="xsl:when">
			   <xsl:attribute name="test">$sCategoriesAreCompatible = $sSuccess and $sFromInflectionClassIsGood = $sSuccess and $sEnvCategoriesAreCompatible = $sSuccess and $sFromProdRestrictAreGood = $sSuccess</xsl:attribute>
			   <xsl:comment> percolate these features </xsl:comment>
			   <xsl:element name="xsl:attribute">
				  <xsl:attribute name="name">syncat</xsl:attribute>
				  <!-- percolation and priority union for tocat -->
				  <wgd:choose>
					 <wgd:when test="$derivMorph/derivMsa/@toCat=$derivMorph/derivMsa/@fromCat">
						<wgd:value-of select="@syncat"/>
					 </wgd:when>
					 <wgd:otherwise>
						<wgd:value-of select="$derivMorph/derivMsa/@toCat"/>
					 </wgd:otherwise>
				  </wgd:choose>
			   </xsl:element>
			   <xsl:element name="xsl:attribute">
				  <xsl:attribute name="name">syncatAbbr</xsl:attribute>
				  <xsl:element name="xsl:value-of">
					 <xsl:attribute name="select">$derivMorph/derivMsa/@toCatAbbr</xsl:attribute>
				  </xsl:element>
			   </xsl:element>
			   <xsl:element name="xsl:attribute">
				  <xsl:attribute name="name">inflClass</xsl:attribute>
				  <xsl:element name="xsl:choose">
					 <xsl:element name="xsl:when">
						<xsl:attribute name="test">$derivMorph/derivMsa/@toInflClass != '0'</xsl:attribute>
						<xsl:element name="xsl:value-of">
						   <xsl:attribute name="select">$derivMorph/derivMsa/@toInflClass</xsl:attribute>
						</xsl:element>
					 </xsl:element>
					 <xsl:element name="xsl:otherwise">
						<xsl:element name="xsl:value-of">
						   <xsl:attribute name="select">@inflClass</xsl:attribute>
						</xsl:element>
					 </xsl:element>
				  </xsl:element>
			   </xsl:element>
			   <xsl:element name="xsl:attribute">
				  <xsl:attribute name="name">inflClassAbbr</xsl:attribute>
				  <xsl:element name="xsl:choose">
					 <xsl:element name="xsl:when">
						<xsl:attribute name="test">$derivMorph/derivMsa/@toInflClass != '0'</xsl:attribute>
						<xsl:element name="xsl:value-of">
						   <xsl:attribute name="select">$derivMorph/derivMsa/@toInflClassAbbr</xsl:attribute>
						</xsl:element>
					 </xsl:element>
					 <xsl:element name="xsl:otherwise">
						<xsl:element name="xsl:value-of">
						   <xsl:attribute name="select">@inflClassAbbr</xsl:attribute>
						</xsl:element>
					 </xsl:element>
				  </xsl:element>
			   </xsl:element>
			   <xsl:element name="xsl:attribute">
				  <xsl:attribute name="name">requiresInfl</xsl:attribute>
				  <xsl:element name="xsl:value-of">
					 <xsl:attribute name="select">$derivMorph/derivMsa/@requiresInfl</xsl:attribute>
				  </xsl:element>
			   </xsl:element>
			   <xsl:element name="xsl:attribute">
				  <xsl:attribute name="name">blocksInfl</xsl:attribute>
				  <xsl:element name="xsl:text">-</xsl:element>
			   </xsl:element>
			   <xsl:call-template name="UnifyFromFeaturesWithStem"/>
			   <wgd:choose>
				  <wgd:when test="$derivMorph/derivMsa/toProductivityRestriction">
					 <wgd:copy-of select="$derivMorph/derivMsa/toProductivityRestriction"/>
				  </wgd:when>
				  <wgd:otherwise>
					 <wgd:copy-of select="stemMsa/productivityRestriction"/>
				  </wgd:otherwise>
			   </wgd:choose>
			   <wgd:choose>
				  <wgd:when test="stemName">
					 <wgd:copy-of select="stemName"/>
				  </wgd:when>
				  <wgd:when test="morph/stemName">
					 <wgd:copy-of select="morph/stemName"/>
				  </wgd:when>
			   </wgd:choose>
			   <wgd:call-template name="CheckAffixAllomorphFeatures">
				  <wgd:with-param name="sAffix" select="'derivational'"/>
				  <wgd:with-param name="sAttachesTo" select="'stem'"/>
				  <wgd:with-param name="morph" select="$derivMorph"/>
				  <wgd:with-param name="stem" select="$stem"/>
			   </wgd:call-template>
			</xsl:element>
			<xsl:element name="xsl:otherwise">
			   <xsl:element name="xsl:variable">
				  <xsl:attribute name="name">sPreface</xsl:attribute>
				  <xsl:element name="xsl:text">In attaching a derivational </xsl:element>
				  <xsl:element name="xsl:value-of">
					 <xsl:attribute name="select">$sType</xsl:attribute>
				  </xsl:element>
				  <xsl:text>: </xsl:text>
			   </xsl:element>
			   <xsl:element name="xsl:if">
				  <xsl:attribute name="test">$sCategoriesAreCompatible != $sSuccess</xsl:attribute>
				  <xsl:element name="xsl:call-template">
					 <xsl:attribute name="name">ReportFailure</xsl:attribute>
					 <xsl:element name="xsl:with-param">
						<xsl:attribute name="name">sPreface</xsl:attribute>
						<xsl:element name="xsl:value-of">
						   <xsl:attribute name="select">$sPreface</xsl:attribute>
						</xsl:element>
					 </xsl:element>
					 <xsl:element name="xsl:with-param">
						<xsl:attribute name="name">sFirstItemComponent</xsl:attribute>
						<xsl:element name="xsl:text">from category</xsl:element>
					 </xsl:element>
					 <xsl:element name="xsl:with-param">
						<xsl:attribute name="name">sFirstItemComponentAbbr</xsl:attribute>
						<xsl:attribute name="select">$derivMorph/derivMsa/@fromCatAbbr</xsl:attribute>
					 </xsl:element>
					 <xsl:element name="xsl:with-param">
						<xsl:attribute name="name">sFirstItemDescription</xsl:attribute>
						<xsl:element name="xsl:text">derivational </xsl:element>
						<xsl:element name="xsl:value-of">
						   <xsl:attribute name="select">$sType</xsl:attribute>
						</xsl:element>
					 </xsl:element>
					 <xsl:element name="xsl:with-param">
						<xsl:attribute name="name">sFirstItemValue</xsl:attribute>
						<xsl:attribute name="select">$derivMorph/shortName</xsl:attribute>
					 </xsl:element>
					 <xsl:element name="xsl:with-param">
						<xsl:attribute name="name">sSecondItemComponent</xsl:attribute>
						<xsl:element name="xsl:text">category</xsl:element>
					 </xsl:element>
					 <xsl:element name="xsl:with-param">
						<xsl:attribute name="name">sSecondItemComponentAbbr</xsl:attribute>
						<xsl:attribute name="select">@syncatAbbr</xsl:attribute>
					 </xsl:element>
					 <xsl:element name="xsl:with-param">
						<xsl:attribute name="name">sSecondItemDescription</xsl:attribute>
						<xsl:element name="xsl:text">stem</xsl:element>
					 </xsl:element>
				  </xsl:element>
			   </xsl:element>
			   <xsl:element name="xsl:if">
				  <xsl:attribute name="test">$sFromInflectionClassIsGood != $sSuccess</xsl:attribute>
				  <xsl:element name="xsl:call-template">
					 <xsl:attribute name="name">ReportFailure</xsl:attribute>
					 <xsl:element name="xsl:with-param">
						<xsl:attribute name="name">sPreface</xsl:attribute>
						<xsl:element name="xsl:value-of">
						   <xsl:attribute name="select">$sPreface</xsl:attribute>
						</xsl:element>
					 </xsl:element>
					 <xsl:element name="xsl:with-param">
						<xsl:attribute name="name">sFirstItemComponent</xsl:attribute>
						<xsl:element name="xsl:text">from inflection class</xsl:element>
					 </xsl:element>
					 <xsl:element name="xsl:with-param">
						<xsl:attribute name="name">sFirstItemComponentAbbr</xsl:attribute>
						<xsl:attribute name="select">$derivMorph/derivMsa/@fromInflClassAbbr</xsl:attribute>
					 </xsl:element>
					 <xsl:element name="xsl:with-param">
						<xsl:attribute name="name">sFirstItemDescription</xsl:attribute>
						<xsl:element name="xsl:text">derivational </xsl:element>
						<xsl:element name="xsl:value-of">
						   <xsl:attribute name="select">$sType</xsl:attribute>
						</xsl:element>
					 </xsl:element>
					 <xsl:element name="xsl:with-param">
						<xsl:attribute name="name">sFirstItemValue</xsl:attribute>
						<xsl:attribute name="select">$derivMorph/shortName</xsl:attribute>
					 </xsl:element>
					 <xsl:element name="xsl:with-param">
						<xsl:attribute name="name">sSecondItemComponent</xsl:attribute>
						<xsl:element name="xsl:text">inflection class</xsl:element>
					 </xsl:element>
					 <xsl:element name="xsl:with-param">
						<xsl:attribute name="name">sSecondItemComponentAbbr</xsl:attribute>
						<xsl:attribute name="select">@inflClassAbbr</xsl:attribute>
					 </xsl:element>
					 <xsl:element name="xsl:with-param">
						<xsl:attribute name="name">sSecondItemDescription</xsl:attribute>
						<xsl:element name="xsl:text">stem</xsl:element>
					 </xsl:element>
				  </xsl:element>
			   </xsl:element>
			   <xsl:element name="xsl:if">
				  <xsl:attribute name="test">$sEnvCategoriesAreCompatible != $sSuccess</xsl:attribute>
				  <xsl:element name="xsl:call-template">
					 <xsl:attribute name="name">ReportFailure</xsl:attribute>
					 <xsl:element name="xsl:with-param">
						<xsl:attribute name="name">sPreface</xsl:attribute>
						<xsl:element name="xsl:value-of">
						   <xsl:attribute name="select">$sPreface</xsl:attribute>
						</xsl:element>
					 </xsl:element>
					 <xsl:element name="xsl:with-param">
						<xsl:attribute name="name">sFirstItemComponent</xsl:attribute>
						<xsl:element name="xsl:text">environment category</xsl:element>
					 </xsl:element>
					 <xsl:element name="xsl:with-param">
						<xsl:attribute name="name">sFirstItemComponentAbbr</xsl:attribute>
						<xsl:attribute name="select">$derivMorph/envCat/@value</xsl:attribute>
					 </xsl:element>
					 <xsl:element name="xsl:with-param">
						<xsl:attribute name="name">sFirstItemDescription</xsl:attribute>
						<xsl:element name="xsl:text">derivational </xsl:element>
						<xsl:element name="xsl:value-of">
						   <xsl:attribute name="select">$sType</xsl:attribute>
						</xsl:element>
					 </xsl:element>
					 <xsl:element name="xsl:with-param">
						<xsl:attribute name="name">sFirstItemValue</xsl:attribute>
						<xsl:attribute name="select">$derivMorph/shortName</xsl:attribute>
					 </xsl:element>
					 <xsl:element name="xsl:with-param">
						<xsl:attribute name="name">sSecondItemComponent</xsl:attribute>
						<xsl:element name="xsl:text">category</xsl:element>
					 </xsl:element>
					 <xsl:element name="xsl:with-param">
						<xsl:attribute name="name">sSecondItemComponentAbbr</xsl:attribute>
						<xsl:attribute name="select">@syncatAbbr</xsl:attribute>
					 </xsl:element>
					 <xsl:element name="xsl:with-param">
						<xsl:attribute name="name">sSecondItemDescription</xsl:attribute>
						<xsl:element name="xsl:text">stem</xsl:element>
					 </xsl:element>
				  </xsl:element>
			   </xsl:element>
			   <wgd:if test="$sFromProdRestrictAreGood  != $sSuccess">
				  <wgd:variable name="stem2" select="."/>
				  <wgd:for-each select="$derivMorph/derivMsa/fromProductivityRestriction">
					 <wgd:variable name="sThisId" select="@id"/>
					 <wgd:if test="not($stem2/productivityRestriction[@id=$sThisId])">
						<wgd:call-template name="ReportFailure">
						   <wgd:with-param name="sPreface" select="$sPreface"/>
						   <wgd:with-param name="sFirstItemComponent">
							  <wgd:text>from exception feature</wgd:text>
						   </wgd:with-param>
						   <wgd:with-param name="sFirstItemComponentAbbr">
							  <wgd:value-of select="name"/>
						   </wgd:with-param>
						   <wgd:with-param name="sFirstItemDescription">derivational <wgd:value-of select="$sType"/>
						   </wgd:with-param>
						   <wgd:with-param name="sFirstItemValue" select="$derivMorph/shortName"/>
						   <wgd:with-param name="sSecondItemComponent">
							  <wgd:text>exception features</wgd:text>
						   </wgd:with-param>
						   <wgd:with-param name="sSecondItemComponentAbbr"/>
						   <wgd:with-param name="sSecondItemDescription"> stem</wgd:with-param>
						</wgd:call-template>
					 </wgd:if>
				  </wgd:for-each>
			   </wgd:if>
			</xsl:element>
		 </xsl:element>
		 <xsl:element name="xsl:if">
			<xsl:attribute name="test">$sType='prefix' or $sType='circumfix'</xsl:attribute>
			<xsl:comment> copy derivational prefix</xsl:comment>
			<xsl:element name="xsl:copy-of">
			   <xsl:attribute name="select">preceding-sibling::morph[1]</xsl:attribute>
			</xsl:element>
		 </xsl:element>
		 <xsl:comment> copy right-hand side stem </xsl:comment>
		 <xsl:element name="xsl:copy-of">
			<xsl:attribute name="select">.</xsl:attribute>
		 </xsl:element>
		 <xsl:element name="xsl:if">
			<xsl:attribute name="test">$sType='suffix' or $sType='circumfix'</xsl:attribute>
			<xsl:comment> copy derivational suffix </xsl:comment>
			<xsl:element name="xsl:copy-of">
			   <xsl:attribute name="select">following-sibling::morph[1]</xsl:attribute>
			</xsl:element>
		 </xsl:element>
	  </xsl:element>
   </xsl:template>
   <xsl:template name="CheckAffixAllomorphFeaturesNamedTemplate">
	  <xsl:comment>
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 CheckAffixAllomorphFeatures
		 Check for valid affix allomorph features
		 Parameters: stemMorph = stem morpheme
		 inlfMorph = inflectional affix
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  </xsl:comment>
	  <wgd:template name="CheckAffixAllomorphFeatures">
		 <wgd:param name="sAffix" select="'An'"/>
		 <wgd:param name="sAttachesTo" select="stem"/>
		 <wgd:param name="morph"/>
		 <wgd:param name="stem"/>
		 <wgd:variable name="affixAlloFeats" select="$morph/affixAlloFeats/fs"/>
		 <wgd:variable name="notAffixAlloFeats" select="$morph/affixAlloFeats/not"/>
		 <wgd:variable name="sAffixAlloFeatsSubsumeInflFeatures">
			<wgd:choose>
			   <wgd:when test="$affixAlloFeats">
				  <wgd:call-template name="XSubsumesY">
					 <wgd:with-param name="X" select="auto-ns1:node-set($affixAlloFeats)"/>
					 <wgd:with-param name="Y" select="auto-ns1:node-set($stem)/fs"/>
				  </wgd:call-template>
			   </wgd:when>
			   <wgd:when test="$notAffixAlloFeats">
				  <wgd:for-each select="$notAffixAlloFeats/fs">
					 <wgd:call-template name="XSubsumesY">
						<wgd:with-param name="X" select="auto-ns1:node-set(.)"/>
						<wgd:with-param name="Y" select="auto-ns1:node-set($stem)/fs"/>
					 </wgd:call-template>
				  </wgd:for-each>
			   </wgd:when>
			   <wgd:otherwise>
				  <!-- no features in the lexical entry, so it always will subsume -->
				  <wgd:text>Y</wgd:text>
			   </wgd:otherwise>
			</wgd:choose>
		 </wgd:variable>
		 <wgd:choose>
			<wgd:when test="contains($sAffixAlloFeatsSubsumeInflFeatures,'N') and $affixAlloFeats">
			   <xsl:comment>The affix itself had features but they do not match the features of what it attaches to.</xsl:comment>
			   <failure>
				  <wgd:text>The </wgd:text>
				  <wgd:value-of select="$sAffix"/>
				  <wgd:text> affix allomorph '</wgd:text>
				  <wgd:value-of select="$morph/shortName"/>
				  <wgd:text>' is conditioned to only occur when the </wgd:text>
				  <wgd:value-of select="$sAttachesTo"/>
				  <wgd:text> it attaches to has certain features, but the </wgd:text>
				  <wgd:value-of select="$sAttachesTo"/>
				  <wgd:text> does not have them.  The required features the affix must be inflected for are: </wgd:text>
				  <wgd:for-each select="$affixAlloFeats">
					 <wgd:call-template name="OutputFeatureStructureAsText">
						<wgd:with-param name="fs" select="."/>
					 </wgd:call-template>
				  </wgd:for-each>
				  <wgd:text>.  The inflected features for this </wgd:text>
				  <wgd:value-of select="$sAttachesTo"/>
				  <wgd:text> are: </wgd:text>
				  <wgd:for-each select="auto-ns1:node-set($stem)">
					 <wgd:call-template name="OutputFeatureStructureAsText">
						<wgd:with-param name="fs" select="fs"/>
					 </wgd:call-template>
				  </wgd:for-each>
				  <wgd:text>.</wgd:text>
			   </failure>
			</wgd:when>
			<wgd:when test="contains($sAffixAlloFeatsSubsumeInflFeatures,'Y') and $notAffixAlloFeats">
			   <xsl:comment>Other affixes in the entry had features, but this allomorph meets one of them, so it is a failure (the other allomorph should be used).</xsl:comment>
			   <failure>
				  <wgd:text>While the </wgd:text>
				  <wgd:value-of select="$sAffix"/>
				  <wgd:text> affix allomorph '</wgd:text>
				  <wgd:value-of select="$morph/shortName"/>
				  <wgd:text>' is not conditioned to occur when the </wgd:text>
				  <wgd:value-of select="$sAttachesTo"/>
				  <wgd:text> it attaches to has certain features, there are other allomorphs in the entry that are so conditioned.  Thus, the </wgd:text>
				  <wgd:value-of select="$sAttachesTo"/>
				  <wgd:text> must not be inflected for certain features, but it is.  The features the affix must not be inflected for are: </wgd:text>
				  <wgd:for-each select="$notAffixAlloFeats/fs">
					 <wgd:call-template name="OutputFeatureStructureAsText">
						<wgd:with-param name="fs" select="."/>
					 </wgd:call-template>
					 <wgd:if test="position()!=last()">
						<wgd:text> and also </wgd:text>
					 </wgd:if>
				  </wgd:for-each>
				  <wgd:text>.  The inflected features for this </wgd:text>
				  <wgd:value-of select="$sAttachesTo"/>
				  <wgd:text> are: </wgd:text>
				  <wgd:for-each select="auto-ns1:node-set($stem)">
					 <wgd:call-template name="OutputFeatureStructureAsText">
						<wgd:with-param name="fs" select="fs"/>
					 </wgd:call-template>
				  </wgd:for-each>
				  <wgd:text>.</wgd:text>
			   </failure>
			</wgd:when>
		 </wgd:choose>
	  </wgd:template>
   </xsl:template>
   <xsl:template name="UnifyFromFeaturesWithStem">
	  <xsl:comment>Unify from features with stem</xsl:comment>
	  <wgd:variable name="UnificationOfStemAndDerivFrom">
		 <wgd:call-template name="UnifyTwoFeatureStructures">
			<wgd:with-param name="FirstFS" select="fs"/>
			<wgd:with-param name="SecondFS" select="$derivMorph/derivMsa/fromFS"/>
			<wgd:with-param name="sTopLevelId" select="fs/@id"/>
			<wgd:with-param name="sRuleInfo">
			   <wgd:text>Attaching the derivational </wgd:text>
			   <wgd:value-of select="$sType"/>
			   <wgd:text> (</wgd:text>
			   <wgd:value-of select="$derivMorph/shortName"/>
			   <wgd:text>)</wgd:text>
			</wgd:with-param>
			<wgd:with-param name="sFirstDescription" select="'stem'"/>
			<wgd:with-param name="sSecondDescription">
			   <wgd:text>derivational </wgd:text>
			   <wgd:value-of select="$sType"/>
			   <wgd:text> (</wgd:text>
			   <wgd:value-of select="$derivMorph/shortName"/>
			   <wgd:text>)</wgd:text>
			</wgd:with-param>
		 </wgd:call-template>
	  </wgd:variable>
	  <wgd:choose>
		 <wgd:when test="auto-ns1:node-set($UnificationOfStemAndDerivFrom)/descendant::failure">
			<wgd:copy-of select="$UnificationOfStemAndDerivFrom"/>
		 </wgd:when>
		 <wgd:otherwise>
			<xsl:comment>Override those unified features with deriv to features</xsl:comment>
			<wgd:variable name="PriorityUnion">
			   <wgd:call-template name="OverrideFirstFsWithSecondFs">
				  <wgd:with-param name="FirstFS" select="auto-ns1:node-set($UnificationOfStemAndDerivFrom)/fs"/>
				  <wgd:with-param name="SecondFS" select="$derivMorph/derivMsa/toFS"/>
			   </wgd:call-template>
			</wgd:variable>
			<wgd:if test="auto-ns1:node-set($PriorityUnion)/descendant::feature">
			   <wgd:copy-of select="$PriorityUnion"/>
			</wgd:if>
		 </wgd:otherwise>
	  </wgd:choose>
   </xsl:template>
   <xsl:template name="DerivCircumPfxStemDerivCircumSfxProductionNamedTemplate">
	  <xsl:comment>
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 DerivCircumPfxStemDerivCircumSfxProduction
		 Params: none
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  </xsl:comment>
	  <xsl:element name="xsl:template">
		 <xsl:attribute name="name">DerivCircumPfxStemDerivCircumSfxProduction</xsl:attribute>
		 <seq step="Try to build a Stem node by prepending a derivational circumfix-prefix and, at the same time, appending a derivational circumfix-suffix to another Stem node.">
			<xsl:comment>copy what's before the derivational circumfix-prefix</xsl:comment>
			<xsl:element name="xsl:for-each">
			   <xsl:attribute name="select">preceding-sibling::*[position()&gt;1]</xsl:attribute>
			   <xsl:element name="xsl:copy-of">
				  <xsl:attribute name="select">.</xsl:attribute>
			   </xsl:element>
			</xsl:element>
			<xsl:comment>have a new stem</xsl:comment>
			<stem>
			   <xsl:element name="xsl:call-template">
				  <xsl:attribute name="name">ApplyDerivationalConstraints</xsl:attribute>
				  <xsl:element name="xsl:with-param">
					 <xsl:attribute name="name">derivMorph</xsl:attribute>
					 <xsl:attribute name="select">preceding-sibling::morph[1]</xsl:attribute>
				  </xsl:element>
				  <xsl:element name="xsl:with-param">
					 <xsl:attribute name="name">sType</xsl:attribute>
					 <xsl:element name="xsl:text">circumfix</xsl:element>
				  </xsl:element>
			   </xsl:element>
			</stem>
			<xsl:comment> copy what's after the derivational circumfix-suffix </xsl:comment>
			<xsl:element name="xsl:for-each">
			   <xsl:attribute name="select">following-sibling::*[position()&gt;1]</xsl:attribute>
			   <xsl:element name="xsl:copy-of">
				  <xsl:attribute name="select">.</xsl:attribute>
			   </xsl:element>
			</xsl:element>
		 </seq>
	  </xsl:element>
   </xsl:template>
   <xsl:template name="DerivPfxStemProductionNamedTemplate">
	  <xsl:comment>
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 DerivPfxStemProduction
		 Params: none
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  </xsl:comment>
	  <xsl:element name="xsl:template">
		 <xsl:attribute name="name">DerivPfxStemProduction</xsl:attribute>
		 <seq step="Try to build a Stem node by prepending a derivational prefix to another Stem node.">
			<xsl:comment>copy what's before the derivational prefix</xsl:comment>
			<xsl:element name="xsl:for-each">
			   <xsl:attribute name="select">preceding-sibling::*[position()&gt;1]</xsl:attribute>
			   <xsl:element name="xsl:copy-of">
				  <xsl:attribute name="select">.</xsl:attribute>
			   </xsl:element>
			</xsl:element>
			<xsl:comment>have a new stem</xsl:comment>
			<stem>
			   <xsl:element name="xsl:call-template">
				  <xsl:attribute name="name">ApplyDerivationalConstraints</xsl:attribute>
				  <xsl:element name="xsl:with-param">
					 <xsl:attribute name="name">derivMorph</xsl:attribute>
					 <xsl:attribute name="select">preceding-sibling::morph[1]</xsl:attribute>
				  </xsl:element>
				  <xsl:element name="xsl:with-param">
					 <xsl:attribute name="name">sType</xsl:attribute>
					 <xsl:element name="xsl:text">prefix</xsl:element>
				  </xsl:element>
			   </xsl:element>
			</stem>
			<xsl:comment> copy what's after the stem </xsl:comment>
			<xsl:element name="xsl:for-each">
			   <xsl:attribute name="select">following-sibling::*</xsl:attribute>
			   <xsl:element name="xsl:copy-of">
				  <xsl:attribute name="select">.</xsl:attribute>
			   </xsl:element>
			</xsl:element>
		 </seq>
	  </xsl:element>
   </xsl:template>
   <xsl:template name="IndicateInflAffixSucceededNamedTemplate">
	  <xsl:comment>
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 IndicateInflAffixSucceeded
		 Params: none
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  </xsl:comment>
	  <xsl:element name="xsl:template">
		 <xsl:attribute name="name">IndicateInflAffixSucceeded</xsl:attribute>
		 <xsl:comment>Output special code indicating affix succeeded.</xsl:comment>
		 <xsl:element name="xsl:text">
			<xsl:text>x</xsl:text>
		 </xsl:element>
	  </xsl:element>
   </xsl:template>
   <xsl:template name="InflectionalProductionsNamedTemplate">
	  <xsl:comment>
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 InflectionalProductions
		 Params: none
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  </xsl:comment>
	  <xsl:element name="xsl:template">
		 <xsl:attribute name="name">InflectionalProductions</xsl:attribute>
		 <xsl:element name="xsl:if">
			<!-- avoid an impossible sitution as far as the phrase structure rules go -->
			<xsl:attribute name="test">not(preceding-sibling::stem) and not(following-sibling::stem)</xsl:attribute>
			<xsl:element name="xsl:call-template">
			   <xsl:attribute name="name">UninflectedStemProduction</xsl:attribute>
			</xsl:element>
		 </xsl:element>
		 <xsl:for-each select="$affixTemplates">
			<xsl:choose>
			   <xsl:when test="@Final='true'">
				  <!-- for final template, only invoke the template if there are no stems on either side (if there are, it's doomed to failure in the phrase structure rules) -->
				  <xsl:element name="xsl:if">
					 <xsl:attribute name="test">not(preceding-sibling::stem) and not(following-sibling::stem)</xsl:attribute>
					 <xsl:element name="xsl:call-template">
						<xsl:attribute name="name">
						   <xsl:text>InflectionalTemplate</xsl:text>
						   <xsl:value-of select="@Id"/>
						</xsl:attribute>
					 </xsl:element>
				  </xsl:element>
			   </xsl:when>
			   <xsl:otherwise>
				  <xsl:element name="xsl:call-template">
					 <xsl:attribute name="name">
						<xsl:text>InflectionalTemplate</xsl:text>
						<xsl:value-of select="@Id"/>
					 </xsl:attribute>
				  </xsl:element>
			   </xsl:otherwise>
			</xsl:choose>
		 </xsl:for-each>
	  </xsl:element>
   </xsl:template>
   <xsl:template name="ReportFailureNamedTemplate">
	  <xsl:comment>
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
	  </xsl:comment>
	  <xsl:element name="xsl:template">
		 <xsl:attribute name="name">ReportFailure</xsl:attribute>
		 <xsl:element name="xsl:param">
			<xsl:attribute name="name">sPreface</xsl:attribute>
		 </xsl:element>
		 <xsl:element name="xsl:param">
			<xsl:attribute name="name">sFirstItemComponent</xsl:attribute>
		 </xsl:element>
		 <xsl:element name="xsl:param">
			<xsl:attribute name="name">sFirstItemComponentAbbr</xsl:attribute>
		 </xsl:element>
		 <xsl:element name="xsl:param">
			<xsl:attribute name="name">sFirstItemDescription</xsl:attribute>
		 </xsl:element>
		 <xsl:element name="xsl:param">
			<xsl:attribute name="name">sFirstItemValue</xsl:attribute>
		 </xsl:element>
		 <xsl:element name="xsl:param">
			<xsl:attribute name="name">sSecondItemComponent</xsl:attribute>
		 </xsl:element>
		 <xsl:element name="xsl:param">
			<xsl:attribute name="name">sSecondItemComponentAbbr</xsl:attribute>
		 </xsl:element>
		 <xsl:element name="xsl:param">
			<xsl:attribute name="name">sSecondItemDescription</xsl:attribute>
		 </xsl:element>
		 <xsl:element name="xsl:element">
			<xsl:attribute name="name">failure</xsl:attribute>
			<xsl:element name="xsl:value-of">
			   <xsl:attribute name="select">$sPreface</xsl:attribute>
			</xsl:element>
			<xsl:element name="xsl:text">The </xsl:element>
			<xsl:element name="xsl:value-of">
			   <xsl:attribute name="select">$sFirstItemComponent</xsl:attribute>
			</xsl:element>
			<xsl:element name="xsl:text"> (</xsl:element>
			<xsl:element name="xsl:value-of">
			   <xsl:attribute name="select">$sFirstItemComponentAbbr</xsl:attribute>
			</xsl:element>
			<xsl:element name="xsl:text">) of the </xsl:element>
			<xsl:element name="xsl:value-of">
			   <xsl:attribute name="select">$sFirstItemDescription</xsl:attribute>
			</xsl:element>
			<xsl:element name="xsl:text"> "</xsl:element>
			<xsl:element name="xsl:value-of">
			   <xsl:attribute name="select">$sFirstItemValue</xsl:attribute>
			</xsl:element>
			<xsl:element name="xsl:text">" is incompatible with the </xsl:element>
			<xsl:element name="xsl:value-of">
			   <xsl:attribute name="select">$sSecondItemComponent</xsl:attribute>
			</xsl:element>
			<wgd:if test="string-length($sSecondItemComponentAbbr) &gt; 0">
			   <xsl:element name="xsl:text"> (</xsl:element>
			   <xsl:element name="xsl:value-of">
				  <xsl:attribute name="select">$sSecondItemComponentAbbr</xsl:attribute>
			   </xsl:element>
			   <wgd:text>)</wgd:text>
			</wgd:if>
			<xsl:element name="xsl:text"> of the </xsl:element>
			<xsl:element name="xsl:value-of">
			   <xsl:attribute name="select">$sSecondItemDescription</xsl:attribute>
			</xsl:element>
			<xsl:element name="xsl:text">.</xsl:element>
		 </xsl:element>
	  </xsl:element>
   </xsl:template>
   <xsl:template name="ReportCompoundRuleFailureNamedTemplate">
	  <xsl:comment>
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
	  </xsl:comment>
	  <wgd:template name="ReportCompoundRuleFailure">
		 <wgd:param name="sPreface"/>
		 <wgd:param name="sFirstItemComponent"/>
		 <wgd:param name="sFirstItemComponentAbbr"/>
		 <wgd:param name="sFirstItemDescription"/>
		 <wgd:param name="sFirstItemValue"/>
		 <wgd:param name="sSecondItemComponent"/>
		 <wgd:param name="sSecondItemComponentAbbr"/>
		 <wgd:param name="sSecondItemDescription"/>
		 <wgd:element name="failure">
			<content>
			   <wgd:value-of select="$sPreface"/>
			   <wgd:text>The </wgd:text>
			   <wgd:value-of select="$sSecondItemComponent"/>
			   <wgd:if test="string-length($sSecondItemComponentAbbr) &gt; 0">
				  <wgd:text> (</wgd:text>
				  <wgd:value-of select="$sSecondItemComponentAbbr"/>
				  <wgd:text>)</wgd:text>
			   </wgd:if>
			   <wgd:text> of the </wgd:text>
			   <wgd:value-of select="$sSecondItemDescription"/>
			   <wgd:text> is incompatible with the </wgd:text>
			   <wgd:value-of select="$sFirstItemComponent"/>
			   <wgd:text> (</wgd:text>
			   <wgd:value-of select="$sFirstItemComponentAbbr"/>
			   <wgd:text>) of the </wgd:text>
			   <wgd:value-of select="$sFirstItemDescription"/>
			   <wgd:text>:</wgd:text>
			</content>
			<wgd:copy-of select="$sFirstItemValue"/>
		 </wgd:element>
	  </wgd:template>
   </xsl:template>
   <xsl:template name="StemDerivSfxProductionNamedTemplate">
	  <xsl:comment>
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 StemDerivSfxProduction
		 Params: none
		 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  </xsl:comment>
	  <xsl:element name="xsl:template">
		 <xsl:attribute name="name">StemDerivSfxProduction</xsl:attribute>
		 <seq step="Try to build a Stem node by appending a derivational suffix to another Stem node.">
			<xsl:comment>copy what's before the stem</xsl:comment>
			<xsl:element name="xsl:for-each">
			   <xsl:attribute name="select">preceding-sibling::*</xsl:attribute>
			   <xsl:element name="xsl:copy-of">
				  <xsl:attribute name="select">.</xsl:attribute>
			   </xsl:element>
			</xsl:element>
			<xsl:comment>have a new stem</xsl:comment>
			<stem>
			   <xsl:element name="xsl:call-template">
				  <xsl:attribute name="name">ApplyDerivationalConstraints</xsl:attribute>
				  <xsl:element name="xsl:with-param">
					 <xsl:attribute name="name">derivMorph</xsl:attribute>
					 <xsl:attribute name="select">following-sibling::morph[1]</xsl:attribute>
				  </xsl:element>
				  <xsl:element name="xsl:with-param">
					 <xsl:attribute name="name">sType</xsl:attribute>
					 <xsl:element name="xsl:text">suffix</xsl:element>
				  </xsl:element>
			   </xsl:element>
			</stem>
			<xsl:comment> copy what's after the derivational suffix </xsl:comment>
			<xsl:element name="xsl:for-each">
			   <xsl:attribute name="select">following-sibling::*[position()&gt;1]</xsl:attribute>
			   <xsl:element name="xsl:copy-of">
				  <xsl:attribute name="select">.</xsl:attribute>
			   </xsl:element>
			</xsl:element>
		 </seq>
	  </xsl:element>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CallNextAffixSlot
	Invoke NextAffixSlot for either a prefix or a suffix
		Parameters: sType = prefix or suffix
							 bNextMorph = 'y' if it is to check the next morph element
							 iTemplateId = the Id of the template
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="CallNextAffixSlot">
	  <xsl:param name="sType"/>
	  <xsl:param name="bNextMorph"/>
	  <xsl:param name="iTemplateId"/>
	  <xsl:choose>
		 <xsl:when test="$sType='prefix'">
			<xsl:call-template name="NextAffixSlot">
			   <xsl:with-param name="sMorph">
				  <xsl:choose>
					 <xsl:when test="$bNextMorph='y'">
						<xsl:text>$morph/preceding-sibling::morph[1]</xsl:text>
					 </xsl:when>
					 <xsl:otherwise>
						<xsl:text>$morph</xsl:text>
					 </xsl:otherwise>
				  </xsl:choose>
			   </xsl:with-param>
			   <xsl:with-param name="sType">PrefixSlot</xsl:with-param>
			   <xsl:with-param name="iTemplateId">
				  <xsl:value-of select="$iTemplateId"/>
			   </xsl:with-param>
			   <xsl:with-param name="NextSlot" select="preceding-sibling::PrefixSlots[1]"/>
			</xsl:call-template>
		 </xsl:when>
		 <xsl:otherwise>
			<xsl:call-template name="NextAffixSlot">
			   <xsl:with-param name="sMorph">
				  <xsl:choose>
					 <xsl:when test="$bNextMorph='y'">
						<xsl:text>$morph/following-sibling::morph[1]</xsl:text>
					 </xsl:when>
					 <xsl:otherwise>
						<xsl:text>$morph</xsl:text>
					 </xsl:otherwise>
				  </xsl:choose>
			   </xsl:with-param>
			   <xsl:with-param name="sType">SuffixSlot</xsl:with-param>
			   <xsl:with-param name="iTemplateId">
				  <xsl:value-of select="$iTemplateId"/>
			   </xsl:with-param>
			   <xsl:with-param name="NextSlot" select="following-sibling::SuffixSlots[1]"/>
			</xsl:call-template>
		 </xsl:otherwise>
	  </xsl:choose>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CheckInflAffixCompatibility
	Check the compatibility of an inflectional affix; if OK, go check next affix slot
		Parameters: sType = prefix or suffix
							 iTemplateId = the Id of the template
							 sSlotType = 'required' or 'optional'
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="CheckInflAffixCompatibility">
	  <xsl:param name="sType"/>
	  <xsl:param name="iTemplateId"/>
	  <xsl:param name="sSlotType">required</xsl:param>
	  <xsl:comment>Check inflection class compatibility</xsl:comment>
	  <xsl:element name="xsl:variable">
		 <xsl:attribute name="name">sStemInflClass</xsl:attribute>
		 <xsl:element name="xsl:value-of">
			<xsl:attribute name="select">@inflClass</xsl:attribute>
		 </xsl:element>
	  </xsl:element>
	  <xsl:element name="xsl:choose">
		 <xsl:element name="xsl:when">
			<xsl:attribute name="test">$morph/inflClasses</xsl:attribute>
			<xsl:element name="xsl:choose">
			   <xsl:element name="xsl:when">
				  <xsl:attribute name="test">$morph/inflClasses[inflClass/@hvo=$sStemInflClass] or $sStemInflClass=''</xsl:attribute>
				  <xsl:element name="xsl:call-template">
					 <xsl:attribute name="name">IndicateInflAffixSucceeded</xsl:attribute>
				  </xsl:element>
				  <xsl:call-template name="CallNextAffixSlot">
					 <xsl:with-param name="sType">
						<xsl:value-of select="$sType"/>
					 </xsl:with-param>
					 <xsl:with-param name="bNextMorph">y</xsl:with-param>
					 <xsl:with-param name="iTemplateId">
						<xsl:value-of select="$iTemplateId"/>
					 </xsl:with-param>
				  </xsl:call-template>
			   </xsl:element>
			   <xsl:element name="xsl:otherwise">
				  <!-- The inflection class does not match: give failure notice -->
				  <xsl:call-template name="ReportIncompatibleInflectionClassInTemplate">
					 <xsl:with-param name="sType">
						<xsl:value-of select="$sType"/>
					 </xsl:with-param>
					 <xsl:with-param name="sSlotType">
						<xsl:value-of select="$sSlotType"/>
					 </xsl:with-param>
				  </xsl:call-template>
			   </xsl:element>
			</xsl:element>
		 </xsl:element>
		 <xsl:element name="xsl:otherwise">
			<xsl:comment>No inflection classes to check; indicate success and try next slot (if any)</xsl:comment>
			<xsl:element name="xsl:call-template">
			   <xsl:attribute name="name">IndicateInflAffixSucceeded</xsl:attribute>
			</xsl:element>
			<xsl:call-template name="CallNextAffixSlot">
			   <xsl:with-param name="sType">
				  <xsl:value-of select="$sType"/>
			   </xsl:with-param>
			   <xsl:with-param name="bNextMorph">y</xsl:with-param>
			   <xsl:with-param name="iTemplateId">
				  <xsl:value-of select="$iTemplateId"/>
			   </xsl:with-param>
			</xsl:call-template>
		 </xsl:element>
	  </xsl:element>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CreateCompoundRuleAttribute
	Create an attribute for the lefth hand stem of an endocentric compound rule
		Parameters: member = member which is the head
							sAttr - the attribute to create
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="CreateCompoundRuleAttribute">
	  <xsl:param name="Member"/>
	  <xsl:param name="sAttr"/>
	  <xsl:element name="xsl:attribute">
		 <xsl:attribute name="name">
			<xsl:value-of select="$sAttr"/>
		 </xsl:attribute>
		 <xsl:element name="xsl:value-of">
			<xsl:attribute name="select">
			   <xsl:value-of select="$Member"/>
			   <xsl:text>/@</xsl:text>
			   <xsl:value-of select="$sAttr"/>
			</xsl:attribute>
		 </xsl:element>
	  </xsl:element>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CreateInflectionalTemplateSlotTemplate
	Create an attribute for the lefth hand stem of an endocentric compound rule
		Parameters: member = member which is the head
							sAttr - the attribute to create
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="CreateInflectionalTemplateSlotTemplate">
	  <xsl:param name="sType"/>
	  <xsl:param name="iTemplateId"/>
	  <xsl:variable name="sSlotType">
		 <xsl:choose>
			<xsl:when test="$sType='prefix'">PrefixSlot</xsl:when>
			<xsl:otherwise>SuffixSlot</xsl:otherwise>
		 </xsl:choose>
	  </xsl:variable>
	  <xsl:comment>
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		 <xsl:call-template name="CreateInflectionalTemplateWithSlotName">
			<xsl:with-param name="sTemplateID" select="$iTemplateId"/>
			<xsl:with-param name="sSlotType" select="$sSlotType"/>
			<xsl:with-param name="thisSlot" select="."/>
		 </xsl:call-template>
	Params: morph - morpheme to check
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  </xsl:comment>
	  <xsl:element name="xsl:template">
		 <xsl:attribute name="name">
			<xsl:call-template name="CreateInflectionalTemplateWithSlotName">
			   <xsl:with-param name="sTemplateID" select="$iTemplateId"/>
			   <xsl:with-param name="sSlotType" select="$sSlotType"/>
			   <xsl:with-param name="thisSlot" select="."/>
			</xsl:call-template>
		 </xsl:attribute>
		 <xsl:element name="xsl:param">
			<xsl:attribute name="name">morph</xsl:attribute>
		 </xsl:element>
		 <xsl:choose>
			<xsl:when test="key('SlotID',@dst)/@Optional='false'">
			   <!-- slot is required -->
			   <xsl:element name="xsl:choose">
				  <xsl:element name="xsl:when">
					 <xsl:attribute name="test">$morph/@wordType = '<xsl:value-of select="@dst"/>'</xsl:attribute>
					 <xsl:comment>Required slot matches: if it is compatible, try next slot (if any) with next morph</xsl:comment>
					 <xsl:call-template name="CheckInflAffixCompatibility">
						<xsl:with-param name="sType">
						   <xsl:value-of select="$sType"/>
						</xsl:with-param>
						<xsl:with-param name="iTemplateId">
						   <xsl:value-of select="$iTemplateId"/>
						</xsl:with-param>
						<xsl:with-param name="sSlotType">required </xsl:with-param>
					 </xsl:call-template>
				  </xsl:element>
				  <xsl:element name="xsl:otherwise">
					 <xsl:comment>Required slot does not match: give failure notice</xsl:comment>
					 <failure>
						<xsl:call-template name="ReportInflectionalTemplateName">
						   <xsl:with-param name="template" select=".."/>
						</xsl:call-template>
						<xsl:text> failed because the required </xsl:text>
						<xsl:value-of select="$sType"/>
						<xsl:text> slot '</xsl:text>
						<xsl:value-of select="key('SlotID',@dst)/Name"/>
						<xsl:text>' was not found.</xsl:text>
					 </failure>
				  </xsl:element>
			   </xsl:element>
			</xsl:when>
			<xsl:otherwise>
			   <!-- slot is optional -->
			   <xsl:element name="xsl:choose">
				  <xsl:element name="xsl:when">
					 <xsl:attribute name="test">$morph/@wordType = '<xsl:value-of select="@dst"/>'</xsl:attribute>
					 <xsl:comment>Optional slot matches: if it is compatible, try next slot (if any) with next morph</xsl:comment>
					 <xsl:call-template name="CheckInflAffixCompatibility">
						<xsl:with-param name="sType">
						   <xsl:value-of select="$sType"/>
						</xsl:with-param>
						<xsl:with-param name="iTemplateId">
						   <xsl:value-of select="$iTemplateId"/>
						</xsl:with-param>
						<xsl:with-param name="sSlotType">optional </xsl:with-param>
					 </xsl:call-template>
				  </xsl:element>
				  <xsl:element name="xsl:otherwise">
					 <xsl:comment>Optional slot does not match: try next slot (if any) with this morph</xsl:comment>
					 <wgd:if test="$morph">
						<xsl:call-template name="CallNextAffixSlot">
						   <xsl:with-param name="sType">
							  <xsl:value-of select="$sType"/>
						   </xsl:with-param>
						   <xsl:with-param name="bNextMorph">n</xsl:with-param>
						   <xsl:with-param name="iTemplateId">
							  <xsl:value-of select="$iTemplateId"/>
						   </xsl:with-param>
						</xsl:call-template>
					 </wgd:if>
				  </xsl:element>
			   </xsl:element>
			</xsl:otherwise>
		 </xsl:choose>
	  </xsl:element>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CreateInflectionalTemplateInnards
	Create code for handling an inflectional template.
		Parameters: sTemplate = template id
						 pos = Part of Speech
						 bPartial = Flag for whether the infl template is partial (or main)
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="CreateInflectionalTemplateInnards">
	  <xsl:param name="template"/>
	  <xsl:param name="pos"/>
	  <xsl:param name="bPartial"/>
	  <xsl:element name="xsl:variable">
		 <xsl:attribute name="name">PrefixSlotMorphs</xsl:attribute>
		 <xsl:if test="$template/PrefixSlots">
			<xsl:element name="xsl:call-template">
			   <xsl:attribute name="name">
				  <xsl:call-template name="CreateInflectionalTemplateWithSlotName">
					 <xsl:with-param name="sTemplateID" select="$template/@Id"/>
					 <xsl:with-param name="sSlotType" select="'PrefixSlot'"/>
					 <xsl:with-param name="thisSlot" select="$template/PrefixSlots[last()]"/>
				  </xsl:call-template>
			   </xsl:attribute>
			   <xsl:element name="xsl:with-param">
				  <xsl:attribute name="name">morph</xsl:attribute>
				  <xsl:attribute name="select">preceding-sibling::*[1]</xsl:attribute>
			   </xsl:element>
			</xsl:element>
		 </xsl:if>
	  </xsl:element>
	  <xsl:element name="xsl:variable">
		 <xsl:attribute name="name">iPrefixSlotMorphs</xsl:attribute>
		 <xsl:choose>
			<xsl:when test="PrefixSlots">
			   <xsl:element name="xsl:choose">
				  <xsl:element name="xsl:when">
					 <xsl:attribute name="test">not(contains($PrefixSlotMorphs, ' '))</xsl:attribute>
					 <xsl:element name="xsl:value-of">
						<xsl:attribute name="select">string-length($PrefixSlotMorphs)</xsl:attribute>
					 </xsl:element>
				  </xsl:element>
				  <xsl:element name="xsl:otherwise">0</xsl:element>
			   </xsl:element>
			</xsl:when>
			<xsl:otherwise>0</xsl:otherwise>
		 </xsl:choose>
	  </xsl:element>
	  <xsl:element name="xsl:variable">
		 <xsl:attribute name="name">SuffixSlotMorphs</xsl:attribute>
		 <xsl:if test="$template/SuffixSlots">
			<xsl:element name="xsl:call-template">
			   <xsl:attribute name="name">
				  <xsl:call-template name="CreateInflectionalTemplateWithSlotName">
					 <xsl:with-param name="sTemplateID" select="$template/@Id"/>
					 <xsl:with-param name="sSlotType" select="'SuffixSlot'"/>
					 <xsl:with-param name="thisSlot" select="$template/SuffixSlots[1]"/>
				  </xsl:call-template>
			   </xsl:attribute>
			   <xsl:element name="xsl:with-param">
				  <xsl:attribute name="name">morph</xsl:attribute>
				  <xsl:attribute name="select">following-sibling::*[1]</xsl:attribute>
			   </xsl:element>
			</xsl:element>
		 </xsl:if>
	  </xsl:element>
	  <xsl:element name="xsl:variable">
		 <xsl:attribute name="name">iSuffixSlotMorphs</xsl:attribute>
		 <xsl:choose>
			<xsl:when test="$template/SuffixSlots">
			   <xsl:element name="xsl:choose">
				  <xsl:element name="xsl:when">
					 <xsl:attribute name="test">not(contains($SuffixSlotMorphs, ' '))</xsl:attribute>
					 <xsl:element name="xsl:value-of">
						<xsl:attribute name="select">string-length($SuffixSlotMorphs)</xsl:attribute>
					 </xsl:element>
				  </xsl:element>
				  <xsl:element name="xsl:otherwise">0</xsl:element>
			   </xsl:element>
			</xsl:when>
			<xsl:otherwise>0</xsl:otherwise>
		 </xsl:choose>
	  </xsl:element>
	  <xsl:comment> copy what's before the template </xsl:comment>
	  <xsl:element name="xsl:for-each">
		 <xsl:attribute name="select">preceding-sibling::*[position()&gt;$iPrefixSlotMorphs]</xsl:attribute>
		 <xsl:element name="xsl:copy-of">
			<xsl:attribute name="select">.</xsl:attribute>
		 </xsl:element>
	  </xsl:element>
	  <xsl:choose>
		 <xsl:when test="$bPartial='Y'">
			<partial>
			   <xsl:call-template name="ProcessInflectionalTemplatePercolationAndConstraints">
				  <xsl:with-param name="template" select="$template"/>
				  <xsl:with-param name="pos" select="$pos"/>
				  <xsl:with-param name="bPartial" select="$bPartial"/>
			   </xsl:call-template>
			</partial>
		 </xsl:when>
		 <xsl:otherwise>
			<xsl:call-template name="CreateMainInflectionTemplateCore">
			   <xsl:with-param name="template" select="$template"/>
			   <xsl:with-param name="pos" select="$pos"/>
			   <xsl:with-param name="bPartial">
				  <xsl:text>N</xsl:text>
			   </xsl:with-param>
			</xsl:call-template>
		 </xsl:otherwise>
	  </xsl:choose>
	  <xsl:comment> copy what's after the template </xsl:comment>
	  <xsl:element name="xsl:for-each">
		 <xsl:attribute name="select">following-sibling::*[position()&gt;$iSuffixSlotMorphs]</xsl:attribute>
		 <xsl:element name="xsl:copy-of">
			<xsl:attribute name="select">.</xsl:attribute>
		 </xsl:element>
	  </xsl:element>
   </xsl:template>
   <!--
	  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  CreateInflectionalTemplateWithSlotName
	  Create the name for a template-with-slot
	  Parameters: sTemplateID = template id
	  thisSlot = the current slot being processed
	  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
   -->
   <xsl:template name="CreateInflectionalTemplateWithSlotName">
	  <xsl:param name="sTemplateID"/>
	  <xsl:param name="sSlotType"/>
	  <xsl:param name="thisSlot"/>
	  <xsl:variable name="sThisSlotDst" select="$thisSlot/@dst"/>
	  <xsl:text>InflectionalTemplate</xsl:text>
	  <xsl:value-of select="$sTemplateID"/>
	  <xsl:value-of select="$sSlotType"/>
	  <xsl:value-of select="$sThisSlotDst"/>
	  <xsl:text>-</xsl:text>
	  <xsl:choose>
		 <xsl:when test="$sSlotType='SuffixSlot'">
			<xsl:value-of select="count($thisSlot/preceding-sibling::SuffixSlots[@dst=$sThisSlotDst])"/>
		 </xsl:when>
		 <xsl:otherwise>
			<xsl:value-of select="count($thisSlot/following-sibling::PrefixSlots[@dst=$sThisSlotDst])"/>
		 </xsl:otherwise>
	  </xsl:choose>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CreateInvocationsOfPartialInflectionalTemplate
	Create an invocation of a partial inflectional template.  Is recursive on pos.
		Parameters: sTemplate = template id
						 pos = Part of Speech
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="CreateInvocationsOfPartialInflectionalTemplate">
	  <xsl:param name="sTemplate"/>
	  <xsl:param name="pos"/>
	  <xsl:element name="xsl:call-template">
		 <xsl:attribute name="name">
			<xsl:text>PartialInflectionalTemplate</xsl:text>
			<xsl:value-of select="$sTemplate"/>
			<xsl:text>POS</xsl:text>
			<xsl:value-of select="$pos/@Id"/>
		 </xsl:attribute>
	  </xsl:element>
	  <xsl:variable name="subPOSes" select="key('POSID',$pos/@Id)/SubPossibilities"/>
	  <xsl:if test="$subPOSes">
		 <xsl:element name="xsl:if">
			<xsl:attribute name="test">
			   <xsl:text>@syncat!='0'</xsl:text>
			</xsl:attribute>
			<xsl:for-each select="$subPOSes">
			   <xsl:call-template name="CreateInvocationsOfPartialInflectionalTemplate">
				  <xsl:with-param name="sTemplate">
					 <xsl:value-of select="$sTemplate"/>
				  </xsl:with-param>
				  <xsl:with-param name="pos" select="key('POSID',@dst)"/>
			   </xsl:call-template>
			</xsl:for-each>
		 </xsl:element>
	  </xsl:if>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CreateMainInflectionTemplateCore
	Create code for handling an inflectional template.
		Parameters: sTemplate = template id
						 pos = Part of Speech
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="CreateMainInflectionTemplateCore">
	  <xsl:param name="template"/>
	  <xsl:param name="pos"/>
	  <xsl:choose>
		 <xsl:when test="$template/@Final='true'">
			<xsl:comment>have a full</xsl:comment>
			<full>
			   <xsl:call-template name="ProcessInflectionalTemplatePercolationAndConstraints">
				  <xsl:with-param name="template" select="$template"/>
				  <xsl:with-param name="pos" select="$pos"/>
			   </xsl:call-template>
			</full>
		 </xsl:when>
		 <xsl:otherwise>
			<xsl:comment>have a stem</xsl:comment>
			<stem>
			   <xsl:call-template name="ProcessInflectionalTemplatePercolationAndConstraints">
				  <xsl:with-param name="template" select="$template"/>
				  <xsl:with-param name="pos" select="$pos"/>
			   </xsl:call-template>
			   <wgd:copy-of select="productivityRestriction"/>
			</stem>
		 </xsl:otherwise>
	  </xsl:choose>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CreatePartialInflectionalTemplate
	Create code for handling a partial inflectional template.  Is recursive on POS
		Parameters: sTemplate = template id
						 pos = Part of Speech
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="CreatePartialInflectionalTemplate">
	  <xsl:param name="template"/>
	  <xsl:param name="pos"/>
	  <xsl:variable name="sPartialTemplateName">PartialInflectionalTemplate<xsl:value-of select="$template/@Id"/>POS<xsl:value-of select="$pos/@Id"/>
	  </xsl:variable>
	  <xsl:comment>
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
<xsl:value-of select="$sPartialTemplateName"/>
	Params: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	  </xsl:comment>
	  <xsl:element name="xsl:template">
		 <xsl:attribute name="name">
			<xsl:value-of select="$sPartialTemplateName"/>
		 </xsl:attribute>
		 <seq>
			<xsl:attribute name="step">
			   <xsl:text>Try to apply inflectional template '</xsl:text>
			   <xsl:value-of select="normalize-space($template/Name)"/>
			   <xsl:text>' on a Partial analysis node.  Apply it for category '</xsl:text>
			   <xsl:value-of select="normalize-space($pos/Name)"/>
			   <xsl:text>'. The result will be another Partial analysis node.</xsl:text>
			</xsl:attribute>
			<xsl:call-template name="CreateInflectionalTemplateInnards">
			   <xsl:with-param name="template" select="$template"/>
			   <xsl:with-param name="pos" select="$pos"/>
			   <xsl:with-param name="bPartial" select="'Y'"/>
			</xsl:call-template>
		 </seq>
	  </xsl:element>
	  <xsl:for-each select="key('POSID',$pos/@Id)/SubPossibilities">
		 <xsl:call-template name="CreatePartialInflectionalTemplate">
			<xsl:with-param name="template" select="$template"/>
			<xsl:with-param name="pos" select="key('POSID',@dst)"/>
		 </xsl:call-template>
	  </xsl:for-each>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CreateStemNameCheckForSuperPOS
	Check any owning POS
		Parameters: stemName= stem name id
						 InflFeatures = inflectional features to check against
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="CreateStemNameCheckForSuperPOS">
	  <xsl:param name="stemName"/>
	  <xsl:param name="InflFeatures"/>
	  <xsl:variable name="pos" select="@Id"/>
	  <xsl:variable name="superPOSes" select="$partsOfSpeech/SubPossibilities[@dst=$pos]"/>
	  <xsl:choose>
		 <xsl:when test="$superPOSes">
			<xsl:for-each select="$superPOSes">
			   <wgd:call-template>
				  <xsl:attribute name="name">
					 <xsl:text>CheckStemNames</xsl:text>
					 <xsl:value-of select="../@Id"/>
				  </xsl:attribute>
				  <wgd:with-param name="stemName" select="$stemName"/>
				  <wgd:with-param name="InflFeatures" select="$InflFeatures"/>
			   </wgd:call-template>
			</xsl:for-each>
		 </xsl:when>
	  </xsl:choose>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
NestedPOSids
	Perform a recursive pass through nested PartOfSpeech elements
		Parameters: pos = key for current PartOfSpeech
						 sType = flag for whether it's for
						'Full'	a Full production
						'Stem_1'	a Full production, but for a non-final inflectional template
						'Partial'	a Partial production
						'Label'	PartOfSpeech label only (as of 2003.08.13, no longer used)
						'DerivAffix'	DerivationalAffix
						'CompoundStem2' Left member of a compound
						'CompoundStem3' Right member of a compound
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <!-- recursive pass through POS -->
   <xsl:template name="NestedPOSids">
	  <xsl:param name="pos" select="."/>
	  <xsl:param name="sNested" select="n"/>
	  <xsl:for-each select="key('POSID',$pos)/SubPossibilities">
		 <xsl:if test="position() != '1' or $sNested='y'">
			<xsl:text> or </xsl:text>
		 </xsl:if>
		 <xsl:text>$sSecondCat = '</xsl:text>
		 <xsl:value-of select="@dst"/>
		 <xsl:text>'</xsl:text>
		 <xsl:call-template name="NestedPOSids">
			<xsl:with-param name="pos" select="@dst"/>
			<xsl:with-param name="sNested">
			   <xsl:text>y</xsl:text>
			</xsl:with-param>
		 </xsl:call-template>
	  </xsl:for-each>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
NextAffixSlot
	Try the next affix slot to see if it matches the given morph
		Parameters: sMorph = string descrbing morph to use
			sType = 'PrefixSlot' for prefix slots, 'SuffixSlot' for suffix slots
			iTemplateId = the Id attr of the template
			NextSlot = the next slot to consider (if any)
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="NextAffixSlot">
	  <xsl:param name="sMorph"/>
	  <xsl:param name="sType"/>
	  <xsl:param name="iTemplateId"/>
	  <xsl:param name="NextSlot"/>
	  <xsl:if test="$NextSlot">
		 <xsl:element name="xsl:call-template">
			<xsl:attribute name="name">
			   <xsl:call-template name="CreateInflectionalTemplateWithSlotName">
				  <xsl:with-param name="sTemplateID" select="$iTemplateId"/>
				  <xsl:with-param name="sSlotType" select="$sType"/>
				  <xsl:with-param name="thisSlot" select="$NextSlot"/>
			   </xsl:call-template>
			</xsl:attribute>
			<xsl:element name="xsl:with-param">
			   <xsl:attribute name="name">morph</xsl:attribute>
			   <xsl:attribute name="select">
				  <xsl:value-of select="$sMorph"/>
			   </xsl:attribute>
			</xsl:element>
		 </xsl:element>
	  </xsl:if>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputElsewhereChecksForStemNamesUsedInLexicalEntries
	Output logical constraint statements for "not" stem names used in lexical entries
		Parameters: sList = list of strings to look at
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="OutputElsewhereChecksForStemNamesUsedInLexicalEntries">
	  <xsl:param name="sList"/>
	  <xsl:param name="pos"/>
	  <xsl:variable name="sFirst" select="substring-before($sList,' ')"/>
	  <xsl:variable name="sRest" select="substring-after($sList,' ')"/>
	  <xsl:if test="string-length($sFirst) &gt; 0">
		 <wgd:when>
			<xsl:attribute name="test">
			   <xsl:text>$stemName='</xsl:text>
			   <xsl:value-of select="$sFirst"/>
			   <xsl:text>'</xsl:text>
			</xsl:attribute>
			<wgd:variable name="sStemNameNotSubsumesRegionFs">
			   <xsl:call-template name="OutputMultipleStemNameRegionsSubsumptionChecks">
				  <xsl:with-param name="sStemNames" select="$sFirst"/>
				  <xsl:with-param name="pos" select="$pos"/>
			   </xsl:call-template>
			</wgd:variable>
			<wgd:if test="contains($sStemNameNotSubsumesRegionFs,'N')">
			   <failure>
				  <xsl:text>While this stem allomorph does not belong to any Stem Names, another stem allomorph in this entry belongs to Stem Name '</xsl:text>
				  <xsl:call-template name="OutputMultipleStemNameNames">
					 <xsl:with-param name="sStemNames" select="$sFirst"/>
				  </xsl:call-template>
				  <xsl:text>.'  Thus, the word must not be inflected for certain features, but it is.  The </xsl:text>
				  <xsl:choose>
					 <xsl:when test="contains(substring-after($sFirst,'StemName'),'StemName') or count(key('StemNameID',substring-after($sFirst,'NotStemName'))/Regions/FsFeatStruc) &gt; 1">
						<xsl:text>possible feature sets it must not be inflected for are: </xsl:text>
					 </xsl:when>
					 <xsl:otherwise>
						<xsl:text>feature set it must not be inflected for is: </xsl:text>
					 </xsl:otherwise>
				  </xsl:choose>
				  <xsl:call-template name="OutputMultipleStemNameRegionsFeatureStructures">
					 <xsl:with-param name="sStemNames" select="$sFirst"/>
				  </xsl:call-template>
				  <xsl:text>.  The inflected features for this word are: </xsl:text>
				  <wgd:for-each select="auto-ns1:node-set($InflFeatures)">
					 <wgd:call-template name="OutputFeatureStructureAsText">
						<wgd:with-param name="fs" select="fs"/>
					 </wgd:call-template>
				  </wgd:for-each>
			   </failure>
			</wgd:if>
		 </wgd:when>
		 <xsl:if test="$sRest">
			<xsl:call-template name="OutputElsewhereChecksForStemNamesUsedInLexicalEntries">
			   <xsl:with-param name="sList" select="$sRest"/>
			   <xsl:with-param name="pos" select="$pos"/>
			</xsl:call-template>
		 </xsl:if>
	  </xsl:if>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputFsFeatStrucAsText
	Routine to recursively output an FsFeatStruc as text
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="OutputFsFeatStrucAsText">
	  <xsl:text>[</xsl:text>
	  <xsl:for-each select="FsComplexValue">
		 <xsl:value-of select="key('FeatureID',@Feature)/Abbreviation"/>
		 <xsl:text>:</xsl:text>
		 <xsl:for-each select="FsFeatStruc">
			<xsl:call-template name="OutputFsFeatStrucAsText"/>
		 </xsl:for-each>
	  </xsl:for-each>
	  <xsl:for-each select="FsClosedValue">
		 <xsl:if test="position()!=1">
			<xsl:text disable-output-escaping="yes">&#x20;</xsl:text>
		 </xsl:if>
		 <xsl:value-of select="key('FeatureID',@Feature)/Abbreviation"/>
		 <xsl:text>:</xsl:text>
		 <xsl:value-of select="key('ValueID',@Value)/Abbreviation"/>
	  </xsl:for-each>
	  <xsl:text>]</xsl:text>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputFsFeatStrucAsXml
	Routine to recursively output an FsFeatStruc as xml
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="OutputFsFeatStrucAsXml">
	  <fs id="{@Id}">
		 <xsl:for-each select="FsComplexValue">
			<feature>
			   <name>
				  <xsl:value-of select="key('FeatureID',@Feature)/Abbreviation"/>
			   </name>
			   <value>
				  <xsl:for-each select="FsFeatStruc">
					 <xsl:call-template name="OutputFsFeatStrucAsXml"/>
				  </xsl:for-each>
			   </value>
			</feature>
		 </xsl:for-each>
		 <xsl:for-each select="FsClosedValue">
			<feature>
			   <name>
				  <xsl:value-of select="key('FeatureID',@Feature)/Abbreviation"/>
			   </name>
			   <value>
				  <xsl:value-of select="key('ValueID',@Value)/Abbreviation"/>
			   </value>
			</feature>
		 </xsl:for-each>
	  </fs>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputMultipleStemNameNames
	Output all stem names in list
		Parameters: sStemNames = the list of stem names
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="OutputMultipleStemNameNames">
	  <xsl:param name="sStemNames"/>
	  <xsl:variable name="sn1" select="substring-after($sStemNames, 'StemName')"/>
	  <xsl:variable name="sn2" select="substring-before($sn1,'StemName')"/>
	  <xsl:variable name="sn">
		 <xsl:choose>
			<xsl:when test="string-length($sn2) &gt; 0">
			   <xsl:value-of select="$sn2"/>
			</xsl:when>
			<xsl:otherwise>
			   <xsl:value-of select="$sn1"/>
			</xsl:otherwise>
		 </xsl:choose>
	  </xsl:variable>
	  <xsl:for-each select="key('StemNameID',$sn)">
		 <xsl:value-of select="Name"/>
	  </xsl:for-each>
	  <xsl:if test="string-length($sn2) &gt; 0">
		 <xsl:text>' or '</xsl:text>
		 <xsl:call-template name="OutputMultipleStemNameNames">
			<xsl:with-param name="sStemNames" select="substring-after($sStemNames, $sn2)"/>
		 </xsl:call-template>
	  </xsl:if>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputMultipleStemNameRegionsFeatureStructures
	Output all stem names in list
		Parameters: sStemNames = the list of stem names
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="OutputMultipleStemNameRegionsFeatureStructures">
	  <xsl:param name="sStemNames"/>
	  <xsl:variable name="sn1" select="substring-after($sStemNames, 'StemName')"/>
	  <xsl:variable name="sn2" select="substring-before($sn1,'StemName')"/>
	  <xsl:variable name="sn">
		 <xsl:choose>
			<xsl:when test="string-length($sn2) &gt; 0">
			   <xsl:value-of select="$sn2"/>
			</xsl:when>
			<xsl:otherwise>
			   <xsl:value-of select="$sn1"/>
			</xsl:otherwise>
		 </xsl:choose>
	  </xsl:variable>
	  <xsl:for-each select="key('StemNameID',$sn)">
		 <xsl:for-each select="Regions/FsFeatStruc[descendant::FsClosedValue]">
			<xsl:if test="position()!=1">
			   <xsl:text> or </xsl:text>
			</xsl:if>
			<xsl:call-template name="OutputFsFeatStrucAsText"/>
		 </xsl:for-each>
	  </xsl:for-each>
	  <xsl:if test="string-length($sn2) &gt; 0">
		 <xsl:text> or </xsl:text>
		 <xsl:call-template name="OutputMultipleStemNameRegionsFeatureStructures">
			<xsl:with-param name="sStemNames" select="substring-after($sStemNames, $sn2)"/>
		 </xsl:call-template>
	  </xsl:if>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
OutputMultipleStemNameRegionsSubsumptionChecks
	Output feature structures based on a list of stem names
		Parameters: sStemNames = the list of stem names in the form StemNameXStemNameY...StemNameZ
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="OutputMultipleStemNameRegionsSubsumptionChecks">
	  <xsl:param name="sStemNames"/>
	  <xsl:param name="pos"/>
	  <xsl:variable name="sn1" select="substring-after($sStemNames, 'StemName')"/>
	  <xsl:variable name="sn2" select="substring-before($sn1,'StemName')"/>
	  <xsl:variable name="sn">
		 <xsl:choose>
			<xsl:when test="string-length($sn2) &gt; 0">
			   <xsl:value-of select="$sn2"/>
			</xsl:when>
			<xsl:otherwise>
			   <xsl:value-of select="$sn1"/>
			</xsl:otherwise>
		 </xsl:choose>
	  </xsl:variable>
	  <xsl:for-each select="key('StemNameID',$sn)">
		 <xsl:variable name="bStemNameRelevantToPOS">
			<xsl:call-template name="StemNameRelevantToPOS">
			   <xsl:with-param name="stemname" select="."/>
			   <xsl:with-param name="pos" select="$pos"/>
			</xsl:call-template>
		 </xsl:variable>
		 <xsl:if test="contains($bStemNameRelevantToPOS,'Y')">
			<xsl:variable name="regions" select="Regions/FsFeatStruc[descendant::FsClosedValue]"/>
			<xsl:for-each select="$regions">
			   <xsl:variable name="regionFsWithId">
				  <xsl:text>regionFs</xsl:text>
				  <xsl:value-of select="@Id"/>
			   </xsl:variable>
			   <wgd:variable name="{$regionFsWithId}">
				  <xsl:call-template name="OutputFsFeatStrucAsXml"/>
			   </wgd:variable>
			   <wgd:variable name="{$regionFsWithId}SubsumptionResult">
				  <wgd:call-template name="XSubsumesY">
					 <wgd:with-param name="X" select="auto-ns1:node-set(${$regionFsWithId})/fs"/>
					 <wgd:with-param name="Y" select="auto-ns1:node-set($InflFeatures)/fs"/>
				  </wgd:call-template>
			   </wgd:variable>
			   <wgd:if test="contains(${$regionFsWithId}SubsumptionResult,'Y')">
				  <xsl:comment>if this fs does subsume the infl features, then we have a failure.  Indicate this by outputting an N</xsl:comment>
				  <wgd:text>N</wgd:text>
			   </wgd:if>
			</xsl:for-each>
		 </xsl:if>
		 <xsl:if test="string-length($sn2) &gt; 0">
			<xsl:call-template name="OutputMultipleStemNameRegionsSubsumptionChecks">
			   <xsl:with-param name="sStemNames" select="substring-after($sStemNames, $sn2)"/>
			   <xsl:with-param name="pos" select="$pos"/>
			</xsl:call-template>
		 </xsl:if>
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
	  <xsl:variable name="sNewList" select="concat(normalize-space($sList),' ')"/>
	  <xsl:variable name="sFirst" select="substring-before($sNewList,' ')"/>
	  <xsl:variable name="sRest" select="substring-after($sNewList,' ')"/>
	  <xsl:if test="string-length($sFirst) &gt; 0 and not(contains($sRest,concat($sFirst,' ')))">
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
PercolateHeadedCompoundInfo
	Pss up attributes of a headed compound
		Parameters: member = member which is the head
							  override = overriding msa (if any)
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="PercolateHeadedCompoundInfo">
	  <xsl:param name="Member"/>
	  <xsl:param name="Override"/>
	  <xsl:choose>
		 <xsl:when test="$Override and $Override/@PartOfSpeech!=0">
			<wgd:attribute name="syncat">
			   <xsl:value-of select="$Override/@PartOfSpeech"/>
			</wgd:attribute>
			<wgd:attribute name="syncatAbbr">
			   <xsl:value-of select="key('POSID',$Override/@PartOfSpeech)/Abbreviation"/>
			</wgd:attribute>
		 </xsl:when>
		 <xsl:otherwise>
			<xsl:call-template name="CreateCompoundRuleAttribute">
			   <xsl:with-param name="Member" select="$Member"/>
			   <xsl:with-param name="sAttr">syncat</xsl:with-param>
			</xsl:call-template>
			<xsl:call-template name="CreateCompoundRuleAttribute">
			   <xsl:with-param name="Member" select="$Member"/>
			   <xsl:with-param name="sAttr">syncatAbbr</xsl:with-param>
			</xsl:call-template>
		 </xsl:otherwise>
	  </xsl:choose>
	  <xsl:call-template name="CreateCompoundRuleAttribute">
		 <xsl:with-param name="Member" select="$Member"/>
		 <xsl:with-param name="sAttr">inflClass</xsl:with-param>
	  </xsl:call-template>
	  <xsl:call-template name="CreateCompoundRuleAttribute">
		 <xsl:with-param name="Member" select="$Member"/>
		 <xsl:with-param name="sAttr">inflClassAbbr</xsl:with-param>
	  </xsl:call-template>
	  <xsl:call-template name="CreateCompoundRuleAttribute">
		 <xsl:with-param name="Member" select="$Member"/>
		 <xsl:with-param name="sAttr">requiresInfl</xsl:with-param>
	  </xsl:call-template>
	  <xsl:element name="xsl:attribute">
		 <xsl:attribute name="name">blocksInfl</xsl:attribute>-</xsl:element>
	  <wgd:choose>
		 <wgd:when test="stemName">
			<wgd:copy-of select="stemName"/>
		 </wgd:when>
		 <wgd:when test="morph/stemName">
			<wgd:copy-of select="morph/stemName"/>
		 </wgd:when>
	  </wgd:choose>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ProcessInflectionalTemplatePercolationAndConstraints
	Pass up attributes of a headed compound
		Parameters: template = MoInflAffixTemplate this is for
						 pos = Part of Speech
						 bPartial = Flag for when is Partial
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="ProcessInflectionalTemplatePercolationAndConstraints">
	  <xsl:param name="template"/>
	  <xsl:param name="pos"/>
	  <xsl:param name="bPartial"/>
	  <xsl:choose>
		 <xsl:when test="$bPartial != 'Y'">
			<xsl:element name="xsl:attribute">
			   <xsl:attribute name="name">syncat</xsl:attribute>
			   <xsl:element name="xsl:value-of">
				  <xsl:attribute name="select">@syncat</xsl:attribute>
			   </xsl:element>
			</xsl:element>
			<xsl:element name="xsl:attribute">
			   <xsl:attribute name="name">syncatAbbr</xsl:attribute>
			   <xsl:element name="xsl:value-of">
				  <xsl:attribute name="select">@syncatAbbr</xsl:attribute>
			   </xsl:element>
			</xsl:element>
		 </xsl:when>
		 <xsl:otherwise>
			<xsl:element name="xsl:attribute">
			   <xsl:attribute name="name">syncat</xsl:attribute>
			   <xsl:element name="xsl:text">
				  <xsl:value-of select="$pos/@Id"/>
			   </xsl:element>
			</xsl:element>
			<xsl:element name="xsl:attribute">
			   <xsl:attribute name="name">syncatAbbr</xsl:attribute>
			   <xsl:element name="xsl:text">
				  <xsl:value-of select="$pos/Abbreviation"/>
			   </xsl:element>
			</xsl:element>
		 </xsl:otherwise>
	  </xsl:choose>
	  <xsl:choose>
		 <xsl:when test="$template/@Final='true'">
			<xsl:element name="xsl:attribute">
			   <xsl:attribute name="name">requiresInfl</xsl:attribute>
			   <xsl:element name="xsl:text">-</xsl:element>
			</xsl:element>
			<xsl:element name="xsl:attribute">
			   <xsl:attribute name="name">inflected</xsl:attribute>
			   <xsl:element name="xsl:text">+</xsl:element>
			</xsl:element>
		 </xsl:when>
		 <xsl:otherwise>
			<xsl:element name="xsl:attribute">
			   <xsl:attribute name="name">requiresInfl</xsl:attribute>
			   <xsl:element name="xsl:value-of">
				  <xsl:attribute name="select">@requiresInfl</xsl:attribute>
			   </xsl:element>
			</xsl:element>
			<xsl:element name="xsl:attribute">
			   <xsl:attribute name="name">inflected</xsl:attribute>
			   <xsl:element name="xsl:text">-</xsl:element>
			</xsl:element>
			<xsl:element name="xsl:attribute">
			   <xsl:attribute name="name">blocksInflection</xsl:attribute>
			   <xsl:element name="xsl:text">+</xsl:element>
			</xsl:element>
		 </xsl:otherwise>
	  </xsl:choose>
	  <xsl:if test="$template/@Final='false'">
		 <xsl:variable name="sAllSlotsOptional">
			<xsl:for-each select="$template/PrefixSlots | $template/SuffixSlots">
			   <xsl:if test="key('SlotID',@dst)/@Optional='false'">N</xsl:if>
			</xsl:for-each>
		 </xsl:variable>
		 <xsl:if test="not(contains($sAllSlotsOptional,'N'))">
			<xsl:comment> all slots were optional in a non-final template; this does not work because it would produce an infinite loop (stem_1 = stem_2)</xsl:comment>
			<warning>
			   <xsl:call-template name="ReportInflectionalTemplateName">
				  <xsl:with-param name="template" select="$template"/>
			   </xsl:call-template>
			   <xsl:text> is invalid because it requires more derivation and all slots were optional.</xsl:text>
			   <xsl:text> You must have at least one required slot.   We suggest that you fix this before going on.</xsl:text>
			</warning>
		 </xsl:if>
	  </xsl:if>
	  <xsl:if test="$bPartial != 'Y'">
		 <xsl:comment>check category of the stem</xsl:comment>
		 <xsl:element name="xsl:variable">
			<xsl:attribute name="name">sCategoriesAreCompatible</xsl:attribute>
			<xsl:element name="xsl:call-template">
			   <xsl:attribute name="name">TestCompatibleCategories</xsl:attribute>
			   <xsl:element name="xsl:with-param">
				  <xsl:attribute name="name">sFirstCat</xsl:attribute>
				  <xsl:value-of select="$pos/@Id"/>
			   </xsl:element>
			   <xsl:element name="xsl:with-param">
				  <xsl:attribute name="name">sSecondCat</xsl:attribute>
				  <xsl:element name="xsl:value-of">
					 <xsl:attribute name="select">@syncat</xsl:attribute>
				  </xsl:element>
			   </xsl:element>
			</xsl:element>
		 </xsl:element>
		 <xsl:element name="xsl:if">
			<xsl:attribute name="test">$sCategoriesAreCompatible != $sSuccess</xsl:attribute>
			<xsl:element name="xsl:call-template">
			   <xsl:attribute name="name">ReportFailure</xsl:attribute>
			   <xsl:element name="xsl:with-param">
				  <xsl:attribute name="name">sFirstItemComponent</xsl:attribute>
				  <xsl:element name="xsl:text"> category</xsl:element>
			   </xsl:element>
			   <xsl:element name="xsl:with-param">
				  <xsl:attribute name="name">sFirstItemComponentAbbr</xsl:attribute>
				  <xsl:value-of select="$template/../../Abbreviation"/>
			   </xsl:element>
			   <xsl:element name="xsl:with-param">
				  <xsl:attribute name="name">sFirstItemDescription</xsl:attribute>
				  <xsl:element name="xsl:text">inflectional template</xsl:element>
			   </xsl:element>
			   <xsl:element name="xsl:with-param">
				  <xsl:attribute name="name">sFirstItemValue</xsl:attribute>
				  <xsl:value-of select="Name"/>
			   </xsl:element>
			   <xsl:element name="xsl:with-param">
				  <xsl:attribute name="name">sSecondItemComponent</xsl:attribute>
				  <xsl:element name="xsl:text">category</xsl:element>
			   </xsl:element>
			   <xsl:element name="xsl:with-param">
				  <xsl:attribute name="name">sSecondItemComponentAbbr</xsl:attribute>
				  <xsl:attribute name="select">@syncatAbbr</xsl:attribute>
			   </xsl:element>
			   <xsl:element name="xsl:with-param">
				  <xsl:attribute name="name">sSecondItemDescription</xsl:attribute>
				  <xsl:element name="xsl:text">stem</xsl:element>
			   </xsl:element>
			</xsl:element>
		 </xsl:element>
		 <xsl:comment>Check for productivity restrictions</xsl:comment>
		 <wgd:variable name="stem" select="."/>
		 <wgd:if test="$iPrefixSlotMorphs &gt; 0">
			<wgd:for-each select="preceding-sibling::*[position()&lt;=$iPrefixSlotMorphs]">
			   <wgd:call-template name="CheckInflectionalAffixProdRestrict">
				  <wgd:with-param name="stemMorph" select="$stem"/>
				  <wgd:with-param name="inflMorph" select="."/>
				  <wgd:with-param name="sType" select="'prefix'"/>
			   </wgd:call-template>
			   <wgd:call-template name="CheckAffixAllomorphFeatures">
				  <wgd:with-param name="sAffix" select="'inflectional'"/>
				  <wgd:with-param name="sAttachesTo" select="'inflected form'"/>
				  <wgd:with-param name="morph" select="."/>
				  <wgd:with-param name="stem" select="$stem"/>
			   </wgd:call-template>
			</wgd:for-each>
		 </wgd:if>
		 <wgd:if test="$iSuffixSlotMorphs &gt; 0">
			<wgd:for-each select="following-sibling::*[position()&lt;=$iSuffixSlotMorphs]">
			   <wgd:call-template name="CheckInflectionalAffixProdRestrict">
				  <wgd:with-param name="stemMorph" select="$stem"/>
				  <wgd:with-param name="inflMorph" select="."/>
				  <wgd:with-param name="sType" select="'suffix'"/>
			   </wgd:call-template>
			   <wgd:call-template name="CheckAffixAllomorphFeatures">
				  <wgd:with-param name="sAffix" select="'inflectional'"/>
				  <wgd:with-param name="sAttachesTo" select="'inflected form'"/>
				  <wgd:with-param name="morph" select="."/>
				  <wgd:with-param name="stem" select="$stem"/>
			   </wgd:call-template>
			</wgd:for-each>
		 </wgd:if>
		 <xsl:comment>Percolate features</xsl:comment>
		 <xsl:comment>Check compatitbility of all inflection features</xsl:comment>
		 <wgd:variable name="InflFeaturesToPercolate">
			<wgd:variable name="PrefixSlotsUnification">
			   <wgd:choose>
				  <wgd:when test="$iPrefixSlotMorphs &gt; 1">
					 <wgd:call-template name="UnifyPrefixSlots">
						<wgd:with-param name="PreviousResult"/>
						<wgd:with-param name="PrefixSlot" select="preceding-sibling::*[1]"/>
						<wgd:with-param name="iRemaining" select="$iPrefixSlotMorphs - 1"/>
						<wgd:with-param name="sRuleInfo">
						   <xsl:call-template name="ReportInflectionalTemplateName">
							  <xsl:with-param name="template" select="."/>
						   </xsl:call-template>
						</wgd:with-param>
					 </wgd:call-template>
				  </wgd:when>
				  <wgd:when test="$iPrefixSlotMorphs=1">
					 <wgd:variable name="InflectionalPrefix" select="preceding-sibling::*[1]"/>
					 <wgd:call-template name="UnifyTwoFeatureStructures">
						<wgd:with-param name="FirstFS" select="fs"/>
						<wgd:with-param name="SecondFS" select="$InflectionalPrefix/inflMsa/fs"/>
						<wgd:with-param name="sTopLevelId" select="fs/@id"/>
						<wgd:with-param name="sRuleInfo">
						   <xsl:call-template name="ReportInflectionalTemplateName">
							  <xsl:with-param name="template" select="."/>
						   </xsl:call-template>
						</wgd:with-param>
						<wgd:with-param name="sFirstDescription" select="'stem'"/>
						<wgd:with-param name="sSecondDescription">
						   <wgd:text>inflectional prefix (</wgd:text>
						   <wgd:value-of select="$InflectionalPrefix/shortName"/>
						   <wgd:text>)</wgd:text>
						</wgd:with-param>
					 </wgd:call-template>
				  </wgd:when>
			   </wgd:choose>
			</wgd:variable>
			<wgd:variable name="SuffixSlotsUnification">
			   <wgd:choose>
				  <wgd:when test="$iSuffixSlotMorphs &gt; 1">
					 <wgd:call-template name="UnifySuffixSlots">
						<wgd:with-param name="PreviousResult"/>
						<wgd:with-param name="SuffixSlot" select="following-sibling::*[1]"/>
						<wgd:with-param name="iRemaining" select="$iSuffixSlotMorphs - 1"/>
						<wgd:with-param name="sRuleInfo">
						   <xsl:call-template name="ReportInflectionalTemplateName">
							  <xsl:with-param name="template" select="."/>
						   </xsl:call-template>
						</wgd:with-param>
					 </wgd:call-template>
				  </wgd:when>
				  <wgd:when test="$iSuffixSlotMorphs=1">
					 <wgd:variable name="InflectionalSuffix" select="following-sibling::*[1]"/>
					 <wgd:call-template name="UnifyTwoFeatureStructures">
						<wgd:with-param name="FirstFS" select="fs"/>
						<wgd:with-param name="SecondFS" select="$InflectionalSuffix/inflMsa/fs"/>
						<wgd:with-param name="sTopLevelId" select="fs/@id"/>
						<wgd:with-param name="sRuleInfo">
						   <xsl:call-template name="ReportInflectionalTemplateName">
							  <xsl:with-param name="template" select="."/>
						   </xsl:call-template>
						</wgd:with-param>
						<wgd:with-param name="sFirstDescription" select="'stem'"/>
						<wgd:with-param name="sSecondDescription">
						   <wgd:text>inflectional suffix (</wgd:text>
						   <wgd:value-of select="$InflectionalSuffix/shortName"/>
						   <wgd:text>)</wgd:text>
						</wgd:with-param>
					 </wgd:call-template>
				  </wgd:when>
			   </wgd:choose>
			</wgd:variable>
			<wgd:variable name="PrefixSuffixSlotsUnification">
			   <wgd:choose>
				  <wgd:when test="string-length($PrefixSlotsUnification) &gt; 0 and string-length($SuffixSlotsUnification) &gt; 0">
					 <wgd:call-template name="UnifyTwoFeatureStructures">
						<wgd:with-param name="FirstFS" select="auto-ns1:node-set($PrefixSlotsUnification)/fs"/>
						<wgd:with-param name="SecondFS" select="auto-ns1:node-set($SuffixSlotsUnification)/fs"/>
						<wgd:with-param name="sTopLevelId" select="auto-ns1:node-set($PrefixSlotsUnification)/fs/@id"/>
						<wgd:with-param name="sRuleInfo">
						   <xsl:call-template name="ReportInflectionalTemplateName">
							  <xsl:with-param name="template" select="."/>
						   </xsl:call-template>
						</wgd:with-param>
						<wgd:with-param name="sFirstDescription">
						   <wgd:value-of select="'unification of all the prefix slots'"/>
						</wgd:with-param>
						<wgd:with-param name="sSecondDescription">
						   <wgd:value-of select="'unification of all the suffix slots'"/>
						</wgd:with-param>
					 </wgd:call-template>
				  </wgd:when>
				  <wgd:when test="string-length($PrefixSlotsUnification) &gt; 0">
					 <wgd:copy-of select="$PrefixSlotsUnification"/>
				  </wgd:when>
				  <wgd:when test="string-length($SuffixSlotsUnification) &gt; 0">
					 <wgd:copy-of select="$SuffixSlotsUnification"/>
				  </wgd:when>
			   </wgd:choose>
			</wgd:variable>
			<wgd:call-template name="UnifyTwoFeatureStructures">
			   <wgd:with-param name="FirstFS" select="fs"/>
			   <wgd:with-param name="SecondFS" select="auto-ns1:node-set($PrefixSuffixSlotsUnification)/fs"/>
			   <wgd:with-param name="sTopLevelId" select="fs/@id"/>
			   <wgd:with-param name="sRuleInfo">
				  <xsl:call-template name="ReportInflectionalTemplateName">
					 <xsl:with-param name="template" select="."/>
				  </xsl:call-template>
			   </wgd:with-param>
			   <wgd:with-param name="sFirstDescription">
				  <wgd:value-of select="'stem'"/>
			   </wgd:with-param>
			   <wgd:with-param name="sSecondDescription">
				  <wgd:value-of select="'unification of all the slots'"/>
			   </wgd:with-param>
			</wgd:call-template>
		 </wgd:variable>
		 <wgd:if test="auto-ns1:node-set($InflFeaturesToPercolate)/fs/descendant::feature">
			<wgd:copy-of select="$InflFeaturesToPercolate"/>
		 </wgd:if>
		 <!-- check on stem names
		choose based on stem name flid (have to look at all stem names of this and parent POSes)
		  positive case:
			 when stem name X: create a choose statement to test if each region FS subsumes $InflFeaturesToPercolate; otherwise case is a failure
				 can we have a subsume template for each stemname and call them as appropriate for each category?@#@
		  NotStemNameXStemNameY case: same as for positive, but invert result

		-->
		 <wgd:variable name="StemNameConstraintResult">
			<wgd:call-template>
			   <xsl:attribute name="name">
				  <xsl:text>CheckStemNames</xsl:text>
				  <xsl:value-of select="$pos/@Id"/>
			   </xsl:attribute>
			   <wgd:with-param name="stemName" select="stemName/@id"/>
			   <wgd:with-param name="InflFeatures" select="auto-ns1:node-set($InflFeaturesToPercolate)"/>
			</wgd:call-template>
		 </wgd:variable>
		 <wgd:if test="auto-ns1:node-set($StemNameConstraintResult)/failure">
			<wgd:copy-of select="auto-ns1:node-set($StemNameConstraintResult)"/>
		 </wgd:if>
		 <!-- end of stem name work -->
	  </xsl:if>
	  <xsl:if test="$template/@Final='true'">
		 <xsl:element name="xsl:if">
			<xsl:attribute name="test">@blocksInflection!='-'</xsl:attribute>
			<failure>
			   <xsl:call-template name="ReportInflectionalTemplateName">
				  <xsl:with-param name="template" select="$template"/>
			   </xsl:call-template>
			   <xsl:text> failed because the stem was built by a template that requires more derivation and there was no intervening derivation or compounding.</xsl:text>
			</failure>
		 </xsl:element>
		 <xsl:element name="xsl:if">
			<xsl:attribute name="test">
			   <xsl:text>name()='partial' and @inflected='+'</xsl:text>
			</xsl:attribute>
			<failure>Partial inflectional template has already been inflected.</failure>
		 </xsl:element>
	  </xsl:if>
	  <xsl:comment>copy any inflectional prefixes</xsl:comment>
	  <xsl:element name="xsl:choose">
		 <xsl:element name="xsl:when">
			<xsl:attribute name="test">$iPrefixSlotMorphs > 0</xsl:attribute>
			<xsl:element name="xsl:for-each">
			   <xsl:attribute name="select">preceding-sibling::*[position()&lt;=$iPrefixSlotMorphs]</xsl:attribute>
			   <xsl:element name="xsl:copy-of">
				  <xsl:attribute name="select">.</xsl:attribute>
			   </xsl:element>
			</xsl:element>
		 </xsl:element>
		 <xsl:element name="xsl:otherwise">
			<xsl:comment> output any failure nodes </xsl:comment>
			<xsl:element name="xsl:copy-of">
			   <xsl:attribute name="select">$PrefixSlotMorphs</xsl:attribute>
			</xsl:element>
		 </xsl:element>
	  </xsl:element>
	  <xsl:comment>copy the stem</xsl:comment>
	  <xsl:element name="xsl:copy-of">
		 <xsl:attribute name="select">.</xsl:attribute>
	  </xsl:element>
	  <xsl:comment>copy any inflectional suffixes</xsl:comment>
	  <xsl:element name="xsl:choose">
		 <xsl:element name="xsl:when">
			<xsl:attribute name="test">$iSuffixSlotMorphs > 0</xsl:attribute>
			<xsl:element name="xsl:for-each">
			   <xsl:attribute name="select">following-sibling::*[position()&lt;=$iSuffixSlotMorphs]</xsl:attribute>
			   <xsl:element name="xsl:copy-of">
				  <xsl:attribute name="select">.</xsl:attribute>
			   </xsl:element>
			</xsl:element>
		 </xsl:element>
		 <xsl:element name="xsl:otherwise">
			<xsl:comment> output any failure nodes </xsl:comment>
			<xsl:element name="xsl:copy-of">
			   <xsl:attribute name="select">$SuffixSlotMorphs</xsl:attribute>
			</xsl:element>
		 </xsl:element>
	  </xsl:element>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ReportIncompatibleInflectionClassInTemplate
	Params: morph - inflectional affix morph element
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="ReportIncompatibleInflectionClassInTemplate">
	  <xsl:param name="sType"/>
	  <xsl:param name="sSlotType"/>
	  <failure>
		 <xsl:call-template name="ReportInflectionalTemplateName">
			<xsl:with-param name="template" select=".."/>
		 </xsl:call-template>
		 <xsl:element name="xsl:text"> failed because in the </xsl:element>
		 <xsl:value-of select="$sSlotType"/>
		 <xsl:value-of select="$sType"/>
		 <xsl:element name="xsl:text"> slot '</xsl:element>
		 <xsl:value-of select="key('SlotID',@dst)/Name"/>
		 <xsl:element name="xsl:text">', the inflection class of the stem (</xsl:element>
		 <xsl:element name="xsl:value-of">
			<xsl:attribute name="select">@inflClassAbbr</xsl:attribute>
		 </xsl:element>
		 <xsl:element name="xsl:text">) does not match any of the inflection classes of the inflectional affix (</xsl:element>
		 <xsl:element name="xsl:value-of">
			<xsl:attribute name="select">$morph/shortName</xsl:attribute>
		 </xsl:element>
		 <xsl:element name="xsl:text">).  The inflection class</xsl:element>
		 <xsl:element name="xsl:choose">
			<xsl:element name="xsl:when">
			   <xsl:attribute name="test">count($morph/inflClasses/inflClass)=1</xsl:attribute>
			   <xsl:element name="xsl:text"> of this affix is: </xsl:element>
			</xsl:element>
			<xsl:element name="xsl:otherwise">
			   <xsl:element name="xsl:text">es of this affix are: </xsl:element>
			</xsl:element>
		 </xsl:element>
		 <xsl:element name="xsl:for-each">
			<xsl:attribute name="select">$morph/inflClasses/inflClass</xsl:attribute>
			<xsl:element name="xsl:value-of">
			   <xsl:attribute name="select">@abbr</xsl:attribute>
			</xsl:element>
			<xsl:element name="xsl:if">
			   <xsl:attribute name="test">position() != last()</xsl:attribute>
			   <xsl:element name="xsl:text">, </xsl:element>
			</xsl:element>
		 </xsl:element>
		 <xsl:element name="xsl:text">.</xsl:element>
	  </failure>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ReportInflectionalTemplateName
	Params: template = MoInflAffixTemplate node
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="ReportInflectionalTemplateName">
	  <xsl:param name="template"/>
	  <xsl:text>The inflectional template named '</xsl:text>
	  <xsl:value-of select="$template/Name"/>
	  <xsl:text>' for category '</xsl:text>
	  <xsl:value-of select="$template/../../Name"/>
	  <xsl:text>'</xsl:text>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
StemNameRelevantToPOS
	Determine if the stem name is relevant for this POS or any of its parent POSes
	Output a 'Y' if it is.
		Parameters: stemname = stem name to check
							 pos = current PartOfSpeech
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="StemNameRelevantToPOS">
	  <xsl:param name="stemname"/>
	  <xsl:param name="pos"/>
	  <xsl:choose>
		 <xsl:when test="$pos/StemNames/MoStemName[@Id=$stemname/@Id]">
			<xsl:text>Y</xsl:text>
		 </xsl:when>
		 <xsl:otherwise>
			<!-- this POS did not; check parent POS -->
			<xsl:for-each select="$partsOfSpeech/SubPossibilities[@dst=$pos/@Id]">
			   <xsl:call-template name="StemNameRelevantToPOS">
				  <xsl:with-param name="stemname" select="$stemname"/>
				  <xsl:with-param name="pos" select="../@Id"/>
			   </xsl:call-template>
			</xsl:for-each>
		 </xsl:otherwise>
	  </xsl:choose>
   </xsl:template>
   <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
SuperPOSHasTemplate
	check for inflectional template in this and all super PartOfSpeech elements (i.e.
	those that have a SubPossibilities element that refers to this PartOfSpeech)
		Parameters: pos = current PartOfSpeech
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
   <xsl:template name="SuperPOSHasTemplate">
	  <xsl:param name="pos"/>
	  <xsl:variable name="CurrentPOS" select="key('POSID',$pos)"/>
	  <!-- if this has a template, we're done.  -->
	  <xsl:if test="$CurrentPOS/AffixTemplates/MoInflAffixTemplate">Y</xsl:if>
	  <xsl:if test="not($CurrentPOS/AffixTemplates/MoInflAffixTemplate)">
		 <!-- Did not have one.  Check for any above. -->
		 <xsl:for-each select="$partsOfSpeech/SubPossibilities[@dst=$pos]">
			<xsl:call-template name="SuperPOSHasTemplate">
			   <xsl:with-param name="pos" select="../@Id"/>
			</xsl:call-template>
		 </xsl:for-each>
	  </xsl:if>
   </xsl:template>
</xsl:stylesheet>
<!--
================================================================
Revision History
- - - - - - - - - - - - - - - - - - -
08-Dec-2004    Andy Black  Began working on Initial Draft
================================================================
-->
