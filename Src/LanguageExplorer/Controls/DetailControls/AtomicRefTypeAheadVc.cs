// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;

namespace LanguageExplorer.Controls.DetailControls
{
	internal class AtomicRefTypeAheadVc : FwBaseVc
	{
		public AtomicRefTypeAheadVc(int flid, LcmCache cache)
		{
			TasVc = new TypeAheadSupportVc(flid, cache);
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			TasVc.Insert(vwenv, hvo);
		}

		public TypeAheadSupportVc TasVc { get; }
	}
}