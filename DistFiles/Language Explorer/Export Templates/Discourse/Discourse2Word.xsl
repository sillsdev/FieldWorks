<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns="http://www.w3.org/1999/XSL/Format" xmlns:w="http://schemas.microsoft.com/office/word/2003/wordml" xmlns:v="urn:schemas-microsoft-com:vml" version="1.0">
  <xsl:output method="xml" encoding="UTF-8" indent="yes" />
  <!-- This stylesheet transforms a discourse chart as output by FieldWorks into a form which can be directly read
  by Microsoft Word (tested with Word 2003 SP 3 build 11.8169.8172). -->
  <!-- Beware! Some editors will destroy the Unicode Line Separator characters in the note template. -->
  <!-- This is to generate a style sheet, with a distinct style for each kind of data. -->
  <xsl:template match="document">
	<xsl:processing-instruction name="mso-application"> progid="Word.Document"</xsl:processing-instruction>
	<w:wordDocument xmlns:w="http://schemas.microsoft.com/office/word/2003/wordml" xmlns:v="urn:schemas-microsoft-com:vml">
	  <w:styles>
		<!--Base for all discourse styles -->
		<w:style w:type="paragraph" w:styleId="Discourse Base">
		  <w:rPr>
			<w:noProof w:val="on" />
			<w:rFonts>
			  <xsl:attribute name="w:ascii">
				<xsl:value-of select="//languages/language[@vernacular='true']/@font" />
			  </xsl:attribute>
			  <xsl:attribute name="w:h-ansi">
				<xsl:value-of select="//languages/language[@vernacular='true']/@font" />
			  </xsl:attribute>
			  <xsl:attribute name="w:cs">
				<xsl:value-of select="//languages/language[@vernacular='true']/@font" />
			  </xsl:attribute>
			  <xsl:attribute name="w:fareast">
				<xsl:value-of select="//languages/language[@vernacular='true']/@font" />
			  </xsl:attribute>
			</w:rFonts>
		  </w:rPr>
		</w:style>
		<!--Top row headers -->
		<w:style w:type="paragraph" w:styleId="Discourse Title1">
		  <w:basedOn w:val="Discourse Base" />
		  <w:name w:val="Discourse Title1" />
		  <w:pPr>
			<w:jc w:val="center" />
		  </w:pPr>
		  <w:rPr>
			<w:b w:val="on" />
			<w:sz w:val="28" />
		  </w:rPr>
		</w:style>
		<!--Second row headers -->
		<w:style w:type="paragraph" w:styleId="Discourse Title2">
		  <w:basedOn w:val="Discourse Base" />
		  <w:name w:val="Discourse Title2" />
		  <w:pPr>
			<w:jc w:val="center" />
		  </w:pPr>
		  <w:rPr>
			<w:b w:val="on" />
			<w:sz w:val="24" />
		  </w:rPr>
		</w:style>
		<!--Words in the baseline -->
		<w:style w:type="paragraph" w:styleId="Discourse Normal">
		  <w:basedOn w:val="Discourse Base" />
		  <w:name w:val="Discourse Normal" />
		  <w:rPr>
			<w:noProof w:val="on" />
			<!--Don't spell-check vernacular -->
			<w:sz w:val="21" />
		  </w:rPr>
		</w:style>
		<!--word glosses -->
		<w:style w:type="paragraph" w:styleId="Discourse Gloss">
		  <w:basedOn w:val="Discourse Normal" />
		  <w:name w:val="Discourse Gloss" />
		  <w:rPr>
			<w:color w:val="808080" />
			<w:sz w:val="18" />
			<w:noProof w:val="off" />
			<w:rFonts>
			  <xsl:attribute name="w:ascii">
				<xsl:value-of select="//languages/language[not(@vernacular='true')]/@font" />
			  </xsl:attribute>
			  <xsl:attribute name="w:h-ansi">
				<xsl:value-of select="//languages/language[not(@vernacular='true')]/@font" />
			  </xsl:attribute>
			  <xsl:attribute name="w:cs">
				<xsl:value-of select="//languages/language[not(@vernacular='true')]/@font" />
			  </xsl:attribute>
			  <xsl:attribute name="w:fareast">
				<xsl:value-of select="//languages/language[not(@vernacular='true')]/@font" />
			  </xsl:attribute>
			</w:rFonts>
		  </w:rPr>
		</w:style>
		<!--Dependent (not speech) clauses...also a base for the other dependent clause styles -->
		<w:style w:type="paragraph" w:styleId="Discourse Dependent">
		  <w:basedOn w:val="Discourse Normal" />
		  <w:name w:val="Discourse Dependent" />
		  <w:rPr>
			<w:color w:val="0000FF" />
			<w:sz w:val="20" />
		  </w:rPr>
		</w:style>
		<!--Speech clause -->
		<w:style w:type="paragraph" w:styleId="Discourse Speech">
		  <w:basedOn w:val="Discourse Dependent" />
		  <w:name w:val="Discourse Speech" />
		  <w:rPr>
			<w:color w:val="008000" />
			<w:u w:val="single" />
		  </w:rPr>
		</w:style>
		<!--Song clause -->
		<w:style w:type="paragraph" w:styleId="Discourse Song">
		  <w:basedOn w:val="Discourse Dependent" />
		  <w:name w:val="Discourse Song" />
		  <w:rPr>
			<w:color w:val="993366" />
		  </w:rPr>
		</w:style>
		<!--"Marker" is a list ref that marks the clause properties -->
		<w:style w:type="character" w:styleId="Discourse Marker">
		  <w:name w:val="Discourse Marker" />
		  <w:rPr>
			<w:color w:val="FF9900" />
		  </w:rPr>
		</w:style>
		<!--Actual words that have been preposed/postposed -->
		<w:style w:type="character" w:styleId="Discourse Moved Text">
		  <w:name w:val="Discourse Moved Text" />
		  <w:rPr>
			<w:color w:val="FF0000" />
		  </w:rPr>
		</w:style>
		<!--Subtype for second (and subsequent) on line -->
		<w:style w:type="character" w:styleId="Discourse Moved Text 2nd">
		  <w:basedOn w:val="Discourse Moved Text" />
		  <w:name w:val="Discourse Moved Text 2nd" />
		  <w:rPr>
			<w:u w:val="single" />
		  </w:rPr>
		</w:style>
		<!--Prepose/postpose markers (<< or >>) -->
		<w:style w:type="character" w:styleId="Discourse Move Mkr">
		  <w:name w:val="Discourse Move Mkr" />
		  <w:rPr>
			<w:color w:val="FF8080" />
		  </w:rPr>
		</w:style>
		<!-- Subtype when target is not first on line.-->
		<w:style w:type="character" w:styleId="Discourse Move Mkr 2nd">
		  <w:basedOn w:val="Discourse Move Mkr" />
		  <w:name w:val="Discourse Move Mkr 2nd" />
		  <w:rPr>
			<w:u w:val="single" />
		  </w:rPr>
		</w:style>
		<!--Marker indicating where dep clause 'belongs' -->
		<w:style w:type="character" w:styleId="Discourse Dep Mkr">
		  <w:name w:val="Discourse Dep Mkr" />
		  <w:rPr>
			<w:color w:val="0000FF" />
		  </w:rPr>
		</w:style>
		<!--Marker indicating where speech clause 'belongs' -->
		<w:style w:type="character" w:styleId="Discourse Speech Mkr">
		  <w:name w:val="Discourse Speech Mkr" />
		  <w:rPr>
			<w:color w:val="008000" />
		  </w:rPr>
		</w:style>
		<!--Marker indicating where song clause 'belongs' -->
		<w:style w:type="character" w:styleId="Discourse Song Mkr">
		  <w:name w:val="Discourse Song Mkr" />
		  <w:rPr>
			<w:color w:val="993366" />
		  </w:rPr>
		</w:style>
		<!--Undo paragraph level formatting where it isn't appropriate (Row Numbers and Notes)-->
		<w:style w:type="character" w:styleId="Default Discourse Character">
		  <w:name w:val="Discourse Default Character" />
		  <w:rPr>
			<w:color w:val="000000" />
			<w:sz w:val="21" />
			<w:u w:val="none" />
		  </w:rPr>
		</w:style>
	  </w:styles>
	  <w:body>
		<xsl:apply-templates />
	  </w:body>
	</w:wordDocument>
  </xsl:template>
  <!-- CHART LEVEL -->
  <xsl:template match="chart">
	<w:tbl>
	  <w:tblPr>
		<w:tblCellMar>
		  <!-- cell margins, twips-->
		  <w:left w:w="40" w:type="dxa" />
		  <w:right w:w="20" w:type="dxa" />
		  <w:top w:w="20" w:type="dxa" />
		  <w:bottom w:w="20" w:type="dxa" />
		</w:tblCellMar>
		<!-- table default borders. Some settings are duplicated when setting props for individual cells.-->
		<w:tblBorders>
		  <w:top w:val="single" w:sz="4" w:space="0" w:color="EAEAEA" />
		  <w:left w:val="single" w:sz="4" w:space="0" w:color="EAEAEA" />
		  <w:bottom w:val="single" w:sz="4" w:space="0" w:color="EAEAEA" />
		  <w:right w:val="single" w:sz="4" w:space="0" w:color="EAEAEA" />
		  <w:insideH w:val="single" w:sz="4" w:space="0" w:color="EAEAEA" />
		  <w:insideV w:val="single" w:sz="4" w:space="0" w:color="EAEAEA" />
		</w:tblBorders>
	  </w:tblPr>
	  <xsl:apply-templates />
	</w:tbl>
  </xsl:template>
  <!-- A chart row generates TWO rows of cells, the second containing glosses, the first everything else.
	When the second row cell is empty, it is 'merged' with the cell above by setting vmerge properties on both.
	The second row is generated by applying the embedded templates again in a mode.-->
  <xsl:template match="row">
	<w:tr>
	  <xsl:apply-templates />
	</w:tr>
	<xsl:if test="cell/glosses">
	  <w:tr>
		<xsl:apply-templates mode="glosses" />
	  </w:tr>
	</xsl:if>
  </xsl:template>
  <xsl:template match="cell">
	<w:tc>
	  <w:tcPr>
		<w:gridSpan>
		  <xsl:attribute name="w:val">
			<xsl:value-of select="@cols" />
		  </xsl:attribute>
		</w:gridSpan>
		<xsl:if test="not(glosses)">
		  <w:vmerge w:val="restart" />
		</xsl:if>
		<xsl:if test="glosses">
		  <w:tcBorders>
			<!-- These are the same as for the table as a whole, but if not specified here,
						they are considered missing, and disappear anywhere there is a margin.-->
			<w:top w:val="single" w:sz="4" w:space="0" w:color="EAEAEA" />
			<w:left w:val="single" w:sz="4" w:space="0" w:color="EAEAEA" />
			<w:right w:val="single" w:sz="4" w:space="0" w:color="EAEAEA" />
			<!--contrary to at least one version of the doc, this border is overridden by
