/// --------------------------------------------------------------------------------------------
#region /// Copyright (c) 2002, SIL International. All Rights Reserved.
/// <copyright from='2002' to='2002' company='SIL International'>
///		Copyright (c) 2002, SIL International. All Rights Reserved.
///
///		Distributable under the terms of either the Common Public License or the
///		GNU Lesser General Public License, as specified in the LICENSING.txt file.
/// </copyright>
#endregion
///
/// File: HelloViewView.cs
/// Responsibility: Eberhard Beilharz
/// Last reviewed:
///
/// <remarks>
/// Implementation of a really simple view, similar to the sample described in
/// Views User Guide.htm.
/// </remarks>
/// --------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;


namespace SIL.FieldWorks.Samples.HelloView
{
	public class HelloViewView : SIL.FieldWorks.Common.RootSites.SimpleRootSite
	{
		public const int ktagProp = 99;
		public const int kfrText = 0;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		private HvVc m_vVc;
		ISilDataAccess m_sda;
		ILgWritingSystemFactory m_wsf;

		#region Constructor, Dispose and Component Designer generated code
		public HelloViewView()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
			m_wsf.Shutdown(); // Not normally in View Dispose, but after closing ALL views.
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}
		#endregion
		#endregion

		public override void MakeRoot()
		{
			m_rootb = (IVwRootBox)new FwViews.VwRootBoxClass();
			m_rootb.SetSite(this);

			int hvoRoot = 1;

			m_sda = (ISilDataAccess) new FwViews.VwCacheDaClass();
			// Usually not here, but in some application global passed to each view.
			m_wsf = (ILgWritingSystemFactory) new FwLanguage.LgWritingSystemFactoryClass();
			m_sda.set_WritingSystemFactory(m_wsf);
			m_rootb.set_DataAccess(m_sda);

			ITsStrFactory tsf = (ITsStrFactory)new FwKernelLib.TsStrFactoryClass();
			ITsString tss = tsf.MakeString("Hello World! This is a view", m_wsf.get_UserWs());

			IVwCacheDa cda = (IVwCacheDa) m_sda;
			cda.CacheStringProp(hvoRoot, ktagProp, tss);

			m_vVc = new HvVc();

			m_rootb.SetRootObject(hvoRoot, m_vVc, kfrText, null);
			m_fRootboxMade = true;
			m_dxdLayoutWidth = -50000; // Don't try to draw until we get OnSize and do layout.
		}
	}
}
