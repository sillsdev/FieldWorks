// NAnt - A .NET build tool
// Copyright (C) 2001-2002 Gerry Shaw
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
//
// Gerry Shaw (gerry_shaw@yahoo.com)
// Scott Hernandez (ScottHernandez@hotmail.com)

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Xml;

using NAnt.Core.Attributes;

namespace NAnt.Core.Tasks
{
	/// <summary>
	/// Runs NAnt on a supplied build file. This can be used to build subprojects.
	/// </summary>
	/// <example>
	///   <para>Build the BuildServer project located in a different directory but only if the <c>debug</c> property is not true.</para>
	///   <code><![CDATA[<nant unless="${debug}" buildfile="${src.dir}/Extras/BuildServer/BuildServer.build"/>]]></code>
	/// </example>
	[TaskName("nantex")]
	public class NAntExTask : NAntTask
	{
		// named properties that were borrowed from Project.
		// The are 'internal' to the main NAnt dll, so are not available here.
		internal const string NAntPlatform = "nant.platform";
		internal const string NAntPlatformName = NAntPlatform + ".name";
		internal const string NAntPropertyFileName = "nant.filename";
		internal const string NAntPropertyVersion = "nant.version";
		internal const string NAntPropertyLocation = "nant.location";
		internal const string NAntPropertyProjectName = "nant.project.name";
		internal const string NAntPropertyProjectBuildFile = "nant.project.buildfile";
		internal const string NAntPropertyProjectBaseDir = "nant.project.basedir";
		internal const string NAntPropertyProjectDefault = "nant.project.default";
		internal const string NAntPropertyOnSuccess = "nant.onsuccess";
		internal const string NAntPropertyOnFailure = "nant.onfailure";

		private string m_passByRef = string.Empty;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Properties that are passed to the called build file and are passed back, i.e.
		/// modified in this build file. Comparable to passing parameters by reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("passbyref")]
		public string PassByRef
		{
			get { return m_passByRef; }
			set { m_passByRef = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do it! - identical to that in NantTask.cs, but unfortunately NantTask.RunBuild is
		/// private so that we can't override it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ExecuteTask()
		{
			// run the build file specified in an attribute
			if (BuildFile != null)
			{
				RunBuild(BuildFile);
			}
			else
			{
				if (BuildFiles.FileNames.Count == 0)
				{
					Log(Level.Warning, "No matching build files found to run.");
					return;
				}

				// run all build files specified in the fileset
				foreach (string buildFile in BuildFiles.FileNames)
				{
					RunBuild(new FileInfo(buildFile));
				}
			}
		}

		private void RunBuild(FileInfo buildFile)
		{
			Log(Level.Info, "{0} {1}", buildFile.FullName, DefaultTarget);

			// create new project with same threshold as current project and
			// increased indentation level, and initialize it using the same
			// configuration node
			Project project = new Project(buildFile.FullName, Project.Threshold,
				Project.IndentationLevel + 1, Project.ConfigurationNode);

			// add listeners of current project to new project
			project.AttachBuildListeners(Project.BuildListeners);

			// have the new project inherit the runtime framework from the
			// current project
			if (Project.RuntimeFramework != null && project.Frameworks.Contains(Project.RuntimeFramework.Name))
			{
				project.RuntimeFramework = project.Frameworks[Project.RuntimeFramework.Name];
			}

			// have the new project inherit the current framework from the
			// current project
			if (Project.TargetFramework != null && project.Frameworks.Contains(Project.TargetFramework.Name))
			{
				project.TargetFramework = project.Frameworks[Project.TargetFramework.Name];
			}

			// have the new project inherit properties from the current project
			if (InheritAll)
			{
				StringCollection excludes = new StringCollection();
				excludes.Add(NAntPropertyFileName);
				excludes.Add(NAntPropertyLocation);
				excludes.Add(NAntPropertyOnSuccess);
				excludes.Add(NAntPropertyOnFailure);
				excludes.Add(NAntPropertyProjectBaseDir);
				excludes.Add(NAntPropertyProjectBuildFile);
				excludes.Add(NAntPropertyProjectDefault);
				excludes.Add(NAntPropertyProjectName);
				excludes.Add(NAntPropertyVersion);
				project.Properties.Inherit(Properties, excludes);
			}

			// add/overwrite properties
			foreach (PropertyTask property in OverrideProperties)
			{
				// expand properties in context of current project for non-dynamic
				// properties
				if (!property.Dynamic)
				{
					property.Value = Project.ExpandProperties(property.Value, Location);
				}
				property.Project = project;
				property.Execute();
			}

			if (InheritRefs)
			{
				// pass datatypes thru to the child project
				project.DataTypeReferences.Inherit(Project.DataTypeReferences);
			}

			// handle multiple targets
			if (DefaultTarget != null)
			{
				foreach (string t in DefaultTarget.Split(' '))
				{
					string target = t.Trim();
					if (target.Length > 0)
					{
						project.BuildTargets.Add(target);
					}
				}
			}

			try
			{
				// run the given build
				if (!project.Run())
				{
					throw new BuildException("Nested build failed.  Refer to build log for exact reason.");
				}
			}
			finally
			{
				if (PassByRef.Length > 0)
				{
					foreach (string p in PassByRef.Split(' '))
					{
						if (p != null)
						{
							string property = p.Trim();
							if (property.Length > 0 && project.Properties[property] != null)
								Properties[property] = project.Properties[property];
						}
					}
				}
			}
		}
	}
}