// Copyright (c) 2009-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.IText;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Drawing;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.XWorks;

namespace LanguageExplorer.Areas.TextsAndWords
{
#if RANDYTODO
	// TODO: After move, spin off all other classes.
#endif
	/// <summary />
	public partial class RespellerDlg : Form, IFlexComponent
	{
		private FdoCache m_cache;
		private XMLViewsDataCache m_specialSda;
		private IWfiWordform m_srcwfiWordform;
		private int m_vernWs;
		private RecordClerk m_srcClerk;
		private RecordClerk m_dstClerk;
		private const string s_helpTopic = "khtpRespellerDlg";
		private string m_sMoreButtonText; // original text of More button
		private Size m_moreMinSize; // minimum size when 'more' options shown (=original size, minus a bit on height)
		private Size m_lessMinSize; // minimum size when 'less' options shown.

		// Preview related
		private bool m_previewOn;
		private XmlNode m_oldOccurrenceColumn = null; // original display of occurrences, when preview off
		// special preview display of occurrences, with string truncated after occurrence
		private XmlNode m_previewOccurrenceColumn;
		private string m_previewButtonText = null; // original text of the button (before changed to e.g. Clear).
		RespellUndoAction m_respellUndoaction; // created when we do a preview, used when check boxes change.

		readonly HashSet<int> m_enabledItems = new HashSet<int>(); // items still enabled.
		// Flag set true if there are other known occurrences so AllChanged should always return false.
		bool m_fOtherOccurrencesExist;

		string m_lblExplainText; // original text of m_lblExplainDisabled

		// Typically the clerk of the calling Words/Analysis view that manages a list of wordforms.
		// May be null when called from TE change spelling dialog.
		RecordClerk m_wfClerk;
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

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="propertyTable">Interface to a property table.</param>
		/// <param name="publisher">Interface to the publisher.</param>
		/// <param name="subscriber">Interface to the subscriber.</param>
		public void InitializeFlexComponent(IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber)
		{
			FlexComponentCheckingService.CheckInitializationValues(propertyTable, publisher, subscriber, PropertyTable, Publisher, Subscriber);

			PropertyTable = propertyTable;
			Publisher = publisher;
			Subscriber = subscriber;
		}

		#endregion

		/// <summary>
		/// This version is used inside FLEx when the friendly tool is not active. So, we need to
		/// build the concordance, but on FLEx's list, and we can assume all the parts and layouts
		/// are loaded.
		/// </summary>
		internal bool SetDlgInfo(IWfiWordform wf, XmlNode configurationParams)
		{
			using (var dlg = new ProgressDialogWorkingOn())
			{
				dlg.Owner = ActiveForm;
				if (dlg.Owner != null) // I think it's only null when debugging? But play safe.
					dlg.Icon = dlg.Owner.Icon;
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

		internal bool SetDlgInfo(XmlNode configurationParameters)
		{
			m_wfClerk = PropertyTable.GetValue<RecordClerk>("RecordClerk-concordanceWords");
			m_wfClerk.SuppressSaveOnChangeRecord = true; // various things trigger change record and would prevent Undo

			//We need to re-parse the interesting texts so that the rows in the dialog show all the occurrences (make sure it is up to date)
			if(m_wfClerk is InterlinearTextsRecordClerk)
			{
				//Unsuppress to allow for the list to be reloaded during ParseInterstingTextsIfNeeded()
				//(this clerk and its list are not visible in this dialog, so there will be no future reload)
				m_wfClerk.ListLoadingSuppressed = false;
				(m_wfClerk as InterlinearTextsRecordClerk).ParseInterstingTextsIfNeeded(); //Trigger the parsing
			}
			m_srcwfiWordform = (IWfiWordform)m_wfClerk.CurrentObject;
			return SetDlgInfoPrivate(configurationParameters);
		}

		private bool SetDlgInfoPrivate(XmlNode configurationParameters)
		{
			using (new WaitCursor(this))
			{
				m_btnRefresh.Image = ResourceHelper.RefreshIcon;

				m_rbDiscardAnalyses.Checked = PropertyTable.GetValue("RemoveAnalyses", true);
				m_rbKeepAnalyses.Checked = !m_rbDiscardAnalyses.Checked;
				m_rbDiscardAnalyses.Click += m_rbDiscardAnalyses_Click;
				m_rbKeepAnalyses.Click += m_rbDiscardAnalyses_Click;

				m_cbUpdateLexicon.Checked = PropertyTable.GetValue("UpdateLexiconIfPossible", true);
				m_cbCopyAnalyses.Checked = PropertyTable.GetValue("CopyAnalysesToNewSpelling", true);
				m_cbCopyAnalyses.Click += m_cbCopyAnalyses_Click;
				m_cbMaintainCase.Checked = PropertyTable.GetValue("MaintainCaseOnChangeSpelling", true);
				m_cbMaintainCase.Click += m_cbMaintainCase_Click;
				m_cache = PropertyTable.GetValue<FdoCache>("cache");

				// We need to use the 'best vern' ws,
				// since that is what is showing in the Words-Analyses detail edit control.
				// Access to this respeller dlg is currently (Jan. 2008) only via a context menu in the detail edit pane.
				// The user may be showing multiple wordform WSes in the left hand browse view,
				// but we have no way of knowing if the user thinks one of those alternatives is wrong without asking.
				m_vernWs = WritingSystemServices.ActualWs(m_cache,
					WritingSystemServices.kwsFirstVern,
					m_srcwfiWordform.Hvo,
					WfiWordformTags.kflidForm);
				// Bail out if no vernacular writing system was found (see LT-8892).
				Debug.Assert(m_vernWs != 0);
				if (m_vernWs == 0)
					return false;
				// Bail out, rather than run into a null reference exception.
				// (Should fix LT-7666.)
				var vernForm = m_srcwfiWordform.Form.get_String(m_vernWs);
				if (vernForm == null || vernForm.Length == 0)
					return false;

				m_cbNewSpelling.WritingSystemFactory = m_cache.LanguageWritingSystemFactoryAccessor;
				m_cbNewSpelling.WritingSystemCode = m_vernWs;
				m_cbNewSpelling.StyleSheet = FontHeightAdjuster.StyleSheetFromPropertyTable(PropertyTable);
				Debug.Assert(m_cbNewSpelling.StyleSheet != null); // if it is we get a HUGE default font (and can't get the correct size)
				if (m_cbNewSpelling.WritingSystemFactory.get_EngineOrNull(m_vernWs).RightToLeftScript)
				{
					m_cbNewSpelling.RightToLeft = RightToLeft.Yes;
				}
				m_cbNewSpelling.Tss = vernForm;
				m_cbNewSpelling.AdjustForStyleSheet(this, null, m_cbNewSpelling.StyleSheet);
				if (!Application.RenderWithVisualStyles)
					m_cbNewSpelling.Padding = new Padding(1, 2, 1, 1);

				SetSuggestions();

				m_btnApply.Enabled = false;
				m_cbNewSpelling.TextChanged += m_dstWordform_TextChanged;

#if RANDYTODO
				// Setup source browse view.
				var toolNode = configurationParameters.SelectSingleNode("controls/control[@id='srcSentences']/parameters");
				m_srcClerk = RecordClerkFactory.CreateClerk(PropertyTable, Publisher, Subscriber, true);
				m_srcClerk.OwningObject = m_srcwfiWordform;
#endif
				m_sourceSentences.InitializeFlexComponent(PropertyTable, Publisher, Subscriber);
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
				int fakeFlid = specialMdc.GetFieldId2(WfiWordformTags.kClassId, "Occurrences", false);
				int[] concordanceItems = m_specialSda.VecProp(m_srcwfiWordform.Hvo, fakeFlid);
				// (Re)set selected state in cache, so default behavior of checked is used.
				foreach (var concId in concordanceItems)
				{
					m_specialSda.SetInt(concId, m_sourceSentences.BrowseViewer.PreviewEnabledTag, 1);
					m_specialSda.SetInt(concId, XMLViewsDataCache.ktagItemSelected, 1);
				}
				// We initially check everything.
				var segmentRepos = m_cache.ServiceLocator.GetInstance<ISegmentRepository>();
				foreach (var hvo in m_sourceSentences.BrowseViewer.AllItems)
					m_enabledItems.Add(hvo);

				// no good...code in MakeRoot of XmlBrowseView happens later and overrides. Control with
				// selectionType attr in Xml configuration.
				//m_sourceSentences.BrowseViewer.SelectedRowHighlighting = XmlBrowseViewBase.SelectionHighlighting.none;

				m_lblExplainText = m_lblExplainDisabled.Text;
				// We only reload the list when refresh is pressed.
				m_srcClerk.ListLoadingSuppressed = true;
				CheckForOtherOccurrences();
				SetEnabledState();
			}
			return true;
		}

		void CheckForOtherOccurrences()
		{
			var allAnalysesCandidatesOfWordform = new List<IAnalysis>
													{
														m_srcwfiWordform
													};
			foreach (var anal in m_srcwfiWordform.AnalysesOC)
			{
				allAnalysesCandidatesOfWordform.Add(anal);
				foreach (var gloss in anal.MeaningsOC)
					allAnalysesCandidatesOfWordform.Add(gloss);
			}
			var allUsedSegments = new HashSet<ISegment>();
			foreach (var segment in m_cache.ServiceLocator.GetInstance<ISegmentRepository>().AllInstances().Where(
				segment => segment.AnalysesRS.Any(allAnalysesCandidatesOfWordform.Contains)))
			{
				allUsedSegments.Add(segment);
			}
			// There are 'other' occurrences if some of the real ones aren't in the displayed list.
			if (m_repoSeg == null)
				m_repoSeg = m_cache.ServiceLocator.GetInstance<ISegmentRepository>();
			var enabledSegments = new HashSet<ISegment>();
			foreach (int hvoFake in m_enabledItems)
			{
				int hvoSeg = m_specialSda.get_ObjectProp(hvoFake, ConcDecorator.kflidSegment);
				if (hvoSeg > 0)
					enabledSegments.Add(m_repoSeg.GetObject(hvoSeg));
			}
			m_fOtherOccurrencesExist = allUsedSegments.Union(enabledSegments).Count() != allUsedSegments.Count();
		}

		void m_btnPreviewClear_Click(object sender, EventArgs e)
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
				m_respellUndoaction.SetupPreviews(RespellingSda.kflidSpellingPreview,
					previewTag,
					RespellingSda.kflidAdjustedBeginOffset,
					RespellingSda.kflidAdjustedEndOffset,
					m_sourceSentences.BrowseViewer.PreviewEnabledTag,
					m_sourceSentences.BrowseViewer.AllItems,
					m_sourceSentences.BrowseViewer.BrowseView.RootBox);
				// Create m_previewOccurrenceColumn if needed
				EnsurePreviewColumn();
				m_oldOccurrenceColumn = m_sourceSentences.BrowseViewer.ReplaceColumn("Occurrence", m_previewOccurrenceColumn);
				m_previewButtonText = m_btnPreviewClear.Text;
				m_btnPreviewClear.Text = TextAndWordsResources.ksClear;
			}
		}

