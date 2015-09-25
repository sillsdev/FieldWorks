// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FdoUi;

namespace LanguageExplorer.Areas.Grammar.Tools.PhonologicalFeaturesAdvancedEdit
{
	/// <summary />
	internal sealed class PhonologicalFeatureListDlgLauncherView : RootSiteControl, IVwNotifyChange
	{
		private IFsFeatStruc m_fs;
		private CmObjectUi.CmAnalObjectVc m_vc;

		/// <summary />
		public void Init(FdoCache cache, IFsFeatStruc fs)
		{
			CheckDisposed();

			m_fs = fs;
			m_fdoCache = cache;

			UpdateRootObject();
			m_fdoCache.DomainDataByFlid.AddNotification(this);
		}

		/// <summary />
		protected override void Dispose(bool disposing)
		{
			if (IsDisposed)
				return;

			if (disposing)
				m_fdoCache.DomainDataByFlid.RemoveNotification(this);

			base.Dispose(disposing);
		}

		/// <summary />
		public void UpdateFS(IFsFeatStruc fs)
		{
			m_fs = fs;
			UpdateRootObject();
		}

		private void UpdateRootObject()
		{
			if (m_rootb == null)
			{
				MakeRoot();
			}
			else if (m_fs != null)
			{
				m_rootb.SetRootObject(m_fs.Hvo, m_vc, (int)VcFrags.kfragName, m_rootb.Stylesheet);
				m_rootb.Reconstruct();
			}
		}

		/// <summary />
		public IPhPhoneme Phoneme { get; set; }

		#region RootSite required methods

		/// <summary />
		public override void MakeRoot()
		{
			CheckDisposed();

			base.MakeRoot();

			if (m_fdoCache == null || DesignMode)
				return;

			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);
			m_rootb.DataAccess = m_fdoCache.MainCacheAccessor;
			m_vc = new CmObjectUi.CmAnalObjectVc(m_fdoCache);

			if (m_fs != null)
			{
				m_rootb.SetRootObject(m_fs.Hvo, m_vc, (int)VcFrags.kfragName, m_rootb.Stylesheet);
			}
		}

		#endregion // RootSite required methods

		/// <summary>
		/// Listen for change to basic IPA symbol
		/// If description and/or features are empty, try to supply the values associated with the symbol
		/// </summary>
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (hvo == 0)
				return;
			// We only want to do something when the basic IPA symbol changes
			if ((tag != PhPhonemeTags.kflidFeatures) && (tag != FsFeatStrucTags.kflidFeatureSpecs))
				return;
			if (tag == FsFeatStrucTags.kflidFeatureSpecs)
			{
				IFsFeatStruc featStruc = m_fdoCache.ServiceLocator.GetInstance<IFsFeatStrucRepository>().GetObject(hvo);
				// only want to do something when the feature structure is part of a IPhPhoneme))
				if (featStruc.OwningFlid != PhPhonemeTags.kflidFeatures)
					return;
			}
			if (tag == PhPhonemeTags.kflidFeatures && Phoneme != null && hvo == Phoneme.Hvo)
			{
				m_fs = Phoneme.FeaturesOA;
				if (m_fs != null && m_rootb != null)
					m_rootb.SetRootObject(m_fs.Hvo, m_vc, (int)VcFrags.kfragName, m_rootb.Stylesheet);
			}

			if (m_rootb != null)
				m_rootb.Reconstruct();
		}
	}
}
