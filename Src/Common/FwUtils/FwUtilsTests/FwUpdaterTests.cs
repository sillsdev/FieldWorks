// Copyright (c) 2021-2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.IO;
using SIL.TestUtilities;

namespace SIL.FieldWorks.Common.FwUtils
{
	[TestFixture]
	[Platform(Exclude = "Linux", Reason = "uses a different update system")]
	public class FwUpdaterTests
	{
		private const string ListBucketTemplate = @"<ListBucketResult xmlns=""http://s3.amazonaws.com/doc/2006-03-01/"">{0}</ListBucketResult>";

		private const string ContentsTemplate = @"<Contents>
	<Key>{0}</Key>
	<LastModified>2020-12-13T04:46:57.000Z</LastModified>
	<ETag>""3a474435577458c18cdc98e1b9336ce1-42""</ETag>
	<Size>{1}</Size>
	<StorageClass>STANDARD</StorageClass>
</Contents>";

		[Test]
		public void EmptyBucket_DoesNotThrow()
		{
			const string bucketURL = "https://test.s3.amazonaws.com/";
			// ReSharper disable once InconsistentNaming
			const int Base = 10;
			var listDoc = XDocument.Parse(string.Format(ListBucketTemplate, "<metadata/>"));
			var current = new FwUpdate(new Version("9.0.15"), true, Base, FwUpdate.Typ.Offline);

			// SUT
			Assert.That(FwUpdater.GetLatestUpdateFrom(current, listDoc, bucketURL), Is.Null);
		}

		[Test]
		public void LatestUpdateFrom_Patch()
		{
			const string bucketURL = "https://test.s3.amazonaws.com/";
			// ReSharper disable once InconsistentNaming
			const int Base = 10;
			const int arch = 64;
			var template = string.Format(ContentsTemplate, "{0}", ushort.MaxValue);
			var newestCompatibleKey = Key("9.0.21", Base, arch);
			var bucketList = string.Format(ListBucketTemplate, string.Join(Environment.NewLine,
				string.Format(template, Key("9.0.40", 14, arch)), // base must match
				string.Format(template, Key("9.0.18", Base, arch)), // this matches
				string.Format(template, newestCompatibleKey), // this matches and is a greater version
				string.Format(template, Key("9.0.90", Base, 32)), // arch must match
				string.Format(template, Key("9.0.17", Base, arch)))); // this matches but is a lesser version
			var listDoc = XDocument.Parse(bucketList);
			var current = new FwUpdate(new Version("9.0.15"), true, Base, FwUpdate.Typ.Offline);

			// SUT
			var result = FwUpdater.GetLatestUpdateFrom(current, listDoc, bucketURL);

			Assert.That(result.Version.ToString(), Is.EqualTo("9.0.21"));
			Assert.That(result.URL, Is.EqualTo($"{bucketURL}{newestCompatibleKey}"));
		}

		[Test]
		public void LatestUpdateFrom_Base()
		{
			const string bucketURL = "https://test.s3.amazonaws.com/";
			const int base1 = 10;
			const int base2 = 14;
			const int arch = 64;
			var template = string.Format(ContentsTemplate, "{0}", ushort.MaxValue);
			var newestCompatibleKey = BaseKey("9.1.5", base2, arch, true);
			var inBetweenBaseBuildKey = BaseKey("9.1.1", 12, arch, true);
			var irrelevantKey = Path.ChangeExtension(BaseKey("9.3.7", 17, arch, true), "msi");
			var bucketList = string.Format(ListBucketTemplate, string.Join(Environment.NewLine,
				string.Format(template, Key("9.0.18", base1, arch)), // matching patch
				string.Format(template, newestCompatibleKey), // this is the latest base
				string.Format(template, inBetweenBaseBuildKey), // this is a later base than current, but not the latest
				string.Format(template, Key("9.0.90", base1, 32)), // arch must match
				string.Format(template, Key("9.1.9", base2, arch)), // this is a patch on the latest base, but cannot be applied directly to the current version
				string.Format(template, irrelevantKey), // we can't use an MSI, because there may be updated shared libraries
				string.Format(template, Key("9.0.21", base1, arch)))); // matching patch
			var listDoc = XDocument.Parse(bucketList);
			var current = new FwUpdate(new Version("9.0.15"), true, base1, FwUpdate.Typ.Offline);

			// SUT
			var result = FwUpdater.GetLatestUpdateFrom(current, listDoc, bucketURL);

			Assert.That(result.Version.ToString(), Is.EqualTo("9.1.5"));
			Assert.That(result.URL, Is.EqualTo($"{bucketURL}{newestCompatibleKey}"));
		}

