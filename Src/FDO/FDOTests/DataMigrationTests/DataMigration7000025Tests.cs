using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using NUnit.Framework;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000024 to 7000025.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000025 : DataMigrationTestsBase
	{
		private readonly string _utcCreatedAsString;
		private readonly string _utcModifiedAsString;
		private readonly string _utcResolvedAsString;
		private readonly string _utcRunDateAsString;

		/// <summary>
		/// Constructor.
		/// </summary>
		public DataMigrationTests7000025()
		{
			// <DateCreated val="2009-12-31 23:59:59.000" /> All of these are the same for every object.
			// <DateModified val="2010-01-01 23:59:59.000" /> All of these are the same for every object.
			var asLocalCreated = new DateTime(2009, 12, 31, 23, 59, 59, 0);
			var asLocalModified = new DateTime(2010, 1, 1, 23, 59, 59, 0);
			var asUtcCreated = asLocalCreated.ToUniversalTime();
			_utcCreatedAsString = String.Format("{0}-{1}-{2} {3}:{4}:{5}.{6}",
											asUtcCreated.Year,
											asUtcCreated.Month,
											asUtcCreated.Day,
											asUtcCreated.Hour,
											asUtcCreated.Minute,
											asUtcCreated.Second,
											asUtcCreated.Millisecond);
			var asUtcModified = asLocalModified.ToUniversalTime();
			_utcModifiedAsString = String.Format("{0}-{1}-{2} {3}:{4}:{5}.{6}",
											asUtcModified.Year,
											asUtcModified.Month,
											asUtcModified.Day,
											asUtcModified.Hour,
											asUtcModified.Minute,
											asUtcModified.Second,
											asUtcModified.Millisecond);
			// <DateResolved val="2010-02-01 23:59:59.000" /> Used by ScrScriptureNote
			var asLocalResolved = new DateTime(2010, 2, 1, 23, 59, 59, 0);
			var asUtcResolved = asLocalResolved.ToUniversalTime();
			_utcResolvedAsString = String.Format("{0}-{1}-{2} {3}:{4}:{5}.{6}",
											asUtcResolved.Year,
											asUtcResolved.Month,
											asUtcResolved.Day,
											asUtcResolved.Hour,
											asUtcResolved.Minute,
											asUtcResolved.Second,
											asUtcResolved.Millisecond);
			_utcRunDateAsString = _utcCreatedAsString; // Just make run date the same as created in the test.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000024 to 7000025.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000025Test()
		{
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000025.xml");

			// Set up mock MDC.
			var mockMdc = new MockMDCForDataMigration();
			mockMdc.AddClass(1, "CmObject", null, new List<string>
													{
														"CmProject",
														"CmMajorObject",
														"RnGenericRec",
														"LexEntry",
														"CmAnnotation",
														"CmPossibility",
														"StJournalText", // Really a subclass of StText, but StText is not needed for the test.
														"ScrDraft",
														"ScrCheckRun"
													});
			mockMdc.AddClass(2, "CmProject", "CmObject", new List<string> {"LangProject"});
				mockMdc.AddClass(3, "LangProject", "CmProject", new List<string>());
			mockMdc.AddClass(4, "CmMajorObject", "CmObject", new List<string> {"CmPossibilityList"});
				mockMdc.AddClass(5, "CmPossibilityList", "CmMajorObject", new List<string>());
			mockMdc.AddClass(6, "CmPossibility", "CmObject", new List<string> { "PartOfSpeech" });
				mockMdc.AddClass(7, "PartOfSpeech", "CmPossibility", new List<string>());
			mockMdc.AddClass(8, "CmAnnotation", "CmObject", new List<string> { "CmBaseAnnotation" });
				mockMdc.AddClass(9, "CmBaseAnnotation", "CmAnnotation", new List<string> { "ScrScriptureNote" });
					mockMdc.AddClass(10, "ScrScriptureNote", "CmBaseAnnotation", new List<string>());
			mockMdc.AddClass(11, "StJournalText", "CmObject", new List<string>());
			mockMdc.AddClass(12, "LexEntry", "CmObject", new List<string>());
			mockMdc.AddClass(13, "RnGenericRec", "CmObject", new List<string>());
			mockMdc.AddClass(14, "ScrDraft", "CmObject", new List<string>());
			mockMdc.AddClass(15, "ScrCheckRun", "CmObject", new List<string>());
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000024, dtos, mockMdc, null);

			// SUT
			m_dataMigrationManager.PerformMigration(dtoRepos, 7000025);

			var dict = new Dictionary<string, string>
						{
							{_utcCreatedAsString, "DateCreated"},
							{_utcModifiedAsString, "DateModified"}
						};
			// Check what happened on LangProject (derives from CmProject, which defines the props)
			CheckMainObject(dtoRepos, "c1ecd171-e382-11de-8a39-0800200c9a66", dict);

			// Check what happened on CmPossibilityList (defines the props)
			CheckMainObject(dtoRepos, "c1ecd172-e382-11de-8a39-0800200c9a66", dict);

			// Check what happened on CmPossibility (defines the props)
			CheckMainObject(dtoRepos, "c1ecd173-e382-11de-8a39-0800200c9a66", dict);

			// Check what happened on PartOfSpeech (derives from CmPossibility)
			CheckMainObject(dtoRepos, "c1ecd174-e382-11de-8a39-0800200c9a66", dict);

			// Check what happened on CmBaseAnnotation (derives from CmAnnotation, which defines the props)
			CheckMainObject(dtoRepos, "c1ecd175-e382-11de-8a39-0800200c9a66", dict);

			// Check what happened on StJournalText (derives from StText, which defines the props)
			CheckMainObject(dtoRepos, "c1ecd177-e382-11de-8a39-0800200c9a66", dict);

			// Check what happened on LexEntry (defines the props)
			CheckMainObject(dtoRepos, "c1ecd178-e382-11de-8a39-0800200c9a66", dict);

			// Check what happened on RnGenericRec (defines the props)
			CheckMainObject(dtoRepos, "c1ecd179-e382-11de-8a39-0800200c9a66", dict);

			// Check what happened on ScrScriptureNote (derives from CmBaseAnnotation)
			dict.Add(_utcResolvedAsString, "DateResolved");
			CheckMainObject(dtoRepos, "c1ecd176-e382-11de-8a39-0800200c9a66", dict);
			dict.Remove(_utcResolvedAsString);

			// Check what happened on ScrDraft (defines the props)
			dict.Remove(_utcModifiedAsString);
			CheckMainObject(dtoRepos, "c1ecd17a-e382-11de-8a39-0800200c9a66", dict);

			// Check what happened on ScrCheckRun (defines the props)
			dict[_utcCreatedAsString] = "RunDate";
			CheckMainObject(dtoRepos, "c1ecd17b-e382-11de-8a39-0800200c9a66", dict);

			Assert.AreEqual(7000025, dtoRepos.CurrentModelVersion, "Wrong updated version.");
		}

		private static void CheckMainObject(IDomainObjectDTORepository dtoRepos, string guid, IEnumerable<KeyValuePair<string, string>> propertiesToCheck)
		{
			var dto = dtoRepos.GetDTO(guid);
			var rtElement = XElement.Parse(dto.Xml);
			foreach (var kvp in propertiesToCheck)
				CheckProperty(rtElement, kvp);
		}

		private static void CheckProperty(XContainer root, KeyValuePair<string, string> kvp)
		{
			Assert.AreEqual(kvp.Key, root.Element(kvp.Value).Attribute("val").Value);
		}
	}
}