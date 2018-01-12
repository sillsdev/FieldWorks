// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Xml;
using LanguageExplorer.Controls.XMLViews;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Filters;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
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
	/// That is, for a selected wordform (top-most soruce object),
	/// the displayed text instances will not include those that have been assigned to
	/// an analysis or to a word gloss.
	/// </summary>
	internal sealed class ConcordanceDlg : Form, IFwGuiControl
	{
		#region Data Members

		private string _filterMessage = string.Empty;
		private IWfiWordform _wordform;
		private LcmCache _cache;
		private XmlNode _configurationNode;
		private RecordBrowseView _currentBrowseView = null;
		private Dictionary<int, XmlNode> _configurationNodes = new Dictionary<int, XmlNode>(3);
		private Dictionary<int, IRecordList> _recordLists = new Dictionary<int, IRecordList>(3);
		private Dictionary<string, bool> _originalRecordListIgnoreStatusPanelValues = new Dictionary<string, bool>(3);
		private XMLViewsDataCache _specialSda;
		private int _currentSourceMadeUpFieldIdentifier;

		private ConcDecorator ConcSda
		{
			get
			{
				return ((DomainDataByFlidDecoratorBase)_specialSda.BaseSda).BaseSda as ConcDecorator;
			}
		}

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
		private const string s_helpTopic = "khtpAssignAnalysisUsage";

		private HelpProvider helpProvider;
		private StatusStrip _statusStrip;
		private ToolStripProgressBar _toolStripProgressBar;
		private ToolStripStatusLabel _toolStripFilterStatusLabel;
		private ToolStripStatusLabel _toolStripRecordStatusLabel;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private Container components = null;

		#endregion Data Members (designer managed)

		#endregion Data Members

		#region Construction, Initialization, Disposal

		public ConcordanceDlg(ICmObject sourceObject)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AccessibleName = GetType().Name;

			helpProvider = new HelpProvider();

			CheckAssignBtnEnabling();

			if (sourceObject is IWfiWordform)
			{
				_wordform = (IWfiWordform)sourceObject;
			}
			else
			{
				var anal = sourceObject is IWfiAnalysis
										? (IWfiAnalysis)sourceObject
										: sourceObject.OwnerOfClass<IWfiAnalysis>();
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

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentCheckingService.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;

			helpProvider.HelpNamespace = PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider").HelpFile;
			helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			helpProvider.SetHelpKeyword(this, PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider").GetHelpString(s_helpTopic));
			helpProvider.SetShowHelp(this, true);

			_progAdvInd = new ProgressReporting(_toolStripProgressBar);

#if RANDYTODO
			// Gather up the nodes.
			const string xpathBase = "/window/controls/parameters[@id='guicontrols']/guicontrol[@id='{0}']/parameters[@id='{1}']";
			var xpath = String.Format(xpathBase, "WordformConcordanceBrowseView", "WordformInSegmentsOccurrenceList");
			var configNode = m_configurationNode.SelectSingleNode(xpath);
			// And create the RecordLists.
<clerk id="segmentOccurrencesOfWfiWordform" shouldHandleDeletion="false">
    <dynamicloaderinfo assemblyPath="xWorks.dll" class="SIL.FieldWorks.XWorks.TemporaryRecordClerk" />
    <recordList class="WfiWordform" field="ExactOccurrences">
    <decoratorClass assemblyPath="xWorks.dll" class="SIL.FieldWorks.XWorks.ConcDecorator" />
    </recordList>
    <filters />
    <sortMethods />
</clerk>
			var recordList = RecordClerkFactory.CreateClerk(PropertyTable, Publisher, Subscriber, true);
			recordList.ProgressReporter = m_progAdvInd;
			_originalRecordListIgnoreStatusPanelValues[recordList.Id] = recordList.IgnoreStatusPanel;
			recordList.IgnoreStatusPanel = true;
			_recordLists[WfiWordformTags.kClassId] = recordList;
			m_configurationNodes[WfiWordformTags.kClassId] = configNode;

			xpath = String.Format(xpathBase, "AnalysisConcordanceBrowseView", "AnalysisInSegmentsOccurrenceList");
			configNode = m_configurationNode.SelectSingleNode(xpath);
<clerk id="segmentOccurrencesOfWfiAnalysis" shouldHandleDeletion="false">
    <dynamicloaderinfo assemblyPath="xWorks.dll" class="SIL.FieldWorks.XWorks.TemporaryRecordClerk" />
    <recordList class="WfiAnalysis" field="ExactOccurrences">
    <decoratorClass assemblyPath="xWorks.dll" class="SIL.FieldWorks.XWorks.ConcDecorator" />
    </recordList>
    <filters />
    <sortMethods />
</clerk>
			recordList = RecordClerkFactory.CreateClerk(PropertyTable, Publisher, Subscriber, true);
			recordList.ProgressReporter = m_progAdvInd;
			_originalRecordListIgnoreStatusPanelValues[recordList.Id] = recordList.IgnoreStatusPanel;
			recordList.IgnoreStatusPanel = true;
			_recordLists[WfiAnalysisTags.kClassId] = recordList;
			m_configurationNodes[WfiAnalysisTags.kClassId] = configNode;

			xpath = String.Format(xpathBase, "GlossConcordanceBrowseView", "GlossInSegmentsOccurrenceList");
			configNode = m_configurationNode.SelectSingleNode(xpath);
<clerk id="segmentOccurrencesOfWfiGloss" shouldHandleDeletion="false">
    <dynamicloaderinfo assemblyPath="xWorks.dll" class="SIL.FieldWorks.XWorks.TemporaryRecordClerk" />
    <recordList class="WfiGloss" field="ExactOccurrences">
    <decoratorClass assemblyPath="xWorks.dll" class="SIL.FieldWorks.XWorks.ConcDecorator" />
    </recordList>
    <filters />
    <sortMethods />
</clerk>
			recordList = RecordClerkFactory.CreateClerk(PropertyTable, Publisher, Subscriber, true);
			recordList.ProgressReporter = m_progAdvInd;
			_originalRecordListIgnoreStatusPanelValues[recordList.Id] = recordList.IgnoreStatusPanel;
			recordList.IgnoreStatusPanel = true;
			_recordLists[WfiGlossTags.kClassId] = recordList;
			m_configurationNodes[WfiGlossTags.kClassId] = configNode;
#endif


			tvSource.Font = new Font(MiscUtils.StandardSansSerif, 9);
			tvTarget.Font = new Font(MiscUtils.StandardSansSerif, 9);

			var srcTnWf = new TreeNode();
			var tarTnWf = new TreeNode();
			tarTnWf.Text = srcTnWf.Text = TsStringUtils.NormalizeToNFC(LanguageExplorerResources.ksNoAnalysis);
			tarTnWf.Tag = srcTnWf.Tag = _wordform;
			tvSource.Nodes.Add(srcTnWf);
			tvTarget.Nodes.Add(tarTnWf);
			if (srcTnWf.Tag == _wordform)
				tvSource.SelectedNode = srcTnWf;
			var cnt = 0;
			// Note: the left side source tree only has human approved analyses,
			// since only those can have instances from text-land pointing at them.
			foreach (var anal in _wordform.HumanApprovedAnalyses)
			{
				var srcTnAnal = new TreeNode();
				var tarTnAnal = new TreeNode
				{
					Text = srcTnAnal.Text = TsStringUtils.NormalizeToNFC(
												String.Format(LanguageExplorerResources.ksAnalysisX, (++cnt))),
					Tag = srcTnAnal.Tag = anal
				};
				srcTnWf.Nodes.Add(srcTnAnal);
				tarTnWf.Nodes.Add(tarTnAnal);
				if (srcTnAnal.Tag == _wordform)
					tvSource.SelectedNode = srcTnAnal;
				foreach (var gloss in anal.MeaningsOC)
				{
					var srcTnGloss = new TreeNode();
					var tarTnGloss = new TreeNode();
					var tss = gloss.Form.BestAnalysisAlternative;
					var props = tss.get_PropertiesAt(0);
					int nVar;
					var ws = props.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
					var fontname = _wordform.Cache.ServiceLocator.WritingSystemManager.Get(ws).DefaultFontName;
					tarTnGloss.NodeFont = new Font(fontname, 9);
					srcTnGloss.NodeFont = new Font(fontname, 9);
					tarTnGloss.Text = srcTnGloss.Text = TsStringUtils.NormalizeToNFC(tss.Text);
					tarTnGloss.Tag = srcTnGloss.Tag = gloss;
					srcTnAnal.Nodes.Add(srcTnGloss);
					tarTnAnal.Nodes.Add(tarTnGloss);
					if (srcTnGloss.Tag == _wordform)
						tvSource.SelectedNode = srcTnGloss;
				}
			}
			tvSource.ExpandAll();
			tvSource.SelectedNode.EnsureVisible();
			tvTarget.ExpandAll();

			Subscriber.Subscribe("DialogFilterStatus", DialogFilterStatus_Handler);
		}

		private void DialogFilterStatus_Handler(object newValue)
		{
			_filterMessage = ((string)newValue).Trim();
		}

		#endregion

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(string.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
				return;

			if( disposing )
			{
				components?.Dispose();

				Subscriber.Unsubscribe("DialogFilterStatus", DialogFilterStatus_Handler);

				foreach (var recordList in _recordLists.Values)
				{
					recordList.Dispose();
				}
				_recordLists.Clear();
				_configurationNodes.Clear();
			}
			base.Dispose(disposing);

			_recordLists = null;
			_wordform = null;
			_cache = null;
			_configurationNode = null;
			_currentBrowseView = null;
			_specialSda = null;
			PropertyTable = null;
			Publisher = null;
			Subscriber = null;
		}

		#endregion Construction, Initialization, Disposal

		/// <summary>
		/// This class provides access to the status strip's progress bar.
		/// </summary>
		private class ProgressReporting : IProgress
		{
			event CancelEventHandler IProgress.Canceling
			{
				add { throw new NotImplementedException(); }
				remove { throw new NotImplementedException(); }
			}

			private readonly ToolStripProgressBar _progressBar;

			public ProgressReporting(ToolStripProgressBar bar)
			{
				_progressBar = bar;
				_progressBar.Step = 1;
				_progressBar.Minimum = 0;
				_progressBar.Maximum = 100;
				_progressBar.Value = 0;
				_progressBar.Style = ProgressBarStyle.Continuous;
			}

			#region IProgress Members

			public int Minimum
			{
				get { return _progressBar.Minimum; }
				set { _progressBar.Minimum = value; }
			}

			public int Maximum
			{
				get { return _progressBar.Maximum; }
				set { _progressBar.Maximum = value; }
			}

			public bool Canceled
			{
				get { return false; }
			}

			/// <summary>
			/// Gets an object to be used for ensuring that required tasks are invoked on the main
			/// UI thread.
			/// </summary>
			public ISynchronizeInvoke SynchronizeInvoke
			{
				get { return _progressBar.Control; }
			}

			public Form Form
			{
				get { return _progressBar.Control.FindForm(); }
			}

			public bool IsIndeterminate
			{
				get { throw new NotImplementedException(); }
				set { throw new NotImplementedException(); }
			}

			public bool AllowCancel
			{
				get { throw new NotImplementedException(); }
				set { throw new NotImplementedException(); }
			}

			public string Message
			{
				get { return _progressBar.ToolTipText; }
				set { _progressBar.ToolTipText = value; }
			}

			public int Position
			{
				get { return _progressBar.Value; }
				set { _progressBar.Value = value; }
			}

			public void Step(int nStepAmt)
			{
				_progressBar.Increment(nStepAmt);
			}

			public int StepSize
			{
				get { return _progressBar.Step; }
				set { _progressBar.Step = value; }
			}

			public string Title
			{
				get { return String.Empty; }
				set { }
			}
			#endregion
		}

		private ProgressReporting _progAdvInd = null;

		#region IFwGuiControl implementation

		/// <summary>
		/// launch the dlg.
		/// </summary>
		public void Launch()
		{
			CheckDisposed();

			ShowDialog(PropertyTable.GetValue<Form>("window"));
		}

		#endregion IFwGuiControl implementation

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
			btnAssign.Enabled = (tvSource.SelectedNode != null && tvTarget.SelectedNode != null
				&& (tvSource.SelectedNode.Tag != tvTarget.SelectedNode.Tag)
				&& _currentBrowseView.CheckedItems.Count > 0);
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

				XmlNode configurationNode;
				IRecordList recordList;
				var selObj = (IAnalysis)tvSource.SelectedNode.Tag;
				switch (selObj.ClassID)
				{
					default:
						throw new InvalidOperationException("Class not recognized.");
					case WfiWordformTags.kClassId:
						configurationNode = _configurationNodes[WfiWordformTags.kClassId];
						recordList = _recordLists[WfiWordformTags.kClassId];
						break;
					case WfiAnalysisTags.kClassId:
						configurationNode = _configurationNodes[WfiAnalysisTags.kClassId];
						recordList = _recordLists[WfiAnalysisTags.kClassId];
						break;
					case WfiGlossTags.kClassId:
						configurationNode = _configurationNodes[WfiGlossTags.kClassId];
						recordList = _recordLists[WfiGlossTags.kClassId];
						break;
				}
				recordList.OwningObject = selObj;

				_currentBrowseView = new RecordBrowseView();
				_currentBrowseView.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				// Ensure that the list gets updated whenever it's reloaded.  See LT-8661.
				var sPropName = recordList.Id + "_AlwaysRecomputeVirtualOnReloadList";
				PropertyTable.SetProperty(sPropName, true, false, false);
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
					_specialSda.SetInt(concId, XMLViewsDataCache.ktagItemSelected, 1);

				// Set the initial value for the filtering status.
				SetFilterStatus(!string.IsNullOrWhiteSpace(_filterMessage));
				CheckAssignBtnEnabling();
			}
		}

		void BrowseViewer_FilterChanged(object sender, FilterChangeEventArgs e)
		{
			SetFilterStatus(e.Added != null);
			SetRecordStatus();
		}

		void BrowseViewer_SelectionChanged(object sender, FwObjectSelectionEventArgs e)
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
			var cobj = _currentBrowseView.MyRecordList.ListSize;
			var idx = _currentBrowseView.BrowseViewer.SelectedIndex;
			var sMsg = cobj == 0 ? LanguageExplorerResources.ksNoRecords : String.Format("{0}/{1}", idx + 1, cobj);
			_toolStripRecordStatusLabel.Text = sMsg;
			_toolStripProgressBar.Value = _toolStripProgressBar.Minimum;	// clear the progress bar
		}

		void CurrentBrowseView_CheckBoxChanged(object sender, CheckBoxChangedEventArgs e)
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
				if (checkedItems.Count > 0)
				{
					_toolStripProgressBar.Minimum = 0;
					_toolStripProgressBar.Maximum = checkedItems.Count;
					_toolStripProgressBar.Step = 1;
					_toolStripProgressBar.Value = 0;
				}

				UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoAssignAnalyses, LanguageExplorerResources.ksRedoAssignAnalyses, _specialSda.GetActionHandler(), () =>
				{
					var concSda = ConcSda;
					var originalValues = new Dictionary<int, IParaFragment>();
					foreach (var originalHvo in concSda.VecProp(src.Hvo, _currentSourceMadeUpFieldIdentifier))
						originalValues.Add(originalHvo, concSda.OccurrenceFromHvo(originalHvo));
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
			ShowHelp.ShowHelpTopic(PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), s_helpTopic);
		}

		#endregion Event Handlers
	}
}
