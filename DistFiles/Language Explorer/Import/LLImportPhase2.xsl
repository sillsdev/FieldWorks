<?xml version="1.0"?>
<xsl:transform xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0" xmlns:msxsl="urn:schemas-microsoft-com:xslt" xmlns:user="urn:my-scripts">
   <xsl:output encoding="UTF-8" indent="yes"/>

<!-- Rename classes and properties to shortened names -->
<!-- These names changed, but they aren't used in the final output
<xsl:template match="AnalysisWritingSystems6001">
<AnalysisWss6001>
<xsl:copy-of select="@*"/>
<xsl:apply-templates/>
</AnalysisWss6001>
</xsl:template>

<xsl:template match="AnnotationDefinitions6001">
<AnnotationDefs6001>
<xsl:copy-of select="@*"/>
<xsl:apply-templates/>
</AnnotationDefs6001>
</xsl:template>

<xsl:template match="CurrentAnalysisWritingSystems6001">
<CurAnalysisWss6001>
<xsl:copy-of select="@*"/>
<xsl:apply-templates/>
</CurAnalysisWss6001>
</xsl:template>

<xsl:template match="CurrentPronunciationWritingSystems6001">
<CurPronunWss6001>
<xsl:copy-of select="@*"/>
<xsl:apply-templates/>
</CurPronunWss6001>
</xsl:template>

<xsl:template match="CurrentVernacularWritingSystems6001">
<CurVernWss6001>
<xsl:copy-of select="@*"/>
<xsl:apply-templates/>
</CurVernWss6001>
</xsl:template>

<xsl:template match="LexicalDatabase6001"> This was changed in phase1.
<LexDb6001>
<xsl:copy-of select="@*"/>
<xsl:apply-templates/>
</LexDb6001>
</xsl:template>

<xsl:template match="RnResearchNotebook">
<RnResearchNbk>
<xsl:copy-of select="@*"/>
<xsl:apply-templates/>
</RnResearchNbk>
</xsl:template>

<xsl:template match="VernacularWritingSystems6001">
<VernWss6001>
<xsl:copy-of select="@*"/>
<xsl:apply-templates/>
</VernWss6001>
</xsl:template>

<xsl:template match="WordformLookupItem">
<WordFormLookup>
<xsl:copy-of select="@*"/>
<xsl:apply-templates/>
</WordFormLookup>
</xsl:template>
-->

<xsl:template match="LexicalDatabase">
<LexDb>
<xsl:copy-of select="@*"/>
<xsl:apply-templates/>
</LexDb>
</xsl:template>

<xsl:template match="LexReferenceType">
<LexRefType>
<xsl:copy-of select="@*"/>
<xsl:apply-templates/>
</LexRefType>
</xsl:template>

<xsl:template match="MoEndocentricCompound">
<MoEndoCompound>
<xsl:copy-of select="@*"/>
<xsl:apply-templates/>
</MoEndoCompound>
</xsl:template>

<xsl:template match="MoMorphologicalData">
<MoMorphData >
<xsl:copy-of select="@*"/>
<xsl:apply-templates/>
</MoMorphData>
</xsl:template>

<xsl:template match="MoMorphoSyntaxAnalysis">
<MoMorphSynAnalysis>
<xsl:copy-of select="@*"/>
<xsl:apply-templates/>
</MoMorphSynAnalysis>
</xsl:template>

<xsl:template match="PhPhonologicalContext">
<PhPhonContext>
<xsl:copy-of select="@*"/>
<xsl:apply-templates/>
</PhPhonContext>
</xsl:template>

<xsl:template match="PhPhonologicalData">
<PhPhonData>
<xsl:copy-of select="@*"/>
<xsl:apply-templates/>
</PhPhonData>
</xsl:template>

<xsl:template match="RestOfMorphemes5102">
<RestOfMorphs5102>
<xsl:copy-of select="@*"/>
<xsl:apply-templates/>
</RestOfMorphs5102>
</xsl:template>

