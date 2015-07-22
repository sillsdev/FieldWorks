using System;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	public class InsertRecordDlg : Form, IFWDisposable
	{
		private FwTextBox m_titleTextBox;
		private Label m_titleLabel;
		private Label m_typeLabel;
		private Button m_btnHelp;
		private Button m_btnCancel;
		private Button m_btnOK;
		private HelpProvider m_helpProvider;
		private TreeCombo m_typeCombo;

		private FdoCache m_cache;
		private IHelpTopicProvider m_helpTopicProvider;
		private PossibilityListPopupTreeManager m_typePopupTreeManager;
		private IRnGenericRec m_newRecord;
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
		/// <value>The help topic.</value>
		public string HelpTopic
		{
			get
			{
				CheckDisposed();
				return m_helpTopic;
			}

			set
			{
				CheckDisposed();
				m_helpTopic = value;
				m_helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(value));
			}
		}

		public IRnGenericRec NewRecord
		{
			get
			{
				CheckDisposed();
				return m_newRecord;
			}
		}

		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(string.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		public void SetDlgInfo(FdoCache cache, Mediator mediator, PropertyTable propertyTable, ICmObject owner)
		{
			CheckDisposed();

			m_cache = cache;
			m_owner = owner;

			m_helpTopic = "khtpDataNotebook-InsertRecordDlg";

			m_helpTopicProvider = propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider");
			if (m_helpTopicProvider != null) // Will be null when running tests
			{
				m_helpProvider.HelpNamespace = m_helpTopicProvider.HelpFile;
				m_helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(m_helpTopic));
				m_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			}

			IVwStylesheet stylesheet = FontHeightAdjuster.StyleSheetFromPropertyTable(propertyTable);
			m_titleTextBox.StyleSheet = stylesheet;
			m_titleTextBox.WritingSystemFactory = m_cache.WritingSystemFactory;
			m_titleTextBox.WritingSystemCode = m_cache.DefaultAnalWs;
			AdjustControlAndDialogHeight(m_titleTextBox, m_titleTextBox.PreferredHeight);

			m_typeCombo.StyleSheet = stylesheet;
			m_typeCombo.WritingSystemFactory = m_cache.WritingSystemFactory;
			m_typeCombo.WritingSystemCode = m_cache.DefaultAnalWs;
			AdjustControlAndDialogHeight(m_typeCombo, m_typeCombo.PreferredHeight);

			ICmPossibilityList recTypes = m_cache.LanguageProject.ResearchNotebookOA.RecTypesOA;
			m_typePopupTreeManager = new PossibilityListPopupTreeManager(m_typeCombo, m_cache, mediator, propertyTable,
				recTypes, cache.DefaultAnalWs, false, this);
			m_typePopupTreeManager.LoadPopupTree(m_cache.ServiceLocator.GetObject(RnResearchNbkTags.kguidRecObservation).Hvo);
			// Ensure that we start out focused in the Title text box.  See FWR-2731.
			m_titleTextBox.Select();
		}

		public void SetDlgInfo(FdoCache cache, Mediator mediator, PropertyTable propertyTable, ICmObject owner, ITsString tssTitle)
		{
			SetDlgInfo(cache, mediator, propertyTable, owner);
			m_titleTextBox.Tss = tssTitle;
		}

		private void AdjustControlAndDialogHeight(Control control, int preferredHeight)
		{
			int tbNewHeight = Math.Max(preferredHeight, control.Height);
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
					MessageBox.Show(this, LexTextControls.ksFillInTitle, LexTextControls.ksMissingInformation,
						MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}

				using (new WaitCursor(this))
				{
					UndoableUnitOfWorkHelper.Do(LexTextControls.ksUndoCreateRecord, LexTextControls.ksRedoCreateRecord, m_cache.ActionHandlerAccessor, () =>
					{
						var recFactory = m_cache.ServiceLocator.GetInstance<IRnGenericRecFactory>();
						int posHvo = ((HvoTreeNode) m_typeCombo.SelectedNode).Hvo;
						ICmPossibility type = m_cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(posHvo);
						switch (m_owner.ClassID)
						{
							case RnResearchNbkTags.kClassId:
								m_newRecord = recFactory.Create((IRnResearchNbk) m_owner, m_titleTextBox.Tss, type);
								break;

							case RnGenericRecTags.kClassId:
								m_newRecord = recFactory.Create((IRnGenericRec) m_owner, m_titleTextBox.Tss, type);
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
			this.m_titleTextBox = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.m_typeCombo = new SIL.FieldWorks.Common.Widgets.TreeCombo();
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
	}
}
