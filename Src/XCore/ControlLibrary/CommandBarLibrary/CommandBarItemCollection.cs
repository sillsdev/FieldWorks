// ---------------------------------------------------------
// Windows Forms CommandBar Control
// Copyright (C) 2001-2003 Lutz Roeder. All rights reserved.
// http://www.aisto.com/roeder
// roeder@aisto.com
// ---------------------------------------------------------
namespace Reflector.UserInterface
{
	using System;
	using System.Collections;
	using System.ComponentModel;
	using System.Diagnostics.CodeAnalysis;
	using System.Drawing;
	using System.Globalization;
	using System.Windows.Forms;

	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="Parent should dispose items in this collectioin.")]
	public class CommandBarItemCollection : ICollection
	{
		private CommandBar commandBar;
		private ArrayList items;

		internal CommandBarItemCollection()
		{
			this.commandBar = null;
			this.items = new ArrayList();
		}

		internal CommandBarItemCollection(CommandBar commandBar)
		{
			this.commandBar = commandBar;
			this.items = new ArrayList();
		}

		public IEnumerator GetEnumerator()
		{
			return this.items.GetEnumerator();
		}

		public int Count
		{
			get { return this.items.Count; }
		}

		public void Clear()
		{
			while (this.Count > 0)
			{
				this.RemoveAt(0);
			}
		}

		public void Add(CommandBarItem item)
		{
			this.items.Add(item);

			if (this.commandBar != null)
			{
				this.commandBar.AddItem(item);
			}
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="Add to MenuItems and disposed in parent's Dispose")]
		public void AddSeparator()
		{
			this.Add(new CommandBarSeparator());
		}

		public CommandBarMenu AddMenu(string text)
		{
			CommandBarMenu menu = new CommandBarMenu(text);
			this.Add(menu);
			return menu;
		}

		public CommandBarMenu AddMenu(Image image, string text)
		{
			CommandBarMenu menu = this.AddMenu(text);
			menu.Image = image;
			return menu;
		}

		public CommandBarMenu AddMenu(string text, EventHandler dropDownHandler)
		{
			CommandBarMenu menu = this.AddMenu(text);
			menu.DropDown += dropDownHandler;
			return menu;
		}

		public CommandBarMenu AddMenu(Image image, string text, EventHandler dropDownHandler)
		{
			CommandBarMenu menu = this.AddMenu(text);
			menu.Image = image;
			menu.DropDown += dropDownHandler;
			return menu;
		}

		public CommandBarButton AddButton(string text, EventHandler clickHandler)
		{
			CommandBarButton button = new CommandBarButton(text);
			button.Click += clickHandler;
			this.Add(button);
			return button;
		}

		public CommandBarButton AddButton(string text, EventHandler clickHandler, Keys shortcut)
		{
			CommandBarButton button = this.AddButton(text, clickHandler);
			button.Shortcut = shortcut;
			return button;
		}

		public CommandBarButton AddButton(Image image, string text, EventHandler clickHandler)
		{
			CommandBarButton button = this.AddButton(text, clickHandler);
			button.Image = image;
			return button;
		}

		public CommandBarButton AddButton(Image image, string text, EventHandler clickHandler, Keys shortcut)
		{
			CommandBarButton button = this.AddButton(text, clickHandler, shortcut);
			button.Image = image;
			return button;
		}

		public CommandBarCheckBox AddCheckBox(string text)
		{
			CommandBarCheckBox checkBox = new CommandBarCheckBox(text);
			this.Add(checkBox);
			return checkBox;
		}

		public CommandBarCheckBox AddCheckBox(string text, Keys shortcut)
		{
			CommandBarCheckBox checkBox = this.AddCheckBox(text);
			checkBox.Shortcut = shortcut;
			return checkBox;
		}

		public CommandBarCheckBox AddCheckBox(Image image, string text, Keys shortcut)
		{
			CommandBarCheckBox checkBox = this.AddCheckBox(text, shortcut);
			checkBox.Image = image;
			return checkBox;
		}

		public CommandBarCheckBox AddCheckBox(Image image, string text)
		{
			CommandBarCheckBox checkBox = this.AddCheckBox(text);
			checkBox.Image = image;
			return checkBox;
		}

		public CommandBarComboBox AddComboBox(string text, ComboBox comboBox)
		{
			CommandBarComboBox item = new CommandBarComboBox(text, comboBox);
			this.Add(item);
			return item;
		}

		public void AddRange(ICollection items)
		{
			foreach (CommandBarItem item in items)
			{
				this.items.Add(item);

				if (this.commandBar != null)
				{
					this.commandBar.AddItem(item);
				}
			}
		}

		public void Insert(int index, CommandBarItem item)
		{
			items.Insert(index, item);

			if (this.commandBar != null)
			{
				this.commandBar.AddItem(item);
			}
		}

		public void RemoveAt(int index)
		{
			CommandBarItem item = (CommandBarItem) this.items[index];
			items.RemoveAt(index);

			if (this.commandBar != null)
			{
				this.commandBar.RemoveItem(item);
			}
		}

		public void Remove(CommandBarItem item)
		{
			if (this.items.Contains(item))
			{
				this.items.Remove(item);

				if (this.commandBar != null)
				{
					this.commandBar.RemoveItem(item);
				}
			}
		}

		public bool Contains(CommandBarItem item)
		{
			return this.items.Contains(item);
		}

		public int IndexOf(CommandBarItem item)
		{
			return this.items.IndexOf(item);
		}

		public CommandBarItem this[int index]
		{
			get { return (CommandBarItem)items[index]; }
		}

		public object SyncRoot
		{
			get { throw new NotSupportedException(); }
		}

		public bool IsSynchronized
		{
			get { return false; }
		}

		public void CopyTo(Array array, int index)
		{
			this.items.CopyTo(array, index);
		}

		public void CopyTo(CommandBarItem[] array, int index)
		{
			this.items.CopyTo(array, index);
		}

		// TODO
		internal CommandBarItem[] this[Keys shortcut]
		{
			get
			{
				ArrayList list = new ArrayList();

				foreach (CommandBarItem item in items)
				{
					CommandBarButtonBase buttonBase = item as CommandBarButtonBase;
					if (buttonBase != null)
					{
						if ((buttonBase.Shortcut == shortcut) && (buttonBase.IsEnabled) && (buttonBase.IsVisible))
						{
							list.Add(buttonBase);
						}
					}
				}

				foreach (CommandBarItem item in items)
				{
					CommandBarMenu menu = item as CommandBarMenu;
					if (menu != null)
					{
						list.AddRange(menu.Items[shortcut]);
					}
				}

				CommandBarItem[] array = new CommandBarItem[list.Count];
				list.CopyTo(array, 0);
				return array;
			}
		}

		// TODO Only used in CommandBar.PreProcessMnemonic
		internal CommandBarItem[] this[char mnemonic]
		{
			get
			{
				ArrayList list = new ArrayList();

				foreach (CommandBarItem item in items)
				{
					string text = item.Text;
					for (int i = 0; i < text.Length; i++)
					{
						if ((text[i] == '&') && (i + 1 < text.Length) && (text[i + 1] != '&'))
							if (mnemonic == Char.ToUpper(text[i + 1], CultureInfo.InvariantCulture))
								list.Add(item);
					}
				}

				CommandBarItem[] array = new CommandBarItem[list.Count];
				list.CopyTo(array, 0);
				return array;
			}
		}
	}
}
