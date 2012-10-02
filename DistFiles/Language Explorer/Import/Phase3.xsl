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

	// this method is for concatinating residue fields
	public static string Residue = "";
	public void ClearResidue() { Residue = "";}
	public string GetResidue() { return Residue; }
	public void AddResidue(string sfm, string data)
	{
		if (Residue.Length > 0)
			Residue += " ";
		Residue += "\\" + sfm + " " + data;
	}


	// create a new GUID and return it
	public string CreateGUID_NEW()
	{
		System.Guid newGuid = Guid.NewGuid();
		return "I" + newGuid.ToString().ToUpper();
	}
	// create a MSA GUID and return it
	public string CreateGUID()
	{
		return "MSA" + nextID().ToString();
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

	public void ResetGUIDKeyPairs()
	{
	  GUIDvalues.Clear();
	}

	public static double ID = 1000;	// this is a global ID
	public double nextID()
	{
		return ID++;
	}

	// if the data ends with a space and digit then return a
	//  'sense' otherwise return 'entry'
	public string GetMainEntriesOrSensesType(string data)
	{
		int spacePos = data.LastIndexOf(' ');
		// have the last space and not at end of the string
		if (spacePos > 0 && spacePos < data.Length-1)
		{
			if (Char.IsDigit(data[spacePos+1]))
				return "sense";
		}
		return "entry";
	}

	// remove the rightmost part of a sense number.  So 1.2.3.4 becomes 1.2.3
	public string SenseNumberBase(string data)
	{
		int br = data.LastIndexOf('.');
		if (br == -1) {
			return "";
		}
		else {
			return data.Substring(0, br);
		}
	}

	public string Trim(string data)
	{
	  return data.Trim();
	}

	  ]]>
   </msxsl:script>

  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Main template
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->


  <xsl:template match="/dictionary">
<!--
  <?xml version="1.0" encoding="UTF-8"?>
  <!DOCTYPE LexDb SYSTEM "FwDatabase.dtd">
-->
<!--    <root>
	  <AdditionalFields>
		<CustomField name="custom12" class="LexEntry" type="Unicode" big="0" wsSelector="-1" userLabel="Created By" helpString="This is the person that created the Entry."/>
	  </AdditionalFields>
-->

	  <LexDb>
		<Entries>
		  <xsl:for-each select="//Entry">
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
DoLexemeAllomorphs
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoLexemeAllomorphs">
	<xsl:param name="lexElement"/>
	  <!-- <TESTING temp="DoLexemeAllomorphs" parm="{$lexElement}" dot="{.}"/> -->
	  <LexemeForm>
		<xsl:element name="{$lexElement/@allomorphClass}">
		  <MorphType ws="{$lexElement/@morphTypeWs}"><xsl:value-of select="$lexElement/@morphType"/></MorphType>
		  <xsl:for-each select="$lexElement">
			  <Form ws="{@ws}"><xsl:value-of select="."/></Form>
		  </xsl:for-each>
		</xsl:element>
	  </LexemeForm>

	  <xsl:if test="$lexElement/../sulf">
		<AlternateForms>
		  <xsl:for-each select="$lexElement/../sulf">
			<xsl:element name="{$lexElement/@allomorphClass}">
			  <MorphType ws="{$lexElement/@morphTypeWs}"><xsl:value-of select="$lexElement/@morphType"/></MorphType>
			  <Form ws="{@ws}"><xsl:value-of select="."/></Form>
			</xsl:element>
		  </xsl:for-each>
		</AlternateForms>
	  </xsl:if>
  </xsl:template>


  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoAllomorphs
	process allomorphs
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoAllomorphs">

	  <!-- first do the LexemeForm allomorphs -->
	  <xsl:if test="lex or (ulf and not(lex))">
	  <LexemeForm>
		<xsl:choose>
		  <xsl:when test="lex">
			<xsl:element name="{lex/@allomorphClass}">
			<MorphType ws="{lex/@morphTypeWs}"><xsl:value-of select="lex/@morphType"/></MorphType>
			  <xsl:for-each select="lex">
				<Form ws="{@ws}"><xsl:copy-of select="."/></Form>
			  </xsl:for-each>
			</xsl:element>
		  </xsl:when>
		  <!-- Now handle the ulf, only here if there is no lex -->
		  <xsl:when test="not(lex)">
			<xsl:for-each select="ulf">
			  <xsl:element name="{./@allomorphClass}">
				<MorphType ws="{./@morphTypeWs}"><xsl:value-of select="./@morphType"/></MorphType>
				<Form ws="{@ws}"><xsl:value-of select="."/></Form>
				<IsAbstract><xsl:text>true</xsl:text></IsAbstract>
			  </xsl:element>
			</xsl:for-each>
		  </xsl:when>
		</xsl:choose>
	  </LexemeForm>
	  </xsl:if>

	  <xsl:if test="allo or (ulf and lex)">
	  <AlternateForms>
		<!-- handle the allo elements -->
		<xsl:for-each select="allo">
		  <xsl:element name="{./@allomorphClass}">
			<MorphType ws="{./@morphTypeWs}"><xsl:value-of select="./@morphType"/></MorphType>
			<Form ws="{@ws}"><xsl:value-of select="."/></Form>
		  </xsl:element>
		</xsl:for-each>

		<xsl:if test="ulf and lex">
		  <xsl:element name="{lex/@allomorphClass}">
			<MorphType ws="{lex/@morphTypeWs}"><xsl:value-of select="lex/@morphType"/></MorphType>
			<xsl:for-each select="ulf">
			  <Form ws="{@ws}"><xsl:value-of select="."/></Form>
			</xsl:for-each>
			<IsAbstract><xsl:text>true</xsl:text></IsAbstract>
		  </xsl:element>
		</xsl:if>

	  </AlternateForms>
	</xsl:if>

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
	<xsl:if test="cit|scit">
	  <xsl:for-each select="cit|scit">
		<!-- the C# code strips any affix markers while writing Phase1Output.xml -->
		<CitationForm ws="{@ws}"><xsl:value-of select="."/></CitationForm>
	  </xsl:for-each>
	</xsl:if>
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
DoEtymology
	process an Etymology element
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoEtymology">
	<xsl:if test="Etymology">
	  <xsl:element name="Etymology">
		<xsl:for-each select="Etymology">
		  <xsl:element name="LexEtymology">
			<!-- Form -->
					  <xsl:if test="etf">
			<xsl:call-template name="JoinOnWS">
				<xsl:with-param name="abbr" select="etf"/>	                    <!-- Abbr: element name to look for -->
				<xsl:with-param name="eNameOut">Form</xsl:with-param>		<!-- eNameOut: element name to output -->
			</xsl:call-template>
					  </xsl:if>
			<!-- Comment -->
			<xsl:if test="etc">
			  <xsl:call-template name="JoinOnWS">
				<xsl:with-param name="abbr" select="etc"/>	                    <!-- Abbr: element name to look for -->
				<xsl:with-param name="eNameOut">Comment</xsl:with-param>		<!-- eNameOut: element name to output -->
			  </xsl:call-template>
			</xsl:if>

			<!-- Gloss -->
			  <xsl:if test="etg">
				<xsl:call-template name="JoinOnWS">
				<xsl:with-param name="abbr" select="etg"/>	                    <!-- Abbr: element name to look for -->
				<xsl:with-param name="eNameOut">Gloss</xsl:with-param>		<!-- eNameOut: element name to output -->
				</xsl:call-template>
			  </xsl:if>

			<!-- Source -->
				<xsl:if test="ets">
				  <xsl:call-template name="JoinOnWS">
				<xsl:with-param name="abbr" select="ets"/>	                    <!-- Abbr: element name to look for -->
				<xsl:with-param name="eNameOut">Source</xsl:with-param>		<!-- eNameOut: element name to output -->
				  </xsl:call-template>
				</xsl:if>

			<!--
			<xsl:for-each select="etf">
			  <Form ws="{@ws}"><xsl:value-of select="."/></Form>
			</xsl:for-each>
			<xsl:for-each select="etc">
			  <Comment ws="{@ws}"><xsl:value-of select="."/></Comment>
			</xsl:for-each>
			<xsl:for-each select="etg">
			  <Gloss ws="{@ws}"><xsl:value-of select="."/></Gloss>
			</xsl:for-each>
			<xsl:for-each select="ets">
			  <Source><xsl:value-of select="."/></Source>
			</xsl:for-each> -->
		  </xsl:element>
		</xsl:for-each>
	  </xsl:element>
	</xsl:if>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoPronunciation
	process a Pronunciation element
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoPronunciation">
	<xsl:if test="Pronunciation">
	  <xsl:element name="Pronunciations">
		<xsl:for-each select="Pronunciation">
		  <xsl:element name="LexPronunciation">
			<xsl:if test="prnf">
						<xsl:call-template name="JoinOnWS">
							<xsl:with-param name="abbr" select="prnf"/>
							<xsl:with-param name="eNameOut">Form</xsl:with-param>
						</xsl:call-template>
			</xsl:if>

