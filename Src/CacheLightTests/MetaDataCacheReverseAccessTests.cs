// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.CacheLightTests
{
	/// <summary>
	/// Test the reverse access methods.
	/// </summary>
	[TestFixture]
	public class MetaDataCacheReverseAccessTests : MetaDataCacheBase
	{
		/// <summary>
		/// Tests GetClassId with a valid class name
		/// </summary>
		[Test]
		public void GetClassId_Valid()
		{
			var clid = m_metaDataCache.GetClassId("ClassD");
			Assert.AreEqual(2, clid, "Wrong class Id.");
		}

		/// <summary>
		/// Tests GetClassId with an invalid class name
		/// </summary>
		[Test]
		[ExpectedException(typeof(InvalidOperationException))]
		public void GetClassId_Invalid()
		{
			m_metaDataCache.GetClassId("NonExistentClassName");
		}

		/// <summary>
		/// Tests the GetFieldId method on a field that is directly on the named class
		/// </summary>
		[Test]
		public void GetFieldId_SansSuperClass()
		{
			var flid = m_metaDataCache.GetFieldId("ClassD", "MultiUnicodeProp12", false);
			Assert.AreEqual(2003, flid, "Wrong field Id.");
		}

		/// <summary>
		/// Tests the GetFieldId method on a field that is on a superclass
		/// </summary>
		[Test]
		public void GetFieldId_WithSuperClass()
		{
			var flid = m_metaDataCache.GetFieldId("ClassL2", "Whatever", true);
			Assert.AreEqual(35001, flid, "Wrong field Id.");
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly.
		/// </summary>
		[Test]
		public void GetFieldId_SansSuperClass_Nonexistent()
		{
			Assert.AreEqual(0, m_metaDataCache.GetFieldId("BaseClass", "Monkeyruski", false));
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetFieldId_WithSuperClass_Nonexistent()
		{
			var flid = m_metaDataCache.GetFieldId("ClassL2", "Flurskuiwert", true);
			Assert.AreEqual(0, flid, "Wrong field Id.");
		}

		/// <summary>
		/// Tests GetFieldId2 method on a class that has the requested field directly
		/// </summary>
		[Test]
		public void GetFieldId2_SansSuperClass()
		{
			var flid = m_metaDataCache.GetFieldId2(2, "MultiUnicodeProp12", false);
			Assert.AreEqual(2003, flid, "Wrong field Id.");
		}

		/// <summary>
		/// Tests GetFieldId2 method on a class whose superclass has the requested field
		/// </summary>
		[Test]
		public void GetFieldId2_WithSuperClass()
		{
			var flid = m_metaDataCache.GetFieldId2(45, "Whatever", true);
			Assert.AreEqual(35001, flid, "Wrong field Id.");
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly.
		/// </summary>
		[Test]
		public void GetFieldId2_SansSuperClass_Nonexistent()
		{
			Assert.AreEqual(0, m_metaDataCache.GetFieldId2(1, "MultiUnicodeProp12", false));
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetFieldId2_WithSuperClass_Nonexistent()
		{
			Assert.AreEqual(0, m_metaDataCache.GetFieldId2(45, "MultiUnicodeProp12", true));
		}

		/// <summary>
		/// Test the GetDirectSubclasses method for a class having no sublasses
		/// </summary>
		[Test]
		public void GetDirectSubclasses_None()
		{
			int countDirectSubclasses;
			using (var clids = MarshalEx.ArrayToNative<int>(10))
			{
				// Check ClassB.
				m_metaDataCache.GetDirectSubclasses(45, 10, out countDirectSubclasses, clids);
				Assert.AreEqual(0, countDirectSubclasses, "Wrong number of subclasses returned.");
			}
		}

		/// <summary>
		/// Test the GetDirectSubclasses method for a class having two subclasses
		/// </summary>
		[Test]
		public void GetDirectSubclasses()
		{
			using (var clids = MarshalEx.ArrayToNative<int>(10))
			{
				// Check ClassL (all of its direct subclasses).
				int countDirectSubclasses;
				m_metaDataCache.GetDirectSubclasses(35, 10, out countDirectSubclasses, clids);
				Assert.AreEqual(2, countDirectSubclasses, "Wrong number of subclasses returned.");
				var ids = MarshalEx.NativeToArray<int>(clids, 10);
				for (var i = 0; i < ids.Length; ++i)
				{
					var clid = ids[i];
					if (i < 2)
					{
						Assert.IsTrue(((clid == 28) || (clid == 45)), "Clid should be 28 or 49 for direct subclasses of ClassL.");
					}
					else
					{
						Assert.AreEqual(0, clid, "Clid should be 0 from here on.");
					}
				}
			}
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetDirectSubclasses_CountUnknown()
		{
			int countAllClasses;
			m_metaDataCache.GetDirectSubclasses(35, 0, out countAllClasses, null);
			Assert.AreEqual(2, countAllClasses, "Wrong number of subclasses returned.");
		}

		/// <summary>
		/// Tests getting the count of all subclasses of a class that has none. Count includes only the class itself.
		/// </summary>
		[Test]
		public void GetAllSubclasses_None()
		{
			using (var clids = MarshalEx.ArrayToNative<int>(10))
			{
				// Check ClassC.
				int countAllSubclasses;
				m_metaDataCache.GetAllSubclasses(26, 10, out countAllSubclasses, clids);
				Assert.AreEqual(1, countAllSubclasses, "Wrong number of subclasses returned.");
			}
		}

		/// <summary>
		/// Tests getting the count of all subclasses of a class (includes the class itself)
		/// </summary>
		[Test]
		public void GetAllSubclasses_ClassL()
		{
			using (var clids = MarshalEx.ArrayToNative<int>(10))
			{
				// Check ClassL (all of its direct subclasses).
				int countAllSubclasses;
				m_metaDataCache.GetAllSubclasses(35, 10, out countAllSubclasses, clids);
				Assert.AreEqual(3, countAllSubclasses, "Wrong number of subclasses returned.");
			}
		}

		/// <summary>
		/// Tests getting the count of all subclasses of a class (includes the class itself), limited
		/// by the maximum number requested
		/// </summary>
		[Test]
		public void GetAllSubclasses_ClassL_Limited()
		{
			using (var clids = MarshalEx.ArrayToNative<int>(2))
			{
				// Check ClassL (but get it and only 1 of its subclasses).
				int countAllSubclasses;
				m_metaDataCache.GetAllSubclasses(35, 2, out countAllSubclasses, clids);
				Assert.AreEqual(2, countAllSubclasses, "Wrong number of subclasses returned.");
			}
		}

		/// <summary>
		/// Tests getting the count of all subclasses of the base class (includes the class itself)
		/// </summary>
		[Test]
		public void GetAllSubclasses_BaseClass()
		{
			var countAllClasses = m_metaDataCache.ClassCount;
			using (var clids = MarshalEx.ArrayToNative<int>(countAllClasses))
			{
				// Check BaseClass.
				int countAllSubclasses;
				m_metaDataCache.GetAllSubclasses(0, countAllClasses, out countAllSubclasses, clids);
				Assert.AreEqual(countAllClasses, countAllSubclasses, "Wrong number of subclasses returned.");
			}
		}
	}
}