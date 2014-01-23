// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: OverwriteExistingProject.cs
// Responsibility: FW Team
// --------------------------------------------------------------------------------------------
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using XCore;

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

		/// <summary>
		/// Constructor
		/// </summary>
		private OverwriteExistingProject()
		{
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="OverwriteExistingProject"/> class.
		/// </summary>
		/// <param name="projectPath">The project path.</param>
		/// <param name="helpTopicProvider"></param>
		/// ------------------------------------------------------------------------------------
		public OverwriteExistingProject(string projectPath, IHelpTopicProvider helpTopicProvider) : this()
		{
			m_helpTopicProvider = helpTopicProvider;
			m_lblInfo.Text = string.Format(m_lblInfo.Text, projectPath);
		}

		/// <summary>
		///
		/// </summary>
		public bool BackupBeforeOverwritting
		{
			get { return m_checkbox_BackupFirst.Checked; }
		}

		private void m_btnHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, sHelpTopic);
		}
	}
}
