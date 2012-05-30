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

	<!-- This key is used as part of a trick way to output several style definitions once for each UNIQUE font attribute
	 that occurs in a language element -->
	<xsl:key name="distinct-font" match="language" use="@font"/>
	<xsl:template match="document">
	<office:document-content office:version="1.0">
		- <office:automatic-styles>
			- <style:style style:name="fr1" style:family="graphic" style:parent-style-name="Frame">
				<style:graphic-properties fo:margin-left="0in" fo:margin-right="0.05in" style:vertical-pos="top" style:vertical-rel="baseline" fo:padding="0in" fo:border="none" style:shadow="none" />
			</style:style>
		</office:automatic-styles>
		<office:body>
				<office:text>
					<xsl:apply-templates/>
				</office:text>
			</office:body>
	</office:document-content>
  </xsl:template>

  <!-- INTERLINEAR-TEXT LEVEL -->

  <xsl:template match="interlinear-text">
	<xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="interlinear-text/item">
	<text:p text:style-name="Default">
		<xsl:apply-templates/>
	</text:p>
  </xsl:template>

  <xsl:template match="interlinear-text/item[@type='title']">
	<text:p text:style-name="Interlin_Title">
		<xsl:apply-templates/>
	</text:p>
  </xsl:template>
  <xsl:template match="interlinear-text/item[@type='title-abbreviation']"/>
  <xsl:template match="interlinear-text/item[@type='source']">
	<text:p text:style-name="Interlin_Source">
		<xsl:apply-templates/>
	</text:p>
  </xsl:template>

  <!-- PARAGRAPH LEVEL -->

  <xsl:template match="paragraphs">
	<xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="paragraph">
	<xsl:apply-templates/>
  </xsl:template>

  <!-- PHRASE LEVEL -->

  <xsl:template match="phrases">
	<xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="phrase">
	  <text:p text:style-name="Interlin_Words">
		<draw:frame text:anchor-type="as-char" draw:style-name="Interlin_Frame_Number" fo:min-width="0.1402in">
			<xsl:attribute name="draw:name">Frame<xsl:number level="any"/></xsl:attribute>
			<xsl:attribute name="draw:z-index"><xsl:number level="any" count="item"/></xsl:attribute>
			<draw:text-box fo:min-height="0.1402in">
			  <xsl:if test="item[@type='segnum']">
				<text:p text:style-name="Interlin_Phrase_Number">
				  <xsl:value-of select="item[@type='segnum']"/>
				</text:p>
			  </xsl:if>
			</draw:text-box>
		</draw:frame>
		<xsl:apply-templates mode="words"/>
	</text:p>
	<xsl:apply-templates mode="items"/>
  </xsl:template>

  <xsl:template match="phrase/item" mode="items"> <!-- freeform in its own paragraph -->
	<xsl:if test="@type!='segnum'">
	  <text:p>
		  <xsl:attribute name="text:style-name">Interlin_Freeform_Gloss_<xsl:value-of select="@lang"/></xsl:attribute>
		  <xsl:apply-templates/>
	  </text:p>
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

  <xsl:template match="word">
	<draw:frame text:anchor-type="as-char" draw:style-name="Interlin_Frame_Word" fo:min-width="0.1402in">
		<xsl:attribute name="draw:name">Frame<xsl:number level="any"/></xsl:attribute>
		<xsl:attribute name="draw:z-index">
			<xsl:number level="any" count="item"/>
		</xsl:attribute>
		<draw:text-box fo:min-height="0.1402in">
			<xsl:apply-templates/>
		</draw:text-box>
	</draw:frame>
  </xsl:template>

  <xsl:template match="word/item[@type='txt']">
	  <text:p>
		<xsl:attribute name="text:style-name">Interlin_Base_<xsl:value-of select="@lang"/></xsl:attribute>
		<xsl:apply-templates/>
	</text:p>
  </xsl:template>

  <xsl:template match="word/item[@type='punct']">
	  <text:p text:style-name="Interlin_Baseline">
		<xsl:apply-templates/>
	</text:p>
  </xsl:template>

  <xsl:template match="word/item[@type='gls']">
	<text:p>
		<xsl:attribute name="text:style-name">Interlin_Word_Gloss_<xsl:value-of select="@lang"/></xsl:attribute>
		<xsl:apply-templates/>
	</text:p>
  </xsl:template>

  <xsl:template match="word/item[@type='pos']">
	  <text:p text:style-name="Interlin_Word_POS">
		<xsl:apply-templates/>
	</text:p>
  </xsl:template>

  <!-- MORPHEME LEVEL -->

  <xsl:template match="morphemes">
	  <text:p text:style-name="Interlin_Morphemes">
		  <xsl:apply-templates/>
	  </text:p>
	</xsl:template>

	<xsl:template match="morph">
		<draw:frame text:anchor-type="as-char" draw:style-name="Interlin_Frame_Morpheme" fo:min-width="0.1402in">
			<xsl:attribute name="draw:name">Frame<xsl:number level="any"/></xsl:attribute>
			<xsl:attribute name="draw:z-index">
				<xsl:number level="any" count="item"/>
			</xsl:attribute>
			<draw:text-box fo:min-height="0.1402in">
				<xsl:apply-templates mode="rowItems"/>
			</draw:text-box>
		</draw:frame>
	</xsl:template>

  <xsl:template match="item[@type!='hn' and @type!='variantTypes' and @type!='glsAppend']" mode="rowItems">
	  <text:p>
		  <xsl:if test="@type='txt'">
			<xsl:attribute name="text:style-name">Interlin_Morph_<xsl:value-of select="@lang"/></xsl:attribute>
		  </xsl:if>
		  <xsl:if test="@type='cf'">
			  <xsl:attribute name="text:style-name">Interlin_Cf_<xsl:value-of select="@lang"/></xsl:attribute>
		  </xsl:if>
		  <xsl:if test="@type='msa'">
			  <xsl:attribute name="text:style-name">Interlin_Morpheme_POS</xsl:attribute>
		  </xsl:if>
		  <xsl:apply-templates/>
	  </text:p>
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
	  <text:span text:style-name="Interlin_Homograph"><xsl:apply-templates/></text:span>
  </xsl:template>
  <xsl:template match="morph/item[@type='variantTypes']" mode="variantTypes">
	<text:span text:style-name="Interlin_VariantTypes"><xsl:apply-templates/></text:span>
  </xsl:template>
  <xsl:template match="morph/item[@type='glsAppend']" mode="glsAppend">
	<text:span text:style-name="Interlin_VariantTypes"><xsl:apply-templates/></text:span>
  </xsl:template>

  <xsl:template match="morph/item[@type='cf']" mode="rowItems">
		<text:p>
			<xsl:attribute name="text:style-name">Interlin_Cf_<xsl:value-of select="@lang"/></xsl:attribute>
			<xsl:apply-templates/>
	  <xsl:variable name="homographNumber" select="following-sibling::item[1][@type='hn']"/>
			<xsl:if test="$homographNumber">
				<!-- todo: make a subscript with a rPr element-->
						<xsl:apply-templates select="$homographNumber" mode="hn"/>
		   </xsl:if>
	  <xsl:variable name="variantTypes" select="following-sibling::item[(count($homographNumber)+1)][@type='variantTypes']"/>
	  <xsl:if test="$variantTypes">
		<xsl:apply-templates select="$variantTypes" mode="variantTypes"/>
	  </xsl:if>
		</text:p>
  </xsl:template>

  <xsl:template match="morph/item[@type='gls']" mode="rowItems">
		<text:p>
			<xsl:attribute name="text:style-name">Interlin_Morpheme_Gloss_<xsl:value-of select="@lang"/></xsl:attribute>
			<xsl:apply-templates/>
	  <xsl:variable name="glsAppend" select="following-sibling::item[1][@type='glsAppend']"/>
	  <xsl:if test="$glsAppend">
		<xsl:apply-templates select="$glsAppend" mode="glsAppend"/>
	  </xsl:if>
		</text:p>
  </xsl:template>

  <!-- MISCELLANEOUS -->

</xsl:stylesheet>
