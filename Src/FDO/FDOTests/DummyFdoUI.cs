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
using SIL.Utils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Dummy implementation of FdoUserAction for unit tests
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="m_threadHelper is a singleton and disposed by the SingletonsContainer")]
	public class DummyFdoUI : IFdoUI
	{
		private readonly ThreadHelper m_threadHelper = SingletonsContainer.Get<ThreadHelper>();

		/// <summary>
		/// Gets the object that is used to invoke methods on the main UI thread.
		/// </summary>
		public ISynchronizeInvoke SynchronizeInvoke
		{
			get { return m_threadHelper; }
		}

		/// <summary>
		/// Gets the error message.
		/// </summary>
		/// <value>
		/// The error message.
		/// </value>
		public string ErrorMessage { get; private set; }

		/// <summary>
		/// Check with user regarding conflicting changes
		/// </summary>
		/// <returns>True if user wishes to revert to saved state. False otherwise.</returns>
		public bool ConflictingSave()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Inform the user of a lost connection
		/// </summary>
		/// <returns>True if user wishes to attempt reconnect.  False otherwise.</returns>
		public bool ConnectionLost()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the last time that there was user activity.
		/// </summary>
		public DateTime LastActivityTime
		{
			get { return DateTime.Now; }
		}

		/// <summary>
		/// Check with user regarding which files to use
		/// </summary>
		/// <returns></returns>
		public FileSelection ChooseFilesToUse()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Check with user regarding restoring linked files in the project folder or original path
		/// </summary>
		/// <returns>True if user wishes to restore linked files in project folder. False to leave them in the original location.</returns>
		public bool RestoreLinkedFilesInProjectFolder()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Cannot restore linked files to original path.
		/// Check with user regarding restoring linked files in the project folder or not at all
		/// </summary>
		/// <returns>OkYes to restore to project folder, OkNo to skip restoring linked files, Cancel otherwise</returns>
		public YesNoCancel CannotRestoreLinkedFilesToOriginalLocation()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Displays information to the user
		/// </summary>
		/// <param name="type"></param>
		/// <param name="message"></param>
		/// <param name="caption"></param>
		/// <param name="helpTopic"></param>
		public void DisplayMessage(MessageType type, string message, string caption, string helpTopic)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Show a dialog or output to the error log, as appropriate.
		/// </summary>
		/// <param name="error">the exception you want to report</param>
		/// <param name="isLethal">set to <c>true</c> if the error is lethal, otherwise
		/// <c>false</c>.</param>
		/// <returns>True if the error was lethal and the user chose to exit the application,
		/// false otherwise.</returns>
		public void ReportException(Exception error, bool isLethal)
		{
			// Store the message so we can check it later
			ErrorMessage = error.Message;
		}

		/// <summary>
		/// Reports duplicate guids to the user
		/// </summary>
		/// <param name="errorText">The error text.</param>
		public void ReportDuplicateGuids(string errorText)
		{
			throw new NotImplementedException();
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
			throw new NotImplementedException();
		}

		/// <summary>
		/// Exits the application.
		/// </summary>
		/// <exception cref="System.NotImplementedException"></exception>
		public void Exit()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Present a message to the user and allow the options to Retry or Cancel
		/// </summary>
		/// <param name="msg">The message.</param>
		/// <param name="caption">The caption.</param>
		/// <returns>True to retry.  False otherwise</returns>
		public bool Retry(string msg, string caption)
		{
			throw new NotImplementedException();
		}
	}
}
