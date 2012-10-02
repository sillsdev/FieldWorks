<?xml version="1.0" encoding="UTF-8"?>
<!--
// NAnt - A .NET build tool
// Copyright (C) 2002 Gordon Weakliem
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

// Gordon Weakliem (gweakliem@yahoo.com)
//
// Modified by Randy Regnier and Eberhard Beilharz for use in FieldWorks.
//
// Handles C++ project files.
-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
				xmlns:ms="http://schemas.microsoft.com/developer/msbuild/2003"
				xmlns="http://fieldworks.sil.org/nant/fwnant.xsd"
				xmlns:fw="http://fieldworks.sil.org/nant/fwnant.xsd"
				exclude-result-prefixes="ms" >
	<xsl:output method="xml" version="1.0" encoding="UTF-8" indent="yes"/>
	<xsl:param name="XmlInclude"/>
	<xsl:param name="LocalPath"/>
	<xsl:param name="TestAvailable" select="False"/>
	<xsl:param name="localsys-workaround" select="False"/>
	<!-- set to true to use Output paths specified in VS, otherwise output will go
	to ${dir.outputBase}\${config} -->
	<xsl:param name="UseVsPath"/>

	<!--
-->

	<xsl:variable name="projectType">C++</xsl:variable>
	<xsl:variable name="is-library">false</xsl:variable>
	<xsl:variable name="GlobalInclude" select="document($XmlInclude)"/>
	<xsl:variable name="filename">
		<xsl:value-of select="$LocalPath"/>\buildinclude.xml
	</xsl:variable>
	<xsl:variable name="LocalInclude" select="document($filename)"/>

	<!--
-->
	<xsl:template match="VisualStudioProject">
		<xsl:comment>DO NOT MODIFY! This file was generated from the Visual Studio project file by VSConvert-vcproj.xsl</xsl:comment>
		<project default="test">
			<xsl:call-template name="addProjectNameAttribute">
				<xsl:with-param name="value" select="@Name"/>
			</xsl:call-template>
			<xsl:call-template name="Cxx"/>
			<include buildfile="${{fwroot}}\Bld\VSConvert-shared.build.xml"/>
		</project>
	</xsl:template>
	<!--
-->
	<!-- Add the 'name' attribute to the main project element. -->
	<xsl:template name="addProjectNameAttribute">
		<xsl:param name="value"/>
		<xsl:param name="ext"/>
		<xsl:attribute name="name">
			<xsl:value-of select="$value"/>
			<xsl:value-of select="$ext"/>
		</xsl:attribute>
	</xsl:template>
	<!--
-->
	<!-- boilerplate prolog code -->
	<!-- todo: could move this so that context is Build/Settings -->
	<xsl:template name="prolog">
		<sysinfo failonerror="false"/>
		<if test="${{property::exists('dir.fwoutput')}}">
			<property name="dir.outputBase" value="${{dir.fwoutput}}"/>
		</if>
		<if test="${{not property::exists('dir.fwoutput')}}">
			<property name="dir.outputBase" value="${{fwroot}}\Output"/>
		</if>
		<property name="project.basedir" value="${{dir.srcProj}}" />
		<property name="dir.customConfig" value="${{config}}" />
		<regex pattern="(?'dummy'\\|/)(?'project_name'\w+)\.?\w*$" input="${{project::get-buildfile-path()}}"/>
		<echo message="project_name=${{project_name}};NAnt build file=${{project::get-buildfile-path()}}"/>
	</xsl:template>
	<!--
-->
	<!-- boilerplate epilog code -->
	<xsl:template name="epilog">
		<xsl:variable name="lib" select="Build/Settings/@OutputType='Library' or Build/Settings/@OutputType='Module'"/>
		<target name="buildtest" depends="build" description="build the tests">
			<xsl:if test="$TestAvailable = 'True'">
				<property name="dir.srcProjTmp" value="${{dir.srcProj}}"/>
				<property name="filename.destBuildTmp" value="${{filename.destBuild}}"/>
				<property name="filename.srcProjectTmp" value="${{filename.srcProject}}"/>
				<property name="targetTmp" value="${{target}}"/>

				<property name="dir.srcProj" value="${{project.basedir}}\${{project.FormalName}}Tests"/>
				<property name="filename.srcProject" value=""/>
				<property name="filename.destBuild" value=""/>
				<property name="target" value="buildtest"/>
				<call target="vsconvert-convert"/>

				<property name="dir.srcProj" value="${{dir.srcProjTmp}}"/>
				<property name="filename.destBuild" value="${{filename.destBuildTmp}}"/>
				<property name="filename.srcProject" value="${{filename.srcProjectTmp}}"/>
				<property name="target" value="${{targetTmp}}"/>
			</xsl:if>
		</target>
		<target name="test" depends="build" description="run the tests">
			<xsl:comment>Rename old asserts.log file</xsl:comment>
			<if test="${{file::exists(dir.buildOutput + '\asserts.log')}}">
				<delete file="${{dir.buildOutput}}\asserts_old.log" failonerror="false"/>
				<move file="${{dir.buildOutput}}\asserts.log"
					tofile="${{dir.buildOutput}}\asserts_old.log" overwrite="true"/>
			</if>
			<xsl:comment>run tests</xsl:comment>
			<call target="test-internal" failonerror="false" cascade="false"/>
			<xsl:comment>check if tests threw asserts</xsl:comment>
			<if test="${{file::exists(dir.buildOutput + '\asserts.log')}}">
				<loadfile file="${{dir.buildOutput}}\asserts.log" property="asserts-file"/>
				<!-- we match . so that we don't get a warning that regex didn't match anything -->
				<regex input="${{asserts-file}}" failonerror="false"
					pattern="(?'GotAsserts'(DEBUG ASSERTION FAILED|Assertion failed\!))|."/>
				<if test="${{string::get-length(GotAsserts) &gt; 0}}">
					<property name="fw-test-core-error" value="true"/>
				</if>
				<echo message="Contents of asserts.log file:" />
				<concatex file="${{dir.buildOutput}}\asserts.log" smartlines="true"/>
				<delete file="${{dir.buildOutput}}\asserts.log" failonerror="false"/>
			</if>
			<if test="${{fw-test-core-error}}">
				<xsl:comment>
					Because the tests failed we want to run them again next time, so
					rename results file
				</xsl:comment>
				<delete file="${{dir.buildOutput}}\${{project.output}}-failed-results.xml" failonerror="false"/>
				<move file="${{dir.buildOutput}}\${{project.output}}-results.xml"
					tofile="${{dir.buildOutput}}\${{project.output}}-failed-results.xml" failonerror="false"/>
				<if test="${{property::exists('fw-test-error')}}">
					<property name="fw-test-error"
						value="${{fw-test-error}};${{project.FormalName}}"/>
				</if>
				<if test="${{not property::exists('fw-test-error')}}">
					<property name="fw-test-error"
						value="${{project.FormalName}}"/>
				</if>
				<echo message="********* At least one test for ${{project.FormalName}} failed ********"/>
			</if>
		</target>
		<target name="test-internal" description="run the tests. Shouldn't be called directly">
			<xsl:comment>
				Set ${{fw-test-core-error}} to true and at the end to false. If tests fail,
				${{fw-test-core-error}} will remain true, so we know if anything happened
			</xsl:comment>
			<property name="fw-test-core-error" value="true"/>
			<xsl:choose>
				<xsl:when test="$TestAvailable = 'True'">
					<property name="dir.srcProjTmp" value="${{dir.srcProj}}"/>
					<property name="filename.destBuildTmp" value="${{filename.destBuild}}"/>
					<property name="filename.srcProjectTmp" value="${{filename.srcProject}}"/>
					<property name="targetTmp" value="${{target}}"/>

					<property name="dir.srcProj" value="${{project.basedir}}\${{project.FormalName}}Tests"/>
					<property name="filename.srcProject" value=""/>
					<property name="filename.destBuild" value=""/>
					<property name="target" value="test"/>
					<call target="vsconvert-convert"/>

					<property name="dir.srcProj" value="${{dir.srcProjTmp}}"/>
					<property name="filename.destBuild" value="${{filename.destBuildTmp}}"/>
					<property name="filename.srcProject" value="${{filename.srcProjectTmp}}"/>
					<property name="dir.srcProj" value="${{project.basedir}}\${{project.FormalName}}Tests"/>
					<property name="target" value="${{targetTmp}}"/>
				</xsl:when>
				<xsl:otherwise>
					<xsl:if test="Build/References/Reference[@Name='nunit.framework']">
						<property name="excludedCategories" value="LongRunning,ByHand,SmokeTest"/>
						<if test="${{property::exists('runAllTests')}}">
							<property name="excludedCategories" value="ByHand,SmokeTest"/>
						</if>
						<nunit2ex verbose="${{verbose}}" useX86="true" excludedCategories="${{excludedCategories}}"
								  if="${{forcetests or (not file::up-to-date(dir.buildOutput + '\' + project.output,
								  dir.buildOutput + '\' + project.output + '-results.xml'))}}">
							<test assemblyname="${{dir.buildOutput}}\${{project.output}}"/>
						</nunit2ex>
					</xsl:if>
				</xsl:otherwise>
			</xsl:choose>
			<property name="fw-test-core-error" value="false"/>
		</target>
		<xsl:variable name="IsCpp" select="/VisualStudioProject/@ProjectType = 'Visual C++'"/>
		<xsl:variable name="depends">
			<!-- Unregister is first thing we have to do, otherwise some files might already be gone -->
			<xsl:if test="$IsCpp and (count(/VisualStudioProject/Files/Filter/File[contains(@RelativePath, '.idl')]) > 0)">
				<xsl:text>unregister</xsl:text>
			</xsl:if>
		</xsl:variable>
		<xsl:call-template name="IncludePreTarget">
			<xsl:with-param name="target" select="'pre-clean'"/>
			<xsl:with-param name="GlobalNodes" select="$GlobalInclude/include/pre-clean"/>
			<xsl:with-param name="LocalNodes" select="$LocalInclude/include/pre-clean"/>
			<xsl:with-param name="depends" select="$depends"/>
		</xsl:call-template>
		<target name="clean" description="Delete output of a build">
			<xsl:attribute name="depends">
				<xsl:value-of select="'init'"/>
				<xsl:call-template name="IncludeDepends">
					<xsl:with-param name="target" select="'pre-clean'"/>
					<xsl:with-param name="GlobalNodes" select="$GlobalInclude/include/pre-clean"/>
					<xsl:with-param name="LocalNodes" select="$LocalInclude/include/pre-clean"/>
				</xsl:call-template>
				<xsl:if test="$IsCpp and (count(/VisualStudioProject/Files/Filter/File[contains(@RelativePath, '.idl')]) > 0)">
					<xsl:text>,unregister</xsl:text>
				</xsl:if>
			</xsl:attribute>
			<!-- delete files generated from C++ compiler and linker -->
			<xsl:comment>Delete files specific to C++ build process</xsl:comment>
			<delete verbose="true" failonerror="false">
				<fileset basedir="${{dir.obj}}\">
					<include name="${{pdb-file}}"/>
					<include name="${{dir.buildOutput}}\${{project.FormalName}}.ilk"/>
					<xsl:apply-templates select="Files/Filter[@Name='Resource Files']/File" mode="Cxx.res"/>
					<xsl:apply-templates select="Files/Filter[@Name='Source Files']/File" mode="Cxx.obj" />
					<include name="vc70.pdb"/>
					<include name="${{project.FormalName}}.pch"/>
					<xsl:if test="count(/VisualStudioProject/Files/Filter/File[contains(@RelativePath, '.idl')]) > 0">
						<include name="${{project.basedir}}\${{midl.iid}}"/>
						<include name="${{project.basedir}}\${{midl.proxy}}"/>
						<include name="${{project.basedir}}\${{midl.tlb}}"/>
						<include name="${{project.basedir}}\${{midl.header}}"/>
						<include name="${{project.basedir}}\${{midl.dlldata}}"/>
					</xsl:if>
				</fileset>
			</delete>
			<!-- TODO: delete files generated from tests -->
			<xsl:comment>Delete generic files</xsl:comment>
			<delete file="${{dir.buildOutput}}\${{project.output}}" verbose="true" failonerror="false"/>
			<delete file="${{dir.buildOutput}}\${{project.FormalName}}.pdb" verbose="true" failonerror="false"/>

			<xsl:call-template name="IncludePostTarget">
				<xsl:with-param name="GlobalNodes" select="$GlobalInclude/include/post-clean"/>
				<xsl:with-param name="LocalNodes" select="$LocalInclude/include/post-clean"/>
			</xsl:call-template>
			<xsl:comment>Finally delete build file itself</xsl:comment>
			<delete file="${{project::get-base-directory()}}\${{project.FormalName}}.build" failonerror="false" />
		</target>
		<target name="register" depends="init,build" description="Re-register the output">
			<xsl:if test="count(/VisualStudioProject/Files/Filter/File[contains(@RelativePath, '.idl')]) > 0">
				<comregisterex file="${{dir.buildOutput}}\${{project.output}}" unregister="false" verbose="${{verbose}}"/>
			</xsl:if>
		</target>
		<target name="unregister" depends="init" description="Unregister the output">
			<xsl:if test="count(/VisualStudioProject/Files/Filter/File[contains(@RelativePath, '.idl')]) > 0">
				<comregisterex file="${{dir.buildOutput}}\${{project.output}}" unregister="true" verbose="${{verbose}}"/>
			</xsl:if>
		</target>

		<include buildfile="${{VSConvertBuildFile}}"/>
	</xsl:template>
	<!--
