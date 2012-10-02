<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:msxsl="urn:schemas-microsoft-com:xslt"
  xmlns:user="urn:my-scripts">
  <xsl:output method="xml" version="1.0" encoding="UTF-8" indent="yes"/>
  <!--
================================================================
Convert SFM Import XML phase 2 to XML phase 3
  Input:    SFM Import phase 2 XML
  Output: SFM Import phase 3 XML
================================================================
Revision History is at the end of this file.

- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Preamble
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->

  <msxsl:script language="C#" implements-prefix="user">
	 <![CDATA[

	// create a new GUID and return it
	public string CreateGUID()
	{
		System.Guid newGuid = Guid.NewGuid();
		return "I" + newGuid.ToString().ToUpper();
	}

	// Create a global hashtable for key - id pairs
	public static Hashtable IDvalues = new Hashtable();

	// Create a global hashtable for key - guid pairs
	public static Hashtable GUIDvalues = new Hashtable();

	// Get the next id for a given key
	public double nextKeyID(string name)
	{
		if (!IDvalues.ContainsKey(name))
			IDvalues.Add(name, (double)0);
		double nValue = (double)IDvalues[name];
		IDvalues[name] = ++nValue;
		return nValue;
	}

	public double FoundGUIDKey(string name)
	{
		if (!GUIDvalues.ContainsKey(name))
			return 1;
		return 0;
	}

	// Get the guid for a given key
	public string GetKeyGUID(string name)
	{
		if (!GUIDvalues.ContainsKey(name))
			GUIDvalues.Add(name, CreateGUID());

		return (string)GUIDvalues[name];
	}

	public static double ID = 1000;	// this is a global ID
	public double nextID()
	{
		return ID++;
	}

	// get the string rep for the affix type
	public string GetAffixType(string data, string affixMarker)
	{
		if (data.StartsWith(affixMarker) && data.EndsWith(affixMarker))
			return "infix";
		else if (data.StartsWith(affixMarker))
			return "suffix";
		else if (data.EndsWith(affixMarker))
			return "prefix";
		return "stem";
	}


	  ]]>
   </msxsl:script>

  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Main template
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <!-- Declare a variable to contian the affixMarker START -->
  <xsl:variable name="affixMarker">
	<xsl:choose>
	  <xsl:when test="@affixMarker">
		 <xsl:value-of select="@affixMarker"/>
	  </xsl:when>
	  <xsl:otherwise><xsl:text>-</xsl:text></xsl:otherwise>
	</xsl:choose>
  </xsl:variable>
  <!-- Declare a variable to contian the affixMarker END -->


  <xsl:template match="/dictionary">
<!--
  <?xml version="1.0" encoding="UTF-8"?>
  <!DOCTYPE LexDb SYSTEM "FwDatabase.dtd">
-->
<!--    <root>
	  <AdditionalFields>
		<CustomField name="custom12" class="LexEntry" type="Unicode" big="0" wsSelector="-1" userLabel="Created By" helpString="This is the person that created the entry."/>
	  </AdditionalFields>
-->

	  <LexDb>
		<Entries>
		  <xsl:for-each select="//entry">
			<xsl:call-template name="DoEntry"/>
		  </xsl:for-each>
		</Entries>
	  </LexDb>
<!--    </root> -->
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Inline elements
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
<!--
  <xsl:template match="//b | //vern | //es">
	<xsl:copy-of select="."/>
  </xsl:template>
