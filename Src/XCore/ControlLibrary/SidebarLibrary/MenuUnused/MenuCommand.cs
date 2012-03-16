#if USE_THIS
using System;
using System.Collections;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing;
using SidebarLibrary.Collections;

namespace SidebarLibrary.Menus
{
	[ToolboxItem(false)]
	public class MenuCommand : Component
	{
		// Enumeration of property change events
		public enum Property
		{
			Text,
			Enabled,
			ImageIndex,
			ImageList,
			Shortcut,
			Checked,
			RadioCheck,
			Break,
			Infrequent,
			Visible,
			Image,
			RealOwner,
			ComboBox
		}

		// Declare the property change event signature
		public delegate void PropChangeHandler(MenuCommand item, Property prop);

		// Public events
		public event PropChangeHandler PropertyChanged;

		// Instance fields
		protected bool visible;
		protected bool _break;
		protected string text;
		protected bool enabled;
		protected bool _checked;
		protected int imageIndex;
		protected bool infrequent;
		protected object userData;
		protected bool radioCheck;
		protected Shortcut shortcut;
		protected ImageList imageList;
		protected Bitmap image;
		protected Object realOwner;
		protected ComboBox comboBox;
		protected MenuCommandCollection menuItems;

		// Exposed events
		public event EventHandler Click;
		public event EventHandler Update;

		public MenuCommand()
		{
			InternalConstruct("MenuItem", null, -1, Shortcut.None, null, null, null);
		}

		public MenuCommand(string text)
		{
			InternalConstruct(text, null, -1, Shortcut.None, null, null, null);
		}

		public MenuCommand(string text, EventHandler clickHandler)
		{
			InternalConstruct(text, null, -1, Shortcut.None, clickHandler, null, null);
		}

		public MenuCommand(string text, Shortcut shortcut)
		{
			InternalConstruct(text, null, -1, shortcut, null, null, null);
		}

		public MenuCommand(string text, Shortcut shortcut, EventHandler clickHandler)
		{
			InternalConstruct(text, null, -1, shortcut, clickHandler, null, null);
		}

		public MenuCommand(string text, ImageList imageList, int imageIndex)
		{
			InternalConstruct(text, imageList, imageIndex, Shortcut.None, null, null, null);
		}

		public MenuCommand(string text, ImageList imageList, int imageIndex, Shortcut shortcut)
		{
			InternalConstruct(text, imageList, imageIndex, shortcut, null, null, null);
		}

		public MenuCommand(string text, ImageList imageList, int imageIndex,
						   Shortcut shortcut, EventHandler clickHandler)
		{
			InternalConstruct(text, imageList, imageIndex, shortcut, clickHandler, null, null);
		}

		public MenuCommand(string text, Bitmap bitmap, Shortcut shortcut, EventHandler clickHandler, Object realOwner)
		{
			InternalConstruct(text, null, -1, shortcut, clickHandler, bitmap, realOwner);
		}

		public MenuCommand(ComboBox comboBox)
		{
			InternalConstruct("", null, -1, Shortcut.None, null, null, null);
			this.comboBox = comboBox;
		}


