<?xml version="1.0" encoding="UTF-8"?>
<?xml-stylesheet type="text/xsl" href="Chart.xsl"?>

<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0" xmlns:xlink="http://www.w3.org/1999/xlink"
xmlns:svg="http://www.w3.org/2000/svg" xmlns:ms="urn:schemas-microsoft-com:xslt" xmlns:me="MathExtension">
<xsl:output method="html" encoding="UTF-8" indent="yes"/>

<xsl:variable name="data">
  <action desc="Thai db Lex Edit, Sort Lexeme Form">
	<time ellapsed="7641" expected="10000" date="12/12/2007" ver="D" />
	<time ellapsed="8016" expected="10000" date="12/11/2007" ver="D" />
	<time ellapsed="6765" expected="10000" date="12/10/2007" ver="5.0" />
  </action>
</xsl:variable>

<ms:script language="JScript" implements-prefix="me">
   function unEE(num)
   {
	  if (num.indexOf("E") == -1) return num;
	  var parts = num.split("E");
	  return (parts[0]*Math.pow(10,parts[1]));
   }
   function abs(num) {return Math.abs(num);}
   function int(num) {return Math.ceil(num);}
   function sin(num) {return Math.sin(num);}
   function cos(num) {return Math.cos(num);}
</ms:script>

<xsl:template match="/">
<html>
<head><title><xsl:value-of select="data/action/@desc"/></title>
	<object id="AdobeSVG" classid="clsid:78156a80-c6a1-4bbf-8e6a-3cd390eeb4e2"></object>
	<xsl:processing-instruction name="import">
		<xsl:text>namespace="svg" implementation="#AdobeSVG"</xsl:text>
	</xsl:processing-instruction>
</head>
<body>
  <h1><xsl:value-of select="data/action/@desc"/></h1>
  <xsl:variable name="maxX">
	<xsl:call-template name="findMaxX">
  </xsl:variable>
  <xsl:variable name="maxX">
	<xsl:call-template name="findMinX">
  </xsl:variable>



  <!--xsl:variable name="maxH" select="$Hscale*me:int(string($track/track/ramp/@height))"/-->
  <xsl:variable name="maxX" select="me:int(string($track/track/@base))"/>
  <xsl:variable name="limY" select="$maxH+20"/>
  <xsl:variable name="limX" select="$maxX+50"/>
  <xsl:variable name="offX" select="$limX - $maxX"/>
  <xsl:variable name="feet" select="me:int($maxX div 12)"/>
  <xsl:variable name="foot" select="12"/>

<svg:svg width="6in" height="{6*($limY div $limX)}in" viewBox="'0 0 {$limX} {$limY}" contentScriptType = "text/ecmascript" xmlns="http://www.w3.org/2000/svg:svg">
 <svg:title>Car Dynamics Graph</svg:title>
 <svg:defs>

  <svg:g id="Frame">
   <svg:rect x="{$offX}" y="1" height="{$limY - 2}" width="{$maxX}" style="fill:none; stroke:blue; stroke-width:2;" />
   <svg:rect x="0" y="1" height="{$maxH}" width="{$limX} " style="fill:none; stroke:blue; stroke-width:2;" />
  </svg:g>

  <svg:g id="xAxis">
   <svg:path style="stroke:black; stroke-width:1; fill:none; stroke-dasharray:3 3;" d="M {$offX+$foot} 1 v {$maxH} ">
	 <xsl:attribute name="d">
	   <xsl:call-template name="vertDashes">
		 <xsl:with-param name="length" select="$feet"/>
		 <xsl:with-param name="offX" select="$offX"/>
		 <xsl:with-param name="maxH" select="$maxH"/>
	   </xsl:call-template>
	 </xsl:attribute>
   </svg:path>
   <xsl:call-template name="XLabels">
	 <xsl:with-param name="length" select="$feet"/>
	 <xsl:with-param name="offX" select="$offX"/>
	 <xsl:with-param name="maxH" select="$maxH"/>
   </xsl:call-template>
   <svg:text style="text-anchor:middle; font-size:8pt; stroke:blue;" x="{$offX+140}" y="{$maxH +17}">Base Distance (feet)</svg:text>
  </svg:g>

  <svg:g id="yAxis" style="stroke:purple;">
   <svg:path style="stroke:black; stroke-width:1; fill:none; stroke-dasharray:3 3;" d="">
	 <xsl:attribute name="d">
	   <xsl:call-template name="horizDashes">
		 <xsl:with-param name="height" select="$maxH"/>
		 <xsl:with-param name="offX" select="$offX"/>
		 <xsl:with-param name="maxX" select="$maxX"/>
	   </xsl:call-template>
	 </xsl:attribute>
   </svg:path>
   <xsl:call-template name="YLabels">
	 <xsl:with-param name="height" select="$maxH"/>
	 <xsl:with-param name="offX" select="$offX"/>
   </xsl:call-template>
   <svg:text style="text-anchor:middle; font-size:8pt; stroke:purple;" transform="translate(25,{$maxH - 15}) rotate(270)">Hight (feet)</svg:text>
  </svg:g>

  <svg:path id="h" style="fill:none; stroke:red; stroke-width:1.0;"
	transform="matrix(1 0 0 -1 {$offX} {$maxH})"
	d="M 2,0 3,0.1 4,0.3 5,0.2">
  <xsl:attribute name="d">
	 <xsl:text>M</xsl:text>
	 <xsl:apply-templates select="$track/track/vertex/@h"/>
  </xsl:attribute>
  </svg:path>

  <svg:path id="cm" style="fill:none; stroke:black; stroke-width:0.5;"
	transform="matrix(1 0 0 -1 {$offX} {$maxH})"
	d="M 2,0 3,0.1 4,0.3 5,0.2">
  <xsl:attribute name="d">
	 <xsl:text>M</xsl:text>
	 <xsl:apply-templates select="vertex/@h"/>
  </xsl:attribute>
  </svg:path>

 </svg:defs>

 <svg:use xlink:href="#Frame"/>
 <svg:use xlink:href="#xAxis"/>
 <svg:use xlink:href="#yAxis"/>
 <svg:use xlink:href="#h"/>
 <!--svg:use xlink:href="#theta"/-->
 <svg:use xlink:href="#cm"/>
