// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using FwBuildTasks;
using NUnit.Framework;
using SIL.FieldWorks.Build.Tasks.Localization;
using SIL.TestUtilities;

namespace SIL.FieldWorks.Build.Tasks.FwBuildTasksTests
{
	[TestFixture]
	[SuppressMessage("ReSharper", "StringLiteralTypo", Justification = "many are abbreviations and other languages")]
	internal class GoldEticToXliffTests
	{
		private const string TestFileName = "TestFile.xml";
		private const string WsEn = "en";
		private const string WsEs = "es";
		private const string WsDe = "de";
		private const string WsZh = "zh-CN";

		[Test]
		public void BadInputThrows()
		{
			Assert.Throws<ArgumentException>(() => GoldEticToXliff.ConvertGoldEticToXliff(TestFileName,
				XDocument.Parse("<eticPOSList><source/></eticPOSList>")),
				"File with no item should throw");
		}

		[Test]
		public void SourceConverted()
		{
			const string source = "based on <link>http://www.linguistics-ontology.org/ns/gold/0.2/gold-pos.owl</link> &amp; modified by hand.";
			var xliffDocs = GoldEticToXliff.ConvertGoldEticToXliff(TestFileName,
				XDocument.Parse("<eticPOSList><source>" + source + "</source>" +
					"<item id='test' guid='810e6d78-4239-4902-9ff0-8fbcd2173d6e'>" +
						"<abbrev ws='" + WsEn + "'>test</abbrev>" +
						"<term ws='" + WsEn + "'>test</term>" +
						"<def ws='" + WsEn + "'>test</def>" +
					"</item></eticPOSList>"));
			const string sourceXPath = "/xliff/file/body/*[local-name()='source']";
			AssertThatXmlIn.String(xliffDocs[WsEn].ToString()).HasSpecifiedNumberOfMatchesForXpath(sourceXPath, 1);
			var sourceElt = xliffDocs[WsEn].XPathSelectElement(sourceXPath);
			// ReSharper disable once PossibleNullReferenceException -- we just asserted there is 1 sourceElt
			Assert.That(sourceElt.ToString(), Does.Contain(source));
		}

		[Test]
		public void CitationHasInstructions()
		{
			const string id = "tst";
			const string guid = "810e6d78-4239-4902-9ff0-8fbcd2173d6e";
			var xliffDocs = GoldEticToXliff.ConvertGoldEticToXliff(TestFileName,
				XDocument.Parse("<eticPOSList><source/><item type='category' id='" + id + @"' guid='" + guid + @"'>" +
						"<abbrev ws='" + WsEn + "'>test</abbrev>" +
						"<term ws='" + WsEn + "'>test</term>" +
						"<def ws='" + WsEn + "'>test</def>" +
					"</item></eticPOSList>"));
			AssertThatXmlIn.String(xliffDocs[WsEn].ToString()).HasSpecifiedNumberOfMatchesForXpath(
				$"/xliff/file/body/group[@id='{guid}_{id}']/trans-unit[@id='{guid}_{id}_cit']/note[text()]", 1);
		}

