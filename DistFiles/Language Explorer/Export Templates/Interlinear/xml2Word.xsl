<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
				xmlns="http://www.w3.org/1999/XSL/Format"
				xmlns:w="http://schemas.microsoft.com/office/word/2003/wordml"
				xmlns:v="urn:schemas-microsoft-com:vml"
				version="1.0">
  <xsl:output method="xml" encoding="UTF-8" indent="yes" />
  <!-- This stylesheet transforms an interlinear text as output by FieldWorks into a form which can be directly read
  by Microsoft Word (tested with Word 2003 SP 2 build 11.6568.6568). -->

  <!-- This key is used as part of a trick way to output several style definitions once for each UNIQUE lang attribute
  that occur in a gls item -->
  <xsl:key name="distinct-lang-gls" match="item[@type='gls']" use="@lang"/>
  <!-- This key is used as part of a trick way to output several style definitions once for each UNIQUE lang attribute
  that occur in a txt or cf item -->
  <xsl:key name="distinct-lang-txtcf" match="item[@type='txt' or @type='cf']" use="@lang"/>

  <!-- Most of this is to generate a style sheet, with a distinct style for each line of text and a few other things.
  Then we output the actual interlinear data. -->
  <xsl:template match="document">
	<xsl:processing-instruction name="mso-application"> progid="Word.Document"</xsl:processing-instruction>
	<w:wordDocument
		xmlns:w="http://schemas.microsoft.com/office/word/2003/wordml" xmlns:v="urn:schemas-microsoft-com:vml">
			<w:styles>
				<!--Base for all interlinear styles -->
				<w:style w:type="paragraph" w:styleId="Interlin Base">
				</w:style>
				<!--Title paragraph taken from item with type="title" in main document -->
				<w:style w:type="paragraph" w:styleId="Interlin Title">
					<w:basedOn  w:val="Interlin Base"/>
					<w:name w:val="Interlin Title" />
					<w:pPr>
						<w:spacing  w:before="40" w:after="120"/>
						<w:keepNext w:val="on"/>
					</w:pPr>
					<w:rPr>
						<w:b  w:val="on"/>
						<w:sz  w:val="32"/>
					</w:rPr>
				</w:style>
				<!--Used for the marker paragraphs that indicate the paragraphs in the original text. -->
				<w:style w:type="paragraph" w:styleId="Interlin Paragraph Marker">
					<w:name w:val="Interlin Paragraph Marker" />
					<w:basedOn  w:val="Interlin Base"/>
					<w:pPr>
						<w:spacing  w:before="40" w:after="40"/>
						<w:keepNext w:val="on"/>
					</w:pPr>
					<w:rPr>
						<w:b  w:val="on"/>
						<w:keepNext w:val="on"/>
					</w:rPr>
				</w:style>
				<!--Used for root items with type="desc", typically a brief description of the whole document -->
				<w:style w:type="paragraph" w:styleId="Interlin Description">
					<w:name w:val="Interlin Description" />
					<w:basedOn  w:val="Interlin Base"/>
				</w:style>
				<!--Author paragraph taken from item with type="source" in main document -->
				<w:style w:type="paragraph" w:styleId="Interlin Source">
					<w:basedOn  w:val="Interlin Base"/>
					<w:name w:val="Interlin Title" />
					<w:pPr>
						<w:spacing  w:before="40" w:after="40"/>
						<w:keepNext w:val="on"/>
					</w:pPr>
					<w:rPr>
						<w:b  w:val="on"/>
						<w:sz  w:val="24"/>
					</w:rPr>
				</w:style>
				<!--Base for all vernacular text styles -->
				<w:style w:type="paragraph" w:styleId="Interlin Vernacular">
					<w:name w:val="Interlin Vernacular" />
					<w:basedOn  w:val="Interlin Base"/>
					<w:rPr>
						<w:i  w:val="on"/>
						<w:noProof w:val="on"/>
						<w:rFonts>
							<xsl:attribute name="w:ascii"><xsl:value-of select="//languages/language[@vernacular='true']/@font"/></xsl:attribute>
							<xsl:attribute name="w:h-ansi"><xsl:value-of select="//languages/language[@vernacular='true']/@font"/></xsl:attribute>
							<xsl:attribute name="w:cs"><xsl:value-of select="//languages/language[@vernacular='true']/@font"/></xsl:attribute>
							<xsl:attribute name="w:fareast"><xsl:value-of select="//languages/language[@vernacular='true']/@font"/></xsl:attribute>
						</w:rFonts>
					</w:rPr>
				</w:style>
				<!--For each unique language that occurs on an item of type txt or cf, generate some more styles
				with names that include the language id -->
				<xsl:for-each select="//item[(@type='txt' or @type='cf') and generate-id()=generate-id(key('distinct-lang-txtcf', @lang))]">
					<xsl:apply-templates mode="vstyle" select="."/>
				</xsl:for-each>
				<!-- The paragraphs that contain the interlinear bundles.
				Modify this to adjust spacing and indentation of the interlinear phrases as a whole-->
				<w:style w:type="paragraph" w:styleId="Interlin Words">
					<w:name w:val="Interlin Words" />
					<w:basedOn  w:val="Interlin Baseline"/>
					<w:bidi/>
				</w:style>
				<!--Used to mark the homograph number, which is output as an addendum to the citation form
				if it is present (morph/item[@type='hn']) -->
				<w:style w:type="character" w:styleId="Interlin Homograph">
					<w:name w:val="Interlin Homograph" />
					<w:rPr>
						<w:vertAlign  w:val="subscript"/>
					</w:rPr>
				</w:style>
		<w:style w:type="character" w:styleId="Interlin Variant Types">
		  <w:name w:val="Interlin Variant Types" />
		</w:style>
		<!--Base style for things typically in the main analysis language -->
				<w:style w:type="paragraph" w:styleId="Interlin Analysis">
					<w:name w:val="Interlin Analysis" />
					<w:basedOn  w:val="Interlin Base"/>
				</w:style>
				<!--Used for numbering of individual phrases -->
				<w:style w:type="paragraph" w:styleId="Interlin Phrase Number">
					<w:name w:val="Interlin Phrase Number" />
					<w:basedOn  w:val="Interlin Analysis"/>
				</w:style>
				<!--The morpheme part-of-speech (technically morpho-syntactic information) line (morph/item[@type='pos']) -->
				<w:style w:type="paragraph" w:styleId="Interlin Morpheme POS">
					<w:name w:val="Interlin Morpheme POS" />
					<w:basedOn  w:val="Interlin Analysis"/>
					<w:rPr>
						<w:sz  w:val="16"/>
					</w:rPr>
				</w:style>
				<!--The word part-of-speech line (word/item[@type='pos']) -->
				<w:style w:type="paragraph" w:styleId="Interlin Word POS">
					<w:name w:val="Interlin Word POS" />
					<w:basedOn  w:val="Interlin Analysis"/>
				</w:style>
				<!--For each unique language that occurs on an item of type gls, generate some more styles
				with names that include the language id -->
				<xsl:for-each select="//item[@type='gls' and generate-id()=generate-id(key('distinct-lang-gls', @lang))]">
					<xsl:apply-templates mode="style" select="."/>
				</xsl:for-each>
			</w:styles>
			<w:body>
				<xsl:apply-templates/>
			</w:body>

		</w:wordDocument>

	</xsl:template>

	<!-- Language-dependent styles -->
	<!-- Gets invoked with a randomly-chosen item of type txt or cf (probably the first) having each unique @lang attribute.
	Used to generate several styles for each WritingSystem used in such items. We may over-generate slightly; for example,
	if one WS is used only for word word/txt, we will still generate a morpheme/txt style for it.-->
	<xsl:template match="item" mode="vstyle">
		<!--Base style for vernacular stuff in a particular writing system, e.g., Interlin_Vern_en -->
		<w:style w:type="paragraph">
			<xsl:attribute name="w:styleId">Interlin Vern <xsl:value-of select="@lang"/></xsl:attribute>
			<w:name>
				<xsl:attribute name="w:val">Interlin Vern <xsl:value-of select="@lang"/></xsl:attribute>
			</w:name>
			<w:rPr>
				<w:rFonts>
					<xsl:attribute name="w:ascii"><xsl:value-of select="//languages/language[@lang=current()/@lang]/@font"/></xsl:attribute>
					<xsl:attribute name="w:h-ansi"><xsl:value-of select="//languages/language[@lang=current()/@lang]/@font"/></xsl:attribute>
					<xsl:attribute name="w:cs"><xsl:value-of select="//languages/language[@lang=current()/@lang]/@font"/></xsl:attribute>
					<xsl:attribute name="w:fareast"><xsl:value-of select="//languages/language[@lang=current()/@lang]/@font"/></xsl:attribute>
				</w:rFonts>
			</w:rPr>
			<w:basedOn  w:val="Interlin Vernacular"/>
		</w:style>
		<!--Main baseline text being annotated (word/item[@type='txt']) -->
		<w:style w:type="paragraph">
			<xsl:attribute name="w:styleId">Interlin Base <xsl:value-of select="@lang"/></xsl:attribute>
			<w:name>
				<xsl:attribute name="w:val">Interlin Base <xsl:value-of select="@lang"/></xsl:attribute>
			</w:name>
			<w:basedOn>
				<xsl:attribute name="w:val">Interlin Vern <xsl:value-of select="@lang"/></xsl:attribute>
			</w:basedOn>
			<w:rPr>
				<w:b  w:val="on"/>
			</w:rPr>
		</w:style>
		<!--The citation form line (morph/item[@type='cf']) -->
		<w:style w:type="paragraph">
			<xsl:attribute name="w:styleId">Interlin Cf <xsl:value-of select="@lang"/></xsl:attribute>
			<w:name>
				<xsl:attribute name="w:val">Interlin Cf <xsl:value-of select="@lang"/></xsl:attribute>
			</w:name>
			<w:basedOn>
				<xsl:attribute name="w:val">Interlin Vern <xsl:value-of select="@lang"/></xsl:attribute>
			</w:basedOn>
		</w:style>
		<!--Shows how the word was broken into morphemes (morph/item[@type='txt']) -->
		<w:style w:type="paragraph">
			<xsl:attribute name="w:styleId">Interlin Morph <xsl:value-of select="@lang"/></xsl:attribute>
			<w:name>
				<xsl:attribute name="w:val">Interlin Morph <xsl:value-of select="@lang"/></xsl:attribute>
			</w:name>
			<w:basedOn>
				<xsl:attribute name="w:val">Interlin Vern <xsl:value-of select="@lang"/></xsl:attribute>
			</w:basedOn>
		</w:style>
	</xsl:template>
	<xsl:template match="item" mode="style">

	<!--Base style for analysis stuff in a particular writing system -->
	<w:style w:type="paragraph">
		<xsl:attribute name="w:styleId">Interlin Analysis <xsl:value-of select="@lang"/></xsl:attribute>
		<w:name>
			<xsl:attribute name="w:val">Interlin Analysis <xsl:value-of select="@lang"/></xsl:attribute>
		</w:name>
		<w:rPr>
			<w:rFonts>
				<xsl:attribute name="w:ascii"><xsl:value-of select="//languages/language[@lang=current()/@lang]/@font"/></xsl:attribute>
				<xsl:attribute name="w:h-ansi"><xsl:value-of select="//languages/language[@lang=current()/@lang]/@font"/></xsl:attribute>
				<xsl:attribute name="w:cs"><xsl:value-of select="//languages/language[@lang=current()/@lang]/@font"/></xsl:attribute>
				<xsl:attribute name="w:fareast"><xsl:value-of select="//languages/language[@lang=current()/@lang]/@font"/></xsl:attribute>
			</w:rFonts>
		</w:rPr>
		<w:basedOn  w:val="Interlin Base"/>
	</w:style>
	<!--The morpheme gloss line (morph/item[@type='gls']) for a particular writing system -->
	<w:style w:type="paragraph">
		<xsl:attribute name="w:styleId">Interlin Morpheme Gloss <xsl:value-of select="@lang"/></xsl:attribute>
		<w:name>
			<xsl:attribute name="w:val">Interlin Morpheme Gloss <xsl:value-of select="@lang"/></xsl:attribute>
		</w:name>
		<w:basedOn>
			<xsl:attribute name="w:val">Interlin Analysis <xsl:value-of select="@lang"/></xsl:attribute>
		</w:basedOn>
	</w:style>
	<!--The word gloss line (word/item[@type='gls']) for a particular writing system -->
	<w:style w:type="paragraph">
		<xsl:attribute name="w:styleId">Interlin Word Gloss <xsl:value-of select="@lang"/></xsl:attribute>
		<w:name>
			<xsl:attribute name="w:val">Interlin Word Gloss <xsl:value-of select="@lang"/></xsl:attribute>
		</w:name>
		<w:basedOn>
			<xsl:attribute name="w:val">Interlin Analysis <xsl:value-of select="@lang"/></xsl:attribute>
		</w:basedOn>
	</w:style>
	<!--The phrase gloss line (phrase/item[@type='gls']) for a particular writing system -->
	<!-- enhance: possibly we should have distinct styles for free translation, literal translation, and note? -->
	<w:style w:type="paragraph">
		<xsl:attribute name="w:styleId">Interlin Freeform <xsl:value-of select="@lang"/></xsl:attribute>
		<w:name>
			<xsl:attribute name="w:val">Interlin Freeform <xsl:value-of select="@lang"/></xsl:attribute>
		</w:name>
		<w:basedOn>
			<xsl:attribute name="w:val">Interlin Analysis <xsl:value-of select="@lang"/></xsl:attribute>
		</w:basedOn>
		<w:pPr>
			<w:spacing  w:after="30"/>
		</w:pPr>
	</w:style>
