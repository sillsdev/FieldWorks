<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="xml" version="1.0" encoding="utf-8" indent="yes"/>

<!-- This stylesheet works on the output of Language Explorer to produce an XML file containing
	 a reasonable representation of a reversal index. -->


<!-- Remove unwanted element levels. -->

<xsl:template match="Paragraph|ReversalIndexEntry_Hvo|LexSenseLink_OwningEntry">
  <xsl:apply-templates/>
</xsl:template>


<!-- Remove LiteralString elements entirely. -->

<xsl:template match="LiteralString"/>


<!-- Modify the ReversalIndexEntry_ReversalForm element to contain AUni elements instead of
	 AStr elements. -->

<xsl:template match="ReversalIndexEntry_ReversalForm">
  <xsl:copy>
	<xsl:text>&#13;&#10;            </xsl:text>
	<AUni>
	  <xsl:attribute name="ws"><xsl:value-of select="AStr/@ws"/></xsl:attribute>
	  <xsl:value-of select="AStr/Run"/>
	</AUni>
	<xsl:text>&#13;&#10;        </xsl:text>
  </xsl:copy>
</xsl:template>


<!-- Modify the ReversalIndexEntry_OtherWsFormsTSS element to become a
	 ReversalIndexEntry_RelatedForm element, and change the Str element to an AUni element. -->

<xsl:template match="ReversalIndexEntry_OtherWsFormsTSS">
  <ReversalIndexEntry_RelatedForm>
	<xsl:for-each select="Str/Run">
	  <xsl:if test="not(string(.)=' [' or string(.)=', ' or string(.)=']' or string(.)='')">
		<AUni>
		  <xsl:attribute name="ws">
			<xsl:value-of select="@ws"/>
		  </xsl:attribute>
		  <xsl:value-of select="."/>
		</AUni>
	  </xsl:if>
	</xsl:for-each>
  </ReversalIndexEntry_RelatedForm>
</xsl:template>


<!-- Standard template for handling Link type elements.
	 Merge child $NameNode/AStr and $AbbrNode/AStr elements into a single Link element with
	 embedded Alt elements handling possible multilingual references. -->

<xsl:template name="ProcessLinkElement">
  <xsl:param name="AbbrNode"/>
  <xsl:param name="NameNode"/>
  <xsl:param name="AttrPrefix"/>
	<xsl:text>&#13;&#10;            </xsl:text>
  <Link>
	<xsl:for-each select="$AbbrNode/AStr">
	  <xsl:variable name="wsAbbr">
		<xsl:value-of select="@ws"/>
	  </xsl:variable>
	  <Alt>
		<xsl:attribute name="ws">
		  <xsl:value-of select="@ws"/>
		</xsl:attribute>
		<xsl:attribute name="{$AttrPrefix}abbr">
		  <xsl:value-of select="Run"/>
		</xsl:attribute>
		<xsl:for-each select="$NameNode/AStr[@ws=$wsAbbr]">
		  <xsl:attribute name="{$AttrPrefix}name">
			<xsl:value-of select="Run"/>
		  </xsl:attribute>
		</xsl:for-each>
	  </Alt>
	</xsl:for-each>
	<xsl:for-each select="$NameNode/AStr">
	  <xsl:variable name="wsName">
		<xsl:value-of select="@ws"/>
	  </xsl:variable>
	  <xsl:if test="not($AbbrNode/AStr[@ws=$wsName])">
		<Alt>
		  <xsl:attribute name="ws">
			<xsl:value-of select="@ws"/>
		  </xsl:attribute>
		  <xsl:attribute name="{$AttrPrefix}name">
			<xsl:value-of select="Run"/>
		  </xsl:attribute>
		</Alt>
	  </xsl:if>
	</xsl:for-each>
  </Link>
	<xsl:text>&#13;&#10;        </xsl:text>
</xsl:template>


<!-- Process the part of speech display into a link.  -->

<xsl:template match="ReversalIndexEntry_PartOfSpeech">
  <xsl:copy>
	<xsl:copy-of select="@*"/>
	<xsl:call-template name="ProcessLinkElement">
	  <xsl:with-param name="AbbrNode" select="PartOfSpeechLink/CmPossibility_Abbreviation"/>
	  <xsl:with-param name="NameNode" select="PartOfSpeechLink/CmPossibility_Name"/>
	  <xsl:with-param name="AttrPrefix"></xsl:with-param>
	</xsl:call-template>
  </xsl:copy>
</xsl:template>

<xsl:template match="LexSense_MorphoSyntaxAnalysis">
  <xsl:copy>
	<xsl:copy-of select="@*"/>
	<xsl:apply-templates/>
  </xsl:copy>
</xsl:template>

