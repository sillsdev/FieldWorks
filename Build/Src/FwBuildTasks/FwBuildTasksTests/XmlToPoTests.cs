// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using System.IO;
using System.Xml.Linq;
using SIL.FieldWorks.Build.Tasks.Localization;

namespace SIL.FieldWorks.Build.Tasks.FwBuildTasksTests
{
	[TestFixture]
	public class XmlToPoTests
	{
		[Test]
		public void TestComputeAutoCommentFilePath()
		{
			var result = XmlToPo.ComputeAutoCommentFilePath(@"E:\fwrepo/fw\DistFiles",
				@"E:\fwrepo\fw\DistFiles\Language Explorer\DefaultConfigurations\Dictionary\Hybrid.fwdictconfig");
			Assert.That(result, Is.EqualTo(@"/Language Explorer/DefaultConfigurations/Dictionary/Hybrid.fwdictconfig"));

			result = XmlToPo.ComputeAutoCommentFilePath(@"C:\fwrepo\fw\DistFiles",
				@"E:\fwrepo\fw\DistFiles\Language Explorer\DefaultConfigurations\Dictionary\Hybrid.fwdictconfig");
			Assert.That(result, Is.EqualTo(@"E:/fwrepo/fw/DistFiles/Language Explorer/DefaultConfigurations/Dictionary/Hybrid.fwdictconfig"));

			result = XmlToPo.ComputeAutoCommentFilePath("/home/steve/fwrepo/fw/DistFiles",
				"/home/steve/fwrepo/fw/DistFiles/Language Explorer/Configuration/Parts/LexEntry.fwlayout");
			Assert.That(result, Is.EqualTo("/Language Explorer/Configuration/Parts/LexEntry.fwlayout"));

			result = XmlToPo.ComputeAutoCommentFilePath("/home/john/fwrepo/fw/DistFiles",
				"/home/steve/fwrepo/fw/DistFiles/Language Explorer/Configuration/Parts/LexEntry.fwlayout");
			Assert.That(result, Is.EqualTo("/home/steve/fwrepo/fw/DistFiles/Language Explorer/Configuration/Parts/LexEntry.fwlayout"));
		}

#region TestData
		private static readonly string FwlayoutData =
"<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + Environment.NewLine +
"<LayoutInventory xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:noNamespaceSchemaLocation='ViewsLayout.xsd'>" + Environment.NewLine +
"  <layout class=\"LexEntry\" type=\"detail\" name=\"Normal\">" + Environment.NewLine +
"    <part label=\"Lexeme Form\" ref=\"LexemeForm\"/>" + Environment.NewLine +
"    <part label=\"Citation Form\" ref=\"CitationFormAllV\"/>" + Environment.NewLine +
"    <part ref=\"ComplexFormEntries\" visibility=\"ifdata\"/>" + Environment.NewLine +
"    <part ref=\"EntryRefs\" param=\"Normal\" visibility=\"ifdata\"/>" + Environment.NewLine +
"    <part ref=\"SummaryDefinitionAllA\" visibility=\"ifdata\"/>" + Environment.NewLine +
"    <part ref=\"CurrentLexReferences\"   visibility=\"ifdata\" />" + Environment.NewLine +
"    <part customFields=\"here\" />" + Environment.NewLine +
"    <part ref=\"ImportResidue\" label=\"Import Residue\" visibility=\"ifdata\"/>" + Environment.NewLine +
"    <part ref=\"DateCreatedAllA\"  visibility=\"never\"/>" + Environment.NewLine +
"    <part ref=\"DateModifiedAllA\"  visibility=\"never\"/>" + Environment.NewLine +
"    <part ref=\"Messages\" visibility=\"always\"/>" + Environment.NewLine +
"      <part ref=\"Senses\" param=\"Normal\" expansion=\"expanded\"/>" + Environment.NewLine +
"    <part ref=\"VariantFormsSection\" expansion=\"expanded\" label=\"Variants\" menu=\"mnuDataTree-VariantForms\" hotlinks=\"mnuDataTree-VariantForms-Hotlinks\">" + Environment.NewLine +
"      <indent><part ref=\"VariantForms\"/></indent>" + Environment.NewLine +
"    </part>" + Environment.NewLine +
"    <part ref=\"AlternateFormsSection\" expansion=\"expanded\" label=\"Allomorphs\" menu=\"mnuDataTree-AlternateForms\" hotlinks=\"mnuDataTree-AlternateForms-Hotlinks\">" + Environment.NewLine +
"      <indent><part ref=\"AlternateForms\" param=\"Normal\"/></indent>" + Environment.NewLine +
"    </part>" + Environment.NewLine +
"    <part ref=\"GrammaticalFunctionsSection\" label=\"Grammatical Info. Details\" menu=\"mnuDataTree-Help\" hotlinks=\"mnuDataTree-Help\">" + Environment.NewLine +
"      <indent><part ref=\"MorphoSyntaxAnalyses\" param=\"Normal\"/></indent>" + Environment.NewLine +
"    </part>" + Environment.NewLine +
"    <part ref=\"PublicationSection\" label=\"Publication Settings\" menu=\"mnuDataTree-Help\" hotlinks=\"mnuDataTree-Help\">" + Environment.NewLine +
"      <indent>" + Environment.NewLine +
"        <part ref=\"PublishIn\"   visibility=\"always\" />" + Environment.NewLine +
"        <part ref=\"ShowMainEntryIn\" label=\"Show As Headword In\" visibility=\"always\" />" + Environment.NewLine +
"        <part ref=\"EntryRefs\" param=\"Publication\" visibility=\"ifdata\"/>" + Environment.NewLine +
"      </indent>" + Environment.NewLine +
"    </part>" + Environment.NewLine +
"  </layout>" + Environment.NewLine +
"  <layout class=\"LexEntry\" type=\"detail\" name=\"AsVariantForm\">" + Environment.NewLine +
"    <part ref=\"AsVariantForm\"/>" + Environment.NewLine +
"  </layout>" + Environment.NewLine +
"  <layout class=\"LexEntry\" type=\"jtview\" name=\"CrossRefPub\">" + Environment.NewLine +
"    <part ref=\"MLHeadWordPub\" label=\"Headword\" before=\" CrossRef:\" after=\". \" visibility=\"ifdata\" ws=\"vernacular\" sep=\"; \" showLabels=\"false\" style=\"Dictionary-CrossReferences\"/>" + Environment.NewLine +
"  </layout>" + Environment.NewLine +
"  <layout class=\"LexEntry\" type=\"jtview\" name=\"SubentryUnderPub\">" + Environment.NewLine +
"    <part ref=\"MLHeadWordPub\" label=\"Headword\" before=\" [\" after=\"]\" visibility=\"ifdata\" ws=\"vernacular\" sep=\"; \" hideConfig=\"true\" showLabels=\"false\" style=\"Dictionary-CrossReferences\"/>" + Environment.NewLine +
"  </layout>" + Environment.NewLine +
"</LayoutInventory>";
#endregion