-->
	<xsl:template match="Files">
		<sources>
			<xsl:attribute name="basedir">${dir.srcProj}\</xsl:attribute>
			<xsl:apply-templates select="Include/File[@BuildAction='Compile']"/>
		</sources>
		<xsl:if test="count(Include/File[@BuildAction='EmbeddedResource']) &gt; 0">
			<resources>
				<xsl:attribute name="basedir">${dir.srcProj}\</xsl:attribute>
				<xsl:attribute name="dynamicprefix">true</xsl:attribute>
				<xsl:attribute name="prefix">${rootNamespace}</xsl:attribute>
				<xsl:apply-templates select="Include/File[@BuildAction='EmbeddedResource']"/>
			</resources>
		</xsl:if>
	</xsl:template>
	<!--
-->
	<xsl:template match="File">
		<xsl:choose>
			<xsl:when test="count(@RelPath) > 0">
				<include name="{@RelPath}"/>
			</xsl:when>
			<xsl:otherwise>
				<include name="{@RelativePath}"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
-->
	<!-- Rules for determining if we have to compile, i.e. if anything changed -->
	<xsl:template match="Files" mode="dependencies">
		<xsl:apply-templates select="Include/File[@BuildAction='Compile']"/>
		<xsl:apply-templates select="Include/File[@BuildAction='EmbeddedResource']"/>
	</xsl:template>
	<xsl:template match="References" mode="dependencies">
		<xsl:apply-templates/>
	</xsl:template>
	<!--
-->
	<!-- rules to handle importing COM components via tlbimp -->
	<xsl:template match="References" mode="tlbimp">
		<xsl:if test="count(Reference[@WrapperTool='tlbimp'])>0">
			<xsl:apply-templates mode="tlbimp" select="Reference[@WrapperTool='tlbimp']"/>
		</xsl:if>
	</xsl:template>
	<!--
-->
	<xsl:template match="Reference" mode="tlbimp">
		<target name="{@Name}" depends="init">
			<xsl:element name="gettypelib">
				<xsl:attribute name="guid">
					<xsl:value-of select="@Guid"/>
				</xsl:attribute>
				<xsl:attribute name="propertyname">
					<xsl:value-of select="@Name"/>.path
				</xsl:attribute>
				<xsl:attribute name="versionmajor">
					<xsl:value-of select="@VersionMajor"/>
				</xsl:attribute>
				<xsl:attribute name="versionminor">
					<xsl:value-of select="@VersionMinor"/>
				</xsl:attribute>
				<xsl:attribute name="lcid">
					<xsl:value-of select="@Lcid"/>
				</xsl:attribute>
			</xsl:element>
			<xsl:element name="tlbimp">
				<xsl:variable name="Name" select="@Name"/>
				<xsl:attribute name="output">
					${dir.buildOutput}/<xsl:value-of select="$Name"/>Interop.dll
				</xsl:attribute>
				<xsl:attribute name="typelib">
					${<xsl:value-of select="$Name"/>.path}
				</xsl:attribute>
				<xsl:attribute name="keyfile">${fwroot}\src\FieldWorks.snk</xsl:attribute>
				<xsl:apply-templates select="$GlobalInclude/include/taskconfig/tlbimp/typelib[@name=$Name]/namespace" mode="include"/>
				<xsl:apply-templates select="$GlobalInclude/include/taskconfig/tlbimp/typelib[@name=$Name]/references" mode="include"/>
			</xsl:element>
		</target>
	</xsl:template>
	<xsl:template match="Reference" mode="tlbimpDelete">
		<xsl:element name="delete">
			<xsl:attribute name="file">
				${dir.buildOutput}/<xsl:value-of select="@Name"/>Interop.dll
			</xsl:attribute>
			<xsl:attribute name="verbose">true</xsl:attribute>
			<xsl:attribute name="failonerror">false</xsl:attribute>
		</xsl:element>
		<xsl:comment>Also delete the Interop dlls that Visual Studio generates automatically</xsl:comment>
		<xsl:element name="delete">
			<xsl:attribute name="file">
				${dir.buildOutput}/Interop.<xsl:value-of select="@Name"/>.dll
			</xsl:attribute>
			<xsl:attribute name="verbose">true</xsl:attribute>
			<xsl:attribute name="failonerror">false</xsl:attribute>
		</xsl:element>
	</xsl:template>
	<!--
-->
	<!-- Rules to handle importing ActiveX components via aximp. -->
	<!--
	<Reference
		Name = "AxSHDocVw"
		Guid = "{EAB22AC0-30C1-11CF-A7EB-0000C05BAE0B}"
		VersionMajor = "1"
		VersionMinor = "1"
		Lcid = "0"
		WrapperTool = "aximp"
		/>
-->
	<xsl:template match="References" mode="aximp">
		<xsl:if test="count(Reference[@WrapperTool='aximp'])>0">
			<xsl:apply-templates mode="aximp" select="Reference[@WrapperTool='aximp']"/>
		</xsl:if>
	</xsl:template>
	<!--
-->
	<xsl:template match="Reference" mode="aximp">
		<target name="{@Name}" depends="init">
			<xsl:element name="gettypelib">
				<xsl:attribute name="guid">
					<xsl:value-of select="@Guid"/>
				</xsl:attribute>
				<xsl:attribute name="propertyname">
					<xsl:value-of select="@Name"/>.path
				</xsl:attribute>
				<xsl:attribute name="versionmajor">
					<xsl:value-of select="@VersionMajor"/>
				</xsl:attribute>
				<xsl:attribute name="versionminor">
					<xsl:value-of select="@VersionMinor"/>
				</xsl:attribute>
				<xsl:attribute name="lcid">
					<xsl:value-of select="@Lcid"/>
				</xsl:attribute>
			</xsl:element>
			<xsl:element name="aximp">
				<xsl:variable name="Name" select="@Name"/>
				<xsl:attribute name="ocx">
					${<xsl:value-of select="@Name"/>.path}
				</xsl:attribute>
				<xsl:attribute name="out">
					${dir.buildOutput}/<xsl:value-of select="$Name"/>Interop.dll
				</xsl:attribute>
				<xsl:attribute name="keyfile">${fwroot}\src\FieldWorks.snk</xsl:attribute>
				<xsl:apply-templates select="$GlobalInclude/include/taskconfig/aximp/typelib[@name=$Name]/namespace" mode="include"/>
				<xsl:apply-templates select="$GlobalInclude/include/taskconfig/aximp/typelib[@name=$Name]/references" mode="include"/>
			</xsl:element>
		</target>
	</xsl:template>
	<xsl:template match="Reference" mode="aximpDelete">
		<xsl:element name="delete">
			<xsl:attribute name="file">
				${dir.buildOutput}/<xsl:value-of select="@Name"/>Interop.dll
			</xsl:attribute>
			<xsl:attribute name="verbose">true</xsl:attribute>
			<xsl:attribute name="failonerror">false</xsl:attribute>
		</xsl:element>
		<xsl:comment>Also delete the Interop dlls that Visual Studio generates automatically</xsl:comment>
		<xsl:element name="delete">
			<xsl:attribute name="file">
				${dir.buildOutput}/Interop.<xsl:value-of select="@Name"/>.dll
			</xsl:attribute>
			<xsl:attribute name="verbose">true</xsl:attribute>
			<xsl:attribute name="failonerror">false</xsl:attribute>
		</xsl:element>
	</xsl:template>
	<!--
