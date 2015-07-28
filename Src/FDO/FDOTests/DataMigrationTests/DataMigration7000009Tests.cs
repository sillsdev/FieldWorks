// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000008 to 7000009.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000009 : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000008 to 7000009.
		/// Make sure we get rid of some owningFlid attributes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000009Test()
		{
			var dtos = new HashSet<DomainObjectDTO>();
			var sb = new StringBuilder();
			// 1. Add barebones LP.
			sb.Append("<rt class=\"LangProject\" guid=\"9719A466-2240-4DEA-9722-9FE0746A30A6\">");
			sb.Append("<LangProject>");
			sb.Append("<Texts>");
			var lpTextsGuids = new StTextAndParaInfo("9719A466-2240-4DEA-9722-9FE0746A30A6", "Normal");
			sb.Append("<objsur guid=\"" + lpTextsGuids.textGuid + "\" t=\"o\" />");
			sb.Append("</Texts>");
			sb.Append("<TranslatedScripture>");
			sb.Append("<objsur guid=\"2c5c1f5f-1f08-41d7-99fe-23893ee4ceef\" t=\"o\" />");
			sb.Append("</TranslatedScripture>");
			sb.Append("</LangProject>");
			sb.Append("</rt>");
			var expectedLp = sb.ToString();
			var lpDto = new DomainObjectDTO("9719A466-2240-4DEA-9722-9FE0746A30A6",
											"LangProject",
											expectedLp);
			dtos.Add(lpDto);

			// 2. Add Scripture
			sb = new StringBuilder();
			sb.Append(
				"<rt class=\"Scripture\" guid=\"2c5c1f5f-1f08-41d7-99fe-23893ee4ceef\" ownerguid=\"9719A466-2240-4DEA-9722-9FE0746A30A6\"");
			int index = sb.Length;
			sb.Append(">");
			sb.Append("<Scripture>");
			sb.Append("<Books>");
			sb.Append("<objsur guid=\"f213db11-7007-4a2f-9b94-06d6c96014ca\" t=\"o\" />");
			sb.Append("</Books>");
			sb.Append("</Scripture>");
			sb.Append("</rt>");
			string expected = sb.ToString();
			sb.Insert(index, " owningflid=\"6001040\" owningord=\"0\"");

			var scrDto = new DomainObjectDTO("2c5c1f5f-1f08-41d7-99fe-23893ee4ceef",
											 "Scripture",
											 sb.ToString());
			dtos.Add(scrDto);
			// Set up mock MDC.
			var mockMDC = new MockMDCForDataMigration();
			mockMDC.AddClass(1, "CmObject", null, new List<string> { "LangProject", "Scripture" });
			mockMDC.AddClass(2, "LangProject", "CmObject", new List<string>());
			mockMDC.AddClass(4, "Scripture", "CmObject", new List<string>());
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000008, dtos, mockMDC, null, FwDirectoryFinder.FdoDirectories);

			m_dataMigrationManager.PerformMigration(dtoRepos, 7000009, new DummyProgressDlg());

			Assert.AreEqual(expected, scrDto.Xml);
			Assert.AreEqual(expectedLp, lpDto.Xml);

			Assert.AreEqual(7000009, dtoRepos.CurrentModelVersion, "Wrong updated version.");

		}
	}
}
