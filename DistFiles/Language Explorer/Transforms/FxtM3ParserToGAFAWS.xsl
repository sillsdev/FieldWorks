<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="xml" version="1.0" encoding="UTF-8" indent="yes"/>
	<!--
================================================================
M3 to Sets of GAFAWS data (used to determine orderclass for XAmple)
  Input:    XML output from M3GAFAWS.fxt
  Output: Sets of GAFAWS data

================================================================
Revision History is at the end of this file.

- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Preamble
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<!-- Using keys instead of IDs (so no DTD or XSD required) -->
	<xsl:key name="PrefixSlots" match="PrefixSlots" use="@dst"/>
	<xsl:key name="SuffixSlots" match="SuffixSlots" use="@dst"/>
	<!--
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Main template
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
-->
	<xsl:template match="/PartOfSpeech">
		<GAFAWSData>
			<WordRecords>
				<xsl:for-each select="descendant-or-self::MoInflAffixTemplate[PrefixSlots or SuffixSlots]">
					<WordRecord>
						<xsl:attribute name="WRID">
							<xsl:value-of select="@Id"/>
						</xsl:attribute>
						<xsl:if test="count(PrefixSlots) > 0">
							<Prefixes>
								<xsl:for-each select="PrefixSlots">
									<Affix>
										<xsl:attribute name="MIDREF">
											<xsl:value-of select="@dst"/>
										</xsl:attribute>
									</Affix>
								</xsl:for-each>
							</Prefixes>
						</xsl:if>
						<Stem MIDREF="R"/>
						<xsl:if test="count(SuffixSlots) > 0">
							<Suffixes>
								<xsl:for-each select="SuffixSlots">
									<Affix>
										<xsl:attribute name="MIDREF">
											<xsl:value-of select="@dst"/>
										</xsl:attribute>
									</Affix>
								</xsl:for-each>
							</Suffixes>
						</xsl:if>
					</WordRecord>
				</xsl:for-each>
			</WordRecords>
			<Morphemes>
				<Morpheme MID="R" type="s"/>
				<xsl:for-each select="descendant-or-self::MoInflAffixSlot">
					<Morpheme>
						<xsl:attribute name="MID">
							<xsl:value-of select="@Id"/>
						</xsl:attribute>
						<xsl:attribute name="type">
							<xsl:choose>
								<xsl:when test="key('PrefixSlots',@Id)">
									<xsl:text>pfx</xsl:text>
								</xsl:when>
								<xsl:otherwise>
									<xsl:text>sfx</xsl:text>
								</xsl:otherwise>
							</xsl:choose>
						</xsl:attribute>
					</Morpheme>
					<!-- check for any circumfixes which have both prefix and suffix slots and add the suffix one
							(the prefix one got added above)
					-->
					<xsl:if test="key('PrefixSlots',@Id) and key('SuffixSlots',@Id)">
						<Morpheme MID="{@Id}" type="sfx"/>
					</xsl:if>
				</xsl:for-each>
			</Morphemes>
			<Classes>
				<PrefixClasses/>
				<SuffixClasses/>
			</Classes>
			<Challenges/>
		</GAFAWSData>
	</xsl:template>
</xsl:stylesheet>
