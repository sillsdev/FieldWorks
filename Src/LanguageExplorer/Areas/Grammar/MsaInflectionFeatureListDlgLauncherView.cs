// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.LcmUi;
using SIL.LCModel;
using SIL.FieldWorks.Common.RootSites;

namespace LanguageExplorer.Areas.Grammar
{
	/// <summary />
	internal class MsaInflectionFeatureListDlgLauncherView : RootSiteControl
	{
		private IFsFeatStruc m_fs;
		private CmAnalObjectVc m_vc;

		private System.ComponentModel.IContainer components = null;

		/// <summary />
		public MsaInflectionFeatureListDlgLauncherView()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
		}

		/// <summary />
		public void Init(LcmCache cache, IFsFeatStruc fs)
		{
			m_fs = fs;
			m_cache = cache;

			if (m_rootb == null)
			{
				MakeRoot();
			}
			else
			{
				m_rootb.SetRootObject(m_fs?.Hvo ?? 0, m_vc, (int)VcFrags.kfragName, m_rootb.Stylesheet);
				m_rootb.Reconstruct();
			}
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose(disposing);

			if (disposing)
			{
				components?.Dispose();
			}
			m_vc = null;
			m_fs = null;
		}

		#region RootSite required methods

		/// <summary />
		public override void MakeRoot()
		{
			if (m_cache == null || DesignMode)
			{
				return;
			}

			base.MakeRoot();

			m_rootb.DataAccess = m_cache.DomainDataByFlid;
			m_vc = new CmAnalObjectVc(m_cache);

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
			// MsaInflectionFeatureListDlgLauncherView
			//
			this.Name = "MsaInflectionFeatureListDlgLauncherView";
			this.Size = new System.Drawing.Size(168, 24);

		}
		#endregion
	}
}