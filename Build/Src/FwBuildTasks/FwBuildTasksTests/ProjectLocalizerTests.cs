// Copyright (c) 2021-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.Build.Tasks.Localization;

namespace SIL.FieldWorks.Build.Tasks.FwBuildTasksTests
{
	[TestFixture]
	public class ProjectLocalizerTests
	{
		private ProjectLocalizer _sut;
		private string _rootPath;
		private string _l10NDir;
		private string _localeDir;
		private string _srcDir;
		private string _projectDir;

		private const string LocaleGe = "ge";

		private const string FdoProjName = "FDO";

		[SetUp]
		public void Setup()
		{
			_rootPath = Path.Combine(Path.GetTempPath(), "TestRoot4ProjLocalizer");
			_srcDir = Path.Combine(_rootPath, "Src");
			_projectDir = Path.Combine(_srcDir, FdoProjName);
			_l10NDir = Path.Combine(_rootPath, "Localizations", "l10ns");
			_localeDir = Path.Combine(_l10NDir, LocaleGe);
			var task = new LocalizeFieldWorks
			{
				L10nFileDirectory = _l10NDir,
				RootDirectory = _rootPath,
				SrcFolder = _srcDir,
				OutputFolder = Path.Combine(_rootPath, "Output")
			};
			var lOptions = new LocalizerOptions(task);
			var localizer = new Localization.Localizer { Locale = LocaleGe };
			localizer.Initialize(_localeDir, lOptions);
			var plOptions = new ProjectLocalizerOptions(localizer, lOptions);
			_sut = new ProjectLocalizer(_projectDir, plOptions);
		}

		[TearDown]
		public void TearDown()
		{
			DeleteRootDir();
		}

		private void DeleteRootDir()
		{
			if (Directory.Exists(_rootPath))
				Directory.Delete(_rootPath, true);
		}

		[Test]
		public void GetLocalizedResxSourcePath()
		{
			var resxPath = Path.Combine(_projectDir, "strings.resx");
			var expected = Path.Combine(_localeDir, "Src", FdoProjName, $"strings.{LocaleGe}.resx");

			var result = _sut.GetLocalizedResxSourcePath(resxPath);
			Assert.That(result, Is.EqualTo(expected));
		}
	}
}
