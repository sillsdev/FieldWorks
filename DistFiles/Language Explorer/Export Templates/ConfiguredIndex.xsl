<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="xml" version="1.0" encoding="utf-8" indent="yes"/>

<!-- This stylesheet works on the output of Configured.xsl to produce a tightly configure
	 XML file that can be processed by another XSLT to show "index cards" in a browser. -->

<!-- Strip all white space and leave it up to the stylesheet text elements below to put in
	 appropriate spacing. -->

<xsl:strip-space elements="*"/>
<xsl:preserve-space elements="Run"/><!-- but these spaces are significant! -->

<xsl:template match="ExportedDictionary">
	<!-- Write Mara cluster header -->
	<xsl:text>&#13;&#10;</xsl:text>
	<xsl:text disable-output-escaping="yes">&lt;?xml-stylesheet type="text/xsl" href="C:\Program Files\SIL\FieldWorks\Language Explorer\Export Templates\IndexCards.xslt"?&gt;</xsl:text><xsl:text>&#13;&#10;</xsl:text>
	<xsl:comment>Card display of FW configured view</xsl:comment><xsl:text>&#13;&#10;</xsl:text>
	<flex-configured-lexicon><xsl:text>&#13;&#10;</xsl:text>
	<xsl:apply-templates/>
	</flex-configured-lexicon><xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<xsl:template match="LexEntry">
	<!-- If this is not the very first entry, add a blank line before it. -->
	<xsl:if test="position()>1">
		<xsl:text>&#13;&#10;</xsl:text>
	</xsl:if>
	<headword>
		<xsl:attribute name="name">
			<xsl:value-of select="LexEntry_HeadWord[1]/AStr/Run"/>
		</xsl:attribute>
		<xsl:attribute name="ws">
			<xsl:value-of select="LexEntry_HeadWord[1]/AStr/Run/@ws"/>
		</xsl:attribute>
		<xsl:text>&#13;&#10;</xsl:text>
		<xsl:if test="LexEntry_HomographNumber/Integer/@val">
			<xsl:text>&#32;&#32;</xsl:text>
			<homograph><xsl:value-of select="LexEntry_HomographNumber/Integer/@val"/></homograph>
			<xsl:text>&#13;&#10;</xsl:text>
		</xsl:if>
		<xsl:apply-templates/>
	</headword>
	<xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- already handled above, so ignore contents. -->

<xsl:template match="LexEntry_HeadWord"/>
<xsl:template match="LexEntry_HomographNumber"/>


<!-- Output the primary lexeme form(s) and morphtype. **lexeme form-->

<xsl:template match="LexEntry_LexemeForm">
	<xsl:for-each select="MoForm/MoForm_Form/AStr">
		<xsl:text>&#32;&#32;</xsl:text>
		<lexeme>
			<xsl:copy-of select="@ws"/>
			<xsl:value-of select="Run"/><!-- assumes a single Run -->
		</lexeme>
		<xsl:text>&#13;&#10;</xsl:text>
	</xsl:for-each>
</xsl:template>


<!-- Output the alternate forms and morphtypes (not part of standard MDF). -->

<xsl:template match="LexEntry_AlternateForms">
	<xsl:for-each select="Allomorph/MoForm_Form/AStr">
		<xsl:text>&#32;&#32;</xsl:text>
		<alt-form>
			<xsl:copy-of select="@ws"/>
			<xsl:value-of select="Run"/><!-- assumes a single Run -->
		</alt-form>
		<xsl:text>&#13;&#10;</xsl:text>
	</xsl:for-each>
	<xsl:choose>
		<xsl:when test="Allomorph/MoForm_MorphType/Link/Alt/@name">
			<xsl:text>&#32;&#32;</xsl:text>
			<alt-name>
				<xsl:value-of select="Allomorph/MoForm_MorphType/Link/Alt/@name"/>
			</alt-name>
			<xsl:text>&#13;&#10;</xsl:text>
		</xsl:when>
		<xsl:when test="Allomorph/MoForm_MorphType/Link/Alt/@abbr">
			<xsl:text>&#32;&#32;</xsl:text>
			<alt-abbrev>
				<xsl:value-of select="Allomorph/MoForm_MorphType/Link/Alt/@abbr"/>
			</alt-abbrev>
			<xsl:text>&#13;&#10;</xsl:text>
		</xsl:when>
	</xsl:choose>
