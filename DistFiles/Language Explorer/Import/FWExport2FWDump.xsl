<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="2.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<!--  <xsl:output method="xml" version="1.0" encoding="UTF-8" indent="yes"/> -->

  <!-- Transform from FLEx XML to FLEx LL import XML -->

<!-- DOCUMENT LEVEL -->
  <xsl:template match="/document">
	<xsl:element name="FwDatabase">
	  <xsl:element name="LangProject">

		<!-- Read all the interlinear-text elements and pull out the Annotation info and group in a single Annotations6001 element -->
		<xsl:element name="Annotations6001">
		  <xsl:for-each select="interlinear-text">
			  <xsl:call-template name="DoParagraphAnnotations"/>
		  </xsl:for-each>
		</xsl:element>

		<!-- Read all the interlinear-text elements and pull out the Texts info and group in a single Texts6001 element  -->
		<xsl:element name="Texts6001">
		  <xsl:for-each select="interlinear-text">
			  <xsl:call-template name="DoTextsProcessing"/>
		  </xsl:for-each>
		</xsl:element>
	  </xsl:element>
	</xsl:element>
  </xsl:template>

  <!-- Process the paragraph elements (they all go into the Annotations6001 element -->
  <xsl:template name="DoParagraphAnnotations">
	<xsl:variable name="ITPos" select="position()"/>
	<xsl:for-each select="paragraphs/paragraph">
	  <!-- Create a paragraph id of the form IT1.P1, IT1.P2, IT3.P1, etc. -->
	  <xsl:variable name="paragraphPOS" select="position()"/>
	  <xsl:variable name="ParagraphID">
		<xsl:value-of select="concat('IT', $ITPos)"/>
		<xsl:text>.</xsl:text>
		<xsl:value-of select="concat('P',position())"/>
	  </xsl:variable>
	  <xsl:for-each select="phrases/phrase">
		<!-- Create the internal Ref variable: if the @ref exists for this phrase it will be that value, otherwise it will be a unique value. -->
		<xsl:variable name="ref">
		  <xsl:choose>
			<xsl:when test="@ref">
			  <xsl:text>PH</xsl:text>
			  <xsl:value-of select="$ITPos"/>
			  <xsl:value-of select="@ref"/>
			</xsl:when>
			<xsl:otherwise>
			  <xsl:text>PH</xsl:text>
			  <xsl:value-of select="$ITPos"/>
			  <xsl:text>_</xsl:text>
			  <!--xsl:value-of select="$paragraphsPOS"/>
			  <xsl:text>_</xsl:text> -->
			  <xsl:value-of select="$paragraphPOS"/>
			  <xsl:text>_</xsl:text>
			  <xsl:value-of select="position()"/>
			</xsl:otherwise>
		  </xsl:choose>
		</xsl:variable>

		<CmBaseAnnotation>
		  <!-- Only four values change for different segments. -->
		  <!-- The id needs to be unique for each one (e.g., BA1, BA2, etc.). -->
		  <!-- *1* For now, using the \ref line from the SFM text for this -->
		  <!-- Better to not depend on that. Better to generate a count, but
			haven't figured out how to keep it from starting over for each paragraph -->
		  <xsl:attribute name="id">
			<xsl:value-of select="$ref"/>
		  </xsl:attribute>
		  <BeginObject37>
			<Link class="StTxtPara">
			  <!-- *2* The two Link targets need to refer to the StTxtPara id that owns the segment. -->
			  <xsl:attribute name="target">
				<xsl:value-of select="$ParagraphID"/>
			  </xsl:attribute>
			</Link>
		  </BeginObject37>
		  <EndObject37>
			<Link class="StTxtPara">
			  <!-- *3* The two Link targets need to refer to the StTxtPara id that owns the segment. -->
			  <xsl:attribute name="target">
				<xsl:value-of select="$ParagraphID"/>
			  </xsl:attribute>
			</Link>
		  </EndObject37>
		  <Flid37>
			<Integer val="16002" />
		  </Flid37>
		  <BeginOffset37>
			<Integer>
			  <!-- *4* The BeginOffset integer value needs to be in the sequence
				of the segments within the paragraph (e.g., 1, 2, 3). -->
			  <xsl:attribute name="val">
				<xsl:value-of select="position()"/>
			  </xsl:attribute>
			</Integer>
		  </BeginOffset37>
		  <AnnotationType34>
			<Link ws="en" name="Text Segment" />
		  </AnnotationType34>
		  <CompDetails34>
			<Uni>LLImport</Uni>
		  </CompDetails34>
		</CmBaseAnnotation>

<!-- Each Free Translation has a CmIndirectAnnotation similar to this. -->

		<xsl:if test="item[@type='gls']">
		  <CmIndirectAnnotation>
			<AppliesTo36>
			  <Link class="Segment">
<!-- The two values that change are the Link target and the Run. -->
<!-- *1* The Link target needs to match the CmBaseAnnotation corresponding to the segment using this translation. -->
				<xsl:attribute name="target">
				  <xsl:value-of select="$ref"/>
				</xsl:attribute>
			  </Link>
			</AppliesTo36>
			<AnnotationType34>
			  <Link ws="en" name="Free Translation" />
			</AnnotationType34>
			<Comment34>
<!-- If there are translations in other writing systems, repeat the AStr and Run
element using the appropriate writing system code. -->
			  <xsl:for-each select="item[@type='gls']">
				<AStr>
				  <xsl:attribute name="ws">
					<xsl:value-of select="@lang"/>
				  </xsl:attribute>
<!-- *2* The Run contains the text of the free translation. -->
				  <Run>
					<xsl:attribute name="ws">
					  <xsl:value-of select="@lang"/>
					</xsl:attribute>
					<xsl:value-of select="."/>
				  </Run>
				</AStr>
			  </xsl:for-each>
			</Comment34>
			<CompDetails34>
			  <Uni>LLImport</Uni>
			</CompDetails34>
		  </CmIndirectAnnotation>
		</xsl:if>

<!-- A literal translation is the same as a free translation except the AnnotationType link name is "Literal Translation" instead of "Free Translation". -->

		<xsl:if test="item[@type='lit']">
		  <CmIndirectAnnotation>
			<AppliesTo36>
			  <Link class="Segment">
				<xsl:attribute name="target">
				  <xsl:value-of select="$ref"/>
				</xsl:attribute>
			  </Link>
			</AppliesTo36>
			<AnnotationType34>
			  <Link ws="en" name="Literal Translation" />
			</AnnotationType34>
			<Comment34>
			  <xsl:for-each select="item[@type='lit']">
				<AStr>
				  <xsl:attribute name="ws">
					<xsl:value-of select="@lang"/>
				  </xsl:attribute>
				  <Run>
					<xsl:attribute name="ws">
					  <xsl:value-of select="@lang"/>
					</xsl:attribute>
					<xsl:value-of select="."/>
				  </Run>
				</AStr>
			  </xsl:for-each>
			</Comment34>
			<CompDetails34>
			  <Uni>LLImport</Uni>
			</CompDetails34>
		  </CmIndirectAnnotation>
		</xsl:if>

<!-- You can also include Note fields by using "Note" instead of "Free Translation". -->

		<xsl:if test="item[@type='note']">
		  <CmIndirectAnnotation>
			<AppliesTo36>
			  <Link class="Segment">
				<xsl:attribute name="target">
				  <xsl:value-of select="$ref"/>
				</xsl:attribute>
			  </Link>
			</AppliesTo36>
			<AnnotationType34>
			  <Link ws="en" name="Note" />
			</AnnotationType34>
			<Comment34>
			  <xsl:for-each select="item[@type='note']">
				<AStr>
				  <xsl:attribute name="ws">
					<xsl:value-of select="@lang"/>
				  </xsl:attribute>
				  <Run>
					<xsl:attribute name="ws">
					  <xsl:value-of select="@lang"/>
					</xsl:attribute>
					<xsl:value-of select="."/>
				  </Run>
				</AStr>
			  </xsl:for-each>
			</Comment34>
			<CompDetails34>
			  <Uni>LLImport</Uni>
			</CompDetails34>
		  </CmIndirectAnnotation>
		</xsl:if>

	  </xsl:for-each>
	</xsl:for-each>
	<!--/Annotations6001-->
	</xsl:template>

  <!-- Process the Texts6001 elements -->
  <xsl:template name="DoTextsProcessing">
  <!-- Texts6001 -->

	<!-- Text interlinear text documents ... -->
	<!-- The various fields are optional, but normally you would want at least a Name (title) and one or more paragraphs. (The Source and Abbreviation seem to not work. This is a bug.) -->
	<xsl:variable name="ITPos" select="position()"/>
	<xsl:for-each select="paragraphs">
	  <Text>
		<xsl:if test="../item[@type='title']">
		  <Name5>
			<xsl:for-each select="../item[@type='title']">
			  <AUni>
				<xsl:attribute name="ws">
				  <xsl:value-of select="@lang"/>
				</xsl:attribute>
				<xsl:value-of select="."/>
			  </AUni>
			</xsl:for-each>
		  </Name5>
		</xsl:if>

		<xsl:if test="../item[@type='title-abbreviation']">
		  <Abbreviation5054>
			<xsl:for-each select="../item[@type='title-abbreviation']">
			  <AUni>
				<xsl:attribute name="ws">
				  <xsl:value-of select="@lang"/>
				</xsl:attribute>
				<xsl:value-of select="."/>
			  </AUni>
			</xsl:for-each>
		  </Abbreviation5054>
		</xsl:if>

		<xsl:if test="../item[@type='source']">
		  <Source5054>
			<xsl:for-each select="../item[@type='source']">
			  <AStr>
				<xsl:attribute name="ws">
				  <xsl:value-of select="@lang"/>
				</xsl:attribute>
				<Run>
				  <xsl:attribute name="ws">
					<xsl:value-of select="@lang"/>
				  </xsl:attribute>
				  <xsl:value-of select="."/>
				</Run>
			  </AStr>
			</xsl:for-each>
		  </Source5054>
		</xsl:if>

		<xsl:if test="../item[@type='description']">
		  <Description5>
			<xsl:for-each select="../item[@type='description']">
			  <AStr>
				<xsl:attribute name="ws">
				  <xsl:value-of select="@lang"/>
				</xsl:attribute>
				<Run>
				  <xsl:attribute name="ws">
					<xsl:value-of select="@lang"/>
				  </xsl:attribute>
				  <xsl:value-of select="."/>
				</Run>
			  </AStr>
			</xsl:for-each>
		  </Description5>
		</xsl:if>

	  <Contents5054>
		<StText>
		  <Paragraphs14>
			<!--      ...StTxtPara paragraph elements... -->
			<!-- The paragraph holding the baseline of the text is in this format: -->
			<xsl:for-each select="paragraph">
			  <!-- Create a paragraph id of the form P1, P2, P3, etc. -->
			  <!-- xsl:variable name="ParagraphID" select="concat('P',position())"/ -->
			  <!-- Create a paragraph id of the form P1, P2, P3, etc. -->
			  <xsl:variable name="paragraphPOS" select="position()"/>
			  <xsl:variable name="ParagraphID">
				<xsl:value-of select="concat('IT', $ITPos)"/>
				<xsl:text>.</xsl:text>
				<xsl:value-of select="concat('P',position())"/>
			  </xsl:variable>
			  <StTxtPara>
				<!-- *1* Each StTxtPara needs a unique id (e.g., P1, P2, etc.) -->
				<xsl:attribute name="id">
				  <xsl:value-of select="$ParagraphID"/>
				</xsl:attribute>
				<Contents16>
				  <Str>
					<xsl:for-each select="phrases/phrase[words/word]">
					  <!-- *2* Each segment is in a Run element -->
					  <Run>
						<xsl:attribute name="ws">
						  <xsl:value-of select="words/word[1]/item[@type='txt' or @type='punct']/@lang"/>
						</xsl:attribute>
						<xsl:for-each select="words/word/item[@type='txt' or @type='punct']">
							<!-- Except at the end of a segment, change any occurrence of the characters .?! to ;
							to avoid generation of unwanted line breaks in FLEx. Note that the first . in the
							translate function is not a period, but the value of the current element -->

						  <!-- LT-10363 can't just process all the words now: have to create new runs when new lang's are used-->
						  <!-- Determine if the current run should be closed and a new one started:
						  if it's not the first one AND the @lang is different, start a new one. -->
						  <xsl:variable name="pos" select="position()"/>
						  <xsl:variable name="lang" select="../preceding-sibling::word/item[1]/@lang"/>
						  <!-- xsl:variable name="langX" select="../preceding::word/item/@lang[1]"/ -->
						  <xsl:variable name="lang2" select="$lang[last()]"/>
						  <!--xsl:comment>- <xsl:value-of select="$pos"/> - <xsl:value-of select="$lang"/></xsl:comment>
						  <xsl:comment>- <xsl:value-of select="$lang2"/> - </xsl:comment-->
						  <xsl:if test="not(position() = 1) and @lang!=$lang[last()]">
							<xsl:text disable-output-escaping="yes">&lt;/Run&gt;</xsl:text>
							<xsl:text disable-output-escaping="yes">&lt;Run ws=&quot;</xsl:text>
							<xsl:value-of select="@lang"/>
							<xsl:text disable-output-escaping="yes">&quot;&gt;</xsl:text>
						  </xsl:if>
						  <xsl:choose>
							<xsl:when test="not(position()=last())">
							  <xsl:value-of select="translate(.,'.?!§',';;;;')"/>
							</xsl:when>
							<xsl:otherwise>
							  <xsl:value-of select="."/>
							</xsl:otherwise>
						  </xsl:choose>
						  <xsl:variable name="nextContent"><xsl:value-of select="../following-sibling::word[1]/item[@type='punct']"/></xsl:variable>
						  <!--xsl:comment><xsl:text>NextContent=[</xsl:text><xsl:value-of select="$nextContent"/>]</xsl:comment-->
							<!-- xsl:if test="(not(position()=last()) and not(../following-sibling::word[1]/item[@type='punct']) and position()!=1) or (position()=1 and @type!='punct')" -->
						  <xsl:if test="(not(position()=last()) and not(../following-sibling::word[1]/item[@type='punct'] and ($nextContent != '(' and $nextContent != '[' and $nextContent != '&lt;' and $nextContent != '‘' and $nextContent != '“' and $nextContent != '«' and $nextContent != '{')) and position()!=1) or (position()=1 and @type!='punct')">
								<xsl:if test=". != '[' and . != '(' and . != '&lt;' and . != '‘' and . != '“' and . != '«' and . != '{' ">
							  <xsl:text> </xsl:text>
							  </xsl:if>
							</xsl:if>
						</xsl:for-each>
					  </Run>
					  <!-- followed by a second Run element if it is not the end of the paragraph. -->
					  <!-- The second Run should always contain a space. -->
					  <xsl:if test="not(position()=last())">
						<Run>
						  <xsl:attribute name="ws">
							<xsl:value-of select="words/word[1]/item/@lang"/>
						  </xsl:attribute>
						  <!-- If the previous segment did not end with period, question mark, or
							exclamation point, a segment marker (U+00A7) should be inserted before the space. -->
						  <xsl:variable name="len" select="string-length(words/word[position()=last()]/item[@type='txt' or @type='punct'])"/>
						  <xsl:variable name="lastChar" select="substring(words/word[position()=last()]/item[@type='txt'or @type='punct'], $len, 1)"/>
						  <xsl:choose>
							<xsl:when test="$lastChar !='.' and $lastChar !='?' and $lastChar !='!'">
							  <xsl:text>&#xA7; </xsl:text>
							</xsl:when>
							<xsl:otherwise>
							  <xsl:text> </xsl:text>
							</xsl:otherwise>
						  </xsl:choose>

						  <!-- The following piece works only in XSLT 2.0 because of the "ends-with" function.
							It has been replaced by the preceding piece for use in XSLT 1.0 -->
						  <!--                          <xsl:if test="not(ends-with(words/word[position()=last()]/item[@type='txt'], '.'))and
							not(ends-with(words/word[position()=last()]/item[@type='txt'], '?'))and
							not(ends-with(words/word[position()=last()]/item[@type='txt'], '!'))">
							<xsl:text>&#x00A7;</xsl:text>
							</xsl:if> -->
						  <!--                          <xsl:text> </xsl:text> -->

						</Run>
					  </xsl:if>
					</xsl:for-each>
				  </Str>
				</Contents16>
			  </StTxtPara>
			</xsl:for-each>
		  </Paragraphs14>
		</StText>
	  </Contents5054>
	</Text>
  <!--/Texts6001-->
	</xsl:for-each>
  </xsl:template>

</xsl:stylesheet>
