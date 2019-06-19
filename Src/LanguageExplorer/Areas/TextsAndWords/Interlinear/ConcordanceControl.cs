// Copyright (c) 2007-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.Filters;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using WaitCursor = SIL.FieldWorks.Common.FwUtils.WaitCursor;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	internal partial class ConcordanceControl : ConcordanceControlBase
	{
		private RegexHelperContextMenu _regexContextContextMenu;
		private IVwPattern m_vwPattern;
		private bool m_fObjectConcorded;
		private int m_hvoMatch;
		private int m_backupHvo;
		private POSPopupTreeManager m_pOSPopupTreeManager;
		private ISharedEventHandlers _sharedEventHandlers;

		public ConcordanceControl()
		{
			ConstructorSurrogate();
		}

		internal ConcordanceControl(ISharedEventHandlers sharedEventHandlers, MatchingConcordanceItems recordList)
			: base(recordList)
		{
			Guard.AgainstNull(sharedEventHandlers, nameof(sharedEventHandlers));

			_sharedEventHandlers = sharedEventHandlers;
			ConstructorSurrogate();
		}

		private void ConstructorSurrogate()
		{
			InitializeComponent();

			m_vwPattern = VwPatternClass.Create();
			helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			helpProvider.SetShowHelp(this, true);
			m_tbSearchText.SuppressEnter = true;
			_sharedEventHandlers.Add(Command.JumpToConcordance, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(JumpToConcordance_Clicked, _sharedEventHandlers.SeeAndDo));
		}

		#region Overrides of ConcordanceControlBase

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			m_tbSearchText.WritingSystemFactory = m_cache.LanguageWritingSystemFactoryAccessor;
			m_tbSearchText.AdjustForStyleSheet(FwUtils.StyleSheetFromPropertyTable(PropertyTable));
			m_tbSearchText.Text = string.Empty;
			m_tbSearchText.TextChanged += m_tbSearchText_TextChanged;
			m_tbSearchText.KeyDown += m_tbSearchText_KeyDown;
			FillLineComboList();

			m_fwtbItem.WritingSystemFactory = m_cache.LanguageWritingSystemFactoryAccessor;
			m_fwtbItem.StyleSheet = FwUtils.StyleSheetFromPropertyTable(PropertyTable);
			m_fwtbItem.WritingSystemCode = m_cache.DefaultVernWs;
			m_fwtbItem.Text = string.Empty;
			m_fwtbItem.Visible = false; // Needed to prevent LT-12162 unneeded text box.

			// Set some default values.
			m_rbtnAnywhere.Checked = true;
			m_btnRegExp.Enabled = false;
			m_chkMatchDiacritics.Checked = false;
			m_chkMatchCase.Checked = false;
			m_btnSearch.Enabled = false;

			_regexContextContextMenu = new RegexHelperContextMenu(m_tbSearchText, m_helpTopicProvider);

			if (m_helpTopicProvider != null)
			{
				helpProvider.HelpNamespace = m_helpTopicProvider.HelpFile;
			}
			helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			helpProvider.SetShowHelp(this, true);
			if (m_helpTopicProvider != null)
			{
				helpProvider.SetHelpKeyword(this, "khtpSpecConcordanceCrit");
				m_btnHelp.Enabled = true;
			}
			m_cbSearchText.WritingSystemFactory = m_cache.LanguageWritingSystemFactoryAccessor;
			if (m_recordList.SuspendLoadingRecordUntilOnJumpToRecord)
			{
				return; // we're bound to process OnJumpToRecord, so skip any further initialization.
			}
			// Load any saved settings.
			LoadSettings();
		}

		#endregion

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				components?.Dispose();
				_sharedEventHandlers.Remove(Command.JumpToConcordance);
				if (m_recordList != null)
				{
					// Don't dispose of the record list, since it can monitor relevant PropChanges
					// that affect the NeedToReloadVirtualProperty.
					m_recordList.ConcordanceControl = null;
				}
				m_pOSPopupTreeManager?.Dispose();
			}
			m_recordList = null;
			m_pOSPopupTreeManager = null;
			base.Dispose(disposing);
		}

		private void LoadSettings()
		{
			var sLine = PropertyTable.GetValue("ConcordanceLine", "kBaseline", SettingsGroup.LocalSettings);
			ConcordanceLines line;
			try
			{
				line = (ConcordanceLines)Enum.Parse(typeof(ConcordanceLines), sLine);
			}
			catch
			{
				line = ConcordanceLines.kBaseline;
			}
			SetConcordanceLine(line);
			var sWs = PropertyTable.GetValue<string>("ConcordanceWs", SettingsGroup.LocalSettings);
			int ws;
			if (sWs != null)
			{
				ws = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(sWs);
				if (ws != 0)    // could be obsolete data.
				{
					SetWritingSystem(ws);
				}
			}
			ws = CurrentSelectedWs();
			m_tbSearchText.WritingSystemCode = ws;
			var sText = PropertyTable.GetValue<string>("ConcordanceText", SettingsGroup.LocalSettings);
			if (sText != null)
			{
				m_tbSearchText.Text = sText;
			}
			var fMatchCase = PropertyTable.GetValue("ConcordanceMatchCase", m_chkMatchCase.Checked, SettingsGroup.LocalSettings);
			m_chkMatchCase.Checked = fMatchCase;
			var fMatchDiacritics = PropertyTable.GetValue("ConcordanceMatchDiacritics", m_chkMatchDiacritics.Checked, SettingsGroup.LocalSettings);
			m_chkMatchDiacritics.Checked = fMatchDiacritics;
			var sConcordanceOption = PropertyTable.GetValue<string>("ConcordanceOption", SettingsGroup.LocalSettings);
			SetConcordanceOption(sConcordanceOption);
		}

		private int CurrentSelectedWs()
		{
			var ws = m_cbWritingSystem.SelectedItem as CoreWritingSystemDefinition;
			// Could have nothing selected.  See LT-8041.
			return ws?.Handle ?? m_cache.DefaultVernWs;
		}

		protected string GetConcordanceOption()
		{
			string sConcordanceOption;
			if (m_rbtnWholeItem.Checked)
			{
				sConcordanceOption = "WholeItem";
			}
			else if (m_rbtnAtEnd.Checked)
			{
				sConcordanceOption = "AtEnd";
			}
			else if (m_rbtnAtStart.Checked)
			{
				sConcordanceOption = "AtStart";
			}
			else if (m_rbtnUseRegExp.Checked)
			{
				sConcordanceOption = "UseRegExp";
			}
			else
			{
				sConcordanceOption = "Anywhere";
			}
			return sConcordanceOption;
		}

		protected void SetConcordanceOption(string sConcordanceOption)
		{
			if (sConcordanceOption == null)
			{
				return;
			}
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

		private void m_tbSearchText_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter && m_tbSearchText.Text.Length > 0)
			{
				m_btnSearch_Click(sender, e);
			}
		}

		#region IMainUserControl Members

		public override string AccName => "LanguageExplorer.Areas.TextsAndWords.Interlinear.ConcordanceControl";
		#endregion

		#region IMainContentControl Members

		/// <summary>
		/// This is called when the main window is closing.
		/// </summary>
		public bool OnConsideringClosing(object sender, CancelEventArgs arg)
		{
			arg.Cancel = !PrepareToGoAway();
			return arg.Cancel;
		}

		#endregion

		#region Overrides

		protected override void OnLeave(EventArgs e)
		{
			base.OnLeave(e);

			SaveSettings();
		}

		private void SaveSettings()
		{
			// Save our settings for later.
			PropertyTable.SetProperty("ConcordanceLine", ((ConcordLine)m_cbLine.SelectedItem).Line.ToString(), true, settingsGroup: SettingsGroup.LocalSettings);
			PropertyTable.SetProperty("ConcordanceWs", ((CoreWritingSystemDefinition)m_cbWritingSystem.SelectedItem).Id, true, settingsGroup: SettingsGroup.LocalSettings);
			PropertyTable.SetProperty("ConcordanceText", m_tbSearchText.Text.Trim(), true, settingsGroup: SettingsGroup.LocalSettings);
			PropertyTable.SetProperty("ConcordanceMatchCase", m_chkMatchCase.Checked, true, settingsGroup: SettingsGroup.LocalSettings);
			PropertyTable.SetProperty("ConcordanceMatchDiacritics", m_chkMatchDiacritics.Checked, true, settingsGroup: SettingsGroup.LocalSettings);
			PropertyTable.SetProperty("ConcordanceOption", GetConcordanceOption(), true, settingsGroup: SettingsGroup.LocalSettings);
		}

		#endregion

		#region Internal types

		private enum ConcordanceLines
		{
			kBaseline,
			kWord,
			kMorphemes,
			kLexEntry,
			kLexGloss,
			kWordGloss,
			kFreeTranslation,
			kLiteralTranslation,
			kNote,
			kGramCategory,
			kWordCategory,
			kTags
		}

		/// <summary>
		/// Like the base class, but match ignores case.
		/// </summary>
		private sealed class ExactCaseInsensitiveLiteralMatcher : ExactLiteralMatcher
		{
			public ExactCaseInsensitiveLiteralMatcher(string target, int ws)
				: base(target.ToLower(), ws)
			{
			}

			internal override bool MatchText(string p)
			{
				return base.MatchText(p.ToLower());
			}
		}

		/// <summary>
		/// This class stores the objects used by the combo box by the Word Cat. and Lex. Gram. Info. lines.
		/// </summary>
		private sealed class POSComboController : POSPopupTreeManager
		{
			/// <summary />
			public POSComboController(TreeCombo treeCombo, LcmCache cache, ICmPossibilityList list, int ws, bool useAbbr, IPropertyTable propertyTable, IPublisher publisher, Form parent) :
				base(treeCombo, cache, list, ws, useAbbr, propertyTable, publisher, parent)
			{
				Sorted = true;
			}

			protected override TreeNode MakeMenuItems(PopupTree popupTree, int hvoTarget)
			{
				popupTree.Sorted = Sorted;
				TreeNode match = null;
				if (List != null)
				{
					match = AddNodes(popupTree.Nodes, List.Hvo, CmPossibilityListTags.kflidPossibilities, hvoTarget, UseAbbr ? CmPossibilityTags.kflidAbbreviation : CmPossibilityTags.kflidName);
				}
				return match ?? popupTree.Nodes[0];
			}

			public bool Sorted { get; set; }
		}

		/// <summary>
		/// This class stores the objects used by the Line combo box.
		/// </summary>
		private sealed class ConcordLine
		{
			internal ConcordLine(string name, int wsMagic, ConcordanceLines line)
			{
				Name = name;
				MagicWs = wsMagic;
				Line = line;
			}

			private string Name { get; }

			internal int MagicWs { get; }

			internal ConcordanceLines Line { get; }

			public override string ToString()
			{
				return Name;
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
			SyncWritingSystemComboToSelectedLine((ConcordLine)m_cbLine.SelectedItem);
		}

		private void SyncWritingSystemComboToSelectedLine(ConcordLine sel)
		{
			FillWritingSystemCombo(sel.MagicWs);
		}

		private void UpdateButtonState()
		{
			var sel = (ConcordLine)m_cbLine.SelectedItem;
			m_searchContentLabel.Text = ITextStrings.ConcordanceSearchTextLabel;
			switch (sel.Line)
			{
				case ConcordanceLines.kGramCategory:
				case ConcordanceLines.kWordCategory:
				case ConcordanceLines.kTags:
					m_cbSearchText.Enabled = true;
					m_cbSearchText.Visible = true;
					FillSearchComboList(sel.Line);
					m_tbSearchText.Visible = m_btnRegExp.Visible = false;
					DisableDetailedSearchControls();
					m_searchContentLabel.Text = sel.Line != ConcordanceLines.kTags ? ITextStrings.ConcordanceSearchCatLabel : ITextStrings.ConcordanceSearchTagLabel;
					break;
				case ConcordanceLines.kBaseline:
					SyncWritingSystemComboToSelectedLine(sel);
					SetDefaultButtonState();
					// the Baseline currently tries to match in an entire paragraph.
					// so disable "at start" and "at end" and "whole item" matchers.
					if (!m_rbtnAnywhere.Checked && !m_rbtnUseRegExp.Checked)
					{
						m_rbtnAnywhere.Checked = true;
					}
					m_rbtnAtEnd.Enabled = false;
					m_rbtnAtStart.Enabled = false;
					m_rbtnWholeItem.Enabled = false;
					break;
				default:
					SyncWritingSystemComboToSelectedLine(sel);
					SetDefaultButtonState();
					break;
			}
		}

		private void SetDefaultButtonState()
		{
			EnableDetailedSearchControls();
			m_btnRegExp.Visible = m_tbSearchText.Visible = true;
			m_cbSearchText.Enabled = m_cbSearchText.Visible = false;
		}

		private void EnableDetailedSearchControls()
		{
			m_rbtnAtEnd.Enabled = true;
			m_rbtnAtStart.Enabled = true;
			m_rbtnWholeItem.Enabled = true;
			m_rbtnAnywhere.Enabled = true;
			m_rbtnUseRegExp.Enabled = true;
			m_chkMatchCase.Enabled = true;
			m_chkMatchDiacritics.Enabled = true;
			m_cbWritingSystem.Enabled = true;
		}

		/// <summary>
		/// Don't want all the radio buttons and checkboxes active when searching
		/// by possibility list item.
		/// </summary>
		private void DisableDetailedSearchControls()
		{
			m_rbtnAtEnd.Enabled = false;
			m_rbtnAtStart.Enabled = false;
			m_rbtnWholeItem.Enabled = false;
			m_rbtnAnywhere.Enabled = false;
			m_rbtnUseRegExp.Enabled = false;
			// LT-6966/10312 reopened. Don't confuse user with ws options when they just want to find
			// the tag or category they used. The list already shows BestAnalysisAlternative. We will
			// search for that too.
			m_cbWritingSystem.Enabled = false;
			// Again, don't confuse the user by making this look like a string match.
			// We want these disabled for list item matching.
			m_chkMatchDiacritics.Enabled = false;
			m_chkMatchCase.Enabled = false;
		}

		/// <summary>
		/// This method will fill in the DropDownList which replaces the Textbox for searching on certain lines
		/// </summary>
		/// <param name="line"></param>
		private void FillSearchComboList(ConcordanceLines line)
		{
			m_pOSPopupTreeManager?.Dispose();
			switch (line)
			{
				case ConcordanceLines.kTags:
					m_pOSPopupTreeManager = new POSComboController(m_cbSearchText, m_cache, InterlinTaggingChild.GetTaggingLists(m_cache.LangProject),
							m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle, false, PropertyTable, Publisher, PropertyTable.GetValue<Form>(FwUtils.window))
					{
						Sorted = false
					};
					break;
				default: //Lex. Gram. Info and Word Cat. both work the same, and are handled here in the default option
					m_pOSPopupTreeManager = new POSComboController(m_cbSearchText, m_cache, m_cache.LanguageProject.PartsOfSpeechOA,
						m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle, false, PropertyTable, Publisher, PropertyTable.GetValue<Form>(FwUtils.window));
					break;
			}
			m_pOSPopupTreeManager.AfterSelect += POSAfterSelect;
			m_pOSPopupTreeManager.LoadPopupTree(0);
		}

		private void POSAfterSelect(object sender, TreeViewEventArgs e)
		{
			// Enable the search after any selection in the SearchCombo
			m_btnSearch.Enabled = true;
		}

		private void m_cbWritingSystem_SelectedIndexChanged(object sender, EventArgs e)
		{
			var ws = m_cbWritingSystem.SelectedItem as CoreWritingSystemDefinition;
			if (ws == null)
			{
				Debug.Assert(m_cbWritingSystem.SelectedIndex == -1);
				return;
			}
			m_tbSearchText.WritingSystemCode = ws.Handle;
			m_tbSearchText.Tss = TsStringUtils.MakeString(m_tbSearchText.Text.Trim(), ws.Handle);
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

		protected override List<IParaFragment> SearchForMatches()
		{
			if (m_fObjectConcorded)
			{
				return FindMatchingItems();
			}
			List<IParaFragment> occurrences = null;
			using (new WaitCursor(this))
			{
				var sMatch = m_tbSearchText.Visible ? m_tbSearchText.Text.Trim() : m_cbSearchText.SelectedItem.ToString();
				if (sMatch.Length == 0)
				{
					return new List<IParaFragment>();
				}
				if (sMatch.Length > 1000)
				{
					sMatch = sMatch.Substring(0, 1000);
					MessageBox.Show(ITextStrings.ksMatchStringTooLong, ITextStrings.ksWarning);
					m_tbSearchText.Text = sMatch;
				}
				var ws = ((CoreWritingSystemDefinition)m_cbWritingSystem.SelectedItem).Handle;
				switch (((ConcordLine)m_cbLine.SelectedItem).Line)
				{
					case ConcordanceLines.kBaseline:
						occurrences = UpdateConcordanceForBaseline(ws);
						break;
					case ConcordanceLines.kWord:
						occurrences = UpdateConcordanceForWord(ws);
						break;
					case ConcordanceLines.kMorphemes:
						occurrences = UpdateConcordanceForMorphemes(ws);
						break;
					case ConcordanceLines.kLexEntry:
						occurrences = UpdateConcordanceForLexEntry(ws);
						break;
					case ConcordanceLines.kLexGloss:
						occurrences = UpdateConcordanceForLexGloss(ws);
						break;
					case ConcordanceLines.kWordGloss:
						occurrences = UpdateConcordanceForWordGloss(ws);
						break;
					case ConcordanceLines.kFreeTranslation:
						occurrences = UpdateConcordanceForFreeTranslation(ws);
						break;
					case ConcordanceLines.kLiteralTranslation:
						occurrences = UpdateConcordanceForLiteralTranslation(ws);
						break;
					case ConcordanceLines.kNote:
						occurrences = UpdateConcordanceForNote(ws);
						break;
					case ConcordanceLines.kGramCategory:
						occurrences = UpdateConcordanceForGramInfo(ws);
						break;
					case ConcordanceLines.kWordCategory:
						occurrences = UpdateConcordanceForWordCategory(ws);
						break;
					case ConcordanceLines.kTags:
						occurrences = UpdateConcordanceForTag(ws);
						break;
					default:
						occurrences = new List<IParaFragment>();
						break;
				}
			}
			return occurrences;
		}

		private List<IParaFragment> FindMatchingItems()
		{
			var result = new List<IParaFragment>();
			var target = GetMatchObject();
			if (target == null)
			{
				m_hvoMatch = 0;
				HasLoadedMatches = false;
				return result; // shouldn't happen... :)
			}
			var clid = target.ClassID;
			var analyses = new HashSet<IAnalysis>();
			switch (clid)
			{
				case WfiGlossTags.kClassId:
					{
						var targetGloss = (IWfiGloss)target;
						analyses.Add(targetGloss);
						foreach (var gloss in m_cache.ServiceLocator.GetInstance<IWfiGlossRepository>().AllInstances().Where(g => g != targetGloss))
						{
							if (targetGloss.Form.AvailableWritingSystemIds.Any(ws => targetGloss.Form.get_String(ws).Equals(gloss.Form.get_String(ws))))
							{
								analyses.Add(gloss);
							}
						}
						return GetOccurrencesOfAnalyses(analyses);
					}
				case WfiAnalysisTags.kClassId:
					{
						analyses.Add(m_cache.ServiceLocator.GetObject(m_hvoMatch) as IAnalysis);
						return GetOccurrencesOfAnalyses(analyses);
					}
				case PartOfSpeechTags.kClassId:
					{
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
						foreach (var mb in m_cache.ServiceLocator.GetInstance<IWfiMorphBundleRepository>().AllInstances())
						{
							if (mb.SenseRA == target)
							{
								analyses.Add(mb.Owner as IWfiAnalysis);
							}
						}
						return GetOccurrencesOfAnalyses(analyses);
					}
				case MoStemMsaTags.kClassId:
				case MoInflAffMsaTags.kClassId:
				case MoDerivAffMsaTags.kClassId:
				case MoUnclassifiedAffixMsaTags.kClassId:
					// In the interlinear texts analysis tab the user selects Concord On -> Lex Gram Info while right clicking on a
					// morpheme.
					// This code finds each Wordform with a morpheme bundle that matched the morpheme selected.
					foreach (var mb in m_cache.ServiceLocator.GetInstance<IWfiMorphBundleRepository>().AllInstances())
					{
						if (mb.MsaRA != null && mb.MsaRA.ComponentsRS != null)
						{
							if (mb.MsaRA.Hvo == m_hvoMatch)
							{
								analyses.Add(mb.Owner as IWfiAnalysis);
							}
						}
					}
					return GetOccurrencesOfAnalyses(analyses);
				default:
					if (m_cache.ClassIsOrInheritsFrom((int)clid, (int)MoFormTags.kClassId))
					{
						foreach (var mb in m_cache.ServiceLocator.GetInstance<IWfiMorphBundleRepository>().AllInstances())
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

		private ICmObject GetMatchObject()
		{
			ICmObject matchingObject;
			if (m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().TryGetObject(m_hvoMatch, out matchingObject))
			{
				return matchingObject;
			}
			// LT-13503 It is just possible that we are deleting the last remaining analysis of a wordform
			m_hvoMatch = m_backupHvo;
			if (m_hvoMatch <= 0)
			{
				return null;
			}
			var newTarget = m_cache.ServiceLocator.GetObject(m_hvoMatch);
			var targetAsWordform = newTarget as IWfiWordform;
			if (targetAsWordform != null && targetAsWordform.AnalysesOC.Count > 0)
			{
				InitializeConcordanceSearch(((IWfiWordform)newTarget).AnalysesOC.First());
			}
			else
			{
				InitializeConcordanceSearch(newTarget);
			}
			return newTarget;
		}

		private void m_btnSearch_Click(object sender, EventArgs e)
		{
			LoadMatches(true);
		}

		private void m_tbSearchText_TextChanged(object sender, EventArgs e)
		{
			m_btnSearch.Enabled = m_tbSearchText.Text.Length > 0;
		}

		private void m_btnRegExp_Click(object sender, EventArgs e)
		{
			_regexContextContextMenu.Show(m_btnRegExp, new System.Drawing.Point(m_btnRegExp.Width, 0));
		}

		private void m_lnkSpecify_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			if (m_fObjectConcorded)
			{
				ICmObject cmCurrent = null;
				if (m_recordList.CurrentObjectHvo != 0)
				{
					var hvoCurrent = m_recordList.VirtualListPublisher.get_ObjectProp(m_recordList.CurrentObjectHvo, ConcDecorator.kflidAnalysis);
					if (hvoCurrent != 0)
					{
						cmCurrent = m_cache.ServiceLocator.GetObject(hvoCurrent);
					}
					// enhance JohnT: if we aren't concording on an analysis, we could still get the BeginOffset
					// from the ParaFragment, and figure which analysis that is part of or closest to.
				}
				ITsString tss = null;
				var ws = 0;
				var wordform = (IWfiWordform)cmCurrent?.OwnerOfClass(WfiWordformTags.kClassId);
				if (wordform != null)
				{
					tss = wordform.Form.BestVernacularAlternative;
					var ttp = tss.get_PropertiesAt(0);
					int nVar;
					ws = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
				}
				if (tss == null)
				{
					ws = m_cache.DefaultVernWs;
					tss = TsStringUtils.EmptyString(ws);
				}
				SetDefaultVisibilityOfItems(true, string.Empty);
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
			m_cbLine.Items.Add(new ConcordLine(ITextStrings.ksBaseline, WritingSystemServices.kwsVerns, ConcordanceLines.kBaseline));
			m_cbLine.Items.Add(new ConcordLine(ITextStrings.ksWord, WritingSystemServices.kwsVerns, ConcordanceLines.kWord));
			m_cbLine.Items.Add(new ConcordLine(ITextStrings.ksMorphemes, WritingSystemServices.kwsVerns, ConcordanceLines.kMorphemes));
			m_cbLine.Items.Add(new ConcordLine(ITextStrings.ksLexEntry, WritingSystemServices.kwsVerns, ConcordanceLines.kLexEntry));
			m_cbLine.Items.Add(new ConcordLine(ITextStrings.ksLexGloss, WritingSystemServices.kwsAnals, ConcordanceLines.kLexGloss));
			m_cbLine.Items.Add(new ConcordLine(AreaResources.Lex_Gram_Info, WritingSystemServices.kwsAnals, ConcordanceLines.kGramCategory));
			m_cbLine.Items.Add(new ConcordLine(ITextStrings.ksWordGloss, WritingSystemServices.kwsAnals, ConcordanceLines.kWordGloss));
			m_cbLine.Items.Add(new ConcordLine(ITextStrings.ksWordCat, WritingSystemServices.kwsAnals, ConcordanceLines.kWordCategory));
			m_cbLine.Items.Add(new ConcordLine(ITextStrings.ksFreeTranslation, WritingSystemServices.kwsAnals, ConcordanceLines.kFreeTranslation));
			m_cbLine.Items.Add(new ConcordLine(ITextStrings.ksLiteralTranslation, WritingSystemServices.kwsAnals, ConcordanceLines.kLiteralTranslation));
			m_cbLine.Items.Add(new ConcordLine(ITextStrings.ksNote, WritingSystemServices.kwsAnals, ConcordanceLines.kNote));
			m_cbLine.Items.Add(new ConcordLine(ITextStrings.ksTagging, WritingSystemServices.kwsAnals, ConcordanceLines.kTags));
			m_cbLine.SelectedIndex = 0;
		}

		private void SetConcordanceLine(ConcordanceLines line)
		{
			var idx = 0;
			for (var i = 0; i < m_cbLine.Items.Count; ++i)
			{
				if ((m_cbLine.Items[i] as ConcordLine).Line == line)
				{
					idx = i;
					break;
				}
			}
			if (m_cbLine.SelectedIndex != idx)
			{
				m_cbLine.SelectedIndex = idx;
			}
		}

		private void FillWritingSystemCombo(int wsMagic)
		{
			//store the current selection if any
			var current = m_cbWritingSystem.SelectedItem;
			m_cbWritingSystem.Items.Clear();
			var wsSet = 0;
			switch (wsMagic)
			{
				case WritingSystemServices.kwsVerns:
					foreach (var ws in m_cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems)
					{
						m_cbWritingSystem.Items.Add(ws);
					}
					wsSet = m_cache.DefaultVernWs;
					break;
				case WritingSystemServices.kwsAnals:
					foreach (var ws in m_cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
					{
						m_cbWritingSystem.Items.Add(ws);
					}
					wsSet = m_cache.DefaultAnalWs;
					break;
			}
			//Keep the users current selection if they have switched to a similar field (vernacular or analysis)
			if (current != null && m_cbWritingSystem.Items.Contains(current))
			{
				m_cbWritingSystem.SelectedItem = current;
			}
			else //otherwise set it to the default for the correct language type
			{
				SetWritingSystem(wsSet);
			}
		}

		private void SetWritingSystem(int ws)
		{
			var idx = -1;
			for (var i = 0; i < m_cbWritingSystem.Items.Count; ++i)
			{
				if (((CoreWritingSystemDefinition)m_cbWritingSystem.Items[i]).Handle == ws)
				{
					idx = i;
					break;
				}
			}
			if (idx == -1)
			{
				foreach (var wsObj in m_cache.ServiceLocator.WritingSystems.AllWritingSystems)
				{
					if (wsObj.Handle == ws)
					{
						m_cbWritingSystem.Items.Add(wsObj);
						idx = m_cbWritingSystem.Items.IndexOf(wsObj);
					}
				}
			}
			if (idx != -1 && m_cbWritingSystem.SelectedIndex != idx)
			{
				m_cbWritingSystem.SelectedIndex = idx;
			}
		}

		private static IParaFragment MakeOccurrence(IStTxtPara para, int ichMin, int ichLim)
		{
			var seg = para.SegmentsOS[0]; // Since we found something in the paragraph, assume it has at least one!
			foreach (var seg1 in para.SegmentsOS)
			{
				if (seg1.BeginOffset <= ichMin)
				{
					seg = seg1;
				}
				else
				{
					break;
				}
			}
			return new ParaFragment(seg, ichMin, ichLim, null);
		}

		private List<IParaFragment> UpdateConcordanceForBaseline(int ws)
		{
			var matcher = GetMatcher(ws) as SimpleStringMatcher;
			if (!matcher.IsValid())
			{
				return new List<IParaFragment>();
			}
			var occurrences = new List<IParaFragment>();
			var cPara = 0;
			foreach (var para in ParagraphsToSearch)
			{
				++cPara;
				// Find occurrences of the string in this paragraph.
				if (matcher.Matches(para.Contents))
				{
					// Create occurrences for each match.
					var results = matcher.GetAllResults();
					foreach (var range in results)
					{
						occurrences.Add(MakeOccurrence(para, range.IchMin, range.IchLim));
						if (occurrences.Count >= MaxConcordanceMatches())
						{
							MessageBox.Show(string.Format(ITextStrings.ksShowingOnlyTheFirstXXXMatches, occurrences.Count, cPara, ParagraphsToSearch.Count), ITextStrings.ksNotice, MessageBoxButtons.OK, MessageBoxIcon.Information);
							return occurrences;
						}
					}
				}
			}
			return occurrences;
		}

		private List<IParaFragment> UpdateConcordanceForWordCategory(int ws)
		{
			// Find analyses that have the relevant Category.
			var analyses = new HashSet<IAnalysis>();
			var hvoPossToMatch = GetHvoOfListItemToMatch(ws);
			foreach (var mb in m_cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().AllInstances())
			{
				if (mb.Analysis?.CategoryRA != null && hvoPossToMatch == mb.Analysis.CategoryRA.Hvo)
				{
					analyses.Add(mb);
				}
			}
			return GetOccurrencesOfAnalyses(analyses);
		}

		/// <summary>
		/// In the concordance the user selects the Lex Gram Info line along with a particular part of speech.
		/// This method finds each Wordform analysis with a morpheme that has the part of speech
		/// matching the selected part of speech.
		/// </summary>
		private List<IParaFragment> UpdateConcordanceForGramInfo(int ws)
		{
			// Find analyses that have the relevant morpheme.
			var analyses = new HashSet<IAnalysis>();
			var hvoPossToMatch = GetHvoOfListItemToMatch(ws);
			foreach (var mb in m_cache.ServiceLocator.GetInstance<IWfiMorphBundleRepository>().AllInstances())
			{
				if (mb.MsaRA?.ComponentsRS != null)
				{
					if (GetHvoOfMsaPartOfSpeech(mb.MsaRA).Contains(hvoPossToMatch))
					{
						analyses.Add(mb.Owner as IWfiAnalysis);
					}
				}
			}
			return GetOccurrencesOfAnalyses(analyses);
		}

		/// <summary>
		/// Get the hvo(s) for the Part of Speech for the various subclasses of MSA.
		/// N.B. If we add new subclasses or rearrange the class hierarchy, this will
		/// need to change.
		/// </summary>
		private static List<int> GetHvoOfMsaPartOfSpeech(IMoMorphSynAnalysis msa)
		{
			var result = new List<int>();
			ICmPossibility pos;
			if (msa is IMoInflAffMsa)
			{
				pos = ((IMoInflAffMsa)msa).PartOfSpeechRA;
				if (pos != null)
				{
					result.Add(pos.Hvo);
				}
			}
			if (msa is IMoStemMsa)
			{
				pos = ((IMoStemMsa)msa).PartOfSpeechRA;
				if (pos != null)
				{
					result.Add(pos.Hvo);
				}
			}
			if (msa is IMoDerivAffMsa)
			{
				var derivMsa = (IMoDerivAffMsa)msa;
				pos = derivMsa.ToPartOfSpeechRA;
				if (pos != null)
				{
					result.Add(pos.Hvo);
				}
				pos = derivMsa.FromPartOfSpeechRA;
				if (pos != null)
				{
					result.Add(pos.Hvo);
				}
			}
			if (msa is IMoDerivStepMsa)
			{
				pos = ((IMoDerivStepMsa)msa).PartOfSpeechRA;
				if (pos != null)
				{
					result.Add(pos.Hvo);
				}
			}
			if (msa is IMoUnclassifiedAffixMsa)
			{
				pos = ((IMoUnclassifiedAffixMsa)msa).PartOfSpeechRA;
				if (pos != null)
				{
					result.Add(pos.Hvo);
				}
			}
			return result;
		}

		/// <summary>
		/// This one matches on the Tags from the Tagging tab.
		/// </summary>
		private List<IParaFragment> UpdateConcordanceForTag(int ws)
		{
			// Find analyses that have the relevant morpheme.
			var matchedTags = new HashSet<ITextTag>();
			var hvoPossToMatch = GetHvoOfListItemToMatch(ws);
			var tagRepo = m_cache.ServiceLocator.GetInstance<ITextTagRepository>();
			foreach (var tagInstance in tagRepo.AllInstances())
			{
				// LT-10312 reopened: BestAnalysisAlternative is how we build the chooser list,
				// but now we want to search by possibility item, not by string matching.
				if (tagInstance.IsValidRef && tagInstance.TagRA != null && hvoPossToMatch == tagInstance.TagRA.Hvo)
				{
					matchedTags.Add(tagInstance);
				}
			}
			return GetParaFragmentsOfTags(matchedTags);
		}

		private List<IParaFragment> GetParaFragmentsOfTags(IEnumerable<ITextTag> matchedTags)
		{
			var result = new List<IParaFragment>();
			var interestingParas = ParagraphsToSearch;
			foreach (var tagInstance in matchedTags)
			{
				// Enhance GJM: This works until tags can span paragraphs
				var myPara = tagInstance.BeginSegmentRA.Owner as IStTxtPara;
				if (!interestingParas.Contains(myPara))
				{
					continue;
				}
				result.Add(MakeOccurrence(myPara, GetReferenceBeginOffsetInPara(tagInstance), GetReferenceEndOffsetInPara(tagInstance)));
			}
			return result;
		}

		private static int GetReferenceBeginOffsetInPara(IAnalysisReference refToAnalyses)
		{
			return refToAnalyses.BegRef().GetMyBeginOffsetInPara();
		}

		private static int GetReferenceEndOffsetInPara(IAnalysisReference refToAnalyses)
		{
			return refToAnalyses.EndRef().GetMyEndOffsetInPara();
		}

		/// <summary>
		/// Get the paragraphs we are interested in concording.
		/// </summary>
		private HashSet<IStTxtPara> ParagraphsToSearch
		{
			get
			{
				var result = new HashSet<IStTxtPara>();
				var needsParsing = new List<IStTxtPara>();
				var concDecorator = ConcDecorator;
				foreach (var sttext in concDecorator.InterestingTexts)
				{
					AddUnparsedParagraphs(sttext, needsParsing, result);
				}
				if (needsParsing.Any())
				{
					NonUndoableUnitOfWorkHelper.DoSomehow(m_cache.ActionHandlerAccessor, () =>
					{
						foreach (var para in needsParsing)
						{
							ParagraphParser.ParseParagraph(para);
							if (para.SegmentsOS.Count > 0)
							{
								result.Add(para);
							}
						}
					});
				}
				return result;
			}
		}

		private static void AddUnparsedParagraphs(IStText text, List<IStTxtPara> collectUnparsed, HashSet<IStTxtPara> collectUsefulParas)
		{
			foreach (IStTxtPara para in text.ParagraphsOS)
			{
				if (para.ParseIsCurrent)
				{
					if (para.SegmentsOS.Count > 0)
					{
						collectUsefulParas.Add(para);
					}
				}
				else
				{
					collectUnparsed.Add(para);
				}
			}
		}

		private List<IParaFragment> UpdateConcordanceForWord(int ws)
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
					{
						continue;
					}
					if (segs.Contains(seg))
					{
						continue; // wordform occurs in it more than once, but we only want to add the occurences once.
					}
					segs.Add(seg);
					foreach (var occurrence in seg.GetOccurrencesOfAnalysis(analysis, int.MaxValue, true))
					{
						result.Add(occurrence);
					}
				}
			}
			return result;
		}

		/// <summary>
		/// Concordance contains all occurrences of analyses which contain exactly the specified morpheme.
		/// A match may be either on the Form of the morph bundle, or on the form of the MoForm it points to.
		/// </summary>
		private List<IParaFragment> UpdateConcordanceForMorphemes(int ws)
		{
			// Find analyses that have the relevant morpheme.
			var analyses = new HashSet<IAnalysis>();
			var matcher = GetMatcher(ws);
			foreach (var mb in m_cache.ServiceLocator.GetInstance<IWfiMorphBundleRepository>().AllInstances())
			{
				if (mb.MorphRA != null && matcher.Matches(mb.MorphRA.Form.get_String(ws)) || matcher.Matches(mb.Form.get_String(ws)))
				{
					analyses.Add(mb.Owner as IWfiAnalysis);
				}
			}
			return GetOccurrencesOfAnalyses(analyses);
		}

		private int GetHvoOfListItemToMatch(int ws)
		{
			return ((HvoTreeNode)m_cbSearchText.SelectedItem).Hvo;
		}

		private IMatcher GetMatcher(int ws)
		{
			IMatcher matcher;
			SetupSearchPattern(ws);
			if (m_rbtnUseRegExp.Checked)
			{
				matcher = new RegExpMatcher(m_vwPattern);
			}
			else if (m_rbtnWholeItem.Checked)
			{
				// See whether we can use the MUCH more efficient ExactLiteralMatcher
				if (!m_vwPattern.UseRegularExpressions && m_vwPattern.MatchDiacritics && m_vwPattern.MatchOldWritingSystem && m_vwPattern.Pattern.RunCount == 1)
				{
					var target = m_vwPattern.Pattern.Text;
					int nVar;
					var wsMatch = m_vwPattern.Pattern.get_Properties(0).GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
					return m_vwPattern.MatchCase ? new ExactLiteralMatcher(target, wsMatch) : new ExactCaseInsensitiveLiteralMatcher(target, wsMatch);
				}
				matcher = new ExactMatcher(m_vwPattern);
			}
			else if (m_rbtnAtEnd.Checked)
			{
				matcher = new EndMatcher(m_vwPattern);
			}
			else if (m_rbtnAtStart.Checked)
			{
				matcher = new BeginMatcher(m_vwPattern);
			}
			else
			{
				matcher = new AnywhereMatcher(m_vwPattern);
			}
			if (!matcher.IsValid())
			{
				if (matcher is RegExpMatcher)
				{
					ShowRegExpMatcherError();
				}
				else
				{
					MessageBox.Show(this, matcher.ErrorMessage(), @"Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			return matcher;
		}

		private void ShowRegExpMatcherError()
		{
			var errMsg = "Invalid regular expression";
			MessageBox.Show(this, errMsg, @"Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		private void SetupSearchPattern(int ws)
		{
			m_vwPattern.MatchWholeWord = m_rbtnWholeItem.Checked;
			m_vwPattern.UseRegularExpressions = m_rbtnUseRegExp.Checked;
			m_vwPattern.MatchDiacritics = m_chkMatchDiacritics.Checked;
			m_vwPattern.MatchCase = m_chkMatchCase.Checked;
			m_vwPattern.Pattern = GetSearchText();
			m_vwPattern.IcuLocale = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(ws);
			m_vwPattern.MatchOldWritingSystem = true;
		}

		private ITsString GetSearchText()
		{
			return m_tbSearchText.Visible ? m_tbSearchText.Tss : ((HvoTreeNode)m_cbSearchText.SelectedItem).Tss;
		}

		/// <summary>
		/// Concordance contains all occurrences of analyses which contain the specified lex entry, in the sense
		/// that they point to an MoForm whose owner's LexemeForm matches the pattern.
		/// Enhance JohnT: the VC will show the citation form, in the (unlikely? impossible?) event that the
		/// LexemeForm doesn't have a form. Should we search there too?
		/// </summary>
		private List<IParaFragment> UpdateConcordanceForLexEntry(int ws)
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
		private List<IParaFragment> UpdateConcordanceForLexGloss(int ws)
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
		private List<IParaFragment> UpdateConcordanceForWordGloss(int ws)
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
		private List<IParaFragment> UpdateConcordanceForFreeTranslation(int ws)
		{
			var result = new List<IParaFragment>();
			var matcher = GetMatcher(ws);
			foreach (var para in ParagraphsToSearch)
			{
				foreach (var seg in para.SegmentsOS)
				{
					if (matcher.Matches(seg.FreeTranslation.get_String(ws)))
					{
						result.Add(MakeOccurrence(para, seg.BeginOffset, seg.EndOffset));
					}
				}
			}
			return result;
		}

		/// <summary>
		/// Here the match is a complete segment, if the requested literal translation matches.
		/// </summary>
		private List<IParaFragment> UpdateConcordanceForLiteralTranslation(int ws)
		{
			var result = new List<IParaFragment>();
			var matcher = GetMatcher(ws);
			foreach (var para in ParagraphsToSearch)
			{
				result.AddRange(para.SegmentsOS.Where(seg => matcher.Matches(seg.LiteralTranslation.get_String(ws))).Select(seg => MakeOccurrence(para, seg.BeginOffset, seg.EndOffset)));
			}
			return result;
		}

		/// <summary>
		/// Here the match is a complete segment, if one of its notes matches.
		/// </summary>
		private List<IParaFragment> UpdateConcordanceForNote(int ws)
		{
			var result = new List<IParaFragment>();
			var matcher = GetMatcher(ws);
			foreach (var para in ParagraphsToSearch)
			{
				foreach (var seg in para.SegmentsOS)
				{
					result.AddRange(seg.NotesOS.Where(note => matcher.Matches(note.Content.get_String(ws))).Select(note => MakeOccurrence(para, seg.BeginOffset, seg.EndOffset)));
				}
			}
			return result;
		}

		private void InitializeConcordanceSearch(string sMatch, int ws, ConcordanceLines line)
		{
			SetDefaultVisibilityOfItems(true, string.Empty);
			m_fObjectConcorded = false;
			if (string.IsNullOrEmpty(sMatch))
			{
				return;
			}
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
				m_lblTop.Text = string.Format(ITextStrings.ksConcordedOn0, sConcordedOn);
				m_lnkSpecify.Text = ITextStrings.ksSpecifyConcordanceCriteria_;
				m_lnkSpecify.Enabled = m_lnkSpecify.Visible = true;
				m_fwtbItem.Location = new Point(m_lblTop.Location.X + m_lblTop.Width + 10, m_lblTop.Location.Y);
				m_fwtbItem.BorderStyle = BorderStyle.None;
			}
			label2.Visible = fDefault;
			m_searchContentLabel.Visible = fDefault;
			label4.Visible = fDefault;
			m_cbLine.Visible = fDefault;
			m_cbWritingSystem.Visible = fDefault;
			m_tbSearchText.Visible = fDefault;
			m_cbSearchText.Visible = false; // See LT-12255.
			m_btnRegExp.Visible = fDefault;
			m_chkMatchCase.Visible = fDefault;
			m_chkMatchDiacritics.Visible = fDefault;
			groupBox1.Visible = fDefault;
			m_btnSearch.Visible = fDefault;
		}

		private void InitializeConcordanceSearch(ICmObject cmo)
		{
			InitializeConcordanceSearch(cmo, cmo.ShortNameTSS);
		}

		private void InitializeConcordanceSearch(ICmObject cmo, ITsString tssObj)
		{
			var sType = cmo.GetType().Name;
			var sTag = StringTable.Table.GetString(sType, StringTable.ClassNames);
			SetDefaultVisibilityOfItems(false, sTag);
			m_fObjectConcorded = true;
			m_hvoMatch = cmo.Hvo;
			m_backupHvo = cmo.Owner?.Hvo ?? 0;
			var ttpObj = tssObj.get_PropertiesAt(0);
			int nVar;
			var ws = ttpObj.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
			m_fwtbItem.WritingSystemCode = (ws > 0) ? ws : m_cache.DefaultVernWs;
			var dyHeight = m_fwtbItem.PreferredHeight;
			m_fwtbItem.Height = dyHeight;
			m_fwtbItem.Tss = tssObj;
			var dxWidth = m_fwtbItem.PreferredWidth;
			m_fwtbItem.Width = dxWidth;
			LoadMatches(true);
		}

		/// <summary>
		/// Select the Word Category line to search on and then select the part of speech to match the target
		/// then search based on those selections.
		/// </summary>
		private void InitializeConcordanceSearchWordPOS(ICmObject target)
		{
			if (!(target is IPartOfSpeech))
			{
				return;
			}
			var partOfSpeech = (IPartOfSpeech)target;
			SetConcordanceLine(ConcordanceLines.kWordCategory);
			m_pOSPopupTreeManager.LoadPopupTree(partOfSpeech.Hvo);
			LoadMatches(true);
			SaveSettings();
		}

		#endregion

		private void JumpToConcordance_Clicked(object sender, EventArgs e)
		{
			try
			{
				var tag = (List<object>)((ToolStripMenuItem)sender).Tag;
				var target = (ICmObject)tag[0];
				var clid = target.ClassID;
				switch (clid)
				{
					case WfiWordformTags.kClassId:
						var wwf = (IWfiWordform)target;
						int ws;
						ITsString tss;
						if (wwf.Form != null && wwf.Form.TryWs(WritingSystemServices.kwsFirstVern, out ws, out tss))
						{
							InitializeConcordanceSearch(tss.Text, ws, ConcordanceLines.kWord);
						}
						break;
					case LexEntryTags.kClassId:
					case LexSenseTags.kClassId:
					case WfiAnalysisTags.kClassId:
					case PartOfSpeechTags.kClassId:
					case WfiGlossTags.kClassId:
						if (((string)tag[1]).Equals(AreaServices.WordPartOfSpeech))
						{
							InitializeConcordanceSearchWordPOS(target);
						}
						else
						{
							InitializeConcordanceSearch(target);
						}
						break;
					case MoStemMsaTags.kClassId:
					case MoInflAffMsaTags.kClassId:
					case MoDerivAffMsaTags.kClassId:
					case MoUnclassifiedAffixMsaTags.kClassId:
						if (((string)tag[1]).Equals(AreaServices.PartOfSpeechGramInfo))
						{
							Debug.Assert(target is IMoMorphSynAnalysis);
							var msa = target as IMoMorphSynAnalysis;
							InitializeConcordanceSearch(target, msa.InterlinearNameTSS);
						}
						else
						{
							InitializeConcordanceSearch(target);
						}
						break;
					default:
						if (m_cache.ClassIsOrInheritsFrom(clid, MoFormTags.kClassId))
						{
							InitializeConcordanceSearch(target);
						}
						break;
				}
			}
			finally
			{
				// indicate that JumpToConcordance_Clicked has been handled.
				m_recordList.SuspendLoadingRecordUntilOnJumpToRecord = false;
			}
		}
	}
}