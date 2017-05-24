// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FdoUi;

namespace SIL.FieldWorks.XWorks.LexEd
{
	public class MSADlglauncherView : RootSiteControl, IVwNotifyChange
	{
		private IMoMorphSynAnalysis m_msa;
		private MoMorphSynAnalysisUi.MsaVc m_vc;

		private System.ComponentModel.IContainer components = null;

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
			if (m_rootb == null)
			{
				MakeRoot();
			}
			else
			{
				m_rootb.SetRootObject(m_msa.Hvo, Vc,
					(int)VcFrags.kfragFullMSAInterlinearname, m_rootb.Stylesheet);
				m_rootb.Reconstruct();
			}
			m_cache.DomainDataByFlid.AddNotification(this);
		}

		private IVwViewConstructor Vc
		{
			get
			{
				if (m_vc == null)
					m_vc = new MoMorphSynAnalysisUi.MsaVc(m_cache);
				return m_vc;
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

			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
				if (m_cache != null)
					m_cache.DomainDataByFlid.RemoveNotification(this);
			}
			m_vc = null;
			m_msa = null;

			base.Dispose(disposing);
		}

		#region RootSite required methods

		public override void MakeRoot()
		{
			if (m_cache == null || DesignMode)
				return;

			base.MakeRoot();

			m_rootb.DataAccess = m_cache.DomainDataByFlid;
			m_rootb.SetRootObject(m_msa.Hvo, Vc,
				(int)VcFrags.kfragFullMSAInterlinearname, m_rootb.Stylesheet);
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
			CheckDisposed();

			if (m_msa.Hvo == hvo)
				m_rootb.Reconstruct();
		}

		#endregion
	}
}
