// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.Common.FwUtils
{
	public class HelpTopicProviderBase : IHelpTopicProvider
	{
		public static IHelpTopicProvider Instance { get; }

		static HelpTopicProviderBase()
		{
			Instance = new HelpTopicProviderBase();
		}

		#region IHelpTopicProvider implementation
		public virtual string GetHelpString(string id)
		{
			return ResourceHelper.GetHelpString(id);
		}

		public string HelpFile => FwDirectoryFinder.CodeDirectory + GetHelpString(FwUtilsConstants.UserHelpFile);
		#endregion
	}
}
