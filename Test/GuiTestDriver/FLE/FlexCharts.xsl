<?xml version="1.0" encoding="UTF-8"?>
<?xml-stylesheet type="text/xsl" href="FlexCharts.xsl"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0" xmlns:xlink="http://www.w3.org/1999/xlink"
xmlns:svg="http://www.w3.org/2000/svg" xmlns:ms="urn:schemas-microsoft-com:xslt" xmlns:me="MathExtension">
<xsl:output method="html" encoding="UTF-8" indent="yes"/>

  <xsl:include href="MinMax.xsl"/>

  <xsl:variable name="times" select="document('Times.xml')"/>

  <xsl:variable name="timeList">
	 <xsl:for-each select="$times/performance/action[position() &lt; 5]/time/@ellapsed">
		<l><xsl:value-of select="."/></l>
	 </xsl:for-each>
  </xsl:variable>

  <xsl:variable name="minY">
	  <xsl:call-template name="minOfList">
		<xsl:with-param name="list" select="$timeList"/>
	  </xsl:call-template>
  </xsl:variable>

  <xsl:variable name="maxY">
	  <xsl:call-template name="maxOfList">
		<xsl:with-param name="list" select="$timeList"/>
	  </xsl:call-template>
  </xsl:variable>

  <xsl:variable name="dateList">
	 <xsl:for-each select="$times/performance/action/time/@date">
		<l><xsl:value-of select="me:date2Days(string(.))"/></l>
	 </xsl:for-each>
  </xsl:variable>

  <xsl:variable name="minX">
	  <xsl:call-template name="minOfList">
		<xsl:with-param name="list" select="$dateList"/>
	  </xsl:call-template>
  </xsl:variable>

  <xsl:variable name="maxX">
	  <xsl:call-template name="maxOfList">
		<xsl:with-param name="list" select="$dateList"/>
	  </xsl:call-template>
  </xsl:variable>

  <xsl:variable name="rangeY" select="$maxY - $minY"/>
  <xsl:variable name="rangeX" select="$maxX - $minX"/>
  <xsl:variable name="scaleY" select="340 div $rangeY"/>
  <xsl:variable name="scaleX" select="490 div $rangeX"/>

<ms:script language="JScript" implements-prefix="me">
   function unEE(num)
   {
	  if (num.indexOf("E") == -1) return num;
	  var parts = num.split("E");
	  return (parts[0]*Math.pow(10,parts[1]));
   }
   function abs(num)
   {
	  return Math.abs(num);
   }
   function date2Days(date)
   { // date is mm/dd/yyyy
	  return Date.parse(date)/1000.0/60.0/60.0/24.0;
   }
</ms:script>

<xsl:template match="/">
  <xsl:apply-templates select="$times/performance"/>
</xsl:template>

<xsl:template match="performance">
<html xmlns:svg="http://www.w3.org/2000/svg">
<head><title>FieldWorks FLEx Performance</title>
	<object id="AdobeSVG" classid="clsid:78156a80-c6a1-4bbf-8e6a-3cd390eeb4e2"></object>
	<xsl:processing-instruction name="import">
		<xsl:text>namespace="svg" implementation="#AdobeSVG"</xsl:text>
	</xsl:processing-instruction>

</head>
<body>
  <h1>FieldWorks FLEx Performance</h1>
   <!--p>minY = <xsl:value-of select="$minY"/></p>
   <p>maxY = <xsl:value-of select="$maxY"/></p>
   <p>minX = <xsl:value-of select="$minX"/></p>
   <p>maxX = <xsl:value-of select="$maxX"/></p>
   <p>rangeY = <xsl:value-of select="$rangeY"/></p>
   <p>rangeX = <xsl:value-of select="$rangeX"/></p>
   <p>scaleY = <xsl:value-of select="$scaleY"/></p>
   <p>scaleX = <xsl:value-of select="$scaleX"/></p-->

   <xsl:variable name="zeroTo10">
	 <l>0</l><l>1</l><l>2</l><l>3</l><l>4</l><l>5</l><l>6</l><l>7</l><l>8</l><l>9</l><l>10</l>
   </xsl:variable>

   <xsl:variable name="xTicks">
	 <xsl:for-each select="ms:node-set($zeroTo10)/l">
	   <l n="{.}" x="{100 + .*$rangeX*$scaleX div 10}" y="365" label="{round(.*$rangeX div 10)}"/>
	 </xsl:for-each>
   </xsl:variable>
   <!--ul>
	 <xsl:for-each select="ms:node-set($xTicks)/l">
	   <li><xsl:value-of select="concat('n=',@n,' (',round(@x),',',round(@y),') ',@label)"/></li>
	 </xsl:for-each>
   </ul-->

   <xsl:variable name="yTicks">
	 <xsl:for-each select="ms:node-set($zeroTo10)/l">
	   <l n="{.}" x="100" y="{($rangeY - .*$rangeY div 10)*$scaleY + 10}" label="{round(($minY + .*$rangeY div 10) div 1000)}"/>
	 </xsl:for-each>
   </xsl:variable>
   <!--ul>
	 <xsl:for-each select="ms:node-set($yTicks)/l">
	   <li><xsl:value-of select="concat('n=',@n,' (',round(@x),',',round(@y),') ',@label)"/></li>
	 </xsl:for-each>
   </ul-->

