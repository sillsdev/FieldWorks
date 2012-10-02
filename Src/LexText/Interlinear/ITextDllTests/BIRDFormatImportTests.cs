using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Schema;
using NUnit.Framework;
using Palaso.TestUtilities;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.IText.FlexInterlinModel;

namespace SIL.FieldWorks.IText
{
	class BIRDFormatImportTests : MemoryOnlyBackendProviderBasicTestBase
	{
		public override void TestTearDown()
		{
			base.TestTearDown();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => Cache.LanguageProject.TextsOC.Clear());
		}

		[Test]
		public void ValidateBirdDocSchema()
		{
			string title = "atrocious";
			string abbr = "atroc";
			//an interliner text example xml string
			string xml = "<document><interlinear-text>" +
						 "<item type=\"title\" lang=\"en\">" + title + "</item>" +
						 "<item type=\"title-abbreviation\" lang=\"en\">" + abbr + "</item>" +
						 "<paragraphs><paragraph><phrases><phrase>" +
						 "<item type=\"reference-number\" lang=\"en\">1 Musical</item>" +
						 "<item type=\"note\" lang=\"pt\">origem: mary poppins</item>" +
						 "<words><word><item type=\"txt\" lang=\"en\">supercalifragilisticexpialidocious</item>" +
						 "<morphemes><morph><item type=\"txt\" lang=\"en\">supercali-</item><item type=\"gls\" lang=\"pt\">superlative</item></morph></morphemes>" +
						 "<item type=\"gls\" lang=\"pt\">absurdo</item></word>" +
						 "</words></phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			XmlReader xmlReader = GetXmlReaderForTest(xml);

			Assert.DoesNotThrow( ()=> ReadXmlForValidation(xmlReader));
		}

		private static void ReadXmlForValidation(XmlReader xmlReader)
		{
			try
			{
				while (xmlReader.Read())
				{
				}
			}
			catch (Exception e)
			{
				Debug.Print(e.Message);
				throw;
			}

		}

		#region ScrElements

