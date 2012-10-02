<?xml version="1.0" encoding="UTF-8"?>
<?xml-stylesheet type="text/xsl" href="MinMaxTest.xsl"?>
<xsl:stylesheet
xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0"
xmlns:ms="urn:schemas-microsoft-com:xslt">
<xsl:output method="html" encoding="UTF-8" indent="yes"/>

  <xsl:variable name="expectedMin" select="124"/>
  <xsl:variable name="expectedMax" select="784"/>

	<xsl:variable name="testList1">
	  <l><xsl:value-of select="$expectedMin"/></l>
	  <l>213</l>
	  <l>476</l>
	  <l>346</l>
	  <l>678</l>
	  <l>744</l>
	  <l>768</l>
	  <l>363</l>
	  <l>365</l>
	  <l>365</l>
	  <l>478</l>
	  <l>234</l>
	  <l><xsl:value-of select="$expectedMax"/></l>
	</xsl:variable>

	<xsl:variable name="testList2">
	  <l>213</l>
	  <l>476</l>
	  <l>346</l>
	  <l>678</l>
	  <l>744</l>
	  <l>346</l>
	  <l>768</l>
	  <l><xsl:value-of select="$expectedMax"/></l>
	  <l><xsl:value-of select="$expectedMin"/></l>
	  <l>363</l>
	  <l>346</l>
	  <l>365</l>
	  <l>365</l>
	  <l>478</l>
	  <l>234</l>
	</xsl:variable>

	<xsl:variable name="testList3">
	  <l><xsl:value-of select="$expectedMax"/></l>
	  <l>213</l>
	  <l>476</l>
	  <l>346</l>
	  <l>678</l>
	  <l>365</l>
	  <l>365</l>
	  <l>478</l>
	  <l>234</l>
	  <l><xsl:value-of select="$expectedMin"/></l>
	</xsl:variable>

	<xsl:variable name="testList4">
	  <l><xsl:value-of select="$expectedMin"/></l>
	  <l><xsl:value-of select="$expectedMin"/></l>
	  <l><xsl:value-of select="$expectedMin"/></l>
	  <l><xsl:value-of select="$expectedMin"/></l>
	</xsl:variable>

	<xsl:variable name="testList5">
	  <l></l>
	</xsl:variable>

