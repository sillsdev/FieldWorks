using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.UIAdapters;
using XCore;
using SIL.FieldWorks.TE;
using SIL.FieldWorks.Common.Controls;

namespace UIAdaptersTestClient
{
	/// <summary>
	/// Main window of test application, to hold the side bar.
	/// Contains some code from TeMainWnd.cs and FwMainWnd.cs.
	/// </summary>
	public class MainWindow : Form
	{
		private System.ComponentModel.IContainer components = null;
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
				components.Dispose();
			base.Dispose(disposing);
		}

		public MainWindow()
		{

			this.Height = 800;

			components = new System.ComponentModel.Container();
			m_mediator = new Mediator();

			CreateSideBarInfoBarAdapter();
			if (!(components is FwContainer))
				components = new FwContainer(components);
			components.Add(this);

			SetupSideBarInfoBar();

			AddDraftView();
			AddVerticalView();
			AddVerticalView2();
		}

		/// <summary></summary>
		protected ISIBInterface m_sibAdapter;
		/// <summary></summary>
		protected Panel m_infoBarContainer;
		private Mediator m_mediator;

		/// <summary>The default width of the side bar</summary>
		protected const int kDefaultSideBarWidth = 200;
		protected const int defaultSideBarHeight = 700;
		/// <summary>Internal name of default button in the Scripture taskbar tab</summary>
		private const string kScrDraftViewSBItemName = "ScrDraftViewItem";
		/// <summary>Internal name of vertical draft view button in the Scripture taskbar tab</summary>
		private const string kScrVerticalViewSBItemName = "ScrVertDraftViewItem";

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// - from FwMainWnd.cs
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create and initializes a new sidebar/info. bar adapter. - from FwMainWnd.cs
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void CreateSideBarInfoBarAdapter()
		{
			m_sibAdapter = AdapterHelper.CreateSideBarInfoBarAdapter();

			if (m_sibAdapter != null)
				m_sibAdapter.Initialize(this/*m_sideBarContainer*/, m_infoBarContainer, m_mediator);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the sidebar/info. bar adapter - from FwMainWnd.cs
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ISIBInterface SIBAdapter {
			get {
				CheckDisposed();
				return m_sibAdapter;
			}
		}

		/// <summary>Internal name of default tab on the sidebar - from TeMainWnd.cs</summary>
		private const string kScrSBTabName = "TabScripture";
		/// <summary>Internal name of back translation tab on the sidebar - from TeMainWnd.cs</summary>
		private const string kBTSBTabName = "TabBackTrans";
		/// <summary>Internal name of checking tab on the sidebar - from TeMainWnd.cs</summary>
		private const string kChkSBTabName = "TabChecking";
		/// <summary>Internal name of publications tab on the sidebar - from TeMainWnd.cs</summary>
		private const string kPubSBTabName = "TabPubs";

		/// <summary>
		/// Setup the sidebar/info. bar adapter. - from TeMainWnd.cs
		/// </summary>
		private void SetupSideBarInfoBar()
		{
			// Null when running tests.
			if (SIBAdapter == null) {
				return;
			}

			SIBAdapter.ItemImageListLarge = TeResourceHelper.TeSideBarLargeImages;
			SIBAdapter.ItemImageListSmall = TeResourceHelper.TeSideBarSmallImages;
			SIBAdapter.TabImageList = TeResourceHelper.TeSideBarTabImages;
			SIBAdapter.LargeIconModeImageIndex = 4;
			SIBAdapter.SmallIconModeImageIndex = 5;

			string cfgMsg = "SideBarConfigure";
			string cfgText = TeResourceHelper.GetResourceString("kstidSideBarConfigureItem");
			string fmttooltip = TeResourceHelper.GetResourceString("kstidInfoBarButtonTooltipFormat");

			// Add the scripture tab.
			SBTabProperties tabProps = new SBTabProperties();
			tabProps.Name = kScrSBTabName;
			tabProps.Text = TeResourceHelper.GetResourceString("kstidScriptureTask");
			tabProps.Message = "SideBarTabClicked";
			tabProps.ConfigureMessage = cfgMsg;
			tabProps.ConfigureMenuText = cfgText;
			tabProps.InfoBarButtonToolTipFormat = fmttooltip;
			tabProps.ImageIndex = 0;
			SIBAdapter.AddTab(tabProps);

			// Add the back translation tab.
			tabProps = new SBTabProperties();
			tabProps.Name = kBTSBTabName;
			tabProps.Text = TeResourceHelper.GetResourceString("kstidBackTransTask");
			tabProps.Message = "SideBarTabClicked";
			tabProps.ConfigureMessage = cfgMsg;
			tabProps.ConfigureMenuText = cfgText;
			tabProps.InfoBarButtonToolTipFormat = fmttooltip;
			tabProps.ImageIndex = 1;
			SIBAdapter.AddTab(tabProps);

			// Add the checking tab.
			tabProps = new SBTabProperties();
			tabProps.Name = kChkSBTabName;
			tabProps.Text = TeResourceHelper.GetResourceString("kstidCheckingTask");
			tabProps.Message = "SideBarTabClicked";
			tabProps.ConfigureMessage = cfgMsg;
			tabProps.ConfigureMenuText = cfgText;
			tabProps.InfoBarButtonToolTipFormat = fmttooltip;
			tabProps.ImageIndex = 2;
			SIBAdapter.AddTab(tabProps);

			// Add the publications tab.
			tabProps = new SBTabProperties();
			tabProps.Name = kPubSBTabName;
			tabProps.Text = TeResourceHelper.GetResourceString("kstidPublicationsTask");
			tabProps.Message = "SideBarTabClicked";
			tabProps.ConfigureMessage = cfgMsg;
			tabProps.ConfigureMenuText = cfgText;
			tabProps.InfoBarButtonToolTipFormat = fmttooltip;
			tabProps.Enabled = false;
			tabProps.ImageIndex = 3;
			SIBAdapter.AddTab(tabProps);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the Scripture/Draft View - from TeMainWnd.cs
		/// </summary>
		/// <param name="userView"></param>
		/// ------------------------------------------------------------------------------------
		protected virtual void AddDraftView()//IUserView userView)
		{
			// Add this user view to the Scripture sidebar tab.
			SBTabItemProperties itemProps = new SBTabItemProperties(this);
			itemProps.Name = kScrDraftViewSBItemName;
			itemProps.Text = "Parallel Print Layout";
			itemProps.ImageIndex = (int)TeResourceHelper.SideBarIndices.Draft;
			//itemProps.Tag = TeViewType.DraftView;
			itemProps.Message = "SwitchActiveView";
			AddSideBarTabItem(kScrSBTabName, itemProps);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds tab items to the sidebar (if the adapter is not null) via the sidebar/info.
		/// bar adapter. When running tests, the adapter will be null. - from FwMainWnd.cs
		/// </summary>
		/// <param name="tab"></param>
		/// <param name="itemProps"></param>
		/// ------------------------------------------------------------------------------------
		protected void AddSideBarTabItem(string tab, SBTabItemProperties itemProps)
		{
			if (SIBAdapter != null)
				SIBAdapter.AddTabItem(tab, itemProps);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the Scripture/Vertical View. - from TeMainWnd.cs
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void AddVerticalView()//IUserView userView)
		{
			// Add this user view to the Scripture sidebar tab.
			SBTabItemProperties itemProps = new SBTabItemProperties(this);
			itemProps.Name = kScrVerticalViewSBItemName;
			itemProps.Text = "VEdit";
			itemProps.ImageIndex = (int)TeResourceHelper.SideBarIndices.Draft; // should maybe be some new constant?
		//	itemProps.Tag = TeViewType.VerticalView;
			itemProps.Message = "SwitchActiveView";
			AddSideBarTabItem(kBTSBTabName, itemProps);
		}

		protected virtual void AddVerticalView2()//IUserView userView)
		{
			// Add this user view to the Scripture sidebar tab.
			SBTabItemProperties itemProps = new SBTabItemProperties(this);
			itemProps.Name = kScrVerticalViewSBItemName+"2";
			itemProps.Text = "VEdit2";
			itemProps.ImageIndex = (int)TeResourceHelper.SideBarIndices.Draft;
		//	itemProps.Tag = TeViewType.VerticalView;
			itemProps.Message = "SwitchActiveView";
			AddSideBarTabItem(kBTSBTabName, itemProps);
		}
	}
}