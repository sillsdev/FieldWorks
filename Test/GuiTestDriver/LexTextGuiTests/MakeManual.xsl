<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
xmlns:ms="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="ms">
<xsl:output method="html" encoding="UTF-8" indent="yes" />

<xsl:template match="/accil">
<html>
<head>
<title><xsl:value-of select="goal"/></title>
</head>
<body>

 <h1><xsl:value-of select="goal"/></h1>

 <table border="0">
   <xsl:apply-templates select="bug"/>
 </table>

 <h2>Steps</h2>
 <ol>
   <xsl:apply-templates select="*[name() != 'bug' and name() != 'goal' and name() != 'var']"/>
 </ol>

<p>That's all!</p>

</body>
</html>
</xsl:template>

<xsl:template match="bug">
  <tr><td>Bug Reported:</td>
	<td><xsl:value-of select="@id"/></td>
	<td><xsl:value-of select="@desc"/></td>
  </tr>
</xsl:template>

<xsl:template match="sound">
 <li>
 <xsl:call-template name="getComment"/>
 <div>
  <xsl:text>Tone played</xsl:text>
 </div>
 </li>
</xsl:template>

<xsl:template match="registry">
 <li>
 <xsl:call-template name="getComment"/>
 <div>
  <xsl:text>Set registry key [</xsl:text>
  <xsl:value-of select="@key"/><xsl:text>] to [</xsl:text>
  <xsl:value-of select="@data"/><xsl:text>]</xsl:text>
 </div>
 </li>
</xsl:template>

<xsl:template match="on-application">
 <li>
 <xsl:call-template name="getComment"/>
 <div>
  <xsl:choose>
  <xsl:when test="@run='yes'">
	<xsl:text>If running, close the appliction. Start the application</xsl:text>
  </xsl:when>
  <xsl:when test="@run='no'">
	<xsl:text>The application should already be running.</xsl:text>
  </xsl:when>
  <xsl:otherwise>
	<xsl:text>If not already running, start the application</xsl:text>
  </xsl:otherwise>
  </xsl:choose>

  <xsl:if test="@args">
	<xsl:text> with command line arguements: </xsl:text>
	<xsl:call-template name="evalVar">
	  <xsl:with-param name="string" select="@args" />
	</xsl:call-template>
  </xsl:if>
 </div>

 <ol>
   <xsl:apply-templates select="*[name() != 'var']"/>
 </ol>

 </li>
</xsl:template>

<xsl:template match="click">
 <li>
 <xsl:call-template name="getComment"/>
 <div>
  <xsl:choose>
  <xsl:when test="@side='right'">
	<xsl:text>Right-c</xsl:text>
  </xsl:when>
  <xsl:otherwise>
	<xsl:text>C</xsl:text>
  </xsl:otherwise>
  </xsl:choose>
  <xsl:text>lick on </xsl:text>

  <span style="color:blue">
	<xsl:call-template name="evalVar">
	  <xsl:with-param name="string" select="@select" />
	</xsl:call-template>
  </span>
  <xsl:text> </xsl:text>
  <span style="color:purple"><xsl:value-of select="@path"/></span>
 </div>
 </li>
</xsl:template>

<xsl:template match="model">
 <li>
 <xsl:call-template name="getComment"/>
 <div>
  <xsl:text>Use </xsl:text>
  <xsl:call-template name="prettyModelPath" >
	<xsl:with-param name="modelPath" select="@select"/>
  </xsl:call-template>
 </div>
   <ol>
	 <xsl:apply-templates select="*[name() != 'var']"/>
   </ol>
 </li>
</xsl:template>

<xsl:template match="on-dialog">
 <li>
 <xsl:call-template name="getComment"/>
 <div>
  <xsl:text>On the </xsl:text>
  <xsl:call-template name="prettyModelPath" >
	<xsl:with-param name="modelPath" select="@select"/>
  </xsl:call-template>
  <xsl:value-of select="@title"/>
  <xsl:if test="@title and not(@select)">
	<xsl:text> dialog</xsl:text>
  </xsl:if>
 </div>
   <ol>
	 <xsl:apply-templates select="*[name() != 'var']"/>
   </ol>
 </li>
