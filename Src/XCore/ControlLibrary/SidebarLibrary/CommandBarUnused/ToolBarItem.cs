using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using SidebarLibrary.Win32;
using SidebarLibrary.Menus;


namespace SidebarLibrary.CommandBars
{
	/// <summary>
	/// Summary description for ToolBarItem.
	/// </summary>
	public enum ToolBarItemStyle
	{
		DropDownButton = 0,
		PushButton = 1,
		Separator = 2,
		ComboBox = 3
	}

	public class ToolBarItem
	{
		public event EventHandler Changed;
		public event EventHandler Click;
		public event EventHandler DropDown;

		Image image = null;
		string text = string.Empty;
		bool enabled = true;
		bool check = false;
		bool visible = true;
		bool dropped = false;
		bool raiseEvents = true;
		Keys shortCut = Keys.None;
		ToolBarItemStyle style = ToolBarItemStyle.PushButton;
		string toolTip = null;
		Object tag = null;
		EventHandler clickHandler = null;
		ComboBox comboBox = null;
		int index = -1;
		ToolBarEx parentToolBar = null;
		MenuItem[] menuItems = null;
		CommandBarMenu commandBarMenu = null;


		public ToolBarItem(ToolBarItemStyle style, ComboBox comboBox)
		{
			Debug.Assert(comboBox != null);
			Debug.Assert(style == ToolBarItemStyle.ComboBox);
			this.style = style;
			this.comboBox = comboBox;
		}

		public ToolBarItem()
		{
		}

		public ToolBarItem(string text)
		{
			Initialize(null, text, null, Keys.None, null);
		}

		public ToolBarItem(string text, EventHandler clickHandler)
		{
			Initialize(null, text, clickHandler, Keys.None, null);
		}

		public ToolBarItem(Image image, EventHandler clickHandler)
		{

			Initialize(image, null, clickHandler, Keys.None, null);
		}

		public ToolBarItem(Image image, string text, EventHandler clickHandler, Shortcut shortcut)
		{
			Initialize(image, text, clickHandler, shortCut, null);
		}

		public ToolBarItem(Image image, EventHandler clickHandler, Keys shortCut)
		{
			Initialize(image, null, clickHandler, shortCut, null);
		}

		public ToolBarItem(Image image, EventHandler clickHandler, Keys shortCut, string toolTip)
		{
			Initialize(image, null, clickHandler, shortCut, toolTip);
		}

		public ToolBarItem(Image image, string text, EventHandler clickHandler, Keys shortCut, string toolTip)
		{
			Initialize(image, text, clickHandler, shortCut, toolTip);
		}

		private void Initialize(Image image, string text, EventHandler clickHandler, Keys shortCut , string toolTip)
		{
			this.image = image;
			this.text = text;
			if ( clickHandler != null )
			{
				this.Click += clickHandler;
				this.clickHandler = clickHandler;
			}
			this.shortCut = shortCut;
			this.toolTip = toolTip;

		}

		internal bool RaiseEvents
		{
			set { raiseEvents = value;}
			get { return raiseEvents; }
		}

		public MenuItem[] MenuItems
		{
			set {
				if ( menuItems != value)
				{
					menuItems = value;
					commandBarMenu = new CommandBarMenu(menuItems);

					RaiseChanged();
				}
			}
			get { return menuItems; }
		}

		public CommandBarMenu ToolBarItemMenu
		{
			get { return commandBarMenu; }
		}

		public ComboBox ComboBox
		{
			get { return comboBox; }
		}

		public bool Visible
		{
			set { if ( visible != value ) { visible = value; RaiseChanged();} }
			get { return visible; }
		}

		public object Tag
		{
			// No need to raised a changed event since this does not alter the
			// state of the ToolbarItem but it is just a convinience for the user
			// to associate an object with the ToolbarItem
			set { tag = value;  }
			get { return tag; }
		}

		public Image Image
		{
			set { if ( image != value) { image = value; RaiseChanged();}  }
			get { return image; }
		}

		public string Text
		{
			set { if (text != value) { text = value; RaiseChanged(); } }
			get { return text; }
		}

		public bool Enabled
		{
			set
			{
				if (enabled != value)
				{
					enabled = value;
					if ( ComboBox != null)
						ComboBox.Enabled = value;
					RaiseChanged();
			}	}
			get { return enabled; }
		}

		public bool Checked
		{
			set { if (check != value) { check = value; RaiseChanged(); } }
			get { return check; }
		}

		public Keys Shortcut
		{
			set { if (shortCut != value) { shortCut = value; RaiseChanged(); } }
			get { return shortCut; }
		}

		public ToolBarItemStyle Style
		{
			set { if (style != value) { style = value; RaiseChanged(); } }
			get { return style; }
		}

		public string ToolTip
		{
			set { if (toolTip != value) { toolTip = value; RaiseChanged(); } }
			get { return toolTip; }
		}

		public int Index
		{
			set { index = value; }
			get { return index; }
		}

		public bool Dropped
		{
			set { dropped = value; }
			get { return dropped; }
		}

		public Rectangle ItemRectangle
		{
			get
			{
				// toolBar object must have been setup right before
				// rendering the toolbar by the toolbar itself and for all items
				Debug.Assert(parentToolBar != null);
				RECT rect = new RECT();
				WindowsAPI.SendMessage(parentToolBar.Handle, (int)ToolBarMessages.TB_GETRECT, index, ref rect);
				return new Rectangle(rect.left, rect.top, rect.right-rect.left, rect.bottom-rect.top);
			}
		}

		public ToolBarEx ToolBar
		{
			set { parentToolBar = value; }
		}

		public EventHandler ClickHandler
		{
			set { if (clickHandler != value) { clickHandler = value; RaiseChanged(); } }
			get { return clickHandler; }
		}

		void RaiseChanged()
		{
			if (Changed != null && raiseEvents ) Changed(this, EventArgs.Empty);
		}

		internal void RaiseClick()
		{
			if (Click != null && raiseEvents ) Click(this, EventArgs.Empty);
		}

		internal void RaiseDropDown()
		{
			if (DropDown != null && raiseEvents ) DropDown(this, EventArgs.Empty);
		}
	}
}
