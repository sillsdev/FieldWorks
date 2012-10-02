<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:fo="http://www.w3.org/1999/XSL/Format" xmlns="urn:schemas-microsoft-com:xml-data" xmlns:dt="urn:schemas-microsoft-com:datatypes" xmlns:sql="urn:schemas-microsoft-com:xml-sql">
  <xsl:output method="xml" version="1.0" encoding="UTF-8"/>
  <!--
================================================================
Creates an XDR file for use with SQL Server 8.
  Input:    xmi2cellar3.xml file (this file is generated from the FieldWorks.xmi file after Magic2CellarStageX are applied).
  Output: XDR file
================================================================
Revision History is at the end of this file.
-->
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Main template
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template match="/">
	<xsl:variable name="sRecursionDepth">1 </xsl:variable>
	<!-- 2001.10.31 using only one for now; was "1 2 3 4 5 6 7 8 9 " -->
	<Schema xmlns="urn:schemas-microsoft-com:xml-data" xmlns:dt="urn:schemas-microsoft-com:datatypes" xmlns:sql="urn:schemas-microsoft-com:xml-sql" name="Schema" sql:id="MyMappingSchema" sql:is-mapping-schema="1">
	  <!-- Process all classes except CmObject-->
	  <xsl:for-each select="EntireModel/CellarModule/class">
		<!-- [@id!='CmObject'] -->
		<xsl:call-template name="CreateRealElementType">
		  <xsl:with-param name="ElementName" select="@id"/>
		  <xsl:with-param name="NameAugment"/>
		</xsl:call-template>
		<!-- Handle any recursion -->
		<xsl:if test="props/owning/@sig=@id">
		  <xsl:call-template name="ProcessRealElementTypesWithRecursion">
			<xsl:with-param name="sList">
			  <xsl:value-of select="$sRecursionDepth"/>
			</xsl:with-param>
			<xsl:with-param name="ElementName" select="@id"/>
		  </xsl:call-template>
		</xsl:if>
	  </xsl:for-each>
	  <!--
				Process strings
-->
	  <!-- note that this select can include all multi types only because we're *not* currently doing the flid-dependent method as described below. -->
	  <xsl:for-each select="EntireModel/CellarModule/class[props/basic/@sig='MultiUnicode' or props/basic/@sig='MultiString'  or props/basic/@sig='MultiText' ]">
		<xsl:variable name="ClassName" select="@id"/>
		<xsl:for-each select="props/basic">
		  <!-- note:the following is ideal but won't work until we can get access to flids, -->
		  <!--<ElementType>
						<xsl:attribute name="name"><xsl:value-of select="$ClassName"/>-<xsl:value-of select="@id"/></xsl:attribute>
						<element type="MultiTxt" sql:limit-field="Flid"
									   sql:limit-value="7001">
								<sql:relationship>
								<xsl:attribute name="key-relation">
									<xsl:value-of select="$ClassName"/>
								</xsl:attribute>
								<xsl:attribute name="key">Id</xsl:attribute>
								<xsl:attribute name="foreign-relation">MultiTxt$</xsl:attribute>
								<xsl:attribute name="foreign-key">Obj</xsl:attribute>
								</sql:relationship>
						</element>
					</ElementType>
					-->
		  <!-- note: until we can get the above to work, we need to explicitly use a View for this attr.
						note: this element is invoked on the owning class with something like this:
								<element type="CmPossibility_Name">
								<sql:relationship key-relation="CmPossibility" key="id" foreign-relation="CmPossibility_Name" foreign-key="obj"/>
								</element>
						-->
		  <ElementType>
			<xsl:attribute name="name"><xsl:value-of select="$ClassName"/>_<xsl:value-of select="@id"/></xsl:attribute>
			<AttributeType name="enc" dt:type="i4"/>
			<AttributeType name="txt" dt:type="string"/>
			<attribute type="enc" required="no"/>
			<attribute type="txt" required="no"/>
		  </ElementType>
		</xsl:for-each>
	  </xsl:for-each>
	  <!--
				Process all owning class_property combinations
