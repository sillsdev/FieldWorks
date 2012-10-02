<?xml version="1.0" encoding="UTF-8"?>

<!--
	Convert a standard full XML dump of a FieldWorks Notebook into a flat Standard Format file.
 -->

<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="text" omit-xml-declaration="yes" encoding="UTF-8"/>

<!--
	Strip all white space and leave it up to the stylesheet text elements below to put in
	appropriate spacing.
 -->

<xsl:strip-space elements="*"/>
<xsl:preserve-space elements="Run"/><!-- but these spaces are significant! -->

<!-- This is the basic default processing: ignore this element, but process all subelements. -->

<xsl:template match="*">
	<xsl:apply-templates/>
</xsl:template>

<!-- Omit these elements altogether. -->

<xsl:template match="Languages|Styles">
</xsl:template>

<!-- Write a header info field when the Entries element is encountered. -->

<xsl:template match="Entries">
	<xsl:text>\_info Exported from FieldWorks Language Explorer/Notebook:  </xsl:text>
	<xsl:value-of select="../@project"/>
	<xsl:text>&#32;&#32;</xsl:text>
	<xsl:value-of select="../@dateExported"/>
	<xsl:apply-templates/>
	<xsl:text>&#13;&#10;</xsl:text>
</xsl:template>

<!-- Find each Entry element and indicate it with a \entry field marker. -->

<xsl:template match="Entry">
	<xsl:text>&#13;&#10;&#13;&#10;\entry&#32;</xsl:text>
	<xsl:number level="multiple" format="1.1" count="Entry"/>
	<xsl:text>&#13;&#10;\dc&#32;</xsl:text>
	<xsl:value-of select="@dateCreated"/>
	<xsl:text>&#13;&#10;\dm&#32;</xsl:text>
	<xsl:value-of select="@dateModified"/>
	<xsl:apply-templates/>
	<xsl:text>&#13;&#10;\-entry&#32;</xsl:text>
	<xsl:number level="multiple" format="1.1" count="Entry"/>
</xsl:template>

<xsl:template match="Field[@type!='StText' and @type!='TsString' and @card!='collection' and @card!='sequence']">
	<xsl:choose>
		<xsl:when test="@name='DateOfEvent'">
			<xsl:text>&#13;&#10;\date&#32;</xsl:text>
			<xsl:value-of select="Item"/>
		</xsl:when>
		<xsl:when test="@name='Type'">
			<xsl:text>&#13;&#10;\type&#32;</xsl:text>
			<xsl:value-of select="Item"/>
		</xsl:when>
		<xsl:when test="@name='Confidence'">
			<xsl:text>&#13;&#10;\conf&#32;</xsl:text>
			<xsl:value-of select="Item"/>
		</xsl:when>
		<xsl:when test="@name='Status'">
			<xsl:text>&#13;&#10;\stat&#32;</xsl:text>
			<xsl:value-of select="Item"/>
		</xsl:when>
	</xsl:choose>
</xsl:template>

<!--
	@card='collection' or @card='sequence' means that the element may contain multiple Item
	subelements.
 -->

