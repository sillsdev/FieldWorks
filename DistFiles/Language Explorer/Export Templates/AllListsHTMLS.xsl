<?xml version="1.0" encoding="UTF-8"?>
<!-- XSLT Transform for producing list of Languages from FLEx -->
<!-- Copyright (c) 2022 SIL International
	This software is licensed under the LGPL, version 2.1 or later
	(http://www.gnu.org/licenses/lgpl-2.1.html) -->
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
	<xsl:output encoding="UTF-8" method="html" indent="no"/>
	<!--
		abbr for semantic domain
	-->
	<xsl:template match="abbr[parent::sditem]">
		<span class="sdAbbr">
			<xsl:value-of select="str"/>
		</span>
		<xsl:if test="preceding-sibling::name">
			<xsl:for-each select="preceding-sibling::name/str">
				<span class="langName">
					<xsl:value-of select="."/>
				</span>
			</xsl:for-each>
		</xsl:if>
		<xsl:if test="not(not(normalize-space(following-sibling::conf) = '') or not(normalize-space(following-sibling::lnc) = '') or not(normalize-space(following-sibling::ocm) = '') or not(normalize-space(following-sibling::res) = '') or normalize-space(following-sibling::reschrs) = '') or not(normalize-space(following-sibling::restrs) = '') or not(normalize-space(following-sibling::stat) = '')">
			<br/>
		</xsl:if>
	</xsl:template>
	<!--
		conf
	-->
	<xsl:template match="conf">
		<span class="confidence">
			<xsl:value-of select="."/>
		</span>
	</xsl:template>
	<!--
		csid
	-->
	<xsl:template match="csid">
		<xsl:if test="not(normalize-space(.) = '') ">
			<span class="catsrc">
				<xsl:value-of select="."/>
			</span>
		</xsl:if>
	</xsl:template>
	<!--
		edu
	-->
	<xsl:template match="edu">
		<span class="education">
			<xsl:value-of select="."/>
		</span>
	</xsl:template>
	<!--
		exsen
	-->
	<xsl:template match="exsen">
		<xsl:if test="not(normalize-space(.) = '') ">
			<xsl:for-each select="str">
				<span class="sdSentences">
					<xsl:value-of select="."/>
				</span>
			</xsl:for-each>
		</xsl:if>
	</xsl:template>
	<!--
		exwrd
	-->
	<xsl:template match="exwrd">
		<xsl:if test="not(normalize-space(.) = '') ">
			<xsl:for-each select="str">
				<span class="sdWords">
					<xsl:value-of select="."/>
				</span>
			</xsl:for-each>
		</xsl:if>
	</xsl:template>
	<!--
		gen
	-->
	<xsl:template match="gen">
		<xsl:if test=". = 1">
			<span class="gender">Male</span>
		</xsl:if>
		<xsl:if test=". = 2">
			<span class="gender">Female</span>
		</xsl:if>
	</xsl:template>
	<!--
		ires
	-->
	<xsl:template match="isres">
		<span class="isResearcher">
			<xsl:value-of select="."/>
		</span>
	</xsl:template>
	<!--
		item
	-->
	<xsl:template match="item | aitem | locitem | mtitem | peritem | positem | sditem | lrtitem | letitem">
		<div class="listItem">
			<xsl:apply-templates/>
		</div>
	</xsl:template>
	<!--
		items
	-->
	<xsl:template match="items | subitems">
		<xsl:if test="name(*[1]) = 'sditem'">
			<xsl:apply-templates>
				<xsl:sort select="abbr/str"/>
			</xsl:apply-templates>
		</xsl:if>
		<xsl:if test="name(*[1]) != 'sditem'">
			<xsl:apply-templates>
				<xsl:sort select="name/str"/>
			</xsl:apply-templates>
		</xsl:if>
	</xsl:template>
	<!--
		list
	-->
	<xsl:template match="list">
		<p class="listName">
			<xsl:value-of select="name/str"/>
		</p>
		<xsl:apply-templates/>
	</xsl:template>
	<!--
		lists
	-->
	<xsl:template match="lists">
		<html>
			<head>
				<title>All Lists</title>
<style>
.alias + .alias:before {
	content: "; ";
}

.alias:before {
	content:" Alias: ";
	font-style: italic;
}

.catsrc:before {
	content: "Catalog source: ";
	font-style: italic;
}

.catsrc {
	font-size: smaller;
}

.closeParen:after {
	content: ") "
}

.confidence:before {
	content: "Confidence: ";
	font-style: italic;
}

.confidence {
	font-size: smaller;
}

.description + .description:before {
	content: "; "
}

.description:before {
	content:"\A";
	white-space:pre;
}

.description {
	font-weight: normal;
}

.discussion:before {
	content:"\A";
	white-space:pre;
}

.discussion {
	font-weight: normal;
}

.education:before {
	content: "Education: ";
	font-style: italic;
}

.education {
	font-weight: normal;
	font-size: smaller;
}

.gender:before {
	content: "Gender: ";
	font-style: italic;
}

.gender {
	font-size: smaller;
}

.isResearcher:before {
	content: "Researcher: ";
	font-style: italic;
}

.isResearcher {
	font-size: smaller;
}

.langAbbr + .langAbbr:before {
	content: "; "
}

.langName + .langName:before {
	content: "; "
}

.langName {
	font-weight: bold;
}

.listItem {
	padding-top: 6px ;
	padding-left: 22px ;
	text-indent: -22px ;
}

.listName {
	font-weight:bold;
	font-size:xx-large;
}

.loweNida:before {
	content: "L&amp;N: ";
	font-style: italic;
}

.loweNida {
	font-size: smaller;
}

.mapType:before {
	content: "Mapping type: ";
	font-style: italic;
}

.mapType {
	font-size: smaller;
}

.ocm:before {
	content: "Ocm: ";
	font-style: italic;
}

.ocm {
	font-size: smaller;
}

.openParen:before {
	content: " ("
}

.position + .position:before {
	content: "; "
}

.position:before {
	content: "Positions: ";
	font-style: italic;
}

.position {
	font-weight: normal;
	font-size: smaller;
}

.postfix:before {
	content: "Postfix: ";
	font-style: italic;
}

.postfix {
	font-size: smaller;
}

.prefix:before {
	content: "Prefix: ";
	font-style: italic;
}

.prefix {
	font-size: smaller;
}

.researcher + .researcher:before {
	content: "; "
}
.researcher:before {
	content: "Researchers: ";
	font-style: italic;
}

.researcher {
	font-weight: normal;
	font-size: smaller;
}

.residence + .residence:before {
	content: "; "
}

.residence:before {
	content: "Residences: ";
	font-style: italic;
}

.residence {
	font-weight: normal;
	font-size: smaller;
}

.restriction + .restriction:before {
	content: "; "
}

.restriction:before {
	content: "Restrictions: ";
	font-style: italic;
}

.restriction {
	font-weight: normal;
	font-size: smaller;
}

.reverseAbbr + .reverseAbbr:before {
	content: "; ";
}

.reverseAbbr:before {
	content: "Reverse abbrev: ";
	font-style: italic;
}

.reverseAbbr {
	font-size: smaller;
}

.reverseName + .reverseName:before {
	content: "; ";
}

.reverseName:before {
	content: "Reverse name: ";
	font-style: italic;
}

.reverseName {
	font-size: smaller;
}

.sdAbbr:after {
	content: " ";
}

.sdAbbr {
	font-weight: bold;
}

.sdQuestion + .sdQuestion:before {
	content: "; "
}

.sdQuestion:before {
	content:"\A";
	white-space:pre;
}

.sdSentences + .sdSentences:before {
	content: "; "
}

.sdSentences:before {
	content:"\A • Sentences: ";
	white-space:pre;
	font-style: italic;
}

.sdWords + .sdWords:before {
	content: "; "
}

.sdWords:before {
	content:"\A ‣ Words: ";
	font-style:italic;
	white-space:pre;
}

.secorder:before {
	content: "Secondary order: ";
	font-style: italic;
}

.secorder {
	font-size: smaller;
}

.status:before {
	content: "Status: ";
	font-style: italic;
}

.status {
	font-size: smaller;
}
</style>
			</head>
			<body>
				<xsl:apply-templates>
					<xsl:sort select="name/str"/>
				</xsl:apply-templates>
			</body>
		</html>
	</xsl:template>
	<!--
		lnc
	-->
	<xsl:template match="lnc">
		<xsl:if test="not(normalize-space(.) = '') ">
			<span class="loweNida">
				<xsl:value-of select="."/>
			</span>
		</xsl:if>
	</xsl:template>
	<!--
		maptyp
	-->
	<xsl:template match="maptyp">
		<xsl:if test=". = 0">
			<span class="mapType">Sense Collection</span>
		</xsl:if>
		<xsl:if test=". = 1">
			<span class="mapType">Sense Pair</span>
		</xsl:if>
		<xsl:if test=". = 2">
			<span class="mapType">Sense Pair Asymmetric</span>
		</xsl:if>
		<xsl:if test=". = 3">
			<span class="mapType">Sense Tree</span>
		</xsl:if>
		<xsl:if test=". = 4">
			<span class="mapType">Sense Sequence</span>
		</xsl:if>
		<xsl:if test=". = 5">
			<span class="mapType">Entry Collection</span>
		</xsl:if>
		<xsl:if test=". = 6">
			<span class="mapType">Entry Pair</span>
		</xsl:if>
		<xsl:if test=". = 7">
			<span class="mapType">Entry Pair Asymmetric</span>
		</xsl:if>
		<xsl:if test=". = 8">
			<span class="mapType">Entry Tree</span>
		</xsl:if>
		<xsl:if test=". = 9">
			<span class="mapType">Entry Sequence</span>
		</xsl:if>
		<xsl:if test=". = 10">
			<span class="mapType">Entry/Sense Collection</span>
		</xsl:if>
		<xsl:if test=". = 11">
			<span class="mapType">Entry/Sense Pair</span>
		</xsl:if>
		<xsl:if test=". = 12">
			<span class="mapType">Entry/Sense Pair Asymmetric</span>
		</xsl:if>
		<xsl:if test=". = 13">
			<span class="mapType">Entry/Sense Tree</span>
		</xsl:if>
		<xsl:if test=". = 14">
			<span class="mapType">Entry/Sense Seequence</span>
		</xsl:if>
		<xsl:if test=". = 15">
			<span class="mapType">Sense Unidirectional</span>
		</xsl:if>
		<xsl:if test=". = 16">
			<span class="mapType">Entry Unidirectional</span>
		</xsl:if>
		<xsl:if test=". = 17">
			<span class="mapType">Entry/Sense Unidifrectional</span>
		</xsl:if>
	</xsl:template>
	<!--
		name all except sditem
	-->
	<xsl:template match="name[parent::item or parent::aitem or parent::locitem or parent::mtitem or parent::peritem or parent::positem or parent::letitem or parent::lrtitem]">
		<xsl:for-each select="str">
			<span class="langName">
				<xsl:value-of select="."/>
			</span>
		</xsl:for-each>
		<xsl:if test="following-sibling::abbr">
			<xsl:if test="not(normalize-space(following-sibling::abbr) = '') ">
				<span class="openParen"/>
				<xsl:for-each select="following-sibling::abbr/str">
					<span class="langAbbr">
						<xsl:value-of select="."/>
					</span>
				</xsl:for-each>
				<span class="closeParen"/>
			</xsl:if>
		</xsl:if>
		<xsl:if test="following-sibling::alias">
			<xsl:for-each select="following-sibling::alias/str">
				<span class="alias">
					<xsl:value-of select="."/>
				</span>
			</xsl:for-each>
		</xsl:if>
		<xsl:if test="following-sibling::descr">
			<xsl:if test="not(normalize-space(following-sibling::descr) = '') ">
				<xsl:for-each select="following-sibling::descr/str">
					<span class="description">
						<xsl:value-of select="."/>
					</span>
				</xsl:for-each>
			</xsl:if>
		</xsl:if>
		<xsl:if test="following-sibling::disc">
			<xsl:if test="not(normalize-space(following-sibling::disc) = '') ">
				<span class="discussion">
					<xsl:value-of select="following-sibling::disc"/>
				</span>
			</xsl:if>
		</xsl:if>
		<xsl:if test="not(not(normalize-space(following-sibling::conf) = '') or not(normalize-space(following-sibling::csid) = '') or not(normalize-space(following-sibling::edu) = '') or not(normalize-space(following-sibling::gen) = '') or not(normalize-space(following-sibling::isres) = '') or not(normalize-space(following-sibling::maptyp) = '') or not(normalize-space(following-sibling::pofix) = '') or not(normalize-space(following-sibling::posn) = '') or not(normalize-space(following-sibling::prefix) = '') or not(normalize-space(following-sibling::res) = '') or normalize-space(following-sibling::reschrs) = '') or not(normalize-space(following-sibling::restrs) = '') or not(normalize-space(following-sibling::revabbr) = '') or not(normalize-space(following-sibling::revname) = '') or not(normalize-space(following-sibling::secord) = '') or not(normalize-space(following-sibling::stat) = '')">
			<br/>
		</xsl:if>
	</xsl:template>
	<!--
		ocm
	-->
	<xsl:template match="ocm">
		<xsl:if test="not(normalize-space(.) = '') ">
			<span class="ocm">
				<xsl:value-of select="."/>
			</span>
		</xsl:if>
	</xsl:template>
	<!--
		pofix
	-->
	<xsl:template match="pofix">
		<xsl:if test="not(normalize-space(.) = '') ">
			<span class="postfix">
				<xsl:value-of select="."/>
			</span>
		</xsl:if>
	</xsl:template>
	<!--
		posn
	-->
	<xsl:template match="posn">
		<xsl:if test="not(normalize-space(.) = '') ">
			<xsl:for-each select="itemref">
				<span class="position">
					<xsl:value-of select="."/>
				</span>
			</xsl:for-each>
		</xsl:if>
	</xsl:template>
	<!--
		prfix
	-->
	<xsl:template match="prfix">
		<xsl:if test="not(normalize-space(.) = '') ">
			<span class="prefix">
				<xsl:value-of select="."/>
			</span>
		</xsl:if>
	</xsl:template>
	<!--
		ques
	-->
	<xsl:template match="ques">
		<xsl:if test="not(normalize-space(.) = '') ">
			<xsl:for-each select="str">
				<span class="sdQuestion">
					<xsl:value-of select="."/>
				</span>
			</xsl:for-each>
		</xsl:if>
	</xsl:template>
	<!--
		res
	-->
	<xsl:template match="res">
		<xsl:if test="not(normalize-space(.) = '') ">
			<xsl:for-each select="itemref">
				<span class="residence">
					<xsl:value-of select="."/>
				</span>
			</xsl:for-each>
		</xsl:if>
	</xsl:template>
	<!--
		reschrs
	-->
	<xsl:template match="reschrs">
		<xsl:if test="not(normalize-space(.) = '') ">
			<xsl:for-each select="itemref">
				<span class="researcher">
					<xsl:value-of select="."/>
				</span>
			</xsl:for-each>
		</xsl:if>
	</xsl:template>
	<!--
		restrs
	-->
	<xsl:template match="restrs">
		<xsl:if test="not(normalize-space(.) = '') ">
			<xsl:for-each select="itemref">
				<span class="restriction">
					<xsl:value-of select="."/>
				</span>
			</xsl:for-each>
		</xsl:if>
	</xsl:template>
	<!--
		revabbr
	-->
	<xsl:template match="revabbr">
		<xsl:if test="not(normalize-space(.) = '') ">
			<xsl:for-each select="str">
				<span class="reverseAbbr">
					<xsl:value-of select="."/>
				</span>
			</xsl:for-each>
		</xsl:if>
	</xsl:template>
	<!--
		revname
	-->
	<xsl:template match="revname">
		<xsl:if test="not(normalize-space(.) = '') ">
			<xsl:for-each select="str">
				<span class="reverseName">
					<xsl:value-of select="."/>
				</span>
			</xsl:for-each>
		</xsl:if>
	</xsl:template>
	<!--
		sdq
	-->
	<xsl:template match="sdq">
		<xsl:apply-templates/>
	</xsl:template>
	<!--
		sdqs
	-->
	<xsl:template match="sdqs">
		<xsl:apply-templates/>
	</xsl:template>
	<!--
		secord
	-->
	<xsl:template match="secord">
		<xsl:if test="not(normalize-space(.) = '') ">
			<span class="secorder">
				<xsl:value-of select="."/>
			</span>
		</xsl:if>
	</xsl:template>
	<!--
		stat
	-->
	<xsl:template match="stat">
		<span class="status">
			<xsl:value-of select="."/>
		</span>
	</xsl:template>
	<!--
		ignore the following
	-->
	<xsl:template match="abbr"/>
	<xsl:template match="alias"/>
	<xsl:template match="cid"/>
	<xsl:template match="descr"/>
	<xsl:template match="disc"/>
	<xsl:template match="guidi"/>
	<xsl:template match="guidl"/>
	<xsl:template match="name"/>
</xsl:stylesheet>
