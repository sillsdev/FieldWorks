// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000027 to 7000028.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000028 : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000025 to 7000026.
		/// (Lex entries are no longer owned)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000028Test()
		{
			//Bring in data from xml file.
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000028.xml");


			var mockMDC = new MockMDCForDataMigration();
			mockMDC.AddClass(1, "CmObject", null, new List<string> { "CmProject" });
			mockMDC.AddClass(2, "CmProject", "CmObject", new List<string> { "LangProject" });
			mockMDC.AddClass(3, "LangProject", "CmProject", new List<string>());
			mockMDC.AddClass(4, "LexDb", "CmObject", new List<string> ());
			mockMDC.AddClass(5, "LexEntry", "CmObject", new List<string>());

			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000027, dtos, mockMDC, null, FwDirectoryFinder.FdoDirectories);

			m_dataMigrationManager.PerformMigration(dtoRepos, 7000028, new DummyProgressDlg());

			// The Entries property of the LexDb should be gone.
			var lexDbDto = dtoRepos.AllInstancesSansSubclasses("LexDb").First();
			var lexDbElement = XElement.Parse(lexDbDto.Xml);
			var entriesElt = lexDbElement.Element("Entries");
			Assert.That(lexDbElement.Name.LocalName, Is.EqualTo("rt"));
			Assert.That(entriesElt, Is.Null);

			// LexEntries should no longer know owners.
			var entryDto = dtoRepos.AllInstancesSansSubclasses("LexEntry").First();
			var entryElt = XElement.Parse(entryDto.Xml);
			var ownerAttr = entryElt.Attribute("ownerguid");
			Assert.That(ownerAttr, Is.Null);

			Assert.AreEqual(7000028, dtoRepos.CurrentModelVersion, "Wrong updated version.");
		}
	}
}