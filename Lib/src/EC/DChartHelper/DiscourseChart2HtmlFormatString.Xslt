<?xml version="1.0" encoding="UTF-8" ?>

<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
						xmlns:my="http://www.sil.org/computing/schemas/DiscourseChartData.xsd">

	<xsl:template match="//my:DiscourseChartData">
		<html>
			<body>
				<table id="table1" style="border-style: solid; border-width: 1px; padding-left: 4px; padding-right: 4px; padding-top: 1px; padding-bottom: 1px">
					<tr>
						<td align="center" style="border-style: solid; border-width: 1px">
							<font size="2">
								<b>{0}</b>
							</font>
						</td>
						<td align="center" style="border-style: solid; border-width: 1px">
							<font size="2">
								<b>{1}</b>
							</font>
						</td>
						<td align="center" style="border-style: solid; border-width: 1px">
							<font size="2">
								<b>{2}</b>
							</font>
						</td>
						<td align="center" style="border-style: solid; border-width: 1px">
							<font size="2">
								<b>{3}</b>
							</font>
						</td>
						<td align="center" style="border-style: solid; border-width: 1px">
							<font size="2">
								<b>{4}</b>
							</font>
						</td>
						<td align="center" style="border-style: solid; border-width: 1px">
							<font size="2">
								<b>{5}</b>
							</font>
						</td>
						<td align="center" style="border-style: solid; border-width: 1px">
							<font size="2">
								<b>{6}</b>
							</font>
						</td>
					</tr>
					<xsl:for-each select="my:DiscourseClause">
						<tr>
							<td  style="border-style: solid; border-width: 1px">
								<xsl:value-of select="@{0}"/>
							</td>
							<td  style="border-style: solid; border-width: 1px">
								<p style="margin-top: 3; margin-bottom: 0">
									<font face="{7}" color="{8}" size="2">
										<xsl:value-of select="my:Vernacular/my:{1}"/>
									</font>
								</p>
								<p style="margin-top: 0; margin-bottom: 0">
									<font face="{9}" color="{10}" size="2">
										<xsl:value-of select="my:Transliteration/my:{1}"/>
									</font>
								</p>
								<p style="margin-top: 0; margin-bottom: 3">
									<font face="{11}" color="{12}" size="2">
										<xsl:value-of select="my:Gloss/my:{1}"/>
									</font>
								</p>
							</td>
							<td  style="border-style: solid; border-width: 1px">
								<p style="margin-top: 3; margin-bottom: 0">
									<font face="{7}" color="{8}" size="2">
										<xsl:value-of select="my:Vernacular/my:{2}"/>
									</font>
								</p>
								<p style="margin-top: 0; margin-bottom: 0">
									<font face="{9}" color="{10}" size="2">
										<xsl:value-of select="my:Transliteration/my:{2}"/>
									</font>
								</p>
								<p style="margin-top: 0; margin-bottom: 3">
									<font face="{11}" color="{12}" size="2">
										<xsl:value-of select="my:Gloss/my:{2}"/>
									</font>
								</p>
							</td>
							<td  style="border-style: solid; border-width: 1px">
								<p style="margin-top: 3; margin-bottom: 0">
									<font face="{7}" color="{8}" size="2">
										<xsl:value-of select="my:Vernacular/my:{3}"/>
									</font>
								</p>
								<p style="margin-top: 0; margin-bottom: 0">
									<font face="{9}" color="{10}" size="2">
										<xsl:value-of select="my:Transliteration/my:{3}"/>
									</font>
								</p>
								<p style="margin-top: 0; margin-bottom: 3">
									<font face="{11}" color="{12}" size="2">
										<xsl:value-of select="my:Gloss/my:{3}"/>
									</font>
								</p>
							</td>
							<td  style="border-style: solid; border-width: 1px">
								<p style="margin-top: 3; margin-bottom: 0">
									<font face="{7}" color="{8}" size="2">
										<xsl:value-of select="my:Vernacular/my:{4}"/>
									</font>
								</p>
								<p style="margin-top: 0; margin-bottom: 0">
									<font face="{9}" color="{10}" size="2">
										<xsl:value-of select="my:Transliteration/my:{4}"/>
									</font>
								</p>
								<p style="margin-top: 0; margin-bottom: 3">
									<font face="{11}" color="{12}" size="2">
										<xsl:value-of select="my:Gloss/my:{4}"/>
									</font>
								</p>
							</td>
							<td  style="border-style: solid; border-width: 1px">
								<p style="margin-top: 3; margin-bottom: 0">
									<font face="{7}" color="{8}" size="2">
										<xsl:value-of select="my:Vernacular/my:{5}"/>
									</font>
								</p>
								<p style="margin-top: 0; margin-bottom: 0">
									<font face="{9}" color="{10}" size="2">
										<xsl:value-of select="my:Transliteration/my:{5}"/>
									</font>
								</p>
								<p style="margin-top: 0; margin-bottom: 3">
									<font face="{11}" color="{12}" size="2">
										<xsl:value-of select="my:Gloss/my:{5}"/>
									</font>
								</p>
							</td>
							<td  style="border-style: solid; border-width: 1px">
								<p style="margin-top: 3; margin-bottom: 0">
									<font face="{7}" color="{8}" size="2">
										<xsl:value-of select="my:Vernacular/my:{6}"/>
									</font>
								</p>
								<p style="margin-top: 0; margin-bottom: 0">
									<font face="{9}" color="{10}" size="2">
										<xsl:value-of select="my:Transliteration/my:{6}"/>
									</font>
								</p>
								<p style="margin-top: 0; margin-bottom: 3">
									<font face="{11}" color="{12}" size="2">
										<xsl:value-of select="my:Gloss/my:{6}"/>
									</font>
								</p>
							</td>
						</tr>
						<xsl:if test="my:FreeTranslation">
							<tr>
								<td colspan="7" style="border-style: solid; border-width: 1px">
									<font face="{11}" color="#000000" size="2">
										<xsl:value-of select="my:FreeTranslation"/>
									</font>
								</td>
							</tr>
						</xsl:if>
					</xsl:for-each>
				</table>
			</body>
		</html>
	</xsl:template>

</xsl:stylesheet>
