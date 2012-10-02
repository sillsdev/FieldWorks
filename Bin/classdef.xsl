<?xml version='1.0'?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/TR/WD-xsl">

<xsl:template><xsl:apply-templates/></xsl:template>

<xsl:template match="text()"><xsl:value-of select="."/></xsl:template>

<xsl:template match="/">



<html>


<head>
<style>

<!--
   A:hover {color:red;  text-decoration:none;}
   A:link  {color:navy; text-decoration:none;}
   A:visited  {color:navy;  text-decoration:none;}
.Hm{ background-color: rgb(192,192,192); border-top: 2px solid; border-bottom: 2px solid }
.Hd{ background-color: rgb(192,192,192) }
.R1 { margin-top: 0px, margin-left: 25px }
.indent1 { margin-left: 25px }
.indent2 { margin-left: 50px }
-->

</style>
<title><xsl:value-of select="class/@id"/></title></head>

<script>
				function getDirFromSignature(sSig)
				{
					var prefix = sSig.substr(0, 2);
					var sDir="?WHERE?";
					if (sSig == "Location") {sDir =  "Notebk";}
					else if (sSig == "ParticipantWithRoles") {sDir =  "Notebk";}
					else if (sSig == "Person") {sDir =  "Notebk";}
					else if (sSig == "Reminder") {sDir =  "Notebk";}
					else if (sSig == "LangProject") {sDir =  "LangProj";}
					else if (sSig == "LgEncoding") {sDir =  "LangProj";}
					else if (sSig == "LpTranslation") {sDir =  "Ling";}
					else if (sSig == "PartOfSpeech") {sDir =  "Ling";}

					else
					{
						if (prefix == "Cm") {sDir =  "Cellar";}
						if (prefix == "Fs") {sDir =  "FeatSys";}
						if (prefix == "La") {sDir =  "LangProj";}
						if (prefix == "Lg") {sDir =  "LangProj";}
						if (prefix == "Lex") {sDir =  "Ling";}
						if ( (prefix == "Mo") || (prefix == "Ph") || (prefix == "Re") )
							{sDir =  "Ling";}
						if (prefix == "Rn") {sDir =  "Notebk";}
						if (prefix == "St") {sDir =  "Cellar";}
						if (prefix == "Tx") {sDir =  "Ling";}
						if (prefix == "Te") {sDir =  "Ling";}
						if (prefix == "Wf") {sDir =  "Ling";}
						if (prefix == "Wo") {sDir =  "Ling";}
					}
					return sDir;
				}

				function goClassPage(sSig)
				{	var test = (sSig == "Time")||(sSig == "Integer") || (sSig == "Boolean") || (sSig == "Guid")  || (sSig == "Image") || (sSig == "Numeric") || (sSig == "Float") ||(sSig == "String") || (sSig == "MultiString") || (sSig == "Unicode") || (sSig == "MultiUnicode") || (sSig == "EncUnicode") || (sSig == "GenDate") || (sSig == "Binary");
					if (test)
						{
						window.location="../../../Doc/primitivesonly.htm";
						window.event.returnValue = false;
						}
					else
					{
					var sDir= getDirFromSignature(sSig);

					window.location = "../../"+sDir+"/xml/"+sSig+".xml";
					//window.location="http://www.yahoo.com";
					//alert("location="+window.location);
					window.event.returnValue = false;  // make the A element ignore the href value
					}
				}
</script>


<body background="..\..\..\Doc\Background.gif">
  <table border="1" width="100%" cellpadding="2" cellspacing="0">
	<tr>
			<td width="50%" colspan="5">
			<h1 align="center"><b><xsl:value-of select="$ClassId"/></b></h1>
			<p align="center"><b>Base: </b>
			<A>
			<xsl:attribute name="href">
				{$destinationDir}/<xsl:value-of select="$BaseObjectGeneralizationParentGUID">Report.html</xsl:value-of>
				</xsl:attribute>
			<xsl:value-of select="$ClassBase"/>
			</A>
			| <b>Abstract:</b> <xsl:choose><xsl:when test="$ClassAbstract[.='true']">true</xsl:when>
			<xsl:otherwise>false</xsl:otherwise></xsl:choose>
			| <b>Abbrev: </b><xsl:value-of select="$ClassAbbr"/>
			| <b>Num: </b><xsl:value-of select="$ClassNum"/>
			</p>
		</td>
	</tr>
</table>
<p></p>

