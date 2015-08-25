using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.LexText.Controls;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// An StTextSlice implements the sttext editor type for atomic attributes whose value is an StText.
	/// The resulting view allows the editing of the text, including creating and destroying (and splitting
	/// and merging) of the paragraphs using the usual keyboard actions.
	/// </summary>
	public class StTextSlice : ViewPropertySlice
	{
		private readonly int m_ws;

		public StTextSlice(ICmObject obj, int flid, int ws)
			: base(new StTextView(), obj, flid)
		{
			m_ws = ws;
		}

		public override void FinishInit()
		{
			CheckDisposed();
			base.FinishInit();

			var textHvo = m_cache.DomainDataByFlid.get_ObjectProp(m_obj.Hvo, m_flid);
			((StTextView) RootSite).Init(textHvo == 0 ? null : m_cache.ServiceLocator.GetInstance<IStTextRepository>().GetObject(textHvo), m_ws);
		}

#if RANDYTODO
		public bool OnDisplayLexiconLookup(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Visible = true;
			display.Enabled = false;
			// Enable the command if the selection exists and we actually have a word.
			int ichMin;
			int ichLim;
			int hvo;
			int tag;
			int ws;
			ITsString tss;
			GetWordLimitsOfSelection(out ichMin, out ichLim, out hvo, out tag, out ws, out tss);
			if (ichLim > ichMin)
				display.Enabled = true;
			return true;
		}
#endif

		/// <summary>
		/// Select at the specified position in the first paragraph.
		/// </summary>
		internal void SelectAt(int ich)
		{
			((StTextView) Control).SelectAt(ich);
		}

		/// <summary>
		/// Return a word selection based on the beginning of the current selection.
		/// Here the "beginning" of the selection is the offset corresponding to word order,
		/// not the selection anchor.
		/// </summary>
		/// <returns>null if we couldn't handle the selection</returns>
		private static IVwSelection SelectionBeginningGrowToWord(IVwSelection sel)
		{
			if (sel == null)
				return null;
			var sel2 = sel.EndBeforeAnchor ? sel.EndPoint(true) : sel.EndPoint(false);
			if (sel2 == null)
				return null;
			var sel3 = sel2.GrowToWord();
			return sel3;
		}

		/// <summary>
		/// Look up the selected wordform in the dictionary and display its lexical entry.
		/// </summary>
		/// <param name="argument"></param>
		public bool OnLexiconLookup(object argument)
		{
			CheckDisposed();

			int ichMin;
			int ichLim;
			int hvo;
			int tag;
			int ws;
			ITsString tss;
			GetWordLimitsOfSelection(out ichMin, out ichLim, out hvo, out tag, out ws, out tss);
			if (ichLim > ichMin)
			{
				LexEntryUi.DisplayOrCreateEntry(m_cache, hvo, tag, ws, ichMin, ichLim, this,
					PropertyTable, Publisher, PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), "UserHelpFile");
				return true;
			}
			return false;
		}

		private void GetWordLimitsOfSelection(out int ichMin, out int ichLim,
			out int hvo, out int tag, out int ws, out ITsString tss)
		{
			ichMin = ichLim = hvo = tag = ws = 0;
			tss = null;
			IVwSelection wordsel = SelectionBeginningGrowToWord(RootSite.RootBox.Selection);
			if (wordsel == null)
				return;

			bool fAssocPrev;
			wordsel.TextSelInfo(false, out tss, out ichMin, out fAssocPrev, out hvo, out tag,
				out ws);
			wordsel.TextSelInfo(true, out tss, out ichLim, out fAssocPrev, out hvo, out tag,
				out ws);

		}

#if RANDYTODO
		public bool OnDisplayAddToLexicon(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Visible = true;
			display.Enabled = false;
			// Enable the command if the selection exists, we actually have a word, and it's in
			// the default vernacular writing system.
			int ichMin;
			int ichLim;
			int hvo;
			int tag;
			int ws;
			ITsString tss;
			GetWordLimitsOfSelection(out ichMin, out ichLim, out hvo, out tag, out ws, out tss);
			if (ws == 0)
				ws = GetWsFromString(tss, ichMin, ichLim);
			if (ichLim > ichMin && ws == m_cache.DefaultVernWs)
				display.Enabled = true;
			return true;
		}
