// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2013, SIL International. All Rights Reserved.
// <copyright from='2013' to='2013' company='SIL International'>
//		Copyright (c) 2013, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DataMigration7000066Tests.cs
// Responsibility: RandyR
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000065 to 7000066.
	/// </summary>
	[TestFixture]
	public sealed class DataMigration7000066Tests : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000065 to 7000066 for basic data regular properties.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AllRegularBasicDataPropertiesExistAfterDataMigration66()
		{
			var mockMdc = new MockMDCForDataMigration();
			mockMdc.AddClass(1, "CmObject", null, new List<string> { "RegularPropertyMagnet" });
			mockMdc.AddClass(2, "RegularPropertyMagnet", "CmObject", new List<string>());

			// These are all present in the original data file and act as controls.
			// They should not be changed.
			var currentFlid = 2000;
			mockMdc.AddField(++currentFlid, "ExtantOwningAtomic", CellarPropertyType.OwningAtom, 2);
			mockMdc.AddField(++currentFlid, "ExtantBooleanProperty", CellarPropertyType.Boolean, 0);
			mockMdc.AddField(++currentFlid, "ExtantGenDateProperty", CellarPropertyType.GenDate, 0);
			mockMdc.AddField(++currentFlid, "ExtantGuidProperty", CellarPropertyType.Guid, 0);
			// Not used in model yet (as of 23 march 2013) mockMdc.AddField(++currentFlid, "ExtantFloatProperty", CellarPropertyType.Float, 0);
			mockMdc.AddField(++currentFlid, "ExtantIntegerProperty", CellarPropertyType.Integer, 0);
			// Not used in model yet (as of 23 march 2013) var mockMdc.AddField(++currentFlid, "ExtantNumericProperty", CellarPropertyType.Numeric, 0);
			mockMdc.AddField(++currentFlid, "ExtantTimeProperty", CellarPropertyType.Time, 0);

			// These are all missing in the original data file.
			// They should all end up with the default values for the given type of data.
			mockMdc.AddField(++currentFlid, "NewBooleanProperty", CellarPropertyType.Boolean, 0);
			mockMdc.AddField(++currentFlid, "NewGenDateProperty", CellarPropertyType.GenDate, 0);
			mockMdc.AddField(++currentFlid, "NewGuidProperty", CellarPropertyType.Guid, 0);
			// Not used in model yet (as of 23 march 2013) mockMdc.AddField(++currentFlid, "NewFloatProperty", CellarPropertyType.Float, 0);
			mockMdc.AddField(++currentFlid, "NewIntegerProperty", CellarPropertyType.Integer, 0);
			// Not used in model yet (as of 23 march 2013) mockMdc.AddField(++currentFlid, "NewNumericProperty", CellarPropertyType.Numeric, 0);
			mockMdc.AddField(++currentFlid, "NewTimeProperty", CellarPropertyType.Time, 0);

			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000066_RegularPropertyMagnet.xml");
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000065, dtos, mockMdc, null);

			m_dataMigrationManager.PerformMigration(dtoRepos, 7000066, new DummyProgressDlg());

			var magnet = XElement.Parse(dtoRepos.AllInstancesSansSubclasses("RegularPropertyMagnet").First().Xml);

			// Check the 'control' props to make sure they were not changed.
			Assert.AreEqual("e268fe80-5d9d-4f6b-a68f-37db8218b15d", magnet.Element("ExtantOwningAtomic").Element("objsur").Attribute("guid").Value);
			Assert.AreEqual("True", GetRegularPropertyValue(magnet, "ExtantBooleanProperty"));
			Assert.AreEqual("-201303233", GetRegularPropertyValue(magnet, "ExtantGenDateProperty"));
			Assert.AreEqual("c1ee311b-e382-11de-8a39-0800200c9a66", GetRegularPropertyValue(magnet, "ExtantGuidProperty"));
			Assert.AreEqual("1", GetRegularPropertyValue(magnet, "ExtantIntegerProperty"));
			Assert.AreEqual("2006-3-12 18:19:46.87", GetRegularPropertyValue(magnet, "ExtantTimeProperty"));

			// Check the newly added props to make sure they are present and using default values.
			Assert.AreEqual("False", GetRegularPropertyValue(magnet, "NewBooleanProperty"));
			Assert.AreEqual("-000000000", GetRegularPropertyValue(magnet, "NewGenDateProperty"));
			Assert.AreEqual(Guid.Empty.ToString(), GetRegularPropertyValue(magnet, "NewGuidProperty"));
			Assert.AreEqual("0", GetRegularPropertyValue(magnet, "NewIntegerProperty"));
			Assert.IsNotNull(GetRegularPropertyValue(magnet, "NewTimeProperty"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000065 to 7000066 for inherited basic data regular properties.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AllInheritedRegularBasicDataPropertiesExistAfterDataMigration66()
		{
			var mockMdc = new MockMDCForDataMigration();
			mockMdc.AddClass(1, "CmObject", null, new List<string> { "RegularPropertyMagnet" });
			// Add some CmObject basic properties that should never be written out.
			var currentFlid = 1000;
			mockMdc.AddField(++currentFlid, "ClassID", CellarPropertyType.Integer, 0);
			mockMdc.AddField(++currentFlid, "Guid", CellarPropertyType.Guid, 0);
			mockMdc.AddField(++currentFlid, "OwningFlid", CellarPropertyType.Integer, 0);
			mockMdc.AddField(++currentFlid, "OwnOrd", CellarPropertyType.Integer, 0);
			mockMdc.AddVirtualProp("CmObject", "VirtualProp", ++currentFlid, (int)CellarPropertyType.Integer);
			mockMdc.AddClass(2, "RegularPropertyMagnet", "CmObject", new List<string>());

			// These are all defined on CmObject, so RegularPropertyMagnet should inherit them.
			// These are all present in the original data file and act as controls.
			// They should not be changed.
			mockMdc.AddField(++currentFlid, "ExtantOwningAtomic", CellarPropertyType.OwningAtom, 2);
			mockMdc.AddField(++currentFlid, "ExtantBooleanProperty", CellarPropertyType.Boolean, 0);
			mockMdc.AddField(++currentFlid, "ExtantGenDateProperty", CellarPropertyType.GenDate, 0);
			mockMdc.AddField(++currentFlid, "ExtantGuidProperty", CellarPropertyType.Guid, 0);
			// Not used in model yet (as of 23 march 2013) mockMdc.AddField(++currentFlid, "ExtantFloatProperty", CellarPropertyType.Float, 0);
			mockMdc.AddField(++currentFlid, "ExtantIntegerProperty", CellarPropertyType.Integer, 0);
			// Not used in model yet (as of 23 march 2013) var mockMdc.AddField(++currentFlid, "ExtantNumericProperty", CellarPropertyType.Numeric, 0);
			mockMdc.AddField(++currentFlid, "ExtantTimeProperty", CellarPropertyType.Time, 0);

			// These are all missing in the original data file.
			// They should all end up with the default values for the given type of data.
			mockMdc.AddField(++currentFlid, "NewBooleanProperty", CellarPropertyType.Boolean, 0);
			mockMdc.AddField(++currentFlid, "NewGenDateProperty", CellarPropertyType.GenDate, 0);
			mockMdc.AddField(++currentFlid, "NewGuidProperty", CellarPropertyType.Guid, 0);
			// Not used in model yet (as of 23 march 2013) mockMdc.AddField(++currentFlid, "NewFloatProperty", CellarPropertyType.Float, 0);
			mockMdc.AddField(++currentFlid, "NewIntegerProperty", CellarPropertyType.Integer, 0);
			// Not used in model yet (as of 23 march 2013) mockMdc.AddField(++currentFlid, "NewNumericProperty", CellarPropertyType.Numeric, 0);
			mockMdc.AddField(++currentFlid, "NewTimeProperty", CellarPropertyType.Time, 0);

			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000066_RegularPropertyMagnet.xml");
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000065, dtos, mockMdc, null);

			m_dataMigrationManager.PerformMigration(dtoRepos, 7000066, new DummyProgressDlg());

			var magnet = XElement.Parse(dtoRepos.AllInstancesSansSubclasses("RegularPropertyMagnet").First().Xml);

			// Make sure these properties aren't written out.
			Assert.IsNull(magnet.Element("ClassID"));
			Assert.IsNull(magnet.Element("Guid"));
			Assert.IsNull(magnet.Element("OwningFlid"));
			Assert.IsNull(magnet.Element("OwnOrd"));
			Assert.IsNull(magnet.Element("VirtualProp"));

			// Check the 'control' props to make sure they were not changed.
			Assert.AreEqual("e268fe80-5d9d-4f6b-a68f-37db8218b15d", magnet.Element("ExtantOwningAtomic").Element("objsur").Attribute("guid").Value);
			Assert.AreEqual("True", GetRegularPropertyValue(magnet, "ExtantBooleanProperty"));
			Assert.AreEqual("-201303233", GetRegularPropertyValue(magnet, "ExtantGenDateProperty"));
			Assert.AreEqual("c1ee311b-e382-11de-8a39-0800200c9a66", GetRegularPropertyValue(magnet, "ExtantGuidProperty"));
			Assert.AreEqual("1", GetRegularPropertyValue(magnet, "ExtantIntegerProperty"));
			Assert.AreEqual("2006-3-12 18:19:46.87", GetRegularPropertyValue(magnet, "ExtantTimeProperty"));

			// Check the newly added props to make sure they are present and using default values.
			Assert.AreEqual("False", GetRegularPropertyValue(magnet, "NewBooleanProperty"));
			Assert.AreEqual("-000000000", GetRegularPropertyValue(magnet, "NewGenDateProperty"));
			Assert.AreEqual(Guid.Empty.ToString(), GetRegularPropertyValue(magnet, "NewGuidProperty"));
			Assert.AreEqual("0", GetRegularPropertyValue(magnet, "NewIntegerProperty"));
			Assert.IsNotNull(GetRegularPropertyValue(magnet, "NewTimeProperty"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000065 to 7000066 for basic data custom properties.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AllCustomBasicDataPropertiesExistAfterDataMigration66()
		{
			var mockMdc = new MockMDCForDataMigration();
			mockMdc.AddClass(1, "CmObject", null, new List<string> { "CustomPropertyMagnet" });
			mockMdc.AddClass(2, "CustomPropertyMagnet", "CmObject", new List<string>());

			// These are all present in the original data file and act as controls.
			// They should not be changed.
			mockMdc.AddField(2001, "RegularOwningAtomic", CellarPropertyType.OwningAtom, 2);
			var extantExtantOwningAtomicFlid = mockMdc.AddCustomField("CustomPropertyMagnet", "ExtantOwningAtomic", CellarPropertyType.OwningAtom, 2);
			var extantBoolFlid = mockMdc.AddCustomField("CustomPropertyMagnet", "ExtantBooleanProperty", CellarPropertyType.Boolean, 0);
			var extantGenDateFlid = mockMdc.AddCustomField("CustomPropertyMagnet", "ExtantGenDateProperty", CellarPropertyType.GenDate, 0);
			var extantGuidFlid = mockMdc.AddCustomField("CustomPropertyMagnet", "ExtantGuidProperty", CellarPropertyType.Guid, 0);
			// Not used in model yet (as of 23 march 2013) var extantFloatFlid = mockMdc.AddCustomField("CustomPropertyMagnet", "ExtantFloatProperty", CellarPropertyType.Float, 0);
			var extantIntegerFlid = mockMdc.AddCustomField("CustomPropertyMagnet", "ExtantIntegerProperty", CellarPropertyType.Integer, 0);
			// Not used in model yet (as of 23 march 2013) var extantNumericFlid = mockMdc.AddCustomField("CustomPropertyMagnet", "ExtantNumericProperty", CellarPropertyType.Numeric, 0);
			var extantTimeFlid = mockMdc.AddCustomField("CustomPropertyMagnet", "ExtantTimeProperty", CellarPropertyType.Time, 0);

			// These are all missing in the original data file.
			// They should all end up with the default values for the given type of data.
			var missingBoolFlid = mockMdc.AddCustomField("CustomPropertyMagnet", "NewBooleanProperty", CellarPropertyType.Boolean, 0);
			var missingGenDateFlid = mockMdc.AddCustomField("CustomPropertyMagnet", "NewGenDateProperty", CellarPropertyType.GenDate, 0);
			var missingGuidFlid = mockMdc.AddCustomField("CustomPropertyMagnet", "NewGuidProperty", CellarPropertyType.Guid, 0);
			// Not used in model yet (as of 23 march 2013) var missingFloatFlid = mockMdc.AddCustomField("CustomPropertyMagnet", "NewFloatProperty", CellarPropertyType.Float, 0);
			var missingIntegerFlid = mockMdc.AddCustomField("CustomPropertyMagnet", "NewIntegerProperty", CellarPropertyType.Integer, 0);
			// Not used in model yet (as of 23 march 2013) var missingNumericFlid = mockMdc.AddCustomField("CustomPropertyMagnet", "NewNumericProperty", CellarPropertyType.Numeric, 0);
			var missingTimeFlid = mockMdc.AddCustomField("CustomPropertyMagnet", "NewTimeProperty", CellarPropertyType.Time, 0);

			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000066_CustomPropertyMagnet.xml");
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000065, dtos, mockMdc, null);

			m_dataMigrationManager.PerformMigration(dtoRepos, 7000066, new DummyProgressDlg());

			var magnet = XElement.Parse(dtoRepos.AllInstancesSansSubclasses("CustomPropertyMagnet").First().Xml);

			// Check the 'control' props to make sure they were not changed.
			Assert.AreEqual("e268fe80-5d9d-4f6b-a68f-37db8218b15e", magnet.Element("RegularOwningAtomic").Element("objsur").Attribute("guid").Value);
			Assert.AreEqual("e268fe80-5d9d-4f6b-a68f-37db8218b15d", GetCustomPropertyElement(magnet, "ExtantOwningAtomic").Element("objsur").Attribute("guid").Value);
			Assert.AreEqual("True", GetCustomPropertyValue(magnet, "ExtantBooleanProperty"));
			Assert.AreEqual("-201303233", GetCustomPropertyValue(magnet, "ExtantGenDateProperty"));
			Assert.AreEqual("c1ee311b-e382-11de-8a39-0800200c9a66", GetCustomPropertyValue(magnet, "ExtantGuidProperty"));
			Assert.AreEqual("1", GetCustomPropertyValue(magnet, "ExtantIntegerProperty"));
			Assert.AreEqual("2006-3-12 18:19:46.87", GetCustomPropertyValue(magnet, "ExtantTimeProperty"));

			// Check the newly added props to make sure they are present and using default values.
			Assert.AreEqual("False", GetCustomPropertyValue(magnet, "NewBooleanProperty"));
			Assert.AreEqual("-000000000", GetCustomPropertyValue(magnet, "NewGenDateProperty"));
			Assert.AreEqual(Guid.Empty.ToString(), GetCustomPropertyValue(magnet, "NewGuidProperty"));
			Assert.AreEqual("0", GetCustomPropertyValue(magnet, "NewIntegerProperty"));
			Assert.IsNotNull(GetCustomPropertyValue(magnet, "NewTimeProperty"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000065 to 7000066 for inherited basic data custom properties.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AllInheritedCustomBasicDataPropertiesExistAfterDataMigration66()
		{
			var mockMdc = new MockMDCForDataMigration();
			mockMdc.AddClass(1, "CmObject", null, new List<string> { "CustomPropertyMagnet" });
			mockMdc.AddClass(2, "CustomPropertyMagnet", "CmObject", new List<string>());

			// These are all present in the original data file and act as controls.
			// They should not be changed.
			mockMdc.AddField(1001, "RegularOwningAtomic", CellarPropertyType.OwningAtom, 2);
			var extantExtantOwningAtomicFlid = mockMdc.AddCustomField("CmObject", "ExtantOwningAtomic", CellarPropertyType.OwningAtom, 2);
			var extantBoolFlid = mockMdc.AddCustomField("CmObject", "ExtantBooleanProperty", CellarPropertyType.Boolean, 0);
			var extantGenDateFlid = mockMdc.AddCustomField("CmObject", "ExtantGenDateProperty", CellarPropertyType.GenDate, 0);
			var extantGuidFlid = mockMdc.AddCustomField("CmObject", "ExtantGuidProperty", CellarPropertyType.Guid, 0);
			// Not used in model yet (as of 23 march 2013) var extantFloatFlid = mockMdc.AddCustomField("CmObject", "ExtantFloatProperty", CellarPropertyType.Float, 0);
			var extantIntegerFlid = mockMdc.AddCustomField("CmObject", "ExtantIntegerProperty", CellarPropertyType.Integer, 0);
			// Not used in model yet (as of 23 march 2013) var extantNumericFlid = mockMdc.AddCustomField("CmObject", "ExtantNumericProperty", CellarPropertyType.Numeric, 0);
			var extantTimeFlid = mockMdc.AddCustomField("CmObject", "ExtantTimeProperty", CellarPropertyType.Time, 0);

			// These are all missing in the original data file.
			// They should all end up with the default values for the given type of data.
			var missingBoolFlid = mockMdc.AddCustomField("CmObject", "NewBooleanProperty", CellarPropertyType.Boolean, 0);
			var missingGenDateFlid = mockMdc.AddCustomField("CmObject", "NewGenDateProperty", CellarPropertyType.GenDate, 0);
			var missingGuidFlid = mockMdc.AddCustomField("CmObject", "NewGuidProperty", CellarPropertyType.Guid, 0);
			// Not used in model yet (as of 23 march 2013) var missingFloatFlid = mockMdc.AddCustomField("CmObject", "NewFloatProperty", CellarPropertyType.Float, 0);
			var missingIntegerFlid = mockMdc.AddCustomField("CmObject", "NewIntegerProperty", CellarPropertyType.Integer, 0);
			// Not used in model yet (as of 23 march 2013) var missingNumericFlid = mockMdc.AddCustomField("CmObject", "NewNumericProperty", CellarPropertyType.Numeric, 0);
			var missingTimeFlid = mockMdc.AddCustomField("CmObject", "NewTimeProperty", CellarPropertyType.Time, 0);

			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000066_CustomPropertyMagnet.xml");
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000065, dtos, mockMdc, null);

			m_dataMigrationManager.PerformMigration(dtoRepos, 7000066, new DummyProgressDlg());

			var magnet = XElement.Parse(dtoRepos.AllInstancesSansSubclasses("CustomPropertyMagnet").First().Xml);

			// Check the 'control' props to make sure they were not changed.
			Assert.AreEqual("e268fe80-5d9d-4f6b-a68f-37db8218b15e", magnet.Element("RegularOwningAtomic").Element("objsur").Attribute("guid").Value);
			Assert.AreEqual("e268fe80-5d9d-4f6b-a68f-37db8218b15d", GetCustomPropertyElement(magnet, "ExtantOwningAtomic").Element("objsur").Attribute("guid").Value);
			Assert.AreEqual("True", GetCustomPropertyValue(magnet, "ExtantBooleanProperty"));
			Assert.AreEqual("-201303233", GetCustomPropertyValue(magnet, "ExtantGenDateProperty"));
			Assert.AreEqual("c1ee311b-e382-11de-8a39-0800200c9a66", GetCustomPropertyValue(magnet, "ExtantGuidProperty"));
			Assert.AreEqual("1", GetCustomPropertyValue(magnet, "ExtantIntegerProperty"));
			Assert.AreEqual("2006-3-12 18:19:46.87", GetCustomPropertyValue(magnet, "ExtantTimeProperty"));

			// Check the newly added props to make sure they are present and using default values.
			Assert.AreEqual("False", GetCustomPropertyValue(magnet, "NewBooleanProperty"));
			Assert.AreEqual("-000000000", GetCustomPropertyValue(magnet, "NewGenDateProperty"));
			Assert.AreEqual(Guid.Empty.ToString(), GetCustomPropertyValue(magnet, "NewGuidProperty"));
			Assert.AreEqual("0", GetCustomPropertyValue(magnet, "NewIntegerProperty"));
			Assert.IsNotNull(GetCustomPropertyValue(magnet, "NewTimeProperty"));
		}

		private static string GetRegularPropertyValue(XElement objectElement, string regularPropertyName)
		{
			return objectElement.Element(regularPropertyName).Attribute("val").Value;
		}

		private static string GetCustomPropertyValue(XElement objectElement, string customPropertyName)
		{
			var customPropertyElement = GetCustomPropertyElement(objectElement, customPropertyName);
			return customPropertyElement.Attribute("val").Value;
		}

		private static XElement GetCustomPropertyElement(XElement objectElement, string customPropertyName)
		{
			return objectElement.Elements("Custom").First(customProperty => customProperty.Attribute("name").Value == customPropertyName);
		}
	}
}