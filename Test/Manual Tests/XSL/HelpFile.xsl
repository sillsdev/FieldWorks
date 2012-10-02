<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
<xsl:output method="html" encoding="UTF-8" indent="yes"/>
<xsl:param name="target">xml</xsl:param>
<xsl:param name="picdir">..</xsl:param>

<xsl:template match="/modularbook">
 <xsl:element name="html">
  <xsl:element name="head">
   <title>
	<xsl:value-of select="titlePage/title"/>
   </title>
  </xsl:element>
  <body bgcolor="MINTCREAM" text="DARKBLUE" link="FORESTGREEN" vlink="DARKGOLDENROD" alink="INDIGO">
   <xsl:apply-templates/>
  </body>
 </xsl:element>
</xsl:template>

<xsl:template match="titlePage | overview | task | concept | structure | fact | process | principle | story | training | test">
 <xsl:if test="not(/modularbook)">
  <xsl:element name="html">
   <xsl:element name="head">
	<title><xsl:value-of select="title"/></title>
   </xsl:element>
   <body bgcolor="MINTCREAM" text="DARKBLUE" link="FORESTGREEN" vlink="DARKGOLDENROD" alink="INDIGO">
	<xsl:choose>
	 <xsl:when test="name()='titlePage'">
	  <xsl:call-template name="tpbody"/>
	 </xsl:when>
	 <xsl:when test="local-name()='test'">
	  <xsl:call-template name="dotest"/>
	 </xsl:when>
	 <xsl:otherwise>
	  <xsl:call-template name="modulebody"/>
	 </xsl:otherwise>
	</xsl:choose>
   </body>
  </xsl:element>
 </xsl:if>
 <xsl:if test="/modularbook">
  <xsl:choose>
   <xsl:when test="name()='titlePage'">
	<xsl:call-template name="tpbody"/>
   </xsl:when>
   <xsl:when test="local-name()='test'">
	<xsl:call-template name="dotest"/>
   </xsl:when>
   <xsl:otherwise>
	<xsl:call-template name="modulebody"/>
   </xsl:otherwise>
  </xsl:choose>
 </xsl:if>
</xsl:template>

<xsl:template name="modulebody">
 <xsl:apply-templates/>
 <HR SHADE="5"/>
</xsl:template>

<xsl:template name="tpbody">
 <xsl:apply-templates select="title"/>
 <xsl:apply-templates select="authors"/>
 <xsl:apply-templates select="copywrite"/>
 <P align="CENTER">
  <xsl:apply-templates select="link"/>
 </P>
 <xsl:if test="img">
  <P align="CENTER">
   <xsl:apply-templates select="img"/>
  </P>
 </xsl:if>
 <xsl:apply-templates select="shortcuts"/>
 <HR SHADE="5"/>
</xsl:template>

<xsl:template match="title">
 <H1 Align="CENTER">
  <xsl:element name="a">
   <xsl:attribute name="name">
	<xsl:value-of select="@tag"/>
   </xsl:attribute>
   <xsl:value-of select="."/>
  </xsl:element>
 </H1>
</xsl:template>

<xsl:template match="index">  <!-- This transform does nothing with index -->
</xsl:template>

<xsl:template match="authors">
 <xsl:apply-templates/>
</xsl:template>

<xsl:template match="copywrite">
 <p align="CENTER">
  <xsl:text disable-output-escaping="yes">&amp;copy; </xsl:text>
  <xsl:apply-templates/>
 </p>
</xsl:template>

<xsl:template match="shortcuts">
 <xsl:for-each select="link">
  <DIV ALIGN="CENTER">
   <a>
	<xsl:attribute name="href">
	 <xsl:call-template name="targetUri"/>
	</xsl:attribute>
	<xsl:apply-templates/>
   </a>
  </DIV>
 </xsl:for-each>
</xsl:template>

<xsl:template match="introduction">
 <H2>Introduction</H2>
 <BLOCKQUOTE>
  <xsl:apply-templates/>
 </BLOCKQUOTE>
