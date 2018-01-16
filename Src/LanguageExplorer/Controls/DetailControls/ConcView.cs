// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace LanguageExplorer.Controls.DetailControls
{
	public class ConcView : RootSiteControl
	{
		IConcSliceInfo m_info;
		IVwViewConstructor m_vc;
		public ConcView(IConcSliceInfo info)
		{
			m_info = info;
		}
		public IConcSliceInfo SliceInfo
		{
			get { CheckDisposed(); return m_info; }
		}

		public override void MakeRoot()
		{
			CheckDisposed();

			if (m_cache == null || DesignMode)
			{
				return;
			}

			base.MakeRoot();

			m_rootb.DataAccess = m_cache.DomainDataByFlid;

			m_vc = m_info.Vc;
			if (m_vc == null)
			{
				if (m_info.DisplayAsContext)
				{
					m_vc = new ContextVc(m_info);
				}
				else
				{
					m_vc = new SummaryVc(m_info);
				}
			}

			// The root object is the one, if any, that the policy gives us.
			// If it doesn't give us one the vc will obtain a key string from the policy
			// directly. The frag argument is arbitrary. Note that we have to use a non-zero
			// HVO, even when it doesn't mean anything, to avoid triggering an Assert in the Views code.
			m_rootb.SetRootObject(m_info.Hvo == 0 ? 1 : m_info.Hvo, m_vc, 1, m_styleSheet);
		}
	}
}