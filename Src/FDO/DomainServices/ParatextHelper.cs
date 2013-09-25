// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2013, SIL International. All Rights Reserved.
// <copyright from='2006' to='2013' company='SIL International'>
//		Copyright (c) 2013, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ParatextHelper.cs
// Responsibility: FieldWorks Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Paratext;
using Paratext.DerivedTranslation;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.FDO.DomainServices
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the mappings for a Paratext 6/7 project into the specified list.
		/// </summary>
		/// <param name="project">Paratext project ID</param>
		/// <param name="mappingList">ScrMappingList to which new mappings will be added</param>
		/// <param name="domain">The import domain for which this project is the source</param>
		/// <returns><c>true</c> if the Paratext mappings were loaded successfully; <c>false</c>
		/// otherwise</returns>
		/// ------------------------------------------------------------------------------------
		bool LoadProjectMappings(string project, ScrMappingList mappingList, ImportDomain domain);
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Helper methods used to access Paratext stuff. Tests can poke in a different
	/// implementation of IParatextHelper by using the internal Manager class.
	/// ENHANCE (TimS): This class should somehow make it's way into FwUtils or similar.
	/// The only reason it hasn't been moved already is because LoadProjecMappings depends
	/// on ScrMappingList and ImportDomain which are currently defined in FDO. Those classes
	/// could, theoretically, also be moved, but I didn't feel like it. :p
	/// Enhance (response, Hasso, 2013.09): LoadProjectMappings also depends on a couple other classes, some of which
	/// depend on FDOGenerate, and thus cannot be removed from FDO without removing those dependencies.
	/// Enhance (response, TimS, 2013.09): I think the idea was to get rid FDO's direct dependency on ParatextShared.dll.
	/// This could be accomplished by moving the remaining method to ParatextHelper. The way this could be done would be
	/// to factor out the Paratext code and move it to ParatextHelper (i.e. the code that gets the marker/endmarker
	/// combinations and determining which markers are actually in-use). The code that uses the ParatextProxy could then
	/// just get this information directly from the ParatextHelper instead and ParatextProxy could cease to exist.
	/// Enhance (response, Hasso, 2013.09): too much effort and risk for now, given we want a stable release this month;
	/// however, combining Paratext functionality into one class so initialization is in one place (LT-14887)
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
			/// Resets the IParatextHelper adapter to a new instance of the default adapter which accesses Paratext.
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
			private bool m_IsParatextInitialized;

			/// <summary/>
			public ParatextHelperAdapter()
			{
				RefreshProjects();
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the Paratext projects directory (null if none)
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public string ProjectsDirectory
			{
				get
				{
					if (m_IsParatextInitialized)
					{
						if (MiscUtils.IsUnix)
						{
							// TODO FWNX-1235: Why does SrcTextCollection.SettingsDirectory not work in Unix?
							// Does ScrTextCollection work at all in Unix?
							return FwRegistryHelper.ParatextSettingsDirectory();
						}
						return ScrTextCollection.SettingsDirectory;
					}
					return null;
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
					if (FwRegistryHelper.ParatextSettingsDirectoryExists())
					{
						if (!m_IsParatextInitialized)
						{
							// It is possible that the Projects directory was not available when we first initialized
							// ScrTextCollection, but it now is (e.g. USB drive plugged or unplugged).  So we initialize
							// again. ScrTextCollection.Initialize is safe to call multiple times and also refreshes texts.
							ScrTextCollection.Initialize();
							m_IsParatextInitialized = true;
						}
						else
						{
							ScrTextCollection.RefreshScrTexts();
						}
					}
					else
					{
						m_IsParatextInitialized = false;
					}
				}
				catch (Exception e)
				{
					Logger.WriteError(e);
					m_IsParatextInitialized = false;
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
				if (m_IsParatextInitialized)
				{
					return ScrTextCollection.ScrTextNames;
				}
				return new string[0];
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the list of Paratext projects.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public IEnumerable<ScrText> GetProjects()
			{
				RefreshProjects();

				if (m_IsParatextInitialized)
				{
					try
					{
						return ScrTextCollection.ScrTexts;
					}
					catch (Exception e)
					{
						Logger.WriteError(e);
						m_IsParatextInitialized = false;
					}
				}
				return new ScrText[0];
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Load the mappings for a Paratext 6/7 project into the specified list.
			/// </summary>
			/// <param name="project">Paratext project ID</param>
			/// <param name="mappingList">ScrMappingList to which new mappings will be added</param>
			/// <param name="domain">The import domain for which this project is the source</param>
			/// <returns><c>true</c> if the Paratext mappings were loaded successfully; <c>false</c>
			/// otherwise</returns>
			/// ------------------------------------------------------------------------------------
			[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
				Justification = "See REVIEW comment")]
			public bool LoadProjectMappings(string project, ScrMappingList mappingList, ImportDomain domain)
			{
				RefreshProjects();

				// If Paratext is not initialized or the new project ID is null, then do not load mappings.
				if (!m_IsParatextInitialized || project == null)
					return false;

				// Load the tags from the paratext project and create mappings for them.
				ScrText scParatextText;
				try
				{
					// REVIEW (EberhardB): I'm not sure if ScrTextCollection.Get() returns a
					// reference to a ScrText or a new object (in which case we would have to
					// call Dispose() on it)
					scParatextText = ScrTextCollection.Get(project);
				}
				catch (Exception ex)
				{
					Logger.WriteError(ex);
					m_IsParatextInitialized = false;
					return false;
				}

				mappingList.ResetInUseFlags(domain);
				try
				{
					foreach (ScrTag tag in scParatextText.DefaultStylesheet.Tags)
					{
						if (tag == null)
							break;
						string marker = @"\" + tag.Marker;
						string endMarker = string.Empty;
						if (!String.IsNullOrEmpty(tag.Endmarker))
							endMarker = @"\" + tag.Endmarker;

						// When the nth marker has an end marker, the nth + 1 marker will be
						// that end marker. Therefore, we have to skip those "end style" markers.
						if (tag.StyleType == ScrStyleType.scEndStyle)
							continue;

						// Create a new mapping for this marker.
						mappingList.AddDefaultMappingIfNeeded(marker, endMarker, domain, false, false);
					}
					ScrParser parser = scParatextText.Parser();
					foreach (int bookNum in scParatextText.BooksPresentSet.SelectedBookNumbers())
					{
						foreach (UsfmToken token in parser.GetUsfmTokens(new VerseRef(bookNum, 0, 0), false, true))
						{
							if (token.Marker == null)
								continue; // Tokens alternate between text and marker types

							ImportMappingInfo mapping = mappingList[@"\" + token.Marker];
							if (mapping != null)
								mapping.SetIsInUse(domain, true);
						}
					}
				}
				catch (Exception ex)
				{
					Logger.WriteError(ex);
					// A lot goes on in the try block, so this exception doesn't necessarily mean Paratext is inaccessible,
					// so don't mark Paratext as uninitialized
					return false;
				}
				return true;
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
		/// Gets the projects that have at least one book (restricted to the old and new testaments)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static IEnumerable<ScrText> ProjectsWithBooks
		{
			get
			{
				s_ptHelper.RefreshProjects();
				return s_ptHelper.GetProjects().Where(project =>
					project.BooksPresentSet.SelectedBookNumbers().Any(bookNum => bookNum <= BCVRef.LastBook));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Paratext project, if any, that is associated with the specified FieldWorks project.
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
				// ignore error - probably Paratext installation problem. Caller can check number of books present.
			}

			return new int[0];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the mappings for a Paratext 6/7 project into the specified list.
		/// </summary>
		/// <param name="project">Paratext project ID</param>
		/// <param name="mappingList">ScrMappingList to which new mappings will be added</param>
		/// <param name="domain">The import domain for which this project is the source</param>
		/// <returns><c>true</c> if the Paratext mappings were loaded successfully; <c>false</c>
		/// otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public static bool LoadProjectMappings(string project, ScrMappingList mappingList, ImportDomain domain)
		{
			return s_ptHelper.LoadProjectMappings(project, mappingList, domain);
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

			ScrText existingProj = s_ptHelper.GetProjects().FirstOrDefault(x =>
				x.Name.Equals(projShortName, StringComparison.InvariantCultureIgnoreCase));

			return existingProj == null || (string.IsNullOrEmpty(existingProj.AssociatedLexicalProject.ProjectId) &&
											existingProj.Editable && !existingProj.IsResourceText);
		}
		#endregion
	}
}
