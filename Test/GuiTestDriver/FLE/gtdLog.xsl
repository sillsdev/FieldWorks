<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<xsl:output method="html" encoding="UTF-8" indent="yes" />

<xsl:template match="gtdLog">
<html>
<head>
<title><xsl:value-of select="concat('Run of ',set-up/script/@name,' Testing ',set-up/application/@exe)" /></title>
<style type="text/css">
 .indent {margin-left:2em}
 .indent2 {margin-left:4em}
 .ins {color:black}
 .attr {color:MAROON}
 .value {color:PURPLE}
 .para {color:blue}
 .result {color:ORANGERED}
</style>
</head>
<body>
 <h1><xsl:value-of select="concat('Run of ',set-up/script/@name,' Testing ',set-up/application/@exe)" /></h1>
  <xsl:variable name="ins" select="//*[@n]"/>
  <h3><a href="#ins{$ins[last()]/@n}">Go to last executed instruction</a></h3>
 <xsl:apply-templates/>
</body>
</html>
</xsl:template>

<xsl:template match="set-up">
Test set up:
<div class="indent">
 <xsl:apply-templates />
</div>
</xsl:template>

<xsl:template match="result">
 <div>
  <span class="result">
   <xsl:value-of select="name()" />
  </span>
  <xsl:apply-templates select="@*"/>
 </div>
</xsl:template>

<xsl:template match="*">
 <div>
  <xsl:if test="@id">
   <a name="id{@id}">
   </a>
  </xsl:if>
  <xsl:if test="@n">
   <a name="ins{@n}">
   <xsl:value-of select="@n" />
   <xsl:text> - </xsl:text>
   </a>
  </xsl:if>
  <span class="ins">
   <xsl:value-of select="name()" />
  </span>
  <xsl:apply-templates select="@*"/>
 </div>
</xsl:template>

<xsl:template match="@ins">
  <a class="value" href="#ins{.}">
   <xsl:text> </xsl:text>
   <xsl:value-of select="." />
  </a>
</xsl:template>

<xsl:template match="@*">
 <xsl:if test="not(name()='n')">
  <xsl:text> </xsl:text>
  <span class="attr">
   <xsl:value-of select="name()" />
  </span>
  <xsl:text>=</xsl:text>
  <span class="value">
   <xsl:call-template name="parsePath">
	 <xsl:with-param name="path" select="."/>
   </xsl:call-template>
   <!--xsl:value-of select="." /-->
  </span>
 </xsl:if>
</xsl:template>

<xsl:template name="parsePath">
<xsl:param name="path"/>
  <xsl:if test="$path!=''">
	<!--xsl:value-of select="concat('path is:',$path)"/-->
	<xsl:variable name="before" select="substring-before($path,'$')"/>
	<xsl:variable name="after" select="substring-after($path,'$')"/>
	<xsl:choose>
	<xsl:when test="$before or $after">
	  <xsl:value-of select="$before"/>
	  <xsl:variable name="varRef">
		 <xsl:choose>
		 <xsl:when test="contains($after,';')">
			<xsl:value-of select="substring-before($after,';')" />
		 </xsl:when>
		 <xsl:when test="contains($after,' ')">
			<xsl:value-of select="substring-before($after,' ')" />
		 </xsl:when>
		 <xsl:otherwise>
			<xsl:value-of select="$after"/>
		 </xsl:otherwise>
		 </xsl:choose>
	  </xsl:variable>
	  <xsl:variable name="afterVar">
		 <xsl:choose>
		 <xsl:when test="contains($after,';')">
			<xsl:value-of select="substring-after($after,';')" />
		 </xsl:when>
		 <xsl:when test="contains($after,' ')">
			<xsl:value-of select="substring-after($after,' ')" />
		 </xsl:when>
		 <xsl:otherwise>
		 </xsl:otherwise>
		 </xsl:choose>
	  </xsl:variable>
	  <a href="#id{$varRef}"><xsl:value-of select="concat('$',$varRef,';')"/></a>
	  <xsl:call-template name="parsePath">
		 <xsl:with-param name="path" select="$afterVar"/>
	  </xsl:call-template>
	</xsl:when>
	<xsl:otherwise>
	  <xsl:value-of select="$path"/>
	</xsl:otherwise>
	</xsl:choose>
  </xsl:if>
</xsl:template>

<xsl:template match="assertion">
<div class="indent">
 <span class="result">Assert
  <xsl:value-of select="@type"/> <xsl:text> </xsl:text>
 </span>
 <xsl:apply-templates />
</div>
</xsl:template>

<xsl:template match="head">
  <xsl:variable name="idNum" select="concat('head',count(preceding::head)+count(ancestor::head))"/>
  <div class="indent" style="cursor:pointer" onclick="var ob = document.getElementById('{$idNum}').style; ob.display = (ob.display == 'block')?'none':'block';">
  <xsl:value-of select="concat($idNum,'  ')"/>
  <xsl:apply-templates select="text()"/>
  </div>
  <!-- get list of child searches -->
  <div id="{$idNum}" style="display:none">
	 <xsl:apply-templates select="*"/>
  </div>
</xsl:template>

<xsl:template match="item">
	<div class="indent">
	   <xsl:apply-templates />
	</div>
</xsl:template>

<xsl:template match="p">
	<div class="indent">
	   <xsl:apply-templates />
	</div>
</xsl:template>

<xsl:template match="text()">
<span class="para">
<xsl:value-of select="." />
</span>
</xsl:template>

</xsl:stylesheet>