</xsl:template>

<xsl:template match="insert">
 <li>
 <xsl:call-template name="getComment"/>
 <div>
  <xsl:text>insert &quot;</xsl:text>
  <xsl:variable name="noTilde">
	<xsl:call-template name="replace" >
	  <xsl:with-param name="string" select="."/>
	  <xsl:with-param name="target" select="'~'"/>
	  <xsl:with-param name="new" select="'{ENTER}'"/>
	</xsl:call-template>
  </xsl:variable>
  <xsl:variable name="noCtrl">
	<xsl:call-template name="replace" >
	  <xsl:with-param name="string" select="$noTilde"/>
	  <xsl:with-param name="target" select="'^'"/>
	  <xsl:with-param name="new" select="'{ctrl}'"/>
	</xsl:call-template>
  </xsl:variable>
  <xsl:variable name="noShift">
	<xsl:call-template name="replace" >
	  <xsl:with-param name="string" select="$noCtrl"/>
	  <xsl:with-param name="target" select="'+'"/>
	  <xsl:with-param name="new" select="'{shift}'"/>
	</xsl:call-template>
  </xsl:variable>
  <xsl:variable name="clean">
	<xsl:call-template name="replace" >
	  <xsl:with-param name="string" select="$noShift"/>
	  <xsl:with-param name="target" select="'%'"/>
	  <xsl:with-param name="new" select="'{alt}'"/>
	</xsl:call-template>
  </xsl:variable>
  <xsl:value-of select="$clean"/>
  <xsl:text>&quot;</xsl:text>
 </div>
 </li>
</xsl:template>

<xsl:template match="glimpse">
 <li style="color:green">
 <xsl:call-template name="getComment"/>
 <div style="color:green">
  <xsl:text>Verify that </xsl:text>
  <span style="color:blue">
	<xsl:call-template name="evalVar">
	  <xsl:with-param name="string" select="@select" />
	</xsl:call-template>
  </span>
  <xsl:text> </xsl:text>
  <span style="color:purple"><xsl:value-of select="@path"/></span>

  <!-- @prop values are:
name, role, hotkey, value, visible, handle, children, checked, selected, present, absent, unavailable
  -->
  <xsl:text> </xsl:text>

  <xsl:choose>
  <xsl:when test="@expect">
	<xsl:value-of select="@prop"/>
	<xsl:text> is </xsl:text>
	<xsl:if test="@on-pass='assert'">
	  <span style="color:red">NOT </span>
	</xsl:if>
	<xsl:value-of select="@expect"/>
  </xsl:when>
  <xsl:otherwise> <!-- no @expect found -->
	<xsl:choose>
	<xsl:when test="not(@prop) or @prop=''">
	  <xsl:text>exists</xsl:text>
	</xsl:when>
	<xsl:when test="(not(@prop) or @prop='') and @on-pass='assert'">
	  <xsl:text>does not exist</xsl:text>
	</xsl:when>
	<xsl:when test="@prop='absent' or (@prop='present' and @on-pass='assert')">
	  <xsl:text>does not exist</xsl:text>
	</xsl:when>
	<xsl:when test="@prop='present' or (@prop='absent' and @on-pass='assert')">
	  <xsl:text>exists</xsl:text>
	</xsl:when>
	<xsl:otherwise>
	  <xsl:value-of select="@prop"/>
	</xsl:otherwise>
	</xsl:choose>
  </xsl:otherwise>
  </xsl:choose>

  <xsl:if test="@on-fail='skip'">
	<xsl:text> and set </xsl:text>
	<xsl:value-of select="@id"/>
	<xsl:text> to the result</xsl:text>
  </xsl:if>
 </div>
 </li>
</xsl:template>

<xsl:template match="*">
 <li style="color:red">
 <xsl:call-template name="getComment"/>
 <div style="color:red">
  <xsl:value-of select="name()"/>
 </div>
 </li>
</xsl:template>




