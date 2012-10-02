<?xml version="1.0" encoding="UTF-8"?>
<?xml-stylesheet type="text/xsl" href="Tests.xsl"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
xmlns:ms="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="ms"  xmlns:me="MathExtension">
<xsl:output method="html" encoding="UTF-8" indent="yes" />

<ms:script language="JScript" implements-prefix="me">
  function today()
  {
	 d = new Date();
	 image = (d.getMonth()+1) + "/";
	 image += d.getDate() + "/";
	 image += d.getYear();
	 return(image);
  }
</ms:script>

<xsl:variable name="tests" select="document('zzzTestRegistry.xml')"/>

<xsl:template match="/">
   <xsl:apply-templates select="$tests/tests"/>
</xsl:template>

<xsl:template match="tests">
<html>
<head>
<title><xsl:value-of select="@label"/></title>
</head>
<body>

 <xsl:variable name="totalTests" select="count(test)"/>
 <h1><xsl:value-of select="concat($totalTests,' ',@label,': ',me:today())"/></h1>

<xsl:variable name="info">
   <for-each>
	  <xsl:apply-templates select="test"/>
   </for-each>
</xsl:variable>

<p>
 <table border="1">
  <xsl:variable name="pass" select="count(ms:node-set($info)/for-each/test-info[@result='pass'])"/>
  <xsl:variable name="fail" select="count(ms:node-set($info)/for-each/test-info[@result='fail'])"/>
  <xsl:variable name="crash" select="count(ms:node-set($info)/for-each/test-info[@result='crash!'  or (@result='fail' and (contains(.,'crash') or contains(.,'Crash')))])"/>
  <xsl:variable name="caf" select="count(ms:node-set($info)/for-each/test-info[(@result='pass') and (contains(.,'crash') or contains(.,'Crash'))])"/>

   <tr><th>Total</th><th bgcolor="green">Pass</th><th bgcolor="red">Fail</th><th bgcolor="orange">Crash</th><th>Crash as other</th></tr>
   <tr>
	  <td><xsl:value-of select="$totalTests"/></td>
	  <td bgcolor="green"><xsl:value-of select="$pass"/></td>
	  <td bgcolor="red"><xsl:value-of select="$fail"/></td>
	  <td bgcolor="orange"><xsl:value-of select="$crash"/></td>
	  <td><xsl:value-of select="$caf"/></td>
   </tr>
   <tr>
	  <td>100%</td>
	  <td bgcolor="green"><xsl:value-of select="format-number($pass div $totalTests,'0.00%')"/></td>
	  <td bgcolor="red"><xsl:value-of select="format-number($fail div $totalTests,'0.00%')"/></td>
	  <td bgcolor="orange"><xsl:value-of select="format-number($crash div $totalTests,'0.00%')"/></td>
	  <td><xsl:value-of select="format-number($caf div $totalTests,'0.00%')"/></td>
   </tr>
 </table>

</p>

 <table border="1">
   <tr><th>File</th><th>Bugs<br/>Caught</th><th>Log</th><th>Status</th><th>Notes on current status with bug ids</th><th>Short Test Description</th></tr>
   <xsl:apply-templates select="ms:node-set($info)/for-each/test-info"/>
 </table>

<p>
  <table border="1">
   <tr><th>Prefix</th><th>Tool Category</th></tr>
   <tr><td>gm</td><td>Grammar</td></tr>
   <tr><td>li</td><td>Lists</td></tr>
   <tr><td>lx</td><td>Lexicon</td></tr>
   <tr><td>p</td><td>File and Project Menus</td></tr>
   <tr><td>tls</td><td>Tools Menu</td></tr>
   <tr><td>tx</td><td>Text &amp; Words</td></tr>
   <tr><td>wd</td><td>Text &amp; Words</td></tr>
  </table>
</p>

</body>
</html>
</xsl:template>

<xsl:template match="test">
  <xsl:variable name="fileName" select="substring-before(@file,'.')"/>
  <xsl:variable name="content">
	 <xsl:if test="boolean(document(string(concat($fileName,'.xlg'))))">
			<xsl:copy-of select="document(string(concat($fileName,'.xlg')))"/>
	 </xsl:if>
  </xsl:variable>
  <xsl:variable name="script">
	 <xsl:if test="boolean(document(string(concat($fileName,'.xml'))))">
			<xsl:copy-of select="document(string(concat($fileName,'.xml')))"/>
	 </xsl:if>
  </xsl:variable>
  <test-info name="{$fileName}" goal="{ms:node-set($script)/accil/goal}" bugs='{count(ms:node-set($script)/accil/bug[not(@id="")])}' other="{@log}">
	 <xsl:attribute name="log">
		 <xsl:choose>
		 <xsl:when test="$content">
			 <xsl:text>yes</xsl:text>
		 </xsl:when>
		 <xsl:otherwise>
			 <xsl:text>no</xsl:text>
		 </xsl:otherwise>
		 </xsl:choose>
	 </xsl:attribute>
	 <xsl:attribute name="result">
		 <xsl:choose>
		 <xsl:when test="contains(ms:node-set($content)//assertion,'Got an error window!')">
			 <xsl:text>crash!</xsl:text>
		 </xsl:when>
		 <xsl:when test="ms:node-set($content)//assertion">
			 <xsl:text>fail</xsl:text>
		 </xsl:when>
		 <xsl:otherwise>
			 <xsl:text>pass</xsl:text>
		 </xsl:otherwise>
		 </xsl:choose>
	 </xsl:attribute>
	  <xsl:value-of select="@note"/>
   </test-info>
</xsl:template>

<xsl:template match="test-info">
  <tr>
	 <td>
		 <a href="{@name}.xml"><xsl:value-of select="@name"/></a>
	 </td>
	 <td style="text-align:center">
		 <xsl:value-of select="@bugs"/>
	 </td>
	 <td style="text-align:center">
		 <xsl:choose>
		 <xsl:when test="@log = 'yes'">
			<a href="{@name}.xlg"><xsl:value-of select="'log'"/></a>
		 </xsl:when>
		 <xsl:otherwise>
			 <xsl:text>no log</xsl:text>
		 </xsl:otherwise>
		 </xsl:choose>
	 </td>
	 <td style="text-align:center">
		 <xsl:attribute name="style">
		 <xsl:choose>
		 <xsl:when test="@result = 'crash!'">
			 <xsl:text>background-color:orange</xsl:text>
		 </xsl:when>
		 <xsl:when test="@result = 'fail'">
			 <xsl:text>background-color:red</xsl:text>
		 </xsl:when>
		 <xsl:otherwise>
			 <xsl:text>background-color:green</xsl:text>
		 </xsl:otherwise>
		 </xsl:choose>
		 </xsl:attribute>
		 <xsl:value-of select="@result"/>
	 </td>
	 <td>
		 <xsl:attribute name="style">
		   <xsl:choose>
		   <xsl:when test="contains(.,'crash') or contains(.,'Crash')">
			   <xsl:text>background-color:orange</xsl:text>
		   </xsl:when>
		   <xsl:when test="contains(.,'yellow') or contains(.,'Yellow')">
			   <xsl:text>background-color:yellow</xsl:text>
		   </xsl:when>
		   <xsl:when test="@other!=''">
			   <xsl:text>background-color:plum</xsl:text>
		   </xsl:when>
		   </xsl:choose>
		 </xsl:attribute>
		 <xsl:value-of select="."/>
	 </td>
	 <td>
		 <xsl:value-of select="@goal"/>
	 </td>
  </tr>
</xsl:template>

</xsl:stylesheet>