		[TestCase("https://test-bucket.s3.amazonaws.com/", "9.0.14.10", 367, 64, 217055232, 207)]
		[TestCase("https://bucket-test.s3.amazonaws.com/", "9.0.15.85", 365, 32, 217055233, 208)]
		[TestCase("https://test-bucket.s3.amazonaws.com/", "9.0.14.10", 367, 64, -23456789, -1)]
		public void Parse_S3ContentsForPatch(string baseURL, string version, int baseBuild, int arch, int byteSize, int mbSize)
		{
			var key = Key(version, baseBuild, arch);
			var xElt = XElement.Parse(string.Format(ContentsTemplate, key, byteSize));

			var result = FwUpdater.Parse(xElt, baseURL);

			Assert.AreEqual(version, result.Version.ToString());
			Assert.AreEqual($"{baseURL}{key}", result.URL);
			Assert.AreEqual(baseBuild, result.BaseBuild);
			Assert.AreEqual(FwUpdate.Typ.Patch, result.InstallerType);
			Assert.AreEqual(arch == 64, result.Is64Bit, $"Arch: {arch}");
			Assert.AreEqual(mbSize, result.Size);
		}

		[TestCase("https://test.s3.amazonaws.com/", "9.0.15.1", 316, 64, true, 536111222, 512)]
		[TestCase("https://test.s3.amazonaws.com/", "9.0.15.1", 32, 32, false, 535000222, 511)]
		public void Parse_S3ContentsForBase(string baseURL, string version, int baseBuild, int arch, bool isOnline, int byteSize, int mbSize)
		{
			var key = BaseKey(version, baseBuild, arch, isOnline);
			var xElt = XElement.Parse(string.Format(ContentsTemplate, key, byteSize));

			var result = FwUpdater.Parse(xElt, baseURL);

			Assert.AreEqual(version, result.Version.ToString());
			Assert.AreEqual($"{baseURL}{key}", result.URL);
			Assert.AreEqual(baseBuild, result.BaseBuild);
			Assert.AreEqual(isOnline ? FwUpdate.Typ.Online : FwUpdate.Typ.Offline, result.InstallerType);
			Assert.AreEqual(arch == 64, result.Is64Bit, $"Arch: {arch}");
			Assert.AreEqual(mbSize, result.Size);
		}

		[TestCase("jobs/FieldWorks-Win-all-Release-Patch/761/FieldWorks_9.1.1.7.6.1_b12_x64.msp")]
		[TestCase("jobs/FieldWorks-Win-all-Release-Patch/761/FieldWorks_9.1.1.761_bad_x64.msp")]
		[TestCase("jobs/FieldWorks-Win-all-Release-Patch/761/FieldWorks_9.1.1.761_x64.msp")]
		public void Parse_S3ContentsWithErrors_ReturnsNull(string key)
		{
			var xElt = XElement.Parse(string.Format(ContentsTemplate, key, 0));

			var result = FwUpdater.Parse(xElt, "https://test.s3.amazonaws.com/");
			Assert.Null(result);
		}

		[TestCase("https://downloads.languagetechnology.org/", "9.0.14.10", 367, 64, 217055232, 207)]
		[TestCase("https://downloads.languagetechnology.org/", "9.0.15.85", 365, 32, 217055233, 208)]
		[TestCase("https://downloads.languagetechnology.org/", "9.0.14.10", 367, 64, -23456789, -1)]
		public void Parse_OurContentsForPatch(string baseURL, string version, int baseBuild, int arch, int byteSize, int mbSize)
		{
			var key = $"fieldworks/{version}/{PatchFileName(version, baseBuild, arch)}";
			var xElt = XElement.Parse(string.Format(ContentsTemplate, key, byteSize));

			var result = FwUpdater.Parse(xElt, baseURL);

			Assert.AreEqual(version, result.Version.ToString());
			Assert.AreEqual($"{baseURL}{key}", result.URL);
			Assert.AreEqual(baseBuild, result.BaseBuild);
			Assert.AreEqual(FwUpdate.Typ.Patch, result.InstallerType);
			Assert.AreEqual(arch == 64, result.Is64Bit, $"Arch: {arch}");
			Assert.AreEqual(mbSize, result.Size);
		}

