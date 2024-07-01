// Copyright (c) 2021-2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using SIL.Extensions;
using SIL.LCModel.Utils;
using FileUtils = SIL.LCModel.Utils.FileUtils;

namespace SIL.FieldWorks.Common.FwUtils
{
	[TestFixture]
	[Platform(Exclude = "Linux", Reason = "uses a different update system")]
	public class FwUpdaterTests
	{
		private const string ListBucketTemplate = @"<ListBucketResult xmlns=""http://s3.amazonaws.com/doc/2006-03-01/"">{0}</ListBucketResult>";

		[OneTimeTearDown]
		public void TearDown()
		{
			FileUtils.Manager.Reset();
		}

		[TestCase(null, "https://downloads.languagetechnology.org/flexbridge/", "https://downloads.languagetechnology.org/flexbridge/")]
		[TestCase(" ", "https://downloads.languagetechnology.org/flexbridge/", "https://downloads.languagetechnology.org/flexbridge/")]
		[TestCase("https://test.s3.amazonaws.com",  "https://downloads.languagetechnology.org/flexbridge/", "https://test.s3.amazonaws.com/")]
		[TestCase("https://test.s3.amazonaws.com/",  "https://downloads.languagetechnology.org/flexbridge/", "https://test.s3.amazonaws.com/")]
		public void GetBaseUrlFromUpdateInfo(string inFile, string inCode, string result)
		{
			var urlElt = inFile == null ? null : $"<BaseUrl>{inFile}</BaseUrl>";
			var doc = XDocument.Parse($"<UpdateInfo>{urlElt}</UpdateInfo>");
			FwUpdater.GetBaseUrlFromUpdateInfo(doc, ref inCode);
			Assert.That(inCode, Is.EqualTo(result));
		}

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
			var template = Contents("{0}", ushort.MaxValue);
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
			var template = Contents("{0}", ushort.MaxValue);
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
		[TestCase("https://test-bucket.s3.amazonaws.com/", "9.0.14.10", 367, 64, -23456789, 0)]
		public void Parse_S3ContentsForPatch(string baseURL, string version, int baseBuild, int arch, int byteSize, int mbSize)
		{
			var key = Key(version, baseBuild, arch);
			var xElt = XElement.Parse(Contents(key, byteSize));

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
			var xElt = XElement.Parse(Contents(key, byteSize));

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
			var xElt = XElement.Parse(Contents(key));

			var result = FwUpdater.Parse(xElt, "https://test.s3.amazonaws.com/");
			Assert.Null(result);
		}

		[TestCase("https://downloads.languagetechnology.org/", "9.0.14.10", 367, 64, 217055232, 207)]
		[TestCase("https://downloads.languagetechnology.org/", "9.0.15.85", 365, 32, 217055233, 208)]
		[TestCase("https://downloads.languagetechnology.org/", "9.0.14.10", 367, 64, -23456789, 0)]
		public void Parse_OurContentsForPatch(string baseURL, string version, int baseBuild, int arch, int byteSize, int mbSize)
		{
			var key = $"fieldworks/{version}/{PatchFileName(version, baseBuild, arch)}";
			var xElt = XElement.Parse(Contents(key, byteSize));

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
			var xElt = XElement.Parse(Contents(key, byteSize));

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
			var xElt = XElement.Parse(Contents(key));

			var result = FwUpdater.Parse(xElt, "https://test.s3.amazonaws.com/");
			Assert.Null(result);
		}

