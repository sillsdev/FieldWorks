<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet
xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0"
xmlns:ms="urn:schemas-microsoft-com:xslt"
xmlns:me="MathExtension">

<!--
minOfList(list)
maxOfList(list)
averageOfList(list)
sumOfSqrs(list)
stDevOfList(list) : for a sample (n-1)
dropFirstNfromList(n,list)
-->

<ms:script language="JScript" implements-prefix="me">
  function sqroot(num) {return Math.sqrt(num);}
</ms:script>

  <xsl:variable name="zeroList">
	<l>0</l>
  </xsl:variable>

  <!-- The list structure is &lt;l>num1&lt;/l> .. &lt;l>numN&lt;/l> -->
  <!-- Returns the least number from the list -->
  <xsl:template name="minOfList">
	<xsl:param name="list" select="$zeroList"/>
	<xsl:call-template name="minimumOfList">
	  <xsl:with-param name="minSoFar" select="$list/l[1]"/>
	  <xsl:with-param name="scanInd" select="2"/>
	  <xsl:with-param name="list" select="$list"/>
	</xsl:call-template>
  </xsl:template>

  <xsl:template name="minimumOfList">
	<xsl:param name="minSoFar"/>
	<xsl:param name="scanInd"/>
	<xsl:param name="list"/>
	<xsl:choose>
	<xsl:when test="$list/l[$scanInd]">
	  <xsl:variable name="newMin">
		<xsl:choose>
		<xsl:when test="$list/l[$scanInd] &lt; $minSoFar">
		  <xsl:value-of select="$list/l[$scanInd]"/>
		</xsl:when>
		<xsl:otherwise>
		  <xsl:value-of select="$minSoFar"/>
		</xsl:otherwise>
		</xsl:choose>
	  </xsl:variable>
	  <xsl:call-template name="minimumOfList">
		<xsl:with-param name="minSoFar" select="$newMin"/>
		<xsl:with-param name="scanInd" select="$scanInd + 1"/>
		<xsl:with-param name="list" select="$list"/>
	  </xsl:call-template>
	</xsl:when>
	<xsl:otherwise>
	  <xsl:value-of select="$minSoFar"/>
	</xsl:otherwise>
	</xsl:choose>
  </xsl:template>


  <!-- The list structure is &lt;l>num1&lt;/l> .. &lt;l>numN&lt;/l> -->
  <!-- Returns the greatest number from the list -->
  <xsl:template name="maxOfList">
	<xsl:param name="list" select="$zeroList"/>
	<xsl:call-template name="maximumOfList">
	  <xsl:with-param name="maxSoFar" select="$list/l[1]"/>
	  <xsl:with-param name="scanInd" select="2"/>
	  <xsl:with-param name="list" select="$list"/>
	</xsl:call-template>
  </xsl:template>

  <xsl:template name="maximumOfList">
	<xsl:param name="maxSoFar"/>
	<xsl:param name="scanInd"/>
	<xsl:param name="list"/>
	<xsl:choose>
	<xsl:when test="$list/l[$scanInd]">
	  <xsl:variable name="newMax">
		<xsl:choose>
		<xsl:when test="$list/l[$scanInd] &gt; $maxSoFar">
		  <xsl:value-of select="$list/l[$scanInd]"/>
		</xsl:when>
		<xsl:otherwise>
		  <xsl:value-of select="$maxSoFar"/>
		</xsl:otherwise>
		</xsl:choose>
	  </xsl:variable>
	  <xsl:call-template name="maximumOfList">
		<xsl:with-param name="maxSoFar" select="$newMax"/>
		<xsl:with-param name="scanInd" select="$scanInd + 1"/>
		<xsl:with-param name="list" select="$list"/>
	  </xsl:call-template>
	</xsl:when>
	<xsl:otherwise>
	  <xsl:value-of select="$maxSoFar"/>
	</xsl:otherwise>
	</xsl:choose>
  </xsl:template>


  <!-- The list structure is &lt;l>num1&lt;/l> .. &lt;l>numN&lt;/l> -->
  <!-- Returns the average number from the list -->
  <xsl:template name="averageOfList">
	<xsl:param name="list" select="$zeroList"/>
	<xsl:value-of select="sum($list/l) div count($list/l)"/>
  </xsl:template>


  <!-- The list structure is &lt;l>num1&lt;/l> .. &lt;l>numN&lt;/l> -->
  <!-- Returns the sum of the squares of each number from the list -->
  <xsl:template name="sumOfSqrs">
	<xsl:param name="list" select="$zeroList"/>
	<xsl:call-template name="sumSqOfList">
	  <xsl:with-param name="sumSqSoFar" select="$list/l[1] * $list/l[1]"/>
	  <xsl:with-param name="scanInd" select="2"/>
	  <xsl:with-param name="list" select="$list"/>
	</xsl:call-template>
  </xsl:template>

  <xsl:template name="sumSqOfList">
	<xsl:param name="sumSqSoFar"/>
	<xsl:param name="scanInd"/>
	<xsl:param name="list"/>
	<xsl:choose>
	<xsl:when test="$list/l[$scanInd]">
	  <xsl:call-template name="sumSqOfList">
		<xsl:with-param name="sumSqSoFar" select="$sumSqSoFar + $list/l[$scanInd] * $list/l[$scanInd]"/>
		<xsl:with-param name="scanInd" select="$scanInd + 1"/>
		<xsl:with-param name="list" select="$list"/>
	  </xsl:call-template>
	</xsl:when>
	<xsl:otherwise>
	  <xsl:value-of select="$sumSqSoFar"/>
	</xsl:otherwise>
	</xsl:choose>
  </xsl:template>


  <!-- The list structure is &lt;l>num1&lt;/l> .. &lt;l>numN&lt;/l> -->
  <!-- Returns the standard deviation of the list of numbers -->
  <xsl:template name="stDevOfList">
	<xsl:param name="list" select="$zeroList"/>
	<xsl:variable name="ssqs">
	  <xsl:call-template name="sumOfSqrs">
		<xsl:with-param name="list" select="$list"/>
	  </xsl:call-template>
	</xsl:variable>
	<xsl:variable name="ave" select="sum($list/l) div count($list/l)"/>
	<xsl:value-of select="me:sqroot(string(($ssqs - count($list/l)*$ave*$ave) div (count($list/l) - 1)))"/>
  </xsl:template>


  <!-- Drops the first N items from the list -->
  <xsl:template name="dropFirstNfromList">
	<xsl:param name="n" select="0"/>
	<xsl:param name="list" select="'&lt;l>0&lt;/l>'"/>
	<xsl:if test="$n &gt; 0">
	  <xsl:for-each select="$list/l[position() &gt; $n]">
		<xsl:copy-of select="."/>
	  </xsl:for-each>
	</xsl:if>
  </xsl:template>

</xsl:stylesheet>
