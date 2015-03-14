// Copyright (c) 2003-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.FDO.CoreTests.MetaDataCacheTests
{
	#region MDC base class

	/// <summary>
	/// Base class for all MDC tests. (I store the MDC in a data member for ease of access.)
	/// </summary>
	public class FieldTestBase : MemoryOnlyBackendProviderTestBase
	{
		/// <summary>
		/// The MDC.
		/// </summary>
		protected IFwMetaDataCacheManaged m_mdc;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a test overrides this, it should call this base implementation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			m_mdc = (IFwMetaDataCacheManaged)Cache.DomainDataByFlid.MetaDataCache;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a test overrides this, it should call this base implementation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureTeardown()
		{
			m_mdc = null;

			base.FixtureTeardown();
		}
	}

	#endregion MDC base class

	#region MetaDataCacheFieldAccessTests class

	/// <summary>
	/// Test the field access methods.
	/// </summary>
	[TestFixture]
	public class FieldAccessTests : FieldTestBase
	{
		/// <summary>
		/// Check to see if some existing field is virtual.
		/// (It should not be.)
		/// </summary>
		[Test]
		public void get_IsVirtualTest()
		{
			Assert.IsFalse(m_mdc.get_IsVirtual(101), "Wrong field virtual setting.");
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		public void GetDstClsNameTest()
		{
			var className = m_mdc.GetDstClsName(5002011);
			Assert.AreEqual("LexSense", className, "Wrong class name");
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		public void GetOwnClsNameTest()
		{
			var className = m_mdc.GetOwnClsName(5002001); // HomographNumber
			Assert.AreEqual("LexEntry", className, "Wrong class name");
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		public void GetOwnClsIdTest()
		{
			var implementingClid = m_mdc.GetOwnClsId(5002001); // HomographNumber
			Assert.AreEqual(5002, implementingClid, "Wrong class implementor.");
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		public void GetDstClsIdTest()
		{
			var destinationClid = m_mdc.GetDstClsId(5002009);
			Assert.AreEqual(5041, destinationClid, "Wrong class Signature.");
		}

		/// <summary>
		/// This should test for any case where the given flid is not valid.
		/// </summary>
		[Test]
		[ExpectedException(typeof(FDOInvalidFieldException))]
		public void GetClsNameForBadFlidTest()
		{
			m_mdc.GetOwnClsName(50);
		}

		/// <summary>
		/// This should crash where the given flid does not exist.
		/// </summary>
		[Test]
		[ExpectedException(typeof(FDOInvalidFieldException))]
		public void GetNonExistingFlidTest()
		{
			m_mdc.GetFieldId("WfiWordform", "Certified", false);
		}

		/// <summary>
		/// Tests GetFieldIds method.
		/// </summary>
		[Test]
		public void GetFieldIdsTest()
		{
			var flidSize = m_mdc.FieldCount;

			int[] uIds;
			var testFlidSize = flidSize - 1;

			using (var flidsPtr = MarshalEx.ArrayToNative<int>(testFlidSize))
			{
				m_mdc.GetFieldIds(testFlidSize, flidsPtr);
				uIds = MarshalEx.NativeToArray<int>(flidsPtr, testFlidSize);
				Assert.AreEqual(testFlidSize, uIds.Length, "Wrong size of fields returned.");
				foreach (var flid in uIds)
					Assert.IsTrue(flid > 0, "Wrong flid value: " + flid);
			}

			testFlidSize = flidSize;
			using (var flidsPtr = MarshalEx.ArrayToNative<int>(testFlidSize))
			{
				m_mdc.GetFieldIds(testFlidSize, flidsPtr);
				uIds = MarshalEx.NativeToArray<int>(flidsPtr, testFlidSize);
				var uIdsNonCOM = m_mdc.GetFieldIds();
				Assert.AreEqual(uIds.Length, uIdsNonCOM.Length, "COM non-COM GetFieldIds different sizes.");
				for (var i = 0; i < uIdsNonCOM.Length; ++i)
					Assert.AreEqual(uIdsNonCOM[i], uIds[i], "");
				Assert.AreEqual(testFlidSize, uIds.Length, "Wrong size of fields returned.");
				foreach (var flid in uIds)
					Assert.IsTrue(flid > 0, "Wrong flid value: " + flid);
			}


			testFlidSize = flidSize + 1;
			using (var flidsPtr = MarshalEx.ArrayToNative<int>(testFlidSize))
			{
				m_mdc.GetFieldIds(testFlidSize, flidsPtr);
				uIds = MarshalEx.NativeToArray<int>(flidsPtr, testFlidSize);
				Assert.AreEqual(testFlidSize, uIds.Length, "Wrong size of fields returned.");
				for (var iflid = 0; iflid < uIds.Length; ++iflid)
				{
					var flid = uIds[iflid];
					if (iflid < uIds.Length - 1)
						Assert.IsTrue(flid > 0, "Wrong flid value: " + flid);
					else
						Assert.AreEqual(0, flid, "Wrong value for flid beyond actual length.");
				}
			}
		}

		/// <summary>
		/// Test method that retrieves fields with given destination class.
		/// </summary>
		[Test]
		public void GetIncomingFieldsTest()
		{
			var flids = new HashSet<int>(m_mdc.GetIncomingFields(CmPersonTags.kClassId, (int)CellarPropertyTypeFilter.All));
			Assert.IsTrue(flids.Contains(CmPossibilityTags.kflidResearchers), "found ref collection on exact class");
			Assert.IsTrue(flids.Contains(StJournalTextTags.kflidCreatedBy), "found ref atomic on exact class");
			Assert.IsTrue(flids.Contains(ScrScriptureNoteTags.kflidCategories), "found ref seq on base class");
			Assert.IsTrue(flids.Contains(CmPossibilityListTags.kflidPossibilities), "found owning seq on base class");
			Assert.IsTrue(flids.Contains(CmPersonTags.kflidPositions), "found ref collection on base class");
			Assert.IsFalse(flids.Contains(LexEntryTags.kflidSenses), "should not find unrelated prop");
			Assert.IsFalse(flids.Contains(CmPersonTags.kflidPlaceOfBirth ), "should not prop of source class referring to other subclass of base");

			flids = new HashSet<int>(m_mdc.GetIncomingFields(StTextTags.kClassId, (int)CellarPropertyTypeFilter.All));
			Assert.IsTrue(flids.Contains(TextTags.kflidContents), "found owning atomic on exact class");
			Assert.IsTrue(flids.Contains(ScrSectionTags.kflidHeading), "found owning seq on exact class");

			flids = new HashSet<int>(m_mdc.GetIncomingFields(MoMorphSynAnalysisTags.kClassId, (int)CellarPropertyTypeFilter.All));
			Assert.IsTrue(flids.Contains(LexEntryTags.kflidMorphoSyntaxAnalyses), "found owning collection on exact class");
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetFieldNameTest()
		{
			var fieldName = m_mdc.GetFieldName(5002003);
			Assert.AreEqual(fieldName, "CitationForm", "Wrong field name.");
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetFieldLabelIsNullTest()
		{
			var fieldLabel = m_mdc.GetFieldLabel(5002003);
			Assert.IsNull(fieldLabel, "Field label not null.");
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetFieldHelpIsNullTest()
		{
			var fieldHelp = m_mdc.GetFieldHelp(5002003);
			Assert.IsNull(fieldHelp, "Field help not null.");
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetFieldXmlIsNullTest()
		{
			var fieldXml = m_mdc.GetFieldXml(5002003);
			Assert.IsNull(fieldXml, "Field XML not null.");
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetFieldListRootIsZeroTest()
		{
			var fieldListRoot = m_mdc.GetFieldListRoot(5002003);
			Assert.AreEqual(Guid.Empty, fieldListRoot, "Field XML not zero.");
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetFieldWsIsZeroTest()
		{
			var ws = m_mdc.GetFieldWs(5002003);
			Assert.AreEqual(0, ws, "Writing system not zero.");
		}

		/// <summary>
		/// Check for all the types used in the model.
		/// </summary>
		[Test]
		public void GetFieldTypeTest()
		{
			var type = (CellarPropertyType)m_mdc.GetFieldType(7019);
			Assert.AreEqual(CellarPropertyType.Boolean, type, "Wrong field data type for Boolean data.");

			type = (CellarPropertyType)m_mdc.GetFieldType(7015);
			Assert.AreEqual(CellarPropertyType.Integer, type, "Wrong field data type for Integer data.");

			// CellarPropertyType.Numeric: (Not used in model, as of 9 Februrary 2008.)

			// CellarPropertyType.Float: (Not used in model, as of 9 Februrary 2008.)

			type = (CellarPropertyType)m_mdc.GetFieldType(7011);
			Assert.AreEqual(CellarPropertyType.Time, type, "Wrong field data type for Time data.");

			type = (CellarPropertyType)m_mdc.GetFieldType(8021);
			Assert.AreEqual(CellarPropertyType.Guid, type, "Wrong field data type for Guid data.");

			// CellarPropertyType.Image: (Not used in model, as of 9 Februrary 2008.)

			type = (CellarPropertyType)m_mdc.GetFieldType(13004);
			Assert.AreEqual(CellarPropertyType.GenDate, type, "Wrong field data type for GenDate data.");

			type = (CellarPropertyType)m_mdc.GetFieldType(15002);
			Assert.AreEqual(CellarPropertyType.Binary, type, "Wrong field data type for Binary data.");

			type = (CellarPropertyType)m_mdc.GetFieldType(5097008);
			Assert.AreEqual(CellarPropertyType.String, type, "Wrong field data type for String data.");

			type = (CellarPropertyType)m_mdc.GetFieldType(5016016);
			Assert.AreEqual(CellarPropertyType.MultiString, type, "Wrong field data type for MultiString data.");

			type = (CellarPropertyType)m_mdc.GetFieldType(55009);
			Assert.AreEqual(CellarPropertyType.Unicode, type, "Wrong field data type for Unicode data.");

			type = (CellarPropertyType)m_mdc.GetFieldType(55005);
			Assert.AreEqual(CellarPropertyType.MultiUnicode, type, "Wrong field data type for MultiUnicode data.");

			type = (CellarPropertyType)m_mdc.GetFieldType(7012);
			Assert.AreEqual(CellarPropertyType.OwningAtomic, type, "Wrong field data type for Atomic Owing data.");

			type = (CellarPropertyType)m_mdc.GetFieldType(4001);
			Assert.AreEqual(CellarPropertyType.ReferenceAtomic, type, "Wrong field data type for Atomic Reference data.");

			type = (CellarPropertyType)m_mdc.GetFieldType(2003);
			Assert.AreEqual(CellarPropertyType.OwningCollection, type, "Wrong field data type for Owning Collection data.");

			type = (CellarPropertyType)m_mdc.GetFieldType(7007);
			Assert.AreEqual(CellarPropertyType.ReferenceCollection, type, "Wrong field data type for Reference Collection data.");

			type = (CellarPropertyType)m_mdc.GetFieldType(8008);
			Assert.AreEqual(CellarPropertyType.OwningSequence, type, "Wrong field data type for Owning Sequence data.");

			type = (CellarPropertyType)m_mdc.GetFieldType(5002019);
			Assert.AreEqual(CellarPropertyType.ReferenceSequence, type, "Wrong field data type for Reference Sequence data.");
		}

		/// <summary>
		/// Check for validity of adding the given clid to some field.
		/// </summary>
		[Test]
		public void get_IsValidClassTest()
		{
			var isValid = m_mdc.get_IsValidClass(5002019, 0);
			Assert.IsTrue(isValid, "Can't put a CmObject into a signature of CmObject?");

			isValid = m_mdc.get_IsValidClass(5002011, 0);
			Assert.IsFalse(isValid, "Can put a CmObject into a signature of LexSense?");

			isValid = m_mdc.get_IsValidClass(5002011, 5064);
			Assert.IsFalse(isValid, "Can put a WordformLookupItem into a signature of LexSense?");

			// 5002023 on LexEntry went away, so find another candidate.
			//isValid = m_mdc.get_IsValidClass(5002023, 5049);
			//Assert.IsTrue(isValid, "Can't put a PartOfSpeech into a signature of CmPossibility, even if you probably shouldn't?");

			isValid = m_mdc.get_IsValidClass(5002001, 5049);
			Assert.IsFalse(isValid, "Can put a PartOfSpeech into a basic field?");
		}

		/// <summary>
		/// Check for validity of adding the given clid to an illegal field of 0.
		/// </summary>
		[Test]
		[ExpectedException(typeof(FDOInvalidFieldException))]
		public void get_IsValidClassBadTest()
		{
			m_mdc.get_IsValidClass(0, 0);
		}

		/// <summary>
		/// Check if 'opinionated' is working.
		/// </summary>
		[Test]
		[ExpectedException(typeof(FDOInvalidFieldException))]
		public void Nonmodel_flid_is_bad_for_finding_FieldType()
		{
			m_mdc.GetFieldType(int.MaxValue);
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void GetFieldNameOrNull()
		{
			Assert.AreEqual(null,m_mdc.GetFieldNameOrNull(1));
			Assert.AreEqual("Senses", m_mdc.GetFieldNameOrNull(LexEntryTags.kflidSenses));
		}
	}

	#endregion MetaDataCacheFieldAccessTests class

	#region CustomFieldTests class

	/// <summary>
	/// Test User-defined custom fields.
	/// </summary>
	[TestFixture]
	public class CustomFieldTests : FieldTestBase
	{
		/// <summary>
		/// Add custom field to bogus class.
		/// </summary>
		[Test]
		[ExpectedException(typeof(KeyNotFoundException))]
		public void AddCustomFieldToNonExtantDestClassClassTest()
		{
			m_mdc.AddCustomField("WfiWordform", "NewAtomic", CellarPropertyType.OwningAtomic, Int32.MaxValue);
		}

		/// <summary>
		/// Add custom field to bogus class.
		/// </summary>
		[Test]
		[ExpectedException(typeof(FDOInvalidClassException))]
		public void AddCustomFieldToBogusClassTest()
		{
			m_mdc.AddCustomField("FakeObj", "FakeField", CellarPropertyType.Boolean, 0);
		}

		/// <summary>
		/// Add custom field to null class.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void AddCustomFieldToEmptyClassTest()
		{
			m_mdc.AddCustomField(null, "FakeField", CellarPropertyType.Boolean, 0);
		}

		/// <summary>
		/// Add null custom field.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void AddNullCustomFieldTest()
		{
			m_mdc.AddCustomField("CmObject", null, CellarPropertyType.Boolean, 0);
		}

		/// <summary>
		/// Add custom field that matches extant field.
		/// </summary>
		[Test]
		[ExpectedException(typeof(FDOInvalidFieldException))]
		public void AddDuplicatedCustomFieldTest()
		{
			m_mdc.AddCustomField("MoStemMsa", "PartOfSpeech", CellarPropertyType.Boolean, 0);
		}

		/// <summary>
		/// Add good custom fields.
		/// </summary>
		[Test]
		public void AddGoodCustomFieldTest()
		{
			var flid = m_mdc.AddCustomField("WfiWordform", "Certified", CellarPropertyType.Boolean, 0);
			Assert.IsTrue(m_mdc.IsCustom(flid), "Not a custom field.");

			var analFlid = m_mdc.GetFieldId("WfiWordform", "Analyses", false);
			Assert.IsFalse(m_mdc.IsCustom(analFlid), "Should be normal field");
			var vFlid = m_mdc.GetFieldId("WfiWordform", "ParserCount", false);
			Assert.IsFalse(m_mdc.IsCustom(vFlid), "Should not be custom field");

			// Add atomic owning custom property.
			flid = m_mdc.AddCustomField("WfiWordform", "NewAtomicOwning", CellarPropertyType.OwningAtomic, PartOfSpeechTags.kClassId);
			Assert.IsTrue(m_mdc.IsCustom(flid), "Not a custom field.");

			// Add vector custom property.
			flid = m_mdc.AddCustomField("WfiWordform", "NewOwningCol", CellarPropertyType.OwningCollection, PartOfSpeechTags.kClassId);
			Assert.IsTrue(m_mdc.IsCustom(flid), "Not a custom field.");
		}
	}

	#endregion CustomFieldTests class

	#region MetaDataCacheClassAccessTests class

	/// <summary>
	/// Test the class access methods.
	/// </summary>
	[TestFixture]
	public class ClassAccessTests : FieldTestBase
	{
		/// <summary>
		/// Check for finding the class name based on the given clid.
		/// </summary>
		[Test]
		public void GetClassNameTest()
		{
			string className = m_mdc.GetClassName(5049);
			Assert.AreEqual("PartOfSpeech", className, "Wrong class name for PartOfSpeech.");
		}

		/// <summary>
		/// Check for finding the class name based on the given clid.
		/// </summary>
		[Test]
		public void GetAbstractTest()
		{
			var isAbstract = m_mdc.GetAbstract(5049);
			Assert.IsFalse(isAbstract, "PartOfSpeech is a concrete class.");

			isAbstract = m_mdc.GetAbstract(0);
			Assert.IsTrue(isAbstract, "CmObject is an abstract class.");
		}

		/// <summary>
		/// Check for finding the class name based on the given clid.
		/// </summary>
		[Test]
		public void GetBaseClsIdTest()
		{
			var baseClassClid = m_mdc.GetBaseClsId(5049);
			Assert.AreEqual(7, baseClassClid, "Wrong base class id for PartOfSpeech.");
		}

		/// <summary>
		/// Check for finding the class name based on the given clid.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void GetBaseClsIdBadTest()
		{
			m_mdc.GetBaseClsId(0);
		}

		/// <summary>
		/// Check for finding the class name based on the given clid.
		/// </summary>
		[Test]
		public void GetBaseClsNameTest()
		{
			var baseClassName = m_mdc.GetBaseClsName(5049);
			Assert.AreEqual("CmPossibility", baseClassName, "Wrong base class id for PartOfSpeech.");
		}

		/// <summary>
		/// Check for finding the class name based on the given clid.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void GetBaseClsNameBadTest()
		{
			m_mdc.GetBaseClsName(0);
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetClassIdsTest()
		{
			// Just use the count for all classes,
			// even though we know it will never be that high a number that can be returned.
			var countAllClasses = m_mdc.ClassCount;

			int[] ids;
			using (var clidsPtr = MarshalEx.ArrayToNative<int>(countAllClasses))
			{
				m_mdc.GetClassIds(countAllClasses, clidsPtr);
				ids = MarshalEx.NativeToArray<int>(clidsPtr, countAllClasses);
				Assert.AreEqual(countAllClasses, ids.Length, "Wrong number of classes returned.");
			}

			countAllClasses = 2;
			// Check MoForm (all of its direct subclasses).
			using (var clidsPtr = MarshalEx.ArrayToNative<int>(countAllClasses))
			{
				m_mdc.GetClassIds(countAllClasses, clidsPtr);
				ids = MarshalEx.NativeToArray<int>(clidsPtr, countAllClasses);
				Assert.AreEqual(countAllClasses, ids.Length, "Wrong number of classes returned.");
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void GetFieldsTest()
		{
			using (var flids = MarshalEx.ArrayToNative<int>(500))
			{
				var countAllFlidsOut = m_mdc.GetFields(0, true, (int)CellarPropertyTypeFilter.All, 0, flids);
				countAllFlidsOut = m_mdc.GetFields(0, true, (int)CellarPropertyTypeFilter.All, countAllFlidsOut, flids);

				MarshalEx.NativeToArray<int>(flids, countAllFlidsOut);
				Assert.AreEqual(7, countAllFlidsOut, "Wrong number of fields returned for CmObject.");
			}
		}

		/// <summary>
		/// GetFields should not include base class fields when told not to.
		/// </summary>
		[Test]
		public void GetFieldsDoesNotIncludeBaseFields()
		{
			using (var flids = MarshalEx.ArrayToNative<int>(500))
			{
				var countAllFlidsOut = m_mdc.GetFields(MoStemAllomorphTags.kClassId, false, (int)CellarPropertyTypeFilter.All, 0, flids);
				countAllFlidsOut = m_mdc.GetFields(MoStemAllomorphTags.kClassId, false, (int)CellarPropertyTypeFilter.All, countAllFlidsOut, flids);

				var fields = new List<int>(MarshalEx.NativeToArray<int>(flids, countAllFlidsOut));
				Assert.That(fields, Has.Member(MoStemAllomorphTags.kflidPhoneEnv)); // not inherited
				Assert.That(fields, Has.No.Member(MoFormTags.kflidForm)); // inherited
			}

		}
	}

	#endregion MetaDataCacheClassAccessTests class

	#region MetaDataCacheReverseAccessTests class

	/// <summary>
	/// Test the reverse access methods.
	/// </summary>
	[TestFixture]
	public class ReverseAccessTests : FieldTestBase
	{
		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		public void GetClassIdTest()
		{
			var clid = m_mdc.GetClassId("LexEntry");
			Assert.AreEqual(5002, clid, "Wrong class Id.");
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		[ExpectedException(typeof(FDOInvalidClassException))]
		public void GetClassIdBadTest()
		{
			m_mdc.GetClassId("NonExistantClassName");
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		public void GetFieldIdSansSuperClassCheckTest()
		{
			var flid = m_mdc.GetFieldId("LexEntry", "CitationForm", false);
			Assert.AreEqual(5002003, flid, "Wrong field Id.");
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		public void GetFieldIdWithSuperClassCheckTest()
		{
			var flid = m_mdc.GetFieldId("PartOfSpeech", "Name", true);
			Assert.AreEqual(7001, flid, "Wrong field Id.");
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly.
		/// </summary>
		[Test]
		[ExpectedException(typeof(FDOInvalidFieldException))]
		public void GetFieldIdSansSuperClassCheckBadTest1()
		{
			m_mdc.GetFieldId("MoStemMsa", "CitationForm", false);
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		[ExpectedException(typeof(FDOInvalidFieldException))]
		public void GetFieldIdWithSuperClassCheckBadTest()
		{
			m_mdc.GetFieldId("MoStemMsa", "CitationForm", true);
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		public void GetFieldId2SansSuperClassCheckTest()
		{
			var flid = m_mdc.GetFieldId2(5002, "CitationForm", false);
			Assert.AreEqual(5002003, flid, "Wrong field Id.");
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		public void GetFieldId2WithSuperClassCheckTest()
		{
			var flid = m_mdc.GetFieldId2(5049, "Name", true);
			Assert.AreEqual(7001, flid, "Wrong field Id.");
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly.
		/// </summary>
		[Test]
		[ExpectedException(typeof(FDOInvalidFieldException))]
		public void GetFieldId2SansSuperClassCheckBadTest1()
		{
			m_mdc.GetFieldId2(5001, "CitationForm", false);
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		[ExpectedException(typeof(FDOInvalidFieldException))]
		public void GetFieldId2WithSuperClassCheckBadTest()
		{
			m_mdc.GetFieldId2(5001, "CitationForm", true);
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetDirectSubclassesTest()
		{
			// Just use the count for all classes,
			// even though we know it will never be that high a number that can be returned.
			var countAllClasses = m_mdc.ClassCount;
			int countDirectSubclasses;
			//int[] clids = new int[countAllClasses];
			// Check PartOfSpeech.
			using (var clids = MarshalEx.ArrayToNative<int>(countAllClasses))
			{
				// Check PartOfSpeech.
				m_mdc.GetDirectSubclasses(5049, countAllClasses, out countDirectSubclasses, clids);
				Assert.AreEqual(0, countDirectSubclasses, "Wrong number of subclasses returned.");
			}

			using (var clids = MarshalEx.ArrayToNative<int>(countAllClasses))
			{
				// Check MoForm (all of its direct subclasses).
				m_mdc.GetDirectSubclasses(5035, countAllClasses, out countDirectSubclasses, clids);
				Assert.AreEqual(2, countDirectSubclasses, "Wrong number of subclasses returned.");
				var uIds = MarshalEx.NativeToArray<int>(clids, countAllClasses);
				for (var i = 0; i < uIds.Length; ++i)
				{
					var clid = uIds[i];
					if (i < 2)
						Assert.IsTrue(((clid == 5028) || (clid == 5045)), "Clid should be 5028 or 5049 for direct subclasses of MoForm.");
					else
						Assert.AreEqual(0, clid, "Clid should be 0 from here on.");
				}
			}

			/* The method does not support getting some arbitrary subset of subclasses.
			 * The array must contain at least that many spaces, if not more.
			countDirectSubclasses = 0;
			using (ArrayPtr clids = MarshalEx.ArrayToNative(1, typeof(int)))
			{
				// Check MoForm (but only 1 of its subclasses).
				m_mdc.GetDirectSubclasses(5035, 1, out countDirectSubclasses, clids);
				Assert.AreEqual(1, countDirectSubclasses, "Wrong number of subclasses returned.");
			}
			*/
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetDirectSubclassesCountUnknownTest()
		{
			int countAllClasses;
			m_mdc.GetDirectSubclasses(5035, 0, out countAllClasses, null);
			Assert.AreEqual(2, countAllClasses, "Wrong number of subclasses returned.");
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetAllSubclassesPartOfSpeechTest()
		{
			// Just use the count for all classes,
			// even though we know it will never be that high a number that can be returned.
			var countAllClasses = m_mdc.ClassCount;
			using (var clids = MarshalEx.ArrayToNative<int>(countAllClasses))
			{
				// Check PartOfSpeech.
				int countAllSubclasses;
				m_mdc.GetAllSubclasses(5049, countAllClasses, out countAllSubclasses, clids);
				Assert.AreEqual(1, countAllSubclasses, "Wrong number of subclasses returned.");
			}
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetAllSubclassesMoFormAllTest()
		{
			// Just use the count for all classes,
			// even though we know it will never be that high a number that can be returned.
			var countAllClasses = m_mdc.ClassCount;
			using (var clids = MarshalEx.ArrayToNative<int>(countAllClasses))
			{
				// Check MoForm (all of its direct subclasses).
				int countAllSubclasses;
				m_mdc.GetAllSubclasses(5035, countAllClasses, out countAllSubclasses, clids);
				Assert.AreEqual(5, countAllSubclasses, "Wrong number of subclasses returned.");
			}
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetAllSubclassesMoFormLimitedTest()
		{
			// Just use the count for all classes,
			// even though we know it will never be that high a number that can be returned.
			var countAllClasses = m_mdc.ClassCount;
			using (var clids = MarshalEx.ArrayToNative<int>(countAllClasses))
			{
				// Check MoForm (but get it and only 1 of its subclasses).
				int countAllSubclasses;
				m_mdc.GetAllSubclasses(5035, 2, out countAllSubclasses, clids);
				Assert.AreEqual(2, countAllSubclasses, "Wrong number of subclasses returned.");
			}
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetAllSubclassesCmObjectTest()
		{
			// Just use the count for all classes,
			// even though we know it will never be that high a number that can be returned.
			var countAllClasses = m_mdc.ClassCount;
			using (var clids = MarshalEx.ArrayToNative<int>(countAllClasses))
			{
				// Check CmObject.
				int countAllSubclasses;
				m_mdc.GetAllSubclasses(0, countAllClasses, out countAllSubclasses, clids);
				Assert.AreEqual(countAllClasses, countAllSubclasses, "Wrong number of subclasses returned.");
			}
		}
	}

	#endregion MetaDataCacheReverseAccessTests class

	#region MetaDataCacheNotImplementedTests class

	/// <summary>
	/// Test the methods that are not to be implemented.
	/// </summary>
	[TestFixture]
	public class NotImplementedTests : FieldTestBase
	{
		/// <summary>
		/// Not implemented test for Init method.
		/// </summary>
		[Test]
		[ExpectedException(typeof(NotSupportedException))]
		public void InitXmlTest()
		{
			m_mdc.InitXml(null, true);
		}

		/// <summary>
		/// Not implemented test for Init method.
		/// </summary>
		[Test]
		[ExpectedException(typeof(NotSupportedException))]
		public void AddVirtualProp()
		{
			m_mdc.AddVirtualProp(null, null, 0, 0);
		}
	}

	#endregion MetaDataCacheNotImplementedTests class

}