		protected void InternalConstruct(string text, ImageList imageList, int imageIndex,
										 Shortcut shortcut, EventHandler clickHandler, Bitmap bitmap, object realOwner)
		{
			// Save parameters
			this.text = text;
			this.imageList = imageList;
			this.imageIndex = imageIndex;
			this.shortcut = shortcut;
			image = bitmap;
			this.realOwner = realOwner;
			this.comboBox = null;

			if (clickHandler != null)
				Click += clickHandler;

			// Define defaults for others
			enabled = true;
			_checked = false;
			radioCheck = false;
			_break = false;
			userData = null;
			visible = true;
			infrequent = false;

			// Create the collection of embedded menu commands
			menuItems = new MenuCommandCollection();
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public MenuCommandCollection MenuCommands
		{
			get { return menuItems; }

			set
			{
				menuItems.Clear();
				menuItems = value;
			}
		}

		public string Text
		{
			get { return text; }

			set
			{
				if (text != value)
				{
					text = value;

					// Any attached event handlers?
					if (PropertyChanged != null)
					{
						// Raise event to notify property value has changed
						PropertyChanged(this, Property.Text);
					}
				}
			}
		}

		public bool Enabled
		{
			get { return enabled; }

			set
			{
				if (enabled != value)
				{
					enabled = value;

					// Any attached event handlers?
					if (PropertyChanged != null)
					{
						// Raise event to notify property value has changed
						PropertyChanged(this, Property.Enabled);
					}
				}
			}
		}

		public int ImageIndex
		{
			get { return imageIndex; }

			set
			{
				if (imageIndex != value)
				{
					imageIndex = value;

					// Any attached event handlers?
					if (PropertyChanged != null)
					{
						// Raise event to notify property value has changed
						PropertyChanged(this, Property.ImageIndex);
					}
				}
			}
		}

		public ImageList ImageList
		{
			get { return imageList; }

			set
			{
				if (imageList != value)
				{
					imageList = value;

					// Any attached event handlers?
					if (PropertyChanged != null)
					{
						// Raise event to notify property value has changed
						PropertyChanged(this, Property.ImageList);
					}
				}
			}
		}

		public Shortcut Shortcut
		{
			get { return shortcut; }

			set
			{
				if (shortcut != value)
				{
					shortcut = value;

					// Any attached event handlers?
					if (PropertyChanged != null)
					{
						// Raise event to notify property value has changed
						PropertyChanged(this, Property.Shortcut);
					}
				}
			}
		}

		public bool Checked
		{
			get { return _checked; }

			set
			{
				if (_checked != value)
				{
					_checked = value;

					// Any attached event handlers?
					if (PropertyChanged != null)
					{
						// Raise event to notify property value has changed
						PropertyChanged(this, Property.Checked);
					}
				}
			}
		}

		public bool RadioCheck
		{
			get { return radioCheck; }

			set
			{
				if (radioCheck != value)
				{
					radioCheck = value;

					// Any attached event handlers?
					if (PropertyChanged != null)
					{
						// Raise event to notify property value has changed
						PropertyChanged(this, Property.RadioCheck);
					}
				}
			}
		}

		public bool Break
		{
			get { return _break; }

			set
			{
				if (_break != value)
				{
					_break = value;

					// Any attached event handlers?
					if (PropertyChanged != null)
					{
						// Raise event to notify property value has changed
						PropertyChanged(this, Property.Break);
					}
				}
			}
		}

		public bool Infrequent
		{
			get { return infrequent; }

			set
			{
				if (infrequent != value)
				{
					infrequent = value;

					// Any attached event handlers?
					if (PropertyChanged != null)
					{
						// Raise event to notify property value has changed
						PropertyChanged(this, Property.Infrequent);
					}
				}
			}
		}

		public bool Visible
		{
			get { return visible; }

			set
			{
				if (visible != value)
				{
					visible = value;

					// Any attached event handlers?
					if (PropertyChanged != null)
					{
						// Raise event to notify property value has changed
						PropertyChanged(this, Property.Visible);
					}
				}
			}
		}

		public Bitmap Image
		{
			get { return image; }

			set
			{
				if (image != value)
				{
					image = value;

					// Any attached event handlers?
					if (PropertyChanged != null)
					{
						// Raise event to notify property value has changed
						PropertyChanged(this, Property.Image);
					}
				}
			}
		}

		public Object RealOwner
		{
			get { return realOwner; }

			set
			{
				if (realOwner != value)
				{
					realOwner = value;

					// Any attached event handlers?
					if (PropertyChanged != null)
					{
						// Raise event to notify property value has changed
						PropertyChanged(this, Property.RealOwner);
					}
				}
			}
		}

		public ComboBox ComboBox
		{
			get { return comboBox; }

			set
			{
				if (comboBox != value)
				{
					comboBox = value;

					// Any attached event handlers?
					if (PropertyChanged != null)
					{
						// Raise event to notify property value has changed
						PropertyChanged(this, Property.ComboBox);
					}
				}
			}
		}

		public object UserData
		{
			get { return userData; }
			set { userData = value; }
		}

		public virtual void OnClick(EventArgs e)
		{
			if (Click != null)
			{
				if ( realOwner != null )
					Click(realOwner, e);
				else
				  Click(this, e);
			}
		}

		public virtual void OnUpdate(EventArgs e)
		{
			if (Update != null)
				Update(this, e);
		}
	}
}
#endif