</xsl:template>

<xsl:template match="description">
 <H2>Description</H2>
 <BLOCKQUOTE>
  <xsl:apply-templates/>
 </BLOCKQUOTE>
</xsl:template>

<xsl:template match="parts">
 <H2>Parts</H2>
 <BLOCKQUOTE>
  <xsl:apply-templates/>
 </BLOCKQUOTE>
</xsl:template>

<xsl:template match="thingsToDo">
 <H2>Things to do</H2>
 <BLOCKQUOTE>
  <xsl:apply-templates select="p"/>
  <ul>
   <xsl:apply-templates select="step"/>
  </ul>
 </BLOCKQUOTE>
</xsl:template>

<xsl:template match="steps">
 <H2>Steps</H2>
 <BLOCKQUOTE>
  <xsl:apply-templates select="p"/>
  <ol>
   <xsl:apply-templates select="step"/>
  </ol>
 </BLOCKQUOTE>
</xsl:template>

<xsl:template match="alternatives">
 <H2>Alternatives</H2>
 <BLOCKQUOTE>
  <xsl:apply-templates select="p"/>
  <ol type="A">
   <xsl:apply-templates select="step"/>
  </ol>
 </BLOCKQUOTE>
</xsl:template>

<xsl:template name="dotest">
 <H2>
  <xsl:value-of select="@num"/><xsl:text> </xsl:text>
  <xsl:value-of select="title"/><xsl:text> (</xsl:text>
  <xsl:value-of select="@id"/><xsl:text>)</xsl:text>
 </H2>
 <xsl:apply-templates select="introduction"/>
 <xsl:apply-templates select="benefits"/>
 <xsl:apply-templates select="context"/>
 <xsl:apply-templates select="objectives"/>
 <xsl:apply-templates select="indications"/>
 <xsl:apply-templates select="prerequisites"/>
 <xsl:apply-templates select="materials"/>
 <xsl:apply-templates select="warning"/>
 <xsl:apply-templates select="guidelines"/>
  <xsl:apply-templates select="case"/>
 <xsl:apply-templates select="seeAlso"/>
 <xsl:apply-templates select="sources"/>
 <xsl:apply-templates select="update"/>
</xsl:template>

<xsl:template match="case">
 <table border="1">
  <tr>
   <td colspan="4">
	<strong>
	 <font size="+2">
	  <a id="{@id}">
	   <xsl:value-of select="@num"/><xsl:text> </xsl:text>
	   <xsl:value-of select="title"/>
	  </a>
	 </font>
	 <xsl:text> (</xsl:text>
	 <xsl:value-of select="@id"/><xsl:text>) </xsl:text>
	 <xsl:if test="'any'=@order">- Do in any order</xsl:if>
	 <xsl:if test="'123'=@order">- Do in sequence</xsl:if>
	</strong>
   </td>
  </tr>
  <xsl:apply-templates select="context"/>
  <xsl:apply-templates select="p"/>
  <tr>
   <th>Step</th><th>Action</th><th>Result</th><th>Bugs</th>
  </tr>
  <xsl:apply-templates select="step"/>
 </table>
 <br/>
</xsl:template>

<xsl:template match="case/step">
 <tr>
  <td>
   <STRONG><xsl:value-of select="@name"/></STRONG>
  </td>
  <td>
  <xsl:apply-templates select="text() | i | b | link | img"/>
  </td>
  <td>
   <xsl:apply-templates select="r"/>
  </td>
  <td>
   <xsl:choose>
   <xsl:when test="'Bug'=@link">
	<a href="PatList.htm#{concat(./parent::case/@id,@name)}">
	 <xsl:value-of select="'Bug'"/>
	</a>
   </xsl:when>
   <xsl:when test="@uri">
	<a>
	 <xsl:attribute name="href">
	  <xsl:call-template name="targetUri"/>
	 </xsl:attribute>
	 <xsl:value-of select="@link"/>
	</a>
   </xsl:when>
   </xsl:choose>
  </td>
 </tr>
 <xsl:for-each select="p">
  <xsl:if test="@name!='bug' or not(@name)">
   <tr><td></td>
	<td colspan="3">
	 <div>
	  <xsl:if test="not(@name)">
	   <STRONG>Discussion<xsl:text>: </xsl:text></STRONG>
	  </xsl:if>
	  <xsl:call-template name="pguts" />
	 </div>
	</td>
   </tr>
  </xsl:if>
 </xsl:for-each>