<!--        <xsl:for-each select="prnf">
			  <Form ws="{@ws}"><xsl:value-of select="."/></Form>
			</xsl:for-each> -->
			<xsl:for-each select="prncv">
			  <CVPattern ws="{@ws}"><xsl:value-of select="."/></CVPattern>
			</xsl:for-each>
			<xsl:for-each select="prnt">
			  <Tone ws="{@ws}"><xsl:value-of select="."/></Tone>
			</xsl:for-each>
			<xsl:for-each select="prnl">
			  <Location><Link ws="{@ws}" name="{.}"/></Location>
			</xsl:for-each>
			<xsl:for-each select="prnm">
			  <MediaFiles>
				<CmMedia>
				  <MediaFile>
					<Link path="{.}"/>
				  </MediaFile>
				</CmMedia>
			  </MediaFiles>
			</xsl:for-each>
		  </xsl:element>
		</xsl:for-each>
	  </xsl:element>
	</xsl:if>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoPicture
	process a Picture element
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoPicture">
	<xsl:if test="Picture">
	  <xsl:element name="Pictures">
		<xsl:for-each select="Picture">
		  <xsl:element name="CmPicture">
			<xsl:for-each select="picf">
			  <PictureFile><Link path="{.}"/></PictureFile>
			</xsl:for-each>
			<xsl:if test="picc">
						<xsl:call-template name="JoinOnWS">
							<xsl:with-param name="abbr" select="picc"/>
							<xsl:with-param name="eNameOut">Caption</xsl:with-param>
						</xsl:call-template>
			</xsl:if>

<!--						<xsl:for-each select="picc">
			  <Caption><AStr ws="{@ws}"><Run ws="{@ws}"><xsl:value-of select="."/></Run></AStr></Caption>
			</xsl:for-each>  -->
		  </xsl:element>
		</xsl:for-each>
	  </xsl:element>
	</xsl:if>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoSemanticDomain
	process a SemanticDomain element
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoSemanticDomain">
	<xsl:if test="SemanticDomain">
	  <xsl:element name="SemanticDomains">
		<xsl:for-each select="SemanticDomain">
		  <xsl:element name="Link">
			<xsl:if test="sem">
			  <!-- variable to become either 'name' or 'abbr' depending on the attributes -->
			  <xsl:attribute name="ws"><xsl:value-of select="./sem/@ws"/></xsl:attribute>
			  <xsl:variable name="aORn"><xsl:call-template name="abbrORname"/></xsl:variable>
			  <xsl:attribute name="{$aORn}"><xsl:value-of select="./sem"/></xsl:attribute>
			  <xsl:attribute name="abbr"><xsl:value-of select="./sem"/></xsl:attribute>
			</xsl:if>
			<xsl:if test="seme">
			  <xsl:attribute name="ws"><xsl:value-of select="./seme/@ws"/></xsl:attribute>
			  <xsl:attribute name="name"><xsl:value-of select="./seme"/></xsl:attribute>
			</xsl:if>
			<xsl:if test="semv">
			  <xsl:attribute name="wsv"><xsl:value-of select="./semv/@ws"/></xsl:attribute>
			  <xsl:attribute name="namev"><xsl:value-of select="./semv"/></xsl:attribute>
			</xsl:if>
		  </xsl:element>
		</xsl:for-each>
	  </xsl:element>
	</xsl:if>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
GetWSAttribute
	create the EntryType element
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="GetWSAttribute">
	<xsl:param name="element"/>
	<!-- <xsl:value-of select="$element/@ws"/> -->
	<xsl:text>en</xsl:text>
  </xsl:template>

  <xsl:template name="DoLexEntryRef">
	<xsl:param name="LexElement"/>
	<xsl:param name="VarX"/>
	<xsl:param name="GUID"/>
	<!-- Handle the ComplexEntryTypes element  was EntryType element -->
	<xsl:comment>name of LexElement =<xsl:value-of select="name($LexElement)"/></xsl:comment>
	<xsl:comment>*** DoLexEntryRef *** template </xsl:comment>
	<xsl:choose>
	  <!-- if it's a variant, create the VariantEntryTypes element -->
	  <xsl:when test="name($VarX) = 'va'">
		<xsl:element name="VariantEntryTypes">
		  <xsl:attribute name="ws">
			<xsl:value-of select="$VarX/@varTypeWs"/>
		  </xsl:attribute>
		  <xsl:value-of select="$VarX/@varType"/>
		</xsl:element>
		<!-- create the ComponentLexemes  -->
		<xsl:element name="ComponentLexemes">
		  <xsl:attribute name="type"><xsl:text>target</xsl:text></xsl:attribute>
		  <xsl:value-of select="$GUID"/>
		</xsl:element>
	  </xsl:when>
	  <xsl:when test="name($VarX) = 'se'">
		<xsl:element name="SubEntryType">
		  <xsl:attribute name="ws">
			<xsl:value-of select="$VarX/@subTypeWs"/>
		  </xsl:attribute>
		  <xsl:value-of select="$VarX/@subType"/>
		</xsl:element>
		<!-- create the PrimaryLexemes  -->
		<xsl:element name="PrimaryLexemes">
		  <xsl:attribute name="type"><xsl:text>target</xsl:text></xsl:attribute>
		  <xsl:value-of select="$GUID"/>
		</xsl:element>
	  </xsl:when>

	</xsl:choose>

	 <!-- Handle the Allomorphs, with the form info coming from the variant entry -->
	<xsl:call-template name="DoLexemeAllomorphs">
	  <xsl:with-param name="lexElement" select="$VarX"/>
	</xsl:call-template>
	<!-- Handle comments -->
	<xsl:if test="varc">
		<xsl:call-template name="JoinOnWS">
		  <xsl:with-param name="abbr" select="varc"/>
		  <xsl:with-param name="eNameOut">Summary</xsl:with-param>
		</xsl:call-template>
	</xsl:if>

  </xsl:template>


