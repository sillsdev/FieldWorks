// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DraftStyleBar.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DraftStyleBar : RootSite
	{
		#region Member variables
		DraftStyleBarVc m_styleBarVc;
		private bool m_displayForFootnotes;
		private int m_filterInstance;
		private int m_prevPara1Hvo;
		private int m_prevPara2Hvo;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructs a style pane for a regular (non-footnote) draft view
		/// </summary>
		/// <param name="cache">The cache</param>
		/// <param name="filterInstance"></param>
		/// ------------------------------------------------------------------------------------
		public DraftStyleBar(FdoCache cache, int filterInstance)
			: this(cache, false, filterInstance)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructs a style pane
		/// </summary>
		/// <param name="cache">The cache</param>
		/// <param name="displayForFootnotes">True to display styles of the footnote paragraphs,
		/// false to display styles of scripture paragraphs</param>
		/// <param name="filterInstance"></param>
		/// ------------------------------------------------------------------------------------
		public DraftStyleBar(FdoCache cache, bool displayForFootnotes, int filterInstance)
			: base(cache)
		{
			// the entire view is read-only so let the view know.
			ReadOnlyView = true;
			m_displayForFootnotes = displayForFootnotes;
			m_filterInstance = filterInstance;
			BackColor = SystemColors.Window;
			EditingHelper.DefaultCursor = TeResourceHelper.RightCursor;
		}
		#endregion

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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose(disposing);

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_styleBarVc != null)
					m_styleBarVc.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_styleBarVc = null;
		}

		#endregion IDisposable override

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets value of filter instance - used to make filters unique per main window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int FilterInstance
		{
			get
			{
				CheckDisposed();
				return m_filterInstance;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the draft view that should accept a selection when made in the
		/// style bar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int DraftViewSlaveIndex
		{
			get
			{
				Debug.Assert(Group != null && Group.Slaves.Count >= 2);
				Debug.Assert(this == Group.Slaves[0], "We assume the style bar is the first slave");
				// if we are in the back translation split view and are displaying right-to-left
				// text, then the back translation and front translation are switched.
				if (Group.Slaves.Count == 3 && m_styleBarVc.RightToLeft)
					return 2;
				return 1;
			}
		}
		#endregion

		#region Overrides of RootSite
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We override this method to select the corresponding paragraph in the draft or
		/// footnote view.
		/// </summary>
		/// <param name="prootb"></param>
		/// <param name="vwselNew"></param>
		/// ------------------------------------------------------------------------------------
		protected override void HandleSelectionChange(IVwRootBox prootb, IVwSelection vwselNew)
		{
			CheckDisposed();

			base.HandleSelectionChange(prootb, vwselNew);
			if (s_fInSelectionChanged)
				return;

			SelectionHelper origSelection = SelectionHelper.Create(vwselNew, this);
			if (origSelection != null && Group.Slaves[DraftViewSlaveIndex] is IVwRootSite)
			{
				IVwRootSite view = (IVwRootSite)Group.Slaves[DraftViewSlaveIndex];
				SelectAssociatedPara(origSelection, view);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new selection restorer.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override SelectionRestorer CreateSelectionRestorer()
		{
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process mouse button up event
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			int draftViewIndex = DraftViewSlaveIndex;
			if (m_prevPara1Hvo != 0)
			{
				Debug.Assert(m_prevPara2Hvo != 0);
				m_prevPara1Hvo = m_prevPara2Hvo = 0;

				if (Group.Slaves[draftViewIndex] is Control)
				{
					Control view = (Control)Group.Slaves[draftViewIndex];
					view.Focus();
				}
			}

			if (Group.Slaves[draftViewIndex] is SimpleRootSite)
			{
				SimpleRootSite view = (SimpleRootSite)Group.Slaves[draftViewIndex];
				view.ShowRangeSelAfterLostFocus = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process left or right mouse button down
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);

			if (Group.Slaves[DraftViewSlaveIndex] is SimpleRootSite)
			{
				SimpleRootSite view = (SimpleRootSite)Group.Slaves[DraftViewSlaveIndex];
				view.ShowRangeSelAfterLostFocus = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the average paragraph height for the style pane.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override int AverageParaHeight
		{
			get
			{
				CheckDisposed();
				return (int)(12 * Zoom);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Estimate height in points for the given book or section.
		/// </summary>
		/// <param name="hvo">The hvo of a book or section.</param>
		/// <param name="frag">The frag.</param>
		/// <param name="dxAvailWidth">Width of the dx avail.</param>
		/// ------------------------------------------------------------------------------------
		public override int EstimateHeight(int hvo, int frag, int dxAvailWidth)
		{
			CheckDisposed();

			// Call the EstimateHeight() method on our controller
			if (Group != null)
			{
				Debug.Assert(Group.ScrollingController != this);
				return ((IHeightEstimator)Group.ScrollingController).EstimateHeight(hvo, frag,
					dxAvailWidth);
			}
			return 120;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override bool OnFilePrint(object args)
		{
			CheckDisposed();

			// We want the main window to handle this
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override the zoom (we can't zoom the style bar)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override float Zoom
		{
			get
			{
				CheckDisposed();
				return base.Zoom;
			}
			set
			{
				CheckDisposed();
				/* do nothing */
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We override this because this is basically true (The view should not wrap text so
		/// our width can be as wide as we want it)
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override int GetAvailWidth(IVwRootBox prootb)
		{
			CheckDisposed();

			return 1600;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make the root box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void MakeRoot()
		{
			CheckDisposed();

			if (m_fdoCache == null || DesignMode)
				return;

			Debug.Assert(Group != null && Group.ScrollingController != null);

			bool fSiblingEditable = ((DraftViewBase)Group.ScrollingController).InitialEditableState;
			BackColor = fSiblingEditable ? SystemColors.Window : TeResourceHelper.NonEditableColor;

			// This is muy importante. If a spurious rootbox got made already and we just replace
			// it with a new one, the old one will fire an assertion in its destructor.
			// REVIEW: Is this okay? Is there any way to prevent early creation of rootboxes?
			Debug.Assert(m_rootb == null, "Rootbox should be null");
			if (m_rootb == null)
				m_rootb = VwRootBoxClass.Create();

			m_rootb.SetSite(this);

			// Set up a new view constructor.
			m_styleBarVc = new DraftStyleBarVc(FilterInstance, m_displayForFootnotes);
			// Stylenames
			m_styleBarVc.DefaultWs = m_fdoCache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en");
			m_styleBarVc.Cache = m_fdoCache;
			m_styleBarVc.BackColor = BackColor;
			m_styleBarVc.HeightEstimator = Group as IHeightEstimator;

			m_rootb.DataAccess = new ScrBookFilterDecorator(m_fdoCache, m_filterInstance);
			m_rootb.SetRootObject(m_fdoCache.LangProject.TranslatedScriptureOA.Hvo,
				m_styleBarVc,
				m_displayForFootnotes ? (int)FootnoteFrags.kfrScripture : (int)ScrFrags.kfrScripture,
				null);

			//TODO:
			//ptmw->RegisterRootBox(qrootb);

			base.MakeRoot();
			m_dxdLayoutWidth = kForceLayout; // Don't try to draw until we get OnSize and do layout.

			Synchronize(m_rootb);
		}
		#endregion

		#region Private helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Selects the paragraph in the DraftView or FootnoteView associated with the style.
		/// </summary>
		/// <param name="origSelection">selection user originally made</param>
		/// <param name="view">The DraftView or FootnoteView</param>
		/// ------------------------------------------------------------------------------------
		private void SelectAssociatedPara(SelectionHelper origSelection, IVwRootSite view)
		{
			SelLevInfo paraInfoAnchor = origSelection.GetLevelInfoForTag(
				StTextTags.kflidParagraphs, SelectionHelper.SelLimitType.Top);
			SelLevInfo paraInfoEnd = origSelection.GetLevelInfoForTag(
				StTextTags.kflidParagraphs, SelectionHelper.SelLimitType.Bottom);

			if (paraInfoAnchor.hvo != m_prevPara1Hvo || paraInfoEnd.hvo != m_prevPara2Hvo)
			{
				// The selection changed paragraphs, so update the selection in the
				// DraftView or FootnoteView
				IStTxtPara para1 = m_fdoCache.ServiceLocator.GetInstance<IStTxtParaRepository>().GetObject(paraInfoAnchor.hvo);
				IStTxtPara para2 = m_fdoCache.ServiceLocator.GetInstance<IStTxtParaRepository>().GetObject(paraInfoEnd.hvo);
				SelectionHelper helper = MakeSelection(para1, para2, view);
				IVwSelection sel = helper.SetSelection(view, true, false);
				// If the selection fails then try selecting the user prompt.
				if (sel == null)
				{
					AdjustSelectionForPrompt(helper, para1, para2);
					sel = helper.SetSelection(view, true, false);
				}

				Debug.Assert(sel != null || ((SimpleRootSite)view).ReadOnlyView);
				m_prevPara1Hvo = paraInfoAnchor.hvo;
				m_prevPara2Hvo = paraInfoEnd.hvo;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the both the anchor and the end of the specified selection. If its located in
		/// a book title or a section head, then it sets the properties to a user prompt.
		/// </summary>
		/// <param name="helper">The selection</param>
		/// <param name="paraAnchor">The paragraph at the anchor.</param>
		/// <param name="paraEnd">The paragraph at the end.</param>
		/// ------------------------------------------------------------------------------------
		private void AdjustSelectionForPrompt(SelectionHelper helper, IStTxtPara paraAnchor,
			IStTxtPara paraEnd)
		{
			SelectionHelper.SelLimitType limit = SelectionHelper.SelLimitType.Anchor;
			if ((helper.IsFlidInLevelInfo(ScrSectionTags.kflidHeading, limit) ||
				helper.IsFlidInLevelInfo(ScrBookTags.kflidTitle, limit)) &&
				paraAnchor.Contents.Length == 0)
			{
				helper.SetTextPropId(SelectionHelper.SelLimitType.Anchor,
					SimpleRootSite.kTagUserPrompt);
			}

			limit = SelectionHelper.SelLimitType.End;
			if ((helper.IsFlidInLevelInfo(ScrSectionTags.kflidHeading, limit) ||
				helper.IsFlidInLevelInfo(ScrBookTags.kflidTitle, limit)) &&
				paraEnd.Contents.Length == 0)
			{
				helper.SetTextPropId(SelectionHelper.SelLimitType.End,
					SimpleRootSite.kTagUserPrompt);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes a selection from one paragraph to another paragraph.
		/// NOTE: This selects the entirety of both paragraphs.
		/// </summary>
		/// <param name="para1">The first paragraph.</param>
		/// <param name="para2">The last paragraph (could be same as first).</param>
		/// <param name="view">DraftView or FootnoteView</param>
		/// ------------------------------------------------------------------------------------
		private SelectionHelper MakeSelection(IStTxtPara para1, IStTxtPara para2, IVwRootSite view)
		{
			Debug.Assert(view != null);
			Debug.Assert(para1.OwningFlid == StTextTags.kflidParagraphs);
			Debug.Assert(para2.OwningFlid == StTextTags.kflidParagraphs);

			SelectionHelper helper = new SelectionHelper();
			IStText text1 = (IStText)para1.Owner;
			IStText text2 = (IStText)para2.Owner;

			// build the anchor part of the selection
			SetupSelectionFor(helper, text1, para1, SelectionHelper.SelLimitType.Anchor,
				(FwRootSite)view);

			// build the end part of the selection
			SetupSelectionFor(helper, text2, para2, SelectionHelper.SelLimitType.End,
				(FwRootSite)view);
			helper.IchAnchor = 0;
			helper.IchEnd = para2.Contents.Length;

			return helper;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up the specifed SelectionHelper for the specified paragraph and StText.
		/// </summary>
		/// <param name="helper">The SelectionHelper.</param>
		/// <param name="text">The StText.</param>
		/// <param name="para">The para.</param>
		/// <param name="limit">The limit.</param>
		/// <param name="view">The view</param>
		/// ------------------------------------------------------------------------------------
		private void SetupSelectionFor(SelectionHelper helper, IStText text, IStTxtPara para,
			SelectionHelper.SelLimitType limit, FwRootSite view)
		{
			Debug.Assert((view is DraftView && ((DraftView)view).TeEditingHelper != null) ||
				(view is FootnoteView && ((FootnoteView)view).EditingHelper != null));

			helper.SetTextPropId(limit, StTxtParaTags.kflidContents);

			if (view is DraftView)
			{
				DraftView draftView = (DraftView)view;

				if ((text.OwningFlid == ScrSectionTags.kflidContent ||
					text.OwningFlid == ScrSectionTags.kflidHeading))
				{
					// text belongs to section heading or contents
					IScrSection section = (IScrSection)text.Owner;
					Debug.Assert(section.OwningFlid == ScrBookTags.kflidSections);

					helper.SetNumberOfLevels(limit, 4);
					SelLevInfo[] info = helper.GetLevelInfo(limit);
					info[0].ihvo = para.IndexInOwner;
					info[0].tag = StTextTags.kflidParagraphs;
					info[1].ihvo = 0;
					info[1].tag = text.OwningFlid;
					info[2].ihvo = section.IndexInOwner;
					info[2].tag = ScrBookTags.kflidSections;
					info[3].ihvo = draftView.TeEditingHelper.BookFilter.GetBookIndex((IScrBook)section.Owner);
					info[3].tag = draftView.TeEditingHelper.BookFilter.Tag;
				}
				else
				{
					// text belongs to a book title
					Debug.Assert(text.OwningFlid == ScrBookTags.kflidTitle);
					IScrBook book = (IScrBook)text.Owner;

					helper.SetNumberOfLevels(limit, 3);
					SelLevInfo[] info = helper.GetLevelInfo(limit);
					info[0].ihvo = para.IndexInOwner;
					info[0].tag = StTextTags.kflidParagraphs;
					info[1].ihvo = 0;
					info[1].tag = text.OwningFlid;
					info[2].ihvo = draftView.TeEditingHelper.BookFilter.GetBookIndex(book);
					info[2].tag = draftView.TeEditingHelper.BookFilter.Tag;
				}
			}
			else if (view is FootnoteView && text.OwningFlid == ScrBookTags.kflidFootnotes)
			{
				// text belongs to a footnote
				FootnoteView footnoteView = (FootnoteView)view;
				IStFootnote footnote = (IStFootnote)para.Owner;
				IScrBook book = (IScrBook)text.Owner;

				helper.SetNumberOfLevels(limit, 3);
				SelLevInfo[] info = helper.GetLevelInfo(limit);
				info[0].hvo = text.Hvo;
				info[0].tag = StTextTags.kflidParagraphs;
				info[1].hvo = footnote.Hvo;
				info[1].ihvo = footnote.IndexInOwner;
				info[1].tag = ScrBookTags.kflidFootnotes;
				info[2].hvo = book.Hvo;
				info[2].ihvo = footnoteView.BookFilter.GetBookIndex(book);
				info[2].tag = footnoteView.BookFilter.Tag;
				info[0].ich = info[1].ich = info[2].ich = -1;
			}
		}
		#endregion
	}
}
