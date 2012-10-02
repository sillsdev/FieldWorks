<?xml version="1.0" encoding="UTF-8"?>
<!-- edited with XML Spy v4.4 U (http://www.xmlspy.com) by Larr Hayashi (private) -->
<!--This XSL is the last of 4 in a series to export FieldWorks data to RTF.
FwExport2RTF1 - Strips extra information and outputs standard WorldPad format.
FwExport2RTF1b - Creates unique integer ids for Styles and Fonts so that they can be more easily referenced when creating the RTF.
FwExport2RTF2a - Adds style information locally to the paragraph particularly for bulleted or numbered paragraphs to facilitate creation of RTF codes.
** FwExport2RTF2b - Creates RTF codes.

It highly recommended that you use an XML viewer to review or edit these files in order to easily delineate commented code from actual code.
-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:msxsl="urn:schemas-microsoft-com:xslt" xmlns:user="http://mycompany.com/mynamespace">
	<xsl:key name="ParagraphStyles" match="//StStyle" use="Name17/Uni"/>
	<xsl:key name="LgEncodings" match="//LgEncoding" use="@id"/>
	<xsl:key name="BulletFonts" match="//BulletFont" use="@bulletFontName"/>
	<msxsl:script language="javascript" implements-prefix="user"><![CDATA[
	function getCharCodes(RunString){
	   var Length = RunString.length;
	   var RTF = "";//initialize variable RTF as empty.
		for (i = 0; i < Length; i++) {
		   var CurrentCharCode = RunString.charCodeAt(i);
		   {if (CurrentCharCode > 127 || CurrentCharCode == 92 || CurrentCharCode == 123 || CurrentCharCode == 125 ) //127 is decimal value for U+007F, 92 is backslash, 123 and 125 are curly braces.
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
		<!--xsl:call-template name="List"/-->
		<!--xsl:call-template name="ListOverride"/-->
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
		<xsl:text>{\f2\ftech\fcharset1\fprq2 Wingdings;}&#13;</xsl:text>
		<!-- This section defines a unique number for each encoding -->
		<xsl:for-each select="//LgEncoding">
			<!-- Output the serif font number and information.-->
			<xsl:text>{\f</xsl:text>
			<xsl:value-of select="@serifFontNum"/>
			<xsl:text>\froman\fcharset0\fprq2 </xsl:text>
			<xsl:value-of select="WritingSystems24/LgWritingSystem/DefaultSerif25/Uni"/>
			<xsl:text>;}&#13;</xsl:text>
			<!--Output the sans serif font number and information.-->
			<xsl:text>{\f</xsl:text>
			<xsl:value-of select="@sansSerifFontNum"/>
			<xsl:text>\fswiss\fcharset0\fprq2 </xsl:text>
			<xsl:value-of select="WritingSystems24/LgWritingSystem/DefaultSansSerif25/Uni"/>
			<xsl:text>;}&#13;</xsl:text>
		</xsl:for-each>
		<!--Here we define any fonts used in the WPX file for Bullets or Number Lists -->
		<xsl:for-each select="//BulletFont">
			<xsl:text>{\f</xsl:text>
			<xsl:value-of select="@bulletFontId"/>
			<xsl:text>&#32;</xsl:text>
			<xsl:value-of select="@bulletFontName"/>
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
			<xsl:if test="@type='s'">
				<xsl:text>&#13;{\s</xsl:text>
				<xsl:value-of select="@styleNum"/>
				<!-- <brdrdef> : border properties included in the syle -->
				<!-- <apoctl> location properties included in the style -->
				<!-- <tabdef> tab sets included in the style -->
				<xsl:for-each select="Rules17/Prop">
					<xsl:call-template name="ParagraphFormatting"/>
				</xsl:for-each>
				<!-- <chrfmt> character formatting properties included in the style -->
				<!-- ******************************CHARACTER FORMATTING FOR PARAGRAPH STYLE DEFINED HERE*************************************-->
				<xsl:for-each select="Rules17/Prop/WsStyles9999/WsProp[1]">
					<xsl:call-template name="FontInfo"/>
					<xsl:call-template name="CharacterFormatting"/>
				</xsl:for-each>
			</xsl:if>
			<!-- **************************************CHARACTER STYLES DEFINED HERE********************************************************************************-->
			<xsl:if test="@type='cs'">
				<xsl:text>&#13;{\*\cs</xsl:text>
				<xsl:value-of select="@styleNum"/>
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
			<!-- **************************************FOLLOWING APPLIES TO BOTH CHARACTER AND PARAGRAPH STYLES****************************-->
			<!-- Based on another style -->
			<xsl:if test="BasedOn17/Uni[.!='']">
				<xsl:if test="BasedOn17/Uni[.!='Default Paragraph Characters']">
					<xsl:text>\sbasedon</xsl:text>
					<!--xsl:variable name="basedOn" select="BasedOn17/Uni[.]"/-->
					<xsl:for-each select="key('ParagraphStyles', BasedOn17/Uni[.])">
						<xsl:value-of select="@styleNum"/>
					</xsl:for-each>
					<!--xsl:for-each select="//StStyle/Name17/Uni[.=$basedOn]">
						<xsl:number value="position() - 1"/>
					</xsl:for-each-->
				</xsl:if>
			</xsl:if>
			<!-- Next style associated with the current style
			if omitted, the next style is the current style -->
			<xsl:if test="Rules17/Prop">
				<!--xsl:variable name="next" select="Next17/Uni"/-->
				<xsl:choose>
					<xsl:when test="Next17/Uni[.!='']">
						<xsl:text>\snext</xsl:text>
						<xsl:for-each select="key('ParagraphStyles', Next17/Uni[.])">
							<xsl:value-of select="@styleNum"/>
						</xsl:for-each>
						<!--xsl:for-each select="//StStyle/Name17/Uni[.=$next]">
							<xsl:number value="position()-1"/>
						</xsl:for-each-->
					</xsl:when>
					<xsl:otherwise>
						<xsl:text>\snext</xsl:text>
						<xsl:value-of select="@styleNum"/>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:if>
			<xsl:choose>
				<xsl:when test="Rules17/Prop">
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
		<!-- ************************CREATE A CHAR STYLE FOR EACH LANGUAGE BASED ON NORMAL******************************************-->
		<!--This section goes through the Normal paragraph Style and creates a language character style for each language represented.-->
		<xsl:for-each select="//StStyle[Name17/Uni[.='Normal']]">
			<xsl:for-each select="Rules17/Prop/WsStyles9999/WsProp">
				<xsl:text>&#13;{\*\cs</xsl:text>
				<xsl:value-of select="position() + 500"/>
				<xsl:text>\additive</xsl:text>
				<!--When font family is equal to serif then use font 100 + sequence number of encoding, otherwise use font 200 + sequence number of encoding-->
				<xsl:choose>
					<xsl:when test="@fontFamily='&lt;default sans serif&gt;'">
						<xsl:variable name="Encoding" select="@enc"/>
						<xsl:text>\f</xsl:text>
						<xsl:for-each select="key('LgEncodings', @enc)">
							<xsl:value-of select="@sansSerifFontNum"/>
						</xsl:for-each>
					</xsl:when>
					<xsl:when test="@fontFamily='&lt;default serif&gt;'">
						<xsl:text>\f</xsl:text>
						<xsl:for-each select="key('LgEncodings', @enc)">
							<xsl:value-of select="@serifFontNum"/>
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
				<xsl:text>&#32;</xsl:text>
				<xsl:for-each select="key('LgEncodings', @enc)">
					<!--xsl:value-of select="WritingSystems24/LgWritingSystem[1]/Name25/Str"/-->
					<xsl:value-of select="@id"/>
				</xsl:for-each>
				<xsl:text>;}&#13;</xsl:text>
			</xsl:for-each>
			<!--xsl:for-each select="Rules17/Prop/BulNumFontInfo">
				<xsl:call-template name="BulNumFormat"/>
			</xsl:for-each-->
		</xsl:for-each>
		<xsl:text>}</xsl:text>
	</xsl:template>
	<!-- **************************************LIST TEMPLATE***********************************************************************************-->
	<!-- Don't need this for now: xsl:template name="List" match="//Styles"-->
	<!-- The listtable is a list of lists. Each list contains a number of list properties that
		pertain to the entire list, and a list of levels, each of which contains properties that
		pertain to that level only.
		<listtable> - {\*\listtable <list>+}
		<list>      - properties that pertain to the entire list .. <listlevel>+
		<listlevel> - <number> <justification> <leveltext> <levelnumbers> <chrfmt> ...
					- properties pertaining to one level only
	-->
	<!--xsl:text>&#13;{\*\listtable</xsl:text-->
	<!--xsl:for-each select="//StStyle"-->
	<!-- include styles for paragraph types -->
	<!--xsl:if test="Rules17/Prop/@bulNumScheme">
				<xsl:text>{\list\listtemplateid</xsl:text>
				<xsl:number value="position() + 2500 - 1"/>
				<xsl:text>\listhybrid</xsl:text-->
	<!-- Need bulNumScheme property data assignment clarification -->
	<!--xsl:call-template name="Hybrid">
					<xsl:with-param name="Id" select="position()*10 + 2100"/>
				</xsl:call-template>
				<xsl:call-template name="Hybrid">
					<xsl:with-param name="Id" select="position()*10 + 2101"/>
				</xsl:call-template>
				<xsl:call-template name="Hybrid">
					<xsl:with-param name="Id" select="position()*10 + 2102"/>
				</xsl:call-template>
				<xsl:call-template name="Hybrid">
					<xsl:with-param name="Id" select="position()*10 + 2103"/>
				</xsl:call-template>
				<xsl:call-template name="Hybrid">
					<xsl:with-param name="Id" select="position()*10 + 2104"/>
				</xsl:call-template>
				<xsl:call-template name="Hybrid">
					<xsl:with-param name="Id" select="position()*10 + 2105"/>
				</xsl:call-template>
				<xsl:call-template name="Hybrid">
					<xsl:with-param name="Id" select="position()*10 + 2106"/>
				</xsl:call-template>
				<xsl:call-template name="Hybrid">
					<xsl:with-param name="Id" select="position()*10 + 2107"/>
				</xsl:call-template>
				<xsl:call-template name="Hybrid">
					<xsl:with-param name="Id" select="position()*10 + 2108"/>
				</xsl:call-template>
				<xsl:text>{\listname </xsl:text>
				<xsl:value-of select="Name17/Uni"/>
				<xsl:text>;}</xsl:text>
				<xsl:text>\listid</xsl:text>
				<xsl:number value="position() + 2000 - 1"/>
				<xsl:text>}</xsl:text>
			</xsl:if>
		</xsl:for-each>
		<xsl:text>}</xsl:text>
	</xsl:template-->
	<!-- *********************************LIST OVERRIDE TEMPLATE************************************************************************-->
	<!--xsl:template name="ListOverride" match="//Styles">
		<xsl:text>&#13;{\*\listoverridetable</xsl:text>
		<xsl:for-each select="//StStyle">
			<xsl:if test="Rules17/Prop/@bulNumScheme">
				<xsl:text>{\listoverride\listid</xsl:text>
				<xsl:number value="position()+2000-1"/>
				<xsl:text>\listoverridecount0\ls</xsl:text>
				<xsl:number value="position()-1"/>
				<xsl:text>}</xsl:text>
			</xsl:if>
		</xsl:for-each>
		<xsl:text>}</xsl:text>
	</xsl:template-->
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
			<xsl:text> \sectd </xsl:text>
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
		<xsl:for-each select="//StTxtPara">
			<!--If a style is specified for the paragraph, then we apply the properties of that style to the run????-->
			<xsl:choose>
				<xsl:when test="StyleRules15/Prop/@namedStyle">
					<xsl:call-template name="Run">
						<xsl:with-param name="styleRule" select="StyleRules15/Prop/@namedStyle"/>
					</xsl:call-template>
				</xsl:when>
				<!--If a style is not specified, then we use the paragraph properties of the previous paragraph.-->
				<xsl:otherwise>
					<xsl:call-template name="Run">
						<xsl:with-param name="styleRule" select="preceding-sibling::StyleRule15/Prop/@nameStyle"/>
					</xsl:call-template>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:for-each>
		<xsl:text>}</xsl:text>
	</xsl:template>
	<!-- **************************************RUN TEMPLATE********************************************************************************-->
	<xsl:template name="Run" match="//StTxtPara">
		<xsl:param name="styleRule"/>
		<xsl:text>&#13;</xsl:text>
		<!--If this is not the first paragraph in the document then output a carriage return.-->
		<xsl:if test="position()!=1">
			<xsl:text>\par</xsl:text>
		</xsl:if>
		<!--Get the style information for paragraph-->
		<!--Check to see if this paragraph is the first paragraph in a bulleted or numbered list.
1. If it has a bulNumScheme that is for bullets (bulNumScheme > 100) then ...
	If it has a bulNumScheme that is for numbers (bulNumScheme < 100) then ...

  a. If yes, the I want to check if it has the same bulNumScheme as the previous paragraph.

	i. If yes, then I also want to check if it does not have the same bulNumStartAt (which in the case of bullets is usually absent).

	 I. If true then it is the "first" in a sequence of items (even though it might be the "third numbered paragraph" it starts after a
		  normal paragraph and thus starts a new sequence starting at 3)and I want to output {\*\pn etc.} using \pnstart with the value given in
		  the bulNumStartAt attribute.

	II. If false, then I do not output {\*\pn etc.} because this is "inherited" from the previous paragraph -
		  i.e this paragraph is not the first in a sequence of bullets or numbered items.

	ii. If not then it is the first in a sequence of items (perhaps the only one ) and I want to output {\*\pn etc.}

  b. If no, then I can quit here.
-->
		<xsl:choose>
			<!--Check to see if it is part of a bulleted or numbered list-->
			<xsl:when test="StyleRules15/Prop[@bulNumScheme]">
				<xsl:choose>
					<!-- a. If yes, the I want to check if it has the same bulNumScheme as the previous paragraph.-->
					<xsl:when test="StyleRules15/Prop/@bulNumScheme = preceding-sibling::StTxtPara[1]/StyleRules15/Prop/@bulNumScheme">
						<!--    i. If yes, then I also want to check if it does not have the same bulNumStartAt (which in the case of bullets is usually absent).-->
						<!--The following is very unlikely to occur.-->
						<xsl:if test="StyleRules15/Prop/@bulNumStartAt">
							<!--     I. If true then it is the "first" in a sequence of items (even though it might be the "third numbered paragraph" it starts after a
		  normal paragraph and thus starts a new sequence starting at 3)and I want to output {\*\pn etc.} using \pnstart with the value given in
		  the bulNumStartAt attribute.-->
							<xsl:if test="StyleRules15/Prop/@bulNumStartAt != preceding-sibling::StTxtPara[1]/StyleRules15/Prop/@bulNumStartAt">
								<xsl:text>\pard \plain</xsl:text>
								<xsl:call-template name="RTFParagraphNumbering"/>
							</xsl:if>
							<!--    II. If false, then I do not output {\*\pn etc.} because this is "inherited" from the previous paragraph -
		  i.e this paragraph is not the first in a sequence of bullets or numbered items.-->
						</xsl:if>
					</xsl:when>
					<!--    ii. If not then it is the first in a sequence of items (perhaps the only one ) and I want to output {\*\pn etc.}-->
					<xsl:otherwise>
						<xsl:text>\pard \plain</xsl:text>
						<xsl:call-template name="RTFParagraphNumbering"/>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:when>
			<xsl:otherwise>
				<!--If not part of a bulleted or numbered list then we should reset the paragraph properties.-->
				<xsl:text>\pard \plain</xsl:text>
			</xsl:otherwise>
		</xsl:choose>
		<!--Get properties of the Style-->
		<xsl:for-each select="key('ParagraphStyles', $styleRule)">
			<xsl:text>\s</xsl:text>
			<xsl:value-of select="@styleNum"/>
			<!--Get properties of Paragraph Style and place in stream of RTF-->
			<xsl:for-each select="Rules17/Prop">
				<xsl:call-template name="ParagraphFormatting"/>
			</xsl:for-each>
			<!-- insert character formatting from the based on paragraph style -->
			<xsl:if test="BasedOn17/Uni[.!='']">
				<xsl:call-template name="Char">
					<xsl:with-param name="based" select="BasedOn17/Uni"/>
				</xsl:call-template>
			</xsl:if>
			<!-- insert bullet/number list format -->
			<!--xsl:if test="Rules17/Prop/@bulNumScheme">
				<xsl:text>\s1000 \ls</xsl:text>
				<xsl:number value="Rules17/Prop/@bulNumScheme"/>
				<xsl:text>\adjustright</xsl:text>
			</xsl:if-->
			<!-- insert character formatting from the paragraph style manually -->
			<xsl:for-each select="Rules17/Prop/WsStyles9999/WsProp[1]">
				<xsl:call-template name="FontInfo"/>
				<xsl:call-template name="CharacterFormatting"/>
			</xsl:for-each>
		</xsl:for-each>
		<!--Get local properties of the paragraph-->
		<xsl:for-each select="StyleRules15/Prop">
			<xsl:call-template name="ParagraphFormatting"/>
		</xsl:for-each>
		<!-- **************************************REAL RUN TEMPLATE - ... CF. ABOVE ********************************************************************************-->
		<xsl:for-each select="*/Str/Run">
			<xsl:variable name="style" select="@namedStyle"/>
			<!--Temporary bug fix here for when new custom fields are added and exported right away.
			For some reason, an @enc is not included with the Run until the user exits the database and restarts it.-->
			<xsl:choose>
				<xsl:when test="@enc">
					<xsl:variable name="lgEnc" select="@enc"/>
					<!-- insert default language character formatting -->
					<xsl:call-template name="LgEncode">
						<xsl:with-param name="encoding" select="$lgEnc"/>
					</xsl:call-template>
				</xsl:when>
				<xsl:otherwise>
					<xsl:text>{</xsl:text>
				</xsl:otherwise>
			</xsl:choose>
			<!-- insert named style character formatting -->
			<xsl:if test="$style!=''">
				<xsl:for-each select="key('ParagraphStyles', $style)">
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
							<xsl:value-of select="@styleNum"/>
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
			<!--Temporary fix-->
			<xsl:text>}</xsl:text>
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
						<!--xsl:for-each select="//Languages/LgEncoding[@id=$encoding]">
							<xsl:value-of select="1 + count(preceding-sibling::*) + 100"/>
						</xsl:for-each-->
						<xsl:for-each select="key('LgEncodings', $encoding)">
							<xsl:value-of select="@serifFontNum"/>
						</xsl:for-each>
					</xsl:when>
					<xsl:when test="@fontFamily='&lt;default san serif&gt;'">
						<xsl:for-each select="key('LgEncodings', $encoding)">
							<xsl:value-of select="@sansSerifFontNum"/>
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
	<!-- **************************************Grab the character style format properties ********************************************************************************-->
	<xsl:template name="Char" match="//StStyle">
		<xsl:param name="based"/>
		<!--First grab the font properties of the based on style-->
		<xsl:for-each select="key('ParagraphStyles', $based)">
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
	<!--xsl:template name="BulNumFormat" match="//StStyle/Rules17/Prop/BulNumFontInfo">
		<xsl:text>{\s1000 </xsl:text>
		<xsl:call-template name="FontInfo"/>
		<xsl:call-template name="ParagraphFormatting"/>
		<xsl:call-template name="CharacterFormatting"/>
		<xsl:text>\fs20 \fi-360\li760\jclisttab\tx760</xsl:text>
		<xsl:text> BulNumInfo;}</xsl:text>
	</xsl:template-->
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
		</xsl:if>
		<xsl:if test="@trailingIndent">
			<xsl:text>\rin</xsl:text>
			<xsl:number value="@trailingIndent div(50)"/>
		</xsl:if>
		<xsl:if test="@firstIndent">
			<xsl:text>\fi</xsl:text>
			<xsl:number value="@firstIndent div(50)"/>
			<xsl:choose>
				<xsl:when test="not(@leadingIndent)">
					<xsl:text>\lin</xsl:text>
					<xsl:number value="-1*@firstIndent div(50)"/>
				</xsl:when>
				<xsl:otherwise>
					<xsl:text>\lin</xsl:text>
					<xsl:number value="(@leadingIndent div(50)) + (-1*@firstIndent div(50))"/>
				</xsl:otherwise>
			</xsl:choose>
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
			<xsl:when test="@fontFamily='&lt;default serif&gt;'">\froman</xsl:when>
			<xsl:when test="@fontFamily='Times New Roman'">\froman</xsl:when>
			<xsl:when test="@fontFamily='&lt;default san serif&gt;'">\fswiss</xsl:when>
			<xsl:when test="@fontFamily='Arial'">\swiss</xsl:when>
			<xsl:when test="@fontFamily='Wingdings'">\tech</xsl:when>
		</xsl:choose>
		<xsl:if test="@fontsize">
			<xsl:text>\fs</xsl:text>
			<xsl:choose>
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
	<!-- **************************************BULNUMFONTINFO TEMPLATE********************************************************************************-->
	<!-- This is separate from the above one because unfortunately we were inconsistent between Bullet font attributes and regular font attributes,
   namely, that the unit is assumed to be mpt with Bullets-->
	<xsl:template name="BulNumFontInfo">
		<xsl:variable name="MyFontName" select="@fontFamily"/>
		<xsl:variable name="BulFont2" select="//BulNumFontInfo[not(@fontFamily=following::BulNumFontInfo/@fontFamily)]"/>
		<xsl:for-each select="$BulFont2[@fontFamily=$MyFontName]">
			<xsl:variable name="MyFontNumber" select="position() + 300"/>
		</xsl:for-each>
		<xsl:if test="@fontsize">
			<xsl:text>\fs</xsl:text>
			<xsl:choose>
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
		<!--xsl:if test="@backcolor">
		 <xsl:text>\cb</xsl:text>
		 <xsl:call-template name="MatchColorTable">
			<xsl:with-param name="WPXColor" select="@backcolor"/>
		 </xsl:call-template>
	  </xsl:if-->
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
		<!--xsl:if test="@forecolor">
		 <xsl:text>\cf</xsl:text>
		 <xsl:call-template name="MatchColorTable">
			<xsl:with-param name="WPXColor" select="@forecolor"/>
		 </xsl:call-template>
	  </xsl:if-->
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
		<!--xsl:if test="@undercolor">
		 <xsl:text>\ulc</xsl:text>
		 <xsl:call-template name="MatchColorTable">
			<xsl:with-param name="WPXColor" select="@undercolor"/>
		 </xsl:call-template>
	  </xsl:if-->
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
	<xsl:template name="Counter" match="//Styles">
		<xsl:param name="nmStl"/>
		<!-- init-number could be a param -->
		<xsl:variable name="init-number">0</xsl:variable>
		<xsl:variable name="number">
			<xsl:number/>
		</xsl:variable>
		<xsl:for-each select="StStyle/Name17/Uni[.=$nmStl]">
			<xsl:value-of select="$number"/>
		</xsl:for-each>
		<xsl:value-of select="$init-number + $number - 1"/>
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
	<!--Don't need this for now: xsl:template name="Hybrid" match="//StStyle">
		<xsl:param name="Id"/>
		<xsl:text>{\listlevel</xsl:text>
		<xsl:choose>
			<xsl:when test="Rules17/Prop/@bulNumScheme &gt; 100">
				<xsl:text>\levelnfc23\levelnfcn23\leveljc0\leveljcn0</xsl:text>
			</xsl:when>
			<xsl:otherwise>
				<xsl:text>\levelnfc</xsl:text>
				<xsl:value-of select="Rules17/Prop/@bulNumScheme"/>
				<xsl:text>\levelnfcn</xsl:text>
				<xsl:value-of select="Rules17/Prop/@bulNumScheme"/>
				<xsl:text>\leveljc0\leveljcn0</xsl:text>
			</xsl:otherwise>
		</xsl:choose>
		<xsl:text>\levelfollow0</xsl:text>
		<xsl:choose>
			<xsl:when test="Rules17/Prop/@bulNumStartAt">
				<xsl:text>\levelstartat</xsl:text>
				<xsl:value-of select="Rules17/Prop/@bulNumStartAt"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:text>\levelstartat1</xsl:text>
			</xsl:otherwise>
		</xsl:choose>
		<xsl:text>\levelspace360\levelindent0</xsl:text>
		<xsl:choose>
			<xsl:when test="Rules17/Prop/@bulNumScheme &gt; 100">
				<xsl:text>{\leveltext\leveltemplateid</xsl:text>
				<xsl:value-of select="$Id"/>
				<xsl:text>\'0</xsl:text>
				<xsl:choose>
					<xsl:when test="Rules17/Prop/@bulNumTxtBef">
						<xsl:variable name="TxtBefore" select="string-length(string(Rules17/Prop/@bulNumTxtBef))"/>
						<xsl:if test="Rules17/Prop/@bulNumTxtAft">
							<xsl:variable name="TxtAfter" select="string-length(string(Rules17/Prop/@bulNumTxtAft))"/>
							<xsl:value-of select="$TxtBefore + $TxtAfter + 1"/>
						</xsl:if>
						<xsl:if test="not(Rules17/Prop/@bulNumTxtAft)">
							<xsl:value-of select="$TxtBefore + 1"/>
						</xsl:if>
					</xsl:when>
					<xsl:otherwise>
						<xsl:if test="Rules17/Prop/@bulNumTxtAft">
							<xsl:variable name="TxtAfter" select="string-length(string(Rules17/Prop/@bulNumTxtAft))"/>
							<xsl:value-of select="$TxtAfter + 1"/>
						</xsl:if>
						<xsl:if test="not(Rules17/Prop/@bulNumTxtAft)">
							<xsl:value-of select="1"/>
						</xsl:if>
					</xsl:otherwise>
				</xsl:choose>
				<xsl:if test="Rules17/Prop/@bulNumTxtBef">
					<xsl:value-of select="Rules17/Prop/@bulNumTxtBef"/>
				</xsl:if>
				<xsl:text>\u</xsl:text>
				<xsl:call-template name="BulletValue">
					<xsl:with-param name="Bullet" select="Rules17/Prop/@bulNumScheme"/>
				</xsl:call-template>
				<xsl:if test="Rules17/Prop/@bulNumTxtAft">
					<xsl:value-of select="Rules17/Prop/@bulNumTxtAft"/>
				</xsl:if>
				<xsl:text>;}{\levelnumbers;}</xsl:text>
				<xsl:for-each select="//StStyle/Rules17/Prop/BulNumFontInfo">
					<xsl:call-template name="FontInfo"/>
					<xsl:call-template name="ParagraphFormatting"/>
					<xsl:call-template name="CharacterFormatting"/>
					<xsl:text>\fs20 \fi-360\li760\jclisttab\tx760}</xsl:text>
				</xsl:for-each>
			</xsl:when>
			<xsl:otherwise>
				<xsl:text>{\leveltext\leveltemplateid</xsl:text>
				<xsl:value-of select="$Id"/>
				<xsl:text>\'0</xsl:text>
				<xsl:choose>
					<xsl:when test="Rules17/Prop/@bulNumTxtBef">
						<xsl:variable name="TxtBefore" select="string-length(string(Rules17/Prop/@bulNumTxtBef))"/>
						<xsl:if test="Rules17/Prop/@bulNumTxtAft">
							<xsl:variable name="TxtAfter" select="string-length(string(Rules17/Prop/@bulNumTxtAft))"/>
							<xsl:value-of select="$TxtBefore + $TxtAfter + 2"/>
						</xsl:if>
						<xsl:if test="not(Rules17/Prop/@bulNumTxtAft)">
							<xsl:value-of select="$TxtBefore + 2"/>
						</xsl:if>
					</xsl:when>
					<xsl:otherwise>
						<xsl:if test="Rules17/Prop/@bulNumTxtAft">
							<xsl:variable name="TxtAfter" select="string-length(string(Rules17/Prop/@bulNumTxtAft))"/>
							<xsl:value-of select="$TxtAfter + 2"/>
						</xsl:if>
						<xsl:if test="not(Rules17/Prop/@bulNumTxtAft)">
							<xsl:value-of select="2"/>
						</xsl:if>
					</xsl:otherwise>
				</xsl:choose>
				<xsl:if test="Rules17/Prop/@bulNumTxtBef">
					<xsl:value-of select="Rules17/Prop/@bulNumTxtBef"/>
				</xsl:if>
				<xsl:text>\'00.</xsl:text>
				<xsl:if test="Rules17/Prop/@bulNumTxtAft">
					<xsl:value-of select="Rules17/Prop/@bulNumTxtAft"/>
				</xsl:if>
				<xsl:text>;}{\levelnumbers\'01;}</xsl:text>
				<xsl:for-each select="//StStyle/Rules17/Prop/BulNumFontInfo">
					<xsl:call-template name="FontInfo"/>
					<xsl:call-template name="ParagraphFormatting"/>
					<xsl:call-template name="CharacterFormatting"/>
					<xsl:text>\fs20 \fi-360\li760\jclisttab\tx760}</xsl:text>
				</xsl:for-each>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template-->
	<xsl:template name="RTFParagraphNumbering">
		<xsl:text>{\*\pn</xsl:text>
		<!--Get the font information for non-standard font bullets including font, size, etc.if there is one. If there is not then we need to put symbol for bullets or just leave it for numbers.-->
		<!--Unfortunately we cannot use the standard font formatting template because there are some inconsistencies in the dtd with bullets-->
		<!--Here we find the sequence number of the font so we can identify its \fN font  number for RTF-->
		<!--Test to see if bullets (bulNumScheme > 100 or numbers < 100-->
		<xsl:choose>
			<!--Bullets-->
			<xsl:when test="StyleRules15/Prop/@bulNumScheme > 100">
				<!--FW/WorldPad don't allow the user to select a font for bullet. Symbol is always used-->
				<xsl:text>\pnlvlblt\pnf2\pnindent0{\pntxtb</xsl:text>
				<xsl:choose>
					<xsl:when test="StyleRules15/Prop/@bulNumScheme='100'">
						<xsl:text>\'9E</xsl:text>
					</xsl:when>
					<xsl:when test="StyleRules15/Prop/@bulNumScheme='101'">
						<xsl:text>\'9F</xsl:text>
					</xsl:when>
					<xsl:when test="StyleRules15/Prop/@bulNumScheme='102'">
						<xsl:text>\'6C</xsl:text>
					</xsl:when>
					<xsl:when test="StyleRules15/Prop/@bulNumScheme='103'">
						<xsl:text>\'6D</xsl:text>
					</xsl:when>
					<xsl:when test="StyleRules15/Prop/@bulNumScheme='104'">
						<xsl:text>\'6E</xsl:text>
					</xsl:when>
					<xsl:when test="StyleRules15/Prop/@bulNumScheme='105'">
						<xsl:text>\'6F</xsl:text>
					</xsl:when>
					<xsl:when test="StyleRules15/Prop/@bulNumScheme='106'">
						<xsl:text>\'71</xsl:text>
					</xsl:when>
					<xsl:when test="StyleRules15/Prop/@bulNumScheme='107'">
						<xsl:text>\'72</xsl:text>
					</xsl:when>
					<xsl:when test="StyleRules15/Prop/@bulNumScheme='108'">
						<xsl:text>\'73</xsl:text>
					</xsl:when>
					<xsl:when test="StyleRules15/Prop/@bulNumScheme='109'">
						<xsl:text>\'74</xsl:text>
					</xsl:when>
					<xsl:when test="StyleRules15/Prop/@bulNumScheme='110'">
						<xsl:text>\'75</xsl:text>
					</xsl:when>
					<xsl:when test="StyleRules15/Prop/@bulNumScheme='111'">
						<xsl:text>\'76</xsl:text>
					</xsl:when>
					<xsl:when test="StyleRules15/Prop/@bulNumScheme='112'">
						<xsl:text>\'7A</xsl:text>
					</xsl:when>
					<xsl:when test="StyleRules15/Prop/@bulNumScheme='113'">
						<xsl:text>\'46</xsl:text>
					</xsl:when>
					<xsl:when test="StyleRules15/Prop/@bulNumScheme='114'">
						<xsl:text>\'55</xsl:text>
					</xsl:when>
					<xsl:when test="StyleRules15/Prop/@bulNumScheme='115'">
						<xsl:text>\'56</xsl:text>
					</xsl:when>
					<xsl:when test="StyleRules15/Prop/@bulNumScheme='116'">
						<xsl:text>\'B6</xsl:text>
					</xsl:when>
					<xsl:when test="StyleRules15/Prop/@bulNumScheme='117'">
						<xsl:text>\'D8</xsl:text>
					</xsl:when>
					<xsl:when test="StyleRules15/Prop/@bulNumScheme='118'">
						<xsl:text>\'DC</xsl:text>
					</xsl:when>
					<xsl:when test="StyleRules15/Prop/@bulNumScheme='119'">
						<xsl:text>\'E0</xsl:text>
					</xsl:when>
					<xsl:when test="StyleRules15/Prop/@bulNumScheme='120'">
						<xsl:text>\'E8</xsl:text>
					</xsl:when>
					<xsl:when test="StyleRules15/Prop/@bulNumScheme='121'">
						<xsl:text>\'F0</xsl:text>
					</xsl:when>
					<xsl:when test="StyleRules15/Prop/@bulNumScheme='122'">
						<xsl:text>\'FC</xsl:text>
					</xsl:when>
					<xsl:otherwise>
						<xsl:text>Please notify the FieldWorks team of the presence of this message.</xsl:text>
					</xsl:otherwise>
				</xsl:choose>
				<xsl:text>}</xsl:text>
			</xsl:when>
			<!--Numbers instead of bullets-->
			<xsl:otherwise>
				<!--!!!Need to put default font here-->
				<xsl:text>\pnlvlbody</xsl:text>
				<xsl:choose>
					<xsl:when test="StyleRules15/Prop/@bulNumScheme='10'">
						<xsl:text>\pndec</xsl:text>
					</xsl:when>
					<xsl:when test="StyleRules15/Prop/@bulNumScheme='11'">
						<xsl:text>\pnucrmI</xsl:text>
					</xsl:when>
					<xsl:when test="StyleRules15/Prop/@bulNumScheme='12'">
						<xsl:text>\pnlcrm</xsl:text>
					</xsl:when>
					<xsl:when test="StyleRules15/Prop/@bulNumScheme='13'">
						<xsl:text>\pnucltr</xsl:text>
					</xsl:when>
					<xsl:when test="StyleRules15/Prop/@bulNumScheme='14'">
						<xsl:text>\pnlcltr</xsl:text>
					</xsl:when>
					<!--Should probably put a textbefore value of 0 on this one ...01, 02, 03...-->
					<xsl:when test="StyleRules15/Prop/@bulNumScheme='15'">
						<xsl:text>\pndec</xsl:text>
					</xsl:when>
				</xsl:choose>
				<!--			ATTLIST BulNumFontInfo
	backcolor CDATA #IMPLIED - No equivalent?
	bold (on | off | invert) #IMPLIED \pnb
	fontsize CDATA #IMPLIED \pnfsN (in half-points)
	forecolor CDATA #IMPLIED \pncfN
	italic (on | off | invert) #IMPLIED \pni
	offset CDATA #IMPLIED
	superscript (off | sub | super) #IMPLIED  Not currently used on bullets.
	undercolor CDATA #IMPLIED - No RTF equivalent.
	underline (none | dotted | dashed | single | double | squiggle) #IMPLIED  \pnulnone \pnuld  \pnuldash \pnul \pnuldb \pnulwave
	fontFamily CDATA #IMPLIED \pnf

	<Rules17>
				<Prop bulNumScheme="10" bulNumStartAt="1">
					<BulNumFontInfo backcolor="red" bold="invert" forecolor="black" italic="invert" offset="0mpt" undercolor="00ffc000" underline="single" fontFamily="Algerian"/>
				</Prop>
-->
				<xsl:for-each select="StyleRules15/Prop/BulNumFontInfo">
					<xsl:if test="@fontFamily">
						<xsl:text>\pnf</xsl:text>
						<xsl:for-each select="key('BulletFonts', @fontFamily)">
							<xsl:value-of select="@bulletFontId"/>
						</xsl:for-each>
					</xsl:if>
					<xsl:if test="@fontsize">
						<xsl:text>\pnfs</xsl:text>
						<xsl:variable name="fsize" select="@fontsize"/>
						<xsl:if test="contains($fsize, 'mpt')">
							<xsl:variable name="actualfsize" select="substring-before($fsize, 'mpt')"/>
							<xsl:number value="$actualfsize div(500)"/>
						</xsl:if>
					</xsl:if>
					<xsl:if test="@bold">
						<xsl:choose>
							<xsl:when test="@bold='on'">
								<xsl:text>\pnb</xsl:text>
							</xsl:when>
							<!--Not sure what to do with the off and invert values here yet.-->
						</xsl:choose>
					</xsl:if>
					<xsl:if test="@italic">
						<xsl:choose>
							<xsl:when test="@italic='on'">
								<xsl:text>\pni</xsl:text>
							</xsl:when>
							<!--Not sure what to do with the off and invert values here yet.-->
						</xsl:choose>
					</xsl:if>
					<xsl:if test="@underline">
						<xsl:choose>
							<xsl:when test="@underline='none'">\pnulnone</xsl:when>
							<xsl:when test="@underline='single'">\pnul</xsl:when>
							<xsl:when test="@underline='dotted'">\pnuld</xsl:when>
							<xsl:when test="@underline='dash'">\pnuldash</xsl:when>
							<xsl:when test="@underline='double'">\pnuldb</xsl:when>
							<xsl:when test="@underline='squiggle'">\pnulwave</xsl:when>
						</xsl:choose>
					</xsl:if>
					<xsl:if test="@forecolor">
						<xsl:text>\pncf</xsl:text>
						<xsl:call-template name="MatchColorTable">
							<xsl:with-param name="WPXColor" select="@forecolor"/>
						</xsl:call-template>
					</xsl:if>
				</xsl:for-each>
				<!--Account for bulNumStartAt position (on first paragraph in sequence)-->
				<xsl:choose>
					<!--If a bulNumStartAt position is specified (on first paragraph in sequence), then use it, otherwise assume 1-->
					<xsl:when test="StyleRules15/Prop/@bulNumStartAt">
						<xsl:text>\pnstart</xsl:text>
						<xsl:value-of select="StyleRules15/Prop/@bulNumStartAt"/>
					</xsl:when>
					<xsl:otherwise>
						<xsl:text>\pnstart1</xsl:text>
					</xsl:otherwise>
				</xsl:choose>
				<!--Get font number here of style or if specified locally-->
				<!--Temporary-->
				<xsl:text>\pnindent0</xsl:text>
				<xsl:if test="StyleRules15/Prop/@bulNumTxtBef">
					<xsl:text>{\pntxtb </xsl:text>
					<xsl:value-of select="StyleRules15/Prop/@bulNumTxtBef"/>
					<xsl:text>}</xsl:text>
				</xsl:if>
				<xsl:if test="StyleRules15/Prop/@bulNumTxtAft">
					<xsl:text>{\pntxta </xsl:text>
					<xsl:value-of select="StyleRules15/Prop/@bulNumTxtAft"/>
					<xsl:text>}</xsl:text>
				</xsl:if>
			</xsl:otherwise>
		</xsl:choose>
		<!--Need to work out where this goes xsl:for-each select="BulNumFontInfo">
			<xsl:variable name="MyFontName" select="@fontFamily"/>
			<xsl:variable name="BulFont2" select="//BulNumFontInfo[not(@fontFamily=following::BulNumFontInfo/@fontFamily)]"/>
			<xsl:for-each select="$BulFont2[@fontFamily=$MyFontName]">
				<xsl:variable name="MyFontNumber" select="position() + 300"/>
				<xsl:text>\f</xsl:text>
				<xsl:value-of select="$MyFontNumber"/>
			</xsl:for-each-->
		<!--Here we add other character formatting attributes-->
		<!--xsl:call-template name="CharacterFormatting"/-->
		<!--/xsl:for-each-->
		<!--Put the appropriate bullet in-->
		<!--Numbers: allow textbefore and textafter as well as startAt
					In addition we need to calculate the numbers for each paragraph-->
		<xsl:text>}</xsl:text>
	</xsl:template>
</xsl:stylesheet>
