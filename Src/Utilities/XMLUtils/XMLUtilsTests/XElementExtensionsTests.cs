// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using NUnit.Framework;

namespace SIL.Utils
{
	/// <summary>
	/// Test all extentions for XElement.
	/// </summary>
	[TestFixture]
	public class XElementExtensionsTests
	{
		/// <summary>
		/// Make sure XElement clones are the same as the source.
		/// </summary>
		[Test]
		public void CloneIsTheSameAsSource()
		{
			DoCloneAndCheckIt("<stuff />", "stuff");

			DoCloneAndCheckIt("<stuff attr='myAttrValue' />", "myAttrValue");

			DoCloneAndCheckIt("<stuff attr='myAttrValue' ><child attr='myChildAttrValue' /></stuff>", "myChildAttrValue");

			DoCloneAndCheckIt("<stuff attr='myAttrValue' ><!-- Some comment --><child attr='myChildAttrValue' /></stuff>", "<!-- Some comment -->");
		}

		private static void DoCloneAndCheckIt(string sourceData, string testData)
		{
			StringAssert.Contains(testData, sourceData);
			var sourceElement = XElement.Parse(sourceData);
			var clone = sourceElement.Clone();
			Assert.AreNotSame(sourceElement, clone);
			Assert.AreEqual(clone.ToString(), sourceElement.ToString());
			StringAssert.Contains(testData, clone.ToString());
		}

		/// <summary>
		/// Make sure XElement "GetOuterXml" (aka: 'OuterXml') is the same as the ToString() contents.
		/// </summary>
		[Test]
		public void OuterXmlIsSameAsToStringText()
		{
			const string outerXmlData = "<stuff attr='myAttrValue' ><!-- Some comment --><child attr='myChildAttrValue' /></stuff>";
			const string testData = "<!-- Some comment -->";
			StringAssert.Contains(testData, outerXmlData);
			var element = XElement.Parse(outerXmlData);
			Assert.AreEqual(element.ToString(), element.GetOuterXml());
			StringAssert.Contains(testData, element.GetOuterXml());
		}

		/// <summary>
		/// Make sure XElement "GetInnerXml" (aka: 'InnerXml') is the same as combined child node contents (sans comments, if present).
		/// </summary>
		[Test]
		public void InnerXmlIsCombinedChildElementXml()
		{
			var outerTextData = "<stuff attr='myAttrValue' ><!-- Some comment --><child attr=\"myChildAttrValue\" /><child2 attr=\"myChild2AttrValue\" /></stuff>";
			const string testData = "<child attr=\"myChildAttrValue\" /><child2 attr=\"myChild2AttrValue\" />";
			StringAssert.Contains(testData, outerTextData);
			var element = XElement.Parse(outerTextData);
			Assert.AreEqual(testData, element.GetInnerXml());

			outerTextData = outerTextData.Replace("<!-- Some comment -->", string.Empty);
			StringAssert.DoesNotContain("<!-- Some comment -->", outerTextData);
			element = XElement.Parse(outerTextData);
			Assert.AreEqual(testData, element.GetInnerXml());
		}

		/// <summary>
		/// Make sure XElement "GetInnerText" (aka: 'InnerText') is the same as self's Value concatenated child node Value properties (sans comments, if present).
		/// </summary>
		[Test]
		public void InnerTextIsConcatenatedElementValues()
		{
			var outerTextData = "<stuff attr='myAttrValue' ><!-- Some comment -->My Value<child attr=\"myChildAttrValue\" >Child Value</child><child2 attr=\"myChild2AttrValue\" >Child2 Value</child2></stuff>";
			const string stuffExpected = "My Value";
			StringAssert.Contains(stuffExpected, outerTextData);
			const string childExpected = "Child Value";
			StringAssert.Contains(childExpected, outerTextData);
			const string child2Expected = "Child2 Value";
			StringAssert.Contains(child2Expected, outerTextData);
			var element = XElement.Parse(outerTextData);
			Assert.AreEqual(string.Concat(stuffExpected, childExpected, child2Expected), element.GetInnerText());

			outerTextData = outerTextData.Replace("<!-- Some comment -->", string.Empty);
			StringAssert.DoesNotContain("<!-- Some comment -->", outerTextData);
			element = XElement.Parse(outerTextData);
			Assert.AreEqual(string.Concat(stuffExpected, childExpected, child2Expected), element.GetInnerText());
		}
	}
}
