// Copyright (c) 2010-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs.Controls;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.Notebook
{
	public class InsertRecordDlg : Form, IFlexComponent
	{
		private FwTextBox m_titleTextBox;
		private Label m_titleLabel;
		private Label m_typeLabel;
		private Button m_btnHelp;
		private Button m_btnCancel;
		private Button m_btnOK;
		private HelpProvider m_helpProvider;
		private TreeCombo m_typeCombo;
		private LcmCache m_cache;
		private IHelpTopicProvider m_helpTopicProvider;
		private PossibilityListPopupTreeManager m_typePopupTreeManager;
		private ICmObject m_owner;
		private string m_helpTopic = "khtpNoHelpTopic";

		public InsertRecordDlg()
		{
			InitializeComponent();
			AccessibleName = GetType().Name;
		}

		/// <summary>
		/// Gets or sets the help topic.
		/// </summary>
		public string HelpTopic
		{
			get
			{
				return m_helpTopic;
			}

			set
			{
				m_helpTopic = value;
				m_helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(value));
			}
		}

		public IRnGenericRec NewRecord { get; private set; }

		#region Dispose

		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
			if (IsDisposed)
			{
				// No need to run more than once.
				return;
			}

			if (disposing)
			{
				m_typePopupTreeManager?.Dispose();
			}
			m_typePopupTreeManager = null;

			base.Dispose(disposing);
		}
		#endregion Dispose

		public void SetDlgInfo(LcmCache cache, ICmObject owner, ITsString tssTitle = null)
		{
			m_cache = cache;
			m_owner = owner;

			m_helpTopic = "khtpDataNotebook-InsertRecordDlg";

			m_helpTopicProvider = PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider);
			if (m_helpTopicProvider != null) // Will be null when running tests
			{
				m_helpProvider.HelpNamespace = m_helpTopicProvider.HelpFile;
				m_helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(m_helpTopic));
				m_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			}
			IVwStylesheet stylesheet = FwUtils.StyleSheetFromPropertyTable(PropertyTable);
			m_titleTextBox.StyleSheet = stylesheet;
			m_titleTextBox.WritingSystemFactory = m_cache.WritingSystemFactory;
			m_titleTextBox.WritingSystemCode = m_cache.DefaultAnalWs;
			AdjustControlAndDialogHeight(m_titleTextBox, m_titleTextBox.PreferredHeight);

			m_typeCombo.StyleSheet = stylesheet;
			m_typeCombo.WritingSystemFactory = m_cache.WritingSystemFactory;
			m_typeCombo.WritingSystemCode = m_cache.DefaultAnalWs;
			AdjustControlAndDialogHeight(m_typeCombo, m_typeCombo.PreferredHeight);

			var recTypes = m_cache.LanguageProject.ResearchNotebookOA.RecTypesOA;
			m_typePopupTreeManager = new PossibilityListPopupTreeManager(m_typeCombo, m_cache, PropertyTable, Publisher, recTypes, cache.DefaultAnalWs, false, this);
			m_typePopupTreeManager.LoadPopupTree(m_cache.ServiceLocator.GetObject(RnResearchNbkTags.kguidRecObservation).Hvo);
			// Ensure that we start out focused in the Title text box.  See FWR-2731.
			m_titleTextBox.Select();
			if (tssTitle != null)
			{
				m_titleTextBox.Tss = tssTitle;
			}
		}

		private void AdjustControlAndDialogHeight(Control control, int preferredHeight)
		{
			var tbNewHeight = Math.Max(preferredHeight, control.Height);
			FontHeightAdjuster.GrowDialogAndAdjustControls(this, tbNewHeight - control.Height, control);
			control.Height = tbNewHeight;
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			if (DialogResult == DialogResult.OK)
			{
				if (string.IsNullOrEmpty(m_titleTextBox.Text))
				{
					e.Cancel = true;
					MessageBox.Show(this, LanguageExplorerControls.ksFillInTitle, LanguageExplorerControls.ksMissingInformation, MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
				using (new WaitCursor(this))
				{
					UndoableUnitOfWorkHelper.Do(LanguageExplorerControls.ksUndoCreateRecord, LanguageExplorerControls.ksRedoCreateRecord, m_cache.ActionHandlerAccessor, () =>
					{
						var recFactory = m_cache.ServiceLocator.GetInstance<IRnGenericRecFactory>();
						var posHvo = ((HvoTreeNode)m_typeCombo.SelectedNode).Hvo;
						var type = m_cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(posHvo);
						switch (m_owner.ClassID)
						{
							case RnResearchNbkTags.kClassId:
								NewRecord = recFactory.Create((IRnResearchNbk)m_owner, m_titleTextBox.Tss, type);
								break;
							case RnGenericRecTags.kClassId:
								NewRecord = recFactory.Create((IRnGenericRec)m_owner, m_titleTextBox.Tss, type);
								break;
						}
					});
				}
			}
			base.OnClosing(e);
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, m_helpTopic);
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InsertRecordDlg));
			this.m_titleTextBox = new SIL.FieldWorks.FwCoreDlgs.Controls.FwTextBox();
			this.m_typeCombo = new LanguageExplorer.Controls.TreeCombo();
			this.m_titleLabel = new System.Windows.Forms.Label();
			this.m_typeLabel = new System.Windows.Forms.Label();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_helpProvider = new HelpProvider();
			((System.ComponentModel.ISupportInitialize)(this.m_titleTextBox)).BeginInit();
			this.SuspendLayout();
			//
			// m_titleTextBox
			//
			this.m_titleTextBox.AdjustStringHeight = false;
			this.m_titleTextBox.controlID = null;
			resources.ApplyResources(this.m_titleTextBox, "m_titleTextBox");
			this.m_titleTextBox.HasBorder = true;
			this.m_titleTextBox.Name = "m_titleTextBox";
			//
			// m_typeCombo
			//
			this.m_typeCombo.AdjustStringHeight = false;
			this.m_typeCombo.HasBorder = true;
			resources.ApplyResources(this.m_typeCombo, "m_typeCombo");
			this.m_typeCombo.Name = "m_typeCombo";
			//
			// m_titleLabel
			//
			resources.ApplyResources(this.m_titleLabel, "m_titleLabel");
			this.m_titleLabel.Name = "m_titleLabel";
			//
			// m_typeLabel
			//
			resources.ApplyResources(this.m_typeLabel, "m_typeLabel");
			this.m_typeLabel.Name = "m_typeLabel";
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_btnCancel
			//
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btnCancel.Name = "m_btnCancel";
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			this.m_btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btnOK.Name = "m_btnOK";
			//
			// InsertRecordDlg
			//
			this.AcceptButton = this.m_btnOK;
			this.CancelButton = this.m_btnCancel;
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnOK);
			this.Controls.Add(this.m_typeLabel);
			this.Controls.Add(this.m_titleLabel);
			this.Controls.Add(this.m_typeCombo);
			this.Controls.Add(this.m_titleTextBox);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.Name = "InsertRecordDlg";
			this.ShowInTaskbar = false;
			((System.ComponentModel.ISupportInitialize)(this.m_titleTextBox)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

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
		}
		#endregion
	}
}