<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoEntryResidue
	process all entry residue items
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoEntryResidue">
	<!-- we only want one of the Sense/eires elements, so gather them and then take the first one. -->
	<xsl:variable name="senseEiresElements" select="Sense/eires"/>
	<xsl:variable name="senseSeiresElements" select="Sense/seires"/>
	<xsl:if test="eires | $senseEiresElements[1]">
	  <xsl:call-template name="JoinResidueOnWS">
		<xsl:with-param name="abbr" select="eires | $senseEiresElements[1]"/>			<!-- Abbr: element name to look for -->
		<xsl:with-param name="eNameOut">ImportEntryResidue</xsl:with-param>	<!-- eNameOut: element name to output -->
	  </xsl:call-template>
	</xsl:if>
	<xsl:if test="seires | $senseSeiresElements[1]">
	  <xsl:call-template name="JoinResidueOnWS">
		<xsl:with-param name="abbr" select="seires | $senseSeiresElements[1]"/>		<!-- Abbr: element name to look for -->
		<xsl:with-param name="eNameOut">ImportEntryResidue</xsl:with-param>	<!-- eNameOut: element name to output -->
	  </xsl:call-template>
	</xsl:if>
	<xsl:if test="veires">
	  <xsl:call-template name="JoinResidueOnWS">
		<xsl:with-param name="abbr" select="veires"/>					<!-- Abbr: element name to look for -->
		<xsl:with-param name="eNameOut">ImportEntryResidue</xsl:with-param>	<!-- eNameOut: element name to output -->
	  </xsl:call-template>
	</xsl:if>
  </xsl:template>

  <xsl:template name="DoEntryResidue_OLD">
	<xsl:if test="eires or seires or veires">
	  <xsl:element name="ImportEntryResidue">
		<xsl:variable name="notUsed"><xsl:value-of select="user:ClearResidue()"/></xsl:variable>
		<xsl:for-each select="eires">
		  <xsl:variable name="data"><xsl:value-of select="."/></xsl:variable>
		  <xsl:variable name="notUsed2"><xsl:value-of select="user:AddResidue(@sfm, $data)"/></xsl:variable>
		</xsl:for-each>
		<xsl:for-each select="seires">
		  <xsl:variable name="data"><xsl:value-of select="."/></xsl:variable>
		  <xsl:variable name="notUsed2"><xsl:value-of select="user:AddResidue(@sfm, $data)"/></xsl:variable>
		</xsl:for-each>
		<xsl:for-each select="veires">
		  <xsl:variable name="data"><xsl:value-of select="."/></xsl:variable>
		  <xsl:variable name="notUsed2"><xsl:value-of select="user:AddResidue(@sfm, $data)"/></xsl:variable>
		</xsl:for-each>
		<xsl:value-of select="user:GetResidue()"/>
	  </xsl:element>
	</xsl:if>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoSenseResidue
	process all sense residue items
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoSenseResidue">
	<xsl:if test="sires">
	  <xsl:call-template name="JoinResidueOnWS">
		<xsl:with-param name="abbr" select="sires"/>					<!-- Abbr: element name to look for -->
		<xsl:with-param name="eNameOut">ImportSenseResidue</xsl:with-param>	<!-- eNameOut: element name to output -->
	  </xsl:call-template>
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
DoEntry
	process an Entry's information
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoEntry">
	<!-- reset the GUID key pairs for a new entry -->
	<xsl:value-of select="user:ResetGUIDKeyPairs()"/>
	<!-- get the GUID attribute value assigned to this element -->
	<xsl:variable name="GUID" select="@guid"/>
	<!-- create the LexEntry element -->
	<xsl:element name="LexEntry">
	  <!-- Only put out the 'id' attribute if the entry has variants or subentries -->
	  <xsl:if test="Subentry or Variant">
		<xsl:attribute name="id">
		  <xsl:value-of select="$GUID"/>
		</xsl:attribute>
	  </xsl:if>

	  <!-- this was where the EntryType was put out before, but will now be a part of the LexEntryRef -->

	  <!-- 9/8/05 - only puts out citation form if there is a cit entry -->
	  <xsl:call-template name="DoCitationForm"/>
	  <xsl:call-template name="DoAllomorphs"/>
	  <xsl:call-template name="DoDateCreated"/>
	  <xsl:call-template name="DoHomographNumber"/>
	  <xsl:call-template name="DoDateModified"/>
	  <xsl:call-template name="DoEtymology"/>		<!-- Add to subentry -->
	  <xsl:call-template name="DoPronunciation"/>		<!-- Add to subentry -->

	  <!--
	  <xsl:element name="TestMSALexElementData">
		<xsl:attribute name="currentNodeName"><xsl:value-of select="name()"/></xsl:attribute>
		<xsl:value-of select="lex"/>
	  </xsl:element>
	  -->
	  <xsl:if test="lex">
	  <xsl:call-template name="DoMSAs">
		<xsl:with-param name="lexElement" select="lex"/>
	  </xsl:call-template>
	  </xsl:if>

	  <xsl:call-template name="DoEntryResidue"/>

	  <xsl:call-template name="DoCrossReferences"/>

	  <!-- Restrictions -->
	  <xsl:if test="erest">
	  <xsl:call-template name="JoinOnWS">
		<xsl:with-param name="abbr" select="erest"/>					<!-- Abbr: element name to look for -->
		<xsl:with-param name="eNameOut">Restrictions</xsl:with-param>	<!-- eNameOut: element name to output -->
	  </xsl:call-template>
	  </xsl:if>
	  <!-- Bibliography -->
	  <xsl:if test="ebib">
	  <xsl:call-template name="JoinOnWS">
		<xsl:with-param name="abbr" select="ebib"/>					<!-- Abbr: element name to look for -->
		<xsl:with-param name="eNameOut">Bibliography</xsl:with-param>	<!-- eNameOut: element name to output -->
	  </xsl:call-template>
	  </xsl:if>
	  <!-- Comment -->
	  <xsl:if test="com">
	  <xsl:call-template name="JoinOnWS">
		<xsl:with-param name="abbr" select="com"/>	                    <!-- Abbr: element name to look for -->
		<xsl:with-param name="eNameOut">Comment</xsl:with-param>		<!-- eNameOut: element name to output -->
	  </xsl:call-template>
	  </xsl:if>

	  <!-- Summary Definition -->
	  <xsl:if test="sdef">
		<xsl:call-template name="DoLexSenseMultiString">
		<xsl:with-param name="abbrNodes" select="sdef"/>
		<xsl:with-param name="elementName">SummaryDefinition</xsl:with-param>
		</xsl:call-template>
	  </xsl:if>

	  <!-- Literal Meaning -->
	  <xsl:if test="litm">
		<xsl:call-template name="DoLexSenseMultiString">
		<xsl:with-param name="abbrNodes" select="litm"/>
		<xsl:with-param name="elementName">LiteralMeaning</xsl:with-param>
	  </xsl:call-template>
	  </xsl:if>

	  <xsl:call-template name="DoSenses"/>
	  <xsl:call-template name="DoMainEntryCrossRef"/>
	  <!-- Custom fields in Entry -->
	  <xsl:call-template name="JoinCustomOnFwid"/>

	</xsl:element>

	<!-- New stuff (3/4/09) -->

	  <!--xsl:for-each select="Variant">
		<xsl:element name="LexEntry">

		</xsl:element>
	  </xsl:for-each>

	  <xsl:call-template name="DoLexEntryRef">
		<xsl:with-param name="LexElement" select="."/>
		<xsl:with-param name="VarX" />
		<xsl:with-param name="GUID" select="$GUID"/>
		</xsl:call-template -->

	<!-- ADDED above comment 3/5 -->
	  <!-- Variants for the Entry/lex elements -->

		<!--xsl:comment>calling DoVariantsNew with node = <xsl:value-of select="name(.)"/></xsl:comment-->
	  <xsl:call-template name="DoVariantsNew">
		  <xsl:with-param name="parentNode" select="."/>
		  <xsl:with-param name="GUID" select="@guid"/>
	  </xsl:call-template>

	  <!-- Variants for the Entry/Sense/sn elements -->
	  <!-- xsl:if test="Sense/sn">
		  <xsl:for-each select="Sense">
			  <xsl:call-template name="DoVariants">
				  <xsl:with-param name="lexElement" select="sn"/>
				  <xsl:with-param name="GUID" select="@guid"/> < was sn/@guid >
			  </xsl:call-template>
		  </xsl:for-each>
	  </xsl:if -->

	  <!-- Subentries -->
	<xsl:if test="lex">
	  <xsl:call-template name="DoSubEntryNew">
	  <xsl:with-param name="lexElement" select="lex"/>
	  <xsl:with-param name="GUID" select="@guid"/>	<!-- was lex/@guid -->
	  </xsl:call-template>
	</xsl:if>

	</xsl:template>


  <!--
	- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	DoVariantsNew
	process variants
	Parameters: the lex element associated with these variants
	- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
  -->

  <xsl:template name="DoVariantsNew">
	<xsl:param name="parentNode"/>
	<xsl:param name="GUID"/>
	<!--xsl:comment> In DoVariants Call, parentNode=<xsl:value-of select="name($parentNode)"/> GUID=<xsl:value-of select="$GUID"/></xsl:comment-->
	<!-- Process variants for the lexElement -->
	<xsl:if test="$parentNode/Variant">
	  <xsl:for-each select="$parentNode/Variant">
		<xsl:variable name="parentGUID" select="$GUID"/>
		<!--xsl:comment>parentGUID = <xsl:value-of select="$parentGUID"/></xsl:comment-->

		<xsl:element name="LexEntry">
		<!-- Handle the Allomorphs, with the form info coming from the variant entry -->
		  <xsl:if test="var">
			<xsl:call-template name="DoLexemeAllomorphs">
			  <xsl:with-param name="lexElement" select="var"/>
			</xsl:call-template>
		  </xsl:if>


		<xsl:element name="LexEntryRef">
		  <!-- reset the GUID key pairs for a new entry -->
		  <xsl:value-of select="user:ResetGUIDKeyPairs()"/>
		  <!--  Handle the EntryType, MainEntriesOrSenses, CitationForm & Allomorphs element for each variant type -->

		  <!-- New code START -->

		  <!-- Handle comments -->
		  <xsl:if test="varc">
			<xsl:call-template name="JoinOnWS">
			  <xsl:with-param name="abbr" select="varc"/>
			  <xsl:with-param name="eNameOut">Summary</xsl:with-param>
			</xsl:call-template>
		  </xsl:if>
		  <xsl:if test="subc">
			<xsl:call-template name="JoinOnWS">
			  <xsl:with-param name="abbr" select="subc"/>
			  <xsl:with-param name="eNameOut">Summary</xsl:with-param>
			</xsl:call-template>
		  </xsl:if>

		  <xsl:choose>
			<!-- if it's a variant, create the VariantEntryTypes element -->
			<xsl:when test="var">
			  <xsl:element name="VariantEntryTypes">
				<xsl:attribute name="ws">
				  <xsl:value-of select="var/@funcWS"/>
				</xsl:attribute>
				<xsl:value-of select="var/@func"/>
			  </xsl:element>
			  <!-- create the ComponentLexemes  -->
			  <xsl:element name="ComponentLexemes">
				<xsl:attribute name="type"><xsl:text>target</xsl:text></xsl:attribute>
				<xsl:value-of select="$parentGUID"/>
			  </xsl:element>
			</xsl:when>

			<xsl:when test="sub">
			  <xsl:element name="SubEntryType">
				<xsl:attribute name="ws">
				  <xsl:value-of select="sub/@funcWS"/>
				</xsl:attribute>
				<xsl:value-of select="sub/@func"/>
			  </xsl:element>

			  <!-- create the PrimaryLexemes  -->
			  <xsl:element name="PrimaryLexemes">
				<xsl:attribute name="type"><xsl:text>target</xsl:text></xsl:attribute>
				<xsl:value-of select="$parentGUID"/>
			  </xsl:element>
			</xsl:when>

		  </xsl:choose>

		  <!-- New code End -->

		  <xsl:call-template name="DoEntryResidue"/>
		  <!-- Custom fields in this Sense -->
		  <xsl:call-template name="JoinCustomOnFwid"/>

		</xsl:element>
		<!-- </xsl:if> -->
		  </xsl:element>
	  </xsl:for-each>
	</xsl:if>

	<!-- Variants for the sense of the current element -->
	<xsl:if test="$parentNode/Sense">
	  <xsl:for-each select="$parentNode/Sense">
	  <xsl:call-template name="DoVariantsNew">
		<xsl:with-param name="parentNode" select="."/>
		<xsl:with-param name="GUID" select="./@guid"/>
	  </xsl:call-template>
	  </xsl:for-each>
	</xsl:if>

	<!-- Variants for the subentry of the current element -->
	<xsl:if test="$parentNode/Subentry">
	  <xsl:for-each select="$parentNode/Subentry">
		<xsl:call-template name="DoVariantsNew">
		  <xsl:with-param name="parentNode" select="."/>
		  <xsl:with-param name="GUID" select="$GUID"/>
		</xsl:call-template>
	  </xsl:for-each>
	</xsl:if>

	<!--xsl:if test="$lexElement/Subentry">
	  <!- xsl:for-each select="$lexElement/Subentry" -->
	  <!--xsl:call-template name="DoVariants">
		<xsl:with-param name="lexElement" select="$lexElement/Subentry"/>
		<xsl:with-param name="GUID" select="$lexElement/Subentry/@guid"/>
	  </xsl:call-template>
	  <!- /xsl:for-each -->
	<!--/xsl:if-->


  </xsl:template>

	<!--
	- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	DoSubEntryNew
	process subentries
	Parameters: the lex element associated with this subentry, the GUID for this item
	- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
  -->
  <xsl:template name="DoSubEntryNew">
	<xsl:param name="lexElement"/>
	<xsl:param name="GUID"/>
	<!--xsl:comment> In DoSubEntryNew Call, parentNode=<xsl:value-of select="name($lexElement)"/> GUID=<xsl:value-of select="$GUID"/></xsl:comment-->
	<xsl:if test="Subentry">
	  <xsl:for-each select="Subentry">
		<!-- Create the LexEntry element -->
		<xsl:element name="LexEntry">
		  <xsl:attribute name="id"><xsl:value-of select="@guid"/></xsl:attribute>
		  <!-- reset the GUID key pairs for a new entry -->
		  <xsl:value-of select="user:ResetGUIDKeyPairs()"/>
		  <!--  Handle the EntryType, MainEntriesOrSenses, CitationForm & Allomorphs element for each variant type -->

		  <!-- New Code START -->

		  <!-- Handle the MSAs -->
		  <xsl:if test="sub">
		  <xsl:call-template name="DoMSAs">
			<xsl:with-param name="lexElement" select="sub"/>
		  </xsl:call-template>

		  <!-- Handle the Allomorphs, with the form info coming from the subentry -->
		  <xsl:call-template name="DoLexemeAllomorphs">
			  <xsl:with-param name="lexElement" select="sub"/>
			</xsl:call-template>
		  </xsl:if>

		  <xsl:element name="LexEntryRef">
 <!--
				<xsl:when test="sub">
				  <xsl:element name="SubEntryType">
					<xsl:attribute name="ws">
					  <xsl:value-of select="sub/@funcWS"/>
					</xsl:attribute>
					<xsl:value-of select="sub/@func"/>
				  </xsl:element>

				  <xsl:element name="PrimaryLexemes">
					<xsl:attribute name="type"><xsl:text>target</xsl:text></xsl:attribute>
					<xsl:value-of select="$parentGUID"/>
				  </xsl:element>
				</xsl:when>
	   -->
			  <!-- New code End -->

 <!--
			  <xsl:call-template name="DoEntryResidue"/>
			   <xsl:call-template name="JoinCustomOnFwid"/>

			</xsl:element>
	   -->
		  <!-- New Code END -->

		  <xsl:if test="./subc">
			<xsl:call-template name="JoinOnWS">
			  <xsl:with-param name="abbr" select="./subc"/>
			  <xsl:with-param name="eNameOut">Summary</xsl:with-param>
			</xsl:call-template>
		  </xsl:if>

			<xsl:if test="./sub">
			  <xsl:element name="ComplexEntryTypes">
				<xsl:attribute name="ws">
				  <xsl:value-of select="sub/@funcWS"/>
				</xsl:attribute>
				<xsl:value-of select="sub/@func"/>
			  </xsl:element>

			  <!-- create the ComponentLexemes  -->
			  <xsl:element name="ComponentLexemes">
				<xsl:attribute name="type"><xsl:text>target</xsl:text></xsl:attribute>
				<xsl:value-of select="$GUID"/>
			  </xsl:element>

			  <!-- create the PrimaryLexemes  -->
			  <xsl:element name="PrimaryLexemes">
				<xsl:attribute name="type"><xsl:text>target</xsl:text></xsl:attribute>
				<xsl:value-of select="$GUID"/>
			  </xsl:element>
			</xsl:if>

			</xsl:element>

