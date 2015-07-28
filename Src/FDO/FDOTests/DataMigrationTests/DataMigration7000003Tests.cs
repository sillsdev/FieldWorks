// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000002 to 7000003.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000003 : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000002 to 7000003.
		/// (Change StFootnote to ScrFootnote)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000003Test()
		{
			var dtos = new HashSet<DomainObjectDTO>();
			var sb = new StringBuilder();
			// 1. Add barebones LP.
			sb.Append("<rt class=\"LangProject\" guid=\"9719A466-2240-4DEA-9722-9FE0746A30A6\">");
			sb.Append("<LangProject>");
			sb.Append("<TranslatedScripture>");
			sb.Append("<objsur guid=\"2c5c1f5f-1f08-41d7-99fe-23893ee4ceef\" t=\"o\" />");
			sb.Append("</TranslatedScripture>");
			sb.Append("</LangProject>");
			sb.Append("</rt>");
			var lpDto = new DomainObjectDTO("9719A466-2240-4DEA-9722-9FE0746A30A6",
											"LangProject",
											sb.ToString());
			dtos.Add(lpDto);

			// 2. Add Scripture
			sb = new StringBuilder();
			sb.Append("<rt class=\"Scripture\" guid=\"2c5c1f5f-1f08-41d7-99fe-23893ee4ceef\" ownerguid=\"9719A466-2240-4DEA-9722-9FE0746A30A6\" owningflid=\"6001040\" owningord=\"0\">");
			sb.Append("<Scripture>");
			sb.Append("<Books>");
			sb.Append("<objsur guid=\"f213db11-7007-4a2f-9b94-06d6c96014ca\" t=\"o\" />");
			sb.Append("</Books>");
			sb.Append("</Scripture>");
			sb.Append("</rt>");
			var scrDto = new DomainObjectDTO("2c5c1f5f-1f08-41d7-99fe-23893ee4ceef",
											 "Scripture",
											 sb.ToString());
			dtos.Add(scrDto);

			// 3. Add a ScrBook
			sb = new StringBuilder();
			sb.Append("<rt class=\"ScrBook\" guid=\"f213db11-7007-4a2f-9b94-06d6c96014ca\" ownerguid=\"2c5c1f5f-1f08-41d7-99fe-23893ee4ceef\" owningflid=\"3001001\" owningord=\"0\">");
			sb.Append("<ScrBook>");
			sb.Append("<Name>");
			sb.Append("<AUni ws=\"fr\">Genesis</AUni>");
			sb.Append("</Name>");
			sb.Append("<Footnotes>");
			var footnoteGuids = new StTextAndParaInfo("StFootnote", "f213db11-7007-4a2f-9b94-06d6c96014ca", "Title Main");
			sb.Append("<objsur guid=\"" + footnoteGuids.textGuid + "\" t=\"o\" />");
			sb.Append("</Footnotes>");
			sb.Append("</ScrBook>");
			sb.Append("</rt>");
			var bookDto = new DomainObjectDTO("f213db11-7007-4a2f-9b94-06d6c96014ca",
											  "ScrBook",
											  sb.ToString());
			dtos.Add(bookDto);
			// Add footnote
			var footnoteDto = new DomainObjectDTO(footnoteGuids.textGuid.ToString(),
												  "StFootnote",
												  footnoteGuids.textXml);
			dtos.Add(footnoteDto);
			// Footnote para
			var footnoteParaDto = new DomainObjectDTO(footnoteGuids.paraGuid.ToString(),
													  "StTxtPara",
													  footnoteGuids.paraXml);
			dtos.Add(footnoteParaDto);

			// Set up mock MDC.
			var mockMDC = new MockMDCForDataMigration();
			mockMDC.AddClass(1, "CmObject", null, new List<string> { "LangProject", "StText",
																	 "Scripture", "ScrBook", "StPara" });
			mockMDC.AddClass(2, "LangProject", "CmObject", new List<string>());
			mockMDC.AddClass(3, "StText", "CmObject", new List<string> { "StFootnote" });
			mockMDC.AddClass(4, "Scripture", "CmObject", new List<string>());
			mockMDC.AddClass(5, "ScrBook", "CmObject", new List<string>());
			mockMDC.AddClass(6, "StFootnote", "CmObject", new List<string> { "ScrFootnote" });
			mockMDC.AddClass(8, "StTxtPara", "CmObject", new List<string> { "ScrTxtPara" });
			mockMDC.AddClass(9, "ScrFootnote", "CmObject", new List<string>());
			mockMDC.AddClass(10, "ScrTxtPara", "CmObject", new List<string>());
			mockMDC.AddClass(11, "StPara", "CmObject", new List<string> { "StTxtPara" });
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000002, dtos, mockMDC, null, FwDirectoryFinder.FdoDirectories);
			m_dataMigrationManager.PerformMigration(dtoRepos, 7000003, new DummyProgressDlg());
			Assert.AreEqual(7000003, dtoRepos.CurrentModelVersion, "Wrong updated version.");

			// Check the footnote to make sure it is the correct class
			XElement footnoteElement = XElement.Parse(footnoteDto.Xml);
			Assert.AreEqual("ScrFootnote", footnoteDto.Classname, "Oops. Class was not changed.");
			Assert.AreEqual("ScrFootnote", footnoteElement.Attribute("class").Value, "Oops. Class was not changed.");
			Assert.IsNotNull(footnoteElement.Element("ScrFootnote"));
			Assert.IsTrue(footnoteElement.Element("ScrFootnote").IsEmpty);
		}
	}
}