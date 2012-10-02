<?xml version="1.0"?>
<!-- edited with XML Spy v3.5 NT (http://www.xmlspy.com) by Larry Hayashi (SIL International) -->
<!--
*********** PURPOSE **************
Purpose: to convert XMI file to Cellar3 class file. Creates bin\src\xmi\transforms\xmiTempOutputs\xmi2cellar1.xml.

*********** XSL Stylesheet documentation: *****************
HISTORY:
April 17, 2002 L. Hayashi Added new attribute to Class that indicates the CellarModule of the class (modNum=num of the module to which a class belongs plus three zeros).
	This was added for John Hatton to add additional information to FDO.

June 12, 2002 L.Hayashi Above change was commented out John doesn't need it for now.
June 12, 2002 L. Hayashi Made some changes to speed up creation of this file.
February 4, 2005 L. Hayashi and M. Bostrom changed to match the new MagicDraw version 8.0 output (numerous XPATH statements had to change and some other minor adjustments).  M. Bostrom changed some of the xsl syntax to a more concise format where attributes are written directly with braces operators to reference their values. (Note that this syntax should only be used for single required attributes. It should not be used for attributes that might be implied or a spurious NULL value will be generated for the attribute.)  M.Bostrom also made the lines involving 'key' statements more concise and moved some redundant xsl lines to a new called template named "SetCardSig".
************* To do: *******************
-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:UML="omg.org/UML/1.4" exclude-result-prefixes="UML">
   <xsl:key name="CoreClassID" match="UML:Class" use="@xmi.id"/>
   <xsl:key name="CoreGenID" match="UML:Generalization" use="@xmi.id"/>
   <xsl:key name="CoreDataType" match="UML:DataType" use="@xmi.id"/>
   <xsl:output method="xml" version="1.0" encoding="UTF-8"/>
   <xsl:template match="/">
	  <EntireModel>
		 <xsl:for-each select="/XMI/XMI.content/UML:Model/UML:Namespace.ownedElement/UML:Package/UML:Namespace.ownedElement/UML:Package">
			<CellarModule
			   id="{@name}"
			   num="{UML:ModelElement.taggedValue/UML:TaggedValue[@name='num']/UML:TaggedValue.dataValue}"
			   ver="{UML:ModelElement.taggedValue/UML:TaggedValue[@name='ver']/UML:TaggedValue.dataValue}"
		   verBack="{UML:ModelElement.taggedValue/UML:TaggedValue[@name='verBack']/UML:TaggedValue.dataValue}">

			   <!--********CLASSES BEGIN HERE********-->
			   <xsl:for-each select="UML:Namespace.ownedElement/UML:Class">
				  <class
					 num="{UML:ModelElement.taggedValue/UML:TaggedValue[@name='num']/UML:TaggedValue.dataValue}"
					 id="{@name}"
					 abstract="{@isAbstract}">
					 <xsl:for-each select="UML:ModelElement.taggedValue/UML:TaggedValue[@name='abbrev']">
						<xsl:attribute name="abbr"><xsl:value-of select="./UML:TaggedValue.dataValue"/></xsl:attribute>
					 </xsl:for-each>
			 <!-- Get the base object, if it exists, using the parent GUID in the particular
						  Generalization that the current generalization GUID points to-->
			 <xsl:for-each select="key('CoreClassID',key('CoreGenID',@generalization)/@parent)">
						<xsl:attribute name="base"><xsl:value-of select="@name"/></xsl:attribute>
			<!--Here we test if the base is CmObject. If so, depth="0", if not then we set the depth to 1 and find out the Base of the Base and increment depth for every layer of inheritance-->
						<xsl:call-template name="GetSuperClass">
						   <xsl:with-param name="depth" select="0"/>
						</xsl:call-template>
					 </xsl:for-each>

					 <!--********************Basic PROPERTIES BEGIN HERE***********************-->
					 <props>
						<xsl:for-each select="UML:Classifier.feature/UML:Attribute">
						   <basic
							  num="{UML:ModelElement.taggedValue/UML:TaggedValue[@name='num']/UML:TaggedValue.dataValue}"
							  id="{@name}">
							  <!-- Test for what the value of the signature is and change to upper case and remove big as separate attribute-->
				  <!-- Look up type signature using the GUID in the current 'type' attribute-->
				  <xsl:for-each select="key('CoreDataType',@type)">
				 <xsl:attribute name="sig">
									<xsl:choose>
									   <!--In MagicDraw, I created basic datatypes with lower case signatures so that type ahead would only select these basic datatypes rather than the uppercase class names as well-->
									   <xsl:when test="@name[.='boolean']"><xsl:text>Boolean</xsl:text></xsl:when>
									   <xsl:when test="@name[.='genDate']"><xsl:text>GenDate</xsl:text></xsl:when>
									   <xsl:when test="@name[.='void']"><xsl:text>Void</xsl:text></xsl:when>
									   <xsl:when test="@name[.='unicode']"><xsl:text>Unicode</xsl:text></xsl:when>
									   <xsl:when test="@name[.='numeric']"><xsl:text>Numeric</xsl:text></xsl:when>
									   <xsl:when test="@name[.='binary']"><xsl:text>Binary</xsl:text></xsl:when>
					   <!--textPropBinary was added to help FDO efficiently deal with binary signatures. But for *.cm purposes, we change it to Binary, but in stage 3 [TODO: this comment wasn't finished, please clarify] -->
									   <xsl:when test="@name[.='textPropBinary']"><xsl:text>TextPropBinary</xsl:text></xsl:when>
									   <xsl:when test="@name[.='guid']"><xsl:text>Guid</xsl:text></xsl:when>
									   <xsl:when test="@name[.='integer']"><xsl:text>Integer</xsl:text></xsl:when>
									   <xsl:when test="@name[.='multiString']"><xsl:text>MultiString</xsl:text></xsl:when>
									   <xsl:when test="@name[.='image']"><xsl:text>Image</xsl:text></xsl:when>
									   <xsl:when test="@name[.='float']"><xsl:text>Float</xsl:text></xsl:when>
									   <xsl:when test="@name[.='multiUnicode']"><xsl:text>MultiUnicode</xsl:text></xsl:when>
									   <xsl:when test="@name[.='string']"><xsl:text>String</xsl:text></xsl:when>
									   <xsl:when test="@name[.='time']"><xsl:text>Time</xsl:text></xsl:when>
									   <!--Check for sig and adapt to big for: string, multiString, unicode, multiUnicode-->
									   <xsl:when test="@name[.='bigString']"><xsl:text>String</xsl:text></xsl:when>
									   <xsl:when test="@name[.='multiBigString']"><xsl:text>MultiString</xsl:text></xsl:when>
									   <xsl:when test="@name[.='bigUnicode']"><xsl:text>Unicode</xsl:text></xsl:when>
									   <xsl:when test="@name[.='multiBigUnicode']"><xsl:text>MultiUnicode</xsl:text></xsl:when>
					   <xsl:otherwise><xsl:value-of select="@name"/></xsl:otherwise>
									</xsl:choose>
				 </xsl:attribute>
								 <!--Add Big attribute for Big signatures-->
								 <xsl:if test="contains(@name, 'big') or contains(@name, 'Big')">
									<xsl:attribute name="big"><xsl:text>true</xsl:text></xsl:attribute>
								 </xsl:if>
							  </xsl:for-each>
							  <!--For Integer signatures set min and max values if any-->
							  <xsl:for-each select="UML:ModelElement.taggedValue/UML:TaggedValue[@name='min']">
								 <xsl:attribute name="min"><xsl:value-of select="./UML:TaggedValue.dataValue"/></xsl:attribute>
							  </xsl:for-each>
							  <xsl:for-each select="UML:ModelElement.taggedValue/UML:TaggedValue[@name='max']">
								 <xsl:attribute name="max"><xsl:value-of select="./UML:TaggedValue.dataValue"/></xsl:attribute>
							  </xsl:for-each>
						   </basic>
						</xsl:for-each>
						<!--**********************OWNING PROPERTIES START HERE ****************************-->
			<!--Owning properties in UML XMI are expressed as associations with source end having a
							composite aggregation starting at this class. We first need to find all the associations
							(which are not contained in the CoreClass unfortunately) that have a composite end with this classGUID-->
						<xsl:variable name="ClassGUID" select="@xmi.id"/>
						<xsl:for-each select="//UML:Association/UML:Association.connection/UML:AssociationEnd[position()=1 and @participant=$ClassGUID]">
						   <!--OWNING REALLY STARTS HERE-->
						   <xsl:if test="@aggregation='composite'">
							  <owning
								 num="{../../UML:ModelElement.taggedValue/UML:TaggedValue[@name='num']/UML:TaggedValue.dataValue}"
								 id="{../../@name}">
				 <xsl:call-template name="SetCardSig"></xsl:call-template>
							  </owning>
						   </xsl:if>
						   <!--REFERENCE REALLY STARTS HERE-->
						   <xsl:if test="@aggregation='none'">
							  <rel
								 num="{../../UML:ModelElement.taggedValue/UML:TaggedValue[@name='num']/UML:TaggedValue.dataValue}"
								 id="{../../@name}">
				 <xsl:call-template name="SetCardSig"></xsl:call-template>
							  </rel>
						   </xsl:if>
						</xsl:for-each>
					 </props>
				  </class>
			   </xsl:for-each>
			</CellarModule>
		 </xsl:for-each>
	  </EntireModel>
   </xsl:template>
   <xsl:template name="GetSuperClass"><xsl:param name="depth"/>
	  <xsl:choose>
		 <xsl:when test="@name='CmObject'">
			<xsl:attribute name="depth"><xsl:value-of select="$depth"/></xsl:attribute>
		 </xsl:when>
		 <xsl:otherwise>
			<!-- Get the base object of the current object -->
			<xsl:for-each select="key('CoreClassID',key('CoreGenID',@generalization)/@parent)">
			   <!--Here we test if the base is CmObject. If so, depth="1", if not then we set the depth to 2
				  and find out the Base of the Base and increment depth for every layer of inheritance-->
			   <xsl:call-template name="GetSuperClass"><xsl:with-param name="depth" select="$depth+1"/>
			   </xsl:call-template>
			</xsl:for-each>
		 </xsl:otherwise>
	  </xsl:choose>
   </xsl:template>
   <xsl:template name="SetCardSig">
	  <!-- Now that we have the association we need to look at the end point to see
		   the nature of the cardinality and its signature-->
	  <!--We get the endpoint by going to the second association end in the assoication connection-->
	  <xsl:for-each select="../UML:AssociationEnd[position()=2]">
		 <xsl:variable name="multiplicityUpper" select="UML:AssociationEnd.multiplicity/UML:Multiplicity/UML:Multiplicity.range/UML:MultiplicityRange/@upper"/>
		 <xsl:attribute name="card">
			<xsl:choose>
			   <!--We test the multiplicity upper bounds. If it is equal to 1 we know the card is atomic,
				   otherwise it is either seq or card.-->
			   <xsl:when test="$multiplicityUpper = 1"><xsl:text>atomic</xsl:text></xsl:when>
			   <xsl:otherwise>
				  <xsl:choose>
					 <!--If the ordering is ordered then we know the attribute is not collection
						 but a sequence (ordered collection) the default is NULL so we can test for bad input-->
					 <xsl:when test="@ordering='ordered'"><xsl:text>seq</xsl:text></xsl:when>
					 <xsl:when test="@ordering='unordered'"><xsl:text>col</xsl:text></xsl:when>
					 <xsl:otherwise></xsl:otherwise>
				  </xsl:choose>
			   </xsl:otherwise>
			</xsl:choose>
		 </xsl:attribute>
		 <!-- Here we get the signature class GUID -->
		 <xsl:for-each select="key('CoreClassID',@participant)">
			<xsl:attribute name="sig"><xsl:value-of select="@name"/></xsl:attribute>
		 </xsl:for-each>
	  </xsl:for-each>
   </xsl:template>
</xsl:stylesheet>
