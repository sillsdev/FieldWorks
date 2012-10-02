// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ShowHelp.cs
// Responsibility: TE Team
//
// <remarks>
// This class provides common code for displaying a help topic.
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using XCore;
using SIL.Utils;
using System.Diagnostics;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class provides common code for displaying a help topic.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class ShowHelp
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show a help topic.
		/// </summary>
		/// <param name="helpTopicProvider"></param>
		/// <param name="helpTopicKey">string key to get the help topic</param>
		/// ------------------------------------------------------------------------------------
		public static void ShowHelpTopic(IHelpTopicProvider helpTopicProvider, string helpTopicKey)
		{
			ShowHelpTopic(helpTopicProvider, "UserHelpFile", helpTopicKey);
		}

		/// <summary>
		/// This method removes all spaces from initialStr and returns the result.
		/// </summary>
		/// <param name="initialStr">String</param>
		/// <returns></returns>
		public static String RemoveSpaces(String initialStr)
		{
			string[] parts = initialStr.Trim().Split(' ');
			StringBuilder strBldr = new StringBuilder("");
			foreach (string str in parts)
			{
				strBldr.Append(str);
			}
			return strBldr.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show a help topic.
		/// </summary>
		/// <param name="helpTopicProvider"></param>
		/// <param name="helpFileKey">string key to get the help file name</param>
		/// <param name="helpTopicKey">string key to get the help topic</param>
		/// ------------------------------------------------------------------------------------
		public static void ShowHelpTopic(IHelpTopicProvider helpTopicProvider, string helpFileKey,
			string helpTopicKey)
		{
			string helpFile;

			// sanity check
			if (helpTopicProvider == null || helpFileKey == null || helpTopicKey == null ||
				string.Empty.Equals(helpFileKey) || string.Empty.Equals(helpTopicKey))
				return;

			// try to get a path to the help file.
			try
			{
				//helpFile = DirectoryFinder.FWCodeDirectory +
				//	helpTopicProvider.GetHelpString("UserHelpFile");
				helpFile = DirectoryFinder.FWCodeDirectory +
					helpTopicProvider.GetHelpString(helpFileKey);
			}
			catch
			{
				MessageBox.Show(FwUtilsStrings.ksCannotFindHelp);
				return;
			}

			// try to get a topic to show
			string helpTopic = helpTopicProvider.GetHelpString(helpTopicKey);

			if (string.IsNullOrEmpty(helpTopic))
			{
				MessageBox.Show(String.Format(FwUtilsStrings.ksNoHelpTopicX, helpTopicKey));
				return;
			}

			if (MiscUtils.IsUnix)
			{
				ShowHelpTopic_Linux(helpFile, helpTopic);
			}
			else
			{
				// Ok, show the help. We have to use a label because without it the help is always
				// on top of the window
				Help.ShowHelp(new Label(), helpFile, helpTopic);
			}
		}

		/// <summary>Show a help file and topic using a Linux help viewer</summary>
		/// <param name="helpFile">.chm help file</param>
		/// <param name="helpTopic">path to a topic in helpFile, or null</param>
		public static void ShowHelpTopic_Linux(string helpFile, string helpTopic)
		{
			if (helpFile == null)
				throw new ArgumentNullException();
			if (helpFile == String.Empty)
				throw new ArgumentException();

			// Adjust helpFile path to use only forward slashes
			helpFile = helpFile.Replace(@"\", "/");

			string helpViewer = "chmsee";
			string arguments = helpFile;
			if (!String.IsNullOrEmpty(helpTopic))
				arguments = String.Format("'{0}::{1}'", helpFile, helpTopic);

			if (!RunNonblockingProcess(helpViewer, arguments))
				MessageBox.Show(String.Format(FwUtilsStrings.ksLinuxHelpViewerCouldNotLoad, helpViewer));
		}

		/// <returns>
		/// whether successfully started (or reused) process
		/// </returns>
		private static bool RunNonblockingProcess(string command, string arguments)
		{
			Process process = new Process();
			process.StartInfo.FileName = command;
			process.StartInfo.Arguments = arguments;
			process.StartInfo.UseShellExecute = false;

			try
			{
				process.Start();
			}
			catch
			{
				return false;
			}

			return true;
		}
	}
}