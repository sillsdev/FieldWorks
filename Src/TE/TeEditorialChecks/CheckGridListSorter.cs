using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Diagnostics;

namespace SIL.FieldWorks.TE.TeEditorialChecks
{
	#region ICheckGridRowObject interface
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface ICheckGridRowObject
	{
		/// <summary></summary>
		object GetPropValue(string propName);
	}

	#endregion

	#region GenericComparer class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Implements a generic comparing class in which the two objects being compared must
	/// derive from IComparable to be of much use.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class GenericComparer : IComparer
	{
		#region IComparer Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Compare(object x, object y)
		{
			IComparable xc = x as IComparable;
			IComparable yc = y as IComparable;
			return (xc == null || yc == null ? 0 : xc.CompareTo(yc));
		}

		#endregion
	}

	#endregion

	#region StableSortInfo class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class StableSortInfo
	{
		/// <summary></summary>
		public string PropName;
		/// <summary></summary>
		public SortOrder SortDirection = SortOrder.Ascending;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StableSortInfo(string propName, SortOrder direction)
		{
			PropName = propName;
			SortDirection = direction;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return string.Format("{0}, {1}", PropName, SortDirection);
		}
	}

	#endregion

	#region CheckGridListSorter class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class CheckGridListSorter
	{
		private List<ICheckGridRowObject> m_list;
		private List<StableSortInfo> m_stableOrder = new List<StableSortInfo>();
		private Dictionary<string, IComparer> m_colComparers = new Dictionary<string, IComparer>();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructs a new CheckGridListSorter for the specified list of
		/// ICheckGridRowObjects objects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CheckGridListSorter(List<ICheckGridRowObject> list)
		{
			m_list = list;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a comparer for property <paramref name="propName"/>.
		/// </summary>
		/// <param name="propName">Name of the property.</param>
		/// <param name="comparer">The comparer object</param>
		/// <remarks>Adding a comparer for a property allows that property to be sorted by the
		/// specified comparer (e.g. when the user clicks on the corresponding column header in
		/// a datagrid view.</remarks>
		/// ------------------------------------------------------------------------------------
		public void AddComparer(string propName, IComparer comparer)
		{
			m_colComparers[propName] = comparer;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SetFirstSortPropName(string propName, bool toggleDirection)
		{
			SortOrder direction = SortOrder.Ascending;

			for (int i = 0; i < m_stableOrder.Count; i++)
			{
				if (m_stableOrder[i].PropName == propName)
				{
					direction = m_stableOrder[i].SortDirection;
					if (toggleDirection)
					{
						direction = (direction == SortOrder.Ascending ?
							SortOrder.Descending : SortOrder.Ascending);
					}

					m_stableOrder.RemoveAt(i);
					break;
				}
			}

			m_stableOrder.Insert(0, new StableSortInfo(propName, direction));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sorts the list, making the specified property name the primary sort field and
		/// toggles the sort direction if that field is already in the sort order. If not,
		/// then the direction is ascending.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Sort(string sortPropName, bool toggleDirection)
		{
			SetFirstSortPropName(sortPropName, toggleDirection);
			Sort();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sorts the list using the current sort order.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Sort()
		{
			DateTime start = DateTime.Now;
			Debug.WriteLine("Beginning sort at: " + start);
			m_list.Sort(RowComparer);
			DateTime end = DateTime.Now;
			Debug.WriteLine("Completed sort at: " + end + " (" + (end - start) + ")");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares two objects and returns a value indicating whether one is less than,
		/// equal to, or greater than the other.
		/// </summary>
		/// <param name="x">The first object to compare.</param>
		/// <param name="y">The second object to compare.</param>
		/// <returns>
		/// Value Condition Less than zero <paramref name="x"/> is less than
		/// <paramref name="y"/>. Zero <paramref name="x"/> equals <paramref name="y"/>.
		/// Greater than zero <paramref name="x"/> is greater than <paramref name="y"/>.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">Neither <paramref name="x"/> nor
		/// <paramref name="y"/> implements the <see cref="T:System.IComparable"/>
		/// interface.-or- <paramref name="x"/> and <paramref name="y"/> are of different
		/// types and neither one can handle comparisons with the other. </exception>
		/// ------------------------------------------------------------------------------------
		public int RowComparer(object x, object y)
		{
			ICheckGridRowObject xcgrobj = x as ICheckGridRowObject;
			ICheckGridRowObject ycgrobj = y as ICheckGridRowObject;

			if (xcgrobj != null && ycgrobj != null)
			{
				foreach (StableSortInfo ssinfo in m_stableOrder)
				{
					IComparer comparer;
					if (m_colComparers.TryGetValue(ssinfo.PropName, out comparer))
					{
						int result = comparer.Compare(
							xcgrobj.GetPropValue(ssinfo.PropName),
							ycgrobj.GetPropValue(ssinfo.PropName));

						if (result != 0)
						{
							return (ssinfo.SortDirection == SortOrder.Ascending ?
								result : result * -1);
						}
					}
				}

				return 0;
			}

			IComparable xcomp = x as IComparable;
			IComparable ycomp = y as IComparable;
			if (xcomp != null && ycomp != null)
				return xcomp.CompareTo(ycomp);

			if (xcomp == null && ycomp != null)
				return -1;

			return (xcomp != null ? 1 : 0);
		}

		#region Methods for reading and writing the sort order info. to the registry.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reads the sort order information from the registry.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ReadSortInfoFromReg(RegistryKey key)
		{
			if (key == null)
				return false;

			bool valuesRead = false;
			m_stableOrder.Clear();
			string val;
			int i = 0;

			while ((val = key.GetValue("SortField" + i++, null) as string) != null)
			{
				string[] values = val.Split(',');
				if (values.Length >= 2)
				{
					SortOrder order = SortOrder.Ascending;
					try
					{
						order = (SortOrder)Enum.Parse(typeof(SortOrder), values[1]);
						m_stableOrder.Add(new StableSortInfo(values[0], order));
						valuesRead = true;
					}
					catch
					{
						break;
					}
				}
			}

			return valuesRead;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Writes the sort order information to the registry.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void WriteSortInfoToReg(RegistryKey key)
		{
			string fmt = "{0},{1}";
			for (int i = 0; i < m_stableOrder.Count; i++)
			{
				key.SetValue("SortField" + i, string.Format(fmt,
					m_stableOrder[i].PropName, m_stableOrder[i].SortDirection));
			}
		}

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the primary sort property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string PrimarySortProperty
		{
			get {return (m_stableOrder.Count == 0 ? null : m_stableOrder[0].PropName);}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the direction of the primary sort property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SortOrder PrimarySortDirection
		{
			get
			{
				return (m_stableOrder.Count == 0 ?
					SortOrder.Ascending : m_stableOrder[0].SortDirection);
			}
		}

		#endregion
	}

	#endregion
}
