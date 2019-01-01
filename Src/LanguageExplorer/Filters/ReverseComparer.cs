// Copyright (c) 2004-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Xml.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// This class reverses the polarity of another IComparer.
	/// Note especially the Reverse(IComparer) static function, which creates
	/// a ReverseComparer if necessary, but can also unwrap an existing one to retrieve
	/// the original comparer.
	/// </summary>
	public class ReverseComparer : IComparer, IPersistAsXml, IStoresLcmCache, IStoresDataAccess, ICloneable
	{
		/// <summary />
		public ReverseComparer(IComparer comp)
		{
			SubComp = comp;
		}

		/// <summary>
		/// default for persistence
		/// </summary>
		public ReverseComparer()
		{
		}

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
			return -SubComp.Compare(x, y);
		}

		#endregion

		/// <summary>
		/// Gets the sub comp.
		/// </summary>
		public IComparer SubComp { get; private set; }

		/// <summary>
		/// Return a comparer with the opposite sense of comp. If it is itself a ReverseComparer,
		/// achieve this by unwrapping and returning the original comparer; otherwise, create
		/// a ReverseComparer.
		/// </summary>
		public static IComparer Reverse(IComparer comp)
		{
			var rc = comp as ReverseComparer;
			return rc == null ? new ReverseComparer(comp) : rc.SubComp;
		}

		#region IPersistAsXml Members

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public void PersistAsXml(XElement element)
		{
			DynamicLoader.PersistObject(SubComp, element, "comparer");
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public void InitXml(XElement element)
		{
			SubComp = DynamicLoader.RestoreFromChild(element, "comparer") as IComparer;
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
			if (GetType() != obj.GetType())
			{
				return false;
			}
			var that = (ReverseComparer)obj;
			return SubComp == null ? that.SubComp == null : that.SubComp != null && SubComp.Equals(that.SubComp);
		}

		/// <summary />
		public override int GetHashCode()
		{
			var hash = GetType().GetHashCode();
			if (SubComp != null)
			{
				hash *= SubComp.GetHashCode();
			}
			return hash;
		}

		public LcmCache Cache
		{
			set
			{
				if (SubComp is IStoresLcmCache)
				{
					((IStoresLcmCache)SubComp).Cache = value;
				}
			}
		}

		public ISilDataAccess DataAccess
		{
			set
			{
				if (SubComp is IStoresDataAccess)
				{
					((IStoresDataAccess)SubComp).DataAccess = value;
				}
			}
		}

		#region ICloneable Members
		/// <summary>
		/// Clones the current object
		/// </summary>
		public object Clone()
		{
			return new ReverseComparer(SubComp is ICloneable ? (IComparer)((ICloneable)SubComp).Clone() : SubComp);
		}
		#endregion
	}
}