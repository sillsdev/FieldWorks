// Copyright (c) 2006-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls.XMLViews;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using WaitCursor = SIL.FieldWorks.Common.FwUtils.WaitCursor;

namespace LanguageExplorer.Areas.TextsAndWords
{
	/// <summary>
	/// This class (Form/Dlg) allows the user to do 'bulk edits' on text instances and appropriate analyses.
	///
	/// The user selects a source in the upper-left tree control,
	/// and then selects a target in the upper-right tree control.
	/// Then, the user selects one or more text instances from the browse/concordance view,
	/// and 'Assigns' them to the selected target.
	///
	/// The browse view is to only show text instances from the selected source,
	/// which will not include text instances form any of its child objects.
	/// That is, for a selected wordform (top-most source object),
	/// the displayed text instances will not include those that have been assigned to
	/// an analysis or to a word gloss.
	/// </summary>
	internal sealed class ConcordanceDlg : Form, IFlexComponent
	{
		#region Data Members

		private string _filterMessage = string.Empty;
		private IWfiWordform _wordform;
		private LcmCache _cache;
		private RecordBrowseView _currentBrowseView;
		private Dictionary<int, XElement> _configurationNodes = new Dictionary<int, XElement>(3);
		private Dictionary<int, IRecordList> _recordLists = new Dictionary<int, IRecordList>(3);
		private XMLViewsDataCache _specialSda;
		private int _currentSourceMadeUpFieldIdentifier;
		private const string HelpTopic = "khtpAssignAnalysisUsage";
		private const string SegmentOccurrencesOfWfiWordform = "segmentOccurrencesOfWfiWordform";
		private const string SegmentOccurrencesOfWfiAnalysis = "segmentOccurrencesOfWfiAnalysis";
		private const string SegmentOccurrencesOfWfiGloss = "segmentOccurrencesOfWfiGloss";

		#region Data Members (designer managed)

		private TreeView tvSource;
		private Label label1;
		private Label label2;
		private TreeView tvTarget;
		private Button btnAssign;
		private Label label3;
		private Button btnClose;
		private Panel _pnlConcBrowseHolder;
		private Label label4;
		private Label label5;
		private Button btnHelp;
		private HelpProvider helpProvider;
		private StatusStrip _statusStrip;
		private ToolStripProgressBar _toolStripProgressBar;
		private ToolStripStatusLabel _toolStripFilterStatusLabel;
		private ToolStripStatusLabel _toolStripRecordStatusLabel;
		private StatusBar _mainWindowStatusBar;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private Container components = null;

		#endregion Data Members (designer managed)

		#endregion Data Members

		#region Construction, Initialization, Disposal

