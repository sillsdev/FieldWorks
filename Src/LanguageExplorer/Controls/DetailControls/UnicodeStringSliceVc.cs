// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;

namespace LanguageExplorer.Controls.DetailControls
{
	internal class UnicodeStringSliceVc : FwBaseVc
	{
		private readonly int m_flid;

		public UnicodeStringSliceVc()
		{
			m_wsDefault = -1;
		}

		public UnicodeStringSliceVc(int flid, int ws, LcmCache lcmCache)
		{
			m_flid = flid;
			m_wsDefault = ws == -1 ? lcmCache.WritingSystemFactory.UserWs : ws;
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			vwenv.AddUnicodeProp(m_flid, m_wsDefault, this);
		}
	}
}