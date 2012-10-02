<?xml version="1.0" encoding="UTF-8"?>
<!-- Transform from SHUtils XML format to FLEx XML -->
<xsl:stylesheet version="2.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
   <xsl:output encoding="UTF-8" indent="yes"/>
  <xsl:param name="id">id</xsl:param>
  <xsl:param name="p">p</xsl:param>
  <xsl:param name="ref">ref</xsl:param>
  <xsl:param name="tx">tx</xsl:param>
  <xsl:param name="wg">wg</xsl:param>
  <xsl:param name="wp">wp</xsl:param>
  <xsl:param name="mb">mb</xsl:param>
  <xsl:param name="ge">ge</xsl:param>
  <xsl:param name="ps">ps</xsl:param>
  <xsl:param name="ft">ft</xsl:param>

<!-- ENTIRE DOCUMENT LEVEL-->
  <xsl:template match="/shoebox">
	<document>
	  <interlinear-text>
		<xsl:apply-templates select="*[name(.)=$id]" mode="id"/>
		<languages>
<!-- The following line makes a list of the language identities found in the SFM code definitions -->
<!-- Though many duplicate language identities occur, the list is stripped down to just one of each -->
		  <xsl:for-each select="/shoebox/shoebox-format/marker[generate-id()=generate-id(key('LANG',language)[1])]">
			<language>
			  <xsl:attribute name="lang">
				<xsl:value-of select="language"/>
			  </xsl:attribute>
			</language>
		  </xsl:for-each>
		</languages>
	  </interlinear-text>
	</document>
  </xsl:template>

  <xsl:key name="LANG" match="/shoebox/shoebox-format/marker" use="language"/>

<!-- INTERLINEAR TEXT and PARAGRAPH LEVEL-->
<!-- ***<xsl:template match="*[name(.)=$id]"> -->
  <xsl:template match="*" mode="id">
   <xsl:if test="name(.)=$id">
	<item type="title">
	  <xsl:attribute name="lang">
		<xsl:value-of select="/shoebox/shoebox-format/marker[@name=$id]/language"/>
	  </xsl:attribute>
	  <xsl:value-of select="@value"/>
	</item>
	<paragraphs>
<!-- Check the position of the first paragraph mark -->
	  <xsl:variable name="next-p" select="*[name(.)=$p][1]"/>
<!-- If the first ("interlinear-block" or "p") element is an "interlinear-block", this -->
<!-- means that the first interlinear block comes before the first paragraph mark.   -->
<!-- Create an extra paragraph to contain all text before the first marked paragraph. -->
<!-- For cases where no paragraphs at all are marked, this will include the entire text -->
	  <xsl:if test="name(interlinear-block[1]|*[name(.)=$p][1])='interlinear-block'">
		<paragraph>
		  <phrases>
			<xsl:apply-templates select="interlinear-block[1]" mode="walker"/>
		  </phrases>
		</paragraph>
	  </xsl:if>
<!-- Begin processing the first marked paragraph here -->
	  <xsl:if test="($next-p)">
		<xsl:apply-templates select="*[name(.)=$p]" mode="walker"/>
	  </xsl:if>
	</paragraphs>
   </xsl:if>
  </xsl:template>

<!-- PARAGRAPH LEVEL-->
<!-- The "Paragraph" markup passed from SHUtils, <p>para</p>, is followed by any number -->
<!-- of <interlinear-block> elements which are all to be taken as part of that paragraph -->
<!-- until another Paragraph markup is encountered. This will be restructured, putting -->
<!-- everything after the Paragraph markup, up until the next one is encountered, between -->
<!-- the start and end tags of a paragraph element.                                     -->

<!-- Note: the "para" content of the "p" element is there just to get sh2xml to pass it -->
<!-- through to the XML. Any other content would serve the same purpose. So don't specify -->
<!-- any particular content here. Just match on the "p" element.                         -->

<!-- ***<xsl:template match="*[name(.)=$p]" mode="walker"> -->
  <xsl:template match="*" mode="walker">
   <xsl:if test="name(.)=$p">
	<paragraph>
	  <phrases>
		<xsl:apply-templates select="following-sibling::interlinear-block[1]" mode="walker"/>
	  </phrases>
	</paragraph>
   </xsl:if>
  </xsl:template>

