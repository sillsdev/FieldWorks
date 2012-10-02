// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeBtPrintLayoutSideBySide.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.PrintLayout;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.IText;

namespace SIL.FieldWorks.TE
{
	#region TeBtPrintLayoutConfig class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The configurer for back translation print layout views
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TeBtPrintLayoutConfig : TePrintLayoutConfig
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructs a TePrintLayoutConfig to configure the main print layout
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="styleSheet">The style sheet.</param>
		/// <param name="publication">The publication.</param>
		/// <param name="viewType">Type of the view.</param>
		/// <param name="filterInstance">the book filter instance in effect</param>
		/// <param name="printDateTime">printing date and time</param>
		/// <param name="fIntroDivision">set to <c>true</c> for a division that displays book
		/// title and introduction material, <c>false</c> for a division that displays main
		/// scripture text.</param>
		/// <param name="hvoBook">The hvo of the book.</param>
		/// <param name="sharedStream">A layout stream used for footnotes which is shared across
		/// multiple divisions</param>
		/// <param name="ws">The writing system to use for the back translation</param>
		/// ------------------------------------------------------------------------------------
		public TeBtPrintLayoutConfig(FdoCache cache, IVwStylesheet styleSheet,
			IPublication publication, TeViewType viewType, int filterInstance,
			DateTime printDateTime, bool fIntroDivision, int hvoBook, IVwLayoutStream sharedStream,
			int ws)
			: base(cache, styleSheet, publication, viewType, filterInstance, printDateTime,
			fIntroDivision, hvoBook, sharedStream, ws)
		{
		}

