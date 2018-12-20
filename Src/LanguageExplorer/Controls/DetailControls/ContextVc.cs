// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace LanguageExplorer.Controls.DetailControls
{
	internal class ContextVc : FwBaseVc
	{
		private IConcSliceInfo m_info;

		public ContextVc(IConcSliceInfo info)
		{
			m_info = info;
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			// Enhance JohnT: make the alignment position a function of window width.
			// Enhance JohnT: change background if this is the selected context line.
			vwenv.OpenConcPara(m_info.ContextStringStartOffset, m_info.ContextStringStartOffset + m_info.ContextStringLength, VwConcParaOpts.kcpoDefault, 72 * 2 * 1000); // 72 pts per inch * 2 inches * 1000 -> 2" in millipoints.
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