<!--          <xsl:if test="./subc">
			<xsl:call-template name="DoSubXElements">
			  <xsl:with-param name="LexElement" select="$lexElement"/>
			  <xsl:with-param name="SubX" select="./subc"/>
			  <xsl:with-param name="GUID" select="$GUID"/>
			</xsl:call-template>
		  </xsl:if>
-->
		  <!-- Handle Senses/glos -->
		  <xsl:call-template name="DoSenses"/>

		  <!-- Restrictions -->
		  <xsl:if test="serest">
			<xsl:call-template name="JoinOnWS">
			<xsl:with-param name="abbr" select="serest"/>					<!-- Abbr: element name to look for -->
			<xsl:with-param name="eNameOut">Restrictions</xsl:with-param>	<!-- eNameOut: element name to output -->
			</xsl:call-template>
		  </xsl:if>


		  <!-- Bibliography -->
		  <xsl:if test="sebib">
			<xsl:call-template name="JoinOnWS">
			<xsl:with-param name="abbr" select="sebib"/>					<!-- Abbr: element name to look for -->
			<xsl:with-param name="eNameOut">Bibliography</xsl:with-param>	<!-- eNameOut: element name to output -->
			</xsl:call-template>
		  </xsl:if>

		  <!-- Comment -->
		  <xsl:if test="scom">
			<xsl:call-template name="JoinOnWS">
			<xsl:with-param name="abbr" select="scom"/>	                    <!-- Abbr: element name to look for -->
			<xsl:with-param name="eNameOut">Comment</xsl:with-param>		<!-- eNameOut: element name to output -->
			</xsl:call-template>
		  </xsl:if>

		  <xsl:call-template name="DoEntryResidue"/>
		  <xsl:call-template name="JoinCustomOnFwid"/>
		  <xsl:call-template name="DoCrossReferences"/>
		  <xsl:if test="smeref">
			<xsl:element name="LexEntryRef">
			  <xsl:variable name="tdata"><xsl:value-of select="smeref"/></xsl:variable>
			  <ComponentLexemes>
				<xsl:attribute name="type"><xsl:value-of select="user:GetMainEntriesOrSensesType($tdata)"/></xsl:attribute>
				<xsl:attribute name="ws"><xsl:value-of select="smeref/@ws"/></xsl:attribute>
				<xsl:value-of select="$tdata"/>
			  </ComponentLexemes>
			</xsl:element>
		  </xsl:if>

		  <!-- Summary Definition -->
		  <xsl:if test="ssdef">
			<xsl:call-template name="DoLexSenseMultiString">
			<xsl:with-param name="abbrNodes" select="ssdef"/>
			<xsl:with-param name="elementName">SummaryDefinition</xsl:with-param>
			</xsl:call-template>
		  </xsl:if>

		  <!-- Literal Meaning -->
		  <xsl:if test="slitm">
			<xsl:call-template name="DoLexSenseMultiString">
			<xsl:with-param name="abbrNodes" select="slitm"/>
			<xsl:with-param name="elementName">LiteralMeaning</xsl:with-param>
			</xsl:call-template>
		  </xsl:if>

		  <xsl:call-template name="DoEtymology"/>			<!-- Add to subentry -->
		  <xsl:call-template name="DoPronunciation"/>		<!-- Add to subentry -->
		  <xsl:call-template name="DoCitationForm"/>		<!-- Added to subentry too -->

		</xsl:element>
	  </xsl:for-each>
	</xsl:if>
  </xsl:template>
  <!-- End of DoSubEntryNew template -->

  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoExamples
	process examples
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoExamples">
	<xsl:if test="Example">
	<!-- <xsl:if test="count(./*)"> -->
	  <Examples>
		<xsl:for-each select="Example">
		<!-- <xsl:if test="sent or trans or ./ref"> -->
		  <LexExampleSentence>
		 <!-- Example -->
			<xsl:if test="sent">
			  <xsl:call-template name="JoinOnWS">
			<xsl:with-param name="abbr" select="sent"/> 			<!-- Abbr: element name to look for -->
			<xsl:with-param name="eNameOut">Example</xsl:with-param> <!-- eNameOut: element name to output -->
			  </xsl:call-template>
			</xsl:if>

			  <Translations>
		<!-- CmTranslation -->
				<xsl:if test="trans">
				  <xsl:call-template name="JoinOnWS">
			<xsl:with-param name="abbr" select="trans"/>								<!-- Abbr: element name to look for -->
			<xsl:with-param name="eNameOut">CmTranslation</xsl:with-param><!-- eNameOut: element name to output -->
				  </xsl:call-template>
				</xsl:if>
			  </Translations>
			<xsl:if test="./ref">
			  <!-- <Reference ws="{./ref/@ws}"><xsl:value-of select="./ref"/></Reference> -->
			  <Reference ws="{./ref/@ws}"><xsl:apply-templates select="./ref/*|./ref/text()" mode="IncludeIFMs"/></Reference>
			</xsl:if>
			<!-- Custom fields in this LexExampleSentence -->
			<xsl:call-template name="JoinCustomOnFwid"/>
		  </LexExampleSentence>
		<!-- </xsl:if> -->
		</xsl:for-each>
	  </Examples>
	<!-- </xsl:if> -->
	</xsl:if>
  </xsl:template>

  <xsl:template match="*" mode="IncludeIFMs">
	<xsl:copy-of select="."/>
  </xsl:template>

  <!-- xsl:template match="Custom">
	<xsl:variable name="eName">
	  <xsl:if test="@type = 'string'">Custom</xsl:if>
	  <xsl:if test="not(@type = 'string')">CustomStr</xsl:if>
	</xsl:variable>
	<xsl:element name="{$eName}">
	  <xsl:attribute name="name"><xsl:value-of select="@fwid"/></xsl:attribute>
	  <xsl:choose>
		<xsl:when test="@type = 'string'">

		</xsl:when>
	  </xsl:choose>

	</xsl:element>
  </xsl:template -->

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
  <xsl:param name="lexElement"/>
	<!-- <xsl:value-of select="$lexElement"/> -->
	<xsl:text>_</xsl:text>
	<xsl:value-of select="."/>
