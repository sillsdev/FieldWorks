// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
using System.Xml.Linq;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// This class compares two integers represented as strings using integer comparison.
	/// </summary>
	public class IntStringComparer : IComparer, IPersistAsXml
	{
		#region IComparer Members

		/// <summary>
		/// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
		/// </summary>
		/// <param name="x">The first object to compare.</param>
		/// <param name="y">The second object to compare.</param>
		/// <returns>
		/// Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero x is greater than y.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">Neither x nor y implements the <see cref="T:System.IComparable"></see> interface.-or- x and y are of different types and neither one can handle comparisons with the other. </exception>
		public int Compare(object x, object y)
		{
			var xn = int.Parse(x.ToString());
			var yn = int.Parse(y.ToString());
			if (xn < yn)
			{
				return -1;
			}
			if (xn > yn)
			{
				return 1;
			}
			return 0;
		}

		#endregion

		#region IPersistAsXml Members

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public void PersistAsXml(XElement node)
		{
			// nothing to do.
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public void InitXml(XElement node)
		{
			// Nothing to do
		}

		#endregion

		/// <summary />
		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			// TODO-Linux: System.Boolean System.Type::op_Inequality(System.Type,System.Type)
			// is marked with [MonoTODO] and might not work as expected in 4.0.
			return GetType() == obj.GetType();
		}

		/// <summary />
		public override int GetHashCode()
		{
			return GetType().GetHashCode();
		}
	}
}