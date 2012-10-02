using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;

using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Filters;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.XWorks;
using XCore;
using SIL.FieldWorks.FDO.Application;


namespace SIL.FieldWorks.IText
{
	public partial class ConcordanceControl : UserControl, IxCoreContentControl, IFWDisposable
	{
		Mediator m_mediator;
		XmlNode m_configurationParameters;
		protected FdoCache m_cache;
		protected OccurrencesOfSelectedUnit m_clerk;
		private RegexHelperMenu m_regexContextMenu;
		private IVwPattern m_vwPattern;
		private bool m_fObjectConcorded = false;
		private int m_hvoMatch = 0;
		private IHelpTopicProvider m_helpTopicProvider;

		// True after the first time we do it.
		internal bool HasLoadedMatches { get; private set; }
		// True while loading matches, to prevent recursive call.
		internal bool IsLoadingMatches { get; private set; }

		public ConcordanceControl()
		{
			InitializeComponent();

			m_vwPattern = VwPatternClass.Create();
			this.helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			this.helpProvider.SetShowHelp(this, true);
			m_tbSearchText.SuppressEnter = true;
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
			m_helpTopicProvider = m_mediator.HelpTopicProvider;
			m_configurationParameters = configurationParameters;
			m_cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			string name = XmlUtils.GetAttributeValue(configurationParameters, "clerk");
			m_clerk = (OccurrencesOfSelectedUnit)m_mediator.PropertyTable.GetValue(name);
			if (m_clerk == null)
				m_clerk = RecordClerkFactory.CreateClerk(m_mediator, m_configurationParameters, true) as OccurrencesOfSelectedUnit;
			m_clerk.ConcordanceControl = this;

			m_tbSearchText.WritingSystemFactory = m_cache.LanguageWritingSystemFactoryAccessor;
			m_tbSearchText.StyleSheet = Common.Widgets.FontHeightAdjuster.StyleSheetFromMediator(mediator);
			m_tbSearchText.Text = String.Empty;
			m_tbSearchText.TextChanged += m_tbSearchText_TextChanged;
			m_tbSearchText.KeyDown += m_tbSearchText_KeyDown;
			FillLineComboList();

			m_fwtbItem.WritingSystemFactory = m_cache.LanguageWritingSystemFactoryAccessor;
			m_fwtbItem.StyleSheet = Common.Widgets.FontHeightAdjuster.StyleSheetFromMediator(m_mediator);
			m_fwtbItem.WritingSystemCode = m_cache.DefaultVernWs;
			m_fwtbItem.Text = String.Empty;

			// Set some default values.

			m_rbtnAnywhere.Checked = true;
			m_btnRegExp.Enabled = false;
			m_chkMatchDiacritics.Checked = false;
			m_chkMatchCase.Checked = false;
			m_btnSearch.Enabled = false;


			m_regexContextMenu = new RegexHelperMenu(m_tbSearchText, m_helpTopicProvider);

			if (m_helpTopicProvider != null)
				this.helpProvider.HelpNamespace = m_helpTopicProvider.HelpFile;
			this.helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			this.helpProvider.SetShowHelp(this, true);
			if (m_helpTopicProvider != null)
			{
				helpProvider.SetHelpKeyword(this, "khtpSpecConcordanceCrit");
				m_btnHelp.Enabled = true;
			}


			if (m_clerk.SuspendLoadingRecordUntilOnJumpToRecord)
			{
				return;	// we're bound to process OnJumpToRecord, so skip any further initialization.
			}
			// Load any saved settings.
			LoadSettings();
		}

		/// <summary>
		/// Should not be called if disposed.
		/// </summary>
		public bool ShouldNotCall
		{
			get { return IsDisposed; }
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
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
			var ws = m_cbWritingSystem.SelectedItem as IWritingSystem;
			// Could have nothing selected.  See LT-8041.
			if (ws == null)
				return m_cache.DefaultVernWs;
			else
				return ws.Handle;
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
			((PaneBarContainer) Parent).PaneBar.Text = ITextStrings.ksSpecifyConcordanceCriteria;
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
				((ConcordLine) m_cbLine.SelectedItem).Line.ToString(), false,
				PropertyTable.SettingsGroup.LocalSettings);
			m_mediator.PropertyTable.SetPropertyPersistence("ConcordanceLine", true,
				PropertyTable.SettingsGroup.LocalSettings);

