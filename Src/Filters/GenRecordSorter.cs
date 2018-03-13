// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.Filters
{
	/// <summary>
	/// A very general record sorter class, based on an arbitrary implementation of IComparer
	/// that can compare two LCM objects.
	/// </summary>
	public class GenRecordSorter : RecordSorter
	{
		/// <summary>
		/// Normal constructor.
		/// </summary>
		public GenRecordSorter(IComparer comp): this()
		{
			Comparer = comp;
		}

		/// <summary>
		/// Default constructor for IPersistAsXml
		/// </summary>
		public GenRecordSorter()
		{
		}

		protected internal override IComparer getComparer()
		{
			return Comparer;
		}

		/// <summary>
		/// See whether the comparer can preload. Currently we only know about one kind that can.
		/// </summary>
		public override void Preload(object rootObj)
		{
			base.Preload(rootObj);
			if (Comparer is StringFinderCompare)
			{
				((StringFinderCompare)Comparer).Preload(rootObj);
			}
		}

		/// <summary>
		/// Override to pass it on to the comparer, if relevant.
		/// </summary>
		public override ISilDataAccess DataAccess
		{
			set
			{
				base.DataAccess = value;
				if (Comparer is StringFinderCompare)
				{
					((StringFinderCompare)Comparer).DataAccess = value;
				}
			}
		}

		/// <summary>
		/// Return true if the other sorter is 'compatible' with this, in the sense that
		/// either they produce the same sort sequence, or one derived from it (e.g., by reversing).
		/// </summary>
		public override bool CompatibleSorter(RecordSorter other)
		{
			var grsOther = other as GenRecordSorter;
			if (grsOther == null)
			{
				return false;
			}

			if (CompatibleComparers(Comparer, grsOther.Comparer))
			{
				return true;
			}
			// Currently the only other kind of compatibility we know how to detect
			// is StringFinderCompares that do more-or-less the same thing.
			var sfcThis = Comparer as StringFinderCompare;
			var sfcOther = grsOther.Comparer as StringFinderCompare;
			if (sfcThis == null || sfcOther == null)
			{
				return false;
			}
			if (!sfcThis.Finder.SameFinder(sfcOther.Finder))
			{
				return false;
			}
			// We deliberately don't care if one has a ReverseCompare and the other
			// doesn't. That's handled by a different icon.
			var subCompOther = UnpackReverseCompare(sfcOther);
			var subCompThis = UnpackReverseCompare(sfcThis);
			return CompatibleComparers(subCompThis, subCompOther);
		}

		private IComparer UnpackReverseCompare(StringFinderCompare sfc)
		{
			var subComp = sfc.SubComparer;
			if (subComp is ReverseComparer)
			{
				subComp = (subComp as ReverseComparer).SubComp;
			}
			return subComp;
		}

		/// <summary>
		/// Return true if the two comparers will give the same result. Ideally this would
		/// be an interface method on ICompare, but that interface is defined by .NET so we
		/// can't enhance it. This knows about a few interesting cases.
		/// </summary>
		private static bool CompatibleComparers(IComparer first, IComparer second)
		{
			// identity
			if (first == second)
			{
				return true;
			}

			// IcuComparers on same Ws?
			var firstIcu = first as IcuComparer;
			var secondIcu = second as IcuComparer;
			if (firstIcu != null && secondIcu != null && firstIcu.WsCode == secondIcu.WsCode)
			{
				return true;
			}

			// WritingSystemComparers on same Ws?
			var firstWs = first as WritingSystemComparer;
			var secondWs = second as WritingSystemComparer;
			if (firstWs != null && secondWs != null && firstWs.WsId == secondWs.WsId)
			{
				return true;
			}

			// Both IntStringComparers?
			if (first is IntStringComparer && second is IntStringComparer)
			{
				return true;
			}

			// LcmComparers on the same property?
			var firstLcm = first as LcmCompare;
			var secondLcm = second as LcmCompare;
			return firstLcm != null && secondLcm != null && firstLcm.PropertyName == secondLcm.PropertyName;
		}

		/// <summary>
		/// Gets or sets the comparer.
		/// </summary>
		public IComparer Comparer { get; set; }

		/// <summary>
		/// Add to the specified XML node information required to create a new
		/// record sorter equivalent to yourself.
		/// The default is not to add any information.
		/// It may be assumed that the node already contains the assembly and class
		/// information required to create an instance of the sorter in a default state.
		/// An equivalent XmlNode will be passed to the InitXml method of the
		/// re-created sorter.
		/// This default implementation does nothing.
		/// </summary>
		public override void PersistAsXml(XElement node)
		{
			base.PersistAsXml(node); // does nothing, but in case needed later...
			var persistComparer = Comparer as IPersistAsXml;
			if (persistComparer == null)
			{
				throw new Exception($"cannot persist GenRecSorter with comparer class {Comparer.GetType().AssemblyQualifiedName}");
			}
			DynamicLoader.PersistObject(persistComparer, node, "comparer");
		}

		/// <summary>
		/// Initialize an instance into the state indicated by the node, which was
		/// created by a call to PersistAsXml.
		/// </summary>
		public override void InitXml(XElement node)
		{
			base.InitXml (node);
			var compNode = node.Elements().First();
			if (compNode.Name != "comparer")
			{
				throw new Exception("persist info for GenRecordSorter must have comparer child element");
			}
			Comparer = DynamicLoader.RestoreObject(compNode) as IComparer;
			if (Comparer == null)
			{
				throw new Exception("restoring sorter failed...comparer does not implement IComparer");
			}
		}

		/// <summary>
		/// Set an LcmCache for anything that needs to know.
		/// </summary>
		public override LcmCache Cache
		{
			set
			{
				if (Comparer is IStoresLcmCache)
				{
					((IStoresLcmCache)Comparer).Cache = value;
				}
			}
		}

		/// <summary>
		/// Add to collector the ManyOnePathSortItems which this sorter derives from
		/// the specified object. This default method makes a single mopsi not involving any
		/// path.
		/// </summary>
		public override void CollectItems(int hvo, ArrayList collector)
		{
			if (Comparer is StringFinderCompare)
			{
				((StringFinderCompare)Comparer).CollectItems(hvo, collector);
			}
			else
				base.CollectItems(hvo, collector);
		}

		/// <summary>
		/// Sorts the specified records.
		/// </summary>
		public override void Sort(/*ref*/ ArrayList records)
		{
#if DEBUG
			var dt1 = DateTime.Now;
			var tc1 = Environment.TickCount;
#endif
			if (Comparer is StringFinderCompare)
			{
				((StringFinderCompare)Comparer).Init();
				((StringFinderCompare)Comparer).ComparisonNoter = this;
				m_comparisonsDone = 0;
				m_percentDone = 0;
				// Make sure at least 1 so we don't divide by zero.
				m_comparisonsEstimated = Math.Max(records.Count*(int)Math.Ceiling(Math.Log(records.Count, 2.0)), 1);
			}

			//records.Sort(m_comp);
			MergeSort.Sort(ref records, Comparer);

			if (Comparer is StringFinderCompare)
			{
				((StringFinderCompare)Comparer).Cleanup();
			}
#if DEBUG
			// only do this if the timing switch is info or verbose
			if (RuntimeSwitches.RecordTimingSwitch.TraceInfo)
			{
				var tc2 = Environment.TickCount;
				var ts1 = DateTime.Now - dt1;
				var s = $"GenRecordSorter:  Sorting {records.Count} records took {tc2 - tc1} ticks, or {ts1.Minutes}:{ts1.Seconds}.{ts1.Milliseconds:d3} min:sec.";
				Debug.WriteLine(s, RuntimeSwitches.RecordTimingSwitch.DisplayName);
			}
#endif
		}
		/// <summary>
		/// Required implementation.
		/// </summary>
		public override void MergeInto(ArrayList records, ArrayList newRecords)
		{
			if (Comparer is StringFinderCompare)
			{
				((StringFinderCompare)Comparer).Init();
			}

			MergeInto(records, newRecords, Comparer);

			if (Comparer is StringFinderCompare)
			{
				((StringFinderCompare)Comparer).Cleanup();
			}
		}

		/// <summary>
		/// Check whether this GenRecordSorter is equal to another object.
		/// </summary>
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
			var that = (GenRecordSorter)obj;
			if (Comparer == null)
			{
				if (that.Comparer != null)
				{
					return false;
				}
			}
			else
			{
				if (that.Comparer == null)
				{
					return false;
				}
				// TODO-Linux: System.Boolean System.Type::op_Inequality(System.Type,System.Type)
				// is marked with [MonoTODO] and might not work as expected in 4.0.
				return Comparer.GetType() == that.Comparer.GetType() && Comparer.Equals(that.Comparer);
			}
			return true;
		}

		/// <summary />
		public override int GetHashCode()
		{
			var hash = GetType().GetHashCode();
			if (Comparer != null)
			{
				hash *= Comparer.GetHashCode();
			}
			return hash;
		}
	}
}