</xsl:template>


<!-- Output the citation form(s). -->

<xsl:template match="LexEntry_CitationForm">
	<xsl:for-each select="AStr">
		<xsl:text>&#32;&#32;</xsl:text>
		<citation-form>
			<xsl:copy-of select="@ws"/>
			<xsl:value-of select="Run"/><!-- assumes a single Run -->
		</citation-form>
		<xsl:text>&#13;&#10;</xsl:text>
	</xsl:for-each>
</xsl:template>

<!-- Process subentries of this entry. -->

<xsl:template match="_ComplexFormEntryBackRefs">
	<xsl:for-each select="LexEntryRefLink/LexEntryRefLink_OwningEntry/Link/Alt">
		<xsl:if test="@entry">
			<xsl:text>&#32;&#32;</xsl:text>
			<subentry>
				<xsl:copy-of select="@ws"/>
				<xsl:value-of select="@entry"/>
			</subentry>
			<xsl:text>&#13;&#10;</xsl:text>
		</xsl:if>
	</xsl:for-each>
<!-- is this only for stem-based dictionaries?  The following would be for root-based dictionary. -->
	<!--xsl:for-each select="LexEntryRefLink/Paragraph/LexEntryRefLink_OwningEntry">
		<xsl:call-template name="ProcessSubentry"/>
		</xsl:for-each-->
</xsl:template>


<!-- Output main entry (or sense) links. -->

<!-- Output the lex entry type. -->

<xsl:template match="_VisibleVariantEntryRefs">
	<xsl:for-each select="LexEntryRefLink">
		<xsl:for-each select="LexEntryRef_VariantEntryTypes/Link">
			<xsl:choose>
				<xsl:when test="Alt/@abbr">
					<xsl:text>&#32;&#32;</xsl:text>
					<variant-type-abbr>
						<xsl:attribute name="ws"><xsl:value-of select="Alt/@ws"/></xsl:attribute>
						<xsl:value-of select="Alt/@abbr"/>
					</variant-type-abbr>
					<xsl:text>&#13;&#10;</xsl:text>
				</xsl:when>
				<xsl:when test="Alt/@name">
					<xsl:text>&#32;&#32;</xsl:text>
					<variant-type>
						<xsl:attribute name="ws"><xsl:value-of select="Alt/@ws"/></xsl:attribute>
						<xsl:value-of select="Alt/@name"/>
					</variant-type>
					<xsl:text>&#13;&#10;</xsl:text>
				</xsl:when>
			</xsl:choose>
		</xsl:for-each>
		<xsl:for-each select="LexEntryRef_ComponentLexemes/Link">
			<xsl:choose>
				<xsl:when test="Alt/@entry">
					<xsl:text>&#32;&#32;</xsl:text>
					<variant-component>
						<xsl:attribute name="ws"><xsl:value-of select="Alt/@ws"/></xsl:attribute>
						<xsl:value-of select="Alt/@entry"/>
					</variant-component>
					<xsl:text>&#13;&#10;</xsl:text>
				</xsl:when>
				<xsl:when test="Alt/@sense">
					<xsl:text>&#32;&#32;</xsl:text>
					<variant-component>
						<xsl:attribute name="ws"><xsl:value-of select="Alt/@ws"/></xsl:attribute>
						<xsl:value-of select="Alt/@sense"/>
					</variant-component>
					<xsl:text>&#13;&#10;</xsl:text>
				</xsl:when>
			</xsl:choose>
		</xsl:for-each>
	</xsl:for-each>
