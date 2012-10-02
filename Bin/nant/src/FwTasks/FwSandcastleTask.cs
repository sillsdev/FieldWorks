// --------------------------------------------------------------------------------------------
#region // Copyright 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwSandcastleTask.cs
// Responsibility: Corey Wenger and Jon Shaneyfelt
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
// This task builds MSDN style class documentation for FieldWorks using Sandcastle.

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

using NAnt.DotNet.Types;

namespace NAnt.DotNet.Tasks {
	/// <summary>
	/// Runs Sandcastle 2.4.10115 (1/16/2008) to create documentation.
	/// </summary>
	/// <remarks>
	///   <para>
	///   See the <see href="http://www.codeplex.com/Sandcastle">Sandcastle home page</see> for more
	///   information.
	///   </para>
	/// </remarks>
	/// <example>
	///   <para>
	///   Document two assemblies using Sandcastle.
	///   </para>
	///   <code>
	///     <![CDATA[
	///			<target name="sandcastle-VS2005"
	///			    description="Builds VS2005-style class documentation using Sandcastle">
	///			    <property name="sandcastledoctype" value="vs2005"/>
	///			    <call target="sandcastle" cascade="true"/>
	///			</target>
	///			<target name="sandcastle-Hana"
	///			    description="Builds Hana-style class documentation using Sandcastle">
	///			    <property name="sandcastledoctype" value="Hana"/>
	///			    <call target="sandcastle" cascade="true"/>
	///			</target>
	///			<target name="sandcastle-Prototype"
	///			    description="Builds class documentation using Sandcastle">
	///			    <property name="sandcastledoctype" value="Prototype"/>
	///			    <call target="sandcastle" cascade="true"/>
	///			</target>
	///			<target name="sandcastle">
	///			    <fwsandcastle
	///			        verbose="${verbose}"
	///			        documenttype="${sandcastledoctype}"
	///			        sandcastlepath="${fwroot}/bin/Sandcastle"
	///			        hhpath="C:/Program Files/HTML Help Workshop"
	///			        outputpath="${dir.fwoutput}/docs/HTML/FieldWorks">
	///			        <assemblies basedir="${dir.fwoutput}/${config}">
	///			            <include name="BasicUtils.dll"/>
	///			            <include name="ZipUtils.dll"/>
	///			        </assemblies>
	///			    </fwsandcastle>
	///				<if test="${file::exists('${dir.fwoutput}/docs/FieldWorks_Classes/Chm/FieldWorks_Classes.chm')}">
	///					<copy
	///				        file="${dir.fwoutput}/docs/FieldWorks_Classes/Chm/FieldWorks_Classes.chm"
	///				        tofile="${dir.fwoutput}/docs/FieldWorks_Classes.chm"/>
	///				</if>
	///			</target>
	///     ]]>
	///   </code>
	/// </example>
	[TaskName("fwsandcastle")]
	public class FwSandcastleTask : Task
	{
		#region Private Instance Fields

		private AssemblyFileSet m_assemblies = new AssemblyFileSet();
		private string m_documentType = string.Empty;
		private string m_sandcastlePath = string.Empty;
		private string m_hhPath = string.Empty;
		private string m_outputPath = string.Empty;

		#endregion Private Instance Fields

		#region Public Instance Properties

		/// <summary>
		/// The set of assemblies to document.
		/// </summary>
		[BuildElement("assemblies", Required=true)]
		public AssemblyFileSet Assemblies {
			get { return m_assemblies; }
			set { m_assemblies = value; }
		}

		/// <summary>
		/// Type of documentation to generate.
		/// </summary>
		[TaskAttribute("documenttype")]
		public string DocumentType
		{
			get { return m_documentType; }
			set { m_documentType = value; }
		}

		/// <summary>
		/// Path to Sandcastle location.
		/// </summary>
		[TaskAttribute("sandcastlepath")]
		public string SandcastlePath
		{
			get { return m_sandcastlePath; }
			set { m_sandcastlePath = value; }
		}

