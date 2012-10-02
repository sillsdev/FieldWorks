<?xml version="1.0" encoding="ISO-8859-1"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<xsl:param name="nant.filename" />
<xsl:param name="nant.version" />
<xsl:param name="nant.project.name" />
<xsl:param name="nant.project.buildfile" />
<xsl:param name="nant.project.basedir" />
<xsl:param name="nant.project.default" />
<xsl:param name="sys.os" />
<xsl:param name="sys.os.platform" />
<xsl:param name="sys.os.version" />
<xsl:param name="sys.clr.version" />

<!-- key used to select test-case classnames -->
<xsl:key name="classnameKey" match="test-case" use="@classname" />


<!--
   This XSL File is based on the toolkit.xsl
   template created by Erik Hatcher fot Ant's JUnitReport

   Modified by Tomas Restrepo (tomasr@mvps.org) for use
   with NUnitReport
-->

<!--
	format a number in to display its value in percent
	@param value the number to format
-->
<xsl:template name="display-time">
	<xsl:param name="value"/>
	<xsl:value-of select="format-number($value,'0.000')"/>
</xsl:template>

<!--
	format a number in to display its value in percent
	@param value the number to format
-->
<xsl:template name="display-percent">
	<xsl:param name="value"/>
	<xsl:value-of select="format-number($value,'0.00%')"/>
</xsl:template>

<!--
	transform string like a.b.c to ../../../
	@param path the path to transform into a descending directory path
-->
<xsl:template name="path">
	<xsl:param name="path"/>
	<xsl:if test="contains($path,'.')">
		<xsl:text>../</xsl:text>
		<xsl:call-template name="path">
			<xsl:with-param name="path"><xsl:value-of select="substring-after($path,'.')"/></xsl:with-param>
		</xsl:call-template>
	</xsl:if>
	<xsl:if test="not(contains($path,'.')) and not($path = '')">
		<xsl:text>../</xsl:text>
	</xsl:if>
</xsl:template>

<!--
	template that will convert a carriage return into a br tag
	@param word the text from which to convert CR to BR tag
-->
<xsl:template name="br-replace">
	<xsl:param name="word"/>
	<xsl:choose>
		<xsl:when test="contains($word,'&#xA;')">
			<xsl:value-of select="substring-before($word,'&#xA;')"/>
			<br/>
			<xsl:call-template name="br-replace">
				<xsl:with-param name="word" select="substring-after($word,'&#xA;')"/>
			</xsl:call-template>
		</xsl:when>
		<xsl:otherwise>
			<xsl:value-of select="$word"/>
		</xsl:otherwise>
	</xsl:choose>
</xsl:template>


<!--
		=====================================================================
		classes summary header
		=====================================================================
-->
<xsl:template name="header">
	<xsl:param name="path"/>
	<h1>Unit Tests Results - <xsl:value-of select="$nant.project.name"/></h1>
	<table width="100%">
	<tr>
	   <td align="left">
		  Generated: <xsl:value-of select="@created"/> -
		  <a href="#envinfo">Environment Information</a>
	   </td>
		<td align="right">Designed for use with
		   <a href='http://nunit.sourceforge.net/'>NUnit</a> and
		   <a href='http://nant.sourceforge.net/'>NAnt</a>.
		</td>
	</tr>
	</table>
	<hr size="1"/>
</xsl:template>

<xsl:template name="summaryHeader">
	<thead>
	<tr valign="top">
		<td>Tests</td>
		<td>Failures</td>
		<td>Ignored</td>
		<td>Success Rate</td>
		<td nowrap="nowrap">Time(s)</td>
	</tr>
	</thead>
</xsl:template>

<!--
		=====================================================================
		package summary header
		=====================================================================
-->
<xsl:template name="packageSummaryHeader">
	<thead>
	<tr valign="top">
		<td width="75%">Name</td>
		<td width="5%">Tests</td>
		<td width="5%">Ignored</td>
		<td width="5%">Failures</td>
		<td width="10%" nowrap="nowrap">Time(s)</td>
	</tr>
	</thead>
</xsl:template>

<!--
		=====================================================================
		classes summary header
		=====================================================================
-->
<xsl:template name="classesSummaryHeader">
	<thead>
	<tr valign="top" style="height: 4px">
		<td width="85%">Name</td>
		<td width="10%">Status</td>
		<td width="5%" nowrap="nowrap">Time(s)</td>
	</tr>
	</thead>
