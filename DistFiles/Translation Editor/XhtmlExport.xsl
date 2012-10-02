<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="xml" version="1.0" encoding="utf-8" indent="yes"/>

  <!--*************************************************************************************-->
  <!--* This stylesheet works on the output produced by TE specifically for XHTML export. *-->
  <!--*************************************************************************************-->

  <!-- strip out the writing system information, since only the stylesheet needs it. -->

  <xsl:template match="WritingSystemInfo"/>

  <!--*************************************************************************************-->
  <!-- Strip all white space and leave it up to the stylesheet text elements below to put
	   in appropriate spacing. -->
  <!--*************************************************************************************-->

  <xsl:strip-space elements="*"/>

  <!--***********************************************************************************-->
  <!-- insert a comment explaining why there's so little whitespace in the xhtml output. -->
  <!--***********************************************************************************-->

  <xsl:template match="/html">
	<xsl:copy>
	  <xsl:copy-of select="@*"/><xsl:text>&#13;&#10;</xsl:text>
	  <xsl:comment>
There are no spaces or newlines between &lt;span&gt; elements in this file because
whitespace is significant.  We don't want extraneous spaces appearing in the
display/printout!
	  </xsl:comment><xsl:text>&#13;&#10;</xsl:text>
	  <xsl:apply-templates/>
	</xsl:copy>
  </xsl:template>

  <!--**************************************************************-->
  <!-- insert the XHTML DOCTYPE declaration before the root element -->
  <!--**************************************************************-->

  <xsl:template match="/">
	<xsl:text disable-output-escaping="yes">&#13;&#10;&lt;!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd"&gt;&#13;&#10;</xsl:text>
	<xsl:apply-templates/>
  </xsl:template>

  <!--*****************************************-->
  <!-- insert newlines after <head> and <body> -->
  <!--*****************************************-->

  <xsl:template match="head|body">
	<xsl:copy>
	  <xsl:copy-of select="@*"/><xsl:text>&#13;&#10;</xsl:text>
	  <xsl:apply-templates/>
	</xsl:copy><xsl:text>&#13;&#10;</xsl:text>
  </xsl:template>

  <!-- ***************************************** -->
  <!-- * This is the basic default processing. * -->
  <!-- ***************************************** -->

  <xsl:template match="*">
	<xsl:copy>
	  <xsl:copy-of select="@*"/>
	  <xsl:apply-templates/>
	</xsl:copy>
  </xsl:template>

  <!-- ************************************************ -->
  <!-- * Pass through only valid attributes of <div>. * -->
  <!-- ************************************************ -->

  <xsl:template match="div">
	<xsl:copy>
	  <xsl:if test="@class">
		<xsl:attribute name="class">
		  <xsl:call-template name="FixSpaces">
			<xsl:with-param name="text"><xsl:value-of select="@class"/></xsl:with-param>
		  </xsl:call-template>
		</xsl:attribute>
	  </xsl:if>
	  <xsl:copy-of select="@id"/>
	  <xsl:copy-of select="@style"/>
	  <xsl:copy-of select="@title"/>
	  <xsl:copy-of select="@lang"/>
	  <xsl:copy-of select="@xml:lang"/>
	  <xsl:copy-of select="@dir"/>
	  <xsl:copy-of select="@onclick"/>
	  <xsl:copy-of select="@ondblclick"/>
	  <xsl:copy-of select="@onmousedown"/>
	  <xsl:copy-of select="@onmouseup"/>
	  <xsl:copy-of select="@onmouseover"/>
	  <xsl:copy-of select="@onmousemove"/>
	  <xsl:copy-of select="@onmouseout"/>
	  <xsl:copy-of select="@onkeypress"/>
	  <xsl:copy-of select="@onkeydown"/>
	  <xsl:copy-of select="@onkeyup"/>
	  <xsl:if test="@marker"><span class="scrFootnoteMarker"><xsl:copy-of select="@lang"/><xsl:value-of select="@marker"/></span></xsl:if>
	  <xsl:apply-templates/>
	</xsl:copy>
  </xsl:template>


  <!-- ***************************************** -->
  <!-- * Throw away the <Str> level of markup. * -->
  <!-- ***************************************** -->

  <xsl:template match="Str">
	<xsl:apply-templates/>
	<xsl:text>&#13;&#10;</xsl:text>
  </xsl:template>

  <!--***********************************************************************-->
  <!-- Turn each <Run> into a <span>, ensuring that no whitespace is output. -->
  <!-- This handles footnote references along the way.                       -->
  <!--***********************************************************************-->

  <xsl:template match="Run"><xsl:if test="not(@moveableObj)"><xsl:if test="@ownlink"><xsl:call-template name="InsertFootnoteMarker"><xsl:with-param name="ref">F<xsl:value-of select="@ownlink"/></xsl:with-param></xsl:call-template></xsl:if><xsl:if test="not(@ownlink)"><span><xsl:if test="@namedStyle"><xsl:attribute name="class"><xsl:call-template name="FixSpaces"><xsl:with-param name="text"><xsl:value-of select="@namedStyle"/></xsl:with-param></xsl:call-template></xsl:attribute></xsl:if><xsl:if test="@ws"><xsl:attribute name="lang"><xsl:value-of select="@ws"/></xsl:attribute></xsl:if><xsl:value-of select="."/></span></xsl:if></xsl:if></xsl:template>

  <!--*****************************************************************************-->
  <!-- Insert a footnote marker in the text with a hyperlink to the footnote text. -->
  <!--*****************************************************************************-->

  <xsl:template name="InsertFootnoteMarker"><xsl:param name="ref"/><span class="scrFootnoteMarker"><a><xsl:attribute name="href">#<xsl:value-of select="$ref"/></xsl:attribute><xsl:value-of select="//div[@id=$ref]/@marker"/></a></span></xsl:template>

  <!--************************************-->
  <!-- convert all space characters to _. -->
  <!--************************************-->

  <xsl:template name="FixSpaces">
	<xsl:param name="text"/>
	<xsl:choose>
	  <xsl:when test="contains($text, ' ')">
		<xsl:value-of select="substring-before($text, ' ')"/>
		<xsl:value-of select="'_'"/>
		<xsl:call-template name="FixSpaces">
		  <xsl:with-param name="text" select="substring-after($text, ' ')"/>
		</xsl:call-template>
	  </xsl:when>
	  <xsl:otherwise>
		<xsl:value-of select="$text"/>
	  </xsl:otherwise>
	</xsl:choose>
  </xsl:template>

</xsl:stylesheet>
