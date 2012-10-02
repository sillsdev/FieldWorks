<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="text" version="1.0" encoding="UTF-8" indent="yes"/>
	<xsl:key name="MoStemAllomorph_id" match="MoStemAllomorph" use="@Id"/>
	<!-- Need to create a list of all unique stem name combinations used in lexical entries -->
	<xsl:variable name="sAllStemNamesUsedInLexicalEntries">
		<xsl:for-each select="$LexEntries">
			<xsl:variable name="allos" select="AlternateForms | LexemeForm"/>
			<!-- collect stem names used so we can output any default stem name allomorph properties -->
			<xsl:variable name="stemallos" select="key('MoStemAllomorph_id', $allos/@dst)[@IsAbstract='0' and @MorphType!=$sPhrase and @MorphType!=$sDiscontiguousPhrase and @StemName!='0']"/>
			<xsl:variable name="sStemNamesUsedAll">
				<xsl:for-each select="$stemallos">
					<xsl:sort select="@StemName"/>
					<xsl:text>StemName</xsl:text>
					<xsl:value-of select="@StemName"/>
					<xsl:text>;</xsl:text>
				</xsl:for-each>
			</xsl:variable>
			<xsl:variable name="sStemNamesUsed">
				<xsl:call-template name="OutputUniqueStemNameStrings">
					<xsl:with-param name="sList" select="$sStemNamesUsedAll"/>
				</xsl:call-template>
			</xsl:variable>
			<xsl:if test="string-length($sStemNamesUsed) &gt; 0">
				<xsl:text>Not</xsl:text>
				<xsl:value-of select="$sStemNamesUsed"/>
				<xsl:text>&#x20;</xsl:text>
			</xsl:if>
		</xsl:for-each>
	</xsl:variable>
	<xsl:variable name="sUniqueStemNamesUsedInLexicalEntries">
		<xsl:call-template name="OutputUniqueStrings">
			<xsl:with-param name="sList" select="$sAllStemNamesUsedInLexicalEntries"/>
		</xsl:call-template>
	</xsl:variable>
	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		OutputUniqueStemNameStrings
		Routine to output a list of strings, only using those strings which are unique
		Parameters: sList = list of strings to look at, in determining which are unique
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="OutputUniqueStemNameStrings">
		<xsl:param name="sList"/>
		<xsl:variable name="sNewList" select="normalize-space($sList)"/>
		<xsl:variable name="sFirst" select="substring-before($sNewList,';')"/>
		<xsl:variable name="sRest" select="substring-after($sNewList,';')"/>
		<xsl:if test="not(contains($sRest,concat($sFirst,';')))">
			<xsl:value-of select="translate($sFirst,';','')"/>
		</xsl:if>
		<xsl:if test="$sRest">
			<xsl:call-template name="OutputUniqueStemNameStrings">
				<xsl:with-param name="sList" select="$sRest"/>
			</xsl:call-template>
		</xsl:if>
	</xsl:template>
</xsl:stylesheet>