-->
	  <xsl:for-each select="EntireModel/CellarModule/class[props/owning]">
		<xsl:variable name="ClassName" select="@id"/>
		<xsl:for-each select="props/owning">
		  <xsl:variable name="FirstRecursionAugment">
			<xsl:if test="@sig=$ClassName">
			  <xsl:value-of select="substring-before($sRecursionDepth,' ')"/>
			</xsl:if>
		  </xsl:variable>
		  <xsl:call-template name="CreateClassPropertyElementType">
			<xsl:with-param name="ClassName" select="$ClassName"/>
			<xsl:with-param name="NameAugment"/>
			<xsl:with-param name="NextAugment" select="$FirstRecursionAugment"/>
		  </xsl:call-template>
		  <!--
				Handle any recursion
-->
		  <xsl:if test="@sig=$ClassName">
			<xsl:call-template name="ProcessClassPropertyElementTypesWithRecursion">
			  <xsl:with-param name="sList">
				<xsl:value-of select="$sRecursionDepth"/>
			  </xsl:with-param>
			  <xsl:with-param name="ClassName" select="$ClassName"/>
			  <xsl:with-param name="NameAugment"/>
			</xsl:call-template>
		  </xsl:if>
		</xsl:for-each>
	  </xsl:for-each>
	  <xsl:for-each select="EntireModel/CellarModule/class/props/rel[@card!='atomic']">
		<ElementType content="mixed" model="closed" order="many">
		  <xsl:attribute name="name"><xsl:value-of select="../../@id"/>_<xsl:value-of select="@id"/></xsl:attribute>
		  <AttributeType dt:type="i4">
			<xsl:attribute name="name">dst</xsl:attribute>
		  </AttributeType>
		  <attribute>
			<xsl:attribute name="type">dst</xsl:attribute>
			<xsl:attribute name="sql:field">dst</xsl:attribute>
		  </attribute>
		  <xsl:if test="@card='seq'">
			<AttributeType dt:type="i4">
			  <xsl:attribute name="name">ord</xsl:attribute>
			</AttributeType>
			<attribute>
			  <xsl:attribute name="type">ord</xsl:attribute>
			  <xsl:attribute name="sql:field">ord</xsl:attribute>
			</attribute>
		  </xsl:if>
		</ElementType>
	  </xsl:for-each>
	</Schema>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