<svg:svg width="6in" height="4in" viewBox="0 0 600 400" contentScriptType = "text/ecmascript" xmlns="http://www.w3.org/2000/svg">
 <svg:title>FLEx Performance Graph</svg:title>
 <svg:defs>

  <svg:g id="Frame">
   <svg:rect x="100" y="10" height="380" width="490" style="fill:none; stroke:blue; stroke-width:4;" />
   <svg:rect x="10" y="10" height="340" width="580" style="fill:none; stroke:blue; stroke-width:4;" />
  </svg:g>

  <svg:g id="xAxis" style="stroke:black;">
   <svg:path d="dummy" style="stroke:black; stroke-width:1; fill:none; stroke-dasharray:3 3;">
	 <xsl:attribute name="d">
	   <xsl:for-each select="ms:node-set($xTicks)/l[@n &gt; 0 and @n &lt; 10]"><!-- 1 to 9 -->
		 <xsl:text>M </xsl:text><!-- M is move to absolute location -->
		 <!-- x --><xsl:value-of select="@x"/>
		 <!-- y --><xsl:text> 347 v -340  </xsl:text><!-- v is vertical line length -->
	   </xsl:for-each>
	 </xsl:attribute>
   </svg:path>
   <xsl:for-each select="ms:node-set($xTicks)/l">
	 <svg:text style="text-anchor:middle" x="{@x - 5}" y="365">
	   <xsl:value-of select="@label"/>
	 </svg:text>
   </xsl:for-each>
   <svg:text style="text-anchor:middle; font-size:10pt; stroke:blue;" x="325" y="380">Days</svg:text>
  </svg:g>

  <svg:g id="yAxis" style="stroke:black;">
   <svg:path style="stroke:black; stroke-width:1; fill:none; stroke-dasharray:3 3;">
	 <xsl:attribute name="d">
	   <xsl:for-each select="ms:node-set($yTicks)/l[@n &gt; 0 and @n &lt; 10]"><!-- 1 to 9 -->
		 <xsl:text>M 100 </xsl:text><!-- M is move to absolute location --><!-- x -->
		 <!-- y --><xsl:value-of select="@y"/>
		 <xsl:text> h 490  </xsl:text><!-- h is horizontal line length -->
	   </xsl:for-each>
	 </xsl:attribute>
   </svg:path>
   <xsl:for-each select="ms:node-set($yTicks)/l">
	 <svg:text style="text-anchor:middle" x="60" y="{@y+5}">
	   <xsl:value-of select="@label"/>
	 </svg:text>
   </xsl:for-each>
   <svg:text style="text-anchor:middle; font-size:10pt; stroke:blue;" transform="translate(35,230) rotate(270)">Ellapsed Time (s)</svg:text>
  </svg:g>

  <svg:path id="a" style="fill:none; stroke:purple; stroke-width:1.0;" d="dummy">
  <xsl:attribute name="d">
	 <xsl:text>M</xsl:text>
	 <xsl:apply-templates select="action[1]/time/@ellapsed"/>
  </xsl:attribute>
  </svg:path>

  <svg:path id="v" style="fill:none; stroke:red; stroke-width:1.0;" d="dummy">
  <xsl:attribute name="d">
	 <xsl:text>M</xsl:text>
	 <xsl:apply-templates select="action[2]/time/@ellapsed"/>
  </xsl:attribute>
  </svg:path>

  <svg:path id="t" style="fill:none; stroke:black; stroke-width:1.0;" d="dummy">
  <xsl:attribute name="d">
	 <xsl:text>M</xsl:text>
	 <xsl:apply-templates select="action[3]/time/@ellapsed"/>
  </xsl:attribute>
  </svg:path>

  <svg:path id="dt" style="fill:none; stroke:green; stroke-width:1.0;" d="dummy">
  <xsl:attribute name="d">
	 <xsl:text>M</xsl:text>
	 <xsl:apply-templates select="action[4]/time/@ellapsed"/>
  </xsl:attribute>
  </svg:path>

 </svg:defs>

 <svg:use xlink:href="#Frame" />
 <svg:use xlink:href="#xAxis"/>
 <svg:use xlink:href="#yAxis"/>
 <svg:use xlink:href="#a"/>
 <svg:use xlink:href="#v"/>
 <svg:use xlink:href="#t"/>
 <svg:use xlink:href="#dt"/>
</svg:svg>

   <p>This diagram shows ...</p>

</body>
</html>
</xsl:template>

<xsl:template match="@*" >
  <xsl:value-of select="concat(' ',(me:date2Days(string(../@date)) - $minX)*$scaleX + 100,',',($maxY - .)*$scaleY + 10)"/>
</xsl:template>

<xsl:template match="*" />

</xsl:stylesheet>
