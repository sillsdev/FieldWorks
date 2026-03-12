// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.LCModel.Core.Text;
using SIL.LCModel;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Utils;
using XCore;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// A slice that displays the media file URIs as static text.
	/// </summary>
	public class MediaInfoSlice : ViewSlice
	{
		public MediaInfoSlice()
		{
			InitializeComponent();
		}

		private void InitializeComponent()
		{
			this.Name = "MediaInfoSlice";
			this.Size = new System.Drawing.Size(208, 32);
		}

		protected override void Dispose(bool disposing)
		{
			if (IsDisposed)
				return;
			base.Dispose(disposing);
		}

		public ILcmOwningCollection<ICmMediaURI> MediaURIs
		{
			get
			{
				CheckDisposed();
				return (m_obj as IText)?.MediaFilesOA?.MediaURIsOC;
			}
		}

		/// <summary>
		/// Create the embedded RootSite-based view.
		/// </summary>
		public override void FinishInit()
		{
			CheckDisposed();
			Control = new MediaInfoView(m_obj.Hvo);
		}

		public override void Install(DataTree parent)
		{
			CheckDisposed();
			base.Install(parent);

			var view = Control as MediaInfoView;
			if (view != null)
			{
				LcmCache cache = null;
				if (m_propertyTable != null)
					cache = m_propertyTable.GetValue<LcmCache>("cache");
				view.Init(cache, m_propertyTable, MediaURIs);
			}
		}

		/// <summary>
		/// RootSite is the embedded control (MediaInfoView derives from RootSiteControl).
		/// </summary>
		public override RootSite RootSite
		{
			get
			{
				CheckDisposed();
				return base.Control as RootSite;
			}
		}
	}

	/// <summary>
	/// RootSite control that displays static strings (media file paths).
	/// </summary>
	public class MediaInfoView : RootSiteControl
	{
		public const int kfragRoot = 1;
		private int m_ownerHvo;
		private MediaInfoVc m_vc;
		private ILcmOwningCollection<ICmMediaURI> m_mediaURIs;

		public MediaInfoView(int ownerHvo)
		{
			m_ownerHvo = ownerHvo;
			BackColor = System.Drawing.SystemColors.Window;
			Dock = DockStyle.Fill;
		}

		public void Init(LcmCache cache, PropertyTable propertyTable, ILcmOwningCollection<ICmMediaURI> mediaURIs)
		{
			CheckDisposed();
			m_cache = cache;
			m_propertyTable = propertyTable;
			m_mediaURIs = mediaURIs;

			MakeRoot();
		}

		public override void MakeRoot()
		{
			CheckDisposed();

			if (DesignMode)
				return;

			if (m_rootb == null)
			{
				base.MakeRoot();

				// Ensure root box has a data access appropriate for view constructors that may query it.
				m_rootb.DataAccess = m_cache.DomainDataByFlid;
			}

			m_vc = new MediaInfoVc(m_cache, m_mediaURIs);

			// We need to pass in a valid Hvo, so we are using the ownerHvo. This call is
			// needed so the VC's Display() will be called.
			m_rootb.SetRootObject(m_ownerHvo, m_vc, kfragRoot, m_rootb.Stylesheet);
			m_rootb.Reconstruct();
		}

		protected override void Dispose(bool disposing)
		{
			if (IsDisposed)
				return;

			base.Dispose(disposing);
		}
	}

	/// <summary>
	/// View constructor that displays the URI strings.
	/// </summary>
	public class MediaInfoVc : FwBaseVc
	{
		private ILcmOwningCollection<ICmMediaURI> m_mediaURIs;

		public MediaInfoVc(LcmCache cache, ILcmOwningCollection<ICmMediaURI> mediaURIs)
		{
			m_cache = cache;
			m_mediaURIs = mediaURIs;
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				case MediaInfoView.kfragRoot:
					if (m_mediaURIs == null)
						return;

					// Make non-editable.
					vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
						(int)FwTextPropVar.ktpvDefault,
						(int)TptEditable.ktptNotEditable);

					// Add the URI strings.
					int userWs = m_cache.WritingSystemFactory.UserWs;
					foreach (var medUri in m_mediaURIs)
					{
						Uri uri = new Uri(medUri.MediaURI);
						string uriString = uri.LocalPath;
						ITsString tss = TsStringUtils.MakeString(uriString, userWs);
						vwenv.OpenParagraph();
						vwenv.AddString(tss);
						vwenv.CloseParagraph();
					}
					break;
				default:
					throw new ArgumentException("Unknown frag in MediaInfoVc", nameof(frag));
			}
		}
	}
}
