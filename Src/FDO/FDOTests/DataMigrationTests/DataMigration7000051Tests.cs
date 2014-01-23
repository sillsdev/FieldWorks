// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataMigrationTests7000047.cs
// Responsibility: FW team

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000050 to 7000051.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000051 : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000050 to 7000051.
		/// (Change Guid of LangProject.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000051Test()
		{
			var dtos = new HashSet<DomainObjectDTO>();
			var sb = new StringBuilder();
			// Add Lang Project dto.
			const string sLpGuid = "9719A466-2240-4DEA-9722-9FE0746A30A6";
			const string afxCatGuid = "60ab6c6c-43f3-4a7f-af61-96b4b77648a5";
			sb.AppendFormat("<rt class=\"LangProject\" guid=\"{0}\">", sLpGuid);
			sb.Append("<AffixCategories>");
			sb.AppendFormat("<objsur guid=\"{0}\" t=\"o\" />", afxCatGuid);
			sb.Append("</AffixCategories>");
			sb.Append("</rt>");
			var oldDto = new DomainObjectDTO(sLpGuid, "LangProject", sb.ToString());
			dtos.Add(oldDto);
			sb.Length = 0;

			sb.AppendFormat("<rt class=\"CmPossibilityList\" guid=\"{0}\"  ownerguid=\"{1}\" />", afxCatGuid, sLpGuid);
			var afxCatDto = new DomainObjectDTO(afxCatGuid, "CmPossibilityList", sb.ToString());
			dtos.Add(afxCatDto);

			// Set up mock MDC.
			var mockMDC = new MockMDCForDataMigration();
			mockMDC.AddClass(1, "CmObject", null, new List<string> { "LangProject", "CmPossibilityList" }); // Not true, but no matter.
			mockMDC.AddClass(2, "LangProject", "CmObject", new List<string>());
			mockMDC.AddClass(3, "CmPossibilityList", "CmObject", new List<string>());
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000050, dtos, mockMDC, null, FwDirectoryFinder.FdoDirectories);

			m_dataMigrationManager.PerformMigration(dtoRepos, 7000051, new DummyProgressDlg());
			Assert.AreEqual(7000051, dtoRepos.CurrentModelVersion, "Wrong updated version.");

			// Check that the old LP is not present.
			DomainObjectDTO gonerDto;
			Assert.IsFalse(dtoRepos.TryGetValue(sLpGuid, out gonerDto));
			Assert.IsTrue(((DomainObjectDtoRepository)dtoRepos).Goners.Contains(oldDto));
			var newDto = dtoRepos.AllInstancesSansSubclasses("LangProject").FirstOrDefault();
			Assert.IsNotNull(newDto);
			Assert.AreNotSame(oldDto, newDto);
			var newDtoGuid = newDto.Guid.ToLowerInvariant();
			Assert.AreNotEqual(sLpGuid.ToLowerInvariant(), newDtoGuid);

			// Check that ownerguid was changed on afxCatDto.
			var afxCatElm = XElement.Parse(afxCatDto.Xml);
			Assert.AreEqual(newDtoGuid, afxCatElm.Attribute("ownerguid").Value.ToLowerInvariant());
		}
	}
}