</xsl:template>

<!--
		=====================================================================
		Write the summary report
		It creates a table with computed values from the document:
		User | Date | Environment | Tests | Failures | Ignored | Rate | Time
		Note : this template must call at the testsuites level
		=====================================================================
-->
	<xsl:template name="summary">
		<h2>Summary</h2>
		<xsl:variable name="ignoredCount" select="sum(//test-results/@not-run)"/>
		<xsl:variable name="testCount" select="sum(//test-results/@total) + $ignoredCount"/>
		<xsl:variable name="failureCount" select="sum(//test-results/@failures) + sum(//test-results/@errors)"/>
		<xsl:variable name="timeCount" select="sum(//test-results/test-suite/@time)"/>
		<xsl:variable name="successRate" select="($testCount - $failureCount - $ignoredCount) div $testCount"/>

		<table border="0" class="DetailTable" width="95%">
		<xsl:call-template name="summaryHeader"/>
		<tr valign="top">
			<xsl:attribute name="class">
				<xsl:choose>
					<xsl:when test="$failureCount &gt; 0">Failure</xsl:when>
					<xsl:when test="$ignoredCount &gt; 0">Ignored</xsl:when>
					<xsl:otherwise>Pass</xsl:otherwise>
				</xsl:choose>
			</xsl:attribute>
			<td><xsl:value-of select="$testCount"/></td>
			<td><xsl:value-of select="$failureCount"/></td>
			<td><xsl:value-of select="$ignoredCount"/></td>
			<td>
				<xsl:call-template name="display-percent">
					<xsl:with-param name="value" select="$successRate"/>
				</xsl:call-template>
			</td>
			<td>
				<xsl:call-template name="display-time">
					<xsl:with-param name="value" select="$timeCount"/>
				</xsl:call-template> (<xsl:value-of select="floor($timeCount div 3600)"/> h
				<xsl:value-of select="floor($timeCount div 60)"/> m
				<xsl:value-of select="floor($timeCount mod 60)"/> s)
			</td>
		</tr>
		</table>
	</xsl:template>

<!--
		=====================================================================
		test-case report
		=====================================================================
-->
<xsl:template match="test-case">
   <xsl:variable name="result">
			<xsl:choose>
			  <xsl:when test="contains(@success, 'False')">Failure</xsl:when>
				<xsl:when test="contains(@executed, 'False')">Ignored</xsl:when>
				<xsl:otherwise>Pass</xsl:otherwise>
			</xsl:choose>
   </xsl:variable>
   <xsl:variable name="newid" select="generate-id(@name)" />
	<TR valign="top">
		<xsl:attribute name="class"><xsl:value-of select="$result"/></xsl:attribute>
	   <xsl:if test="$result != &quot;Pass&quot;">
		  <xsl:attribute name="onclick">javascript:toggle(<xsl:value-of select="$newid"/>)</xsl:attribute>
		  <xsl:attribute name="style">cursor: hand;</xsl:attribute>
	   </xsl:if>

		<TD><xsl:value-of select="@name"/><br/>
			<xsl:if test="$result != &quot;Pass&quot;">
			<table width="100%">
	   <tr style="display: block;">
		  <xsl:attribute name="id">
			 <xsl:value-of select="$newid"/>
		  </xsl:attribute>
		  <td colspan="3" class="FailureDetail">
			 <xsl:apply-templates select="reason"/>
			 <xsl:apply-templates select="failure"/>
		 </td>
	  </tr></table>
	</xsl:if>
	</TD>
		<td><xsl:value-of select="$result"/></td>
		<td>
			<xsl:call-template name="display-time">
				<xsl:with-param name="value" select="@time"/>
			</xsl:call-template>
		</td>
	</TR>
</xsl:template>

<!-- Note : the below template error and failure are the same style
			so just call the same style store in the toolkit template -->
<xsl:template match="failure">
	<xsl:choose>
		<xsl:when test="not(message)">N/A</xsl:when>
		<xsl:otherwise>
			<xsl:value-of select="message"/>
		</xsl:otherwise>
	</xsl:choose>
	<!-- display the stacktrace -->
	<code>
		<p/>
		<xsl:call-template name="br-replace">
			<xsl:with-param name="word" select="stack-trace"/>
		</xsl:call-template>
	</code>
