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

	<xsl:variable name="projectType">
		<xsl:choose>
			<xsl:when test="ms:Project/ms:Import/@Project='$(MSBuildBinPath)\Microsoft.CSharp.targets'">C#</xsl:when>
			<xsl:when test="ms:Project/ms:Import/@Project='$(MSBuildToolsPath)\Microsoft.CSharp.targets'">C#</xsl:when>
			<xsl:otherwise>VB</xsl:otherwise>
		</xsl:choose>
	</xsl:variable>
	<xsl:variable name="compiler">
		<xsl:choose>
			<xsl:when test="ms:Project/ms:Import/@Project='$(MSBuildBinPath)\Microsoft.CSharp.targets'">csc</xsl:when>
			<xsl:when test="ms:Project/ms:Import/@Project='$(MSBuildToolsPath)\Microsoft.CSharp.targets'">csc</xsl:when>
			<xsl:otherwise>vbc</xsl:otherwise>
		</xsl:choose>
	</xsl:variable>
	<xsl:variable name="is-library">
		<xsl:choose>
			<xsl:when test="ms:Project/ms:PropertyGroup/ms:OutputType = 'Library'
				or ms:Project/ms:PropertyGroup/ms:OutputType = 'Module'">true</xsl:when>
			<xsl:otherwise>false</xsl:otherwise>
		</xsl:choose>
	</xsl:variable>
	<xsl:variable name="GlobalInclude" select="document($XmlInclude)"/>
	<xsl:variable name="filename">
		<xsl:value-of select="$LocalPath"/><xsl:text>\buildinclude.xml</xsl:text>
	</xsl:variable>
	<xsl:variable name="LocalInclude" select="document($filename)"/>

	<!--
-->
	<!-- C# and VB projects -->
	<xsl:template match="ms:Project">
		<xsl:comment>DO NOT MODIFY! This file was generated from the Visual Studio project file by VSConvert-csproj.xsl</xsl:comment>
		<project default="test">
			<xsl:variable name="ext">
				<xsl:choose>
					<xsl:when test="ms:PropertyGroup/ms:OutputType='Library'">.dll</xsl:when>
					<xsl:when test="ms:PropertyGroup/ms:OutputType='WinExe'">.exe</xsl:when>
					<xsl:when test="ms:PropertyGroup/ms:OutputType='Module'">.dll</xsl:when>
					<xsl:otherwise>.exe</xsl:otherwise>
				</xsl:choose>
			</xsl:variable>
			<xsl:call-template name="addProjectNameAttribute">
				<xsl:with-param name="value" select="ms:PropertyGroup/ms:AssemblyName"/>
				<xsl:with-param name="ext" select="$ext"/>
			</xsl:call-template>
			<xsl:call-template name="prolog"/>
			<xsl:call-template name="Build"/>
			<xsl:call-template name="epilog"/>
			<include buildfile="${{fwroot}}\Bld\VSConvert-shared.build.xml"/>
		</project>
	</xsl:template>
	<!--