		[Test]
		public void FileAttributes_ForEachLanguage()
		{
			var xliffDocs = GoldEticToXliff.ConvertGoldEticToXliff(TestFileName,
				XDocument.Parse(
					"<eticPOSList><source/><item type='category' id='test' guid='810e6d78-4239-4902-9ff0-8fbcd2173d6e'>" +
					"<abbrev ws='" + WsEn + "'>test</abbrev>" +
					"<abbrev ws='" + WsEs + "'>prueba</abbrev>" +
					"<abbrev ws='" + WsDe + "'>Probe</abbrev>" +
					"<term ws='" + WsEn + "'>test</term>" +
					"<term ws='" + WsEs + "'>prueba</term>" +
					"<term ws='" + WsDe + "'>Probe</term>" +
					"<def ws='" + WsEn + "'>test</def>" +
					"<def ws='" + WsEs + "'>prueba</def>" +
					"<def ws='" + WsDe + "'>Probe</def></item></eticPOSList>"));

			Assert.AreEqual(3, xliffDocs.Count);
			Assert.Contains(WsEn, xliffDocs.Keys);
			Assert.Contains(WsEs, xliffDocs.Keys);
			Assert.Contains(WsDe, xliffDocs.Keys);

			var originalXpath = $"/xliff/file[@original='{TestFileName}']";
			AssertThatXmlIn.String(xliffDocs[WsEn].ToString()).HasSpecifiedNumberOfMatchesForXpath(originalXpath, 1);
			AssertThatXmlIn.String(xliffDocs[WsEs].ToString()).HasSpecifiedNumberOfMatchesForXpath(originalXpath, 1);
			AssertThatXmlIn.String(xliffDocs[WsDe].ToString()).HasSpecifiedNumberOfMatchesForXpath(originalXpath, 1);

			AssertThatXmlIn.String(xliffDocs[WsEn].ToString()).HasNoMatchForXpath("/xliff/file[@target-language]");
			AssertThatXmlIn.String(xliffDocs[WsEs].ToString()).HasSpecifiedNumberOfMatchesForXpath(
				$"/xliff/file[@target-language='{WsEs}']", 1);
			AssertThatXmlIn.String(xliffDocs[WsDe].ToString()).HasSpecifiedNumberOfMatchesForXpath(
				$"/xliff/file[@target-language='{WsDe}']", 1);
		}

		[Test]
		public void FileAttributes_NoPath()
		{
			const string fullPath = "D:/Some/Long/Path/" + TestFileName;
			var xliffDocs = GoldEticToXliff.ConvertGoldEticToXliff(fullPath,
				XDocument.Parse(
					"<eticPOSList><source/><item type='category' id='test' guid='810e6d78-4239-4902-9ff0-8fbcd2173d6e'>" +
					"<abbrev ws='" + WsEn + "'>test</abbrev>" +
					"<term ws='" + WsEn + "'>test</term>" +
					"<def ws='" + WsEn + "'>test</def></item></eticPOSList>"));

			Assert.AreEqual(1, xliffDocs.Count);
			Assert.Contains(WsEn, xliffDocs.Keys);

			AssertThatXmlIn.String(xliffDocs[WsEn].ToString()).HasSpecifiedNumberOfMatchesForXpath($"/xliff/file[@original='{TestFileName}']", 1);
			AssertThatXmlIn.String(xliffDocs[WsEn].ToString()).HasNoMatchForXpath($"/xliff/file[@original='{fullPath}']");
		}