</xsl:template>
<xsl:template match="LexEntry_EntryRefs">
  <xsl:for-each select="LexEntryRef">
	<xsl:for-each select="LexEntryRef_ComplexEntryTypes/Link">
	  <xsl:choose>
		<xsl:when test="Alt/@abbr">
			<xsl:text>&#32;&#32;</xsl:text>
			<complex-form-type-abbr>
				<xsl:attribute name="ws"><xsl:value-of select="Alt/@ws"/></xsl:attribute>
				<xsl:value-of select="Alt/@abbr"/>
			</complex-form-type-abbr>
			<xsl:text>&#13;&#10;</xsl:text>
		</xsl:when>
		<xsl:when test="Alt/@name">
			<xsl:text>&#32;&#32;</xsl:text>
			<complex-form-type>
				<xsl:attribute name="ws"><xsl:value-of select="Alt/@ws"/></xsl:attribute>
				<xsl:value-of select="Alt/@name"/>
			</complex-form-type>
			<xsl:text>&#13;&#10;</xsl:text>
		</xsl:when>
	  </xsl:choose>
	</xsl:for-each>
	<xsl:for-each select="LexEntryRef_VariantEntryTypes/Link">
	  <xsl:choose>
		<xsl:when test="Alt/@abbr">
			<xsl:text>&#32;&#32;</xsl:text>
			<variant-type-abbr>
				<xsl:attribute name="ws"><xsl:value-of select="Alt/@ws"/></xsl:attribute>
				<xsl:value-of select="Alt/@abbr"/>
			</variant-type-abbr>
			<xsl:text>&#13;&#10;</xsl:text>
		</xsl:when>
		<xsl:when test="Alt/@name">
			<xsl:text>&#32;&#32;</xsl:text>
			<variant-type>
				<xsl:attribute name="ws"><xsl:value-of select="Alt/@ws"/></xsl:attribute>
				<xsl:value-of select="Alt/@name"/>
			</variant-type>
			<xsl:text>&#13;&#10;</xsl:text>
		</xsl:when>
	  </xsl:choose>
	</xsl:for-each>
	<xsl:for-each select="LexEntryRef_ComponentLexemes/Link">
	  <xsl:choose>
		<xsl:when test="Alt/@entry">
			<xsl:text>&#32;&#32;</xsl:text>
			<entry-crossref>
				<xsl:attribute name="ws"><xsl:value-of select="Alt/@ws"/></xsl:attribute>
				<xsl:value-of select="Alt/@entry"/>
			</entry-crossref>
			<xsl:text>&#13;&#10;</xsl:text>
		</xsl:when>
		<xsl:when test="Alt/@sense">
			<xsl:text>&#32;&#32;</xsl:text>
			<entry-crossref>
				<xsl:attribute name="ws"><xsl:value-of select="Alt/@ws"/></xsl:attribute>
				<xsl:value-of select="Alt/@sense"/>
			</entry-crossref>
			<xsl:text>&#13;&#10;</xsl:text>
		</xsl:when>
	  </xsl:choose>
	</xsl:for-each>
  </xsl:for-each>
</xsl:template>


<!-- Output minor entry (variant) links. -->

<xsl:template match="_VariantFormEntryBackRefs">
  <xsl:for-each select="LexEntryRefLink">
	<xsl:for-each select="LexEntryRefLink_OwningEntry/Link/Alt">
	  <xsl:if test="@entry">
		  <xsl:text>&#32;&#32;</xsl:text>
		  <variant>
			  <xsl:copy-of select="@ws"/>
			  <xsl:value-of select="@entry"/>
		  </variant>
		  <xsl:text>&#13;&#10;</xsl:text>
	  </xsl:if>
	</xsl:for-each>
	<xsl:for-each select="LexEntryRef_VariantEntryTypes/Link/Alt">
		<xsl:text>&#32;&#32;</xsl:text>
		<variant-abbr>
			<xsl:copy-of select="@ws"/>
			<xsl:value-of select="@revabbr"/>
		</variant-abbr>
		<xsl:text>&#13;&#10;</xsl:text>
	</xsl:for-each>
  </xsl:for-each>
