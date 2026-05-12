<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:my="http://schema.infor.com/InforOAGIS/2">
	<xsl:output method="xml" version="1.0" encoding="UTF-8" indent="yes" />

	<xsl:template match="@*|node()" priority="-1">
		<xsl:copy>
			<xsl:apply-templates select="@*|node()"/>
		</xsl:copy>
	</xsl:template>

	<!-- Change KeyPath="yes" to KeyPath="no" in files harvested by heat.exe.
		This allows a later base installer to overwrite the higher versioned file with a lower versioned file.
		Patch files do NOT have this capability; this hack results in an error during the patch creation process -->
	<xsl:template match="@KeyPath[.='yes']">
		<xsl:attribute name="KeyPath">
			<xsl:value-of select="'no'"/>
		</xsl:attribute>
	</xsl:template>

	<xsl:template name="HeatHarvestBaseFileName">
		<xsl:param name="path"/>
		<xsl:choose>
			<xsl:when test="contains($path, '\')">
				<xsl:call-template name="HeatHarvestBaseFileName">
					<xsl:with-param name="path" select="substring-after($path, '\')"/>
				</xsl:call-template>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="$path"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<!--
		Remove single-File Heat components whose file basename is listed in heat-exclude.xml (same directory
		as this XSL). FieldWorks uses this for Newtonsoft.Json.dll + authored NewtonsoftJsonApp / NewtonsoftJsonGac
		in Shared/Common/CustomComponents.wxi.
		Also drop ComponentRef rows that point at removed components (Heat -gg leaves refs in HarvestedAppFiles).
	-->
	<xsl:template match="*[local-name()='ComponentRef']" priority="2">
		<xsl:variable name="rid" select="@Id"/>
		<xsl:variable name="comp" select="//*[local-name()='Component'][@Id = $rid]"/>
		<xsl:choose>
			<xsl:when test="count($comp) = 0"/>
			<xsl:when test="count($comp/*[local-name()='File']) = 1">
				<xsl:variable name="src" select="string($comp/*[local-name()='File']/@Source)"/>
				<xsl:variable name="base">
					<xsl:call-template name="HeatHarvestBaseFileName">
						<xsl:with-param name="path" select="translate($src, '/', '\')"/>
					</xsl:call-template>
				</xsl:variable>
				<xsl:variable name="isExcluded">
					<xsl:for-each select="document('heat-exclude.xml')">
						<xsl:value-of select="count(/*[local-name()='HeatHarvestExcludes']/*[local-name()='Exclude'][@Name and translate(@Name, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz') = translate($base, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')])"/>
					</xsl:for-each>
				</xsl:variable>
				<xsl:choose>
					<xsl:when test="$isExcluded &gt; 0"/>
					<xsl:otherwise>
						<xsl:copy>
							<xsl:apply-templates select="@*|node()"/>
						</xsl:copy>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:when>
			<xsl:otherwise>
				<xsl:copy>
					<xsl:apply-templates select="@*|node()"/>
				</xsl:copy>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template match="*[local-name()='Component'][count(*[local-name()='File'])=1]" priority="1">
		<xsl:variable name="src" select="string(*[local-name()='File']/@Source)"/>
		<xsl:variable name="base">
			<xsl:call-template name="HeatHarvestBaseFileName">
				<xsl:with-param name="path" select="translate($src, '/', '\')"/>
			</xsl:call-template>
		</xsl:variable>
		<xsl:variable name="isExcluded">
			<xsl:for-each select="document('heat-exclude.xml')">
				<xsl:value-of select="count(/*[local-name()='HeatHarvestExcludes']/*[local-name()='Exclude'][@Name and translate(@Name, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz') = translate($base, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')])"/>
			</xsl:for-each>
		</xsl:variable>
		<xsl:choose>
			<xsl:when test="$isExcluded &gt; 0"/>
			<xsl:otherwise>
				<xsl:copy>
					<xsl:apply-templates select="@*|node()"/>
				</xsl:copy>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
</xsl:stylesheet>
