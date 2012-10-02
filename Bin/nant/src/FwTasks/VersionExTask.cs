// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: VersionTask.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Specialized;
using System.IO;
using System.Text.RegularExpressions;
using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Tasks;

namespace SIL.FieldWorks.Build.Tasks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Generate a file from a template and expand variables.
	/// </summary>
	/// <example>
	/// <para>Generate the file <c>GeneratedAssemblyInfo.cs</c> from the file <c>AssemblyInfo.cs
	/// </c>. Generation will happen if <c>GeneratedAssemblyInfo.cs</c> doesn't exist, or if one
	/// of <c>AssemblyInfo.cs</c>, <c>ArrayPtr.cs</c> or <c>DbAccessInterop.dll</c> have
	/// changed.</para>
	/// <code><![CDATA[
	/// <version output="${dir.srcProj}\GeneratedAssemblyInfo.cs" template="${dir.srcProj}\AssemblyInfo.cs">
	///     <sources basedir="${dir.srcProj}">
	///			<includes name="ArrayPtr.cs" />
	///		</sources>
	///		<references basedir="${dir.srcProj}">
	///			<includes name="${dir.buildOutput}/DbAccessInterop.dll" />
	///		</references>
	/// </version>
	/// ]]></code>
	/// </example>
	/// ----------------------------------------------------------------------------------------
	[TaskName("versionex")]
	public class VersionExTask: FwBaseTask
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="VersionExTask"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public VersionExTask()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The name of the template file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string Template
		{
			get
			{
				string template = Sources.FileNames[0];
				if (template == null)
					throw new BuildException("Missing template file.");
				return template;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the file Extension required by the current compiler
		/// </summary>
		/// <returns>Returns the file Extension required by the current compiler</returns>
		/// ------------------------------------------------------------------------------------
		public override string Extension
		{
			get { return "cs"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do the generation
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ExecuteTask()
		{
			base.ExecuteTask();

			string template = Template;

			// Execute this task always
			Log(Level.Info, "Generating {0} from {1}", OutputFile.Name,
				Path.GetFileName(template));

			try
			{
				StreamReader stream = new StreamReader(template);
				string fileContents = stream.ReadToEnd();
				stream.Close();

				Regex regex = new Regex("\\$YEAR");
				fileContents = regex.Replace(fileContents, DateTime.Now.Year.ToString());

				regex = new Regex("\\$MONTH");
				fileContents = regex.Replace(fileContents, string.Format("{0:MM}", DateTime.Now));

				regex = new Regex("\\$DAY");
				fileContents = regex.Replace(fileContents, string.Format("{0:dd}", DateTime.Now));

				regex = new Regex("\\$NUMBEROFDAYS");
				fileContents = regex.Replace(fileContents,
					Convert.ToInt32(Math.Truncate(DateTime.Now.ToOADate())).ToString());

				regex = new Regex("\\$!((?<env>\\w+)|\\{(?<env>\\w+):(?<default>\\w+)\\})");
				Match match = regex.Match(fileContents);
				while (match.Success)
				{
					string strEnv = match.Result("${env}");
					string strDefault = match.Result("${default}");
					string strEnvValue = Project.Properties[strEnv];
					if (strEnvValue != null && strEnvValue != string.Empty)
						fileContents = regex.Replace(fileContents, strEnvValue, 1, match.Index);
					else
						fileContents = regex.Replace(fileContents, strDefault, 1, match.Index);

					match = regex.Match(fileContents);
				}

				fileContents = string.Format("// This file is generated from {0}. Do NOT modify!\n{1}",
					Path.GetFileName(template), fileContents);

				StreamWriter outStream = new StreamWriter(OutputFile.FullName);
				outStream.Write(fileContents);
				outStream.Close();
			}
			catch(Exception e)
			{
				throw new BuildException("Generation failed.", Location, e);
			}
		}
	}
}