props/basic
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template match="props/basic">
	<xsl:param name="SourceTable"/>
	<xsl:choose>
	  <!-- types that require a seperate element for this property -->
	  <xsl:when test="@sig[.='MultiString'] or  @sig[.='MultiText'] or @sig[.='MultiUnicode']">
		<xsl:variable name="JoinerTable">
		  <xsl:value-of select="../../@id"/>
		  <xsl:text>_</xsl:text>
		  <xsl:value-of select="@id"/>
		</xsl:variable>
		<element>
		  <xsl:attribute name="type"><xsl:value-of select="$JoinerTable"/></xsl:attribute>
		  <sql:relationship>
			<xsl:attribute name="key-relation"><xsl:value-of select="$SourceTable"/></xsl:attribute>
			<xsl:attribute name="key"><xsl:text>id</xsl:text></xsl:attribute>
			<xsl:attribute name="foreign-relation"><xsl:value-of select="$JoinerTable"/></xsl:attribute>
			<xsl:attribute name="foreign-key"><xsl:text>obj</xsl:text></xsl:attribute>
		  </sql:relationship>
		</element>
	  </xsl:when>
	  <!-- the remaining types can be handled by an attribute -->
	  <xsl:otherwise>
		<AttributeType>
		  <xsl:attribute name="name"><xsl:value-of select="@id"/></xsl:attribute>
		  <xsl:attribute name="dt:type"><xsl:choose><xsl:when test="@sig[.='Integer']">i4</xsl:when><xsl:when test="@sig[.='Boolean']">boolean</xsl:when><xsl:when test="@sig[.='String'] or @sig[.='String'] or @sig[.='Text'] or  @sig[.='Unicode']">string</xsl:when><!--We 		haven't accounted for all basic signature types yet but for now we will try and use string--><xsl:otherwise>string</xsl:otherwise></xsl:choose></xsl:attribute>
		</AttributeType>
		<attribute>
		  <xsl:attribute name="type"><xsl:value-of select="@id"/></xsl:attribute>
		  <xsl:choose>
			<!--				<xsl:when test="@sig[.='String'] or @sig[.='String'] or @sig[.='MultiString'] or @sig[.='Text'] or @sig[.='MultiText']"> -->
			<xsl:when test="@sig='MultiString' or @sig='MultiUnicode'">
			  <xsl:variable name="JoinerTable">
				<xsl:value-of select="../../@id"/>
				<xsl:text>_</xsl:text>
				<xsl:value-of select="@id"/>
			  </xsl:variable>
			  <xsl:attribute name="sql:relation"><xsl:value-of select="$JoinerTable"/></xsl:attribute>
			  <xsl:attribute name="sql:field"><xsl:text>txt</xsl:text></xsl:attribute>
			  <sql:relationship>
				<xsl:attribute name="key-relation"><xsl:value-of select="$SourceTable"/></xsl:attribute>
				<xsl:attribute name="key"><xsl:text>id</xsl:text></xsl:attribute>
				<xsl:attribute name="foreign-relation"><xsl:value-of select="$JoinerTable"/></xsl:attribute>
				<xsl:attribute name="foreign-key"><xsl:text>obj</xsl:text></xsl:attribute>
			  </sql:relationship>
			</xsl:when>
			<xsl:otherwise>
			  <xsl:attribute name="sql:field"><xsl:value-of select="@id"/></xsl:attribute>
			  <xsl:if test="../../@abstract='true'">
				<xsl:attribute name="sql:relation"><xsl:value-of select="../../@id"/></xsl:attribute>
				<sql:relationship>
				  <xsl:attribute name="key-relation"><xsl:value-of select="$SourceTable"/></xsl:attribute>
				  <xsl:attribute name="key">id</xsl:attribute>
				  <xsl:attribute name="foreign-relation"><xsl:value-of select="../../@id"/></xsl:attribute>
				  <xsl:attribute name="foreign-key">id</xsl:attribute>
				</sql:relationship>
			  </xsl:if>
			</xsl:otherwise>
		  </xsl:choose>
		</attribute>
	  </xsl:otherwise>
	</xsl:choose>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
props/rel
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template match="props/rel">
	<xsl:param name="SourceTable"/>
	<xsl:if test="not(@id='Rule' and @sig='CmObject')">
	  <!-- 2001.10.30 This ref attr doesn't have a real signature (yet); ignore it -->
	  <AttributeType>
		<xsl:attribute name="name"><xsl:value-of select="@id"/></xsl:attribute>
		<xsl:attribute name="dt:type">idref<xsl:if test="card!='atomic'">s</xsl:if></xsl:attribute>
	  </AttributeType>
	  <xsl:if test="@card='atomic'">
		<attribute>
		  <xsl:attribute name="type"><xsl:value-of select="@id"/></xsl:attribute>
		  <xsl:if test="../../@base='CmObject'">
			<!-- in superclass -->
			<xsl:attribute name="sql:relation"><xsl:value-of select="$SourceTable"/>_</xsl:attribute>
			<sql:relationship>
			  <xsl:attribute name="key-relation"><xsl:value-of select="$SourceTable"/></xsl:attribute>
			  <xsl:attribute name="key">Id</xsl:attribute>
			  <xsl:attribute name="foreign-relation"><xsl:value-of select="$SourceTable"/>_</xsl:attribute>
			  <xsl:attribute name="foreign-key">id</xsl:attribute>
			</sql:relationship>
		  </xsl:if>
		  <!-- 2001.10.26 following not needed (i.e. does not give correct output)
				<xsl:attribute name="sql:relation"><xsl:value-of select="@sig"/></xsl:attribute>
				<xsl:attribute name="sql:field">id</xsl:attribute>
				<xsl:if test="../../@base!='CmObject'"> - in subclass -
				<sql:relationship>
					<xsl:attribute name="key-relation"><xsl:value-of select="$SourceTable"/></xsl:attribute>
					<xsl:attribute name="key"><xsl:value-of select="@id"/></xsl:attribute>
					<xsl:attribute name="foreign-relation"><xsl:value-of select="@sig"/></xsl:attribute>
					<xsl:attribute name="foreign-key">id</xsl:attribute>
				</sql:relationship>
				</xsl:if>
				<xsl:if test="../../@base='CmObject'"> - in superclass -
				<sql:relationship>
					<xsl:attribute name="key-relation"><xsl:value-of select="$SourceTable"/></xsl:attribute>
					<xsl:attribute name="key">id</xsl:attribute>
					<xsl:attribute name="foreign-relation"><xsl:value-of select="../../@id"/></xsl:attribute>
					<xsl:attribute name="foreign-key">id</xsl:attribute>
				</sql:relationship>
				<sql:relationship>
					<xsl:attribute name="key-relation"><xsl:value-of select="../../@id"/></xsl:attribute>
					<xsl:attribute name="key"><xsl:value-of select="@id"/></xsl:attribute>
					<xsl:attribute name="foreign-relation"><xsl:value-of select="@sig"/></xsl:attribute>
					<xsl:attribute name="foreign-key">id</xsl:attribute>
				</sql:relationship>
				</xsl:if>
