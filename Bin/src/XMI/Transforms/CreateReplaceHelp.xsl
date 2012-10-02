<?xml version="1.0" encoding="UTF-8"?>
<!-- edited with XML Spy v4.4 U (http://www.xmlspy.com) by Larr Hayashi (private) -->
<!--This transform will output HTML Class files for use in the Model Documentation help..-->
<!--IMPORTANT XSLT note. L. Hayashi
As of April 7, 2005 this file requires SAXON XSLT processor because it xsl:document to generate the *.html files. xsl:document is part of the XSLT v1.1 specification which Microsoft currently does not support.
It should also be noted that in the current XSLT v2.0 draft, xsl:document has been replaced by xsl:result-document.
Once XSLT processors such as MSXML or SAXON support this standard, this stylesheet will need to be changed.-->
<!--History
April 7, 2005 L. Hayashi - Added mod attributes to indicate the CellarModule where "referenced" classes come from
Check multiplicity on owning ... refer to LexEntry class.

If there is no comment or documentation, leave out the button.
Refer to backreference to dos
Also inherited attributes?

-->
<!--&#xA0; = newline-->
<xsl:stylesheet version="1.1" xmlns:UML="omg.org/UML/1.4" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
   <xsl:key name="CoreClassID" match="UML:Class" use="@xmi.id"/>
   <xsl:key name="CoreModuleID" match="UML:Package" use="@name"/>
   <xsl:key name="CoreGenID" match="UML:Generalization" use="@xmi.id"/>
   <xsl:key name="ChildGenID" match="UML:Generalization" use="@child"/>
   <xsl:key name="ParentGenID" match="UML:Generalization" use="@parent"/>
   <xsl:key name="CoreDataType" match="UML:DataType" use="@xmi.id"/>
   <xsl:output method="html" version="1.0" encoding="UTF-8"/>
   <xsl:template match="/">
	  <xsl:apply-templates select="//UML:Class"/>
   </xsl:template>
   <xsl:template match="UML:Class">
	  <xsl:document href="XMITempOutputs/ReplaceHelp/{@xmi.id}Report.html">
		 <head>
			<title>
			   <xsl:value-of select="@name"/>
			</title>
			<xsl:comment>
			   <xsl:text>2005 April 28: LHayashi: This class documentation is generated from fw\bin\scr\xmi\fieldworks.xml using createReplaceHelp.xsl and saxon xsl processor.</xsl:text>
			</xsl:comment>
			<style type="text/css">
			   <xsl:comment>
.docs {
font: 12px verdana,arial,helvetica;
}

.docsOpen {
font: 12px verdana,arial,helvetica;
height: auto;
overflow: hidden;
display:"";
}

.docsClosed {
font: 12px verdana,arial,helvetica;
height: 14px;
overflow: hidden;
display:none;
}

</xsl:comment>
			</style>
			<script language="javascript" src="styles.js"/>
		 </head>
		 <body>
			<table>
			   <tr style="background-color: #F5F5DC" class="docs">
				  <td>
					 <p>
						<img border="0" src="Collapsed.gif"/>&#160;<input class="docs" type="button" height="10" value="Show All docs" name="Show All Docs" onclick="showAllDocs()"/>&#160;&#160;&#160;<img border="0" src="Expanded.gif"/>&#160;<input class="docs" type="button" value="Hide All docs" name="Hide All Docs" onclick="hideAllDocs()"/>
					 </p>
				  </td>
				  <td>Or use button on each attribute for individual documentation display (if any).</td>
			   </tr>
			</table>
			<!--Display Class information in a table-->
			<h1>
			   <xsl:value-of select="@name"/>
			</h1>
			<table border="1" cellpadding="2" cellspacing="0" width="100%">
			   <tr>
				  <!--Display class num value-->
				  <td width="70px" align="right">
					 <!--If there are any comments, display a comment button.-->
					 <xsl:if test="UML:ModelElement.comment/UML:Comment/@name">
						<img style="" align="left" alt="test" src="collapsed.gif">
						   <xsl:attribute name="name">ClassDocBlockImage</xsl:attribute>
						   <xsl:attribute name="onmouseup"><xsl:text>expand("ClassDocBlock")</xsl:text></xsl:attribute>
						</img>
					 </xsl:if>
					 <b>num:</b>
					 <xsl:value-of select="UML:ModelElement.taggedValue/UML:TaggedValue[@name='num']/UML:TaggedValue.dataValue"/>
				  </td>
				  <!--Display class abbrev-->
				  <td align="right">&#xA0;<b>abbrev:&#xA0;</b>
					 <xsl:value-of select="UML:ModelElement.taggedValue/UML:TaggedValue[@name='abbrev']/UML:TaggedValue.dataValue"/>
				  </td>
				  <!--Display whether abstract or not-->
				  <td align="right">&#xA0; <b>abstract:&#xA0;</b>
					 <xsl:choose>
						<xsl:when test="@isAbstract">
						   <xsl:value-of select="@isAbstract"/>
						</xsl:when>
						<xsl:otherwise>false</xsl:otherwise>
					 </xsl:choose>
				  </td>
				  <!--Determine and display FW module name (and number) of class-->
				  <td align="right">&#xA0; <b>module:&#xA0;</b>
					 <xsl:for-each select="key('CoreModuleID',../../@name)">
						<a href="{@xmi.id}Report.html">
						   <xsl:value-of select="@name"/>
						   <xsl:text>(num:</xsl:text>
						   <xsl:value-of select="UML:ModelElement.taggedValue/UML:TaggedValue[@name='num']/UML:TaggedValue.dataValue"/>
						   <xsl:text>)</xsl:text>
						</a>
					 </xsl:for-each>
				  </td>
				  <!--Determine and display superclass (base class) of class-->
				  <td align="right">&#xA0; <b>base:&#xA0;</b>
					 <xsl:for-each select="key('ChildGenID', @xmi.id)">
						<xsl:for-each select="key('CoreClassID', @parent)">
						   <a href="{@xmi.id}Report.html">
							  <xsl:value-of select="@name"/>
						   </a>
						</xsl:for-each>
					 </xsl:for-each>
				  </td>
			   </tr>
			   <!--If any documentation for class, output as table row-->
			   <xsl:if test="UML:ModelElement.comment/UML:Comment/@name">
				  <tr class="docsClosed" id="ClassDocBlock">
					 <td align="right">&#160;</td>
					 <td colspan="4">
						<!--Class docs is used to display just one line of documentation in show all documentation mode
					 which is the reason we need this div here.-->
						<div style="margin-left:0px" class="docs">
						   <xsl:value-of select="UML:ModelElement.comment/UML:Comment/@name"/>
						</div>
					 </td>
				  </tr>
			   </xsl:if>
			</table>
			<!-- LIST OF SUBCLASSES IF ANY-->
			<xsl:if test="key('ParentGenID', @xmi.id)">
			   <h2>Direct Subclasses</h2>
			   <ul>
			   <xsl:for-each select="key('ParentGenID', @xmi.id)">
						<xsl:for-each select="key('CoreClassID', @child)">
						   <li>
						   <a href="{@xmi.id}Report.html">
							  <xsl:value-of select="@name"/>
						   </a>
						   </li>
						</xsl:for-each>
					 </xsl:for-each>
			   </ul>
			</xsl:if>
			<!--

			TABLE OF BASIC ATTRIBUTES

			-->
			<h2>Basic Attributes <font size="2">(sorted by attribute name)</font></h2>
			<table width="100%" border="1" cellpadding="2" cellspacing="0">
			   <!--BASIC ATTRIBUTES HERE-->
			   <tr>
				  <th>Type</th>
				  <th>Name</th>
				  <th>Num</th>
				  <th>Signature</th>
				  <th>Other</th>
			   </tr>
			   <!--BASIC TYPE and documentation button if any-->
			   <xsl:for-each select="UML:Classifier.feature/UML:Attribute">
				  <xsl:sort select="@name"/>
				  <tr bgcolor="#FFFFCC">
					 <td width="70px" align="right">
						<xsl:if test="UML:ModelElement.comment/UML:Comment/@name">
						   <img style="" align="left" alt="test" src="collapsed.gif">
							  <xsl:attribute name="name"><xsl:value-of select="@name"/>DocImage</xsl:attribute>
							  <xsl:attribute name="onmouseup"><xsl:text>expand("</xsl:text><xsl:value-of select="@name"/><xsl:text>Doc")</xsl:text></xsl:attribute>
						   </img>
						</xsl:if>Basic
				  </td>
					 <!--Basic attribute name-->
					 <td>
						<xsl:attribute name="style"><xsl:text>{font-weight:bold}</xsl:text></xsl:attribute>
						<xsl:value-of select="@name"/>
					 </td>
					 <!--Basic attribute num-->
					 <td>
						<xsl:value-of select="UML:ModelElement.taggedValue/UML:TaggedValue[@name='num']/UML:TaggedValue.dataValue"/>
					 </td>
					 <!--Basic attribute data type-->
					 <td>
						<xsl:for-each select="key('CoreDataType',@type)">
						   <xsl:choose>
							  <!--In MagicDraw, I created basic datatypes with lower case signatures so that type ahead would only select these basic datatypes rather than the uppercase class names as well-->
							  <xsl:when test="@name[.='boolean']">
								 <xsl:text>Boolean</xsl:text>
							  </xsl:when>
							  <xsl:when test="@name[.='genDate']">
								 <xsl:text>GenDate</xsl:text>
							  </xsl:when>
							  <xsl:when test="@name[.='void']">
								 <xsl:text>Void</xsl:text>
							  </xsl:when>
							  <xsl:when test="@name[.='unicode']">
								 <xsl:text>Unicode</xsl:text>
							  </xsl:when>
							  <xsl:when test="@name[.='numeric']">
								 <xsl:text>Numeric</xsl:text>
							  </xsl:when>
							  <xsl:when test="@name[.='binary']">
								 <xsl:text>Binary</xsl:text>
							  </xsl:when>
							  <!--textPropBinary was added to help FDO efficiently deal with binary signatures. But for *.cm purposes, we change it to Binary, but in stage 3 [TODO: this comment wasn't finished, please clarify] -->
							  <xsl:when test="@name[.='textPropBinary']">
								 <xsl:text>TextPropBinary</xsl:text>
							  </xsl:when>
							  <xsl:when test="@name[.='guid']">
								 <xsl:text>Guid</xsl:text>
							  </xsl:when>
							  <xsl:when test="@name[.='integer']">
								 <xsl:text>Integer</xsl:text>
							  </xsl:when>
							  <xsl:when test="@name[.='multiString']">
								 <xsl:text>MultiString</xsl:text>
							  </xsl:when>
							  <xsl:when test="@name[.='image']">
								 <xsl:text>Image</xsl:text>
							  </xsl:when>
							  <xsl:when test="@name[.='float']">
								 <xsl:text>Float</xsl:text>
							  </xsl:when>
							  <xsl:when test="@name[.='multiUnicode']">
								 <xsl:text>MultiUnicode</xsl:text>
							  </xsl:when>
							  <xsl:when test="@name[.='string']">
								 <xsl:text>String</xsl:text>
							  </xsl:when>
							  <xsl:when test="@name[.='time']">
								 <xsl:text>Time</xsl:text>
							  </xsl:when>
							  <!--Check for sig and adapt to big for: string, multiString, unicode, multiUnicode-->
							  <xsl:when test="@name[.='bigString']">
								 <xsl:text>String</xsl:text>
							  </xsl:when>
							  <xsl:when test="@name[.='multiBigString']">
								 <xsl:text>MultiString</xsl:text>
							  </xsl:when>
							  <xsl:when test="@name[.='bigUnicode']">
								 <xsl:text>Unicode</xsl:text>
							  </xsl:when>
							  <xsl:when test="@name[.='multiBigUnicode']">
								 <xsl:text>MultiUnicode</xsl:text>
							  </xsl:when>
							  <xsl:otherwise>
								 <xsl:value-of select="@name"/>
							  </xsl:otherwise>
						   </xsl:choose>
						   <!--Add Big attribute for Big signatures-->
						   <xsl:if test="contains(@name, 'big') or contains(@name, 'Big')">
							  <xsl:text>-big</xsl:text>
						   </xsl:if>
						</xsl:for-each>
					 </td>
					 <td>
						<!--For Basic Integer signatures check for min and max values if any
								 min value goes here-->
						<xsl:for-each select="UML:ModelElement.taggedValue/UML:TaggedValue[@name='min']">
min: <xsl:value-of select="./UML:TaggedValue.dataValue"/>
						</xsl:for-each>
					 </td>
					 <!-- Basic integer max value if any-->
					 <td>
						<xsl:for-each select="UML:ModelElement.taggedValue/UML:TaggedValue[@name='max']">
		 max:<xsl:value-of select="./UML:TaggedValue.dataValue"/>
						</xsl:for-each>
					 </td>
				  </tr>
				  <xsl:if test="UML:ModelElement.comment/UML:Comment/@name">
					 <tr class="docsClosed">
						<xsl:attribute name="id"><xsl:value-of select="@name"/><xsl:text>Doc</xsl:text></xsl:attribute>
						<td align="right">&#160;</td>
						<td colspan="4">
						   <div style="margin-left:0px">
							  <xsl:value-of select="UML:ModelElement.comment/UML:Comment/@name"/>
						   </div>
						</td>
					 </tr>
				  </xsl:if>
			   </xsl:for-each>
			</table>
			<h2>Owning and reference attributes <font size="2">(sorted by type and then attribute name)</font></h2>
			<!--OWNING AND REFERENCE ATTRIBUTES TABLE-->
			<table width="100%" border="1" cellpadding="2" cellspacing="0">
			   <tr>
				  <th>Type</th>
				  <th>Name</th>
				  <th>Num</th>
				  <th>Card</th>
				  <th>Sig</th>
			   </tr>
			   <!--Owning and references for a class are stored as associations with the class xmi.id being in position 1 of the association
			   and the signature class in position 2. Owning are unique in that their @aggregation is set to "composite".-->
			   <xsl:variable name="ClassGUID" select="@xmi.id"/>
			   <xsl:for-each select="//UML:Association/UML:Association.connection/UML:AssociationEnd[position()=1 and @participant=$ClassGUID]">
				  <xsl:sort select="@aggregation" order="descending"/>
				  <xsl:sort select="../../@name"/>
				  <!--Determine if owning or reference and set backcolor and label accordingly-->
				  <xsl:variable name="vBKCOLOR">
					 <xsl:choose>
						<xsl:when test="@aggregation='composite'">
						   <xsl:text>#F0F8FF</xsl:text>
						</xsl:when>
						<xsl:otherwise>
						   <xsl:text>#CCFFFF</xsl:text>
						</xsl:otherwise>
					 </xsl:choose>
				  </xsl:variable>
				  <xsl:variable name="vLABEL">
					 <xsl:choose>
						<xsl:when test="@aggregation='composite'">
						   Owning
						</xsl:when>
						<xsl:otherwise>Refer</xsl:otherwise>
					 </xsl:choose>
				  </xsl:variable>
				  <!--Row for attribute with bgcolor set-->
				  <tr bgcolor="{$vBKCOLOR}">
					 <!--Each image is given an individual name so that the user can toggle documentation for a particular attribute-->
					 <td width="70px" align="right">
						<xsl:if test="../../UML:ModelElement.comment/UML:Comment/@name">
						   <img style="" align="left" alt="test" src="collapsed.gif" name="{../../@name}DocImage" onmouseup="expand(&#34;{../../@name}Doc&#34;)"/>
						</xsl:if>
						<xsl:value-of select="$vLABEL"/>
					 </td>
					 <td>
						<xsl:attribute name="style"><xsl:text>{font-weight:bold}</xsl:text></xsl:attribute>
						<xsl:value-of select="../../@name"/>
					 </td>
					 <td>
						<xsl:value-of select="../../UML:ModelElement.taggedValue/UML:TaggedValue[@name='num']/UML:TaggedValue.dataValue"/>
					 </td>
					 <xsl:call-template name="SetCardSig"/>
				  </tr>
				  <xsl:if test="../../UML:ModelElement.comment/UML:Comment/@name">
					 <tr class="docsClosed" id="{../../@name}Doc">
						<td align="right">&#160;</td>
						<td colspan="4">
						   <div style="margin-left: 0">
							  <xsl:value-of select="../../UML:ModelElement.comment/UML:Comment/@name"/>
						   </div>
						</td>
					 </tr>
				  </xsl:if>
			   </xsl:for-each>
			</table>
			<!--BACK REFERENCES HERE-->
			<!--BACK REFERENCES HERE-->
			<!--BACK REFERENCES HERE-->
			<h2>Back references <font size="2">(sorted by attribute name)</font></h2>
			<!--OWNED BY AND REFER BY ATTRIBUTES HERE-->
			<table width="100%" border="1" cellpadding="2" cellspacing="0">
			   <tr>
				  <th>Type</th>
				  <th>Name</th>
				  <th>Num</th>
				  <th>Card</th>
				  <th>Sig</th>
			   </tr>
			   <xsl:variable name="ClassGUID" select="@xmi.id"/>
			   <xsl:for-each select="//UML:Association/UML:Association.connection/UML:AssociationEnd[position()=2 and @participant=$ClassGUID]">
				  <xsl:sort select="../../@name"/>
				  <!--Determine if owning or reference and set backcolor and label accordingly-->
				  <xsl:variable name="vBKCOLOR">
					 <xsl:choose>
						<xsl:when test="../UML:AssociationEnd[position()=1]/@aggregation='composite'">
						   <xsl:text>##90EE90</xsl:text>
						</xsl:when>
						<xsl:otherwise>
						   <xsl:text>##FFCC99</xsl:text>
						</xsl:otherwise>
					 </xsl:choose>
				  </xsl:variable>
				  <xsl:variable name="vLABEL">
					 <xsl:choose>
						<xsl:when test="../UML:AssociationEnd[position()=1]/@aggregation='composite'">
						   Owned by
						</xsl:when>
						<xsl:otherwise>Refer'd by</xsl:otherwise>
					 </xsl:choose>
				  </xsl:variable>
				  <!--Row for attribute with bgcolor set-->
				  <tr bgcolor="{$vBKCOLOR}">
					 <!--Each image is given an individual name so that the user can toggle documentation for a particular attribute-->
					 <td width="95px" align="right">
						<xsl:if test="../../UML:ModelElement.comment/UML:Comment/@name">
						   <img style="" align="left" alt="test" src="collapsed.gif" name="{../../@name}DocImage" onmouseup="expand(&#34;{../../@name}Doc&#34;)"/>
						</xsl:if>
						<xsl:value-of select="$vLABEL"/>
					 </td>
					 <td>
						<xsl:attribute name="style"><xsl:text>{font-weight:bold}</xsl:text></xsl:attribute>
						<xsl:variable name="BackRefClassGUID" select="../UML:AssociationEnd[position()=1]/@participant"/>
						<xsl:for-each select="key('CoreClassID', $BackRefClassGUID)">
						   <a href="{@xmi.id}Report.html">
							  <xsl:value-of select="@name"/>
						   </a>
						</xsl:for-each>
						<xsl:text>:</xsl:text>
						<xsl:value-of select="../../@name"/>
					 </td>
					 <td>
						<xsl:value-of select="../../UML:ModelElement.taggedValue/UML:TaggedValue[@name='num']/UML:TaggedValue.dataValue"/>
					 </td>
					 <xsl:call-template name="SetCardSig"/>
				  </tr>
				  <xsl:if test="../../UML:ModelElement.comment/UML:Comment/@name">
					 <tr class="docsClosed" id="{../../@name}Doc">
						<td align="right">&#160;</td>
						<td colspan="4">
						   <div style="margin-left: 0">
							  <xsl:value-of select="../../UML:ModelElement.comment/UML:Comment/@name"/>
						   </div>
						</td>
					 </tr>
				  </xsl:if>
			   </xsl:for-each>
			</table>
		 </body>
	  </xsl:document>
   </xsl:template>
   <xsl:template name="SetCardSig">
	  <!-- Now that we have the association we need to look at the end point to see
		   the nature of the cardinality and its signature-->
	  <!--We get the endpoint by going to the second association end in the assoication connection-->
	  <xsl:for-each select="../UML:AssociationEnd[position()=2]">
		 <xsl:variable name="multiplicityUpper" select="UML:AssociationEnd.multiplicity/UML:Multiplicity/UML:Multiplicity.range/UML:MultiplicityRange/@upper"/>
		 <td>
			<xsl:choose>
			   <!--We test the multiplicity upper bounds. If it is equal to 1 we know the card is atomic,
				   otherwise it is either seq or card.-->
			   <xsl:when test="$multiplicityUpper = 1">
				  <xsl:text>atomic</xsl:text>
			   </xsl:when>
			   <xsl:otherwise>
				  <xsl:choose>
					 <!--If the ordering is ordered then we know the attribute is not collection
						 but a sequence (ordered collection) the default is NULL so we can test for bad input-->
					 <xsl:when test="@ordering='ordered'">
						<xsl:text>sequence</xsl:text>
					 </xsl:when>
					 <xsl:when test="@ordering='unordered'">
						<xsl:text>collection</xsl:text>
					 </xsl:when>
					 <xsl:otherwise>
						<xsl:text>collection</xsl:text>
					 </xsl:otherwise>
				  </xsl:choose>
			   </xsl:otherwise>
			</xsl:choose>
		 </td>
		 <!-- Here we get the signature class GUID -->
		 <td>
			<xsl:for-each select="key('CoreClassID',@participant)">
			   <a href="{@xmi.id}Report.html">
				  <xsl:value-of select="@name"/>
			   </a>
			</xsl:for-each>
		 </td>
	  </xsl:for-each>
   </xsl:template>
</xsl:stylesheet>
