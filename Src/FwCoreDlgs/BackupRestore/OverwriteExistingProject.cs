// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FwCoreDlgs.BackupRestore
{
	/// <summary>
	/// This dialog is used to inform the user that they are about to restore from backup
	/// a project over an existing project and give them the opportunity to backup the project
	/// before doing the restore.
	/// </summary>
	public partial class OverwriteExistingProject : Form
	{

		#region Member Variables

		private IHelpTopicProvider m_helpTopicProvider;
		private const string sHelpTopic = "khtpOverwriteExistingWarning";

		#endregion

		/// <summary />
		private OverwriteExistingProject()
		{
			InitializeComponent();
		}

		/// <summary />
		public OverwriteExistingProject(string projectPath, IHelpTopicProvider helpTopicProvider)
			: this()
		{
			m_helpTopicProvider = helpTopicProvider;
			m_lblInfo.Text = string.Format(m_lblInfo.Text, projectPath);
		}

		/// <summary />
		public bool BackupBeforeOverwriting => m_checkbox_BackupFirst.Checked;

		private void m_btnHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, sHelpTopic);
		}
	}
}