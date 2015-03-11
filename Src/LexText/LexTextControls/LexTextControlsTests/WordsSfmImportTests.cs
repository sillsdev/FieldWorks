using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.Test.TestUtils;
using Sfm2Xml;

namespace LexTextControlsTests
{
	/// <summary>
	/// These are largely adapted from the tests in SIL.FieldWorks.IText.InterlinSfmImportTests
	/// </summary>
	[TestFixture]
	public class WordsSfmImportTests : BaseTest
	{
		[SuppressMessage("Gendarme.Rules.Portability", "NewLineLiteralRule",
			Justification="New lines in input strings are different depending on platform")]
		public WordsSfmImportTests()
		{
		}

		/// <summary>
		/// This tests out most aspects of the conversion.
		/// </summary>
		[Test]
		public void BasicConversion()
		{
			const string input1 =
@"\lx glossedonce
\ge onlygloss
\del 0
\wc Pyle:PYLEE-1007
\cdt 2012-06-04T08:06:14Z

\lx glossedtwice
\ge firstgloss
\del 0
\wc Pyle:PYLEE-1007
\cdt 2012-06-05T08:06:14Z
\ge secondgloss
\del 1
\wc Pyle:PYLEE-1007
\cdt 2012-06-05T08:06:29Z
\ddt 2012-06-07T08:49:08Z

\lx support a phrase
\ge phrase gloss
\del 0
\wc Pyle:PYLEE-1007
\cdt 2012-06-05T08:23:54Z
";
			var mappings = new List<InterlinearMapping>();
			mappings.Add(new InterlinearMapping { Marker = "lx", Destination = InterlinDestination.Wordform, WritingSystem = "qaa-x-kal" });
			mappings.Add(new InterlinearMapping { Marker = "ge", Destination = InterlinDestination.WordGloss, WritingSystem = "en" });
			var wsf = GetWsf();
			var input = new ByteReader("input1", Encoding.UTF8.GetBytes(input1));
			var converter = new Sfm2FlexTextWordsFrag();
			var output = converter.Convert(input, mappings, wsf);
			using (var outputStream = new MemoryStream(output))
			{
				using (var reader = new StreamReader(outputStream))
				{
					var outputElt = XElement.Load(reader);
					Assert.That(outputElt.Name.LocalName, Is.EqualTo("document"));
					var words = outputElt.Elements("word").ToList();
					Assert.That(words, Has.Count.EqualTo(3));

					{
						var word1 = words[0];
						var txtItems = word1.XPathSelectElements("item[@type='txt']").ToList();
						var glsItems = word1.XPathSelectElements("item[@type='gls']").ToList();
						Assert.That(txtItems, Has.Count.EqualTo(1));
						Assert.That(glsItems, Has.Count.EqualTo(1));
						VerifyItem(word1, "./item[@type='txt']", "qaa-x-kal", "glossedonce");
						VerifyItem(word1, "./item[@type='gls']", "en", "onlygloss");
					}

					{
						var word2 = words[1];
						var txtItems = word2.XPathSelectElements("item[@type='txt']").ToList();
						var glsItems = word2.XPathSelectElements("item[@type='gls']").ToList();
						Assert.That(txtItems, Has.Count.EqualTo(1));
						Assert.That(glsItems, Has.Count.EqualTo(2));
						VerifyItem(word2, "./item[@type='txt']", "qaa-x-kal", "glossedtwice");
						VerifyItem(word2, "./item[@type='gls'][1]", "en", "firstgloss");
						VerifyItem(word2, "./item[@type='gls'][2]", "en", "secondgloss");
					}

					{
						var word3 = words[2];
						var txtItems = word3.XPathSelectElements("item[@type='txt']").ToList();
						var glsItems = word3.XPathSelectElements("item[@type='gls']").ToList();
						Assert.That(txtItems, Has.Count.EqualTo(1));
						Assert.That(glsItems, Has.Count.EqualTo(1));
						VerifyItem(word3, "./item[@type='txt']", "qaa-x-kal", "support a phrase");
						VerifyItem(word3, "./item[@type='gls']", "en", "phrase gloss");
					}
				}
			}
		}

