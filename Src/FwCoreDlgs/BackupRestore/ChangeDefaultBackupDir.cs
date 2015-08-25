using System;
using System.Windows.Forms;
using SIL.CoreImpl;
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

		/// <summary>
		/// Constructor
		/// </summary>
		private ChangeDefaultBackupDir()
		{
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ChangeDefaultBackupDir"/> class.
		/// </summary>
		/// <param name="helpTopicProvider"></param>
		/// ------------------------------------------------------------------------------------
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
