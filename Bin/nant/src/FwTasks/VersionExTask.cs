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
using System.Security.Cryptography;
using System.Text;

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

			// Execute this task always, but don't always overwrite the output file - only
			// if something changed in the resulting file.
			Log(Level.Verbose, "Checking if we have to generate {0} from {1}", OutputFile.Name,
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

				string hashOfOldFile = null;
				if (File.Exists(OutputFile.FullName))
				{
					using (FileStream oldFileStream = File.OpenRead(OutputFile.FullName))
					{
						hashOfOldFile = ComputeHash(oldFileStream);
					}
				}
				// Modify file only if content really changed.
				using (MemoryStream memoryStream = new MemoryStream())
				{
					StreamWriter outStream = new StreamWriter(memoryStream);
					outStream.Write(fileContents);

					// Calculate hash
					string hashOfNewFile = ComputeHash(memoryStream);
					outStream.Close();

					if (hashOfNewFile != hashOfOldFile || ForceRebuild)
					{
						Log(Level.Info, "Generating {0} from {1}", OutputFile.Name,
							Path.GetFileName(template));

						StreamWriter outFileStream = new StreamWriter(OutputFile.FullName);
						outFileStream.Write(fileContents);
						outFileStream.Close();
					}
				}
			}
			catch(Exception e)
			{
				throw new BuildException("Generation failed.", Location, e);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Computes the hash value for the provided stream and returns a string with the
		/// hexadecimal hash value.
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <returns>Hash string</returns>
		/// ------------------------------------------------------------------------------------
		private string ComputeHash(Stream stream)
		{
			using (MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider())
			{
				byte[] hash = md5.ComputeHash(stream);

				StringBuilder sBuilder = new StringBuilder();

				// Loop through each byte of the hashed data
				// and format each one as a hexadecimal string.
				foreach (byte b in hash)
					sBuilder.Append(b.ToString("x2"));
				return sBuilder.ToString();
			}
		}
	}
}
