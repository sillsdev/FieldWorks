using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FdoUi;
using XCore;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.XWorks.LexEd
{
	public class MSADlglauncherView : RootSiteControl
	{
		private IMoMorphSynAnalysis m_msa;
		private MoMorphSynAnalysisUi.MsaVc m_vc;

		private System.ComponentModel.IContainer components = null;

		public MSADlglauncherView()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
		}

		public void Init(Mediator mediator, IMoMorphSynAnalysis msa)
		{
			Debug.Assert(msa != null);
			m_msa = msa;
			m_fdoCache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			if (m_rootb == null)
			{
				MakeRoot();
			}
			else
			{
				MoMorphSynAnalysisUi msaUi = new MoMorphSynAnalysisUi(m_msa);
				m_rootb.SetRootObject(m_msa.Hvo, msaUi.Vc,
					(int)VcFrags.kfragFullMSAInterlinearname, m_rootb.Stylesheet);
				m_rootb.Reconstruct();
			}
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
			}
			m_vc = null;
			m_msa = null;
		}

		#region RootSite required methods

		public override void MakeRoot()
		{
			base.MakeRoot();

			if (m_fdoCache == null || DesignMode)
				return;

			MoMorphSynAnalysisUi msaUi = new MoMorphSynAnalysisUi(m_msa);
			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);
			m_rootb.DataAccess = m_fdoCache.MainCacheAccessor;
			m_vc = msaUi.Vc as MoMorphSynAnalysisUi.MsaVc;
			m_rootb.SetRootObject(m_msa.Hvo, m_vc,
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
	}
}
