// Copyright (c) 2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.IO;
using FwBuildTasks;
using Newtonsoft.Json;
using NUnit.Framework;
using SIL.FieldWorks.Build.Tasks.Localization;

namespace SIL.FieldWorks.Build.Tasks.FwBuildTasksTests
{
	[TestFixture]
	public class MoveResxsToLocaleSrcTests
	{
		private const string Locale = "lws";

		private TestBuildEngine _tbi;
		private MoveResxsToLocaleSrc _sut;
		private DirectoryInfo _rootPath;
		private DirectoryInfo _localeDir;
		private string _crowdinJsonPath;

		[SetUp]
		public void Setup()
		{
			_rootPath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "TestRoot4MoveResx2LocSrc"));
			TaskTestUtils.RecreateDirectory(_rootPath.FullName);
			_localeDir = _rootPath.CreateSubdirectory($"Localizations4{Locale}");
			_crowdinJsonPath = Path.Combine(_rootPath.FullName, "crowdin.json");
			_sut = new MoveResxsToLocaleSrc
			{
				BuildEngine = _tbi = new TestBuildEngine(),
				LocaleDirectory = _localeDir.FullName,
				CrowdinJson = _crowdinJsonPath
			};
		}

		[TearDown]
		public void TearDown()
		{
			DeleteRootDir();
		}

		private void DeleteRootDir()
		{
			if (Directory.Exists(_rootPath.FullName))
				Directory.Delete(_rootPath.FullName, true);
		}

		[Test]
		public void MovesResxs([Values(null, "FW-9.8")] string branch)
		{
			FullSetup(branch);

			Assert.That(_sut.Execute());
			Assert.That(_tbi.Warnings, Is.Empty);
			Assert.That(_tbi.Errors, Is.Empty);

			TaskTestUtils.AssertFileExists(Path.Combine(_localeDir.FullName, "Src", "xWorks", "strings.resx"));
			TaskTestUtils.AssertFileExists(Path.Combine(_localeDir.FullName, "Src", "root.resx"));
			TaskTestUtils.AssertFileExists(Path.Combine(_localeDir.FullName, "src", "SIL.LCModel", $"Strings.{Locale}.resx"));
			TaskTestUtils.AssertFileExists(Path.Combine(_localeDir.FullName, "src", "ruth.resx"));
		}

		[Test]
		public void MissingFwDir_LogsError([Values(null, "FW-8")] string branch)
		{
			FullSetup(branch);
			GetFullBranchDir(branch).CreateSubdirectory(MoveResxsToLocaleSrc.FwDirInCrowdin).Delete(true);

			Assert.That(_sut.Execute(), Is.False);

			TaskTestUtils.AssertContainsExpectedSubstrings(_tbi.Errors[0], "Localization directory not found ", Locale, branch);
		}

		[Test]
		public void MissingFwResxs_LogsError([Values(null, "FW-9.1")] string branch)
		{
			FullSetup(branch);
			foreach (var resx in GetFullBranchDir(branch).CreateSubdirectory(MoveResxsToLocaleSrc.FwDirInCrowdin)
				.EnumerateFiles("*.resx", SearchOption.AllDirectories))
			{
				resx.Delete();
			}

			Assert.That(_sut.Execute(), Is.False);

			TaskTestUtils.AssertContainsExpectedSubstrings(_tbi.Errors[0], "No localized resx files found in ", Locale, branch);
		}

		[Test]
		public void MissingLcmDir_LogsError([Values(null, "FW-10")] string branch)
		{
			FullSetup(branch);
			GetFullBranchDir(branch).CreateSubdirectory(MoveResxsToLocaleSrc.LcmDirInCrowdin).Delete(true);

			Assert.That(_sut.Execute(), Is.False);

			TaskTestUtils.AssertContainsExpectedSubstrings(_tbi.Errors[0], "Localization directory not found ", Locale, branch);
		}

		[Test]
		public void MissingLcmResxs_LogsError([Values(null, "FW-9.0")] string branch)
		{
			FullSetup(branch);
			foreach (var resx in GetFullBranchDir(branch).CreateSubdirectory(MoveResxsToLocaleSrc.LcmDirInCrowdin)
				.EnumerateFiles("*.resx", SearchOption.AllDirectories))
			{
				resx.Delete();
			}

			Assert.That(_sut.Execute(), Is.False);

			TaskTestUtils.AssertContainsExpectedSubstrings(_tbi.Errors[0], "No localized resx files found in ", Locale, branch);
		}

		[Test]
		public void GetCrowdinBranch_NullOrEmptyFilename_NullOrEmpty([Values(null, "")] string filename)
		{
			_sut.CrowdinJson = filename;
			Assert.That(_sut.GetCrowdinBranch(), Is.Null.Or.Empty);
		}

		[Test]
		public void GetCrowdinBranch_NoFile_NullOrEmpty()
		{
			Assert.That(_sut.GetCrowdinBranch(), Is.Null.Or.Empty);
		}

		[Test]
		public void GetCrowdinBranch_NullOrEmptyBranch_NullOrEmpty([Values(null, "")] string branch)
		{
			WriteCrowdinJson(branch);
			Assert.That(_sut.GetCrowdinBranch(), Is.Null.Or.Empty);
		}

		[Test]
		public void GetCrowdinBranch_GetsBranch()
		{
			const string expected = "FW-9.1";
			WriteCrowdinJson(expected);
			Assert.That(_sut.GetCrowdinBranch(), Is.EqualTo(expected));
		}

		private void FullSetup(string branch)
		{
			if (branch != null)
			{
				WriteCrowdinJson(branch);
			}

			var branchDir = GetFullBranchDir(branch);
			var fwDir = branchDir.CreateSubdirectory(MoveResxsToLocaleSrc.FwDirInCrowdin);
			// Although we do not expect any resx files directly in Src, it is better to plan ahead than to be surprised later.
			File.WriteAllText(Path.Combine(fwDir.FullName, "root.resx"), "<no></surprises>");
			File.WriteAllText(Path.Combine(fwDir.CreateSubdirectory("xWorks").FullName, "strings.resx"), "<who></cares>");
			var lcmDir = branchDir.CreateSubdirectory(MoveResxsToLocaleSrc.LcmDirInCrowdin);
			File.WriteAllText(Path.Combine(lcmDir.FullName, "ruth.resx"), "<justin></case>");
			File.WriteAllText(Path.Combine(lcmDir.CreateSubdirectory("SIL.LCModel").FullName, $"Strings.{Locale}.resx"), "<ear></relevant>");
		}

		private DirectoryInfo GetFullBranchDir(string branch)
		{
			return branch == null ? _localeDir : _localeDir.CreateSubdirectory(branch);
		}

		private void WriteCrowdinJson(string branch)
		{
			var json = new Dictionary<string, object>
			{
				{
					"files", new Dictionary<string, string>
					{
						{"source", "Localizations/LCM/**/*.resx"},
						{"translation", "/%locale%/%original_path%/%file_name%.%locale%.%file_extension%"}
					}
				}
			};
			if (branch != null)
			{
				json["branch"] = branch;
			}

			File.WriteAllText(_crowdinJsonPath, JsonConvert.SerializeObject(json));
		}
	}
}