<xsl:template name="getComment">
 <!-- uses current context to get comment -->
 <!-- the first node is a text node with a newline, so use the 2nd -->
 <xsl:if test="preceding-sibling::node()[2][self::comment()]">
   <div>// <xsl:value-of select="preceding-sibling::comment()[1]"/></div>
 </xsl:if>
</xsl:template>

<xsl:template name="evalVar">
<xsl:param name="string" />
  <xsl:choose>
  <xsl:when test="contains($string,';')">
	<xsl:variable name="beforeSc" select="substring-before($string,';')"/>
	<xsl:variable name="afterSc" select="substring-after($string,';')"/>
	<xsl:variable name="out" select="substring-before($beforeSc,'$')"/>
	<xsl:variable name="varRef" select="substring-after($beforeSc,'$')"/>
	<xsl:call-template name="noAnywhere" >
	  <xsl:with-param name="string" select="$out" />
	</xsl:call-template>
	<xsl:call-template name="evalRef">
	  <xsl:with-param name="varRef" select="$varRef" />
	</xsl:call-template>

	<xsl:call-template name="evalVar">
	  <xsl:with-param name="string" select="$afterSc" />
	</xsl:call-template>
  </xsl:when>
  <xsl:when test="contains($string,'$')"> <!-- at end of string -->
	<xsl:variable name="out" select="substring-before($string,'$')"/>
	<xsl:variable name="varRef" select="substring-after($string,'$')"/>

	<xsl:if test="$out and not(starts-with($out,'//'))">
	  <xsl:value-of select="substring($out,1,2)"/>
	</xsl:if>
	<xsl:value-of select="substring($out,3)"/>

	<xsl:call-template name="evalRef">
	  <xsl:with-param name="varRef" select="$varRef" />
	</xsl:call-template>
  </xsl:when>
  <xsl:otherwise>
	<xsl:if test="$string and not(starts-with($string,'//'))">
	  <xsl:value-of select="substring($string,1,2)"/>
	</xsl:if>
	<xsl:value-of select="substring($string,3)"/>
  </xsl:otherwise>
  </xsl:choose>
</xsl:template>

<xsl:template name="evalRef">
<xsl:param name="varRef"/>
  <xsl:value-of select="preceding::var[@id=$varRef]/@set"/>
  <!--xsl:value-of select="$varRef"/-->
</xsl:template>

<xsl:template name="prettyModelPath" >
<xsl:param name="modelPath" />
  <xsl:variable name="beforeSb" select="substring-before($modelPath,'[')"/>
  <xsl:variable name="afterSb" select="substring-after($modelPath,']')"/>
  <xsl:variable name="role" select='substring-before(substring-after($modelPath,"@role=&apos;"),"&apos;")'/>
  <xsl:call-template name="noAnywhere" >
	<xsl:with-param name="string" select="$beforeSb" />
  </xsl:call-template>
  <xsl:value-of select="$afterSb"/>
  <xsl:text> </xsl:text>
  <xsl:value-of select="$role"/>
</xsl:template>

<xsl:template name="noAnywhere" >
<xsl:param name="string" />
	<xsl:if test="$string and not(starts-with($string,'//'))">
	  <xsl:value-of select="substring($string,1,2)"/>
	</xsl:if>
	<xsl:value-of select="substring($string,3)"/>
</xsl:template>

<xsl:template name="replace" >
<xsl:param name="string" />
<xsl:param name="target" />
<xsl:param name="new" />
  <xsl:choose>
  <xsl:when test="contains($string,$target)">
	<xsl:variable name="before" select="substring-before($string,$target)"/>
	<xsl:variable name="after" select="substring-after($string,$target)"/>
	<xsl:value-of select="concat($before,$new)"/>
	<xsl:if test="$after">
	  <xsl:call-template name="replace" >
		<xsl:with-param name="string" select="$after"/>
		<xsl:with-param name="target" select="$target"/>
		<xsl:with-param name="new" select="$new"/>
	  </xsl:call-template>
	</xsl:if>
  </xsl:when>
  <xsl:otherwise>
	<xsl:value-of select="$string"/>
  </xsl:otherwise>
  </xsl:choose>
</xsl:template>


</xsl:stylesheet>
