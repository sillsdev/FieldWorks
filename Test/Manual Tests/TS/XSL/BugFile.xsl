<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
<xsl:output method="html" encoding="UTF-8"/>

<xsl:template match="/modularbook">
 <xsl:element name="html">
  <xsl:element name="head">
   <title>Bug Pattern List</title>
  </xsl:element>
  <xsl:element name="body">
   <H1>Bug Pattern List</H1>
   <xsl:apply-templates select="document(include-test)"/>
  </xsl:element>
 </xsl:element>
</xsl:template>

<xsl:template match="test">
 <xsl:apply-templates select="case[step/p[@name='bug']]"/>
</xsl:template>

<xsl:template match="case">
 <h2><xsl:value-of select="./parent::test/title"/><xsl:text>: </xsl:text>
<xsl:value-of select="./title"/></h2>
 <xsl:apply-templates select="./step/p[@name='bug']"/>
</xsl:template>

<xsl:template match="p">
 <strong>
  <a>
   <xsl:attribute name="name">
	<xsl:value-of select="./ancestor::case/@id"/>
	<xsl:value-of select="./parent::step/@name"/>
   </xsl:attribute>
	<xsl:value-of select="./ancestor::case/@id"/>
	<xsl:value-of select="./parent::step/@name"/>
  </a>
 </strong>
 <xsl:text> </xsl:text><em>Title: </em><xsl:value-of select="."/>
  <blockquote>
<xsl:text>
   </xsl:text>
  <p><em>Actions: </em>
   <xsl:apply-templates select="./parent::step"/></p>
<xsl:text>
   </xsl:text>
  <p><em>Failed Expectation: </em>
   <xsl:apply-templates select="./preceding-sibling::r"/>
  </p>
<xsl:text>
  </xsl:text>
  <p><em>Additional observations: </em> <u><xsl:text>                                                         </xsl:text></u></p>
 </blockquote>
<xsl:text>
  </xsl:text>
</xsl:template>

<xsl:template match="step">
 <xsl:apply-templates select="./text()|i|b|link"/>
</xsl:template>

<xsl:template match="r">
 <xsl:apply-templates />
</xsl:template>

<xsl:template match="ul | ol">
 <blockquote>
 <xsl:apply-templates />
 </blockquote>
</xsl:template>

<xsl:template match="li">
 <div>
  <xsl:text disable-output-escaping="yes">[&amp;nbsp;&amp;nbsp;] </xsl:text><xsl:apply-templates />
 </div>
</xsl:template>



</xsl:stylesheet>