</svg:svg>

   <p>the <span style="color:purple;">stuff</span> with</p>

</body>
</html>
</xsl:template>

<xsl:template name="vertDashes">
<xsl:param name="length" select="32"/>
<xsl:param name="step" select="1"/>
<xsl:param name="offX" select="1"/>
<xsl:param name="maxH" select="4"/>
  <xsl:text>M </xsl:text>
  <xsl:value-of select="$offX+$step*12"/>
  <xsl:text> 1 v </xsl:text>
  <xsl:value-of select="$maxH"/>
  <xsl:text> </xsl:text>
   <xsl:if test="$step &lt; $length - 1">
	 <xsl:call-template name="vertDashes">
		<xsl:with-param name="length" select="$length"/>
		<xsl:with-param name="step" select="$step+1"/>
		<xsl:with-param name="offX" select="$offX"/>
		<xsl:with-param name="maxH" select="$maxH"/>
	 </xsl:call-template>
   </xsl:if>
</xsl:template>

<xsl:template name="XLabels">
<xsl:param name="length" select="32"/>
<xsl:param name="step" select="1"/>
<xsl:param name="offX" select="1"/>
<xsl:param name="maxH" select="4"/>
   <svg:text style="text-anchor:middle; font-size:8pt; " x="{$offX - 15 + $step*12}" y="{$maxH+10}"><xsl:value-of select="$step - 1"/></svg:text>
   <xsl:if test="$step &lt; $length">
	 <xsl:call-template name="XLabels">
	   <xsl:with-param name="length" select="$length"/>
	   <xsl:with-param name="step" select="$step+1"/>
	   <xsl:with-param name="offX" select="$offX"/>
	   <xsl:with-param name="maxH" select="$maxH"/>
	 </xsl:call-template>
   </xsl:if>
</xsl:template>

<xsl:template name="horizDashes">
<xsl:param name="height" select="4"/>
<xsl:param name="step" select="0"/>
<xsl:param name="offX" select="1"/>
<xsl:param name="maxX" select="32"/>
  <xsl:text>M </xsl:text>
  <xsl:value-of select="$offX"/>
  <xsl:text> </xsl:text>
  <xsl:value-of select="$height - $step"/>
  <xsl:text> h </xsl:text>
  <xsl:value-of select="$maxX"/>
  <xsl:text> </xsl:text>
   <xsl:if test="$step &lt; $height">
	   <xsl:call-template name="horizDashes">
		<xsl:with-param name="height" select="$height"/>
		<xsl:with-param name="step" select="$step + $Hscale*12"/>
		<xsl:with-param name="offX" select="$offX"/>
		<xsl:with-param name="maxX" select="$maxX"/>
	   </xsl:call-template>
   </xsl:if>
</xsl:template>

<xsl:template name="YLabels">
<xsl:param name="height" select="48"/>
<xsl:param name="step" select="0"/>
<xsl:param name="offX" select="1"/>
   <svg:text style="text-anchor:middle; font-size:8pt; " x="{$offX - 10}" y="{$height - $step + 4}"><xsl:value-of select="$step div ($Hscale*12)"/></svg:text>
   <xsl:if test="$step &lt; $height">
	 <xsl:call-template name="YLabels">
		<xsl:with-param name="height" select="$height"/>
		<xsl:with-param name="step" select="$step + $Hscale*12"/>
		<xsl:with-param name="offX" select="$offX"/>
	 </xsl:call-template>
   </xsl:if>
</xsl:template>

<xsl:template match="@*" >
  <xsl:value-of select="concat(' ',../@x,',',2*.)"/>
</xsl:template>

<xsl:template match="*" />

</xsl:stylesheet>
