<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:xhtml="http://www.w3.org/1999/xhtml" exclude-result-prefixes="xhtml">
  <xsl:output method="html" encoding="UTF-8"/>
  <xsl:template match="/">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="xhtml:P">
	<xsl:choose>
	  <xsl:when test="xhtml:a[1][not(@name=preceding::xhtml:P/xhtml:a/@name)]">
		<P>
		  <xsl:apply-templates select="@* | node()"/>
		</P>
	  </xsl:when>
	  <xsl:otherwise>
		<P>
		  <xsl:apply-templates select="@* | node()[node()[name()!='xhtml:a'] | //xhtml:p/xhtml:a[position()!=1]]"/>
		</P>
	  </xsl:otherwise>
	</xsl:choose>
  </xsl:template>
  <xsl:template match="xhtml:a[@name]">
	<xsl:variable name="Name" select="@name"/>
	<xsl:variable name="UpperCaseEtAl">
	  <xsl:text>ABCDEFGHIJKLMNOPQRSTUVWXYZ</xsl:text>
	</xsl:variable>
	<xsl:variable name="LowerCaseEtAl">
	  <xsl:text>abcdefghijklmnopqrstuvwxyz</xsl:text>
	</xsl:variable>
	<xsl:variable name="NewName" select="translate($Name,$UpperCaseEtAl,$LowerCaseEtAl)"/>
	<a>
	  <xsl:attribute name="name">
		<xsl:value-of select="$NewName"/>
	  </xsl:attribute>
	  </a>
  </xsl:template>
  <xsl:template match="*">
	<xsl:element name="{local-name()}">
	  <xsl:apply-templates select="@*|node()"/>
	</xsl:element>
  </xsl:template>
  <xsl:template match="@*">
	<xsl:attribute name="{local-name()}"><xsl:value-of select="."/></xsl:attribute>
  </xsl:template>
</xsl:stylesheet>