-->
		</attribute>
	  </xsl:if>
	  <xsl:if test="@card!='atomic'">
		<element>
		  <xsl:attribute name="type"><xsl:value-of select="../../@id"/>_<xsl:value-of select="@id"/></xsl:attribute>
		  <sql:relationship>
			<xsl:attribute name="key-relation"><xsl:value-of select="../../@id"/></xsl:attribute>
			<xsl:attribute name="key">id</xsl:attribute>
			<xsl:attribute name="foreign-relation"><xsl:value-of select="../../@id"/>_<xsl:value-of select="@id"/></xsl:attribute>
			<xsl:attribute name="foreign-key">src</xsl:attribute>
		  </sql:relationship>
		</element>
		<!-- 2001.10.31 not correct; using above
				<xsl:variable name="JoinerTable">
					<xsl:if test="../../@abstract='true'">
						<xsl:value-of select="../../@id"/>_<xsl:value-of select="@id"/>
					</xsl:if>
					<xsl:if test="../../@abstract='false'">
						<xsl:value-of select="$SourceTable"/>_<xsl:value-of select="@id"/>
					</xsl:if>
				</xsl:variable>
				<attribute>
					<xsl:attribute name="type"><xsl:value-of select="@id"/></xsl:attribute>
					<xsl:attribute name="sql:relation"><xsl:value-of select="@sig"/></xsl:attribute>
					<xsl:attribute name="sql:field">id</xsl:attribute>
					<sql:relationship>
						<xsl:attribute name="key-relation"><xsl:value-of select="$SourceTable"/></xsl:attribute>
						<xsl:attribute name="key">id</xsl:attribute>
						<xsl:attribute name="foreign-relation"><xsl:value-of select="$JoinerTable"/></xsl:attribute>
						<xsl:attribute name="foreign-key">src</xsl:attribute>
					</sql:relationship>
					<sql:relationship>
						<xsl:attribute name="key-relation"><xsl:value-of select="$JoinerTable"/></xsl:attribute>
						<xsl:attribute name="key">dst</xsl:attribute>
						<xsl:attribute name="foreign-relation"><xsl:value-of select="@sig"/></xsl:attribute>
						<xsl:attribute name="foreign-key">id</xsl:attribute>
					</sql:relationship>
				</attribute>
				-->
	  </xsl:if>
	</xsl:if>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
