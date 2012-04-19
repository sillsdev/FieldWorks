// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: MenuExtender.cs
// Responsibility: DavidO
// Last reviewed:
//
// <remarks>
// This extends the MenuItem class to allow images to be placed on menus. Thanks to Chris
// Beckett who wrote this originally. I downloaded it from CodeProject.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;

using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal interface IMenuDrawingHelper: IDisposable
	{
		ImageList ImageList	{get; set;}
		MenuItem MenuItem {set;}
		Graphics Graphics {set;}
		void DrawMenu(Rectangle bounds, DrawItemState state, int imageIndex);
		int CalcHeight();
		int CalcWidth();
	}

	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// These are the two styles of menus available to draw.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public enum MenuStyles
	{
		/// <summary>Standard menu style</summary>
		Standard,
		/// <summary>Office XP/Visual Studio 7+ menu style</summary>
		OfficeXP
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class extends a menu item. Placing a LabelMenuItem in a menu will cause that menu
	/// item to act like a label but not like a menu item in that it cannot be selected.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class LabelMenuItem : MenuItem
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Instantiates a new LabelMenuItem
		/// </summary>
		/// <param name="text"></param>
		/// ------------------------------------------------------------------------------------
		public LabelMenuItem(string text) : base(text)
		{
			// This will cause clicks on this menu item to be ignored.
			Enabled = false;
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A custom extender class that adds a <c>MenuImage</c> attribute to <c>MenuItem</c>
	/// objects, and custom drawns the menu with an icon stored in a referenced <c>ImageList</c>
	/// control.
	/// </summary>
	/// <remarks>
	/// This extension was written to provide an simple way to link icons in an Imagelist with
	/// a menu, and owner draw the menu. Other menu icon samples sub-class a MenuItem which
	/// interferes with the Visual Studio IDE for designing menus. Other examples required a lot
	/// of custom tooling and hand-coding. By using an extender, no custom coding is required.
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	[ToolboxBitmap(typeof(MainMenu))]
	[ProvideProperty("ImageIndex", typeof(MenuItem))]
	[ProvideProperty("ImageList", typeof(MenuItem))]
	[ProvideProperty("StatusMessage", typeof(MenuItem))]
	[ProvideProperty("CommandId", typeof(MenuItem)) ]
	[DefaultProperty("ImageList")]
	public class MenuExtender : Component, IFWDisposable, IExtenderProvider, ISupportInitialize
	{
		// Used in the IMenuDrawingHelper classes.
#if __MonoCS__
		// Linux doesn't have the Marlett font.  (This whole assembly doesn't appear to be used
		// in FieldWorks -- maybe it should be removed?)
		internal static Font CheckFont = new Font("OpenSymbol", 12);
		internal static string CheckMarkGlyph = "\u2713";
#else
		internal static Font CheckFont = new Font("Marlett", 12);
		internal static string CheckMarkGlyph = "a";
#endif

		#region Private fields
		/// <summary>Dictionary used to store <c>MenuItem</c> image lists.</summary>
		private Dictionary<MenuItem, ImageList> m_htImageLists = new Dictionary<MenuItem, ImageList>();

		/// <summary>Dictionary used to store <c>MenuItem</c> image indexes.</summary>
		private Dictionary<MenuItem, int> m_htImageIndexes = new Dictionary<MenuItem, int>();

		/// <summary>Dictionary used to store <c>MenuItem</c> status bar texts.</summary>
		private Dictionary<MenuItem, string> m_htStatusMessage = new Dictionary<MenuItem, string>();

		private Mediator m_mediator = null;
		private Dictionary<MenuItem, string> m_htCmdIds = new Dictionary<MenuItem, string>();
		private Dictionary<MenuItem, object> m_Tags = new Dictionary<MenuItem, object>();

		/// <summary>
		/// Holds a reference to the image list from which menus get their images when an
		/// individual menu item hasn't been assigned an image list.
		/// </summary>
		private ImageList m_imageList = null;

		private MenuStyles m_menuStyle = MenuStyles.Standard;
		private static IMenuDrawingHelper m_menuDrawHelper;
		private MenuItem m_menuItem;
		private string m_defaultStatusMessage;
		private StatusBarPanel m_statusBar;
		private Component m_Parent;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for instance that supports Class Composition designer.
		/// </summary>
		/// <param name="container">Reference to container hosting this instance.</param>
		/// ------------------------------------------------------------------------------------
		public MenuExtender(System.ComponentModel.IContainer container) : this()
		{
			container.Add(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public MenuExtender()
		{
			CreateMenuDrawingHelper();
		}

		#endregion

		#region Disposal

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		///
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		private bool m_isDisposed = false;

		/// <summary>
		///
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			if (IsDisposed)
				return;

			if (disposing)
			{
				m_menuDrawHelper.Dispose();
			}

			base.Dispose(disposing);

			m_isDisposed = true;
		}

		#endregion Disposal

		#region IExtenderProvider Method
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Used to determine if a specific form component instance supports this attribute
		/// extension. Allows custom attributes to target specific control types.
		/// </summary>
		/// <param name="component">component to evaluate for compatability</param>
		/// <returns>Returns True/False if the component supports the extender.</returns>
		/// ------------------------------------------------------------------------------------
		public bool CanExtend(object component)
		{
			CheckDisposed();

			return (component is MenuItem);

			//			// Only support MenuItem objects that are not top-level menus (default rendering
			//			// for top-level menus is fine - does not need extension
			//			if (component is MenuItem)
			//			{
			//				MenuItem menuItem = (MenuItem)component;
			//				return (!(menuItem.Parent is MainMenu));
			//			}
			//
			//			return false;
		}

		#endregion

		#region Public Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the <c>ImageList</c> control that holds menu images. When individual
		/// menu items haven't been assigned an image list, this image list is used instead.
		/// </summary>
		/// <value>an <c>ImageList</c> instance that holds menu icons.</value>
		/// ------------------------------------------------------------------------------------
		[DefaultValue(null)]
		[Localizable(true)]
		[Description("The image list used when individual menu item's are not assigned an image list.")]
		public ImageList ImageList
		{
			get
			{
				CheckDisposed();
				return m_imageList;
			}
			set
			{
				CheckDisposed();
				m_imageList = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating what style to use to draw the menu items.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DefaultValue(MenuStyles.Standard)]
		[Localizable(false)]
		[Description("The style in which menu items will be drawn.")]
		public MenuStyles MenuStyle
		{
			get
			{
				CheckDisposed();
				return m_menuStyle;
			}
			set
			{
				CheckDisposed();

				m_menuStyle = value;
				CreateMenuDrawingHelper();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The default help text that will be shown in the status bar
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Description("The default help text that will be shown if no menu item is selected")]
		[DefaultValue("")]
		[Localizable(true)]
		public string DefaultHelpText
		{
			get
			{
				CheckDisposed();
				return m_defaultStatusMessage;
			}
			set
			{
				CheckDisposed();
				m_defaultStatusMessage = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Assign the StatusBarPanel that will display the menu's message
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Description("The status bar panel that will display the menu's messages")]
		[DefaultValue(null)]
		public StatusBarPanel StatusBar
		{
			get
			{
				CheckDisposed();
				return m_statusBar;
			}
			set
			{
				CheckDisposed();
				m_statusBar = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the message mediator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Description("The message mediator that will deal with menu clicks and updates")]
		[DefaultValue(null)]
		public Mediator MessageMediator
		{
			get
			{
				CheckDisposed();
				return m_mediator;
			}
			set
			{
				CheckDisposed();
				m_mediator = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Component Parent
		{
			get
			{
				CheckDisposed();
				return m_Parent;
			}
			set
			{
				CheckDisposed();
				m_Parent = value;
			}
		}
		#endregion

		#region Extender Property Set and Get methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="menuItem"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		[Localizable(true)]
		[Category("Extender Properties")]
		[Description("The image list from which the menu item's image is taken. Note: this property is ignored for top-level menus (i.e. menu items whose parent is a MainMenu).")]
		public ImageList GetImageList(MenuItem menuItem)
		{
			CheckDisposed();

			if (m_htImageLists.ContainsKey(menuItem))
				return m_htImageLists[menuItem];

			return m_imageList;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="menuItem"></param>
		/// <param name="newImageList"></param>
		/// ------------------------------------------------------------------------------------
		public void SetImageList(MenuItem menuItem, ImageList newImageList)
		{
			CheckDisposed();

			// Don't allow this property to be set for menu item's that are top-level menus.
			if (menuItem.Parent is MainMenu || newImageList == null)
				return;

			if (!m_htImageLists.ContainsKey(menuItem))
			{
				m_htImageLists.Add(menuItem, newImageList);
			}
			else
			{
				// Get the old image list assigned to this component.
				ImageList imgList = GetImageList(menuItem);

				// Is the new image list different from the old one?
				if (imgList != newImageList)
				{
					// Replace the existing dictioanry entry for this component.
					m_htImageLists[menuItem] = newImageList;

					// Because we replaced the hash table entry (i.e. assigned a new image list
					// to this component, we need to make sure that, if the component also had
					// an image index, it should be reset since the new image list may not have
					// an image at the old index.
					if (GetImageIndex(menuItem) >= newImageList.Images.Count)
						SetImageIndex(menuItem, -1);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Used to retrieve the MenuImage extender property value for a given <c>MenuItem</c>
		/// component instance.
		/// </summary>
		/// <param name="component">the menu item instance associated with the value</param>
		/// <returns>Returns the MenuImage index property value for the specified
		/// <c>MenuItem</c> component instance.</returns>
		/// ------------------------------------------------------------------------------------
		[Localizable(true)]
		[Category("Extender Properties")]
		[Description("The index into the image list of the image used for this menu item. Note: this property is ignored for top-level menus (i.e. menu items whose parent is a MainMenu).")]
		[Editor("System.Windows.Forms.Design.ImageIndexEditor", typeof(System.Drawing.Design.UITypeEditor))]
		[TypeConverter("System.Windows.Forms.ImageIndexConverter")]
		public int GetImageIndex(MenuItem component)
		{
			CheckDisposed();

			if (m_htImageIndexes.ContainsKey(component))
				return m_htImageIndexes[component];

			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Used to set a image index property value for a specific <c>MenuItem</c> instance.
		/// </summary>
		/// <param name="menuItem">the <c>MenuItem</c> object to store</param>
		/// <param name="imageIndex">the image index to associate with the menu item.</param>
		/// ------------------------------------------------------------------------------------
		public void SetImageIndex(MenuItem menuItem, int imageIndex)
		{
			CheckDisposed();

			// Don't allow this property to be set for menu item's that are top-level menus.
			if (menuItem.Parent is MainMenu)
				return;

			// Store a dictionary entry containing the image index associated with this component.
			if (m_htImageIndexes.ContainsKey(menuItem))
				m_htImageIndexes[menuItem] = imageIndex;
			else
			{
				m_htImageIndexes.Add(menuItem, imageIndex);

				// This will set the owner draw flag and hook the measure and draw events.
				AddMenuItem(menuItem);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Assign/Retrieve the message that will appear in the StatusBarPanel when a MenuItem
		/// is selected
		/// </summary>
		/// <remarks>
		/// Extended properties are actual methods because they take an additional parameter
		/// that is the object or control to provide the property for.
		/// </remarks>
		/// <param name="component"></param>
		/// ------------------------------------------------------------------------------------
		[Localizable(true)]
		[Category("Extender Properties")]
		[Description("The help text displayed in the status bar")]
		public string GetStatusMessage(MenuItem component)
		{
			CheckDisposed();

			if( m_htStatusMessage.ContainsKey(component))
				return m_htStatusMessage[component];

			return m_defaultStatusMessage;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Assign/Retrieve the message that will appear in the StatusBarPanel when a MenuItem
		/// is selected
		/// </summary>
		/// <param name="menuItem"></param>
		/// <param name="helpText"></param>
		/// ------------------------------------------------------------------------------------
		public void SetStatusMessage(MenuItem menuItem, string helpText)
		{
			CheckDisposed();

			if (m_htStatusMessage.ContainsKey(menuItem))
			{
				if (helpText == null)
					m_htStatusMessage.Remove(menuItem);
				else
					m_htStatusMessage[menuItem] = helpText;
			}
			else if (helpText != null)
			{
				m_htStatusMessage.Add(menuItem, helpText);
				menuItem.Select += new EventHandler(OnMenuSelect);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the command ID that Xcore will use for associating a MenuItem with
		/// an event handler
		/// </summary>
		/// <param name="component"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		[Localizable(false)]
		[Category("Extender Properties")]
		[Description("The command ID used to find the handling message")]
		public string GetCommandId(MenuItem component)
		{
			CheckDisposed();

			if( m_htCmdIds.ContainsKey(component))
				return m_htCmdIds[component];

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Assign the command ID that Xcore will use for associating a MenuItem with
		/// an event handler
		/// </summary>
		/// <param name="menuItem"></param>
		/// <param name="cmdId"></param>
		/// ------------------------------------------------------------------------------------
		public void SetCommandId(MenuItem menuItem, string cmdId)
		{
			CheckDisposed();

			if (cmdId == null || cmdId == string.Empty)
			{
				m_htCmdIds.Remove(menuItem);
				// unhook the event in case it's previously been hooked.
				menuItem.Click -= new EventHandler(OnMenuItemClick);
			}
			else if (m_htCmdIds.ContainsKey(menuItem))
				m_htCmdIds[menuItem] = cmdId;
			else
			{
				m_htCmdIds.Add(menuItem, cmdId);
				menuItem.Click += new EventHandler(OnMenuItemClick);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve additional info associated with the MenuItem
		/// </summary>
		/// <param name="component"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		[Localizable(false)]
		[Category("Extender Properties")]
		[Description("The tag can be any object used for anything the app needs to remember")]
		public object GetTag(MenuItem component)
		{
			CheckDisposed();

			if (m_Tags.ContainsKey(component))
				return m_Tags[component];
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Assign additional info associated with the MenuItem
		/// </summary>
		/// <param name="menuItem"></param>
		/// <param name="tag">anything you want</param>
		/// ------------------------------------------------------------------------------------
		public void SetTag(MenuItem menuItem, object tag)
		{
			CheckDisposed();

			if (tag == null)
				m_Tags.Remove(menuItem);
			else
				m_Tags[menuItem] = tag;
		}
		#endregion

		#region Helpers Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method is provided so menu items that aren't added through the designer can
		/// still be owner drawn along with all the other menu items coming through the
		/// extender's draw methods.
		/// </summary>
		/// <param name="menuItem">The menu item to add to the extender.</param>
		/// ------------------------------------------------------------------------------------
		public void AddMenuItem(MenuItem menuItem)
		{
			CheckDisposed();

			// This is a no-no.
			if (menuItem.Parent is MainMenu)
				return;

			// set the menu to owner draw false so that we can get the Accessibility name.
			// It will be set to OwnerDraw=true when we show the menu item.
			menuItem.OwnerDraw = false;

			// unhook the menu owner drawn events in case they've previously been hooked.
			menuItem.MeasureItem -= new MeasureItemEventHandler(OnMeasureItem);
			menuItem.DrawItem -= new DrawItemEventHandler(OnDrawItem);

			// hook up the menu owner drawn events
			menuItem.MeasureItem += new MeasureItemEventHandler(OnMeasureItem);
			menuItem.DrawItem += new DrawItemEventHandler(OnDrawItem);

			if (menuItem.MenuItems.Count > 0)
			{
				// Before subscribing to the popup event, first, make sure there aren't
				// any handlers already hooked-up.
				menuItem.Popup -= new EventHandler(OnMenuPopup);
				menuItem.Popup += new EventHandler(OnMenuPopup);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="menuItem"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private ImageList InternalGetImageList(MenuItem menuItem)
		{
			// Get the image list assigned to this component, if there is one.
			ImageList imgList = GetImageList(menuItem);

			if (imgList != null)
				return imgList;

			// At this point, we know the component wasn't assigned an image list. Therefore,
			// if the component has an image index and there is an image list associated with
			// the extender, use that image list for the component.
			if (GetImageIndex(menuItem) >= 0)
				return m_imageList;

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CreateMenuDrawingHelper()
		{
			if (m_menuStyle == MenuStyles.Standard)
				m_menuDrawHelper = new DrawStandardMenuHelper();
			else
				m_menuDrawHelper = new DrawOfficeXPMenuHelper();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is called by the menu item's measure item and draw methods to setup the
		/// drawing helper class to do the drawing.
		/// </summary>
		/// <param name="menuItem"></param>
		/// <param name="graphics"></param>
		/// ------------------------------------------------------------------------------------
		private void InitHelperForMenuItem(MenuItem menuItem, Graphics graphics)
		{
			if (m_menuDrawHelper == null)
				CreateMenuDrawingHelper();

			m_menuItem = menuItem;
			m_menuDrawHelper.MenuItem = m_menuItem;
			m_menuDrawHelper.Graphics = graphics;

			// Retrieve the image list from hash table.
			m_menuDrawHelper.ImageList = InternalGetImageList(menuItem);
		}

		#endregion

		#region ShouldSerialize and Reset methods
		// These methods prevent that the properties are written in the generated code
		// unless they are really set.

#pragma warning disable 0169 // error CS0169: The private method ... is never used, but they are
		// needed in Designer

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if the status message should be serialized by the code generator.
		/// </summary>
		/// <param name="menuItem"></param>
		/// <returns><c>true</c> if a non-default status message text is set, otherwise
		/// <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		private bool ShouldSerializeStatusMessage(MenuItem menuItem)
		{
			bool fRet = GetStatusMessage(menuItem) != m_defaultStatusMessage
				&& GetStatusMessage(menuItem) != null;
			return fRet;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if the ImageIndex should be serialized by the code generator
		/// </summary>
		/// <param name="menuItem"></param>
		/// <returns><c>true</c> if an image index is set, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		private bool ShouldSerializeImageIndex(MenuItem menuItem)
		{
			return (!(menuItem.Parent is MainMenu));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if the image list should be serialized by the code generator
		/// </summary>
		/// <param name="menuItem"></param>
		/// <returns><c>true</c> if an image list is set for the menu item, otherwise
		/// <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		private bool ShouldSerializeImageList(MenuItem menuItem)
		{
			return GetImageList(menuItem) != null && GetImageList(menuItem) != m_imageList;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if the CommandId should be serialized by the code generator
		/// </summary>
		/// <param name="menuItem"></param>
		/// <returns><c>true</c> if a command id text is set, otherwise
		/// <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		private bool ShouldSerializeCommandId(MenuItem menuItem)
		{
			return GetCommandId(menuItem) != null;
		}

		private void ResetStatusMessage(MenuItem menuItem)
		{
			SetStatusMessage(menuItem, m_defaultStatusMessage);
		}

		private void ResetImageIndex(MenuItem menuItem)
		{
			SetImageIndex(menuItem, -1);
		}

		private void ResetImageList(MenuItem menuItem)
		{
			SetImageList(menuItem, null);
		}

		private void ResetCommandId(MenuItem menuItem)
		{
			SetCommandId(menuItem, null);
		}

#pragma warning restore 0169

		#endregion

		#region Menu Item Event Delegates
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Event triggered to measure the size of a owner drawn <c>MenuItem</c>.
		/// </summary>
		/// <param name="sender">the menu item client object</param>
		/// <param name="e">the event arguments</param>
		/// ------------------------------------------------------------------------------------
		private void OnMeasureItem(object sender, MeasureItemEventArgs e)
		{
			InitHelperForMenuItem(sender as MenuItem, e.Graphics);

			// Calculate the menu height
			e.ItemHeight = m_menuDrawHelper.CalcHeight();
			e.ItemWidth = m_menuDrawHelper.CalcWidth();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Event triggered to owner draw the provide <c>MenuItem</c>.
		/// </summary>
		/// <param name="sender">the menu item client object</param>
		/// <param name="e">the event arguments</param>
		/// -----------------------------------------------------------------------------------
		private void OnDrawItem(object sender, DrawItemEventArgs e)
		{
			InitHelperForMenuItem(sender as MenuItem, e.Graphics);

			// Get the index of the item's image in the image list.
			int imageIndex = GetImageIndex(sender as MenuItem);

			ImageList imgList = m_menuDrawHelper.ImageList;

			// Make sure the image index is valid.
			if (imgList == null || imageIndex >= imgList.Images.Count)
				imageIndex = -1;

			m_menuDrawHelper.DrawMenu(e.Bounds, e.State, imageIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display the help text in the status bar for the selected menu item
		/// </summary>
		/// <param name="control"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void OnMenuSelect(object control, EventArgs e)
		{
			if (StatusBar == null)
				return;

			MenuItem key = control as MenuItem;
			if (m_htStatusMessage.ContainsKey(key))
				StatusBar.Text = m_htStatusMessage[key];
			if (StatusBar.Text.Length == 0)
				StatusBar.Text = m_defaultStatusMessage;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Message handling method
		/// </summary>
		/// <param name="menuItem"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void OnMenuItemClick(object menuItem, EventArgs e)
		{
			MenuItem key = menuItem as MenuItem;
			if (key != null && m_htCmdIds.ContainsKey(key) && m_mediator != null)
				m_mediator.SendMessage(m_htCmdIds[key], menuItem);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Menu is about to be shown
		/// </summary>
		/// <param name="menuItem"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void OnMenuPopup(object menuItem, EventArgs e)
		{
			RefreshStatus(menuItem);
		}
		#endregion

		#region Accessibility related methods for testing
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the menu is shown. Set OwnerDraw to true. Needed to get accesibility
		/// names (see comments in <see cref="SetOwnerDrawFlag"/>.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void OnMenuStart(object sender, EventArgs e)
		{
			Form form = m_Parent as Form;
			if (form != null && form.Menu != null)
				SetOwnerDrawFlag(form.Menu.MenuItems, true, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the menu is hidden.  Set OwnerDraw to false. Needed to get accesibility
		/// names (see comments in <see cref="SetOwnerDrawFlag"/>.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void OnMenuComplete(object sender, EventArgs e)
		{
			Form form = m_Parent as Form;
			if (form != null && form.Menu != null)
				SetOwnerDrawFlag(form.Menu.MenuItems, false, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loop through all menuItems setting OwnerDraw to the fOwnerDraw value.
		/// </summary>
		/// <param name="menuItems">The menu items</param>
		/// <param name="fOwnerDraw">New value for the owner draw</param>
		/// <param name="fMainFrameMenu">True if it is the MainFrame menu</param>
		/// ------------------------------------------------------------------------------------
		private void SetOwnerDrawFlag(Menu.MenuItemCollection menuItems, bool fOwnerDraw,
			bool fMainFrameMenu)
		{
			foreach(MenuItem menuItem in menuItems)
			{
				// we set the owner draw flag before we show the menu item and set it to
				// false again when the menu goes away. The reason is that accessibility
				// can't get the names of menu items that are owner drawn. Doing it this way
				// is weird, but at least it works.
				if (!fMainFrameMenu)
					menuItem.OwnerDraw = fOwnerDraw;

				SetOwnerDrawFlag(menuItem.MenuItems, fOwnerDraw, false);
			}
		}
		#endregion

		#region ISupportInitialize Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called at the beginning of InitializeComponent for the form containing the extender.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void BeginInit()
		{
			CheckDisposed();

			// nothing to do here
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called at the end of InitializeComponent for the form containing the extender.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void EndInit()
		{
			CheckDisposed();

			if (!DesignMode)
			{
				Form form = m_Parent as Form;
				if (form != null && form.Menu != null)
				{
					foreach (MenuItem menuItem in form.Menu.MenuItems)
					{
						if (menuItem.MergeType == MenuMerge.Add && menuItem.MergeOrder == 0)
							menuItem.MergeType = MenuMerge.MergeItems;

						ChangeMenuItemsMergeOrder(menuItem);
					}

					// The following two lines are needed so that we get accessibility names
					// (which are used for testing).
					form.MenuStart += new EventHandler(OnMenuStart);
					form.MenuComplete += new EventHandler(OnMenuComplete);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the MergeOrder properties of the form's main menu to prepare it for merging
		/// with derived form's menus.
		/// </summary>
		/// <param name="parent"></param>
		/// ------------------------------------------------------------------------------------
		private void ChangeMenuItemsMergeOrder(MenuItem parent)
		{
			if (parent == null)
				return;

			if (parent.MergeOrder == 0)
				parent.MergeOrder = parent.Index + 1;

			foreach (MenuItem menuItem in parent.MenuItems)
				ChangeMenuItemsMergeOrder(menuItem);
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Subscribe to the Popup event for the menu item and all sub menus
		/// </summary>
		/// <param name="menu"></param>
		/// ------------------------------------------------------------------------------------
		private void SubscribeToPopup(Menu menu)
		{
			if (menu == null)
				return;

			foreach (MenuItem menuItem in menu.MenuItems)
			{
				// First unhook the popup handler in case there's already a hook.
				menuItem.Popup -= new EventHandler(OnMenuPopup);

				// Now create a hook to the popup event for this menu item.
				menuItem.Popup += new EventHandler(OnMenuPopup);
				SubscribeToPopup(menuItem);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a context menu.
		/// </summary>
		/// <param name="contextMenu">The context menu</param>
		/// <param name="menuItems">The menu item</param>
		/// <param name="cmdIds">Command id</param>
		/// ------------------------------------------------------------------------------------
		public void AddContextMenu(ContextMenu contextMenu, MenuItem[] menuItems, string[] cmdIds)
		{
			CheckDisposed();

			Debug.Assert(menuItems.Length == cmdIds.Length);

			// Set the command IDs for each menu item.
			for (int i = 0; i < menuItems.Length; i++)
			{
				// Find another menu item with the same command ID (i.e. probably a main
				// menu item that performs the same command as this context menu).
				// that's on a main menu.
				MenuItem itemWithSameCommandId = FindMenu(cmdIds[i]);
				if (itemWithSameCommandId != null)
				{
					// If a main menu item with the same command ID is found then assume
					// the purpose of this context menu is the same. Therefore, use the
					// image from the main menu item for this context menu.
					SetImageIndex(menuItems[i], GetImageIndex(itemWithSameCommandId));
					SetImageList(menuItems[i], GetImageList(itemWithSameCommandId));
				}

				// Save the command ID for the menu item.
				SetCommandId(menuItems[i], cmdIds[i]);
			}

			AddContextMenu(contextMenu);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a context menu to the menu extender. This overload assumes command handlers,
		/// image indexes and image lists have already been set for items in the context menu.
		/// </summary>
		/// <param name="contextMenu">The context menu</param>
		/// ------------------------------------------------------------------------------------
		public void AddContextMenu(ContextMenu contextMenu)
		{
			CheckDisposed();

			// If there are menu items that were not cloned (such as separators) then
			// do them now.  They are ones that do not have owner draw set yet.
			foreach (MenuItem mi in contextMenu.MenuItems)
			{
				if (!mi.OwnerDraw)
				{
					AddMenuItem(mi);
					mi.OwnerDraw = true;
				}
			}

			// If this context menu was added in the past, be sure unsubscribe to its popup event
			//  before re-subscribing. This will only be necessary when this same context menu
			//  was previously added to the extender. Therefore, unsubscribing here is for
			//  insurance purposes only. If the context menu was not previously added,
			//  unsubscribing isn't necessary but won't hurt.
			contextMenu.Popup -= new EventHandler(OnMenuPopup);

			// Now make sure we get the message when the context menu is popped-up so we
			// have a chance to call the update handlers for each of the context menu's
			// menu items.
			contextMenu.Popup += new EventHandler(OnMenuPopup);

			// Now go through the menu items on the context menu and subscribe to their
			// popup events.
			SubscribeToPopup(contextMenu);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// <p>Merge the first and second main menu and return a copy of it.</p>
		/// <p>Side effect: This method changes the main menu of the form if one of the two
		/// passed in menus is set as main menu of the form!</p>
		/// </summary>
		/// <param name="firstMainMenu"></param>
		/// <param name="secondMainMenu"></param>
		/// <returns>The expanded first main menu</returns>
		/// ------------------------------------------------------------------------------------
		public MainMenu MergeMenus(MainMenu firstMainMenu, MainMenu secondMainMenu)
		{
			CheckDisposed();

			MainMenuMerge mainMenuMerge = new MainMenuMerge(this);
			mainMenuMerge.MergeMenu(firstMainMenu, secondMainMenu);
			SubscribeToPopup(firstMainMenu);
			return firstMainMenu;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Register the main menu
		/// </summary>
		/// <param name="mainMenu">The main menu</param>
		/// ------------------------------------------------------------------------------------
		public void AddMenu(MainMenu mainMenu)
		{
			CheckDisposed();

			SubscribeToPopup(mainMenu);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make a clone of an existing menu item, including the extended properties. When using
		/// a menu extender, this should be used instead of MenuItem.CloneMenu().
		/// </summary>
		/// <param name="src"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public MenuItem CloneMenu(MenuItem src)
		{
			CheckDisposed();

			MainMenuMerge mainMenuMerge = new MainMenuMerge(this);
			return mainMenuMerge.CopyMenu(src, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find a menu item based on the specified command id
		/// </summary>
		/// <param name="sCommandId">The Command Id string</param>
		/// <returns>The menu item that corresponds to the given command id, if any; otherwise
		/// <c>null</c>.</returns>
		/// ------------------------------------------------------------------------------------
		public MenuItem FindMenu(string sCommandId)
		{
			CheckDisposed();

			foreach (MenuItem key in m_htCmdIds.Keys)
			{
				if (m_htCmdIds[key] == sCommandId)
					return key;
			}

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize menu items based on the old menu item. Also remove the old menu item
		/// from our hash tables.
		/// </summary>
		/// <param name="oldMenuItem"></param>
		/// <param name="newMenuItem"></param>
		/// <param name="fRemoveOld"><c>true</c> to remove old menu item from menu extender
		/// (as when merging, as opposed to cloning, menus)</param>
		/// <remarks>NOTE: because the new menu item was created by CloneMenu, we have
		/// already subscribed to the events. Therefore we have to remove one of the
		/// two subscriptions.</remarks>
		/// ------------------------------------------------------------------------------------
		internal void InitializeMenuItem(MenuItem oldMenuItem, MenuItem newMenuItem,
			bool fRemoveOld)
		{
			CheckDisposed();

			if (oldMenuItem == null || newMenuItem == null)
				return;

			// If the command ID dictionary contains the old menu item, then add
			// the new menu item to the dictionary and remove the old one.
			if (m_htCmdIds.ContainsKey(oldMenuItem))
			{
				SetCommandId(newMenuItem, GetCommandId(oldMenuItem));
				newMenuItem.Click -= new EventHandler(OnMenuItemClick);
				if (fRemoveOld)
					m_htCmdIds.Remove(oldMenuItem);
			}

			// If the status bar text dictionary contains the old menu item, then add
			// the new menu item to the dictionary and remove the old one.
			if (m_htStatusMessage.ContainsKey(oldMenuItem))
			{
				SetStatusMessage(newMenuItem, GetStatusMessage(oldMenuItem));
				newMenuItem.Select -= new EventHandler(OnMenuSelect);
				if (fRemoveOld)
					m_htStatusMessage.Remove(oldMenuItem);
			}

			// If the image indexes hash table contains the old menu item, then add
			// the new menu item to the hash table and remove the old one.
			if (m_htImageIndexes.ContainsKey(oldMenuItem))
			{
				SetImageIndex(newMenuItem, GetImageIndex(oldMenuItem));
				if (fRemoveOld)
					m_htImageIndexes.Remove(oldMenuItem);
			}

			// If the image list dictionary contains the old menu item, then add
			// the new menu item to the dictionary and remove the old one.
			if (m_htImageLists.ContainsKey(oldMenuItem))
			{
				SetImageList(newMenuItem, GetImageList(oldMenuItem));
				if (fRemoveOld)
					m_htImageLists.Remove(oldMenuItem);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes sure information for the specified menu item is cleared from the menu
		/// extender's internal hash tables.
		/// </summary>
		/// <param name="menuItem"></param>
		/// ------------------------------------------------------------------------------------
		public void ClearMenuItem(MenuItem menuItem)
		{
			CheckDisposed();

			SetTag(menuItem, null);
			SetCommandId(menuItem, null);
			SetImageList(menuItem, null);
			SetImageIndex(menuItem, -1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refresh status of given menu item by using "Update" command.
		/// </summary>
		/// <param name="menuItem"></param>
		/// ------------------------------------------------------------------------------------
		public void RefreshStatus(Object menuItem)
		{
			CheckDisposed();

			if ((menuItem is MenuItem || menuItem is ContextMenu) && m_mediator != null)
			{
				MenuItem mi = menuItem as MenuItem;
				// call update for the main menu item
				if (mi != null && m_htCmdIds.ContainsKey(mi))
					m_mediator.SendMessage("Update" + m_htCmdIds[mi], menuItem);

				// loop through all the submenu items and call the update method (if there is one)
				Menu.MenuItemCollection menuItems;
				if (mi != null)
					menuItems = mi.MenuItems;
				else
					menuItems = ((ContextMenu)menuItem).MenuItems;

				foreach (MenuItem subMenuItem in menuItems)
				{
					if (m_htCmdIds.ContainsKey(subMenuItem))
					{
						// Call update method (e.g. OnUpdateEditCopy). If that method doesn't
						// exist, or if all update methods return false, we check for the
						// existence of the command handler.
						if (!m_mediator.SendMessage("Update" + m_htCmdIds[subMenuItem],
							subMenuItem))
						{
							// Check if there is a command handler for it (e.g. OnEditCopy)
							subMenuItem.Enabled = m_mediator.HasReceiver(m_htCmdIds[subMenuItem]);
						}
					}
				}
			}
		}
	}
}
