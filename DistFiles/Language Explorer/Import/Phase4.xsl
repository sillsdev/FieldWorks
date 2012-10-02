<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:msxsl="urn:schemas-microsoft-com:xslt">
	<xsl:output method="xml" version="1.0" encoding="UTF-8" indent="yes"/>
	<!--
================================================================
Convert SFM Import XML phase 3 to XML phase 4
  Input:    SFM Import phase 3 XML
  Output: SFM Import phase 4 XML
================================================================
Revision History is at the end of this file.

- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Preamble
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Main template
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
<!--
<xsl:text>
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE LexDb SYSTEM "FwDatabase.dtd">
</xsl:text>
-->
	<xsl:template match="@* | node()">
		<xsl:choose>
			<!-- Allomorphs -->
			<xsl:when test="name(.)='Allomorphs'">
				<Allomorphs>
					<xsl:apply-templates select="@*"/>
					<xsl:apply-templates/>
				</Allomorphs>
			</xsl:when>
			<!-- UnderlyingForm -->
			<xsl:when test="name(.)='UnderlyingForm'">
				<UnderlyingForm>
					<xsl:apply-templates select="@*"/>
					<xsl:apply-templates/>
				</UnderlyingForm>
			</xsl:when>
			<!-- b -->
			<xsl:when test="name(.)='b'">
				<Run>
					<xsl:choose>
						<xsl:when test="@ws">
							<xsl:attribute name="ws"><xsl:value-of select="@ws"/></xsl:attribute>
						</xsl:when>
						<xsl:otherwise>
							<xsl:attribute name="ws"><xsl:value-of select="../@ws"/></xsl:attribute>
						</xsl:otherwise>
					</xsl:choose>
					<xsl:attribute name="bold"><xsl:text>on</xsl:text></xsl:attribute>
					<xsl:apply-templates select="@*"/>
					<xsl:apply-templates/>
				</Run>
			</xsl:when>
			<!-- CitationForm -->
			<xsl:when test="name(.)='CitationForm'">
				<xsl:if test="not(preceding-sibling::CitationForm)">
					<CitationForm>
						<xsl:call-template name="DoAUni"/>
						<xsl:for-each select="following-sibling::CitationForm">
							<xsl:call-template name="DoAUni"/>
						</xsl:for-each>
					</CitationForm>
				</xsl:if>
			</xsl:when>
			<!-- CrossReferences -->
			<xsl:when test="name(.)='CrossReferences'">
				<xsl:element name="{name(.)}">
					<xsl:apply-templates/>
				</xsl:element>
			</xsl:when>
			<!-- Restrictions -->
			<xsl:when test="name(.)='Restrictions'">
				<xsl:variable name="eleName">
					<xsl:choose>
						<xsl:when test="name(..)='LexSense'">Restrictions</xsl:when>
						<xsl:otherwise>Restrictions</xsl:otherwise>
					</xsl:choose>
				</xsl:variable>
				<xsl:if test="not(preceding-sibling::Restrictions)">
					<xsl:element name="{$eleName}">
						<xsl:call-template name="DoAUni"/>
						<xsl:for-each select="following-sibling::Restrictions">
							<xsl:call-template name="DoAUni"/>
						</xsl:for-each>
					</xsl:element>
				</xsl:if>
			</xsl:when>
			<!-- CmTranslation -->
			<xsl:when test="name(.)='CmTranslation'">
				<xsl:if test="not(preceding-sibling::CmTranslation)">
					<CmTranslation>
						<Type>
							<Link ws="en" name="Free translation"/>
						</Type>
						<Translation>
							<xsl:call-template name="DoAStr"/>
							<xsl:for-each select="following-sibling::CmTranslation">
								<xsl:call-template name="DoAStr"/>
							</xsl:for-each>
						</Translation>
					</CmTranslation>
				</xsl:if>
			</xsl:when>
			<!-- DateModified -->
			<xsl:when test="name(.)='DateModified'">
				<xsl:call-template name="DoDateField"/>
			</xsl:when>
			<!-- DateCreated -->
			<xsl:when test="name(.)='DateCreated'">
				<xsl:call-template name="DoDateField"/>
			</xsl:when>
			<!-- HomographNumber -->
			<xsl:when test="name(.)='HomographNumber'">
				<xsl:element name="{name(.)}">
					<Integer val="{.}" />
				</xsl:element>
			</xsl:when>
			<!-- IsAbstract -->
			<xsl:when test="name(.)='IsAbstract'">
				<xsl:call-template name="DoBooleanField"/>
			</xsl:when>
			<!-- AlternateForms -->
			<xsl:when test="name(.)='AlternateForms'">
				<xsl:element name="{name(.)}">
					<xsl:apply-templates/>
				</xsl:element>
			</xsl:when>
			<!-- AnthroNote -->
			<xsl:when test="name(.)='AnthroNote'">
				<xsl:if test="not(preceding-sibling::AnthroNote)">
					<xsl:element name="{name(.)}">
						<xsl:call-template name="DoAStr"/>
						<xsl:for-each select="following-sibling::AnthroNote">
							<xsl:call-template name="DoAStr"/>
						</xsl:for-each>
					</xsl:element>
				</xsl:if>
			</xsl:when>
			<!-- AnthroCodes -->
			<xsl:when test="name(.)='AnthroCodes'">
				<xsl:if test="not(preceding-sibling::AnthroCodes)">
					<xsl:element name="AnthroCodes">
						<xsl:apply-templates/>
						<xsl:for-each select="following-sibling::AnthroCodes">
							<xsl:apply-templates/>
						</xsl:for-each>
					</xsl:element>
				</xsl:if>
			</xsl:when>
			<!-- Bibliography -->
