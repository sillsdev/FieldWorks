// Copyright (c) 2004-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Windows.Forms;
using System.Xml.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Utils;
using SIL.Xml;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// This class compares two ManyOnePathSortItems by making use of the ability of a StringFinder
	/// object to obtain strings from the KeyObject hvo, then
	/// a (simpler) IComparer to compare the strings.
	/// </summary>
	public class StringFinderCompare : IComparer, IPersistAsXml, IStoresLcmCache, IStoresDataAccess, ICloneable
	{
		/// <summary>This is used, during a single sort, to cache keys.</summary>
		protected Hashtable m_objToKey = new Hashtable();
		internal INoteComparision ComparisonNoter { get; set; }

		/// <summary />
		public StringFinderCompare(IStringFinder finder, IComparer subComp)
		{
			Finder = finder;
			SubComparer = subComp;
			SortedFromEnd = false;
			SortedByLength = false;
		}

		/// <summary>
		/// For use with IPersistAsXml
		/// </summary>
		public StringFinderCompare() : this(null, null)
		{
		}

		/// <summary>
		/// Gets the finder.
		/// </summary>
		public IStringFinder Finder { get; protected set; }

		public ISilDataAccess DataAccess
		{
			set
			{
				if (Finder != null)
				{
					Finder.DataAccess = value;
				}
			}
		}

		/// <summary>
		/// May be implemented to preload before sorting large collections (typically most of the instances).
		/// Currently will not be called if the column is also filtered; typically the same Preload() would end
		/// up being done.
		/// </summary>
		public virtual void Preload(object rootObj)
		{
			Finder.Preload(rootObj);
		}

		/// <summary>
		/// Gets the sub comparer.
		/// </summary>
		public IComparer SubComparer { get; protected set; }

		/// <summary>
		/// Copy our comparer's SubComparer and SortedFromEnd to another comparer.
		/// </summary>
		public void CopyTo(StringFinderCompare copyComparer)
		{
			copyComparer.SubComparer = SubComparer is ICloneable ? ((ICloneable)SubComparer).Clone() as IComparer : SubComparer;
			copyComparer.SortedFromEnd = SortedFromEnd;
			copyComparer.SortedByLength = SortedByLength;
		}

		/// <summary>
		/// Add to collector the ManyOnePathSortItems which this sorter derives from
		/// the specified object. This default method makes a single mopsi not involving any
		/// path.
		/// </summary>
		public void CollectItems(int hvo, ArrayList collector)
		{
			Finder.CollectItems(hvo, collector);
		}

		/// <summary>
		/// Give the sort order. Considered to be ascending unless it has been reversed.
		/// </summary>
		public SortOrder Order => SubComparer is ReverseComparer ? SortOrder.Descending : SortOrder.Ascending;

		/// <summary>
		/// Flag whether to sort normally from the beginnings of words, or to sort from the
		/// ends of words.  This is useful for grouping words by suffix.
		/// </summary>
		public bool SortedFromEnd { get; set; }

		/// <summary>
		/// Flag whether to sort normally from the beginnings of words, or to sort from the
		/// ends of words.  This is useful for grouping words by suffix.
		/// </summary>
		public bool SortedByLength { get; set; }

		/// <summary>
		/// Inits this instance.
		/// </summary>
		public void Init()
		{
			m_objToKey.Clear();
			if (SubComparer is IcuComparer)
			{
				((IcuComparer)SubComparer).OpenCollatingEngine();
			}
			else if (SubComparer is ReverseComparer)
			{
				var ct = ReverseComparer.Reverse(SubComparer);
				if (ct is IcuComparer)
				{
					((IcuComparer)ct).OpenCollatingEngine();
				}
			}
		}

		/// <summary>
		/// Cleanups this instance. Note that clients are not obliged to call this; it just
		/// helps a little with garbage collection.
		/// </summary>
		public void Cleanup()
		{
			m_objToKey.Clear(); // redundant, but may help free memory.
			if (SubComparer is IcuComparer)
			{
				((IcuComparer)SubComparer).CloseCollatingEngine();
			}
			else if (SubComparer is ReverseComparer)
			{
				var ct = ReverseComparer.Reverse(SubComparer);
				if (ct is IcuComparer)
				{
					((IcuComparer)ct).CloseCollatingEngine();
				}
			}
		}

		/// <summary>
		/// Reverse the order of the sort.
		/// </summary>
		public void Reverse()
		{
			SubComparer = ReverseComparer.Reverse(SubComparer);
		}

		protected internal string[] GetValue(object key, bool sortedFromEnd)
		{
			try
			{
				var result = m_objToKey[key] as string[];
				if (result != null)
				{
					return result;
				}
				var item = key as IManyOnePathSortItem;
				result = Finder.SortStrings(item, sortedFromEnd);
				m_objToKey[key] = result;
				return result;
			}
			catch (Exception e)
			{
				throw new Exception("StringFinderCompare could not get key for " + key, e);
			}
		}

		#region IComparer Members

		/// <summary>
		/// Compares two objects and returns a value indicating whether one is less than, equal
		/// to, or greater than the other.
		/// </summary>
		/// <param name="x">The first object to compare.</param>
		/// <param name="y">The second object to compare.</param>
		/// <returns>
		/// Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero
		/// x is greater than y.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">Neither x nor y implements the
		/// <see cref="T:System.IComparable"></see> interface.-or- x and y are of different
		/// types and neither one can handle comparisons with the other. </exception>
		public int Compare(object x, object y)
		{
			ComparisonNoter?.ComparisonOccurred(); // for progress reporting.

			try
			{
				if (x == y)
				{
					return 0;
				}
				if (x == null)
				{
					return SubComparer is ReverseComparer ? 1 : -1;
				}
				if (y == null)
				{
					return SubComparer is ReverseComparer ? -1 : 1;
				}
				// We pass GetValue m_fSortedFromEnd, and it is responsible for taking care of flipping the string if that's needed.
				// The reason to do it there is because sometimes a sort key will have a homograph number appeneded to the end.
				// If we do the flipping here, the resulting string will be a backwards homograph number followed by the backwards
				// sort key itself.  GetValue should know enough to return the flipped string followed by a regular homograph number.
				var keysA = GetValue(x, SortedFromEnd);
				var keysB = GetValue(y, SortedFromEnd);
				// There will usually only be one element in the array, but just in case...
				if (SortedFromEnd)
				{
					Array.Reverse(keysA);
					Array.Reverse(keysB);
				}
				var cstrings = Math.Min(keysA.Length, keysB.Length);
				for (var i = 0; i < cstrings; i++)
				{
					// Sorted by length (if enabled) will be the primary sorting factor
					if (SortedByLength)
					{
						var cchA = OrthographicLength(keysA[i]);
						var cchB = OrthographicLength(keysB[i]);
						if (cchA < cchB)
						{
							return SubComparer is ReverseComparer ? 1 : -1;
						}
						if (cchB < cchA)
						{
							return SubComparer is ReverseComparer ? -1 : 1;
						}
					}
					// However, if there's no difference in length, we continue with the
					// rest of the sort
					var result = SubComparer.Compare(keysA[i], keysB[i]);
					if (result != 0)
					{
						return result;
					}
				}
				// All corresponding strings are equal according to the comparer, so sort based on the number of strings
				if (keysA.Length < keysB.Length)
				{
					return SubComparer is ReverseComparer ? 1 : -1;
				}
				if (keysA.Length > keysB.Length)
				{
					return SubComparer is ReverseComparer ? -1 : 1;
				}
				return 0;
			}
			catch (Exception error)
			{
				throw new Exception("Comparing objects failed", error);
			}
		}

		/// <summary>
		/// Count the number of orthographic characters (word-forming, nondiacritic) in the
		/// key.
		/// </summary>
		private int OrthographicLength(string key)
		{
			var cchOrtho = 0;
			var rgch = key.ToCharArray();
			for (var i = 0; i < rgch.Length; ++i)
			{
				// Handle surrogate pairs carefully!
				int ch;
				var ch1 = rgch[i];
				// if the character is a lead surrogate, and there is a following character
				if (Surrogates.IsLeadSurrogate(ch1) && i < rgch.Length)
				{
					var ch2 = rgch[i + 1];
					// if the following char is the other half then make the char from it
					if (Surrogates.IsTrailSurrogate(ch2))
					{
						ch = Surrogates.Int32FromSurrogates(ch1, ch2);
						++i;
					}
					else // otherwise it is half a surrogate pair (bad data)
					{
						ch = ch1;
					}
				}
				else
				{
					ch = ch1;
				}
				if (Icu.Character.IsAlphabetic(ch))
				{
					++cchOrtho;     // Seems not to include UCHAR_DIACRITIC.
				}
				else
				{
					if (Icu.Character.IsIdeographic(ch))
					{
						++cchOrtho;
					}
				}
			}
			return cchOrtho;
		}
		#endregion

		#region IPersistAsXml Members

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public void PersistAsXml(XElement element)
		{
			DynamicLoader.PersistObject(Finder, element, "finder");
			DynamicLoader.PersistObject(SubComparer, element, "comparer");
			if (SortedFromEnd)
			{
				XmlUtils.SetAttribute(element, "sortFromEnd", "true");
			}
			if (SortedByLength)
			{
				XmlUtils.SetAttribute(element, "sortByLength", "true");
			}
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public void InitXml(XElement element)
		{
			Finder = DynamicLoader.RestoreFromChild(element, "finder") as IStringFinder;
			SubComparer = DynamicLoader.RestoreFromChild(element, "comparer") as IComparer;
			SortedFromEnd = XmlUtils.GetOptionalBooleanAttributeValue(element, "sortFromEnd", false);
			SortedByLength = XmlUtils.GetOptionalBooleanAttributeValue(element, "sortByLength", false);
		}

		#endregion

		#region IStoresLcmCache members

		/// <summary>
		/// Given a cache, see whether your finder wants to know about it.
		/// </summary>
		public LcmCache Cache
		{
			set
			{
				if (Finder is IStoresLcmCache)
				{
					((IStoresLcmCache)Finder).Cache = value;
				}
				if (SubComparer is IStoresLcmCache)
				{
					((IStoresLcmCache)SubComparer).Cache = value;
				}
			}
		}
		#endregion

		#region ICloneable Members
		/// <summary>
		/// Clones the current object
		/// </summary>
		public object Clone()
		{
			return new StringFinderCompare
			{
				Finder = Finder,
				SortedByLength = SortedByLength,
				SortedFromEnd = SortedFromEnd,
				m_objToKey = m_objToKey.Clone() as Hashtable,
				SubComparer = SubComparer is ICloneable ? ((ICloneable)SubComparer).Clone() as IComparer : SubComparer,
				ComparisonNoter = ComparisonNoter
			};
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
			var that = (StringFinderCompare)obj;
			if (Finder == null)
			{
				if (that.Finder != null)
				{
					return false;
				}
			}
			else
			{
				if (that.Finder == null)
				{
					return false;
				}
				if (!Finder.SameFinder(that.Finder))
				{
					return false;
				}
			}
			if (SortedByLength != that.SortedByLength)
			{
				return false;
			}
			if (SortedFromEnd != that.SortedFromEnd)
			{
				return false;
			}
			if (m_objToKey == null)
			{
				if (that.m_objToKey != null)
				{
					return false;
				}
			}
			else
			{
				if (m_objToKey.Count != that.m_objToKey?.Count)
				{
					return false;
				}
				var ie = that.m_objToKey.GetEnumerator();
				while (ie.MoveNext())
				{
					if (!m_objToKey.ContainsKey(ie.Key) || m_objToKey[ie.Key] != ie.Value)
					{
						return false;
					}
				}
			}
			if (SubComparer == null)
			{
				return that.SubComparer == null;
			}
			return SubComparer.Equals(that.SubComparer);
		}

		/// <summary />
		public override int GetHashCode()
		{
			var hash = GetType().GetHashCode();
			if (Finder != null)
			{
				hash += Finder.GetHashCode();
			}
			if (SortedByLength)
			{
				hash *= 3;
			}
			if (SortedFromEnd)
			{
				hash *= 17;
			}
			if (m_objToKey != null)
			{
				hash += m_objToKey.Count * 53;
			}
			if (SubComparer != null)
			{
				hash += SubComparer.GetHashCode();
			}
			return hash;
		}
	}
}