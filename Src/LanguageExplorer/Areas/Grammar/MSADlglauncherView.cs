// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using LanguageExplorer.Controls;
using LanguageExplorer.LcmUi;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace LanguageExplorer.Areas.Grammar
{
	/// <summary />
	internal sealed class MSADlglauncherView : RootSiteControl, IVwNotifyChange
	{
		private IMoMorphSynAnalysis m_msa;
		private MsaVc m_vc;

		private System.ComponentModel.IContainer components = null;

		/// <summary />
		public MSADlglauncherView()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
		}

		public void Init(LcmCache cache, IMoMorphSynAnalysis msa)
		{
			Debug.Assert(msa != null);
			m_msa = msa;
			m_cache = cache;
			if (RootBox == null)
			{
				MakeRoot();
			}
			else
			{
				RootBox.SetRootObject(m_msa.Hvo, Vc, (int)VcFrags.kfragFullMSAInterlinearname, RootBox.Stylesheet);
				RootBox.Reconstruct();
			}
			m_cache.DomainDataByFlid.AddNotification(this);
		}

		private IVwViewConstructor Vc => m_vc ?? (m_vc = new MsaVc(m_cache));

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				components?.Dispose();
				m_cache?.DomainDataByFlid.RemoveNotification(this);
			}
			m_vc = null;
			m_msa = null;

			base.Dispose(disposing);
		}

		#region RootSite required methods

		public override void MakeRoot()
		{
			if (m_cache == null || DesignMode)
			{
				return;
			}

			base.MakeRoot();

			RootBox.DataAccess = m_cache.DomainDataByFlid;
			RootBox.SetRootObject(m_msa.Hvo, Vc, (int)VcFrags.kfragFullMSAInterlinearname, RootBox.Stylesheet);
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
			// MSADlglauncherView
			//
			this.Name = "MSADlglauncherView";
			this.Size = new System.Drawing.Size(168, 24);

		}
		#endregion

		#region IVwNotifyChange Members

		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (m_msa.Hvo == hvo)
			{
				RootBox.Reconstruct();
			}
		}

		#endregion
	}
}