<!-- SENTENCE LEVEL-->
  <xsl:template match="interlinear-block" mode="walker">
	<!-- If this is an Interlinear-block that contains an ft, then ignore it's processing here -->
	<xsl:variable name="ignore">
	  <xsl:if test="child::*[name()=$tx]/child::*[name()=$ft]">
		<xsl:text>true</xsl:text>
	  </xsl:if>
	</xsl:variable>
	<xsl:if test="$ignore != 'true' ">
	<phrase>
<!-- The following instance of "ref" needs to remain a string, not a parameter -->
<!-- It becomes the name of a phrase attribute in the output XML -->
	  <xsl:attribute name="ref">
		<xsl:value-of select="preceding-sibling::*[name()=$ref][1]"/>
	  </xsl:attribute>
	  <words>
		<xsl:apply-templates select="*[name(.)=$tx]" mode="tx"/>
	  </words>
	  <xsl:variable name="hasFT">
		<xsl:if test="following-sibling::*[position()= 1 and name()=$ft][1] or following-sibling::interlinear-block[1]/child::*[name()=$tx]/child::*[name()=$ft]">
		  <xsl:text>Yes</xsl:text>
		</xsl:if>
	  </xsl:variable>
	  <xsl:if test="$hasFT = 'Yes' ">
		<item type="gls">
		  <xsl:attribute name="lang">
			<xsl:value-of select="/shoebox/shoebox-format/marker[@name=$ft]/language"/>
		  </xsl:attribute>
		  <xsl:choose>
			<xsl:when test="following-sibling::*[position()= 1 and name()=$ft][1]">
			  <xsl:value-of select="following-sibling::*[position()= 1 and name()=$ft][1]"/>
			</xsl:when>
			<xsl:when test="following-sibling::interlinear-block[1]/child::*[name()=$tx]/child::*[name()=$ft]">
			  <xsl:value-of select="following-sibling::interlinear-block[1]/child::*[name()=$tx]/child::*[name()=$ft]"/>
			</xsl:when>
		  </xsl:choose>

		  <!-- xsl:value-of select="following-sibling::*[name()=$ft][1]"/ -->
		</item>
	  </xsl:if>
	</phrase>
	</xsl:if>


<!-- If the next ("interlinear-block" or "p") element is an "interlinear-block" -->
<!-- then go and include this next interlinear block in the current paragraph -->
<!-- Otherwise it's time to end the current paragraph (and this template) -->
	<xsl:if test="name(following-sibling::interlinear-block[1]|following-sibling::*[name(.)=$p][1])='interlinear-block'">
	  <xsl:apply-templates select="following-sibling::interlinear-block[1]" mode="walker"/>
	</xsl:if>
  </xsl:template>

  <!-- Replacement function for the 'replace' function that is available in version 2.0 -->
  <xsl:template name="string-replace-all">
	<xsl:param name="text"/>
	<xsl:param name="replace"/>
	<xsl:param name="by"/>
	<xsl:choose>
	  <xsl:when test="contains($text, $replace)">
		<xsl:value-of select="substring-before($text, $replace)"/>
		<xsl:value-of select="$by"/>
		<xsl:call-template name="string-replace-all">
		  <xsl:with-param name="text" select="substring-after($text, $replace)"/>
		  <xsl:with-param name="replace" select="$replace"/>
		  <xsl:with-param name="by" select="$by"/>
		</xsl:call-template>
	  </xsl:when>
	  <xsl:otherwise>
		<xsl:value-of select="$text"/>
	  </xsl:otherwise>
	</xsl:choose>
  </xsl:template>

  <!-- Replace punctuation from a string and replace with * -->
  <xsl:template name="ReplacePunctuation">
	<xsl:param name="data"/>
		<xsl:call-template name="string-replace-all">
		  <xsl:with-param name="text">
			<xsl:call-template name="string-replace-all">
			<xsl:with-param name="text">
			  <xsl:call-template name="string-replace-all">
			  <xsl:with-param name="text">
				  <xsl:call-template name="string-replace-all">
					<xsl:with-param name="text" select="$data"/>
					<xsl:with-param name="replace" select="'&#xa7;'"/>
					<xsl:with-param name="by" select="'*'"/>
				  </xsl:call-template>
				</xsl:with-param>
				<xsl:with-param name="replace" select="'?'"/>
				<xsl:with-param name="by" select="'*'"/>
			  </xsl:call-template>
			  </xsl:with-param>
			  <xsl:with-param name="replace" select="'!'"/>
			  <xsl:with-param name="by" select="'*'"/>
			</xsl:call-template>
		  </xsl:with-param>
		  <xsl:with-param name="replace" select="'&#x2e;'"/>
		  <xsl:with-param name="by" select="'*'"/>
		  </xsl:call-template>

	<!-- 2.0 versions using Replace and Tokenize - both not allowed in version 1.0 -->
	<!--    <xsl:value-of select="replace(replace(replace(replace($data, '&#xa7;', '*' ), '?', '*'), '!', '*'), '&#x2e;', '*') "/> -->
	<!--
	<xsl:variable name="result">
	  <xsl:for-each select="tokenize($data, '[.!?]') ">
		<xsl:value-of select="."/>
		<xsl:if test="position() != last()">
		  <xsl:text>*</xsl:text>
		</xsl:if>
	  </xsl:for-each>
	</xsl:variable>
	<xsl:value-of select="replace($result, '&#xa7;', '*' )"/>
	-->
  </xsl:template>


