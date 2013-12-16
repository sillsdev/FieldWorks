// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ParaNodeMapTests.cs
// Responsibility: TE Team

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using NUnit.Framework;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the ParaMapNode
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ParaNodeMapTests: SIL.FieldWorks.Test.TestUtils.BaseTest
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CompareTo method in the ParaNodeMap to ensure that they compare accurately
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParaNodeMap_AccurateComparison()
		{
			// Artificially create a few ParaNodeMaps
			ParaNodeMap map1 = new DummyParaNodeMap(0, 0, 0, 0, 0);
			ParaNodeMap map2 = new DummyParaNodeMap(0, 0, 0, 1, 2);
			ParaNodeMap map3 = new DummyParaNodeMap(0, 0, 0, 11, 0);
			ParaNodeMap map4 = new DummyParaNodeMap(1, 0, 0, 0, 0);

			// Now run through our checks:
			// We expect map1 to come before map2
			Assert.AreEqual(-1, map1.CompareTo(map2));
			// Likewise, we expect map2 to come after map1
			Assert.AreEqual(1, map2.CompareTo(map1));
			// Map2 should be less than map3
			Assert.AreEqual(-1, map2.CompareTo(map3));

			// If we create a list and add maps 1-4, we should expect
			// (after sorting), to find them in that same order.  We'll
			// insert them out of order just to be sure it works
			List<ParaNodeMap> toSort = new List<ParaNodeMap>();
			toSort.Add(map3);
			toSort.Add(map2);
			toSort.Add(map4);
			toSort.Add(map1);
			toSort.Sort();
			// Now verify the list is in the expected order
			Assert.IsTrue(map1.Equals(toSort[0]));
			Assert.IsTrue(map2.Equals(toSort[1]));
			Assert.IsTrue(map3.Equals(toSort[2]));
			Assert.IsTrue(map4.Equals(toSort[3]));

			// Finally, create another map, identical to map3, and verify
			// that when the two objects are equal, comparison also returns 0
			ParaNodeMap map5 = new DummyParaNodeMap(0, 0, 0, 11, 0);
			Assert.IsTrue(map3.Equals(map5));
			Assert.AreEqual(0, map3.CompareTo(map5));
		}
	}

	#region DummyParaNodeMap Class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A dummy class used be this testing module to artificially create a
	/// ParaNodeMap, so that it's comparison method can be tested fully
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	class DummyParaNodeMap : ParaNodeMap
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// constructor for testing
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyParaNodeMap(int bookIndex, int bookFlid, int sectionIndex,
			int sectionFlid, int paraIndex)
		{
			// Just set the data as recieved
			m_location[kBookIndex] = bookIndex;
			m_location[kBookFlidIndex] = bookFlid;
			m_location[kSectionIndex] = sectionIndex;
			m_location[kSectionFlidIndex] = sectionFlid;
			m_location[kParaIndex] = paraIndex;
		}
	}
	#endregion
}