		private void MakeUndoAction()
		{
			m_respellUndoaction = new RespellUndoAction(
				m_specialSda,
				m_cache,
				m_vernWs,
				m_srcwfiWordform.Form.get_String(m_vernWs).Text,
				m_cbNewSpelling.Text)
									{
										PreserveCase = m_cbMaintainCase.Checked
									};
			var tagEnabled = m_sourceSentences.BrowseViewer.PreviewEnabledTag;
			foreach (int hvo in m_sourceSentences.BrowseViewer.CheckedItems)
			{
				if (m_specialSda.get_IntProp(hvo, tagEnabled) == 1)
					m_respellUndoaction.AddOccurrence(hvo);
			}
		}

		// Create the preview column if we haven't already.
		private void EnsurePreviewColumn()
		{
			if (m_previewOccurrenceColumn != null)
				return;
			var doc = new XmlDocument();
			var fRtl = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.RightToLeftScript;
			var insert = "";
			if (fRtl)
				insert = "<righttoleft value=\"on\"/>";
			doc.LoadXml(
				  "<column label=\"Occurrence\" width=\"415000\" multipara=\"true\" doNotPersist=\"true\">"
				+     "<concpara min=\"FakeOccurrence.AdjustedBeginOffset\" lim=\"FakeOccurrence.AdjustedEndOffset\" align=\"144000\">"
				+         "<properties><editable value=\"false\"/>" + insert + "</properties>"
				+         "<string class=\"FakeOccurrence\" field=\"SpellingPreview\"/>"
				+         "<preview ws=\"vernacular\"/>"
				+     "</concpara>"
				+ "</column>");
			m_previewOccurrenceColumn = doc.DocumentElement;
		}