props/owning
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template match="props/owning">
	<xsl:param name="SubClassName"/>
	<xsl:param name="AugmentedName"/>
	<xsl:variable name="sig" select="@sig"/>
	<xsl:variable name="comboName">
	  <xsl:if test="$sig=$SubClassName">
		<xsl:value-of select="$AugmentedName"/>
	  </xsl:if>
	  <xsl:if test="$sig!=$SubClassName">
		<xsl:value-of select="$SubClassName"/>
	  </xsl:if>
	  <xsl:text>-</xsl:text>
	  <!-- 2001.10.26 was _ -->
	  <xsl:value-of select="@id"/>
	</xsl:variable>
	<xsl:if test="not(@id='FeatureSpecs' and @sig='FsFeatureSpecification' or @id='FeatureDisjunctions' and @sig='FsFeatStrucDisj')">
	  <!-- 2001.10.30 not supporting indirect recursion and these have it -->
	  <!--We look for the class that is the signature of the owning property (there will only be one)-->
	  <xsl:for-each select="//class[@id=$sig]">
		<element>
		  <xsl:attribute name="type"><xsl:value-of select="$comboName"/></xsl:attribute>
		</element>
	  </xsl:for-each>
	</xsl:if>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CreateClassPropertyElementType
	create <ElementType> for concrete_property combinations
		Parameters:	ClassName		= name of the class
					NameAugment	=
					NextAugment	=
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="CreateClassPropertyElementType">
	<xsl:param name="ClassName"/>
	<xsl:param name="NameAugment"/>
	<xsl:param name="NextAugment"/>
	<xsl:variable name="AugmentedName">
	  <xsl:value-of select="$ClassName"/>
	  <xsl:value-of select="$NameAugment"/>
	</xsl:variable>
	<xsl:variable name="sig" select="@sig"/>
	<xsl:variable name="ElementName">
	  <xsl:value-of select="@sig"/>
	  <xsl:value-of select="$NextAugment"/>
	</xsl:variable>
	<xsl:variable name="ClassPropertyName">
	  <xsl:value-of select="$AugmentedName"/>-<xsl:value-of select="@id"/>
	  <!-- 2001.10.26 was _ -->
	</xsl:variable>
	<xsl:variable name="ViewName">
	  <xsl:value-of select="$ClassName"/>_<xsl:value-of select="@id"/>
	</xsl:variable>
	<!--		<xsl:if test="../../@abstract='false'"> -->
	<ElementType content="mixed" model="closed" order="many">
	  <xsl:attribute name="name"><xsl:value-of select="$ClassPropertyName"/></xsl:attribute>
	  <xsl:attribute name="sql:is-constant">1</xsl:attribute>
	  <xsl:for-each select="//class[@id=$sig]">
		<xsl:if test="@abstract='false'">
		  <xsl:if test="$NameAugment='' or $NameAugment!='' and $NextAugment!=''">
			<element>
			  <xsl:attribute name="type"><xsl:value-of select="$ElementName"/></xsl:attribute>
			  <xsl:call-template name="CreateSqlRelationships">
				<xsl:with-param name="SourceTable" select="$ClassName"/>
				<xsl:with-param name="JoinerTable" select="$ViewName"/>
				<xsl:with-param name="TargetTable" select="$sig"/>
			  </xsl:call-template>
			</element>
		  </xsl:if>
		</xsl:if>
		<xsl:if test="@abstract='true'">
		  <xsl:call-template name="GetSubClassesOfAnOwnedAbstractClass">
			<xsl:with-param name="SourceTable" select="$ClassName"/>
			<xsl:with-param name="JoinerTable" select="$ViewName"/>
		  </xsl:call-template>
		</xsl:if>
	  </xsl:for-each>
	</ElementType>
	<!-- 2001.10.31 above applies to all classes
		</xsl:if>
		<xsl:if test="../../@abstract='true'">
			<xsl:variable name="AttrName" select="@id"/>
			<xsl:for-each select="../..">
				<xsl:call-template name="GetSubClassesOfAnOwningAbstractClass">
					<xsl:with-param name="AttrName" select="$AttrName"/>
					<xsl:with-param name="JoinerTable" select="$ViewName"/>
					<xsl:with-param name="TargetTable" select="$sig"/>
					<xsl:with-param name="NameAugment" select="$NameAugment"/>
				</xsl:call-template>
			</xsl:for-each>
		</xsl:if>
		-->
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CreateRealElementType
	create <ElementType> for real, concrete classes
		Parameters:	ElementName	= name of the element
					NameAugment	=
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="CreateRealElementType">
	<xsl:param name="ElementName"/>
	<xsl:param name="NameAugment"/>
	<xsl:variable name="AugmentedName">
	  <xsl:value-of select="$ElementName"/>
	  <xsl:value-of select="$NameAugment"/>
	</xsl:variable>
	<ElementType content="mixed" model="closed" order="many">
	  <xsl:attribute name="name"><xsl:value-of select="$AugmentedName"/></xsl:attribute>
	  <xsl:if test="$NameAugment!=''">
		<xsl:attribute name="sql:relation"><xsl:value-of select="$ElementName"/></xsl:attribute>
	  </xsl:if>
	  <!-- Add attributes from CmObject -->
	  <AttributeType>
		<xsl:attribute name="name">Id</xsl:attribute>
		<xsl:attribute name="dt:type">id</xsl:attribute>
	  </AttributeType>
	  <attribute>
		<xsl:attribute name="type">Id</xsl:attribute>
		<xsl:attribute name="sql:field">Id</xsl:attribute>
	  </attribute>
	  <!-- process properties -->
	  <xsl:apply-templates select="props/basic">
		<xsl:with-param name="SourceTable" select="$ElementName"/>
	  </xsl:apply-templates>
	  <xsl:apply-templates select="props/rel">
		<xsl:with-param name="SourceTable" select="$ElementName"/>
	  </xsl:apply-templates>
	  <xsl:apply-templates select="props/owning">
		<xsl:with-param name="SubClassName" select="$ElementName"/>
		<xsl:with-param name="AugmentedName" select="$AugmentedName"/>
	  </xsl:apply-templates>
	  <!-- 2001.10.31 had:
			<xsl:call-template name="GetAttributesOfSuperClass">
				<xsl:with-param name="OrigClassName" select="$ElementName"/>
			</xsl:call-template>
