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
// File: DataMigrationTests7000058.cs
// Responsibility: FW team
// ---------------------------------------------------------------------------------------------
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using NUnit.Framework;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000057 to 7000058.
	/// Test Data migration to change Irregularly Inflected Form variant types to class LexEntryInflType (for LT-7581).
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000058 : DataMigrationTestsBase
	{
		/// <summary>(Deprecated. See LexEntryInflTypeTags.) Plural Variant item in LexEntry Types list</summary>
		readonly string kguidLexTypPluralVar = LexEntryInflTypeTags.kguidLexTypPluralVar.ToString();
		/// <summary>(Deprecated. See LexEntryInflTypeTags.) Past Variant item in LexEntry Types list</summary>
		readonly string kguidLexTypPastVar = LexEntryInflTypeTags.kguidLexTypPastVar.ToString();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test filling the GlossAppend fields for the standard system variant types Plural and Past
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000058Test_FillEmptyGlossAppend()
		{
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000058_EmptyGlossAppend.xml");
			// Set up mock MDC.
			var mockMdc = new MockMDCForDataMigration();
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000057, dtos, mockMdc, null);

			m_dataMigrationManager.PerformMigration(dtoRepos, 7000058, new DummyProgressDlg());
			Assert.AreEqual(7000058, dtoRepos.CurrentModelVersion, "Wrong updated version.");

			// Fill GlossAppend for Irregularly Inflected Form Type (Plural)
			{
				var iifPlural = dtoRepos.GetDTO(kguidLexTypPluralVar);
				var iifNewPluralElt = XElement.Parse(iifPlural.Xml);
				Assert.That(iifNewPluralElt.XPathSelectElement("GlossAppend/AUni[@ws='en']").Value,
					Is.EqualTo(".pl"));
			}

			//  Fill GlossAppend for Irregularly Inflected Form Type (Past)
			{
				var iifPast = dtoRepos.GetDTO(kguidLexTypPastVar);
				var iifNewPastElt = XElement.Parse(iifPast.Xml);
				Assert.That(iifNewPastElt.XPathSelectElement("GlossAppend/AUni[@ws='en']").Value,
					Is.EqualTo(".pst"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test skipping the non-empty GlossAppend fields for the standard system variant types Plural and Past
		/// (Not likely that someone already filled this information in without a UI, but just to be sure.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000058Test_SkipNonEmptyGlossAppend()
		{
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000058_NonEmptyGlossAppend.xml");
			// Set up mock MDC.
			var mockMdc = new MockMDCForDataMigration();
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000057, dtos, mockMdc, null);

			m_dataMigrationManager.PerformMigration(dtoRepos, 7000058, new DummyProgressDlg());
			Assert.AreEqual(7000058, dtoRepos.CurrentModelVersion, "Wrong updated version.");

			// Skip GlossAppend for Irregularly Inflected Form Type (Plural)
			{
				var iifPlural = dtoRepos.GetDTO(kguidLexTypPluralVar);
				var iifNewPluralElt = XElement.Parse(iifPlural.Xml);
				Assert.That(iifNewPluralElt.XPathSelectElement("GlossAppend/AUni[@ws='en']").Value,
					Is.EqualTo(".pL"));
			}

			//  Skip GlossAppend for Irregularly Inflected Form Type (Past)
			{
				var iifPast = dtoRepos.GetDTO(kguidLexTypPastVar);
				var iifNewPastElt = XElement.Parse(iifPast.Xml);
				Assert.That(iifNewPastElt.XPathSelectElement("GlossAppend/AUni[@ws='en']").Value,
					Is.EqualTo(".pST"));
			}
		}
	}
}