-->
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoAllomorphs
	process allomorphs
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoAllomorphs">
	<Allomorphs>
	  <!-- see if the lex entry has the affixMarker at begin and end, begin, end or neither -->
	  <xsl:variable name="affixType"><xsl:value-of select="user:GetAffixType(lex, $affixMarker)"/></xsl:variable>
	  <xsl:variable name="allomorphName">
		<xsl:choose>
		  <xsl:when test="$affixType='stem'">MoStemAllomorph</xsl:when>
		  <xsl:otherwise>MoAffixAllomorph</xsl:otherwise>
		</xsl:choose>
	  </xsl:variable>
	  <xsl:element name="{$allomorphName}">
		<MorphType ws="en"><xsl:value-of select="$affixType"/></MorphType>
		  <!-- put out the Form element START -->
		  <xsl:choose>
			<xsl:when test="lex">
			  <xsl:for-each select="lex">
				<Form ws="{@ws}"><xsl:value-of select="."/></Form>
			  </xsl:for-each>
			</xsl:when>
			<xsl:otherwise>
			  <xsl:for-each select="cit">
				<Form ws="{@ws}"><xsl:value-of select="."/></Form>
			  </xsl:for-each>
			</xsl:otherwise>
		  </xsl:choose>
		  <!-- put out the Form element END -->
	  </xsl:element>
	</Allomorphs>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoAnthroCodes
	process AnthroCodes
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoAnthroCodes">
	<xsl:if test="anth">
	  <AnthroCodes>
		<xsl:for-each select="anth">
		  <!-- variable to become either 'name' or 'abbr' depending on the attributes -->
		  <xsl:variable name="aORn"><xsl:call-template name="abbrORname"/></xsl:variable>
		  <Link ws="{@ws}">
			<xsl:attribute name="{$aORn}"><xsl:value-of select="."/></xsl:attribute>
		  </Link>
		</xsl:for-each>
	  </AnthroCodes>
	</xsl:if>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoCitationForm
	process a citation form
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoCitationForm">
	<xsl:choose>
	  <xsl:when test="cit">
		<xsl:for-each select="cit">
		  <CitationForm ws="{@ws}"><xsl:value-of select="."/></CitationForm>
		</xsl:for-each>
	  </xsl:when>
	  <xsl:otherwise>
		<xsl:for-each select="lex">
		  <CitationForm ws="{@ws}"><xsl:value-of select="."/></CitationForm>
		</xsl:for-each>
	  </xsl:otherwise>
	</xsl:choose>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoDateCreated
	process a date created element
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoDateCreated">
	<xsl:if test="creat">
	  <xsl:element name="DateCreated">
		<xsl:value-of select="creat"/>
	  </xsl:element>
	</xsl:if>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoHomographNumber
	process a
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoHomographNumber">
	<xsl:if test="hom">
	  <HomographNumber><xsl:value-of select="hom"/></HomographNumber>
	</xsl:if>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoDateModified
	process a date modified element
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoDateModified">
	<xsl:if test="mod">
	  <DateModified>
		<xsl:value-of select="mod"/>
	  </DateModified>
	</xsl:if>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoBibliography
	process bibliographys
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoBibliography">
	<xsl:param name="element"/>
	<xsl:for-each select="$element">
	  <xsl:element name="Bibliography">
		<xsl:attribute name="ws"><xsl:value-of select="@ws"/></xsl:attribute>
		<xsl:apply-templates/>
	  </xsl:element>
	</xsl:for-each>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoEntry
	process an entry's information
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoEntry">
	<LexMajorEntry>
	  <xsl:call-template name="DoCitationForm"/>
	  <xsl:call-template name="DoAllomorphs"/>
	  <xsl:call-template name="DoDateCreated"/>
	  <xsl:call-template name="DoHomographNumber"/>
	  <xsl:call-template name="DoDateModified"/>
	  <xsl:call-template name="DoMSAs"/>

	  <!-- Restrictions -->
	  <!-- <xsl:call-template name="DoRestrictions"><xsl:with-param name="element" select="erest"/></xsl:call-template> -->
	  <xsl:call-template name="JoinOnWS">
		<xsl:with-param name="abbr" select="erest"/>					<!-- Abbr: element name to look for -->
		<xsl:with-param name="eNameOut">Restrictions</xsl:with-param>	<!-- eNameOut: element name to output -->
	  </xsl:call-template>
	  <!-- Bibliography -->
	  <!-- <xsl:call-template name="DoBibliography"><xsl:with-param name="element" select="ebib"/></xsl:call-template> -->
	  <xsl:call-template name="JoinOnWS">
		<xsl:with-param name="abbr" select="ebib"/>					<!-- Abbr: element name to look for -->
		<xsl:with-param name="eNameOut">Bibliography</xsl:with-param>	<!-- eNameOut: element name to output -->
	  </xsl:call-template>

	  <xsl:call-template name="DoSenses"/>
	</LexMajorEntry>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoExamples
	process examples
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoExamples">
	<xsl:if test="examp">
	  <Examples>
		<xsl:for-each select="examp">
		  <LexExampleSentence>
			<xsl:for-each select="sent">
			  <Example>
				<xsl:attribute name="ws"><xsl:value-of select="@ws"/></xsl:attribute>
				<xsl:apply-templates/>
			  </Example>
			</xsl:for-each>
			<xsl:if test="trans">
			  <Translations>
			  <xsl:for-each select="trans">
				<CmTranslation>
				  <xsl:attribute name="ws"><xsl:value-of select="@ws"/></xsl:attribute>
				  <xsl:apply-templates/>
				</CmTranslation>
			  </xsl:for-each>
			  </Translations>
			</xsl:if>
			<xsl:if test="./ref">
			  <Reference ws="{./ref/@ws}"><xsl:value-of select="./ref"/></Reference>
			</xsl:if>
		  </LexExampleSentence>
		</xsl:for-each>
	  </Examples>
	</xsl:if>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoScientificName
	process ScientificName
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoScientificName">
	<xsl:if test="sci">
	  <xsl:element name="ScientificName">
		<xsl:attribute name="ws"><xsl:value-of select="sci/@ws"/></xsl:attribute>
		<xsl:value-of select="sci"/>
		<!-- <xsl:apply-templates/> -->
	  </xsl:element>
	</xsl:if>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoSource
	process Source
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoSource">
	<xsl:if test="src">
	  <xsl:element name="Source">
		<xsl:attribute name="ws"><xsl:value-of select="src/@ws"/></xsl:attribute>
		<xsl:value-of select="src"/>
		<!-- <xsl:apply-templates/> -->
	  </xsl:element>
	</xsl:if>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