		#region Implementation of IPrintLayoutConfigurer
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates and returns the primary view construtor for the main view in the layout.
		/// This is only called once.
		/// </summary>
		/// <param name="div"></param>
		/// <returns>The view constructor to be used for the main view</returns>
		/// ------------------------------------------------------------------------------------
		public override IVwViewConstructor MakeMainVc(DivisionLayoutMgr div)
		{
			BtPrintLayoutSideBySideVc vc = new BtPrintLayoutSideBySideVc(
				TeStVc.LayoutViewTarget.targetPrint, div.FilterInstance, m_styleSheet,
				m_fdoCache, m_ws);
			vc.HeightEstimator = this;
			return vc;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes a call to MakeSubordinateView, to add a view containing footnotes.
		/// </summary>
		/// <param name="div">The PrintLayout manager object</param>
		/// ------------------------------------------------------------------------------------
		public override void ConfigureSubordinateViews(DivisionLayoutMgr div)
		{
			int hvoScripture = m_fdoCache.LangProject.TranslatedScriptureOAHvo;

			BtFootnotePrintLayoutSideBySideVc footnoteVc = new BtFootnotePrintLayoutSideBySideVc(
				TeStVc.LayoutViewTarget.targetPrint, div.FilterInstance, m_styleSheet,
				m_fdoCache, m_ws);
			NLevelOwnerSvd ownerSvd = new NLevelOwnerSvd(2, m_fdoCache.MainCacheAccessor,
				hvoScripture);

			IVwVirtualHandler vh =
				FilteredScrBooks.GetFilterInstance(m_fdoCache, div.FilterInstance);

			if (vh != null)
			{
				ownerSvd.AddTagLookup((int)Scripture.ScriptureTags.kflidScriptureBooks,
					vh.Tag);
			}

			div.AddSubordinateStream(hvoScripture, (int)FootnoteFrags.kfrScripture,
				footnoteVc,	ownerSvd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create and return a configurer for headers and footers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override IHeaderFooterConfigurer HFConfigurer
		{
			get
			{
				PubDivision pubDiv = (PubDivision)m_pub.DivisionsOS[0];
				int hvoHFSet = pubDiv.HFSetOAHvo;
				return new TeHeaderFooterConfigurer(m_fdoCache, hvoHFSet, m_ws,
					m_bookFilterInstance, m_printDateTime, m_sectionFilter.Tag);
			}
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the average height for a paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override int AverageParaHeight
		{
			get
			{
				// We are displaying front and back trans side by side, so our para height
				// is twice the size.
				return 2 * base.AverageParaHeight;
			}
		}
	}
	#endregion

	#region TeBtPublication
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TeBtPublication : ScripturePublication
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:TeBtPublication"/> class.
		/// </summary>
		/// <param name="cache">The database connection</param>
		/// <param name="stylesheet">The stylesheet to be used for this publication (can be
		/// different from the one used for drafting, but should probably have all the same
		/// styles)</param>
		/// <param name="filterInstance">number used to make filters unique per main window</param>
		/// <param name="publication">The publication to get the information from (or
		/// null to keep the defaults)</param>
		/// <param name="viewType">Type of the view.</param>
		/// <param name="printDateTime">Date/Time of the printing</param>
		/// ------------------------------------------------------------------------------------
		public TeBtPublication(FdoCache cache, FwStyleSheet stylesheet, int filterInstance,
			IPublication publication, TeViewType viewType, DateTime printDateTime)
			: base(stylesheet, filterInstance, publication, viewType, printDateTime)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the print layout configurer.
		/// </summary>
		/// <param name="fIntroDivision">set to <c>true</c> for a division that displays book
		/// title and introduction material, <c>false</c> for a division that displays main
		/// scripture text.</param>
		/// <param name="hvoBook">The hvo of the book.</param>
		/// <param name="sharedStream">A layout stream used for footnotes which is shared across
		/// multiple divisions</param>
		/// <param name="ws">The writing system</param>
		/// <returns>A print layout configurer</returns>
		/// ------------------------------------------------------------------------------------
		protected override TePrintLayoutConfig GetPrintLayoutConfigurer(bool fIntroDivision,
			int hvoBook, IVwLayoutStream sharedStream, int ws)
		{
			return new TeBtPrintLayoutConfig(m_cache, m_stylesheet, m_publication, m_viewType,
				m_filterInstance, m_printDateTime, fIntroDivision, hvoBook, sharedStream, ws);
		}

		/// <summary>
		/// Although this view considers itself to have a ContentType of BT, it is helpful
		/// in enabling menu options (that are applicable to the LHS) if we do NOT consider
		/// the editing helper to be a BT one.
		/// </summary>
		/// <returns></returns>
		protected override EditingHelper GetInternalEditingHelper()
		{
			TeEditingHelper result = base.GetInternalEditingHelper() as TeEditingHelper;
			result.ContentType = StVc.ContentTypes.kctNormal;
			result.EnableBtSegmentUpdate(m_BackTranslationWS);
			return result;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing system for the HVO. This could either be the vernacular or
		/// analysis writing system.
		/// </summary>
		/// <param name="hvo">HVO</param>
		/// <returns>Writing system</returns>
		/// ------------------------------------------------------------------------------------
		public override int GetWritingSystemForHvo(int hvo)
		{
			CheckDisposed();

			BtPrintLayoutSideBySideVc vc = Divisions[0].MainVc as BtPrintLayoutSideBySideVc;
			return vc.GetWritingSystemForHvo(hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the section tag.
		/// </summary>
		/// <param name="configurer">The configurer.</param>
		/// <returns>The section tag</returns>
		/// ------------------------------------------------------------------------------------
		protected override int GetSectionTag(TePrintLayoutConfig configurer)
		{
			return (int)ScrBook.ScrBookTags.kflidSections;
		}

		/// <summary>
		/// Override to restore prompt selection if relevant (for segmented BT).
		/// </summary>
		/// <param name="e"></param>
		protected override void OnKeyPress(System.Windows.Forms.KeyPressEventArgs e)
		{
			using (new PromptSelectionRestorer(RootBox))
				base.OnKeyPress(e);
		}

		private FreeTransEditMonitor m_ftMonitor;
		private CmTranslationEditMonitor m_cmtMonitor;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Focus got set to the draft view
		/// </summary>
		/// <param name="e">The event data</param>
		/// -----------------------------------------------------------------------------------
		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);

			// Supposedly, m_ftMonitor should always be null, since it only gets set in OnGetFocus
			// and it gets cleared in OnLostFocus. However there have been odd cases. If we're already
			// tracking a change we don't want to lose it.
			// Enhance JohnT: we don't really need this unless we're doing segment BTs.
			if (m_ftMonitor == null)
			{
				m_ftMonitor = new FreeTransEditMonitor(Cache, BackTranslationWS);
				// Unfortunately, when the main window closes, both our Dispose() method and our OnLostFocus() method
				// get called during the Dispose() of the main window, which is AFTER the FdoCache gets disposed.
				// We need to dispose our FreeTransEditMonitor before the cache is disposed, so we can update the
				// CmTranslation if necessary.
				if (TopLevelControl is Form)
					(TopLevelControl as Form).FormClosing += new FormClosingEventHandler(FormClosing);
			}
			if (m_cmtMonitor == null)
			{
				m_cmtMonitor = new CmTranslationEditMonitor(Cache, BackTranslationWS);
				// Unfortunately, when the main window closes, both our Dispose() method and our OnLostFocus() method
				// get called during the Dispose() of the main window, which is AFTER the FdoCache gets disposed.
				// We need to dispose our FreeTransEditMonitor before the cache is disposed, so we can update the
				// CmTranslation if necessary.
				if (TopLevelControl is Form)
					(TopLevelControl as Form).FormClosing += new FormClosingEventHandler(FormClosing);
			}
		}

		void FormClosing(object sender, FormClosingEventArgs e)
		{
			DisposeFtMonitor();
		}

		/// <summary>
		/// Any pending changes to the CmTranslation BT required to match changes to the segmented BT
		/// should now be done.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnLostFocus(EventArgs e)
		{
			DisposeFtMonitor();
			base.OnLostFocus(e);
		}

		private void DisposeFtMonitor()
		{
			if (m_ftMonitor != null)
			{
				if (TopLevelControl is Form)
					(TopLevelControl as Form).FormClosing -= new FormClosingEventHandler(FormClosing);
				m_ftMonitor.Dispose();
				m_ftMonitor = null;
			}
			if (m_cmtMonitor != null)
			{
				if (TopLevelControl is Form)
					(TopLevelControl as Form).FormClosing -= new FormClosingEventHandler(FormClosing);
				m_cmtMonitor.Dispose();
				m_cmtMonitor = null;
			}
		}
	}
	#endregion
}
