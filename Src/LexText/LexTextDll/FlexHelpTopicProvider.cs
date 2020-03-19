// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Reflection;
using System.Resources;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.XWorks.LexText
{
	/// <summary>
	/// FLEx-specific HelpTopicProvider
	/// </summary>
	public class FlexHelpTopicProvider : HelpTopicProviderBase
	{
		private static ResourceManager s_helpResources;

		public override string GetHelpString(string id)
		{
			if (s_helpResources == null)
			{
				s_helpResources = new ResourceManager("SIL.FieldWorks.XWorks.LexText.HelpTopicPaths",
					Assembly.GetExecutingAssembly());
			}

			if (id == null)
				return "NullStringID";

			// First try to find it in our resource file. If that doesn't work, try the more general one
			return s_helpResources.GetString(id) ?? base.GetHelpString(id);
		}
	}
}
