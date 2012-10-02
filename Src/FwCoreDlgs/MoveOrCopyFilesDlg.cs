using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;

using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	#region FileLocationChoice enumeration
	/// <summary>Options the user has for files that are not under the External Links folder</summary>
	public enum FileLocationChoice
	{
		/// <summary>Copy file to External Links folder</summary>
		Copy,
		/// <summary>Move file to External Links folder</summary>
		Move,
		/// <summary>Leave file in original folder</summary>
		Leave,
	}
	#endregion

	#region IFwMoveOrCopyFilesDlg interface
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface for MoveOrCopyFilesDlg
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[ComVisible(true)]
	[Guid("33E8B0FA-A1DA-445A-AA7C-BF18F2DE8B96")]
	public interface IFwMoveOrCopyFilesDlg
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the dialog as a modal dialog
		/// </summary>
		/// <returns>A DialogResult value: OK means move or copy, Cancel means leave</returns>
		/// ------------------------------------------------------------------------------------
		int ShowDlg();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize for a change in the external links root directory.
		/// </summary>
		/// <param name="cFiles">number of files affected</param>
		/// <param name="sOldDir">the old External Links Root Directory</param>
		/// <param name="sNewDir">the new External Links Root Directory</param>
		/// <param name="helpTopicProvider"></param>
		/// ------------------------------------------------------------------------------------
		void Initialize(int cFiles, string sOldDir, string sNewDir, IHelpTopicProvider helpTopicProvider);

		/// <summary>
		/// Initialize for adding a new file link.
		/// </summary>
		/// <param name="sFilename"></param>
		/// <param name="sRootDir"></param>
		/// <param name="helpTopicProvider"></param>
		void Initialize2(string sFilename, string sRootDir, IHelpTopicProvider helpTopicProvider);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// returns true if files are to be copied.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool CopyFiles();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// returns true if files are to be moved.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool MoveFiles();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// returns true if files are to be left where they are.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool LeaveFiles();
	}
	#endregion

	#region MoveOrCopyFilesDlg dialog
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This dialog is used whenever LangProject.ExtLinkRootDir is changed, to find out what
	/// the user wants to do with the files that exist in the old location.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[ProgId("FwCoreDlgs.MoveOrCopyFilesDlg")]
	// Key attribute to hide the "clutter" from System.Windows.Forms.Form
	[ClassInterface(ClassInterfaceType.None)]
	[GuidAttribute("B2FF6EA7-1071-4E92-8D91-1BF6860990B4")]
	[ComVisible(true)]
	public partial class MoveOrCopyFilesDlg : Form, IFwMoveOrCopyFilesDlg, IFWDisposable
	{
		#region Member variables
		private bool m_fCopyFiles = false;
		private bool m_fMoveFiles = false;
		private bool m_fLeaveFiles = false;
		private FileLocationChoice m_choice;
		private string m_sHelpTopic = null;

		private IHelpTopicProvider m_helpTopicProvider = null;
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
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_btnCopy_Click(object sender, EventArgs e)
		{
			m_fCopyFiles = true;
			m_fMoveFiles = false;
			m_fLeaveFiles = false;
			m_choice = FileLocationChoice.Copy;
			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnMove control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_btnMove_Click(object sender, EventArgs e)
		{
			m_fCopyFiles = false;
			m_fMoveFiles = true;
			m_fLeaveFiles = false;
			m_choice = FileLocationChoice.Move;
			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnLeave control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_btnLeave_Click(object sender, EventArgs e)
		{
			m_fCopyFiles = false;
			m_fMoveFiles = false;
			m_fLeaveFiles = true;
			m_choice = FileLocationChoice.Leave;
			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnHelp control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, m_sHelpTopic);
		}
		#endregion

		#region implementation of IFwMoveOrCopyFilesDlg
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the dialog as a modal dialog
		/// </summary>
		/// <returns>A DialogResult value</returns>
		/// ------------------------------------------------------------------------------------
		public int ShowDlg()
		{
			CheckDisposed();
			return (int)ShowDialog();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the header and message before showing the dialog.
		/// </summary>
		/// <param name="cFiles"></param>
		/// <param name="sOldDir"></param>
		/// <param name="sNewDir"></param>
		/// <param name="helpTopicProvider"></param>
		/// ------------------------------------------------------------------------------------
		public void Initialize(int cFiles, string sOldDir, string sNewDir,
			IHelpTopicProvider helpTopicProvider)
		{
			CheckDisposed();
			this.ControlBox = false;	// don't show Cancel button in the titlebar
			m_msgText.Text = String.Format(FwCoreDlgs.ksMoveOrCopyToNewDir, cFiles);
			m_msgOldDir.Text = String.Format(FwCoreDlgs.ksPreviousFolder,
				ShortenMyDocsPath(sOldDir));
			m_msgNewDir.Text = String.Format(FwCoreDlgs.ksNewFolder,
				ShortenMyDocsPath(sNewDir));

			SetupHelp(helpTopicProvider, "khtpMoveOrCopyFiles");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the header and message before showing the dialog.
		/// </summary>
		/// <param name="sFilename"></param>
		/// <param name="sRootDir"></param>
		/// <param name="helpTopicProvider"></param>
		/// ------------------------------------------------------------------------------------
		public void Initialize2(string sFilename, string sRootDir,
			IHelpTopicProvider helpTopicProvider)
		{
			CheckDisposed();
			m_msgText.Text = String.Format(FwCoreDlgs.ksMoveOrCopyFileToExtDir);
			m_msgOldDir.Text = String.Format(FwCoreDlgs.ksExternalLinksFolder,
				ShortenMyDocsPath(sRootDir));

			// Adjust button locations and dialog size to conceal empty space left by invisible
			// m_msgNewDir.
			m_msgNewDir.Visible = false;
			int dy = m_msgNewDir.Height + 10;
			m_btnCopy.Location = new Point(m_btnCopy.Location.X, m_btnCopy.Location.Y - dy);
			m_btnMove.Location = new Point(m_btnMove.Location.X, m_btnMove.Location.Y - dy);
			m_btnLeave.Location = new Point(m_btnLeave.Location.X, m_btnLeave.Location.Y - dy);
			m_btnHelp.Location = new Point(m_btnHelp.Location.X, m_btnHelp.Location.Y - dy);
			Size szNew = new Size(this.Size.Width, this.Size.Height - dy);
			this.MaximumSize = szNew;
			this.MinimumSize = szNew;
			this.Size = szNew;

			// These become singular in wording instead of plural.
			this.Text = FwCoreDlgs.ksMoveOrCopyFile;
			m_btnCopy.Text = FwCoreDlgs.ksCopyFile;
			m_btnMove.Text = FwCoreDlgs.ksMoveFile;
			m_btnLeave.Text = FwCoreDlgs.ksLeaveFile;

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
				this.m_helpProvider.HelpNamespace = Path.Combine(DirectoryFinder.FWCodeDirectory,
					m_helpTopicProvider.GetHelpString("UserHelpFile", 0));
				this.m_helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(m_sHelpTopic, 0));
				this.m_helpProvider.SetHelpNavigator(this, System.Windows.Forms.HelpNavigator.Topic);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the given folder path is in the "My Documents" folder, trim the "My Documents"
		/// portion off the path.
		/// </summary>
		/// <param name="sDir">The name of the path to try to shorten.</param>
		/// <returns>The (potentially) trimmed path name</returns>
		/// ------------------------------------------------------------------------------------
		private static string ShortenMyDocsPath(string sDir)
		{
			string sMyDocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			if (sDir.ToLowerInvariant().StartsWith(sMyDocs.ToLowerInvariant()))
			{
				int idx = sMyDocs.LastIndexOf(Path.DirectorySeparatorChar);
				return sDir.Substring(idx + 1);
			}
			else
			{
				return sDir;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the desired action after the dialog closes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool CopyFiles()
		{
			CheckDisposed();
			return m_fCopyFiles;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the desired action after the dialog closes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool MoveFiles()
		{
			CheckDisposed();
			return m_fMoveFiles;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the desired action after the dialog closes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool LeaveFiles()
		{
			CheckDisposed();
			return m_fLeaveFiles;
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
		#endregion

		#region IFWDisposable Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check whether this dialog has already been disposed.  If so, throw a fit.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}
		#endregion

		#region Static methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks to see whether the given file is located in the given root directory (or any
		/// subfolder of it) and if not prompts the user to allow FW to move, copy or leave the
		/// file.
		/// </summary>
		/// <param name="sFile">The fully-specified path name of the file.</param>
		/// <param name="sRootDir">The fully-specified path name of the external links root
		/// directory.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <returns>The fully specified path name of the file to use, which might be the same
		/// as the given path or it could be in its new location under the external links folder
		/// if the user elected to move or copy it.</returns>
		/// ------------------------------------------------------------------------------------
		public static string MoveCopyOrLeaveFile(string sFile, string sRootDir,
			IHelpTopicProvider helpTopicProvider)
		{
			// Check whether the file is found within the directory.  If so, just return.
			if (FileIsInExternalLinksFolder(sFile, sRootDir))
				return sFile;

			using (MoveOrCopyFilesDlg dlg = new MoveOrCopyFilesDlg())
			{
				dlg.Initialize2(sFile, sRootDir, helpTopicProvider);
				if (dlg.ShowDialog() != DialogResult.OK)
					return null;	// leave where it is.
				return PerformMoveCopyOrLeaveFile(sFile, sRootDir, dlg.Choice);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs the action the user requested: move, copy, or leave the file.
		/// </summary>
		/// <param name="sFile">The fully-specified path name of the file.</param>
		/// <param name="sRootDir">The fully-specified path name of the new target directory.
		/// </param>
		/// <param name="action">the action the user chose (copy, move or leave)</param>
		/// <returns>The fully-specified path name of the (possibly newly moved or copied)
		/// file</returns>
		/// ------------------------------------------------------------------------------------
		internal static string PerformMoveCopyOrLeaveFile(string sFile, string sRootDir,
			 FileLocationChoice action)
		{
			if (action == FileLocationChoice.Leave)
				return sFile; // use original location.

			string sNewFile = Path.Combine(sRootDir, Path.GetFileName(sFile));
			if (File.Exists(sNewFile))
			{
				if (MessageBox.Show(String.Format(FwCoreDlgs.ksAlreadyExists, sNewFile),
					FwCoreDlgs.kstidWarning, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) ==
					DialogResult.No)
				{
					return sFile;
				}
				try
				{
					File.Delete(sNewFile);
				}
				catch
				{
					MessageBox.Show(FwCoreDlgs.ksDeletePictureBeforeReplacingFile,
						FwCoreDlgs.ksCannotReplaceDisplayedPicture);
					return sNewFile;
				}
			}
			try
			{
				switch (action)
				{
					case FileLocationChoice.Move:
						File.Move(sFile, sNewFile);
						break;
					case FileLocationChoice.Copy:
						File.Copy(sFile, sNewFile);
						break;
				}
				return sNewFile;
			}
			catch (Exception e)
			{
				string sAction = (action == FileLocationChoice.Copy ? "copy" : "mov");
				Logger.WriteEvent(
					string.Format("Error {0}ing file '{1}' to '{2}'", sAction, sFile, sNewFile));

				Logger.WriteError(e);
				return sFile;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the given file is located in the given root directory (or any
		/// subfolder of it).
		/// </summary>
		/// <param name="sFile">The fully-specified path name of the file.</param>
		/// <param name="sRootDir">The fully-specified path name of the external links root
		/// directory.</param>
		/// <returns><c>true</c> if the given file is located in the given root directory.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		internal static bool FileIsInExternalLinksFolder(string sFile, string sRootDir)
		{
			// Check whether the file is found within the directory.  If so, just return.
			if (sFile.ToLowerInvariant().StartsWith(sRootDir.ToLowerInvariant()))
			{
				int cchDir = sRootDir.Length;
				if (cchDir > 0 && sRootDir[cchDir - 1] == Path.DirectorySeparatorChar)
					return true;
				if (sFile.Length > cchDir && sFile[cchDir] == Path.DirectorySeparatorChar)
					return true;
			}
			return false;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fix so that 120DPI fonts don't push the buttons to the bottom of
		/// the dialog.  (See comment added to LT-8968.)
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			int dy = ClientSize.Height - (m_btnMove.Location.Y + m_btnMove.Height);
			if (dy < 10)
			{
				int height = this.Height + 10 - dy;
				Size sz = new Size(this.Width, this.Height + 10 - dy);
				this.MaximumSize = sz;
				this.Size = sz;
				this.MinimumSize = sz;
			}
		}
	}
	#endregion
}
