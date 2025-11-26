// Copyright (c) 2015-2025 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace XMLViewsTests
{
	[TestFixture]
	public class XmlBrowseViewBaseVcTests
	{
		[Test]
		public void MigrateVersion18Columns_ExtendedNoteColumns()
		{
			var input = @"<column layout='ExtNoteType' label='Ext. Note Type' multipara='true' ws='$ws=analysis' transduce='LexExtendedNote.ExtendedNoteType' ghostListField='LexDb.AllPossibleExtendedNotes' editable='false' visibility='dialog' />
				<column layout='ExtNoteDiscussion' label='Ext. Note Discussion' multipara='true' ws='$ws=analysis' transduce='LexExtendedNote.Discussion' ghostListField='LexDb.AllPossibleExtendedNotes' editable='false' visibility='dialog' />
				".Replace("'", "\"");
			var expectedOutput = @"<column layout='ExtNoteType' label='Ext. Note - Type' multipara='true' ghostListField='LexDb.AllExtendedNoteTargets' visibility='dialog' list='LexDb.ExtendedNoteTypes' field='LexExtendedNote.ExtendedNoteType' bulkEdit='atomicFlatListItem' displayWs='best vernoranal' displayNameProperty='ShortNameTSS'/>
				<column layout='ExtNoteDiscussion' label='Ext. Note - Discussion' multipara='true' ws='$ws=analysis' transduce='LexExtendedNote.Discussion' ghostListField='LexDb.AllExtendedNoteTargets' editable='true' visibility='dialog' />
				".Replace("'", "\"");
			var output = XmlBrowseViewBaseVc.FixVersion18Columns(input);
			Assert.That(output, Is.EqualTo(expectedOutput), "transduce attributes should be added");
		}

		[Test]
		public void MigrateVersion16Columns_Etymology()
		{
			var input = @"<column layout='EtymologyGloss' label='Etymology - Gloss'  multipara='true' ws='$ws=analysis' editable='false' visibility='dialog' />
	<column layout='EtymologySource' label='Etymology - Source'  multipara='true' editable='false' visibility='dialog'/>
	<column layout='EtymologyForm' label='Etymology - Form'  multipara='true' ws='$ws=vernacular' editable='false' visibility='dialog' />
	<column layout='EtymologyComment' label='Etymology - Comment'  multipara='true' ws='$ws=analysis' editable='false' visibility='dialog' />
".Replace("'", "\"");
			var expectedOutput = @"<column layout='EtymologyGloss' label='Etymology - Gloss'  multipara='true' ws='$ws=analysis' editable='false' visibility='dialog' transduce='LexEntry.Etymology.Gloss'/>
	<column layout='EtymologySource' label='Etymology - Source'  multipara='true' editable='false' visibility='dialog' transduce='LexEntry.Etymology.Source'/>
	<column layout='EtymologyForm' label='Etymology - Form'  multipara='true' ws='$ws=vernacular' editable='false' visibility='dialog' transduce='LexEntry.Etymology.Form'/>
	<column layout='EtymologyComment' label='Etymology - Comment'  multipara='true' ws='$ws=analysis' editable='false' visibility='dialog' transduce='LexEntry.Etymology.Comment'/>
".Replace("'", "\"");
			var output = XmlBrowseViewBaseVc.FixVersion16Columns(input);
			Assert.That(output, Is.EqualTo(expectedOutput), "transduce attributes should be added");
		}

		[Test]
		public void MigrateVersion16Columns_EntryTypes()
		{
			var input = @"<column layout='ComplexEntryTypesBrowse' ws='$ws=analysis' originalWs='$ws=analysis' label='Complex Form Types' multipara='true' editable='false'  visibility='dialog'
			chooserFilter='complexListMultiple' canChooseEmpty='true'
			ghostListField='LexDb.AllComplexEntryRefPropertyTargets'
			bulkEdit='complexEntryTypes' field='LexEntryRef.ComplexEntryTypes' bulkDelete='false' list='LexDb.ComplexEntryTypes' displayNameProperty='ShortNameTSS' displayWs='analysis'/>
			<column layout='VariantEntryTypesBrowse' ws='$ws=analysis' originalWs='$ws=analysis' label='Variant Types' multipara='true' editable='false' visibility='dialog'
			chooserFilter='complexListMultiple' canChooseEmpty='true'
			ghostListField='LexDb.AllVariantEntryRefPropertyTargets'
			bulkEdit='variantEntryTypes' field='LexEntryRef.VariantEntryTypes' list='LexDb.VariantEntryTypes' displayNameProperty='ShortNameTSS' displayWs='analysis'/>
".Replace("'", "\"");
			var expectedOutput = @"<column layout='ComplexEntryTypesBrowse' ws='$ws=best analysis' originalWs='$ws=best analysis' label='Complex Form Types' multipara='true' editable='false'  visibility='dialog'
			chooserFilter='complexListMultiple' canChooseEmpty='true'
			ghostListField='LexDb.AllComplexEntryRefPropertyTargets'
			bulkEdit='complexEntryTypes' field='LexEntryRef.ComplexEntryTypes' bulkDelete='false' list='LexDb.ComplexEntryTypes' displayNameProperty='ShortNameTSS' displayWs='analysis'/>
			<column layout='VariantEntryTypesBrowse' ws='$ws=best analysis' originalWs='$ws=best analysis' label='Variant Types' multipara='true' editable='false' visibility='dialog'
			chooserFilter='complexListMultiple' canChooseEmpty='true'
			ghostListField='LexDb.AllVariantEntryRefPropertyTargets'
			bulkEdit='variantEntryTypes' field='LexEntryRef.VariantEntryTypes' list='LexDb.VariantEntryTypes' displayNameProperty='ShortNameTSS' displayWs='analysis'/>
".Replace("'", "\"");
			var output = XmlBrowseViewBaseVc.FixVersion16Columns(input);
			Assert.That(output, Is.EqualTo(expectedOutput), "ws and original Ws attributes should be changed to best analysis");
		}

		[Test]
		public void MigrateVersion16Columns_CustomPossAtom()
		{
			var input = @"
	<generate class='LexEntry' fieldType='atom' destClass='CmPossibility' restrictions='customOnly'>
		<column layout='CustomPossAtomForEntry_$fieldName' label='$label' visibility='menu'
				bulkEdit='atomicFlatListItem' field='LexEntry.$fieldName' list='$targetList'/>
	</generate>
	<generate class='LexSense' fieldType='atom' destClass='CmPossibility' restrictions='customOnly'>
		<column layout='CustomPossAtomForSense_$fieldName' label='$label' visibility='menu'
				bulkEdit='atomicFlatListItem' field='LexSense.$fieldName' list='$targetList'/>
	</generate>
			<generate class='MoForm' fieldType='atom' destClass='CmPossibility' restrictions='customOnly'>
		<column layout='CustomPossAtomForAllomorph_$fieldName' label='$label' visibility='menu'
				bulkEdit='atomicFlatListItem' field='MoForm.$fieldName' list='$targetList'/>
	</generate>
		<generate class='LexExampleSentence' fieldType='atom' destClass='CmPossibility' restrictions='customOnly'>
		<column layout='CustomPossAtomForExample_$fieldName' label='$label' visibility='menu'
				bulkEdit='atomicFlatListItem' field='LexExampleSentence.$fieldName' list='$targetList'/>
	</generate>
".Replace("'", "\"");
			var expectedOutput = @"
	<generate class='LexEntry' fieldType='atom' destClass='CmPossibility' restrictions='customOnly'>
		<column layout='CustomPossAtomForEntry_$fieldName' label='$label' visibility='menu'
				bulkEdit='atomicFlatListItem' field='LexEntry.$fieldName' list='$targetList' displayNameProperty='ShortNameTSS'/>
	</generate>
	<generate class='LexSense' fieldType='atom' destClass='CmPossibility' restrictions='customOnly'>
		<column layout='CustomPossAtomForSense_$fieldName' label='$label' visibility='menu'
				bulkEdit='atomicFlatListItem' field='LexSense.$fieldName' list='$targetList' displayNameProperty='ShortNameTSS'/>
	</generate>
			<generate class='MoForm' fieldType='atom' destClass='CmPossibility' restrictions='customOnly'>
		<column layout='CustomPossAtomForAllomorph_$fieldName' label='$label' visibility='menu'
				bulkEdit='atomicFlatListItem' field='MoForm.$fieldName' list='$targetList' displayNameProperty='ShortNameTSS'/>
	</generate>
		<generate class='LexExampleSentence' fieldType='atom' destClass='CmPossibility' restrictions='customOnly'>
		<column layout='CustomPossAtomForExample_$fieldName' label='$label' visibility='menu'
				bulkEdit='atomicFlatListItem' field='LexExampleSentence.$fieldName' list='$targetList' displayNameProperty='ShortNameTSS'/>
	</generate>
".Replace("'", "\"");
			var output = XmlBrowseViewBaseVc.FixVersion16Columns(input);
			Assert.That(output, Is.EqualTo(expectedOutput), "displayNameProperty should be added");
		}

		[Test]
		public void MigrateVersion16Columns_ConfigureWs()
		{
			var input = @"
	<column layout='CVPattern' label='CV Patterns' multipara='true' editable='false' ws='pronunciation'
			transduce='LexPronunciation.CVPattern' ghostListField='LexDb.AllPossiblePronunciations'  cansortbylength='true' visibility='dialog'/>
	<column layout='Tone' label='Tones' multipara='true' editable='false' ws='pronunciation'
			transduce='LexPronunciation.Tone' ghostListField='LexDb.AllPossiblePronunciations' cansortbylength='true' visibility='dialog'/>
	<column layout='ScientificNameForSense' label='Scientific Names' multipara='true' ws='analysis' transduce='LexSense.ScientificName' visibility='dialog' />
	<column layout='SourceForSense'  label='Sources' ws='analysis' multipara='true' transduce='LexSense.Source' visibility='dialog' />
".Replace("'", "\"");
			var expectedOutput = @"
	<column layout='CVPattern' label='CV Patterns' multipara='true' editable='false' ws='$ws=pronunciation'
			transduce='LexPronunciation.CVPattern' ghostListField='LexDb.AllPossiblePronunciations'  cansortbylength='true' visibility='dialog'/>
	<column layout='Tone' label='Tones' multipara='true' editable='false' ws='$ws=pronunciation'
			transduce='LexPronunciation.Tone' ghostListField='LexDb.AllPossiblePronunciations' cansortbylength='true' visibility='dialog'/>
	<column layout='ScientificNameForSense' label='Scientific Names' multipara='true' ws='$ws=analysis' transduce='LexSense.ScientificName' visibility='dialog' />
	<column layout='SourceForSense'  label='Sources' ws='$ws=analysis' multipara='true' transduce='LexSense.Source' visibility='dialog' />
".Replace("'", "\"");
			var output = XmlBrowseViewBaseVc.FixVersion16Columns(input);
			Assert.That(output, Is.EqualTo(expectedOutput), "$ws= should be added to various fields");
		}

		[Test]
		public void GetHeaderLabels_ReturnsColumnSpecLabels()
		{
			var testColumns =
@"<columns>
	<column label='Ref' width='60000'>
		<span>
			<properties>
				<editable value='false'/>
			</properties>
			<string class='FakeOccurrence' field='Reference' ws='$ws=best vernoranal'/>
		</span>
	</column>
	<column label='Occurrence' sortType='occurrenceInContext' width='415000' multipara='true'>
		<concpara min='FakeOccurrence.BeginOffset' lim='FakeOccurrence.EndOffset' align='144000'>
			<properties>
				<editable value='false'/>
			</properties>
			<obj class='FakeOccurrence' field='TextObject' layout='empty'>
				<choice>
				<where is='StTxtPara'>
					<string class='StTxtPara' field='Contents' ws='$ws=best vernacular'/>
				</where>
				<where is='CmPicture'>
					<string class='CmPicture' field='Caption' ws='vernacular'/>
				</where>
				</choice>
			</obj>
		</concpara>
	</column>
</columns>";

			var columnDoc = new XmlDocument();
			columnDoc.LoadXml(testColumns);
			var testVc = new XmlBrowseViewBaseVc
			{
				ColumnSpecs = new List<XmlNode>(columnDoc.DocumentElement.GetElementsByTagName("column").OfType<XmlNode>())
			};

			var columnLabels = XmlBrowseViewBaseVc.GetHeaderLabels(testVc);

			CollectionAssert.AreEqual(new List<string> { "Ref", "Occurrence" }, columnLabels);
		}

		/// <remarks>TODO (Hasso) 2025.11: This test needs further setup to find the layout XmlNode in the LayoutCache</remarks>
		public void IsValidColumnSpec_HasLayout_TODO()
		{
			var vc = new XmlBrowseViewBaseVc { PossibleColumnSpecs = new List<XmlNode>(), ListItemsClass = -1 /* can't be 0 */ };
			var possibleColumns = new XmlDocument();
			possibleColumns.LoadXml(@"<columns>
	<column label='Name' width='13%' layout='Name' ws='$ws=best analysis' field='Name'/>
	<column label='Abbreviation' width='10%' layout='Abbreviation' ws='$ws=best analysis' field='Abbreviation'/>
	<column label='Values' width='24%' multipara='true' layout='TypeOrValues' visibility='menu'/>
</columns>");
			foreach (XmlNode node in possibleColumns.DocumentElement.GetElementsByTagName("column"))
			{
				vc.PossibleColumnSpecs.Add(node);
			}

			var validColumns = new XmlDocument();
			validColumns.LoadXml(@"<root version='18'>
	<column label='Name' width='13%' layout='Name' ws='$ws=best analysis' field='Name'/>
	<column width='10%' layout='Abbreviation' ws='$ws=en' field='Abbreviation' originalWs='best analysis' originalLabel='Abbreviation' label='Abreviatura (Eng)'/>
	<column width='10%' layout='Abbreviation' ws='$ws=es' field='Abbreviation' originalWs='best analysis' originalLabel='Abbreviation' label='Abreviatura (Spa)'/>
</root>");

			// SUT
			foreach (XmlNode node in validColumns.DocumentElement.GetElementsByTagName("column"))
			{
				Assert.IsTrue(vc.IsValidColumnSpec(node), $"Should have found this node to be valid: {node.OuterXml}");
			}
		}

		/// <summary>
		/// Tests that IsValidColumnSpec can identify valid columns based on their labels or originalLabels.
		/// Artificially skips the layout check because that requires a more complex setup.
		/// LT-22265: Invalid columns should not be displayed.
		/// </summary>
		[Test]
		public void IsValidColumnSpec_MatchesLabels()
		{
			var vc = new XmlBrowseViewBaseVc { PossibleColumnSpecs = new List<XmlNode>(), ListItemsClass = -1 /* can't be 0 */ };
			var possibleColumns = new XmlDocument();
			possibleColumns.LoadXml("<columns><column label='Ref'/><column label='Other'/></columns>");
			foreach (XmlNode node in possibleColumns.DocumentElement.GetElementsByTagName("column"))
			{
				vc.PossibleColumnSpecs.Add(node);
			}

			var validColumns = new XmlDocument();
			validColumns.LoadXml("<root><column label='Ref'/><column label='Other (Best Ana)' originalLabel='Other'/></root>");

			// SUT
			foreach (XmlNode node in validColumns.DocumentElement.GetElementsByTagName("column"))
			{
				Assert.IsTrue(vc.IsValidColumnSpec(node), $"Should have found this node to be valid: {node.OuterXml}");
			}

			var invalidColumns = new XmlDocument();
			invalidColumns.LoadXml("<root><column label='DoesNotExist' originalLabel='MayHaveExistedBefore'/></root>");
			var invalidColumn = invalidColumns.DocumentElement.SelectSingleNode("column");

			// SUT
			Assert.IsFalse(vc.IsValidColumnSpec(invalidColumn), $"Should have found this node to be invalid: {invalidColumn.OuterXml}");
		}
	}
}