// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003-2009, SIL International. All Rights Reserved.
// <copyright from='2003' to='2009' company='SIL International'>
//		Copyright (c) 2003-2009, SIL International. All Rights Reserved.
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
using NAnt.Core.Attributes;
using System.Reflection;

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
	/// <fwroot test="Bld/SetupInclude.xml"/>
	/// ]]></code>
	/// </example>
	/// ----------------------------------------------------------------------------------------
	[TaskName("fwroot")]
	public class FwRootTask: Task
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The name of the file that must be found in the given relative position to fwroot.
		/// </summary>
		/// <remarks>If the environment variable FWROOT is not set, the fwroot task looks in
		/// the directory of the current build file for the test file. If it is not found there
		/// it looks in the parent directory and so on.</remarks>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("test")]
		public string TestFile { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Wether or not to convert the fwroot path to a unix path (i.e. using forward slash
		/// as directory separator)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("unixPath")]
		public bool UseUnixPath { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do the job
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ExecuteTask()
		{
			try
			{
				if (string.IsNullOrEmpty(Properties["fwroot"]))
				{
					Log(Level.Debug, "Checking environment variable FWROOT");
					string fwroot = Environment.GetEnvironmentVariable("FWROOT");
					if (string.IsNullOrEmpty(fwroot))
					{
						Log(Level.Debug, "Checking based on build file: '{0}'", Project.BuildFileLocalName);
						// first look in the buildfile directory and its parent directories
						fwroot = FindFwRoot(Project.BuildFileLocalName);
						if (fwroot.Length == 0)
						{
							// this didn't work, so look in the directory where NAnt.exe is
							// and its parent directories
							string nantFileName = Assembly.GetEntryAssembly().CodeBase;
							if (nantFileName.StartsWith("file://"))
							{
								// Strip file:/// (on Windows) or file:// (on Linux) (cf FWNX-117)
								nantFileName = nantFileName.Substring(Path.DirectorySeparatorChar == '\\' ? 8 : 7);
							}
							Log(Level.Debug, "Checking based on NAnt executable: '{0}'", nantFileName);
							fwroot = FindFwRoot(nantFileName);
							if (fwroot.Length == 0)
							{
								Log(Level.Debug,
									"All else failed; falling back on hard coded path relativ to NAnt.exe");
								// this didn't work, so just take the directory we know
								// is fwroot at least sometimes...
								string rawPath = Path.GetDirectoryName(Path.Combine(nantFileName,
									"../../.."));
								fwroot = Path.GetFullPath(rawPath);
							}
						}
					}
					if (UseUnixPath)
						fwroot = fwroot.Replace('\\', '/');
					Log(Level.Verbose, "Setting property fwroot to '{0}'", fwroot);
					Properties.AddReadOnly("fwroot", fwroot);
				}
				else
					Log(Level.Verbose, "fwroot is already set");
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
			if (pathToStartWith.StartsWith("file://"))
			{
				// Strip file:/// (on Windows) or file:// (on Linux) (cf FWNX-117)
				pathToStartWith = pathToStartWith.Substring(Path.DirectorySeparatorChar == '\\' ? 8 : 7);
			}
			string rawPath = Path.GetDirectoryName(pathToStartWith);

			while (rawPath.Length > 0 && !File.Exists(Path.Combine(rawPath, TestFile)))
			{
				Log(Level.Debug, "\tChecking rawPath: '{0}'", rawPath);
				if (rawPath == Path.GetPathRoot(rawPath))
				{
					// we are already in the top level directory - can't go further up.
					Log(Level.Debug, "\tAt top level directory and still not found - can't go further up");
					break;
				}
				rawPath = Path.GetFullPath(Path.Combine(rawPath, ".."));
			}
			if (File.Exists(Path.Combine(rawPath, TestFile)))
				return Path.GetFullPath(rawPath);
			return string.Empty;
		}
	}
}
