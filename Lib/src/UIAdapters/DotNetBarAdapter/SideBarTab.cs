// ---------------------------------------------------------------------------------------------
// Copyright (c) 2005-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: SideBarTab.cs
// Responsibility: DavidO
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using DevComponents.DotNetBar;
using XCore;
using System.Collections.Generic;

namespace SIL.FieldWorks.Common.UIAdapters
{
	#region Helper struct for Tag
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Helper class, instances of which are stored in the tag property of side bar buttons
	/// and hold all additional information (beyond the properties inherent in a button) we
	/// need for a button.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class TagHelper
	{
		/// <summary>true indicates that event should always be fired, even if the button
		/// was the last one clicked, false if button fires only once.</summary>
		internal bool ClickAlways;
		/// <summary>The message that is sent if the button is clicked.</summary>
		internal string Message;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="_clickAlways"></param>
		/// <param name="_message"></param>
		/// ------------------------------------------------------------------------------------
		internal TagHelper(bool _clickAlways, string _message)
		{
			ClickAlways = _clickAlways;
			Message = _message;
		}

	}
	#endregion

	#region SideBarTab Class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A SideBarTab is one of the buttons (and it's associated panel) that's in the stack of
	/// buttons at the bottom of a DotNetBar navigation pane.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class SideBarTab : ButtonItem
	{
		private bool m_supressAutoEvent = false;
		private string m_message;
		private SIBAdapter m_owningAdapter;
		private Mediator m_mediator;
		private ButtonItem m_menu;
		private HeaderButton m_hdrMenuItem;
		private ButtonItem m_cfgMenuItem;
		private PanelEx m_infoBarButton;
		private ItemPanel m_itemPanel;
		private ButtonItem m_prevSelectedItem = null;
		private ImageList m_largeImageList;
		private ImageList m_smallImageList;
		private string m_infoBarButtonToolTipFormat;
		private object m_tag;

		#region Construction
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a SideBarTab
		/// </summary>
		/// <param name="owningAdapter"></param>
		/// <param name="mediator"></param>
		/// <param name="tabProps"></param>
		/// ------------------------------------------------------------------------------------
		internal SideBarTab(SIBAdapter owningAdapter, Mediator mediator, SBTabProperties tabProps) :
			base(tabProps.Name, tabProps.Text)
		{
			m_owningAdapter = owningAdapter;
			m_mediator = mediator;
			m_message = tabProps.Message;
			m_tag = tabProps.Tag;
			m_infoBarButtonToolTipFormat = tabProps.InfoBarButtonToolTipFormat;

			ImageIndex = tabProps.ImageIndex;
			OptionGroup = "FwNavPanelTabs";
			ButtonStyle = eButtonStyle.ImageAndText;
			ThemeAware = owningAdapter.m_dnbMngr.IsThemeActive;
			Enabled = tabProps.Enabled;
			owningAdapter.m_navPane.Items.Add(this);

			// This has to be done after being added to the pane's item collection.

			// Add the panel (above the tab) that goes with the tab. This is a control
			// above the stack of buttons.
			NavigationPanePanel panel = new NavigationPanePanel();
			panel.Name = tabProps.Name;
			panel.ParentItem = this;
			panel.Dock = DockStyle.Fill;
			owningAdapter.m_navPane.Controls.Add(panel);

			// Add to the panel that holds the DNB toolbar that holds the tab items.
			m_itemPanel = new ItemPanel(Name, owningAdapter);
			m_itemPanel.ImageListSmall = m_smallImageList;
			m_itemPanel.ImageListLarge = m_largeImageList;
			m_itemPanel.SmallIconMode = tabProps.SmallIconMode;
			panel.Controls.Add(m_itemPanel);

			InitMenuAndInfoBarButton(tabProps);

			Tooltip = Text;
		}

		#endregion

		#region Setup info. bar button and the tab's menu.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitMenuAndInfoBarButton(SBTabProperties tabProps)
		{
			m_infoBarButton = new PanelEx();
			m_infoBarButton.Name = Name;
			m_infoBarButton.Style.BackColor1.Alpha = 255;
			m_infoBarButton.Style.BackColor2.Alpha = 255;
			m_infoBarButton.Style.BackgroundImageAlpha = 255;
			m_infoBarButton.Style.BackgroundImagePosition = eBackgroundImagePosition.Center;
			m_infoBarButton.StyleMouseOver.BackgroundImageAlpha = 255;
			m_infoBarButton.StyleMouseOver.BorderColor.Color = SystemColors.ControlDarkDark;
			m_infoBarButton.StyleMouseOver.BorderWidth = 1;
			m_infoBarButton.StyleMouseOver.Border = eBorderType.Raised;
			m_infoBarButton.StyleMouseDown.BackgroundImageAlpha = 255;
			m_infoBarButton.StyleMouseDown.BorderColor.Color = SystemColors.ControlDarkDark;
			m_infoBarButton.StyleMouseDown.BorderWidth = 1;
			m_infoBarButton.StyleMouseDown.Border = eBorderType.Sunken;
			m_infoBarButton.Anchor = AnchorStyles.Right;
			m_infoBarButton.Enabled = Enabled;
			m_infoBarButton.Click += new EventHandler(m_infoBarButton_Click);
			m_infoBarButton.Tag = this;
			UpdateInfoBarButtonImage();

			m_menu = new ButtonItem(Name, Text);
			m_menu.Enabled = Enabled;
			m_menu.PopupType = ePopupType.Menu;
			m_menu.PopupOpen +=	new DotNetBarManager.PopupOpenEventHandler(Menu_PopupOpen);
			m_menu.PopupClose += new EventHandler(Menu_PopupClose);

			// When m_menu pops-up as a result of clicking on an info. bar button, then this
			// header button is visible. However, when m_menu pops-up from the View menu,
			// this header button is hidden.
			m_hdrMenuItem = new HeaderButton(Name, Text);
			m_hdrMenuItem.ImageIndex = -1;
			m_hdrMenuItem.Visible = false;
			m_menu.SubItems.Add(m_hdrMenuItem);

			// Create the configure menu item. This will be made visible when shown from the
			// View menu but not when menu is popped-up from the info. bar button.
			m_cfgMenuItem = new ButtonItem(Name + "Config", tabProps.ConfigureMenuText);
			m_cfgMenuItem.Tag = new TagHelper(false, tabProps.ConfigureMessage);
			m_cfgMenuItem.Visible = true;
			m_cfgMenuItem.BeginGroup = true;
			m_cfgMenuItem.Click += new EventHandler(HandleMenuItemClick);
			m_menu.SubItems.Add(m_cfgMenuItem);
		}

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the tab's menu whose submenu items are those associated with each item on
		/// the tab.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal ButtonItem Menu
		{
			get {return m_menu;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the tab's configure menu item.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal ButtonItem ConfigureMenuItem
		{
			get {return m_cfgMenuItem;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the tooltip for the tab's information bar button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal string InfoBarButtonToolTipText
		{
			get
			{
				// If we're the current tab, then build a string composed
				// of the tab's text and the tab's current item text.
				if (Checked)
				{
					string format = m_infoBarButtonToolTipFormat;
					string currItemText = (CurrentItemProps == null ? null : CurrentItemProps.Text);

					// If there
					if (format != null && format != string.Empty &&
						currItemText != null && currItemText != string.Empty)
					{
						return string.Format(format, Text, currItemText);
					}
				}

				return Text;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the command message for the tab.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal string Message
		{
			get {return m_message;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the tab is in small icon mode.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool SmallIconMode
		{
			get {return m_itemPanel.SmallIconMode;}
			set {m_itemPanel.SmallIconMode = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the information bar button associated with the tab.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal PanelEx InfoBarButton
		{
			get {return m_infoBarButton;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal ImageList ItemImageListSmall
		{
			set
			{
				m_smallImageList = value;
				m_itemPanel.ImageListSmall = value;
				UpdateMenuImages();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal ImageList ItemImageListLarge
		{
			set
			{
				m_largeImageList = value;
				m_itemPanel.ImageListLarge = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the properties for the tab.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal SBTabProperties Properties
		{
			get
			{
				SBTabProperties tabProps = new SBTabProperties();
				tabProps.Name = Name;
				tabProps.Text = Text;
				tabProps.Enabled = Enabled;
				tabProps.ImageIndex = ImageIndex;
				tabProps.SmallIconMode = SmallIconMode;
				tabProps.Message = m_message;
				tabProps.ConfigureMenuText = m_cfgMenuItem.Text;
				tabProps.ConfigureMessage = ((TagHelper)m_cfgMenuItem.Tag).Message;
				tabProps.InfoBarButtonToolTipFormat = m_infoBarButtonToolTipFormat;
				tabProps.CurrentTabItem = m_itemPanel.CurrentItemName;
				tabProps.Tag = m_tag;
				tabProps.Items = m_itemPanel.AllItemProps;
				return tabProps;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal SBTabItemProperties CurrentItemProps
		{
			get	{return m_itemPanel.CurrentItemProps;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the information for the tab's menu in a TMItemProperties object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal TMItemProperties MenuItemProperties
		{
			get
			{
				TMItemProperties itemProps = new TMItemProperties();
				itemProps.Message = m_message;
				itemProps.Name = m_menu.Name;
				itemProps.Text = m_menu.Text;
				itemProps.Enabled = true;
				itemProps.Visible = true;
				itemProps.Update = true;
				return itemProps;
			}
		}
		#endregion

		#region Internal Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateMenuImages()
		{
			if (m_smallImageList == null)
				return;

			foreach (ButtonItem item in m_menu.SubItems)
			{
				if (item.ImageIndex >= 0 && item.ImageIndex < m_smallImageList.Images.Count)
					item.Image = m_smallImageList.Images[item.ImageIndex];
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void UpdateInfoBarButtonImage()
		{
			Image img;

			if (m_owningAdapter.m_navPane.Images != null && ImageIndex >= 0 &&
				ImageIndex < m_owningAdapter.m_navPane.Images.Images.Count)
			{
				img = m_owningAdapter.m_navPane.Images.Images[ImageIndex];
			}
			else
			{
				Bitmap bmp = new Bitmap(GetType(), "DefaultInfoBarButtonImage.bmp");
				bmp.MakeTransparent(Color.Magenta);
				img = bmp;
			}

			m_infoBarButton.Style.BackgroundImage = img;
			m_infoBarButton.Size = new Size(img.Width + 6, img.Height + 6);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds an item to the specified tab.
		/// </summary>
		/// <param name="itemProps"></param>
		/// ------------------------------------------------------------------------------------
		internal void AddTabItem(SBTabItemProperties itemProps)
		{
			// Add a new item to this tab's item list (which is just a DNB toolbar) and get
			// back the item created for us.
			ButtonItem item = m_itemPanel.AddItem(itemProps);

			if (item == null)
				return;

			item.Click += new EventHandler(item_Click);

			// Create a menu item for this tab item and add it to the menu just above the
			// configure menu item.
			ButtonItem menuItem = (ButtonItem)item.Copy();
			menuItem.Text = menuItem.Text.Replace(Environment.NewLine, " ");

			// Make sure the menu text will be visible.
			if (menuItem.ForeColor == Color.White)
				menuItem.ForeColor = SystemColors.MenuText;

			int i = m_menu.SubItems.IndexOf(m_cfgMenuItem);
			m_menu.SubItems.Add(menuItem, i);
			UpdateMenuImages();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the current tab item.
		/// </summary>
		/// <param name="itemName"></param>
		/// <param name="generateEvents"></param>
		/// ------------------------------------------------------------------------------------
		internal void SetCurrentTabItem(string itemName, bool generateEvents)
		{
			ButtonItem item = m_itemPanel.GetItem(itemName);
			if (item == null)
				return;

			TagHelper tag = item.Tag as TagHelper;

			// If the item is already current, then do nothing.
			if (item.Checked && tag != null && !tag.ClickAlways)
				return;

			if (generateEvents)
				item.RaiseClick();
			else
			{
				m_supressAutoEvent = true;
				item.Checked = true;
				m_supressAutoEvent = false;
				m_prevSelectedItem = item;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="itemName"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		internal SBTabItemProperties GetItemProps(string itemName)
		{
			return m_itemPanel.GetItemProps(itemName);
		}

		#endregion

		#region Event Handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the configure menu item click.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void HandleMenuItemClick(object sender, EventArgs e)
		{
			// Sometimes this has to be forced, so just do it all the
			// time since it's ignored when irrelevant.
			m_menu.ClosePopup();

			// If the configure menu item was clicked on, then handle message dispatching.
			// All the other menu's on the popup are handled by the DNB manager click
			// event handler.
			if (sender is ButtonItem && ((ButtonItem)sender).Name == m_cfgMenuItem.Name)
			{
				if (m_cfgMenuItem.Tag is TagHelper)
				{
					string message = ((TagHelper)m_cfgMenuItem.Tag).Message;
					if (message != null)
						m_mediator.SendMessage(message, Properties);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This handler catches all clicks on menu items and side bar tab items.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void item_Click(object sender, EventArgs e)
		{
			if (m_supressAutoEvent)
				return;

			// Sometimes this has to be forced, so just do it all the
			// time since it's ignored when irrelevant.
			m_menu.ClosePopup();

			ButtonItem item = sender as ButtonItem;

			TagHelper tag = item.Tag as TagHelper;

			// If we clicked on the checked item then don't switch to it.
			if (m_mediator == null || item == null ||
				(item == m_prevSelectedItem && Checked && tag != null && !tag.ClickAlways))
			{
				return;
			}

			// Save the item clicked on so we can check it the next time an item is
			// clicked.
			m_prevSelectedItem = item;

			// Since we might have gotten here by clicking on one of the menu items,
			// make sure the checked side bar item matches the chosen item.
			SetCurrentTabItem(item.Name, false);

			// If we're not checked, it means we got here from clicking on one of the menu
			// items. Therefore, check this tab which will force it to be the active tab.
			if (!Checked)
				Checked = true;

			Application.DoEvents();

			if (tag == null)
				return;

			string message = tag.Message;
			if (message != null)
			{
				SBTabItemProperties itemProps = m_itemPanel.GetItemProps(item.Name);
				if (m_mediator.SendMessage(message, itemProps) &&
					itemProps != null && itemProps.Update)
				{
					m_itemPanel.UpdateItemProps(itemProps);
				}
			}
//			m_mediator.PostMessage(message, m_itemPanel.GetItemProps(item.Name));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Monitor when the information bar button is clicked so we know when to display the
		/// menu associated with this side bar tab.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_infoBarButton_Click(object sender, EventArgs e)
		{
			if (!Enabled || !m_menu.Enabled)
				return;

			PanelEx infoBarButton = sender as PanelEx;
			if (infoBarButton == null)
				return;

			m_hdrMenuItem.Visible = true;
			m_cfgMenuItem.Visible = false;
			Point pt = infoBarButton.PointToScreen(new Point(0, infoBarButton.Bottom));
			m_menu.Popup(pt.X, pt.Y);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This will make sure the appropriate item on the menu is checked.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void Menu_PopupOpen(object sender, PopupOpenEventArgs e)
		{
			// Don't check any menu items if this tab item isn't the currently
			// visible tab.
			try
			{
				foreach (ButtonItem item in m_itemPanel.Items)
					((ButtonItem)m_menu.SubItems[item.Name]).Checked = (Checked && item.Checked);

				HandleConfigureMenuPopup(m_cfgMenuItem);
			}
			catch
			{
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle calling the configure menu's update handler.
		/// </summary>
		/// <param name="cfgMenuItem"></param>
		/// ------------------------------------------------------------------------------------
		internal void HandleConfigureMenuPopup(ButtonItem cfgMenuItem)
		{
			if (cfgMenuItem == null)
				return;

			// Call the configure menu's update handler. If there isn't one and it
			// doesn't have a receiver, then disable it.
			if (!(cfgMenuItem.Tag is TagHelper))
				return;

			string configMessage = ((TagHelper)cfgMenuItem.Tag).Message;
			if (configMessage == null)
				return;

			SBTabProperties tabProps = Properties;
			if (!m_mediator.SendMessage("Update" + configMessage, tabProps))
				cfgMenuItem.Enabled = m_mediator.HasReceiver(configMessage);
			else
			{
				cfgMenuItem.Enabled = tabProps.ConfigureMenuEnabled;
				cfgMenuItem.Visible = tabProps.ConfigureMenuVisible;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void Menu_PopupClose(object sender, EventArgs e)
		{
			// When the menu closes, make sure the header item is hidden and the configure
			// item is visible;
			m_hdrMenuItem.Visible = false;
			m_cfgMenuItem.Visible = true;
		}

		#endregion
	}

	#endregion

	#region ItemPanel Class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Encapsulates a single sidebar tab control with one or more button's aligned
	/// vertically.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class ItemPanel : PanelEx
	{
		private Bar m_bar;
		private bool m_themesActive = false;

		// The SBTabItemProperties objects have a tag property in which the sidebar client
		// code may store whatever information it needs. However, SBTabItemProperties objects
		// are created on the fly using information from their associated ButtonItems. It
		// would be natural to store the client's tag information in the ButtonItem's tag
		// property but the adapter uses the ButtonItem's tag property internally (i.e. to
		// store TagHelper objects). Therefore, the tag objects the client needs to store
		// are maintained in this hash table so everytime an SBTabItemProperties is
		// constructed on the fly using information from a ButtonItem, that ButtonItem is
		// used as a key in the hash table to look up the client's tag information to put
		// in the SBTabItemProperties object being constructed on the fly.
		private Dictionary<ButtonItem, object> m_tags = new Dictionary<ButtonItem, object>();

		#region Construction
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// There is one ItemPanel per tab. The ItemPanel contains the items
		/// for a single tab. The items on a ItemPanel are just items on a toolbar.
		/// The toolbar is contained by the ItemPanel.
		/// </summary>
		/// <param name="name">Name to give this panel.</param>
		/// <param name="owningAdapter"></param>
		/// ------------------------------------------------------------------------------------
		internal ItemPanel(string name, SIBAdapter owningAdapter)
		{
			m_themesActive = owningAdapter.m_dnbMngr.IsThemeActive;

			Name = name;
			Dock = DockStyle.Fill;
			Style.Border = owningAdapter.m_navPane.TitlePanel.Style.Border;
			Style.BorderColor.Color = owningAdapter.m_navPane.TitlePanel.Style.BorderColor.Color;
			Style.GradientAngle = 90;

			m_bar = new Bar();
			m_bar.Name = name;
			m_bar.LayoutType = eLayoutType.TaskList;
			m_bar.CanAutoHide = false;
			m_bar.CanCustomize = false;
			m_bar.CanHide = false;
			m_bar.CanReorderTabs = false;
			m_bar.CanUndock = false;
			m_bar.Stretch = true;
			m_bar.EqualButtonSize = true;
			m_bar.Font = SystemInformation.MenuFont;
			m_bar.PaddingTop = 8;

			m_bar.Style =
				(owningAdapter.m_dnbMngr.IsThemeActive ? eDotNetBarStyle.Office2003 : eDotNetBarStyle.Office2000);

			Controls.Add(m_bar);
			m_bar.ItemSpacing = 100;

			if (m_themesActive)
			{
				Style.BackColor1.Color = m_bar.ColorScheme.BarBackground;
				Style.BackColor2.Color = m_bar.ColorScheme.BarBackground2;
			}
			else
			{
				Style.BackColor1.Color = SystemColors.ControlDark;
				Style.BackColor2.Color = SystemColors.ControlDark;
				m_bar.BackColor = SystemColors.ControlDark;
			}
		}

		#endregion

		#region Overridden Events
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			m_bar.Location = new Point(0, 3);
			m_bar.RecalcLayout();
			OnSizeChanged(null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnSizeChanged(EventArgs e)
		{
			if (e != null)
				base.OnSizeChanged(e);

			if (m_bar != null)
			{
				m_bar.Size = new Size(0, ClientSize.Height - 6);

				// When the bar is in small icon mode, place it on the left side of
				// the panel. Otherwise, center it.
				m_bar.Left = (m_bar.ImageSize == eBarImageSize.Default ?
					2 : (ClientSize.Width - m_bar.Width) / 2);
			}
		}

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal ImageList ImageListSmall
		{
			set {m_bar.Images = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal ImageList ImageListLarge
		{
			set {m_bar.ImagesLarge = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the bar shows small icons.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool SmallIconMode
		{
			get {return m_bar.ImageSize == eBarImageSize.Default;}
			set
			{
				if (value == SmallIconMode)
					return;

				m_bar.SuspendLayout();
				foreach (ButtonItem item in m_bar.Items)
					item.ImagePosition = (value ? eImagePosition.Left : eImagePosition.Top);

				m_bar.ImageSize = (value ? eBarImageSize.Default : eBarImageSize.Large);
				OnSizeChanged(null);
				m_bar.ResumeLayout();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal SubItemsCollection Items
		{
			get {return m_bar.Items;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal ButtonItem CurrentItem
		{
			get
			{
				foreach (ButtonItem item in m_bar.Items)
				{
					if (item.Checked)
						return item;
				}

				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal string CurrentItemName
		{
			get
			{
				ButtonItem item = CurrentItem;
				if (item != null)
					return item.Name;

				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal SBTabItemProperties CurrentItemProps
		{
			get
			{
				ButtonItem item = CurrentItem;
				return (item == null ? null : GetItemProps(item.Name));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an array of all the properties for each item in the tab.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal SBTabItemProperties[] AllItemProps
		{
			get
			{
				List<SBTabItemProperties> items = new List<SBTabItemProperties>();

				foreach (ButtonItem item in m_bar.Items)
					items.Add(GetItemProps(item.Name));

				return items.ToArray();
			}
		}
		#endregion

		#region Misc. Internal Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="itemName"></param>
		/// <param name="text"></param>
		/// <param name="imageIndex"></param>
		/// ------------------------------------------------------------------------------------
		internal ButtonItem AddItem(SBTabItemProperties itemProps)
		{
			ButtonItem item = new ButtonItem(itemProps.Name, itemProps.Text);
			item.Tag = new TagHelper(itemProps.ClickAlways, itemProps.Message);
			item.ImageIndex = itemProps.ImageIndex;
			item.ImagePosition = (SmallIconMode ? eImagePosition.Left : eImagePosition.Top);
			item.Style = (m_themesActive ? eDotNetBarStyle.Office2003 : eDotNetBarStyle.Office2000);
			item.OptionGroup = Name;
			item.AccessibleName = itemProps.Text;
			item.ButtonStyle = eButtonStyle.ImageAndText;

			m_tags[item] = itemProps.Tag;
			m_bar.Items.Add(item);

			// Don't take the default fore color when we're in Window's 2000 style.
			if (!m_themesActive)
				item.ForeColor = Color.White;

			return item;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="itemName"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		internal SBTabItemProperties GetItemProps(string itemName)
		{
			ButtonItem item = m_bar.Items[itemName] as ButtonItem;

			if (item == null)
				return null;

			SBTabItemProperties itemProps = new SBTabItemProperties(this.TopLevelControl as Form);

			// TE-7171 indicates there is some case in which a null reference exception
			// can be thrown in this method. However, the exception cannot be reproduced
			// and the stack trace in the issue does not include line numbers so we're not
			// sure which object is null. Therefore, add some checks for insurance.
			if (itemProps == null)
				return null;

			itemProps.Name = itemName;
			itemProps.Text = item.Text;
			itemProps.Tooltip = item.Tooltip;
			itemProps.ImageIndex = item.ImageIndex;
			itemProps.OwningTabName = Name;
			object clientTag;
			itemProps.Tag = (m_tags.TryGetValue(item, out clientTag) ? clientTag : null);

			// Before TE-7171 we used to assume item.Tag is always a TagHelper (which should
			// always be the case), but for insurance, put a check here so an exception isn't
			// thrown.
			TagHelper tag = item.Tag as TagHelper;
			if (tag != null)
			{
				itemProps.Message = tag.Message;
				itemProps.ClickAlways = tag.ClickAlways;
			}

			return itemProps;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the item props.
		/// </summary>
		/// <param name="itemProps">The item props.</param>
		/// ------------------------------------------------------------------------------------
		internal void UpdateItemProps(SBTabItemProperties itemProps)
		{
			if (!itemProps.Update)
				return;

			ButtonItem item = m_bar.Items[itemProps.Name] as ButtonItem;

			if (item == null)
				return;

			item.Text = itemProps.Text;
			item.Tooltip = itemProps.Tooltip;
			item.ImageIndex = itemProps.ImageIndex;
			m_tags[item] = itemProps.Tag;

			TagHelper tag = item.Tag as TagHelper;
			if (tag == null)
				item.Tag = new TagHelper(itemProps.ClickAlways, itemProps.Message);
			else
			{
				tag.Message = itemProps.Message;
				tag.ClickAlways = itemProps.ClickAlways;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="itemName"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		internal ButtonItem GetItem(string itemName)
		{
			try
			{
				return (ButtonItem)m_bar.Items[itemName];
			}
			catch
			{
				return null;
			}
		}

		#endregion
	}

	#endregion

	#region HeaderButton Class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Creates a heading-type button used as the first item in the info. bar button menus.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	class HeaderButton : ButtonItem
	{
		public HeaderButton(string name, string text) : base(name + "-HDR", text)
		{
			HotTrackingStyle = eHotTrackingStyle.None;

			// Disable so clicks on this item are ignored.
			Enabled = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override the paint to make this item very distict from other menu items.
		/// </summary>
		/// <param name="p"></param>
		/// ------------------------------------------------------------------------------------
		public override void Paint(ItemPaintArgs p)
		{
			// It turns out the only rectangle we're given to work with is the
			// p.Graphics.ClipBounds but that is the rectangle of the entire menu, not just
			// this menu item. To only get the rectangle for this item, use it's size and
			// assume this item is the first item in the menu.
			Rectangle rc = new Rectangle(1, 1, Size.Width, Size.Height);

			p.Graphics.FillRectangle(SystemBrushes.Control, rc);

			p.Graphics.DrawLine(SystemPens.ControlText,
				0, rc.Bottom - 1, rc.Right, rc.Bottom - 1);

			using (Font fnt = new Font(SystemInformation.MenuFont,
				SystemInformation.MenuFont.FontFamily.IsStyleAvailable(FontStyle.Bold) ?
				FontStyle.Bold : FontStyle.Regular))
			{
				using (StringFormat sf = new StringFormat(StringFormat.GenericTypographic))
				{
					sf.Alignment = StringAlignment.Center;
					sf.LineAlignment = StringAlignment.Center;
					p.Graphics.DrawString(Text, fnt, SystemBrushes.ControlText, rc, sf);
				}
			}
		}
	}

	#endregion
}
