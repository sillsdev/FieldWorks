// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using LanguageExplorer.Controls.LexText;
using LanguageExplorer.LcmUi;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// An StTextSlice implements the sttext editor type for atomic attributes whose value is an StText.
	/// The resulting view allows the editing of the text, including creating and destroying (and splitting
	/// and merging) of the paragraphs using the usual keyboard actions.
	/// </summary>
	internal class StTextSlice : ViewPropertySlice
	{
		private readonly int m_ws;

		internal StTextSlice(ICmObject obj, int flid, int ws)
			: base(new StTextView(), obj, flid)
		{
			m_ws = ws;
		}

		public override void FinishInit()
		{
			CheckDisposed();
			base.FinishInit();

			var objPropHvo = m_cache.DomainDataByFlid.get_ObjectProp(m_obj.Hvo, FieldId);
			if (objPropHvo == 0)
			{
				CreateText();
			}
			else
			{
				var rootSiteAsStTextView = (StTextView) RootSite;
				if (rootSiteAsStTextView.StText == null)
				{
					// Owner has the text, but it isn't in the view yet.
					rootSiteAsStTextView.StText = m_cache.ServiceLocator.GetInstance<IStTextRepository>().GetObject(objPropHvo);
				}
			}
			((StTextView)RootSite).Init(m_ws);
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
			((StTextView)Control).SelectAt(ich);
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
			{
				return null;
			}
			var sel2 = sel.EndBeforeAnchor ? sel.EndPoint(true) : sel.EndPoint(false);
			return sel2?.GrowToWord();
		}

		/// <summary>
		/// Look up the selected wordform in the dictionary and display its lexical entry.
		/// </summary>
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
				LexEntryUi.DisplayOrCreateEntry(m_cache, hvo, tag, ws, ichMin, ichLim, this, PropertyTable, Publisher, Subscriber, PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), "UserHelpFile");
				return true;
			}
			return false;
		}

		private void GetWordLimitsOfSelection(out int ichMin, out int ichLim, out int hvo, out int tag, out int ws, out ITsString tss)
		{
			ichMin = ichLim = hvo = tag = ws = 0;
			tss = null;
			var wordsel = SelectionBeginningGrowToWord(RootSite.RootBox.Selection);
			if (wordsel == null)
			{
				return;
			}

			bool fAssocPrev;
			wordsel.TextSelInfo(false, out tss, out ichMin, out fAssocPrev, out hvo, out tag, out ws);
			wordsel.TextSelInfo(true, out tss, out ichLim, out fAssocPrev, out hvo, out tag, out ws);

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
			{
				return 0;
			}
			var runMin = tss.get_RunAt(ichMin);
			var runMax = tss.get_RunAt(ichLim - 1);
			var ws = tss.get_WritingSystem(runMin);
			if (runMin == runMax)
			{
				return ws;
			}
			for (var i = runMin + 1; i <= runMax; ++i)
			{
				var wsT = tss.get_WritingSystem(i);
				if (wsT != ws)
				{
					return 0;
				}
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
			{
				ws = GetWsFromString(tss, ichMin, ichLim);
			}

			if (ichLim <= ichMin || ws != m_cache.DefaultVernWs)
			{
				return false;
			}
			var tsb = tss.GetBldr();
			if (ichLim < tsb.Length)
			{
				tsb.Replace(ichLim, tsb.Length, null, null);
			}

			if (ichMin > 0)
			{
				tsb.Replace(0, ichMin, null, null);
			}
			var tssForm = tsb.GetString();
			using (var dlg = new InsertEntryDlg())
			{
				dlg.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				dlg.SetDlgInfo(m_cache, tssForm);
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					// is there anything special we want to do?
				}
			}
			return true;
		}

		protected override void OnEnter(EventArgs e)
		{
			base.OnEnter(e);

			// If we don't already have an StText in this field, make one now.
			if (((StTextView)RootSite).StText == null)
			{
				CreateText();
			}
		}

		private void CreateText()
		{
			var view = (StTextView)RootSite;
			var textHvo = 0;
			NonUndoableUnitOfWorkHelper.Do(m_cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
			{
				var sda = m_cache.DomainDataByFlid;
				textHvo = sda.MakeNewObject(StTextTags.kClassId, m_obj.Hvo, FieldId, -2);
				var hvoStTxtPara = sda.MakeNewObject(StTxtParaTags.kClassId, textHvo, StTextTags.kflidParagraphs, 0);
				sda.SetString(hvoStTxtPara, StTxtParaTags.kflidContents, TsStringUtils.EmptyString(m_ws == 0 ? m_cache.DefaultAnalWs : m_ws));
			});
			view.StText = m_cache.ServiceLocator.GetInstance<IStTextRepository>().GetObject(textHvo);
		}
	}
}