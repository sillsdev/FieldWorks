// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using System.Xml.Linq;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.Xml;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// This class implements StringFinder in a way appropriate for a cell that shows a sequence
	/// of strings derived from a multistring property of objects at the leaves of a tree.
	/// A sequence of flids specifies the (currently sequence) properties to follow to reach the
	/// leaves. Leaf objects are found from the root by taking retrieving property flidVec[0]
	/// of the root object, then flidVec[1] of the resulting object, and so forth.
	/// </summary>
	public class MultiIndirectMlPropFinder : StringFinderBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:MultiIndirectMlPropFinder"/> class.
		/// </summary>
		public MultiIndirectMlPropFinder(ISilDataAccess sda, int[] flidVec, int flidString, int ws)
			: base(sda)
		{
			VecFlids = flidVec;
			FlidString = flidString;
			Ws = ws;
		}

		/// <summary>
		/// For use with IPersistAsXml
		/// </summary>
		public MultiIndirectMlPropFinder()
		{
		}

		/// <summary>
		/// Gets the vec flids.
		/// </summary>
		public int[] VecFlids { get; private set; }

		/// <summary>
		/// Gets the flid string.
		/// </summary>
		public int FlidString { get; private set; }

		/// <summary>
		/// Gets the ws.
		/// </summary>
		public int Ws { get; private set; }

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement node)
		{
			base.PersistAsXml(node);
			XmlUtils.SetAttribute(node, "flidVec", XmlUtils.MakeIntegerListValue(VecFlids));
			XmlUtils.SetAttribute(node, "flidString", FlidString.ToString());
			XmlUtils.SetAttribute(node, "ws", Ws.ToString());
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public override void InitXml(XElement node)
		{
			base.InitXml(node);
			VecFlids = XmlUtils.GetMandatoryIntegerListAttributeValue(node, "flidVec");
			FlidString = XmlUtils.GetMandatoryIntegerAttributeValue(node, "flidString");
			Ws = XmlUtils.GetMandatoryIntegerAttributeValue(node, "ws");
		}

		// Return the number of values in the tree rooted at hvo, where index (into m_flidVec)
		// gives the index of the property which should be followed from that object.
		private int CountItems(int hvo, int index)
		{
			var count = DataAccess.get_VecSize(hvo, VecFlids[index]);
			if (index == VecFlids.Length - 1)
			{
				return count;
			}
			var total = 0;
			for (var i = 0; i < count; ++i)
			{
				total += CountItems(DataAccess.get_VecItem(hvo, VecFlids[index], i), index + 1);
			}
			return total;
		}

		/// <summary>
		/// Insert into results, starting at resIndex, the strings obtained from the
		/// tree rooted at hvo. flidVec[flidIndex] is the property to follow from hvo.
		/// </summary>
		private void GetItems(int hvo, int flidIndex, string[] results, ref int resIndex)
		{
			if (flidIndex == VecFlids.Length)
			{
				// add the string for this leaf object
				results[resIndex] = DataAccess.get_MultiStringAlt(hvo, FlidString, Ws).Text ?? string.Empty;
				resIndex++;
			}
			else
			{
				var count = DataAccess.get_VecSize(hvo, VecFlids[flidIndex]);
				for (var i = 0; i < count; ++i)
				{
					GetItems(DataAccess.get_VecItem(hvo, VecFlids[flidIndex], i), flidIndex + 1, results, ref resIndex);
				}
			}
		}

		#region StringFinder Members

		/// <summary>
		/// Stringses the specified hvo.
		/// </summary>
		public override string[] Strings(int hvo)
		{
			var result = new string[CountItems(hvo, 0)];
			var resIndex = 0;
			GetItems(hvo, 0, result, ref resIndex);
			return result;
		}

		private static bool SameVec(int[] first, int[] second)
		{
			if (first.Length != second.Length)
			{
				return false;
			}

			return !first.Where((t, i) => t != second[i]).Any();
		}

		/// <summary>
		/// Same if it is the same type for the same flid and DA, etc.
		/// </summary>
		public override bool SameFinder(IStringFinder other)
		{
			var other2 = other as MultiIndirectMlPropFinder;
			if (other2 == null)
			{
				return false;
			}
			return SameVec(other2.VecFlids, VecFlids) && other2.DataAccess == DataAccess && other2.FlidString == FlidString && other2.Ws == Ws;
		}
		#endregion
	}
}