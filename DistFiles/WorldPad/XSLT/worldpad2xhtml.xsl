<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:silfw="http://www.sil.org/fw">
<silfw:file title="XHTML (*.htm)" outputext="htm" description="WorldPad to XHTML" />
	<xsl:template match="/">
		<xsl:for-each select="WpDoc">
			<html>
				<head/>
				<xsl:for-each select="Styles">
					<style type="text/css">
					<xsl:for-each select="StStyle">
						<xsl:for-each select="Name17">
							<xsl:text>.</xsl:text><xsl:value-of select="normalize-space(translate(.,' ',''))"/><xsl:call-template name="name17"><xsl:with-param name="root" select="."/></xsl:call-template>:{<xsl:call-template name="rules17"><xsl:with-param name="location" select="../Rules17"/></xsl:call-template><xsl:for-each select="../Rules17/Prop"><xsl:call-template name="applystyle"><xsl:with-param name="root" select="."/></xsl:call-template></xsl:for-each>}
							<xsl:if test="../Rules17/Prop/@bulNumScheme=101"><xsl:value-of select="normalize-space(translate(.,' ',''))"/>.p{list-style-type:disc}</xsl:if>
							<xsl:if test="../Rules17/Prop/@bulNumScheme=102"><xsl:value-of select="normalize-space(translate(.,' ',''))"/>.p{list-style-type:disc}</xsl:if>
							<xsl:if test="../Rules17/Prop/@bulNumScheme=103"><xsl:value-of select="normalize-space(translate(.,' ',''))"/>.p{list-style-type:circle}</xsl:if>
							<xsl:if test="../Rules17/Prop/@bulNumScheme=104"><xsl:value-of select="normalize-space(translate(.,' ',''))"/>.p{list-style-type:square}</xsl:if>
							<xsl:if test="../Rules17/Prop/@bulNumScheme=105"><xsl:value-of select="normalize-space(translate(.,' ',''))"/>.p{list-style-type:square}</xsl:if>
							<xsl:if test="../Rules17/Prop/@bulNumScheme=106"><xsl:value-of select="normalize-space(translate(.,' ',''))"/>.p{list-style-type:square}</xsl:if>
							<xsl:if test="../Rules17/Prop/@bulNumScheme=107"><xsl:value-of select="normalize-space(translate(.,' ',''))"/>.p{list-style-type:square}</xsl:if>
							<xsl:if test="../Rules17/Prop/@bulNumScheme=108"><xsl:value-of select="normalize-space(translate(.,' ',''))"/>.p{list-style-type:square}</xsl:if>
							<xsl:if test="../Rules17/Prop/@bulNumScheme=109"><xsl:value-of select="normalize-space(translate(.,' ',''))"/>.p{list-style-type:square}</xsl:if>
							<xsl:if test="../Rules17/Prop/@bulNumScheme>109"><xsl:value-of select="normalize-space(translate(.,' ',''))"/>.p{list-style-type:disc}</xsl:if>
							<xsl:if test="../Rules17/Prop/@bulNumScheme=100"><xsl:value-of select="normalize-space(translate(.,' ',''))"/>.p{list-style-type:disc}</xsl:if>

							<xsl:for-each select="../Rules17/Prop/WsStyles9999"><xsl:call-template name="wsstyles"><xsl:with-param name="root" select="."/></xsl:call-template></xsl:for-each>
						</xsl:for-each>
						<xsl:for-each select="BasedOn17">
							HERE<xsl:if test="Uni=''"><xsl:call-template name="defaultlanguage"><xsl:with-param name="root" select="."/></xsl:call-template></xsl:if>
						</xsl:for-each>
					</xsl:for-each>
					</style>
				</xsl:for-each>
				<xsl:for-each select="Body">
					<body>
						<xsl:choose>
							<xsl:when test="normalize-space(@docRightToLeft)='true'">
								<xsl:attribute name="dir">rtl</xsl:attribute></xsl:when>
							<xsl:otherwise>
								<xsl:attribute name="dir">ltr</xsl:attribute>
							</xsl:otherwise>
						</xsl:choose>
						<xsl:for-each select="StTxtPara">
							<p>
							<xsl:if test="StyleRules15/Prop/@bulNumScheme">
								<xsl:if test="StyleRules15/Prop/@bulNumScheme > 99">

									<ul>
										<xsl:for-each select="StyleRules15">
											<xsl:for-each select="Prop">
												<xsl:if test="@namedStyle">
													<xsl:attribute name="class"><xsl:value-of select="normalize-space(translate(@namedStyle,' ',''))"/></xsl:attribute>
												</xsl:if>
												<xsl:if test="@rightToLeft>0">
													<xsl:attribute name="style">dir:rtl</xsl:attribute>
												</xsl:if>
												<span>
													<xsl:attribute name="style">
														<xsl:call-template name="applystyle"><xsl:with-param name="root" select="."/></xsl:call-template>
													</xsl:attribute>
												</span>
											</xsl:for-each>
										</xsl:for-each>


										<xsl:for-each select = "Contents16">
											<li>
												<xsl:if test="../StyleRules15/Prop/@bulNumScheme=101"><xsl:attribute name="style">{list-style-type:disc}</xsl:attribute></xsl:if>
												<xsl:if test="../StyleRules15/Prop/@bulNumScheme=102"><xsl:attribute name="style">{list-style-type:disc}</xsl:attribute></xsl:if>
												<xsl:if test="../StyleRules15/Prop/@bulNumScheme=103"><xsl:attribute name="style">{list-style-type:circle}</xsl:attribute></xsl:if>
												<xsl:if test="../StyleRules15/Prop/@bulNumScheme=104"><xsl:attribute name="style">{list-style-type:square}</xsl:attribute></xsl:if>
												<xsl:if test="../StyleRules15/Prop/@bulNumScheme=105"><xsl:attribute name="style">{list-style-type:square}</xsl:attribute></xsl:if>
												<xsl:if test="../StyleRules15/Prop/@bulNumScheme=106"><xsl:attribute name="style">{list-style-type:square}</xsl:attribute></xsl:if>
												<xsl:if test="../StyleRules15/Prop/@bulNumScheme=107"><xsl:attribute name="style">{list-style-type:square}</xsl:attribute></xsl:if>
												<xsl:if test="../StyleRules15/Prop/@bulNumScheme=108"><xsl:attribute name="style">{list-style-type:square}</xsl:attribute></xsl:if>
												<xsl:if test="../StyleRules15/Prop/@bulNumScheme=109"><xsl:attribute name="style">{list-style-type:square}</xsl:attribute></xsl:if>
												<xsl:if test="../StyleRules15/Prop/@bulNumScheme>109"><xsl:attribute name="style">{list-style-type:disc}</xsl:attribute></xsl:if>

												<xsl:call-template name="contents16"><xsl:with-param name="root" select="."/></xsl:call-template>
											</li>
										</xsl:for-each>
									</ul>
								</xsl:if>
								<xsl:if test="not(StyleRules15/Prop/@bulNumScheme > 99)">
									<xsl:if test="not(preceding-sibling::StTxtPara/StyleRules15/Prop/@bulNumScheme = StyleRules15/Prop/@bulNumScheme)">
										<ol>
											<xsl:for-each select = "StyleRules15">
												<xsl:for-each select="Prop">
													<xsl:if test="@namedStyle">
														<xsl:attribute name="class"><xsl:value-of select="normalize-space(translate(@namedStyle,' ',''))"/></xsl:attribute>
													</xsl:if>
													<xsl:if test="@rightToLeft>0">
														<xsl:attribute name="style">dir:rtl</xsl:attribute>
													</xsl:if>
													<span>
														<xsl:attribute name="style">
														<xsl:call-template name="applystyle"><xsl:with-param name="root" select="."/></xsl:call-template>
														</xsl:attribute>
													</span>
												</xsl:for-each>
											</xsl:for-each>
											<xsl:call-template name="listrecurs"><xsl:with-param name="root" select="."/></xsl:call-template>
										</ol>
									</xsl:if>
								</xsl:if>
							</xsl:if>
							<xsl:if test="not(StyleRules15/Prop/@bulNumScheme)">
								<span>
									<xsl:for-each select="StyleRules15">
										<xsl:for-each select="Prop">
											<xsl:if test="@namedStyle"><xsl:attribute name="class"><xsl:value-of select="normalize-space(translate(@namedStyle,' ',''))"/></xsl:attribute></xsl:if>
											<xsl:if test="@rightToLeft>0"><xsl:attribute name="style">dir:rtl</xsl:attribute></xsl:if>
											<xsl:attribute name = "style">
												<xsl:call-template name="applystyle"><xsl:with-param name="root" select="."/></xsl:call-template>
											</xsl:attribute>
										</xsl:for-each>
									</xsl:for-each>
									<xsl:for-each select = "Contents16">

										<xsl:call-template name="contents16"><xsl:with-param name="root" select="."/></xsl:call-template>
									</xsl:for-each>
								</span>
							</xsl:if>


							</p>
						</xsl:for-each>
					</body>
				</xsl:for-each>
			</html>
		</xsl:for-each>
	</xsl:template>
	<xsl:template match="Prop">
		<p>
			<xsl:attribute name="class"><xsl:value-of select="@namedStyle"/></xsl:attribute><xsl:apply-templates/>
		</p>
	</xsl:template>
	<xsl:template name="name17">
		<xsl:param name="root" select="."/>
		<xsl:for-each select="/WpDoc/Styles/StStyle/BasedOn17">
			<xsl:if test="Uni=$root/Uni">
				<xsl:call-template name="name17"><xsl:with-param name="root" select="../Name17"/></xsl:call-template><xsl:text>,.</xsl:text><xsl:value-of select="translate(../Name17/Uni,' ','')"/>
			</xsl:if>
		</xsl:for-each>
	</xsl:template>
	<xsl:template name="rules17">
		<xsl:param name="location" select="."/>
		<xsl:for-each select="Prop"><xsl:call-template name="prop"><xsl:with-param name="root" select="."/></xsl:call-template></xsl:for-each>
	</xsl:template>
	<xsl:template name="prop">
		<xsl:param name="root" select="."/>
		<xsl:call-template name="applystyle"><xsl:with-param name="root" select = "../WsProp"/></xsl:call-template>
		<xsl:if test="@namedStyle">class=<xsl:value-of select="normalize-space(translate(@namedStyle,' ',''))"/></xsl:if>
		<xsl:apply-templates/>
		</xsl:template>
	<xsl:template name="wsstyles">
		<xsl:param name="root" select="."/>
		<xsl:for-each select="WsProp">
			<xsl:text>.</xsl:text><xsl:value-of select="normalize-space(translate(../../../../Name17/Uni,' ',''))"/><xsl:value-of select="@ws"/>
			<xsl:call-template name="wsstylesrecursive"><xsl:with-param name="root" select="."/></xsl:call-template>

			{<xsl:call-template name="prop"><xsl:with-param name="root" select="."/></xsl:call-template><xsl:call-template name="language"><xsl:with-param name="root" select="."/></xsl:call-template>}
		</xsl:for-each>
	</xsl:template>

	<!-- recursive wsstyles for finding all abs,eng,etc. styles that "inherit" from a style -->
	<xsl:template name="wsstylesrecursive">
		<xsl:param name="root" select="."/>
		<xsl:for-each select="/WpDoc/Styles/StStyle/BasedOn17">
			<xsl:if test="Uni=$root/../../../../Name17/Uni"><xsl:call-template name="wsstylesrecursive"><xsl:with-param name="root" select="../Rules17/Prop/WsStyles9999/WsProp"/></xsl:call-template><xsl:text>,.</xsl:text><xsl:value-of select="translate(../Name17/Uni,' ','')"/><xsl:value-of select="$root/@ws"/></xsl:if>
		</xsl:for-each>
	</xsl:template>


	<xsl:template name="contents16">
		<xsl:param name="root" select="."/>
			<xsl:for-each select="Str">
				<p>
				<xsl:for-each select="Run">
					<span>
						<xsl:if test="@ws"><xsl:attribute name="class"><xsl:value-of select="normalize-space(translate(../../../StyleRules15/Prop/@namedStyle,' ',''))"/><xsl:value-of select="normalize-space(translate(@ws,' ',''))"/></xsl:attribute></xsl:if>
						<span>
							<xsl:if test="@namedStyle"><xsl:attribute name="class"><xsl:value-of select="normalize-space(translate(@namedStyle,' ',''))"/></xsl:attribute>
								<xsl:if test="@ws">
									<span>
										<xsl:attribute name="class"><xsl:value-of select="normalize-space(translate(@namedStyle,' ',''))"/><xsl:value-of select="normalize-space(translate(@ws,' ',''))"/></xsl:attribute>
										<xsl:attribute name="style">
											<xsl:call-template name="applystyle"><xsl:with-param name="root" select = "../Run"/></xsl:call-template>
										</xsl:attribute>
										<xsl:apply-templates/>
									</span>

								</xsl:if>
								<xsl:if test="not(@ws)">
									<span>
										<xsl:attribute name="style">
											<xsl:call-template name="applystyle"><xsl:with-param name="root" select = "../Run"/></xsl:call-template>
										</xsl:attribute>
										<xsl:apply-templates/>
									</span>

								</xsl:if>
							</xsl:if>
							<xsl:if test="not(@namedStyle)">
								<span>
									<xsl:attribute name="style">
										<xsl:call-template name="applystyle"><xsl:with-param name="root" select = "../Run"/></xsl:call-template>
									</xsl:attribute>
									<xsl:apply-templates/>
								</span>
							</xsl:if>
						</span>
					</span>

				</xsl:for-each>
				</p>
			</xsl:for-each>
	</xsl:template>

	<xsl:template name="applystyle">
		<xsl:param name="root" select="."/>
			<xsl:if test="@fontFamily">font-family: <xsl:value-of select="@fontFamily"/>;</xsl:if>
			<xsl:if test="@fontsize">font-size: <xsl:value-of select="@fontsize div 1000"/>pt;</xsl:if>
			<xsl:if test="starts-with((@forecolor),'0')">color: #<xsl:value-of select="substring(@forecolor,3)"/>;</xsl:if>
			<xsl:if test="@forecolor"><xsl:if test="not(starts-with((@forecolor),'0'))">color: <xsl:value-of select="@forecolor"/>;</xsl:if></xsl:if>
			<xsl:if test="starts-with((@backcolor),'0')">background-color: #<xsl:value-of select="substring(@backcolor,3)"/>;</xsl:if>
			<xsl:if test="@backcolor"><xsl:if test="not(starts-with((@backcolor),'0'))">background-color: <xsl:value-of select="@backcolor"/>;</xsl:if></xsl:if>
			<xsl:if test="@superscript = 'super'">vertical-align: super; font-size: 75%;</xsl:if>
			<xsl:if test="@superscript = 'sub'">vertical-align: sub; font-size: 75%;</xsl:if>
			<xsl:if test="@offset">offset: <xsl:value-of select="@offset div 1000"/>pt;</xsl:if>
			<xsl:if test="@align = 'trailing'">text-align: left;</xsl:if>
			<xsl:if test="@fontsize">font-size: <xsl:value-of select="@fontsize div 1000"/>pt;</xsl:if>
			<xsl:if test="@align = 'leading'">text-align: right;</xsl:if>
			<xsl:if test="@align = 'center'">text-align: center;</xsl:if>
			<xsl:if test="@spaceBefore">margin-top: <xsl:value-of select="@spaceBefore div 1000"/>pt;</xsl:if>
			<xsl:if test="@spaceAfter">margin-bottom: <xsl:value-of select="@spaceAfter div 1000"/>pt;</xsl:if>
		<!--	<xsl:if test="@lineHeight">line-height: <xsl:value-of select="@lineHeight div 1000"/>pt;</xsl:if>-->
			<xsl:if test="@rightToLeft=1">direction:rtl;</xsl:if>
		<!--	<xsl:if test="@rightToLeft">direction:ltr;</xsl:if>-->
			<xsl:if test="@borderLeading">border-style:solid;border-left-width: <xsl:value-of select="@borderLeading div 1000"/>pt;</xsl:if>
			<xsl:if test="@borderTrailing">border-style:solid;border-right-width: <xsl:value-of select="@borderTrailing div 1000"/>pt;</xsl:if>
			<xsl:if test="@borderTop">border-style:solid;border-top-width: <xsl:value-of select="@borderTop div 1000"/>pt;</xsl:if>
			<xsl:if test="@borderBottom">border-style:solid;border-bottom-width: <xsl:value-of select="@borderBottom div 1000"/>pt;</xsl:if>
			<xsl:if test="@borderColor">border-color: <xsl:value-of select="@borderColor"/>;</xsl:if>
			<xsl:if test="@firstIndent">text-indent: <xsl:value-of select="@firstIndent div 1000"/>pt;</xsl:if>
			<xsl:if test="@padLeading">padding-left: <xsl:value-of select="@padLeading div 1000"/>pt;</xsl:if>
			<xsl:if test="@padTrailing">padding-right: <xsl:value-of select="@padTrailing div 1000"/>pt;</xsl:if>
			<xsl:if test="@leadingIndent">margin-left:<xsl:if test="@firstIndent"><xsl:value-of select="(@leadingIndent - @firstIndent) div 1000"/>pt;</xsl:if><xsl:if test = "not(@firstIndent)"><xsl:value-of select="@leadingIndent div 1000"/>pt;</xsl:if></xsl:if>
			<xsl:if test="@firstIndent"><xsl:if test = "not(@leadingIndent)">margin-left:<xsl:value-of select="(0 - @firstIndent) div 1000"/>pt;</xsl:if></xsl:if>
			<xsl:if test="@underline='single'">border-bottom:solid;</xsl:if>
			<xsl:if test="@underline='dotted'">border-bottom:dotted;</xsl:if>
			<xsl:if test="@underline='dashed'">border-bottom:dashed;</xsl:if>
			<xsl:if test="@underline='double'">border-bottom:double;</xsl:if>
			<xsl:if test="starts-with((@undercolor),'0')">border-color: #<xsl:value-of select="substring(@undercolor,3)"/>;</xsl:if>
			<xsl:if test="@undercolor"><xsl:if test="not(starts-with((@undercolor),'0'))">border-color: <xsl:value-of select="@undercolor"/>;</xsl:if></xsl:if>
			<xsl:if test="@italic='on'">font-style:italic;</xsl:if>