		/// <summary>
		/// Path to HTML Help Workshop location.
		/// </summary>
		[TaskAttribute("hhpath")]
		public string HHPath
		{
			get { return m_hhPath; }
			set { m_hhPath = value; }
		}

		/// <summary>
		/// Path to location to generate documentation into.
		/// </summary>
		[TaskAttribute("outputpath")]
		public string OutputPath
		{
			get { return m_outputPath; }
			set { m_outputPath = value; }
		}

		#endregion Public Instance Properties

		#region Override implementation of Task

		/// <summary>
		/// Initializes the task.
		/// </summary>
		/// <param name="taskNode"><see cref="XmlNode" /> containing the XML fragment used to define this task instance.</param>
		protected override void InitializeTask(XmlNode taskNode)
		{
			Log(Level.Verbose, "InitializeTask()");

			if (m_sandcastlePath == string.Empty)
				throw (new BuildException("*** FW Sandcastle Task: missing 'sandcastlepath' attribute. ***"));

			if (m_hhPath == string.Empty)
				throw (new BuildException("*** FW Sandcastle Task: missing 'hhpath' attribute. ***"));

			if (OutputPath == string.Empty)
				throw (new BuildException("*** FW Sandcastle Task: missing 'outputpath' attribute. ***"));

			if (!Directory.Exists(OutputPath))
				Directory.CreateDirectory(OutputPath);

			if (!Directory.Exists(OutputPath))
				throw (new BuildException("FW Sandcastle Task: Output directory not found."));

			// Remove html files from previous builds
			if (Directory.Exists(OutputPath + "/Output/html"))
			{
				DirectoryInfo dir = new DirectoryInfo(OutputPath + "/Output/html");
				FileInfo[] files = dir.GetFiles("*.*");
				foreach (FileInfo file in files)
					File.Delete(OutputPath + "/Output/html/" + file.Name);
			}
			if (Directory.Exists(OutputPath + "/Chm/html"))
			{
				DirectoryInfo dir = new DirectoryInfo(OutputPath + "/Chm/html");
				FileInfo[] files = dir.GetFiles("*.*");
				foreach (FileInfo file in files)
					File.Delete(OutputPath + "/Chm/html/" + file.Name);
			}
		}

		/// <summary>
		/// Builds the documentation with Sandcastle.
		/// </summary>
		protected override void ExecuteTask()
		{
			Log(Level.Verbose, "ExecuteTask()");

			// Make sure there is at least one included assembly.  This can't
			// be done in the InitializeTask() method because the files might
			// not have been built at startup time.
			if (Assemblies.FileNames.Count == 0)
			{
				throw new BuildException("There must be at least one included assembly.", Location);
			}

			// Exclude assemblies with no XML comments file.
			foreach (string filename in Assemblies.FileNames)
			{
				if (!File.Exists(GetXMLCommentsFilePath(filename)))
				{
					Log(Level.Verbose, "{0}: missing .xml file, removing...", filename);
					Assemblies.Excludes.Add(filename);
					//Assemblies.FileNames.Remove(filename);
				}
			}

			// Re-scan the fileset to remove assemblies we just excluded.
			Assemblies.Scan();

			Log(Level.Verbose, "OutputPath = {0}", OutputPath);
			Log(Level.Verbose, "DocumentType = {0}", DocumentType);

			try
			{
				BuildDocumentation();
			}
			catch (Exception ex)
			{
				throw new BuildException("Error building documentation.", Location, ex);
			}
		}

		#endregion Override implementation of Task

