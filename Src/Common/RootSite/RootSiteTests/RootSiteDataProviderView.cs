// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Automation.Provider;
using System.Windows.Forms;
using SIL.FieldWorks.CacheLight;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests;

namespace SIL.FieldWorks.Common.RootSites
{
	public partial class RootSiteDataProviderView : SimpleRootSiteDataProviderView
	{
		public RootSiteDataProviderView(RealDataCache rdc)
			: base(rdc) { }

		protected override SimpleRootSiteDataProviderBaseVc CreateVc(SimpleRootSiteDataProviderVc.DisplayOptions options)
		{
			return new RootSiteDataProviderVc(options, Cache);
		}
	}

	/// <summary>
	/// The class that displays the simple string view
	/// </summary>
	public class RootSiteDataProviderVc : SimpleRootSiteDataProviderVc
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the SimpleRootSiteDataProviderVc class
		/// </summary>
		/// <param name="options"></param>
		/// <param name="sda"></param>
		/// ------------------------------------------------------------------------------------
		public RootSiteDataProviderVc(DisplayOptions options, ISilDataAccess sda)
			: base(options)
		{
			// DataAccess = sda;
		}

		// ISilDataAccess DataAccess { get; set; }

		}

	public class RootSiteDataProvider_MultiStringView :
		SimpleRootSiteDataProvider_MultiStringView
	{
		public RootSiteDataProvider_MultiStringView(RealDataCache rdc)
			: base(rdc)
		{
		}


		protected override SimpleRootSiteDataProviderBaseVc CreateVc(
			SimpleRootSiteDataProvider_MultiStringViewVc.DisplayOptions options,
			IList<int> wsOrder)
		{
			Vc = new RootSiteDataProvider_MultiStringViewVc(Cache, options, wsOrder);
			return Vc;
		}

		private RootSiteDataProvider_MultiStringViewVc Vc { get; set; }

		/// <summary>
		/// Creates the UI automation edit controls.
		/// </summary>
		/// <param name="fragmentRoot">The fragment root.</param>
		/// <returns></returns>
		internal IList<IRawElementProviderFragment> CreateUIAutomationEditControls(IChildControlNavigation fragmentRoot)
		{
		return RootSiteServices.CreateUIAutomationEditControls(fragmentRoot, RootBox, Vc, Cache, m_hvoRoot,
														SimpleRootSiteDataProviderBaseVc.kfragRoot);
		}

		internal IList<IVwSelection> CollectEditableStringPropSelections()
		{
		return CollectorEnvServices.CollectEditableSelectionPoints(RootBox).ToList();
		}
	}

	public class RootSiteDataProviderUtils
	{

	}

	/// <summary>
	/// The class that displays the simple string view
	/// </summary>
	public class RootSiteDataProvider_MultiStringViewVc : SimpleRootSiteDataProvider_MultiStringViewVc
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RootSiteDataProvider_MultiStringViewVc"/> class.
		/// </summary>
		/// <param name="sda"></param>
		/// <param name="options">The options.</param>
		/// <param name="wsOrder">a list of writing systems in the order the strings should show in the view</param>
		public RootSiteDataProvider_MultiStringViewVc(ISilDataAccess sda, DisplayOptions options, IList<int> wsOrder)
			: base(options, wsOrder)
		{
		}

		#region Overridden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// ------------------------------------------------------------------------------------
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			var poiCollector = vwenv as PointsOfInterestCollectorEnv;
			base.Display(vwenv, hvo, frag);
			if (poiCollector != null)
				PointsOfInterest = poiCollector.PointsOfInterest;
		}

		internal IList<CollectorEnv.LocationInfo> PointsOfInterest { get; set; }

		#endregion
	}


	/// <summary>
	/// this was basically copied from: <see cref="T:SimpleRootSiteTests.SimpleRootSiteDataProviderViewBase"/>
	/// so that we could use RootSite as a base.
	/// </summary>
	public class RootSiteDataProviderViewBase : RootSite
	{
		protected VwBaseVc m_vc;
		protected readonly int m_hvoRoot;

		/// <summary>Fragment for view constructor</summary>
		protected int m_fragRoot;
		internal const int kclsidOwnerless = 101;
		internal const int kflidSimpleTsString = 101001;

		private readonly ISilDataAccess m_sda;
		internal IVwCacheDa VwCache { get; set; }

		protected RootSiteDataProviderViewBase() { Visible = false; }

		public RootSiteDataProviderViewBase(ISilDataAccess sda)
			: this(sda, sda.MakeNewObject(kclsidOwnerless, 0, -1, -1))
		{

		}

		public RootSiteDataProviderViewBase(ISilDataAccess sda, int hvoRoot)
		{
			m_sda = sda;
			VwCache = sda as IVwCacheDa;
			m_hvoRoot = hvoRoot;

			// We don't actually want to show it, but we need to force the view to create the root
			// box and lay it out so that various test stuff can happen properly.
			Width = 300;
			Height = 307 - 25;
		}

		/// <summary/>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				var disposable = m_vc as IDisposable;
				if (disposable != null)
					disposable.Dispose();
			}
			m_vc = null;
			base.Dispose(disposing);
		}

		internal int RootHvo { get {return m_hvoRoot;} }

		/// <summary>
		///
		/// </summary>
		/// <param name="fragRoot"></param>
		/// <param name="createVc"></param>
		public void MakeRoot(int fragRoot, Func<VwBaseVc> createVc)
		{
			CheckDisposed();

			if (DesignMode)
				return;

			base.MakeRoot();

			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);

			m_fragRoot = fragRoot;
			// Set up a new view constructor.
			m_vc = createVc();

			m_rootb.DataAccess = m_sda;
			m_rootb.SetRootObject(m_hvoRoot, m_vc, fragRoot, m_styleSheet);
		}

		public void ShowForm()
		{
			Visible = true;
			if (!IsHandleCreated)
			{
				// making a selection should help us get a handle created, if it's not already
				try
				{
					RootBox.MakeSimpleSel(true, true, false, true);
				}
				catch (COMException)
				{
					// We ignore failures since the text window may be empty, in which case making a
					// selection is impossible.
				}
			}
			else
			{
				CallLayout();
			}
			AutoScrollPosition = new Point(0, 0);
		}

		internal void CallLayout()
		{
			base.OnLayout(new LayoutEventArgs(this, string.Empty));
		}
	}

	internal class NoPictureVc : SimpleRootSiteDataProviderBaseVc
	{
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				case kfragRoot :
					// draw a paragraph box but don't add a picture
					using (new VwConstructorServices.ParagraphBoxHelper(vwenv,
							null))
					{
					}
					break;
			}
		}
	}

	internal class OnePictureVc : SimpleRootSiteDataProviderBaseVc, IDisposable
	{
		private readonly ComPictureWrapper m_picture;
		internal const int ktagPicture = 101999;
		private const int kmpIconMargin = 3000;

		public OnePictureVc()
		{
			m_picture = VwConstructorServices.ConvertImageToComPicture(Properties.Resources.InterlinPopupArrow);
		}

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~OnePictureVc()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed
			{
			get;
			private set;
		}

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().ToString() + " *******");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				if (m_picture != null)
					m_picture.Dispose();
			}
			IsDisposed = true;
		}
		#endregion

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				case kfragRoot:
					// draw a paragraph box but don't add a picture
					using (new VwConstructorServices.ParagraphBoxHelper(vwenv,
							null))
					{
						vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing,
						(int)FwTextPropVar.ktpvMilliPoint, kmpIconMargin);
						vwenv.set_IntProperty((int)FwTextPropType.ktptOffset,
							(int)FwTextPropVar.ktpvMilliPoint, -2500);
						vwenv.AddPicture(m_picture.Picture, ktagPicture, 0, 0);
					}
					break;
			}
		}
	}

	internal class OnePictureOneEditBoxVc : SimpleRootSiteDataProviderBaseVc, IDisposable
	{
		private readonly ComPictureWrapper m_picture;
		internal const int ktagPicture = 101999;
		const int kmpIconMargin = 3000;

		public OnePictureOneEditBoxVc()
		{
			m_picture = VwConstructorServices.ConvertImageToComPicture(Properties.Resources.InterlinPopupArrow);
		}

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~OnePictureOneEditBoxVc()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed
		{
			get;
			private set;
		}

		/// <summary/>
		public void Dispose()
			{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().ToString() + " *******");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				if (m_picture != null)
					m_picture.Dispose();
			}
			IsDisposed = true;
		}
		#endregion

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				case kfragRoot:
					// draw a paragraph box but don't add a picture
					using (new VwConstructorServices.ParagraphBoxHelper(vwenv,
							null))
					{
						vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing,
						(int)FwTextPropVar.ktpvMilliPoint, kmpIconMargin);
						vwenv.set_IntProperty((int)FwTextPropType.ktptOffset,
							(int)FwTextPropVar.ktpvMilliPoint, -2500);
						vwenv.AddPicture(m_picture.Picture, ktagPicture, 0, 0);

						AddParagraphBoxContents(vwenv,
							() => vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
																				   (int)FwTextPropVar.ktpvEnum,
																				   (int)TptEditable.ktptIsEditable),
							() => vwenv.AddStringProp(SimpleRootSiteDataProviderView.kflidSimpleTsString, null));
					}
					break;
			}
		}


	}
}
