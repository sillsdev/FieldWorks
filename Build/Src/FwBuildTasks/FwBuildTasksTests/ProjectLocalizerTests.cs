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
	public class ProjectLocalizerTests
	{
		private ProjectLocalizer _sut;
		private string _rootPath;
		private string _fwRootPath;
		private string _l10NDir;
		private string _localeDir;
		private string _srcDir;
		private string _projectDir;

		private const string LocaleGe = "ge";

		private const string FdoProjName = "FDO";

		[SetUp]
		public void Setup()
		{
			_rootPath = Path.Combine(Path.GetTempPath(), "TestRoot4ProjLocalizer_LCM");
			_fwRootPath = Path.Combine(Path.GetTempPath(), "TestRoot4ProjLocalizer");
			_srcDir = Path.Combine(_rootPath, "Src");
			_projectDir = Path.Combine(_srcDir, FdoProjName);
			_l10NDir = Path.Combine(_rootPath, "Localizations", "l10ns");
			_localeDir = Path.Combine(_l10NDir, LocaleGe);
			var task = new LocalizeFieldWorks
			{
				L10nFileDirectory = _l10NDir,
				RootDirectory = _rootPath,
				FwRootDirectory = _fwRootPath,
				SrcFolder = _srcDir,
				OutputFolder = Path.Combine(_rootPath, "Output")
			};
			var lOptions = new LocalizerOptions(task);
			var localizer = new Localizer {Locale = LocaleGe};
			localizer.Initialize(_localeDir, lOptions);
			var plOptions = new ProjectLocalizerOptions(localizer, lOptions);
			_sut = new ProjectLocalizer(_projectDir, plOptions);
		}

		[TearDown]
		public void TearDown()
		{
			DeleteRootDirs();
		}

		private void DeleteRootDirs()
		{
			if (Directory.Exists(_rootPath))
				Directory.Delete(_rootPath, true);
			if (Directory.Exists(_fwRootPath))
				Directory.Delete(_fwRootPath, true);
		}

		[Test]
		public void GetLocalizedResxSourcePath_NoCrowdinJson()
		{
			var resxPath = Path.Combine(_projectDir, "strings.resx");
			var expected = Path.Combine(_localeDir, "Src", FdoProjName, $"strings.{LocaleGe}.resx");

			var result = _sut.GetLocalizedResxSourcePath(resxPath);
			Assert.That(result, Is.EqualTo(expected));
		}

		[Test]
		public void GetLocalizedResxSourcePath_WithCrowdinBranch()
		{
			const string branch = "FieldWorks-9.1";
			var resxPath = Path.Combine(_projectDir, "strings.resx");
			var expected = Path.Combine(_localeDir, branch, "Src", FdoProjName, $"strings.{LocaleGe}.resx");
			WriteCrowdinJson(branch);

			var result = _sut.GetLocalizedResxSourcePath(resxPath);
			Assert.That(result, Is.EqualTo(expected));
		}

		[Test]
		public void GetCrowdinBranch_NoFile_NullOrEmpty()
		{
			Assert.That(_sut.GetCrowdinBranch(), Is.Null.Or.Empty);
		}

		[Test]
		public void GetCrowdinBranch_NoBranch_NullOrEmpty()
		{
			WriteCrowdinJson(null);
			Assert.That(_sut.GetCrowdinBranch(), Is.Null.Or.Empty);
		}

		[Test]
		public void GetCrowdinBranch_GetsBranch()
		{
			const string expected = "FW-9.1";
			WriteCrowdinJson(expected);
			Assert.That(_sut.GetCrowdinBranch(), Is.EqualTo(expected));
		}

		private void WriteCrowdinJson(string branch)
		{
			TaskTestUtils.RecreateDirectory(_fwRootPath);
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

			File.WriteAllText(Path.Combine(_fwRootPath, "crowdin.json"), JsonConvert.SerializeObject(json));
		}
	}
}
