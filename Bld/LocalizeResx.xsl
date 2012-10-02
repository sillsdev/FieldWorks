<?xml version='1.0'?>

<!--
	$Id: LocalizeResx.xsl,v 1.10 2006/12/04 22:41:39 mayhewn Exp $

	Insert translated messages into a resource file

	Neil Mayhew - 11 Mar 2005
 -->

 <xsl:stylesheet version='1.0' xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

	<xsl:output method="xml" encoding="utf-8"/>

	<!--xsl:strip-space elements="root"/-->

	<xsl:param name="lang">en</xsl:param>
	<xsl:param name="verbose">false</xsl:param>

	<!-- Cache translations in a variable -->
	<xsl:variable name="translations" select="document(concat('../output/',$lang,'.xml'))"/>

	<!-- Lookup key for translations -->
	<xsl:key name="msg-from-key" match="msg" use="key"/>

	<!-- Copy input to output, by default -->
	<xsl:template match="@*|node()">
		<xsl:copy>
			<xsl:apply-templates select="@*|node()"/>
		</xsl:copy>
	</xsl:template>

	<!-- Translate the specific nodes we are interested in -->
	<xsl:template match="data[not(@type) and not(@mimetype) and
							  not(starts-with(@name,'&gt;&gt;')) and
							  not(substring-after(@name,'.') = 'Name') and
							  not(substring-after(@name,'.') = 'AccessibleName') and
							  not(substring-after(@name,'.') = 'AccessibleDescription')
							  ]/value[. != '']">
		<xsl:copy>
			<xsl:variable name="key" select="."/>
			<xsl:variable name="str">
				<!-- XSLT idiom for looking up a key in another document -->
				<xsl:for-each select="$translations">
					<xsl:value-of select="key('msg-from-key', $key)/str"/>
				</xsl:for-each>
			</xsl:variable>
			<!-- Leave the key unchanged if it has no translation -->
			<xsl:if test="$str = ''">
				<!-- Check whether this is an error -->
				<xsl:variable name="name" select="../@name"/>
				<xsl:if test="
						not(starts-with($name, '&gt;&gt;'))
					and not(starts-with($name, '$this.'))
					and $key != '-'
					and $key != ''">
					<xsl:if test="$verbose = 'true'">
						<xsl:message terminate="no">
						   <xsl:text>No translation for </xsl:text><xsl:value-of select="$key"/>
						</xsl:message>
					</xsl:if>
				</xsl:if>
				<xsl:value-of select="$key"/>
			</xsl:if>
			<!-- Insert translation (may be empty) -->
			<xsl:value-of select="$str"/>
		</xsl:copy>
	</xsl:template>

	<!-- Ignore nodes that we don't want carried across -->
	<!-- xsl:template match="assembly"/ -->
	<xsl:template match="metadata"/>
	<!-- xsl:template match="data[@mimetype or
							  @type or
							  starts-with(@name,'&gt;&gt;') or
							  substring-after(@name,'.') = 'Name' or
							  substring-after(@name,'.') = 'AccessibleName' or
							  substring-after(@name,'.') = 'AccessibleDescription'
							  ]"/ -->

</xsl:stylesheet>
