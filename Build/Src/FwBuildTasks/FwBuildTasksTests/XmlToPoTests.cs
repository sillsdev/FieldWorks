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
			Assert.AreEqual(@"/Language Explorer/DefaultConfigurations/Dictionary/Hybrid.fwdictconfig", result);

			result = XmlToPo.ComputeAutoCommentFilePath(@"C:\fwrepo\fw\DistFiles",
				@"E:\fwrepo\fw\DistFiles\Language Explorer\DefaultConfigurations\Dictionary\Hybrid.fwdictconfig");
			Assert.AreEqual(@"E:/fwrepo/fw/DistFiles/Language Explorer/DefaultConfigurations/Dictionary/Hybrid.fwdictconfig", result);

			result = XmlToPo.ComputeAutoCommentFilePath("/home/steve/fwrepo/fw/DistFiles",
				"/home/steve/fwrepo/fw/DistFiles/Language Explorer/Configuration/Parts/LexEntry.fwlayout");
			Assert.AreEqual("/Language Explorer/Configuration/Parts/LexEntry.fwlayout", result);

			result = XmlToPo.ComputeAutoCommentFilePath("/home/john/fwrepo/fw/DistFiles",
				"/home/steve/fwrepo/fw/DistFiles/Language Explorer/Configuration/Parts/LexEntry.fwlayout");
			Assert.AreEqual("/home/steve/fwrepo/fw/DistFiles/Language Explorer/Configuration/Parts/LexEntry.fwlayout", result);
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
			Assert.IsNotNull(xdoc.Root);
			//SUT
			XmlToPo.ProcessConfigElement(xdoc.Root, "/Language Explorer/Configuration/Parts/LexEntry.fwlayout", poStrings);
			Assert.AreEqual(14, poStrings.Count);
			var postr5 = poStrings[5];
			Assert.IsNotNull(postr5, "Detail Config string[5] has data");
			Assert.IsNotNull(postr5.MsgId, "Detail Config string[5].MsgId");
			Assert.AreEqual(1, postr5.MsgId.Count, "Detail Config string[5].MsgId.Count");
			Assert.AreEqual("Grammatical Info. Details", postr5.MsgId[0], "Detail Config string[5].MsgId[0]");
			Assert.AreEqual("Grammatical Info. Details", postr5.MsgIdAsString(), "Detail Config string[5] is 'Grammatical Info. Details'");
			Assert.IsTrue(postr5.HasEmptyMsgStr, "Detail Config string[5].HasEmptyMsgStr");
			Assert.IsNull(postr5.UserComments, "Detail Config string[5].UserComments");
			Assert.IsNull(postr5.References, "Detail Config string[5].References");
			Assert.IsNull(postr5.Flags, "Detail Config string[5].Flags");
			Assert.IsNotNull(postr5.AutoComments, "Detail Config string[5].AutoComments");
			Assert.AreEqual(1, postr5.AutoComments.Count, "Detail Config string[5].AutoComments.Count");
			Assert.AreEqual(
				"/Language Explorer/Configuration/Parts/LexEntry.fwlayout::/LayoutInventory/layout[\"LexEntry-detail-Normal\"]/part[@ref=\"GrammaticalFunctionsSection\"]/@label",
				postr5.AutoComments[0], "Detail Config string[5].AutoComments[0]");

			var postr8 = poStrings[8];
			Assert.IsNotNull(postr8, "Detail Config string[8] has data");
			Assert.IsNotNull(postr8.MsgId, "Detail Config string[8].MsgId");
			Assert.AreEqual(1, postr8.MsgId.Count, "Detail Config string[8].MsgId.Count");
			Assert.AreEqual("Headword", postr8.MsgId[0], "Detail Config string[8].MsgId[0]");
			Assert.AreEqual("Headword", poStrings[8].MsgIdAsString(), "Detail Config string[8] is 'Headword'");
			Assert.IsTrue(postr8.HasEmptyMsgStr, "Detail Config string[8].HasEmptyMsgStr");
			Assert.IsNull(postr8.UserComments, "Detail Config string[8].UserComments");
			Assert.IsNull(postr8.References, "Detail Config string[8].References");
			Assert.IsNull(postr8.Flags, "Detail Config string[8].Flags");
			Assert.IsNotNull(postr8.AutoComments, "Detail Config string[8].AutoComments");
			Assert.AreEqual(1, postr8.AutoComments.Count, "Detail Config string[8].AutoComments.Count");
			Assert.AreEqual(
				"/Language Explorer/Configuration/Parts/LexEntry.fwlayout::/LayoutInventory/layout[\"LexEntry-jtview-CrossRefPub\"]/part[@ref=\"MLHeadWordPub\"]/@label",
				postr8.AutoComments[0], "Detail Config string[8].AutoComments[0]");

			var postr10 = poStrings[10];
			Assert.IsNotNull(postr10, "Detail Config string[10] has data");
			Assert.IsNotNull(postr10.MsgId, "Detail Config string[10].MsgId");
			Assert.AreEqual(1, postr10.MsgId.Count, "Detail Config string[10].MsgId.Count");
			Assert.AreEqual(" CrossRef:", postr10.MsgId[0], "Detail Config string[10].MsgId[0]");
			Assert.AreEqual(" CrossRef:", poStrings[10].MsgIdAsString(), "Detail Config string[8] is ' CrossRef:'");
			Assert.IsTrue(postr10.HasEmptyMsgStr, "Detail Config string[10].HasEmptyMsgStr");
			Assert.IsNull(postr10.UserComments, "Detail Config string[10].UserComments");
			Assert.IsNull(postr10.References, "Detail Config string[10].References");
			Assert.IsNull(postr10.Flags, "Detail Config string[10].Flags");
			Assert.IsNotNull(postr10.AutoComments, "Detail Config string[10].AutoComments");
			Assert.AreEqual(1, postr10.AutoComments.Count, "Detail Config string[10].AutoComments.Count");
			Assert.AreEqual(
				"/Language Explorer/Configuration/Parts/LexEntry.fwlayout::/LayoutInventory/layout[\"LexEntry-jtview-CrossRefPub\"]/part[@ref=\"MLHeadWordPub\"]/@before",
				postr10.AutoComments[0], "Detail Config string[10].AutoComments[0]");

			var postr11 = poStrings[11];
			Assert.IsNotNull(postr11, "Detail Config string[11] has data");
			Assert.IsNotNull(postr11.MsgId, "Detail Config string[11].MsgId");
			Assert.AreEqual(1, postr11.MsgId.Count, "Detail Config string[11].MsgId.Count");
			Assert.AreEqual("Headword", postr11.MsgId[0], "Detail Config string[11].MsgId[0]");
			Assert.AreEqual("Headword", poStrings[11].MsgIdAsString(), "Detail Config string[8] is 'Headword'");
			Assert.IsTrue(postr11.HasEmptyMsgStr, "Detail Config string[11].HasEmptyMsgStr");
			Assert.IsNull(postr11.UserComments, "Detail Config string[11].UserComments");
			Assert.IsNull(postr11.References, "Detail Config string[11].References");
			Assert.IsNull(postr11.Flags, "Detail Config string[11].Flags");
			Assert.IsNotNull(postr11.AutoComments, "Detail Config string[11].AutoComments");
			Assert.AreEqual(1, postr11.AutoComments.Count, "Detail Config string[11].AutoComments.Count");
			Assert.AreEqual(
				"/Language Explorer/Configuration/Parts/LexEntry.fwlayout::/LayoutInventory/layout[\"LexEntry-jtview-SubentryUnderPub\"]/part[@ref=\"MLHeadWordPub\"]/@label",
				postr11.AutoComments[0], "Detail Config string[11].AutoComments[0]");
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
			Assert.IsNotNull(xdoc.Root);
			//SUT
			XmlToPo.ProcessFwDictConfigElement(xdoc.Root, "/Language Explorer/DefaultConfigurations/Dictionary/Root.fwdictconfig", poStrings);
			Assert.AreEqual(39, poStrings.Count);
			var postr0 = poStrings[0];
			Assert.IsNotNull(postr0, "fwdictconfig string[0] has data");
			Assert.IsNotNull(postr0.MsgId, "fwdictconfig string[0].MsgId");
			Assert.AreEqual(1, postr0.MsgId.Count, "fwdictconfig string[0].MsgId.Count");
			Assert.AreEqual("Root-based (complex forms as subentries)", postr0.MsgId[0], "fwdictconfig string[0].MsgId[0]");
			Assert.AreEqual("Root-based (complex forms as subentries)", postr0.MsgIdAsString(), "fwdictconfig string[0] is 'Root-based (complex forms as subentries)'");
			Assert.IsTrue(postr0.HasEmptyMsgStr, "fwdictconfig string[0].HasEmptyMsgStr");
			Assert.IsNull(postr0.UserComments, "fwdictconfig string[0].UserComments");
			Assert.IsNull(postr0.References, "fwdictconfig string[0].References");
			Assert.IsNull(postr0.Flags, "fwdictconfig string[0].Flags");
			Assert.IsNotNull(postr0.AutoComments, "fwdictconfig string[0].AutoComments");
			Assert.AreEqual(1, postr0.AutoComments.Count, "fwdictconfig string[0].AutoComments.Count");
			Assert.AreEqual("/Language Explorer/DefaultConfigurations/Dictionary/Root.fwdictconfig:://DictionaryConfiguration/@name",
				postr0.AutoComments[0], "fwdictconfig string[0].AutoComments[0]");

			var postr5 = poStrings[5];
			Assert.IsNotNull(postr5, "fwdictconfig string[5] has data");
			Assert.IsNotNull(postr5.MsgId, "fwdictconfig string[5].MsgId");
			Assert.AreEqual(1, postr5.MsgId.Count, "fwdictconfig string[5].MsgId.Count");
			Assert.AreEqual("Grammatical Info.", postr5.MsgId[0], "fwdictconfig string[5].MsgId[0]");
			Assert.AreEqual("Grammatical Info.", postr5.MsgIdAsString(), "fwdictconfig string[5] is 'Grammatical Info.'");
			Assert.IsTrue(postr5.HasEmptyMsgStr, "fwdictconfig string[5].HasEmptyMsgStr");
			Assert.IsNull(postr5.UserComments, "fwdictconfig string[5].UserComments");
			Assert.IsNull(postr5.References, "fwdictconfig string[5].References");
			Assert.IsNull(postr5.Flags, "fwdictconfig string[5].Flags");
			Assert.IsNotNull(postr5.AutoComments, "fwdictconfig string[5].AutoComments");
			Assert.AreEqual(1, postr5.AutoComments.Count, "fwdictconfig string[5].AutoComments.Count");
			Assert.AreEqual("/Language Explorer/DefaultConfigurations/Dictionary/Root.fwdictconfig:://ConfigurationItem[@name='Senses']/ConfigurationItem/@name",
				postr5.AutoComments[0], "fwdictconfig string[5].AutoComments[0]");

			var postr34 = poStrings[34];
			Assert.IsNotNull(postr34, "fwdictconfig string[34] has data");
			Assert.IsNotNull(postr34.MsgId, "fwdictconfig string[34].MsgId");
			Assert.AreEqual(1, postr34.MsgId.Count, "fwdictconfig string[34].MsgId.Count");
			Assert.AreEqual("Date Modified", postr34.MsgId[0], "fwdictconfig string[34].MsgId[0]");
			Assert.AreEqual("Date Modified", postr34.MsgIdAsString(), "fwdictconfig string[34] is 'Date Modified'");
			Assert.IsTrue(postr34.HasEmptyMsgStr, "fwdictconfig string[34].HasEmptyMsgStr");
			Assert.IsNull(postr34.UserComments, "fwdictconfig string[34].UserComments");
			Assert.IsNull(postr34.References, "fwdictconfig string[34].References");
			Assert.IsNull(postr34.Flags, "fwdictconfig string[34].Flags");
			Assert.IsNotNull(postr34.AutoComments, "fwdictconfig string[34].AutoComments");
			Assert.AreEqual(1, postr34.AutoComments.Count, "fwdictconfig string[34].AutoComments.Count");
			Assert.AreEqual("/Language Explorer/DefaultConfigurations/Dictionary/Root.fwdictconfig:://ConfigurationItem[@name='Minor Entry (Complex Forms)']/ConfigurationItem/@name",
				postr34.AutoComments[0], "fwdictconfig string[34].AutoComments[0]");

			var postr35 = poStrings[35];
			Assert.IsNotNull(postr35, "fwdictconfig string[35] has data");
			Assert.IsNotNull(postr35.MsgId, "fwdictconfig string[35].MsgId");
			Assert.AreEqual(1, postr35.MsgId.Count, "fwdictconfig string[35].MsgId.Count");
			Assert.AreEqual("modified on: ", postr35.MsgId[0], "fwdictconfig string[35].MsgId[0]");
			Assert.AreEqual("modified on: ", postr35.MsgIdAsString(), "fwdictconfig string[35] is 'modified on: '");
			Assert.IsTrue(postr35.HasEmptyMsgStr, "fwdictconfig string[35].HasEmptyMsgStr");
			Assert.IsNull(postr35.UserComments, "fwdictconfig string[35].UserComments");
			Assert.IsNull(postr35.References, "fwdictconfig string[35].References");
			Assert.IsNull(postr35.Flags, "fwdictconfig string[35].Flags");
			Assert.IsNotNull(postr35.AutoComments, "fwdictconfig string[35].AutoComments");
			Assert.AreEqual(1, postr35.AutoComments.Count, "fwdictconfig string[35].AutoComments.Count");
			Assert.AreEqual("/Language Explorer/DefaultConfigurations/Dictionary/Root.fwdictconfig:://ConfigurationItem[@name='Date Modified']/@before",
				postr35.AutoComments[0], "fwdictconfig string[35].AutoComments[0]");

			var postr38 = poStrings[38];
			Assert.IsNotNull(postr38, "string[38]");
			Assert.IsNotNull(postr38.MsgId, "string[38].MsgId");
			Assert.AreEqual(1, postr38.MsgId.Count, "fwdictconfig string[38].MsgId.Count");
			Assert.AreEqual("Subsubentries", postr38.MsgId[0], "fwdictconfig string[38].MsgId[0]");
			Assert.AreEqual("Subsubentries", postr38.MsgIdAsString(), "fwdictconfig string[38].MsgIdAsString()");
			Assert.IsTrue(postr38.HasEmptyMsgStr, "fwdictconfig string[38].MsgStr");
			Assert.IsNull(postr38.UserComments, "fwdictconfig string[38].UserComments");
			Assert.IsNull(postr38.References, "fwdictconfig string[38].References");
			Assert.IsNull(postr38.Flags, "fwdictconfig string[38].Flags");
			Assert.IsNotNull(postr38.AutoComments, "fwdictconfig string[38].AutoComments");
			Assert.AreEqual(1, postr38.AutoComments.Count, "fwdictconfig string[38].AutoComments.Count");
			Assert.AreEqual("/Language Explorer/DefaultConfigurations/Dictionary/Root.fwdictconfig:://ConfigurationItem[@name='MainEntrySubentries']/ConfigurationItem/@name",
				postr38.AutoComments[0], "fwdictconfig string[38].AutoComments[0]");

			Assert.False(poStrings.Any(poStr => poStr.MsgIdAsString() == "MainEntrySubentries"), "Shared Items' labels should not be translatable");
		}

		[Test]
		public void TestWriteAndReadPoFile()
		{
			var poStrings = new List<POString>();
			var fwLayoutDoc = XDocument.Parse(FwlayoutData);
			Assert.IsNotNull(fwLayoutDoc.Root);
			XmlToPo.ProcessConfigElement(fwLayoutDoc.Root, "/Language Explorer/Configuration/Parts/LexEntry.fwlayout", poStrings);
			var fwDictConfigDoc = XDocument.Parse(DictConfigData);
			Assert.IsNotNull(fwDictConfigDoc.Root);
			XmlToPo.ProcessFwDictConfigElement(fwDictConfigDoc.Root, "/Language Explorer/DefaultConfigurations/Dictionary/Root.fwdictconfig", poStrings);
			Assert.AreEqual(53, poStrings.Count);
			Assert.AreEqual("Lexeme Form", poStrings[0].MsgIdAsString());
			Assert.AreEqual("modified on: ", poStrings[49].MsgIdAsString());
			poStrings.Sort(POString.CompareMsgIds);
			// SUT
			POString.MergeDuplicateStrings(poStrings);
			Assert.AreEqual(40, poStrings.Count);
			Assert.AreEqual(" - ", poStrings[0].MsgIdAsString());
			Assert.AreEqual("Variants", poStrings[39].MsgIdAsString());
			var sw = new StringWriter();
			XmlToPo.WritePotFile(sw, "/home/testing/fw", poStrings);
			var potFileStr = sw.ToString();
			Assert.IsNotNull(potFileStr);
			var sr = new StringReader(potFileStr);
			var dictPot = PoToXml.ReadPoFile(sr, null);
			Assert.AreEqual(40, dictPot.Count);
			var listPot = dictPot.ToList();
			Assert.AreEqual(" - ", listPot[0].Value.MsgIdAsString());
			Assert.AreEqual("Variants", listPot[39].Value.MsgIdAsString());

			var posHeadword = dictPot["Headword"];
			Assert.AreEqual(6, posHeadword.AutoComments.Count, "Headword AutoComments");
			Assert.AreEqual("/Language Explorer/Configuration/Parts/LexEntry.fwlayout::/LayoutInventory/layout[\"LexEntry-jtview-CrossRefPub\"]/part[@ref=\"MLHeadWordPub\"]/@label", posHeadword.AutoComments[0]);
			Assert.AreEqual("/Language Explorer/Configuration/Parts/LexEntry.fwlayout::/LayoutInventory/layout[\"LexEntry-jtview-SubentryUnderPub\"]/part[@ref=\"MLHeadWordPub\"]/@label", posHeadword.AutoComments[1]);
			Assert.AreEqual("/Language Explorer/DefaultConfigurations/Dictionary/Root.fwdictconfig:://ConfigurationItem[@name='Main Entry']/ConfigurationItem/@name", posHeadword.AutoComments[2]);
			Assert.AreEqual("/Language Explorer/DefaultConfigurations/Dictionary/Root.fwdictconfig:://ConfigurationItem[@name='MainEntrySubentries']/ConfigurationItem/@name", posHeadword.AutoComments[3]);
			Assert.AreEqual("/Language Explorer/DefaultConfigurations/Dictionary/Root.fwdictconfig:://ConfigurationItem[@name='Minor Entry (Complex Forms)']/ConfigurationItem/@name", posHeadword.AutoComments[4]);
			Assert.AreEqual("(String used 5 times.)", posHeadword.AutoComments[5]);

			var posComma = dictPot[", "];
			Assert.AreEqual(4, posComma.AutoComments.Count, "AutoCommas");
			Assert.AreEqual("/Language Explorer/DefaultConfigurations/Dictionary/Root.fwdictconfig:://ConfigurationItem[@name='Allomorphs']/@between", posComma.AutoComments[0]);
			Assert.AreEqual("(String used 3 times.)", posComma.AutoComments[3]);
		}

		[Test]
		public void POString_WriteAndReadLeadingNewlines()
		{
			var poStr = new POString(new []{"Displayed in a message box.", "/Src/FwResources//FwStrings.resx::kstidFatalError2"},
				new[]{@"\n", @"\n", @"In order to protect your data, the FieldWorks program needs to close.\n", @"\n", @"You should be able to restart it normally.\n"});
			Assert.IsNotNull(poStr.MsgId, "First resx string has MsgId data");
			Assert.AreEqual(5, poStr.MsgId.Count, "First resx string has five lines of MsgId data");
			Assert.AreEqual("\\n", poStr.MsgId[0], "First resx string has the expected MsgId data line one");
			Assert.AreEqual("\\n", poStr.MsgId[1], "First resx string has the expected MsgId data line two");
			Assert.AreEqual("In order to protect your data, the FieldWorks program needs to close.\\n", poStr.MsgId[2], "First resx string has the expected MsgId data line three");
			Assert.AreEqual("\\n", poStr.MsgId[3], "First resx string has the expected MsgId data line four");
			Assert.AreEqual("You should be able to restart it normally.\\n", poStr.MsgId[4], "First resx string has the expected MsgId data line five");
			Assert.IsTrue(poStr.HasEmptyMsgStr, "First resx string has no MsgStr data (as expected)");
			Assert.IsNull(poStr.UserComments, "First resx string has no User Comments (as expected)");
			Assert.IsNull(poStr.References, "First resx string has no Reference data (as expected)");
			Assert.IsNull(poStr.Flags, "First resx string.Flags");
			Assert.IsNotNull(poStr.AutoComments, "Third resx string has Auto Comments");
			Assert.AreEqual(2, poStr.AutoComments.Count, "First resx string has two lines of Auto Comments");
			Assert.AreEqual("Displayed in a message box.", poStr.AutoComments[0], "First resx string has the expected Auto Comment line one");
			Assert.AreEqual("/Src/FwResources//FwStrings.resx::kstidFatalError2", poStr.AutoComments[1], "First resx string has the expected Auto Comment line two");

			var sw = new StringWriter();
			// SUT
			poStr.Write(sw);
			poStr.Write(sw); // write a second to ensure they can be read separately
			var serializedPo = sw.ToString();
			Assert.IsNotNull(serializedPo, "Writing resx strings' po data produced output");
			var poLines = serializedPo.Split(new[] { Environment.NewLine }, 100, StringSplitOptions.None);
			for (var i = 0; i <= 10; i += 10)
			{
				Assert.AreEqual("#. Displayed in a message box.", poLines[0 + i], $"Error line {0 + i}");
				Assert.AreEqual("#. /Src/FwResources//FwStrings.resx::kstidFatalError2", poLines[1 + i], $"Error line {1 + i}");
				Assert.AreEqual("msgid \"\"", poLines[2 + i], $"Error line {2 + i}");
				Assert.AreEqual("\"\\n\"", poLines[3 + i], $"Error line {3 + i}");
				Assert.AreEqual("\"\\n\"", poLines[4 + i], $"Error line {4 + i}");
				Assert.AreEqual("\"In order to protect your data, the FieldWorks program needs to close.\\n\"", poLines[5 + i], $"Error line {5 + i}");
				Assert.AreEqual("\"\\n\"", poLines[6 + i], $"Error line {6 + i}");
				Assert.AreEqual("\"You should be able to restart it normally.\\n\"", poLines[7 + i], $"Error line {7 + i}");
				Assert.AreEqual("msgstr \"\"", poLines[8 + i], $"Error line {8 + i}");
				Assert.AreEqual("", poLines[9 + i], $"Error line {9 + i}");
			}
			Assert.AreEqual("", poLines[20]);
			Assert.AreEqual(21, poLines.Length);

			var sr = new StringReader(serializedPo);
			// SUT
			var poStrA = POString.ReadFromFile(sr);
			var poStrB = POString.ReadFromFile(sr);
			var poStrC = POString.ReadFromFile(sr);
			Assert.IsNotNull(poStrA, "Read first message from leading newline test data");
			Assert.IsNotNull(poStrB, "Read second message from leading newline test data");
			Assert.IsNull(poStrC, "Only two messages in leading newline test data");

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
				Assert.IsNull(list2, msg + " (both null)");
				return;
			}
			Assert.IsNotNull(list2, msg + " (both not null)");
			Assert.AreEqual(list1.Count, list2.Count, msg + " (same number of lines)");
			for (var i = 0; i < list1.Count; ++i)
				Assert.AreEqual(list1[i], list2[i], $"{msg} - line {i} is same");
		}
	}
}