		[TestCase("https://downloads.languagetechnology.org/", "9.0.15.1", 316, 64, true, 536111222, 512)]
		[TestCase("https://downloads.languagetechnology.org/", "9.0.15.1", 32, 32, false, 535000222, 511)]
		public void Parse_OurContentsForBase(string baseURL, string version, int baseBuild, int arch, bool isOnline, int byteSize, int mbSize)
		{
			var key = $"fieldworks/{version}/{baseBuild}/{BaseFileName(version, arch, isOnline)}";
			var xElt = XElement.Parse(string.Format(ContentsTemplate, key, byteSize));

			var result = FwUpdater.Parse(xElt, baseURL);

			Assert.AreEqual(version, result.Version.ToString());
			Assert.AreEqual($"{baseURL}{key}", result.URL);
			Assert.AreEqual(baseBuild, result.BaseBuild);
			Assert.AreEqual(isOnline ? FwUpdate.Typ.Online : FwUpdate.Typ.Offline, result.InstallerType);
			Assert.AreEqual(arch == 64, result.Is64Bit, $"Arch: {arch}");
			Assert.AreEqual(mbSize, result.Size);
		}

		[TestCase("fieldworks/9.1.1/FieldWorks9.1.1_Offline_x64.exe")]
		[TestCase("fieldworks/9.1.1/NaN/FieldWorks_9.1.1.1.1_Online_x64.exe")]
		public void Parse_OurContentsWithErrors_ReturnsNull(string key)
		{
			var xElt = XElement.Parse(string.Format(ContentsTemplate, key, 0));

			var result = FwUpdater.Parse(xElt, "https://test.s3.amazonaws.com/");
			Assert.Null(result);
		}

