// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// The display of the media original file pathname.
	/// </summary>
	internal class AudioVisualView : RootSiteControl
	{
		internal const int kfragPathname = 0;
		private System.ComponentModel.IContainer components = null;
		private ICmFile m_file;
		private int m_flid;
		private AudioVisualVc m_vc;

		public AudioVisualView()
		{
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
		}

		public void Init(ICmFile obj, int flid)
		{
			m_cache = PropertyTable.GetValue<LcmCache>(FwUtils.cache);
			m_file = obj;
			m_flid = flid;
			if (RootBox == null)
			{
				MakeRoot();
			}
			else if (m_file != null)
			{
				RootBox.SetRootObject(m_file.Hvo, m_vc, kfragPathname, RootBox.Stylesheet);
				RootBox.Reconstruct();
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
			RootBox.DataAccess = m_cache.DomainDataByFlid;
			m_vc = new AudioVisualVc(m_cache, m_flid, "InternalPath");
			if (m_file != null)
			{
				RootBox.SetRootObject(m_file.Hvo, m_vc, kfragPathname, RootBox.Stylesheet);
			}
		}

		#endregion // RootSite required methods
	}
}