<xsl:template match="MoMorphSynAnalysisLink">
  <xsl:if test="MoMorphSynAnalysisLink_MLPartOfSpeech/AStr/Run">
	<PartOfSpeech>
	  <xsl:call-template name="ProcessLinkElement">
		<xsl:with-param name="AbbrNode" select="MoMorphSynAnalysisLink_MLPartOfSpeech"/>
		<xsl:with-param name="NameNode" select="ThereIsntAny"/>
		<xsl:with-param name="AttrPrefix"/>
	  </xsl:call-template>
	</PartOfSpeech>
  </xsl:if>
</xsl:template>

<!-- convert *Link to Link generically -->

<xsl:template match="CmSemanticDomainLink">
  <xsl:call-template name="ProcessLinkElement">
	<xsl:with-param name="AbbrNode" select="CmPossibility_Abbreviation"/>
	<xsl:with-param name="NameNode" select="CmPossibility_Name"/>
	<xsl:with-param name="AttrPrefix"></xsl:with-param>
  </xsl:call-template>
</xsl:template>
<xsl:template match="CmPossibilityLink">
  <xsl:call-template name="ProcessLinkElement">
	<xsl:with-param name="AbbrNode" select="CmPossibility_Abbreviation"/>
	<xsl:with-param name="NameNode" select="CmPossibility_Name"/>
	<xsl:with-param name="AttrPrefix"></xsl:with-param>
  </xsl:call-template>
</xsl:template>
<xsl:template match="CmLocationLink">
  <xsl:call-template name="ProcessLinkElement">
	<xsl:with-param name="AbbrNode" select="CmPossibility_Abbreviation"/>
	<xsl:with-param name="NameNode" select="CmPossibility_Name"/>
	<xsl:with-param name="AttrPrefix"></xsl:with-param>
  </xsl:call-template>
</xsl:template>

<!-- Change the _ReferringSenses element into a ReversalIndexEntry_ReferringSenses element. -->

<xsl:template match="_ReferringSenses">
  <ReversalIndexEntry_ReferringSenses>
	<xsl:copy-of select="@*"/>
	<xsl:apply-templates/>
  </ReversalIndexEntry_ReferringSenses>
</xsl:template>


<!-- Change the LexSenseLink element into a LexSense element. -->

<xsl:template match="LexSenseLink">
  <LexSense>
	<xsl:attribute name="id"><xsl:value-of select="@target"/></xsl:attribute>
	<xsl:apply-templates/>
  </LexSense>
</xsl:template>


<!-- process LexEntry_LexemeForm into a much simpler structure. -->

<xsl:template match="LexEntry_LexemeForm">
  <xsl:choose>
	<xsl:when test="not(MoForm/*)">
	</xsl:when>
	<xsl:when test="MoForm/MoForm_MorphType/MoMorphTypeLink">
	</xsl:when>
	<xsl:when test="preceding-sibling::LexEntry_LexemeForm[1]/MoForm and following-sibling::LexEntry_LexemeForm[1]/MoForm">
	  <LexEntry_Headword>
		<xsl:text>&#13;&#10;                        </xsl:text>
		  <xsl:if test="not(../LexEntry_LexemeForm/MoForm/MoForm_MorphType/MoMorphTypeLink/MoMorphType_Prefix/AUni='')">
			<MorphTypePrefix>
			  <xsl:copy-of select="../LexEntry_LexemeForm/MoForm/MoForm_MorphType/MoMorphTypeLink/MoMorphType_Prefix/AUni"/>
			</MorphTypePrefix>
		  </xsl:if>
		  <MorphForm>
			  <AUni>
				  <xsl:copy-of select="../LexEntry_LexemeForm/MoForm/MoForm_Form/AStr/@ws"/>
				  <xsl:value-of select="../LexEntry_LexemeForm/MoForm/MoForm_Form/AStr/Run"/>
			  </AUni>
		  </MorphForm>
		  <xsl:if test="not(../LexEntry_LexemeForm/MoForm/MoForm_MorphType/MoMorphTypeLink/MoMorphType_Postfix/AUni='')">
			<MorphTypePostfix>
			  <xsl:copy-of select="../LexEntry_LexemeForm/MoForm/MoForm_MorphType/MoMorphTypeLink/MoMorphType_Postfix/AUni"/>
			</MorphTypePostfix>
		  </xsl:if>
		<xsl:text>&#13;&#10;                    </xsl:text>
	  </LexEntry_Headword>
	</xsl:when>
	<xsl:otherwise>
	  <xsl:copy>
		<xsl:apply-templates/>
	  </xsl:copy>
	</xsl:otherwise>
  </xsl:choose>
</xsl:template>


<!-- change AStr/Run into AUni generically -->

<xsl:template match="AStr">
  <AUni><xsl:copy-of select="@*"/><xsl:value-of select="Run"/></AUni>
</xsl:template>
<xsl:template match="Str">
  <AUni><xsl:copy-of select="Run[1]/@ws"/><xsl:value-of select="Run"/></AUni>
</xsl:template>

<!-- This is the basic default processing. -->

<xsl:template match="*">
  <xsl:copy>
	<xsl:copy-of select="@*"/>
	<xsl:apply-templates/>
  </xsl:copy>
</xsl:template>

</xsl:stylesheet>
