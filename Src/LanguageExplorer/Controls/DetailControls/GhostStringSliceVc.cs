// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace LanguageExplorer.Controls.DetailControls
{
	internal class GhostStringSliceVc : FwBaseVc
	{
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			// The property is absolutely arbitrary because the ghost DA ignores it.
			vwenv.AddStringProp(GhostStringSlice.kflidFake, this);
		}
	}
}