msaKeyName
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
<xsl:template name="msaKeyName">
  <xsl:value-of select="../../lex"/>
  <xsl:text>_</xsl:text>
  <xsl:value-of select="."/>
</xsl:template>

  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
abbrORname
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
<xsl:template name="abbrORname">
  <!-- if there is no abbr then use 'name', otherwise if abbr is false use 'name' else use 'abbr' -->
  <xsl:choose>
	<xsl:when test="not(@abbr)"><xsl:text>name</xsl:text></xsl:when>
	<xsl:when test="@abbr='False'"><xsl:text>name</xsl:text></xsl:when>
	<xsl:otherwise><xsl:text>abbr</xsl:text></xsl:otherwise>
  </xsl:choose>
</xsl:template>
<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
JoinOnWs
		Parameters: abbr: element name to look for
					eNameOut: element name to output

- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="JoinOnWS">
	<xsl:param name="abbr"/>		<!-- Abbr: element name to look for -->
	<xsl:param name="eNameOut"/>	<!-- eNameOut: element name to output -->
	<!-- <xsl:variable name="ename" select="glos"/> -->
	<!-- Create a variable that contains the sorted list of elements on the @ws -->
	<xsl:variable name="sortedNodes">
	  <xsl:for-each select="$abbr">
		<xsl:sort select="@ws"/>
		<xyz>	<!-- Create a new element that is consistantly named so it can be in the XPath -->
		  <xsl:copy-of select="@*"/>
		  <xsl:value-of select="."/>
		</xyz>
	  </xsl:for-each>
	</xsl:variable>
	<!-- Now use the 'sortedNodes' variable as our node-set -->
	<xsl:for-each select="msxsl:node-set($sortedNodes)/xyz[not(@ws=preceding-sibling::xyz[1]/@ws)]">
	  <xsl:element name="{$eNameOut}">
		<xsl:attribute name="ws"><xsl:value-of select="@ws"/></xsl:attribute>
		<xsl:value-of select="."/>
		<xsl:call-template name="join3"/>
	  </xsl:element>
	</xsl:for-each>
  </xsl:template>

  <!-- Recursive helper routine to concat all elements with the @ws = to the first one -->
  <xsl:template name="join3" >
	<xsl:variable name="ws" select="@ws"/>
	<xsl:for-each select="following-sibling::node()[1][@ws=$ws]">
	  <xsl:text>; </xsl:text><xsl:value-of select="."/>
	  <xsl:call-template name="join3"/>
	</xsl:for-each>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoLexicalRelations
	process Lexical Relations
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoLexicalRelations">
	<xsl:if test="func">
	  <LexicalRelations>
		<xsl:for-each select="func">
		  <Link>
			<xsl:attribute name="wsv"><xsl:value-of select="rel[2]/@ws"/></xsl:attribute>
			<xsl:attribute name="sense"><xsl:value-of select="rel[2]"/></xsl:attribute>
			<xsl:attribute name="wsa"><xsl:value-of select="rel[1]/@ws"/></xsl:attribute>
			<xsl:variable name="aORn"><xsl:call-template name="abbrORname"/></xsl:variable>
			<xsl:attribute name="{$aORn}"><xsl:value-of select="rel[1]"/></xsl:attribute>
			<!-- <xsl:attribute name="name"><xsl:value-of select="rel[1]"/></xsl:attribute> -->
		  </Link>
		</xsl:for-each>
	  </LexicalRelations>
	</xsl:if>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoMSAs
	process MSAs
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoMSAs">
	<xsl:if test="sense/pos">
	  <MorphoSyntaxAnalyses>
		<xsl:for-each select="sense/pos">
		  <xsl:variable name="key"><xsl:call-template name="msaKeyName"/></xsl:variable>
		  <xsl:if test="0!=user:FoundGUIDKey($key)">

			<!-- see if the lex entry has the affixMarker at begin and end, begin, end or neither -->
			<xsl:variable name="affixType"><xsl:value-of select="user:GetAffixType(../../lex, $affixMarker)"/></xsl:variable>
			<xsl:variable name="allomorphName">
			  <xsl:choose>
				<xsl:when test="$affixType='stem'">MoStemMsa</xsl:when>
				<xsl:otherwise>MoUnclassifiedAffixMsa</xsl:otherwise>
			  </xsl:choose>
			</xsl:variable>

			<xsl:element name="{$allomorphName}">
			  <xsl:attribute name="id"><xsl:value-of select="user:GetKeyGUID($key)"/></xsl:attribute>
			  <PartOfSpeech>
				<Link>
				  <xsl:attribute name="ws"><xsl:value-of select="@ws"/></xsl:attribute>
				  <!-- variable to become either 'name' or 'abbr' depending on the attributes -->
				  <xsl:variable name="aORn"><xsl:call-template name="abbrORname"/></xsl:variable>
				  <xsl:attribute name="{$aORn}"><xsl:value-of select="."/></xsl:attribute>
				</Link>
			  </PartOfSpeech>
			</xsl:element>

		  </xsl:if>
		</xsl:for-each>
	  </MorphoSyntaxAnalyses>
	</xsl:if>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoReversalEntries
	process Reversal Entries
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoReversalEntries">
	<xsl:if test="rev">
	  <ReversalEntries>
		<xsl:for-each select="rev">
		  <Link ws="{@ws}" form="{.}"/>
		</xsl:for-each>
	  </ReversalEntries>
	</xsl:if>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoSenseMSALinks
	process Sense MSAs
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoSenseMSALinks">
	<xsl:for-each select="pos">
	  <MorphoSyntaxAnalysis>
		<Link>
		  <xsl:variable name="key"><xsl:call-template name="msaKeyName"/></xsl:variable>
		  <xsl:attribute name="target"><xsl:value-of select="user:GetKeyGUID($key)"/></xsl:attribute>
		</Link>
	  </MorphoSyntaxAnalysis>
	</xsl:for-each>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoSenses
	process Senses
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoSenses">
	<Senses>
	  <xsl:for-each select="sense">
		<LexSense>
		  <!-- ************************************************************** -->
		  <!-- Handle all the LexSense MultiString elements -->

		  <!-- AnthroNote -->
		  <xsl:call-template name="DoLexSenseMultiString">
			<xsl:with-param name="abbrNodes" select="anote"/>
			<xsl:with-param name="elementName">AnthroNote</xsl:with-param>
			</xsl:call-template>
		  <!-- Bibliography -->
		  <xsl:call-template name="DoLexSenseMultiString">
			<xsl:with-param name="abbrNodes" select="sbib"/>
			<xsl:with-param name="elementName">Bibliography</xsl:with-param>
		  </xsl:call-template>
		  <!-- Definition -->
		  <xsl:call-template name="DoLexSenseMultiString">
			<xsl:with-param name="abbrNodes" select="def"/>
			<xsl:with-param name="elementName">Definition</xsl:with-param>
		  </xsl:call-template>
		  <!-- DiscourseNote -->
		  <xsl:call-template name="DoLexSenseMultiString">
			<xsl:with-param name="abbrNodes" select="dnote"/>
			<xsl:with-param name="elementName">DiscourseNote</xsl:with-param>
		  </xsl:call-template>
		  <!-- EncyclopedicInfo -->
		  <xsl:call-template name="DoLexSenseMultiString">
			<xsl:with-param name="abbrNodes" select="enc"/>
			<xsl:with-param name="elementName">EncyclopedicInfo</xsl:with-param>
		  </xsl:call-template>
		  <!-- GeneralNote -->
		  <xsl:call-template name="DoLexSenseMultiString">
			<xsl:with-param name="abbrNodes" select="gnote"/>
			<xsl:with-param name="elementName">GeneralNote</xsl:with-param>
		  </xsl:call-template>
		  <!-- GrammarNote -->
		  <xsl:call-template name="DoLexSenseMultiString">
			<xsl:with-param name="abbrNodes" select="grnote"/>
			<xsl:with-param name="elementName">GrammarNote</xsl:with-param>
		  </xsl:call-template>
		  <!-- PhonologyNote -->
		  <xsl:call-template name="DoLexSenseMultiString">
			<xsl:with-param name="abbrNodes" select="pnote"/>
			<xsl:with-param name="elementName">PhonologyNote</xsl:with-param>
		  </xsl:call-template>
		  <!-- SemanticsNote -->
		  <xsl:call-template name="DoLexSenseMultiString">
			<xsl:with-param name="abbrNodes" select="snote"/>
			<xsl:with-param name="elementName">SemanticsNote</xsl:with-param>
		  </xsl:call-template>
		  <!-- SocioLinguisticsNote -->
		  <xsl:call-template name="DoLexSenseMultiString">
			<xsl:with-param name="abbrNodes" select="slnote"/>
			<xsl:with-param name="elementName">SocioLinguisticsNote</xsl:with-param>
		  </xsl:call-template>

		  <!-- ************************************************************** -->
		  <!-- Handle all the LexSense MultiUnicode elements -->

		  <!-- Gloss -->
		  <xsl:call-template name="JoinOnWS">
			<xsl:with-param name="abbr" select="glos"/>				<!-- Abbr: element name to look for -->
			<xsl:with-param name="eNameOut">Gloss</xsl:with-param>	<!-- eNameOut: element name to output -->
		  </xsl:call-template>
		  <!-- Restrictions -->
		  <xsl:call-template name="JoinOnWS">
			<xsl:with-param name="abbr" select="srest"/>				<!-- Abbr: element name to look for -->
			<xsl:with-param name="eNameOut">Restrictions</xsl:with-param>	<!-- eNameOut: element name to output -->
		  </xsl:call-template>

		  <!-- ************************************************************** -->
		  <!-- Handle all the LexSense CmPossibility elements -->

		  <xsl:call-template name="DoLexSenseCmPossibility">
			<xsl:with-param name="abbrNodes" select="dom"/>
			<xsl:with-param name="elementName">DomainTypes</xsl:with-param>
		  </xsl:call-template>
		  <xsl:call-template name="DoLexSenseCmPossibility">
			<xsl:with-param name="abbrNodes" select="styp"/>
			<xsl:with-param name="elementName">SenseType</xsl:with-param>
		  </xsl:call-template>
		  <xsl:call-template name="DoLexSenseCmPossibility">
			<xsl:with-param name="abbrNodes" select="stat"/>
			<xsl:with-param name="elementName">Status</xsl:with-param>
		  </xsl:call-template>
		  <xsl:call-template name="DoLexSenseCmPossibility">
			<xsl:with-param name="abbrNodes" select="utyp"/>
			<xsl:with-param name="elementName">UsageTypes</xsl:with-param>
		  </xsl:call-template>

		  <!-- ************************************************************** -->
		  <!-- Do the remaining -->

		  <xsl:call-template name="DoAnthroCodes"/>
		  <xsl:call-template name="DoSenseMSALinks"/>
		  <xsl:call-template name="DoExamples"/>
		  <xsl:call-template name="DoLexicalRelations"/>
		  <xsl:call-template name="DoReversalEntries"/>
		  <xsl:call-template name="DoScientificName"/>
		  <xsl:call-template name="DoSource"/>
		  <xsl:call-template name="DoSemanticDomains"/>
		  <!-- how deal with custom fields?? -->
		</LexSense>
	  </xsl:for-each>
	</Senses>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoSemanticDomains
	process SemanticDomains
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoSemanticDomains">
	<xsl:if test="sem">
	  <xsl:element name="SemanticDomains">
	  <xsl:for-each select="sem">
		<xsl:element name="Link">
		  <xsl:attribute name="ws"><xsl:value-of select="@ws"/></xsl:attribute>
		  <xsl:variable name="aORn"><xsl:call-template name="abbrORname"/></xsl:variable>
		  <xsl:attribute name="{$aORn}"><xsl:value-of select="."/></xsl:attribute>
		</xsl:element>
	  </xsl:for-each>
	  </xsl:element>
	</xsl:if>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoRestrictions
	process Restrictions
		Parameters: element name
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoRestrictions">
	<xsl:param name="element"/>
	<xsl:for-each select="$element">
	  <Restrictions ws="{@ws}"><xsl:value-of select="."/></Restrictions>
	</xsl:for-each>
  </xsl:template>


  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoLexSenseMultiString
	process Restrictions
		Parameters: abbr		'anote'
					elementName	'AnthroNote'
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoLexSenseMultiString">
	<xsl:param name="abbrNodes"/>
	<xsl:param name="elementName"/>
	<!-- Treat Phase 3 MultiStirng and MultiUnicode the same: combine like @ws elements -->
	<xsl:call-template name="JoinOnWS">
	  <xsl:with-param name="abbr" select="$abbrNodes"/>			<!-- Abbr: element name to look for -->
	  <xsl:with-param name="eNameOut" select="$elementName"/>	<!-- eNameOut: element name to output -->
	</xsl:call-template>