<!--
			<xsl:when test="name(.)='Bibliography'">
				<xsl:call-template name="MultiString">
					<xsl:with-param name="className" select="LexSense"/>
					<xsl:with-param name="elementName" select="Bibliography"/>
				</xsl:call-template>
			</xsl:when>
-->
			<!-- Bibliography -->
			<xsl:when test="name(.)='Bibliography'">
				<xsl:variable name="eleName">
					<xsl:choose>
						<xsl:when test="name(..)='LexSense'">Bibliography</xsl:when>
						<xsl:otherwise>Bibliography</xsl:otherwise>
					</xsl:choose>
				</xsl:variable>
				<xsl:if test="not(preceding-sibling::Bibliography)">
					<xsl:element name="{$eleName}">
						<xsl:call-template name="DoAStr"/>
						<xsl:for-each select="following-sibling::Bibliography">
							<xsl:call-template name="DoAStr"/>
						</xsl:for-each>
					</xsl:element>
				</xsl:if>
			</xsl:when>
			<!-- Comment -->
			<xsl:when test="name(.)='Comment'">
				<xsl:if test="not(preceding-sibling::Comment)">
					<xsl:variable name="elementName">
						<xsl:call-template name="DoParentBasedElementName">
							<xsl:with-param name="defaultNumber" select="5002"/>
						</xsl:call-template>
					</xsl:variable>
					<xsl:element name="{$elementName}">
						<xsl:call-template name="DoAStr"/>
						<xsl:for-each select="following-sibling::Comment">
							<xsl:call-template name="DoAStr"/>
						</xsl:for-each>
					</xsl:element>
				</xsl:if>
			</xsl:when>
			<!-- LexEntryRef -->
			<xsl:when test="name(.)='LexEntryRef'">
				<xsl:element name="EntryRefs">
					<xsl:element name="LexEntryRef">
						<xsl:apply-templates/>
					</xsl:element>
				</xsl:element>
			</xsl:when>
			<!-- Summary -->
			<xsl:when test="name(.)='Summary'">
				<xsl:if test="not(preceding-sibling::Summary)">
				   <xsl:variable name="elementName">
					<xsl:call-template name="DoParentBasedElementName">
					  <xsl:with-param name="defaultNumber" select="5127"/>
					</xsl:call-template>
				   </xsl:variable>
				   <xsl:element name="{$elementName}">
					  <xsl:call-template name="DoAStr"/>
					  <xsl:for-each select="following-sibling::Summary">
						<xsl:call-template name="DoAStr"/>
					  </xsl:for-each>
					</xsl:element>
				</xsl:if>
			</xsl:when>
			<!-- Definition -->
			<xsl:when test="name(.)='Definition'">
				<xsl:if test="not(preceding-sibling::Definition)">
					<Definition>
						<xsl:call-template name="DoAStr"/>
						<xsl:for-each select="following-sibling::Definition">
							<xsl:call-template name="DoAStr"/>
						</xsl:for-each>
					</Definition>
				</xsl:if>
			</xsl:when>
			<!-- DiscourseNote -->
			<xsl:when test="name(.)='DiscourseNote'">
				<xsl:if test="not(preceding-sibling::DiscourseNote)">
					<xsl:element name="DiscourseNote">
						<xsl:call-template name="DoAStr"/>
						<xsl:for-each select="following-sibling::DiscourseNote">
							<xsl:call-template name="DoAStr"/>
						</xsl:for-each>
					</xsl:element>
				</xsl:if>
			</xsl:when>
			<!-- DomainTypes -->
			<xsl:when test="name(.)='DomainTypes'">
				<xsl:if test="not(preceding-sibling::DomainTypes)">
					<xsl:element name="DomainTypes">
					  <xsl:apply-templates/>
						<xsl:for-each select="following-sibling::DomainTypes">
						<xsl:apply-templates/>
						</xsl:for-each>
					</xsl:element>
				</xsl:if>
			</xsl:when>
			<!-- LiteralMeaning-->
			<xsl:when test="name(.)='LiteralMeaning'">
				<xsl:if test="not(preceding-sibling::LiteralMeaning)">
					<xsl:element name="{name(.)}">
						<xsl:call-template name="DoAStr"/>
						<xsl:for-each select="following-sibling::LiteralMeaning">
							<xsl:call-template name="DoAStr"/>
						</xsl:for-each>
					</xsl:element>
				</xsl:if>
			</xsl:when>
			<!-- SenseType -->
			<xsl:when test="name(.)='SenseType'">
				<xsl:if test="not(preceding-sibling::SenseType)">
					<xsl:element name="{name(.)}">
					  <xsl:apply-templates/>
						<xsl:for-each select="following-sibling::SenseType">
						<xsl:apply-templates/>
						</xsl:for-each>
					</xsl:element>
				</xsl:if>
			</xsl:when>
			<!-- Status -->
			<xsl:when test="name(.)='Status'">
				<xsl:if test="not(preceding-sibling::Status)">
					<xsl:element name="{name(.)}">
					  <xsl:apply-templates/>
						<xsl:for-each select="following-sibling::Status">
						<xsl:apply-templates/>
						</xsl:for-each>
					</xsl:element>
				</xsl:if>
			</xsl:when>
			<!-- SummaryDefinition-->
			<xsl:when test="name(.)='SummaryDefinition'">
				<xsl:if test="not(preceding-sibling::SummaryDefinition)">
					<xsl:element name="{name(.)}">
						<xsl:call-template name="DoAStr"/>
						<xsl:for-each select="following-sibling::SummaryDefinition">
							<xsl:call-template name="DoAStr"/>
						</xsl:for-each>
					</xsl:element>
				</xsl:if>
			</xsl:when>
			<!-- UsageTypes -->
			<xsl:when test="name(.)='UsageTypes'">
				<xsl:if test="not(preceding-sibling::UsageTypes)">
					<xsl:element name="{name(.)}">
					  <xsl:apply-templates/>
						<xsl:for-each select="following-sibling::UsageTypes">
						<xsl:apply-templates/>
						</xsl:for-each>
					</xsl:element>
				</xsl:if>
			</xsl:when>
			<!-- EncyclopedicInfo -->
			<xsl:when test="name(.)='EncyclopedicInfo'">
				<xsl:if test="not(preceding-sibling::EncyclopedicInfo)">
					<xsl:element name="EncyclopedicInfo">
						<xsl:call-template name="DoAStr"/>
						<xsl:for-each select="following-sibling::EncyclopedicInfo">
							<xsl:call-template name="DoAStr"/>
						</xsl:for-each>
					</xsl:element>
				</xsl:if>
			</xsl:when>
			<!-- GeneralNote -->
			<xsl:when test="name(.)='GeneralNote'">
				<xsl:if test="not(preceding-sibling::GeneralNote)">
					<xsl:element name="GeneralNote">
						<xsl:call-template name="DoAStr"/>
						<xsl:for-each select="following-sibling::GeneralNote">
							<xsl:call-template name="DoAStr"/>
						</xsl:for-each>
					</xsl:element>
				</xsl:if>
			</xsl:when>
			<!-- GrammarNote -->
			<xsl:when test="name(.)='GrammarNote'">
				<xsl:if test="not(preceding-sibling::GrammarNote)">
					<xsl:element name="GrammarNote">
						<xsl:call-template name="DoAStr"/>
						<xsl:for-each select="following-sibling::GrammarNote">
							<xsl:call-template name="DoAStr"/>
						</xsl:for-each>
					</xsl:element>
				</xsl:if>
			</xsl:when>
			<!-- PhonologyNote -->
			<xsl:when test="name(.)='PhonologyNote'">
				<xsl:if test="not(preceding-sibling::PhonologyNote)">
					<xsl:element name="PhonologyNote">
						<xsl:call-template name="DoAStr"/>
						<xsl:for-each select="following-sibling::PhonologyNote">
							<xsl:call-template name="DoAStr"/>
						</xsl:for-each>
					</xsl:element>
				</xsl:if>
			</xsl:when>
			<!-- SemanticsNote -->
			<xsl:when test="name(.)='SemanticsNote'">
				<xsl:if test="not(preceding-sibling::SemanticsNote)">
					<xsl:element name="{name(.)}">
						<xsl:call-template name="DoAStr"/>
						<xsl:for-each select="following-sibling::SemanticsNote">
							<xsl:call-template name="DoAStr"/>
						</xsl:for-each>
					</xsl:element>
				</xsl:if>
			</xsl:when>
			<!-- SocioLinguisticsNote -->
			<xsl:when test="name(.)='SocioLinguisticsNote'">
				<xsl:if test="not(preceding-sibling::SocioLinguisticsNote)">
					<xsl:element name="SocioLinguisticsNote">
						<xsl:call-template name="DoAStr"/>
						<xsl:for-each select="following-sibling::SocioLinguisticsNote">
							<xsl:call-template name="DoAStr"/>
						</xsl:for-each>
					</xsl:element>
				</xsl:if>
			</xsl:when>
			<!-- Entries -->
			<xsl:when test="name(.)='Entries'">
				<Entries>
					<xsl:apply-templates/>
				</Entries>
			</xsl:when>
			<!-- VariantEntryTypes -->
			<xsl:when test="name(.)='VariantEntryTypes'">
				<xsl:element name="{name(.)}">
					<Link>
						<xsl:attribute name="ws"><xsl:value-of select="@ws"/></xsl:attribute>
						<xsl:attribute name="name"><xsl:value-of select="."/></xsl:attribute>
					</Link>
				</xsl:element>
			</xsl:when>
			<!-- EntryType -->
			<xsl:when test="name(.)='EntryType'">
				<xsl:element name="{name(.)}">
					<Link>
						<xsl:attribute name="ws"><xsl:value-of select="@ws"/></xsl:attribute>
						<xsl:attribute name="name"><xsl:value-of select="."/></xsl:attribute>
					</Link>
				</xsl:element>
			</xsl:when>
			<!-- Example -->
			<xsl:when test="name(.)='Example'">
				<xsl:if test="not(preceding-sibling::Example)">
					<Example>
						<xsl:call-template name="DoAStr"/>
						<xsl:for-each select="following-sibling::Example">
							<xsl:call-template name="DoAStr"/>
						</xsl:for-each>
					</Example>
				</xsl:if>
			</xsl:when>
			<!-- Examples -->
			<xsl:when test="name(.)='Examples'">
				<Examples>
					<xsl:apply-templates/>
				</Examples>
			</xsl:when>
			<!-- Etymology-->
			<xsl:when test="name(.)='Etymology'">
				<xsl:element name="{name(.)}">
					<xsl:apply-templates/>
				</xsl:element>
			</xsl:when>
			<!-- Pronunciations -->
			<xsl:when test="name(.)='Pronunciations'">
				<xsl:element name="{name(.)}">
					<xsl:apply-templates/>
				</xsl:element>
			</xsl:when>
			<!-- Pictures -->
			<xsl:when test="name(.)='Pictures'">
				<xsl:element name="{name(.)}">
					<xsl:apply-templates/>
				</xsl:element>
			</xsl:when>
			<!-- PictureFile -->
			<xsl:when test="name(.)='PictureFile' ">
				<xsl:element name="{name(.)}">
					<xsl:apply-templates/>
				</xsl:element>
			</xsl:when>
			<!-- Caption-->
			<xsl:when test="name(.)='Caption'">
				<xsl:element name="{name(.)}">
					<xsl:call-template name="DoAStr"/> <!-- /xsl:call-template><xsl:apply-templates/ -->
				</xsl:element>
			</xsl:when>
			<!-- Custom -->
			<xsl:when test="name(.)='Custom'">
				<xsl:choose>
					<xsl:when test="@type='string'">
						<xsl:element name="Custom">
							<xsl:attribute name="name"><xsl:value-of select="@fwid"/></xsl:attribute>
							<xsl:call-template name="DoStr"></xsl:call-template>
						</xsl:element>
					</xsl:when>
					<xsl:when test="@type='text'">
						<xsl:element name="Custom">
							<xsl:attribute name="name"><xsl:value-of select="@fwid"/></xsl:attribute>
							<xsl:call-template name="DoStr"></xsl:call-template>
						</xsl:element>
					</xsl:when>
					<xsl:when test="@type='ListRef'">
						<xsl:element name="Custom">
							<xsl:attribute name="name"><xsl:value-of select="@fwid"/></xsl:attribute>
							<Link>
								<xsl:attribute name="ws"><xsl:value-of select="@ws"/></xsl:attribute>
								<xsl:choose>
									<xsl:when test="@abbr='False'">
										<xsl:attribute name="name"><xsl:value-of select="."/></xsl:attribute>
									</xsl:when>
									<xsl:otherwise>
										<xsl:attribute name="abbr"><xsl:value-of select="."/></xsl:attribute>
									</xsl:otherwise>
								</xsl:choose>
							</Link>
						</xsl:element>
					</xsl:when>
					<xsl:when test="@type='ListMultiRef'">
						<xsl:element name="Custom">
							<xsl:attribute name="name"><xsl:value-of select="@fwid"/></xsl:attribute>
							<Link>
								<xsl:attribute name="ws"><xsl:value-of select="@ws"/></xsl:attribute>
								<xsl:choose>
									<xsl:when test="@abbr='False'">
										<xsl:attribute name="name"><xsl:value-of select="."/></xsl:attribute>
									</xsl:when>
									<xsl:otherwise>
										<xsl:attribute name="abbr"><xsl:value-of select="."/></xsl:attribute>
									</xsl:otherwise>
								</xsl:choose>
							</Link>
						</xsl:element>
					</xsl:when>
					<xsl:otherwise>
						<xsl:element name="CustomStr">
							<xsl:attribute name="name"><xsl:value-of select="@fwid"/></xsl:attribute>
							<xsl:call-template name="DoAUni"></xsl:call-template>
						</xsl:element>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:when>
			<!-- CVPattern OR Tone -->
			<xsl:when test="name(.)='CVPattern' or name(.)='Tone'">
				<xsl:element name="{name(.)}">
					<xsl:call-template name="DoStr"/>
				</xsl:element>
			</xsl:when>
			<!-- Location OR MediaFiles-->
			<xsl:when test="name(.)='Location' or name(.)='MediaFiles'">
				<xsl:element name="{name(.)}">
					<xsl:apply-templates/>
				</xsl:element>
			</xsl:when>
			<!-- MediaFile -->
			<xsl:when test="name(.)='MediaFile'">
				<xsl:element name="{name(.)}">
					<xsl:apply-templates/>
				</xsl:element>
			</xsl:when>



			<!-- Form -->
			<xsl:when test="name(.)='Form'">
				<xsl:if test="not(preceding-sibling::Form)">
				   <xsl:variable name="elementName">
					<xsl:call-template name="DoParentBasedElementName">
					  <xsl:with-param name="defaultNumber" select="5035"/>
					</xsl:call-template>
				  </xsl:variable>
				  <xsl:element name="{$elementName}">
					<xsl:call-template name="DoAUni"/>
					  <xsl:for-each select="following-sibling::Form">
						<xsl:call-template name="DoAUni"/>
					  </xsl:for-each>
				  </xsl:element>
				</xsl:if>
			</xsl:when>
			<!-- Gloss -->
			<xsl:when test="name(.)='Gloss'">
				<xsl:if test="not(preceding-sibling::Gloss)">
				   <xsl:variable name="elementName">
					<xsl:call-template name="DoParentBasedElementName">
					  <xsl:with-param name="defaultNumber" select="5016"/>
					</xsl:call-template>
				   </xsl:variable>
				   <xsl:element name="{$elementName}">
					  <xsl:call-template name="DoAUni"/>
					  <xsl:for-each select="following-sibling::Gloss">
						<xsl:call-template name="DoAUni"/>
					  </xsl:for-each>
				   </xsl:element>
				</xsl:if>
			</xsl:when>
			<!-- LexemeForm -->
			<xsl:when test="name(.)='LexemeForm'">
				<xsl:element name="{name(.)}">
					<xsl:apply-templates/>
				</xsl:element>
			</xsl:when>
			<!-- LexicalRelations -->
			<xsl:when test="name(.)='LexicalRelations'">
				<xsl:element name="{name(.)}">
					<xsl:apply-templates/>
				</xsl:element>
			</xsl:when>
			<!-- MainEntriesOrSenses -->
			<!-- VariantEntryTypes -->
			<xsl:when test="name(.)='ComplexEntryTypes'">
				<xsl:element name="{name(.)}">
					<Link>
						<xsl:attribute name="ws"><xsl:value-of select="@ws"/></xsl:attribute>
						<xsl:attribute name="name"><xsl:value-of select="."/></xsl:attribute>
					</Link>
				</xsl:element>
				<RefType><Integer val="1"/></RefType>
			</xsl:when>
			<!-- MainEntriesOrSenses -->
			<xsl:when test="name(.)='ComponentLexemes' or name(.)='PrimaryLexemes' ">
				<xsl:element name="{name(.)}">
					<!-- type="target" -->
					<xsl:if test="@type='target'">
						<Link target="{.}"/>
					</xsl:if>
					<!-- type="entry" -->
					<xsl:if test="@type='entry'">
						<Link ws="{@ws}" entry="{.}"/>
					</xsl:if>
					<xsl:if test="@type='sense'">
						<Link ws="{@ws}" sense="{.}"/>
					</xsl:if>
				</xsl:element>
			</xsl:when>
			<!-- MainEntriesOrSenses -->
			<xsl:when test="name(.)='MainEntriesOrSenses'">
				<xsl:element name="{name(.)}">
				  <!-- type="target" -->
				  <xsl:if test="@type='target'">
					<Link target="{.}"/>
				  </xsl:if>
				  <!-- type="entry" -->
				  <xsl:if test="@type='entry'">
					<Link ws="{@ws}" entry="{.}"/>
				  </xsl:if>
				  <xsl:if test="@type='sense'">
					<Link ws="{@ws}" sense="{.}"/>
				  </xsl:if>
				</xsl:element>
			</xsl:when>
			<!-- MorphosyntaxAnalyses -->
			<xsl:when test="name(.)='MorphoSyntaxAnalyses'">
				<MorphoSyntaxAnalyses>
					<xsl:apply-templates/>
				</MorphoSyntaxAnalyses>
			</xsl:when>
			<!-- MorphosyntaxAnalysis -->
			<xsl:when test="name(.)='MorphoSyntaxAnalysis'">
				<MorphoSyntaxAnalysis>
					<xsl:apply-templates/>
				</MorphoSyntaxAnalysis>
			</xsl:when>
			<!-- MorphType -->
			<xsl:when test="name(.)='MorphType'">
				<MorphType>
					<Link>
						<xsl:attribute name="ws"><xsl:value-of select="@ws"/></xsl:attribute>
						<xsl:attribute name="name"><xsl:value-of select="."/></xsl:attribute>
						<!-- <xsl:apply-templates/> -->
					</Link>
				</MorphType>
			</xsl:when>
			<!-- PartOfSpeech -->
			<xsl:when test="name(.)='PartOfSpeech'">
				<xsl:choose>
					<xsl:when test="name(..) = 'MoStemMsa'">
						<PartOfSpeech>
							<xsl:apply-templates/>
						</PartOfSpeech>
					</xsl:when>
					<xsl:otherwise>
						<PartOfSpeech>
							<xsl:apply-templates/>
						</PartOfSpeech>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:when>
			<!-- ImportEntryResidue -->
			<xsl:when test="name(.)='ImportEntryResidue'">
				<xsl:if test="not(preceding-sibling::ImportEntryResidue)">
					<xsl:element name="ImportResidue">
						<Str>
							<xsl:call-template name="DoRuns"/>
							<xsl:for-each select="following-sibling::ImportEntryResidue">
								<xsl:call-template name="DoRuns"/>
							</xsl:for-each>
						</Str>
					</xsl:element>
				</xsl:if>
			</xsl:when>
			<!-- ImportSenseResidue -->
			<xsl:when test="name(.)='ImportSenseResidue'">
				<xsl:if test="not(preceding-sibling::ImportSenseResidue)">
					<xsl:element name="ImportResidue">
						<Str>
							<xsl:call-template name="DoRuns"/>
							<xsl:for-each select="following-sibling::ImportSenseResidue">
								<xsl:call-template name="DoRuns"/>
							</xsl:for-each>
						</Str>
					</xsl:element>
				</xsl:if>
			</xsl:when>
			<!-- ReversalEntries -->
			<xsl:when test="name(.)='ReversalEntries'">
				<ReversalEntries>
					<xsl:apply-templates/>
				</ReversalEntries>
			</xsl:when>
			<!-- Reference -->
			<xsl:when test="name(.)='Reference'">
				<xsl:element name="Reference">
					<xsl:call-template name="DoStr"/>
				</xsl:element>
			</xsl:when>
			<!-- ScientificName -->
			<xsl:when test="name(.)='ScientificName'">
				<xsl:element name="ScientificName">
					<xsl:call-template name="DoStr"/>
				</xsl:element>
			</xsl:when>
			<!-- Source -->
			<xsl:when test="name(.)='Source'">
			   <xsl:variable name="elementName">
				 <xsl:call-template name="DoParentBasedElementName">
				   <xsl:with-param name="defaultNumber" select="5016"/>
				   </xsl:call-template>
				 </xsl:variable>
				<xsl:element name="{$elementName}">
				  <!-- Based on the parent name, put out different type of data -->
				  <xsl:choose>
					<xsl:when test="name(..)='LexEtymology'">
					  <xsl:call-template name="DoUni"/>
					</xsl:when>
					<xsl:otherwise>
					  <xsl:call-template name="DoStr"/>
					</xsl:otherwise>
				  </xsl:choose>
				</xsl:element>
			</xsl:when>
			<!-- SemanticDomains -->
			<xsl:when test="name(.)='SemanticDomains'">
				<SemanticDomains>
					<xsl:apply-templates/>
				</SemanticDomains>
			</xsl:when>
			<!-- Senses, either 5002 or 5016 depending on the parent -->
			<xsl:when test="name(.)='Senses'">
				<xsl:if test="name(..)='LexEntry'">
					<Senses>
						<xsl:apply-templates/>
					</Senses>
				</xsl:if>
				<xsl:if test="name(..)='LexSense'">
					<Senses>
						<xsl:apply-templates/>
					</Senses>
				</xsl:if>
			</xsl:when>
			<!-- Translations -->
			<xsl:when test="name(.)='Translations'">
				<Translations>
					<xsl:apply-templates/>
				</Translations>
			</xsl:when>
			<!-- vern -->
			<xsl:when test="name(.)='vern'">
				<Run>
					<xsl:choose>
						<xsl:when test="@ws">
							<xsl:attribute name="ws"><xsl:value-of select="@ws"/></xsl:attribute>
						</xsl:when>
						<xsl:otherwise>
							<xsl:attribute name="ws"><xsl:value-of select="../@ws"/></xsl:attribute>
						</xsl:otherwise>
					</xsl:choose>
					<xsl:apply-templates select="@*"/>
					<xsl:apply-templates/>
				</Run>
			</xsl:when>
			<!-- case to strip out the InFieldMarker from elements that don't process it currently -->
			<!--
			<xsl:when test="name(.)='InFieldMarker'">
				<xsl:value-of select="@beginMarker"/>
				<xsl:apply-templates/>>
				<xsl:if test="@endMarker">
					<xsl:value-of select="@endMarker"/>
				</xsl:if>
			</xsl:when>
			-->
			<!-- every thing else -->
			<xsl:when test=".=InFieldMarker/text()">
			  </xsl:when>

			<xsl:otherwise>
				<xsl:copy>
					<xsl:apply-templates select="@*"/>
					<xsl:apply-templates/>
				</xsl:copy>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>