</xsl:template>

  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
lexKeyName
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
<xsl:template name="lexKeyName">
  <xsl:param name="lexElement"/>
  <xsl:text>lex_</xsl:text>
  <xsl:value-of select="$lexElement"/>
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
	JoinCustomOnFwid
	- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
  -->
  <xsl:template name="JoinCustomOnFwid">
	<!-- Create a variable that contains the sorted list of elements on the @fwid -->
	<xsl:variable name="sortedNodes">
	  <xsl:for-each select="Custom">
		<xsl:sort select="@sortKey"/>
		<xyz>	<!-- Create a new element that is consistantly named so it can be in the XPath -->
		  <xsl:copy-of select="@* | * | text() | node()"/>
		</xyz>
	  </xsl:for-each>
	</xsl:variable>
	<!-- Now use the 'sortedNodes' variable as our node-set -->
	<xsl:for-each select="msxsl:node-set($sortedNodes)/xyz[not(@sortKey=preceding-sibling::xyz[1]/@sortKey)]">
	  <xsl:element name="Custom">
		<xsl:attribute name="ws"><xsl:value-of select="@ws"/></xsl:attribute>
		<xsl:attribute name="fwid"><xsl:value-of select="@fwid"/></xsl:attribute>
		<xsl:attribute name="type"><xsl:value-of select="@type"/></xsl:attribute>
		<xsl:copy-of select="node()"/>
		<xsl:call-template name="joinCustom"/>
	  </xsl:element>
	</xsl:for-each>
  </xsl:template>
  <!-- Recursive helper routine to concat all elements with the @fwid = to the first one -->
  <xsl:template name="joinCustom" >
	<xsl:variable name="sortKey" select="@sortKey"/>
	<xsl:for-each select="following-sibling::node()[1][@sortKey=$sortKey]">
	  <xsl:text>; </xsl:text><xsl:copy-of select="node()"/>
	  <xsl:call-template name="joinCustom"/>
	</xsl:for-each>
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
		  <xsl:copy-of select="@* | * | text() | node()"/>
		</xyz>
	  </xsl:for-each>
	</xsl:variable>
	  <!-- Now use the 'sortedNodes' variable as our node-set -->
	<xsl:for-each select="msxsl:node-set($sortedNodes)/xyz[not(@ws=preceding-sibling::xyz[1]/@ws)]">
	  <xsl:element name="{$eNameOut}">
		<xsl:attribute name="ws"><xsl:value-of select="@ws"/></xsl:attribute>
		  <xsl:copy-of select="node()"/>
		  <xsl:call-template name="join3"/>
	  </xsl:element>
	</xsl:for-each>
  </xsl:template>
  <!-- Recursive helper routine to concat all elements with the @ws = to the first one -->
  <xsl:template name="join3" >
	<xsl:variable name="ws" select="@ws"/>
	<xsl:for-each select="following-sibling::node()[1][@ws=$ws]">
		<xsl:text>; </xsl:text><xsl:copy-of select="node()"/>
		<xsl:call-template name="join3"/>
	</xsl:for-each>
  </xsl:template>

  <xsl:template name="JoinResidueOnWS">
	<xsl:param name="abbr"/>		<!-- Abbr: element name to look for -->
	<xsl:param name="eNameOut"/>	<!-- eNameOut: element name to output -->
	<!-- Create a variable that contains the sorted list of elements on the @ws -->
	<xsl:variable name="sortedNodes">
	  <xsl:for-each select="$abbr">
		<xsl:sort select="@ws"/>
		<xyz>	<!-- Create a new element that is consistantly named so it can be in the XPath -->
		  <xsl:copy-of select="@* | * | text() | node()"/>
