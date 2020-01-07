// Copyright (c) 2008-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// This dialog is used whenever LangProject.LinkedFilesRootDir is changed,
	/// to find out what the user wants to do with the files that exist in the old location,
	/// and whenever a file from outside LangProject.LinkedFilesRootDir is linked from this project
	/// to find out whether the user wants to move or copy into LangProject.LinkedFilesRootDir or to leave the file where it is.
	/// </summary>
	public partial class MoveOrCopyFilesDlg : Form
	{
		private string m_sHelpTopic;
		private IHelpTopicProvider m_helpTopicProvider;

		/// <summary />
		public MoveOrCopyFilesDlg()
		{
			InitializeComponent();
		}

		#region Event handlers

		/// <summary>
		/// Handles the Click event of the m_btnCopy control.
		/// </summary>
		private void m_btnCopy_Click(object sender, EventArgs e)
		{
			Choice = FileLocationChoice.Copy;
			DialogResult = DialogResult.OK;
			Close();
		}

		/// <summary>
		/// Handles the Click event of the m_btnMove control.
		/// </summary>
		private void m_btnMove_Click(object sender, EventArgs e)
		{
			Choice = FileLocationChoice.Move;
			DialogResult = DialogResult.OK;
			Close();
		}

		/// <summary>
		/// Handles the Click event of the m_btnLeave control.
		/// </summary>
		private void m_btnLeave_Click(object sender, EventArgs e)
		{
			Choice = FileLocationChoice.Leave;
			DialogResult = DialogResult.OK;
			Close();
		}

		/// <summary>
		/// Handles the Click event of the m_btnHelp control.
		/// </summary>
		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, m_sHelpTopic);
		}
		#endregion Event handlers

		/// <summary>
		/// Initialize the header and message before showing the dialog used whenever LangProject.LinkedFilesRootDir is changed
		/// to find out what the user wants to do with the files that exist in the old location.
		/// </summary>
		public void Initialize(int cFiles, string sOldDir, string sNewDir, IHelpTopicProvider helpTopicProvider)
		{
			ControlBox = false; // don't show Cancel button in the titlebar
			m_msgText.Text = string.Format(FwCoreDlgs.ksMoveOrCopyToNewDir, cFiles);
			m_msgOldDir.Text = string.Format(FwCoreDlgs.ksPreviousFolder, FwUtils.ShortenMyDocsPath(sOldDir));
			m_msgNewDir.Text = string.Format(FwCoreDlgs.ksNewFolder, FwUtils.ShortenMyDocsPath(sNewDir));
			SetupHelp(helpTopicProvider, "khtpMoveOrCopyFiles");
		}

		/// <summary>
		/// Initialize the header and message before showing the dialog used
		/// whenever a file from outside LangProject.LinkedFilesRootDir is linked from this project
		/// to find out whether the user wants to move or copy into LangProject.LinkedFilesRootDir or to leave the file where it is.
		/// </summary>
		public void Initialize(string sRootDir, IHelpTopicProvider helpTopicProvider)
		{
			m_msgText.Text = string.Format(FwCoreDlgs.ksMoveOrCopyFilesToLinkedFilesDir);
			m_msgOldDir.Text = string.Format(FwCoreDlgs.ksLinkedFilesFolder, FwUtils.ShortenMyDocsPath(sRootDir));
			// Adjust dialog size to conceal empty space left by invisible m_msgNewDir.
			// Buttons are anchored to the bottom, so they'll move automatically.
			m_msgNewDir.Visible = false;
			var dy = m_msgNewDir.Height;
			var szNew = new Size(Size.Width, Size.Height - dy);
			MaximumSize = szNew;
			MinimumSize = szNew;
			Size = szNew;
			m_btnLeave.Enabled = true;
			SetupHelp(helpTopicProvider, "khtpMoveOrCopyFile");
		}

		/// <summary>
		/// Sets up the help topic provider and topic.
		/// </summary>
		private void SetupHelp(IHelpTopicProvider helpTopicProvider, string sHelpTopic)
		{
			m_helpTopicProvider = helpTopicProvider;
			m_sHelpTopic = sHelpTopic;
			if (m_helpTopicProvider != null)
			{
				m_helpProvider.HelpNamespace = Path.Combine(FwDirectoryFinder.CodeDirectory, m_helpTopicProvider.GetHelpString("UserHelpFile"));
				m_helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(m_sHelpTopic));
				m_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			}
		}

		/// <summary>
		/// Gets the choice the user made (copy, move or leave).
		/// </summary>
		internal FileLocationChoice Choice { get; private set; }

		/// <summary>
		/// Fix so that 120DPI fonts don't push the buttons to the bottom of the dialog.  (See comment added to LT-8968.)
		/// </summary>
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			var dy = ClientSize.Height - (m_btnMove.Location.Y + m_btnMove.Height);
			if (dy < 10)
			{
				var sz = new Size(Width, Height + 10 - dy);
				MaximumSize = sz;
				Size = sz;
				MinimumSize = sz;
			}
		}
	}
}