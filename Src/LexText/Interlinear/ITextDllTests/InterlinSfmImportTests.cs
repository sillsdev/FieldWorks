using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using NUnit.Framework;
using Sfm2Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.Test.TestUtils;
using SilEncConverters40;

namespace SIL.FieldWorks.IText
{
	[TestFixture]
	public class InterlinSfmImportTests : BaseTest
	{
		[SuppressMessage("Gendarme.Rules.Portability", "NewLineLiteralRule",
			Justification="New lines in input strings are different depending on platform")]
		public InterlinSfmImportTests()
		{
		}

		private string input1 =
			@"\_sh v3.0  943  S Texts
\id Abu
\name Abu Nawas

\au Guna Bte Rintal
\com a funny story (folk tale?) about the relationship between Abu Nawas and a king
\comp Edith Mirafuentes (and revised by Kaili Bte Said)

\p
\ref Abu Nawas 001
\t $$Uun   kono'      serita', dau-dau  (tu)   kisa  Abu Nawas.
\m uun   kono'      serita' dau dau  tu   kisa  Abu Nawas
\ge exist it_is_said story   long_ago this story Abu Nawas
\ps vi    adv        n       adv      deic n     propN

\f There was, it is said, a story about Abu Nawas.
\f JT added some more.
\lt There existed, it is said, long ago, this story about Abu Nawas
\nt sentence adjunct: reported speech
\nt Example hacked by JohnT to exemplify more cases

\ref Abu Nawas 002
\t Abu Nawas kerjo ta'  rojo
\m Abu Nawas kerjo ta'  rojo
\ge Abu Nawas work  at   king
\ps propN     vi    prep n

\f Abu Nawas worked for the king.

\p
\ref Abu Nawas 003
\t John added this
\f and this

\id Jt

\ref Jt 001
\t A second text
\unk some unknown dat to ignore
\t in two parts
\f its free translation
\lt its literal translation
\f an orphan FT
\p
\t second para
\f ft of second para first seg
\t second para second sentence
";

		/// <summary>
		/// This tests out most aspects of the conversion.
		/// </summary>
		[Test]
		public void BasicConversion()
		{
			var mappings = GetMappings();
			mappings.Add(new InterlinearMapping() { Marker = "id", Destination = InterlinDestination.Abbreviation, WritingSystem = "en" });
			var wsf = GetWsf();
			var input = new ByteReader("input1", Encoding.UTF8.GetBytes(input1));
			var converter = new Sfm2FlexText();
			var output = converter.Convert(input, mappings, wsf);
			using (var outputStream = new MemoryStream(output))
			{
				using (var reader = new StreamReader(outputStream))
				{
					var outputElt = XElement.Load(reader);
					Assert.That(outputElt.Name.LocalName, Is.EqualTo("document"));
					var textElt = outputElt.Element("interlinear-text");
					Assert.That(textElt, Is.Not.Null);
					VerifyItem(textElt, "./item[@type='title']", "fr", "Abu Nawas");
					VerifyItem(textElt, "./item[@type='title-abbreviation']", "en", "Abu");
					VerifyItem(textElt, "./item[@type='source']", "en", "Guna Bte Rintal");
					VerifyItem(textElt, "./item[@type='comment']", "en", "a funny story (folk tale?) about the relationship between Abu Nawas and a king");
					var paragraphs = textElt.Element("paragraphs");
					Assert.IsNotNull(paragraphs);
					var para = paragraphs.Element("paragraph");
					Assert.IsNotNull(para);
					var phrases = para.Element("phrases");
					Assert.IsNotNull(phrases);

					var phrase1 = phrases.Element("phrase");
					Assert.IsNotNull(phrase1);
					VerifyItem(phrase1, "./item[@type='reference-label']", "en", "Abu Nawas 001");
					VerifyText(phrase1, new[] {"$$", "Uun", "kono'", "serita'", ",", "dau-dau", "(", "tu",")", "kisa", "Abu", "Nawas", "."},
						new HashSet<string>(new[] {".", ",", "$$", "(", ")"}), "qaa-x-kal");
					VerifyItem(phrase1, "./item[@type='gls']", "en", "There was, it is said, a story about Abu Nawas. JT added some more.");
					VerifyItem(phrase1, "./item[@type='lit']", "en", "There existed, it is said, long ago, this story about Abu Nawas");
					VerifyItem(phrase1, "./item[@type='note']", "en", "sentence adjunct: reported speech");
					VerifyItem(phrase1, "./item[@type='note'][2]", "en", "Example hacked by JohnT to exemplify more cases");

					var phrase2 = phrases.Elements("phrase").Skip(1).First();
					VerifyItem(phrase2, "./item[@type='reference-label']", "en", "Abu Nawas 002");
					VerifyText(phrase2, new[] { "Abu", "Nawas", "kerjo", "ta'", "rojo" },
						new HashSet<string>(), "qaa-x-kal");
					VerifyItem(phrase2, "./item[@type='gls']", "en", "Abu Nawas worked for the king.");

					var phrase3 = paragraphs.XPathSelectElement("./paragraph[2]/phrases/phrase");
					VerifyItem(phrase3, "./item[@type='reference-label']", "en", "Abu Nawas 003");
					VerifyText(phrase3, new[] { "John", "added", "this" },
						new HashSet<string>(), "qaa-x-kal");
					VerifyItem(phrase3, "./item[@type='gls']", "en", "and this");

					var text2 = outputElt.Elements("interlinear-text").Skip(1).First();
					VerifyItem(text2, "./item[@type='title-abbreviation']", "en", "Jt");

					var phrase4 = text2.XPathSelectElement("./paragraphs/paragraph/phrases/phrase");
					VerifyItem(phrase4, "./item[@type='reference-label']", "en", "Jt 001");
					VerifyText(phrase4, new[] { "A", "second", "text", "in", "two", "parts" },
						new HashSet<string>(), "qaa-x-kal");
					VerifyItem(phrase4, "./item[@type='gls']", "en", "its free translation");
					Assert.That(phrase4.XPathSelectElements("./item[@type='gls']").Count(), Is.EqualTo(1));

					var phrase5 = text2.XPathSelectElement("./paragraphs/paragraph[2]/phrases/phrase");
					VerifyText(phrase5, new[] { "second", "para" },
						new HashSet<string>(), "qaa-x-kal");

					// If we unexpectedly get a second text line AFTER some other known field without a ref line, start a new phrase anyway.
					var phrase6 = text2.XPathSelectElement("./paragraphs/paragraph[2]/phrases/phrase[2]");
					VerifyText(phrase6, new[] { "second", "para", "second", "sentence"},
						new HashSet<string>(), "qaa-x-kal");
				}
			}
		}