</xsl:template>


<!-- Output the sense number and part of speech for each LexSense. -->

<xsl:template match="LexSense">
	<xsl:text>&#32;&#32;</xsl:text>
	<sense>
		<xsl:attribute name="num">
			<xsl:number format="1.1.1" level="multiple" count="LexSense"/>
		</xsl:attribute>
		<xsl:text>&#13;&#10;</xsl:text>
		<xsl:apply-templates/>
		<xsl:text>&#32;&#32;</xsl:text>
	</sense>
	<xsl:text>&#13;&#10;</xsl:text>
</xsl:template>

<!-- already handled above, so ignore contents. -->

<xsl:template match="MoStemMsa_PartOfSpeech"/>
<xsl:template match="FsFeatStruc"/>
<xsl:template match="MoInflAffMsa_PartOfSpeech"/>
<xsl:template match="MoDerivStepMSA_PartOfSpeech"/>
<xsl:template match="MoUnclassifiedAffixMsa_PartOfSpeech"/>
<xsl:template match="MoDerivAffMsa_FromPartOfSpeech"/>
<xsl:template match="MoDerivAffMsa_ToPartOfSpeech"/>


<xsl:template match="MoMorphSynAnalysisLink_MLPartOfSpeech">
	<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
	<part-of-speech>
		<xsl:attribute name="ws"><xsl:value-of select="AStr/@ws"/></xsl:attribute>
		<xsl:value-of select="AStr/Run"/>
	</part-of-speech>
	<xsl:text>&#13;&#10;</xsl:text>
</xsl:template>

<xsl:template match="MoInflAffMsa_Slots">
	<xsl:for-each select="MoInflAffixSlotLink">
		<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
		<slot>
			<xsl:attribute name="ws">
				<xsl:value-of select="MoInflAffixSlot_Name/AStr/@ws"/>
			</xsl:attribute>
			<xsl:value-of select="MoInflAffixSlot_Name/AStr/Run"/>
		</slot>
		<xsl:text>&#13;&#10;</xsl:text>
	</xsl:for-each>
</xsl:template>

<xsl:template match="MoMorphSynAnalysisLink_MLInflectionClass">
	<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
	<inflection-class>
		<xsl:attribute name="ws"><xsl:value-of select="AStr/@ws"/></xsl:attribute>
		<xsl:value-of select="AStr/Run"/>
	</inflection-class>
	<xsl:text>&#13;&#10;</xsl:text>
</xsl:template>

<!-- Output the gloss. -->

<xsl:template match="LexSense_Gloss">
	<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
	<gloss>
		<xsl:attribute name="ws"><xsl:value-of select="AStr/@ws"/></xsl:attribute>
		<xsl:value-of select="AStr/Run"/>
	</gloss>
	<xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output the definition. -->

<xsl:template match="LexSense_Definition">
	<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
	<definition>
		<xsl:attribute name="ws"><xsl:value-of select="AStr/@ws"/></xsl:attribute>
		<xsl:value-of select="AStr/Run"/>
	</definition>
	<xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output the semantic domains. -->

<xsl:template match="LexSense_SemanticDomains">
	<xsl:for-each select="Link/Alt">
		<xsl:if test="@abbr">
			<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
			<sem-domain-abbr>
				<xsl:copy-of select="@ws"/>
				<xsl:value-of select="@abbr"/>
			</sem-domain-abbr>
			<xsl:text>&#13;&#10;</xsl:text>
		</xsl:if>
		<xsl:if test="@name">
			<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
			<sem-domain>
				<xsl:copy-of select="@ws"/>
				<xsl:value-of select="@name"/>
			</sem-domain>
			<xsl:text>&#13;&#10;</xsl:text>
		</xsl:if>
	</xsl:for-each>