<xsl:template match="*">
<xsl:copy>
<xsl:copy-of select="@*"/>
<xsl:apply-templates/>
</xsl:copy>
</xsl:template>

   <!-- fix up EOS tags inside Contents5054 -->

   <xsl:template match="Contents5054/StText/Paragraphs14/StTxtPara/Contents16/Str/Run">
   <!--xsl:template match="Run"-->
	  <!-- Note: this works with .NET xsl from within the program, but msxsl requires last()-1 -->
	  <xsl:if test="position()!=last()">
		 <xsl:choose>
			 <xsl:when test="@eos='true'">
			   <xsl:variable name="len" select="string-length(preceding-sibling::Run[1])"/>
			   <xsl:variable name="lastChar" select="substring(preceding-sibling::Run[1], $len, 1)"/>
			   <xsl:element name="Run">
			   <xsl:attribute name="ws">
			   <xsl:value-of select="@ws"/>
			   </xsl:attribute>
			   <xsl:choose>
				  <xsl:when test="$lastChar !='.' and $lastChar !='?' and $lastChar !='!'">
					 <xsl:text>&#xA7; </xsl:text>
				  </xsl:when>
				  <xsl:otherwise>
					 <xsl:text> </xsl:text>
				  </xsl:otherwise>
			   </xsl:choose>
			   </xsl:element>
			</xsl:when>
			<xsl:otherwise>
			   <xsl:copy-of select="."/>
			</xsl:otherwise>
		 </xsl:choose>
	  </xsl:if>
   </xsl:template>

   <!-- Delete some high level items -->

   <xsl:template match="/">
	  <!-- <xsl:text>
		 <![CDATA[<!DOCTYPE LanguageProject SYSTEM "FwDatabase.dtd"> ]]>
		 </xsl:text> -->
	  <xsl:for-each select="//LanguageProject">
			<xsl:element name="LangProject">
			<xsl:for-each select="LexDb6001 | Texts6001 | WordformInventory6001 | Annotations6001 | MorphologicalData6001 | PhonologicalData6001">
			   <xsl:copy>
				  <xsl:apply-templates select="* | @*"/>
			   </xsl:copy>
			</xsl:for-each>
			<xsl:for-each select="AnalyzingAgents6001">
			   <xsl:element name="AnalyzingAgents6001">
				  <xsl:for-each select="CmAgent[Name23/AUni!='M3Parser']">
					 <xsl:element name="CmAgent">
						<xsl:for-each select="@*">
						   <xsl:attribute name="{name()}" >
							  <xsl:value-of select="."/>
						   </xsl:attribute>
						</xsl:for-each>
						<xsl:apply-templates select="*"/>
					 </xsl:element>
				  </xsl:for-each>
			   </xsl:element>
			</xsl:for-each>
			</xsl:element>
	  </xsl:for-each>
   </xsl:template>

   <!-- delete targets in (some) Links -->

   <xsl:template match="Link">
	  <xsl:if test="./parent::*[name() = 'WritingSystem5053'] or ./parent::*[name() = 'SenseType5016'] or ./parent::*[name() = 'UsageTypes5016'] or ./parent::*[name() = 'DomainTypes5016'] or ./parent::*[name() = 'AnthroCodes5016'] or ./parent::*[name() = 'Type29'] or ./parent::*[name() = 'Category5059'] or ./parent::*[name() = 'MorphType5035'] or ./parent::*[name() = 'PartOfSpeech5001'] or ./parent::*[name() = 'PartOfSpeech5117'] or ./parent::*[name() = 'ReversalEntries5016'] or ./parent::*[name() = 'WritingSystem5052'] or ./parent::*[name() = 'AnnotationType34']">
		 <xsl:element name="Link">
			<xsl:for-each select="@*">
			   <xsl:if test="name() != 'target'">
				  <xsl:attribute name="{name()}">
					 <xsl:value-of select="."/>
				  </xsl:attribute>
			   </xsl:if>
			</xsl:for-each>
		 </xsl:element>
	  </xsl:if>
	  <xsl:if test="./parent::*[name() != 'WritingSystem5053'] and ./parent::*[name() != 'SenseType5016'] and ./parent::*[name() != 'UsageTypes5016'] and ./parent::*[name() != 'DomainTypes5016'] and ./parent::*[name() != 'AnthroCodes5016'] and ./parent::*[name() != 'Type29'] and ./parent::*[name() != 'Category5059'] and ./parent::*[name() != 'MorphType5035'] and ./parent::*[name() != 'PartOfSpeech5001'] and ./parent::*[name() != 'PartOfSpeech5117'] and ./parent::*[name() != 'ReversalEntries5016'] and ./parent::*[name() != 'WritingSystem5052'] and ./parent::*[name() != 'AnnotationType34']">
		 <xsl:copy>
			<xsl:apply-templates select="* | @*"/>
		 </xsl:copy>
	  </xsl:if>
   </xsl:template>

   <!-- Delete these tags -->

   <xsl:template match="AllomorphConditions5005 | MorphTypes5005 | Status5005 | SenseTypes5005 | UsageTypes5005 | DomainTypes5005"/>
   <xsl:template match="DateCreated5[./parent::*[name() = 'LexDb'] or ./parent::*[name() = 'WordformInventory']]"/>
   <xsl:template match="DateModified5[./parent::*[name() = 'LexDb'] or ./parent::*[name() = 'WordformInventory']]"/>
   <xsl:template match="Name5[./parent::*[name() = 'LexDb'] or ./parent::*[name() = 'WordformInventory']]"/>

   <!-- Delete ignored languages -->
   <xsl:template match="AUni[@ws='zzzIgnore']"/>
   <xsl:template match="AStr[@ws='zzzIgnore']"/>
   <xsl:template match="Run[@ws='zzzIgnore']"/>
   <xsl:template match="Link[@ws='zzzIgnore'][not(@target)]"/>
   <xsl:template match="Link[@ws='zzzIgnore'][@target] | Link[@wsa='zzzIgnore'][@target] | Link[@wsv='zzzIgnore'][@target]">
	  <xsl:element name="Link">
		 <xsl:attribute name="target">
			<xsl:value-of select="@target"/>
		 </xsl:attribute>
	  </xsl:element>
   </xsl:template>

   <!-- Copy everything else -->

   <xsl:template match="* | @*">
	  <xsl:copy>
		 <xsl:apply-templates select="@* | node()"/>
	  </xsl:copy>
   </xsl:template>

</xsl:transform>