		[Test]
		public void ItemHasAllData()
		{
			const string id = "Adjective";
			const string guid = "30d07580-5052-4d91-bc24-469b8b2d7df9";
			const string abbr = "adj";
			const string term = "Adjective";
			const string def = "An adjective is a part of speech whose members modify nouns.";
			const string cit1 = "Crystal 1997:8";
			const string cit2 = "Mish et al. 1990:56";
			const string cit3 = "Payne 1997:63";
			var xliffDocs = GoldEticToXliff.ConvertGoldEticToXliff(TestFileName,
				XDocument.Parse(@"<eticPOSList><source/><item type='category' id='" + id + @"' guid='" + guid + @"'>
		<abbrev ws='en'>" + abbr + @"</abbrev>
		<term ws='en'>" + term + @"</term>
		<def ws='en'>" + def + @"</def>
		<citation ws='en'>" + cit1 + @"</citation>
		<citation ws='en'>" + cit2 + @"</citation>
		<citation ws='en'>" + cit3 + @"</citation>
	</item>
</eticPOSList>"));


			Assert.AreEqual(1, xliffDocs.Count);
			Assert.Contains(WsEn, xliffDocs.Keys);
			var enXliff = xliffDocs[WsEn].ToString();

			const string itemXpath = "/xliff/file/body/group[@id='" + guid + "_" + id + "']";
			AssertThatXmlIn.String(enXliff).HasSpecifiedNumberOfMatchesForXpath(itemXpath, 1);
			const string tuXpath = itemXpath + "/trans-unit";
			AssertThatXmlIn.String(enXliff).HasSpecifiedNumberOfMatchesForXpath(
				tuXpath + "[@id='" + guid + "_" + id + "_abbr']/source[text()='" + abbr + "']", 1);
			AssertThatXmlIn.String(enXliff).HasSpecifiedNumberOfMatchesForXpath(
				tuXpath + "[@id='" + guid + "_" + id + "_term']/source[text()='" + term + "']", 1);
			AssertThatXmlIn.String(enXliff).HasSpecifiedNumberOfMatchesForXpath(
				tuXpath + "[@id='" + guid + "_" + id + "_def']/source[text()='" + def + "']", 1);
			AssertThatXmlIn.String(enXliff).HasSpecifiedNumberOfMatchesForXpath(
				$"{tuXpath}[@id='{guid}_{id}_cit']/source[text()='{cit1};{cit2};{cit3}']", 1);

			AssertThatXmlIn.String(enXliff).HasNoMatchForXpath(tuXpath + "[@id='" + guid + "_" + id + "_abbr']/target");
			AssertThatXmlIn.String(enXliff).HasNoMatchForXpath(tuXpath + "[@id='" + guid + "_" + id + "_term']/target");
			AssertThatXmlIn.String(enXliff).HasNoMatchForXpath(tuXpath + "[@id='" + guid + "_" + id + "_def']/target");
			AssertThatXmlIn.String(enXliff).HasNoMatchForXpath($"{tuXpath}[@id='{guid}_{id}_cit']/target");
		}

		[Test]
		public void ConvertsData()
		{
			const string id = "Adjective";
			const string guid = "30d07580-5052-4d91-bc24-469b8b2d7df9";
			const string abbrEn = "adj";
			const string abbrEs = "adj";
			const string abbrZh = "形";
			const string termEn = "Adjective";
			const string termEs = "Adjetivo";
			const string termZh = "形容词";
			const string defEn = "An adjective is a part of speech whose members modify nouns.";
			const string defEs = "Un adjectif est un modificateur du nom.";
			const string defZh = "容词";
			const string citEs1 = "Crystal 1997:8";
			const string citEs2 = "Mish et al. 1990:56";
			var xliffDocs = GoldEticToXliff.ConvertGoldEticToXliff(TestFileName,
				XDocument.Parse(@"<eticPOSList><source/><item type='category' id='" + id + @"' guid='" + guid + @"'>
		<abbrev ws='" + WsEn + @"'>" + abbrEn + @"</abbrev>
		<abbrev ws='" + WsEs + @"'>" + abbrEs + @"</abbrev>
		<abbrev ws='" + WsZh + @"'>" + abbrZh + @"</abbrev>
		<term ws='" + WsEn + @"'>" + termEn + @"</term>
		<term ws='" + WsEs + @"'>" + termEs + @"</term>
		<term ws='" + WsZh + @"'>" + termZh + @"</term>
		<def ws='" + WsEn + @"'>" + defEn + @"</def>
		<def ws='" + WsEs + @"'>" + defEs + @"</def>
		<def ws='" + WsZh + @"'>" + defZh + @"</def>
		<citation ws='" + WsEs + @"'>" + citEs1 + @"</citation>
		<citation ws='" + WsEs + @"'>" + citEs2 + @"</citation>
	</item>
</eticPOSList>"));


			Assert.AreEqual(3, xliffDocs.Count);
			Assert.Contains(WsEn, xliffDocs.Keys);
			Assert.Contains(WsEs, xliffDocs.Keys);
			Assert.Contains(WsZh, xliffDocs.Keys);

			var esXliff = xliffDocs[WsEs].ToString();
			var zhXliff = xliffDocs[WsZh].ToString();

			const string itemXpath = "/xliff/file/body/group[@id='" + guid + "_" + id + "']";
			AssertThatXmlIn.String(esXliff).HasSpecifiedNumberOfMatchesForXpath(itemXpath, 1);
			AssertThatXmlIn.String(zhXliff).HasSpecifiedNumberOfMatchesForXpath(itemXpath, 1);
			const string tuXpath = itemXpath + "/trans-unit";
			AssertThatXmlIn.String(esXliff).HasSpecifiedNumberOfMatchesForXpath(
				tuXpath + "[@id='" + guid + "_" + id + "_abbr']/target[text()='" + abbrEs + "']", 1);
			AssertThatXmlIn.String(esXliff).HasSpecifiedNumberOfMatchesForXpath(
				tuXpath + "[@id='" + guid + "_" + id + "_term']/target[text()='" + termEs + "']", 1);
			AssertThatXmlIn.String(esXliff).HasSpecifiedNumberOfMatchesForXpath(
				tuXpath + "[@id='" + guid + "_" + id + "_def']/target[text()='" + defEs + "']", 1);
			AssertThatXmlIn.String(esXliff).HasSpecifiedNumberOfMatchesForXpath(
				$"{tuXpath}[@id='{guid}_{id}_cit']/target[text()='{citEs1};{citEs2}']", 1);
			AssertThatXmlIn.String(zhXliff).HasSpecifiedNumberOfMatchesForXpath(
				tuXpath + "[@id='" + guid + "_" + id + "_abbr']/target[text()='" + abbrZh + "']", 1);
			AssertThatXmlIn.String(zhXliff).HasSpecifiedNumberOfMatchesForXpath(
				tuXpath + "[@id='" + guid + "_" + id + "_term']/target[text()='" + termZh + "']", 1);
			AssertThatXmlIn.String(zhXliff).HasSpecifiedNumberOfMatchesForXpath(
				tuXpath + "[@id='" + guid + "_" + id + "_def']/target[text()='" + defZh + "']", 1);
			AssertThatXmlIn.String(zhXliff).HasNoMatchForXpath($"{tuXpath}[@id='{guid}_{id}_cit']/target");
		}

		[Test]
		public void MissingDataDoesNotThrow()
		{
			const string id = "Adjective";
			const string guid = "30d07580-5052-4d91-bc24-469b8b2d7df9";
			const string abbrEn = "adj";
			const string abbrZh = "形";
			const string termEn = "Adjective";
			const string termEs = "Adjetivo";
			const string termZh = "形容词";
			const string defEn = "An adjective is a part of speech whose members modify nouns.";
			const string defEs = "Un adjectif est un modificateur du nom.";
			var xliffDocs = GoldEticToXliff.ConvertGoldEticToXliff(TestFileName,
				XDocument.Parse(@"<eticPOSList><source/><item type='category' id='" + id + @"' guid='" + guid + @"'>
		<abbrev ws='" + WsEn + @"'>" + abbrEn + @"</abbrev>
		<abbrev ws='" + WsZh + @"'>" + abbrZh + @"</abbrev>
		<term ws='" + WsEn + @"'>" + termEn + @"</term>
		<term ws='" + WsEs + @"'>" + termEs + @"</term>
		<term ws='" + WsZh + @"'>" + termZh + @"</term>
		<def ws='" + WsEn + @"'>" + defEn + @"</def>
		<def ws='" + WsEs + @"'>" + defEs + @"</def>
	</item>
</eticPOSList>"));


			Assert.AreEqual(3, xliffDocs.Count);
			Assert.Contains(WsEn, xliffDocs.Keys);
			Assert.Contains(WsEs, xliffDocs.Keys);
			Assert.Contains(WsZh, xliffDocs.Keys);

			var esXliff = xliffDocs[WsEs].ToString();
			var zhXliff = xliffDocs[WsZh].ToString();

			const string itemXpath = "/xliff/file/body/group[@id='" + guid + "_" + id + "']";
			AssertThatXmlIn.String(esXliff).HasSpecifiedNumberOfMatchesForXpath(itemXpath, 1);
			AssertThatXmlIn.String(zhXliff).HasSpecifiedNumberOfMatchesForXpath(itemXpath, 1);
			const string tuXpath = itemXpath + "/trans-unit";
			AssertThatXmlIn.String(esXliff).HasNoMatchForXpath(
				tuXpath + "[@id='" + guid + "_" + id + "_abbr']/target");
			AssertThatXmlIn.String(esXliff).HasSpecifiedNumberOfMatchesForXpath(
				tuXpath + "[@id='" + guid + "_" + id + "_term']/target[text()='" + termEs + "']", 1);
			AssertThatXmlIn.String(esXliff).HasSpecifiedNumberOfMatchesForXpath(
				tuXpath + "[@id='" + guid + "_" + id + "_def']/target[text()='" + defEs + "']", 1);
			AssertThatXmlIn.String(esXliff).HasNoMatchForXpath($"{tuXpath}[@id='{guid}_{id}_cit']/target");
			AssertThatXmlIn.String(zhXliff).HasSpecifiedNumberOfMatchesForXpath(
				tuXpath + "[@id='" + guid + "_" + id + "_abbr']/target[text()='" + abbrZh + "']", 1);
			AssertThatXmlIn.String(zhXliff).HasSpecifiedNumberOfMatchesForXpath(
				tuXpath + "[@id='" + guid + "_" + id + "_term']/target[text()='" + termZh + "']", 1);
			AssertThatXmlIn.String(zhXliff).HasNoMatchForXpath(
				tuXpath + "[@id='" + guid + "_" + id + "_def']/target");
			AssertThatXmlIn.String(zhXliff).HasNoMatchForXpath($"{tuXpath}[@id='{guid}_{id}_cit']/target");
		}

		[Test]
		public void ConvertsSubItems()
		{
			const string parentId = "ad-position";
			const string parentGuid = "ae115ea8-2cd7-4501-8ae7-dc638e4f17c5";
			const string id = "pre-position";
			const string guid = "923e5aed-d84a-48b0-9b7a-331c1336864a";
			const string abbrEn = "prp";
			const string abbrEs = "prep";
			const string termEn = "Preposition";
			const string termEs = "Preposición";
			const string defEn = "A preposition is an adposition that occurs before its complement.";
			const string defEs = "Una preposición es una adposición.";
			const string citEn = "Crystal 1997:305";
			const string citEs1 = "Mish et al. 1990:929";
			const string citEs2 = "Payne 1997:86";
			var xliffDocs = GoldEticToXliff.ConvertGoldEticToXliff(TestFileName,
				XDocument.Parse(@"<eticPOSList><source/><item type='category' id='" + parentId + @"' guid='" + parentGuid + @"'>
		<abbrev ws='en'>adp</abbrev>
		<abbrev ws='es'>adpos</abbrev>
		<term ws='en'>Adposition</term>
		<term ws='es'>Adposición</term>
		<def ws='en'>An adposition is a part of speech whose members are of a closed set and occur before or after a complement composed of a noun phrase, noun, pronoun, or clause that functions as a noun phrase and forms a single structure with the complement to express its grammatical and semantic relation to another unit within a clause.</def>
		<def ws='es'/>
		<item type='category' id='" + id + @"' guid='" + guid + @"'>
			<abbrev ws='" + WsEn + @"'>" + abbrEn + @"</abbrev>
			<abbrev ws='" + WsEs + @"'>" + abbrEs + @"</abbrev>
			<term ws='" + WsEn + @"'>" + termEn + @"</term>
			<term ws='" + WsEs + @"'>" + termEs + @"</term>
			<def ws='" + WsEn + @"'>" + defEn + @"</def>
			<def ws='" + WsEs + @"'>" + defEs + @"</def>
			<citation ws='" + WsEn + @"'>" + citEn + @"</citation>
			<citation ws='" + WsEs + @"'>" + citEs1 + @"</citation>
			<citation ws='" + WsEs + @"'>" + citEs2 + @"</citation>
		</item>
	</item>
</eticPOSList>"));


			Assert.AreEqual(2, xliffDocs.Count);
			Assert.Contains(WsEn, xliffDocs.Keys);
			Assert.Contains(WsEs, xliffDocs.Keys);
			var esXliff = xliffDocs[WsEs].ToString();

			const string itemXpath = "/xliff/file/body/group[@id='" + parentGuid + "_" + parentId + "']/group[@id='" + guid + "_" + id + "']";
			AssertThatXmlIn.String(esXliff).HasSpecifiedNumberOfMatchesForXpath(itemXpath, 1);
			const string tuXpath = itemXpath + "/trans-unit";
			AssertThatXmlIn.String(esXliff).HasSpecifiedNumberOfMatchesForXpath(
				tuXpath + "[@id='" + guid + "_" + id + "_abbr']/source[text()='" + abbrEn + "']", 1);
			AssertThatXmlIn.String(esXliff).HasSpecifiedNumberOfMatchesForXpath(
				tuXpath + "[@id='" + guid + "_" + id + "_term']/source[text()='" + termEn + "']", 1);
			AssertThatXmlIn.String(esXliff).HasSpecifiedNumberOfMatchesForXpath(
				tuXpath + "[@id='" + guid + "_" + id + "_def']/source[text()='" + defEn + "']", 1);
			AssertThatXmlIn.String(esXliff).HasSpecifiedNumberOfMatchesForXpath(
				$"{tuXpath}[@id='{guid}_{id}_cit']/source[text()='{citEn}']", 1);
			AssertThatXmlIn.String(esXliff).HasSpecifiedNumberOfMatchesForXpath(
				tuXpath + "[@id='" + guid + "_" + id + "_abbr']/target[text()='" + abbrEs + "']", 1);
			AssertThatXmlIn.String(esXliff).HasSpecifiedNumberOfMatchesForXpath(
				tuXpath + "[@id='" + guid + "_" + id + "_term']/target[text()='" + termEs + "']", 1);
			AssertThatXmlIn.String(esXliff).HasSpecifiedNumberOfMatchesForXpath(
				tuXpath + "[@id='" + guid + "_" + id + "_def']/target[text()='" + defEs + "']", 1);
			AssertThatXmlIn.String(esXliff).HasSpecifiedNumberOfMatchesForXpath(
				$"{tuXpath}[@id='{guid}_{id}_cit']/target[text()='{citEs1};{citEs2}']", 1);
		}

		[Test]
		public void TranslationState()
		{
			const string id = "Adjective";
			const string guid = "30d07580-5052-4d91-bc24-469b8b2d7df9";
			const string abbrEn = "adj";
			const string abbrEs = "adj";
			const string abbrZh = "";
			const string termEn = "Adjective";
			const string termEs = "Adjetivo";
			const string termZh = "";
			const string defEn = "An adjective is a part of speech whose members modify nouns.";
			const string defEs = "Un adjectif est un modificateur du nom.";
			const string defZh = "";
			const string citEs1 = "Crystal 1997:8";
			const string citEs2 = "Mish et al. 1990:56";
			var xliffDocs = GoldEticToXliff.ConvertGoldEticToXliff(TestFileName,
				XDocument.Parse(@"<eticPOSList><source/><item type='category' id='" + id + @"' guid='" + guid + @"'>
		<abbrev ws='" + WsEn + @"'>" + abbrEn + @"</abbrev>
		<abbrev ws='" + WsEs + @"'>" + abbrEs + @"</abbrev>
		<abbrev ws='" + WsZh + @"'>" + abbrZh + @"</abbrev>
		<term ws='" + WsEn + @"'>" + termEn + @"</term>
		<term ws='" + WsEs + @"'>" + termEs + @"</term>
		<term ws='" + WsZh + @"'>" + termZh + @"</term>
		<def ws='" + WsEn + @"'>" + defEn + @"</def>
		<def ws='" + WsEs + @"'>" + defEs + @"</def>
		<def ws='" + WsZh + @"'>" + defZh + @"</def>
		<citation ws='" + WsEs + @"'>" + citEs1 + @"</citation>
		<citation ws='" + WsEs + @"'>" + citEs2 + @"</citation>
	</item>
</eticPOSList>"));


			Assert.AreEqual(3, xliffDocs.Count);
			Assert.Contains(WsEn, xliffDocs.Keys);
			Assert.Contains(WsEs, xliffDocs.Keys);
			Assert.Contains(WsZh, xliffDocs.Keys);

			var esXliff = xliffDocs[WsEs].ToString();
			var zhXliff = xliffDocs[WsZh].ToString();

			const string itemXpath = "/xliff/file/body/group[@id='" + guid + "_" + id + "']";
			AssertThatXmlIn.String(esXliff).HasSpecifiedNumberOfMatchesForXpath(itemXpath, 1);
			AssertThatXmlIn.String(zhXliff).HasSpecifiedNumberOfMatchesForXpath(itemXpath, 1);
			const string tuXpath = itemXpath + "/trans-unit";
			AssertThatXmlIn.String(esXliff).HasSpecifiedNumberOfMatchesForXpath(
				tuXpath + "[@id='" + guid + "_" + id + "_abbr']/target[@state='final' and text()='" + abbrEs + "']", 1);
			AssertThatXmlIn.String(esXliff).HasSpecifiedNumberOfMatchesForXpath(
				tuXpath + "[@id='" + guid + "_" + id + "_term']/target[@state='final' and text()='" + termEs + "']", 1);
			AssertThatXmlIn.String(esXliff).HasSpecifiedNumberOfMatchesForXpath(
				tuXpath + "[@id='" + guid + "_" + id + "_def']/target[@state='final' and text()='" + defEs + "']", 1);
			AssertThatXmlIn.String(esXliff).HasSpecifiedNumberOfMatchesForXpath(
				$"{tuXpath}[@id='{guid}_{id}_cit']/target[@state='{XliffUtils.State.Final}' and text()='{citEs1};{citEs2}']", 1);

			AssertThatXmlIn.String(zhXliff).HasNoMatchForXpath(tuXpath + "[@id='" + guid + "_" + id + "_abbr']/target");
			AssertThatXmlIn.String(zhXliff).HasNoMatchForXpath(tuXpath + "[@id='" + guid + "_" + id + "_term']/target");
			AssertThatXmlIn.String(zhXliff).HasNoMatchForXpath(tuXpath + "[@id='" + guid + "_" + id + "_def']/target");
			AssertThatXmlIn.String(zhXliff).HasNoMatchForXpath(tuXpath + "[@id='" + guid + "_" + id + "_cit']/target");
		}

		/// <summary>
		/// Test with an export of TranslatedLists from FieldWorks (you must add a second analysis language to enable this option)
		/// </summary>
		[Ignore("Facilitates human inspection of output files")]
		[Category("ByHand")]
		[Test]
		public void IntegrationTest()
		{
			// clean and create the output directory
			const string outputDir = @"C:\WorkingFiles\XliffGoldEtic";
			TaskTestUtils.RecreateDirectory(outputDir);

			Assert.True(new GoldEticToXliff
			{
				SourceXml = @"..\..\..\..\DistFiles\Templates\GOLDEtic.xml",
				XliffOutputDir = outputDir
			}.Execute());

			var outputFiles = Directory.GetFiles(outputDir).Where(f => !f.EndsWith(".en.xlf")).ToArray();

			Assert.True(new XliffToGoldEtic
			{
				XliffSourceFiles = outputFiles,
				OutputXml = Path.Combine(outputDir, "..", "GOLDEticRoundtripped.xml")
			}.Execute());
		}
	}
}