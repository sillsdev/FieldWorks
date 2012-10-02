<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="xml" version="1.0" encoding="UTF-8" indent="yes"/>
  <xsl:param name="prmiNumber">6</xsl:param>
  <xsl:param name="prmsNumber">six</xsl:param>
  <xsl:template match="/root">
	<html>
	  <p>There are <xsl:value-of select="$prmsNumber"/> items.</p>
	  <p>
		<xsl:choose>
		  <xsl:when test="$prmiNumber!='6'">The above is wrong.</xsl:when>
		  <xsl:otherwise>The above is correct.</xsl:otherwise>
		</xsl:choose>
	  </p>
	</html>
  </xsl:template>
</xsl:stylesheet>
