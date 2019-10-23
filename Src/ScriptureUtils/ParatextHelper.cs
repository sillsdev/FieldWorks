// Copyright (c) 2006-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Core.Scripture;
using SIL.Reporting;

namespace SIL.FieldWorks.Common.ScriptureUtils
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
		/// Refreshes the list of Paratext projects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void RefreshProjects();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reloads the specified Paratext project with the latest data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void ReloadProject(IScrText project);

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
		IEnumerable<IScrText> GetProjects();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the mappings for a Paratext 6/7 project into the specified list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void LoadProjectMappings(IScrImportSet importSettings);
	}

	/// <summary/>
	public interface IScrText
	{
		/// <summary/>
		void Reload();

		/// <summary/>
		IScriptureProviderStyleSheet DefaultStylesheet { get; }

		/// <summary/>
		IScriptureProviderParser Parser { get;  }

		/// <summary/>
		IScriptureProviderBookSet BooksPresentSet { get;  }

		/// <summary/>
		string Name { get; set; }

		/// <summary/>
		ILexicalProject AssociatedLexicalProject { get; set; }

		/// <summary/>
		ITranslationInfo TranslationInfo { get; set; }

		/// <summary/>
		bool Editable { get; set; }

		/// <summary/>
		bool IsResourceText { get; }

		/// <summary/>
		string Directory { get; }

		/// <summary/>
		string BooksPresent { get; set; }

		/// <summary/>
		IScrVerse Versification { get; }

		/// <summary/>
		string JoinedNameAndFullName { get; }

		/// <summary/>
		string FileNamePrePart { get; }

		/// <summary/>
		string FileNameForm { get; }

		/// <summary/>
		string FileNamePostPart { get; }

		/// <summary/>
		object CoreScrText { get; }

		/// <summary/>
		void SetParameterValue(string resourcetext, string s);

		/// <summary/>
		bool BookPresent(int bookCanonicalNum);

		/// <summary/>
		bool IsCheckSumCurrent(int bookCanonicalNum, string checksum);

		/// <summary/>
		string GetBookCheckSum(int canonicalNum);
	}

	/// <summary/>
	public interface ILexicalProject
	{
		/// <summary/>
		string ProjectId { get; }

		/// <summary/>
		string ProjectType { get; }
	}

	/// <summary/>
	public interface ITranslationInfo
	{
		/// <summary/>
		string BaseProjectName { get; }

		/// <summary/>
		ProjectType Type { get; }
	}

	/// <summary/>
	public interface IScriptureProviderBookSet
	{
		/// <summary/>
		IEnumerable<int> SelectedBookNumbers { get; }
	}

	/// <summary>
	/// </summary>
	public interface IScriptureProviderParser
	{
		/// <summary>
		/// </summary>
		/// <param name="verseRef"></param>
		/// <param name="b"></param>
		/// <param name="b1"></param>
		/// <returns></returns>
		IEnumerable<IUsfmToken> GetUsfmTokens(IVerseRef verseRef, bool b, bool b1);
	}

	/// <summary/>
	public interface IUsfmToken
	{
		/// <summary/>
		string Marker { get; }

		/// <summary/>
		string EndMarker { get; }

		/// <summary/>
		TokenType Type { get; }

		/// <summary>To use in places where the type is known in a specific implementation to call methods not exposed through the interface</summary>
		object CoreToken { get; }

		/// <summary/>
		string Text { get; }
	}

	/// <summary/>
	public interface IVerseRef
	{
		/// <summary/>
		int BookNum { get; set; }

		/// <summary/>
		int ChapterNum { get; }

		/// <summary/>
		object CoreVerseRef { get; }

		/// <summary/>
		int VerseNum { get; }

		/// <summary/>
		string Segment();

		/// <summary/>
		IEnumerable<IVerseRef> AllVerses(bool v);
	}

	/// <summary/>
	public interface IScriptureProviderStyleSheet
	{
		/// <summary/>
		IEnumerable<ITag> Tags { get; }
	}

	/// <summary/>
	public interface ITag
	{
		/// <summary/>
		string Marker { get; set; }
		/// <summary/>
		string Endmarker { get; set; }
		/// <summary/>
		ScrStyleType StyleType { get; set; }
		/// <summary/>
		bool IsScriptureBook { get; }
	}

	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Helper methods used to access Paratext stuff. Tests can poke in a different
	/// implementation of IParatextHelper by using the internal Manager class.
	/// </summary>
	/// <remarks>
	/// ENHANCE (TimS): This class should somehow make it's way into FwUtils or similar.
	/// The only reason it hasn't been moved already is because LoadProjecMappings depends
	/// on ScrMappingList and ImportDomain which are currently defined in LCM. Those classes
	/// could, theoretically, also be moved, but I didn't feel like it. :p
	/// Enhance (response, Hasso, 2013.09): LoadProjectMappings also depends on a couple other classes, some of which
	/// depend on LcmGenerate, and thus cannot be removed from LCM without removing those dependencies.
	/// Enhance (response, TimS, 2013.09): I think the idea was to get rid LCM's direct dependency on ParatextShared.dll.
	/// This could be accomplished by moving the remaining method to ParatextHelper. The way this could be done would be
	/// to factor out the Paratext code and move it to ParatextHelper (i.e. the code that gets the marker/endmarker
	/// combinations and determining which markers are actually in-use). The code that uses the ParatextProxy could then
	/// just get this information directly from the ParatextHelper instead and ParatextProxy could cease to exist.
	/// Enhance (response, Hasso, 2013.09): too much effort and risk for now, given we want a stable release this month;
	/// however, combining Paratext functionality into one class so initialization is in one place (LT-14887)
	/// REVIEW (Haso, 2017.07): once again, we're pushing for stable soon. We recently implemented a new ScriptureProvider class which does some
	/// (and could possibly do all) of what PTHelper[Adapter] did.
	/// </remarks>
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
				RefreshProjects(); // REVIEW (Hasso) 2017.07: I don't think we need to do this; it is called before each time it is needed.
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
					if (ScriptureProvider.IsInstalled)
					{
						if (!m_IsParatextInitialized)
						{
							// It is possible that the Projects directory was not available when we first initialized
							// ScrTextCollection, but it now is (e.g. USB drive plugged or unplugged).  So we initialize
							// again. ScrTextCollection.Initialize is safe to call multiple times and also refreshes texts.
							// We pass the directory (rather than passing no arguments, and letting the paratext dll figure
							// it out) because the figuring out goes wrong on Linux, where both programs are simulating
							// the registry in different places. TODO NOT (Hasso) 2017.07
							ScriptureProvider.Initialize();
							m_IsParatextInitialized = true;
						}
						else
						{
							ScriptureProvider.RefreshScrTexts();
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
			public void ReloadProject(IScrText project)
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
					return ScriptureProvider.ScrTextNames;
				}
				return new string[0];
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the list of Paratext projects.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public IEnumerable<IScrText> GetProjects()
			{
				RefreshProjects();

				if (m_IsParatextInitialized)
				{
					try
					{
						// The booleans say we are including resources (translations etc that are part of the Paratext release)
						// and non-Scripture items (not sure what these are).
						// Most likely neither of these are necessary, but I'm preserving the behavior we had with 7.3,
						// which did not have these arguments.
						// We also filter out invalid ScrTexts, because there is a bug in Paratext that allows them to get through.
						return ScriptureProvider.ScrTexts().Where(st => Directory.Exists(st.Directory));
					}
					catch (Exception e)
					{
						Logger.WriteError(e);
						m_IsParatextInitialized = false;
					}
				}
				return new IScrText[0];
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Load the mappings for a Paratext 6/7 project into the specified list.
			/// </summary>
			/// <returns><c>true</c> if the Paratext mappings were loaded successfully; <c>false</c>
			/// otherwise</returns>
			/// ------------------------------------------------------------------------------------
			public void LoadProjectMappings(IScrImportSet importSettings)
			{
				RefreshProjects();
				if (!m_IsParatextInitialized)
				{
					importSettings.ParatextScrProj = null;
					importSettings.ParatextBTProj = null;
					importSettings.ParatextNotesProj = null;
					return;
				}

				if (!LoadProjectMappings(importSettings.ParatextScrProj, importSettings.GetMappingListForDomain(ImportDomain.Main), ImportDomain.Main))
					importSettings.ParatextScrProj = null;

				if (!LoadProjectMappings(importSettings.ParatextBTProj, importSettings.GetMappingListForDomain(ImportDomain.BackTrans), ImportDomain.BackTrans))
					importSettings.ParatextBTProj = null;

				if (!LoadProjectMappings(importSettings.ParatextNotesProj, importSettings.GetMappingListForDomain(ImportDomain.Annotations), ImportDomain.Annotations))
					importSettings.ParatextNotesProj = null;
			}

			private bool LoadProjectMappings(string project, ScrMappingList mappingList, ImportDomain domain)
			{
				// If the new project ID is null, then do not load mappings.
				if (string.IsNullOrEmpty(project))
					return false;

				// Load the tags from the paratext project and create mappings for them.
				IScrText scParatextText;
				try
				{
					// ParatextShared has a static collection that is responsible for the dispose of any IScrText objects
					scParatextText = ScriptureProvider.Get(project);
				}
				catch (Exception ex)
				{
					Logger.WriteError(ex);
					m_IsParatextInitialized = false;
					return false;
				}

				foreach (ImportMappingInfo mapping in mappingList)
					mapping.SetIsInUse(domain, false);
				try
				{
					foreach (var tag in scParatextText.DefaultStylesheet.Tags)
					{
						if (tag == null)
							break;
						string marker = @"\" + tag.Marker;
						string endMarker = string.Empty;
						if (!string.IsNullOrEmpty(tag.Endmarker))
							endMarker = @"\" + tag.Endmarker;

						// When the nth marker has an end marker, the nth + 1 marker will be
						// that end marker. Therefore, we have to skip those "end style" markers.
						if (tag.StyleType == ScrStyleType.scEndStyle)
							continue;

						// Create a new mapping for this marker.
						mappingList.AddDefaultMappingIfNeeded(marker, endMarker, domain, false, false);
					}
					var parser = scParatextText.Parser;
					foreach (int bookNum in scParatextText.BooksPresentSet.SelectedBookNumbers)
					{
						foreach (var token in parser.GetUsfmTokens(ScriptureProvider.MakeVerseRef(bookNum, 0, 0), false, true))
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
		public static IEnumerable<IScrText> ProjectsWithBooks
		{
			get
			{
				s_ptHelper.RefreshProjects();
				return s_ptHelper.GetProjects().Where(project =>
					project.BooksPresentSet.SelectedBookNumbers.Any(bookNum => bookNum <= BCVRef.LastBook));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Paratext project, if any, that is associated with the specified FieldWorks project.
		/// </summary>
		/// <returns>The associated project, or null if there is none.</returns>
		/// ------------------------------------------------------------------------------------
		public static IScrText GetAssociatedProject(IProjectIdentifier projectId)
		{
			var assocProj = s_ptHelper.GetProjects().FirstOrDefault(scrText =>
				scrText.AssociatedLexicalProject.ToString() == projectId.PipeHandle);
			s_ptHelper.ReloadProject(assocProj);
			return assocProj;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets any back translations of the specified base project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static IEnumerable<IScrText> GetBtsForProject(IScrText baseProj)
		{
			// We're looking for projects that are back translations of baseProj. That means they have type
			// back translation, and their base project is the one we want.
			// Seems that baseProj.Equals(proj.BaseScrText) should work, but it doesn't, at least in one unit test,
			// possibly just because the mock is not simulating the real helper well enough.
			return s_ptHelper.GetProjects().Where(proj => baseProj.Name.Equals(proj.TranslationInfo.BaseProjectName) &&
													proj.TranslationInfo.Type == ProjectType.BackTranslation);
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
		public static IEnumerable<int> GetProjectBooks(string projShortName) // REVIEW (Hasso) 2017.06: is this (and everything else) obsoleted by ScrProvider?
		{
			try
			{
				var foundText = s_ptHelper.GetProjects().FirstOrDefault(p => p.Name == projShortName);
				// Make sure we don't add books outside of our valid range
				if (foundText != null)
					return foundText.BooksPresentSet.SelectedBookNumbers.Where(book => book <= BCVRef.LastBook);
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
		/// Load the mappings for a Paratext 6/7 project into the specified import settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void LoadProjectMappings(IScrImportSet importSettings)
		{
			s_ptHelper.LoadProjectMappings(importSettings);
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
			if (ScriptureProvider.NonWorkingTexts.Contains(projShortName.ToLowerInvariant()))
				return false;

			if (fPerformRefresh)
				s_ptHelper.RefreshProjects();

			var existingProj = s_ptHelper.GetProjects().FirstOrDefault(x =>
				x.Name.Equals(projShortName, StringComparison.InvariantCultureIgnoreCase));

			return existingProj == null || (string.IsNullOrEmpty(existingProj.AssociatedLexicalProject.ProjectId) &&
											existingProj.Editable && !existingProj.IsResourceText);
		}
		#endregion
	}
	/// <summary/>
	public enum ScrStyleType
	{
		/// <summary/>
		scUnknownStyle,
		/// <summary/>
		scCharacterStyle,
		/// <summary/>
		scNoteStyle,
		/// <summary/>
		scParagraphStyle,
		/// <summary/>
		scEndStyle
	}

	/// <summary/>
	public enum ProjectType
	{
		/// <summary/>
		Standard,
		/// <summary/>
		Resource,
		/// <summary/>
		BackTranslation,
		/// <summary/>
		Daughter,
		/// <summary/>
		Transliteration,
		/// <summary/>
		StudyBible,
		/// <summary/>
		GlobalConsultantNotes,
		/// <summary/>
		Auxiliary,
		/// <summary/>
		AuxiliaryResource,
		/// <summary/>
		MarbleResource,
		/// <summary/>
		TransliterationWithEncoder,
		/// <summary/>
		ConsultantNotes,
		/// <summary/>
		GlobalAnthropologyNotes,
		/// <summary/>
		StudyBibleAdditions,
		/// <summary/>
		Unknown
	}

	/// <summary/>
	public enum TokenType
	{
		/// <summary/>
		Book,
		/// <summary/>
		Chapter,
		/// <summary/>
		Verse,
		/// <summary/>
		Text,
		/// <summary/>
		Paragraph,
		/// <summary/>
		Character,
		/// <summary/>
		Note,
		/// <summary/>
		End,
		/// <summary/>
		Unknown,
	}
}
