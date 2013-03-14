using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;

namespace XMLViewsTests
{
	[TestFixture]
	public class XmlBrowseViewBaseVcTests
	{
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
	}
}
