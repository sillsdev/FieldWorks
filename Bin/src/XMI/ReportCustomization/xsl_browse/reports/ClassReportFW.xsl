<?xml version="1.0" encoding="UTF-8"?>
<!-- edited with XML Spy v3.5 (http://www.xmlspy.com) by Larry Hayashi (SIL International)
TO DO
Fix the Show documentation template so that it wraps properly for class level documentation.-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:lxslt="http://xml.apache.org/xslt" xmlns:redirect="org.apache.xalan.lib.Redirect" extension-element-prefixes="redirect">
	<!--
Template for Class report
-->
	<xsl:template name="CREATE_CLASS_REPORT">
		<!-- Associations of this element -->
		<xsl:variable name="elementAssociations" select="key('CellarAssociationByEndElementID', @xmi.id)"/>
		<!--	Choose what include in the report -->
		<xsl:variable name="showAnyInnerElement" select="($generateUseCases and Foundation.Core.Namespace.ownedElement/Behavioral_Elements.Use_Cases.UseCase) or ($generateActors and Foundation.Core.Namespace.ownedElement/Behavioral_Elements.Use_Cases.Actor) or ($generateClasses and (Foundation.Core.Namespace.ownedElement/Foundation.Core.Interface or Foundation.Core.Namespace.ownedElement/Foundation.Core.Class or Foundation.Core.Namespace.ownedElement/Foundation.Core.DataType)) or ($generateComponents and (Foundation.Core.Namespace.ownedElement/Foundation.Core.Component or Foundation.Core.Namespace.ownedElement/Behavioral_Elements.Common_Behavior.ComponentInstance)) or ($generateNodes and (Foundation.Core.Namespace.ownedElement/Foundation.Core.Node or Foundation.Core.Namespace.ownedElement/Behavioral_Elements.Common_Behavior.NodeInstance)) or ($generateActivity and (Foundation.Core.Namespace.ownedElement/Behavioral_Elements.Activity_Graphs.ClassifierInState or Foundation.Core.Namespace.ownedElement/Behavioral_Elements.Activity_Graphs.ActivityGraph)) or ($generateState and Foundation.Core.Namespace.ownedElement/Behavioral_Elements.State_Machines.StateMachine) or Foundation.Core.Namespace.ownedElement/Behavioral_Elements.Collaborations.Collaboration or ($generateInstances and Foundation.Core.Namespace.ownedElement/Behavioral_Elements.Common_Behavior.DataValue)"/>
		<xsl:variable name="hasInnerElements" select="Foundation.Core.Namespace.ownedElement and $showAnyInnerElement"/>
		<xsl:variable name="hasAttributes" select="$generateAttributes and Foundation.Core.Classifier.feature/Foundation.Core.Attribute and (not($generatePublicOnly) or ($generatePublicOnly and Foundation.Core.Classifier.feature/Foundation.Core.Attribute[Foundation.Core.ModelElement.visibility/@xmi.value = 'public']))"/>
		<xsl:variable name="hasOperations" select="$generateOperations and Foundation.Core.Classifier.feature/Foundation.Core.Operation and (not($generatePublicOnly) or ($generatePublicOnly and Foundation.Core.Classifier.feature/Foundation.Core.Operation[Foundation.Core.ModelElement.visibility/@xmi.value = 'public']))"/>
		<xsl:variable name="hasTemplateParameters" select="Foundation.Core.ModelElement.templateParameter"/>
		<xsl:variable name="hasRelations" select="Foundation.Core.GeneralizableElement.generalization | Foundation.Core.GeneralizableElement.specialization | Foundation.Core.ModelElement.clientDependency | Foundation.Core.ModelElement.supplierDependency | $elementAssociations"/>
		<xsl:variable name="hasInnerRelations" select="Foundation.Core.Namespace.ownedElement/Foundation.Core.Abstraction or Foundation.Core.Namespace.ownedElement/Foundation.Core.Association or Foundation.Core.Namespace.ownedElement/Foundation.Core.Binding or Foundation.Core.Namespace.ownedElement/Foundation.Core.Dependency or Foundation.Core.Namespace.ownedElement/Foundation.Core.Generalization or Foundation.Core.Namespace.ownedElement/Foundation.Core.Permission or Foundation.Core.Namespace.ownedElement/Foundation.Core.Usage or ($generateUseCases and (Foundation.Core.Namespace.ownedElement/Behavioral_Elements.Use_Cases.Extend or Foundation.Core.Namespace.ownedElement/Behavioral_Elements.Use_Cases.Include))"/>
		<xsl:variable name="hasTaggedValues" select="$showTaggedValues and Foundation.Core.ModelElement.taggedValue"/>
		<xsl:variable name="hasConstraints" select="$showConstraints and Foundation.Core.ModelElement.constraint"/>
		<!-- Associations of this element -->
		<!--Set variables for Class Information -->
		<!--Class GUID-->
		<xsl:variable name="ClassGUID">
			<xsl:value-of select="@xmi.id"/>
		</xsl:variable>
		<xsl:variable name="ClassNum">
			<!--Attribute num -->
			<xsl:for-each select="Foundation.Core.ModelElement.taggedValue/Foundation.Extension_Mechanisms.TaggedValue[Foundation.Core.ModelElement.name='num']">
				<xsl:value-of select="Foundation.Extension_Mechanisms.TaggedValue.dataValue"/>
			</xsl:for-each>
		</xsl:variable>
		<!-- Attribute id -->
		<xsl:variable name="ClassId">
			<xsl:value-of select="Foundation.Core.ModelElement.name"/>
		</xsl:variable>
		<!-- Attribute abstract -->
		<xsl:variable name="ClassAbstract">
			<xsl:value-of select="Foundation.Core.GeneralizableElement.isAbstract/@xmi.value"/>
		</xsl:variable>
		<!-- Attribute abbr -->
		<xsl:variable name="ClassAbbr">
			<xsl:for-each select="Foundation.Core.ModelElement.taggedValue/Foundation.Extension_Mechanisms.TaggedValue[Foundation.Extension_Mechanisms.TaggedValue.tag='abbrev']">
				<xsl:value-of select="./Foundation.Extension_Mechanisms.TaggedValue.value"/>
			</xsl:for-each>
		</xsl:variable>
		<!-- Attribute base -->
		<!--Code to go out and get the name of the uniquely referenced base-->
		<xsl:variable name="BaseObjectGeneralizationGUID" select="Foundation.Core.GeneralizableElement.generalization/Foundation.Core.Generalization/@xmi.idref"/>
		<xsl:for-each select="//Foundation.Core.Generalization[@xmi.id=$BaseObjectGeneralizationGUID]">
			<xsl:variable name="BaseObjectGeneralizationParentGUID" select="Foundation.Core.Generalization.parent/Foundation.Core.Class/@xmi.idref"/>
			<xsl:variable name="ClassBase">
				<xsl:for-each select="//Foundation.Core.Class[@xmi.id=$BaseObjectGeneralizationParentGUID]">
					<xsl:value-of select="Foundation.Core.ModelElement.name"/>
				</xsl:for-each>
			</xsl:variable>
		</xsl:for-each>
		<!-- Create Class report -->
		<!-- -->
		<redirect:write file="{$destinationDir}/{@xmi.id}Report.html">
			<html>
				<head>
					<title>Class <xsl:value-of select="Foundation.Core.ModelElement.name"/> Report
					</title>
				</head>
				<body bgcolor="#FFFFFF">
					<a name="{@xmi.id}"/>
					<!--            ===========  Report header ===========             -->
					<!--            ===================================             -->
					<table width="100%" border="0" bgcolor="#EEEEFF">
						<tr>
							<td width="1" bgcolor="#EEEEFF">
								<font face="Courier New, Courier, mono" size="-1">
									<b>View:</b>
								</font>
							</td>
							<td bgcolor="#EEEEFF">
								<font size="-1">
									<a href="{@xmi.id}Report.html" target="_top">
									Hide Browser
								</a>
| <a href="indexLeft.html" target="_top">Browser on the left</a> | <a href="indexRight.html" target="_top">Browser on the right</a>
									<xsl:if test="$generateDictionary"> | <a href="Dictionary.html" target="_self">Dictionary</a>
									</xsl:if>
									<xsl:if test="$showUMLInfo"> | <a href="UMLInfo/Class.html" target="_self">UML Info</a>
									</xsl:if>
								</font>
							</td>
						</tr>
						<tr>
							<td width="1" bgcolor="#EEEEFF">
								<font face="Courier New, Courier, mono" size="-1">
									<b>Report:</b>
								</font>
							</td>
							<td bgcolor="#EEEEFF">
								<font size="-1">
									<a href="#general" target="_self">General</a>
									<xsl:if test="$hasAttributes"> | <a href="#attributes" target="_self">Attributes</a>
									</xsl:if>
									<xsl:if test="$hasOperations"> | <a href="#operations" target="_self">Operations</a>
									</xsl:if>
									<xsl:if test="$hasTemplateParameters"> | <a href="#template_parameters" target="_self">Template Parameters</a>
									</xsl:if>
									<xsl:if test="$hasInnerElements"> | <a href="#inner_elements" target="_self">Inner Elements</a>
									</xsl:if>
									<xsl:if test="$hasRelations"> | <a href="#relations" target="_self">Relations</a>
									</xsl:if>
									<xsl:if test="$hasInnerRelations"> | <a href="#inner_relations" target="_self">Inner Relations</a>
									</xsl:if>
									<xsl:if test="$hasTaggedValues"> | <a href="#tagged_values" target="_self">Tagged Values</a>
									</xsl:if>
									<xsl:if test="$hasConstraints"> | <a href="#constraints" target="_self">Constraints</a>
									</xsl:if>
								</font>
							</td>
						</tr>
					</table>
					<hr noshade="" size="1"/>
					<p/>
					<!--            ===========  Report body ===========             -->
					<!--            =================================             -->
					<!--         Element type and name             -->
					<xsl:if test="../../Foundation.Core.Namespace.ownedElement = ..">
						<a href="{../../@xmi.id}Report.html" target="_self">
							<xsl:choose>
								<xsl:when test="../../Foundation.Core.ModelElement.name">
									<xsl:value-of select="../../Foundation.Core.ModelElement.name"/>
									<xsl:text>&#32;(</xsl:text>
									<xsl:value-of select="../../Foundation.Core.ModelElement.taggedValue/Foundation.Extension_Mechanisms.TaggedValue[Foundation.Core.ModelElement.name='num']/Foundation.Extension_Mechanisms.TaggedValue.dataValue"/>
									<xsl:text>)</xsl:text>
								</xsl:when>
								<xsl:otherwise>unnamed</xsl:otherwise>
							</xsl:choose>
						</a>
						<br/>
					</xsl:if>
					<font size="+2">class<xsl:text disable-output-escaping="yes">&amp;nbsp;</xsl:text>
						<b>
							<xsl:choose>
								<xsl:when test="Foundation.Core.ModelElement.name">
									<xsl:value-of select="Foundation.Core.ModelElement.name"/>
								</xsl:when>
								<xsl:otherwise>unnamed</xsl:otherwise>
							</xsl:choose>
						</b>
					</font>
					<!--         Class hierarchy          -->
					<xsl:if test="Foundation.Core.GeneralizableElement.generalization">
						<font size="-1" face="Courier New, Courier, mono">
							<p/>
							<br/>
							<xsl:for-each select="Foundation.Core.GeneralizableElement.generalization/Foundation.Core.Generalization">
								<xsl:if test="position() != 1">,<xsl:text disable-output-escaping="yes">&amp;nbsp;</xsl:text>
								</xsl:if>
								<xsl:variable name="superclassID" select="key('GeneralizationByID', @xmi.idref)/Foundation.Core.Generalization.parent/*/@xmi.idref"/>
								<a href="{$superclassID}Report.html" target="_self">
									<xsl:variable name="name" select="key('ElementByID', $superclassID)/Foundation.Core.ModelElement.name"/>
									<xsl:choose>
										<xsl:when test="$name">
											<xsl:value-of select="$name"/>
										</xsl:when>
										<xsl:otherwise>unnamed</xsl:otherwise>
									</xsl:choose>
								</a>
							</xsl:for-each>
							<br/>
							<xsl:text disable-output-escaping="yes">&amp;nbsp;&amp;nbsp;&amp;nbsp;</xsl:text>|<br/>
							<xsl:text disable-output-escaping="yes">&amp;nbsp;&amp;nbsp;&amp;nbsp;</xsl:text>+--<b>
								<xsl:choose>
									<xsl:when test="Foundation.Core.ModelElement.name">
										<xsl:value-of select="Foundation.Core.ModelElement.name"/>
									</xsl:when>
									<xsl:otherwise>unnamed</xsl:otherwise>
								</xsl:choose>
							</b>
						</font>
					</xsl:if>
					<!--         Class subclassifiers     -->
					<xsl:if test="Foundation.Core.GeneralizableElement.specialization">
						<p/>
						<font size="-1">
							<b>Direct Known Subclassifiers:</b>
						</font>
						<br/>
						<xsl:text disable-output-escaping="yes">&amp;nbsp;&amp;nbsp;&amp;nbsp;&amp;nbsp;&amp;nbsp;&amp;nbsp;&amp;nbsp;</xsl:text>
						<xsl:for-each select="Foundation.Core.GeneralizableElement.specialization/Foundation.Core.Generalization">
							<xsl:if test="position() != 1">,<xsl:text disable-output-escaping="yes">&amp;nbsp;</xsl:text>
							</xsl:if>
							<xsl:variable name="subclassID" select="key('GeneralizationByID', @xmi.idref)/Foundation.Core.Generalization.child/*/@xmi.idref"/>
							<a href="{$subclassID}Report.html" target="_self">
								<xsl:variable name="name" select="key('ElementByID', $subclassID)/Foundation.Core.ModelElement.name"/>
								<xsl:choose>
									<xsl:when test="$name">
										<xsl:value-of select="$name"/>
									</xsl:when>
									<xsl:otherwise>unnamed</xsl:otherwise>
								</xsl:choose>
							</a>
						</xsl:for-each>
					</xsl:if>
					<!--         Implemented classifiers     -->
					<xsl:if test="Foundation.Core.ModelElement.supplierDependency/Foundation.Core.Abstraction">
						<p/>
						<font size="-1">
							<b>Known Implemented Classifiers:</b>
						</font>
						<br/>
						<xsl:text disable-output-escaping="yes">&amp;nbsp;&amp;nbsp;&amp;nbsp;&amp;nbsp;&amp;nbsp;&amp;nbsp;&amp;nbsp;</xsl:text>
						<xsl:for-each select="Foundation.Core.ModelElement.supplierDependency/Foundation.Core.Abstraction">
							<xsl:if test="position() != 1">,<xsl:text disable-output-escaping="yes">&amp;nbsp;</xsl:text>
							</xsl:if>
							<xsl:call-template name="SHOW_LINK_TO_ELEMENT">
								<xsl:with-param name="elementID" select="key('AbstractionByID', @xmi.idref)/Foundation.Core.Dependency.supplier/*/@xmi.idref"/>
							</xsl:call-template>
						</xsl:for-each>
					</xsl:if>
					<p/>
					<!--            ===========  General information ===========             -->
					<table width="100%" border="1" cellspacing="0" align="center">
						<tr bgcolor="#CCCCFF">
							<td colspan="2">
								<b>
									<font size="+1">
										<a name="general">General (Cellar Class information)
										</a>
									</font>
								</b>
							</td>
						</tr>
						<!--         Class name [mandatory]         -->
						<tr>
							<td>
								<p>
									<b>Abstract </b>
									<xsl:value-of select="Foundation.Core.GeneralizableElement.isAbstract/@xmi.value"/>
									<xsl:text> | </xsl:text>
									<b>Num	 </b>
									<xsl:value-of select="Foundation.Core.ModelElement.taggedValue/Foundation.Extension_Mechanisms.TaggedValue[Foundation.Core.ModelElement.name='num']/Foundation.Extension_Mechanisms.TaggedValue.dataValue"/>
									<xsl:text> | </xsl:text>
									<b>Abbrev </b>
									<xsl:value-of select="Foundation.Core.ModelElement.taggedValue/Foundation.Extension_Mechanisms.TaggedValue[Foundation.Core.ModelElement.name='abbrev']/Foundation.Extension_Mechanisms.TaggedValue.dataValue"/>
								</p>
							</td>
						</tr>
					</table>
					<!--         Comment             -->
					<xsl:call-template name="SHOW_DOCUMENTATION"/>
					<!--    =========  Class attributes [optional] ===========      -->
					<xsl:if test="$hasAttributes">
						<br/>
						<xsl:call-template name="SHOW_CELLAR_BASIC_PROPERTIES"/>
					</xsl:if>
					<!--    =========  Class relations [optional] ===========      -->
					<xsl:if test="$hasRelations">
						<br/>
						<xsl:call-template name="SHOW_CELLAR_RELATIONS">
							<xsl:with-param name="relationEndElementID" select="@xmi.id"/>
							<xsl:with-param name="relationEndElementName" select="Foundation.Core.ModelElement.name"/>
							<xsl:with-param name="elementAssociations" select="$elementAssociations"/>
						</xsl:call-template>
					</xsl:if>
					<!--Show detailed documentation for each CELLAR property.-->
					<xsl:if test="$hasAttributes">
						<br/>
						<xsl:call-template name="SHOW_CELLAR_BASIC_PROPERTIES_DOCUMENTATION"/>
					</xsl:if>
					<xsl:if test="$hasRelations">
						<br/>
						<xsl:call-template name="SHOW_CELLAR_ASSOCIATIONS_DOCUMENTATION">
							<xsl:with-param name="relationEndElementID" select="@xmi.id"/>
							<xsl:with-param name="relationEndElementName" select="Foundation.Core.ModelElement.name"/>
							<xsl:with-param name="elementAssociations" select="$elementAssociations"/>
						</xsl:call-template>
					</xsl:if>
				</body>
			</html>
		</redirect:write>
		<!-- Write reported element ID to dictionary helper file -->
		<redirect:write file="{$reportedElementsFilePath}">
			<xsl:element name="element">
				<xsl:value-of select="@xmi.id"/>
			</xsl:element>
		</redirect:write>
	</xsl:template>
	<xsl:template name="SHOW_CELLAR_BASIC_PROPERTIES">
		<xsl:param name="attributesORqualifiers">ATTRIBUTES</xsl:param>
		<table border="1" width="100%" cellpadding="2" cellspacing="0">
			<tr bgcolor="#CCCCFF">
				<td colspan="4">
					<b>
						<font size="+1">
							<a name="attributes">
								<xsl:choose>
									<xsl:when test="$attributesORqualifiers = 'QUALIFIERS'">Qualifiers</xsl:when>
									<xsl:otherwise>Attributes - Cellar Basic Properties</xsl:otherwise>
								</xsl:choose>
							</a>
						</font>
					</b>
				</td>
			</tr>
			<tr>
				<td width="2%">
					<b>Num</b>
				</td>
				<td width="40%">
					<b>Basic Property</b>
				</td>
				<td width="8%">
					<b>NA</b>
				</td>
				<td width="40%">
					<b>Signature</b>
				</td>
			</tr>
			<xsl:for-each select="(Foundation.Core.Classifier.feature | Foundation.Core.AssociationEnd.qualifier)/Foundation.Core.Attribute">
				<xsl:sort data-type="number" order="ascending" case-order="upper-first" select="Foundation.Core.ModelElement.taggedValue/Foundation.Extension_Mechanisms.TaggedValue[Foundation.Core.ModelElement.name='num']/Foundation.Extension_Mechanisms.TaggedValue.dataValue"/>
				<xsl:if test="not($generatePublicOnly) or ($generatePublicOnly and Foundation.Core.ModelElement.visibility/@xmi.value = 'public')">
					<!-- Atribute type ID -->
					<xsl:variable name="attributeTypeID" select="Foundation.Core.StructuralFeature.type/*/@xmi.idref"/>
					<!--  Attribute type name -->
					<xsl:variable name="attributeTypeName" select="key('ElementByID', $attributeTypeID)/Foundation.Core.ModelElement.name"/>
					<tr bgcolor="#C0C0C0">
						<td width="2%">
							<xsl:value-of select="Foundation.Core.ModelElement.taggedValue/Foundation.Extension_Mechanisms.TaggedValue[Foundation.Core.ModelElement.name='num']/Foundation.Extension_Mechanisms.TaggedValue.dataValue"/>
						</td>
						<td width="40%">
							<A>
								<xsl:attribute name="href"><xsl:text>#</xsl:text><xsl:value-of select="Foundation.Core.ModelElement.name"/></xsl:attribute>
								<xsl:value-of select="Foundation.Core.ModelElement.name"/>
							</A>
						</td>
						<td width="8%">
							<xsl:text>NA</xsl:text>
						</td>
						<td width="40%">
							<a href="{$attributeTypeID}Report.html" target="_self">
								<xsl:value-of select="$attributeTypeName"/>
							</a>
						</td>
					</tr>
				</xsl:if>
			</xsl:for-each>
		</table>
	</xsl:template>
	<xsl:template name="SHOW_CELLAR_BASIC_PROPERTIES_DOCUMENTATION">
		<xsl:param name="attributesORqualifiers">ATTRIBUTES</xsl:param>
		<xsl:for-each select="(Foundation.Core.Classifier.feature | Foundation.Core.AssociationEnd.qualifier)/Foundation.Core.Attribute">
			<xsl:sort data-type="number" order="ascending" case-order="upper-first" select="Foundation.Core.ModelElement.taggedValue/Foundation.Extension_Mechanisms.TaggedValue[Foundation.Extension_Mechanisms.TaggedValue.tag='num']/Foundation.Extension_Mechanisms.TaggedValue.value"/>
			<xsl:if test="not($generatePublicOnly) or ($generatePublicOnly and Foundation.Core.ModelElement.visibility/@xmi.value = 'public')">
				<!-- Atribute type ID -->
				<xsl:variable name="attributeTypeID" select="Foundation.Core.StructuralFeature.type/*/@xmi.idref"/>
				<!--  Attribute type name -->
				<xsl:variable name="attributeTypeName" select="key('ElementByID', $attributeTypeID)/Foundation.Core.ModelElement.name"/>
				<p>
					<!--Put num value here-->
					<xsl:value-of select="Foundation.Core.ModelElement.taggedValue/Foundation.Extension_Mechanisms.TaggedValue[Foundation.Core.ModelElement.name='num']/Foundation.Extension_Mechanisms.TaggedValue.dataValue"/>
					<xsl:text>:</xsl:text>
					<!--Here we set a HTML Bookmark for the attribute name so that the user can jump to this documentation-->
					<b>
						<A>
							<xsl:attribute name="name"><xsl:value-of select="Foundation.Core.ModelElement.name"/></xsl:attribute>
							<xsl:value-of select="Foundation.Core.ModelElement.name"/>
						</A>
					</b>
					<xsl:text> sig: </xsl:text>
					<a href="{$attributeTypeID}Report.html" target="_self">
						<xsl:value-of select="$attributeTypeName"/>
					</a>
					<xsl:text>: </xsl:text>
					<xsl:value-of select="Foundation.Core.ModelElement.comment/Foundation.Core.Comment/Foundation.Core.ModelElement.name"/>
				</p>
			</xsl:if>
		</xsl:for-each>
	</xsl:template>
	<xsl:template name="SHOW_CELLAR_RELATIONS">
		<xsl:param name="relationEndElementID"/>
		<xsl:param name="relationEndElementName"/>
		<xsl:param name="elementAssociations"/>
		<table width="100%" border="1" cellspacing="0">
			<tr bgcolor="#CCCCFF">
				<td colspan="5">
					<b>
						<font size="+1">
							<a name="relations">Relations (Cellar owning and reference)</a>
						</font>
					</b>
				</td>
			</tr>
			<tr>
				<td width="2%">
					<b>Num</b>
				</td>
				<td>
					<b>Label</b>
				</td>
				<td>
					<b>Cardinality</b>
				</td>
				<td>
					<b>Kind</b>
				</td>
				<td>
					<b>Sig</b>
				</td>
			</tr>
			<xsl:call-template name="SHOW_CELLAR_ASSOCIATIONS">
				<xsl:with-param name="relationEndElementID" select="$relationEndElementID"/>
				<xsl:with-param name="relationEndElementName" select="$relationEndElementName"/>
				<xsl:with-param name="elementAssociations" select="$elementAssociations"/>
			</xsl:call-template>
		</table>
	</xsl:template>
	<!--
