// Copyright (c) 2009-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Core.SpellChecking;
using SIL.LCModel.DomainServices;
using WaitCursor = SIL.FieldWorks.Common.FwUtils.WaitCursor;

namespace LanguageExplorer.Areas.TextsAndWords
{
	/// <summary />
	public partial class RespellerDlg : Form, IFlexComponent
	{
		private LcmCache m_cache;
		private XMLViewsDataCache m_specialSda;
		private IWfiWordform m_srcwfiWordform;
		private int m_vernWs;
		private IRecordList m_srcRecordList;
		private const string s_helpTopic = "khtpRespellerDlg";
		private string m_sMoreButtonText; // original text of More button
		private Size m_moreMinSize; // minimum size when 'more' options shown (=original size, minus a bit on height)
		private Size m_lessMinSize; // minimum size when 'less' options shown.
		// Preview related
		private bool m_previewOn;
		// original display of occurrences, when preview off
		private XElement m_oldOccurrenceColumn = null;
		// special preview display of occurrences, with string truncated after occurrence
		private XElement m_previewOccurrenceColumn;
		private string m_previewButtonText = null; // original text of the button (before changed to e.g. Clear).
		RespellUndoAction m_respellUndoaction; // created when we do a preview, used when check boxes change.
		// items still enabled.
		readonly HashSet<int> m_enabledItems = new HashSet<int>();
		// Flag set true if there are other known occurrences so AllChanged should always return false.
		bool m_fOtherOccurrencesExist;
		string m_lblExplainText; // original text of m_lblExplainDisabled
		// Typically the record list of the calling Words/Analysis view that manages a list of wordforms.
		// May be null when called from TE change spelling dialog.
		IRecordList m_wordformRecordList;
		int m_hvoNewWordform; // if we made a new wordform and changed all instances, this gets set.
		ISegmentRepository m_repoSeg;

		public RespellerDlg()
		{
			InitializeComponent();
			// Handle localization here since the dialog isn't localized, it can't be edited in the
			// designer to make it localized.
			m_cbUpdateLexicon.Text = TextAndWordsResources.ksUpdateMonoMorphemicLexicalEntries;
			m_btnClose.Text = LanguageExplorerResources.ksClose;
			label2.Text = TextAndWordsResources.ksNewSpelling;
			m_btnPreviewClear.Text = TextAndWordsResources.ksPreview;
			m_btnHelp.Text = LanguageExplorerResources.ksHelp;
			m_btnApply.Text = TextAndWordsResources.ksApply;
			m_rbKeepAnalyses.Text = TextAndWordsResources.ksKeepMultiMorphemicAnalyses;
			m_rbDiscardAnalyses.Text = TextAndWordsResources.ksDeleteMultiMorphemicAnalyses;
			m_btnMore.Text = TextAndWordsResources.ksMore;
			m_cbMaintainCase.Text = TextAndWordsResources.ksMaintainExistingCaseInBaseline;
			m_lblExplainDisabled.Text = TextAndWordsResources.ksSomeOptionsAreDisabledBecause_;
			m_cbCopyAnalyses.Text = TextAndWordsResources.ksCopyApprovedAnalysesToNewSpelling;
			m_optionsLabel.Text = TextAndWordsResources.ksOptions;
			Text = TextAndWordsResources.ksChangeSpelling;
		}

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		#endregion

		#region Implementation of IFlexComponent

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentParameters.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;
			m_cache = PropertyTable.GetValue<LcmCache>(LanguageExplorerConstants.cache);
		}