</xsl:template>

<xsl:template match="step">
 <li>
  <xsl:if test="@name">
   <STRONG><xsl:value-of select="@name"/><xsl:text>: </xsl:text></STRONG>
  </xsl:if>
  <xsl:apply-templates select="text() | i | b | link | img"/>
  <xsl:if test="@uri">
   <xsl:text> </xsl:text>
   <a>
	 <xsl:attribute name="href">
	  <xsl:call-template name="targetUri"/>
	 </xsl:attribute>
	 <xsl:value-of select="@link"/>
   </a>
  </xsl:if>
  <xsl:if test="r | p">
   <BLOCKQUOTE>
	<xsl:apply-templates select="r | p"/>
   </BLOCKQUOTE>
  </xsl:if>
 </li>
</xsl:template>

<xsl:template match="guidelines">
 <H2>Guidelines</H2>
 <BLOCKQUOTE>
  <xsl:apply-templates/>
 </BLOCKQUOTE>
</xsl:template>

<xsl:template match="prerequisites">
 <H2>Prerequisites</H2>
 <BLOCKQUOTE>
  <xsl:apply-templates/>
 </BLOCKQUOTE>
</xsl:template>

<xsl:template match="benefits">
 <H2>Benefits</H2>
 <BLOCKQUOTE>
  <xsl:apply-templates/>
 </BLOCKQUOTE>
</xsl:template>

<xsl:template match="warning">
 <H2>Warning!</H2>
 <BLOCKQUOTE>
  <xsl:apply-templates/>
 </BLOCKQUOTE>
</xsl:template>

<xsl:template match="case/context">
 <tr><td colspan="4">
  <strong>Context: </strong>
  <xsl:for-each select="p">
   <xsl:apply-templates/>
  </xsl:for-each><xsl:text>:</xsl:text>
 </td></tr>
</xsl:template>

<xsl:template match="features">
 <H2>Features</H2>
 <BLOCKQUOTE>
  <xsl:apply-templates/>
 </BLOCKQUOTE>
</xsl:template>

<xsl:template match="context">
 <H2>Context</H2>
 <BLOCKQUOTE>
  <xsl:apply-templates/>
 </BLOCKQUOTE>
</xsl:template>

<xsl:template match="seeAlso">
 <H2>See also</H2>
 <BLOCKQUOTE>
  <ul>
   <xsl:apply-templates/>
  </ul>
 </BLOCKQUOTE>
</xsl:template>

<xsl:template match="sources">
 <H2>Sources</H2>
 <BLOCKQUOTE>
  <ul>
   <xsl:apply-templates/>
  </ul>
 </BLOCKQUOTE>
</xsl:template>

<xsl:template match="label">
 <H2><xsl:value-of select="@name"/></H2>
 <BLOCKQUOTE>
  <xsl:apply-templates/>
 </BLOCKQUOTE>
</xsl:template>

<xsl:template match="moduleGroup">
 <H2>In this module group</H2>
 <BLOCKQUOTE>
  <xsl:apply-templates/>
 </BLOCKQUOTE>
</xsl:template>

<xsl:template match="definition">
 <H2>Definition</H2>
 <BLOCKQUOTE>
  <xsl:apply-templates/>
 </BLOCKQUOTE>
</xsl:template>

