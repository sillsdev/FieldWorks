// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.FwCoreDlgs
{
	#region FileLocationChoice enumeration
	/// <summary>Options the user has for files that are not under the LinkedFiles folder</summary>
	public enum FileLocationChoice
	{
		/// <summary>Copy file to LinkedFiles folder</summary>
		Copy,
		/// <summary>Move file to LinkedFiles folder</summary>
		Move,
		/// <summary>Leave file in original folder</summary>
		Leave,
	}
	#endregion

	#region MoveOrCopyFilesDlg dialog
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This dialog is used whenever LangProject.LinkedFilesRootDir is changed,
	/// to find out what the user wants to do with the files that exist in the old location,
	/// and whenever a file from outside LangProject.LinkedFilesRootDir is linked from this project
	/// to find out whether the user wants to move or copy into LangProject.LinkedFilesRootDir or to leave the file where it is.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class MoveOrCopyFilesDlg : Form, IFWDisposable
	{
		#region Member variables
		private FileLocationChoice m_choice;
		private string m_sHelpTopic;

		private IHelpTopicProvider m_helpTopicProvider;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public MoveOrCopyFilesDlg()
		{
			InitializeComponent();
		}
		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnCopy control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_btnCopy_Click(object sender, EventArgs e)
		{
			m_choice = FileLocationChoice.Copy;
			DialogResult = DialogResult.OK;
			Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnMove control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_btnMove_Click(object sender, EventArgs e)
		{
			m_choice = FileLocationChoice.Move;
			DialogResult = DialogResult.OK;
			Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnLeave control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_btnLeave_Click(object sender, EventArgs e)
		{
			m_choice = FileLocationChoice.Leave;
			DialogResult = DialogResult.OK;
			Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnHelp control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, m_sHelpTopic);
		}
		#endregion Event handlers

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the header and message before showing the dialog used whenever LangProject.LinkedFilesRootDir is changed
		/// to find out what the user wants to do with the files that exist in the old location.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Initialize(int cFiles, string sOldDir, string sNewDir, IHelpTopicProvider helpTopicProvider)
		{
			CheckDisposed();
			ControlBox = false;	// don't show Cancel button in the titlebar
			m_msgText.Text = string.Format(FwCoreDlgs.ksMoveOrCopyToNewDir, cFiles);
			m_msgOldDir.Text = string.Format(FwCoreDlgs.ksPreviousFolder, ShortenMyDocsPath(sOldDir));
			m_msgNewDir.Text = string.Format(FwCoreDlgs.ksNewFolder, ShortenMyDocsPath(sNewDir));

			SetupHelp(helpTopicProvider, "khtpMoveOrCopyFiles");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the header and message before showing the dialog used
		/// whenever a file from outside LangProject.LinkedFilesRootDir is linked from this project
		/// to find out whether the user wants to move or copy into LangProject.LinkedFilesRootDir or to leave the file where it is.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Initialize2(string sFilename, string sRootDir, IHelpTopicProvider helpTopicProvider, bool isLocal)
		{
			CheckDisposed();
			m_msgText.Text = string.Format(FwCoreDlgs.ksMoveOrCopyFileToLinkedFilesDir);
			m_msgOldDir.Text = string.Format(FwCoreDlgs.ksLinkedFilesFolder, ShortenMyDocsPath(sRootDir));

			// Adjust dialog size to conceal empty space left by invisible m_msgNewDir.
			// Buttons are anchored to the bottom, so they'll move automatically.
			m_msgNewDir.Visible = false;
			var dy = m_msgNewDir.Height;
			var szNew = new Size(Size.Width, Size.Height - dy);
			MaximumSize = szNew;
			MinimumSize = szNew;
			Size = szNew;

			// These become singular in wording instead of plural.
			Text = FwCoreDlgs.ksMoveOrCopyFile;
			m_btnCopy.Text = FwCoreDlgs.ksCopyFile;
			m_btnMove.Text = FwCoreDlgs.ksMoveFile;
			m_btnLeave.Text = FwCoreDlgs.ksLeaveFile;
			m_btnLeave.Enabled = isLocal;

			SetupHelp(helpTopicProvider, "khtpMoveOrCopyFile");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up the help topic provider and topic.
		/// </summary>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="sHelpTopic">The URL of the help topic.</param>
		/// ------------------------------------------------------------------------------------
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the given folder path is in the "My Documents" folder, trim the "My Documents" portion off the path.
		/// </summary>
		/// <param name="sDir">The name of the path to try to shorten.</param>
		/// <returns>The (potentially) trimmed path name</returns>
		/// <remarks>REVIEW (Hasso) 2015.08: this might be better in the controller</remarks>
		/// ------------------------------------------------------------------------------------
		private static string ShortenMyDocsPath(string sDir)
		{
			var sMyDocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			if (sDir.ToLowerInvariant().StartsWith(sMyDocs.ToLowerInvariant()))
			{
				var idx = sMyDocs.LastIndexOf(Path.DirectorySeparatorChar);
				return sDir.Substring(idx + 1);
			}
			return sDir;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the choice the user made (copy, move or leave).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FileLocationChoice Choice
		{
			get { CheckDisposed(); return m_choice; }
		}

		#region IFWDisposable Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check whether this dialog has already been disposed.  If so, throw a fit.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(string.Format("'{0}' in use after being disposed.", GetType().Name));
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fix so that 120DPI fonts don't push the buttons to the bottom of the dialog.  (See comment added to LT-8968.)
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
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
	#endregion
}
