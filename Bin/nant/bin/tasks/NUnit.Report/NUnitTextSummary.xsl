<?xml version="1.0" encoding="ISO-8859-1"?>

<!--
   Creates a text summary for NUnit tests
-->

<xsl:stylesheet version="1.0"
	  xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	  xmlns:html="http://www.w3.org/Profiles/XHTML-transitional"
   >

   <xsl:output method="text" indent="yes" />

<xsl:template match="testsummary">
	<xsl:variable name="testCount" select="sum(//test-results/@total)"/>
	<xsl:variable name="ignoredCount" select="sum(//test-results/@not-run)"/>
	<xsl:variable name="failureCount" select="sum(//test-results/@failures)"/>
	<xsl:variable name="timeCount" select="sum(//test-results/test-suite/@time)"/>
	<xsl:variable name="successRate" select="($testCount - $failureCount - $ignoredCount) div $testCount"/>
	<xsl:if test="$ignoredCount &gt; 0">
	Ignored tests:
		<xsl:apply-templates select="//test-case[@executed='False']" mode="textIgnored"/>
	</xsl:if>
	<xsl:if test="$failureCount &gt; 0">
	Failed tests:
		<xsl:apply-templates select="//test-case[@success='False']" mode="textFailed"/>
	</xsl:if>
	Tests  Failures  Ignored  Success Rate  Time
	-----------------------------------------------------------------------
	<xsl:call-template name="format-number">
		<xsl:with-param name="number" select="$testCount"/>
		<xsl:with-param name="length" select="'     '"/>
	</xsl:call-template><xsl:text>  </xsl:text>
	<xsl:call-template name="format-number">
		<xsl:with-param name="number" select="$failureCount"/>
		<xsl:with-param name="length" select="'        '"/>
	</xsl:call-template><xsl:text>  </xsl:text>
	<xsl:call-template name="format-number">
		<xsl:with-param name="number" select="$ignoredCount"/>
		<xsl:with-param name="length" select="'       '"/>
	</xsl:call-template><xsl:text>  </xsl:text>
	<xsl:call-template name="format-number">
		<xsl:with-param name="number" select="format-number($successRate,'0.00%')"/>
		<xsl:with-param name="length" select="'        '"/>
	</xsl:call-template><xsl:text>      </xsl:text>
	<xsl:value-of select="format-number($timeCount,'0.000')"/><xsl:text>s (</xsl:text>
	<xsl:value-of select="floor($timeCount div 3600)"/>h<xsl:text> </xsl:text>
	<xsl:value-of select="floor($timeCount div 60)"/>m<xsl:text> </xsl:text>
	<xsl:value-of select="floor($timeCount mod 60)"/>s)
</xsl:template>

	<xsl:template match="test-case" mode="textFailed">
		<xsl:value-of select="@name"/><xsl:text>
		</xsl:text>
	</xsl:template>

	<xsl:template match="test-case" mode="textIgnored">
		<xsl:value-of select="@name"/><xsl:text> (</xsl:text>
		<xsl:value-of select="reason/message"/>
		<xsl:text>)
		</xsl:text>
	</xsl:template>

	<xsl:template name="format-number">
		<xsl:param name="number"/>
		<xsl:param name="length"/>
		<xsl:variable name="concat" select="concat($length, $number)"/>
		<xsl:value-of select="substring($concat, string-length($concat) - string-length($length) + 1)"/>
	</xsl:template>
</xsl:stylesheet>