</xsl:template>

  <!-- INTERLINEAR-TEXT LEVEL -->

  <xsl:template match="interlinear-text">
	<xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="interlinear-text/item">
	<w:p>
		<w:pPr>
			<w:pStyle w:val="Interlin Description"/>
		</w:pPr>
		<w:r>
			<w:t>
				<xsl:apply-templates/>
			</w:t>
		</w:r>
	</w:p>
  </xsl:template>

  <xsl:template match="interlinear-text/item[@type='title']">
	<w:p>
		<w:pPr>
			<w:pStyle w:val="Interlin Title"/>
		</w:pPr>
		<w:r>
			<w:t>
				<xsl:apply-templates/>
			</w:t>
		</w:r>
	</w:p>
  </xsl:template>
  <xsl:template match="interlinear-text/item[@type='title-abbreviation']"/>
  <xsl:template match="interlinear-text/item[@type='source']">
	<w:p>
		<w:pPr>
			<w:pStyle w:val="Interlin Source"/>
		</w:pPr>
		<w:r>
			<w:t>
				<xsl:apply-templates/>
			</w:t>
		</w:r>
	</w:p>
  </xsl:template>

  <!-- PARAGRAPH LEVEL -->

  <xsl:template match="paragraphs">
	<xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="paragraph">
	<xsl:apply-templates/>
  </xsl:template>

  <!-- PHRASE LEVEL (paragraphs of interlinear text in the output; currently whole sentences in most FieldWorks texts) -->

  <xsl:template match="phrases">
	<xsl:apply-templates/>
  </xsl:template>

  <!--For each phrase, we generate a paragraph of words (starting with a number, which is easiest to align by
  giving it its own shape), and then any phrase-level items. The inset="0" prevents apparent indentation
  of the number (if indentation is wanted set it on the Interlin Words style)-->
  <!-- Enhance: I think we can do this with fewer modes by using the select clause of apply-templates, but I
  did not know about that when I wrote this -->
  <xsl:template match="phrase">
	<xsl:if test="item[@type='segnum']">
	<w:p>
		<w:pPr>
			<w:pStyle w:val="Interlin Words"/>
			<xsl:if test="//language[@vernacular='true' and @RightToLeft='true']">
				<w:bidi/>
			</xsl:if>
		</w:pPr>
		<w:pict>
			<v:shape style="width:auto;height:auto"> <!--style="mso-wrap-style:none" makes the text box the right size, but they overlap -->
				<xsl:attribute name="id">interlinear pn<xsl:value-of select="item[@type='segnum']"/></xsl:attribute>
				<v:textbox style="mso-fit-shape-to-text:t;padding-right:0in" inset="0">
					<w:txbxContent>
						<w:p>
							<w:pPr>
								<w:pStyle w:val="Interlin Phrase Number"/>
								<xsl:if test="//language[@vernacular='true' and @RightToLeft='true']">
									<w:bidi/>
								</xsl:if>
							</w:pPr>
							<w:r>
								<w:t>
									<xsl:if test="//language[@vernacular='true' and @RightToLeft='true']">
										<xsl:text>&#160;</xsl:text>
									</xsl:if>
									<xsl:value-of select="item[@type='segnum']"/>
									<xsl:if test="count(//language[@vernacular='true' and @RightToLeft='true']) = 0">
										<xsl:text>&#160;</xsl:text>
									</xsl:if>
								</w:t>
							</w:r>
						</w:p>
					</w:txbxContent>
				</v:textbox>
			</v:shape>
		</w:pict>
		<xsl:apply-templates mode="words"/>
	</w:p>
	</xsl:if>
	<xsl:apply-templates mode="items"/>
  </xsl:template>

  <xsl:template match="phrase/item" mode="items">
	<xsl:if test="@type!='segnum'">
	<w:p>
		<w:pPr>
			<w:pStyle>
				<xsl:attribute name="w:val">Interlin Freeform <xsl:value-of select="@lang"/></xsl:attribute>
			</w:pStyle>
		</w:pPr>
		<w:r>
			<w:t>
			   <xsl:apply-templates/>
			</w:t>
		</w:r>
	</w:p>
	</xsl:if>
  </xsl:template>

  <xsl:template match="*" mode="items">
  </xsl:template>

  <!-- WORD LEVEL -->

  <xsl:template match="words" mode="words">
	<xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="*" mode="words">
  </xsl:template>

  <!--Each word bundle becomes a text box, which in WordML is a txbxContent inside a textbox inside a shape.
  The id attribute on the shape helps the Word macros identify just these shapes to adjust.
  Fit shape to text unfortunately doesn't quite seem to work all the way, but I think it helped some,
  possibly allowing the text box to shrink when it needs to? It certainly doesn't grow automatically to fit in Word 2003.-->
  <xsl:template match="word">
	<w:pict>
		<v:shape style="width:auto;height:auto"> <!--style="mso-wrap-style:none" makes the text box the right size, but they overlap -->
			<xsl:attribute name="id">interlinear <xsl:number level="any" count="word"/></xsl:attribute>
			<v:textbox style="mso-fit-shape-to-text:t;padding-right:0in" inset="0">
				<w:txbxContent>
					<xsl:apply-templates/>
				</w:txbxContent>
			</v:textbox>
		</v:shape>
	</w:pict>
  </xsl:template>

  <!-- the 160 is a Unicode non-breaking space, the easiest way to get some white space between things.
  It might be better to do it with something style-based -->
  <xsl:template match="word/item[@type='txt']">
	<w:p>
		<w:pPr>
			<w:pStyle>
				<xsl:attribute name="w:val">Interlin Base <xsl:value-of select="@lang"/></xsl:attribute>
			</w:pStyle>
			<xsl:if test="//language[@vernacular='true' and @RightToLeft='true']">
				<w:bidi/>
			</xsl:if>
		</w:pPr>
		<w:r>
			<w:t>
				<xsl:apply-templates/>
				<xsl:text>&#160;</xsl:text>
			</w:t>
		</w:r>
	</w:p>
  </xsl:template>
  <!-- no non-breaking space on this -->
  <xsl:template match="word/item[@type='punct']">
	<w:p>
		<w:pPr>
			<w:pStyle>
				<xsl:attribute name="w:val">Interlin Base <xsl:value-of select="@lang"/></xsl:attribute>
			</w:pStyle>
			<xsl:if test="//language[@vernacular='true' and @RightToLeft='true']">
				<w:bidi/>
			</xsl:if>
		</w:pPr>
		<w:r>
			<w:t>
				<xsl:apply-templates/>
			</w:t>
		</w:r>
	</w:p>
  </xsl:template>

  <xsl:template match="word/item[@type='gls']">
	<w:p>
		<w:pPr>
			<w:pStyle>
				<xsl:attribute name="w:val">Interlin Word Gloss <xsl:value-of select="@lang"/></xsl:attribute>
			</w:pStyle>
			<xsl:if test="//language[@vernacular='true' and @RightToLeft='true']">
				<w:bidi/>
			</xsl:if>
		</w:pPr>
		<w:r>
			<w:t>
				<xsl:if test="//language[@vernacular='true' and @RightToLeft='true']">
					<xsl:text>&#160;</xsl:text>
				</xsl:if>
				<xsl:apply-templates/>
				<xsl:if test="count(//language[@vernacular='true' and @RightToLeft='true']) = 0">
					<xsl:text>&#160;</xsl:text>
				</xsl:if>
			</w:t>
		</w:r>
	</w:p>
  </xsl:template>

  <xsl:template match="word/item[@type='pos']">
	<w:p>
		<w:pPr>
			<w:pStyle w:val="Interlin Word POS"/>
			<xsl:if test="//language[@vernacular='true' and @RightToLeft='true']">
				<w:bidi/>
			</xsl:if>
		</w:pPr>
		<w:r>
			<w:t>
				<xsl:if test="//language[@vernacular='true' and @RightToLeft='true']">
					<xsl:text>&#160;</xsl:text>
				</xsl:if>
				<xsl:apply-templates/>
				<xsl:if test="count(//language[@vernacular='true' and @RightToLeft='true']) = 0">
					<xsl:text>&#160;</xsl:text>
				</xsl:if>
			</w:t>
		</w:r>
	</w:p>
  </xsl:template>

  <!-- MORPHEME LEVEL -->

  <!-- The 20 is twips (1/20 point) which means our table columns are only one point wide.
  However, we tell the cells they can't wrap, so they grow the way we want (and shrink back if edited).
  If we don't set this gridCol value, it appears to work at first, but columns won't shrink below their
  original width when edited. -->
  <xsl:template match="morphemes">
	<w:tbl>
		<w:tblGrid>
			<w:gridCol w:w="20"/>
		</w:tblGrid>
		<!-- This flips the columns for RTL text.-->
		<xsl:if test="//language[@vernacular='true' and @RightToLeft='true']">
			<w:tblPr>
				<w:bidiVisual val="on"/>
			</w:tblPr>
		</xsl:if>
		<xsl:apply-templates select="morph[1]"/>
	</w:tbl>
  </xsl:template>

  <!-- This is the trick template that converts a column organization into a row organization.
  We assume the first morpheme has the same rows as all the others and use it as a model.
  In this special mode we process the items of only the first morph-->
  <xsl:template match="morph[1]">
	 <xsl:apply-templates mode="rows"/>
  </xsl:template>

  <!-- This is the other half of the trick template that converts a column organization into a row organization.
  This gets invoked only for the items of the first morph of the word. We find all the corresponding items
  in the other morphs (and this one) and output them as a row.
  The homograph number item is omitted because we don't want a separate row for these.-->
  <xsl:template match="item[@type!='hn' and @type!='variantTypes' and @type!='glsAppend']" mode="rows">
	<w:tr>
		<xsl:variable name="myType" select="@type"/>
		<xsl:variable name="myLang" select="@lang"/>
		<xsl:apply-templates select="../../morph/item[@type=$myType and @lang=$myLang]" mode="rowItems"/>
	</w:tr>
  </xsl:template>

  <!-- We output each morpheme level item as a table cell containing
  a paragraph with a suitable style, and a trailing non-breaking space.
  The gloss style is special because we tag it with the language name, in case there are multiple
  gloss languages. -->
  <xsl:template match="item" mode="rowItems">
	<w:tc>
		<w:tcPr><w:noWrap w:val="on"/></w:tcPr>
		<w:p>
		<w:pPr>
			<!-- playing safe, but the first option is not used. See more specific template for item[@type='txt']-->
			<xsl:if test="@type='txt'">
				<w:pStyle>
					<xsl:attribute name="w:val">Interlin Morph <xsl:value-of select="@lang"/></xsl:attribute>
				</w:pStyle>
			</xsl:if>
			<xsl:if test="@type='msa'">
				<w:pStyle w:val="Interlin Morpheme POS"/>
			</xsl:if>
			<xsl:if test="//language[@vernacular='true' and @RightToLeft='true']">
				<w:bidi/>
			</xsl:if>
		</w:pPr>
			<w:r>
				<w:t>
					<xsl:if test="//language[@vernacular='true' and @RightToLeft='true']">
						<xsl:text>&#160;</xsl:text>
					</xsl:if>
					<xsl:apply-templates/>
					<xsl:if test="count(//language[@vernacular='true' and @RightToLeft='true']) = 0">
						<xsl:text>&#160;</xsl:text>
					</xsl:if>
				</w:t>
			</w:r>
		</w:p>
	</w:tc>
  </xsl:template>
	<xsl:template match="item[@type='txt']" mode="rowItems">
		<w:tc>
			<w:tcPr>
				<w:noWrap w:val="on"/>
			</w:tcPr>
			<w:p>
				<w:pPr>
					<w:pStyle>
						<xsl:attribute name="w:val">Interlin Morph <xsl:value-of select="@lang"/></xsl:attribute>
					</w:pStyle>
					<xsl:if test="//language[@vernacular='true' and @RightToLeft='true']">
						<w:bidi/>
					</xsl:if>
				</w:pPr>
				<w:r>
					<w:t>
						<xsl:apply-templates/>
						<xsl:text>&#160;</xsl:text>
					</w:t>
				</w:r>
			</w:p>
		</w:tc>
	</xsl:template>

	<xsl:template match="*" mode="rowItems">
  </xsl:template>
  <xsl:template match="*" mode="rows">
  </xsl:template>

  <!-- suppress homograph numbers in normal mode, so they don't occupy an extra line-->
  <xsl:template match="morph/item[@type='hn']">
  </xsl:template>
  <xsl:template match="morph/item[@type='variantTypes']">
  </xsl:template>
  <xsl:template match="morph/item[@type='glsAppend']">
  </xsl:template>

  <!-- This mode occurs within the 'cf' item to display the homograph number from the following item.-->
  <xsl:template match="morph/item[@type='hn']" mode="hn">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="morph/item[@type='variantTypes']" mode="variantTypes">
	<xsl:apply-templates/>
  </xsl:template>

  <!-- a special item for citation form may include the homograph number (typically as a subscript)-->
	<xsl:template match="morph/item[@type='cf']" mode="rowItems">
	<w:tc>
		<w:tcPr><w:noWrap w:val="on"/></w:tcPr>
		<w:p>
		<w:pPr>
			<w:pStyle>
				<xsl:attribute name="w:val">Interlin Cf <xsl:value-of select="@lang"/></xsl:attribute>
			</w:pStyle>
			<xsl:if test="//language[@vernacular='true' and @RightToLeft='true']">
				<w:bidi/>
			</xsl:if>
		</w:pPr>
			<w:r>
				<w:t>
					<xsl:apply-templates/>
				</w:t>
			</w:r>
			<xsl:variable name="homographNumber" select="following-sibling::item[1][@type='hn']"/>
			<xsl:if test="$homographNumber">
				<w:r>
					<w:rPr>
						<w:rStyle w:val="Interlin Homograph"/>
					</w:rPr>
					<w:t>
						<xsl:apply-templates select="$homographNumber" mode="hn"/>
					</w:t>
				</w:r>
		   </xsl:if>
			<xsl:variable name="variantTypes" select="following-sibling::item[(count($homographNumber)+1)][@type='variantTypes']"/>
	  <xsl:if test="$variantTypes">
		<w:r>
		  <w:rPr>
			<w:rStyle w:val="Interlin Variant Types"/>
		  </w:rPr>
		  <w:t>
			<xsl:apply-templates select="$variantTypes" mode="variantTypes"/>
		  </w:t>
		</w:r>
	  </xsl:if>
			<xsl:text>&#160;</xsl:text>
		</w:p>
	</w:tc>
  </xsl:template>

  <xsl:template match="morph/item[@type='gls']" mode="rowItems">
	<w:tc>
	  <w:tcPr>
		<w:noWrap w:val="on"/>
	  </w:tcPr>
	  <w:p>
		<w:pPr>
		  <w:pStyle>
			<xsl:attribute name="w:val">Interlin Morpheme Gloss <xsl:value-of select="@lang"/></xsl:attribute>
		  </w:pStyle>
		  <xsl:if test="//language[@vernacular='true' and @RightToLeft='true']">
			<w:bidi/>
		  </xsl:if>
		</w:pPr>
		<w:r>
		  <w:t>
			<xsl:apply-templates/>
		  </w:t>
		</w:r>
		<xsl:variable name="glsAppend" select="following-sibling::item[1][@type='glsAppend']"/>
		<xsl:if test="$glsAppend">
		  <w:r>
			<w:rPr>
			  <w:rStyle w:val="Interlin Variant Types"/>
			</w:rPr>
			<w:t>
			  <xsl:apply-templates select="$glsAppend" mode="glsAppend"/>
			</w:t>
		  </w:r>
		</xsl:if>
		<xsl:text>&#160;</xsl:text>
	  </w:p>
	</w:tc>
  </xsl:template>
</xsl:stylesheet>
