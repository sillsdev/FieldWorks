// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;
using SIL.WritingSystems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SIL.DisambiguateInFLExDBTests
{
	[TestFixture]
	abstract class DisambiguateTests : MemoryOnlyBackendProviderTestBase
	{
		protected string TestDataDir { get; set; }
		protected string SavedTestFile { get; set; }
		protected string TestFile { get; set; }
		protected string TempTestFile { get; set; }
		protected LcmCache MyCache { get; set; }
		public ProjectId ProjId { get; set; }
		const string kTestFile = "PCPATRTesting.fwdata";
		const string kTestFileB4 = "PCPATRTestingB4.fwdata";

		public override void FixtureSetup()
		{
			base.FixtureSetup();
			TestDirInit();
			if (String.IsNullOrEmpty(TestFile))
				TestFile = kTestFile;
			if (String.IsNullOrEmpty(SavedTestFile))
				SavedTestFile = kTestFileB4;

			var temp = new IO.TempFile(Path.Combine(Path.GetTempPath(), TestFile), false);
			TempTestFile = temp.Path;
			File.Copy(Path.Combine(TestDataDir, SavedTestFile), TempTestFile, true);
			ProjId = new ProjectId(TempTestFile);
			FwRegistryHelper.Initialize();
			FwUtils.InitializeIcu();
			var synchronizeInvoke = new SingleThreadedSynchronizeInvoke();

			var logger = new GenerateHCConfig.ConsoleLogger(synchronizeInvoke);
			var dirs = new NullFdoDirectories();
			var settings = new LcmSettings { DisableDataMigration = true };
			var progress = new NullThreadedProgress(synchronizeInvoke);
			MyCache = LcmCache.CreateCacheFromExistingData(ProjId, "en", logger, dirs, settings, progress);
		}

		protected void TestDirInit()
		{
			TestDataDir = Path.Combine(FwDirectoryFinder.SourceDirectory, "Utilities", "pcpatrflex", "DisambiguateInFLExDB", "DisambiguateInFLExDBTests", "TestData");
		}

		/// <summary></summary>
		public override void FixtureTeardown()
		{
			base.FixtureTeardown();
			if (MyCache != null)
			{
				ProjectLockingService.UnlockCurrentProject(MyCache);
			}
		}
	}
}