</xsl:template>


<!-- Output the anthropology codes. -->

<xsl:template match="LexSense_AnthroCodes">
	<xsl:for-each select="Link/Alt">
		<xsl:if test="@abbr">
			<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
			<anth-code-abbr>
				<xsl:copy-of select="@ws"/>
				<xsl:value-of select="@abbr"/>
			</anth-code-abbr>
			<xsl:text>&#13;&#10;</xsl:text>
		</xsl:if>
		<xsl:if test="@name">
			<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
			<anth-code>
				<xsl:copy-of select="@ws"/>
				<xsl:value-of select="@name"/>
			</anth-code>
			<xsl:text>&#13;&#10;</xsl:text>
		</xsl:if>
	</xsl:for-each>
</xsl:template>


<!-- Output the domain types. -->

<xsl:template match="LexSense_DomainTypes">
	<xsl:for-each select="Link/Alt">
		<xsl:if test="@abbr">
			<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
			<domain-abbr>
				<xsl:copy-of select="@ws"/>
				<xsl:value-of select="@abbr"/>
			</domain-abbr>
			<xsl:text>&#13;&#10;</xsl:text>
		</xsl:if>
		<xsl:if test="@name">
			<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
			<domain>
				<xsl:copy-of select="@ws"/>
				<xsl:value-of select="@name"/>
			</domain>
			<xsl:text>&#13;&#10;</xsl:text>
		</xsl:if>
	</xsl:for-each>
</xsl:template>


<!-- Output the usage types. -->

<xsl:template match="LexSense_UsageTypes">
	<xsl:for-each select="Link/Alt">
		<xsl:if test="@abbr">
			<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
			<usage-abbr>
				<xsl:copy-of select="@ws"/>
				<xsl:value-of select="@abbr"/>
			</usage-abbr>
			<xsl:text>&#13;&#10;</xsl:text>
		</xsl:if>
		<xsl:if test="@name">
			<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
			<usage>
				<xsl:copy-of select="@ws"/>
				<xsl:value-of select="@name"/>
			</usage>
			<xsl:text>&#13;&#10;</xsl:text>
		</xsl:if>
	</xsl:for-each>
</xsl:template>


<!-- Output the status. -->

<xsl:template match="LexSense_Status">
	<xsl:for-each select="Link/Alt">
		<xsl:choose>
			<xsl:when test="@name">
				<xsl:text>    &lt;status ws=&quot;</xsl:text>
				<xsl:value-of select="@ws"/>
				<xsl:text>&quot;>&#32;</xsl:text>
				<xsl:value-of select="@name"/>
				<xsl:text>&lt;/status>&#13;&#10;</xsl:text>
			</xsl:when>
			<xsl:when test="@abbr">
				<xsl:text>     &lt;status-abbr ws=&quot;</xsl:text>
				<xsl:value-of select="@ws"/>
				<xsl:text>&quot;>&#32;</xsl:text>
				<xsl:value-of select="@abbr"/>
				<xsl:text>&lt;/status-abbr&#13;&#10;</xsl:text>
			</xsl:when>
		</xsl:choose>
	</xsl:for-each>
</xsl:template>



<!-- Output a vernacular example sentence. -->

<xsl:template match="LexExampleSentence_Example">
	<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
	<example>
		<xsl:attribute name="ws"><xsl:value-of select="AStr/@ws"/></xsl:attribute>
		<xsl:value-of select="AStr/Run"/>
	</example>
	<xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output a translation of an example sentence. -->

<xsl:template match="CmTranslation_Translation">
	<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
	<translation>
		<xsl:attribute name="ws"><xsl:value-of select="AStr/@ws"/></xsl:attribute>
		<xsl:value-of select="AStr/Run"/>
	</translation>
	<xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output the encyclopedic information. -->

