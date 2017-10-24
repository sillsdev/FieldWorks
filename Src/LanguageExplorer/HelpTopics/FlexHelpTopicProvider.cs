// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.ComponentModel.Composition;
using System.Reflection;
using System.Resources;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;

namespace LanguageExplorer.HelpTopics
{
	/// <summary>
	/// FLEx-specific HelpTopicProvider
	/// </summary>
	[Export(typeof(IHelpTopicProvider))]
	internal sealed class FlexHelpTopicProvider : IHelpTopicProvider
	{
		private static ResourceManager s_helpResources;

		private IHelpTopicProvider AsIHelpTopicProvider => this;

		#region IHelpTopicProvider implementation
		/// <summary>
		/// Gets a help file URL or topic
		/// </summary>
		/// <param name="stid"></param>
		/// <returns>The requested string</returns>
		string IHelpTopicProvider.GetHelpString(string stid)
		{
			if (s_helpResources == null)
			{
				s_helpResources = new ResourceManager("LanguageExplorer.HelpTopics.HelpTopicPaths", Assembly.GetExecutingAssembly());
			}

			if (string.IsNullOrWhiteSpace(stid))
			{
				return "NullStringID";
			}

			// First try to find it in our resource file. If that doesn't work, try the more general one
			return s_helpResources.GetString(stid) ?? ResourceHelper.GetHelpString(stid);
		}

		/// <summary>
		/// The HTML help file (.chm) for the app.
		/// </summary>
		string IHelpTopicProvider.HelpFile => FwDirectoryFinder.CodeDirectory + AsIHelpTopicProvider.GetHelpString("UserHelpFile");

		#endregion
	}
}
