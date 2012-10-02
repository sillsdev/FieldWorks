<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
				version="1.0">
  <xsl:output method="xml" encoding="UTF-8" indent="yes" />
  <!-- This stylesheet transforms an interlinear text as output by FieldWorks into a form which can be verfied by an XSD). -->

  <!-- All we really need to do is reorder the children of the 'word' element so we don't have items both before
  and after morphemes, since XSD can't handle that.-->
	<xsl:template match="document">
		<document xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:noNamespaceSchemaLocation="file:FlexInterlinear.xsd">
			<xsl:apply-templates/>
		</document>
	</xsl:template>

  <xsl:template match="phrase">
	  <word>
		  <xsl:apply-templates select="words"/>
		  <xsl:apply-templates select="item"/>
	  </word>
  </xsl:template>


  <xsl:template match="word">
	  <word>
		  <xsl:apply-templates select="item"/>
		  <xsl:apply-templates select="morphemes"/>
	  </word>
  </xsl:template>



	<xsl:template match="node()|@*">

		<xsl:copy>

			<xsl:apply-templates select="node()|@*" />

		</xsl:copy>

	</xsl:template>

	</xsl:stylesheet>
