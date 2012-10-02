using System;
using System.Collections;
using System.Windows.Forms;
using SidebarLibrary.WinControls;

namespace SidebarLibrary.Collections
{
	/// <summary>
	/// Summary description for OutlookBarItemCollection.
	/// </summary>
	public class OutlookBarItemCollection : IEnumerable
	{
		public OutlookBarItemCollection()
		{

		}
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

		public int Add(OutlookBarItem item)
		{
			if (Contains(item)) return -1;
			int index = items.Add(item);
			RaiseChanged();
			return index;
		}

		public void Clear()
		{
			while (Count > 0) RemoveAt(0);
		}

		public bool Contains(OutlookBarItem item)
		{
			return items.Contains(item);
		}

		public int IndexOf(OutlookBarItem item)
		{
			return items.IndexOf(item);
		}

		public void Remove(OutlookBarItem item)
		{
			items.Remove(item);
			RaiseChanged();
		}

		public void RemoveAt(int index)
		{
			items.RemoveAt(index);
			RaiseChanged();
		}

		public void Insert(int index, OutlookBarItem item)
		{
			items.Insert(index, item);
			RaiseChanged();
		}

		public OutlookBarItem this[int index]
		{
			get { return (OutlookBarItem) items[index]; }
			set {  items[index] = value; }
		}

		void RaiseChanged()
		{
			if (Changed != null) Changed(this, null);
		}

	}
}
