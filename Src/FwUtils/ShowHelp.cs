// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Text;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// This class provides common code for displaying a help topic.
	/// </summary>
	public static class ShowHelp
	{
		/// <summary>
		/// Show a help topic.
		/// </summary>
		public static void ShowHelpTopic(IHelpTopicProvider helpTopicProvider, string helpTopicKey)
		{
			ShowHelpTopic(helpTopicProvider, "UserHelpFile", helpTopicKey);
		}

		/// <summary>
		/// This method removes all spaces from initialStr and returns the result.
		/// </summary>
		public static string RemoveSpaces(string initialStr)
		{
			var parts = initialStr.Trim().Split(' ');
			var strBldr = new StringBuilder(string.Empty);
			foreach (var str in parts)
			{
				strBldr.Append(str);
			}
			return strBldr.ToString();
		}

		/// <summary>
		/// Show a help topic.
		/// </summary>
		public static void ShowHelpTopic(IHelpTopicProvider helpTopicProvider, string helpFileKey, string helpTopicKey)
		{
			string helpFile;

			// sanity check
			if (helpTopicProvider == null || helpFileKey == null || helpTopicKey == null || string.Empty.Equals(helpFileKey) || string.Empty.Equals(helpTopicKey))
			{
				return;
			}
			// try to get a path to the help file.
			try
			{
				helpFile = FwDirectoryFinder.CodeDirectory + helpTopicProvider.GetHelpString(helpFileKey);
			}
			catch
			{
				MessageBox.Show(FwUtilsStrings.ksCannotFindHelp);
				return;
			}

			// try to get a topic to show
			var helpTopic = helpTopicProvider.GetHelpString(helpTopicKey);

			if (string.IsNullOrEmpty(helpTopic))
			{
				MessageBox.Show(string.Format(FwUtilsStrings.ksNoHelpTopicX, helpTopicKey));
				return;
			}

			// Show the help. We have to use a label because without it the help is always on top of the window
			Help.ShowHelp(new Label(), helpFile, helpTopic);
		}
	}
}