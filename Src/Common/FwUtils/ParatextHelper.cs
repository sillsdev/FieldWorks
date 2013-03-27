// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2006' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ParatextHelper.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Paratext;
using Paratext.DerivedTranslation;
using SIL.Utils;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.Common.FwUtils
{
	#region IParatextHelper interface
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Methods used to help access Paratext stuff
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IParatextHelper
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Paratext project directory or null if unable to get the project directory
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string ProjectsDirectory { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes the list of Paratext projects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void RefreshProjects();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reloads the specified Paratext project with the latest data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void ReloadProject(ScrText project);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the sorted list of Paratext short names.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IEnumerable<string> GetShortNames();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the list of Paratext projects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IEnumerable<ScrText> GetProjects();
	}
	#endregion

	#region ParatextHelper class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Helper methods used to access Paratext stuff. Tests can poke in a different
	/// implementation of IParatextHelper by setting s_ptHelper using reflection.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class ParatextHelper
	{
		private static IParatextHelper s_ptHelper = new ParatextHelperAdapter();

		#region ParatextHelper Manager class
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows setting a different IParatextHelper adapter (for testing purposes)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static class Manager
		{
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Sets the IParatextHelper adapter.
			/// </summary>
			/// <param name="adapter">The adapter.</param>
			/// --------------------------------------------------------------------------------
			public static void SetParatextHelperAdapter(IParatextHelper adapter)
			{
				s_ptHelper = adapter;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Resets the IParatextHelper adapter to the default adapter which accesses Paratext.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public static void Reset()
			{
				s_ptHelper = new ParatextHelperAdapter();
			}
		}
		#endregion

		#region ParatextHelperAdapter class
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Normal implementation of IParatextHelper that delegates to Paratext
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private class ParatextHelperAdapter : IParatextHelper
		{
			/// <summary/>
			public ParatextHelperAdapter()
			{
				try
				{
					ScrTextCollection.Initialize();
				}
				catch (Exception e)
				{
					Logger.WriteError(e);
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the Paratext project directory
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public string ProjectsDirectory
			{
				get
				{
					if (MiscUtils.IsUnix)
					{
						return Path.Combine(
							Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
							"MyParatextProjects");
					}

					try
					{
						return ScrTextCollection.SettingsDirectory;
					}
					catch (Exception e)
					{
						Logger.WriteError(e);
						return null;
					}
				}
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Refreshes the list of Paratext projects.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public void RefreshProjects()
			{
				try
				{
					ScrTextCollection.RefreshScrTexts();
				}
				catch (Exception e)
				{
					Logger.WriteError(e);
				}
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Reloads the specified Paratext project with the latest data.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public void ReloadProject(ScrText project)
			{
				if (project != null)
					project.Reload();
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the paratext short names.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public IEnumerable<string> GetShortNames()
			{
				try
				{
					return ScrTextCollection.ScrTextNames;
				}
				catch (Exception e)
				{
					Logger.WriteError(e);
					return new string[0];
				}
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the list of Paratext projects.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public IEnumerable<ScrText> GetProjects()
			{
				try
				{
					ScrTextCollection.RefreshScrTexts();
					return ScrTextCollection.ScrTexts;
				}
				catch (Exception e)
				{
					Logger.WriteError(e);
					return new ScrText[0];
				}
			}
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Paratext projects directory.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string ProjectsDirectory
		{
			get { return s_ptHelper.ProjectsDirectory; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the sorted list of paratext short names.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static IEnumerable<string> ShortNames
		{
			get
			{
				s_ptHelper.RefreshProjects();
				return s_ptHelper.GetShortNames();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writable paratext short names.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static IEnumerable<string> WritableShortNames
		{
			get
			{
				s_ptHelper.RefreshProjects();
				// Return a new list here just in case someone calls RefreshProjects() while
				// we are enumerating and the implementation of GetShortNames doesn't return a new list.
				return s_ptHelper.GetShortNames().Where(shortName => IsProjectWritable(shortName, false)).ToList();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the projects that have at least one book (restricted to the old and new
		/// testaments)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static IEnumerable<ScrText> ProjectsWithBooks
		{
			get
			{
				s_ptHelper.RefreshProjects();
				return s_ptHelper.GetProjects().Where(project => project.BooksPresentSet.SelectedBookNumbers().Any(
					bookNum => bookNum <= BCVRef.LastBook));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Paratext project, if any, that is associated with the specified FieldWorks
		/// project.
		/// </summary>
		/// <returns>The associated project, or null if there is none.</returns>
		/// ------------------------------------------------------------------------------------
		public static ScrText GetAssociatedProject(IProjectIdentifier projectId)
		{
			ScrText assocProj = s_ptHelper.GetProjects().FirstOrDefault(scrText =>
				scrText.AssociatedLexicalProject.ToString() == projectId.PipeHandle);
			s_ptHelper.ReloadProject(assocProj);
			return assocProj;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets any back translations of the specified base project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static IEnumerable<ScrText> GetBtsForProject(ScrText baseProj)
		{
			return s_ptHelper.GetProjects().Where(proj => proj.BaseTranslation.Is(baseProj.Name, baseProj.Guid) &&
				proj.BaseTranslation.Type == DerivedTranslationType.BackTranslation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns whether or not the project with the specified short name is abe to be
		/// written. Written being defined as editable and not a resource.
		/// </summary>
		/// <remarks>Returns true if the spcified project could not be found.</remarks>
		/// ------------------------------------------------------------------------------------
		public static bool IsProjectWritable(string projShortName)
		{
			return IsProjectWritable(projShortName, true);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a list of book IDs that exist for the given Paratext project.
		/// </summary>
		/// <returns>A List of integers representing 1-based canonical book numbers that exist
		/// in the given Paratext project</returns>
		/// <remark>The returned list will be empty if there is a problem with the Paratext
		/// installation or the specified project could not be found.</remark>
		/// ------------------------------------------------------------------------------------
		public static IEnumerable<int> GetProjectBooks(string projShortName)
		{
			try
			{
				ScrText foundText = s_ptHelper.GetProjects().FirstOrDefault(p => p.Name == projShortName);
				// Make sure we don't add books outside of our valid range
				if (foundText != null)
					return foundText.BooksPresentSet.SelectedBookNumbers().Where(book => book <= BCVRef.LastBook);
			}
			catch (Exception e)
			{
				Logger.WriteError(e);
				// ignore error - probably Paratext installation problem. Caller can check number
				// of books present.
			}

			return new int[0];
		}

		#endregion

		#region Private helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns whether or not the project with the specified short name is abe to be
		/// written. Written being defined as editable and not a resource.
		/// </summary>
		/// <remarks>Returns true if the spcified project could not be found.</remarks>
		/// ------------------------------------------------------------------------------------
		private static bool IsProjectWritable(string projShortName, bool fPerformRefresh)
		{
			if (ScrTextCollection.SLTTexts.Contains(projShortName.ToLowerInvariant()))
				return false;

			if (fPerformRefresh)
				s_ptHelper.RefreshProjects();

			ScrText existingProj = s_ptHelper.GetProjects().FirstOrDefault(
				x => x.Name.Equals(projShortName, StringComparison.InvariantCultureIgnoreCase));

			return existingProj == null || (string.IsNullOrEmpty(existingProj.AssociatedLexicalProject.ProjectId) &&
				existingProj.Editable && !existingProj.IsResourceText);
		}
		#endregion
	}
	#endregion
}
