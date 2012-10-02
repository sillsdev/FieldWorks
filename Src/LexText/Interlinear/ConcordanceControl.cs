using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Data;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Xml;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Filters;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.XWorks;
using SIL.Utils;
using XCore;


namespace SIL.FieldWorks.IText
{
	public partial class ConcordanceControl : UserControl, IxCoreColleague, IXCoreUserControl,
		IxCoreContentControl, IFWDisposable
	{
		Mediator m_mediator;
		XmlNode m_configurationParameters;
		protected FdoCache m_cache;
		protected OccurrencesOfSelectedUnit m_clerk;
		private RegexHelperMenu m_regexContextMenu;
		private IVwPattern m_vwPattern;
		private bool m_fObjectConcorded = false;
		private int m_hvoMatch = 0;

		public ConcordanceControl()
		{
			InitializeComponent();

			m_regexContextMenu = new RegexHelperMenu(m_tbSearchText, FwApp.App);
			m_vwPattern = VwPatternClass.Create();

			if (FwApp.App != null)
				this.helpProvider.HelpNamespace = FwApp.App.HelpFile;
			this.helpProvider.SetHelpNavigator(this, System.Windows.Forms.HelpNavigator.Topic);
			this.helpProvider.SetShowHelp(this, true);
			if (FwApp.App != null)
			{
				helpProvider.SetHelpKeyword(this, "khtpSpecConcordanceCrit");
				m_btnHelp.Enabled = true;
			}
		}

		#region IxCoreColleague Members

		public IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();
			return new IxCoreColleague[] { this };
		}

