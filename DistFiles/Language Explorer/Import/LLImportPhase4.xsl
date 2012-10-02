<?xml version="1.0"?>
<xsl:transform xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0" xmlns:msxsl="urn:schemas-microsoft-com:xslt" xmlns:user="urn:my-scripts">
	<xsl:output encoding="UTF-8" indent="yes"/>

	<!-- Insert <Segments> element into <StTxtPara> elements -->

	<xsl:template match="StTxtPara">
		<xsl:copy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
			<Segments>
				<xsl:variable name="paraid" select="@id"/>
				<xsl:for-each select="/FwDatabase/CmBaseAnnotation[@targetid=$paraid and @type='Text Segment']">
					<xsl:variable name="baseid" select="@id"/>
					<Segment>
						<FreeTranslation>
							<xsl:for-each select="/FwDatabase/CmIndirectAnnotation[@targetid=$baseid and @type='Free Translation']">
								<xsl:copy-of select="Comment/AStr"/>
							</xsl:for-each>
						</FreeTranslation>
						<LiteralTranslation>
							<xsl:for-each select="/FwDatabase/CmIndirectAnnotation[@targetid=$baseid and @type='Literal Translation']">
								<xsl:copy-of select="Comment/AStr"/>
							</xsl:for-each>
						</LiteralTranslation>
						<Notes>
							<xsl:for-each select="/FwDatabase/CmIndirectAnnotation[@targetid=$baseid and @type='Note']">
								<Note>
									<Content>
										<xsl:copy-of select="Comment/AStr"/>
									</Content>
								</Note>
							</xsl:for-each>
						</Notes>
						<Analyses>
							<xsl:for-each select="/FwDatabase/CmBaseAnnotation[@targetSeg=$baseid and @type='Wordform In Context']">
								<xsl:copy-of select="InstanceOf/Link"/>
							</xsl:for-each>
						</Analyses>
					</Segment>
				</xsl:for-each>
			</Segments>
		</xsl:copy>
	</xsl:template>

	<!-- delete all the annotations that should no longer exist since the data has been moved into StTxtPara/Segments. -->

	<xsl:template match="CmBaseAnnotation[@type='Text Segment' or @type='Wordform In Context' or @type='Punctuation In Context']"/>
	<xsl:template match="CmIndirectAnnotation[@type='Free Translation' or @type='Literal Translation']"/>

	<!-- Handle the redefinitions of <CmAgent>, <CmAgentEvaluation>, and <WfiAnalysis> -->

	<xsl:template match="CmAgent">
		<xsl:copy>
			<xsl:copy-of select="@*"/>
			<xsl:for-each select="Name|Human|StateInformation|Notes">
				<xsl:copy>
					<xsl:copy-of select="@*"/>
					<xsl:apply-templates/>
				</xsl:copy>
			</xsl:for-each>
			<xsl:element name="Approves">
				<xsl:element name="CmAgentEvaluation">
					<xsl:attribute name="id">LL-Evaluation-Approved</xsl:attribute>
				</xsl:element>
			</xsl:element>
			<xsl:element name="Disapproves">
				<xsl:element name="CmAgentEvaluation">
					<xsl:attribute name="id">LL-Evaluation-Disapproved</xsl:attribute>
				</xsl:element>
			</xsl:element>
			<xsl:element name="Version">
				<xsl:element name="Uni">LinguaLinksImport</xsl:element>
			</xsl:element>
		</xsl:copy>
	</xsl:template>

	<xsl:template match="WfiAnalysis">
		<xsl:copy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
			<xsl:variable name="wfaid" select="@id"/>
			<xsl:element name="Evaluations">
				<xsl:for-each select="/FwDatabase/LangProject/AnalyzingAgents/CmAgent/Evaluations/CmAgentEvaluation[Target/Link/@target=$wfaid]">
					<xsl:choose>
						<xsl:when test="Accepted/Boolean/@val='true'">
							<Link target="LL-Evaluation-Approved" class="CmAgentEvaluation"/>
						</xsl:when>
						<xsl:otherwise>
							<Link target="LL-Evaluation-Disapproved" class="CmAgentEvaluation"/>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:for-each>
			</xsl:element>
		</xsl:copy>
	</xsl:template>

	<!-- Copy everything else -->

	<xsl:template match="*">
		<xsl:copy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</xsl:copy>
	</xsl:template>

</xsl:transform>