<xsl:template match="/">
  <html>
  <head><title>Tests MinMax Import Functions</title>
  </head>
  <body>
	<h1>Tests MinMax Import Functions</h1>

	<p>Finding the minimum and maximum values from these lists:
	  <div>List 1: <xsl:apply-templates select="ms:node-set($testList1)/l"/></div>
	  <div>List 2:<xsl:apply-templates select="ms:node-set($testList2)/l"/></div>
	  <div>List 3:<xsl:apply-templates select="ms:node-set($testList3)/l"/></div>
	  <div>List 4:<xsl:apply-templates select="ms:node-set($testList4)/l"/></div>
	  <div>List 5:<xsl:apply-templates select="ms:node-set($testList5)/l"/></div>
	</p>

	<xsl:variable name="min1">
	  <xsl:call-template name="minOfList">
		<xsl:with-param name="list" select="$testList1"/>
	  </xsl:call-template>
	</xsl:variable>

	<xsl:variable name="min2">
	  <xsl:call-template name="minOfList">
		<xsl:with-param name="list" select="$testList2"/>
	  </xsl:call-template>
	</xsl:variable>

	<xsl:variable name="min3">
	  <xsl:call-template name="minOfList">
		<xsl:with-param name="list" select="$testList3"/>
	  </xsl:call-template>
	</xsl:variable>

	<xsl:variable name="min4">
	  <xsl:call-template name="minOfList">
		<xsl:with-param name="list" select="$testList4"/>
	  </xsl:call-template>
	</xsl:variable>

	<xsl:variable name="min5">
	  <xsl:call-template name="minOfList">
		<!--xsl:with-param name="list" select="$testList5"/-->
	  </xsl:call-template>
	</xsl:variable>

	<xsl:variable name="max1">
	  <xsl:call-template name="maxOfList">
		<xsl:with-param name="list" select="$testList1"/>
	  </xsl:call-template>
	</xsl:variable>

	<xsl:variable name="max2">
	  <xsl:call-template name="maxOfList">
		<xsl:with-param name="list" select="$testList2"/>
	  </xsl:call-template>
	</xsl:variable>

	<xsl:variable name="max3">
	  <xsl:call-template name="maxOfList">
		<xsl:with-param name="list" select="$testList3"/>
	  </xsl:call-template>
	</xsl:variable>

	<xsl:variable name="max4">
	  <xsl:call-template name="maxOfList">
		<xsl:with-param name="list" select="$testList4"/>
	  </xsl:call-template>
	</xsl:variable>

	<xsl:variable name="max5">
	  <xsl:call-template name="maxOfList">
		<!--xsl:with-param name="list" select="$testList5"/-->
	  </xsl:call-template>
	</xsl:variable>

	<p>The computed values are:
	  <table border="1">
		<tr><th>Function</th><th>Front</th><th>Middle</th><th>End</th><th>Repeat</th><th>Null</th></tr>
		<tr>
		  <td>Minimum</td>
		  <td style="text-align:center">
			<xsl:call-template name="colorResult">
			  <xsl:with-param name="value" select="$min1"/>
			  <xsl:with-param name="expected" select="$expectedMin"/>
			</xsl:call-template>
		  </td>
		  <td style="text-align:center">
			<span>
			<xsl:call-template name="colorResult">
			  <xsl:with-param name="value" select="$min2"/>
			  <xsl:with-param name="expected" select="$expectedMin"/>
			</xsl:call-template>
			</span>
		  </td>
		  <td style="text-align:center">
			<xsl:call-template name="colorResult">
			  <xsl:with-param name="value" select="$min3"/>
			  <xsl:with-param name="expected" select="$expectedMin"/>
			</xsl:call-template>
		  </td>
		  <td style="text-align:center">
			<xsl:call-template name="colorResult">
			  <xsl:with-param name="value" select="$min4"/>
			  <xsl:with-param name="expected" select="$expectedMin"/>
			</xsl:call-template>
		  </td>
		  <td style="text-align:center">
			<xsl:call-template name="colorResult">
			  <xsl:with-param name="value" select="$min5"/>
			  <xsl:with-param name="expected" select="0"/>
			</xsl:call-template>
		  </td>
		</tr>
		<tr>
		  <td>Maximum</td>
		  <td style="text-align:center">
			<xsl:call-template name="colorResult">
			  <xsl:with-param name="value" select="$max1"/>
			  <xsl:with-param name="expected" select="$expectedMax"/>
			</xsl:call-template>
		  </td>
		  <td style="text-align:center">
			<span>
			<xsl:call-template name="colorResult">
			  <xsl:with-param name="value" select="$max2"/>
			  <xsl:with-param name="expected" select="$expectedMax"/>
			</xsl:call-template>
			</span>
		  </td>
		  <td style="text-align:center">
			<xsl:call-template name="colorResult">
			  <xsl:with-param name="value" select="$max3"/>
			  <xsl:with-param name="expected" select="$expectedMax"/>
			</xsl:call-template>
		  </td>
		  <td style="text-align:center">
			<xsl:call-template name="colorResult">
			  <xsl:with-param name="value" select="$max4"/>
			  <xsl:with-param name="expected" select="$expectedMin"/>
			</xsl:call-template>
		  </td>
		  <td style="text-align:center">
			<xsl:call-template name="colorResult">
			  <xsl:with-param name="value" select="$max5"/>
			  <xsl:with-param name="expected" select="0"/>
			</xsl:call-template>
		  </td>
		</tr>
	  </table>
	</p>
  </body>
  </html>
</xsl:template>

<xsl:template match="l">
  <xsl:text> </xsl:text>
  <span>
	<xsl:choose>
	<xsl:when test=". = $expectedMin">
		<xsl:attribute name="style">
		  <xsl:text>color:blue</xsl:text>
		</xsl:attribute>
	</xsl:when>
	<xsl:when test=". = $expectedMax">
		<xsl:attribute name="style">
		  <xsl:text>color:red</xsl:text>
		</xsl:attribute>
	</xsl:when>
	</xsl:choose>
	<xsl:value-of select="."/>
  </span>
</xsl:template>

<xsl:template name="colorResult">
<xsl:param name="value"/>
<xsl:param name="expected" select="0"/>
  <span>
	<xsl:attribute name="style">
	  <xsl:choose>
	  <xsl:when test="$value = $expected">
		<xsl:text>color:green</xsl:text>
	  </xsl:when>
	  <xsl:otherwise>
		<xsl:text>color:red</xsl:text>
	  </xsl:otherwise>
	  </xsl:choose>
	</xsl:attribute>
	<xsl:value-of select="$value"/>
  </span>
</xsl:template>

<xsl:include href="MinMax.xsl"/>

</xsl:stylesheet>
