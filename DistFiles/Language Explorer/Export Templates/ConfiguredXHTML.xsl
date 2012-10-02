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

  <!-- insert newlines after <head> and <body> -->
  <!-- also move some WritingSystemInfo data into the <head> as a set of <meta> elements -->

  <xsl:template match="head">
	<xsl:copy>
	  <xsl:copy-of select="@*"/><xsl:text>&#13;&#10;</xsl:text>
	  <xsl:apply-templates/><xsl:text>&#13;&#10;</xsl:text>
	  <xsl:for-each select="../WritingSystemInfo">
		<xsl:element name="meta">
		  <xsl:attribute name="name"><xsl:value-of select="@lang"/></xsl:attribute>
		  <xsl:attribute name="content"><xsl:value-of select="@name"/></xsl:attribute>
		  <xsl:attribute name="scheme"><xsl:text>Language Name</xsl:text></xsl:attribute>
	</xsl:element><xsl:text>&#13;&#10;</xsl:text>
		<xsl:element name="meta">
		  <xsl:attribute name="name"><xsl:value-of select="@lang"/></xsl:attribute>
		  <xsl:attribute name="content"><xsl:value-of select="@font"/></xsl:attribute>
		  <xsl:attribute name="scheme"><xsl:text>Default Font</xsl:text></xsl:attribute>
	</xsl:element><xsl:text>&#13;&#10;</xsl:text>
	  </xsl:for-each>
	</xsl:copy><xsl:text>&#13;&#10;</xsl:text>
  </xsl:template>

  <xsl:template match="body">
	<xsl:copy>
	  <xsl:copy-of select="@*"/><xsl:text>&#13;&#10;</xsl:text>
	  <xsl:apply-templates/>
	</xsl:copy><xsl:text>&#13;&#10;</xsl:text>
  </xsl:template>

  <xsl:template match="div">
	<xsl:choose>
	  <xsl:when test="@class='subentries'">
		<xsl:if test=".//LexEntryLink_MLHeadWord">
		  <xsl:text>&#13;&#10;</xsl:text>
		  <xsl:copy>
			<xsl:copy-of select="@*"/><xsl:text>&#13;&#10;</xsl:text>
			<xsl:apply-templates/>
		  </xsl:copy>
		</xsl:if>
	  </xsl:when>
	  <xsl:otherwise>
		<xsl:copy>
		  <xsl:copy-of select="@*"/>
			<xsl:if test="not(@class='letter')"><xsl:text>&#13;&#10;</xsl:text></xsl:if>
		  <xsl:apply-templates/>
		</xsl:copy><xsl:text>&#13;&#10;</xsl:text>
	  </xsl:otherwise>
	</xsl:choose>
  </xsl:template>

  <!-- convert <LexEntry id="xyz"> to <div class="entry" id="xyz"> -->

  <xsl:template match="LexEntry">
		<!-- For minor entries, we need to make sure the minorentry css class is invoked (LT-12119).
		It seems the only way of detecting this is to look for a grandchild Paragraph style of Dictionary-Minor. -->
		<xsl:choose>
			<xsl:when test="*/Paragraph[@style='Dictionary-Minor']">
				<div class="minorentry">
					<xsl:copy-of select="@*"/>
					<xsl:text>&#13;&#10;</xsl:text>
					<xsl:apply-templates/>
					<xsl:text>&#13;&#10;</xsl:text>
				</div>
				<xsl:text>&#13;&#10;</xsl:text>
			</xsl:when>
			<xsl:otherwise>
				<div class="entry">
					<xsl:copy-of select="@*"/>
					<xsl:text>&#13;&#10;</xsl:text>
					<xsl:apply-templates/>
					<xsl:text>&#13;&#10;</xsl:text>
				</div>
				<xsl:text>&#13;&#10;</xsl:text>
			</xsl:otherwise>
		</xsl:choose>
  </xsl:template>

  <!-- process the pictures first so that they can float inside this entry instead of after (in Firefox) -->

  <xsl:template match="LexEntry_Self">
	<xsl:for-each select="Paragraph/_PicturesOfSenses">
		<xsl:apply-templates/>
	</xsl:for-each>
	<!-- the _PicturesOfSenses template does nothing, so a simple apply-templates works to do the rest -->
	<xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="_PicturesOfSenses">
  </xsl:template>

	<!-- ignore some unwanted levels -->
	<xsl:template match="Paragraph[@style='Dictionary-Subentry']">
		<div class="subentry">
			<!-- If our parent is a LexEntryLink we should add an id as an anchor. We want it on the div element so add it here rather than in the LexEntryLink  itself -->
			<xsl:if test="parent::LexEntryLink[@target]"><xsl:attribute name="id"><xsl:value-of select="../@target"/></xsl:attribute></xsl:if>
			<xsl:apply-templates/>
		</div>
	</xsl:template>

  <xsl:template match="Paragraph">
	<xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="LexEntry_Senses|LexEntry_AlternateForms|LexEntry_Etymology|LexEntry_Pronunciations">
	<xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="LexSense_Examples|LexSense_Senses|LexSense_MorphoSyntaxAnalysis|LexSense_LexSenseReferences|LexSense_Subentries|_Subentries|LexEntryLink_Subentries|LexSense_MinorEntries|_Self">
	<xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="LexSense_SemanticDomains|LexSense_AnthroCodes|LexSense_UsageTypes|LexSense_DomainTypes|LexSense_SenseType|LexSense_Status">
	<xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="MoMorphSynAnalysisLink|MoInflAffMsa_Slots|MoForm_MorphType">
	<xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="LexPronunciation_Location|LexReference_Targets|LexExampleSentence_Translations|CmTranslation_Type">
	<xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="LexEntryLink_Hvo|LexEntry_MainEntriesOrSenses|LexEtymology|LexPronunciation">
	  <xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="_ComplexFormEntryBackRefs|_VisibleVariantEntryRefs|_VariantFormEntryBackRefs|_VisibleEntryRefs|_ComplexFormEntryRefs">
	<xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="LexEntryRefLink|LexEntryRefLink_OwningEntry|LexSense_ComplexFormEntryBackRefs|LexEntryRef_OwningEntry">
	  <xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="LexEntry_EntryRefs|LexEntryRef|LexEntryRef_ComplexEntryTypes|LexEntryRef_VariantEntryTypes|LexEntryRef_ComponentLexemes">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="_MinimalLexReferences">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="LexSense_VariantFormEntryBackRefs">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="LexSense_RefsFrom_LexReference_Targets">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="_AllComplexFormEntryBackRefs">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="LexEntryRefLink_MorphoSyntaxAnalyses">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="MoStemMsaLink">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="LexEntryRefLink_ExampleSentences">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="LexExampleSentenceLink">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="MoAffixAllomorph_PhoneEnv">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="PhEnvironmentLink">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="MoForm">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="LexEntryRef_PrimaryLexemes">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="LexSense_VisibleComplexFormBackRefs|LexEntryLink_ComplexFormEntryRefs|LexEntryLink_VariantFormEntryBackRefs">
	<xsl:apply-templates/>
  </xsl:template>

  <!-- skip into _VisibleComplexFormBackRefs, but handle embedded paragraphs if there are any -->

  <xsl:template match="_VisibleComplexFormBackRefs">
	<xsl:choose>
	  <xsl:when test="Paragraph">
		<xsl:for-each select="Paragraph">
		  <div>
			<xsl:attribute name="class"><xsl:text>complexform</xsl:text></xsl:attribute>
			<xsl:apply-templates/>
		  </div>
		</xsl:for-each>
	  </xsl:when>
	  <xsl:otherwise>
		<xsl:apply-templates/>
	  </xsl:otherwise>
	</xsl:choose>
  </xsl:template>

  <!-- ignore LexEntry_HomographNumber unless its parent's class attribute relates to homograph numbers -->

  <xsl:template match="LexEntry_HomographNumber">
	<xsl:if test="contains(../@class,'HomographNumber') or contains(../@class,'homographnumber')">
	  <xsl:apply-templates/>
	</xsl:if>
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
	<LexEntry_HomographNumber class="headword">
		<Integer val="1"/>
	</LexEntry_HomographNumber>

	<span class="headword">
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
					<AStr ws="x-kal">
					  <Run ws="x-kal">bi</Run>
					</AStr>
				</MoForm_Form>
			</MoForm>
		</LexEntry_LexemeForm>
		<LexEntry_LexemeForm>
			<MoForm>
				<MoForm_MorphType>
					<MoMorphTypeLink>
						<MoMorphType_Postfix>
							<AUni ws="en"></AUni>
						</MoMorphType_Postfix>
					</MoMorphTypeLink>
				</MoForm_MorphType>
			</MoForm>
		</LexEntry_LexemeForm>
		<LiteralString>
			<Str>
			  <Run ws="en">  </Run>
			</Str>
		</LiteralString>
