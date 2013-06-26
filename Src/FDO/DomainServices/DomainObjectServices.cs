// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2009' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DomainObjectServices.cs
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.Utils;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Infrastructure;
using SILUBS.SharedScrUtils;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainImpl;

namespace SIL.FieldWorks.FDO.DomainServices
{
	#region ScriptureServices class
	/// <summary>
	/// Scripture services
	/// </summary>
	public static class ScriptureServices
	{
		/// <summary>Book marker</summary>
		public static readonly string kMarkerBook = @"\id";
		/// <summary>Delegate to report a non-fatal "warning" message</summary>
		private static Action<string> ReportWarning;

		internal const char kKeyTokenSeparator = '\uffff';

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the <see cref="ScriptureServices"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static ScriptureServices()
		{
			InitializeWarningHandler();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the delegate for reporting warnings.
		/// </summary>
		/// <remarks>This needs to be a separate method because it is called by reflection in
		/// test(s).</remarks>
		/// ------------------------------------------------------------------------------------
		private static void InitializeWarningHandler()
		{
			ReportWarning = sMsg =>
			{
				ErrorReporter.ReportException(new Exception(sMsg), null, null, null, false);
			};
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the import source key for a hashtable.
		/// </summary>
		/// <param name="wsId">The writing system identifier.</param>
		/// <param name="noteType">Type of the note.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		internal static string CreateImportSourceKey(string wsId, ICmAnnotationDefn noteType)
		{
			return wsId + kKeyTokenSeparator +
				(noteType == null ? string.Empty : noteType.Guid.ToString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the import source key for a hashtable.
		/// </summary>
		/// <param name="importDomain">Used as the key if the ICU locale is null</param>
		/// <param name="wsId">The writing system identifier.</param>
		/// <param name="noteType">Type of the note.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		internal static object CreateImportSourceKey(ImportDomain importDomain,
			string wsId, ICmAnnotationDefn noteType)
		{
			if (wsId == null)
				return importDomain;
			return CreateImportSourceKey(wsId, noteType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates whether the given StText belongs to scripture or not.
		/// Enhance JohnT: this may need to be public beyond FDO, in which case, it needs to move.
		/// </summary>
		/// <param name="stText"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static bool ScriptureIsResponsibleFor(IStText stText)
		{
			if (stText == null)
				return false;
			return stText.IsValidObject && IsScriptureTextFlid(stText.OwningFlid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the book with the specified canonical number in the UI writing
		/// system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string GetUiBookName(int bookNum)
		{
			return ScrFdoResources.ResourceManager.GetString("kstidBookName" + bookNum);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given the flid that owns an StText, answer true if it is part of Scripture.
		/// </summary>
		/// <param name="owningFlid">The owning flid.</param>
		/// ------------------------------------------------------------------------------------
		internal static bool IsScriptureTextFlid(int owningFlid)
		{
			return owningFlid == ScrSectionTags.kflidHeading ||
				   owningFlid == ScrBookTags.kflidFootnotes ||
				   owningFlid == ScrSectionTags.kflidContent ||
				   owningFlid == ScrBookTags.kflidTitle;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the default marker domain for the given style name
		/// </summary>
		/// <param name="styleSheet">style sheet to use for determining the domain</param>
		/// <param name="styleName">The style name</param>
		/// <returns>the default marker domain</returns>
		/// ------------------------------------------------------------------------------------
		internal static MarkerDomain GetDefaultDomainForStyle(IVwStylesheet styleSheet,
			string styleName)
		{
			try
			{
				// Footnote target ref and marker styles are always footnote domain
				if (styleName == ScrStyleNames.FootnoteTargetRef ||
					styleName == ScrStyleNames.FootnoteMarker)
					return MarkerDomain.Footnote;

				// Chapter and verse numbers are always Default domain
				if (styleName == ScrStyleNames.ChapterNumber ||
					styleName == ScrStyleNames.VerseNumber)
					return MarkerDomain.Default;

				if (styleSheet == null)
				{
					// REVIEW: JohnW - Seems like this should not be valid since we won't get
					//         the mapping right. Tried putting a Debug.Fail here to see if
					//         I could eliminate this check, but tests will have to be
					//         changed and the code to update existing settings will need
					//         to be changed.
					return MarkerDomain.Default;
				}

				// Base the domain on the style context
				switch ((ContextValues)styleSheet.GetContext(styleName))
				{
					case ContextValues.Note:
						return MarkerDomain.Footnote;
					case ContextValues.Annotation:
						return MarkerDomain.Note;
					default:
						return MarkerDomain.Default;
				}
			}
			catch
			{
				// This is probably in a test, so we don't care. (Or maybe something really
				// bad happened, like the style was deleted or renamed.)
				return MarkerDomain.Default;
			}
		}

		/// <summary>
		/// Return a string form of the reference for the specified offset in the specified paragraph.
		/// </summary>
		public static string FullScrRef(IScrTxtPara scrPara, int offset, string bookName)
		{
			FdoCache cache = scrPara.Cache;
			BCVRef startRef, endRef;
			scrPara.GetRefsAtPosition(offset, out startRef, out endRef);
			IScripture scripture = cache.LangProject.TranslatedScriptureOA;
			string fullRef = ScrReference.MakeReferenceString(bookName, startRef, endRef,
				scripture.ChapterVerseSepr, scripture.Bridge);
			return fullRef;

		}
		/// <summary>
		/// Compute the label that should be added to the verse reference for the indexed segment of the
		/// specified paragraph, assuming it is part of Scripture. Assumes the indexed segment is not itself
		/// a verse/chapter label. The idea is to add 'a', 'b', 'c' etc. if there is more than one segment
		/// in the same verse.
		/// </summary>
		/// <returns></returns>
		public static string VerseSegLabel(IScrTxtPara para, int idxSeg)
		{
			IStTxtPara curPara = para;
			int idxCurSeg = idxSeg;
			int cprev = 0; // number of previous segments in same verse
			while (GetPrevSeg(ref idxCurSeg, ref curPara))
			{
				if (curPara.SegmentsOS[idxCurSeg].IsLabel)
					break; // some sort of verse or chapter ID, previous seg will have different ref.
				cprev++;
			}
			if (cprev == 0)
			{
				// See if the FOLLOWING segment is a label. We don't care how many following segments there
				// are, except that since there are no previous ones, if there are also no following ones
				// we don't need a label at all and can return an empty string.
				curPara = para;
				idxCurSeg = idxSeg;
				if (!GetNextSeg(ref idxCurSeg, ref curPara))
					return string.Empty; // no more segments, and no previous ones in same verse.

				if (curPara.SegmentsOS[idxCurSeg].IsLabel)
					return string.Empty; // some sort of verse or chapter ID, next seg will have different ref.

			}
			return MakeLabelForSegAtIndex(cprev);

		}

		private static string MakeLabelForSegAtIndex(int cprev)
		{
			return Convert.ToChar(Convert.ToInt32('a') + cprev).ToString();
		}

		/// <summary>
		/// Given that idxCurSeg is the index of a segment of curPara, adjust both until it is the index of the
		/// previous segment (possibly in an earlier paragraph). Return false if no earlier segment exists in
		/// the current text.
		/// </summary>
		private static bool GetPrevSeg(ref int idxCurSeg, ref IStTxtPara curPara)
		{
			IStText text = (IStText)curPara.Owner;
			idxCurSeg--;
			// This while usually has no iterations, and all we do in the method is decrement idxCurSeg.
			// It is even rarer to have more than one iteration but could happen if there is an empty paragraph.
			while (idxCurSeg < 0)
			{
				// set curPara to previous para in StText. If there is none, fail.
				int idxPara = curPara.IndexInOwner;
				if (idxPara == 0)
					return false;
				IStTxtPara prevPara = text[idxPara - 1];
				curPara = prevPara;
				idxCurSeg = curPara.SegmentsOS.Count - 1;
			}
			return true;
		}
		/// <summary>
		/// Given that idxCurSeg is the index of a segment of curPara, adjust both until it is the index of the
		/// next segment (possibly in a later paragraph). Return false if no earlier segment exists in
		/// the current text.
		/// </summary>
		private static bool GetNextSeg(ref int idxCurSeg, ref IStTxtPara curPara)
		{
			IStText text = (IStText)curPara.Owner;
			idxCurSeg++;
			// This for usually exits early in the first iteration, and all we do in the method is increment idxCurSeg.
			// It is even rarer to have more than one full iteration but could happen if there is an empty paragraph.
			while (true)
			{
				int csegs = curPara.SegmentsOS.Count;
				if (idxCurSeg < csegs)
					return true;
				// set curPara to next para in StText. If there is none, fail.
				int idxPara = curPara.IndexInOwner;
				int cpara = text.ParagraphsOS.Count;
				if (idxPara >= cpara - 1)
					return false;
				IStTxtPara nextPara = text[idxPara + 1];
				curPara = nextPara;
				idxCurSeg = 0;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the main CmTranslation from the segmented translation. Basically concatenates
		/// all of the segments to create the CmTranslation.
		/// </summary>
		/// <param name="para">The paragraph to update.</param>
		/// <param name="wss">A list of analysis writing systems to update.</param>
		/// ------------------------------------------------------------------------------------
		internal static void UpdateMainTransFromSegmented(IStTxtPara para, params int[] wss)
		{
			if (!para.IsValidObject || wss.Length == 0)
				return; // in merge, paragraph may be modified then deleted.
			FdoCache cache = para.Cache;
			SegmentServices.EnsureMainParaSegments(para);
			ISilDataAccess sda = cache.MainCacheAccessor;
			IScripture scr = para.Cache.LangProject.TranslatedScriptureOA;
			ICmTranslation originalBT = para.GetBT(); // Can be null
			foreach (int ws in wss)
			{
				ITsTextProps wsOnlyProps = StyleUtils.CharStyleTextProps(null, ws);
				ITsStrBldr bldr = TsStrBldrClass.Create();
				bool wantNextSpace = false; // suppresses space before the first thing we add.
				bool haveBtText = false; // Text that isn't segment label text
				bool lastSegWasLabel = false;
				foreach (ISegment seg in para.SegmentsOS)
				{
					// If it's a label, insert it directly. Suppress following space.
					ITsString tssFt;
					// Whether we want to insert a space before the current segment is determined by the previous one.
					// Save that value so we can set wantSpace appropriately for the following one.
					bool wantSpace = wantNextSpace;
					if (seg.IsLabel)
					{
						if (lastSegWasLabel)
							wantSpace = true; // Need spaces between adjacent label segments

						tssFt = seg.BaselineText;
						tssFt = scr.ConvertCVNumbersInStringForBT(CorrectFootnotes(tssFt), ws);
						// Because the baseline text is in the vernacular WS, we need to change
						// it to the writing system of the free translation we are processing.
						ITsStrBldr ftBldr = tssFt.GetBldr();
						ftBldr.SetIntPropValues(0, ftBldr.Length, (int)FwTextPropType.ktptWs, 0, ws);
						tssFt = ftBldr.GetString();
						wantNextSpace = false;
						lastSegWasLabel = true;
					}
					else
					{
						tssFt = seg.FreeTranslation.get_String(ws);
						haveBtText |= (tssFt.Length > 0);
						wantNextSpace = (tssFt.Length > 0 && EndsWithNonSpace(tssFt));
						if (tssFt.Length > 0) // Only change lastSegWasLabel if we have some text
							lastSegWasLabel = false;
					}
					if (tssFt.Length > 0)
					{
						if (wantSpace)
						{
							// The preceding segment should typically be followed by a space.
							if (!StartsWithSpaceOrOrc(tssFt))
								bldr.Replace(bldr.Length, bldr.Length, " ", wsOnlyProps);
						}
						bldr.ReplaceTsString(bldr.Length, bldr.Length, tssFt);
					}
				}

				// If the back translation doesn't have text, we don't want to create verse
				// segment labels. This prevents the problem where the book thinks it has a
				// back translation because of automatically generated verse labels (TE-8283).
				if (!haveBtText)
					continue;

				ITsString newFt = bldr.GetString();
				ICmTranslation trans;
				if (newFt.Length == 0)
				{
					trans = para.GetBT();
					if (trans == null)
						return; // don't bother creating one to store an empty translation!
				}
				else
				{
					trans = para.GetOrCreateBT();

				}
				// Don't write unless it changed...PropChanged can be expensive.
				if (!trans.Translation.get_String(ws).Equals(newFt))
					trans.Translation.set_String(ws, newFt);
			}
		}

		/// <summary>
		/// True if a string starts with a space or ORC and so does not require a space inserted before it.
		/// (Also if it is empty...don't need to put a space if we aren't going to put something after it.)
		/// </summary>
		/// <param name="tssFt"></param>
		/// <returns></returns>
		private static bool StartsWithSpaceOrOrc(ITsString tssFt)
		{
			if (tssFt.Length == 0)
				return true;
			char first = tssFt.GetChars(0, 1)[0];
			return first == StringUtils.kChObject || first == ' ';
		}

		/// <summary>
		/// True if the string ends with a non-space (and so needs a space inserted after it).
		/// </summary>
		/// <param name="tssFt"></param>
		/// <returns></returns>
		private static bool EndsWithNonSpace(ITsString tssFt)
		{
			int length = tssFt.Length;
			if (length == 0)
				return true;
			return tssFt.GetChars(length - 1, length) != " ";
		}

		/// <summary>
		/// Any ORCs in the given string with ktptObjData of type kodtOwnNameGuidHot (meaning a GUID that
		/// 'owns' the footnote) should be changed to kodtNameGuidHot, since the BT does NOT own
		/// the footnote.
		/// </summary>
		/// <param name="tssFt"></param>
		/// <returns></returns>
		private static ITsString CorrectFootnotes(ITsString tssFt)
		{
			ITsStrBldr bldr = null;
			int crun = tssFt.RunCount;
			for (int iRun = 0; iRun < crun; iRun++)
			{
				string sOrc = tssFt.get_RunText(iRun);
				if (String.IsNullOrEmpty(sOrc))
					continue;
				if (StringUtils.kChObject != sOrc[0])
					continue;
				ITsTextProps orcPropsParaFootnote = tssFt.get_Properties(iRun);
				string objData = orcPropsParaFootnote.GetStrPropValue((int)FwTextPropType.ktptObjData);
				if (String.IsNullOrEmpty(objData))
					continue;
				if ((char)(int)FwObjDataTypes.kodtOwnNameGuidHot != objData[0])
					continue;
				// OK, need to fix it.
				if (bldr == null)
					bldr = tssFt.GetBldr();
				objData = ((char)(int)FwObjDataTypes.kodtNameGuidHot).ToString() + objData.Substring(1);
				bldr.SetStrPropValue(tssFt.get_MinOfRun(iRun), tssFt.get_LimOfRun(iRun), (int)FwTextPropType.ktptObjData, objData);
			}
			return bldr == null ? tssFt : bldr.GetString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjust the footnotes in the archived book.
		/// </summary>
		/// <param name="book">The original book</param>
		/// <param name="archivedBook">The saved version 9i.e., archived draft)</param>
		/// ------------------------------------------------------------------------------------
		internal static void AdjustObjectsInArchivedBook(IScrBook book, IScrBook archivedBook)
		{
			ITsString tss;
			ITsStrBldr strBldr;
			ITsTextProps ttp;
			ITsPropsBldr propsBldr;
			int iFootnote = 0;
			var pictureRepo = book.Cache.ServiceLocator.GetInstance<ICmPictureRepository>();
			foreach (IScrTxtPara revPara in archivedBook.Paragraphs)
			{
				//TODO: TE-5082 Duplicate code! The following should call or use common code with
				// StTxtPara.CreateOwnedObjects()
				bool fChanged = false;
				tss = revPara.Contents;
				strBldr = tss.GetBldr();
				int cRun = tss.RunCount;
				TsRunInfo tri;
				byte[] objData;
				for (int iRun = 0; iRun < cRun; iRun++)
				{
					if (!tss.get_IsRunOrc(iRun))
						continue;
					// Examine this run to see if it is an owned ORC
					FwObjDataTypes odt;
					Guid guid = TsStringUtils.GetOwnedGuidFromRun(tss, iRun, out odt, out tri, out ttp);
					if (guid != Guid.Empty)
					{
						switch (odt)
						{
							case FwObjDataTypes.kodtOwnNameGuidHot:
								// Should be a footnote. Make sure it's the next one in the original sequence.
								if (guid != book.FootnotesOS[iFootnote].Guid)
								{
									int iActualFootnote = -1;
									for (int i = iFootnote + 1; i < book.FootnotesOS.Count; i++)
									{
										if (book.FootnotesOS[i].Guid == guid)
										{
											iActualFootnote = i;
											break;
										}
									}
									if (iActualFootnote == -1)
									{
										// This is an owned ORC with no corresponding footnote or with a footnote that
										// has already been hooked up to an earlier ORC. This should never happen.
										BCVRef startRef, endRef;
										revPara.GetRefsAtPosition(tss.get_MinOfRun(iRun), out startRef, out endRef);
										string sMsg = "Footnote in " +
											BCVRef.MakeReferenceString(startRef, endRef, ":", "-") + " with guid " + guid +
											" does not have a corresponding footnote object owned by " + book.BookId +
											" or refers to a footnote that is owned by another ORC that occurs earlier.";
										ReportWarning(sMsg);
										break;
									}
									Logger.WriteEvent("Footnotes out of order in " + book.BookId + ". Expected footnote with guid " + guid +
										" at position " + iFootnote + ", but found it at position " + iActualFootnote + ".");
									archivedBook.FootnotesOS.Insert(iFootnote, archivedBook.FootnotesOS[iActualFootnote]);
								}
								// adjust the owned ORC
								Guid revGuid = archivedBook.FootnotesOS[iFootnote].Guid;
								objData = TsStringUtils.GetObjData(revGuid, (byte)FwObjDataTypes.kodtOwnNameGuidHot);
								propsBldr = ttp.GetBldr();
								propsBldr.SetStrPropValueRgch(
									(int)FwTextPropType.ktptObjData,
									objData, objData.Length);
								strBldr.SetProperties(tri.ichMin, tri.ichLim,
									propsBldr.GetTextProps());
								// Look thru back translation and adjust ref ORCs for this footnote there
								AdjustBtFootnoteInArchivedBook(book.FootnotesOS[iFootnote].Guid,
									revGuid, revPara);
								fChanged = true;
								iFootnote++;
								break;

							case FwObjDataTypes.kodtGuidMoveableObjDisp:
								// Get the original picture info
								ICmPicture picture = pictureRepo.GetObject(guid);

								//update the new picture info
								ICmPicture newPicture = CopyObject<ICmPicture>.CloneFdoObject(picture, null);

								// update the ORC in the revision to point to the new picture
								objData = TsStringUtils.GetObjData(newPicture.Guid,
									(byte)FwObjDataTypes.kodtGuidMoveableObjDisp);
								propsBldr = ttp.GetBldr();
								propsBldr.SetStrPropValueRgch(
									(int)FwTextPropType.ktptObjData,
									objData, objData.Length);
								strBldr.SetProperties(tri.ichMin, tri.ichLim,
									propsBldr.GetTextProps());
								fChanged = true;
								break;

							default:
								throw new Exception("Found an unexpected kind of ORC");
						}
					}
				}
				if (fChanged)
					revPara.Contents = strBldr.GetString();
			}
			if (iFootnote < archivedBook.FootnotesOS.Count)
			{
				string sMsg = (archivedBook.FootnotesOS.Count -  iFootnote) + " footnote(s) in " +
					book.BookId + " did not correspond to any owned footnotes in the vernacular text of that book. They have been moved to the end of the footnote sequence.";
				ReportWarning(sMsg);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjust the back translation of footnotes to reference the newly-created guid in the
		/// archive.
		/// </summary>
		/// <param name="origGuid">guid for footnote in original book</param>
		/// <param name="newGuid">guid for footnote in archive</param>
		/// <param name="para">paragraph (potentially containing a translation)</param>
		/// ------------------------------------------------------------------------------------
		private static void AdjustBtFootnoteInArchivedBook(Guid origGuid, Guid newGuid, IStTxtPara para)
		{
			//TODO: TE-5082 Duplicate code! Should call StTxtPara.UpdateOrcsInTranslations(), or common code
			ICmTranslation trans = para.GetBT();

			if (trans == null)
				return;

			// Check the back translation for each writing system.
			foreach (IWritingSystem ws in trans.AvailableWritingSystems)
			{
				ITsString btTss = trans.Translation.get_String(ws.Handle);

				// Scan through runs searching for a reference guid with the original guid.
				int cRuns = btTss.RunCount;
				TsRunInfo tri;
				ITsTextProps ttp;
				for (int iRun = 0; iRun < cRuns; iRun++)
				{
					Guid guid = TsStringUtils.GetGuidFromRun(btTss, iRun, out tri, out ttp);

					if (guid != Guid.Empty && guid == origGuid)
					{
						// Guid mapping back to orignal draft found. Update it.
						byte[] objData;
						objData = TsStringUtils.GetObjData(newGuid,
							(byte)FwObjDataTypes.kodtNameGuidHot);
						ITsPropsBldr propsBldr;
						propsBldr = ttp.GetBldr();
						propsBldr.SetStrPropValueRgch(
							(int)FwTextPropType.ktptObjData,
							objData, objData.Length);
						ITsStrBldr btTssBldr = btTss.GetBldr();
						btTssBldr.SetProperties(tri.ichMin, tri.ichLim,
							propsBldr.GetTextProps());
						// Set the translation alt string to the new value
						trans.Translation.set_String(ws.Handle, btTssBldr.GetString());
						break;
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the book in the given sequence which follows the given book (identified by its
		/// canonical number).
		/// </summary>
		/// <param name="booksOs">The books in which to look.</param>
		/// <param name="nCanonicalBookNum">The canonical book number (1-66).</param>
		/// <returns>The following book; or null if no following book is found</returns>
		/// ------------------------------------------------------------------------------------
		internal static IScrBook GetBookFollowing(IEnumerable<IScrBook> booksOs, int nCanonicalBookNum)
		{
			return booksOs.FirstOrDefault(existingBook => existingBook.CanonicalNum > nCanonicalBookNum);
		}
	}
	#endregion

	#region SegmentServices class
	/// <summary>
	/// Segment services
	/// GJM June 1'10: I renamed AnnotationServices to SegmentServices, since
	/// most of the methods have to do with Segments and in the new model
	/// annotations are much less used. In fact, it might be helpful to see if
	/// some methods here should be deprecated [such as SetCbaFields()].
	/// </summary>
	public static class SegmentServices
	{
		/// <summary>
		/// Given a starting segment in a paragraph, return the next segment
		/// in the paragraph, or null if there isn't one.
		/// </summary>
		/// <param name="startSegment"></param>
		/// <returns></returns>
		public static ISegment GetNextSegmentOrNull(ISegment startSegment)
		{
			var para = startSegment.Paragraph;
			var maxIndex = para.SegmentsOS.Count - 1;
			var istartSeg = startSegment.IndexInOwner;
			return (istartSeg < maxIndex) ? para.SegmentsOS[istartSeg + 1] : null;
		}

		/// <summary>
		/// Find the closest segment to the given range of characters.
		/// </summary>
		public static ISegment FindClosestSegment(IEnumerable<ISegment> segments, int ichMin, int ichLim, out bool fExactMatch)
		{
			ISegment segClosest = null;
			fExactMatch = false;	// default
			foreach (var seg in segments)
			{
				if (seg.BeginOffset == ichMin)
				{
					fExactMatch = true;
					return seg;
				}
				if (ichMin >= seg.BeginOffset && ichLim <= seg.EndOffset)
				{
					// this annotation contains the requested range. Return it.
					return seg;
				}
				if (segClosest == null ||
						 Math.Abs(seg.BeginOffset - ichMin) < Math.Abs(segClosest.BeginOffset - ichMin))
				{
					segClosest = seg;
				}
			}
			return segClosest;
		}

		/// <summary>
		/// Return a segment and an index (of an analysis in that segment) of an analysis that
		/// matches the given boundaries in an StTxtPara object.
		/// If no exact match is found, return the nearest analysis (preferably the following word).
		/// </summary>
		/// <param name="para"></param>
		/// <param name="ichMin"></param>
		/// <param name="ichLim"></param>
		/// <param name="fExactMatch"> true if we return an exact match, false otherwise.</param>
		/// <returns>null if not found</returns>
		public static AnalysisOccurrence FindNearestAnalysis(IStTxtPara para, int ichMin, int ichLim, out bool fExactMatch)
		{
			fExactMatch = false;
			if (para == null)
				return null;
			// first find the closest segment.
			var segments = para.SegmentsOS;
			var seg = FindClosestSegment(segments, ichMin, ichLim, out fExactMatch);
			if (seg == null)
				return null;
			return seg.FindWagform(ichMin - seg.BeginOffset, ichLim - seg.BeginOffset, out fExactMatch);
		}

		/// <summary>
		/// Get the segments of the paragraph.  This is public static to allow others to use
		/// the same code.  This will actually parse the text of the paragraph, create any
		/// segments that do not yet exist, and create any needed free translation annotations
		/// to go with them.  It also sets the kflidSegments (virtual) property of the paragraph,
		/// and the kflidFT (virtual) property of the segments.
		/// </summary>
		/// <returns>array of ICmBaseAnnotation objects for the segments</returns>
		public static IList<ISegment> GetMainParaSegments(IStTxtPara para)
		{
			EnsureMainParaSegments(para);
			return para.SegmentsOS.ToList();
		}

		/// <summary>
		/// Ensure that the segments property of the paragraph is consistent with its
		/// contents and consists of real database objects.
		/// </summary>
		internal static void EnsureMainParaSegments(IStTxtPara para)
		{
			using (var pp = new ParagraphParser(para))
			{
				List<int> eosOffsets;
				pp.CollectSegmentsOfPara(out eosOffsets);
			}
		}

		/// <summary>
		/// Iterate through paragraph and return each analysis in succesion.
		/// </summary>
		/// <param name="para"></param>
		/// <returns></returns>
		public static IEnumerable<AnalysisOccurrence> GetAnalysisOccurrences(IStTxtPara para)
		{
			return StTextAnnotationNavigator.GetAnalysisOccurrencesAdvancingInPara(para);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the segments that the specified TsString would contain.
		/// </summary>
		/// <param name="tss">The TsString.</param>
		/// <param name="wsf">The writing system factory.</param>
		/// ------------------------------------------------------------------------------------
		public static List<TsStringSegment> GetSegments(this ITsString tss, ILgWritingSystemFactory wsf)
		{
			return tss.GetSegments(wsf, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the segments that the specified TsString would contain.
		/// </summary>
		/// <param name="tss">The TsString.</param>
		/// <param name="wsf">The writing system factory.</param>
		/// <param name="fTreatOrcsAsLabels"><c>true</c> to treat ORCs as label text
		/// (FW 6.0 style), false otherwise.</param>
		/// ------------------------------------------------------------------------------------
		public static List<TsStringSegment> GetSegments(this ITsString tss, ILgWritingSystemFactory wsf,
			bool fTreatOrcsAsLabels)
		{
			SegmentCollector sc = new SegmentCollector(tss, wsf, fTreatOrcsAsLabels);
			sc.Run();
			return sc.Segments;
		}

		/// <summary>
		/// Encapsulates knowledge for navigating through the annotations of a StText
		/// </summary>
		public class StTextAnnotationNavigator
		{
			/// <summary>
			/// The thing currently in focus in the navigator. Must be an IStText or an AnalysisOccurrence.
			/// </summary>
			object m_currentFocusObject = null;

			/// <summary>
			///
			/// </summary>
			/// <param name="startingoOccurrence">indicates the occurrence in the StText to mark as the starting position</param>
			public StTextAnnotationNavigator(AnalysisOccurrence startingoOccurrence)
			{
				Cache = startingoOccurrence.Analysis.Cache;
				CurrentFocusObject = startingoOccurrence;
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="textToNavigate">the text to navigate</param>
			public StTextAnnotationNavigator(IStText textToNavigate)
			{
				Cache = textToNavigate.Cache;
				CurrentFocusObject = textToNavigate;
			}

			object CurrentFocusObject
			{
				get { return m_currentFocusObject; }
				set
				{
					// reset the state of our properties.
					RootStText = null;
					CurrentPara = null;
					StartingOccurrence = null;
					m_currentFocusObject = value;
					if (m_currentFocusObject == null)
						return;
					if (m_currentFocusObject is AnalysisOccurrence)
					{
						var occurrence = (AnalysisOccurrence)m_currentFocusObject;
						StartingOccurrence = occurrence;
						CurrentPara = occurrence.Segment.Paragraph;
						RootStText = (IStText)CurrentPara.Owner;
					}
					else if (m_currentFocusObject is ISegment)
					{
						CurrentPara = ((ISegment)m_currentFocusObject).Paragraph;
						RootStText = (IStText)CurrentPara.Owner;
					}
					else if (m_currentFocusObject is IStText)
					{
						RootStText = (IStText)m_currentFocusObject;
					}
					else
					{
						throw new ArgumentException("CurrentFocusObject must be AnalysisOccurrence or Segment or StText");
					}
				}
			}

			/// <summary>
			/// The StText being navigated.
			/// </summary>
			public IStText RootStText { get; private set; }
			private IStTxtPara CurrentPara { get; set; }

			/// <summary>
			/// gets or sets the starting analysis occurrence being read from the document.
			/// </summary>
			internal AnalysisOccurrence StartingOccurrence { get; private set; }

			/// <summary>
			/// Returns the current wordform-type occurrence in the StText for which we are navigating.
			/// </summary>
			internal AnalysisOccurrence StartingWordformOccurrence
			{
				get
				{
					if (StartingOccurrence != null && !(StartingOccurrence.Analysis is IPunctuationForm))
						return StartingOccurrence;
					return null;
				}
			}

			private FdoCache Cache { get; set; }

			/// <summary>
			/// Get the next occurrence, starting after one previously set up as the start one.
			/// NOTE: The navigator must be instantiated starting on a wordform occurrence before calling this method.
			/// </summary>
			/// <param name="defaultAnalysis">Typically this should be set to StartingWordform</param>
			/// <returns></returns>
			public virtual AnalysisOccurrence GetNextWordformOrDefault(AnalysisOccurrence defaultAnalysis)
			{
				return GetWordformOccurrencesAdvancingIncludingStartingOccurrence().Skip(1).DefaultIfEmpty(defaultAnalysis).FirstOrDefault();
			}

			/// <summary>
			/// Get the previous occurrence, starting before one previously set up as the start one.
			/// NOTE: The navigator must be instantiated starting on a wordform occurrence before calling this method.
			/// </summary>
			/// <param name="defaultAnalysis">Typically this should be set to StartingWordform; it is returned if there is no previous analysis.</param>
			public virtual AnalysisOccurrence GetPreviousWordformOrDefault(AnalysisOccurrence defaultAnalysis)
			{
				return GetWordformOccurrencesBackwardsIncludingStartingOccurrence().Skip(1).DefaultIfEmpty(defaultAnalysis).FirstOrDefault();
			}

			/// <summary>
			/// Begining at the starting wordform
			/// NOTE: The navigator must be instantiated starting on a wordform occurrence before calling this method.
			/// </summary>
			/// <returns></returns>
			public virtual AnalysisOccurrence GetNextWordformOrStartingWordform()
			{
				return GetWordformOccurrencesAdvancingIncludingStartingOccurrence().Skip(1).DefaultIfEmpty(StartingWordformOccurrence).FirstOrDefault();
			}

			/// <summary>
			/// Get all the occurrences in the text, in order.
			/// </summary>
			public IEnumerable<AnalysisOccurrence> GetAnalysisOccurrencesAdvancingInStText()
			{
				foreach (IStTxtPara para in RootStText.ParagraphsOS)
					foreach (var occurrence in GetAnalysisOccurrencesAdvancingInPara(para))
						yield return occurrence;
			}

			internal IEnumerable<AnalysisOccurrence> GetWordformsAdvancingInStText()
			{
				return GetAnalysisOccurrencesAdvancingInStText()
					.Where(occurrence => !(occurrence.Analysis is IPunctuationForm));
			}

			/// <summary>
			/// NOTE: Needs test.
			/// </summary>
			/// <param name="para"></param>
			/// <returns></returns>
			public static IEnumerable<AnalysisOccurrence> GetWordformOccurrencesAdvancingInPara(IStTxtPara para)
			{
				foreach (var occurrence in GetAnalysisOccurrencesAdvancingInPara(para).Where(occ => occ.HasWordform))
					yield return occurrence;
			}

			internal IEnumerable<AnalysisOccurrence> GetWordformOccurrencesBackwardsInStText()
			{
				return GetAnalysisOccurrencesBackwardsInStText()
					.Where(occ => occ.HasWordform);
			}

			internal static IEnumerable<AnalysisOccurrence> GetAnalysisOccurrencesAdvancingInPara(IStTxtPara para)
			{
				foreach (var seg in para.SegmentsOS)
					for (int i = 0; i < seg.AnalysesRS.Count; i++)
						yield return new AnalysisOccurrence(seg, i);
			}

			internal IEnumerable<AnalysisOccurrence> GetAnalysisOccurrencesBackwardsInStText()
			{
				foreach (IStTxtPara para in RootStText.ParagraphsOS.Reverse())
					foreach (var occurrence in GetAnalysisOccurrencesBackwardsInPara(para))
						yield return occurrence;
			}

			internal static IEnumerable<AnalysisOccurrence> GetAnalysisOccurrencesBackwardsInPara(IStTxtPara para)
			{
				foreach (var seg in para.SegmentsOS.Reverse())
					for (int i = seg.AnalysesRS.Count - 1; i >= 0; i--)
							yield return new AnalysisOccurrence(seg, i);
			}

			/// <summary>
			/// NOTE: only call if StartingOcurrence != null
			/// </summary>
			/// <returns></returns>
			internal IEnumerable<AnalysisOccurrence> GetAnalysisOccurrencesAdvancingIncludingStartingOccurrence()
			{
				if (StartingOccurrence == null)
					throw new ArgumentException("StartingOccurrence should not be null when calling this.");
				return GetAnalysisOccurrencesAdvancingInStText().SkipWhile(occ => occ != StartingOccurrence);
			}

			/// <summary>
			/// NOTE: only call if StartingOcurrence != null
			/// </summary>
			internal IEnumerable<AnalysisOccurrence> GetAnalysisOccurrencesBackwardsIncludingStartingOccurrence()
			{
				if (StartingOccurrence == null)
					throw new ArgumentException("StartingOccurrence should not be null when calling this.");
				return GetAnalysisOccurrencesBackwardsInStText().SkipWhile(occ => occ != StartingOccurrence);
			}

			/// <summary>
			/// Get all the wordform occurrences in the text from the current one onwards.
			/// </summary>
			public IEnumerable<AnalysisOccurrence> GetWordformOccurrencesAdvancingIncludingStartingOccurrence()
			{
				if (StartingWordformOccurrence == null)
					throw new ArgumentException("Starting occurrence should not be null when calling this.");
				return GetAnalysisOccurrencesAdvancingIncludingStartingOccurrence()
					.Where(occ => occ.HasWordform);
			}

			/// <summary>
			/// Get all the wordform occurrences in the text from the current one backwards (in reverse order).
			/// </summary>
			public IEnumerable<AnalysisOccurrence> GetWordformOccurrencesBackwardsIncludingStartingOccurrence()
			{
				if (StartingWordformOccurrence == null)
					throw new ArgumentException("Starting occurrence should not be null when calling this.");
				return GetAnalysisOccurrencesBackwardsIncludingStartingOccurrence()
					.Where(occ => occ.HasWordform);
			}

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ws at para offset.
		/// </summary>
		/// <param name="para"></param>
		/// <param name="ichBeginOffset">The ich begin offset.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static int GetWsIdAtParaOffset(IStTxtPara para, int ichBeginOffset)
		{
			ITsString tssPara = para.Contents;
			int ws = TsStringUtils.GetWsAtOffset(tssPara, ichBeginOffset);
			return ws;
		}

		/// <summary>
		/// Gets all the segments that have the specified WAG as their analyis.
		/// A segment may be returned more than once, if it has multiple refs to the target.
		/// </summary>
		/// <param name="targetWag"></param>
		/// <returns></returns>
		public static IEnumerable<ISegment> SegmentsContainingWag(AnalysisTree targetWag)
		{
			var segments = targetWag.Analysis.Cache.ServiceLocator.GetInstance<ISegmentRepository>().AllInstances();
			return from seg in segments
				   from analysis in seg.AnalysesRS
				   where analysis == targetWag.Analysis
				   select seg;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="cba"></param>
		/// <param name="para"></param>
		/// <param name="ichMin"></param>
		/// <param name="ichLim"></param>
		/// <param name="instanceOf"></param>
		public static void SetCbaFields(ICmBaseAnnotation cba, IStTxtPara para, int ichMin, int ichLim, ICmObject instanceOf)
		{
			SetCbaFields(cba, para, ichMin, ichLim);
			cba.InstanceOfRA = instanceOf;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="cba"></param>
		/// <param name="para"></param>
		/// <param name="ichMin"></param>
		/// <param name="ichLim"></param>
		public static void SetCbaFields(ICmBaseAnnotation cba, IStTxtPara para, int ichMin, int ichLim)
		{
			if (ichMin < 0 || ichLim < 0)
				throw new ArgumentException("new offsets (" + ichMin + ", " + ichLim + ") will extend cba offsets beyond paragraph limits.");
			cba.BeginObjectRA = para;
			cba.EndObjectRA = para;
			cba.Flid = StTxtParaTags.kflidContents;
			cba.BeginOffset = ichMin;
			cba.EndOffset = ichLim;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the tss of the given annotation.
		/// </summary>
		/// <param name="cba">a paragraph annotation.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static ITsString BaselineSubstring(ICmBaseAnnotation cba)
		{
			IStTxtPara paragraph = cba.BeginObjectRA as IStTxtPara;
			Debug.Assert(paragraph != null, String.Format("We expect cba{0} to be a paragraph annotation.", cba.Hvo));
			if (paragraph == null)
				return null;
			ITsString tssContents = paragraph.Contents;
			Debug.Assert(cba.BeginOffset < tssContents.Length && cba.BeginOffset >= 0);
			Debug.Assert(cba.EndOffset <= tssContents.Length);
			return tssContents.GetSubstring(cba.BeginOffset, cba.EndOffset);
		}

		///// <summary>
		///// get the ws at the cba's BeginOffset in its paragraph.
		///// </summary>
		///// <returns></returns>
		///// ------------------------------------------------------------------------------------
		//public static IWritingSystem GetBaselineWs(ICmBaseAnnotation cba)
		//{
		//    int ws = GetWsIdAtParaOffset((IStTxtPara)cba.BeginObjectRA, cba.BeginOffset);
		//    return cba.Cache.ServiceLocator.WritingSystemManager.Get(ws);
		//}
	}
	#endregion

	#region AnalysisTree class
	/// <summary>
	/// encapsulates the current analysis in terms of its WfiGloss, WfiAnalysis, or WfiWordform tree.
	/// </summary>
	public class AnalysisTree
	{
		private IAnalysis m_obj;

		/// <summary>
		///
		/// </summary>
		public AnalysisTree()
		{
		}
		/// <summary>
		///
		/// </summary>
		public AnalysisTree(IAnalysis analysis)
		{
			Analysis = analysis;
		}

		/// <summary>
		/// WfiWordform, WfiAnalysis, or WfiGloss.
		/// Setting this will load the properties Wordform, Analysis, Gloss.
		/// </summary>
		public IAnalysis Analysis
		{
			get
			{
				return m_obj;
			}
			set
			{

				m_obj = value;
				LoadAnalysisOwnershipTree();
			}
		}

		private void LoadAnalysisOwnershipTree()
		{
			// reset the state of our properties.
			Wordform = null;
			WfiAnalysis = null;
			Gloss = null;
			if (m_obj == null)
				return;
			switch (m_obj.ClassID)
			{
				case WfiWordformTags.kClassId:
					Wordform = (IWfiWordform)m_obj;
					break;
				case WfiAnalysisTags.kClassId:
					WfiAnalysis = (IWfiAnalysis)m_obj;
					Wordform = (IWfiWordform)m_obj.Owner;
					break;
				case WfiGlossTags.kClassId:
					Gloss = (IWfiGloss)m_obj;
					WfiAnalysis = (IWfiAnalysis)m_obj.Owner;
					Wordform = (IWfiWordform)m_obj.Owner.Owner;
					break;
				default:
					Wordform = null;
					throw new ArgumentException("analysis must be WfiWordform, WfiAnalysis, or WfiGloss");
			}
		}

		/// <summary>
		/// WfiWordform of Analysis
		/// </summary>
		public IWfiWordform Wordform { get; private set; }
		/// <summary>
		/// WfiAnalysis of Analysis (if any)
		/// </summary>
		public IWfiAnalysis WfiAnalysis { get; private set; }
		/// <summary>
		/// WfiGloss of Analysis (if any)
		/// </summary>
		public IWfiGloss Gloss { get; private set; }
	}
	#endregion

	#region NullWAG class
	/// <summary>
	/// a Null object for a WAG
	/// </summary>
	public class NullWAG : NullCmObject, IAnalysis
	{
		#region IAnalysis Members

		/// <summary>
		///
		/// </summary>
		public IWfiWordform Wordform
		{
			get { return null; }
		}

		/// <summary>
		///
		/// </summary>
		public bool HasWordform
		{
			get { return false; }
		}

		/// <summary>
		///
		/// </summary>
		public IWfiAnalysis Analysis
		{
			get { return null; }
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		public ITsString GetForm(int ws)
		{
			return null;
		}

		#endregion
	}
	#endregion

	#region NullCmObject class
	/// <summary>
	/// class representing a null CmObject
	/// </summary>
	public class NullCmObject : ICmObject, IComparable
	{
		#region IComparable Members

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public int CompareTo(object obj)
		{
			if (Equals(obj))
				return 0;
			return -1;
		}
		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			return obj is NullCmObject;
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return 0;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator ==(NullCmObject a, NullCmObject b)
		{
			// If both are null, or both are same instance, return true.
			if (System.Object.ReferenceEquals(a, b))
			{
				return true;
			}

			// If one is null, but not both, return false.
			if (((object)a == null) || ((object)b == null))
			{
				return false;
			}

			// Return true if the fields match:
			return a.Equals(b);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator !=(NullCmObject a, NullCmObject b)
		{
			return !(a == b);
		}

		#endregion

		#region ICmObject Members

		/// <summary>
		/// Null object has nothing referring to it. (I suppose we could argue that it should return all the
		/// objects that have null as the value of any reference property, but I don't think that would be useful.)
		/// </summary>
		public HashSet<ICmObject> ReferringObjects
		{
			get { return new HashSet<ICmObject>(); }
		}

		/// <summary>
		/// returns 0
		/// </summary>
		public int Hvo
		{
			get { return 0; }
		}

		/// <summary>
		/// returns null
		/// </summary>
		public ICmObject Owner
		{
			get { return null; }
		}

		/// <summary>
		/// returns 0
		/// </summary>
		public int OwningFlid
		{
			get { return 0; }
		}

		/// <summary>
		/// return 0
		/// </summary>
		public int ClassID
		{
			get { return 0; }
		}

		/// <summary>
		/// returns an empty string.
		/// </summary>
		public string ClassName
		{
			get { return string.Empty; }
		}

		/// <summary>
		/// returns null
		/// </summary>
		/// <param name="clsid"></param>
		/// <returns></returns>
		public ICmObject OwnerOfClass(int clsid)
		{
			return null;
		}

		/// <summary>
		/// returns null
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T OwnerOfClass<T>() where T : ICmObject
		{
			T co = default(T);
			return co;
		}

		/// <summary>
		/// returns this;
		/// </summary>
		public ICmObject Self
		{
			get { return this; }
		}

		/// <summary>
		/// returns false.
		/// </summary>
		public bool IsValidObject
		{
			get { return false; }
		}

		/// <summary>
		///  Not implemented
		/// </summary>
		/// <returns></returns>
		public string ToXmlString()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Delete the recipient object.
		/// </summary>
		public void Delete()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		///  Not implemented
		/// </summary>
		public IFdoServiceLocator Services
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="flidToCheck"></param>
		/// <param name="createAnnotation"></param>
		/// <param name="failure"></param>
		/// <returns></returns>
		public bool CheckConstraints(int flidToCheck, bool createAnnotation, out ConstraintFailure failure)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="copyMap"></param>
		public void PostClone(Dictionary<int, ICmObject> copyMap)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Null object doesn't reference any.
		/// </summary>
		public void AllReferencedObjects(List<ICmObject> collector)
		{
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="flid"></param>
		/// <param name="propsToMonitor"></param>
		/// <returns></returns>
		public bool IsFieldRelevant(int flid, HashSet<Tuple<int, int>> propsToMonitor)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Grr!
		/// </summary>
		public bool IsOwnedBy(ICmObject possibleOwner)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="flid"></param>
		/// <returns></returns>
		public ICmObject ReferenceTargetOwner(int flid)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="flid"></param>
		/// <returns></returns>
		public bool IsFieldRequired(int flid)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		///  Not implemented
		/// </summary>
		public int IndexInOwner
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="flid"></param>
		/// <returns></returns>
		public IEnumerable<ICmObject> ReferenceTargetCandidates(int flid)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		///
		/// </summary>
		public FdoCache Cache
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="objSrc"></param>
		public void MergeObject(ICmObject objSrc)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="objSrc"></param>
		/// <param name="fLoseNoStringData"></param>
		public void MergeObject(ICmObject objSrc, bool fLoseNoStringData)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		///
		/// </summary>
		public bool CanDelete
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		///
		/// </summary>
		public string ShortName
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		///
		/// </summary>
		public ITsString ObjectIdName
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		///
		/// </summary>
		public ITsString ShortNameTSS
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		///
		/// </summary>
		public ITsString DeletionTextTSS
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		///
		/// </summary>
		public ITsString ChooserNameTS
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		///
		/// </summary>
		public string SortKey
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		///
		/// </summary>
		public string SortKeyWs
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		///
		/// </summary>
		public int SortKey2
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		///
		/// </summary>
		public string SortKey2Alpha
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		///
		/// </summary>
		public int OwnOrd
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		///
		/// </summary>
		public Guid Guid
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		///
		/// </summary>
		public ICmObjectId Id
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public ICmObject GetObject(ICmObjectRepository repo)
		{
			return this;
		}

		#endregion
	}
	#endregion

	#region WordAnalysisOrGlossServices class
	/// <summary>
	/// WAG services
	/// </summary>
	public static class WordAnalysisOrGlossServices
	{
		/// <summary>
		/// Create a new wag with a gloss based on the wordform
		/// </summary>
		/// <param name="wf"></param>
		/// <returns></returns>
		public static AnalysisTree CreateNewAnalysisTreeGloss(IWfiWordform wf)
		{
			var servLoc = wf.Cache.ServiceLocator;
			var glossFactory = servLoc.GetInstance<IWfiGlossFactory>();
			var analysisFactory = servLoc.GetInstance<IWfiAnalysisFactory>();

			var newAnalysis = analysisFactory.Create(wf, glossFactory);
			return new AnalysisTree(newAnalysis.MeaningsOC.First());
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="wf"></param>
		/// <returns></returns>
		public static IWfiAnalysis CreateNewAnalysisWAG(IWfiWordform wf)
		{
			var wa = CreateNewAnalysis(wf);
			return wa;
		}

		private static IWfiAnalysis CreateNewAnalysis(IWfiWordform wf)
		{
			var servLoc = wf.Cache.ServiceLocator;
			var analysisFactory = servLoc.GetInstance<IWfiAnalysisFactory>();
			var newAnalysis = analysisFactory.Create();
			wf.AnalysesOC.Add(newAnalysis);
			return newAnalysis;
		}
	}
	#endregion

	#region BackTranslationAndFreeTranslationSyncHelper class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Do() will detect whether or not we're already in the process of doing an
	/// update, to prevent recursion/re-entrancy.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class BackTranslationAndFreeTranslationUpdateHelper : FwDisposableBase
	{
		[ThreadStatic]
		private static IStTxtPara s_para;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private BackTranslationAndFreeTranslationUpdateHelper(IStTxtPara para)
		{
			if (para == null)
				throw new ArgumentNullException("para");
			if (s_para != null && para != s_para)
				throw new InvalidOperationException("Attempting to sync the back translation and free translation of more than one paragraph at the same time");
			s_para = para;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Perform the specified update delegate (if a update is not already in progress)
		/// </summary>
		/// <param name="para">the paragraph for which we are updating bt and/or ft's</param>
		/// <param name="updateMethod">The method that will update the back translation or
		/// free translation.</param>
		/// ------------------------------------------------------------------------------------
		internal static void Do(IStTxtPara para, Action updateMethod)
		{
			// we don't want to update the main (back) translation if the back translation
			// is updating the free translations.
			if (s_para == para)
				return;
			using (var syncHelper = new BackTranslationAndFreeTranslationUpdateHelper(para))
			{
				updateMethod();
			}
		}

		#region FWDisposableBase
		/// ------------------------------------------------------------
		/// <summary>
		/// We're all done.
		/// </summary>
		/// ------------------------------------------------------------
		protected override void DisposeManagedResources()
		{
			s_para = null;
		}

		#endregion
	}
	#endregion

	/// <summary>
	/// TODO: try using IRepository{TObject} instead of the direct Repository interface
	/// (for example IStTxtParaRepository)
	/// </summary>
	/// <typeparam name="TRepository"></typeparam>
	/// <typeparam name="TObject"></typeparam>
	internal static class RepositoryUtils<TRepository, TObject> where TRepository : IRepository<TObject> where TObject : ICmObject
	{
		internal static IList<TObject> GetObjects(FdoCache cache, IList<int> hvos)
		{
			IList<TObject> objs = new List<TObject>(hvos.Count);
			TRepository repository = cache.ServiceLocator.GetInstance<TRepository>();
			foreach (int hvo in hvos)
				objs.Add(repository.GetObject(hvo));
			return objs;
		}
	}

	#region SandboxGenericMSA class
	/// <summary>
	/// This class is used to provide 'sandbox' capability during MSA creation.
	/// It can be compared to a real MSA, but does it on the cheap without creating a real one,
	/// which may end be being deleted in the end.
	/// </summary>
	/// <remarks>
	/// I (RandyR) think that a more current term than 'sandbox' might be a Data Transfer Object (DTO).
	/// </remarks>
	public class SandboxGenericMSA
	{
		private MsaType m_type = MsaType.kStem;
		private IPartOfSpeech m_mainPOS;
		private IPartOfSpeech m_secondaryPOS;
		private IMoInflAffixSlot m_slot;
		private IFdoReferenceCollection<IPartOfSpeech> m_fromPOSes;

		/// <summary>
		/// Gets or sets the dummy MSA from parts of speech.
		/// </summary>
		public IFdoReferenceCollection<IPartOfSpeech> FromPartsOfSpeech
		{
			get { return m_fromPOSes; }
			set
			{
				m_fromPOSes = value;
			}
		}

		/// <summary>
		/// Gets or sets the dummy MSA type, which corresponds to the object class.
		/// </summary>
		public MsaType MsaType
		{
			get { return m_type; }
			set
			{
				if (value != MsaType.kNotSet && value != MsaType.kRoot)
					m_type = value;
			}
		}

		/// <summary>
		/// Gets or sets the main POS value.
		/// </summary>
		public IPartOfSpeech MainPOS
		{
			get { return m_mainPOS; }
			set { m_mainPOS = value; }
		}

		/// <summary>
		/// Gets or sets the secondary POS value.
		/// </summary>
		public IPartOfSpeech SecondaryPOS
		{
			get { return m_secondaryPOS; }
			set { m_secondaryPOS = value; }
		}

		/// <summary>
		/// Gets or sets the Slot value.
		/// </summary>
		public IMoInflAffixSlot Slot
		{
			get { return m_slot; }
			set { m_slot = value; }
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public SandboxGenericMSA()
		{
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="msa"></param>
		/// <returns></returns>
		public static SandboxGenericMSA Create(IMoMorphSynAnalysis msa)
		{
			switch (msa.ClassID)
			{
				case MoStemMsaTags.kClassId:
					return new SandboxGenericMSA((IMoStemMsa)msa);
				case MoInflAffMsaTags.kClassId:
					return new SandboxGenericMSA((IMoInflAffMsa)msa);
				case MoDerivAffMsaTags.kClassId:
					return new SandboxGenericMSA((IMoDerivAffMsa)msa);
				case MoUnclassifiedAffixMsaTags.kClassId:
					return new SandboxGenericMSA((IMoUnclassifiedAffixMsa)msa);
				/* Not supported yet, so it throws an exception.
				case MoDerivStepMsa.kclsidMoDerivStepMsa:
					return new SandboxGenericMSA((MoDerivStepMsa)msa);
					*/
			}
			return null;
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public static SandboxGenericMSA Create(MsaType type, IPartOfSpeech mainPos)
		{
			var msa = new SandboxGenericMSA();
			msa.m_type = type;
			msa.m_mainPOS = mainPos;
			return msa;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		private SandboxGenericMSA(IMoDerivAffMsa derivMsa)
		{
			m_type = MsaType.kDeriv;
			m_mainPOS = derivMsa.FromPartOfSpeechRA;
			m_secondaryPOS = derivMsa.ToPartOfSpeechRA;
		}

		///// <summary>
		///// Constructor.
		///// </summary>
		//private SandboxGenericMSA(IMoDerivStepMsa stepMsa)
		//{
		//    Debug.Assert(false, "Step MSAs are not supported yet.");
		//    /*
		//    m_type;
		//    m_mainPOS;
		//    m_secondaryPOS;
		//    m_slot;
		//    */
		//}

		/// <summary>
		/// Constructor.
		/// </summary>
		private SandboxGenericMSA(IMoInflAffMsa inflMsa)
		{
			m_type = MsaType.kInfl;
			m_mainPOS = inflMsa.PartOfSpeechRA;
			//m_slot = inflMsa.SlotsRC;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		private SandboxGenericMSA(IMoStemMsa stemMsa)
		{
			m_type = MsaType.kStem;
			m_mainPOS = stemMsa.PartOfSpeechRA;
			m_fromPOSes = stemMsa.FromPartsOfSpeechRC;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		private SandboxGenericMSA(IMoUnclassifiedAffixMsa uncMsa)
		{
			m_type = MsaType.kUnclassified;
			m_mainPOS = uncMsa.PartOfSpeechRA;
		}
	}
	#endregion

	#region MorphComponents class
	/// <summary>
	///
	/// </summary>
	public class MorphComponents
	{
		/// <summary>
		///
		/// </summary>
		public MorphComponents() { }

		/// <summary> </summary>
		public string Prefix { get; set; }
		/// <summary> </summary>
		public ITsString TssForm { get; set; }
		/// <summary> </summary>
		public string Postfix { get; set; }
		/// <summary> </summary>
		public IMoMorphType MorphType { get; set; }
	}
	#endregion

	#region LexEntryComponents class
	/// <summary>
	/// DataTransferObject for creating a new LexEntry.
	/// </summary>
	public class LexEntryComponents
	{
		/// <summary>
		///
		/// </summary>
		public LexEntryComponents()
		{
			GlossAlternatives = new List<ITsString>();
			LexemeFormAlternatives = new List<ITsString>();
			MSA = new SandboxGenericMSA();
			GlossFeatures = new List<XmlNode>();
		}

		/// <summary>
		/// (doesn't include morpheme markers)
		/// </summary>
		public IList<ITsString> LexemeFormAlternatives;
		/// <summary>
		/// </summary>
		public IMoMorphType MorphType { get; set; }

		/// <summary>
		/// </summary>
		public SandboxGenericMSA MSA { get; set; }
		/// <summary>
		/// The first one should be the main gloss.
		/// </summary>
		public IList<ITsString> GlossAlternatives { get; set; }

		/// <summary>
		/// </summary>
		public IList<XmlNode> GlossFeatures { get; set; }
	}
	#endregion

	#region DomainObjectServices class
	/// <summary>
	/// Services for the CmObjects.
	/// </summary>
	public static class DomainObjectServices
	{
		/// <summary>
		/// Common code used by LexSense and LexEntry
		/// </summary>
		/// <param name="incomingRefs"></param>
		/// <returns></returns>
		internal static List<ILexReference> ExtractMinimalLexReferences(SimpleBag<IReferenceSource> incomingRefs)
		{
			var references = (from collection in incomingRefs.OfType<FdoReferenceSequence<ICmObject>>()
							  where collection.Flid == LexReferenceTags.kflidTargets
							  select collection.MainObject as ILexReference).ToList();
			return LexReference.ExtractMinimalLexReferences(references);
		}

		/// <summary>
		/// Replace all references in the system to 'from' with a reference to 'to'.
		/// Caller is responsible to set up UOW.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		public static void ReplaceReferences(ICmObject from, ICmObject to)
		{
			CmObject.ReplaceReferences(from.Cache, from, to);
		}

		/// <summary>
		/// Replace all references in the system to 'from' with a reference to 'to', provided the new reference is valid.
		/// If the new one is not valid in some instance, go ahead and replace the others.
		/// Caller is responsible to set up UOW.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		public static void ReplaceReferencesWhereValid(ICmObject from, ICmObject to)
		{
			CmObject.ReplaceReferencesWhereValid(from.Cache, from, to);
		}

		/// <summary>
		///
		/// </summary>
		/// <typeparam name="TObj"></typeparam>
		/// <param name="sda"></param>
		/// <param name="repository"></param>
		/// <param name="mainObj"></param>
		/// <param name="flidVector"></param>
		/// <returns></returns>
		public static IEnumerable<int> GetObjectSet<TObj>(ISilDataAccessManaged sda,
			IRepository<TObj> repository,
			ICmObject mainObj, int flidVector)
			where TObj : ICmObject
		{
			return sda.VecProp(mainObj.Hvo, flidVector);
		}

		/// <summary>
		/// Returns the Entry Refs for the given variant/entry.
		/// (Currently we only return ones with one ComponentLexeme.)
		/// </summary>
		/// <param name="variantEntry"></param>
		/// <returns></returns>
		public static IEnumerable<ILexEntryRef> GetVariantRefs(ILexEntry variantEntry)
		{
			Debug.Assert(variantEntry != null, "Variant Entry shouldn't be null.");
			return from entryRef in variantEntry.EntryRefsOS
				   where entryRef.RefType == LexEntryRefTags.krtVariant &&
// Is this necessary:    entryRef.VariantEntryTypesRS != null && entryRef.VariantEntryTypesRS.Count > 0 &&
						 entryRef.ComponentLexemesRS.Count == 1
				   select entryRef;
		}

		/// <summary>
		/// If variantOrEntry is a variant, try to find an entry it's a variant of that
		/// has a sense.  Return the corresponding ILexEntryRef for the first such entry.
		/// If this is being called to establish a default monomorphemic guess, skip over
		/// any bound root or bound stem entries that variantOrEntry may be a variant of.
		/// </summary>
		/// <returns>the lexEntryRef of the primary/main/head entry or sense</returns>
		public static ILexEntryRef GetVariantRef(ILexEntry variantOrEntry, bool fMonoMorphemic)
		{
			foreach (var entryRef in GetVariantRefs(variantOrEntry))
			{
				IVariantComponentLexeme component = (IVariantComponentLexeme)entryRef.ComponentLexemesRS[0];
				if (fMonoMorphemic && IsEntryBound(component))
					continue;
				if (component is ILexSense ||
					component is ILexEntry && ((ILexEntry)component).SensesOS.Count > 0)
				{
					return entryRef;
				}
				else
				{
					// Should we check for a variant of a variant of a ...?
				}
			}
			return null; // nothing useful we can do.
		}

		/// <summary>
		/// Check whether the given entry (or entry owning the given sense) is either a bound
		/// root or a bound stem.  We don't want to use those as guesses for monomorphemic
		/// words.  See LT-10323.
		/// </summary>
		private static bool IsEntryBound(IVariantComponentLexeme component)
		{
			ILexEntry targetEntry;
			if (component is ILexSense)
			{
				ILexSense ls = (ILexSense)component;
				targetEntry = ls.Entry;
				if (!(ls.MorphoSyntaxAnalysisRA is IMoStemMsa))
					return true;		// must be an affix, so it's bound by definition.
			}
			else
			{
				targetEntry = (ILexEntry)component;
			}
			if (targetEntry.LexemeFormOA != null)
			{
				IMoMorphType morphType = targetEntry.LexemeFormOA.MorphTypeRA;
				if (morphType != null)
				{
					if (morphType.IsAffixType)
						return true;
					if (morphType.Guid == MoMorphTypeTags.kguidMorphBoundRoot ||
						morphType.Guid == MoMorphTypeTags.kguidMorphBoundStem)
					{
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Joins a set of ids into a string separated by the given separator.
		/// </summary>
		/// <param name="ids"></param>
		/// <param name="sep"> separator to use for the join.</param>
		/// <returns></returns>
		public static string JoinIds(int[] ids, string sep)
		{
			var strings = new string[ids.Length];
			// convert ids to string[]
			for (var i = 0; i < ids.Length; ++i)
				strings[i] = ids[i].ToString();
			return String.Join(sep, strings);
		}

		/// <summary>
		/// find out if the 1 class is the same as or a subclass of another CELLAR class
		/// </summary>
		/// <param name="mdc"></param>
		/// <param name="classId"></param>
		/// <param name="baseClassId"></param>
		/// <returns></returns>
		public static bool IsSameOrSubclassOf(IFwMetaDataCache mdc, int classId, int baseClassId)
		{
			if (classId == baseClassId)
				return true;

			var allClids = new List<int>(((IFwMetaDataCacheManaged)mdc).GetAllSubclasses(baseClassId));
			return allClids.Contains(classId);
		}

		/// <summary>
		/// Test equivalent of two feature structues (either of which might be null).
		/// </summary>
		/// <param name="first"></param>
		/// <param name="second"></param>
		/// <returns></returns>
		public static bool AreEquivalent(IFsFeatStruc first, IFsFeatStruc second)
		{
			if (first == null)
				return second == null || second.IsEmpty;
			return first.IsEquivalent(second);
		}

		/// <summary>
		/// Get a set of inflectional affix slots which can be prefixal or suffixal
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="allSlots">Original set of all slots</param>
		/// <param name="fLookForPrefixes">whether to look for prefixal slots</param>
		/// <returns>subset of slots that are either prefixal or suffixal</returns>
		public static IEnumerable<IMoInflAffixSlot> GetSomeSlots(FdoCache cache, IEnumerable<IMoInflAffixSlot> allSlots, bool fLookForPrefixes)
		{
			var set = new HashSet<IMoInflAffixSlot>();
			foreach (var slot in allSlots)
			{
				var fStopLooking = false;
				var affixes = slot.Affixes;
				if (affixes.Count() == 0)
				{
					// no affixes in this slot, so include it
					set.Add(slot);
				}
				else
				{
					foreach (var affMsa in affixes)
					{
						var lex = (ILexEntry)affMsa.Owner;
						var morphTypes = lex.MorphTypes;
						foreach (var morphType in morphTypes)
						{
							var fIsCorrectType = fLookForPrefixes ? morphType.IsPrefixishType : morphType.IsSuffixishType;
							if (fIsCorrectType)
							{
								set.Add(slot);
								fStopLooking = true;
								break;
							}
						}
						if (fStopLooking)
							break;
					}
				}
			}
			return set;
		}

		/// <summary>
		/// Get set of inflectional affix slots appropriate for the entry
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="lex">lex entry</param>
		/// <param name="pos">part of speech</param>
		/// <returns></returns>
		public static IEnumerable<IMoInflAffixSlot> GetSlots(FdoCache cache, ILexEntry lex, IPartOfSpeech pos)
		{
			var fIsPrefixal = false;
			var fIsSuffixal = false;
			foreach (var morphType in lex.MorphTypes)
			{
				if (morphType.IsPrefixishType)
					fIsPrefixal = true;
				if (morphType.IsSuffixishType)
					fIsSuffixal = true;
			}
			if (fIsPrefixal && fIsSuffixal)
				return pos.AllAffixSlots;
			else
				return GetSomeSlots(cache, pos.AllAffixSlots, fIsPrefixal);
		}

		/// <summary>
		/// Get set of all inflectional affix slots in the language project
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		public static IEnumerable<IMoInflAffixSlot> GetAllSlots(FdoCache cache)
		{
			var set = new SortedSet<IMoInflAffixSlot>();
			var poses = cache.LangProject.PartsOfSpeechOA;
			foreach (var pos in poses.PossibilitiesOS.Cast<PartOfSpeech>())
			{
				set.UnionWith(pos.AllAffixSlotsIncludingSubPartsOfSpeech);
			}
			return set;

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Locate CmFolder with given name or create it, if neccessary
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="flid">The field identifier that owns sFolder</param>
		/// <param name="sFolder">The name of the CmFolder where picture should be stored</param>
		/// ------------------------------------------------------------------------------------
		public static ICmFolder FindOrCreateFolder(FdoCache cache, int flid, string sFolder)
		{
			IFdoOwningCollection<ICmFolder> prop;
			switch (flid)
			{
				case LangProjectTags.kflidMedia:
					prop = cache.LangProject.MediaOC;
					break;
				case LangProjectTags.kflidPictures:
					prop = cache.LangProject.PicturesOC;
					break;
				default:
					return null; // We have an invalid Flid.
			}
			foreach (ICmFolder folder in prop)
			{
				if (folder.Name != null)
				{
					if (folder.Name.AnalysisDefaultWritingSystem != null &&
						folder.Name.AnalysisDefaultWritingSystem.Text != null &&
						folder.Name.AnalysisDefaultWritingSystem.Text.CompareTo(sFolder) == 0)
					{
						return folder;
					}
					else if (folder.Name.BestAnalysisAlternative != null &&
							 folder.Name.BestAnalysisAlternative.Text != null &&
							 folder.Name.BestAnalysisAlternative.Text.CompareTo(sFolder) == 0)
					{
						return folder;
					}
					else if (folder.Name.BestAnalysisVernacularAlternative != null &&
							 folder.Name.BestAnalysisVernacularAlternative.Text != null &&
							 folder.Name.BestAnalysisVernacularAlternative.Text.CompareTo(sFolder) == 0)
					{
						return folder;
					}
					else if (folder.Name.UserDefaultWritingSystem != null &&
							 folder.Name.UserDefaultWritingSystem.Text != null &&
							 folder.Name.UserDefaultWritingSystem.Text.CompareTo(sFolder) == 0)
					{
						return folder;
					}
				}
			}

			ICmFolder foldr = cache.ServiceLocator.GetInstance<ICmFolderFactory>().Create();
			prop.Add(foldr);
			foldr.Name.AnalysisDefaultWritingSystem = cache.TsStrFactory.MakeString(sFolder,
				WritingSystemServices.FallbackUserWs(cache));

			return foldr;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds another CmFile object whose AbsoluteInternalPath is the same as srcFile. If
		/// one is found then the CmFile object is returned. Otherwise a new one is created for
		/// the srcFile.
		/// </summary>
		/// <param name="folder">CmFolder whose CmFile collection will be searched.</param>
		/// <param name="srcFile">Full path of the picture file being searched for.</param>
		/// <returns>A CmFile object</returns>
		/// ------------------------------------------------------------------------------------
		public static ICmFile FindOrCreateFile(ICmFolder folder, string srcFile)
		{
			if (String.IsNullOrEmpty(srcFile))
				throw new ArgumentException("File path not specified.", "srcFile");

			char[] bad = Path.GetInvalidPathChars();
			int idx = srcFile.IndexOfAny(bad);
			if (idx >= 0)
				throw new ArgumentException("File path (" + srcFile + ") contains at least one invalid character.", "srcFile");

			string newName = Path.GetFileName(srcFile);
			foreach (ICmFile file in folder.FilesOC)
			{
				if (Path.GetFileName(file.AbsoluteInternalPath) == newName &&
					FileUtils.AreFilesIdentical(srcFile, file.AbsoluteInternalPath))
				{
					return file;
				}
			}

			ICmFile cmFile = folder.Services.GetInstance<ICmFileFactory>().Create();
			folder.FilesOC.Add(cmFile);
			cmFile.InternalPath = srcFile;

			return cmFile;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy source file to a unique file in FW\pictures directory.
		/// </summary>
		/// <param name="srcFilename">The path to the original filename (an internal copy will
		/// be made in this method)</param>
		/// <param name="dstSubdir">The subfolder of the FW Data directory in which the
		/// internal copy of the file should be created, if necessary (no backslashes needed)
		/// </param>
		/// <returns>Destination file path, relative to the FW data directory, or ".__NONE__"
		/// if source file doesn't exist.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static string CopyFileToInternal(string srcFilename, string dstSubdir)
		{
			string srcFilenameCorrected;
			if (!FileUtils.TrySimilarFileExists(srcFilename, out srcFilenameCorrected))
				return EmptyFileName;

			var strDestFolder = Path.Combine(DirectoryFinder.FWDataDirectory, dstSubdir);
			if (!Directory.Exists(strDestFolder))
				Directory.CreateDirectory(strDestFolder);

			var strDestFileRelPath = Path.Combine(dstSubdir, Path.GetFileName(srcFilenameCorrected));
			var strDestFileAbsPath = Path.Combine(DirectoryFinder.FWDataDirectory, strDestFileRelPath);

			// (The case-independent comparison is valid only for Microsoft Windows.)
			if (srcFilenameCorrected.Equals(strDestFileAbsPath, StringComparison.OrdinalIgnoreCase))
				return strDestFileRelPath;	// don't copy files already in internal directory.

			var strFile = Path.GetFileNameWithoutExtension(srcFilenameCorrected);
			var strExt = Path.GetExtension(srcFilenameCorrected);
			var iQual = 0;
			while (FileUtils.FileExists(strDestFileAbsPath))
			{
				iQual++;
				strDestFileRelPath = Path.Combine(dstSubdir, strFile + iQual + strExt);
				strDestFileAbsPath = Path.Combine(DirectoryFinder.FWDataDirectory, strDestFileRelPath);
			}
			File.Copy(srcFilenameCorrected, strDestFileAbsPath);
			return strDestFileRelPath;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the empty name of the file.
		/// </summary>
		/// <value>The empty name of the file.</value>
		/// ------------------------------------------------------------------------------------
		public static string EmptyFileName
		{
			get { return ".__NONE__"; }
		}
	}
	#endregion

	#region SectionAdjustmentSuppressionHelper
	/// <summary>
	/// This class is used to suppress the adjustments to the verse references on sections while
	/// some processing is taking place that would fail if the adjustments were done - see TE-9311
	/// </summary>
	public class SectionAdjustmentSuppressionHelper: IDisposable
	{
		private static SectionAdjustmentSuppressionHelper s_helper;
		private Set<ScrSection> m_sections;

		private SectionAdjustmentSuppressionHelper()
		{
			m_sections = new Set<ScrSection>();
			s_helper = this;
		}

		/// <summary>
		/// Allows all scripture section reference updating to be suppressed until the
		/// end of the given action.
		/// </summary>
		public static void Do(Action updateAction)
		{
			if (s_helper != null)
				throw new Exception("Suppression helper already in use.");

			using (var helper = new SectionAdjustmentSuppressionHelper())
			{
				updateAction();
			}
		}

		internal static bool IsSuppressionActive()
		{
			return s_helper != null;
		}

		internal static void RegisterSection(ScrSection section)
		{
			if (s_helper == null)
				throw new Exception("Suppression helper is not active");

			if (!s_helper.m_sections.Contains(section))
				s_helper.m_sections.Add(section);
		}

		#region Disposable stuff
#if DEBUG
		/// <summary/>
		~SectionAdjustmentSuppressionHelper()
		{
			Dispose(false);
		}
#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Section references will be adjusted when helper is disposed.
		/// </summary>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + ". *******");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				foreach (var section in m_sections)
					section.AdjustReferences();
				m_sections.Clear();
			}
			s_helper = null;
			IsDisposed = true;
		}
	#endregion
	}
	#endregion
}