		public ConcordanceDlg(StatusBar mainWindowStatusBar, ICmObject sourceObject)
		{
			Guard.AgainstNull(mainWindowStatusBar, nameof(mainWindowStatusBar));
			Guard.AgainstNull(sourceObject, nameof(sourceObject));

			InitializeComponent();
			AccessibleName = GetType().Name;
			_mainWindowStatusBar = mainWindowStatusBar;
			helpProvider = new HelpProvider();
			CheckAssignBtnEnabling();
			if (sourceObject is IWfiWordform)
			{
				_wordform = (IWfiWordform)sourceObject;
			}
			else
			{
				var anal = sourceObject is IWfiAnalysis ? (IWfiAnalysis)sourceObject : sourceObject.OwnerOfClass<IWfiAnalysis>();
				_wordform = anal.OwnerOfClass<IWfiWordform>();
			}
			_cache = _wordform.Cache;
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

			helpProvider.HelpNamespace = PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider).HelpFile;
			helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			helpProvider.SetHelpKeyword(this, PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider).GetHelpString(HelpTopic));
			helpProvider.SetShowHelp(this, true);
			PropertyTable.SetProperty("IgnoreStatusPanel", true, false, true);
			// Gather up the elements.
			var concordanceColumnsElement = XDocument.Parse(TextAndWordsResources.ConcordanceColumns).Root.Element("columns");
			var configurationElement = XElement.Parse(TextAndWordsResources.WordformInSegmentsOccurrenceList);
			configurationElement.Add(concordanceColumnsElement);
			// And create the RecordLists.
			var recordList = PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(SegmentOccurrencesOfWfiWordform, _mainWindowStatusBar, SegmentOccurrencesOfWfiWordformFactoryMethod);
			_recordLists[WfiWordformTags.kClassId] = recordList;
			_configurationNodes[WfiWordformTags.kClassId] = configurationElement;
			configurationElement = XElement.Parse(TextAndWordsResources.AnalysisInSegmentsOccurrenceList);
			configurationElement.Add(concordanceColumnsElement);
			recordList = PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(SegmentOccurrencesOfWfiWordform, _mainWindowStatusBar, SegmentOccurrencesOfWfiAnalysisFactoryMethod);
			_recordLists[WfiAnalysisTags.kClassId] = recordList;
			_configurationNodes[WfiAnalysisTags.kClassId] = configurationElement;
			configurationElement = XElement.Parse(TextAndWordsResources.GlossInSegmentsOccurrenceList);
			configurationElement.Add(concordanceColumnsElement);
			recordList = PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(SegmentOccurrencesOfWfiWordform, _mainWindowStatusBar, SegmentOccurrencesOfWfiGlossFactoryMethod);
			_recordLists[WfiGlossTags.kClassId] = recordList;
			_configurationNodes[WfiGlossTags.kClassId] = configurationElement;
			tvSource.Font = new Font(MiscUtils.StandardSansSerif, 9);
			tvTarget.Font = new Font(MiscUtils.StandardSansSerif, 9);
			var srcTnWf = new TreeNode();
			var tarTnWf = new TreeNode
			{
				Text = srcTnWf.Text = TsStringUtils.NormalizeToNFC(LanguageExplorerResources.ksNoAnalysis),
				Tag = srcTnWf.Tag = _wordform
			};
			tvSource.Nodes.Add(srcTnWf);
			tvTarget.Nodes.Add(tarTnWf);
			if (srcTnWf.Tag == _wordform)
			{
				tvSource.SelectedNode = srcTnWf;
			}
			var cnt = 0;
			// Note: the left side source tree only has human approved analyses,
			// since only those can have instances from text-land pointing at them.
			foreach (var anal in _wordform.HumanApprovedAnalyses)
			{
				var srcTnAnal = new TreeNode();
				var tarTnAnal = new TreeNode
				{
					Text = srcTnAnal.Text = TsStringUtils.NormalizeToNFC(string.Format(LanguageExplorerResources.ksAnalysisX, ++cnt)),
					Tag = srcTnAnal.Tag = anal
				};
				srcTnWf.Nodes.Add(srcTnAnal);
				tarTnWf.Nodes.Add(tarTnAnal);
				if (srcTnAnal.Tag == _wordform)
				{
					tvSource.SelectedNode = srcTnAnal;
				}
				foreach (var gloss in anal.MeaningsOC)
				{
					var tss = gloss.Form.BestAnalysisAlternative;
					var normalizedText = TsStringUtils.NormalizeToNFC(tss.Text);
					var props = tss.get_PropertiesAt(0);
					int nVar;
					var ws = props.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
					var fontname = _wordform.Cache.ServiceLocator.WritingSystemManager.Get(ws).DefaultFontName;
					var srcTnGloss = new TreeNode
					{
						NodeFont = new Font(fontname, 9),
						Text = normalizedText,
						Tag = gloss
					};
					srcTnAnal.Nodes.Add(srcTnGloss);
					var tarTnGloss = new TreeNode
					{
						NodeFont = new Font(fontname, 9),
						Text = normalizedText,
						Tag = gloss
					};
					tarTnAnal.Nodes.Add(tarTnGloss);
					if (srcTnGloss.Tag == _wordform)
					{
						tvSource.SelectedNode = srcTnGloss;
					}
				}
			}
			tvSource.ExpandAll();
			tvSource.SelectedNode.EnsureVisible();
			tvTarget.ExpandAll();
			Subscriber.Subscribe("DialogFilterStatus", DialogFilterStatus_Handler);
		}

		private static IRecordList SegmentOccurrencesOfWfiWordformFactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			Require.That(recordListId == SegmentOccurrencesOfWfiWordform, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create one with an id of '{SegmentOccurrencesOfWfiWordform}'.");
			/*
			<clerk id="segmentOccurrencesOfWfiWordform" shouldHandleDeletion="false">
			    <dynamicloaderinfo assemblyPath="xWorks.dll" class="SIL.FieldWorks.XWorks.TemporaryRecordClerk" />
			    <recordList class="WfiWordform" field="ExactOccurrences">
			    <decoratorClass assemblyPath="xWorks.dll" class="SIL.FieldWorks.XWorks.ConcDecorator" />
			    </recordList>
			    <filters />
			    <sortMethods />
			</clerk>
			*/
			var concDecorator = new ConcDecorator(cache.ServiceLocator);
			concDecorator.InitializeFlexComponent(flexComponentParameters);
			return new RecordList(recordListId, statusBar, concDecorator, false,
				new VectorPropertyParameterObject(cache.LanguageProject, "AllWordforms", concDecorator.MetaDataCache.GetFieldId(WfiWordformTags.kClassName, "ExactOccurrences", false)));
		}

		private static IRecordList SegmentOccurrencesOfWfiAnalysisFactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			Require.That(recordListId == SegmentOccurrencesOfWfiAnalysis, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create one with an id of '{SegmentOccurrencesOfWfiAnalysis}'.");
			/*
<clerk id="segmentOccurrencesOfWfiAnalysis" shouldHandleDeletion="false">
<dynamicloaderinfo assemblyPath="xWorks.dll" class="SIL.FieldWorks.XWorks.TemporaryRecordClerk" />
<recordList class="WfiAnalysis" field="ExactOccurrences">
<decoratorClass assemblyPath="xWorks.dll" class="SIL.FieldWorks.XWorks.ConcDecorator" />
</recordList>
<filters />
<sortMethods />
</clerk>
*/
			var concDecorator = new ConcDecorator(cache.ServiceLocator);
			concDecorator.InitializeFlexComponent(flexComponentParameters);
			return new RecordList(recordListId, statusBar, concDecorator, false,
				new VectorPropertyParameterObject(cache.LanguageProject, "AllWordforms", concDecorator.MetaDataCache.GetFieldId(WfiAnalysisTags.kClassName, "ExactOccurrences", false)));
		}

		private static IRecordList SegmentOccurrencesOfWfiGlossFactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			Require.That(recordListId == SegmentOccurrencesOfWfiGloss, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create one with an id of '{SegmentOccurrencesOfWfiGloss}'.");
			/*
<clerk id="segmentOccurrencesOfWfiGloss" shouldHandleDeletion="false">
<dynamicloaderinfo assemblyPath="xWorks.dll" class="SIL.FieldWorks.XWorks.TemporaryRecordClerk" />
<recordList class="WfiGloss" field="ExactOccurrences">
<decoratorClass assemblyPath="xWorks.dll" class="SIL.FieldWorks.XWorks.ConcDecorator" />
</recordList>
<filters />
<sortMethods />
</clerk>
*/
			var concDecorator = new ConcDecorator(cache.ServiceLocator);
			concDecorator.InitializeFlexComponent(flexComponentParameters);
			return new RecordList(recordListId, statusBar, concDecorator, false,
				new VectorPropertyParameterObject(cache.LanguageProject, "AllWordforms", concDecorator.MetaDataCache.GetFieldId(WfiGlossTags.kClassName, "ExactOccurrences", false)));
		}

		private void DialogFilterStatus_Handler(object newValue)
		{
			_filterMessage = ((string)newValue).Trim();
		}

		#endregion

		/// <inheritdoc />
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
				PropertyTable.RemoveProperty("IgnoreStatusPanel");
				Subscriber.Unsubscribe("DialogFilterStatus", DialogFilterStatus_Handler);
				var repository = PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository);
				foreach (var recordList in _recordLists.Values)
				{
					repository.RemoveRecordList(recordList);
					recordList.Dispose();
				}
				_recordLists.Clear();
				_configurationNodes.Clear();
			}
			base.Dispose(disposing);

			helpProvider = null;
			_recordLists = null;
			_wordform = null;
			_cache = null;
			_currentBrowseView = null;
			_configurationNodes = null;
			_specialSda = null;
			PropertyTable = null;
			Publisher = null;
			Subscriber = null;
			_mainWindowStatusBar = null;
		}

		#endregion Construction, Initialization, Disposal

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConcordanceDlg));
			this.tvSource = new System.Windows.Forms.TreeView();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.tvTarget = new System.Windows.Forms.TreeView();
			this.btnAssign = new System.Windows.Forms.Button();
			this.label3 = new System.Windows.Forms.Label();
			this.btnClose = new System.Windows.Forms.Button();
			this._pnlConcBrowseHolder = new System.Windows.Forms.Panel();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.btnHelp = new System.Windows.Forms.Button();
			this._statusStrip = new System.Windows.Forms.StatusStrip();
			this._toolStripProgressBar = new System.Windows.Forms.ToolStripProgressBar();
			this._toolStripFilterStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this._toolStripRecordStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this._statusStrip.SuspendLayout();
			this.SuspendLayout();
			//
			// tvSource
			//
			this.tvSource.FullRowSelect = true;
			this.tvSource.HideSelection = false;
			resources.ApplyResources(this.tvSource, "tvSource");
			this.tvSource.Name = "tvSource";
			this.tvSource.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tvSource_AfterSelect);
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			//
			// tvTarget
			//
			this.tvTarget.FullRowSelect = true;
			this.tvTarget.HideSelection = false;
			resources.ApplyResources(this.tvTarget, "tvTarget");
			this.tvTarget.Name = "tvTarget";
			this.tvTarget.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tvTarget_AfterSelect);
			//
			// btnAssign
			//
			resources.ApplyResources(this.btnAssign, "btnAssign");
			this.btnAssign.Name = "btnAssign";
			this.btnAssign.Click += new System.EventHandler(this.btnAssign_Click);
			//
			// label3
			//
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			//
			// btnClose
			//
			resources.ApplyResources(this.btnClose, "btnClose");
			this.btnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnClose.Name = "btnClose";
			//
			// m_pnlConcBrowseHolder
			//
			resources.ApplyResources(this._pnlConcBrowseHolder, "_pnlConcBrowseHolder");
			this._pnlConcBrowseHolder.Name = "_pnlConcBrowseHolder";
			//
			// label4
			//
			resources.ApplyResources(this.label4, "label4");
			this.label4.Name = "label4";
			//
			// label5
			//
			resources.ApplyResources(this.label5, "label5");
			this.label5.Name = "label5";
			//
			// btnHelp
			//
			resources.ApplyResources(this.btnHelp, "btnHelp");
			this.btnHelp.Name = "btnHelp";
			this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// m_statusStrip
			//
			resources.ApplyResources(this._statusStrip, "_statusStrip");
			this._statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this._toolStripProgressBar,
			this._toolStripFilterStatusLabel,
			this._toolStripRecordStatusLabel});
			this._statusStrip.Name = "_statusStrip";
			this._statusStrip.SizingGrip = false;
			this._statusStrip.Stretch = false;
			//
			// m_toolStripProgressBar
			//
			resources.ApplyResources(this._toolStripProgressBar, "_toolStripProgressBar");
			this._toolStripProgressBar.Margin = new System.Windows.Forms.Padding(2, 2, 1, 2);
			this._toolStripProgressBar.Name = "_toolStripProgressBar";
			//
			// m_toolStripFilterStatusLabel
			//
			resources.ApplyResources(this._toolStripFilterStatusLabel, "_toolStripFilterStatusLabel");
			this._toolStripFilterStatusLabel.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top)
						| System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)
						| System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
			this._toolStripFilterStatusLabel.BorderStyle = System.Windows.Forms.Border3DStyle.Sunken;
			this._toolStripFilterStatusLabel.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
			this._toolStripFilterStatusLabel.Name = "_toolStripFilterStatusLabel";
			//
			// m_toolStripRecordStatusLabel
			//
			resources.ApplyResources(this._toolStripRecordStatusLabel, "_toolStripRecordStatusLabel");
			this._toolStripRecordStatusLabel.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top)
						| System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)
						| System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
			this._toolStripRecordStatusLabel.BorderStyle = System.Windows.Forms.Border3DStyle.Sunken;
			this._toolStripRecordStatusLabel.Margin = new System.Windows.Forms.Padding(1, 2, 2, 2);
			this._toolStripRecordStatusLabel.Name = "_toolStripRecordStatusLabel";
			//
			// ConcordanceDlg
			//
			this.AcceptButton = this.btnAssign;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.btnClose;
			this.Controls.Add(this._statusStrip);
			this.Controls.Add(this.btnHelp);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.label4);
			this.Controls.Add(this._pnlConcBrowseHolder);
			this.Controls.Add(this.btnClose);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.btnAssign);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.tvTarget);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.tvSource);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ConcordanceDlg";
			this.ShowInTaskbar = false;
			this._statusStrip.ResumeLayout(false);
			this._statusStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		#region Other methods

		private void CheckAssignBtnEnabling()
		{
			btnAssign.Enabled = tvSource.SelectedNode != null && tvTarget.SelectedNode != null && tvSource.SelectedNode.Tag != tvTarget.SelectedNode.Tag && _currentBrowseView.CheckedItems.Any();
		}

		#endregion Other methods

		#region Event Handlers

		private void tvSource_AfterSelect(object sender, TreeViewEventArgs e)
		{
			using (new WaitCursor(this, true))
			{
				// Swap out the browse view.
				if (_currentBrowseView != null)
				{
					// Get rid of old one.
					_currentBrowseView.Hide();
					_pnlConcBrowseHolder.Controls.Remove(_currentBrowseView);
					_currentBrowseView.Dispose();
					_currentBrowseView = null;
				}
				XElement configurationElement;
				IRecordList recordList;
				var selObj = (IAnalysis)tvSource.SelectedNode.Tag;
				switch (selObj.ClassID)
				{
					default:
						throw new InvalidOperationException("Class not recognized.");
					case WfiWordformTags.kClassId:
						configurationElement = _configurationNodes[WfiWordformTags.kClassId];
						recordList = _recordLists[WfiWordformTags.kClassId];
						break;
					case WfiAnalysisTags.kClassId:
						configurationElement = _configurationNodes[WfiAnalysisTags.kClassId];
						recordList = _recordLists[WfiAnalysisTags.kClassId];
						break;
					case WfiGlossTags.kClassId:
						configurationElement = _configurationNodes[WfiGlossTags.kClassId];
						recordList = _recordLists[WfiGlossTags.kClassId];
						break;
				}
				recordList.OwningObject = selObj;
				_currentBrowseView = new RecordBrowseView(configurationElement, _cache, recordList);
				_currentBrowseView.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				// Ensure that the list gets updated whenever it's reloaded.  See LT-8661.
				var sPropName = recordList.Id + "_AlwaysRecomputeVirtualOnReloadList";
				PropertyTable.SetProperty(sPropName, true);
				_currentBrowseView.Dock = DockStyle.Fill;
				_pnlConcBrowseHolder.Controls.Add(_currentBrowseView);
				_currentBrowseView.CheckBoxChanged += CurrentBrowseView_CheckBoxChanged;
				_currentBrowseView.BrowseViewer.SelectionChanged += BrowseViewer_SelectionChanged;
				_currentBrowseView.BrowseViewer.FilterChanged += BrowseViewer_FilterChanged;
				SetRecordStatus();
				_specialSda = _currentBrowseView.BrowseViewer.SpecialCache;
				var specialMdc = _specialSda.MetaDataCache;
				int[] concordanceItems;
				switch (selObj.ClassID)
				{
					default:
						throw new InvalidOperationException("Class not recognized.");
					case WfiWordformTags.kClassId:
						_currentSourceMadeUpFieldIdentifier = specialMdc.GetFieldId2(WfiWordformTags.kClassId, "ExactOccurrences", false);
						concordanceItems = _specialSda.VecProp(selObj.Hvo, _currentSourceMadeUpFieldIdentifier);
						break;
					case WfiAnalysisTags.kClassId:
						_currentSourceMadeUpFieldIdentifier = specialMdc.GetFieldId2(WfiAnalysisTags.kClassId, "ExactOccurrences", false);
						concordanceItems = _specialSda.VecProp(selObj.Hvo, _currentSourceMadeUpFieldIdentifier);
						break;
					case WfiGlossTags.kClassId:
						_currentSourceMadeUpFieldIdentifier = specialMdc.GetFieldId2(WfiGlossTags.kClassId, "ExactOccurrences", false);
						concordanceItems = _specialSda.VecProp(selObj.Hvo, _currentSourceMadeUpFieldIdentifier);
						break;
				}
				// (Re)set selected state in cache, so default behavior of checked is used.
				foreach (var concId in concordanceItems)
				{
					_specialSda.SetInt(concId, XMLViewsDataCache.ktagItemSelected, 1);
				}
				// Set the initial value for the filtering status.
				SetFilterStatus(!string.IsNullOrWhiteSpace(_filterMessage));
				CheckAssignBtnEnabling();
			}
		}

		private void BrowseViewer_FilterChanged(object sender, FilterChangeEventArgs e)
		{
			SetFilterStatus(e.Added != null);
			SetRecordStatus();
		}

		private void BrowseViewer_SelectionChanged(object sender, FwObjectSelectionEventArgs e)
		{
			SetRecordStatus();
		}

		private void SetFilterStatus(bool fIsFiltered)
		{
			if (fIsFiltered)
			{
				_toolStripFilterStatusLabel.BackColor = Color.Yellow;
				_toolStripFilterStatusLabel.Text = LanguageExplorerResources.ksFiltered;
			}
			else
			{
				_toolStripFilterStatusLabel.BackColor = Color.FromKnownColor(KnownColor.Control);
				_toolStripFilterStatusLabel.Text = string.Empty;
			}
		}

		private void SetRecordStatus()
		{
			var objectCount = _currentBrowseView.MyRecordList.ListSize;
			var message = objectCount == 0 ? LanguageExplorerResources.ksNoRecords : $"{_currentBrowseView.BrowseViewer.SelectedIndex + 1}/{objectCount}";
			_toolStripRecordStatusLabel.Text = message;
			_toolStripProgressBar.Value = _toolStripProgressBar.Minimum;    // clear the progress bar
		}

		private void CurrentBrowseView_CheckBoxChanged(object sender, CheckBoxChangedEventArgs e)
		{
			CheckAssignBtnEnabling();
		}

		private void btnAssign_Click(object sender, EventArgs e)
		{
			using (new WaitCursor(this, false))
			{
				var newTarget = (IAnalysis)(tvTarget.SelectedNode.Tag);
				var checkedItems = _currentBrowseView.CheckedItems;
				var src = (IAnalysis)tvSource.SelectedNode.Tag;
				if (checkedItems.Any())
				{
					_toolStripProgressBar.Minimum = 0;
					_toolStripProgressBar.Maximum = checkedItems.Count;
					_toolStripProgressBar.Step = 1;
					_toolStripProgressBar.Value = 0;
				}
				UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoAssignAnalyses, LanguageExplorerResources.ksRedoAssignAnalyses, _specialSda.GetActionHandler(), () =>
				{
					var concSda = ((DomainDataByFlidDecoratorBase)_specialSda.BaseSda).BaseSda as ConcDecorator;
					var originalValues = concSda.VecProp(src.Hvo, _currentSourceMadeUpFieldIdentifier).ToDictionary(originalHvo => originalHvo, originalHvo => concSda.OccurrenceFromHvo(originalHvo));
					foreach (var fakeHvo in checkedItems)
					{
						originalValues.Remove(fakeHvo);
						var analysisOccurrence = concSda.OccurrenceFromHvo(fakeHvo);
						((AnalysisOccurrence)analysisOccurrence).Analysis = newTarget;
						_specialSda.SetInt(fakeHvo, XMLViewsDataCache.ktagItemSelected, 0);
						_toolStripProgressBar.PerformStep();
					}
					// Make sure the correct updated occurrences will be computed when needed in Refresh of the
					// occurrences pane and anywhere else.
					concSda.UpdateExactAnalysisOccurrences(src);
					var recordList = _recordLists[newTarget.ClassID];
					var recordListSda = (ConcDecorator)((DomainDataByFlidDecoratorBase)recordList.VirtualListPublisher).BaseSda;
					recordListSda.UpdateExactAnalysisOccurrences(newTarget);
				});
				CheckAssignBtnEnabling();
				_toolStripProgressBar.Value = 0;
				SetRecordStatus();
			}
		}

		private void tvTarget_AfterSelect(object sender, TreeViewEventArgs e)
		{
			CheckAssignBtnEnabling();
		}

		private void btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider), HelpTopic);
		}

		#endregion Event Handlers
	}
}