		// \x85 is ellipsis in cp1252, \u2026
		private string input2 =
			@"\_sh v3.0  943  S Texts
" +
"\\id Abu\x2026" + @"
\name Abu Nawas

\au Guna Bte Rintal
\com a funny story (folk tale?) about the relationship between Abu Nawas and a king
\comp Edith Mirafuentes (and revised by Kaili Bte Said)

\p
\ref Abu Nawas 001
\t John added this" + "\x017D";

		/// <summary>
		/// Test the application of encoding converters.
		/// </summary>
		[Test]
		public void EncodingConverters()
		{
			var encConv = new EncConverters();
			encConv.AddConversionMap("XXYTestConverter", "1252",
						ECInterfaces.ConvType.Legacy_to_from_Unicode, "cp", "", "",
						ECInterfaces.ProcessTypeFlags.CodePageConversion);
			var mappings = new List<InterlinearMapping>();
			mappings.Add(new InterlinearMapping()
							{
								Marker = "id",
								Destination = InterlinDestination.Abbreviation	,
								WritingSystem = "en",
								Converter = "XXYTestConverter"
							});
			mappings.Add(new InterlinearMapping()
			{
				Marker = "t",
				Destination = InterlinDestination.Baseline,
				WritingSystem = "qaa-x-kal",
				Converter = "XXYTestConverter"
			});
			var wsf = GetWsf();
			var input = new ByteReader("input2", Encoding.GetEncoding(1252).GetBytes(input2));
			var converter = new Sfm2FlexText();
			var output = converter.Convert(input, mappings, wsf);
			using (var outputStream = new MemoryStream(output))
			{
				using (var reader = new StreamReader(outputStream))
				{
					var outputElt = XElement.Load(reader);
					var textElt = outputElt.Element("interlinear-text");
					VerifyItem(textElt, "./item[@type='title-abbreviation']", "en", "Abu\x2026");
					var phrase1 = textElt.XPathSelectElement("./paragraphs/paragraph/phrases/phrase");
					VerifyText(phrase1, new[] { "John", "added", "this\x017D" },
						new HashSet<string>(), "qaa-x-kal");
					encConv.Remove("XXYTestConverter");
				}
			}
		}

