<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0" xmlns:xlink="http://www.w3.org/1999/xlink"
xmlns:svg="http://www.w3.org/2000/svg" xmlns:ms="urn:schemas-microsoft-com:xslt" xmlns:me="MathExtension">
<xsl:output method="html" encoding="UTF-8" indent="yes"/>

<!-- Includers must supply $dataBase the name each action/@desc starts with that will be included in the chart -->

  <xsl:include href="MinMax.xsl"/>

<ms:script language="JScript" implements-prefix="me">
  function absolute(num) {return Math.abs(num);}
</ms:script>

  <xsl:variable name="times" select="document('Times.xml')"/>

  <xsl:variable name="actions" select="$times/performance/action[starts-with(@desc,$dataBase)]"/>

  <!--xsl:variable name="actions" select="$times/performance/action[position() &gt; ($actionFirst - 1) and position() &lt; ($actionLast + 1)]"/-->

   <xsl:variable name="colors">
	 <l>black</l><l>blue</l><l>purple</l><l>red</l><l>orange</l>
	 <l>gold</l><l>lightgreen</l><l>green</l><l>brown</l>
	 <l>coral</l><l>lightblue</l>
   </xsl:variable>

  <xsl:variable name="timeList">
	 <xsl:for-each select="$actions/time/@ellapsed">
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
	 <xsl:for-each select="$times/performance/action[1]/time">
		<l ver="{@ver}"><xsl:value-of select="me:date2Days(string(@date))"/></l>
	 </xsl:for-each>
  </xsl:variable>

  <xsl:variable name="firstDate">
	  <xsl:call-template name="minOfList">
		<xsl:with-param name="list" select="$dateList"/>
	  </xsl:call-template>
  </xsl:variable>

  <xsl:variable name="countX" select="count(ms:node-set($dateList)/l)"/>

  <xsl:variable name="rangeY" select="$maxY - $minY"/>
  <xsl:variable name="scaleY" select="340 div me:log($rangeY)"/>

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
   function log(num)
   { // log base 10
	 if (0.0 >= num)
	   val = 0.0;
	 else
		val = Math.log(num)/Math.LN10;
	  return val;
   }
   function alog(num)
   { // alog base 10
	  return Math.pow(10,num);
   }
   function date2Days(date)
   { // date is mm/dd/yyyy
	  return Date.parse(date)/1000.0/60.0/60.0/24.0;
   }
  function today()
  {
	 d = new Date();
	 image = (d.getMonth()+1) + "/";
	 image += d.getDate() + "/";
	 image += d.getYear();
	 return(image);
  }
</ms:script>

<xsl:template match="/">
  <xsl:apply-templates select="$times/performance"/>
</xsl:template>

<xsl:template match="performance">
<html xmlns:svg="http://www.w3.org/2000/svg">
<head><title><xsl:value-of select="$dataBase"/> Db FLEx Performance</title>
	<object id="AdobeSVG" classid="clsid:78156a80-c6a1-4bbf-8e6a-3cd390eeb4e2"></object>
	<xsl:processing-instruction name="import">
		<xsl:text>namespace="svg" implementation="#AdobeSVG"</xsl:text>
	</xsl:processing-instruction>

