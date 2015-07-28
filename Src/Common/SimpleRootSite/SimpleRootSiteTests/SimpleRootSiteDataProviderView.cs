// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SIL.FieldWorks.CacheLight;
using SIL.FieldWorks.Common.COMInterfaces;
using System.Linq;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	public partial class SimpleRootSiteDataProviderView : SimpleRootSiteDataProviderViewBase
	{
		public const int kflidSimpleTsString = 101001;

		public SimpleRootSiteDataProviderView()
		{
			InitializeComponent();
			// We don't actually want to show it, but we need to force the view to create the root
			// box and lay it out so that various test stuff can happen properly.
			Width = 300;
			Height = 307 - 25;
		}

		public SimpleRootSiteDataProviderView(RealDataCache rdc)
			: base(rdc) { }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert the specified paragraph box content and display the view.
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="s">the content to add to the views paragraph</param>
		/// <param name="options">options to configure the view</param>
		/// ------------------------------------------------------------------------------------
		public void ShowForm(int ws, string s, SimpleRootSiteDataProviderVc.DisplayOptions options)
		{
			AddSimpleString(ws, s);
			ShowForm(ws, options);
		}

		private void AddSimpleString(int ws, string s)
		{
			ITsStrFactory tsStrFactory = TsStrFactoryClass.Create();
			VwCache.CacheStringProp(m_hvoRoot, kflidSimpleTsString,
				tsStrFactory.MakeString(s, ws));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up the test form.
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="options"></param>
		/// ------------------------------------------------------------------------------------
		protected void ShowForm(int ws, SimpleRootSiteDataProviderVc.DisplayOptions options)
		{
			MakeRoot(m_hvoRoot, 0, SimpleRootSiteDataProviderBaseVc.kfragRoot, ws, CreateVc(options));
			ShowForm(new DisplayOptions {ReadOnlyView = options.ReadOnlyView});
		}

		protected virtual SimpleRootSiteDataProviderBaseVc CreateVc(SimpleRootSiteDataProviderVc.DisplayOptions options)
		{
			return new SimpleRootSiteDataProviderVc(options);
		}
	}

	/// <summary>
	/// The class that displays the simple string view
	/// </summary>
	public class SimpleRootSiteDataProviderVc : SimpleRootSiteDataProviderBaseVc
	{
		/// <summary>How to display the text</summary>
		public struct DisplayOptions
		{
			/// <summary>
			/// makes the view read only
			/// </summary>
			public bool ReadOnlyView { get; set; }
			/// <summary>View adds a read-only label literal string as a label before
			/// each paragraph</summary>
			public bool LiteralStringLabels { get; set; }
		}

		protected readonly DisplayOptions m_displayOptions;
		private int m_counter = 1;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the SimpleRootSiteDataProviderVc class
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SimpleRootSiteDataProviderVc()
			: this(new DisplayOptions())
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the SimpleRootSiteDataProviderVc class
		/// </summary>
		/// <param name="options"></param>
		/// ------------------------------------------------------------------------------------
		public SimpleRootSiteDataProviderVc(DisplayOptions options)
		{
			m_displayOptions = options;
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
			if (m_displayOptions.LiteralStringLabels)
			{
				ITsStrFactory factory = TsStrFactoryClass.Create();
				vwenv.AddString(factory.MakeString("Label" + m_counter++, m_wsDefault));
			}
			switch (frag)
			{
				case kfragRoot: // the root; Display the paragraph
					AddParagraphBoxContents(vwenv, () =>
					{
						if (m_displayOptions.ReadOnlyView)
						{
							vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
								(int)FwTextPropVar.ktpvEnum,
								(int)TptEditable.ktptNotEditable);
						}
					});
					break;
				default:
					throw new ApplicationException("Unexpected frag in SimpleRootSiteDataProviderVc");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the current paragraph's contents.
		/// </summary>
		/// <param name="vwenv">The view environment</param>
		/// <param name="setParagraphProps"></param>
		/// ------------------------------------------------------------------------------------
		private void AddParagraphBoxContents(IVwEnv vwenv, Action setParagraphProps)
		{
		   AddParagraphBoxContents(vwenv, setParagraphProps, () =>
			   vwenv.AddStringProp(SimpleRootSiteDataProviderView.kflidSimpleTsString, null));
		}

		#endregion
	}

	public abstract class SimpleRootSiteDataProviderViewBase : SimpleRootSite
	{
		protected VwBaseVc m_vc;
		protected readonly int m_hvoRoot;
		/// <summary>Fragment for view constructor</summary>
		protected int m_fragRoot;
		internal const int kclsidOwnerless = 101;

		/// <summary>How to display the text</summary>
		public struct DisplayOptions
		{
			/// <summary>
			/// makes the view read only
			/// </summary>
			public bool ReadOnlyView { get; set; }
		}

		protected ISilDataAccess Cache { get; set; }
		protected IVwCacheDa VwCache { get; set; }

		protected SimpleRootSiteDataProviderViewBase() { Visible = false; }

		protected SimpleRootSiteDataProviderViewBase(RealDataCache rdc)
		{
			Cache = rdc;
			VwCache = rdc;
			m_hvoRoot = Cache.MakeNewObject(kclsidOwnerless, 0, -1, -1);
		}

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

		protected void MakeRoot(int hvoRoot, int flid, int frag, int hvoDefaultWs,
			VwBaseVc vc)
		{
			CheckDisposed();

			if (DesignMode)
				return;

			base.MakeRoot();

			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);

			m_fragRoot = frag;
			// Set up a new view constructor.
			m_vc = vc;
			m_vc.DefaultWs = hvoDefaultWs;

			m_rootb.DataAccess = Cache;
			m_rootb.SetRootObject(hvoRoot, m_vc, frag, m_styleSheet);
		}


		protected void ShowForm(DisplayOptions options)
		{
			Visible = true;
			ReadOnlyView = options.ReadOnlyView;
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

	public class SimpleRootSiteDataProvider_MultiStringView :
		SimpleRootSiteDataProviderViewBase
	{
		public SimpleRootSiteDataProvider_MultiStringView(RealDataCache rdc)
			: base(rdc)
		{
			// We don't actually want to show it, but we need to force the view to create the root
			// box and lay it out so that various test stuff can happen properly.
			Width = 300;
			Height = 307 - 25;
		}

		/// <summary>
		/// caches the data for the view, and then builds and displays the view.
		/// </summary>
		/// <param name="wsToValues">map ws values to string values for caching the MultiString data</param>
		/// <param name="options">The options.</param>
		public void ShowForm(IList<KeyValuePair<int, string>> wsToValues, SimpleRootSiteDataProvider_MultiStringViewVc.DisplayOptions options)
		{
			MultiStringInfo = wsToValues;
			ITsStrFactory tsStrFactory = TsStrFactoryClass.Create();
			foreach (var kvp in wsToValues)
			{
				VwCache.CacheStringAlt(m_hvoRoot, SimpleRootSiteDataProvider_MultiStringViewVc.kflidMultiString, kvp.Key,
					tsStrFactory.MakeString(kvp.Value, kvp.Key));
			}
			var wsOrder = wsToValues.Select(kvPair => kvPair.Key).ToList();
			MakeRoot(m_hvoRoot, 0, SimpleRootSiteDataProviderBaseVc.kfragRoot, 0, CreateVc(options, wsOrder));
			ShowForm(new DisplayOptions {ReadOnlyView = options.ReadOnlyView});
		}

		IList<KeyValuePair<int, string>> MultiStringInfo { get; set; }

		protected virtual SimpleRootSiteDataProviderBaseVc CreateVc(SimpleRootSiteDataProvider_MultiStringViewVc.DisplayOptions options,
			IList<int> wsOrder)
		{
			return new SimpleRootSiteDataProvider_MultiStringViewVc(options, wsOrder);
		}

	}

	/// <summary>
	/// The class that displays the simple string view
	/// </summary>
	public class SimpleRootSiteDataProvider_MultiStringViewVc : SimpleRootSiteDataProviderBaseVc
	{
		public const int kflidMultiString = 101002;

		/// <summary>How to display the text</summary>
		public struct DisplayOptions
		{
			/// <summary>
			/// makes the view read only
			/// </summary>
			public bool ReadOnlyView { get; set; }
			/// <summary>View adds a read-only label literal string as a label before
			/// each paragraph</summary>
			public bool LiteralStringLabels { get; set; }

		}

		private readonly DisplayOptions m_displayOptions;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the SimpleRootSiteDataProviderVc class
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SimpleRootSiteDataProvider_MultiStringViewVc()
			: this(new DisplayOptions())
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the SimpleRootSiteDataProviderVc class
		/// </summary>
		/// <param name="options"></param>
		/// ------------------------------------------------------------------------------------
		public SimpleRootSiteDataProvider_MultiStringViewVc(DisplayOptions options)
		{
			m_displayOptions = options;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleRootSiteDataProvider_MultiStringViewVc"/> class.
		/// </summary>
		/// <param name="options">The options.</param>
		/// <param name="wsOrder">a list of writing systems in the order the strings should show in the view</param>
		public SimpleRootSiteDataProvider_MultiStringViewVc(DisplayOptions options, IList<int> wsOrder)
			: this(options)
		{
			WsOrder = wsOrder;
		}

		IList<int> WsOrder { get; set; }

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
			switch (frag)
			{
				case kfragRoot: // the root; Display the paragraph
					using (new VwConstructorServices.ParagraphBoxHelper(vwenv,
							() =>
								{
									if (m_displayOptions.ReadOnlyView)
									{
										vwenv.set_IntProperty(
										(int)FwTextPropType.ktptEditable,
										(int)FwTextPropVar.ktpvEnum,
										(int)TptEditable.ktptNotEditable);
									}
								}))
					{
						if (m_displayOptions.LiteralStringLabels)
						{
							using (new VwConstructorServices.InnerPileHelper(vwenv))
							{
								ITsStrFactory factory = TsStrFactoryClass.Create();
								foreach (var ws in WsOrder)
								{
									using (new VwConstructorServices.ParagraphBoxHelper(vwenv))
									{
										if (m_displayOptions.LiteralStringLabels)
											vwenv.AddString(factory.MakeString("Label" + ws, ws));
									}
								}
							}
						}
						using (new VwConstructorServices.InnerPileHelper(vwenv))
						{
							ITsStrFactory factory = TsStrFactoryClass.Create();
							foreach (var ws in WsOrder)
							{
								using (new VwConstructorServices.ParagraphBoxHelper(vwenv))
								{
									//if (m_displayOptions.LiteralStringLabels)
									//    vwenv.AddString(factory.MakeString("Label" + ws, ws));
									vwenv.AddStringAltMember(kflidMultiString, ws, null);
								}
							}
						}
					}

					break;
				default:
					throw new ApplicationException("Unexpected frag in SimpleRootSiteDataProviderVc");
			}
		}

		#endregion
	}

	/// <summary>
	/// The class that displays the draft view.
	/// </summary>
	public abstract class SimpleRootSiteDataProviderBaseVc : VwBaseVc
	{
		/// <summary></summary>
		public const int kEstimatedParaHeight = 30;

		#region Overridden methods

		/// <summary>
		/// Adds the current paragraph's contents.
		/// </summary>
		/// <param name="vwenv">The view environment</param>
		/// <param name="setParagraphProps">The set paragraph props.</param>
		/// <param name="addContents">add contents to the paragraph flow object.</param>
		/// ------------------------------------------------------------------------------------
		/// ------------------------------------------------------------------------------------
		protected void AddParagraphBoxContents(IVwEnv vwenv, Action setParagraphProps, Action addContents)
		{
			if (setParagraphProps != null)
				setParagraphProps();
			if (addContents != null)
				addContents();

		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This routine is used to estimate the height of an item. The item will be one of
		/// those you have added to the environment using AddLazyItems. Note that the calling
		/// code does NOT ensure that data for displaying the item in question has been loaded.
		/// The first three arguments are as for Display, that is, you are being asked to
		/// estimate how much vertical space is needed to display this item in the available width.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// <param name="dxAvailWidth"></param>
		/// <returns>Height of an item</returns>
		/// ------------------------------------------------------------------------------------
		public override int EstimateHeight(int hvo, int frag, int dxAvailWidth)
		{
			//			Debug.WriteLine(string.Format("Estimateheight for hvo: {0}, frag:{1}", hvo, frag));
			return kEstimatedParaHeight;  // just give any arbitrary number
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the string that should be displayed in place of an object character associated
		/// with the specified GUID. This dummy version just returns something similar to what
		/// TE would normally put in for an alpha footnote.
		/// </summary>
		/// <param name="bstrGuid"></param>
		/// <returns>non-breaking space</returns>
		/// ------------------------------------------------------------------------------------
		public override ITsString GetStrForGuid(string bstrGuid)
		{
			TsStrFactory strFactory = TsStrFactoryClass.Create();
			return strFactory.MakeString("\uFEFFa", m_wsDefault);
		}
		#endregion

		public const int kfragRoot = 1;
	}
}