-->
	<xsl:template match="namespace" mode="include">
		<xsl:attribute name="namespace">
			<xsl:value-of select="."/>
		</xsl:attribute>
	</xsl:template>
	<xsl:template match="references" mode="include">
		<xsl:copy-of select="."/>
	</xsl:template>
	<!--
-->
	<xsl:template match="References">
		<references>
			<xsl:attribute name="basedir">${dir.srcProj}\</xsl:attribute>
			<xsl:apply-templates/>
		</references>
	</xsl:template>
	<!--
-->
	<xsl:template match="Reference">
		<xsl:choose>
			<!-- straight assembly reference -->
			<xsl:when test="@AssemblyName">
				<xsl:if test="@HintPath">
					<include>
						<xsl:attribute name="name">
							<xsl:call-template name="replace-config">
								<xsl:with-param name="string" select="@HintPath"/>
							</xsl:call-template>
						</xsl:attribute>
					</include>
				</xsl:if>
			</xsl:when>
			<!-- project references -->
			<xsl:when test="@Project">
				<!-- we assume that the project being referenced has been built
				already and the compiled dll is sitting in the lib directory. -->
				<include name="${{dir.buildOutput}}/{@Name}.dll"/>
			</xsl:when>
			<!-- COM object reference -->
			<xsl:when test="@Guid">
				<!-- the tlbimp and aximp tasks will put the interop lib into the output directory -->
				<include name="${{dir.buildOutput}}/{@Name}Interop.dll"/>
			</xsl:when>
		</xsl:choose>
	</xsl:template>
	<!--
-->
	<!-- Templates to include parts of global and local include file -->
	<xsl:template name="IncludePreTarget">
		<xsl:param name="target"/>
		<xsl:param name="GlobalNodes"/>
		<xsl:param name="LocalNodes"/>
		<xsl:param name="depends"/>
		<xsl:if test="count($GlobalNodes) > 0 or count($LocalNodes) > 0">
			<target>
				<xsl:attribute name="name">
					<xsl:value-of select="$target"/>
				</xsl:attribute>
				<xsl:if test="string-length($depends) > 0">
					<xsl:attribute name="depends">
						<xsl:value-of select="$depends"/>
					</xsl:attribute>
				</xsl:if>
				<xsl:if test="count($GlobalNodes)>0">
					<xsl:comment>Rules from global include file</xsl:comment>
					<xsl:copy-of select="$GlobalNodes/*"/>
				</xsl:if>
				<xsl:if test="count($LocalNodes)>0">
					<xsl:comment>Rules from local include file</xsl:comment>
					<xsl:copy-of select="$LocalNodes/*"/>
				</xsl:if>
			</target>
		</xsl:if>
	</xsl:template>
	<xsl:template name="IncludePostTarget">
		<xsl:param name="GlobalNodes"/>
		<xsl:param name="LocalNodes"/>
		<xsl:if test="count($GlobalNodes) > 0 or count($LocalNodes) > 0">
			<xsl:if test="count($GlobalNodes)>0">
				<xsl:comment>Rules from global include file</xsl:comment>
				<xsl:copy-of select="$GlobalNodes/*"/>
			</xsl:if>
			<xsl:if test="count($LocalNodes)>0">
				<xsl:comment>Rules from local include file</xsl:comment>
				<xsl:copy-of select="$LocalNodes/*"/>
			</xsl:if>
		</xsl:if>
	</xsl:template>
	<xsl:template name="IncludeDepends">
		<xsl:param name="target"/>
		<xsl:param name="GlobalNodes"/>
		<xsl:param name="LocalNodes"/>
		<xsl:if test="count($GlobalNodes) > 0 or count($LocalNodes) > 0">
			<xsl:value-of select="','"/>
			<xsl:value-of select="$target"/>
		</xsl:if>
	</xsl:template>

	<!-- Replace configuration dependent parts with NAnt variable -->
	<xsl:template name="replace-config">
		<xsl:param name="string"/>
		<xsl:choose>
			<xsl:when test="contains($string, 'debug')">
				<xsl:call-template name="replace-config">
					<xsl:with-param name="string">
						<xsl:value-of select="substring-before($string, 'debug')"/>${dir.customConfig}<xsl:value-of select="substring-after($string, 'debug')"/>
					</xsl:with-param>
				</xsl:call-template>
			</xsl:when>
			<xsl:when test="contains($string, 'Debug')">
				<xsl:call-template name="replace-config">
					<xsl:with-param name="string">
						<xsl:value-of select="substring-before($string, 'Debug')"/>${dir.customConfig}<xsl:value-of select="substring-after($string, 'Debug')"/>
					</xsl:with-param>
				</xsl:call-template>
			</xsl:when>
			<xsl:when test="contains($string, 'release')">
				<xsl:call-template name="replace-config">
					<xsl:with-param name="string">
						<xsl:value-of select="substring-before($string, 'release')"/>${dir.customConfig}<xsl:value-of select="substring-after($string, 'release')"/>
					</xsl:with-param>
				</xsl:call-template>
			</xsl:when>
			<xsl:when test="contains($string, 'Release')">
				<xsl:call-template name="replace-config">
					<xsl:with-param name="string">
						<xsl:value-of select="substring-before($string, 'Release')"/>${dir.customConfig}<xsl:value-of select="substring-after($string, 'Release')"/>
					</xsl:with-param>
				</xsl:call-template>
			</xsl:when>
			<xsl:when test="contains($string, 'Microsoft.NET\Framework')">
				<xsl:call-template name="replace-config">
					<xsl:with-param name="string">
						<!-- pour solution, but the best that I can think of right now. If it is an assembly that is
							in the Framework directory we can't use the HintPath, because the assembly might reside in
							WINDOWS or WINNT or... -->
						<xsl:value-of select="'${framework::get-framework-directory(framework::get-target-framework())}\'"/>
						<xsl:value-of select="@AssemblyName"/>
						<xsl:value-of select="'.dll'"/>
					</xsl:with-param>
				</xsl:call-template>
			</xsl:when>
			<xsl:when test="contains($string, 'Program Files')">
				<xsl:call-template name="replace-config">
					<xsl:with-param name="string">
						<xsl:value-of select="'${sys.os.folder.programfiles}\'"/>
						<xsl:value-of select="substring-after($string, 'Program Files')"/>
					</xsl:with-param>
				</xsl:call-template>
			</xsl:when>
			<xsl:when test="contains($string, '$(ConfigurationName)')">
				<xsl:call-template name="replace-config">
					<xsl:with-param name="string">
						<xsl:value-of select="substring-before($string, '$(ConfigurationName)')"/>${dir.customConfig}<xsl:value-of select="substring-after($string, '$(ConfigurationName)')"/>
					</xsl:with-param>
				</xsl:call-template>
			</xsl:when>
			<xsl:when test="contains($string, '$(ProjectDir)')">
				<xsl:call-template name="replace-config">
					<xsl:with-param name="string">
						<xsl:value-of select="substring-before($string, '$(ProjectDir)')"/>${project.basedir}\<xsl:value-of select="substring-after($string, '$(ProjectDir)')"/>
					</xsl:with-param>
				</xsl:call-template>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="$string"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!-- replaces all occurences of $search with $replace in $string -->
	<xsl:template name="replace">
		<xsl:param name="string"/>
		<xsl:param name="search"/>
		<xsl:param name="replace"/>
		<xsl:choose>
			<xsl:when test="contains($string, $search)">
				<xsl:call-template name="replace">
					<xsl:with-param name="string" select="concat(substring-before($string, $search),
						$replace, substring-after($string, $search))"/>
					<xsl:with-param name="search" select="$search"/>
					<xsl:with-param name="replace" select="$replace"/>
				</xsl:call-template>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="$string"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!-- split the $string at blanks and add $add before each part if it starts with '..' -->
	<xsl:template name="split-string">
		<xsl:param name="string"/>
		<xsl:param name="add"/>
		<xsl:param name="pre"/>
		<xsl:param name="post" select="' '"/>
		<xsl:choose>
			<xsl:when test="contains($string, ' ')">
				<xsl:call-template name="split-string">
					<xsl:with-param name="string" select="substring-before($string, ' ')"/>
					<xsl:with-param name="add" select="$add"/>
					<xsl:with-param name="pre" select="$pre"/>
					<xsl:with-param name="post" select="$post"/>
				</xsl:call-template>
				<xsl:call-template name="split-string">
					<xsl:with-param name="string" select="substring-after($string, ' ')"/>
					<xsl:with-param name="add" select="$add"/>
					<xsl:with-param name="pre" select="$pre"/>
					<xsl:with-param name="post" select="$post"/>
				</xsl:call-template>
			</xsl:when>
			<xsl:when test="contains($string, ';')">
				<xsl:call-template name="split-string">
					<xsl:with-param name="string" select="substring-before($string, ';')"/>
					<xsl:with-param name="add" select="$add"/>
					<xsl:with-param name="pre" select="$pre"/>
					<xsl:with-param name="post" select="$post"/>
				</xsl:call-template>
				<xsl:call-template name="split-string">
					<xsl:with-param name="string" select="substring-after($string, ';')"/>
					<xsl:with-param name="add" select="$add"/>
					<xsl:with-param name="pre" select="$pre"/>
					<xsl:with-param name="post" select="$post"/>
				</xsl:call-template>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="$pre"/>
				<!-- now $string contains only one word -->
				<xsl:if test="starts-with($string, '..')">
					<xsl:value-of select="$add"/>
				</xsl:if>
				<xsl:value-of select="$string"/>
				<xsl:value-of select="$post"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template name="Cxx">
		<property name="project.FormalName" value="{@Name}"/>
		<xsl:call-template name="prolog"/>
		<target name="all" depends="test"/>
		<xsl:apply-templates select="Configurations"/>
		<xsl:call-template name="IncludePreTarget">
			<xsl:with-param name="target" select="'pre-build'"/>
			<xsl:with-param name="GlobalNodes" select="$GlobalInclude/include/pre-build"/>
			<xsl:with-param name="LocalNodes" select="$LocalInclude/include/pre-build"/>
		</xsl:call-template>
		<target name="build" description="Compile and link C++ project" >
			<xsl:attribute name="depends"><xsl:value-of select="'init'"/>
				<xsl:call-template name="IncludeDepends">
					<xsl:with-param name="target" select="'pre-build'"/>
					<xsl:with-param name="GlobalNodes" select="$GlobalInclude/include/pre-build"/>
					<xsl:with-param name="LocalNodes" select="$LocalInclude/include/pre-build"/>
				</xsl:call-template>
			</xsl:attribute>
			<xsl:for-each select="Files/File[Tool/@CommandLine]">
				<echo message="{FileConfiguration/Tool/@Description}"/>
				<exec program="{FileConfiguration/Tool/@CommandLine}"/>
			</xsl:for-each>
			<if test="${{showTargetsRunInReport}}">
				<property name="appTargName" value="${{project.FormalName}}"/>
				<call target="appendTargetName"/>
			</if>

			<uptodate property="Uptodate">
				<sourcefiles basedir="${{dir.srcProj}}\">
					<xsl:apply-templates select="Files/Filter/File"/>
					<xsl:apply-templates select="Files/File"/>
				</sourcefiles>
				<targetfiles>
					<include>
						<xsl:attribute name="name">
							<xsl:value-of select="'${dir.buildOutput}\${project.output}'"/>
						</xsl:attribute>
					</include>
				</targetfiles>
			</uptodate>
			<if test="${{not Uptodate}}">
				<versionex output="${{dir.outputBase}}\common\bldinc.h">
					<sources>
						<include name="${{fwroot}}\src\bldinc.h"/>
					</sources>
				</versionex>
				<xsl:apply-templates select="Files/Filter/File[contains(@RelativePath, '.idl')]" mode="Cxx.idl"/>
				<xsl:apply-templates select="Files/Filter/File[contains(@RelativePath, '.rc')]" mode="Cxx.rc" />
				<property name="options" value="${{cl.args}} ${{cl.includes}} ${{cl.optimize}} ${{cl.preprocessor}} ${{cl.codegen}} ${{cl.language}} ${{cl.general}} ${{cl.xtraopts}} ${{cl.overwrites}}"/>
				<!-- Create precompiled heaer -->
				<if test="${{UsePrecompiledHeader}}">
					<cl outputdir="${{dir.obj}}" verbose="${{verbose}}"
						options="${{options}}" pchfile="${{pchfile}}" pchthroughfile="${{pchthroughfile}}" pchmode="Create">
						<sources>
							<xsl:attribute name="basedir">${dir.srcProj}\</xsl:attribute>
							<xsl:apply-templates select="Files/Filter/File[contains(@RelativePath,'.cpp') or contains(@RelativePath,'.c') or contains(@RelativePath,'.cxx')]"
								mode="CxxPch"/>
							<xsl:apply-templates select="Files/File[contains(@RelativePath,'.cpp') or contains(@RelativePath,'.c') or contains(@RelativePath,'.cxx')]"
								mode="CxxPch"/>
						</sources>
					</cl>
				</if>
				<!-- Use precompiled header -->
				<cl outputdir="${{dir.obj}}" verbose="${{verbose}}" options="${{options}}" pchthroughfile="${{pchthroughfile}}" pchmode="Use"
					pchfile="${{pchfile}}">
					<sources>
						<xsl:attribute name="basedir">${dir.srcProj}\</xsl:attribute>
						<xsl:apply-templates select="Files/Filter/File[contains(@RelativePath,'.cpp') or contains(@RelativePath,'.c') or contains(@RelativePath,'.cxx')]"
							mode="Cxx"/>
						<xsl:apply-templates select="Files/File[contains(@RelativePath,'.cpp') or contains(@RelativePath,'.c') or contains(@RelativePath,'.cxx')]"
							mode="Cxx"/>
					</sources>
				</cl>
				<!-- Not using precompiled header -->
				<xsl:if test="count(Files/Filter/File/FileConfiguration/Tool[@UsePrecompiledHeader = 0]) > 0">
					<!-- Hack: we assume that all files that don't use precompiled headers have the same
					   settings. Right now, the only thing that can be overwritten, is the WarningLevel. This is
					   needed because the files that e.g. MIDL generates produce a lot of Level 4 warnings.
					-->
					<property name="cl.nopchoverwrites">
						<xsl:attribute name="value">
							<xsl:if test="count(Files/Filter/File/FileConfiguration/Tool[@UsePrecompiledHeader = 0]/@WarningLevel) > 0">
								<xsl:text> /W</xsl:text><xsl:value-of select="Files/Filter/File/FileConfiguration/Tool[@UsePrecompiledHeader = 0]/@WarningLevel"/>
							</xsl:if>
						</xsl:attribute>
					</property>
					<cl outputdir="${{dir.obj}}" verbose="${{verbose}}" options="${{options}} ${{cl.nopchoverwrites}}">
						<sources>
							<xsl:attribute name="basedir">${dir.srcProj}\</xsl:attribute>
							<xsl:apply-templates select="Files/Filter/File[contains(@RelativePath,'.cpp') or contains(@RelativePath,'.c') or contains(@RelativePath,'.cxx')]"
								mode="Cxx.nopch"/>
							<xsl:apply-templates select="Files/File[contains(@RelativePath,'.cpp') or contains(@RelativePath,'.c') or contains(@RelativePath,'.cxx')]"
								mode="Cxx.nopch"/>
						</sources>
					</cl>
				</xsl:if>
				<!-- Link -->
				<property name="link.options"
					value="${{link.opts}} ${{link.libs}} ${{link.ignorelibs}} ${{link.inputopts}} ${{link.general}} ${{link.system}} ${{link.optimize}} ${{link.xtraopts}}"/>
				<link output="${{dir.buildOutput}}\${{project.output}}" options="${{link.options}}" verbose="${{verbose}}">
					<sources>
						<xsl:attribute name="basedir">${dir.obj}\</xsl:attribute>
						<xsl:apply-templates select="Files/Filter/File[contains(@RelativePath,'.rc')]"
							mode="Cxx.res"/>
						<xsl:apply-templates select="Files/File[contains(@RelativePath,'.rc')]"
							mode="Cxx.res"/>
						<xsl:apply-templates select="Files/Filter/File[contains(@RelativePath,'.cpp') or contains(@RelativePath,'.c') or contains(@RelativePath,'.cxx')]"
							mode="Cxx.obj" />
						<xsl:apply-templates select="Files/File[contains(@RelativePath,'.cpp') or contains(@RelativePath,'.c') or contains(@RelativePath,'.cxx')]"
							mode="Cxx.obj"/>
					</sources>
					<libdirs>
						<!--<include name="${framework.lib}"/>-->
						<include name="${{fwroot}}\Lib\${{config}}"/>
						<include name="${{fwroot}}\Lib"/>
					</libdirs>
				</link>
				<!-- Embedd manifest file -->
				<if test="${{string::get-length(manifest.opts) &gt; 0}}">
					<exec program="mt.exe" commandline="${{manifest.opts}}" verbose="${{verbose}}"/>
				</if>
				<xsl:call-template name="IncludePostTarget">
					<xsl:with-param name="GlobalNodes" select="$GlobalInclude/include/post-build"/>
					<xsl:with-param name="LocalNodes" select="$LocalInclude/include/post-build"/>
				</xsl:call-template>
				<xsl:if test="count(/VisualStudioProject/Files/Filter/File[contains(@RelativePath, '.idl')]) > 0">
					<call target="register" cascade="false"/>
				</xsl:if>
			</if>
		</target>
		<xsl:call-template name="epilog"/>
	</xsl:template>
	<!--
