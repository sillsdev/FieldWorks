// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.Common.FwUtils
{
	class LayoutKeyUtilsTests : BaseTest
	{
		[Test]
		public void GetSuffixedPartOfNamedViewOrDuplicateNodeWorks()
		{
			string[] keyAttributes = { null, null, "name", null };
			string[] keyValues = { null, null, "test#1", null};
			string[] stdKeyValues;
			// SUT
			var suffix = LayoutKeyUtils.GetSuffixedPartOfNamedViewOrDuplicateNode(keyAttributes, keyValues, out stdKeyValues);
			Assert.That(suffix, Is.StringMatching("#1"));
			Assert.That(stdKeyValues[2], Is.StringMatching("test"));
			keyValues[2] = "test%01";
			suffix = LayoutKeyUtils.GetSuffixedPartOfNamedViewOrDuplicateNode(keyAttributes, keyValues, out stdKeyValues);
			Assert.That(suffix, Is.StringMatching("%01"));
			Assert.That(stdKeyValues[2], Is.StringMatching("test"));
			keyValues[2] = "test_1";
			suffix = LayoutKeyUtils.GetSuffixedPartOfNamedViewOrDuplicateNode(keyAttributes, keyValues, out stdKeyValues);
			Assert.That(suffix, Is.StringMatching("_1"));
			Assert.That(stdKeyValues[2], Is.StringMatching("test"));
			keyValues[2] = "test_AsPara#Stem01";
			suffix = LayoutKeyUtils.GetSuffixedPartOfNamedViewOrDuplicateNode(keyAttributes, keyValues, out stdKeyValues);
			Assert.That(suffix, Is.StringMatching("#Stem01"));
			Assert.That(stdKeyValues[2], Is.StringMatching("test_AsPara"));
			keyValues[2] = "test%01_1.0.0";
			suffix = LayoutKeyUtils.GetSuffixedPartOfNamedViewOrDuplicateNode(keyAttributes, keyValues, out stdKeyValues);
			Assert.That(suffix, Is.StringMatching("%01_1.0.0"));
			Assert.That(stdKeyValues[2], Is.StringMatching("test"));
		}

		[Test]
		public void GetPossibleParamSuffixWorks()
		{
			var node = new XmlDocument();
			node.LoadXml("<part ref=\"RootSubEntryTypeConfig\" param=\"publishRootSubEntryType_1\" hideConfig=\"true\" dup=\"1\" />");
			var suffix = LayoutKeyUtils.GetPossibleParamSuffix(node.DocumentElement);
			Assert.That(suffix, Is.StringMatching("_1"));

			node = new XmlDocument();
			node.LoadXml("<part ref=\"RootSubEntryTypeConfig\" param=\"publishRootSubEntryType_%01_1\" hideConfig=\"true\" dup=\"1\" />");
			suffix = LayoutKeyUtils.GetPossibleParamSuffix(node.DocumentElement);
			Assert.That(suffix, Is.StringMatching("%01_1"));
		}
	}
}
