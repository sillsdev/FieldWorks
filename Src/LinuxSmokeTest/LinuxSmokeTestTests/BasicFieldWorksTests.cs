// Copyright (c) 2010, SIL International. All Rights Reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// Original author: Tom Hindle 2010-12-30

using System;
using System.IO;
using System.Threading;
using LinuxSmokeTest;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;

namespace LinuxSmokeTestTests
{
	[TestFixture()]
	public class BasicFieldWorksTests
	{
		internal string OriginalTestDataPath
		{
			get { return Path.Combine(DirectoryFinder.FwSourceDirectory, "LinuxSmokeTest/LinuxSmokeTestTests/TestData/"); }
		}

		internal string Sena3TestData
		{
			get; set;
		}

		[SetUp]
		public void CopyData()
		{
			// allow us to use DirectoryFinder from unittests.
			RegistryHelper.CompanyName = "Sil";
			RegistryHelper.ProductName = "FieldWorks";

			string Sena3 = "Sena 3.fwdata";
			string newPath = Path.GetTempPath();
			Sena3TestData = Path.Combine(newPath, Sena3);
			File.Copy(Path.Combine(OriginalTestDataPath, Sena3), Sena3TestData);
			File.SetAttributes(Sena3TestData, FileAttributes.Normal);
		}

		[TearDown]
		public void DeleteData()
		{
			File.Delete(Sena3TestData);
		}

		[Test()]
		public void StartTe_UsingSena3_TeShouldStartup()
		{
			using(var helper = new LinuxSmokeTestHelper())
			{
				helper.App = "TE";
				helper.Db = Sena3TestData;

				helper.Start();

				// wait for Fw to start.
				Thread.Sleep(TimeSpan.FromSeconds(40));

				Assert.AreEqual("TeMainWnd", helper.GetMainApplicationFormName());

				helper.CloseFieldWorks();

				// wait for exit
				Thread.Sleep(TimeSpan.FromSeconds(40));
			}
		}

		[Test()]
		public void StartFlex_UsingSena3_FlexShouldStartup()
		{
			using(var helper = new LinuxSmokeTestHelper())
			{
				helper.App = "Flex";
				helper.Db = Sena3TestData;

				helper.Start();

				// wait for Fw to start
				Thread.Sleep(TimeSpan.FromSeconds(40));

				Assert.AreEqual("XWindow", helper.GetMainApplicationFormName());

				helper.CloseFieldWorks();

				// wait for exit
				Thread.Sleep(TimeSpan.FromSeconds(40));
			}
		}
	}
}