#endif

		private static int GetWsFromString(ITsString tss, int ichMin, int ichLim)
		{
			if (tss == null || tss.Length == 0 || ichMin >= ichLim)
				return 0;
			int runMin = tss.get_RunAt(ichMin);
			int runMax = tss.get_RunAt(ichLim - 1);
			int ws = tss.get_WritingSystem(runMin);
			if (runMin == runMax)
				return ws;
			for (int i = runMin + 1; i <= runMax; ++i)
			{
				int wsT = tss.get_WritingSystem(i);
				if (wsT != ws)
					return 0;
			}
			return ws;
		}

		public bool OnAddToLexicon(object argument)
		{
			CheckDisposed();

			int ichMin;
			int ichLim;
			int hvo;
			int tag;
			int ws;
			ITsString tss;
			GetWordLimitsOfSelection(out ichMin, out ichLim, out hvo, out tag, out ws, out tss);
			if (ws == 0)
				ws = GetWsFromString(tss, ichMin, ichLim);
			if (ichLim > ichMin && ws == m_cache.DefaultVernWs)
			{
				ITsStrBldr tsb = tss.GetBldr();
				if (ichLim < tsb.Length)
					tsb.Replace(ichLim, tsb.Length, null, null);
				if (ichMin > 0)
					tsb.Replace(0, ichMin, null, null);
				ITsString tssForm = tsb.GetString();
				using (var dlg = new InsertEntryDlg())
				{
					dlg.SetDlgInfo(m_cache, tssForm, PropertyTable, Publisher);
					if (dlg.ShowDialog(this) == DialogResult.OK)
					{
						// is there anything special we want to do?
					}
				}
				return true;
			}
			return false;
		}

		protected override void OnEnter(EventArgs e)
		{
			base.OnEnter(e);

			// If we don't already have an StText in this field, make one now.
			var view = (StTextView) RootSite;
			if (view.StText == null)
			{
				int textHvo = 0;
				NonUndoableUnitOfWorkHelper.Do(m_cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
				{
					var sda = m_cache.DomainDataByFlid;
					textHvo = sda.MakeNewObject(StTextTags.kClassId, m_obj.Hvo, m_flid, -2);
					var hvoStTxtPara = sda.MakeNewObject(StTxtParaTags.kClassId, textHvo, StTextTags.kflidParagraphs, 0);
					var tsf = m_cache.TsStrFactory;
					sda.SetString(hvoStTxtPara, StTxtParaTags.kflidContents, tsf.EmptyString(m_ws == 0 ? m_cache.DefaultAnalWs : m_ws));
				});
				view.StText = m_cache.ServiceLocator.GetInstance<IStTextRepository>().GetObject(textHvo);
			}
		}
	}

	#region RootSite class

	public class StTextView : RootSiteControl
	{
		private StVc m_vc;
		private IStText m_text;

		/// <summary>
		/// Gets or sets the StText object.
		/// </summary>
		/// <value>The StText object.</value>
		public IStText StText
		{
			get
			{
				CheckDisposed();
				return m_text;
			}

			set
			{
				CheckDisposed();
				IStText oldText = m_text;
				m_text = value;
				if (m_rootb != null && m_text != null && oldText != m_text)
					m_rootb.SetRootObject(m_text.Hvo, m_vc, (int)StTextFrags.kfrText, m_styleSheet);
			}
		}

		/// <summary>
		/// Select at the specified position in the first paragraph.
		/// </summary>
		internal void SelectAt(int ich)
		{
			try
			{
				var vsli = new SelLevInfo[1];
				vsli[0].tag = StTextTags.kflidParagraphs;
				vsli[0].ihvo = 0;
				RootBox.MakeTextSelection(0, 1, vsli, StTxtParaTags.kflidContents, 0, ich, ich, 0, true, -1, null, true);
			}
			catch (Exception)
			{
				Debug.Assert(false, "Unexpected failure to make selection in StTextView");
			}

		}

		public void Init(IStText text, int ws)
		{
			CheckDisposed();
			Cache = PropertyTable.GetValue<FdoCache>("cache");
			StyleSheet = FontHeightAdjuster.StyleSheetFromPropertyTable(PropertyTable);
			m_text = text;
			m_vc = new StVc("Normal", ws) {Cache = m_fdoCache, Editable = true};
			DoSpellCheck = true;
			if (m_rootb == null)
			{
				MakeRoot();
			}
			else if (m_text != null)
			{
				m_rootb.SetRootObject(m_text.Hvo, m_vc, (int)StTextFrags.kfrText, m_styleSheet);
				m_rootb.Reconstruct();
			}
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
				return;

			base.Dispose(disposing);

			if (disposing)
			{
				// Dispose managed resources here.
			}
			m_vc = null;

			// Dispose unmanaged resources here, whether disposing is true or false.
		}

		#endregion IDisposable override

		public override void MakeRoot()
		{
			CheckDisposed();
			base.MakeRoot();

			if (m_fdoCache == null || DesignMode)
				return;

			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);
			m_rootb.DataAccess = m_fdoCache.DomainDataByFlid;
			if (m_text != null)
				m_rootb.SetRootObject(m_text.Hvo, m_vc, (int)StTextFrags.kfrText, m_styleSheet);

			m_dxdLayoutWidth = kForceLayout; // Don't try to draw until we get OnSize and do layout.

			//TODO:
			//ptmw->RegisterRootBox(qrootb);
		}

		protected override bool OnRightMouseUp(Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			if (base.OnRightMouseUp(pt, rcSrcRoot, rcDstRoot))
				return true;
			var mainWind = ParentForm as IFwMainWnd;
			IVwSelection sel = RootBox == null ? null : RootBox.Selection;
			if (mainWind == null || sel == null)
				return false;
#if RANDYTODO
			mainWind.ShowContextMenu("mnuStTextChoices", new Point(Cursor.Position.X, Cursor.Position.Y),
				null, null);
#endif
			return true;
		}
	}

	#endregion RootSite class
}
