// Copyright (c) 2012-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataMigration7000063Tests.cs
// Responsibility: lastufka

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000062 to 7000063.
	/// </summary>
	[TestFixture]
	public sealed class DataMigration7000063Tests : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000062 to 7000063.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000063Test()
		{
			var mockMdc = new MockMDCForDataMigration();
			mockMdc.AddClass(1, "CmObject", null, new List<string> { "LangProject" });
			mockMdc.AddClass(2, "LangProject", "CmObject", new List<string>());

			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000063.xml");
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000062, dtos, mockMdc, null, FwDirectoryFinder.FdoDirectories);

			m_dataMigrationManager.PerformMigration(dtoRepos, 7000063, new DummyProgressDlg());

			var wmbLangProjList = dtoRepos.AllInstancesSansSubclasses("LangProject");
			var wmbLangProj = wmbLangProjList.First();
			var wmbLangProjElt = XElement.Parse(wmbLangProj.Xml);
			// the homograph ws should have been added
			var homographWs = wmbLangProjElt.Element("HomographWs"); // has to be only one
			Assert.IsNotNull(homographWs, "Migration 7000063 failed to add HomographWs element to LangProject class");
			var homographWsUni = homographWs.Element("Uni"); // only one
			Assert.IsNotNull(homographWs, "Migration 7000063 failed to add HomographWs/Uni element to LangProject class");
			var homographWsTag = homographWsUni.Value; // only one
			Assert.AreEqual("fa", homographWsUni.Value, "HomographWs value in LangProject class is incorrect");

			Assert.AreEqual(7000063, dtoRepos.CurrentModelVersion, "Wrong updated version.");
		}
	}
}