-->
	  <!-- add an element for the super class -->
	  <xsl:if test="@base!='CmObject'">
		<element>
		  <xsl:attribute name="type"><xsl:value-of select="@base"/></xsl:attribute>
		  <sql:relationship>
			<xsl:attribute name="key-relation"><xsl:value-of select="$ElementName"/></xsl:attribute>
			<xsl:attribute name="key">id</xsl:attribute>
			<xsl:attribute name="foreign-relation"><xsl:value-of select="@base"/></xsl:attribute>
			<xsl:attribute name="foreign-key">id</xsl:attribute>
		  </sql:relationship>
		</element>
	  </xsl:if>
	</ElementType>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
CreateSqlRelationships
	create <sql:relationship> elements for a joiner table
		Parameters:	SourceTable	= name of source table
					JoinerTable		= name of joiner table
					TargetTable		= name of the targer table
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="CreateSqlRelationships">
	<xsl:param name="SourceTable"/>
	<xsl:param name="JoinerTable"/>
	<xsl:param name="TargetTable"/>
	<sql:relationship>
	  <xsl:attribute name="key-relation"><xsl:value-of select="$SourceTable"/></xsl:attribute>
	  <xsl:attribute name="key">id</xsl:attribute>
	  <xsl:attribute name="foreign-relation"><xsl:value-of select="$JoinerTable"/></xsl:attribute>
	  <xsl:attribute name="foreign-key">src</xsl:attribute>
	</sql:relationship>
	<sql:relationship>
	  <xsl:attribute name="key-relation"><xsl:value-of select="$JoinerTable"/></xsl:attribute>
	  <xsl:attribute name="key">dst</xsl:attribute>
	  <xsl:attribute name="foreign-relation"><xsl:value-of select="$TargetTable"/></xsl:attribute>
	  <xsl:attribute name="foreign-key">id</xsl:attribute>
	</sql:relationship>
  </xsl:template>
  <!--
				 GetAttributesOfSuperClass

	process all attributes of the super class
	-->
  <!-- 2001.10.31 not needed here
	<xsl:template name="GetAttributesOfSuperClass">
		<xsl:param name="OrigClassName"/>
		<xsl:variable name="SubClassNameVar" select="@id"/>
		<xsl:choose>
			<xsl:when test="@base='CmObject'">
				-Here we may need to add custom capabilities to each class-
			</xsl:when>
			<xsl:otherwise>
				<xsl:variable name="SuperClassBase" select="@base"/>
				<xsl:for-each select="//class[@id=$SuperClassBase and @abstract='true']">
					- only needs to apply to abstract superclasses -
					<xsl:apply-templates select="props/basic">
						<xsl:with-param name="SourceTable" select="$OrigClassName"/>
					</xsl:apply-templates>
					<xsl:apply-templates select="props/rel">
						<xsl:with-param name="SourceTable" select="$OrigClassName"/>
					</xsl:apply-templates>
					<xsl:apply-templates select="props/owning">
						<xsl:with-param name="SubClassName" select="$OrigClassName"/>
					</xsl:apply-templates>
					<xsl:call-template name="GetAttributesOfSuperClass">
						<xsl:with-param name="OrigClassName" select="$OrigClassName"/>
					</xsl:call-template>
				</xsl:for-each>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	-->
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
GetSubClassesOfAnOwningAbstractClass
	get all sub classes; create their ElementType (when an abstract class has an owning property)
		Parameters:	AttrName		= name of the attribute
					JoinerTable		= name of joiner table
					TargetTable		= name of the targer table
					NameAugment	=
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="GetSubClassesOfAnOwningAbstractClass">
	<xsl:param name="AttrName"/>
	<xsl:param name="JoinerTable"/>
	<xsl:param name="TargetTable"/>
	<xsl:param name="NameAugment"/>
	<xsl:variable name="AugmentedName">
	  <xsl:value-of select="@id"/>
	  <xsl:value-of select="$NameAugment"/>
	</xsl:variable>
	<xsl:variable name="classID" select="@id"/>
	<xsl:if test="@abstract='false'">
	  <ElementType content="mixed" model="closed" order="many">
		<xsl:attribute name="name"><xsl:value-of select="$AugmentedName"/>-<xsl:value-of select="$AttrName"/></xsl:attribute>
		<!-- 2001.10.26 was _ -->
		<xsl:attribute name="sql:is-constant">1</xsl:attribute>
		<xsl:variable name="ElementClass" select="//class[@id=$TargetTable]"/>
		<xsl:if test="$ElementClass/@abstract='false'">
		  <element>
			<xsl:attribute name="type"><xsl:value-of select="$TargetTable"/></xsl:attribute>
			<xsl:call-template name="CreateSqlRelationships">
			  <xsl:with-param name="SourceTable" select="$classID"/>
			  <xsl:with-param name="JoinerTable" select="$JoinerTable"/>
			  <xsl:with-param name="TargetTable" select="$TargetTable"/>
			</xsl:call-template>
		  </element>
		</xsl:if>
		<xsl:if test="$ElementClass/@abstract='true'">
		  <xsl:for-each select="$ElementClass">
			<xsl:call-template name="GetSubClassesOfAnOwnedAbstractClass">
			  <xsl:with-param name="SourceTable" select="$classID"/>
			  <xsl:with-param name="JoinerTable" select="$JoinerTable"/>
			</xsl:call-template>
		  </xsl:for-each>
		</xsl:if>
	  </ElementType>
	</xsl:if>
	<xsl:for-each select="//class[@base=$classID]">
	  <xsl:call-template name="GetSubClassesOfAnOwningAbstractClass">
		<xsl:with-param name="AttrName" select="$AttrName"/>
		<xsl:with-param name="JoinerTable" select="$JoinerTable"/>
		<xsl:with-param name="TargetTable" select="$TargetTable"/>
		<xsl:with-param name="NameAugment" select="$NameAugment"/>
	  </xsl:call-template>
	</xsl:for-each>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