-->
	<xsl:template name="Build">
		<!-- for C#/VB.NET only -->
		<xsl:apply-templates select="ms:PropertyGroup"/>
		<xsl:apply-templates select="ms:ItemGroup/ms:COMReference[ms:WrapperTool='tlbimp']"/>
		<xsl:apply-templates select="ms:ItemGroup/ms:COMReference[ms:WrapperTool='aximp']"/>
		<xsl:call-template name="IncludePreTarget">
			<xsl:with-param name="target" select="'pre-build'"/>
			<xsl:with-param name="GlobalNodes" select="$GlobalInclude/fw:include/fw:pre-build"/>
			<xsl:with-param name="LocalNodes" select="$LocalInclude/fw:include/fw:pre-build"/>
		</xsl:call-template>
		<target name="build" description="Compile project">
			<xsl:attribute name="depends">
				<xsl:value-of select="'init'"/>
				<xsl:for-each select="ms:ItemGroup/ms:COMReference[ms:WrapperTool='tlbimp' or ms:WrapperTool='aximp']">
					,<xsl:value-of select="@Include"/>
				</xsl:for-each>
				<xsl:call-template name="IncludeDepends">
					<xsl:with-param name="target" select="'pre-build'"/>
					<xsl:with-param name="GlobalNodes" select="$GlobalInclude/fw:include/fw:pre-build"/>
					<xsl:with-param name="LocalNodes" select="$LocalInclude/fw:include/fw:pre-build"/>
				</xsl:call-template>
			</xsl:attribute>
			<if test="${{showTargetsRunInReport}}">
				<property name="appTargName" value="${{project_name}}"/>
				<call target="appendTargetName"/>
			</if>
			<fileset id="cachedFiles" basedir="${{dir.srcProj}}\">
				<include name="${{filename.srcProject}}"/>
				<xsl:apply-templates select="ms:ItemGroup"/>
				<exclude name="GeneratedAssemblyInfo.cs"/>
			</fileset>
			<uptodate property="done.cache" verbose="${{verbose}}">
				<sourcefiles refid="cachedFiles"/>
				<targetfiles>
					<include name="${{dir.buildOutput}}\${{project.output}}"/>
				</targetfiles>
			</uptodate>
			<uptodate property="done.other" verbose="${{verbose}}">
				<sourcefiles basedir="${{dir.srcProj}}\">
					<include name="${{fwroot}}\Bld\VSConvert-csproj.xsl"/>
					<include name="${{dir.nantbuild}}\${{project_name}}.build"/>
					<include name="GeneratedAssemblyInfo.cs"/>
				</sourcefiles>
				<targetfiles>
					<include name="${{dir.buildOutput}}\${{project.output}}"/>
				</targetfiles>
			</uptodate>
			<echo message="Need to build" if="${{verbose and (not done.other or not done.cache)}}"/>
			<echo message="All up-to-date" if="${{verbose}}" unless="${{not done.other or not done.cache}}"/>
			<if test="${{not done.other or not done.cache}}">
				<!-- Disable FileCache since it's not working right now. (recompiling gives
				different MD5 hash value for dll)

				<filescached property="filesInCache" handleproperty="handle" parameters="${{config}}">
					<files refid="cachedFiles"/>
				</filescached>
				<echo message="filesInCache=${{filesInCache}};handle=${{handle}}" if="${{verbose}}"/>
				-->
				<call target="unregister-internal"/>
				<fileset id="refs" basedir="${{dir.srcProj}}\">
					<xsl:apply-templates select="ms:ItemGroup/ms:Reference"/>
					<xsl:apply-templates select="ms:ItemGroup/ms:ProjectReference"/>
					<xsl:apply-templates select="ms:ItemGroup/ms:COMReference"/>
				</fileset>
				<!--
				<if test="${{filesInCache}}">
					<copyfromcache handle="handle" outputdir="${{dir.buildOutput}}"/>
				</if>
				<if test="${{not filesInCache}}">
				-->
					<xsl:apply-templates select="ms:ItemGroup/ms:None[@Include='AssemblyInfo.cs' or @Include='Properties\AssemblyInfo.cs']"
						mode="compile"/>
					<xsl:element name="{$compiler}">
						<xsl:attribute name="target">${target.type}</xsl:attribute>
						<xsl:attribute name="output">${dir.buildOutput}\${project.output}</xsl:attribute>
						<xsl:attribute name="debug">${debug}</xsl:attribute>
						<xsl:attribute name="define">${define}</xsl:attribute>
						<xsl:attribute name="warnaserror">True</xsl:attribute>
						<xsl:attribute name="unsafe">${unsafe}</xsl:attribute>
						<xsl:attribute name="verbose">${verbose}</xsl:attribute>
						<xsl:attribute name="platform">${platformTarget}</xsl:attribute>
						<xsl:if test="$compiler='vbc'">
							<xsl:attribute name="optioncompare">${vbc.optionCompare}</xsl:attribute>
							<xsl:attribute name="optionexplicit">${vbc.optionExplicit}</xsl:attribute>
							<xsl:attribute name="optionstrict">${vbc.optionStrict}</xsl:attribute>
							<xsl:attribute name="removeintchecks">${removeIntChecks}</xsl:attribute>
							<xsl:attribute name="rootnamespace">${rootNamespace}</xsl:attribute>
						</xsl:if>
						<xsl:if test="string-length(ms:PropertyGroup/ms:ApplicationIcon)>0">
							<xsl:attribute name="win32icon">
								${dir.srcProj}\<xsl:value-of select="ms:PropertyGroup/ms:ApplicationIcon"/>
							</xsl:attribute>
						</xsl:if>
						<xsl:attribute name="optimize">${optimize}</xsl:attribute>
						<!--
						This requires the 3.5 compiler
						<xsl:if test="string-length(ms:PropertyGroup/ms:ApplicationManifest) > 0">
							<xsl:element name="arg">
								<xsl:attribute name="line">
									<xsl:text>/win32manifest:"${dir.srcProj}\</xsl:text>
									<xsl:value-of select="ms:PropertyGroup/ms:ApplicationManifest"/>
									<xsl:text>"</xsl:text>
								</xsl:attribute>
							</xsl:element>
						</xsl:if>
						-->
						<arg value='/doc:"${{doc}}"' if="${{doc-exists}}"/>
						<nowarn>
							<warning number="1701" />
							<warning number="1702" />
							<warning number="${{nowarn}}" />
						</nowarn>

						<xsl:apply-templates select="ms:ItemGroup[ms:Compile]" mode="compile"/>
						<xsl:apply-templates select="ms:ItemGroup[ms:EmbeddedResource]" mode="embeddedResource"/>
						<references refid="refs"/>
					</xsl:element>
				<!--
					<cachenewfiles handle="handle">
						<output basedir="${{dir.buildOutput}}">
							<include name="${{project.output}}"/>
							<include name="${{doc}}"/>
						</output>
					</cachenewfiles>
				</if>
				-->
				<copyrefs todir="${{dir.buildOutput}}" failonerror="false">
					<fileset refid="refs"/>
				</copyrefs>
				<!--<available type="File" resource="${{doc}}" property="doc-exists"/>-->
				<if test="${{doc-exists}}">
					<copy file="${{doc}}" todir="${{dir.buildOutput}}" failonerror="false"/>
				</if>
				<call target="register-internal"/>
				<xsl:apply-templates select="ms:ItemGroup/ms:None[@Include='App.config']" mode="compile"/>

				<xsl:call-template name="IncludePostTarget">
					<xsl:with-param name="GlobalNodes" select="$GlobalInclude/fw:include/fw:post-build"/>
					<xsl:with-param name="LocalNodes" select="$LocalInclude/fw:include/fw:post-build"/>
				</xsl:call-template>
			</if>
		</target>
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
		<echo message="project_name=${{project_name}}"/>
	</xsl:template>
	<!--
