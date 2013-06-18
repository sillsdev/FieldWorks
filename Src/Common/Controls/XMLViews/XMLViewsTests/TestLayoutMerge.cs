using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;

namespace XMLViewsTests
{
	[TestFixture]
	public class TestLayoutMerge: SIL.FieldWorks.Test.TestUtils.BaseTest
	{
		void TestMerge(string newMaster, string user, string expectedOutput, string suffix)
		{
			var newMasterDoc = new XmlDocument();
			newMasterDoc.LoadXml(newMaster);
			var userDoc = new XmlDocument();
			userDoc.LoadXml(user);
			var outputDoc = new XmlDocument();
			var merger = new LayoutMerger();
			XmlNode output = merger.Merge(newMasterDoc.DocumentElement, userDoc.DocumentElement, outputDoc, suffix);
			var expectedDoc = new XmlDocument();
			expectedDoc.LoadXml(expectedOutput);
			Assert.IsTrue(XmlUtils.NodesMatch(output, expectedDoc.DocumentElement));
		}

		[Test]
		public void LayoutAttrsCopied()
		{
			TestMerge(
				@"<layout type='jtview' name='publishStemMinorPara'></layout>",
				@"<layout></layout>",
				@"<layout type='jtview' name='publishStemMinorPara'></layout>",
				string.Empty);
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
			</layout>",
			string.Empty);
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
			</layout>",
			string.Empty);
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
			</layout>",
			string.Empty);
		}

		[Test]
		public void UserCreatedConfigsPreserveParamSuffix()
		{
			TestMerge(
			@"<layout class='LexEntry' type='jtview' name='publishSummaryDefForStemVariantRef' extra='new'>
				<part ref='newRef' param='publishNewRefPart' />
			</layout>",
			@"<layout class='LexEntry' type='jtview' name='publishSummaryDefForStemVariantRef#Stem-980'>
				<part ref='oldRef' param='publishOldRefPart#Stem-980' />
			</layout>",
			@"<layout class='LexEntry' type='jtview' name='publishSummaryDefForStemVariantRef#Stem-980' extra='new'>
				<part ref='newRef' param='publishNewRefPart#Stem-980' />
			</layout>",
			"#Stem-980");
		}

		[Test]
		public void UserDuplicatedNodePreservesParamSuffix()
		{
			TestMerge(
			@"<layout class='LexExampleSentence' type='jtview' name='publishStem'>
				<part ref='TranslationsConfig' label='Translations' param='publishStem' css='translations' />
			</layout>",
			@"<layout class='LexExampleSentence' type='jtview' name='publishStem%01'>
				<part ref='TranslationsConfig' label='Translations' param='publishStem%01' css='translations' dup='1.0' />
			</layout>",
			@"<layout class='LexExampleSentence' type='jtview' name='publishStem%01'>
				<part ref='TranslationsConfig' label='Translations' param='publishStem%01' css='translations' dup='1.0' />
			</layout>",
			"%01");
		}

		[Test]
		public void UserDuplicatedNodePreservesPartRefLabelSuffix_Basic()
		{
			TestMerge(
			@"<layout name='publishStem'>
				<part ref='ExamplesConfig' label='Examples' param='publishStem' css='examples'/>
			</layout>",
			@"<layout name='publishStem'>
				<part ref='ExamplesConfig' label='Examples' param='publishStem' css='examples'/>
				<part ref='ExamplesConfig' label='Examples (1)' param='publishStem%01' css='examples' dup='1' />
			</layout>",
			@"<layout name='publishStem'>
				<part ref='ExamplesConfig' label='Examples' param='publishStem' css='examples'/>
				<part ref='ExamplesConfig' label='Examples (1)' param='publishStem%01' css='examples' dup='1' />
			</layout>",
			string.Empty);
		}

		[Test]
		public void UserDuplicatedNodePreservesPartRefLabelSuffix_Two()
		{
			TestMerge(
			@"<layout name='publishStem'>
				<part ref='ExamplesConfig' label='Examples' param='publishStem' css='examples'/>
			</layout>",
			@"<layout name='publishStem%01'>
				<part ref='ExamplesConfig' label='Examples (2)' param='publishStem%01' css='examples' dup='1' />
			</layout>",
			@"<layout name='publishStem%01'>
				<part ref='ExamplesConfig' label='Examples (2)' param='publishStem%01' css='examples' dup='1' />
			</layout>",
			"%01");
		}

		[Test]
		public void UserDuplicatedNodePreservesPartRefLabelSuffix_MoreComplicated()
		{
			TestMerge(
			@"<layout class='LexExampleSentence' type='jtview' name='publishStem'>
				<part ref='ExamplesConfig' label='Examples' param='publishStem' css='examples'/>
			</layout>",
			@"<layout class='LexExampleSentence' type='jtview' name='publishStem%01'>
				<part ref='ExamplesConfig' label='Examples (1) (1)' param='publishStem%01' css='examples' dup='1.1' />
			</layout>",
			@"<layout class='LexExampleSentence' type='jtview' name='publishStem%01'>
				<part ref='ExamplesConfig' label='Examples (1) (1)' param='publishStem%01' css='examples' dup='1.1' />
			</layout>",
			"%01");
		}

		/// <summary>
		/// LT-14650 XHTML output of configured dictionary showed up a bad layout merger
		/// </summary>
		[Test]
		public void SpecialAttrsOverridden_RelTypeSeqAndDup()
		{
			TestMerge(
			@"<layout>
				<part ref='LexReferencesConfig' label='Lexical Relations' before='' after='' sep=' ' visibility='ifData' param='publishStemSenseRef' css='relations' lexreltype='sense' extra='somevalue'/>
			</layout>",
			@"<layout>
				<part ref='LexReferencesConfig' label='Lexical Relations' before='' after=' ' sep='; ' visibility='ifdata' param='publishStemSenseRef' css='relations' lexreltype='sense' reltypeseq='+b7862f14-ea5e-11de-8d47-0013722f8dec,-b7921ac2-ea5e-11de-880d-0013722f8dec' unwanted='2' />
				<part ref='LexReferencesConfig' label='Lexical Relations (1)' before='' after=' ' sep='; ' visibility='ifdata' param='publishStemSenseRef%01' css='relations' lexreltype='sense' reltypeseq='-b7862f14-ea5e-11de-8d47-0013722f8dec,-b7921ac2-ea5e-11de-880d-0013722f8dec' unwanted='3' dup='1' />
			</layout>",
			@"<layout>
				<part ref='LexReferencesConfig' label='Lexical Relations' before='' after=' ' sep='; ' visibility='ifdata' param='publishStemSenseRef' css='relations' lexreltype='sense' extra='somevalue' reltypeseq='+b7862f14-ea5e-11de-8d47-0013722f8dec,-b7921ac2-ea5e-11de-880d-0013722f8dec' />
				<part ref='LexReferencesConfig' label='Lexical Relations (1)' before='' after=' ' sep='; ' visibility='ifdata' param='publishStemSenseRef%01' css='relations' lexreltype='sense' extra='somevalue' reltypeseq='-b7862f14-ea5e-11de-8d47-0013722f8dec,-b7921ac2-ea5e-11de-880d-0013722f8dec' dup='1' />
			</layout>",
			string.Empty);
		}
	}
}