		[Test]
		public void TestReadingDetailConfigData()
		{
			var poStrings = new List<POString>();
			var xdoc = XDocument.Parse(FwlayoutData);
			Assert.That(xdoc.Root, Is.Not.Null);
			//SUT
			XmlToPo.ProcessConfigElement(xdoc.Root, "/Language Explorer/Configuration/Parts/LexEntry.fwlayout", poStrings);
			Assert.That(poStrings.Count, Is.EqualTo(14));
			var postr5 = poStrings[5];
			Assert.That(postr5, Is.Not.Null, "Detail Config string[5] has data");
			Assert.That(postr5.MsgId, Is.Not.Null, "Detail Config string[5].MsgId");
			Assert.That(postr5.MsgId.Count, Is.EqualTo(1), "Detail Config string[5].MsgId.Count");
			Assert.That(postr5.MsgId[0], Is.EqualTo("Grammatical Info. Details"), "Detail Config string[5].MsgId[0]");
			Assert.That(postr5.MsgIdAsString(), Is.EqualTo("Grammatical Info. Details"), "Detail Config string[5] is 'Grammatical Info. Details'");
			Assert.That(postr5.HasEmptyMsgStr, Is.True, "Detail Config string[5].HasEmptyMsgStr");
			Assert.That(postr5.UserComments, Is.Null, "Detail Config string[5].UserComments");
			Assert.That(postr5.References, Is.Null, "Detail Config string[5].References");
			Assert.That(postr5.Flags, Is.Null, "Detail Config string[5].Flags");
			Assert.That(postr5.AutoComments, Is.Not.Null, "Detail Config string[5].AutoComments");
			Assert.That(postr5.AutoComments.Count, Is.EqualTo(1), "Detail Config string[5].AutoComments.Count");
			Assert.That(postr5.AutoComments[0], Is.EqualTo("/Language Explorer/Configuration/Parts/LexEntry.fwlayout::/LayoutInventory/layout[\"LexEntry-detail-Normal\"]/part[@ref=\"GrammaticalFunctionsSection\"]/@label"), "Detail Config string[5].AutoComments[0]");

			var postr8 = poStrings[8];
			Assert.That(postr8, Is.Not.Null, "Detail Config string[8] has data");
			Assert.That(postr8.MsgId, Is.Not.Null, "Detail Config string[8].MsgId");
			Assert.That(postr8.MsgId.Count, Is.EqualTo(1), "Detail Config string[8].MsgId.Count");
			Assert.That(postr8.MsgId[0], Is.EqualTo("Headword"), "Detail Config string[8].MsgId[0]");
			Assert.That(poStrings[8].MsgIdAsString(), Is.EqualTo("Headword"), "Detail Config string[8] is 'Headword'");
			Assert.That(postr8.HasEmptyMsgStr, Is.True, "Detail Config string[8].HasEmptyMsgStr");
			Assert.That(postr8.UserComments, Is.Null, "Detail Config string[8].UserComments");
			Assert.That(postr8.References, Is.Null, "Detail Config string[8].References");
			Assert.That(postr8.Flags, Is.Null, "Detail Config string[8].Flags");
			Assert.That(postr8.AutoComments, Is.Not.Null, "Detail Config string[8].AutoComments");
			Assert.That(postr8.AutoComments.Count, Is.EqualTo(1), "Detail Config string[8].AutoComments.Count");
			Assert.That(postr8.AutoComments[0], Is.EqualTo("/Language Explorer/Configuration/Parts/LexEntry.fwlayout::/LayoutInventory/layout[\"LexEntry-jtview-CrossRefPub\"]/part[@ref=\"MLHeadWordPub\"]/@label"), "Detail Config string[8].AutoComments[0]");

			var postr10 = poStrings[10];
			Assert.That(postr10, Is.Not.Null, "Detail Config string[10] has data");
			Assert.That(postr10.MsgId, Is.Not.Null, "Detail Config string[10].MsgId");
			Assert.That(postr10.MsgId.Count, Is.EqualTo(1), "Detail Config string[10].MsgId.Count");
			Assert.That(postr10.MsgId[0], Is.EqualTo(" CrossRef:"), "Detail Config string[10].MsgId[0]");
			Assert.That(poStrings[10].MsgIdAsString(), Is.EqualTo(" CrossRef:"), "Detail Config string[8] is ' CrossRef:'");
			Assert.That(postr10.HasEmptyMsgStr, Is.True, "Detail Config string[10].HasEmptyMsgStr");
			Assert.That(postr10.UserComments, Is.Null, "Detail Config string[10].UserComments");
			Assert.That(postr10.References, Is.Null, "Detail Config string[10].References");
			Assert.That(postr10.Flags, Is.Null, "Detail Config string[10].Flags");
			Assert.That(postr10.AutoComments, Is.Not.Null, "Detail Config string[10].AutoComments");
			Assert.That(postr10.AutoComments.Count, Is.EqualTo(1), "Detail Config string[10].AutoComments.Count");
			Assert.That(postr10.AutoComments[0], Is.EqualTo("/Language Explorer/Configuration/Parts/LexEntry.fwlayout::/LayoutInventory/layout[\"LexEntry-jtview-CrossRefPub\"]/part[@ref=\"MLHeadWordPub\"]/@before"), "Detail Config string[10].AutoComments[0]");

			var postr11 = poStrings[11];
			Assert.That(postr11, Is.Not.Null, "Detail Config string[11] has data");
			Assert.That(postr11.MsgId, Is.Not.Null, "Detail Config string[11].MsgId");
			Assert.That(postr11.MsgId.Count, Is.EqualTo(1), "Detail Config string[11].MsgId.Count");
			Assert.That(postr11.MsgId[0], Is.EqualTo("Headword"), "Detail Config string[11].MsgId[0]");
			Assert.That(poStrings[11].MsgIdAsString(), Is.EqualTo("Headword"), "Detail Config string[8] is 'Headword'");
			Assert.That(postr11.HasEmptyMsgStr, Is.True, "Detail Config string[11].HasEmptyMsgStr");
			Assert.That(postr11.UserComments, Is.Null, "Detail Config string[11].UserComments");
			Assert.That(postr11.References, Is.Null, "Detail Config string[11].References");
			Assert.That(postr11.Flags, Is.Null, "Detail Config string[11].Flags");
			Assert.That(postr11.AutoComments, Is.Not.Null, "Detail Config string[11].AutoComments");
			Assert.That(postr11.AutoComments.Count, Is.EqualTo(1), "Detail Config string[11].AutoComments.Count");
			Assert.That(postr11.AutoComments[0], Is.EqualTo("/Language Explorer/Configuration/Parts/LexEntry.fwlayout::/LayoutInventory/layout[\"LexEntry-jtview-SubentryUnderPub\"]/part[@ref=\"MLHeadWordPub\"]/@label"), "Detail Config string[11].AutoComments[0]");
		}

#region DictConfigData
		private const string DictConfigData = @"<?xml version=""1.0"" encoding=""utf-8""?>
<DictionaryConfiguration name=""Root-based (complex forms as subentries)"" allPublications=""true"" version=""1"" lastModified=""2014-10-07"">
	<ConfigurationItem name=""Main Entry"" style=""Dictionary-Normal"" isEnabled=""true"" field=""LexEntry"" cssClassNameOverride=""entry"">
		<ParagraphOptions paragraphStyle=""Dictionary-Normal"" continuationParagraphStyle=""Dictionary-Continuation"" />
		<ConfigurationItem name=""Headword"" between="" "" after=""	"" style=""Dictionary-Headword"" isEnabled=""true"" field=""MLHeadWord"" cssClassNameOverride=""mainheadword"">
			<WritingSystemOptions writingSystemType=""vernacular"" displayWSAbreviation=""false"">
				<Option id=""vernacular"" isEnabled=""true""/>
			</WritingSystemOptions>
		</ConfigurationItem>
		<ConfigurationItem name=""Summary Definition"" before="" "" between="" "" after="" "" isEnabled=""false"" field=""SummaryDefinition""/>
		<ConfigurationItem name=""Senses"" between=""	"" after="" "" isEnabled=""true"" field=""SensesOS"" cssClassNameOverride=""senses"">
			<SenseOptions displayEachSenseInParagraph=""false"" numberStyle=""Dictionary-SenseNumber"" numberBefore="""" numberAfter="") "" numberingStyle=""%d"" numberSingleSense=""false"" showSingleGramInfoFirst=""true""/>
			<ConfigurationItem name=""Grammatical Info."" after="" "" style=""Dictionary-Contrasting"" isEnabled=""true"" field=""MorphoSyntaxAnalysisRA"" cssClassNameOverride=""morphosyntaxanalysis"">
				<ConfigurationItem name=""Gram Info (Name)"" between="" "" after="" "" isEnabled=""false"" field=""InterlinearNameTSS"" cssClassNameOverride=""graminfoname""/>
				<ConfigurationItem name=""Gram Info (Abbrev)"" between="" "" after="" "" isEnabled=""false"" field=""InterlinearAbbrTSS"" cssClassNameOverride=""graminfoabbrev""/>
				<ConfigurationItem name=""Category Info."" between="" "" after="" "" isEnabled=""true"" field=""MLPartOfSpeech""/>
			</ConfigurationItem>
			<ConfigurationItem name=""Definition (or Gloss)"" between="" "" after="" "" isEnabled=""true"" field=""DefinitionOrGloss""/>
			<ConfigurationItem name=""Semantic Domains"" before=""(sem. domains: "" between="", "" after="".) "" isEnabled=""true"" field=""SemanticDomainsRC"" cssClassNameOverride=""semanticdomains"">
				<ConfigurationItem name=""Abbreviation"" between="" "" after="" - "" isEnabled=""true"" field=""Abbreviation""/>
				<ConfigurationItem name=""Name"" between="" "" isEnabled=""true"" field=""Name""/>
			</ConfigurationItem>
		</ConfigurationItem>
		<ConfigurationItem name=""Date Created"" before=""created on: "" after="" "" isEnabled=""false"" field=""DateCreated""/>
		<ConfigurationItem name=""Date Modified"" before=""modified on: "" after="" "" isEnabled=""false"" field=""DateModified""/>
	</ConfigurationItem>
	<ConfigurationItem name=""Minor Entry (Complex Forms)"" style=""Dictionary-Minor"" isEnabled=""true"" field=""LexEntry"" cssClassNameOverride=""minorentrycomplex"">
		<ListTypeOptions list=""complex"">
			<Option isEnabled=""true""	id=""a0000000-dd15-4a03-9032-b40faaa9a754""/>
		</ListTypeOptions>
		<ConfigurationItem name=""Headword"" between="" "" after=""	"" style=""Dictionary-Headword"" isEnabled=""true"" field=""MLHeadWord"" cssClassNameOverride=""headword""/>
		<ConfigurationItem name=""Allomorphs"" between="", "" after="" "" isEnabled=""false"" field=""AlternateFormsOS"" cssClassNameOverride=""allomorphs"">
			<ConfigurationItem name=""Morph Type"" between="" "" after="" "" isEnabled=""false"" field=""MorphTypeRA"" cssClassNameOverride=""morphtype"">
				<ConfigurationItem name=""Abbreviation"" between="" "" isEnabled=""false"" field=""Abbreviation""/>
				<ConfigurationItem name=""Name"" between="" "" isEnabled=""false"" field=""Name""/>
			</ConfigurationItem>
			<ConfigurationItem name=""Allomorph"" between="" "" after="" "" isEnabled=""false"" field=""Form""/>
			<ConfigurationItem name=""Environments"" isEnabled=""false"" between="", "" after="" "" field=""AllomorphEnvironments"">
				<ConfigurationItem name=""String Representation"" isEnabled=""true"" field=""StringRepresentation""/>
			</ConfigurationItem>
		</ConfigurationItem>
		<ConfigurationItem name=""Date Created"" before=""created on: "" after="" "" isEnabled=""false"" field=""DateCreated""/>
		<ConfigurationItem name=""Date Modified"" before=""modified on: "" after="" "" isEnabled=""false"" field=""DateModified""/>
		<ConfigurationItem name=""Subentries"" styleType=""character"" before=""  "" isEnabled=""true"" field=""Subentries"">
			<ComplexFormOptions list=""complex"" displayEachComplexFormInParagraph=""false"">
				<Option isEnabled=""true"" id=""a0000000-dd15-4a03-9032-b40faaa9a754""/>
			</ComplexFormOptions>
			<ReferenceItem>MainEntrySubentries</ReferenceItem>
		</ConfigurationItem>
	</ConfigurationItem>
	<SharedItems>
		<ConfigurationItem name=""MainEntrySubentries"" isEnabled=""true"" field=""Subentries"" cssClassNameOverride=""mainentrysubentries"">
			<ConfigurationItem name=""Headword"" between="" "" after="" "" style=""Dictionary-Headword"" isEnabled=""true"" field=""MLHeadWord"" cssClassNameOverride=""headword"">
				<WritingSystemOptions writingSystemType=""vernacular"" displayWSAbreviation=""false"">
					<Option id=""vernacular"" isEnabled=""true""/>
				</WritingSystemOptions>
			</ConfigurationItem>
			<ConfigurationItem name=""Subsubentries"" styleType=""character"" before=""  "" isEnabled=""true"" field=""Subentries"">
				<ComplexFormOptions list=""complex"" displayEachComplexFormInParagraph=""false"">
					<Option isEnabled=""true"" id=""a0000000-dd15-4a03-9032-b40faaa9a754""/>
				</ComplexFormOptions>
				<ReferenceItem>MainEntrySubentries</ReferenceItem>
			</ConfigurationItem>
		</ConfigurationItem>
	</SharedItems>
</DictionaryConfiguration>";

/*

			*/
#endregion DictConfigData

		[Test]
		public void TestReadingDictConfigData()
		{
			var poStrings = new List<POString>();
			var xdoc = XDocument.Parse(DictConfigData);
			Assert.That(xdoc.Root, Is.Not.Null);
			//SUT
			XmlToPo.ProcessFwDictConfigElement(xdoc.Root, "/Language Explorer/DefaultConfigurations/Dictionary/Root.fwdictconfig", poStrings);
			Assert.That(poStrings.Count, Is.EqualTo(39));
			var postr0 = poStrings[0];
			Assert.That(postr0, Is.Not.Null, "fwdictconfig string[0] has data");
			Assert.That(postr0.MsgId, Is.Not.Null, "fwdictconfig string[0].MsgId");
			Assert.That(postr0.MsgId.Count, Is.EqualTo(1), "fwdictconfig string[0].MsgId.Count");
			Assert.That(postr0.MsgId[0], Is.EqualTo("Root-based (complex forms as subentries)"), "fwdictconfig string[0].MsgId[0]");
			Assert.That(postr0.MsgIdAsString(), Is.EqualTo("Root-based (complex forms as subentries)"), "fwdictconfig string[0] is 'Root-based (complex forms as subentries)'");
			Assert.That(postr0.HasEmptyMsgStr, Is.True, "fwdictconfig string[0].HasEmptyMsgStr");
			Assert.That(postr0.UserComments, Is.Null, "fwdictconfig string[0].UserComments");
			Assert.That(postr0.References, Is.Null, "fwdictconfig string[0].References");
			Assert.That(postr0.Flags, Is.Null, "fwdictconfig string[0].Flags");
			Assert.That(postr0.AutoComments, Is.Not.Null, "fwdictconfig string[0].AutoComments");
			Assert.That(postr0.AutoComments.Count, Is.EqualTo(1), "fwdictconfig string[0].AutoComments.Count");
			Assert.That(postr0.AutoComments[0], Is.EqualTo("/Language Explorer/DefaultConfigurations/Dictionary/Root.fwdictconfig:://DictionaryConfiguration/@name"), "fwdictconfig string[0].AutoComments[0]");

			var postr5 = poStrings[5];
			Assert.That(postr5, Is.Not.Null, "fwdictconfig string[5] has data");
			Assert.That(postr5.MsgId, Is.Not.Null, "fwdictconfig string[5].MsgId");
			Assert.That(postr5.MsgId.Count, Is.EqualTo(1), "fwdictconfig string[5].MsgId.Count");
			Assert.That(postr5.MsgId[0], Is.EqualTo("Grammatical Info."), "fwdictconfig string[5].MsgId[0]");
			Assert.That(postr5.MsgIdAsString(), Is.EqualTo("Grammatical Info."), "fwdictconfig string[5] is 'Grammatical Info.'");
			Assert.That(postr5.HasEmptyMsgStr, Is.True, "fwdictconfig string[5].HasEmptyMsgStr");
			Assert.That(postr5.UserComments, Is.Null, "fwdictconfig string[5].UserComments");
			Assert.That(postr5.References, Is.Null, "fwdictconfig string[5].References");
			Assert.That(postr5.Flags, Is.Null, "fwdictconfig string[5].Flags");
			Assert.That(postr5.AutoComments, Is.Not.Null, "fwdictconfig string[5].AutoComments");
			Assert.That(postr5.AutoComments.Count, Is.EqualTo(1), "fwdictconfig string[5].AutoComments.Count");
			Assert.That(postr5.AutoComments[0], Is.EqualTo("/Language Explorer/DefaultConfigurations/Dictionary/Root.fwdictconfig:://ConfigurationItem[@name='Senses']/ConfigurationItem/@name"), "fwdictconfig string[5].AutoComments[0]");

			var postr34 = poStrings[34];
			Assert.That(postr34, Is.Not.Null, "fwdictconfig string[34] has data");
			Assert.That(postr34.MsgId, Is.Not.Null, "fwdictconfig string[34].MsgId");
			Assert.That(postr34.MsgId.Count, Is.EqualTo(1), "fwdictconfig string[34].MsgId.Count");
			Assert.That(postr34.MsgId[0], Is.EqualTo("Date Modified"), "fwdictconfig string[34].MsgId[0]");
			Assert.That(postr34.MsgIdAsString(), Is.EqualTo("Date Modified"), "fwdictconfig string[34] is 'Date Modified'");
			Assert.That(postr34.HasEmptyMsgStr, Is.True, "fwdictconfig string[34].HasEmptyMsgStr");
			Assert.That(postr34.UserComments, Is.Null, "fwdictconfig string[34].UserComments");
			Assert.That(postr34.References, Is.Null, "fwdictconfig string[34].References");
			Assert.That(postr34.Flags, Is.Null, "fwdictconfig string[34].Flags");
			Assert.That(postr34.AutoComments, Is.Not.Null, "fwdictconfig string[34].AutoComments");
			Assert.That(postr34.AutoComments.Count, Is.EqualTo(1), "fwdictconfig string[34].AutoComments.Count");
			Assert.That(postr34.AutoComments[0], Is.EqualTo("/Language Explorer/DefaultConfigurations/Dictionary/Root.fwdictconfig:://ConfigurationItem[@name='Minor Entry (Complex Forms)']/ConfigurationItem/@name"), "fwdictconfig string[34].AutoComments[0]");

			var postr35 = poStrings[35];
			Assert.That(postr35, Is.Not.Null, "fwdictconfig string[35] has data");
			Assert.That(postr35.MsgId, Is.Not.Null, "fwdictconfig string[35].MsgId");
			Assert.That(postr35.MsgId.Count, Is.EqualTo(1), "fwdictconfig string[35].MsgId.Count");
			Assert.That(postr35.MsgId[0], Is.EqualTo("modified on: "), "fwdictconfig string[35].MsgId[0]");
			Assert.That(postr35.MsgIdAsString(), Is.EqualTo("modified on: "), "fwdictconfig string[35] is 'modified on: '");
			Assert.That(postr35.HasEmptyMsgStr, Is.True, "fwdictconfig string[35].HasEmptyMsgStr");
			Assert.That(postr35.UserComments, Is.Null, "fwdictconfig string[35].UserComments");
			Assert.That(postr35.References, Is.Null, "fwdictconfig string[35].References");
			Assert.That(postr35.Flags, Is.Null, "fwdictconfig string[35].Flags");
			Assert.That(postr35.AutoComments, Is.Not.Null, "fwdictconfig string[35].AutoComments");
			Assert.That(postr35.AutoComments.Count, Is.EqualTo(1), "fwdictconfig string[35].AutoComments.Count");
			Assert.That(postr35.AutoComments[0], Is.EqualTo("/Language Explorer/DefaultConfigurations/Dictionary/Root.fwdictconfig:://ConfigurationItem[@name='Date Modified']/@before"), "fwdictconfig string[35].AutoComments[0]");

			var postr38 = poStrings[38];
			Assert.That(postr38, Is.Not.Null, "string[38]");
			Assert.That(postr38.MsgId, Is.Not.Null, "string[38].MsgId");
			Assert.That(postr38.MsgId.Count, Is.EqualTo(1), "fwdictconfig string[38].MsgId.Count");
			Assert.That(postr38.MsgId[0], Is.EqualTo("Subsubentries"), "fwdictconfig string[38].MsgId[0]");
			Assert.That(postr38.MsgIdAsString(), Is.EqualTo("Subsubentries"), "fwdictconfig string[38].MsgIdAsString()");
			Assert.That(postr38.HasEmptyMsgStr, Is.True, "fwdictconfig string[38].MsgStr");
			Assert.That(postr38.UserComments, Is.Null, "fwdictconfig string[38].UserComments");
			Assert.That(postr38.References, Is.Null, "fwdictconfig string[38].References");
			Assert.That(postr38.Flags, Is.Null, "fwdictconfig string[38].Flags");
			Assert.That(postr38.AutoComments, Is.Not.Null, "fwdictconfig string[38].AutoComments");
			Assert.That(postr38.AutoComments.Count, Is.EqualTo(1), "fwdictconfig string[38].AutoComments.Count");
			Assert.That(postr38.AutoComments[0], Is.EqualTo("/Language Explorer/DefaultConfigurations/Dictionary/Root.fwdictconfig:://ConfigurationItem[@name='MainEntrySubentries']/ConfigurationItem/@name"), "fwdictconfig string[38].AutoComments[0]");

			Assert.That(poStrings.Any(poStr => poStr.MsgIdAsString() == "MainEntrySubentries"), Is.False, "Shared Items' labels should not be translatable");
		}

		[Test]
		public void TestWriteAndReadPoFile()
		{
			var poStrings = new List<POString>();
			var fwLayoutDoc = XDocument.Parse(FwlayoutData);
			Assert.That(fwLayoutDoc.Root, Is.Not.Null);
			XmlToPo.ProcessConfigElement(fwLayoutDoc.Root, "/Language Explorer/Configuration/Parts/LexEntry.fwlayout", poStrings);
			var fwDictConfigDoc = XDocument.Parse(DictConfigData);
			Assert.That(fwDictConfigDoc.Root, Is.Not.Null);
			XmlToPo.ProcessFwDictConfigElement(fwDictConfigDoc.Root, "/Language Explorer/DefaultConfigurations/Dictionary/Root.fwdictconfig", poStrings);
			Assert.That(poStrings.Count, Is.EqualTo(53));
			Assert.That(poStrings[0].MsgIdAsString(), Is.EqualTo("Lexeme Form"));
			Assert.That(poStrings[49].MsgIdAsString(), Is.EqualTo("modified on: "));
			poStrings.Sort(POString.CompareMsgIds);
			// SUT
			POString.MergeDuplicateStrings(poStrings);
			Assert.That(poStrings.Count, Is.EqualTo(40));
			Assert.That(poStrings[0].MsgIdAsString(), Is.EqualTo(" - "));
			Assert.That(poStrings[39].MsgIdAsString(), Is.EqualTo("Variants"));
			var sw = new StringWriter();
			XmlToPo.WritePotFile(sw, "/home/testing/fw", poStrings);
			var potFileStr = sw.ToString();
			Assert.That(potFileStr, Is.Not.Null);
			var sr = new StringReader(potFileStr);
			var dictPot = PoToXml.ReadPoFile(sr, null);
			Assert.That(dictPot.Count, Is.EqualTo(40));
			var listPot = dictPot.ToList();
			Assert.That(listPot[0].Value.MsgIdAsString(), Is.EqualTo(" - "));
			Assert.That(listPot[39].Value.MsgIdAsString(), Is.EqualTo("Variants"));

			var posHeadword = dictPot["Headword"];
			Assert.That(posHeadword.AutoComments.Count, Is.EqualTo(6), "Headword AutoComments");
			Assert.That(posHeadword.AutoComments[0], Is.EqualTo("/Language Explorer/Configuration/Parts/LexEntry.fwlayout::/LayoutInventory/layout[\"LexEntry-jtview-CrossRefPub\"]/part[@ref=\"MLHeadWordPub\"]/@label"));
			Assert.That(posHeadword.AutoComments[1], Is.EqualTo("/Language Explorer/Configuration/Parts/LexEntry.fwlayout::/LayoutInventory/layout[\"LexEntry-jtview-SubentryUnderPub\"]/part[@ref=\"MLHeadWordPub\"]/@label"));
			Assert.That(posHeadword.AutoComments[2], Is.EqualTo("/Language Explorer/DefaultConfigurations/Dictionary/Root.fwdictconfig:://ConfigurationItem[@name='Main Entry']/ConfigurationItem/@name"));
			Assert.That(posHeadword.AutoComments[3], Is.EqualTo("/Language Explorer/DefaultConfigurations/Dictionary/Root.fwdictconfig:://ConfigurationItem[@name='MainEntrySubentries']/ConfigurationItem/@name"));
			Assert.That(posHeadword.AutoComments[4], Is.EqualTo("/Language Explorer/DefaultConfigurations/Dictionary/Root.fwdictconfig:://ConfigurationItem[@name='Minor Entry (Complex Forms)']/ConfigurationItem/@name"));
			Assert.That(posHeadword.AutoComments[5], Is.EqualTo("(String used 5 times.)"));

			var posComma = dictPot[", "];
			Assert.That(posComma.AutoComments.Count, Is.EqualTo(4), "AutoCommas");
			Assert.That(posComma.AutoComments[0], Is.EqualTo("/Language Explorer/DefaultConfigurations/Dictionary/Root.fwdictconfig:://ConfigurationItem[@name='Allomorphs']/@between"));
			Assert.That(posComma.AutoComments[3], Is.EqualTo("(String used 3 times.)"));
		}

		[Test]
		public void POString_Sort()
		{
			var poStrings = new[]
			{
				"Remove example",
				"Citation Form",
				"Citation form",
				"Remove allomorph",
				"Remove translation",
				"Remove &example",
				"Remove _example",
				"Citation Form",
				"Citation Family",
				"{0} something else",
				"Citation form",
				"Citation Form",
				"something else"
			}.Select(msgId => new POString(null, new []{msgId})).ToList();
			// SUT
			poStrings.Sort(POString.CompareMsgIds);
			var msgIds = poStrings.Select(poStr => poStr.MsgIdAsString()).ToArray();
			var sortedStrings = new[]
			{
				"Citation Family",
				"Citation Form",
				"Citation Form",
				"Citation Form",
				"Citation form",
				"Citation form",
				"Remove allomorph",
				"Remove &example",
				"Remove _example",
				"Remove example",
				"Remove translation",
				"something else",
				"{0} something else"
			};
			AssertArraysAreEqual(sortedStrings, msgIds);
		}

		private static void AssertArraysAreEqual(IReadOnlyList<string> arr1, IReadOnlyList<string> arr2)
		{
			for (var i = 0; i < arr1.Count && i < arr2.Count; i++)
			{
				Assert.That(arr2[i], Is.EqualTo(arr1[i]), $"Arrays differ at index {i}");
			}
			Assert.That(arr2.Count, Is.EqualTo(arr1.Count), "Array lengths differ");
		}

		[Test]
		public void POString_WriteAndReadLeadingNewlines()
		{
			var poStr = new POString(new []{"Displayed in a message box.", "/Src/FwResources//FwStrings.resx::kstidFatalError2"},
				new[]{@"\n", @"\n", @"In order to protect your data, the FieldWorks program needs to close.\n", @"\n", @"You should be able to restart it normally.\n"});
			Assert.That(poStr.MsgId, Is.Not.Null, "First resx string has MsgId data");
			Assert.That(poStr.MsgId.Count, Is.EqualTo(5), "First resx string has five lines of MsgId data");
			Assert.That(poStr.MsgId[0], Is.EqualTo("\\n"), "First resx string has the expected MsgId data line one");
			Assert.That(poStr.MsgId[1], Is.EqualTo("\\n"), "First resx string has the expected MsgId data line two");
			Assert.That(poStr.MsgId[2], Is.EqualTo("In order to protect your data, the FieldWorks program needs to close.\\n"), "First resx string has the expected MsgId data line three");
			Assert.That(poStr.MsgId[3], Is.EqualTo("\\n"), "First resx string has the expected MsgId data line four");
			Assert.That(poStr.MsgId[4], Is.EqualTo("You should be able to restart it normally.\\n"), "First resx string has the expected MsgId data line five");
			Assert.That(poStr.HasEmptyMsgStr, Is.True, "First resx string has no MsgStr data (as expected)");
			Assert.That(poStr.UserComments, Is.Null, "First resx string has no User Comments (as expected)");
			Assert.That(poStr.References, Is.Null, "First resx string has no Reference data (as expected)");
			Assert.That(poStr.Flags, Is.Null, "First resx string.Flags");
			Assert.That(poStr.AutoComments, Is.Not.Null, "Third resx string has Auto Comments");
			Assert.That(poStr.AutoComments.Count, Is.EqualTo(2), "First resx string has two lines of Auto Comments");
			Assert.That(poStr.AutoComments[0], Is.EqualTo("Displayed in a message box."), "First resx string has the expected Auto Comment line one");
			Assert.That(poStr.AutoComments[1], Is.EqualTo("/Src/FwResources//FwStrings.resx::kstidFatalError2"), "First resx string has the expected Auto Comment line two");

			var sw = new StringWriter();
			// SUT
			poStr.Write(sw);
			poStr.Write(sw); // write a second to ensure they can be read separately
			var serializedPo = sw.ToString();
			Assert.That(serializedPo, Is.Not.Null, "Writing resx strings' po data produced output");
			var poLines = serializedPo.Split(new[] { Environment.NewLine }, 100, StringSplitOptions.None);
			for (var i = 0; i <= 10; i += 10)
			{
				Assert.That(poLines[0 + i], Is.EqualTo("#. Displayed in a message box."), $"Error line {0 + i}");
				Assert.That(poLines[1 + i], Is.EqualTo("#. /Src/FwResources//FwStrings.resx::kstidFatalError2"), $"Error line {1 + i}");
				Assert.That(poLines[2 + i], Is.EqualTo("msgid \"\""), $"Error line {2 + i}");
				Assert.That(poLines[3 + i], Is.EqualTo("\"\\n\""), $"Error line {3 + i}");
				Assert.That(poLines[4 + i], Is.EqualTo("\"\\n\""), $"Error line {4 + i}");
				Assert.That(poLines[5 + i], Is.EqualTo("\"In order to protect your data, the FieldWorks program needs to close.\\n\""), $"Error line {5 + i}");
				Assert.That(poLines[6 + i], Is.EqualTo("\"\\n\""), $"Error line {6 + i}");
				Assert.That(poLines[7 + i], Is.EqualTo("\"You should be able to restart it normally.\\n\""), $"Error line {7 + i}");
				Assert.That(poLines[8 + i], Is.EqualTo("msgstr \"\""), $"Error line {8 + i}");
				Assert.That(poLines[9 + i], Is.EqualTo(""), $"Error line {9 + i}");
			}
			Assert.That(poLines[20], Is.EqualTo(""));
			Assert.That(poLines.Length, Is.EqualTo(21));

			var sr = new StringReader(serializedPo);
			// SUT
			var poStrA = POString.ReadFromFile(sr);
			var poStrB = POString.ReadFromFile(sr);
			var poStrC = POString.ReadFromFile(sr);
			Assert.That(poStrA, Is.Not.Null, "Read first message from leading newline test data");
			Assert.That(poStrB, Is.Not.Null, "Read second message from leading newline test data");
			Assert.That(poStrC, Is.Null, "Only two messages in leading newline test data");

			CheckStringList(poStr.MsgId, poStrA.MsgId, "Preserve MsgId in first message from leading newline test data");
			CheckStringList(poStr.MsgStr, poStrA.MsgStr, "Preserve MsgStr in first message from leading newline test data");
			CheckStringList(poStr.UserComments, poStrA.UserComments, "Preserve UserComments in first message from leading newline test data");
			CheckStringList(poStr.References, poStrA.References, "Preserve Reference in first message from leading newline test data");
			CheckStringList(poStr.Flags, poStrA.Flags, "Preserve Flags in first message from leading newline test data");
			CheckStringList(poStr.AutoComments, poStrA.AutoComments, "Preserve AutoComments in first message from leading newline test data");
		}

		private static void CheckStringList(List<string> list1, List<string> list2, string msg)
		{
			if (list1 == null)
			{
				Assert.That(list2, Is.Null, msg + " (both null)");
				return;
			}
			Assert.That(list2, Is.Not.Null, msg + " (both not null)");
			Assert.That(list2.Count, Is.EqualTo(list1.Count), msg + " (same number of lines)");
			for (var i = 0; i < list1.Count; ++i)
				Assert.That(list2[i], Is.EqualTo(list1[i]), $"{msg} - line {i} is same");
		}
	}
}
