<?xml version="1.0" encoding="UTF-8"?>
<?xml-stylesheet type="text/xsl" href="UpdateTimes1MalayDb.xsl"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
xmlns:ms="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="ms">
<xsl:output method="xml" encoding="UTF-8" indent="yes" standalone="yes" />

  <xsl:variable name="times" select="document('TimesMalayDb.xml')"/>

  <xsl:variable name="log-files">
	 <log>C:\fw\Test\GuiTestDriver\TeScripts\aImportScr.xlg</log>
	 <log>C:\fw\Test\GuiTestDriver\TeScripts\aExportScr.xlg</log>
	 <log>C:\fw\Test\GuiTestDriver\TeScripts\aExportBT.xlg</log>
	 <log>C:\fw\Test\GuiTestDriver\TeScripts\aExportNotes.xlg</log>
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
  <xsl:choose>
  <xsl:when test="$act">
	<action desc="{$desc}">
	  <time ellapsed="{$act/@ellapsed-time}" expected="{$act/@expect}" date="{$act/preceding::set-up/@date}" ver="D" />
	  <xsl:copy-of select="time"/>
	</action>
  </xsl:when>
  <xsl:otherwise>
	<action desc="{$desc}">
	  <xsl:copy-of select="time"/>
	</action>
  </xsl:otherwise>
  </xsl:choose>
</xsl:template>

<xsl:template match="*"/>

</xsl:stylesheet>
