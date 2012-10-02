// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DataMigration7000018Tests.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using NUnit.Framework;

using SIL.FieldWorks.FDO.DomainServices.DataMigration;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for migration from version 7000017 to 7000018.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public sealed class DataMigrationTests7000018 : DataMigrationTestsBase
	{
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method PerformMigration.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void PerformMigration7000018()
		{
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000018.xml");

			var mockMdc = SetupMdc();

			IDomainObjectDTORepository repoDTO = new DomainObjectDtoRepository(7000017, dtos, mockMdc, null);

			// SUT: Do the migration.
			m_dataMigrationManager.PerformMigration(repoDTO, 7000018);

			// Verification Phase
			Assert.AreEqual(7000018, repoDTO.CurrentModelVersion, "Wrong updated version.");

			DomainObjectDTO dtoLP = null;
			foreach (DomainObjectDTO dto in repoDTO.AllInstancesSansSubclasses("LangProject"))
			{
				Assert.IsNull(dtoLP, "Only one Language Project object should exist");
				dtoLP = dto;
			}
			Assert.NotNull(dtoLP, "The Language Project object should exist");

			DomainObjectDTO dtoNtbk = null;
			foreach (DomainObjectDTO dto in repoDTO.AllInstancesSansSubclasses("RnResearchNbk"))
			{
				Assert.IsNull(dtoNtbk, "Only one Data Notebook object should exist");
				dtoNtbk = dto;
			}
			Assert.NotNull(dtoNtbk, "The Data Notebook object should exist");

			DomainObjectDTO dtoLexDb = null;
			foreach (DomainObjectDTO dto in repoDTO.AllInstancesSansSubclasses("LexDb"))
			{
				Assert.IsNull(dtoLexDb, "Only one Lexical Database object should exist");
				dtoLexDb = dto;
			}
			Assert.NotNull(dtoLexDb, "The Lexical Database object should exist");

			string sLexDbXml = dtoLexDb.Xml;
			Assert.False(sLexDbXml.Contains("<Styles>"), "The Styles field should be gone from the Lexical Database object");

			string sLpXml = dtoLP.Xml;
			Assert.True(sLpXml.Contains("<Styles>"), "The Styles field should still exist in the Language Project object");

			VerifyStylesRenamedOrDeleted(repoDTO);
			VerifyStyleReferencesUpdated(repoDTO);
			VerifyScriptureStylesUnchanged(repoDTO);
			VerifyNoDirectFormatting(repoDTO);
		}

		private void VerifyStylesRenamedOrDeleted(IDomainObjectDTORepository repoDTO)
		{
			int cHyperlink = 0;
			int cWrtSysAbbr = 0;
			int cExternalLink = 0;
			int cInternalLink = 0;
			int cLanguageCode = 0;
			int cStrong = 0;
			int cBuiltIn = 0;
			int cCustom = 0;
			foreach (DomainObjectDTO dto in repoDTO.AllInstancesSansSubclasses("StStyle"))
			{
				string sXml = dto.Xml;
				string sName = GetStyleName(sXml);
				DomainObjectDTO dtoOwner = repoDTO.GetOwningDTO(dto);
				if (dtoOwner.Classname != "LangProject")
				{
					Assert.AreEqual(dtoOwner.Classname, "Scripture", "Either LangProject or Scripture owns the style");
					if (sName == "External Link")
						++cExternalLink;
					else if (sName == "Internal Link")
						++cInternalLink;
					else if (sName == "Language Code")
						++cLanguageCode;
				}
				else
				{
					Assert.AreNotEqual(sName, "External Link", "The External Link style should no longer exist");
					Assert.AreNotEqual(sName, "Internal Link", "The Internal Link style should no longer exist");
					Assert.AreNotEqual(sName, "Language Code", "The Language Code style should no longer exist");
					if (sName == "Hyperlink")
						++cHyperlink;
					else if (sName == "Writing System Abbreviation")
						++cWrtSysAbbr;
					else if (sName == "Strong")
						++cStrong;
					if (sXml.Contains("<BasedOn>") || sXml.Contains("<Next>"))
					{
						XElement xeStyle = XElement.Parse(sXml);
						ValidateStyleReference(repoDTO, xeStyle, "BasedOn");
						ValidateStyleReference(repoDTO, xeStyle, "Next");
					}
					switch (sName)
					{
						case "Normal":
						case "Numbered List":
						case "Bulleted List":
						case "Heading 1":
						case "Heading 2":
						case "Heading 3":
						case "Block Quote":
						case "Title Text":
						case "Emphasized Text":
						case "Writing System Abbreviation":
						case "Added Text":
						case "Deleted Text":
						case "Hyperlink":
						case "Strong":
						case "Dictionary-Normal":
						case "Classified-MainEntry":
						case "Classified-Item":
							Assert.IsTrue(IsBuiltInStyle(sXml), sName + " should be marked as built-in");
							++cBuiltIn;
							break;
						default:
							// "Heading 4" and "Dictionary-Custom" should pass through here, plus 7 more
							// created from direct formatting.
							Assert.IsFalse(IsBuiltInStyle(sXml), sName + " should not be marked as built-in");
							++cCustom;
							break;
					}
				}
			}
			Assert.AreEqual(1, cHyperlink, "The Hyperlink style should exist (once)");
			Assert.AreEqual(1, cWrtSysAbbr, "The Writing System Abbreviation style should exist (once)");
			Assert.AreEqual(1, cStrong, "The Strong style should exist (once)");
			Assert.AreEqual(1, cExternalLink, "The External Link style should exist (once) in the Scripture stylesheet");
			Assert.AreEqual(1, cInternalLink, "The Internal Link style should exist (once) in the Scripture stylesheet");
			Assert.AreEqual(1, cLanguageCode, "The Language Code style should exist (once) in the Scripture stylesheet");
			Assert.AreEqual(17, cBuiltIn, "There should be 17 built-in LangProject styles.");
			Assert.AreEqual(8, cCustom, "There should be 8 custom LangProject styles.");
		}

		private bool IsBuiltInStyle(string sXml)
		{
			XElement xe = XElement.Parse(sXml);
			foreach (XElement xeIsBuiltIn in xe.Descendants("IsBuiltIn"))
			{
				XAttribute xaVal = xeIsBuiltIn.Attribute("val");
				if (xaVal != null)
					return xaVal.Value.ToLowerInvariant() == "true";
			}
			return false;
		}

		private void VerifyStyleReferencesUpdated(IDomainObjectDTORepository repoDTO)
		{
			int cHyperlink = 0;
			int cWrtSysAbbr = 0;
			int cHyperUsed = 0;
			int cWSAUsed = 0;
			foreach (DomainObjectDTO dto in repoDTO.AllInstancesSansSubclasses("StTxtPara"))
			{
				string sXml = dto.Xml;
				Assert.False(sXml.Contains(" namedStyle=\"External Link\""), "The External Link style should no longer be used");
				Assert.False(sXml.Contains(" namedStyle=\"Internal Link\""), "The Internal Link style should no longer be used");
				Assert.False(sXml.Contains(" namedStyle=\"Language Code\""), "The Language Code style should no longer be used");
				string sHyperlink = " namedStyle=\"Hyperlink\"";
				if (sXml.Contains(sHyperlink))
				{
					++cHyperlink;
					int idx = sXml.IndexOf(sHyperlink);
					while (idx > 0)
					{
						++cHyperUsed;
						idx = sXml.IndexOf(sHyperlink, idx + sHyperlink.Length);
					}
				}
				string sWSA = " namedStyle=\"Writing System Abbreviation\"";
				if (sXml.Contains(sWSA))
				{
					++cWrtSysAbbr;
					int idx = sXml.IndexOf(sWSA);
					while (idx > 0)
					{
						++cWSAUsed;
						idx = sXml.IndexOf(sWSA, idx + sWSA.Length);
					}
				}
			}
			Assert.AreEqual(3, cHyperlink, "The Hyperlink style should be used in three objects");
			Assert.AreEqual(4, cHyperUsed, "The Hyperlink style should be used four times");
			Assert.AreEqual(2, cWrtSysAbbr, "The Writing System Abbreviation style should be used in two objects");
			Assert.AreEqual(2, cWrtSysAbbr, "The Writing System Abbreviation style should be used twice");
		}

		private void VerifyScriptureStylesUnchanged(IDomainObjectDTORepository repoDTO)
		{
			int cExternal = 0;
			int cInternal = 0;
			int cLangCode = 0;
			foreach (DomainObjectDTO dto in repoDTO.AllInstancesSansSubclasses("ScrTxtPara"))
			{
				string sXml = dto.Xml;
				if (sXml.Contains(" namedStyle=\"External Link\""))
					++cExternal;
				if (sXml.Contains(" namedStyle=\"Internal Link\""))
					++cInternal;
				if (sXml.Contains(" namedStyle=\"Language Code\""))
					++cLangCode;
			}
			Assert.AreEqual(1, cExternal, "Scripture paragraphs retain External Link style reference");
			Assert.AreEqual(1, cInternal, "Scripture paragraphs retain Internal Link style reference");
			Assert.AreEqual(1, cLangCode, "Scripture paragraphs retain Language Code style reference");
		}

		private void ValidateStyleReference(IDomainObjectDTORepository repoDTO, XElement xeStyle, string sRefName)
		{
			XElement xeOnly = null;
			XElement xeObjsur = null;
			foreach (XElement xeRef in xeStyle.Descendants(sRefName))
			{
				Assert.IsNull(xeOnly, sRefName + " should appear only once in an StStyle object");
				xeOnly = xeRef;
				foreach (XElement xeLink in xeRef.Descendants("objsur"))
				{
					Assert.IsNull(xeObjsur, "objsur should appear only once in a " + sRefName + " field");
					xeObjsur = xeLink;
					XAttribute xa = xeObjsur.Attribute("guid");
					Assert.IsNotNull(xa, sRefName + "/objsur must have a guid attribute");
					DomainObjectDTO dto;
					Assert.IsTrue(repoDTO.TryGetValue(xa.Value, out dto), sRefName + " must point to a valid object");
					Assert.AreEqual("StStyle", dto.Classname, sRefName + " must point to a style");
					DomainObjectDTO dtoOwner;
					Assert.IsTrue(repoDTO.TryGetOwner(xa.Value, out dtoOwner), sRefName + " must point to a style with a valid owner");
					Assert.AreEqual("LangProject", dtoOwner.Classname, sRefName + " must point to a style owned by LangProject");
				}
				Assert.IsNotNull(xeObjsur, "objsur should appear once in a " + sRefName + " field");
			}
		}

		private void VerifyNoDirectFormatting(IDomainObjectDTORepository repoDTO)
		{
			byte[] rgbRun = Encoding.UTF8.GetBytes("<Run ");
			foreach (DomainObjectDTO dto in repoDTO.AllInstancesWithValidClasses())
			{
				if (dto.XmlBytes.IndexOfSubArray(rgbRun) <= 0)
					continue;
				XElement xeObj = XElement.Parse(dto.Xml);
				foreach (XElement xe in xeObj.Descendants("Run"))
				{
					foreach (XAttribute xa in xe.Attributes())
					{
						Assert.IsTrue(xa.Name.LocalName == "ws" ||
							xa.Name.LocalName == "namedStyle" ||
							xa.Name.LocalName == "externalLink",
							"only ws, namedStyle, and externalLink should exist as Run attributes in the test data");
					}
				}
			}
			byte[] rgbStyleRules = Encoding.UTF8.GetBytes("<StyleRules>");
			foreach (DomainObjectDTO dto in repoDTO.AllInstancesWithSubclasses("StTxtPara"))
			{
				if (dto.XmlBytes.IndexOfSubArray(rgbStyleRules) <= 0)
					continue;
				XElement xeObj = XElement.Parse(dto.Xml);
				foreach (XElement xe in xeObj.Descendants("Prop"))
				{
					foreach (XAttribute xa in xe.Attributes())
					{
						Assert.AreEqual("namedStyle", xa.Name.LocalName,
							"Direct formatting of paragraphs should not exist (" + xa.Name + ")");
					}
				}
			}
		}

		private string GetStyleName(string sXmlStyle)
		{
			int idxName = sXmlStyle.IndexOf("<Name>");
			int idxNameLim = sXmlStyle.IndexOf("</Name>") + 7;
			string sXmlName = sXmlStyle.Substring(idxName, idxNameLim - idxName);
			int idx = sXmlName.IndexOf("<Uni>") + 5;
			int idxLim = sXmlName.IndexOf("</Uni>");
			return sXmlName.Substring(idx, idxLim - idx);
		}

		private static MockMDCForDataMigration SetupMdc()
		{
			var mockMdc = new MockMDCForDataMigration();
			mockMdc.AddClass(0, "CmObject", null,
				new List<string> { "CmProject", "CmMajorObject", "RnGenericRec", "StStyle", "StText", "StPara" });
			mockMdc.AddClass(CmProjectTags.kClassId, "CmProject", "CmObject", new List<string> { "LangProject" });
			mockMdc.AddClass(CmMajorObjectTags.kClassId, "CmMajorObject", "CmObject",
				new List<string> { "RnResearchNbk", "LexDb" });
			mockMdc.AddClass(LangProjectTags.kClassId, "LangProject", "CmProject", new List<string>());
			mockMdc.AddClass(RnResearchNbkTags.kClassId, "RnResearchNbk", "CmMajorObject", new List<string>());
			mockMdc.AddClass(RnGenericRecTags.kClassId, "RnGenericRec", "CmObject", new List<string>());
			mockMdc.AddClass(LexDbTags.kClassId, "LexDb", "CmMajorObject", new List<string>());
			mockMdc.AddClass(StStyleTags.kClassId, "StStyle", "CmObject", new List<string>());
			mockMdc.AddClass(StTextTags.kClassId, "StText", "CmObject", new List<string>());
			mockMdc.AddClass(StParaTags.kClassId, "StPara", "CmObject", new List<string> { "StTxtPara" });
			mockMdc.AddClass(StTxtParaTags.kClassId, "StTxtPara", "StPara", new List<string>());

			return mockMdc;
		}
	}
}