<!-- ******************** Basic Properties Table ****************-->
<xsl:if test="class/props/basic">
<table border="1" width="100%" cellpadding="2" cellspacing="0">
	<tr>
			<td width="2%"><b>Num</b></td>
			<td width="40%"><b>Basic Properties</b></td>
			<td width="8%"><b>NA</b></td>
		<td width="10%"><b>Big</b></td>
			<td width="30%"><b>Signature</b></td>
	</tr>
	   <xsl:for-each select="class/props/basic" >
	   <tr bgcolor="#C0C0C0">
	<td width="2%">
		<xsl:value-of select="@num" />
	   </td>
	<td width="40%" >
		<A>
			<xsl:attribute name="href">#<xsl:value-of select="@id" /></xsl:attribute><xsl:value-of select="@id"/>
		</A>
	</td>
	   <td width="8%">
		NA
	</td>
	<td width="10%">
		<xsl:choose>
			<xsl:when test="@big[. = 'true']">
					true
					</xsl:when>
			<xsl:otherwise>
					false
				</xsl:otherwise>
		</xsl:choose>
	</td>
	<td width="30%" >
		<xsl:element name="A">
			<xsl:attribute name="href">
			</xsl:attribute>
			<xsl:attribute name="onclick">
				goClassPage(&quot;<xsl:value-of select="@sig"/>&quot;)
			</xsl:attribute>
			<xsl:value-of select="@sig" />
		</xsl:element>
	</td>
	</tr>
	</xsl:for-each>
</table>
</xsl:if>
<!-- ******************** Owning Properties Table ****************-->
<xsl:if test="class/props/owning">
<table border="1" width="100%" cellpadding="2" cellspacing="0">
	<tr>
			<td width="2%"><b>Num</b></td>
			<td width="40%"><b>Owning Properties</b></td>
			<td width="8%"><b>Card</b></td>
		<td width="10%"><b>Kind</b></td>
			<td width="30%"><b>Signature</b></td>
	</tr>
	   <xsl:for-each select="class/props/owning" >
	   <tr bgcolor="#00FFCC">
	<td width="2%">
		<xsl:value-of select="@num" />
	   </td>
	<td width="40%" >
		<A>
			<xsl:attribute name="href">
					#<xsl:value-of select="@id" />
			</xsl:attribute>
		<xsl:value-of select="@id"/>
		</A>
	</td>
	   <td width="8%">
		<xsl:choose>
			<xsl:when test="@card[. = 'seq']">
					seq
					</xsl:when>
			<xsl:when test="@card[. = 'col']">
					col
					</xsl:when>
				<xsl:otherwise>
					atomic
				</xsl:otherwise>
		</xsl:choose>
	</td>
	<td width="10%">
		NA
	</td>
	<td width="30%" >
		<xsl:element name="A">
			<xsl:attribute name="href">
			</xsl:attribute>
			<xsl:attribute name="onclick">
				goClassPage(&quot;<xsl:value-of select="@sig"/>&quot;)
			</xsl:attribute>
			<xsl:value-of select="@sig" />
		</xsl:element>

	</td>
	</tr>
	</xsl:for-each>
</table>
</xsl:if>
<!-- ******************** Reference Properties Table ****************-->
<xsl:if test="class/props/rel">
<table border="1" width="100%" cellpadding="2" cellspacing="0">
	<tr>
			<td width="2%"><b>Num</b></td>
			<td width="40%"><b>Reference Properties</b></td>
			<td width="8%"><b>Card</b></td>
		<td width="10%"><b>Kind</b></td>
			<td width="30%"><b>Signature</b></td>
	</tr>
	   <xsl:for-each select="class/props/rel" >
	   <tr bgcolor="#FFFFCC">
	<td width="2%">
		<xsl:value-of select="@num" />
	   </td>
	<td width="40%" >
		<A>
			<xsl:attribute name="href">
					#<xsl:value-of select="@id" />
			</xsl:attribute>
		<xsl:value-of select="@id"/>
		</A>
	</td>
	<td width="8%">
		<xsl:choose>
			<xsl:when test="@card[. = 'seq']">
					seq
					</xsl:when>
			<xsl:when test="@card[. = 'col']">
					col
					</xsl:when>
				<xsl:otherwise>
					atomic
				</xsl:otherwise>
		</xsl:choose>
	</td>
	<td width="10%">
		<xsl:choose>
			<xsl:when test="@kind[. = 'reference']">
					reference
					</xsl:when>
			<xsl:otherwise>
					reference
				</xsl:otherwise>
		</xsl:choose>
	</td>
	<td width="30%" >
			<xsl:element name="A">
			<xsl:attribute name="href">
			</xsl:attribute>
			<xsl:attribute name="onclick">
				goClassPage(&quot;<xsl:value-of select="@sig"/>&quot;)
			</xsl:attribute>
			<xsl:value-of select="@sig" />
		</xsl:element>
	</td>
	</tr>
	</xsl:for-each>
</table>
</xsl:if>
<!-- ******************** Class Descriptions  ****************-->
<b>Diagram:</b><a href="..\..\..\Doc\CmHtml\index.htm">Index | </a>
<xsl:for-each select="class/descr/submod">
<xsl:choose>
<xsl:when test="@type[. = 'Schema.Model']">


	<xsl:for-each select="p">
			<A>
			<xsl:attribute name="href">
					..\..\..\Doc\CmHtml\<xsl:value-of/>.htm
			</xsl:attribute>
			<xsl:value-of/>
			</A> |
	</xsl:for-each>