			m_mediator.PropertyTable.SetProperty("ConcordanceWs",
				((IWritingSystem) m_cbWritingSystem.SelectedItem).Id, false,
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
			var ws = m_cbWritingSystem.SelectedItem as IWritingSystem;
			if (ws == null)
			{
				Debug.Assert(m_cbWritingSystem.SelectedIndex == -1);
				return;
			}
			m_tbSearchText.WritingSystemCode = ws.Handle;
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			m_tbSearchText.Tss = tsf.MakeString(m_tbSearchText.Text.Trim(), ws.Handle);
		}

		private void m_rbtnUseRegExp_CheckedChanged(object sender, EventArgs e)
		{
			m_btnRegExp.Enabled = m_rbtnUseRegExp.Checked;
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpSpecConcordanceCrit");
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
			LoadMatches(true);
		}

		internal protected virtual void LoadMatches(bool fLoadVirtualProperty)
		{
			var occurrences = SearchForMatches();
			var decorator = (ConcDecorator) ((DomainDataByFlidDecoratorBase)m_clerk.VirtualListPublisher).BaseSda;
			// Set this BEFORE we start loading, otherwise, calls to ReloadList triggered here just make it empty.
			HasLoadedMatches = true;
			IsLoadingMatches = true;
			try
			{
				m_clerk.OwningObject = m_cache.LangProject;
				decorator.SetOccurrences(m_cache.LangProject.Hvo, occurrences);
				m_clerk.UpdateList(true);
			}
			finally
			{
				IsLoadingMatches = false;
			}
		}

		/// <summary>
		/// If asked to Refresh, update your results list.
		/// </summary>
		public void RefreshDisplay()
		{
			LoadMatches(true);
		}

		internal protected virtual List<IParaFragment> SearchForMatches()
		{
			if (m_fObjectConcorded)
				return FindMatchingItems();
			List<IParaFragment> occurrences = null;
#if WANTPORT // FWR-2830 we should display progress somehow...
			bool fCreatedProgressState = false;
#endif
			using (new WaitCursor(this))
			{
#if WANTPORT // FWR-2830 ConcordanceItemsVh no longer exists, we should display progress somehow though.
				if (ConcordanceItemsVh.Progress is NullProgressState)
				{
					ConcordanceItemsVh.Progress = FwXWindow.CreateMilestoneProgressState(m_mediator);
					fCreatedProgressState = true;
				}
#endif
				string sMatch = m_tbSearchText.Text.Trim();
				if (sMatch.Length == 0)
					return new List<IParaFragment>();
				if (sMatch.Length > 1000)
				{
					sMatch = sMatch.Substring(0, 1000);
					MessageBox.Show(ITextStrings.ksMatchStringTooLong, ITextStrings.ksWarning);
					m_tbSearchText.Text = sMatch;
				}
				int ws = ((IWritingSystem) m_cbWritingSystem.SelectedItem).Handle;

				ConcordLine conc = (ConcordLine)m_cbLine.SelectedItem;
				switch (conc.Line)
				{
					case ConcordanceLines.kBaseline:
						occurrences = UpdateConcordanceForBaseline(sMatch, ws);
						break;
					case ConcordanceLines.kWord:
						occurrences = UpdateConcordanceForWord(sMatch, ws);
						break;
					case ConcordanceLines.kMorphemes:
						occurrences = UpdateConcordanceForMorphemes(sMatch, ws);
						break;
					case ConcordanceLines.kLexEntry:
						occurrences = UpdateConcordanceForLexEntry(sMatch, ws);
						break;
					case ConcordanceLines.kLexGloss:
						occurrences = UpdateConcordanceForLexGloss(sMatch, ws);
						break;
					case ConcordanceLines.kWordGloss:
						occurrences = UpdateConcordanceForWordGloss(sMatch, ws);
						break;
					case ConcordanceLines.kFreeTranslation:
						occurrences = UpdateConcordanceForFreeTranslation(sMatch, ws);
						break;
					case ConcordanceLines.kLiteralTranslation:
						occurrences = UpdateConcordanceForLiteralTranslation(sMatch, ws);
						break;
					case ConcordanceLines.kNote:
						occurrences = UpdateConcordanceForNote(sMatch, ws);
						break;
					default:
						occurrences = new List<IParaFragment>();
						break;
				}
			}
#if WANTPORT // FWR-2830 clean up after whatever we now do to get a progress state.
			if (fCreatedProgressState)
			{
				ConcordanceItemsVh.Progress.Dispose();
				ConcordanceItemsVh.Progress = null;
			}
#endif
			return occurrences;
		}