GetSubClassesOfAnOwnedAbstractClass
	get all concrete sub classes and create their <element> (when some class owns an abstract class)
		Parameters:	SourceTable	= name of the source table
					JoinerTable		= name of joiner table
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="GetSubClassesOfAnOwnedAbstractClass">
	<xsl:param name="SourceTable"/>
	<xsl:param name="JoinerTable"/>
	<xsl:variable name="classID" select="@id"/>
	<xsl:if test="@abstract='false'">
	  <element>
		<xsl:attribute name="type"><xsl:value-of select="$classID"/></xsl:attribute>
		<xsl:call-template name="CreateSqlRelationships">
		  <xsl:with-param name="SourceTable" select="$SourceTable"/>
		  <xsl:with-param name="JoinerTable" select="$JoinerTable"/>
		  <xsl:with-param name="TargetTable" select="$classID"/>
		</xsl:call-template>
	  </element>
	</xsl:if>
	<xsl:for-each select="//class[@base=$classID]">
	  <xsl:call-template name="GetSubClassesOfAnOwnedAbstractClass">
		<xsl:with-param name="SourceTable" select="$SourceTable"/>
		<xsl:with-param name="JoinerTable" select="$JoinerTable"/>
	  </xsl:call-template>
	</xsl:for-each>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ProcessClassPropertyElementTypesWithRecursion
	create augmented element_property combinations to handle direct recursion in model
		Parameters:	sList			= recusion depth list of strings
					ClassName		= name of class
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="ProcessClassPropertyElementTypesWithRecursion">
	<xsl:param name="sList"/>
	<xsl:param name="ClassName"/>
	<xsl:variable name="sNewList" select="concat(normalize-space($sList),' ')"/>
	<xsl:variable name="sFirst" select="substring-before($sNewList,' ')"/>
	<xsl:variable name="sRest" select="substring-after($sNewList,' ')"/>
	<xsl:variable name="sSecond" select="substring-before($sRest,' ')"/>
	<xsl:call-template name="CreateClassPropertyElementType">
	  <xsl:with-param name="ClassName" select="$ClassName"/>
	  <xsl:with-param name="NameAugment" select="$sFirst"/>
	  <xsl:with-param name="NextAugment" select="$sSecond"/>
	</xsl:call-template>
	<xsl:if test="$sRest">
	  <xsl:call-template name="ProcessClassPropertyElementTypesWithRecursion">
		<xsl:with-param name="sList" select="$sRest"/>
		<xsl:with-param name="ClassName" select="$ClassName"/>
	  </xsl:call-template>
	</xsl:if>
  </xsl:template>
  <!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
