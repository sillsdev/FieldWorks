<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
version="1.0">
<xsl:output method="xml" encoding="UTF-8" indent="yes" />

<!--
  The output of Csv2Xml.exe has the following form:
	 &lt;doc>
	 &lt;row Test-Case="Data1" Language-Project="Data2" Back-Trans="Data3"
Trans-Notes="Data4" What-to-Import="Data5" Intro="Data6"
Types-of-Data="Data7" MRF-Scr="Data8" MRF-BT="Data9" MRF-Notes="Data10"
Version="Data11" Date="Data12" Run-Test="Data13" />
		:        :       :
	 &lt;row Test-Case="Data1" Language-Project="Data2" Back-Trans="Data3"
Trans-Notes="Data4" What-to-Import="Data5" Intro="Data6"
Types-of-Data="Data7" MRF-Scr="Data8" MRF-BT="Data9" MRF-Notes="Data10"
Version="Data11" Date="Data12" Run-Test="Data13" />
	 &lt;/doc>

Change it to

	&lt;tests>
	&lt;test case="Data1" project="Data2" back-trans="Data3" notes="Data4"
scope="Data5" intro="Data6" data-types="Data7" mfr="Data8" mfr-bt="Data9"
mfr-notes="Data10" version="Data11" date="Data12" run-test="Data13" />
		:        :       :
	&lt;test case="Data1" project="Data2" back-trans="Data3" notes="Data4"
scope="Data5" intro="Data6" data-types="Data7" mfr="Data8" mfr-bt="Data9"
mfr-notes="Data10" version="Data11" date="Data12" run-test="Data13" />
	&lt;/tests>

-->

<xsl:template match="/">
   <xsl:processing-instruction name="xml-stylesheet">
	  <xsl:text>type="text/xsl" href="XmlFromCsv.xsl"</xsl:text>
   </xsl:processing-instruction>
<tests>
   <xsl:apply-templates select="doc"/>
</tests>
</xsl:template>

<xsl:template match="doc">
   <xsl:apply-templates select="row"/>
</xsl:template>

<xsl:template match="row">
  <xsl:if test="@Test-Case != ''">
	<test>
	 <xsl:apply-templates select="@*"/>
	</test>
  </xsl:if>
</xsl:template>

<xsl:template match="@*">

 <xsl:choose>
 <xsl:when test="name()='Test-Case'">
   <xsl:attribute name="case"><xsl:value-of select="."/></xsl:attribute>
 </xsl:when>
 <xsl:when test="name()='Language-Project'">
   <xsl:attribute name="project"><xsl:value-of select="."/></xsl:attribute>
 </xsl:when>
 <xsl:when test="name()='Back-Trans'">
   <xsl:attribute name="back-trans"><xsl:value-of select="."/>
</xsl:attribute>
 </xsl:when>
 <xsl:when test="name()='Trans-Notes'">
   <xsl:attribute name="notes"><xsl:value-of select="."/></xsl:attribute>
 </xsl:when>
 <xsl:when test="name()='What-to-Import'">
   <xsl:attribute name="scope"><xsl:value-of select="."/></xsl:attribute>
 </xsl:when>
 <xsl:when test="name()='Intro'">
   <xsl:attribute name="intro"><xsl:value-of select="."/></xsl:attribute>
 </xsl:when>
 <xsl:when test="name()='Types-of-Data'">
   <xsl:attribute name="data-types"><xsl:value-of select="."/>
</xsl:attribute>
 </xsl:when>
 <xsl:when test="name()='MRF-Scr'">
   <xsl:attribute name="mfr"><xsl:value-of select="."/></xsl:attribute>
 </xsl:when>
 <xsl:when test="name()='MRF-BT'">
   <xsl:attribute name="mfr-bt"><xsl:value-of select="."/></xsl:attribute>
 </xsl:when>
 <xsl:when test="name()='MRF-Notes'">
   <xsl:attribute name="mfr-notes"><xsl:value-of select="."/>
</xsl:attribute>
 </xsl:when>
 <xsl:when test="name()='Version'">
   <xsl:attribute name="version"><xsl:value-of select="."/></xsl:attribute>
 </xsl:when>
 <xsl:when test="name()='Date'">
   <xsl:attribute name="date"><xsl:value-of select="."/></xsl:attribute>
 </xsl:when>
 <xsl:when test="name()='Run-Test'">
   <xsl:attribute name="run-test"><xsl:value-of select="."/>
</xsl:attribute>
 </xsl:when>
 <xsl:otherwise> <!-- unchanged or unexpected attribute name -->
   <xsl:attribute name="{name()}"><xsl:value-of select="."/>
</xsl:attribute>
 </xsl:otherwise>
 </xsl:choose>

</xsl:template>

</xsl:stylesheet>