// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FwCoreDlgs.BackupRestore
{
	/// <summary>
	/// If the user selects a location for FW backups which is not in the default location this dialog asks if
	/// they want to change the default location.
	/// </summary>
	public partial class ChangeDefaultBackupDir : Form
	{
		#region Member Variables

		private IHelpTopicProvider m_helpTopicProvider;
		private const string sHelpTopic = "khtpChangeDefaultBackupDir";

		#endregion

		/// <summary />
		private ChangeDefaultBackupDir()
		{
			InitializeComponent();
		}

		/// <summary />
		public ChangeDefaultBackupDir(IHelpTopicProvider helpTopicProvider)
			: this()
		{
			m_helpTopicProvider = helpTopicProvider;
		}

		private void button_Help_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, sHelpTopic);
		}
	}
}