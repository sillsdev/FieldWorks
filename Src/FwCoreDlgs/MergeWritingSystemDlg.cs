using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FwCoreDlgs.BackupRestore;
using XCore;
using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// The merge writing systems dialog.
	/// </summary>
	public class MergeWritingSystemDlg : Form
	{
		private const string HelpTopic = "khtpProjPropsMergeWS";

		private readonly CoreWritingSystemDefinition m_ws;
		private FdoCache m_cache;
		private readonly IHelpTopicProvider m_helpTopicProvider;

		private Label m_wsLabel;
		private PictureBox m_infoPictureBox;
		private Button m_mergeButton;
		private Button m_cancelButton;
		private Button m_helpButton;

		private ListBox m_wsListBox;
		private Label m_mergeLabel;
		private Button m_backupButton;
		private HelpProvider m_helpProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="MergeWritingSystemDlg"/> class.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "infoIcon is a reference")]
		public MergeWritingSystemDlg(FdoCache cache, CoreWritingSystemDefinition ws, IEnumerable<CoreWritingSystemDefinition> wss, IHelpTopicProvider helpTopicProvider)
		{
			m_cache = cache;
			m_ws = ws;

			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			Icon infoIcon = SystemIcons.Information;
			m_infoPictureBox.Image = infoIcon.ToBitmap();
			m_infoPictureBox.Size = infoIcon.Size;

			foreach (CoreWritingSystemDefinition curWs in wss.Except(new[] { ws }))
				m_wsListBox.Items.Add(curWs);
			m_wsListBox.SelectedIndex = 0;

			m_helpTopicProvider = helpTopicProvider;

			if (m_helpTopicProvider != null) // m_helpTopicProvider could be null for testing
			{
				m_helpProvider = new HelpProvider();
				m_helpProvider.HelpNamespace = m_helpTopicProvider.HelpFile;
				m_helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(HelpTopic));
				m_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			}
		}

		/// <summary>
		/// Gets the selected writing system.
		/// </summary>
		/// <value>The selected writing system.</value>
		public CoreWritingSystemDefinition SelectedWritingSystem
		{
			get
			{
				return (CoreWritingSystemDefinition) m_wsListBox.SelectedItem;
			}
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MergeWritingSystemDlg));
			this.m_wsLabel = new System.Windows.Forms.Label();
			this.m_infoPictureBox = new System.Windows.Forms.PictureBox();
			this.m_mergeButton = new System.Windows.Forms.Button();
			this.m_cancelButton = new System.Windows.Forms.Button();
			this.m_helpButton = new System.Windows.Forms.Button();
			this.m_wsListBox = new System.Windows.Forms.ListBox();
			this.m_mergeLabel = new System.Windows.Forms.Label();
			this.m_helpProvider = new System.Windows.Forms.HelpProvider();
			this.m_backupButton = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.m_infoPictureBox)).BeginInit();
			this.SuspendLayout();
			//
			// m_wsLabel
			//
			resources.ApplyResources(this.m_wsLabel, "m_wsLabel");
			this.m_wsLabel.Name = "m_wsLabel";
			//
			// m_infoPictureBox
			//
			resources.ApplyResources(this.m_infoPictureBox, "m_infoPictureBox");
			this.m_infoPictureBox.Name = "m_infoPictureBox";
			this.m_infoPictureBox.TabStop = false;
			//
			// m_mergeButton
			//
			this.m_mergeButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			resources.ApplyResources(this.m_mergeButton, "m_mergeButton");
			this.m_mergeButton.Name = "m_mergeButton";
			//
			// m_cancelButton
			//
			this.m_cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.m_cancelButton, "m_cancelButton");
			this.m_cancelButton.Name = "m_cancelButton";
			//
			// m_helpButton
			//
			resources.ApplyResources(this.m_helpButton, "m_helpButton");
			this.m_helpButton.Name = "m_helpButton";
			this.m_helpButton.Click += new System.EventHandler(this.m_helpButton_Click);
			//
			// m_wsListBox
			//
			this.m_wsListBox.FormattingEnabled = true;
			resources.ApplyResources(this.m_wsListBox, "m_wsListBox");
			this.m_wsListBox.Name = "m_wsListBox";
			this.m_wsListBox.SelectedIndexChanged += new System.EventHandler(this.m_wsListBox_SelectedIndexChanged);
			//
			// m_mergeLabel
			//
			resources.ApplyResources(this.m_mergeLabel, "m_mergeLabel");
			this.m_mergeLabel.Name = "m_mergeLabel";
			//
			// m_backupButton
			//
			resources.ApplyResources(this.m_backupButton, "m_backupButton");
			this.m_backupButton.Name = "m_backupButton";
			this.m_backupButton.UseVisualStyleBackColor = true;
			this.m_backupButton.Click += new System.EventHandler(this.m_backupButton_Click);
			//
			// MergeWritingSystemDlg
			//
			this.AcceptButton = this.m_mergeButton;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_cancelButton;
			this.CausesValidation = false;
			this.Controls.Add(this.m_backupButton);
			this.Controls.Add(this.m_mergeLabel);
			this.Controls.Add(this.m_wsListBox);
			this.Controls.Add(this.m_helpButton);
			this.Controls.Add(this.m_wsLabel);
			this.Controls.Add(this.m_infoPictureBox);
			this.Controls.Add(this.m_mergeButton);
			this.Controls.Add(this.m_cancelButton);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "MergeWritingSystemDlg";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			((System.ComponentModel.ISupportInitialize)(this.m_infoPictureBox)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		private void m_helpButton_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, HelpTopic);
		}

		private void m_wsListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			m_mergeLabel.Text = string.Format(FwCoreDlgs.kstidMergeWritingSystems, m_ws, SelectedWritingSystem);
		}

		private void m_backupButton_Click(object sender, EventArgs e)
		{
			using (var dlg = new BackupProjectDlg(m_cache, FwUtils.ksFlexAbbrev, m_helpTopicProvider))
				dlg.ShowDialog(this);

		}
	}
}