<!-- WORD LEVEL-->
<!-- ***<xsl:template match="*[name(.)=$tx]"> -->
  <xsl:template match="*" mode="tx">
   <xsl:if test="name(.)=$tx">
	<word>
	  <item type="txt">
		<xsl:attribute name="lang">
		  <xsl:value-of select="/shoebox/shoebox-format/marker[@name=$tx]/language"/>
		</xsl:attribute>
		<!--
		  -Create a data varialbe to containthe portion of the value attribute that is to have puncuation replaced.
		  -If the Last character is a quote then don't use that character
		  -If it's the last one then don't use the last character.
		-->
		<xsl:variable name="inFromEnd">
		  <xsl:choose>
			<!-- xsl:when test="position() = last() and ends-with(@value, ' &quot; ' )"-->
			<xsl:when test="position() = last() and substring(@value, string-length(@value))  = '&quot;' ">
				<xsl:text>2</xsl:text>
			</xsl:when>
			<xsl:when test="position() = last() or substring(@value, string-length(@value))  = '&quot;' ">
			  <xsl:text>1</xsl:text>
			</xsl:when>
			<xsl:otherwise><xsl:text>0</xsl:text></xsl:otherwise>
		  </xsl:choose>
		</xsl:variable>
		<xsl:variable name="data" select="substring(@value, 1, string-length(@value) - number($inFromEnd) )"/>
		<!-- preform the replacement of the punctuation characters if present -->
		<xsl:variable name="replacedValue">
		  <xsl:call-template name="ReplacePunctuation">
			<xsl:with-param name="data" select="$data"/>
		  </xsl:call-template>
		  <xsl:if test="number($inFromEnd) > 0">
			<xsl:value-of select="substring(@value, string-length(@value) -  number($inFromEnd) +1 )"/>
		  </xsl:if>
		</xsl:variable>
		<xsl:if test="$replacedValue != @value">
		  <!-- add an attribute named modified that has a value of 'Yes' if we've changed the contents of the value -->
		  <xsl:attribute name="modified">
			<xsl:text>Yes</xsl:text>
		  </xsl:attribute>
		</xsl:if>
		<!-- Now put out the value attribute - possibly modified -->
		<xsl:value-of select="$replacedValue"/>
		<!--
		  End of the modified code, used to just put out the value of @value
		-->
	  </item>
	  <xsl:if test="(*[name(.)=$wg])">
		<item type="gls">
		  <xsl:attribute name="lang">
			<xsl:value-of select="/shoebox/shoebox-format/marker[@name=$wg]/language"/>
		  </xsl:attribute>
<!-- The following handles only single word glosses in XSLT 1.0 -->
<!--      <xsl:value-of select="*[name(.)=$wg]"/> -->
<!-- The following handles single or multiple word glosses in both XSLT 1.0 & 2.0-->
		  <xsl:for-each select="*[name(.)=$wg]">
			<xsl:value-of select="."/>
			<xsl:if test="not(position()=last())">
			  <xsl:text> </xsl:text>
			</xsl:if>
		  </xsl:for-each>
		</item>
	  </xsl:if>
	  <xsl:if test="(*[name(.)=$wp])">
		<item type="pos">
		  <xsl:attribute name="lang">
			<xsl:value-of select="/shoebox/shoebox-format/marker[@name=$wp]/language"/>
		  </xsl:attribute>