<!--		  <xsl:copy-of select="@*"/>
		  <xsl:value-of select="."/> -->
		</xyz>
	  </xsl:for-each>
	</xsl:variable>
	<!-- Now use the 'sortedNodes' variable as our node-set -->
	<xsl:for-each select="msxsl:node-set($sortedNodes)/xyz[not(@ws=preceding-sibling::xyz[1]/@ws)]">
	  <xsl:element name="{$eNameOut}">
		<xsl:attribute name="ws"><xsl:value-of select="@ws"/></xsl:attribute>
		<xsl:text>\</xsl:text>			<!-- put out the backslash character -->
		<xsl:value-of select="@sfm"/>	<!-- put out the sfm -->
		<xsl:text> </xsl:text>			<!-- put out a space seperator -->
		<xsl:copy-of select="node()"/>
		<!-- xsl:value-of select="."/ -->
		<xsl:call-template name="join4"/>
	  </xsl:element>
	</xsl:for-each>
  </xsl:template>
   <!-- Recursive helper routine to concat all elements with the @ws = to the first one -->
   <xsl:template name="join4" >
	<xsl:variable name="ws" select="@ws"/>
	<xsl:for-each select="following-sibling::node()[1][@ws=$ws]">
<!--      <xsl:text disable-output-escaping="yes">&amp;U-2028;\</xsl:text> -->
	  <xsl:text> \</xsl:text>
	  <xsl:value-of select="@sfm"/><xsl:text> </xsl:text><xsl:copy-of select="node()"/> <!-- xsl:value-of select="."/ -->
	  <xsl:call-template name="join4"/>
	</xsl:for-each>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoLexicalRelations
	process Lexical Relations
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
<!--
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
		  </Link>
		</xsl:for-each>
	  </LexicalRelations>
	</xsl:if>
  </xsl:template>
  -->
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoMSAs
	process MSAs
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoMSAs">
	<xsl:param name="lexElement"/>
	<!-- <TESTING temp="DoMSAs" parm="{$lexElement}" dot="{.}"/> -->
	<xsl:if test="Sense/pos">
	  <MorphoSyntaxAnalyses>
		<xsl:for-each select="Sense/pos">
		  <xsl:variable name="key">
			<xsl:call-template name="msaKeyName">
			  <xsl:with-param name="lexElement" select="$lexElement"/>
			</xsl:call-template>
		  </xsl:variable>
		  <xsl:if test="0!=user:FoundGUIDKey($key)">
			<xsl:variable name="msaName">
			  <xsl:choose>
				<xsl:when test="$lexElement/@allomorphClass='MoStemAllomorph'">MoStemMsa</xsl:when>
				<xsl:otherwise>MoUnclassifiedAffixMsa</xsl:otherwise>
			  </xsl:choose>
			</xsl:variable>
			<xsl:element name="{$msaName}">
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
		Parameters: default (default part of speech value)
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoSenseMSALinks">
	<xsl:param name="default"/>
	<xsl:for-each select="pos">
	  <MorphoSyntaxAnalysis>
		<Link>
		  <xsl:variable name="key">
			<xsl:call-template name="msaKeyName">
			  <!--xsl:with-param name="lexElement" select="../../lex"/-->
			</xsl:call-template>
		  </xsl:variable>


		  <xsl:attribute name="target"><xsl:value-of select="user:GetKeyGUID($key)"/></xsl:attribute>
		  <!--
		  <xsl:attribute name="msakey"><xsl:value-of select="$key"/></xsl:attribute>
		  <xsl:attribute name="msalexElement"><xsl:value-of select="../../lex"/></xsl:attribute>
		  -->
		</Link>
	  </MorphoSyntaxAnalysis>
	</xsl:for-each>
	<!-- if no part of speech is defined for this sense, then use the default (from a higher nesting level) -->
	<xsl:if test="not(pos) and $default">
	  <MorphoSyntaxAnalysis>
		<Link>
		  <xsl:variable name="key">_<xsl:value-of select="$default"/></xsl:variable>
		  <xsl:attribute name="target"><xsl:value-of select="user:GetKeyGUID($key)"/></xsl:attribute>
		</Link>
	  </MorphoSyntaxAnalysis>
	</xsl:if>
  </xsl:template>