		/// <summary>
		/// Hypothetically, an sfm file could just have a list of words without glosses.
		/// create separate word elements for adjacent lx items
		/// </summary>
		[Test]
		public void WordsWithoutGlosses()
		{
			const string input2 =
@"\lx wordone
\lx wordtwo
\lx wordthree
";
			var mappings = new List<InterlinearMapping>();
			mappings.Add(new InterlinearMapping { Marker = "lx", Destination = InterlinDestination.Wordform, WritingSystem = "qaa-x-kal" });
			mappings.Add(new InterlinearMapping { Marker = "ge", Destination = InterlinDestination.WordGloss, WritingSystem = "en" });
			var wsf = GetWsf();
			var input = new ByteReader("input2", Encoding.UTF8.GetBytes(input2));
			var converter = new Sfm2FlexTextWordsFrag();
			var output = converter.Convert(input, mappings, wsf);
			using (var outputStream = new MemoryStream(output))
			{
				using (var reader = new StreamReader(outputStream))
				{
					var outputElt = XElement.Load(reader);
					Assert.That(outputElt.Name.LocalName, Is.EqualTo("document"));
					var words = outputElt.Elements("word").ToList();
					Assert.That(words, Has.Count.EqualTo(3));
					{
						var word1 = words[0];
						var txtItems = word1.XPathSelectElements("item[@type='txt']").ToList();
						Assert.That(txtItems, Has.Count.EqualTo(1));
						VerifyItem(word1, "./item[@type='txt']", "qaa-x-kal", "wordone");
					}
					{
						var word2 = words[1];
						var txtItems = word2.XPathSelectElements("item[@type='txt']").ToList();
						Assert.That(txtItems, Has.Count.EqualTo(1));
						VerifyItem(word2, "./item[@type='txt']", "qaa-x-kal", "wordtwo");
					}
					{
						var word3 = words[2];
						var txtItems = word3.XPathSelectElements("item[@type='txt']").ToList();
						Assert.That(txtItems, Has.Count.EqualTo(1));
						VerifyItem(word3, "./item[@type='txt']", "qaa-x-kal", "wordthree");
					}

				}
			}
		}

		[Test]
		[Ignore("Should we support gloss elements in sfm before their lx elements?")]
		public void GlossesBeforeWords()
		{
		}

		/// <summary>
		/// NOTE: Copied from SIL.FieldWorks.IText.InterlinSfmImportTests
		/// </summary>
		/// <returns></returns>
		private WritingSystemManager GetWsf()
		{
			var wsf = new WritingSystemManager();
			CoreWritingSystemDefinition wsObj;
			wsf.GetOrSet("qaa-x-kal", out wsObj);
			EnsureQuoteAndHyphenWordForming(wsObj);
			return wsf;
		}

		/// <summary>
		/// NOTE: Copied from SIL.FieldWorks.IText.InterlinSfmImportTests
		/// </summary>
		/// <param name="wsObj"></param>
		private void EnsureQuoteAndHyphenWordForming(CoreWritingSystemDefinition wsObj)
		{
			ValidCharacters validChars = ValidCharacters.Load(wsObj, null, FwDirectoryFinder.LegacyWordformingCharOverridesFile);
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

		/// <summary>
		/// VerifyItem(word, "./item[@type='" + itemType + "']", lang, text);
		/// </summary>
		/// <param name="textElt"></param>
		/// <param name="xpath"></param>
		/// <param name="expectedLang"></param>
		/// <param name="expectedValue"></param>
		private void VerifyItem(XElement textElt, string xpath, string expectedLang, string expectedValue)
		{
			var item = textElt.XPathSelectElements(xpath).FirstOrDefault();
			Assert.That(item, Is.Not.Null);
			Assert.That(item.Attribute("lang").Value, Is.EqualTo(expectedLang));
			Assert.That(item.Value, Is.EqualTo(expectedValue));
		}
	}
}