<!-- The following handles only single word annotations in XSLT 1.0 -->
<!--      <xsl:value-of select="*[name(.)=$wp]"/> -->
<!-- The following handles single or multiple word annotations in both XSLT 1.0 & 2.0-->
		  <xsl:for-each select="*[name(.)=$wp]">
			<xsl:value-of select="."/>
			<xsl:if test="not(position()=last())">
			  <xsl:text> </xsl:text>
			</xsl:if>
		  </xsl:for-each>
		</item>
	  </xsl:if>
	  <morphemes>
		<xsl:apply-templates select="*[name(.)=$mb]" mode="mb"/>
	  </morphemes>
	</word>
   </xsl:if>
  </xsl:template>

<!-- MORPHEME LEVEL-->
<!-- ***<xsl:template match="*[name(.)=$mb]"> -->
  <xsl:template match="*" mode="mb">
   <xsl:if test="name(.)=$mb">
	<morph>
	  <xsl:attribute name="type">
		<xsl:variable name="firstChar" select="substring(@value, 1, 1)"/>
		<xsl:variable name="len" select="string-length(@value)"/>
		<xsl:variable name="lastChar" select="substring(@value, $len, 1)"/>
		<xsl:if test="$firstChar !='-' and $lastChar !='-'">root</xsl:if>
		<xsl:if test="$firstChar !='-' and $lastChar  ='-'">prefix</xsl:if>
		<xsl:if test="$firstChar  ='-' and $lastChar !='-'">suffix</xsl:if>
		<xsl:if test="$firstChar  ='-' and $lastChar  ='-'">infix</xsl:if>
	  </xsl:attribute>
	  <item type="txt">
		<xsl:attribute name="lang">
		  <xsl:value-of select="/shoebox/shoebox-format/marker[@name=$mb]/language"/>
		</xsl:attribute>
<!-- Allow the text to come from either a node or an attribute -->
<!-- This permits some flexibility for the CVWATAAM text which allows it to display, but actually skews the data -->
<!-- May want to remove this flexibility and return to the single line that's commented out -->
		<xsl:if test="(@value)">
		  <xsl:value-of select="@value"/>
		</xsl:if>
		<xsl:if test="not(@value)">
		  <xsl:value-of select="."/>
		</xsl:if>
<!--    <xsl:value-of select="@value"/> -->
<!-- Allow the text to come from either a node or an attribute -->
	  </item>
	  <xsl:if test="(*[name(.)=$ge])">
		<item type="gls">
		  <xsl:attribute name="lang">
			<xsl:value-of select="/shoebox/shoebox-format/marker[@name=$ge]/language"/>
		  </xsl:attribute>
<!-- The following handles only single word glosses in XSLT 1.0 -->
<!--      <xsl:value-of select="*[name(.)=$ge]"/> -->
<!-- The following handles single or multiple word glosses in both XSLT 1.0 & 2.0-->
		  <xsl:for-each select="*[name(.)=$ge]">
			<xsl:value-of select="."/>
			<xsl:if test="not(position()=last())">
			  <xsl:text> </xsl:text>
			</xsl:if>
		  </xsl:for-each>
		</item>
	  </xsl:if>
	  <xsl:if test="(*[name(.)=$ps])">
		<item type="msa">
		  <xsl:attribute name="lang">
			<xsl:value-of select="/shoebox/shoebox-format/marker[@name=$ps]/language"/>
		  </xsl:attribute>
<!-- The following handles only single word annotations in XSLT 1.0 -->
<!--      <xsl:value-of select="*[name(.)=$ps]"/> -->
<!-- The following handles single or multiple word annotations in both XSLT 1.0 & 2.0-->
		  <xsl:for-each select="*[name(.)=$ps]">
			<xsl:value-of select="."/>
			<xsl:if test="not(position()=last())">
			  <xsl:text> </xsl:text>
			</xsl:if>
		  </xsl:for-each>
		</item>
	  </xsl:if>
	</morph>
   </xsl:if>
  </xsl:template>

</xsl:stylesheet>
