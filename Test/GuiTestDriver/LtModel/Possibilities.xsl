<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	xmlns:ms="urn:schemas-microsoft-com:xslt">
<xsl:output method="html" encoding="UTF-8" indent="yes" />

<xsl:variable name="root" select="/fwuiml"/>
<xsl:variable name="sections" select="document(/fwuiml/section/@file)"/>

<xsl:template match="/fwuiml">
  <html>
  <head>
	 <title><xsl:value-of select="@model"/></title>
  </head>
  <body>
	<h1>GUI Model for <xsl:value-of select="@model"/></h1>
	<h2>Sections and leaf node accessible paths</h2>
	<xsl:for-each select="$sections/*">
		<h3><xsl:value-of select="name()"/></h3>
		<!-- find all possible legal destinations and their accessible paths -->
		<table border="1">
		  <xsl:apply-templates select="//@role[.='menu']"/>
		  <xsl:apply-templates select="//@role[.='sidebutton']"/>
		  <xsl:apply-templates select="//*[@role='view']//*[(not(*) and name()!='ex') or ex]" mode="path"/>
		</table>
	</xsl:for-each>
  </body>
  </html>
</xsl:template>

<xsl:template match="@role">
	<tr>
	  <td><!--xsl:value-of select="name(..)"/-->
		 <xsl:call-template name="BuildAppPath">
			<xsl:with-param name="node" select=".."/>
		 </xsl:call-template>
	  </td>
	  <td> <!-- show the full path -->
		 <xsl:call-template name="buildAccPath">
		   <xsl:with-param name="node" select=".."/>
		 </xsl:call-template>
	  </td>
	  <td> <!-- when some view or tool -->
		 <xsl:for-each select="../@*[name()='tool' or name()='view']">
			<xsl:variable name="ref">
			  <xsl:call-template name="resolveRefs">
				<xsl:with-param name="path" select="."/>
				<xsl:with-param name="node" select=".."/>
			  </xsl:call-template>
			</xsl:variable>
			<xsl:value-of select="concat('(',name(),')',$ref,' ')"/>
		 </xsl:for-each>
		 <xsl:text disable-output-escaping="yes">&amp;nbsp;</xsl:text>
	  </td>
	  <td>
		 <xsl:value-of select="concat('&lt;',../ex/@instr,' ')"/>
		 <xsl:for-each select="../ex/@*[name()!='instr']">
			<xsl:value-of select="concat(name(),'=&quot;',.,'&quot; ')"/>
		 </xsl:for-each>
		 <xsl:text>&gt;</xsl:text>
	  </td>
	</tr>
</xsl:template>

<xsl:template match="*" mode="path">
  <xsl:if test="name() != 'var'">
	<tr>
	  <td><!--xsl:value-of select="name()"/-->
		 <xsl:call-template name="BuildAppPath">
			<xsl:with-param name="node" select="."/>
		 </xsl:call-template>
	  </td>
	  <td> <!-- show the full path -->
		 <xsl:call-template name="buildAccPath">
		   <xsl:with-param name="node" select="."/>
		 </xsl:call-template>
	  </td>
	  <td> <!-- when some view or tool -->
		 <xsl:for-each select="@*[name()='tool' or name()='view']">
			<xsl:variable name="ref">
			  <xsl:call-template name="resolveRefs">
				<xsl:with-param name="path" select="."/>
				<xsl:with-param name="node" select=".."/>
			  </xsl:call-template>
			</xsl:variable>
			<xsl:value-of select="concat('(',name(),')',$ref,' ')"/>
		 </xsl:for-each>
		 <xsl:text disable-output-escaping="yes">&amp;nbsp;</xsl:text>
	  </td>
	  <td>
		 <xsl:value-of select="concat('&lt;',./ex/@instr,' ')"/>
		 <xsl:for-each select="./ex/@*[name()!='instr']">
			<xsl:value-of select="concat(name(),'=&quot;',.,'&quot; ')"/>
		 </xsl:for-each>
		 <xsl:text>&gt;</xsl:text>
	  </td>
	</tr>
  </xsl:if>
</xsl:template>

<xsl:template name="buildAccPath">
  <xsl:param name="node"/>
  <!-- pre recurse the path up to the root via parents -->
  <xsl:choose>
  <xsl:when test="$node/@root='yes'"/>
  <xsl:otherwise>
	 <xsl:call-template name="buildAccPath">
	   <xsl:with-param name="node" select="$node/.."/>
	 </xsl:call-template>
  </xsl:otherwise>
  </xsl:choose>
  <xsl:choose>
  <xsl:when test="$node/@path">
	 <xsl:call-template name="resolveRefs">
	   <xsl:with-param name="path" select="$node/@path"/>
	   <xsl:with-param name="node" select="$node"/>
	 </xsl:call-template>
  </xsl:when>
  <xsl:when test="$node/@path-ref">
	 <xsl:variable name="refd-path" select="$node/@path-ref"/>
	 <xsl:call-template name="resolveRefs">
	   <xsl:with-param name="path" select="$refd-path"/>
	   <xsl:with-param name="node" select="$node"/>
	 </xsl:call-template>
  </xsl:when>
  <xsl:otherwise>
  </xsl:otherwise>
  </xsl:choose>
</xsl:template>

