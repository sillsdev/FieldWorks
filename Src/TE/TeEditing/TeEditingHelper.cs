// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2004' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeEditingHelper.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.ComponentModel;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.Utils;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.CoreImpl;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class contains editing methods shared by different TE views.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TeEditingHelper : FwEditingHelper
	{
		#region Member variables
		/// <summary></summary>
		protected IScripture m_scr;
		private readonly IApp m_app;
		private readonly TeViewType m_viewType;
		private readonly int m_filterInstance;
		private FilteredScrBooks m_bookFilter;
		private InsertVerseMessageFilter m_InsertVerseMessageFilter;
		private Cursor m_restoreCursor;
		private SelectionHelper m_lastFootnoteTextRepSelection;
		private int m_newFootnoteIndex;
		private bool m_selectionUpdateInProcess;
		private ScrReference m_oldReference;
		private string m_sPrevSelectedText;
		private ILgCharacterPropertyEngine m_cpe;
		private StVc.ContentTypes m_contentType;

		private readonly IStTextRepository m_repoStText;
		private readonly IScrTxtParaRepository m_repoScrTxtPara;
		private readonly IScrBookRepository m_repoScrBook;
		private readonly IScrSectionRepository m_repoScrSection;
		private readonly IScrFootnoteRepository m_repoScrFootnote;
		private readonly ICmTranslationRepository m_repoCmTrans;
		private readonly IWritingSystemContainer m_wsContainer;
		private readonly ICmObjectRepository m_repoCmObject;
		private readonly ISegmentRepository m_repoSegment;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This constructor is for testing so the class can be mocked.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TeEditingHelper()
			: base(null, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TeEditingHelper"/> class.
		/// </summary>
		/// <param name="callbacks">The callbacks.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="filterInstance">The filter instance.</param>
		/// <param name="viewType">Type of the view.</param>
		/// <param name="app">The app.</param>
		/// ------------------------------------------------------------------------------------
		public TeEditingHelper(IEditingCallbacks callbacks, FdoCache cache, int filterInstance,
			TeViewType viewType, IApp app)
			: base(cache, callbacks)
		{
			if (m_cache == null)
				throw new ArgumentNullException("cache");

			m_scr = m_cache.LangProject.TranslatedScriptureOA;
			m_app = app;

			IFdoServiceLocator servloc = m_cache.ServiceLocator;
			m_repoStText = servloc.GetInstance<IStTextRepository>();
			m_repoScrTxtPara = servloc.GetInstance<IScrTxtParaRepository>();
			m_repoScrBook = servloc.GetInstance<IScrBookRepository>();
			m_repoScrSection = servloc.GetInstance<IScrSectionRepository>();
			m_repoScrFootnote = servloc.GetInstance<IScrFootnoteRepository>();
			m_repoCmTrans = servloc.GetInstance<ICmTranslationRepository>();
			m_wsContainer = servloc.WritingSystems;
			m_repoCmObject = servloc.GetInstance<ICmObjectRepository>();
			m_repoSegment = servloc.GetInstance<ISegmentRepository>();

			m_viewType = viewType;
			m_filterInstance = filterInstance;
			PasteFixTssEvent += RemoveHardFormatting;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Used to set the content type, when the view we are 'helping' is some sort of BT.
		/// Enhance JohnT: if we extend the ViewTypes we may be able to determine this from it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StVc.ContentTypes ContentType
		{
			get { return m_contentType; }
			set { m_contentType = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the hard formatting from text that is pasted.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:SIL.FieldWorks.Common.RootSites.FwPasteFixTssEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		public void RemoveHardFormatting(EditingHelper sender, FwPasteFixTssEventArgs e)
		{
			ITsString tss = e.TsString;
			ITsStrBldr tssBldr = tss.GetBldr();
			for (int i = 0; i < tss.RunCount; i++)
			{
				TsRunInfo runInfo;
				ITsTextProps props = tss.FetchRunInfo(i, out runInfo);
				ITsPropsBldr propsBuilder = TsPropsBldrClass.Create();

				// Copy the style name from the run properties
				string style = props.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
				if (style != null)
					propsBuilder.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, style);

				// Copy the WS information from the run properties
				int var;
				int ws = props.GetIntPropValues((int)FwTextPropType.ktptWs, out var);
				if (ws > 0)
					propsBuilder.SetIntPropValues((int)FwTextPropType.ktptWs, var, ws);

				// Copy the ORC information from the run properties
				string orcData = props.GetStrPropValue((int)FwTextPropType.ktptObjData);
				if (orcData != null)
					propsBuilder.SetStrPropValue((int)FwTextPropType.ktptObjData, orcData);

				tssBldr.SetProperties(runInfo.ichMin, runInfo.ichLim, propsBuilder.GetTextProps());
			}

			e.TsString = tssBldr.GetString();
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			// Must not be run more than once.
			if (IsDisposed)
			{
				Debug.Assert(m_cpe == null);
				return;
			}

			if (disposing)
			{
				// Log disposing event - removed logging as part of fix for TE-6551
				//string message = "Disposing TeEditingHelper...\n" +
				//    "Stack Trace:\n" + Environment.StackTrace;
				//SIL.Utils.Logger.WriteEvent(message);

				// Dispose managed resources here.
				if (m_InsertVerseMessageFilter != null)
					Application.RemoveMessageFilter(m_InsertVerseMessageFilter);
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			PasteFixTssEvent -= RemoveHardFormatting;
			m_scr = null;
			m_bookFilter = null;
			m_InsertVerseMessageFilter = null;
			m_restoreCursor = null;
			m_lastFootnoteTextRepSelection = null;
			m_oldReference = null;
				m_cpe = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If this helper is for a view which can display segmented back translations. It is
		/// used to highlight the relevant sentence when the selection is in a back translation.
		/// Note: this is NOT the view to which this helper belongs!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITeDraftView VerncaularDraftView
		{
			get
			{
				return (Control is ITeDraftView) ? ((ITeDraftView)Control).VernacularDraftView : null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If this helper is for a view which can display segmented back translations, this is
		/// the VC which displays the vernacular draft view. It is used to highlight the
		/// relevant segment when the selection is in a back translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private TeStVc VernacularDraftVc
		{
			get
			{
				if (VerncaularDraftView == null)
					return null;
				return VerncaularDraftView.Vc;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the TeViewType of this view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TeViewType ViewType
		{
			get
			{
				CheckDisposed();
				return m_viewType;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the TeViewType of the printable view corresponding to this view. Note that the
		/// returned TeViewType may not correspond to a real view, created or otherwise.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TeViewType CorrespondingPrintableViewType
		{
			get
			{
				CheckDisposed();
				// For BT draft view we have two draft windows - the BT side has both Scripture and BT set
				if ((m_viewType & (TeViewType.Scripture | TeViewType.BackTranslation)) ==
					(TeViewType.Scripture | TeViewType.BackTranslation))
				{
					return (m_viewType | TeViewType.Print) & ~(TeViewType.Draft | TeViewType.Scripture);
				}
				return ((m_viewType | TeViewType.Print) & ~TeViewType.Draft);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Counts the bits set to one.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>Number of set bits</returns>
		/// ------------------------------------------------------------------------------------
		private static int CountSetBits(int value)
		{
			int ones = 0;
			while (value > 0)
			{
				if ((value & 1) != 0)
					ones++;
				value >>= 1;
			}

			return ones;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the view type string.
		/// </summary>
		/// <value>The view type string.</value>
		/// ------------------------------------------------------------------------------------
		public static string ViewTypeString(TeViewType type)
		{
			StringBuilder bldr = new StringBuilder();

			// Content types
			if ((type & TeViewType.BackTranslation) != 0)
				bldr.Append(TeViewType.BackTranslation);
			if ((type & TeViewType.Scripture) != 0)
				bldr.Append(TeViewType.Scripture);
			if ((type & TeViewType.Checks) != 0)
				bldr.Append(TeViewType.Checks);

			// If we have a scripture or BT view, Draft or Print has to be set
			Debug.Assert((type & (TeViewType.Draft | TeViewType.Print)) != 0 ||
				(type & (TeViewType.Scripture | TeViewType.BackTranslation)) == 0);
			if ((type & TeViewType.Draft) != 0)
				bldr.Append(TeViewType.Draft);
			else if ((type & TeViewType.Print) != 0)
				bldr.Append(TeViewType.Print);

			// If we have a scripture or BT view, Horizontal or Vertical has to be set
			Debug.Assert((type & (TeViewType.Horizontal | TeViewType.Vertical)) != 0 ||
				(type & (TeViewType.Scripture | TeViewType.BackTranslation)) == 0);
			if ((type & TeViewType.Vertical) != 0)
				bldr.Append(TeViewType.Vertical);
			else if ((type & TeViewType.Horizontal) != 0)
				bldr.Append(TeViewType.Horizontal);

			// View types
			Debug.Assert(CountSetBits((int)(type & TeViewType.ViewTypeMask)) == 1,
				"Need exactly one view type");
			TeViewType viewType = (type & TeViewType.ViewTypeMask);
			bldr.Append(viewType);

			return bldr.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the current book filter
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FilteredScrBooks BookFilter
		{
			get
			{
				CheckDisposed();

				if (m_bookFilter == null)
					m_bookFilter = m_cache.ServiceLocator.GetInstance<IFilteredScrBookRepository>().GetFilterInstance(m_filterInstance);
				return m_bookFilter;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether the current selection is a valid place to insert a chapter or verse
		/// number.
		/// </summary>
		/// TE-2740: Don't allow numbers to be inserted in intro material
		/// TE-8478: Don't allow numbers to be inserted into picture captions
		/// ------------------------------------------------------------------------------------
		public virtual bool CanInsertNumberInElement
		{
			get
			{
				CheckDisposed();

				int tag;
				int hvoSel;
				return (CurrentSelection != null &&
					GetSelectedScrElement(out tag, out hvoSel) &&
					tag == ScrSectionTags.kflidContent &&
					!InSegmentedBt &&
					!IsInIntroSection(SelectionHelper.SelLimitType.Top) &&
					!IsPictureSelected &&
					CanReduceSelectionToIpAtTop);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the selection is in a user prompt.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsSelectionInUserPrompt
		{
			get { return IsSelectionInPrompt(CurrentSelection); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns whether or not the selection is in a user prompt. (Unless it is in a
		/// picture..I (JohnT) am not sure how a picture selection comes to seem to be in a user
		/// prompt, but see TE-7813: double-clicking one crashes here if we don't check.)
		/// </summary>
		/// <param name="helper">The selection to check.</param>
		/// <returns>true if the selection is in a user prompt; otherwise, false.</returns>
		/// ------------------------------------------------------------------------------------
		private static bool IsSelectionInPrompt(SelectionHelper helper)
		{
			return (helper != null && helper.Selection.SelType != VwSelType.kstPicture &&
				helper.GetTextPropId(SelectionHelper.SelLimitType.Anchor) == SimpleRootSite.kTagUserPrompt &&
				helper.GetTextPropId(SelectionHelper.SelLimitType.End) == SimpleRootSite.kTagUserPrompt);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether the selection is in a segmented BT segment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool InSegmentedBt
		{
			get
			{
				if ((ViewType & TeViewType.BackTranslationParallelPrint) != TeViewType.BackTranslationParallelPrint)
					return m_contentType == StVc.ContentTypes.kctSegmentBT;

				if (CurrentSelection == null)
					return false;
				int textTag = CurrentSelection.GetTextPropId(SelectionHelper.SelLimitType.Top);
				if (textTag == SegmentTags.kflidFreeTranslation || textTag == StTxtParaTags.kflidSegments)
					return true;
				if (textTag != SimpleRootSite.kTagUserPrompt)
					return false;
				// Selection is in a prompt...is it a BT prompt?
				SelLevInfo[] vsli = CurrentSelection.LevelInfo;
				return (vsli.Length > 0 && vsli[0].tag == StTxtParaTags.kflidSegments);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the current view is a trial publication view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsTrialPublicationView
		{
			get
			{
				return ((ViewType & (TeViewType.Scripture | TeViewType.View2 | TeViewType.Print))
					== (TeViewType.Scripture | TeViewType.View2 | TeViewType.Print));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is a correction view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsCorrectionView
		{
			get
			{
				return (ViewType & (TeViewType.BackTranslation | TeViewType.View3)) ==
					(TeViewType.BackTranslation | TeViewType.View3);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance can reduce selection to ip at top.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance can reduce selection to ip at top; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		protected virtual bool CanReduceSelectionToIpAtTop
		{
			get { return (GetSelectionReducedToIp(SelectionHelper.SelLimitType.Top) != null); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if a chapter number can be inserted at the current IP.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool CanInsertChapterNumber
		{
			get
			{
				CheckDisposed();

				if (!CanInsertNumberInElement)
					return false;

				// Get the paragraph text where the selection is
				ITsString paraString;
				int ich;
				bool fAssocPrev;
				int hvoObj;
				int wsAlt; //the WS of the multiString alt, if selection is in a back translation
				int propTag;
				SelectionHelper selHelperIP = GetSelectionReducedToIp(SelectionHelper.SelLimitType.Top);
				if (selHelperIP == null)
					return false;
				selHelperIP.Selection.TextSelInfo(false, out paraString, out ich, out fAssocPrev,
					out hvoObj, out propTag, out wsAlt);

				// We allow "inserting" chapter numbers inside or next to existing chapter numbers
				// in BT's because it really does an update to "fix" them if necessary.
				if (propTag == CmTranslationTags.kflidTranslation)
					return true;

				// Get the style names to the left and right of the IP
				ITsTextProps ttp;
				string leftStyle = null;
				string rightStyle = null;
				//				int ipLocation = selHelperIP.IchAnchor;
				int ipLocation = selHelperIP.GetIch(SelectionHelper.SelLimitType.Top);
				if (ipLocation > 0)
				{
					ttp = paraString.get_PropertiesAt(ipLocation - 1);
					leftStyle = ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
				}
				if (ipLocation <= paraString.Length)
				{
					ttp = paraString.get_PropertiesAt(ipLocation);
					rightStyle = ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
				}

				// If the left or right style is chapter number then don't allow insertion
				if (leftStyle == ScrStyleNames.ChapterNumber || rightStyle == ScrStyleNames.ChapterNumber)
					return false;

				// If both the left and right styles are verse numbers then don't allow insertion
				if (leftStyle == ScrStyleNames.VerseNumber && rightStyle == ScrStyleNames.VerseNumber)
					return false;

				return true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the index of the book for the current selection. Returns -1 if the
		/// selection is undefined, or something else more destructive.
		/// </summary>
		/// <remarks>
		/// Since a selection can cross StText bounds, it is possible for the anchor of the
		/// selection to be in a book title and the end to be in a Scripture section, or vice
		/// versa. In this case this property will return information based strictly on the
		/// location of the anchor. When setting the BookIndex, the insertion point will be
		/// placed at the beginning of the book's title.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual int BookIndex
		{
			get
			{
				CheckDisposed();
				return GetBookIndex(SelectionHelper.SelLimitType.Anchor);
			}
			set
			{
				CheckDisposed();
				if (value >= 0 && value < BookFilter.BookCount)
					SetInsertionPoint(ScrBookTags.kflidTitle, value, 0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the book.
		/// </summary>
		/// <param name="selLimitType">The end of the selection considered (top, bottom, end,
		/// anchor)</param>
		/// <remarks>Virtual to allow tests to mock the implementation</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual int GetBookIndex(SelectionHelper.SelLimitType selLimitType)
		{
			return CurrentSelection == null ? -1 :
				((ITeView)Control).LocationTracker.GetBookIndex(CurrentSelection, selLimitType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the section for the current selection. Returns -1 if the selection
		/// is not in a section.
		/// </summary>
		/// <remarks>
		/// Since a selection can cross StText bounds, it is possible for the anchor of the
		/// selection to be in a book title and the end to be in a Scripture section, or vice
		/// versa. In this case this property will return information based strictly on the
		/// location of the anchor.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual int SectionIndex
		{
			get
			{
				CheckDisposed();
				return GetSectionIndex(SelectionHelper.SelLimitType.Anchor);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the section.
		/// </summary>
		/// <param name="selLimitType">The end of the selection considered (top, bottom, end,
		/// anchor)</param>
		/// ------------------------------------------------------------------------------------
		public int GetSectionIndex(SelectionHelper.SelLimitType selLimitType)
		{
			return ((ITeView)Control).LocationTracker.GetSectionIndexInBook(
				CurrentSelection, selLimitType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the paragraph where the insertion point is located.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int ParagraphIndex
		{
			get
			{
				CheckDisposed();

				if (CurrentSelection == null)
					return 0;
				return CurrentSelection.GetLevelInfoForTag(StTextTags.kflidParagraphs).ihvo;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing system of the view constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int ViewConstructorWS
		{
			get
			{
				CheckDisposed();

				// To support back translations in multiple languages, need to choose the
				// correct default WS.
				if (CurrentSelection != null && CurrentSelection.LevelInfo.Length > 0)
				{
					// try getting WS from the selection - might be front or back translation
					// in side-by-side BT print layout
					int hvo = CurrentSelection.LevelInfo[0].hvo;
					return Callbacks.GetWritingSystemForHvo(hvo);
				}

				return RootVcDefaultWritingSystem;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current rootbox's view constructor's default writing system, or the default
		/// analysis WS if the rootbox's VC is not an StVC.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual int RootVcDefaultWritingSystem
		{
			get
			{
				StVc stvc = ViewConstructor as StVc;
				return stvc == null ? m_cache.DefaultAnalWs : stvc.DefaultWs;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provides simple access to the start ref at the current insertion point.
		/// </summary>
		/// <remarks>
		/// This property is not guaranteed to return a ScrReference containing the book, chapter,
		/// AND verse.  It will return as much as it can, but not neccessarily all of it. It
		/// will not search back into a previous section if it can't find the verse number in
		/// the current section. This means that if a verse crosses a section break, the verse
		/// number will be inferred from the section start ref. For now, section refs are not
		/// very reliable, but this should work pretty well when TE-278 and TE-329 are
		/// completed.
		/// ENHANCE: Consider checking the end of the previous section if
		/// the verse number isn't found, and if it's still in the same chapter, return the
		/// verse number from the end of the previous section instead.
		/// The Word of God will not return void. - Isaiah 55:11.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual ScrReference CurrentStartRef
		{
			get
			{
				CheckDisposed();

				ScrReference[] curRef = GetCurrentRefRange(CurrentSelection,
					SelectionHelper.SelLimitType.Anchor);
				return curRef[0];
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provides simple access to the end ref at the current insertion point.
		/// </summary>
		/// <remarks>
		/// This property is not guaranteed to return a ScrReference containing the book, chapter,
		/// AND verse.  It will return as much as it can, but not neccessarily all of it. It
		/// will not search back into a previous section if it can't find the verse number in
		/// the current section. This means that if a verse crosses a section break, the verse
		/// number will be inferred from the section start ref. For now, section refs are not
		/// very reliable, but this should work pretty well when TE-278 and TE-329 are
		/// completed.
		/// ENHANCE: Consider checking the end of the previous section if
		/// the verse number isn't found, and if it's still in the same chapter, return the
		/// verse number from the end of the previous section instead.
		/// The Word of God will not return void. - Isaiah 55:11.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual ScrReference CurrentEndRef
		{
			get
			{
				CheckDisposed();

				ScrReference[] curRef = GetCurrentRefRange(CurrentSelection,
					SelectionHelper.SelLimitType.Anchor);
				return curRef[1];
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Just like CurrentStartRef but returns a range of references to account for a
		/// selection in a verse bridge.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrReference[] CurrentAnchorRefRange
		{
			get
			{
				CheckDisposed();
				return GetCurrentRefRange(CurrentSelection, SelectionHelper.SelLimitType.Anchor);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Just like CurrentEndRef but returns a range of references to account for a
		/// selection in a verse bridge.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrReference[] CurrentEndRefRange
		{
			get
			{
				CheckDisposed();
				return GetCurrentRefRange(CurrentSelection, SelectionHelper.SelLimitType.End);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Common utility for the CurrentRef* properties
		/// </summary>
		/// <param name="selhelper">The selection helper representing the current selection
		/// (or sometimes the current reduced to an IP)</param>
		/// <param name="selLimit">The limit of the selection (anchor, end, etc.) to get the
		/// reference of</param>
		/// <returns>the start and end reference of the given selection, as an array of two
		/// ScrReference objects</returns>
		/// ------------------------------------------------------------------------------------
		protected ScrReference[] GetCurrentRefRange(SelectionHelper selhelper,
			SelectionHelper.SelLimitType selLimit)
		{
			if (m_cache == null || selhelper == null || BookFilter == null)
				return new ScrReference[] {ScrReference.Empty, ScrReference.Empty};

			ILocationTracker tracker = ((ITeView)Control).LocationTracker;

			// If there is a current book...
			BCVRef start = new BCVRef();
			BCVRef end = new BCVRef();
			int iBook = tracker.GetBookIndex(selhelper, selLimit);
			if (iBook >= 0 && BookFilter.BookCount > 0)
			{
				try
				{
					IScrBook book = BookFilter.GetBook(iBook);

					// if there is not a current section, then use the book and chapter/verse of 0.
					IScrSection section = tracker.GetSection(selhelper, selLimit);
					if (section != null)
					{
						// If there is a section...
						int paraHvo = selhelper.GetLevelInfoForTag(StTextTags.kflidParagraphs, selLimit).hvo;
						IScrTxtPara scrPara = m_cache.ServiceLocator.GetInstance<IScrTxtParaRepository>().GetObject(paraHvo);
						// Get the ich at either the beginning or the end of the selection,
						// as specified with limit. (NB that this is relative to the property, not the whole paragraph.)
						int ich;
						// Get the TsString, whether in vern or BT
						ITsString tss;
						int refWs;
						SelLevInfo segInfo;
						int textPropTag = 0;
						if (selhelper.GetLevelInfoForTag(StTxtParaTags.kflidSegments, selLimit, out segInfo))
						{
							// selection is in a segmented BT segment. Figure the reference based on where the segment is
							// in the underlying paragraph.
							tss = scrPara.Contents; // for check below on range of ich.
							ISegment seg = m_repoSegment.GetObject(segInfo.hvo);
							ich = seg.BeginOffset;
							Debug.Assert(seg.Paragraph == scrPara);
							refWs = -1; // ich is in the paragraph itself, not some CmTranslation
						}
						else
						{
							textPropTag = selhelper.GetTextPropId(selLimit);
							if (textPropTag == SimpleRootSite.kTagUserPrompt)
							{
								ich = 0;
								tss = null;
							}
							else
							{
								ich = selhelper.GetIch(selLimit);
								tss = selhelper.GetTss(selLimit); // Get the TsString, whether in vern or BT
								if (ich < 0 || tss == null)
								{
									HandleFootnoteAnchorIconSelected(selhelper.Selection, (hvo, flid, wsDummy, ichAnchor) =>
									{
										SelectionHelper helperTemp = new SelectionHelper(selhelper);
										ich = helperTemp.IchAnchor = helperTemp.IchEnd = ichAnchor;
										helperTemp.SetSelection(false, false);
										tss = helperTemp.GetTss(selLimit);
									});
								}
							}
							refWs = GetCurrentBtWs(selLimit); // figures out whether it's in a CmTranslation or the para itself.
						}
						Debug.Assert(tss == null || ich <= tss.Length);
						if ((tss != null && ich <= tss.Length) || textPropTag == SimpleRootSite.kTagUserPrompt)
						{
							scrPara.GetRefsAtPosition(refWs, ich, true, out start, out end);

							// If the chapter number is 0, then use the chapter from the section reference
							if (end.Chapter == 0)
								end.Chapter = BCVRef.GetChapterFromBcv(section.VerseRefMin);
							if (start.Chapter == 0)
								start.Chapter = BCVRef.GetChapterFromBcv(section.VerseRefMin);
						}
					}
					else
					{
						// either it didn't find a level or it didn't find an index. Either way,
						// it couldn't find a section.
						start.Book = end.Book = book.CanonicalNum;
					}
				}
				catch
				{
					// Bummer man, something went wrong... don't sweat it though, it happens...
					// This can occur if you are in the introduction or other location that lacks
					// relevant information or other necessary stuff.
				}
			}
			return new ScrReference[] {new ScrReference(start, m_scr.Versification),
				new ScrReference(end, m_scr.Versification)}; ;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates whether the current selection is in a book title.
		/// </summary>
		/// <remarks>
		/// Since a selection can cross StText bounds, it is possible for the anchor of the
		/// selection to be in a book title and the end to be in a Scripture section, or vice
		/// versa. In this case this property will return information based strictly on the
		/// location of the anchor.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual bool InBookTitle
		{
			get
			{
				CheckDisposed();

				if (CurrentSelection == null)
					return false;
				SelLevInfo levelInfo;
				return CurrentSelection.GetLevelInfoForTag(ScrBookTags.kflidTitle, out levelInfo);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates whether the current selection is in a section head.
		/// </summary>
		/// <remarks>
		/// Since a selection can cross StText bounds, it is possible for the anchor of the
		/// selection to be in a section head and the end to be in a Scripture section, or vice
		/// versa. In this case this property will return information based strictly on the
		/// location of the anchor.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual bool InSectionHead
		{
			get
			{
				CheckDisposed();

				if (CurrentSelection == null)
					return false;

				SelLevInfo headInfo;
				return CurrentSelection.GetLevelInfoForTag(ScrSectionTags.kflidHeading, out headInfo);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates where the current selection is anchored in an introduction section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool InIntroSection
		{
			get { return IsInIntroSection(SelectionHelper.SelLimitType.Anchor); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates where the specified end of the current selection is in an introduction
		/// section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsInIntroSection(SelectionHelper.SelLimitType selLimitType)
		{
			CheckDisposed();

			if (CurrentSelection == null || Control == null)
				return false;

			IScrSection section = ((ITeView)Control).LocationTracker.GetSection(CurrentSelection,
				selLimitType);
			return (section != null) ? section.IsIntro : false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the paragraph where the insertion point is located.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private IScrTxtPara SelParagraph
		{
			get
			{
				if (CurrentSelection == null)
					return null;
				SelLevInfo paraInfo;

				if (!CurrentSelection.GetLevelInfoForTag(StTextTags.kflidParagraphs, out paraInfo))
					return null;

				return m_cache.ServiceLocator.GetInstance<IScrTxtParaRepository>().GetObject(paraInfo.hvo);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the StText where the insertion point is located, if it is in a
		/// ScrSection Content field.
		/// Returns null if IP is not in a ScrSection Content.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IStText ScrSectionContent
		{
			get
			{
				CheckDisposed();

				if (CurrentSelection == null)
					return null;

				SelLevInfo contentInfo;
				if (!CurrentSelection.GetLevelInfoForTag(ScrSectionTags.kflidContent, out contentInfo))
					return null;

				return m_repoStText.GetObject(contentInfo.hvo);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets state of "Insert Verse Number" mode.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool InsertVerseActive
		{
			get
			{
				CheckDisposed();
				return m_InsertVerseMessageFilter != null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether the current selection is in back translation data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool IsBackTranslation
		{
			get
			{
				CheckDisposed();

				if (base.IsBackTranslation)
					return true;

				if (m_viewType == TeViewType.BackTranslationParallelPrint)
				{
					SelectionHelper helper = CurrentSelection;
					if (helper != null)
					{
						if (helper.TextPropId == SimpleRootSite.kTagUserPrompt)
							return true; // In BT parallel print layout, we only show prompts on BT side

						if (helper.NumberOfLevels == 0)
							return false;

						return (helper.TextPropId == CmTranslationTags.kflidTranslation &&
							m_repoCmTrans.GetObject(helper.LevelInfo[0].hvo).TypeRA.Guid ==
							CmPossibilityTags.kguidTranBackTranslation);
					}
				}

				return (m_viewType & TeViewType.BackTranslation) != 0;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the selection is at the end of a section.
		/// </summary>
		/// <remarks>This is only valid if the current selection is in the vernacular</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual bool AtEndOfSection
		{
			get
			{
				CheckDisposed();

				SelectionHelper helper = CurrentSelection;
				if (helper == null)
					return false;

				// Check to see if the selection is at the end of the current paragraph
				SelLevInfo paraInfo;
				if (!helper.GetLevelInfoForTag(StTextTags.kflidParagraphs, out paraInfo))
					return false;

				IStTxtPara para = m_repoScrTxtPara.GetObject(paraInfo.hvo);
				if (helper.IchAnchor != para.Contents.Length)
					return false;

				// Check to see if this is the last paragraph in the section. This is checked
				// by looking at the owning section's paragraph list to see if the last
				// paragraph is the one where the selection is.
				IStText text = (IStText)para.Owner;
				if (!(text.Owner is IScrSection))
					return false;

				// Only get section if text is the content of the section
				if (text.OwningFlid != (uint)ScrSectionTags.kflidContent)
					return false;

				// Make sure we're in the last paragraph of the text
				return text.ParagraphsOS[text.ParagraphsOS.Count - 1] == para;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the selection is at the start of the first Scripture section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool AtBeginningOfFirstScriptureSection
		{
			get
			{
				CheckDisposed();

				// If there is no selection, or the selection is not at the start of a paragraph,
				// or there are no books in the book filter, then we are not at the start of the
				// first scripture section
				if (CurrentSelection == null || CurrentSelection.IchAnchor != 0 ||
					BookIndex < 0 || SectionIndex < 0)
				{
					return false;
				}

				// Get the current section. If it is an intro section then we can't be at the
				// start of a Scripture section.
				IScrBook book = BookFilter.GetBook(BookIndex);
				IScrSection section = book[SectionIndex];
				if (section.IsIntro)
					return false;

				// If the previous section is a scripture section, then we can't be at the
				// start of the first scripture section.
				IScrSection prevSection = section.PreviousSection;
				if (prevSection != null && !prevSection.IsIntro)
					return false;

				// Check to see if the selection is at the start of the current section
				SelectionHelper helper = CurrentSelection;
				SelLevInfo paraInfo = helper.GetLevelInfoForTag(StTextTags.kflidParagraphs);
				IStTxtPara para = m_repoScrTxtPara.GetObject(paraInfo.hvo);

				// Make sure that the paragraph is a heading
				IStText text = (IStText)para.Owner;
				if (text.OwningFlid != (uint)ScrSectionTags.kflidHeading)
					return false;

				// Make sure we're in the first paragraph of the text
				return (text.ParagraphsOS.IndexOf(para) == 0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the following section (if any) is a Scripture section. If this is the
		/// last section, then this returns true.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool ScriptureCanImmediatelyFollowCurrentSection
		{
			get
			{
				CheckDisposed();

				SelectionHelper helper = CurrentSelection;
				if (helper == null)
					return false;

				// Get the section from the selection
				IScrSection currSection = ((ITeView)Control).LocationTracker.GetSection(
					CurrentSelection, SelectionHelper.SelLimitType.Anchor);
				if (currSection == null)
					return false;

				// Get the book that owns the section
				IScrBook book = (IScrBook)currSection.Owner;

				// Inserting a Scripture section after the last section in the book is always kosher.
				if (currSection == book[book.SectionsOS.Count - 1])
					return true;

				// Find the position of the section in the list of sections
				int idx = book.SectionsOS.IndexOf(currSection);
				// See if the next one is a scripture section.
				if (BCVRef.GetVerseFromBcv(book[idx + 1].VerseRefMax) != 0) // Scripture always has verse # >= 1
					return true;

				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the Unicode character properties engine.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		private ILgCharacterPropertyEngine UnicodeCharProps
		{
			get
			{
				if (m_cpe == null)
					m_cpe = m_cache.ServiceLocator.UnicodeCharProps;
				return m_cpe;
			}
		}
		#endregion

		#region Overrides of FwEditingHelper
		/// <summary>
		/// Override to suppress tab key. The default implementation is designed to move between
		/// 'fields' by finding a subsequent selection in a different property. All the text in
		/// TE is in the same property, so it searches the whole document, which is very slow.
		/// See TE-8117 for other possible behaviors we might implement one day.
		/// </summary>
		protected override bool CallOnExtendedKey(int chw, VwShiftStatus ss)
		{
			if (chw == 9) // tab
				return false;
			return base.CallOnExtendedKey(chw, ss);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the char style combo box should always be refreshed on selection
		/// changes.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if [force char style combo refresh]; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		public override bool ForceCharStyleComboRefresh
		{
			get
			{
				CheckDisposed();
				return (ViewType == TeViewType.BackTranslationParallelPrint);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the applicable style contexts which will be passed to the Apply Style dialog
		/// and used for populating the Character style list on the toolbar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override List<ContextValues> ApplicableStyleContexts
		{
			get
			{
				CheckDisposed();

				IScrTxtPara para = SelParagraph;
				if (para == null)
					return null;

				List<ContextValues> allowedContexts = new List<ContextValues>();
				allowedContexts.Add(ContextValues.General);
				if (para.Context != ContextValues.General)
					allowedContexts.Add(para.Context);
				if (InternalContext != ContextValues.General)
					allowedContexts.Add(InternalContext);
				if (IsBackTranslation)
					allowedContexts.Add(ContextValues.BackTranslation);

				return allowedContexts;
			}
			set
			{
				CheckDisposed();
				base.ApplicableStyleContexts = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a mouse-down event.
		/// Inserts a verse number if "insert verse" mode is active.
		/// </summary>
		/// <remarks>
		/// Called when the Zebra is killed
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public override void HandleMouseDown()
		{
			CheckDisposed();

			if (InsertVerseActive)
			{
				if (m_InsertVerseMessageFilter != null)
					m_InsertVerseMessageFilter.InsertVerseInProgress = true;
				string undo, redo;
				TeResourceHelper.MakeUndoRedoLabels("kstidInsertVerseNumber", out undo, out redo);
				using (UndoTaskHelper undoHelper = new UndoTaskHelper(
					Cache.ServiceLocator.GetInstance<IActionHandler>(), Callbacks.EditedRootBox.Site,
					undo, redo))
				{
					InsertVerseNumber();
					undoHelper.RollBack = false;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the current view is a Scripture view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool IsScriptureView
		{
			get
			{
				return (ViewType & TeViewType.Scripture) != 0;
			}
		}
		#endregion

		#region Overrides of EditingHelper/RootSiteEditingHelper
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the caption props.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override ITsTextProps CaptionProps
		{
			get
			{
				CheckDisposed();
				ITsPropsBldr bldr = TsPropsBldrClass.Create();
				bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, ScrStyleNames.Figure);
				return bldr.GetTextProps();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the cutting of text into the clipboard is possible.
		/// </summary>
		/// <returns>
		/// Returns <c>true</c> if cutting is possible.
		/// </returns>
		/// <remarks>Formerly <c>AfVwRootSite::CanCut()</c>.</remarks>
		/// ------------------------------------------------------------------------------------
		public override bool CanCut()
		{
			// make sure we can't cut when a user prompt is selected
			if (CurrentSelection == null)
				return base.CanCut();

			int anchorTextPropId = CurrentSelection.GetTextPropId(SelectionHelper.SelLimitType.Anchor);
			int endTextPropId = CurrentSelection.GetTextPropId(SelectionHelper.SelLimitType.End);
			return base.CanCut() && anchorTextPropId != SimpleRootSite.kTagUserPrompt &&
				endTextPropId != SimpleRootSite.kTagUserPrompt;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the copying of text into the clipboard is possible.
		/// </summary>
		/// <returns>
		/// Returns <c>true</c> if copying is possible.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool CanCopy()
		{
			// make sure we can't copy when a user prompt is selected
			if (CurrentSelection == null)
				return base.CanCopy();

			int anchorTextPropId = CurrentSelection.GetTextPropId(SelectionHelper.SelLimitType.Anchor);
			int endTextPropId = CurrentSelection.GetTextPropId(SelectionHelper.SelLimitType.End);
			return base.CanCopy() && anchorTextPropId != SimpleRootSite.kTagUserPrompt &&
				endTextPropId != SimpleRootSite.kTagUserPrompt;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the pasting of text from the clipboard is possible.
		/// </summary>
		/// <returns>
		/// Returns <c>true</c> if pasting is possible.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool CanPaste()
		{
			if (base.CanPaste())
			{
				// Currently, can not paste if the current selection covers more than one paragraph.
				// The deletion of the range will cause the a new selection to be requested at the
				// end of the UOW which will destroy the current selection. This could be solved by
				// splitting the UOW inot 2 parts, but it was decided not to do this right now.
				SelLevInfo anchorInfo;
				SelLevInfo endInfo;
				if (CurrentSelection.GetLevelInfoForTag(StTextTags.kflidParagraphs,
					SelectionHelper.SelLimitType.Anchor, out anchorInfo) &&
					CurrentSelection.GetLevelInfoForTag(StTextTags.kflidParagraphs,
					SelectionHelper.SelLimitType.End, out endInfo))
				{
					return anchorInfo.hvo == endInfo.hvo;
				}
			}
			return false;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new object, given a text representation (e.g., from the clipboard), unless
		/// we are in a footnote or in back translation.
		/// </summary>
		/// <param name="cache">FDO cache representing the DB connection to use</param>
		/// <param name="sTextRep">Text representation of object</param>
		/// <param name="selDst">Provided for information in case it's needed to generate
		/// the new object (E.g., footnotes might need it to generate the proper sequence
		/// letter)</param>
		/// <param name="kodt">The object data type to use for embedding the new object
		/// </param>
		/// <returns>The GUID of the new object, or <c>Guid.Empty</c> if it's illegal to
		/// insert an object in this location.</returns>
		/// ------------------------------------------------------------------------------------
		public override Guid MakeObjFromText(FdoCache cache, string sTextRep,
			IVwSelection selDst, out int kodt)
		{
			CheckDisposed();

			try
			{
				// Prevent objects from getting created in invalid locations: for example,
				// we are in a footnote, a picture caption or the back translation
				int tagSelection;
				int hvoSelection;
				GetSelectedScrElement(out tagSelection, out hvoSelection);
				if (tagSelection == ScrBookTags.kflidFootnotes ||
					tagSelection == 0 || (m_viewType & TeViewType.FootnoteView) != 0 ||
					IsPictureSelected)
				{
					kodt = 0;
					return Guid.Empty;
				}

				// "Owned" footnotes can't be pasted in a BT, but the base class might be able to
				// paste other things (such as pictures or un-owned footnotes)
				if (!IsBackTranslation && !CmPictureServices.IsTextRepOfPicture(sTextRep))
				{
					IScrBook book = GetCurrentBook(cache);
					if (m_lastFootnoteTextRepSelection == CurrentSelection)
						m_newFootnoteIndex++; // same selection, increment footnote index
					else
						m_newFootnoteIndex = FindFootnotePosition(book, CurrentSelection);

					// try to make footnote
					IScrFootnote footnote =
						m_cache.ServiceLocator.GetInstance<IScrFootnoteFactory>().CreateFromStringRep(book,
						sTextRep, m_newFootnoteIndex, ScrStyleNames.FootnoteMarker);
					kodt = (int)FwObjDataTypes.kodtOwnNameGuidHot;
					m_lastFootnoteTextRepSelection = CurrentSelection;
					return footnote.Guid;
				}
			}
			catch (ArgumentException)
			{
				// try a different type
			}

			return base.MakeObjFromText(cache, sTextRep, selDst, out kodt);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current book.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <returns>The current book.</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual IScrBook GetCurrentBook(FdoCache cache)
		{
			return ((ITeView)Control).LocationTracker.GetBook(CurrentSelection,
				SelectionHelper.SelLimitType.Anchor);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overrides the OnKeyPress event in Rootsite to handle typing in a chapter number or
		/// other special cases. Also ensures that the change is wrapped in a single UndoTask
		/// with whatever the annotation adjuster does.
		/// </summary>
		/// <param name="e"></param>
		/// <param name="modifiers"></param>
		/// ------------------------------------------------------------------------------------
		public override void OnKeyPress(KeyPressEventArgs e, Keys modifiers)
		{
			CheckDisposed();

			// We shouldn't ever handle key presses in the editing helper if we can't edit anyway
			if (!Editable)
			{
				base.OnKeyPress(e, modifiers);
				return;
			}

			// ENHANCE (TomB/BryanW/TimS): If the user types a number, followed immediately
			// by a non-numeric character, the CollectTypedInput method could mess us up
			// because the alpha character would never get fed into this method. The result
			// would be that the alpha char(s) would get added to the chapter number and
			// the user will have to remove the formatting manually. If this becomes a
			// major issue, we can fix this by setting a flag to temporarily disable
			// CollectTypedInput whenever we get a Chapter digit here.
			if (modifiers != Keys.Control && modifiers != Keys.Alt && CurrentSelection != null)
			{
				ITsTextProps ttp = CurrentSelection.GetSelProps(SelectionHelper.SelLimitType.Top);
				if (ttp != null)
				{
					if (!char.IsDigit(e.KeyChar) &&
						ttp.Style() == ScrStyleNames.ChapterNumber ||
						(ttp.Style() == ScrStyleNames.VerseNumber &&
						!SelIPInVerseNumber(CurrentSelection.Selection)))
					{
						int nVar;
						CurrentSelection.Selection.SetTypingProps(
							StyleUtils.CharStyleTextProps(null,
							ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar)));
					}
				}
			}

			base.OnKeyPress(e, modifiers);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method is overriden in order to set the selection properties when around an
		/// Object Replacement Character.
		/// </summary>
		/// <param name="e"></param>
		/// <returns><c>true</c> if we handled the key, <c>false</c> otherwise (e.g. we're
		/// already at the end of the rootbox and the user pressed down arrow key).</returns>
		/// ------------------------------------------------------------------------------------
		public override bool OnKeyDown(KeyEventArgs e)
		{
			CheckDisposed();

			IVwSelection vwselOrig = null;

			if (IsSelectionInPrompt(CurrentSelection))
			{
				Debug.Assert(CurrentSelection.IsRange);
				bool? fCollapseToTop = null;
				switch (e.KeyCode)
				{
					case Keys.Right:
						fCollapseToTop = m_cache.ServiceLocator.WritingSystemManager.UserWritingSystem.RightToLeftScript;
						break;
					case Keys.Left:
						fCollapseToTop = !m_cache.ServiceLocator.WritingSystemManager.UserWritingSystem.RightToLeftScript;
						break;
					case Keys.F7: // Forced left even in RtL
						fCollapseToTop = true;
						break;
					case Keys.F8: // Forced right even in RtL
						fCollapseToTop = false;
						break;
				}
				if (fCollapseToTop != null)
				{
					// If we're in a prompt, we want the range selection to behave like an IP.
					// To accomplish this we change the current selection into an IP before
					// we send the key stroke to the Views code.
					vwselOrig = CurrentSelection.Selection;
					m_selectionUpdateInProcess = true;
					try
					{
						CurrentSelection.ReduceToIp((bool)fCollapseToTop ? SelectionHelper.SelLimitType.Top :
							SelectionHelper.SelLimitType.Bottom, false, true);
						ClearCurrentSelection(); // Make sure CurrentSelection isn't out-of-date
					}
					finally
					{
						m_selectionUpdateInProcess = false;
					}
				}
			}

			if (!base.OnKeyDown(e))
			{
				if (vwselOrig != null)
					vwselOrig.Install();
				return false;
			}

			if (DataUpdateMonitor.IsUpdateInProgress())
				return true; //discard this event

			if (Callbacks.EditedRootBox != null)
			{
				switch (e.KeyCode)
				{
					case Keys.PageUp:
					case Keys.PageDown:
					case Keys.End:
					case Keys.Home:
					case Keys.Left:
					case Keys.Up:
					case Keys.Right:
					case Keys.Down:
						{
							// REVIEW (TimS): Is there an easier way to do this?????
							SelectionHelper selHelper = CurrentSelection;
							if (selHelper == null || selHelper.LevelInfo.Length == 0)
								break;

							// If selection is an IP then see if we are on one side or the other of a
							// run containing an Object Replacement Character. If so, associate the
							// selection away from the ORC.
							if (!CurrentSelection.IsRange && CurrentSelection.SelProps != null)
							{
								string sguid = selHelper.SelProps.GetStrPropValue(
									(int)FwTextPropType.ktptObjData);
								if (sguid != null)
								{
									ITsString tss = selHelper.GetTss(SelectionHelper.SelLimitType.Anchor);
									ITsTextProps tssProps = tss.get_PropertiesAt(selHelper.IchAnchor);
									selHelper.AssocPrev = (tssProps.GetStrPropValue(
										(int)FwTextPropType.ktptObjData) != null);
									selHelper.SetSelection(true);
								}
							}
							break;
						}
				}
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the writing system hvo of the current selection if it's in a back translation;
		/// otherwise return -1 if vernacular.
		/// </summary>
		/// <param name="selLimit">the part of the selection considered (top, bottom, end, anchor)
		/// </param>
		/// <returns>the hvo of the writing system used in the selection in the BT, or -1 if
		/// vernacular
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private int GetCurrentBtWs(SelectionHelper.SelLimitType selLimit)
		{
			if (CurrentSelection.GetTextPropId(selLimit) == CmTranslationTags.kflidTranslation)
				return CurrentSelection.GetWritingSystem(selLimit);
			else
				return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="vwsel"></param>
		/// <param name="vttp"></param>
		/// ------------------------------------------------------------------------------------
		protected override void ChangeStyleForPaste(IVwSelection vwsel, ref ITsTextProps[] vttp)
		{
			base.ChangeStyleForPaste(vwsel, ref vttp);

			Debug.Assert(vttp.Length > 0);
			ITsTextProps props = vttp[0];
			if (vttp[0].Style() == ScrStyleNames.ChapterNumber)
				RemoveCharFormatting(vwsel, ref vttp, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Apply style to selection
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void ApplyStyle(string sStyleToApply, IVwSelection vwsel,
			ITsTextProps[] vttpPara, ITsTextProps[] vttpChar)
		{
			CheckDisposed();

			IStStyle newStyle = m_scr.FindStyle(sStyleToApply);
			base.ApplyStyle(newStyle != null ? newStyle.Name : null, vwsel, vttpPara, vttpChar);

			// TODO (TimS): we might eventually handle applying a verse number seperatly.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Apply style to selection
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void ApplyStyle(string sStyleToApply)
		{
			CheckDisposed();

			// Make sure that the Chapter Number style is not applied to non-numeric data
			if (sStyleToApply == ScrStyleNames.ChapterNumber && CurrentSelection != null)
			{
				ITsString tssSelected;
				CurrentSelection.Selection.GetSelectionString(out tssSelected, string.Empty);
				if (tssSelected.Text != null)
				{
					if (tssSelected.Text.Any(ch => !Char.IsDigit(ch)))
					{
						MiscUtils.ErrorBeep();
						return;
					}
				}
			}

			base.ApplyStyle(sStyleToApply);

			// Update the footnote markers if we changed the style of a footnote
			if ((sStyleToApply == ScrStyleNames.CrossRefFootnoteParagraph ||
				sStyleToApply == ScrStyleNames.NormalFootnoteParagraph) &&
				SelParagraph != null)
			{
				// Save the selection
				SelectionHelper prevSelection = CurrentSelection;

				IStTxtPara para = SelParagraph;
				Debug.Assert(para.Owner is IStFootnote);

				IStFootnote footnote = (IStFootnote)para.Owner;
				IScrBook book = (IScrBook)footnote.Owner;

				EditedRootBox.PropChanged(book.Hvo, ScrBookTags.kflidFootnotes, 0,
					book.FootnotesOS.Count, book.FootnotesOS.Count);

				// restore the selection
				if (prevSelection != null)
					prevSelection.SetSelection(true);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a value determining if the new writing systems should be created as a side-effect
		/// of a paste operation.
		/// </summary>
		/// <param name="wsf">writing system factory containing the new writing systems</param>
		/// <param name="destWs">The destination writing system (writing system used at the
		/// selection).</param>
		/// <returns>
		/// 	an indication of how the paste should be handled.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override PasteStatus DeterminePasteWs(ILgWritingSystemFactory wsf, out int destWs)
		{
			destWs = 0;
			// For vernacular and back translation text, we return the default Ws for that view.
			if (IsScriptureView)
				destWs = Cache.DefaultVernWs;
			if (IsBackTranslation)
				destWs = ViewConstructorWS;

			if (destWs == 0)
				destWs = Cache.DefaultVernWs; // set to default vernacular, if 0.

			return PasteStatus.UseDestWs;
		}
		#endregion

		#region Apply Style and related text adjusting methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override ApplyParagraphStyle so that changes in text structure can be handled
		/// correctly.
		/// </summary>
		/// <param name="newParagraphStyle"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override bool ApplyParagraphStyle(string newParagraphStyle)
		{
			CheckDisposed();

			// Paragraph style changes are not permitted in a back translation view.
			if (IsBackTranslation)
			{
				MiscUtils.ErrorBeep();
				return false;
			}

			// Paragraph style changes are restricted to paragraphs in a single StText
			if (!IsSingleTextSelection())
				return false;

			// Check contexts of new and existing paragraph style
			SelLevInfo[] levInfo =
				CurrentSelection.GetLevelInfo(SelectionHelper.SelLimitType.Anchor);

			// only apply a paragraph style to a paragraph....
			if (m_repoCmObject.GetClsid(levInfo[0].hvo) != ScrTxtParaTags.kClassId)
				return false;

			IScrTxtPara para = m_cache.ServiceLocator.GetInstance<IScrTxtParaRepository>().GetObject(levInfo[0].hvo);
			string curStyleName = para.StyleName;
			IStStyle curStyle = m_scr.FindStyle(curStyleName);
			IStStyle newStyle = m_scr.FindStyle(newParagraphStyle);
			if (curStyle == null)
				curStyle = m_scr.FindStyle(para.DefaultStyleName);
			if (curStyle == null || newStyle == null)
			{
				// This should no longer be possible, but to be safe...
				Debug.Assert(curStyle != null);
				Debug.Assert(newStyle != null);
				return true;
			}

			if ((curStyle.Context != newStyle.Context && newStyle.Context != ContextValues.General) ||
				curStyle.Structure != newStyle.Structure)
			{
				// modify the structure and apply the style
				AdjustTextStructure(CurrentSelection, curStyle, newStyle);
				return true;
			}
			else
				return base.ApplyParagraphStyle(newStyle.Name);

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if anchor and end of selection are in paragraphs belonging to the same
		/// StText.
		/// </summary>
		/// <returns><code>true</code> if selection in pargraphs of one StText</returns>
		/// ------------------------------------------------------------------------------------
		private bool IsSingleTextSelection()
		{
			if (CurrentSelection == null || CurrentSelection.Selection == null)
				return false;

			// If not a range selection, must be in one text
			if (!CurrentSelection.Selection.IsRange)
				return true;

			// If number of levels in anchor and end are different, they can't be
			// in the same text.
			if (CurrentSelection.GetNumberOfLevels(SelectionHelper.SelLimitType.Anchor) !=
				CurrentSelection.GetNumberOfLevels(SelectionHelper.SelLimitType.End))
			{
				return false;
			}

			SelLevInfo[] anchor =
				CurrentSelection.GetLevelInfo(SelectionHelper.SelLimitType.Anchor);
			SelLevInfo[] end = CurrentSelection.GetLevelInfo(SelectionHelper.SelLimitType.End);

			// Selections should be same except possibly at level 0 (paragraphs of StText).
			for (int i = 1; i < anchor.Length; i++)
				if (anchor[i].ihvo != end[i].ihvo || anchor[i].tag != end[i].tag)
					return false;

			// All checks passed - is in same text
			return true;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Enter, Delete, and Backspace require special handling in TE.
		/// </summary>
		/// <param name="stuInput">input string</param>
		/// <param name="ss">Status of Shift/Control/Alt key</param>
		/// <param name="modifiers">key modifiers - shift status, etc.</param>
		/// -----------------------------------------------------------------------------------
		protected override void OnCharAux(string stuInput, VwShiftStatus ss, Keys modifiers)
		{
			if (string.IsNullOrEmpty(stuInput))
				return;
			if (stuInput[0] == '\r')
			{
				if ((IsBackTranslation || InBookTitle) && ss != VwShiftStatus.kfssShift)
					GoToNextPara();
				else
					HandleEnterKey(stuInput, ss, modifiers);
				return;
			}

			// Call the base to handle the key
			base.OnCharAux(stuInput, ss, modifiers);

			if ((stuInput[0] == 8 || stuInput[0] == 127) && IsPictureReallySelected && CanDelete())
				DeletePicture();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return true if the deleting of text (or a picture) is possible.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public override bool CanDelete()
		{
			if (!base.CanDelete())
				return false;
			if (Callbacks.EditedRootBox.Selection.SelType != VwSelType.kstPicture)
				return true;

			if (ContentType != StVc.ContentTypes.kctNormal)
				return false; // can't delete pictures in BT views.

			// Also can't delete in BT side of parallel side-by-side view.
			SelLevInfo info;
			return (!CurrentSelection.GetLevelInfoForTag(StTxtParaTags.kflidSegments,
				SelectionHelper.SelLimitType.Anchor, out info));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes a footnote when the selection is on a footnote marker.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void OnDeleteFootnoteAux()
		{
			if (CurrentSelection.Selection.SelType == VwSelType.kstText)
			{
				Debug.Assert(GetFootnoteFromMarkerSelection() != null);
				// for a text selection, deleting the range selection will
				// handle all cases where the ORC may be located - paragraph,
				// CmTranslation or Segment.
				DeleteSelection();
			}
			else
			{
				HandleFootnoteAnchorIconSelected(CurrentSelection.Selection, (hvo, flid, ws, ich) =>
				{
					ITsString tssWithFootnoteOrc;
					if (flid == StTxtParaTags.kflidContents)
					{
						tssWithFootnoteOrc = Cache.DomainDataByFlid.get_StringProp(hvo, flid);
						IScrFootnote footnote = m_repoScrFootnote.GetObject(
							TsStringUtils.GetGuidFromRun(tssWithFootnoteOrc, tssWithFootnoteOrc.get_RunAt(ich)));
						Cache.DomainDataByFlid.DeleteObj(footnote.Hvo);
					}
					else
					{
						tssWithFootnoteOrc = Cache.DomainDataByFlid.get_MultiStringAlt(hvo, flid, ws);
						Cache.DomainDataByFlid.SetMultiStringAlt(hvo, flid, ws, tssWithFootnoteOrc.Remove(ich, 1));
					}
				});
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks to see if there's a footnote on either side of the IP. If there is, then
		/// it's selected.
		/// </summary>
		/// <returns><c>true</c> if a footnote marker is sucessfully selected next to the IP.
		/// Otherwise, <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		public bool SelectFootnoteMarkNextToIP()
		{
			if (CurrentSelection == null)
				return false;

			SelectionHelper helper = new SelectionHelper(CurrentSelection);

			// If the selection is a range then just return what SelectionIsFootnoteMarker determines.
			if (helper.Selection.IsRange)
				return SelectionIsFootnoteMarker(helper.Selection);

			// Now that we know the selection is not a range, look to both sides of the IP
			// to see if we are next to a footnote marker.

			// First, look on the side where the IP is associated. If there's a marker there,
			// select it and get out.
			if (SelectionIsFootnoteMarker(helper.Selection))
			{
				helper.IchEnd = helper.AssocPrev ? (helper.IchAnchor - 1) : (helper.IchAnchor + 1);
				helper.SetSelection(true);
				return true;
			}

			// Check the other side of the IP and select the marker if there's one there.
			helper.AssocPrev = !helper.AssocPrev;
			helper.SetSelection(false);
			if (SelectionIsFootnoteMarker(helper.Selection))
			{
				helper.IchEnd = helper.AssocPrev ? (helper.IchAnchor - 1) : (helper.IchAnchor + 1);
				helper.SetSelection(true);
				return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks if the current selection contains only a footnote reference, or is an IP
		/// associated with	only a footnote reference.
		/// </summary>
		/// <param name="vwsel">Selection to check for footnote marker.</param>
		/// <returns><c>True</c>if current selection is on a footnote</returns>
		/// ------------------------------------------------------------------------------------
		private bool SelectionIsFootnoteMarker(IVwSelection vwsel)
		{
			return (GetFootnoteFromMarkerSelection(vwsel) != null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks if the current selection contains only a footnote reference, or is an IP
		/// associated with	only a footnote reference.
		/// </summary>
		/// <returns>the footnote object, if current selection is on a footnote;
		/// otherwise null</returns>
		/// ------------------------------------------------------------------------------------
		private IStFootnote GetFootnoteFromMarkerSelection()
		{
			CheckDisposed();

			if (CurrentSelection != null)
				return GetFootnoteFromMarkerSelection(CurrentSelection.Selection);
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks if the current selection contains only a footnote reference, or is an IP
		/// associated with	only a footnote reference.
		/// </summary>
		/// <param name="vwsel">selection to get info from</param>
		/// <returns>the footnote object, if current selection is on a footnote;
		/// otherwise null</returns>
		/// ------------------------------------------------------------------------------------
		private IStFootnote GetFootnoteFromMarkerSelection(IVwSelection vwsel)
		{
			CheckDisposed();

			IStFootnote footnote;

			// if we find a single run with the correct props, we are on an ORC hot link
			Guid footnoteGuid = GetOrcHotLinkStrProp(vwsel);
			if (footnoteGuid == Guid.Empty)
				return null; // not a footnote

			// Get the underlying object for the guid.
			Cache.ServiceLocator.GetInstance<IStFootnoteRepository>().TryGetObject(footnoteGuid, out footnote);

			return footnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Helper function. Checks if the selection contains only an Object
		/// Replacement Character (ORC) hot link, or is an IP associated with only an ORC hot
		/// link, by examining the selection's text props.
		/// </summary>
		/// <param name="vwsel">the given selection</param>
		/// <returns>If selection is on an ORC hot link only, returns its string property of
		/// type ktptObjData; if not, returns null.</returns>
		/// ------------------------------------------------------------------------------------
		private Guid GetOrcHotLinkStrProp(IVwSelection vwsel)
		{
			if (vwsel == null)
				return Guid.Empty;

			Guid footnoteGuid = GetGuidForSelectedFootnoteAnchorIcon(vwsel);
			if (footnoteGuid == Guid.Empty)
			{
				ITsTextProps[] vttpTmp;
				IVwPropertyStore[] vvpsTmp;
				int cttp;

				SelectionHelper.GetSelectionProps(vwsel, out vttpTmp, out vvpsTmp, out cttp);

				// If the run count is more than 1, the selection is larger than an ORC.
				// Zero runs can't be an ORC either. Unless there is one, we're outa here.
				if (cttp != 1)
					return Guid.Empty; // no ORC found

				FwObjDataTypes[] desiredTypes = {FwObjDataTypes.kodtNameGuidHot, FwObjDataTypes.kodtOwnNameGuidHot};
				FwObjDataTypes foundType;
				footnoteGuid = TsStringUtils.GetGuidFromProps(vttpTmp[0], desiredTypes, out foundType);
			}
			return footnoteGuid;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// If user presses Enter and a new style is applied to the following paragraph, we
		/// need to mark that style as being in use. If in a section Head, we might need to fix
		/// the structure.
		/// </summary>
		/// <param name="stuInput">input string</param>
		/// <param name="ss">Status of Shift/Control/Alt key</param>
		/// <param name="modifiers">key modifiers - shift status, etc.</param>
		/// -----------------------------------------------------------------------------------
		protected void HandleEnterKey(string stuInput, VwShiftStatus ss, Keys modifiers)
		{
			if (IsPictureSelected) // Enter should do nothing if a picture or caption is selected.
				return;

			int defVernWs = m_cache.DefaultVernWs;
			SelLevInfo[] levInfo;
			// If we are at the end of a heading paragraph, we need to check the "next" style to
			// see if it is a body type. If it is not, then allow processing to proceed as normal.
			// If it is a body type, then don't create a new paragraph, just move down to the start
			// of the first body paragraph in the section.
			if (InSectionHead)
			{
				// if the selection is a range selection then try to delete the selected text first.
				if (CurrentSelection.Selection.IsRange)
				{
					levInfo = CurrentSelection.GetLevelInfo(SelectionHelper.SelLimitType.Top);
					ITsStrFactory factory = TsStrFactoryClass.Create();
					CurrentSelection.Selection.ReplaceWithTsString(
						factory.MakeString(string.Empty, defVernWs));
					// If selection is still a range selection, the deletion failed and we don't
					// need to do anything else.
					if (CurrentSelection == null || CurrentSelection.Selection.IsRange || !InSectionHead)
						return;
				}

				// If the heading style has a following style that is a body style and we are at the
				// end of the paragraph then move the IP to the beginning of the body paragraph.
				levInfo = CurrentSelection.GetLevelInfo(SelectionHelper.SelLimitType.Anchor);
				// This is the paragraph that was originally selected
				IStTxtPara headPara = m_repoScrTxtPara.GetObject(levInfo[0].hvo);
				IStStyle headParaStyle = m_scr.FindStyle(headPara.StyleName);
				IStStyle followStyle = headParaStyle != null ? headParaStyle.NextRA : null;

				if (followStyle != null && followStyle.Structure == StructureValues.Body &&
					SelectionAtEndParagraph())
				{
					// if there is another section head paragraph, then the section needs to be split
					IScrSection section = ((ITeView)Control).LocationTracker.GetSection(CurrentSelection,
						SelectionHelper.SelLimitType.Anchor);
					// Changes made to text will destroy the selection, so we have to remember
					// the current locations.
					int iBook = BookIndex;
					int iSection = SectionIndex;
					if (CurrentSelection.LevelInfo[0].ihvo < section.HeadingOA.ParagraphsOS.Count - 1)
					{

						// break the section
						// create a new empty paragraph in the first section
						// set the IP to the start of the new paragraph
						IScrSection newSection =
							CreateSection(BCVRef.GetVerseFromBcv(section.VerseRefMin) == 0);
						Debug.Assert(newSection != null, "Failed to create a new section");
						IScrSection origSection = ((IScrBook) newSection.Owner).SectionsOS[iSection];
						IStTxtPara contentPara = origSection.ContentOA[0];
						contentPara.StyleRules = StyleUtils.ParaStyleTextProps(followStyle.Name);
						SetInsertionPoint(iBook, iSection, 0, 0, false);
					}
					else
					{
						SetInsertionPoint(iBook, iSection, 0, 0, false);
						// If the first paragraph is not empty, then insert a new paragraph with the
						// follow-on style of the section head.
						IStTxtPara contentPara = (IStTxtPara) section.ContentOA.ParagraphsOS[0];
						if (contentPara.Contents.Length > 0)
						{
							StTxtParaBldr bldr = new StTxtParaBldr(m_cache);
							bldr.ParaStyleName = followStyle.Name;
							bldr.AppendRun(String.Empty, StyleUtils.CharStyleTextProps(null,
								defVernWs));
							bldr.CreateParagraph((IStText)contentPara.Owner, 0);

							SetInsertionPoint(iBook, iSection, 0, 0, false);
						}
					}
					return;
				}
			}

			// Call the base to handle the key
			base.OnCharAux(stuInput, ss, modifiers);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks if current selection is at the end of the paragraph
		/// </summary>
		/// <remarks>This is only valid if the current selection is in the vernacular</remarks>
		/// <returns>true if selection is at end</returns>
		/// ------------------------------------------------------------------------------------
		private bool SelectionAtEndParagraph()
		{
			SelLevInfo paraInfo = CurrentSelection.GetLevelInfoForTag(StTextTags.kflidParagraphs);
			IStTxtPara para = m_repoScrTxtPara.GetObject(paraInfo.hvo);
			return para.Contents.Length == CurrentSelection.IchAnchor;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes changes in text structure when the context of the new paragraph style does not
		/// match the context of the current paragraph style, and applies the new paragraph style.
		/// </summary>
		/// <param name="selHelper">the selection to which the new style is being applied</param>
		/// <param name="curStyle">current style at the selection anchor</param>
		/// <param name="newStyle">new style to be applied (has a new style context)</param>
		/// ------------------------------------------------------------------------------------
		private void AdjustTextStructure(SelectionHelper selHelper,
			IStStyle curStyle, IStStyle newStyle)
		{
			SelLevInfo[] top = selHelper.GetLevelInfo(SelectionHelper.SelLimitType.Top);
			SelLevInfo[] bottom = selHelper.GetLevelInfo(SelectionHelper.SelLimitType.Bottom);
			ILocationTracker tracker = ((ITeView)Control).LocationTracker;
			int scrLevelCount = tracker.GetLevelCount(ScrSectionTags.kflidContent);

			// Adjustments will only be done in section level selections.
			if (top.Length != scrLevelCount || bottom.Length != scrLevelCount)
				return;

			// CASE 1: Change a section head to a content paragraph
			// If style of paragraphs of the section heading are changed to something
			// that is not a section head style, restructure heading paragraphs
			if (top[1].tag == ScrSectionTags.kflidHeading &&
				newStyle.Structure == StructureValues.Body)
			{
				// These current indices will be adjusted after the structure is changed
				// to indicate where the new selection should be
				int iSection = tracker.GetSectionIndexInBook(selHelper, SelectionHelper.SelLimitType.Top);
				int iPara;
				int iBook = tracker.GetBookIndex(selHelper, SelectionHelper.SelLimitType.Top);
				IScrBook book = BookFilter.GetBook(iBook);
				IScrSection section = book[iSection];

				// bool indicates if this section is the first one in either Intro or Scripture
				bool firstSectionInContext;
				if (iSection == 0)
					firstSectionInContext = true;
				else
					firstSectionInContext = !SectionsHaveSameContext(section, section.PreviousSection);

				// If all paragraphs of heading are selected, merge selected section
				// with previous section
				if (AreAllParagraphsSelected(top, bottom, section.HeadingOA))
				{
					if (!firstSectionInContext)
					{
						iPara = book.MergeSectionIntoPreviousSectionContent(iSection, newStyle);
						iSection--;
					}
					else
					{
						// Just need to move all heading paragraphs to the content. The move
						// method will create a new empty heading paragraph.
						iPara = 0;
						section.MoveHeadingParasToContent(0, newStyle);
					}
				}
				// If selection starts at first paragraph of the heading, move heading paragraphs to
				// content of previous section.
				else if (top[0].ihvo == 0)
				{
					if (!firstSectionInContext)
					{
						iPara = book.MoveHeadingParasToPreviousSectionContent(iSection, bottom[0].ihvo, newStyle);
						iSection--;
					}
					else
					{
						// This is the first section in context, and the selection includes the first para of the heading.
						// In this case, we want the selected paragraphs to become the content
						// of the first section, and the following section head
						// paragraph(s) become the heading of a new section object.
						section.SplitSectionHeading_ExistingParaBecomesContent(top[0].ihvo,
							bottom[0].ihvo, newStyle);
						// update insertion point to first paragraph(s) of new section content
						iPara = 0;
					}
				}
				else
				{
					// If selection bottoms at the last paragraph of the heading, move heading
					// paragraphs to content of this section
					if (section.HeadingOA.ParagraphsOS.Count - 1 == bottom[0].ihvo)
					{
						section.MoveHeadingParasToContent(top[0].ihvo, newStyle);
						iPara = 0;
					}
					else
					// The selection must be only inner paragraphs in the section head,
					// not including the first or last paragraphs.
					// In this case, we want the selected paragraph(s) to become content for
					// the heading paragraph(s) above it, and the following section head
					// paragraph(s) to become the heading of a new section object.
					{
						int iParaStart = top[0].ihvo;
						int iParabottom = bottom[0].ihvo;
						section.SplitSectionHeading_ExistingParaBecomesContent(iParaStart, iParabottom, newStyle);

						iPara = 0;
					}
				}

				// Select the original range of paragraphs and characters, now in the new content para
				// TODO: FWR-1542 Currently, a selection made during a unit of work can only be an insertion point.
				SelectRangeOfChars(iBook, iSection, ScrSectionTags.kflidContent, iPara,
					selHelper.GetIch(SelectionHelper.SelLimitType.Top),
					selHelper.GetIch(SelectionHelper.SelLimitType.Top),
					true, true, selHelper.AssocPrev);

			} //bottom of CASE 1 "Change a section head to a scripture paragraph"

			// CASE 2: Change scripture paragraph to a section head
			// - only if the new style has "section head" structure and the paragraph(s)
			// is/are part of the section content.
			else if (top[1].tag == ScrSectionTags.kflidContent &&
				newStyle.Structure == StructureValues.Heading)
			{
				IScrSection section = tracker.GetSection(selHelper, SelectionHelper.SelLimitType.Top);
				int iBook = tracker.GetBookIndex(selHelper, SelectionHelper.SelLimitType.Top);
				int iSection = tracker.GetSectionIndexInBook(selHelper, SelectionHelper.SelLimitType.Top);
				int iPara;
				IScrBook book = (IScrBook)section.Owner;

				// bool indicates if this section is the last one in either Intro or Scripture
				bool lastSectionInContext;
				if (iSection == book.SectionsOS.Count - 1) // last section ends book
					lastSectionInContext = true;
				else
					lastSectionInContext = !SectionsHaveSameContext(section, section.NextSection);

				try
				{
					if (AreAllParagraphsSelected(top, bottom, section.ContentOA))
					{
						if (!lastSectionInContext)
						{
							// Need to combine this section with the following section.
							// Heading of combined section will be section1.Heading +
							// section1.Content + section2.Heading
							iPara = book.MergeSectionIntoNextSectionHeading(iSection, newStyle);
						}
						else
						{
							// Just need to move all content paragraphs to the heading. The move
							// method will create a new empty content paragraph.
							iPara = section.HeadingOA.ParagraphsOS.Count;
							section.MoveContentParasToHeading(section.ContentOA.ParagraphsOS.Count - 1, newStyle);
						}
					}
					else if (top[0].ihvo == 0) //selection starts at first content para
					{
						// Move the first content paragraphs to become the last para of the
						// section head
						section.MoveContentParasToHeading(bottom[0].ihvo, newStyle);

						iPara = section.HeadingOA.ParagraphsOS.Count -
								(bottom[0].ihvo - top[0].ihvo + 1);
					}
					else if (bottom[0].ihvo == section.ContentOA.ParagraphsOS.Count - 1 //selection ends at last content para
							 && !lastSectionInContext) // this is not the last Intro or Scripture section
					{
						// Move the last content paragraphs to become the first para of the next
						// section in the book.
						book.MoveContentParasToNextSectionHeading(iSection, top[0].ihvo, newStyle);
						// update insertion point to first paragraph(s) of next section head
						iSection++;
						iPara = 0;
					}
					else
					{
						// Selection is of middle paragraph(s) in the contents,
						// or selection includes the last para of the last intro section or last Scripture section.
						// In this case, we want the selected paragraph to become the heading
						// of a new section, and the following paragraph(s), if any, become the
						// content of the new section object.
						section.SplitSectionContent_ExistingParaBecomesHeading(
							top[0].ihvo, bottom[0].ihvo - top[0].ihvo + 1, newStyle);
						// update insertion point to first paragraph(s) of next section head
						iSection++;
						iPara = 0;
					}
				}
				catch (InvalidStructureException e)
				{
					// Cancel the request if chapter or verse numbers are present.
					// display message box if not running in a test
					if (!MiscUtils.RunningTests)
					{
						MessageBox.Show(Control,
							TeResourceHelper.GetResourceString("kstidParaHasNumbers"),
							m_app.ApplicationName, MessageBoxButtons.OK);
					}
					return;
				}

				// Select the original range of paragraphs and characters, now in the new heading
				// TODO: FWR-1542 Currently, a selection made during a unit of work can only be an insertion point.
				SelectRangeOfChars(iBook, iSection, ScrSectionTags.kflidHeading, iPara,
					selHelper.GetIch(SelectionHelper.SelLimitType.Top),
					selHelper.GetIch(SelectionHelper.SelLimitType.Top),
					true, true, selHelper.AssocPrev);
			}
			return;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the selection represented by the given top and bottom selection
		/// level information covers the entire range of paragraphs in the selcted text
		/// </summary>
		/// <param name="top">The selection levels corresponding to the top of the selection</param>
		/// <param name="bottom">The selection levels corresponding to the bottom of the
		/// selection</param>
		/// <param name="text">The text.</param>
		/// <returns>
		/// 	<c>true</c> if all paragraphs of the text are included in the selection;
		///		<c>false</c> otherwise
		/// </returns>
		/// <exception cref="Exception">If the selection is invalid, spans multiple StTexts, or
		/// covers more paragraphs than actually exist in the StText</exception>
		/// ------------------------------------------------------------------------------------
		private bool AreAllParagraphsSelected(SelLevInfo[] top, SelLevInfo[] bottom,
			IStText text)
		{
			if (text.Hvo != top[1].hvo || top[1].hvo != bottom[1].hvo ||
				text.ParagraphsOS.Count <= bottom[0].ihvo)
			{
				throw new Exception("Selection is invalid! Top and bottom of selection do not " +
					"correspond to the paragraphs of the same text. textHvo = " + text.Hvo +
					"; top[1].hvo = " + top[1].hvo + "; bottom[1].hvo = " + bottom[1].hvo +
					"; text.ParagraphsOS.Count = " + text.ParagraphsOS.Count +
					"; bottom[0].ihvo = " + bottom[0].ihvo +
					". Please save a backup copy of your database so that developers can attempt to find the cause of this problem (TE-5717).");
			}

			if (top[0].ihvo != 0)
				return false;

			// verify that number of selected paragraphs equals paragraphs in StText
			IStText topText = m_repoStText.GetObject(top[1].hvo);

			return topText.ParagraphsOS.Count == bottom[0].ihvo + 1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use this method to determine if the selection within StText has an edge at or near
		/// the section boundary. I.e.: the selection top is within the first paragraph
		/// in the heading of the section OR the selection bottom is within the last paragraph
		/// in the content of the section.
		/// </summary>
		/// <param name="top">Top of the selection.</param>
		/// <param name="bottom">Bottom of the selection.</param>
		/// <returns>True if the selection is within an StText
		/// AND EITHER the selection top is within the first paragraph of the heading
		/// OR the selection bottom is within the last paragraph of the content.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private bool SelectionNearSectionBoundary(SelLevInfo[] top, SelLevInfo[] bottom)
		{
			IStText text = m_repoStText.GetObject(bottom[1].hvo);

			int headingFlid = ScrSectionTags.kflidHeading;
			int contentFlid = ScrSectionTags.kflidContent;

			return
				// Applies only within an StText;
				(top[1].ihvo == bottom[1].ihvo) &&
				// Top within first paragraph of heading
				((top[1].tag == headingFlid && top[0].ihvo == 0) ||
				// Bottom within last paragraph of content
				(bottom[1].tag == contentFlid && text.ParagraphsOS.Count == bottom[0].ihvo + 1));
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use this method to determine if the selection within StText has an edge adjacent to
		/// the section's other StText. I.e.: the selection bottom is within the last paragraph
		/// in the heading of the section OR the selection top is within the first paragraph
		/// in the content of the section.
		/// </summary>
		/// <param name="top">Top of the selection.</param>
		/// <param name="bottom">Bottom of the selection.</param>
		/// <returns>True if the selection is within an StText
		/// AND EITHER the selection bottom is within the last paragraph of the heading
		/// OR the selection top is within the first paragraph of the content.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private bool SelectionNearInternalBoundary(SelLevInfo[] top, SelLevInfo[] bottom)
		{
			IStText text = m_repoStText.GetObject(top[1].hvo);
			int headingFlid = ScrSectionTags.kflidHeading;
			int contentFlid = ScrSectionTags.kflidContent;

			return
				// Applies only within an StText;
				(top[1].ihvo == bottom[1].ihvo) &&
				// Top within first paragraph of content
				((top[1].tag == contentFlid && top[0].ihvo == 0) ||
				// Bottom within last paragraph of heading
				(bottom[1].tag == headingFlid && text.ParagraphsOS.Count == bottom[0].ihvo + 1));
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use this method to determine if the selection within StText has an edge adjacent to
		/// a different context. I.e.: the selection top is within the first paragraph
		/// of a context OR the selection bottom is within the last paragraph of a context.
		/// </summary>
		/// <param name="top">Top of the selection.</param>
		/// <param name="bottom">Bottom of the selection.</param>
		/// <returns>True if the selection is within an StText
		/// AND EITHER the selection top is within the first paragraph of the context
		/// OR the selection bottom is within the last paragraph of the context.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private bool SelectionNearContextBoundary(SelLevInfo[] top, SelLevInfo[] bottom)
		{
			int headingFlid = ScrSectionTags.kflidHeading;
			int contentFlid = ScrSectionTags.kflidContent;
			if (top[1].ihvo != bottom[1].ihvo)
				return false; // Applies only within an StText

			IStText curText = m_repoStText.GetObject(top[1].hvo);
			return
				// Top within first paragraph of heading of first section
				((top[1].tag == headingFlid & true) ||
				// Bottom within last paragraph of context
				(bottom[1].tag == contentFlid & true));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Reset Paragraph Style menu/toolbar command.
		///
		/// Resets the style of the paragraph to be the default style for the current context.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public void ResetParagraphStyle()
		{
			CheckDisposed();

			if (SelParagraph == null)
				return;

			// Reset paragraph style only active when selection is in a single
			// paragraph, so can just get selected paragraph from anchor
			ApplyStyle(SelParagraph.DefaultStyleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override for the special semantics method. Treat verse and chapter numbers as
		/// special sematics.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool SpecialSemanticsCharacterStyle(string name)
		{
			CheckDisposed();
			return (name == ScrStyleNames.VerseNumber || name == ScrStyleNames.ChapterNumber);
		}

		#endregion

		#region Selection Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the insertion point in this draftview to the specified location.
		/// </summary>
		/// <param name="tag">Indicates whether selection should be made in the title, section
		/// Heading or section Content</param>
		/// <param name="book">The 0-based index of the Scripture book in which to put the
		/// insertion point</param>
		/// <param name="section">The 0-based index of the Scripture section in which to put the
		/// insertion point</param>
		/// <param name="para">The 0-based index of the paragraph in which to put the insertion
		/// point</param>
		/// <param name="character">The 0-based index of the character before which the
		/// insertion point is to be placed</param>
		/// <param name="fAssocPrev">True if the properties of the text entered at the new
		/// insertion point should be associated with the properties of the text before the new
		/// insertion point. False if text entered at the new insertion point should be
		/// associated with the text following the new insertion point.</param>
		/// <param name="scrollOption">Where to scroll the selection</param>
		/// <returns>The selection helper object used to make move the IP.</returns>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper SetInsertionPoint(int tag, int book, int section, int para,
			int character, bool fAssocPrev, VwScrollSelOpts scrollOption)
		{
			CheckDisposed();

			return SelectRangeOfChars(book, section, tag, para, character, character,
				true, true, fAssocPrev, scrollOption);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the insertion point in this draftview to the specified location.
		/// </summary>
		/// <param name="tag">Indicates whether selection should be made in the title, section
		/// Heading or section Content</param>
		/// <param name="book">The 0-based index of the Scripture book in which to put the
		/// insertion point</param>
		/// <param name="section">The 0-based index of the Scripture section in which to put the
		/// insertion point</param>
		/// <param name="para">The 0-based index of the paragraph in which to put the insertion
		/// point</param>
		/// <param name="character">The 0-based index of the character before which the
		/// insertion point is to be placed</param>
		/// <param name="fAssocPrev">True if the properties of the text entered at the new
		/// insertion point should be associated with the properties of the text before the new
		/// insertion point. False if text entered at the new insertion point should be
		/// associated with the text following the new insertion point.</param>
		/// <returns>The selection helper object used to make move the IP.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual SelectionHelper SetInsertionPoint(int tag, int book, int section, int para,
			int character, bool fAssocPrev)
		{
			CheckDisposed();

			return SelectRangeOfChars(book, section, tag, para, character, character,
				true, true, fAssocPrev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the insertion point in this draftview to the specified location in
		/// section content.
		/// </summary>
		/// <param name="book">The 0-based index of the Scripture book in which to put the
		/// insertion point</param>
		/// <param name="section">The 0-based index of the Scripture section in which to put the
		/// insertion point</param>
		/// <param name="para">The 0-based index of the paragraph in which to put the insertion
		/// point</param>
		/// <param name="character">The 0-based index of the character before which the
		/// insertion point is to be placed</param>
		/// <param name="fAssocPrev">True if the properties of the text entered at the new
		/// insertion point should be associated with the properties of the text before the new
		/// insertion point. False if text entered at the new insertion point should be
		/// associated with the text following the new insertion point.</param>
		/// <returns>The selection helper object used to make move the IP.</returns>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper SetInsertionPoint(int book, int section, int para,
			int character, bool fAssocPrev)
		{
			CheckDisposed();

			return SetInsertionPoint(ScrSectionTags.kflidContent, book, section, para,
				character, fAssocPrev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the insertion point at the beginning of any Scripture element: Title, Section Head,
		/// or Section Content.  The selection will be "installed" and scrolled into view.
		/// </summary>
		/// <param name="tag">Indicates whether selection should be made in the title, section
		/// Heading or section Content</param>
		/// <param name="book">The 0-based index of the Scripture book in which to put the
		/// insertion point</param>
		/// <param name="section">The 0-based index of the Scripture section in which to put the
		/// insertion point. Ignored if tag is <see cref="ScrBookTags.kflidTitle"/></param>
		/// <returns>
		/// The selection helper object used to make move the IP.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper SetInsertionPoint(int tag, int book, int section)
		{
			CheckDisposed();
			return SetInsertionPoint(tag, book, section, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the insertion point at the given paragraph of any Scripture element: Title,
		/// Section Head, or Section Content.
		/// The selection will be "installed" and scrolled into view.
		/// </summary>
		/// <param name="tag">Indicates whether selection should be made in the title, section
		/// Heading or section Content</param>
		/// <param name="iBook">The 0-based index of the Scripture book in which to put the
		/// insertion point</param>
		/// <param name="iSection">The 0-based index of the Scripture section in which to put the
		/// insertion point. Ignored if tag is <see cref="ScrBookTags.kflidTitle"/></param>
		/// <param name="iPara">The 0-based index of the paragraph which to put the
		/// insertion point.</param>
		/// <returns>
		/// The selection helper object used to make move the IP.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper SetInsertionPoint(int tag, int iBook, int iSection, int iPara)
		{
			CheckDisposed();

			return SelectRangeOfChars(iBook, iSection, tag, iPara, 0, 0, true, true, false,
				VwScrollSelOpts.kssoDefault);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Selects the text in the given paragraph in the specified scripture book at
		/// the specified offsets. The selection will be "installed" and scrolled into view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SelectRangeOfChars(int iBook, IStTxtPara para, int ichStart, int ichEnd)
		{
			// The text still exists at the same position, so highlight it.
			IStText text = (IStText)para.Owner;
			int iSection = -1;

			if (text.OwningFlid != ScrBookTags.kflidTitle)
				iSection = text.Owner.IndexInOwner;

			if (para.OwnerOfClass(ScrDraftTags.kClassId) != null)
				return; // The paragraph is owned by a ScrDraft. We don't want to use it!

			SelectRangeOfChars(iBook, iSection, text.OwningFlid, para.IndexInOwner,
				ichStart, ichEnd, true, true, false, VwScrollSelOpts.kssoDefault);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set an insertion point or character-range selection in the Section Contents at the
		/// specified offsets. The selection will be "installed" and scrolled into view.
		/// </summary>
		/// <param name="iBook">The 0-based index of the Scripture book in which to put the
		/// insertion point</param>
		/// <param name="iSection">The 0-based index of the Scripture section in which to put the
		/// insertion point</param>
		/// <param name="iPara">The 0-based index of the paragraph in which to put the insertion
		/// point</param>
		/// <param name="startCharacter">The 0-based index of the character at which the
		/// selection begins (or before which the insertion point is to be placed if
		/// startCharacter == endCharacter)</param>
		/// <param name="endCharacter">The character location to end the selection</param>
		/// <returns>The selection helper object used to make move the IP.</returns>
		/// <remarks>This method is only used for tests  ???</remarks>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper SelectRangeOfChars(int iBook, int iSection, int iPara,
			int startCharacter, int endCharacter)
		{
			CheckDisposed();

			return SelectRangeOfChars(iBook, iSection, ScrSectionTags.kflidContent,
				iPara, startCharacter, endCharacter, true, true, true, VwScrollSelOpts.kssoDefault);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set an insertion point or character-range selection at the specified offsets with
		/// specified options.
		/// </summary>
		/// <param name="iBook">The 0-based index of the Scripture book in which to put the
		/// insertion point</param>
		/// <param name="iSection">The 0-based index of the Scripture section in which to put the
		/// insertion point</param>
		/// <param name="tag">Indicates whether selection should be made in the section
		/// Heading or Content or in the book title</param>
		/// <param name="iPara">The 0-based index of the paragraph in which to put the insertion
		/// point</param>
		/// <param name="startCharacter">The 0-based index of the character at which the
		/// selection begins (or before which the insertion point is to be placed if
		/// startCharacter == endCharacter)</param>
		/// <param name="endCharacter">The character location to end the selection</param>
		/// <param name="fInstall"></param>
		/// <param name="fMakeVisible"></param>
		/// <param name="fAssocPrev"></param>
		/// <returns>The selection helper</returns>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper SelectRangeOfChars(int iBook, int iSection, int tag,
			int iPara, int startCharacter, int endCharacter, bool fInstall, bool fMakeVisible,
			bool fAssocPrev)
		{
			CheckDisposed();

			return SelectRangeOfChars(iBook, iSection, tag, iPara, startCharacter, endCharacter,
				fInstall, fMakeVisible, fAssocPrev, VwScrollSelOpts.kssoDefault);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set an insertion point or character-range selection at the specified offsets with
		/// specified options.
		/// </summary>
		/// <param name="iBook">The 0-based index of the Scripture book in which to put the
		/// insertion point</param>
		/// <param name="iSection">The 0-based index of the Scripture section in which to put the
		/// insertion point</param>
		/// <param name="tag">Indicates whether selection should be made in the section
		/// Heading or Content or in the book title</param>
		/// <param name="iPara">The 0-based index of the paragraph in which to put the insertion
		/// point</param>
		/// <param name="startCharacter">The 0-based index of the character at which the
		/// selection begins (or before which the insertion point is to be placed if
		/// startCharacter == endCharacter)</param>
		/// <param name="endCharacter">The character location to end the selection</param>
		/// <param name="fInstall"></param>
		/// <param name="fMakeVisible"></param>
		/// <param name="fAssocPrev">If an insertion point, does it have the properties of the
		/// previous character?</param>
		/// <param name="scrollOption">Where to scroll the selection</param>
		/// <returns>The selection helper</returns>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper SelectRangeOfChars(int iBook, int iSection, int tag,
			int iPara, int startCharacter, int endCharacter, bool fInstall, bool fMakeVisible,
			bool fAssocPrev, VwScrollSelOpts scrollOption)
		{
			Debug.Assert(ContentType != StVc.ContentTypes.kctSegmentBT, "isegment arg required for segment BT SelectRangeOfChars");
			return SelectRangeOfChars(iBook, iSection, tag, iPara, 0, startCharacter, endCharacter,
				fInstall, fMakeVisible, fAssocPrev, scrollOption);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set an insertion point or character-range selection at the specified offsets
		/// (including segment index) with specified options.
		/// This is the actual workhorse for all the above methods.
		/// </summary>
		/// <param name="iBook">The 0-based index of the Scripture book in which to put the
		/// selection</param>
		/// <param name="iSection">The 0-based index of the Scripture section in which to put the
		/// selection</param>
		/// <param name="tag">Indicates whether selection should be made in the section
		/// Heading or Content or in the book title</param>
		/// <param name="isegment">If ContentType == segmentBT, the index of the segment
		/// in which to place the selection; otherwise ignored.</param>
		/// <param name="iPara">The 0-based index of the paragraph in which to put the insertion
		/// point</param>
		/// <param name="startCharacter">The 0-based index of the character at which the
		/// selection begins (or before which the insertion point is to be placed if
		/// startCharacter == endCharacter)</param>
		/// <param name="endCharacter">The character location to end the selection</param>
		/// <param name="fInstall"></param>
		/// <param name="fMakeVisible"></param>
		/// <param name="fAssocPrev">If an insertion point, does it have the properties of the
		/// previous character?</param>
		/// <param name="scrollOption">Where to scroll the selection</param>
		/// <returns>The selection helper</returns>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper SelectRangeOfChars(int iBook, int iSection, int tag,
			int iPara, int isegment, int startCharacter, int endCharacter, bool fInstall, bool fMakeVisible,
			bool fAssocPrev, VwScrollSelOpts scrollOption)
		{
			CheckDisposed();

			if (Callbacks == null || Callbacks.EditedRootBox == null)
				return null;  // can't make a selection

			Debug.Assert(tag == ScrSectionTags.kflidHeading ||
				tag == ScrSectionTags.kflidContent ||
				tag == ScrBookTags.kflidTitle);

			SelectionHelper selHelper = new SelectionHelper();
			selHelper.NumberOfLevels = ((ITeView)Control).LocationTracker.GetLevelCount(tag);
			int levelForPara = LocationTrackerImpl.GetLevelIndexForTag(StTextTags.kflidParagraphs,
				m_contentType);

			selHelper.LevelInfo[levelForPara].tag = StTextTags.kflidParagraphs;
			selHelper.LevelInfo[levelForPara].ihvo = iPara;
			selHelper.LevelInfo[levelForPara + 1].tag = tag;

			((ITeView)Control).LocationTracker.SetBookAndSection(selHelper,
				SelectionHelper.SelLimitType.Anchor, iBook,
				tag == ScrBookTags.kflidTitle ? -1 : iSection);

			if (ContentType == StVc.ContentTypes.kctSimpleBT)
			{
				int levelForBT = LocationTrackerImpl.GetLevelIndexForTag(StTxtParaTags.kflidTranslations,
					m_contentType);
				selHelper.LevelInfo[levelForBT].tag = -1;
				selHelper.LevelInfo[levelForBT].ihvo = 0;
				selHelper.SetTextPropId(SelectionHelper.SelLimitType.Anchor,
					CmTranslationTags.kflidTranslation);
				selHelper.SetTextPropId(SelectionHelper.SelLimitType.End,
					CmTranslationTags.kflidTranslation);
			}
			else if (ContentType == StVc.ContentTypes.kctSegmentBT)
			{
				// In all segment BT views, under the paragraph there is a segment, and under that
				// an object which is the free translation itself.
				selHelper.LevelInfo[1].tag = StTextTags.kflidParagraphs; // JohnT: why don't we need this for non-BT??
				selHelper.LevelInfo[0].ihvo = isegment;
				selHelper.LevelInfo[0].tag = StTxtParaTags.kflidSegments;
				selHelper.SetTextPropId(SelectionHelper.SelLimitType.Anchor,
					SegmentTags.kflidFreeTranslation);
				selHelper.SetTextPropId(SelectionHelper.SelLimitType.End,
					SegmentTags.kflidFreeTranslation);
			}
			else
			{
				// selHelper.LevelInfo[0].tag is set automatically by SelectionHelper class
				selHelper.SetTextPropId(SelectionHelper.SelLimitType.Anchor, StTxtParaTags.kflidContents);
				selHelper.SetTextPropId(SelectionHelper.SelLimitType.End, StTxtParaTags.kflidContents);
			}
			selHelper.AssocPrev = fAssocPrev;
			selHelper.SetLevelInfo(SelectionHelper.SelLimitType.End, selHelper.LevelInfo);

			// Prepare to move the IP to the specified character in the paragraph.
			selHelper.IchAnchor = startCharacter;
			selHelper.IchEnd = endCharacter;

			if (DeferSelectionUntilEndOfUOW)
			{
				// We are within a unit of work, so setting the selection will not work now.
				// we request that a selection be made after the unit of work.
				Debug.Assert(!selHelper.IsRange,
					"Currently, a selection made during a unit of work can only be an insertion point.");
				selHelper.SetIPAfterUOW(EditedRootBox.Site);
				return selHelper;
			}

			// Now that all the preparation to set the IP is done, set it.
			IVwSelection vwsel = selHelper.SetSelection(Callbacks.EditedRootBox.Site, fInstall,
				fMakeVisible, scrollOption);

			// If the selection fails, then try selecting the user prompt.
			if (vwsel == null)
			{
				selHelper.SetTextPropId(SelectionHelper.SelLimitType.Anchor, SimpleRootSite.kTagUserPrompt);
				selHelper.SetTextPropId(SelectionHelper.SelLimitType.End, SimpleRootSite.kTagUserPrompt);
				vwsel = selHelper.SetSelection(Callbacks.EditedRootBox.Site, fInstall, fMakeVisible,
					scrollOption);
			}

			if (vwsel == null)
			{
				Debug.WriteLine("SetSelection failed in TeEditingHelper.SelectRangeOfChars()");
			}

			return selHelper;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a flag indicating whether to defer setting a selection until the end of the
		/// Unit of Work.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual bool DeferSelectionUntilEndOfUOW
		{
			get { return m_cache.ActionHandlerAccessor.CurrentDepth > 0; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes a selection in a picture caption.
		/// </summary>
		/// <param name="iBook">The 0-based index of the Scripture book in which to put the
		/// insertion point</param>
		/// <param name="iSection">The 0-based index of the Scripture section in which to put the
		/// insertion point</param>
		/// <param name="tag">Indicates whether the picture ORC is in the section
		/// Heading or Content, the book title</param>
		/// <param name="iPara">The 0-based index of the paragraph containing the ORC</param>
		/// <param name="ichOrcPos">The character position of the orc in the paragraph.</param>
		/// <param name="startCharacter">The 0-based index of the character at which the
		/// selection begins (or before which the insertion point is to be placed if
		/// startCharacter == endCharacter)</param>
		/// <param name="endCharacter">The character location to end the selection</param>
		/// <exception cref="Exception">Requested selection could not be made in the picture
		/// caption.</exception>
		/// ------------------------------------------------------------------------------------
		public void MakeSelectionInPictureCaption(int iBook, int iSection, int tag,
			int iPara, int ichOrcPos, int startCharacter, int endCharacter)
		{
			CheckDisposed();

			if (Callbacks == null || Callbacks.EditedRootBox == null)
				throw new Exception("Requested selection could not be made in the picture caption.");

			Debug.Assert(tag == ScrSectionTags.kflidHeading ||
				tag == ScrSectionTags.kflidContent ||
				tag == ScrBookTags.kflidTitle);
			Debug.Assert(!IsBackTranslation, "ENHANCE: This code not designed to make a selection in the BT of a picture caption");

			SelectionHelper selHelper = new SelectionHelper();
			selHelper.NumberOfLevels = ((ITeView)Control).LocationTracker.GetLevelCount(tag) + 1;
			int levelForPara = LocationTrackerImpl.GetLevelIndexForTag(
				StTextTags.kflidParagraphs, StVc.ContentTypes.kctNormal) + 1;
			int levelForCaption = LocationTrackerImpl.GetLevelIndexForTag(
				CmPictureTags.kflidCaption, StVc.ContentTypes.kctNormal);
			selHelper.LevelInfo[levelForCaption].ihvo = -1;
			selHelper.LevelInfo[levelForCaption].ich = ichOrcPos;
			selHelper.LevelInfo[levelForCaption].tag = StTxtParaTags.kflidContents;
			selHelper.Ws = m_wsContainer.DefaultVernacularWritingSystem.Handle;
			selHelper.LevelInfo[levelForPara].tag = StTextTags.kflidParagraphs;
			selHelper.LevelInfo[levelForPara].ihvo = iPara;
			selHelper.LevelInfo[levelForPara + 1].tag = tag;

			((ITeView)Control).LocationTracker.SetBookAndSection(selHelper,
				SelectionHelper.SelLimitType.Anchor, iBook,
				tag == ScrBookTags.kflidTitle ? -1 : iSection);

			selHelper.AssocPrev = true;
			selHelper.SetLevelInfo(SelectionHelper.SelLimitType.End, selHelper.LevelInfo);

			// Prepare to move the IP to the specified character in the paragraph.
			selHelper.IchAnchor = startCharacter;
			selHelper.IchEnd = endCharacter;
			selHelper.SetTextPropId(SelectionHelper.SelLimitType.Anchor, CmPictureTags.kflidCaption);
			selHelper.SetTextPropId(SelectionHelper.SelLimitType.End, CmPictureTags.kflidCaption);

			// Now that all the preparation to set the IP is done, set it.
			IVwSelection vwsel = selHelper.SetSelection(Callbacks.EditedRootBox.Site, true,
				true, VwScrollSelOpts.kssoDefault);

			if (vwsel == null)
				throw new Exception("Requested selection could not be made in the picture caption.");

			Application.DoEvents(); // REVIEW: Do we need this? Why?
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes a simple text selection.
		/// </summary>
		/// <param name="newLevInfo">The new selection level info.</param>
		/// <param name="tag">The tag.</param>
		/// <param name="ich">The character offset where the IP should be placed.</param>
		/// ------------------------------------------------------------------------------------
		private void MakeSimpleTextSelection(SelLevInfo[] newLevInfo, int tag, int ich)
		{
			Callbacks.EditedRootBox.MakeTextSelection(
				0, // arbitrarily assume only one root.
				newLevInfo.Length, newLevInfo, // takes us to the paragraph
				tag, // select this property
				0, // first (and only) occurence of contents
				ich, ich, // starts and ends at this char index
				0, // ws is irrelevant (not multilingual)
				true, // associate with previous character...pretty arbitrary
				-1, // all in that one paragraph
				null, // don't impose any special properties for next char typed
				true); // install the new selection.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle TE specific requirements on selection change.
		/// </summary>
		/// <param name="rootb">The rootbox whose selection changed</param>
		/// <param name="vwselNew">The new selection</param>
		/// ------------------------------------------------------------------------------------
		public override void HandleSelectionChange(IVwRootBox rootb, IVwSelection vwselNew)
		{
			CheckDisposed();

			// selection change is being done by this routine, don't need to do processing
			// second time.
			if (m_selectionUpdateInProcess)
				return;

			SelectionHelper helper = CurrentSelection;

			UpdateGotoPassageControl(); // update the verse reference to the new selection
			SetInformationBarForSelection(); // update title bar with section reference range

			if (VernacularDraftVc != null)
				ProcessBTSelChange(helper);

			int hvoSelObj = 0;
			if (helper != null && helper.LevelInfo.Length > 0)
				hvoSelObj = helper.LevelInfo[0].hvo;

			ITeDraftView getVc = rootb.Site as ITeDraftView;
			TeStVc vc = (getVc != null) ? getVc.Vc : null;

			if (vc != null && vc.HvoOfSegmentWhoseBtPromptIsToBeSupressed != hvoSelObj)
			{
				vc.HvoOfSegmentWhoseBtPromptIsToBeSupressed = 0;
				// Enhance JohnT: do a Propchanged (possibly delayed until idle) on hvo.Comment to make the prompt reappear.
			}

			// If the selection is in a user prompt then extend the selection to cover the
			// entire prompt.
			if (IsSelectionInPrompt(helper))
			{
				if (vc != null)
					vc.HvoOfSegmentWhoseBtPromptIsToBeSupressed = hvoSelObj;

				// If we're not really showing the prompt, but just an incomplete composition that was typed
				// over it, we do NOT want to select all of it all the time! (TE-8267).
				if (!rootb.IsCompositionInProgress)
					vwselNew.ExtendToStringBoundaries();
				SetKeyboardForSelection(vwselNew);
			}

			// This isn't ideal but it's one of the better of several bad options for dealing
			// with simplifying the selection changes in footnote views.
			if ((m_viewType & TeViewType.FootnoteView) != 0 || helper == null)
			{
				// This makes sure the writing system and styles combos get updated.
				base.HandleSelectionChange(rootb, vwselNew);
				return;
			}

			// If selection is IP, don't allow it to be associated with a verse number run.
			bool fRangeSelection = vwselNew.IsRange;
			if (!fRangeSelection)
				PreventIPAssociationWithVerseRun(rootb);

			// Need to do this at end since selection may be changed by this method.
			// Doing this at top can also cause value of style in StylesComboBox to flash
			base.HandleSelectionChange(rootb, vwselNew);

			// Make sure the selection is in a valid Scripture element.
			int tagSelection;
			int hvoSelection;
			if (!vwselNew.IsValid || !GetSelectedScrElement(out tagSelection, out hvoSelection))
			{
				m_sPrevSelectedText = null;
				return;
			}

			// Determine whether or not the selection changed but is in a different reference.
			bool fInSameRef = (m_oldReference == CurrentStartRef);
			if (fInSameRef && !fRangeSelection)
			{
				m_sPrevSelectedText = null;
				SyncToScrLocation(true);
				return;
			}

			ScrReference curStartRef = CurrentStartRef;
			if (curStartRef.Chapter == 0)
				curStartRef.Chapter = 1;

			m_oldReference = CurrentStartRef;

			string selectedText = null;
			if (fRangeSelection)
			{
				try
				{
					ITsString tssSelectedText;
					vwselNew.GetSelectionString(out tssSelectedText, string.Empty);
					selectedText = (tssSelectedText != null ? tssSelectedText.Text : null);
				}
				catch
				{
					selectedText = null;
				}
			}

			bool fSameSelectedText = (m_sPrevSelectedText == selectedText);
			m_sPrevSelectedText = selectedText;

			if (!fInSameRef || !fSameSelectedText || InBookTitle || InSectionHead || InIntroSection)
				SyncToScrLocation(fInSameRef);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resends a synchronized scrolling message for the current Scripture reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool OkayToResendScriptureReference
		{
			get { return (m_oldReference != null); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes a selection change in the back translation.
		/// </summary>
		/// <param name="helper">The selection helper corresponding to the new selection.</param>
		/// ------------------------------------------------------------------------------------
		private void ProcessBTSelChange(SelectionHelper helper)
		{
			if (CurrentSelection != null && CurrentSelection.RootSite != null)
			{
				RootSite rootsite = CurrentSelection.RootSite as RootSite;
				if (rootsite != null && rootsite.InSelectionChanged)
					return;
			}

			if (helper != null && helper.NumberOfLevels > 2 &&
				helper.LevelInfo[0].tag == StTxtParaTags.kflidSegments)
			{
				// We're in a segmented back translation...try to highlight the main translation item.
				ISegment seg = m_repoSegment.GetObject(helper.LevelInfo[0].hvo);
				IScrTxtPara para = (IScrTxtPara)seg.Paragraph;
				TeStVc.DispPropInitializer colorProp = delegate(ref DispPropOverride prop)
				{
					prop.chrp.clrBack = ColorUtil.ConvertColorToBGR(
						TeResourceHelper.ReadOnlyTextBackgroundColor);
				};

				VernacularDraftVc.SetupOverrides(para, seg.BeginOffset, seg.EndOffset, colorProp,
					 VerncaularDraftView.RootBox);
			}
			else
			{
				// Not in segmented BT; remove any override. It's still important to pass the edited root box,
				// so the PropChanged doesn't affect us. We might still have a selection in the same paragraph,
				// but (e.g.) in a verse number.
				VernacularDraftVc.SetupOverrides(null, 0, 0, null, VerncaularDraftView.RootBox);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prevents IP-only selections from being associated with a verse number run.
		/// </summary>
		/// <param name="rootb">The root box.</param>
		/// ------------------------------------------------------------------------------------
		private void PreventIPAssociationWithVerseRun(IVwRootBox rootb)
		{
			SelectionHelper helper = CurrentSelection;

			// Get the tss that the IP is in
			ITsString tssSelectedScrPara = null;
			if (helper.LevelInfo.Length > 0)
			{
				ICmObject obj;
				if (!m_repoCmObject.TryGetObject(helper.LevelInfo[0].hvo, out obj))
					return;
				switch (obj.ClassID)
				{
					case CmTranslationTags.kClassId:
						int hvo = helper.LevelInfo[0].hvo;
						int btWs = Callbacks.GetWritingSystemForHvo(hvo);
						ICmTranslation trans = m_repoCmTrans.GetObject(hvo);
						tssSelectedScrPara = trans.Translation.get_String(btWs);
						break;
					case ScrTxtParaTags.kClassId:
						IStTxtPara para = m_repoScrTxtPara.GetObject(helper.LevelInfo[0].hvo);
						tssSelectedScrPara = para.Contents;
						break;
					case CmPictureTags.kClassId:
					case ScrFootnoteTags.kClassId:
					default:
						// Not sure what it is, but it probably doesn't contain verse text...
						break;
				}
			}

			// Following code includes checking zero-length paragraphs for association with
			// the VerseNumber style so that empty paras will use the default character style
			if (tssSelectedScrPara == null)
				return;

			// Get the text props and run info of the run the IP is associating with
			int charPos = helper.IchAnchor;

			if (helper.AssocPrev && charPos > 0)
				charPos -= 1;

			if (charPos > tssSelectedScrPara.Length) // workaround for TE-5561
				charPos = tssSelectedScrPara.Length;

			TsRunInfo tri;
			ITsTextProps ttp = tssSelectedScrPara.FetchRunInfoAt(charPos, out tri);

			// These are the boundary conditions that require our intervention
			// regarding verse numbers.
			bool fEdgeOfTss = (helper.IchAnchor == 0 ||
				helper.IchAnchor == tssSelectedScrPara.Length);
			bool fBeginOfRun = (helper.IchAnchor == tri.ichMin);
			bool fEndOfRun = (helper.IchAnchor == tri.ichLim);

			if ((!fEdgeOfTss && !fBeginOfRun && !fEndOfRun) ||
				ttp.Style() != ScrStyleNames.VerseNumber)
			{
				return;
			}

			// We must disassociate the IP from the verse number style run.
			if (fEdgeOfTss)
			{
				// If IP is at beginning or end of paragraph, need to reset selection to
				// default paragraph chars (null style).
				ITsPropsBldr bldr = ttp.GetBldr();
				bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, null);
				rootb.Selection.SetTypingProps(bldr.GetTextProps());
			}
			else
			{
				// Else make the selection be associated with the other adjacent run.
				rootb.Selection.AssocPrev = fBeginOfRun;
			}
		}

		#endregion

		#region GotoVerse/GetVerseText methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempt to set the selection immediately following the desired verse reference.
		/// This version of GotoVerse sends a synch message at the end, so it should not be
		/// called from within code that is already responding to a synch message.
		/// </summary>
		/// <param name="targetRef">Reference to seek</param>
		/// <returns>true if the selection is changed (to the requested verse or one nearby);
		/// false otherwise</returns>
		/// <remarks>
		/// Searching will start at the current selection location and wrap around if necessary.
		/// If the verse reference is not included in the range of any sections, then the
		/// selection will not be changed. If the reference does not exist but it is in the
		/// range of a section, then a best guess will be done.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual bool GotoVerse(ScrReference targetRef)
		{
			bool retVal = GotoVerse_WithoutSynchMsg(targetRef);

			SyncToScrLocation(false);

			return retVal;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempt to set the selection immediately following the desired verse reference.
		/// This version of GotoVerse does not issue a synch message, so it is suitable for
		/// calling from the
		/// </summary>
		/// <param name="targetRef">Reference to seek</param>
		/// <returns>true if the selection is changed (to the requested verse or one nearby);
		/// false otherwise</returns>
		/// <remarks>
		/// Searching will start at the current selection location and wrap around if necessary.
		/// If the verse reference is not included in the range of any sections, then the
		/// selection will not be changed. If the reference does not exist but it is in the
		/// range of a section, then a best guess will be done.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		private bool GotoVerse_WithoutSynchMsg(ScrReference targetRef)
		{
			CheckDisposed();

			using (new IgnoreSynchMessages(TheMainWnd))
			{
				IScrBook bookToLookFor = BookFilter.GetBookByOrd(targetRef.Book);
				if (bookToLookFor == null)
					return false;

				int iBook = BookFilter.GetBookIndex(bookToLookFor);

				if (targetRef.IsBookTitle)
				{
					SelectRangeOfChars(iBook, -1, ScrBookTags.kflidTitle, 0, 0, 0,
						true, true, false, VwScrollSelOpts.kssoNearTop);
					return true;
				}

				// If the book has no sections, then don't look for any references
				Debug.Assert(bookToLookFor.SectionsOS.Count > 0);
				if (bookToLookFor.SectionsOS.Count == 0)
					return false;

				int startingSectionIndex = 0;
				int startingParaIndex = 0;
				int startingCharIndex = 0;

				// Get the current selection
				if (CurrentSelection != null)
				{
					ILocationTracker tracker = ((ITeView)Control).LocationTracker;
					// If the selection is in the desired book, we start there.
					// Otherwise start at the beginning of the book
					if (tracker.GetBook(CurrentSelection,
						SelectionHelper.SelLimitType.Anchor) == bookToLookFor)
					{
						int tmpSectionIndex = tracker.GetSectionIndexInBook(
							CurrentSelection, SelectionHelper.SelLimitType.Anchor);

						if (tmpSectionIndex >= 0)
						{
							startingSectionIndex = tmpSectionIndex;
							SelLevInfo paraInfo;
							if (CurrentSelection.GetLevelInfoForTag(
								StTextTags.kflidParagraphs, out paraInfo))
							{
								startingParaIndex = paraInfo.ihvo;
								// Start looking 1 character beyond the current selection.
								startingCharIndex = CurrentSelection.IchEnd + 1;
							}
						}
					}
				}

				IScrSection startingSection = bookToLookFor[startingSectionIndex];
				IScrSection section;
				int paraIndex;
				int ichVerseStart;

				// Decide which section to start with. If the current selection
				// is at the end of the section then start with the next section
				IStTxtPara lastParaOfSection = startingSection.LastContentParagraph;
				if (startingParaIndex >= startingSection.ContentParagraphCount - 1 &&
					startingCharIndex >= lastParaOfSection.Contents.Length)
				{
					startingSection = (startingSection.NextSection ?? bookToLookFor.FirstSection);
					startingParaIndex = 0;
					startingCharIndex = 0;
				}

				if (bookToLookFor.GetRefStartFromSection(targetRef, false, startingSection, startingParaIndex,
					startingCharIndex, out section, out paraIndex, out ichVerseStart))
				{
					// We found an exact match, so go there.
					GoToPosition(targetRef, iBook, section, paraIndex, ichVerseStart);
				}
				else
				{
					// We didn't find an exact match, so go somewhere close.
					GotoClosestPrecedingRef(targetRef, bookToLookFor);
				}

				return true;
			}
		}

		#region class VerseTextSubstring
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simple class to keep track of pieces of a verse text which are split across
		/// different paragraphs and/or sections.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public class VerseTextSubstring
		{
			private ITsString m_tssText;
			private int m_iSection;
			private int m_iPara;
			private int m_ich;
			private int m_tag;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="VerseTextSubstring"/> class.
			/// </summary>
			/// <param name="text">The text.</param>
			/// <param name="iSection">The index of the section containing this piece of verse
			/// text.</param>
			/// <param name="iPara">The index of the paragraph containing this piece of verse
			/// text.</param>
			/// <param name="ich">The character offset within the paragraph to the start of the
			/// verse for this part (will always be 0 for any part other than the first one)
			/// </param>
			/// <param name="tag">The tag of the text.</param>
			/// --------------------------------------------------------------------------------
			public VerseTextSubstring(ITsString text, int iSection, int iPara, int ich, int tag)
			{
				m_tssText = text;
				m_iSection = iSection;
				m_iPara = iPara;
				m_ich = ich;
				m_tag = tag;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the starting character offset of this substring, relative to the
			/// containing paragraph.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public int StartOffset
			{
				get { return m_ich; }
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the limit of this substring, relative to the containing paragraph.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public int EndOffset
			{
				get { return m_ich + m_tssText.Length; }
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the verse text.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public string Text
			{
				get { return m_tssText.Text; }
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets or sets the verse text.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public ITsString Tss
			{
				get { return m_tssText; }
				set { m_tssText = value; }
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets or sets the index of the section.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public int SectionIndex
			{
				get { return m_iSection; }
				set { m_iSection = value; }
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets or sets the index of the paragraph.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public int ParagraphIndex
			{
				get { return m_iPara; }
				set { m_iPara = value; }
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the tag.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public int Tag
			{
				get { return m_tag; }
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Returns a <see cref="T:System.String"/> that represents this
			/// <see cref="VerseTextSubstring"/>.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public override string ToString()
			{
				return Text;
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Implicit conversion of a <see cref="VerseTextSubstring"/> to a string
			/// </summary>
			/// <param name="verseTextSubstring">The <see cref="VerseTextSubstring"/> to be cast</param>
			/// <returns>A string containing the verse text of this portion of the verse</returns>
			/// ------------------------------------------------------------------------------------
			public static implicit operator string(VerseTextSubstring verseTextSubstring)
			{
				return verseTextSubstring.Text;
			}
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find a verse and return the text of the verse in one or more
		/// <see cref="VerseTextSubstring"/> objects.
		/// </summary>
		/// <param name="scr">The scripture.</param>
		/// <param name="targetRef">The verse reference to look for</param>
		/// <returns>
		/// The <see cref="VerseTextSubstring"/> objects, each representing
		/// one paragraph worth of verse text (e.g., to deal with poetry)
		/// </returns>
		/// <remarks>Verses would not normally be split across sections, but there are a few
		/// places, such as the end of I Cor. 12, where it can happen.
		/// ENHANCE: Logicially, this method probably really belongs on Scripture, but currently
		/// that FDO doesn't reference ScrUtils (which is where ScrReference is). It probably
		/// could reference it, if we wanted it to. Another option would be to create an
		/// extension method for it, but I'm not sure where it would go.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public static IEnumerable<VerseTextSubstring> GetVerseText(IScripture scr, ScrReference targetRef)
		{
			if (scr.Versification != targetRef.Versification)
				targetRef = new ScrReference(targetRef, scr.Versification);

			List<VerseTextSubstring> verseText = new List<VerseTextSubstring>();

			// Find the book that the reference is in
			IScrBook book = scr.FindBook(targetRef.Book);
			if (book == null)
				return verseText;

			if (targetRef.IsBookTitle)
			{
				foreach (IStTxtPara para in book.TitleOA.ParagraphsOS)
				{
					verseText.Add(new VerseTextSubstring(para.Contents, -1,
						para.IndexInOwner, 0, ScrBookTags.kflidTitle));
				}
				return verseText;
			}

			int iSection = 0;
			// Look through the sections for the target reference
			foreach (IScrSection section in book.SectionsOS)
			{
				if (!section.ContainsReference(targetRef))
				{
					if (verseText.Count > 0)
						return verseText;
				}
				else
				{
					int iPara = 0;
					// Look through each paragraph in the section
					foreach (IScrTxtPara para in section.ContentOA.ParagraphsOS)
					{
						// Search for target reference in the verses in the paragraph
						using (ScrVerseSet verseSet = new ScrVerseSet(para))
						{
							foreach (ScrVerse verse in verseSet)
							{
								if (verse.StartRef <= targetRef && targetRef <= verse.EndRef)
								{
									// If the paragraph has a chapter number, the verse iterator
									// returns this as a separate string with the same reference
									// as the following verse.
									// We want to return the verse string, not the chapter number
									// run, so we skip a string that has only numeric characters.
									ITsString verseTextInPara = verse.Text;
									if (verse.Text.RunCount > 0 &&
										verse.Text.Style(0) == ScrStyleNames.VerseNumber)
									{
										verseTextInPara = verseTextInPara.Substring(verse.Text.get_LimOfRun(0));
									}

									if (!IsNumber(verseTextInPara.Text)) // skip chapter number strings
									{
										int ichStart = (verseText.Count == 0) ? verse.TextStartIndex : 0;
										verseText.Add(new VerseTextSubstring(verseTextInPara, iSection,
											iPara, ichStart, ScrSectionTags.kflidContent));
										break;
									}
								}
								else if (verseText.Count > 0)
									return verseText;
							}
						}
						iPara++;
					}
				}
				iSection++;
			}

			return verseText;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Look for the given picture's ORC in the given verse.
		/// </summary>
		/// <param name="targetRef">The verse reference to look for</param>
		/// <param name="hvoPict">The hvo of the picture to look for.</param>
		/// <param name="iSection">The index of the section where the ORC was found.</param>
		/// <param name="iPara">The index of the para where the ORC was found.</param>
		/// <param name="ichOrcPos">The character position of the ORC in the paragraph.</param>
		/// <returns>
		/// 	<c>true</c> if the given picture is found in the given verse.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool FindPictureInVerse(ScrReference targetRef, int hvoPict, out int iSection,
			out int iPara, out int ichOrcPos)
		{
			CheckDisposed();

			iSection = iPara = ichOrcPos = -1;

			// Find the book that the reference is in
			IScrBook book = m_scr.FindBook(targetRef.Book);
			if (book == null)
				return false;

			iSection = 0;
			// Look through the sections for the target reference
			foreach (IScrSection section in book.SectionsOS)
			{
				if (section.ContainsReference(targetRef))
				{
					iPara = 0;
					// Look through each paragraph in the section
					foreach (IScrTxtPara para in section.ContentOA.ParagraphsOS)
					{
						// Search for target reference in the verses in the paragraph
						using (ScrVerseSet verseSet = new ScrVerseSet(para))
						{
						foreach (ScrVerse verse in verseSet)
						{
							if (verse.StartRef <= targetRef && targetRef <= verse.EndRef)
							{
								// If the paragraph has a chapter number, the verse iterator
								// returns this as a separate string with the same reference
								// as the following verse.
								// We want to return the verse string, not the chapter number
								// run, so we skip a string that has only numeric characters.
								ITsString tssVerse = verse.Text;
								for (int iRun = 0; iRun < tssVerse.RunCount; iRun++)
								{
									string sRun = tssVerse.get_RunText(iRun);
									if (sRun.Length == 1 && sRun[0] == StringUtils.kChObject)
									{
										string str = tssVerse.get_Properties(iRun).GetStrPropValue(
											(int)FwTextPropType.ktptObjData);

										if (!String.IsNullOrEmpty(str) && str[0] == (char)(int)FwObjDataTypes.kodtGuidMoveableObjDisp)
										{
											Guid guid = MiscUtils.GetGuidFromObjData(str.Substring(1));
											if (m_repoCmObject.GetObject(guid).Hvo == hvoPict)
											{
												ichOrcPos = tssVerse.get_MinOfRun(iRun) + verse.VerseStartIndex;
												return true;
											}
										}
									}
								}
							}
						}
						}
						iPara++;
					}
				}
				iSection++;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Selects text in the given verse.
		/// </summary>
		/// <param name="scrRef">The Scripture reference of the verse.</param>
		/// <param name="text">The specific text within the verse to look for (<c>null</c> to
		/// select the text of the entire verse.</param>
		/// <remarks>
		/// REVIEW (TE-4218): Do we need to add a parameter to make it possible to do a case-
		/// insensitive match?
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public void SelectVerseText(ScrReference scrRef, ITsString text)
		{
			SelectVerseText(scrRef, text, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Selects text in the given verse.
		/// </summary>
		/// <param name="scrRef">The Scripture reference of the verse.</param>
		/// <param name="text">The specific text within the verse to look for (<c>null</c> to
		/// select the text of the entire verse.</param>
		/// <param name="fSynchScroll">if set to <c>true</c> then use the flavor of GotoVerse
		/// that will send a synch. message. Otherwise, the other flavor is used.</param>
		/// <remarks>
		/// REVIEW (TE-4218): Do we need to add a parameter to make it possible to do a case-
		/// insensitive match?
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public void SelectVerseText(ScrReference scrRef, ITsString text, bool fSynchScroll)
		{
			if (scrRef.Versification != m_scr.Versification)
				scrRef = new ScrReference(scrRef, m_scr.Versification);

			if (!(fSynchScroll ? GotoVerse(scrRef) : GotoVerse_WithoutSynchMsg(scrRef)))
				return;

			using (new IgnoreSynchMessages(TheMainWnd))
			{
				// We successfully navigated to the verse (or somewhere close) so attempt to make a
				// selection there.
				int ichStart, ichEnd;
				if (text == null || text.Text == null)
				{
					IEnumerable<VerseTextSubstring> verseTexts = GetVerseText(m_scr, scrRef);
					VerseTextSubstring firstPart = verseTexts.FirstOrDefault();
					if (firstPart == null)
						return; // Must not have found the exact verse
					VerseTextSubstring lastPart = verseTexts.Last();

					SelectionHelper helper = CurrentSelection;
					helper.IchEnd = lastPart.EndOffset;
					if (lastPart.SectionIndex != firstPart.SectionIndex)
					{
						SelLevInfo[] levInfo = helper.GetLevelInfo(SelectionHelper.SelLimitType.End);
						int iParaLev = LocationTrackerImpl.GetLevelIndexForTag(StTextTags.kflidParagraphs,
							StVc.ContentTypes.kctNormal);
						int iSectLev = LocationTrackerImpl.GetLevelIndexForTag(ScrBookTags.kflidSections,
							StVc.ContentTypes.kctNormal);
						levInfo[iParaLev].ihvo = lastPart.ParagraphIndex;
						levInfo[iSectLev].ihvo += (lastPart.SectionIndex - firstPart.SectionIndex);
						helper.SetLevelInfo(SelectionHelper.SelLimitType.End, levInfo);
					}
					else if (firstPart != lastPart)
					{
						SelLevInfo[] levInfo = helper.GetLevelInfo(SelectionHelper.SelLimitType.End);
						int iParaLev = LocationTrackerImpl.GetLevelIndexForTag(StTextTags.kflidParagraphs,
							StVc.ContentTypes.kctNormal);
						levInfo[iParaLev].ihvo = lastPart.ParagraphIndex;
						helper.SetLevelInfo(SelectionHelper.SelLimitType.End, levInfo);
					}
					helper.SetSelection(true);
				}
				else
				{
					int iSection, iPara;
					if (FindTextInVerse(m_scr, text, scrRef, false, out iSection, out iPara, out ichStart, out ichEnd))
					{
						// We found the text in the verse.
						if (!InBookTitle)
							SelectRangeOfChars(BookIndex, iSection, iPara, ichStart, ichEnd);
						else
						{
							SelectRangeOfChars(BookIndex, iSection,
								ScrBookTags.kflidTitle, iPara,
								ichStart, ichEnd, true, true, false);
						}
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the given string contains only digits.
		/// </summary>
		/// <param name="str">given string</param>
		/// <returns>true, if string only contains digits; false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		private static bool IsNumber(string str)
		{
			if (String.IsNullOrEmpty(str))
				return false;
			foreach (char character in str)
			{
				// Non-numeric character found?
				if (!char.IsDigit(character))
					return false;
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Goes to the verse reference in the back translation paragraph or the closest
		/// location to where it would occur
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void GotoVerseBT(ScrReference targetRef, int bookIndex,
			IScrSection section, int paraIndex, IScrTxtPara para)
		{
			using (ScrVerseSet verseSet = new ScrVerseSetBT(para, ViewConstructorWS))
			{
			// Look for an exact match of the target verse number in the BT para
			foreach (ScrVerse verse in verseSet)
			{
				if (!verse.VerseNumberRun)
					continue;

				if (verse.StartRef <= targetRef && targetRef <= verse.EndRef)
				{
					// set the IP here now
					SetInsertionPoint(bookIndex, section.IndexInOwner, paraIndex,
						verse.TextStartIndex, false);
					return;
				}
			}

			// An exact match was not found, so look for a best guess spot for it
			foreach (ScrVerse verse in verseSet)
			{
				if (!verse.VerseNumberRun)
					continue;

				if (verse.StartRef >= targetRef)
				{
					// set the IP here now
					SetInsertionPoint(bookIndex, section.IndexInOwner, paraIndex, verse.VerseStartIndex, false);
					return;
				}
			}

			// A best guess spot was not found so put the selection at the end of the paragraph
			SetIpAtEndOfPara(bookIndex, section.IndexInOwner, paraIndex, para);
		}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Go to the indicated verse in the BT. We've already determined the index of the book, the section,
		/// the paragraph, and the character index in the vernacular. We want to figure the corresponding
		/// position in the BT, which should be the non-label segment closest to (hopefully containing) the
		/// vernacular position, and select it.
		/// </summary>
		/// <param name="targetRef">The target reference.</param>
		/// <param name="bookIndex">Index of the book.</param>
		/// <param name="section">The section.</param>
		/// <param name="paraIndex">Index of the paragraph.</param>
		/// <param name="para">The paragraph.</param>
		/// <param name="ichMainPosition">The position where this verse occurs in the main paragraph.</param>
		/// ------------------------------------------------------------------------------------
		private void GotoVerseBtSeg(ScrReference targetRef, int bookIndex,
			IScrSection section, int paraIndex, IScrTxtPara para, int ichMainPosition)
		{
			int isegTarget = GetBtSegIndexForVernChar(para, ichMainPosition, ViewConstructorWS);
			// Select the appropriate segment (or if nothing matched, the last place we can edit).
			if (isegTarget < 0)
				return; // pathological.

			SelectRangeOfChars(bookIndex, section.IndexInOwner, ScrSectionTags.kflidContent, paraIndex, isegTarget,
				0, 0, true, true, false, VwScrollSelOpts.kssoDefault);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a character position in the contents of an ScrTxtPara, return the index of the
		/// corresponding segment (or the closest editable one).
		/// </summary>
		/// <param name="para">The para.</param>
		/// <param name="ichMainPosition">The ich main position.</param>
		/// <param name="btWs">The bt ws.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static int GetBtSegIndexForVernChar(IScrTxtPara para, int ichMainPosition, int btWs)
		{
			int isegTarget = -1;
			for (int i = 0; i < para.SegmentsOS.Count; i++)
			{
				ISegment seg = para.SegmentsOS[i];
				// If it's a 'label' segment, it's not where we want to put the IP.
				if (seg.IsLabel)
					continue;
				isegTarget = i;
				if (ichMainPosition <= seg.EndOffset)
					break; // don't consider any later segment
			}
			return isegTarget;
		}

		// This is the start of an alternative implementation of GotoVerseBtSeg, actually searching the text.
		//BCVRef m_startRef;
			//BCVRef m_endRef;
			//para.GetRefsAtPosition(0, out m_startRef, out m_endRef);
			//int ktagParaSegments = StTxtPara.SegmentsFlid(Cache);
			//int cseg = Cache.GetVectorSize(para.Hvo, ktagParaSegments);
			//int kflidFT = StTxtPara.SegmentFreeTranslationFlid(Cache);
			//ISilDataAccess sda = Cache.MainCacheAccessor;
			//int btWs = ViewConstructorWS;
			//for (int iseg = 0; iseg < cseg; iseg++)
			//{
			//    int hvoSeg = sda.get_VecItem(para.Hvo, ktagParaSegments, iseg);
			//    if (!sda.get_IsPropInCache(hvoSeg, kflidFT, CellarPropertyType.ReferenceAtomic, 0))
			//    {
			//        StTxtPara.LoadSegmentFreeTranslations(new int[] {para.Hvo}, Cache, btWs);
			//    }
			//    int hvoFt = sda.get_ObjectProp(hvoSeg, kflidFT);
			//    ITsString tssTrans = sda.get_MultiStringAlt(hvoFt, kflidComment, btWs);
			//    int crun = tssTrans.RunCount;
			//    for (int irun = 0; irun < crun; irun++)
			//    {
			//        ITsTextProps ttpRun = tssTrans.get_Properties(irun);
			//        if (StStyle.IsStyle(ttpRun, ScrStyleNames.VerseNumber))
			//        {
			//            string sVerseNum = tssTrans.get_RunText(irun);
			//            int nVerseStart, nVerseEnd;
			//            ScrReference.VerseToInt(sVerseNum, out nVerseStart, out nVerseEnd);
			//            m_startRef.Verse = nVerseStart;
			//            m_endRef.Verse = nVerseEnd;
			//        }
			//        else if (StStyle.IsStyle(ttpRun, ScrStyleNames.ChapterNumber))
			//        {
			//            string sChapterNum = tssTrans.get_RunText(irun);
			//            int nChapter = ScrReference.ChapterToInt(sChapterNum);
			//            m_startRef.Chapter = m_endRef.Chapter = nChapter;
			//            // Set the verse number to 1, since the first verse number after a
			//            // chapter is optional. If we happen to get a verse number in the
			//            // next run, this '1' will be overridden (though it will probably
			//            // still be a 1).
			//            m_startRef.Verse = m_endRef.Verse = 1;
			//        }
			//    }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts the IP and scrolls it to near the top of the view. Used for going to a
		/// location in the BT or vernacular where a verse starts (or as close as possible).
		/// </summary>
		/// <param name="targetRef">ScrReference to find</param>
		/// <param name="bookIndex">index of book to look in</param>
		/// <param name="section">section to search</param>
		/// <param name="paraIndex">paragraph to look in</param>
		/// <param name="ichPosition">starting character index to look at</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void GoToPosition(ScrReference targetRef, int bookIndex,
			IScrSection section, int paraIndex, int ichPosition)
		{
			if (ContentType == StVc.ContentTypes.kctSimpleBT)
			{
				GotoVerseBT(targetRef, bookIndex, section, paraIndex, (IScrTxtPara)section.ContentOA[paraIndex]);
			}
			else if (ContentType == StVc.ContentTypes.kctSegmentBT)
			{
				GotoVerseBtSeg(targetRef, bookIndex, section, paraIndex,
					(IScrTxtPara)section.ContentOA[paraIndex], ichPosition);
			}
			else
			{
				SetInsertionPoint(ScrSectionTags.kflidContent, bookIndex, section.IndexInOwner,
					paraIndex, ichPosition, false, VwScrollSelOpts.kssoNearTop);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the IP to the end of the section. Check for vernacular or BT.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetIpAtEndOfPara(int bookIndex, int sectionIndex, int paraIndex, IStTxtPara para)
		{
			int paraLength;
			if (IsBackTranslation)
			{
				ICmTranslation trans = para.GetOrCreateBT();
				paraLength = trans.Translation.get_String(ViewConstructorWS).Length;
			}
			else
				paraLength = para.Contents.Length;

			SetInsertionPoint(bookIndex, sectionIndex, paraIndex, paraLength, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Goes to the closest match in the given book.
		/// </summary>
		/// <param name="targetRef">ScrReference to find</param>
		/// <param name="book">index of book to look in</param>
		/// <returns><c>false</c> if we can't go to the closest match </returns>
		/// ------------------------------------------------------------------------------------
		protected virtual void GotoClosestPrecedingRef(ScrReference targetRef, IScrBook book)
		{
			Debug.Assert(book != null);
			Debug.Assert(book.SectionsOS.Count > 0);
			IScrSection section = null;

			// Move backward through the sections in the book to find the one
			// whose start reference is less than the one we're looking for.
			for (int iSection = book.SectionsOS.Count - 1; iSection >= 0; iSection--)
			{
				section = book[iSection];

				// If the reference we're looking for is greater than the current
				// section's start reference, then get out of the loop because we've
				// found the section in which we need to place the IP.
				if (targetRef >= section.VerseRefStart)
					break;
			}

			// At this point, we know we have the section in which we think the
			// IP should be located.
			int iBook = BookFilter.GetBookIndex(book);

			// If the reference we're looking for is before the section's start reference,
			// then we need to put the IP at the beginning of the book's first section,
			// but after a chapter number if the sections begins with one.
			if (targetRef < section.VerseRefStart)
			{
				GoToFirstChapterInSection(iBook, section);
				return;
			}

			int paraCount = section.ContentParagraphCount;

			// If there are no paragraphs in the section, then we're out of luck.
			Debug.Assert(paraCount > 0);

			// Go through the paragraphs and find the one in which we
			// think the IP should be located.
			for (int iPara = paraCount - 1; iPara >= 0; iPara--)
			{
				ScrVerseList verses = new ScrVerseList((IScrTxtPara)section.ContentOA[iPara]);

				// Go backward through the verses in the paragrah, looking for the
				// first one that is less than the reference we're looking for.
				for (int iVerse = verses.Count - 1; iVerse >= 0; iVerse--)
				{
					// If the current reference is before (i.e. less) the one we're looking
					// for,	then put the IP right after the it's verse number.
					if (verses[iVerse].StartRef <= targetRef)
					{
						GoToPosition(targetRef, iBook, section,
							iPara, verses[iVerse].TextStartIndex);

						return;
					}
				}
			}

			// At this point, we have failed to find a good location for the IP.
			// Therefore, just place it at the beginning of the section.
			GoToSectionStart(iBook, section.IndexInOwner);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the beginning of the text of the first (implicit or explicit) chapter
		/// in the first paragraph in the specified scripture section. If a chapter isn't
		/// found then the IP is put at the beginning of the content paragraph.
		/// </summary>
		/// <param name="iBook">0-based index of the book (in this view)</param>
		/// <param name="section">The section (in the given book, as displayed in this view)
		/// </param>
		/// ------------------------------------------------------------------------------------
		private void GoToFirstChapterInSection(int iBook, IScrSection section)
		{
			int ichChapterStart = 0;
			using (ScrVerseSet verseSet = new ScrVerseSet((IScrTxtPara)section.ContentOA[0]))
			{
			if (verseSet.MoveNext())
			{
				ScrVerse verse = verseSet.Current;
				if (!verse.VerseNumberRun)
					ichChapterStart = verse.TextStartIndex;
			}
			}

			ScrReference scrRef = new ScrReference(section.VerseRefStart, m_scr.Versification);
			GoToPosition(scrRef, iBook, section, 0, ichChapterStart);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine chapter before current reference.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ScrReference GetPrevChapter()
		{
			CheckDisposed();

			ScrReference curStartRef = CurrentStartRef;
			if (curStartRef.Book == 0)
				return ScrReference.Empty;

			if (curStartRef.Chapter == 0)
			{
				// Hmmm... we're probably at the beginning of the book already, and in the
				// title or intro area.  We should move into the previous book.
				if (BookIndex > 0)
				{
					IScrBook book = BookFilter.GetBook(BookIndex - 1);
					int chapter = 0;
					foreach (IScrSection section in book.SectionsOS)
					{
						ScrReference minRef = new ScrReference(section.VerseRefMin, m_scr.Versification);
						ScrReference maxRef = new ScrReference(section.VerseRefMax, m_scr.Versification);

						if (minRef.Valid && maxRef.Valid)
						{
							for (int i = BCVRef.GetChapterFromBcv(section.VerseRefMin); i <= BCVRef.GetChapterFromBcv(section.VerseRefMax); i++)
							{
								if (i > chapter)
									chapter = i;
							}
						}
					}

					return new ScrReference((short)book.CanonicalNum, chapter, 1, m_scr.Versification);
				}

				if (BookIndex == 0)
				{
					// Guess what, we're at the beginning, just move to the first
					// chapter/verse in the book.
					IScrBook book = BookFilter.GetBook(0);
					return new ScrReference((short)book.CanonicalNum, 1, 1, m_scr.Versification);
				}
			}
			else
			{
				// Standard yum-cha situation here... move up to the previous chapter
				// and be happy.
				if (BookIndex >= 0 && SectionIndex >= 0)
				{
					IScrBook book = BookFilter.GetBook(BookIndex);
					if (curStartRef.Chapter > 1)
					{
						return new ScrReference((short)book.CanonicalNum,
							curStartRef.Chapter - 1, 1, m_scr.Versification);
					}

					if (BookIndex > 0)
					{
						// We are at the first chapter of the book so move to the last chapter in
						// the previous book.
						book = BookFilter.GetBook(BookIndex - 1);
						int chapter = 0;
						if (book.SectionsOS.Count > 0)
							chapter = BCVRef.GetChapterFromBcv(book.LastSection.VerseRefMax);

						return new ScrReference((short)book.CanonicalNum, chapter, 1,
							m_scr.Versification);
					}

					return new ScrReference((short)book.CanonicalNum, 1, 1, m_scr.Versification);
				}
			}

			// Splode?
			return ScrReference.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines next chapter following current reference
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ScrReference GetNextChapter()
		{
			CheckDisposed();

			ScrReference curEndRef = CurrentEndRef;
			if (curEndRef.Book == 0)
			{
				// REVIEW BobA(TimS): Something went wrong, let's move to the first verse
				// we know of... Alternative is to say oh well, and do nothing at all.
				return ScrReference.StartOfBible(curEndRef.Versification);
			}

			if (curEndRef.Chapter == 0)
			{
				// Hmmm... we're probably at the beginning of the book already, and in the
				// title or intro area. We should move into the previous book.
				if (BookIndex >= 0)
				{
					// We are in the intro section area, we should move to the
					// first verse of the first chapter
					IScrBook book = BookFilter.GetBook(BookIndex);
					return new ScrReference((short)book.CanonicalNum, 1, 1, m_scr.Versification);
				}
			}
			else
			{
				// Standard yum-cha situation here... move up to the next chapter
				// and be happy.
				if (BookIndex >= 0 && SectionIndex >= 0)
				{
					IScrBook book = BookFilter.GetBook(BookIndex);
					int lastChapter = 0;
					foreach (IScrSection section in book.SectionsOS)
					{
						ScrReference maxSectionRef =
							new ScrReference(section.VerseRefMax, m_scr.Versification);
						ScrReference minSectionRef =
							new ScrReference(section.VerseRefMin, m_scr.Versification);

						if (minSectionRef.Valid && maxSectionRef.Valid &&
							maxSectionRef.Chapter > lastChapter)
						{
							lastChapter = maxSectionRef.Chapter;
						}
					}

					if (BookIndex == BookFilter.BookCount - 1 &&
						curEndRef.Chapter == lastChapter)
					{
						// We're at the end of the book, nowhere to go...
						return new ScrReference((short)book.CanonicalNum, lastChapter, 1,
							m_scr.Versification);
					}

					if (curEndRef.Chapter == lastChapter)
					{
						// We're at the end of the book, nowhere to go...
						book = BookFilter.GetBook(BookIndex + 1);
						return new ScrReference((short)book.CanonicalNum, 1, 1,
							m_scr.Versification);
					}

					// Otherwise, move on.... we were bored of this part anyhow...
					return new ScrReference((short)book.CanonicalNum, curEndRef.Chapter + 1,
						1, m_scr.Versification);
				}
			}

			// Splode?
			return ScrReference.StartOfBible(curEndRef.Versification);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates reference for the last chapter of the currently referenced book.
		/// </summary>
		/// <returns>a Scripture reference for the last chapter of the book</returns>
		/// ------------------------------------------------------------------------------------
		public ScrReference GetLastChapter()
		{
			CheckDisposed();

			IScrBook book = BookFilter.GetBook(BookIndex);
			int chapter = 0;
			if (book.SectionsOS.Count > 0)
				chapter = BCVRef.GetChapterFromBcv(book.LastSection.VerseRefMax);

			return new ScrReference((short)book.CanonicalNum, chapter, 1, m_scr.Versification);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Goes to the text referenced in the ScrScriptureNote. If it does not find the text,
		/// it makes the closest selection to the referenced text that it can.
		/// </summary>
		/// <param name="note">the note containing the Scripture reference to find</param>
		/// ------------------------------------------------------------------------------------
		public void GoToScrScriptureNoteRef(IScrScriptureNote note)
		{
			GoToScrScriptureNoteRef(note, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Goes to the text referenced in the ScrScriptureNote. If it does not find the text,
		/// it makes the closest selection to the referenced text that it can.
		/// </summary>
		/// <param name="note">the note containing the Scripture reference to find</param>
		/// <param name="sendSyncMsg"><c>true</c> to not send a focus sychronization message
		/// when the selection is changed by going to the scripture ref.</param>
		/// ------------------------------------------------------------------------------------
		public void GoToScrScriptureNoteRef(IScrScriptureNote note, bool sendSyncMsg)
		{
			// TODO (TE-1729): Use this method correctly from Annotations view.

			ScrReference scrRef = new ScrReference(note.BeginRef, m_scr.Versification);
			IScrBook book = BookFilter.GetBookByOrd(scrRef.Book);
			if (book == null)
				return;

			int iBook = BookFilter.GetBookIndex(book);

			using (new IgnoreSynchMessages(TheMainWnd))
			{
				if (note.Flid == CmPictureTags.kflidCaption)
				{
					SelectCitedTextInPictureCaption(iBook, note);
					return;
				}

				int ichStart, ichEnd;
				string citedText = note.CitedText;
				ITsString citedTextTss = note.CitedTextTss;
				IStTxtPara para = note.BeginObjectRA as IStTxtPara;
				if (para != null && para.OwnerOfClass<IScrDraft>() == null)
				{
					if (para.Owner is IStFootnote)
					{
						// Make selection in footnote.
						if (TextAtExpectedLoc(para.Contents.Text, citedText, note.BeginOffset, note.EndOffset))
						{
							// Select text in footnote.
							IStFootnote footnote = (IStFootnote)para.Owner;

							SelectionHelper selHelper = new SelectionHelper();
							selHelper.AssocPrev = false;
							selHelper.NumberOfLevels = 3;
							selHelper.LevelInfo[2].tag = BookFilter.Tag;
							selHelper.LevelInfo[2].ihvo = iBook;
							selHelper.LevelInfo[1].tag = ScrBookTags.kflidFootnotes;
							selHelper.LevelInfo[1].ihvo = footnote.IndexInOwner;
							selHelper.LevelInfo[0].tag = StTextTags.kflidParagraphs;
							selHelper.LevelInfo[0].ihvo = 0;

							// Prepare to move the IP to the specified character in the paragraph.
							selHelper.IchAnchor = note.BeginOffset;
							selHelper.IchEnd = note.EndOffset;
							selHelper.SetTextPropId(SelectionHelper.SelLimitType.Anchor, StTxtParaTags.kflidContents);
							selHelper.SetTextPropId(SelectionHelper.SelLimitType.End, StTxtParaTags.kflidContents);

							// Now that all the preparation to set the IP is done, set it.
							selHelper.SetSelection(Callbacks.EditedRootBox.Site, true, true);
						}
						return;
					}

					// Make selection in Scripture text
					if (TextAtExpectedLoc(para.Contents.Text, citedText, note.BeginOffset, note.EndOffset))
					{
						SelectRangeOfChars(iBook, para, note.BeginOffset, note.EndOffset);
						return;
					}

					if (scrRef.Verse == 0)
					{
						// Either a missing chapter number or something in intro material.
						// Not much chance of finding it by reference (even if the chapter number
						// has been added, we never find the 0th verse, and 99% of the time that
						// chapter number would have been added to the same paragraph where it
						// was missing in the first place), so just try to find the text in the
						// paragraph, if it still exists.
						if (string.IsNullOrEmpty(citedText))
						{
							SelectRangeOfChars(iBook, para, note.BeginOffset, note.BeginOffset);
							return;
						}

						// The text may be null if the paragraph only contains the prompt. TE-8315
						if (para.Contents.Text != null)
						{
							int i = para.Contents.Text.IndexOf(citedText);
							if (i >= 0)
							{
								SelectRangeOfChars(iBook, para, i, i + citedText.Length);
								return;
							}
						}
					}
				}


				// A selection could not be made at the specified location. Attempt to go to
				// the specified verse and then try to find the text.

				// REVIEW (TimS): Why do we call GotoVerse here when we select the characters
				// down below? We might consider doing this only if we fail down below.
				if (sendSyncMsg)
					GotoVerse(scrRef);
				else
					GotoVerse_WithoutSynchMsg(scrRef);

				int iSection, iPara;
				if (citedText != null && FindTextInVerse(m_scr, citedTextTss, scrRef, false, out iSection,
					out iPara, out ichStart, out ichEnd))
				{
					// We found the text in the verse at a different character offset.
					SelectRangeOfChars(iBook, iSection, iPara, ichStart, ichEnd);
				}
				else if (note.BeginOffset > 0 && para != null &&
					IsOffsetValidLoc(para.Contents.Text, note.BeginOffset))
				{
					// We couldn't find the cited text at the specified offset, nor anywhere
					// in the paragraph. Therefore, just set the IP at the begin offset.
					SelectRangeOfChars(iBook, para, note.BeginOffset, note.BeginOffset);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if the specified text is in expected location in the paragraph.
		/// </summary>
		/// <param name="searchText">The text to search for in the paragraph.</param>
		/// <param name="paraText">The text in the paragraph.</param>
		/// <param name="beginOffset">expected beginning offset of searchText</param>
		/// <param name="endOffset">expected ending offset of searchText</param>
		/// <returns><c>true</c> if searchText found at specified offset, <c>false</c>
		/// otherwise</returns>
		/// ------------------------------------------------------------------------------------
		private bool TextAtExpectedLoc(string paraText, string searchText, int beginOffset,
			int endOffset)
		{
			return (paraText != null && paraText.Length >= endOffset &&
				paraText.Length > beginOffset && beginOffset < endOffset &&
				paraText.Substring(beginOffset, endOffset - beginOffset) == (searchText ?? string.Empty));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if the specified offset is less than or equal to the length of the
		/// specified text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool IsOffsetValidLoc(string paraText, int offset)
		{
			return (!string.IsNullOrEmpty(paraText) && offset <= paraText.Length);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Selects the given note's cited text in a picture caption.
		/// </summary>
		/// <param name="iBook">The index of the book in the filter (this is passed as a
		/// convenience -- it could be recalculated).</param>
		/// <param name="note">The note.</param>
		/// ------------------------------------------------------------------------------------
		private void SelectCitedTextInPictureCaption(int iBook, IScrScriptureNote note)
		{
			try
			{
				ICmPicture picture = (ICmPicture)note.BeginObjectRA;
				int iSection, iPara, ichOrcPos;
				ITsString citedText = note.CitedTextTss;
				if (!FindPictureInVerse(new ScrReference(note.BeginRef, m_scr.Versification),
					picture.Hvo, out iSection, out iPara, out ichOrcPos))
				{
					throw new Exception("Picture ORC not found in verse"); // See catch
				}
				int ichStart, ichEnd;
				ITsString sCaptionText = picture.Caption.VernacularDefaultWritingSystem;
				if (sCaptionText.Length >= note.EndOffset &&
					sCaptionText.Substring(note.BeginOffset, note.EndOffset - note.BeginOffset) == citedText)
				{
					ichStart = note.BeginOffset;
					ichEnd = note.EndOffset;
				}
				else
				{
					// Search for word form in caption
					if (!TsStringUtils.FindWordFormInString(citedText, sCaptionText,
						m_cache.WritingSystemFactory, out ichStart, out ichEnd))
					{
						ichStart = ichEnd = 0;
					}
				}
				MakeSelectionInPictureCaption(iBook, iSection,
					ScrSectionTags.kflidContent, iPara,
					ichOrcPos, ichStart, ichEnd);
			}
			catch
			{
				// Picture or caption no longer exists. Go to the start of the specified verse.
				GotoVerse(new ScrReference(note.BeginRef, m_scr.Versification));
				return;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempts to find the specified text in the given verse and return the character
		/// offsets (relative to the whole paragraph) to allow it to be selected.
		/// </summary>
		/// <param name="scr">The scripture.</param>
		/// <param name="tssTextToFind">The text to search for</param>
		/// <param name="scrRef">The reference of the verse in which to look</param>
		/// <param name="fMatchWholeWord">True to match to a whole word, false to just look
		/// for the specified text</param>
		/// <param name="iSection">Index of the section where the text was found.</param>
		/// <param name="iPara">Index of the para in the section contents.</param>
		/// <param name="ichStart">if found, the character offset from the start of the para to
		/// the start of the sought text</param>
		/// <param name="ichEnd">if found, the character offset from the start of the para to
		/// the end of the sought text</param>
		/// <returns>
		/// 	<c>true</c> if found; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static bool FindTextInVerse(IScripture scr, ITsString tssTextToFind, ScrReference scrRef,
			bool fMatchWholeWord, out int iSection, out int iPara, out int ichStart, out int ichEnd)
		{
			// Get verse text
			foreach (VerseTextSubstring verseText in GetVerseText(scr, scrRef))
			{
				// Search for wordform in verse text
				if (TsStringUtils.FindWordFormInString(tssTextToFind, verseText.Tss,
					scr.Cache.WritingSystemFactory, out ichStart, out ichEnd))
				{
					ichStart += verseText.StartOffset;
					ichEnd += verseText.StartOffset;
					iSection = verseText.SectionIndex;
					iPara = verseText.ParagraphIndex;
					return true;
				}
			}

			ichStart = ichEnd = iSection = iPara = -1;
			return false;
		}
		#endregion

		#region Goto Book methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the first Scripture book
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void GoToFirstBook()
		{
			CheckDisposed();
			BookIndex = 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the previous Scripture book
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void GoToPrevBook()
		{
			CheckDisposed();
			BookIndex--;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the next Scripture book
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void GoToNextBook()
		{
			CheckDisposed();
			BookIndex++;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the last Scripture book
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void GoToLastBook()
		{
			CheckDisposed();
			BookIndex = BookFilter.BookCount - 1;
		}

		#endregion

		#region Goto Section methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the first Scripture section in the current book
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void GoToFirstSection()
		{
			CheckDisposed();
			GoToSectionStart(BookIndex, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the previous Scripture section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void GoToPrevSection()
		{
			CheckDisposed();

			int iBook = GetBookIndex(SelectionHelper.SelLimitType.Top);
			int iSection = GetSectionIndex(SelectionHelper.SelLimitType.Top);
			if (iSection > 0)
				GoToSectionStart(iBook, iSection - 1);
			else
			{
				// If the insertion point is in a book title or we're in the first section
				// of a book then go to the last section of the preceeding book.
				if (iBook > 0)
				{
					iBook--;
					IScrBook book = BookFilter.GetBook(iBook);

					if (book.SectionsOS.Count > 0)
						GoToSectionStart(iBook, book.SectionsOS.Count - 1);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the next Scripture section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void GoToNextSection()
		{
			CheckDisposed();

			int iBook = GetBookIndex(SelectionHelper.SelLimitType.Bottom);
			IScrBook book = BookFilter.GetBook(iBook);
			int iSection = GetSectionIndex(SelectionHelper.SelLimitType.Bottom);
			if (iSection < book.SectionsOS.Count - 1)
				GoToSectionStart(iBook, iSection + 1);
			else if (iBook < BookFilter.BookCount - 1)
			{
				// If the insertion point is in the last section of a book then go to the
				// first section of the following book.
				iBook++;
				book = BookFilter.GetBook(iBook);

				if (book.SectionsOS.Count > 0)
					GoToSectionStart(iBook, 0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the last Scripture section in the current book
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void GoToLastSection()
		{
			CheckDisposed();

			int iBook = GetBookIndex(SelectionHelper.SelLimitType.Bottom);
			IScrBook book = BookFilter.GetBook(iBook);
			GoToSectionStart(iBook, book.SectionsOS.Count - 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the beginning of the specified Scripture section.
		/// </summary>
		/// <param name="iBook">0-based index of the book (in this view)</param>
		/// <param name="iSection">0-based index of the section (in the given book, as
		/// displayed in this view)</param>
		/// <remarks>In an unfiltered view using the default sort, these indexes will
		/// correspond to the order of the books and sections in the DB model</remarks>
		/// ------------------------------------------------------------------------------------
		protected void GoToSectionStart(int iBook, int iSection)
		{
			if (iBook >= 0 && iSection >= 0 && iBook < BookFilter.BookCount)
			{
				IScrBook book = BookFilter.GetBook(iBook);
				if (iSection < book.SectionsOS.Count)
				{
					IScrSection section = book[iSection];
					SetInsertionPoint(section.HeadingParagraphCount > 0 ?
						ScrSectionTags.kflidHeading :
						ScrSectionTags.kflidContent, iBook, iSection);
				}
			}
		}
		#endregion

		#region Goto Footnote methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Go to next footnote.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual IScrFootnote GoToNextFootnote()
		{
			CheckDisposed();

			SelectionHelper selHelper = GetSelectionReducedToIp(SelectionHelper.SelLimitType.Bottom);
			if (selHelper == null)
				return null;
			// Get the information needed from the current selection
			int paraLev = selHelper.GetLevelForTag(StTextTags.kflidParagraphs);
			SelLevInfo[] levels = selHelper.LevelInfo;
			int iBook = ((ITeView)Control).LocationTracker.GetBookIndex(
				selHelper, SelectionHelper.SelLimitType.Anchor);
			int tag = levels[paraLev + 1].tag;
			int iSection = ((ITeView)Control).LocationTracker.GetSectionIndexInBook(
				selHelper, SelectionHelper.SelLimitType.Anchor);
			int iPara = levels[paraLev].ihvo;
			int ich = selHelper.IchAnchor;

			// Look for the next footnote
			IScrFootnote footnote = BookFilter.GetBook(iBook).FindNextFootnote(
				ref iSection, ref iPara, ref ich, ref tag);

			// If we didn't find a footnote in the current book then go through the rest of the
			// books until we find one or we run out of books.
			while (footnote == null && iBook < BookFilter.BookCount - 1)
			{
				tag = ScrBookTags.kflidTitle;
				iBook++;
				iPara = iSection = ich = 0;
				footnote = BookFilter.GetBook(iBook).FindNextFootnote(ref iSection,
					ref iPara, ref ich, ref tag);
			}

			// Set the IP
			if (footnote != null)
				SetInsertionPoint(tag, iBook, iSection, iPara, ich, false);

			return footnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Go to previous footnote.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual IScrFootnote GoToPreviousFootnote()
		{
			CheckDisposed();

			if (CurrentSelection == null)
			{
				Debug.Fail("Selection required");
				return null;
			}
			// Get the information needed from the current selection
			int paraLev = CurrentSelection.GetLevelForTag(StTextTags.kflidParagraphs);
			SelLevInfo[] levels = CurrentSelection.LevelInfo;
			int iBook = ((ITeView)Control).LocationTracker.GetBookIndex(
				CurrentSelection, SelectionHelper.SelLimitType.Anchor);
			int tag = levels[paraLev + 1].tag;
			int iSection = ((ITeView)Control).LocationTracker.GetSectionIndexInBook(
				CurrentSelection, SelectionHelper.SelLimitType.Anchor);
			int iPara = levels[paraLev].ihvo;
			int ich = CurrentSelection.IchAnchor;

			// Look for the previous footnote
			IScrFootnote footnote = BookFilter.GetBook(iBook).FindPrevFootnote(
				ref iSection, ref iPara, ref ich, ref tag);

			// If we didn't find a footnote in the current book then go through the rest of the
			// books until we find one or we run out of books.
			while (footnote == null && iBook > 0)
			{
				tag = ScrSectionTags.kflidContent;
				iBook--;
				IScrBook book = BookFilter.GetBook(iBook);
				iSection = book.SectionsOS.Count - 1;
				iPara = -1;
				ich = -1;

				footnote = book.FindPrevFootnote(ref iSection, ref iPara, ref ich, ref tag);
			}

			// Set the IP
			if (footnote != null)
				SetInsertionPoint(tag, iBook, iSection, iPara, ich, true);

			return footnote;
		}
		#endregion

		#region Misc public methods
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the name of the current book as a string.
		/// </summary>
		/// <param name="selLimitType">Specify Top or Bottom</param>
		/// <returns>The name of the current book, or <c>string.Empty</c> if we don't have a
		/// selection or the selection is in a place we don't know how to handle.</returns>
		/// -----------------------------------------------------------------------------------
		public virtual string CurrentBook(SelectionHelper.SelLimitType selLimitType)
		{
			CheckDisposed();

			// if there is no selection then there can be no book
			if (CurrentSelection == null || m_cache == null)
				return string.Empty;

			int iBook = ((ITeView)Control).LocationTracker.GetBookIndex(
				CurrentSelection, selLimitType);

			if (iBook < 0)
				return string.Empty;

			IScrBook book = BookFilter.GetBook(iBook);
			string sBook = book.Name.UserDefaultWritingSystem.Text;
			if (sBook != null)
				return sBook;

			MultilingScrBooks multiScrBooks = new MultilingScrBooks((IScrProjMetaDataProvider)m_scr);
			return multiScrBooks.GetBookName(book.CanonicalNum);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Retrieves the flid and hvo corresponding to the current Scripture element (e.g.,
		/// section heading, section contents, or title) selected.
		/// </summary>
		/// <param name="tag">The flid of the selected owning element</param>
		/// <param name="hvoSel">The hvo of the selected owning element (hvo of either section
		/// or book)</param>
		/// <returns>True, if a known element is found at this current selection</returns>
		/// -----------------------------------------------------------------------------------
		public virtual bool GetSelectedScrElement(out int tag, out int hvoSel)
		{
			CheckDisposed();

			hvoSel = 0;
			tag = 0;
			if (CurrentSelection == null)
				return false;
			try
			{
				SelectionHelper helper = CurrentSelection;
				SelLevInfo selLevInfo;
				if (!helper.GetLevelInfoForTag(ScrBookTags.kflidTitle,
					 SelectionHelper.SelLimitType.Top, out selLevInfo))
				{
					if (!helper.GetLevelInfoForTag(ScrSectionTags.kflidHeading,
						 SelectionHelper.SelLimitType.Top, out selLevInfo))
					{
						if (!helper.GetLevelInfoForTag(ScrSectionTags.kflidContent,
							 SelectionHelper.SelLimitType.Top, out selLevInfo))
						{
							return false;
						}
					}
				}
				tag = selLevInfo.tag;
				if (selLevInfo.hvo > 0)		// Tests can't depend on throwing for bad HVO value.
					hvoSel = m_repoCmObject.GetObject(selLevInfo.hvo).Owner.Hvo;
			}
			catch
			{
				return false;
			}

			return true;
		}
		#endregion

		#region Paste-related methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle insertion of paragraphs (i.e., from clipboard) with properties that don't
		/// match the properties of the paragraph where they are being inserted. This gives us
		/// the opportunity to create/modify the DB structure to recieve the paragraphs being
		/// inserted and to reject certain types of paste operations (such as attempting to
		/// paste a book).
		/// </summary>
		/// <param name="rootBox">the sender</param>
		/// <param name="ttpDest">properties of destination paragraph</param>
		/// <param name="cPara">number of paragraphs to be inserted</param>
		/// <param name="ttpSrcArray">Array of props of each para to be inserted</param>
		/// <param name="tssParas">Array of TsStrings for each para to be inserted</param>
		/// <param name="tssTrailing">Text of an incomplete paragraph to insert at end (with
		/// the properties of the destination paragraph.</param>
		/// <returns>One of the following:
		/// kidprDefault - causes the base implementation to insert the material as part of the
		/// current StText in the usual way;
		/// kidprFail - indicates that we have decided that this text should not be pasted at
		/// this location at all, causing entire operation to roll back;
		/// kidprDone - indicates that we have handled the paste ourselves, inserting the data
		/// whereever it ought to go and creating any necessary new structure.</returns>
		/// ------------------------------------------------------------------------------------
		public VwInsertDiffParaResponse InsertDiffParas(IVwRootBox rootBox,
			ITsTextProps ttpDest, int cPara, ITsTextProps[] ttpSrcArray, ITsString[] tssParas,
			ITsString tssTrailing)
		{
			CheckDisposed();

			Debug.Assert(!IsBackTranslation);

			if (cPara != ttpSrcArray.Length || cPara != tssParas.Length || ttpDest == null ||
				IsBackTranslation)
				return VwInsertDiffParaResponse.kidprFail;

			// Get the context of the style we are inserting into
			string destStyleName =
				ttpDest.GetStrPropValue((int)FwTextStringProp.kstpNamedStyle);
			IStStyle destStyle = null;
			if (destStyleName != null)
				destStyle = m_scr.FindStyle(destStyleName);
			if (destStyle == null)
				return VwInsertDiffParaResponse.kidprFail;
			ContextValues destContext = destStyle.Context;
			StructureValues destStructure = destStyle.Structure;

			// If pasted data came from a non-FW app, all elements in the source props array
			// will be null. In this case, the default behavior will work fine.
			if (ttpSrcArray[0] == null)
			{
				int i;
				for (i = 1; i < ttpSrcArray.Length; i++)
				{
					if (ttpSrcArray[i] != null)
						break;
				}
				if (i >= ttpSrcArray.Length)
					return VwInsertDiffParaResponse.kidprDefault;
			}

			// Look through the source data (being inserted) to see if there is a conflict.
			ContextValues srcContext;
			StructureValues srcStructure;
			if (!GetSourceContextAndStructure(ttpSrcArray, out srcContext, out srcStructure))
				return VwInsertDiffParaResponse.kidprFail;

			// If context of the style is Title then throw out the entire insert.
			// REVIEW: Should this allow insertion of a title para within an existing title?
			if (srcContext == ContextValues.Title)
				return VwInsertDiffParaResponse.kidprFail;

			// Set a flag if the src context/structure is different from the dest context/structure
			bool foundMismatch = (srcContext != destContext) || (srcStructure != destStructure);

			if (!foundMismatch)
				// let the views handle it!
				return VwInsertDiffParaResponse.kidprDefault;

			bool noTrailingText =
				(tssTrailing == null || tssTrailing.Length == 0);

			// If insertion point is at beginning of paragraph and there is no trailing text
			if (CurrentSelection.IchAnchor == 0 && noTrailingText)
			{
				if (InBookTitle)
				{
					if (InsertParagraphsBeforeBook(ttpSrcArray, tssParas, srcContext))
						return VwInsertDiffParaResponse.kidprDone;
				}
				else if (InSectionHead)
				{
					if (SectionIndex > 0 && ParagraphIndex == 0 &&
						InsertParagraphsBeforeSection(ttpSrcArray, tssParas, srcContext))
						return VwInsertDiffParaResponse.kidprDone;
				}
				else if (srcContext == destContext && InsertParagraphsInSection(ttpSrcArray, tssParas))
					return VwInsertDiffParaResponse.kidprDone;
			}

			// The contexts don't match and we don't handle it so fail.
			return VwInsertDiffParaResponse.kidprFail;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine the context and structure of the paragraphs to be inserted
		/// </summary>
		/// <param name="ttpSrcArray">Array of props of each para to be inserted</param>
		/// <param name="srcContext">The context of the paragraphs to be inserted, if they
		/// are all the same; undefined if this method returns false.</param>
		/// <param name="srcStructure">The structure of the paragraphs to be inserted, if they
		/// are all the same; StructureValues.Undefined if there is a mix.</param>
		/// <returns>false if any of the paragraphs to be inserted have null properties OR if
		/// there is a mix of different contexts in the paragraphs</returns>
		/// ------------------------------------------------------------------------------------
		private bool GetSourceContextAndStructure(ITsTextProps[] ttpSrcArray,
			out ContextValues srcContext, out StructureValues srcStructure)
		{
			srcContext = ContextValues.General;
			srcStructure = StructureValues.Undefined;

			for (int i = 0; i < ttpSrcArray.Length; i++)
			{
				string srcStyleName = null;
				if (ttpSrcArray[i] != null)
					srcStyleName = ttpSrcArray[i].GetStrPropValue((int)FwTextStringProp.kstpNamedStyle);

				if (srcStyleName == null)
					continue;

				IStStyle srcStyle = m_scr.FindStyle(srcStyleName);

				if (srcStyle == null || srcStyle.Type != StyleType.kstParagraph || (i > 0 && srcContext != srcStyle.Context))
					return false;

				srcContext = srcStyle.Context;
				srcStructure = (i > 0 && srcStructure != srcStyle.Structure ?
					StructureValues.Undefined : srcStyle.Structure);
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="ttpSrcArray">Array of props of each para to be inserted</param>
		/// <param name="tssParas">Array of TsStrings for each para to be inserted</param>
		/// <param name="srcContext">Context of paragraphs to be inserted</param>
		/// <returns>False if we can't allow the insertion</returns>
		/// ------------------------------------------------------------------------------------
		private bool InsertParagraphsBeforeSection(ITsTextProps[] ttpSrcArray,
			ITsString[] tssParas, ContextValues srcContext)
		{
			// save indices, not sure if insertion point will be valid after paragraphs have
			// been added.
			int bookIndex = BookIndex;
			int sectionIndex = SectionIndex;
			IScrBook book = BookFilter.GetBook(bookIndex);
			IScrSection section = book.SectionsOS[sectionIndex - 1];

			if (section.Context != srcContext)
				return false;

			int cAddedSections;
			if (!InsertParagraphsAtSectionEnd(book, section, ttpSrcArray, tssParas,
				sectionIndex, out cAddedSections))
				return false;

			if (cAddedSections > 0)
				SetInsertionPoint(ScrSectionTags.kflidHeading, bookIndex, sectionIndex + cAddedSections);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts paragraphs into the content of the previous visible book.
		/// </summary>
		/// <param name="ttpSrcArray">Array of props of each para to be inserted</param>
		/// <param name="tssParas">Array of TsStrings for each para to be inserted</param>
		/// <param name="srcContext">Context of paragraphs to be inserted</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool InsertParagraphsBeforeBook(ITsTextProps[] ttpSrcArray,
			ITsString[] tssParas, ContextValues srcContext)
		{
			int prevBook = BookIndex - 1;
			if (prevBook < 0)
				return false;

			IScrBook book = BookFilter.GetBook(prevBook);
			IScrSection section = book.SectionsOS[book.SectionsOS.Count - 1];

			if (section.Context != srcContext)
				return false;

			int cAddedSections;
			return InsertParagraphsAtSectionEnd(book, section, ttpSrcArray, tssParas,
				book.SectionsOS.Count, out cAddedSections);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts one or more paragraphs at the end of a section.  New sections for the book
		/// may also be created.
		/// </summary>
		/// <param name="book"></param>
		/// <param name="section"></param>
		/// <param name="ttpSrcArray">Array of props of each para to be inserted</param>
		/// <param name="tssParas">Array of TsStrings for each para to be inserted</param>
		/// <param name="sectionIndex"></param>
		/// <param name="cAddedSections"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		private bool InsertParagraphsAtSectionEnd(IScrBook book, IScrSection section,
			ITsTextProps[] ttpSrcArray, ITsString[] tssParas, int sectionIndex, out int cAddedSections)
		{
			cAddedSections = 0;
			bool isIntro = false;

			for (int i = 0; i < ttpSrcArray.Length; i++)
			{
				string styleName = ttpSrcArray[i].GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
				IStStyle style = m_scr.FindStyle(styleName);
				if (style.Structure == StructureValues.Heading)
				{
					// If content has been added to section, create a new section.  Otherwise,
					// add the new paragraph to the end of the current section heading.
					if (section.ContentOA.ParagraphsOS.Count == 0)
					{
						// Create heading paragraph at end of section heading
						section.HeadingOA.InsertNewPara(-1, styleName, tssParas[i]);
					}
					else
					{
						isIntro = (style.Context == ContextValues.Intro);
						// Create a new section and add the current paragraph to the heading
						section = m_cache.ServiceLocator.GetInstance<IScrSectionFactory>().CreateEmptySection(
							book, sectionIndex + cAddedSections++);

						section.HeadingOA.InsertNewPara(-1, styleName, tssParas[i]);
					}
				}
				else
				{
					// Create content paragraph for the current section
					section.ContentOA.InsertNewPara(-1, styleName, tssParas[i]);
				}
			}

			// create an empty paragraph if section content is empty
			if (section.ContentOA.ParagraphsOS.Count == 0)
			{
				string styleName = isIntro ? ScrStyleNames.IntroParagraph : ScrStyleNames.NormalParagraph;
				StTxtParaBldr bldr = new StTxtParaBldr(m_cache);
				bldr.ParaStyleName = styleName;
				ITsTextProps charProps = StyleUtils.CharStyleTextProps(styleName,
					m_wsContainer.DefaultVernacularWritingSystem.Handle);
				bldr.AppendRun(string.Empty, charProps);
				bldr.CreateParagraph(section.ContentOA);
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts one or more paragraphs in the middle of a section.
		/// </summary>
		/// <param name="ttpSrcArray">Array of props of each para to be inserted</param>
		/// <param name="tssParas">Array of TsStrings for each para to be inserted</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool InsertParagraphsInSection(ITsTextProps[] ttpSrcArray, ITsString[] tssParas)
		{
			// save indices, not sure if insertion point will be valid after paragraphs have
			// been added.
			int bookIndex = BookIndex;
			int sectionIndex = SectionIndex;
			int paragraphIndex = ParagraphIndex;
			IScrBook book = BookFilter.GetBook(bookIndex);
			IScrSection section = book.SectionsOS[sectionIndex];
			int cAddedSections = 0;
			int cAddedParagraphs = 0;	// number of paragraphs added in current section

			for (int i = 0; i < ttpSrcArray.Length; i++)
			{
				string styleName = ttpSrcArray[i].GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
				IStStyle style = m_scr.FindStyle(styleName);
				if (style.Structure == StructureValues.Heading)
				{
					// If not at first paragraph of content, create a new section.  Otherwise
					// add the paragraph to the end of the section heading.
					if (paragraphIndex + cAddedParagraphs > 0)
					{
						cAddedSections++;
						IScrSection newSection = section.SplitSectionContent_atIP(
							paragraphIndex + cAddedParagraphs, tssParas[i], styleName);
						section = newSection;

						cAddedParagraphs = 0;
						paragraphIndex = 0;
					}
					else
					{
						// Create another heading paragraph for the current section
						section.HeadingOA.InsertNewPara(-1, styleName, tssParas[i]);
					}
				}
				else
				{
					// Create content paragraph for the current section
					section.ContentOA.InsertNewPara(paragraphIndex + cAddedParagraphs++,
						styleName, tssParas[i]);
				}
			}

			// Update Insertion point
			if (cAddedSections > 0 || cAddedParagraphs > 0)
			{
				SetInsertionPoint(bookIndex, sectionIndex + cAddedSections,
				paragraphIndex + cAddedParagraphs, 0, false);
			}

			return true;
		}
		#endregion

		#region Insert Section
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For the Insert Section menu, if IP at the end of a section, inserts a new section.
		/// Sets the IP in the new para of the new section.
		/// </summary>
		/// <param name="insertIntroSection">True to make the created section an intro section,
		/// false otherwise</param>
		/// <returns>The created section if one was created, otherwise null</returns>
		/// ------------------------------------------------------------------------------------
		public IScrSection CreateSection(bool insertIntroSection)
		{
			CheckDisposed();

			if (CurrentSelection == null)
				return null;

			if (CurrentSelection.Selection.IsRange)
			{
				SelectionHelper helper = GetSelectionReducedToIp(SelectionHelper.SelLimitType.Top);
				if (helper == null)
					return null;
				helper.SetSelection(false);
			}

			int iSection = SectionIndex; // current section; will also be updated for the selection in the new section
			int iBook = BookIndex;

			// Verify that IP is in valid location for new section
			Debug.Assert(iSection != -1 || InBookTitle);

			IScrBook book = BookFilter.GetBook(iBook);
			IScrSection section = null;
			if (iSection >= 0)
				section = book[iSection];
			int ichIP = CurrentSelection.IchAnchor;
			int iPara = CurrentSelection.LevelInfo[0].ihvo;
			IStTxtPara para;

			IScrSection newSection = null;
			if (InBookTitle)
			{
				iSection = 0;
				newSection = InsertSectionAtIndex(book, iSection, insertIntroSection);
			}
			else if (CurrentSelection.LevelInfo[1].tag == ScrSectionTags.kflidContent)
			{
				// if IP is in a content paragraph
				para = section.ContentOA[iPara];
				if (ichIP > para.Contents.Length)
					ichIP = para.Contents.Length;

				// Now insert the new section
				// if IP is at the end of the last paragraph of the section...
				if (iPara == section.ContentOA.ParagraphsOS.Count - 1 &&
					(para.Contents.Text == null ||
					ichIP == para.Contents.Length))
				{
					iSection++; // insert after the current section
					newSection = InsertSectionAtIndex(book, iSection, insertIntroSection);
				}
				else
				{
					// Need to create a new section and split the section content between
					// the new section and the existing section
					section = book[iSection];
					newSection = section.SplitSectionContent_atIP(iPara, ichIP);
					iSection++; // our selection needs to move to the new section
				}
			}
			else if (CurrentSelection.LevelInfo[1].tag ==
				ScrSectionTags.kflidHeading)
			{
				para = section.HeadingOA[iPara];
				if (ichIP > para.Contents.Length)
					ichIP = para.Contents.Length;

				if (iPara == 0 && ichIP == 0)
				{
					// Insert an empty section before the current section
					newSection = InsertSectionAtIndex(book, iSection, insertIntroSection);
				}
				else
				{
					// Need to create a new section and split the section heading between
					// the new section and the existing section
					section = book[iSection];
					newSection = section.SplitSectionHeading_atIP(iPara, ichIP);
				}
			}

			// Set the insertion point at the beg of the new section contents
			if (iBook > -1 && newSection != null)
			{
				//newSection = book[iSection];
				IStTxtPara firstPara = newSection.FirstContentParagraph;

				// if dividing an existing section, set the IP in the section heading;
				// otherwise, set the IP in the paragraph content.
				SelectionHelper selHelperIP = (firstPara != null && firstPara.Contents.Length > 0) ?
					SetInsertionPoint(ScrSectionTags.kflidHeading, iBook, iSection) :
					SetInsertionPoint(iBook, iSection, 0, 0, false);

				// Don't set the character props if the selection is a range
				// selection. This most likely is the case only if the new
				// selection is in a user prompt. The user prompt consists of
				// 2 runs which makes the below code fail because we only give
				// it enough props to replace 1 run.
				if (selHelperIP != null && selHelperIP.Selection != null &&
					!selHelperIP.Selection.IsRange)
				{
					//TODO: Only do this if we're creating a brand new section, not if
					// we're splitting one
					ITsTextProps[] rgttp = new ITsTextProps[] {
							StyleUtils.CharStyleTextProps(null, m_cache.DefaultVernWs) };
					selHelperIP.Selection.SetSelectionProps(1, rgttp);
				}
			}
			return newSection;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a new section at the given position. The new section will include an empty
		/// paragraph.
		/// </summary>
		/// <param name="book"></param>
		/// <param name="iSection"></param>
		/// <param name="isIntroSection"></param>
		/// ------------------------------------------------------------------------------------
		protected internal IScrSection InsertSectionAtIndex(IScrBook book, int iSection,
			bool isIntroSection)
		{
			if (m_cache == null)
				return null;

			return m_cache.ServiceLocator.GetInstance<IScrSectionFactory>().CreateSection(
				book, iSection, isIntroSection, true, true);
		}

		#endregion

		#region Insert Book
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert Scripture book in view.
		/// </summary>
		/// <param name="bookNum">Ordinal number of the book to insert (i.e. one-based book
		/// number).</param>
		/// <returns>The new book.</returns>
		/// ------------------------------------------------------------------------------------
		public IScrBook InsertBook(int bookNum)
		{
			CheckDisposed();

			if (m_cache == null)
				return null;

			IStText bookTitle;
			IScrBook newBook = m_cache.ServiceLocator.GetInstance<IScrBookFactory>().Create(
				bookNum, out bookTitle);

			// Insert the new book title and set the book names
			newBook.InitTitlePara();

			// Now insert the first section for the new book.
			ITsTextProps textProps = StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				m_cache.DefaultVernWs);
			m_cache.ServiceLocator.GetInstance<IScrSectionFactory>().CreateScrSection(
				newBook, 0, m_scr.ConvertToString(1), textProps, false);

			// Update the book filter
			IUndoAction filterAction = BookFilter.UpdateFilterAndCreateUndoAction(newBook);
			m_cache.ActionHandlerAccessor.AddAction(filterAction);

			// Set the insertion point at the end of the new section contents
			SetInsertionPoint(0, 0, 0, 1, false);

			return m_scr.FindBook(bookNum);
		}

		#endregion

		#region Problem Deletion
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a problem deletion - a complex selection crossing sections or other
		/// difficult cases such as BS/DEL at boundaries.
		/// </summary>
		/// <param name="sel"></param>
		/// <param name="dpt"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public VwDelProbResponse OnProblemDeletion(IVwSelection sel,
			VwDelProbType dpt)
		{
			CheckDisposed();

			// Problem deletions are not permitted in a back translation text.
			if (IsBackTranslation)
			{
				MiscUtils.ErrorBeep();
				return VwDelProbResponse.kdprAbort;
			}

			SelectionHelper helper = SelectionHelper.GetSelectionInfo(sel,
				Callbacks.EditedRootBox.Site);

			if (helper == null)
			{
				MiscUtils.ErrorBeep();
				throw new NotImplementedException();
			}

			// There should only be one scripture object as the root of the view.
			// If this changes, enhance this method to fail if they are not equal.
			Debug.Assert(helper.GetIhvoRoot(SelectionHelper.SelLimitType.Top) == 0);
			Debug.Assert(helper.GetIhvoRoot(SelectionHelper.SelLimitType.Bottom) == 0);

			// Handle a BS/DEL at the boundary of a paragraph
			if (dpt == VwDelProbType.kdptBsAtStartPara ||
				dpt == VwDelProbType.kdptDelAtEndPara)
			{
				using (new WaitCursor(Control))
				{
					if (HandleBsOrDelAtTextBoundary(helper, dpt))
						return VwDelProbResponse.kdprDone;
				}
			}

				// handle a complex selection range where the deletion will require restructuring
			// paragraphs or sections.
			else if (dpt == VwDelProbType.kdptComplexRange)
			{
				using (new WaitCursor(Control))
				{
					if (HandleComplexDeletion(helper))
						return VwDelProbResponse.kdprDone;
					else
					{
						// Don't want default behavior for complex deletions since it
						// only will delete a single paragraph and this is probably worse
						// than doing nothing.
						MiscUtils.ErrorBeep();
						return VwDelProbResponse.kdprAbort;
					}
				}
			}
			// If it's not a case we know how to deal with, treat as not implemented and accept
			// the default behavior.

			MiscUtils.ErrorBeep();
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Try to do something about an IP selection deletion that is at the start or end of an
		/// StText. If successful return true, otherwise false.
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="dpt"></param>
		/// <returns><c>true</c> if we successfully handled the deletion.</returns>
		/// ------------------------------------------------------------------------------------
		internal bool HandleBsOrDelAtTextBoundary(SelectionHelper helper, VwDelProbType dpt)
		{
			CheckDisposed();

			SelLevInfo[] levInfo = helper.GetLevelInfo(SelectionHelper.SelLimitType.Anchor);

			ILocationTracker tracker = ((ITeView)Control).LocationTracker;
			// bail out if we are not in a paragraph within a scripture section
			if (levInfo.Length !=
				tracker.GetLevelCount(ScrSectionTags.kflidContent) ||
				tracker.GetSectionIndexInView(helper, SelectionHelper.SelLimitType.Anchor) < 0 ||
				levInfo[0].tag != StTextTags.kflidParagraphs)
			{
				// Assume we are in a book title
				SelLevInfo dummyInfo;
				if (helper.GetLevelInfoForTag(ScrBookTags.kflidTitle, out dummyInfo))
					return MergeParasInTable(helper, dpt);
				return false;
			}

			// Level 1 will have tags showing which field of section is selected
			int iLevelSection = helper.GetLevelForTag(ScrSectionTags.kflidHeading);
			if (iLevelSection >= 0)
			{
				if (levInfo[0].ihvo == 0 && dpt == VwDelProbType.kdptBsAtStartPara)
				{
					// first paragraph of section head
					return HandleBackspaceAfterEmptyContentParagraph(helper);
				}
				else if (levInfo[0].ihvo == 0 && helper.IchAnchor == 0)
				{
					// Delete was pressed in an empty section head - try to combine with previous
					// return DeleteSectionHead(helper, false, false);
					if (dpt == VwDelProbType.kdptBsAtStartPara)
						return HandleBackspaceAfterEmptySectionHeadParagraph(helper);
					return HandleDeleteBeforeEmptySectionHeadParagraph(helper, true);
				}
				// NOTE: we check the vector size for the parent of the paragraph (levInfo[1].hvo)
				// but with our own tag (levInfo[0].tag)!
				else if (levInfo[0].ihvo == m_cache.DomainDataByFlid.get_VecSize(levInfo[iLevelSection].hvo,
					levInfo[0].tag) - 1
					&& dpt == VwDelProbType.kdptDelAtEndPara)
				{
					// last paragraph of section head
					return HandleDeleteBeforeEmptySectionContentParagraph(helper);
				}
				else
				{
					// other problem deletion: e.g. delete in BT side-by-side view. Because
					// we're displaying the paragraphs in a table with two columns, the views
					// code can't handle that. We have to merge the two paragraphs manually.
					return MergeParasInTable(helper, dpt);
				}
			}
			else if (helper.GetLevelForTag(ScrSectionTags.kflidContent) >= 0)
			{
				iLevelSection = helper.GetLevelForTag(ScrSectionTags.kflidContent);
				if (levInfo[0].ihvo == 0 && dpt == VwDelProbType.kdptBsAtStartPara)
				{
					// first paragraph of section
					return HandleBackspaceAfterEmptySectionHeadParagraph(helper);
				}
				else if (levInfo[0].ihvo == 0 && helper.IchAnchor == 0)
				{
					// Delete was pressed in an empty section content - try to combine with previous
					if (dpt == VwDelProbType.kdptBsAtStartPara)
						return HandleBackspaceAfterEmptyContentParagraph(helper);
					return HandleDeleteBeforeEmptySectionContentParagraph(helper);
				}
				// NOTE: we check the vector size for the parent of the paragraph (levInfo[1].hvo)
				// but with our own tag (levInfo[0].tag)!
				else if (levInfo[0].ihvo == m_cache.DomainDataByFlid.get_VecSize(levInfo[iLevelSection].hvo,
					levInfo[0].tag) - 1 && dpt == VwDelProbType.kdptDelAtEndPara)
				{
					// last paragraph of section
					return HandleDeleteBeforeEmptySectionHeadParagraph(helper, false);
				}
				else
				{
					// other problem deletion: e.g. delete in BT side-by-side view. Because
					// we're displaying the paragraphs in a table with two columns, the views
					// code can't handle that. We have to merge the two paragraphs manually.
					return MergeParasInTable(helper, dpt);
				}
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges the paras in table.
		/// </summary>
		/// <param name="helper">The helper.</param>
		/// <param name="dpt">The problem deletion type.</param>
		/// <returns><c>true</c> if we merged the paras, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		protected internal bool MergeParasInTable(SelectionHelper helper, VwDelProbType dpt)
		{
			SelLevInfo[] levInfo = helper.GetLevelInfo(SelectionHelper.SelLimitType.Top);
			if (levInfo[0].tag != StTextTags.kflidParagraphs)
				return false;

			ILocationTracker tracker = ((ITeView)Control).LocationTracker;
			IScrBook book = tracker.GetBook(helper, SelectionHelper.SelLimitType.Anchor);

			SelLevInfo tmpInfo;
			IStText text;
			if (helper.GetLevelInfoForTag(ScrBookTags.kflidTitle, out tmpInfo))
				text = book.TitleOA;
			else
			{
				IScrSection section = tracker.GetSection(
					helper, SelectionHelper.SelLimitType.Anchor);

				text = (levInfo[1].tag == ScrSectionTags.kflidHeading ?
					section.HeadingOA : text = section.ContentOA);
			}

			int iPara = helper.GetLevelInfoForTag(StTextTags.kflidParagraphs).ihvo;
			IStTxtPara currPara = text[iPara];
			ITsStrBldr bldr;

			// Backspace at beginning of paragraph
			if (dpt == VwDelProbType.kdptBsAtStartPara)
			{
				if (iPara <= 0)
				{
					MiscUtils.ErrorBeep();
					return false;
				}

				IStTxtPara prevPara = text[iPara - 1];
				int prevParaLen = prevPara.Contents.Length;

				prevPara.MergeParaWithNext();

				helper.SetIch(SelectionHelper.SelLimitType.Top, prevParaLen);
				helper.SetIch(SelectionHelper.SelLimitType.Bottom, prevParaLen);
				levInfo[0].ihvo = iPara - 1;
				helper.SetLevelInfo(SelectionHelper.SelLimitType.Top, levInfo);
				helper.SetLevelInfo(SelectionHelper.SelLimitType.Bottom, levInfo);
				if (DeferSelectionUntilEndOfUOW)
				{
					// We are within a unit of work, so setting the selection will not work now.
					// we request that a selection be made after the unit of work.
					Debug.Assert(!helper.IsRange,
						"Currently, a selection made during a unit of work can only be an insertion point.");
					helper.SetIPAfterUOW(EditedRootBox.Site);
				}
				else
				{
					helper.SetSelection(true);
				}
				return true;
			}
			// delete at end of a paragraph
			int cParas = text.ParagraphsOS.Count;
			if (iPara + 1 >= cParas)
				return false; // We don't handle merging across StTexts

			currPara.MergeParaWithNext();

			if (DeferSelectionUntilEndOfUOW)
			{
				// We are within a unit of work, so setting the selection will not work now.
				// we request that a selection be made after the unit of work.
				Debug.Assert(!helper.IsRange,
					"Currently, a selection made during a unit of work can only be an insertion point.");
				helper.SetIPAfterUOW(EditedRootBox.Site);
			}
			else
			{
				helper.SetSelection(true);
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles deletions where the selection is not contained within paragraphs of a
		/// single section.
		/// </summary>
		/// <param name="helper"></param>
		/// <returns><code>true</code> if deletion was handled</returns>
		/// ------------------------------------------------------------------------------------
		internal protected bool HandleComplexDeletion(SelectionHelper helper)
		{
			SelLevInfo[] topInfo = helper.GetLevelInfo(SelectionHelper.SelLimitType.Top);
			SelLevInfo[] bottomInfo = helper.GetLevelInfo(SelectionHelper.SelLimitType.Bottom);

			// Determine if whole section head was deleted
			if (IsSectionHeadDeletion(helper, topInfo, bottomInfo))
			{
				return DeleteSectionHead(helper, true, false);
			}
			else if (IsSectionDeletion(helper, topInfo, bottomInfo))
			{
				return DeleteSections(helper, topInfo, bottomInfo);
			}
			else if (IsMultiSectionContentSelection(helper, topInfo, bottomInfo) &&
				SectionsHaveSameContext(helper))
			{
				return DeleteMultiSectionContentRange(helper);
			}
			else if (IsBookTitleSelection(helper, topInfo, bottomInfo))
			{
				return DeleteBookTitle();
			}
			else if (IsBookSelection(helper, topInfo, bottomInfo))
			{
				// Don't use BookIndex, since it will use anchor which may be wrong book.
				RemoveBook(((ITeView)Control).LocationTracker.GetBookIndex(
					helper, SelectionHelper.SelLimitType.Top));
				return true;
			}
			// TODO: Add complex deletion when deleting a range selection that includes two
			// partial sections.
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a single section is wholly selected or the selection goes from the top of one
		/// section to the top of the following section, then the selected section may be
		/// deleted.
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="topInfo"></param>
		/// <param name="bottomInfo"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool IsSectionDeletion(SelectionHelper helper, SelLevInfo[] topInfo,
			SelLevInfo[] bottomInfo)
		{
			bool sectionDeletion = false;
			ILocationTracker tracker = ((ITeView)Control).LocationTracker;

			int clev = tracker.GetLevelCount(ScrSectionTags.kflidContent);
			int levelText = helper.GetLevelForTag(ScrSectionTags.kflidContent);
			if (levelText < 0)
				levelText = helper.GetLevelForTag(ScrSectionTags.kflidHeading);
			int levelPara = helper.GetLevelForTag(StTextTags.kflidParagraphs);
			// the selection must be the right number of levels deep.
			if (topInfo.Length == clev && bottomInfo.Length == clev && levelText >= 0 && levelPara >= 0)
			{
				// if the selection is in the same scripture and the same book...
				if (tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Top) ==
					tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Bottom) &&
					// not selecting something weird
					topInfo[levelText - 1].tag == StTextTags.kflidParagraphs &&
					bottomInfo[levelText - 1].tag == StTextTags.kflidParagraphs)
				{
					// Selection top is in one section and bottom is in a following one
					if (tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Top) <=
						tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Bottom) - 1 &&
						// if the selection begins and ends in section heading
						topInfo[levelText].tag == ScrSectionTags.kflidHeading &&
						bottomInfo[levelText].tag == ScrSectionTags.kflidHeading &&
						topInfo[levelText].ihvo == 0 &&
						bottomInfo[levelText].ihvo == 0 &&
						// and the selection begins and ends in the first paragraph of those headings
						topInfo[levelPara].ihvo == 0 &&
						bottomInfo[levelPara].ihvo == 0)
					{
						// Does selection go from the beginning of one heading to the beginning of another?
						sectionDeletion = (helper.IchAnchor == 0 && helper.IchEnd == 0);
					}
					// Selection starts at beginning of one section heading and ends
					// at end of content of another section.
					else if (tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Top) <=
						tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Bottom) &&
						// Selection top is in section heading and bottom is in content
						topInfo[levelText].tag == ScrSectionTags.kflidHeading &&
						bottomInfo[levelText].tag == ScrSectionTags.kflidContent &&
						topInfo[levelText].ihvo == 0 &&
						bottomInfo[levelText].ihvo == 0 &&
						// Top of selection is in first heading paragraph
						topInfo[levelPara].ihvo == 0)
					{
						int ichTop = helper.GetIch(SelectionHelper.SelLimitType.Top);
						sectionDeletion =
							ichTop == 0 && IsSelectionAtEndOfSection(helper, bottomInfo, clev);
					}
				}
			}
			return sectionDeletion;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Selections at end of section.
		/// </summary>
		/// <param name="helper">The helper.</param>
		/// <param name="bottomInfo">The bottom info.</param>
		/// <param name="clev">The clev.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool IsSelectionAtEndOfSection(SelectionHelper helper, SelLevInfo[] bottomInfo,
			int clev)
		{
			bool atEndOfSection = false;
			// Is the bottom of the selection in the last para of the section?
			IScrSection bottomSection = ((ITeView)Control).LocationTracker.GetSection(
				helper, SelectionHelper.SelLimitType.Bottom);

			IStText bottomContent = bottomSection.ContentOA;
			int levelPara = helper.GetLevelForTag(StTextTags.kflidParagraphs);
			if (bottomInfo[levelPara].ihvo == bottomContent.ParagraphsOS.Count - 1)
			{
				IStTxtPara lastPara = m_repoScrTxtPara.GetObject(bottomInfo[levelPara].hvo);
				ITsString lastParaContents = lastPara.Contents;
				int ichBottom = helper.GetIch(SelectionHelper.SelLimitType.Bottom);
				atEndOfSection = ichBottom == lastParaContents.Length;
			}
			return atEndOfSection;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine whether this is a selection that goes from the content of one section to
		/// the content of another.
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="topInfo"></param>
		/// <param name="bottomInfo"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool IsMultiSectionContentSelection(SelectionHelper helper, SelLevInfo[] topInfo,
			SelLevInfo[] bottomInfo)
		{
			bool multiSectionSelection = false;
			ILocationTracker tracker = ((ITeView)Control).LocationTracker;
			int sectionTag = ScrSectionTags.kflidContent;
			int clev = tracker.GetLevelCount(sectionTag);

			if (topInfo.Length == clev && bottomInfo.Length == clev)
			{
				int paraTag = StTextTags.kflidParagraphs;
				// if selection starts in content of section in one book and goes to content
				// of another section in the same book
				int sectionLevelTop = helper.GetLevelForTag(sectionTag, SelectionHelper.SelLimitType.Top);
				if (sectionLevelTop == -1)
					return false;

				if (tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Top) ==
					tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Bottom) &&
					tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Top) <
					tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Bottom) &&
					sectionLevelTop ==
					helper.GetLevelForTag(sectionTag, SelectionHelper.SelLimitType.Bottom) &&
					helper.GetLevelForTag(paraTag, SelectionHelper.SelLimitType.Top) ==
					helper.GetLevelForTag(paraTag, SelectionHelper.SelLimitType.Bottom))
				{
					multiSectionSelection = true;
				}
			}

			return multiSectionSelection;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if the selection starts at the beginning of the section head and ends at
		/// the beginning of the section content.
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="topInfo"></param>
		/// <param name="bottomInfo"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool IsSectionHeadDeletion(SelectionHelper helper, SelLevInfo[] topInfo,
			SelLevInfo[] bottomInfo)
		{
			int levelText = helper.GetLevelForTag(ScrSectionTags.kflidContent);
			if (levelText < 0)
				levelText = helper.GetLevelForTag(ScrSectionTags.kflidHeading);
			if (levelText < 0)
				return false;

			bool sectionHeadDeletion = false;
			int levelPara = helper.GetLevelForTag(StTextTags.kflidParagraphs);
			ILocationTracker tracker = ((ITeView)Control).LocationTracker;
			int clev = tracker.GetLevelCount(ScrSectionTags.kflidContent);

			if (topInfo.Length == clev && bottomInfo.Length == clev && levelPara >= 0)
			{
				if (tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Top) ==
					tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Bottom) &&
					tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Top) ==
					tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Bottom) &&
					topInfo[levelText].tag == ScrSectionTags.kflidHeading &&
					bottomInfo[levelText].tag == ScrSectionTags.kflidContent &&
					topInfo[levelText].ihvo == 0 &&
					bottomInfo[levelText].ihvo == 0 &&
					topInfo[levelPara].tag == StTextTags.kflidParagraphs &&
					bottomInfo[levelPara].tag == StTextTags.kflidParagraphs &&
					topInfo[levelPara].ihvo == 0 &&
					bottomInfo[levelPara].ihvo == 0)
				{
					sectionHeadDeletion = (helper.IchAnchor == 0 && helper.IchEnd == 0);
					if (sectionHeadDeletion)
					{
						IStTxtPara para = m_repoScrTxtPara.GetObject(bottomInfo[levelPara].hvo);
						sectionHeadDeletion = (para.Contents.Text != null);
					}
				}
			}

			return sectionHeadDeletion;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if the selection starts at the beginning of the book title and ends
		/// at the beginning of the first section heading of the book.
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="topInfo"></param>
		/// <param name="bottomInfo"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool IsBookTitleSelection(SelectionHelper helper, SelLevInfo[] topInfo,
			SelLevInfo[] bottomInfo)
		{
			ILocationTracker tracker = ((ITeView)Control).LocationTracker;

			if (topInfo.Length != tracker.GetLevelCount(ScrBookTags.kflidTitle) ||
				bottomInfo.Length != tracker.GetLevelCount(ScrSectionTags.kflidContent))
			{
				return false;
			}

			return (tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Top) ==
				tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Bottom) &&
				topInfo[1].tag == ScrBookTags.kflidTitle &&
				topInfo[1].ihvo == 0 &&
				topInfo[0].tag == StTextTags.kflidParagraphs &&
				topInfo[0].ihvo == 0 &&
				tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Bottom) == 0 &&
				bottomInfo[1].tag == ScrSectionTags.kflidHeading &&
				bottomInfo[1].ihvo == 0 &&
				bottomInfo[0].tag == StTextTags.kflidParagraphs &&
				bottomInfo[0].ihvo == 0 &&
				helper.IchAnchor == 0 &&
				helper.IchEnd == 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if the selection is an entire book
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="topInfo"></param>
		/// <param name="bottomInfo"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool IsBookSelection(SelectionHelper helper, SelLevInfo[] topInfo,
			SelLevInfo[] bottomInfo)
		{
			ILocationTracker tracker = ((ITeView)Control).LocationTracker;
			int bookTitleLevelCount = tracker.GetLevelCount(ScrBookTags.kflidTitle);
			if (topInfo.Length == bookTitleLevelCount && bottomInfo.Length == bookTitleLevelCount)
			{
				// Verify that selection goes from beginning of book title to
				// beginning of title in next book.
				return (tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Top) ==
					tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Bottom) - 1 &&
					topInfo[1].tag == ScrBookTags.kflidTitle &&
					topInfo[1].ihvo == 0 &&
					topInfo[0].tag == StTextTags.kflidParagraphs &&
					topInfo[0].ihvo == 0 &&
					bottomInfo[1].tag == ScrBookTags.kflidTitle &&
					bottomInfo[1].ihvo == 0 &&
					bottomInfo[0].tag == StTextTags.kflidParagraphs &&
					bottomInfo[0].ihvo == 0 &&
					helper.IchAnchor == 0 &&
					helper.IchEnd == 0);
			}
			else if (topInfo.Length == bookTitleLevelCount && bottomInfo.Length ==
				tracker.GetLevelCount(ScrSectionTags.kflidContent))
			{
				// Check that selection is at start of title
				bool bookSel = (tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Top) ==
					tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Bottom) &&
					topInfo[1].tag == ScrBookTags.kflidTitle &&
					topInfo[1].ihvo == 0 &&
					topInfo[0].tag == StTextTags.kflidParagraphs &&
					topInfo[0].ihvo == 0 &&
					helper.GetIch(SelectionHelper.SelLimitType.Top) == 0);

				if (bookSel)
				{
					// Get information about last paragraph of the book
					IScrBook book = tracker.GetBook(helper, SelectionHelper.SelLimitType.Top);
					int iSection = book.SectionsOS.Count - 1;
					IScrSection section = book[iSection];
					int iPara = section.ContentOA.ParagraphsOS.Count - 1;
					IStTxtPara para = section.ContentOA[iPara];
					int ichEnd = para.Contents.Length;

					// Check that selection is at end of last paragraph of book
					bookSel = (tracker.GetSectionIndexInBook(helper,
						SelectionHelper.SelLimitType.Bottom) == iSection &&
						bottomInfo[1].tag == ScrSectionTags.kflidContent &&
						bottomInfo[0].ihvo == iPara &&
						bottomInfo[0].tag == StTextTags.kflidParagraphs &&
						helper.GetIch(SelectionHelper.SelLimitType.Bottom) == ichEnd);
				}

				return bookSel;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete all footnotes between firstFootnoteToDelete and lastFootnoteToDelete.
		/// </summary>
		/// <remarks>Use this version of DeleteFootnotes() when there is no need to clean up
		/// ORCs in corresponding BTs, such as when the deletion being performed is deleting
		/// only entire paragraphs.</remarks>
		/// <param name="book">Book that contains the sections to delete</param>
		/// <param name="firstFootnoteToDelete">First footnote that will be deleted
		/// (might be null).</param>
		/// <param name="lastFootnoteToDelete">Last footnote that will be deleted
		/// (might be null).</param>
		/// ------------------------------------------------------------------------------------
		private void DeleteFootnotes(IScrBook book, IScrFootnote firstFootnoteToDelete,
			IScrFootnote lastFootnoteToDelete)
		{
			DeleteFootnotes(book, firstFootnoteToDelete, lastFootnoteToDelete, null, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete all footnotes between firstFootnoteToDelete and lastFootnoteToDelete. Clean
		/// up BT ORCs for the paragraphs specified.
		/// </summary>
		/// <param name="book">Book that contains the sections to delete</param>
		/// <param name="firstFootnoteToDelete">First footnote that will be deleted
		/// (might be null).</param>
		/// <param name="lastFootnoteToDelete">Last footnote that will be deleted
		/// (might be null).</param>
		/// <param name="firstPara">First (possibly partial) paragraph being deleted that
		/// might contain ORCs for the first footnotes. Any footnotes being deleted whose ORCs
		/// are in this paragraph will have their corresponding BT ORCs removed as well.
		/// If zero, we don't bother to remove any corresponding BT ORCs in the first para.</param>
		/// <param name="lastPara">Last (possibly partial) paragraph being deleted that
		/// might contain ORCs for the last footnotes. Any footnotes being deleted whose ORCs
		/// are in this paragraph will have their corresponding BT ORCs removed as well.
		/// If zero, we don't bother to remove any corresponding BT ORCs in the last para.</param>
		/// ------------------------------------------------------------------------------------
		private void DeleteFootnotes(IScrBook book, IScrFootnote firstFootnoteToDelete,
			IScrFootnote lastFootnoteToDelete, IStTxtPara firstPara, IStTxtPara lastPara)
		{
			Debug.Assert((firstFootnoteToDelete == null && lastFootnoteToDelete == null)
				|| (firstFootnoteToDelete != null && lastFootnoteToDelete != null));

			if (firstFootnoteToDelete != null)
			{
				int lastFootnote = lastFootnoteToDelete.IndexInOwner;
				int firstFootnote = firstFootnoteToDelete.IndexInOwner;
				for (int i = lastFootnote; i >= firstFootnote; i--)
				{
					// This code fixes TE-4882.
					IScrFootnote footnote = book.FootnotesOS[i];
					book.FootnotesOS.Remove(footnote);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete the sections that are entirely spanned by the given selection. Selection is
		/// assumed to start a heading of one section and either end at the end of the content
		/// of a section or end at the beginning of another section heading. All selected
		/// sections will be deleted.
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="topInfo"></param>
		/// <param name="bottomInfo"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool DeleteSections(SelectionHelper helper, SelLevInfo[] topInfo,
			SelLevInfo[] bottomInfo)
		{
			ILocationTracker tracker = ((ITeView)Control).LocationTracker;

			// Verify some of the assumptions
			int sectionLevelCount = tracker.GetLevelCount(ScrSectionTags.kflidContent);
			Debug.Assert(topInfo.Length == sectionLevelCount);
			Debug.Assert(bottomInfo.Length == sectionLevelCount);
			Debug.Assert(topInfo[0].tag == StTextTags.kflidParagraphs &&
				bottomInfo[0].tag == StTextTags.kflidParagraphs);
			Debug.Assert(tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Top) ==
				tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Bottom));

			int ihvoBook = tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Top);
			IScrBook book = BookFilter.GetBook(ihvoBook);

			int ihvoSectionStart = tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Top);
			int ihvoSectionEnd = tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Bottom);
			// if selection ends in heading, delete stops at end of previous section.
			if (bottomInfo[1].tag == ScrSectionTags.kflidHeading)
				ihvoSectionEnd--;
			int cDeletedSections = ihvoSectionEnd - ihvoSectionStart + 1;

			// Delete footnotes that are in the sections to delete
			IFdoOwningSequence<IScrSection> sections = book.SectionsOS;
			IScrFootnote firstFootnoteToDelete = book.FindFirstFootnoteInSectionRange(
				ihvoSectionStart, ihvoSectionEnd);
			IScrFootnote lastFootnoteToDelete = firstFootnoteToDelete != null ?
				book.FindLastFootnoteInSectionRange(ihvoSectionStart, ihvoSectionEnd) :
				null;
			DeleteFootnotes(book, firstFootnoteToDelete, lastFootnoteToDelete);

			// If all sections in the book are being deleted, then delete all except one
			// section and then delete the header and contents of the last remaining
			// section. This will leave a skeleton structure of a single section in place.
			if (sections.Count == cDeletedSections)
			{
				for (int i = 1; i < cDeletedSections; i++)
					sections.RemoveAt(0);

				IScrSection section = sections[0];

				IFdoOwningSequence<IStPara> paras = section.HeadingOA.ParagraphsOS;
				while (paras.Count > 1)
					paras.RemoveAt(paras.Count - 1);

				((IStTxtPara)paras[0]).Contents =
					m_cache.TsStrFactory.MakeString(string.Empty, m_cache.DefaultVernWs);

				paras = section.ContentOA.ParagraphsOS;
				while (paras.Count > 1)
					paras.RemoveAt(paras.Count - 1);

				((IStTxtPara)paras[0]).Contents =
					m_cache.TsStrFactory.MakeString(string.Empty, m_cache.DefaultVernWs);
			}
			else
			{
				for (int i = ihvoSectionEnd; i >= ihvoSectionStart; i--)
					sections.RemoveAt(i);
			}

			if (ihvoSectionStart < sections.Count)
				SetInsertionPoint(ScrSectionTags.kflidHeading, ihvoBook,
					ihvoSectionStart);
			else
			{
				// Need to set position at end of new last section when last section has been
				// deleted
				IScrSection lastSection = sections[sections.Count - 1];
				IFdoOwningSequence<IStPara> lastSectionParas = lastSection.ContentOA.ParagraphsOS;
				IStTxtPara lastParaInSection =
					(IStTxtPara)lastSectionParas[lastSectionParas.Count - 1];
				int lastPosition = lastParaInSection.Contents.Length;
				SetInsertionPoint(ihvoBook, sections.Count - 1, lastSectionParas.Count - 1,
					lastPosition, helper.AssocPrev);
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes the title of the currently selected book and makes the empty paragraph
		/// have "Main Title" style.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool DeleteBookTitle()
		{
			int iBook = BookIndex;
			IScrBook book = BookFilter.GetBook(iBook);

			// remove all except first paragraph of book title
			for (int i = book.TitleOA.ParagraphsOS.Count - 1; i > 0; i--)
				book.TitleOA.ParagraphsOS.RemoveAt(i);

			// Clear contents of remaining paragraph
			IStTxtPara para = book.TitleOA[0];
			para.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.MainBookTitle);
			ITsTextProps ttp = para.Contents.get_Properties(0);
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			// Set the writing system to the previously used writing system, if valid.
			// Otherwise, use the default vernacular writing system.
			int dummy;
			int titleWs = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out dummy);
			para.Contents = tsf.MakeString(string.Empty,
				titleWs > 0 ? titleWs : RootVcDefaultWritingSystem);

			// Set selection into title
			SetInsertionPoint(ScrBookTags.kflidTitle, iBook, 0);

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete the given selection, removing any spanned sections. Selection is assumed to
		/// start in content of one section and end in content of another section.
		/// </summary>
		/// <param name="helper"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool DeleteMultiSectionContentRange(SelectionHelper helper)
		{
			if (helper.GetLevelInfo(SelectionHelper.SelLimitType.Top)[0].tag !=
				StTextTags.kflidParagraphs)
			{
				// Something changed catastrophically. StText has something in it other than paragraphs!
				Debug.Assert(false);
				return false;
			}

			ILocationTracker tracker = ((ITeView)Control).LocationTracker;
			int iBook = tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Top);
			int iSectionStart = tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Top);
			int iSectionEnd = tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Bottom);
			if (iBook < 0 || iSectionStart < 0 || iSectionEnd < 0)
			{
				// Something changed catastrophically. Maybe we added introductory material?
				Debug.Assert(false);
				return false;
			}

			int iParaStart = helper.GetLevelInfo(SelectionHelper.SelLimitType.Top)[0].ihvo;
			int iParaEnd = helper.GetLevelInfo(SelectionHelper.SelLimitType.Bottom)[0].ihvo;
			int ichStart = helper.GetIch(SelectionHelper.SelLimitType.Top);
			int ichEnd = helper.GetIch(SelectionHelper.SelLimitType.Bottom);

			// Do the deletion
			IScrBook book = BookFilter.GetBook(iBook);
			book.DeleteMultiSectionContentRange(iSectionStart, iSectionEnd, iParaStart,
				iParaEnd, ichStart, ichEnd);

			// Set the insertion point.
			SetInsertionPoint(iBook, iSectionStart, iParaStart, ichStart,
				helper.AssocPrev);

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles deletion of empty section heading paragraph on delete key being pressed at
		/// text boundary.
		/// </summary>
		/// <param name="helper">The helper.</param>
		/// <param name="fRestoreSelectionIfParaDeleted">True if the selection should be reset
		/// if the first section head paragraph is deleted, false if the selection should remain
		/// unchanged.</param>
		/// ------------------------------------------------------------------------------------
		private bool HandleDeleteBeforeEmptySectionHeadParagraph(SelectionHelper helper,
			bool fRestoreSelectionIfParaDeleted)
		{
			IScrBook book = ((ITeView)Control).LocationTracker.GetBook(
				helper, SelectionHelper.SelLimitType.Top);

			// Delete problem deletion will be at end of last paragraph of content,
			// need to check following section head (if available)

			// Get next section of book.
			int iSection = ((ITeView)Control).LocationTracker.GetSectionIndexInBook(
				helper, SelectionHelper.SelLimitType.Top);

			bool positionAtEnd = false;
			if (!InSectionHead)
			{
				iSection++;
				if (iSection > book.SectionsOS.Count - 1)
					return false;
				positionAtEnd = true;
			}
			else if (iSection == 0)
				return false;

			IScrSection section = book[iSection];

			if (section.HeadingOA == null || section.HeadingOA.ParagraphsOS.Count == 0)
			{
				// don't crash if database is corrupt - allow user to merge the two
				// sections (TE-4869)
				return MergeWithPreviousSectionIfInSameContext(helper, book,
					section, positionAtEnd);
			}
			else
			{
				// if there are more than one paragraph in heading, check to see if first
				// paragraph can be deleted.
				if (section.HeadingOA.ParagraphsOS.Count > 1)
				{
					if (section.HeadingOA[0].Contents.Length == 0)
					{
						section.HeadingOA.ParagraphsOS.RemoveAt(0);
						if (fRestoreSelectionIfParaDeleted)
						{
							// Paragraph where the selection was located was removed so we need
							// to reset the selection to its same location.
							helper.SetIPAfterUOW();
						}
						return true;
					}
				}
				else if (section.HeadingOA[0].Contents.Length == 0)
				{
					// If we are at end of content before an empty section head,
					// we should merge sections.
					return MergeWithPreviousSectionIfInSameContext(helper, book,
						section, positionAtEnd);
				}
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles deletion of empty section heading paragraph on backspace key being pressed
		/// at text boundary.
		/// </summary>
		/// <param name="helper">The selection helper.</param>
		/// <returns><c>true</c> if we handled the deletion, otherwise <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		private bool HandleBackspaceAfterEmptySectionHeadParagraph(SelectionHelper helper)
		{
			ILocationTracker tracker = ((ITeView)Control).LocationTracker;
			IScrBook book = tracker.GetBook(helper, SelectionHelper.SelLimitType.Top);
			IScrSection section = tracker.GetSection(helper, SelectionHelper.SelLimitType.Top);
			int iSection = tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Top);
			int cParas = section.HeadingOA != null ? section.HeadingOA.ParagraphsOS.Count : 0;
			if (cParas > 0)
			{
				// If we are the beginning of content before a multi-paragraph heading
				// and the last paragraph is empty, we delete the last paragraph of
				// the heading.
				if (cParas > 1)
				{
					IStTxtPara para = section.HeadingOA[cParas - 1];
					if (para.Contents.Length == 0)
					{
						section.HeadingOA.ParagraphsOS.RemoveAt(cParas - 1);
						return true;
					}
				}
				else if (iSection > 0)
				{
					// At this point, we know we're at beginning of content before an empty
					// section head and not in the first section, we should merge sections.
					IStTxtPara para = section.HeadingOA[cParas - 1];
					if (para.Contents.Length == 0)
					{
						return MergeWithPreviousSectionIfInSameContext(
							helper, book, section, false);
					}
				}

				return false;	// heading is not empty
			}

			// don't crash if database is corrupt - allow user to merge with
			// previous section (TE-4869)
			if (iSection > 0)
			{
				return MergeWithPreviousSectionIfInSameContext(
					helper, book, section, false);
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges the content of the current section with the previous section if in the
		/// sections have the same context.
		/// </summary>
		/// <param name="helper">The selection helper.</param>
		/// <param name="book">The current book.</param>
		/// <param name="section">The current section.</param>
		/// <param name="fPositionAtEnd">If true position of Selection is placed at end of
		/// paragraph, else at the beginning.</param>
		/// <returns><c>true</c> if we merged the sections, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		private bool MergeWithPreviousSectionIfInSameContext(SelectionHelper helper, IScrBook book,
			IScrSection section, bool fPositionAtEnd)
		{
			IScrSection prevSection = section.PreviousSection;
			if (SectionsHaveSameContext(prevSection, section))
			{
				MergeContentWithPreviousSection(helper, book, section, fPositionAtEnd);
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles deletion of empty section content paragraph on delete key being pressed at
		/// text boundary.
		/// </summary>
		/// <param name="helper">The selection helper.</param>
		/// <returns><c>true</c> if we merged the sections, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		private bool HandleDeleteBeforeEmptySectionContentParagraph(SelectionHelper helper)
		{
			// delete problem deletion will occur at end of section heading
			ILocationTracker tracker = ((ITeView)Control).LocationTracker;
			IScrBook book = tracker.GetBook(helper, SelectionHelper.SelLimitType.Top);
			int iSection = tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Top);
			IScrSection section = book[iSection];
			int cParas = section.ContentOA != null ? section.ContentOA.ParagraphsOS.Count : 0;

			if (cParas == 0)
			{
				return MergeWithFollowingSectionIfInSameContext(
					helper, book, iSection, section, true);
			}

			// If we are the end of heading before a multi-paragraph content
			// and the first paragraph is empty, we delete the first paragraph of
			// the content.
			if (cParas > 1)
			{
				if (section.ContentOA[0].Contents.Length == 0)
				{
					section.ContentOA.ParagraphsOS.RemoveAt(0);
					helper.SetIPAfterUOW();
					return true;
				}
			}
			// If we are at end of section heading or beginning of section content
			// and not in the last section, we should merge sections.
			else
			{
				if (iSection < book.SectionsOS.Count - 1 &&
					section.ContentOA[0].Contents.Length == 0)
				{
					return MergeWithFollowingSectionIfInSameContext(
						helper, book, iSection, section, true);
				}
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges the section the with following section if they are in the same context.
		/// </summary>
		/// <param name="helper">The selection helper.</param>
		/// <param name="book">The book.</param>
		/// <param name="iSection">The index of the current section.</param>
		/// <param name="section">The current section.</param>
		/// <param name="fDeleteForward">if set to <c>true</c> the user pressed the delete key,
		/// otherwise the backspace key.</param>
		/// <returns><c>true</c> if we merged the sections, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		private bool MergeWithFollowingSectionIfInSameContext(SelectionHelper helper,
			IScrBook book, int iSection, IScrSection section, bool fDeleteForward)
		{
			IScrSection nextSection = section.NextSection;

			// Merge the sections if they have the same context.
			if (!SectionsHaveSameContext(nextSection, section))
				return false;

			Debug.Assert(helper.Selection == CurrentSelection.Selection);
			bool wasInHeading = InSectionHead;
			int lastParaIndex = section.HeadingOA.ParagraphsOS.Count - 1;
			MergeHeadingWithFollowingSection(helper, book, section);

			// Now we have to re-establish a selection
			if (wasInHeading && fDeleteForward)
			{
				// delete key was pressed at end of section head, place IP at
				// end of what was the original heading
				section = book[iSection];
				IStTxtPara lastPara = section.HeadingOA[lastParaIndex];
				SetInsertionPoint(ScrSectionTags.kflidHeading,
					BookFilter.GetBookIndex(book), iSection, lastParaIndex, lastPara.Contents.Length, true);
			}
			else
			{
				// delete key was pressed in empty section content, place IP
				// at beginning of what was following section heading
				SetInsertionPoint(ScrSectionTags.kflidHeading,
					BookFilter.GetBookIndex(book), iSection, lastParaIndex + 1);
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles deletion of empty section content paragraph on backspace key being pressed
		/// at text boundary.
		/// </summary>
		/// <param name="helper">The selection helper.</param>
		/// <returns><c>true</c> if we merged the sections, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		private bool HandleBackspaceAfterEmptyContentParagraph(SelectionHelper helper)
		{
			ILocationTracker tracker = ((ITeView)Control).LocationTracker;
			IScrBook book = tracker.GetBook(helper, SelectionHelper.SelLimitType.Top);
			int iSection = tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Top);
			if (iSection == 0)
				return false;

			IScrSection prevSection = book[iSection - 1];
			int cParas = prevSection.ContentOA != null ? prevSection.ContentOA.ParagraphsOS.Count : 0;
			if (cParas == 0)
			{
				return MergeWithFollowingSectionIfInSameContext(
					helper, book, iSection - 1, prevSection, false);
			}

			// If we are the beginning of heading before a multi-paragraph content
			// and the last paragraph is empty, we delete the last paragraph of
			// the content.
			if (cParas > 1)
			{
				if (prevSection.ContentOA[cParas - 1].Contents.Length == 0)
				{
					prevSection.ContentOA.ParagraphsOS.RemoveAt(cParas - 1);
					return true;
				}
			}
			// If we are at beginning of heading before an empty section content and
			// not in the first section, we should merge sections.
			else if (prevSection.ContentOA[cParas - 1].Contents.Length == 0)
			{
				return MergeWithFollowingSectionIfInSameContext(
					helper, book, iSection - 1, prevSection, false);
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes a section heading.
		/// </summary>
		/// <param name="helper">Selection information</param>
		/// <param name="allowHeadingText">if <code>true</code> section head will be deleted
		/// even if heading contains text</param>
		/// <param name="positionAtEnd">if <code>true</code> IP will be at end of paragraph
		/// before top point of current selection</param>
		/// <returns><code>true</code> if deletion was done</returns>
		/// ------------------------------------------------------------------------------------
		private bool DeleteSectionHead(SelectionHelper helper, bool allowHeadingText,
			bool positionAtEnd)
		{
			ILocationTracker tracker = ((ITeView)Control).LocationTracker;
			if (helper.GetNumberOfLevels(SelectionHelper.SelLimitType.Top) !=
				tracker.GetLevelCount(ScrSectionTags.kflidContent))
				return false;

			int tag = helper.GetTextPropId(SelectionHelper.SelLimitType.Top);
			if (tag != StTxtParaTags.kflidContents && tag != SimpleRootSite.kTagUserPrompt)
			{
				// Currently these are the only possible leaf properties;
				// if this changes somehow, we'll probably need to enhance this code.
				Debug.Fail("Unexpected property in Scripture. tag == " + tag);
				return false;
			}

			IScrBook book = tracker.GetBook(helper, SelectionHelper.SelLimitType.Top);
			if (book == null)
			{
				Debug.Fail("No current book");
				return false;
			}

			IScrSection section = tracker.GetSection(helper, SelectionHelper.SelLimitType.Top);
			if (section == null)
			{
				Debug.Fail("Something changed catastrophically. Book has something in it other than sections!");
				return false;
			}

			if (helper.GetLevelInfo(SelectionHelper.SelLimitType.Top)[0].tag !=
				StTextTags.kflidParagraphs)
			{
				Debug.Fail("Something changed catastrophically. StText has something in it other than paragraphs!");
				return false;
			}

			// For now we just handle the heading.
			if (helper.GetLevelInfo(SelectionHelper.SelLimitType.Top)[1].tag !=
				ScrSectionTags.kflidHeading)
			{
				// Add code here if desired to handle bsp/del at boundary of body.
				return false;
			}

			// OK, we're dealing with a change at the boundary of a section heading
			// (in a paragraph of an StText that is the heading of an ScrSection in an ScrBook).
			IStText text = section.HeadingOA;
			int iPara = helper.GetLevelInfo(SelectionHelper.SelLimitType.Top)[0].ihvo;
			IStTxtPara para = text[iPara];

			if (!allowHeadingText && para.Contents.Length > 0)
			{
				// The current heading paragraph has something in it!
				// For now we won't try to handle this.
				// (The problem is knowing what to do with the undeleted header text...
				// make it part of the previous section? The previous header? Delete it?)
				return false;
			}
			if (text.ParagraphsOS.Count != 1)
			{
				// Backspace at start or delete at end of non-empty section, and the paragraph is
				// empty. Do nothing.
				return false;

				// Other options:
				// - delete the section? But what do we do with the rest of its heading?
				// - delete the empty paragraph? That's easy...just
				//		text.ParagraphsOS.RemoveAt(ihvoPara);
				// But where do we put the IP afterwards? At the end of the previous body,
				// for bsp, or the start of our own body, for del? Or keep it in the heading?
			}
			// OK, we're in a completely empty section heading.
			// If it's the very first section of the book, we can't join it to the previous
			// section, so do nothing. (May eventually enhance to join the two books...
			// perhaps after asking for confirmation!)
			if (section.IndexInOwner == 0)
				return false;

			// Finally...we know we're going to merge the two sections.
			MergeContentWithPreviousSection(helper, book, section, positionAtEnd);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges content of given section into the content of the previous section and then
		/// deletes the given section.
		/// </summary>
		/// <param name="helper"> </param>
		/// <param name="book"></param>
		/// <param name="section"></param>
		/// <param name="fPositionAtEnd">If true position of Selection is placed at end of
		/// paragraph, else at the beginning.</param>
		/// ------------------------------------------------------------------------------------
		private void MergeContentWithPreviousSection(SelectionHelper helper, IScrBook book,
			IScrSection section, bool fPositionAtEnd)
		{
			//REVIEW: Can the methods that call this be refactored
			//to use (a refactored?) ScrSection.MergeWithPreviousSection?
			//
			// Get the previous section and move the paragraphs.
			IScrSection sectionPrev = section.PreviousSection;
			IStText textPrev = sectionPrev.ContentOA;
			ILocationTracker tracker = ((ITeView)Control).LocationTracker;
			int iBook = tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Top);
			int cparaPrev = 0;
			if (textPrev == null)
			{
				// Prevent crash when dealing with corrupt database (TE-4869)
				// Since the previous section doesn't have a text, we simply move the entire text
				// object from the current section to the previous section.
				sectionPrev.ContentOA = section.ContentOA;
			}
			else
			{
				cparaPrev = textPrev.ParagraphsOS.Count;
				IStText textOldContents = section.ContentOA;
				textOldContents.ParagraphsOS.MoveTo(0, textOldContents.ParagraphsOS.Count - 1,
					textPrev.ParagraphsOS, cparaPrev);
			}

			// protected for some reason...textPrev.ParagraphsOS.Append(text.ParagraphsOS.HvoArray);
			book.SectionsOS.Remove(section);

			// Now we have to re-establish a selection. Whatever happens, it will be in the
			// same book as before, and the previous section, and in the body.
			if (InSectionHead || !fPositionAtEnd)
			{
				tracker.SetBookAndSection(helper, SelectionHelper.SelLimitType.Top,
					iBook, sectionPrev.IndexInOwner);
				helper.GetLevelInfo(SelectionHelper.SelLimitType.Top)[1].tag =
					ScrSectionTags.kflidContent;
			}
			Debug.Assert(helper.GetLevelInfo(SelectionHelper.SelLimitType.Top)[1].tag ==
				ScrSectionTags.kflidContent);

			if (fPositionAtEnd)
			{
				// we want selection at end of last paragraph of old previous section.
				// (That is, at the end of paragraph cparaPrev - 1.)
				Debug.Assert(cparaPrev > 0);
				helper.GetLevelInfo(SelectionHelper.SelLimitType.Top)[0].ihvo = cparaPrev - 1;
				IStTxtPara paraPrev = textPrev[cparaPrev - 1];

				int cchParaPrev = paraPrev.Contents.Length;
				helper.IchAnchor = cchParaPrev;
				helper.IchEnd = cchParaPrev;
				helper.AssocPrev = true;
			}
			else
			{
				// want selection at start of old first paragraph of deleted section.
				// (That is, at the start of paragraph cparaPrev.)
				helper.GetLevelInfo(SelectionHelper.SelLimitType.Top)[0].ihvo = cparaPrev;
				helper.IchAnchor = 0;
				helper.IchEnd = 0;
				helper.AssocPrev = false;
			}
			helper.SetLevelInfo(SelectionHelper.SelLimitType.Bottom,
				helper.GetLevelInfo(SelectionHelper.SelLimitType.Top));
			helper.SetIPAfterUOW();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges heading of given section into the heading of the follwing section and then
		/// deletes the given section. We assume that the content of the current section is
		/// empty.
		/// </summary>
		/// <param name="helper"> </param>
		/// <param name="book"></param>
		/// <param name="section"></param>
		/// ------------------------------------------------------------------------------------
		private void MergeHeadingWithFollowingSection(SelectionHelper helper, IScrBook book,
			IScrSection section)
		{
			// Get the following section and move the paragraphs.
			IScrSection sectionNext = section.NextSection;
			IStText textNext = sectionNext.HeadingOA;
			if (textNext == null)
			{
				// Prevent crash when dealing with corrupt database (TE-4869)
				// Since the next section doesn't have a heading text, we simply move the entire
				// text object from the current section head to the next section.
				sectionNext.HeadingOA = section.HeadingOA;
			}
			else
			{
				IStText textOldHeading = section.HeadingOA;
				textOldHeading.ParagraphsOS.MoveTo(0, textOldHeading.ParagraphsOS.Count - 1,
					textNext.ParagraphsOS, 0);
			}

			// protected for some reason...textNext.ParagraphsOS.Append(text.ParagraphsOS.HvoArray);
			book.SectionsOS.Remove(section);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if the beginning and the end of the selection are within the same type
		/// of paragraph (e.g. all in intro material or all in scripture paragraphs).
		/// </summary>
		/// <param name="helper"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool SectionsHaveSameContext(SelectionHelper helper)
		{
			// Make sure the beginning and end of the selection are in the same book.
			ILocationTracker tracker = ((ITeView)Control).LocationTracker;

			Debug.Assert(tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Top) ==
				tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Bottom));

			IScrSection section1 = tracker.GetSection(helper, SelectionHelper.SelLimitType.Top);
			IScrSection section2 = tracker.GetSection(helper, SelectionHelper.SelLimitType.Bottom);
			return SectionsHaveSameContext(section1, section2);
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if the beginning and the end of the selection are within the same type
		/// of paragraph (e.g. all in intro material or all in scripture paragraphs).
		/// </summary>
		/// <param name="section1">The section1.</param>
		/// <param name="section2">The section2.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------------
		private static bool SectionsHaveSameContext(IScrSection section1, IScrSection section2)
		{
			return (section1.IsIntro == section2.IsIntro);
		}

		#endregion

		#region Book Deletion
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the selected book.
		/// </summary>
		/// <param name="iBook">Index of book in the filter</param>
		/// ------------------------------------------------------------------------------------
		public virtual void RemoveBook(int iBook)
		{
			CheckDisposed();

			IVwRootSite rootSite = Callbacks.EditedRootBox.Site;

			int caNum = BookFilter.GetBook(iBook).CanonicalNum;
			IScrBook[] newFilterList = BookFilter.FilteredBooks.Select(x => x).Where(x => x.CanonicalNum != caNum).ToArray();
			IUndoAction filterAction = BookFilter.UpdateFilterAndCreateUndoAction(newFilterList);
			m_cache.ActionHandlerAccessor.AddAction(filterAction);

			m_scr.ScriptureBooksOS.Remove(m_scr.FindBook(caNum));

			if (m_app == null) // Can be null in the case of tests
			{
				IRootSite site = Callbacks.EditedRootBox.Site as IRootSite;
				if (site != null)
					site.RefreshDisplay();
			}

			// Reset IP if any books are left.
			if (BookFilter.BookCount > 0)
			{
				// need to adjust index if last book was deleted.
				iBook = Math.Min(iBook, BookFilter.BookCount - 1);
				// put IP at beginning of book title
				SetInsertionPoint(ScrBookTags.kflidTitle, iBook, 0);
			}
		}

		#endregion

		#region Picture Deletion
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes the currently selected picture, if there is one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void DeletePicture()
		{
			CheckDisposed();

			if (!IsPictureReallySelected)
				return;

			string undo;
			string redo;
			TeResourceHelper.MakeUndoRedoLabels("kstidDeletePicture", out undo, out redo);
			UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(undo, redo,
				m_cache.ServiceLocator.GetInstance<IActionHandler>(), ()=>
			{
				DeletePicture(CurrentSelection, CurrentSelection.LevelInfo[0].hvo);
			});
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes the picture specified by the hvo.
		/// </summary>
		/// <param name="helper">Selection containing the picture.</param>
		/// <param name="hvoPic">hvo of picture.</param>
		/// ------------------------------------------------------------------------------------
		protected void DeletePicture(SelectionHelper helper, int hvoPic)
		{
			int paraHvo = helper.GetLevelInfoForTag(StTextTags.kflidParagraphs).hvo;
			Debug.Assert(paraHvo != 0);

			int iBook, iSection;

			iBook = ((ITeView)Control).LocationTracker.GetBookIndex(helper,
				SelectionHelper.SelLimitType.Anchor);

			iSection = ((ITeView)Control).LocationTracker.GetSectionIndexInBook(helper,
				SelectionHelper.SelLimitType.Anchor);

			IStTxtPara para = m_repoScrTxtPara.GetObject(paraHvo);

			// Find the ORC and delete it from the paragraph
			ITsString contents = para.Contents;
			int startOfRun = 0;
			for (int i = 0; i < contents.RunCount; i++)
			{
				string str = contents.get_Properties(i).GetStrPropValue(
					(int)FwTextPropType.ktptObjData);

				if (str != null)
				{
					Guid guid = MiscUtils.GetGuidFromObjData(str.Substring(1));
					if (m_repoCmObject.GetObject(guid).Hvo == hvoPic)
					{
						ITsStrBldr bldr = contents.GetBldr();
						startOfRun = contents.get_MinOfRun(i);
						bldr.Replace(startOfRun, contents.get_LimOfRun(i), string.Empty, null);
						para.Contents = bldr.GetString();
						break;
					}
				}
			}

			// TODO (TE-4967): do a prop change that actually works.
			//			m_cache.DomainDataByFlid.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
			//				para.Hvo, StTxtParaTags.kflidContents,
			//				startOfRun, 0, 1);
			if (m_app != null)
				m_app.RefreshAllViews();

			// REVIEW: Isn't there another way to delete pictures in the new FDO?
			// They don't have an owner!!!!
			m_cache.DomainDataByFlid.DeleteObj(hvoPic);

			// TODO (TimS): This code to create a selection in the paragraph the picture was
			// in probably won't work when deleting a picture in back translation
			// material (which isn't possible to insert yet).

			((ITeView)Control).LocationTracker.SetBookAndSection(helper,
				SelectionHelper.SelLimitType.Anchor, iBook, iSection);

			helper.RemoveLevel(StTxtParaTags.kflidContents);

			if (Callbacks != null && Callbacks.EditedRootBox != null) // may not exist in tests.
			{
				try
				{
					MakeSimpleTextSelection(helper.LevelInfo, StTxtParaTags.kflidContents, startOfRun);
				}
				catch
				{
					// If we couldn't make the selection in the contents, it's probably because
					// we are in a user prompt, so try that instead.
					MakeSimpleTextSelection(helper.LevelInfo, SimpleRootSite.kTagUserPrompt, startOfRun);
				}

				Callbacks.EditedRootBox.Site.ScrollSelectionIntoView(
					Callbacks.EditedRootBox.Selection, VwScrollSelOpts.kssoNearTop);
			}
		}
		#endregion

		#region Insert Chapter Number
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a chapter number at the current location
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InsertChapterNumber()
		{
			CheckDisposed();

			SelectionHelper selHelper = GetSelectionReducedToIp(SelectionHelper.SelLimitType.Top);
			if (selHelper == null)
				return;

			// TODO: Try this with range selections in both directions
			selHelper.SetSelection(false); // need to install this because some calls below need it

			// get relevant information about the selection.
			ITsString tss;
			int ich;
			bool fAssocPrev;
			int hvoObj;
			int wsAlt; //the WS of the multiString alt, if selection is in a back translation
			int propTag;
			selHelper.Selection.TextSelInfo(true, out tss, out ich, out fAssocPrev,
				out hvoObj, out propTag, out wsAlt);

			string chapterNumber = null; // String representation of chapter number to insert
			int ichLimNew;

			SelLevInfo paraInfo = selHelper.GetLevelInfoForTag(StTextTags.kflidParagraphs);
			IScrTxtPara para = m_cache.ServiceLocator.GetInstance<IScrTxtParaRepository>().GetObject(paraInfo.hvo);

			// Establish whether we are in a vernacular paragraph or a back translation,
			// depending on the propTag of the given selection
			if (propTag == StTxtParaTags.kflidContents)
			{
				// selection is in a vernacular paragraph
				Debug.Assert(wsAlt == 0, "wsAlt should be 0 for a bigString");
				wsAlt = 0; // Some code depends on this being zero to indicate we are in vernacular
				// Adjust the insertion position to the beginning of a word - not in the middle
				ich = tss.FindWordBoundary(ich, UnicodeCharProps, ScrStyleNames.ChapterAndVerse);

				// Make a reference for the book and chapter + 1, validate it and get the
				// string equivalent.
				ScrReference curStartRef = CurrentStartRef;
				curStartRef.Chapter += 1;
				curStartRef.MakeValid();
				chapterNumber = m_scr.ConvertToString(curStartRef.Chapter);

				// If we are at the beginning of the first section, then use chapter 1.
				if (para.IndexInOwner == 0 && ich == 0 && IsFirstScriptureSection)
					chapterNumber = m_scr.ConvertToString(1);

				ITsTextProps ttp = StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
					m_wsContainer.DefaultVernacularWritingSystem.Handle);

				// Insert the chapter number into the tss, and update the cache
				para.ReplaceInParaOrBt(wsAlt, chapterNumber, ttp, ich, ich, out ichLimNew);
			}
			else
			{
				if (propTag == CmTranslationTags.kflidTranslation)
				{
					// selection is in a back translation
					Debug.Assert(wsAlt > 0, "wsAlt should be a valid WS for Translation multiBigString alt");
				}
				else if (propTag == RootSite.kTagUserPrompt)
				{
					// selection is a user prompt. By TE design, in Scripture this happens only in a back translation
					int nVar; //dummy for out params
					// get writing system from zero-width space in run 0 of the user prompt
					wsAlt = tss.get_Properties(0).GetIntPropValues(
						(int)FwTextPropType.ktptWs, out nVar);
					Debug.Assert(wsAlt > 0, "wsAlt should be a valid WS for Translation multiBigString alt");
					Debug.Assert(tss.get_Properties(0).GetIntPropValues(SimpleRootSite.ktptUserPrompt, out nVar) == 1);

					// Replace the TextSelInfo with stuff we can use
					ITsStrFactory tssFactory = TsStrFactoryClass.Create();
					tss = tssFactory.MakeString(string.Empty, wsAlt);
					ich = 0; // This should already be the case
				}
				else
					Debug.Assert(false, "Unexpected propTag");

				ich = tss.FindWordBoundary(ich, UnicodeCharProps, ScrStyleNames.ChapterAndVerse);

				// Are we in or next to a chapter/verse number?
				int ichMinRefBt, ichLimRefBt;
				if (InReference(tss, ich, true, out ichMinRefBt, out ichLimRefBt))
				{
					// move our focus to the start of the chapter/verse numbers
					ich = ichMinRefBt;
				}

				// Figure out the corresponding chapter number in the vernacular, if any.
				if (!para.InsertNextChapterNumberInBt(wsAlt, ich, ichLimRefBt, out ichLimNew))
				{
					MiscUtils.ErrorBeep(); // No verse number inserted or updated
					return;
				}
			}

			// set the cursor at the end of the chapter number
			if (CurrentSelection != null)
			{
				CurrentSelection.IchAnchor = ichLimNew;
				CurrentSelection.SetIPAfterUOW();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the current section is the first section of the book that contains
		/// scripture.
		/// </summary>
		/// <returns>true if it is the first section with scripture</returns>
		/// ------------------------------------------------------------------------------------
		public bool IsFirstScriptureSection
		{
			get
			{
				CheckDisposed();

				IScrBook book = BookFilter.GetBook(BookIndex);
				// Look at all of the previous sections to see if any are scripture sections
				int iSection = SectionIndex;
				return iSection >= 0 ? book[iSection].IsFirstScriptureSection : false;
			}
		}

		#endregion

		#region Insert Verse Number
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process the InsertVerseNumbers request.
		/// </summary>
		/// <param name="currentStateEnabled">current state of the insert verse numbers
		/// mode</param>
		/// ------------------------------------------------------------------------------------
		public void ProcessInsertVerseNumbers(bool currentStateEnabled)
		{
			CheckDisposed();

			if (currentStateEnabled)
				EndInsertVerseNumbers();
			else
			{
				// Enter InsertVerseNumbers mode.
				m_restoreCursor = DefaultCursor;
				DefaultCursor = TeResourceHelper.InsertVerseCursor;
				// don't re-add a message filter if verse number mode was already on
				if (!InsertVerseActive)
				{
					m_InsertVerseMessageFilter = new InsertVerseMessageFilter(Control, this);
					Application.AddMessageFilter(m_InsertVerseMessageFilter);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ends the inserting verse numbers mode
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void EndInsertVerseNumbers()
		{
			CheckDisposed();

			DefaultCursor = m_restoreCursor;
			Application.RemoveMessageFilter(m_InsertVerseMessageFilter);
			m_InsertVerseMessageFilter = null;

			if (Callbacks.EditedRootBox.Selection != null)
				SelectionChanged();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert verse number at the current insertion point, with undo.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InsertVerseNumber()
		{
			CheckDisposed();

			SelectionHelper selHelper = GetSelectionReducedToIp(SelectionHelper.SelLimitType.Top);
			if (selHelper == null)
			{
				MiscUtils.ErrorBeep();
				return;
			}

			selHelper.SetSelection(false); // need to install this because some calls below need it

			// If selection is not in ScrSection Content (vern or BT), quit
			if (!CanInsertNumberInElement)
			{
				MiscUtils.ErrorBeep();
				return;
			}

			InsertVerseNumber(selHelper);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Low-level implementation of insert verse number.
		/// </summary>
		/// <param name="selHelper">the given SelectionHelper</param>
		/// ------------------------------------------------------------------------------------
		public void InsertVerseNumber(SelectionHelper selHelper)
		{
			CheckDisposed();
			Debug.Assert(selHelper != null);
			Debug.Assert(!selHelper.IsRange || IsSelectionInPrompt(selHelper));

			// Get the details about the current selection
			int ichSelOrig; //the character offset of the selection in the ITsString
			int hvoObj; //the id of the object the selection is in (StTxtPara or CmTranslation)
			int propTag; //property tag of object
			ITsString tssSel; //ITsString containing the selection
			int wsAlt; //the WS of the multiString alt, if selection is in a back translation
			ichSelOrig = GetSelectionInfo(selHelper, out hvoObj, out propTag, out tssSel, out wsAlt);

			// The current run is a chapter number and IP is either at the beginning of the line or
			// in the middle of the chapter number, we need to jump past it to insert the verse number
			// otherwise it will insert it before the chapter number or in the chapter number.
			int iRun = tssSel.get_RunAt(ichSelOrig);
			if (tssSel.Style(iRun) == ScrStyleNames.ChapterNumber &&
				(ichSelOrig == 0 || ichSelOrig > tssSel.get_MinOfRun(iRun)))
				ichSelOrig = tssSel.get_LimOfRun(iRun);

			// Adjust the insertion position to the beginning of a word - not in the middle
			//  (may move to an existing verse number too)
			int ichWord = tssSel.FindWordBoundary(ichSelOrig, UnicodeCharProps, ScrStyleNames.ChapterAndVerse);

			//			TomB and MarkB have decided we won't do this, at least for now
			//			// If the start of the Bt does not match the vernacular, adjust it if required.
			//			if (ichWord > 0)
			//				cInsDel = SetVerseAtStartOfBtIfNeeded();
			//			ichWord += cInsDel; //adjust

			// some key variables set by Update or Insert methods, etc
			string sVerseNumIns = null; // will hold the verse number string we inserted; could be
			//   a simple number, a verse bridge, or the end number added to a bridge
			string sChapterNumIns = null; // will hold chapter number string inserted or null if none
			int ichLimIns = -1; //will hold the end of the new chapter/verse numbers we update or insert

			// Is ichWord in or next to a verse number? (if so, get its ich range)
			bool fCheckForChapter = (wsAlt != 0); //check for chapter in BT only
			int ichMin; // min of the verse number run, if we are on one
			int ichLim; // lim of the verse number run, if we are on one
			bool fFoundExistingRef =
				InReference(tssSel, ichWord, fCheckForChapter, out ichMin, out ichLim);

			SelLevInfo paraInfo = selHelper.GetLevelInfoForTag(StTextTags.kflidParagraphs);
			IScrTxtPara para = m_cache.ServiceLocator.GetInstance<IScrTxtParaRepository>().GetObject(paraInfo.hvo);

			// If we moved the selection forward (over spaces or punctuation) to an
			//  existing verse number ...
			if (fFoundExistingRef && (ichSelOrig < ichWord))
			{
				//Attempt to insert a verse number at the IP, if one is missing there.
				// if selection is in vernacular...
				if (propTag == StTxtParaTags.kflidContents)
				{
					// Insert missing verse number in vernacular
					para.InsertMissingVerseNumberInVern(ichSelOrig, ichWord, out sVerseNumIns,
						out ichLimIns);
				}
			}

			// if a verse number was not inserted, sVerseNumIns is null
			// If no verse number inserted yet...
			if (sVerseNumIns == null)
			{
				if (fFoundExistingRef)
				{
					//We must update the existing verse number at ichWord
					// is selection in vern or BT?
					if (propTag == StTxtParaTags.kflidContents)
					{
						// Update verse number in vernacular
						para.UpdateExistingVerseNumberInVern(ichMin, ichLim, out sVerseNumIns,
							out ichLimIns);
					}
					else
					{
						//Update verse number in back translation
						para.UpdateExistingVerseNumberInBt(wsAlt, ichMin, ichLim, out sVerseNumIns,
							out sChapterNumIns, out ichLimIns);
					}
				}
				else
				{
					// We're NOT on an existing verse number, so insert the next appropriate one.
					// is selection in vern or BT?
					if (propTag == StTxtParaTags.kflidContents)
					{
						para.InsertNextVerseNumberInVern(ichWord, out sVerseNumIns, out ichLimIns);
					}
					else
					{
						para.InsertNextVerseNumberInBt(wsAlt, ichWord, out sVerseNumIns,
							out sChapterNumIns, out ichLimIns);
						selHelper.SetTextPropId(SelectionHelper.SelLimitType.Anchor,
							CmTranslationTags.kflidTranslation);
						selHelper.SetTextPropId(SelectionHelper.SelLimitType.End,
							CmTranslationTags.kflidTranslation);
					}
				}
			}

			if (sVerseNumIns == null)
			{
				MiscUtils.ErrorBeep(); // No verse number inserted or updated
				return;
			}

			// set new IP behind the verse number
			selHelper.IchAnchor = ichLimIns;
			selHelper.IchEnd = ichLimIns;
			selHelper.AssocPrev = true;
			selHelper.SetIPAfterUOW();

			// Remove any duplicate chapter/verse numbers following the new verse number.
			para.RemoveDuplicateVerseNumbers(wsAlt, sChapterNumIns, sVerseNumIns, ichLimIns);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get information about the selection including the selection, the property tag,
		/// and the writing system.
		/// </summary>
		/// <param name="selHelper">selection helper. This is intended to be called with a
		/// selection helper which represents an insertion point, but if this is a range
		/// selection, the information returned will be for the anchor point.
		/// </param>
		/// <param name="hvoObj">out: the id of the object the selection is in (StTxtPara or
		/// CmTranslation)</param>
		/// <param name="propTag">out: tag/flid of the field that contains the TsString (e.g.,
		/// for vernacular text, this will be StTxtPara.StTxtParaTags.kflidContents)</param>
		/// <param name="tssSel">out: ITsString containing selection (this is the full string,
		/// not just the selected portion)</param>
		/// <param name="wsAlt">out: the WS id of the multiString alt, if selection is in a back
		/// translation; otherwise 0</param>
		/// <returns>the character offset (in the ITsString) represented by the anchor of the
		/// given selection helper.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private int GetSelectionInfo(SelectionHelper selHelper, out int hvoObj, out int propTag,
			out ITsString tssSel, out int wsAlt)
		{
			Debug.Assert(selHelper != null);
			Debug.Assert(selHelper.Selection != null);

			// get relevant information about the selection.
			int ichSel;
			bool fAssocPrev;
			selHelper.Selection.TextSelInfo(true, out tssSel, out ichSel,
				out fAssocPrev, out hvoObj, out propTag, out wsAlt);

			// Establish whether we are in a vernacular paragraph or a back translation,
			//  depending on the propTag of the given selection
			if (propTag == StTxtParaTags.kflidContents)
			{
				// selection is in a vernacular paragraph
				Debug.Assert(wsAlt == 0, "wsAlt should be 0 for a bigString");
				wsAlt = 0; // Some code depends on this being zero to indicate we are in vernacular
			}
			else if (propTag == CmTranslationTags.kflidTranslation || propTag == SegmentTags.kflidFreeTranslation)
			{
				// selection is in a back translation
				Debug.Assert(wsAlt > 0, "wsAlt should be a valid WS for Translation multiBigString alt");
			}
			else if (propTag == SimpleRootSite.kTagUserPrompt)
			{
				int nVar; //dummy for out params
				Debug.Assert(tssSel.get_Properties(0).GetIntPropValues(SimpleRootSite.ktptUserPrompt, out nVar) == 1);
				wsAlt = tssSel.get_Properties(0).GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
				tssSel = TsStringUtils.MakeTss(string.Empty, wsAlt);
				ichSel = 0; // This should already be the case

				if (selHelper.LevelInfo[0].tag == StTextTags.kflidParagraphs)
				{
					// Prompt was in the vernacular (in an empty title or section head)
					wsAlt = 0; // Some code depends on this being zero to indicate we are in vernacular
					propTag = StTxtParaTags.kflidContents;
				}
				else
				{
					// Get writing system from zero-width space in run 0 of the user prompt
					Debug.Assert(wsAlt > 0, "wsAlt should be a valid WS for Translation multiBigString alt");
					propTag = Options.UseInterlinearBackTranslation ?
						SegmentTags.kflidFreeTranslation : CmTranslationTags.kflidTranslation;
				}
			}
			else
				Debug.Assert(false, "Unexpected propTag");

			return ichSel;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scan for previous chapter number run in a given ITsString.
		/// </summary>
		/// <param name="tss">given ITsString</param>
		/// <param name="ichMin">starting character index</param>
		/// <returns>value of previous chapter in tss, or 0 if no valid chapter found</returns>
		/// ------------------------------------------------------------------------------------
		private int GetPreviousChapter(ITsString tss, int ichMin)
		{
			for (int iRun = tss.get_RunAt(ichMin) - 1; iRun >= 0; iRun--)
			{
				if (tss.Style(iRun) == ScrStyleNames.ChapterNumber)
					return ScrReference.ChapterToInt(tss.get_RunText(iRun));
			}

			return 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If selection is a simple IP, check current run or runs on either side (if between
		/// runs). If run(s) is/are a verse number run, return true.
		/// </summary>
		/// <param name="vwsel">selection to check</param>
		/// <returns>True if IP is in or next to a verse number run</returns>
		/// ------------------------------------------------------------------------------------
		private bool SelIPInVerseNumber(IVwSelection vwsel)
		{
			if (vwsel.IsRange)
				return false;

			ITsString tss;
			int ich, hvo, tag, ws, ichMin, ichLim;
			bool fAssocPrev;
			vwsel.TextSelInfo(true, out tss, out ich, out fAssocPrev, out hvo, out tag, out ws);
			return InReference(tss, ich, false, out ichMin, out ichLim);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the given character position is next to or in a chapter/verse number
		/// run in the given ITsString. This overload provides less detail.
		/// </summary>
		/// <param name="tss">The string to examine</param>
		/// <param name="ich">character index to look at</param>
		/// <param name="fCheckForChapter">true if we want to include searching for chapters</param>
		/// <param name="ichMin">Gets set to the beginning of the chapter/verse number run if
		/// we find a verse number</param>
		/// <param name="ichLim">Gets set to the Lim of the chapter/verse number run if
		/// we find a verse number</param>
		/// <returns><c>true</c> if we find a verse number/bridge (and/or chapter);
		/// <c>false</c> if not</returns>
		/// ------------------------------------------------------------------------------------
		private bool InReference(ITsString tss, int ich, bool fCheckForChapter, out int ichMin,
			out int ichLim)
		{
			int ichBetweenChapterAndVerse;
			bool fFoundVerse;
			return InReference(tss, ich, fCheckForChapter, out ichMin, out ichLim,
				out ichBetweenChapterAndVerse, out fFoundVerse);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the given character position is in or next to a chapter/verse number
		/// run in the given ITsString.
		/// If we are, check the run beyond so we can locate the entire chapt
		/// </summary>
		/// <param name="tss">The string to examine</param>
		/// <param name="ich">character index to look at</param>
		/// <param name="fCheckForChapter">true if we want to include searching for chapters</param>
		/// <param name="ichMin">output: Gets set to the beginning of the chapter/verse number
		/// run if we find a verse/chapter number; otherwise -1</param>
		/// <param name="ichLim">output: Gets set to the Lim of the chapter/verse number run if
		/// we find a verse/chapter number; otherwise -1</param>
		/// <param name="ichBetweenChapterAndVerse">NO LONGER USED
		/// Was output: set to the position between the
		/// chapter and verse number runs if we find a verse number and chapter number run
		/// at (near) ich; otherwise -1</param>
		/// <param name="fVerseAtIch">output: true if ich is in or next to a verse number</param>
		/// <returns><c>true</c> if we find a verse number/bridge (and/or chapter);
		/// <c>false</c> if not</returns>
		/// ------------------------------------------------------------------------------------
		private bool InReference(ITsString tss, int ich, bool fCheckForChapter, out int ichMin,
			out int ichLim, out int ichBetweenChapterAndVerse, out bool fVerseAtIch)
		{
			ichMin = -1;
			ichLim = -1;
			ichBetweenChapterAndVerse = -1;
			fVerseAtIch = false;

			if (tss.Length == 0)
				return false;

			// Check the run to the right of ich
			int iRun = tss.get_RunAt(ich);
			if (FindRefRunMinLim(tss, iRun, fCheckForChapter,
				out ichMin, out ichLim, out fVerseAtIch))
			{
				// We found a verse (or chapter) run. Check following run too.
				if (tss.RunCount > iRun + 1)
				{
					bool dummy;
					int ichMinNext, ichLimNext;
					if (FindRefRunMinLim(tss, iRun + 1, fCheckForChapter,
						out ichMinNext, out ichLimNext, out dummy))
					{
						// found another reference run. extend the ichLim.
						ichLim = ichLimNext;
					}
				}
			}

			// if iRun is the first run: we are done, return our result
			if (iRun == 0)
				return (ichMin > -1);

			// Is the char to the left of ich in the same run as ich??
			int iRunPrevChar = tss.get_RunAt(ich - 1);
			if (iRunPrevChar == iRun)
				return (ichMin > -1); //no adjacent ref to the left, so we are done

			// Check the run to the left of ich
			bool dummy2;
			int ichMinPrev, ichLimPrev;
			if (FindRefRunMinLim(tss, iRunPrevChar, fCheckForChapter,
				out ichMinPrev, out ichLimPrev, out dummy2))
			{
				// We found a verse (or chapter) run.
				ichMin = ichMinPrev; // set or extend the ichMin
				if (ichLim == -1) // reference not found after ich
					ichLim = ichLimPrev;
				// check an additional previous run, too
				if (iRunPrevChar >= 1)
				{
					if (FindRefRunMinLim(tss, iRunPrevChar - 1, fCheckForChapter,
						out ichMinPrev, out ichLimPrev, out dummy2))
					{
						// found a second previous reference run. extend the ichMin.
						ichMin = ichMinPrev;
					}
				}
			}

			return (ichMin > -1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the extent of the current run if it is a chapter (optionally) or verse
		/// number.
		/// </summary>
		/// <param name="tss">paragraph</param>
		/// <param name="iRun">run index</param>
		/// <param name="fCheckForChapter">true if we want to include searching for chapters
		/// </param>
		/// <param name="ichMin">output: index at beginning of run</param>
		/// <param name="ichLim">output: index at limit of run</param>
		/// <param name="fIsVerseNumber">output: <c>true</c> if this run is a verse number run
		/// </param>
		/// <returns><c>true</c> if this run is a verse number/bridge (or chapter, if checking
		/// for that); <c>false</c> if not</returns>
		/// ------------------------------------------------------------------------------------
		private bool FindRefRunMinLim(ITsString tss, int iRun, bool fCheckForChapter,
			out int ichMin, out int ichLim, out bool fIsVerseNumber)
		{
			ichMin = -1;
			ichLim = -1;

			// Check current run to see if it's a verse (or chapter) number.
			fIsVerseNumber = tss.Style(iRun) == ScrStyleNames.VerseNumber;
			bool fIsChapterNumber = false;

			if (!fIsVerseNumber && fCheckForChapter)
				fIsChapterNumber = tss.Style(iRun) == ScrStyleNames.ChapterNumber;

			if (fIsVerseNumber || fIsChapterNumber)
			{
				ichMin = tss.get_MinOfRun(iRun);
				ichLim = tss.get_LimOfRun(iRun);
			}

			return (fIsVerseNumber || fIsChapterNumber);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prevents window events from ending insert verse mode.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void PreventEndInsertVerseNumbers()
		{
			CheckDisposed();

			if (m_InsertVerseMessageFilter != null)
				m_InsertVerseMessageFilter.PreventEndInsertVerseNumbers();
		}

		//		// TomB and MarkB have decided not to do this, at least for now
		//		//  but it's a unique bunch of logic that we may need later
		//		private int SetVerseAtStartOfBtIfNeeded(ITsString tssBt, int ichSel, ITsString tssVern)
		//		{
		//			if (ichSel == 0)
		//				return 0; // Insert/Update methods will handle start of BT
		//
		//			bool fShouldCheckStartOfBt = false;
		//
		//			// Search for a verse number that preceeds the current run
		//			TsRunInfo tsiBt;
		//			tssBt.FetchRunInfoAt(ichSel, out tsiBt);
		//			int ich = tsiBt.ichMin;
		//			//			if (ich == 0) // if we're at the first run, no need to continue search
		//			//				fShouldCheckStartOfBt = true;
		//			//			else
		//			if (ich > 0)
		//				--ich;
		//
		//			ITsTextProps ttpBt;
		//			while (ich >= 0)
		//			{
		//				// Get props of current run.
		//				ttpBt = tss.FetchRunInfoAt(ich, out tsiBt);
		//				// See if it is our verse number style.
		//				if (StStyle.IsStyle(ttpBt, ScrStyleNames.VerseNumber))
		//				{
		//					if (tsiBt.irun > 0)
		//						return 0; //this verse number is in mid para; no need to update start of BT
		//				}
		//
		//				// move index (going backwards) to the Min of the run we just looked at
		//				ich = tsi.ichMin;
		//				// then decrement index so it's in the previous run, if any
		//				ich--;
		//			}
		//
		//			// We now have information about the first run of the BT.
		//			// Now we need to get information about the first run of the vernacular.
		//			TsRunInfo tsiVern;
		//			ITsTextProps ttpVern;
		//			ttpVern = tssVern.FetchRunInfo(0, out tsiVern);
		//
		//			int charToInsert;
		//			bool vernStartsWithVerse = StStyle.IsStyle(ttpVern, ScrStyleNames.VerseNumber);
		//			bool btStartsWithVerse = StStyle.IsStyle(ttpBt, ScrStyleNames.VerseNumber);
		//			if (!vernStartsWithVerse && !btStartsWithVerse)
		//			{
		//				// Both vernacular and BT do not start with verse numbers.
		//				// No need to update.
		//				return 0;
		//			}
		//			else if (vernStartsWithVerse)
		//			{
		//				// get verse from vernacular and insert into back translation
		//				string sVerseNumIns = tssVern.get_RunAt(0);
		//				return (sVerseNumIns.Length);
		//			}
		//
		//			// Are there previous verse number runs in the BT para?
		//
		//			string btRun, vernRun; ...
		//		}

		#endregion

		#region Insert Footnote
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts a footnote at the current selection
		/// </summary>
		/// <param name="styleName">style name for created footnote</param>
		/// <param name="iFootnote">out: If selHelper is in vernacular para, the ihvo of the
		/// footnote just inserted. If selHelper is in back trans, the ihvo of the footnote
		/// corresponding to the ref ORC just inserted in the BT, or -1 no corresponding</param>
		/// <returns>The created/corresponding footnote</returns>
		/// ------------------------------------------------------------------------------------
		public IStFootnote InsertFootnote(string styleName, out int iFootnote)
		{
			CheckDisposed();

			return InsertFootnote(CurrentSelection, styleName, out iFootnote);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts a footnote at the given selection
		/// </summary>
		/// <param name="selHelper">Current selection information</param>
		/// <param name="styleName">style name for created footnote</param>
		/// <param name="iFootnote">out: If selHelper is in vernacular para, the ihvo of the
		/// footnote just inserted. If selHelper is in back trans, the ihvo of the footnote
		/// corresponding to the ref ORC just inserted in the BT, or -1 no corresponding</param>
		/// <returns>The created/corresponding footnote</returns>
		/// ------------------------------------------------------------------------------------
		public virtual IStFootnote InsertFootnote(SelectionHelper selHelper, string styleName,
			out int iFootnote)
		{
			CheckDisposed();
			// Get any selected text.
			ITsString tssSelectedText;
			IVwSelection vwsel = selHelper.Selection;
			if (!IsSelectionInUserPrompt && IsSelectionInOneEditableProp(vwsel))
				vwsel.GetSelectionString(out tssSelectedText, string.Empty);
			else
				tssSelectedText = TsStringUtils.MakeTss(string.Empty, m_cache.DefaultVernWs);

			int hvoObj;
			ITsString tssSel; // This is either the vernacular or the BT, depending on the selection location.
			int propTag;
			int ws;
			selHelper.ReduceToIp(SelectionHelper.SelLimitType.Bottom, false, false);
			int ichSel = GetSelectionInfo(selHelper, out hvoObj, out propTag, out tssSel, out ws);
			if (propTag == StTxtParaTags.kflidContents)
				ws = Cache.DefaultVernWs;

			// Make sure the selection is updated with the new ich position in case it was wrong
			// (e.g. If the selection is in a user prompt) (TE-8919)
			selHelper.IchAnchor = selHelper.IchEnd = ichSel;

			// get book info
			IScrBook book = GetCurrentBook(m_cache);

			// get paragraph info
			int paraHvo = selHelper.GetLevelInfoForTag(StTextTags.kflidParagraphs).hvo;
			IScrTxtPara para = m_repoScrTxtPara.GetObject(paraHvo);

			tssSelectedText = TsStringUtils.GetCleanTsString(tssSelectedText, ScrStyleNames.ChapterAndVerse);
			Debug.Assert(tssSelectedText != null);
			if (tssSelectedText.Length > 0)
			{
				ITsStrBldr bldr = tssSelectedText.GetBldr();
				bldr.SetStrPropValue(0, bldr.Length, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.ReferencedText);
				bldr.ReplaceRgch(bldr.Length, bldr.Length, " ", 1, StyleUtils.CharStyleTextProps(null, ws));
				tssSelectedText = bldr.GetString();
			}

			//Cache.ActionHandlerAccessor.AddAction(new UndoWithRefreshAction());
			IScrFootnote footnote = null;
			iFootnote = -1;
			if (propTag == StTxtParaTags.kflidContents)
			{
				// Inserting footnote into the vernacular paragraph
				iFootnote = FindFootnotePosition(book, selHelper);
				ITsStrBldr tsStrBldr = para.Contents.GetBldr();
				// create the footnote and insert its marker into the paragraph's string
				// builder.
				footnote = book.InsertFootnoteAt(iFootnote, tsStrBldr, ichSel);

				// BEFORE we insert the ORC in the paragraph, we need to insert an empty
				// paragraph into the new StFootnote, because the para style is needed to
				// determine the footnote marker type.
				IStTxtPara footnotePara = footnote.AddNewTextPara(styleName);

				// update the paragraph contents to include the footnote marker
				para.Contents = tsStrBldr.GetString();

				// Insert the selected text (or an empty run) into the footnote paragraph.
				footnotePara.Contents = tssSelectedText;
			}
			else
			{
				IMultiString bt = null;
				if (propTag == SegmentTags.kflidFreeTranslation)
				{
					// Inserting footnote reference ORC into a segment free translation
					ISegment segment = m_repoSegment.GetObject(selHelper.GetLevelInfoForTag(StTxtParaTags.kflidSegments).hvo);
					bt = segment.FreeTranslation;
					footnote = FindVernParaFootnote(segment, ws);
				}
				else if (propTag == CmTranslationTags.kflidTranslation)
				{
					// Inserting footnote reference ORC into a back translation
					footnote = FindVernParaFootnote(ichSel, tssSel, para);
					bt = para.GetBT().Translation;
				}

				if (footnote != null)
				{
					// Insert footnote reference ORC into back translation paragraph.
					ITsStrBldr tssBldr = tssSel.GetBldr();
					//if reference to footnote already somewhere else in para, delete it first
					int ichDel = TsStringUtils.DeleteOrcFromBuilder(tssBldr, footnote.Guid);
					if (ichDel >= 0 && ichSel > ichDel)
						ichSel--;

					TsStringUtils.InsertOrcIntoPara(footnote.Guid, FwObjDataTypes.kodtNameGuidHot,
						tssBldr, ichSel, ichSel, ws);
					bt.set_String(ws, tssBldr.GetString());
					iFootnote = footnote.IndexInOwner;

					if (tssSelectedText.Length > 0)
					{
						ICmTranslation btFootnoteTrans = ((IStTxtPara)footnote.ParagraphsOS[0]).GetBT();
						ITsString btFootnoteTss = btFootnoteTrans.Translation.get_String(ws);

						// Insert any selected text into this back translation for the footnote paragraph.
						btFootnoteTrans.Translation.set_String(ws, tssSelectedText);
					}
				}
			}

			if (footnote == null)
				MiscUtils.ErrorBeep(); // No footnote reference ORC inserted
			else
			{
				// Update the selection in the view that the footnote was inserted.
				selHelper.UpdateScrollLocation(); // Make sure this is up-to-date
				selHelper = new SelectionHelper(selHelper); // Get a new copy, so any subsequent changes won't affect us.
				selHelper.IchAnchor++;
				selHelper.AssocPrev = false; // Associate away from the newly inserted marker to avoid shifting scroll position by a couple pixels
				selHelper.IchEnd = selHelper.IchAnchor;
				Callbacks.RequestVisibleSelectionAtEndOfUow(selHelper);
			}

			return footnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds position in footnote list (0-based index) where new footnote should be
		/// inserted.
		/// </summary>
		/// <param name="book">book where footnote will be inserted</param>
		/// <param name="helper"></param>
		/// <returns>index of the footnote in the book</returns>
		/// ------------------------------------------------------------------------------------
		private int FindFootnotePosition(IScrBook book, SelectionHelper helper)
		{
			if (book.FootnotesOS.Count == 0)
				return 0;

			IStFootnote prevFootnote = FindFootnoteNearSelection(helper, book, false);
			if (prevFootnote == null)
				return 0;

			return prevFootnote.IndexInOwner + 1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the first available vernacular footnote which does not correspond to any
		/// existing one in the given segment's free translation.
		/// </summary>
		/// <param name="segment">The segment.</param>
		/// <param name="ws">The writing system of the translation.</param>
		/// <returns>corresponding vernacular footnote, or null if no corresponding footnote</returns>
		/// ------------------------------------------------------------------------------------
		private IScrFootnote FindVernParaFootnote(ISegment segment, int ws)
		{
			ITsString tss = segment.FreeTranslation.get_String(ws);
			// Scan the segment free translation to collect the set of footnotes already present
			IEnumerable<Guid> existingBtFootnotes = tss.GetAllEmbeddedObjectGuids(FwObjDataTypes.kodtNameGuidHot);
			foreach (Guid footnote in segment.BaselineText.GetAllEmbeddedObjectGuids(FwObjDataTypes.kodtOwnNameGuidHot))
			{
				if (!existingBtFootnotes.Contains(footnote))
					return m_repoScrFootnote.GetObject(footnote);
			}

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the vernacular footnote which corresponds to the selection position in the
		/// back translation. If no footnote in the vernacular corresponds to the one in the
		/// back translation, then return null.
		/// </summary>
		/// <param name="ichBtSel">the character offset in the back translation paragraph</param>
		/// <param name="btTss">back translation ITsString</param>
		/// <param name="vernPara">owning vernacular paragraph</param>
		/// <returns>corresponding vernacular footnote, or null if no corresponding footnote
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private IScrFootnote FindVernParaFootnote(int ichBtSel, ITsString btTss, IStTxtPara vernPara)
		{
			// Scan through the back translation paragraph to see if there are any footnotes
			int iBtFootnote = FindBtFootnoteIndex(ichBtSel, btTss);

			// Scan through the owning paragraph to see if there are enough footnotes to allow
			// the insertion of another footnote in the back translation
			return FindFootnoteAtIndex(vernPara, iBtFootnote);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine the zero-based index within a paragraph of a footnote to be inserted in
		/// the back translation.</summary>
		/// <param name="ichBtSel">the character offset in the back translation paragraph</param>
		/// <param name="btTss">back translation ITsString</param>
		/// <returns>zero-based index of a footnote to be inserted</returns>
		/// ------------------------------------------------------------------------------------
		private int FindBtFootnoteIndex(int ichBtSel, ITsString btTss)
		{
			// Scan through the back translation paragraph to find the index of the footnote
			// to insert
			int iRun = 0;
			int iBtFootnote = 0;
			int cFullRunsBeforeIchBtSel = (ichBtSel < btTss.Length) ? btTss.get_RunAt(ichBtSel) : btTss.RunCount;

			while (iRun < cFullRunsBeforeIchBtSel)
			{
				if (TsStringUtils.GetHotObjectGuidFromProps(btTss.get_Properties(iRun)) != Guid.Empty)
				{
					// Found footnote ORC in back translation
					iBtFootnote++;
				}
				iRun++;
			}

			return iBtFootnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find a footnote in the vernacular paragraph at the specified index within the para.
		/// </summary>
		/// <param name="vernPara">owning vernacular paragraph</param>
		/// <param name="index">index within the vernPara of footnote to find</param>
		/// <returns>footnote in vernacular, if found</returns>
		/// ------------------------------------------------------------------------------------
		private IScrFootnote FindFootnoteAtIndex(IStTxtPara vernPara, int index)
		{
			ITsString vernTss = vernPara.Contents;

			// Scan through the vernacular paragraph to find the corresponding footnote
			Guid guid;
			FwObjDataTypes odt;
			int iRun = 0;
			int iVernFootnote = 0;
			while (iRun < vernTss.RunCount)
			{
				guid = TsStringUtils.GetOwnedGuidFromRun(vernTss, iRun, out odt);

				if (odt == FwObjDataTypes.kodtOwnNameGuidHot)
				{
					if (index == iVernFootnote)
					{
						// Found a footnote in the vernacular that is the same index in the paragraph
						// as the one we are trying to insert into the back translation
						return m_repoScrFootnote.GetObject(guid);
					}
					iVernFootnote++; // Found a footnote, but it was before the one we are looking for
				}
				iRun++;
			}

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the nearest footnote around the given selection. Searches backward and
		/// forward.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IStFootnote FindFootnoteNearSelection(SelectionHelper helper)
		{
			CheckDisposed();

			if (CurrentSelection == null)
				return null;

			return FindFootnoteNearSelection(helper, GetCurrentBook(m_cache), true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the nearest footnote before the given selection.
		/// </summary>
		/// <param name="helper">The selection helper.</param>
		/// <param name="book">The book that owns the footnote collection.</param>
		/// <param name="fSearchForward">True to also search forward within the current paragraph</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private IStFootnote FindFootnoteNearSelection(SelectionHelper helper, IScrBook book,
			bool fSearchForward)
		{
			CheckDisposed();

			if (helper == null)
				helper = CurrentSelection;
			if (helper == null || book == null)
				return null;

			SelLevInfo[] levels = helper.GetLevelInfo(SelectionHelper.SelLimitType.Anchor);

			int iParagraph = -1;
			int tag = 0;
			int ich = helper.IchAnchor;
			int paraLev = helper.GetLevelForTag(StTextTags.kflidParagraphs);
			int contentLev = helper.GetLevelForTag(StTxtParaTags.kflidContents);

			Debug.Assert(paraLev != -1, "Need a paragraph for this method");
			iParagraph = levels[paraLev].ihvo;

			int iSection = ((ITeView)Control).LocationTracker.GetSectionIndexInBook(
				helper, SelectionHelper.SelLimitType.Anchor);

			if (iSection < 0)
				tag = ScrBookTags.kflidTitle;
			else
			{
				tag = (helper.GetLevelForTag(ScrSectionTags.kflidContent) >= 0 ?
					ScrSectionTags.kflidContent : ScrSectionTags.kflidHeading);
			}

			// Special case: if we're in the caption of a picture, get the ich from
			// the first level instead of the anchor. (TE-4696)
			if (contentLev >= 0 && levels[contentLev].ihvo == -1)
				ich = levels[contentLev].ich;

			IScrFootnote prevFootnote = null;
			if (fSearchForward) // look first at our current position, if we are searching foward
				prevFootnote = book.FindCurrentFootnote(iSection, iParagraph, ich, (int)tag);

			if (prevFootnote == null)
			{
				IScrTxtPara para = m_repoScrTxtPara.GetObject(levels[paraLev].hvo);
				ITsString tss = para.Contents;
				if (ich != 0)
				{
					// look backwards in our current paragraph - skip the current run, except when
					// at the end of the text.
					prevFootnote = para.FindPrevFootnoteInContents(ref ich, ich < tss.Length);
				}
				else if (iParagraph > 0)
				{
					// look at the previous paragraph for a footnote at the end
					IStText text = m_repoStText.GetObject(levels[paraLev + 1].hvo);
					IScrTxtPara prevPara = (IScrTxtPara)text[iParagraph - 1];
					ITsString prevTss = prevPara.Contents;
					int ichTmp = -1;
					prevFootnote = prevPara.FindPrevFootnoteInContents(ref ichTmp, false);
					if (prevFootnote != null)
					{
						if (ichTmp == prevTss.Length - 1)
							ich = ichTmp;
						else
							prevFootnote = null;
					}
					// ENHANCE: Look across contexts.
				}
			}
			if (prevFootnote == null && fSearchForward)
			{
				// look ahead in the same paragraph
				IScrTxtPara para = m_repoScrTxtPara.GetObject(levels[paraLev].hvo);
				ITsString tss = para.Contents;
				prevFootnote = para.FindNextFootnoteInContents(ref ich, true);
			}
			if (prevFootnote == null)
			{
				// just go back until we find one
				prevFootnote = book.FindPrevFootnote(ref iSection, ref iParagraph, ref ich, ref tag);
			}

			return prevFootnote;
		}

		#endregion

		#region Methods to support inserting annotations
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets info about the current selection needed to insert an annotation.
		/// </summary>
		/// <param name="beginObj">The begin object referenced by the annotation.</param>
		/// <param name="endObj">The end object referenced by the annotation.</param>
		/// <param name="wsSelector">The writing system selector.</param>
		/// <param name="tssQuote">The quote in the annotation (referenced text).</param>
		/// <param name="startRef">The starting reference in the selection.</param>
		/// <param name="endRef">The end reference in the selection.</param>
		/// <param name="startOffset">The starting character offset in the beginObj.</param>
		/// <param name="endOffset">The ending character offset in the endObj.</param>
		/// <exception cref="InvalidOperationException">When the current selection is not in
		/// a paragraph at all</exception>
		/// ------------------------------------------------------------------------------------
		public void GetAnnotationLocationInfo(out ICmObject beginObj, out ICmObject endObj,
			out int wsSelector, out int startOffset, out int endOffset, out ITsString tssQuote,
			out BCVRef startRef, out BCVRef endRef)
		{
			CheckDisposed();

			beginObj = null;
			endObj = null;
			if (CurrentSelection != null)
			{
				// Get the Scripture reference information that the note will apply to
				SelLevInfo[] startSel = CurrentSelection.GetLevelInfo(SelectionHelper.SelLimitType.Top);
				SelLevInfo[] endSel = CurrentSelection.GetLevelInfo(SelectionHelper.SelLimitType.Bottom);
				if (startSel.Length > 0 && endSel.Length > 0)
				{
					// Get the objects at the beginning and end of the selection.
					beginObj = m_repoCmObject.GetObject(startSel[0].hvo);
					endObj = m_repoCmObject.GetObject(endSel[0].hvo);
				}
			}

			if (beginObj == null || endObj == null)
			{
				// No selection, or selection is in unexpected object.
				throw new InvalidOperationException("No translation or paragraph in current selection levels");
			}

			int wsStart = GetCurrentBtWs(SelectionHelper.SelLimitType.Top);
			int wsEnd = GetCurrentBtWs(SelectionHelper.SelLimitType.Bottom);

			ScrReference[] startScrRef = GetCurrentRefRange(CurrentSelection, SelectionHelper.SelLimitType.Top);
			ScrReference[] endScrRef = GetCurrentRefRange(CurrentSelection, SelectionHelper.SelLimitType.Bottom);

			if (wsStart == wsEnd && beginObj == endObj)
			{
				// The selection range does not include more than one para or type of translation.
				wsSelector = wsStart;
				tssQuote = GetCleanSelectedText(out startOffset, out endOffset);
			}
			else
			{
				wsSelector = -1;
				startOffset = 0;
				endOffset = 0;
				tssQuote = null;
			}

			startRef = startScrRef[0];
			endRef = endScrRef[1];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the cleaned-up selected text (no ORCS, chapter/verse numbers, or
		/// leading/trailing space).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsString GetCleanSelectedText()
		{
			int startOffset, endOffset;
			return GetCleanSelectedText(out startOffset, out endOffset);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the cleaned-up selected text (no ORCS, chapter/verse numbers, or
		/// leading/trailing space).
		/// </summary>
		/// <param name="startOffset">The start offset.</param>
		/// <param name="endOffset">The end offset.</param>
		/// ------------------------------------------------------------------------------------
		private ITsString GetCleanSelectedText(out int startOffset, out int endOffset)
		{
			if (CurrentSelection == null)
			{
				Debug.Fail("Should not have null selection.");
				startOffset = endOffset = -1;
				return null;
			}
			startOffset = CurrentSelection.GetIch(SelectionHelper.SelLimitType.Top);
			endOffset = CurrentSelection.GetIch(SelectionHelper.SelLimitType.Bottom);
			if (CurrentSelection.IsRange)
			{
				ITsString tssQuote;
				CurrentSelection.Selection.GetSelectionString(out tssQuote, string.Empty);
				return TsStringUtils.GetCleanTsString(tssQuote, ScrStyleNames.ChapterAndVerse);
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempts to find a complete word the selection corresponds to. If it is a range,
		/// then the first word in the range is returned. Before the word is returned, it
		/// is cleaned of ORCs, chapter/verse numbers, and leading/trailing spaces.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CleanSelectedWord
		{
			get
			{
				if (CurrentSelection == null || CurrentSelection.SelectedWord == null)
					return null;

				ITsString tssWord = TsStringUtils.GetCleanTsString(CurrentSelection.SelectedWord,
					ScrStyleNames.ChapterAndVerse);
				return (tssWord == null ? null : tssWord.Text);
			}
		}

		#endregion

		#region "Focus-sharing" methods (for synchronizing with Paratext, TW, etc.)
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Syncs other windows/listeners to the current Scripture reference location.
		/// </summary>
		/// <param name="fInternalOnly">if set to <c>true</c> does not send reference to
		/// third-party listeners.</param>
		/// ------------------------------------------------------------------------------------
		private void SyncToScrLocation(bool fInternalOnly)
		{
			IScrRefTracker tracker = TheMainWnd as IScrRefTracker;
			if (tracker != null)
				tracker.SyncToScrLocation(this, fInternalOnly);
		}
		#endregion

		#region Information Bar methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Uses current selection to update information bar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SetInformationBarForSelection()
		{
			CheckDisposed();

			int tagSelection;
			int hvoSelection;
			if (GetSelectedScrElement(out tagSelection, out hvoSelection))
				SetInformationBarForSelection(tagSelection, hvoSelection);
			else if (TheMainWnd != null)
				TheMainWnd.InformationBarText = string.Empty;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Update the text in a main window's caption bar based on the selection. If the
		/// selection is a scripture passage, the BCV reference is appended.
		/// </summary>
		/// <param name="tag">The flid of the selected element</param>
		/// <param name="hvoSel">The hvo of the selected element</param>
		/// -----------------------------------------------------------------------------------
		public void SetInformationBarForSelection(int tag, int hvoSel)
		{
			CheckDisposed();

			if (TheMainWnd == null || TheClientWnd == null)
				return;

			string sEditRef = null;
			try
			{
				sEditRef = GetPassageAsString(tag, hvoSel);
			}
			catch { }

			if (sEditRef == null)
				TheMainWnd.InformationBarText = TheClientWnd.BaseInfoBarCaption;
			else
			{
				TheMainWnd.InformationBarText = string.Format(
					TeResourceHelper.GetResourceString("kstidCaptionWRefFormat"),
					TheClientWnd.BaseInfoBarCaption, sEditRef);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use to update the GoTo Reference control that is part of the
		/// information toolbar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void UpdateGotoPassageControl()
		{
			CheckDisposed();

			if (TheMainWnd == null || TheMainWnd.Mediator == null)
				return;

			if (BookIndex != -1)
			{
				try
				{
					ScrReference currStartRef = CurrentStartRef;
					if (currStartRef.Verse < 1)
						currStartRef.Verse = 1;
					if (currStartRef.Chapter < 1)
						currStartRef.Chapter = 1;

					TheMainWnd.Mediator.SendMessage("SelectedPassageChanged", currStartRef);
				}
				catch
				{
					// You can't update the GotoReferenceControl properly if there's no text
					// hanging around to make a selection.  Yeah.
				}
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get a string that describes the Scripture passage based on the selection.
		/// </summary>
		/// <param name="tag">The flid of the selected element</param>
		/// <param name="hvoSel">The hvo of the selected element, either a ScrSection (usually)
		/// or ScrBook (if in a title)</param>
		/// <returns>String that describes the Scripture passage or null if the selection
		/// can't be interpreted as a book and/or section reference.</returns>
		/// -----------------------------------------------------------------------------------
		public string GetPassageAsString(int tag, int hvoSel)
		{
			CheckDisposed();

			return GetPassageAsString(tag, hvoSel, false);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get a string that describes the Scripture passage based on the selection.
		/// </summary>
		/// <param name="tag">The flid of the selected element</param>
		/// <param name="hvoSel">The hvo of the selected element, either a ScrSection (usually)
		/// or ScrBook (if in a title)</param>
		/// <param name="fSimpleFormat">Gets a simple, standardized reference (uses SIL 3-letter
		/// codes and no verse bridges)</param>
		/// <returns>String that describes the Scripture passage or null if the selection
		/// can't be interpreted as a book and/or section reference.</returns>
		/// -----------------------------------------------------------------------------------
		public virtual string GetPassageAsString(int tag, int hvoSel, bool fSimpleFormat)
		{
			CheckDisposed();

			if (m_cache == null)
				return null;

			string sEditRef = null; // Title/reference/etc of text being edited in the draft pane
			switch (tag)
			{
				case ScrSectionTags.kflidHeading:
				case ScrSectionTags.kflidContent:
					{
						// ENHANCE TomB: might want to use low-level methods to get
						// cached values directly instead of creating the FDO objects
						// and having them reread the info from the DB. Also, may want
						// to cache the hvoSel in a method static variable so that when
						// the selection hasn't really changed to a new section or book,
						// we stop here.
						IScrSection section = m_repoScrSection.GetObject(hvoSel);
						IScrBook book = (IScrBook)section.Owner;
						BCVRef startRef;
						BCVRef endRef;
						section.GetDisplayRefs(out startRef, out endRef);
						if (fSimpleFormat)
							sEditRef = startRef.AsString;
						else
						{
							sEditRef = ScrReference.MakeReferenceString(book.BestUIName,
								startRef, endRef, m_scr.ChapterVerseSepr, m_scr.Bridge);
						}
						break;
					}
				case ScrBookTags.kflidTitle:
					{
						IScrBook book = m_repoScrBook.GetObject(hvoSel);
						sEditRef = (fSimpleFormat ? (book.BookId + " 0:0") : book.BestUIName);
						break;
					}
				default:
					return null;
			}

			// Add the back translation writing system info to the output string, if needed
			if (IsBackTranslation)
			{
				IWritingSystem ws = m_cache.ServiceLocator.WritingSystemManager.Get(ViewConstructorWS);
				sEditRef = string.Format(
					TeResourceHelper.GetResourceString("kstidCaptionInBackTrans"),
					sEditRef, ws.DisplayLabel);
			}

			return (sEditRef != null && sEditRef.Length != 0) ? sEditRef : null;
		}
		#endregion

		#region Back Translation methods

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the current section and generates a template for the back translation with
		/// corresponding chapter or verse numbers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void GenerateTranslationCVNumsForSection()
		{
			CheckDisposed();

			Debug.Assert(BookIndex != -1);
			Debug.Assert(SectionIndex != -1);

			IScrBook book = BookFilter.GetBook(BookIndex);
			IScrSection section = book[SectionIndex];
			int wsTrans = ViewConstructor as StVc != null ?
				((StVc)ViewConstructor).BackTranslationWS : Cache.DefaultAnalWs;

			foreach (IStTxtPara para in section.ContentOA.ParagraphsOS)
			{
				ICmTranslation trans = para.GetOrCreateBT();

				// If there is already text in the translation then skip this para
				if (trans.Translation.get_String(wsTrans).Text != null)
					continue;

				// create the Chapter-Verse template in the translation
				ITsString tssTrans = GenerateTranslationCVNums(para.Contents, wsTrans);
				if (tssTrans != null)
					trans.Translation.set_String(wsTrans, tssTrans);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given an ITsString from a scripture paragraph, generate matching chapter and verse
		/// numbers in an ITsString for the translation.
		/// </summary>
		/// <param name="tssParentPara">content of the parent scripture paragraph</param>
		/// <param name="wsTrans">writing system of the translation</param>
		/// <returns>ITsString for the translation, or null if there are no chapter or verse
		/// numbers in the parent paragraph</returns>
		/// ------------------------------------------------------------------------------------
		public ITsString GenerateTranslationCVNums(ITsString tssParentPara, int wsTrans)
		{
			CheckDisposed();

			ITsStrBldr strBldr = TsStrBldrClass.Create();
			bool PrevRunIsVerseNumber = false;

			for (int iRun = 0; iRun < tssParentPara.RunCount; iRun++)
			{
				string styleName = tssParentPara.get_Properties(iRun).GetStrPropValue(
					(int)FwTextPropType.ktptNamedStyle);

				if (styleName == ScrStyleNames.ChapterNumber ||
					styleName == ScrStyleNames.VerseNumber)
				{
					if (styleName == ScrStyleNames.VerseNumber)
					{
						// Previous token is verse number, add space with default paragraph style
						if (PrevRunIsVerseNumber)
							AddRunToStrBldr(strBldr, " ", wsTrans, null);

						PrevRunIsVerseNumber = true;
					}
					else
						PrevRunIsVerseNumber = false;

					string number = m_scr.ConvertVerseChapterNumForBT(tssParentPara.get_RunText(iRun));

					// Add corresponding chapter or verse number to string builder
					AddRunToStrBldr(strBldr, number, wsTrans, styleName);
				}
			}

			// return null if there are no chapter or verse numbers
			if (strBldr.Length == 0)
				return null;

			return strBldr.GetString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="vernVerseChapterText">Verse or chapter number text. This may be
		/// <c>null</c>.</param>
		/// <param name="scr"></param>
		/// <returns>The converted verse or chapter number, or empty string if
		/// <paramref name="vernVerseChapterText"/> is <c>null</c>.</returns>
		/// ------------------------------------------------------------------------------------
		public static string ConvertVerseChapterNumForBT(string vernVerseChapterText, IScripture scr)
		{
			if (vernVerseChapterText == null)
				return string.Empty; // empty verse/chapter number run.

			StringBuilder btVerseChapterText = new StringBuilder(vernVerseChapterText.Length);
			char baseChar = scr.UseScriptDigits ? (char)scr.ScriptDigitZero : '0';
			for (int i = 0; i < vernVerseChapterText.Length; i++)
			{
				btVerseChapterText.Append(char.IsDigit(vernVerseChapterText[i]) ?
					(char)('0' + (vernVerseChapterText[i] - baseChar)) : vernVerseChapterText[i]);
			}

			return btVerseChapterText.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper function appends a run to the given string builder
		/// </summary>
		/// <param name="strBldr"></param>
		/// <param name="text"></param>
		/// <param name="ws"></param>
		/// <param name="charStyle"></param>
		/// ------------------------------------------------------------------------------------
		private void AddRunToStrBldr(ITsStrBldr strBldr, string text, int ws, string charStyle)
		{
			strBldr.Replace(strBldr.Length, strBldr.Length, text,
				StyleUtils.CharStyleTextProps(charStyle, ws));
		}
		#endregion
	}

	#region Message filter class for inserting verses
	/// ------------------------------------------------------------------------------------
	/// <summary>Message filter for detecting events that may turn off
	/// the insert verse numbers mode</summary>
	/// ------------------------------------------------------------------------------------
	public class InsertVerseMessageFilter : IMessageFilter
	{
		/// <summary></summary>
		private Control m_control;
		private TeEditingHelper m_helper;
		private int m_cMessageTimeBomb;
		private bool m_fCanEndInsertVerseNumbers = true;
		private bool m_fInsertVerseInProgress = false;

		/// <summary>Constructor for filter object</summary>
		public InsertVerseMessageFilter(Control ctrl, TeEditingHelper helper)
		{
			m_control = ctrl;
			m_helper = helper;
			m_cMessageTimeBomb = 0;
		}

		/// --------------------------------------------------------------------------------
		/// <summary></summary>
		/// <param name="m">message to be filtered</param>
		/// <returns>true if the message is consumed, false to pass it on.</returns>
		/// --------------------------------------------------------------------------------
		public bool PreFilterMessage(ref Message m)
		{
			const int WM_TURNOFFINSERTVERSE = (int)Win32.WinMsgs.WM_APP + 0x123;
			const int CHAR_ESC = 0x001b;
			const int HTMENU = 0x05;

			//Debug.WriteLine("Msg: " + (Win32.WinMsgs)m.Msg);

			switch ((Win32.WinMsgs)m.Msg)
			{
				case Win32.WinMsgs.WM_SYSKEYDOWN:
				{
					// WM_SYSKEYDOWN will result from a system key that pops up a menu.
					// Popping up a menu will exit insert verse numbers mode.
					m_helper.EndInsertVerseNumbers();
					return false;
				}
				case Win32.WinMsgs.WM_COMMAND:
				{
					// WM_COMMAND results from clicking in the menu bar and selecting a menu
					// item. We need to catch WM_COMMAND rather than exiting insert verse
					// number mode on a WM_NCLCLICK so that we can stay in the mode on an
					// undo.
					//Debug.WriteLine("WM_COMMAND");
					Win32.PostMessage(m_control.Handle, WM_TURNOFFINSERTVERSE, 0, 0);
					return false;
				}
				case Win32.WinMsgs.WM_NCLBUTTONDOWN:
				case Win32.WinMsgs.WM_NCLBUTTONUP:
				{
					// Handle any non-client mouse left button activity.
					// Non-client areas include the title bar, menu bar, window borders,
					// and scroll bars.
					// Debug.WriteLine("NC LClick; hit-test value:" + m.WParam);
					if (m.WParam.ToInt32() == HTMENU)
					{
						// user clicked menu - handled when WM_COMMAND comes
						return false;
					}
					Control c = Control.FromHandle(m.HWnd);
					// Clicking on the scroll bar in the draft view should NOT exit verse
					// insert mode. Clicking on other non-client areas will exit the insert
					// verse numbers mode.
					if (c != m_control)
						m_helper.EndInsertVerseNumbers();
					return false;
				}
				case Win32.WinMsgs.WM_LBUTTONDOWN:
				case Win32.WinMsgs.WM_LBUTTONUP:
				{
					// Handle client area LEFT mouse click activity
					// If a down click is detected outside the draft view, then wait for the
					// up click message.  When the second message comes in then post a
					// message to turn off the insert verse mode.  The PostMessage will
					// allow the insert verse toolbar button a chance to handle the click
					// before turning off insert verse mode.
					//Debug.WriteLine("LClick");
					Control c = Control.FromHandle(m.HWnd);
					if (c != m_control)
					{
						if (++m_cMessageTimeBomb > 1)
						{
							// Debug.WriteLine("Time Bomb inc: " + m_cMessageTimeBomb);
							Win32.PostMessage(m_control.Handle, WM_TURNOFFINSERTVERSE, 0, 0);
						}
					}
					else if (m_cMessageTimeBomb > 0)
					{
						// Any second mouse click will turn off insert mode.  This can happen if the down
						// click was outside the draft view but the up click was in the draft view.
						m_helper.EndInsertVerseNumbers();
					}
					else if ((Win32.WinMsgs)m.Msg == Win32.WinMsgs.WM_LBUTTONUP && InsertVerseInProgress)
					{
						// Left Button up ends the current verse insertion
						InsertVerseInProgress = false;
					}
					return false;
				}
				case Win32.WinMsgs.WM_KEYDOWN:
				{
					// Handle key presses.
					//Debug.WriteLine("KeyDown: VK: " + ((int)m.WParam).ToString("x"));
					switch((int)m.WParam)
					{
							// Navigation keys will be passed through to allow
							// keyboard navigation during verse insert mode.
						case (int)Win32.VirtualKeycodes.VK_CONTROL:
						case (int)Win32.VirtualKeycodes.VK_PRIOR:
						case (int)Win32.VirtualKeycodes.VK_NEXT:
						case (int)Win32.VirtualKeycodes.VK_END:
						case (int)Win32.VirtualKeycodes.VK_HOME:
						case (int)Win32.VirtualKeycodes.VK_LEFT:
						case (int)Win32.VirtualKeycodes.VK_UP:
						case (int)Win32.VirtualKeycodes.VK_RIGHT:
						case (int)Win32.VirtualKeycodes.VK_DOWN:
							return false;

							// the escape key will be passed on and consumed in the WM_CHAR message
						case (int)Win32.VirtualKeycodes.VK_ESCAPE:
							return false;

							// other keys will terminate the insert verse mode
						default:
							Win32.PostMessage(m_control.Handle, WM_TURNOFFINSERTVERSE, 0, 0);
							return false;
					}
				}
				case Win32.WinMsgs.WM_CHAR:
				{
					// Handle WM_CHAR messages.  We only handle the ESC key at this point.
					//Debug.WriteLine("WM_CHAR: " + ((int)m.WParam).ToString("x"));
					m_helper.EndInsertVerseNumbers();
					// consume an escape key
					if ((int)m.WParam == CHAR_ESC)
						return true;

					// let all other characters pass through
					return false;
				}
				case Win32.WinMsgs.WM_RBUTTONDOWN:
				case Win32.WinMsgs.WM_RBUTTONUP:
				case Win32.WinMsgs.WM_RBUTTONDBLCLK:
				case Win32.WinMsgs.WM_MBUTTONDOWN:
				case Win32.WinMsgs.WM_MBUTTONUP:
				case Win32.WinMsgs.WM_NCRBUTTONDOWN:
				case Win32.WinMsgs.WM_NCMBUTTONUP:
					// Handle right or middle button mouse clicks in or out of the draft
					// view. This will end insert verse mode.
					m_helper.EndInsertVerseNumbers();
					return false;
				case Win32.WinMsgs.WM_MOUSEMOVE:
					// Mouse movements will be ignored if verse insert is in progress.  This
					// will prevent selections from being created
					return InsertVerseInProgress;
				default:
				{
					if (m.Msg == WM_TURNOFFINSERTVERSE)
					{
						// Handle the message that was posted to turn off insert verse mode.
						if (m_fCanEndInsertVerseNumbers)
							m_helper.EndInsertVerseNumbers();
						else
							m_fCanEndInsertVerseNumbers = true;
					}
					return false;
				}
			}
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Prevents ending the insert verse numbers mode. This gets called
		/// if user clicks on Undo button and prevents exit from InsertVerseMode.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public void PreventEndInsertVerseNumbers()
		{
			m_fCanEndInsertVerseNumbers = false;
			m_cMessageTimeBomb = 0;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Property indicating that verse number insert is in progress.  Mouse movements
		/// will be ignored so that selection is not created.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public bool InsertVerseInProgress
		{
			get {return m_fInsertVerseInProgress;}
			set {m_fInsertVerseInProgress = value;}
		}
	}
	#endregion

	/// --------------------------------------------------------------------------------
	/// <summary>
	/// Class for enabling the dialog to change multiple mis-spelled words to be invoked.
	/// </summary>
	/// --------------------------------------------------------------------------------
	public class ChangeSpellingInfo
	{
		private readonly string m_word; // the word(form) we want to change occurrences of.
		private readonly IWritingSystem m_ws;
		private readonly FdoCache m_cache;
		private readonly IApp m_app;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make one.
		/// </summary>
		/// <param name="word">The word.</param>
		/// <param name="ws">The ws.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="app">The app.</param>
		/// ------------------------------------------------------------------------------------
		public ChangeSpellingInfo(string word, int ws, FdoCache cache, IApp app)
		{
			m_word = word;
			m_cache = cache;
			m_ws = m_cache.ServiceLocator.WritingSystemManager.Get(ws);
			m_app = app;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Execute the command...that is, run the change spelling dialog on the specified wordform.
		/// </summary>
		/// <param name="site">The rootsite.</param>
		/// ------------------------------------------------------------------------------------
		public void DoIt(IVwRootSite site)
		{
			IWfiWordform wf = null;
			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
			{
				wf = WfiWordformServices.FindOrCreateWordform(m_cache, m_word, m_ws);
			});
			// Do NOT make un Undo action; the respeller Dialog makes its own, if needed.
			object respellerDlg = ReflectionHelper.CreateObject("MorphologyEditorDll.dll",
				"SIL.FieldWorks.XWorks.MorphologyEditor.RespellerDlg", new object[0]);
			try
			{
				ReflectionHelper.CallMethod(respellerDlg, "SetDlgInfo", new object[] { wf, m_app.ActiveMainWindow, m_app });
				ReflectionHelper.CallMethod(respellerDlg, "ShowDialog", new object[] { });
				ReflectionHelper.CallMethod(respellerDlg, "SaveSettings", new object[] { });
			}
			finally
			{
				ReflectionHelper.CallMethod(respellerDlg, "Dispose", new object[] { });
			}
		}
	}
}