-->
	<!-- boilerplate epilog code -->
	<xsl:template name="epilog">
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
			<delete file="${{dir.buildOutput}}\${{project.output}}-failed-results.xml" failonerror="false"/>
			<xsl:comment>run tests</xsl:comment>
			<call target="test-internal" cascade="false">
				<xsl:choose>
					<xsl:when test="$TestAvailable = 'True'">
						<!-- we want to fail if test project fails to build-->
					</xsl:when>
					<xsl:otherwise>
						<!-- inside of the test project when we run the tests we ignore errors -->
						<xsl:attribute name="failonerror">false</xsl:attribute>
					</xsl:otherwise>
				</xsl:choose>
			</call>
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
				<xsl:comment>Because the tests failed we want to run them again next time, so
					rename results file</xsl:comment>
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
			<xsl:comment>Set ${fw-test-core-error} to true and at the end to false. If tests fail,
				${fw-test-core-error} will remain true, so we know if anything happened</xsl:comment>
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
					<xsl:if test="ms:ItemGroup/ms:Reference/@Include[starts-with(., 'nunit.framework')]">
						<property name="excludedCategories" value="LongRunning,ByHand,SmokeTest"/>
						<if test="${{property::exists('runAllTests')}}">
							<property name="excludedCategories" value="ByHand,SmokeTest"/>
						</if>
						<xsl:variable name="elemname">
							<xsl:choose>
								<xsl:when test="$localsys-workaround = 'True'">nunit2exworkaround</xsl:when>
								<xsl:otherwise>nunit2ex</xsl:otherwise>
							</xsl:choose>
						</xsl:variable>
						<xsl:element name="{$elemname}">
							<xsl:attribute name="verbose">${verbose}</xsl:attribute>
							<xsl:attribute name="useX86">true</xsl:attribute>
							<xsl:attribute name="excludedCategories">${excludedCategories}</xsl:attribute>
							<xsl:attribute name="if">${forcetests or (not file::up-to-date(dir.buildOutput + '\' + project.output, dir.buildOutput + '\' + project.output + '-results.xml'))}</xsl:attribute>
							<test assemblyname="${{dir.buildOutput}}\${{project.output}}"/>
						</xsl:element>
					</xsl:if>
				</xsl:otherwise>
			</xsl:choose>
			<property name="fw-test-core-error" value="false"/>
		</target>
		<xsl:call-template name="IncludePreTarget">
			<xsl:with-param name="target" select="'pre-clean'"/>
			<xsl:with-param name="GlobalNodes" select="$GlobalInclude/fw:include/fw:pre-clean"/>
			<xsl:with-param name="LocalNodes" select="$LocalInclude/fw:include/fw:pre-clean"/>
		</xsl:call-template>
		<target name="clean" description="Delete output of a build">
			<xsl:attribute name="depends">
				<xsl:value-of select="'init'"/>
				<xsl:call-template name="IncludeDepends">
					<xsl:with-param name="target" select="'pre-clean'"/>
					<xsl:with-param name="GlobalNodes" select="$GlobalInclude/fw:include/fw:pre-clean"/>
					<xsl:with-param name="LocalNodes" select="$LocalInclude/fw:include/fw:pre-clean"/>
				</xsl:call-template>
			</xsl:attribute>
			<!-- C#/VB.NET projects -->
			<xsl:choose>
				<xsl:when test="$TestAvailable = 'True'">
					<xsl:comment>delete test subproject</xsl:comment>
					<property name="dir.srcProjTmp" value="${{dir.srcProj}}"/>
					<property name="filename.destBuildTmp" value="${{filename.destBuild}}"/>
					<property name="filename.srcProjectTmp" value="${{filename.srcProject}}"/>

					<property name="dir.srcProj" value="${{project.basedir}}\${{project.FormalName}}Tests"/>
					<property name="filename.srcProject" value=""/>
					<property name="filename.destBuild" value=""/>
					<call target="vsconvert-clean" cascade="false"/>

					<property name="dir.srcProj" value="${{dir.srcProjTmp}}"/>
					<property name="filename.destBuild" value="${{filename.destBuildTmp}}"/>
					<property name="filename.srcProject" value="${{filename.srcProjectTmp}}"/>
				</xsl:when>
				<xsl:otherwise>
					<!-- delete test output -->
					<delete file="${{dir.buildOutput}}\${{project.output}}-results.xml" verbose="true" failonerror="false"/>
				</xsl:otherwise>
			</xsl:choose>
			<xsl:comment>Delete files specific to C#/VB.NET build process</xsl:comment>
			<call target="unregister-internal"/>
			<delete file="${{dir.fwoutput}}\common\${{project.FormalName}}.tlb" verbose="true" failonerror="false" if="${{registerCom}}" />
			<delete file="${{dir.buildOutput}}\${{project.output}}.incr" verbose="true" failonerror="false"/>
			<!-- delete documentation -->
			<if test="${{doc-exists}}">
				<delete file="${{doc}}" verbose="true" failonerror="false"/>
				<delete file="${{dir.buildOutput}}\${{path::get-file-name(doc)}}" failonerror="false"/>
			</if>
			<!-- delete files generated by Version task -->
			<xsl:apply-templates select="ms:ItemGroup/ms:None[@Include='AssemblyInfo.cs' or @Include='Properties\AssemblyInfo.cs']"
				mode="delete"/>
			<!-- delete files generated by TlbImp task -->
			<xsl:apply-templates select="ms:ItemGroup/ms:COMReference[ms:WrapperTool='tlbimp']" mode="delete"/>
			<!-- delete files generated by AxImp task -->
			<xsl:apply-templates select="ms:ItemGroup/ms:COMReference[ms:WrapperTool='aximp']" mode="delete"/>
			<xsl:comment>Delete generic files</xsl:comment>
			<delete file="${{dir.buildOutput}}\${{project.output}}" verbose="true" failonerror="false"/>
			<delete file="${{dir.buildOutput}}\${{project.FormalName}}.pdb" verbose="true" failonerror="false"/>

			<xsl:call-template name="IncludePostTarget">
				<xsl:with-param name="GlobalNodes" select="$GlobalInclude/fw:include/fw:post-clean"/>
				<xsl:with-param name="LocalNodes" select="$LocalInclude/fw:include/fw:post-clean"/>
			</xsl:call-template>
			<xsl:comment>Finally delete build file itself</xsl:comment>
			<delete file="${{project::get-base-directory()}}\${{project.FormalName}}.build" failonerror="false" />
		</target>
		<target name="register" depends="init,build" description="Re-register the output">
			<call target="register-internal"/>
		</target>
		<target name="register-internal">
			<choose if="${{registerCom}}">
				<when test="${{user::is-admin()}}">
					<regasm assembly="${{dir.buildOutput}}\${{project.output}}" exporttypelib="true"
							typelib="${{dir.fwoutput}}\common\${{project.FormalName}}.tlb" unregister="false" />
				</when>
				<otherwise>
					<!-- running on LUA - have to save information under HKCU -->
					<tlbexp assembly="${{dir.buildOutput}}\${{project.output}}"
							output="${{dir.fwoutput}}\common\${{project.FormalName}}.tlb"
							verbose="${{verbose}}"/>
					<regasm assembly="${{dir.buildOutput}}\${{project.output}}"
							regfile="${{dir.fwoutput}}\common\${{project.FormalName}}.reg" unregister="false" />
					<importregistry regfile="${{dir.fwoutput}}\common\${{project.FormalName}}.reg"
									unregister="false" peruser="true"/>
					<delete file="${{dir.fwoutput}}\common\${{project.FormalName}}.reg" failonerror="false"/>
				</otherwise>
			</choose>
		</target>
		<target name="unregister" depends="init" description="Unregister the output">
			<call target="unregister-internal"/>
		</target>
		<target name="unregister-internal">
			<choose if="${{registerCom and file::exists(dir.fwoutput + '\common\' + project.FormalName + '.tlb')}}">
				<when test="${{user::is-admin()}}">
					<regasm assembly="${{dir.buildOutput}}\${{project.output}}" exporttypelib="true"
							typelib="${{dir.fwoutput}}\common\${{project.FormalName}}.tlb" unregister="true" failonerror="false" />
				</when>
				<otherwise>
					<!-- running on LUA - have to save information under HKCU -->
					<regasm assembly="${{dir.buildOutput}}\${{project.output}}"
							regfile="${{dir.fwoutput}}\common\${{project.FormalName}}.reg" failonerror="false"/>
					<importregistry regfile="${{dir.fwoutput}}\common\${{project.FormalName}}.reg"
									unregister="true" peruser="true" failonerror="false"/>
					<delete file="${{dir.fwoutput}}\common\${{project.FormalName}}.reg" failonerror="false"/>
				</otherwise>
			</choose>
		</target>

		<include buildfile="${{VSConvertBuildFile}}"/>
	</xsl:template>
	<!--
-->
	<xsl:template match="ms:PropertyGroup">
		<!-- do nothing -->
	</xsl:template>
	<xsl:template match="ms:PropertyGroup[ms:ProductVersion]">
		<property name="target.type">
			<xsl:attribute name="value">
				<xsl:choose>
					<xsl:when test="ms:OutputType='Library'">library</xsl:when>
					<xsl:when test="ms:OutputType='WinExe'">winexe</xsl:when>
					<xsl:when test="ms:OutputType='Module'">module</xsl:when>
					<xsl:otherwise>exe</xsl:otherwise>
				</xsl:choose>
			</xsl:attribute>
		</property>
		<property name="project.FormalName" value="{ms:AssemblyName}"/>
		<xsl:variable name="output-file-name">
			<xsl:choose>
				<xsl:when test="$is-library = 'true'">${project.FormalName}.dll</xsl:when>
				<xsl:otherwise>${project.FormalName}.exe</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<xsl:variable name="manifestfile" select="ms:ApplicationManifest"/>
		<target name="all" depends="test"/>
		<xsl:call-template name="IncludePreTarget">
			<xsl:with-param name="target" select="'pre-init'"/>
			<xsl:with-param name="GlobalNodes" select="$GlobalInclude/fw:include/fw:pre-init"/>
			<xsl:with-param name="LocalNodes" select="$LocalInclude/fw:include/fw:pre-init"/>
		</xsl:call-template>
		<target name="init" description="Initialize properties for the build">
			<xsl:attribute name="depends">init-${config}<xsl:call-template name="IncludeDepends">
					<xsl:with-param name="target" select="'pre-init'"/>
					<xsl:with-param name="GlobalNodes" select="$GlobalInclude/fw:include/fw:pre-init"/>
					<xsl:with-param name="LocalNodes" select="$LocalInclude/fw:include/fw:pre-init"/>
				</xsl:call-template>
			</xsl:attribute>
			<tstamp/>
			<mkdir dir="${{dir.outputBase}}"/>
			<mkdir dir="${{dir.buildOutput}}"/>
			<property name="project.output" value="{$output-file-name}"/>
			<property name="rootNamespace" value="{ms:RootNamespace}"/>
			<xsl:call-template name="IncludePostTarget">
				<xsl:with-param name="GlobalNodes" select="$GlobalInclude/fw:include/fw:post-init"/>
				<xsl:with-param name="LocalNodes" select="$LocalInclude/fw:include/fw:post-init"/>
			</xsl:call-template>
		</target>
		<xsl:apply-templates select="../ms:PropertyGroup" mode="general"/>
	</xsl:template>
	<!--
-->
	<xsl:template match="ms:PropertyGroup" mode="general">
		<xsl:choose>
			<xsl:when test="contains(@Condition, 'Debug')">
				<xsl:call-template name="configuration">
					<xsl:with-param name="kind">Debug</xsl:with-param>
					<xsl:with-param name="outputDir">Debug</xsl:with-param>
				</xsl:call-template>
				<xsl:call-template name="configuration">
					<xsl:with-param name="kind">Bounds</xsl:with-param>
					<xsl:with-param name="outputDir">Debug</xsl:with-param>
				</xsl:call-template>
			</xsl:when>
			<xsl:when test="contains(@Condition, 'Release')">
				<xsl:call-template name="configuration">
					<xsl:with-param name="kind">Release</xsl:with-param>
					<xsl:with-param name="outputDir">Release</xsl:with-param>
				</xsl:call-template>
			</xsl:when>
			<xsl:otherwise>
				<!--
			Do nothing for any other kind of config, such as the old Bounds config.
			The old Bounds stuff is handled another way now.
			-->
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--
-->
	<xsl:template name="configuration">
		<xsl:param name="kind"/>
		<xsl:param name="outputDir"/>
		<!-- create targets for each configuration listed -->
		<target>
			<xsl:attribute name="name">init-<xsl:value-of select="$kind"/>
			</xsl:attribute>
			<property name="dir.buildOutput">
				<xsl:choose>
					<xsl:when test="$UseVsPath = 'True'">
						<!-- I (RandyR) think this may only be used when running nant inside VS. -->
						<xsl:attribute name="value">${dir.srcProj}\<xsl:value-of select="@OutputPath"/>
						</xsl:attribute>
					</xsl:when>
					<xsl:otherwise>
						<!-- I (RandyR) think this is used for regular nant runs outside of VS. -->
						<xsl:attribute name="value">${dir.outputBase}\<xsl:value-of select="$outputDir"/>
						</xsl:attribute>
					</xsl:otherwise>
				</xsl:choose>
			</property>
			<property name="dir.obj">
				<xsl:attribute name="value">${dir.fwobj}\<xsl:value-of select="$outputDir"/>\${project.FormalName}</xsl:attribute>
			</property>
			<property name="dir.customConfig">
				<xsl:attribute name="value">
					<xsl:value-of select="$outputDir"/>
				</xsl:attribute>
			</property>
			<property name="define">
				<xsl:attribute name="value">
					<!-- if the project already contains a define for the platform (i.e. WIN32 or UNIX), we
					want to strip that and add the current platform -->
					<xsl:choose>
						<xsl:when test="contains(ms:DefineConstants, 'WIN32')">
							<xsl:value-of select="substring-before(ms:DefineConstants, ';WIN32')"/>
							<xsl:if test="(string-length(substring-before(ms:DefineConstants, ';WIN32')) > 0) and (string-length(substring-after(ms:DefineConstants, 'WIN32;')) > 0)">;</xsl:if>
							<xsl:value-of select="substring-after(ms:DefineConstants, 'WIN32;')"/>
						</xsl:when>
						<xsl:when test="contains(ms:DefineConstants, 'UNIX')">
							<xsl:value-of select="substring-before(ms:DefineConstants, ';UNIX')"/>
							<xsl:if test="(string-length(substring-before(ms:DefineConstants, ';UNIX')) > 0) and (string-length(substring-after(ms:DefineConstants, 'UNIX;')) > 0)">;</xsl:if>
							<xsl:value-of select="substring-after(ms:DefineConstants, 'UNIX;')"/>
						</xsl:when>
						<xsl:otherwise>
							<xsl:value-of select="ms:DefineConstants"/>
						</xsl:otherwise>
					</xsl:choose>;NANT_BUILD;${platform}</xsl:attribute>
			</property>
			<property name="nowarn" value="{translate(ms:NoWarn, ';', ',')}"/>
			<property name="optimize" value="{ms:Optimize}"/>
			<property name="platformTarget" value="{ms:PlatformTarget}"/>
			<property name="unsafe">
				<xsl:choose>
					<xsl:when test="string-length(ms:AllowUnsafeBlocks)>0">
						<xsl:attribute name="value">
							<xsl:value-of select="ms:AllowUnsafeBlocks"/>
						</xsl:attribute>
					</xsl:when>
					<xsl:otherwise>
						<xsl:attribute name="value">false</xsl:attribute>
					</xsl:otherwise>
				</xsl:choose>
			</property>
			<xsl:choose>
				<xsl:when test="string-length(ms:DebugType)>0">
					<!-- replace first char with uppercase equivalent. The only possible choices
						for DebugType are None, Enable, Full, PdbOnly -->
					<property name="debug"
						value="{concat(translate(substring(ms:DebugType, 1, 1), 'nefp', 'NEFP'), substring(ms:DebugType, 2))}"/>
				</xsl:when>
				<xsl:otherwise>
					<property name="debug" value="None"/>
				</xsl:otherwise>
			</xsl:choose>
			<xsl:choose>
				<xsl:when test="string-length(ms:DocumentationFile)>0">
					<property name="doc-exists" value="true"/>
					<property name="doc" value="${{dir.srcProj}}\{ms:DocumentationFile}"/>
					<mkdir dir="${{doc}}/.."/>
				</xsl:when>
				<xsl:otherwise>
					<property name="doc-exists" value="false"/>
					<property name="doc" value=""/>
				</xsl:otherwise>
			</xsl:choose>
			<property name="removeintchecks" value="{ms:RemoveIntegerChecks}"/>
			<property name="registerCom">
				<xsl:choose>
					<xsl:when test="string-length(ms:RegisterForComInterop)>0">
						<xsl:attribute name="value">
							<xsl:value-of select="ms:RegisterForComInterop"/>
						</xsl:attribute>
					</xsl:when>
					<xsl:otherwise>
						<xsl:attribute name="value">false</xsl:attribute>
					</xsl:otherwise>
				</xsl:choose>
			</property>
		</target>
	</xsl:template>
	<!--
-->
	<!-- Rules for determining if we have to compile, i.e. if anything changed -->
	<xsl:template match="ms:ItemGroup">
		<xsl:apply-templates/>
	</xsl:template>
	<xsl:template match="ms:BootstrapperPackage">
		<!-- Ignore this (new in VS 2008) -->
	</xsl:template>
	<xsl:template match="ms:Compile|ms:EmbeddedResource">
		<include name="${{dir.srcProj}}\{@Include}" asis="true"/>
	</xsl:template>
	<xsl:template match="ms:None">
		<!-- just ignore -->
	</xsl:template>
	<!--
-->
	<xsl:template match="ms:None[@Include='AssemblyInfo.cs' or @Include='Properties\AssemblyInfo.cs']" mode="compile">
		<versionex>
			<xsl:attribute name="output">${dir.srcProj}\<xsl:value-of select="substring-before(@Include, '\')"/>\GeneratedAssemblyInfo.cs</xsl:attribute>
			<sources>
				<include name="${{dir.srcProj}}\{@Include}" asis="true"/>
			</sources>
		</versionex>
	</xsl:template>
	<xsl:template match="ms:None[@Include='App.config']" mode="compile">
		<copy file="${{dir.srcProj}}\{@Include}"
			tofile="${{dir.buildOutput}}\${{project.output}}.config" failonerror="false"/>
	</xsl:template>
	<xsl:template match="ms:None[@Include='AssemblyInfo.cs' or @Include='Properties\AssemblyInfo.cs']" mode="delete">
		<delete verbose="true" failonerror="false">
			<xsl:attribute name="file">${dir.srcProj}\<xsl:value-of select="substring-before(@Include, '\')"/>\GeneratedAssemblyInfo.cs</xsl:attribute>
		</delete>
	</xsl:template>
	<!--
-->
	<!-- rules to handle importing COM components via tlbimp -->
	<xsl:template match="ms:COMReference" mode="tlbimp">
		<target name="{@Include}" depends="init">
			<xsl:element name="gettypelib">
				<xsl:attribute name="guid">
					<xsl:value-of select="ms:Guid"/>
				</xsl:attribute>
				<xsl:attribute name="propertyname">
					<xsl:value-of select="@Include"/>.path
				</xsl:attribute>
				<xsl:attribute name="versionmajor">
					<xsl:value-of select="ms:VersionMajor"/>
				</xsl:attribute>
				<xsl:attribute name="versionminor">
					<xsl:value-of select="ms:VersionMinor"/>
				</xsl:attribute>
				<xsl:attribute name="lcid">
					<xsl:value-of select="ms:Lcid"/>
				</xsl:attribute>
			</xsl:element>
			<xsl:element name="tlbimp">
				<xsl:variable name="Name" select="@Include"/>
				<xsl:attribute name="output">
					${dir.buildOutput}/<xsl:value-of select="$Name"/>Interop.dll
				</xsl:attribute>
				<xsl:attribute name="typelib">
					${<xsl:value-of select="$Name"/>.path}
				</xsl:attribute>
				<xsl:attribute name="keyfile">${fwroot}\src\FieldWorks.snk</xsl:attribute>
				<xsl:apply-templates select="$GlobalInclude/include/taskconfig/tlbimp/typelib[@Include=$Name]/namespace" mode="include"/>
				<xsl:apply-templates select="$GlobalInclude/include/taskconfig/tlbimp/typelib[@Include=$Name]/references" mode="include"/>
			</xsl:element>
		</target>
	</xsl:template>
	<xsl:template match="ms:COMReference[ms:WrapperTool='tlbimp']" mode="delete">
		<xsl:element name="delete">
			<xsl:attribute name="file">
				${dir.buildOutput}/<xsl:value-of select="@Include"/>Interop.dll
			</xsl:attribute>
			<xsl:attribute name="verbose">true</xsl:attribute>
			<xsl:attribute name="failonerror">false</xsl:attribute>
		</xsl:element>
		<xsl:comment>Also delete the Interop dlls that Visual Studio generates automatically</xsl:comment>
		<xsl:element name="delete">
			<xsl:attribute name="file">
				${dir.buildOutput}/Interop.<xsl:value-of select="@Include"/>.dll
			</xsl:attribute>
			<xsl:attribute name="verbose">true</xsl:attribute>
			<xsl:attribute name="failonerror">false</xsl:attribute>
		</xsl:element>
	</xsl:template>
	<!--
-->
	<!-- Rules to handle importing ActiveX components via aximp. -->
	<!--
	<COMReference Include="AxSHDocVw">
	  <Guid>{EAB22AC0-30C1-11CF-A7EB-0000C05BAE0B}</Guid>
	  <VersionMajor>1</VersionMajor>
	  <VersionMinor>1</VersionMinor>
	  <Lcid>0</Lcid>
	  <WrapperTool>aximp</WrapperTool>
	  <Isolated>False</Isolated>
	</COMReference>
-->
	<xsl:template match="ms:COMReference[ms:WrapperTool='aximp']">
		<target name="{@Include}" depends="init">
			<xsl:element name="gettypelib">
				<xsl:attribute name="guid">
					<xsl:value-of select="ms:Guid"/>
				</xsl:attribute>
				<xsl:attribute name="propertyname">
					<xsl:value-of select="@Include"/>.path
				</xsl:attribute>
				<xsl:attribute name="versionmajor">
					<xsl:value-of select="ms:VersionMajor"/>
				</xsl:attribute>
				<xsl:attribute name="versionminor">
					<xsl:value-of select="ms:VersionMinor"/>
				</xsl:attribute>
				<xsl:attribute name="lcid">
					<xsl:value-of select="ms:Lcid"/>
				</xsl:attribute>
			</xsl:element>
			<xsl:element name="aximp">
				<xsl:variable name="Name" select="@Include"/>
				<xsl:attribute name="ocx">
					${<xsl:value-of select="@Include"/>.path}
				</xsl:attribute>
				<xsl:attribute name="out">
					${dir.buildOutput}/<xsl:value-of select="$Name"/>Interop.dll
				</xsl:attribute>
				<xsl:attribute name="keyfile">${fwroot}\src\FieldWorks.snk</xsl:attribute>
				<xsl:apply-templates select="$GlobalInclude/include/taskconfig/aximp/typelib[@Include=$Name]/namespace" mode="include"/>
				<xsl:apply-templates select="$GlobalInclude/include/taskconfig/aximp/typelib[@Include=$Name]/references" mode="include"/>
			</xsl:element>
		</target>
	</xsl:template>
	<xsl:template match="ms:COMReference[ms:WrapperTool='aximp']" mode="delete">
		<xsl:element name="delete">
			<xsl:attribute name="file">
				${dir.buildOutput}/<xsl:value-of select="@Include"/>Interop.dll
			</xsl:attribute>
			<xsl:attribute name="verbose">true</xsl:attribute>
			<xsl:attribute name="failonerror">false</xsl:attribute>
		</xsl:element>
		<xsl:comment>Also delete the Interop dlls that Visual Studio generates automatically</xsl:comment>
		<xsl:element name="delete">
			<xsl:attribute name="file">
				${dir.buildOutput}/Interop.<xsl:value-of select="@Include"/>.dll
			</xsl:attribute>
			<xsl:attribute name="verbose">true</xsl:attribute>
			<xsl:attribute name="failonerror">false</xsl:attribute>
		</xsl:element>
	</xsl:template>
	<!--
-->
	<xsl:template match="ms:namespace" mode="include">
		<xsl:attribute name="namespace">
			<xsl:value-of select="."/>
		</xsl:attribute>
	</xsl:template>
	<xsl:template match="ms:references" mode="include">
		<xsl:copy-of select="."/>
	</xsl:template>
	<!--
-->
	<xsl:template match="ms:ItemGroup" mode="compile">
		<sources>
			<xsl:attribute name="basedir">${dir.srcProj}\</xsl:attribute>
			<xsl:apply-templates select="ms:Compile"/>
		</sources>
	</xsl:template>
	<!--
-->
	<xsl:template match="ms:ItemGroup" mode="embeddedResource">
		<resources>
			<xsl:attribute name="basedir">${dir.srcProj}\</xsl:attribute>
			<xsl:attribute name="dynamicprefix">true</xsl:attribute>
			<xsl:attribute name="prefix">${rootNamespace}</xsl:attribute>
			<xsl:apply-templates select="ms:EmbeddedResource"/>
		</resources>
	</xsl:template>
	<!--
-->
	<!-- straight assembly reference -->
	<xsl:template match="ms:Reference">
		<include>
			<xsl:attribute name="name">
				<xsl:choose>
					<xsl:when test="ms:HintPath">
						<xsl:call-template name="replace-config">
							<xsl:with-param name="string" select="ms:HintPath"/>
						</xsl:call-template>
					</xsl:when>
					<xsl:otherwise>
						<xsl:choose>
							<!-- for now we assume that if we don't have a HintPath but a version number etc. after a comma,
							it is a local reference (which got added in VS2005), otherwise it's a reference in the GAC -->
							<xsl:when test="contains(@Include, ',')"><xsl:text>${dir.buildOutput}\</xsl:text><xsl:value-of select="substring-before(@Include, ',')"/></xsl:when>
							<!-- special case NUnit.framework because that's in the GAC, but not in the frameworkdirectory -->
							<xsl:when test="@Include='nunit.framework'">
								<xsl:text>${dir.buildOutput}\</xsl:text>
								<xsl:value-of select="@Include"/>
							</xsl:when>
							<xsl:otherwise>${framework::get-framework-directory(framework::get-target-framework())}\<xsl:value-of select="@Include"/></xsl:otherwise>
						</xsl:choose>
						<xsl:text>.dll</xsl:text>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:attribute>
		</include>
	</xsl:template>
	<!-- project references -->
	<xsl:template match="ms:ProjectReference">
		<!-- we assume that the project being referenced has been built
		already and the compiled dll is sitting in the lib directory. -->
		<include name="${{dir.buildOutput}}/{ms:Name}.dll"/>
	</xsl:template>
	<!-- COM object reference -->
	<xsl:template match="ms:COMReference">
		<!-- the tlbimp and aximp tasks will put the interop lib into the output directory -->
		<include name="${{dir.buildOutput}}/{@Include}Interop.dll"/>
	</xsl:template>
	<!--
-->
	<!-- Templates to include parts of global and local include file -->
	<xsl:template name="IncludePreTarget">
		<xsl:param name="target"/>
		<xsl:param name="GlobalNodes"/>
		<xsl:param name="LocalNodes"/>
		<xsl:if test="count($GlobalNodes) > 0 or count($LocalNodes) > 0">
			<target>
				<xsl:attribute name="name">
					<xsl:value-of select="$target"/>
				</xsl:attribute>
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
				<xsl:value-of select="substring-before($string, 'debug')"/>${dir.customConfig}<xsl:value-of select="substring-after($string, 'debug')"/>
			</xsl:when>
			<xsl:when test="contains($string, 'Debug')">
				<xsl:value-of select="substring-before($string, 'Debug')"/>${dir.customConfig}<xsl:value-of select="substring-after($string, 'Debug')"/>
			</xsl:when>
			<xsl:when test="contains($string, 'release')">
				<xsl:value-of select="substring-before($string, 'release')"/>${dir.customConfig}<xsl:value-of select="substring-after($string, 'release')"/>
			</xsl:when>
			<xsl:when test="contains($string, 'Release')">
				<xsl:value-of select="substring-before($string, 'Release')"/>${dir.customConfig}<xsl:value-of select="substring-after($string, 'Release')"/>
			</xsl:when>
			<xsl:when test="contains($string, 'Program Files')">
				<xsl:value-of select="'${sys.os.folder.programfiles}\'"/>
				<xsl:value-of select="substring-after($string, 'Program Files')"/>
			</xsl:when>
			<xsl:when test="contains($string, '$(ConfigurationName)')">
				<xsl:value-of select="substring-before($string, '$(ConfigurationName)')"/>${dir.customConfig}<xsl:value-of select="substring-after($string, '$(ConfigurationName)')"/>
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

</xsl:stylesheet>
