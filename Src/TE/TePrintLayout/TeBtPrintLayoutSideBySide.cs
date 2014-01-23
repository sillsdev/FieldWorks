// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TeBtPrintLayoutSideBySide.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.PrintLayout;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.IText;
using SIL.FieldWorks.FDO.DomainServices;
using XCore;

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
		/// <param name="hvoBook">The hvo of the book.</param>
		/// <param name="sharedStream">A layout stream used for footnotes which is shared across
		/// multiple divisions</param>
		/// <param name="ws">The writing system to use for the back translation</param>
		/// ------------------------------------------------------------------------------------
		public TeBtPrintLayoutConfig(FdoCache cache, IVwStylesheet styleSheet,
			IPublication publication, TeViewType viewType, int filterInstance,
			DateTime printDateTime, int hvoBook, IVwLayoutStream sharedStream,
			int ws)
			: base(cache, styleSheet, publication, viewType, filterInstance, printDateTime,
			PrintLayoutPortion.AllContent, hvoBook, sharedStream, ws)
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
			int hvoScripture = m_fdoCache.LangProject.TranslatedScriptureOA.Hvo;

			BtFootnotePrintLayoutSideBySideVc footnoteVc = new BtFootnotePrintLayoutSideBySideVc(
				TeStVc.LayoutViewTarget.targetPrint, div.FilterInstance, m_styleSheet,
				m_fdoCache, m_ws);
			NLevelOwnerSvd ownerSvd = new NLevelOwnerSvd(2,
				new ScrBookFilterDecorator(m_fdoCache, m_bookFilterInstance), hvoScripture);

			div.AddSubordinateStream(hvoScripture, (int)FootnoteFrags.kfrScripture,
				footnoteVc,	ownerSvd);
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the data access object used for all views.
		/// Review JohnT: is this the right way to get this? And, should it be an FdoCache?
		/// I'd prefer not to make PrintLayout absolutely dependent on having an FdoCache,
		/// but usually it will have, and there are things to take advantage of if it does.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		public override ISilDataAccess DataAccess
		{
			get
			{
				return new ScrBookFilterDecorator(m_fdoCache, m_bookFilterInstance);
			}
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
				IPubDivision pubDiv = m_pub.DivisionsOS[0];
				int hvoHFSet = pubDiv.HFSetOA.Hvo;
				return new TeHeaderFooterConfigurer(m_fdoCache, hvoHFSet, m_ws,
					m_bookFilterInstance, m_printDateTime);
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
		/// <param name="stylesheet">The stylesheet to be used for this publication (can be
		/// different from the one used for drafting, but should probably have all the same
		/// styles)</param>
		/// <param name="filterInstance">number used to make filters unique per main window</param>
		/// <param name="publication">The publication to get the information from (or
		/// null to keep the defaults)</param>
		/// <param name="viewType">Type of the view.</param>
		/// <param name="printDateTime">Date/Time of the printing</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="app">The app.</param>
		/// <param name="btWs">Backtranslation WS</param>
		/// ------------------------------------------------------------------------------------
		public TeBtPublication(FwStyleSheet stylesheet, int filterInstance,
			IPublication publication, TeViewType viewType, DateTime printDateTime,
			IHelpTopicProvider helpTopicProvider, IApp app, int btWs)
			: base(stylesheet, filterInstance, publication, viewType, printDateTime,
			helpTopicProvider, app, btWs)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the print layout configurer.
		/// </summary>
		/// <param name="divisionPortion">division portion - not used since BT layouts out all content</param>
		/// <param name="hvoBook">The hvo of the book.</param>
		/// <param name="sharedStream">A layout stream used for footnotes which is shared across
		/// multiple divisions</param>
		/// <param name="ws">The writing system</param>
		/// <returns>A print layout configurer</returns>
		/// ------------------------------------------------------------------------------------
		protected override TePrintLayoutConfig GetPrintLayoutConfigurer(PrintLayoutPortion divisionPortion,
			int hvoBook, IVwLayoutStream sharedStream, int ws)
		{
			return new TeBtPrintLayoutConfig(m_cache, m_stylesheet, m_publication, m_viewType,
				m_filterInstance, m_printDateTime, hvoBook, sharedStream, ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Although this view considers itself to have a ContentType of BT, it is helpful
		/// in enabling menu options (that are applicable to the LHS) if we do NOT consider
		/// the editing helper to be a BT one.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override RootSiteEditingHelper GetInternalEditingHelper()
		{
			TeEditingHelper result = (TeEditingHelper)base.GetInternalEditingHelper();
			result.ContentType = StVc.ContentTypes.kctNormal;
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

		/// <summary>
		/// Override to restore prompt selection if relevant (for segmented BT).
		/// </summary>
		/// <param name="e"></param>
		protected override void OnKeyPress(System.Windows.Forms.KeyPressEventArgs e)
		{
			using (new PromptSelectionRestorer(RootBox))
				base.OnKeyPress(e);
		}
	}
	#endregion
}
