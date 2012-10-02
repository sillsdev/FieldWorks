<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
xmlns:office="urn:oasis:names:tc:opendocument:xmlns:office:1.0"
xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
xmlns:xsd="http://www.w3.org/2001/XMLSchema"
xmlns:xforms="http://www.w3.org/2002/xforms"
xmlns:dom="http://www.w3.org/2001/xml-events"
xmlns:oooc="http://openoffice.org/2004/calc"
xmlns:ooow="http://openoffice.org/2004/writer"
xmlns:ooo="http://openoffice.org/2004/office"
xmlns:script="urn:oasis:names:tc:opendocument:xmlns:script:1.0"
xmlns:form="urn:oasis:names:tc:opendocument:xmlns:form:1.0"
xmlns:math="http://www.w3.org/1998/Math/MathML"
xmlns:dr3d="urn:oasis:names:tc:opendocument:xmlns:dr3d:1.0"
xmlns:chart="urn:oasis:names:tc:opendocument:xmlns:chart:1.0"
xmlns:svg="urn:oasis:names:tc:opendocument:xmlns:svg-compatible:1.0"
xmlns:number="urn:oasis:names:tc:opendocument:xmlns:datastyle:1.0"
xmlns:meta="urn:oasis:names:tc:opendocument:xmlns:meta:1.0"
xmlns:dc="http://purl.org/dc/elements/1.1/"
xmlns:xlink="http://www.w3.org/1999/xlink"
xmlns:fo="urn:oasis:names:tc:opendocument:xmlns:xsl-fo-compatible:1.0"
xmlns:draw="urn:oasis:names:tc:opendocument:xmlns:drawing:1.0"
xmlns:table="urn:oasis:names:tc:opendocument:xmlns:table:1.0"
xmlns:text="urn:oasis:names:tc:opendocument:xmlns:text:1.0"
xmlns:style="urn:oasis:names:tc:opendocument:xmlns:style:1.0"
version="1.0">
<!--  <xsl:output method="html" version="4.0" encoding="UTF-8" indent="yes" /> -->
  <xsl:output method="xml" encoding="UTF-8" indent="yes" />
	<!-- This key is used as part of a trick way to output several style definitions once for each UNIQUE lang attribute
  that occur in a gls item -->
	<xsl:key name="distinct-lang-gls" match="item[@type='gls']" use="@lang"/>
	<!-- This key is used as part of a trick way to output several style definitions once for each UNIQUE lang attribute
  that occur in a txt or cf item -->
	<xsl:key name="distinct-lang-txtcf" match="item[@type='txt' or @type='cf']" use="@lang"/>
	<!-- This key is used as part of a trick way to output several style definitions once for each UNIQUE font attribute
	 that occurs in a language element -->
	<xsl:key name="distinct-font" match="language" use="@font"/>

	<xsl:template match="document">
	  <office:document-styles office:version="1.0">
		  <!-- Parts of this are a copy of styles info from a blank OO document. -->
		<office:font-face-decls>
				<xsl:for-each select="//language[generate-id()=generate-id(key('distinct-font', @font))]">
					<style:font-face>
						<xsl:attribute name="style:name"><xsl:value-of select="@font"/></xsl:attribute>
						<xsl:attribute name="svg:font-family">&apos;<xsl:value-of select="@font"/>&apos;</xsl:attribute>
					</style:font-face>
				</xsl:for-each>
		</office:font-face-decls>
		<office:styles>
			<style:default-style style:family="graphic">
				<style:graphic-properties draw:shadow-offset-x="0.1181in" draw:shadow-offset-y="0.1181in" draw:start-line-spacing-horizontal="0.1114in" draw:start-line-spacing-vertical="0.1114in" draw:end-line-spacing-horizontal="0.1114in" draw:end-line-spacing-vertical="0.1114in" style:flow-with-text="false"/>
				<style:paragraph-properties style:text-autospace="ideograph-alpha" style:line-break="strict" style:writing-mode="lr-tb" style:font-independent-line-spacing="false">
					<style:tab-stops/>
				</style:paragraph-properties>
				<style:text-properties style:use-window-font-color="true" fo:font-size="12pt" fo:language="en" fo:country="US" style:font-size-asian="12pt" style:language-asian="none" style:country-asian="none" style:font-size-complex="12pt" style:language-complex="none" style:country-complex="none"/>
			</style:default-style>
			<style:default-style style:family="paragraph">
				<style:paragraph-properties fo:hyphenation-ladder-count="no-limit" style:text-autospace="ideograph-alpha" style:punctuation-wrap="hanging" style:line-break="strict" style:tab-stop-distance="0.4925in" style:writing-mode="page"/>
				<style:text-properties style:use-window-font-color="true" style:font-name="Times New Roman" fo:font-size="12pt" fo:language="en" fo:country="US" style:font-name-asian="Lucida Sans Unicode" style:font-size-asian="12pt" style:language-asian="none" style:country-asian="none" style:font-name-complex="Tahoma" style:font-size-complex="12pt" style:language-complex="none" style:country-complex="none" fo:hyphenate="false" fo:hyphenation-remain-char-count="2" fo:hyphenation-push-char-count="2"/>
			</style:default-style>
			<style:default-style style:family="table">
				<style:table-properties table:border-model="collapsing"/>
			</style:default-style>
			<style:default-style style:family="table-row">
				<style:table-row-properties fo:keep-together="auto"/>
			</style:default-style>
			<style:style style:name="Standard" style:family="paragraph" style:class="text"/>
			<style:style style:name="Text_20_body" style:display-name="Text body" style:family="paragraph" style:parent-style-name="Standard" style:class="text">
				<style:paragraph-properties fo:margin-top="0in" fo:margin-bottom="0.0835in"/>
			</style:style>
			<style:style style:name="Heading" style:family="paragraph" style:parent-style-name="Standard" style:next-style-name="Text_20_body" style:class="text">
				<style:paragraph-properties fo:margin-top="0.1665in" fo:margin-bottom="0.0835in" fo:keep-with-next="always"/>
				<style:text-properties style:font-name="Arial" fo:font-size="14pt" style:font-name-asian="MS Mincho" style:font-size-asian="14pt" style:font-name-complex="Tahoma" style:font-size-complex="14pt"/>
			</style:style>
			<style:style style:name="Heading_20_1" style:display-name="Heading 1" style:family="paragraph" style:parent-style-name="Heading" style:next-style-name="Text_20_body" style:class="text" style:default-outline-level="1">
				<style:text-properties fo:font-size="115%" fo:font-weight="bold" style:font-size-asian="115%" style:font-weight-asian="bold" style:font-size-complex="115%" style:font-weight-complex="bold"/>
			</style:style>
			<style:style style:name="List" style:family="paragraph" style:parent-style-name="Text_20_body" style:class="list">
				<style:text-properties style:font-name-complex="Tahoma1"/>
			</style:style>
			<style:style style:name="Caption" style:family="paragraph" style:parent-style-name="Standard" style:class="extra">
				<style:paragraph-properties fo:margin-top="0.0835in" fo:margin-bottom="0.0835in" text:number-lines="false" text:line-number="0"/>
				<style:text-properties fo:font-size="12pt" fo:font-style="italic" style:font-size-asian="12pt" style:font-style-asian="italic" style:font-name-complex="Tahoma1" style:font-size-complex="12pt" style:font-style-complex="italic"/>
			</style:style>
			<style:style style:name="Frame_20_contents" style:display-name="Frame contents" style:family="paragraph" style:parent-style-name="Text_20_body" style:class="extra"/>
			<style:style style:name="Index" style:family="paragraph" style:parent-style-name="Standard" style:class="index">
				<style:paragraph-properties text:number-lines="false" text:line-number="0"/>
				<style:text-properties style:font-name-complex="Tahoma1"/>
			</style:style>
			<style:style style:name="Frame" style:family="graphic">
				<style:graphic-properties text:anchor-type="paragraph" svg:x="0in" svg:y="0in" fo:margin-left="0.0791in" fo:margin-right="0.0791in" fo:margin-top="0.0791in" fo:margin-bottom="0.0791in" style:wrap="parallel" style:number-wrapped-paragraphs="no-limit" style:wrap-contour="false" style:vertical-pos="top" style:vertical-rel="paragraph-content" style:horizontal-pos="center" style:horizontal-rel="paragraph-content" fo:padding="0.0591in" fo:border="0.0008in solid #000000"/>
			</style:style>
			<!-- here we insert generated styles for the interlinear-->
			<!--Base for all interlinear styles -->
			<style:style style:name="Interlin_Base" style:display-name="Interlin Base" style:family="paragraph" style:parent-style-name="Standard" style:class="text">
				<style:paragraph-properties fo:margin-top="0in" fo:margin-bottom="0in"/>
			</style:style>
			<!--Title paragraph taken from item with type="title" in main document -->
			<style:style style:name="Interlin_Title" style:display-name="Interlin Title" style:family="paragraph" style:parent-style-name="Interlin_Base" style:class="text">
				<style:paragraph-properties fo:margin-top="2pt" fo:margin-bottom="6pt"/>
				<style:text-properties fo:font-size="14pt" fo:font-weight="bold" style:font-size-asian="14pt" style:font-weight-asian="bold" style:font-size-complex="14pt" style:font-weight-complex="bold"/>
			</style:style>
			<!--Source paragraph taken from item with type="source" in main document -->
			<style:style style:name="Interlin_Source" style:display-name="Interlin Source" style:family="paragraph" style:parent-style-name="Interlin_Base" style:class="text">
				<style:paragraph-properties fo:margin-top="2pt" fo:margin-bottom="2pt"/>
				<style:text-properties fo:font-size="12pt" fo:font-weight="bold" style:font-size-asian="12pt" style:font-weight-asian="bold" style:font-size-complex="12pt" style:font-weight-complex="bold"/>
			</style:style>
			<!--Used for the marker paragraphs that indicate the paragraphs in the original text. -->
			<style:style style:name="Interlin_Paragraph_Marker" style:display-name="Interlin Paragraph Marker" style:family="paragraph" style:parent-style-name="Interlin_Base" style:class="text">
				<style:paragraph-properties fo:margin-bottom="2pt" fo:keep-with-next="always"/>
				<style:text-properties fo:font-weight="bold" style:font-weight-asian="bold" style:font-weight-complex="bold"/>
			</style:style>
			<!--Used for root items with type="desc", typically a brief description of the whole document -->
			<style:style style:name="Interlin_Description" style:display-name="Interlin Description" style:family="paragraph" style:parent-style-name="Interlin_Base" style:class="text">
			</style:style>
			<!--Base for all vernacular text styles -->
			<style:style style:name="Interlin_Vernacular" style:display-name="Interlin Vernacular" style:family="paragraph" style:parent-style-name="Interlin_Base" style:class="text">
				<style:paragraph-properties fo:margin-bottom="0pt">
					<xsl:if test="//language[@vernacular='true' and @RightToLeft='true']">
						<xsl:attribute name="fo:text-align">end</xsl:attribute>
						<xsl:attribute name="style:writing-mode">rl-tb</xsl:attribute>
					</xsl:if>
				</style:paragraph-properties>
				<style:text-properties fo:language="none">
					<!-- Careful! White space is significant in these elements, don't let VS pretty-print them.-->
					<xsl:attribute name="style:font-name"><xsl:value-of select="//languages/language[@vernacular='true']/@font"/></xsl:attribute>
					<xsl:attribute name="style:font-name-asian"><xsl:value-of select="//languages/language[@vernacular='true']/@font"/></xsl:attribute>
					<xsl:attribute name="style:font-name-complex"><xsl:value-of select="//languages/language[@vernacular='true']/@font"/></xsl:attribute>
				</style:text-properties>
			</style:style>
			<!-- The paragraphs that contain the interlinear bundles of words.
				Modify this to adjust spacing and indentation of the interlinear phrases as a whole-->
			<style:style style:name="Interlin_Words" style:display-name="Interlin Words" style:family="paragraph" style:parent-style-name="Interlin_Base" style:class="text">
				<style:paragraph-properties fo:margin-bottom="0pt">
					<xsl:if test="//language[@vernacular='true' and @RightToLeft='true']">
						<xsl:attribute name="fo:text-align">end</xsl:attribute>
						<xsl:attribute name="style:writing-mode">rl-tb</xsl:attribute>
					</xsl:if>
				</style:paragraph-properties>
			</style:style>
			<!-- The paragraphs that contain the interlinear bundles of morphemes.
				Modify this to adjust spacing and indentation of the interlinear phrases as a whole-->
			<style:style style:name="Interlin_Morphemes" style:display-name="Interlin Morphemes" style:family="paragraph" style:parent-style-name="Interlin_Base" style:class="text">
				<style:paragraph-properties>
					<xsl:if test="//language[@vernacular='true' and @RightToLeft='true']">
						<xsl:attribute name="fo:text-align">end</xsl:attribute>
						<xsl:attribute name="style:writing-mode">rl-tb</xsl:attribute>
					</xsl:if>
				</style:paragraph-properties>
			</style:style>
			<!--For each unique language that occurs on an item of type txt or cf, generate some more styles
				with names that include the language id -->
			<xsl:for-each select="//item[(@type='txt' or @type='cf') and generate-id()=generate-id(key('distinct-lang-txtcf', @lang))]">
				<xsl:apply-templates mode="vstyle" select="."/>
			</xsl:for-each>

			<!--Used to mark the homograph number, which is output as an addendum to the citation form
				if it is present (morph/item[@type='hn']) -->
			- <style:style style:name="Interlin_Homograph" style:display-name="Interlin Homograph" style:family="text">
				<style:text-properties style:text-position="sub 58%" />
			</style:style>
			<!--Base style for things typically in the main analysis language -->
			<style:style style:name="Interlin_Analysis" style:display-name="Interlin Analysis" style:family="paragraph" style:parent-style-name="Interlin_Base" style:class="text">
				<style:paragraph-properties>
					<xsl:if test="//language[@vernacular='true' and @RightToLeft='true']">
						<xsl:attribute name="fo:text-align">end</xsl:attribute>
					</xsl:if>
				</style:paragraph-properties>
				<!-- eventually we may select the default analysis font here -->
				<!--style:text-properties>
					<xsl:attribute name="style:font-name">
						<xsl:value-of select="//languages/language[@vernacular='true']/@font"/>
					</xsl:attribute>
					<xsl:attribute name="style:font-name-asian">
						<xsl:value-of select="//languages/language[@vernacular='true']/@font"/>
					</xsl:attribute>
					<xsl:attribute name="style:font-name-complex">
						<xsl:value-of select="//languages/language[@vernacular='true']/@font"/>
					</xsl:attribute>
				</style:text-properties-->
			</style:style>
			<!--Used for numbering of individual phrases -->
			<style:style style:name="Interlin_Phrase_Number" style:display-name="Interlin Phrase Number" style:family="paragraph" style:parent-style-name="Interlin_Analysis" style:class="text">
			</style:style>
			<!--The morpheme part-of-speech (technically morpho-syntactic information) line (morph/item[@type='pos']) -->
			<style:style style:name="Interlin_Morpheme_POS" style:display-name="Interlin Morpheme POS" style:family="paragraph" style:parent-style-name="Interlin_Analysis" style:class="text">
				<style:text-properties fo:font-size="75%" />
			</style:style>
			<!--The word part-of-speech line (word/item[@type='pos']) -->
			<style:style style:name="Interlin_Word_POS" style:display-name="Interlin Word POS" style:family="paragraph" style:parent-style-name="Interlin_Analysis" style:class="text">
			</style:style>
			<!--For each unique language that occurs on an item of type gls, generate some more styles
				with names that include the language id -->
			<xsl:for-each select="//item[@type='gls' and generate-id()=generate-id(key('distinct-lang-gls', @lang))]">
				<xsl:apply-templates mode="style" select="."/>
			</xsl:for-each>
			<style:style style:name="Interlin_Frame" style:display-name="Interlin Frame" style:family="graphic" style:parent-style-name="Frame">
				<style:graphic-properties fo:margin-left="0in" fo:margin-right="0.05in" style:vertical-pos="top" style:vertical-rel="line"
					fo:padding="0in" fo:border="none" style:shadow="none" fo:margin-top="0in" fo:margin-bottom="0in"/>
			</style:style>
			<style:style style:name="Interlin_Frame_Morpheme" style:display-name="Interlin Frame Morpheme" style:family="graphic" style:parent-style-name="Interlin_Frame">
				<style:graphic-properties  fo:margin-right="0.0in" />
			</style:style>
			<style:style style:name="Interlin_Frame_Word" style:display-name="Interlin Frame Morpheme" style:family="graphic" style:parent-style-name="Interlin_Frame">
			</style:style>
			<style:style style:name="Interlin_Frame_Number" style:display-name="Interlin Frame Number" style:family="graphic" style:parent-style-name="Interlin_Frame">
			</style:style>
			<!-- and this is the 'tail end' of the stuff from a default OO styles.xml-->
			<text:outline-style>
				<text:outline-level-style text:level="1" style:num-format="">
					<style:list-level-properties text:min-label-distance="0.15in"/>
				</text:outline-level-style>
				<text:outline-level-style text:level="2" style:num-format="">
					<style:list-level-properties text:min-label-distance="0.15in"/>
				</text:outline-level-style>
				<text:outline-level-style text:level="3" style:num-format="">
					<style:list-level-properties text:min-label-distance="0.15in"/>
				</text:outline-level-style>
				<text:outline-level-style text:level="4" style:num-format="">
					<style:list-level-properties text:min-label-distance="0.15in"/>
				</text:outline-level-style>
				<text:outline-level-style text:level="5" style:num-format="">
					<style:list-level-properties text:min-label-distance="0.15in"/>
				</text:outline-level-style>
				<text:outline-level-style text:level="6" style:num-format="">
					<style:list-level-properties text:min-label-distance="0.15in"/>
				</text:outline-level-style>
				<text:outline-level-style text:level="7" style:num-format="">
					<style:list-level-properties text:min-label-distance="0.15in"/>
				</text:outline-level-style>
				<text:outline-level-style text:level="8" style:num-format="">
					<style:list-level-properties text:min-label-distance="0.15in"/>
				</text:outline-level-style>
				<text:outline-level-style text:level="9" style:num-format="">
					<style:list-level-properties text:min-label-distance="0.15in"/>
				</text:outline-level-style>
				<text:outline-level-style text:level="10" style:num-format="">
					<style:list-level-properties text:min-label-distance="0.15in"/>
				</text:outline-level-style>
			</text:outline-style>
			<text:notes-configuration text:note-class="footnote" style:num-format="1" text:start-value="0" text:footnotes-position="page" text:start-numbering-at="document"/>
			<text:notes-configuration text:note-class="endnote" style:num-format="i" text:start-value="0"/>
			<text:linenumbering-configuration text:number-lines="false" text:offset="0.1965in" style:num-format="1" text:number-position="left" text:increment="5"/>
		</office:styles>
		<office:automatic-styles>
			<style:page-layout style:name="pm1">
				<style:page-layout-properties fo:page-width="8.5in" fo:page-height="11in" style:num-format="1" style:print-orientation="portrait" fo:margin-top="0.7874in" fo:margin-bottom="0.7874in" fo:margin-left="0.7874in" fo:margin-right="0.1709in" style:writing-mode="lr-tb" style:footnote-max-height="0in">
					<style:footnote-sep style:width="0.0071in" style:distance-before-sep="0.0398in" style:distance-after-sep="0.0398in" style:adjustment="left" style:rel-width="25%" style:color="#000000"/>
				</style:page-layout-properties>
				<style:header-style/>
				<style:footer-style/>
			</style:page-layout>
		</office:automatic-styles>
		<office:master-styles>
			<style:master-page style:name="Standard" style:page-layout-name="pm1"/>
		</office:master-styles>
	</office:document-styles>

