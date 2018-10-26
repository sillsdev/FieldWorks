// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.CacheLightTests
{
	/// <summary>
	/// Test the field access methods.
	/// </summary>
	[TestFixture]
	public class MetaDataCacheFieldAccessTests : MetaDataCacheBase
	{
		/// <summary>
		/// Test of GetDstClsName method
		/// </summary>
		[Test]
		public void GetDstClsNameTest()
		{
			Assert.AreEqual("ClassL", m_metaDataCache.GetDstClsName(59005), "Wrong class name");
		}

		/// <summary>
		/// Test of GetOwnClsName method
		/// </summary>
		[Test]
		public void GetOwnClsNameTest()
		{
			Assert.AreEqual("ClassG", m_metaDataCache.GetOwnClsName(15068), "Wrong class name");
		}

		/// <summary>
		/// Test of GetOwnClsId method
		/// </summary>
		[Test]
		public void GetOwnClsIdTest()
		{
			Assert.AreEqual(15, m_metaDataCache.GetOwnClsId(15068), "Wrong class implementor.");
		}

		/// <summary>
		/// Test of GetDstClsId method
		/// </summary>
		[Test]
		public void GetDstClsIdTest()
		{
			Assert.AreEqual(49, m_metaDataCache.GetDstClsId(59003), "Wrong class Signature.");
		}

		/// <summary>
		/// This should test for any case where the given flid is not valid.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void GetClsNameForBadFlidTest()
		{
			m_metaDataCache.GetOwnClsName(50);
		}

		/// <summary />
		[Test]
		public void GetFieldIdsTest()
		{
			var flidSize = m_metaDataCache.FieldCount;

			int[] ids;
			var testFlidSize = flidSize - 1;
			using (var flids = MarshalEx.ArrayToNative<int>(testFlidSize))
			{
				m_metaDataCache.GetFieldIds(testFlidSize, flids);
				ids = MarshalEx.NativeToArray<int>(flids, testFlidSize);
				Assert.AreEqual(testFlidSize, ids.Length, "Wrong size of fields returned.");
				foreach (var flid in ids)
				{
					Assert.IsTrue(flid > 0, "Wrong flid value: " + flid);
				}
			}
			testFlidSize = flidSize;
			using (var flids = MarshalEx.ArrayToNative<int>(testFlidSize))
			{
				m_metaDataCache.GetFieldIds(testFlidSize, flids);
				ids = MarshalEx.NativeToArray<int>(flids, testFlidSize);
				Assert.AreEqual(testFlidSize, ids.Length, "Wrong size of fields returned.");
				foreach (var flid in ids)
				{
					Assert.IsTrue(flid > 0, "Wrong flid value: " + flid);
				}
			}
			testFlidSize = flidSize + 1;
			using (var flids = MarshalEx.ArrayToNative<int>(testFlidSize))
			{
				m_metaDataCache.GetFieldIds(testFlidSize, flids);
				ids = MarshalEx.NativeToArray<int>(flids, testFlidSize);
				Assert.AreEqual(testFlidSize, ids.Length, "Wrong size of fields returned.");
				for (var iflid = 0; iflid < ids.Length; ++iflid)
				{
					var flid = ids[iflid];
					if (iflid < ids.Length - 1)
					{
						Assert.IsTrue(flid > 0, "Wrong flid value: " + flid);
					}
					else
					{
						Assert.AreEqual(0, flid, "Wrong value for flid beyond actual length.");
					}
				}
			}
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetFieldNameTest()
		{
			Assert.AreEqual("MultiUnicodeProp12", m_metaDataCache.GetFieldName(2003));
		}

		/// <summary>
		/// CacheLight doesn't support field labels, so they are always null
		/// </summary>
		[Test]
		public void GetFieldLabelIsNullTest()
		{
			Assert.IsNull(m_metaDataCache.GetFieldLabel(59003), "Field label not null.");
		}

		/// <summary>
		/// CacheLight doesn't support field help, so it is always null
		/// </summary>
		[Test]
		public void GetFieldHelpIsNullTest()
		{
			Assert.IsNull(m_metaDataCache.GetFieldHelp(59003), "Field help not null.");
		}

		/// <summary>
		/// CacheLight doesn't support field XML, so it is always null
		/// </summary>
		[Test]
		public void GetFieldXmlIsNullTest()
		{
			Assert.IsNull(m_metaDataCache.GetFieldXml(59003), "Field XML not null.");
		}

		/// <summary>
		/// CacheLight doesn't support writing system selectors, so they are always 0
		/// </summary>
		[Test]
		public void GetFieldWsIsZeroTest()
		{
			Assert.AreEqual(0, m_metaDataCache.GetFieldWs(59003), "Writing system not zero.");
		}

		/// <summary>
		/// Check for all the types used in the model.
		/// </summary>
		/// <remarks>
		/// See the Same named method in the NoRealDataForTests for data types not used in the model.
		/// </remarks>
		[Test]
		public void GetFieldTypeTest()
		{
			Assert.AreEqual(CellarPropertyType.Boolean, (CellarPropertyType)m_metaDataCache.GetFieldType(2027), "Wrong field data type for Boolean data.");
			Assert.AreEqual(CellarPropertyType.Integer, (CellarPropertyType)m_metaDataCache.GetFieldType(26002), "Wrong field data type for Integer data.");
			Assert.AreEqual(CellarPropertyType.Time, (CellarPropertyType)m_metaDataCache.GetFieldType(2005), "Wrong field data type for Time data.");
			Assert.AreEqual(CellarPropertyType.Guid, (CellarPropertyType)m_metaDataCache.GetFieldType(8002), "Wrong field data type for Guid data.");
			Assert.AreEqual(CellarPropertyType.GenDate, (CellarPropertyType)m_metaDataCache.GetFieldType(13004), "Wrong field data type for GenDate data.");
			Assert.AreEqual(CellarPropertyType.Binary, (CellarPropertyType)m_metaDataCache.GetFieldType(15002), "Wrong field data type for Binary data.");
			Assert.AreEqual(CellarPropertyType.String, (CellarPropertyType)m_metaDataCache.GetFieldType(97008), "Wrong field data type for String data.");
			Assert.AreEqual(CellarPropertyType.MultiString, (CellarPropertyType)m_metaDataCache.GetFieldType(97021), "Wrong field data type for MultiString data.");
			Assert.AreEqual(CellarPropertyType.Unicode, (CellarPropertyType)m_metaDataCache.GetFieldType(1001), "Wrong field data type for Unicode data.");
			Assert.AreEqual(CellarPropertyType.MultiUnicode, (CellarPropertyType)m_metaDataCache.GetFieldType(7001), "Wrong field data type for MultiUnicode data.");
		}

		/// <summary>
		/// Check for validity of adding the given clid to some field.
		/// </summary>
		[Test]
		public void get_IsValidClassTest()
		{
			// Exact match
			var isValid = m_metaDataCache.get_IsValidClass(59004, 0);
			Assert.IsTrue(isValid, "Object of type BaseClass should be able to be assigned to a field whose signature is BaseClass");

			// Prevent use of base class when specific subclass is expected
			isValid = m_metaDataCache.get_IsValidClass(59003, 0);
			Assert.IsFalse(isValid, "Object of type BaseClass should NOT be able to be assigned to a field whose signature is ClassB");

			// Mismatch
			isValid = m_metaDataCache.get_IsValidClass(59003, 45);
			Assert.IsFalse(isValid, "Object of type ClassL2 should NOT be able to be assigned to a field whose signature is ClassB");

			// Allow subclass when base class is expected
			isValid = m_metaDataCache.get_IsValidClass(59005, 45);
			Assert.IsTrue(isValid, "Object of type ClassL2 should be able to be assigned to a field whose signature is ClassL");

			// Prevent assignment of object to field that is expecting a basic type
			isValid = m_metaDataCache.get_IsValidClass(28002, 97);
			Assert.IsFalse(isValid, "Can put a ClassJ into a basic (Unicode) field?");
		}

		/// <summary>
		/// Check for validity of adding the given clid to an illegal field of 0.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void get_IsValidClassBadTest()
		{
			m_metaDataCache.get_IsValidClass(0, 0);
		}
	}
}