<!--    PREVIOUS way of handling all MultiStrings

	<xsl:for-each select="$abbrNodes">
	  <xsl:element name="{$elementName}">
		<xsl:attribute name="ws"><xsl:value-of select="@ws"/></xsl:attribute>
		<xsl:value-of select="."/>
	  </xsl:element>
	</xsl:for-each>
-->
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoLexSenseCmPossibility
	process Restrictions
		Parameters: abbrNodes
					elementName
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoLexSenseCmPossibility">
	<xsl:param name="abbrNodes"/>
	<xsl:param name="elementName"/>
	<xsl:if test="$abbrNodes">
	  <xsl:element name="{$elementName}">
		<xsl:for-each select="$abbrNodes">
		  <!-- variable to become either 'name' or 'abbr' depending on the attributes -->
		  <xsl:variable name="aORn"><xsl:call-template name="abbrORname"/></xsl:variable>
		  <Link ws="{@ws}">
			<xsl:attribute name="{$aORn}"><xsl:value-of select="."/></xsl:attribute>
		  </Link>
		</xsl:for-each>
	  </xsl:element>
	</xsl:if>
  </xsl:template>

</xsl:stylesheet>
<!--
================================================================
Revision History
- - - - - - - - - - - - - - - - - - -
03-Mar-2005    Andy Black  Began working on Initial Draft
xx-Mar-2005    dlh - adding functionality...
xx-Apr-2005    dlh - adding functionality & generic templates.
================================================================
 -->