</xsl:template>
<!-- Gets invoked with a randomly-chosen item of type txt or cf (probably the first) having each unique @lang attribute.
	Used to generate several styles for each WritingSystem used in such items. We may over-generate slightly; for example,
	if one WS is used only for word word/txt, we will still generate a morpheme/txt style for it.-->
<xsl:template match="item" mode="vstyle">

	<!--Base style for vernacular stuff in a particular writing system, e.g., Interlin_Vern_en -->
	<style:style style:family="paragraph" style:parent-style-name="Interlin_Vernacular" style:class="text">
		<!-- Careful! White space is significant in these elements, don't let VS pretty-print them.-->
		<xsl:attribute name="style:name">Interlin_Vern_<xsl:value-of select="@lang"/></xsl:attribute>
		<xsl:attribute name="style:display-name">Interlin Vern <xsl:value-of select="@lang"/></xsl:attribute>
		<style:text-properties>
			<xsl:attribute name="style:font-name"><xsl:value-of select="//languages/language[@lang=current()/@lang]/@font"/></xsl:attribute>
			<xsl:attribute name="style:font-name-asian"><xsl:value-of select="//languages/language[@lang=current()/@lang]/@font"/></xsl:attribute>
			<xsl:attribute name="style:font-name-complex"><xsl:value-of select="//languages/language[@lang=current()/@lang]/@font"/></xsl:attribute>
		</style:text-properties>
	</style:style>
	<!--Main baseline text being annotated (word/item[@type='txt']) -->
	<style:style style:family="paragraph" style:class="text">
		<xsl:attribute name="style:name">Interlin_Base_<xsl:value-of select="@lang"/></xsl:attribute>
		<xsl:attribute name="style:display-name">Interlin Base <xsl:value-of select="@lang"/></xsl:attribute>
		<xsl:attribute name="style:parent-style-name">Interlin_Vern_<xsl:value-of select="@lang"/></xsl:attribute>
		<style:text-properties fo:font-weight="bold" style:font-weight-asian="bold" style:font-weight-complex="bold"/>
	</style:style>
	<!--Shows how the word was broken into morphemes (morph/item[@type='txt']) -->
	<style:style style:family="paragraph" style:class="text">
		<xsl:attribute name="style:name">Interlin_Morph_<xsl:value-of select="@lang"/></xsl:attribute>
		<xsl:attribute name="style:display-name">Interlin Morph <xsl:value-of select="@lang"/></xsl:attribute>
		<xsl:attribute name="style:parent-style-name">Interlin_Vern_<xsl:value-of select="@lang"/></xsl:attribute>
	</style:style>
	<!--The citation form line (morph/item[@type='cf']) -->
	<style:style style:family="paragraph" style:class="text">
		<xsl:attribute name="style:name">Interlin_Cf_<xsl:value-of select="@lang"/></xsl:attribute>
		<xsl:attribute name="style:display-name">Interlin Cf <xsl:value-of select="@lang"/></xsl:attribute>
		<xsl:attribute name="style:parent-style-name">Interlin_Vern_<xsl:value-of select="@lang"/></xsl:attribute>
	</style:style>