</span>

	into:

	<span class="headword">-banku=<span class="xhomographnumber">1</span></span>
  -->

  <xsl:template match="LexEntry_LexemeForm">
	<xsl:choose>
	  <xsl:when test="not(MoForm/*)">
	  </xsl:when>
	  <xsl:when test="MoForm/span/MoForm_MorphType/MoMorphTypeLink">
		<xsl:apply-templates/>
	  </xsl:when>
	  <xsl:when test="preceding-sibling::LexEntry_LexemeForm[1]/MoForm and following-sibling::LexEntry_LexemeForm[1]/MoForm">
		<xsl:choose>
		  <xsl:when test="MoForm/Alternative/MoForm_Form/AStr">
			<xsl:for-each select="MoForm/Alternative">
			  <span class="xitem"><xsl:attribute name="lang"><xsl:value-of select="@ws"/></xsl:attribute>
				<xsl:value-of select="../../../LexEntry_LexemeForm/MoForm/span/MoForm_MorphType/MoMorphTypeLink/MoMorphType_Prefix/AUni"/>
				<xsl:value-of select="MoForm_Form/AStr/Run"/>
				<xsl:value-of select="../../../LexEntry_LexemeForm/MoForm/span/MoForm_MorphType/MoMorphTypeLink/MoMorphType_Postfix/AUni"/>
				<xsl:if test="../../../LexEntry_HomographNumber/Integer/@val">
				  <span class="xhomographnumber"><xsl:attribute name="lang"><xsl:value-of select="/html/WritingSystemInfo[@analTag='']/@lang"/></xsl:attribute><xsl:value-of select="../../../LexEntry_HomographNumber/Integer/@val"/></span>
				</xsl:if>
			  </span>
			</xsl:for-each>
		  </xsl:when>
		  <xsl:otherwise>
			<xsl:value-of select="../LexEntry_LexemeForm/MoForm/span/MoForm_MorphType/MoMorphTypeLink/MoMorphType_Prefix/AUni"/>
			<xsl:value-of select="MoForm/MoForm_Form/AStr/Run"/>
			<xsl:value-of select="../LexEntry_LexemeForm/MoForm/span/MoForm_MorphType/MoMorphTypeLink/MoMorphType_Postfix/AUni"/>
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

  <!-- transform the following structure: (with Citation Form)

