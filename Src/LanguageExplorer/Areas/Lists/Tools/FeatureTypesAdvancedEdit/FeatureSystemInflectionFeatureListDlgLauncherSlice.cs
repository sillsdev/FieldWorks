// Copyright (c) 2012-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Areas.Grammar;

namespace LanguageExplorer.Areas.Lists.Tools.FeatureTypesAdvancedEdit
{
	/// <summary />
	internal sealed class FeatureSystemInflectionFeatureListDlgLauncherSlice : MsaInflectionFeatureListDlgLauncherSlice
	{
		public FeatureSystemInflectionFeatureListDlgLauncherSlice()
			: base()
		{
		}

		/// <summary>
		/// This method, called once we have a cache and object, is our first chance to
		/// actually create the embedded control.
		/// </summary>
		public override void FinishInit()
		{
			Control = new FeatureSystemInflectionFeatureListDlgLauncher();
		}
	}
}