<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoSenseFields
	process Sense fields, but not the embedded senses
		Parameters: pos (default part of speech value)
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoSenseFields">
	<xsl:param name="pos"/>

	<!-- get the sense guid attribute for possible future use -->
	<xsl:variable name="GUID">
		<xsl:value-of select="@guid"/>  <!-- was sn/@guid"/ -->
	</xsl:variable>

	<!-- Only put out the 'id' attribute if the sense has variants -->
	<xsl:if test="Variant">
		<xsl:attribute name="id">
			<xsl:value-of select="$GUID"/>
		</xsl:attribute>
	</xsl:if>

	<!-- Handle all the LexSense MultiString elements -->

	<!-- AnthroNote -->
	<xsl:if test="anote">
	  <xsl:call-template name="DoLexSenseMultiString">
	  <xsl:with-param name="abbrNodes" select="anote"/>
	  <xsl:with-param name="elementName">AnthroNote</xsl:with-param>
	  </xsl:call-template>
	</xsl:if>

	<!-- Bibliography -->
	<xsl:if test="sbib">
	  <xsl:call-template name="DoLexSenseMultiString">
	  <xsl:with-param name="abbrNodes" select="sbib"/>
	  <xsl:with-param name="elementName">Bibliography</xsl:with-param>
	  </xsl:call-template>
	</xsl:if>

	<!-- Definition -->
	<xsl:if test="def">
	  <xsl:call-template name="DoLexSenseMultiString">
	  <xsl:with-param name="abbrNodes" select="def"/>
	  <xsl:with-param name="elementName">Definition</xsl:with-param>
	  </xsl:call-template>
	</xsl:if>

	<!-- DiscourseNote -->
	<xsl:if test="dnote">
	  <xsl:call-template name="DoLexSenseMultiString">
	  <xsl:with-param name="abbrNodes" select="dnote"/>
	  <xsl:with-param name="elementName">DiscourseNote</xsl:with-param>
	  </xsl:call-template>
	</xsl:if>

	<!-- EncyclopedicInfo -->
	<xsl:if test="enc">
	  <xsl:call-template name="DoLexSenseMultiString">
	  <xsl:with-param name="abbrNodes" select="enc"/>
	  <xsl:with-param name="elementName">EncyclopedicInfo</xsl:with-param>
	  </xsl:call-template>
	</xsl:if>

	<!-- GeneralNote -->
	<xsl:if test="gnote">
	  <xsl:call-template name="DoLexSenseMultiString">
	  <xsl:with-param name="abbrNodes" select="gnote"/>
	  <xsl:with-param name="elementName">GeneralNote</xsl:with-param>
	  </xsl:call-template>
	</xsl:if>

	<!-- GrammarNote -->
	<xsl:if test="grnote">
	  <xsl:call-template name="DoLexSenseMultiString">
	  <xsl:with-param name="abbrNodes" select="grnote"/>
	  <xsl:with-param name="elementName">GrammarNote</xsl:with-param>
	  </xsl:call-template>
	</xsl:if>

	<!-- PhonologyNote -->
	<xsl:if test="pnote">
	  <xsl:call-template name="DoLexSenseMultiString">
	  <xsl:with-param name="abbrNodes" select="pnote"/>
	  <xsl:with-param name="elementName">PhonologyNote</xsl:with-param>
	  </xsl:call-template>
	</xsl:if>

	<!-- SemanticsNote -->
	<xsl:if test="snote">
	  <xsl:call-template name="DoLexSenseMultiString">
	  <xsl:with-param name="abbrNodes" select="snote"/>
	  <xsl:with-param name="elementName">SemanticsNote</xsl:with-param>
	  </xsl:call-template>
	</xsl:if>

	<!-- SocioLinguisticsNote -->
	<xsl:if test="slnote">
	  <xsl:call-template name="DoLexSenseMultiString">
	  <xsl:with-param name="abbrNodes" select="slnote"/>
	  <xsl:with-param name="elementName">SocioLinguisticsNote</xsl:with-param>
	  </xsl:call-template>
	</xsl:if>


	<!-- ************************************************************** -->
	<!-- Handle all the LexSense MultiUnicode elements -->

	<!-- Gloss -->
	<xsl:if test="glos">
	  <xsl:call-template name="JoinOnWS">
	  <xsl:with-param name="abbr" select="glos"/>				<!-- Abbr: element name to look for -->
	  <xsl:with-param name="eNameOut">Gloss</xsl:with-param>	<!-- eNameOut: element name to output -->
	  </xsl:call-template>
	</xsl:if>

	<!-- Restrictions -->
	<xsl:if test="srest">
	  <xsl:call-template name="JoinOnWS">
	  <xsl:with-param name="abbr" select="srest"/>				<!-- Abbr: element name to look for -->
	  <xsl:with-param name="eNameOut">Restrictions</xsl:with-param>	<!-- eNameOut: element name to output -->
	  </xsl:call-template>
	</xsl:if>


	<!-- ************************************************************** -->
	<!-- Handle all the LexSense CmPossibility elements -->

	<xsl:if test="dom">
	  <xsl:call-template name="DoLexSenseCmPossibility">
	  <xsl:with-param name="abbrNodes" select="dom"/>
	  <xsl:with-param name="elementName">DomainTypes</xsl:with-param>
	  </xsl:call-template>
	</xsl:if>

	<xsl:if test="styp">
	  <xsl:call-template name="DoLexSenseCmPossibility">
	  <xsl:with-param name="abbrNodes" select="styp"/>
	  <xsl:with-param name="elementName">SenseType</xsl:with-param>
	  </xsl:call-template>
	</xsl:if>

	<xsl:if test="stat">
	  <xsl:call-template name="DoLexSenseCmPossibility">
	  <xsl:with-param name="abbrNodes" select="stat"/>
	  <xsl:with-param name="elementName">Status</xsl:with-param>
	  </xsl:call-template>
	</xsl:if>

	<xsl:if test="utyp">
	  <xsl:call-template name="DoLexSenseCmPossibility">
	  <xsl:with-param name="abbrNodes" select="utyp"/>
	  <xsl:with-param name="elementName">UsageTypes</xsl:with-param>
	  </xsl:call-template>
	</xsl:if>


	<!-- ************************************************************** -->
	<!-- Do the remaining -->

	<xsl:call-template name="DoAnthroCodes"/>
	<xsl:call-template name="DoSenseMSALinks">
	  <xsl:with-param name="default" select="$pos"/>
	</xsl:call-template>
	<xsl:call-template name="DoExamples"/>
<!--           <xsl:call-template name="DoLexicalRelations"/>  -->
	<xsl:call-template name="DoReversalEntries"/>
	<xsl:call-template name="DoScientificName"/>
	<xsl:call-template name="DoSource"/>
	<xsl:call-template name="DoSemanticDomain"/>
	<xsl:call-template name="DoSenseResidue"/>
	<!-- how deal with custom fields?? -->

	<xsl:call-template name="DoPicture"/>

	<!-- do the LexicalRelations -->
	<xsl:call-template name="DoLexicalRelations"/>

	<!-- and now add the variants -->
	<!-- Variants have to be done outside this loop
	<xsl:call-template name="DoVariants">
		<xsl:with-param name="lexElement" select="sn"/>
		<xsl:with-param name="GUID" select="$GUID"/>
	</xsl:call-template>
	-->


  </xsl:template>

