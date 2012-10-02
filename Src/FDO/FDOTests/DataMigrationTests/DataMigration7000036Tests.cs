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
// File: DataMigrationTests7000036.cs
// Responsibility: FW team
// ---------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;
using System;
using SIL.FieldWorks.FDO.DomainImpl;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000035 to 7000036.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000036 : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000035 to 7000036:
		/// Add DateModified to StText
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000036Test()
		{
			var dtos = new HashSet<DomainObjectDTO>();
			var sb = new StringBuilder();
			// 1. Add barebones LP.
			sb.Append("<rt class=\"LangProject\" guid=\"9719A466-2240-4DEA-9722-9FE0746A30A6\">");
			sb.Append("<Texts>");
			StTextAndParaInfo lpTextsGuids = new StTextAndParaInfo("9719A466-2240-4DEA-9722-9FE0746A30A6", "Normal", false, false);
			sb.Append("<objsur guid=\"" + lpTextsGuids.textGuid + "\" t=\"o\" />");
			sb.Append("</Texts>");
			sb.Append("<TranslatedScripture>");
			sb.Append("<objsur guid=\"2c5c1f5f-1f08-41d7-99fe-23893ee4ceef\" t=\"o\" />");
			sb.Append("</TranslatedScripture>");
			sb.Append("</rt>");
			var lpDto = new DomainObjectDTO("9719A466-2240-4DEA-9722-9FE0746A30A6",
				"LangProject", sb.ToString());
			dtos.Add(lpDto);

			// Add text dto.
			var txtDto = new DomainObjectDTO(lpTextsGuids.textGuid.ToString(),  "StText",
				lpTextsGuids.textXml);
			dtos.Add(txtDto);
			// Add text para dto.
			var txtParaDto = new DomainObjectDTO(lpTextsGuids.paraGuid.ToString(), "ScrTxtPara",
				lpTextsGuids.paraXml);
			dtos.Add(txtParaDto);

			// 2. Add Scripture
			sb = new StringBuilder();
			sb.Append("<rt class=\"Scripture\" guid=\"2c5c1f5f-1f08-41d7-99fe-23893ee4ceef\" ownerguid=\"9719A466-2240-4DEA-9722-9FE0746A30A6\" owningflid=\"6001040\" owningord=\"0\">");
			sb.Append("<Books>");
			sb.Append("<objsur guid=\"f213db11-7007-4a2f-9b94-06d6c96014ca\" t=\"o\" />");
			sb.Append("</Books>");
			sb.Append("</rt>");
			var scrDto = new DomainObjectDTO("2c5c1f5f-1f08-41d7-99fe-23893ee4ceef", "Scripture",
				sb.ToString());
			dtos.Add(scrDto);

			// 3. Add a ScrBook
			sb = new StringBuilder();
			sb.Append("<rt class=\"ScrBook\" guid=\"f213db11-7007-4a2f-9b94-06d6c96014ca\" ownerguid=\"2c5c1f5f-1f08-41d7-99fe-23893ee4ceef\" owningflid=\"3001001\" owningord=\"0\">");
			sb.Append("<Name>");
			sb.Append("<AUni ws=\"fr\">Genesis</AUni>");
			sb.Append("</Name>");
			sb.Append("<Title>");
			StTextAndParaInfo titleTextGuids = new StTextAndParaInfo("f213db11-7007-4a2f-9b94-06d6c96014ca", "Title Main", true, false);
			sb.Append("<objsur guid=\"" + titleTextGuids.textGuid + "\" t=\"o\" />");
			sb.Append("</Title>");
			sb.Append("<Sections>");
			sb.Append("<objsur guid=\"834e1bf8-3a25-47d6-9f92-806b38b5f815\" t=\"o\" />");
			sb.Append("</Sections>");
			sb.Append("<Footnotes>");
			// This footnote should also have its ParaContainingOrc property set, but this test really doesn't care.
			StTextAndParaInfo footnoteGuids = new StTextAndParaInfo("ScrFootnote", "f213db11-7007-4a2f-9b94-06d6c96014ca", "Note General Paragraph", null, true, false);
			sb.Append("<objsur guid=\"" + footnoteGuids.textGuid + "\" t=\"o\" />");
			sb.Append("</Footnotes>");
			sb.Append("</rt>");
			var bookDto = new DomainObjectDTO("f213db11-7007-4a2f-9b94-06d6c96014ca", "ScrBook", sb.ToString());
			dtos.Add(bookDto);

			// Add title
			var titleDto = new DomainObjectDTO(titleTextGuids.textGuid.ToString(), "StText",
				titleTextGuids.textXml);
			dtos.Add(titleDto);
			// Title para
			var titleParaDto = new DomainObjectDTO(titleTextGuids.paraGuid.ToString(), "ScrTxtPara",
				titleTextGuids.paraXml);
			dtos.Add(titleParaDto);

			// Add footnote
			var footnoteDto = new DomainObjectDTO(footnoteGuids.textGuid.ToString(), "ScrFootnote",
				footnoteGuids.textXml);
			dtos.Add(footnoteDto);
			// Footnote para
			var footnoteParaDto = new DomainObjectDTO(footnoteGuids.paraGuid.ToString(), "ScrTxtPara",
				footnoteGuids.paraXml);
			dtos.Add(footnoteParaDto);

			// 4. Add a section to the book
			sb = new StringBuilder();
			sb.Append("<rt class=\"ScrSection\" guid=\"834e1bf8-3a25-47d6-9f92-806b38b5f815\" ownerguid=\"f213db11-7007-4a2f-9b94-06d6c96014ca\" owningflid=\"3002001\" owningord=\"0\">");
			sb.Append("<Content>");
			StTextAndParaInfo contentsTextGuids = new StTextAndParaInfo("StText", "834e1bf8-3a25-47d6-9f92-806b38b5f815", "Paragraph",
				"<Run ws=\"fr\" link=\"" + footnoteGuids.textGuid + "\"></Run>", true, false,
				"<Run ws=\"en\" link=\"" + footnoteGuids.textGuid + "\"></Run>");
			sb.Append("<objsur guid=\"" + contentsTextGuids.textGuid + "\" t=\"o\" />");
			sb.Append("</Content>");
			sb.Append("<Heading>");
			StTextAndParaInfo headingTextGuids = new StTextAndParaInfo("834e1bf8-3a25-47d6-9f92-806b38b5f815", "Section Head", true, false);
			sb.Append("<objsur guid=\"" + headingTextGuids.textGuid + "\" t=\"o\" />");
			sb.Append("</Heading>");
			sb.Append("</rt>");
			var sectionDto = new DomainObjectDTO("834e1bf8-3a25-47d6-9f92-806b38b5f815", "ScrSection",
				sb.ToString());
			dtos.Add(sectionDto);

			sb.Length = 0;
			sb.Append("<rt class=\"StJournalText\" guid=\"c1ecd177-e382-11de-8a39-0800200c9a66\">");
			sb.Append("<DateCreated val=\"2009-12-31 23:59:59.000\" />");
			sb.Append("<DateModified val=\"2010-01-01 23:59:59.000\" />");
			sb.Append("</rt>");
			var journalTextDto = new DomainObjectDTO("c1ecd177-e382-11de-8a39-0800200c9a66", "StJounalText",
				sb.ToString());
			dtos.Add(journalTextDto);

			// Add the contents
			var contentsDto = new DomainObjectDTO(contentsTextGuids.textGuid.ToString(), "StText",
				contentsTextGuids.textXml);
			dtos.Add(contentsDto);
			// Contents para
			var contentsParaDto = new DomainObjectDTO(contentsTextGuids.paraGuid.ToString(), "ScrTxtPara",
				contentsTextGuids.paraXml);
			dtos.Add(contentsParaDto);
			// BT of para
			var btDto = new DomainObjectDTO(contentsTextGuids.btGuid.ToString(), "CmTranslation",
				contentsTextGuids.btXml);
			dtos.Add(btDto);

			// Add the heading to the xml
			var headingDto = new DomainObjectDTO(headingTextGuids.textGuid.ToString(), "StText",
				headingTextGuids.textXml);
			dtos.Add(headingDto);
			// heading para
			var headingParaDto = new DomainObjectDTO(headingTextGuids.paraGuid.ToString(), "ScrTxtPara",
				headingTextGuids.paraXml);
			dtos.Add(headingParaDto);

			// Set up mock MDC.
			var mockMDC = new MockMDCForDataMigration();
			mockMDC.AddClass(1, "CmObject", null, new List<string> { "LangProject", "StText", "Scripture",
				"ScrBook", "StFootnote", "ScrSection", "StPara" });
			mockMDC.AddClass(2, "LangProject", "CmObject", new List<string>());
			mockMDC.AddClass(3, "StText", "CmObject", new List<string> { "StFootnote", "StJounalText" });
			mockMDC.AddClass(4, "Scripture", "CmObject", new List<string>());
			mockMDC.AddClass(5, "ScrBook", "CmObject", new List<string>());
			mockMDC.AddClass(6, "StFootnote", "StText", new List<string> { "ScrFootnote" });
			mockMDC.AddClass(7, "ScrSection", "CmObject", new List<string>());
			mockMDC.AddClass(8, "StTxtPara", "StPara", new List<string> { "ScrTxtPara" });
			mockMDC.AddClass(9, "ScrFootnote", "StFootnote", new List<string>());
			mockMDC.AddClass(10, "ScrTxtPara", "StTxtPara", new List<string>());
			mockMDC.AddClass(11, "StPara", "CmObject", new List<string> { "StTxtPara" });
			mockMDC.AddClass(12, "StJounalText", "StText", new List<string>());
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000035, dtos, mockMDC, null);

			DateTime beforeMigration = DateTime.Now.AddSeconds(-1); // avoid tick problem

			m_dataMigrationManager.PerformMigration(dtoRepos, 7000036);
			Assert.AreEqual(7000036, dtoRepos.CurrentModelVersion, "Wrong updated version.");

			int cJournalTexts = 0;
			foreach (DomainObjectDTO stTextDTO in dtoRepos.AllInstancesWithSubclasses("StText"))
			{
				XElement stText = XElement.Parse(stTextDTO.Xml);
				Assert.AreEqual(1, stText.Elements("DateModified").Count());
				XElement dateModified = stText.Element("DateModified");
				Assert.IsNotNull(dateModified);
				if (stTextDTO.Classname == "StJounalText")
				{
					Assert.AreEqual(new DateTime(2010, 1, 1, 23, 59, 59).ToLocalTime(), ReadWriteServices.LoadDateTime(dateModified));
					cJournalTexts++;
				}
				else
					Assert.GreaterOrEqual(ReadWriteServices.LoadDateTime(dateModified), beforeMigration);
			}
			Assert.AreEqual(1, cJournalTexts);
		}
	}
}