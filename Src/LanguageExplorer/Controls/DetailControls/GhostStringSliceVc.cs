// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace LanguageExplorer.Controls.DetailControls
{
	internal sealed class GhostStringSliceVc : FwBaseVc
	{
		internal const int kflidFake = -2001;

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			// The property is absolutely arbitrary because the ghost DA ignores it.
			vwenv.AddStringProp(kflidFake, this);
		}
	}
}