</head>
<body>
  <h1><xsl:value-of select="$dataBase"/> FLEx <span style="font-size:16pt">Performance
	  <xsl:value-of select="$times/performance/action/time[last()]/@date"/>
	  to <xsl:value-of select="me:today()"/></span>
  </h1>
   <!--p>minY = <xsl:value-of select="$minY"/></p>
   <p>maxY = <xsl:value-of select="$maxY"/></p>
   <p>minX = <xsl:value-of select="$firstDate"/></p>
   <p>rangeY = <xsl:value-of select="$rangeY"/></p>
   <p>rangeX = <xsl:value-of select="$countX"/></p>
   <p>scaleY = <xsl:value-of select="$scaleY"/></p-->

   <xsl:variable name="zeroTo10">
	 <l>0</l><l>1</l><l>2</l><l>3</l><l>4</l><l>5</l><l>6</l><l>7</l><l>8</l><l>9</l><l>10</l>
   </xsl:variable>

   <xsl:variable name="xTicks">
	 <xsl:for-each select="ms:node-set($dateList)/l">
	   <l n="{position()}" x="{100 + (($countX - position())*490 div ($countX - 1))}" y="365" label="{round(. - $firstDate)}" color="black">
		 <xsl:attribute name="color">
			<xsl:choose>
			<xsl:when test="@ver='D'">
			   <xsl:text>green</xsl:text>
			</xsl:when>
			<xsl:otherwise>
			   <xsl:text>black</xsl:text>
			</xsl:otherwise>
			</xsl:choose>
		 </xsl:attribute>
	   </l>
	 </xsl:for-each>
   </xsl:variable>
   <!--ul>
	 <xsl:for-each select="ms:node-set($xTicks)/l">
	   <li><xsl:value-of select="concat('n=',@n,' (',round(@x),',',round(@y),') ',@label)"/></li>
	 </xsl:for-each>
   </ul-->

   <xsl:variable name="yTicks">
	 <xsl:for-each select="ms:node-set($zeroTo10)/l">
	   <l n="{.}" x="100" y="{((10 - .) div 10)*me:log($rangeY)*$scaleY + 10}" label="{format-number(($minY + me:alog((. div 10)*me:log($rangeY))) div 1000,'0.00')}"/>
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
	   <xsl:for-each select="ms:node-set($xTicks)/l">
		 <xsl:text>M </xsl:text><!-- M is move to absolute location -->
		 <!-- x --><xsl:value-of select="@x"/>
		 <!-- y --><xsl:text> 347 v -340  </xsl:text><!-- v is vertical line length -->
	   </xsl:for-each>
	 </xsl:attribute>
   </svg:path>
   <xsl:for-each select="ms:node-set($xTicks)/l">
	 <svg:text style="text-anchor:middle;" x="{@x - 5}" y="365">
	   <xsl:attribute name="style">
		  <xsl:text>stroke:</xsl:text><xsl:value-of select="@color"/>
	   </xsl:attribute>
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

 </svg:defs>

 <svg:use xlink:href="#Frame" />
 <svg:use xlink:href="#xAxis"/>
 <svg:use xlink:href="#yAxis"/>

 <xsl:apply-templates select="$actions" mode="path"/>

</svg:svg>

  <table border="1">
  <tr><td align="center">All times in seconds</td><td align="center">Min</td><td align="center">Ave</td><td align="center">Max</td><td align="center">Range</td><td align="center">StDev</td><td align="center">Delta</td></tr>
  <xsl:apply-templates select="$actions"/>
  </table>
</body>
</html>
</xsl:template>

<xsl:template match="@*" >
  <xsl:value-of select="concat(' ',100 + ($countX - position())*490 div ($countX - 1),',',350 - me:log(. - $minY)*$scaleY)"/>
</xsl:template>

