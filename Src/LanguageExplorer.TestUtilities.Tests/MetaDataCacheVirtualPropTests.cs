// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using SIL.LCModel.Core.Cellar;

namespace LanguageExplorer.TestUtilities.Tests
{
	/// <summary>
	/// Test the reverse access methods.
	/// </summary>
	[TestFixture]
	public class MetaDataCacheVirtualPropTests : MetaDataCacheBase
	{
		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void AddVirtualPropTest()
		{
			const int flid = 2000000001;
			const CellarPropertyType type = CellarPropertyType.Image;
			const string className = "ClassB";
			const string fieldName = "NewImageVP";
			m_metaDataCache.AddVirtualProp(className, fieldName, flid, (int)type);
			// Check its flid.
			Assert.AreEqual(flid, m_metaDataCache.GetFieldId(className, fieldName, false), "Wrong field Id.");
			// Check its data type.
			Assert.AreEqual(type, (CellarPropertyType)m_metaDataCache.GetFieldType(flid), "Wrong field type.");
			// Check to see it is virtual.
			Assert.IsTrue(m_metaDataCache.get_IsVirtual(flid), "Wrong field virtual setting.");
			// Check the clid it was supposed to be placed in.
			Assert.AreEqual(m_metaDataCache.GetClassId(className), m_metaDataCache.GetOwnClsId(flid), "Wrong clid for new virtual field.");
			Assert.AreEqual(fieldName, m_metaDataCache.GetFieldName(flid), "Wrong field name for new virtual field.");
		}

		/// <summary>
		/// Check to see if some existing field is virtual.
		/// (It should not be.)
		/// </summary>
		[Test]
		public void get_IsVirtualTest()
		{
			Assert.IsFalse(m_metaDataCache.get_IsVirtual(1001), "Wrong field virtual setting.");
		}

		/// <summary>
		/// Check for case where the specified class for the new virtual field doesn't exist.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void AddVirtualPropNoClassTest()
		{
			m_metaDataCache.AddVirtualProp("BogusClass", "NewImageVP", 2000000002, (int)CellarPropertyType.Image);
		}

		/// <summary>
		/// Check for case where the specified field name for the new virtual field exists.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void AddVirtualPropFieldExistsTest()
		{
			m_metaDataCache.AddVirtualProp("ClassK", "MultiStringProp11", 2000000003, (int)CellarPropertyType.Image);
		}

		/// <summary>
		/// Check for case where the specified flid for the new virtual field exists.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void AddVirtualPropFlidExistsTest()
		{
			m_metaDataCache.AddVirtualProp("ClassB", "NewName", m_metaDataCache.GetFieldId("ClassB", "WhoCares", true), (int)CellarPropertyType.Image);
		}

		/// <summary>
		/// Check for case where the specified flid for the new virtual field exists.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void AddVirtualPropInvalidLowFieldTypeTest()
		{
			m_metaDataCache.AddVirtualProp("ClassB", "NewName", 2000000004, 0);
		}

		/// <summary>
		/// Check for case where the specified flid for the new virtual field exists.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void AddVirtualPropInvalidHighFieldTypeTest()
		{
			m_metaDataCache.AddVirtualProp("ClassB", "NewName", 2000000005, 1000);
		}
	}
}