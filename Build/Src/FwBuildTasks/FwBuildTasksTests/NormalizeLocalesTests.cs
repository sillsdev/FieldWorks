// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using FwBuildTasks;
using NUnit.Framework;

namespace SIL.FieldWorks.Build.Tasks.FwBuildTasksTests
{
	[TestFixture]
	public class NormalizeLocalesTests
	{
		private TestBuildEngine _tbi;
		private NormalizeLocales _task;
		private string _testDir;

		[SetUp]
		public void TestSetup()
		{
			_tbi = new TestBuildEngine();
			_testDir = Path.Combine(Path.GetTempPath(), GetType().Name);
			_task = new NormalizeLocales { BuildEngine = _tbi, L10nsDirectory = _testDir };

			if(Directory.Exists(_testDir))
			{
				TestUtilities.TestUtilities.DeleteFolderThatMayBeInUse(_testDir);
			}
			Directory.CreateDirectory(_testDir);
		}

		[TearDown]
		public void TestTeardown()
		{
			TestUtilities.TestUtilities.DeleteFolderThatMayBeInUse(_testDir);
		}

		[Test]
		public void DoesntCrashWhenNoNameChange()
		{
			FileSystemSetup(new[] {"de", "en-US"});

			_task.Execute();

			// Verify that the already-normalized locale is still normalized
			VerifyLocale("de", "never-existed");
			// Verify that the locale that needed to be normalized has been
			VerifyLocale("en", "en-US");
		}

		[Test]
		public void Works()
		{
			FileSystemSetup(new[] {"de-DE", "en-US", "zh-CN"});

			_task.Execute();

			// Verify that normal languages have no country codes
			VerifyLocale("de", "de-DE");
			VerifyLocale("en", "en-US");

			// Verify that Chinese has the country code and that there is no regionless Chinese.
			VerifyLocale("zh-CN", "zh");
		}

		private void FileSystemSetup(string[] locales)
		{
			foreach (var locale in locales)
			{
				var localeDir = Path.Combine(_testDir, locale);
				Directory.CreateDirectory(localeDir);
				File.WriteAllText(Path.Combine(localeDir, $"strings-{locale}.xml"), "some strings");
				var projectDir = Path.Combine(localeDir, "someProject");
				Directory.CreateDirectory(projectDir);
				File.WriteAllText(Path.Combine(projectDir, $"SomeFile.{locale}.resx"), "contents");
			}
		}

		private void VerifyLocale(string expected, string not)
		{
			var localeDir = Path.Combine(_testDir, expected);
			AssertDirExists(localeDir);
			AssertDirDoesNotExist(Path.Combine(_testDir, not));

			AssertFileExists(Path.Combine(localeDir, $"strings-{expected}.xml"));
			AssertFileDoesNotExist(Path.Combine(localeDir, $"strings-{not}.xml"));
			AssertFileExists(Path.Combine(localeDir, "someProject", $"SomeFile.{expected}.resx"));
			AssertFileDoesNotExist(Path.Combine(localeDir, "someProject", $"SomeFile.{not}.resx"));
		}

		private static void AssertDirExists(string path)
		{
			Assert.That(Directory.Exists(path), $"Expected the directory {path} to exist, but it did not.");
		}

		private static void AssertDirDoesNotExist(string path)
		{
			Assert.That(!Directory.Exists(path), $"Expected the directory {path} not to exist, but it did.");
		}

		private static void AssertFileExists(string path)
		{
			Assert.That(File.Exists(path), $"Expected the file {path} to exist, but it did not.");
		}

		private static void AssertFileDoesNotExist(string path)
		{
			Assert.That(!File.Exists(path), $"Expected the file {path} not to exist, but it did.");
		}
	}
}