<xsl:template name="DoInFieldMarker">
	<xsl:apply-templates/>
</xsl:template>

<!-- This handler for InFieldmarker is used to restore the beging and end markers into the data stream -->
<xsl:template match="InFieldMarker" mode="PutBackMarkers">
		<xsl:if test="not(@ignore)">
		<xsl:value-of select="@beginMarker"/>
		</xsl:if>
		<xsl:apply-templates mode="PutBackMarkers"/>
		<xsl:if test="not(@ignore) and @endMarker">
			<xsl:value-of select="@endMarker"/>
		</xsl:if>
</xsl:template>


	<xsl:template match="InFieldMarker/text()" mode="Run">
		<xsl:choose>
			<xsl:when test="../@ignore">
				<xsl:element name="Run">
					 <xsl:value-of select="."/>
				 </xsl:element>
			</xsl:when>
			<xsl:otherwise>
				<xsl:element name="Run">
					<!-- If there is no ws then include the one from the parent -->
					<xsl:choose>
						<xsl:when test="../@ws"><xsl:attribute name="ws"><xsl:value-of select="../@ws"/></xsl:attribute></xsl:when>
						<xsl:when test="../../@ws"><xsl:attribute name="ws"><xsl:value-of select="../../@ws"/></xsl:attribute></xsl:when>
						<xsl:when test="../../../@ws"><xsl:attribute name="ws"><xsl:value-of select="../../../@ws"/></xsl:attribute></xsl:when>
						<xsl:otherwise><xsl:attribute name="ws"><xsl:value-of select="../../../../@ws"/></xsl:attribute></xsl:otherwise>
					</xsl:choose>
					<!-- If there is no style then use the one of the parent if one exists -->
					<xsl:choose>
						<xsl:when test="../@style"><xsl:attribute name="namedStyle"><xsl:value-of select="../@style"/></xsl:attribute></xsl:when>
						<xsl:when test="../../@style"><xsl:attribute name="namedStyle"><xsl:value-of select="../../@style"/></xsl:attribute></xsl:when>
						<xsl:otherwise></xsl:otherwise>
					</xsl:choose>
					 <xsl:value-of select="."/>
				</xsl:element>
				<!-- xsl:apply-templates/  **************************************************** -->
				<xsl:apply-templates/>
				</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template match="text()" mode="Run">
		 <xsl:choose>
			<xsl:when test="string-length(.)='0'">    <!-- Dont put out a run for an empty string -->
			</xsl:when>
			<xsl:otherwise>
				<xsl:element name="Run">
					<!-- If there is no ws then include the one from the parent -->
					<xsl:choose>
						<xsl:when test="@ws"><xsl:attribute name="ws"><xsl:value-of select="@ws"/></xsl:attribute></xsl:when>
						<xsl:otherwise><xsl:attribute name="ws"><xsl:value-of select="../@ws"/></xsl:attribute></xsl:otherwise>
					</xsl:choose>
					<!-- If there is no style then use the one of the parent if one exists -->
					<xsl:choose>
						<xsl:when test="@style"><xsl:attribute name="namedStyle"><xsl:value-of select="@style"/></xsl:attribute></xsl:when>
						<xsl:when test="../@style"><xsl:attribute name="namedStyle"><xsl:value-of select="../@style"/></xsl:attribute></xsl:when>
						<xsl:otherwise></xsl:otherwise>
					</xsl:choose>
					<xsl:value-of select="."/>
				</xsl:element>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>