<xsl:template match="Field[@card='collection' or @card='sequence']">
	<xsl:if test="@name='Subentries'">
		<xsl:apply-templates/>
	</xsl:if>
	<xsl:if test="@name!='Subentries'">
		<xsl:for-each select="Item">
			<xsl:choose>
				<xsl:when test="../@name='TimeOfEvent'">
					<xsl:text>&#13;&#10;\time&#32;</xsl:text>
				</xsl:when>
				<xsl:when test="../@name='Researchers'">
					<xsl:text>&#13;&#10;\rsrc&#32;</xsl:text>
				</xsl:when>
				<xsl:when test="../@name='Sources'">
					<xsl:text>&#13;&#10;\so&#32;</xsl:text>
				</xsl:when>
				<xsl:when test="../@name='Locations'">
					<xsl:text>&#13;&#10;\loc&#32;</xsl:text>
				</xsl:when>
				<xsl:when test="../@name='Restrictions'">
					<xsl:text>&#13;&#10;\rest&#32;</xsl:text>
				</xsl:when>
				<xsl:when test="../@name='PhraseTags'">
					<xsl:text>&#13;&#10;\ptag&#32;</xsl:text>
				</xsl:when>
				<xsl:when test="../@name='AnthroCodes'">
					<xsl:text>&#13;&#10;\anth&#32;</xsl:text>
				</xsl:when>
				<xsl:when test="../@name='SeeAlso'">
					<xsl:text>&#13;&#10;\see&#32;</xsl:text>
				</xsl:when>
				<xsl:when test="../@name='SupersededBy'">
					<xsl:text>&#13;&#10;\supby&#32;</xsl:text>
				</xsl:when>
				<xsl:when test="../@name='CounterEvidence'">
					<xsl:text>&#13;&#10;\counter&#32;</xsl:text>
				</xsl:when>
				<xsl:when test="../@name='SupportingEvidence'">
					<xsl:text>&#13;&#10;\support&#32;</xsl:text>
				</xsl:when>
				<xsl:when test="../@name='Participants'">
					<xsl:text>&#13;&#10;\part&#32;</xsl:text>
				</xsl:when>
				<xsl:otherwise>
					<xsl:text>&#13;&#10;\</xsl:text>
					<xsl:value-of select="translate(../@name,' ','_')"/>
					<xsl:text>&#32;</xsl:text>
				</xsl:otherwise>
			</xsl:choose>
			<xsl:value-of select="."/>
		</xsl:for-each>
	</xsl:if>
</xsl:template>

<!-- TsString has Str and Runs, not one or Item elements -->

<xsl:template match="Field[@type='TsString']">
	<xsl:choose>
		<xsl:when test="@name='Title'">
			<xsl:text>&#13;&#10;\titl&#32;</xsl:text>
		</xsl:when>
		<xsl:otherwise>
			<xsl:text>&#13;&#10;\</xsl:text>
			<xsl:value-of select="translate(@name,' ','_')"/>
			<xsl:text>&#32;</xsl:text>
		</xsl:otherwise>
	</xsl:choose>
	<xsl:for-each select="Str/Run">
		<xsl:value-of select="."/>
	</xsl:for-each>
</xsl:template>


<!-- StText is rather complicated, but all such element share the same structure -->

<xsl:template match="Field[@type='StText']">
	<xsl:choose>
		<xsl:when test="@name='Description'">
			<xsl:text>&#13;&#10;\data&#32;</xsl:text>
		</xsl:when>
		<xsl:when test="@name='Hypothesis'">
			<xsl:text>&#13;&#10;\hype&#32;</xsl:text>
		</xsl:when>
		<xsl:when test="@name='Discussion'">
			<xsl:text>&#13;&#10;\disc&#32;</xsl:text>
		</xsl:when>
		<xsl:when test="@name='Conclusions'">
			<xsl:text>&#13;&#10;\conc&#32;</xsl:text>
		</xsl:when>
		<xsl:when test="@name='ExternalMaterials'">
			<xsl:text>&#13;&#10;\mtrl&#32;</xsl:text>
		</xsl:when>
		<xsl:when test="@name='FurtherQuestions'">
			<xsl:text>&#13;&#10;\ques&#32;</xsl:text>
		</xsl:when>
		<xsl:when test="@name='ResearchPlan'">
			<xsl:text>&#13;&#10;\plan&#32;</xsl:text>
		</xsl:when>
		<xsl:when test="@name='PersonalNotes'">
			<xsl:text>&#13;&#10;\note&#32;</xsl:text>
		</xsl:when>
		<xsl:otherwise>
			<xsl:text>&#13;&#10;\</xsl:text>
			<xsl:value-of select="translate(@name,' ','_')"/>
			<xsl:text>&#32;</xsl:text>
		</xsl:otherwise>
	</xsl:choose>
	<xsl:for-each select="StTxtPara">
		<xsl:if test="position()!=1">
			<xsl:text>&#13;&#10;</xsl:text>
		</xsl:if>
		<xsl:for-each select="Contents/Str/Run">
			<xsl:value-of select="."/>
		</xsl:for-each>
		<xsl:if test="position()!=last()">
			<xsl:text>&#13;&#10;</xsl:text>
		</xsl:if>
	</xsl:for-each>
</xsl:template>

</xsl:stylesheet>
