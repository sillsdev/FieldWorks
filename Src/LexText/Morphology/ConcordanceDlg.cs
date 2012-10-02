using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.FdoUi;
using XCore;
using SIL.FieldWorks.Common.Controls;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// Summary description for ConcordanceDlg.
	/// </summary>
	public class ConcordanceDlg : Form, IFwGuiControl
	{
		#region Data Members

		private IWfiWordform m_wordform;
		private ICmObject m_sourceObject;
		private FdoCache m_cache;
		private Mediator m_mediator;
		private XmlNode m_configurationNode;
		private RecordBrowseView m_currentBrowseView = null;
		private Dictionary<int, XmlNode> m_configurationNodes = new Dictionary<int, XmlNode>(3);
		private Dictionary<int, RecordClerk> m_recordClerks = new Dictionary<int, RecordClerk>(3);

		#region Data Members (designer managed)

		private System.Windows.Forms.TreeView tvSource;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TreeView tvTarget;
		private System.Windows.Forms.Button btnAssign;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Button btnClose;
		private System.Windows.Forms.Panel m_pnlConcBrowseHolder;
		private Label label4;
		private Label label5;
		private Button btnHelp;
		private const string s_helpTopic = "khtpAssignAnalysisUsage";

		private System.Windows.Forms.HelpProvider helpProvider;
		private StatusStrip m_statusStrip;
		private ToolStripProgressBar m_toolStripProgressBar;
		private ToolStripStatusLabel m_toolStripFilterStatusLabel;
		private ToolStripStatusLabel m_toolStripRecordStatusLabel;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		#endregion Data Members (designer managed)

		#endregion Data Members

		#region Construction, Initialization, Disposal

		public ConcordanceDlg()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			this.helpProvider = new System.Windows.Forms.HelpProvider();
			this.helpProvider.HelpNamespace = FwApp.App.HelpFile;
			this.helpProvider.SetHelpNavigator(this, System.Windows.Forms.HelpNavigator.Topic);
			this.helpProvider.SetHelpKeyword(this, FwApp.App.GetHelpString(s_helpTopic, 0));
			this.helpProvider.SetShowHelp(this, true);

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
		protected override void Dispose( bool disposing )
		{
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
			m_sourceObject = null;
			m_cache = null;
			if (m_mediator != null)
				m_mediator.PropertyTable.RemoveProperty("IgnoreStatusPanel");
			m_mediator = null;
			m_configurationNode = null;
			m_currentBrowseView = null;
		}

		#endregion Construction, Initialization, Disposal

		/// <summary>
		/// This class provides access to the status strip's progress bar.
		/// </summary>
		protected class ProgressReporting : IAdvInd4
		{
			ToolStripProgressBar m_progressBar;

			public ProgressReporting(ToolStripProgressBar bar)
			{
				m_progressBar = bar;
				m_progressBar.Step = 1;
				m_progressBar.Minimum = 0;
				m_progressBar.Maximum = 100;
				m_progressBar.Value = 0;
				m_progressBar.ProgressBar.Style = ProgressBarStyle.Continuous;
			}

			#region IAdvInd4 Members

			public void GetRange(out int nMin, out int nMax)
			{
				nMin = m_progressBar.Minimum;
				nMax = m_progressBar.Maximum;
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

			public void SetRange(int nMin, int nMax)
			{
				m_progressBar.Minimum = nMin;
				m_progressBar.Maximum = nMax;
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
		/// <param name="cache"></param>
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
			m_sourceObject = sourceObject;
			if (sourceObject is IWfiWordform)
			{
				m_wordform = sourceObject as IWfiWordform;
			}
			else
			{
				IWfiAnalysis anal = null;
				if (sourceObject is IWfiAnalysis)
					anal = sourceObject as IWfiAnalysis;
				else
					anal = WfiAnalysis.CreateFromDBObject(m_cache, sourceObject.OwnerHVO);
				m_wordform = WfiWordform.CreateFromDBObject(m_cache, anal.OwnerHVO);
			}

			m_mediator.PropertyTable.SetProperty("IgnoreStatusPanel", true);
			m_mediator.PropertyTable.SetPropertyPersistence("IgnoreStatusPanel", false);
			m_progAdvInd = new ProgressReporting(m_toolStripProgressBar);

			// Gather up the nodes.
			string xpathBase = "/window/controls/parameters[@id='guicontrols']/guicontrol[@id='{0}']/parameters[@id='{1}']";
			string xpath = String.Format(xpathBase, "WordformConcordanceBrowseView", "WordformInTwficsOccurrenceList");
			XmlNode configNode = m_configurationNode.SelectSingleNode(xpath);
			// And create the RecordClerks.
			RecordClerk clerk = RecordClerkFactory.CreateClerk(m_mediator, configNode);
			clerk.ProgressReporter = m_progAdvInd;
			m_recordClerks[WfiWordform.kclsidWfiWordform] = clerk;
			m_configurationNodes[WfiWordform.kclsidWfiWordform] = configNode;

			xpath = String.Format(xpathBase, "AnalysisConcordanceBrowseView", "AnalysisInTwficsOccurrenceList");
			configNode = m_configurationNode.SelectSingleNode(xpath);
			clerk = RecordClerkFactory.CreateClerk(m_mediator, configNode);
			clerk.ProgressReporter = m_progAdvInd;
			m_recordClerks[WfiAnalysis.kclsidWfiAnalysis] = clerk;
			m_configurationNodes[WfiAnalysis.kclsidWfiAnalysis] = configNode;

			xpath = String.Format(xpathBase, "GlossConcordanceBrowseView", "GlossInTwficsOccurrenceList");
			configNode = m_configurationNode.SelectSingleNode(xpath);
			clerk = RecordClerkFactory.CreateClerk(m_mediator, configNode);
			clerk.ProgressReporter = m_progAdvInd;
			m_recordClerks[WfiGloss.kclsidWfiGloss] = clerk;
			m_configurationNodes[WfiGloss.kclsidWfiGloss] = configNode;

			int concordOnHvo = sourceObject.Hvo;

			Debug.Assert(m_wordform != null);
			Debug.Assert(concordOnHvo > 0);

			tvSource.Font = new Font("Arial", 9);
			tvTarget.Font = new Font("Arial", 9);

			TreeNode srcTnWf = new TreeNode();
			TreeNode tarTnWf = new TreeNode();
			StringUtils.InitIcuDataDir();	// used for normalizing strings to NFC
			tarTnWf.Text = srcTnWf.Text = StringUtils.NormalizeToNFC(MEStrings.ksNoAnalysis);
			tarTnWf.Tag = srcTnWf.Tag = m_wordform.Hvo;
			tvSource.Nodes.Add(srcTnWf);
			tvTarget.Nodes.Add(tarTnWf);
			if ((int)srcTnWf.Tag == concordOnHvo)
				tvSource.SelectedNode = srcTnWf;
			int cnt = 0;
			// Note: the left side source tree only has human approved analyses,
			// since only those can have instances from text-land pointing at them.
			foreach (int humanApprovedAnalId in m_wordform.HumanApprovedAnalyses)
			{
				IWfiAnalysis anal = WfiAnalysis.CreateFromDBObject(m_wordform.Cache, humanApprovedAnalId);
				TreeNode srcTnAnal = new TreeNode();
				TreeNode tarTnAnal = new TreeNode();
				tarTnAnal.Text = srcTnAnal.Text = StringUtils.NormalizeToNFC(
					String.Format(MEStrings.ksAnalysisX, (++cnt).ToString()));
				tarTnAnal.Tag = srcTnAnal.Tag = anal.Hvo;
				srcTnWf.Nodes.Add(srcTnAnal);
				tarTnWf.Nodes.Add(tarTnAnal);
				if ((int)srcTnAnal.Tag == concordOnHvo)
					tvSource.SelectedNode = srcTnAnal;
				foreach (WfiGloss gloss in anal.MeaningsOC)
				{
					TreeNode srcTnGloss = new TreeNode();
					TreeNode tarTnGloss = new TreeNode();
					ITsString tss = gloss.Form.BestAnalysisAlternative;
					ITsTextProps props = tss.get_PropertiesAt(0);
					int nVar;
					int ws = props.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
					string fontname = m_wordform.Cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(ws).DefaultMonospace;
					tarTnGloss.NodeFont = new Font(fontname, 9);
					srcTnGloss.NodeFont = new Font(fontname, 9);
					tarTnGloss.Text = srcTnGloss.Text = StringUtils.NormalizeToNFC(tss.Text);
					tarTnGloss.Tag = srcTnGloss.Tag = gloss.Hvo;
					srcTnAnal.Nodes.Add(srcTnGloss);
					tarTnAnal.Nodes.Add(tarTnGloss);
					if ((int)srcTnGloss.Tag == concordOnHvo)
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
				&& ((int)(tvSource.SelectedNode.Tag) != (int)(tvTarget.SelectedNode.Tag))
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

		private void tvSource_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e)
		{
			using (new SIL.FieldWorks.Common.Utils.WaitCursor(this, true))
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

				XmlNode configurationNode = null;
				RecordClerk clerk = null;
				ICmObject selObj = CmObject.CreateFromDBObject(m_cache, (int)tvSource.SelectedNode.Tag);
				List<int> concIds = null;
				switch (selObj.ClassID)
				{
					case WfiWordform.kclsidWfiWordform:
						configurationNode = m_configurationNodes[WfiWordform.kClassId];
						clerk = m_recordClerks[WfiWordform.kClassId];
						concIds = (selObj as IWfiWordform).ConcordanceIds;
						break;
					case WfiAnalysis.kclsidWfiAnalysis:
						configurationNode = m_configurationNodes[WfiAnalysis.kClassId];
						clerk = m_recordClerks[WfiAnalysis.kClassId];
						concIds = (selObj as IWfiAnalysis).ConcordanceIds;
						break;
					case WfiGloss.kclsidWfiGloss:
						configurationNode = m_configurationNodes[WfiGloss.kClassId];
						clerk = m_recordClerks[WfiGloss.kClassId];
						concIds = (selObj as IWfiGloss).ConcordanceIds;
						break;
				}
				clerk.OwningObject = selObj;

				// (Re)set selected state in cache, so default behavior of checked is used.
				foreach (int concId in concIds)
				{
					m_cache.VwCacheDaAccessor.CacheIntProp(concId, XmlBrowseViewVc.ktagItemSelected, 1);
				}
				m_currentBrowseView = new RecordBrowseView();
				m_currentBrowseView.Init(m_mediator, configurationNode);
				// Ensure that the list gets updated whenever it's reloaded.  See LT-8661.
				string sPropName = clerk.Id + "_AlwaysRecomputeVirtualOnReloadList";
				m_mediator.PropertyTable.SetProperty(sPropName, true, false);
				m_mediator.PropertyTable.SetPropertyPersistence(sPropName, false);
				m_currentBrowseView.Dock = DockStyle.Fill;
				m_pnlConcBrowseHolder.Controls.Add(m_currentBrowseView);
				m_currentBrowseView.CheckBoxChanged += new SIL.FieldWorks.Common.Controls.CheckBoxChangedEventHandler(m_currentBrowseView_CheckBoxChanged);
				m_currentBrowseView.BrowseViewer.SelectionChanged += new FwSelectionChangedEventHandler(BrowseViewer_SelectionChanged);
				m_currentBrowseView.BrowseViewer.FilterChanged += new SIL.FieldWorks.Filters.FilterChangeHandler(BrowseViewer_FilterChanged);
				SetRecordStatus();
				// Set the initial value for the filtering status.
				string sFilterMsg = m_mediator.PropertyTable.GetStringProperty("DialogFilterStatus", String.Empty);
				if (sFilterMsg != null)
					sFilterMsg = sFilterMsg.Trim();
				SetFilterStatus(!String.IsNullOrEmpty(sFilterMsg));
				CheckAssignBtnEnabling();
			}
		}

		void BrowseViewer_FilterChanged(object sender, SIL.FieldWorks.Filters.FilterChangeEventArgs e)
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
			string sMsg;
			int cobj = m_currentBrowseView.Clerk.ListSize;
			int idx = m_currentBrowseView.BrowseViewer.SelectedIndex;
			if (cobj == 0)
				sMsg = MEStrings.ksNoRecords;
			else
				sMsg = String.Format("{0}/{1}", idx + 1, cobj);
			m_toolStripRecordStatusLabel.Text = sMsg;
			m_toolStripProgressBar.Value = m_toolStripProgressBar.Minimum;	// clear the progress bar
		}

		void m_currentBrowseView_CheckBoxChanged(object sender, SIL.FieldWorks.Common.Controls.CheckBoxChangedEventArgs e)
		{
			CheckAssignBtnEnabling();
		}

		private void btnAssign_Click(object sender, System.EventArgs e)
		{
			using (new SIL.FieldWorks.Common.Utils.WaitCursor(this, false))
			{
				int newTargetHvo = (int)(tvTarget.SelectedNode.Tag);
				List<int> checkedItems = m_currentBrowseView.CheckedItems;
				int virtFlid = m_currentBrowseView.Clerk.VirtualFlid;
				int srcHvo = (int)tvSource.SelectedNode.Tag;
				if (checkedItems.Count > 0)
				{
					m_toolStripProgressBar.Minimum = 0;
					m_toolStripProgressBar.Maximum = checkedItems.Count;
					m_toolStripProgressBar.Step = 1;
					m_toolStripProgressBar.Value = 0;
				}
				foreach (int annHvo in checkedItems)
				{
					ICmBaseAnnotation ann = null;
					if (m_cache.IsDummyObject(annHvo))
					{
						ann = ConvertDummyToReal(srcHvo, virtFlid, annHvo) as ICmBaseAnnotation;
						Debug.Assert(ann != null);
					}
					else
					{
						ann = CmBaseAnnotation.CreateFromDBObject(m_cache, annHvo);
					}
					ann.InstanceOfRAHvo = newTargetHvo;
					m_cache.SetIntProperty(ann.Hvo, XmlBrowseViewVc.ktagItemSelected, 0);
					m_toolStripProgressBar.PerformStep();
				}
				// After these changes, we need to refresh the display.  PropChanged() redraws the
				// list, but incorrect (and inconsistent) items are then displayed.  See LT-8703.
				// Refreshing the clerk reloads the list and then redraws it.
				m_cache.PropChanged(null, PropChangeType.kpctNotifyAll, srcHvo, virtFlid, 0, 0,
					checkedItems.Count);
				m_currentBrowseView.Clerk.OnRefresh(null);
				CheckAssignBtnEnabling();
				m_toolStripProgressBar.Value = 0;
				SetRecordStatus();
			}
		}

		/// <summary>
		/// Let the caller issue a PropChanged for this list whenever it is through with its conversions.
		/// </summary>
		/// <param name="owningflid"></param>
		/// <param name="hvoDummyId"></param>
		/// <returns></returns>
		private ICmObject ConvertDummyToReal(int owningHvo, int virtflid, int hvoDummyId)
		{
			ICmObject ann = null;
			List<int> annotationItems = new List<int>(m_cache.GetVectorProperty(owningHvo, virtflid, true));
			int indexOfId = annotationItems.IndexOf(hvoDummyId);
			Debug.Assert(indexOfId >= 0);
			if (indexOfId >= 0)
			{
				ann = CmObject.ConvertDummyToReal(m_cache, hvoDummyId);
				Debug.Assert(ann != null);
				m_cache.VwCacheDaAccessor.CacheReplace(owningHvo,
					virtflid, indexOfId, indexOfId + 1, new int[] { ann.Hvo }, 1);
			}
			return ann;
		}

		private void tvTarget_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e)
		{
			CheckAssignBtnEnabling();
		}

		private void btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(FwApp.App, s_helpTopic);
		}

		#endregion Event Handlers
	}
}
