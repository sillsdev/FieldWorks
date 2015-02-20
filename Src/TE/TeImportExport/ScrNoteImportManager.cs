// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ScrNoteImportManager.cs
// Responsibility: DavidO
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;

using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Resources;
using SIL.WritingSystems;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.TE.TeEditorialChecks;
using System.Diagnostics;
using System.Globalization;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class ScrNoteImportManager
	{
		private static Dictionary<ScrNoteKey, IScrScriptureNote> s_existingAnnotations;
		private static Dictionary<string, Guid> s_checkNamesToGuids = null;
		private static IScrBookAnnotations s_annotationList;
		private static IScripture s_scr;
		private static int s_prevBookNum = 0;
		// TODO WS: how should this be used in the new world?
		private static string s_alternateRfcWsDir;

		#region Initialization and cleanup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the scripture note import manager.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void Initialize(IScripture scr, int bookNum)
		{
			Initialize(scr, bookNum, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the scripture note import manager.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void Initialize(IScripture scr, int bookNum, string alternateRfcWsDir)
		{
			s_scr = scr;

			if (!string.IsNullOrEmpty(alternateRfcWsDir))
				s_alternateRfcWsDir = alternateRfcWsDir;

			CacheCheckIds();

			if (bookNum != s_prevBookNum)
			{
				s_prevBookNum = bookNum;
				s_annotationList = scr.BookAnnotationsOS[bookNum - 1];
				s_existingAnnotations = new Dictionary<ScrNoteKey, IScrScriptureNote>();

				foreach (IScrScriptureNote ann in s_annotationList.NotesOS)
					s_existingAnnotations[ann.Key] = ann;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cleanups the scripture note import manager.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void Cleanup()
		{
			s_existingAnnotations = null;
			s_checkNamesToGuids = null;
			s_annotationList = null;
			s_prevBookNum = 0;
			s_scr = null;
		}
		#endregion

		#region Methods for finding/creating annotations
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds or creates an annotation.
		/// </summary>
		/// <param name="info">The information about a annotation being imported.</param>
		/// <param name="annotatedObjGuid">The annotated obj GUID.</param>
		/// <returns>
		/// The annotation (whether created or found)
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static IScrScriptureNote FindOrCreateAnnotation(ScrAnnotationInfo info,
			Guid annotatedObjGuid)
		{
			IScrScriptureNote ann;

			// If an identical note is not found...
			if (!s_existingAnnotations.TryGetValue(info.Key, out ann))
			{
				ICmObject annotatedObj = null;
				if (annotatedObjGuid != Guid.Empty)
					s_scr.Cache.ServiceLocator.GetInstance<ICmObjectRepository>().TryGetObject(annotatedObjGuid, out annotatedObj);
				ann = s_annotationList.InsertImportedNote(info.startReference, info.endReference,
					annotatedObj, annotatedObj,	info.guidAnnotationType, null);

				ann.BeginOffset = ann.EndOffset = info.ichOffset;

				if (ann.CitedText != null)
					ann.EndOffset += ann.CitedText.Length;
			}

			return ann;
		}

		#endregion

		#region Methods for getting annotation types
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Ensure that the error checking annotation subtypes are defined.
		/// </summary>
		/// <remarks>This may need to be made more flexible somehow.</remarks>
		/// -----------------------------------------------------------------------------------
		private static void CacheCheckIds()
		{
			if (s_checkNamesToGuids != null)
				return;

			// This creates the annotation types for installed checks.
			SortedList<ScrCheckKey, IScriptureCheck> chks =
				InstalledScriptureChecks.GetChecks(new ScrChecksDataSource(s_scr.Cache,
					ResourceHelper.GetResourceString("kstidPunctCheckWhitespaceChar"), FwDirectoryFinder.LegacyWordformingCharOverridesFile));

			if (chks != null)
			{
				s_checkNamesToGuids = new Dictionary<string, Guid>(chks.Count);
				foreach (IScriptureCheck check in chks.Values)
					s_checkNamesToGuids[check.CheckName] = check.CheckId;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the guid of the relevant AnnotationType.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public static Guid GetAnnotationTypeGuid(string sType, string subType)
		{
			switch (sType)
			{
				default:
				case "consultantNote":
					return CmAnnotationDefnTags.kguidAnnConsultantNote;
				case "translatorNote":
					return CmAnnotationDefnTags.kguidAnnTranslatorNote;
				case "pre-typesettingCheck":
					Guid guid;
					// The new way to identify Scripture checking annotations in OXES is by their normal
					// names, so we'll try to look them up that way first.
					if (s_checkNamesToGuids != null && s_checkNamesToGuids.TryGetValue(subType, out guid))
						return guid;

					try
					{
						// Attempt to interpret the subtype as a string-representation of a
						// GUID (which is the fall-back if no name is available)
						guid = new Guid(subType);
						return guid;
					}
					catch
					{
						// No match, so try the old special check names
						switch (subType)
						{
							case "chapterVerseCheck":
								return StandardCheckIds.kguidChapterVerse;
							case "characterCheck":
								return StandardCheckIds.kguidCharacters;
							case "matchedPairsCheck":
								return StandardCheckIds.kguidMatchedPairs;
							case "mixedCapitalizationCheck":
								return StandardCheckIds.kguidMixedCapitalization;
							case "punctuationCheck":
								return StandardCheckIds.kguidPunctuation;
							case "repeatedWordsCheck":
								return StandardCheckIds.kguidRepeatedWords;
							case "capitalizationCheck":
								return StandardCheckIds.kguidCapitalization;
							case "quotationCheck":
								return StandardCheckIds.kguidQuotations;
							default:
								Debug.Fail("OXES import does not recognize the editorial check: " + subType);
								return Guid.Empty;
						}
					}
			}
		}

		#endregion

		#region Misc. methods
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Find or create the writing system code for the given RFC4646 language tag. If it's
		/// not in either the list of vernacular writing systems or the list of analysis
		/// writing systems, add it to the list of analysis writing systems.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public static int GetWsForLocale(string locale)
		{
			string identifier = IetfLanguageTagHelper.ToIetfLanguageTag(locale);
			WritingSystem ws;
			if (s_scr.Cache.ServiceLocator.WritingSystemManager.TryGetOrSet(identifier, out ws))
			{
				if (!s_scr.Cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Contains(ws))
					s_scr.Cache.ServiceLocator.WritingSystems.AddToCurrentAnalysisWritingSystems(ws);
				return ws.Handle;
			}

			throw new UnknownPalasoWsException(identifier + " is an unknown RFC5646 language tag.",
				locale, identifier);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Parse the date/time string, assuming it to be in Universal Time (UTC).
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public static DateTime ParseUniversalTime(string sDateTime)
		{
			DateTime dt;
			if (DateTime.TryParse(sDateTime, DateTimeFormatInfo.InvariantInfo,
				DateTimeStyles.AssumeUniversal, out dt))
			{
				return dt;
			}

			// REVIEW: report time format problem??
			return DateTime.Now.ToUniversalTime();
		}

		#endregion
	}
}
