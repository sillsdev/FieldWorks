// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using LanguageExplorer.Controls;
using LanguageExplorer.LcmUi;
using SIL.LCModel;

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

			if (RootBox == null)
			{
				MakeRoot();
			}
			else
			{
				RootBox.SetRootObject(m_fs?.Hvo ?? 0, m_vc, (int)VcFrags.kfragName, RootBox.Stylesheet);
				RootBox.Reconstruct();
			}
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

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

			RootBox.DataAccess = m_cache.DomainDataByFlid;
			m_vc = new CmAnalObjectVc(m_cache);
			if (m_fs != null)
			{
				RootBox.SetRootObject(m_fs.Hvo, m_vc, (int)VcFrags.kfragName, RootBox.Stylesheet);
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