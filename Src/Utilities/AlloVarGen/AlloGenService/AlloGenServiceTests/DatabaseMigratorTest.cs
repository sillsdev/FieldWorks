// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SIL.AlloGenModel;
using SIL.AlloGenService;

namespace SIL.AlloGenServiceTest
{
	[TestFixture]
	class DatabaseMigratorTest : AlloGenTestBase
	{
		string AlloGenProduced { get; set; }
		DatabaseMigrator migrator = new DatabaseMigrator();

		[Test]
		public void Migrate03to04Test()
		{
			AlloGenFile = Path.Combine(TestDataDir, "AlloGenVersion03.xml");
			AlloGenExpected = Path.Combine(TestDataDir, "AlloGenVersion04.xml");
			migrator.LatestVersion = 4;
			Assert.AreEqual(4, migrator.LatestVersion);
			string newAlloGen = migrator.Migrate(AlloGenFile);
			using (var streamReader = new StreamReader(AlloGenExpected, Encoding.UTF8))
			{
				AlloGenExpected = streamReader.ReadToEnd().Replace("\r", "");
			}
			using (var streamReader = new StreamReader(newAlloGen, Encoding.UTF8))
			{
				AlloGenProduced = streamReader.ReadToEnd().Replace("\r", "");
			}
			Assert.AreEqual(AlloGenExpected, AlloGenProduced);
		}

		[Test]
		public void CreateFileNameTest()
		{
			string fileName = Path.Combine(TestDataDir, "AlloGenVersion01.xml");
			string backup = migrator.CreateBackupFileName(fileName);
			Assert.AreEqual(
				fileName.Substring(0, fileName.Length - 3),
				backup.Substring(0, backup.Length - 7) // we had to use .xml for commit, so skip that here.
			);
			Assert.AreEqual("bak", backup.Substring(backup.Length - 3));

			fileName = Path.Combine(TestDataDir, "AlloGenVersion01.myext");
			backup = migrator.CreateBackupFileName(fileName);
			Assert.AreEqual(
				fileName.Substring(0, fileName.Length),
				backup.Substring(0, backup.Length - 4)
			);
			Assert.AreEqual(".bak", backup.Substring(backup.Length - 4));
		}
	}
}
