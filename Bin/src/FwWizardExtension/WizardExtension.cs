using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.VisualStudio.TemplateWizard;
using EnvDTE;
using EnvDTE80;

namespace FwWizardExtension
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class WizardExtension: IWizard
	{
		private Window m_windowToActivate;
		#region IWizard Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Runs custom wizard logic before opening an item in the template.
		/// </summary>
		/// <param name="projectItem">The project item that will be opened.</param>
		/// ------------------------------------------------------------------------------------
		public void BeforeOpeningFile(EnvDTE.ProjectItem projectItem)
		{
			// see if there is also a test project. If so, we should open the file in that
			// as well.
			string projectName = projectItem.ContainingProject.Name;
			string unitTestFile = Path.Combine(Path.GetDirectoryName(projectItem.ContainingProject.FileName),
				Path.Combine(projectName + "Tests", projectName + "Tests.cs"));
			if (File.Exists(unitTestFile))
				m_windowToActivate = projectItem.DTE.ItemOperations.OpenFile(unitTestFile, Constants.vsViewKindTextView);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Runs custom wizard logic when a project has finished generating.
		/// </summary>
		/// <param name="project">The project that finished generating.</param>
		/// ------------------------------------------------------------------------------------
		public void ProjectFinishedGenerating(EnvDTE.Project project)
		{
			if (project == null)
				return;

			// Go through all project items and remove *.vspscc and *.user file (it has to be in
			// the project so that it gets added and properly named by the wizard)
			RemoveItemFromProject(project, project.Name + ".csproj.vspscc");
			RemoveItemFromProject(project, project.Name + ".csproj.user");

			// The project may contain items for a test project - we need to remove those and
			// add it as a separate project! This can't be done using a multi-project template,
			// because we want the projects to be nested.
			ArrayList itemsToRemove = new ArrayList();
			foreach (ProjectItem item in project.ProjectItems)
			{
				if (item.Name.StartsWith(project.Name) && item.ProjectItems.Count > 0)
					itemsToRemove.Add(item.Name);
			}
			foreach (string itemName in itemsToRemove)
				RemoveItemFromProject(project, itemName);
			if (itemsToRemove.Count > 0)
			{
				string newProject = Path.Combine(Path.GetDirectoryName(project.FileName),
					Path.Combine(project.Name + "Tests", project.Name + "Tests.csproj"));
				// Rename the temporary bla.csprojX file to bla.csproj
				File.Move(newProject + "X", newProject);
				// Add the test project to the solution
				project.DTE.Solution.AddFromFile(newProject, false);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the item from project.
		/// </summary>
		/// <param name="project">The project.</param>
		/// <param name="projectItem">The project item.</param>
		/// ------------------------------------------------------------------------------------
		private static void RemoveItemFromProject(EnvDTE.Project project, string projectItem)
		{
			try
			{
				ProjectItem item = project.ProjectItems.Item(projectItem);
				if (item != null)
					item.Remove();
			}
			catch (Exception e)
			{
				System.Diagnostics.Debug.WriteLine("Got exception trying to remove project item " +
					projectItem + ": " + e.Message);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Runs custom wizard logic when a project item has finished generating.
		/// </summary>
		/// <param name="projectItem">The project item that finished generating.</param>
		/// ------------------------------------------------------------------------------------
		public void ProjectItemFinishedGenerating(EnvDTE.ProjectItem projectItem)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Runs custom wizard logic when the wizard has completed all tasks.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RunFinished()
		{
			// Activate the unit test document if we have added a project + unit test project
			if (m_windowToActivate != null)
				m_windowToActivate.Activate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Runs custom wizard logic at the beginning of a template wizard run.
		/// </summary>
		/// <param name="automationObject">The automation object being used by the template
		/// wizard.</param>
		/// <param name="replacementsDictionary">The list of standard parameters to be
		/// replaced.</param>
		/// <param name="runKind">A <see cref="T:Microsoft.VisualStudio.TemplateWizard.WizardRunKind"/>
		/// indicating the type of wizard run.</param>
		/// <param name="customParams">The custom parameters with which to perform parameter
		/// replacement in the project.</param>
		/// ------------------------------------------------------------------------------------
		public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary,
			WizardRunKind runKind, object[] customParams)
		{
			DTE2 dte = automationObject as DTE2;

			if (replacementsDictionary.ContainsKey("$destinationdirectory$"))
			{
				string directory = replacementsDictionary["$destinationdirectory$"];
				replacementsDictionary.Add("$fwrootnamespace$", FwNameSpace(directory));
				replacementsDictionary.Add("$fwroot$", FindFwRoot(directory));
				string fwRoot = FwRoot(directory);
				replacementsDictionary.Add("$fwrelroot$", fwRoot);
				if (fwRoot.Length > 0)
					replacementsDictionary.Add("$fwoutput$", fwRoot + @"\Output");
				else
					replacementsDictionary.Add("$fwoutput$", "bin");
			}

			string username = Environment.GetEnvironmentVariable("FULL_USERNAME");
			if (username == null || username.Length == 0)
				username = Environment.GetEnvironmentVariable("USERNAME");
			replacementsDictionary.Add("$full_username$", username);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates whether the specified project item should be added to the project.
		/// </summary>
		/// <param name="filePath">The path to the project item.</param>
		/// <returns>
		/// true if the project item should be added to the project; otherwise, false.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool ShouldAddProjectItem(string filePath)
		{
			return true;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the FieldWorks name space.
		/// </summary>
		/// <param name="directory">The file path.</param>
		/// <returns>FieldWorks namespace</returns>
		/// ------------------------------------------------------------------------------------
		private string FwNameSpace(string directory)
		{
			// Get the FWROOT directory
			string fwRoot = FindFwRoot(directory);

			StringBuilder fwNameSpace = new StringBuilder("SIL.FieldWorks");
			if (fwRoot.Length > 0)
			{
				// now build the namespace similar to the directory structure. We skip the first
				// subdirectory, because that's SRC. Also, we have to check the last subdirectory.
				// If that ends with "Tests" we don't want to include that in the namespace.
				string[] subDirs = directory.Substring(fwRoot.Length + 1).Split('\\');
				int iLim = subDirs.Length;
				if (subDirs[iLim - 1].ToLower().EndsWith("tests"))
					iLim--;
				for (int i = 1; i < iLim; i++)
				{
					fwNameSpace.Append(".");
					fwNameSpace.Append(subDirs[i].Replace(' ', '_'));
				}
			}
			return fwNameSpace.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the relative path to the FieldWorks directory.
		/// </summary>
		/// <param name="directory">The directory.</param>
		/// <returns>The FieldWorks output directory, or "bin" if not under FW.</returns>
		/// ------------------------------------------------------------------------------------
		private string FwRoot(string directory)
		{
			// Get the FWROOT directory
			string fwRoot = FindFwRoot(directory);

			// now build the relative path to the output directory
			StringBuilder fwRootRelative = new StringBuilder();
			if (fwRoot.Length > 0)
			{
				string[] subDirs = directory.Substring(fwRoot.Length).Split('\\');
				for (int i = 1; i < subDirs.Length; i++)
				{
					if (fwRootRelative.Length > 0)
						fwRootRelative.Append(@"\");
					fwRootRelative.Append("..");
				}
				return fwRootRelative.ToString();
			}
			return string.Empty;
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
			string fwRoot = Environment.GetEnvironmentVariable("FWROOT");
			if (fwRoot != null && fwRoot.Length > 0)
				return fwRoot;

			string rawPath = pathToStartWith;
			if (rawPath.StartsWith(@"file:\"))
				rawPath = rawPath.Substring(6);
			while (rawPath.Length > 0 && !File.Exists(Path.Combine(rawPath, @"bld\FieldWorks.build")))
			{
				if (rawPath == Path.GetPathRoot(rawPath))
				{
					// we are already in the top level directory - can't go further up.
					break;
				}
				rawPath = Path.GetFullPath(Path.Combine(rawPath, ".."));
			}
			if (File.Exists(Path.Combine(rawPath, @"bld\FieldWorks.build")))
			{
				return Path.GetFullPath(rawPath);
			}
			return string.Empty;
		}

	}
}