<xsl:template match="LexSense_EncyclopedicInfo">
	<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
	<enc-info>
		<xsl:attribute name="ws"><xsl:value-of select="AStr/@ws"/></xsl:attribute>
		<xsl:value-of select="AStr/Run"/>
	</enc-info>
	<xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output the restrictions. -->

<xsl:template match="LexSense_Restrictions">
	<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
	<restrictions>
		<xsl:attribute name="ws"><xsl:value-of select="AStr/@ws"/></xsl:attribute>
		<xsl:value-of select="AStr/Run"/>
	</restrictions>
	<xsl:text>&#13;&#10;</xsl:text>
</xsl:template>

<!-- Output the anthropology notes. -->

<xsl:template match="LexSense_AnthroNote">
	<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
	<anthro-note>
		<xsl:attribute name="ws"><xsl:value-of select="AStr/@ws"/></xsl:attribute>
		<xsl:value-of select="AStr/Run"/>
	</anthro-note>
	<xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output the bibliography information. -->

<xsl:template match="LexEntry_Bibliography">
	<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
	<bibliography>
		<xsl:attribute name="ws"><xsl:value-of select="AStr/@ws"/></xsl:attribute>
		<xsl:value-of select="AStr/Run"/>
	</bibliography>
	<xsl:text>&#13;&#10;</xsl:text>
</xsl:template>
<xsl:template match="LexSense_Bibliography">
	<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
	<bibliography>
		<xsl:attribute name="ws"><xsl:value-of select="AStr/@ws"/></xsl:attribute>
		<xsl:value-of select="AStr/Run"/>
	</bibliography>
	<xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output the general notes. -->

<xsl:template match="LexSense_GeneralNote">
	<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
	<general-note>
		<xsl:attribute name="ws"><xsl:value-of select="AStr/@ws"/></xsl:attribute>
		<xsl:value-of select="AStr/Run"/>
	</general-note>
	<xsl:text>&#13;&#10;</xsl:text>
</xsl:template>

<!-- Output the discourse notes. -->

<xsl:template match="LexSense_DiscourseNote">
	<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
	<discourse-note>
		<xsl:attribute name="ws"><xsl:value-of select="AStr/@ws"/></xsl:attribute>
		<xsl:value-of select="AStr/Run"/>
	</discourse-note>
	<xsl:text>&#13;&#10;</xsl:text>
</xsl:template>
<!-- Output the phonology notes. -->

<xsl:template match="LexSense_PhonologyNote">
	<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
	<phonology-note>
		<xsl:attribute name="ws"><xsl:value-of select="AStr/@ws"/></xsl:attribute>
		<xsl:value-of select="AStr/Run"/>
	</phonology-note>
	<xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output the semantics notes. -->

<xsl:template match="LexSense_SemanticsNote">
	<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
	<semantic-note>
		<xsl:attribute name="ws"><xsl:value-of select="AStr/@ws"/></xsl:attribute>
		<xsl:value-of select="AStr/Run"/>
	</semantic-note>
	<xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output the sociolinguistics notes. -->

<xsl:template match="LexSense_SocioLinguisticsNote">
	<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
	<socio-note>
		<xsl:attribute name="ws"><xsl:value-of select="AStr/@ws"/></xsl:attribute>
		<xsl:value-of select="AStr/Run"/>
	</socio-note>
	<xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output the literal meaning. -->

<xsl:template match="LexEntry_LiteralMeaning">
	<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
	<literal>
		<xsl:attribute name="ws"><xsl:value-of select="AStr/@ws"/></xsl:attribute>
		<xsl:value-of select="AStr/Run"/>
	</literal>
	<xsl:text>&#13;&#10;</xsl:text>
</xsl:template>

<!-- Output a comment (note). -->