		#endregion

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				components?.Dispose();
				if (m_cbNewSpelling != null)
				{
					m_cbNewSpelling.TextChanged -= m_dstWordform_TextChanged;
				}
				if (m_sourceSentences != null)
				{
					m_sourceSentences.CheckBoxChanged -= sentences_CheckBoxChanged;
				}
				if (m_srcRecordList != null)
				{
					PropertyTable.GetValue<IRecordListRepository>(LanguageExplorerConstants.RecordListRepository).RemoveRecordList(m_srcRecordList);
				}
			}
			m_cache = null;
			m_srcwfiWordform = null;
			m_srcRecordList = null;
			PropertyTable = null;
			Publisher = null;
			Subscriber = null;

			base.Dispose(disposing);
		}

		/// <summary>
		/// This version is used inside FLEx when the friendly tool is not active. So, we need to
		/// build the concordance, but on FLEx's list, and we can assume all the parts and layouts
		/// are loaded.
		/// </summary>
		internal bool SetDlgInfo(IWfiWordform wf, XElement configurationParams)
		{
			using (var dlg = new ProgressDialogWorkingOn())
			{
				dlg.Owner = ActiveForm;
				if (dlg.Owner != null) // I think it's only null when debugging? But play safe.
				{
					dlg.Icon = dlg.Owner.Icon;
				}
				dlg.Text = TextAndWordsResources.ksFindingOccurrences;
				dlg.WorkingOnText = TextAndWordsResources.ksSearchingOccurrences;
				dlg.ProgressLabel = TextAndWordsResources.ksProgress;
				dlg.Show(ActiveForm);
				dlg.Update();
				dlg.BringToFront();
				try
				{
					var cache = wf.Cache;
					m_srcwfiWordform = wf;
					m_cache = cache;
					return SetDlgInfoPrivate(configurationParams);
				}
				finally
				{
					dlg.Close();
				}
			}
		}

		internal bool SetDlgInfo(XElement configurationParameters)
		{
			m_wordformRecordList = PropertyTable.GetValue<IRecordListRepository>(LanguageExplorerConstants.RecordListRepository).GetRecordList(TextAndWordsArea.ConcordanceWords);
			m_wordformRecordList.SuppressSaveOnChangeRecord = true; // various things trigger change record and would prevent Undo

			//We need to re-parse the interesting texts so that the rows in the dialog show all the occurrences (make sure it is up to date)
			if (m_wordformRecordList is InterlinearTextsRecordList)
			{
				//Un-suppress to allow for the list to be reloaded during ParseInterestingTextsIfNeeded()
				//(this record list and its list are not visible in this dialog, so there will be no future reload)
				m_wordformRecordList.ListLoadingSuppressed = false;
				(m_wordformRecordList as InterlinearTextsRecordList).ParseInterestingTextsIfNeeded(); //Trigger the parsing
			}
			m_srcwfiWordform = (IWfiWordform)m_wordformRecordList.CurrentObject;
			return SetDlgInfoPrivate(configurationParameters);
		}

		private bool SetDlgInfoPrivate(XElement configurationParameters)
		{
			using (new WaitCursor(this))
			{
				m_btnRefresh.Image = ResourceHelper.RefreshIcon;
				m_rbDiscardAnalyses.Checked = PropertyTable.GetValue<bool>("RemoveAnalyses");
				m_rbKeepAnalyses.Checked = !m_rbDiscardAnalyses.Checked;
				m_rbDiscardAnalyses.Click += m_rbDiscardAnalyses_Click;
				m_rbKeepAnalyses.Click += m_rbDiscardAnalyses_Click;
				m_cbUpdateLexicon.Checked = PropertyTable.GetValue<bool>("UpdateLexiconIfPossible");
				m_cbCopyAnalyses.Checked = PropertyTable.GetValue<bool>("CopyAnalysesToNewSpelling");
				m_cbCopyAnalyses.Click += m_cbCopyAnalyses_Click;
				m_cbMaintainCase.Checked = PropertyTable.GetValue<bool>("MaintainCaseOnChangeSpelling");
				m_cbMaintainCase.Click += m_cbMaintainCase_Click;
				m_cache = PropertyTable.GetValue<LcmCache>(LanguageExplorerConstants.cache);
				// We need to use the 'best vern' ws,
				// since that is what is showing in the Words-Analyses detail edit control.
				// Access to this respeller dlg is currently (Jan. 2008) only via a context menu in the detail edit pane.
				// The user may be showing multiple wordform WSes in the left hand browse view,
				// but we have no way of knowing if the user thinks one of those alternatives is wrong without asking.
				m_vernWs = WritingSystemServices.ActualWs(m_cache, WritingSystemServices.kwsFirstVern, m_srcwfiWordform.Hvo, WfiWordformTags.kflidForm);
				// Bail out if no vernacular writing system was found (see LT-8892).
				Debug.Assert(m_vernWs != 0);
				if (m_vernWs == 0)
				{
					return false;
				}
				// Bail out, rather than run into a null reference exception.
				// (Should fix LT-7666.)
				var vernForm = m_srcwfiWordform.Form.get_String(m_vernWs);
				if (vernForm == null || vernForm.Length == 0)
				{
					return false;
				}
				m_cbNewSpelling.WritingSystemFactory = m_cache.LanguageWritingSystemFactoryAccessor;
				m_cbNewSpelling.WritingSystemCode = m_vernWs;
				m_cbNewSpelling.StyleSheet = FwUtils.StyleSheetFromPropertyTable(PropertyTable);
				Debug.Assert(m_cbNewSpelling.StyleSheet != null); // if it is we get a HUGE default font (and can't get the correct size)
				if (m_cbNewSpelling.WritingSystemFactory.get_EngineOrNull(m_vernWs).RightToLeftScript)
				{
					m_cbNewSpelling.RightToLeft = RightToLeft.Yes;
				}
				m_cbNewSpelling.Tss = vernForm;
				m_cbNewSpelling.AdjustForStyleSheet(this, null, m_cbNewSpelling.StyleSheet);
				if (!Application.RenderWithVisualStyles)
				{
					m_cbNewSpelling.Padding = new Padding(1, 2, 1, 1);
				}
				SetSuggestions();
				m_btnApply.Enabled = false;
				m_cbNewSpelling.TextChanged += m_dstWordform_TextChanged;

#if RANDYTODO
				/*
          <clerk id="SrcWfiWordformConc" shouldHandleDeletion="false">
            <dynamicloaderinfo assemblyPath="MorphologyEditorDll.dll" class="SIL.FieldWorks.XWorks.MorphologyEditor.RespellerTemporaryRecordClerk" />
            <recordList class="WfiWordform" field="Occurrences">
              <dynamicloaderinfo assemblyPath="MorphologyEditorDll.dll" class="SIL.FieldWorks.XWorks.MorphologyEditor.RespellerRecordList" />
              <decoratorClass assemblyPath="MorphologyEditorDll.dll" class="SIL.FieldWorks.XWorks.MorphologyEditor.RespellingSda" />
            </recordList>
            <filters />
            <sortMethods />
          </clerk>
				*/
				// Setup source browse view.
				var toolNode = configurationParameters.SelectSingleNode("controls/control[@id='srcSentences']/parameters");
				m_srcRecordList = RecordClerkFactory.CreateClerk(PropertyTable, Publisher, Subscriber, true);
				m_srcRecordList.OwningObject = m_srcwfiWordform;
#endif
				m_sourceSentences.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				m_sourceSentences.CheckBoxChanged += sentences_CheckBoxChanged;
				m_specialSda = m_sourceSentences.BrowseViewer.SpecialCache;
				m_moreMinSize = Size;
				m_moreMinSize.Height -= m_sourceSentences.Height / 2;
				m_lessMinSize = m_moreMinSize;
				m_lessMinSize.Height -= m_optionsPanel.Height;
				AdjustHeightAndMinSize(Height - m_optionsPanel.Height, m_lessMinSize);
				m_optionsPanel.Visible = false;
				m_btnMore.Image = ResourceHelper.MoreButtonDoubleArrowIcon;
				m_btnMore.Click += btnMore_Click;
				m_sMoreButtonText = m_btnMore.Text;
				m_optionsPanel.Paint += m_optionsPanel_Paint;
				m_btnPreviewClear.Click += m_btnPreviewClear_Click;
				var specialMdc = m_specialSda.MetaDataCache;
				var madeUpFieldIdentifier = specialMdc.GetFieldId2(WfiWordformTags.kClassId, "Occurrences", false);
				var concordanceItems = m_specialSda.VecProp(m_srcwfiWordform.Hvo, madeUpFieldIdentifier);
				// (Re)set selected state in cache, so default behavior of checked is used.
				foreach (var concId in concordanceItems)
				{
					m_specialSda.SetInt(concId, m_sourceSentences.BrowseViewer.PreviewEnabledTag, 1);
					m_specialSda.SetInt(concId, XMLViewsDataCache.ktagItemSelected, 1);
				}
				// We initially check everything.
				foreach (var hvo in m_sourceSentences.BrowseViewer.AllItems)
				{
					m_enabledItems.Add(hvo);
				}
				m_lblExplainText = m_lblExplainDisabled.Text;
				// We only reload the list when refresh is pressed.
				m_srcRecordList.ListLoadingSuppressed = true;
				CheckForOtherOccurrences();
				SetEnabledState();
			}
			return true;
		}

		private void CheckForOtherOccurrences()
		{
			var allAnalysesCandidatesOfWordform = new List<IAnalysis>
			{
				m_srcwfiWordform
			};
			foreach (var anal in m_srcwfiWordform.AnalysesOC)
			{
				allAnalysesCandidatesOfWordform.Add(anal);
				allAnalysesCandidatesOfWordform.AddRange(anal.MeaningsOC);
			}
			var allUsedSegments = new HashSet<ISegment>();
			foreach (var segment in m_cache.ServiceLocator.GetInstance<ISegmentRepository>().AllInstances().Where(segment => segment.AnalysesRS.Any(allAnalysesCandidatesOfWordform.Contains)))
			{
				allUsedSegments.Add(segment);
			}
			// There are 'other' occurrences if some of the real ones aren't in the displayed list.
			if (m_repoSeg == null)
			{
				m_repoSeg = m_cache.ServiceLocator.GetInstance<ISegmentRepository>();
			}
			var enabledSegments = new HashSet<ISegment>();
			foreach (var hvoFake in m_enabledItems)
			{
				var hvoSeg = m_specialSda.get_ObjectProp(hvoFake, ConcDecorator.kflidSegment);
				if (hvoSeg > 0)
				{
					enabledSegments.Add(m_repoSeg.GetObject(hvoSeg));
				}
			}
			m_fOtherOccurrencesExist = allUsedSegments.Union(enabledSegments).Count() != allUsedSegments.Count();
		}

		private void m_btnPreviewClear_Click(object sender, EventArgs e)
		{
			if (m_previewOn)
			{
				m_previewOn = false;
				m_sourceSentences.BrowseViewer.PreviewColumn = -1;
				m_sourceSentences.BrowseViewer.ReplaceColumn("Occurrence", m_oldOccurrenceColumn);
				m_btnPreviewClear.Text = m_previewButtonText;
				m_respellUndoaction = null;
			}
			else
			{
				m_previewOn = true;
				var previewTag = m_sourceSentences.BrowseViewer.PreviewValuesTag;
				// Initialize PrecedingContext etc for each occurrence (text up to and including old spelling)
				MakeUndoAction();
				m_respellUndoaction.SetupPreviews(RespellingSda.kflidSpellingPreview, previewTag, RespellingSda.kflidAdjustedBeginOffset, RespellingSda.kflidAdjustedEndOffset,
					m_sourceSentences.BrowseViewer.PreviewEnabledTag, m_sourceSentences.BrowseViewer.AllItems, m_sourceSentences.BrowseViewer.BrowseView.RootBox);
				// Create m_previewOccurrenceColumn if needed
				EnsurePreviewColumn();
				m_oldOccurrenceColumn = m_sourceSentences.BrowseViewer.ReplaceColumn("Occurrence", m_previewOccurrenceColumn);
				m_previewButtonText = m_btnPreviewClear.Text;
				m_btnPreviewClear.Text = TextAndWordsResources.ksClear;
			}
		}

		private void MakeUndoAction()
		{
			m_respellUndoaction = new RespellUndoAction(m_specialSda, m_cache, m_srcwfiWordform.Form.get_String(m_vernWs).Text, m_cbNewSpelling.Text, m_vernWs)
			{
				PreserveCase = m_cbMaintainCase.Checked
			};
			var tagEnabled = m_sourceSentences.BrowseViewer.PreviewEnabledTag;
			foreach (var hvo in m_sourceSentences.BrowseViewer.CheckedItems)
			{
				if (m_specialSda.get_IntProp(hvo, tagEnabled) == 1)
				{
					m_respellUndoaction.AddOccurrence(hvo);
				}
			}
		}

		// Create the preview column if we haven't already.
		private void EnsurePreviewColumn()
		{
			if (m_previewOccurrenceColumn != null)
			{
				return;
			}
			var fRtl = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.RightToLeftScript;
			var insert = string.Empty;
			if (fRtl)
			{
				insert = "<righttoleft value=\"on\"/>";
			}
			var doc = XDocument.Parse(
				  "<column label=\"Occurrence\" width=\"415000\" multipara=\"true\" doNotPersist=\"true\">"
				+ "<concpara min=\"FakeOccurrence.AdjustedBeginOffset\" lim=\"FakeOccurrence.AdjustedEndOffset\" align=\"144000\">"
				+ "<properties><editable value=\"false\"/>" + insert + "</properties>"
				+ "<string class=\"FakeOccurrence\" field=\"SpellingPreview\"/>"
				+ "<preview ws=\"vernacular\"/>"
				+ "</concpara>"
				+ "</column>");
			m_previewOccurrenceColumn = doc.Root;
		}

		private void SetSuggestions()
		{
			var dict = SpellingHelper.GetSpellChecker(m_vernWs, m_cache.LanguageWritingSystemFactoryAccessor);
			if (dict == null)
			{
				return;
			}
			var suggestions = dict.Suggest(m_cbNewSpelling.Text);
			foreach (var suggestion in suggestions)
			{
				m_cbNewSpelling.Items.Add(suggestion);
			}
		}

		protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);
			m_cbNewSpelling.Select();
			m_cbNewSpelling.SelectAll();
		}

		/// <summary>
		/// Adjust our height (but, since we're changing the height to hide a panel at the bottom,
		/// do NOT move controls docked bottom).
		/// </summary>
		private void AdjustHeightAndMinSize(int newHeight, Size newMinSize)
		{
			var bottomControls = new List<Control>();
			SuspendLayout();
			foreach (Control c in Controls)
			{
				if (((int)c.Anchor & (int)AnchorStyles.Bottom) == 0)
				{
					continue;
				}

				bottomControls.Add(c);
				c.Anchor = (AnchorStyles)((int)c.Anchor & ~((int)AnchorStyles.Bottom));
			}
			if (newHeight < Height)
			{
				// Adjust minsize first, lest new height is less than old minimum.
				MinimumSize = newMinSize;
				Height = newHeight;
			}
			else // increasing height
			{
				// Adjust height first, lest old height is less than new minimum.
				Height = newHeight;
				MinimumSize = newMinSize;
			}
			PerformLayout();
			foreach (var c in bottomControls)
			{
				c.Anchor = (AnchorStyles)((int)c.Anchor | ((int)AnchorStyles.Bottom));
			}
			ResumeLayout();
		}

		private void sentences_CheckBoxChanged(object sender, CheckBoxChangedEventArgs e)
		{
			if (m_respellUndoaction != null)
			{
				// We're displaying a Preview, and need to update things.
				if (e.HvosChanged.Length == 1)
				{
					// Single check box clicked, update with PropChanged
					var hvo = e.HvosChanged[0];
					// We only consider it 'checked' if both checked AND enabled.
					var itemChecked = GetItemChecked(hvo);
					m_respellUndoaction?.UpdatePreview(hvo, itemChecked);
				}
				else
				{
					// Update the status of items
					foreach (var hvo in e.HvosChanged)
					{
						if (GetItemChecked(hvo))
						{
							m_respellUndoaction.AddOccurrence(hvo);
						}
						else
						{
							m_respellUndoaction.RemoveOccurrence(hvo);
						}
					}
					// the regenerate all the previews.
					m_respellUndoaction.UpdatePreviews(true);
				}
			}
			SetEnabledState();
		}

		private bool GetItemChecked(int hvo)
		{
			return m_sourceSentences.BrowseViewer.IsItemChecked(hvo) && m_specialSda.get_IntProp(hvo, m_sourceSentences.BrowseViewer.PreviewEnabledTag) == 1;
		}

		private bool WordformHasMonomorphemicAnalyses => m_srcwfiWordform.IsValidObject && m_srcwfiWordform.AnalysesOC.Any(analysis => analysis.MorphBundlesOS.Count == 1);

		private bool WordformHasMultimorphemicAnalyses => m_srcwfiWordform.IsValidObject && m_srcwfiWordform.AnalysesOC.Any(analysis => analysis.MorphBundlesOS.Count > 1);

		private void SetEnabledState()
		{
			var enabledBasic = !string.IsNullOrEmpty(m_cbNewSpelling.Text) && m_cbNewSpelling.Text != m_srcwfiWordform.Form.get_String(m_vernWs).Text;
			bool someWillChange;
			var changeAll = AllWillChange(out someWillChange);
			m_btnApply.Enabled = enabledBasic && someWillChange;
			m_btnPreviewClear.Enabled = enabledBasic && someWillChange; // todo: also if 'clear' needed.
			m_cbUpdateLexicon.Enabled = changeAll && WordformHasMonomorphemicAnalyses;
			m_rbDiscardAnalyses.Enabled = m_rbKeepAnalyses.Enabled = changeAll && WordformHasMultimorphemicAnalyses;
			m_cbCopyAnalyses.Enabled = someWillChange && !changeAll;
			if (changeAll)
			{
				m_lblExplainDisabled.Text = string.Empty;
			}
			else if (m_fOtherOccurrencesExist)
			{
				m_lblExplainDisabled.Text = TextAndWordsResources.ksExplainDisabledScripture;
			}
			else
			{
				m_lblExplainDisabled.Text = m_lblExplainText; // original message says not all checked.
			}
		}

		#region Event handlers

		private void m_cbUpdateLexicon_Click(object sender, EventArgs e)
		{
			PropertyTable.SetProperty("UpdateLexiconIfPossible", m_cbUpdateLexicon.Checked, true, settingsGroup: SettingsGroup.GlobalSettings);
		}

		void m_rbDiscardAnalyses_Click(object sender, EventArgs e)
		{
			PropertyTable.SetProperty("RemoveAnalyses", m_rbDiscardAnalyses.Checked, true, settingsGroup: SettingsGroup.GlobalSettings);
		}

		void m_cbCopyAnalyses_Click(object sender, EventArgs e)
		{
			PropertyTable.SetProperty("CopyAnalysesToNewSpelling", m_cbCopyAnalyses.Checked, true, settingsGroup: SettingsGroup.GlobalSettings);
		}

		void m_cbMaintainCase_Click(object sender, EventArgs e)
		{
			PropertyTable.SetProperty("MaintainCaseOnChangeSpelling", m_cbMaintainCase.Checked, true, settingsGroup: SettingsGroup.GlobalSettings);
		}

		protected override void OnClosed(EventArgs e)
		{
			// Any way we get closed we want to be sure this gets restored.
			if (m_wordformRecordList != null)
			{
				m_wordformRecordList.SuppressSaveOnChangeRecord = false;
				if (m_hvoNewWordform != 0)
				{
					// Move the record list to the new word if possible.
					m_wordformRecordList.JumpToRecord(m_hvoNewWordform);
				}
			}
			base.OnClosed(e);
		}

		private void m_btnClose_Click(object sender, EventArgs e)
		{
			if (ChangesWereMade)
			{
				PropertyTable.GetValue<IFwMainWnd>(FwUtils.window).RefreshAllViews();
			}
			Close();
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider), "FLExHelpFile", s_helpTopic);
		}

		private void m_dstWordform_TextChanged(object sender, EventArgs e)
		{
			SetEnabledState();
		}

		/// <summary>
		/// Flag set if we changed anything and need to reload lists.
		/// </summary>
		public bool ChangesWereMade { get; set; }

		private void m_btnApply_Click(object sender, EventArgs e)
		{
			if (!m_sourceSentences.CheckedItems.Any())
			{
				return;
			}
			using (new WaitCursor(this))
			{
				// NB: occurrence may ref. wf, anal, or gloss.
				// NB: Need to support selective spelling change.
				//	(gunaa->gwnaa when it means woman, but not for gu-naa, IMP+naa)
				if (m_respellUndoaction == null)
				{
					MakeUndoAction();
				}
				bool someWillChange;
				if (AllWillChange(out someWillChange))
				{
					// No point in letting the user think they can make more changes.
					m_cbNewSpelling.Enabled = false;

					m_respellUndoaction.AllChanged = true;
					m_respellUndoaction.KeepAnalyses = !m_rbDiscardAnalyses.Checked;
					m_respellUndoaction.UpdateLexicalEntries = m_cbUpdateLexicon.Checked;
				}
				else
				{
					m_respellUndoaction.CopyAnalyses = m_cbCopyAnalyses.Checked;
				}
				m_respellUndoaction.DoIt(Publisher);
				// On the other hand, we don't want to update the new wordform until after DoIt...it might not exist before,
				// and we won't be messing up any existing occurrences.
				Publisher.Publish("ItemDataModified", m_cache.ServiceLocator.GetObject(m_respellUndoaction.NewWordform));
				ChangesWereMade = true;
				m_respellUndoaction.RemoveChangedItems(m_enabledItems, m_sourceSentences.BrowseViewer.PreviewEnabledTag);
				// If everything changed remember the new wordform.
				if (m_respellUndoaction.AllChanged)
				{
					m_hvoNewWordform = m_respellUndoaction.NewWordform;
				}
				if (m_previewOn)
				{
					EnsurePreviewOff(); // will reconstruct
				}
				else
				{
					m_respellUndoaction = null; // Make sure we use a new one for any future previews or Apply's.
					m_sourceSentences.BrowseViewer.ReconstructView(); // ensure new offsets used.
				}
				SetEnabledState();
			}
		}

		/// <summary>
		/// Return true if every remaining item will change. True if all enabled items are also checked, and there
		/// are no known items that aren't listed at all (e.g., Scripture not currently included).
		/// </summary>
		/// <param name="someWillChange">Set true if some item will change, that is, some checked item is enabled</param>
		private bool AllWillChange(out bool someWillChange)
		{
			var checkedItems = new HashSet<int>(m_sourceSentences.CheckedItems);
			var changeCount = checkedItems.Intersect(m_enabledItems).Count();
			someWillChange = changeCount > 0;
			return changeCount == m_enabledItems.Count && !m_fOtherOccurrencesExist;
		}

		/// <summary>
		/// Show more options.
		/// </summary>
		protected void btnMore_Click(object sender, System.EventArgs e)
		{
			m_btnMore.Text = TextAndWordsResources.ksLess;
			m_btnMore.Image = ResourceHelper.LessButtonDoubleArrowIcon;
			m_btnMore.Click -= btnMore_Click;
			m_btnMore.Click += btnLess_Click;
			AdjustHeightAndMinSize(Height + m_optionsPanel.Height, m_moreMinSize);
			m_optionsPanel.Visible = true;
		}

		/// <summary>
		/// Show fewer options.
		/// </summary>
		protected void btnLess_Click(object sender, System.EventArgs e)
		{
			m_btnMore.Text = m_sMoreButtonText;
			m_btnMore.Image = ResourceHelper.MoreButtonDoubleArrowIcon;
			m_btnMore.Click += btnMore_Click;
			m_btnMore.Click -= btnLess_Click;
			AdjustHeightAndMinSize(Height - m_optionsPanel.Height, m_lessMinSize);
			m_optionsPanel.Visible = false;
		}

		/// <summary>
		/// Draws an etched line on the dialog to separate the Search Options from the
		/// basic controls.
		/// </summary>
		void m_optionsPanel_Paint(object sender, PaintEventArgs e)
		{
			const int dxMargin = 10;
			var left = m_optionsLabel.Right;
			LineDrawing.Draw(e.Graphics, left, (m_optionsLabel.Top + m_optionsLabel.Bottom) / 2, m_optionsPanel.Right - left - dxMargin, LineTypes.Etched);
		}

		#endregion Event handlers

		private void m_refreshButton_Click(object sender, EventArgs e)
		{
			// Make sure preview is off.
			EnsurePreviewOff();
			// Check all items still enabled
			foreach (var hvoFake in m_enabledItems)
			{
				m_specialSda.SetInt(hvoFake, XMLViewsDataCache.ktagItemSelected, 1);
			}
			// Reconstruct the main list of objects we are showing.
			m_srcRecordList.ListLoadingSuppressed = false;
			m_srcRecordList.OnRefresh(null);
			m_srcRecordList.ListLoadingSuppressed = true;
		}

		private void EnsurePreviewOff()
		{
			if (m_previewOn)
			{
				m_btnPreviewClear_Click(this, new EventArgs());
			}
		}

		private void m_cbMaintainCase_CheckedChanged(object sender, EventArgs e)
		{
			if (m_respellUndoaction == null)
			{
				return;
			}
			m_respellUndoaction.PreserveCase = m_cbMaintainCase.Checked;
			m_respellUndoaction.UpdatePreviews(false);
			m_sourceSentences.BrowseViewer.ReconstructView();
		}
	}
}