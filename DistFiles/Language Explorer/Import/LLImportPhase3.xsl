<?xml version="1.0"?>
<xsl:transform xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0" xmlns:msxsl="urn:schemas-microsoft-com:xslt" xmlns:user="urn:my-scripts">
	<xsl:output encoding="UTF-8" indent="yes"/>

	<!-- Move wordforms and cmanalyses to the top (unowned) level -->
	<!-- also copy some internal values to become attributes of Cm*Annotation in order to -->
	<!-- facilitate changes in the next phase -->

	<xsl:template match="FwDatabase" mode="F">
		<xsl:copy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
			<xsl:for-each select="LangProject/WordformInventory6001/WordformInventory/Wordforms5063/*|LangProject/Annotations6001/*">
				<xsl:copy>
					<xsl:copy-of select="@*"/>
					<xsl:if test="BeginObject37/Link/@target">
						<xsl:attribute name="targetid">
							<xsl:value-of select="BeginObject37/Link/@target"/>
						</xsl:attribute>
					</xsl:if>
					<xsl:if test="AppliesTo36/Link/@target">
						<xsl:attribute name="targetid">
							<xsl:value-of select="AppliesTo36/Link/@target"/>
						</xsl:attribute>
					</xsl:if>
					<xsl:if test="AnnotationType34/Link/@name">
						<xsl:variable name="typename" select="AnnotationType34/Link/@name"/>
						<xsl:attribute name="type">
							<xsl:value-of select="$typename"/>
						</xsl:attribute>
						<xsl:if test="$typename='Wordform In Context' or $typename='Punctuation In Context'">
							<xsl:for-each select="preceding-sibling::CmBaseAnnotation/AnnotationType34/Link[@name='Text Segment'][1]/../..">
								<xsl:attribute name="targetSeg">
									<xsl:value-of select="@id"/>
								</xsl:attribute>
							</xsl:for-each>
						</xsl:if>
					</xsl:if>
					<xsl:if test="BeginOffset37/Integer/@val">
						<xsl:attribute name="idx">
							<xsl:value-of select="BeginOffset37/Integer/@val"/>
						</xsl:attribute>
					</xsl:if>
					<xsl:if test="EndOffset37/Integer/@val">
						<xsl:attribute name="length">
							<xsl:value-of select="EndOffset37/Integer/@val"/>
						</xsl:attribute>
					</xsl:if>
					<xsl:apply-templates/>
				</xsl:copy>
			</xsl:for-each>
		</xsl:copy>
	</xsl:template>
	<xsl:template match="WordformInventory6001|Annotations6001"/>

	<!-- Rename the 900+ field elements that have class numbers appended. -->
	<!-- The Microsoft XSLT Compiler couldn't handle choosing between 900 templates
	so they have been segmented by the first letter with mode="@" where @ is the letter -->

	<xsl:template match="*">
		<xsl:variable name="ch1" select="substring(local-name(),1,1)"/>
		<xsl:choose>
			<xsl:when test="$ch1 = 'A'">
				<xsl:apply-templates select="." mode="A"/>
			</xsl:when>
			<xsl:when test="$ch1 = 'B'">
				<xsl:apply-templates select="." mode="B"/>
			</xsl:when>
			<xsl:when test="$ch1 = 'C'">
				<xsl:apply-templates select="." mode="C"/>
			</xsl:when>
			<xsl:when test="$ch1 = 'D'">
				<xsl:apply-templates select="." mode="D"/>
			</xsl:when>
			<xsl:when test="$ch1 = 'E'">
				<xsl:apply-templates select="." mode="E"/>
			</xsl:when>
			<xsl:when test="$ch1 = 'F'">
				<xsl:apply-templates select="." mode="F"/>
			</xsl:when>
			<xsl:when test="$ch1 = 'G'">
				<xsl:apply-templates select="." mode="G"/>
			</xsl:when>
			<xsl:when test="$ch1 = 'H'">
				<xsl:apply-templates select="." mode="H"/>
			</xsl:when>
			<xsl:when test="$ch1 = 'I'">
				<xsl:apply-templates select="." mode="I"/>
			</xsl:when>
			<xsl:when test="$ch1 = 'K'">
				<xsl:apply-templates select="." mode="K"/>
			</xsl:when>
			<xsl:when test="$ch1 = 'L'">
				<xsl:apply-templates select="." mode="L"/>
			</xsl:when>
			<xsl:when test="$ch1 = 'M'">
				<xsl:apply-templates select="." mode="M"/>
			</xsl:when>
			<xsl:when test="$ch1 = 'N'">
				<xsl:apply-templates select="." mode="N"/>
			</xsl:when>
			<xsl:when test="$ch1 = 'O'">
				<xsl:apply-templates select="." mode="O"/>
			</xsl:when>
			<xsl:when test="$ch1 = 'P'">
				<xsl:apply-templates select="." mode="P"/>
			</xsl:when>
			<xsl:when test="$ch1 = 'Q'">
				<xsl:apply-templates select="." mode="Q"/>
			</xsl:when>
			<xsl:when test="$ch1 = 'R'">
				<xsl:apply-templates select="." mode="R"/>
			</xsl:when>
			<xsl:when test="$ch1 = 'S'">
				<xsl:apply-templates select="." mode="S"/>
			</xsl:when>
			<xsl:when test="$ch1 = 'T'">
				<xsl:apply-templates select="." mode="T"/>
			</xsl:when>
			<xsl:when test="$ch1 = 'U'">
				<xsl:apply-templates select="." mode="U"/>
			</xsl:when>
			<xsl:when test="$ch1 = 'V'">
				<xsl:apply-templates select="." mode="V"/>
			</xsl:when>
			<xsl:when test="$ch1 = 'W'">
				<xsl:apply-templates select="." mode="W"/>
			</xsl:when>
			<xsl:when test="$ch1 = 'Z'">
				<xsl:apply-templates select="." mode="Z"/>
			</xsl:when>
		</xsl:choose>
	</xsl:template>

	<xsl:template match="Name1" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="DateCreated1" mode="D">
		<DateCreated>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateCreated>
	</xsl:template>

	<xsl:template match="DateModified1" mode="D">
		<DateModified>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateModified>
	</xsl:template>

	<xsl:template match="Description1" mode="D">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Name2" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="SubFolders2" mode="S">
		<SubFolders>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SubFolders>
	</xsl:template>

	<xsl:template match="Description2" mode="D">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Files2" mode="F">
		<Files>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Files>
	</xsl:template>

	<xsl:template match="Type4" mode="T">
		<Type>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Type>
	</xsl:template>

	<xsl:template match="Name5" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="DateCreated5" mode="D">
		<DateCreated>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateCreated>
	</xsl:template>

	<xsl:template match="DateModified5" mode="D">
		<DateModified>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateModified>
	</xsl:template>

	<xsl:template match="Description5" mode="D">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Publications5" mode="P">
		<Publications>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Publications>
	</xsl:template>

	<xsl:template match="HeaderFooterSets5" mode="H">
		<HeaderFooterSets>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</HeaderFooterSets>
	</xsl:template>

	<xsl:template match="Name7" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Abbreviation7" mode="A">
		<Abbreviation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Abbreviation>
	</xsl:template>

	<xsl:template match="Description7" mode="D">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="SubPossibilities7" mode="S">
		<SubPossibilities>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SubPossibilities>
	</xsl:template>

	<xsl:template match="SortSpec7" mode="S">
		<SortSpec>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SortSpec>
	</xsl:template>

	<xsl:template match="Restrictions7" mode="R">
		<Restrictions>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Restrictions>
	</xsl:template>

	<xsl:template match="Confidence7" mode="C">
		<Confidence>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Confidence>
	</xsl:template>

	<xsl:template match="Status7" mode="S">
		<Status>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Status>
	</xsl:template>

	<xsl:template match="DateCreated7" mode="D">
		<DateCreated>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateCreated>
	</xsl:template>

	<xsl:template match="DateModified7" mode="D">
		<DateModified>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateModified>
	</xsl:template>

	<xsl:template match="Discussion7" mode="D">
		<Discussion>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Discussion>
	</xsl:template>

	<xsl:template match="Researchers7" mode="R">
		<Researchers>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Researchers>
	</xsl:template>

	<xsl:template match="HelpId7" mode="H">
		<HelpId>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</HelpId>
	</xsl:template>

	<xsl:template match="ForeColor7" mode="F">
		<ForeColor>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ForeColor>
	</xsl:template>

	<xsl:template match="BackColor7" mode="B">
		<BackColor>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BackColor>
	</xsl:template>

	<xsl:template match="UnderColor7" mode="U">
		<UnderColor>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</UnderColor>
	</xsl:template>

	<xsl:template match="UnderStyle7" mode="U">
		<UnderStyle>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</UnderStyle>
	</xsl:template>

	<xsl:template match="Hidden7" mode="H">
		<Hidden>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Hidden>
	</xsl:template>

	<xsl:template match="IsProtected7" mode="I">
		<IsProtected>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IsProtected>
	</xsl:template>

	<xsl:template match="Depth8" mode="D">
		<Depth>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Depth>
	</xsl:template>

	<xsl:template match="PreventChoiceAboveLevel8" mode="P">
		<PreventChoiceAboveLevel>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PreventChoiceAboveLevel>
	</xsl:template>

	<xsl:template match="IsSorted8" mode="I">
		<IsSorted>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IsSorted>
	</xsl:template>

	<xsl:template match="IsClosed8" mode="I">
		<IsClosed>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IsClosed>
	</xsl:template>

	<xsl:template match="PreventDuplicates8" mode="P">
		<PreventDuplicates>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PreventDuplicates>
	</xsl:template>

	<xsl:template match="PreventNodeChoices8" mode="P">
		<PreventNodeChoices>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PreventNodeChoices>
	</xsl:template>

	<xsl:template match="Possibilities8" mode="P">
		<Possibilities>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Possibilities>
	</xsl:template>

	<xsl:template match="Abbreviation8" mode="A">
		<Abbreviation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Abbreviation>
	</xsl:template>

	<xsl:template match="HelpFile8" mode="H">
		<HelpFile>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</HelpFile>
	</xsl:template>

	<xsl:template match="UseExtendedFields8" mode="U">
		<UseExtendedFields>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</UseExtendedFields>
	</xsl:template>

	<xsl:template match="DisplayOption8" mode="D">
		<DisplayOption>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DisplayOption>
	</xsl:template>

	<xsl:template match="ItemClsid8" mode="I">
		<ItemClsid>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ItemClsid>
	</xsl:template>

	<xsl:template match="IsVernacular8" mode="I">
		<IsVernacular>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IsVernacular>
	</xsl:template>

	<xsl:template match="WritingSystem8" mode="W">
		<WritingSystem>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</WritingSystem>
	</xsl:template>

	<xsl:template match="WsSelector8" mode="W">
		<WsSelector>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</WsSelector>
	</xsl:template>

	<xsl:template match="ListVersion8" mode="L">
		<ListVersion>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ListVersion>
	</xsl:template>

	<xsl:template match="Name9" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="ClassId9" mode="C">
		<ClassId>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ClassId>
	</xsl:template>

	<xsl:template match="FieldId9" mode="F">
		<FieldId>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FieldId>
	</xsl:template>

	<xsl:template match="FieldInfo9" mode="F">
		<FieldInfo>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FieldInfo>
	</xsl:template>

	<xsl:template match="App9" mode="A">
		<App>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</App>
	</xsl:template>

	<xsl:template match="Type9" mode="T">
		<Type>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Type>
	</xsl:template>

	<xsl:template match="Rows9" mode="R">
		<Rows>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Rows>
	</xsl:template>

	<xsl:template match="ColumnInfo9" mode="C">
		<ColumnInfo>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ColumnInfo>
	</xsl:template>

	<xsl:template match="ShowPrompt9" mode="S">
		<ShowPrompt>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ShowPrompt>
	</xsl:template>

	<xsl:template match="PromptText9" mode="P">
		<PromptText>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PromptText>
	</xsl:template>

	<xsl:template match="Cells10" mode="C">
		<Cells>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Cells>
	</xsl:template>

	<xsl:template match="Contents11" mode="C">
		<Contents>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Contents>
	</xsl:template>

	<xsl:template match="Alias12" mode="A">
		<Alias>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Alias>
	</xsl:template>

	<xsl:template match="Alias13" mode="A">
		<Alias>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Alias>
	</xsl:template>

	<xsl:template match="Gender13" mode="G">
		<Gender>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Gender>
	</xsl:template>

	<xsl:template match="DateOfBirth13" mode="D">
		<DateOfBirth>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateOfBirth>
	</xsl:template>

	<xsl:template match="PlaceOfBirth13" mode="P">
		<PlaceOfBirth>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PlaceOfBirth>
	</xsl:template>

	<xsl:template match="IsResearcher13" mode="I">
		<IsResearcher>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IsResearcher>
	</xsl:template>

	<xsl:template match="PlacesOfResidence13" mode="P">
		<PlacesOfResidence>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PlacesOfResidence>
	</xsl:template>

	<xsl:template match="Education13" mode="E">
		<Education>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Education>
	</xsl:template>

	<xsl:template match="DateOfDeath13" mode="D">
		<DateOfDeath>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateOfDeath>
	</xsl:template>

	<xsl:template match="Positions13" mode="P">
		<Positions>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Positions>
	</xsl:template>

	<xsl:template match="Paragraphs14" mode="P">
		<Paragraphs>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Paragraphs>
	</xsl:template>

	<xsl:template match="RightToLeft14" mode="R">
		<RightToLeft>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RightToLeft>
	</xsl:template>

	<xsl:template match="StyleName15" mode="S">
		<StyleName>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</StyleName>
	</xsl:template>

	<xsl:template match="StyleRules15" mode="S">
		<StyleRules>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</StyleRules>
	</xsl:template>

	<xsl:template match="Label16" mode="L">
		<Label>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Label>
	</xsl:template>

	<xsl:template match="Contents16" mode="C">
		<Contents>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Contents>
	</xsl:template>

	<xsl:template match="TextObjects16" mode="T">
		<TextObjects>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</TextObjects>
	</xsl:template>

	<xsl:template match="AnalyzedTextObjects16" mode="A">
		<AnalyzedTextObjects>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AnalyzedTextObjects>
	</xsl:template>

	<xsl:template match="ObjRefs16" mode="O">
		<ObjRefs>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ObjRefs>
	</xsl:template>

	<xsl:template match="Translations16" mode="T">
		<Translations>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Translations>
	</xsl:template>

	<xsl:template match="Name17" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="BasedOn17" mode="B">
		<BasedOn>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BasedOn>
	</xsl:template>

	<xsl:template match="Next17" mode="N">
		<Next>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Next>
	</xsl:template>

	<xsl:template match="Type17" mode="T">
		<Type>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Type>
	</xsl:template>

	<xsl:template match="Rules17" mode="R">
		<Rules>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Rules>
	</xsl:template>

	<xsl:template match="IsPublishedTextStyle17" mode="I">
		<IsPublishedTextStyle>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IsPublishedTextStyle>
	</xsl:template>

	<xsl:template match="IsBuiltIn17" mode="I">
		<IsBuiltIn>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IsBuiltIn>
	</xsl:template>

	<xsl:template match="IsModified17" mode="I">
		<IsModified>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IsModified>
	</xsl:template>

	<xsl:template match="UserLevel17" mode="U">
		<UserLevel>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</UserLevel>
	</xsl:template>

	<xsl:template match="Context17" mode="C">
		<Context>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Context>
	</xsl:template>

	<xsl:template match="Structure17" mode="S">
		<Structure>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Structure>
	</xsl:template>

	<xsl:template match="Function17" mode="F">
		<Function>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Function>
	</xsl:template>

	<xsl:template match="Usage17" mode="U">
		<Usage>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Usage>
	</xsl:template>

	<xsl:template match="Name18" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Type18" mode="T">
		<Type>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Type>
	</xsl:template>

	<xsl:template match="App18" mode="A">
		<App>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</App>
	</xsl:template>

	<xsl:template match="Records18" mode="R">
		<Records>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Records>
	</xsl:template>

	<xsl:template match="Details18" mode="D">
		<Details>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Details>
	</xsl:template>

	<xsl:template match="System18" mode="S">
		<System>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</System>
	</xsl:template>

	<xsl:template match="SubType18" mode="S">
		<SubType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SubType>
	</xsl:template>

	<xsl:template match="Clsid19" mode="C">
		<Clsid>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Clsid>
	</xsl:template>

	<xsl:template match="Level19" mode="L">
		<Level>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Level>
	</xsl:template>

	<xsl:template match="Fields19" mode="F">
		<Fields>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Fields>
	</xsl:template>

	<xsl:template match="Details19" mode="D">
		<Details>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Details>
	</xsl:template>

	<xsl:template match="Label20" mode="L">
		<Label>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Label>
	</xsl:template>

	<xsl:template match="HelpString20" mode="H">
		<HelpString>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</HelpString>
	</xsl:template>

	<xsl:template match="Type20" mode="T">
		<Type>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Type>
	</xsl:template>

	<xsl:template match="Flid20" mode="F">
		<Flid>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Flid>
	</xsl:template>

	<xsl:template match="Visibility20" mode="V">
		<Visibility>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Visibility>
	</xsl:template>

	<xsl:template match="Required20" mode="R">
		<Required>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Required>
	</xsl:template>

	<xsl:template match="Style20" mode="S">
		<Style>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Style>
	</xsl:template>

	<xsl:template match="SubfieldOf20" mode="S">
		<SubfieldOf>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SubfieldOf>
	</xsl:template>

	<xsl:template match="Details20" mode="D">
		<Details>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Details>
	</xsl:template>

	<xsl:template match="IsCustomField20" mode="I">
		<IsCustomField>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IsCustomField>
	</xsl:template>

	<xsl:template match="PossList20" mode="P">
		<PossList>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PossList>
	</xsl:template>

	<xsl:template match="WritingSystem20" mode="W">
		<WritingSystem>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</WritingSystem>
	</xsl:template>

	<xsl:template match="WsSelector20" mode="W">
		<WsSelector>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</WsSelector>
	</xsl:template>

	<xsl:template match="Name21" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="PossList21" mode="P">
		<PossList>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PossList>
	</xsl:template>

	<xsl:template match="PossItems21" mode="P">
		<PossItems>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PossItems>
	</xsl:template>

	<xsl:template match="Name23" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="StateInformation23" mode="S">
		<StateInformation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</StateInformation>
	</xsl:template>

	<xsl:template match="Human23" mode="H">
		<Human>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Human>
	</xsl:template>

	<xsl:template match="Notes23" mode="N">
		<Notes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Notes>
	</xsl:template>

	<xsl:template match="Version23" mode="V">
		<Version>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Version>
	</xsl:template>

	<xsl:template match="Evaluations23" mode="E">
		<Evaluations>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Evaluations>
	</xsl:template>

	<xsl:template match="Name24" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Locale24" mode="L">
		<Locale>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Locale>
	</xsl:template>

	<xsl:template match="Abbr24" mode="A">
		<Abbr>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Abbr>
	</xsl:template>

	<xsl:template match="DefaultMonospace24" mode="D">
		<DefaultMonospace>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DefaultMonospace>
	</xsl:template>

	<xsl:template match="DefaultSansSerif24" mode="D">
		<DefaultSansSerif>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DefaultSansSerif>
	</xsl:template>

	<xsl:template match="DefaultSerif24" mode="D">
		<DefaultSerif>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DefaultSerif>
	</xsl:template>

	<xsl:template match="FontVariation24" mode="F">
		<FontVariation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FontVariation>
	</xsl:template>

	<xsl:template match="KeyboardType24" mode="K">
		<KeyboardType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</KeyboardType>
	</xsl:template>

	<xsl:template match="RightToLeft24" mode="R">
		<RightToLeft>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RightToLeft>
	</xsl:template>

	<xsl:template match="Collations24" mode="C">
		<Collations>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Collations>
	</xsl:template>

	<xsl:template match="Description24" mode="D">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="ICULocale24" mode="I">
		<ICULocale>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ICULocale>
	</xsl:template>

	<xsl:template match="KeymanKeyboard24" mode="K">
		<KeymanKeyboard>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</KeymanKeyboard>
	</xsl:template>

	<xsl:template match="LegacyMapping24" mode="L">
		<LegacyMapping>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LegacyMapping>
	</xsl:template>

	<xsl:template match="SansFontVariation24" mode="S">
		<SansFontVariation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SansFontVariation>
	</xsl:template>

	<xsl:template match="LastModified24" mode="L">
		<LastModified>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LastModified>
	</xsl:template>

	<xsl:template match="DefaultBodyFont24" mode="D">
		<DefaultBodyFont>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DefaultBodyFont>
	</xsl:template>

	<xsl:template match="BodyFontFeatures24" mode="B">
		<BodyFontFeatures>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BodyFontFeatures>
	</xsl:template>

	<xsl:template match="ValidChars24" mode="V">
		<ValidChars>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ValidChars>
	</xsl:template>

	<xsl:template match="SpellCheckDictionary24" mode="S">
		<SpellCheckDictionary>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SpellCheckDictionary>
	</xsl:template>

	<xsl:template match="MatchedPairs24" mode="M">
		<MatchedPairs>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MatchedPairs>
	</xsl:template>

	<xsl:template match="PunctuationPatterns24" mode="P">
		<PunctuationPatterns>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PunctuationPatterns>
	</xsl:template>

	<xsl:template match="CapitalizationInfo24" mode="C">
		<CapitalizationInfo>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CapitalizationInfo>
	</xsl:template>

	<xsl:template match="QuotationMarks24" mode="Q">
		<QuotationMarks>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</QuotationMarks>
	</xsl:template>

	<xsl:template match="Comment28" mode="C">
		<Comment>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Comment>
	</xsl:template>

	<xsl:template match="Translation29" mode="T">
		<Translation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Translation>
	</xsl:template>

	<xsl:template match="Type29" mode="T">
		<Type>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Type>
	</xsl:template>

	<xsl:template match="Status29" mode="S">
		<Status>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Status>
	</xsl:template>

	<xsl:template match="Name30" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="WinLCID30" mode="W">
		<WinLCID>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</WinLCID>
	</xsl:template>

	<xsl:template match="WinCollation30" mode="W">
		<WinCollation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</WinCollation>
	</xsl:template>

	<xsl:template match="IcuResourceName30" mode="I">
		<IcuResourceName>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IcuResourceName>
	</xsl:template>

	<xsl:template match="IcuResourceText30" mode="I">
		<IcuResourceText>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IcuResourceText>
	</xsl:template>

	<xsl:template match="ICURules30" mode="I">
		<ICURules>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ICURules>
	</xsl:template>

	<xsl:template match="Name31" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="App31" mode="A">
		<App>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</App>
	</xsl:template>

	<xsl:template match="ClassId31" mode="C">
		<ClassId>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ClassId>
	</xsl:template>

	<xsl:template match="PrimaryField31" mode="P">
		<PrimaryField>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PrimaryField>
	</xsl:template>

	<xsl:template match="PrimaryCollType31" mode="P">
		<PrimaryCollType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PrimaryCollType>
	</xsl:template>

	<xsl:template match="PrimaryReverse31" mode="P">
		<PrimaryReverse>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PrimaryReverse>
	</xsl:template>

	<xsl:template match="SecondaryField31" mode="S">
		<SecondaryField>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SecondaryField>
	</xsl:template>

	<xsl:template match="SecondaryCollType31" mode="S">
		<SecondaryCollType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SecondaryCollType>
	</xsl:template>

	<xsl:template match="SecondaryReverse31" mode="S">
		<SecondaryReverse>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SecondaryReverse>
	</xsl:template>

	<xsl:template match="TertiaryField31" mode="T">
		<TertiaryField>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</TertiaryField>
	</xsl:template>

	<xsl:template match="TertiaryCollType31" mode="T">
		<TertiaryCollType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</TertiaryCollType>
	</xsl:template>

	<xsl:template match="TertiaryReverse31" mode="T">
		<TertiaryReverse>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</TertiaryReverse>
	</xsl:template>

	<xsl:template match="IncludeSubentries31" mode="I">
		<IncludeSubentries>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IncludeSubentries>
	</xsl:template>

	<xsl:template match="PrimaryWs31" mode="P">
		<PrimaryWs>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PrimaryWs>
	</xsl:template>

	<xsl:template match="SecondaryWs31" mode="S">
		<SecondaryWs>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SecondaryWs>
	</xsl:template>

	<xsl:template match="TertiaryWs31" mode="T">
		<TertiaryWs>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</TertiaryWs>
	</xsl:template>

	<xsl:template match="PrimaryCollation31" mode="P">
		<PrimaryCollation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PrimaryCollation>
	</xsl:template>

	<xsl:template match="SecondaryCollation31" mode="S">
		<SecondaryCollation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SecondaryCollation>
	</xsl:template>

	<xsl:template match="TertiaryCollation31" mode="T">
		<TertiaryCollation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</TertiaryCollation>
	</xsl:template>

	<xsl:template match="Target32" mode="T">
		<Target>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Target>
	</xsl:template>

	<xsl:template match="DateCreated32" mode="D">
		<DateCreated>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateCreated>
	</xsl:template>

	<xsl:template match="Accepted32" mode="A">
		<Accepted>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Accepted>
	</xsl:template>

	<xsl:template match="Details32" mode="D">
		<Details>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Details>
	</xsl:template>

	<xsl:template match="CompDetails34" mode="C">
		<CompDetails>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CompDetails>
	</xsl:template>

	<xsl:template match="Comment34" mode="C">
		<Comment>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Comment>
	</xsl:template>

	<xsl:template match="AnnotationType34" mode="A">
		<AnnotationType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AnnotationType>
	</xsl:template>

	<xsl:template match="Source34" mode="S">
		<Source>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Source>
	</xsl:template>

	<xsl:template match="InstanceOf34" mode="I">
		<InstanceOf>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</InstanceOf>
	</xsl:template>

	<xsl:template match="Text34" mode="T">
		<Text>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Text>
	</xsl:template>

	<xsl:template match="Features34" mode="F">
		<Features>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Features>
	</xsl:template>

	<xsl:template match="DateCreated34" mode="D">
		<DateCreated>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateCreated>
	</xsl:template>

	<xsl:template match="DateModified34" mode="D">
		<DateModified>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateModified>
	</xsl:template>

	<xsl:template match="AllowsComment35" mode="A">
		<AllowsComment>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AllowsComment>
	</xsl:template>

	<xsl:template match="AllowsFeatureStructure35" mode="A">
		<AllowsFeatureStructure>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AllowsFeatureStructure>
	</xsl:template>

	<xsl:template match="AllowsInstanceOf35" mode="A">
		<AllowsInstanceOf>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AllowsInstanceOf>
	</xsl:template>

	<xsl:template match="InstanceOfSignature35" mode="I">
		<InstanceOfSignature>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</InstanceOfSignature>
	</xsl:template>

	<xsl:template match="UserCanCreate35" mode="U">
		<UserCanCreate>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</UserCanCreate>
	</xsl:template>

	<xsl:template match="CanCreateOrphan35" mode="C">
		<CanCreateOrphan>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CanCreateOrphan>
	</xsl:template>

	<xsl:template match="PromptUser35" mode="P">
		<PromptUser>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PromptUser>
	</xsl:template>

	<xsl:template match="CopyCutPastable35" mode="C">
		<CopyCutPastable>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CopyCutPastable>
	</xsl:template>

	<xsl:template match="ZeroWidth35" mode="Z">
		<ZeroWidth>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ZeroWidth>
	</xsl:template>

	<xsl:template match="Multi35" mode="M">
		<Multi>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Multi>
	</xsl:template>

	<xsl:template match="Severity35" mode="S">
		<Severity>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Severity>
	</xsl:template>

	<xsl:template match="MaxDupOccur35" mode="M">
		<MaxDupOccur>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MaxDupOccur>
	</xsl:template>

	<xsl:template match="AppliesTo36" mode="A">
		<AppliesTo>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AppliesTo>
	</xsl:template>

	<xsl:template match="BeginOffset37" mode="B">
		<BeginOffset>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BeginOffset>
	</xsl:template>

	<xsl:template match="Flid37" mode="F">
		<Flid>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Flid>
	</xsl:template>

	<xsl:template match="EndOffset37" mode="E">
		<EndOffset>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</EndOffset>
	</xsl:template>

	<xsl:template match="BeginObject37" mode="B">
		<BeginObject>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BeginObject>
	</xsl:template>

	<xsl:template match="EndObject37" mode="E">
		<EndObject>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</EndObject>
	</xsl:template>

	<xsl:template match="OtherObjects37" mode="O">
		<OtherObjects>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</OtherObjects>
	</xsl:template>

	<xsl:template match="WritingSystem37" mode="W">
		<WritingSystem>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</WritingSystem>
	</xsl:template>

	<xsl:template match="WsSelector37" mode="W">
		<WsSelector>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</WsSelector>
	</xsl:template>

	<xsl:template match="BeginRef37" mode="B">
		<BeginRef>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BeginRef>
	</xsl:template>

	<xsl:template match="EndRef37" mode="E">
		<EndRef>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</EndRef>
	</xsl:template>

	<xsl:template match="FootnoteMarker39" mode="F">
		<FootnoteMarker>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FootnoteMarker>
	</xsl:template>

	<xsl:template match="DisplayFootnoteReference39" mode="D">
		<DisplayFootnoteReference>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DisplayFootnoteReference>
	</xsl:template>

	<xsl:template match="DisplayFootnoteMarker39" mode="D">
		<DisplayFootnoteMarker>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DisplayFootnoteMarker>
	</xsl:template>

	<xsl:template match="Sid40" mode="S">
		<Sid>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Sid>
	</xsl:template>

	<xsl:template match="UserLevel40" mode="U">
		<UserLevel>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</UserLevel>
	</xsl:template>

	<xsl:template match="HasMaintenance40" mode="H">
		<HasMaintenance>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</HasMaintenance>
	</xsl:template>

	<xsl:template match="UserConfigAcct41" mode="U">
		<UserConfigAcct>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</UserConfigAcct>
	</xsl:template>

	<xsl:template match="ApplicationId41" mode="A">
		<ApplicationId>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ApplicationId>
	</xsl:template>

	<xsl:template match="FeatureId41" mode="F">
		<FeatureId>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FeatureId>
	</xsl:template>

	<xsl:template match="ActivatedLevel41" mode="A">
		<ActivatedLevel>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ActivatedLevel>
	</xsl:template>

	<xsl:template match="Name42" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Description42" mode="D">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="PageHeight42" mode="P">
		<PageHeight>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PageHeight>
	</xsl:template>

	<xsl:template match="PageWidth42" mode="P">
		<PageWidth>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PageWidth>
	</xsl:template>

	<xsl:template match="IsLandscape42" mode="I">
		<IsLandscape>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IsLandscape>
	</xsl:template>

	<xsl:template match="GutterMargin42" mode="G">
		<GutterMargin>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</GutterMargin>
	</xsl:template>

	<xsl:template match="GutterLoc42" mode="G">
		<GutterLoc>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</GutterLoc>
	</xsl:template>

	<xsl:template match="Divisions42" mode="D">
		<Divisions>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Divisions>
	</xsl:template>

	<xsl:template match="FootnoteSepWidth42" mode="F">
		<FootnoteSepWidth>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FootnoteSepWidth>
	</xsl:template>

	<xsl:template match="PaperHeight42" mode="P">
		<PaperHeight>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PaperHeight>
	</xsl:template>

	<xsl:template match="PaperWidth42" mode="P">
		<PaperWidth>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PaperWidth>
	</xsl:template>

	<xsl:template match="BindingEdge42" mode="B">
		<BindingEdge>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BindingEdge>
	</xsl:template>

	<xsl:template match="SheetLayout42" mode="S">
		<SheetLayout>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SheetLayout>
	</xsl:template>

	<xsl:template match="SheetsPerSig42" mode="S">
		<SheetsPerSig>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SheetsPerSig>
	</xsl:template>

	<xsl:template match="BaseFontSize42" mode="B">
		<BaseFontSize>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BaseFontSize>
	</xsl:template>

	<xsl:template match="BaseLineSpacing42" mode="B">
		<BaseLineSpacing>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BaseLineSpacing>
	</xsl:template>

	<xsl:template match="DifferentFirstHF43" mode="D">
		<DifferentFirstHF>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DifferentFirstHF>
	</xsl:template>

	<xsl:template match="DifferentEvenHF43" mode="D">
		<DifferentEvenHF>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DifferentEvenHF>
	</xsl:template>

	<xsl:template match="StartAt43" mode="S">
		<StartAt>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</StartAt>
	</xsl:template>

	<xsl:template match="PageLayout43" mode="P">
		<PageLayout>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PageLayout>
	</xsl:template>

	<xsl:template match="HFSet43" mode="H">
		<HFSet>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</HFSet>
	</xsl:template>

	<xsl:template match="NumColumns43" mode="N">
		<NumColumns>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</NumColumns>
	</xsl:template>

	<xsl:template match="Name44" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Description44" mode="D">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="MarginTop44" mode="M">
		<MarginTop>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MarginTop>
	</xsl:template>

	<xsl:template match="MarginBottom44" mode="M">
		<MarginBottom>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MarginBottom>
	</xsl:template>

	<xsl:template match="MarginInside44" mode="M">
		<MarginInside>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MarginInside>
	</xsl:template>

	<xsl:template match="MarginOutside44" mode="M">
		<MarginOutside>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MarginOutside>
	</xsl:template>

	<xsl:template match="PosHeader44" mode="P">
		<PosHeader>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PosHeader>
	</xsl:template>

	<xsl:template match="PosFooter44" mode="P">
		<PosFooter>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PosFooter>
	</xsl:template>

	<xsl:template match="IsBuiltIn44" mode="I">
		<IsBuiltIn>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IsBuiltIn>
	</xsl:template>

	<xsl:template match="IsModified44" mode="I">
		<IsModified>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IsModified>
	</xsl:template>

	<xsl:template match="Name45" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Description45" mode="D">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="DefaultHeader45" mode="D">
		<DefaultHeader>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DefaultHeader>
	</xsl:template>

	<xsl:template match="DefaultFooter45" mode="D">
		<DefaultFooter>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DefaultFooter>
	</xsl:template>

	<xsl:template match="FirstHeader45" mode="F">
		<FirstHeader>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FirstHeader>
	</xsl:template>

	<xsl:template match="FirstFooter45" mode="F">
		<FirstFooter>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FirstFooter>
	</xsl:template>

	<xsl:template match="EvenHeader45" mode="E">
		<EvenHeader>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</EvenHeader>
	</xsl:template>

	<xsl:template match="EvenFooter45" mode="E">
		<EvenFooter>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</EvenFooter>
	</xsl:template>

	<xsl:template match="InsideAlignedText46" mode="I">
		<InsideAlignedText>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</InsideAlignedText>
	</xsl:template>

	<xsl:template match="CenteredText46" mode="C">
		<CenteredText>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CenteredText>
	</xsl:template>

	<xsl:template match="OutsideAlignedText46" mode="O">
		<OutsideAlignedText>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</OutsideAlignedText>
	</xsl:template>

	<xsl:template match="Name47" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Description47" mode="D">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="OriginalPath47" mode="O">
		<OriginalPath>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</OriginalPath>
	</xsl:template>

	<xsl:template match="InternalPath47" mode="I">
		<InternalPath>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</InternalPath>
	</xsl:template>

	<xsl:template match="Copyright47" mode="C">
		<Copyright>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Copyright>
	</xsl:template>

	<xsl:template match="PictureFile48" mode="P">
		<PictureFile>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PictureFile>
	</xsl:template>

	<xsl:template match="Caption48" mode="C">
		<Caption>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Caption>
	</xsl:template>

	<xsl:template match="Description48" mode="D">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="LayoutPos48" mode="L">
		<LayoutPos>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LayoutPos>
	</xsl:template>

	<xsl:template match="ScaleFactor48" mode="S">
		<ScaleFactor>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ScaleFactor>
	</xsl:template>

	<xsl:template match="LocationRangeType48" mode="L">
		<LocationRangeType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LocationRangeType>
	</xsl:template>

	<xsl:template match="LocationMin48" mode="L">
		<LocationMin>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LocationMin>
	</xsl:template>

	<xsl:template match="LocationMax48" mode="L">
		<LocationMax>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LocationMax>
	</xsl:template>

	<xsl:template match="Types49" mode="T">
		<Types>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Types>
	</xsl:template>

	<xsl:template match="Features49" mode="F">
		<Features>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Features>
	</xsl:template>

	<xsl:template match="Values50" mode="V">
		<Values>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Values>
	</xsl:template>

	<xsl:template match="Value51" mode="V">
		<Value>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Value>
	</xsl:template>

	<xsl:template match="Value53" mode="V">
		<Value>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Value>
	</xsl:template>

	<xsl:template match="Value54" mode="V">
		<Value>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Value>
	</xsl:template>

	<xsl:template match="Name55" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Abbreviation55" mode="A">
		<Abbreviation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Abbreviation>
	</xsl:template>

	<xsl:template match="Description55" mode="D">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Default55" mode="D">
		<Default>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Default>
	</xsl:template>

	<xsl:template match="GlossAbbreviation55" mode="G">
		<GlossAbbreviation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</GlossAbbreviation>
	</xsl:template>

	<xsl:template match="RightGlossSep55" mode="R">
		<RightGlossSep>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RightGlossSep>
	</xsl:template>

	<xsl:template match="ShowInGloss55" mode="S">
		<ShowInGloss>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ShowInGloss>
	</xsl:template>

	<xsl:template match="DisplayToRightOfValues55" mode="D">
		<DisplayToRightOfValues>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DisplayToRightOfValues>
	</xsl:template>

	<xsl:template match="CatalogSourceId55" mode="C">
		<CatalogSourceId>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CatalogSourceId>
	</xsl:template>

	<xsl:template match="RefNumber56" mode="R">
		<RefNumber>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RefNumber>
	</xsl:template>

	<xsl:template match="ValueState56" mode="V">
		<ValueState>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ValueState>
	</xsl:template>

	<xsl:template match="Feature56" mode="F">
		<Feature>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Feature>
	</xsl:template>

	<xsl:template match="FeatureDisjunctions57" mode="F">
		<FeatureDisjunctions>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FeatureDisjunctions>
	</xsl:template>

	<xsl:template match="FeatureSpecs57" mode="F">
		<FeatureSpecs>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FeatureSpecs>
	</xsl:template>

	<xsl:template match="Type57" mode="T">
		<Type>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Type>
	</xsl:template>

	<xsl:template match="Contents58" mode="C">
		<Contents>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Contents>
	</xsl:template>

	<xsl:template match="Name59" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Abbreviation59" mode="A">
		<Abbreviation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Abbreviation>
	</xsl:template>

	<xsl:template match="Description59" mode="D">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Features59" mode="F">
		<Features>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Features>
	</xsl:template>

	<xsl:template match="CatalogSourceId59" mode="C">
		<CatalogSourceId>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CatalogSourceId>
	</xsl:template>

	<xsl:template match="Value61" mode="V">
		<Value>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Value>
	</xsl:template>

	<xsl:template match="WritingSystem62" mode="W">
		<WritingSystem>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</WritingSystem>
	</xsl:template>

	<xsl:template match="WsSelector62" mode="W">
		<WsSelector>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</WsSelector>
	</xsl:template>

	<xsl:template match="Value63" mode="V">
		<Value>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Value>
	</xsl:template>

	<xsl:template match="Value64" mode="V">
		<Value>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Value>
	</xsl:template>

	<xsl:template match="Name65" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Abbreviation65" mode="A">
		<Abbreviation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Abbreviation>
	</xsl:template>

	<xsl:template match="Description65" mode="D">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="GlossAbbreviation65" mode="G">
		<GlossAbbreviation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</GlossAbbreviation>
	</xsl:template>

	<xsl:template match="RightGlossSep65" mode="R">
		<RightGlossSep>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RightGlossSep>
	</xsl:template>

	<xsl:template match="ShowInGloss65" mode="S">
		<ShowInGloss>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ShowInGloss>
	</xsl:template>

	<xsl:template match="CatalogSourceId65" mode="C">
		<CatalogSourceId>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CatalogSourceId>
	</xsl:template>

	<xsl:template match="LouwNidaCodes66" mode="L">
		<LouwNidaCodes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LouwNidaCodes>
	</xsl:template>

	<xsl:template match="OcmCodes66" mode="O">
		<OcmCodes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</OcmCodes>
	</xsl:template>

	<xsl:template match="OcmRefs66" mode="O">
		<OcmRefs>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</OcmRefs>
	</xsl:template>

	<xsl:template match="RelatedDomains66" mode="R">
		<RelatedDomains>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RelatedDomains>
	</xsl:template>

	<xsl:template match="Questions66" mode="Q">
		<Questions>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Questions>
	</xsl:template>

	<xsl:template match="Question67" mode="Q">
		<Question>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Question>
	</xsl:template>

	<xsl:template match="ExampleWords67" mode="E">
		<ExampleWords>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ExampleWords>
	</xsl:template>

	<xsl:template match="ExampleSentences67" mode="E">
		<ExampleSentences>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ExampleSentences>
	</xsl:template>

	<xsl:template match="DateCreated68" mode="D">
		<DateCreated>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateCreated>
	</xsl:template>

	<xsl:template match="DateModified68" mode="D">
		<DateModified>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateModified>
	</xsl:template>

	<xsl:template match="CreatedBy68" mode="C">
		<CreatedBy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CreatedBy>
	</xsl:template>

	<xsl:template match="ModifiedBy68" mode="M">
		<ModifiedBy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ModifiedBy>
	</xsl:template>

	<xsl:template match="Label69" mode="L">
		<Label>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Label>
	</xsl:template>

	<xsl:template match="MediaFile69" mode="M">
		<MediaFile>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MediaFile>
	</xsl:template>

	<xsl:template match="Name70" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Version70" mode="V">
		<Version>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Version>
	</xsl:template>

	<xsl:template match="ScriptureBooks3001" mode="S">
		<ScriptureBooks>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ScriptureBooks>
	</xsl:template>

	<xsl:template match="Styles3001" mode="S">
		<Styles>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Styles>
	</xsl:template>

	<xsl:template match="RefSepr3001" mode="R">
		<RefSepr>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RefSepr>
	</xsl:template>

	<xsl:template match="ChapterVerseSepr3001" mode="C">
		<ChapterVerseSepr>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ChapterVerseSepr>
	</xsl:template>

	<xsl:template match="VerseSepr3001" mode="V">
		<VerseSepr>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</VerseSepr>
	</xsl:template>

	<xsl:template match="Bridge3001" mode="B">
		<Bridge>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Bridge>
	</xsl:template>

	<xsl:template match="ImportSettings3001" mode="I">
		<ImportSettings>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ImportSettings>
	</xsl:template>

	<xsl:template match="ArchivedDrafts3001" mode="A">
		<ArchivedDrafts>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ArchivedDrafts>
	</xsl:template>

	<xsl:template match="FootnoteMarkerSymbol3001" mode="F">
		<FootnoteMarkerSymbol>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FootnoteMarkerSymbol>
	</xsl:template>

	<xsl:template match="DisplayFootnoteReference3001" mode="D">
		<DisplayFootnoteReference>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DisplayFootnoteReference>
	</xsl:template>

	<xsl:template match="RestartFootnoteSequence3001" mode="R">
		<RestartFootnoteSequence>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RestartFootnoteSequence>
	</xsl:template>

	<xsl:template match="RestartFootnoteBoundary3001" mode="R">
		<RestartFootnoteBoundary>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RestartFootnoteBoundary>
	</xsl:template>

	<xsl:template match="UseScriptDigits3001" mode="U">
		<UseScriptDigits>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</UseScriptDigits>
	</xsl:template>

	<xsl:template match="ScriptDigitZero3001" mode="S">
		<ScriptDigitZero>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ScriptDigitZero>
	</xsl:template>

	<xsl:template match="ConvertCVDigitsOnExport3001" mode="C">
		<ConvertCVDigitsOnExport>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ConvertCVDigitsOnExport>
	</xsl:template>

	<xsl:template match="Versification3001" mode="V">
		<Versification>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Versification>
	</xsl:template>

	<xsl:template match="VersePunct3001" mode="V">
		<VersePunct>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</VersePunct>
	</xsl:template>

	<xsl:template match="ChapterLabel3001" mode="C">
		<ChapterLabel>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ChapterLabel>
	</xsl:template>

	<xsl:template match="PsalmLabel3001" mode="P">
		<PsalmLabel>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PsalmLabel>
	</xsl:template>

	<xsl:template match="BookAnnotations3001" mode="B">
		<BookAnnotations>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BookAnnotations>
	</xsl:template>

	<xsl:template match="NoteCategories3001" mode="N">
		<NoteCategories>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</NoteCategories>
	</xsl:template>

	<xsl:template match="FootnoteMarkerType3001" mode="F">
		<FootnoteMarkerType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FootnoteMarkerType>
	</xsl:template>

	<xsl:template match="DisplayCrossRefReference3001" mode="D">
		<DisplayCrossRefReference>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DisplayCrossRefReference>
	</xsl:template>

	<xsl:template match="CrossRefMarkerSymbol3001" mode="C">
		<CrossRefMarkerSymbol>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CrossRefMarkerSymbol>
	</xsl:template>

	<xsl:template match="CrossRefMarkerType3001" mode="C">
		<CrossRefMarkerType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CrossRefMarkerType>
	</xsl:template>

	<xsl:template match="CrossRefsCombinedWithFootnotes3001" mode="C">
		<CrossRefsCombinedWithFootnotes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CrossRefsCombinedWithFootnotes>
	</xsl:template>

	<xsl:template match="DisplaySymbolInFootnote3001" mode="D">
		<DisplaySymbolInFootnote>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DisplaySymbolInFootnote>
	</xsl:template>

	<xsl:template match="DisplaySymbolInCrossRef3001" mode="D">
		<DisplaySymbolInCrossRef>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DisplaySymbolInCrossRef>
	</xsl:template>

	<xsl:template match="Resources3001" mode="R">
		<Resources>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Resources>
	</xsl:template>

	<xsl:template match="Sections3002" mode="S">
		<Sections>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Sections>
	</xsl:template>

	<xsl:template match="Name3002" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="BookId3002" mode="B">
		<BookId>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BookId>
	</xsl:template>

	<xsl:template match="Title3002" mode="T">
		<Title>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Title>
	</xsl:template>

	<xsl:template match="Abbrev3002" mode="A">
		<Abbrev>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Abbrev>
	</xsl:template>

	<xsl:template match="IdText3002" mode="I">
		<IdText>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IdText>
	</xsl:template>

	<xsl:template match="Footnotes3002" mode="F">
		<Footnotes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Footnotes>
	</xsl:template>

	<xsl:template match="Diffs3002" mode="D">
		<Diffs>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Diffs>
	</xsl:template>

	<xsl:template match="UseChapterNumHeading3002" mode="U">
		<UseChapterNumHeading>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</UseChapterNumHeading>
	</xsl:template>

	<xsl:template match="CanonicalNum3002" mode="C">
		<CanonicalNum>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CanonicalNum>
	</xsl:template>

	<xsl:template match="Books3003" mode="B">
		<Books>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Books>
	</xsl:template>

	<xsl:template match="BookName3004" mode="B">
		<BookName>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BookName>
	</xsl:template>

	<xsl:template match="BookAbbrev3004" mode="B">
		<BookAbbrev>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BookAbbrev>
	</xsl:template>

	<xsl:template match="BookNameAlt3004" mode="B">
		<BookNameAlt>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BookNameAlt>
	</xsl:template>

	<xsl:template match="Heading3005" mode="H">
		<Heading>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Heading>
	</xsl:template>

	<xsl:template match="Content3005" mode="C">
		<Content>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Content>
	</xsl:template>

	<xsl:template match="VerseRefStart3005" mode="V">
		<VerseRefStart>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</VerseRefStart>
	</xsl:template>

	<xsl:template match="VerseRefEnd3005" mode="V">
		<VerseRefEnd>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</VerseRefEnd>
	</xsl:template>

	<xsl:template match="VerseRefMin3005" mode="V">
		<VerseRefMin>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</VerseRefMin>
	</xsl:template>

	<xsl:template match="VerseRefMax3005" mode="V">
		<VerseRefMax>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</VerseRefMax>
	</xsl:template>

	<xsl:template match="ImportType3008" mode="I">
		<ImportType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ImportType>
	</xsl:template>

	<xsl:template match="ImportSettings3008" mode="I">
		<ImportSettings>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ImportSettings>
	</xsl:template>

	<xsl:template match="ImportProjToken3008" mode="I">
		<ImportProjToken>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ImportProjToken>
	</xsl:template>

	<xsl:template match="Name3008" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Description3008" mode="D">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="ScriptureSources3008" mode="S">
		<ScriptureSources>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ScriptureSources>
	</xsl:template>

	<xsl:template match="BackTransSources3008" mode="B">
		<BackTransSources>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BackTransSources>
	</xsl:template>

	<xsl:template match="NoteSources3008" mode="N">
		<NoteSources>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</NoteSources>
	</xsl:template>

	<xsl:template match="ScriptureMappings3008" mode="S">
		<ScriptureMappings>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ScriptureMappings>
	</xsl:template>

	<xsl:template match="NoteMappings3008" mode="N">
		<NoteMappings>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</NoteMappings>
	</xsl:template>

	<xsl:template match="Description3010" mode="D">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Books3010" mode="B">
		<Books>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Books>
	</xsl:template>

	<xsl:template match="DateCreated3010" mode="D">
		<DateCreated>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateCreated>
	</xsl:template>

	<xsl:template match="Type3010" mode="T">
		<Type>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Type>
	</xsl:template>

	<xsl:template match="Protected3010" mode="P">
		<Protected>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Protected>
	</xsl:template>

	<xsl:template match="RefStart3011" mode="R">
		<RefStart>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RefStart>
	</xsl:template>

	<xsl:template match="RefEnd3011" mode="R">
		<RefEnd>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RefEnd>
	</xsl:template>

	<xsl:template match="DiffType3011" mode="D">
		<DiffType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DiffType>
	</xsl:template>

	<xsl:template match="RevMin3011" mode="R">
		<RevMin>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RevMin>
	</xsl:template>

	<xsl:template match="RevLim3011" mode="R">
		<RevLim>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RevLim>
	</xsl:template>

	<xsl:template match="RevParagraph3011" mode="R">
		<RevParagraph>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RevParagraph>
	</xsl:template>

	<xsl:template match="ICULocale3013" mode="I">
		<ICULocale>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ICULocale>
	</xsl:template>

	<xsl:template match="NoteType3013" mode="N">
		<NoteType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</NoteType>
	</xsl:template>

	<xsl:template match="ParatextID3014" mode="P">
		<ParatextID>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ParatextID>
	</xsl:template>

	<xsl:template match="FileFormat3015" mode="F">
		<FileFormat>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FileFormat>
	</xsl:template>

	<xsl:template match="Files3015" mode="F">
		<Files>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Files>
	</xsl:template>

	<xsl:template match="BeginMarker3016" mode="B">
		<BeginMarker>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BeginMarker>
	</xsl:template>

	<xsl:template match="EndMarker3016" mode="E">
		<EndMarker>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</EndMarker>
	</xsl:template>

	<xsl:template match="Excluded3016" mode="E">
		<Excluded>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Excluded>
	</xsl:template>

	<xsl:template match="Target3016" mode="T">
		<Target>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Target>
	</xsl:template>

	<xsl:template match="Domain3016" mode="D">
		<Domain>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Domain>
	</xsl:template>

	<xsl:template match="ICULocale3016" mode="I">
		<ICULocale>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ICULocale>
	</xsl:template>

	<xsl:template match="Style3016" mode="S">
		<Style>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Style>
	</xsl:template>

	<xsl:template match="NoteType3016" mode="N">
		<NoteType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</NoteType>
	</xsl:template>

	<xsl:template match="Notes3017" mode="N">
		<Notes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Notes>
	</xsl:template>

	<xsl:template match="ChkHistRecs3017" mode="C">
		<ChkHistRecs>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ChkHistRecs>
	</xsl:template>

	<xsl:template match="ResolutionStatus3018" mode="R">
		<ResolutionStatus>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ResolutionStatus>
	</xsl:template>

	<xsl:template match="Categories3018" mode="C">
		<Categories>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Categories>
	</xsl:template>

	<xsl:template match="Quote3018" mode="Q">
		<Quote>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Quote>
	</xsl:template>

	<xsl:template match="Discussion3018" mode="D">
		<Discussion>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Discussion>
	</xsl:template>

	<xsl:template match="Recommendation3018" mode="R">
		<Recommendation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Recommendation>
	</xsl:template>

	<xsl:template match="Resolution3018" mode="R">
		<Resolution>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Resolution>
	</xsl:template>

	<xsl:template match="Responses3018" mode="R">
		<Responses>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Responses>
	</xsl:template>

	<xsl:template match="DateResolved3018" mode="D">
		<DateResolved>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateResolved>
	</xsl:template>

	<xsl:template match="CheckId3019" mode="C">
		<CheckId>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CheckId>
	</xsl:template>

	<xsl:template match="RunDate3019" mode="R">
		<RunDate>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RunDate>
	</xsl:template>

	<xsl:template match="Result3019" mode="R">
		<Result>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Result>
	</xsl:template>

	<xsl:template match="Records4001" mode="R">
		<Records>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Records>
	</xsl:template>

	<xsl:template match="Reminders4001" mode="R">
		<Reminders>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Reminders>
	</xsl:template>

	<xsl:template match="EventTypes4001" mode="E">
		<EventTypes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</EventTypes>
	</xsl:template>

	<xsl:template match="CrossReferences4001" mode="C">
		<CrossReferences>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CrossReferences>
	</xsl:template>

	<xsl:template match="Title4004" mode="T">
		<Title>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Title>
	</xsl:template>

	<xsl:template match="VersionHistory4004" mode="V">
		<VersionHistory>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</VersionHistory>
	</xsl:template>

	<xsl:template match="Reminders4004" mode="R">
		<Reminders>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Reminders>
	</xsl:template>

	<xsl:template match="Researchers4004" mode="R">
		<Researchers>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Researchers>
	</xsl:template>

	<xsl:template match="Confidence4004" mode="C">
		<Confidence>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Confidence>
	</xsl:template>

	<xsl:template match="Restrictions4004" mode="R">
		<Restrictions>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Restrictions>
	</xsl:template>

	<xsl:template match="AnthroCodes4004" mode="A">
		<AnthroCodes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AnthroCodes>
	</xsl:template>

	<xsl:template match="PhraseTags4004" mode="P">
		<PhraseTags>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PhraseTags>
	</xsl:template>

	<xsl:template match="SubRecords4004" mode="S">
		<SubRecords>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SubRecords>
	</xsl:template>

	<xsl:template match="DateCreated4004" mode="D">
		<DateCreated>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateCreated>
	</xsl:template>

	<xsl:template match="DateModified4004" mode="D">
		<DateModified>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateModified>
	</xsl:template>

	<xsl:template match="CrossReferences4004" mode="C">
		<CrossReferences>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CrossReferences>
	</xsl:template>

	<xsl:template match="ExternalMaterials4004" mode="E">
		<ExternalMaterials>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ExternalMaterials>
	</xsl:template>

	<xsl:template match="FurtherQuestions4004" mode="F">
		<FurtherQuestions>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FurtherQuestions>
	</xsl:template>

	<xsl:template match="SeeAlso4004" mode="S">
		<SeeAlso>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SeeAlso>
	</xsl:template>

	<xsl:template match="Hypothesis4005" mode="H">
		<Hypothesis>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Hypothesis>
	</xsl:template>

	<xsl:template match="ResearchPlan4005" mode="R">
		<ResearchPlan>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ResearchPlan>
	</xsl:template>

	<xsl:template match="Discussion4005" mode="D">
		<Discussion>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Discussion>
	</xsl:template>

	<xsl:template match="Conclusions4005" mode="C">
		<Conclusions>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Conclusions>
	</xsl:template>

	<xsl:template match="SupportingEvidence4005" mode="S">
		<SupportingEvidence>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SupportingEvidence>
	</xsl:template>

	<xsl:template match="CounterEvidence4005" mode="C">
		<CounterEvidence>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CounterEvidence>
	</xsl:template>

	<xsl:template match="SupersededBy4005" mode="S">
		<SupersededBy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SupersededBy>
	</xsl:template>

	<xsl:template match="Status4005" mode="S">
		<Status>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Status>
	</xsl:template>

	<xsl:template match="Description4006" mode="D">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Participants4006" mode="P">
		<Participants>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Participants>
	</xsl:template>

	<xsl:template match="Locations4006" mode="L">
		<Locations>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Locations>
	</xsl:template>

	<xsl:template match="Type4006" mode="T">
		<Type>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Type>
	</xsl:template>

	<xsl:template match="Weather4006" mode="W">
		<Weather>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Weather>
	</xsl:template>

	<xsl:template match="Sources4006" mode="S">
		<Sources>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Sources>
	</xsl:template>

	<xsl:template match="DateOfEvent4006" mode="D">
		<DateOfEvent>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateOfEvent>
	</xsl:template>

	<xsl:template match="TimeOfEvent4006" mode="T">
		<TimeOfEvent>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</TimeOfEvent>
	</xsl:template>

	<xsl:template match="PersonalNotes4006" mode="P">
		<PersonalNotes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PersonalNotes>
	</xsl:template>

	<xsl:template match="Description4007" mode="D">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Date4007" mode="D">
		<Date>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Date>
	</xsl:template>

	<xsl:template match="Participants4010" mode="P">
		<Participants>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Participants>
	</xsl:template>

	<xsl:template match="Role4010" mode="R">
		<Role>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Role>
	</xsl:template>

	<xsl:template match="MsFeatures5001" mode="M">
		<MsFeatures>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MsFeatures>
	</xsl:template>

	<xsl:template match="PartOfSpeech5001" mode="P">
		<PartOfSpeech>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PartOfSpeech>
	</xsl:template>

	<xsl:template match="InflectionClass5001" mode="I">
		<InflectionClass>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</InflectionClass>
	</xsl:template>

	<xsl:template match="Stratum5001" mode="S">
		<Stratum>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Stratum>
	</xsl:template>

	<xsl:template match="ProdRestrict5001" mode="P">
		<ProdRestrict>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ProdRestrict>
	</xsl:template>

	<xsl:template match="FromPartsOfSpeech5001" mode="F">
		<FromPartsOfSpeech>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FromPartsOfSpeech>
	</xsl:template>

	<xsl:template match="HomographNumber5002" mode="H">
		<HomographNumber>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</HomographNumber>
	</xsl:template>

	<xsl:template match="CitationForm5002" mode="C">
		<CitationForm>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CitationForm>
	</xsl:template>

	<xsl:template match="DateCreated5002" mode="D">
		<DateCreated>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateCreated>
	</xsl:template>

	<xsl:template match="DateModified5002" mode="D">
		<DateModified>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateModified>
	</xsl:template>

	<xsl:template match="MorphoSyntaxAnalyses5002" mode="M">
		<MorphoSyntaxAnalyses>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MorphoSyntaxAnalyses>
	</xsl:template>

	<xsl:template match="Senses5002" mode="S">
		<Senses>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Senses>
	</xsl:template>

	<xsl:template match="Bibliography5002" mode="B">
		<Bibliography>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Bibliography>
	</xsl:template>

	<xsl:template match="Etymology5002" mode="E">
		<Etymology>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Etymology>
	</xsl:template>

	<xsl:template match="Restrictions5002" mode="R">
		<Restrictions>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Restrictions>
	</xsl:template>

	<xsl:template match="SummaryDefinition5002" mode="S">
		<SummaryDefinition>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SummaryDefinition>
	</xsl:template>

	<xsl:template match="LiteralMeaning5002" mode="L">
		<LiteralMeaning>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LiteralMeaning>
	</xsl:template>

	<xsl:template match="MainEntriesOrSenses5002" mode="M">
		<MainEntriesOrSenses>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MainEntriesOrSenses>
	</xsl:template>

	<xsl:template match="Comment5002" mode="C">
		<Comment>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Comment>
	</xsl:template>

	<xsl:template match="DoNotUseForParsing5002" mode="D">
		<DoNotUseForParsing>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DoNotUseForParsing>
	</xsl:template>

	<xsl:template match="ExcludeAsHeadword5002" mode="E">
		<ExcludeAsHeadword>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ExcludeAsHeadword>
	</xsl:template>

	<xsl:template match="LexemeForm5002" mode="L">
		<LexemeForm>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LexemeForm>
	</xsl:template>

	<xsl:template match="AlternateForms5002" mode="A">
		<AlternateForms>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AlternateForms>
	</xsl:template>

	<xsl:template match="Pronunciations5002" mode="P">
		<Pronunciations>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Pronunciations>
	</xsl:template>

	<xsl:template match="ImportResidue5002" mode="I">
		<ImportResidue>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ImportResidue>
	</xsl:template>

	<xsl:template match="LiftResidue5002" mode="L">
		<LiftResidue>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LiftResidue>
	</xsl:template>

	<xsl:template match="EntryRefs5002" mode="E">
		<EntryRefs>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</EntryRefs>
	</xsl:template>

	<xsl:template match="Example5004" mode="E">
		<Example>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Example>
	</xsl:template>

	<xsl:template match="Reference5004" mode="R">
		<Reference>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Reference>
	</xsl:template>

	<xsl:template match="Translations5004" mode="T">
		<Translations>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Translations>
	</xsl:template>

	<xsl:template match="LiftResidue5004" mode="L">
		<LiftResidue>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LiftResidue>
	</xsl:template>

	<xsl:template match="Entries5005" mode="E">
		<Entries>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Entries>
	</xsl:template>

	<xsl:template match="Appendixes5005" mode="A">
		<Appendixes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Appendixes>
	</xsl:template>

	<xsl:template match="SenseTypes5005" mode="S">
		<SenseTypes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SenseTypes>
	</xsl:template>

	<xsl:template match="UsageTypes5005" mode="U">
		<UsageTypes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</UsageTypes>
	</xsl:template>

	<xsl:template match="DomainTypes5005" mode="D">
		<DomainTypes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DomainTypes>
	</xsl:template>

	<xsl:template match="MorphTypes5005" mode="M">
		<MorphTypes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MorphTypes>
	</xsl:template>

	<xsl:template match="LexicalFormIndex5005" mode="L">
		<LexicalFormIndex>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LexicalFormIndex>
	</xsl:template>

	<xsl:template match="AllomorphIndex5005" mode="A">
		<AllomorphIndex>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AllomorphIndex>
	</xsl:template>

	<xsl:template match="Introduction5005" mode="I">
		<Introduction>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Introduction>
	</xsl:template>

	<xsl:template match="IsHeadwordCitationForm5005" mode="I">
		<IsHeadwordCitationForm>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IsHeadwordCitationForm>
	</xsl:template>

	<xsl:template match="IsBodyInSeparateSubentry5005" mode="I">
		<IsBodyInSeparateSubentry>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IsBodyInSeparateSubentry>
	</xsl:template>

	<xsl:template match="Status5005" mode="S">
		<Status>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Status>
	</xsl:template>

	<xsl:template match="Styles5005" mode="S">
		<Styles>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Styles>
	</xsl:template>

	<xsl:template match="ReversalIndexes5005" mode="R">
		<ReversalIndexes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ReversalIndexes>
	</xsl:template>

	<xsl:template match="References5005" mode="R">
		<References>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</References>
	</xsl:template>

	<xsl:template match="Resources5005" mode="R">
		<Resources>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Resources>
	</xsl:template>

	<xsl:template match="VariantEntryTypes5005" mode="V">
		<VariantEntryTypes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</VariantEntryTypes>
	</xsl:template>

	<xsl:template match="ComplexEntryTypes5005" mode="C">
		<ComplexEntryTypes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ComplexEntryTypes>
	</xsl:template>

	<xsl:template match="Form5014" mode="F">
		<Form>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Form>
	</xsl:template>

	<xsl:template match="Location5014" mode="L">
		<Location>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Location>
	</xsl:template>

	<xsl:template match="MediaFiles5014" mode="M">
		<MediaFiles>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MediaFiles>
	</xsl:template>

	<xsl:template match="CVPattern5014" mode="C">
		<CVPattern>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CVPattern>
	</xsl:template>

	<xsl:template match="Tone5014" mode="T">
		<Tone>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Tone>
	</xsl:template>

	<xsl:template match="LiftResidue5014" mode="L">
		<LiftResidue>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LiftResidue>
	</xsl:template>

	<xsl:template match="MorphoSyntaxAnalysis5016" mode="M">
		<MorphoSyntaxAnalysis>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MorphoSyntaxAnalysis>
	</xsl:template>

	<xsl:template match="AnthroCodes5016" mode="A">
		<AnthroCodes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AnthroCodes>
	</xsl:template>

	<xsl:template match="Senses5016" mode="S">
		<Senses>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Senses>
	</xsl:template>

	<xsl:template match="Appendixes5016" mode="A">
		<Appendixes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Appendixes>
	</xsl:template>

	<xsl:template match="Definition5016" mode="D">
		<Definition>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Definition>
	</xsl:template>

	<xsl:template match="DomainTypes5016" mode="D">
		<DomainTypes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DomainTypes>
	</xsl:template>

	<xsl:template match="Examples5016" mode="E">
		<Examples>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Examples>
	</xsl:template>

	<xsl:template match="Gloss5016" mode="G">
		<Gloss>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Gloss>
	</xsl:template>

	<xsl:template match="ReversalEntries5016" mode="R">
		<ReversalEntries>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ReversalEntries>
	</xsl:template>

	<xsl:template match="ScientificName5016" mode="S">
		<ScientificName>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ScientificName>
	</xsl:template>

	<xsl:template match="SenseType5016" mode="S">
		<SenseType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SenseType>
	</xsl:template>

	<xsl:template match="ThesaurusItems5016" mode="T">
		<ThesaurusItems>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ThesaurusItems>
	</xsl:template>

	<xsl:template match="UsageTypes5016" mode="U">
		<UsageTypes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</UsageTypes>
	</xsl:template>

	<xsl:template match="AnthroNote5016" mode="A">
		<AnthroNote>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AnthroNote>
	</xsl:template>

	<xsl:template match="Bibliography5016" mode="B">
		<Bibliography>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Bibliography>
	</xsl:template>

	<xsl:template match="DiscourseNote5016" mode="D">
		<DiscourseNote>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DiscourseNote>
	</xsl:template>

	<xsl:template match="EncyclopedicInfo5016" mode="E">
		<EncyclopedicInfo>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</EncyclopedicInfo>
	</xsl:template>

	<xsl:template match="GeneralNote5016" mode="G">
		<GeneralNote>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</GeneralNote>
	</xsl:template>

	<xsl:template match="GrammarNote5016" mode="G">
		<GrammarNote>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</GrammarNote>
	</xsl:template>

	<xsl:template match="PhonologyNote5016" mode="P">
		<PhonologyNote>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PhonologyNote>
	</xsl:template>

	<xsl:template match="Restrictions5016" mode="R">
		<Restrictions>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Restrictions>
	</xsl:template>

	<xsl:template match="SemanticsNote5016" mode="S">
		<SemanticsNote>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SemanticsNote>
	</xsl:template>

	<xsl:template match="SocioLinguisticsNote5016" mode="S">
		<SocioLinguisticsNote>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SocioLinguisticsNote>
	</xsl:template>

	<xsl:template match="Source5016" mode="S">
		<Source>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Source>
	</xsl:template>

	<xsl:template match="Status5016" mode="S">
		<Status>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Status>
	</xsl:template>

	<xsl:template match="SemanticDomains5016" mode="S">
		<SemanticDomains>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SemanticDomains>
	</xsl:template>

	<xsl:template match="Pictures5016" mode="P">
		<Pictures>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Pictures>
	</xsl:template>

	<xsl:template match="ImportResidue5016" mode="I">
		<ImportResidue>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ImportResidue>
	</xsl:template>

	<xsl:template match="LiftResidue5016" mode="L">
		<LiftResidue>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LiftResidue>
	</xsl:template>

	<xsl:template match="Adjacency5026" mode="A">
		<Adjacency>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Adjacency>
	</xsl:template>

	<xsl:template match="MsEnvFeatures5027" mode="M">
		<MsEnvFeatures>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MsEnvFeatures>
	</xsl:template>

	<xsl:template match="PhoneEnv5027" mode="P">
		<PhoneEnv>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PhoneEnv>
	</xsl:template>

	<xsl:template match="MsEnvPartOfSpeech5027" mode="M">
		<MsEnvPartOfSpeech>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MsEnvPartOfSpeech>
	</xsl:template>

	<xsl:template match="Position5027" mode="P">
		<Position>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Position>
	</xsl:template>

	<xsl:template match="InflectionClasses5028" mode="I">
		<InflectionClasses>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</InflectionClasses>
	</xsl:template>

	<xsl:template match="Input5029" mode="I">
		<Input>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Input>
	</xsl:template>

	<xsl:template match="Output5029" mode="O">
		<Output>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Output>
	</xsl:template>

	<xsl:template match="Name5030" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Description5030" mode="D">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Stratum5030" mode="S">
		<Stratum>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Stratum>
	</xsl:template>

	<xsl:template match="ToProdRestrict5030" mode="T">
		<ToProdRestrict>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ToProdRestrict>
	</xsl:template>

	<xsl:template match="FromMsFeatures5031" mode="F">
		<FromMsFeatures>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FromMsFeatures>
	</xsl:template>

	<xsl:template match="ToMsFeatures5031" mode="T">
		<ToMsFeatures>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ToMsFeatures>
	</xsl:template>

	<xsl:template match="FromPartOfSpeech5031" mode="F">
		<FromPartOfSpeech>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FromPartOfSpeech>
	</xsl:template>

	<xsl:template match="ToPartOfSpeech5031" mode="T">
		<ToPartOfSpeech>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ToPartOfSpeech>
	</xsl:template>

	<xsl:template match="FromInflectionClass5031" mode="F">
		<FromInflectionClass>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FromInflectionClass>
	</xsl:template>

	<xsl:template match="ToInflectionClass5031" mode="T">
		<ToInflectionClass>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ToInflectionClass>
	</xsl:template>

	<xsl:template match="AffixCategory5031" mode="A">
		<AffixCategory>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AffixCategory>
	</xsl:template>

	<xsl:template match="FromStemName5031" mode="F">
		<FromStemName>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FromStemName>
	</xsl:template>

	<xsl:template match="Stratum5031" mode="S">
		<Stratum>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Stratum>
	</xsl:template>

	<xsl:template match="FromProdRestrict5031" mode="F">
		<FromProdRestrict>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FromProdRestrict>
	</xsl:template>

	<xsl:template match="ToProdRestrict5031" mode="T">
		<ToProdRestrict>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ToProdRestrict>
	</xsl:template>

	<xsl:template match="PartOfSpeech5032" mode="P">
		<PartOfSpeech>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PartOfSpeech>
	</xsl:template>

	<xsl:template match="MsFeatures5032" mode="M">
		<MsFeatures>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MsFeatures>
	</xsl:template>

	<xsl:template match="InflFeats5032" mode="I">
		<InflFeats>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</InflFeats>
	</xsl:template>

	<xsl:template match="InflectionClass5032" mode="I">
		<InflectionClass>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</InflectionClass>
	</xsl:template>

	<xsl:template match="ProdRestrict5032" mode="P">
		<ProdRestrict>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ProdRestrict>
	</xsl:template>

	<xsl:template match="HeadLast5033" mode="H">
		<HeadLast>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</HeadLast>
	</xsl:template>

	<xsl:template match="OverridingMsa5033" mode="O">
		<OverridingMsa>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</OverridingMsa>
	</xsl:template>

	<xsl:template match="ToMsa5034" mode="T">
		<ToMsa>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ToMsa>
	</xsl:template>

	<xsl:template match="Form5035" mode="F">
		<Form>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Form>
	</xsl:template>

	<xsl:template match="MorphType5035" mode="M">
		<MorphType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MorphType>
	</xsl:template>

	<xsl:template match="IsAbstract5035" mode="I">
		<IsAbstract>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IsAbstract>
	</xsl:template>

	<xsl:template match="LiftResidue5035" mode="L">
		<LiftResidue>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LiftResidue>
	</xsl:template>

	<xsl:template match="Name5036" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Description5036" mode="D">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Optional5036" mode="O">
		<Optional>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Optional>
	</xsl:template>

	<xsl:template match="Name5037" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Description5037" mode="D">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Slots5037" mode="S">
		<Slots>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Slots>
	</xsl:template>

	<xsl:template match="Stratum5037" mode="S">
		<Stratum>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Stratum>
	</xsl:template>

	<xsl:template match="Region5037" mode="R">
		<Region>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Region>
	</xsl:template>

	<xsl:template match="PrefixSlots5037" mode="P">
		<PrefixSlots>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PrefixSlots>
	</xsl:template>

	<xsl:template match="SuffixSlots5037" mode="S">
		<SuffixSlots>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SuffixSlots>
	</xsl:template>

	<xsl:template match="Final5037" mode="F">
		<Final>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Final>
	</xsl:template>

	<xsl:template match="InflFeats5038" mode="I">
		<InflFeats>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</InflFeats>
	</xsl:template>

	<xsl:template match="AffixCategory5038" mode="A">
		<AffixCategory>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AffixCategory>
	</xsl:template>

	<xsl:template match="PartOfSpeech5038" mode="P">
		<PartOfSpeech>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PartOfSpeech>
	</xsl:template>

	<xsl:template match="Slots5038" mode="S">
		<Slots>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Slots>
	</xsl:template>

	<xsl:template match="FromProdRestrict5038" mode="F">
		<FromProdRestrict>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FromProdRestrict>
	</xsl:template>

	<xsl:template match="Abbreviation5039" mode="A">
		<Abbreviation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Abbreviation>
	</xsl:template>

	<xsl:template match="Description5039" mode="D">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Name5039" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Subclasses5039" mode="S">
		<Subclasses>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Subclasses>
	</xsl:template>

	<xsl:template match="RulesOfReferral5039" mode="R">
		<RulesOfReferral>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RulesOfReferral>
	</xsl:template>

	<xsl:template match="StemNames5039" mode="S">
		<StemNames>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</StemNames>
	</xsl:template>

	<xsl:template match="ReferenceForms5039" mode="R">
		<ReferenceForms>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ReferenceForms>
	</xsl:template>

	<xsl:template match="Strata5040" mode="S">
		<Strata>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Strata>
	</xsl:template>

	<xsl:template match="CompoundRules5040" mode="C">
		<CompoundRules>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CompoundRules>
	</xsl:template>

	<xsl:template match="AdhocCoProhibitions5040" mode="A">
		<AdhocCoProhibitions>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AdhocCoProhibitions>
	</xsl:template>

	<xsl:template match="AnalyzingAgents5040" mode="A">
		<AnalyzingAgents>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AnalyzingAgents>
	</xsl:template>

	<xsl:template match="TestSets5040" mode="T">
		<TestSets>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</TestSets>
	</xsl:template>

	<xsl:template match="GlossSystem5040" mode="G">
		<GlossSystem>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</GlossSystem>
	</xsl:template>

	<xsl:template match="ParserParameters5040" mode="P">
		<ParserParameters>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ParserParameters>
	</xsl:template>

	<xsl:template match="ProdRestrict5040" mode="P">
		<ProdRestrict>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ProdRestrict>
	</xsl:template>

	<xsl:template match="Components5041" mode="C">
		<Components>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Components>
	</xsl:template>

	<xsl:template match="GlossString5041" mode="G">
		<GlossString>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</GlossString>
	</xsl:template>

	<xsl:template match="GlossBundle5041" mode="G">
		<GlossBundle>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</GlossBundle>
	</xsl:template>

	<xsl:template match="LiftResidue5041" mode="L">
		<LiftResidue>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LiftResidue>
	</xsl:template>

	<xsl:template match="Postfix5042" mode="P">
		<Postfix>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Postfix>
	</xsl:template>

	<xsl:template match="Prefix5042" mode="P">
		<Prefix>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Prefix>
	</xsl:template>

	<xsl:template match="SecondaryOrder5042" mode="S">
		<SecondaryOrder>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SecondaryOrder>
	</xsl:template>

	<xsl:template match="Name5044" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Description5044" mode="D">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Input5044" mode="I">
		<Input>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Input>
	</xsl:template>

	<xsl:template match="Output5044" mode="O">
		<Output>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Output>
	</xsl:template>

	<xsl:template match="PhoneEnv5045" mode="P">
		<PhoneEnv>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PhoneEnv>
	</xsl:template>

	<xsl:template match="StemName5045" mode="S">
		<StemName>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</StemName>
	</xsl:template>

	<xsl:template match="Contents5046" mode="C">
		<Contents>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Contents>
	</xsl:template>

	<xsl:template match="Abbreviation5047" mode="A">
		<Abbreviation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Abbreviation>
	</xsl:template>

	<xsl:template match="Description5047" mode="D">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Name5047" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Regions5047" mode="R">
		<Regions>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Regions>
	</xsl:template>

	<xsl:template match="DefaultAffix5047" mode="D">
		<DefaultAffix>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DefaultAffix>
	</xsl:template>

	<xsl:template match="DefaultStem5047" mode="D">
		<DefaultStem>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DefaultStem>
	</xsl:template>

	<xsl:template match="Abbreviation5048" mode="A">
		<Abbreviation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Abbreviation>
	</xsl:template>

	<xsl:template match="Description5048" mode="D">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Name5048" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Phonemes5048" mode="P">
		<Phonemes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Phonemes>
	</xsl:template>

	<xsl:template match="InherFeatVal5049" mode="I">
		<InherFeatVal>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</InherFeatVal>
	</xsl:template>

	<xsl:template match="EmptyParadigmCells5049" mode="E">
		<EmptyParadigmCells>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</EmptyParadigmCells>
	</xsl:template>

	<xsl:template match="RulesOfReferral5049" mode="R">
		<RulesOfReferral>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RulesOfReferral>
	</xsl:template>

	<xsl:template match="InflectionClasses5049" mode="I">
		<InflectionClasses>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</InflectionClasses>
	</xsl:template>

	<xsl:template match="AffixTemplates5049" mode="A">
		<AffixTemplates>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AffixTemplates>
	</xsl:template>

	<xsl:template match="AffixSlots5049" mode="A">
		<AffixSlots>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AffixSlots>
	</xsl:template>

	<xsl:template match="StemNames5049" mode="S">
		<StemNames>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</StemNames>
	</xsl:template>

	<xsl:template match="BearableFeatures5049" mode="B">
		<BearableFeatures>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BearableFeatures>
	</xsl:template>

	<xsl:template match="InflectableFeats5049" mode="I">
		<InflectableFeats>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</InflectableFeats>
	</xsl:template>

	<xsl:template match="ReferenceForms5049" mode="R">
		<ReferenceForms>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ReferenceForms>
	</xsl:template>

	<xsl:template match="DefaultFeatures5049" mode="D">
		<DefaultFeatures>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DefaultFeatures>
	</xsl:template>

	<xsl:template match="DefaultInflectionClass5049" mode="D">
		<DefaultInflectionClass>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DefaultInflectionClass>
	</xsl:template>

	<xsl:template match="CatalogSourceId5049" mode="C">
		<CatalogSourceId>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CatalogSourceId>
	</xsl:template>

	<xsl:template match="PartsOfSpeech5052" mode="P">
		<PartsOfSpeech>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PartsOfSpeech>
	</xsl:template>

	<xsl:template match="Entries5052" mode="E">
		<Entries>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Entries>
	</xsl:template>

	<xsl:template match="WritingSystem5052" mode="W">
		<WritingSystem>
			<xsl:copy-of select="@*"/>
			<Uni><xsl:value-of select="Link/@ws"/></Uni>
		</WritingSystem>
	</xsl:template>

	<xsl:template match="Subentries5053" mode="S">
		<Subentries>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Subentries>
	</xsl:template>

	<xsl:template match="PartOfSpeech5053" mode="P">
		<PartOfSpeech>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PartOfSpeech>
	</xsl:template>

	<xsl:template match="ReversalForm5053" mode="R">
		<ReversalForm>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ReversalForm>
	</xsl:template>

	<xsl:template match="Source5054" mode="S">
		<Source>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Source>
	</xsl:template>

	<xsl:template match="SoundFilePath5054" mode="S">
		<SoundFilePath>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SoundFilePath>
	</xsl:template>

	<xsl:template match="Contents5054" mode="C">
		<Contents>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Contents>
	</xsl:template>

	<xsl:template match="Genres5054" mode="G">
		<Genres>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Genres>
	</xsl:template>

	<xsl:template match="Abbreviation5054" mode="A">
		<Abbreviation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Abbreviation>
	</xsl:template>

	<xsl:template match="IsTranslated5054" mode="I">
		<IsTranslated>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IsTranslated>
	</xsl:template>

	<xsl:template match="Category5059" mode="C">
		<Category>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Category>
	</xsl:template>

	<xsl:template match="MsFeatures5059" mode="M">
		<MsFeatures>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MsFeatures>
	</xsl:template>

	<xsl:template match="Stems5059" mode="S">
		<Stems>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Stems>
	</xsl:template>

	<xsl:template match="Derivation5059" mode="D">
		<Derivation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Derivation>
	</xsl:template>

	<xsl:template match="Meanings5059" mode="M">
		<Meanings>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Meanings>
	</xsl:template>

	<xsl:template match="MorphBundles5059" mode="M">
		<MorphBundles>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MorphBundles>
	</xsl:template>

	<xsl:template match="CompoundRuleApps5059" mode="C">
		<CompoundRuleApps>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CompoundRuleApps>
	</xsl:template>

	<xsl:template match="InflTemplateApps5059" mode="I">
		<InflTemplateApps>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</InflTemplateApps>
	</xsl:template>

	<xsl:template match="Form5060" mode="F">
		<Form>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Form>
	</xsl:template>

	<xsl:template match="Form5062" mode="F">
		<Form>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Form>
	</xsl:template>

	<xsl:template match="Analyses5062" mode="A">
		<Analyses>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Analyses>
	</xsl:template>

	<xsl:template match="SpellingStatus5062" mode="S">
		<SpellingStatus>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SpellingStatus>
	</xsl:template>

	<xsl:template match="Checksum5062" mode="C">
		<Checksum>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Checksum>
	</xsl:template>

	<xsl:template match="Form5064" mode="F">
		<Form>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Form>
	</xsl:template>

	<xsl:template match="ThesaurusCentral5064" mode="T">
		<ThesaurusCentral>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ThesaurusCentral>
	</xsl:template>

	<xsl:template match="ThesaurusItems5064" mode="T">
		<ThesaurusItems>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ThesaurusItems>
	</xsl:template>

	<xsl:template match="AnthroCentral5064" mode="A">
		<AnthroCentral>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AnthroCentral>
	</xsl:template>

	<xsl:template match="AnthroCodes5064" mode="A">
		<AnthroCodes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AnthroCodes>
	</xsl:template>

	<xsl:template match="Wordforms5065" mode="W">
		<Wordforms>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Wordforms>
	</xsl:template>

	<xsl:template match="WritingSystem5065" mode="W">
		<WritingSystem>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</WritingSystem>
	</xsl:template>

	<xsl:template match="Content5068" mode="C">
		<Content>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Content>
	</xsl:template>

	<xsl:template match="Content5069" mode="C">
		<Content>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Content>
	</xsl:template>

	<xsl:template match="Content5070" mode="C">
		<Content>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Content>
	</xsl:template>

	<xsl:template match="Modification5070" mode="M">
		<Modification>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Modification>
	</xsl:template>

	<xsl:template match="OutputForm5072" mode="O">
		<OutputForm>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</OutputForm>
	</xsl:template>

	<xsl:template match="LeftForm5073" mode="L">
		<LeftForm>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LeftForm>
	</xsl:template>

	<xsl:template match="RightForm5073" mode="R">
		<RightForm>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RightForm>
	</xsl:template>

	<xsl:template match="Linker5073" mode="L">
		<Linker>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Linker>
	</xsl:template>

	<xsl:template match="AffixForm5074" mode="A">
		<AffixForm>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AffixForm>
	</xsl:template>

	<xsl:template match="AffixMsa5074" mode="A">
		<AffixMsa>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AffixMsa>
	</xsl:template>

	<xsl:template match="OutputMsa5074" mode="O">
		<OutputMsa>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</OutputMsa>
	</xsl:template>

	<xsl:template match="Slot5075" mode="S">
		<Slot>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Slot>
	</xsl:template>

	<xsl:template match="AffixForm5075" mode="A">
		<AffixForm>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AffixForm>
	</xsl:template>

	<xsl:template match="AffixMsa5075" mode="A">
		<AffixMsa>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AffixMsa>
	</xsl:template>

	<xsl:template match="Template5076" mode="T">
		<Template>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Template>
	</xsl:template>

	<xsl:template match="SlotApps5076" mode="S">
		<SlotApps>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SlotApps>
	</xsl:template>

	<xsl:template match="Rule5077" mode="R">
		<Rule>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Rule>
	</xsl:template>

	<xsl:template match="VacuousApp5077" mode="V">
		<VacuousApp>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</VacuousApp>
	</xsl:template>

	<xsl:template match="Stratum5078" mode="S">
		<Stratum>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Stratum>
	</xsl:template>

	<xsl:template match="CompoundRuleApps5078" mode="C">
		<CompoundRuleApps>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CompoundRuleApps>
	</xsl:template>

	<xsl:template match="DerivAffApp5078" mode="D">
		<DerivAffApp>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DerivAffApp>
	</xsl:template>

	<xsl:template match="TemplateApp5078" mode="T">
		<TemplateApp>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</TemplateApp>
	</xsl:template>

	<xsl:template match="PRuleApps5078" mode="P">
		<PRuleApps>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PRuleApps>
	</xsl:template>

	<xsl:template match="Name5081" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Description5081" mode="D">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Minimum5082" mode="M">
		<Minimum>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Minimum>
	</xsl:template>

	<xsl:template match="Maximum5082" mode="M">
		<Maximum>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Maximum>
	</xsl:template>

	<xsl:template match="Member5082" mode="M">
		<Member>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Member>
	</xsl:template>

	<xsl:template match="Members5083" mode="M">
		<Members>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Members>
	</xsl:template>

	<xsl:template match="FeatureStructure5085" mode="F">
		<FeatureStructure>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FeatureStructure>
	</xsl:template>

	<xsl:template match="FeatureStructure5086" mode="F">
		<FeatureStructure>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FeatureStructure>
	</xsl:template>

	<xsl:template match="PlusConstr5086" mode="P">
		<PlusConstr>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PlusConstr>
	</xsl:template>

	<xsl:template match="MinusConstr5086" mode="M">
		<MinusConstr>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MinusConstr>
	</xsl:template>

	<xsl:template match="FeatureStructure5087" mode="F">
		<FeatureStructure>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FeatureStructure>
	</xsl:template>

	<xsl:template match="Name5089" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Phonemes5089" mode="P">
		<Phonemes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Phonemes>
	</xsl:template>

	<xsl:template match="BoundaryMarkers5089" mode="B">
		<BoundaryMarkers>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BoundaryMarkers>
	</xsl:template>

	<xsl:template match="Description5089" mode="D">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Name5090" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Description5090" mode="D">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Codes5090" mode="C">
		<Codes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Codes>
	</xsl:template>

	<xsl:template match="BasicIPASymbol5092" mode="B">
		<BasicIPASymbol>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BasicIPASymbol>
	</xsl:template>

	<xsl:template match="Features5092" mode="F">
		<Features>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Features>
	</xsl:template>

	<xsl:template match="Name5093" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Description5093" mode="D">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Abbreviation5093" mode="A">
		<Abbreviation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Abbreviation>
	</xsl:template>

	<xsl:template match="Features5094" mode="F">
		<Features>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Features>
	</xsl:template>

	<xsl:template match="Segments5095" mode="S">
		<Segments>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Segments>
	</xsl:template>

	<xsl:template match="Feature5096" mode="F">
		<Feature>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Feature>
	</xsl:template>

	<xsl:template match="Name5097" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Description5097" mode="D">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="LeftContext5097" mode="L">
		<LeftContext>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LeftContext>
	</xsl:template>

	<xsl:template match="RightContext5097" mode="R">
		<RightContext>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RightContext>
	</xsl:template>

	<xsl:template match="AMPLEStringSegment5097" mode="A">
		<AMPLEStringSegment>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AMPLEStringSegment>
	</xsl:template>

	<xsl:template match="StringRepresentation5097" mode="S">
		<StringRepresentation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</StringRepresentation>
	</xsl:template>

	<xsl:template match="Representation5098" mode="R">
		<Representation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Representation>
	</xsl:template>

	<xsl:template match="PhonemeSets5099" mode="P">
		<PhonemeSets>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PhonemeSets>
	</xsl:template>

	<xsl:template match="Environments5099" mode="E">
		<Environments>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Environments>
	</xsl:template>

	<xsl:template match="NaturalClasses5099" mode="N">
		<NaturalClasses>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</NaturalClasses>
	</xsl:template>

	<xsl:template match="Contexts5099" mode="C">
		<Contexts>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Contexts>
	</xsl:template>

	<xsl:template match="FeatConstraints5099" mode="F">
		<FeatConstraints>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FeatConstraints>
	</xsl:template>

	<xsl:template match="PhonRuleFeats5099" mode="P">
		<PhonRuleFeats>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PhonRuleFeats>
	</xsl:template>

	<xsl:template match="PhonRules5099" mode="P">
		<PhonRules>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PhonRules>
	</xsl:template>

	<xsl:template match="StemForm5100" mode="S">
		<StemForm>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</StemForm>
	</xsl:template>

	<xsl:template match="StemMsa5100" mode="S">
		<StemMsa>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</StemMsa>
	</xsl:template>

	<xsl:template match="InflectionalFeats5100" mode="I">
		<InflectionalFeats>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</InflectionalFeats>
	</xsl:template>

	<xsl:template match="StratumApps5100" mode="S">
		<StratumApps>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</StratumApps>
	</xsl:template>

	<xsl:template match="Allomorphs5101" mode="A">
		<Allomorphs>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Allomorphs>
	</xsl:template>

	<xsl:template match="FirstAllomorph5101" mode="F">
		<FirstAllomorph>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FirstAllomorph>
	</xsl:template>

	<xsl:template match="RestOfAllos5101" mode="R">
		<RestOfAllos>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RestOfAllos>
	</xsl:template>

	<xsl:template match="Morphemes5102" mode="M">
		<Morphemes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Morphemes>
	</xsl:template>

	<xsl:template match="FirstMorpheme5102" mode="F">
		<FirstMorpheme>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FirstMorpheme>
	</xsl:template>

	<xsl:template match="RestOfMorphs5102" mode="R">
		<RestOfMorphs>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RestOfMorphs>
	</xsl:template>

	<xsl:template match="Content5103" mode="C">
		<Content>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Content>
	</xsl:template>

	<xsl:template match="Name5105" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Description5105" mode="D">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Cases5105" mode="C">
		<Cases>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Cases>
	</xsl:template>

	<xsl:template match="LeftMsa5106" mode="L">
		<LeftMsa>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LeftMsa>
	</xsl:template>

	<xsl:template match="RightMsa5106" mode="R">
		<RightMsa>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RightMsa>
	</xsl:template>

	<xsl:template match="Linker5106" mode="L">
		<Linker>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Linker>
	</xsl:template>

	<xsl:template match="Glosses5108" mode="G">
		<Glosses>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Glosses>
	</xsl:template>

	<xsl:template match="Name5109" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Abbreviation5109" mode="A">
		<Abbreviation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Abbreviation>
	</xsl:template>

	<xsl:template match="Type5109" mode="T">
		<Type>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Type>
	</xsl:template>

	<xsl:template match="AfterSeparator5109" mode="A">
		<AfterSeparator>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AfterSeparator>
	</xsl:template>

	<xsl:template match="ComplexNameSeparator5109" mode="C">
		<ComplexNameSeparator>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ComplexNameSeparator>
	</xsl:template>

	<xsl:template match="ComplexNameFirst5109" mode="C">
		<ComplexNameFirst>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ComplexNameFirst>
	</xsl:template>

	<xsl:template match="Status5109" mode="S">
		<Status>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Status>
	</xsl:template>

	<xsl:template match="FeatStructFrag5109" mode="F">
		<FeatStructFrag>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FeatStructFrag>
	</xsl:template>

	<xsl:template match="GlossItems5109" mode="G">
		<GlossItems>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</GlossItems>
	</xsl:template>

	<xsl:template match="Target5109" mode="T">
		<Target>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Target>
	</xsl:template>

	<xsl:template match="EticID5109" mode="E">
		<EticID>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</EticID>
	</xsl:template>

	<xsl:template match="Name5110" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Description5110" mode="D">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Members5110" mode="M">
		<Members>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Members>
	</xsl:template>

	<xsl:template match="Form5112" mode="F">
		<Form>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Form>
	</xsl:template>

	<xsl:template match="Morph5112" mode="M">
		<Morph>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Morph>
	</xsl:template>

	<xsl:template match="Msa5112" mode="M">
		<Msa>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Msa>
	</xsl:template>

	<xsl:template match="Sense5112" mode="S">
		<Sense>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Sense>
	</xsl:template>

	<xsl:template match="Comment5113" mode="C">
		<Comment>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Comment>
	</xsl:template>

	<xsl:template match="Form5113" mode="F">
		<Form>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Form>
	</xsl:template>

	<xsl:template match="Gloss5113" mode="G">
		<Gloss>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Gloss>
	</xsl:template>

	<xsl:template match="Source5113" mode="S">
		<Source>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Source>
	</xsl:template>

	<xsl:template match="LiftResidue5113" mode="L">
		<LiftResidue>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LiftResidue>
	</xsl:template>

	<xsl:template match="Ref5116" mode="R">
		<Ref>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Ref>
	</xsl:template>

	<xsl:template match="KeyWord5116" mode="K">
		<KeyWord>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</KeyWord>
	</xsl:template>

	<xsl:template match="Status5116" mode="S">
		<Status>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Status>
	</xsl:template>

	<xsl:template match="Rendering5116" mode="R">
		<Rendering>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Rendering>
	</xsl:template>

	<xsl:template match="Location5116" mode="L">
		<Location>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Location>
	</xsl:template>

	<xsl:template match="PartOfSpeech5117" mode="P">
		<PartOfSpeech>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PartOfSpeech>
	</xsl:template>

	<xsl:template match="ReverseAbbr5118" mode="R">
		<ReverseAbbr>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ReverseAbbr>
	</xsl:template>

	<xsl:template match="ReverseAbbreviation5119" mode="R">
		<ReverseAbbreviation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ReverseAbbreviation>
	</xsl:template>

	<xsl:template match="MappingType5119" mode="M">
		<MappingType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MappingType>
	</xsl:template>

	<xsl:template match="Members5119" mode="M">
		<Members>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Members>
	</xsl:template>

	<xsl:template match="ReverseName5119" mode="R">
		<ReverseName>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ReverseName>
	</xsl:template>

	<xsl:template match="Comment5120" mode="C">
		<Comment>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Comment>
	</xsl:template>

	<xsl:template match="Targets5120" mode="T">
		<Targets>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Targets>
	</xsl:template>

	<xsl:template match="Name5120" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="LiftResidue5120" mode="L">
		<LiftResidue>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LiftResidue>
	</xsl:template>

	<xsl:template match="Explanation5121" mode="E">
		<Explanation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Explanation>
	</xsl:template>

	<xsl:template match="Sense5121" mode="S">
		<Sense>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Sense>
	</xsl:template>

	<xsl:template match="Template5122" mode="T">
		<Template>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Template>
	</xsl:template>

	<xsl:template match="BasedOn5123" mode="B">
		<BasedOn>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BasedOn>
	</xsl:template>

	<xsl:template match="Rows5123" mode="R">
		<Rows>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Rows>
	</xsl:template>

	<xsl:template match="ConstChartTempl5124" mode="C">
		<ConstChartTempl>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ConstChartTempl>
	</xsl:template>

	<xsl:template match="Charts5124" mode="C">
		<Charts>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Charts>
	</xsl:template>

	<xsl:template match="ChartMarkers5124" mode="C">
		<ChartMarkers>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ChartMarkers>
	</xsl:template>

	<xsl:template match="Occurrences5125" mode="O">
		<Occurrences>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Occurrences>
	</xsl:template>

	<xsl:template match="SeeAlso5125" mode="S">
		<SeeAlso>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SeeAlso>
	</xsl:template>

	<xsl:template match="Renderings5125" mode="R">
		<Renderings>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Renderings>
	</xsl:template>

	<xsl:template match="TermId5125" mode="T">
		<TermId>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</TermId>
	</xsl:template>

	<xsl:template match="SurfaceForm5126" mode="S">
		<SurfaceForm>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SurfaceForm>
	</xsl:template>

	<xsl:template match="Meaning5126" mode="M">
		<Meaning>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Meaning>
	</xsl:template>

	<xsl:template match="Explanation5126" mode="E">
		<Explanation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Explanation>
	</xsl:template>

	<xsl:template match="VariantEntryTypes5127" mode="V">
		<VariantEntryTypes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</VariantEntryTypes>
	</xsl:template>

	<xsl:template match="ComplexEntryTypes5127" mode="C">
		<ComplexEntryTypes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ComplexEntryTypes>
	</xsl:template>

	<xsl:template match="PrimaryLexemes5127" mode="P">
		<PrimaryLexemes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PrimaryLexemes>
	</xsl:template>

	<xsl:template match="ComponentLexemes5127" mode="C">
		<ComponentLexemes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ComponentLexemes>
	</xsl:template>

	<xsl:template match="HideMinorEntry5127" mode="H">
		<HideMinorEntry>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</HideMinorEntry>
	</xsl:template>

	<xsl:template match="Summary5127" mode="S">
		<Summary>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Summary>
	</xsl:template>

	<xsl:template match="LiftResidue5127" mode="L">
		<LiftResidue>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LiftResidue>
	</xsl:template>

	<xsl:template match="RefType5127" mode="R">
		<RefType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RefType>
	</xsl:template>

	<xsl:template match="Description5128" mode="D">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Name5128" mode="N">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Direction5128" mode="D">
		<Direction>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Direction>
	</xsl:template>

	<xsl:template match="InitialStratum5128" mode="I">
		<InitialStratum>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</InitialStratum>
	</xsl:template>

	<xsl:template match="FinalStratum5128" mode="F">
		<FinalStratum>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FinalStratum>
	</xsl:template>

	<xsl:template match="StrucDesc5128" mode="S">
		<StrucDesc>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</StrucDesc>
	</xsl:template>

	<xsl:template match="RightHandSides5129" mode="R">
		<RightHandSides>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RightHandSides>
	</xsl:template>

	<xsl:template match="StrucChange5130" mode="S">
		<StrucChange>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</StrucChange>
	</xsl:template>

	<xsl:template match="LeftContext5131" mode="L">
		<LeftContext>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LeftContext>
	</xsl:template>

	<xsl:template match="RightContext5131" mode="R">
		<RightContext>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RightContext>
	</xsl:template>

	<xsl:template match="StrucChange5131" mode="S">
		<StrucChange>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</StrucChange>
	</xsl:template>

	<xsl:template match="InputPOSes5131" mode="I">
		<InputPOSes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</InputPOSes>
	</xsl:template>

	<xsl:template match="ExclRuleFeats5131" mode="E">
		<ExclRuleFeats>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ExclRuleFeats>
	</xsl:template>

	<xsl:template match="ReqRuleFeats5131" mode="R">
		<ReqRuleFeats>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ReqRuleFeats>
	</xsl:template>

	<xsl:template match="Item5132" mode="I">
		<Item>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Item>
	</xsl:template>

	<xsl:template match="EthnologueCode6001" mode="E">
		<EthnologueCode>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</EthnologueCode>
	</xsl:template>

	<xsl:template match="WorldRegion6001" mode="W">
		<WorldRegion>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</WorldRegion>
	</xsl:template>

	<xsl:template match="MainCountry6001" mode="M">
		<MainCountry>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MainCountry>
	</xsl:template>

	<xsl:template match="FieldWorkLocation6001" mode="F">
		<FieldWorkLocation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FieldWorkLocation>
	</xsl:template>

	<xsl:template match="PartsOfSpeech6001" mode="P">
		<PartsOfSpeech>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PartsOfSpeech>
	</xsl:template>

	<xsl:template match="Texts6001" mode="T">
		<Texts>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Texts>
	</xsl:template>

	<xsl:template match="TranslationTags6001" mode="T">
		<TranslationTags>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</TranslationTags>
	</xsl:template>

	<xsl:template match="Thesaurus6001" mode="T">
		<Thesaurus>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Thesaurus>
	</xsl:template>

	<xsl:template match="WordformLookupLists6001" mode="W">
		<WordformLookupLists>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</WordformLookupLists>
	</xsl:template>

	<xsl:template match="AnthroList6001" mode="A">
		<AnthroList>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AnthroList>
	</xsl:template>

	<xsl:template match="LexDb6001" mode="L">
		<LexDb>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LexDb>
	</xsl:template>

	<xsl:template match="ResearchNotebook6001" mode="R">
		<ResearchNotebook>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ResearchNotebook>
	</xsl:template>

	<xsl:template match="AnalysisWss6001" mode="A">
		<AnalysisWss>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AnalysisWss>
	</xsl:template>

	<xsl:template match="CurVernWss6001" mode="C">
		<CurVernWss>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CurVernWss>
	</xsl:template>

	<xsl:template match="CurAnalysisWss6001" mode="C">
		<CurAnalysisWss>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CurAnalysisWss>
	</xsl:template>

	<xsl:template match="CurPronunWss6001" mode="C">
		<CurPronunWss>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CurPronunWss>
	</xsl:template>

	<xsl:template match="MsFeatureSystem6001" mode="M">
		<MsFeatureSystem>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MsFeatureSystem>
	</xsl:template>

	<xsl:template match="MorphologicalData6001" mode="M">
		<MorphologicalData>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MorphologicalData>
	</xsl:template>

	<xsl:template match="Styles6001" mode="S">
		<Styles>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Styles>
	</xsl:template>

	<xsl:template match="Filters6001" mode="F">
		<Filters>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Filters>
	</xsl:template>

	<xsl:template match="ConfidenceLevels6001" mode="C">
		<ConfidenceLevels>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ConfidenceLevels>
	</xsl:template>

	<xsl:template match="Restrictions6001" mode="R">
		<Restrictions>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Restrictions>
	</xsl:template>

	<xsl:template match="WeatherConditions6001" mode="W">
		<WeatherConditions>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</WeatherConditions>
	</xsl:template>

	<xsl:template match="Roles6001" mode="R">
		<Roles>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Roles>
	</xsl:template>

	<xsl:template match="Status6001" mode="S">
		<Status>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Status>
	</xsl:template>

	<xsl:template match="Locations6001" mode="L">
		<Locations>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Locations>
	</xsl:template>

	<xsl:template match="People6001" mode="P">
		<People>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</People>
	</xsl:template>

	<xsl:template match="Education6001" mode="E">
		<Education>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Education>
	</xsl:template>

	<xsl:template match="TimeOfDay6001" mode="T">
		<TimeOfDay>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</TimeOfDay>
	</xsl:template>

	<xsl:template match="AffixCategories6001" mode="A">
		<AffixCategories>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AffixCategories>
	</xsl:template>

	<xsl:template match="PhonologicalData6001" mode="P">
		<PhonologicalData>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PhonologicalData>
	</xsl:template>

	<xsl:template match="Positions6001" mode="P">
		<Positions>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Positions>
	</xsl:template>

	<xsl:template match="Overlays6001" mode="O">
		<Overlays>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Overlays>
	</xsl:template>

	<xsl:template match="AnalyzingAgents6001" mode="A">
		<AnalyzingAgents>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AnalyzingAgents>
	</xsl:template>

	<xsl:template match="TranslatedScripture6001" mode="T">
		<TranslatedScripture>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</TranslatedScripture>
	</xsl:template>

	<xsl:template match="VernWss6001" mode="V">
		<VernWss>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</VernWss>
	</xsl:template>

	<xsl:template match="ExtLinkRootDir6001" mode="E">
		<ExtLinkRootDir>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ExtLinkRootDir>
	</xsl:template>

	<xsl:template match="SortSpecs6001" mode="S">
		<SortSpecs>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SortSpecs>
	</xsl:template>

	<xsl:template match="UserAccounts6001" mode="U">
		<UserAccounts>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</UserAccounts>
	</xsl:template>

	<xsl:template match="ActivatedFeatures6001" mode="A">
		<ActivatedFeatures>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ActivatedFeatures>
	</xsl:template>

	<xsl:template match="AnnotationDefs6001" mode="A">
		<AnnotationDefs>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AnnotationDefs>
	</xsl:template>

	<xsl:template match="Pictures6001" mode="P">
		<Pictures>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Pictures>
	</xsl:template>

	<xsl:template match="SemanticDomainList6001" mode="S">
		<SemanticDomainList>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SemanticDomainList>
	</xsl:template>

	<xsl:template match="CheckLists6001" mode="C">
		<CheckLists>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CheckLists>
	</xsl:template>

	<xsl:template match="Media6001" mode="M">
		<Media>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Media>
	</xsl:template>

	<xsl:template match="GenreList6001" mode="G">
		<GenreList>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</GenreList>
	</xsl:template>

	<xsl:template match="DiscourseData6001" mode="D">
		<DiscourseData>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DiscourseData>
	</xsl:template>

	<xsl:template match="TextMarkupTags6001" mode="T">
		<TextMarkupTags>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</TextMarkupTags>
	</xsl:template>

	<xsl:template match="PhFeatureSystem6001" mode="P">
		<PhFeatureSystem>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PhFeatureSystem>
	</xsl:template>

	<!-- Copy everything else -->
	<!-- There needs to be one everything else rule for each segment.
	Note that only 23 letters of the alphabet begin matches above and below. -->

	<xsl:template match="*" mode="A">
		<xsl:copy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</xsl:copy>
	</xsl:template>

	<xsl:template match="*" mode="B">
		<xsl:copy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</xsl:copy>
	</xsl:template>

	<xsl:template match="*" mode="C">
		<xsl:copy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</xsl:copy>
	</xsl:template>

	<xsl:template match="*" mode="D">
		<xsl:copy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</xsl:copy>
	</xsl:template>

	<xsl:template match="*" mode="E">
		<xsl:copy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</xsl:copy>
	</xsl:template>

	<xsl:template match="*" mode="F">
		<xsl:copy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</xsl:copy>
	</xsl:template>

	<xsl:template match="*" mode="G">
		<xsl:copy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</xsl:copy>
	</xsl:template>

	<xsl:template match="*" mode="H">
		<xsl:copy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</xsl:copy>
	</xsl:template>

	<xsl:template match="*" mode="I">
		<xsl:copy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</xsl:copy>
	</xsl:template>

	<xsl:template match="*" mode="K">
		<xsl:copy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</xsl:copy>
	</xsl:template>

	<xsl:template match="*" mode="L">
		<xsl:copy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</xsl:copy>
	</xsl:template>

	<xsl:template match="*" mode="M">
		<xsl:copy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</xsl:copy>
	</xsl:template>

	<xsl:template match="*" mode="N">
		<xsl:copy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</xsl:copy>
	</xsl:template>

	<xsl:template match="*" mode="O">
		<xsl:copy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</xsl:copy>
	</xsl:template>

	<xsl:template match="*" mode="P">
		<xsl:copy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</xsl:copy>
	</xsl:template>

	<xsl:template match="*" mode="Q">
		<xsl:copy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</xsl:copy>
	</xsl:template>

	<xsl:template match="*" mode="R">
		<xsl:copy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</xsl:copy>
	</xsl:template>

	<xsl:template match="*" mode="S">
		<xsl:copy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</xsl:copy>
	</xsl:template>

	<xsl:template match="*" mode="T">
		<xsl:copy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</xsl:copy>
	</xsl:template>

	<xsl:template match="*" mode="U">
		<xsl:copy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</xsl:copy>
	</xsl:template>

	<xsl:template match="*" mode="V">
		<xsl:copy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</xsl:copy>
	</xsl:template>

	<xsl:template match="*" mode="W">
		<xsl:copy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</xsl:copy>
	</xsl:template>

	<xsl:template match="*" mode="Z">
		<xsl:copy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</xsl:copy>
	</xsl:template>

</xsl:transform>
