using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using System.Reflection;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Display message, with button for backing up the database.
	/// </summary>
	public partial class MergeToExistingWsDlg : Form
	{
		IApp m_app;
		Icon m_icon;

		// Help info
		IHelpTopicProvider m_helptopicProvider;
		string m_sHelpTopicKey;

		/// <summary>
		/// Constructor.
		/// </summary>
		public MergeToExistingWsDlg(IHelpTopicProvider helptopicProvider)
		{
			InitializeComponent();
			m_icon = SystemIcons.Exclamation;
			m_helptopicProvider = helptopicProvider;
			m_sHelpTopicKey = "khtpMergeToExistingWsDlg";
		}

		/// <summary>
		/// Set the message, and other data needed to handle backup.
		/// </summary>
		/// <param name="sMsg"></param>
		/// <param name="app">the application -- used for backup</param>
		public void Initialize(string sMsg, IApp app)
		{
			m_tbMessage.Text = sMsg;
			m_app = app;
		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.OK;
		}

		private void m_btnCancel_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
		}

		private void m_btnBackup_Click(object sender, EventArgs e)
		{
			if (m_app != null && m_app is IBackupDelegates && m_app is IHelpTopicProvider)
			{
				DIFwBackupDb backupSystem = FwBackupClass.Create();
				backupSystem.Init(m_app as IBackupDelegates, Handle.ToInt32());
				int nBkResult;
				nBkResult = backupSystem.UserConfigure(m_app, false);
				backupSystem.Close();
			}
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helptopicProvider, m_sHelpTopicKey);
		}

		/// <summary>
		/// Draw the icon we want when the dialog is displayed.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			if (m_icon != null)
				e.Graphics.DrawIcon(m_icon, new Rectangle(m_panelIcon.Location, new Size(32, 32)));
		}
	}
}