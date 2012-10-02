<?xml version="1.0" encoding="ISO-8859-1"?>

<!--
   This XSL File is based on the summary_overview.xsl
   template created by Erik Hatcher fot Ant's JUnitReport.

   Modified by Tomas Restrepo (tomasr@mvps.org) for use
   with NUnitReport
-->

<xsl:stylesheet version="1.0"
	  xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	  xmlns:html="http://www.w3.org/Profiles/XHTML-transitional"
   >

   <xsl:output method="html" indent="yes" />
   <xsl:include href="toolkit.xsl"/>

<!--
	====================================================
		Create the page structure
	====================================================
-->
<xsl:template match="testsummary">
	<HTML>
		<HEAD>
			<title>Unit Tests Results - <xsl:value-of select="$nant.project.name"/></title>
		<!-- put the style in the html so that we can mail it w/o problem -->
		<style type="text/css">
			BODY {
			   font: normal 10px verdana, arial, helvetica;
			   color:#000000;
			}
			TD {
			   FONT-SIZE: 10px
			}
			P {
			   line-height:1.5em;
			   margin-top:0.5em; margin-bottom:1.0em;
			}
			H1 {
			MARGIN: 0px 0px 5px;
			FONT: bold arial, verdana, helvetica;
			FONT-SIZE: 16px;
			}
			H2 {
			MARGIN-TOP: 1em; MARGIN-BOTTOM: 0.5em;
			FONT: bold 14px verdana,arial,helvetica
			}
			H3 {
			MARGIN-BOTTOM: 0.5em; FONT: bold 13px verdana,arial,helvetica
			}
			H4 {
			   MARGIN-BOTTOM: 0.5em; FONT: bold 100% verdana,arial,helvetica
			}
			H5 {
			MARGIN-BOTTOM: 0.5em; FONT: bold 100% verdana,arial,helvetica
			}
			H6 {
			MARGIN-BOTTOM: 0.5em; FONT: bold 100% verdana,arial,helvetica
			}
		 .Ignored {
			font-weight:bold; background:#EEEEE0; color:purple;
		 }
		 .Failure {
			font-weight:bold; background:#EEEEE0; color:red;
		 }
		 .ClassName {
			font-weight:bold;
			padding-left: 18px;
			cursor: hand;
			color: #777;
		 }
		 .TestClassDetails {
			width: 95%;
			margin-bottom: 10px;
			border-bottom: 1px dotted #999;
		 }
		 .FailureDetail {
			font-size: -1;
			padding-left: 2.0em;
			border: 1px solid #999;
			background: white;
		 }
		 .Pass {
			background:#EEEEE0;
		 }
		 .White {
			background: white;
		 }
		 .DetailTable TD {
			padding-top: 1px;
			padding-bottom: 1px;
			padding-left: 3px;
			padding-right: 3px;
		 }
		 thead {
			background: #6699cc;
			color: white;
			font-weight: bold;
			horizontal-align: center;
		 }
		 .EnvInfoHeader {
			background: #ff0000;
			color: white;
			font-weight: bold;
			horizontal-align: center;
		 }
		 .EnvInfoRow {
			background:#EEEEE0
		 }

		 A:visited {
			color: #0000ff;
		 }
		 A {
			color: #0000ff;
		 }
		 A:active {
			color: #800000;
		 }
		 thead td {
			font-weight: bold;
		 }
		 tfoot {
			background: #6699cc;
			border-top: thick solid black;
			color: white;
			font-weight: bold;
		 }
			</style>
	  <script language="JavaScript"><![CDATA[
		function toggle (field)
		{
		  field.style.display = (field.style.display == "block") ? "none" : "block";
		}  ]]>
	  </script>
		</HEAD>
		<body text="#000000" bgColor="#ffffff">
			<a name="#top"></a>
			<xsl:call-template name="header"/>

			<!-- Summary part -->
			<xsl:call-template name="summary"/>
			<hr size="1" width="95%" align="left"/>

			<!-- Package List part -->
			<xsl:call-template name="packagelist"/>

			<!-- Detailed results for each assembly -->
			<hr size="3" width="95%" align="left"/>
			<h1>TestSuite Details</h1>
			<xsl:apply-templates select="//test-results">
				<xsl:sort select="@name"/>
			</xsl:apply-templates>

			<!-- Environment info part -->
			<xsl:call-template name="envinfo"/>

		</body>
	</HTML>
