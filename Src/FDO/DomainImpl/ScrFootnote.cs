// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2012, SIL International. All Rights Reserved.
// <copyright from='2004' to='2012' company='SIL International'>
//		Copyright (c) 2012, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrFootnote.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;

using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.DomainImpl
{
	#region ScrFootnote
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Extends the StFootnote class for methods that are specific to scripture.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal partial class ScrFootnote
	{
		#region Data members
		protected RefRange m_location = RefRange.EMPTY;
		private bool m_fIgnoreDisplaySettings = false;
		private IScripture m_scr;
		private static int s_maxAllowedParagraphs = 1; // Should be readonly, but need to allow for unit tests to change this
		#endregion

		#region Enumeration
		private enum RefResult
		{
			/// <summary>Scripture reference info not found so had to (re)scan all footnotes</summary>
			ScannedAllFootnotes,
			/// <summary>Scripture reference found for footnote</summary>
			Found,
			/// <summary>Scripture reference not found for footnote so continue searching</summary>
			NotFound
		}
		#endregion

		#region Overridden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the default values after the initialization of a CmObject. At the point that
		/// this method is called, the object should have an HVO, Guid, and a cache set.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void SetDefaultValuesAfterInit()
		{
			base.SetDefaultValuesAfterInit();
			m_scr = Cache.LangProject.TranslatedScriptureOA;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get m_scr set.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void DoAdditionalReconstruction()
		{
			base.DoAdditionalReconstruction();
			m_scr = Cache.LangProject.TranslatedScriptureOA;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Before the footnote is deleted, we need to remove the marker from the containing
		/// paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnBeforeObjectDeleted()
		{
			DeleteFootnoteMarker();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validation done before adding an object to some vector flid.
		/// </summary>
		/// <exception cref="InvalidOperationException">The addition is not valid</exception>
		/// ------------------------------------------------------------------------------------
		protected override void ValidateAddObjectInternal(AddObjectEventArgs e)
		{
			if (e.Flid == ScrFootnoteTags.kflidParagraphs && ParagraphsOS.Count >= s_maxAllowedParagraphs)
				throw new InvalidOperationException("Scripture footnotes are limited to " + s_maxAllowedParagraphs + " paragraph(s).");
			base.ValidateAddObjectInternal(e);
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the Scripture reference for this footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateFootnoteRef()
		{
			IScrBook owningBook = OwnerOfClass<IScrBook>();
			if (owningBook == null)
				throw new InvalidOperationException("Can not calculate footnote reference until footnote is inserted");

			if (ParaContainingOrcRA == null)
				return; // Would be better to throw an exception, but will happen in tests

			BCVRef startRef = new BCVRef(owningBook.CanonicalNum, 0, 0);
			BCVRef endRef = new BCVRef(owningBook.CanonicalNum, 0, 0);

			IScrSection owningSection = ParaContainingOrcRA.OwnerOfClass<IScrSection>();
			if (owningSection != null && BCVRef.GetChapterFromBcv(owningSection.VerseRefStart) ==
				BCVRef.GetChapterFromBcv(owningSection.VerseRefEnd))
			{
				// Section only contains one chapter, so we know which chapter to use
				startRef.Chapter = endRef.Chapter = BCVRef.GetChapterFromBcv(owningSection.VerseRefStart);
			}

			IStText owningText = (IStText)ParaContainingOrcRA.Owner;
			for (int iPara = owningText.ParagraphsOS.IndexOf(ParaContainingOrcRA); iPara >= 0; iPara--)
			{
				IScrTxtPara para = (IScrTxtPara)owningText[iPara];
				RefResult result = GetReference(owningBook, para, startRef, endRef);

				if (result == RefResult.ScannedAllFootnotes)
					return; // no need to finish since full scan updated my reference
				if (result == RefResult.Found)
					break;
			}

			if (owningSection != null)
			{
				if (startRef.Verse == 0)
					startRef.Verse = endRef.Verse = new BCVRef(owningSection.VerseRefStart).Verse;

				if (startRef.Chapter == 0)
					startRef.Chapter = endRef.Chapter = new BCVRef(owningSection.VerseRefStart).Chapter;
			}

			FootnoteRefInfo = new RefRange(startRef, endRef);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the reference for this footnote.
		/// </summary>
		/// <param name="owningBook">The owning book.</param>
		/// <param name="para">The para to search for a Scripture reference (verse or chapter).
		/// </param>
		/// <param name="startRef">The starting reference for this footnote (updated in this
		/// method).</param>
		/// <param name="endRef">The ending reference for this footnote (updated in this
		/// method).</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private RefResult GetReference(IScrBook owningBook, IScrTxtPara para, BCVRef startRef, BCVRef endRef)
		{
			bool foundSelf = (para != ParaContainingOrcRA);
			IStFootnoteRepository footnoteRepo = Cache.ServiceLocator.GetInstance<IStFootnoteRepository>();

			ITsString tssContents = para.Contents;
			for (int i = tssContents.RunCount - 1; i >= 0; i--)
			{
				string styleName = tssContents.get_StringProperty(i, (int)FwTextPropType.ktptNamedStyle);
				if (foundSelf && styleName == ScrStyleNames.VerseNumber && startRef.Verse == 0)
				{
					int nVerseStart, nVerseEnd;
					ScrReference.VerseToInt(tssContents.get_RunText(i), out nVerseStart, out nVerseEnd);
					startRef.Verse = nVerseStart;
					endRef.Verse = nVerseEnd;
				}
				else if (foundSelf && styleName == ScrStyleNames.ChapterNumber && startRef.Chapter == 0)
				{
					try
					{
						startRef.Chapter = endRef.Chapter =
							ScrReference.ChapterToInt(tssContents.get_RunText(i));
					}
					catch (ArgumentException)
					{
						// ignore runs with invalid Chapter numbers
					}
					if (startRef.Verse == 0)
						startRef.Verse = endRef.Verse = 1;
				}
				else if (styleName == null)
				{
					IScrFootnote footnote = (IScrFootnote)footnoteRepo.GetFootnoteFromObjData(tssContents.get_StringProperty(i, (int)FwTextPropType.ktptObjData));
					if (footnote != null)
					{
						if (footnote == this)
						{
							foundSelf = true;
							continue;
						}
						RefRange otherFootnoteLocation = ((ScrFootnote)footnote).FootnoteRefInfo_Internal;
						if (foundSelf && otherFootnoteLocation != RefRange.EMPTY)
						{
							// Found another footnote with a reference we can use
							if (startRef.Verse == 0)
							{
								startRef.Verse = otherFootnoteLocation.StartRef.Verse;
								endRef.Verse = otherFootnoteLocation.EndRef.Verse;
							}

							if (startRef.Chapter == 0)
							{
								startRef.Chapter = otherFootnoteLocation.StartRef.Chapter;
								endRef.Chapter = otherFootnoteLocation.EndRef.Chapter;
							}
						}
						else if (foundSelf)
						{
							// Previous footnote does not have a reference yet. We presume, for performance
							// reasons, that none of the previous footnotes have valid references yet, so
							// we set all the footnotes for the book.
							((ScrBook)owningBook).RefreshFootnoteRefs();
							return RefResult.ScannedAllFootnotes;
						}
					}
				}

				if (startRef.Verse != 0 && endRef.Verse != 0 && startRef.Chapter != 0 &&
					endRef.Chapter != 0)
				{
					return RefResult.Found;
				}
			}
			return RefResult.NotFound;
		}

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the RefRange containing meta-info for the footnote (Scripture refs)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal RefRange FootnoteRefInfo
		{
			get
			{
				if (FootnoteRefInfo_Internal == RefRange.EMPTY)
					UpdateFootnoteRef();
				return FootnoteRefInfo_Internal;
			}

			set
			{
				if (value == null)
					throw new ArgumentNullException("value");
				lock (SyncRoot)
					m_location = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the RefRange containing meta-info for the footnote (Scripture refs).
		/// Returns the actual value (i.e. will not try to update if it's empty)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private RefRange FootnoteRefInfo_Internal
		{
			get
			{
				lock (SyncRoot)
					return m_location;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a string representing the reference of the footnote (chapter:verse + trailing
		/// space)
		/// NOTE: This is formatted for the default vernacular writing system.
		/// </summary>
		/// <returns>
		/// The reference of this footnote, or an empty string if no paragraph was found.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public string RefAsString
		{
			get { return GetReference(m_cache.DefaultVernWs); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is used to get reference information irrespective of display settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IgnoreDisplaySettings
		{
			get
			{
				lock (SyncRoot)
					return m_fIgnoreDisplaySettings;
			}
			set
			{
				lock (SyncRoot)
					m_fIgnoreDisplaySettings = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the start ref.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrReference StartRef
		{
			get
			{
				RefRange footnoteEntry = FootnoteRefInfo;
				return new ScrReference(footnoteEntry.StartRef, m_scr.Versification);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the end ref.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrReference EndRef
		{
			get
			{
				RefRange footnoteEntry = FootnoteRefInfo;
				return new ScrReference(footnoteEntry.EndRef, m_scr.Versification);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the paragraph style name of the first paragraph in the footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ParaStyleId
		{
			get
			{
				return (ParagraphsOS.Count == 0) ? ScrStyleNames.NormalFootnoteParagraph :
					ParagraphsOS[0].StyleName;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overridden because this property is dictated by the Scripture object for Scripture
		/// footnotes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool DisplayFootnoteMarker
		{
			get { return m_scr.GetDisplayFootnoteMarker(ParaStyleId); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type of the footnote.
		/// </summary>
		/// <value>The type of the footnote.</value>
		/// ------------------------------------------------------------------------------------
		public override FootnoteMarkerTypes MarkerType
		{
			get { return m_scr.DetermineFootnoteMarkerType(ParaStyleId); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overridden because this property is dictated by the Scripture object for Scripture
		/// footnotes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool DisplayFootnoteReference
		{
			get { return m_scr.GetDisplayFootnoteReference(ParaStyleId); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overridden to defer to the Scripture object when getting the marker
		/// </summary>
		/// <remarks>The WS of the marker will be the default VERN writing system</remarks>
		/// ------------------------------------------------------------------------------------
		public override ITsString FootnoteMarker
		{
			get
			{
				ITsStrBldr bldr = MakeFootnoteMarker(m_cache.DefaultVernWs);
				return bldr.GetString();
			}
			set
			{
				base.FootnoteMarker = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the marker text (a plain string).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string MarkerAsString
		{
			get
			{
				return (MarkerType == FootnoteMarkerTypes.AutoFootnoteMarker) ?
					((char)('a' + (MarkerIndex % 26))).ToString() :
					m_scr.GetFootnoteMarker(ParaStyleId, base.FootnoteMarker).Text;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the footnote marker.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int MarkerIndex
		{
			get
			{
				int index = 0;
				foreach (IScrFootnote bookFootnote in ((IScrBook)Owner).FootnotesOS)
				{
					if (((ScrFootnote)bookFootnote).MarkerType == FootnoteMarkerTypes.AutoFootnoteMarker)
					{
						if (bookFootnote == this)
							return index;
						index++;
					}
				}
				return -1;
			}
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a string representing the reference of the footnote (chapter:verse + trailing
		/// space)
		/// </summary>
		/// <param name="hvoWs">HVO of the writing system for which we are formatting this
		/// reference.</param>
		/// ------------------------------------------------------------------------------------
		public string GetReference(int hvoWs)
		{
			if (!IgnoreDisplaySettings && !m_scr.GetDisplayFootnoteReference(ParaStyleId))
				return string.Empty;
			RefRange footnoteEntry = FootnoteRefInfo;
			// Don't display the reference if the reference is for an intro
			if (footnoteEntry.StartRef.Verse == 0 && footnoteEntry.EndRef.Verse == 0)
				return string.Empty;

			return m_scr.ChapterVerseBridgeAsString(footnoteEntry.StartRef,
				footnoteEntry.EndRef, hvoWs) + " ";
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the section that this footnote belongs to (if any).
		/// Returns null if it no longer is found in a paragraph text property (LTB-408),
		/// or if it doesn't belong to a section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool TryGetContainingSection(out IScrSection section)
		{
			section = null;
			IStText stText;
			if (TryGetContainingStText(out stText))
			{
				if (stText.Owner is IScrSection)
					section = (IScrSection)stText.Owner;
			}
			return section != null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the title (StText) that this footnote belongs to (if any).
		/// Returns 0 if it no longer is found in a paragraph text property (LTB-408),
		/// or if it doesn't belong to a title.
		/// </summary>
		/// <param name="title">The title.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool TryGetContainingTitle(out IStText title)
		{
			title = null;
			IStText stText;
			if (TryGetContainingStText(out stText))
			{
				if (stText.Owner is IScrBook)
					title = stText;
			}
			return title != null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes the footnote marker.
		/// </summary>
		/// <param name="markerWS">The WS of the marker</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ITsStrBldr MakeFootnoteMarker(int markerWS)
		{
			// create a TsString to hold the marker
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, MarkerAsString, StyleUtils.CharStyleTextProps(ScrStyleNames.FootnoteMarker, markerWS));
			return strBldr;
		}
		#endregion

		#region Private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempts to get the StText containing this footnote ORC
		/// </summary>
		/// <param name="stText"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool TryGetContainingStText(out IStText stText)
		{
			stText = null;
			if (ParaContainingOrcRA == null)
				return false;

			stText = (IStText)ParaContainingOrcRA.Owner;
			return stText != null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes markers that reference the current footnote in the text and back translations.
		/// This also recalculates the footnote markers of following footnotes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DeleteFootnoteMarker()
		{
			// Get paragraph containing footnote and search for run having footnote ref.
			IScrTxtPara para = ParaContainingOrcRA;
			if (para != null && para.IsValidObject)
			{
				para.DeleteAnyBtMarkersForObject(this.Guid);
				ITsString tssContent = para.Contents;
				TsRunInfo runInfo = new TsRunInfo();
				int i;
				for (i = 0; i < tssContent.RunCount; i++)
				{
					ITsTextProps textProps = tssContent.FetchRunInfo(i, out runInfo);
					string strGuid =
						textProps.GetStrPropValue((int)FwTextPropType.ktptObjData);
					if (strGuid != null)
					{
						Guid guid = MiscUtils.GetGuidFromObjData(strGuid.Substring(1));
						if (this.Guid == guid)
							break;
					}
				}

				if (i < tssContent.RunCount)
				{
					// We found the footnote marker
					// Remove footnote ref from paragraph - then delete the footnote
					ITsStrBldr bldr = tssContent.GetBldr();
					bldr.Replace(runInfo.ichMin, runInfo.ichLim, string.Empty, null);
					para.Contents = bldr.GetString();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if a character in the footnote marker needs to wrap from 'z' to 'a'
		/// </summary>
		/// <param name="markerChar">Marker character to test.</param>
		/// <param name="nextChar">The correct marker character that should follow the one
		/// being tested.</param>
		/// <returns><c>true</c> if markerChar was wrapped from 'z' to 'a'</returns>
		/// ------------------------------------------------------------------------------------
		private static bool ShouldMarkerCharWrap(char markerChar, out char nextChar)
		{
			if (markerChar == 'z')
			{
				nextChar = 'a';
				return true;
			}

			nextChar = (char)(markerChar + 1);
			return false;
		}
		#endregion
	}
	#endregion
}
