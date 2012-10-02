<?xml version="1.0" encoding="UTF-8"?>
<!--This file is no longer needed and is kept for archive purposes because it houses another methodology for getting lists and their items
namely, that of using RTF \listtable.-->
<!-- edited with XML Spy v4.4 U (http://www.xmlspy.com) by Larr Hayashi (private) -->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:msxsl="urn:schemas-microsoft-com:xslt" xmlns:user="http://mycompany.com/mynamespace">
	<!--Problems:
1. Use nesting to percolate. However saves in Word as unnested.
2. Too many styles for language encodings. Mismatch in WorldPad between languages and ws.
3. Number of style not appearing correctly in RTF. -->
	<!--Assumptions about style percolation:
Paragraph styles and their defined character formats percolate into character styles which percolate into language styles (also character styles)-->
	<msxsl:script language="javascript" implements-prefix="user"><![CDATA[
	function getCharCodes(RunString){
	   var Length = RunString.length;
	   var RTF = "";//initialize variable RTF as empty.
		for (i = 0; i < Length; i++) {
		   var CurrentCharCode = RunString.charCodeAt(i);
		   {if (CurrentCharCode > 127) //127 is decimal value for U+007F.
			   RTFAdd  = "\\uc0\\u".concat(CurrentCharCode," " );
		  else
			RTFAdd = RunString.charAt(i)
		  RTF = RTF.concat(RTFAdd)
		  }
		}
		  return RTF;
		 }
		   ]]></msxsl:script>
	<xsl:output method="text" encoding="UTF-8"/>
	<xsl:strip-space elements="*"/>
	<xsl:preserve-space elements="Run"/>
	<xsl:template match="/">
		<!-- RTF header has the following syntax:
	\rtfN - RTF version
	<charset> - ANSI, Macintosh, IBM, IBM OS/2, or Unicode (ansicpgN)
	\deff? - default font reference from font table
	<fonttbl> - define each font available in the document
	<filetbl>?
	<colortbl>?
	<stylesheet>?
	<listtables>?
	<revtbl>?
	-->
		<!-- "root" sets \rtfN, <charset>, and \deffN properties -->
		<xsl:text>{\rtf1\ansi\ansicpg1252 \deff0\deflang1033\delangfe1033</xsl:text>
		<xsl:call-template name="Lang"/>
		<xsl:call-template name="Style"/>
		<xsl:call-template name="List"/>
		<xsl:call-template name="ListOverride"/>
		<xsl:call-template name="Section"/>
		<xsl:call-template name="Body"/>
	</xsl:template>
	<!-- Wpdocs/Languages transforms font table, and color table is preset -->
	<xsl:template name="Lang" match="//Languages">
		<!-- Wpdocs/Languages transforms font table, and color table is preset -->
		<!-- the font table group has the following syntax:
		<fontnum>       - defines each font number available in the document uniquely -
						- this is defined for each language encoding by 100 + its sequence number in the list of encodings for serif and 200 + its sequence number for sans serif (arbitrarily)
		<fontfamily>    - attempt to intelligently choose fonts if exact font is not on reader - in FW the user can define a serif=\froman or sansserif= \fswiss font for each language encoding
		<fcharset>?     - specifies character set of the font in the table - for now we will assume to use the ANSI charset because we are still using ANSI fonts.
		<fprq>?         - specifies pitch of font (fixed/variable) - optional left out
		<panose>? : <data>
						- destination keyword , which is a 10-byte panose standard specification
		<nontaggedname>?
		<fontemb>? :    <fonttype>
						<fontfname>? : <codepage>?
						<data>?
						- embedded fonts can be specified by a filename, or the
						actual font data may be located inside the group
		<codepage>?     - a font may have a different character set than that of
						the document RTF describes this with a \cpgN control word
		<fontname>        - name of font #PCDATA
		<fontaltname>?    - alternative fontname to use if the specified font is not available
		-->
		<xsl:text>&#13;{\fonttbl{\f0\froman\fcharset0\fprq2 Times New Roman;}&#13;</xsl:text>
		<xsl:text>{\f1\fswiss\fcharset0\fprq2 Arial;}&#13;</xsl:text>
		<xsl:text>{\f2\ftech\fcharset0\fprq2 Wingdings;}&#13;</xsl:text>
		<!-- This section defines a unique number for each encoding -->
		<xsl:for-each select="//LgEncoding">
			<xsl:variable name="serifFontNum" select="position() + 100"/>
			<xsl:variable name="sansSerifFontNum" select="position() + 200"/>
			<!-- Output the serif font number and information.-->
			<xsl:text>{\f</xsl:text>
			<xsl:value-of select="position() + 100"/>
			<xsl:text>\froman\fcharset0\fprq2 </xsl:text>
			<xsl:value-of select="WritingSystems24/LgWritingSystem/DefaultSerif25/Uni"/>
			<xsl:text>;}&#13;</xsl:text>
			<!--Output the sans serif font number and information.-->
			<xsl:text>{\f</xsl:text>
			<xsl:value-of select="position() + 200"/>
			<xsl:text>\fswiss\fcharset0\fprq2 </xsl:text>
			<xsl:value-of select="WritingSystems24/LgWritingSystem/DefaultSansSerif25/Uni"/>
			<xsl:text>;}&#13;</xsl:text>
		</xsl:for-each>
		<xsl:text>&#13;</xsl:text>
		<!-- the color table group defines the screen and character colors,
		and has the following syntax:
		<colordef> : \red? & \green? & \blue?;
		-->
		<xsl:text>}&#13;{\colortbl;
\blue0\green0\red0;
\blue0\green47\red144;
\blue0\green47\red47;
\blue0\green47\red0;
\blue96\green47\red0;
\blue127\green0\red0;
\blue144\green47\red47;
\blue47\green47\red47;
\blue0\green0\red127;
\blue0\green96\red255;
\blue0\green127\red127;
\blue0\green127\red0;
\blue127\green127\red0;
\blue255\green0\red0;
\blue144\green96\red96;
\blue127\green127\red127;
\blue0\green0\red255;
\blue0\green144\red255;
\blue0\green192\red144;
\blue96\green144\red47;
\blue192\green192\red47;
\blue255\green96\red47;
\blue127\green0\red127;
\blue144\green144\red144;
\blue255\green0\red255;
\blue0\green192\red255;
\blue0\green255\red255;
\blue0\green255\red0;
\blue255\green255\red0;
\blue255\green192\red0;
\blue96\green47\red144;
\blue192\green192\red192;
\blue192\green144\red255;
\blue144\green192\red255;
\blue144\green255\red255;
\blue207\green255\red207;
\blue255\green255\red192;
\blue255\green192\red144;
\blue255\green144\red192;
\blue255\green255\red255;}</xsl:text>
	</xsl:template>
	<xsl:template name="Style" match="//Styles">
		<!-- WpDoc/Styles transforms stylesheet for each document
	the style sheet group has the following syntax:
	<styledef>?     - paragraph, character or section style designation
	<keycode>? : <keys>
					- describe a quick key select to the style name
	<formatting> : (<brdrdef>|<parfmt>|<apoctl>|<tabdef>|<chrfmt>)+
					- any paragraph may have its own border attributes
					- paragraph formatting control words specify generic properties
					- apoctl specify the location of a paragragh on a page
					- any paragrapgh may have its own tab set
					- font(character) formatting control words
	<additive>?     - indicates the character style attributes are to be added to
					the current paragraph style
	<based>?        - defines the style on which the current style is based (default 222-none)
	<next>?         - defines next style associated with current style. If omitted,
					the next is the current
	<autoupd>?      - automatically update style
	<hidden>?       - style does not appear in the drop down list
	<personal>?     - style is a personal e-mail style
	<compose>?      - style is the e-mail compose style
	<reply>?        - style is the e-mail reply style
	<stylename>?    - name #PCDATA
	-->
		<xsl:text>&#13;{\stylesheet</xsl:text>
		<xsl:for-each select="//StStyle">
			<!-- include styles for paragraph types -->
			<xsl:if test="Type17/Integer[@val=0]">
				<xsl:if test="position()=1 or Rules17/Prop[not(@bulNumScheme)]">
					<xsl:text>&#13;{\s</xsl:text>
					<xsl:number value="position() - 1"/>
					<!-- <brdrdef> : border properties included in the syle -->
					<!-- <apoctl> location properties included in the style -->
					<!-- <tabdef> tab sets included in the style -->
					<xsl:for-each select="Rules17/Prop">
						<xsl:call-template name="ParagraphFormatting"/>
					</xsl:for-each>
					<!-- <chrfmt> character formatting properties included in the style -->
					<!-- ******************************CHARACTER FORMATTING FOR PARAGRAPH STYLE DEFINED HERE****************************************************************************************-->
					<xsl:for-each select="Rules17/Prop/WsStyles9999/WsProp[1]">
						<xsl:call-template name="FontInfo"/>
						<xsl:call-template name="CharacterFormatting"/>
					</xsl:for-each>
				</xsl:if>
			</xsl:if>
			<!-- **************************************CHARACTER STYLES DEFINED HERE********************************************************************************-->
			<xsl:if test="Type17/Integer[@val=1]">
				<xsl:text>&#13;{\*\cs</xsl:text>
				<xsl:number value="position() - 1"/>
				<xsl:for-each select="Rules17/Prop">
					<xsl:call-template name="CharacterFormatting"/>
				</xsl:for-each>
				<xsl:for-each select="Rules17/Prop/WsStyles9999/WsProp[1]">
					<xsl:call-template name="FontInfo"/>
					<xsl:call-template name="CharacterFormatting"/>
				</xsl:for-each>
				<!--Used in a character style definition ('{\*'\cs...'}'). Indicates
					that character style attributes are to be added to the current
					paragraph style attributes, rather than setting the paragraph
					attributes to only those defined in the character style definition.-->
				<xsl:text>\additive</xsl:text>
				<!-- <chrfmt> character formatting properties included in the style -->
			</xsl:if>
			<!-- **************************************FOLLOWING APPLIES TO BOTH CHARACTER AND PARAGRAPH STYLES********************************************************************************-->
			<!-- Based on another style -->
			<xsl:if test="BasedOn17/Uni[.!='']">
				<xsl:if test="BasedOn17/Uni[.!='Default Paragraph Characters']">
					<xsl:text>\sbasedon</xsl:text>
					<xsl:variable name="basedOn" select="BasedOn17/Uni[.]"/>
					<xsl:for-each select="//StStyle/Name17/Uni[.=$basedOn]">
						<xsl:number value="position() - 1"/>
					</xsl:for-each>
				</xsl:if>
			</xsl:if>
			<!-- Next style associated with the current style
			if omitted, the next style is the current style -->
			<xsl:if test="Rules17/Prop[not(@bulNumScheme)]">
				<xsl:variable name="next" select="Next17/Uni"/>
				<xsl:choose>
					<xsl:when test="Next17/Uni[.!='']">
						<xsl:text>\snext</xsl:text>
						<xsl:for-each select="//StStyle/Name17/Uni[.=$next]">
							<xsl:number value="position()-1"/>
						</xsl:for-each>
					</xsl:when>
					<xsl:otherwise>
						<xsl:text>\snext</xsl:text>
						<xsl:number value="position()-1"/>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:if>
			<xsl:choose>
				<xsl:when test="Rules17/Prop[not(@bulNumScheme)]">
					<xsl:text>&#32;</xsl:text>
					<xsl:value-of select="Name17/Uni"/>
					<xsl:text>;}</xsl:text>
				</xsl:when>
				<xsl:when test="position()=1">
					<xsl:text>&#32;</xsl:text>
					<xsl:value-of select="Name17/Uni"/>
					<xsl:text>;}</xsl:text>
				</xsl:when>
			</xsl:choose>
		</xsl:for-each>
		<!-- **************************************CREATE A CHAR STYLE FOR EACH LANGUAGE BASED ON NORMAL********************************************************************************-->
		<!--This section goes through the Normal paragraph Style and creates a language character style for each language represented.-->
		<xsl:for-each select="//StStyle[Name17/Uni[.='Normal']]">
			<xsl:for-each select="Rules17/Prop/WsStyles9999/WsProp">
				<xsl:text>&#13;{\*\cs</xsl:text>
				<xsl:value-of select="position() + 500"/>
				<xsl:text>\additive</xsl:text>
				<!--When font family is equal to serif then use font 100 + sequence number of encoding, otherwise use font 200 + sequence number of encoding-->
				<xsl:choose>
					<xsl:when test="@fontFamily='&lt;default serif&gt;'">
						<xsl:variable name="Encoding" select="@enc"/>
						<xsl:text>\f</xsl:text>
						<xsl:for-each select="//Languages/LgEncoding[@id=$Encoding]">
							<xsl:value-of select="position() + 100"/>
						</xsl:for-each>
					</xsl:when>
					<xsl:when test="@fontFamily='&lt;default san serif&gt;'">
						<xsl:variable name="Encoding" select="@enc"/>
						<xsl:text>\f</xsl:text>
						<xsl:for-each select="//Languages/LgEncoding[@id=$Encoding]">
							<xsl:value-of select="position() + 200"/>
						</xsl:for-each>
					</xsl:when>
				</xsl:choose>
				<xsl:if test="@fontsize">
					<xsl:text>\fs</xsl:text>
					<xsl:if test="@fontsizeUnit='mpt'">
						<xsl:number value="@fontsize div(500)"/>
					</xsl:if>
					<xsl:if test="@fontsizeUnit='rel'">
						<xsl:number value="@fontsize div(500)"/>
					</xsl:if>
				</xsl:if>
				<xsl:call-template name="CharacterFormatting"/>
				<!--Put name of style here as Encoding name-->
				<xsl:variable name="Encoding" select="@enc"/>
				<xsl:text>&#32;</xsl:text>
				<xsl:for-each select="//Languages/LgEncoding[@id=$Encoding]">
					<!--xsl:value-of select="WritingSystems24/LgWritingSystem[1]/Name25/Str"/-->
					<xsl:value-of select="@id"/>
				</xsl:for-each>
				<xsl:text>;}&#13;</xsl:text>
			</xsl:for-each>
			<xsl:for-each select="Rules17/Prop/BulNumFontInfo">
				<xsl:call-template name="BulNumFormat"/>
			</xsl:for-each>
		</xsl:for-each>
		<xsl:text>}</xsl:text>
	</xsl:template>
	<!-- **************************************LIST TEMPLATE***********************************************************************************-->
	<xsl:template name="List">
		<!-- The listtable is a list of lists. Each list contains a number of list properties that
		pertain to the entire list, and a list of levels, each of which contains properties that
		pertain to that level only.
		<listtable> - {\*\listtable <list>+}
		<list>      - properties that pertain to the entire list .. <listlevel>+
		<listlevel> - <number> <justification> <leveltext> <levelnumbers> <chrfmt> ...
					- properties pertaining to one level only
	-->
		<xsl:text>&#13;{\*\listtable</xsl:text>
		<xsl:variable name="count">0</xsl:variable>
		<xsl:for-each select="//Styles/StStyle">
			<xsl:if test="Rules17/Prop/@bulNumScheme">
				<xsl:text>{\list\listtemplateid</xsl:text>
				<xsl:number value="position() + 2500 - 1"/>
				<xsl:text>\listhybrid</xsl:text>
				<xsl:call-template name="Hybrid">
					<xsl:with-param name="HybridPath" select="Rules17/Prop"/>
					<xsl:with-param name="Id" select="position()*10 + 3100"/>
				</xsl:call-template>
				<xsl:call-template name="Hybrid">
					<xsl:with-param name="HybridPath" select="Rules17/Prop"/>
					<xsl:with-param name="Id" select="position()*10 + 3101"/>
				</xsl:call-template>
				<xsl:call-template name="Hybrid">
					<xsl:with-param name="HybridPath" select="Rules17/Prop"/>
					<xsl:with-param name="Id" select="position()*10 + 3102"/>
				</xsl:call-template>
				<xsl:call-template name="Hybrid">
					<xsl:with-param name="HybridPath" select="Rules17/Prop"/>
					<xsl:with-param name="Id" select="position()*10 + 3103"/>
				</xsl:call-template>
				<xsl:call-template name="Hybrid">
					<xsl:with-param name="HybridPath" select="Rules17/Prop"/>
					<xsl:with-param name="Id" select="position()*10 + 3104"/>
				</xsl:call-template>
				<xsl:call-template name="Hybrid">
					<xsl:with-param name="HybridPath" select="Rules17/Prop"/>
					<xsl:with-param name="Id" select="position()*10 + 3105"/>
				</xsl:call-template>
				<xsl:call-template name="Hybrid">
					<xsl:with-param name="HybridPath" select="Rules17/Prop"/>
					<xsl:with-param name="Id" select="position()*10 + 3106"/>
				</xsl:call-template>
				<xsl:call-template name="Hybrid">
					<xsl:with-param name="HybridPath" select="Rules17/Prop"/>
					<xsl:with-param name="Id" select="position()*10 + 3107"/>
				</xsl:call-template>
				<xsl:call-template name="Hybrid">
					<xsl:with-param name="HybridPath" select="Rules17/Prop"/>
					<xsl:with-param name="Id" select="position()*10 + 3108"/>
				</xsl:call-template>
				<xsl:text>{\listname </xsl:text>
				<xsl:value-of select="Name17/Uni"/>
				<xsl:text>;}</xsl:text>
				<xsl:text>\listid</xsl:text>
				<xsl:number value="position() + 2000 - 1"/>
				<xsl:text>}</xsl:text>
				<xsl:variable name="styleCount">
					<xsl:value-of select="$count = position() - 1"/>
				</xsl:variable>
			</xsl:if>
		</xsl:for-each>
		<xsl:for-each select="//Body/StTxtPara">
			<xsl:if test="StyleRules15/Prop/@bulNumScheme">
				<xsl:text>{\list\listtemplateid</xsl:text>
				<xsl:number value="position() + 2500 - 1"/>
				<xsl:text>\listhybrid</xsl:text>
				<xsl:call-template name="Hybrid">
					<xsl:with-param name="HybridPath" select="StyleRules15/Prop"/>
					<xsl:with-param name="Id" select="position()*10 + 4100"/>
				</xsl:call-template>
				<xsl:call-template name="Hybrid">
					<xsl:with-param name="HybridPath" select="StyleRules15/Prop"/>
					<xsl:with-param name="Id" select="position()*10 + 4101"/>
				</xsl:call-template>
				<xsl:call-template name="Hybrid">
					<xsl:with-param name="HybridPath" select="StyleRules15/Prop"/>
					<xsl:with-param name="Id" select="position()*10 + 4102"/>
				</xsl:call-template>
				<xsl:call-template name="Hybrid">
					<xsl:with-param name="HybridPath" select="StyleRules15/Prop"/>
					<xsl:with-param name="Id" select="position()*10 + 4103"/>
				</xsl:call-template>
				<xsl:call-template name="Hybrid">
					<xsl:with-param name="HybridPath" select="StyleRules15/Prop"/>
					<xsl:with-param name="Id" select="position()*10 + 4104"/>
				</xsl:call-template>
				<xsl:call-template name="Hybrid">
					<xsl:with-param name="HybridPath" select="StyleRules15/Prop"/>
					<xsl:with-param name="Id" select="position()*10 + 4105"/>
				</xsl:call-template>
				<xsl:call-template name="Hybrid">
					<xsl:with-param name="HybridPath" select="StyleRules15/Prop"/>
					<xsl:with-param name="Id" select="position()*10 + 4106"/>
				</xsl:call-template>
				<xsl:call-template name="Hybrid">
					<xsl:with-param name="HybridPath" select="StyleRules15/Prop"/>
					<xsl:with-param name="Id" select="position()*10 + 4107"/>
				</xsl:call-template>
				<xsl:call-template name="Hybrid">
					<xsl:with-param name="HybridPath" select="StyleRules15/Prop"/>
					<xsl:with-param name="Id" select="position()*10 + 4108"/>
				</xsl:call-template>
				<xsl:text>{\listname ;}</xsl:text>
				<xsl:text>\listid</xsl:text>
				<xsl:number value="position() + $count + 2000 - 1"/>
				<xsl:text>}</xsl:text>
			</xsl:if>
		</xsl:for-each>
		<xsl:text>}</xsl:text>
	</xsl:template>
	<!-- *********************************LIST OVERRIDE TEMPLATE************************************************************************-->
	<xsl:template name="ListOverride">
		<xsl:text>&#13;{\*\listoverridetable</xsl:text>
		<xsl:variable name="count">0</xsl:variable>
		<xsl:for-each select="//Styles/StStyle">
			<xsl:if test="Rules17/Prop/@bulNumScheme">
				<xsl:text>{\listoverride\listid</xsl:text>
				<xsl:number value="position() + 2000 - 1"/>
				<xsl:text>\listoverridecount0\ls</xsl:text>
				<xsl:number value="position() - 1"/>
				<xsl:text>}</xsl:text>
				<xsl:variable name="styleCount">
					<xsl:value-of select="$count = position() - 1"/>
				</xsl:variable>
			</xsl:if>
		</xsl:for-each>
		<xsl:for-each select="//Body/StTxtPara">
			<xsl:if test="StyleRules15/Prop/@bulNumScheme">
				<xsl:text>{\listoverride\listid</xsl:text>
				<xsl:number value="position() + $count + 2000 - 1"/>
				<xsl:text>\listoverridecount0\ls</xsl:text>
				<xsl:number value="position() + $count - 1"/>
				<xsl:text>}</xsl:text>
			</xsl:if>
		</xsl:for-each>
		<xsl:text>}</xsl:text>
	</xsl:template>
	<!-- **************************************SECTION TEMPLATE********************************************************************************-->
	<xsl:template name="Section" match="//PageSetup">
		<!-- WpDoc/PageSetup transforms document format, and section format
	<info>?     - contains information about the document (title, author, date, etc.)
	<docfmt>*   - specify the attributes of the document (margins,footnotes, etc.)
	<section> : <secfmt>* <hdrftr>? <para>+ (\sect <section>)
				- secfmt specifies section formatting properties
	-->
		<!-- defines the document format attributes -->
		<xsl:for-each select="//PageInfo">
			<xsl:text>\viewkind1 </xsl:text>
			<xsl:if test="TopMargin9999/Integer/@val">
				<xsl:text>\margt</xsl:text>
				<xsl:value-of select="TopMargin9999/Integer/@val div(50)"/>
			</xsl:if>
			<xsl:if test="BottomMargin9999/Integer/@val">
				<xsl:text>\margb</xsl:text>
				<xsl:value-of select="BottomMargin9999/Integer/@val div(50)"/>
			</xsl:if>
			<xsl:if test="LeftMargin9999/Integer/@val">
				<xsl:text>\margl</xsl:text>
				<xsl:value-of select="//LeftMargin9999/Integer/@val div(50)"/>
			</xsl:if>
			<xsl:if test="RightMargin9999/Integer/@val">
				<xsl:text>\margr</xsl:text>
				<xsl:value-of select="RightMargin9999/Integer/@val div(50)"/>
			</xsl:if>
			<xsl:if test="PageSize9999/Integer/@val">
				<xsl:text>\psz</xsl:text>
				<xsl:value-of select="PageSize9999/Integer/@val"/>
			</xsl:if>
			<xsl:if test="PageHeight9999/Integer/@val">
				<xsl:text>\paperh</xsl:text>
				<xsl:value-of select="PageHeight9999/Integer/@val div(50)"/>
			</xsl:if>
			<xsl:if test="PageWidth9999/Integer/@val">
				<xsl:text>\paperw</xsl:text>
				<xsl:value-of select="PageWidth9999/Integer/@val div(50)"/>
			</xsl:if>
			<xsl:if test="PageOrientation/Integer[@val=1]">\landscape</xsl:if>
			<!-- define the section format properties -->
			<xsl:text>\sectd </xsl:text>
			<xsl:if test="HeaderMargin9999/Integer/@val">
				<xsl:text>\headery</xsl:text>
				<xsl:value-of select="HeaderMargin9999/Integer/@val div(50)"/>
			</xsl:if>
			<xsl:if test="FooterMargin9999/Integer/@val">
				<xsl:text>\footery</xsl:text>
				<xsl:value-of select="FooterMargin9999/Integer/@val div(50)"/>
			</xsl:if>
			<xsl:if test="Footer9999/Str/Run[.]">
				<xsl:text>{\footer </xsl:text>
				<xsl:call-template name="Parse">
					<xsl:with-param name="string" select="."/>
				</xsl:call-template>
				<xsl:text>\par}</xsl:text>
			</xsl:if>
		</xsl:for-each>
	</xsl:template>
	<!-- **************************************BODY TEMPLATE********************************************************************************-->
	<xsl:template name="Body" match="//Body">
		<!-- WpDoc/Body transforms the paragragh format and char format
	<pn>?       - numbering and bullets properties
	<brdrdef>?  - paragraph border properties
	<parfmt>*   - specify generic paragaph formating properties
	<apoctl>*   - paragraph page location properties
	<tabdef>?   - paragraph tab properties
	<shading>?  - character shading properties
	<char> : (<chrfmt>* <char>)|(<ltrrun>|<rtlrun>)
				- character format properties | associated character properties
	-->
		<!-- ************** FOR EACH PARAGRAPH IN THE TEXT ... ****************-->
		<xsl:text>{\*\pnseclvl1\pnucrm\pnstart1\pnindent720\pnhang{\pntxta .}}</xsl:text>
		<xsl:text>{\*\pnseclvl2\pnucltr\pnstart1\pnindent720\pnhang{\pntxta .}}</xsl:text>
		<xsl:text>{\*\pnseclvl3\pndec\pnstart1\pnindent720\pnhang{\pntxta .}}</xsl:text>
		<xsl:text>{\*\pnseclvl4\pnlcltr\pnstart1\pnindent720\pnhang{\pntxta )}}</xsl:text>
		<xsl:text>{\*\pnseclvl5\pndec\pnstart1\pnindent720\pnhang{\pntxtb (}{\pntxta )}}</xsl:text>
		<xsl:text>{\*\pnseclvl6\pnlcltr\pnstart1\pnindent720\pnhang{\pntxtb (}{\pntxta )}}</xsl:text>
		<xsl:text>{\*\pnseclvl7\pnlcrm\pnstart1\pnindent720\pnhang{\pntxtb (}{\pntxta )}}</xsl:text>
		<xsl:text>{\*\pnseclvl8\pnlcltr\pnstart1\pnindent720\pnhang{\pntxtb (}{\pntxta )}}</xsl:text>
		<xsl:text>{\*\pnseclvl9\pnlcrm\pnstart1\pnindent720\pnhang{\pntxtb (}{\pntxta )}}</xsl:text>
		<xsl:for-each select="//StTxtPara">
			<!--If a style is specified for the paragraph, then we apply the properties of that style to the run????-->
			<xsl:choose>
				<xsl:when test="StyleRules15/Prop/@namedStyle">
					<xsl:call-template name="Run">
						<xsl:with-param name="styleRule" select="StyleRules15/Prop/@namedStyle"/>
						<xsl:with-param name="bulNum" select="StyleRules15/Prop/@bulNumScheme"/>
					</xsl:call-template>
				</xsl:when>
				<!--If a style is not specified, then we use the paragraph properties of the previous paragraph.-->
				<xsl:otherwise>
					<xsl:call-template name="Run">
						<xsl:with-param name="styleRule" select="preceding-sibling::StyleRules15/Prop/@nameStyle"/>
						<xsl:with-param name="bulNum" select="preceding-sibling::StyleRules15/Prop/@bulNumScheme"/>
					</xsl:call-template>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:for-each>
		<xsl:text>}</xsl:text>
	</xsl:template>
	<!-- **************************************RUN TEMPLATE - ... THIS NEEDS WORK ********************************************************************************-->
	<xsl:template name="Run" match="//StTxtPara">
		<xsl:param name="styleRule"/>
		<xsl:param name="bulNum">-1</xsl:param>
		<xsl:text>\pard \plain</xsl:text>
		<xsl:for-each select="//StStyle[Name17/Uni[.=$styleRule]]">
			<xsl:choose>
				<xsl:when test="not($bulNum) and not(Rules17/Prop/@bulNumScheme)">
					<!-- insert style formatting -->
					<xsl:text>\s</xsl:text>
					<xsl:call-template name="Counter">
						<xsl:with-param name="nmStl" select="$styleRule"/>
					</xsl:call-template>
				</xsl:when>
				<xsl:when test="not($bulNum) and Rules17/Prop/@bulNumScheme">
					<!-- insert style from normal and bullet/number list format -->
					<xsl:text>\s1000 \ls</xsl:text>
					<xsl:call-template name="Counter">
						<xsl:with-param name="nmStl" select="$styleRule"/>
					</xsl:call-template>
				</xsl:when>
				<xsl:when test="$bulNum &gt; -1">
					<!-- insert style formatting -->
					<xsl:text>\s</xsl:text>
					<xsl:call-template name="Counter">
						<xsl:with-param name="nmStl" select="$styleRule"/>
					</xsl:call-template>
					<!-- insert bullet/number list format -->
					<xsl:text>\s1000 \ls</xsl:text>
					<xsl:for-each select="//StTxtPara[StyleRules15/Prop[@bulNumScheme=$bulNum]]">
						<xsl:call-template name="ListCounter">
							<xsl:with-param name="bulVal" select="$bulNum"/>
						</xsl:call-template>
					</xsl:for-each>
					<!-- add paragraph formatting from text definition -->
					<xsl:for-each select="//StTxtPara/StyleRules15/Prop[@bulNumScheme=$bulNum]">
						<xsl:call-template name="ParagraphFormatting"/>
					</xsl:for-each>
				</xsl:when>
			</xsl:choose>
		</xsl:for-each>
		<!-- **************************************REAL RUN TEMPLATE - ... CF. ABOVE ********************************************************************************-->
		<xsl:for-each select="*/Str/Run">
			<xsl:variable name="style" select="@namedStyle"/>
			<xsl:variable name="lgEnc" select="@enc"/>
			<!-- insert default language character formatting -->
			<xsl:call-template name="LgEncode">
				<xsl:with-param name="encoding" select="$lgEnc"/>
			</xsl:call-template>
			<!-- insert named style character formatting -->
			<xsl:if test="$style!=''">
				<xsl:for-each select="//StStyle[Name17/Uni[.=$style]]">
					<xsl:choose>
						<xsl:when test="BasedOn17/Uni[.!='']">
							<xsl:text>\cs</xsl:text>
							<xsl:call-template name="Counter">
								<xsl:with-param name="nmStl" select="BasedOn17/Uni"/>
							</xsl:call-template>
							<!-- insert character formatting from the 'BasedOn' style -->
							<xsl:call-template name="Char">
								<xsl:with-param name="based" select="BasedOn17/Uni"/>
							</xsl:call-template>
						</xsl:when>
						<xsl:otherwise>
							<xsl:text>\cs</xsl:text>
							<xsl:call-template name="Counter">
								<xsl:with-param name="nmStl" select="$style"/>
							</xsl:call-template>
							<!-- insert character formatting from the 'namedStyle' style -->
							<xsl:call-template name="Char">
								<xsl:with-param name="based" select="$style"/>
							</xsl:call-template>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:for-each>
			</xsl:if>
			<!-- insert any manual character formatting -->
			<xsl:call-template name="FontInfo"/>
			<xsl:call-template name="CharacterFormatting"/>
			<xsl:text>&#32;</xsl:text>
			<xsl:value-of select="user:getCharCodes(string(.))"/>
			<xsl:choose>
				<xsl:when test="position()=last()">}\par</xsl:when>
				<xsl:otherwise>}</xsl:otherwise>
			</xsl:choose>
		</xsl:for-each>
	</xsl:template>
	<!-- **************************************REFERENCE THE CHAR STYLE USED FOR EACH INSTANCE OF A LANGUAGE ENCODING  ********************************************************************************-->
	<!--THIS NEEDS WORK !!!!-->
	<xsl:template name="LgEncode" match="//StStyle">
		<xsl:param name="encoding"/>
		<xsl:for-each select="//StStyle[Name17/Uni[.='Normal']]">
			<xsl:for-each select="Rules17/Prop/WsStyles9999/WsProp[@enc=$encoding]">
				<!--Put font style name here-->
				<xsl:text>{\cs</xsl:text>
				<xsl:value-of select="1 + count(preceding-sibling::*) + 500"/>
				<xsl:text>\f</xsl:text>
				<!-- When font family is equal to serif then use font 100 +
					sequence number of encoding, otherwise use font 200 +
					sequence number of encoding -->
				<xsl:choose>
					<xsl:when test="@fontFamily='&lt;default serif&gt;'">
						<xsl:for-each select="//Languages/LgEncoding[@id=$encoding]">
							<xsl:value-of select="1 + count(preceding-sibling::*) + 100"/>
						</xsl:for-each>
					</xsl:when>
					<xsl:when test="@fontFamily='&lt;default san serif&gt;'">
						<xsl:for-each select="//Languages/LgEncoding[@id=$encoding]">
							<xsl:value-of select="1 + count(preceding-sibling::*) + 200"/>
						</xsl:for-each>
					</xsl:when>
				</xsl:choose>
				<xsl:if test="@fontsize">
					<xsl:text>\fs</xsl:text>
					<xsl:if test="@fontsizeUnit='mpt'">
						<xsl:number value="@fontsize div(500)"/>
					</xsl:if>
					<xsl:if test="@fontsizeUnit='rel'">
						<xsl:number value="@fontsize div(500)"/>
					</xsl:if>
				</xsl:if>
				<xsl:call-template name="CharacterFormatting"/>
			</xsl:for-each>
		</xsl:for-each>
	</xsl:template>
	<!-- **************************************Grab the character style format properties ******************************-->
	<xsl:template name="Char" match="//StStyle">
		<xsl:param name="based"/>
		<!--First grab the font properties of the based on style-->
		<xsl:for-each select="//StStyle[Name17/Uni[.=$based]]">
			<xsl:for-each select="Rules17/Prop">
				<xsl:call-template name="FontInfo"/>
				<xsl:call-template name="CharacterFormatting"/>
			</xsl:for-each>
			<xsl:for-each select="Rules17/Prop/WsStyles9999/WsProp[1]">
				<xsl:call-template name="FontInfo"/>
				<xsl:call-template name="CharacterFormatting"/>
			</xsl:for-each>
		</xsl:for-each>
		<!--Then grab the font properties of the actual style-->
		<xsl:for-each select="Rules17/Prop">
			<xsl:call-template name="FontInfo"/>
			<xsl:call-template name="CharacterFormatting"/>
		</xsl:for-each>
		<xsl:for-each select="Rules17/Prop/WsStyles9999/WsProp[1]">
			<xsl:call-template name="FontInfo"/>
			<xsl:call-template name="CharacterFormatting"/>
		</xsl:for-each>
	</xsl:template>
	<!--************************************Bullet/Number Formatting Template*****************************************-->
	<xsl:template name="BulNumFormat" match="//StStyle[Rules17/Prop[BulNumFontInfo]]">
		<xsl:text>{\s1000 </xsl:text>
		<xsl:for-each select="//Rules17/Prop/BulNumFontInfo">
			<xsl:call-template name="FontInfo"/>
		</xsl:for-each>
		<xsl:for-each select="//Rules17/Prop[BulNumFontInfo]">
			<xsl:call-template name="ParagraphFormatting"/>
		</xsl:for-each>
		<xsl:text>\jclisttab</xsl:text>
		<xsl:text> BulNumInfo;}</xsl:text>
	</xsl:template>
	<!-- **************************************Paragraph Formatting TEMPLATE********************************************************************************-->
	<xsl:template name="ParagraphFormatting">
		<xsl:if test="@borderTop!=0">
			<xsl:text>\brdrt\brdrs\brdrw</xsl:text>
			<xsl:number value="@borderTop div(50)"/>
			<xsl:if test="@borderColor">
				<xsl:text>\brdrcf</xsl:text>
				<xsl:call-template name="MatchColorTable">
					<xsl:with-param name="WPXColor" select="@borderColor"/>
				</xsl:call-template>
			</xsl:if>
		</xsl:if>
		<xsl:if test="@borderBottom!=0">
			<xsl:text>\brdrb\brbrs\brdrw</xsl:text>
			<xsl:number value="@borderBottom div(50)"/>
			<xsl:if test="@borderColor">
				<xsl:text>\brdrcf</xsl:text>
				<xsl:call-template name="MatchColorTable">
					<xsl:with-param name="WPXColor" select="@borderColor"/>
				</xsl:call-template>
			</xsl:if>
		</xsl:if>
		<xsl:if test="@borderLeading!=0">
			<xsl:text>\brdrl\brbrs\brdrw</xsl:text>
			<xsl:number value="/@borderLeading div(50)"/>
			<xsl:if test="@borderColor">
				<xsl:text>\brdrcf</xsl:text>
				<xsl:call-template name="MatchColorTable">
					<xsl:with-param name="WPXColor" select="@borderColor"/>
				</xsl:call-template>
			</xsl:if>
		</xsl:if>
		<xsl:if test="@borderTrailing!=0">
			<xsl:text>\brdrr\brbrs\brdrw</xsl:text>
			<xsl:number value="@borderTrailing div(50)"/>
			<xsl:if test="@borderColor">
				<xsl:text>\brdrcf</xsl:text>
				<xsl:call-template name="MatchColorTable">
					<xsl:with-param name="WPXColor" select="@borderColor"/>
				</xsl:call-template>
			</xsl:if>
		</xsl:if>
		<!-- <parfmt> paragragh formatting properties included in the style -->
		<xsl:choose>
			<xsl:when test="@rightToLeft=0">\ltrpar</xsl:when>
			<xsl:when test="@rightToLeft=1">\rtlpar</xsl:when>
		</xsl:choose>
		<xsl:choose>
			<xsl:when test="@align='leading'">\ql</xsl:when>
			<xsl:when test="@align='left'">\ql</xsl:when>
			<xsl:when test="@align='center'">\qc</xsl:when>
			<xsl:when test="@align='right'">\qr</xsl:when>
			<xsl:when test="@align='trailing'">\qr</xsl:when>
			<xsl:when test="@align='justify'">\qj</xsl:when>
		</xsl:choose>
		<xsl:if test="@leadingIndent">
			<xsl:text>\lin</xsl:text>
			<xsl:number value="@leadingIndent div(50)"/>
			<xsl:if test="@bulNumScheme and not(@firstIndent)">
				<xsl:text>\fi</xsl:text>
				<xsl:number value="@leadingIndent div(-50)"/>
			</xsl:if>
		</xsl:if>
		<xsl:if test="@trailingIndent">
			<xsl:text>\rin</xsl:text>
			<xsl:number value="@trailingIndent div(50)"/>
		</xsl:if>
		<xsl:if test="@firstIndent">
			<xsl:text>\fi</xsl:text>
			<xsl:number value="@firstIndent div(50)"/>
			<xsl:if test="@bulNumScheme and not(@leadingIndent)">
				<xsl:text>\lin</xsl:text>
				<xsl:number value="@firstIndent div(-50)"/>
			</xsl:if>
		</xsl:if>
		<xsl:if test="@spaceBefore">
			<xsl:text>\sb</xsl:text>
			<xsl:number value="@spaceBefore div(50)"/>
		</xsl:if>
		<xsl:if test="@spaceAfter">
			<xsl:text>\sa</xsl:text>
			<xsl:number value="@spaceAfter div(50)"/>
		</xsl:if>
		<xsl:if test="@lineHeight">
			<xsl:if test="@lineHeightUnit='rel'">
				<xsl:text>\sl</xsl:text>
				<xsl:number value="@lineHeight div(50)"/>
			</xsl:if>
			<xsl:if test="@lineHeightUnit='mpt'">
				<xsl:text>\sl</xsl:text>
				<xsl:number value="@lineHeight div(50)"/>
			</xsl:if>
		</xsl:if>
	</xsl:template>
	<!-- **************************************FONT INFO TEMPLATE********************************************************************************-->
	<xsl:template name="FontInfo">
		<xsl:choose>
			<xsl:when test="@fontFamily='&lt;default serif&gt;'">\f0</xsl:when>
			<xsl:when test="@fontFamily='Times New Roman'">\f0</xsl:when>
			<xsl:when test="@fontFamily='&lt;default san serif&gt;'">\f1</xsl:when>
			<xsl:when test="@fontFamily='Ariel'">\f1</xsl:when>
			<xsl:when test="@fontFamily='Wingdings'">\f2</xsl:when>
			<xsl:when test="@fontFamily='Symbol'">\f3</xsl:when>
		</xsl:choose>
		<xsl:if test="@fontsize">
			<xsl:variable name="fsize" select="@fontsize"/>
			<xsl:text>\fs</xsl:text>
			<xsl:choose>
				<xsl:when test="contains($fsize, 'mpt')">
					<xsl:variable name="actualfsize" select="substring-before($fsize, 'mpt')"/>
					<xsl:number value="$actualfsize div(500)"/>
				</xsl:when>
				<xsl:when test="@fontsizeUnit='mpt'">
					<xsl:number value="@fontsize div(500)"/>
				</xsl:when>
				<!--xsl:when test="@fontsizeUnit='rel'">
					<xsl:number value="@fontsize div(500)"/>
				</xsl:when-->
				<xsl:otherwise>
					<xsl:number value="@fontsize div(500)"/>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:if>
	</xsl:template>
	<!-- **************************************CHARACTER FORMATTING TEMPLATE********************************************************************************-->
	<xsl:template name="CharacterFormatting">
		<xsl:if test="@backcolor">
			<xsl:choose>
				<xsl:when test="@backcolor='white'"/>
				<xsl:otherwise>
					<xsl:text>\chshdng0\chcbpat</xsl:text>
					<!-- For some reason this does not work xsl:text>\cb</xsl:text-->
					<xsl:call-template name="MatchColorTable">
						<xsl:with-param name="WPXColor" select="@backcolor"/>
					</xsl:call-template>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:if>
		<xsl:if test="@forecolor">
			<xsl:choose>
				<xsl:when test="@forecolor='black'"/>
				<xsl:otherwise>
					<xsl:text>\cf</xsl:text>
					<xsl:call-template name="MatchColorTable">
						<xsl:with-param name="WPXColor" select="@forecolor"/>
					</xsl:call-template>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:if>
		<xsl:choose>
			<xsl:when test="@italic='on'">\i</xsl:when>
			<!--xsl:when test="@italic='off'">\i0</xsl:when-->
			<xsl:when test="@italic='invert'">\i</xsl:when>
		</xsl:choose>
		<xsl:choose>
			<xsl:when test="@bold='on'">\b</xsl:when>
			<!--xsl:when test="@bold='off'">\b0</xsl:when-->
			<xsl:when test="@bold='invert'">\b</xsl:when>
			<!-- ?? -->
		</xsl:choose>
		<xsl:choose>
			<xsl:when test="@superscript='super'">\super</xsl:when>
			<xsl:when test="@superscript='sub'">\sub</xsl:when>
			<!--xsl:when test="@superscript='off'">\nosupersub</xsl:when-->
		</xsl:choose>
		<xsl:choose>
			<xsl:when test="@underline='continuous'">\ul</xsl:when>
			<!--xsl:when test="@underline='none'">\ulnone</xsl:when-->
			<xsl:when test="@underline='dotted'">\uld</xsl:when>
			<xsl:when test="@underline='dash'">\uldash</xsl:when>
			<xsl:when test="@underline='dash dot'">\uldashd</xsl:when>
			<xsl:when test="@underline='dash dot dot'">\uldashdd</xsl:when>
			<xsl:when test="@underline='double'">\uldb</xsl:when>
			<xsl:when test="@underline='heavy wave'">\ulhwave</xsl:when>
			<xsl:when test="@underline='long dash'">\ulldash</xsl:when>
			<xsl:when test="@underline='thick'">\ulth</xsl:when>
			<xsl:when test="@underline='thick dotted'">\ulthd</xsl:when>
			<xsl:when test="@underline='thick dash'">\ulthdash</xsl:when>
			<xsl:when test="@underline='thick dash dot'">\ulthdashd</xsl:when>
			<xsl:when test="@underline='thick dash dot dot'">\ulthdashdd</xsl:when>
			<xsl:when test="@underline='thick long dash'">\ulthldash</xsl:when>
			<xsl:when test="@underline='double wave'">\uldbwave</xsl:when>
			<xsl:when test="@underline='word'">\ulw</xsl:when>
			<xsl:when test="@underline='wave'">\ulwave</xsl:when>
		</xsl:choose>
		<xsl:if test="@undercolor">
			<xsl:choose>
				<xsl:when test="@undercolor='black'"/>
				<xsl:otherwise>
					<xsl:text>\ulc</xsl:text>
					<xsl:call-template name="MatchColorTable">
						<xsl:with-param name="WPXColor" select="@undercolor"/>
					</xsl:call-template>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:if>
	</xsl:template>
	<xsl:template name="MatchColorTable">
		<xsl:param name="WPXColor"/>
		<xsl:choose>
			<xsl:when test="$WPXColor='black'">1</xsl:when>
			<xsl:when test="$WPXColor='00002f90'">2</xsl:when>
			<xsl:when test="$WPXColor='00002f2f'">3</xsl:when>
			<xsl:when test="$WPXColor='00002f00'">4</xsl:when>
			<xsl:when test="$WPXColor='00602f00'">5</xsl:when>
			<xsl:when test="$WPXColor='007f0000'">6</xsl:when>
			<xsl:when test="$WPXColor='00902f2f'">7</xsl:when>
			<xsl:when test="$WPXColor='002f2f2f'">8</xsl:when>
			<xsl:when test="$WPXColor='0000007f'">9</xsl:when>
			<xsl:when test="$WPXColor='000060ff'">10</xsl:when>
			<xsl:when test="$WPXColor='00007f7f'">11</xsl:when>
			<xsl:when test="$WPXColor='green'">12</xsl:when>
			<xsl:when test="$WPXColor='007f7f00'">13</xsl:when>
			<xsl:when test="$WPXColor='blue'">14</xsl:when>
			<xsl:when test="$WPXColor='00906060'">15</xsl:when>
			<xsl:when test="$WPXColor='007f7f7f'">16</xsl:when>
			<xsl:when test="$WPXColor='red'">17</xsl:when>
			<xsl:when test="$WPXColor='000090ff'">18</xsl:when>
			<xsl:when test="$WPXColor='0000c090'">19</xsl:when>
			<xsl:when test="$WPXColor='0060902f'">20</xsl:when>
			<xsl:when test="$WPXColor='00c0c02f'">21</xsl:when>
			<xsl:when test="$WPXColor='00ff602f'">22</xsl:when>
			<xsl:when test="$WPXColor='007f007f'">23</xsl:when>
			<xsl:when test="$WPXColor='00909090'">24</xsl:when>
			<xsl:when test="$WPXColor='magenta'">25</xsl:when>
			<xsl:when test="$WPXColor='0000c0ff'">26</xsl:when>
			<xsl:when test="$WPXColor='yellow'">27</xsl:when>
			<xsl:when test="$WPXColor='0000ff00'">28</xsl:when>
			<xsl:when test="$WPXColor='cyan'">29</xsl:when>
			<xsl:when test="$WPXColor='00ffc000'">30</xsl:when>
			<xsl:when test="$WPXColor='00602f90'">31</xsl:when>
			<xsl:when test="$WPXColor='00c0c0c0'">32</xsl:when>
			<xsl:when test="$WPXColor='00c090ff'">33</xsl:when>
			<xsl:when test="$WPXColor='0090c0ff'">34</xsl:when>
			<xsl:when test="$WPXColor='0090ffff'">35</xsl:when>
			<xsl:when test="$WPXColor='00cfffcf'">36</xsl:when>
			<xsl:when test="$WPXColor='00ffffc0'">37</xsl:when>
			<xsl:when test="$WPXColor='00ffc090'">38</xsl:when>
			<xsl:when test="$WPXColor='00ff90c0'">39</xsl:when>
			<xsl:when test="$WPXColor='white'">40</xsl:when>
		</xsl:choose>
	</xsl:template>
	<!--*************************** STYLE COUNTER TEMPLATE*************************************-->
	<xsl:template name="Counter">
		<xsl:param name="nmStl"/>
		<!-- init-number could be a param -->
		<xsl:variable name="init-number">0</xsl:variable>
		<xsl:variable name="number">
			<xsl:number/>
		</xsl:variable>
		<xsl:for-each select="//StStyle[Name17/Uni[.=$nmStl]]">
			<xsl:variable name="tmp">
				<xsl:number value="$number"/>
			</xsl:variable>
		</xsl:for-each>
		<xsl:number value="$init-number + $number - 1"/>
	</xsl:template>
	<!--*************************** LIST COUNTER TEMPLATE*************************************-->
	<xsl:template name="ListCounter">
		<xsl:param name="bulVal"/>
		<!-- init-number could be a param -->
		<xsl:variable name="init-number">
			<xsl:number/>
		</xsl:variable>
		<xsl:for-each select="//Styles/StStyle[Rules17/Prop[.=@bulNumScheme]]">
			<xsl:variable name="tmp1">
				<xsl:number value="$init-number"/>
			</xsl:variable>
		</xsl:for-each>
		<xsl:variable name="number">0</xsl:variable>
		<xsl:for-each select="//Body/StTxtPara[StyleRules15/Prop[@bulNumScheme=$bulVal]]">
			<xsl:variable name="tmp2">
				<xsl:number value="$number"/>
			</xsl:variable>
		</xsl:for-each>
		<xsl:number value="$init-number + $number - 1"/>
	</xsl:template>
	<!--****************************PARSE FOOTER TEMPLATE***************************************-->
	<xsl:template name="Parse">
		<xsl:param name="string"/>
		<xsl:choose>
			<xsl:when test="contains($string, '&amp;[')">
				<xsl:if test="substring-before($string, '&amp;[')">
					<xsl:value-of select="substring-before($string, '&amp;[')"/>
				</xsl:if>
				<xsl:variable name="footfrmt" select="substring-before(substring-after($string, '&amp;['), ']')"/>
				<xsl:if test="$footfrmt='date'">
					<xsl:text>\chdate</xsl:text>
				</xsl:if>
				<xsl:if test="$footfrmt='page'">
					<xsl:text>\chpgn</xsl:text>
				</xsl:if>
				<xsl:call-template name="Parse">
					<xsl:with-param name="string" select="substring-after(substring-after($string, '&amp;['), ']')"/>
				</xsl:call-template>
			</xsl:when>
			<xsl:otherwise>
				<xsl:if test="normalize-space($string)">
					<xsl:value-of select="$string"/>
				</xsl:if>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--*************************** LIST HYBRID TEMPLATE ***********************************************-->
	<xsl:template name="Hybrid">
		<xsl:param name="HybridPath"/>
		<xsl:param name="Id"/>
		<xsl:for-each select="$HybridPath">
			<xsl:text>{\listlevel</xsl:text>
			<xsl:choose>
				<xsl:when test="@bulNumScheme &gt; 100">
					<xsl:text>\levelnfc23\levelnfcn23\leveljc0\leveljcn0</xsl:text>
				</xsl:when>
				<xsl:otherwise>
					<xsl:text>\levelnfc</xsl:text>
					<xsl:call-template name="NumberValue">
						<xsl:with-param name="Number" select="@bulNumScheme"/>
					</xsl:call-template>
					<xsl:text>\levelnfcn</xsl:text>
					<xsl:call-template name="NumberValue">
						<xsl:with-param name="Number" select="@bulNumScheme"/>
					</xsl:call-template>
					<xsl:text>\leveljc0\leveljcn0</xsl:text>
				</xsl:otherwise>
			</xsl:choose>
			<xsl:text>\levelfollow0</xsl:text>
			<xsl:choose>
				<xsl:when test="@bulNumStartAt">
					<xsl:text>\levelstartat</xsl:text>
					<xsl:value-of select="@bulNumStartAt"/>
				</xsl:when>
				<xsl:otherwise>
					<xsl:text>\levelstartat1</xsl:text>
				</xsl:otherwise>
			</xsl:choose>
			<xsl:text>\levelspace360\levelindent0</xsl:text>
			<xsl:choose>
				<xsl:when test="@bulNumScheme &gt; 100">
					<xsl:text>{\leveltext\leveltemplateid</xsl:text>
					<xsl:value-of select="$Id"/>
					<xsl:text>\'0</xsl:text>
					<xsl:choose>
						<xsl:when test="@bulNumTxtBef">
							<xsl:variable name="TxtBefore" select="string-length(string(@bulNumTxtBef))"/>
							<xsl:if test="@bulNumTxtAft">
								<xsl:variable name="TxtAfter" select="string-length(string(@bulNumTxtAft))"/>
								<xsl:value-of select="$TxtBefore + $TxtAfter + 1"/>
							</xsl:if>
							<xsl:if test="not(@bulNumTxtAft)">
								<xsl:value-of select="$TxtBefore + 1"/>
							</xsl:if>
						</xsl:when>
						<xsl:otherwise>
							<xsl:if test="@bulNumTxtAft">
								<xsl:variable name="TxtAfter" select="string-length(string(@bulNumTxtAft))"/>
								<xsl:value-of select="$TxtAfter + 1"/>
							</xsl:if>
							<xsl:if test="not(@bulNumTxtAft)">
								<xsl:value-of select="1"/>
							</xsl:if>
						</xsl:otherwise>
					</xsl:choose>
					<xsl:if test="@bulNumTxtBef">
						<xsl:value-of select="@bulNumTxtBef"/>
					</xsl:if>
					<xsl:text>\u</xsl:text>
					<xsl:call-template name="BulletValue">
						<xsl:with-param name="Bullet" select="@bulNumScheme"/>
					</xsl:call-template>
					<xsl:if test="@bulNumTxtAft">
						<xsl:value-of select="@bulNumTxtAft"/>
					</xsl:if>
					<xsl:text>;}{\levelnumbers;}</xsl:text>
					<xsl:text>\f2</xsl:text>
					<xsl:if test="@fontsize">
						<xsl:text>\fs</xsl:text>
						<xsl:variable name="fsize" select="@fontsize"/>
						<xsl:if test="contains($fsize, 'mpt')">
							<xsl:variable name="actualfsize" select="substring-before($fsize, 'mpt')"/>
							<xsl:number value="$actualfsize div(500)"/>
						</xsl:if>
					</xsl:if>
					<xsl:call-template name="ParagraphFormatting"/>
					<xsl:text>}</xsl:text>
				</xsl:when>
				<xsl:otherwise>
					<xsl:text>{\leveltext\leveltemplateid</xsl:text>
					<xsl:value-of select="$Id"/>
					<xsl:text>\'0</xsl:text>
					<xsl:choose>
						<xsl:when test="@bulNumTxtBef">
							<xsl:variable name="TxtBefore" select="string-length(string(@bulNumTxtBef))"/>
							<xsl:if test="@bulNumTxtAft">
								<xsl:variable name="TxtAfter" select="string-length(string(@bulNumTxtAft))"/>
								<xsl:value-of select="$TxtBefore + $TxtAfter + 2"/>
							</xsl:if>
							<xsl:if test="not(@bulNumTxtAft)">
								<xsl:value-of select="$TxtBefore + 2"/>
							</xsl:if>
						</xsl:when>
						<xsl:otherwise>
							<xsl:if test="@bulNumTxtAft">
								<xsl:variable name="TxtAfter" select="string-length(string(@bulNumTxtAft))"/>
								<xsl:value-of select="$TxtAfter + 2"/>
							</xsl:if>
							<xsl:if test="not(@bulNumTxtAft)">
								<xsl:value-of select="2"/>
							</xsl:if>
						</xsl:otherwise>
					</xsl:choose>
					<xsl:if test="@bulNumTxtBef">
						<xsl:value-of select="@bulNumTxtBef"/>
					</xsl:if>
					<xsl:text>\'00.</xsl:text>
					<xsl:if test="@bulNumTxtAft">
						<xsl:value-of select="@bulNumTxtAft"/>
					</xsl:if>
					<xsl:text>;}{\levelnumbers\'01;}</xsl:text>
					<xsl:text>\f3</xsl:text>
					<xsl:if test="@fontsize">
						<xsl:text>\fs</xsl:text>
						<xsl:variable name="fsize" select="@fontsize"/>
						<xsl:if test="contains($fsize, 'mpt')">
							<xsl:variable name="actualfsize" select="substring-before($fsize, 'mpt')"/>
							<xsl:number value="$actualfsize div(500)"/>
						</xsl:if>
					</xsl:if>
					<xsl:call-template name="ParagraphFormatting"/>
					<xsl:text>}</xsl:text>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:for-each>
	</xsl:template>
	<xsl:template name="BulletValue">
		<xsl:param name="Bullet"/>
		<xsl:choose>
			<xsl:when test="$Bullet=102">159 ?</xsl:when>
			<xsl:when test="$Bullet=103">108 ?</xsl:when>
			<xsl:when test="$Bullet=104">109 ?</xsl:when>
			<xsl:when test="$Bullet=105">167 ?</xsl:when>
			<xsl:when test="$Bullet=106">110 ?</xsl:when>
			<xsl:when test="$Bullet=107">250 ?</xsl:when>
			<xsl:when test="$Bullet=108">111 ?</xsl:when>
			<xsl:when test="$Bullet=109">113 ?</xsl:when>
			<xsl:when test="$Bullet=110">114 ?</xsl:when>
			<xsl:when test="$Bullet=111">115 ?</xsl:when>
			<xsl:when test="$Bullet=112">116 ?</xsl:when>
			<xsl:when test="$Bullet=113">117 ?</xsl:when>
			<xsl:when test="$Bullet=114">118 ?</xsl:when>
			<xsl:when test="$Bullet=115">122 ?</xsl:when>
			<xsl:when test="$Bullet=116">70 ?</xsl:when>
			<xsl:when test="$Bullet=117">85 ?</xsl:when>
			<xsl:when test="$Bullet=118">86 ?</xsl:when>
			<xsl:when test="$Bullet=119">182 ?</xsl:when>
			<xsl:when test="$Bullet=120">216 ?</xsl:when>
			<xsl:when test="$Bullet=121">220 ?</xsl:when>
			<xsl:when test="$Bullet=122">224 ?</xsl:when>
			<xsl:when test="$Bullet=123">232 ?</xsl:when>
			<xsl:when test="$Bullet=124">240 ?</xsl:when>
			<xsl:when test="$Bullet=125">252 ?</xsl:when>
			<xsl:otherwise>158 ?</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<xsl:template name="NumberValue">
		<xsl:param name="Number"/>
		<xsl:choose>
			<xsl:when test="$Number=11">1</xsl:when>
			<xsl:when test="$Number=12">2</xsl:when>
			<xsl:when test="$Number=13">3</xsl:when>
			<xsl:when test="$Number=14">4</xsl:when>
			<xsl:when test="$Number=15">22</xsl:when>
			<xsl:otherwise>0</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
</xsl:stylesheet>