-->
	<xsl:template match="Configurations">
		<xsl:call-template name="IncludePreTarget">
			<xsl:with-param name="target" select="'pre-init'"/>
			<xsl:with-param name="GlobalNodes" select="$GlobalInclude/include/pre-init"/>
			<xsl:with-param name="LocalNodes" select="$LocalInclude/include/pre-init"/>
		</xsl:call-template>
		<target name="init" description="Initialize properties for the build">
			<xsl:attribute name="depends">init-${config}<xsl:call-template name="IncludeDepends">
					<xsl:with-param name="target" select="'pre-init'"/>
					<xsl:with-param name="GlobalNodes" select="$GlobalInclude/include/pre-init"/>
					<xsl:with-param name="LocalNodes" select="$LocalInclude/include/pre-init"/>
				</xsl:call-template></xsl:attribute>
			<tstamp />
			<sysinfo failonerror="false"/>
			<mkdir dir="${{dir.outputBase}}" />
			<mkdir dir="${{dir.buildOutput}}" />
			<mkdir dir="${{dir.obj}}" />
			<xsl:call-template name="IncludePostTarget">
				<xsl:with-param name="GlobalNodes" select="$GlobalInclude/include/post-init"/>
				<xsl:with-param name="LocalNodes" select="$LocalInclude/include/post-init"/>
			</xsl:call-template>
		</target>
		<xsl:apply-templates select="Configuration"/>
	</xsl:template>
	<xsl:template match="Configuration">
		<xsl:variable name="debug" select="Tool[@Name='VCLinkerTool']/@GenerateDebugInformation = 'true'"/>
		<xsl:variable name="this-config" select="substring-before(@Name, '|')"/>
		<target>
			<xsl:attribute name="name">init-<xsl:value-of select="$this-config"/></xsl:attribute>
			<!-- general settings -->
			<xsl:comment>General settings</xsl:comment>
			<property name="dir.buildOutput">
				<xsl:choose>
					<xsl:when test="$UseVsPath = 'True'">
						<xsl:attribute name="value">${dir.srcProj}\<xsl:value-of select="@OutputDirectory"/></xsl:attribute>
					</xsl:when>
					<xsl:otherwise>
						<xsl:attribute name="value">${dir.outputBase}\<xsl:value-of select="substring-before(@Name, '|')"/></xsl:attribute>
					</xsl:otherwise>
				</xsl:choose>
			</property>
			<property name="dir.obj">
				<xsl:choose>
					<xsl:when test="$UseVsPath = 'True'">
						<xsl:attribute name="value">${dir.srcProj}\<xsl:value-of select="@IntermediateDirectory"/></xsl:attribute>
					</xsl:when>
					<xsl:otherwise>
						<xsl:attribute name="value">${dir.fwobj}\<xsl:value-of select="substring-before(@Name, '|')"/>\${project.FormalName}</xsl:attribute>
					</xsl:otherwise>
				</xsl:choose>
			</property>
			<property name="project.output">
				<xsl:attribute name="value">
					<xsl:choose>
						<xsl:when test="count(Tool[@Name='VCLinkerTool']/@OutputFile) > 0">
							<xsl:value-of select="substring-after(Tool[@Name='VCLinkerTool']/@OutputFile,'$(OutDir)/')"/>
						</xsl:when>
						<xsl:otherwise>
							<xsl:text>${project.FormalName}.dll</xsl:text>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:attribute>
			</property>
			<property name="pdb-file">
				<xsl:attribute name="value">
					<xsl:if test="$debug">
						<xsl:choose>
							<xsl:when test="count(Tool[@Name='VCLinkerTool']/@ProgramDatabaseFile) > 0">
								<xsl:text>${dir.buildOutput}\</xsl:text><xsl:value-of select="substring-after(Tool[@Name='VCLinkerTool']/@ProgramDatabaseFile,'$(OutDir)/')"/>
							</xsl:when>
							<xsl:otherwise>
								<xsl:text>${dir.buildOutput}\${project.FormalName}.pdb</xsl:text>
							</xsl:otherwise>
						</xsl:choose>
					</xsl:if>
				</xsl:attribute>
			</property>
			<xsl:call-template name="config-general-settings"/>
			<!-- Compiler settings -->
			<xsl:comment>Compiler settings</xsl:comment>
			<property name="cl.warn" value="{Tool[@Name='VCCLCompilerTool']/@WarningLevel}"/>
			<property name="cl.inc" value="{Tool[@Name='VCCLCompilerTool']/@MinimalRebuild}"/>
			<xsl:call-template name="cl-general-settings"/>
			<xsl:call-template name="cl-preprocessor-settings"/>
			<xsl:call-template name="cl-optimize-settings"/>
			<xsl:call-template name="cl-codegeneration-settings"/>
			<xsl:call-template name="cl-language-settings"/>
			<xsl:call-template name="PCH-Settings"/>
			<!-- overwrite of VCProj settings -->
			<property name="cl.overwrites">
				<xsl:attribute name="value">
					<xsl:if test="@ManagedExtensions = '4'"> /clr</xsl:if>
					<xsl:text> /W4 /WX</xsl:text>
				</xsl:attribute>
			</property>
			<!-- Linker settings -->
			<xsl:comment>Linker settings</xsl:comment>
			<xsl:call-template name="linker-general-settings"/>
			<xsl:call-template name="linker-input-settings"/>
			<xsl:call-template name="linker-system-settings"/>
			<xsl:call-template name="linker-optimization-settings"/>
			<xsl:if test="count(/VisualStudioProject/Files/Filter/File[contains(@RelativePath, '.idl')]) > 0">
				<!-- MIDL settings -->
				<xsl:comment>MIDL settings</xsl:comment>
				<xsl:call-template name="midl-general-settings"/>
				<xsl:call-template name="midl-output-settings"/>
				<xsl:call-template name="midl-advanced-settings"/>
			</xsl:if>
		</target>
	</xsl:template>
	<!--
