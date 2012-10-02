<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

  <xsl:template match="/KB">
	<KB docVersion="{@docVersion}" srcName="{@srcName}" tgtName="{@tgtName}" max="{@max}">
	  <xsl:for-each select="MAP">
		<xsl:sort select="@mn" order="ascending"/>
		<MAP mn="{@mn}">
		  <xsl:for-each select="TU">
			<xsl:sort select="@k" order="ascending"/>
			<TU f="{@f}" k="{@k}">
			  <xsl:for-each select="RS">
				<RS n="{@n}" a="{@a}" />
			  </xsl:for-each>
			</TU>
		  </xsl:for-each>
		</MAP>
	  </xsl:for-each>
	</KB>
  </xsl:template>
</xsl:stylesheet>