<!--This is incorrect behaviour used for testing only!--><xsl:if test="@italic='invert'">font-style:italic;</xsl:if>
			<xsl:if test="@bold='on'">font-weight:bolder;</xsl:if>
<!--This is incorrect behaviour used for testing only!--><xsl:if test="@bold='invert'">font-weight:bolder;</xsl:if>

	</xsl:template>
	<xsl:template name="listrecurs">
		<xsl:param name="root" select="."/>
			<xsl:for-each select = "$root/Contents16">
				<li>
					<xsl:if test="../StyleRules15/Prop/@bulNumScheme=10"><xsl:attribute name="style">{list-style-type:decimal}</xsl:attribute></xsl:if>
					<xsl:if test="../StyleRules15/Prop/@bulNumScheme=11"><xsl:attribute name="style">{list-style-type:upperroman}</xsl:attribute></xsl:if>
					<xsl:if test="../StyleRules15/Prop/@bulNumScheme=12"><xsl:attribute name="style">{list-style-type:lowerroman}</xsl:attribute></xsl:if>
					<xsl:if test="../StyleRules15/Prop/@bulNumScheme=13"><xsl:attribute name="style">{list-style-type:upperalpha}</xsl:attribute></xsl:if>
					<xsl:if test="../StyleRules15/Prop/@bulNumScheme=14"><xsl:attribute name="style">{list-style-type:loweralpha}</xsl:attribute></xsl:if>
					<xsl:if test="../StyleRules15/Prop/@bulNumScheme=15"><xsl:attribute name="style">{list-style-type:decimal}</xsl:attribute></xsl:if>
					<xsl:call-template name="contents16"><xsl:with-param name="root" select="."/></xsl:call-template>
				</li>
			</xsl:for-each>
			<xsl:if test="$root/StyleRules15/Prop/@bulNumScheme = $root/following-sibling::StTxtPara[position()=1]/StyleRules15/Prop/@bulNumScheme">
				<xsl:call-template name="listrecurs"><xsl:with-param name="root" select = "$root/following-sibling::StTxtPara[position()=1]"/></xsl:call-template>
			</xsl:if>
	</xsl:template>
	<xsl:template name ="language">
		<xsl:param name="root" select="."/>
			<xsl:for-each select = "/WpDoc/Languages/LgWritingSystem">
				<xsl:if test="@id = $root/@ws">
					<xsl:if test="RightToLeft24/Boolean/@val = 'true'">direction:rtl;</xsl:if>
					<xsl:if test="DefaultSerif24">font-family:<xsl:value-of select="DefaultSerif24/Uni"/>;</xsl:if></xsl:if>
			</xsl:for-each>
	</xsl:template>

	<xsl:template name ="defaultlanguage">
		<xsl:param name="root" select="."/>
			<xsl:for-each select = "/WpDoc/Languages/LgWritingSystem">
				.<xsl:value-of select ="$root/../Name17/Uni"/><xsl:value-of select = "@id"/>{<xsl:if test="RightToLeft24/Boolean/@val = 'true'">direction:rtl;</xsl:if><xsl:if test="DefaultSerif24">font-family:<xsl:value-of select="DefaultSerif24/Uni"/>;</xsl:if>}
			</xsl:for-each>
	</xsl:template>
</xsl:stylesheet>