		private void SetSuggestions()
		{
			var dict = SpellingHelper.GetSpellChecker(m_vernWs, m_cache.LanguageWritingSystemFactoryAccessor);
			if (dict == null)
				return;
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
		void AdjustHeightAndMinSize(int newHeight, Size newMinSize)
		{
			var bottomControls = new List<Control>();
			SuspendLayout();
			foreach (Control c in Controls)
			{
				if (((int) c.Anchor & (int) AnchorStyles.Bottom) == 0)
					continue;

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

		void sentences_CheckBoxChanged(object sender, CheckBoxChangedEventArgs e)
		{
			if (m_respellUndoaction != null)
			{
				// We're displaying a Preview, and need to update things.
				if (e.HvosChanged.Length == 1)
				{
					// Single check box clicked, update with PropChanged
					var hvo = e.HvosChanged[0];
					// We only consider it 'checked' if both checked AND enabled.
					bool itemChecked = GetItemChecked(hvo);
					if (m_respellUndoaction != null)
						m_respellUndoaction.UpdatePreview(hvo, itemChecked);
				}
				else
				{
					// Update the status of items
					foreach (var hvo in e.HvosChanged)
						if (GetItemChecked(hvo))
							m_respellUndoaction.AddOccurrence(hvo);
						else
							m_respellUndoaction.RemoveOccurrence(hvo);
					// the regenerate all the previews.
					m_respellUndoaction.UpdatePreviews(true);
				}
			}
			SetEnabledState();
		}

		private bool GetItemChecked(int hvo)
		{
			return m_sourceSentences.BrowseViewer.IsItemChecked(hvo)
				   && m_specialSda.get_IntProp(hvo, m_sourceSentences.BrowseViewer.PreviewEnabledTag) == 1;
		}

		private bool WordformHasMonomorphemicAnalyses
		{
			get
			{
				return m_srcwfiWordform.IsValidObject &&
					m_srcwfiWordform.AnalysesOC.Any(analysis => analysis.MorphBundlesOS.Count == 1);
			}
		}
		private bool WordformHasMultimorphemicAnalyses
		{
			get
			{
				return m_srcwfiWordform.IsValidObject &&
					m_srcwfiWordform.AnalysesOC.Any(analysis => analysis.MorphBundlesOS.Count > 1);
			}
		}

		private void SetEnabledState()
		{
			var enabledBasic = (!string.IsNullOrEmpty(m_cbNewSpelling.Text)
				&& m_cbNewSpelling.Text != m_srcwfiWordform.Form.get_String(m_vernWs).Text);

			bool someWillChange;
			var changeAll = AllWillChange(out someWillChange);
			m_btnApply.Enabled = enabledBasic && someWillChange;
			m_btnPreviewClear.Enabled = enabledBasic && someWillChange; // todo: also if 'clear' needed.
			m_cbUpdateLexicon.Enabled = changeAll && WordformHasMonomorphemicAnalyses;
			m_rbDiscardAnalyses.Enabled = m_rbKeepAnalyses.Enabled = changeAll && WordformHasMultimorphemicAnalyses;
			m_cbCopyAnalyses.Enabled = someWillChange && !changeAll;
			if (changeAll)
				m_lblExplainDisabled.Text = "";
			else if (m_fOtherOccurrencesExist)
				m_lblExplainDisabled.Text = TextAndWordsResources.ksExplainDisabledScripture;
			else
				m_lblExplainDisabled.Text = m_lblExplainText; // original message says not all checked.
		}

	#region Event handlers

		private void m_cbUpdateLexicon_Click(object sender, EventArgs e)
		{
			PropertyTable.SetProperty("UpdateLexiconIfPossible", m_cbUpdateLexicon.Checked, true, false);
		}

		void m_rbDiscardAnalyses_Click(object sender, EventArgs e)
		{
			PropertyTable.SetProperty("RemoveAnalyses", m_rbDiscardAnalyses.Checked, true, false);
		}

		void m_cbCopyAnalyses_Click(object sender, EventArgs e)
		{
			PropertyTable.SetProperty("CopyAnalysesToNewSpelling", m_cbCopyAnalyses.Checked, true, false);
		}

		void m_cbMaintainCase_Click(object sender, EventArgs e)
		{
			PropertyTable.SetProperty("MaintainCaseOnChangeSpelling", m_cbMaintainCase.Checked, true, false);
		}

		protected override void OnClosed(EventArgs e)
		{
			// Any way we get closed we want to be sure this gets restored.
			if (m_wfClerk != null)
			{
				m_wfClerk.SuppressSaveOnChangeRecord = false;
				if (m_hvoNewWordform != 0)
				{
					// Move the clerk to the new word if possible.
					m_wfClerk.JumpToRecord(m_hvoNewWordform);
				}
			}
			base.OnClosed(e);
		}

		private void m_btnClose_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), "FLExHelpFile", s_helpTopic);
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
			if (m_sourceSentences.CheckedItems.Count <= 0)
				return;

			using (new WaitCursor(this))
			{
				// NB: occurrence may ref. wf, anal, or gloss.
				// NB: Need to support selective spelling change.
				//	(gunaa->gwnaa when it means woman, but not for gu-naa, IMP+naa)
				if (m_respellUndoaction == null)
					MakeUndoAction();
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


				// Reloads the conc. virtual prop for the old wordform,
				// so the old values are removed.
				// This will allow .CanDelete' to return true.
				// Otherwise, it won't be deletable,
				// as it will still have those occurrences pointing at it.
				//FirePropChanged(m_srcwfiWordform.Hvo);
				//UpdateDisplay();
				m_respellUndoaction.RemoveChangedItems(m_enabledItems, m_sourceSentences.BrowseViewer.PreviewEnabledTag);
				// If everything changed remember the new wordform.
				if (m_respellUndoaction.AllChanged)
					m_hvoNewWordform = m_respellUndoaction.NewWordform;
				if (m_previewOn)
					EnsurePreviewOff(); // will reconstruct
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
		/// <returns></returns>
		private bool AllWillChange(out bool someWillChange)
		{
			var checkedItems = new Set<int>(m_sourceSentences.CheckedItems);
			var changeCount = checkedItems.Intersection(m_enabledItems).Count;
			someWillChange = changeCount > 0;
			return changeCount == m_enabledItems.Count && !m_fOtherOccurrencesExist;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show more options.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void btnMore_Click(object sender, System.EventArgs e)
		{
			m_btnMore.Text = TextAndWordsResources.ksLess;
			m_btnMore.Image = ResourceHelper.LessButtonDoubleArrowIcon;
			m_btnMore.Click -= btnMore_Click;
			m_btnMore.Click += btnLess_Click;
			AdjustHeightAndMinSize(Height + m_optionsPanel.Height, m_moreMinSize);
			m_optionsPanel.Visible = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show fewer options.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void btnLess_Click(object sender, System.EventArgs e)
		{
			m_btnMore.Text = m_sMoreButtonText;
			m_btnMore.Image = ResourceHelper.MoreButtonDoubleArrowIcon;
			m_btnMore.Click += btnMore_Click;
			m_btnMore.Click -= btnLess_Click;
			AdjustHeightAndMinSize(Height - m_optionsPanel.Height, m_lessMinSize);
			m_optionsPanel.Visible = false;
		}

		///-------------------------------------------------------------------------------
		/// <summary>
		/// Draws an etched line on the dialog to separate the Search Options from the
		/// basic controls.
		/// </summary>
		///-------------------------------------------------------------------------------
		void m_optionsPanel_Paint(object sender, PaintEventArgs e)
		{
			var dxMargin = 10;
			var left = m_optionsLabel.Right;
			LineDrawing.Draw(e.Graphics, left,
				(m_optionsLabel.Top + m_optionsLabel.Bottom) / 2,
				m_optionsPanel.Right - left - dxMargin, LineTypes.Etched);
		}

		#endregion Event handlers

		private void m_refreshButton_Click(object sender, EventArgs e)
		{
			// Make sure preview is off.
			EnsurePreviewOff();
			// Check all items still enabled
			foreach (int hvoFake in m_enabledItems)
				m_specialSda.SetInt(hvoFake, XMLViewsDataCache.ktagItemSelected, 1);
			// Reconstruct the main list of objects we are showing.
			m_srcClerk.ListLoadingSuppressed = false;
			m_srcClerk.OnRefresh(null);
			m_srcClerk.ListLoadingSuppressed = true;
		}

		private void EnsurePreviewOff()
		{
			if (m_previewOn)
				m_btnPreviewClear_Click(this, new EventArgs());
		}

		private void m_cbMaintainCase_CheckedChanged(object sender, EventArgs e)
		{
			if (m_respellUndoaction == null)
				return;

			m_respellUndoaction.PreserveCase = m_cbMaintainCase.Checked;
			m_respellUndoaction.UpdatePreviews(false);
			m_sourceSentences.BrowseViewer.ReconstructView();
		}
	}

	/// <summary>
	/// a data record class for an occurrence of a spelling change
	/// </summary>
	internal class RespellOccurrence
	{
#pragma warning disable 0414
		int m_hvoBa; // Hvo of CmBaseAnnotation representing occurrence
		// position in the UNMODIFIED input string. (Note that it may not start at this position
		// in the output string, since there may be prior occurrences.) Initially equal to
		// m_hvoBa.BeginOffset; but that may get changed if we are undone.
		int m_ich;
#pragma warning restore 0414

		public RespellOccurrence(int hvoBa, int ich)
		{
			m_hvoBa = hvoBa;
			m_ich = ich;
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Information about how a paragraph is being changed. Also handles actually making the change.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class ParaChangeInfo
	{
		readonly RespellUndoAction m_action;
		readonly int m_hvoTarget; // the one being changed.
		ITsString m_newContents; // series of changes made to chwhat it will become if the change goes ahead.
		ITsString m_oldContents; // what it was to start with (and will be again if Undone)
		readonly List<int> m_changes = new List<int>(); // hvos of occurrences being changed.
		readonly int m_flid; // property being changed (typically paragraph contents, but occasionally picture caption)
		readonly int m_ws; // if m_flid is multilingual, ws of alternative; otherwise, zero.

		public ParaChangeInfo(RespellUndoAction action, int hvoTarget, int flid, int ws)
		{
			m_action = action;
			m_hvoTarget = hvoTarget;
			m_flid = flid;
			m_ws = ws;
		}

		/// <summary>
		/// para contents if change proceeds.
		/// </summary>
		public ITsString NewContents
		{
			get { return m_newContents; }
			set { m_newContents = value; }
		}

		/// <summary>
		/// para contents if change does not proceed (or is undone).
		/// </summary>
		public ITsString OldContents
		{
			get { return m_oldContents; }
			set { m_oldContents = value; }
		}

		/// <summary>
		/// Get the list of changes that are to be made to this paragraph, that is, CmBaseAnnotations
		/// which point at it and have been selected to be changed.
		/// </summary>
		public List<int> Changes
		{
			get { return m_changes; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Figure what the new contents needs to be. (Also sets OldContents.) Maybe even set
		/// the contents.
		/// </summary>
		/// <param name="fMakeChangeNow">if set to <c>true</c> make the actual change now;
		/// otherwise, just figure out what the new contents will be.</param>
		/// <param name="progress"></param>
		/// ------------------------------------------------------------------------------------
		public void MakeNewContents(bool fMakeChangeNow, ProgressDialogWorkingOn progress)
		{
			RespellingSda sda = m_action.RespellSda;
			m_oldContents = RespellUndoAction.AnnotationTargetString(m_hvoTarget, m_flid, m_ws, sda);
			ITsStrBldr bldr = m_oldContents.GetBldr();
			m_changes.Sort((left, right) => sda.get_IntProp(left, ConcDecorator.kflidBeginOffset).CompareTo(
				sda.get_IntProp(right, ConcDecorator.kflidBeginOffset)));

			for (int i = m_changes.Count - 1; i >= 0; i--)
			{
				int ichMin = sda.get_IntProp(m_changes[i], ConcDecorator.kflidBeginOffset);
				int ichLim = sda.get_IntProp(m_changes[i], ConcDecorator.kflidEndOffset);
				string replacement = Replacement(m_action.OldOccurrence(m_changes[i]));
				bldr.Replace(ichMin, ichLim, replacement, null);
				if (fMakeChangeNow)
				{
					ITsString tssNew = bldr.GetString();
					if (!m_oldContents.Equals(tssNew))
					{
						if (m_ws == 0)
							sda.SetString(m_hvoTarget, m_flid, tssNew);
						else
							sda.SetMultiStringAlt(m_hvoTarget, m_flid, m_ws, tssNew);
					}
				}
			}
			RespellUndoAction.UpdateProgress(progress);
			m_newContents = bldr.GetString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update all the changed wordforms to point at the new Wf.
		/// Must be called before we change the text of the paragraph, as it
		/// depends on offsets into the old string.
		/// This is usually redundant, since updating the text of the paragraph automatically updates the
		/// segment analysis. However, this can be used to force an upper case occurrence to be analyzed
		/// as the lower-case wordform (FWR-3134). The paragraph adjustment does not force this back
		/// (at least not immediately).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateInstanceOf(ProgressDialogWorkingOn progress)
		{
			Debug.Fail(
				@"use of this method was causing very unpleasant data corruption in texts, the bug it fixed needs addressing though.");
			var analysesToChange = new List<Tuple<ISegment, int>>();
			RespellingSda sda = m_action.RespellSda;
			foreach (var hvoFake in m_changes)
			{
				int hvoSeg = sda.get_ObjectProp(hvoFake, ConcDecorator.kflidSegment);
				int beginOffset = sda.get_IntProp(hvoFake, ConcDecorator.kflidBeginOffset);
				if (hvoSeg > 0)
				{
					ISegment seg = m_action.RepoSeg.GetObject(hvoSeg);
					int canal = seg.AnalysesRS.Count;
					for (int i = 0; i < canal; ++i)
					{
						IAnalysis anal = seg.AnalysesRS[i];
						if (anal.HasWordform && anal.Wordform.Hvo == m_action.OldWordform)
						{
							if (seg.GetAnalysisBeginOffset(i) == beginOffset)
							{
								// Remember that we want to change it, but don't do it yet,
								// because there may be other occurrences in this paragraph,
								// and changing the analysis to something which may have a different
								// length could mess things up.
								analysesToChange.Add(new Tuple<ISegment, int>(seg, i));
							}
						}
					}
				}
			}
			if (analysesToChange.Count > 0)
			{
				var newVal = new[] { m_action.RepoWf.GetObject(m_action.NewWordform) };
				foreach (var change in analysesToChange)
					change.Item1.AnalysesRS.Replace(change.Item2, 1, newVal);
			}
			RespellUndoAction.UpdateProgress(progress);
		}

		private string Replacement(ITsString oldTss)
		{
			var replacement = m_action.NewSpelling;
			if (m_action.PreserveCase)
			{
				int var;
				int ws = oldTss.get_Properties(0).GetIntPropValues((int)FwTextPropType.ktptWs, out var);
				CaseFunctions cf = m_action.GetCaseFunctionFor(ws);
				if (cf.StringCase(oldTss.Text) == StringCaseStatus.title)
					replacement = cf.ToTitle(replacement);
			}
			return replacement;
		}
	}

	/// <summary>
	/// This class handles some of the functionality that is common to doing the action and previewing it.
	/// </summary>
	internal class RespellUndoAction : IUndoAction
	{
		// The spelling change
		private readonly string m_oldSpelling;
		private readonly string m_newSpelling;
		readonly Set<int> m_changes = new Set<int>(); // CBAs that represent occurrences we will change.
		/// <summary>
		/// Key is hvo of StTxtPara, value is list (eventually sorted by BeginOffset) of
		/// CBAs that refer to it AND ARE BEING CHANGED.
		/// </summary>
		readonly Dictionary<int, ParaChangeInfo> m_changedParas = new Dictionary<int, ParaChangeInfo>();
		readonly XMLViewsDataCache m_specialSda;
		readonly FdoCache m_cache;
		IEnumerable<int> m_occurrences; // items requiring preview.
		bool m_fPreserveCase;
		bool m_fUpdateLexicalEntries;
		bool m_fAllChanged; // set true if all occurrences changed.
		bool m_fKeepAnalyses; // set true to keep analyses of old wordform, even if all changed.
		bool m_fCopyAnalyses;

		private ISegmentRepository m_repoSeg;
		private IStTxtParaRepository m_repoPara;
		private IWfiWordformRepository m_repoWf;
		private IWfiWordformFactory m_factWf;
		private IWfiAnalysisFactory m_factWfiAnal;
		private IWfiGlossFactory m_factWfiGloss;
		private IWfiMorphBundleFactory m_factWfiMB;

		int m_tagPrecedingContext;
		int m_tagPreview;
		int m_tagAdjustedBegin;
		int m_tagAdjustedEnd;
		int m_tagEnabled;
		// Case functions per writing system
		private readonly Dictionary<int, CaseFunctions> m_caseFunctions = new Dictionary<int, CaseFunctions>();
		readonly int m_vernWs; // The WS we want to use throughout.

		/// <summary>
		/// HVO of wordform created (or found or made real) during DoIt for new spelling.
		/// </summary>
		internal int NewWordform { get; private set; }
		/// <summary>
		/// HVO of original wordform (possibly made real during DoIt) for old spelling.
		/// </summary>
		internal int OldWordform { get; private set; }

		// These preserve the old spelling-dictionary status for Undo.
		bool m_fWasOldSpellingCorrect = false;
		bool m_fWasNewSpellingCorrect = false;

		// Info to support efficient Undo/Redo for large lists of changes.
		//readonly List<int> m_hvosToChangeIntProps = new List<int>(); // objects with integer props needing change
		//readonly List<int> m_tagsToChangeIntProps = new List<int>(); // tags of the properties
		//readonly List<int> m_oldValues = new List<int>(); // initial values (target value for Undo)
		//readonly List<int> m_newValues = new List<int>(); // alternate values (target value for Redo).

		private int[] m_oldOccurrencesOldWf; // occurrences of original wordform before change
		private int[] m_oldOccurrencesNewWf; // occurrences of new spelling wordform before change
		private int[] m_newOccurrencesOldWf; // occurrences of original wordform after change
		private int[] m_newOccurrencesNewWf; // occurrences of new spelling after change.

		private IVwRootBox m_rootb;

		/// <summary>
		/// Used in tests only at present, assumes default vernacular WS.
		/// </summary>
		internal RespellUndoAction(XMLViewsDataCache sda, FdoCache cache, string oldSpelling, string newSpelling)
			:this(sda, cache, cache.DefaultVernWs, oldSpelling, newSpelling)
		{
		}

		#region Properties
		internal string NewSpelling
		{
			get { return m_newSpelling; }
		}

		internal int[] OldOccurrencesOfOldWordform
		{
			get { return m_oldOccurrencesOldWf; }
		}

		internal RespellingSda RespellSda
		{
			get
			{
				return ((DomainDataByFlidDecoratorBase)m_specialSda.BaseSda).BaseSda as RespellingSda;
			}
		}

		internal ISegmentRepository RepoSeg
		{
			get
			{
				if (m_repoSeg == null)
					m_repoSeg = m_cache.ServiceLocator.GetInstance<ISegmentRepository>();
				return m_repoSeg;
			}
		}

		internal IStTxtParaRepository RepoPara
		{
			get
			{
				if (m_repoPara == null)
					m_repoPara = m_cache.ServiceLocator.GetInstance<IStTxtParaRepository>();
				return m_repoPara;
			}
		}

		internal IWfiWordformRepository RepoWf
		{
			get
			{
				if (m_repoWf == null)
					m_repoWf = m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>();
				return m_repoWf;
			}
		}

		internal IWfiWordformFactory FactWf
		{
			get
			{
				if (m_factWf == null)
					m_factWf = m_cache.ServiceLocator.GetInstance<IWfiWordformFactory>();
				return m_factWf;
			}
		}

		internal IWfiAnalysisFactory FactWfiAnal
		{
			get
			{
				if (m_factWfiAnal == null)
					m_factWfiAnal = m_cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>();
				return m_factWfiAnal;
			}
		}

		internal IWfiGlossFactory FactWfiGloss
		{
			get
			{
				if (m_factWfiGloss == null)
					m_factWfiGloss = m_cache.ServiceLocator.GetInstance<IWfiGlossFactory>();
				return m_factWfiGloss;
			}
		}

		internal IWfiMorphBundleFactory FactWfiMB
		{
			get
			{
				if (m_factWfiMB == null)
					m_factWfiMB = m_cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>();
				return m_factWfiMB;
			}
		}
		#endregion

		/// <summary>
		/// Normal constructor
		/// </summary>
		internal RespellUndoAction(XMLViewsDataCache sda, FdoCache cache, int vernWs,
			string oldSpelling, string newSpelling)
		{
			m_specialSda = sda;
			m_cache = cache;
			m_vernWs = vernWs;
			m_oldSpelling = oldSpelling;
			m_newSpelling = newSpelling;
		}

		internal bool PreserveCase
		{
			get { return m_fPreserveCase; }
			set { m_fPreserveCase = value; }
		}

		/// <summary>
		/// Note one occurrence that we should change.
		/// </summary>
		internal void AddOccurrence(int hvoCba)
		{
			m_changes.Add(hvoCba);
		}
		internal void RemoveOccurrence(int hvoCba)
		{
			m_changes.Remove(hvoCba);
		}

		/// <summary>
		/// These three properties determine what we will do to a run which is corrected,
		/// but is not the primary spelling change in focus
		/// </summary>
		static internal int SecondaryTextProp
		{
			get { return (int)FwTextPropType.ktptForeColor; }
		}

		static internal int SecondaryTextVar
		{
			get { return (int)FwTextPropVar.ktpvDefault; }
		}

		static internal int SecondaryTextVal
		{
			get { return (int)ColorUtil.ConvertColorToBGR(Color.Gray); } //(int)FwTextToggleVal.kttvForceOn; }
		}

		/// <summary>
		/// Set up the appropriate preceding and following context for the given occurrence.
		/// </summary>
		internal void SetupPreviews(int tagPrecedingContext, int tagPreview,
			int tagAdjustedBegin, int tagAdjustedEnd, int tagEnabled, IEnumerable<int> occurrences,
			IVwRootBox rootb)
		{
			m_tagPrecedingContext = tagPrecedingContext;
			m_tagPreview = tagPreview;
			m_tagAdjustedBegin = tagAdjustedBegin;
			m_tagAdjustedEnd = tagAdjustedEnd;
			m_tagEnabled = tagEnabled;
			m_occurrences = occurrences;
			UpdatePreviews(false);
			m_rootb = rootb;
		}

		/// <summary>
		/// Update all previews for the previously supplied list of occurrences.
		/// </summary>
		internal void UpdatePreviews(bool fPropChange)
		{
			// Build the dictionary that indicates what will change in each paragraph
			m_changedParas.Clear();
			BuildChangedParasInfo();
			ComputeParaChanges(false, null);
			UpdatePreviews(m_occurrences);
			if (fPropChange && RootBox != null)
			{
				foreach (var hvo in m_occurrences)
				{
					// This is enough PropChanged to redraw the whole containing paragraph
					RootBox.PropChanged(hvo, m_tagPreview, 0, 0, 0);
				}
			}
		}

		/// <summary>
		/// Return the original text indicated by the cba
		/// </summary>
		/// <param name="hvoCba"></param>
		/// <returns></returns>
		internal ITsString OldOccurrence(int hvoCba)
		{
			return OldOccurrence(hvoCba, 0);
		}

		/// <summary>
		/// Return the original text indicated by the cba
		/// </summary>
		/// <param name="hvoFake"></param>
		/// <param name="delta">Normally zero, in one case BeginOffset has already been adjusted by
		/// adding delta, need to subtract it here.</param>
		/// <returns></returns>
		internal ITsString OldOccurrence(int hvoFake, int delta)
		{
			int hvoTarget = GetTargetObject(hvoFake);
			int flid = FlidOfTarget(hvoTarget);
			int ws = 0;
			if (flid == CmPictureTags.kflidCaption)
				ws = m_cache.DefaultVernWs;
			ITsString tssValue;
			tssValue = AnnotationTargetString(hvoTarget, flid, ws, RespellSda);
			ITsStrBldr bldr = tssValue.GetBldr();
			int ichBegin = BeginOffset(hvoFake) - delta;
			int ichLim = EndOffset(hvoFake);
			if (ichLim < bldr.Length)
				bldr.Replace(ichLim, bldr.Length, "", null);
			if (ichBegin > 0)
				bldr.Replace(0, ichBegin, "", null);
			return bldr.GetString();
		}

		// Enhance JohnT: could we get the FDO object and just ask whether it is StTxtPara/CmPicture?
		// This approach is brittle if we add subclasses of either.
		private int FlidOfTarget(int hvoTarget)
		{
			int clid = m_specialSda.get_IntProp(hvoTarget, CmObjectTags.kflidClass);
			switch (clid)
			{
				case ScrTxtParaTags.kClassId:
				case StTxtParaTags.kClassId:
					return StTxtParaTags.kflidContents;
				case CmPictureTags.kClassId:
					return CmPictureTags.kflidCaption;
				default:
					return 0;
			}
		}

		internal static ITsString AnnotationTargetString(int hvoTarget, int flid, int ws, RespellingSda sda)
		{
			ITsString tssValue;
			if (IsMultilingual(flid))
				tssValue = sda.get_MultiStringAlt(hvoTarget, flid, ws);
			else
				tssValue = sda.get_StringProp(hvoTarget, flid);
			return tssValue;
		}

		/// <summary>
		/// Update previews for the listed occurrences.
		/// </summary>
		private void UpdatePreviews(IEnumerable<int> occurrences)
		{
			foreach (var hvoFake in occurrences)
			{
				var hvoPara = GetTargetObject(hvoFake);
				ParaChangeInfo info;
				if (m_changedParas.TryGetValue(hvoPara, out info))
				{
					// We have to build a modified string, and we might find hvoCba in the list.
					// We also have to figure out how much our offset changed, if the new spelling differs in length.
					var bldr = info.NewContents.GetBldr();
					var delta = 0; // amount to add to offsets of later words.
					var beginTarget = BeginOffset(hvoFake);
					var ichange = 0;
					var fGotOffsets = false;
					for(; ichange < info.Changes.Count; ichange++)
					{
						var hvoChange = info.Changes[ichange];
						var beginChange = BeginOffset(hvoChange);
						if (hvoChange == hvoFake)
						{
							// stop preceding context just before the current one.
							var ich = BeginOffset(hvoFake) + delta;

							bldr.ReplaceTsString(ich, bldr.Length, OldOccurrence(hvoFake));
							m_specialSda.SetInt(hvoFake, m_tagAdjustedBegin, BeginOffset(hvoFake) + delta);
							m_specialSda.SetInt(hvoFake, m_tagAdjustedEnd, EndOffset(hvoFake) + delta);
							break;
						}
						else if (beginChange > beginTarget && !fGotOffsets)
						{
							// This and future changes are after this occurrence, so the current delta is the one
							// we want (and this is an occurrence we are not changing, or we would have found it).
							SetOffsets(m_tagAdjustedBegin, m_tagAdjustedEnd, hvoFake, delta, beginTarget);
							fGotOffsets = true;
							// don't stop the loop, we want everything in the preceding context string, with later occurrences marked.
							// enhance JohnT: preceding context is the same for every unchanged occurrence in the paragraph,
							// if it is common to change some but not all in the same paragraph we could save it.
						}
						// It's another changed occurrence, not the primary one, highlight it.
						bldr.SetIntPropValues(beginChange + delta, beginChange + delta + NewSpelling.Length,
							SecondaryTextProp, SecondaryTextVar, SecondaryTextVal);
						delta += NewSpelling.Length - m_oldSpelling.Length;
					}
					m_specialSda.SetString(hvoFake, m_tagPrecedingContext, bldr.GetString());
					if (ichange < info.Changes.Count)
					{
						// need to set up following context also
						bldr = info.NewContents.GetBldr();
						bldr.Replace(0, beginTarget + delta, "", null); // remove everything before occurrence.
						// Make the primary occurrence bold
						bldr.SetIntPropValues(0, NewSpelling.Length, (int)FwTextPropType.ktptBold,
							(int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
						delta = -beginTarget + NewSpelling.Length - m_oldSpelling.Length;
						ichange++;
						for (; ichange < info.Changes.Count; ichange++)
						{
							int hvoChange = info.Changes[ichange];
							int beginChange = BeginOffset(hvoChange);
							// It's another changed occurrence, not the primary one, highlight it.
							bldr.SetIntPropValues(beginChange + delta, beginChange + delta + NewSpelling.Length,
								SecondaryTextProp, SecondaryTextVar, SecondaryTextVal);
							delta += NewSpelling.Length - m_oldSpelling.Length;
						}
						m_specialSda.SetString(hvoFake, m_tagPreview, bldr.GetString());
					}
					else if (!fGotOffsets)
					{
						// an unchanged occurrence after all the changed ones
						SetOffsets(m_tagAdjustedBegin, m_tagAdjustedEnd, hvoFake, delta, beginTarget);
					}
				}
				else
				{
					// Unchanged paragraph, copy the key info over.
					ITsString tssVal;
					int flid = FlidOfTarget(hvoPara);
					if (IsMultilingual(flid))
					{
						int ws = m_cache.DefaultVernWs;
						tssVal = m_specialSda.get_MultiStringAlt(hvoPara, flid, ws);
					}
					else
					{
						tssVal = m_specialSda.get_StringProp(hvoPara, flid);
					}
					m_specialSda.SetString(hvoFake, m_tagPrecedingContext, tssVal);
					m_specialSda.SetInt(hvoFake, m_tagAdjustedBegin, BeginOffset(hvoFake));
					m_specialSda.SetInt(hvoFake, m_tagAdjustedEnd, EndOffset(hvoFake));
				}
			}
		}

		private void SetOffsets(int tagAdjustedBegin, int tagAdjustedEnd, int hvoFake, int delta, int beginTarget)
		{
			m_specialSda.SetInt(hvoFake, tagAdjustedBegin, beginTarget + delta);
			m_specialSda.SetInt(hvoFake, tagAdjustedEnd, beginTarget + m_oldSpelling.Length + delta);
		}

		internal int BeginOffset(int hvoFake)
		{
			return m_specialSda.get_IntProp(hvoFake, ConcDecorator.kflidBeginOffset);
		}

		private int EndOffset(int hvoFake)
		{
			return m_specialSda.get_IntProp(hvoFake, ConcDecorator.kflidEndOffset);
		}

		/// <summary>
		/// Set up the dictionary which tracks changes for each paragraph.
		/// </summary>
		private void BuildChangedParasInfo()
		{
			foreach (var hvoCba in m_changes)
			{
				ParaChangeInfo info = EnsureParaInfo(hvoCba, 0);
				if (!info.Changes.Contains(hvoCba))
					info.Changes.Add(hvoCba);
			}
		}

		/// <summary>
		/// Determine the contents for each of the paragraphs to be changed, applying the
		/// changes immediately if so requested.
		/// </summary>
		private void ComputeParaChanges(bool fMakeChangeNow, ProgressDialogWorkingOn progress)
		{
			// Build the new strings for each
			foreach (var info1 in m_changedParas.Values)
				info1.MakeNewContents(fMakeChangeNow, progress);
		}

		private ParaChangeInfo EnsureParaInfo(int hvoFake, int hvoTargetPara)
		{
			var hvoPara = GetTargetObject(hvoFake);
			if (hvoTargetPara != 0 && hvoPara != hvoTargetPara)
				return null;
			ParaChangeInfo info;
			if (!m_changedParas.TryGetValue(hvoPara, out info))
			{
				int flid = FlidOfTarget(hvoPara);
				int ws = 0;
				if (flid == CmPictureTags.kflidCaption)
					ws = m_cache.DefaultVernWs;
				Debug.Assert(flid != 0);
				info = new ParaChangeInfo(this, hvoPara, flid, ws);
				m_changedParas[hvoPara] = info;
			}
			return info;
		}

		// For now it's good enough to treat anything but StTxtPara.Contents as multilingual.
		internal static bool IsMultilingual(int flid)
		{
			return flid != StTxtParaTags.kflidContents;
		}

		private int GetTargetObject(int hvoCba)
		{
			return m_specialSda.get_ObjectProp(hvoCba, ConcDecorator.kflidTextObject);
		}

		IVwRootBox RootBox
		{
			get { return m_rootb; }
		}

		/// <summary>
		/// Update the preview when the check status of a single HVO changes.
		/// </summary>
		internal void UpdatePreview(int hvoChanged, bool isChecked)
		{
			if (m_changes.Contains(hvoChanged) == isChecked)
				return;
			if (isChecked)
				m_changes.Add(hvoChanged);
			else
				m_changes.Remove(hvoChanged);
			var hvoPara = GetTargetObject(hvoChanged);
			IEnumerable<int> occurrencesAffected = m_occurrences.Where(hvo => GetTargetObject(hvo) == hvoPara);
			var info = EnsureParaInfo(hvoChanged, hvoPara);
			if (isChecked)
				info.Changes.Add(hvoChanged);
			else
				info.Changes.Remove(hvoChanged);
			info.MakeNewContents(false, null);
			UpdatePreviews(occurrencesAffected);
			if (RootBox != null)
			{
				foreach (var hvo in occurrencesAffected)
				{
					// This is enough PropChanged to redraw the whole containing paragraph
					RootBox.PropChanged(hvo, m_tagEnabled, 0, 0, 0);
				}
			}
		}

		/// <summary>
		/// Actually make the change indicated in the action (execute the original command,
		/// from the Apply button in the dialog).
		/// </summary>
		public void DoIt(IPublisher publisher)
		{
			if (m_changes.Count == 0)
				return;

			BuildChangedParasInfo();

			if (m_changedParas.Count < 10)
			{
				CoreDoIt(null, publisher);
			}
			else
			{
				using (var dlg = new ProgressDialogWorkingOn())
				{
					dlg.Owner = Form.ActiveForm;
					dlg.Icon = dlg.Owner.Icon;
					dlg.Minimum = 0;
					// 2x accounts for two main loops; extra 10 very roughly accounts for final cleanup.
					dlg.Maximum = m_changedParas.Count * 2 + 10;
					dlg.Text = TextAndWordsResources.ksChangingSpelling;
					dlg.WorkingOnText = TextAndWordsResources.ksChangingSpelling;
					dlg.ProgressLabel = TextAndWordsResources.ksProgress;
					dlg.Show();
					dlg.BringToFront();
					CoreDoIt(dlg, publisher);
					dlg.Close();
				}
			}
		}

		/// <summary>
		/// Core of the DoIt method, may be called with or without progress dialog.
		/// </summary>
		private void CoreDoIt(ProgressDialogWorkingOn progress, IPublisher publisher)
		{
			var specialMdc = m_specialSda.MetaDataCache;
			int flidOccurrences = specialMdc.GetFieldId2(WfiWordformTags.kClassId, "Occurrences", false);

			using (UndoableUnitOfWorkHelper uuow = new UndoableUnitOfWorkHelper(m_cache.ActionHandlerAccessor,
								String.Format(TextAndWordsResources.ksUndoChangeSpelling, m_oldSpelling, NewSpelling),
				String.Format(TextAndWordsResources.ksRedoChangeSpelling, m_oldSpelling, NewSpelling)))
			{
				IWfiWordform wfOld = FindOrCreateWordform(m_oldSpelling, m_vernWs);
				var originalOccurencesInTexts = wfOld.OccurrencesInTexts.ToList(); // At all levels.
				IWfiWordform wfNew = FindOrCreateWordform(NewSpelling, m_vernWs);
				SetOldOccurrencesOfWordforms(flidOccurrences, wfOld, wfNew);
				UpdateProgress(progress);

				// It's important to do this BEFORE we update the changed paragraphs. As we update the analysis to point
				// at the new wordform and update the text, it may happen that AnalysisAdjuster sees the only occurrence
				// of the (new) wordform go away, if the text is being changed to an other-case form. If we haven't set
				// the spelling status first, the wordform may get deleted before we ever record its spelling status.
				// This way, having a known spelling status will prevent the deletion.
				SetSpellingStatus(wfNew);

				ComputeParaChanges(true, progress);

				if (progress != null)
					progress.WorkingOnText = TextAndWordsResources.ksDealingAnalyses;
				UpdateProgress(progress);

				// Compute new occurrence lists, save and cache
				SetNewOccurrencesOfWordforms(progress);
				UpdateProgress(progress);

				// Deal with analyses.
				if (wfOld.IsValidObject && CopyAnalyses)
				{
					// Note: "originalOccurencesInTexts" may have fewer segments, after the call, as they can be removed.
					CopyAnalysesToNewWordform(originalOccurencesInTexts, wfOld, wfNew);
				}
				UpdateProgress(progress);
				if (AllChanged)
				{
					SpellingHelper.SetSpellingStatus(m_oldSpelling, m_vernWs,
						m_cache.LanguageWritingSystemFactoryAccessor, false);
					if (wfOld.IsValidObject)
					{
						ProcessAnalysesAndLexEntries(progress, wfOld, wfNew);
					}
					UpdateProgress(progress);
				}

				// Only mess with shifting if it was only a case diff in wf, but no changes were made in paragraphs.
				// Regular spelling changes will trigger re-tokenization of para, otherwise
				if (PreserveCase)
				{
					// Move pointers in segments to new WF, if the segment references the original WF.
					foreach (var segment in originalOccurencesInTexts)
					{
						if (!m_changedParas.ContainsKey(segment.Owner.Hvo))
							continue; // Skip shifting it for items that were not checked

						var wfIdx = segment.AnalysesRS.IndexOf(wfOld);
						while (wfIdx > -1)
						{
							segment.AnalysesRS.RemoveAt(wfIdx);
							segment.AnalysesRS.Insert(wfIdx, wfNew);
							wfIdx = segment.AnalysesRS.IndexOf(wfOld);
						}
					}
				}

				// The timing of this is rather crucial. During the work above, we may (if this is invoked from a
				// wordform concordance) detect that the current occurrence is no longer valid (since we change the spelling
				// and that wordform no longer occurs in that position). This leads to reloading the list and broadcasting
				// a RecordNavigation message, as we switch the selection to the first item. However, before we reload the
				// list, we need to process ItemDataModified, because that figures out what the new list items are. If
				// it doesn't happen before we reload the list, we will put a bunch of invalid occurrences back into it.
				// Things to downhill from there as we try to select an invalid one.
				// OTOH, we can't figure out the new item data for the wordforms until the work above updates the occurrences!
				// The right solution is to wait until we have updated the instances, then send ItemDataModified
				// to update the ConcDecorator state, then close the UOW which triggers other PropChanged effects.
				// We have to use SendMessage so the ConcDecorator gets that updated before the Clerk using it
				// tries to re-read the list.
				if (wfOld.CanDelete)
				{
					wfOld.Delete();
				}
				else
				{
					publisher.Publish("ItemDataModified", wfOld);
				}
				publisher.Publish("ItemDataModified", wfNew);

				uuow.RollBack = false;
			}
		}

		private IWfiWordform FindOrCreateWordform(string sForm, int wsForm)
		{
			return RepoWf.GetMatchingWordform(wsForm, sForm) ??
				FactWf.Create(m_cache.TsStrFactory.MakeString(sForm, wsForm));
		}

		private void SetOldOccurrencesOfWordforms(int flidOccurrences, IWfiWordform wfOld, IWfiWordform wfNew)
		{
			OldWordform = wfOld.Hvo;
			m_oldOccurrencesOldWf = m_specialSda.VecProp(wfOld.Hvo, flidOccurrences);
			m_fWasOldSpellingCorrect = wfOld.SpellingStatus == (int)SpellingStatusStates.correct;

			NewWordform = wfNew.Hvo;
			m_oldOccurrencesNewWf = m_specialSda.VecProp(wfNew.Hvo, flidOccurrences);
			m_fWasNewSpellingCorrect = wfNew.SpellingStatus == (int)SpellingStatusStates.correct;
		}

		private void SetNewOccurrencesOfWordforms(ProgressDialogWorkingOn progress)
		{
			Set<int> changes = new Set<int>();
			foreach (ParaChangeInfo info in m_changedParas.Values)
			{
				changes.AddRange(info.Changes);
			}
			if (AllChanged)
			{
				m_newOccurrencesOldWf = new int[0]; // no remaining occurrences
			}
			else
			{
				// Only some changed, need to figure m_newOccurrences
				List<int> newOccurrencesOldWf = new List<int>();
				foreach (int hvo in OldOccurrencesOfOldWordform)
				{
					//The offsets of our occurrences have almost certainly changed.
					//Update them so that the respelling dialog view will appear correct.
					var occur = RespellSda.OccurrenceFromHvo(hvo) as LocatedAnalysisOccurrence;
					if (occur != null)
					{
						occur.ResetSegmentOffsets();
					}

					if (!changes.Contains(hvo))
					{
						newOccurrencesOldWf.Add(hvo);
					}
				}
				m_newOccurrencesOldWf = newOccurrencesOldWf.ToArray();
			}
			UpdateProgress(progress);
			List<int> newOccurrences = new List<int>(m_oldOccurrencesNewWf.Length + changes.Count);
			newOccurrences.AddRange(m_oldOccurrencesNewWf);
			newOccurrences.AddRange(changes);
			m_newOccurrencesNewWf = newOccurrences.ToArray();
			RespellSda.ReplaceOccurrences(OldWordform, m_newOccurrencesOldWf);
			RespellSda.ReplaceOccurrences(NewWordform, m_newOccurrencesNewWf);
			SendCountVirtualPropChanged(NewWordform);
			SendCountVirtualPropChanged(OldWordform);
		}

		private void SetSpellingStatus(IWfiWordform wfNew)
		{
			wfNew.SpellingStatus = (int)SpellingStatusStates.correct;
			SpellingHelper.SetSpellingStatus(NewSpelling, m_vernWs,
				m_cache.LanguageWritingSystemFactoryAccessor, true);
		}

		private void CopyAnalysesToNewWordform(ICollection<ISegment> originalOccurencesInTexts, IWfiWordform wfOld, IWfiWordform wfNew)
		{
			var shiftedSegments = new List<ISegment>(originalOccurencesInTexts.Count);
			foreach (IWfiAnalysis oldAnalysis in wfOld.AnalysesOC)
			{
				// Only copy approved analyses.
				if (oldAnalysis.GetAgentOpinion(m_cache.LangProject.DefaultUserAgent) != Opinions.approves)
					continue;

				IWfiAnalysis newAnalysis = FactWfiAnal.Create();
				wfNew.AnalysesOC.Add(newAnalysis);
				foreach (var segment in originalOccurencesInTexts)
				{
					if (!m_changedParas.ContainsKey(segment.Owner.Hvo))
						continue; // Skip shifting it for items that were not checked

					var analysisIdx = segment.AnalysesRS.IndexOf(oldAnalysis);
					while (analysisIdx > -1)
					{
						shiftedSegments.Add(segment);
						segment.AnalysesRS.RemoveAt(analysisIdx);
						segment.AnalysesRS.Insert(analysisIdx, newAnalysis);
						analysisIdx = segment.AnalysesRS.IndexOf(oldAnalysis);
					}
				}
				foreach (var shiftedSegment in shiftedSegments)
				{
					originalOccurencesInTexts.Remove(shiftedSegment);
				}
				shiftedSegments.Clear();
				foreach (IWfiGloss oldGloss in oldAnalysis.MeaningsOC)
				{
					IWfiGloss newGloss = FactWfiGloss.Create();
					newAnalysis.MeaningsOC.Add(newGloss);
					newGloss.Form.CopyAlternatives(oldGloss.Form);
					foreach (var segment in originalOccurencesInTexts)
					{
						if (!m_changedParas.ContainsKey(segment.Owner.Hvo))
							continue; // Skip shifting it for items that were not checked

						var glossIdx = segment.AnalysesRS.IndexOf(oldGloss);
						while (glossIdx > -1)
						{
							shiftedSegments.Add(segment);
							segment.AnalysesRS.RemoveAt(glossIdx);
							segment.AnalysesRS.Insert(glossIdx, newGloss);
							glossIdx = segment.AnalysesRS.IndexOf(oldGloss);
						}
					}
				}
				foreach (var shiftedSegment in shiftedSegments)
				{
					originalOccurencesInTexts.Remove(shiftedSegment);
				}
				foreach (IWfiMorphBundle bundle in oldAnalysis.MorphBundlesOS)
				{
					IWfiMorphBundle newBundle = FactWfiMB.Create();
					newAnalysis.MorphBundlesOS.Add(newBundle);
					newBundle.Form.CopyAlternatives(bundle.Form);
					newBundle.SenseRA = bundle.SenseRA;
					newBundle.MorphRA = bundle.MorphRA;
					newBundle.MsaRA = bundle.MsaRA;
				}
			}
		}

		private void ProcessAnalysesAndLexEntries(ProgressDialogWorkingOn progress, IWfiWordform wfOld, IWfiWordform wfNew)
		{
			wfOld.SpellingStatus = (int)SpellingStatusStates.incorrect;

			//if (UpdateLexicalEntries)
			//{
			//    foreach (IWfiAnalysis wa in wfOld.AnalysesOC)
			//    {
			//        if (wa.MorphBundlesOS.Count == 1)
			//        {
			//        }
			//    }
			//}
			if (!KeepAnalyses)
			{
				// Remove multi-morpheme anals in src wf.
				List<IWfiAnalysis> goners = new List<IWfiAnalysis>();
				foreach (IWfiAnalysis goner in wfOld.AnalysesOC)
				{
					if (goner.MorphBundlesOS.Count > 1)
					{
						goners.Add(goner);
				}
				}
				foreach (IWfiAnalysis goner in goners)
				{
					IWfiWordform wf = goner.OwnerOfClass<IWfiWordform>();
					wf.AnalysesOC.Remove(goner);
				}
				goners.Clear();
			}
			if (UpdateLexicalEntries)
			{
				// Change LE allo on single morpheme anals.
				foreach (IWfiAnalysis update in wfOld.AnalysesOC)
				{
					if (update.MorphBundlesOS.Count != 1)
						continue; // Skip any with zero or more than one.

					IWfiMorphBundle mb = update.MorphBundlesOS[0];
					ITsString tss = mb.Form.get_String(m_vernWs);
					string srcForm = tss.Text;
					if (srcForm != null)
					{
						// Change morph bundle form.
						mb.Form.set_String(m_vernWs, NewSpelling);
					}
					IMoForm mf = mb.MorphRA;
					if (mf != null)
					{
						mf.Form.set_String(m_vernWs, NewSpelling);
					}
				}
			}

			// Move remaining anals from src wf to new wf.
			// This changes the owners of the remaining ones,
			// since it is an owning property.
			var analyses = new List<IWfiAnalysis>();
			analyses.AddRange(wfOld.AnalysesOC);
			foreach (var anal in analyses)
				wfNew.AnalysesOC.Add(anal);
		}

		internal static void UpdateProgress(ProgressDialogWorkingOn dlg)
		{
			if (dlg == null)
				return;

			dlg.PerformStep();
			dlg.Update();
		}

		private void SendCountVirtualPropChanged(int hvoWf)
		{
			// Notify everyone about the change in the virtual properties
			// for the three types of analyses.
			WordformVirtualPropChanged(hvoWf, "HumanApprovedAnalyses");
			WordformVirtualPropChanged(hvoWf, "HumanNoOpinionParses");
			WordformVirtualPropChanged(hvoWf, "HumanDisapprovedParses");
			WordformVirtualPropChanged(hvoWf, "FullConcordanceCount");
			WordformVirtualPropChanged(hvoWf, "UserCount");
			WordformVirtualPropChanged(hvoWf, "ParserCount");
			WordformVirtualPropChanged(hvoWf, "ConflictCount");
		}
		void WordformVirtualPropChanged(int hvoWf, string name)
		{
			if (RootBox != null)
			{
				RootBox.PropChanged(hvoWf,
					m_specialSda.MetaDataCache.GetFieldId2(WfiWordformTags.kClassId, name, false),
					0, 0, 1);
			}
		}

		///// <summary>
		///// Switch the values of the integer variables as indicated.
		///// </summary>
		///// <param name="altVals"></param>
		//void SwitchValues(IList<int> altVals)
		//{
		//    for (var i = 0; i < m_hvosToChangeIntProps.Count; i++)
		//    {
		//        var hvo = m_hvosToChangeIntProps[i];
		//        var tag = m_tagsToChangeIntProps[i];
		//        var newVal = altVals[i];
		//        m_specialSda.SetInt(hvo, tag, newVal);
		//    }
		//}

		/// <summary>
		/// Flag may be set to true so that where a wordform has monomorphemic analysis/es,
		/// the lexical entry(s) will also be updated. That is, if this is true, and there is a
		/// monomorphemic analysis that points at a particular MoForm, the form of the MoForm will
		/// be updated.
		/// </summary>
		public bool UpdateLexicalEntries
		{
			get { return m_fUpdateLexicalEntries; }
			set { m_fUpdateLexicalEntries = value; }
		}

		/// <summary>
		/// Flag set true if all occurrences changed. Enables effects of UpdateLexicalEntries
		/// and KeepAnalyses, and causes old wordform to be marked incorrect.
		/// </summary>
		public bool AllChanged
		{
			get { return m_fAllChanged; }
			set { m_fAllChanged = value; }
		}

		/// <summary>
		/// Flag set true to keep analyses, even if all occurrences changed.
		/// (Monomorphemic analyses are kept anyway, if UpdateLexicalEntries is true.)
		/// </summary>
		public bool KeepAnalyses
		{
			get { return m_fKeepAnalyses; }
			set { m_fKeepAnalyses = value; }
		}

		/// <summary>
		/// Flag set true to copy analyses.
		/// </summary>
		public bool CopyAnalyses
		{
			get { return m_fCopyAnalyses; }
			set { m_fCopyAnalyses = value; }
		}

		#region IUndoAction Members

		public void Commit()
		{
		}

		public bool IsDataChange
		{
			get { return true; }
		}

		public bool IsRedoable
		{
			get { return true; }
		}

		public bool Redo()
		{
			//SwitchValues(m_newValues);
			//foreach (var pair in m_changedParas)
			//{
			//    // Review JohnT: should we do the PropChanged, or not?? If not, we should force
			//    // a Refresh when the dialog closes.
			//    pair.Value.SetString(m_cache, pair.Value.NewContents, !fRefreshPending);
			//}
			//UpdateInstanceOf(m_hvoNewWf);
			////// Make sure that the re-created new wordform is the one we will look up if we ask for one for this string and ws.
			////// This MIGHT only be needed during testing (when a test has created a dummy in the process of verifying the Undo),
			////// but it makes the shipping code more robust, too.
			////m_cache.LangProject.WordformInventoryOA.UpdateConcWordform(m_newSpelling, m_vernWs, m_hvoNewWf);
			//SpellingHelper.SetSpellingStatus(m_newSpelling, m_vernWs,
			//    m_cache.LanguageWritingSystemFactoryAccessor, true);
			//if (AllChanged)
			//{
			//    SpellingHelper.SetSpellingStatus(m_oldSpelling, m_vernWs,
			//        m_cache.LanguageWritingSystemFactoryAccessor, false);
			//    SendAnalysisVirtualPropChanged(m_hvoNewWf);
			//    SendAnalysisVirtualPropChanged(m_hvoOldWf);
			//}
			//RespellSda.ReplaceOccurrences(m_hvoOldWf, m_newOccurrencesOldWf);
			//RespellSda.ReplaceOccurrences(m_hvoNewWf, m_newOccurrencesNewWf);
			//SendCountVirtualPropChanged(m_hvoNewWf);
			//SendCountVirtualPropChanged(m_hvoOldWf);
			return true;
		}

		public bool SuppressNotification
		{
			set { }
		}

		public bool Undo()
		{
			//SwitchValues(m_oldValues);
			//foreach (var pair in m_changedParas)
			//{
			//    // Review JohnT: should we do the PropChanged, or not?? If not, we should force
			//    // a Refresh when the dialog closes.
			//    pair.Value.SetString(m_cache, pair.Value.OldContents, !fRefreshPending);
			//}
			//UpdateInstanceOf(m_hvoOldWf);
			//SpellingHelper.SetSpellingStatus(m_newSpelling, m_vernWs,
			//    m_cache.LanguageWritingSystemFactoryAccessor, m_fWasNewSpellingCorrect);
			//SpellingHelper.SetSpellingStatus(m_oldSpelling, m_vernWs,
			//    m_cache.LanguageWritingSystemFactoryAccessor, m_fWasOldSpellingCorrect);
			//RespellSda.ReplaceOccurrences(m_hvoOldWf, m_oldOccurrencesOldWf);
			//RespellSda.ReplaceOccurrences(m_hvoNewWf, m_oldOccurrencesNewWf);
			//SendCountVirtualPropChanged(m_hvoNewWf);
			//SendCountVirtualPropChanged(m_hvoOldWf);
			//if (AllChanged)
			//{
			//    SendAnalysisVirtualPropChanged(m_hvoNewWf);
			//    SendAnalysisVirtualPropChanged(m_hvoOldWf);
			//}
			return true;
		}

		#endregion

		/// <summary>
		/// Remove all changed items from the set of enabled ones.
		/// </summary>
		internal void RemoveChangedItems(HashSet<int> enabledItems, int tagEnabled)
		{
			foreach (var info in m_changedParas.Values)
				foreach (var hvoFake in info.Changes)
				{
					m_specialSda.SetInt(hvoFake, tagEnabled, 0);
					var matchingItem = (from item in enabledItems
							   where item == hvoFake
							   select item).FirstOrDefault();
					if (matchingItem != 0)		// 0 is the standard default value for ints.
						enabledItems.Remove(matchingItem);
				}
		}

		/// <summary>
		/// Gets the case function for the given writing system.
		/// </summary>
		internal CaseFunctions GetCaseFunctionFor(int ws)
		{
			CaseFunctions cf;
			if (!m_caseFunctions.TryGetValue(ws, out cf))
			{
				string icuLocale = m_cache.ServiceLocator.WritingSystemManager.Get(ws).IcuLocale;
				cf = new CaseFunctions(icuLocale);
				m_caseFunctions[ws] = cf;
			}
			return cf;
		}
	}

	/// <summary>
	/// Entend the ConcDecorator with a few more properties needed for respelling.
	/// </summary>
	public class RespellingSda : ConcDecorator, ISetCache
	{
		internal class RespellInfo
		{
			public int AdjustedBeginOffset;
			public int AdjustedEndOffset;
			public ITsString SpellingPreview;
		}

		public const int kflidAdjustedBeginOffset = 9909101;	// on occurrence, int
		public const int kflidAdjustedEndOffset = 9909102;		// on occurrence, int
		public const int kflidSpellingPreview = 9909103;		// on occurrence, string
		public const int kflidOccurrencesInCaptions = 9909104;	// on WfiWordform, reference seq

		Dictionary<int, RespellInfo> m_mapRespell = new Dictionary<int, RespellInfo>();

		public RespellingSda(ISilDataAccessManaged domainDataByFlid, IFdoServiceLocator services)
			: base(domainDataByFlid, services)
		{
			SetOverrideMdc(new RespellingMdc(MetaDataCache as IFwMetaDataCacheManaged));
		}

		public override int get_IntProp(int hvo, int tag)
		{
			RespellInfo info;
			switch (tag)
			{
				case kflidAdjustedBeginOffset:
					if (m_mapRespell.TryGetValue(hvo, out info))
						return info.AdjustedBeginOffset;
					else
						return 0;
				case kflidAdjustedEndOffset:
					if (m_mapRespell.TryGetValue(hvo, out info))
						return info.AdjustedEndOffset;
					else
						return 0;
			}
			return base.get_IntProp(hvo, tag);
		}

		public override ITsString get_StringProp(int hvo, int tag)
		{
			RespellInfo info;
			switch (tag)
			{
				case kflidSpellingPreview:
					if (m_mapRespell.TryGetValue(hvo, out info))
						return info.SpellingPreview;
					else
						return null;
			}
			return base.get_StringProp(hvo, tag);
		}

		public override int[] VecProp(int hvo, int tag)
		{
			switch (tag)
			{
				case kflidOccurrencesInCaptions:
					return new int[0];

			}
			return base.VecProp(hvo, tag);
		}

		public override int get_VecItem(int hvo, int tag, int index)
		{
			switch (tag)
			{
				case kflidOccurrencesInCaptions:
					return 0;
			}
			return base.get_VecItem(hvo, tag, index);
		}

		public override int get_VecSize(int hvo, int tag)
		{
			switch (tag)
			{
				case kflidOccurrencesInCaptions:
					return 0;
			}
			return base.get_VecSize(hvo, tag);
		}

		public override void SetInt(int hvo, int tag, int n)
		{
			RespellInfo info;
			switch (tag)
			{
				case kflidAdjustedBeginOffset:
					if (m_mapRespell.TryGetValue(hvo, out info))
					{
						info.AdjustedBeginOffset = n;
					}
					else
					{
						info = new RespellInfo();
						info.AdjustedBeginOffset = n;
						m_mapRespell.Add(hvo, info);
					}
					break;
				case kflidAdjustedEndOffset:
					if (m_mapRespell.TryGetValue(hvo, out info))
					{
						info.AdjustedEndOffset = n;
					}
					else
					{
						info = new RespellInfo();
						info.AdjustedEndOffset = n;
						m_mapRespell.Add(hvo, info);
					}
					break;
				case ConcDecorator.kflidBeginOffset:
					base.OccurrenceFromHvo(hvo).SetMyBeginOffsetInPara(n);
					break;
				case ConcDecorator.kflidEndOffset:
					base.OccurrenceFromHvo(hvo).SetMyEndOffsetInPara(n);
					break;
				default:
					base.SetInt(hvo, tag, n);
					break;
			}
		}

		public override void SetString(int hvo, int tag, ITsString _tss)
		{
			RespellInfo info;
			switch (tag)
			{
				case kflidSpellingPreview:
					if (m_mapRespell.TryGetValue(hvo, out info))
					{
						info.SpellingPreview = _tss;
					}
					else
					{
						info = new RespellInfo();
						info.SpellingPreview = _tss;
						m_mapRespell.Add(hvo, info);
					}
					break;
				default:
					base.SetString(hvo, tag, _tss);
					break;
			}
		}

		/// <summary>
		/// Allow the Occurrences virtual vector property to be updated.  Only hvos obtained
		/// from the Occurrences property earlier are valid for the values array.
		/// </summary>
		internal void ReplaceOccurrences(int hvo, int[] values)
		{
			ReplaceAnalysisOccurrences(hvo, values);
		}

		/// <summary>
		/// Make additional fake occurrences for where the wordform occurs in captions.
		/// </summary>
		protected override void AddAdditionalOccurrences(int hvoWf, Dictionary<int, IParaFragment> occurrences, ref int nextId, List<int> valuesList)
		{
			var wf = Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().GetObject(hvoWf);
			var wordform = wf.Form.VernacularDefaultWritingSystem.Text;
			if (string.IsNullOrEmpty(wordform))
				return; // paranoia.
			var desiredType = new HashSet<FwObjDataTypes> { FwObjDataTypes.kodtGuidMoveableObjDisp };
			var cmObjRepos = Cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			ProgressState state = null;
			if (PropertyTable != null)
			{
				state = PropertyTable.GetValue<ProgressState>("SpellingPrepState");
			}
			int done = 0;
			int total = InterestingTexts.Count();
			foreach (var text in InterestingTexts)
			{
				done++;
				foreach (IStTxtPara para in text.ParagraphsOS)
				{
					if (!(para is IScrTxtPara))
						continue; // currently only these have embedded pictures.
					var contents = para.Contents;
					var crun = contents.RunCount;
					for (var irun = 0; irun < crun; irun++)
					{
						// See if the run is a picture ORC
						TsRunInfo tri;
						FwObjDataTypes odt;
						ITsTextProps props;
						Guid guid = TsStringUtils.GetGuidFromRun(contents, irun, out odt, out tri, out props, desiredType.ToArray());
						if (guid == Guid.Empty)
							continue;
						// See if its caption contains our wordform
						var obj = cmObjRepos.GetObject(guid);
						var clsid = obj.ClassID;
						if (clsid != CmPictureTags.kClassId)
							continue; // bizarre, just for defensiveness.
						var picture = (ICmPicture) obj;
						var caption = picture.Caption.get_String(Cache.DefaultVernWs);
						var wordMaker = new WordMaker(caption, Cache.LanguageWritingSystemFactoryAccessor);
						for (; ; )
						{
							int ichMin;
							int ichLim;
							var tssTxtWord = wordMaker.NextWord(out ichMin, out ichLim);
							if (tssTxtWord == null)
								break;
							if (tssTxtWord.Text != wordform)
								continue;
							// Make a fake occurrence.
							int hvoFake = nextId--;
							valuesList.Add(hvoFake);
							var occurrence = new CaptionParaFragment();
							occurrence.SetMyBeginOffsetInPara(ichMin);
							occurrence.SetMyEndOffsetInPara(ichLim);
							occurrence.ContainingParaOffset = tri.ichMin;
							occurrence.Paragraph = para;
							occurrence.Picture = picture;
							occurrences[hvoFake] = occurrence;
						}
					}
				}
				if (state != null)
				{
					state.PercentDone = 50 + 50*done/total;
					state.Breath();
				}
			}
		}

		FdoCache Cache { set; get; }

		public void SetCache(FdoCache cache)
		{
			Cache = cache;
		}
	}

	internal class CaptionParaFragment : IParaFragment
	{
		private int m_beginOffset;
		private int m_endOffset;

		//For this case the begin and end offsets are relative to the caption.
		public int GetMyBeginOffsetInPara()
		{
			return m_beginOffset;
		}

		public int GetMyEndOffsetInPara()
		{
			return m_endOffset;
		}

		public void SetMyBeginOffsetInPara(int begin)
		{
			m_beginOffset = begin;
		}

		public void SetMyEndOffsetInPara(int end)
		{
			m_endOffset = end;
		}

		public ISegment Segment
		{
			get { return null; }
		}

		public int ContainingParaOffset { get; set; }

		public ICmPicture Picture { get; set; }

		public ITsString Reference
		{
			get
			{
				var containingseg = Paragraph.SegmentsOS
					.Where(seg => seg.BeginOffset <= ContainingParaOffset)
					.Last();
				return Paragraph.Reference(containingseg, ContainingParaOffset);
			}
		}

		public IStTxtPara Paragraph { get; set;}

		public ICmObject TextObject
		{
			get { return Picture; }
		}

		public int TextFlid
		{
			get { return CmPictureTags.kflidCaption; }
		}

		public bool IsValid
		{
			get { return true; }
		}

		public IAnalysis Analysis
		{
			get { return null; }
		}

		public AnalysisOccurrence BestOccurrence
		{
			get { return null; }
		}
	}

	public class RespellingMdc : ConcMdc
	{
		public RespellingMdc(IFwMetaDataCacheManaged metaDataCache)
			: base(metaDataCache)
		{
		}

		public override void AddVirtualProp(string bstrClass, string bstrField, int luFlid, int type)
		{
			throw new NotImplementedException();
		}

		public override int GetFieldId2(int clid, string sFieldName, bool fIncludeBaseClasses)
		{
			switch (clid)
			{
				case ConcDecorator.kclidFakeOccurrence:
					switch (sFieldName)
					{
						case "AdjustedBeginOffset":
							return RespellingSda.kflidAdjustedBeginOffset;
						case "AdjustedEndOffset":
							return RespellingSda.kflidAdjustedEndOffset;
						case "SpellingPreview":
							return RespellingSda.kflidSpellingPreview;
					}
					break;
				case WfiWordformTags.kClassId:
					if (sFieldName == "OccurrencesInCaptions")
						return RespellingSda.kflidOccurrencesInCaptions;
					break;
			}
			return base.GetFieldId2(clid, sFieldName, fIncludeBaseClasses);
		}

		public override int GetFieldId(string sClassName, string sFieldName, bool fIncludeBaseClasses)
		{
			switch (sClassName)
			{
				case "FakeOccurrence":
					switch (sFieldName)
					{
						case "AdjustedBeginOffset":
							return RespellingSda.kflidAdjustedBeginOffset;
						case "AdjustedEndOffset":
							return RespellingSda.kflidAdjustedEndOffset;
						case "SpellingPreview":
							return RespellingSda.kflidSpellingPreview;
					}
					break;
				case "WfiWordform":
					if (sFieldName == "OccurrencesInCaptions")
						return RespellingSda.kflidOccurrencesInCaptions;
					break;
			}
			return base.GetFieldId(sClassName, sFieldName, fIncludeBaseClasses);
		}

		public override string GetOwnClsName(int flid)
		{
			switch (flid)
			{
				case RespellingSda.kflidAdjustedBeginOffset:
				case RespellingSda.kflidAdjustedEndOffset:
				case RespellingSda.kflidSpellingPreview:
					return "FakeOccurrence";
				case RespellingSda.kflidOccurrencesInCaptions:
					return "WfiWordform";
			}
			return base.GetOwnClsName(flid);
		}

		/// <summary>
		/// The clerk currently ignores properties with signature 0, so doesn't do more with them.
		/// </summary>
		public override int GetDstClsId(int flid)
		{
			switch (flid)
			{
				case RespellingSda.kflidAdjustedBeginOffset:
				case RespellingSda.kflidAdjustedEndOffset:
				case RespellingSda.kflidSpellingPreview:
					return 0;
				case RespellingSda.kflidOccurrencesInCaptions:
					return 0;		// I suppose we could return ConcDecorator.kclidFakeOccurrence
			}
			return base.GetDstClsId(flid);
		}

		public override string GetFieldName(int flid)
		{
			switch (flid)
			{
				case RespellingSda.kflidAdjustedBeginOffset:
					return "AdjustedBeginOffset";
				case RespellingSda.kflidAdjustedEndOffset:
					return "AdjustedEndOffset";
				case RespellingSda.kflidSpellingPreview:
					return "SpellingPreview";
				case RespellingSda.kflidOccurrencesInCaptions:
					return "OccurrencesInCaptions";
			}
			return base.GetFieldName(flid);
		}

		public override int GetFieldType(int flid)
		{
			switch (flid)
			{
				case RespellingSda.kflidOccurrencesInCaptions:
					return (int)CellarPropertyType.ReferenceSequence;
				case RespellingSda.kflidAdjustedBeginOffset:
				case RespellingSda.kflidAdjustedEndOffset:
					return (int)CellarPropertyType.Integer;
				case RespellingSda.kflidSpellingPreview:
					return (int)CellarPropertyType.String;
			}
			return base.GetFieldType(flid);
		}
	}
}