		public void Init(Mediator mediator, XmlNode configurationParameters)
		{
			CheckDisposed();
			m_mediator = mediator;
			m_configurationParameters = configurationParameters;
			m_cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			string name = XmlUtils.GetAttributeValue(configurationParameters, "clerk");
			m_clerk = (OccurrencesOfSelectedUnit)m_mediator.PropertyTable.GetValue(name);
			if (m_clerk == null)
				m_clerk = RecordClerkFactory.CreateClerk(m_mediator, m_configurationParameters) as OccurrencesOfSelectedUnit;
			m_clerk.ConcordanceControl = this;

			m_tbSearchText.WritingSystemFactory = m_cache.LanguageWritingSystemFactoryAccessor;
			m_tbSearchText.StyleSheet = SIL.FieldWorks.Common.Widgets.FontHeightAdjuster.StyleSheetFromMediator(mediator);
			m_tbSearchText.Text = String.Empty;
			m_tbSearchText.TextChanged += new EventHandler(m_tbSearchText_TextChanged);
			m_tbSearchText.KeyDown += new KeyEventHandler(m_tbSearchText_KeyDown);
			FillLineComboList();

			m_fwtbItem.WritingSystemFactory = m_cache.LanguageWritingSystemFactoryAccessor;
			m_fwtbItem.StyleSheet = SIL.FieldWorks.Common.Widgets.FontHeightAdjuster.StyleSheetFromMediator(m_mediator);
			m_fwtbItem.WritingSystemCode = m_cache.DefaultVernWs;
			m_fwtbItem.Text = String.Empty;

			// Set some default values.

			m_rbtnAnywhere.Checked = true;
			m_btnRegExp.Enabled = false;
			m_chkMatchDiacritics.Checked = false;
			m_chkMatchCase.Checked = false;
			m_btnSearch.Enabled = false;

			if (m_clerk.SuspendLoadingRecordUntilOnJumpToRecord)
			{
				return;	// we're bound to process OnJumpToRecord, so skip any further initialization.
			}
			// Load any saved settings.
			LoadSettings();

			// if we need to reload the property, we'll just clear our results, so that we won't automatically load the list. (LT-6967)
			if (m_clerk.NeedToReloadVirtualProperty)
			{
				m_clerk.NeedToReloadVirtualProperty = false;
				ClearResults();
			}
		}


		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (components != null)
					components.Dispose();
				if (m_clerk != null)
					m_clerk.ConcordanceControl = null;
				// Don't dispose of the clerk, since it can monitor relevant PropChanges
				// that affect the NeedToReloadVirtualProperty.
			}
			m_clerk = null;
			m_mediator = null;
			base.Dispose(disposing);
		}


		private void LoadSettings()
		{
			string sLine = m_mediator.PropertyTable.GetStringProperty("ConcordanceLine", "kBaseline",
						 PropertyTable.SettingsGroup.LocalSettings);
			ConcordanceLines line = ConcordanceLines.kBaseline;
			try
			{
				line = (ConcordanceLines)Enum.Parse(typeof(ConcordanceLines), sLine);
			}
			catch
			{
				line = ConcordanceLines.kBaseline;
			}
			SetConcordanceLine(line);

			string sWs = m_mediator.PropertyTable.GetStringProperty("ConcordanceWs", null,
				PropertyTable.SettingsGroup.LocalSettings);
			int ws = 0;
			if (sWs != null)
			{
				ws = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(sWs);
				if (ws != 0)	// could be obsolete data.
					SetWritingSystem(ws);
			}
			ws = CurrentSelectedWs();
			m_tbSearchText.WritingSystemCode = ws;

			string sText = m_mediator.PropertyTable.GetStringProperty("ConcordanceText", null,
				PropertyTable.SettingsGroup.LocalSettings);
			if (sText != null)
				m_tbSearchText.Text = sText;

			bool fMatchCase = m_mediator.PropertyTable.GetBoolProperty("ConcordanceMatchCase",
				m_chkMatchCase.Checked, PropertyTable.SettingsGroup.LocalSettings);
			m_chkMatchCase.Checked = fMatchCase;

			bool fMatchDiacritics = m_mediator.PropertyTable.GetBoolProperty("ConcordanceMatchDiacritics",
				m_chkMatchDiacritics.Checked, PropertyTable.SettingsGroup.LocalSettings);
			m_chkMatchDiacritics.Checked = fMatchDiacritics;

			string sConcordanceOption = m_mediator.PropertyTable.GetStringProperty("ConcordanceOption",
				null, PropertyTable.SettingsGroup.LocalSettings);
			SetConcordanceOption(sConcordanceOption);
		}

		private int CurrentSelectedWs()
		{
			ILgWritingSystem lgws = m_cbWritingSystem.SelectedItem as ILgWritingSystem;
			// Could have nothing selected.  See LT-8041.
			if (lgws == null)
				return m_cache.DefaultVernWs;
			else
				return lgws.Hvo;
		}

		protected string GetConcordanceOption()
		{
			string sConcordanceOption;
			if (m_rbtnWholeItem.Checked)
				sConcordanceOption = "WholeItem";
			else if (m_rbtnAtEnd.Checked)
				sConcordanceOption = "AtEnd";
			else if (m_rbtnAtStart.Checked)
				sConcordanceOption = "AtStart";
			else if (m_rbtnUseRegExp.Checked)
				sConcordanceOption = "UseRegExp";
			else
				sConcordanceOption = "Anywhere";
			return sConcordanceOption;
		}

		protected void SetConcordanceOption(string sConcordanceOption)
		{
			if (sConcordanceOption != null)
			{
				switch (sConcordanceOption)
				{
					case "WholeItem":
						m_rbtnWholeItem.Checked = true;
						break;
					case "AtEnd":
						m_rbtnAtEnd.Checked = true;
						break;
					case "AtStart":
						m_rbtnAtStart.Checked = true;
						break;
					case "UseRegExp":
						m_rbtnUseRegExp.Checked = true;
						break;
					default:
						m_rbtnAnywhere.Checked = true;
						break;
				}
			}
		}

		/// <summary>
		/// Gets selected radio box option for the search.
		/// </summary>
		internal ConcordanceSearchOption SearchOption
		{
			get
			{
				return (ConcordanceSearchOption)Enum.Parse(typeof(ConcordanceSearchOption), GetConcordanceOption());
			}
			set
			{
				SetConcordanceOption(value.ToString());
			}
		}

		void m_tbSearchText_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter && m_tbSearchText.Text.Length > 0)
				m_btnSearch_Click(sender, e);
		}

		#endregion

		#region IXCoreUserControl Members

		public string AccName
		{
			get
			{
				CheckDisposed();
				return "Common.Controls.ConcordanceControl";
			}
		}

		#endregion

		#region IFWDisposable Members

		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		#endregion


		#region IxCoreContentControl Members

		public string AreaName
		{
			get
			{
				CheckDisposed();
				return XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "area", "unknown");
			}
		}

		public bool OnPrepareToRefresh(object args)
		{
			// we want to reload our list after a refresh, if our last load was requested.
			// otherwise, the user will have to do the search over again explicitly if they expect new results.
			// LT-6967.
			IVwVirtualHandler vh;
			if (m_cache.TryGetVirtualHandler(WordformInventory.MatchingConcordanceItemsFlid(m_cache), out vh))
			{
				ConcordanceItemsVirtualHandler civh = vh as ConcordanceItemsVirtualHandler;
				// Enhance: How do we guarantee that we use the expected ConcordanceControl to
				// reload our concordance during a MasterRefresh?
				// Even if we were to get information on the window issuing the MasterRefresh
				// ConcordanceControl gets disposed during refresh, and so the virtual property
				// will end up having the control for the last window in our project that gets rebuilt during refresh.
				//civh.ConcordanceControl = this;
				civh.LoadRequested = civh.LastLoadWasRequested;
			}
			return false;
		}

		/// <summary>
		/// This is called on a MasterRefresh
		/// </summary>
		/// <returns></returns>
		public bool PrepareToGoAway()
		{
			CheckDisposed();
			SaveSettings();
			return true;
		}

		/// <summary>
		/// This is called when the main window is closing.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="arg"></param>
		/// <returns></returns>
		public bool OnConsideringClosing(object sender, CancelEventArgs arg)
		{
			arg.Cancel = !PrepareToGoAway();
			return arg.Cancel;
		}

		#endregion

		#region IxCoreCtrlTabProvider Members

		public Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates)
		{
			CheckDisposed();
			if (targetCandidates == null)
				throw new ArgumentNullException("'targetCandidates' is null.");
			targetCandidates.Add(this);
			return ContainsFocus ? this : null;
		}

		#endregion

		#region Overrides

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			(this.Parent as PaneBarContainer).PaneBar.Text = ITextStrings.ksSpecifyConcordanceCriteria;
		}

		protected override void OnLeave(EventArgs e)
		{
			base.OnLeave(e);

			SaveSettings();
		}

		private void SaveSettings()
		{
			// Save our settings for later.
			m_mediator.PropertyTable.SetProperty("ConcordanceLine",
				(m_cbLine.SelectedItem as ConcordLine).Line.ToString(), false,
				PropertyTable.SettingsGroup.LocalSettings);
			m_mediator.PropertyTable.SetPropertyPersistence("ConcordanceLine", true,
				PropertyTable.SettingsGroup.LocalSettings);

			m_mediator.PropertyTable.SetProperty("ConcordanceWs",
				(m_cbWritingSystem.SelectedItem as ILgWritingSystem).ICULocale, false,
				PropertyTable.SettingsGroup.LocalSettings);
			m_mediator.PropertyTable.SetPropertyPersistence("ConcordanceWs", true,
				PropertyTable.SettingsGroup.LocalSettings);

			m_mediator.PropertyTable.SetProperty("ConcordanceText",
				m_tbSearchText.Text.Trim(), false, PropertyTable.SettingsGroup.LocalSettings);
			m_mediator.PropertyTable.SetPropertyPersistence("ConcordanceText", true,
				PropertyTable.SettingsGroup.LocalSettings);

			m_mediator.PropertyTable.SetProperty("ConcordanceMatchCase",
				m_chkMatchCase.Checked, false, PropertyTable.SettingsGroup.LocalSettings);
			m_mediator.PropertyTable.SetPropertyPersistence("ConcordanceMatchCase", true,
				PropertyTable.SettingsGroup.LocalSettings);

			m_mediator.PropertyTable.SetProperty("ConcordanceMatchDiacritics",
				m_chkMatchDiacritics.Checked, false, PropertyTable.SettingsGroup.LocalSettings);
			m_mediator.PropertyTable.SetPropertyPersistence("ConcordanceMatchDiacritics", true,
				PropertyTable.SettingsGroup.LocalSettings);

			string sConcordanceOption = GetConcordanceOption();
			m_mediator.PropertyTable.SetProperty("ConcordanceOption",
				sConcordanceOption, false, PropertyTable.SettingsGroup.LocalSettings);
			m_mediator.PropertyTable.SetPropertyPersistence("ConcordanceOption", true,
				PropertyTable.SettingsGroup.LocalSettings);
		}

		#endregion

		#region Internal types

		internal enum ConcordanceSearchOption { Anywhere, WholeItem, AtEnd, AtStart, UseRegExp } ;

		internal enum ConcordanceLines
		{
			kBaseline,
			kWord,
			kMorphemes,
			kLexEntry,
			kLexGloss,
			kWordGloss,
			kFreeTranslation,
			kLiteralTranslation,
			kNote
		};

		/// <summary>
		/// This class stores the objects used by the Line combo box.
		/// </summary>
		private class ConcordLine
		{
			private string m_name;
			private int m_wsMagic;
			ConcordanceLines m_line;

			internal ConcordLine(string name, int wsMagic, ConcordanceLines line)
			{
				m_name = name;
				m_wsMagic = wsMagic;
				m_line = line;
			}

			internal string Name
			{
				get { return m_name; }
			}

			internal int MagicWs
			{
				get { return m_wsMagic; }
			}

			internal ConcordanceLines Line
			{
				get { return m_line; }
			}

			public override string ToString()
			{
				return m_name;
			}
		}

		#endregion

		#region Triggered Event Handlers

		private void m_cbLine_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateButtonState();
		}

		protected void SyncWritingSystemComboToSelectedLine()
		{
			SyncWritingSystemComboToSelectedLine((ConcordLine) m_cbLine.SelectedItem);
		}

		private void SyncWritingSystemComboToSelectedLine(ConcordLine sel)
		{
			FillWritingSystemCombo(sel.MagicWs);
		}

		private void UpdateButtonState()
		{
			ConcordLine sel = (ConcordLine)m_cbLine.SelectedItem;
			SyncWritingSystemComboToSelectedLine(sel);
			switch (sel.Line)
			{
				case ConcordanceLines.kBaseline:
					// the Baseline currently tries to match in an entire paragraph.
					// so disable "at start" and "at end" and "whole item" matchers.
					if (!m_rbtnAnywhere.Checked && !m_rbtnUseRegExp.Checked)
						m_rbtnAnywhere.Checked = true;
					m_rbtnAtEnd.Enabled = false;
					m_rbtnAtStart.Enabled = false;
					m_rbtnWholeItem.Enabled = false;
					break;
				default:
					m_rbtnAtEnd.Enabled = true;
					m_rbtnAtStart.Enabled = true;
					m_rbtnWholeItem.Enabled = true;
					break;
			}
		}

		private void m_cbWritingSystem_SelectedIndexChanged(object sender, EventArgs e)
		{
			ILgWritingSystem lgws = m_cbWritingSystem.SelectedItem as ILgWritingSystem;
			if (lgws == null)
			{
				Debug.Assert(m_cbWritingSystem.SelectedIndex == -1);
				return;
			}
			m_tbSearchText.WritingSystemCode = lgws.Hvo;
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			m_tbSearchText.Tss = tsf.MakeString(m_tbSearchText.Text.Trim(), lgws.Hvo);
		}

		private void m_rbtnUseRegExp_CheckedChanged(object sender, EventArgs e)
		{
			m_btnRegExp.Enabled = m_rbtnUseRegExp.Checked;
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(FwApp.App, "khtpSpecConcordanceCrit");
		}


		/// <summary>
		/// Instead of reloading the list, clear the results if we are needing a reload.
		/// This will force the user to hit 'Search' explicitly, to prevent automatic reloading
		/// when the search might take a long time. LT-6967
		/// </summary>
		private void ClearResults()
		{
			LoadMatches(false);
		}

		/// <summary>
		/// Load the matches based upon the state of the ConcordanceControl.
		/// </summary>
		internal protected void LoadMatches()
		{
			LoadMatches(ConcordanceItemsVh.LoadRequested);
		}

		internal protected virtual void LoadMatches(bool fLoadVirtualProperty)
		{
			IVwVirtualHandler vh;
			if (m_cache.TryGetVirtualHandler(WordformInventory.MatchingConcordanceItemsFlid(m_cache), out vh))
			{
				WordformInventory wfi = m_cache.LangProject.WordformInventoryOA as WordformInventory;
				ConcordanceItemsVirtualHandler civh = vh as ConcordanceItemsVirtualHandler;
				// this will indirectly call SearchForMatches() in order to Cache the results.
				civh.LoadRequested = fLoadVirtualProperty;
				civh.ConcordanceControl = this;
				civh.UpdateList(null);
			}
		}

		internal protected virtual List<int> SearchForMatches()
		{
			if (m_fObjectConcorded)
				return FindMatchingItems();
			List<int> rghvoMatching = null;
			bool fCreatedProgressState = false;
			using (new WaitCursor(this))
			{
				if (ConcordanceItemsVh.Progress is NullProgressState)
				{
					ConcordanceItemsVh.Progress = FwXWindow.CreateMilestoneProgressState(m_mediator);
					fCreatedProgressState = true;
				}
				string sMatch = m_tbSearchText.Text.Trim();
				if (sMatch.Length == 0)
					return new List<int>();
				if (sMatch.Length > 1000)
				{
					sMatch = sMatch.Substring(0, 1000);
					MessageBox.Show(ITextStrings.ksMatchStringTooLong, ITextStrings.ksWarning);
					m_tbSearchText.Text = sMatch;
				}
				int ws = (m_cbWritingSystem.SelectedItem as ILgWritingSystem).Hvo;

				ConcordLine conc = (ConcordLine)m_cbLine.SelectedItem;
				switch (conc.Line)
				{
					case ConcordanceLines.kBaseline:
						rghvoMatching = UpdateConcordanceForBaseline(sMatch, ws);
						break;
					case ConcordanceLines.kWord:
						rghvoMatching = UpdateConcordanceForWord(sMatch, ws);
						break;
					case ConcordanceLines.kMorphemes:
						rghvoMatching = UpdateConcordanceForMorphemes(sMatch, ws);
						break;
					case ConcordanceLines.kLexEntry:
						rghvoMatching = UpdateConcordanceForLexEntry(sMatch, ws);
						break;
					case ConcordanceLines.kLexGloss:
						rghvoMatching = UpdateConcordanceForLexGloss(sMatch, ws);
						break;
					case ConcordanceLines.kWordGloss:
						rghvoMatching = UpdateConcordanceForWordGloss(sMatch, ws);
						break;
					case ConcordanceLines.kFreeTranslation:
						rghvoMatching = UpdateConcordanceForFreeTranslation(sMatch, ws);
						break;
					case ConcordanceLines.kLiteralTranslation:
						rghvoMatching = UpdateConcordanceForLiteralTranslation(sMatch, ws);
						break;
					case ConcordanceLines.kNote:
						rghvoMatching = UpdateConcordanceForNote(sMatch, ws);
						break;
					default:
						rghvoMatching = new List<int>();
						break;
				}
			}
			if (fCreatedProgressState)
			{
				ConcordanceItemsVh.Progress.Dispose();
				ConcordanceItemsVh.Progress = null;
			}
			return rghvoMatching;
		}

		private List<int> FindMatchingItems()
		{
			List<int> rghvoMatching = null;
			int clid = m_cache.GetClassOfObject(m_hvoMatch);
			switch (clid)
			{
				case WfiAnalysis.kclsidWfiAnalysis:
					rghvoMatching = UpdateConcordanceForObject("fnConcordForAnalysis");
					break;
				case PartOfSpeech.kclsidPartOfSpeech:
					rghvoMatching = UpdateConcordanceForObject("fnConcordForPartOfSpeech");
					break;
				case LexEntry.kclsidLexEntry:
					rghvoMatching = UpdateConcordanceForObject("fnConcordForLexEntryHvo");
					break;
				case LexSense.kclsidLexSense:
					rghvoMatching = UpdateConcordanceForObject("fnConcordForLexSense");
					break;
				case WfiGloss.kclsidWfiGloss:
					rghvoMatching = UpdateConcordanceForObject("fnConcordForWfiGloss");
					break;
				default:
					if (m_cache.ClassIsOrInheritsFrom((uint)clid, (uint)MoForm.kclsidMoForm))
						rghvoMatching = UpdateConcordanceForObject("fnConcordForMoForm");
					break;
			}
			if (rghvoMatching != null)
				return rghvoMatching;
			else
				return new List<int>();
		}

		private List<int> UpdateConcordanceForObject(string sFnName)
		{
			// Minimally, we need to filter out scripture if none is selected, or TE is not installed.
			// Otherwise, we need to include the selected scripture in our search.
			List<int> hvoScriptureIds = GetScriptureFilterIds();
			string sqlScriptureFilter = "''";
			if (hvoScriptureIds.Count > 0)
				sqlScriptureFilter = String.Format("'{0}'", CmObject.JoinIds(hvoScriptureIds.ToArray(), ","));
			StringBuilder sb = new StringBuilder();
			sb.Append("SELECT BeginObject, AnnotationId\n");
			sb.AppendFormat("FROM dbo.{0}({1}, {2}, {3})\n", sFnName,
				(int)FDO.Ling.Text.TextTags.kflidContents, m_hvoMatch, sqlScriptureFilter);
			sb.Append("ORDER BY BeginObject, BeginOffset, AnnotationId\n");
			return UpdateConcordForAnnotations(sb.ToString(), null, 0);
		}

		private void m_btnSearch_Click(object sender, EventArgs e)
		{
			LoadMatches(true);
		}

		void m_tbSearchText_TextChanged(object sender, EventArgs e)
		{
			m_btnSearch.Enabled = m_tbSearchText.Text.Length > 0;
		}

		private void m_btnRegExp_Click(object sender, EventArgs e)
		{
			m_regexContextMenu.Show(m_btnRegExp, new System.Drawing.Point(m_btnRegExp.Width, 0));
		}

		private void m_lnkSpecify_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			Debug.Assert(m_clerk.CurrentObject == null || m_clerk.CurrentObject is CmAnnotation);
			if (m_fObjectConcorded)
			{
				ICmObject cmCurrent = null;
				if (m_clerk.CurrentObject as CmAnnotation != null)
					cmCurrent = (m_clerk.CurrentObject as CmAnnotation).InstanceOfRA;
				ITsString tss = null;
				int ws = 0;
				if (cmCurrent != null)
				{
					int hvoOwner = cmCurrent.OwnerHVO;
					int hvoOwner2 = m_cache.GetOwnerOfObject(hvoOwner);
					if (m_cache.GetClassOfObject(hvoOwner2) == WfiAnalysis.kclsidWfiAnalysis)
						hvoOwner2 = m_cache.GetOwnerOfObject(hvoOwner2);
					if (m_cache.GetClassOfObject(hvoOwner2) == WfiWordform.kclsidWfiWordform)
					{
						MultiUnicodeAccessor mua = new MultiUnicodeAccessor(m_cache, hvoOwner2,
							(int)WfiWordform.WfiWordformTags.kflidForm, "WfiWordform_Form");
						tss = mua.BestVernacularAlternative;
						ITsTextProps ttp = tss.get_PropertiesAt(0);
						int nVar;
						ws = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
					}
				}
				if (tss == null)
				{
					ws = m_cache.DefaultVernWs;
					tss = m_cache.MakeVernTss("");
				}
				SetDefaultVisibilityOfItems(true, String.Empty);
				m_fObjectConcorded = false;
				SetConcordanceLine(ConcordanceLines.kWord);
				SetWritingSystem(ws);
				m_tbSearchText.Tss = tss;
			}
			else if (m_hvoMatch != 0)
			{
				InitializeConcordanceSearch(CmObject.CreateFromDBObject(m_cache, m_hvoMatch));
			}
		}

		#endregion

		#region Other methods...

		public static int MaxConcordanceMatches()
		{
			// TODO: pull this value from the registry? or make it a settable option?
			return 10000;
		}

		private void FillLineComboList()
		{
			m_cbLine.Items.Clear();
			m_cbLine.Items.Add(new ConcordLine(SIL.FieldWorks.IText.ITextStrings.ksBaseline,
				(int)LangProject.kwsVerns,
				ConcordanceLines.kBaseline));
			m_cbLine.Items.Add(new ConcordLine(SIL.FieldWorks.IText.ITextStrings.ksWord,
				(int)LangProject.kwsVerns,
				ConcordanceLines.kWord));
			m_cbLine.Items.Add(new ConcordLine(SIL.FieldWorks.IText.ITextStrings.ksMorphemes,
				(int)LangProject.kwsVerns,
				ConcordanceLines.kMorphemes));
			m_cbLine.Items.Add(new ConcordLine(SIL.FieldWorks.IText.ITextStrings.ksLexEntry,
				(int)LangProject.kwsVerns,
				ConcordanceLines.kLexEntry));
			m_cbLine.Items.Add(new ConcordLine(SIL.FieldWorks.IText.ITextStrings.ksLexGloss,
				(int)LangProject.kwsAnals,
				ConcordanceLines.kLexGloss));
			m_cbLine.Items.Add(new ConcordLine(SIL.FieldWorks.IText.ITextStrings.ksWordGloss,
				(int)LangProject.kwsAnals,
				ConcordanceLines.kWordGloss));
			m_cbLine.Items.Add(new ConcordLine(SIL.FieldWorks.IText.ITextStrings.ksFreeTranslation,
				(int)LangProject.kwsAnals,
				ConcordanceLines.kFreeTranslation));
			m_cbLine.Items.Add(new ConcordLine(SIL.FieldWorks.IText.ITextStrings.ksLiteralTranslation,
				(int)LangProject.kwsAnals,
				ConcordanceLines.kLiteralTranslation));
			m_cbLine.Items.Add(new ConcordLine(SIL.FieldWorks.IText.ITextStrings.ksNote,
				(int)LangProject.kwsAnals,
				ConcordanceLines.kNote));
			m_cbLine.SelectedIndex = 0;
		}

		internal void SetConcordanceLine(ConcordanceLines line)
		{
			int idx = 0;
			for (int i = 0; i < m_cbLine.Items.Count; ++i)
			{
				if ((m_cbLine.Items[i] as ConcordLine).Line == line)
				{
					idx = i;
					break;
				}
			}
			if (m_cbLine.SelectedIndex != idx)
				m_cbLine.SelectedIndex = idx;
		}

		private void FillWritingSystemCombo(int wsMagic)
		{
			m_cbWritingSystem.Items.Clear();
			int wsSet = 0;
			switch (wsMagic)
			{
				case LangProject.kwsVerns:
					foreach (ILgWritingSystem lgws in m_cache.LangProject.CurVernWssRS)
					{
						m_cbWritingSystem.Items.Add(lgws);
					}
					wsSet = m_cache.DefaultVernWs;
					break;
				case LangProject.kwsAnals:
					foreach (ILgWritingSystem lgws in m_cache.LangProject.CurAnalysisWssRS)
					{
						m_cbWritingSystem.Items.Add(lgws);
					}
					wsSet = m_cache.DefaultAnalWs;
					break;
			}
			// now try to add the ws of the tss string in our textbox if it hasn't already been added.
			if (m_tbSearchText.Tss.Length != 0)
				wsSet = StringUtils.GetWsAtOffset(m_tbSearchText.Tss, 0);
			SetWritingSystem(wsSet);
		}

		private void SetWritingSystem(int ws)
		{
			int idx = -1;
			for (int i = 0; i < m_cbWritingSystem.Items.Count; ++i)
			{
				if ((m_cbWritingSystem.Items[i] as ILgWritingSystem).Hvo == ws)
				{
					idx = i;
					break;
				}
			}
			if (idx == -1)
			{
				foreach (NamedWritingSystem nws in m_cache.LangProject.GetActiveNamedWritingSystems())
				{
					if (nws.Hvo == ws)
					{
						ILgWritingSystem lgws = nws.GetLgWritingSystem(m_cache);
						m_cbWritingSystem.Items.Add(lgws);
						idx = m_cbWritingSystem.Items.IndexOf(lgws);
					}
				}
			}
			if (idx != -1 && m_cbWritingSystem.SelectedIndex != idx)
				m_cbWritingSystem.SelectedIndex = idx;
		}

		private List<int> UpdateConcordanceForBaseline(string sMatch, int ws)
		{
			SimpleStringMatcher matcher = GetMatcher(ws) as SimpleStringMatcher;
			if (!matcher.IsValid())
				return new List<int>();
			//ITsStrFactory tsf = null;
			//tsf = TsStrFactoryClass.Create();
			BaseFDOPropertyVirtualHandler paraSegmentsVh = BaseFDOPropertyVirtualHandler.GetInstalledHandler(m_cache, "StTxtPara", "Segments")
				as BaseFDOPropertyVirtualHandler;
			ISilDataAccess sda = m_cache.MainCacheAccessor;

			// detect whether or not we want to filter scripture in the concordance
			string sQry = SearchQueryForParagraphContents(sMatch);
			int[] hvosStTxtPara = DbOps.ReadIntArrayFromCommand(m_cache, sQry, null);
			// preload paragraph contents.
			m_cache.PreloadIfMissing(hvosStTxtPara, (int)StTxtPara.StTxtParaTags.kflidContents, 0, false);
			List<int> rghvoMatching = new List<int>();
			Set<int> rghvoParasToLoadSegs = new Set<int>();
			int cPara = 0;
			try
			{
				foreach (IStTxtPara para in new FdoObjectSet<IStTxtPara>(m_cache, hvosStTxtPara, false, typeof(StTxtPara)))
				{
					++cPara;
					// Find occurrences of the string in this paragraph.
					bool fOk = matcher.Matches(para.Contents.UnderlyingTsString);
					if (fOk)
					{
						// Create occurrences for each match.
						// Make non-twfic annotations.
						List<MatchRangePair> results = matcher.GetAllResults();
						if (results.Count > 0)
						{
							foreach (MatchRangePair range in results)
							{
								// make a typeless (non-twfic and non-punctuation) Annotation.
								int hvoCbaRaw = CmBaseAnnotation.CreateDummyAnnotation(m_cache, para.Hvo, 0, range.IchMin, range.IchLim, 0);
								rghvoMatching.Add(hvoCbaRaw);
								// if we haven't loaded segments for this paragraph we need to do so for the Ref column sake.
								if (!paraSegmentsVh.IsPropInCache(sda, para.Hvo, 0))
									rghvoParasToLoadSegs.Add(para.Hvo);
								if (rghvoMatching.Count >= MaxConcordanceMatches())
								{
									MessageBox.Show(String.Format(ITextStrings.ksShowingOnlyTheFirstXXXMatches,
										rghvoMatching.Count, cPara, hvosStTxtPara.Length), ITextStrings.ksNotice,
										MessageBoxButtons.OK, MessageBoxIcon.Information);
									return rghvoMatching;
								}
							}
						}
					}
				}
				return rghvoMatching;
			}
			finally
			{
				// load segments for this paragraph we need to do for the Ref column sake.
				//ParagraphParser.ConcordParagraphs(m_cache, rghvoParasToLoadSegs.ToArray(), new NullProgressState());
			}
		}

		private string SearchQueryForParagraphContents(string sMatch)
		{
			string sFilterString = FirstPassFilterString(sMatch);
			return SearchQueryForParagraphContentsWithFirstPassFilter(sFilterString);
		}

		private string SearchQueryForParagraphContentsWithFirstPassFilter(string sFilterString)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("SELECT p.Id FROM StTxtPara_ p\n");
			sb.Append("JOIN StText_ t ON t.Id=p.Owner$\n");
			sb.AppendFormat("WHERE p.Contents LIKE N'%{0}%' AND ({1})",
							sFilterString, ConstructScriptureFilterStringForWhereStatement());
			return sb.ToString();
		}

		ConcordanceItemsVirtualHandler ConcordanceItemsVh
		{
			get
			{
				IVwVirtualHandler vh;
				if (m_cache.TryGetVirtualHandler(WordformInventory.MatchingConcordanceItemsFlid(m_cache), out vh))
				{
					return vh as ConcordanceItemsVirtualHandler;
				}
				return null;
			}
		}

		private List<int> UpdateConcordanceForWord(string sMatch, int ws)
		{
			IMatcher matcher = GetMatcher(ws);
			if (!matcher.IsValid())
				return new List<int>();
			// To allow searching word forms in alternate case, always include all paragraphs.
			// Optimize JohnT: if we are ever searching using exact matching, we could possibly do
			// better using an approach that retrieves only the paragraphs whose contents are 'like sMatch'.
			List<int> hvosStText = new List<int>(m_cache.GetVectorProperty(m_cache.LangProject.Hvo, LangProject.InterlinearTextsFlid(m_cache), false));
			// first collect matching annotations
			List<int> rgMatchingAnnotations = ParagraphParser.ConcordParagraphsOfTexts(m_cache, hvosStText.ToArray(),
				ConcordanceItemsVh.Progress, matcher, ConcordanceLines.kWord);
			// Enhance: weneed to bulk load when sorting/filtering on Reference column.
			//Set<int> rgHvoPara = new Set<int>();
			//ISilDataAccess sda = m_cache.MainCacheAccessor;
			//foreach (int twfic in rgMatchingAnnotations)
			//{
			//    int hvoPara = sda.get_ObjectProp(twfic, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject);
			//    rgHvoPara.Add(hvoPara);
			//}
			// ParagraphParser.ConcordParagraphs(m_cache, rgHvoPara.ToArray(), ConcordanceItemsVh.Progress, null, ConcordanceLines.kWord);
			return rgMatchingAnnotations;
			//SimpleStringMatcher matcher = GetMatcher(ws) as SimpleStringMatcher;
			//if (!matcher.IsValid())
			//    return new List<int>();
			//BaseFDOPropertyVirtualHandler paraSegmentsVh = BaseFDOPropertyVirtualHandler.GetInstalledHandler(m_cache, "StTxtPara", "Segments")
			//    as BaseFDOPropertyVirtualHandler;
			//string sQry = SearchQueryForParagraphContents(sMatch);
			//int[] hvosStTxtPara = DbOps.ReadIntArrayFromCommand(m_cache, sQry, null);
			//// first collect matching annotations
			//List<int> rgMatchingAnnotations = ParagraphParser.ConcordParagraphs(m_cache, hvosStTxtPara, new NullProgressState(), matcher, ConcordanceLines.kWord);
			//Set<int> rghvoParasToLoadSegs = new Set<int>();
			//ISilDataAccess sda = m_cache.MainCacheAccessor;
			//foreach (int twfic in rgMatchingAnnotations)
			//{
			//    int hvoPara = sda.get_ObjectProp(twfic, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject);
			//    // if we haven't loaded segments for this paragraph we need to do so for the Ref column sake.
			//    if (!paraSegmentsVh.IsPropInCache(sda, hvoPara, 0))
			//        rghvoParasToLoadSegs.Add(hvoPara);
			//}
			//ParagraphParser.ConcordParagraphs(m_cache, rghvoParasToLoadSegs.ToArray(), new NullProgressState());
			//return rgMatchingAnnotations;
		}

		private List<int> GetScriptureFilterIds()
		{
			// get the virtual handler
			InterlinearTextsVirtualHandler vh = BaseVirtualHandler.GetInstalledHandler(m_cache,
				"LangProject", "InterlinearTexts") as InterlinearTextsVirtualHandler;
			// see if we can get scripture ids
			List<int> hvoScriptureIds = vh.GetScriptureIds();
			return hvoScriptureIds;
		}

		/// <summary>
		/// If we're not matching diacritics, then our first pass has to allow for diacritics
		/// in the baseline text, but we can at least filter on all the non-diacritic chars in
		/// sequence to maybe eliminate some paragraphs at this initial stage.
		/// </summary>
		/// <param name="sMatch"></param>
		/// <returns></returns>
		private string FirstPassFilterString(string sMatch)
		{
			if (m_rbtnUseRegExp.Checked || !m_chkMatchCase.Checked)
				return "%";		// can we do better?
			StringBuilder sb = new StringBuilder(sMatch);
			if (!m_chkMatchDiacritics.Checked)
			{
				// Allow any number of diacritics (or other chars for that matter, alas) between
				// every nondiacritic character in the string.
				for (int ich = sb.Length - 1; ich > 0; --ich)
				{
					if (Icu.IsDiacritic(sb[ich]))
						sb[ich] = '%';
					else
						sb.Insert(ich, '%');
				}
			}
			// Add beginning and ending wildcards as needed.
			if (m_rbtnAnywhere.Checked || m_rbtnAtEnd.Checked)
				sb.Insert(0, '%');
			if (m_rbtnAnywhere.Checked || m_rbtnAtStart.Checked)
				sb.Append('%');
			// Get rid of any doubled wildcard markers.  Doing this 3 times reduces as many as
			// 8 consecutive markers to a single one.
			sb.Replace("%%", "%");
			sb.Replace("%%", "%");
			sb.Replace("%%", "%");
			// Double single quotes to quote them as part of an SQL string.
			return sb.ToString().Replace("'", "''");
		}

		private List<int> UpdateConcordanceForMorphemes(string sMatch, int ws)
		{
			string sFilter = FirstPassFilterString(sMatch);

			// Minimally, we need to filter out scripture if none is selected, or TE is not installed.
			// Otherwise, we need to include the selected scripture in our search.
			List<int> hvoScriptureIds = GetScriptureFilterIds();
			string sqlScriptureFilter = "''";
			if (hvoScriptureIds.Count > 0)
			{
				sqlScriptureFilter = String.Format("'{0}'", CmObject.JoinIds(hvoScriptureIds.ToArray(), ","));
			}

			StringBuilder sb = new StringBuilder();
			sb.Append("SELECT BeginObject, AnnotationId, Txt\n");
			sb.AppendFormat("FROM dbo.fnConcordForMorphemes({0}, '{1}', {2}, {3}) f\n",
				(int)FDO.Ling.Text.TextTags.kflidContents, sFilter, ws, sqlScriptureFilter);
			sb.Append("ORDER BY BeginObject, Ord, AnnotationId\n");
			return UpdateConcordForAnnotations(sb.ToString(), sMatch, ws);
		}

		/// <summary>
		/// Minimally, we need to filter out scripture if none is selected, or TE is not installed.
		/// Otherwise, we need to include the selected scripture in our search.
		/// NOTE: assumes that the TextOwnFlid in the SQL is an StText that owns the paragraph
		/// referred to by CmBaseAnnotation.BeginObject
		/// </summary>
		/// <returns></returns>
		private string ConstructScriptureFilterStringForWhereStatement()
		{
			string sqlScriptureFilter = String.Format("t.OwnFlid$={0}", (int)FDO.Ling.Text.TextTags.kflidContents);
			List<int> hvoScriptureIds = GetScriptureFilterIds();
			if (hvoScriptureIds.Count > 0)
			{
				sqlScriptureFilter = String.Format("{0} or t.id in ({1})", sqlScriptureFilter, CmObject.JoinIds(hvoScriptureIds.ToArray(), ","));
			}
			return sqlScriptureFilter;
		}

		private List<int> UpdateConcordForAnnotations(string sQry, string sMatch, int ws)
		{
			// 1) get a list of annotations matching specified morpheme by querying possible
			//    matches and doing appropriate string matching on the forms
			Dictionary<int, List<int>> dictAnaPara = LoadConcordData(sQry, sMatch, ws);

			// 2) ConcordParagraphs on Set<paragraphs> referenced by those annotations.BeginObject
			// Enhance: Bulk load these only when we're sorting/filtering on the Reference column.
			List<int> rgHvoPara = new List<int>(dictAnaPara.Keys);
			ParagraphParser.ConcordParagraphs(m_cache, rgHvoPara.ToArray(), ConcordanceItemsVh.Progress);

			// 3) set MatchingConcordanceItems to the list of annotations.
			List<int> rgHvoAnno = new List<int>();
			foreach (List<int> rgHvo in dictAnaPara.Values)
				rgHvoAnno.AddRange(rgHvo);

			// TODO: verify that all of these values are still valid.  Step 2 may have possibly
			// invalidated some of them according to EricP.
			return rgHvoAnno;
		}

		private Dictionary<int, List<int>> LoadConcordData(string sQry, string sMatch, int ws)
		{
			Dictionary<int, List<int>> dictParaAna = new Dictionary<int, List<int>>();
			IMatcher matcher;
			if (!String.IsNullOrEmpty(sMatch))
			{
				matcher = GetMatcher(ws);
				if (matcher != null && !matcher.IsValid())
					return dictParaAna;
			}
			else
			{
				matcher = null;
			}
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			IOleDbCommand odc = null;
			try
			{
				List<int> list = null;
				m_cache.DatabaseAccessor.CreateCommand(out odc);
				odc.ExecCommand(sQry, (int)SqlStmtType.knSqlStmtSelectWithOneRowset);
				odc.GetRowset(0);
				bool fMoreRows;
				odc.NextRow(out fMoreRows);
				bool fIsNull;
				uint cbSpaceTaken;
				int hvoKey = 0;
				using (ArrayPtr rgHvo = MarshalEx.ArrayToNative(1, typeof(uint)))
				{
					using (ArrayPtr rgText = MarshalEx.ArrayToNative(4000, typeof(char)))
					{
						for (; fMoreRows; odc.NextRow(out fMoreRows))
						{
							odc.GetColValue(1, rgHvo, rgHvo.Size, out cbSpaceTaken, out fIsNull, 0);
							if (fIsNull)
								continue;
							int hvoPara = (int)(uint)Marshal.PtrToStructure((IntPtr)rgHvo, typeof(uint));
							if (hvoPara == 0)
								continue;
							odc.GetColValue(2, rgHvo, rgHvo.Size, out cbSpaceTaken, out fIsNull, 0);
							if (fIsNull)
								continue;
							int hvoAnno = (int)(uint)Marshal.PtrToStructure((IntPtr)rgHvo, typeof(uint));
							if (matcher != null)
							{
								odc.GetColValue(3, rgText, rgText.Size, out cbSpaceTaken, out fIsNull, 0);
								if (fIsNull)
									continue;
								byte[] rgbTemp = (byte[])MarshalEx.NativeToArray(rgText, (int)cbSpaceTaken, typeof(byte));
								string sForm = Encoding.Unicode.GetString(rgbTemp);
								if (matcher != null && !matcher.Matches(tsf.MakeString(sForm, ws)))
									continue;
							}
							if (hvoKey != hvoPara)
							{
								list = new List<int>();
								hvoKey = hvoPara;
								dictParaAna[hvoKey] = list;
							}
							list.Add(hvoAnno);
						}
					}
				}
			}
			finally
			{
				if ((odc != null) && Marshal.IsComObject(odc))
				{
					Marshal.ReleaseComObject(odc);
					odc = null;
				}
			}
			return dictParaAna;
		}

		private IMatcher GetMatcher(int ws)
		{
			IMatcher matcher = null;

			SetupSearchPattern(ws);

			if (m_rbtnUseRegExp.Checked)
				matcher = new RegExpMatcher(m_vwPattern);
			else if (m_rbtnWholeItem.Checked)
			{
				// See whether we can use the MUCH more efficient ExactLiteralMatcher
				if (!m_vwPattern.UseRegularExpressions
					&& m_vwPattern.MatchDiacritics
					&& m_vwPattern.MatchOldWritingSystem
					&& m_vwPattern.Pattern.RunCount == 1)
				{
					string target = m_vwPattern.Pattern.Text;
					int nVar;
					int wsMatch = m_vwPattern.Pattern.get_Properties(0).GetIntPropValues((int) FwTextPropType.ktptWs,
																					out nVar);
					if (m_vwPattern.MatchCase)
						return new ExactLiteralMatcher(target, wsMatch);
					return new ExactCaseInsensitiveLiteralMatcher(target, wsMatch);
				}
				matcher = new ExactMatcher(m_vwPattern);
			}
			else if (m_rbtnAtEnd.Checked)
				matcher = new EndMatcher(m_vwPattern);
			else if (m_rbtnAtStart.Checked)
				matcher = new BeginMatcher(m_vwPattern);
			else
				matcher = new AnywhereMatcher(m_vwPattern);

			if (!matcher.IsValid())
			{
				if (matcher is RegExpMatcher)
					ShowRegExpMatcherError(matcher);
				else
					MessageBox.Show(this, matcher.ErrorMessage(), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			return matcher;
		}

		private void ShowRegExpMatcherError(IMatcher matcher)
		{
			string errMsg = String.Format("Invalid regular expression", matcher.ErrorMessage());
			MessageBox.Show(this, errMsg, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		private void SetupSearchPattern(int ws)
		{
			m_vwPattern.MatchWholeWord = m_rbtnWholeItem.Checked;
			m_vwPattern.UseRegularExpressions = m_rbtnUseRegExp.Checked;
			m_vwPattern.MatchDiacritics = m_chkMatchDiacritics.Checked;
			m_vwPattern.MatchCase = m_chkMatchCase.Checked;
			m_vwPattern.Pattern = m_tbSearchText.Tss;
			m_vwPattern.IcuLocale = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(ws);
			m_vwPattern.MatchOldWritingSystem = true;
		}

		private IMatcher GetRegExpMatcher(int ws)
		{
			SetupSearchPattern(ws);
			IMatcher matcher = new RegExpMatcher(m_vwPattern);
			if (!matcher.IsValid())
			{
				ShowRegExpMatcherError(matcher);
			}
			return matcher;
		}

		private List<int> UpdateConcordanceForLexEntry(string sMatch, int ws)
		{
			string sFilter = FirstPassFilterString(sMatch);

			// Minimally, we need to filter out scripture if none is selected, or TE is not installed.
			// Otherwise, we need to include the selected scripture in our search.
			List<int> hvoScriptureIds = GetScriptureFilterIds();
			string sqlScriptureFilter = "''";
			if (hvoScriptureIds.Count > 0)
			{
				sqlScriptureFilter = String.Format("'{0}'", CmObject.JoinIds(hvoScriptureIds.ToArray(), ","));
			}

			StringBuilder sb = new StringBuilder();
			sb.Append("SELECT BeginObject, AnnotationId, Txt\n");
			sb.AppendFormat("FROM dbo.fnConcordForLexEntry({0}, '{1}', {2}, {3}) f\n",
				(int)FDO.Ling.Text.TextTags.kflidContents, sFilter, ws, sqlScriptureFilter);
			sb.Append("ORDER BY BeginObject, Ord, AnnotationId\n");
			return UpdateConcordForAnnotations(sb.ToString(), sMatch, ws);
		}

		private List<int> UpdateConcordanceForLexGloss(string sMatch, int ws)
		{
			string sFilter = FirstPassFilterString(sMatch);

			// Minimally, we need to filter out scripture if none is selected, or TE is not installed.
			// Otherwise, we need to include the selected scripture in our search.
			List<int> hvoScriptureIds = GetScriptureFilterIds();
			string sqlScriptureFilter = "''";
			if (hvoScriptureIds.Count > 0)
			{
				sqlScriptureFilter = String.Format("'{0}'", CmObject.JoinIds(hvoScriptureIds.ToArray(), ","));
			}

			StringBuilder sb = new StringBuilder();
			sb.Append("SELECT BeginObject, AnnotationId, Txt\n");
			sb.AppendFormat("FROM dbo.fnConcordForLexGloss({0}, '{1}', {2}, {3}) f\n",
				(int)FDO.Ling.Text.TextTags.kflidContents, sFilter, ws, sqlScriptureFilter);
			sb.Append("ORDER BY BeginObject, Ord, AnnotationId\n");
			return UpdateConcordForAnnotations(sb.ToString(), sMatch, ws);
		}

		private List<int> UpdateConcordanceForWordGloss(string sMatch, int ws)
		{
			string sFilter = FirstPassFilterString(sMatch);
			StringBuilder sb = new StringBuilder();
			sb.Append("SELECT a.BeginObject, a.Id, wgf.Txt\n");
			sb.Append("FROM CmBaseAnnotation_ a\n");
			sb.Append("JOIN WfiGloss_Form wgf ON wgf.Obj=a.InstanceOf AND ");
			sb.AppendFormat("wgf.Txt LIKE N'{0}' AND wgf.Ws = {1}\n", sFilter, ws);
			// append the JOIN statements to get the StText_ records that owns the CmBaseAnnotation.BeginObject (StTxtPara).
			sb.Append("JOIN StTxtPara_ p ON p.Id=a.BeginObject\n");
			sb.Append("JOIN StText_ t ON t.Id=p.Owner$\n");

			string sqlScriptureFilter = ConstructScriptureFilterStringForWhereStatement();
			sb.AppendFormat("WHERE {0}\n", sqlScriptureFilter);
			sb.Append("ORDER BY a.BeginObject, a.Id");
			return UpdateConcordForAnnotations(sb.ToString(), sMatch, ws);
		}

		private List<int> UpdateConcordanceForFreeTranslation(string sMatch, int ws)
		{
			return UpdateConcordForSegmentAnnotation(sMatch, ws, LangProject.kguidAnnFreeTranslation);
		}

		private List<int> UpdateConcordForSegmentAnnotation(string sMatch, int ws, String sGuidType)
		{
			string sFilter = FirstPassFilterString(sMatch);
			StringBuilder sb = new StringBuilder();
			sb.Append("SELECT a.BeginObject, a.Id, cc.Txt\n");
			sb.Append("FROM CmAnnotation ca\n");
			sb.Append("JOIN CmIndirectAnnotation_AppliesTo ref ON ref.Src = ca.id\n");
			sb.Append("JOIN CmBaseAnnotation_ a ON a.id = ref.Dst AND a.AnnotationType = ");
			sb.AppendFormat("(SELECT Id FROM CmObject WHERE Guid$ = '{0}')\n", LangProject.kguidAnnTextSegment);
			sb.AppendFormat("JOIN CmAnnotation_Comment cc ON cc.obj = ca.id AND cc.ws = {0} AND ", ws);
			sb.AppendFormat("cc.Txt LIKE '{0}'\n", sFilter);
			// append the JOIN statements to get the StText_ records that owns the CmBaseAnnotation.BeginObject (StTxtPara).
			sb.Append("JOIN StTxtPara_ p ON p.Id=a.BeginObject\n");
			sb.Append("JOIN StText_ t ON t.Id=p.Owner$\n");

			sb.AppendFormat("WHERE (ca.AnnotationType = (SELECT Id FROM CmObject WHERE Guid$ = '{0}') AND ({1}))\n",
				sGuidType, ConstructScriptureFilterStringForWhereStatement());
			sb.Append("ORDER BY a.BeginObject, a.id\n");
			return UpdateConcordForAnnotations(sb.ToString(), sMatch, ws);
		}

		private List<int> UpdateConcordanceForLiteralTranslation(string sMatch, int ws)
		{
			return UpdateConcordForSegmentAnnotation(sMatch, ws, LangProject.kguidAnnLiteralTranslation);
		}

		private List<int> UpdateConcordanceForNote(string sMatch, int ws)
		{
			return UpdateConcordForSegmentAnnotation(sMatch, ws, LangProject.kguidAnnNote.ToString());
		}

		private bool InitializeConcordanceSearch(string sMatch, int ws, ConcordanceLines line)
		{
			SetDefaultVisibilityOfItems(true, String.Empty);
			m_fObjectConcorded = false;
			if (String.IsNullOrEmpty(sMatch))
				return false;
			SetConcordanceLine(line);
			SetWritingSystem(ws);
			m_rbtnUseRegExp.Checked = false;
			m_chkMatchDiacritics.Checked = true;
			m_rbtnWholeItem.Checked = true;
			m_tbSearchText.WritingSystemCode = ws;
			m_tbSearchText.Text = sMatch;
			m_btnSearch.Enabled = true;
			m_btnSearch_Click(this, new EventArgs());
			SaveSettings();
			return true;
		}

		private void SetDefaultVisibilityOfItems(bool fDefault, string sConcordedOn)
		{
			m_fwtbItem.Enabled = m_fwtbItem.Visible = !fDefault;
			if (fDefault)
			{
				m_lblTop.Text = ITextStrings.ksToSpecifyAConcordance_;
				m_lnkSpecify.Text = ITextStrings.ksBackToConcordedItem;
				m_lnkSpecify.Enabled = m_lnkSpecify.Visible = (m_hvoMatch != 0);
			}
			else
			{
				m_lblTop.Text = String.Format(ITextStrings.ksConcordedOn0, sConcordedOn);
				m_lnkSpecify.Text = ITextStrings.ksSpecifyConcordanceCriteria_;
				m_lnkSpecify.Enabled = m_lnkSpecify.Visible = true;
				m_fwtbItem.Location = new Point(m_lblTop.Location.X + m_lblTop.Width + 10, m_lblTop.Location.Y);
				m_fwtbItem.BorderStyle = BorderStyle.None;
			}
			label2.Visible = fDefault;
			label3.Visible = fDefault;
			label4.Visible = fDefault;
			m_cbLine.Visible = fDefault;
			m_cbWritingSystem.Visible = fDefault;
			m_tbSearchText.Visible = fDefault;
			m_btnRegExp.Visible = fDefault;
			m_chkMatchCase.Visible = fDefault;
			m_chkMatchDiacritics.Visible = fDefault;
			groupBox1.Visible = fDefault;
			m_btnSearch.Visible = fDefault;
		}

		private bool InitializeConcordanceSearch(ICmObject cmo)
		{
			string sType = cmo.GetType().Name;
			string sTag = m_mediator.StringTbl.GetString(sType, "ClassNames");
			SetDefaultVisibilityOfItems(false, sTag);
			m_fObjectConcorded = true;
			m_hvoMatch = cmo.Hvo;
			ITsString tssObj = cmo.ShortNameTSS;
			ITsTextProps ttpObj = tssObj.get_PropertiesAt(0);
			int nVar;
			int ws = ttpObj.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
			m_fwtbItem.WritingSystemCode = (ws > 0) ? ws : m_cache.DefaultVernWs;
			int dyHeight = m_fwtbItem.PreferredHeight;
			if (dyHeight != m_fwtbItem.Height)
				m_fwtbItem.Height = dyHeight;
			m_fwtbItem.Tss = tssObj;
			int dxWidth = m_fwtbItem.PreferredWidth;
			if (dxWidth != m_fwtbItem.Width)
				m_fwtbItem.Width = dxWidth;
			LoadMatches(true);
			return true;
		}
		#endregion

		#region IXCore related (callable) methods

		public bool OnJumpToRecord(object argument)
		{
			CheckDisposed();
			// Check if we're the right tool, and that we have a valid object id.
			string toolName = m_mediator.PropertyTable.GetStringProperty("currentContentControl", null);
			string areaName = m_mediator.PropertyTable.GetStringProperty("areaChoice", null);
			Debug.Assert(!String.IsNullOrEmpty(toolName) && !String.IsNullOrEmpty(areaName));
			if (areaName != "textsWords" || toolName != "concordance")
				return false;
			int hvoTarget = (int)argument;
			if (!m_cache.IsValidObject(hvoTarget))
				return false;
			try
			{
				int clid = m_cache.GetClassOfObject(hvoTarget);
				int ws = 0;
				ITsString tss;
				switch (clid)
				{
					case WfiWordform.kclsidWfiWordform:
						IWfiWordform wwf = WfiWordform.CreateFromDBObject(m_cache, hvoTarget);
						if (wwf.Form != null && wwf.Form.TryWs(LangProject.kwsFirstVern, out ws, out tss))
							InitializeConcordanceSearch(tss.Text, ws, ConcordanceLines.kWord);
						break;
					case LexEntry.kclsidLexEntry:
						InitializeConcordanceSearch(LexEntry.CreateFromDBObject(m_cache, hvoTarget));
						break;
					case LexSense.kclsidLexSense:
						InitializeConcordanceSearch(LexSense.CreateFromDBObject(m_cache, hvoTarget));
						break;
					case WfiAnalysis.kclsidWfiAnalysis:
						InitializeConcordanceSearch(WfiAnalysis.CreateFromDBObject(m_cache, hvoTarget));
						break;
					case PartOfSpeech.kclsidPartOfSpeech:
						InitializeConcordanceSearch(PartOfSpeech.CreateFromDBObject(m_cache, hvoTarget));
						break;
					case WfiGloss.kclsidWfiGloss:
						InitializeConcordanceSearch(WfiGloss.CreateFromDBObject(m_cache, hvoTarget));
						break;
					default:
						if (m_cache.ClassIsOrInheritsFrom((uint)clid, (uint)MoForm.kclsidMoForm))
							InitializeConcordanceSearch(MoForm.CreateFromDBObject(m_cache, hvoTarget));
						break;
				}
			}
			finally
			{
				// indicate that OnJumpToRecord has been handled.
				m_clerk.SuspendLoadingRecordUntilOnJumpToRecord = false;
			}
			return true;
		}

		#endregion
	}

	public class OccurrencesOfSelectedUnit : InterlinearTextsRecordClerk
	{
		ConcordanceControl m_concordanceControl = null;

		internal ConcordanceControl ConcordanceControl
		{
			get { return m_concordanceControl; }
			set
			{
				m_concordanceControl = value;
				// Currently ConcordanceItemsVirtualHandler depends upon our control being in the property table.
				IVwVirtualHandler vh;
				if (Cache.TryGetVirtualHandler(WordformInventory.MatchingConcordanceItemsFlid(Cache), out vh))
				{
					ConcordanceItemsVirtualHandler civh = vh as ConcordanceItemsVirtualHandler;
					civh.ConcordanceControl = m_concordanceControl;
				}
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				m_concordanceControl = null;
			}
			base.Dispose(disposing);
		}

		internal bool NeedToReloadVirtualProperty
		{
			get { return m_list.NeedToReloadVirtualProperty; }
			set { m_list.NeedToReloadVirtualProperty = value; }
		}

		protected override void RefreshAfterInvalidObject()
		{
			ConcordanceControl.LoadMatches(true);
		}
	}

	public class MatchingConcordanceItems : DummyRecordList
	{
		protected override void ForceReloadVirtualProperty(IVwVirtualHandler handler)
		{
			OccurrencesOfSelectedUnit clerk = Clerk as OccurrencesOfSelectedUnit;
			clerk.ConcordanceControl.LoadMatches();
		}

		public override void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			ConcordanceItemsVirtualHandler cvh = VirtualHandler as ConcordanceItemsVirtualHandler;
			// in the special case of including/removing Scripture ids, we want to treat it like a change in a filter
			// so we really want to try to reload rather than clearing our list.
			if (tag == LangProject.InterlinearTextsFlid(m_cache))
			{
				cvh.LoadRequested = true;
			}
			// by default, we don't want to reload this tool due to PropChanges, unless it's our property.
			using (RecordClerk.ListUpdateHelper luh = new RecordClerk.ListUpdateHelper(Clerk))
			{
				base.PropChanged(hvo, tag, ivMin, cvIns, cvDel);
				// we DO want to reload if our property changed, or we've set LoadRequested.
				// otherwise, we'll try to live with things being out of a valid state.
				luh.TriggerPendingReloadOnDispose = (tag == Flid && hvo == m_owningObject.Hvo || cvh.LoadRequested == true);
			}
			// if we delete a text, then we need to clear the list of concordance matches since
			// some of them may be pointing to the deleted text.  See LT-7130.
			if (hvo == m_cache.LangProject.Hvo && tag < 0 && cvIns < cvDel)
			{
				RecordClerk clerk = m_mediator.PropertyTable.GetValue("ActiveClerk") as RecordClerk;
				if (clerk != null && clerk.VirtualFlid == tag && clerk is InterlinearTextsRecordClerk)
					base.NeedToReloadVirtualProperty = true;
			}
		}
	}

	internal class ConcordanceItemsVirtualHandler : FDOSequencePropertyTableVirtualHandler
	{
		ConcordanceControl m_concordanceControl = null;
		public ConcordanceItemsVirtualHandler(XmlNode configuration, FdoCache cache)
			: base(configuration, cache)
		{
		}

		bool m_fLoadRequested = false;
		/// <summary>
		/// Used to clear out the values in the cache.
		/// </summary>
		internal bool LoadRequested
		{
			get { return m_fLoadRequested; }
			set { m_fLoadRequested = value; }
		}

		/// <summary>
		/// since we are not trying to sync to a list in the PropertyTable, let's just assume we're always up to date.
		/// </summary>
		protected override bool IsUpToDate
		{
			get
			{
				return true;
			}
		}

		/// <summary>
		/// This loads the list from the control providing a list, rather than storing the list directly in the property table.
		/// </summary>
		/// <returns></returns>
		protected override List<int> GetListForCache()
		{
			List<int> matchingItems = new List<int>();
			m_fLastLoadRequested = LoadRequested;
			if (!LoadRequested)
				return matchingItems;	// empty list;
			if (m_concordanceControl != null && !m_concordanceControl.IsDisposed)
			{
				matchingItems = m_concordanceControl.SearchForMatches();
			}
			return matchingItems;
		}

		/// <summary>
		/// The concordance control to use to load our concordance settings.
		/// </summary>
		internal ConcordanceControl ConcordanceControl
		{
			set { m_concordanceControl = value; }
		}

		public override void Load(int hvo, int tag, int ws, IVwCacheDa cda)
		{
			base.Load(hvo, tag, ws, cda);
			LoadRequested = false;
		}

		protected override List<int> PropertyTableList()
		{
			if ((m_owner as WordformInventory) != null &&
				Cache.MainCacheAccessor.get_IsPropInCache(m_owner.Hvo, this.Tag, this.Type, 0))
			{
				return (m_owner as WordformInventory).MatchingConcordanceItems;
			}
			else
			{
				return new List<int>();
			}
		}

		/// <summary>
		/// Indicated whether the last load was requested or not.
		/// </summary>
		bool m_fLastLoadRequested = false;
		internal bool LastLoadWasRequested
		{
			get { return m_fLastLoadRequested; }
		}


		protected override void UpdatePropertyTableList(int[] hvosForPropertyTable)
		{
			// don't need to do anything to the property table, since we store our control.
		}
	}
}