-->
	<xsl:template match="File" mode="PchFileName">
		<xsl:param name="pch-filename"/>
		<xsl:choose>
			<xsl:when test="string-length($pch-filename) > 0">
				<xsl:value-of select="$pch-filename"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:choose>
					<xsl:when test="string-length(substring-before(@RelativePath, '.')) > 0">
						<xsl:value-of select="substring-before(@RelativePath, '.')"/><xsl:value-of select="'.h'"/>
					</xsl:when>
					<xsl:otherwise>
						<xsl:value-of select="'stdafx.h'"/>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<xsl:template match="File" mode="Cxx">
		<xsl:choose>
			<xsl:when test="FileConfiguration[@ExcludedFromBuild = 'true']">
				<!-- ignore that -->
			</xsl:when>
			<xsl:when test="FileConfiguration/Tool[@UsePrecompiledHeader = 1]">
				<!-- ignore that -->
			</xsl:when>
			<xsl:when test="FileConfiguration/Tool[@UsePrecompiledHeader = 0]">
				<!-- ignore that -->
			</xsl:when>
			<!--
			<xsl:when test="count(FileConfiguration) = 0">
				<include name="{@RelativePath}"/>
			</xsl:when>
			-->
			<xsl:otherwise>
				<include name="{@RelativePath}"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<xsl:template match="File" mode="Cxx.nopch"> <!-- Files not using PCH -->
		<xsl:choose>
			<xsl:when test="FileConfiguration/Tool[@UsePrecompiledHeader = 0]">
				<include name="{@RelativePath}"/>
			</xsl:when>
			<xsl:otherwise>
				<!-- ignore that -->
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<xsl:template match="File" mode="CxxPch"> <!-- generate PCH -->
		<xsl:if test="FileConfiguration/Tool[@UsePrecompiledHeader = 1]">
			<include name="{@RelativePath}"/>
		</xsl:if>
	</xsl:template>
	<xsl:template match="File" mode="Cxx.res">
		<xsl:if test="contains(substring(@RelativePath, string-length(@RelativePath)-3), '.rc')">
			<xsl:variable name="filename">
				<xsl:call-template name="get-filename">
					<xsl:with-param name="path" select="@RelativePath"/>
				</xsl:call-template>
			</xsl:variable>
			<xsl:element name="includes">
				<xsl:attribute name="name">
					<xsl:value-of select="substring-before($filename,'.rc')"/>
					<xsl:text>.res</xsl:text>
				</xsl:attribute>
			</xsl:element>
		</xsl:if>
	</xsl:template>
	<xsl:template match="File" mode="Cxx.obj">
		<xsl:variable name="filename">
			<xsl:call-template name="get-filename">
				<xsl:with-param name="path" select="@RelativePath"/>
			</xsl:call-template>
		</xsl:variable>
		<include name="{substring-before($filename,'.')}.obj" />
	</xsl:template>
	<xsl:template match="File" mode="Cxx.lib">
		<xsl:text>${dir.srcProj}\</xsl:text><xsl:value-of select="@RelativePath"/><xsl:text> </xsl:text>
	</xsl:template>
	<xsl:template match="File" mode="Cxx.rc">
		<xsl:variable name="relpath">
			<xsl:call-template name="get-directory">
				<xsl:with-param name="path" select="@RelativePath"/>
			</xsl:call-template>
		</xsl:variable>
		<xsl:variable name="filename">
			<xsl:call-template name="get-filename">
				<xsl:with-param name="path" select="@RelativePath"/>
			</xsl:call-template>
		</xsl:variable>
		<xsl:variable name="resname" select="substring-before($filename,'.rc')"/>

		<rc rcfile="${{project.basedir}}/{$relpath}{$resname}.rc" output="${{dir.obj}}/{$resname}.res"
			 verbose="${{verbose}}"/>
	</xsl:template>
	<xsl:template match="File" mode="Cxx.idl">
		<midlex filename="${{project.basedir}}\{@RelativePath}" env="${{midl.env}}" char="${{midl.char}}"
			dlldata="${{midl.dlldata}}" tlb="${{midl.tlb}}" header="${{midl.header}}"
			iid="${{midl.iid}}" proxy="${{midl.proxy}}" verbose="${{verbose}}" outputdir="${{project.basedir}}"
			rawoptions="${{midl.general}} ${{midl.output}} ${{midl.advanced}}"/>
	</xsl:template>
	<!-- Handles settings for precompiled headers -->
	<xsl:template name="PCH-Settings">
		<xsl:if test="Tool[@Name='VCCLCompilerTool']/@UsePrecompiledHeader = 2">
			<property name="pchfile">
				<xsl:attribute name="value">${dir.obj}\${project.FormalName}.pch</xsl:attribute>
			</property>
			<property name="pchthroughfile">
				<xsl:attribute name="value">
					<xsl:apply-templates
						select="../../Files/Filter/File[FileConfiguration/Tool/@UsePrecompiledHeader = 1]"
						mode="PchFileName">
						<xsl:with-param name="pch-filename" select="Tool[@Name='VCCLCompilerTool']/@PrecompiledHeaderThrough"/>
					</xsl:apply-templates>
				</xsl:attribute>
			</property>
		</xsl:if>
		<property name="UsePrecompiledHeader" value="{Tool[@Name='VCCLCompilerTool']/@UsePrecompiledHeader = 2}"/>
	</xsl:template>
	<!-- Handle general configuration settings -->
	<xsl:template name="config-general-settings">
		<property name="cl.args">
			<xsl:attribute name="value">
				<xsl:choose>
					<xsl:when test="@CharacterSet = 1"> /D_UNICODE /DUNICODE</xsl:when>
					<xsl:when test="@CharacterSet = 2"> /D_MBCS</xsl:when>
				</xsl:choose>
				<xsl:if test="@ConfigurationType = '2'"> /D_WINDLL</xsl:if>
				<xsl:if test="@ManagedExtensions = '4'"> /clr</xsl:if>
				<xsl:if test="@ManagedExtensions = '1'"> /clr</xsl:if>
			</xsl:attribute>
		</property>
		<property name="link.opts">
			<xsl:attribute name="value">
				<xsl:text> /FIXED:NO</xsl:text>
				<xsl:choose>
					<xsl:when test="@ConfigurationType = '2'">	<!-- Output is DLL -->
						<!--<xsl:text> /DLL /implib:${dir.obj}\${project.FormalName}.lib </xsl:text>-->						<xsl:text> /DLL </xsl:text>
					</xsl:when>
				</xsl:choose>
				<xsl:if test="Tool[@Name='VCLinkerTool']/@GenerateDebugInformation = 'true'">
					<xsl:text> /DEBUG /PDB:${pdb-file}</xsl:text>
				</xsl:if>
				<xsl:choose>
					<xsl:when test="Tool[@Name='VCLinkerTool']/@AssemblyDebug = '0'"/>
					<xsl:when test="Tool[@Name='VCLinkerTool']/@AssemblyDebug = '1'">
						<xsl:text> /ASSEMBLYDEBUG</xsl:text>
					</xsl:when>
					<xsl:when test="Tool[@Name='VCLinkerTool']/@AssemblyDebug = '2'">
						<xsl:text> /ASSEMBLYDEBUG:DISABLE</xsl:text>
					</xsl:when>
				</xsl:choose>
				<xsl:choose>
					<xsl:when test="Tool[@Name='VCLinkerTool']/@GenerateManifest = 'false'">
						<xsl:text> /MANIFEST:NO</xsl:text>
					</xsl:when>
					<xsl:otherwise>
						<xsl:text> /MANIFEST  /MANIFESTFILE:"${dir.obj}\${project.output}.intermediate.manifest"</xsl:text>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:attribute>
		</property>
		<property name="manifest.opts">
			<xsl:attribute name="value">
				<xsl:choose>
					<xsl:when test="Tool[@Name='VCLinkerTool']/@GenerateManifest = 'false'"/>
					<xsl:otherwise>
						<xsl:text>/nologo /outputresource:"${dir.buildOutput}\${project.output};#2" -manifest "${dir.obj}\${project.output}.intermediate.manifest"</xsl:text>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:attribute>
		</property>
	</xsl:template>
	<!-- Handle general settings -->
	<xsl:template name="cl-general-settings">
		<!-- TODO: set fileset here and reference it in build target. We need NAnt 0.8.3 or higher
			to be able to do that -->
		<property name="cl.includes">
			<xsl:attribute name="value">
				<xsl:if test="count(Tool[@Name='VCCLCompilerTool']/@AdditionalIncludeDirectories)>0">
					<xsl:call-template name="replace-config">
						<xsl:with-param name="string">
							<xsl:call-template name="split-string">
								<xsl:with-param name="string" select="Tool[@Name='VCCLCompilerTool']/@AdditionalIncludeDirectories"/>
								<xsl:with-param name="add" select="'${dir.srcProj}\'"/>
								<xsl:with-param name="pre" select="'/I '"/>
							</xsl:call-template>
						</xsl:with-param>
					</xsl:call-template>
				</xsl:if>
				<xsl:text>/I${fwroot}\Lib\${config} /I${fwroot}\Include </xsl:text>
			</xsl:attribute>
		</property>
		<property name="cl.general">
			<xsl:attribute name="value">
				<xsl:if test="count(Tool[@Name='VCCLCompilerTool']/@AdditionalUsingDirectories) > 0">
					<xsl:call-template name="replace-config">
						<xsl:with-param name="string">
							<xsl:call-template name="split-string">
								<xsl:with-param name="string" select="Tool[@Name='VCCLCompilerTool']/@AdditionalUsingDirectories"/>
								<xsl:with-param name="add" select="'${dir.srcProj}\'"/>
								<xsl:with-param name="pre" select="'/AI '"/>
							</xsl:call-template>
						</xsl:with-param>
					</xsl:call-template>
				</xsl:if>
				<xsl:choose>
					<xsl:when test="Tool[@Name='VCCLCompilerTool']/@DebugInformationFormat = 1"> /Z7</xsl:when>
					<xsl:when test="Tool[@Name='VCCLCompilerTool']/@DebugInformationFormat = 2"> /Zd</xsl:when>
					<xsl:when test="Tool[@Name='VCCLCompilerTool']/@DebugInformationFormat = 3"> /Zi</xsl:when>
					<xsl:when test="Tool[@Name='VCCLCompilerTool']/@DebugInformationFormat = 4"> /ZI</xsl:when>
				</xsl:choose>
				<xsl:if test="Tool[@Name='VCCLCompilerTool']/@SuppressStartupBanner = 'true'"> /nologo</xsl:if>
				<xsl:text> /W</xsl:text><xsl:value-of select="Tool[@Name='VCCLCompilerTool']/@WarningLevel"/>
				<xsl:if test="Tool[@Name='VCCLCompilerTool']/@WarnAsError = 'true'"> /WX</xsl:if>
				<xsl:if test="Tool[@Name='VCCLCompilerTool']/@Detect64BitPortabilityProblems = 'true'"> /Wp64</xsl:if>
			</xsl:attribute>
		</property>
	</xsl:template>
	<!-- Handle preprocessor settings -->
	<xsl:template name="cl-preprocessor-settings">
		<property name="cl.preprocessor">
			<xsl:attribute name="value">
				<xsl:call-template name="split-string">
					<xsl:with-param name="string" select="Tool[@Name='VCCLCompilerTool']/@PreprocessorDefinitions"/>
					<xsl:with-param name="pre" select="'/D'"/>
				</xsl:call-template>
				<xsl:if test="Tool[@Name='VCCLCompilerTool']/@IgnoreStandardIncludePath = 'true'"> /X</xsl:if>
				<xsl:choose>
					<xsl:when test="Tool[@Name='VCCLCompilerTool']/@GeneratePreprocessedFile = 1"> /P</xsl:when>
					<xsl:when test="Tool[@Name='VCCLCompilerTool']/@GeneratePreprocessedFile = 2"> /EP /P</xsl:when>
				</xsl:choose>
				<xsl:if test="Tool[@Name='VCCLCompilerTool']/@KeepComments = 'true'"> /C</xsl:if>
			</xsl:attribute>
		</property>
	</xsl:template>
	<!-- Handle settings for optimization -->
	<xsl:template name="cl-optimize-settings">
		<property name="cl.optimize">
			<xsl:attribute name="value">
				<xsl:choose>
					<xsl:when test="Tool[@Name='VCCLCompilerTool']/@Optimization = 0">/Od</xsl:when>
					<xsl:when test="Tool[@Name='VCCLCompilerTool']/@Optimization = 1">/O1</xsl:when>
					<xsl:when test="Tool[@Name='VCCLCompilerTool']/@Optimization = 2">/O2</xsl:when>
					<xsl:when test="Tool[@Name='VCCLCompilerTool']/@Optimization = 3">/Ox</xsl:when>
				</xsl:choose>
				<xsl:if test="Tool[@Name='VCCLCompilerTool']/@GlobalOptimizations = 'true'"> /Og</xsl:if>
				<xsl:choose>
					<xsl:when test="Tool[@Name='VCCLCompilerTool']/@InlineFunctionExpansion = 1"> /Ob1</xsl:when>
					<xsl:when test="Tool[@Name='VCCLCompilerTool']/@InlineFunctionExpansion = 2"> /Ob2</xsl:when>
				</xsl:choose>
				<xsl:if test="Tool[@Name='VCCLCompilerTool']/@EnableIntrinsicFunctions = 'true'"> /Oi</xsl:if>
				<xsl:if test="Tool[@Name='VCCLCompilerTool']/@ImproveFloatingPointConsistency = 'true'"> /Op</xsl:if>
				<xsl:choose>
					<xsl:when test="Tool[@Name='VCCLCompilerTool']/@FavorSizeOrSpeed = 1"> /Ot</xsl:when>
					<xsl:when test="Tool[@Name='VCCLCompilerTool']/@FavorSizeOrSpeed = 2"> /Os</xsl:when>
				</xsl:choose>
				<xsl:if test="Tool[@Name='VCCLCompilerTool']/@OmitFramePointers = 'true'"> /Oy</xsl:if>
				<xsl:if test="Tool[@Name='VCCLCompilerTool']/@EnableFiberSafeOptimizations = 'true'"> /GT</xsl:if>
				<xsl:choose>
					<xsl:when test="Tool[@Name='VCCLCompilerTool']/@OptimizeForProcessor = 1"> /G5</xsl:when>
					<xsl:when test="Tool[@Name='VCCLCompilerTool']/@OptimizeForProcessor = 2"> /G6</xsl:when>
				</xsl:choose>
				<xsl:if test="Tool[@Name='VCCLCompilerTool']/@OptimizeForWindowsApplication = 'true'"> /GA</xsl:if>
			</xsl:attribute>
		</property>
	</xsl:template>
	<!-- Handle code generation settings -->
	<xsl:template name="cl-codegeneration-settings">
		<property name="cl.codegen">
			<xsl:attribute name="value">
				<xsl:if test="Tool[@Name='VCCLCompilerTool']/@StringPooling = 'true'"> /GF</xsl:if>
				<xsl:if test="Tool[@Name='VCCLCompilerTool']/@MinimalRebuild = 'true'"> /Gm</xsl:if>
				<xsl:choose>
					<xsl:when test="Tool[@Name='VCCLCompilerTool']/@ExceptionHandling = 1"> /EHsc</xsl:when>
					<xsl:when test="Tool[@Name='VCCLCompilerTool']/@ExceptionHandling = 2"> /EHa</xsl:when>
				</xsl:choose>
				<xsl:if test="Tool[@Name='VCCLCompilerTool']/@SmallerTypeCheck = 'true'"> /RTCc</xsl:if>
				<xsl:choose>
					<xsl:when test="Tool[@Name='VCCLCompilerTool']/@BasicRuntimeChecks = 1"> /RTCs</xsl:when>
					<xsl:when test="Tool[@Name='VCCLCompilerTool']/@BasicRuntimeChecks = 2"> /RTCu</xsl:when>
					<xsl:when test="Tool[@Name='VCCLCompilerTool']/@BasicRuntimeChecks = 3"> /RTC1</xsl:when>
				</xsl:choose>
				<xsl:choose>
					<xsl:when test="Tool[@Name='VCCLCompilerTool']/@RuntimeLibrary = 0"> /MT</xsl:when>
					<xsl:when test="Tool[@Name='VCCLCompilerTool']/@RuntimeLibrary = 1"> /MTd</xsl:when>
					<xsl:when test="Tool[@Name='VCCLCompilerTool']/@RuntimeLibrary = 2"> /MD</xsl:when>
					<xsl:when test="Tool[@Name='VCCLCompilerTool']/@RuntimeLibrary = 3"> /MDd</xsl:when>
					<xsl:when test="Tool[@Name='VCCLCompilerTool']/@RuntimeLibrary = 4"> /ML</xsl:when>
					<xsl:when test="Tool[@Name='VCCLCompilerTool']/@RuntimeLibrary = 5"> /MLd</xsl:when>
				</xsl:choose>
				<xsl:choose>
					<xsl:when test="Tool[@Name='VCCLCompilerTool']/@StructMemberAlignment = 1"> /Zp1</xsl:when>
					<xsl:when test="Tool[@Name='VCCLCompilerTool']/@StructMemberAlignment = 2"> /Zp2</xsl:when>
					<xsl:when test="Tool[@Name='VCCLCompilerTool']/@StructMemberAlignment = 3"> /Zp4</xsl:when>
					<xsl:when test="Tool[@Name='VCCLCompilerTool']/@StructMemberAlignment = 4"> /Zp8</xsl:when>
					<xsl:when test="Tool[@Name='VCCLCompilerTool']/@StructMemberAlignment = 5"> /Zp16</xsl:when>
				</xsl:choose>
				<xsl:if test="Tool[@Name='VCCLCompilerTool']/@BufferSecurityCheck = 'true'"> /GS</xsl:if>
				<xsl:if test="Tool[@Name='VCCLCompilerTool']/@EnableFunctionLevelLinking = 'true'"> /Gy</xsl:if>
			</xsl:attribute>
		</property>
	</xsl:template>
	<!-- Handle language settings -->
	<xsl:template name="cl-language-settings">
		<property name="cl.language">
			<xsl:attribute name="value">
				<xsl:if test="Tool[@Name='VCCLCompilerTool']/@DisableLanguageExtensions = 'true'"> /Za</xsl:if>
				<xsl:if test="Tool[@Name='VCCLCompilerTool']/@DefaultCharIsUnsigned = 'true'"> /J</xsl:if>
				<xsl:if test="Tool[@Name='VCCLCompilerTool']/@TreatWChar_tAsBuiltInType = 'true'"> /Zc:wchar_t</xsl:if>
				<xsl:if test="Tool[@Name='VCCLCompilerTool']/@ForceConformanceInForLoopScope = 'true'"> /Zc:forScope</xsl:if>
				<xsl:if test="Tool[@Name='VCCLCompilerTool']/@RuntimeTypeInfo = 'true'"> /GR</xsl:if>
				<xsl:if test="Tool[@Name='VCCLCompilerTool']/@RuntimeTypeInfo = 'false'"> /GR-</xsl:if>
			</xsl:attribute>
		</property>
	</xsl:template>
	<!-- Handle general linker settings -->
	<xsl:template name="linker-general-settings">
		<property name="link.libdir">
			<xsl:attribute name="value">
				<xsl:call-template name="replace-config">
					<xsl:with-param name="string">
						<xsl:call-template name="split-string">
							<xsl:with-param name="string">
								<xsl:call-template name="replace">
									<xsl:with-param name="string" select="Tool[@Name='VCLinkerTool']/@AdditionalLibraryDirectories"/>
									<xsl:with-param name="search" select="'&quot;'"/>
								</xsl:call-template>
							</xsl:with-param>
							<xsl:with-param name="add" select="'${dir.srcProj}\'"/>
						</xsl:call-template>
					</xsl:with-param>
				</xsl:call-template>
			</xsl:attribute>
		</property>
		<property name="link.general">
			<xsl:attribute name="value">
				<xsl:choose>
					<xsl:when test="Tool[@Name='VCLinkerTool']/@LinkIncremental = 1"> /INCREMENTAL:NO</xsl:when>
					<xsl:when test="Tool[@Name='VCLinkerTool']/@LinkIncremental = 2"> /INCREMENTAL</xsl:when>
				</xsl:choose>
			</xsl:attribute>
		</property>
	</xsl:template>
	<!-- Handle linker input settings -->
	<xsl:template name="linker-input-settings">
		<property name="link.ignorelibs">
			<xsl:attribute name="value">
				<xsl:if test="count(Tool[@Name='VCLinkerTool']/@IgnoreDefaultLibraryNames) > 0">
					<xsl:call-template name="replace-config">
						<xsl:with-param name="string">
							<xsl:call-template name="split-string">
								<xsl:with-param name="string">
									<xsl:call-template name="replace">
										<xsl:with-param name="string" select="Tool[@Name='VCLinkerTool']/@IgnoreDefaultLibraryNames"/>
										<xsl:with-param name="search" select="'&quot;'"/>
									</xsl:call-template>
								</xsl:with-param>
								<xsl:with-param name="pre" select="'/NODEFAULTLIB:'"/>
							</xsl:call-template>
						</xsl:with-param>
					</xsl:call-template>
				</xsl:if>
				<xsl:if test="Tool[@Name='VCLinkerTool']/@IgnoreAllDefaultLibraries = 'true'">
					<xsl:text> /NODEFAULTLIB</xsl:text>
				</xsl:if>
			</xsl:attribute>
		</property>
		<property name="link.libs">
			<xsl:attribute name="value">
				<xsl:if test="count(Tool[@Name='VCLinkerTool']/@AdditionalDependencies) > 0">
					<xsl:call-template name="split-string">
						<xsl:with-param name="string" select="Tool[@Name='VCLinkerTool']/@AdditionalDependencies"/>
						<xsl:with-param name="add" select="'${dir.srcProj}\'"/>
					</xsl:call-template>
				</xsl:if>
				<xsl:text>kernel32.lib user32.lib advapi32.lib gdi32.lib winspool.lib comdlg32.lib shell32.lib ole32.lib oleaut32.lib uuid.lib odbc32.lib odbccp32.lib </xsl:text>
				<xsl:apply-templates select="/VisualStudioProject/Files/Filter/File[contains(@RelativePath,'.lib')]" mode="Cxx.lib"/>
				<xsl:apply-templates select="/VisualStudioProject/Files/File[contains(@RelativePath,'.lib')]" mode="Cxx.lib"/>
			</xsl:attribute>
		</property>
		<property name="link.inputopts">
			<xsl:attribute name="value">
				<xsl:if test="string-length(Tool[@Name='VCLinkerTool']/@ModuleDefinitionFile) > 0">
					<xsl:text>/DEF:${dir.srcProj}\</xsl:text><xsl:value-of select="Tool[@Name='VCLinkerTool']/@ModuleDefinitionFile"/>
					<xsl:text> </xsl:text>
				</xsl:if>
				<xsl:if test="count(Tool[@Name='VCLinkerTool']/@AddModuleNamesToAssembly) > 0">
					<xsl:call-template name="replace-config">
						<xsl:with-param name="string">
							<xsl:call-template name="split-string">
								<xsl:with-param name="string">
									<xsl:call-template name="replace">
										<xsl:with-param name="string" select="Tool[@Name='VCLinkerTool']/@AddModuleNamesToAssembly"/>
										<xsl:with-param name="search" select="'&quot;'"/>
									</xsl:call-template>
								</xsl:with-param>
								<xsl:with-param name="pre" select="'/ASSEMBLYMODULE:'"/>
							</xsl:call-template>
						</xsl:with-param>
					</xsl:call-template>
					<xsl:text> </xsl:text>
				</xsl:if>
				<xsl:if test="count(Tool[@Name='VCLinkerTool']/@EmbedManagedResourceFile) > 0">
					<xsl:call-template name="replace-config">
						<xsl:with-param name="string">
							<xsl:call-template name="split-string">
								<xsl:with-param name="string">
									<xsl:call-template name="replace">
										<xsl:with-param name="string" select="Tool[@Name='VCLinkerTool']/@EmbedManagedResourceFile"/>
										<xsl:with-param name="search" select="'&quot;'"/>
									</xsl:call-template>
								</xsl:with-param>
								<xsl:with-param name="pre" select="'/ASSEMBLYRESOURCE:'"/>
							</xsl:call-template>
						</xsl:with-param>
					</xsl:call-template>
					<xsl:text> </xsl:text>
				</xsl:if>
				<xsl:if test="count(Tool[@Name='VCLinkerTool']/@ForceSymbolReferences) > 0">
					<xsl:call-template name="replace-config">
						<xsl:with-param name="string">
							<xsl:call-template name="split-string">
								<xsl:with-param name="string">
									<xsl:call-template name="replace">
										<xsl:with-param name="string" select="Tool[@Name='VCLinkerTool']/@ForceSymbolReferences"/>
										<xsl:with-param name="search" select="'&quot;'"/>
									</xsl:call-template>
								</xsl:with-param>
								<xsl:with-param name="pre" select="'/INCLUDE:'"/>
							</xsl:call-template>
						</xsl:with-param>
					</xsl:call-template>
					<xsl:text> </xsl:text>
				</xsl:if>
				<xsl:if test="count(Tool[@Name='VCLinkerTool']/@DelayLoadDLLs) > 0 and string-length(Tool[@Name='VCLinkerTool']/@DelayLoadDLLs) > 0">
					<xsl:call-template name="replace-config">
						<xsl:with-param name="string">
							<xsl:call-template name="split-string">
								<xsl:with-param name="string">
									<xsl:call-template name="replace">
										<xsl:with-param name="string" select="Tool[@Name='VCLinkerTool']/@DelayLoadDLLs"/>
										<xsl:with-param name="search" select="'&quot;'"/>
									</xsl:call-template>
								</xsl:with-param>
								<xsl:with-param name="pre" select="'/DELAYLOAD:'"/>
							</xsl:call-template>
						</xsl:with-param>
					</xsl:call-template>
					<xsl:text> </xsl:text>
				</xsl:if>
			</xsl:attribute>
		</property>
	</xsl:template>
	<!-- Handle linker system settings -->
	<xsl:template name="linker-system-settings">
		<property name="link.system">
			<xsl:attribute name="value">
				<xsl:choose>
					<xsl:when test="Tool[@Name='VCLinkerTool']/@SubSystem = 1"> /SUBSYSTEM:CONSOLE</xsl:when>
					<xsl:when test="Tool[@Name='VCLinkerTool']/@SubSystem = 2"> /SUBSYSTEM:WINDOWS</xsl:when>
				</xsl:choose>
			</xsl:attribute>
		</property>
	</xsl:template>
	<!-- Handle linker optimization settings -->
	<xsl:template name="linker-optimization-settings">
		<property name="link.optimize">
			<xsl:attribute name="value">
				<xsl:choose>
					<xsl:when test="Tool[@Name='VCLinkerTool']/@OptimizeReferences = 1"> /OPT:NOREF</xsl:when>
					<xsl:when test="Tool[@Name='VCLinkerTool']/@OptimizeReferences = 2"> /OPT:REF</xsl:when>
				</xsl:choose>
				<xsl:choose>
					<xsl:when test="Tool[@Name='VCLinkerTool']/@EnableCOMDATFolding = 1"> /OPT:NOICF</xsl:when>
					<xsl:when test="Tool[@Name='VCLinkerTool']/@EnableCOMDATFolding = 2"> /OPT:ICF</xsl:when>
				</xsl:choose>
				<xsl:choose>
					<xsl:when test="Tool[@Name='VCLinkerTool']/@OptimizeForWindows98 = 1"> /OPT:NOWIN98</xsl:when>
					<xsl:when test="Tool[@Name='VCLinkerTool']/@OptimizeForWindows98 = 2"> /OPT:WIN98</xsl:when>
				</xsl:choose>
			</xsl:attribute>
		</property>
	</xsl:template>
	<!-- Handle MIDl settings -->
	<xsl:template name="midl-general-settings">
		<property name="midl.general">
			<xsl:attribute name="value">
				<xsl:call-template name="split-string">
					<xsl:with-param name="string" select="Tool[@Name='VCMIDLTool']/@PreprocessorDefinitions"/>
					<xsl:with-param name="pre" select="'/D'"/>
				</xsl:call-template>
				<xsl:if test="string-length(Tool[@Name='VCMIDLTool']/@AdditionalIncludeDirectories)>0">
					<xsl:call-template name="replace-config">
						<xsl:with-param name="string">
							<xsl:call-template name="split-string">
								<xsl:with-param name="string" select="Tool[@Name='VCMIDLTool']/@AdditionalIncludeDirectories"/>
								<xsl:with-param name="add" select="'${dir.srcProj}\'"/>
								<xsl:with-param name="pre" select="'/I '"/>
							</xsl:call-template>
						</xsl:with-param>
					</xsl:call-template>
				</xsl:if>
				<xsl:text> </xsl:text>
				<xsl:if test="Tool[@Name='VCMIDLTool']/@IgnoreStandardIncludePath = 'true'"> /no_def_idir</xsl:if>
				<xsl:if test="Tool[@Name='VCMIDLTool']/@MkTypLibCompatible = 'true'"> /mktyplib203</xsl:if>
				<xsl:choose>
					<xsl:when test="Tool[@Name='VCMIDLTool']/@WarningLevel = 0"> /W0</xsl:when>
					<xsl:when test="Tool[@Name='VCMIDLTool']/@WarningLevel = 1"> /W1</xsl:when>
					<xsl:when test="Tool[@Name='VCMIDLTool']/@WarningLevel = 2"> /W2</xsl:when>
					<xsl:when test="Tool[@Name='VCMIDLTool']/@WarningLevel = 3"> /W3</xsl:when>
					<xsl:when test="Tool[@Name='VCMIDLTool']/@WarningLevel = 4"> /W4</xsl:when>
				</xsl:choose>
				<xsl:if test="Tool[@Name='VCMIDLTool']/@WarnAsError = 'true'"> /WX</xsl:if>
				<xsl:if test="Tool[@Name='VCMIDLTool']/@SuppressStartupBanner = 'true'"> /nologo</xsl:if>
				<xsl:if test="Tool[@Name='VCMIDLTool']/@GenerateStublessProxies = 'true'"> /Oicf</xsl:if>
			</xsl:attribute>
		</property>
		<property name="midl.char">
			<xsl:attribute name="value">
				<xsl:choose>
					<xsl:when test="Tool[@Name='VCMIDLTool']/@DefaultCharType = 1">signed</xsl:when>
					<xsl:when test="Tool[@Name='VCMIDLTool']/@DefaultCharType = 2">ascii7</xsl:when>
					<xsl:otherwise>unsigned</xsl:otherwise>
				</xsl:choose>
			</xsl:attribute>
		</property>
		<property name="midl.env">
			<xsl:attribute name="value">
				<xsl:choose>
					<xsl:when test="Tool[@Name='VCMIDLTool']/@TargetEnvironment = 2">win64</xsl:when>
					<xsl:otherwise>win32</xsl:otherwise>
				</xsl:choose>
			</xsl:attribute>
		</property>
	</xsl:template>
	<xsl:template name="midl-output-settings">
		<property name="midl.output">
			<xsl:attribute name="value">
				<xsl:if test="count(Tool[@Name='VCMIDLTool']/@OutputDirectory) > 0">
					<xsl:text> /out "</xsl:text>
					<xsl:value-of select="Tool[@Name='VCMIDLTool']/@OutputDirectory"/>
					<xsl:text>"</xsl:text>
				</xsl:if>
				<xsl:if test="Tool[@Name='VCMIDLTool']/@GenerateTypeLibrary = 'FALSE'"> /notlb</xsl:if>
			</xsl:attribute>
		</property>
		<property name="midl.header">
			<xsl:attribute name="value">
				<xsl:choose>
					<xsl:when test="string-length(Tool[@Name='VCMIDLTool']/@HeaderFileName) > 0">
						<xsl:value-of select="Tool[@Name='VCMIDLTool']/@HeaderFileName"/>
					</xsl:when>
					<xsl:otherwise>
						<xsl:text>${project.FormalName}.h</xsl:text>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:attribute>
		</property>
		<property name="midl.dlldata">
			<xsl:attribute name="value">
				<xsl:choose>
					<xsl:when test="string-length(Tool[@Name='VCMIDLTool']/@DLLDataFileName) > 0">
						<xsl:value-of select="Tool[@Name='VCMIDLTool']/@DLLDataFileName"/>
					</xsl:when>
					<xsl:otherwise>
						<xsl:text>dlldata.c</xsl:text>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:attribute>
		</property>
		<property name="midl.iid">
			<xsl:attribute name="value">
				<xsl:choose>
					<xsl:when test="string-length(Tool[@Name='VCMIDLTool']/@InterfaceIdentifierFileName) > 0">
						<xsl:value-of select="Tool[@Name='VCMIDLTool']/@InterfaceIdentifierFileName"/>
					</xsl:when>
					<xsl:otherwise>
						<xsl:text>${project.FormalName}_i.c</xsl:text>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:attribute>
		</property>
		<property name="midl.proxy">
			<xsl:attribute name="value">
				<xsl:choose>
					<xsl:when test="string-length(Tool[@Name='VCMIDLTool']/@ProxyFileName) > 0">
						<xsl:value-of select="Tool[@Name='VCMIDLTool']/@ProxyFileName"/>
					</xsl:when>
					<xsl:otherwise>
						<xsl:text>${project.FormalName}_p.c</xsl:text>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:attribute>
		</property>
		<property name="midl.tlb">
			<xsl:attribute name="value">
				<xsl:choose>
					<xsl:when test="string-length(Tool[@Name='VCMIDLTool']/@TypeLibraryName) > 0">
						<xsl:value-of select="Tool[@Name='VCMIDLTool']/@TypeLibraryName"/>
					</xsl:when>
					<xsl:otherwise>
						<xsl:text>${project.FormalName}.tlb</xsl:text>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:attribute>
		</property>
	</xsl:template>
	<xsl:template name="midl-advanced-settings">
		<property name="midl.advanced">
			<xsl:attribute name="value">
				<xsl:choose>
					<xsl:when test="Tool[@Name='VCMIDLTool']/@EnableErrorChecks = 1"> /error none</xsl:when>
					<xsl:when test="Tool[@Name='VCMIDLTool']/@EnableErrorChecks = 2"> /error all</xsl:when>
				</xsl:choose>
				<xsl:if test="Tool[@Name='VCMIDLTool']/@ErrorCheckAllocations = 'true'"> /error allocation</xsl:if>
				<xsl:if test="Tool[@Name='VCMIDLTool']/@ErrorCheckBounds = 'true'"> /error bounds_check</xsl:if>
				<xsl:if test="Tool[@Name='VCMIDLTool']/@ErrorCheckEnumRange = 'true'"> /error enum</xsl:if>
				<xsl:if test="Tool[@Name='VCMIDLTool']/@ErrorCheckRefPointers = 'true'"> /error ref</xsl:if>
				<xsl:if test="Tool[@Name='VCMIDLTool']/@ErrorCheckStubData = 'true'"> /error stub_data</xsl:if>
				<xsl:if test="Tool[@Name='VCMIDLTool']/@ValidateParameters = 'true'"> /error robust</xsl:if>
				<xsl:choose>
					<xsl:when test="Tool[@Name='VCMIDLTool']/@StructMemberAlignment = 1"> /Zp1</xsl:when>
					<xsl:when test="Tool[@Name='VCMIDLTool']/@StructMemberAlignment = 2"> /Zp2</xsl:when>
					<xsl:when test="Tool[@Name='VCMIDLTool']/@StructMemberAlignment = 3"> /Zp4</xsl:when>
					<xsl:when test="Tool[@Name='VCMIDLTool']/@StructMemberAlignment = 4"> /Zp8</xsl:when>
				</xsl:choose>
				<xsl:if test="count(Tool[@Name='VCMIDLTool']/@RedirectOutputAndErrors) > 0">
					<xsl:text> /o </xsl:text>
					<xsl:value-of select="Tool[@Name='VCMIDLTool']/@RedirectOutputAndErrors"/>
				</xsl:if>
				<xsl:if test="count(Tool[@Name='VCMIDLTool']/@CPreprocessOptions) > 0">
					<xsl:call-template name="split-string">
						<xsl:with-param name="string" select="Tool[@Name='VCMIDLTool']/@CPreprocessOptions"/>
						<xsl:with-param name="pre" select="' /cpp_opt'"/>
					</xsl:call-template>
				</xsl:if>
				<xsl:if test="count(Tool[@Name='VCMIDLTool']/@UndefinePreprocessorDefinitions) > 0">
					<xsl:call-template name="split-string">
						<xsl:with-param name="string" select="Tool[@Name='VCMIDLTool']/@UndefinePreprocessorDefinitions"/>
						<xsl:with-param name="pre" select="' /U'"/>
					</xsl:call-template>
				</xsl:if>
			</xsl:attribute>
		</property>
	</xsl:template>
	<!-- helper templates -->

	<!-- extracts the filename and extension out of a path -->
	<xsl:template name="get-filename">
		<xsl:param name="path"/>
		<xsl:choose>
			<xsl:when test="contains($path, '/')">
				<xsl:call-template name="get-filename">
					<xsl:with-param name="path" select="substring-after($path, '/')"/>
				</xsl:call-template>
			</xsl:when>
			<xsl:when test="contains($path, '\')">
				<xsl:call-template name="get-filename">
					<xsl:with-param name="path" select="substring-after($path, '\')"/>
				</xsl:call-template>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="$path"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!-- extracts the directory out of the path (including trailing slash) -->
	<xsl:template name="get-directory">
		<xsl:param name="path"/>
		<xsl:variable name="slashed" select="substring-before($path, '/')"/>
		<xsl:variable name="backslashed" select="substring-before($path, '\')"/>
		<!-- find which one comes first -->
		<xsl:variable name="firstpart">
			<xsl:choose>
				<xsl:when test="string-length($slashed) &lt; string-length($backslashed) and string-length($slashed) > 0">
					<xsl:value-of select="$slashed"/>
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="$backslashed"/>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>

		<xsl:if test="string-length($firstpart) > 0">
			<xsl:value-of select="$firstpart"/><xsl:text>\</xsl:text>
			<xsl:call-template name="get-directory">
				<xsl:with-param name="path" select="substring($path, string-length($firstpart)+2)"/>
			</xsl:call-template>
		</xsl:if>
	</xsl:template>
</xsl:stylesheet>
