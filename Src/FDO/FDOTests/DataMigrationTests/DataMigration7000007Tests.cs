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
	/// Test framework for migration from version 7000006 to 7000007.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000007 : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000006 to 7000007.
		/// 1) Remove ScrImportSet.ImportSettings
		/// 2) Remove StFootnote.DisplayFootnoteReference and DisplayFootnoteMarker
		/// 3) Remove StPara.StyleName
		/// /// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000007Test()
		{
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000007Tests.xml");

			// Set up mock MDC.
			var mockMDC = new MockMDCForDataMigration();
			mockMDC.AddClass(1, "CmObject", null, new List<string> { "ScrImportSet", "StPara", "StText"});
			mockMDC.AddClass(2, "ScrImportSet", "CmObject", new List<string>());
			mockMDC.AddClass(4, "StPara", "CmObject", new List<string> {"StTxtPara"});
			mockMDC.AddClass(6, "StText", "CmObject", new List<string> {"StFootnote"});
			mockMDC.AddClass(7, "StTxtPara", "StPara", new List<string>());
			mockMDC.AddClass(9, "StFootnote", "StText", new List<string> {"ScrFootnote"});
			mockMDC.AddClass(10, "ScrFootnote", "StFootnote", new List<string>());
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000006, dtos, mockMDC, null, FwDirectoryFinder.FdoDirectories);
			m_dataMigrationManager.PerformMigration(dtoRepos, 7000007, new DummyProgressDlg());

			var ScrInpDto = dtoRepos.AllInstancesSansSubclasses("ScrImportSet").First();
			var ScrInpElement = XElement.Parse(ScrInpDto.Xml);

			Assert.AreEqual(7000007, dtoRepos.CurrentModelVersion, "Wrong updated version.");

			Assert.IsNull(ScrInpElement.XPathSelectElement("ScrImportSet/ImportSettings"));
			// Get the ImportSettings Element in ScrImportSet (should be gone)
			Assert.IsNotNull(ScrInpElement.XPathSelectElement("ScrImportSet/ImportType"));
			// Get the ImportType Element in ScrImportSet.

			var ScrFootDto = dtoRepos.AllInstancesSansSubclasses("ScrFootnote").First();
			var ScrFootElement = XElement.Parse(ScrFootDto.Xml);
			Assert.IsNull(ScrFootElement.XPathSelectElement("StFootnote/DisplayFootnoteReference"));
			// Get the DisplayFootnoteReference Element in StFootnote (should be gone)
			Assert.IsNull(ScrFootElement.XPathSelectElement("StFootnote/DisplayFootnoteMarker"));
			// Get the DisplayFootnoteMarker Element in StFootnote (should be gone)

			var StTxtDto = dtoRepos.AllInstancesSansSubclasses("StTxtPara").First();
			var StTxtElement = XElement.Parse(StTxtDto.Xml);
			Assert.IsNotNull(StTxtElement.XPathSelectElement("StPara/StyleRules"));
			// Get the StyleRules Element in StPara
			Assert.IsNull(StTxtElement.XPathSelectElement("StPara/StyleName"));
			// Get the StyleName Element in StPara (should be gone)
		}
	}
}