</xsl:when><p></p>

<xsl:when test="@type[. = 'Description']">
	<b>Description:</b>
	<xsl:for-each>
		<xsl:value-of/>
	</xsl:for-each>
</xsl:when>

<xsl:otherwise>
	<b><xsl:value-of select="@type" />:</b>
	<xsl:for-each>
		<xsl:value-of/><p></p>
	</xsl:for-each>
</xsl:otherwise>
</xsl:choose>

</xsl:for-each>

<hr></hr>
<ul>
<!-- ******************** Basic Properties Detail ****************-->
 <xsl:for-each select="class/props/basic" >
	<li>
		<b>
		<a>
		<xsl:attribute name="name"><xsl:value-of select="@id" /></xsl:attribute><xsl:value-of select="@id"/>
		</a>
		</b>
		(
		<i>
		<xsl:choose>
			<xsl:when test="@big[. = 'true']">
					big
					</xsl:when>
			<xsl:otherwise>
				</xsl:otherwise>
		</xsl:choose>
			<xsl:element name="A">
			<xsl:attribute name="href">
			</xsl:attribute>
			<xsl:attribute name="onclick">
				goClassPage(&quot;<xsl:value-of select="@sig"/>&quot;)
			</xsl:attribute>
			<xsl:value-of select="@sig" />
		</xsl:element>
		</i>
		) -
		<xsl:for-each select="descr">
			<xsl:apply-templates></xsl:apply-templates>
		</xsl:for-each>
	</li>

</xsl:for-each>



<!-- ******************** Owning Properties Detail ****************-->
 <xsl:for-each select="class/props/owning" >

	<li>
		<b>
		<a>
		<xsl:attribute name="name"><xsl:value-of select="@id" /></xsl:attribute><xsl:value-of select="@id"/>
		</a>
		</b>
		(
		<i>
		<xsl:choose>
			<xsl:when test="@card[. = 'seq']">
					seq of
					</xsl:when>
			<xsl:when test="@card[. = 'col']">
					col of
					</xsl:when>
				<xsl:otherwise>
					atomic
				</xsl:otherwise>
		</xsl:choose>
			<xsl:element name="A">
			<xsl:attribute name="href">
			</xsl:attribute>
			<xsl:attribute name="onclick">
				goClassPage(&quot;<xsl:value-of select="@sig"/>&quot;)
			</xsl:attribute>
			<xsl:value-of select="@sig" />
		</xsl:element>
	</i>
		) -
		<xsl:for-each select="descr">
			<xsl:apply-templates></xsl:apply-templates>
		</xsl:for-each>
	</li>

</xsl:for-each>






<!-- ******************** Reference Properties Detail ****************-->
 <xsl:for-each select="class/props/rel" >
<li>
		<b>
		<a>
		<xsl:attribute name="name"><xsl:value-of select="@id" /></xsl:attribute><xsl:value-of select="@id"/>
		</a>
		</b>
		(
		<i>
		<xsl:choose>
			<xsl:when test="@kind[. = 'reference']">
					refers to
					</xsl:when>
			<xsl:otherwise>
					refers to
				</xsl:otherwise>
		</xsl:choose>
		<xsl:choose>
			<xsl:when test="@card[. = 'seq']">
					seq of
					</xsl:when>
			<xsl:when test="@card[. = 'col']">
					col of
					</xsl:when>
				<xsl:otherwise>
					atomic
				</xsl:otherwise>
		</xsl:choose>

			<xsl:element name="A">
			<xsl:attribute name="href">
			</xsl:attribute>
			<xsl:attribute name="onclick">
				goClassPage(&quot;<xsl:value-of select="@sig"/>&quot;)
			</xsl:attribute>
			<xsl:value-of select="@sig" />
		</xsl:element>
	</i>
		) -
		<xsl:for-each select="descr">
			<xsl:apply-templates></xsl:apply-templates>
		</xsl:for-each>
	</li>

</xsl:for-each>
</ul>




</body>

</html></xsl:template>

<xsl:template match="submod">
<xsl:choose>
<xsl:when test="@type[. = 'Description']">
	<xsl:for-each>
		<xsl:value-of/>
	</xsl:for-each>
</xsl:when>

<xsl:when test="@type[. = 'Schema.Model']">
	Diagrams:
	<xsl:for-each select="p">
			<A>
			<xsl:attribute name="href">
					..\..\..\Doc\CmHtml\<xsl:value-of/>.htm
			</xsl:attribute>
			<xsl:value-of/>
			</A> |
	</xsl:for-each>
</xsl:when>
<xsl:otherwise>
	<b><xsl:value-of select="@type" />:</b>
	<xsl:for-each>
		<xsl:value-of/></xsl:for-each>
</xsl:otherwise>
</xsl:choose>

</xsl:template>

</xsl:stylesheet>
