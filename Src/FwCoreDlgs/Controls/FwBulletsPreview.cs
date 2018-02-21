// Copyright (c) 2007-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.FwCoreDlgs.Controls
{
	/// <summary>
	/// Preview view in the Bullets and Numbering tab
	/// </summary>
	internal class BulletsPreview : SimpleRootSite
	{
		#region Data members

		// This 'view' displays the string m_tssData by pretending it is property ktagText of
		// object khvoRoot.
		protected internal const int ktagText = 9001; // completely arbitrary, but recognizable.
		protected const int kfragRoot = 8002; // likewise.
		protected const int khvoRoot = 7003; // likewise.

		// Neither of these caches are used by LcmCache.
		// They are only used here.
		protected IVwCacheDa m_CacheDa; // Main cache object
		protected ISilDataAccess m_DataAccess; // Another interface on m_CacheDa.
		BulletsPreviewVc m_vc;
		protected bool m_fUsingTempWsFactory;
		private int m_WritingSystem;
		#endregion // Data members

		#region Constructor/destructor
		/// <summary>
		/// Default constructor
		/// </summary>
		public BulletsPreview()
		{
			m_CacheDa = VwCacheDaClass.Create();
			m_CacheDa.TsStrFactory = TsStringUtils.TsStrFactory;
			m_DataAccess = (ISilDataAccess)m_CacheDa;
			m_vc = new BulletsPreviewVc();

			// So many things blow up so badly if we don't have one of these that I finally decided to just
			// make one, even though it won't always, perhaps not often, be the one we want.
			CreateTempWritingSystemFactory();
			m_DataAccess.WritingSystemFactory = WritingSystemFactory;
			VScroll = false; // no vertical scroll bar visible.
			AutoScroll = false; // not even if the root box is bigger than the window.
		}

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
				ShutDownTempWsFactory(); // Must happen before call to base.
			}

			base.Dispose(disposing);

			if (disposing)
			{
				m_CacheDa?.ClearAllData();
			}

			m_vc = null;
			m_DataAccess = null;
			m_wsf = null;
			if (m_CacheDa != null && Marshal.IsComObject(m_CacheDa))
			{
				Marshal.ReleaseComObject(m_CacheDa);
			}
			m_CacheDa = null;
		}
		#endregion

		#region Temporary writing system factory methods
		/// <summary>
		/// Make a writing system factory that is based on the Languages folder (ICU-based).
		/// This is only used in Designer, tests, and momentarily (during construction) in
		/// production, until the client sets supplies a real one.
		/// </summary>
		private void CreateTempWritingSystemFactory()
		{
			m_wsf = new WritingSystemManager();
			m_fUsingTempWsFactory = true;
		}

		/// <summary>
		/// Shut down the writing system factory and release it explicitly.
		/// </summary>
		private void ShutDownTempWsFactory()
		{
			if (m_fUsingTempWsFactory)
			{
				var disposable = m_wsf as IDisposable;
				disposable?.Dispose();
				m_wsf = null;
				m_fUsingTempWsFactory = false;
			}
		}
		#endregion

		#region Properties
		/// <summary>
		/// The writing system that should be used to construct a TsString out of a string in Text.set.
		/// If one has not been supplied use the User interface writing system from the factory.
		/// </summary>
		[BrowsableAttribute(false), DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public int WritingSystemCode
		{
			get
			{
				if (m_WritingSystem == 0)
				{
					m_WritingSystem = WritingSystemFactory.UserWs;
				}
				return m_WritingSystem;
			}
			set
			{
				m_WritingSystem = value;
			}
		}

		/// <summary>
		/// For this class, if we haven't been given a WSF we create a default one (based on
		/// the registry). (Note this is kind of overkill, since the constructor does this too.
		/// But I left it here in case we change our minds about the constructor.)
		/// </summary>
		[BrowsableAttribute(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public override ILgWritingSystemFactory WritingSystemFactory
		{
			get
			{
				if (base.WritingSystemFactory == null)
				{
					CreateTempWritingSystemFactory();
				}
				return base.WritingSystemFactory;
			}
			set
			{
				if (base.WritingSystemFactory != value)
				{
					ShutDownTempWsFactory();
					// when the writing system factory changes, delete any string that was there
					// and reconstruct the root box.
					base.WritingSystemFactory = value;
					// Enhance JohnT: Base class should probably do this.
					if (m_DataAccess != null)
					{
						m_DataAccess.WritingSystemFactory = value;
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether to display the preview right to left.
		/// </summary>
		public bool IsRightToLeft
		{
			get
			{
				return m_vc != null && m_vc.IsRightToLeft;
			}
			set
			{
				if (m_vc != null)
				{
					m_vc.IsRightToLeft = value;
				}
			}
		}
		#endregion

		#region Overridden rootsite methods
		/// <summary>
		/// Simulate infinite width if needed.
		/// </summary>
		public override int GetAvailWidth(IVwRootBox prootb)
		{
			return ClientRectangle.Width - (HorizMargin * 2);
		}

		/// <summary>
		/// Create the root box and initialize it. We want this one to work even in design mode,
		/// and since we supply the cache and data ourselves, that's possible.
		/// </summary>
		public override void MakeRoot()
		{
			base.MakeRoot();

			m_rootb.DataAccess = m_DataAccess;
			m_rootb.SetRootObject(khvoRoot, m_vc, kfragRoot, null);
		}

		/// <summary>
		/// Gets a value indicating whether the root can be constructed in design mode.
		/// </summary>
		protected override bool AllowPaintingInDesigner => true;
		#endregion

		/// <summary>
		/// Sets the text properties.
		/// </summary>
		internal void SetProps(ITsTextProps propertiesForFirstPreviewParagraph, ITsTextProps propertiesForFollowingPreviewParagraph)
		{
			if (m_vc != null)
			{
				m_vc.SetProps(propertiesForFirstPreviewParagraph, propertiesForFollowingPreviewParagraph);
				RefreshDisplay();
			}
		}
	}
}