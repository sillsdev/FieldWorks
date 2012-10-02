<?xml version="1.0" encoding="UTF-8"?>
<?xml-stylesheet type="text/xsl" href="UpdateTimes.xsl"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
xmlns:ms="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="ms">
<xsl:output method="xml" encoding="UTF-8" indent="yes" standalone="yes" />

  <xsl:variable name="times" select="document('Times.xml')"/>

  <xsl:variable name="log-files">
	 <log>aaThaiOpen.xlg</log>
	 <log>abThaiReopen.xlg</log>
	 <log>acThaiSort.xlg</log>
	 <log>adThaiViews.xlg</log>
	 <!--log>baTagaOpen.xlg</log-->
	 <log>bbTagaWords.xlg</log>
	 <!--log>caMutsunOpen.xlg</log-->
	 <log>cbMutsun.xlg</log>
	 <log>ccMutsun.xlg</log>
	 <!--log>daEngBibleOpen.xlg</log-->
	 <log>dbEngBible.xlg</log>
  </xsl:variable>

  <xsl:variable name="logs" select="document(ms:node-set($log-files)/log)"/>

<xsl:template match="/">

  <xsl:processing-instruction name="xml-stylesheet">
	 <xsl:text>type="text/xsl" href="Performance.xsl"</xsl:text>
  </xsl:processing-instruction>

  <performance>
	<xsl:apply-templates select="$times/performance/action"/>
  </performance>

</xsl:template>

<xsl:template match="action">
  <xsl:variable name="desc" select="@desc"/>
  <xsl:variable name="act" select="$logs/gtdLog/result[@tag = 'monitor-time' and @desc = $desc]"/>
  <action desc="{$desc}">
	  <xsl:if test="$act">
		  <time ellapsed="{$act/@ellapsed-time}" expected="{$act/@expect}" date="{$act/preceding::set-up/@date}" ver="D" />
	  </xsl:if>
	  <xsl:copy-of select="time"/>
  </action>
</xsl:template>

<xsl:template match="*"/>

</xsl:stylesheet>
