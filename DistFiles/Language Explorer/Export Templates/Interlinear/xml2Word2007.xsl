<?xml version="1.0" encoding="utf-8" standalone="yes"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
				xmlns="http://www.w3.org/1999/XSL/Format"
				xmlns:v="urn:schemas-microsoft-com:vml"
				version="1.0">
  <xsl:output method="xml" encoding="UTF-8" indent="yes" />
  <!-- This stylesheet transforms an interlinear text as output by FieldWorks into a form which can be directly read
  by Microsoft Word 2007. -->

  <!-- This key is used as part of a trick way to output several style definitions once for each UNIQUE lang attribute
  that occur in a gls item -->
  <xsl:key name="distinct-lang" match="item[@type='gls']" use="@lang"/>
	<!-- This key is used as part of a trick way to output several style definitions once for each UNIQUE lang attribute
  that occur in a txt or cf item -->
	<xsl:key name="distinct-lang-txtcf" match="item[@type='txt' or @type='cf']" use="@lang"/>

	<!-- Most of this is to generate a style sheet, with a distinct style for each line of text and a few other things.
  Then we output the actual interlinear data. -->
	<xsl:template match="document">
		<xsl:processing-instruction name="mso-application"> progid="Word.Document"</xsl:processing-instruction>
		<pkg:package xmlns:pkg="http://schemas.microsoft.com/office/2006/xmlPackage">
			<!-- This seems to be essentially a declaration that the package contains a main document part as it must have...but without this
			declaration, it isn't valid and won't load).-->
			<pkg:part pkg:name="/_rels/.rels" pkg:contentType="application/vnd.openxmlformats-package.relationships+xml" pkg:padding="512">
				<pkg:xmlData>
					<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
						<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
					</Relationships>
				</pkg:xmlData>
			</pkg:part>
			<!-- This seems to be the way to declare that it also has a style sheet. Without this, it ignores all the style info.-->
			<pkg:part pkg:name="/word/_rels/document.xml.rels" pkg:contentType="application/vnd.openxmlformats-package.relationships+xml" pkg:padding="256">
				<pkg:xmlData>
					<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
						<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
					</Relationships>
				</pkg:xmlData>
			</pkg:part>
			<!-- This is the main body of the document.-->
			<pkg:part pkg:name="/word/document.xml" pkg:contentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml">
				<pkg:xmlData>
					<w:document xmlns:ve="http://schemas.openxmlformats.org/markup-compatibility/2006"
								xmlns:o="urn:schemas-microsoft-com:office:office"
								xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"
								xmlns:m="http://schemas.openxmlformats.org/officeDocument/2006/math"
								xmlns:v="urn:schemas-microsoft-com:vml"
								xmlns:wp="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing"
								xmlns:w10="urn:schemas-microsoft-com:office:word"
								xmlns:wne="http://schemas.microsoft.com/office/word/2006/wordml"
								xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
						<w:body>
							<xsl:apply-templates/>
						</w:body>
					</w:document>
				</pkg:xmlData>
			</pkg:part>
			<!-- This is the definition of the styles used in the body.-->
			<pkg:part pkg:name="/word/styles.xml" pkg:contentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml">
				<pkg:xmlData>
					<w:styles xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
						<!--Base for all interlinear paragraph styles -->
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
						<!--Source paragraph taken from item with type="source" in main document -->
						<w:style w:type="paragraph" w:styleId="Interlin Source">
							<w:basedOn  w:val="Interlin Base"/>
							<w:name w:val="Interlin Source" />
							<w:pPr>
								<w:spacing  w:before="40" w:after="40"/>
								<w:keepNext w:val="on"/>
							</w:pPr>
							<w:rPr>
								<w:b  w:val="on"/>
								<w:sz  w:val="24"/>
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
						<!--Base for all vernacular text styles -->
						<w:style w:type="character" w:styleId="Interlin Vernacular">
							<w:name w:val="Interlin Vernacular" />
							<w:rPr>
								<w:noProof w:val="on"/>
								<w:rFonts>
									<xsl:attribute name="w:ascii">
										<xsl:value-of select="//languages/language[@vernacular='true']/@font"/>
									</xsl:attribute>
									<xsl:attribute name="w:h-ansi">
										<xsl:value-of select="//languages/language[@vernacular='true']/@font"/>
									</xsl:attribute>
									<xsl:attribute name="w:cs">
										<xsl:value-of select="//languages/language[@vernacular='true']/@font"/>
									</xsl:attribute>
									<xsl:attribute name="w:fareast">
										<xsl:value-of select="//languages/language[@vernacular='true']/@font"/>
									</xsl:attribute>
								</w:rFonts>
							</w:rPr>
						</w:style>
						<!--For each unique language that occurs on an item of type txt or cf, generate some more styles
						with names that include the language id -->
						<xsl:for-each select="//item[(@type='txt' or @type='cf') and generate-id()=generate-id(key('distinct-lang-txtcf', @lang))]">
							<xsl:apply-templates mode="vstyle" select="."/>
						</xsl:for-each>
						<!--Main baseline text being annotated (word/item[@type='txt']) -->
						<w:style w:type="character" w:styleId="Interlin Baseline">
							<w:name w:val="Interlin Baseline" />
							<w:basedOn  w:val="Interlin Vernacular"/>
							<w:rPr>
								<w:b  w:val="on"/>
							</w:rPr>
						</w:style>
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
						<w:style w:type="character" w:styleId="Interlin Analysis">
							<w:name w:val="Interlin Analysis" />
							<w:rPr>
								<w:rFonts>
									<xsl:attribute name="w:ascii">
										<xsl:value-of select="//languages/language[not(@vernacular) or @vernacular='false'][1]/@font"/>
									</xsl:attribute>
									<xsl:attribute name="w:h-ansi">
										<xsl:value-of select="//languages/language[not(@vernacular) or @vernacular='false'][1]/@font"/>
									</xsl:attribute>
									<xsl:attribute name="w:cs">
										<xsl:value-of select="//languages/language[not(@vernacular) or @vernacular='false'][1]/@font"/>
									</xsl:attribute>
									<xsl:attribute name="w:fareast">
										<xsl:value-of select="//languages/language[not(@vernacular) or @vernacular='false'][1]/@font"/>
									</xsl:attribute>
								</w:rFonts>
							</w:rPr>
						</w:style>
						<!--Used for numbering of individual phrases -->
						<w:style w:type="character" w:styleId="Interlin Phrase Number">
							<w:name w:val="Interlin Phrase Number" />
							<w:basedOn  w:val="Interlin Analysis"/>
						</w:style>
						<!--The morpheme part-of-speech (technically morpho-syntactic information) line (morph/item[@type='pos']) -->
						<w:style w:type="character" w:styleId="Interlin Morpheme POS">
							<w:name w:val="Interlin Morpheme POS" />
							<w:basedOn  w:val="Interlin Analysis"/>
						</w:style>
						<!--The word part-of-speech line (word/item[@type='pos']) -->
						<w:style w:type="character" w:styleId="Interlin Word POS">
							<w:name w:val="Interlin Word POS" />
							<w:basedOn  w:val="Interlin Analysis"/>
						</w:style>
						<!--For each unique language that occurs on an item of type gls, generate some more styles
				with names that include the language id -->
						<xsl:for-each select="//item[@type='gls' and generate-id()=generate-id(key('distinct-lang', @lang))]">
							<xsl:apply-templates mode="style" select="."/>
						</xsl:for-each>
					</w:styles>
				</pkg:xmlData>
			</pkg:part>
		</pkg:package>
	</xsl:template>

  <!-- Language-dependent styles -->

 <xsl:template match="item" mode="style" xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:m="http://schemas.openxmlformats.org/officeDocument/2006/math">
	 <!--Base style for analysis stuff in a particular writing system -->
	 <w:style w:type="character" >
		 <xsl:attribute name="w:styleId">Interlin Analysis <xsl:value-of select="@lang"/></xsl:attribute>
		 <w:name>
			 <xsl:attribute name="w:val">Interlin Analysis <xsl:value-of select="@lang"/></xsl:attribute>
		 </w:name>
		 <w:rPr>
			 <w:rFonts>
				 <xsl:attribute name="w:ascii">
					 <xsl:value-of select="//languages/language[@lang=current()/@lang]/@font"/>
				 </xsl:attribute>
				 <xsl:attribute name="w:h-ansi">
					 <xsl:value-of select="//languages/language[@lang=current()/@lang]/@font"/>
				 </xsl:attribute>
				 <xsl:attribute name="w:cs">
					 <xsl:value-of select="//languages/language[@lang=current()/@lang]/@font"/>
				 </xsl:attribute>
				 <xsl:attribute name="w:fareast">
					 <xsl:value-of select="//languages/language[@lang=current()/@lang]/@font"/>
				 </xsl:attribute>
			 </w:rFonts>
		 </w:rPr>
		 <w:basedOn  w:val="Interlin Base"/>
	 </w:style>
	 <!--The morpheme gloss line (morph/item[@type='gls']) for a particular writing system -->
	 <w:style w:type="character">
		<xsl:attribute name="w:styleId">Interlin Morpheme Gloss <xsl:value-of select="@lang"/></xsl:attribute>
		<w:name>
			<xsl:attribute name="w:val">Interlin Morpheme Gloss <xsl:value-of select="@lang"/></xsl:attribute>
		</w:name>
		<w:basedOn>
			<xsl:attribute name="w:val">Interlin Analysis <xsl:value-of select="@lang"/></xsl:attribute>
		</w:basedOn>
	</w:style>
	<!--The word gloss line (word/item[@type='gls']) for a particular writing system -->
	<w:style w:type="character">
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
	<xsl:template match="item" mode="vstyle" xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:m="http://schemas.openxmlformats.org/officeDocument/2006/math">
		<!--Base style for vernacular stuff in a particular writing system, e.g., Interlin_Vern_en -->
		<w:style w:type="character" >
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
		<w:style w:type="character">
			<xsl:attribute name="w:styleId">Interlin Base <xsl:value-of select="@lang"/></xsl:attribute>
			<w:name>
				<xsl:attribute name="w:val">Interlin Base <xsl:value-of select="@lang"/></xsl:attribute>
			</w:name>
			<w:basedOn>
				<xsl:attribute name="w:val">Interlin Vern <xsl:value-of select="@lang"/></xsl:attribute>
			</w:basedOn>
		</w:style>
		<!--The citation form line (morph/item[@type='cf']) -->
		<w:style w:type="character">
			<xsl:attribute name="w:styleId">Interlin Cf <xsl:value-of select="@lang"/></xsl:attribute>
			<w:name>
				<xsl:attribute name="w:val">Interlin Cf <xsl:value-of select="@lang"/></xsl:attribute>
			</w:name>
			<w:basedOn>
				<xsl:attribute name="w:val">Interlin Vern <xsl:value-of select="@lang"/></xsl:attribute>
			</w:basedOn>
		</w:style>
		<!--Shows how the word was broken into morphemes (morph/item[@type='txt']) -->
		<w:style w:type="character">
			<xsl:attribute name="w:styleId">Interlin Morph <xsl:value-of select="@lang"/></xsl:attribute>
			<w:name>
				<xsl:attribute name="w:val">Interlin Morph <xsl:value-of select="@lang"/></xsl:attribute>
			</w:name>
			<w:basedOn>
				<xsl:attribute name="w:val">Interlin Vern <xsl:value-of select="@lang"/></xsl:attribute>
			</w:basedOn>
		</w:style>
	</xsl:template>

	<!-- INTERLINEAR-TEXT LEVEL -->

  <xsl:template match="interlinear-text" xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:m="http://schemas.openxmlformats.org/officeDocument/2006/math">
	<xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="interlinear-text/item" xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:m="http://schemas.openxmlformats.org/officeDocument/2006/math">
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

  <xsl:template match="interlinear-text/item[@type='title']" xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:m="http://schemas.openxmlformats.org/officeDocument/2006/math">
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
  <xsl:template match="interlinear-text/item[@type='title-abbreviation']" xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:m="http://schemas.openxmlformats.org/officeDocument/2006/math"/>
  <xsl:template match="interlinear-text/item[@type='source']" xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:m="http://schemas.openxmlformats.org/officeDocument/2006/math">
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

  <!-- generates a 'paragraph N' label for each paragraph of the original text -->
  <xsl:template match="paragraph" xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:m="http://schemas.openxmlformats.org/officeDocument/2006/math">
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
  <xsl:template match="phrase" xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:m="http://schemas.openxmlformats.org/officeDocument/2006/math">
	<xsl:if test="item[@type='segnum']">
	<w:p>
		<w:pPr>
			<w:pStyle w:val="Interlin Words"/>
			<xsl:if test="//language[@vernacular='true' and @RightToLeft='true']">
				<w:bidi/>
			</xsl:if>
		</w:pPr>
		<w:r>
			<w:rPr>
				<w:rStyle w:val="Interlin Phrase Number"/>
			</w:rPr>
			<w:t>
				<xsl:value-of select="item[@type='segnum']"/>
			</w:t>
		</w:r>
		<w:r>
			<xsl:if test="count(//language[@vernacular='true' and @RightToLeft='true'])">
				<w:rPr>
					<w:rtl/>
				</w:rPr>
			</xsl:if>
			<w:t xml:space="preserve"> </w:t>
		</w:r>
		<xsl:apply-templates mode="words"/>
	</w:p>
	</xsl:if>
	<xsl:apply-templates mode="items"/>
  </xsl:template>

  <xsl:template match="phrase/item" mode="items" xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:m="http://schemas.openxmlformats.org/officeDocument/2006/math">
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

  <!--Each word bundle becomes a mathematical equation-->
  <xsl:template match="word" xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:m="http://schemas.openxmlformats.org/officeDocument/2006/math">
	  <m:oMath>
		  <m:m>
			  <m:mPr>
				  <m:baseJc m:val="top"/>
				  <m:rSpRule m:val="3"/>
				  <m:rSp m:val="20"/>
				  <m:mcs>
					  <m:mc>
						  <m:mcPr>
							  <m:count m:val="1"/>
							  <xsl:choose>
								  <xsl:when test="count(//language[@vernacular='true' and @RightToLeft='true'])">
									  <m:mcJc m:val="right"/>
								  </xsl:when>
								  <xsl:otherwise>
									  <m:mcJc m:val="left"/>
								  </xsl:otherwise>
							  </xsl:choose>
						  </m:mcPr>
					  </m:mc>
				  </m:mcs>
			  </m:mPr>
			  <xsl:apply-templates/>
		  </m:m>
	  </m:oMath>
	  <w:r>
		  <!-- w:t xml:space="preserve"> </w:t -->
		  <!--w:t>
			  <xsl:if test="count(//language[@vernacular='true' and @RightToLeft='true'])">
				  <xsl:text>&#x200F;</xsl:text>
			  </xsl:if>
			  &#x20;
			  <xsl:if test="count(//language[@vernacular='true' and @RightToLeft='true'])">
				  <xsl:text>&#x200F;</xsl:text>
			  </xsl:if>
		  </w:t-->
		  <xsl:if test="count(//language[@vernacular='true' and @RightToLeft='true'])">
			  <w:rPr>
				  <w:rtl/>
			  </w:rPr>
		  </xsl:if>
		  <w:t xml:space="preserve"> </w:t>
	  </w:r>
  </xsl:template>

  <!-- the 160 is a Unicode non-breaking space, the easiest way to get some white space between things.
  It might be better to do it with something style-based.
  A txt item is a row in the matrix. -->
  <xsl:template match="word/item[@type='txt']" xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:m="http://schemas.openxmlformats.org/officeDocument/2006/math">
	  <m:mr>
		  <m:e>
			  <m:r>
				  <m:rPr>
					  <m:nor/>
				  </m:rPr>
				  <w:rPr>
					<w:rStyle>
						<xsl:attribute name="w:val">Interlin Base <xsl:value-of select="@lang"/></xsl:attribute>
					</w:rStyle>
				  </w:rPr>
				  <m:t>
					  <xsl:apply-templates/>
					  <xsl:text>&#160;</xsl:text>
				  </m:t>
			  </m:r>
		  </m:e>
	  </m:mr>
  </xsl:template>

  <!-- no non-breaking space on this -->
  <xsl:template match="word/item[@type='punct']" xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:m="http://schemas.openxmlformats.org/officeDocument/2006/math">
	  <m:mr>
		  <m:e>
			  <m:r>
				  <m:rPr>
					  <m:nor/>
				  </m:rPr>
				  <w:rPr>
					<w:rStyle>
						<xsl:attribute name="w:val">Interlin Base <xsl:value-of select="@lang"/></xsl:attribute>
					</w:rStyle>
						<xsl:if test="count(//language[@lang=current()/@lang and @RightToLeft='true'])">
							<w:rtl/>
						</xsl:if>
				  </w:rPr>
				  <m:t>
					  <xsl:apply-templates/>
					  <xsl:text>&#160;</xsl:text>
				  </m:t>
			  </m:r>
		  </m:e>
	  </m:mr>
  </xsl:template>

  <xsl:template match="word/item[@type='gls']" xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:m="http://schemas.openxmlformats.org/officeDocument/2006/math">
	  <m:mr>
		  <m:e>
			  <m:r>
				  <m:rPr>
					  <m:nor/>
				  </m:rPr>
				  <w:rPr>
					  <w:rStyle>
						  <xsl:attribute name="w:val">Interlin Word Gloss <xsl:value-of select="@lang"/></xsl:attribute>
						  <xsl:if test="count(//language[@lang=current()/@lang and @RightToLeft='true'])">
							  <w:rtl/>
						  </xsl:if>
					  </w:rStyle>
				  </w:rPr>
				  <m:t>
					  <xsl:if test="//language[@vernacular='true' and @RightToLeft='true']">
						  <xsl:text>&#160;</xsl:text>
					  </xsl:if>
					  <xsl:apply-templates/>
					  <xsl:if test="count(//language[@vernacular='true' and @RightToLeft='true']) = 0">
						  <xsl:text>&#160;</xsl:text>
					  </xsl:if>
				  </m:t>
			  </m:r>
		  </m:e>
	  </m:mr>
  </xsl:template>

  <xsl:template match="word/item[@type='pos']" xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:m="http://schemas.openxmlformats.org/officeDocument/2006/math">
	  <m:mr>
		  <m:e>
			  <m:r>
				  <m:rPr>
					  <m:nor/>
				  </m:rPr>
				  <w:rPr>
					  <w:rStyle>
						  <xsl:attribute name="w:val">Interlin Word POS</xsl:attribute>
						  <xsl:if test="count(//language[@lang=current()/@lang and @RightToLeft='true'])">
							  <w:rtl/>
						  </xsl:if>
					  </w:rStyle>
				  </w:rPr>
				  <m:t>
					  <xsl:if test="//language[@vernacular='true' and @RightToLeft='true']">
						  <xsl:text>&#160;</xsl:text>
					  </xsl:if>
					  <xsl:apply-templates/>
					  <xsl:if test="count(//language[@vernacular='true' and @RightToLeft='true']) = 0">
						  <xsl:text>&#160;</xsl:text>
					  </xsl:if>
				  </m:t>
			  </m:r>
		  </m:e>
	  </m:mr>
  </xsl:template>

  <!-- MORPHEME LEVEL -->

  <!-- The 20 is twips (1/20 point) which means our table columns are only one point wide.
  However, we tell the cells they can't wrap, so they grow the way we want (and shrink back if edited).
  If we don't set this gridCol value, it appears to work at first, but columns won't shrink below their
  original width when edited. -->
  <xsl:template match="morphemes" xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:m="http://schemas.openxmlformats.org/officeDocument/2006/math">
	  <m:mr>
		  <m:e>
			  <m:m>
				  <m:mPr>
					  <m:baseJc m:val="top"/>
					  <m:rSpRule m:val="3"/>
					  <m:rSp m:val="20"/>
					  <m:mcs>
						  <m:mc>
							  <m:mcPr>
								  <m:count>
									  <xsl:attribute name="m:val">
										  <xsl:value-of select="count(morph)"/>
									  </xsl:attribute>
								  </m:count>
								  <xsl:choose>
									  <xsl:when test="count(//language[@vernacular='true' and @RightToLeft='true'])">
										  <m:mcJc m:val="right"/>
									  </xsl:when>
									  <xsl:otherwise>
										  <m:mcJc m:val="left"/>
									  </xsl:otherwise>
								  </xsl:choose>
							  </m:mcPr>
						  </m:mc>
					  </m:mcs>
				  </m:mPr>
				  <xsl:apply-templates select="morph[1]"/>
			  </m:m>
		  </m:e>
	  </m:mr>
  </xsl:template>

  <!-- This is the trick template that converts a column organization into a row organization.
  We assume the first morpheme has the same rows as all the others and use it as a model.
  In this special mode we process the items of only the first morph-->
  <xsl:template match="morph[1]">
	 <xsl:apply-templates mode="rows"/>
  </xsl:template>

  <!-- This is the other half of the trick template that converts a column organization into a row organization.
  This gets invoked only for the items of the first morph of the word. We find all the corresponding items
  in the other morphs (and this one) and output them as a matrix row.
  The homograph number item is omitted because we don't want a separate row for these.-->
  <xsl:template match="item[@type!='hn' and @type!='variantTypes' and @type!='glsAppend']" mode="rows" xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:m="http://schemas.openxmlformats.org/officeDocument/2006/math">
	<m:mr>
		<xsl:variable name="myType" select="@type"/>
		<xsl:variable name="myLang" select="@lang"/>
		<!-- Two cases differ only by the sort in reverse order. Actually reversing the morphemes seems to be the only
		way to get the morphems in RTL in Word 2007. According to Murray Sargent at Microsoft, RTL Math was 'postponed'.-->
		<xsl:choose>
			<xsl:when test="count(//language[@vernacular='true' and @RightToLeft='true'])">
				<xsl:apply-templates select="../../morph/item[@type=$myType and @lang=$myLang]" mode="rowItems">
					<xsl:sort select="position()" data-type="number" order="descending"/>
				</xsl:apply-templates>
			</xsl:when>
			<xsl:otherwise>
				<xsl:apply-templates select="../../morph/item[@type=$myType and @lang=$myLang]" mode="rowItems">
				</xsl:apply-templates>
			</xsl:otherwise>
		</xsl:choose>
	</m:mr>
  </xsl:template>

  <!-- We output each morpheme level item as a matrix element containing
  the text with a suitable style, and a trailing non-breaking space.
  The gloss style is special because we tag it with the language name, in case there are multiple
  gloss languages. -->
	<xsl:template match="item" mode="rowItems" xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:m="http://schemas.openxmlformats.org/officeDocument/2006/math">
		<m:e>
			<m:r>
				<m:rPr>
					<m:nor/>
				</m:rPr>
				<w:rPr>
					<xsl:if test="@type='txt'">
						<w:rStyle>
							<xsl:attribute name="w:val">Interlin Morph <xsl:value-of select="@lang"/></xsl:attribute>
						</w:rStyle>
						<xsl:if test="count(//language[@lang=current()/@lang and @RightToLeft='true'])">
							<w:rtl/>
						</xsl:if>
					</xsl:if>
					<xsl:if test="@type='msa'">
						<w:rStyle w:val="Interlin Morpheme POS"/>
					</xsl:if>
					<xsl:if test="count(//language[@lang=current()/@lang and @RightToLeft='true'])">
						<w:rtl/>
					</xsl:if>
				</w:rPr>
				<m:t>
					<xsl:if test="//language[@vernacular='true' and @RightToLeft='true']">
						<xsl:text>&#160;</xsl:text>
					</xsl:if>
					<xsl:apply-templates/>
					<xsl:if test="count(//language[@vernacular='true' and @RightToLeft='true']) = 0">
						<xsl:text>&#160;</xsl:text>
					</xsl:if>
				</m:t>
			</m:r>
		</m:e>
	</xsl:template>

	  <!-- Any other kind of item don't output.-->
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
  <xsl:template match="morph/item[@type='glsAppend']" mode="glsAppend">
	<xsl:apply-templates/>
  </xsl:template>

  <!-- a special item for citation form may include the homograph number (typically as a subscript)-->
	<xsl:template match="morph/item[@type='cf']" mode="rowItems" xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:m="http://schemas.openxmlformats.org/officeDocument/2006/math">
		<m:e>
			<m:r>
				<m:rPr>
					<m:nor/>
				</m:rPr>
		  <xsl:if test="//language[@vernacular='true' and @RightToLeft='true']">
			<m:t>
			<xsl:text>&#160;</xsl:text>
			</m:t>
		  </xsl:if>
		<w:rPr>
					<w:rStyle>
						<xsl:attribute name="w:val">Interlin Cf <xsl:value-of select="@lang"/></xsl:attribute>
					</w:rStyle>
					<xsl:if test="count(//language[@lang=current()/@lang and @RightToLeft='true'])">
						<w:rtl/>
					</xsl:if>
				</w:rPr>
				<m:t>
					<xsl:apply-templates/>
				</m:t>
				<xsl:variable name="homographNumber" select="following-sibling::item[1][@type='hn']"/>
				<xsl:if test="$homographNumber">
					<w:rPr>
						<w:rStyle w:val="Interlin Homograph"/>
					</w:rPr>
					<m:t>
						<xsl:apply-templates select="$homographNumber" mode="hn"/>
					</m:t>
				</xsl:if>
		<xsl:variable name="variantTypes" select="following-sibling::item[(count($homographNumber)+1)][@type='variantTypes']"/>
		<xsl:if test="$variantTypes">
		  <w:rPr>
			<w:rStyle w:val="Interlin Variant Types"/>
		  </w:rPr>
		  <m:t>
			<xsl:apply-templates select="$variantTypes" mode="variantTypes"/>
		  </m:t>
		</xsl:if>
		<xsl:if test="count(//language[@vernacular='true' and @RightToLeft='true']) = 0">
		  <m:t>
			<xsl:text>&#160;</xsl:text>
		  </m:t>
		</xsl:if>
			</m:r>
		</m:e>
  </xsl:template>

  <xsl:template match="morph/item[@type='gls']" mode="rowItems" xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:m="http://schemas.openxmlformats.org/officeDocument/2006/math">
	<m:e>
	  <m:r>
		<m:rPr>
		  <m:nor/>
		</m:rPr>
		<xsl:if test="//language[@vernacular='true' and @RightToLeft='true']">
		  <m:t>
			<xsl:text>&#160;</xsl:text>
		  </m:t>
		</xsl:if>
		<w:rPr>
		  <w:rStyle>
			<xsl:attribute name="w:val">Interlin Morph <xsl:value-of select="@lang"/></xsl:attribute>
		  </w:rStyle>
		  <xsl:if test="count(//language[@lang=current()/@lang and @RightToLeft='true'])">
			<w:rtl/>
		  </xsl:if>
		</w:rPr>
		<m:t>
		  <xsl:apply-templates/>
		</m:t>
		<xsl:variable name="glsAppend" select="following-sibling::item[1][@type='glsAppend']"/>
		<xsl:if test="$glsAppend">
		  <w:rPr>
			<w:rStyle w:val="Interlin Variant Types"/>
		  </w:rPr>
		  <m:t>
			<xsl:apply-templates select="$glsAppend" mode="glsAppend"/>
		  </m:t>
		</xsl:if>
		<xsl:if test="count(//language[@vernacular='true' and @RightToLeft='true']) = 0">
		   <m:t>
			<xsl:text>&#160;</xsl:text>
		  </m:t>
		</xsl:if>
	  </m:r>
	</m:e>
  </xsl:template>

</xsl:stylesheet>
