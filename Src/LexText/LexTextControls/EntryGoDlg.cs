using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;
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
		private EntrySearchFieldGetter m_searchFieldGetter;

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
				return m_startingEntry;
			}
			set
			{
				CheckDisposed();
				m_startingEntry = value;
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

		protected override void InitializeMatchingObjects(FdoCache cache, Mediator mediator)
		{
			var xnWindow = (XmlNode)m_mediator.PropertyTable.GetValue("WindowConfiguration");
			XmlNode configNode = xnWindow.SelectSingleNode("controls/parameters/guicontrol[@id=\"matchingEntries\"]/parameters");
			var selectedWs = (IWritingSystem)m_cbWritingSystems.SelectedItem;
			var wsSearch = selectedWs != null ? selectedWs.Handle : 0;
			m_searchFieldGetter = new EntrySearchFieldGetter { AnalHvos = m_analHvos,
				VernHvos = m_vernHvos, SearchWs = wsSearch };
			m_matchingObjectsBrowser.Initialize(cache, FontHeightAdjuster.StyleSheetFromMediator(mediator), mediator, configNode,
				cache.ServiceLocator.GetInstance<ILexEntryRepository>().AllInstances().Cast<ICmObject>(), SearchType.Prefix,
				m_searchFieldGetter.GetEntrySearchFields);
		}

		#endregion Construction and Destruction

		#region	Other methods

		/// <summary>
		/// Reset the list of matching items.
		/// </summary>
		/// <param name="searchKey"></param>
		protected override void ResetMatches(string searchKey)
		{
			using (new WaitCursor(this))
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
				var selectedWs = (IWritingSystem) m_cbWritingSystems.SelectedItem;
				int wsSelHvo = selectedWs != null ? selectedWs.Handle : 0;

				string form, gloss;
				int vernWs, analWs;
				if (!GetSearchKey(wsSelHvo, searchKey, out form, out vernWs, out gloss, out analWs))
				{
					int ws = StringUtils.GetWsAtOffset(m_tbForm.Tss, 0);
					if (!GetSearchKey(ws, searchKey, out form, out vernWs, out gloss, out analWs))
						return;
					wsSelHvo = ws;
				}

				if (m_oldSearchKey == searchKey && m_oldSearchWs == wsSelHvo)
					return; // Nothing new to do, so skip it.
				if (m_oldSearchWs != wsSelHvo)
				{
					m_matchingObjectsBrowser.Reset();
					// This updates the writing system for the functor.
					m_searchFieldGetter.SearchWs = wsSelHvo;
				}
				// disable Go button until we rebuild our match list.
				m_btnOK.Enabled = false;
				m_oldSearchKey = searchKey;
				m_oldSearchWs = wsSelHvo;

				var fields = new List<SearchField>();
				if (form != null)
				{
					ITsString tssForm = m_tsf.MakeString(form, vernWs);
					fields.Add(new SearchField(LexEntryTags.kflidCitationForm, tssForm));
					fields.Add(new SearchField(LexEntryTags.kflidLexemeForm, tssForm));
					fields.Add(new SearchField(LexEntryTags.kflidAlternateForms, tssForm));
				}
				if (gloss != null)
				{
					ITsString tssGloss = m_tsf.MakeString(gloss, analWs);
					fields.Add(new SearchField(LexSenseTags.kflidGloss, tssGloss));
				}

				if (!Controls.Contains(m_searchAnimation))
				{
					Controls.Add(m_searchAnimation);
					m_searchAnimation.BringToFront();
				}


				m_matchingObjectsBrowser.Search(fields, m_startingEntry == null ? null : new[] {m_startingEntry});

				if (Controls.Contains(m_searchAnimation))
					Controls.Remove(m_searchAnimation);
			}
		}

		protected override void m_cbWritingSystems_SelectedIndexChanged(object sender, EventArgs e)
		{
			m_matchingObjectsBrowser.Reset();
			base.m_cbWritingSystems_SelectedIndexChanged(sender, e);
		}

		private bool GetSearchKey(int ws, string searchKey, out string form, out int vernWs, out string gloss, out int analWs)
		{
			form = gloss = null;
			vernWs = analWs = 0;

			bool isVernWs = m_vernHvos.Contains(ws);
			bool isAnalWs = m_analHvos.Contains(ws);
			if (isVernWs && isAnalWs)
			{
				// Ambiguous, so search both.
				vernWs = ws;
				analWs = ws;
				form = searchKey;
				gloss = searchKey;
			}
			else if (isVernWs)
			{
				vernWs = ws;
				form = searchKey;
			}
			else if (isAnalWs)
			{
				analWs = ws;
				gloss = searchKey;
			}
			else
			{
				return false;
			}

			return true;
		}

		private class EntrySearchFieldGetter
		{
			public int SearchWs { private get; set; }
			public HashSet<int> VernHvos { private get; set; }
			public HashSet<int> AnalHvos { private get; set; }

			/// <summary>
			/// To avoid looking up the combo every time this is called...once per entry in the DB!...
			/// the caller passes us an array in which we can cache it.
			/// </summary>
			public IEnumerable<SearchField> GetEntrySearchFields(ICmObject obj)
			{
				var entry = (ILexEntry)obj;
				if (VernHvos.Contains(SearchWs))
				{
					var cf = entry.CitationForm.StringOrNull(SearchWs);
					if (cf != null && cf.Length > 0)
						yield return new SearchField(LexEntryTags.kflidCitationForm, cf);
					if (entry.LexemeFormOA != null)
					{
						var lf = entry.LexemeFormOA.Form.StringOrNull(SearchWs);
						if (lf != null && lf.Length > 0)
							yield return new SearchField(LexEntryTags.kflidLexemeForm, lf);
					}
					foreach (IMoForm form in entry.AlternateFormsOS)
					{
						var af = form.Form.StringOrNull(SearchWs);
						if (af != null && af.Length > 0)
							yield return new SearchField(LexEntryTags.kflidAlternateForms, af);
					}
				}

				if (AnalHvos.Contains(SearchWs))
				{
					foreach (ILexSense sense in entry.SensesOS)
					{
						var gloss = sense.Gloss.StringOrNull(SearchWs);
						if (gloss != null && gloss.Length > 0)
							yield return new SearchField(LexSenseTags.kflidGloss, gloss);
					}
				}
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
				ITsString tssFormTrimmed = StringUtils.MakeTss(form, StringUtils.GetWsAtOffset(m_tbForm.Tss, 0));
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
