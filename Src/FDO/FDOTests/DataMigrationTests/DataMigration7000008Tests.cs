using System.Collections.Generic;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000007 to 7000008.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000008 : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000007 to 7000008.
		/// 1) Remove orphaned CmBaseAnnotations, as per FWR-98:
		/// "Since we won't try to reuse wfic or segment annotations that no longer have
		/// BeginObject point to a paragraph, we should remove (ignore) these annotations
		/// when migrating an old database (FW 6.0 or older) into the new architecture"
		/// /// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000008Test()
		{
			// Add at least one ScrScriptureNote which has a null BeginObject prop.
			// This ScrScriptureNote should not be removed in this migration.

			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000008Tests.xml");

			// Set up mock MDC.
			var mockMDC = new MockMDCForDataMigration();
			mockMDC.AddClass(1, "CmObject", null, new List<string> { "CmBaseAnnotation", "LangProject" });
			mockMDC.AddClass(2, "LangProject", "CmObject", new List<string>());
			mockMDC.AddClass(3, "CmBaseAnnotation", "CmObject", new List<string> { "ScrScriptureNote" });
			mockMDC.AddClass(4, "ScrScriptureNote", "CmBaseAnnotation", new List<string>());
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000007, dtos, mockMDC, null, FwDirectoryFinder.FdoDirectories);

			m_dataMigrationManager.PerformMigration(dtoRepos, 7000008, new DummyProgressDlg());

			var goners = ((DomainObjectDtoRepository) dtoRepos).Goners;
			Assert.AreEqual(4, goners.Count, "Wrong number removed.");
			var gonerGuids = new List<string>
								{
									("54E4A881-23D7-48FC-BD05-14DD0CA86D5B").ToLower(), // Defective Discourse Chart ann.
									("22a8431f-f974-412f-a261-8bd1a4e1be1b").ToLower(),
									("155B8419-0A9B-44A4-A960-F78983C84768").ToLower(),
									("84FC5548-8AB2-4AA0-AFE5-72F64F567982").ToLower()
								};
			foreach (var goner in goners)
				Assert.Contains(goner.Guid.ToLower(), gonerGuids, "Goner guid not found.");
		}
	}
}