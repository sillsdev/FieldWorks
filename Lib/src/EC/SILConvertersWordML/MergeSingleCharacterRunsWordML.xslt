<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
				xmlns:fo="http://www.w3.org/1999/XSL/Format"
				xmlns:w="http://schemas.microsoft.com/office/word/2003/wordml"
				xmlns:wx="http://schemas.microsoft.com/office/word/2003/auxHint"
				xmlns:msxsl="urn:schemas-microsoft-com:xslt"
				xmlns:user="urn:my-scripts">

  <msxsl:script language="C#" implements-prefix="user">
	<msxsl:assembly name="System.Windows.Forms" />
	<![CDATA[
	// input is like "F067"
	// output is the character value of that (i.e. )
	//  (or the original string if not conversion fails)
	public string CharValue(string strChar)
	{
	  try
	  {
		int nVal = Convert.ToInt32(strChar, 16);
		char ch = (char)nVal;
		return new String(ch, 1);
	  }
	  catch {}
	  return strChar;
	}
  ]]>
  </msxsl:script>

  <xsl:output method="xml" indent="yes" />
  <xsl:strip-space elements="w:p"/>

  <!--This is the identity template and will copy anything that isn't otherwise matched-->
  <xsl:template match="@*|node()">
	<xsl:copy>
	  <xsl:apply-templates select="@*|node()"/>
	</xsl:copy>
  </xsl:template>
  <xsl:template match="w:r[not(w:rPr)][w:t[string-length(text()) = 1]]">
	<xsl:choose>
	  <xsl:when test="preceding-sibling::w:r[1][w:t[string-length(text()) = 1]]">
		<!--Already extracted the information we needed, so skip these subsequent w:r's-->
	  </xsl:when>
	  <!--During the first such node, we want to then grab the w:t single char
		and concatenate them all into one (as long as they all have the same ancestor:w:p)-->
	  <xsl:otherwise>
		<xsl:element name="w:r">
		  <xsl:element name="w:t">
			<xsl:value-of select="w:t[text()]"/>
			<xsl:call-template name="followingWRWT1">
			  <xsl:with-param name="nextWR" select="following-sibling::w:r"/>
			</xsl:call-template>
		  </xsl:element>
		</xsl:element>
	  </xsl:otherwise>
	</xsl:choose>
  </xsl:template>
  <xsl:template name="followingWRWT1">
	<xsl:param name="nextWR"/>
	<xsl:choose>
	  <xsl:when test="$nextWR[1][not(w:rPr)][w:t[string-length(text()) = 1]]">
		<xsl:value-of select="$nextWR[1]/w:t[text()]"/>
		<xsl:call-template name="followingWRWT1">
		  <xsl:with-param name="nextWR" select="$nextWR[position() > 1]"/>
		</xsl:call-template>
	  </xsl:when>
	  <xsl:otherwise><!-- exit recursion --></xsl:otherwise>
	</xsl:choose>
  </xsl:template>
  <xsl:template match="w:r[w:rPr/wx:font/@wx:val = w:rPr/wx:sym/@wx:font or w:rPr/wx:font/@wx:val = w:sym/@w:font]">
	<xsl:variable name="fontname" select="w:rPr/wx:font/@wx:val"/>
	<xsl:choose>
	  <xsl:when test="preceding-sibling::w:r[1][w:rPr/wx:font/@wx:val = $fontname]">
		<!--Already extracted the information we needed, so skip these subsequent w:r's
		<xsl:when test="w:rPr[wx:font/@wx:val = $fontname][preceding-sibling::w:r[1][w:rPr/wx:font/@wx:val = $fontname]]">
		<xsl:when test="w:rPr[wx:font/@wx:val = $fontname][ancestor::w:p/preceding-sibling::w:r[1][w:rPr/wx:font/@wx:val = $fontname]]">
		-->
	  </xsl:when>
	  <!--During the first such node, we want to then grab the wx:char or w:char value out
		of all subsequent matching nodes-->
	  <xsl:when test="w:rPr[wx:font/@wx:val = $fontname]">
		<xsl:element name="w:r">
		  <xsl:apply-templates select="@*" mode="ButRemoveSym"/>
		  <xsl:apply-templates select="w:rPr" mode="ButRemoveSym" />
		  <xsl:element name="w:t">
			<xsl:choose>
			  <xsl:when test="w:rPr[wx:sym/@wx:char]">
				<xsl:value-of select="user:CharValue(w:rPr/wx:sym/@wx:char)"/>
			  </xsl:when>
			  <xsl:when test="w:sym[@w:char]">
				<xsl:value-of select="user:CharValue(w:sym/@w:char)"/>
			  </xsl:when>
			</xsl:choose>
			<xsl:call-template name="followingWR">
			  <xsl:with-param name="nextWR" select="following-sibling::w:r"/>
			  <xsl:with-param name="fontname" select="$fontname"/>
			</xsl:call-template>
		  </xsl:element>
		</xsl:element>
	  </xsl:when>
	  <xsl:otherwise><!--never happens as we already skipped in first when--></xsl:otherwise>
	</xsl:choose>
  </xsl:template>
  <xsl:template name="followingWR">
	<xsl:param name="nextWR"/>
	<xsl:param name="fontname"/>
	<xsl:choose>
	  <xsl:when test="$nextWR[1][w:rPr/wx:font/@wx:val = $fontname][w:rPr/wx:font/@wx:val = w:rPr/wx:sym/@wx:font or w:rPr/wx:font/@wx:val = w:sym/@w:font]">
		<xsl:choose>
		  <xsl:when test="$nextWR[1][w:rPr/wx:sym/@wx:char]">
			<xsl:value-of select="user:CharValue($nextWR[1]/w:rPr/wx:sym/@wx:char)"/>
		  </xsl:when>
		  <xsl:when test="$nextWR[1][w:sym/@w:char]">
			<xsl:value-of select="user:CharValue($nextWR[1]/w:sym/@w:char)"/>
		  </xsl:when>
		</xsl:choose>
		<xsl:call-template name="followingWR">
		  <xsl:with-param name="nextWR" select="$nextWR[position() > 1]"/>
		  <xsl:with-param name="fontname" select="$fontname"/>
		</xsl:call-template>
	  </xsl:when>
	  <xsl:otherwise><!-- exit recursion --></xsl:otherwise>
	</xsl:choose>
  </xsl:template>
  <xsl:template match="wx:sym" mode="ButRemoveSym">
	<!--Skip this, because that would only be for the first of the single-run elements-->
  </xsl:template>
  <xsl:template match="w:sym" mode="ButRemoveSym">
	<!--Skip this, because that would only be for the first of the single-run elements-->
  </xsl:template>
  <xsl:template match="@*|node()" mode="ButRemoveSym">
	<xsl:copy>
	  <xsl:apply-templates select="@*|node()"/>
	</xsl:copy>
  </xsl:template>
</xsl:stylesheet>
