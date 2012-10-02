using System;
using System.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FdoUi;
using XCore;
using SIL.FieldWorks.Common.Controls;
using SIL.CoreImpl;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
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
	/// the displayed text instances will not include thsoe that have been assigned to
	/// an analysis or to a word gloss.
	/// </summary>
	public class ConcordanceDlg : Form, IFwGuiControl
	{
		#region Data Members

		private IWfiWordform m_wordform;
		// private ICmObject m_sourceObject; // CS0414
		private FdoCache m_cache;
		private Mediator m_mediator;
		private XmlNode m_configurationNode;
		private RecordBrowseView m_currentBrowseView = null;
		private readonly Dictionary<int, XmlNode> m_configurationNodes = new Dictionary<int, XmlNode>(3);
		private readonly Dictionary<int, RecordClerk> m_recordClerks = new Dictionary<int, RecordClerk>(3);
		private XMLViewsDataCache m_specialSda;
		private int m_currentSourceFakeFlid;

		private ConcDecorator ConcSda
		{
			get
			{
				return ((DomainDataByFlidDecoratorBase)m_specialSda.BaseSda).BaseSda as ConcDecorator;
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
		private Panel m_pnlConcBrowseHolder;
		private Label label4;
		private Label label5;
		private Button btnHelp;
		private const string s_helpTopic = "khtpAssignAnalysisUsage";

		private HelpProvider helpProvider;
		private StatusStrip m_statusStrip;
		private ToolStripProgressBar m_toolStripProgressBar;
		private ToolStripStatusLabel m_toolStripFilterStatusLabel;
		private ToolStripStatusLabel m_toolStripRecordStatusLabel;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private Container components = null;

		#endregion Data Members (designer managed)

		#endregion Data Members

		#region Construction, Initialization, Disposal

		public ConcordanceDlg()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AccessibleName = GetType().Name;

			helpProvider = new HelpProvider();

			CheckAssignBtnEnabling();
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
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
				if(components != null)
				{
					components.Dispose();
				}
				foreach (RecordClerk clerk in m_recordClerks.Values)
				{
					// Take it out of the Mediator and Dispose it.
					m_mediator.PropertyTable.RemoveProperty("RecordClerk-" + clerk.Id);
					clerk.Dispose();
				}
				m_recordClerks.Clear();
				m_configurationNodes.Clear();
			}
			base.Dispose( disposing );

			m_wordform = null;
			// m_sourceObject = null; // CS0414
			m_cache = null;
			if (m_mediator != null)
				m_mediator.PropertyTable.RemoveProperty("IgnoreStatusPanel");
			m_mediator = null;
			m_configurationNode = null;
			m_currentBrowseView = null;
			m_specialSda = null;
		}

		#endregion Construction, Initialization, Disposal

		/// <summary>
		/// This class provides access to the status strip's progress bar.
		/// </summary>
		protected class ProgressReporting : IProgress
		{
			event CancelEventHandler IProgress.Canceling
			{
				add { throw new NotImplementedException(); }
				remove { throw new NotImplementedException(); }
			}

			private readonly ToolStripProgressBar m_progressBar;

			public ProgressReporting(ToolStripProgressBar bar)
			{
				m_progressBar = bar;
				m_progressBar.Step = 1;
				m_progressBar.Minimum = 0;
				m_progressBar.Maximum = 100;
				m_progressBar.Value = 0;
				m_progressBar.ProgressBar.Style = ProgressBarStyle.Continuous;
			}

			#region IProgress Members

			public int Minimum
			{
				get { return m_progressBar.Minimum; }
				set { m_progressBar.Minimum = value; }
			}

			public int Maximum
			{
				get { return m_progressBar.Maximum; }
				set { m_progressBar.Maximum = value; }
			}

			public bool Canceled
			{
				get { return false; }
			}

			public Form Form
			{
				get { return m_progressBar.Control.FindForm(); }
			}
			public ProgressBarStyle ProgressBarStyle
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
				get { return m_progressBar.ToolTipText; }
				set { m_progressBar.ToolTipText = value; }
			}

			public int Position
			{
				get { return m_progressBar.Value; }
				set { m_progressBar.Value = value; }
			}

			public void Step(int nStepAmt)
			{
				m_progressBar.Increment(nStepAmt);
			}

			public int StepSize
			{
				get { return m_progressBar.Step; }
				set { m_progressBar.Step = value; }
			}

			public string Title
			{
				get { return String.Empty; }
				set { }
			}
			#endregion
		}

		private ProgressReporting m_progAdvInd = null;

		#region IFwGuiControl implementation

		/// <summary>
		/// Initilize the gui control.
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="configurationNode">NB: In this case it is the main 'window' element node,
		/// se we have to drill down to find the control definition node(s).</param>
		/// <param name="sourceObject"></param>
		public void Init(Mediator mediator, XmlNode configurationNode, ICmObject sourceObject)
		{
			CheckDisposed();

			Debug.Assert(mediator != null);
			Debug.Assert(configurationNode != null);
			Debug.Assert(sourceObject != null && (sourceObject is IWfiWordform || sourceObject is IWfiAnalysis || sourceObject is IWfiGloss));

			m_cache = sourceObject.Cache;
			m_mediator = mediator;
			m_configurationNode = configurationNode;
			// m_sourceObject = sourceObject; // CS0414
			if (sourceObject is IWfiWordform)
			{
				m_wordform = (IWfiWordform)sourceObject;
			}
			else
			{
				var anal = sourceObject is IWfiAnalysis
										? (IWfiAnalysis)sourceObject
										: sourceObject.OwnerOfClass<IWfiAnalysis>();
				m_wordform = anal.OwnerOfClass<IWfiWordform>();
			}

			helpProvider.HelpNamespace = m_mediator.HelpTopicProvider.HelpFile;
			helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			helpProvider.SetHelpKeyword(this, m_mediator.HelpTopicProvider.GetHelpString(s_helpTopic));
			helpProvider.SetShowHelp(this, true);


			m_mediator.PropertyTable.SetProperty("IgnoreStatusPanel", true);
			m_mediator.PropertyTable.SetPropertyPersistence("IgnoreStatusPanel", false);
			m_progAdvInd = new ProgressReporting(m_toolStripProgressBar);

			// Gather up the nodes.
			const string xpathBase = "/window/controls/parameters[@id='guicontrols']/guicontrol[@id='{0}']/parameters[@id='{1}']";
			var xpath = String.Format(xpathBase, "WordformConcordanceBrowseView", "WordformInSegmentsOccurrenceList");
			var configNode = m_configurationNode.SelectSingleNode(xpath);
			// And create the RecordClerks.
			var clerk = RecordClerkFactory.CreateClerk(m_mediator, configNode, true);
			clerk.ProgressReporter = m_progAdvInd;
			m_recordClerks[WfiWordformTags.kClassId] = clerk;
			m_configurationNodes[WfiWordformTags.kClassId] = configNode;

			xpath = String.Format(xpathBase, "AnalysisConcordanceBrowseView", "AnalysisInSegmentsOccurrenceList");
			configNode = m_configurationNode.SelectSingleNode(xpath);
			clerk = RecordClerkFactory.CreateClerk(m_mediator, configNode, true);
			clerk.ProgressReporter = m_progAdvInd;
			m_recordClerks[WfiAnalysisTags.kClassId] = clerk;
			m_configurationNodes[WfiAnalysisTags.kClassId] = configNode;

			xpath = String.Format(xpathBase, "GlossConcordanceBrowseView", "GlossInSegmentsOccurrenceList");
			configNode = m_configurationNode.SelectSingleNode(xpath);
			clerk = RecordClerkFactory.CreateClerk(m_mediator, configNode, true);
			clerk.ProgressReporter = m_progAdvInd;
			m_recordClerks[WfiGlossTags.kClassId] = clerk;
			m_configurationNodes[WfiGlossTags.kClassId] = configNode;

			Debug.Assert(m_wordform != null);
			Debug.Assert(sourceObject != null);

			tvSource.Font = new Font("Arial", 9);
			tvTarget.Font = new Font("Arial", 9);

			var srcTnWf = new TreeNode();
			var tarTnWf = new TreeNode();
			tarTnWf.Text = srcTnWf.Text = TsStringUtils.NormalizeToNFC(MEStrings.ksNoAnalysis);
			tarTnWf.Tag = srcTnWf.Tag = m_wordform;
			tvSource.Nodes.Add(srcTnWf);
			tvTarget.Nodes.Add(tarTnWf);
			if (srcTnWf.Tag == sourceObject)
				tvSource.SelectedNode = srcTnWf;
			var cnt = 0;
			// Note: the left side source tree only has human approved analyses,
			// since only those can have instances from text-land pointing at them.
			foreach (var anal in m_wordform.HumanApprovedAnalyses)
			{
				var srcTnAnal = new TreeNode();
				var tarTnAnal = new TreeNode
									{
										Text = srcTnAnal.Text = TsStringUtils.NormalizeToNFC(
																	String.Format(MEStrings.ksAnalysisX, (++cnt))),
										Tag = srcTnAnal.Tag = anal
									};
				srcTnWf.Nodes.Add(srcTnAnal);
				tarTnWf.Nodes.Add(tarTnAnal);
				if (srcTnAnal.Tag == sourceObject)
					tvSource.SelectedNode = srcTnAnal;
				foreach (var gloss in anal.MeaningsOC)
				{
					var srcTnGloss = new TreeNode();
					var tarTnGloss = new TreeNode();
					var tss = gloss.Form.BestAnalysisAlternative;
					var props = tss.get_PropertiesAt(0);
					int nVar;
					var ws = props.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
					var fontname = m_wordform.Cache.ServiceLocator.WritingSystemManager.Get(ws).DefaultFontName;
					tarTnGloss.NodeFont = new Font(fontname, 9);
					srcTnGloss.NodeFont = new Font(fontname, 9);
					tarTnGloss.Text = srcTnGloss.Text = TsStringUtils.NormalizeToNFC(tss.Text);
					tarTnGloss.Tag = srcTnGloss.Tag = gloss;
					srcTnAnal.Nodes.Add(srcTnGloss);
					tarTnAnal.Nodes.Add(tarTnGloss);
					if (srcTnGloss.Tag == sourceObject)
						tvSource.SelectedNode = srcTnGloss;
				}
			}
			tvSource.ExpandAll();
			tvSource.SelectedNode.EnsureVisible();
			tvTarget.ExpandAll();
		}

		/// <summary>
		/// launch the dlg.
		/// </summary>
		public void Launch()
		{
			CheckDisposed();

			ShowDialog((Form)m_mediator.PropertyTable.GetValue("window"));
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
			this.m_pnlConcBrowseHolder = new System.Windows.Forms.Panel();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.btnHelp = new System.Windows.Forms.Button();
			this.m_statusStrip = new System.Windows.Forms.StatusStrip();
			this.m_toolStripProgressBar = new System.Windows.Forms.ToolStripProgressBar();
			this.m_toolStripFilterStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_toolStripRecordStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_statusStrip.SuspendLayout();
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
			resources.ApplyResources(this.m_pnlConcBrowseHolder, "m_pnlConcBrowseHolder");
			this.m_pnlConcBrowseHolder.Name = "m_pnlConcBrowseHolder";
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
			resources.ApplyResources(this.m_statusStrip, "m_statusStrip");
			this.m_statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.m_toolStripProgressBar,
			this.m_toolStripFilterStatusLabel,
			this.m_toolStripRecordStatusLabel});
			this.m_statusStrip.Name = "m_statusStrip";
			this.m_statusStrip.SizingGrip = false;
			this.m_statusStrip.Stretch = false;
			//
			// m_toolStripProgressBar
			//
			resources.ApplyResources(this.m_toolStripProgressBar, "m_toolStripProgressBar");
			this.m_toolStripProgressBar.Margin = new System.Windows.Forms.Padding(2, 2, 1, 2);
			this.m_toolStripProgressBar.Name = "m_toolStripProgressBar";
			//
			// m_toolStripFilterStatusLabel
			//
			resources.ApplyResources(this.m_toolStripFilterStatusLabel, "m_toolStripFilterStatusLabel");
			this.m_toolStripFilterStatusLabel.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top)
						| System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)
						| System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
			this.m_toolStripFilterStatusLabel.BorderStyle = System.Windows.Forms.Border3DStyle.Sunken;
			this.m_toolStripFilterStatusLabel.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
			this.m_toolStripFilterStatusLabel.Name = "m_toolStripFilterStatusLabel";
			//
			// m_toolStripRecordStatusLabel
			//
			resources.ApplyResources(this.m_toolStripRecordStatusLabel, "m_toolStripRecordStatusLabel");
			this.m_toolStripRecordStatusLabel.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top)
						| System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)
						| System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
			this.m_toolStripRecordStatusLabel.BorderStyle = System.Windows.Forms.Border3DStyle.Sunken;
			this.m_toolStripRecordStatusLabel.Margin = new System.Windows.Forms.Padding(1, 2, 2, 2);
			this.m_toolStripRecordStatusLabel.Name = "m_toolStripRecordStatusLabel";
			//
			// ConcordanceDlg
			//
			this.AcceptButton = this.btnAssign;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.btnClose;
			this.Controls.Add(this.m_statusStrip);
			this.Controls.Add(this.btnHelp);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.m_pnlConcBrowseHolder);
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
			this.m_statusStrip.ResumeLayout(false);
			this.m_statusStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		#region Other methods

		private void CheckAssignBtnEnabling()
		{
			btnAssign.Enabled = (tvSource.SelectedNode != null && tvTarget.SelectedNode != null
				&& (tvSource.SelectedNode.Tag != tvTarget.SelectedNode.Tag)
				&& m_currentBrowseView.CheckedItems.Count > 0);
		}

		#endregion Other methods

		#region Event Handlers

		protected override void OnClosing(CancelEventArgs e)
		{
			if (!IsDisposed && m_mediator != null)
			{
				m_mediator.PropertyTable.SetProperty("IgnoreStatusPanel", false);
				m_mediator.PropertyTable.SetPropertyPersistence("IgnoreStatusPanel", false);
			}
		}

		private void tvSource_AfterSelect(object sender, TreeViewEventArgs e)
		{
			using (new WaitCursor(this, true))
			{
				// Swap out the browse view.
				if (m_currentBrowseView != null)
				{
					// Get rid of old one.
					m_currentBrowseView.Hide();
					m_pnlConcBrowseHolder.Controls.Remove(m_currentBrowseView);
					m_currentBrowseView.Dispose();
					m_currentBrowseView = null;
				}

				XmlNode configurationNode;
				RecordClerk clerk;
				var selObj = (IAnalysis)tvSource.SelectedNode.Tag;
				switch (selObj.ClassID)
				{
					default:
						throw new InvalidOperationException("Class not recognized.");
					case WfiWordformTags.kClassId:
						configurationNode = m_configurationNodes[WfiWordformTags.kClassId];
						clerk = m_recordClerks[WfiWordformTags.kClassId];
						break;
					case WfiAnalysisTags.kClassId:
						configurationNode = m_configurationNodes[WfiAnalysisTags.kClassId];
						clerk = m_recordClerks[WfiAnalysisTags.kClassId];
						break;
					case WfiGlossTags.kClassId:
						configurationNode = m_configurationNodes[WfiGlossTags.kClassId];
						clerk = m_recordClerks[WfiGlossTags.kClassId];
						break;
				}
				clerk.OwningObject = selObj;

				m_currentBrowseView = new RecordBrowseView();
				m_currentBrowseView.Init(m_mediator, configurationNode);
				// Ensure that the list gets updated whenever it's reloaded.  See LT-8661.
				var sPropName = clerk.Id + "_AlwaysRecomputeVirtualOnReloadList";
				m_mediator.PropertyTable.SetProperty(sPropName, true, false);
				m_mediator.PropertyTable.SetPropertyPersistence(sPropName, false);
				m_currentBrowseView.Dock = DockStyle.Fill;
				m_pnlConcBrowseHolder.Controls.Add(m_currentBrowseView);
				m_currentBrowseView.CheckBoxChanged += m_currentBrowseView_CheckBoxChanged;
				m_currentBrowseView.BrowseViewer.SelectionChanged += BrowseViewer_SelectionChanged;
				m_currentBrowseView.BrowseViewer.FilterChanged += BrowseViewer_FilterChanged;
				SetRecordStatus();

				m_specialSda = m_currentBrowseView.BrowseViewer.SpecialCache;
				var specialMdc = m_specialSda.MetaDataCache;
				int[] concordanceItems;
				switch (selObj.ClassID)
				{
					default:
						throw new InvalidOperationException("Class not recognized.");
					case WfiWordformTags.kClassId:
						m_currentSourceFakeFlid = specialMdc.GetFieldId2(WfiWordformTags.kClassId, "ExactOccurrences", false);
						concordanceItems = m_specialSda.VecProp(selObj.Hvo, m_currentSourceFakeFlid);
						break;
					case WfiAnalysisTags.kClassId:
						m_currentSourceFakeFlid = specialMdc.GetFieldId2(WfiAnalysisTags.kClassId, "ExactOccurrences", false);
						concordanceItems = m_specialSda.VecProp(selObj.Hvo, m_currentSourceFakeFlid);
						break;
					case WfiGlossTags.kClassId:
						m_currentSourceFakeFlid = specialMdc.GetFieldId2(WfiGlossTags.kClassId, "ExactOccurrences", false);
						concordanceItems = m_specialSda.VecProp(selObj.Hvo, m_currentSourceFakeFlid);
						break;
				}
				// (Re)set selected state in cache, so default behavior of checked is used.
				foreach (var concId in concordanceItems)
					m_specialSda.SetInt(concId, XMLViewsDataCache.ktagItemSelected, 1);

				// Set the initial value for the filtering status.
				var sFilterMsg = m_mediator.PropertyTable.GetStringProperty("DialogFilterStatus", String.Empty);
				if (sFilterMsg != null)
					sFilterMsg = sFilterMsg.Trim();
				SetFilterStatus(!String.IsNullOrEmpty(sFilterMsg));
				CheckAssignBtnEnabling();
			}
		}

		void BrowseViewer_FilterChanged(object sender, Filters.FilterChangeEventArgs e)
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
				m_toolStripFilterStatusLabel.BackColor = Color.Yellow;
				m_toolStripFilterStatusLabel.Text = MEStrings.ksFiltered;
			}
			else
			{
				m_toolStripFilterStatusLabel.BackColor = Color.FromKnownColor(KnownColor.Control);
				m_toolStripFilterStatusLabel.Text = String.Empty;
			}
		}

		private void SetRecordStatus()
		{
			var cobj = m_currentBrowseView.Clerk.ListSize;
			var idx = m_currentBrowseView.BrowseViewer.SelectedIndex;
			var sMsg = cobj == 0 ? MEStrings.ksNoRecords : String.Format("{0}/{1}", idx + 1, cobj);
			m_toolStripRecordStatusLabel.Text = sMsg;
			m_toolStripProgressBar.Value = m_toolStripProgressBar.Minimum;	// clear the progress bar
		}

		void m_currentBrowseView_CheckBoxChanged(object sender, CheckBoxChangedEventArgs e)
		{
			CheckAssignBtnEnabling();
		}

		private void btnAssign_Click(object sender, EventArgs e)
		{
			using (new WaitCursor(this, false))
			{
				var newTarget = (IAnalysis)(tvTarget.SelectedNode.Tag);
				var checkedItems = m_currentBrowseView.CheckedItems;
				var src = (IAnalysis)tvSource.SelectedNode.Tag;
				if (checkedItems.Count > 0)
				{
					m_toolStripProgressBar.Minimum = 0;
					m_toolStripProgressBar.Maximum = checkedItems.Count;
					m_toolStripProgressBar.Step = 1;
					m_toolStripProgressBar.Value = 0;
				}

				UndoableUnitOfWorkHelper.Do(MEStrings.ksUndoAssignAnalyses, MEStrings.ksRedoAssignAnalyses, m_specialSda.GetActionHandler(), () =>
				{
					var concSda = ConcSda;
					var originalValues = new Dictionary<int, IParaFragment>();
					foreach (var originalHvo in concSda.VecProp(src.Hvo, m_currentSourceFakeFlid))
						originalValues.Add(originalHvo, concSda.OccurrenceFromHvo(originalHvo));
					foreach (var fakeHvo in checkedItems)
					{
						originalValues.Remove(fakeHvo);
						var analysisOccurrence = concSda.OccurrenceFromHvo(fakeHvo);
						((AnalysisOccurrence)analysisOccurrence).Analysis = newTarget;
						m_specialSda.SetInt(fakeHvo, XMLViewsDataCache.ktagItemSelected, 0);
						m_toolStripProgressBar.PerformStep();
					}
					// Make sure the correct updated occurrences will be computed when needed in Refresh of the
					// occurrences pane and anywhere else.
					concSda.UpdateExactAnalysisOccurrences(src);
					var clerk = m_recordClerks[newTarget.ClassID];
					var clerkSda = (ConcDecorator)((DomainDataByFlidDecoratorBase) clerk.VirtualListPublisher).BaseSda;
					clerkSda.UpdateExactAnalysisOccurrences(newTarget);
				});

				CheckAssignBtnEnabling();
				m_toolStripProgressBar.Value = 0;
				SetRecordStatus();
			}
		}

		private void tvTarget_AfterSelect(object sender, TreeViewEventArgs e)
		{
			CheckAssignBtnEnabling();
		}

		private void btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_mediator.HelpTopicProvider, s_helpTopic);
		}

		#endregion Event Handlers
	}
}
