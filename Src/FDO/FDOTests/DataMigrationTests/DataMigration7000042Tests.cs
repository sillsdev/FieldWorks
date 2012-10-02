// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DataMigration7000042Tests.cs
// Responsibility: mcconnel
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml.XPath;
using NUnit.Framework;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test the migration from version 7000041 to 7000042.  This migration fixes problems with
	/// LexEntryType objects.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class DataMigration7000042Tests : DataMigrationTestsBase
	{
		/// <summary>This is used for creating meaningful assertion messages.</summary>
		private readonly Dictionary<string, string> m_mapGuidToName =
			new Dictionary<string, string>(new StringIgnoreCaseComparer());

		///<summary>
		/// Set up the data for the test.
		///</summary>
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			m_mapGuidToName.Add(LexEntryTypeTags.kguidLexTypCompound.ToString(), "Compound");
			m_mapGuidToName.Add(LexEntryTypeTags.kguidLexTypContraction.ToString(), "Contraction");
			m_mapGuidToName.Add(LexEntryTypeTags.kguidLexTypDerivation.ToString(), "Derivative");
			m_mapGuidToName.Add(LexEntryTypeTags.kguidLexTypIdiom.ToString(), "Idiom");
			m_mapGuidToName.Add(LexEntryTypeTags.kguidLexTypPhrasalVerb.ToString(), "Phrasal Verb");
			m_mapGuidToName.Add(LexEntryTypeTags.kguidLexTypSaying.ToString(), "Saying");
			m_mapGuidToName.Add(LexEntryTypeTags.kguidLexTypDialectalVar.ToString(), "Dialectal Variant");
			m_mapGuidToName.Add(LexEntryTypeTags.kguidLexTypFreeVar.ToString(), "Free Variant");
			m_mapGuidToName.Add(LexEntryTypeTags.kguidLexTypIrregInflectionVar.ToString(), "Irregularly Inflected Form");
			m_mapGuidToName.Add(LexEntryTypeTags.kguidLexTypPluralVar.ToString(), "Plural");
			m_mapGuidToName.Add(LexEntryTypeTags.kguidLexTypPastVar.ToString(), "Past");
			m_mapGuidToName.Add(LexEntryTypeTags.kguidLexTypSpellingVar.ToString(), "Spelling Variant");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests migration for data that has all of the required types (with proper guids), but
		/// has some improper (or missing) IsProtected values.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000042TestA()
		{
			// Bring in data from xml file.
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000042A.xml");

			// Create the DTO repository, do the migration, and verify the results.
			VerifyMigration(dtos);
		}

		///--------------------------------------------------------------------------------------
		///<summary>
		/// Tests migration for data that is missing a required type, has some improper guids,
		/// and has some improper (or missing) IsProtected values.
		///</summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000042TestB()
		{
			// Bring in data from xml file.
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000042B.xml");

			// Create the DTO repository, do the migration, and verify the results.
			VerifyMigration(dtos);
		}

		///--------------------------------------------------------------------------------------
		///<summary>
		/// Tests migration for data that is missing a required type, has some improper guids,
		/// and has some improper (or missing) IsProtected values, and has a large number of
		/// added types and subtypes.
		///</summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000042TestC()
		{
			// Bring in data from xml file.
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000042C.xml");

			// Create the DTO repository, do the migration, and verify the results.
			VerifyMigration(dtos);
		}

		///--------------------------------------------------------------------------------------
		///<summary>
		/// Tests migration for data that is missing a required type, has some improper guids,
		/// and has some improper (or missing) IsProtected values, and has a large number of
		/// added types and subtypes.
		///</summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000042TestD()
		{
			// Bring in data from xml file.
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000042D.xml");

			// Create the DTO repository, do the migration, and verify the results.
			VerifyMigration(dtos);
		}

		///--------------------------------------------------------------------------------------
		///<summary>
		/// Tests migration for data that is missing a required type, has some improper guids,
		/// and has some improper (or missing) IsProtected values, and has a large number of
		/// added types and subtypes.
		///</summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000042TestE()
		{
			// Bring in data from xml file.
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000042E.xml");

			// Create the DTO repository, do the migration, and verify the results.
			VerifyMigration(dtos);
		}

		private void VerifyMigration(HashSet<DomainObjectDTO> dtos)
		{
			// Create all the Mock classes for the classes in my test data.
			var mockMDC = new MockMDCForDataMigration();
			mockMDC.AddClass(1, "CmObject", null, new List<string> {
				"CmMajorObject", "CmPossibility", "CmProject", "LexEntry", "LexEntryRef", "LexSense",
				"MoForm", "MoMorphoSynAnalysis"});
			mockMDC.AddClass(2, "CmMajorObject", "CmObject", new List<string> {"CmPossibilityList", "LexDb"});
			mockMDC.AddClass(3, "CmPossibility", "CmObject", new List<string> {"LexEntryType", "MoMorphType"});
			mockMDC.AddClass(4, "CmPossibilityList", "CmMajorObject", new List<string>());
			mockMDC.AddClass(5, "CmProject", "CmObject", new List<string> {"LangProject"});
			mockMDC.AddClass(6, "LangProject", "CmProject", new List<string>());
			mockMDC.AddClass(7, "LexDb", "CmMajorObject", new List<string>());
			mockMDC.AddClass(8, "LexEntryType", "CmPossibility", new List<string>());
			mockMDC.AddClass(9, "LexEntry", "CmObject", new List<string>());
			mockMDC.AddClass(10, "LexEntryRef", "CmObject", new List<string>());
			mockMDC.AddClass(11, "LexSense", "CmObject", new List<string>());
			mockMDC.AddClass(12, "MoMorphType", "CmPossibility", new List<string>());
			mockMDC.AddClass(13, "MoForm", "CmObject", new List<string> { "MoStemAllomorph" });
			mockMDC.AddClass(14, "MoStemAllomorph", "MoForm", new List<string>());
			mockMDC.AddClass(15, "MoMorphoSynAnalysis", "CmObject", new List<string> { "MoStemMsa" });
			mockMDC.AddClass(16, "MoStemMsa", "MoMorphoSynAnalysis", new List<string>());

			// Create the DTO repository.
			IDomainObjectDTORepository repoDto = new DomainObjectDtoRepository(7000041, dtos, mockMDC,
				FileUtils.ChangePathToPlatform("C:\\WW\\DistFiles\\Projects\\TokPisin"));

			// Do Migration
			m_dataMigrationManager.PerformMigration(repoDto, 7000042, new DummyProgressDlg());

			// Check that the version was updated.
			Assert.AreEqual(7000042, repoDto.CurrentModelVersion, "Wrong updated version.");

			VerifyEntryType(repoDto, LexEntryTypeTags.kguidLexTypCompound.ToString());
			VerifyEntryType(repoDto, LexEntryTypeTags.kguidLexTypContraction.ToString());
			VerifyEntryType(repoDto, LexEntryTypeTags.kguidLexTypDerivation.ToString());
			VerifyEntryType(repoDto, LexEntryTypeTags.kguidLexTypIdiom.ToString());
			VerifyEntryType(repoDto, LexEntryTypeTags.kguidLexTypPhrasalVerb.ToString());
			VerifyEntryType(repoDto, LexEntryTypeTags.kguidLexTypSaying.ToString());
			VerifyEntryType(repoDto, LexEntryTypeTags.kguidLexTypDialectalVar.ToString());
			VerifyEntryType(repoDto, LexEntryTypeTags.kguidLexTypFreeVar.ToString());
			VerifyEntryType(repoDto, LexEntryTypeTags.kguidLexTypIrregInflectionVar.ToString());
			VerifyEntryType(repoDto, LexEntryTypeTags.kguidLexTypPluralVar.ToString());
			VerifyEntryType(repoDto, LexEntryTypeTags.kguidLexTypPastVar.ToString());
			VerifyEntryType(repoDto, LexEntryTypeTags.kguidLexTypSpellingVar.ToString());

			VerifyTypeLists(repoDto);
			VerifyLexEntryRefs(repoDto);
			VerifyEntryTypeOwners(repoDto);
		}

		/// <summary>
		/// Verify that the ComplexEntryTypes and VariantEntryTypes lists contain all of the
		/// standard LexEntryType objects, and that that they point only to valid LexEntryType
		/// objects.
		/// </summary>
		private void VerifyTypeLists(IDomainObjectDTORepository repoDto)
		{
			foreach (var dtoList in repoDto.AllInstancesWithSubclasses("CmPossibilityList"))
			{
				var xeList = XElement.Parse(dtoList.Xml);
				foreach (var objsur in xeList.XPathSelectElements("Possibilities/objsur"))
				{
					var xaGuid = objsur.Attribute("guid");
					Assert.IsNotNull(xaGuid, "objsur elements should always have a guid attribute.");
					var guid = xaGuid.Value;
					string name;
					DomainObjectDTO dtoPoss;
					Assert.IsTrue(repoDto.TryGetValue(guid, out dtoPoss), "Possibility Lists should point to valid objects.");
					VerifySubpossibilities(repoDto, dtoPoss);
				}
			}
		}

		private void VerifySubpossibilities(IDomainObjectDTORepository repoDto, DomainObjectDTO dtoPoss)
		{
			var xePoss = XElement.Parse(dtoPoss.Xml);
			foreach (var objsur in xePoss.XPathSelectElements("SubPossibilities/objsur"))
			{
				var xaGuid = objsur.Attribute("guid");
				Assert.IsNotNull(xaGuid, "objsur elements should always have a guid attribute.");
				var guid = xaGuid.Value;
				string name;
				DomainObjectDTO dtoSub;
				Assert.IsTrue(repoDto.TryGetValue(guid, out dtoSub), "SubPossibility Lists should point to valid objects.");
				VerifySubpossibilities(repoDto, dtoSub);
			}
		}

		/// <summary>
		/// Verify that all LexEntryRef objects point to valid LexEntryType objects.
		/// </summary>
		private void VerifyLexEntryRefs(IDomainObjectDTORepository repoDto)
		{
			foreach (var dtoRef in repoDto.AllInstancesWithSubclasses("LexEntryRef"))
			{
				var xeRef = XElement.Parse(dtoRef.Xml);
				foreach (var objsur in xeRef.XPathSelectElements("ComplexEntryTypes/objsur"))
				{
					var xaGuid = objsur.Attribute("guid");
					Assert.IsNotNull(xaGuid, "objsur elements should always have a guid attribute.");
					var guid = xaGuid.Value;
					DomainObjectDTO dtoType;
					Assert.IsTrue(repoDto.TryGetValue(guid, out dtoType), "LexEntryRef.ComplexEntryTypes should point to valid objects.");
				}
				foreach (var objsur in xeRef.XPathSelectElements("VariantEntryTypes/objsur"))
				{
					var xaGuid = objsur.Attribute("guid");
					Assert.IsNotNull(xaGuid, "objsur elements should always have a guid attribute.");
					var guid = xaGuid.Value;
					DomainObjectDTO dtoType;
					Assert.IsTrue(repoDto.TryGetValue(guid, out dtoType), "LexEntryRef.VariantEntryTypes should point to valid objects.");
				}
			}
		}

		/// <summary>
		/// Verify that a given LexEntryType exists, and that IsProtected is set to true.
		/// </summary>
		private void VerifyEntryType(IDomainObjectDTORepository repoDto, string guid)
		{
			DomainObjectDTO dto;
			Assert.IsTrue(repoDto.TryGetValue(guid, out dto),
				String.Format("Check for known guid of LexEntryType ({0}).", m_mapGuidToName[guid]));
			var xeType = XElement.Parse(dto.Xml);
			Assert.IsNotNull(xeType);
			var xeProt = xeType.XPathSelectElement("IsProtected");
			Assert.IsNotNull(xeProt,
				String.Format("IsProtected should exist ({0}).", m_mapGuidToName[guid]));
			var xaVal = xeProt.Attribute("val");
			Assert.IsNotNull(xaVal,
				String.Format("IsProtected should have a val attribute ({0}).", m_mapGuidToName[guid]));
			Assert.AreEqual("true", xaVal.Value.ToLowerInvariant(),
				String.Format("IsProtected should be true ({0}).", m_mapGuidToName[guid]));
		}

		/// <summary>
		/// Verify that every LexEntryType points back to a valid owner after migration, and that
		/// the owner points to the LexEntryType.
		/// </summary>
		/// <param name="repoDto"></param>
		private static void VerifyEntryTypeOwners(IDomainObjectDTORepository repoDto)
		{
			foreach (var dto in repoDto.AllInstancesWithSubclasses("LexEntryType"))
			{
				DomainObjectDTO dtoOwner;
				Assert.IsTrue(repoDto.TryGetOwner(dto.Guid, out dtoOwner), "All entry types should have valid owners!");
				DomainObjectDTO dtoOwnedOk = null;
				foreach (var dtoT in repoDto.GetDirectlyOwnedDTOs(dtoOwner.Guid))
				{
					if (dtoT == dto)
					{
						dtoOwnedOk = dtoT;
						break;
					}
				}
				Assert.AreEqual(dto, dtoOwnedOk, "The owner should own the entry type!");
			}
		}
	}
}