<xsl:template match="partsAndFunctions">
 <H2>Parts and Functions</H2>
 <BLOCKQUOTE>
  <xsl:apply-templates/>
 </BLOCKQUOTE>
</xsl:template>

<xsl:template match="Parts">
 <H2>Parts</H2>
 <BLOCKQUOTE>
  <xsl:apply-templates/>
 </BLOCKQUOTE>
</xsl:template>

<xsl:template match="discussion">
 <H2>Discussion</H2>
 <BLOCKQUOTE>
  <xsl:apply-templates/>
 </BLOCKQUOTE>
</xsl:template>

<xsl:template match="indications">
 <H2>Indications</H2>
 <BLOCKQUOTE>
  <xsl:apply-templates/>
 </BLOCKQUOTE>
</xsl:template>

<xsl:template match="div">
 <div align="CENTER">
  <xsl:if test="(parent::authors) and (position()=1)">
   <xsl:text>by </xsl:text>
  </xsl:if>
  <xsl:apply-templates/>
 </div>
</xsl:template>

<xsl:template match="case/p">
 <tr><td colspan="4">
  <xsl:call-template name="pguts" />
 </td></tr>
</xsl:template>

<xsl:template match="p">
 <p>
  <xsl:if test="@align">
   <xsl:attribute name="align">
	<xsl:value-of select="@align"/>
   </xsl:attribute>
  </xsl:if>
  <xsl:call-template name="pguts" />
 </p>
</xsl:template>

<xsl:template name="pguts">
 <xsl:choose>
 <xsl:when test="@name='todo'">
  <font color="green"><STRONG><xsl:value-of select="@name"/><xsl:text>: </xsl:text></STRONG>
   <xsl:apply-templates/>
  </font>
 </xsl:when>
 <xsl:when test="@name">
  <STRONG><xsl:value-of select="@name"/><xsl:text>: </xsl:text></STRONG>
  <xsl:apply-templates/>
 </xsl:when>
 <xsl:otherwise>
  <xsl:apply-templates/>
 </xsl:otherwise>
 </xsl:choose>
</xsl:template>

<xsl:template match="step/r">
 <div>
  <xsl:choose>
  <xsl:when test="@name">
   <STRONG><xsl:value-of select="@name"/><xsl:text>: </xsl:text></STRONG>
  </xsl:when>
  <xsl:otherwise>
   <STRONG>Result<xsl:text>: </xsl:text></STRONG>
  </xsl:otherwise>
  </xsl:choose>
  <xsl:apply-templates/>
 </div>
</xsl:template>

<xsl:template match="case/step/r">
 <xsl:apply-templates/>
</xsl:template>

<xsl:template match="pre">
 <pre><xsl:value-of select="."/>
 </pre>
</xsl:template>

<xsl:template match="i">
 <em>
 <xsl:apply-templates/>
 </em>
</xsl:template>

<xsl:template match="b">
 <strong>
 <xsl:apply-templates/>
 </strong>
</xsl:template>

<xsl:template match="link">
 <xsl:call-template name="linkguts" />
</xsl:template>

<xsl:template name="targetUri">
  <xsl:choose>
  <xsl:when test="@help and $target='help'">
   <xsl:value-of select="@help"/>
  </xsl:when>
  <xsl:when test="@web and ($target='html' or $target='help')">
   <xsl:value-of select="@web"/>
  </xsl:when>
  <xsl:when test="contains(@uri,'.xml') and ($target='html' or $target='help')">
   <xsl:value-of select="concat(substring-before(@uri,'.xml'),'.htm',substring-after(@uri,'.xml'))"/>
  </xsl:when>
  <xsl:when test="contains(@uri,'..\..\TS\') and ($target='html' or $target='help')">
   <xsl:value-of select="concat('..',substring-after(@uri,'..\..\TS'))"/>
  </xsl:when>
  <xsl:otherwise>
   <xsl:value-of select="@uri"/>
  </xsl:otherwise>
  </xsl:choose>
</xsl:template>

