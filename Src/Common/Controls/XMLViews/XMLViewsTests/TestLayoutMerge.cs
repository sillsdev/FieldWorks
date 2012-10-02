using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;

namespace XMLViewsTests
{
	[TestFixture]
	public class TestLayoutMerge: SIL.FieldWorks.Test.TestUtils.BaseTest
	{
		void TestMerge(string current, string user, string expectedOutput)
		{
			XmlDocument currentDoc = new XmlDocument();
			currentDoc.LoadXml(current);
			XmlDocument userDoc = new XmlDocument();
			userDoc.LoadXml(user);
			XmlDocument outputDoc = new XmlDocument();
			LayoutMerger merger = new LayoutMerger();
			XmlNode output = merger.Merge(currentDoc.DocumentElement, userDoc.DocumentElement, outputDoc);
			XmlDocument expectedDoc = new XmlDocument();
			expectedDoc.LoadXml(expectedOutput);
			Assert.IsTrue(XmlUtils.NodesMatch(output, expectedDoc.DocumentElement));
		}
		[Test]
		public void LayoutAttrsCopied()
		{
			TestMerge(@"<layout type='jtview' name='publishStemMinorPara'></layout>",
				@"<layout></layout>",
				@"<layout type='jtview' name='publishStemMinorPara'></layout>");
		}

		[Test]
		public void NonPartChildrenCopied()
		{
			TestMerge(
			@"<layout>
				<generate someAttr='nonsence'></generate>
			</layout>",
			@"<layout>
			</layout>",
			@"<layout>
				<generate someAttr='nonsence'></generate>
			</layout>");
		}

		// Cur1 is output first because before any node that occurs in wanted.
		// Cur3 is output next because in wanted.
		// Cur4 is not in output, but follows cur3 in input.
		// Cur2 is then copied in expected output order.
		// Cur3 is then copied again, but without another copy of node 4.
		// Custom3 is then copied from output, since $child is special.
		// Custom1 is then copied from input, with following custom1.
		// Cur5 and 6 must follow "generate", but are put in output order.
		// Custom4 is output in the second group, as that's where it occurs in the input.
		[Test]
		public void PartsCopiedAndReordered()
		{
			TestMerge(
			@"<layout>
				<part ref='cur1'/>
				<part ref='cur2'/>
				<part ref='cur3'/>
				<part ref='cur4'/>
				<part ref='$child' label='custom1'/>
				<part ref='$child' label='custom2'/>
				<generate/>
				<part ref='cur5'/>
				<part ref='cur6'/>
				<part ref='$child' label='custom4'/>
		   </layout>",
			@"<layout>
				<part ref='cur6'/>
				<part ref='cur5'/>
				<part ref='cur3'/>
				<part ref='cur2'/>
				<part ref='cur3'/>
				<part ref='$child' label='custom3'/>
				<part ref='$child' label='custom1'/>
				<part ref='$child' label='custom4'/>
			</layout>",
			@"<layout>
				<part ref='cur1'/>
				<part ref='cur3'/>
				<part ref='cur4'/>
				<part ref='cur2'/>
				<part ref='cur3'/>
				<part ref='$child' label='custom3'/>
				<part ref='$child' label='custom1'/>
				<part ref='$child' label='custom2'/>
				<generate/>
				<part ref='cur6'/>
				<part ref='cur5'/>
				<part ref='$child' label='custom4'/>
			</layout>");
		}

		[Test]
		public void SpecialAttrsOverridden()
		{
			TestMerge(
			@"<layout>
				<part ref='cur1' before='' after='' ws='analysis' style='oldStyle' number='true' numstyle='0' numsingle='false' visibility='ifData' extra='somevalue'/>
			</layout>",
			@"<layout>
				<part ref='cur1' before=' (' after=') ' sep=', ' ws='xkal' style='newStyle' showLabels='true' number='false' numstyle='9' numsingle='true' visibility='never'/>
			</layout>",
			@"<layout>
				<part ref='cur1' before=' (' after=') ' sep=', ' ws='xkal' style='newStyle' showLabels='true' number='false' numstyle='9' numsingle='true' visibility='never' extra='somevalue'/>
			</layout>");
		}
	}
}