<xsl:template match="action" >
  <xsl:variable name="pos" select="position()"/>
  <tr>
  <td style="font-weight:bold; color:{ms:node-set($colors)/l[$pos]}">
	<span style="color:black"><xsl:value-of select="$pos"/></span>
	<xsl:text>) </xsl:text>
	<xsl:value-of select="@desc"/>
  </td>
  <!-- collect all times for this one action -->
  <xsl:variable name="at">
	 <xsl:for-each select="time/@ellapsed">
		<l><xsl:value-of select=". div 1000"/></l>
	 </xsl:for-each>
  </xsl:variable>
  <td align="right">
	<xsl:variable name="minAction">
	  <xsl:call-template name="minOfList">
		<xsl:with-param name="list" select="$at"/>
	  </xsl:call-template>
	</xsl:variable>
	<xsl:value-of select="format-number($minAction,'###0.00')"/>
  </td>
  <td align="right">
	<xsl:variable name="aveAction">
	  <xsl:call-template name="averageOfList">
		<xsl:with-param name="list" select="$at"/>
	  </xsl:call-template>
	</xsl:variable>
	<xsl:value-of select="format-number($aveAction,'###0.00')"/>
  </td>
  <td align="right">
	<xsl:variable name="maxAction">
	  <xsl:call-template name="maxOfList">
		<xsl:with-param name="list" select="$at"/>
	  </xsl:call-template>
	</xsl:variable>
	<xsl:value-of select="format-number($maxAction,'###0.00')"/>
  </td>
  <td align="right">
	<xsl:value-of select="format-number(($maxAction - $minAction),'###0.00')"/>
  </td>
  <td align="right">
  <xsl:variable name="atm1"><!--don't list the first item - the last data point added to the set-->
	 <xsl:for-each select="time[position() > 1]/@ellapsed">
		<l><xsl:value-of select=". div 1000"/></l>
	 </xsl:for-each>
  </xsl:variable>
	<xsl:variable name="stDev">
	  <xsl:call-template name="stDevOfList">
		<xsl:with-param name="list" select="$atm1"/>
	  </xsl:call-template>
	</xsl:variable>
	<xsl:variable name="zVal" select="(time[1]/@ellapsed div 1000 - $aveAction) div $stDev"/>
	<xsl:attribute name="style">
	  <xsl:choose>
	  <xsl:when test="string($zVal) > 2.0">
		 <xsl:text>color:red</xsl:text>
	  </xsl:when>
	  <xsl:when test="string($zVal) &lt; -2.0">
		 <xsl:text>color:green</xsl:text>
	  </xsl:when>
	  <xsl:otherwise>
		 <xsl:text>color:blue</xsl:text>
	  </xsl:otherwise>
	  </xsl:choose>
	</xsl:attribute>
	<xsl:value-of select="format-number($stDev,'###0.00')"/>
	<!--xsl:value-of select="format-number($zVal,'######0.00')"/-->
  </td>
  <xsl:variable name="lastTime" select="time[1]/@ellapsed div 1000"/>
  <td align="right">
	<xsl:attribute name="style">
	  <xsl:choose>
	  <xsl:when test="string($lastTime) > $aveAction">
		 <xsl:text>color:red</xsl:text>
	  </xsl:when>
	  <xsl:when test="string($lastTime) &lt; $aveAction">
		 <xsl:text>color:green</xsl:text>
	  </xsl:when>
	  <xsl:otherwise>
		 <xsl:text>color:blue</xsl:text>
	  </xsl:otherwise>
	  </xsl:choose>
	</xsl:attribute>
	  <xsl:variable name="dispTime" select="$lastTime - ms:node-set($at)/l[last()]"/>
	  <xsl:choose>
	  <xsl:when test="string($lastTime) > $aveAction">
		 <xsl:value-of select="format-number($dispTime,'######0.00')"/>
	  </xsl:when>
	  <xsl:when test="string($lastTime) &lt; $aveAction">
		 <xsl:value-of select="format-number($dispTime,'######0.00')"/>
	  </xsl:when>
	  <xsl:otherwise>
		 <xsl:value-of select="format-number($lastTime,'######0.00')"/>
	  </xsl:otherwise>
	  </xsl:choose>
  </td>

  </tr>
</xsl:template>

<xsl:template match="action" mode="path">
  <xsl:variable name="pos" select="position()"/>
  <svg:path style="fill:none; stroke:{ms:node-set($colors)/l[$pos]}; stroke-width:2.0;" d="dummy">
  <xsl:attribute name="d">
	 <xsl:text>M</xsl:text>
	 <xsl:apply-templates select="time/@ellapsed"/>
  </xsl:attribute>
  </svg:path>
</xsl:template>

<xsl:template match="*" />

</xsl:stylesheet>