<xsl:template name="linkguts">
 <xsl:if test="@type='doc' or (@help and $target='help' and not(contains(@help,'help')))">
  <img src="{$picdir}/earth.gif" alt="Visit another website"/>
 </xsl:if>
 <xsl:if test="@type='e-mail'">
  <img src="{$picdir}/mail.gif" alt="Send an e-mail"/>
 </xsl:if>
 <xsl:if test="@type='photo'">
  <img src="{$picdir}/camera.gif" alt="Link to a photogragh"/>
 </xsl:if>
 <a>
  <xsl:attribute name="href">
   <xsl:choose>
	<xsl:when test="@type='e-mail'">
	 <xsl:text>mailto:</xsl:text><xsl:value-of select="@uri"/>
	</xsl:when>
	<xsl:when test="@type='doc'">
	 <xsl:value-of select="@uri"/>
	</xsl:when>
	<xsl:otherwise>
	 <xsl:call-template name="targetUri"/>
	</xsl:otherwise>
   </xsl:choose>
  </xsl:attribute>
  <xsl:if test="@title">
   <xsl:attribute name="title"><xsl:value-of select="@title"/></xsl:attribute>
  </xsl:if>
  <xsl:apply-templates/>
 </a>
</xsl:template>

<xsl:template match="ul">
 <xsl:if test="@name">
  <DIV><STRONG><xsl:value-of select="@name"/></STRONG></DIV>
 </xsl:if>
 <ul>
 <xsl:apply-templates/>
 </ul>
</xsl:template>

<xsl:template match="ol">
 <xsl:if test="@name">
  <DIV><STRONG><xsl:value-of select="@name"/></STRONG></DIV>
 </xsl:if>
 <ol>
 <xsl:if test="@type">
  <xsl:attribute name="type"><xsl:value-of select="@type"/></xsl:attribute>
 </xsl:if>
 <xsl:apply-templates/>
 </ol>
</xsl:template>

<xsl:template match="li">
 <li>
  <xsl:apply-templates/>
 </li>
</xsl:template>

<xsl:template match="img">
 <xsl:element name="img">
  <xsl:attribute name="src">
   <xsl:value-of select="@uri"/>
  </xsl:attribute>
  <xsl:attribute name="alt">
   <xsl:value-of select="@alt"/>
  </xsl:attribute>
  <xsl:if test="@align">
   <xsl:attribute name="align">
	<xsl:value-of select="@align"/>
   </xsl:attribute>
  </xsl:if>
  <xsl:if test="@height">
   <xsl:attribute name="height">
	<xsl:value-of select="@height"/>
   </xsl:attribute>
  </xsl:if>
  <xsl:if test="@width">
   <xsl:attribute name="width">
	<xsl:value-of select="@width"/>
   </xsl:attribute>
  </xsl:if>
 </xsl:element>
</xsl:template>

<xsl:template match="table">
 <table border="1">
  <xsl:apply-templates/>
 </table>
</xsl:template>

<xsl:template match="tr">
 <tr>
  <xsl:apply-templates/>
 </tr>
</xsl:template>

<xsl:template match="th">
 <th>
  <xsl:apply-templates/>
 </th>
</xsl:template>

<xsl:template match="td">
 <td>
  <xsl:apply-templates/>
 </td>
</xsl:template>

<xsl:template match="update">
 <xsl:if test="position()=1">
  <H2>Test Script History</H2>
 </xsl:if>
 <div>
  <xsl:call-template name="timestamp">
   <xsl:with-param name="P" select="." />
  </xsl:call-template>
  <xsl:apply-templates/>
 </div>
</xsl:template>

<xsl:template name="timestamp">
 <xsl:param name="P" />
 Date: <xsl:value-of select="$P/@date"/><xsl:text>, </xsl:text>
 Author: <xsl:value-of select="$P/@author"/><xsl:text>: </xsl:text>
</xsl:template>

</xsl:stylesheet>