the insideH border on the table as a whole unless its color is brighter
or its width is wider. I made it a fraction wider so I could make the
color lighter.-->
			<w:bottom w:val="single" w:sz="5" w:space="0" w:color="EAEAEA" />
		  </w:tcBorders>
		</xsl:if>
	  </w:tcPr>
	  <w:tblGrid>
		<w:gridCol w:w="200" />
		<!-- minimum cell width in twips (1/20 pt)-->
	  </w:tblGrid>
	  <xsl:apply-templates />
	</w:tc>
  </xsl:template>
  <xsl:template match="cell" mode="glosses">
	<w:tc>
	  <w:tcPr>
		<w:gridSpan>
		  <xsl:attribute name="w:val">
			<xsl:value-of select="@cols" />
		  </xsl:attribute>
		</w:gridSpan>
		<xsl:if test="not(glosses)">
		  <w:vmerge />
		</xsl:if>
		<xsl:if test="ancestor::row/attribute::endPara='true' or ancestor::row/attribute::endSent='true'">
		  <w:tcBorders>
			<!-- These are the same as for the table as a whole, but if not specified here,
						they are considered missing, and disappear anywhere there is a margin.-->
			<w:top w:val="single" w:sz="4" w:space="0" w:color="EAEAEA" />
			<w:left w:val="single" w:sz="4" w:space="0" w:color="EAEAEA" />
			<w:right w:val="single" w:sz="4" w:space="0" w:color="EAEAEA" />
			<xsl:if test="ancestor::row/attribute::endPara='true'">
			  <w:bottom w:val="single" w:sz="14" w:space="0" w:color="0" />
			</xsl:if>
			<xsl:if test="ancestor::row/attribute::endSent='true'">
			  <w:bottom w:val="single" w:sz="8" w:space="0" w:color="0" />
			</xsl:if>
		  </w:tcBorders>
		</xsl:if>
	  </w:tcPr>
	  <w:tblGrid>
		<w:gridCol w:w="200" />
		<!-- twips (1/20 pt)-->
	  </w:tblGrid>
	  <xsl:if test="not(glosses)">
		<w:p />
	  </xsl:if>
	  <xsl:apply-templates mode="glosses" />
	</w:tc>
  </xsl:template>
  <xsl:template match="main" mode="glosses"></xsl:template>
  <xsl:template match="main">
	<w:p>
	  <w:pPr>
		<xsl:if test="ancestor::row/attribute::type='dependent'">
		  <w:pStyle w:val="Discourse Dependent" />
		</xsl:if>
		<xsl:if test="ancestor::row/attribute::type='speech'">
		  <w:pStyle w:val="Discourse Speech" />
		</xsl:if>
		<xsl:if test="ancestor::row/attribute::type='song'">
		  <w:pStyle w:val="Discourse Song" />
		</xsl:if>
		<xsl:if test="ancestor::row/attribute::type='normal'">
		  <w:pStyle w:val="Discourse Normal" />
		</xsl:if>
		<xsl:if test="ancestor::row/attribute::type='title1'">
		  <w:pStyle w:val="Discourse Title1" />
		</xsl:if>
		<xsl:if test="ancestor::row/attribute::type='title2'">
		  <w:pStyle w:val="Discourse Title2" />
		</xsl:if>
		<!-- Todo: for RTL we need to be smarter.-->
		<xsl:if test="parent::cell[@reversed='true']">
		  <w:jc w:val="right" />
		</xsl:if>
	  </w:pPr>
	  <xsl:apply-templates />
	</w:p>
  </xsl:template>
  <xsl:template match="clauseMkr">
	<w:r>
	  <w:rPr>
		<xsl:variable name="myTarget" select="@target" />
		<xsl:if test="//row[@id=$myTarget]/attribute::type='dependent'">
		  <w:rStyle w:val="Discourse Dep Mkr" />
		</xsl:if>
		<xsl:if test="//row[@id=$myTarget]/attribute::type='speech'">
		  <w:rStyle w:val="Discourse Speech Mkr" />
		</xsl:if>
		<xsl:if test="//row[@id=$myTarget]/attribute::type='song'">
		  <w:rStyle w:val="Discourse Song Mkr" />
		</xsl:if>
	  </w:rPr>
	  <w:t>
		<xsl:apply-templates />
	  </w:t>
	</w:r>
  </xsl:template>
  <xsl:template match="moveMkr">
	<w:r>
	  <w:rPr>
		<xsl:choose>
		  <xsl:when test="@targetFirstOnLine='false'">
			<w:rStyle w:val="Discourse Move Mkr 2nd" />
		  </xsl:when>
		  <xsl:otherwise>
			<w:rStyle w:val="Discourse Move Mkr" />
		  </xsl:otherwise>
		</xsl:choose>
	  </w:rPr>
	  <w:t>
		<xsl:apply-templates />
	  </w:t>
	</w:r>
  </xsl:template>
  <xsl:template match="listRef">
	<w:r>
	  <w:rPr>
		<w:rStyle w:val="Discourse Marker" />
	  </w:rPr>
	  <w:t>
		<xsl:apply-templates />
	  </w:t>
	</w:r>
  </xsl:template>
  <xsl:template match="glosses"></xsl:template>
  <xsl:template match="glosses" mode="glosses">
	<w:p>
	  <w:pPr>
		<w:pStyle w:val="Discourse Gloss" />
		<!-- Todo: for RTL we need to be smarter.-->
		<xsl:if test="parent::cell[@reversed='true']">
		  <w:jc w:val="right" />
		</xsl:if>
	  </w:pPr>
	  <xsl:apply-templates />
	</w:p>
  </xsl:template>
  <xsl:template match="gloss">
	<w:r>
	  <w:t>
		<xsl:apply-templates />
		<xsl:text> </xsl:text>
	  </w:t>
	</w:r>
  </xsl:template>
  <xsl:template match="word">
	<w:r>
	  <xsl:if test="@moved='true'">
		<w:rPr>
		  <xsl:choose>
			<xsl:when test="../../preceding-sibling::cell/main/word/@moved='true'">
			  <w:rStyle w:val="Discourse Moved Text 2nd" />
			</xsl:when>
			<xsl:otherwise>
			  <w:rStyle w:val="Discourse Moved Text" />
			</xsl:otherwise>
		  </xsl:choose>
		</w:rPr>
	  </xsl:if>
	  <w:t>
		<xsl:apply-templates />
		<xsl:if test="not(following-sibling::*[1][self::lit/@noSpaceBefore='true'])">
		  <xsl:text> </xsl:text>
		</xsl:if>
	  </w:t>
	</w:r>
  </xsl:template>
  <xsl:template match="lit">
	<w:r>
	  <w:rPr>
		<xsl:choose>
		  <xsl:when test="preceding-sibling::*[1][self::listRef] or following-sibling::*[1][self::listRef]">
			<w:rStyle w:val="Discourse Marker" />
		  </xsl:when>
		  <xsl:when test="preceding-sibling::*[1][self::clauseMkr] or following-sibling::*[1][self::clauseMkr]">
			<xsl:variable name="myTarget" select="ancestor::main/clauseMkr[@target]"/>
			<xsl:if test="//row[@id=$myTarget]/attribute::type='dependent'">
			  <w:rStyle w:val="Discourse Dep Mkr"/>
			</xsl:if>
			<xsl:if test="//row[@id=$myTarget]/attribute::type='speech'">
			  <w:rStyle w:val="Discourse Speech Mkr"/>
			</xsl:if>
			<xsl:if test="//row[@id=$myTarget]/attribute::type='song'">
			  <w:rStyle w:val="Discourse Song Mkr"/>
			</xsl:if>
		  </xsl:when>
		</xsl:choose>
	  </w:rPr>
	  <w:t>
		<xsl:apply-templates />
		<xsl:if test="not(@noSpaceAfter='true')">
		  <xsl:text> </xsl:text>
		</xsl:if>
	  </w:t>
	</w:r>
  </xsl:template>
  <xsl:template match="rownum">
	<w:r>
	  <w:rPr>
		<w:rStyle w:val="Default Discourse Character" />
	  </w:rPr>
	  <w:t>
		<xsl:apply-templates />
	  </w:t>
	</w:r>
  </xsl:template>
  <xsl:template match="note">
	<xsl:variable name="text">
	  <xsl:value-of select="." />
	</xsl:variable>
	<w:r>
	  <w:rPr>
		<w:rStyle w:val="Default Discourse Character" />
	  </w:rPr>
	  <w:t>
		<xsl:choose>
		  <xsl:when test='contains($text, "&#x2028;")'>
			<xsl:value-of select='substring-before($text, "&#x2028;")' />
			<xsl:element name="w:br" />
			<xsl:value-of select='substring-after($text, "&#x2028;")' />
		  </xsl:when>
		  <xsl:otherwise>
			<xsl:value-of select="$text"/>
		  </xsl:otherwise>
		</xsl:choose>
	  </w:t>
	</w:r>
  </xsl:template>
</xsl:stylesheet>