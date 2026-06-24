// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using NUnit.Framework;
using SIL.TestUtilities;
using Sfm2Xml;
using System.Text;
using System.Linq;
using System.Xml;

namespace Sfm2XmlTests
{
	[TestFixture]
	public class ConverterTests
	{
		[Test]
		public void ConverterHandlesSubEntryExampleFollowedByEntry()
		{
			const string sfmString = @"\lx a
\ps v
\ge a
\se aa
\xe Should be definition

\lx ab
\ps v
\ge ab";
			const string mappingString = @"<sfmMapping version='6.1'>
<settings>
<meaning app='fw.sil.org'/>
</settings>
<options>
<option id='chkCreateMissingLinks' type='Checkbox' checked='False'/>
</options>
<languages>
<langDef id='National' xml:lang='ignore'/>
<langDef id='English' xml:lang='en'/>
<langDef id='Vernacular' xml:lang='fr'/>
<langDef id='Regional' xml:lang='ignore'/>
</languages>
<hierarchy>
<level name='Entry' partOf='records' beginFields='lx'/>
<level name='Example' partOf='Sense' beginFields='ignore'/>
<level name='ExampleTranslation' partOf='Example' beginFields='xe'/>
<level name='Function' partOf='Subentry Sense Entry' beginFields='ignore'/>
<level name='Picture' partOf='Sense' beginFields='ignore'/>
<level name='Pronunciation' partOf='Entry Subentry' beginFields='ignore'/>
<level name='SemanticDomain' partOf='Sense' beginFields='ignore'/>
<level name='Sense' partOf='Entry Subentry' beginFields='ge ps'/>
<level name='Subentry' partOf='Entry' beginFields='se' uniqueFields='se'/>
</hierarchy>
<fieldDescriptions>
<field sfm='ps' name='Category (Part Of Speech)' type='string' lang='English' abbr='True' >
<meaning app='fw.sil.org' id='pos'/>
</field>
<field sfm='ge' name='Gloss' type='string' lang='English' >
<meaning app='fw.sil.org' id='glos'/>
</field>
<field sfm='lx' name='Lexeme Form' type='string' lang='Vernacular' >
<meaning app='fw.sil.org' id='lex'/>
</field>
<field sfm='se' name='Subentry/Complex Form' type='string' lang='Vernacular' >
<meaning app='fw.sil.org' id='sub' funcWS='en' func='Unspecified Complex Form'/>
</field>
<field sfm='xe' name='Example Translation' type='string' lang='English' >
<meaning app='fw.sil.org' id='trans'/>
</field>
</fieldDescriptions>
</sfmMapping>";
			var sfmFile = Path.GetTempFileName();
			var mappingFile = Path.GetTempFileName();
			var outputFile = Path.GetTempFileName();
			File.WriteAllText(sfmFile, sfmString);
			File.WriteAllText(mappingFile, mappingString);
			var converter = new Converter(null);
			converter.Convert(sfmFile, mappingFile, outputFile);
			AssertThatXmlIn.File(outputFile).HasSpecifiedNumberOfMatchesForXpath("//Entry", 2);
			AssertThatXmlIn.File(outputFile).HasSpecifiedNumberOfMatchesForXpath("//Entry/Subentry", 1);
		}

		[Test]
		public void ConverterNormalizesTextToNfd()
		{
			// NFC form: é (U+00E9)
			const string composed = "\u00E9";

			// SFM input containing NFC text
			string sfmString = $@"\lx {composed}
\ps n
\ge test";

			// Reuse the same mapping as other tests
			const string mappingString = @"<sfmMapping version='6.1'>
<settings>
<meaning app='fw.sil.org'/>
</settings>
<languages>
<langDef id='English' xml:lang='en'/>
<langDef id='Vernacular' xml:lang='fr'/>
</languages>
<hierarchy>
<level name='Entry' partOf='records' beginFields='lx'/>
<level name='Sense' partOf='Entry' beginFields='ge ps'/>
</hierarchy>
<fieldDescriptions>
<field sfm='lx' name='Lexeme Form' type='string' lang='Vernacular'/>
<field sfm='ps' name='Category' type='string' lang='English'/>
<field sfm='ge' name='Gloss' type='string' lang='English'/>
</fieldDescriptions>
</sfmMapping>";

			var sfmFile = Path.GetTempFileName();
			var mappingFile = Path.GetTempFileName();
			var outputFile = Path.GetTempFileName();

			File.WriteAllText(sfmFile, sfmString);
			File.WriteAllText(mappingFile, mappingString);

			var converter = new Converter(null);
			converter.Convert(sfmFile, mappingFile, outputFile);

			// Extract the lexeme text from output XML
			var doc = new XmlDocument();
			doc.Load(outputFile);

			var lexemeNode = doc.SelectSingleNode("//lx | //LexemeForm | //Lexeme");
			Assert.NotNull(lexemeNode, "Lexeme node was not found in output XML");

			string outputText = lexemeNode.InnerText;

			// Assert normalization
			Assert.IsTrue(IsNfd(outputText),
				$"Expected NFD normalization, but got: {string.Join(" ", outputText.Select(c => $"U+{(int)c:X4}"))}");
		}

		[Test]
		public void ConverterPreservesSupplementaryPlaneCharacters()
		{
			// Wancho letters in the Supplementary Multilingual Plane (U+1E2C0 block),
			// each encoded in .NET as a UTF-16 surrogate pair. Previously the importer
			// stripped these as "invalid" characters because the validity check omitted
			// the U+10000-U+10FFFF range (LT-20644).
			const string supplementary = "\U0001E2CC\U0001E2C1\U0001E2D4"; // three Wancho letters

			string sfmString = $@"\lx {supplementary}
\ps n
\ge test";

			const string mappingString = @"<sfmMapping version='6.1'>
<settings>
<meaning app='fw.sil.org'/>
</settings>
<languages>
<langDef id='English' xml:lang='en'/>
<langDef id='Vernacular' xml:lang='fr'/>
</languages>
<hierarchy>
<level name='Entry' partOf='records' beginFields='lx'/>
<level name='Sense' partOf='Entry' beginFields='ge ps'/>
</hierarchy>
<fieldDescriptions>
<field sfm='lx' name='Lexeme Form' type='string' lang='Vernacular'/>
<field sfm='ps' name='Category' type='string' lang='English'/>
<field sfm='ge' name='Gloss' type='string' lang='English'/>
</fieldDescriptions>
</sfmMapping>";

			var sfmFile = Path.GetTempFileName();
			var mappingFile = Path.GetTempFileName();
			var outputFile = Path.GetTempFileName();

			// SFM files are read as UTF-8 by the importer.
			File.WriteAllText(sfmFile, sfmString, new UTF8Encoding(false));
			File.WriteAllText(mappingFile, mappingString);

			var converter = new Converter(null);
			converter.Convert(sfmFile, mappingFile, outputFile);

			var doc = new XmlDocument();
			doc.Load(outputFile);

			var lexemeNode = doc.SelectSingleNode("//lx | //LexemeForm | //Lexeme");
			Assert.NotNull(lexemeNode, "Lexeme node was not found in output XML");

			// The supplementary characters must survive the import unchanged
			// (NFD normalization leaves these code points untouched).
			Assert.AreEqual(supplementary, lexemeNode.InnerText,
				"Supplementary-plane characters were not preserved during SFM import");
		}

		private static bool IsNfd(string s)
		{
			return s == s.Normalize(NormalizationForm.FormD);
		}
	}
}
