// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2013, SIL International. All Rights Reserved.
// <copyright from='2013' to='2013' company='SIL International'>
//		Copyright (c) 2013, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
// ---------------------------------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FdoUi.Dialogs;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// The implementation of IFdoUI for FieldWorks apps.
	/// </summary>
	public class FwFdoUI : IFdoUI
	{
		private readonly IHelpTopicProvider m_helpTopicProvider;
		private readonly ISynchronizeInvoke m_synchronizeInvoke;

		public FwFdoUI(IHelpTopicProvider helpTopicProvider, ISynchronizeInvoke synchronizeInvoke)
		{
			m_helpTopicProvider = helpTopicProvider;
			m_synchronizeInvoke = synchronizeInvoke;
			Application.AddMessageFilter(new UserActivityMonitor(this));
		}

		/// <summary>
		/// Gets the object that is used to invoke methods on the main UI thread.
		/// </summary>
		public ISynchronizeInvoke SynchronizeInvoke
		{
			get { return m_synchronizeInvoke; }
		}

		/// <summary>
		/// Check with user regarding conflicting changes
		/// </summary>
		/// <returns>True if user wishes to revert to saved state. False otherwise.</returns>
		public bool ConflictingSave()
		{
			using (var dlg = new ConflictingSaveDlg())
			{
				DialogResult result = dlg.ShowDialog();
				return result != DialogResult.OK;
			}
		}

		/// <summary>
		/// Inform the user of a lost connection
		/// </summary>
		/// <returns>True if user wishes to attempt reconnect.  False otherwise.</returns>
		public bool ConnectionLost()
		{
			using (var dlg = new ConnectionLostDlg())
			{
				return dlg.ShowDialog() == DialogResult.Yes;
			}
		}

		/// <summary>
		/// Check with user regarding which files to use
		/// </summary>
		/// <returns></returns>
		public FileSelection ChooseFilesToUse()
		{
			using (var dlg = new FilesToRestoreAreOlder(m_helpTopicProvider))
			{
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					if (dlg.fKeepFilesThatAreNewer)
					{
						return FileSelection.OkKeepNewer;
					}
					if (dlg.fOverWriteThatAreNewer)
					{
						return FileSelection.OkUseOlder;
					}
				}
				return FileSelection.Cancel;
			}
		}

		/// <summary>
		/// Check with user regarding restoring linked files in the project folder or original path
		/// </summary>
		/// <returns>True if user wishes to restore linked files in project folder. False to leave them in the original location.</returns>
		public bool RestoreLinkedFilesInProjectFolder()
		{
			using (var dlg = new RestoreLinkedFilesToProjectsFolder(m_helpTopicProvider))
			{
				return dlg.ShowDialog() == DialogResult.OK && dlg.fRestoreLinkedFilesToProjectFolder;
			}
		}

		/// <summary>
		/// Cannot restore linked files to original path.
		/// Check with user regarding restoring linked files in the project folder or not at all
		/// </summary>
		/// <returns>OkYes to restore to project folder, OkNo to skip restoring linked files, Cancel otherwise</returns>
		public YesNoCancel CannotRestoreLinkedFilesToOriginalLocation()
		{
			using (var dlgCantWriteFiles = new CantRestoreLinkedFilesToOriginalLocation(m_helpTopicProvider))
			{
				if (dlgCantWriteFiles.ShowDialog() == DialogResult.OK)
				{
					if (dlgCantWriteFiles.fRestoreLinkedFilesToProjectFolder)
					{
						return YesNoCancel.OkYes;
					}
					if (dlgCantWriteFiles.fDoNotRestoreLinkedFiles)
					{
						return YesNoCancel.OkNo;
					}
				}
				return YesNoCancel.Cancel;
			}
		}

		/// <summary>
		/// Displays the message.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <param name="message">The message.</param>
		/// <param name="caption">The caption.</param>
		/// <param name="helpTopic">The help topic.</param>
		public void DisplayMessage(MessageType type, string message, string caption, string helpTopic)
		{
			var icon = MessageBoxIcon.Information;
			switch (type)
			{
				case MessageType.Error:
					icon = MessageBoxIcon.Error;
					break;
				case MessageType.Info:
					icon = MessageBoxIcon.Information;
					break;
				case MessageType.Warning:
					icon = MessageBoxIcon.Warning;
					break;
			}
			m_synchronizeInvoke.Invoke(() =>
				{
					if (string.IsNullOrEmpty(helpTopic))
						MessageBox.Show(message, caption, MessageBoxButtons.OK, icon);
					else
						MessageBox.Show(message, caption, MessageBoxButtons.OK, icon, MessageBoxDefaultButton.Button1,
							0, m_helpTopicProvider.HelpFile, HelpNavigator.Topic, helpTopic);
				});
		}

		/// <summary>
		/// show a dialog or output to the error log, as appropriate.
		/// </summary>
		/// <param name="error">the exception you want to report</param>
		/// <param name="isLethal">set to <c>true</c> if the error is lethal, otherwise
		/// <c>false</c>.</param>
		public void ReportException(Exception error, bool isLethal)
		{
			m_synchronizeInvoke.Invoke(() => ErrorReporter.ReportException(error, null, null, null, isLethal));
		}

		public DateTime LastActivityTime { get; private set; }

		/// <summary>
		/// Reports duplicate guids to the user
		/// </summary>
		/// <param name="applicationKey">The application key.</param>
		/// <param name="emailAddress">The email address.</param>
		/// <param name="errorText">The error text.</param>
		public void ReportDuplicateGuids(RegistryKey applicationKey, string emailAddress, string errorText)
		{
			m_synchronizeInvoke.Invoke(() => ErrorReporter.ReportDuplicateGuids(applicationKey, emailAddress, null, errorText));
		}

		/// <summary>
		/// Ask user if they wish to restore an XML project from a backup project file.
		/// </summary>
		/// <param name="projectPath">The project path.</param>
		/// <param name="backupPath">The backup path.</param>
		/// <returns></returns>
		/// <exception cref="System.NotImplementedException"></exception>
		public bool OfferToRestore(string projectPath, string backupPath)
		{
			return m_synchronizeInvoke.Invoke(() => MessageBox.Show(
				String.Format(FdoUiStrings.kstidOfferToRestore, projectPath, File.GetLastWriteTime(projectPath),
				backupPath, File.GetLastWriteTime(backupPath)),
				FdoUiStrings.kstidProblemOpeningFile, MessageBoxButtons.YesNo,
				MessageBoxIcon.Error) == DialogResult.Yes);
		}

		/// <summary>
		/// Exits the application.
		/// </summary>
		public void Exit()
		{
			Application.Exit();
		}

		/// <summary>
		/// Present a message to the user and allow the options to Retry or Cancel
		/// </summary>
		/// <param name="msg">The message.</param>
		/// <param name="caption">The caption.</param>
		/// <returns>True to retry.  False otherwise</returns>
		public bool Retry(string msg, string caption)
		{
			return MessageBox.Show(msg, caption,
				MessageBoxButtons.RetryCancel, MessageBoxIcon.None) == DialogResult.Retry;
		}

		/// <summary>
		/// This class is a message filter which can be installed in order to track when the user last
		/// pressed a key or did any mouse action, including moving the mouse.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Design", "TypesWithNativeFieldsShouldBeDisposableRule", Justification="No unmanaged resources to release")]
		class UserActivityMonitor : IMessageFilter
		{
			private readonly FwFdoUI m_ui;

			public UserActivityMonitor(FwFdoUI ui)
			{
				m_ui = ui;
			}

			private IntPtr m_lastMousePosition;

			public bool PreFilterMessage(ref Message m)
			{
				if(m.Msg == (int)Win32.WinMsgs.WM_MOUSEMOVE)
				{
					// For mouse move, we get spurious ones when it didn't really move. So check the actual position.
					if (m.LParam != m_lastMousePosition)
					{
						m_ui.LastActivityTime = DateTime.Now;
						m_lastMousePosition = m.LParam;
						// Enhance JohnT: suppress ones where it doesn't move??
					}
					return false;
				}
				if ((m.Msg >= (int)Win32.WinMsgs.WM_MOUSE_Min && m.Msg <= (int)Win32.WinMsgs.WM_MOUSE_Max)
					|| (m.Msg >= (int)Win32.WinMsgs.WM_KEY_Min && m.Msg <= (int)Win32.WinMsgs.WM_KEY_Max))
				{
					m_ui.LastActivityTime = DateTime.Now;
				}
				return false; // don't want to block any messages.
			}
		}
	}
}
