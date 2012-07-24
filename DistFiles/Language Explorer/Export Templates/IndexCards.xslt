 <xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
 <xsl:output method="html" indent="yes"/>
 <xsl:preserve-space elements="*"/>

	<!-- set debug to "true" to turn on debug text-->
	  <xsl:variable name="debug" select="'false'"/>

	<!-- Note that there are several places where you see <xsl:text> tags and these are there in case you want to "label" fields-->

	<!-- in Kwaya custom 3 is the plural form -->
	<!-- if you do not want any special formatting of custom fields >
	<then set it to some big number (more than the number of custom fields) -->

	<!-- Note that we expect A4 paper and .25in or 0.6cm margins. Seems to Print best in Internet Explorer. -->

   <!-- Key field formatting-->
	  <xsl:variable name="headword-fmt" select="'bigbold-left'"/>
	  <xsl:variable name="lexeme-fmt" select="'bigbold-left'"/>
	  <xsl:variable name="citation-fmt" select="'bigbold-left'"/>
	  <xsl:variable name="gloss-fmt" select="'Cregular-left'"/>
	  <xsl:variable name="name-fmt" select="'Cregular-left'"/> <!-- currently all names are handled the same-->
	  <xsl:variable name="abbr-fmt" select="'Cregular-left'"/>  <!-- currently all abbreviations are handled the same-->
	  <xsl:variable name="pluralForm-fmt" select="'Cbigbold-left'"/>
	  <xsl:variable name="cvPattern-fmt" select="'Csmall-right'"/>

	<!-- Custom Field Formatting -->

	  <xsl:variable name="custom-a" select="''"/>
	  <xsl:variable name="custom-a-fmt" select="'Csmall-right'"/>
	  <xsl:variable name="custom-b" select="'1'"/>
	  <xsl:variable name="custom-b-fmt" select="'Csmall-right'"/>
	  <xsl:variable name="custom-c" select="'2'"/>
	  <xsl:variable name="custom-c-fmt" select="'Csmall-right'"/>
	  <xsl:variable name="custom-d" select="'3'"/>
	  <xsl:variable name="custom-d-fmt" select="'Cbigbold-left'"/>
	  <xsl:variable name="custom-e" select="'4'"/>
	  <xsl:variable name="custom-e-fmt" select="'Csmall-right'"/>
	  <xsl:variable name="custom-f" select="'400'"/>
	  <xsl:variable name="custom-f-fmt" select="'regular-right'"/>
	  <xsl:variable name="custom-g" select="'400'"/>
	  <xsl:variable name="custom-g-fmt" select="'regular-right'"/>
	  <xsl:variable name="custom-h" select="'400'"/>
	  <xsl:variable name="custom-h-fmt" select="'regular-right'"/>

	<xsl:template name="debugoutput">
	  <xsl:param name="temp-name"/>
	  <xsl:if test="$debug = 'true'">
		<xsl:text>Inside </xsl:text>
		<xsl:value-of select="$temp-name"/>
		<xsl:text>&#32;</xsl:text>
	  </xsl:if>
	</xsl:template>

	<xsl:template match="/">
		<html>
			<head>
		<style type="text/css">
									  .bigbold-right {text-align: right; font-family: 'arial'; font-size: 24px; width=100%;}
									  .regular-right {text-align: right; font-family: 'arial'; font-size: 12px; width=100%;}
									  .small-right {text-align: right; font-family: 'arial'; font-size: 10px; width=100%;}
									  .bigbold-left {text-align: left; font-family: 'arial'; font-size: 24px; width=100%;}
									  .regular-left {text-align: left; font-family: 'arial'; font-size: 12px; width=100%;}
									  .small-left {text-align: left; font-family: 'arial'; font-size: 10px; width=100%;}
									  .Cbigbold-right { /* margin-bottom:1em; */ text-align: right; font-family: 'arial'; font-size: 24px; width=100%;}
									  .Cregular-right { /* margin-bottom:1em; */ text-align: right; font-family: 'arial'; font-size: 12px; width=100%;}
									  .Csmall-right   { /* margin-bottom:1em; */ text-align: right; font-family: 'arial'; font-size: 10px; width=100%;}
									  .Cbigbold-left  { /* margin-bottom:.5em; */ text-align: left; font-family: 'arial'; font-size: 24px; width=100%;}
									  .Cregular-left  { margin-top:1em; text-align: left; font-family: 'arial'; font-size: 12px; width=100%;}
									  .Csmall-left    { /* margin-top:1em; */ text-align: left; font-family: 'arial'; font-size: 10px; width=100%;}
					  td {width: 50%; height: 266px; vertical-align: top; padding-right: 20px; padding-left: 20px;}
				  p {margin:0; font-size: 12px; font-family: 'times new roman'; width=100%}
		</style>
			</head>

			<body>
			   <table width="100%">
					<xsl:for-each select="flex-configured-lexicon">

		<!-- Put the header row in for the mail merge to use as the name-->
			<xsl:for-each select="headword">
							 <xsl:if test="(count(preceding-sibling::*)mod 2) = 0">
		   <xsl:call-template name="headword-row"/>
							 </xsl:if>
		  </xsl:for-each>
					</xsl:for-each>
				 </table>
			</body>
		</html>
	</xsl:template>

   <xsl:template name="headword-row">
	  <tr>
		  <xsl:apply-templates select="."/>
		  <xsl:apply-templates select="following-sibling::headword[1]"/>
	  </tr>
   </xsl:template>

   <xsl:template match="headword">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
				  <td>
		  <p class="{$headword-fmt}">
			<xsl:text> </xsl:text>
					   <xsl:value-of select="@name"/>
		  </p>
					<xsl:apply-templates/>
	</td>
	</xsl:template>

   <xsl:template match="pluralForm">
	 <xsl:call-template name="debugoutput">
	   <xsl:with-param name="temp-name" select="name(.)"/>
	 </xsl:call-template>
	 <p class="{$pluralForm-fmt}">
	   <xsl:text> </xsl:text>
	   <xsl:value-of select="@name"/>
	 </p>
   </xsl:template>

   <xsl:template match="cvPattern">
	 <xsl:call-template name="debugoutput">
	   <xsl:with-param name="temp-name" select="name(.)"/>
	 </xsl:call-template>
	 <p class="{$cvPattern-fmt}">
	   <xsl:text> </xsl:text>
	   <xsl:value-of select="@name"/>
	 </p>
   </xsl:template>

   <xsl:template match="lexeme">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
		  <p class="{$lexeme-fmt}">
			<xsl:text> </xsl:text>
					   <xsl:value-of select="."/>
		  </p>
	</xsl:template>

	<xsl:template match="citation-form">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
		  <p class="{$citation-fmt}">
			<xsl:text> </xsl:text>
					   <xsl:value-of select="."/>
		  </p>
	</xsl:template>

	<xsl:template match="subentry">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>sub-</xsl:text>
					<xsl:value-of select="."/>
					<xsl:text>&#13;&#10;</xsl:text>
	</xsl:template>

	<xsl:template match="variant">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>var-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="variant-abbr">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>vabbr-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="entry-crossref">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>xref-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="alt-form">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>alt</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="alt-name">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>alt</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="part-of-speech">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
	  <xsl:apply-templates/>

	</xsl:template>

	<xsl:template match="name">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
	<p class="{$name-fmt}">
			 <xsl:text> </xsl:text>
						<xsl:value-of select="@name"/>
				</p>
	</xsl:template>

	<xsl:template match="abbr">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
	<p class="{$abbr-fmt}">
				<xsl:text> </xsl:text>
			<xsl:value-of select="."/>
				</p>
	</xsl:template>

	<xsl:template match="feature">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
						<xsl:text>feat-</xsl:text>
						<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="fm-to-part-of-speech">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>change- </xsl:text>
	  <xsl:apply-templates/>
   </xsl:template>

	<xsl:template match="from-name">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
						<xsl:text>fm-</xsl:text>
						<xsl:value-of select="."/>
   </xsl:template>

	<xsl:template match="from-abbr">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
						<xsl:text>fmab-</xsl:text>
						<xsl:value-of select="."/>
   </xsl:template>

	<xsl:template match="to-name">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
						<xsl:text>to-</xsl:text>
						<xsl:value-of select="."/>
   </xsl:template>

	<xsl:template match="to-abbr">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
						<xsl:text>toab-</xsl:text>
						<xsl:value-of select="."/>
   </xsl:template>

	<xsl:template match="from-part-of-speech">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>change- </xsl:text>
	  <xsl:apply-templates/>
   </xsl:template>

	<xsl:template match="alt-abbrev">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>alt</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="gloss">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
	<p class="{$gloss-fmt}">
					<xsl:text> </xsl:text>
					<xsl:value-of select="."/>
	</p>
	</xsl:template>

	<xsl:template match="definition">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text> </xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>


	<xsl:template match="example">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text> </xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>


	<xsl:template match="sem-domain-abbr">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>sem-do-ab-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="sem-domain">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>sem-do-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="anth-code">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>anth-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="anth-ab-">
					<xsl:text>sem-do-ab-</xsl:text>
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="domain-abbr">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>do-ab-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="domain">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>do-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="usage-abbr">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>use-ab-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="usage">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>use-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="status-abbr">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>sts-ab-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="status">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>sts-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="translation">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>trans-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="enc-info">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>enc-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="restrictions">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>res-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="anthro-note">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>anth-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="bibliography">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>bib-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="discourse-note">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>disc-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="phonology-note">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>pho-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="semantic-note">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>sem-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="socio-note">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>socio-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="literal">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>lit-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="comment">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>cmt-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="sci-name">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>sci-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="source">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>src-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="lex-function">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>lfun-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="lex-function-abbr">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>lfun-ab-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="lex-function-rev-abbr">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>lfunr-ab-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="lex-function-rev">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>lfrev-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="lex-ref">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>lexref-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="etymology-link">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>et-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="etymology-abbr">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>et-ab-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="etymology">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>ety-</xsl:text>
					<xsl:value-of select="etymology/form"/>
					<xsl:text>gl-</xsl:text>
					<xsl:value-of select="etymology/gloss"/>
					<xsl:text>cmt-</xsl:text>
					<xsl:value-of select="etymology/comment"/>
	</xsl:template>

	<!--xsl:template match="IntPicturePath">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>Ipic-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template-->

	<!--xsl:template match="ExtPicturePath">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>epic-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template-->

	<!--xsl:template match="caption">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>cap-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template-->

	<xsl:template match="date-created">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>cdate-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="date-mod">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>mdate-</xsl:text>
					<xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="custom">
			 <xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>

	<xsl:choose>
	<xsl:when test="@num=$custom-a">
		  <p class="{$custom-a-fmt}">
			<xsl:text> </xsl:text>
					   <xsl:value-of select="."/>
		  </p>
	</xsl:when>
	<xsl:when test="@num=$custom-b">
		  <p class="{$custom-b-fmt}">
			<xsl:text> </xsl:text>
					   <xsl:value-of select="."/>
		  </p>
	</xsl:when>
	<xsl:when test="@num=$custom-c">
		  <p class="{$custom-c-fmt}">
			<xsl:text> </xsl:text>
					   <xsl:value-of select="."/>
		  </p>
	</xsl:when>
	<xsl:when test="@num=$custom-d">
		  <p class="{$custom-d-fmt}">
			<xsl:text> </xsl:text>
					   <xsl:value-of select="."/>
		  </p>
	</xsl:when>
	<xsl:when test="@num=$custom-e">
		  <p class="{$custom-e-fmt}">
			<xsl:text> </xsl:text>
					   <xsl:value-of select="."/>
		  </p>
	</xsl:when>
	<xsl:when test="@num=$custom-f">
		  <p class="{$custom-f-fmt}">
			<xsl:text> </xsl:text>
					   <xsl:value-of select="."/>
		  </p>
	</xsl:when>
	<xsl:when test="@num=$custom-g">
		  <p class="{$custom-g-fmt}">
			<xsl:text> </xsl:text>
					   <xsl:value-of select="."/>
		  </p>
	</xsl:when>
	<xsl:when test="@num=$custom-h">
		  <p class="{$custom-h-fmt}">
			<xsl:text> </xsl:text>
					   <xsl:value-of select="."/>
		  </p>
	</xsl:when>
	<xsl:otherwise>
			<xsl:text> </xsl:text>
					   <xsl:value-of select="."/>
	</xsl:otherwise>
	</xsl:choose>
	</xsl:template>

	<xsl:template match="homograph">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text>(hom #</xsl:text>
					<xsl:value-of select="."/>
					 <xsl:text>)</xsl:text>
	</xsl:template>

	<xsl:template match="sense">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
					<xsl:text> </xsl:text>
					<!--<xsl:value-of select="@num"/>-->
	  <xsl:apply-templates/>
	</xsl:template>

	<xsl:template match="variant-type">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
			 <xsl:text>vt-</xsl:text>
			 <xsl:value-of select="."/>
	</xsl:template>
	<xsl:template match="variant-type-abbr">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
			 <xsl:text>vt-ab-</xsl:text>
			 <xsl:value-of select="."/>
	</xsl:template>
	<xsl:template match="complex-form-type">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
			 <xsl:text>ct-</xsl:text>
			 <xsl:value-of select="."/>
	</xsl:template>
	<xsl:template match="complex-form-type-abbr">
			<xsl:call-template name="debugoutput">
				<xsl:with-param name="temp-name" select="name(.)"/>
			 </xsl:call-template>
			 <xsl:text>ct-ab-</xsl:text>
			 <xsl:value-of select="."/>
	</xsl:template>

	<xsl:template match="*">
	  <xsl:value-of select="."/>
					<xsl:text>is NOT DeltWith</xsl:text>
	</xsl:template>

</xsl:stylesheet>
