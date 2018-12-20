// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.DetailControls
{
	internal class TwoLevelConcView : RootSiteControl
	{
		internal TwoLevelConcVc m_cvc;
		IConcPolicy m_cp;
		IGetNodeInfo m_gni;
		int m_index; // slice number
		public TwoLevelConcView(IConcPolicy cp, IGetNodeInfo gni, int index)
		{
			m_cp = cp;
			m_gni = gni;
			m_index = index;
		}

		public override void MakeRoot()
		{
			if (m_cache == null || DesignMode)
			{
				return;
			}
			base.MakeRoot();
			RootBox.DataAccess = m_cache.DomainDataByFlid;
			m_cvc = new TwoLevelConcVc(m_cp, m_gni, m_index);
			// The root object is the one, if any, that the policy gives us for this slice.
			// If it doesn't give us one the vc will obtain a key string from the policy
			// directly. The frag argument is arbitrary.
			RootBox.SetRootObject(m_cp.Item(m_index), m_cvc, 1, m_styleSheet);
		}
	}
}