// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
	internal class XliffToGoldEticTests
	{
		private const string WsEn = "en";
		private const string WsEs = "es";
		private const string WsZh = "zh-CN";

		private TestBuildEngine _tbi;
		private XliffToGoldEtic _task;

		[SetUp]
		public void TestSetup()
		{
			_tbi = new TestBuildEngine();
			_task = new XliffToGoldEtic { BuildEngine = _tbi };
		}

		[Test]
		public void SourceConverted()
		{
			const string source = "based on <link>http://www.linguistics-ontology.org/ns/gold/0.2/gold-pos.owl</link> &amp; modified by hand.";
			var xlfEn = XDocument.Parse(@"<xliff version='1.2' xmlns:sil='software.sil.org'>
	<file source-language='EN' datatype='plaintext' original='test.xml'>
		<body>
			<sil:source>" + source + @"</sil:source>
		</body></file></xliff>");

			var result = _task.CombineXliffs(new List<XDocument> {xlfEn});

			const string sourceXpath = "/eticPOSList/source[text()]";
			AssertThatXmlIn.String(result.ToString()).HasSpecifiedNumberOfMatchesForXpath(sourceXpath, 1);
			// ReSharper disable once PossibleNullReferenceException -- ReSharper doesn't understand the assert on the previous line
			Assert.That(result.XPathSelectElement(sourceXpath).ToString(), Does.Contain(source));
		}

		[Test]
		public void ItemsConverted()
		{
			const string id = "Adjective";
			const string guid = "30d07580-5052-4d91-bc24-469b8b2d7df9";
			var xlfEn = XDocument.Parse($@"<xliff version='1.2' xmlns:sil='software.sil.org'>
	<file source-language='EN' datatype='plaintext' original='test.xml'>
		<body><sil:source/>
			{BuildXliffGroup(id, guid)}
		</body></file></xliff>");

			var result = _task.CombineXliffs(new List<XDocument> {xlfEn}).ToString();

			const string itemXpath = "/eticPOSList/item";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(itemXpath, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(itemXpath +
				"[@type='category' and @id='" + id + "' and @guid='" + guid + "']", 1);
		}

		[Test]
		public void MissingItemLogsError()
		{
			const string id = "Adjective";
			const string guid = "30d07580-5052-4d91-bc24-469b8b2d7df9";
			const string abbrEn = "adj";
			const string abbrEs = "adj";
			const string termEn = "Adjective";
			const string termEs = "Adjetivo";
			const string defEn = "An adjective is a part of speech whose members modify nouns.";
			const string defEs = "Un adjectif est un modificateur du nom.";
			var xlfEs = XDocument.Parse($@"<xliff version='1.2' xmlns:sil='software.sil.org'>
	<file source-language='EN' datatype='plaintext' original='test.xml' target-language='{WsEs}'>
		<body><sil:source/>
			{BuildXliffGroup(id, guid, abbrEn, abbrEs, termEn, termEs, defEn, defEs, string.Empty, string.Empty)}
		</body></file></xliff>");
			var xlfZh = XDocument.Parse($@"<xliff version='1.2' xmlns:sil='software.sil.org'>
	<file source-language='EN' datatype='plaintext' original='test.xml' target-language='{WsZh}'>
		<body><sil:source/></body></file></xliff>");

			_task.CombineXliffs(new List<XDocument> {xlfEs, xlfZh});

			Assert.That(_tbi.Errors.Any(e => e.Contains("different number of items")));

			_tbi.Errors.Clear();

			// test the other order for good measure
			_task.CombineXliffs(new List<XDocument> {xlfZh, xlfEs});

			Assert.That(_tbi.Errors.Any(e => e.Contains("different number of items")));
		}

		[Test]
		public void MissingTargetTolerated()
		{
			const string id = "Adjective";
			const string guid = "30d07580-5052-4d91-bc24-469b8b2d7df9";
			const string abbrEn = "adj";
			const string abbrEs = "adj";
			const string termEn = "Adjective";
			const string termEs = "Adjetivo";
			const string defEn = "An adjective is a part of speech whose members modify nouns.";
			const string defEs = "Un adjectif est un modificateur du nom.";
			var xlfEs = XDocument.Parse($@"<xliff version='1.2' xmlns:sil='software.sil.org'>
	<file source-language='EN' datatype='plaintext' original='test.xml' target-language='{WsEs}'>
		<body><sil:source/>
			{BuildXliffGroup(id, guid, abbrEn, abbrEs, termEn, termEs, defEn, defEs, string.Empty, string.Empty)}
		</body></file></xliff>");

			foreach (var target in xlfEs.XPathSelectElements("//target").ToArray())
			{
				target.Remove();
			}

			_task.CombineXliffs(new List<XDocument> {xlfEs});

			Assert.False(_tbi.Errors.Any());
		}

		[Test]
		public void LocalesNormalized()
		{
			const string id = "Adjective";
			const string guid = "30d07580-5052-4d91-bc24-469b8b2d7df9";
			const string localeEs = WsEs + "-ES";
			var xlfEs = XDocument.Parse($@"<xliff version='1.2' xmlns:sil='software.sil.org'>
	<file source-language='EN' datatype='plaintext' original='test.xml' target-language='{localeEs}'>
		<body><sil:source/>
			{BuildXliffGroup(id, guid)}
		</body></file></xliff>");
			var xlfZh = XDocument.Parse($@"<xliff version='1.2' xmlns:sil='software.sil.org'>
	<file source-language='EN' datatype='plaintext' original='test.xml' target-language='{WsZh}'>
		<body><sil:source/>
			{BuildXliffGroup(id, guid)}
		</body></file></xliff>");

			var result = _task.CombineXliffs(new List<XDocument> {xlfEs, xlfZh}).ToString();

			AssertThatXmlIn.String(result).HasNoMatchForXpath("//*[@ws='" + localeEs + "']");

			const string itemXpath = "/eticPOSList/item/";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(itemXpath + "/abbrev[@ws='" + WsEs + "']", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(itemXpath + "/abbrev[@ws='" + WsZh + "']", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(itemXpath + "/term[@ws='" + WsEs + "']", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(itemXpath + "/term[@ws='" + WsZh + "']", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(itemXpath + "/def[@ws='" + WsEs + "']", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(itemXpath + "/def[@ws='" + WsZh + "']", 1);
		}

		[Test]
		public void DifferentItemLogsError()
		{
			const string idEs = "Adjective";
			const string idZh = "Adposition";
			const string guidEs = "30d07580-5052-4d91-bc24-469b8b2d7df9";
			const string guidZh = "ae115ea8-2cd7-4501-8ae7-dc638e4f17c5";
			var xlfEs = XDocument.Parse($@"<xliff version='1.2' xmlns:sil='software.sil.org'>
	<file source-language='EN' datatype='plaintext' original='test.xml' target-language='{WsEs}'>
		<body><sil:source/>
			{BuildXliffGroup(idEs, guidEs)}
		</body></file></xliff>");
			var xlfZh = XDocument.Parse($@"<xliff version='1.2' xmlns:sil='software.sil.org'>
	<file source-language='EN' datatype='plaintext' original='test.xml' target-language='{WsZh}'>
		<body><sil:source/>
			{BuildXliffGroup(idZh, guidZh)}
		</body></file></xliff>");

			_task.CombineXliffs(new List<XDocument> {xlfEs, xlfZh});

			Assert.That(_tbi.Errors.Any(e => e.Contains(guidEs + "_" + idEs)));
		}

		[Test]
		public void ItemsConvertedForAllLanguages()
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
			const string citEn1 = "Crystal 1997:8";
			const string citEn2 = "Peterson 2003:2041";
			const string citEn = citEn1 + ";" + citEn2;
			const string citEs = "Mish et al. 1990:56";
			var xlfEs = XDocument.Parse($@"<xliff version='1.2' xmlns:sil='software.sil.org'>
	<file source-language='EN' datatype='plaintext' original='test.xml' target-language='{WsEs}'>
		<body><sil:source/>
			{BuildXliffGroup(id, guid, abbrEn, abbrEs, termEn, termEs, defEn, defEs, citEn, citEs)}
		</body></file></xliff>");
			var xlfZh = XDocument.Parse($@"<xliff version='1.2' xmlns:sil='software.sil.org'>
	<file source-language='EN' datatype='plaintext' original='test.xml' target-language='{WsZh}'>
		<body><sil:source/>
			{BuildXliffGroup(id, guid, abbrEn, abbrZh, termEn, termZh, defEn, defZh, citEn, string.Empty)}
		</body></file></xliff>");

			var result = _task.CombineXliffs(new List<XDocument> {xlfEs, xlfZh}).ToString();

			const string itemXpath = "/eticPOSList/item[@type='category' and @id='" + id + "' and @guid='" + guid + "']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(itemXpath, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(itemXpath + "/abbrev", 3);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(itemXpath + "/term", 3);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(itemXpath + "/def", 3);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(itemXpath + "/citation", 3); // 2 English, 1 Spanish, 0 Chinese
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				itemXpath + "/abbrev[@ws='" + WsEn + "' and text()='" + abbrEn + "']", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				itemXpath + "/term[@ws='" + WsEn + "' and text()='" + termEn + "']", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				itemXpath + "/def[@ws='" + WsEn + "' and text()='" + defEn + "']", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				itemXpath + "/citation[@ws='" + WsEn + "' and text()='" + citEn1 + "']", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				itemXpath + "/citation[@ws='" + WsEn + "' and text()='" + citEn2 + "']", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				itemXpath + "/abbrev[@ws='" + WsEs + "' and text()='" + abbrEs + "']", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				itemXpath + "/term[@ws='" + WsEs + "' and text()='" + termEs + "']", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				itemXpath + "/def[@ws='" + WsEs + "' and text()='" + defEs + "']", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				itemXpath + "/citation[@ws='" + WsEs + "' and text()='" + citEs + "']", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				itemXpath + "/abbrev[@ws='" + WsZh + "' and text()='" + abbrZh + "']", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				itemXpath + "/term[@ws='" + WsZh + "' and text()='" + termZh + "']", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				itemXpath + "/def[@ws='" + WsZh + "' and text()='" + defZh + "']", 1);
		}

		[Test]
		public void SubItemsConverted()
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
			var xlfEs = XDocument.Parse($@"<xliff version='1.2' xmlns:sil='software.sil.org'>
	<file source-language='EN' datatype='plaintext' original='test.xml' target-language='{WsEs}'>
		<body><sil:source/>
			{BuildXliffGroup(parentId, parentGuid,
				BuildXliffGroup(id, guid, abbrEn, abbrEs, termEn, termEs, defEn, defEs, citEn, $"{citEs1};{citEs2}"))}
		</body></file></xliff>");

			var result = _task.CombineXliffs(new List<XDocument> {xlfEs}).ToString();

			const string parentXpath = "/eticPOSList/item[@type='category' and @id='" + parentId + "' and @guid='" + parentGuid + "']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(parentXpath, 1);
			const string subItemXpath = parentXpath + "/item[@type='category' and @id='" + id + "' and @guid='" + guid + "']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subItemXpath, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subItemXpath + "/abbrev", 2);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subItemXpath + "/term", 2);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subItemXpath + "/def", 2);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subItemXpath + "/citation", 3); // 1 English, 2 Spanish
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				subItemXpath + "/abbrev[@ws='" + WsEn + "' and text()='" + abbrEn + "']", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				subItemXpath + "/term[@ws='" + WsEn + "' and text()='" + termEn + "']", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				subItemXpath + "/def[@ws='" + WsEn + "' and text()='" + defEn + "']", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				subItemXpath + "/citation[@ws='" + WsEn + "' and text()='" + citEn + "']", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				subItemXpath + "/abbrev[@ws='" + WsEs + "' and text()='" + abbrEs + "']", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				subItemXpath + "/term[@ws='" + WsEs + "' and text()='" + termEs + "']", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				subItemXpath + "/def[@ws='" + WsEs + "' and text()='" + defEs + "']", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				subItemXpath + "/citation[@ws='" + WsEs + "' and text()='" + citEs1 + "']", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				subItemXpath + "/citation[@ws='" + WsEs + "' and text()='" + citEs2 + "']", 1);
		}

		[TestCase(XliffUtils.State.NeedsTranslation, false)]
		[TestCase(XliffUtils.State.Translated, true)]
		[TestCase(XliffUtils.State.Final, true)]
		public void TranslationStateChecked(string state, bool isTranslated)
		{
			const string id = "Adjective";
			const string guid = "30d07580-5052-4d91-bc24-469b8b2d7df9";
			const string abbrEn = "adj";
			const string abbrEs = "adj";
			const string termEn = "Adjective";
			const string termEs = "Adjetivo";
			const string defEn = "An adjective is a part of speech whose members modify nouns.";
			const string defEs = "Un adjectif est un modificateur du nom.";
			const string citEn = "Crystal 1997:8";
			const string citEs = "Mish et al. 1990:56";
			var xlfEs = XDocument.Parse($@"<xliff version='1.2' xmlns:sil='software.sil.org'>
	<file source-language='EN' datatype='plaintext' original='test.xml' target-language='{WsEs}'>
		<body><sil:source/>
			{BuildXliffGroup(id, guid, abbrEn, abbrEs, termEn, termEs, defEn, defEs, citEn, citEs)}
		</body></file></xliff>");
			foreach (var targetElt in xlfEs.XPathSelectElements("//target"))
			{
				// ReSharper disable once PossibleNullReferenceException
				targetElt.Attribute("state").Value = state;
			}

			var result = _task.CombineXliffs(new List<XDocument> {xlfEs}).ToString();

			const string abbrevXpath = "/eticPOSList/item/abbrev";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(abbrevXpath, isTranslated ? 2 : 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				abbrevXpath + "[@ws='" + WsEs + "' and text()='" + abbrEs + "']", isTranslated ? 1 : 0);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				abbrevXpath + "[@ws='" + WsEn + "' and text()='" + abbrEn + "']", 1);
			const string termXpath = "/eticPOSList/item/term";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(termXpath, isTranslated ? 2 : 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				termXpath + "[@ws='" + WsEs + "' and text()='" + termEs + "']", isTranslated ? 1 : 0);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				termXpath + "[@ws='" + WsEn + "' and text()='" + termEn + "']", 1);
			const string defXpath = "/eticPOSList/item/def";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(defXpath, isTranslated ? 2 : 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				defXpath + "[@ws='" + WsEs + "' and text()='" + defEs + "']", isTranslated ? 1 : 0);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				defXpath + "[@ws='" + WsEn + "' and text()='" + defEn + "']", 1);
			const string citationXpath = "/eticPOSList/item/citation";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(citationXpath, isTranslated ? 2 : 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				citationXpath + "[@ws='" + WsEs + "' and text()='" + citEs + "']", isTranslated ? 1 : 0);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				citationXpath + "[@ws='" + WsEn + "' and text()='" + citEn + "']", 1);
		}

		private string BuildXliffGroup(string id, string guid, string subItems = "")
		{
			return BuildXliffGroup(id, guid, "abbr", "abr", "term", "terme", "def", "dffn", string.Empty, string.Empty, subItems);
		}

		private string BuildXliffGroup(string id, string guid,
			string abbrSrc, string abbrTrg, string termSrc, string termTrg, string defSrc, string defTrg, string citSrc, string citTrg,
			string subItems = "")
		{
			return $@"<group id='{guid}_{id}'>
				<trans-unit id='{guid}_{id}_abbr'>
					<source>{abbrSrc}</source>
					<target state='{XliffUtils.State.Final}'>{abbrTrg}</target>
				</trans-unit>
				<trans-unit id='{guid}_{id}_term'>
					<source>{termSrc}</source>
					<target state='{XliffUtils.State.Final}'>{termTrg}</target>
				</trans-unit>
				<trans-unit id='{guid}_{id}_def'>
					<source>{defSrc}</source>
					<target state='{XliffUtils.State.Final}'>{defTrg}</target>
				</trans-unit>
				<trans-unit id='{guid}_{id}_cit'>
					<source>{citSrc}</source>
					<note>this TU has a note. It should be ignored by the converter.</note>
					<target state='{XliffUtils.State.Final}'>{citTrg}</target>
				</trans-unit>
				{subItems}
			</group>";
		}
	}
}