		private List<IParaFragment> FindMatchingItems()
		{
			var result = new List<IParaFragment>();
			var target = m_cache.ServiceLocator.GetObject(m_hvoMatch);
			int clid = target.ClassID;
			switch (clid)
			{
				case WfiGlossTags.kClassId:
				case WfiAnalysisTags.kClassId:
					{
						var analyses = new List<IAnalysis>();
						analyses.Add(m_cache.ServiceLocator.GetObject(m_hvoMatch) as IAnalysis);
						return GetOccurrencesOfAnalyses(analyses);
					}
				case PartOfSpeechTags.kClassId:
					{
						var analyses = new HashSet<IAnalysis>();
						foreach (var analysis in m_cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().AllInstances())
						{
							if (analysis.CategoryRA == target)
							{
								analyses.Add(analysis);
							}
						}
						return GetOccurrencesOfAnalyses(analyses);
					}
				case LexEntryTags.kClassId:
					{
						var analyses = new HashSet<IAnalysis>();
						foreach (var mb in m_cache.ServiceLocator.GetInstance<IWfiMorphBundleRepository>().AllInstances())
						{
							if (mb.MorphRA != null && mb.MorphRA.Owner == target)
							{
								analyses.Add(mb.Owner as IWfiAnalysis);
							}
						}
						return GetOccurrencesOfAnalyses(analyses);
					}
				case LexSenseTags.kClassId:
					{
						var analyses = new HashSet<IAnalysis>();
						foreach (var mb in m_cache.ServiceLocator.GetInstance<IWfiMorphBundleRepository>().AllInstances())
						{
							if (mb.SenseRA == target)
							{
								analyses.Add(mb.Owner as IWfiAnalysis);
							}
						}
						return GetOccurrencesOfAnalyses(analyses);
					}
				default:
					if (m_cache.ClassIsOrInheritsFrom((int)clid, (int)MoFormTags.kClassId))
					{
						var analyses = new HashSet<IAnalysis>();
						foreach (
							var mb in m_cache.ServiceLocator.GetInstance<IWfiMorphBundleRepository>().AllInstances())
						{
							if (mb.MorphRA == target)
							{
								analyses.Add(mb.Owner as IWfiAnalysis);
							}
						}
						return GetOccurrencesOfAnalyses(analyses);
					}
					return new List<IParaFragment>();
			}
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
			if (m_fObjectConcorded)
			{
				ICmObject cmCurrent = null;
				if (m_clerk.CurrentObjectHvo != 0)
				{
					int hvoCurrent = m_clerk.VirtualListPublisher.get_ObjectProp(m_clerk.CurrentObjectHvo,
						ConcDecorator.kflidAnalysis);
					if (hvoCurrent != 0)
						cmCurrent = m_cache.ServiceLocator.GetObject(hvoCurrent);
					// enhance JohnT: if we aren't concording on an analysis, we could still get the BeginOffset
					// from the ParaFragment, and figure which analysis that is part of or closest to.
				}

				ITsString tss = null;
				int ws = 0;
				if (cmCurrent != null)
				{
					var wordform = (IWfiWordform)cmCurrent.OwnerOfClass(WfiWordformTags.kClassId);
					if (wordform != null)
					{
						tss = wordform.Form.BestVernacularAlternative;
						ITsTextProps ttp = tss.get_PropertiesAt(0);
						int nVar;
						ws = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
					}
				}
				if (tss == null)
				{
					ws = m_cache.DefaultVernWs;
					tss = m_cache.TsStrFactory.MakeString("", ws);
				}
				SetDefaultVisibilityOfItems(true, String.Empty);
				m_fObjectConcorded = false;
				SetConcordanceLine(ConcordanceLines.kWord);
				SetWritingSystem(ws);
				m_tbSearchText.Tss = tss;
			}
			else if (m_hvoMatch != 0)
			{
				InitializeConcordanceSearch(m_cache.ServiceLocator.GetObject(m_hvoMatch));
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
			m_cbLine.Items.Add(new ConcordLine(ITextStrings.ksBaseline,
				WritingSystemServices.kwsVerns,
				ConcordanceLines.kBaseline));
			m_cbLine.Items.Add(new ConcordLine(ITextStrings.ksWord,
				WritingSystemServices.kwsVerns,
				ConcordanceLines.kWord));
			m_cbLine.Items.Add(new ConcordLine(ITextStrings.ksMorphemes,
				WritingSystemServices.kwsVerns,
				ConcordanceLines.kMorphemes));
			m_cbLine.Items.Add(new ConcordLine(ITextStrings.ksLexEntry,
				WritingSystemServices.kwsVerns,
				ConcordanceLines.kLexEntry));
			m_cbLine.Items.Add(new ConcordLine(ITextStrings.ksLexGloss,
				WritingSystemServices.kwsAnals,
				ConcordanceLines.kLexGloss));
			m_cbLine.Items.Add(new ConcordLine(ITextStrings.ksWordGloss,
				WritingSystemServices.kwsAnals,
				ConcordanceLines.kWordGloss));
			m_cbLine.Items.Add(new ConcordLine(ITextStrings.ksFreeTranslation,
				WritingSystemServices.kwsAnals,
				ConcordanceLines.kFreeTranslation));
			m_cbLine.Items.Add(new ConcordLine(ITextStrings.ksLiteralTranslation,
				WritingSystemServices.kwsAnals,
				ConcordanceLines.kLiteralTranslation));
			m_cbLine.Items.Add(new ConcordLine(ITextStrings.ksNote,
				WritingSystemServices.kwsAnals,
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
				case WritingSystemServices.kwsVerns:
					foreach (IWritingSystem ws in m_cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems)
						m_cbWritingSystem.Items.Add(ws);
					wsSet = m_cache.DefaultVernWs;
					break;
				case WritingSystemServices.kwsAnals:
					foreach (IWritingSystem ws in m_cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
						m_cbWritingSystem.Items.Add(ws);
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
				if (((IWritingSystem) m_cbWritingSystem.Items[i]).Handle == ws)
				{
					idx = i;
					break;
				}
			}
			if (idx == -1)
			{
				foreach (IWritingSystem wsObj in m_cache.ServiceLocator.WritingSystems.AllWritingSystems)
				{
					if (wsObj.Handle == ws)
					{
						m_cbWritingSystem.Items.Add(wsObj);
						idx = m_cbWritingSystem.Items.IndexOf(wsObj);
					}
				}
			}
			if (idx != -1 && m_cbWritingSystem.SelectedIndex != idx)
				m_cbWritingSystem.SelectedIndex = idx;
		}

		IParaFragment MakeOccurrence(IStTxtPara para, int ichMin, int ichLim)
		{
			var seg = para.SegmentsOS[0]; // Since we found something in the paragraph, assume it has at least one!
			foreach (var seg1 in para.SegmentsOS)
				if (seg1.BeginOffset <= ichMin)
					seg = seg1;
				else
					break;
			return new ParaFragment(seg, ichMin, ichLim, null);
		}

		private List<IParaFragment> UpdateConcordanceForBaseline(string sMatch, int ws)
		{
			SimpleStringMatcher matcher = GetMatcher(ws) as SimpleStringMatcher;
			if (!matcher.IsValid())
				return new List<IParaFragment>();
			ISilDataAccess sda = m_cache.MainCacheAccessor;

			var occurrences = new List<IParaFragment>();
			int cPara = 0;
			foreach (var para in ParagraphsToSearch)
			{
				++cPara;
				// Find occurrences of the string in this paragraph.
				if (matcher.Matches(para.Contents))
				{
					// Create occurrences for each match.
					List<MatchRangePair> results = matcher.GetAllResults();
					foreach (MatchRangePair range in results)
					{
						occurrences.Add(MakeOccurrence(para, range.IchMin, range.IchLim));
						if (occurrences.Count >= MaxConcordanceMatches())
						{
							MessageBox.Show(String.Format(ITextStrings.ksShowingOnlyTheFirstXXXMatches,
								occurrences.Count, cPara, ParagraphsToSearch.Count), ITextStrings.ksNotice,
								MessageBoxButtons.OK, MessageBoxIcon.Information);
							return occurrences;
						}
					}
				}
			}
			return occurrences;
		}

		/// <summary>
		/// Get the paragraphs we are interested in concording.
		/// </summary>
		HashSet<IStTxtPara> ParagraphsToSearch
		{
			get
			{
				var result = new HashSet<IStTxtPara>();
				var needsParsing = new List<IStTxtPara>();
				var concDecorator = GetConcDecorator();
				foreach (var sttext in concDecorator.InterestingTexts)
					AddUnparsedParagraphs(sttext, needsParsing, result);
				if (needsParsing.Count > 0)
				{
					NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor,
						() =>
						{
							foreach (var para in needsParsing)
							{
								ParagraphParser.ParseParagraph(para);
								if (para.SegmentsOS.Count > 0)
									result.Add(para);
							}
						});
				}
				return result;
			}
		}

		private ConcDecorator GetConcDecorator()
		{
			return (m_clerk.VirtualListPublisher as ObjectListPublisher).BaseSda as ConcDecorator;
		}

		private void AddUnparsedParagraphs(IStText text, List<IStTxtPara> collectUnparsed, HashSet<IStTxtPara> collectUsefulParas)
		{
			foreach (IStTxtPara para in text.ParagraphsOS)
				if (para.ParseIsCurrent)
				{
					if (para.SegmentsOS.Count > 0)
						collectUsefulParas.Add(para);
				}
				else
				{
					collectUnparsed.Add(para);
				}
		}

		private List<IParaFragment> UpdateConcordanceForWord(string sMatch, int ws)
		{
			// Find analyses that have the relevant word.
			var analyses = new HashSet<IAnalysis>();
			var matcher = GetMatcher(ws);
			foreach (var wf in m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>().AllInstances())
			{
				if (matcher.Matches(wf.Form.get_String(ws)))
				{
					analyses.Add(wf);
				}
			}
			return GetOccurrencesOfAnalyses(analyses);
			//IMatcher matcher = GetMatcher(ws);
			//if (!matcher.IsValid())
			//	return new List<IParaFragment>();
			//IWfiWordform wf;
			//var analyses = new List<IAnalysis>();
			//var wfRepo = m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>();
			//if (wfRepo.TryGetObject(m_cache.TsStrFactory.MakeString(sMatch, ws), out wf))
			//	analyses.Add(wf);
			//var lower = sMatch.ToLowerInvariant();
			//if (lower != sMatch && wfRepo.TryGetObject(m_cache.TsStrFactory.MakeString(lower, ws), out wf))
			//	analyses.Add(wf);
			//var upper = sMatch.ToUpperInvariant();
			//if (upper != sMatch && wfRepo.TryGetObject(m_cache.TsStrFactory.MakeString(upper, ws), out wf))
			//	analyses.Add(wf);
			//var title = Icu.ToTitle(lower, null);
			//if (title != sMatch && title != upper && wfRepo.TryGetObject(m_cache.TsStrFactory.MakeString(title, ws), out wf))
			//	analyses.Add(wf);
			//return GetOccurrencesOfAnalyses(analyses);
		}

		private List<IParaFragment> GetOccurrencesOfAnalyses(IEnumerable<IAnalysis> analyses)
		{
			var result = new List<IParaFragment>();
			var interestingParas = ParagraphsToSearch;
			foreach (var analysis in analyses)
			{
				// Don't create this one layer further out! We want to avoid processing the same segment
				// repeatedly for the SAME analysis, but not if other analyses occur in it.
				var segs = new HashSet<ISegment>();
				foreach (var seg in analysis.Wordform.OccurrencesInTexts)
				{
					if (!interestingParas.Contains(seg.Owner as IStTxtPara))
						continue;
					if (segs.Contains(seg))
						continue; // wordform occurs in it more than once, but we only want to add the occurences once.
					segs.Add(seg);
					foreach (var occurrence in seg.GetOccurrencesOfAnalysis(analysis, int.MaxValue, true))
						result.Add(occurrence);
				}
			}
			return result;
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

		/// <summary>
		/// Concordance contains all occurrences of analyses which contain exactly the specified morpheme.
		/// A match may be either on the Form of the morph bundle, or on the form of the MoForm it points to.
		/// </summary>
		private List<IParaFragment> UpdateConcordanceForMorphemes(string sMatch, int ws)
		{
			// Find analyses that have the relevant morpheme.
			var analyses = new HashSet<IAnalysis>();
			var matcher = GetMatcher(ws);
			foreach (var mb in m_cache.ServiceLocator.GetInstance<IWfiMorphBundleRepository>().AllInstances())
			{
				if (mb.MorphRA != null && matcher.Matches(mb.MorphRA.Form.get_String(ws))
					|| matcher.Matches(mb.Form.get_String(ws)))
				{
					analyses.Add(mb.Owner as IWfiAnalysis);
				}
			}
			return GetOccurrencesOfAnalyses(analyses);
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

// CS0169
#if false
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
#endif

		/// <summary>
		/// Concordance contains all occurrences of analyses which contain the specified lex entry, in the sense
		/// that they point to an MoForm whose owner's LexemeForm matches the pattern.
		/// Enhance JohnT: the VC will show the citation form, in the (unlikely? impossible?) event that the
		/// LexemeForm doesn't have a form. Should we search there too?
		/// </summary>
		private List<IParaFragment> UpdateConcordanceForLexEntry(string sMatch, int ws)
		{
			// Find analyses that have the relevant morpheme.
			var analyses = new HashSet<IAnalysis>();
			var matcher = GetMatcher(ws);
			foreach (var mb in m_cache.ServiceLocator.GetInstance<IWfiMorphBundleRepository>().AllInstances())
			{
				if (mb.MorphRA != null && matcher.Matches(((ILexEntry)mb.MorphRA.Owner).LexemeFormOA.Form.get_String(ws)))
				{
					analyses.Add(mb.Owner as IWfiAnalysis);
				}
			}
			return GetOccurrencesOfAnalyses(analyses);
		}

		/// <summary>
		/// This one matches on the gloss of the morph bundle's sense.
		/// </summary>
		private List<IParaFragment> UpdateConcordanceForLexGloss(string sMatch, int ws)
		{
			// Find analyses that have the relevant morpheme.
			var analyses = new HashSet<IAnalysis>();
			var matcher = GetMatcher(ws);
			foreach (var mb in m_cache.ServiceLocator.GetInstance<IWfiMorphBundleRepository>().AllInstances())
			{
				if (mb.SenseRA != null && matcher.Matches(mb.SenseRA.Gloss.get_String(ws)))
				{
					analyses.Add(mb.Owner as IWfiAnalysis);
				}
			}
			return GetOccurrencesOfAnalyses(analyses);
		}

		/// <summary>
		///  Here we're looking for ones that match on the word gloss.
		/// </summary>
		/// <param name="sMatch"></param>
		/// <param name="ws"></param>
		/// <returns></returns>
		private List<IParaFragment> UpdateConcordanceForWordGloss(string sMatch, int ws)
		{
			// Find analyses that have the relevant gloss.
			var analyses = new HashSet<IAnalysis>();
			var matcher = GetMatcher(ws);
			foreach (var wg in m_cache.ServiceLocator.GetInstance<IWfiGlossRepository>().AllInstances())
			{
				if (matcher.Matches(wg.Form.get_String(ws)))
				{
					analyses.Add(wg);
				}
			}
			return GetOccurrencesOfAnalyses(analyses);
		}

		/// <summary>
		/// Here the match is a complete segment, if the requested free translation matches.
		/// </summary>
		private List<IParaFragment> UpdateConcordanceForFreeTranslation(string sMatch, int ws)
		{
			var result = new List<IParaFragment>();
			var matcher = GetMatcher(ws);
			foreach (var para in ParagraphsToSearch)
			{
				foreach (var seg in para.SegmentsOS)
				{
					if (matcher.Matches(seg.FreeTranslation.get_String(ws)))
						result.Add(MakeOccurrence(para, seg.BeginOffset, seg.EndOffset));
				}
			}
			return result;
		}

		/// <summary>
		/// Here the match is a complete segment, if the requested literal translation matches.
		/// </summary>
		private List<IParaFragment> UpdateConcordanceForLiteralTranslation(string sMatch, int ws)
		{
			var result = new List<IParaFragment>();
			var matcher = GetMatcher(ws);
			foreach (var para in ParagraphsToSearch)
			{
				foreach (var seg in para.SegmentsOS)
				{
					if (matcher.Matches(seg.LiteralTranslation.get_String(ws)))
						result.Add(MakeOccurrence(para, seg.BeginOffset, seg.EndOffset));
				}
			}
			return result;
		}

		/// <summary>
		/// Here the match is a complete segment, if one of its notes matches.
		/// </summary>
		private List<IParaFragment> UpdateConcordanceForNote(string sMatch, int ws)
		{
			var result = new List<IParaFragment>();
			var matcher = GetMatcher(ws);
			foreach (var para in ParagraphsToSearch)
			{
				foreach (var seg in para.SegmentsOS)
				{
					foreach (var note in seg.NotesOS)
					{
						if (matcher.Matches(note.Content.get_String(ws)))
							result.Add(MakeOccurrence(para, seg.BeginOffset, seg.EndOffset));
					}
				}
			}
			return result;
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
			if (!m_cache.ServiceLocator.IsValidObjectId(hvoTarget))
				return false;
			try
			{
				ICmObject target = m_cache.ServiceLocator.GetObject(hvoTarget);
				int clid = target.ClassID;
				int ws = 0;
				ITsString tss;
				switch (clid)
				{
					case WfiWordformTags.kClassId:
						var wwf = (IWfiWordform)target;
						if (wwf.Form != null && wwf.Form.TryWs(WritingSystemServices.kwsFirstVern, out ws, out tss))
							InitializeConcordanceSearch(tss.Text, ws, ConcordanceLines.kWord);
						break;
					case LexEntryTags.kClassId:
					case LexSenseTags.kClassId:
					case WfiAnalysisTags.kClassId:
					case PartOfSpeechTags.kClassId:
					case WfiGlossTags.kClassId:
						InitializeConcordanceSearch(target);
						break;
					default:
						if (m_cache.ClassIsOrInheritsFrom((int)clid, (int)MoFormTags.kClassId))
							InitializeConcordanceSearch(target);
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
				((MatchingConcordanceItems) m_list).OwningControl = value;
			}
		}

		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			if (disposing)
			{
				m_concordanceControl = null;
			}
			base.Dispose(disposing);
		}

		protected override void RefreshAfterInvalidObject()
		{
			ConcordanceControl.LoadMatches(true);
		}

		/// <summary>
		/// Overridden to prevent trying to get a name for the "current object" which we can't do because
		/// it is not a true CmObject.
		/// </summary>
		protected override string GetStatusBarMsgForCurrentObject()
		{
			return "";
		}
	}

	/// <summary>
	/// This class is used for the record list of the concordance view.
	/// We fudge the owning object, since the decorator doesn't care what class it is, but
	/// the base class does care that it is some kind of real object.
	/// </summary>
	public class MatchingConcordanceItems : RecordList
	{
		internal ConcordanceControl OwningControl { get; set; }

		public override void Init(FdoCache cache, Mediator mediator, XmlNode recordListNode)
		{
			base.Init(cache, mediator, recordListNode);
			m_owningObject = cache.LangProject;
		}

		/// <summary>
		/// Override to force recomputing the list. This is tricky because LoadMatches calls a Clerk routine which
		/// recursively calls ReloadList. Therefore if we call LoadMatches, we don't need to call the base routine.
		/// If we're in the middle of loading a list, though, we want to only do the base thing.
		/// Finally, if the OwningControl has never been loaded (user hasn't yet selected option), just load the (typically empty) list.
		/// </summary>
		public override void ReloadList()
		{
			if (OwningControl != null && OwningControl.HasLoadedMatches)
			{
				if (OwningControl.IsLoadingMatches)
				{
					// calling from inside the call to LoadMatches, we've already rebuild the main list,
					// just need to do the rest of the normal reload.
					base.ReloadList();
					return;
				}
				OwningControl.LoadMatches();
				// Fall through to base impl.
			}
			else
			{
				// It's in a disposed state...make it empty for now.
				(VirtualListPublisher as ObjectListPublisher).SetOwningPropValue(new int[0]);
				ConcDecorator concSda = GetConcDecorator();
				if (concSda != null)
					concSda.UpdateOccurrences(new int[0]);
			}
			base.ReloadList();
		}

		private ConcDecorator GetConcDecorator()
		{
			return (VirtualListPublisher as ObjectListPublisher).BaseSda as ConcDecorator;
		}
	}

}
