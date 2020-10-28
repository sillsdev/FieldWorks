// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;

namespace SIL.Utils
{
	/// <summary>
	/// Summary description for XmlUtilsTest.
	/// </summary>
	[TestFixture]
	public class XmlUtilsTest
	{
		[TestCase(null, ExpectedResult = false)]
		[TestCase("", ExpectedResult = false)]
		[TestCase("FALSE", ExpectedResult = false)]
		[TestCase("False", ExpectedResult = false)]
		[TestCase("false", ExpectedResult = false)]
		[TestCase("NO", ExpectedResult = false)]
		[TestCase("No", ExpectedResult = false)]
		[TestCase("no", ExpectedResult = false)]
		[TestCase("TRUE", ExpectedResult = true)]
		[TestCase("True", ExpectedResult = true)]
		[TestCase("true", ExpectedResult = true)]
		[TestCase("YES", ExpectedResult = true)]
		[TestCase("Yes", ExpectedResult = true)]
		[TestCase("yes", ExpectedResult = true)]
		public bool GetBooleanAttributeValue(string input)
		{
			return XmlUtils.GetBooleanAttributeValue(input);
		}

		[Test]
		public void MakeSafeXmlAttributeTest()
		{
			string sFixed = XmlUtils.MakeSafeXmlAttribute("abc&def<ghi>jkl\"mno'pqr&stu");
			Assert.AreEqual("abc&amp;def&lt;ghi&gt;jkl&quot;mno&apos;pqr&amp;stu", sFixed, "First Test of MakeSafeXmlAttribute");

			sFixed = XmlUtils.MakeSafeXmlAttribute("abc&def\r\nghi\u001Fjkl\u007F\u009Fmno");
			Assert.AreEqual("abc&amp;def&#xD;&#xA;ghi&#x1F;jkl&#x7F;&#x9F;mno", sFixed, "Second Test of MakeSafeXmlAttribute");
		}
	}

	[TestFixture]
	public class XmlResourceResolverTests
	{
		[TestCase(null, "", "res:///")]
		[TestCase(null, "foo", "res://foo")]
		[TestCase(null, "/foo", "res:///foo")]
		[TestCase(null, "/tmp/foo", "res:///tmp/foo")]
		[TestCase(null, @"c:\tmp\foo", "res:///c:/tmp/foo")]
		[TestCase(null, "res:///tmp/foo", "res:///tmp/foo")]
		[TestCase(null, "file:///tmp/foo", "res:///tmp/foo")]
		[TestCase("res:///tmp/foo", "bar", "res:///tmp/bar")]
		[TestCase("file:///tmp/foo", "bar", "file:///tmp/bar")]
		[TestCase("file:///tmp/foo", "res:///bar", "res:///bar")]
		[TestCase("res:///tmp/foo", "file:///bar", "file:///bar")]
		[TestCase(@"c:\tmp\foo", "bar", "file:///c:/tmp/bar")]
		[TestCase("res:///tmp/foo/", "bar", "res:///tmp/foo/bar")]
		[TestCase("file:///tmp/foo/", "bar", "file:///tmp/foo/bar")]
		[TestCase(@"c:\tmp\foo\", "bar", "file:///c:/tmp/foo/bar")]
		[TestCase("file:///tmp/foo/", "res:///bar", "res:///bar")]
		[TestCase("res:///tmp/foo/", "file:///bar", "file:///bar")]
		public void ResolveUri(string baseUriString, string relativeUri, string resultUri)
		{
			var sut = XmlUtils.GetResourceResolver("XMLUtilsTests");

			var baseUri = baseUriString == null ? null : new Uri(baseUriString);
			Assert.That(sut.ResolveUri(baseUri, relativeUri), Is.EqualTo(new Uri(resultUri)));
		}

		[Platform(Include = "Linux")]
		[TestCase("/tmp/foo", "bar", "file:///tmp/bar")]
		[TestCase("/tmp/foo/", "bar", "file:///tmp/foo/bar")]
		public void ResolveUri_Linux(string baseUriString, string relativeUri, string resultUri)
		{
			var sut = XmlUtils.GetResourceResolver("XMLUtilsTests");

			var baseUri = baseUriString == null ? null : new Uri(baseUriString);
			Assert.That(sut.ResolveUri(baseUri, relativeUri), Is.EqualTo(new Uri(resultUri)));
		}

		[TestCase(null, typeof(ArgumentNullException))]
		public void ResolveUri_ThrowsException(string relativeUri, Type expectedException)
		{
			var sut = XmlUtils.GetResourceResolver("XMLUtilsTests");

			Assert.That(() => sut.ResolveUri(null, relativeUri), Throws.InstanceOf(expectedException));
		}
	}
}
