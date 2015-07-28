// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FdoUi;
using XCore;

namespace SIL.FieldWorks.XWorks.LexEd
{
	public class MsaInflectionFeatureListDlgLauncherView : RootSiteControl
	{
		private IFsFeatStruc m_fs;
		private CmObjectUi.CmAnalObjectVc m_vc;

		private System.ComponentModel.IContainer components = null;

		public MsaInflectionFeatureListDlgLauncherView()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
		}

		public void Init(Mediator mediator, IFsFeatStruc fs)
		{
			CheckDisposed();

			m_fs = fs;
			m_fdoCache = (FdoCache)mediator.PropertyTable.GetValue("cache");

			if (m_rootb == null)
			{
				MakeRoot();
			}
			else
			{
				m_rootb.SetRootObject(m_fs == null ? 0 : m_fs.Hvo, m_vc, (int)VcFrags.kfragName, m_rootb.Stylesheet);
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
				if (components != null)
				{
					components.Dispose();
				}
			}
			m_vc = null;
			m_fs = null;
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
			m_rootb.DataAccess = m_fdoCache.DomainDataByFlid;
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
			// MsaInflectionFeatureListDlgLauncherView
			//
			this.Name = "MsaInflectionFeatureListDlgLauncherView";
			this.Size = new System.Drawing.Size(168, 24);

		}
		#endregion
	}
}
