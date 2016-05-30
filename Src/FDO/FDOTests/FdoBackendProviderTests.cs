// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.Infrastructure.Impl;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Test framework for migration from version 7000068 to 7000069.
	/// </summary>
	[TestFixture]
	public class FdoBackendProviderTests
	{
		/// <summary>
		/// Test the CustomField usage note with multi unicode.
		/// </summary>
		[Test]
		public void CustomUsageNoteWithMultiUnicode()
		{
			var cfiList = new List<CustomFieldInfo>
			{
				new CustomFieldInfo { m_classname = "LexSense", m_fieldname = "UsageNote", m_fieldType = CellarPropertyType.MultiUnicode }
			};

			FDOBackendProvider.PreLoadCustomFields(cfiList);

			Assert.AreEqual(0, cfiList.Count, "Identical Custom Field should have been removed");
		}

		/// <summary>
		/// Test the CustomField usage note with multi string.
		/// </summary>
		[Test]
		public void CustomUsageNoteWithMultiString()
		{
			var cfiList = new List<CustomFieldInfo>
			{
				new CustomFieldInfo { m_classname = "LexSense", m_fieldname = "UsageNote", m_fieldType = CellarPropertyType.MultiString }
			};

			FDOBackendProvider.PreLoadCustomFields(cfiList);

			Assert.AreEqual(0, cfiList.Count, "Identical Custom Field should have been removed");
		}

		/// <summary>
		/// Test the CustomField usage note with integer.
		/// </summary>
		[Test]
		public void CustomUsageNoteWithInteger()
		{
			var cfiList = new List<CustomFieldInfo>
				{
					new CustomFieldInfo { m_classname = "LexSense", m_fieldname = "UsageNote", m_fieldType = CellarPropertyType.Integer }
				};

			FDOBackendProvider.PreLoadCustomFields(cfiList);

			Assert.AreEqual(1, cfiList.Count);
			Assert.AreEqual("LexSense", cfiList[0].m_classname);
			// uses first available number to rename old UsageNote, which in this case is UsageNote0.
			Assert.AreEqual("UsageNote0", cfiList[0].m_fieldname);
		}

		/// <summary>
		/// Test with multiple conflicting ExemplarN and UsageNoteN fields.
		/// </summary>
		[Test]
		public void MultipleConflictingCustomFields()
		{
			var cfiList = new List<CustomFieldInfo>
			{
				new CustomFieldInfo { m_classname = "LexSense", m_fieldname = "UsageNote" },
				new CustomFieldInfo { m_classname = "LexSense", m_fieldname = "UsageNote0" },
				new CustomFieldInfo { m_classname = "LexSense", m_fieldname = "UsageNote3" },
				new CustomFieldInfo { m_classname = "LexSense", m_fieldname = "Exemplar" },
				new CustomFieldInfo { m_classname = "LexSense", m_fieldname = "Exemplar0" },
				new CustomFieldInfo { m_classname = "LexSense", m_fieldname = "Exemplar1" }
			};

			FDOBackendProvider.PreLoadCustomFields(cfiList);

			Assert.AreEqual(6, cfiList.Count);
			foreach(var cfi in cfiList)
				Assert.AreEqual("LexSense", cfi.m_classname, "Classname should not have changed");

			// uses first available number to rename old UsageNote, which in this case is UsageNote1.
			Assert.AreEqual("UsageNote1", cfiList[0].m_fieldname);
			Assert.AreEqual("UsageNote0", cfiList[1].m_fieldname);
			Assert.AreEqual("UsageNote3", cfiList[2].m_fieldname);
			// uses first available number to rename old Exemplar, which in this case is Exemplar2.
			Assert.AreEqual("Exemplar2", cfiList[3].m_fieldname);
			Assert.AreEqual("Exemplar0", cfiList[4].m_fieldname);
			Assert.AreEqual("Exemplar1", cfiList[5].m_fieldname);
		}
	}
}