Template for associations
-->
	<xsl:template name="SHOW_CELLAR_ASSOCIATIONS">
		<xsl:param name="relationEndElementID"/>
		<xsl:param name="relationEndElementName"/>
		<xsl:param name="elementAssociations"/>
		<xsl:for-each select="$elementAssociations">
			<xsl:sort data-type="number" order="ascending" case-order="upper-first" select="Foundation.Core.ModelElement.taggedValue/Foundation.Extension_Mechanisms.TaggedValue[Foundation.Core.ModelElement.name='num']/Foundation.Extension_Mechanisms.TaggedValue.dataValue"/>
			<tr>
				<!--Show attribute num-->
				<td width="2%">
					<xsl:value-of select="Foundation.Core.ModelElement.taggedValue/Foundation.Extension_Mechanisms.TaggedValue[Foundation.Core.ModelElement.name='num']/Foundation.Extension_Mechanisms.TaggedValue.dataValue"/>
				</td>
				<!--Show attribute name-->
				<td>
					<!--xsl:text disable-output-escaping="yes">&amp;nbsp;</xsl:text>
					<a href="{@xmi.id}Report.html" target="_self">
						<xsl:choose>
							<xsl:when test="Foundation.Core.ModelElement.name">
								<xsl:value-of select="Foundation.Core.ModelElement.name"/>
							</xsl:when>
							<xsl:otherwise>unnamed</xsl:otherwise>
						</xsl:choose>
					</a-->
					<A>
						<xsl:attribute name="href"><xsl:text>#</xsl:text><xsl:value-of select="Foundation.Core.ModelElement.name"/></xsl:attribute>
						<xsl:value-of select="Foundation.Core.ModelElement.name"/>
					</A>
				</td>
				<!--Show attribute card-->
				<td>
					<xsl:variable name="multiplicityUpper" select="Foundation.Core.Association.connection/Foundation.Core.AssociationEnd[position()=2]/Foundation.Core.AssociationEnd.multiplicity/Foundation.Data_Types.Multiplicity/Foundation.Data_Types.Multiplicity.range/Foundation.Data_Types.MultiplicityRange/Foundation.Data_Types.MultiplicityRange.upper"/>
					<xsl:choose>
						<!--We test the multiplicity upper bounds. If it is less than or equal to 1 we know the card is atomic, otherwise it is either seq or card.-->
						<xsl:when test="$multiplicityUpper = 1 or $multiplicityUpper = -1">
							<xsl:text>atomic</xsl:text>
						</xsl:when>
						<xsl:otherwise>
							<xsl:choose>
								<!--If the ordering is ordered then we know the attribute is not collection but a sequence (ordered collection)-->
								<xsl:when test="Foundation.Core.Association.connection/Foundation.Core.AssociationEnd/Foundation.Core.AssociationEnd.ordering/@xmi.value='ordered'">
									<xsl:text>seq (ordered)</xsl:text>
								</xsl:when>
								<xsl:otherwise>
									<xsl:text>col (unordered)</xsl:text>
								</xsl:otherwise>
							</xsl:choose>
						</xsl:otherwise>
					</xsl:choose>
				</td>
				<!--Show attribute kind (reference (association) or owning (composite aggregation)-->
				<td>
					<xsl:choose>
						<xsl:when test="Foundation.Core.Association.connection/Foundation.Core.AssociationEnd[position()=1]/Foundation.Core.AssociationEnd.aggregation/@xmi.value='composite'">
							<xsl:text>owning (composite aggregation)</xsl:text>
						</xsl:when>
						<xsl:otherwise>
							<xsl:text>reference (association)</xsl:text>
						</xsl:otherwise>
					</xsl:choose>
				</td>
				<!--Show signature of Cellar attribute-->
				<xsl:for-each select="Foundation.Core.Association.connection/Foundation.Core.AssociationEnd[position()=2]/Foundation.Core.AssociationEnd.type/*">
					<xsl:choose>
						<xsl:when test="$relationEndElementID = @xmi.idref">
							<td>
								<a href="{$relationEndElementID}Report.html" target="_self">
									<xsl:choose>
										<xsl:when test="$relationEndElementName">
											<xsl:value-of select="$relationEndElementName"/>
										</xsl:when>
										<xsl:otherwise>unnamed</xsl:otherwise>
									</xsl:choose>
								</a>
							</td>
						</xsl:when>
						<xsl:otherwise>
							<xsl:variable name="elementName" select="key('ElementByID',@xmi.idref)/Foundation.Core.ModelElement.name"/>
							<td>
								<a href="{@xmi.idref}Report.html" target="_self">
									<xsl:choose>
										<xsl:when test="$elementName">
											<xsl:value-of select="$elementName"/>
										</xsl:when>
										<xsl:otherwise>unnamed</xsl:otherwise>
									</xsl:choose>
								</a>
							</td>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:for-each>
			</tr>
		</xsl:for-each>
	</xsl:template>
	<xsl:template name="SHOW_CELLAR_ASSOCIATIONS_DOCUMENTATION">
		<xsl:param name="relationEndElementID"/>
		<xsl:param name="relationEndElementName"/>
		<xsl:param name="elementAssociations"/>
		<xsl:for-each select="$elementAssociations">
			<xsl:sort data-type="number" order="ascending" case-order="upper-first" select="Foundation.Core.ModelElement.taggedValue/Foundation.Extension_Mechanisms.TaggedValue[Foundation.Core.ModelElement.name='num']/Foundation.Extension_Mechanisms.TaggedValue.dataValue"/>
			<p>
				<!--Show attribute num-->
				<xsl:value-of select="Foundation.Core.ModelElement.taggedValue/Foundation.Extension_Mechanisms.TaggedValue[Foundation.Core.ModelElement.name='num']/Foundation.Extension_Mechanisms.TaggedValue.dataValue"/>
				<xsl:text>: </xsl:text>
				<!--Show attribute name-->
				<xsl:text disable-output-escaping="yes">&amp;nbsp;</xsl:text>
				<!--Here we add a bookmark so that this documentation can be easily accessed from the table above.-->
				<b>
					<A>
						<xsl:attribute name="name"><xsl:value-of select="Foundation.Core.ModelElement.name"/></xsl:attribute>
						<xsl:value-of select="Foundation.Core.ModelElement.name"/>
					</A>
				</b>
				<xsl:text/>
				<!--Show attribute card-->
				<xsl:variable name="multiplicityUpper" select="Foundation.Core.Association.connection/Foundation.Core.AssociationEnd[position()=2]/Foundation.Core.AssociationEnd.multiplicity/Foundation.Data_Types.Multiplicity/Foundation.Data_Types.Multiplicity.range/Foundation.Data_Types.MultiplicityRange/Foundation.Data_Types.MultiplicityRange.upper"/>
				<xsl:choose>
					<!--We test the multiplicity upper bounds. If it is less than or equal to 1 we know the card is atomic, otherwise it is either seq or card.-->
					<xsl:when test="$multiplicityUpper = 1 or $multiplicityUpper = -1">
						<xsl:text> card: atomic </xsl:text>
					</xsl:when>
					<xsl:otherwise>
						<xsl:choose>
							<!--If the ordering is ordered then we know the attribute is not collection but a sequence (ordered collection)-->
							<xsl:when test="Foundation.Core.Association.connection/Foundation.Core.AssociationEnd/Foundation.Core.AssociationEnd.ordering/@xmi.value='ordered'">
								<xsl:text> card: seq (ordered) </xsl:text>
							</xsl:when>
							<xsl:otherwise>
								<xsl:text> card: col (unordered) </xsl:text>
							</xsl:otherwise>
						</xsl:choose>
					</xsl:otherwise>
				</xsl:choose>
				<!--Show attribute kind (reference (association) or owning (composite aggregation)-->
				<xsl:choose>
					<xsl:when test="Foundation.Core.Association.connection/Foundation.Core.AssociationEnd[position()=1]/Foundation.Core.AssociationEnd.aggregation/@xmi.value='composite'">
						<xsl:text>owning </xsl:text>
					</xsl:when>
					<xsl:otherwise>
						<xsl:text>reference </xsl:text>
					</xsl:otherwise>
				</xsl:choose>
				<!--Show signature of Cellar attribute-->
				<xsl:for-each select="Foundation.Core.Association.connection/Foundation.Core.AssociationEnd[position()=2]/Foundation.Core.AssociationEnd.type/*">
					<xsl:text>sig: </xsl:text>
					<xsl:choose>
						<xsl:when test="$relationEndElementID = @xmi.idref">
							<a href="{$relationEndElementID}Report.html" target="_self">
								<xsl:choose>
									<xsl:when test="$relationEndElementName">
										<xsl:value-of select="$relationEndElementName"/>
									</xsl:when>
									<xsl:otherwise>unnamed</xsl:otherwise>
								</xsl:choose>
							</a><xsl:text>&#32;</xsl:text>
						</xsl:when>
						<xsl:otherwise>
							<xsl:variable name="elementName" select="key('ElementByID',@xmi.idref)/Foundation.Core.ModelElement.name"/>
							<a href="{@xmi.idref}Report.html" target="_self">
								<xsl:choose>
									<xsl:when test="$elementName">
										<xsl:value-of select="$elementName"/>
									</xsl:when>
									<xsl:otherwise>unnamed</xsl:otherwise>
								</xsl:choose>
							</a><xsl:text>&#32;</xsl:text>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:for-each>
				<xsl:text/>
				<xsl:value-of select="Foundation.Core.ModelElement.comment/Foundation.Core.Comment/Foundation.Core.ModelElement.name"/>
			</p>
		</xsl:for-each>
	</xsl:template>
</xsl:stylesheet>