<span class="headword">
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
<Alternative ws="seh"
	<LexEntry_CitationForm>
		<AStr ws="seh">
		  <Run ws="seh">banku</Run>
		</AStr>
	</LexEntry_CitationForm>
</Alternative>
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
	<LexEntry_HomographNumber class="lexical-unit">
		<Integer val="1"/>
	</LexEntry_HomographNumber>
</span>

	into:

	<span class="headword">-banku=<span class="xhomographnumber">1</span></span>
  -->

  <xsl:template match="LexEntry_CitationForm">
	<xsl:choose>
	  <xsl:when test="preceding-sibling::LexEntry_LexemeForm[1]/MoForm and following-sibling::LexEntry_LexemeForm[1]/MoForm">
		  <xsl:value-of select="../LexEntry_LexemeForm/MoForm/span/MoForm_MorphType/MoMorphTypeLink/MoMorphType_Prefix/AUni"/>
		  <xsl:value-of select="AStr/Run"/>
		  <xsl:value-of select="../LexEntry_LexemeForm/MoForm/span/MoForm_MorphType/MoMorphTypeLink/MoMorphType_Postfix/AUni"/>
		  <xsl:if test="../LexEntry_HomographNumber/Integer/@val">
			<span class="xhomographnumber"><xsl:attribute name="lang"><xsl:value-of select="/html/WritingSystemInfo[@analTag='']/@lang"/></xsl:attribute><xsl:value-of select="../LexEntry_HomographNumber/Integer/@val"/></span>
		  </xsl:if>
	  </xsl:when>
	  <xsl:when test="../preceding-sibling::LexEntry_LexemeForm[1]/MoForm and ../following-sibling::LexEntry_LexemeForm[1]/MoForm">
		  <xsl:value-of select="../../LexEntry_LexemeForm/MoForm/span/MoForm_MorphType/MoMorphTypeLink/MoMorphType_Prefix/AUni"/>
		  <xsl:value-of select="AStr/Run"/>
		  <xsl:value-of select="../../LexEntry_LexemeForm/MoForm/span/MoForm_MorphType/MoMorphTypeLink/MoMorphType_Postfix/AUni"/>
		  <xsl:if test="../../LexEntry_HomographNumber/Integer/@val">
			<span class="xhomographnumber"><xsl:attribute name="lang"><xsl:value-of select="/html/WritingSystemInfo[@analTag='']/@lang"/></xsl:attribute><xsl:value-of select="../../LexEntry_HomographNumber/Integer/@val"/></span>
		  </xsl:if>
	  </xsl:when>

	  <xsl:otherwise>
		<xsl:call-template name="ProcessMultiString"></xsl:call-template>
	  </xsl:otherwise>
	</xsl:choose>
  </xsl:template>


  <xsl:template match="Integer">
	<xsl:value-of select="@val"/>
  </xsl:template>

  <xsl:template match="LexSense/LiteralString">
	<xsl:if test="Str/Run/@bold='on'">
	  <span class="xsensenumber"><xsl:value-of select="Str/Run"/></span>
	</xsl:if>
  </xsl:template>

  <xsl:template match="LiteralString">
	<xsl:if test="Str/Run/@fontsize and Str/Run/@editable='not'">
	  <span><xsl:attribute name="class">xlanguagetag</xsl:attribute><xsl:attribute name="lang"><xsl:value-of select="Str/Run/@ws"/></xsl:attribute>
		<xsl:value-of select="Str/Run"/>
	  </span>
	</xsl:if>
  </xsl:template>


  <xsl:template match="LexEtymology_Form">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>

  <xsl:template match="LexEtymology_Comment">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>

  <xsl:template match="LexEtymology_Gloss">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>

  <xsl:template match="MoStemMsaLink_MLPartOfSpeech">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>

  <xsl:template match="LexEtymology_Source">
	<xsl:call-template name="ProcessMultiUnicode"></xsl:call-template>
  </xsl:template>

  <xsl:template match="MoMorphType_Prefix|MoMorphType_Postfix">
	<xsl:call-template name="ProcessMultiUnicode"></xsl:call-template>
  </xsl:template>

  <xsl:template match="MoForm_Form">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>


  <!-- convert <LexEntryRef_Summary>  -->

  <xsl:template match="LexEntryRef_Summary">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>


  <!-- convert <LexPronunciation_Form>  -->

  <xsl:template match="LexPronunciation_Form">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>


  <!-- convert <LexPronunciation_CVPattern>  -->

  <xsl:template match="LexPronunciation_CVPattern">
	<xsl:call-template name="ProcessString"></xsl:call-template>
  </xsl:template>


  <!-- convert <LexPronunciation_Tone>  -->

  <xsl:template match="LexPronunciation_Tone">
	<xsl:call-template name="ProcessString"></xsl:call-template>
  </xsl:template>

  <xsl:template match="PhEnvironment_StringRepresentation">
	<xsl:call-template name="ProcessString"/>
  </xsl:template>

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

  <xsl:template match="CmAnthroItemLink">
	<xsl:choose>
	  <xsl:when test="count(../CmAnthroItemLink) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>

  <xsl:template match="LexEntryTypeLink">
	<xsl:choose>
	  <xsl:when test="count(../LexEntryTypeLink) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>

  <xsl:template match="LexEntryRefLink">
	<xsl:choose>
	  <xsl:when test="Paragraph"><div class="subentry"><xsl:apply-templates></xsl:apply-templates></div><xsl:text>&#13;&#10;</xsl:text></xsl:when>
	  <xsl:otherwise>
		<xsl:choose>
		  <xsl:when test="count(../LexEntryRefLink) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
		  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
		</xsl:choose>
	  </xsl:otherwise>
	</xsl:choose>
  </xsl:template>

  <xsl:template match="LexRefTypeLink">
	<xsl:choose>
	  <xsl:when test="count(../LexRefTypeLink) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>

  <xsl:template match="MoInflAffixSlotLink">
	<xsl:choose>
	  <xsl:when test="count(../MoInflAffixSlotLink) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>

  <xsl:template match="MoMorphTypeLink">
	<xsl:choose>
	  <xsl:when test="count(../MoMorphTypeLink) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>

  <xsl:template match="LexReferenceLink">
	<xsl:choose>
	  <xsl:when test="count(../LexReferenceLink) > 1"><span class="xitem"><xsl:apply-templates/></span></xsl:when>
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
	</xsl:choose>
  </xsl:template>

  <xsl:template match="LexSenseLink">
		<xsl:choose>
		  <xsl:when test="count(../LexSenseLink)+count(../LexEntryLink) > 1"><span class="xitem"><a href="#{@target}"><xsl:apply-templates/></a></span></xsl:when>
		   <xsl:otherwise><a href="#{@target}"><xsl:apply-templates/></a></xsl:otherwise>
		</xsl:choose>

  </xsl:template>

  <xsl:template match="LexEntryLink">
	  <xsl:choose>
	  <xsl:when test="parent::_Subentries"><xsl:apply-templates/></xsl:when> <!-- If the parent is "subentries" this is a subentry and there isn't another one to jump to. -->
	  <xsl:when test="Paragraph/LexEntryLink_Hvo"><div class="subentry"><a href="#{@target}"><xsl:apply-templates/></a></div><xsl:text>&#13;&#10;</xsl:text></xsl:when>
	  <xsl:when test="count(../LexEntryLink)+count(../LexSenseLink) > 1"><span class="xitem"><a href="#{@target}"><xsl:apply-templates/></a></span></xsl:when>
	  <xsl:when test="child::span"><a href="#{@target}"><xsl:apply-templates/></a></xsl:when> <!-- if there is a span child the anchor should always be added (I think) naylor Jul 2011 -->
	  <xsl:otherwise><xsl:apply-templates/></xsl:otherwise> <!-- we can't arbitrarily put a link here without risking invalid xhtml. Newly identified missing anchors should be handled with a new test -->
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



  <xsl:template match="CmPossibility_Abbreviation|CmPossibility_Name">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexEntryType_ReverseAbbr|LexEntryType_ReverseName">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>
  <xsl:template match="LexRefType_ReverseAbbreviation|LexRefType_ReverseName">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>

  <!-- convert <LexEntry_Comment>  -->

  <xsl:template match="LexEntry_Comment">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>


  <!-- convert <LexEntry_Bibliography>  -->

  <xsl:template match="LexEntry_Bibliography">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>


  <!-- convert <LexEntry_LiteralMeaning>  -->

  <xsl:template match="LexEntry_LiteralMeaning">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>


  <!-- convert <LexEntry_DateCreated> and <LexEntry_DateModified> -->

  <xsl:template match="LexEntry_DateCreated|LexEntry_DateModified">
	<xsl:call-template name="ProcessString"></xsl:call-template>
  </xsl:template>


  <!-- convert <LexSense> to <span class="sense"-->

  <xsl:template match="LexSense">
	<xsl:choose>
			<!-- If there is a Paragraph style indicated, put that in: -->
	  <xsl:when test="../../Paragraph">
		<div class="sensepara">
					<xsl:copy-of select="@id"/>
					<xsl:if test="../../Paragraph/@style"><xsl:attribute name="style"><xsl:value-of select="../../Paragraph/@style"/></xsl:attribute></xsl:if>
					<!-- If there is a sense number indicated, this will precede the current LexSense (LT-12119), so we have to hunt for it: -->
					<xsl:if test="preceding-sibling::ItemNumber[1]/@class='xsensenumber'">
						<span class="xsensenumber"><xsl:value-of select="../ItemNumber/Str/Run"/></span>
					</xsl:if>
		  <xsl:apply-templates/>
		</div>
	  </xsl:when>
	  <xsl:when test="following-sibling::LexSense[1]/@id=@id">
		<span class="sense">
		  <xsl:apply-templates/>
		</span>
	  </xsl:when>
	  <xsl:otherwise>
		<span class="sense">
		  <xsl:copy-of select="@id"/>
		  <xsl:apply-templates/>
		</span>
	  </xsl:otherwise>
	</xsl:choose>
  </xsl:template>

	<xsl:template match="ItemNumber">
		<!-- We've already dealt with sense number ItemNumber elements preceding a LexSense where a Paragraph style is invoked, so exlcude those here: -->
		<xsl:if test="not(@class='xsensenumber' and following-sibling::LexSense and ../../Paragraph)">
	  <span><xsl:attribute name="class"><xsl:value-of select="@class"/></xsl:attribute><xsl:value-of select="Str/Run"/></span>
		</xsl:if>
  </xsl:template>

	<!-- convert <MoMorphSynAnalysisLink_MLPartOfSpeech> to <span class="grammatical-info_lg"> -->

  <xsl:template match="MoMorphSynAnalysisLink_MLPartOfSpeech">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>


  <xsl:template match="MoInflAffixSlot_Name">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>


  <!-- convert <MoMorphSynAnalysisLink_FeaturesTSS> to list of <span class="grammatical-info-feature"> -->

  <xsl:template match="MoMorphSynAnalysisLink_FeaturesTSS">
	<xsl:call-template name="ProcessString"></xsl:call-template>
  </xsl:template>


  <!-- convert <MoMorphSynAnalysisLink_ExceptionFeaturesTSS> to list of <span class="grammatical-info-exceptfeature"> -->

  <xsl:template match="MoMorphSynAnalysisLink_ExceptionFeaturesTSS">
	<xsl:call-template name="ProcessString"></xsl:call-template>
  </xsl:template>


  <!-- convert <MoMorphSynAnalysisLink_MLInflectionClass> to <span class="grammatical-info_lg"> -->

  <xsl:template match="MoMorphSynAnalysisLink_MLInflectionClass">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>


  <!-- convert <LexSense_Source> -->

  <xsl:template match="LexSense_Source">
	<xsl:call-template name="ProcessString"></xsl:call-template>
  </xsl:template>


  <!-- convert <LexSense_Gloss> or <LexSense_Definition> to <span class="definition_lg"> -->

  <xsl:template match="LexSense_Gloss|LexSense_Definition">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>


  <xsl:template match="LexExampleSentence_Example">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>

  <xsl:template match="CmTranslation_Translation">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>


  <xsl:template match="LexSenseLink_MLOwnerOutlineName|LexEntryLink_MLHeadWord|LexEntryLink_HeadWordRef|_MLHeadWord">
	  <xsl:for-each select="AStr/Run">
		<xsl:choose>
		  <xsl:when test="@namedStyle='' or not(@namedStyle)"><xsl:value-of select="."/></xsl:when>
		  <xsl:when test="@namedStyle='Homograph-Number'"><span class="xhomographnumber"><xsl:value-of select="."/></span></xsl:when>
		  <!-- won't appear in LexEntryLink_MLHeadWord -->
		  <xsl:when test="@namedStyle='Sense-Reference-Number'"><span class="xsensenumber"><xsl:value-of select="."/></span></xsl:when>
		</xsl:choose>
	  </xsl:for-each>
  </xsl:template>


  <!-- convert <LexEntry_SummaryDefinition> -->

  <xsl:template match="LexEntry_SummaryDefinition">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>


  <!-- convert <LexSense_SocioLinguisticsNote> -->

  <xsl:template match="LexSense_SocioLinguisticsNote">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>


  <!-- convert <LexSense_GeneralNote> -->

  <xsl:template match="LexSense_GeneralNote">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>


  <!-- convert <LexSense_Restrictions> -->

  <xsl:template match="LexSense_Restrictions">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>


  <!-- convert <LexSense_PhonologyNote> -->

  <xsl:template match="LexSense_PhonologyNote">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>


  <!-- convert <LexSense_SemanticsNote> -->

  <xsl:template match="LexSense_SemanticsNote">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>


  <!-- convert <LexSense_AnthroNote> -->

  <xsl:template match="LexSense_AnthroNote">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>


  <!-- convert <LexSense_Bibliography> -->

  <xsl:template match="LexSense_Bibliography">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>


  <!-- convert <LexSense_DiscourseNote> -->

  <xsl:template match="LexSense_DiscourseNote">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>


  <!-- convert <LexSense_ScientificName> -->

  <xsl:template match="LexSense_ScientificName">
	<xsl:call-template name="ProcessString"></xsl:call-template>
  </xsl:template>


  <!-- convert <LexSense_EncyclopedicInfo> -->

  <xsl:template match="LexSense_EncyclopedicInfo">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
  </xsl:template>


  <xsl:template match="CmPictureLink">
	  <div class="pictureRight">
		<xsl:for-each select=".//CmPicture_PictureFile">
		  <xsl:call-template name="ProcessPictureFile"/>
		</xsl:for-each>
		<div class="pictureCaption">
		  <xsl:apply-templates/>
		</div>
	  </div><xsl:text>&#13;&#10;</xsl:text>
  </xsl:template>
  <xsl:template match="CmPicture_PictureFile"/>

  <xsl:template name="ProcessPictureFile">
	<img class="picture">
	  <xsl:attribute name="src">
		<xsl:call-template name="FixSlashes">
		  <xsl:with-param name="text">
			<xsl:choose>
			  <xsl:when test="not(CmFileLink/CmFile_InternalPath/AUni='')">
				<xsl:value-of select="CmFileLink/CmFile_InternalPath/AUni"/>
			  </xsl:when>
			</xsl:choose>
		  </xsl:with-param>
		</xsl:call-template>
	  </xsl:attribute>
	  <xsl:attribute name="alt">
		<xsl:choose>
		  <xsl:when test="..//CmPicture_Caption/AStr/Run">
			<xsl:value-of select="..//CmPicture_Caption/AStr[1]/Run"/>
		  </xsl:when>
		  <xsl:otherwise>
			<xsl:call-template name="FixSlashes">
			  <xsl:with-param name="text" select="CmFileLink/CmFile_InternalPath/AUni"/>
			</xsl:call-template>
		  </xsl:otherwise>
		</xsl:choose>
	  </xsl:attribute>
	</img>
  </xsl:template>


  <!-- convert <CmPictureLink_SenseNumberTSS> -->

  <xsl:template match="CmPictureLink_SenseNumberTSS">
	<xsl:if test="count(ancestor::LexEntry_Hvo/Paragraph/span/LexEntry_Senses//LexSense)>1">
	  <xsl:call-template name="ProcessString"></xsl:call-template>
	</xsl:if>
  </xsl:template>


  <!-- convert <CmPicture_Caption> -->

  <xsl:template match="CmPicture_Caption">
	<xsl:call-template name="ProcessMultiString"></xsl:call-template>
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
	<xsl:if test="AUni">
	  <xsl:call-template name="ProcessMultiUnicode"></xsl:call-template>
	</xsl:if>
	<xsl:if test="StText/StText_Paragraphs/StTxtPara">
	  <div>
		<xsl:attribute name="class">customtext</xsl:attribute>
		<xsl:for-each select="StText/StText_Paragraphs/StTxtPara">
			<xsl:call-template name="ProcessStTxtPara"></xsl:call-template>
		</xsl:for-each>
	  </div>
	</xsl:if>
  </xsl:template>


  <!-- *************************************************************** -->
  <!-- * Some span classes need to be tagged to reflect their content. -->
  <!-- *************************************************************** -->

  <xsl:template match="span">
	<xsl:copy>
	  <xsl:copy-of select="@title"/>
	  <xsl:choose>
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
		<xsl:when test="LexEntryLink_HeadWordRef/AStr/Run">
		  <xsl:call-template name="SetVernAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="LexEntryLink_HeadWordRef/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="_MLHeadWord/AStr/Run">
		  <xsl:call-template name="SetVernAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="_MLHeadWord/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="LexEtymology_Form/AStr/Run">
		  <xsl:call-template name="SetVernAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="LexEtymology_Form/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="LexExampleSentence_Example/AStr/Run">
		  <xsl:call-template name="SetVernAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="LexExampleSentence_Example/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="MoForm_Form/AStr/Run">
		  <xsl:call-template name="SetVernAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="MoForm_Form/AStr[1]/@ws"/></xsl:call-template>
		</xsl:when>
		<xsl:when test="LexPronunciation_Form/AStr/Run">
		  <xsl:call-template name="SetPronAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="LexPronunciation_Form/AStr[1]/@ws"/></xsl:call-template>
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
		<xsl:when test="Alternative/LexEntryLink_HeadWordRef/AStr/Run">
		  <xsl:call-template name="SetVernAttrs"><xsl:with-param name="Class" select="@class"/><xsl:with-param name="Lang" select="Alternative/LexEntryLink_HeadWordRef/AStr[1]/@ws"/></xsl:call-template>
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

  <xsl:template name="ProcessAStrRuns">
	<xsl:for-each select="Run">
	  <xsl:choose>
		<xsl:when test="not(@ws=../@ws) or not(@namedStyle=../@namedStyle)">
		  <span>
			<xsl:if test="@namedStyle"><xsl:attribute name="class"><xsl:value-of select="translate(@namedStyle,' ','_')"/></xsl:attribute></xsl:if>
			<xsl:attribute name="lang"><xsl:value-of select="@ws"/></xsl:attribute>
			<xsl:value-of select="."/>
		  </span>
		</xsl:when>
		<xsl:otherwise>
		  <xsl:value-of select="."/>
		</xsl:otherwise>
	  </xsl:choose>
	</xsl:for-each>
  </xsl:template>

  <!-- process content that consists of one or more <AUni> elements -->

  <xsl:template name="ProcessMultiUnicode">
	<xsl:for-each select="AUni">
	  <span class="xitem"><xsl:attribute name="lang"><xsl:value-of select="@ws"/></xsl:attribute>
		<xsl:value-of select="."/>
	  </span>
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
	<xsl:for-each select="Str/Run">
	  <span>
		<xsl:if test="@namedStyle"><xsl:attribute name="class"><xsl:value-of select="translate(@namedStyle,' ','_')"/></xsl:attribute></xsl:if>
		<xsl:attribute name="lang"><xsl:value-of select="@ws"/></xsl:attribute>
		<xsl:value-of select="."/>
	  </span>
	</xsl:for-each>
  </xsl:template>


  <!-- process content that consists of a <StTxtPara> element -->

  <xsl:template name="ProcessStTxtPara">
	<div>
		<xsl:if test="Paragraph/@style"><xsl:attribute name="class"><xsl:value-of select="translate(Paragraph/@style,' ','_')"/></xsl:attribute></xsl:if>
		<xsl:for-each select="Paragraph/StTxtPara_Contents">
		  <xsl:call-template name="ProcessString"/>
		</xsl:for-each>
	</div>
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
