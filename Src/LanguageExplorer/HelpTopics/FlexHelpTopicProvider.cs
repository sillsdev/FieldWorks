// Copyright (c) 2010-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Reflection;
using System.Resources;
using SIL.FieldWorks.Common.Framework;

namespace LanguageExplorer.HelpTopics
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// FLEx-specific HelpTopicProvider
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal sealed class FlexHelpTopicProvider : HelpTopicProviderBase
	{
		private static ResourceManager s_helpResources = null;

		internal FlexHelpTopicProvider() {}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a help file URL or topic
		/// </summary>
		/// <param name="stid"></param>
		/// <returns>The requested string</returns>
		/// ------------------------------------------------------------------------------------
		public override string GetHelpString(string stid)
		{
			if (s_helpResources == null)
			{
				s_helpResources = new ResourceManager("LanguageExplorer.HelpTopics.HelpTopicPaths",
					Assembly.GetExecutingAssembly());
			}

			if (stid == null)
				return "NullStringID";

			// First try to find it in our resource file. If that doesn't work, try the more general one
			return s_helpResources.GetString(stid) ?? base.GetHelpString(stid);
		}
	}
}
