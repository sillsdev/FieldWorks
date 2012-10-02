<?xml version="1.0" encoding="UTF-8"?>
<!-- edited with XML Spy v4.1 U (http://www.xmlspy.com) by Larr Hayashi (private) -->
<!--Editing history:
3 Oct 2001 LSH Changed Run enc attribute to be REQUIRED rather than IMPLIED.
-->
<!--This stylesheet converts the simple EntireModelCellar3.xml to a FieldWorksSchema for validating data
NOTE:
MultiString -> seq of AStr
MultiUnicode -> seq of AUni
String -> Str
Unicode -> Uni

PROBLEMS:

One thing we have to calculate is the concrete subclasses of superclasses for the possible signatures of an attribute.
This means finding all the classes - where abstract is not false - that have a base equal to the signature of the attribute.
We should first check to see if the signature is abstract or not. If it is, then check its subclasses.

Need to define CustomField as an element and all the objects that it can be a part of (which as far as I know is all classes as enumeration value) and all those objects that can be targets.


-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:fo="http://www.w3.org/1999/XSL/Format" xmlns:xs="http://www.w3.org/2001/XMLSchema">
	<xsl:output method="xml" version="1.0" encoding="UTF-8"/>
	<xsl:template match="/">
		<xs:schema>
			<xsl:attribute name="elementFormDefault">qualified</xsl:attribute>
			<xsl:apply-templates/>
		</xs:schema>
	</xsl:template>
	<xsl:template match="EntireModel">
		<!--These elements are basic signatures and not generated off of the model. Run is included here as well.-->
		<xs:element name="FwDatabase">
			<xs:complexType>
				<xs:choice minOccurs="0" maxOccurs="unbounded">
					<xs:element ref="AdditionalFields"/>
					<xs:element ref="LangProject"/>
					<xs:element ref="LgWritingSystem"/>
					<xs:element ref="ReversalIndex"/>
					<xs:element ref="UserView"/>
					<xs:element ref="ScrRefSystem"/>
					<xs:element ref="CmPossibilityList"/>
					<xs:element ref="CmPicture"/>
				</xs:choice>
				<xs:attribute name="version" type="xs:string" use="optional"/>
			</xs:complexType>
		</xs:element>
		<xs:element name="AdditionalFields">
			<xs:complexType>
				<xs:sequence minOccurs="0" maxOccurs="unbounded">
					<xs:element ref="CustomField"/>
				</xs:sequence>
			</xs:complexType>
		</xs:element>
		<xs:element name="Run">
			<xs:complexType>
				<xs:simpleContent>
					<xs:extension base="xs:string">
						<xs:attribute name="type">
							<xs:simpleType>
								<xs:restriction base="xs:NMTOKEN">
									<xs:enumeration value="chars"/>
									<xs:enumeration value="picture"/>
								</xs:restriction>
							</xs:simpleType>
						</xs:attribute>
						<xs:attribute name="ownlink" type="xs:string"/>
						<xs:attribute name="contextString" type="xs:string"/>
						<xs:attribute name="backcolor" type="xs:string"/>
						<xs:attribute name="bold">
							<xs:simpleType>
								<xs:restriction base="xs:NMTOKEN">
									<xs:enumeration value="off"/>
									<xs:enumeration value="on"/>
									<xs:enumeration value="invert"/>
								</xs:restriction>
							</xs:simpleType>
						</xs:attribute>
						<xs:attribute name="ws" type="xs:string" use="required"/>
						<xs:attribute name="wsBase" type="xs:string"/>
						<xs:attribute name="externalLink" type="xs:string"/>
						<xs:attribute name="fontFamily" type="xs:string"/>
						<xs:attribute name="fontsize" type="xs:string"/>
						<xs:attribute name="fontsizeUnit" type="xs:string"/>
						<xs:attribute name="forecolor" type="xs:string"/>
						<xs:attribute name="italic">
							<xs:simpleType>
								<xs:restriction base="xs:NMTOKEN">
									<xs:enumeration value="off"/>
									<xs:enumeration value="on"/>
									<xs:enumeration value="invert"/>
								</xs:restriction>
							</xs:simpleType>
						</xs:attribute>
						<xs:attribute name="link" type="xs:string"/>
						<xs:attribute name="namedStyle" type="xs:string"/>
						<xs:attribute name="offset" type="xs:string"/>
						<xs:attribute name="offsetUnit" type="xs:string"/>
						<xs:attribute name="superscript">
							<xs:simpleType>
								<xs:restriction base="xs:NMTOKEN">
									<xs:enumeration value="super"/>
									<xs:enumeration value="sub"/>
								</xs:restriction>
							</xs:simpleType>
						</xs:attribute>
						<xs:attribute name="tabList" type="xs:string"/>
						<xs:attribute name="tags" type="xs:IDREFS"/>
						<xs:attribute name="undercolor" type="xs:string"/>
						<xs:attribute name="underline">
							<xs:simpleType>
								<xs:restriction base="xs:NMTOKEN">
									<xs:enumeration value="none"/>
									<xs:enumeration value="single"/>
									<xs:enumeration value="double"/>
									<xs:enumeration value="dotted"/>
									<xs:enumeration value="dashed"/>
									<xs:enumeration value="squiggle"/>
									<xs:enumeration value="strikethrough"/>
								</xs:restriction>
							</xs:simpleType>
						</xs:attribute>
					</xs:extension>
				</xs:simpleContent>
			</xs:complexType>
		</xs:element>
		<xs:element name="AStr">
			<xs:complexType>
				<xs:sequence minOccurs="0" maxOccurs="unbounded">
					<xs:element ref="Run"/>
				</xs:sequence>
				<xs:attribute name="ws" type="xs:string" use="required"/>
			</xs:complexType>
		</xs:element>
		<xs:element name="AUni">
			<xs:complexType>
				<xs:simpleContent>
					<xs:extension base="xs:string">
						<xs:attribute name="ws" type="xs:string" use="required"/>
					</xs:extension>
				</xs:simpleContent>
			</xs:complexType>
		</xs:element>
		<xs:element name="Binary" type="xs:string"/>
		<xs:element name="Numeric">
			<xs:complexType>
				<xs:attribute name="val" type="xs:string" use="required"/>
			</xs:complexType>
		</xs:element>
		<xs:element name="Float">
			<xs:complexType>
				<xs:attribute name="val" type="xs:string" use="required"/>
				<xs:attribute name="bin" type="xs:string"/>
			</xs:complexType>
		</xs:element>
		<xs:element name="Image" type="xs:string"/>
		<xs:element name="Boolean">
			<xs:complexType>
				<xs:attribute name="val" use="required">
					<xs:simpleType>
						<xs:restriction base="xs:NMTOKEN">
							<xs:enumeration value="true"/>
							<xs:enumeration value="false"/>
						</xs:restriction>
					</xs:simpleType>
				</xs:attribute>
			</xs:complexType>
		</xs:element>
		<xs:element name="GenDate">
			<xs:complexType>
				<xs:attribute name="val" type="xs:string" use="required"/>
			</xs:complexType>
		</xs:element>
		<xs:element name="Guid">
			<xs:complexType>
				<xs:attribute name="val" type="xs:string" use="required"/>
			</xs:complexType>
		</xs:element>
		<xs:element name="Integer">
			<xs:complexType>
				<xs:attribute name="val" type="xs:string" use="required"/>
				<xs:attribute name="str" type="xs:string" use="optional"/>
				<xsl:comment>The attribute str exists for the following reasons - 1. to make it possible for a user to specify the ethnologue code in the XML file for import rather than having to calculate the integer value for LgWritingSystem and 2. for human readability in the XML export whenever we calculate the integer value off of some other value.</xsl:comment>
			</xs:complexType>
		</xs:element>
		<xs:element name="Link">
			<xs:complexType>
				<xs:attribute name="target" type="xs:IDREF" use="implied"/>
				<xs:attribute name="class" type="xs:string"/>
				<xs:attribute name="ord" type="xs:string"/>
				<xs:attribute name="db" type="xs:string"/>
				<xs:attribute name="ws" type="xs:string"/>
				<xs:attribute name="form" type="xs:string"/>
				<xs:attribute name="abbr" type="xs:string"/>
				<xs:attribute name="abbrOwner" type="xs:string"/>
				<xs:attribute name="name" type="xs:string"/>
				<xs:attribute name="namev" type="xs:string"/>
				<xs:attribute name="nameOwner" type="xs:string"/>
				<xs:attribute name="wsa" type="xs:string"/>
				<xs:attribute name="wsv" type="xs:string"/>
				<xs:attribute name="entry" type="xs:string"/>
				<xs:attribute name="sense" type="xs:string"/>
				<xs:attribute name="path" type="xs:string"/>
			</xs:complexType>
		</xs:element>
		<xs:element name="Str">
			<xs:complexType>
				<xs:sequence minOccurs="0" maxOccurs="unbounded">
					<xs:element ref="Run"/>
				</xs:sequence>
			</xs:complexType>
		</xs:element>
		<xs:element name="Time">
			<xs:complexType>
				<xs:attribute name="val" type="xs:string" use="required"/>
			</xs:complexType>
		</xs:element>
		<xs:element name="Uni" type="xs:string"/>
		<!--The following accounts for how we are symbolically representing style rules. See S. McConnel if questions-->
		<xs:element name="BulNumFontInfo">
			<xs:complexType>
				<xs:attribute name="backcolor" type="xs:string"/>
				<xs:attribute name="bold">
					<xs:simpleType>
						<xs:restriction base="xs:NMTOKEN">
							<xs:enumeration value="on"/>
							<xs:enumeration value="off"/>
							<xs:enumeration value="invert"/>
						</xs:restriction>
					</xs:simpleType>
				</xs:attribute>
				<xs:attribute name="fontsize" type="xs:string"/>
				<xs:attribute name="forecolor" type="xs:string"/>
				<xs:attribute name="italic">
					<xs:simpleType>
						<xs:restriction base="xs:NMTOKEN">
							<xs:enumeration value="on"/>
							<xs:enumeration value="off"/>
							<xs:enumeration value="invert"/>
						</xs:restriction>
					</xs:simpleType>
				</xs:attribute>
				<xs:attribute name="offset" type="xs:string"/>
				<xs:attribute name="superscript">
					<xs:simpleType>
						<xs:restriction base="xs:NMTOKEN">
							<xs:enumeration value="off"/>
							<xs:enumeration value="sub"/>
							<xs:enumeration value="super"/>
						</xs:restriction>
					</xs:simpleType>
				</xs:attribute>
				<xs:attribute name="undercolor" type="xs:string"/>
				<xs:attribute name="underline">
					<xs:simpleType>
						<xs:restriction base="xs:NMTOKEN">
							<xs:enumeration value="none"/>
							<xs:enumeration value="dotted"/>
							<xs:enumeration value="dashed"/>
							<xs:enumeration value="single"/>
							<xs:enumeration value="double"/>
							<xs:enumeration value="squiggle"/>
							<xs:enumeration value="strikethrough"/>
						</xs:restriction>
					</xs:simpleType>
				</xs:attribute>
				<xs:attribute name="fontFamily" type="xs:string"/>
			</xs:complexType>
		</xs:element>
		<xs:element name="WsProp">
			<xs:complexType>
				<xs:attribute name="ws" type="xs:string" use="required"/>
				<xs:attribute name="backcolor" type="xs:string"/>
				<xs:attribute name="bold">
					<xs:simpleType>
						<xs:restriction base="xs:NMTOKEN">
							<xs:enumeration value="invert"/>
							<xs:enumeration value="off"/>
							<xs:enumeration value="on"/>
						</xs:restriction>
					</xs:simpleType>
				</xs:attribute>
				<xs:attribute name="fontFamily" type="xs:string"/>
				<xs:attribute name="fontsize" type="xs:string"/>
				<xs:attribute name="fontsizeUnit">
					<xs:simpleType>
						<xs:restriction base="xs:NMTOKEN">
							<xs:enumeration value="mpt"/>
							<xs:enumeration value="rel"/>
						</xs:restriction>
					</xs:simpleType>
				</xs:attribute>
				<xs:attribute name="forecolor" type="xs:string"/>
				<xs:attribute name="italic">
					<xs:simpleType>
						<xs:restriction base="xs:NMTOKEN">
							<xs:enumeration value="invert"/>
							<xs:enumeration value="off"/>
							<xs:enumeration value="on"/>
						</xs:restriction>
					</xs:simpleType>
				</xs:attribute>
				<xs:attribute name="offset" type="xs:string"/>
				<xs:attribute name="offsetUnit">
					<xs:simpleType>
						<xs:restriction base="xs:NMTOKEN">
							<xs:enumeration value="mpt"/>
							<xs:enumeration value="rel"/>
						</xs:restriction>
					</xs:simpleType>
				</xs:attribute>
				<xs:attribute name="superscript">
					<xs:simpleType>
						<xs:restriction base="xs:NMTOKEN">
							<xs:enumeration value="off"/>
							<xs:enumeration value="sub"/>
							<xs:enumeration value="super"/>
						</xs:restriction>
					</xs:simpleType>
				</xs:attribute>
				<xs:attribute name="undercolor" type="xs:string"/>
				<xs:attribute name="underline">
					<xs:simpleType>
						<xs:restriction base="xs:NMTOKEN">
							<xs:enumeration value="dashed"/>
							<xs:enumeration value="dotted"/>
							<xs:enumeration value="double"/>
							<xs:enumeration value="none"/>
							<xs:enumeration value="single"/>
							<xs:enumeration value="squiggle"/>
							<xs:enumeration value="strikethrough"/>
						</xs:restriction>
					</xs:simpleType>
				</xs:attribute>
			</xs:complexType>
		</xs:element>
		<xs:element name="WsStyles9999">
			<xs:complexType>
				<xs:sequence minOccurs="0" maxOccurs="unbounded">
					<xs:element ref="WsProp"/>
				</xs:sequence>
			</xs:complexType>
		</xs:element>
		<xs:element name="Prop">
			<xs:complexType>
				<xs:choice minOccurs="0" maxOccurs="unbounded">
					<xs:element ref="BulNumFontInfo"/>
					<xs:element ref="WsStyles9999"/>
				</xs:choice>
				<xs:attribute name="align" type="xs:string"/>
				<xs:attribute name="backcolor" type="xs:string"/>
				<xs:attribute name="bold">
					<xs:simpleType>
						<xs:restriction base="xs:NMTOKEN">
							<xs:enumeration value="invert"/>
							<xs:enumeration value="off"/>
							<xs:enumeration value="on"/>
						</xs:restriction>
					</xs:simpleType>
				</xs:attribute>
				<xs:attribute name="borderBottom" type="xs:string"/>
				<xs:attribute name="borderColor" type="xs:string"/>
				<xs:attribute name="borderLeading" type="xs:string"/>
				<xs:attribute name="borderTop" type="xs:string"/>
				<xs:attribute name="borderTrailing" type="xs:string"/>
				<xs:attribute name="bulNumScheme" type="xs:string"/>
				<xs:attribute name="bulNumStartAt" type="xs:string"/>
				<xs:attribute name="bulNumTxtAft" type="xs:string"/>
				<xs:attribute name="bulNumTxtBef" type="xs:string"/>
				<xs:attribute name="charStyle" type="xs:string"/>
				<xs:attribute name="firstIndent" type="xs:string"/>
				<xs:attribute name="fontFamily" type="xs:string"/>
				<xs:attribute name="fontsize" type="xs:string"/>
				<xs:attribute name="fontsizeUnit">
					<xs:simpleType>
						<xs:restriction base="xs:NMTOKEN">
							<xs:enumeration value="mpt"/>
							<xs:enumeration value="rel"/>
						</xs:restriction>
					</xs:simpleType>
				</xs:attribute>
				<xs:attribute name="forecolor" type="xs:string"/>
				<xs:attribute name="italic">
					<xs:simpleType>
						<xs:restriction base="xs:NMTOKEN">
							<xs:enumeration value="invert"/>
							<xs:enumeration value="off"/>
							<xs:enumeration value="on"/>
						</xs:restriction>
					</xs:simpleType>
				</xs:attribute>
				<xs:attribute name="keepTogether" type="xs:string"/>
				<xs:attribute name="keepWithNext" type="xs:string"/>
				<xs:attribute name="leadingIndent" type="xs:string"/>
				<xs:attribute name="lineHeight" type="xs:string"/>
				<xs:attribute name="lineHeightUnit">
					<xs:simpleType>
						<xs:restriction base="xs:NMTOKEN">
							<xs:enumeration value="mpt"/>
							<xs:enumeration value="rel"/>
						</xs:restriction>
					</xs:simpleType>
				</xs:attribute>
				<xs:attribute name="lineHeightType">
					<xs:simpleType>
						<xs:restriction base="xs:NMTOKEN">
							<xs:enumeration value="atLeast"/>
							<xs:enumeration value="exact"/>
						</xs:restriction>
					</xs:simpleType>
				</xs:attribute>
				<xs:attribute name="marginTop" type="xs:string"/>
				<xs:attribute name="namedStyle" type="xs:string"/>
				<xs:attribute name="offset" type="xs:string"/>
				<xs:attribute name="offsetUnit">
					<xs:simpleType>
						<xs:restriction base="xs:NMTOKEN">
							<xs:enumeration value="mpt"/>
							<xs:enumeration value="rel"/>
						</xs:restriction>
					</xs:simpleType>
				</xs:attribute>
				<xs:attribute name="padBottom" type="xs:string"/>
				<xs:attribute name="padLeading" type="xs:string"/>
				<xs:attribute name="padTop" type="xs:string"/>
				<xs:attribute name="padTrailing" type="xs:string"/>
				<xs:attribute name="paracolor" type="xs:string"/>
				<xs:attribute name="rightToLeft" type="xs:string"/>
				<xs:attribute name="spaceAfter" type="xs:string"/>
				<xs:attribute name="spaceBefore" type="xs:string"/>
				<xs:attribute name="spellcheck" type="xs:string"/>
				<xs:attribute name="superscript">
					<xs:simpleType>
						<xs:restriction base="xs:NMTOKEN">
							<xs:enumeration value="off"/>
							<xs:enumeration value="sub"/>
							<xs:enumeration value="super"/>
						</xs:restriction>
					</xs:simpleType>
				</xs:attribute>
				<xs:attribute name="tabDef" type="xs:string"/>
				<xs:attribute name="trailingIndent" type="xs:string"/>
				<xs:attribute name="undercolor" type="xs:string"/>
				<xs:attribute name="underline">
					<xs:simpleType>
						<xs:restriction base="xs:NMTOKEN">
							<xs:enumeration value="dashed"/>
							<xs:enumeration value="dotted"/>
							<xs:enumeration value="double"/>
							<xs:enumeration value="none"/>
							<xs:enumeration value="single"/>
							<xs:enumeration value="squiggle"/>
							<xs:enumeration value="strikethrough"/>
						</xs:restriction>
					</xs:simpleType>
				</xs:attribute>
				<xs:attribute name="widowOrphan" type="xs:string"/>
				<xs:attribute name="ws" type="xs:string"/>
				<xs:attribute name="wsBase" type="xs:string"/>
			</xs:complexType>
		</xs:element>
		<xs:element name="LexicalRelations5016">
			<xs:complexType>
				<xs:choice minOccurs="0">
					<xs:element ref="Link"/>
				</xs:choice>
			</xs:complexType>
		</xs:element>
		<xs:element name="CrossReferences5002">
			<xs:complexType>
				<xs:choice minOccurs="0">
					<xs:element ref="Link"/>
				</xs:choice>
			</xs:complexType>
		</xs:element>
		<!--Custom field information goes here in these next Custom elements-->
		<!-- This WILL MOST ASSUREDLY CHANGE WHEN WE REFINE CUSTOM-->
		<xs:element name="Custom">
			<xs:complexType>
				<xs:choice minOccurs="0">
					<xs:element ref="Uni"/>
					<xs:element ref="Str"/>
					<xs:element ref="Boolean"/>
					<xs:element ref="Integer"/>
					<xs:element ref="Float"/>
					<xs:element ref="Time"/>
					<xs:element ref="Image"/>
					<xs:element ref="Guid"/>
					<xs:element ref="Binary"/>
					<xs:element ref="GenDate"/>
					<xs:element ref="Numeric"/>
				</xs:choice>
				<xs:attribute name="name" type="xs:string" use="required"/>
			</xs:complexType>
		</xs:element>
		<xs:element name="CustomField">
			<xs:complexType>
				<xs:attribute name="class" use="required">
					<xs:simpleType>
						<xs:restriction base="xs:NMTOKEN">
							<xsl:for-each select="//class">
								<xsl:sort select="@id"/>
								<xs:enumeration>
									<xsl:attribute name="value"><xsl:value-of select="@id"/></xsl:attribute>
								</xs:enumeration>
							</xsl:for-each>
						</xs:restriction>
					</xs:simpleType>
				</xs:attribute>
				<xs:attribute name="name" type="xs:string" use="required"/>
				<xs:attribute name="userLabel" type="xs:string" use="required"/>
				<xs:attribute name="helpString" type="xs:string" use="implied"/>
				<xs:attribute name="wsSelector" type="xs:string" use="required"/>
				<xs:attribute name="flid" type="xs:string" use="required"/>
				<xs:attribute name="big" type="xs:string" use="required"/>
				<!--This needs to be manually tweaked whenever we change types of basic signatures-->
				<xs:attribute name="type" use="required">
					<xs:simpleType>
						<xs:restriction base="xs:NMTOKEN">
							<xs:enumeration value="Boolean"/>
							<xs:enumeration value="Integer"/>
							<xs:enumeration value="Numeric"/>
							<xs:enumeration value="Float"/>
							<xs:enumeration value="Time"/>
							<xs:enumeration value="Guid"/>
							<xs:enumeration value="Image"/>
							<xs:enumeration value="GenDate"/>
							<xs:enumeration value="Binary"/>
							<xs:enumeration value="String"/>
							<xs:enumeration value="MultiString"/>
							<xs:enumeration value="Unicode"/>
							<xs:enumeration value="MultiUnicode"/>
							<xs:enumeration value="BigString"/>
							<xs:enumeration value="MultiBigString"/>
							<xs:enumeration value="BigUnicode"/>
							<xs:enumeration value="MultiBigUnicode"/>
							<xs:enumeration value="OwningAtom"/>
							<xs:enumeration value="ReferenceAtom"/>
							<xs:enumeration value="OwningCollection"/>
							<xs:enumeration value="ReferenceCollection"/>
							<xs:enumeration value="OwningSequence"/>
							<xs:enumeration value="ReferenceSequence"/>
						</xs:restriction>
					</xs:simpleType>
				</xs:attribute>
				<xs:attribute name="target">
					<xs:simpleType>
						<xs:restriction base="xs:NMTOKEN">
							<xsl:for-each select="//class">
								<xsl:sort select="@id"/>
								<xs:enumeration>
									<xsl:attribute name="value"><xsl:value-of select="@id"/></xsl:attribute>
								</xs:enumeration>
							</xsl:for-each>
						</xs:restriction>
					</xs:simpleType>
				</xs:attribute>
			</xs:complexType>
		</xs:element>
		<xs:element name="CustomLink">
			<xs:complexType>
				<xs:sequence minOccurs="0" maxOccurs="unbounded">
					<xs:element ref="Link"/>
				</xs:sequence>
				<xs:attribute name="name" type="xs:string" use="required"/>
			</xs:complexType>
		</xs:element>
		<xs:element name="CustomObj">
			<xs:complexType>
				<xs:choice minOccurs="0" maxOccurs="unbounded">
					<!--For this type of field we only want to be able to point to concrete classes-->
					<xsl:for-each select="//class[@abstract='false']">
						<xsl:sort select="@id"/>
						<xs:element>
							<xsl:attribute name="ref"><xsl:value-of select="@id"/></xsl:attribute>
						</xs:element>
					</xsl:for-each>
				</xs:choice>
				<xs:attribute name="name" type="xs:string" use="required"/>
			</xs:complexType>
		</xs:element>
		<xs:element name="CustomStr">
			<xs:complexType>
				<xs:choice minOccurs="0" maxOccurs="unbounded">
					<xs:element ref="AStr"/>
					<xs:element ref="AUni"/>
				</xs:choice>
				<xs:attribute name="name" type="xs:string" use="required"/>
			</xs:complexType>
		</xs:element>
		<!--Here we go to each class and create an element.
We also go to each of its properties and create corresponding elements-->
		<xsl:apply-templates/>
	</xsl:template>
	<xsl:template match="CellarModule">
		<xsl:variable name="CellarModuleNum">
			<xsl:value-of select="@num"/>
		</xsl:variable>
		<xsl:for-each select="class">
			<xsl:variable name="ClassNum">
				<xsl:value-of select="@num"/>
			</xsl:variable>
			<xsl:variable name="ClassName">
				<xsl:value-of select="@id"/>
			</xsl:variable>
			<xs:element>
				<xsl:attribute name="name"><xsl:value-of select="$ClassName"/></xsl:attribute>
				<xs:complexType>
					<xs:choice minOccurs="0" maxOccurs="unbounded">
						<xsl:for-each select="props/*">
							<xs:element>
								<!--If the CellarModule is Cellar with num=0 then we want to keep the leading zeros off
								This was done, I presume to save space in the XML file
								Example CmProject:Name0011 should be Name11-->
								<xsl:choose>
									<xsl:when test="../../../@num=0">
										<xsl:attribute name="ref"><xsl:value-of select="@id"/><xsl:number value="../../@num"/></xsl:attribute>
									</xsl:when>
									<xsl:otherwise>
										<xsl:attribute name="ref"><xsl:value-of select="@id"/><xsl:value-of select="../../../@num"/><xsl:number value="../../@num" format="001"/></xsl:attribute>
									</xsl:otherwise>
								</xsl:choose>
							</xs:element>
						</xsl:for-each>
						<xsl:call-template name="GetAttributesOfSuperClass"/>
						<!--Here we are the attributes of the superclasses-->
					</xs:choice>
					<!--This following two lines are the equivalent of
				id ID #IMPLIED
				ord CDATA #IMPLIED in DTD.
			-->
					<xs:attribute name="id" type="xs:ID"/>
					<xs:attribute name="ord" type="xs:string"/>
				</xs:complexType>
			</xs:element>
			<!--Here we add elements for each basic property and its signature-->
			<xsl:for-each select="props/basic">
				<xs:element>
					<!--If the CellarModule is Cellar with num=0 then we want to keep the leading zeros off
								This was done, I presume to save space in the XML file
								Example CmProject:Name0011 should be Name11-->
					<xsl:choose>
						<xsl:when test="../../../@num=0">
							<xsl:attribute name="name"><xsl:value-of select="@id"/><xsl:number value="../../@num"/></xsl:attribute>
						</xsl:when>
						<xsl:otherwise>
							<xsl:attribute name="name"><xsl:value-of select="@id"/><xsl:value-of select="../../../@num"/><xsl:number value="../../@num" format="001"/></xsl:attribute>
						</xsl:otherwise>
					</xsl:choose>
					<xsl:choose>
						<xsl:when test="@sig='MultiString'">
							<xs:complexType>
								<xs:sequence minOccurs="0" maxOccurs="unbounded">
									<xs:element>
										<xsl:attribute name="ref">AStr</xsl:attribute>
									</xs:element>
								</xs:sequence>
							</xs:complexType>
						</xsl:when>
						<xsl:when test="@sig='MultiUnicode'">
							<xs:complexType>
								<xs:sequence minOccurs="0" maxOccurs="unbounded">
									<xs:element>
										<xsl:attribute name="ref">AUni</xsl:attribute>
									</xs:element>
								</xs:sequence>
							</xs:complexType>
						</xsl:when>
						<xsl:otherwise>
							<xs:complexType>
								<xs:sequence minOccurs="0">
									<xs:element>
										<xsl:attribute name="ref"><xsl:choose><xsl:when test="@sig='String'">Str</xsl:when><xsl:when test="@sig='Unicode'">Uni</xsl:when><xsl:otherwise><xsl:value-of select="@sig"/></xsl:otherwise></xsl:choose></xsl:attribute>
									</xs:element>
								</xs:sequence>
							</xs:complexType>
						</xsl:otherwise>
					</xsl:choose>
				</xs:element>
			</xsl:for-each>
			<!--Here we add elements for each owning property and its signature and all other objects pointing to it-->
			<xsl:for-each select="props/owning">
				<xs:element>
					<!--If the CellarModule is Cellar with num=0 then we want to keep the leading zeros off
								This was done, I presume to save space in the XML file
								Example CmProject:Name0011 should be Name11-->
					<xsl:choose>
						<xsl:when test="../../../@num=0">
							<xsl:attribute name="name"><xsl:value-of select="@id"/><xsl:number value="../../@num"/></xsl:attribute>
						</xsl:when>
						<xsl:otherwise>
							<xsl:attribute name="name"><xsl:value-of select="@id"/><xsl:value-of select="../../../@num"/><xsl:number value="../../@num" format="001"/></xsl:attribute>
						</xsl:otherwise>
					</xsl:choose>
					<xs:complexType>
						<xsl:variable name="SuperClass" select="@sig"/>
						<xsl:choose>
							<xsl:when test="@card='atomic'">
								<!--If the signature is a superclass then we have choices rather than a simple sequence here-->
								<xsl:choose>
									<xsl:when test="//class[@base=$SuperClass]">
										<xs:choice minOccurs="0">
											<xsl:if test="//class[@id=$SuperClass][@abstract='false']">
												<xs:element>
													<xsl:attribute name="ref"><xsl:value-of select="@sig"/></xsl:attribute>
												</xs:element>
											</xsl:if>
											<!--Check to see if the signature class has any subclasses-->
											<xsl:if test="//class[@base=$SuperClass]">
												<xsl:for-each select="//class[@base=$SuperClass]">
													<xsl:call-template name="GetSubClassesNames"/>
												</xsl:for-each>
											</xsl:if>
										</xs:choice>
									</xsl:when>
									<xsl:otherwise>
										<xs:sequence minOccurs="0">
											<!--Check to see that the signature class is not abstract-->
											<xsl:if test="//class[@id=$SuperClass][@abstract='false']">
												<xs:element>
													<xsl:attribute name="ref"><xsl:value-of select="@sig"/></xsl:attribute>
												</xs:element>
											</xsl:if>
											<!--Check to see if the signature class has any subclasses-->
											<xsl:if test="//class[@base=$SuperClass]">
												<xsl:for-each select="//class[@base=$SuperClass]">
													<xsl:call-template name="GetSubClassesNames"/>
												</xsl:for-each>
											</xsl:if>
										</xs:sequence>
									</xsl:otherwise>
								</xsl:choose>
							</xsl:when>
							<!--If cardinality is not atomic then xs:sequence becomes xs:choice on owning-->
							<xsl:otherwise>
								<xsl:choose>
									<xsl:when test="//class[@base=$SuperClass]">
										<xs:choice minOccurs="0" maxOccurs="unbounded">
											<xsl:if test="//class[@id=$SuperClass][@abstract='false']">
												<xs:element>
													<xsl:attribute name="ref"><xsl:value-of select="@sig"/></xsl:attribute>
												</xs:element>
											</xsl:if>
											<!--Check to see if the signature class has any subclasses-->
											<xsl:if test="//class[@base=$SuperClass]">
												<xsl:for-each select="//class[@base=$SuperClass]">
													<xsl:call-template name="GetSubClassesNames"/>
												</xsl:for-each>
											</xsl:if>
										</xs:choice>
									</xsl:when>
									<xsl:otherwise>
										<xs:sequence minOccurs="0" maxOccurs="unbounded">
											<!--Check to see that the signature class is not abstract-->
											<xsl:if test="//class[@id=$SuperClass][@abstract='false']">
												<xs:element>
													<xsl:attribute name="ref"><xsl:value-of select="@sig"/></xsl:attribute>
												</xs:element>
											</xsl:if>
											<!--Check to see if the signature class has any subclasses-->
											<xsl:if test="//class[@base=$SuperClass]">
												<xsl:for-each select="//class[@base=$SuperClass]">
													<xsl:call-template name="GetSubClassesNames"/>
												</xsl:for-each>
											</xsl:if>
										</xs:sequence>
									</xsl:otherwise>
								</xsl:choose>
							</xsl:otherwise>
						</xsl:choose>
						<!--Check for not atomic. If so add following info -->
						<xsl:if test="not(@card='atomic')">
							<xs:attribute name="size" type="xs:string"/>
						</xsl:if>
					</xs:complexType>
				</xs:element>
			</xsl:for-each>
			<!--Here we add elements for each rel property and its signature and all other objects pointing to it-->
			<xsl:for-each select="props/rel">
				<xs:element>
					<!--If the CellarModule is Cellar with num=0 then we want to keep the leading zeros off
								This was done, I presume to save space in the XML file
								Example CmProject:Name0011 should be Name11-->
					<xsl:choose>
						<xsl:when test="../../../@num=0">
							<xsl:attribute name="name"><xsl:value-of select="@id"/><xsl:number value="../../@num"/></xsl:attribute>
						</xsl:when>
						<xsl:otherwise>
							<xsl:attribute name="name"><xsl:value-of select="@id"/><xsl:value-of select="../../../@num"/><xsl:number value="../../@num" format="001"/></xsl:attribute>
						</xsl:otherwise>
					</xsl:choose>
					<xs:complexType>
						<xs:sequence minOccurs="0">
							<!--Check for not atomic. If so add attribute maxOccurs set to unbounded-->
							<xsl:if test="not(@card='atomic')">
								<xsl:attribute name="maxOccurs">unbounded</xsl:attribute>
							</xsl:if>
							<xs:element>
								<xsl:attribute name="ref">Link</xsl:attribute>
							</xs:element>
							<!--<xsl:variable name="SuperClass" select="@sig"/>-->
							<!--Check to see that the signature class is not abstract-->
							<!--<xsl:if test="//class[@id=$SuperClass][@abstract='false']">
								<xs:element>
									<xsl:attribute name="ref">-->
							<!--<xsl:value-of select="@sig"/>-->
							<!--</xsl:attribute>
								</xs:element>
							</xsl:if>-->
							<!--Check to see if the signature class has any subclasses-->
							<!--<xsl:if test="//class[@base=$SuperClass]">
								<xsl:for-each select="//class[@base=$SuperClass]">
									<xsl:call-template name="GetSubClassesNames"/>
								</xsl:for-each>
							</xsl:if>-->
						</xs:sequence>
						<!--Check for not atomic. If so add following info -->
						<xsl:if test="not(@card='atomic')">
							<xs:attribute name="size" type="xs:string"/>
						</xsl:if>
					</xs:complexType>
				</xs:element>
			</xsl:for-each>
		</xsl:for-each>
	</xsl:template>
	<xsl:template name="GetSubClassesNames">
		<xs:element>
			<xsl:attribute name="ref"><xsl:value-of select="@id"/></xsl:attribute>
		</xs:element>
		<xsl:variable name="SuperClass" select="@id"/>
		<xsl:if test="//class[@base=$SuperClass]">
			<xsl:for-each select="//class[@base=$SuperClass]">
				<xsl:call-template name="GetSubClassesNames"/>
			</xsl:for-each>
		</xsl:if>
	</xsl:template>
	<xsl:template name="GetAttributesOfSuperClass">
		<xsl:choose>
			<xsl:when test="@base='CmObject'">
				<!--Here we add custom capabilities to each class-->
				<xs:element ref="Custom"/>
				<xs:element ref="CustomStr"/>
				<xs:element ref="CustomLink"/>
				<xs:element ref="CustomObj"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:variable name="SuperClassBase" select="@base"/>
				<xsl:for-each select="//class[@id=$SuperClassBase]">
					<xsl:for-each select="props/*">
						<xs:element>
							<xsl:choose>
								<xsl:when test="../../../@num=0">
									<xsl:attribute name="ref"><xsl:value-of select="@id"/><xsl:number value="../../@num"/></xsl:attribute>
								</xsl:when>
								<xsl:otherwise>
									<xsl:attribute name="ref"><xsl:value-of select="@id"/><xsl:value-of select="../../../@num"/><xsl:number value="../../@num" format="001"/></xsl:attribute>
								</xsl:otherwise>
							</xsl:choose>
						</xs:element>
					</xsl:for-each>
					<xsl:call-template name="GetAttributesOfSuperClass"/>
				</xsl:for-each>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
</xsl:stylesheet>
