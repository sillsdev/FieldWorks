<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
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
				<Allomorphs5002>
					<xsl:apply-templates select="@*"/>
					<xsl:apply-templates/>
				</Allomorphs5002>
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
					<CitationForm5002>
						<xsl:call-template name="DoAUni"/>
						<xsl:for-each select="following-sibling::CitationForm">
							<xsl:call-template name="DoAUni"/>
						</xsl:for-each>
					</CitationForm5002>
				</xsl:if>
			</xsl:when>
			<!-- Restrictions -->
			<xsl:when test="name(.)='Restrictions'">
				<xsl:variable name="eleName">
					<xsl:choose>
						<xsl:when test="name(..)='LexSense'">Restrictions5016</xsl:when>
						<xsl:otherwise>Restrictions5002</xsl:otherwise>
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
						<Type29>
							<Link ws="en" name="Free translation"/>
						</Type29>
						<Translation29>
							<xsl:call-template name="DoAStr"/>
							<xsl:for-each select="following-sibling::CmTranslation">
								<xsl:call-template name="DoAStr"/>
							</xsl:for-each>
						</Translation29>
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
				<xsl:element name="{name(.)}5002">
					<Integer val="{.}" />
				</xsl:element>
			</xsl:when>
			<!-- AnthroNote -->
			<xsl:when test="name(.)='AnthroNote'">
				<xsl:if test="not(preceding-sibling::AnthroNote)">
					<xsl:element name="{name(.)}5016">
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
					<xsl:element name="AnthroCodes5016">
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
						<xsl:when test="name(..)='LexSense'">Bibliography5016</xsl:when>
						<xsl:otherwise>Bibliography5002</xsl:otherwise>
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
			<!-- Definition -->
			<xsl:when test="name(.)='Definition'">
				<xsl:if test="not(preceding-sibling::Definition)">
					<Definition5016>
						<xsl:call-template name="DoAStr"/>
						<xsl:for-each select="following-sibling::Definition">
							<xsl:call-template name="DoAStr"/>
						</xsl:for-each>
					</Definition5016>
				</xsl:if>
			</xsl:when>
			<!-- DiscourseNote -->
			<xsl:when test="name(.)='DiscourseNote'">
				<xsl:if test="not(preceding-sibling::DiscourseNote)">
					<xsl:element name="DiscourseNote5016">
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
					<xsl:element name="DomainTypes5016">
					  <xsl:apply-templates/>
						<xsl:for-each select="following-sibling::DomainTypes">
						<xsl:apply-templates/>
						</xsl:for-each>
					</xsl:element>
				</xsl:if>
			</xsl:when>
			<!-- SenseType -->
			<xsl:when test="name(.)='SenseType'">
				<xsl:if test="not(preceding-sibling::SenseType)">
					<xsl:element name="{name(.)}5016">
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
					<xsl:element name="{name(.)}5016">
					  <xsl:apply-templates/>
						<xsl:for-each select="following-sibling::Status">
						<xsl:apply-templates/>
						</xsl:for-each>
					</xsl:element>
				</xsl:if>
			</xsl:when>
			<!-- UsageTypes -->
			<xsl:when test="name(.)='UsageTypes'">
				<xsl:if test="not(preceding-sibling::UsageTypes)">
					<xsl:element name="{name(.)}5016">
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
					<xsl:element name="EncyclopedicInfo5016">
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
					<xsl:element name="GeneralNote5016">
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
					<xsl:element name="GrammarNote5016">
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
					<xsl:element name="PhonologyNote5016">
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
					<xsl:element name="{name(.)}5016">
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
					<xsl:element name="SocioLinguisticsNote5016">
						<xsl:call-template name="DoAStr"/>
						<xsl:for-each select="following-sibling::SocioLinguisticsNote">
							<xsl:call-template name="DoAStr"/>
						</xsl:for-each>
					</xsl:element>
				</xsl:if>
			</xsl:when>
			<!-- Entries -->
			<xsl:when test="name(.)='Entries'">
				<Entries5005>
					<xsl:apply-templates/>
				</Entries5005>
			</xsl:when>
			<!-- Example -->
			<xsl:when test="name(.)='Example'">
				<xsl:if test="not(preceding-sibling::Example)">
					<Example5004>
						<xsl:call-template name="DoAStr"/>
						<xsl:for-each select="following-sibling::Example">
							<xsl:call-template name="DoAStr"/>
						</xsl:for-each>
					</Example5004>
				</xsl:if>
			</xsl:when>
			<!-- Examples -->
			<xsl:when test="name(.)='Examples'">
				<Examples5016>
					<xsl:apply-templates/>
				</Examples5016>
			</xsl:when>
			<!-- Form -->
			<xsl:when test="name(.)='Form'">
				<xsl:if test="not(preceding-sibling::Form)">
					<Form5035>
						<xsl:call-template name="DoAUni"/>
						<xsl:for-each select="following-sibling::Form">
							<xsl:call-template name="DoAUni"/>
						</xsl:for-each>
					</Form5035>
				</xsl:if>
			</xsl:when>
			<!-- Gloss -->
			<xsl:when test="name(.)='Gloss'">
				<xsl:if test="not(preceding-sibling::Gloss)">
					<Gloss5016>
						<xsl:call-template name="DoAUni"/>
						<xsl:for-each select="following-sibling::Gloss">
							<xsl:call-template name="DoAUni"/>
						</xsl:for-each>
					</Gloss5016>
				</xsl:if>
			</xsl:when>
			<!-- MorphosyntaxAnalyses -->
			<xsl:when test="name(.)='MorphoSyntaxAnalyses'">
				<MorphoSyntaxAnalyses5002>
					<xsl:apply-templates/>
				</MorphoSyntaxAnalyses5002>
			</xsl:when>
			<!-- MorphosyntaxAnalysis -->
			<xsl:when test="name(.)='MorphoSyntaxAnalysis'">
				<MorphoSyntaxAnalysis5016>
					<xsl:apply-templates/>
				</MorphoSyntaxAnalysis5016>
			</xsl:when>
			<!-- MorphType -->
			<xsl:when test="name(.)='MorphType'">
				<MorphType5035>
					<Link>
						<xsl:attribute name="ws"><xsl:value-of select="@ws"/></xsl:attribute>
						<xsl:attribute name="name"><xsl:value-of select="."/></xsl:attribute>
						<!-- <xsl:apply-templates/> -->
					</Link>
				</MorphType5035>
			</xsl:when>
			<!-- PartOfSpeech -->
			<xsl:when test="name(.)='PartOfSpeech'">
				<PartOfSpeech5001>
					<xsl:apply-templates/>
				</PartOfSpeech5001>
			</xsl:when>
			<!-- ReversalEntries -->
			<xsl:when test="name(.)='ReversalEntries'">
				<ReversalEntries5016>
					<xsl:apply-templates/>
				</ReversalEntries5016>
			</xsl:when>
			<!-- Reference -->
			<xsl:when test="name(.)='Reference'">
				<xsl:element name="Reference5004">
					<xsl:call-template name="DoStr"/>
				</xsl:element>
			</xsl:when>
			<!-- ScientificName -->
			<xsl:when test="name(.)='ScientificName'">
				<xsl:element name="ScientificName5016">
					<xsl:call-template name="DoStr"/>
				</xsl:element>
			</xsl:when>
			<!-- Source -->
			<xsl:when test="name(.)='Source'">
				<xsl:element name="{name(.)}5016">
					<xsl:call-template name="DoStr"/>
				</xsl:element>
			</xsl:when>
			<!-- SemanticDomains -->
			<xsl:when test="name(.)='SemanticDomains'">
				<SemanticDomains5016>
					<xsl:apply-templates/>
				</SemanticDomains5016>
			</xsl:when>
			<!-- Senses -->
			<xsl:when test="name(.)='Senses'">
				<Senses5002>
					<xsl:apply-templates/>
				</Senses5002>
			</xsl:when>
			<!-- Translations -->
			<xsl:when test="name(.)='Translations'">
				<Translations5004>
					<xsl:apply-templates/>
				</Translations5004>
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
			<!-- every thing else -->
			<xsl:otherwise>
				<xsl:copy>
					<xsl:apply-templates select="@*"/>
					<xsl:apply-templates/>
				</xsl:copy>
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
		<xsl:element name="{name(.)}5002">
			<Time val="{.}" />
		</xsl:element>
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
			<Run>
				<xsl:apply-templates select="./@*"/>
				<xsl:value-of select="."/>
			</Run>
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
			<xsl:for-each select="text()">
				<Run>
					<xsl:apply-templates select="../@*"/>
					<xsl:value-of select="."/>
				</Run>
				<xsl:variable name="iPos" select="position()"/>
				<xsl:apply-templates select="../*[position()=$iPos]"/>
			</xsl:for-each>
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
			<xsl:apply-templates select="@*"/>
			<xsl:apply-templates/>
		</AUni>
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
================================================================
 -->
