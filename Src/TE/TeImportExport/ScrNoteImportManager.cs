// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrNoteImportManager.cs
// Responsibility: DavidO
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Cellar;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.TE.TeEditorialChecks;
using SIL.FieldWorks.FDO.LangProj;
using System.Diagnostics;
using System.Globalization;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class ScrNoteImportManager
	{
		private static Dictionary<ScrScriptureNote.ScrNoteKey, ScrScriptureNote> s_existingAnnotations;
		private static Dictionary<string, Guid> s_checkNamesToGuids = null;
		private static RfcWritingSystem s_rfcWs;
		private static ScrBookAnnotations s_annotationList;
		private static IScripture s_scr;
		private static int s_prevBookNum = 0;
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

			if (s_rfcWs == null)
				s_rfcWs = new RfcWritingSystem(s_scr.Cache, false);

			if (bookNum != s_prevBookNum)
			{
				s_prevBookNum = bookNum;
				s_annotationList = (ScrBookAnnotations)scr.BookAnnotationsOS[bookNum - 1];
				s_existingAnnotations = new Dictionary<ScrScriptureNote.ScrNoteKey, ScrScriptureNote>();

				foreach (ScrScriptureNote ann in s_annotationList.NotesOS)
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
			s_rfcWs = null;
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
		public static ScrScriptureNote FindOrCreateAnnotation(ScrAnnotationInfo info,
			Guid annotatedObjGuid)
		{
			ScrScriptureNote ann;

			// If an identical note is not found...
			if (!s_existingAnnotations.TryGetValue(info.Key, out ann))
			{
				using (new SuppressSubTasks(s_scr.Cache))
				{
					int hvo = s_scr.Cache.GetIdFromGuid(annotatedObjGuid);
					ICmObject annotatedObj = hvo <= 0 ? null :
						CmObject.CreateFromDBObject(s_scr.Cache, hvo);
					ann = (ScrScriptureNote)s_annotationList.InsertImportedNote(
						info.startReference, info.endReference, annotatedObj, annotatedObj,
						info.guidAnnotationType, null);
				}

				if (s_scr.Cache.ActionHandlerAccessor != null)
					s_scr.Cache.ActionHandlerAccessor.AddAction(new UndoImportObjectAction(ann));

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
				InstalledScriptureChecks.GetChecks(new ScrChecksDataSource(s_scr.Cache));

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
					return LangProject.kguidAnnConsultantNote;
				case "translatorNote":
					return LangProject.kguidAnnTranslatorNote;
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
			return s_rfcWs.GetWsFromRfcLang(locale, s_alternateRfcWsDir);
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
