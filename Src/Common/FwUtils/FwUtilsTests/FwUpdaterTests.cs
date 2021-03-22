// Copyright (c) 2021-2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Xml.Linq;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils
{
	[TestFixture]
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
		public void LatestPatchOnThisBase()
		{
			const string bucketURL = "https://test.s3.amazonaws.com/";
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
			var result = FwUpdater.GetLatestNightlyPatch(current, listDoc, bucketURL);

			Assert.That(result.Version.ToString(), Is.EqualTo("9.0.21"));
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
			var key = $"jobs/FieldWorks_Win-all-Base/{baseBuild}/FieldWorks_{version}_O{(isOnline ? "n" : "ff")}line_x{arch}.exe";
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

		// TODO: is base of higher version; (is patch on base of higher version?)

		private static string Key(string version, int baseBuild, int arch)
		{
			return $"jobs/FieldWorks-Win-all-Patch/10/FieldWorks_{version}_b{baseBuild}_x{arch}.msp";
		}
	}
}