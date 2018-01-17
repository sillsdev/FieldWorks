// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;

namespace LanguageExplorer.Areas
{
	internal class StringRepSliceVc : FwBaseVc
	{
		public static int Flid => PhEnvironmentTags.kflidStringRepresentation;

		public StringRepSliceVc()
		{
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			vwenv.AddStringProp(Flid, this);
		}
	}
}