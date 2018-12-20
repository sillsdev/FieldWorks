// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace LanguageExplorer.Controls.DetailControls
{
	internal class SummaryVc : FwBaseVc
	{
		private readonly IConcSliceInfo m_info;

		public SummaryVc(IConcSliceInfo info)
		{
			m_info = info;
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			// Enhance JohnT: change background if this is the selected slice.
			vwenv.OpenParagraph();
			if (m_info.Hvo == 0 || m_info.ContentStringFlid == 0)
			{
				vwenv.AddString(m_info.ContentString);
			}
			else
			{
				Debug.Assert(hvo == m_info.Hvo);
				vwenv.AddStringProp(m_info.ContentStringFlid, this);
			}
			vwenv.CloseParagraph();
		}
	}
}