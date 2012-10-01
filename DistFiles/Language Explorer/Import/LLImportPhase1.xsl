<?xml version="1.0"?>
<xsl:transform xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
   <xsl:output encoding="UTF-8" indent="yes"/>

   <!-- Add this change to this phase so that the for-each will work in the next phase with the new name -->
   <xsl:template match="LexicalDatabase6001">
	  <LexDb6001>
		 <xsl:copy-of select="@*"/>
		 <xsl:apply-templates/>
	  </LexDb6001>
   </xsl:template>

   <!-- Remove duplicate morphemes from lexical entries -->

   <xsl:template name="lexeme-form">
	  <xsl:if test="Allomorphs5002//Form5035">
		 <LexemeForm5002>
			<xsl:element name="{name(Allomorphs5002/*[1])}">
			   <xsl:for-each select="Allomorphs5002/*[1]/@*">
				  <xsl:copy/>
			   </xsl:for-each>
			   <xsl:for-each select="Allomorphs5002/*[1]/node()">
				  <xsl:copy-of select="."/>
			   </xsl:for-each>
			   <xsl:if test="not (Allomorphs5002/*[1]/MorphType5035)">
				  <MorphType5035>
					 <Link ws="en" abbr="ubd stm" name="stem"/>
				  </MorphType5035>
			   </xsl:if>
			</xsl:element>
		 </LexemeForm5002>
		 <xsl:if test="CitationForm5002 and (CitationForm5002/* != Allomorphs5002/*[1]/Form5035/*)">
			<xsl:copy-of select="CitationForm5002"/>
		 </xsl:if>
		 <xsl:if test="Allomorphs5002/*[2]">
			<AlternateForms5002>
			   <xsl:for-each select="Allomorphs5002/*[position()>1]">
				  <xsl:copy-of select="."/>
			   </xsl:for-each>
			</AlternateForms5002>
		 </xsl:if>
	  </xsl:if>
	  <xsl:if test="UnderlyingForm5002 and not (Allomorphs5002/*)">
		 <LexemeForm5002>
			<xsl:copy-of select="UnderlyingForm5002/*"/>
		 </LexemeForm5002>
		 <xsl:if test="CitationForm5002 and (CitationForm5002/* != UnderlyingForm5002//Form5035/*)">
			<xsl:copy-of select="CitationForm5002"/>
		 </xsl:if>
	  </xsl:if>
	  <xsl:apply-templates
		 select="*[name() != 'UnderlyingForm5002'][name() != 'CitationForm5002'][name() != 'Allomorphs5002']
		 [name() != 'MainEntryOrSense5009'] [name() != 'Comment5009'] [name() != 'MainEntriesOrSenses5007']"
	  />
   </xsl:template>

   <!-- Minor Entries -->

   <xsl:template match="LexMinorEntry">
	  <xsl:element name="LexEntry">
		 <xsl:apply-templates select="@*" mode="detailed"/>
			<xsl:element name="EntryRefs5002">
			   <xsl:element name="LexEntryRef">
				  <xsl:element name="VariantEntryTypes5127">
					 <Link ws="en" name="Irregularly Inflected Form"/>
				  </xsl:element>
				  <xsl:apply-templates select="MainEntryOrSense5009 | Comment5009 | MainEntryOrSenses5007 "/>
			   </xsl:element>
			</xsl:element>
	   <xsl:call-template name="lexeme-form"/>
	   </xsl:element>
   </xsl:template>

   <!-- Sub Entries -->

   <xsl:template match="LexSubentry">
	  <xsl:element name="LexEntry">
		 <xsl:apply-templates select="@*" mode="detailed"/>
			<xsl:element name="EntryRefs5002">
			   <xsl:element name="LexEntryRef">
				  <xsl:choose>
					 <xsl:when test="SubentryType5007/Link[@target='ID7F713EA-E8CF-11D3-9764-00C04F186933']">
						<ComplexEntryTypes5127>
						   <Link ws="en" name="Compound"/>
						</ComplexEntryTypes5127>
					 </xsl:when>
					 <xsl:when test="SubentryType5007/Link[@target='ID7F713EB-E8CF-11D3-9764-00C04F186933']">
						<ComplexEntryTypes5127>
						   <Link ws="en" name="Derivative"/>
						</ComplexEntryTypes5127>
					 </xsl:when>
					 <xsl:when test="SubentryType5007/Link[@target='ID7F713EC-E8CF-11D3-9764-00C04F186933']">
						<ComplexEntryTypes5127>
						   <Link ws="en" name="Idiom"/>
						</ComplexEntryTypes5127>
					 </xsl:when>
					 <xsl:when test="SubentryType5007/Link[@target='ID7F713ED-E8CF-11D3-9764-00C04F186933']">
						<ComplexEntryTypes5127>
						   <Link ws="en" name="Keyterm Phrase"/>
						</ComplexEntryTypes5127>
					 </xsl:when>
					 <xsl:when test="SubentryType5007/Link[@target='ID7F713EE-E8CF-11D3-9764-00C04F186933']">
						<ComplexEntryTypes5127>
						   <Link ws="en" name="Saying"/>
						</ComplexEntryTypes5127>
					 </xsl:when>
					 <xsl:otherwise>
						<ComplexEntryTypes5127>
			 <xsl:choose>
			<xsl:when test="SubentryType5007/Link/@name">
			   <Link ws="en" name="{SubentryType5007/Link/@name}"/>
			</xsl:when>
			<xsl:otherwise>
									<Link ws="en" name="Derivative"/>
			</xsl:otherwise>
			 </xsl:choose>
						</ComplexEntryTypes5127>
					 </xsl:otherwise>
				  </xsl:choose>
				  <RefType5127><Integer val="1"/></RefType5127>
				  <xsl:apply-templates select="MainEntryOrSense5009 | Comment5009 | MainEntriesOrSenses5007"/>
			   </xsl:element>
			</xsl:element>
		 <xsl:call-template name="lexeme-form"/>
	  </xsl:element>
   </xsl:template>

   <!-- Major Entries -->

   <xsl:template match="LexMajorEntry">
	  <xsl:element name="LexEntry">
		 <xsl:apply-templates select="@*" mode="detailed"/>
		 <xsl:call-template name="lexeme-form"/>
	  </xsl:element>
   </xsl:template>

   <!-- Miscellaneous Entry changes -->

   <xsl:template match="MainEntryOrSense5009[parent::LexMinorEntry]">
	  <xsl:element name="ComponentLexemes5127">
		 <xsl:apply-templates select="@*" mode="detailed"/>
		 <xsl:apply-templates select="*"/>
	  </xsl:element>
   </xsl:template>

   <xsl:template match="MainEntriesOrSenses5007[parent::LexSubentry]">
	  <xsl:element name="ComponentLexemes5127">
		 <xsl:apply-templates select="@*" mode="detailed"/>
		 <xsl:apply-templates select="*"/>
	  </xsl:element>
	  <xsl:element name="PrimaryLexemes5127">
		 <xsl:apply-templates select="@*" mode="detailed"/>
		 <xsl:apply-templates select="*"/>
	  </xsl:element>
   </xsl:template>

   <xsl:template match="MainEntryOrSense5009 | MainEntriesOrSenses5007">
	  <xsl:element name="MainEntriesOrSenses5002">
		 <xsl:apply-templates select="@*" mode="detailed"/>
		 <xsl:apply-templates select="*"/>
	  </xsl:element>
   </xsl:template>

   <xsl:template match="Comment5009 | Condition5009">
	  <xsl:element name="Summary5127">
		 <xsl:apply-templates select="@*" mode="detailed"/>
		 <xsl:apply-templates select="*"/>
	  </xsl:element>
   </xsl:template>

   <xsl:template match="SummaryDefinition5008 | SummaryDefinition5007">
	  <xsl:element name="SummaryDefinition5002">
		 <xsl:apply-templates select="@*" mode="detailed"/>
		 <xsl:apply-templates select="*"/>
	  </xsl:element>
   </xsl:template>

   <xsl:template match="LiteralMeaning5007">
	  <xsl:element name="LiteralMeaning5002">
		 <xsl:apply-templates select="@*" mode="detailed"/>
		 <xsl:apply-templates select="*"/>
	  </xsl:element>
   </xsl:template>

   <!-- Simple Sets -->

   <xsl:template match="LexicalRelationGroup[SetType5006/Integer/@val = 1]">
	  <LexRefType>
		 <MappingType5119>
			<Integer val="0"/>
		 </MappingType5119>
		 <xsl:if test="AVariableName5006">
			<Name7>
			   <xsl:apply-templates select="AVariableName5006" mode="str2uni"/>
			</Name7>
		 </xsl:if>
		 <xsl:if test="not (AVariableName5006)">
			<xsl:if test="Name5006">
			   <Name7>
				  <xsl:apply-templates select="Name5006" mode="str2uni"/>
			   </Name7>
			</xsl:if>
		 </xsl:if>
		 <xsl:if test="AVariableAbbr5006">
			<Abbreviation7>
			   <xsl:apply-templates select="AVariableAbbr5006" mode="str2uni"/>
			</Abbreviation7>
		 </xsl:if>
		 <xsl:if test="not (AVariableAbbr5006)">
			<xsl:if test="Abbreviation5006">
			   <Abbreviation7>
				  <xsl:apply-templates select="Abbreviation5006" mode="str2uni"/>
			   </Abbreviation7>
			</xsl:if>
		 </xsl:if>
		 <xsl:if test="BVariableName5006">
			<ReverseName5119>
			   <xsl:apply-templates select="BVariableName5006" mode="str2uni"/>
			</ReverseName5119>
		 </xsl:if>
		 <xsl:if test="BVariableAbbr5006">
			<ReverseAbbreviation5119>
			   <xsl:apply-templates select="BVariableAbbr5006" mode="str2uni"/>
			</ReverseAbbreviation5119>
		 </xsl:if>
		 <xsl:if test="Members5006">
			<Members5119>
			   <xsl:for-each select="Members5006/LexSimpleSet">
				  <LexReference>
					 <xsl:if test="Name5017">
						<Name5120>
						   <xsl:apply-templates select="Name5017" mode="str2uni"/>
						</Name5120>
					 </xsl:if>
					 <xsl:if test="Members5019">
						<Targets5120>
						   <xsl:for-each select=".//Link">
							  <xsl:element name="Link">
								 <xsl:attribute name="target">
									<xsl:value-of select="@target"/>
								 </xsl:attribute>
							  </xsl:element>
						   </xsl:for-each>
						</Targets5120>
					 </xsl:if>
				  </LexReference>
			   </xsl:for-each>
			</Members5119>
		 </xsl:if>
		 <xsl:if test="Comment5006">
			<Description7>
			   <xsl:apply-templates select="Comment5006/*"/>
			</Description7>
		 </xsl:if>
	  </LexRefType>
   </xsl:template>

   <!-- Pairs -->

   <xsl:template match="LexicalRelationGroup[SetType5006/Integer/@val = 2]">
	  <xsl:for-each select="Members5006/LexPairRelation">
		 <xsl:variable name="aname" select="AName5011/AStr/Run"/>
		 <xsl:variable name="bname" select="BName5011/AStr/Run"/>
		 <xsl:if test="not (preceding::*[AName5011/AStr/Run = $aname][BName5011/AStr/Run = $bname])">
			<LexRefType>
			   <MappingType5119>
				  <xsl:if test="AAbbr5011 = BAbbr5011">
					 <Integer val="1"/>
				  </xsl:if>
				  <xsl:if test="AAbbr5011 != BAbbr5011">
					 <Integer val="2"/>
				  </xsl:if>
			   </MappingType5119>
			   <xsl:if test="AName5011">
				  <Name7>
					 <xsl:apply-templates select="AName5011" mode="str2uni"/>
				  </Name7>
			   </xsl:if>
			   <xsl:if test="AAbbr5011">
				  <Abbreviation7>
					 <xsl:apply-templates select="AAbbr5011" mode="str2uni"/>
				  </Abbreviation7>
			   </xsl:if>
			   <xsl:if test="BName5011">
				  <ReverseName5119>
					 <xsl:apply-templates select="BName5011" mode="str2uni"/>
				  </ReverseName5119>
			   </xsl:if>
			   <xsl:if test="BAbbr5011">
				  <ReverseAbbreviation5119>
					 <xsl:apply-templates select="BAbbr5011" mode="str2uni"/>
				  </ReverseAbbreviation5119>
			   </xsl:if>
			   <xsl:if
				  test="ancestor::LexicalRelationGroup/Members5006/LexPairRelation[AName5011/AStr/Run = $aname][BName5011/AStr/Run = $bname][Members5011]">
				  <Members5119>
					 <xsl:for-each
						select="ancestor::LexicalRelationGroup/Members5006/LexPairRelation[AName5011/AStr/Run = $aname][BName5011/AStr/Run = $bname]">
						<xsl:for-each select="Members5011/LexPair">
						   <LexReference>
							  <xsl:if test="../../Name5017">
								 <Name5120>
									<xsl:apply-templates select="../../Name5017" mode="str2uni"/>
								 </Name5120>
							  </xsl:if>
							  <Targets5120>
								 <xsl:for-each select=".//Link">
									<xsl:element name="Link">
									   <xsl:attribute name="target">
										  <xsl:value-of select="@target"/>
									   </xsl:attribute>
									</xsl:element>
								 </xsl:for-each>
							  </Targets5120>
						   </LexReference>
						</xsl:for-each>
					 </xsl:for-each>
				  </Members5119>
			   </xsl:if>
			</LexRefType>
		 </xsl:if>
	  </xsl:for-each>
   </xsl:template>

   <!-- Scales -->

   <xsl:template match="LexicalRelationGroup[SetType5006/Integer/@val = 3]">
	  <LexRefType>
		 <MappingType5119>
			<Integer val="4"/>
		 </MappingType5119>
		 <xsl:if test="AVariableName5006">
			<Name7>
			   <xsl:apply-templates select="AVariableName5006" mode="str2uni"/>
			</Name7>
		 </xsl:if>
		 <xsl:if test="not (AVariableName5006)">
			<xsl:if test="Name5006">
			   <Name7>
				  <xsl:apply-templates select="Name5006" mode="str2uni"/>
			   </Name7>
			</xsl:if>
		 </xsl:if>
		 <xsl:if test="AVariableAbbr5006">
			<Abbreviation7>
			   <xsl:apply-templates select="AVariableAbbr5006" mode="str2uni"/>
			</Abbreviation7>
		 </xsl:if>
		 <xsl:if test="not (AVariableAbbr5006)">
			<xsl:if test="Abbreviation5006">
			   <Abbreviation7>
				  <xsl:apply-templates select="Abbreviation5006" mode="str2uni"/>
			   </Abbreviation7>
			</xsl:if>
		 </xsl:if>
		 <xsl:if test="BVariableName5006">
			<ReverseName5119>
			   <xsl:apply-templates select="BVariableName5006" mode="str2uni"/>
			</ReverseName5119>
		 </xsl:if>
		 <xsl:if test="BVariableAbbr5006">
			<ReverseAbbreviation5119>
			   <xsl:apply-templates select="BVariableAbbr5006" mode="str2uni"/>
			</ReverseAbbreviation5119>
		 </xsl:if>
		 <xsl:if test="Members5006">
			<Members5119>
			   <xsl:for-each select="Members5006/LexScale">
				  <LexReference>
					 <xsl:if test="Name5017">
						<Name5120>
						   <xsl:apply-templates select="Name5017" mode="str2uni"/>
						</Name5120>
					 </xsl:if>
					 <Targets5120>
						<xsl:for-each select="Positive5015//Link">
						   <xsl:element name="Link">
							  <xsl:attribute name="target">
								 <xsl:value-of select="@target"/>
							  </xsl:attribute>
						   </xsl:element>
						</xsl:for-each>
						<xsl:for-each select="Neutral5015//Link">
						   <xsl:element name="Link">
							  <xsl:attribute name="target">
								 <xsl:value-of select="@target"/>
							  </xsl:attribute>
						   </xsl:element>
						</xsl:for-each>
						<xsl:for-each select="Negative5015//Link">
						   <xsl:element name="Link">
							  <xsl:attribute name="target">
								 <xsl:value-of select="@target"/>
							  </xsl:attribute>
						   </xsl:element>
						</xsl:for-each>
					 </Targets5120>
				  </LexReference>
			   </xsl:for-each>
			</Members5119>
		 </xsl:if>
		 <xsl:if test="Comment5006">
			<Description7>
			   <xsl:apply-templates select="Comment5006/*"/>
			</Description7>
		 </xsl:if>
	  </LexRefType>
   </xsl:template>

   <!-- Trees -->

   <xsl:template match="LexicalRelationGroup[SetType5006/Integer/@val = 4]">
	  <xsl:for-each select="Members5006//LexTreeRelation">
		 <xsl:variable name="lowername" select="LowerName5024/AStr/Run"/>
		 <xsl:variable name="uppername" select="UpperName5024/AStr/Run"/>
		 <xsl:variable name="comment" select="Comment5006/AStr/Run"/>
		 <xsl:if test="not (preceding::*[LowerName5024/AStr/Run = $lowername][UpperName5024/AStr/Run = $uppername])">
			<LexRefType>
			   <MappingType5119>
				  <Integer val="3"/>
			   </MappingType5119>
			   <xsl:if test="LowerName5024">
				  <Name7>
					 <xsl:apply-templates select="LowerName5024" mode="str2uni"/>
				  </Name7>
			   </xsl:if>
			   <xsl:if test="LowerAbbr5024">
				  <Abbreviation7>
					 <xsl:apply-templates select="LowerAbbr5024" mode="str2uni"/>
				  </Abbreviation7>
			   </xsl:if>
			   <xsl:if test="UpperName5024">
				  <ReverseName5119>
					 <xsl:apply-templates select="UpperName5024" mode="str2uni"/>
				  </ReverseName5119>
			   </xsl:if>
			   <xsl:if test="UpperAbbr5024">
				  <ReverseAbbreviation5119>
					 <xsl:apply-templates select="UpperAbbr5024" mode="str2uni"/>
				  </ReverseAbbreviation5119>
			   </xsl:if>
			   <xsl:if test="ancestor::LexicalRelationGroup/Members5006//LexTreeRelation[LowerName5024/AStr/Run = $lowername][UpperName5024/AStr/Run = $uppername][Items5024]">
				  <Members5119>
					 <xsl:for-each select="ancestor::LexicalRelationGroup/Members5006//LexTreeRelation[LowerName5024/AStr/Run = $lowername][UpperName5024/AStr/Run = $uppername]">
						<xsl:for-each select="Items5024//Items5023">
						   <LexReference>
							  <xsl:for-each select="ancestor::LexTreeRelation">
								 <xsl:if test="Name5017">
									<Name5120>
									   <xsl:apply-templates select="Name5017" mode="str2uni"/>
									</Name5120>
								 </xsl:if>
								 <xsl:if test="Comment5017">
									<Comment5120>
									   <xsl:copy-of select="Comment5017/*"/>
									</Comment5120>
								 </xsl:if>
							  </xsl:for-each>
							  <Targets5120>
								 <xsl:if test="../Member5023/LexSetItem/Sense5018/Link">
									<xsl:element name="Link">
									   <xsl:attribute name="target">
										  <xsl:value-of select="../Member5023/LexSetItem/Sense5018/Link/@target"/>
									   </xsl:attribute>
									</xsl:element>
								 </xsl:if>
								 <xsl:for-each select="LexTreeItem/Member5023/LexSetItem/Sense5018/Link">
									<xsl:element name="Link">
									   <xsl:attribute name="target">
										  <xsl:value-of select="@target"/>
									   </xsl:attribute>
									</xsl:element>
								 </xsl:for-each>
							  </Targets5120>
						   </LexReference>
						</xsl:for-each>
					 </xsl:for-each>
				  </Members5119>
			   </xsl:if>
			   <xsl:if test="ancestor::LexicalRelationGroup/Members5006//LexTreeRelation[LowerName5024/AStr/Run = $lowername][UpperName5024/AStr/Run = $uppername][Comment5006]">
				  <Description7>
					 <xsl:apply-templates select="Comment5006/*"/>
					 <xsl:for-each select="ancestor::LexicalRelationGroup/Members5006//LexTreeRelation[LowerName5024/AStr/Run = $lowername][UpperName5024/AStr/Run = $uppername][Comment5006/AStr/Run != $comment]">
						<xsl:apply-templates select="Comment5006/*"/>
					 </xsl:for-each>
				  </Description7>
			   </xsl:if>
			</LexRefType>
		 </xsl:if>
	  </xsl:for-each>
   </xsl:template>

   <!-- Overall Relations plus CrossReferences (entry and sense pairs) -->

   <xsl:template match="LexicalRelations5005">
	  <References5005>
		 <CmPossibilityList>
			<Depth8>
			   <Integer val="1"/>
			</Depth8>
			<IsSorted8>
			   <Boolean val="true"/>
			</IsSorted8>
			<PreventDuplicates8>
			   <Boolean val="true"/>
			</PreventDuplicates8>
			<ItemClsid8>
			   <Integer val="5119"/>
			</ItemClsid8>
			<WsSelector8>
			   <Integer val="-3"/>
			</WsSelector8>
			<Name5>
			   <AUni ws="en">Lexical Reference Types</AUni>
			</Name5>
			<Abbreviation8>
			   <AUni ws="en">RefTyp</AUni>
			</Abbreviation8>
			<Possibilities8>
			   <xsl:apply-templates select="LexicalRelationGroup"/>
			   <xsl:if test="//CrossReferences5002/Link | //LexicalRelations5016/Link">
				  <LexRefType>
					 <MappingType5119>
						<Integer val="11"/>
					 </MappingType5119>
					 <Name7>
						<AUni ws="en">LinguaLinks Cross Reference</AUni>
					 </Name7>
					 <Abbreviation7>
						<AUni ws="en">llcr</AUni>
					 </Abbreviation7>
					 <Members5119>
						<xsl:for-each select="//CrossReferences5002/Link">
						   <xsl:variable name="targetid" select="@target"/>
						   <xsl:if test="preceding::*[@id = $targetid]">
							  <LexReference>
								 <Targets5120>
									<xsl:element name="Link">
									   <xsl:attribute name="target">
										  <xsl:value-of select="../../@id"/>
									   </xsl:attribute>
									</xsl:element>
									<xsl:element name="Link">
									   <xsl:attribute name="target">
										  <xsl:value-of select="@target"/>
									   </xsl:attribute>
									</xsl:element>
								 </Targets5120>
							  </LexReference>
						   </xsl:if>
						</xsl:for-each>
						<xsl:for-each select="//LexicalRelations5016/Link">
						   <xsl:variable name="targetid" select="@target"/>
						   <xsl:if test="preceding::*[@id = $targetid]">
							  <LexReference>
								 <Targets5120>
									<xsl:element name="Link">
									   <xsl:attribute name="target">
										  <xsl:value-of select="../../@id"/>
									   </xsl:attribute>
									</xsl:element>
									<xsl:element name="Link">
									   <xsl:attribute name="target">
										  <xsl:value-of select="@target"/>
									   </xsl:attribute>
									</xsl:element>
								 </Targets5120>
							  </LexReference>
						   </xsl:if>
						</xsl:for-each>
					 </Members5119>
				  </LexRefType>
			   </xsl:if>
			</Possibilities8>
		 </CmPossibilityList>
	  </References5005>
   </xsl:template>

   <!-- Concatenate ImportResidue5016 runs; also for ImportResidue5002 -->

   <xsl:template match="ImportResidue5016 | ImportResidue5002">
	  <xsl:element name="{name()}">
		 <Str>
			<xsl:for-each select="Str">
			   <xsl:copy-of select="Run"/>
			</xsl:for-each>
		 </Str>
	  </xsl:element>
   </xsl:template>

   <!-- Sense Annotations -->
   <!-- Sense Annotations template -->

   <xsl:template name="Annotations">
	  <xsl:param name="trigger"/>
	  <xsl:param name="newelement"/>
	  <xsl:if test="CmAnnotation/InstanceOf34/Link[@name = $trigger]">
		 <xsl:element name="{$newelement}">
			<xsl:for-each
			   select="CmAnnotation/Comment34/AStr[@ws!='' and ../../InstanceOf34/Link/@name = $trigger]">
			   <xsl:variable name="thisws" select="@ws"/>
			   <!-- xsl:if test="string-length(string(../../preceding-sibling::CmAnnotation/Comment34/AStr[../../InstanceOf34/Link/@name = $trigger][@ws = $thisws])) = 0" -->
			   <xsl:if
				  test="not (../../preceding-sibling::CmAnnotation/Comment34/AStr[../../InstanceOf34/Link/@name = $trigger][@ws = $thisws])">
				  <AStr>
					 <xsl:attribute name="ws">
						<xsl:value-of select="@ws"/>
					 </xsl:attribute>
					 <xsl:for-each
						select="../../../CmAnnotation/Comment34/AStr[../../InstanceOf34/Link/@name = $trigger][@ws = $thisws]">
						<xsl:if test="position() = 1">
						   <xsl:copy-of select="Run"/>
						</xsl:if>
						<xsl:if test="position() != 1">
						   <xsl:for-each select="Run">
							  <xsl:if test="position() = 1">
								 <Run>
									<xsl:attribute name="ws">
									   <xsl:value-of select="@ws"/>
									</xsl:attribute>
									<xsl:text>;&#x0020;</xsl:text>
									<xsl:value-of select="."/>
								 </Run>
							  </xsl:if>
							  <xsl:if test="position() != 1">
								 <xsl:copy-of select="."/>
							  </xsl:if>
						   </xsl:for-each>
						</xsl:if>
					 </xsl:for-each>
				  </AStr>
			   </xsl:if>
			</xsl:for-each>
		 </xsl:element>
	  </xsl:if>
   </xsl:template>

   <!-- Sense Annotations, except General -->

   <xsl:template match="Annotations5016">
	  <xsl:call-template name="Annotations">
		 <xsl:with-param name="trigger">grammar</xsl:with-param>
		 <xsl:with-param name="newelement">GrammarNote5016</xsl:with-param>
	  </xsl:call-template>
	  <xsl:call-template name="Annotations">
		 <xsl:with-param name="trigger">anthropology</xsl:with-param>
		 <xsl:with-param name="newelement">AnthroNote5016</xsl:with-param>
	  </xsl:call-template>
	  <xsl:call-template name="Annotations">
		 <xsl:with-param name="trigger">discourse</xsl:with-param>
		 <xsl:with-param name="newelement">DiscourseNote5016</xsl:with-param>
	  </xsl:call-template>
	  <xsl:call-template name="Annotations">
		 <xsl:with-param name="trigger">phonology</xsl:with-param>
		 <xsl:with-param name="newelement">PhonologyNote5016</xsl:with-param>
	  </xsl:call-template>
	  <xsl:call-template name="Annotations">
		 <xsl:with-param name="trigger">semantics</xsl:with-param>
		 <xsl:with-param name="newelement">SemanticsNote5016</xsl:with-param>
	  </xsl:call-template>
	  <xsl:call-template name="Annotations">
		 <xsl:with-param name="trigger">sociolinguistics</xsl:with-param>
		 <xsl:with-param name="newelement">SocioLinguisticsNote5016</xsl:with-param>
	  </xsl:call-template>
	  <xsl:call-template name="Annotations">
		 <xsl:with-param name="trigger">encyclopedic</xsl:with-param>
		 <xsl:with-param name="newelement">EncyclopedicInfo5016</xsl:with-param>
	  </xsl:call-template>
	  <xsl:call-template name="Annotations">
		 <xsl:with-param name="trigger">bibliography</xsl:with-param>
		 <xsl:with-param name="newelement">Bibliography5016</xsl:with-param>
	  </xsl:call-template>

	  <!-- Sense Annotations, General (be careful, Media file (externalLink/namedStyle) falls into here) -->

	  <xsl:if
		 test="CmAnnotation/InstanceOf34/Link[@name != 'anthropology'][@name != 'discourse'][@name != 'grammar'][@name != 'phonology'][@name != 'semantics'][@name != 'sociolinguistics'][@name != 'encyclopedic'][@name != 'bibliography']">
		 <GeneralNote5016>
			<xsl:for-each
			   select="CmAnnotation/Comment34/AStr[@ws!='' and ../../InstanceOf34/Link/@name != 'anthropology'][../../InstanceOf34/Link/@name != 'discourse'][../../InstanceOf34/Link/@name != 'grammar'][../../InstanceOf34/Link/@name != 'phonology'][../../InstanceOf34/Link/@name != 'semantics'][../../InstanceOf34/Link/@name != 'sociolinguistics'][../../InstanceOf34/Link/@name != 'encyclopedic'][../../InstanceOf34/Link/@name != 'bibliography']">
			   <xsl:variable name="thisws" select="@ws"/>
			   <xsl:if
				  test="not (../../preceding-sibling::CmAnnotation/Comment34/AStr[../../InstanceOf34/Link/@name != 'anthropology'][../../InstanceOf34/Link/@name != 'discourse'][../../InstanceOf34/Link/@name != 'grammar'][../../InstanceOf34/Link/@name != 'phonology'][../../InstanceOf34/Link/@name != 'semantics'][../../InstanceOf34/Link/@name != 'sociolinguistics'][../../InstanceOf34/Link/@name != 'encyclopedic'][../../InstanceOf34/Link/@name != 'bibliography'][@ws = $thisws])">
				  <AStr>
					 <xsl:attribute name="ws">
						<xsl:value-of select="@ws"/>
					 </xsl:attribute>
					 <xsl:for-each
						select="../../../CmAnnotation/Comment34/AStr[../../InstanceOf34/Link/@name != 'anthropology'][../../InstanceOf34/Link/@name != 'discourse'][../../InstanceOf34/Link/@name != 'grammar'][../../InstanceOf34/Link/@name != 'phonology'][../../InstanceOf34/Link/@name != 'semantics'][../../InstanceOf34/Link/@name != 'sociolinguistics'][../../InstanceOf34/Link/@name != 'encyclopedic'][../../InstanceOf34/Link/@name != 'bibliography'][@ws = $thisws]">
						<xsl:if test="position() = 1">
						   <xsl:for-each select="Run">
							  <xsl:if test="position() = 1">
								 <xsl:if test="@externalLink">
									<Run>
									   <xsl:attribute name="ws">
										  <xsl:value-of select="../../../InstanceOf34/Link/@ws"/>
									   </xsl:attribute>
									   <xsl:value-of select="../../../InstanceOf34/Link/@name"/>
									   <xsl:text>:&#x0020;</xsl:text>
									</Run>
									<Run>
									   <xsl:attribute name="ws">
										  <xsl:value-of select="@ws"/>
									   </xsl:attribute>
									   <xsl:attribute name="externalLink">
										  <xsl:value-of select="@externalLink"/>
									   </xsl:attribute>
									   <xsl:attribute name="namedStyle">
										  <xsl:value-of select="@namedStyle"/>
									   </xsl:attribute>
									   <xsl:value-of select="."/>
									</Run>
								 </xsl:if>
								 <xsl:if test="not(@externalLink)">
									<xsl:if test="../../../InstanceOf34/Link/@ws = @ws">
									   <Run>
										  <xsl:attribute name="ws">
											 <xsl:value-of select="@ws"/>
										  </xsl:attribute>
										  <xsl:value-of select="../../../InstanceOf34/Link/@name"/>
										  <xsl:text>:&#x0020;</xsl:text>
										  <xsl:value-of select="."/>
									   </Run>
									</xsl:if>
									<xsl:if test="../../../InstanceOf34/Link/@ws != @ws">
									   <Run>
										  <xsl:attribute name="ws">
											 <xsl:value-of select="../../../InstanceOf34/Link/@ws"/>
										  </xsl:attribute>
										  <xsl:value-of select="../../../InstanceOf34/Link/@name"/>
										  <xsl:text>:&#x0020;</xsl:text>
									   </Run>
									   <Run>
										  <xsl:attribute name="ws">
											 <xsl:value-of select="@ws"/>
										  </xsl:attribute>
										  <xsl:value-of select="."/>
									   </Run>
									</xsl:if>
								 </xsl:if>
							  </xsl:if>
							  <xsl:if test="position() != 1">
								 <xsl:copy-of select="."/>
							  </xsl:if>
						   </xsl:for-each>
						</xsl:if>
						<xsl:if test="position() != 1">
						   <xsl:for-each select="Run">
							  <xsl:if test="position() = 1">
								 <xsl:if test="@externalLink">
									<Run>
									   <xsl:attribute name="ws">
										  <xsl:value-of select="../../../InstanceOf34/Link/@ws"/>
									   </xsl:attribute>
									   <xsl:text>;&#x2028;</xsl:text>
									   <xsl:value-of select="../../../InstanceOf34/Link/@name"/>
									   <xsl:text>:&#x0020;</xsl:text>
									</Run>
									<Run>
									   <xsl:attribute name="ws">
										  <xsl:value-of select="@ws"/>
									   </xsl:attribute>
									   <xsl:attribute name="externalLink">
										  <xsl:value-of select="@externalLink"/>
									   </xsl:attribute>
									   <xsl:attribute name="namedStyle">
										  <xsl:value-of select="@namedStyle"/>
									   </xsl:attribute>
									   <xsl:value-of select="."/>
									</Run>
								 </xsl:if>
								 <xsl:if test="not(@externalLink)">
									<xsl:if test="../../../InstanceOf34/Link/@ws = @ws">
									   <Run>
										  <xsl:attribute name="ws">
											 <xsl:value-of select="@ws"/>
										  </xsl:attribute>
										  <xsl:text>;&#x2028;</xsl:text>
										  <xsl:value-of select="../../../InstanceOf34/Link/@name"/>
										  <xsl:text>:&#x0020;</xsl:text>
										  <xsl:value-of select="."/>
									   </Run>
									</xsl:if>
									<xsl:if test="../../../InstanceOf34/Link/@ws != @ws">
									   <Run>
										  <xsl:attribute name="ws">
											 <xsl:value-of select="../../../InstanceOf34/Link/@ws"/>
										  </xsl:attribute>
										  <xsl:text>;&#x2028;</xsl:text>
										  <xsl:value-of select="../../../InstanceOf34/Link/@name"/>
										  <xsl:text>:&#x0020;</xsl:text>
									   </Run>
									   <Run>
										  <xsl:attribute name="ws">
											 <xsl:value-of select="@ws"/>
										  </xsl:attribute>
										  <xsl:value-of select="."/>
									   </Run>
									</xsl:if>
								 </xsl:if>
							  </xsl:if>
							  <xsl:if test="position() != 1">
								 <xsl:copy-of select="."/>
							  </xsl:if>
						   </xsl:for-each>
						</xsl:if>
					 </xsl:for-each>
				  </AStr>
			   </xsl:if>
			</xsl:for-each>
		 </GeneralNote5016>
	  </xsl:if>
   </xsl:template>

   <!-- Entry Annotations -->

   <xsl:template match="Annotations5002">
	  <Comment5002>
		 <xsl:for-each select="CmAnnotation/Comment34/AStr[@ws!='']">
			<xsl:variable name="thisws" select="@ws"/>
			<xsl:if test="not (../../preceding-sibling::CmAnnotation/Comment34/AStr[@ws = $thisws])">
			   <AStr>
				  <xsl:attribute name="ws">
					 <xsl:value-of select="@ws"/>
				  </xsl:attribute>
				  <xsl:for-each select="../../../CmAnnotation/Comment34/AStr[@ws = $thisws]">
					 <xsl:if test="position() = 1">
						<xsl:for-each select="Run">
						   <xsl:if test="position() = 1">
							  <Run>
								 <xsl:attribute name="ws">
									<xsl:value-of select="@ws"/>
								 </xsl:attribute>
								 <xsl:value-of select="../../../InstanceOf34/Link/@name"/>
								 <xsl:text>:&#x0020;</xsl:text>
								 <xsl:value-of select="."/>
							  </Run>
						   </xsl:if>
						   <xsl:if test="position() != 1">
							  <xsl:copy-of select="."/>
						   </xsl:if>
						</xsl:for-each>
					 </xsl:if>
					 <xsl:if test="position() != 1">
						<xsl:for-each select="Run">
						   <xsl:if test="position() = 1">
							  <Run>
								 <xsl:attribute name="ws">
									<xsl:value-of select="@ws"/>
								 </xsl:attribute>
								 <xsl:text>;&#x2028;</xsl:text>
								 <xsl:value-of select="../../../InstanceOf34/Link/@name"/>
								 <xsl:text>:&#x0020;</xsl:text>
								 <xsl:value-of select="."/>
							  </Run>
						   </xsl:if>
						   <xsl:if test="position() != 1">
							  <xsl:copy-of select="."/>
						   </xsl:if>
						</xsl:for-each>
					 </xsl:if>
				  </xsl:for-each>
			   </AStr>
			</xsl:if>
		 </xsl:for-each>
	  </Comment5002>
   </xsl:template>

   <!-- Delete empty ReversalIndexEntries and change Form5053 and WritingSystem5053 to ReversalForm5053 -->

   <xsl:template match="ReversalIndexEntry">
	  <xsl:if test="Form5053/Uni/node()">
		 <xsl:copy>
			<xsl:apply-templates select="@*"/>
			<ReversalForm5053>
			   <xsl:element name="AUni">
				  <xsl:attribute name="ws">
					 <xsl:value-of select="WritingSystem5053/Link/@ws"/>
				  </xsl:attribute>
				  <xsl:value-of select="Form5053/Uni/node()"/>
			   </xsl:element>
			</ReversalForm5053>
		 </xsl:copy>
	  </xsl:if>
   </xsl:template>

   <xsl:template match="ReversalEntries5016">
	  <xsl:if test="Link[@form!='' and @ws!='']">
		 <ReversalEntries5016>
			<xsl:for-each select="Link">
			   <xsl:if test="@form!='' and @ws!=''">
				  <xsl:copy>
					 <xsl:apply-templates select="@* | node()"/>
				  </xsl:copy>
			   </xsl:if>
			</xsl:for-each>
		 </ReversalEntries5016>
	  </xsl:if>
   </xsl:template>

   <!-- Thesaurus Entries become Semantic Domains -->

   <xsl:template match="ThesaurusItems5016">
	  <xsl:if
		 test="Link[@abbr = '1']|Link[@abbr = '1.1']|Link[@abbr = '1.1.1']|Link[@abbr = '1.1.2']|Link[@abbr = '1.1.2.1']|Link[@abbr = '1.1.2.2']|Link[@abbr = '1.1.2.3']|Link[@abbr = '1.1.2.4']|Link[@abbr = '1.1.3']|Link[@abbr = '1.1.3.1']|Link[@abbr = '1.1.3.2']|Link[@abbr = '1.1.3.3']|Link[@abbr = '1.1.3.4']|Link[@abbr = '1.1.3.5']|Link[@abbr = '1.1.3.6']|Link[@abbr = '1.1.4']|Link[@abbr = '1.1.4.1']|Link[@abbr = '1.1.4.2']|Link[@abbr = '1.1.4.3']|Link[@abbr = '1.1.4.4']|Link[@abbr = '1.1.4.5']|Link[@abbr = '1.1.4.6']|Link[@abbr = '1.2']|Link[@abbr = '1.2.1']|Link[@abbr = '1.2.2']|Link[@abbr = '1.2.3']|Link[@abbr = '1.2.4']|Link[@abbr = '1.2.5']|Link[@abbr = '1.2.6']|Link[@abbr = '1.2.7']|Link[@abbr = '1.3']|Link[@abbr = '1.3.1']|Link[@abbr = '1.3.2']|Link[@abbr = '1.4']|Link[@abbr = '1.4.1']|Link[@abbr = '1.4.2']|Link[@abbr = '1.4.2.1']|Link[@abbr = '1.4.2.2']|Link[@abbr = '1.4.2.3']|Link[@abbr = '1.4.2.4']|Link[@abbr = '1.4.2.5']|Link[@abbr = '1.4.2.6']|Link[@abbr = '1.4.2.7']|Link[@abbr = '1.4.2.8']|Link[@abbr = '1.4.2.9']|Link[@abbr = '1.4.3']|Link[@abbr = '1.4.3.1']|Link[@abbr = '1.4.3.2']|Link[@abbr = '1.4.4']|Link[@abbr = '1.4.5']|Link[@abbr = '1.4.5.1']|Link[@abbr = '1.4.5.2']|Link[@abbr = '1.4.5.3']|Link[@abbr = '1.4.5.4']|Link[@abbr = '1.4.6']|Link[@abbr = '1.4.6.1']|Link[@abbr = '1.4.6.2']|Link[@abbr = '1.4.7']|Link[@abbr = '1.4.7.1']|Link[@abbr = '1.4.7.2']|Link[@abbr = '1.4.7.3']|Link[@abbr = '1.4.8']|Link[@abbr = '1.4.8.1']|Link[@abbr = '1.4.8.2']|Link[@abbr = '1.4.9']|Link[@abbr = '1.4.9.1']|Link[@abbr = '1.4.9.2']|Link[@abbr = '1.4.9.3']|Link[@abbr = '1.4.9.4']|Link[@abbr = '1.5']|Link[@abbr = '1.5.1']|Link[@abbr = '1.5.1.1']|Link[@abbr = '1.5.1.2']|Link[@abbr = '1.5.1.3']|Link[@abbr = '1.5.2']|Link[@abbr = '1.5.3']|Link[@abbr = '1.5.4']|Link[@abbr = '1.5.5']|Link[@abbr = '1.5.6']|Link[@abbr = '1.6']|Link[@abbr = '1.7']|Link[@abbr = '1.7.1']|Link[@abbr = '1.7.2']|Link[@abbr = '1.7.3']|Link[@abbr = '1.7.4']|Link[@abbr = '1.7.5']|Link[@abbr = '1.7.6']|Link[@abbr = '1.7.7']|Link[@abbr = '1.7.8']|Link[@abbr = '1.7.9']|Link[@abbr = '1.8']|Link[@abbr = '2']|Link[@abbr = '2.1']|Link[@abbr = '2.1.1']|Link[@abbr = '2.1.10']|Link[@abbr = '2.1.2']|Link[@abbr = '2.1.3']|Link[@abbr = '2.1.4']|Link[@abbr = '2.1.5']|Link[@abbr = '2.1.6']|Link[@abbr = '2.1.7']|Link[@abbr = '2.1.8']|Link[@abbr = '2.1.9']|Link[@abbr = '2.2']|Link[@abbr = '2.2.1']|Link[@abbr = '2.2.2']|Link[@abbr = '2.2.3']|Link[@abbr = '2.2.4']|Link[@abbr = '2.2.5']|Link[@abbr = '2.3']|Link[@abbr = '2.3.1']|Link[@abbr = '2.3.2']|Link[@abbr = '2.3.3']|Link[@abbr = '2.4']|Link[@abbr = '2.4.1']|Link[@abbr = '2.4.2']|Link[@abbr = '2.4.3']|Link[@abbr = '2.4.4']|Link[@abbr = '2.4.5']|Link[@abbr = '2.5']|Link[@abbr = '2.5.1']|Link[@abbr = '2.5.2']|Link[@abbr = '2.5.3']|Link[@abbr = '2.5.4']|Link[@abbr = '2.6']|Link[@abbr = '3']|Link[@abbr = '3.1']|Link[@abbr = '3.2']|Link[@abbr = '3.3']|Link[@abbr = '4']|Link[@abbr = '4.1']|Link[@abbr = '4.1.1']|Link[@abbr = '4.1.1.1']|Link[@abbr = '4.1.1.2']|Link[@abbr = '4.1.1.3']|Link[@abbr = '4.1.1.4']|Link[@abbr = '4.1.1.5']|Link[@abbr = '4.1.2']|Link[@abbr = '4.1.2.1']|Link[@abbr = '4.1.2.2']|Link[@abbr = '4.1.2.3']|Link[@abbr = '4.1.2.4']|Link[@abbr = '4.1.2.5']|Link[@abbr = '4.10']|Link[@abbr = '4.10.1']|Link[@abbr = '4.10.10']|Link[@abbr = '4.10.10.1']|Link[@abbr = '4.10.10.2']|Link[@abbr = '4.10.2']|Link[@abbr = '4.10.2.1']|Link[@abbr = '4.10.2.2']|Link[@abbr = '4.10.3']|Link[@abbr = '4.10.4']|Link[@abbr = '4.10.5']|Link[@abbr = '4.10.6']|Link[@abbr = '4.10.7']|Link[@abbr = '4.10.8']|Link[@abbr = '4.10.9']|Link[@abbr = '4.10.9.1']|Link[@abbr = '4.10.9.2']|Link[@abbr = '4.11']|Link[@abbr = '4.11.1']|Link[@abbr = '4.11.1.1']|Link[@abbr = '4.11.1.2']|Link[@abbr = '4.11.1.3']|Link[@abbr = '4.11.1.4']|Link[@abbr = '4.11.1.5']|Link[@abbr = '4.11.1.6']|Link[@abbr = '4.11.1.7']|Link[@abbr = '4.11.2']|Link[@abbr = '4.11.2.1']|Link[@abbr = '4.11.2.2']|Link[@abbr = '4.11.2.3']|Link[@abbr = '4.11.2.4']|Link[@abbr = '4.11.3']|Link[@abbr = '4.11.3.1']|Link[@abbr = '4.11.3.2']|Link[@abbr = '4.11.3.3']|Link[@abbr = '4.11.3.4']|Link[@abbr = '4.11.4']|Link[@abbr = '4.11.4.1']|Link[@abbr = '4.11.4.2']|Link[@abbr = '4.11.4.3']|Link[@abbr = '4.11.4.4']|Link[@abbr = '4.11.4.5']|Link[@abbr = '4.11.4.6']|Link[@abbr = '4.11.4.7']|Link[@abbr = '4.11.5']|Link[@abbr = '4.11.5.1']|Link[@abbr = '4.11.5.2']|Link[@abbr = '4.11.5.3']|Link[@abbr = '4.11.5.4']|Link[@abbr = '4.11.5.5']|Link[@abbr = '4.11.5.6']|Link[@abbr = '4.11.5.7']|Link[@abbr = '4.11.5.8']|Link[@abbr = '4.11.5.9']|Link[@abbr = '4.11.6']|Link[@abbr = '4.11.6.1']|Link[@abbr = '4.11.6.2']|Link[@abbr = '4.11.6.3']|Link[@abbr = '4.11.6.4']|Link[@abbr = '4.11.6.5']|Link[@abbr = '4.12']|Link[@abbr = '4.13']|Link[@abbr = '4.13.1']|Link[@abbr = '4.13.1.1']|Link[@abbr = '4.13.1.2']|Link[@abbr = '4.13.1.3']|Link[@abbr = '4.13.1.4']|Link[@abbr = '4.13.1.5']|Link[@abbr = '4.13.1.6']|Link[@abbr = '4.13.1.7']|Link[@abbr = '4.13.2']|Link[@abbr = '4.13.2.1']|Link[@abbr = '4.13.2.1.1']|Link[@abbr = '4.13.2.1.2']|Link[@abbr = '4.13.2.1.3']|Link[@abbr = '4.13.2.1.4']|Link[@abbr = '4.13.2.1.5']|Link[@abbr = '4.13.2.2']|Link[@abbr = '4.13.2.2.1']|Link[@abbr = '4.13.2.2.2']|Link[@abbr = '4.13.2.2.3']|Link[@abbr = '4.13.2.3']|Link[@abbr = '4.13.2.3.1']|Link[@abbr = '4.13.2.3.2']|Link[@abbr = '4.13.2.3.3']|Link[@abbr = '4.13.2.3.4']|Link[@abbr = '4.13.2.4']|Link[@abbr = '4.13.2.4.1']|Link[@abbr = '4.13.2.4.10']|Link[@abbr = '4.13.2.4.2']|Link[@abbr = '4.13.2.4.3']|Link[@abbr = '4.13.2.4.4']|Link[@abbr = '4.13.2.4.5']|Link[@abbr = '4.13.2.4.6']|Link[@abbr = '4.13.2.4.7']|Link[@abbr = '4.13.2.4.8']|Link[@abbr = '4.13.2.4.9']|Link[@abbr = '4.13.2.5']|Link[@abbr = '4.13.2.5.1']|Link[@abbr = '4.13.2.5.2']|Link[@abbr = '4.13.2.6']|Link[@abbr = '4.13.2.6.1']|Link[@abbr = '4.13.2.6.2']|Link[@abbr = '4.13.2.6.3']|Link[@abbr = '4.13.2.7']|Link[@abbr = '4.13.2.7.1']|Link[@abbr = '4.13.2.7.2']|Link[@abbr = '4.13.3']|Link[@abbr = '4.13.3.1']|Link[@abbr = '4.13.3.2']|Link[@abbr = '4.13.3.3']|Link[@abbr = '4.13.3.4']|Link[@abbr = '4.13.4']|Link[@abbr = '4.13.4.1']|Link[@abbr = '4.13.4.2']|Link[@abbr = '4.13.4.3']|Link[@abbr = '4.13.4.4']|Link[@abbr = '4.13.4.5']|Link[@abbr = '4.13.4.6']|Link[@abbr = '4.13.4.7']|Link[@abbr = '4.13.4.8']|Link[@abbr = '4.13.4.9']|Link[@abbr = '4.13.5']|Link[@abbr = '4.13.5.1']|Link[@abbr = '4.13.5.2']|Link[@abbr = '4.13.5.3']|Link[@abbr = '4.13.5.4']|Link[@abbr = '4.13.6']|Link[@abbr = '4.13.7']|Link[@abbr = '4.14']|Link[@abbr = '4.14.1']|Link[@abbr = '4.14.1.1']|Link[@abbr = '4.14.1.2']|Link[@abbr = '4.14.1.3']|Link[@abbr = '4.14.1.4']|Link[@abbr = '4.14.1.5']|Link[@abbr = '4.14.1.6']|Link[@abbr = '4.14.1.7']|Link[@abbr = '4.14.1.8']|Link[@abbr = '4.14.1.9']|Link[@abbr = '4.14.2']|Link[@abbr = '4.14.2.1']|Link[@abbr = '4.14.2.2']|Link[@abbr = '4.14.2.3']|Link[@abbr = '4.14.2.4']|Link[@abbr = '4.14.3']|Link[@abbr = '4.14.3.1']|Link[@abbr = '4.14.3.1.1']|Link[@abbr = '4.14.3.1.2']|Link[@abbr = '4.14.3.1.3']|Link[@abbr = '4.14.3.1.4']|Link[@abbr = '4.14.3.1.5']|Link[@abbr = '4.14.3.2']|Link[@abbr = '4.14.3.2.1']|Link[@abbr = '4.14.3.2.2']|Link[@abbr = '4.14.3.2.3']|Link[@abbr = '4.14.3.2.4']|Link[@abbr = '4.14.3.2.5']|Link[@abbr = '4.14.4']|Link[@abbr = '4.14.4.1']|Link[@abbr = '4.14.4.2']|Link[@abbr = '4.15']|Link[@abbr = '4.15.1']|Link[@abbr = '4.15.1.1']|Link[@abbr = '4.15.1.1.1']|Link[@abbr = '4.15.1.1.2']|Link[@abbr = '4.15.1.1.3']|Link[@abbr = '4.15.1.1.4']|Link[@abbr = '4.15.1.1.5']|Link[@abbr = '4.15.1.1.6']|Link[@abbr = '4.15.1.2']|Link[@abbr = '4.15.1.2.1']|Link[@abbr = '4.15.1.2.2']|Link[@abbr = '4.15.1.2.3']|Link[@abbr = '4.15.1.2.4']|Link[@abbr = '4.15.1.2.5']|Link[@abbr = '4.15.1.3']|Link[@abbr = '4.15.1.3.1']|Link[@abbr = '4.15.2']|Link[@abbr = '4.15.2.1']|Link[@abbr = '4.15.2.2']|Link[@abbr = '4.16']|Link[@abbr = '4.16.1']|Link[@abbr = '4.16.2']|Link[@abbr = '4.16.3']|Link[@abbr = '4.16.4']|Link[@abbr = '4.16.5']|Link[@abbr = '4.17']|Link[@abbr = '4.17.1']|Link[@abbr = '4.17.1.1']|Link[@abbr = '4.17.1.2']|Link[@abbr = '4.17.1.3']|Link[@abbr = '4.17.1.4']|Link[@abbr = '4.17.1.5']|Link[@abbr = '4.17.1.6']|Link[@abbr = '4.17.10']|Link[@abbr = '4.17.11']|Link[@abbr = '4.17.11.1']|Link[@abbr = '4.17.11.2']|Link[@abbr = '4.17.11.3']|Link[@abbr = '4.17.12']|Link[@abbr = '4.17.12.1']|Link[@abbr = '4.17.12.2']|Link[@abbr = '4.17.12.3']|Link[@abbr = '4.17.12.4']|Link[@abbr = '4.17.12.5']|Link[@abbr = '4.17.12.6']|Link[@abbr = '4.17.12.7']|Link[@abbr = '4.17.12.8']|Link[@abbr = '4.17.2']|Link[@abbr = '4.17.2.1']|Link[@abbr = '4.17.2.2']|Link[@abbr = '4.17.3']|Link[@abbr = '4.17.4']|Link[@abbr = '4.17.4.1']|Link[@abbr = '4.17.5']|Link[@abbr = '4.17.5.1']|Link[@abbr = '4.17.5.2']|Link[@abbr = '4.17.5.3']|Link[@abbr = '4.17.6']|Link[@abbr = '4.17.6.1']|Link[@abbr = '4.17.6.2']|Link[@abbr = '4.17.7']|Link[@abbr = '4.17.7.1']|Link[@abbr = '4.17.7.2']|Link[@abbr = '4.17.8']|Link[@abbr = '4.17.9']|Link[@abbr = '4.17.9.1']|Link[@abbr = '4.17.9.2']|Link[@abbr = '4.17.9.2.1']|Link[@abbr = '4.17.9.2.2']|Link[@abbr = '4.17.9.2.3']|Link[@abbr = '4.17.9.3']|Link[@abbr = '4.17.9.3.1']|Link[@abbr = '4.17.9.3.2']|Link[@abbr = '4.17.9.3.3']|Link[@abbr = '4.17.9.4']|Link[@abbr = '4.17.9.4.1']|Link[@abbr = '4.17.9.4.2']|Link[@abbr = '4.17.9.4.3']|Link[@abbr = '4.17.9.4.4']|Link[@abbr = '4.17.9.4.5']|Link[@abbr = '4.18']|Link[@abbr = '4.18.1']|Link[@abbr = '4.18.1.1']|Link[@abbr = '4.18.1.2']|Link[@abbr = '4.18.1.3']|Link[@abbr = '4.18.1.4']|Link[@abbr = '4.18.1.5']|Link[@abbr = '4.18.2']|Link[@abbr = '4.18.2.1']|Link[@abbr = '4.18.2.2']|Link[@abbr = '4.18.2.3']|Link[@abbr = '4.18.2.4']|Link[@abbr = '4.18.2.5']|Link[@abbr = '4.18.3']|Link[@abbr = '4.18.3.1']|Link[@abbr = '4.18.3.2']|Link[@abbr = '4.18.3.3']|Link[@abbr = '4.18.3.4']|Link[@abbr = '4.18.3.5']|Link[@abbr = '4.18.4']|Link[@abbr = '4.18.4.1']|Link[@abbr = '4.18.4.2']|Link[@abbr = '4.18.4.3']|Link[@abbr = '4.18.5']|Link[@abbr = '4.18.5.1']|Link[@abbr = '4.18.5.2']|Link[@abbr = '4.18.6']|Link[@abbr = '4.2']|Link[@abbr = '4.2.1']|Link[@abbr = '4.2.1.1']|Link[@abbr = '4.2.1.2']|Link[@abbr = '4.2.1.3']|Link[@abbr = '4.2.2']|Link[@abbr = '4.2.3']|Link[@abbr = '4.2.4']|Link[@abbr = '4.2.5']|Link[@abbr = '4.2.6']|Link[@abbr = '4.2.7']|Link[@abbr = '4.3']|Link[@abbr = '4.3.1']|Link[@abbr = '4.3.1.1']|Link[@abbr = '4.3.1.10']|Link[@abbr = '4.3.1.11']|Link[@abbr = '4.3.1.2']|Link[@abbr = '4.3.1.3']|Link[@abbr = '4.3.1.4']|Link[@abbr = '4.3.1.5']|Link[@abbr = '4.3.1.6']|Link[@abbr = '4.3.1.7']|Link[@abbr = '4.3.1.8']|Link[@abbr = '4.3.1.9']|Link[@abbr = '4.3.10']|Link[@abbr = '4.3.10.1']|Link[@abbr = '4.3.2']|Link[@abbr = '4.3.2.1']|Link[@abbr = '4.3.2.2']|Link[@abbr = '4.3.2.3']|Link[@abbr = '4.3.3']|Link[@abbr = '4.3.4']|Link[@abbr = '4.3.4.1']|Link[@abbr = '4.3.4.2']|Link[@abbr = '4.3.5']|Link[@abbr = '4.3.6']|Link[@abbr = '4.3.6.1']|Link[@abbr = '4.3.6.2']|Link[@abbr = '4.3.6.3']|Link[@abbr = '4.3.6.4']|Link[@abbr = '4.3.6.5']|Link[@abbr = '4.3.6.6']|Link[@abbr = '4.3.6.7']|Link[@abbr = '4.3.7']|Link[@abbr = '4.3.7.1']|Link[@abbr = '4.3.7.2']|Link[@abbr = '4.3.8']|Link[@abbr = '4.3.9']|Link[@abbr = '4.3.9.1']|Link[@abbr = '4.3.9.2']|Link[@abbr = '4.3.9.3']|Link[@abbr = '4.3.9.4']|Link[@abbr = '4.3.9.5']|Link[@abbr = '4.3.9.6']|Link[@abbr = '4.3.9.7']|Link[@abbr = '4.4']|Link[@abbr = '4.4.1']|Link[@abbr = '4.4.2']|Link[@abbr = '4.4.3']|Link[@abbr = '4.4.4']|Link[@abbr = '4.4.5']|Link[@abbr = '4.4.6']|Link[@abbr = '4.4.7']|Link[@abbr = '4.5']|Link[@abbr = '4.5.1']|Link[@abbr = '4.5.1.1']|Link[@abbr = '4.5.1.2']|Link[@abbr = '4.5.2']|Link[@abbr = '4.5.2.1']|Link[@abbr = '4.5.2.10']|Link[@abbr = '4.5.2.2']|Link[@abbr = '4.5.2.3']|Link[@abbr = '4.5.2.4']|Link[@abbr = '4.5.2.5']|Link[@abbr = '4.5.2.6']|Link[@abbr = '4.5.2.7']|Link[@abbr = '4.5.2.8']|Link[@abbr = '4.5.2.9']|Link[@abbr = '4.6']|Link[@abbr = '4.6.1']|Link[@abbr = '4.6.2']|Link[@abbr = '4.6.3']|Link[@abbr = '4.6.4']|Link[@abbr = '4.6.5']|Link[@abbr = '4.6.6']|Link[@abbr = '4.6.7']|Link[@abbr = '4.7']|Link[@abbr = '4.7.1']|Link[@abbr = '4.7.1.1']|Link[@abbr = '4.7.1.2']|Link[@abbr = '4.7.1.3']|Link[@abbr = '4.7.1.4']|Link[@abbr = '4.7.2']|Link[@abbr = '4.7.2.1']|Link[@abbr = '4.7.2.2']|Link[@abbr = '4.7.2.3']|Link[@abbr = '4.7.2.4']|Link[@abbr = '4.7.2.5']|Link[@abbr = '4.7.2.6']|Link[@abbr = '4.7.3']|Link[@abbr = '4.7.3.1']|Link[@abbr = '4.7.3.2']|Link[@abbr = '4.7.3.3']|Link[@abbr = '4.7.3.4']|Link[@abbr = '4.7.3.5']|Link[@abbr = '4.7.3.6']|Link[@abbr = '4.7.3.7']|Link[@abbr = '4.8']|Link[@abbr = '4.8.1']|Link[@abbr = '4.8.1.1']|Link[@abbr = '4.8.1.10']|Link[@abbr = '4.8.1.11']|Link[@abbr = '4.8.1.12']|Link[@abbr = '4.8.1.13']|Link[@abbr = '4.8.1.14']|Link[@abbr = '4.8.1.2']|Link[@abbr = '4.8.1.3']|Link[@abbr = '4.8.1.4']|Link[@abbr = '4.8.1.5']|Link[@abbr = '4.8.1.6']|Link[@abbr = '4.8.1.7']|Link[@abbr = '4.8.1.8']|Link[@abbr = '4.8.1.9']|Link[@abbr = '4.8.2']|Link[@abbr = '4.8.2.1']|Link[@abbr = '4.8.2.2']|Link[@abbr = '4.8.2.3']|Link[@abbr = '4.8.2.4']|Link[@abbr = '4.8.2.5']|Link[@abbr = '4.8.2.6']|Link[@abbr = '4.8.2.7']|Link[@abbr = '4.9']|Link[@abbr = '4.9.1']|Link[@abbr = '4.9.1.1']|Link[@abbr = '4.9.1.2']|Link[@abbr = '4.9.1.3']|Link[@abbr = '4.9.1.4']|Link[@abbr = '4.9.2']|Link[@abbr = '4.9.3']|Link[@abbr = '4.9.3.1']|Link[@abbr = '4.9.3.2']|Link[@abbr = '4.9.3.3']|Link[@abbr = '4.9.4']|Link[@abbr = '4.9.5']|Link[@abbr = '4.9.6']|Link[@abbr = '5']|Link[@abbr = '5.1']|Link[@abbr = '5.1.1']|Link[@abbr = '5.1.1.1']|Link[@abbr = '5.1.1.2']|Link[@abbr = '5.1.1.3']|Link[@abbr = '5.1.2']|Link[@abbr = '5.1.2.1']|Link[@abbr = '5.1.2.2']|Link[@abbr = '5.1.2.3']|Link[@abbr = '5.1.3']|Link[@abbr = '5.1.3.1']|Link[@abbr = '5.1.3.2']|Link[@abbr = '5.1.3.3']|Link[@abbr = '5.1.4']|Link[@abbr = '5.1.5']|Link[@abbr = '5.1.6']|Link[@abbr = '5.10']|Link[@abbr = '5.10.1']|Link[@abbr = '5.10.1.1']|Link[@abbr = '5.10.1.2']|Link[@abbr = '5.10.2']|Link[@abbr = '5.10.2.1']|Link[@abbr = '5.10.2.10']|Link[@abbr = '5.10.2.11']|Link[@abbr = '5.10.2.12']|Link[@abbr = '5.10.2.13']|Link[@abbr = '5.10.2.14']|Link[@abbr = '5.10.2.15']|Link[@abbr = '5.10.2.16']|Link[@abbr = '5.10.2.17']|Link[@abbr = '5.10.2.2']|Link[@abbr = '5.10.2.3']|Link[@abbr = '5.10.2.4']|Link[@abbr = '5.10.2.5']|Link[@abbr = '5.10.2.6']|Link[@abbr = '5.10.2.7']|Link[@abbr = '5.10.2.8']|Link[@abbr = '5.10.2.9']|Link[@abbr = '5.10.3']|Link[@abbr = '5.10.3.1']|Link[@abbr = '5.10.3.2']|Link[@abbr = '5.10.3.3']|Link[@abbr = '5.10.3.4']|Link[@abbr = '5.10.3.5']|Link[@abbr = '5.10.3.6']|Link[@abbr = '5.10.4']|Link[@abbr = '5.10.4.1']|Link[@abbr = '5.10.5']|Link[@abbr = '5.10.5.1']|Link[@abbr = '5.10.5.2']|Link[@abbr = '5.10.5.3']|Link[@abbr = '5.10.5.4']|Link[@abbr = '5.10.6']|Link[@abbr = '5.10.6.1']|Link[@abbr = '5.10.6.2']|Link[@abbr = '5.10.7']|Link[@abbr = '5.11']|Link[@abbr = '5.11.1']|Link[@abbr = '5.11.1.1']|Link[@abbr = '5.11.1.2']|Link[@abbr = '5.11.2']|Link[@abbr = '5.11.2.1']|Link[@abbr = '5.11.2.2']|Link[@abbr = '5.11.2.3']|Link[@abbr = '5.11.2.4']|Link[@abbr = '5.11.2.5']|Link[@abbr = '5.11.2.6']|Link[@abbr = '5.11.3']|Link[@abbr = '5.11.3.1']|Link[@abbr = '5.11.3.2']|Link[@abbr = '5.11.4']|Link[@abbr = '5.11.4.1']|Link[@abbr = '5.11.4.10']|Link[@abbr = '5.11.4.2']|Link[@abbr = '5.11.4.3']|Link[@abbr = '5.11.4.4']|Link[@abbr = '5.11.4.5']|Link[@abbr = '5.11.4.6']|Link[@abbr = '5.11.4.7']|Link[@abbr = '5.11.4.8']|Link[@abbr = '5.11.4.9']|Link[@abbr = '5.11.5']|Link[@abbr = '5.11.5.1']|Link[@abbr = '5.11.5.2']|Link[@abbr = '5.11.5.3']|Link[@abbr = '5.12']|Link[@abbr = '5.12.1']|Link[@abbr = '5.12.1.1']|Link[@abbr = '5.12.1.2']|Link[@abbr = '5.12.1.3']|Link[@abbr = '5.12.1.4']|Link[@abbr = '5.12.1.5']|Link[@abbr = '5.12.2']|Link[@abbr = '5.12.2.1']|Link[@abbr = '5.12.2.10']|Link[@abbr = '5.12.2.11']|Link[@abbr = '5.12.2.12']|Link[@abbr = '5.12.2.13']|Link[@abbr = '5.12.2.14']|Link[@abbr = '5.12.2.2']|Link[@abbr = '5.12.2.3']|Link[@abbr = '5.12.2.4']|Link[@abbr = '5.12.2.5']|Link[@abbr = '5.12.2.6']|Link[@abbr = '5.12.2.7']|Link[@abbr = '5.12.2.8']|Link[@abbr = '5.12.2.9']|Link[@abbr = '5.12.3']|Link[@abbr = '5.12.3.1']|Link[@abbr = '5.12.3.10']|Link[@abbr = '5.12.3.11']|Link[@abbr = '5.12.3.12']|Link[@abbr = '5.12.3.13']|Link[@abbr = '5.12.3.14']|Link[@abbr = '5.12.3.15']|Link[@abbr = '5.12.3.16']|Link[@abbr = '5.12.3.17']|Link[@abbr = '5.12.3.2']|Link[@abbr = '5.12.3.3']|Link[@abbr = '5.12.3.4']|Link[@abbr = '5.12.3.5']|Link[@abbr = '5.12.3.6']|Link[@abbr = '5.12.3.7']|Link[@abbr = '5.12.3.8']|Link[@abbr = '5.12.3.9']|Link[@abbr = '5.12.4']|Link[@abbr = '5.12.4.1']|Link[@abbr = '5.12.4.2']|Link[@abbr = '5.12.4.3']|Link[@abbr = '5.12.4.4']|Link[@abbr = '5.12.4.5']|Link[@abbr = '5.12.4.6']|Link[@abbr = '5.12.4.7']|Link[@abbr = '5.12.5']|Link[@abbr = '5.12.5.1']|Link[@abbr = '5.12.5.10']|Link[@abbr = '5.12.5.11']|Link[@abbr = '5.12.5.12']|Link[@abbr = '5.12.5.13']|Link[@abbr = '5.12.5.14']|Link[@abbr = '5.12.5.15']|Link[@abbr = '5.12.5.16']|Link[@abbr = '5.12.5.17']|Link[@abbr = '5.12.5.2']|Link[@abbr = '5.12.5.3']|Link[@abbr = '5.12.5.4']|Link[@abbr = '5.12.5.5']|Link[@abbr = '5.12.5.6']|Link[@abbr = '5.12.5.7']|Link[@abbr = '5.12.5.8']|Link[@abbr = '5.12.5.9']|Link[@abbr = '5.12.6']|Link[@abbr = '5.12.6.1']|Link[@abbr = '5.12.6.2']|Link[@abbr = '5.12.6.3']|Link[@abbr = '5.12.6.4']|Link[@abbr = '5.12.6.5']|Link[@abbr = '5.12.6.6']|Link[@abbr = '5.12.6.7']|Link[@abbr = '5.12.6.8']|Link[@abbr = '5.12.6.9']|Link[@abbr = '5.12.7']|Link[@abbr = '5.12.7.1']|Link[@abbr = '5.12.7.10']|Link[@abbr = '5.12.7.11']|Link[@abbr = '5.12.7.12']|Link[@abbr = '5.12.7.13']|Link[@abbr = '5.12.7.14']|Link[@abbr = '5.12.7.2']|Link[@abbr = '5.12.7.3']|Link[@abbr = '5.12.7.4']|Link[@abbr = '5.12.7.5']|Link[@abbr = '5.12.7.6']|Link[@abbr = '5.12.7.7']|Link[@abbr = '5.12.7.8']|Link[@abbr = '5.12.7.9']|Link[@abbr = '5.12.8']|Link[@abbr = '5.12.8.1']|Link[@abbr = '5.12.8.2']|Link[@abbr = '5.12.8.3']|Link[@abbr = '5.12.8.4']|Link[@abbr = '5.12.8.5']|Link[@abbr = '5.12.8.6']|Link[@abbr = '5.2']|Link[@abbr = '5.2.1']|Link[@abbr = '5.2.1.1']|Link[@abbr = '5.2.1.2']|Link[@abbr = '5.2.1.3']|Link[@abbr = '5.2.1.4']|Link[@abbr = '5.2.1.5']|Link[@abbr = '5.2.1.6']|Link[@abbr = '5.2.1.7']|Link[@abbr = '5.2.1.8']|Link[@abbr = '5.2.2']|Link[@abbr = '5.2.2.1']|Link[@abbr = '5.2.2.2']|Link[@abbr = '5.2.2.3']|Link[@abbr = '5.2.2.4']|Link[@abbr = '5.2.2.5']|Link[@abbr = '5.2.2.6']|Link[@abbr = '5.2.2.7']|Link[@abbr = '5.3']|Link[@abbr = '5.3.1']|Link[@abbr = '5.3.2']|Link[@abbr = '5.3.2.1']|Link[@abbr = '5.3.2.2']|Link[@abbr = '5.4']|Link[@abbr = '5.4.1']|Link[@abbr = '5.4.2']|Link[@abbr = '5.4.3']|Link[@abbr = '5.4.4']|Link[@abbr = '5.4.5']|Link[@abbr = '5.4.6']|Link[@abbr = '5.4.7']|Link[@abbr = '5.5']|Link[@abbr = '5.5.1']|Link[@abbr = '5.5.2']|Link[@abbr = '5.5.2.1']|Link[@abbr = '5.5.2.2']|Link[@abbr = '5.5.2.3']|Link[@abbr = '5.5.2.4']|Link[@abbr = '5.5.2.5']|Link[@abbr = '5.6']|Link[@abbr = '5.6.1']|Link[@abbr = '5.6.1.1']|Link[@abbr = '5.6.1.2']|Link[@abbr = '5.6.1.3']|Link[@abbr = '5.6.1.4']|Link[@abbr = '5.6.1.5']|Link[@abbr = '5.6.1.6']|Link[@abbr = '5.6.2']|Link[@abbr = '5.7']|Link[@abbr = '5.7.1']|Link[@abbr = '5.7.1.1']|Link[@abbr = '5.7.1.1.1']|Link[@abbr = '5.7.1.1.2']|Link[@abbr = '5.7.1.1.3']|Link[@abbr = '5.7.1.1.4']|Link[@abbr = '5.7.1.2']|Link[@abbr = '5.7.1.2.1']|Link[@abbr = '5.7.1.2.2']|Link[@abbr = '5.7.1.2.3']|Link[@abbr = '5.7.1.3']|Link[@abbr = '5.7.1.3.1']|Link[@abbr = '5.7.1.3.2']|Link[@abbr = '5.7.1.3.3']|Link[@abbr = '5.7.2']|Link[@abbr = '5.7.2.1']|Link[@abbr = '5.7.2.2']|Link[@abbr = '5.7.2.3']|Link[@abbr = '5.7.2.4']|Link[@abbr = '5.7.2.5']|Link[@abbr = '5.7.2.6']|Link[@abbr = '5.7.2.7']|Link[@abbr = '5.7.3']|Link[@abbr = '5.7.3.1']|Link[@abbr = '5.7.3.2']|Link[@abbr = '5.7.3.3']|Link[@abbr = '5.7.3.4']|Link[@abbr = '5.7.3.5']|Link[@abbr = '5.7.4']|Link[@abbr = '5.8']|Link[@abbr = '5.8.1']|Link[@abbr = '5.8.1.1']|Link[@abbr = '5.8.1.2']|Link[@abbr = '5.8.1.3']|Link[@abbr = '5.8.2']|Link[@abbr = '5.8.2.1']|Link[@abbr = '5.8.2.2']|Link[@abbr = '5.8.3']|Link[@abbr = '5.9']|Link[@abbr = '5.9.1']|Link[@abbr = '5.9.2']|Link[@abbr = '5.9.3']|Link[@abbr = '5.9.4']|Link[@abbr = '5.9.5']|Link[@abbr = '6']|Link[@abbr = '6.1']|Link[@abbr = '6.2']|Link[@abbr = '6.3']|Link[@abbr = '6.4']|Link[@abbr = '6.5']|Link[@abbr = '6.6']|Link[@abbr = '6.7']|Link[@abbr = '6.8']|Link[@abbr = '7']|Link[@abbr = '7.1']|Link[@abbr = '7.2']">
		 <SemanticDomains5016>
			<xsl:for-each select="Link">
			   <xsl:choose>
				  <xsl:when test="@abbr = '1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.2.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.1.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.1.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.1.2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.1.2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.2.1.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.1.2.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.1.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.1.1.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.1.1.2</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.1.1.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.1.2.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.1.3.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.1.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.2.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.1.3.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.2.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.1.3.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.2.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.1.3.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.2.1.4</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.2.1.5</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.2.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.1.3.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.3.1.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.1.3.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.1.3.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.2.1.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.1.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.6.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.1.4.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.6.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.1.4.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.6.7.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.1.4.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.6.7.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.1.4.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.6.7.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.1.4.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.2.9</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.1.4.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.5.4.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.2.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.2.2.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.1.2</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.2.3.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.2.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">5.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.2.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.2.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.2.2.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.2.2.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.2.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.2.2.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.2.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.2.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">5.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.3.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">5.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.3.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">5.2.3.3</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">5.2.3.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.2.8</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.4.5.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.2.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.7.5</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.5.4.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.2.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.4.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.2.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.7.7.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.2.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8.3.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.2.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.7.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.2.8'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.2.3.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.2.9'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.2.8</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.4.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.3.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.4.2.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.3.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.4.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.8.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">5.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.5.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9.8.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.5.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.3.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.5.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">5.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.5.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.7.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">5.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.6.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">5.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.6.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">5.4.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.5.7.2</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">5.4.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.7.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.2.5.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.7.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.5.7.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.7.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">5.4.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.8'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.8.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.7.8</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.8.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.3.1.9</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.9'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.2.6</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.6.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.9.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.9.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.7.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.7.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.9.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.2.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.4.9.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.6.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.5.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.5.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.5.1.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.5.1.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.5.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.5.1.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.5.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.5.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.5.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.5.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.6.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.5.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.5.1.5</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.5.2.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.5.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.5.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.5.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.5.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.2.1.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.7.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.2.1.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.7.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.7.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.6.6.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.7.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.1.9.8</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.7.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.5.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.7.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8.3.6</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8.3.6.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.7.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8.3.6.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.7.8'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.5</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.6.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.7.9'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.1.3.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '1.8'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.1.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.1.10'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.5.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.1.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.5.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.1.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.5.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.1.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.5.5</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">5.2.3.1.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.1.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.5.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.1.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.2.5.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.1.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.5.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.1.8'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.5.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.1.9'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.5.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.6.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.6.1.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.2.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.6.1.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.2.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.6.1.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.2.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.6.1.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.3.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.6.6.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.3.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.3.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.4.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.4.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.6.7</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.6.5.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.4.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.6.7</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.6.5.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.4.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.6.4.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.4.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.7.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.1.9</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.5.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.1.9.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.5.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.1.9.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.5.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.1.9.1.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.5.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.1.9.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '2.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.6.1.9</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '3.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '3.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '3.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.1.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.1.1.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.1.1.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.1.1.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.1.1.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.1.2.3.4</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.1.1.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.1.1.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.1.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.1.1.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.1.2.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.1.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.5.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.1.2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.5.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.1.2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.3.4.3</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.5.9</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.1.2.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.7.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.1.2.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.4.5.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.1.2.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">5.9</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.10'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.10.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.10.10'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.4.2.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.10.10.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.4.2.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.4.2.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.10.10.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.4.2.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.10.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.4.1.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.10.2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.4.1.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.10.2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.4.1.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.10.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.6.6.4</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.6.4</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.6.5</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.2.8</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.2.8.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.10.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.4.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.10.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.4.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.10.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.4.2.1.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.4.2.1.2</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.4.2.1.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.10.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.4.2.2.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.10.8'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.4.1.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.10.9'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.4.2.4.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.10.9.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.4.1.2.2</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.4.2.4.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.10.9.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.4.2.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.1.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.1.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.6.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.1.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.7.5.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.1.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.2.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.1.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.2.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.1.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.1.2.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.1.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.1.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.6.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.2.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.2.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.3.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.3.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.3.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.6.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.3.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.3.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.6.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.3.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.4.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.4.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.4.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.4.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.3.1.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.4.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.4.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.3.1.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.4.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.5.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.5.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.4.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.5.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.5.4</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.5.4.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.5.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.5.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.4.4.6.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.5.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.5</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.5.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.5.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.5.8'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.5.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.5.9'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.5.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9.7.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.6.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.6.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.6.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.6.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.1.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.11.6.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.1.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.12'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.5.8</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.1.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.1.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.1.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.1.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.1.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.1.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.7.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.1.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.7.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.1.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.2.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.2.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.1.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.2.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.1.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.2.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.1.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.2.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9.5.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.1.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.7.5.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.1.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.5.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.2.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.8.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.7.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.3.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9.4.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.3.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.9</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.3.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.7.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.3.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9.4.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.4.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.4.10'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.3.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.4.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.2.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.4.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.5.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.4.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.8.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.4.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.8</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.4.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.7.5.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.4.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.7.5.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.4.8'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.4.9'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.6.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.8.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.5.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.8.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.5.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.8.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.7.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.6.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.7.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.6.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.6.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.7.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.6.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.7.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.2.7.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.8</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.3.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.3.2</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.3.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9.5.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.3.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.5</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.5.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.3.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.3.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.3.3.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.4.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.3.3.8</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.4.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.3.3.2</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.3.3.3</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.3.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.4.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.3.3.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.4.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.4.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.4.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.4.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.2.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.4.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.3.3.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.4.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.5.3.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.4.8'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8.4.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.4.9'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.3.3.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.7.5.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.5.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.7.5.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.5.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9.4.3</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9.4.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.5.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.2.3.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.5.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.7.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.1.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.13.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.6.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.14'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.14.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.14.1.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.4.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.14.1.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.5.4.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.14.1.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.4.5.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.14.1.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.4.5.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.14.1.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.4.5.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.14.1.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.6.4.2.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.14.1.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.1.9.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.14.1.8'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.3.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.14.1.9'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.5.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.14.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.5.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.14.2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.5.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.14.2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.5.3.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.14.2.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.5.4.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.14.2.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.5.4.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.14.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.6.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.14.3.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.6.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.14.3.1.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.6.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.14.3.1.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.3.3.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.14.3.1.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.3.4.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.5.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.14.3.1.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.6.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.14.3.1.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.5.3.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.14.3.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.6.6.1.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.6.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.14.3.2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.6.6.1.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.6.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.14.3.2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8.2.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.14.3.2.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.7.7.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.14.3.2.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.7.7.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.14.3.2.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.3.4.5</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.6.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.14.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.7.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.14.4.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.7.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.14.4.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.7.7.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.15'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.15.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.15.1.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.15.1.1.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.1.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.15.1.1.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.1.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.15.1.1.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8.2.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.15.1.1.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.15.1.1.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.5.4.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.15.1.1.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8.2.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.15.1.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.15.1.2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.7.9.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.15.1.2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.15.1.2.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8.2.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.15.1.2.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.15.1.2.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8.2.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.15.1.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.5.4</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8.3.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.15.1.3.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.5.4</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8.3.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.15.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8.4.9</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.15.2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8.4.9</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.15.2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8.4.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.16'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.16.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.16.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.9.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.16.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.16.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.5.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.16.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.8</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8.4.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.1.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.1.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.1.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.1.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.1.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.1.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.1.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.2.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.1.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.1.2.3</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.1.2.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.1.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.1.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.10'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.4.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.11'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.11.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8.3.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.11.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8.2</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.11.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8.3.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.12'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.12.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.7.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.12.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.7.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.12.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.7.5.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.12.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.7.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.12.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.7.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.12.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.7.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.12.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.7.4.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.12.8'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.7.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.4.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">5.2.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.3.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.5.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.3.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.5.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">5.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.5.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.3.3</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.3.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.6.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.6.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.6.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.6.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">5.3.7</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">5.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.2.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.7.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.2.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.7.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.2.2</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.2.2.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.2.2.2</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.2.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.8'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.6.6.3</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.6.6.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.9'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.9.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.9.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9.5.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.9.2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9.5.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.9.2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9.5.8</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.9.2.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9.5.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.9.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9.5.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.9.3.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9.5.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.9.3.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9.5.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.9.3.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9.9</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.9.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.9.4.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9.5.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.9.4.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9.5.9</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.9.4.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9.7.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.9.4.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9.4.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.17.9.4.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9.4.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.18'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.8</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.18.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.8.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.18.1.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.8.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.18.1.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.1.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.18.1.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.8.1.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.18.1.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.4.6</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.1.7.2</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.1.7.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.18.1.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.8.1.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.18.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.8.3.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.4.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.18.2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.8.3.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.4.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.18.2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.8.4.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.18.2.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.8.4.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.18.2.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.8.5.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.18.2.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.8.1.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.18.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.4.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.18.3.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.8.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.18.3.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.4.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.18.3.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.8.2.2</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.8.2.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.18.3.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.8.5.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.18.3.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.8.9.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.18.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.8.4.9</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.18.4.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.8.4.9</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.5.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.18.4.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.8.4.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.18.4.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.8.4.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.18.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.8.1.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.18.5.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.8.8</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.18.5.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.8.1.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.18.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.8.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.1.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.2.1.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.1.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.2.1.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.1.3.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.2.1.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.1.3.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.3.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.2.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.2.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.3.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.2.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">5.5.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.2.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.3.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.2.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.2.1.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.1.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.1.10'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.5.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.1.11'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.2.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.1.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.1.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.3.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.1.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.3.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.1.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.2.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.1.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.2.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.1.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.2.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.1.8'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.5.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.4.5.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.1.9'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.5.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.10'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.2.8</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.3.1.3</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.3.4.7</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.3.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.10.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.2.8</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.3.1.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.1.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.2.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.1.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.3.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.4.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.3.3</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.3.3.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.4.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.3.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.2.5.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.6.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.5.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.6.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.5.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.6.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.3.3.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.6.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.3.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.6.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.6.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.3.2.8</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.6.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.3.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.7.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.5</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.5.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.7.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.5.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.8'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.1.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.9'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.9.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.9.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.1.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.9.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.1.1.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.9.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.2.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.9.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.4.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.9.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.4.2.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.3.9.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.2.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.4.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.4.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.1.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.4.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.1.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.4.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.1.4</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.1.4.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.1.4.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.4.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.1.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.4.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.1.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.4.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.1.8</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.5.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.3.4.4</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.5.2</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.5.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.5.1.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.3.4.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.5.1.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.3.2.4.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.5.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.5.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.5.2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.5.2.10'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.6.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.6.1.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.5.2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.1.7</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.2.1.8.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.5.2.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.2.1.8.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.5.2.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.1.6.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.5.2.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.1.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.5.2.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.2.1.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.5.2.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.2.1.4.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.5.2.8'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.2.1.4.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.5.2.9'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.4.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.6.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.7.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.6.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.8.3</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.8.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.6.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.8.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.6.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.8.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.6.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.7.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.6.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.8.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.6.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.7.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.7.4</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.7.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8.2</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8.2.8</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.7.1.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8.2.8</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.7.1.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.5.3</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.9.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.7.1.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.9.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.7.1.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.6.6.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.7.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.4.2.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.7.2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.4.2.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.7.2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.4.4.8</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.7.2.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.4.4.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.7.2.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.4.4.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.7.2.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.4.4.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.7.2.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9.5.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.7.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.7.3.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.4.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.7.3.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.4.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.7.3.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.4.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.7.3.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.4.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.7.3.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.4.4.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.7.3.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.4.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.7.3.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.4.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.8'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.8.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.8.1.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">5.2.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.8.1.10'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.2.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.8.1.11'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.5.6</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.6.4.6</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.2.2.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.8.1.12'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.5.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.8.1.13'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.7.8</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.8.1.14'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.8.1.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.1.1.4</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.2.2</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">5.2.2.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.8.1.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.6.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.8.1.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.6.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.8.1.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">5.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.8.1.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.4.4</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.4.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.8.1.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.6</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.6.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.8.1.8'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.8.1.9'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.5.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.5.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.8.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.8.2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.3.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.5.4.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.8.2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.3.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.8.2.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.3.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.8.2.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.3.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.8.2.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.3.5</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.3.4.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.8.2.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.5.6.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.8.2.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.9'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.9.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.9.1.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.9.1.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.3.1.8</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.9.1.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.7.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.2.7.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.9.1.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.3.2.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.9.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.3.1.6</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.4.1.4.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.9.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.7.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.9.3.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.4.1.1.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.9.3.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.4.1.1.8</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.4.1.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.9.3.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.7.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.9.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.4.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.9.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.3.1.6</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.1.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '4.9.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.4.1.1.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.1.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.1.1.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.5.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.1.1.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.3.1.8</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.1.1.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.5.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.1.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.1.2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.1.2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.5.2.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.1.2.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.5.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.1.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.5.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.1.3.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.5.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.1.3.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.5.3.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.1.3.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.5.3.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.1.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.5.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.1.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.4.6.5</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.4.6.5.3</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.4.6.5.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.1.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.1.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.1.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.1.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.1.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.1.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.7.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.1.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.2.10'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.3.3</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">1.3.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.2.11'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.2.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.2.12'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.2.13'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.5.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.2.14'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.6.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.2.15'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.6.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.2.16'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.7.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.2.17'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.6.3</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.6.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.3.1.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.2.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.3.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.2.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.3.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.2.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">5.6.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.2.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">5.6.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.2.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.7.8.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.2.8'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.4.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.4.2</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.6.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.2.9'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.4</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.4.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.2.8</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.3.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.1.3</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.1.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.3.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.3.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.3.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.2</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.2.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.3.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.2.9.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.2.9.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.3.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.2.9</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.3.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.4.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.3.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.3.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.5.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.3.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.5.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.3.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.5.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.3.7.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.5.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.1.5.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.3.1.8</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.6.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.3.1.8.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.3.1.8.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.6.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.8.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.10.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.11'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.5.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.11.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.5.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.11.1.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.5.4</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.5.4.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.11.1.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.5.4.2</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.3.6.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.11.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.2.8</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.11.2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.2.8</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.11.2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.2.2.2</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.2.6.3</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.2.6.4</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.2.6.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.11.2.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.2.2</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.2.2.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.2.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.11.2.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.2.3</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.2.3.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.2.3.2</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.2.3.3</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.2.4</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.2.4.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.11.2.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.2.5.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.11.2.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.2.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.11.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.5.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.11.3.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.5.2.8</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.11.3.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.5.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.11.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.11.4.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.5.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.11.4.10'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.5.1.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.11.4.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.11.4.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.5.1.2.2</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.5.1.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.11.4.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.5.1.2.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.5.1.4.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.11.4.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.2.6.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.2.6.2</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.5.1.2</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.5.1.5</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.5.1.5.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.11.4.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.5.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.11.4.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.5.1.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.11.4.8'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.5.1.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.11.4.9'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.5.1.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.11.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.3.4.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.11.5.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.3.4.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.11.5.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.3.4.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.11.5.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.3.4.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.5.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.1.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.5.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.1.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.5.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.1.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.5.6.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.1.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.5.6.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.1.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.5.4.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.2.10'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.4.4.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.2.11'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.6</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.6.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.2.12'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.1.3.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.2.13'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.1.3</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.1.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.2.14'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.8.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.2.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.9.5.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.2.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.1.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.2.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.2.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.1.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.2.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.2.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.2.8'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.4.4.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.2.9'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.3.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.1.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.7.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.3.10'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.4.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.3.11'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.1.2.4.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.3.12'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.6.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.3.13'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.3.14'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.3.15'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">2.6.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.3.16'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">5.2.3.7.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.3.17'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.3.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.4.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.3.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.7.9.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.3.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.7.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.3.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.8.9.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.3.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.4.2.2.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.3.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.5.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.3.8'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.5.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.3.9'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.7.9.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.4.2.1.2</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.4.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.4.2.1.8</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.4.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.4.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.4.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.4.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.4.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.2.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.4.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.4.2.1.2</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.4.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.3.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.4.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.3.1.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.5.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.5.10'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.6.1.8</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.5.11'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.5.12'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.6.1.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.5.13'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.6.1.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.5.14'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.6.1.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.5.15'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.6.1.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.5.16'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.5.17'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.5.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.6.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.5.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.6.2.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.5.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.6.2.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.5.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.6.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.5.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.6.2.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.5.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.6.1.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.5.8'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.6.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.5.9'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.5.3.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.6.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.6.2.5</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.6.2.5.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.6.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.6.2.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.6.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.3.1.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.6.2.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.6.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.6.2.8</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.6.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.6.2.9</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.6.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.5.1.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.6.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.5.1.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.6.8'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.5.1.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.6.9'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.4.5.2.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.7.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.7.10'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.7.11'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.7.12'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.7.13'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.7.14'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.7.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.5.1.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.7.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.7.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.7.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.7.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.7.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.7.8'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.7.9'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.5.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.8'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.6.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.8.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.6.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.8.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.6.3.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.8.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.6.3.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.8.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.6.3.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.8.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.6.3.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.12.8.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.4.3</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.3.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.2.1.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.1.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.2.1.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.1.3.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.1.3.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.2.1.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.1.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.2.1.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.1.8</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.1.8.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.2.1.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.1.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.2.1.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.1.3.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.1.7.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.2.1.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.1.4.2</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.1.4.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.2.1.8'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.5.2.3</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.1.2.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.2.2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.2.2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.1.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.2.2.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.1.1.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.2.2.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.1.1.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.2.2.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.1.1.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.2.2.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.1.1.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.2.2.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.1.1.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.3.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.4.5.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.3.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.3.2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.3.2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.5.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.1.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.4.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.1.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.4.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">4.1.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.4.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.5.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.4.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.1.6.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.4.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.1.7.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.4.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.8</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.4.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.5.1.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.5.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.5.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.5.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.5.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.5.2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.3.1.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.3.1.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.5.2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.3.1.3</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.3.1.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.5.2.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.1.5.8.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.5.2.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.3.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.5.2.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.7.9</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.6.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.7.9</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.6.1.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.7.9</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.6.1.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.7.9</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.6.1.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.6.1.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.1.2.2.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.1.2.2.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.6.1.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.1.2.2.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.1.2.2.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.6.1.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.7.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.6.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.7.7</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.3.7.7.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.7.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.7.1.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.4.1.2.3</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.4.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.7.1.1.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.4.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.7.1.1.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.4.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.7.1.1.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.4.6.1.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.7.1.1.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.4.1.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.7.1.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.4.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.7.1.2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.4.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.7.1.2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.4.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.7.1.2.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.4.5.2.2</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.4.5.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.7.1.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.4.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.7.1.3.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.4.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.7.1.3.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.4.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.7.1.3.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.4.1.2.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.7.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.4.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.7.2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.4.6.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.7.2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.4.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.7.2.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.1.2.3.5</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.1.3.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.7.2.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">7.2.7.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.7.2.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.1.2.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.7.2.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.1.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.7.2.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.4.6.4.4</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.4.8</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">8.4.8.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.7.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.4.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.7.3.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.4.4.4</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.4.4.9</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.7.3.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.4.4.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.7.3.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.4.4.1</xsl:attribute>
					 </Link>
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.4.4.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.7.3.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.4.3.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.7.3.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.4.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.7.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.3.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.8'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.4.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.8.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.4.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.8.1.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.4.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.8.1.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.4.6.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.8.1.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.4.6.2</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.8.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.8.2.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.8.2.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.8.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">3.5.1.3.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.9'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.4.2.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.9.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.4.2.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.9.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.1.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.9.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.1.2.3.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.9.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.1.2.3.4</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '5.9.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">6.1.2.6</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '6.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '6.2'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '6.3'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '6.4'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '6.5'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.2.3.1</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '6.6'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.2.3.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '6.7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.2.3.5</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '6.8'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.2.3</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '7'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.7</xsl:attribute>
					 </Link>
				  </xsl:when>
				  <xsl:when test="@abbr = '7.1'">
					 <Link>
						<xsl:attribute name="ws">
						   <xsl:value-of select="@ws"/>
						</xsl:attribute>
						<xsl:attribute name="abbr">9.7.1</xsl:attribute>
					 </Link>
				  </xsl:when>
			   </xsl:choose>
			</xsl:for-each>
		 </SemanticDomains5016>
	  </xsl:if>
   </xsl:template>

   <!-- Delete these tags -->

   <xsl:template
	  match="IsBodyWithHeadword5007 | SubentryType5007 | SubentryTypes5005 | CrossReferences5002 | LexicalRelations5016 | Sound5014 | SourceSegment5004"/>

	<!-- Delete elements with empty writing systems (arising from ignore requests in the configuration).
	Mainly we want to delete useless alternatives, but also bad runs in multilingual strings in a different main WS,
	and entire reversal indexes in unwanted ones.-->
	<xsl:template match="Link[@ws='']"/>
	<xsl:template match="AStr[@ws='']"/>
	<xsl:template match="AUni[@ws='']"/>
	<xsl:template match="Run[@ws='']"/>
	<xsl:template match="ReversalIndex[WritingSystem5052/Link/@ws='']"/>

	<!-- Insert the DTD statement at the root -->

   <xsl:template match="/">
	  <!-- <xsl:text disable-output-escaping="yes">&lt;!DOCTYPE FwDatabase SYSTEM "FwDatabase.dtd"&gt;</xsl:text> -->
	  <xsl:copy>
		 <xsl:apply-templates select="@* | node()"/>
	  </xsl:copy>
   </xsl:template>

   <!-- Copy everything else -->

   <xsl:template match="* | @*">
	  <xsl:copy>
		 <xsl:apply-templates select="@* | node()"/>
	  </xsl:copy>
   </xsl:template>

   <!-- Copy attributes -->

   <xsl:template match="*" mode="detailed">
	  <xsl:copy>
		 <xsl:apply-templates select="@* | node()"/>
	  </xsl:copy>
   </xsl:template>

   <xsl:template match="@*" mode="detailed">
	  <xsl:attribute name="{name(.)}">
		 <xsl:value-of select="."/>
	  </xsl:attribute>
   </xsl:template>

   <!-- Change "string data" to "Unicode data" -->

   <xsl:template match="AStr" mode="str2uni">
	  <xsl:element name="AUni">
		 <xsl:apply-templates select="@*" mode="detailed"/>
		 <xsl:apply-templates select="*" mode="str2uni"/>
	  </xsl:element>
   </xsl:template>

</xsl:transform>