<xsl:template name="resolveRefs">
  <xsl:param name="path"/>
  <xsl:param name="node"/>
  <xsl:variable name="preDollar" select="substring-before($path,'$')"/>
  <xsl:variable name="postDollar" select="substring-after($path,'$')"/>
  <xsl:choose>
  <xsl:when test="$path = ''"/> <!-- no path to begin with -->
  <xsl:when test="$preDollar='' and $postDollar=''"> <!-- no $ in the path, so, no var, just use the path -->
	 <xsl:value-of select="$path"/>
  </xsl:when>
  <xsl:when test="$postDollar=''"> <!-- just a $ at the end of the path, so, no var, just use the path -->
	 <xsl:value-of select="$path"/>
  </xsl:when>
  <xsl:otherwise>
	 <xsl:value-of select="$preDollar"/>
	 <xsl:variable name="maybeVarRef1" select="substring-before($postDollar,';')"/>
	 <xsl:variable name="maybeVarRef2" select="substring-before($postDollar,' ')"/>
	 <xsl:variable name="varName">
	   <xsl:call-template name="resolveName">
		 <xsl:with-param name="full" select="$postDollar"/>
		 <xsl:with-param name="semi" select="$maybeVarRef1"/>
		 <xsl:with-param name="space" select="$maybeVarRef2"/>
	   </xsl:call-template>
	 </xsl:variable>

	 <xsl:choose>
	   <xsl:when test="$varName=''"> <!-- might still get nothing from $; -->
		  <xsl:value-of select="'$'"/>
	   </xsl:when>
	   <xsl:otherwise><!-- varName is a name, can it be found? -->

		  <!-- try to find a matching var on the ancestor axis and decode it too -->
		  <!--(<xsl:value-of select="$varName"/>= -->
		  <xsl:variable name="deref">
			 <xsl:call-template name="derefVariable">
				<xsl:with-param name="node" select="$node"/>
				<xsl:with-param name="varName" select="$varName" />
			 </xsl:call-template>
		  </xsl:variable>
		  <xsl:call-template name="resolveRefs">
			 <xsl:with-param name="path" select="ms:node-set($deref)"/>
			 <xsl:with-param name="node" select="$node"/>
		  </xsl:call-template>

		  <!-- if found continue parsing the path -->
		  <xsl:variable name="postVar0" select="substring-after($path,concat('$',$varName))"/>
		  <xsl:variable name="postVar" select="substring($postVar0,2)"/>
		  <xsl:if test="$postVar != ''">
			<xsl:call-template name="resolveRefs">
			  <xsl:with-param name="path" select="$postVar"/>
			  <xsl:with-param name="node" select="$node"/>
			</xsl:call-template>
		  </xsl:if>
	   </xsl:otherwise>
	   </xsl:choose>

  </xsl:otherwise>
  </xsl:choose>

</xsl:template>

<!-- find the most likely name from the 3 possible -->
<xsl:template name="resolveName">
  <xsl:param name="full" /> <!-- the longest -->
  <xsl:param name="semi" />
  <xsl:param name="space" />
  <!-- find the shortest -->
  <xsl:choose>
  <xsl:when test="$space = '' and $semi = ''">
	  <xsl:value-of select="$full"/>
  </xsl:when>
  <xsl:when test="$space = ''">
	  <xsl:value-of select="$semi"/>
  </xsl:when>
  <xsl:when test="$semi = ''">
	  <xsl:value-of select="$space"/>
  </xsl:when>
  <xsl:when test="string-length($semi) > string-length($space)">
	  <xsl:value-of select="$space"/>
  </xsl:when>
  <xsl:otherwise>
	  <xsl:value-of select="$semi"/>
  </xsl:otherwise>
  </xsl:choose>
</xsl:template>

<!-- dereference the variable, local then ancestral -->
<xsl:template name="derefVariable">
  <xsl:param name="node" />
  <xsl:param name="varName" />
  <!-- is it a local referant -->
  <xsl:variable name="localVar" select="$node/@*[name()=$varName]"/>
  <xsl:choose>
  <xsl:when test="$localVar != ''">
	  <xsl:value-of select="$localVar"/>
  </xsl:when>
  <xsl:otherwise> <!-- is it ancestral? -->
	  <xsl:variable name="anVar" select="$node/ancestor::*/var[@id=$varName]/@set"/>
	  <xsl:choose>
	  <xsl:when test="$anVar != ''">
		  <xsl:value-of select="$anVar"/>
	  </xsl:when>
	  <xsl:otherwise> <!-- is it in the root file? -->
		  <xsl:value-of select="$root//var[@id=$varName]/@set"/>
	  </xsl:otherwise>
	  </xsl:choose>
  </xsl:otherwise>
  </xsl:choose>
</xsl:template>

<!-- Assemble the appPath from this model node to its root -->
<xsl:template name="BuildAppPath">
  <xsl:param name="node" />
  <xsl:choose>
  <xsl:when test="$node/@root='yes'"/>
  <xsl:otherwise>
	 <xsl:call-template name="BuildAppPath">
	   <xsl:with-param name="node" select="$node/.."/>
	 </xsl:call-template>
  </xsl:otherwise>
  </xsl:choose>
  <xsl:value-of select="name($node)"/>
  <xsl:if test="$node/*[name()!='ex']">
	 <xsl:text>/</xsl:text>
  </xsl:if>
</xsl:template>


</xsl:stylesheet>