		[TestCase(@"C:\ProgramData\SIL\FieldWorks\DownloadedUpdates\", "9.0.15.1", 316, 64, true)]
		[TestCase(@"C:\ProgramData\SIL\FieldWorks\DownloadedUpdates\", "9.0.15.1", 32, 32, false)]
		public void Parse_LocalContentsForBase(string baseURL, string version, int baseBuild, int arch, bool isOnline)
		{
			var filename = BaseFileName(version, arch, isOnline);

			var result = FwUpdater.Parse(filename, baseURL);

			Assert.AreEqual(version, result.Version.ToString());
			Assert.AreEqual($"{baseURL}{filename}", result.URL);
			Assert.AreEqual(0, result.BaseBuild, "not important at this point");
			Assert.AreEqual(isOnline ? FwUpdate.Typ.Online : FwUpdate.Typ.Offline, result.InstallerType);
			Assert.AreEqual(arch == 64, result.Is64Bit, $"Arch: {arch}");
		}

		[TestCase("9.0.16", "9.0.17", true, true, 314, 314, FwUpdate.Typ.Patch, ExpectedResult = true)]
		[TestCase("9.0.16", "9.0.17", true, true, 314, 314, FwUpdate.Typ.Online, ExpectedResult = false, TestName = "Not a patch")]
		[TestCase("9.0.16", "9.0.16", true, true, 314, 314, FwUpdate.Typ.Patch, ExpectedResult = false, TestName = "Same version")]
		[TestCase("9.0.16", "9.0.17", true, false, 314, 314, FwUpdate.Typ.Patch, ExpectedResult = false, TestName = "Different Architecture")]
		[TestCase("9.0.16", "9.0.17", false, false, 314, 314, FwUpdate.Typ.Patch, ExpectedResult = true, TestName = "Both 32-bit")]
		[TestCase("9.0.16", "9.0.17", true, true, 314, 320, FwUpdate.Typ.Patch, ExpectedResult = false, TestName = "Different Base")]
		public bool IsPatchOn(string thisVer, string thatVer, bool isThis64Bit, bool isThat64Bit, int thisBase, int thatBase, FwUpdate.Typ thatType)
		{
			var current = new FwUpdate(new Version(thisVer), isThis64Bit, thisBase, FwUpdate.Typ.Offline);
			var available = new FwUpdate(new Version(thatVer), isThat64Bit, thatBase, thatType);
			return FwUpdater.IsPatchOn(current, available);
		}

		[Test]
		public void IsPatchOn_NullDoesNotThrow()
		{
			Assert.That(FwUpdater.IsPatchOn(new FwUpdate("9.0.14", true, 366, FwUpdate.Typ.Patch), null), Is.False,
				"Null likely means something wasn't parseable, which means it isn't applicable");
		}

		[TestCase("9.0.16", "9.0.17", true, true, 314, 316, FwUpdate.Typ.Online, ExpectedResult = true)]
		[TestCase("9.0.16", "9.0.17", true, true, 314, 316, FwUpdate.Typ.Offline, ExpectedResult = false, TestName = "Online would be better")]
		[TestCase("9.0.16", "9.0.17", true, true, 314, 314, FwUpdate.Typ.Patch, ExpectedResult = false, TestName = "Not a base")]
		[TestCase("9.0.16", "9.0.17", true, true, 314, 320, FwUpdate.Typ.Patch, ExpectedResult = false, TestName = "Patches a different Base")]
		[TestCase("9.0.16", "9.0.16", true, true, 314, 314, FwUpdate.Typ.Online, ExpectedResult = false, TestName = "Same version")]
		[TestCase("9.0.16", "9.0.17", true, false, 314, 316, FwUpdate.Typ.Online, ExpectedResult = false, TestName = "Different Architecture")]
		[TestCase("9.0.16", "9.0.17", false, false, 314, 316, FwUpdate.Typ.Online, ExpectedResult = true, TestName = "Both 32-bit")]
		public bool IsNewerBase(string thisVer, string thatVer, bool isThis64Bit, bool isThat64Bit, int thisBase, int thatBase, FwUpdate.Typ thatType)
		{
			var current = new FwUpdate(new Version(thisVer), isThis64Bit, thisBase, FwUpdate.Typ.Offline);
			var available = new FwUpdate(new Version(thatVer), isThat64Bit, thatBase, thatType);
			return FwUpdater.IsNewerBase(current, available);
		}

		[Test]
		public void IsNewerBase_NullDoesNotThrow()
		{
			Assert.That(FwUpdater.IsNewerBase(new FwUpdate("9.0.14", true, 366, FwUpdate.Typ.Patch), null), Is.False,
				"Null likely means something wasn't parseable, which means it isn't applicable");
		}

		[Test]
		public static void GetLatestDownloadedUpdate_DirectoryDoesNotExist()
		{
			var current = new FwUpdate("9.0.15", true, 10, FwUpdate.Typ.Offline);
			var updateDir = Path.Combine(Path.GetTempPath(), "NonExtantFwUpdatesDir");
			Assert.That(RobustIO.DeleteDirectoryAndContents(updateDir), "this test requires a nonexistent directory");
			// SUT
			Assert.That(FwUpdater.GetLatestDownloadedUpdate(current, updateDir), Is.Null);
		}

		[Test]
		public static void GetLatestDownloadedUpdate_NoneExist()
		{
			var current = new FwUpdate("9.0.15", true, 10, FwUpdate.Typ.Offline);
			using (var updateDir = new TemporaryFolder("EmptyFwUpdateDir"))
			{
				Assert.That(FwUpdater.GetLatestDownloadedUpdate(current, updateDir.Path), Is.Null);
			}
		}

		[Test]
		public static void GetLatestDownloadedUpdate_SkipsPartialDownloads()
		{
			const int baseBld = 12;
			var current = new FwUpdate("9.0.15.1", true, baseBld, FwUpdate.Typ.Offline);
			using (var updateDir = new TemporaryFolder("TestFwUpdateDir"))
			{
				var updateFileName = Path.Combine(updateDir.Path, $"{PatchFileName("9.0.15.2", baseBld, 64)}.tmp");
				File.WriteAllText(updateFileName, string.Empty);

				Assert.That(FwUpdater.GetLatestDownloadedUpdate(current, updateDir.Path), Is.Null);
			}
		}

		[Test]
		public static void GetLatestDownloadedUpdate_BadFilename_DoesNotThrow()
		{
			var current = new FwUpdate("9.0.15.1", true, 12, FwUpdate.Typ.Offline);
			using (var updateDir = new TemporaryFolder("TestFwUpdateDir"))
			{
				var updateFileName = Path.Combine(updateDir.Path, $"{PatchFileName("version", 12, 64)}.tmp");
				File.WriteAllText(updateFileName, string.Empty);

				Assert.That(FwUpdater.GetLatestDownloadedUpdate(current, updateDir.Path), Is.Null);
			}
		}

		[Test]
		public static void GetLatestDownloadedUpdate_DoesNotReinstallTheSameVersion()
		{
			const string version = "9.0.15.1";
			const int baseBld = 14;
			var current = new FwUpdate(version, true, baseBld, FwUpdate.Typ.Offline);
			using (var updateDir = new TemporaryFolder("TestFwUpdateDir"))
			{
				var updateFileName = Path.Combine(updateDir.Path, PatchFileName(version, baseBld, 64));
				File.WriteAllText(updateFileName, string.Empty);

				Assert.That(FwUpdater.GetLatestDownloadedUpdate(current, updateDir.Path), Is.Null);
			}
		}

		[Test]
		public static void GetLatestDownloadedUpdate_GetsLatestPatchForThisBase()
		{
			const int baseBld = 14;
			var current = new FwUpdate("9.0.15.1", true, baseBld, FwUpdate.Typ.Offline);
			using (var updateDir = new TemporaryFolder("TestFwUpdateDir"))
			{
				// earlier patch
				File.WriteAllText(Path.Combine(updateDir.Path, PatchFileName("9.0.17.4", baseBld, 64)), string.Empty);
				// latest patch for this base
				var updateFileName = Path.Combine(updateDir.Path, PatchFileName("9.0.18.8", baseBld, 64));
				File.WriteAllText(updateFileName, string.Empty);
				// patch for a different base
				File.WriteAllText(Path.Combine(updateDir.Path, PatchFileName("9.0.21.42", baseBld + 1, 64)), string.Empty);
				// irrelevant file
				var otherFileName = Path.Combine(updateDir.Path, Path.ChangeExtension(PatchFileName("9.3.5", baseBld, 64), "xml"));
				File.WriteAllText(otherFileName, string.Empty);

				// SUT
				var result = FwUpdater.GetLatestDownloadedUpdate(current, updateDir.Path);

				Assert.That(result.URL, Is.EqualTo(updateFileName));
				Assert.That(result.Version, Is.EqualTo(new Version(9, 0, 18, 8)));
			}
		}

		[Test]
		public static void GetLatestDownloadedUpdate_GetsLatestBase()
		{
			var current = new FwUpdate("9.0.15.1", true, 12, FwUpdate.Typ.Offline);
			using (var updateDir = new TemporaryFolder("TestFwUpdateDir"))
			{
				// earlier base
				File.WriteAllText(Path.Combine(updateDir.Path, BaseFileName("9.0.17.4")), string.Empty);
				// latest base
				var updateFileName = Path.Combine(updateDir.Path, BaseFileName("9.0.18.8"));
				File.WriteAllText(updateFileName, string.Empty);
				// patch for a different base
				File.WriteAllText(Path.Combine(updateDir.Path, PatchFileName("9.0.21.42", 15, 64)), string.Empty);
				// irrelevant file (although, perhaps, in the future, we will support .msi installers)
				var otherFileName = Path.Combine(updateDir.Path, Path.ChangeExtension(BaseFileName("9.3.5"), "msi"));
				File.WriteAllText(otherFileName, string.Empty);

				// SUT
				var result = FwUpdater.GetLatestDownloadedUpdate(current, updateDir.Path);

				Assert.That(result.URL, Is.EqualTo(updateFileName));
				Assert.That(result.Version, Is.EqualTo(new Version(9, 0, 18, 8)));
			}
		}

		private static string Key(string version, int baseBuild, int arch)
		{
			return $"jobs/FieldWorks-Win-all-Patch/10/{PatchFileName(version, baseBuild, arch)}";
		}

		private static string PatchFileName(string version, int baseBuild, int arch)
		{
			return $"FieldWorks_{version}_b{baseBuild}_x{arch}.msp";
		}

		private static string BaseKey(string version, int baseBuild, int arch, bool isOnline)
		{
			return $"jobs/FieldWorks-Win-all-Release-Base/{baseBuild}/{BaseFileName(version, arch, isOnline)}";
		}

		private static string BaseFileName(string version, int arch = 64, bool isOnline = true)
		{
			return $"FieldWorks_{version}_O{(isOnline ? "n" : "ff")}line_x{arch}.exe";
		}
	}
}