<!--
	<xsl:template name="MultiString">
		<xsl:param name="className"/>
		<xsl:param name="elementName"/>
		<xsl:variable name="newElementName">
			<xsl:choose>
				<xsl:if test="$className = 'LexSense'"><xsl:value-of select="{$elementName}5016"/></xsl:if>
				<xsl:otherwise>blahblahblah</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<xsl:variable name="pathvar">not(preceding-sibling::$elementName"</xsl:variable>
				<xsl:if test="$pathvar">
					<xsl:element name="{$newElementName}">
						<xsl:call-template name="DoAStr"/>
						<xsl:variable name="pathvarb">following-sibling::$elementName"</xsl:variable>
						<xsl:for-each select="$pathvarb">
							<xsl:call-template name="DoAStr"/>
						</xsl:for-each>
					</xsl:element>
				</xsl:if>
	</xsl:template>
-->

	<xsl:template name="DoDateField">
		<xsl:element name="{name(.)}">
			<Time val="{.}" />
		</xsl:element>
	</xsl:template>


	<xsl:template name="DoBooleanField">
		<xsl:element name="{name(.)}">
			<Boolean val="{.}" />
		</xsl:element>
	</xsl:template>

  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoForm
	process for
		Parameters: form number
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->

  <xsl:template name="DoParentBasedElementName">
	<xsl:param name="defaultNumber"/>
	<xsl:choose>
		<xsl:when test="name(..)='LexEtymology'">
		  <xsl:value-of select="name(.)"/>
		</xsl:when>
		<xsl:when test="name(..)='LexPronunciation'">
		  <xsl:value-of select="name(.)"/>
		</xsl:when>
		<xsl:otherwise>
		  <xsl:value-of select="name(.)"/>
		</xsl:otherwise>
	</xsl:choose>
  </xsl:template>

<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoInFieldMarker
	process the Run elements of a Str or AStr element
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="DoInFieldMarker_GOOD_LAST_ONE" >
	<xsl:choose>
		<!-- ignore the infield markers that have an ignore attribute that is set to true -->
		<xsl:when test="name(.)='InFieldMarker' and @ignore='true'"/>	<!-- don't do anything -->
		<!-- xsl:when test="node()=. and (not(text()) or text()='')">
			<xsl:comment>Ignoring node with no text</xsl:comment>
		</xsl:when -->
		<xsl:otherwise>
			<xsl:if test="node()=. and (not(text()) or text()='')">
				<xsl:comment> TODO: Ignore node with no text </xsl:comment>
			</xsl:if>
			<Run>
				<!-- debugging attributes or comments -->
				<!-- xsl:attribute name="text"><xsl:value-of select="text()"/></xsl:attribute>
				<xsl:attribute name="text2"><xsl:value-of select="text()[2]"/></xsl:attribute>
				<xsl:attribute name="text3"><xsl:value-of select="text()[3]"/></xsl:attribute>
				<xsl:attribute name="dot"><xsl:value-of select="."/></xsl:attribute -->
				<!-- If there is no ws then include the one from the parent -->
				<xsl:choose>
					<xsl:when test="@ws"><xsl:attribute name="ws"><xsl:value-of select="@ws"/></xsl:attribute></xsl:when>
					<xsl:otherwise><xsl:attribute name="ws"><xsl:value-of select="../@ws"/></xsl:attribute></xsl:otherwise>
				</xsl:choose>
				<!-- If there is no style then use the one of the parent if one exists -->
				<xsl:choose>
					<xsl:when test="@style"><xsl:attribute name="namedStyle"><xsl:value-of select="@style"/></xsl:attribute></xsl:when>
					<xsl:when test="../@style"><xsl:attribute name="namedStyle"><xsl:value-of select="../@style"/></xsl:attribute></xsl:when>
					<xsl:otherwise></xsl:otherwise>
				</xsl:choose>
				<xsl:choose>
					<xsl:when test="node()=.">
						<xsl:value-of select="text()"/>
					</xsl:when>
					<xsl:when test="text()=.">
						<xsl:value-of select="text()"/>
					</xsl:when>
					<xsl:when test="./InFieldMarker">
						<xsl:value-of select="text()"/>
					</xsl:when>
					<xsl:otherwise>
						<xsl:value-of select="."/>
					</xsl:otherwise>
				</xsl:choose>
			</Run>
			<xsl:for-each select="./InFieldMarker">
				<xsl:call-template name="DoInFieldMarker"/>
				<xsl:for-each select="following-sibling::text()[1]">
					<xsl:call-template name="DoInFieldMarker"/>
				</xsl:for-each>
			</xsl:for-each>
		</xsl:otherwise>
	</xsl:choose>
  </xsl:template>

<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoRuns
	process the Run elements of a Str or AStr element
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="DoRuns">
		<xsl:apply-templates mode="Run"/>
		<!--
			<xsl:for-each select="*|text()|node()|text()[2]">
			<xsl:call-template name="DoInFieldMarker"/>
		 </xsl:for-each>
		 -->
	</xsl:template>

	<xsl:template name="DoRunsOld">
		<xsl:for-each select="*|text()|node()">
			<xsl:call-template name="DoInFieldMarker"/>
			<xsl:choose>
				<!-- ignore the infield markers that have an ignore attribute that is set to true -->
				<xsl:when test="name(.)='InFieldMarker' and @ignore='true'"/>	<!-- don't do anything -->
				<xsl:otherwise>
					<Run>
						<!-- If there is no ws then include the one from the parent -->
						<xsl:choose>
							<xsl:when test="@ws"><xsl:attribute name="ws"><xsl:value-of select="@ws"/></xsl:attribute></xsl:when>
							<xsl:otherwise><xsl:attribute name="ws"><xsl:value-of select="../@ws"/></xsl:attribute></xsl:otherwise>
						</xsl:choose>
						<!-- If there is no style then use the one of the parent if one exists -->
						<xsl:choose>
							<xsl:when test="@style"><xsl:attribute name="namedStyle"><xsl:value-of select="@style"/></xsl:attribute></xsl:when>
							<xsl:when test="../@style"><xsl:attribute name="namedStyle"><xsl:value-of select="../@style"/></xsl:attribute></xsl:when>
							<xsl:otherwise></xsl:otherwise>
						</xsl:choose>
						<xsl:value-of select="."/>
					</Run>
				</xsl:otherwise>
			</xsl:choose>
		 </xsl:for-each>
	</xsl:template>

	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoStr
	process a Str element
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="DoStr">
		<Str>
			<!-- xsl:variable name="RawData" select="node() | text() | ."/ -->
			<!-- xsl:comment><xsl:value-of select="$RawData"/></xsl:comment -->
			<xsl:call-template name="DoRuns"/>
		</Str>
	</xsl:template>

	<xsl:template name="GoodPrevious_DoStr">
		<Str>
			<!-- <xsl:apply-templates select="@*"/> -->
			<xsl:copy-of select="@*"/>
			<xsl:for-each select="*|text()">
				<Run>
					<xsl:apply-templates select="../@*"/> <!-- copy-of  to copy the attributes -->
					<xsl:value-of select="."/>
				</Run>
				<xsl:variable name="iPos" select="position()"/>
				<xsl:apply-templates select="../*[position()=$iPos]"/>
			</xsl:for-each>
		</Str>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoAStr
	process an AStr element
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="DoAStr">
		<AStr>
			<xsl:apply-templates select="@*"/>
			<xsl:call-template name="DoRuns"/>
<!--
			<xsl:for-each select="text()">
				<Run>
					<xsl:apply-templates select="../@*"/>
					<xsl:value-of select="."/>
				</Run>
				<xsl:variable name="iPos" select="position()"/>
				<xsl:apply-templates select="../*[position()=$iPos]"/>
			</xsl:for-each>
-->
		</AStr>
	</xsl:template>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoAUni
	process an AUni element
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="DoAUni">
		<AUni>
			<xsl:apply-templates select="@*" />
			<xsl:value-of select="."/>
		</AUni>
	</xsl:template>


	<xsl:template name="DoRun_xxxxx">
		<Run>
			<xsl:apply-templates select="@*"/>
			<xsl:value-of select="."/>
		</Run>
	</xsl:template>


	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
DoUni
	process an Uni element
		Parameters: none
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template name="DoUni">
		<Uni>
			<xsl:apply-templates mode="PutBackMarkers"/>
		</Uni>
	</xsl:template>
</xsl:stylesheet>



<!-- "template" of mappings used in the main template:

<XYZ> ... </XYZ> goes to <XYZdddd> ... </XYZdddd>:

			<xsl:when test="name(.)='XYZ'">
				<XYZdddd>
					<xsl:apply-templates/>
				</XYZdddd>
			</xsl:when>

<XYZ attr="abcd"> ... </XYZ> goes to <XYZdddd attr="abcd"> ... </XYZdddd>:

			<xsl:when test="name(.)='XYZ'">
				<XYZdddd>
					<xsl:apply-templates select="@*"/>
					<xsl:apply-templates/>
				</XYZdddd>
			</xsl:when>

<XYZ attr="abcd"> ... </XYZ> goes to <XYZdddd><ZZZ  attr="abcd"> ... </ZZZ></XYZdddd>:

			<xsl:when test="name(.)='XYZ'">
				<XYZdddd>
					<ZZZ>
					<xsl:apply-templates select="@*"/>
					<xsl:apply-templates/>
					</ZZZ>
				</XYZdddd>
			</xsl:when>

<XYZ attr="abcd"/>
<XYZ attr="efgh"/> goes to <XYZdddd><ZZZ  attr="abcd"/><ZZZ  attr="efgh"/></XYZdddd>:

			<xsl:when test="name(.)='XYZ'">
				<xsl:if test="not(preceding-sibling::XYZ)">   if it is the first one
					<XYZdddd>
						<ZZZ>
							<xsl:apply-templates select="@*"/>
							<xsl:apply-templates/>
						</ZZZ>
						<xsl:for-each select="following-sibling::XYZ">  go do the others
							<ZZZ>
								<xsl:apply-templates select="@*"/>
								<xsl:apply-templates/>
							</ZZZ>
						</xsl:for-each>
					</XYZdddd>
				</xsl:if>
			</xsl:when>

<XYZ>abcd</XYZ> goes to <XYZdddd><ZZZ  attr="abcd"></ZZZ></XYZdddd>:

			<xsl:when test="name(.)='XYZ'">
				<XYZdddd>
					<ZZZ>
					<xsl:attribute name="attr"><xsl:value-of select="."/></xsl:attribute>
					</ZZZ>
				</XYZdddd>
			</xsl:when>

-->
<!--
================================================================
Revision History
- - - - - - - - - - - - - - - - - - -
04-Mar-2005    Andy Black  Began working on Initial Draft
XX-Mar-2005    dlh - Adding functionality...
08-Aug-2006    Bev - Senses to Senses5016 inside LexSense
20-Nov-2009	   SRMc - remove class numbers from element names
================================================================
 -->
