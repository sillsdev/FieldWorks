using System;
using System.Windows.Forms;
using XCore;

namespace SIL.FieldWorks.Common.UIAdapters
{
	/// <summary>
	/// Side Bar and Information Bar Interface.
	/// </summary>
	public interface ISIBInterface
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sbContainer">Sidebar Container</param>
		/// <param name="ibContainer">Info. Bar Container</param>
		/// <param name="mediator"></param>
		/// ------------------------------------------------------------------------------------
		void Initialize(Control sbContainer, Control ibContainer, Mediator mediator);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="tabProps"></param>
		/// ------------------------------------------------------------------------------------
		void AddTab(SBTabProperties tabProps);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="tabName">Name of the tab to which an item will be added.</param>
		/// <param name="itemProps">Properties of the new tab item.</param>
		/// ------------------------------------------------------------------------------------
		void AddTabItem(string tabName, SBTabItemProperties itemProps);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="tabIndex">Index of the tab to which an item will be added.</param>
		/// <param name="itemProps">Properties of the new tab item.</param>
		/// ------------------------------------------------------------------------------------
		void AddTabItem(int tabIndex, SBTabItemProperties itemProps);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the current sidebar tab.
		/// </summary>
		/// <param name="tabIndex">index of tab to make current.</param>
		/// <param name="generateEvents"><c>true</c> to cause events to be fired when setting
		/// the current sidebar tab (like when the user clicks on an item). Otherwise,
		/// <c>false</c>.</param>
		/// ------------------------------------------------------------------------------------
		void SetCurrentTab(int tabIndex, bool generateEvents);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the current sidebar tab.
		/// </summary>
		/// <param name="tabName">Name of the tab to make current.</param>
		/// <param name="generateEvents"><c>true</c> to cause events to be fired when setting
		/// the current sidebar tab (like when the user clicks on an item). Otherwise,
		/// <c>false</c>.</param>
		/// ------------------------------------------------------------------------------------
		void SetCurrentTab(string tabName, bool generateEvents);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the current sidebar tab item.
		/// </summary>
		/// <param name="tabIndex">index of tab containing the tab item to make current.</param>
		/// <param name="itemName">Name of tab item to make current.</param>
		/// <param name="generateEvents"><c>true</c> to cause events to be fired when setting
		/// the current sidebar tab item (like when the user clicks on an item). Otherwise,
		/// <c>false</c>.</param>
		/// ------------------------------------------------------------------------------------
		void SetCurrentTabItem(int tabIndex, string itemName, bool generateEvents);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the current sidebar tab item.
		/// </summary>
		/// <param name="tabName">Name of tab containing the tab item to make current.</param>
		/// <param name="itemName">Name of tab item to make current.</param>
		/// <param name="generateEvents"><c>true</c> to cause events to be fired when setting
		/// the current sidebar tab item (like when the user clicks on an item). Otherwise,
		/// <c>false</c>.</param>
		/// ------------------------------------------------------------------------------------
		void SetCurrentTabItem(string tabName, string itemName, bool generateEvents);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tells the side bar adapter to setup it's menus so they show up on the application's
		/// view menu. This method should be called after all the tabs and tab items have been
		/// created.
		/// </summary>
		/// <param name="adapter">ITMAdapter used by the application.</param>
		/// <param name="insertBeforeItem">Name of the menu item before which the sidebar
		/// menus will be added.</param>
		/// ------------------------------------------------------------------------------------
		void SetupViewMenuForSideBarTabs(ITMAdapter adapter, string insertBeforeItem);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the properties for the specified item on the specified tab.
		/// </summary>
		/// <param name="tabName">Tab containing the item.</param>
		/// <param name="itemName">Item whose properties are being requested.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		SBTabItemProperties GetTabItemProperties(string tabName, string itemName);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tells the adapter to load it's settings from the specified keys.
		/// </summary>
		/// <param name="key"></param>
		/// ------------------------------------------------------------------------------------
		void LoadSettings(Microsoft.Win32.RegistryKey key);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tells the adapter to save it's settings to the specified keys.
		/// </summary>
		/// <param name="key"></param>
		/// ------------------------------------------------------------------------------------
		void SaveSettings(Microsoft.Win32.RegistryKey key);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the index of the image used for the context menu item that allows the
		/// user to switch to large icons on the side bar. It is assumed the image for those
		/// menu items is found in the same image list that's specified for the TabImageList
		/// property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int LargeIconModeImageIndex
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the index of the image used for the context menu item that allows the
		/// user to switch to small icons on the side bar. It is assumed the image for those
		/// menu items is found in the same image list that's specified for the TabImageList
		/// property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int SmallIconModeImageIndex
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the visible state of the sidebar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool SideBarVisible
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the visible state of the info. bar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool InformationBarVisible
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the array of tabs contained in the sidebar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		SBTabProperties[] Tabs
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the image list for small icon mode for the individual items on the
		/// side bar. A single image list is used for the items on all tabs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		ImageList ItemImageListSmall
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the image list for large icon mode for the individual items on the
		/// side bar. A single image list is used for the items on all tabs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		ImageList ItemImageListLarge
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the image list for the side bar tab icons that go on the tab button
		/// (i.e. next to the tab's text) and the information bar buttons.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		ImageList TabImageList
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current tab.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		SBTabProperties CurrentTabProperties
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current tab item.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		SBTabItemProperties CurrentTabItemProperties
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the text on the information bar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string InformationBarText
		{
			get;
			set;
		}
	}
}
