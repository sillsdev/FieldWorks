// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000013 to 7000014.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000014 : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000013 to 7000014.
		/// (Clean up usage of CmAgentEvaluation.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000014Test()
		{
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000014_Evaluations.xml");

			var mockMDC = SetupMDC();

			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000013, dtos, mockMDC, null, FwDirectoryFinder.FdoDirectories);

			// SUT; Do the migration.
			m_dataMigrationManager.PerformMigration(dtoRepos, 7000014, new DummyProgressDlg());

			// Verification Phase
			Assert.AreEqual(7000014, dtoRepos.CurrentModelVersion, "Wrong updated version.");

			//
			var firstAgent = dtoRepos.GetDTO("c1ec8357-e382-11de-8a39-0800200c9a66");
			var secondAgent = dtoRepos.GetDTO("c1ec8357-e382-11de-8a39-0800200c9a67");
			var thirdAgent = dtoRepos.GetDTO("c1ec8357-e382-11de-8a39-0800200c9a68");
			var emptyAgent = dtoRepos.GetDTO("c1ec8357-e382-11de-8a39-0800200c9a6c");
			var firstAnalysis = dtoRepos.GetDTO("c84614e6-168b-4304-a025-0d6ae6573086");
			var secondAnalysis = dtoRepos.GetDTO("c84614e6-168b-4304-a025-0d6ae6573087");

			string firstApprovesGuid,
				   firstDisapprovesGuid,
				   secondApprovesGuid,
				   secondDisapprovesGuid,
					   thirdApprovesGuid,
				   thirdDisapprovesGuid,
			   emptyApprovesGuid,
				   emptyDisapprovesGuid;
			VerifyAgent(firstAgent, dtoRepos, out firstApprovesGuid, out firstDisapprovesGuid);
			VerifyAgent(secondAgent, dtoRepos, out secondApprovesGuid, out secondDisapprovesGuid);
			VerifyAgent(thirdAgent, dtoRepos, out thirdApprovesGuid, out thirdDisapprovesGuid);
			VerifyAgent(emptyAgent, dtoRepos, out emptyApprovesGuid, out emptyDisapprovesGuid);

			VerifyAnalysis(firstAnalysis, new string[] {firstApprovesGuid, secondDisapprovesGuid, thirdApprovesGuid});
			VerifyAnalysis(secondAnalysis, new string[] { firstApprovesGuid });

			var goners = ((DomainObjectDtoRepository)dtoRepos).Goners;
			Assert.AreEqual(4, goners.Count, "Wrong number removed.");
			var gonerGuids = new List<string>
								{
									("c84614e6-168b-4304-a025-0d6ae6573085").ToLower(),
									("c84614e6-168b-4304-a025-0d6ae6573088").ToLower(),
									("8151a002-a32e-476f-9c32-f95ee48fc71c").ToLower(),
									("c84614e6-168b-4304-a025-0d6ae657308a").ToLower()
								};
			foreach (var goner in goners)
				Assert.Contains(goner.Guid.ToLower(), gonerGuids, "Goner guid not found.");
		}

		private void VerifyAnalysis(DomainObjectDTO analysis, string[] evaluations)
		{
			var rtElement = XElement.Parse(analysis.Xml);
			var agentElt = rtElement.Element("WfiAnalysis");
			var evaluationsElt = agentElt.Element("Evaluations");
			Assert.IsNotNull(evaluationsElt);
			Assert.AreEqual(evaluations.Length, evaluationsElt.Elements().Count());
			var wanted = new HashSet<string>(evaluations);
			foreach (var objsur in evaluationsElt.Elements())
				VerifyReference(objsur, wanted);
		}

		private void VerifyReference(XElement objsur, HashSet<string> wanted)
		{
			Assert.AreEqual("objsur", objsur.Name.LocalName);
			Assert.AreEqual("r", objsur.Attribute("t").Value);
			var guid = objsur.Attribute("guid").Value;
			Assert.IsTrue(wanted.Contains(guid));
		}

		private void VerifyAgent(DomainObjectDTO agent, IDomainObjectDTORepository dtoRepos, out string approvesGuid, out string disapprovesGuid)
		{
			var rtElement = XElement.Parse(agent.Xml);
			var agentElement = rtElement.Element("CmAgent");
			//Assert.AreEqual(0, agentElement.Elements("Evaluations").Count(), "old evaluations should be deleted");
			Assert.IsNull(agentElement.Element("Evaluations"));
			var approves = agentElement.Element("Approves");
			VerifyAgentEvaluation(approves, dtoRepos, out approvesGuid);
			var disapproves = agentElement.Element("Disapproves");
			VerifyAgentEvaluation(disapproves, dtoRepos, out disapprovesGuid);
		}

		private void VerifyAgentEvaluation(XElement approves, IDomainObjectDTORepository dtoRepos, out string guid)
		{
			Assert.IsNotNull(approves);
			var objsur = approves.Element("objsur");
			Assert.IsNotNull(objsur);
			Assert.AreEqual("o",objsur.Attribute("t").Value);
			guid = objsur.Attribute("guid").Value;

			var eval = dtoRepos.GetDTO(guid);
			Assert.AreEqual("CmAgentEvaluation", eval.Classname);
			var evalElt = XElement.Parse(eval.Xml);

			Assert.IsNotNull(evalElt.Elements("CmObject").First());
			var agentEval = evalElt.Elements("CmAgentEvaluation").First();
			Assert.IsNotNull(agentEval);
			Assert.AreEqual(0, agentEval.Elements().Count());
		}


		private static MockMDCForDataMigration SetupMDC()
		{
			var mockMDC = new MockMDCForDataMigration();
			mockMDC.AddClass(1, "CmObject", null, new List<string> { "LangProject", "CmAgent", "WfiWordform",
				"WfiAnalysis", "CmAgentEvaluation" });
			mockMDC.AddClass(2, "CmAgent", "CmObject", new List<string>());
			mockMDC.AddClass(3, "LangProject", "CmObject", new List<string>());
			mockMDC.AddClass(4, "WfiWordform", "CmObject", new List<string>());
			mockMDC.AddClass(5, "WfiAnalysis", "CmObject", new List<string>());
			mockMDC.AddClass(6, "CmAgentEvaluation", "CmObject", new List<string>());
			return mockMDC;
		}
	}
}