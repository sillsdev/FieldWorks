// Copyright (c) 2013-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using SIL.LCModel;
using SIL.LCModel.Utils;
using SIL.PlatformUtilities;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// The implementation of ILcmUI for FieldWorks apps.
	/// </summary>
	public sealed class FwLcmUI : ILcmUI
	{
		private readonly IHelpTopicProvider m_helpTopicProvider;
		private readonly UserActivityMonitor m_activityMonitor;
		private readonly ISynchronizeInvoke _synchronizeInvoke;

		public FwLcmUI(IHelpTopicProvider helpTopicProvider, ISynchronizeInvoke synchronizeInvoke)
		{
			m_helpTopicProvider = helpTopicProvider;
			_synchronizeInvoke = synchronizeInvoke;
			m_activityMonitor = new UserActivityMonitor();
			m_activityMonitor.StartMonitoring();
		}

		/// <summary>
		/// Gets the object that is used to invoke methods on the main UI thread.
		/// </summary>
		ISynchronizeInvoke ILcmUI.SynchronizeInvoke => _synchronizeInvoke;

		/// <summary>
		/// Check with user regarding conflicting changes
		/// </summary>
		/// <returns>True if user wishes to revert to saved state. False otherwise.</returns>
		bool ILcmUI.ConflictingSave()
		{
			using (var dlg = new ConflictingSaveDlg())
			{
				var result = dlg.ShowDialog();
				return result != DialogResult.OK;
			}
		}

		DateTime ILcmUI.LastActivityTime => m_activityMonitor.LastActivityTime;

		/// <summary>
		/// Check with user regarding which files to use
		/// </summary>
		/// <returns></returns>
		FileSelection ILcmUI.ChooseFilesToUse()
		{
			using (var dlg = new FilesToRestoreAreOlder(m_helpTopicProvider))
			{
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					if (dlg.KeepFilesThatAreNewer)
					{
						return FileSelection.OkKeepNewer;
					}
					if (dlg.OverWriteThatAreNewer)
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
		bool ILcmUI.RestoreLinkedFilesInProjectFolder()
		{
			using (var dlg = new RestoreLinkedFilesToProjectsFolder(m_helpTopicProvider))
			{
				return dlg.ShowDialog() == DialogResult.OK && dlg.RestoreLinkedFilesToProjectFolder;
			}
		}

		/// <summary>
		/// Cannot restore linked files to original path.
		/// Check with user regarding restoring linked files in the project folder or not at all
		/// </summary>
		/// <returns>OkYes to restore to project folder, OkNo to skip restoring linked files, Cancel otherwise</returns>
		YesNoCancel ILcmUI.CannotRestoreLinkedFilesToOriginalLocation()
		{
			using (var dlgCantWriteFiles = new CantRestoreLinkedFilesToOriginalLocation(m_helpTopicProvider))
			{
				if (dlgCantWriteFiles.ShowDialog() == DialogResult.OK)
				{
					if (dlgCantWriteFiles.RestoreLinkedFilesToProjectFolder)
					{
						return YesNoCancel.OkYes;
					}
					if (dlgCantWriteFiles.DoNotRestoreLinkedFiles)
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
		void ILcmUI.DisplayMessage(MessageType type, string message, string caption, string helpTopic)
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
			_synchronizeInvoke.Invoke(() =>
			{
				if (Platform.IsMono)
				{
					// Mono doesn't support Help
					MessageBox.Show(message, caption, MessageBoxButtons.OK, icon);
				}
				else if (string.IsNullOrEmpty(helpTopic))
				{
					MessageBox.Show(message, caption, MessageBoxButtons.OK, icon);
				}
				else
				{
					MessageBox.Show(message, caption, MessageBoxButtons.OK, icon, MessageBoxDefaultButton.Button1, 0, m_helpTopicProvider.HelpFile, HelpNavigator.Topic, helpTopic);
				}
			});
		}

		/// <summary>
		/// show a dialog or output to the error log, as appropriate.
		/// </summary>
		void ILcmUI.ReportException(Exception error, bool isLethal)
		{
			_synchronizeInvoke.Invoke(() => ErrorReporter.ReportException(error, null, null, null, isLethal));
		}

		/// <summary>
		/// Reports duplicate guids to the user
		/// </summary>
		void ILcmUI.ReportDuplicateGuids(string errorText)
		{
			_synchronizeInvoke.Invoke(() => ErrorReporter.ReportDuplicateGuids(FwRegistryHelper.FieldWorksRegistryKey, "FLExErrors@sil.org", null, errorText));
		}

		/// <summary>
		/// Displays the circular reference breaker report.
		/// </summary>
		void ILcmUI.DisplayCircularRefBreakerReport(string report, string caption)
		{
			_synchronizeInvoke.Invoke(() => MessageBox.Show(report, caption, MessageBoxButtons.OK, MessageBoxIcon.Information));
		}

		/// <summary>
		/// Present a message to the user and allow the options to Retry or Cancel
		/// </summary>
		/// <returns>True to retry. False otherwise.</returns>
		bool ILcmUI.Retry(string msg, string caption)
		{
			return _synchronizeInvoke.Invoke(() => MessageBox.Show(msg, caption, MessageBoxButtons.RetryCancel, MessageBoxIcon.None) == DialogResult.Retry);
		}

		/// <summary>
		/// Ask user if they wish to restore an XML project from a backup project file.
		/// </summary>
		bool ILcmUI.OfferToRestore(string projectPath, string backupPath)
		{
			return _synchronizeInvoke.Invoke(() => MessageBox.Show(string.Format(FwUtilsStrings.kstidOfferToRestore, projectPath, File.GetLastWriteTime(projectPath),
				backupPath, File.GetLastWriteTime(backupPath)), FwUtilsStrings.kstidProblemOpeningFile, MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes);
		}
	}
}