</xsl:template>

<!-- Gets invoked with a randomly-chosen item of type gls (probably the first) having each unique @lang attribute.
Used to generate several styles for each WritingSystem used in item glosses. We may over-generate slightly; for example,
if one WS is used only for word glosses, we will still generate a morpheme-gloss style for it.-->
<xsl:template match="item" mode="style">

	<!--Base style for analysis stuff in a particular writing system -->
	<style:style style:family="paragraph" style:parent-style-name="Interlin_Base" style:class="text">
		<!-- Careful! White space is significant in these elements, don't let VS pretty-print them.-->
		<xsl:attribute name="style:name">Interlin_Analysis_<xsl:value-of select="@lang"/></xsl:attribute>
		<xsl:attribute name="style:display-name">Interlin Analysis <xsl:value-of select="@lang"/></xsl:attribute>
		<style:text-properties>
			<xsl:attribute name="style:font-name"><xsl:value-of select="//languages/language[@lang=current()/@lang]/@font"/></xsl:attribute>
			<xsl:attribute name="style:font-name-asian"><xsl:value-of select="//languages/language[@lang=current()/@lang]/@font"/></xsl:attribute>
			<xsl:attribute name="style:font-name-complex"><xsl:value-of select="//languages/language[@lang=current()/@lang]/@font"/></xsl:attribute>
		</style:text-properties>
		<style:paragraph-properties>
			<xsl:if test="//language[@vernacular='true' and @RightToLeft='true']">
				<xsl:attribute name="fo:text-align">end</xsl:attribute>
			</xsl:if>
		</style:paragraph-properties>
	</style:style>
	<!--The morpheme gloss line (morph/item[@type='gls']) for a particular writing system, e.g. Interlin_Morpheme_Gloss_en -->
	<style:style style:family="paragraph" style:class="text">
		<!-- Careful! White space is significant in these elements, don't let VS pretty-print them.-->
		<xsl:attribute name="style:name">Interlin_Morpheme_Gloss_<xsl:value-of select="@lang"/></xsl:attribute>
		<xsl:attribute name="style:display-name">Interlin Morpheme Gloss <xsl:value-of select="@lang"/></xsl:attribute>
		<xsl:attribute name="style:parent-style-name">Interlin_Analysis_<xsl:value-of select="@lang"/></xsl:attribute>
	</style:style>
	<!--The word gloss line (word/item[@type='gls']) for a particular writing system -->
	<style:style style:family="paragraph" style:class="text">
		<!-- Careful! White space is significant in these elements, don't let VS pretty-print them.-->
		<xsl:attribute name="style:name">Interlin_Word_Gloss_<xsl:value-of select="@lang"/></xsl:attribute>
		<xsl:attribute name="style:display-name">Interlin Word Gloss <xsl:value-of select="@lang"/></xsl:attribute>
		<xsl:attribute name="style:parent-style-name">Interlin_Analysis_<xsl:value-of select="@lang"/></xsl:attribute>
	</style:style>
	<!--The phrase gloss line (phrase/item[@type='gls']) for a particular writing system -->
	<!-- enhance: possibly we should have distinct styles for free translation, literal translation, and note? -->
	<style:style style:family="paragraph" style:class="text">
		<!-- Careful! White space is significant in these elements, don't let VS pretty-print them.-->
		<xsl:attribute name="style:name">Interlin_Freeform_Gloss_<xsl:value-of select="@lang"/></xsl:attribute>
		<xsl:attribute name="style:display-name">Interlin Freeform Gloss <xsl:value-of select="@lang"/></xsl:attribute>
		<xsl:attribute name="style:parent-style-name">Interlin_Analysis_<xsl:value-of select="@lang"/></xsl:attribute>
		<style:paragraph-properties>
			<xsl:if test="//language[@vernacular='true' and @RightToLeft='true']">
				<xsl:attribute name="fo:text-align">start</xsl:attribute>
			</xsl:if>
		</style:paragraph-properties>
	</style:style>
</xsl:template>

</xsl:stylesheet>
