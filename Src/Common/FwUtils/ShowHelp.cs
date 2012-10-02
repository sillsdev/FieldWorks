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
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class provides common code for displaying a help topic.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ShowHelp
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
				//    helpTopicProvider.GetHelpString("UserHelpFile", 0);
				helpFile = DirectoryFinder.FWCodeDirectory +
					helpTopicProvider.GetHelpString(helpFileKey, 0);
			}
			catch
			{
				MessageBox.Show(FwUtilsStrings.ksCannotFindHelp);
				return;
			}

			// try to get a topic to show
			string helpTopic = helpTopicProvider.GetHelpString(helpTopicKey, 0);
			if (helpTopicKey == "khtpScrSectionFilter")
			{
				MessageBox.Show(FwUtilsStrings.khtpScrSectionFilter, FwUtilsStrings.ksScrCaption);
				return;
			}
			if (string.IsNullOrEmpty(helpTopic))
			{
				MessageBox.Show(String.Format(FwUtilsStrings.ksNoHelpTopicX, helpTopicKey));
				return;
			}

			// Ok, show the help. We have to use a label because without it the help is always
			// on top of the window
			Help.ShowHelp(new Label(), helpFile, helpTopic);
		}
	}
}
