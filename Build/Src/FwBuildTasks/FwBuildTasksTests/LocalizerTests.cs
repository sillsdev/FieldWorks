// Copyright (c) 2021-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;

namespace SIL.FieldWorks.Build.Tasks.FwBuildTasksTests
{
	[TestFixture]
	public class LocalizerTests
	{
		[TestCase("9.2.5", "9.2.5")]
		[TestCase("9.2.8.3.5", "9.2.8.3")]
		[TestCase("8.3.6 beta 4 (debug)", "8.3.6.4")]
		[TestCase("10.2.0-beta007", "10.2.0.7")]
		public static void ParseInformationVersion(string infoVersion, string versionVersion)
		{
			var sut = new Localization.Localizer();
			sut.ParseInformationVersion(infoVersion);
			Assert.That(sut.InformationVersion, Is.EqualTo(infoVersion), "info");
			Assert.That(sut.Version, Is.EqualTo(versionVersion), "version");
			Assert.That(sut.FileVersion, Is.EqualTo(versionVersion), "file");
		}
	}
}
