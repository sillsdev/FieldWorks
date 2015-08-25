using System;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.IText
{
	public class ComplexConcTagDlg : Form
	{
		const string s_helpTopic = "khtpComplexConcTagDlg";

		private Button m_btnHelp;
		private Button m_btnCancel;
		private Button m_btnOK;
		private HelpProvider m_helpProvider;
		private TreeCombo m_tagComboBox;

		private FdoCache m_cache;
		private IHelpTopicProvider m_helpTopicProvider;
		private ComplexConcTagNode m_node;
		private PossibilityComboController m_posPopupTreeManager;

		public ComplexConcTagDlg()
		{
			InitializeComponent();
			AccessibleName = GetType().Name;
		}

		public void SetDlgInfo(FdoCache cache, IPropertyTable propertyTable, IPublisher publisher, ComplexConcTagNode node)
		{
			m_cache = cache;
			m_node = node;

			m_tagComboBox.WritingSystemFactory = m_cache.LanguageWritingSystemFactoryAccessor;

			m_posPopupTreeManager = new PossibilityComboController(m_tagComboBox,
									m_cache,
									m_cache.LanguageProject.TextMarkupTagsOA,
									m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle,
									false,
									propertyTable,
									publisher,
									propertyTable.GetValue<Form>("window"));

			m_posPopupTreeManager.LoadPopupTree(m_node.Tag != null ? m_node.Tag.Hvo : 0);

			m_helpTopicProvider = propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider");

			m_helpProvider.HelpNamespace = m_helpTopicProvider.HelpFile;
			m_helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(s_helpTopic));
			m_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			var node = (HvoTreeNode) m_tagComboBox.SelectedNode;
			if (node.Hvo != 0)
			{
				ICmPossibility tag = m_cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(node.Hvo);
				if (tag.OwningPossibility != null)
				{
					m_node.Tag = tag;
					DialogResult = DialogResult.OK;
				}
				else
				{
					MessageBox.Show(this, ITextStrings.ksInvalidTagMsg, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			else
			{
				DialogResult = DialogResult.OK;
			}
		}

		private void m_btnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, s_helpTopic);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ComplexConcTagDlg));
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_helpProvider = new System.Windows.Forms.HelpProvider();
			this.m_tagComboBox = new SIL.FieldWorks.Common.Widgets.TreeCombo();
			this.SuspendLayout();
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.UseVisualStyleBackColor = true;
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_btnCancel
			//
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.Name = "m_btnCancel";
			this.m_btnCancel.UseVisualStyleBackColor = true;
			this.m_btnCancel.Click += new System.EventHandler(this.m_btnCancel_Click);
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			this.m_btnOK.Name = "m_btnOK";
			this.m_btnOK.UseVisualStyleBackColor = true;
			this.m_btnOK.Click += new System.EventHandler(this.m_btnOK_Click);
			//
			// m_tagComboBox
			//
			this.m_tagComboBox.AdjustStringHeight = true;
			this.m_tagComboBox.BackColor = System.Drawing.SystemColors.Window;
			this.m_tagComboBox.DropDownWidth = 120;
			this.m_tagComboBox.DroppedDown = false;
			this.m_tagComboBox.HasBorder = true;
			resources.ApplyResources(this.m_tagComboBox, "m_tagComboBox");
			this.m_tagComboBox.Name = "m_tagComboBox";
			this.m_helpProvider.SetShowHelp(this.m_tagComboBox, ((bool)(resources.GetObject("m_tagComboBox.ShowHelp"))));
			this.m_tagComboBox.UseVisualStyleBackColor = true;
			//
			// ComplexConcTagDlg
			//
			this.AcceptButton = this.m_btnOK;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btnCancel;
			this.Controls.Add(this.m_tagComboBox);
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnOK);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ComplexConcTagDlg";
			this.m_helpProvider.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);

		}

		#endregion
	}
}
