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
// File: DataMigrationTests7000047.cs
// Responsibility: FW team
// ---------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000046 to 7000047.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000047 : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000046 to 7000047.
		/// (Clean up legacy ChkRendering objects with null SurfaceForms)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000047Test()
		{
			var dtos = new HashSet<DomainObjectDTO>();
			var sb = new StringBuilder();
			// Add Lang Project dto.
			const string sLpGuid = "9719A466-2240-4DEA-9722-9FE0746A30A6";
			sb.Append("<rt class=\"LangProject\" guid=\"" + sLpGuid + "\">");
			sb.Append("<CheckLists>");
			sb.Append("<objsur guid=\"" + LangProjectTags.kguidChkKeyTermsList + "\" t=\"o\" />");
			sb.Append("</CheckLists>");
			sb.Append("</rt>");
			dtos.Add(new DomainObjectDTO(sLpGuid, "LangProject", sb.ToString()));
			sb.Length = 0;

			// Add Key Terms List dto.
			sb.Append("<rt class=\"CmPossibilityList\" guid=\"" + LangProjectTags.kguidChkKeyTermsList + "\" ownerguid=\"" + sLpGuid + "\">");
			sb.Append("<Possibilities>");
			sb.Append("<objsur t=\"o\" guid=\"27C32299-3B41-4FAD-A85C-F47657BCF95A\" />");
			sb.Append("<objsur t=\"o\" guid=\"5E3D9C56-404C-44C5-B3CB-99BF390E322E\" />");
			sb.Append("</Possibilities>");
			sb.Append("</rt>");
			dtos.Add(new DomainObjectDTO(LangProjectTags.kguidChkKeyTermsList.ToString(), "CmPossibilityList", sb.ToString()));
			sb.Length = 0;

			// Add Key Term 1 dto.
			sb.Append("<rt class=\"ChkTerm\" guid=\"22E6AF17-34BD-4433-BFDA-16C736E1F3F0\" ownerguid=\"" + LangProjectTags.kguidChkKeyTermsList + "\">");
			sb.Append("<Renderings>");
			sb.Append("<objsur t=\"o\" guid=\"27C32299-3B41-4FAD-A85C-F47657BCF95A\" />"); // bogus rendering 1
			sb.Append("<objsur t=\"o\" guid=\"5E3D9C56-404C-44C5-B3CB-99BF390E322E\" />"); // valid rendering
			sb.Append("</Renderings>");
			sb.Append("</rt>");
			DomainObjectDTO term1 = new DomainObjectDTO("22E6AF17-34BD-4433-BFDA-16C736E1F3F0", "ChkTerm", sb.ToString());
			dtos.Add(term1);
			sb.Length = 0;

			// Add Key Term 2 dto.
			sb.Append("<rt class=\"ChkTerm\" guid=\"B6C6C9B1-664A-4033-9937-DDA00C4000A7\" ownerguid=\"" + LangProjectTags.kguidChkKeyTermsList + "\">");
			sb.Append("<Renderings>");
			sb.Append("<objsur t=\"o\" guid=\"5FB86AAE-5E05-4d57-92B8-FFD0B67545CA\" />"); // bogus rendering 2
			sb.Append("<objsur t=\"o\" guid=\"B86AD2DF-98D0-4ec7-93DC-723D90A209EC\" />"); // bogus rendering 3
			sb.Append("</Renderings>");
			sb.Append("</rt>");
			DomainObjectDTO term2 = new DomainObjectDTO("B6C6C9B1-664A-4033-9937-DDA00C4000A7", "ChkTerm", sb.ToString());
			dtos.Add(term2);
			sb.Length = 0;

			// Add bogus ChkRendering 1 dto.
			DomainObjectDTO bogusRendering1 = new DomainObjectDTO("27C32299-3B41-4FAD-A85C-F47657BCF95A", "ChkRendering",
				"<rt class=\"ChkRendering\" guid=\"27C32299-3B41-4FAD-A85C-F47657BCF95A\" ownerguid=\"22E6AF17-34BD-4433-BFDA-16C736E1F3F0\"/>");
			dtos.Add(bogusRendering1);

			// Add valid ChkRendering dto.
			sb.Append("<rt class=\"ChkRendering\" guid=\"5E3D9C56-404C-44C5-B3CB-99BF390E322E\" ownerguid=\"22E6AF17-34BD-4433-BFDA-16C736E1F3F0\">");
			sb.Append("<SurfaceForm>");
			sb.Append("<objsur guid=\"BD8B2BE2-BDC7-476a-A627-5B59480A6490\" t=\"r\" />");
			sb.Append("</SurfaceForm>");
			sb.Append("</rt>");
			DomainObjectDTO validRendering = new DomainObjectDTO("5E3D9C56-404C-44C5-B3CB-99BF390E322E", "ChkRendering", sb.ToString());
			dtos.Add(validRendering);
			sb.Length = 0;

			// Add bogus ChkRendering 2 dto.
			DomainObjectDTO bogusRendering2 = new DomainObjectDTO("5FB86AAE-5E05-4d57-92B8-FFD0B67545CA", "ChkRendering",
				"<rt class=\"ChkRendering\" guid=\"5FB86AAE-5E05-4d57-92B8-FFD0B67545CA\" ownerguid=\"B6C6C9B1-664A-4033-9937-DDA00C4000A7\"/>");
			dtos.Add(bogusRendering2);

			// Add bogus ChkRendering 3 dto.
			DomainObjectDTO bogusRendering3 = new DomainObjectDTO("B86AD2DF-98D0-4ec7-93DC-723D90A209EC", "ChkRendering",
				"<rt class=\"ChkRendering\" guid=\"B86AD2DF-98D0-4ec7-93DC-723D90A209EC\" ownerguid=\"B6C6C9B1-664A-4033-9937-DDA00C4000A7\"/>");
			dtos.Add(bogusRendering3);

			// Add valid WfiWordform dto.
			sb.Append("<rt class=\"WfiWordform\" guid=\"BD8B2BE2-BDC7-476a-A627-5B59480A6490\" ownerguid=\"DC93551D-0DFB-48fd-8BF4-46FF9BF03BCD\">");
			sb.Append("<Form>");
			sb.Append("<AUni ws=\"es\">carro</AUni>");
			sb.Append("</Form>");
			sb.Append("</rt>");
			DomainObjectDTO wordForm = new DomainObjectDTO("BD8B2BE2-BDC7-476a-A627-5B59480A6490", "WfiWordform", sb.ToString());
			dtos.Add(wordForm);
			sb.Length = 0;

			// Set up mock MDC.
			var mockMDC = new MockMDCForDataMigration();
			mockMDC.AddClass(1, "CmObject", null, new List<string> { "LangProject", "CmPossibilityList", "CmPossibility",
				"CkRendering", "WfiWordform" });
			mockMDC.AddClass(2, "LangProject", "CmObject", new List<string>());
			mockMDC.AddClass(3, "CmPossibilityList", "CmObject", new List<string>());
			mockMDC.AddClass(4, "CmPossibility", "CmObject", new List<string> { "ChkTerm" });
			mockMDC.AddClass(5, "ChkTerm", "CmObject", new List<string>());
			mockMDC.AddClass(6, "CkRendering", "CmObject", new List<string>());
			mockMDC.AddClass(7, "WfiWordform", "CmObject", new List<string>());
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000046, dtos, mockMDC, null);

			m_dataMigrationManager.PerformMigration(dtoRepos, 7000047, new DummyProgressDlg());
			Assert.AreEqual(7000047, dtoRepos.CurrentModelVersion, "Wrong updated version.");

			// Check that ChkTerm references to the bogus ChkRenderings have been removed
			Assert.IsFalse(term1.Xml.Contains("<objsur t=\"o\" guid=\"27C32299-3B41-4FAD-A85C-F47657BCF95A\" />"));
			Assert.IsTrue(term1.Xml.Contains("<objsur t=\"o\" guid=\"5E3D9C56-404C-44C5-B3CB-99BF390E322E\" />"));
			Assert.IsFalse(term2.Xml.Contains("<objsur t=\"o\" guid=\"5FB86AAE-5E05-4d57-92B8-FFD0B67545CA\" />"));
			Assert.IsFalse(term2.Xml.Contains("<objsur t=\"o\" guid=\"B86AD2DF-98D0-4ec7-93DC-723D90A209EC\" />"));

			// Check that the bogus ChkRenderings have been removed
			Assert.IsFalse(dtos.Contains(bogusRendering1));
			Assert.IsTrue(dtos.Contains(validRendering));
			Assert.IsFalse(dtos.Contains(bogusRendering2));
			Assert.IsFalse(dtos.Contains(bogusRendering3));
		}
	}
}