		private string input3 =
	@"\_sh v3.0  943  S Texts
\id
\ab Abu
\name Abu Nawas
\com a funny story (folk tale?) about the relationship between Abu Nawas and a king
\comp Edith Mirafuentes (and revised by Kaili Bte Said)

\p
\ref Abu Nawas 001
\t John added this

\id
\ab MyT
\name MyText
\com Some coments
\com more comments
\p
\ref MyText 001
\t Some text

\name Another
\name Text
\com More comments
\name this is ignored
\p
\ref AT 001
\t third text

\id
\ab Yet
\name Yet another
\p
\ref Yet 001
\t fourth text
";

		/// <summary>
		/// Test reading multiple texts from a single file (and multiple adjcant lines of header)
		/// </summary>
		[Test]
		public void MultipleTexts()
		{
			var mappings = GetMappings();
			mappings.Add(new InterlinearMapping() { Marker = "id", Destination = InterlinDestination.Id, WritingSystem = "en" });
			mappings.Add(new InterlinearMapping() { Marker = "ab", Destination = InterlinDestination.Abbreviation, WritingSystem = "en" });
			var wsf = GetWsf();
			var input = new ByteReader("input3", Encoding.GetEncoding(1252).GetBytes(input3));
			var converter = new Sfm2FlexText();
			var output = converter.Convert(input, mappings, wsf);
			using (var outputStream = new MemoryStream(output))
			{
				using (var reader = new StreamReader(outputStream))
				{
					var outputElt = XElement.Load(reader);
					var textElt = outputElt.Element("interlinear-text");
					VerifyItem(textElt, "./item[@type='title-abbreviation']", "en", "Abu");
					var phrase1 = textElt.XPathSelectElement("./paragraphs/paragraph/phrases/phrase");
					VerifyText(phrase1, new[] { "John", "added", "this" },
						new HashSet<string>(), "qaa-x-kal");

					//\id
					//\ab MyT
					//\name MyText
					//\com Some coments
					//\com more comments
					//\p
					//\ref MyText 001
					//\t Some text
					textElt = outputElt.Elements("interlinear-text").Skip(1).First();
					VerifyItem(textElt, "./item[@type='title-abbreviation']", "en", "MyT");
					VerifyItem(textElt, "./item[@type='title']", "fr", "MyText");
					VerifyItem(textElt, "./item[@type='comment']", "en", "Some coments more comments");
					phrase1 = textElt.XPathSelectElement("./paragraphs/paragraph/phrases/phrase");
					VerifyText(phrase1, new[] { "Some", "text" }, new HashSet<string>(), "qaa-x-kal");

					// Verifies that:
					//	- \name can occur twice and be concatenated
					//	- \name can force the start of a new text
					//	- a subsequent \name not following some content is ignored
					//\name Another
					//\name Text
					//\com More comments
					//\name this is ignored
					//\p
					//\ref AT 001
					//\t third text
					textElt = outputElt.Elements("interlinear-text").Skip(2).First();
					VerifyItem(textElt, "./item[@type='title']", "fr", "Another Text");
					VerifyItem(textElt, "./item[@type='comment']", "en", "More comments");
					phrase1 = textElt.XPathSelectElement("./paragraphs/paragraph/phrases/phrase");
					VerifyText(phrase1, new[] { "third", "text" }, new HashSet<string>(), "qaa-x-kal");

					// Verifies that:
					//	- \id can force the start of a new text
					//  - \ab does not start yet another (when no intervening content)
					//\id
					//\ab Yet
					//\name Yet another
					//\p
					//\ref Yet 001
					//\t fourth text			textElt = outputElt.Elements("interlinear-text").Skip(2).First();
					textElt = outputElt.Elements("interlinear-text").Skip(3).First();
					VerifyItem(textElt, "./item[@type='title']", "fr", "Yet another");
					VerifyItem(textElt, "./item[@type='title-abbreviation']", "en", "Yet");
					phrase1 = textElt.XPathSelectElement("./paragraphs/paragraph/phrases/phrase");
					VerifyText(phrase1, new[] { "fourth", "text" }, new HashSet<string>(), "qaa-x-kal");
				}
			}
		}