<xsl:template match="LexEntry_Comment">
	<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
	<comment>
		<xsl:attribute name="ws"><xsl:value-of select="AStr/@ws"/></xsl:attribute>
		<xsl:value-of select="AStr/Run"/>
	</comment>
	<xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output the scientific name. -->

<xsl:template match="LexSense_ScientificName">
	<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
	<sci-name>
		<xsl:value-of select="Str/Run"/>
	</sci-name>
	<xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output the source information. -->

<xsl:template match="LexSense_Source">
	<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
	<source>
		<xsl:value-of select="Str/Run"/>
	</source>
	<xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Output the lexical references. -->

<xsl:template match="LexReferenceLink">
	<xsl:choose>
		<xsl:when test="LexReferenceLink_Type/Link/Alt/@abbr">
			<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
			<lex-function-abbr>
				<xsl:attribute name="ws">
					<xsl:value-of select="LexReferenceLink_Type/Link/Alt/@ws"/>
				</xsl:attribute>
				<xsl:value-of select="LexReferenceLink_Type/Link/Alt/@abbr"/>
			</lex-function-abbr>
			<xsl:text>&#13;&#10;</xsl:text>
		</xsl:when>
		<xsl:when test="LexReferenceLink_Type/Link/Alt/@name">
			<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
			<lex-function>
				<xsl:value-of select="LexReferenceLink_Type/Link/Alt/@name"/>
			</lex-function>
			<xsl:text>&#13;&#10;</xsl:text>
		</xsl:when>
		<xsl:when test="LexReferenceLink_Type/Link/Alt/@revabbr">
			<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
			<lex-function-rev-abbr>
				<xsl:value-of select="LexReferenceLink_Type/Link/Alt/@revabbr"/>
			</lex-function-rev-abbr>
			<xsl:text>&#13;&#10;</xsl:text>
		</xsl:when>
		<xsl:when test="LexReferenceLink_Type/Link/Alt/@revname">
			<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
			<lex-function-rev>
				<xsl:value-of select="LexReferenceLink_Type/Link/Alt/@revname"/>
			</lex-function-rev>
			<xsl:text>&#13;&#10;</xsl:text>
		</xsl:when>
	</xsl:choose>
	<xsl:for-each select="LexReference_Targets/Link">
		<xsl:choose>
			<xsl:when test="Alt/@entry">
				<xsl:text>&#32;&#32;</xsl:text>
				<lex-ref>
					<xsl:attribute name="ws"><xsl:value-of select="Alt/@ws"/></xsl:attribute>
					<xsl:value-of select="Alt/@entry"/>
				</lex-ref>
				<xsl:text>&#13;&#10;</xsl:text>
			</xsl:when>
			<xsl:when test="Alt/@sense">
				<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
				<lex-ref>
					<xsl:attribute name="ws"><xsl:value-of select="Alt/@ws"/></xsl:attribute>
					<xsl:value-of select="Alt/@sense"/>
				</lex-ref>
				<xsl:text>&#13;&#10;</xsl:text>
			</xsl:when>
		</xsl:choose>
	</xsl:for-each>
</xsl:template>


<!-- Output the picture file path and caption -->

<xsl:template match="CmPicture">
  <xsl:choose>
	<xsl:when test="CmPicture_InternalFilePath">
		<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
		<IntPicturePath>
			<xsl:value-of select="CmPicture_InternalFilePath"/>
		</IntPicturePath>
		<xsl:text>&#13;&#10;</xsl:text>
	</xsl:when>
	<xsl:when test="CmPicture_OriginalFilePath">
		<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
		<ExtPicturePath>
			<xsl:value-of select="CmPicture_OriginalFilePath"/>
		</ExtPicturePath>
		<xsl:text>&#13;&#10;</xsl:text>
	</xsl:when>
  </xsl:choose>
  <xsl:apply-templates/>