<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoNestedSenses
	process Senses one level deeper in the hierarchy
		Parameters: level (last level processed - if level is "1.2" then this template processes 1.2.1, 1.2.2, etc.)
							pos (part of speech value specified for last level processed)
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoNestedSenses">
	<xsl:param name="level"/>
	<xsl:param name="pos"/>
	<!-- check all senses to find the ones embedded within $level -->
	<xsl:for-each select="Sense[$level=user:SenseNumberBase(sn)]">
	  <LexSense>
		<!-- if a sn element exists then put out its value in a 'no' attribute -->
		<xsl:if test="sn">
		  <xsl:attribute name="no"><xsl:value-of select="sn"/></xsl:attribute>
		</xsl:if>
		<!-- put out all the subfields, excluding sn -->
		<xsl:call-template name="DoSenseFields">
		  <xsl:with-param name="pos" select="$pos"/>
		</xsl:call-template>
		<!-- if a sn element exists then put out the next lower hierarchy level -->
		<xsl:if test="sn">
		  <!-- the current sense number will become the base for the next lower hierarchy level -->
		  <xsl:variable name="level2" select="sn"/>
		  <!-- if this level has a pos, then pass it to the lower levels.  otherwise, pass a higher pos to the lower levels. -->
		  <xsl:variable name="pos2">
			<xsl:if test="pos">
			  <xsl:value-of select="pos"/>
			</xsl:if>
			<xsl:if test="not(pos)">
			  <xsl:value-of select="$pos"/>
			</xsl:if>
		  </xsl:variable>
		  <!-- move up one level in the XML because each instance of DoNestedSenses runs at the same level -->
		  <xsl:for-each select="..">

			<!-- check to make sure embedded senses exist before putting out the Senses element -->
			<xsl:if test="Sense[$level2=user:SenseNumberBase(sn)]">
			  <Senses>
				<xsl:call-template name="DoNestedSenses">
				  <xsl:with-param name="level" select="$level2"/>
				  <xsl:with-param name="pos" select="$pos2"/>
				</xsl:call-template>
			  </Senses>
			</xsl:if>
		  </xsl:for-each>
		</xsl:if>
		<!-- Custom fields in this Sense -->
		<xsl:call-template name="JoinCustomOnFwid"/>
	  </LexSense>
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
	<!-- If a Sense doesn't exist then don't put out an empty Senses element -->
	<xsl:if test="Sense">
	<Senses>
	  <!-- Put out the senses, nested as appropriate, this starts the recursive calls -->
	  <xsl:call-template name="DoNestedSenses">
		<xsl:with-param name="level"/>
	  </xsl:call-template>
	</Senses>
	</xsl:if>
  </xsl:template>

  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoMainEntryCrossRef
	process main entry cross references
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoMainEntryCrossRef">
	<xsl:if test="meref">
		<xsl:variable name="tdata"><xsl:value-of select="meref"/></xsl:variable>
	  <xsl:element name="LexEntryRef">
	  <xsl:element name="ComponentLexemes">
		  <xsl:attribute name="type"><xsl:value-of select="user:GetMainEntriesOrSensesType($tdata)"/></xsl:attribute>
		  <xsl:attribute name="ws"><xsl:value-of select="meref/@ws"/></xsl:attribute>
		  <xsl:value-of select="$tdata"/>
	  </xsl:element>
	  </xsl:element>
	</xsl:if>
  </xsl:template>

  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoLexicalRelations
	process relations
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoLexicalRelations">
	<xsl:if test="lxrel or Function or funold">
	  <xsl:element name="LexicalRelations">
		<!-- Do all the lxrel elements -->
		<xsl:for-each select="lxrel">
		  <xsl:element name="Link">
			<xsl:attribute name="wsa"><xsl:text>en</xsl:text></xsl:attribute>
			<xsl:attribute name="abbr"><xsl:value-of select="@func"/></xsl:attribute>
			<xsl:attribute name="wsv"><xsl:value-of select="@ws"/></xsl:attribute>
			<xsl:attribute name="sense"><xsl:value-of select="."/></xsl:attribute>
		  </xsl:element>
		</xsl:for-each>
		<!-- Do all the Function entries -->
		<xsl:for-each select="Function">
		  <xsl:if test="func and funlex">
		  <xsl:element name="Link">
			<xsl:attribute name="wsa"><xsl:value-of select="func/@ws"/></xsl:attribute>
			<xsl:attribute name="abbr"><xsl:value-of select="func"/></xsl:attribute>
			<xsl:attribute name="wsv"><xsl:value-of select="funlex/@ws"/></xsl:attribute>
			<xsl:attribute name="sense"><xsl:value-of select="funlex"/></xsl:attribute>
		  </xsl:element>
		  </xsl:if>
		</xsl:for-each>
		<!-- Do all the 'funold' elements -->
		<xsl:for-each select="funold">
		  <xsl:element name="Link">
			<xsl:attribute name="wsa"><xsl:text>en</xsl:text></xsl:attribute>
			<xsl:variable name="data"><xsl:value-of select="."/></xsl:variable>
			<xsl:variable name="predata"><xsl:value-of select="substring-before($data, '=')"/></xsl:variable>
			<xsl:variable name="postdata"><xsl:value-of select="substring-after($data, '=')"/></xsl:variable>
			<xsl:attribute name="abbr"><xsl:value-of select="user:Trim($predata)"/></xsl:attribute>
			<xsl:attribute name="wsv"><xsl:value-of select="@ws"/></xsl:attribute>
			<xsl:attribute name="sense"><xsl:value-of select="user:Trim($postdata)"/></xsl:attribute>
		  </xsl:element>
		</xsl:for-each>
	  </xsl:element>
	</xsl:if>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoCrossReferences
	process
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoCrossReferences">
	<xsl:if test="cref or scref or lxrel or Function or funold">
	  <xsl:element name="CrossReferences">
		<xsl:for-each select="cref">
		  <xsl:element name="Link">
			<xsl:attribute name="wsa"><xsl:text>en</xsl:text></xsl:attribute>
			<xsl:attribute name="abbr"><xsl:value-of select="@func"/></xsl:attribute>
			<xsl:attribute name="wsv"><xsl:value-of select="@ws"/></xsl:attribute>
			<xsl:attribute name="entry"><xsl:value-of select="."/></xsl:attribute>
		  </xsl:element>
		</xsl:for-each>
		<xsl:for-each select="scref">
		  <xsl:element name="Link">
			<xsl:attribute name="wsa"><xsl:text>en</xsl:text></xsl:attribute>
			<xsl:attribute name="abbr"><xsl:value-of select="@func"/></xsl:attribute>
			<xsl:attribute name="wsv"><xsl:value-of select="@ws"/></xsl:attribute>
			<xsl:attribute name="entry"><xsl:value-of select="."/></xsl:attribute>
		  </xsl:element>
		</xsl:for-each>
		<!-- Do all the lxrel elements -->
		<xsl:for-each select="lxrel">
		  <xsl:element name="Link">
			<xsl:attribute name="wsa"><xsl:text>en</xsl:text></xsl:attribute>
			<xsl:attribute name="abbr"><xsl:value-of select="@func"/></xsl:attribute>
			<xsl:attribute name="wsv"><xsl:value-of select="@ws"/></xsl:attribute>
			<xsl:attribute name="entry"><xsl:value-of select="."/></xsl:attribute>
		  </xsl:element>
		</xsl:for-each>
		<!-- Do all the Function entries -->
		<xsl:for-each select="Function">
		  <xsl:if test="func and funlex">
		  <xsl:element name="Link">
			<xsl:attribute name="wsa"><xsl:value-of select="func/@ws"/></xsl:attribute>
			<xsl:attribute name="abbr"><xsl:value-of select="func"/></xsl:attribute>
			<xsl:attribute name="wsv"><xsl:value-of select="funlex/@ws"/></xsl:attribute>
			<xsl:attribute name="entry"><xsl:value-of select="funlex"/></xsl:attribute>
		  </xsl:element>
		  </xsl:if>
		</xsl:for-each>
		<!-- Do all the 'funold' elements -->
		<xsl:for-each select="funold">
		  <xsl:element name="Link">
			<xsl:attribute name="wsa"><xsl:text>en</xsl:text></xsl:attribute>
			<xsl:variable name="data"><xsl:value-of select="."/></xsl:variable>
			<xsl:variable name="predata"><xsl:value-of select="substring-before($data, '=')"/></xsl:variable>
			<xsl:variable name="postdata"><xsl:value-of select="substring-after($data, '=')"/></xsl:variable>
			<xsl:attribute name="abbr"><xsl:value-of select="user:Trim($predata)"/></xsl:attribute>
			<xsl:attribute name="wsv"><xsl:value-of select="@ws"/></xsl:attribute>
			<xsl:attribute name="entry"><xsl:value-of select="user:Trim($postdata)"/></xsl:attribute>
		  </xsl:element>
		</xsl:for-each>
	  </xsl:element>
	</xsl:if>
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
xx-Jun-2005    dlh - adding residue processing.
08-Sep-2005	   dlh - changing <Allomorphs> to <LexemeForm>
08-Aug-2006    Bev - adding nested senses
08-Aug-2006    Bev - fixing LT-4826 in join3
================================================================
 -->