		private XmlReader GetXmlReaderForTest(string xml)
		{
			var settings = new XmlReaderSettings {ValidationType = ValidationType.Schema};

			string p = Path.Combine(DirectoryFinder.FlexFolder, @"Export Templates\Interlinear");
			string file = Path.Combine(p, "FlexInterlinear.xsd");
			settings.Schemas.Add("", file);
			//settings.ValidationEventHandler += ValidationCallBack;
			return XmlReader.Create(new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())), settings);
		}

		[Test]
		public void ValidateExportSourceAttribute()
		{
			const string xml = "<document exportSource='paratext' exportTarget='flex'><interlinear-text/></document>";

			XmlReader xmlReader = GetXmlReaderForTest(xml);
			Assert.DoesNotThrow(() => ReadXmlForValidation(xmlReader));
		}

		[Test]
		public void ValidateLanguageEncodingAttribute()
		{
			const string xml = "<document><interlinear-text><paragraphs/>" +
							"<languages><language lang='en' encoding='ISO639-1'/></languages>" +
						 "</interlinear-text></document>";

			XmlReader xmlReader = GetXmlReaderForTest(xml);
			Assert.DoesNotThrow(() => ReadXmlForValidation(xmlReader));
		}

		[Test]
		public void ValidateLanguageEncodingAttributeMultiple()
		{
			const string xml = "<document>" +
				"<interlinear-text><paragraphs/>" +
					"<languages><language lang='en' encoding='ISO639-1'/><language lang='jit' encoding='ISO639-1' vernacular='true'/></languages>" +
				"</interlinear-text>" +
				"<interlinear-text><paragraphs/>" +
					"<languages><language lang='en' encoding='ISO639-1'/><language lang='jit' encoding='ISO639-1' vernacular='true'/></languages>" +
				"</interlinear-text>" +
				"</document>";

			XmlReader xmlReader = GetXmlReaderForTest(xml);
			Assert.DoesNotThrow(() => ReadXmlForValidation(xmlReader));
		}

		[Test]
		public void ValidateScrBookAttribute()
		{
			const string xml = "<document>" +
							"<interlinear-text scrBook='MAT'/>" +
						 "</document>";

			XmlReader xmlReader = GetXmlReaderForTest(xml);
			Assert.DoesNotThrow(() => ReadXmlForValidation(xmlReader));
		}

		[Test]
		[Ignore("EricP: Add valid ScrBook values to the schema? (e.g. GEN, MAT)...or reference an external schema for those?")]
		public void InvalidScrBookAttributeValue()
		{
			const string xml = "<document>" +
							"<interlinear-text scrBook='invalid'/>" +
						 "</document>";

			XmlReader xmlReader = GetXmlReaderForTest(xml);
			var ex = Assert.Throws<XmlSchemaValidationException>(() => ReadXmlForValidation(xmlReader));
			Assert.That(ex.Message, Is.EqualTo("The 'scrSectionType' attribute is invalid - The value 'invalid' is invalid according to its datatype 'scrSectionTypes' - The Enumeration constraint failed."));
		}

		[Test]
		public void ValidateScrSectionTypeAttributes()
		{
			const string xml = "<document>" +
							"<interlinear-text scrSectionType='heading'/>" +
							"<interlinear-text scrSectionType='title'/>" +
							"<interlinear-text scrSectionType='verseText'/>" +
						 "</document>";

			XmlReader xmlReader = GetXmlReaderForTest(xml);
			Assert.DoesNotThrow(() => ReadXmlForValidation(xmlReader));
		}

		[Test]
		public void InvalidScrSectionTypeAttributeValue()
		{
			const string xml = "<document>" +
							"<interlinear-text scrSectionType='invalid'/>" +
						 "</document>";

			XmlReader xmlReader = GetXmlReaderForTest(xml);
			var ex = Assert.Throws<XmlSchemaValidationException>(() => ReadXmlForValidation(xmlReader));
			Assert.That(ex.Message, Is.EqualTo("The 'scrSectionType' attribute is invalid - The value 'invalid' is invalid according to its datatype 'scrSectionTypes' - The Enumeration constraint failed."));
		}

		[Test]
		public void ValidateScrMilestone()
		{
			//an interliner text example xml string
			const string xml = "<document><interlinear-text><paragraphs><paragraph><phrases><phrase>" +
							"<words><scrMilestone chapter='1' verse='1'/></words>" +
						 "</phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			XmlReader xmlReader = GetXmlReaderForTest(xml);
			Assert.DoesNotThrow(() => ReadXmlForValidation(xmlReader));
		}

		[Test]
		public void InvalidScrMilestonesMissingChapter()
		{
			const string xml = "<document><interlinear-text><paragraphs><paragraph><phrases><phrase>" +
							"<words><scrMilestone/></words>" +
						 "</phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			XmlReader xmlReader = GetXmlReaderForTest(xml);
			var ex = Assert.Throws<XmlSchemaValidationException>(() => ReadXmlForValidation(xmlReader));
			Assert.That(ex.Message, Is.EqualTo("The required attribute 'chapter' is missing."));
		}

		[Test]
		public void InvalidScrMilestonesMissingVerse()
		{
			const string xml = "<document><interlinear-text><paragraphs><paragraph><phrases><phrase>" +
							"<words><scrMilestone chapter='1'/></words>" +
						 "</phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			XmlReader xmlReader = GetXmlReaderForTest(xml);
			var ex = Assert.Throws<XmlSchemaValidationException>(() => ReadXmlForValidation(xmlReader));
			Assert.That(ex.Message, Is.EqualTo("The required attribute 'verse' is missing."));
		}

		[Test]
		public void InvalidScrMilestonePhraseSequence()
		{
			const string xml = "<document><interlinear-text><paragraphs><paragraph><phrases><phrase>" +
							"<words><word/><scrMilestone chapter='1' verse='1'/></words>" +
						 "</phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			XmlReader xmlReader = GetXmlReaderForTest(xml);
			var ex = Assert.Throws<XmlSchemaValidationException>(() => ReadXmlForValidation(xmlReader));
			Assert.That(ex.Message, Is.EqualTo("The element 'words' has invalid child element 'scrMilestone'. List of possible elements expected: 'word'."));
		}

		[Test]
		public void InvalidScrMilestoneChapterType()
		{
			const string xml = "<document><interlinear-text><paragraphs><paragraph><phrases><phrase>" +
							"<words><scrMilestone chapter='one' verse='1'/></words>" +
						 "</phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			XmlReader xmlReader = GetXmlReaderForTest(xml);
			var ex = Assert.Throws<XmlSchemaValidationException>(() => ReadXmlForValidation(xmlReader));
			Assert.That(ex.Message, Is.EqualTo("The 'chapter' attribute is invalid - The value 'one' is invalid according to its datatype 'http://www.w3.org/2001/XMLSchema:integer' - The string 'one' is not a valid Integer value."));
		}

		[Test]
		public void InvalidScrMilestoneVerseType()
		{
			const string xml = "<document><interlinear-text><paragraphs><paragraph><phrases><phrase>" +
							"<words><scrMilestone chapter='1' verse='one'/></words>" +
						 "</phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			XmlReader xmlReader = GetXmlReaderForTest(xml);
			var ex = Assert.Throws<XmlSchemaValidationException>(() => ReadXmlForValidation(xmlReader));
			Assert.That(ex.Message, Is.EqualTo("The 'verse' attribute is invalid - The value 'one' is invalid according to its datatype 'http://www.w3.org/2001/XMLSchema:integer' - The string 'one' is not a valid Integer value."));
		}

		[Test]
		public void ValidatePhraseWord()
		{
			//an interliner text example xml string
			const string xml = "<document><interlinear-text><paragraphs><paragraph><phrases><phrase><words>" +
							"<word type='phrase'/>" +
						 "</words></phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			XmlReader xmlReader = GetXmlReaderForTest(xml);
			Assert.DoesNotThrow(() => ReadXmlForValidation(xmlReader));
		}

		[Test]
		public void ValidateMorphemeAnalysisStatus()
		{
			//an interliner text example xml string
			const string xml = "<document><interlinear-text><paragraphs><paragraph><phrases><phrase><words>" +
							"<word><morphemes analysisStatus='guess'/></word>" +
							"<word><morphemes analysisStatus='humanApproved'/></word>" +
						 "</words></phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			XmlReader xmlReader = GetXmlReaderForTest(xml);
			Assert.DoesNotThrow(() => ReadXmlForValidation(xmlReader));
		}

		[Test]
		public void InvalidMorphemeAnalysisStatus()
		{
			//an interliner text example xml string
			const string xml = "<document><interlinear-text><paragraphs><paragraph><phrases><phrase><words>" +
							"<word><morphemes analysisStatus='invalid'/></word>" +
						 "</words></phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			XmlReader xmlReader = GetXmlReaderForTest(xml);
			var ex = Assert.Throws<XmlSchemaValidationException>(() => ReadXmlForValidation(xmlReader));
			Assert.That(ex.Message, Is.EqualTo("The 'analysisStatus' attribute is invalid - The value 'invalid' is invalid according to its datatype 'analysisStatusTypes' - The Enumeration constraint failed."));
		}

		[Test]
		public void ValidateItemGlossAnalysisStatus()
		{
			//an interliner text example xml string
			const string xml = "<document><interlinear-text><paragraphs><paragraph><phrases><phrase><words>" +
							"<word><item type='gls' lang='en' analysisStatus='humanApproved'/></word>" +
							"<word><item type='gls' lang='en' analysisStatus='guessByStatisticalAnalysis'/></word>" +
							"<word><item type='gls' lang='en' analysisStatus='guessByHumanApproved'/></word>" +
						 "</words></phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			XmlReader xmlReader = GetXmlReaderForTest(xml);
			Assert.DoesNotThrow(() => ReadXmlForValidation(xmlReader));
		}

		bool DummyCheckAndAddLanguagesInternal(FdoCache cache, Interlineartext interlinText, ILgWritingSystemFactory wsFactory, IThreadedProgress progress)
		{
			return true;
		}

		[Test]
		public void ValidateFlexTextFile()
		{
			string path = Path.Combine(DirectoryFinder.FwSourceDirectory, @"LexText\Interlinear\ITextDllTests\FlexTextImport");
			string file = Path.Combine(path, "FlexTextExportOutput.xml");
			XmlDocument doc = new XmlDocument();
			doc.Load(file);

			XmlReader xmlReader = GetXmlReaderForTest(doc.OuterXml);
			Assert.DoesNotThrow(() => ReadXmlForValidation(xmlReader));
		}

		[Test]
		public void ImportParatextExportBasic()
		{
			string path = Path.Combine(DirectoryFinder.FwSourceDirectory, @"LexText\Interlinear\ITextDllTests\FlexTextImport");
			string file = Path.Combine(path, "FlexTextExportOutput.xml");
			var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);

			LinguaLinksImport li = new LinguaLinksImport(Cache, null, null);
			FDO.IText text = null;
			var options = new LinguaLinksImport.ImportInterlinearOptions{Progress = new DummyProgressDlg(),
				BirdData = fileStream, AllottedProgress = 0,
				CheckAndAddLanguages = DummyCheckAndAddLanguagesInternal };

			bool result = li.ImportInterlinear(options, ref text);
			Assert.True(result, "ImportInterlinear was not successful.");
		}

		#endregion ScrElements

		[Test]
		public void OneOfEachElementTypeTest()
		{
			string title = "atrocious";
			string abbr = "atroc";
			//an interliner text example xml string
			string xml = "<document><interlinear-text>" +
			"<item type=\"title\" lang=\"en\">" + title + "</item>" +
			"<item type=\"title-abbreviation\" lang=\"en\">" + abbr + "</item>" +
			"<paragraphs><paragraph><phrases><phrase>" +
			"<item type=\"reference-number\" lang=\"en\">1 Musical</item>" +
			"<item type=\"note\" lang=\"pt\">origem: mary poppins</item>" +
			"<words><word><item type=\"txt\" lang=\"en\">supercalifragilisticexpialidocious</item>" +
			"<item type=\"gls\" lang=\"pt\">absurdo</item></word>" +
			"</words></phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			LinguaLinksImport li = new LinguaLinksImport(Cache, null, null);
			FDO.IText text = null;
			li.ImportInterlinear(new DummyProgressDlg(), new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())), 0, ref text);
			var firstEntry = Cache.LanguageProject.TextsOC.GetEnumerator();
			firstEntry.MoveNext();
			var imported = firstEntry.Current;
			//The title imported
			Assert.True(imported.Name.get_String(Cache.WritingSystemFactory.get_Engine("en").Handle).Text.Equals(title));
			//The title abbreviation imported
			Assert.True(imported.Abbreviation.get_String(Cache.WritingSystemFactory.get_Engine("en").Handle).Text.Equals(abbr));
		}

		[Test]
		public void TestSpacesAroundPunct()
		{
			string xml = "<document><interlinear-text>" +
			"<item type=\"title\" lang=\"en\">wordspace</item>" +
			"<item type=\"title-abbreviation\" lang=\"en\">ws</item>" +
			"<paragraphs><paragraph><phrases><phrase>" +
			"<item type=\"reference-number\" lang=\"en\">1 Musical</item>" +
			"<item type=\"note\" lang=\"pt\">origem: mary poppins</item>" +
			"<words><word><item type=\"txt\" lang=\"en\">a</item></word>" +
			"<word><item type=\"punct\" lang=\"en\">,</item></word>" +
			"<word><item type=\"txt\" lang=\"en\">s</item></word>" +
			"<word><item type=\"punct\" lang=\"en\">.</item></word>" +
			"<word><item type=\"txt\" lang=\"en\">s</item></word>" +
			"<word><item type=\"punct\" lang=\"en\">.&quot;</item></word>" +
			"<word><item type=\"txt\" lang=\"en\">s</item></word>" +
			"<word><item type=\"punct\" lang=\"en\">&quot;:</item></word>" +
			"<word><item type=\"txt\" lang=\"en\">s</item></word>" +
			"<word><item type=\"punct\" lang=\"en\">&quot;</item></word>" +
			"<word><item type=\"txt\" lang=\"en\">s</item></word>" +
			"</words></phrase></phrases></paragraph></paragraphs></interlinear-text></document>";
			// ---------------------
			// 012345678901234567890
			// a, s. s." s": s "s
			// ---------------------

			LinguaLinksImport li = new LinguaLinksImport(Cache, null, null);
			FDO.IText text = null;
			li.ImportInterlinear(new DummyProgressDlg(), new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())), 0, ref text);
			var firstEntry = Cache.LanguageProject.TextsOC.GetEnumerator();
			firstEntry.MoveNext();
			var imported = firstEntry.Current;
			var para = imported.ContentsOA[0];
			var spaceArray = new char[] { ' ' };
			var spaceOne = para.Contents.Text.Substring(2, 1); //should be: " "
			var spaceTwo = para.Contents.Text.Substring(5, 1); //should be: " "
			var spaceThree = para.Contents.Text.Substring(9, 1);
			var spaceFour = para.Contents.Text.Substring(13, 1);
			var spaceFive = para.Contents.Text.Substring(15, 1);
			//test to make sure no space was inserted before the comma, this is probably captured by the other assert
			Assert.AreEqual(6, para.Contents.Text.Split(spaceArray).Length); //capture correct number of spaces, and no double spaces
			//test to make sure spaces were inserted in each expected place
			CollectionAssert.AreEqual(new [] {spaceOne, spaceTwo, spaceThree, spaceFour, spaceFive},
									  new [] {" ", " ", " ", " ", " "});

		}

		[Test]
		public void TestSpacesAroundSpanishPunct()
		{
			string xml = "<document><interlinear-text>" +
			"<item type=\"title\" lang=\"en\">wordspace</item>" +
			"<item type=\"title-abbreviation\" lang=\"en\">ws</item>" +
			"<paragraphs><paragraph><phrases><phrase>" +
			"<item type=\"reference-number\" lang=\"en\">1 Musical</item>" +
			"<item type=\"note\" lang=\"pt\">origem: mary poppins</item>" +
			"<words>" +
			"<word><item type=\"txt\" lang=\"en\">s</item></word>" +
			"<word><item type=\"punct\" lang=\"en\">.&#xA1;</item></word>" + // spanish begin exclamation
			"<word><item type=\"txt\" lang=\"en\">s</item></word>" +
			"<word><item type=\"punct\" lang=\"en\">!&#xBF;</item></word>" + // spanish begin question
			"<word><item type=\"txt\" lang=\"en\">s</item></word>" +
			"<word><item type=\"punct\" lang=\"en\">?</item></word>" +
			"<word><item type=\"txt\" lang=\"en\">s</item></word>" +
			"<word><item type=\"punct\" lang=\"en\">. &#xBF;</item></word>" +
			"<word><item type=\"txt\" lang=\"en\">s</item></word>" +
			"<word><item type=\"punct\" lang=\"en\">? &quot;&#xBF;</item></word>" +
			"<word><item type=\"txt\" lang=\"en\">s</item></word>" +
			"<word><item type=\"punct\" lang=\"en\">&#xBF;</item></word>" +
			"<word><item type=\"txt\" lang=\"en\">s</item></word>" +
			"</words></phrase></phrases></paragraph></paragraphs></interlinear-text></document>";
			// --------------------------
			// 01234567890123456789012345
			// s. es! qs? s. qs? "qs qs (e=spanish beg. exclamation, q=spanish beg. question)
			// --------------------------

			LinguaLinksImport li = new LinguaLinksImport(Cache, null, null);
			FDO.IText text = null;
			li.ImportInterlinear(new DummyProgressDlg(),
				new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())), 0, ref text);
			var firstEntry = Cache.LanguageProject.TextsOC.GetEnumerator();
			firstEntry.MoveNext();
			var imported = firstEntry.Current; // why is this null?!
			var para = imported.ContentsOA[0];
			var spaceArray = new char[] { ' ' };
			var spaceOne = para.Contents.Text.Substring(2, 1); //should be: " "
			var spaceTwo = para.Contents.Text.Substring(6, 1); //should be: " "
			var spaceThree = para.Contents.Text.Substring(10, 1);
			var spaceFour = para.Contents.Text.Substring(13, 1);
			var spaceFive = para.Contents.Text.Substring(17, 1);
			var spaceSix = para.Contents.Text.Substring(21, 1);
			//test to make sure no space was inserted before the comma, this is probably captured by the other assert
			Assert.AreEqual(7, para.Contents.Text.Split(spaceArray).Length); //capture correct number of spaces, and no double spaces
			//test to make sure spaces were inserted in each expected place
			CollectionAssert.AreEqual(new[] { spaceOne, spaceTwo, spaceThree, spaceFour, spaceFive, spaceSix },
									  new[] { " ", " ", " ", " ", " ", " " });

		}

		[Test]
		public void TestSpacesBetweenWords()
		{
			string xml = "<document><interlinear-text>" +
			"<item type=\"title\" lang=\"en\">wordspace</item>" +
			"<item type=\"title-abbreviation\" lang=\"en\">ws</item>" +
			"<paragraphs><paragraph><phrases><phrase>" +
			"<item type=\"reference-number\" lang=\"en\">1 Musical</item>" +
			"<item type=\"note\" lang=\"pt\">origem: mary poppins</item>" +
			"<words><word><item type=\"txt\" lang=\"en\">a</item></word>" +
			"<word><item type=\"txt\" lang=\"en\">space</item></word>" +
			"<word><item type=\"txt\" lang=\"en\">space</item></word>" +
			"</words></phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			LinguaLinksImport li = new LinguaLinksImport(Cache, null, null);
			FDO.IText text = null;
			li.ImportInterlinear(new DummyProgressDlg(), new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())), 0, ref text);
			var firstEntry = Cache.LanguageProject.TextsOC.GetEnumerator();
			firstEntry.MoveNext();
			var imported = firstEntry.Current;
			var para = imported.ContentsOA[0];
			var spaceOne = para.Contents.Text.Substring(1, 1); //should be: " "
			var wordAfter = para.Contents.Text.Substring(2, 5); //should be: "space"
			var spaceTwo = para.Contents.Text.Substring(7, 1); //should be: " "
			//test to make sure no space was inserted before the first word.
			Assert.IsFalse(" ".Equals(para.Contents.GetSubstring(0, 1)));
			//test to make sure spaces were inserted between "a" and "space", and between "space" and "space"
			//any extra spaces would result in the "space" word looking like " spac"
			Assert.IsTrue(spaceOne.Equals(spaceTwo));
			Assert.IsTrue(wordAfter.Equals("space"));
		}

		[Test]
		public void TestProvidedTextUsedIfPresent()
		{
			string xml = "<document><interlinear-text>" +
			"<item type=\"title\" lang=\"en\">wordspace</item>" +
			"<item type=\"title-abbreviation\" lang=\"en\">ws</item>" +
			"<paragraphs><paragraph><phrases><phrase>" +
			"<item type=\"txt\" lang=\"en\">Text not built from words.</item>" +
			"<item type=\"note\" lang=\"pt\">origem: mary poppins</item>" +
			"<words><word><item type=\"txt\" lang=\"en\">a</item></word>" +
			"<word><item type=\"txt\" lang=\"en\">space</item></word>" +
			"<word><item type=\"txt\" lang=\"en\">space</item></word>" +
			"</words></phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			LinguaLinksImport li = new LinguaLinksImport(Cache, null, null);
			FDO.IText text = null;
			li.ImportInterlinear(new DummyProgressDlg(), new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())), 0, ref text);
			var firstEntry = Cache.LanguageProject.TextsOC.GetEnumerator();
			firstEntry.MoveNext();
			var imported = firstEntry.Current;
			var para = imported.ContentsOA[0];
			Assert.IsTrue(para.Contents.Text.Equals("Text not built from words."));
		}

		[Test]
		public void TestEmptyParagraph()
		{
			string xml = "<document><interlinear-text>" +
			"<item type=\"title\" lang=\"en\">wordspace</item>" +
			"<item type=\"title-abbreviation\" lang=\"en\">ws</item>" +
			"<paragraphs><paragraph/></paragraphs></interlinear-text></document>";

			LinguaLinksImport li = new LinguaLinksImport(Cache, null, null);
			FDO.IText text = null;
			li.ImportInterlinear(new DummyProgressDlg(), new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())), 0, ref text);
			var firstEntry = Cache.LanguageProject.TextsOC.GetEnumerator();
			firstEntry.MoveNext();
			var imported = firstEntry.Current;
			Assert.True(imported.ContentsOA.ParagraphsOS.Count > 0, "Empty paragraph was not imported as text content.");
			var para = imported.ContentsOA[0];
			Assert.NotNull(para, "The imported paragraph is null?");
		}

		[Test]
		public void TestImportFullELANData()
		{
			string xml = "<document><interlinear-text guid=\"AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA\">" +
			"<item type=\"title\" lang=\"en\">wordspace</item>" +
			"<item type=\"title-abbreviation\" lang=\"en\">ws</item>" +
			"<paragraphs><paragraph><phrases><phrase media-file=\"FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF\" guid=\"BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB\" begin-time-offset=\"1\" end-time-offset=\"2\">" +
			"<item type=\"text\">This is a test with text.</item><word>This</word></phrase></phrases></paragraph></paragraphs>" +
			"<languages><language lang=\"en\" font=\"latin\" vernacular=\"false\"/></languages>" +
			"<media-files offset-type=\"milliseconds\"><media guid=\"FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF\" location=\"file:\\\\test.wav\"/></media-files></interlinear-text></document>";

			LinguaLinksImport li = new LinguaLinksImport(Cache, null, null);
			FDO.IText text = null;
			li.ImportInterlinear(new DummyProgressDlg(), new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())), 0, ref text);
			var firstEntry = Cache.LanguageProject.TextsOC.GetEnumerator();
			firstEntry.MoveNext();
			var imported = firstEntry.Current;
			Assert.True(imported.ContentsOA.ParagraphsOS.Count > 0, "Paragraph was not imported as text content.");
			var para = imported.ContentsOA[0];
			Assert.NotNull(para, "The imported paragraph is null?");
			Assert.True(para.SegmentsOS[0].Guid.ToString().ToUpper().Equals("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB"), "Segment guid not maintained on import.");
			VerifyMediaLink(imported);
		}

		[Test]
		public void TestImportMergeInELANData()
		{
			string firstxml = "<document><interlinear-text guid=\"AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA\"><item type=\"title\" lang=\"en\">wordspace</item>" +
				"<item type=\"title-abbreviation\" lang=\"en\">ws</item><paragraphs><paragraph><phrases><phrase guid=\"BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB\">" +
				"<word guid=\"CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC\"><item type=\"txt\" lang=\"fr\">Word</item></word></phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			string secondxml = "<document><interlinear-text guid=\"AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA\"><item type=\"title\" lang=\"en\">wordspace</item>" +
				"<item type=\"title-abbreviation\" lang=\"en\">ws</item><paragraphs><paragraph><phrases><phrase guid=\"BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB\"  media-file=\"FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF\"  begin-time-offset=\"1\" end-time-offset=\"2\">" +
				"<word guid=\"CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC\"><item type=\"txt\" lang=\"fr\">Word</item></word></phrase></phrases></paragraph></paragraphs>" +
				"<media-files offset-type=\"milliseconds\"><media guid=\"FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF\" location=\"file:\\\\test.wav\"/></media-files></interlinear-text></document>";

			LinguaLinksImport li = new LLIMergeExtension(Cache, null, null);
			FDO.IText text = null;
			li.ImportInterlinear(new DummyProgressDlg(), new MemoryStream(Encoding.ASCII.GetBytes(firstxml.ToCharArray())), 0, ref text);
			Assert.True(text.Guid.ToString().ToUpper().Equals("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA"), "Guid not maintained during import.");
			li.ImportInterlinear(new DummyProgressDlg(), new MemoryStream(Encoding.ASCII.GetBytes(secondxml.ToCharArray())), 0, ref text);
			Assert.True(text.Guid.ToString().ToUpper().Equals("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA"), "Guid not maintained during import.");
			Assert.True(Cache.LanguageProject.TextsOC.Count == 1, "Second text not merged with the first.");
			Assert.True(text.ContentsOA.ParagraphsOS.Count == 1 && text.ContentsOA[0].SegmentsOS.Count == 1, "Segments from second import not merged with the first.");
			VerifyMediaLink(text);
		}

		internal class LLIMergeExtension : LinguaLinksImport
		{
			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="LinguaLinksImport"/> class.
			/// </summary>
			/// <param name="cache">The FDO cache.</param>
			/// <param name="tempDir">The temp directory.</param>
			/// <param name="rootDir">The root directory.</param>
			/// ------------------------------------------------------------------------------------
			public LLIMergeExtension(FdoCache cache, string tempDir, string rootDir) : base(cache, tempDir, rootDir)
			{
			}

			protected override DialogResult ShowPossibleMergeDialog(IThreadedProgress progress)
			{
				return DialogResult.Yes;
			}
		}

		[Test]
		public void TestImportCreatesPersonForSpeaker()
		{
			string xml = "<document><interlinear-text guid=\"AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA\">" +
			"<item type=\"title\" lang=\"en\">wordspace</item>" +
			"<item type=\"title-abbreviation\" lang=\"en\">ws</item>" +
			"<paragraphs><paragraph><phrases><phrase speaker=\"Jimmy Dorante\" media-file=\"FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF\" guid=\"BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB\" begin-time-offset=\"1\" end-time-offset=\"2\">" +
			"<item type=\"text\">This is a test with text.</item><word>This</word></phrase></phrases></paragraph></paragraphs>" +
			"<languages><language lang=\"en\" font=\"latin\" vernacular=\"false\"/></languages>" +
			"<media-files offset-type=\"milliseconds\"><media guid=\"FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF\" location=\"file:\\\\test.wav\"/></media-files></interlinear-text></document>";

			LinguaLinksImport li = new LinguaLinksImport(Cache, null, null);
			FDO.IText text = null;
			li.ImportInterlinear(new DummyProgressDlg(), new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())), 0, ref text);

			Assert.True((Cache.LanguageProject.PeopleOA.PossibilitiesOS[0] as ICmPerson).Name.get_String(Cache.DefaultVernWs).Text.Equals("Jimmy Dorante"),
				"Speaker was not created during the import.");
		}

		[Test]
		public void TestImportCreatesReusesExistingSpeaker()
		{
			string xml = "<document><interlinear-text guid=\"AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA\">" +
			"<item type=\"title\" lang=\"en\">wordspace</item>" +
			"<item type=\"title-abbreviation\" lang=\"en\">ws</item>" +
			"<paragraphs><paragraph><phrases><phrase speaker=\"Jimmy Dorante\" media-file=\"FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF\" guid=\"BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB\" begin-time-offset=\"1\" end-time-offset=\"2\">" +
			"<item type=\"text\">This is a test with text.</item><word>This</word></phrase></phrases></paragraph></paragraphs>" +
			"<languages><language lang=\"en\" font=\"latin\" vernacular=\"false\"/></languages>" +
			"<media-files offset-type=\"milliseconds\"><media guid=\"FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF\" location=\"file:\\\\test.wav\"/></media-files></interlinear-text></document>";

			ICmPerson newPerson = null;
			//Create and add Jimmy to the project.
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				() => { Cache.LanguageProject.PeopleOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
						//person not found create one and add it.
						newPerson = Cache.ServiceLocator.GetInstance<ICmPersonFactory>().Create();
						Cache.LanguageProject.PeopleOA.PossibilitiesOS.Add(newPerson);
						newPerson.Name.set_String(Cache.DefaultVernWs, "Jimmy Dorante");
					  });

			LinguaLinksImport li = new LinguaLinksImport(Cache, null, null);
			FDO.IText text = null;
			li.ImportInterlinear(new DummyProgressDlg(), new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())), 0, ref text);

			//If the import sets the speaker in the segment to our Jimmy, and not a new Jimmy then all is well
			Assert.True(newPerson != null && text.ContentsOA[0].SegmentsOS[0].SpeakerRA == newPerson, "Speaker not reused.");
		}

		private static void VerifyMediaLink(FDO.IText imported)
		{
			var mediaFilesContainer = imported.MediaFilesOA;
			var para = imported.ContentsOA[0];
			Assert.NotNull(mediaFilesContainer, "Media Files not being imported.");
			Assert.True(mediaFilesContainer.MediaURIsOC.Count == 1, "Media file not imported.");
			var enumerator = para.SegmentsOS.GetEnumerator();
			enumerator.MoveNext();
			var seg = enumerator.Current;
			Assert.True(seg.BeginTimeOffset.Equals("1"), "Begin offset not imported correctly");
			Assert.True(seg.EndTimeOffset.Equals("2"), "End offset not imported correctly");
			Assert.True(seg.MediaURIRA == mediaFilesContainer.MediaURIsOC.First(), "Media not correctly linked to segment.");
		}
	}
}
