// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.ViewsInterfaces;

namespace LanguageExplorer.Controls.DetailControls
{
	internal class ConcView : RootSiteControl
	{
		IVwViewConstructor m_vc;
		public ConcView(IConcSliceInfo info)
		{
			SliceInfo = info;
		}
		public IConcSliceInfo SliceInfo { get; }

		public override void MakeRoot()
		{
			if (m_cache == null || DesignMode)
			{
				return;
			}

			base.MakeRoot();

			m_rootb.DataAccess = m_cache.DomainDataByFlid;

			m_vc = SliceInfo.Vc;
			if (m_vc == null)
			{
				if (SliceInfo.DisplayAsContext)
				{
					m_vc = new ContextVc(SliceInfo);
				}
				else
				{
					m_vc = new SummaryVc(SliceInfo);
				}
			}

			// The root object is the one, if any, that the policy gives us.
			// If it doesn't give us one the vc will obtain a key string from the policy
			// directly. The frag argument is arbitrary. Note that we have to use a non-zero
			// HVO, even when it doesn't mean anything, to avoid triggering an Assert in the Views code.
			m_rootb.SetRootObject(SliceInfo.Hvo == 0 ? 1 : SliceInfo.Hvo, m_vc, 1, m_styleSheet);
		}
	}
}