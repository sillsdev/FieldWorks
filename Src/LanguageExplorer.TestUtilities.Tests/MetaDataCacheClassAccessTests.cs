// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Utils;

namespace LanguageExplorer.TestUtilities.Tests
{
	/// <summary>
	/// Test the class access methods.
	/// </summary>
	[TestFixture]
	public class MetaDataCacheClassAccessTests : MetaDataCacheBase
	{
		/// <summary>
		/// Check for finding the class name based on the given clid.
		/// </summary>
		[Test]
		public void GetClassNameTest()
		{
			Assert.AreEqual("ClassB", m_metaDataCache.GetClassName(49), "Wrong class name for ClassB.");
		}

		/// <summary>
		/// Check for finding the class name based on the given clid.
		/// </summary>
		[Test]
		public void GetAbstractTest()
		{
			Assert.IsFalse(m_metaDataCache.GetAbstract(49), "ClassB is a concrete class.");
			Assert.IsTrue(m_metaDataCache.GetAbstract(0), "BaseClass is an abstract class.");
		}

		/// <summary>
		/// Check for finding the class name based on the given clid.
		/// </summary>
		[Test]
		public void GetBaseClsIdTest()
		{
			Assert.AreEqual(7, m_metaDataCache.GetBaseClsId(49), "Wrong base class id for ClassB.");
		}

		/// <summary>
		/// Check for finding the class name based on the given clid.
		/// </summary>
		[Test]
		[ExpectedException(typeof(InvalidOperationException))]
		public void GetBaseClsIdBadTest()
		{
			m_metaDataCache.GetBaseClsId(0);
		}

		/// <summary>
		/// Check for finding the class name based on the given clid.
		/// </summary>
		[Test]
		public void GetBaseClsNameTest()
		{
			Assert.AreEqual("ClassK", m_metaDataCache.GetBaseClsName(49), "Wrong base class id for ClassB.");
		}

		/// <summary>
		/// Check for finding the class name based on the given clid.
		/// </summary>
		[Test]
		[ExpectedException(typeof(InvalidOperationException))]
		public void GetBaseClsNameBadTest()
		{
			m_metaDataCache.GetBaseClsName(0);
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetClassIdsTest()
		{
			// Just use the count for all classes,
			// even though we know it will never be that high a number that can be returned.
			int[] ids;
			var countAllClasses = m_metaDataCache.ClassCount;
			using (var clids = MarshalEx.ArrayToNative<int>(countAllClasses))
			{
				m_metaDataCache.GetClassIds(countAllClasses, clids);
				ids = MarshalEx.NativeToArray<int>(clids, countAllClasses);
				Assert.AreEqual(countAllClasses, ids.Length, "Wrong number of classes returned.");
			}
			countAllClasses = 2;
			using (var clids = MarshalEx.ArrayToNative<int>(countAllClasses))
			{
				// Check ClassL (all of its direct subclasses).
				m_metaDataCache.GetClassIds(countAllClasses, clids);
				ids = MarshalEx.NativeToArray<int>(clids, 2);
				Assert.AreEqual(countAllClasses, ids.Length, "Wrong number of classes returned.");
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void GetFieldsTest()
		{
			int countAllFlidsOut;
			using (var flids = MarshalEx.ArrayToNative<int>(500))
			{
				countAllFlidsOut = m_metaDataCache.GetFields(0, true, (int)CellarPropertyTypeFilter.All, 0, flids);
				var countAllFlids = countAllFlidsOut;
				countAllFlidsOut = m_metaDataCache.GetFields(0, true, (int)CellarPropertyTypeFilter.All, countAllFlidsOut, flids);
				Assert.AreEqual(countAllFlids, countAllFlidsOut, "Wrong number of fields returned for BaseClass.");
			}
			using (var flids = MarshalEx.ArrayToNative<int>(500))
			{
				countAllFlidsOut = m_metaDataCache.GetFields(49, true, (int)CellarPropertyTypeFilter.All, 0, flids);
				countAllFlidsOut = m_metaDataCache.GetFields(49, true, (int)CellarPropertyTypeFilter.All, countAllFlidsOut, flids);
				Assert.AreEqual(8, countAllFlidsOut, "Wrong number of fields returned for 49.");
			}
			using (var flids = MarshalEx.ArrayToNative<int>(500))
			{
				countAllFlidsOut = m_metaDataCache.GetFields(49, true, (int)CellarPropertyTypeFilter.AllReference, 0, flids);
				countAllFlidsOut = m_metaDataCache.GetFields(49, true, (int)CellarPropertyTypeFilter.AllReference, countAllFlidsOut, flids);
				Assert.AreEqual(1, countAllFlidsOut, "Wrong number of fields returned for 49.");
			}
		}

		/// <summary />
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void GetFieldsBadTest()
		{
			using (var flids = MarshalEx.ArrayToNative<int>(500))
			{
				const int countAllFlidsOut = 1;
				m_metaDataCache.GetFields(49, true, (int)CellarPropertyTypeFilter.All, countAllFlidsOut, flids);
			}
		}
	}
}