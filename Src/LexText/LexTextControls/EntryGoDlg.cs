// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using System.Xml;

using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	public class EntryGoDlg : BaseGoDlg
	{
		#region	Data members

		protected bool m_fNewlyCreated;
		protected ILexSense m_newSense;
		protected ILexEntry m_startingEntry;
		protected int m_oldSearchWs;

		#endregion	// Data members

		#region Properties

		protected override WindowParams DefaultWindowParams
		{
			get
			{
				return new WindowParams {m_title = LexTextControls.ksFindLexEntry};
			}
		}

		protected override string PersistenceLabel
		{
			get { return "EntryGo"; }
		}

		/// <summary>
		/// Get/Set the starting entry object.  This will not be displayed in the list of
		/// matching entries.
		/// </summary>
		public ILexEntry StartingEntry
		{
			get
			{
				CheckDisposed();
				return (ILexEntry) m_matchingObjectsBrowser.StartingObject;
			}
			set
			{
				CheckDisposed();
				m_matchingObjectsBrowser.StartingObject = value;
			}
		}

		protected override string Form
		{
			set
			{
				base.Form = MorphServices.EnsureNoMarkers(value, m_cache);
			}
		}

		#endregion Properties

		#region	Construction and Destruction

		/// <summary>
		/// Constructor.
		/// </summary>
		public EntryGoDlg()
		{
			SetHelpTopic("khtpFindInDictionary"); // Default help topic ID
			m_objectsLabel.Text = LexTextControls.ksLexicalEntries;
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "searchEngine is disposed by the mediator.")]
		protected override void InitializeMatchingObjects(FdoCache cache, Mediator mediator)
		{
			var xnWindow = (XmlNode) m_mediator.PropertyTable.GetValue("WindowConfiguration");
			XmlNode configNode = xnWindow.SelectSingleNode("controls/parameters/guicontrol[@id=\"matchingEntries\"]/parameters");

			SearchEngine searchEngine = SearchEngine.Get(mediator, "EntryGoSearchEngine", () => new EntryGoSearchEngine(cache));

			m_matchingObjectsBrowser.Initialize(cache, FontHeightAdjuster.StyleSheetFromMediator(mediator), mediator, configNode,
				searchEngine);

			m_matchingObjectsBrowser.ColumnsChanged += m_matchingObjectsBrowser_ColumnsChanged;

			// start building index
			var selectedWs = (CoreWritingSystemDefinition) m_cbWritingSystems.SelectedItem;
			if (selectedWs != null)
				m_matchingObjectsBrowser.SearchAsync(GetFields(string.Empty, selectedWs.Handle));
		}
		#endregion Construction and Destruction

		#region	Other methods

		/// <summary>
		/// Reset the list of matching items.
		/// </summary>
		/// <param name="searchKey"></param>
		protected override void ResetMatches(string searchKey)
		{
			string sAdjusted;
			var mmt = MorphServices.GetTypeIfMatchesPrefix(m_cache, searchKey, out sAdjusted);
			if (mmt != null)
			{
				searchKey = String.Empty;
				m_btnInsert.Enabled = false;
			}
			else if (searchKey.Length > 0)
			{
				// NB: This method strips off reserved characters for searchKey,
				// which is a good thing.  (fixes LT-802?)
				try
				{
					int clsidForm;
					MorphServices.FindMorphType(m_cache, ref searchKey, out clsidForm);
					m_btnInsert.Enabled = searchKey.Length > 0;
				}
				catch (Exception ex)
				{
					Cursor = Cursors.Default;
					MessageBox.Show(ex.Message, LexText.Controls.LexTextControls.ksInvalidForm,
						MessageBoxButtons.OK);
					m_btnInsert.Enabled = false;
					return;
				}
			}
			else
			{
				m_btnInsert.Enabled = false;
			}
			var selectedWs = (CoreWritingSystemDefinition) m_cbWritingSystems.SelectedItem;
			int wsSelHvo = selectedWs != null ? selectedWs.Handle : 0;

			if (!m_vernHvos.Contains(wsSelHvo) && !m_analHvos.Contains(wsSelHvo))
			{
				wsSelHvo = TsStringUtils.GetWsAtOffset(m_tbForm.Tss, 0);
				if (!m_vernHvos.Contains(wsSelHvo) && !m_analHvos.Contains(wsSelHvo))
					return;
			}

			if (m_oldSearchKey == searchKey && m_oldSearchWs == wsSelHvo)
				return; // Nothing new to do, so skip it.

			if (m_oldSearchKey != string.Empty || searchKey != string.Empty)
				StartSearchAnimation();

			// disable Go button until we rebuild our match list.
			m_btnOK.Enabled = false;
			m_oldSearchKey = searchKey;
			m_oldSearchWs = wsSelHvo;

			m_matchingObjectsBrowser.SearchAsync(GetFields(searchKey, wsSelHvo));
		}

		private IEnumerable<SearchField> GetFields(string str, int ws)
		{
			var tssKey = m_tsf.MakeString(str, ws);
			if (m_vernHvos.Contains(ws))
			{
				if (m_matchingObjectsBrowser.IsVisibleColumn("EntryHeadword") || m_matchingObjectsBrowser.IsVisibleColumn("CitationForm"))
					yield return new SearchField(LexEntryTags.kflidCitationForm, tssKey);
				if (m_matchingObjectsBrowser.IsVisibleColumn("EntryHeadword") || m_matchingObjectsBrowser.IsVisibleColumn("LexemeForm"))
					yield return new SearchField(LexEntryTags.kflidLexemeForm, tssKey);
				if (m_matchingObjectsBrowser.IsVisibleColumn("Allomorphs"))
					yield return new SearchField(LexEntryTags.kflidAlternateForms, tssKey);
			}
			if (m_analHvos.Contains(ws))
			{
				if (m_matchingObjectsBrowser.IsVisibleColumn("Glosses"))
					yield return new SearchField(LexSenseTags.kflidGloss, tssKey);
				if (m_matchingObjectsBrowser.IsVisibleColumn("Reversals"))
					yield return new SearchField(LexSenseTags.kflidReversalEntries, tssKey);
				if (m_matchingObjectsBrowser.IsVisibleColumn("Definitions"))
					yield return new SearchField(LexSenseTags.kflidDefinition, tssKey);
			}
		}

		private void m_matchingObjectsBrowser_ColumnsChanged(object sender, EventArgs e)
		{
			if (m_oldSearchKey != string.Empty && m_oldSearchWs != 0)
			{
				var tempKey = m_oldSearchKey;
				m_oldSearchKey = string.Empty;
				ResetMatches(tempKey); // force Reset w/o changing strings
			}
		}

		#endregion	// Other methods

		#region	Event handlers

		protected override string AdjustText(out int addToSelection)
		{
			bool selWasAtEnd = m_tbForm.SelectionStart + m_tbForm.SelectionLength == m_tbForm.Text.Length;
			string fixedText = base.AdjustText(out addToSelection);
			// Only do the morpheme marker trick if the selection is at the end, a good sign the user just
			// typed it. This avoids the situation where it is impossible to delete one of a pair of tildes.
			if (!selWasAtEnd)
				return fixedText;
			// Check whether we need to handle partial marking of a morphtype (suprafix in the
			// default case: see LT-6082).
			string sAdjusted;
			var mmt = MorphServices.GetTypeIfMatchesPrefix(m_cache, fixedText, out sAdjusted);
			if (mmt != null && fixedText != sAdjusted)
			{
				m_skipCheck = true;
				m_tbForm.Text = sAdjusted;
				m_skipCheck = false;
				return sAdjusted;
			}
			return fixedText;
		}

		protected override void m_btnInsert_Click(object sender, EventArgs e)
		{
			using (var dlg = new InsertEntryDlg())
			{
				string form = m_tbForm.Text.Trim();
				ITsString tssFormTrimmed = TsStringUtils.MakeTss(form, TsStringUtils.GetWsAtOffset(m_tbForm.Tss, 0));
				dlg.SetDlgInfo(m_cache, tssFormTrimmed, m_mediator);
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					ILexEntry entry;
					dlg.GetDialogInfo(out entry, out m_fNewlyCreated);
					m_selObject = entry;
					if (m_fNewlyCreated)
						m_newSense = entry.SensesOS[0];
					// If we ever decide not to simulate the btnOK click at this point, then
					// the new sense id will need to be handled by a subclass differently (ie,
					// being added to the list of senses maintained by LinkEntryOrSenseDlg,
					// the selected index into that list also being changed).
					HandleMatchingSelectionChanged();
					if (m_btnOK.Enabled)
						m_btnOK.PerformClick();
				}
			}
		}

		#endregion	// Event handlers
	}
}
