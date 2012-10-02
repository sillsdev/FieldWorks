<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
				xmlns:fo="http://www.w3.org/1999/XSL/Format"
				xmlns:w="http://schemas.microsoft.com/office/word/2003/wordml"
				xmlns:wx="http://schemas.microsoft.com/office/word/2003/auxHint"
				xmlns:wsp="http://schemas.microsoft.com/office/word/2003/wordml/sp2"
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
  <xsl:template match="w:r[not(w:rPr)][not(w:tab)][w:t[string-length(text()) = 1]]">
	<xsl:choose>
	  <xsl:when test="preceding-sibling::w:r[1][not(w:rPr)][not(w:tab)][w:t[string-length(text()) = 1]]">
		<!--Already extracted the information we needed, so skip these subsequent w:r's-->
	  </xsl:when>
	  <!--During the first such node, we want to then grab the w:t single char
		and concatenate them all into one (as long as they all have the same ancestor:w:p)-->
	  <xsl:otherwise>
		<xsl:variable name="accumulatedWT">
		  <xsl:value-of select="w:t[text()]"/>
		  <xsl:call-template name="followingWRWT1">
			<xsl:with-param name="nextWR" select="following-sibling::w:r"/>
		  </xsl:call-template>
		</xsl:variable>
		<xsl:choose>
		  <!--if the accumulated text is only a single char, then just replicate the original element-->
		  <xsl:when test="string-length($accumulatedWT) = 1">
			<xsl:copy>
			  <xsl:apply-templates select="@*|node()"/>
			</xsl:copy>
		  </xsl:when>
		  <xsl:otherwise>
			<!--otherwise, create our own w:r/w:t to put the accumulated string-->
			<xsl:element name="w:r">
			  <xsl:element name="w:t">
				<xsl:value-of select="$accumulatedWT"/>
			  </xsl:element>
			</xsl:element>
		  </xsl:otherwise>
		</xsl:choose>
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
	<xsl:variable name="color">
	  <xsl:choose>
		<xsl:when test="w:rPr/w:color">
		  <xsl:value-of select="w:rPr/w:color/@w:val"/>
		</xsl:when>
		<xsl:otherwise>
		  <xsl:text>none</xsl:text>
		</xsl:otherwise>
	  </xsl:choose>
	</xsl:variable>
	<xsl:variable name="size">
	  <xsl:choose>
		<xsl:when test="w:rPr/w:sz">
		  <xsl:value-of select="w:rPr/w:sz/@w:val"/>
		</xsl:when>
		<xsl:otherwise>
		  <xsl:text>none</xsl:text>
		</xsl:otherwise>
	  </xsl:choose>
	</xsl:variable>
	<xsl:variable name="vertAlign">
	  <xsl:choose>
		<xsl:when test="w:rPr/w:vertAlign">
		  <xsl:value-of select="w:rPr/w:vertAlign/@w:val"/>
		</xsl:when>
		<xsl:otherwise>
		  <xsl:text>none</xsl:text>
		</xsl:otherwise>
	  </xsl:choose>
	</xsl:variable>
	<xsl:choose>
	  <!--when the font name is the same and either:
	  1) there was no color (or size) element before and there still isn't one or
	  2) there was a color (or size) element before and its value is the same,
	  then do nothing (i.e. prevent the otherwise case)-->
	  <xsl:when test="preceding-sibling::w:r[1][w:rPr/wx:font/@wx:val = $fontname]
					  and ((preceding-sibling::w:r[1][not(w:rPr/w:color)] and w:rPr[not(w:color)])
						or preceding-sibling::w:r[1][w:rPr/w:color/@w:val = $color])
					  and ((preceding-sibling::w:r[1][not(w:rPr/w:sz)] and w:rPr[not(w:sz)])
						or preceding-sibling::w:r[1][w:rPr/w:sz/@w:val = $size])
					  and ((preceding-sibling::w:r[1][not(w:rPr/w:vertAlign)] and w:rPr[not(w:vertAlign)])
						or preceding-sibling::w:r[1][w:rPr/w:vertAlign/@w:val = $vertAlign])">
		<!--If we get here, it means we've already extracted the information we needed through the recursion.
		So skip these subsequent w:r's that have already been processed.
		There are three reasons why the recursion would have stopped:
		1) the font name isn't the same
		2) there wasn't a w:color element, but now there is
		3) there was a w:color element, but its value is different
		4) there was a w:color element, and now there isn't
		-->
	  </xsl:when>
	  <!--During the first such node, we want to grab the wx:char or w:char value out
		of all subsequent matching nodes. Do this via recursion-->
	  <xsl:otherwise>
		<!--Accumulate the w:t values from this element and subsequent 'similar' siblings-->
		<xsl:variable name="accumulatedWT">
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
			<xsl:with-param name="color" select="$color"/>
			<xsl:with-param name="size" select="$size"/>
			<xsl:with-param name="vertAlign" select="$vertAlign"/>
		  </xsl:call-template>
		</xsl:variable>
		<xsl:choose>
		  <!--if the accumulated text is only a single char, then just replicate the original element-->
		  <xsl:when test="string-length($accumulatedWT) = 1">
			<xsl:copy>
			  <xsl:apply-templates select="@*|node()"/>
			</xsl:copy>
		  </xsl:when>
		  <xsl:otherwise>
			<!--otherwise, create our own w:r/w:t to put the accumulated string-->
			<xsl:element name="w:r">
			  <xsl:apply-templates select="@*" mode="ButRemoveSym"/>
			  <xsl:apply-templates select="w:rPr" mode="ButRemoveSym" />
			  <xsl:element name="w:t">
				<xsl:value-of select="$accumulatedWT"/>
			  </xsl:element>
			</xsl:element>
		  </xsl:otherwise>
		</xsl:choose>
	  </xsl:otherwise>
	</xsl:choose>
  </xsl:template>
  <xsl:template name="followingWR">
	<xsl:param name="nextWR"/>
	<xsl:param name="fontname"/>
	<xsl:param name="color"/>
	<xsl:param name="size"/>
	<xsl:param name="vertAlign"/>
	<xsl:choose>
	  <xsl:when test="$nextWR[1]
				[not(w:rPr/w:color) and ($color = 'none')]
				[not(w:rPr/w:sz) and ($size = 'none')]
				[not(w:rPr/w:vertAlign) and ($vertAlign = 'none')]
				[w:rPr/wx:font/@wx:val = $fontname]
				[(w:rPr/wx:font/@wx:val = w:rPr/wx:sym/@wx:font) or (w:rPr/wx:font/@wx:val = w:sym/@w:font)]">
		<xsl:call-template name="ProcessInsertedSymbol">
		  <xsl:with-param name="nextWR" select="$nextWR"/>
		  <xsl:with-param name="fontname" select="$fontname"/>
		  <xsl:with-param name="color">
			<xsl:text>none</xsl:text>
		  </xsl:with-param>
		  <xsl:with-param name="size">
			<xsl:text>none</xsl:text>
		  </xsl:with-param>
		  <xsl:with-param name="vertAlign">
			<xsl:text>none</xsl:text>
		  </xsl:with-param>
		</xsl:call-template>
	  </xsl:when>
	  <xsl:when test="$nextWR[1]
				[not(w:rPr/w:color) and ($color = 'none')]
				[w:rPr/w:sz/@w:val = $size]
				[not(w:rPr/w:vertAlign) and ($vertAlign = 'none')]
				[w:rPr/wx:font/@wx:val = $fontname]
				[(w:rPr/wx:font/@wx:val = w:rPr/wx:sym/@wx:font) or (w:rPr/wx:font/@wx:val = w:sym/@w:font)]">
		<xsl:call-template name="ProcessInsertedSymbol">
		  <xsl:with-param name="nextWR" select="$nextWR"/>
		  <xsl:with-param name="fontname" select="$fontname"/>
		  <xsl:with-param name="color">
			<xsl:text>none</xsl:text>
		  </xsl:with-param>
		  <xsl:with-param name="size" select="w:rPr/w:sz/@w:val"/>
		  <xsl:with-param name="vertAlign">
			<xsl:text>none</xsl:text>
		  </xsl:with-param>
		</xsl:call-template>
	  </xsl:when>
	  <xsl:when test="$nextWR[1]
				[w:rPr/w:color/@w:val = $color]
				[not(w:rPr/w:sz) and ($size = 'none')]
				[not(w:rPr/w:vertAlign) and ($vertAlign = 'none')]
				[w:rPr/wx:font/@wx:val = $fontname]
				[(w:rPr/wx:font/@wx:val = w:rPr/wx:sym/@wx:font) or (w:rPr/wx:font/@wx:val = w:sym/@w:font)]">
		<xsl:call-template name="ProcessInsertedSymbol">
		  <xsl:with-param name="nextWR" select="$nextWR"/>
		  <xsl:with-param name="fontname" select="$fontname"/>
		  <xsl:with-param name="color" select="w:rPr/w:color/@w:val"/>
		  <xsl:with-param name="size">
			<xsl:text>none</xsl:text>
		  </xsl:with-param>
		  <xsl:with-param name="vertAlign">
			<xsl:text>none</xsl:text>
		  </xsl:with-param>
		</xsl:call-template>
	  </xsl:when>
	  <xsl:when test="$nextWR[1]
				[w:rPr/w:color/@w:val = $color]
				[w:rPr/w:sz/@w:val = $size]
				[not(w:rPr/w:vertAlign) and ($vertAlign = 'none')]
				[w:rPr/wx:font/@wx:val = $fontname]
				[(w:rPr/wx:font/@wx:val = w:rPr/wx:sym/@wx:font) or (w:rPr/wx:font/@wx:val = w:sym/@w:font)]">
		<xsl:call-template name="ProcessInsertedSymbol">
		  <xsl:with-param name="nextWR" select="$nextWR"/>
		  <xsl:with-param name="fontname" select="$fontname"/>
		  <xsl:with-param name="color" select="w:rPr/w:color/@w:val"/>
		  <xsl:with-param name="size" select="w:rPr/w:sz/@w:val"/>
		  <xsl:with-param name="vertAlign">
			<xsl:text>none</xsl:text>
		  </xsl:with-param>
		</xsl:call-template>
	  </xsl:when>
	  <xsl:when test="$nextWR[1]
				[not(w:rPr/w:color) and ($color = 'none')]
				[not(w:rPr/w:sz) and ($size = 'none')]
				[w:rPr/w:vertAlign/@w:val = $vertAlign]
				[w:rPr/wx:font/@wx:val = $fontname]
				[(w:rPr/wx:font/@wx:val = w:rPr/wx:sym/@wx:font) or (w:rPr/wx:font/@wx:val = w:sym/@w:font)]">
		<xsl:call-template name="ProcessInsertedSymbol">
		  <xsl:with-param name="nextWR" select="$nextWR"/>
		  <xsl:with-param name="fontname" select="$fontname"/>
		  <xsl:with-param name="color">
			<xsl:text>none</xsl:text>
		  </xsl:with-param>
		  <xsl:with-param name="size">
			<xsl:text>none</xsl:text>
		  </xsl:with-param>
		  <xsl:with-param name="vertAlign" select="w:rPr/w:vertAlign/@w:val"/>
		</xsl:call-template>
	  </xsl:when>
	  <xsl:when test="$nextWR[1]
				[not(w:rPr/w:color) and ($color = 'none')]
				[w:rPr/w:sz/@w:val = $size]
				[w:rPr/w:vertAlign/@w:val = $vertAlign]
				[w:rPr/wx:font/@wx:val = $fontname]
				[(w:rPr/wx:font/@wx:val = w:rPr/wx:sym/@wx:font) or (w:rPr/wx:font/@wx:val = w:sym/@w:font)]">
		<xsl:call-template name="ProcessInsertedSymbol">
		  <xsl:with-param name="nextWR" select="$nextWR"/>
		  <xsl:with-param name="fontname" select="$fontname"/>
		  <xsl:with-param name="color">
			<xsl:text>none</xsl:text>
		  </xsl:with-param>
		  <xsl:with-param name="size" select="w:rPr/w:sz/@w:val"/>
		  <xsl:with-param name="vertAlign" select="w:rPr/w:vertAlign/@w:val"/>
		</xsl:call-template>
	  </xsl:when>
	  <xsl:when test="$nextWR[1]
				[w:rPr/w:color/@w:val = $color]
				[not(w:rPr/w:sz) and ($size = 'none')]
				[w:rPr/w:vertAlign/@w:val = $vertAlign]
				[w:rPr/wx:font/@wx:val = $fontname]
				[(w:rPr/wx:font/@wx:val = w:rPr/wx:sym/@wx:font) or (w:rPr/wx:font/@wx:val = w:sym/@w:font)]">
		<xsl:call-template name="ProcessInsertedSymbol">
		  <xsl:with-param name="nextWR" select="$nextWR"/>
		  <xsl:with-param name="fontname" select="$fontname"/>
		  <xsl:with-param name="color" select="w:rPr/w:color/@w:val"/>
		  <xsl:with-param name="size">
			<xsl:text>none</xsl:text>
		  </xsl:with-param>
		  <xsl:with-param name="vertAlign" select="w:rPr/w:vertAlign/@w:val"/>
		</xsl:call-template>
	  </xsl:when>
	  <xsl:when test="$nextWR[1]
				[w:rPr/w:color/@w:val = $color]
				[w:rPr/w:sz/@w:val = $size]
				[w:rPr/w:vertAlign/@w:val = $vertAlign]
				[w:rPr/wx:font/@wx:val = $fontname]
				[(w:rPr/wx:font/@wx:val = w:rPr/wx:sym/@wx:font) or (w:rPr/wx:font/@wx:val = w:sym/@w:font)]">
		<xsl:call-template name="ProcessInsertedSymbol">
		  <xsl:with-param name="nextWR" select="$nextWR"/>
		  <xsl:with-param name="fontname" select="$fontname"/>
		  <xsl:with-param name="color" select="w:rPr/w:color/@w:val"/>
		  <xsl:with-param name="size" select="w:rPr/w:sz/@w:val"/>
		  <xsl:with-param name="vertAlign" select="w:rPr/w:vertAlign/@w:val"/>
		</xsl:call-template>
	  </xsl:when>
	  <xsl:otherwise>
		<!-- exit recursion -->
	  </xsl:otherwise>
	</xsl:choose>
  </xsl:template>
  <xsl:template name="ProcessInsertedSymbol">
	<xsl:param name="nextWR"/>
	<xsl:param name="fontname"/>
	<xsl:param name="color"/>
	<xsl:param name="size"/>
	<xsl:param name="vertAlign"/>
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
	  <xsl:with-param name="color" select="$color"/>
	  <xsl:with-param name="size" select="$size"/>
	  <xsl:with-param name="vertAlign" select="$vertAlign"/>
	</xsl:call-template>
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
