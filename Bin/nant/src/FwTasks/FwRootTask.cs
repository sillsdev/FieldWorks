// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwRootTask.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using NAnt.Core;
using NAnt.Core.Tasks;
using NAnt.Core.Attributes;

namespace SIL.FieldWorks.Build.Tasks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Set the FwRoot property if not already set.
	/// </summary>
	/// <example>
	/// <para>Set the fwroot property. If set use the environment variable FWROOT, otherwise
	/// calculate the base directory from the directory where the build file is located.</para>
	/// <code><![CDATA[
	/// <fwroot test="bld/SetupInclude.xml"/>
	/// ]]></code>
	/// </example>
	/// ----------------------------------------------------------------------------------------
	[TaskName("fwroot")]
	public class FwRootTask: Task
	{
		private string m_TestFile;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The name of the file that must be found in the given relative position to fwroot.
		/// </summary>
		/// <remarks>If the environment variable FWROOT is not set, the fwroot task looks in
		/// the directory of the current build file for the test file. If it is not found there
		/// it looks in the parent directory and so on.</remarks>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("test")]
		public string TestFile
		{
			get { return m_TestFile; }
			set { m_TestFile = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do the job
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ExecuteTask()
		{
			try
			{
				if (Properties["fwroot"] == null || Properties["fwroot"] == string.Empty)
				{
					string fwroot = Environment.GetEnvironmentVariable("FWROOT");
					if (fwroot == null || fwroot == string.Empty)
					{
						// first look in the buildfile directory and its parent directories
						fwroot = FindFwRoot(Project.BuildFileLocalName);
						if (fwroot.Length == 0)
						{
							// this didn't work, so look in the directory where NAnt.exe is
							// and its parent directories
							fwroot = FindFwRoot(Properties["nant.filename"]);
							if (fwroot.Length == 0)
							{
								// this didn' work, so just take the directory we know
								// is fwroot at least sometimes...
								string rawPath = Path.GetDirectoryName(Path.Combine(Properties["nant.filename"],
									"../../.."));
								if (rawPath.StartsWith(@"file:\"))
									rawPath = rawPath.Substring(6);
								fwroot = Path.GetFullPath(rawPath);
							}
						}
					}
					Properties.AddReadOnly("fwroot", fwroot);
				}
			}
			catch (Exception e)
			{
				throw new BuildException(
					string.Format("Error {0} setting property 'fwroot'", e.Message),
					Location, e);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the fwroot directory
		/// </summary>
		/// <param name="pathToStartWith">Path that we use to start the search</param>
		/// <returns>The fwroot, or <c>string.Empty</c> if not found.</returns>
		/// ------------------------------------------------------------------------------------
		private string FindFwRoot(string pathToStartWith)
		{
			string rawPath = Path.GetDirectoryName(pathToStartWith);
			if (rawPath.StartsWith(@"file:\"))
				rawPath = rawPath.Substring(6);
			while (rawPath.Length > 0 && !File.Exists(Path.Combine(rawPath, TestFile)))
			{
				if (rawPath == Path.GetPathRoot(rawPath))
				{
					// we are already in the top level directory - can't go further up.
					break;
				}
				rawPath = Path.GetFullPath(Path.Combine(rawPath, ".."));
			}
			if (File.Exists(Path.Combine(rawPath, TestFile)))
			{
				return Path.GetFullPath(rawPath);
			}
			return string.Empty;
		}
	}
}