</xsl:template>



	<!-- ================================================================== -->
	<!-- Write a list of all packages with an hyperlink to the anchor of    -->
	<!-- of the package name.                                               -->
	<!-- ================================================================== -->
	<xsl:template name="packagelist">
		<h2>TestSuite Summary</h2>
		<table border="0" class="DetailTable" width="95%" style="behavior:url(sort.htc);">
			<xsl:call-template name="packageSummaryHeader"/>
			<!-- list all packages recursively -->
			<tbody>
			<xsl:for-each select="//test-results">
				<xsl:sort select="@name"/>
				<xsl:variable name="testCount" select="@total + @not-run"/>
				<xsl:variable name="notrunCount" select="@not-run"/>
				<xsl:variable name="failureCount" select="@failures"/>
				<xsl:variable name="errorCount" select="@errors"/>
				<xsl:variable name="timeCount" select="sum(test-suite/@time)"/>
				<xsl:variable name="successRate" select="($testCount - $failureCount - $errorCount - $notrunCount) div $testCount"/>
				<!-- write a summary for the package -->
				<tr valign="top">
					<!-- set a nice color depending if there is an error/failure -->
					<xsl:attribute name="class">
						<xsl:choose>
							<xsl:when test="($failureCount + $errorCount) &gt; 0">Failure</xsl:when>
							<xsl:when test="$notrunCount &gt; 0">Ignored</xsl:when>
							<xsl:otherwise>Pass</xsl:otherwise>
						</xsl:choose>
					</xsl:attribute>
					<td><a href="#{generate-id(@name)}"><xsl:call-template name="filename">
						<xsl:with-param name="filename" select="@name"/></xsl:call-template></a></td>
					<td><xsl:value-of select="$testCount"/></td>
					<td><xsl:value-of select="$notrunCount"/></td>
					<td><xsl:value-of select="$failureCount + $errorCount"/></td>
					<td>
				  <xsl:call-template name="display-time">
					 <xsl:with-param name="value" select="$timeCount"/>
				  </xsl:call-template>
					</td>
				</tr>
			</xsl:for-each>
			</tbody>
		</table>
	</xsl:template>

	<!-- ================================================================== -->
	<!-- Writes summary line with the number of failed and passed tests     -->
	<!-- and time                                                           -->
	<!-- ================================================================== -->
	<xsl:template name="test-results-summary">
		<xsl:variable name="totalTestCount" select="@total + @not-run"/>
		<xsl:variable name="totalNotrunCount" select="@not-run"/>
		<xsl:variable name="totalFailureCount" select="@failures"/>
		<xsl:variable name="totalErrorCount" select="@errors"/>
		<xsl:variable name="totalTimeCount" select="test-suite/@time"/>
		<xsl:variable name="totalSuccessRate" select="($totalTestCount - $totalFailureCount - $totalErrorCount - $totalNotrunCount) div $totalTestCount"/>
		<!-- write a summary line -->
		<tfoot>
		<tr valign="top">
			<td class="White"></td>
			<td><xsl:value-of select="$totalTestCount"/></td>
			<td><xsl:value-of select="$totalNotrunCount"/></td>
			<td><xsl:value-of select="$totalFailureCount + $totalErrorCount"/></td>
			<td>
			<xsl:call-template name="display-time">
				<xsl:with-param name="value" select="$totalTimeCount"/>
			</xsl:call-template>
			</td>
		</tr>
		</tfoot>
	</xsl:template>

	<!-- ================================================================== -->
	<!-- Write a list of all packages with an hyperlink to the anchor of    -->
	<!-- of the package name.                                               -->
	<!-- ================================================================== -->
	<xsl:template match="test-results">
		<xsl:variable name="details"><xsl:value-of select="generate-id(@name)"/></xsl:variable>
		<h2 id="{$details}" ><xsl:call-template name="filename">
			<xsl:with-param name="filename" select="@name"/></xsl:call-template> Summary</h2>
		<table border="0" class="DetailTable" width="95%" style="behavior:url(sort.htc);">
			<xsl:call-template name="packageSummaryHeader"/>
			<tbody>
			<!-- list all packages recursively -->
			<xsl:for-each select="descendant::node()/test-suite[results/test-case]">
				<xsl:sort select="@name"/>
				<xsl:variable name="testCount" select="count(results/test-case)"/>
				<xsl:variable name="notrunCount" select="count(results/test-case[@executed='False'])"/>
				<xsl:variable name="failureCount" select="count(results/test-case[@success='False'])"/>
				<xsl:variable name="timeCount" select="@time"/>
				<xsl:variable name="successRate" select="($testCount - $failureCount - $notrunCount) div $testCount"/>
				<!-- write a summary for the package -->
				<tr valign="top">
					<!-- set a nice color depending if there is an error/failure -->
					<xsl:attribute name="class">
						<xsl:choose>
							<xsl:when test="$failureCount &gt; 0">Failure</xsl:when>
							<xsl:when test="$notrunCount &gt; 0">Ignored</xsl:when>
							<xsl:otherwise>Pass</xsl:otherwise>
						</xsl:choose>
					</xsl:attribute>
					<td><a href="#{generate-id(@name)}"><xsl:value-of select="@name"/></a></td>
					<td><xsl:value-of select="$testCount"/></td>
					<td><xsl:value-of select="$notrunCount"/></td>
					<td><xsl:value-of select="$failureCount"/></td>
					<td>
				  <xsl:call-template name="display-time">
					 <xsl:with-param name="value" select="$timeCount"/>
				  </xsl:call-template>
					</td>
				</tr>
			</xsl:for-each>
			</tbody>
			<xsl:call-template name="test-results-summary"/>
		</table>

		<!-- Detailed results for each test suite -->
		<xsl:if test="count(descendant::node()/test-suite[results/test-case]) > 0">
			<hr size="1" width="95%" align="left"/>
			<h2>Test Details</h2>
			<xsl:apply-templates select="descendant::node()/test-suite[results/test-case]">
				<xsl:sort select="@name"/>
			</xsl:apply-templates>
		</xsl:if>

	</xsl:template>

	<!-- ================================================================== -->
	<!-- Write a list of all classes used in a testsuite, alongside with    -->
	<!-- the results for each one.                                          -->
	<!-- ================================================================== -->
	<xsl:template match="test-suite">

		<!-- create an anchor to this class name -->
		<a name="#{generate-id(@name)}"></a>
		<h3>TestSuite <xsl:value-of select="@name"/></h3>

		 <xsl:variable name="details"><xsl:value-of select="generate-id(@name)"/></xsl:variable>
		 <div class="TestClassDetails">
			<div class="ClassName" onclick="toggle({$details})">
			   <xsl:value-of select="@classname"/>
			</div>
			  <table border="0" width="80%" id="{$details}"
				style="behavior:url(sort.htc);display: block; margin-left: 35px" class="DetailTable">
				  <xsl:call-template name="classesSummaryHeader"/>
				  <tbody>
				  <xsl:apply-templates select="results/test-case">
					  <xsl:sort select="@name" />
				  </xsl:apply-templates>
				  </tbody>
				  <xsl:call-template name="test-case-summary"/>
			  </table>
		   </div>
		<a href="#top">Back to top</a>
		<hr size="1" width="95%" align="left"/>
	</xsl:template>

	<!-- ================================================================== -->
	<!-- Writes summary line with the time the tests took                   -->
	<!-- ================================================================== -->
	<xsl:template name="test-case-summary">
		<!-- write a summary line -->
		<tfoot>
		<tr valign="top">
			<td class="White"></td>
			<td class="White"></td>
			<td>
			<xsl:call-template name="display-time">
				<xsl:with-param name="value" select="@time"/>
			</xsl:call-template>
			</td>
		</tr>
		</tfoot>
	</xsl:template>


  <xsl:template name="dot-replace">
	  <xsl:param name="package"/>
	  <xsl:choose>
		  <xsl:when test="contains($package,'.')"><xsl:value-of select="substring-before($package,'.')"/>_<xsl:call-template name="dot-replace"><xsl:with-param name="package" select="substring-after($package,'.')"/></xsl:call-template></xsl:when>
		  <xsl:otherwise><xsl:value-of select="$package"/></xsl:otherwise>
	  </xsl:choose>
  </xsl:template>

</xsl:stylesheet>