ProcessRealElementTypesWithRecursion
	create augmented real elements to handle recursion in model
		Parameters:	sList			= recusion depth list of strings
					ElementName	= name of element
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
  <xsl:template name="ProcessRealElementTypesWithRecursion">
	<xsl:param name="sList"/>
	<xsl:param name="ElementName"/>
	<xsl:variable name="sNewList" select="concat(normalize-space($sList),' ')"/>
	<xsl:variable name="sFirst" select="substring-before($sNewList,' ')"/>
	<xsl:variable name="sRest" select="substring-after($sNewList,' ')"/>
	<xsl:call-template name="CreateRealElementType">
	  <xsl:with-param name="ElementName" select="$ElementName"/>
	  <xsl:with-param name="NameAugment" select="$sFirst"/>
	</xsl:call-template>
	<xsl:if test="$sRest">
	  <xsl:call-template name="ProcessRealElementTypesWithRecursion">
		<xsl:with-param name="sList" select="$sRest"/>
		<xsl:with-param name="ElementName" select="$ElementName"/>
	  </xsl:call-template>
	</xsl:if>
  </xsl:template>
</xsl:stylesheet>
<!--
================================================================
Revision History
- - - - - - - - - - - - - - - - - - -
05-Feb-2002	Andy Black		Added comments
31-Oct-2001	Andy Black		Refined
				John Hatton
28-Sep-2001	Andy Black		Reworked
12-Apr-2001	John Hatton		Initial Draft
				Larry Hayashi
================================================================
 -->