		[TestCase(@"C:\ProgramData\SIL\FieldWorks\DownloadedUpdates\", "9.0.15.1", 64, true)]
		[TestCase(@"C:\ProgramData\SIL\FieldWorks\DownloadedUpdates\", "9.0.15.1", 32, false)]
		public void Parse_LocalContentsForBase(string baseURL, string version, int arch, bool isOnline)
		{
			var filename = BaseFileName(version, arch, isOnline);

			var result = FwUpdater.Parse(filename, baseURL);

			Assert.AreEqual(version, result.Version.ToString());
			Assert.AreEqual($"{baseURL}{filename}", result.URL);
			Assert.AreEqual(0, result.BaseBuild, "not important at this point");
			Assert.AreEqual(isOnline ? FwUpdate.Typ.Online : FwUpdate.Typ.Offline, result.InstallerType);
			Assert.AreEqual(arch == 64, result.Is64Bit, $"Arch: {arch}");
		}

		[Test]
		public void Parse_FLExBridge()
		{
			const string baseURL = "https://software.sil.org/downloads/r/fieldworks/";
			const string filename = "FLExBridge_Offline_4.1.0.exe";

			var result = FwUpdater.Parse(filename, baseURL);

			Assert.That(result.Version.ToString(), Is.EqualTo("4.1.0"));
			Assert.That(result.URL, Is.EqualTo($"{baseURL}{filename}"));
			Assert.That(result.BaseBuild, Is.EqualTo(0), "not important at this point");
			Assert.That(result.InstallerType, Is.EqualTo(FwUpdate.Typ.Offline));
			Assert.That(result.Is64Bit, Is.False);
		}

		[TestCase("7000072", "0.13_ldml3", "7500002")]
		[TestCase("7000076", "2.0_ldml4", "75000002")]
		public static void Parse_ModelVersions(string lcm, string lift, string flexBridge)
		{
			var xElt = XElement.Parse(Contents(Key("9.8", 0, 8), modelVersion: lcm, liftModelVersion: lift, flexBridgeDataVersion: flexBridge));

			var result = FwUpdater.Parse(xElt, "/");

			Assert.That(result.LCModelVersion, Is.EqualTo(lcm));
			Assert.That(result.LIFTModelVersion, Is.EqualTo(lift));
			Assert.That(result.FlexBridgeDataVersion, Is.EqualTo(flexBridge));
		}

		[Test]
		public static void Parse_Sparse()
		{
			const string version = "9.3.7.0";
			const int baseBld = 42;
			var key = Key(version, baseBld, 64);
			const string baseUrl = "/what/ever/";
			var xElt = XElement.Parse($"<Contents><Key>{key}</Key></Contents>");

			var result = FwUpdater.Parse(xElt, baseUrl);

			Assert.That(result.Version.ToString(), Is.EqualTo(version));
			Assert.That(result.BaseBuild, Is.EqualTo(baseBld));
			Assert.That(result.Is64Bit, Is.True);
			Assert.That(result.URL, Is.EqualTo($"{baseUrl}{key}"));
			Assert.That(result.Size, Is.EqualTo(0));
			Assert.That(result.LCModelVersion, Is.Null);
			Assert.That(result.LIFTModelVersion, Is.Null);
			Assert.That(result.FlexBridgeDataVersion, Is.Null);
		}

		[Test]
		public static void AddMetadata()
		{
			const int size = 1048900;
			const string date = "1997-12-25T12:46:57.000Z";
			const string lcm = "7000070";
			const string lift = "0.12";
			const string flexBridge = "7500";
			var before = new FwUpdate("9.1.8", false, 44, FwUpdate.Typ.Online, url: "https://example.com");
			var xElt = XElement.Parse(Contents("not checked in SUT", size, date, lcm, lift, flexBridge));

			var result = FwUpdater.AddMetadata(before, xElt);

			Assert.That(result, Is.Not.SameAs(before));
			Assert.That(result.Version, Is.EqualTo(before.Version));
			Assert.That(result.Is64Bit, Is.EqualTo(before.Is64Bit));
			Assert.That(result.BaseBuild, Is.EqualTo(before.BaseBuild));
			Assert.That(result.URL, Is.EqualTo(before.URL));
			Assert.That(result.InstallerType, Is.EqualTo(before.InstallerType));
			Assert.That(result.Size, Is.EqualTo(2));
			Assert.That(result.Date, Is.EqualTo(new DateTime(1997, 12, 25, 12, 46, 57, DateTimeKind.Utc)));
			Assert.That(result.LCModelVersion, Is.EqualTo(lcm));
			Assert.That(result.LIFTModelVersion, Is.EqualTo(lift));
			Assert.That(result.FlexBridgeDataVersion, Is.EqualTo(flexBridge));
		}

		[TestCase("7000072", "7000072", "0.13_ldml3", "0.13_ldml3", "7500002", "7600042", false, false, true, TestName = "new FLEx Bridge")]
		[TestCase("7000072", null, "0.13_ldml3", null, "7500002", "7600042", false, false, true, TestName = "new FLEx Bridge installer")]
		[TestCase("7000072", "7000076", "0.13_ldml3", "0.13_ldml3", "7500002", "7500002", true, false, false, TestName = "new LCM")]
		[TestCase("7000072", "7000076", "0.13_ldml3", "0.13_ldml3", "7500002", "7600042", true, false, false, TestName = "new LCM and FLEx Bridge")]
		[TestCase("7000072", "7000076", "0.13_ldml3", "1.13_ldml3", "7500002", "7500002", true, true, false, TestName = "new LCM and LIFT")]
		[TestCase("7000072", "7000072", "0.13_ldml3", "1.13_ldml3", "7500002", "7500002", false, true, false, TestName = "new LIFT")]
		[TestCase("7000072", "7000072", "0.13_ldml3", "1.13_ldml3", "7500002", "7500042", false, true, true, TestName = "new LIFT and FLEx Bridge")]
		[TestCase("7000072", "7000076", "0.13_ldml3", "1.13_ldml3", "7500002", "7500042", true, true, false, TestName = "new everything")]
		[TestCase("7000072", "7000072", "0.13_ldml3", "0.13_ldml3", null, "7500002", false, false, false, TestName = "no FLEx Bridge installed")]
		[TestCase("7000072", null, "0.13_ldml3", null, "7500002", null, false, false, false, TestName = "no new versions found")]
		[TestCase("7000072", "7000072", "0.13_ldml3", "0.13_ldml3", "7500002", "7500002", false, false, false, TestName = "no change")]
		public static void GetUpdateMessage(string curLCM, string newLCM, string curLIFT, string newLIFT, string curSR, string newSR,
			bool wantLCMMsg, bool wantLIFTMsg, bool wantSRMsg)
		{
			const string curVer = "9.0.14";
			const string newVer = "9.3.7";
			const string prompt = "Do it now!";
			var current = new FwUpdate(curVer, true, 2, FwUpdate.Typ.Patch, 0, new DateTime(2021-11-01), curLCM, curLIFT, curSR);
			var available = new FwUpdate(newVer, true, 2, FwUpdate.Typ.Patch, 0, DateTime.Today, newLCM, newLIFT, newSR);

			var result = FwUpdater.GetUpdateMessage(FwUtilsStrings.UpdateDownloadedVersionYCurrentX, current, available, prompt);

			var versionMsg = string.Format(FwUtilsStrings.UpdateDownloadedVersionYCurrentX, curVer, newVer);
			var lcmMsg = FwUtilsStrings.ModelChangeLCM;
			var liftMsg = FwUtilsStrings.ModelChangeLIFT;
			var srMsg = string.Format(FwUtilsStrings.ModelChangeFBButNotFW, curVer);

			Assert.That(result, Does.StartWith(versionMsg));
			Assert.That(result, Does.EndWith(prompt));
			Assert.That(result, wantLCMMsg ? Does.Contain(lcmMsg) : Does.Not.Contain(lcmMsg));
			Assert.That(result, wantLIFTMsg ? Does.Contain(liftMsg) : Does.Not.Contain(liftMsg));
			Assert.That(result, wantSRMsg ? Does.Contain(srMsg) : Does.Not.Contain(srMsg));
		}

		[Test]
		public static void GetUpdateMessage()
		{
			const string curVer = "9.1.22";
			const string newVer = "9.3.7";
			const string prompt = "Do it now?";
			const string instructions = "Close FLEx Bridge first.";
			var current = new FwUpdate(curVer, true, 2, FwUpdate.Typ.Patch, 0, new DateTime(2023-08-04));
			var available = new FwUpdate(newVer, true, 2, FwUpdate.Typ.Patch, 0, DateTime.Today);

			var result = FwUpdater.GetUpdateMessage(FwUtilsStrings.UpdateDownloadedVersionYCurrentX, current, available, prompt, instructions);

			var versionMsg = string.Format(FwUtilsStrings.UpdateDownloadedVersionYCurrentX, curVer, newVer);
			var lcmMsg = FwUtilsStrings.ModelChangeLCM;
			var liftMsg = FwUtilsStrings.ModelChangeLIFT;
			var srMsg = string.Format(FwUtilsStrings.ModelChangeFBButNotFW, curVer);

			Assert.That(result, Does.StartWith(versionMsg));
			Assert.That(result, Does.Contain(prompt));
			Assert.That(result, Does.EndWith(instructions));
			Assert.That(result, Does.Not.Contain(lcmMsg));
			Assert.That(result, Does.Not.Contain(liftMsg));
			Assert.That(result, Does.Not.Contain(srMsg));
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
		[TestCase("9.0.16", "9.0.17", true, true, 314, 316, FwUpdate.Typ.Offline, ExpectedResult = true, TestName = "Offline works, too")]
		[TestCase("9.0.16", "9.0.17", true, true, 314, 314, FwUpdate.Typ.Patch, ExpectedResult = false, TestName = "Not a base")]
		[TestCase("9.0.16", "9.0.17", true, true, 314, 320, FwUpdate.Typ.Patch, ExpectedResult = false, TestName = "Patches a different Base")]
		[TestCase("9.0.16", "9.0.16", true, true, 314, 314, FwUpdate.Typ.Online, ExpectedResult = false, TestName = "Same version")]
		[TestCase("9.0.16", "9.0.17", true, false, 314, 316, FwUpdate.Typ.Online, ExpectedResult = false, TestName = "Different Architecture")]
		[TestCase("9.0.16", "9.0.17", false, false, 314, 316, FwUpdate.Typ.Online, ExpectedResult = true, TestName = "Both 32-bit")]
		public bool IsNewerBase(string thisVer, string thatVer, bool isThis64Bit, bool isThat64Bit, int thisBase, int thatBase, FwUpdate.Typ thatType)
		{
			var current = new FwUpdate(new Version(thisVer), isThis64Bit, thisBase, FwUpdate.Typ.Patch);
			var available = new FwUpdate(new Version(thatVer), isThat64Bit, thatBase, thatType);
			return FwUpdater.IsNewerBase(current, available);
		}

		[TestCase("9.1.5", "9.1.5", 5, 5, FwUpdate.Typ.Patch, FwUpdate.Typ.Online, ExpectedResult = false)]
		[TestCase("9.1.5", "9.1.5", 5, 5, FwUpdate.Typ.Patch, FwUpdate.Typ.Offline, ExpectedResult = false)]
		[TestCase("9.1.5", "9.1.5", 5, 5, FwUpdate.Typ.Online, FwUpdate.Typ.Offline, ExpectedResult = false)]
		[TestCase("9.1.5", "9.1.5", 5, 5, FwUpdate.Typ.Online, FwUpdate.Typ.Offline, ExpectedResult = false, TestName = "Keeps online")]
		[TestCase("9.1.5", "9.1.5", 5, 5, FwUpdate.Typ.Offline, FwUpdate.Typ.Online, ExpectedResult = true, TestName = "Prefers online")]
		[TestCase("9.1.5", "9.1.6", 5, 6, FwUpdate.Typ.Online, FwUpdate.Typ.Offline, ExpectedResult = true, TestName = "Prefers newer")]
		[TestCase("9.1.5", "9.1.6", 0, 0, FwUpdate.Typ.Online, FwUpdate.Typ.Offline, ExpectedResult = true, TestName = "Prefers newer (missing BB)")]
		[TestCase("9.1.6", "9.1.5", 6, 5, FwUpdate.Typ.Online, FwUpdate.Typ.Offline, ExpectedResult = false, TestName = "Prevents backsliding")]
		public bool IsNewerBase(string ver1, string ver2, int base1, int base2, FwUpdate.Typ type1, FwUpdate.Typ type2)
		{
			var bestSoFar = new FwUpdate(ver1, true, base1, type1);
			var nextCandidate = new FwUpdate(ver2, true, base2, type2);
			return FwUpdater.IsNewerBase(bestSoFar, nextCandidate);
		}

		[Test]
		public void IsNewerBase_NullDoesNotThrow()
		{
			Assert.That(FwUpdater.IsNewerBase(new FwUpdate("9.0.14", true, 366, FwUpdate.Typ.Patch), null), Is.False,
				"Null likely means something wasn't parseable, which means it isn't applicable");
		}

		[TestCase("9.0.17", true, true, 314, 316, FwUpdate.Typ.Online, ExpectedResult = true)]
		[TestCase("9.0.17", true, true, 314, 316, FwUpdate.Typ.Offline, ExpectedResult = true, TestName = "Offline works, too")]
		[TestCase("9.0.17", true, true, 314, 314, FwUpdate.Typ.Patch, ExpectedResult = false, TestName = "Not a base")]
		[TestCase("9.0.17", true, true, 314, 320, FwUpdate.Typ.Patch, ExpectedResult = false, TestName = "Patches a different Base")]
		[TestCase("9.0.16", true, true, 314, 314, FwUpdate.Typ.Online, ExpectedResult = false, TestName = "Same version")]
		[TestCase("9.0.1", true, true, 200, 300, FwUpdate.Typ.Online, ExpectedResult = true, TestName = "newer than this patch's base")]
		[TestCase("9.0.17", true, false, 314, 316, FwUpdate.Typ.Online, ExpectedResult = false, TestName = "Different Architecture")]
		[TestCase("9.0.17", false, false, 314, 316, FwUpdate.Typ.Online, ExpectedResult = true, TestName = "Both 32-bit")]
		[TestCase("9.0.17", true, true, 0, 0, FwUpdate.Typ.Online, ExpectedResult = true, TestName = "Missing Base Build number")]
		public bool IsNewerBaseThanBase(string thatVer, bool isThis64Bit, bool isThat64Bit, int thisBase, int thatBase, FwUpdate.Typ thatType)
		{
			var current = new FwUpdate(new Version("9.0.16"), isThis64Bit, thisBase, FwUpdate.Typ.Patch);
			var available = new FwUpdate(new Version(thatVer), isThat64Bit, thatBase, thatType);
			return FwUpdater.IsNewerBaseThanBase(current, available);
		}

		[Test]
		public void IsNewerBaseThanBase_NullDoesNotThrow()
		{
			Assert.That(FwUpdater.IsNewerBaseThanBase(new FwUpdate("9.0.14", true, 366, FwUpdate.Typ.Patch), null), Is.False,
				"Null likely means something wasn't parseable, which means it isn't applicable");
		}

		/// <summary>
		/// For now, get them all; in the future, we may wish to filter bases by whether they have patches
		/// </summary>
		[Test]
		public void GetAvailableUpdatesFrom()
		{
			const int base0 = 10;
			const int base1 = 12;
			const int base2 = 14;
			var current = new FwUpdate(new Version("9.0.15.800"), true, base0, FwUpdate.Typ.Offline);

			var match1 = new FwUpdate("9.0.21", true, base0, FwUpdate.Typ.Patch); // this matches
			var badArchKey = new FwUpdate("9.0.90", false, base0, FwUpdate.Typ.Patch); // arch must match
			var oldKey = new FwUpdate("9.0.10", true, base0, FwUpdate.Typ.Patch); // this matches but is a lesser version
			var match2 = new FwUpdate("9.1.5", true, base2, FwUpdate.Typ.Online); // this is the latest base
			var match3 = new FwUpdate("9.1.1", true, base1, FwUpdate.Typ.Online); // this is a later base than current, but not the latest
			var badBaseKey = new FwUpdate("9.1.9", true, base2, FwUpdate.Typ.Patch); // this is a patch on the latest base, but cannot be applied directly to the current version
			var match4 = new FwUpdate("9.0.18", true, base0, FwUpdate.Typ.Patch); // matching patch
			var match5 = new FwUpdate("9.0.15.1", true, base1, FwUpdate.Typ.Offline); // Lower version, but a newer base

			// SUT
			var result = FwUpdater.GetAvailableUpdatesFrom(current,
				new[] { match1, badArchKey, oldKey, match2, match3, badBaseKey, match4, match5 }).ToList();

			//Assert.That(result.Count, Is.EqualTo(6));
			Assert.That(result, Is.EquivalentTo(new[] { match1, match2, match3, match4, match5 }));
		}

		[Test]
		public static void GetLatestDownloadedUpdate_DirectoryDoesNotExist()
		{
			var current = new FwUpdate("9.0.15", true, 10, FwUpdate.Typ.Offline);
			FileUtils.Manager.SetFileAdapter(new MockFileOS());
			// SUT
			Assert.That(FwUpdater.GetLatestDownloadedUpdate(current), Is.Null);
		}

		[Test]
		public static void GetLatestDownloadedUpdate_NoneExist()
		{
			var current = new FwUpdate("9.0.15", true, 10, FwUpdate.Typ.Offline);
			var mockFileOS = new MockFileOS();
			FileUtils.Manager.SetFileAdapter(mockFileOS);
			mockFileOS.ExistingDirectories.Add(FwDirectoryFinder.DownloadedUpdates);
			Assert.That(FwUpdater.GetLatestDownloadedUpdate(current), Is.Null);
		}

		[Test]
		public static void GetLatestDownloadedUpdate_SkipsPartialDownloads()
		{
			const int baseBld = 12;
			var current = new FwUpdate("9.0.15.1", true, baseBld, FwUpdate.Typ.Offline);
			var updateDir = FwDirectoryFinder.DownloadedUpdates;
			var mockFileOS = new MockFileOS();
			FileUtils.Manager.SetFileAdapter(mockFileOS);
			var updateFileName = Path.Combine(updateDir, $"{PatchFileName("9.0.15.2", baseBld, 64)}.tmp");
			mockFileOS.AddFile(updateFileName, string.Empty);

			Assert.That(FwUpdater.GetLatestDownloadedUpdate(current), Is.Null);
		}

		[Test]
		public static void GetLatestDownloadedUpdate_BadFilename_DoesNotThrow()
		{
			var current = new FwUpdate("9.0.15.1", true, 12, FwUpdate.Typ.Offline);
			var updateDir = FwDirectoryFinder.DownloadedUpdates;
			var mockFileOS = new MockFileOS();
			FileUtils.Manager.SetFileAdapter(mockFileOS);
			var updateFileName = Path.Combine(updateDir, PatchFileName("bad-version", 12, 64));
			mockFileOS.AddFile(updateFileName, string.Empty);

			Assert.That(FwUpdater.GetLatestDownloadedUpdate(current), Is.Null);
		}

		[Test]
		public static void GetLatestDownloadedUpdate_DoesNotReinstallTheSameVersion()
		{
			const string version = "9.0.15.1";
			const int baseBld = 14;
			var current = new FwUpdate(version, true, baseBld, FwUpdate.Typ.Offline);
			var updateDir = FwDirectoryFinder.DownloadedUpdates;
			var mockFileOS = new MockFileOS();
			FileUtils.Manager.SetFileAdapter(mockFileOS);
			var updateFileName = Path.Combine(updateDir, PatchFileName(version, baseBld, 64));
			mockFileOS.AddFile(updateFileName, string.Empty);

			Assert.That(FwUpdater.GetLatestDownloadedUpdate(current), Is.Null);
		}

		[Test]
		public static void GetLatestDownloadedUpdate_DoesNotCheckIfNoXml()
		{
			const int baseBld = 14;
			var current = new FwUpdate("9.0.15.1", true, baseBld, FwUpdate.Typ.Offline);
			var updateDir = FwDirectoryFinder.DownloadedUpdates;
			var mockFileOS = new MockFileOS();
			FileUtils.Manager.SetFileAdapter(mockFileOS);
			mockFileOS.AddFile(Path.Combine(updateDir, PatchFileName("9.0.18.8", baseBld, 64)), string.Empty);

			Assert.That(FwUpdater.GetLatestDownloadedUpdate(current), Is.Null);
		}

		[TestCase(false, TestName = "invalid XML")]
		[TestCase(true, TestName = "mismatching metadata")]
		public static void GetLatestDownloadedUpdate_WorksWithIncompleteXml(bool isXmlValid)
		{
			const int baseBld = 14;
			var current = new FwUpdate("9.0.15.1", true, baseBld, FwUpdate.Typ.Offline);
			var updateDir = FwDirectoryFinder.DownloadedUpdates;
			var mockFileOS = new MockFileOS();
			FileUtils.Manager.SetFileAdapter(mockFileOS);
			const string updateVersion = "9.0.18.8";
			var updateFileName = Path.Combine(updateDir, PatchFileName(updateVersion, baseBld, 64));
			mockFileOS.AddFile(updateFileName, string.Empty);
			mockFileOS.AddFile(FwUpdater.LocalUpdateInfoFilePath(false), isXmlValid
				? "<notValidXml><see></notValidXml>"
				: string.Format(ListBucketTemplate, Contents(Key("8.0.3", baseBld - 1, 64))));

			// SUT
			var result = FwUpdater.GetLatestDownloadedUpdate(current);

			Assert.That(result.URL, Is.EqualTo(updateFileName));
			Assert.That(result.Version, Is.EqualTo(new Version(updateVersion)));
			Assert.That(result.LCModelVersion, Is.Null);
			Assert.That(result.LIFTModelVersion, Is.Null);
			Assert.That(result.FlexBridgeDataVersion, Is.Null);
			Assert.That(FileUtils.FileExists(FwUpdater.LocalUpdateInfoFilePath(false)), Is.False, "Local update XML should have been deleted");
		}

		[Test]
		public static void GetLatestDownloadedUpdate_GetsLatestPatchForThisBase()
		{
			const int baseBld = 14;
			var current = new FwUpdate("9.0.15.1", true, baseBld, FwUpdate.Typ.Offline);
			var updateDir = FwDirectoryFinder.DownloadedUpdates;
			var mockFileOS = new MockFileOS();
			FileUtils.Manager.SetFileAdapter(mockFileOS);
			// UpdateInfo XML file
			mockFileOS.AddFile(FwUpdater.LocalUpdateInfoFilePath(false), string.Format(ListBucketTemplate,
				Contents(Key("9.0.18.8", baseBld, 64), modelVersion:"1.0", liftModelVersion:"2.0",flexBridgeDataVersion:"3.0")));
			// earlier patch
			mockFileOS.AddFile(Path.Combine(updateDir, PatchFileName("9.0.17.4", baseBld, 64)), string.Empty);
			// latest patch for this base
			var updateFileName = Path.Combine(updateDir, PatchFileName("9.0.18.8", baseBld, 64));
			mockFileOS.AddFile(updateFileName, string.Empty);
			// patch for a different base
			mockFileOS.AddFile(Path.Combine(updateDir, PatchFileName("9.0.21.42", baseBld + 1, 64)), string.Empty);
			// irrelevant file
			var otherFileName = Path.Combine(updateDir, Path.ChangeExtension(PatchFileName("9.3.5", baseBld, 64), "xml"));
			mockFileOS.AddFile(otherFileName, string.Empty);

			// SUT
			var result = FwUpdater.GetLatestDownloadedUpdate(current);

			Assert.That(result.URL, Is.EqualTo(updateFileName));
			Assert.That(result.Version, Is.EqualTo(new Version(9, 0, 18, 8)));
			Assert.That(result.LCModelVersion, Is.EqualTo("1.0"));
			Assert.That(result.LIFTModelVersion, Is.EqualTo("2.0"));
			Assert.That(result.FlexBridgeDataVersion, Is.EqualTo("3.0"));
			Assert.False(FileUtils.FileExists(FwUpdater.LocalUpdateInfoFilePath(false)), "Local update XML should have been deleted");
		}

		[Test]
		public static void GetLatestDownloadedUpdate_GetsLatestBase()
		{
			var current = new FwUpdate("9.0.15.1", true, 12, FwUpdate.Typ.Offline);
			var updateDir = FwDirectoryFinder.DownloadedUpdates;
			var mockFileOS = new MockFileOS();
			FileUtils.Manager.SetFileAdapter(mockFileOS);
			// UpdateInfo XML file
			mockFileOS.AddFile(FwUpdater.LocalUpdateInfoFilePath(false), string.Format(ListBucketTemplate,
				Contents(Key("9.0.18.8", 18, 64), modelVersion:"1.0", liftModelVersion:"2.0", flexBridgeDataVersion:"3.0")));
			// earlier base
			mockFileOS.AddFile(Path.Combine(updateDir, BaseFileName("9.0.17.4")), string.Empty);
			// latest base
			var updateFileName = Path.Combine(updateDir, BaseFileName("9.0.18.8"));
			mockFileOS.AddFile(updateFileName, string.Empty);
			// patch for a different base
			mockFileOS.AddFile(Path.Combine(updateDir, PatchFileName("9.0.21.42", 15, 64)), string.Empty);
			// irrelevant file (although, perhaps, in the future, we will support .msi installers)
			var otherFileName = Path.Combine(updateDir, Path.ChangeExtension(BaseFileName("9.3.5"), "msi"));
			mockFileOS.AddFile(otherFileName, string.Empty);

			// SUT
			var result = FwUpdater.GetLatestDownloadedUpdate(current);

			Assert.That(result.URL, Is.EqualTo(updateFileName));
			Assert.That(result.Version, Is.EqualTo(new Version(9, 0, 18, 8)));
			Assert.That(result.LCModelVersion, Is.EqualTo("1.0"));
			Assert.That(result.LIFTModelVersion, Is.EqualTo("2.0"));
			Assert.That(result.FlexBridgeDataVersion, Is.EqualTo("3.0"));
			Assert.False(FileUtils.FileExists(FwUpdater.LocalUpdateInfoFilePath(false)), "Local update XML should have been deleted");
		}

		[Test]
		public static void DeleteOldUpdateFiles_UpdateBase()
		{
			var current = new FwUpdate("9.0.15.1", true, 12, FwUpdate.Typ.Offline);
			var updateDir = FwDirectoryFinder.DownloadedUpdates;
			var mockFileOS = new MockFileOS();
			FileUtils.Manager.SetFileAdapter(mockFileOS);
			// UpdateInfo XML file
			mockFileOS.AddFile(FwUpdater.LocalUpdateInfoFilePath(false), string.Format(ListBucketTemplate,
				Contents(Key("9.0.18.1", 18, 64))));
			// earlier base
			var earlierBaseFileName = Path.Combine(updateDir, BaseFileName("9.0.17.1"));
			mockFileOS.AddFile(earlierBaseFileName, string.Empty);
			// latest base
			var updateFileName = Path.Combine(updateDir, BaseFileName("9.0.18.1"));
			mockFileOS.AddFile(updateFileName, string.Empty);
			// patch for a different base
			var patchForDifferentBase = Path.Combine(updateDir, PatchFileName("9.0.21.42", 15, 64));
			mockFileOS.AddFile(patchForDifferentBase, string.Empty);
			// irrelevant file (although, perhaps, in the future, we will support .msi installers)
			var otherFileName = Path.Combine(updateDir, Path.ChangeExtension(BaseFileName("9.3.5"), "msi"));
			mockFileOS.AddFile(otherFileName, string.Empty);
			var result = FwUpdater.GetLatestDownloadedUpdate(current);

			// SUT
			FwUpdater.DeleteOldUpdateFiles(result);

			Assert.False(FileUtils.FileExists(FwUpdater.LocalUpdateInfoFilePath(false)), "Local update XML should have been deleted");
			Assert.False(FileUtils.FileExists(earlierBaseFileName), "Earlier Base should have been deleted");
			Assert.True(FileUtils.FileExists(updateFileName), "The Update File should NOT have been deleted");
			Assert.False(FileUtils.FileExists(patchForDifferentBase), "Patch For Different Base should have been deleted");
			Assert.False(FileUtils.FileExists(otherFileName), "Other File should have been deleted");
		}

		[Test]
		public static void DeleteOldUpdateFiles_UpdatePatch()
		{
			const int baseBld = 14;
			var current = new FwUpdate("9.0.18.1", true, baseBld, FwUpdate.Typ.Offline);
			var updateDir = FwDirectoryFinder.DownloadedUpdates;
			var mockFileOS = new MockFileOS();
			FileUtils.Manager.SetFileAdapter(mockFileOS);
			// UpdateInfo XML file
			mockFileOS.AddFile(FwUpdater.LocalUpdateInfoFilePath(false), string.Format(ListBucketTemplate,
				Contents(Key("9.0.19.8", baseBld, 64))));
			// earlier patch
			var earlierPatchFileName = Path.Combine(updateDir, PatchFileName("9.0.18.4", baseBld, 64));
			mockFileOS.AddFile(earlierPatchFileName, string.Empty);
			// earlier base
			var earlierBaseFileName = Path.Combine(updateDir, BaseFileName("9.0.17.1"));
			mockFileOS.AddFile(earlierBaseFileName, string.Empty);
			// latest base - online
			var onlineBaseFileName = Path.Combine(updateDir, BaseFileName("9.0.18.1"));
			mockFileOS.AddFile(onlineBaseFileName, string.Empty);
			// latest base - offline
			var offlineBaseFileName = Path.Combine(updateDir, BaseFileName("9.0.18.1", isOnline: false));
			mockFileOS.AddFile(offlineBaseFileName, string.Empty);
			// latest patch for this base
			var updateFileName = Path.Combine(updateDir, PatchFileName("9.0.19.8", baseBld, 64));
			mockFileOS.AddFile(updateFileName, string.Empty);
			// patch for a different base
			var patchForDifferentBase = Path.Combine(updateDir, PatchFileName("9.1.21.42", baseBld + 1, 64));
			mockFileOS.AddFile(patchForDifferentBase, string.Empty);
			// irrelevant file
			var otherFileName = Path.Combine(updateDir, Path.ChangeExtension(PatchFileName("9.3.5", baseBld, 64), "xml"));
			mockFileOS.AddFile(otherFileName, string.Empty);
			var result = FwUpdater.GetLatestDownloadedUpdate(current);

			// SUT
			FwUpdater.DeleteOldUpdateFiles(result);

			Assert.False(FileUtils.FileExists(FwUpdater.LocalUpdateInfoFilePath(false)), "Local update XML should have been deleted");
			Assert.False(FileUtils.FileExists(earlierPatchFileName), "Earlier Patch should have been deleted");
			Assert.False(FileUtils.FileExists(earlierBaseFileName), "Earlier Base should have been deleted");
			Assert.False(FileUtils.FileExists(onlineBaseFileName), "The Online Base File should have been deleted");
			Assert.True(FileUtils.FileExists(offlineBaseFileName), "The Offline Base File should NOT have been deleted");
			Assert.True(FileUtils.FileExists(updateFileName), "The Update File should NOT have been deleted");
			Assert.False(FileUtils.FileExists(patchForDifferentBase), "Patch For Different Base should have been deleted");
			Assert.False(FileUtils.FileExists(otherFileName), "Other File should have been deleted");
		}

		[Test]
		public void VersionInfoProvider_GetVersionInfo_WorksForOddCulture()
		{
			var versionInfo = new VersionInfoProvider(Assembly.GetAssembly(GetType()), true);
			var originalCulture = Thread.CurrentThread.CurrentCulture;
			var oddCulture = new CultureInfo("th-TH");
			oddCulture.DateTimeFormat.TimeSeparator = "-";
			Thread.CurrentThread.CurrentCulture = oddCulture;
			try
			{
				// Simulate the generation of the ISO8601 date string
				string iso8601DateString = new DateTime(2024, 6, 27).ToISO8601TimeFormatDateOnlyString();


				// Asserting that the parse result should fail (which it should, given the culture mismatch)
				Assert.Throws<FormatException>(()=> DateTime.Parse(iso8601DateString), "Test not valid if this doesn't throw");

				// Asserting that the version info provider's apparent build date is correctly handled (or not)
				Assert.That(versionInfo.ApparentBuildDate, Is.Not.EqualTo(VersionInfoProvider.DefaultBuildDate));
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = originalCulture;
			}
		}

		private static string Contents(string key, int size = 0, string modified = "2020-12-13T04:46:57.000Z",
			string modelVersion = null, string liftModelVersion = null, string flexBridgeDataVersion = null)
		{
			return $@"<Contents>
	<Key>{key}</Key>
	<LastModified>{modified}</LastModified>
	<ETag>""3a474435577458c18cdc98e1b9336ce1-42""</ETag>
	<LCModelVersion>{modelVersion}</LCModelVersion>
	<LIFTModelVersion>{liftModelVersion}</LIFTModelVersion>
	<FlexBridgeDataVersion>{flexBridgeDataVersion}</FlexBridgeDataVersion>
	<Size>{size}</Size>
	<StorageClass>STANDARD</StorageClass>
</Contents>";
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