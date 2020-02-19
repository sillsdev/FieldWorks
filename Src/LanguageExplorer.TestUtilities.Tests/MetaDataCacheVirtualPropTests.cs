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
			var newFlid = m_metaDataCache.GetFieldId(className, fieldName, false);
			Assert.AreEqual(flid, newFlid, "Wrong field Id.");
			// Check its data type.
			Assert.AreEqual(type, (CellarPropertyType)m_metaDataCache.GetFieldType(flid), "Wrong field type.");
			// Check to see it is virtual.
			var isVirtual = m_metaDataCache.get_IsVirtual(flid);
			Assert.IsTrue(isVirtual, "Wrong field virtual setting.");
			// Check the clid it was supposed to be placed in.
			var clid = m_metaDataCache.GetClassId(className);
			Assert.AreEqual(clid, m_metaDataCache.GetOwnClsId(flid), "Wrong clid for new virtual field.");
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
			const int flid = 2000000002;
			const int type = (int)CellarPropertyType.Image;
			const string className = "BogusClass";
			const string fieldName = "NewImageVP";
			m_metaDataCache.AddVirtualProp(className, fieldName, flid, type);
		}

		/// <summary>
		/// Check for case where the specified field name for the new virtual field exists.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void AddVirtualPropFieldExistsTest()
		{
			const int flid = 2000000003;
			const int type = (int)CellarPropertyType.Image;
			const string className = "ClassK";
			const string fieldName = "MultiStringProp11";
			m_metaDataCache.AddVirtualProp(className, fieldName, flid, type);
		}

		/// <summary>
		/// Check for case where the specified flid for the new virtual field exists.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void AddVirtualPropFlidExistsTest()
		{
			var flid = m_metaDataCache.GetFieldId("ClassB", "WhoCares", true);
			const int type = (int)CellarPropertyType.Image;
			const string className = "ClassB";
			const string fieldName = "NewName";
			m_metaDataCache.AddVirtualProp(className, fieldName, flid, type);
		}

		/// <summary>
		/// Check for case where the specified flid for the new virtual field exists.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void AddVirtualPropInvalidLowFieldTypeTest()
		{
			const int flid = 2000000004;
			const int type = 0;
			const string className = "ClassB";
			const string fieldName = "NewName";
			m_metaDataCache.AddVirtualProp(className, fieldName, flid, type);
		}

		/// <summary>
		/// Check for case where the specified flid for the new virtual field exists.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void AddVirtualPropInvalidHighFieldTypeTest()
		{
			const int flid = 2000000005;
			const int type = 1000;
			const string className = "ClassB";
			const string fieldName = "NewName";
			m_metaDataCache.AddVirtualProp(className, fieldName, flid, type);
		}
	}
}