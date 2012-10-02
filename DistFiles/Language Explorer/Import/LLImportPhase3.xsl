<?xml version="1.0"?>
<xsl:transform xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0" xmlns:msxsl="urn:schemas-microsoft-com:xslt" xmlns:user="urn:my-scripts">
	<xsl:output encoding="UTF-8" indent="yes"/>

	<!-- Move wordforms and cmanalyses to the top (unowned) level -->
	<!-- also copy some internal values to become attributes of Cm*Annotation in order to -->
	<!-- facilitate changes in the next phase -->

	<xsl:template match="FwDatabase">
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

	<xsl:template match="Name1">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="DateCreated1">
		<DateCreated>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateCreated>
	</xsl:template>

	<xsl:template match="DateModified1">
		<DateModified>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateModified>
	</xsl:template>

	<xsl:template match="Description1">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Name2">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="SubFolders2">
		<SubFolders>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SubFolders>
	</xsl:template>

	<xsl:template match="Description2">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Files2">
		<Files>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Files>
	</xsl:template>

	<xsl:template match="Type4">
		<Type>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Type>
	</xsl:template>

	<xsl:template match="Name5">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="DateCreated5">
		<DateCreated>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateCreated>
	</xsl:template>

	<xsl:template match="DateModified5">
		<DateModified>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateModified>
	</xsl:template>

	<xsl:template match="Description5">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Publications5">
		<Publications>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Publications>
	</xsl:template>

	<xsl:template match="HeaderFooterSets5">
		<HeaderFooterSets>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</HeaderFooterSets>
	</xsl:template>

	<xsl:template match="Name7">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Abbreviation7">
		<Abbreviation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Abbreviation>
	</xsl:template>

	<xsl:template match="Description7">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="SubPossibilities7">
		<SubPossibilities>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SubPossibilities>
	</xsl:template>

	<xsl:template match="SortSpec7">
		<SortSpec>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SortSpec>
	</xsl:template>

	<xsl:template match="Restrictions7">
		<Restrictions>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Restrictions>
	</xsl:template>

	<xsl:template match="Confidence7">
		<Confidence>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Confidence>
	</xsl:template>

	<xsl:template match="Status7">
		<Status>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Status>
	</xsl:template>

	<xsl:template match="DateCreated7">
		<DateCreated>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateCreated>
	</xsl:template>

	<xsl:template match="DateModified7">
		<DateModified>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateModified>
	</xsl:template>

	<xsl:template match="Discussion7">
		<Discussion>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Discussion>
	</xsl:template>

	<xsl:template match="Researchers7">
		<Researchers>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Researchers>
	</xsl:template>

	<xsl:template match="HelpId7">
		<HelpId>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</HelpId>
	</xsl:template>

	<xsl:template match="ForeColor7">
		<ForeColor>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ForeColor>
	</xsl:template>

	<xsl:template match="BackColor7">
		<BackColor>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BackColor>
	</xsl:template>

	<xsl:template match="UnderColor7">
		<UnderColor>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</UnderColor>
	</xsl:template>

	<xsl:template match="UnderStyle7">
		<UnderStyle>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</UnderStyle>
	</xsl:template>

	<xsl:template match="Hidden7">
		<Hidden>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Hidden>
	</xsl:template>

	<xsl:template match="IsProtected7">
		<IsProtected>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IsProtected>
	</xsl:template>

	<xsl:template match="Depth8">
		<Depth>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Depth>
	</xsl:template>

	<xsl:template match="PreventChoiceAboveLevel8">
		<PreventChoiceAboveLevel>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PreventChoiceAboveLevel>
	</xsl:template>

	<xsl:template match="IsSorted8">
		<IsSorted>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IsSorted>
	</xsl:template>

	<xsl:template match="IsClosed8">
		<IsClosed>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IsClosed>
	</xsl:template>

	<xsl:template match="PreventDuplicates8">
		<PreventDuplicates>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PreventDuplicates>
	</xsl:template>

	<xsl:template match="PreventNodeChoices8">
		<PreventNodeChoices>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PreventNodeChoices>
	</xsl:template>

	<xsl:template match="Possibilities8">
		<Possibilities>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Possibilities>
	</xsl:template>

	<xsl:template match="Abbreviation8">
		<Abbreviation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Abbreviation>
	</xsl:template>

	<xsl:template match="HelpFile8">
		<HelpFile>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</HelpFile>
	</xsl:template>

	<xsl:template match="UseExtendedFields8">
		<UseExtendedFields>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</UseExtendedFields>
	</xsl:template>

	<xsl:template match="DisplayOption8">
		<DisplayOption>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DisplayOption>
	</xsl:template>

	<xsl:template match="ItemClsid8">
		<ItemClsid>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ItemClsid>
	</xsl:template>

	<xsl:template match="IsVernacular8">
		<IsVernacular>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IsVernacular>
	</xsl:template>

	<xsl:template match="WritingSystem8">
		<WritingSystem>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</WritingSystem>
	</xsl:template>

	<xsl:template match="WsSelector8">
		<WsSelector>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</WsSelector>
	</xsl:template>

	<xsl:template match="ListVersion8">
		<ListVersion>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ListVersion>
	</xsl:template>

	<xsl:template match="Name9">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="ClassId9">
		<ClassId>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ClassId>
	</xsl:template>

	<xsl:template match="FieldId9">
		<FieldId>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FieldId>
	</xsl:template>

	<xsl:template match="FieldInfo9">
		<FieldInfo>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FieldInfo>
	</xsl:template>

	<xsl:template match="App9">
		<App>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</App>
	</xsl:template>

	<xsl:template match="Type9">
		<Type>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Type>
	</xsl:template>

	<xsl:template match="Rows9">
		<Rows>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Rows>
	</xsl:template>

	<xsl:template match="ColumnInfo9">
		<ColumnInfo>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ColumnInfo>
	</xsl:template>

	<xsl:template match="ShowPrompt9">
		<ShowPrompt>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ShowPrompt>
	</xsl:template>

	<xsl:template match="PromptText9">
		<PromptText>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PromptText>
	</xsl:template>

	<xsl:template match="Cells10">
		<Cells>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Cells>
	</xsl:template>

	<xsl:template match="Contents11">
		<Contents>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Contents>
	</xsl:template>

	<xsl:template match="Alias12">
		<Alias>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Alias>
	</xsl:template>

	<xsl:template match="Alias13">
		<Alias>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Alias>
	</xsl:template>

	<xsl:template match="Gender13">
		<Gender>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Gender>
	</xsl:template>

	<xsl:template match="DateOfBirth13">
		<DateOfBirth>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateOfBirth>
	</xsl:template>

	<xsl:template match="PlaceOfBirth13">
		<PlaceOfBirth>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PlaceOfBirth>
	</xsl:template>

	<xsl:template match="IsResearcher13">
		<IsResearcher>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IsResearcher>
	</xsl:template>

	<xsl:template match="PlacesOfResidence13">
		<PlacesOfResidence>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PlacesOfResidence>
	</xsl:template>

	<xsl:template match="Education13">
		<Education>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Education>
	</xsl:template>

	<xsl:template match="DateOfDeath13">
		<DateOfDeath>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateOfDeath>
	</xsl:template>

	<xsl:template match="Positions13">
		<Positions>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Positions>
	</xsl:template>

	<xsl:template match="Paragraphs14">
		<Paragraphs>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Paragraphs>
	</xsl:template>

	<xsl:template match="RightToLeft14">
		<RightToLeft>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RightToLeft>
	</xsl:template>

	<xsl:template match="StyleName15">
		<StyleName>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</StyleName>
	</xsl:template>

	<xsl:template match="StyleRules15">
		<StyleRules>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</StyleRules>
	</xsl:template>

	<xsl:template match="Label16">
		<Label>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Label>
	</xsl:template>

	<xsl:template match="Contents16">
		<Contents>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Contents>
	</xsl:template>

	<xsl:template match="TextObjects16">
		<TextObjects>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</TextObjects>
	</xsl:template>

	<xsl:template match="AnalyzedTextObjects16">
		<AnalyzedTextObjects>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AnalyzedTextObjects>
	</xsl:template>

	<xsl:template match="ObjRefs16">
		<ObjRefs>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ObjRefs>
	</xsl:template>

	<xsl:template match="Translations16">
		<Translations>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Translations>
	</xsl:template>

	<xsl:template match="Name17">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="BasedOn17">
		<BasedOn>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BasedOn>
	</xsl:template>

	<xsl:template match="Next17">
		<Next>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Next>
	</xsl:template>

	<xsl:template match="Type17">
		<Type>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Type>
	</xsl:template>

	<xsl:template match="Rules17">
		<Rules>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Rules>
	</xsl:template>

	<xsl:template match="IsPublishedTextStyle17">
		<IsPublishedTextStyle>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IsPublishedTextStyle>
	</xsl:template>

	<xsl:template match="IsBuiltIn17">
		<IsBuiltIn>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IsBuiltIn>
	</xsl:template>

	<xsl:template match="IsModified17">
		<IsModified>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IsModified>
	</xsl:template>

	<xsl:template match="UserLevel17">
		<UserLevel>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</UserLevel>
	</xsl:template>

	<xsl:template match="Context17">
		<Context>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Context>
	</xsl:template>

	<xsl:template match="Structure17">
		<Structure>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Structure>
	</xsl:template>

	<xsl:template match="Function17">
		<Function>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Function>
	</xsl:template>

	<xsl:template match="Usage17">
		<Usage>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Usage>
	</xsl:template>

	<xsl:template match="Name18">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Type18">
		<Type>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Type>
	</xsl:template>

	<xsl:template match="App18">
		<App>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</App>
	</xsl:template>

	<xsl:template match="Records18">
		<Records>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Records>
	</xsl:template>

	<xsl:template match="Details18">
		<Details>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Details>
	</xsl:template>

	<xsl:template match="System18">
		<System>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</System>
	</xsl:template>

	<xsl:template match="SubType18">
		<SubType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SubType>
	</xsl:template>

	<xsl:template match="Clsid19">
		<Clsid>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Clsid>
	</xsl:template>

	<xsl:template match="Level19">
		<Level>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Level>
	</xsl:template>

	<xsl:template match="Fields19">
		<Fields>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Fields>
	</xsl:template>

	<xsl:template match="Details19">
		<Details>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Details>
	</xsl:template>

	<xsl:template match="Label20">
		<Label>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Label>
	</xsl:template>

	<xsl:template match="HelpString20">
		<HelpString>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</HelpString>
	</xsl:template>

	<xsl:template match="Type20">
		<Type>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Type>
	</xsl:template>

	<xsl:template match="Flid20">
		<Flid>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Flid>
	</xsl:template>

	<xsl:template match="Visibility20">
		<Visibility>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Visibility>
	</xsl:template>

	<xsl:template match="Required20">
		<Required>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Required>
	</xsl:template>

	<xsl:template match="Style20">
		<Style>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Style>
	</xsl:template>

	<xsl:template match="SubfieldOf20">
		<SubfieldOf>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SubfieldOf>
	</xsl:template>

	<xsl:template match="Details20">
		<Details>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Details>
	</xsl:template>

	<xsl:template match="IsCustomField20">
		<IsCustomField>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IsCustomField>
	</xsl:template>

	<xsl:template match="PossList20">
		<PossList>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PossList>
	</xsl:template>

	<xsl:template match="WritingSystem20">
		<WritingSystem>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</WritingSystem>
	</xsl:template>

	<xsl:template match="WsSelector20">
		<WsSelector>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</WsSelector>
	</xsl:template>

	<xsl:template match="Name21">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="PossList21">
		<PossList>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PossList>
	</xsl:template>

	<xsl:template match="PossItems21">
		<PossItems>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PossItems>
	</xsl:template>

	<xsl:template match="Name23">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="StateInformation23">
		<StateInformation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</StateInformation>
	</xsl:template>

	<xsl:template match="Human23">
		<Human>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Human>
	</xsl:template>

	<xsl:template match="Notes23">
		<Notes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Notes>
	</xsl:template>

	<xsl:template match="Version23">
		<Version>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Version>
	</xsl:template>

	<xsl:template match="Evaluations23">
		<Evaluations>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Evaluations>
	</xsl:template>

	<xsl:template match="Name24">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Locale24">
		<Locale>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Locale>
	</xsl:template>

	<xsl:template match="Abbr24">
		<Abbr>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Abbr>
	</xsl:template>

	<xsl:template match="DefaultMonospace24">
		<DefaultMonospace>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DefaultMonospace>
	</xsl:template>

	<xsl:template match="DefaultSansSerif24">
		<DefaultSansSerif>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DefaultSansSerif>
	</xsl:template>

	<xsl:template match="DefaultSerif24">
		<DefaultSerif>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DefaultSerif>
	</xsl:template>

	<xsl:template match="FontVariation24">
		<FontVariation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FontVariation>
	</xsl:template>

	<xsl:template match="KeyboardType24">
		<KeyboardType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</KeyboardType>
	</xsl:template>

	<xsl:template match="RightToLeft24">
		<RightToLeft>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RightToLeft>
	</xsl:template>

	<xsl:template match="Collations24">
		<Collations>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Collations>
	</xsl:template>

	<xsl:template match="Description24">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="ICULocale24">
		<ICULocale>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ICULocale>
	</xsl:template>

	<xsl:template match="KeymanKeyboard24">
		<KeymanKeyboard>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</KeymanKeyboard>
	</xsl:template>

	<xsl:template match="LegacyMapping24">
		<LegacyMapping>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LegacyMapping>
	</xsl:template>

	<xsl:template match="SansFontVariation24">
		<SansFontVariation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SansFontVariation>
	</xsl:template>

	<xsl:template match="LastModified24">
		<LastModified>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LastModified>
	</xsl:template>

	<xsl:template match="DefaultBodyFont24">
		<DefaultBodyFont>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DefaultBodyFont>
	</xsl:template>

	<xsl:template match="BodyFontFeatures24">
		<BodyFontFeatures>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BodyFontFeatures>
	</xsl:template>

	<xsl:template match="ValidChars24">
		<ValidChars>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ValidChars>
	</xsl:template>

	<xsl:template match="SpellCheckDictionary24">
		<SpellCheckDictionary>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SpellCheckDictionary>
	</xsl:template>

	<xsl:template match="MatchedPairs24">
		<MatchedPairs>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MatchedPairs>
	</xsl:template>

	<xsl:template match="PunctuationPatterns24">
		<PunctuationPatterns>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PunctuationPatterns>
	</xsl:template>

	<xsl:template match="CapitalizationInfo24">
		<CapitalizationInfo>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CapitalizationInfo>
	</xsl:template>

	<xsl:template match="QuotationMarks24">
		<QuotationMarks>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</QuotationMarks>
	</xsl:template>

	<xsl:template match="Comment28">
		<Comment>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Comment>
	</xsl:template>

	<xsl:template match="Translation29">
		<Translation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Translation>
	</xsl:template>

	<xsl:template match="Type29">
		<Type>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Type>
	</xsl:template>

	<xsl:template match="Status29">
		<Status>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Status>
	</xsl:template>

	<xsl:template match="Name30">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="WinLCID30">
		<WinLCID>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</WinLCID>
	</xsl:template>

	<xsl:template match="WinCollation30">
		<WinCollation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</WinCollation>
	</xsl:template>

	<xsl:template match="IcuResourceName30">
		<IcuResourceName>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IcuResourceName>
	</xsl:template>

	<xsl:template match="IcuResourceText30">
		<IcuResourceText>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IcuResourceText>
	</xsl:template>

	<xsl:template match="ICURules30">
		<ICURules>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ICURules>
	</xsl:template>

	<xsl:template match="Name31">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="App31">
		<App>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</App>
	</xsl:template>

	<xsl:template match="ClassId31">
		<ClassId>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ClassId>
	</xsl:template>

	<xsl:template match="PrimaryField31">
		<PrimaryField>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PrimaryField>
	</xsl:template>

	<xsl:template match="PrimaryCollType31">
		<PrimaryCollType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PrimaryCollType>
	</xsl:template>

	<xsl:template match="PrimaryReverse31">
		<PrimaryReverse>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PrimaryReverse>
	</xsl:template>

	<xsl:template match="SecondaryField31">
		<SecondaryField>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SecondaryField>
	</xsl:template>

	<xsl:template match="SecondaryCollType31">
		<SecondaryCollType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SecondaryCollType>
	</xsl:template>

	<xsl:template match="SecondaryReverse31">
		<SecondaryReverse>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SecondaryReverse>
	</xsl:template>

	<xsl:template match="TertiaryField31">
		<TertiaryField>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</TertiaryField>
	</xsl:template>

	<xsl:template match="TertiaryCollType31">
		<TertiaryCollType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</TertiaryCollType>
	</xsl:template>

	<xsl:template match="TertiaryReverse31">
		<TertiaryReverse>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</TertiaryReverse>
	</xsl:template>

	<xsl:template match="IncludeSubentries31">
		<IncludeSubentries>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IncludeSubentries>
	</xsl:template>

	<xsl:template match="PrimaryWs31">
		<PrimaryWs>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PrimaryWs>
	</xsl:template>

	<xsl:template match="SecondaryWs31">
		<SecondaryWs>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SecondaryWs>
	</xsl:template>

	<xsl:template match="TertiaryWs31">
		<TertiaryWs>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</TertiaryWs>
	</xsl:template>

	<xsl:template match="PrimaryCollation31">
		<PrimaryCollation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PrimaryCollation>
	</xsl:template>

	<xsl:template match="SecondaryCollation31">
		<SecondaryCollation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SecondaryCollation>
	</xsl:template>

	<xsl:template match="TertiaryCollation31">
		<TertiaryCollation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</TertiaryCollation>
	</xsl:template>

	<xsl:template match="Target32">
		<Target>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Target>
	</xsl:template>

	<xsl:template match="DateCreated32">
		<DateCreated>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateCreated>
	</xsl:template>

	<xsl:template match="Accepted32">
		<Accepted>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Accepted>
	</xsl:template>

	<xsl:template match="Details32">
		<Details>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Details>
	</xsl:template>

	<xsl:template match="CompDetails34">
		<CompDetails>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CompDetails>
	</xsl:template>

	<xsl:template match="Comment34">
		<Comment>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Comment>
	</xsl:template>

	<xsl:template match="AnnotationType34">
		<AnnotationType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AnnotationType>
	</xsl:template>

	<xsl:template match="Source34">
		<Source>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Source>
	</xsl:template>

	<xsl:template match="InstanceOf34">
		<InstanceOf>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</InstanceOf>
	</xsl:template>

	<xsl:template match="Text34">
		<Text>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Text>
	</xsl:template>

	<xsl:template match="Features34">
		<Features>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Features>
	</xsl:template>

	<xsl:template match="DateCreated34">
		<DateCreated>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateCreated>
	</xsl:template>

	<xsl:template match="DateModified34">
		<DateModified>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateModified>
	</xsl:template>

	<xsl:template match="AllowsComment35">
		<AllowsComment>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AllowsComment>
	</xsl:template>

	<xsl:template match="AllowsFeatureStructure35">
		<AllowsFeatureStructure>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AllowsFeatureStructure>
	</xsl:template>

	<xsl:template match="AllowsInstanceOf35">
		<AllowsInstanceOf>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AllowsInstanceOf>
	</xsl:template>

	<xsl:template match="InstanceOfSignature35">
		<InstanceOfSignature>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</InstanceOfSignature>
	</xsl:template>

	<xsl:template match="UserCanCreate35">
		<UserCanCreate>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</UserCanCreate>
	</xsl:template>

	<xsl:template match="CanCreateOrphan35">
		<CanCreateOrphan>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CanCreateOrphan>
	</xsl:template>

	<xsl:template match="PromptUser35">
		<PromptUser>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PromptUser>
	</xsl:template>

	<xsl:template match="CopyCutPastable35">
		<CopyCutPastable>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CopyCutPastable>
	</xsl:template>

	<xsl:template match="ZeroWidth35">
		<ZeroWidth>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ZeroWidth>
	</xsl:template>

	<xsl:template match="Multi35">
		<Multi>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Multi>
	</xsl:template>

	<xsl:template match="Severity35">
		<Severity>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Severity>
	</xsl:template>

	<xsl:template match="MaxDupOccur35">
		<MaxDupOccur>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MaxDupOccur>
	</xsl:template>

	<xsl:template match="AppliesTo36">
		<AppliesTo>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AppliesTo>
	</xsl:template>

	<xsl:template match="BeginOffset37">
		<BeginOffset>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BeginOffset>
	</xsl:template>

	<xsl:template match="Flid37">
		<Flid>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Flid>
	</xsl:template>

	<xsl:template match="EndOffset37">
		<EndOffset>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</EndOffset>
	</xsl:template>

	<xsl:template match="BeginObject37">
		<BeginObject>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BeginObject>
	</xsl:template>

	<xsl:template match="EndObject37">
		<EndObject>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</EndObject>
	</xsl:template>

	<xsl:template match="OtherObjects37">
		<OtherObjects>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</OtherObjects>
	</xsl:template>

	<xsl:template match="WritingSystem37">
		<WritingSystem>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</WritingSystem>
	</xsl:template>

	<xsl:template match="WsSelector37">
		<WsSelector>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</WsSelector>
	</xsl:template>

	<xsl:template match="BeginRef37">
		<BeginRef>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BeginRef>
	</xsl:template>

	<xsl:template match="EndRef37">
		<EndRef>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</EndRef>
	</xsl:template>

	<xsl:template match="FootnoteMarker39">
		<FootnoteMarker>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FootnoteMarker>
	</xsl:template>

	<xsl:template match="DisplayFootnoteReference39">
		<DisplayFootnoteReference>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DisplayFootnoteReference>
	</xsl:template>

	<xsl:template match="DisplayFootnoteMarker39">
		<DisplayFootnoteMarker>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DisplayFootnoteMarker>
	</xsl:template>

	<xsl:template match="Sid40">
		<Sid>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Sid>
	</xsl:template>

	<xsl:template match="UserLevel40">
		<UserLevel>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</UserLevel>
	</xsl:template>

	<xsl:template match="HasMaintenance40">
		<HasMaintenance>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</HasMaintenance>
	</xsl:template>

	<xsl:template match="UserConfigAcct41">
		<UserConfigAcct>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</UserConfigAcct>
	</xsl:template>

	<xsl:template match="ApplicationId41">
		<ApplicationId>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ApplicationId>
	</xsl:template>

	<xsl:template match="FeatureId41">
		<FeatureId>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FeatureId>
	</xsl:template>

	<xsl:template match="ActivatedLevel41">
		<ActivatedLevel>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ActivatedLevel>
	</xsl:template>

	<xsl:template match="Name42">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Description42">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="PageHeight42">
		<PageHeight>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PageHeight>
	</xsl:template>

	<xsl:template match="PageWidth42">
		<PageWidth>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PageWidth>
	</xsl:template>

	<xsl:template match="IsLandscape42">
		<IsLandscape>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IsLandscape>
	</xsl:template>

	<xsl:template match="GutterMargin42">
		<GutterMargin>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</GutterMargin>
	</xsl:template>

	<xsl:template match="GutterLoc42">
		<GutterLoc>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</GutterLoc>
	</xsl:template>

	<xsl:template match="Divisions42">
		<Divisions>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Divisions>
	</xsl:template>

	<xsl:template match="FootnoteSepWidth42">
		<FootnoteSepWidth>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FootnoteSepWidth>
	</xsl:template>

	<xsl:template match="PaperHeight42">
		<PaperHeight>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PaperHeight>
	</xsl:template>

	<xsl:template match="PaperWidth42">
		<PaperWidth>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PaperWidth>
	</xsl:template>

	<xsl:template match="BindingEdge42">
		<BindingEdge>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BindingEdge>
	</xsl:template>

	<xsl:template match="SheetLayout42">
		<SheetLayout>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SheetLayout>
	</xsl:template>

	<xsl:template match="SheetsPerSig42">
		<SheetsPerSig>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SheetsPerSig>
	</xsl:template>

	<xsl:template match="BaseFontSize42">
		<BaseFontSize>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BaseFontSize>
	</xsl:template>

	<xsl:template match="BaseLineSpacing42">
		<BaseLineSpacing>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BaseLineSpacing>
	</xsl:template>

	<xsl:template match="DifferentFirstHF43">
		<DifferentFirstHF>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DifferentFirstHF>
	</xsl:template>

	<xsl:template match="DifferentEvenHF43">
		<DifferentEvenHF>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DifferentEvenHF>
	</xsl:template>

	<xsl:template match="StartAt43">
		<StartAt>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</StartAt>
	</xsl:template>

	<xsl:template match="PageLayout43">
		<PageLayout>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PageLayout>
	</xsl:template>

	<xsl:template match="HFSet43">
		<HFSet>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</HFSet>
	</xsl:template>

	<xsl:template match="NumColumns43">
		<NumColumns>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</NumColumns>
	</xsl:template>

	<xsl:template match="Name44">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Description44">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="MarginTop44">
		<MarginTop>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MarginTop>
	</xsl:template>

	<xsl:template match="MarginBottom44">
		<MarginBottom>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MarginBottom>
	</xsl:template>

	<xsl:template match="MarginInside44">
		<MarginInside>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MarginInside>
	</xsl:template>

	<xsl:template match="MarginOutside44">
		<MarginOutside>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MarginOutside>
	</xsl:template>

	<xsl:template match="PosHeader44">
		<PosHeader>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PosHeader>
	</xsl:template>

	<xsl:template match="PosFooter44">
		<PosFooter>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PosFooter>
	</xsl:template>

	<xsl:template match="IsBuiltIn44">
		<IsBuiltIn>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IsBuiltIn>
	</xsl:template>

	<xsl:template match="IsModified44">
		<IsModified>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IsModified>
	</xsl:template>

	<xsl:template match="Name45">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Description45">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="DefaultHeader45">
		<DefaultHeader>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DefaultHeader>
	</xsl:template>

	<xsl:template match="DefaultFooter45">
		<DefaultFooter>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DefaultFooter>
	</xsl:template>

	<xsl:template match="FirstHeader45">
		<FirstHeader>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FirstHeader>
	</xsl:template>

	<xsl:template match="FirstFooter45">
		<FirstFooter>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FirstFooter>
	</xsl:template>

	<xsl:template match="EvenHeader45">
		<EvenHeader>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</EvenHeader>
	</xsl:template>

	<xsl:template match="EvenFooter45">
		<EvenFooter>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</EvenFooter>
	</xsl:template>

	<xsl:template match="InsideAlignedText46">
		<InsideAlignedText>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</InsideAlignedText>
	</xsl:template>

	<xsl:template match="CenteredText46">
		<CenteredText>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CenteredText>
	</xsl:template>

	<xsl:template match="OutsideAlignedText46">
		<OutsideAlignedText>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</OutsideAlignedText>
	</xsl:template>

	<xsl:template match="Name47">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Description47">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="OriginalPath47">
		<OriginalPath>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</OriginalPath>
	</xsl:template>

	<xsl:template match="InternalPath47">
		<InternalPath>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</InternalPath>
	</xsl:template>

	<xsl:template match="Copyright47">
		<Copyright>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Copyright>
	</xsl:template>

	<xsl:template match="PictureFile48">
		<PictureFile>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PictureFile>
	</xsl:template>

	<xsl:template match="Caption48">
		<Caption>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Caption>
	</xsl:template>

	<xsl:template match="Description48">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="LayoutPos48">
		<LayoutPos>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LayoutPos>
	</xsl:template>

	<xsl:template match="ScaleFactor48">
		<ScaleFactor>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ScaleFactor>
	</xsl:template>

	<xsl:template match="LocationRangeType48">
		<LocationRangeType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LocationRangeType>
	</xsl:template>

	<xsl:template match="LocationMin48">
		<LocationMin>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LocationMin>
	</xsl:template>

	<xsl:template match="LocationMax48">
		<LocationMax>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LocationMax>
	</xsl:template>

	<xsl:template match="Types49">
		<Types>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Types>
	</xsl:template>

	<xsl:template match="Features49">
		<Features>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Features>
	</xsl:template>

	<xsl:template match="Values50">
		<Values>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Values>
	</xsl:template>

	<xsl:template match="Value51">
		<Value>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Value>
	</xsl:template>

	<xsl:template match="Value53">
		<Value>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Value>
	</xsl:template>

	<xsl:template match="Value54">
		<Value>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Value>
	</xsl:template>

	<xsl:template match="Name55">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Abbreviation55">
		<Abbreviation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Abbreviation>
	</xsl:template>

	<xsl:template match="Description55">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Default55">
		<Default>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Default>
	</xsl:template>

	<xsl:template match="GlossAbbreviation55">
		<GlossAbbreviation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</GlossAbbreviation>
	</xsl:template>

	<xsl:template match="RightGlossSep55">
		<RightGlossSep>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RightGlossSep>
	</xsl:template>

	<xsl:template match="ShowInGloss55">
		<ShowInGloss>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ShowInGloss>
	</xsl:template>

	<xsl:template match="DisplayToRightOfValues55">
		<DisplayToRightOfValues>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DisplayToRightOfValues>
	</xsl:template>

	<xsl:template match="CatalogSourceId55">
		<CatalogSourceId>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CatalogSourceId>
	</xsl:template>

	<xsl:template match="RefNumber56">
		<RefNumber>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RefNumber>
	</xsl:template>

	<xsl:template match="ValueState56">
		<ValueState>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ValueState>
	</xsl:template>

	<xsl:template match="Feature56">
		<Feature>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Feature>
	</xsl:template>

	<xsl:template match="FeatureDisjunctions57">
		<FeatureDisjunctions>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FeatureDisjunctions>
	</xsl:template>

	<xsl:template match="FeatureSpecs57">
		<FeatureSpecs>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FeatureSpecs>
	</xsl:template>

	<xsl:template match="Type57">
		<Type>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Type>
	</xsl:template>

	<xsl:template match="Contents58">
		<Contents>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Contents>
	</xsl:template>

	<xsl:template match="Name59">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Abbreviation59">
		<Abbreviation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Abbreviation>
	</xsl:template>

	<xsl:template match="Description59">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Features59">
		<Features>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Features>
	</xsl:template>

	<xsl:template match="CatalogSourceId59">
		<CatalogSourceId>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CatalogSourceId>
	</xsl:template>

	<xsl:template match="Value61">
		<Value>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Value>
	</xsl:template>

	<xsl:template match="WritingSystem62">
		<WritingSystem>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</WritingSystem>
	</xsl:template>

	<xsl:template match="WsSelector62">
		<WsSelector>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</WsSelector>
	</xsl:template>

	<xsl:template match="Value63">
		<Value>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Value>
	</xsl:template>

	<xsl:template match="Value64">
		<Value>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Value>
	</xsl:template>

	<xsl:template match="Name65">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Abbreviation65">
		<Abbreviation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Abbreviation>
	</xsl:template>

	<xsl:template match="Description65">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="GlossAbbreviation65">
		<GlossAbbreviation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</GlossAbbreviation>
	</xsl:template>

	<xsl:template match="RightGlossSep65">
		<RightGlossSep>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RightGlossSep>
	</xsl:template>

	<xsl:template match="ShowInGloss65">
		<ShowInGloss>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ShowInGloss>
	</xsl:template>

	<xsl:template match="CatalogSourceId65">
		<CatalogSourceId>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CatalogSourceId>
	</xsl:template>

	<xsl:template match="LouwNidaCodes66">
		<LouwNidaCodes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LouwNidaCodes>
	</xsl:template>

	<xsl:template match="OcmCodes66">
		<OcmCodes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</OcmCodes>
	</xsl:template>

	<xsl:template match="OcmRefs66">
		<OcmRefs>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</OcmRefs>
	</xsl:template>

	<xsl:template match="RelatedDomains66">
		<RelatedDomains>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RelatedDomains>
	</xsl:template>

	<xsl:template match="Questions66">
		<Questions>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Questions>
	</xsl:template>

	<xsl:template match="Question67">
		<Question>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Question>
	</xsl:template>

	<xsl:template match="ExampleWords67">
		<ExampleWords>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ExampleWords>
	</xsl:template>

	<xsl:template match="ExampleSentences67">
		<ExampleSentences>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ExampleSentences>
	</xsl:template>

	<xsl:template match="DateCreated68">
		<DateCreated>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateCreated>
	</xsl:template>

	<xsl:template match="DateModified68">
		<DateModified>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateModified>
	</xsl:template>

	<xsl:template match="CreatedBy68">
		<CreatedBy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CreatedBy>
	</xsl:template>

	<xsl:template match="ModifiedBy68">
		<ModifiedBy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ModifiedBy>
	</xsl:template>

	<xsl:template match="Label69">
		<Label>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Label>
	</xsl:template>

	<xsl:template match="MediaFile69">
		<MediaFile>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MediaFile>
	</xsl:template>

	<xsl:template match="Name70">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Version70">
		<Version>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Version>
	</xsl:template>

	<xsl:template match="ScriptureBooks3001">
		<ScriptureBooks>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ScriptureBooks>
	</xsl:template>

	<xsl:template match="Styles3001">
		<Styles>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Styles>
	</xsl:template>

	<xsl:template match="RefSepr3001">
		<RefSepr>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RefSepr>
	</xsl:template>

	<xsl:template match="ChapterVerseSepr3001">
		<ChapterVerseSepr>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ChapterVerseSepr>
	</xsl:template>

	<xsl:template match="VerseSepr3001">
		<VerseSepr>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</VerseSepr>
	</xsl:template>

	<xsl:template match="Bridge3001">
		<Bridge>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Bridge>
	</xsl:template>

	<xsl:template match="ImportSettings3001">
		<ImportSettings>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ImportSettings>
	</xsl:template>

	<xsl:template match="ArchivedDrafts3001">
		<ArchivedDrafts>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ArchivedDrafts>
	</xsl:template>

	<xsl:template match="FootnoteMarkerSymbol3001">
		<FootnoteMarkerSymbol>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FootnoteMarkerSymbol>
	</xsl:template>

	<xsl:template match="DisplayFootnoteReference3001">
		<DisplayFootnoteReference>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DisplayFootnoteReference>
	</xsl:template>

	<xsl:template match="RestartFootnoteSequence3001">
		<RestartFootnoteSequence>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RestartFootnoteSequence>
	</xsl:template>

	<xsl:template match="RestartFootnoteBoundary3001">
		<RestartFootnoteBoundary>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RestartFootnoteBoundary>
	</xsl:template>

	<xsl:template match="UseScriptDigits3001">
		<UseScriptDigits>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</UseScriptDigits>
	</xsl:template>

	<xsl:template match="ScriptDigitZero3001">
		<ScriptDigitZero>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ScriptDigitZero>
	</xsl:template>

	<xsl:template match="ConvertCVDigitsOnExport3001">
		<ConvertCVDigitsOnExport>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ConvertCVDigitsOnExport>
	</xsl:template>

	<xsl:template match="Versification3001">
		<Versification>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Versification>
	</xsl:template>

	<xsl:template match="VersePunct3001">
		<VersePunct>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</VersePunct>
	</xsl:template>

	<xsl:template match="ChapterLabel3001">
		<ChapterLabel>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ChapterLabel>
	</xsl:template>

	<xsl:template match="PsalmLabel3001">
		<PsalmLabel>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PsalmLabel>
	</xsl:template>

	<xsl:template match="BookAnnotations3001">
		<BookAnnotations>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BookAnnotations>
	</xsl:template>

	<xsl:template match="NoteCategories3001">
		<NoteCategories>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</NoteCategories>
	</xsl:template>

	<xsl:template match="FootnoteMarkerType3001">
		<FootnoteMarkerType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FootnoteMarkerType>
	</xsl:template>

	<xsl:template match="DisplayCrossRefReference3001">
		<DisplayCrossRefReference>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DisplayCrossRefReference>
	</xsl:template>

	<xsl:template match="CrossRefMarkerSymbol3001">
		<CrossRefMarkerSymbol>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CrossRefMarkerSymbol>
	</xsl:template>

	<xsl:template match="CrossRefMarkerType3001">
		<CrossRefMarkerType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CrossRefMarkerType>
	</xsl:template>

	<xsl:template match="CrossRefsCombinedWithFootnotes3001">
		<CrossRefsCombinedWithFootnotes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CrossRefsCombinedWithFootnotes>
	</xsl:template>

	<xsl:template match="DisplaySymbolInFootnote3001">
		<DisplaySymbolInFootnote>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DisplaySymbolInFootnote>
	</xsl:template>

	<xsl:template match="DisplaySymbolInCrossRef3001">
		<DisplaySymbolInCrossRef>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DisplaySymbolInCrossRef>
	</xsl:template>

	<xsl:template match="Resources3001">
		<Resources>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Resources>
	</xsl:template>

	<xsl:template match="Sections3002">
		<Sections>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Sections>
	</xsl:template>

	<xsl:template match="Name3002">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="BookId3002">
		<BookId>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BookId>
	</xsl:template>

	<xsl:template match="Title3002">
		<Title>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Title>
	</xsl:template>

	<xsl:template match="Abbrev3002">
		<Abbrev>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Abbrev>
	</xsl:template>

	<xsl:template match="IdText3002">
		<IdText>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IdText>
	</xsl:template>

	<xsl:template match="Footnotes3002">
		<Footnotes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Footnotes>
	</xsl:template>

	<xsl:template match="Diffs3002">
		<Diffs>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Diffs>
	</xsl:template>

	<xsl:template match="UseChapterNumHeading3002">
		<UseChapterNumHeading>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</UseChapterNumHeading>
	</xsl:template>

	<xsl:template match="CanonicalNum3002">
		<CanonicalNum>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CanonicalNum>
	</xsl:template>

	<xsl:template match="Books3003">
		<Books>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Books>
	</xsl:template>

	<xsl:template match="BookName3004">
		<BookName>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BookName>
	</xsl:template>

	<xsl:template match="BookAbbrev3004">
		<BookAbbrev>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BookAbbrev>
	</xsl:template>

	<xsl:template match="BookNameAlt3004">
		<BookNameAlt>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BookNameAlt>
	</xsl:template>

	<xsl:template match="Heading3005">
		<Heading>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Heading>
	</xsl:template>

	<xsl:template match="Content3005">
		<Content>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Content>
	</xsl:template>

	<xsl:template match="VerseRefStart3005">
		<VerseRefStart>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</VerseRefStart>
	</xsl:template>

	<xsl:template match="VerseRefEnd3005">
		<VerseRefEnd>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</VerseRefEnd>
	</xsl:template>

	<xsl:template match="VerseRefMin3005">
		<VerseRefMin>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</VerseRefMin>
	</xsl:template>

	<xsl:template match="VerseRefMax3005">
		<VerseRefMax>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</VerseRefMax>
	</xsl:template>

	<xsl:template match="ImportType3008">
		<ImportType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ImportType>
	</xsl:template>

	<xsl:template match="ImportSettings3008">
		<ImportSettings>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ImportSettings>
	</xsl:template>

	<xsl:template match="ImportProjToken3008">
		<ImportProjToken>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ImportProjToken>
	</xsl:template>

	<xsl:template match="Name3008">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Description3008">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="ScriptureSources3008">
		<ScriptureSources>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ScriptureSources>
	</xsl:template>

	<xsl:template match="BackTransSources3008">
		<BackTransSources>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BackTransSources>
	</xsl:template>

	<xsl:template match="NoteSources3008">
		<NoteSources>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</NoteSources>
	</xsl:template>

	<xsl:template match="ScriptureMappings3008">
		<ScriptureMappings>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ScriptureMappings>
	</xsl:template>

	<xsl:template match="NoteMappings3008">
		<NoteMappings>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</NoteMappings>
	</xsl:template>

	<xsl:template match="Description3010">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Books3010">
		<Books>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Books>
	</xsl:template>

	<xsl:template match="DateCreated3010">
		<DateCreated>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateCreated>
	</xsl:template>

	<xsl:template match="Type3010">
		<Type>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Type>
	</xsl:template>

	<xsl:template match="Protected3010">
		<Protected>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Protected>
	</xsl:template>

	<xsl:template match="RefStart3011">
		<RefStart>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RefStart>
	</xsl:template>

	<xsl:template match="RefEnd3011">
		<RefEnd>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RefEnd>
	</xsl:template>

	<xsl:template match="DiffType3011">
		<DiffType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DiffType>
	</xsl:template>

	<xsl:template match="RevMin3011">
		<RevMin>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RevMin>
	</xsl:template>

	<xsl:template match="RevLim3011">
		<RevLim>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RevLim>
	</xsl:template>

	<xsl:template match="RevParagraph3011">
		<RevParagraph>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RevParagraph>
	</xsl:template>

	<xsl:template match="ICULocale3013">
		<ICULocale>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ICULocale>
	</xsl:template>

	<xsl:template match="NoteType3013">
		<NoteType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</NoteType>
	</xsl:template>

	<xsl:template match="ParatextID3014">
		<ParatextID>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ParatextID>
	</xsl:template>

	<xsl:template match="FileFormat3015">
		<FileFormat>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FileFormat>
	</xsl:template>

	<xsl:template match="Files3015">
		<Files>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Files>
	</xsl:template>

	<xsl:template match="BeginMarker3016">
		<BeginMarker>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BeginMarker>
	</xsl:template>

	<xsl:template match="EndMarker3016">
		<EndMarker>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</EndMarker>
	</xsl:template>

	<xsl:template match="Excluded3016">
		<Excluded>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Excluded>
	</xsl:template>

	<xsl:template match="Target3016">
		<Target>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Target>
	</xsl:template>

	<xsl:template match="Domain3016">
		<Domain>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Domain>
	</xsl:template>

	<xsl:template match="ICULocale3016">
		<ICULocale>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ICULocale>
	</xsl:template>

	<xsl:template match="Style3016">
		<Style>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Style>
	</xsl:template>

	<xsl:template match="NoteType3016">
		<NoteType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</NoteType>
	</xsl:template>

	<xsl:template match="Notes3017">
		<Notes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Notes>
	</xsl:template>

	<xsl:template match="ChkHistRecs3017">
		<ChkHistRecs>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ChkHistRecs>
	</xsl:template>

	<xsl:template match="ResolutionStatus3018">
		<ResolutionStatus>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ResolutionStatus>
	</xsl:template>

	<xsl:template match="Categories3018">
		<Categories>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Categories>
	</xsl:template>

	<xsl:template match="Quote3018">
		<Quote>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Quote>
	</xsl:template>

	<xsl:template match="Discussion3018">
		<Discussion>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Discussion>
	</xsl:template>

	<xsl:template match="Recommendation3018">
		<Recommendation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Recommendation>
	</xsl:template>

	<xsl:template match="Resolution3018">
		<Resolution>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Resolution>
	</xsl:template>

	<xsl:template match="Responses3018">
		<Responses>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Responses>
	</xsl:template>

	<xsl:template match="DateResolved3018">
		<DateResolved>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateResolved>
	</xsl:template>

	<xsl:template match="CheckId3019">
		<CheckId>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CheckId>
	</xsl:template>

	<xsl:template match="RunDate3019">
		<RunDate>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RunDate>
	</xsl:template>

	<xsl:template match="Result3019">
		<Result>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Result>
	</xsl:template>

	<xsl:template match="Records4001">
		<Records>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Records>
	</xsl:template>

	<xsl:template match="Reminders4001">
		<Reminders>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Reminders>
	</xsl:template>

	<xsl:template match="EventTypes4001">
		<EventTypes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</EventTypes>
	</xsl:template>

	<xsl:template match="CrossReferences4001">
		<CrossReferences>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CrossReferences>
	</xsl:template>

	<xsl:template match="Title4004">
		<Title>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Title>
	</xsl:template>

	<xsl:template match="VersionHistory4004">
		<VersionHistory>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</VersionHistory>
	</xsl:template>

	<xsl:template match="Reminders4004">
		<Reminders>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Reminders>
	</xsl:template>

	<xsl:template match="Researchers4004">
		<Researchers>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Researchers>
	</xsl:template>

	<xsl:template match="Confidence4004">
		<Confidence>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Confidence>
	</xsl:template>

	<xsl:template match="Restrictions4004">
		<Restrictions>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Restrictions>
	</xsl:template>

	<xsl:template match="AnthroCodes4004">
		<AnthroCodes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AnthroCodes>
	</xsl:template>

	<xsl:template match="PhraseTags4004">
		<PhraseTags>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PhraseTags>
	</xsl:template>

	<xsl:template match="SubRecords4004">
		<SubRecords>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SubRecords>
	</xsl:template>

	<xsl:template match="DateCreated4004">
		<DateCreated>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateCreated>
	</xsl:template>

	<xsl:template match="DateModified4004">
		<DateModified>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateModified>
	</xsl:template>

	<xsl:template match="CrossReferences4004">
		<CrossReferences>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CrossReferences>
	</xsl:template>

	<xsl:template match="ExternalMaterials4004">
		<ExternalMaterials>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ExternalMaterials>
	</xsl:template>

	<xsl:template match="FurtherQuestions4004">
		<FurtherQuestions>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FurtherQuestions>
	</xsl:template>

	<xsl:template match="SeeAlso4004">
		<SeeAlso>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SeeAlso>
	</xsl:template>

	<xsl:template match="Hypothesis4005">
		<Hypothesis>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Hypothesis>
	</xsl:template>

	<xsl:template match="ResearchPlan4005">
		<ResearchPlan>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ResearchPlan>
	</xsl:template>

	<xsl:template match="Discussion4005">
		<Discussion>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Discussion>
	</xsl:template>

	<xsl:template match="Conclusions4005">
		<Conclusions>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Conclusions>
	</xsl:template>

	<xsl:template match="SupportingEvidence4005">
		<SupportingEvidence>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SupportingEvidence>
	</xsl:template>

	<xsl:template match="CounterEvidence4005">
		<CounterEvidence>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CounterEvidence>
	</xsl:template>

	<xsl:template match="SupersededBy4005">
		<SupersededBy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SupersededBy>
	</xsl:template>

	<xsl:template match="Status4005">
		<Status>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Status>
	</xsl:template>

	<xsl:template match="Description4006">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Participants4006">
		<Participants>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Participants>
	</xsl:template>

	<xsl:template match="Locations4006">
		<Locations>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Locations>
	</xsl:template>

	<xsl:template match="Type4006">
		<Type>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Type>
	</xsl:template>

	<xsl:template match="Weather4006">
		<Weather>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Weather>
	</xsl:template>

	<xsl:template match="Sources4006">
		<Sources>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Sources>
	</xsl:template>

	<xsl:template match="DateOfEvent4006">
		<DateOfEvent>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateOfEvent>
	</xsl:template>

	<xsl:template match="TimeOfEvent4006">
		<TimeOfEvent>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</TimeOfEvent>
	</xsl:template>

	<xsl:template match="PersonalNotes4006">
		<PersonalNotes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PersonalNotes>
	</xsl:template>

	<xsl:template match="Description4007">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Date4007">
		<Date>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Date>
	</xsl:template>

	<xsl:template match="Participants4010">
		<Participants>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Participants>
	</xsl:template>

	<xsl:template match="Role4010">
		<Role>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Role>
	</xsl:template>

	<xsl:template match="MsFeatures5001">
		<MsFeatures>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MsFeatures>
	</xsl:template>

	<xsl:template match="PartOfSpeech5001">
		<PartOfSpeech>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PartOfSpeech>
	</xsl:template>

	<xsl:template match="InflectionClass5001">
		<InflectionClass>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</InflectionClass>
	</xsl:template>

	<xsl:template match="Stratum5001">
		<Stratum>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Stratum>
	</xsl:template>

	<xsl:template match="ProdRestrict5001">
		<ProdRestrict>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ProdRestrict>
	</xsl:template>

	<xsl:template match="FromPartsOfSpeech5001">
		<FromPartsOfSpeech>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FromPartsOfSpeech>
	</xsl:template>

	<xsl:template match="HomographNumber5002">
		<HomographNumber>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</HomographNumber>
	</xsl:template>

	<xsl:template match="CitationForm5002">
		<CitationForm>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CitationForm>
	</xsl:template>

	<xsl:template match="DateCreated5002">
		<DateCreated>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateCreated>
	</xsl:template>

	<xsl:template match="DateModified5002">
		<DateModified>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DateModified>
	</xsl:template>

	<xsl:template match="MorphoSyntaxAnalyses5002">
		<MorphoSyntaxAnalyses>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MorphoSyntaxAnalyses>
	</xsl:template>

	<xsl:template match="Senses5002">
		<Senses>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Senses>
	</xsl:template>

	<xsl:template match="Bibliography5002">
		<Bibliography>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Bibliography>
	</xsl:template>

	<xsl:template match="Etymology5002">
		<Etymology>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Etymology>
	</xsl:template>

	<xsl:template match="Restrictions5002">
		<Restrictions>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Restrictions>
	</xsl:template>

	<xsl:template match="SummaryDefinition5002">
		<SummaryDefinition>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SummaryDefinition>
	</xsl:template>

	<xsl:template match="LiteralMeaning5002">
		<LiteralMeaning>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LiteralMeaning>
	</xsl:template>

	<xsl:template match="MainEntriesOrSenses5002">
		<MainEntriesOrSenses>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MainEntriesOrSenses>
	</xsl:template>

	<xsl:template match="Comment5002">
		<Comment>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Comment>
	</xsl:template>

	<xsl:template match="DoNotUseForParsing5002">
		<DoNotUseForParsing>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DoNotUseForParsing>
	</xsl:template>

	<xsl:template match="ExcludeAsHeadword5002">
		<ExcludeAsHeadword>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ExcludeAsHeadword>
	</xsl:template>

	<xsl:template match="LexemeForm5002">
		<LexemeForm>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LexemeForm>
	</xsl:template>

	<xsl:template match="AlternateForms5002">
		<AlternateForms>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AlternateForms>
	</xsl:template>

	<xsl:template match="Pronunciations5002">
		<Pronunciations>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Pronunciations>
	</xsl:template>

	<xsl:template match="ImportResidue5002">
		<ImportResidue>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ImportResidue>
	</xsl:template>

	<xsl:template match="LiftResidue5002">
		<LiftResidue>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LiftResidue>
	</xsl:template>

	<xsl:template match="EntryRefs5002">
		<EntryRefs>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</EntryRefs>
	</xsl:template>

	<xsl:template match="Example5004">
		<Example>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Example>
	</xsl:template>

	<xsl:template match="Reference5004">
		<Reference>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Reference>
	</xsl:template>

	<xsl:template match="Translations5004">
		<Translations>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Translations>
	</xsl:template>

	<xsl:template match="LiftResidue5004">
		<LiftResidue>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LiftResidue>
	</xsl:template>

	<xsl:template match="Entries5005">
		<Entries>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Entries>
	</xsl:template>

	<xsl:template match="Appendixes5005">
		<Appendixes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Appendixes>
	</xsl:template>

	<xsl:template match="SenseTypes5005">
		<SenseTypes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SenseTypes>
	</xsl:template>

	<xsl:template match="UsageTypes5005">
		<UsageTypes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</UsageTypes>
	</xsl:template>

	<xsl:template match="DomainTypes5005">
		<DomainTypes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DomainTypes>
	</xsl:template>

	<xsl:template match="MorphTypes5005">
		<MorphTypes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MorphTypes>
	</xsl:template>

	<xsl:template match="LexicalFormIndex5005">
		<LexicalFormIndex>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LexicalFormIndex>
	</xsl:template>

	<xsl:template match="AllomorphIndex5005">
		<AllomorphIndex>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AllomorphIndex>
	</xsl:template>

	<xsl:template match="Introduction5005">
		<Introduction>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Introduction>
	</xsl:template>

	<xsl:template match="IsHeadwordCitationForm5005">
		<IsHeadwordCitationForm>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IsHeadwordCitationForm>
	</xsl:template>

	<xsl:template match="IsBodyInSeparateSubentry5005">
		<IsBodyInSeparateSubentry>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IsBodyInSeparateSubentry>
	</xsl:template>

	<xsl:template match="Status5005">
		<Status>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Status>
	</xsl:template>

	<xsl:template match="Styles5005">
		<Styles>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Styles>
	</xsl:template>

	<xsl:template match="ReversalIndexes5005">
		<ReversalIndexes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ReversalIndexes>
	</xsl:template>

	<xsl:template match="References5005">
		<References>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</References>
	</xsl:template>

	<xsl:template match="Resources5005">
		<Resources>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Resources>
	</xsl:template>

	<xsl:template match="VariantEntryTypes5005">
		<VariantEntryTypes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</VariantEntryTypes>
	</xsl:template>

	<xsl:template match="ComplexEntryTypes5005">
		<ComplexEntryTypes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ComplexEntryTypes>
	</xsl:template>

	<xsl:template match="Form5014">
		<Form>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Form>
	</xsl:template>

	<xsl:template match="Location5014">
		<Location>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Location>
	</xsl:template>

	<xsl:template match="MediaFiles5014">
		<MediaFiles>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MediaFiles>
	</xsl:template>

	<xsl:template match="CVPattern5014">
		<CVPattern>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CVPattern>
	</xsl:template>

	<xsl:template match="Tone5014">
		<Tone>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Tone>
	</xsl:template>

	<xsl:template match="LiftResidue5014">
		<LiftResidue>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LiftResidue>
	</xsl:template>

	<xsl:template match="MorphoSyntaxAnalysis5016">
		<MorphoSyntaxAnalysis>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MorphoSyntaxAnalysis>
	</xsl:template>

	<xsl:template match="AnthroCodes5016">
		<AnthroCodes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AnthroCodes>
	</xsl:template>

	<xsl:template match="Senses5016">
		<Senses>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Senses>
	</xsl:template>

	<xsl:template match="Appendixes5016">
		<Appendixes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Appendixes>
	</xsl:template>

	<xsl:template match="Definition5016">
		<Definition>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Definition>
	</xsl:template>

	<xsl:template match="DomainTypes5016">
		<DomainTypes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DomainTypes>
	</xsl:template>

	<xsl:template match="Examples5016">
		<Examples>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Examples>
	</xsl:template>

	<xsl:template match="Gloss5016">
		<Gloss>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Gloss>
	</xsl:template>

	<xsl:template match="ReversalEntries5016">
		<ReversalEntries>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ReversalEntries>
	</xsl:template>

	<xsl:template match="ScientificName5016">
		<ScientificName>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ScientificName>
	</xsl:template>

	<xsl:template match="SenseType5016">
		<SenseType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SenseType>
	</xsl:template>

	<xsl:template match="ThesaurusItems5016">
		<ThesaurusItems>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ThesaurusItems>
	</xsl:template>

	<xsl:template match="UsageTypes5016">
		<UsageTypes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</UsageTypes>
	</xsl:template>

	<xsl:template match="AnthroNote5016">
		<AnthroNote>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AnthroNote>
	</xsl:template>

	<xsl:template match="Bibliography5016">
		<Bibliography>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Bibliography>
	</xsl:template>

	<xsl:template match="DiscourseNote5016">
		<DiscourseNote>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DiscourseNote>
	</xsl:template>

	<xsl:template match="EncyclopedicInfo5016">
		<EncyclopedicInfo>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</EncyclopedicInfo>
	</xsl:template>

	<xsl:template match="GeneralNote5016">
		<GeneralNote>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</GeneralNote>
	</xsl:template>

	<xsl:template match="GrammarNote5016">
		<GrammarNote>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</GrammarNote>
	</xsl:template>

	<xsl:template match="PhonologyNote5016">
		<PhonologyNote>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PhonologyNote>
	</xsl:template>

	<xsl:template match="Restrictions5016">
		<Restrictions>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Restrictions>
	</xsl:template>

	<xsl:template match="SemanticsNote5016">
		<SemanticsNote>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SemanticsNote>
	</xsl:template>

	<xsl:template match="SocioLinguisticsNote5016">
		<SocioLinguisticsNote>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SocioLinguisticsNote>
	</xsl:template>

	<xsl:template match="Source5016">
		<Source>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Source>
	</xsl:template>

	<xsl:template match="Status5016">
		<Status>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Status>
	</xsl:template>

	<xsl:template match="SemanticDomains5016">
		<SemanticDomains>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SemanticDomains>
	</xsl:template>

	<xsl:template match="Pictures5016">
		<Pictures>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Pictures>
	</xsl:template>

	<xsl:template match="ImportResidue5016">
		<ImportResidue>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ImportResidue>
	</xsl:template>

	<xsl:template match="LiftResidue5016">
		<LiftResidue>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LiftResidue>
	</xsl:template>

	<xsl:template match="Adjacency5026">
		<Adjacency>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Adjacency>
	</xsl:template>

	<xsl:template match="MsEnvFeatures5027">
		<MsEnvFeatures>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MsEnvFeatures>
	</xsl:template>

	<xsl:template match="PhoneEnv5027">
		<PhoneEnv>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PhoneEnv>
	</xsl:template>

	<xsl:template match="MsEnvPartOfSpeech5027">
		<MsEnvPartOfSpeech>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MsEnvPartOfSpeech>
	</xsl:template>

	<xsl:template match="Position5027">
		<Position>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Position>
	</xsl:template>

	<xsl:template match="InflectionClasses5028">
		<InflectionClasses>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</InflectionClasses>
	</xsl:template>

	<xsl:template match="Input5029">
		<Input>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Input>
	</xsl:template>

	<xsl:template match="Output5029">
		<Output>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Output>
	</xsl:template>

	<xsl:template match="Name5030">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Description5030">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Stratum5030">
		<Stratum>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Stratum>
	</xsl:template>

	<xsl:template match="ToProdRestrict5030">
		<ToProdRestrict>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ToProdRestrict>
	</xsl:template>

	<xsl:template match="FromMsFeatures5031">
		<FromMsFeatures>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FromMsFeatures>
	</xsl:template>

	<xsl:template match="ToMsFeatures5031">
		<ToMsFeatures>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ToMsFeatures>
	</xsl:template>

	<xsl:template match="FromPartOfSpeech5031">
		<FromPartOfSpeech>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FromPartOfSpeech>
	</xsl:template>

	<xsl:template match="ToPartOfSpeech5031">
		<ToPartOfSpeech>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ToPartOfSpeech>
	</xsl:template>

	<xsl:template match="FromInflectionClass5031">
		<FromInflectionClass>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FromInflectionClass>
	</xsl:template>

	<xsl:template match="ToInflectionClass5031">
		<ToInflectionClass>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ToInflectionClass>
	</xsl:template>

	<xsl:template match="AffixCategory5031">
		<AffixCategory>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AffixCategory>
	</xsl:template>

	<xsl:template match="FromStemName5031">
		<FromStemName>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FromStemName>
	</xsl:template>

	<xsl:template match="Stratum5031">
		<Stratum>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Stratum>
	</xsl:template>

	<xsl:template match="FromProdRestrict5031">
		<FromProdRestrict>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FromProdRestrict>
	</xsl:template>

	<xsl:template match="ToProdRestrict5031">
		<ToProdRestrict>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ToProdRestrict>
	</xsl:template>

	<xsl:template match="PartOfSpeech5032">
		<PartOfSpeech>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PartOfSpeech>
	</xsl:template>

	<xsl:template match="MsFeatures5032">
		<MsFeatures>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MsFeatures>
	</xsl:template>

	<xsl:template match="InflFeats5032">
		<InflFeats>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</InflFeats>
	</xsl:template>

	<xsl:template match="InflectionClass5032">
		<InflectionClass>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</InflectionClass>
	</xsl:template>

	<xsl:template match="ProdRestrict5032">
		<ProdRestrict>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ProdRestrict>
	</xsl:template>

	<xsl:template match="HeadLast5033">
		<HeadLast>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</HeadLast>
	</xsl:template>

	<xsl:template match="OverridingMsa5033">
		<OverridingMsa>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</OverridingMsa>
	</xsl:template>

	<xsl:template match="ToMsa5034">
		<ToMsa>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ToMsa>
	</xsl:template>

	<xsl:template match="Form5035">
		<Form>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Form>
	</xsl:template>

	<xsl:template match="MorphType5035">
		<MorphType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MorphType>
	</xsl:template>

	<xsl:template match="IsAbstract5035">
		<IsAbstract>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IsAbstract>
	</xsl:template>

	<xsl:template match="LiftResidue5035">
		<LiftResidue>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LiftResidue>
	</xsl:template>

	<xsl:template match="Name5036">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Description5036">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Optional5036">
		<Optional>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Optional>
	</xsl:template>

	<xsl:template match="Name5037">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Description5037">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Slots5037">
		<Slots>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Slots>
	</xsl:template>

	<xsl:template match="Stratum5037">
		<Stratum>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Stratum>
	</xsl:template>

	<xsl:template match="Region5037">
		<Region>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Region>
	</xsl:template>

	<xsl:template match="PrefixSlots5037">
		<PrefixSlots>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PrefixSlots>
	</xsl:template>

	<xsl:template match="SuffixSlots5037">
		<SuffixSlots>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SuffixSlots>
	</xsl:template>

	<xsl:template match="Final5037">
		<Final>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Final>
	</xsl:template>

	<xsl:template match="InflFeats5038">
		<InflFeats>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</InflFeats>
	</xsl:template>

	<xsl:template match="AffixCategory5038">
		<AffixCategory>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AffixCategory>
	</xsl:template>

	<xsl:template match="PartOfSpeech5038">
		<PartOfSpeech>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PartOfSpeech>
	</xsl:template>

	<xsl:template match="Slots5038">
		<Slots>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Slots>
	</xsl:template>

	<xsl:template match="FromProdRestrict5038">
		<FromProdRestrict>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FromProdRestrict>
	</xsl:template>

	<xsl:template match="Abbreviation5039">
		<Abbreviation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Abbreviation>
	</xsl:template>

	<xsl:template match="Description5039">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Name5039">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Subclasses5039">
		<Subclasses>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Subclasses>
	</xsl:template>

	<xsl:template match="RulesOfReferral5039">
		<RulesOfReferral>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RulesOfReferral>
	</xsl:template>

	<xsl:template match="StemNames5039">
		<StemNames>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</StemNames>
	</xsl:template>

	<xsl:template match="ReferenceForms5039">
		<ReferenceForms>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ReferenceForms>
	</xsl:template>

	<xsl:template match="Strata5040">
		<Strata>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Strata>
	</xsl:template>

	<xsl:template match="CompoundRules5040">
		<CompoundRules>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CompoundRules>
	</xsl:template>

	<xsl:template match="AdhocCoProhibitions5040">
		<AdhocCoProhibitions>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AdhocCoProhibitions>
	</xsl:template>

	<xsl:template match="AnalyzingAgents5040">
		<AnalyzingAgents>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AnalyzingAgents>
	</xsl:template>

	<xsl:template match="TestSets5040">
		<TestSets>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</TestSets>
	</xsl:template>

	<xsl:template match="GlossSystem5040">
		<GlossSystem>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</GlossSystem>
	</xsl:template>

	<xsl:template match="ParserParameters5040">
		<ParserParameters>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ParserParameters>
	</xsl:template>

	<xsl:template match="ProdRestrict5040">
		<ProdRestrict>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ProdRestrict>
	</xsl:template>

	<xsl:template match="Components5041">
		<Components>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Components>
	</xsl:template>

	<xsl:template match="GlossString5041">
		<GlossString>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</GlossString>
	</xsl:template>

	<xsl:template match="GlossBundle5041">
		<GlossBundle>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</GlossBundle>
	</xsl:template>

	<xsl:template match="LiftResidue5041">
		<LiftResidue>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LiftResidue>
	</xsl:template>

	<xsl:template match="Postfix5042">
		<Postfix>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Postfix>
	</xsl:template>

	<xsl:template match="Prefix5042">
		<Prefix>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Prefix>
	</xsl:template>

	<xsl:template match="SecondaryOrder5042">
		<SecondaryOrder>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SecondaryOrder>
	</xsl:template>

	<xsl:template match="Name5044">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Description5044">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Input5044">
		<Input>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Input>
	</xsl:template>

	<xsl:template match="Output5044">
		<Output>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Output>
	</xsl:template>

	<xsl:template match="PhoneEnv5045">
		<PhoneEnv>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PhoneEnv>
	</xsl:template>

	<xsl:template match="StemName5045">
		<StemName>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</StemName>
	</xsl:template>

	<xsl:template match="Contents5046">
		<Contents>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Contents>
	</xsl:template>

	<xsl:template match="Abbreviation5047">
		<Abbreviation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Abbreviation>
	</xsl:template>

	<xsl:template match="Description5047">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Name5047">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Regions5047">
		<Regions>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Regions>
	</xsl:template>

	<xsl:template match="DefaultAffix5047">
		<DefaultAffix>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DefaultAffix>
	</xsl:template>

	<xsl:template match="DefaultStem5047">
		<DefaultStem>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DefaultStem>
	</xsl:template>

	<xsl:template match="Abbreviation5048">
		<Abbreviation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Abbreviation>
	</xsl:template>

	<xsl:template match="Description5048">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Name5048">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Phonemes5048">
		<Phonemes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Phonemes>
	</xsl:template>

	<xsl:template match="InherFeatVal5049">
		<InherFeatVal>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</InherFeatVal>
	</xsl:template>

	<xsl:template match="EmptyParadigmCells5049">
		<EmptyParadigmCells>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</EmptyParadigmCells>
	</xsl:template>

	<xsl:template match="RulesOfReferral5049">
		<RulesOfReferral>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RulesOfReferral>
	</xsl:template>

	<xsl:template match="InflectionClasses5049">
		<InflectionClasses>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</InflectionClasses>
	</xsl:template>

	<xsl:template match="AffixTemplates5049">
		<AffixTemplates>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AffixTemplates>
	</xsl:template>

	<xsl:template match="AffixSlots5049">
		<AffixSlots>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AffixSlots>
	</xsl:template>

	<xsl:template match="StemNames5049">
		<StemNames>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</StemNames>
	</xsl:template>

	<xsl:template match="BearableFeatures5049">
		<BearableFeatures>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BearableFeatures>
	</xsl:template>

	<xsl:template match="InflectableFeats5049">
		<InflectableFeats>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</InflectableFeats>
	</xsl:template>

	<xsl:template match="ReferenceForms5049">
		<ReferenceForms>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ReferenceForms>
	</xsl:template>

	<xsl:template match="DefaultFeatures5049">
		<DefaultFeatures>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DefaultFeatures>
	</xsl:template>

	<xsl:template match="DefaultInflectionClass5049">
		<DefaultInflectionClass>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DefaultInflectionClass>
	</xsl:template>

	<xsl:template match="CatalogSourceId5049">
		<CatalogSourceId>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CatalogSourceId>
	</xsl:template>

	<xsl:template match="PartsOfSpeech5052">
		<PartsOfSpeech>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PartsOfSpeech>
	</xsl:template>

	<xsl:template match="Entries5052">
		<Entries>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Entries>
	</xsl:template>

	<xsl:template match="WritingSystem5052">
		<WritingSystem>
			<xsl:copy-of select="@*"/>
			<Uni><xsl:value-of select="Link/@ws"/></Uni>
		</WritingSystem>
	</xsl:template>

	<xsl:template match="Subentries5053">
		<Subentries>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Subentries>
	</xsl:template>

	<xsl:template match="PartOfSpeech5053">
		<PartOfSpeech>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PartOfSpeech>
	</xsl:template>

	<xsl:template match="ReversalForm5053">
		<ReversalForm>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ReversalForm>
	</xsl:template>

	<xsl:template match="Source5054">
		<Source>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Source>
	</xsl:template>

	<xsl:template match="SoundFilePath5054">
		<SoundFilePath>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SoundFilePath>
	</xsl:template>

	<xsl:template match="Contents5054">
		<Contents>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Contents>
	</xsl:template>

	<xsl:template match="Genres5054">
		<Genres>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Genres>
	</xsl:template>

	<xsl:template match="Abbreviation5054">
		<Abbreviation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Abbreviation>
	</xsl:template>

	<xsl:template match="IsTranslated5054">
		<IsTranslated>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</IsTranslated>
	</xsl:template>

	<xsl:template match="Category5059">
		<Category>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Category>
	</xsl:template>

	<xsl:template match="MsFeatures5059">
		<MsFeatures>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MsFeatures>
	</xsl:template>

	<xsl:template match="Stems5059">
		<Stems>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Stems>
	</xsl:template>

	<xsl:template match="Derivation5059">
		<Derivation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Derivation>
	</xsl:template>

	<xsl:template match="Meanings5059">
		<Meanings>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Meanings>
	</xsl:template>

	<xsl:template match="MorphBundles5059">
		<MorphBundles>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MorphBundles>
	</xsl:template>

	<xsl:template match="CompoundRuleApps5059">
		<CompoundRuleApps>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CompoundRuleApps>
	</xsl:template>

	<xsl:template match="InflTemplateApps5059">
		<InflTemplateApps>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</InflTemplateApps>
	</xsl:template>

	<xsl:template match="Form5060">
		<Form>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Form>
	</xsl:template>

	<xsl:template match="Form5062">
		<Form>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Form>
	</xsl:template>

	<xsl:template match="Analyses5062">
		<Analyses>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Analyses>
	</xsl:template>

	<xsl:template match="SpellingStatus5062">
		<SpellingStatus>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SpellingStatus>
	</xsl:template>

	<xsl:template match="Checksum5062">
		<Checksum>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Checksum>
	</xsl:template>

	<xsl:template match="Form5064">
		<Form>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Form>
	</xsl:template>

	<xsl:template match="ThesaurusCentral5064">
		<ThesaurusCentral>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ThesaurusCentral>
	</xsl:template>

	<xsl:template match="ThesaurusItems5064">
		<ThesaurusItems>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ThesaurusItems>
	</xsl:template>

	<xsl:template match="AnthroCentral5064">
		<AnthroCentral>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AnthroCentral>
	</xsl:template>

	<xsl:template match="AnthroCodes5064">
		<AnthroCodes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AnthroCodes>
	</xsl:template>

	<xsl:template match="Wordforms5065">
		<Wordforms>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Wordforms>
	</xsl:template>

	<xsl:template match="WritingSystem5065">
		<WritingSystem>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</WritingSystem>
	</xsl:template>

	<xsl:template match="Content5068">
		<Content>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Content>
	</xsl:template>

	<xsl:template match="Content5069">
		<Content>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Content>
	</xsl:template>

	<xsl:template match="Content5070">
		<Content>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Content>
	</xsl:template>

	<xsl:template match="Modification5070">
		<Modification>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Modification>
	</xsl:template>

	<xsl:template match="OutputForm5072">
		<OutputForm>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</OutputForm>
	</xsl:template>

	<xsl:template match="LeftForm5073">
		<LeftForm>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LeftForm>
	</xsl:template>

	<xsl:template match="RightForm5073">
		<RightForm>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RightForm>
	</xsl:template>

	<xsl:template match="Linker5073">
		<Linker>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Linker>
	</xsl:template>

	<xsl:template match="AffixForm5074">
		<AffixForm>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AffixForm>
	</xsl:template>

	<xsl:template match="AffixMsa5074">
		<AffixMsa>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AffixMsa>
	</xsl:template>

	<xsl:template match="OutputMsa5074">
		<OutputMsa>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</OutputMsa>
	</xsl:template>

	<xsl:template match="Slot5075">
		<Slot>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Slot>
	</xsl:template>

	<xsl:template match="AffixForm5075">
		<AffixForm>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AffixForm>
	</xsl:template>

	<xsl:template match="AffixMsa5075">
		<AffixMsa>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AffixMsa>
	</xsl:template>

	<xsl:template match="Template5076">
		<Template>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Template>
	</xsl:template>

	<xsl:template match="SlotApps5076">
		<SlotApps>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SlotApps>
	</xsl:template>

	<xsl:template match="Rule5077">
		<Rule>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Rule>
	</xsl:template>

	<xsl:template match="VacuousApp5077">
		<VacuousApp>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</VacuousApp>
	</xsl:template>

	<xsl:template match="Stratum5078">
		<Stratum>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Stratum>
	</xsl:template>

	<xsl:template match="CompoundRuleApps5078">
		<CompoundRuleApps>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CompoundRuleApps>
	</xsl:template>

	<xsl:template match="DerivAffApp5078">
		<DerivAffApp>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DerivAffApp>
	</xsl:template>

	<xsl:template match="TemplateApp5078">
		<TemplateApp>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</TemplateApp>
	</xsl:template>

	<xsl:template match="PRuleApps5078">
		<PRuleApps>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PRuleApps>
	</xsl:template>

	<xsl:template match="Name5081">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Description5081">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Minimum5082">
		<Minimum>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Minimum>
	</xsl:template>

	<xsl:template match="Maximum5082">
		<Maximum>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Maximum>
	</xsl:template>

	<xsl:template match="Member5082">
		<Member>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Member>
	</xsl:template>

	<xsl:template match="Members5083">
		<Members>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Members>
	</xsl:template>

	<xsl:template match="FeatureStructure5085">
		<FeatureStructure>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FeatureStructure>
	</xsl:template>

	<xsl:template match="FeatureStructure5086">
		<FeatureStructure>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FeatureStructure>
	</xsl:template>

	<xsl:template match="PlusConstr5086">
		<PlusConstr>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PlusConstr>
	</xsl:template>

	<xsl:template match="MinusConstr5086">
		<MinusConstr>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MinusConstr>
	</xsl:template>

	<xsl:template match="FeatureStructure5087">
		<FeatureStructure>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FeatureStructure>
	</xsl:template>

	<xsl:template match="Name5089">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Phonemes5089">
		<Phonemes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Phonemes>
	</xsl:template>

	<xsl:template match="BoundaryMarkers5089">
		<BoundaryMarkers>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BoundaryMarkers>
	</xsl:template>

	<xsl:template match="Description5089">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Name5090">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Description5090">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Codes5090">
		<Codes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Codes>
	</xsl:template>

	<xsl:template match="BasicIPASymbol5092">
		<BasicIPASymbol>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BasicIPASymbol>
	</xsl:template>

	<xsl:template match="Features5092">
		<Features>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Features>
	</xsl:template>

	<xsl:template match="Name5093">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Description5093">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Abbreviation5093">
		<Abbreviation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Abbreviation>
	</xsl:template>

	<xsl:template match="Features5094">
		<Features>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Features>
	</xsl:template>

	<xsl:template match="Segments5095">
		<Segments>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Segments>
	</xsl:template>

	<xsl:template match="Feature5096">
		<Feature>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Feature>
	</xsl:template>

	<xsl:template match="Name5097">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Description5097">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="LeftContext5097">
		<LeftContext>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LeftContext>
	</xsl:template>

	<xsl:template match="RightContext5097">
		<RightContext>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RightContext>
	</xsl:template>

	<xsl:template match="AMPLEStringSegment5097">
		<AMPLEStringSegment>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AMPLEStringSegment>
	</xsl:template>

	<xsl:template match="StringRepresentation5097">
		<StringRepresentation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</StringRepresentation>
	</xsl:template>

	<xsl:template match="Representation5098">
		<Representation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Representation>
	</xsl:template>

	<xsl:template match="PhonemeSets5099">
		<PhonemeSets>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PhonemeSets>
	</xsl:template>

	<xsl:template match="Environments5099">
		<Environments>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Environments>
	</xsl:template>

	<xsl:template match="NaturalClasses5099">
		<NaturalClasses>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</NaturalClasses>
	</xsl:template>

	<xsl:template match="Contexts5099">
		<Contexts>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Contexts>
	</xsl:template>

	<xsl:template match="FeatConstraints5099">
		<FeatConstraints>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FeatConstraints>
	</xsl:template>

	<xsl:template match="PhonRuleFeats5099">
		<PhonRuleFeats>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PhonRuleFeats>
	</xsl:template>

	<xsl:template match="PhonRules5099">
		<PhonRules>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PhonRules>
	</xsl:template>

	<xsl:template match="StemForm5100">
		<StemForm>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</StemForm>
	</xsl:template>

	<xsl:template match="StemMsa5100">
		<StemMsa>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</StemMsa>
	</xsl:template>

	<xsl:template match="InflectionalFeats5100">
		<InflectionalFeats>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</InflectionalFeats>
	</xsl:template>

	<xsl:template match="StratumApps5100">
		<StratumApps>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</StratumApps>
	</xsl:template>

	<xsl:template match="Allomorphs5101">
		<Allomorphs>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Allomorphs>
	</xsl:template>

	<xsl:template match="FirstAllomorph5101">
		<FirstAllomorph>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FirstAllomorph>
	</xsl:template>

	<xsl:template match="RestOfAllos5101">
		<RestOfAllos>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RestOfAllos>
	</xsl:template>

	<xsl:template match="Morphemes5102">
		<Morphemes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Morphemes>
	</xsl:template>

	<xsl:template match="FirstMorpheme5102">
		<FirstMorpheme>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FirstMorpheme>
	</xsl:template>

	<xsl:template match="RestOfMorphs5102">
		<RestOfMorphs>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RestOfMorphs>
	</xsl:template>

	<xsl:template match="Content5103">
		<Content>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Content>
	</xsl:template>

	<xsl:template match="Name5105">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Description5105">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Cases5105">
		<Cases>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Cases>
	</xsl:template>

	<xsl:template match="LeftMsa5106">
		<LeftMsa>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LeftMsa>
	</xsl:template>

	<xsl:template match="RightMsa5106">
		<RightMsa>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RightMsa>
	</xsl:template>

	<xsl:template match="Linker5106">
		<Linker>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Linker>
	</xsl:template>

	<xsl:template match="Glosses5108">
		<Glosses>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Glosses>
	</xsl:template>

	<xsl:template match="Name5109">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Abbreviation5109">
		<Abbreviation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Abbreviation>
	</xsl:template>

	<xsl:template match="Type5109">
		<Type>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Type>
	</xsl:template>

	<xsl:template match="AfterSeparator5109">
		<AfterSeparator>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AfterSeparator>
	</xsl:template>

	<xsl:template match="ComplexNameSeparator5109">
		<ComplexNameSeparator>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ComplexNameSeparator>
	</xsl:template>

	<xsl:template match="ComplexNameFirst5109">
		<ComplexNameFirst>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ComplexNameFirst>
	</xsl:template>

	<xsl:template match="Status5109">
		<Status>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Status>
	</xsl:template>

	<xsl:template match="FeatStructFrag5109">
		<FeatStructFrag>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FeatStructFrag>
	</xsl:template>

	<xsl:template match="GlossItems5109">
		<GlossItems>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</GlossItems>
	</xsl:template>

	<xsl:template match="Target5109">
		<Target>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Target>
	</xsl:template>

	<xsl:template match="EticID5109">
		<EticID>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</EticID>
	</xsl:template>

	<xsl:template match="Name5110">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Description5110">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Members5110">
		<Members>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Members>
	</xsl:template>

	<xsl:template match="Form5112">
		<Form>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Form>
	</xsl:template>

	<xsl:template match="Morph5112">
		<Morph>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Morph>
	</xsl:template>

	<xsl:template match="Msa5112">
		<Msa>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Msa>
	</xsl:template>

	<xsl:template match="Sense5112">
		<Sense>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Sense>
	</xsl:template>

	<xsl:template match="Comment5113">
		<Comment>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Comment>
	</xsl:template>

	<xsl:template match="Form5113">
		<Form>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Form>
	</xsl:template>

	<xsl:template match="Gloss5113">
		<Gloss>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Gloss>
	</xsl:template>

	<xsl:template match="Source5113">
		<Source>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Source>
	</xsl:template>

	<xsl:template match="LiftResidue5113">
		<LiftResidue>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LiftResidue>
	</xsl:template>

	<xsl:template match="Ref5116">
		<Ref>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Ref>
	</xsl:template>

	<xsl:template match="KeyWord5116">
		<KeyWord>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</KeyWord>
	</xsl:template>

	<xsl:template match="Status5116">
		<Status>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Status>
	</xsl:template>

	<xsl:template match="Rendering5116">
		<Rendering>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Rendering>
	</xsl:template>

	<xsl:template match="Location5116">
		<Location>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Location>
	</xsl:template>

	<xsl:template match="PartOfSpeech5117">
		<PartOfSpeech>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PartOfSpeech>
	</xsl:template>

	<xsl:template match="ReverseAbbr5118">
		<ReverseAbbr>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ReverseAbbr>
	</xsl:template>

	<xsl:template match="ReverseAbbreviation5119">
		<ReverseAbbreviation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ReverseAbbreviation>
	</xsl:template>

	<xsl:template match="MappingType5119">
		<MappingType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MappingType>
	</xsl:template>

	<xsl:template match="Members5119">
		<Members>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Members>
	</xsl:template>

	<xsl:template match="ReverseName5119">
		<ReverseName>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ReverseName>
	</xsl:template>

	<xsl:template match="Comment5120">
		<Comment>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Comment>
	</xsl:template>

	<xsl:template match="Targets5120">
		<Targets>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Targets>
	</xsl:template>

	<xsl:template match="Name5120">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="LiftResidue5120">
		<LiftResidue>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LiftResidue>
	</xsl:template>

	<xsl:template match="Explanation5121">
		<Explanation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Explanation>
	</xsl:template>

	<xsl:template match="Sense5121">
		<Sense>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Sense>
	</xsl:template>

	<xsl:template match="Template5122">
		<Template>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Template>
	</xsl:template>

	<xsl:template match="BasedOn5123">
		<BasedOn>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</BasedOn>
	</xsl:template>

	<xsl:template match="Rows5123">
		<Rows>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Rows>
	</xsl:template>

	<xsl:template match="ConstChartTempl5124">
		<ConstChartTempl>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ConstChartTempl>
	</xsl:template>

	<xsl:template match="Charts5124">
		<Charts>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Charts>
	</xsl:template>

	<xsl:template match="ChartMarkers5124">
		<ChartMarkers>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ChartMarkers>
	</xsl:template>

	<xsl:template match="Occurrences5125">
		<Occurrences>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Occurrences>
	</xsl:template>

	<xsl:template match="SeeAlso5125">
		<SeeAlso>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SeeAlso>
	</xsl:template>

	<xsl:template match="Renderings5125">
		<Renderings>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Renderings>
	</xsl:template>

	<xsl:template match="TermId5125">
		<TermId>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</TermId>
	</xsl:template>

	<xsl:template match="SurfaceForm5126">
		<SurfaceForm>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SurfaceForm>
	</xsl:template>

	<xsl:template match="Meaning5126">
		<Meaning>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Meaning>
	</xsl:template>

	<xsl:template match="Explanation5126">
		<Explanation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Explanation>
	</xsl:template>

	<xsl:template match="VariantEntryTypes5127">
		<VariantEntryTypes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</VariantEntryTypes>
	</xsl:template>

	<xsl:template match="ComplexEntryTypes5127">
		<ComplexEntryTypes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ComplexEntryTypes>
	</xsl:template>

	<xsl:template match="PrimaryLexemes5127">
		<PrimaryLexemes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PrimaryLexemes>
	</xsl:template>

	<xsl:template match="ComponentLexemes5127">
		<ComponentLexemes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ComponentLexemes>
	</xsl:template>

	<xsl:template match="HideMinorEntry5127">
		<HideMinorEntry>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</HideMinorEntry>
	</xsl:template>

	<xsl:template match="Summary5127">
		<Summary>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Summary>
	</xsl:template>

	<xsl:template match="LiftResidue5127">
		<LiftResidue>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LiftResidue>
	</xsl:template>

	<xsl:template match="RefType5127">
		<RefType>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RefType>
	</xsl:template>

	<xsl:template match="Description5128">
		<Description>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Description>
	</xsl:template>

	<xsl:template match="Name5128">
		<Name>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Name>
	</xsl:template>

	<xsl:template match="Direction5128">
		<Direction>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Direction>
	</xsl:template>

	<xsl:template match="InitialStratum5128">
		<InitialStratum>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</InitialStratum>
	</xsl:template>

	<xsl:template match="FinalStratum5128">
		<FinalStratum>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FinalStratum>
	</xsl:template>

	<xsl:template match="StrucDesc5128">
		<StrucDesc>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</StrucDesc>
	</xsl:template>

	<xsl:template match="RightHandSides5129">
		<RightHandSides>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RightHandSides>
	</xsl:template>

	<xsl:template match="StrucChange5130">
		<StrucChange>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</StrucChange>
	</xsl:template>

	<xsl:template match="LeftContext5131">
		<LeftContext>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LeftContext>
	</xsl:template>

	<xsl:template match="RightContext5131">
		<RightContext>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</RightContext>
	</xsl:template>

	<xsl:template match="StrucChange5131">
		<StrucChange>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</StrucChange>
	</xsl:template>

	<xsl:template match="InputPOSes5131">
		<InputPOSes>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</InputPOSes>
	</xsl:template>

	<xsl:template match="ExclRuleFeats5131">
		<ExclRuleFeats>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ExclRuleFeats>
	</xsl:template>

	<xsl:template match="ReqRuleFeats5131">
		<ReqRuleFeats>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ReqRuleFeats>
	</xsl:template>

	<xsl:template match="Item5132">
		<Item>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Item>
	</xsl:template>

	<xsl:template match="EthnologueCode6001">
		<EthnologueCode>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</EthnologueCode>
	</xsl:template>

	<xsl:template match="WorldRegion6001">
		<WorldRegion>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</WorldRegion>
	</xsl:template>

	<xsl:template match="MainCountry6001">
		<MainCountry>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MainCountry>
	</xsl:template>

	<xsl:template match="FieldWorkLocation6001">
		<FieldWorkLocation>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</FieldWorkLocation>
	</xsl:template>

	<xsl:template match="PartsOfSpeech6001">
		<PartsOfSpeech>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PartsOfSpeech>
	</xsl:template>

	<xsl:template match="Texts6001">
		<Texts>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Texts>
	</xsl:template>

	<xsl:template match="TranslationTags6001">
		<TranslationTags>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</TranslationTags>
	</xsl:template>

	<xsl:template match="Thesaurus6001">
		<Thesaurus>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Thesaurus>
	</xsl:template>

	<xsl:template match="WordformLookupLists6001">
		<WordformLookupLists>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</WordformLookupLists>
	</xsl:template>

	<xsl:template match="AnthroList6001">
		<AnthroList>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AnthroList>
	</xsl:template>

	<xsl:template match="LexDb6001">
		<LexDb>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</LexDb>
	</xsl:template>

	<xsl:template match="ResearchNotebook6001">
		<ResearchNotebook>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ResearchNotebook>
	</xsl:template>

	<xsl:template match="AnalysisWss6001">
		<AnalysisWss>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AnalysisWss>
	</xsl:template>

	<xsl:template match="CurVernWss6001">
		<CurVernWss>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CurVernWss>
	</xsl:template>

	<xsl:template match="CurAnalysisWss6001">
		<CurAnalysisWss>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CurAnalysisWss>
	</xsl:template>

	<xsl:template match="CurPronunWss6001">
		<CurPronunWss>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CurPronunWss>
	</xsl:template>

	<xsl:template match="MsFeatureSystem6001">
		<MsFeatureSystem>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MsFeatureSystem>
	</xsl:template>

	<xsl:template match="MorphologicalData6001">
		<MorphologicalData>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</MorphologicalData>
	</xsl:template>

	<xsl:template match="Styles6001">
		<Styles>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Styles>
	</xsl:template>

	<xsl:template match="Filters6001">
		<Filters>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Filters>
	</xsl:template>

	<xsl:template match="ConfidenceLevels6001">
		<ConfidenceLevels>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ConfidenceLevels>
	</xsl:template>

	<xsl:template match="Restrictions6001">
		<Restrictions>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Restrictions>
	</xsl:template>

	<xsl:template match="WeatherConditions6001">
		<WeatherConditions>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</WeatherConditions>
	</xsl:template>

	<xsl:template match="Roles6001">
		<Roles>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Roles>
	</xsl:template>

	<xsl:template match="Status6001">
		<Status>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Status>
	</xsl:template>

	<xsl:template match="Locations6001">
		<Locations>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Locations>
	</xsl:template>

	<xsl:template match="People6001">
		<People>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</People>
	</xsl:template>

	<xsl:template match="Education6001">
		<Education>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Education>
	</xsl:template>

	<xsl:template match="TimeOfDay6001">
		<TimeOfDay>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</TimeOfDay>
	</xsl:template>

	<xsl:template match="AffixCategories6001">
		<AffixCategories>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AffixCategories>
	</xsl:template>

	<xsl:template match="PhonologicalData6001">
		<PhonologicalData>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PhonologicalData>
	</xsl:template>

	<xsl:template match="Positions6001">
		<Positions>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Positions>
	</xsl:template>

	<xsl:template match="Overlays6001">
		<Overlays>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Overlays>
	</xsl:template>

	<xsl:template match="AnalyzingAgents6001">
		<AnalyzingAgents>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AnalyzingAgents>
	</xsl:template>

	<xsl:template match="TranslatedScripture6001">
		<TranslatedScripture>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</TranslatedScripture>
	</xsl:template>

	<xsl:template match="VernWss6001">
		<VernWss>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</VernWss>
	</xsl:template>

	<xsl:template match="ExtLinkRootDir6001">
		<ExtLinkRootDir>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ExtLinkRootDir>
	</xsl:template>

	<xsl:template match="SortSpecs6001">
		<SortSpecs>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SortSpecs>
	</xsl:template>

	<xsl:template match="UserAccounts6001">
		<UserAccounts>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</UserAccounts>
	</xsl:template>

	<xsl:template match="ActivatedFeatures6001">
		<ActivatedFeatures>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</ActivatedFeatures>
	</xsl:template>

	<xsl:template match="AnnotationDefs6001">
		<AnnotationDefs>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</AnnotationDefs>
	</xsl:template>

	<xsl:template match="Pictures6001">
		<Pictures>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Pictures>
	</xsl:template>

	<xsl:template match="SemanticDomainList6001">
		<SemanticDomainList>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</SemanticDomainList>
	</xsl:template>

	<xsl:template match="CheckLists6001">
		<CheckLists>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</CheckLists>
	</xsl:template>

	<xsl:template match="Media6001">
		<Media>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</Media>
	</xsl:template>

	<xsl:template match="GenreList6001">
		<GenreList>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</GenreList>
	</xsl:template>

	<xsl:template match="DiscourseData6001">
		<DiscourseData>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</DiscourseData>
	</xsl:template>

	<xsl:template match="TextMarkupTags6001">
		<TextMarkupTags>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</TextMarkupTags>
	</xsl:template>

	<xsl:template match="PhFeatureSystem6001">
		<PhFeatureSystem>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</PhFeatureSystem>
	</xsl:template>

	<!-- Copy everything else -->

	<xsl:template match="*">
		<xsl:copy>
			<xsl:copy-of select="@*"/>
			<xsl:apply-templates/>
		</xsl:copy>
	</xsl:template>

</xsl:transform>