		#region Private Instance Methods

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the XML comments file path for a specified assembly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static string GetXMLCommentsFilePath(string filename)
		{
			string path;

			if (Path.GetExtension(filename) == ".exe")
			{
				string commentsFilename = Path.GetFileNameWithoutExtension(filename) + "Exe.xml";
				path = Path.Combine(Path.GetDirectoryName(filename), commentsFilename);
			}
			else
				path = Path.ChangeExtension(filename, ".xml");

			return path;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Build documentation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void BuildDocumentation()
		{
			Log(Level.Verbose, "BuildDocumentation()");

			ProcessStartInfo startInfo = new ProcessStartInfo();
			startInfo = SetStartInfo(startInfo);

			// Generate reflection report
			RunMRefBuilder(startInfo);

			// Apply Transforms to reflection report
			// XslTransform
			//		/xsl:"%DXROOT%\ProductionTransforms\ApplyVSDocModel.xsl"
			//		reflection.org
			// 		/xsl:"%DXROOT%\ProductionTransforms\AddFriendlyFilenames.xsl"
			//		/out:reflection.xml
			//		/arg:IncludeAllMembersTopic=true
			//		/arg:IncludeInheritedOverloadTopics=true
			string[] transforms = new string[2];
			transforms[0] = "ApplyVSDocModel.xsl";
			transforms[1] = "AddFriendlyFilenames.xsl";
			string args = "/arg:IncludeAllMembersTopic=true "
				+ "/arg:IncludeInheritedOverloadTopics=true";
			ApplyXSLT(transforms, "reflection.org", "reflection.xml", args, startInfo);

			// Generate topic manifest
			// XslTransform
			//		/xsl:"%DXROOT%\ProductionTransforms\ReflectionToManifest.xsl"
			//		reflection.xml /out:manifest.xml
			transforms = new string[1];
			transforms[0] = "ReflectionToManifest.xsl";
			ApplyXSLT(transforms, "reflection.xml", "manifest.xml", null, startInfo);

			// Copy supporting files (icons, scripts and styles)
			CopySupportingFiles(startInfo);

			CopyComments();

			// Generate HTML
			RunBuildAssembler(startInfo);

			// Generate intermediate TOC file
			// XslTransform
			//		/xsl:"%DXROOT%\ProductionTransforms\createvstoc.xsl"
			//		reflection.xml /out:toc.xml
			transforms = new string[1];
			transforms[0] = "createvstoc.xsl";
			ApplyXSLT(transforms, "reflection.xml", "toc.xml", null, startInfo);

			// Generate CHM
			SetupChmFolder();
			RunChmBuilder(startInfo);
			RunDBCSFix(startInfo);
			CompileChmFile(startInfo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set start information (e.g., working directory, environment variables) for
		/// sandcastle processes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ProcessStartInfo SetStartInfo(ProcessStartInfo startInfo)
		{
			startInfo.WorkingDirectory = OutputPath;
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardOutput = true;
			startInfo.EnvironmentVariables["DXROOT"] = SandcastlePath;

			return startInfo;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compile assembly reflection report using the MRefBuilder utility.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void RunMRefBuilder(ProcessStartInfo startInfo)
		{
			startInfo.FileName = string.Format("\"{0}/ProductionTools/MRefBuilder.exe\"",
				SandcastlePath);
			for (int i = 0; i < Assemblies.FileNames.Count; i++)
				startInfo.Arguments += string.Format("\"{0}\" ", Assemblies.FileNames[i]);
			startInfo.Arguments += "/out:reflection.org";
			Log(Level.Verbose, "{0} {1}", startInfo.FileName, startInfo.Arguments);

			Process proc = new Process();
			proc = Process.Start(startInfo);
			proc.WaitForExit();
			CaptureStdOut(proc, true); // for debugging
			Log(Level.Verbose, "{0} Exit Code: {1}", DateTime.Now.ToString("T"), proc.ExitCode);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Apply an XSLT using the XSLTransform utility.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ApplyXSLT(string[] transforms, string inFile, string outFile, string args, ProcessStartInfo startInfo)
		{
			// TODO: enclose path in quotes. Mixed \ and /'s are a problem.
			startInfo.FileName = string.Format("\"{0}/ProductionTools/XSLTransform.exe\"",
				SandcastlePath);
			//startInfo.FileName = Path.GetFullPath(startInfo.FileName);
			startInfo.Arguments = string.Empty;

			// add transform to argument list
			string xslPathFormat = "/xsl:\"{0}/ProductionTransforms/{1}\" ";
			startInfo.Arguments += string.Format(xslPathFormat, SandcastlePath, transforms[0]);

			// add input file
			startInfo.Arguments += inFile + " ";

			// add second transform (if supplied)
			if (transforms.Length > 1)
				startInfo.Arguments
					+= string.Format(xslPathFormat, SandcastlePath, transforms[1]);

			// add output file
			startInfo.Arguments += "/out:" + outFile;

			// add arguments
			startInfo.Arguments += " " + args;

			Log(Level.Verbose, "{0} {1}", startInfo.FileName, startInfo.Arguments);

			Process proc = new Process();
			proc = Process.Start(startInfo);
			proc.WaitForExit();
			CaptureStdOut(proc, true); // for debugging
			Log(Level.Verbose, "{0} Exit Code: {1}", DateTime.Now.ToString("T"), proc.ExitCode);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy supporting files (icons, scripts and styles) for generating HTML.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CopySupportingFiles(ProcessStartInfo startInfo)
		{
			// call "%DXROOT%\Presentation\%1\copyOutput.bat"

			// The process seems to hang if we don't read StandardOutput.
			startInfo.RedirectStandardOutput = true;
			startInfo.FileName = string.Format("\"{0}/Presentation/{1}/copyOutput.bat\"",
				SandcastlePath, DocumentType);
			startInfo.Arguments = string.Empty;
			Log(Level.Verbose, "{0} {1}", startInfo.FileName, startInfo.Arguments);

			Process proc = new Process();
			proc = Process.Start(startInfo);
			CaptureStdOut(proc, false);
			Log(Level.Verbose, "{0} Exit Code: {1}", DateTime.Now.ToString("T"), proc.ExitCode);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy XML comments file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CopyComments()
		{
			string commentsPath = Path.Combine(OutputPath, "comments");
			if (!Directory.Exists(commentsPath))
				Directory.CreateDirectory(commentsPath);
			foreach (string fileName in Assemblies.FileNames)
			{
				string sourceFile = GetXMLCommentsFilePath(fileName);
				string assemblyName = Path.GetFileNameWithoutExtension(fileName);
				string destFile = Path.Combine(commentsPath, assemblyName + ".xml");
				File.Copy(sourceFile, destFile, true);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Generate HTML using BuildAssembler utility.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void RunBuildAssembler(ProcessStartInfo startInfo)
		{
			// BuildAssembler
			//		/config:"%DXROOT%\Presentation\%1\configuration\sandcastle.config"
			//		manifest.xml

			// The process seems to hang if we don't read StandardOutput.
			startInfo.RedirectStandardOutput = true;
			startInfo.FileName =  string.Format("\"{0}/ProductionTools/BuildAssembler.exe\"",
				SandcastlePath);
			string configPath = Path.Combine(
				Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
				string.Format("FwSandcastle.{0}.config", DocumentType));
			startInfo.Arguments = string.Format("/config:\"{0}\" manifest.xml", configPath);
			Log(Level.Verbose, "{0} {1}", startInfo.FileName, startInfo.Arguments);

			Process proc = new Process();
			proc = Process.Start(startInfo);
			CaptureStdOut(proc, true);
			Log(Level.Verbose, "{0} Exit Code: {1}", DateTime.Now.ToString("T"), proc.ExitCode);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the Chm folder and copy supporting files.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetupChmFolder()
		{
			// Copy icons, scripts and styles
			if (!Directory.Exists(OutputPath + "/Chm"))
				Directory.CreateDirectory(OutputPath + "/Chm");
			if (!Directory.Exists(OutputPath + "/Chm/html"))
				Directory.CreateDirectory(OutputPath + "/Chm/html");
			if (!Directory.Exists(OutputPath + "/Chm/icons"))
				Directory.CreateDirectory(OutputPath + "/Chm/icons");
			if (!Directory.Exists(OutputPath + "/Chm/scripts"))
				Directory.CreateDirectory(OutputPath + "/Chm/scripts");
			if (!Directory.Exists(OutputPath + "/Chm/styles"))
				Directory.CreateDirectory(OutputPath + "/Chm/styles");

			DirectoryInfo dir = new DirectoryInfo(OutputPath + "/output/icons");
			FileInfo[] files = dir.GetFiles("*.*");
			foreach (FileInfo file in files)
				File.Copy(OutputPath + "/output/icons/" + file.Name, OutputPath + "/Chm/icons/" + file.Name, true);

			dir = new DirectoryInfo(OutputPath + "/output/scripts");
			files = dir.GetFiles("*.*");
			foreach (FileInfo file in files)
				File.Copy(OutputPath + "/output/scripts/" + file.Name, OutputPath + "/Chm/scripts/" + file.Name, true);

			dir = new DirectoryInfo(OutputPath + "/output/styles");
			files = dir.GetFiles("*.*");
			foreach (FileInfo file in files)
				File.Copy(OutputPath + "/output/styles/" + file.Name, OutputPath + "/Chm/styles/" + file.Name, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Build the HTML Help project files using the ChmBuilder utility.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void RunChmBuilder(ProcessStartInfo startInfo)
		{
			//ChmBuilder.exe /project:%2 /html:Output\html /lcid:1033 /toc:Toc.xml /out:Chm

			startInfo.FileName = string.Format("\"{0}/ProductionTools/ChmBuilder.exe\"",
				SandcastlePath);
			startInfo.Arguments = "/project:FieldWorks_Classes /html:Output/html";
			startInfo.Arguments += " /lcid:1033 /toc:toc.xml /out:Chm";
			Log(Level.Verbose, "{0} {1}", startInfo.FileName, startInfo.Arguments);

			Process proc = new Process();
			proc = Process.Start(startInfo);
			proc.WaitForExit();
			CaptureStdOut(proc, true);
			Log(Level.Verbose, "{0} Exit Code: {1}", DateTime.Now.ToString("T"), proc.ExitCode);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fix Unicode code point problems using the RunDBCSFix utility.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void RunDBCSFix(ProcessStartInfo startInfo)
		{
			//DBCSFix.exe /d:Chm /l:1033

			startInfo.FileName = string.Format("\"{0}/ProductionTools/DBCSFix.exe\"",
				SandcastlePath);
			startInfo.Arguments = string.Format(" /d:Chm /l:1033");
			Log(Level.Verbose, "{0} {1}", startInfo.FileName, startInfo.Arguments);

			Process proc = new Process();
			proc = Process.Start(startInfo);
			proc.WaitForExit();
			CaptureStdOut(proc, true);
			Log(Level.Verbose, "{0} Exit Code: {1}", DateTime.Now.ToString("T"), proc.ExitCode);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Build the CHM using the HTML Help compiler.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CompileChmFile(ProcessStartInfo startInfo)
		{
			//hhc Chm/%2.hhp

			startInfo.FileName = string.Format("\"{0}/hhc.exe\"", HHPath);
			startInfo.Arguments = "Chm/FieldWorks_Classes.hhp";
			Log(Level.Verbose, "{0} {1}", startInfo.FileName, startInfo.Arguments);

			Process proc = new Process();
			proc = Process.Start(startInfo);
			proc.WaitForExit();
			CaptureStdOut(proc, true);
			Log(Level.Verbose, "{0} Exit Code: {1}", DateTime.Now.ToString("T"), proc.ExitCode);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Capture Standard Output and optionally display it in the NAnt log.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CaptureStdOut(Process proc, bool show)
		{
			StreamReader sr = proc.StandardOutput;
			string output = sr.ReadToEnd();

			if (show)
				Log(Level.Verbose, output);

			sr.Close();
			sr.Dispose();
		}

		#endregion Private Instance Methods
	}
}