</xsl:template>
<xsl:template match="CmPicture_InternalFilePath"/>
<xsl:template match="CmPicture_OriginalFilePath"/>
<xsl:template match="CmPicture_Caption">
	<xsl:text>&#32;&#32;&#32;&#32;</xsl:text>
	<caption>
		<xsl:attribute name="ws">
			<xsl:value-of select="AStr/@ws"/>
		</xsl:attribute>
		<xsl:value-of select="AStr/Run"/>
	</caption>
	<xsl:text>&#13;&#10;</xsl:text>
</xsl:template>
<xsl:template match="CmPictureLink_SenseNumberTSS"/>

<!-- Output the date modified. -->

<xsl:template match="LexEntry_DateModified">
	<xsl:text>&#32;&#32;</xsl:text>
	<date-mod>
		<xsl:value-of select="Str/Run"/>&#13;&#10;
	</date-mod>
	<xsl:text>&#13;&#10;</xsl:text>
</xsl:template>

<xsl:template match="LexEntry_Pronunciations"/>

<xsl:template match="LexEntry_Etymology">
	<xsl:text>&#32;&#32;</xsl:text>
	<etymology>
		<xsl:text>&#13;&#10;&#32;&#32;&#32;&#32;</xsl:text>
		<form>
			<xsl:attribute name="ws">
				<xsl:value-of select="LexEtymology/LexEtymology_Form/AStr/@ws"/>
			</xsl:attribute>
			<xsl:value-of select="LexEtymology/LexEtymology_Form/AStr/Run"/>
		</form>
		<xsl:text>&#13;&#10;&#32;&#32;&#32;&#32;</xsl:text>
		<gloss>
			<xsl:attribute name="ws">
				<xsl:value-of select="LexEtymology/LexEtymology_Gloss/AStr/@ws"/>
			</xsl:attribute>
			<xsl:value-of select="LexEtymology/LexEtymology_Gloss/AStr/Run"/>
		</gloss>
		<xsl:text>&#13;&#10;&#32;&#32;&#32;&#32;</xsl:text>
		<comment>
			<xsl:attribute name="ws">
				<xsl:value-of select="LexEtymology/LexEtymology_Comment/AStr/@ws"/>
			</xsl:attribute>
			<xsl:value-of select="LexEtymology/LexEtymology_Comment/AStr/Run"/>
		</comment>
		<xsl:text>&#13;&#10;&#32;&#32;</xsl:text>
	</etymology>
	<xsl:text>&#13;&#10;</xsl:text>
</xsl:template>

<!-- Output the date created. -->

<xsl:template match="LexEntry_DateCreated">
	<xsl:text>&#32;&#32;</xsl:text>
	<date-created>
		<xsl:value-of select="Str/Run"/>
	</date-created>
	<xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- Handle a custom field. -->

<xsl:template name="ProcessCustomElement">
	<xsl:param name="TagNumber"/>
	<xsl:param name="TagLang"/>
	<xsl:text>&#32;&#32;</xsl:text>
	<custom>
		<xsl:attribute name="num"><xsl:value-of select="$TagNumber"/></xsl:attribute>
		<xsl:attribute name="ws"><xsl:value-of select="$TagLang"/></xsl:attribute>
		<xsl:apply-templates/>
		<xsl:text>&#32;&#32;</xsl:text>
	</custom>
	<xsl:text>&#13;&#10;</xsl:text>
</xsl:template>


<!-- This is the basic default processing. -->

<xsl:template match="*">
  <xsl:choose>
	<xsl:when test="contains(name(), '_custom')">
	  <xsl:call-template name="ProcessCustomElement">
		<xsl:with-param name="TagNumber" select="substring-after(name(), '_custom')"/>
		<xsl:with-param name="TagLang" select="Str/Run/@ws"/>
	  </xsl:call-template>
	</xsl:when>
	<xsl:otherwise>
	  <xsl:apply-templates/>
	</xsl:otherwise>
  </xsl:choose>
</xsl:template>


</xsl:stylesheet>
