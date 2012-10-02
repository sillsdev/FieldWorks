using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FdoUi;
using XCore;

namespace SIL.FieldWorks.XWorks.LexEd
{
	public class PhonologicalFeatureListDlgLauncherView : RootSiteControl, IVwNotifyChange
	{
		private IFsFeatStruc m_fs;
		private CmObjectUi.CmAnalObjectVc m_vc;

		private System.ComponentModel.IContainer components = null;
		private ISilDataAccess m_sda;
		private IPhPhoneme m_phoneme;

		public PhonologicalFeatureListDlgLauncherView()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
		}

		public void Init(Mediator mediator, IFsFeatStruc fs)
		{
			CheckDisposed();

			m_fs = fs;
			m_fdoCache = (FdoCache)mediator.PropertyTable.GetValue("cache");

			UpdateRootObject();
			m_sda = m_fdoCache.MainCacheAccessor;
			m_sda.AddNotification(this);
		}

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

		public IPhPhoneme Phoneme
		{
			get { return m_phoneme; }
			set { m_phoneme = value; }
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose(disposing);

			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
				if (m_vc != null)
					m_vc.Dispose();
				m_sda.RemoveNotification(this);
			}
			m_vc = null;
			m_fs = null;
			m_sda = null;
		}

		#region RootSite required methods

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

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			//
			// PhonologicalFeatureListDlgLauncherView
			//
			this.Name = "PhonologicalFeatureListDlgLauncherView";
			this.Size = new System.Drawing.Size(168, 24);

		}
		#endregion
		/// <summary>
		/// Listen for change to basic IPA symbol
		/// If description and/or features are empty, try to supply the values associated with the symbol
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ivMin"></param>
		/// <param name="cvIns"></param>
		/// <param name="cvDel"></param>
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (hvo == 0)
				return;
			// We only want to do something when the basic IPA symbol changes
			if ((tag != (int)PhPhoneme.PhPhonemeTags.kflidFeatures) && (tag != (int)FsFeatStruc.FsFeatStrucTags.kflidFeatureSpecs))
				return;
			if (tag == (int)FsFeatStruc.FsFeatStrucTags.kflidFeatureSpecs)
			{
				// only want to do something when the feature structure is part of a PhPhoneme
				if (Cache.GetOwningFlidOfObject(hvo) != (int)PhPhoneme.PhPhonemeTags.kflidFeatures)
					return;
			}
			if (tag == (int)PhPhoneme.PhPhonemeTags.kflidFeatures && Phoneme != null && hvo == Phoneme.Hvo)
			{
				m_fs = Phoneme.FeaturesOA;
				m_rootb.SetRootObject(Phoneme.FeaturesOAHvo, m_vc, (int)VcFrags.kfragName, m_rootb.Stylesheet);
			}

			if (m_rootb != null)
				m_rootb.Reconstruct();
		}
	}
}
