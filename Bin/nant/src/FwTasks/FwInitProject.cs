// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwInitProject
// Responsibility:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.IO;
using NAnt.Core;
using NAnt.Core.Tasks;
using NAnt.Core.Attributes;

namespace SIL.FieldWorks.Build.Tasks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Initialize project variable with name of *.??proj file for use with VS-convert
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TaskName("initproj")]
	public class FwInitProject: Task
	{
		private bool PropertyExists(string name)
		{
			return Project.Properties[name] != null && Project.Properties[name] != string.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do the job
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ExecuteTask()
		{
			if (!PropertyExists("dir.srcProj"))
			{
				Project.Properties["dir.srcProj"] = Directory.GetCurrentDirectory();
			}
			if (Project.Properties["filename.srcProject"] == null
				|| Project.Properties["filename.srcProject"] == string.Empty)
			{
				// look for candidate project files in the source directory
				string[] filelist = Directory.GetFiles(Project.Properties["dir.srcProj"], "*.??proj");
				if (filelist.Length == 1)
				{
					Project.Properties["filename.srcProject"] = filelist[0];
				}
				else if (filelist.Length == 0)
				{
					throw new BuildException("Can't find any projects in directory "
						+ Project.Properties["dir.srcProj"]);
				}
				else
				{
					throw new BuildException("More than one project in directory "
						+ Project.Properties["dir.srcProj"]);
				}
			}
			string srcProject = Project.Properties["filename.srcProject"];
			if (!PropertyExists("filename.destBuild"))
			{
				Project.Properties["filename.destBuild"] = Path.Combine(Project.Properties["dir.nantbuild"],
					Path.GetFileNameWithoutExtension(srcProject) + ".build");
			}

			Project.Properties["projectname"] = Path.GetFileNameWithoutExtension(srcProject);
			Project.Properties["projectext"] = Path.GetExtension(srcProject).Substring(1); // extension without "."
		}
	}
}