		[Test]
		public void MultipleFileSplitAndJoin()
		{
			// I've chosen not to test what it does with empty strings because we really don't care. Path names won't be empty.
			string[][] inputs =
			{
				new string[0], // empty
				new [] {"abc"}, // simple
				new []{"abc", "def", "xyz"}, // simple list
				new []{"a b", "c  d", "4%f ", " ab"}, // spaces in various places
				new []{"ab", "a b", "cd", "ef", "q d", "t,f", "34"}, // mix of things with and without problem characters
				new []{",file", "name,", "a,b", "c;d;e", "~`@#$%^&() /-_=\\+{}[]'", @"C:\Users\John\AppData\Roaming\Favorite Files\\Myfile.sfm"} // edge cases and a realistic path
			};
			foreach (var list in inputs)
			{
				var combined = InterlinearSfmImportWizard.JoinPaths(list);
				var split = InterlinearSfmImportWizard.SplitPaths(combined);
				Assert.That(split.Length, Is.EqualTo(list.Length));
				for (int i = 0; i < split.Length; i++)
					Assert.That(split[i], Is.EqualTo(list[i]));
			}
			var output = InterlinearSfmImportWizard.SplitPaths("ab, \"unterminated");
			Assert.That(output.Length, Is.EqualTo(2));
			Assert.That(output[0], Is.EqualTo("ab"));
			Assert.That(output[1], Is.EqualTo("unterminated"));
		}

		private WritingSystemManager GetWsf()
		{
			var wsf = new WritingSystemManager();
			CoreWritingSystemDefinition wsObj;
			wsf.GetOrSet("qaa-x-kal", out wsObj);
			EnsureQuoteAndHyphenWordForming(wsObj);
			return wsf;
		}

		private List<InterlinearMapping> GetMappings()
		{
			var mappings = new List<InterlinearMapping>();
			mappings.Add(new InterlinearMapping() { Marker = "name", Destination = InterlinDestination.Title, WritingSystem = "fr" });
			mappings.Add(new InterlinearMapping() { Marker = "au", Destination = InterlinDestination.Source, WritingSystem = "en" });
			mappings.Add(new InterlinearMapping() { Marker = "com", Destination = InterlinDestination.Comment, WritingSystem = "en" });
			mappings.Add(new InterlinearMapping() { Marker = "p", Destination = InterlinDestination.ParagraphBreak });
			mappings.Add(new InterlinearMapping() { Marker = "ref", Destination = InterlinDestination.Reference, WritingSystem = "en" });
			mappings.Add(new InterlinearMapping() { Marker = "t", Destination = InterlinDestination.Baseline, WritingSystem = "qaa-x-kal" });
			mappings.Add(new InterlinearMapping() { Marker = "f", Destination = InterlinDestination.FreeTranslation, WritingSystem = "en" });
			mappings.Add(new InterlinearMapping() { Marker = "lt", Destination = InterlinDestination.LiteralTranslation, WritingSystem = "en" });
			mappings.Add(new InterlinearMapping() { Marker = "nt", Destination = InterlinDestination.Note, WritingSystem = "en" });
			return mappings;
		}

		private void EnsureQuoteAndHyphenWordForming(CoreWritingSystemDefinition wsObj)
		{
			ValidCharacters validChars = ValidCharacters.Load(wsObj);
			var fChangedSomething = false;
			if (!validChars.IsWordForming('-'))
			{
				validChars.AddCharacter("-");
				validChars.MoveBetweenWordFormingAndOther(new List<string>(new[] { "-" }), true);
				fChangedSomething = true;
			}
			if (!validChars.IsWordForming('\''))
			{
				validChars.AddCharacter("'");
				validChars.MoveBetweenWordFormingAndOther(new List<string>(new[] { "'" }), true);
				fChangedSomething = true;
			}
			if (!fChangedSomething)
				return;
			validChars.SaveTo(wsObj);
		}

		private void VerifyText(XElement phrase, string[] items, HashSet<string> punctItems, string lang)
		{
			var words = phrase.Element("words").Elements("word");
			Assert.That(words.Count(), Is.EqualTo(items.Length));
			int i = 0;
			foreach (var word in words)
			{
				var text = items[i];
				i++;
				var itemType = punctItems.Contains(text) ? "punct" : "txt";
				VerifyItem(word, "./item[@type='" + itemType + "']", lang, text);
			}
		}

		private void VerifyItem(XElement textElt, string xpath, string expectedLang, string expectedValue)
		{
			var item = textElt.XPathSelectElements(xpath).FirstOrDefault();
			Assert.That(item, Is.Not.Null);
			Assert.That(item.Attribute("lang").Value, Is.EqualTo(expectedLang));
			Assert.That(item.Value, Is.EqualTo(expectedValue));
		}
	}
}
