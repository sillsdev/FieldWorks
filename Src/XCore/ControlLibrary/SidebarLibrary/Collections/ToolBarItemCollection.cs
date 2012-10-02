using System;
using System.Diagnostics;
using System.Collections;
using System.Windows.Forms;
using SidebarLibrary.CommandBars;

namespace SidebarLibrary.Collections
{
	/// <summary>
	/// Summary description for ToolbarButtonCollection.
	/// </summary>
	public class ToolBarItemCollection : IEnumerable
	{
			public event EventHandler Changed;
			ArrayList items = new ArrayList();

			public IEnumerator GetEnumerator()
			{
				return items.GetEnumerator();
			}

			public int Count
			{
				get { return items.Count; }
			}

			public void Clear()
			{
				while (Count > 0)
					RemoveAt(0);
			}

			public void Add(ToolBarItem item)
			{
				items.Add(item);
				RaiseChanged();
			}

			public void AddRange(ToolBarItem[] items)
			{
				foreach (ToolBarItem item in items)
					this.items.Add(item);
				RaiseChanged();
			}

			public void Insert(int index, ToolBarItem item)
			{
				items.Insert(index, item);
				RaiseChanged();
			}

			public void RemoveAt(int index)
			{
				//ToolBarItem item = (ToolBarItem)items[index];
				items.RemoveAt(index);
				RaiseChanged();
			}

			public void Remove(ToolBarItem item)
			{
				if (!items.Contains(item)) return;
				items.Remove(item);
				RaiseChanged();
			}

			public bool Contains(ToolBarItem item)
			{
				return items.Contains(item);
			}

			public int IndexOf(ToolBarItem item)
			{
				return items.IndexOf(item);
			}

			public ToolBarItem this[int index]
			{
				get { return (ToolBarItem)items[index]; }
			}

			internal ToolBarItem this[Keys shortcut]
			{
				get
				{
					foreach (ToolBarItem item in items)
						if ( (item.Shortcut == shortcut) && (item.Enabled) && (item.Visible) )
							return item;
					return null;
				}
			}

			internal ToolBarItem this[char mnemonic]
			{
				get
				{
					foreach (ToolBarItem item in items)
					{
						string text = item.Text;
						if ( text != string.Empty && text != null )
						{
							for (int i = 0; i < text.Length; i++)
							{
								if ((text[i] == '&') && (i + 1 < text.Length) && (text[i + 1] != '&'))
									if (mnemonic == Char.ToUpper(text[i + 1]))
										return item;
							}
						}
					}
					return null;
				}
			}

			void RaiseChanged()
			{
				if (Changed != null) Changed(this, null);
			}
	}


}
