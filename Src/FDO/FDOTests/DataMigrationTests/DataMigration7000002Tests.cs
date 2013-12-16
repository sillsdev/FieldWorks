// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataMigrationTests7000002.cs
// Responsibility: FW team

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000001 to 7000002.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000002 : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000001 to 7000002.
		/// (Change StTxtPara to ScrTxtPara when the paragraphs are owned by scripture)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000002Test()
		{
			var dtos = new HashSet<DomainObjectDTO>();
			var sb = new StringBuilder();
			// 1. Add barebones LP.
			sb.Append("<rt class=\"LangProject\" guid=\"9719A466-2240-4DEA-9722-9FE0746A30A6\">");
			sb.Append("<LangProject>");
			sb.Append("<Texts>");
			StTextAndParaInfo lpTextsGuids = new StTextAndParaInfo("9719A466-2240-4DEA-9722-9FE0746A30A6", "Normal");
			sb.Append("<objsur guid=\"" + lpTextsGuids.textGuid + "\" t=\"o\" />");
			sb.Append("</Texts>");
			sb.Append("<TranslatedScripture>");
			sb.Append("<objsur guid=\"2c5c1f5f-1f08-41d7-99fe-23893ee4ceef\" t=\"o\" />");
			sb.Append("</TranslatedScripture>");
			sb.Append("</LangProject>");
			sb.Append("</rt>");
			var lpDto = new DomainObjectDTO("9719A466-2240-4DEA-9722-9FE0746A30A6",
											"LangProject",
											sb.ToString());
			dtos.Add(lpDto);

			// Add text dto.
			var txtDto = new DomainObjectDTO(lpTextsGuids.textGuid.ToString(),
											 "StText",
											 lpTextsGuids.textXml);
			dtos.Add(txtDto);
			// Add text para dto.
			var txtParaDto = new DomainObjectDTO(lpTextsGuids.paraGuid.ToString(),
												 "StTxtPara",
												 lpTextsGuids.paraXml);
			dtos.Add(txtParaDto);

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
			sb.Append("<Title>");
			StTextAndParaInfo titleTextGuids = new StTextAndParaInfo("f213db11-7007-4a2f-9b94-06d6c96014ca", "Title Main");
			sb.Append("<objsur guid=\"" + titleTextGuids.textGuid + "\" t=\"o\" />");
			sb.Append("</Title>");
			sb.Append("<Sections>");
			sb.Append("<objsur guid=\"834e1bf8-3a25-47d6-9f92-806b38b5f815\" t=\"o\" />");
			sb.Append("</Sections>");
			sb.Append("<Footnotes>");
			StTextAndParaInfo footnoteGuids = new StTextAndParaInfo("StFootnote", "f213db11-7007-4a2f-9b94-06d6c96014ca", "Title Main");
			sb.Append("<objsur guid=\"" + footnoteGuids.textGuid + "\" t=\"o\" />");
			sb.Append("</Footnotes>");
			sb.Append("</ScrBook>");
			sb.Append("</rt>");
			var bookDto = new DomainObjectDTO("f213db11-7007-4a2f-9b94-06d6c96014ca",
											  "ScrBook",
											  sb.ToString());
			dtos.Add(bookDto);

			// Add title
			var titleDto = new DomainObjectDTO(titleTextGuids.textGuid.ToString(),
											   "StText",
											   titleTextGuids.textXml);
			dtos.Add(titleDto);
			// Title para
			var titleParaDto = new DomainObjectDTO(titleTextGuids.paraGuid.ToString(),
												   "StTxtPara",
												   titleTextGuids.paraXml);
			dtos.Add(titleParaDto);

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

			// 4. Add a section to the book
			sb = new StringBuilder();
			sb.Append("<rt class=\"ScrSection\" guid=\"834e1bf8-3a25-47d6-9f92-806b38b5f815\" ownerguid=\"f213db11-7007-4a2f-9b94-06d6c96014ca\" owningflid=\"3002001\" owningord=\"0\">");
			sb.Append("<CmObject />");
			sb.Append("<ScrSection>");
			sb.Append("<Content>");
			StTextAndParaInfo contentsTextGuids = new StTextAndParaInfo("834e1bf8-3a25-47d6-9f92-806b38b5f815", "Paragraph");
			sb.Append("<objsur guid=\"" + contentsTextGuids.textGuid + "\" t=\"o\" />");
			sb.Append("</Content>");
			sb.Append("<Heading>");
			StTextAndParaInfo headingTextGuids = new StTextAndParaInfo("834e1bf8-3a25-47d6-9f92-806b38b5f815", "Section Head");
			sb.Append("<objsur guid=\"" + headingTextGuids.textGuid + "\" t=\"o\" />");
			sb.Append("</Heading>");
			sb.Append("</ScrSection>");
			sb.Append("</rt>");
			var sectionDto = new DomainObjectDTO("834e1bf8-3a25-47d6-9f92-806b38b5f815",
												 "ScrSection",
												 sb.ToString());
			dtos.Add(sectionDto);

			// Add the contents
			var contentsDto = new DomainObjectDTO(contentsTextGuids.textGuid.ToString(),
												  "StText",
												  contentsTextGuids.textXml);
			dtos.Add(contentsDto);
			// Contents para
			var contentsParaDto = new DomainObjectDTO(contentsTextGuids.paraGuid.ToString(),
													  "StTxtPara",
													  contentsTextGuids.paraXml);
			dtos.Add(contentsParaDto);

			// Add the heading to the xml
			var headingDto = new DomainObjectDTO(headingTextGuids.textGuid.ToString(),
												 "StText",
												 headingTextGuids.textXml);
			dtos.Add(headingDto);
			// heading para
			var headingParaDto = new DomainObjectDTO(headingTextGuids.paraGuid.ToString(),
													 "StTxtPara",
													 headingTextGuids.paraXml);
			dtos.Add(headingParaDto);

			// Set up mock MDC.
			var mockMDC = new MockMDCForDataMigration();
			mockMDC.AddClass(1, "CmObject", null, new List<string> { "LangProject", "StText",
																	 "Scripture", "ScrBook", "StFootnote", "ScrSection", "StPara" });
			mockMDC.AddClass(2, "LangProject", "CmObject", new List<string>());
			mockMDC.AddClass(3, "StText", "CmObject", new List<string>());
			mockMDC.AddClass(4, "Scripture", "CmObject", new List<string>());
			mockMDC.AddClass(5, "ScrBook", "CmObject", new List<string>());
			mockMDC.AddClass(6, "StFootnote", "CmObject", new List<string> { "ScrFootnote" });
			mockMDC.AddClass(7, "ScrSection", "CmObject", new List<string>());
			mockMDC.AddClass(8, "StTxtPara", "CmObject", new List<string> { "ScrTxtPara" });
			mockMDC.AddClass(9, "ScrFootnote", "CmObject", new List<string>());
			mockMDC.AddClass(10, "ScrTxtPara", "CmObject", new List<string>());
			mockMDC.AddClass(11, "StPara", "CmObject", new List<string> { "StTxtPara" });
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000001, dtos, mockMDC, null);

			m_dataMigrationManager.PerformMigration(dtoRepos, 7000002, new DummyProgressDlg());
			Assert.AreEqual(7000002, dtoRepos.CurrentModelVersion, "Wrong updated version.");
			// Check the paragraphs to make sure they are the correct class
			Assert.AreEqual("StTxtPara", txtParaDto.Classname, "Oops. Class was changed.");
			XElement paraElement = XElement.Parse(txtParaDto.Xml);
			Assert.AreEqual("StTxtPara", paraElement.Attribute("class").Value, "Oops. Class was changed.");

			paraElement = XElement.Parse(titleParaDto.Xml);
			Assert.AreEqual("ScrTxtPara", titleParaDto.Classname, "Oops. Class was not changed.");
			Assert.AreEqual("ScrTxtPara", paraElement.Attribute("class").Value, "Oops. Class was not changed.");
			Assert.IsNotNull(paraElement.Element("ScrTxtPara"));
			Assert.IsTrue(paraElement.Element("ScrTxtPara").IsEmpty);
			Assert.AreEqual("ScrTxtPara", contentsParaDto.Classname, "Oops. Class was not changed.");
			Assert.AreEqual("ScrTxtPara", headingParaDto.Classname, "Oops. Class was not changed.");
			Assert.AreEqual("ScrTxtPara", footnoteParaDto.Classname, "Oops. Class was not changed.");
		}
	}

	#region StTextAndParaInfo class
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Creates and holds the guids and xml data for an StText and an StTxtPara
	/// </summary>
	/// ------------------------------------------------------------------------------------
	internal class StTextAndParaInfo
	{
		public readonly Guid textGuid = Guid.NewGuid();
		public readonly Guid paraGuid = Guid.NewGuid();
		public readonly Guid btGuid = Guid.NewGuid();
		public readonly string textXml;
		public readonly string paraXml;
		public readonly string btXml;

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="StTextAndParaInfo"/> class.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public StTextAndParaInfo(string ownerGuid, string styleName)
			: this("StText", ownerGuid, styleName)
		{
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="StTextAndParaInfo"/> class.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public StTextAndParaInfo(string ownerGuid, string styleName, bool paraIsForScripture,
			bool oldStyle) : this("StText", ownerGuid, styleName, null, paraIsForScripture, oldStyle)
		{
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="StTextAndParaInfo"/> class.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public StTextAndParaInfo(string stTextType, string ownerGuid, string styleName) :
			this (stTextType, ownerGuid, styleName, null, false, true)
		{
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="StTextAndParaInfo"/> class.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public StTextAndParaInfo(string stTextType, string ownerGuid, string styleName,
			string paraContentsXML, bool paraIsForScripture, bool oldStyle) :
			this(stTextType, ownerGuid, styleName, paraContentsXML, paraIsForScripture,
			oldStyle, null)
		{
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="StTextAndParaInfo"/> class.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public StTextAndParaInfo(string stTextType, string ownerGuid, string styleName,
			string paraContentsXML, bool paraIsForScripture, bool oldStyle, string BTContentsXML)
		{
			string paraType = paraIsForScripture ? "ScrTxtPara" : "StTxtPara";
			StringBuilder sb = new StringBuilder();

			sb.Append("<rt class=\"" + stTextType + "\" guid=\"" + textGuid + "\" ownerguid=\"" + ownerGuid + "\" owningflid=\"6001006\" owningord=\"1\">");
			if (oldStyle) sb.Append("<StText>");
			sb.Append("<Paragraphs>");
			sb.Append("<objsur guid=\"" + paraGuid + "\" t=\"o\" />");
			sb.Append("</Paragraphs>");
			if (oldStyle) sb.Append("</StText>");
			sb.Append("</rt>");
			textXml = sb.ToString();

			sb = new StringBuilder();
			sb.Append("<rt guid=\"" + paraGuid + "\" class=\"" + paraType + "\" ownerguid=\"" + textGuid + "\" owningflid=\"14001\" owningord=\"0\">");
			if (oldStyle) sb.Append("<CmObject />");
			if (oldStyle) sb.Append("<StPara>");
			sb.Append("<StyleRules><Prop namedStyle=\"" + styleName + "\"/>");
			sb.Append("</StyleRules>");
			if (oldStyle) sb.Append("</StPara>");
			if (oldStyle) sb.Append("<StTxtPara>");
			sb.Append("<Contents><Str>");
			if (!string.IsNullOrEmpty(paraContentsXML))
				sb.Append(paraContentsXML);
			else
				sb.Append("<Run ws=\"fr\"></Run>");
			sb.Append("</Str>");
			sb.Append("</Contents>");
			if (BTContentsXML != null)
			{
				sb.Append("<Translations>");
				sb.Append("<objsur t=\"o\" guid=\"" + btGuid + "\" />");
				sb.Append("</Translations>");
			}
			if (oldStyle) sb.Append("</StTxtPara>");
			sb.Append("</rt>");
			paraXml = sb.ToString();

			if (BTContentsXML != null)
			{
				sb = new StringBuilder();
				sb.Append("<rt class=\"CmTranslation\" guid=\"" + btGuid + "\" ownerguid=\"" + paraGuid + "\">");
				sb.Append("<Type>");
				sb.Append("<objsur t=\"r\" guid=\"" + LangProjectTags.kguidTranBackTranslation + "\" />");
				sb.Append("</Type>");
				sb.Append("<Translation>");
				sb.Append("<AStr ws=\"en\">");
				sb.Append(BTContentsXML);
				sb.Append("</AStr>");
				sb.Append("</Translation>");
				sb.Append("</rt>");
				btXml = sb.ToString();
			}
		}
	}
	#endregion
}