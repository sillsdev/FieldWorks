// Copyright (c) 2018-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.DisambiguateInFLExDB;
using SIL.FieldWorks;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;
using SIL.PrepFLExDB;
using SIL.WritingSystems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SIL.PrepFLExDBTests
{
	/// <summary>
	/// Tests related to Preparer.
	/// </summary>
	///
	[TestFixture]
	class PreparerTests : MemoryOnlyBackendProviderTestBase
	{
		LcmCache MyCache { get; set; }
		public ProjectId ProjId { get; set; }
		private List<FieldDescription> customFields;

		public override void FixtureSetup()
		{
			base.FixtureSetup();
			string basedir = Path.Combine(FwDirectoryFinder.SourceDirectory, "Utilities", "pcpatrflex", "PrepFLExDBDll");
			string testfile = Path.Combine(basedir, "PrepFLExDBTests", "TestData", "PCPATRTestingEmpty.fwdata");

			ProjId = new ProjectId(testfile);

			FwRegistryHelper.Initialize();
			FwUtils.InitializeIcu();
			var synchronizeInvoke = new SingleThreadedSynchronizeInvoke();

			var logger = new GenerateHCConfig.ConsoleLogger(synchronizeInvoke);
			var dirs = new DisambiguateInFLExDBTests.NullFdoDirectories();
			var settings = new LcmSettings { DisableDataMigration = true };
			var progress = new DisambiguateInFLExDBTests.NullThreadedProgress(synchronizeInvoke);
			MyCache = LcmCache.CreateCacheFromExistingData(ProjId, "en", logger, dirs, settings, progress);
		}

		/// <summary></summary>
		public override void FixtureTeardown()
		{
			//Directory.Delete(m_projectsDirectory, true);
			base.FixtureTeardown();
			MyCache.Dispose();
		}

		/// <summary>
		/// Test we get the expected results for the preparer service.
		/// </summary>
		[Test, Ignore("Ignoring this test for timing purposes")]
		public void PCPATRPreparerTest()
		{
			FixtureSetup();
			Assert.IsNotNull(MyCache);
			Assert.AreEqual(5, MyCache.LangProject.AllPartsOfSpeech.Count);
			Assert.AreEqual(0, MyCache.LangProject.LexDbOA.Entries.Count());
			var preparer = new Preparer(MyCache, false);

			// If we try to create the custom field before the master possibility list, the field is not created.
			customFields = preparer.GetListOfCustomFields();
			Assert.AreEqual(0, customFields.Count);
			preparer.AddPCPATRSenseCustomField();
			customFields = preparer.GetListOfCustomFields();
			Assert.AreEqual(0, customFields.Count);

			var possListRepository = MyCache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();
			Assert.AreEqual(34, possListRepository.AllInstances().Count());
			preparer.AddPCPATRList();
			CheckPossibilityList(possListRepository);
			// Invoke it again.  Only one copy should exist.
			preparer.AddPCPATRList();
			CheckPossibilityList(possListRepository);

			// Add custom field
			customFields = preparer.GetListOfCustomFields();
			Assert.AreEqual(0, customFields.Count);
			preparer.AddPCPATRSenseCustomField();
			customFields = preparer.GetListOfCustomFields();
			Assert.AreEqual(1, customFields.Count);
			var cf = customFields.Find(fd => fd.Name == PcPatrConstants.PcPatrFeatureDescriptorCustomField);
			Assert.IsNotNull(cf);
			Assert.IsTrue(cf.IsCustomField);
			Assert.AreEqual(PcPatrConstants.PcPatrFeatureDescriptorCustomField, cf.Name);
			Assert.AreEqual(PcPatrConstants.PcPatrFeatureDescriptorCustomField, cf.Userlabel);
			Assert.AreEqual(CellarPropertyType.ReferenceCollection, cf.Type);
			Assert.AreEqual(LexSenseTags.kClassId, cf.Class);
			Assert.AreEqual(CmCustomItemTags.kClassId, cf.DstCls);
			Assert.AreEqual(WritingSystemServices.kwsAnal, cf.WsSelector);
			// Invoke it again.  Only one should exist.
			preparer.AddPCPATRSenseCustomField();
			customFields = preparer.GetListOfCustomFields();
			Assert.AreEqual(1, customFields.Count);
			FixtureTeardown();
		}

		private void CheckPossibilityList(ICmPossibilityListRepository possListRepository)
		{
			Assert.AreEqual(35, possListRepository.AllInstances().Count());
			var pcPatrList = possListRepository.AllInstances().Last();
			Assert.AreEqual(PcPatrConstants.PcPatrFeatureDescriptorList, pcPatrList.Name.BestAnalysisAlternative.Text);
			Assert.AreEqual(665, pcPatrList.PossibilitiesOS.Count);
			CheckMatch(pcPatrList, "+root"); // first
			CheckMatch(pcPatrList, "causative_syntax"); // early middle
			CheckMatch(pcPatrList, "indefinite"); // late middle
			CheckMatch(pcPatrList, "witness"); // last
		}

		private void CheckMatch(ICmPossibilityList last, string sToMatch)
		{
			Assert.AreEqual(sToMatch, last.FindPossibilityByName(last.PossibilitiesOS, sToMatch, base.Cache.DefaultAnalWs).Name.BestAnalysisAlternative.Text);
		}

		/// <summary>
		/// Test we get the expected results for the preparer service.
		/// </summary>
		[Test, Ignore("Ignoring this test for timing purposes")]
		public void ToneParsPreparerTest()
		{
			FixtureSetup();
			Assert.IsNotNull(MyCache);
			Assert.AreEqual(5, MyCache.LangProject.AllPartsOfSpeech.Count);
			Assert.AreEqual(0, MyCache.LangProject.LexDbOA.Entries.Count());
			var preparer = new Preparer(MyCache, false);

			// If we try to create the custom field before the master possibility list, the field is not created.
			customFields = preparer.GetListOfCustomFields();
			Assert.AreEqual(0, customFields.Count);
			preparer.AddToneParsSenseCustomField();
			customFields = preparer.GetListOfCustomFields();
			Assert.AreEqual(0, customFields.Count);
			preparer.AddToneParsFormCustomField();
			customFields = preparer.GetListOfCustomFields();
			Assert.AreEqual(0, customFields.Count);

			var possListRepository = MyCache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();
			Assert.AreEqual(34, possListRepository.AllInstances().Count());
			preparer.AddToneParsList();
			ToneParsCheckPossibilityList(possListRepository);
			// Invoke it again.  Only one copy should exist.
			preparer.AddToneParsList();
			ToneParsCheckPossibilityList(possListRepository);

			// Add sense custom field
			customFields = preparer.GetListOfCustomFields();
			Assert.AreEqual(0, customFields.Count);
			preparer.AddToneParsSenseCustomField();
			customFields = preparer.GetListOfCustomFields();
			Assert.AreEqual(1, customFields.Count);
			var cf = customFields.Find(fd => fd.Name == ToneParsConstants.ToneParsPropertiesSenseCustomField);
			Assert.IsNotNull(cf);
			Assert.IsTrue(cf.IsCustomField);
			Assert.AreEqual(ToneParsConstants.ToneParsPropertiesSenseCustomField, cf.Name);
			Assert.AreEqual(ToneParsConstants.ToneParsPropertiesSenseCustomField, cf.Userlabel);
			Assert.AreEqual(CellarPropertyType.ReferenceCollection, cf.Type);
			Assert.AreEqual(LexSenseTags.kClassId, cf.Class);
			Assert.AreEqual(CmCustomItemTags.kClassId, cf.DstCls);
			Assert.AreEqual(WritingSystemServices.kwsAnal, cf.WsSelector);
			// Invoke it again.  Only one should exist.
			preparer.AddToneParsSenseCustomField();
			customFields = preparer.GetListOfCustomFields();
			Assert.AreEqual(1, customFields.Count);

			// Add form custom field
			customFields = preparer.GetListOfCustomFields();
			Assert.AreEqual(1, customFields.Count);
			preparer.AddToneParsFormCustomField();
			customFields = preparer.GetListOfCustomFields();
			Assert.AreEqual(2, customFields.Count);
			cf = customFields.Find(fd => fd.Name == ToneParsConstants.ToneParsPropertiesFormCustomField);
			Assert.IsNotNull(cf);
			Assert.IsTrue(cf.IsCustomField);
			Assert.AreEqual(ToneParsConstants.ToneParsPropertiesFormCustomField, cf.Name);
			Assert.AreEqual(ToneParsConstants.ToneParsPropertiesFormCustomField, cf.Userlabel);
			Assert.AreEqual(CellarPropertyType.ReferenceCollection, cf.Type);
			Assert.AreEqual(MoFormTags.kClassId, cf.Class);
			Assert.AreEqual(CmCustomItemTags.kClassId, cf.DstCls);
			Assert.AreEqual(WritingSystemServices.kwsAnal, cf.WsSelector);
			// Invoke it again.  Only two should still exist.
			preparer.AddToneParsFormCustomField();
			customFields = preparer.GetListOfCustomFields();
			Assert.AreEqual(2, customFields.Count);

			FixtureTeardown();
		}

		private void ToneParsCheckPossibilityList(ICmPossibilityListRepository possListRepository)
		{
			Assert.AreEqual(35, possListRepository.AllInstances().Count());
			var pcPatrList = possListRepository.AllInstances().Last();
			Assert.AreEqual(ToneParsConstants.ToneParsPropertiesList, pcPatrList.Name.BestAnalysisAlternative.Text);
			Assert.AreEqual(2, pcPatrList.PossibilitiesOS.Count);
			CheckMatch(pcPatrList, "sampleToneParsAllomorphProperty"); // first
			CheckMatch(pcPatrList, "sampleToneParsMorphemeProperty"); // last
		}
	}
}
