using System;
using System.Collections;
using System.Windows.Forms;


namespace SidebarLibrary.Collections
{
	/// <summary>
	/// Summary description for RebarBandCollection.
	/// </summary>
	public class RebarBandCollection : IEnumerable
	{
		public event EventHandler Changed;
		ArrayList bands = new ArrayList();

		public IEnumerator GetEnumerator()
		{
			return bands.GetEnumerator();
		}

		public int Count
		{
			get { return bands.Count; }
		}

		public int Add(Control control)
		{
			if (Contains(control)) return -1;
			int index = bands.Add(control);
			RaiseChanged();
			return index;
		}

		public void Clear()
		{
			while (Count > 0) RemoveAt(0);
		}

		public bool Contains(Control control)
		{
			return bands.Contains(control);
		}

		public int IndexOf(Control control)
		{
			return bands.IndexOf(control);
		}

		public void Remove(Control control)
		{
			bands.Remove(control);
			RaiseChanged();
		}

		public void RemoveAt(int index)
		{
			bands.RemoveAt(index);
			RaiseChanged();
		}

		public Control this[int index]
		{
			get { return (Control) bands[index]; }
		}

		void RaiseChanged()
		{
			if (Changed != null) Changed(this, null);
		}
	}
}
