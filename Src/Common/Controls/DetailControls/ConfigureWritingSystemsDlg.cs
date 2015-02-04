using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// The configure writing systems dialog that is used by MultiStringSlice.
	/// </summary>
	public class ConfigureWritingSystemsDlg : Form
	{
		private const string HelpTopic = "khtpChoose-DataTreeWritingSystems";

		private readonly IHelpTopicProvider m_helpTopicProvider;

		private Label m_wsLabel;
		private Button m_okButton;
		private Button m_cancelButton;
		private Button m_helpButton;

		private CheckedListBox m_wsListBox;
		private HelpProvider m_helpProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="ConfigureWritingSystemsDlg"/> class.
		/// </summary>
		/// <param name="allWss">All writing systems to display.</param>
		/// <param name="selectedWss">The selected writing systems.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		public ConfigureWritingSystemsDlg(IEnumerable<WritingSystem> allWss, IEnumerable<WritingSystem> selectedWss,
			IHelpTopicProvider helpTopicProvider)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			foreach (WritingSystem curWs in allWss)
				m_wsListBox.Items.Add(curWs, selectedWss.Contains(curWs));
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
		public IEnumerable<WritingSystem> SelectedWritingSystems
		{
			get
			{
				return m_wsListBox.CheckedItems.Cast<WritingSystem>();
			}
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConfigureWritingSystemsDlg));
			this.m_wsLabel = new System.Windows.Forms.Label();
			this.m_okButton = new System.Windows.Forms.Button();
			this.m_cancelButton = new System.Windows.Forms.Button();
			this.m_helpButton = new System.Windows.Forms.Button();
			this.m_helpProvider = new System.Windows.Forms.HelpProvider();
			this.m_wsListBox = new System.Windows.Forms.CheckedListBox();
			this.SuspendLayout();
			//
			// m_wsLabel
			//
			resources.ApplyResources(this.m_wsLabel, "m_wsLabel");
			this.m_wsLabel.Name = "m_wsLabel";
			//
			// m_okButton
			//
			this.m_okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			resources.ApplyResources(this.m_okButton, "m_okButton");
			this.m_okButton.Name = "m_okButton";
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
			this.m_wsListBox.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.m_wsListBox_ItemCheck);
			//
			// ConfigureWritingSystemsDlg
			//
			this.AcceptButton = this.m_okButton;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.m_cancelButton;
			this.CausesValidation = false;
			this.Controls.Add(this.m_wsLabel);
			this.Controls.Add(this.m_wsListBox);
			this.Controls.Add(this.m_helpButton);
			this.Controls.Add(this.m_okButton);
			this.Controls.Add(this.m_cancelButton);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ConfigureWritingSystemsDlg";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);

		}
		#endregion

		private void m_helpButton_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, HelpTopic);
		}

		private void m_wsListBox_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			m_okButton.Enabled = e.NewValue == CheckState.Checked || m_wsListBox.CheckedIndices.Count > 1
				|| !m_wsListBox.CheckedIndices.Contains(e.Index);
		}
	}
}
