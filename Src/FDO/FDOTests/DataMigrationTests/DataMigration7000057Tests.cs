// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataMigrationTests7000057.cs
// Responsibility: FW team

using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000056 to 7000057.
	/// Test Data migration to change Irregularly Inflected Form variant types to class LexEntryInflType (for LT-7581).
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000057 : DataMigrationTestsBase
	{
		readonly string kguidLexTypIrregInflectionVar = LexEntryInflTypeTags.kguidLexTypIrregInflectionVar.ToString();
		/// <summary>(Deprecated. See LexEntryInflTypeTags.) Plural Variant item in LexEntry Types list</summary>
		readonly string kguidLexTypPluralVar = LexEntryInflTypeTags.kguidLexTypPluralVar.ToString();
		/// <summary>(Deprecated. See LexEntryInflTypeTags.) Past Variant item in LexEntry Types list</summary>
		readonly string kguidLexTypPastVar = LexEntryInflTypeTags.kguidLexTypPastVar.ToString();


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Normal situation where the user only has the Irregularly Inflected Form list items
		/// that come with NewLangProj.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000057Test_Normal()
		{
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000057_Normal.xml");
			// Set up mock MDC.
			var mockMdc = new MockMDCForDataMigration();
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000056, dtos, mockMdc, null, FwDirectoryFinder.FdoDirectories);

			m_dataMigrationManager.PerformMigration(dtoRepos, 7000057, new DummyProgressDlg());
			Assert.AreEqual(7000057, dtoRepos.CurrentModelVersion, "Wrong updated version.");

			// check that Irregularly Inflected Form Type has new class
			{
				var iifNew = dtoRepos.GetDTO(kguidLexTypIrregInflectionVar);
				Assert.That(iifNew.Classname, Is.EqualTo(LexEntryInflTypeTags.kClassName));
				var iifNewElt = XElement.Parse(iifNew.Xml);
				Assert.That(iifNewElt.XPathSelectElement("Name/AUni").Value,
					Is.EqualTo("Irregularly Inflected Form"));
			}

			// check that we haven't changed another owned object class
			{
				// Discussion for Irregularly Inflected Form Type
				var iifDiscussion = dtoRepos.GetDTO("b6f4c056-ea5e-11de-8a9c-0013722f8dec");
				Assert.That(iifDiscussion.Classname, Is.EqualTo(StTextTags.kClassName));
			}

			// check that Irregularly Inflected Form Type (Plural) has new class
			{
				var iifNewPlural = dtoRepos.GetDTO(kguidLexTypPluralVar);
				Assert.That(iifNewPlural, Is.Not.Null);
				Assert.That(iifNewPlural.Classname, Is.EqualTo(LexEntryInflTypeTags.kClassName));
				var iifNewPluralElt = XElement.Parse(iifNewPlural.Xml);
				Assert.That(iifNewPluralElt.XPathSelectElement("Name/AUni").Value,
					Is.EqualTo("Plural"));
			}

			// check that Irregularly Inflected Form Type (Past) has new class
			{
				var iifNewPast = dtoRepos.GetDTO(kguidLexTypPastVar);
				Assert.That(iifNewPast, Is.Not.Null);
				Assert.That(iifNewPast.Classname, Is.EqualTo(LexEntryInflTypeTags.kClassName));
				var iifNewPastElt = XElement.Parse(iifNewPast.Xml);
				Assert.That(iifNewPastElt.XPathSelectElement("Name/AUni").Value,
					Is.EqualTo("Past"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the special situation where the user has added Irregularly Inflected Form list items
		/// under the standard/system ones.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000057Test_SubInflTypes()
		{
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000057_SubInflTypes.xml");
			// Set up mock MDC.
			var mockMdc = new MockMDCForDataMigration();
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000056, dtos, mockMdc, null, FwDirectoryFinder.FdoDirectories);

			m_dataMigrationManager.PerformMigration(dtoRepos, 7000057, new DummyProgressDlg());
			Assert.AreEqual(7000057, dtoRepos.CurrentModelVersion, "Wrong updated version.");

			const string guidNonStandard =      "1df6f9da-7b69-44e4-b98d-13a0cee17b77";
			const string guidNonStandardChild = "208cc589-f10f-485b-bdcc-843f046d4146";
			const string guidPluralChild =      "2bd3437f-8bde-495c-a12b-d0c8feb7c3e9";
			const string guidPastChild =        "7c607ea9-c1a0-4b7f-aee7-06ead42ac299";

			// test that non-standard (user created) Irregularly Inflected Form has the right class
			{
				var nonStandard = dtoRepos.GetDTO(guidNonStandard);
				Assert.That(nonStandard.Classname, Is.EqualTo(LexEntryInflTypeTags.kClassName));
				var nonStandardElt = XElement.Parse(nonStandard.Xml);
				Assert.That(nonStandardElt.XPathSelectElement("Name/AUni").Value,
							Is.EqualTo("NonStandard"));
			}

			// test that non-standard child has correct class
			{
				var nonStandardChild = dtoRepos.GetDTO(guidNonStandardChild);
				Assert.That(nonStandardChild.Classname, Is.EqualTo(LexEntryInflTypeTags.kClassName));
				var nonStandardChildElt = XElement.Parse(nonStandardChild.Xml);
				Assert.That(nonStandardChildElt.XPathSelectElement("Name/AUni").Value,
							Is.EqualTo("NonStandardChild"));
			}

			// test that plural child has correct class
			{
				var varTypePluralChild = dtoRepos.GetDTO(guidPluralChild);
				Assert.That(varTypePluralChild.Classname, Is.EqualTo(LexEntryInflTypeTags.kClassName));
				var varTypePluralChildElt = XElement.Parse(varTypePluralChild.Xml);
				Assert.That(varTypePluralChildElt.XPathSelectElement("Name/AUni").Value,
							Is.EqualTo("PluralChild"));
			}

			// test that past child has correct class
			{
				var varTypePastChild = dtoRepos.GetDTO(guidPastChild);
				Assert.That(varTypePastChild.Classname, Is.EqualTo(LexEntryInflTypeTags.kClassName));
				var varTypePastChildElt = XElement.Parse(varTypePastChild.Xml);
				Assert.That(varTypePastChildElt.XPathSelectElement("Name/AUni").Value,
							Is.EqualTo("PastChild"));
			}
		}
	}
}