// Copyright (c) 2005-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DiffFootnoteView.cs
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
	/// Summary description for DiffFootnoteView.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DiffFootnoteView : FwRootSite, ISelectableView
	{
		#region Data members
		private DiffFootnoteVc m_diffFootnoteVc;
		private IScrBook m_scrBook;
		private IApp m_app;
		private DifferenceList m_Differences;
		private bool m_fRev;
		private int m_filterInstance;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the DiffFootnoteView class
		/// </summary>
		/// <param name="cache">The FDO cache.</param>
		/// <param name="book">Scripture book to be displayed as the root in this view</param>
		/// <param name="differences">the list of differences</param>
		/// <param name="fRev"><c>true</c> if we display the revision, <c>false</c> if we
		/// display the current version.</param>
		/// <param name="filterInstance">The filter instance.</param>
		/// <param name="app">The app.</param>
		/// ------------------------------------------------------------------------------------
		public DiffFootnoteView(FdoCache cache, IScrBook book, DifferenceList differences,
			bool fRev, int filterInstance, IApp app) : base(cache)
		{
			m_filterInstance = filterInstance;
			m_app = app;
			m_scrBook = book;
			m_Differences = differences;
			m_fRev = fRev;
			Editable = false;
		}
		#endregion

		#region Overrides of Control methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <param name="charCode"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override bool IsInputChar(char charCode)
		{
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the client size changed we have to recalculate the average paragraph height
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);

			SelectionHelper selHelper = EditingHelper.CurrentSelection;
			if (selHelper == null)
				return;

			//int paraIndex = selHelper.GetLevelForTag((int)StText.StTextTags.kflidParagraphs);
			//int hvoPara = selHelper.LevelInfo[paraIndex].hvo;
			//if (m_fdoCache.IsRealObject(hvoPara, StTxtPara.kClassId))
			//{
			//    // don't attempt a prop change for an empty paragraph
			//    StTxtPara para = new StTxtPara(m_fdoCache, hvoPara);
			//    if (para.Contents.Length != 0)
			//    {
			//        m_fdoCache.PropChanged(null, PropChangeType.kpctNotifyAll, hvoPara,
			//            (int)StTxtPara.StParaTags.kflidStyleRules, 0, 1, 1);
			//    }
			//}
			selHelper.SetSelection(this, true, true);
		}
		#endregion

		#region Public Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scrolls a footnote paragraph into view.
		/// </summary>
		/// <param name="diff">The sub-diff, which contains the id of the paragraph in the
		/// footnote which is to be scrolled into view. Or null if no footnote is specified.</param>
		/// ------------------------------------------------------------------------------------
		public void ScrollToFootnotePara(Difference diff)
		{
			CheckDisposed();

			// TODO:  TE-4610 temporary? ignore scrolling when no footnote in this diff
			//Debug.Assert(diff != null);
			if (diff == null)
				return;

			// Get the paragraph which contains the difference.
			IScrTxtPara para = diff.ParaCurr != null ? diff.ParaCurr : diff.ParaRev;

			// Get the footnote which contains the paragraph.
			IScrFootnote footnote = (IScrFootnote)para.Owner;

			// Find the index of the paragraph.
			int iPara = para.IndexInOwner;
			if (iPara < 0)
				return;

			// Find index of the footnote.
			int iFootnote = footnote.IndexInOwner;
			if (iFootnote < 0)
				return;

			// Create selection pointing to this footnote.
			SelectionHelper selHelper = new SelectionHelper();
			selHelper.AssocPrev = false;
			selHelper.NumberOfLevels = 2;
			selHelper.TextPropId = StTxtParaTags.kflidContents;
			selHelper.LevelInfo[0].tag = StTextTags.kflidParagraphs;
			selHelper.LevelInfo[0].ihvo = iPara;
			selHelper.LevelInfo[1].tag = ScrBookTags.kflidFootnotes;
			selHelper.LevelInfo[1].ihvo = iFootnote;
			// note: We don't care about seting the ich stuff for this temporary selection.
			// (The view constructor takes care of highlighting the proper ich range.)
			// Our mission here is only to scroll to the paragraph.
			selHelper.IchAnchor = 0;
			selHelper.IchEnd = 0;

			// Set the selection.
			selHelper.SetSelection(this, true, true);
			ScrollSelectionIntoView(null, VwScrollSelOpts.kssoNearTop);
		}
		#endregion

		#region Overrides of RootSite
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

			// Check for non-existing rootbox before creating a new one. By doing that we
			// can mock the rootbox in our tests. However, this might cause problems in our
			// real code - altough I think MakeRoot() should be called only once.
			if (m_rootb == null)
				m_rootb = VwRootBoxClass.Create();

			m_rootb.SetSite(this);

			// Set up a new view constructor.
			m_diffFootnoteVc = new DiffFootnoteVc(m_Differences, m_fRev, m_fdoCache);

			m_rootb.DataAccess = m_fdoCache.MainCacheAccessor;

			m_rootb.SetRootObject(m_scrBook.Hvo, m_diffFootnoteVc, (int)FootnoteFrags.kfrBook,
				m_styleSheet);

			base.MakeRoot();
			m_dxdLayoutWidth = kForceLayout; // Don't try to draw until we get OnSize and do layout.

			Synchronize(m_rootb);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provide a TE diff footnote-specific implementation of the EditingHelper
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override EditingHelper CreateEditingHelper()
		{
			Debug.Assert(Cache != null);
			var editingHelper = new FootnoteEditingHelper(this, Cache, m_filterInstance,
				null, TeViewType.FootnoteView | TeViewType.Horizontal | TeViewType.Scripture, m_app);
			editingHelper.InternalContext = ContextValues.Note;
			return editingHelper;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Don't allow user to paste wacky stuff in the footnote pane.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override VwInsertDiffParaResponse OnInsertDiffParas(IVwRootBox prootb,
			ITsTextProps ttpDest, int cPara, ITsTextProps[] ttpSrc, ITsString[] tssParas,
			ITsString tssTrailing)
		{
			CheckDisposed();

			return VwInsertDiffParaResponse.kidprFail;
		}

		/// <summary> see OnInsertDiffParas </summary>
		public override VwInsertDiffParaResponse OnInsertDiffPara(IVwRootBox prootb,
			ITsTextProps ttpDest, ITsTextProps ttpSrc, ITsString tssParas,
			ITsString tssTrailing)
		{
			CheckDisposed();

			return VwInsertDiffParaResponse.kidprFail;
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get/set the editable state of the view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool Editable
		{
			get
			{
				CheckDisposed();
				return EditingHelper.Editable;
			}
			set
			{
				CheckDisposed();

				EditingHelper.Editable = value;
				if (m_diffFootnoteVc != null)
					m_diffFootnoteVc.NeedHighlight = !EditingHelper.Editable;
				BackColor = value ? SystemColors.Window : TeResourceHelper.NonEditableColor;
			}
		}
		#endregion

		#region ISelectableView implementation
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Activates the view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual void ActivateView()
		{
			CheckDisposed();

			PerformLayout();
			Show();
			Focus();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string BaseInfoBarCaption
		{
			get
			{
				CheckDisposed();
				return null;
			}
			set
			{
				CheckDisposed();
				/* do nothing */
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void DeactivateView()
		{
			CheckDisposed();

			Hide();
		}
		#endregion
	}
}