</xsl:template>

<xsl:template match="reason">
	<xsl:choose>
		<xsl:when test="not(message)">N/A</xsl:when>
		<xsl:otherwise>
			<xsl:value-of select="message"/>
		</xsl:otherwise>
	</xsl:choose>
</xsl:template>


<!--
		=====================================================================
		Environtment Info Report
		=====================================================================
-->
<xsl:template name="envinfo">
   <a name="envinfo"></a>
	<h2>Environment Information</h2>
	<table border="0" class="DetailTable" width="95%">
	   <tr class="EnvInfoHeader">
		  <td>Property</td>
		  <td>Value</td>
	   </tr>
		<tr class="EnvInfoRow">
			<td>NUnit Version</td>
			<td>
				<xsl:value-of select="//environment/@nunit-version"/>
			</td>
		</tr>
		<tr class="EnvInfoRow">
		  <td>NAnt Location</td>
		  <td><xsl:value-of select="$nant.filename"/></td>
	   </tr>
	   <tr class="EnvInfoRow">
		  <td>NAnt Version</td>
		  <td><xsl:value-of select="$nant.version"/></td>
	   </tr>
	   <tr class="EnvInfoRow">
		  <td>Buildfile</td>
		  <td><xsl:value-of select="$nant.project.buildfile"/></td>
	   </tr>
	   <tr class="EnvInfoRow">
		  <td>Base Directory</td>
		  <td><xsl:value-of select="$nant.project.basedir"/></td>
	   </tr>
	   <tr class="EnvInfoRow">
		  <td>Operating System</td>
		  <td><xsl:value-of select="$sys.os"/></td>
<!--
	  If this doesn't look right, your version of NAnt
	  has a broken sysinfo task...
		  <td><xsl:value-of select="$sys.os.platform"/> - <xsl:value-of select="$sys.os.version"/></td>
	  or
		  <td><xsl:value-of select="$sys.os.version"/></td>
-->
	   </tr>
	   <tr class="EnvInfoRow">
		  <td>.NET CLR Version</td>
		  <td><xsl:value-of select="$sys.clr.version"/></td>
	   </tr>
		<tr class="EnvInfoRow">
			<td>Current Culture</td>
			<td>
				<xsl:value-of select="//culture-info/@current-culture"/>
			</td>
		</tr>
		<tr class="EnvInfoRow">
			<td>Current UI Culture</td>
			<td>
				<xsl:value-of select="//culture-info/@current-uiculture"/>
			</td>
		</tr>
	</table>
	<a href="#top">Back to top</a>
</xsl:template>

<!-- I am sure that all nodes are called -->
<xsl:template match="*">
	<xsl:apply-templates/>
</xsl:template>

	<!-- Extracts the filename and extension from a full path -->
	<xsl:template name="filename">
		<xsl:param name="filename"/>
		<xsl:choose>
			<xsl:when test="string-length(substring-after($filename, '/')) &gt; 0">
				<xsl:call-template name="filename-ux">
					<xsl:with-param name="filename" select="substring-after($filename, '/')"/>
				</xsl:call-template>
			</xsl:when>
			<xsl:otherwise>
				<xsl:call-template name="filename-win">
					<xsl:with-param name="filename" select="$filename"/>
				</xsl:call-template>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template name="filename-win">
		<xsl:param name="filename"/>
		<xsl:choose>
			<xsl:when test="string-length(substring-after($filename, '\')) &gt; 0">
				<xsl:call-template name="filename-win">
					<xsl:with-param name="filename" select="substring-after($filename, '\')"/>
				</xsl:call-template>
			</xsl:when>
			<xsl:otherwise><xsl:value-of select="$filename"/></xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<xsl:template name="filename-ux">
		<xsl:param name="filename"/>
		<xsl:choose>
			<xsl:when test="string-length(substring-after($filename, '/')) &gt; 0">
				<xsl:call-template name="filename-ux">
					<xsl:with-param name="filename" select="substring-after($filename, '/')"/>
				</xsl:call-template>
			</xsl:when>
			<xsl:otherwise><xsl:value-of select="$filename"/></xsl:otherwise>
		</xsl:choose>
	</xsl:template>

</xsl:stylesheet>
