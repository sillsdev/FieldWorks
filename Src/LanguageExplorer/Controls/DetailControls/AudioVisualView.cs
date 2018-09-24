// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// The display of the media original file pathname.
	/// </summary>
	public class AudioVisualView : RootSiteControl
	{
		internal const int kfragPathname = 0;

		private System.ComponentModel.IContainer components = null;
		private ICmFile m_file;
		private int m_flid;
		private AudioVisualVc m_vc;

		public AudioVisualView()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
		}

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
			this.Name = "AudioVisualView";
			this.Size = new System.Drawing.Size(168, 24);

		}
		#endregion

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

			base.Dispose(disposing);

			if (disposing)
			{
				components?.Dispose();
			}
			m_vc = null;
		}

		public void Init(ICmFile obj, int flid)
		{
			m_cache = PropertyTable.GetValue<LcmCache>(LanguageExplorerConstants.cache);
			m_file = obj;
			m_flid = flid;
			if (m_rootb == null)
			{
				MakeRoot();
			}
			else if (m_file != null)
			{
				m_rootb.SetRootObject(m_file.Hvo, m_vc, kfragPathname, m_rootb.Stylesheet);
				m_rootb.Reconstruct();
			}
		}

		#region RootSite required methods

		public override void MakeRoot()
		{
			if (m_cache == null || DesignMode)
			{
				return;
			}

			base.MakeRoot();

			m_rootb.DataAccess = m_cache.DomainDataByFlid;
			m_vc = new AudioVisualVc(m_cache, m_flid, "InternalPath");
			if (m_file != null)
			{
				m_rootb.SetRootObject(m_file.Hvo, m_vc, kfragPathname, m_rootb.Stylesheet);
			}

		}

		#endregion // RootSite required methods
	}
}