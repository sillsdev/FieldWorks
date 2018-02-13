// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace LanguageExplorer.Controls.DetailControls
{
	internal class TwoLevelConcVc : FwBaseVc
	{
		IConcPolicy m_cp;
		IGetNodeInfo m_gni;
		int m_index; // slice number
		bool m_fExpanded;
		INodeInfo m_ni;
		public TwoLevelConcVc(IConcPolicy cp, IGetNodeInfo gni, int index)
		{
			m_cp = cp;
			m_gni = gni;
			m_index = index;
		}
		public bool Expanded
		{
			get
			{
				return m_fExpanded;
			}
			set
			{
				// Caller should arrange to regenerate the view.
				m_fExpanded = value;
				// Review JohnT: should we refresh this even if already obtianed when expanding?
				if (m_fExpanded && m_ni == null)
				{
					m_ni = m_gni.InfoFor(m_index, m_cp.Item(m_index));
				}
			}
		}
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				case 1:
				{
					// The top-level.
					// Enhance JohnT: add a property setting to make the key bold
					// Roughly, vwenv.set_IntProperty(ktptBold, ktpvEnum, kttvForceOn);
					// If we can get an hvo and flid, display that property of that object.
					var flid = 0;
					if (hvo != 0)
					{
						flid = m_cp.FlidFor(m_index, hvo);
					}
					if (flid != 0)
					{
						// Warning (JohnT): this option not yet tested...
						vwenv.AddStringProp(flid, this);
						return;
					}
					// Otherwise display a literal string straight from the policy object.
					vwenv.AddString(m_cp.KeyFor(m_index, hvo));

					if (m_fExpanded)
					{
						vwenv.AddLazyVecItems(m_ni.ListFlid, this, 2);
					}
					break;
				}
				case 2:
				{
					// One line of context.

					// Figure the index of this object in the next object out (the root).
					int hvoOuter, tagOuter, ihvo;
					vwenv.GetOuterObject(vwenv.EmbeddingLevel - 1, out hvoOuter, out tagOuter, out ihvo);
					var ichKey = m_ni.ContextStringStartOffset(ihvo, hvo);
					var cchKey = m_ni.ContextStringLength(ihvo, hvo);
					// Enhance JohnT: make the alignment position a function of window width.
					// Enhance JohnT: change background if this is the selected context line.
					vwenv.OpenConcPara(ichKey, ichKey + cchKey, VwConcParaOpts.kcpoDefault, 72 * 2 * 1000); // 72 pts per inch * 2 inches * 1000 -> 2" in millipoints.
					var flidKey = m_ni.ContextStringFlid(ihvo, hvo);
					if (flidKey == 0)
					{
						// Not tested yet.
						vwenv.AddString(m_ni.ContextString(ihvo, hvo));
					}
					else
					{
						vwenv.AddStringProp(flidKey, this);
					}
					vwenv.CloseParagraph();
					break;
				}
			}
		}
	}
}