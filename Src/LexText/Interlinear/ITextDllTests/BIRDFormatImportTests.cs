using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Schema;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.IText.FlexInterlinModel;
using SIL.Utils;

namespace SIL.FieldWorks.IText
{
	class BIRDFormatImportTests : MemoryOnlyBackendProviderBasicTestBase
	{
		public override void TestTearDown()
		{
			base.TestTearDown();
			var repo = Cache.ServiceLocator.GetInstance<ITextRepository>();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				{
					foreach (var text in repo.AllInstances())
						text.Delete();
				});
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

			string p = Path.Combine(FwDirectoryFinder.FlexFolder, @"Export Templates/Interlinear");
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
			// TODO-Linux: The message on Mono doesn't state the failing attribute
			if (!MiscUtils.IsMono)
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
			// TODO-Linux: The message on Mono doesn't state the failing attribute
			if (!MiscUtils.IsMono)
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
			// TODO-Linux: The message on Mono doesn't state the failing attribute
			if (!MiscUtils.IsMono)
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
			// TODO-Linux: The message on Mono doesn't state the failing attribute
			if (!MiscUtils.IsMono)
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
			// TODO-Linux: The message on Mono doesn't state the failing attribute
			if (!MiscUtils.IsMono)
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
			// TODO-Linux: The message on Mono doesn't state the failing attribute
			if (!MiscUtils.IsMono)
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
			// TODO-Linux: The message on Mono doesn't state the failing attribute
			if (!MiscUtils.IsMono)
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
			// TODO-Linux: The message on Mono doesn't state the failing attribute
			if (!MiscUtils.IsMono)
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
			string path = Path.Combine(FwDirectoryFinder.SourceDirectory, @"LexText/Interlinear/ITextDllTests/FlexTextImport");
			string file = Path.Combine(path, "FlexTextExportOutput.flextext");
			XmlDocument doc = new XmlDocument();
			doc.Load(file);

			XmlReader xmlReader = GetXmlReaderForTest(doc.OuterXml);
			Assert.DoesNotThrow(() => ReadXmlForValidation(xmlReader));
		}

		[Test]
		public void ImportParatextExportBasic()
		{
			string path = Path.Combine(FwDirectoryFinder.SourceDirectory, @"LexText/Interlinear/ITextDllTests/FlexTextImport");
			string file = Path.Combine(path, "FlexTextExportOutput.flextext");
			using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
			{
				LinguaLinksImport li = new LinguaLinksImport(Cache, null, null);
				FDO.IText text = null;
				var options = new LinguaLinksImport.ImportInterlinearOptions{Progress = new DummyProgressDlg(),
					BirdData = fileStream, AllottedProgress = 0,
					CheckAndAddLanguages = DummyCheckAndAddLanguagesInternal };

				bool result = li.ImportInterlinear(options, ref text);
				Assert.True(result, "ImportInterlinear was not successful.");
			}
		}

		/*
		 * LT-13505 - FlexText Import of texts with multiple Writing Systems gets them all mixed up
		 */
		[Test]
		public void ImportWordsWithMultipleWss()
		{
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			IWritingSystem wsWbl;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("wbl-Arab-AF", out wsWbl);
			wsWbl.RightToLeftScript = true;
			IWritingSystem wsWblIpa;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("wbl-Qaaa-AF-fonipa-x-Zipa", out wsWblIpa);

			const string xml =
			@"<document version='2'>
			  <interlinear-text guid='5eecc8be-f41b-4433-be94-8950a8ce75e5'>
				<item type='title' lang='wbl-Arab-AF'>تست</item>
				<item type='title' lang='en'>Test</item>
				<item type='comment' lang='en'></item>
				<paragraphs>
				  <paragraph guid='b21daced-5c85-4610-8023-8d7d4b3191f4'>
					<phrases>
					  <phrase guid='0b0346e0-3bb8-40e7-a0a4-f7771d233e93'>
						<item type='segnum' lang='en'>1</item>
						<words>
						  <word guid='0b548dff-6a8e-4c21-a977-fcc4ddc268be'>
							<item type='txt' lang='wbl-Arab-AF'>baseline</item>
							<item type='txt' lang='wbl-Qaaa-AF-fonipa-x-Zipa'>beslain</item>
							<item type='gls' lang='en'>gloss</item>
						  </word>
						</words>
					  </phrase>
					</phrases>
				  </paragraph>
				</paragraphs>
				<languages>
					<language lang='wbl-Arab-AF' font='Times New Roman' vernacular='true' RightToLeft='true' />
					<language lang='en' font='Times New Roman' />
					<language lang='wbl-Qaaa-AF-fonipa-x-Zipa' font='Doulos SIL' vernacular='true' />
				</languages>
			</interlinear-text>
			</document>";

			using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())))
			{
				var options = new LinguaLinksImport.ImportInterlinearOptions
				{
					Progress = new DummyProgressDlg(),
					AnalysesLevel = LinguaLinksImport.ImportAnalysesLevel.Wordform,
					BirdData = stream,
					AllottedProgress = 0,
					CheckAndAddLanguages = DummyCheckAndAddLanguagesInternal
				};
				FDO.IText importedText = null;
				var li = new BIRDFormatImportTests.LLIMergeExtension(Cache, null, null);
				var result = li.ImportInterlinear(options, ref importedText);
				Assert.True(result, "ImportInterlinear was not successful.");
				Assert.That(importedText.ContentsOA.ParagraphsOS.Count, Is.EqualTo(1));
				var paraImported = importedText.ContentsOA[0];
				var testPara = paraImported.Contents;
				Assert.That(testPara.Text, Is.EqualTo("baseline"));
				Assert.That(TsStringUtils.GetWsAtOffset(testPara, 0), Is.EqualTo(wsWbl.Handle));
				Assert.That(testPara.RunCount, Is.EqualTo(1));
				// main writing system should be the first one in the import
				var mainWs = importedText.ContentsOA.MainWritingSystem;
				Assert.That(mainWs, Is.EqualTo(wsWbl.Handle));

				// Next verify that the IPA content got added to the imported word form
				var a0 = paraImported.SegmentsOS[0].AnalysesRS[0];
				var wf0 = a0 as IWfiWordform;
				Assert.That(wf0, Is.Not.Null);
				Assert.That(wf0.Form.get_String(wsWbl.Handle).Text, Is.EqualTo("baseline"));
				Assert.That(wf0.Form.get_String(wsWblIpa.Handle).Text, Is.EqualTo("beslain"));
			}
		}

		#endregion ScrElements

		[Test]
		public void UglyEmptyDataShouldNotCrash()
		{
			const string textGuid = "eb0770f4-23aa-4f7b-b45d-fe745a3790a2";
			//an interliner text example xml string
			const string xml = "<document><interlinear-text guid='" + textGuid + "'>" +
									 "<paragraphs><paragraph guid='819742f3-3840-479e-9300-51755880680b'/></paragraphs><languages/>" +
									 "</interlinear-text></document>";

			var li = new LinguaLinksImport(Cache, null, null);
			FDO.IText text = null;
			using(var stream = new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())))
			{
				Assert.DoesNotThrow(()=> li.ImportInterlinear(new DummyProgressDlg(), stream, 0, ref text));
				using(var firstEntry = Cache.LanguageProject.Texts.GetEnumerator())
				{
					firstEntry.MoveNext();
					var imported = firstEntry.Current;
					//The empty ugly text imported as its empty ugly self
					Assert.AreEqual(imported.Guid.ToString(), textGuid);
				}
			}
		}

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
			using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())))
			{
				li.ImportInterlinear(new DummyProgressDlg(), stream, 0, ref text);
				using (var firstEntry = Cache.LanguageProject.Texts.GetEnumerator())
				{
					firstEntry.MoveNext();
					var imported = firstEntry.Current;
					//The title imported
					Assert.True(imported.Name.get_String(Cache.WritingSystemFactory.get_Engine("en").Handle).Text.Equals(title));
					//The title abbreviation imported
					Assert.True(imported.Abbreviation.get_String(Cache.WritingSystemFactory.get_Engine("en").Handle).Text.Equals(abbr));
				}
			}
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
			using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())))
			{
				li.ImportInterlinear(new DummyProgressDlg(), stream, 0, ref text);
				using (var firstEntry = Cache.LanguageProject.Texts.GetEnumerator())
				{
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
					CollectionAssert.AreEqual(new [] {" ", " ", " ", " ", " "},
						new [] {spaceOne, spaceTwo, spaceThree, spaceFour, spaceFive});
				}
			}
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
			using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())))
			{
				li.ImportInterlinear(new DummyProgressDlg(), stream, 0, ref text);
				using (var firstEntry = Cache.LanguageProject.Texts.GetEnumerator())
				{
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
					CollectionAssert.AreEqual(new[] { " ", " ", " ", " ", " ", " " },
						new[] { spaceOne, spaceTwo, spaceThree, spaceFour, spaceFive, spaceSix });
				}
			}
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
			using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())))
			{
				li.ImportInterlinear(new DummyProgressDlg(), stream, 0, ref text);
				using (var firstEntry = Cache.LanguageProject.Texts.GetEnumerator())
				{
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
			}
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
			using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())))
			{
				li.ImportInterlinear(new DummyProgressDlg(), stream, 0, ref text);
				using (var firstEntry = Cache.LanguageProject.Texts.GetEnumerator())
				{
					firstEntry.MoveNext();
					var imported = firstEntry.Current;
					var para = imported.ContentsOA[0];
					Assert.IsTrue(para.Contents.Text.Equals("Text not built from words."));
				}
			}
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
			using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())))
			{
				li.ImportInterlinear(new DummyProgressDlg(), stream, 0, ref text);
				using (var firstEntry = Cache.LanguageProject.Texts.GetEnumerator())
				{
					firstEntry.MoveNext();
					var imported = firstEntry.Current;
					Assert.True(imported.ContentsOA.ParagraphsOS.Count > 0, "Empty paragraph was not imported as text content.");
					var para = imported.ContentsOA[0];
					Assert.NotNull(para, "The imported paragraph is null?");
				}
			}
		}

		[Test]
		public void TestImportFullELANData()
		{
			const string xml = "<document><interlinear-text guid=\"AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA\">" +
				"<item type=\"title\" lang=\"en\">wordspace</item>" +
				"<item type=\"title-abbreviation\" lang=\"en\">ws</item>" +
				"<paragraphs><paragraph><phrases>" +
				"<phrase media-file=\"FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF\" guid=\"BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB\" begin-time-offset=\"1\" end-time-offset=\"2\">" +
				"<item type=\"text\">This is a test with text.</item><word>This</word></phrase></phrases></paragraph></paragraphs>" +
				"<languages><language lang=\"en\" font=\"latin\" vernacular=\"false\"/></languages>" +
				@"<media-files offset-type=""milliseconds""><media guid=""FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"" location=""file:\\test.wav""/></media-files></interlinear-text></document>";

			LinguaLinksImport li = new LinguaLinksImport(Cache, null, null);
			FDO.IText text = null;
			using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())))
			{
				li.ImportInterlinear(new DummyProgressDlg(), stream, 0, ref text);
				using (var firstEntry = Cache.LanguageProject.Texts.GetEnumerator())
				{
					firstEntry.MoveNext();
					var imported = firstEntry.Current;
					Assert.True(imported.ContentsOA.ParagraphsOS.Count > 0, "Paragraph was not imported as text content.");
					var para = imported.ContentsOA[0];
					Assert.NotNull(para, "The imported paragraph is null?");
					Assert.AreEqual(new Guid("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB"), para.SegmentsOS[0].Guid, "Segment guid not maintained on import.");
					VerifyMediaLink(imported);
				}
			}
		}

		[Test]
		public void TestImportMergeInELANData()
		{
			const string firstxml = "<document><interlinear-text guid=\"AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA\"><item type=\"title\" lang=\"en\">wordspace</item>" +
				"<item type=\"title-abbreviation\" lang=\"en\">ws</item><paragraphs><paragraph><phrases><phrase guid=\"BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB\">" +
				"<word guid=\"CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC\"><item type=\"txt\" lang=\"fr\">Word</item></word></phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			// Contains all of the original contents of firstxml, plus a media file
			const string secondxml = "<document><interlinear-text guid=\"AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA\"><item type=\"title\" lang=\"en\">wordspace</item>" +
				"<item type=\"title-abbreviation\" lang=\"en\">ws</item><paragraphs><paragraph><phrases>" +
				"<phrase guid=\"BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB\" media-file=\"FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF\" begin-time-offset=\"1\" end-time-offset=\"2\">" +
				"<word guid=\"CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC\"><item type=\"txt\" lang=\"fr\">Word</item></word></phrase></phrases></paragraph></paragraphs>" +
				@"<media-files offset-type=""milliseconds""><media guid=""FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"" location=""file:\\test.wav""/></media-files></interlinear-text></document>";

			var li = new LLIMergeExtension(Cache, null, null);
			FDO.IText text = null;
			using (var firstStream = new MemoryStream(Encoding.ASCII.GetBytes(firstxml.ToCharArray())))
			{
				li.ImportInterlinear(new DummyProgressDlg(), firstStream, 0, ref text);
				Assert.AreEqual(0, li.NumTimesDlgShown, "The user should not have been prompted to merge on the initial import.");
				Assert.AreEqual(new Guid("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA"), text.Guid, "Guid not maintained during import.");
				using (var secondStream = new MemoryStream(Encoding.ASCII.GetBytes(secondxml.ToCharArray())))
				{
					li.ImportInterlinear(new DummyProgressDlg(), secondStream, 0, ref text);
					Assert.AreEqual(1, li.NumTimesDlgShown, "The user should have been prompted to merge the text with the same Guid.");
					Assert.AreEqual(new Guid("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA"), text.Guid, "Guid not maintained during import.");
					Assert.AreEqual(1, Cache.LanguageProject.Texts.Count, "Second text not merged with the first.");
					Assert.AreEqual(1, text.ContentsOA.ParagraphsOS.Count, "Paragraph from second import not merged with the first.");
					Assert.AreEqual(1, text.ContentsOA[0].SegmentsOS.Count, "Segment from second import not merged with the first.");
					VerifyMediaLink(text);
					var mediaContainerGuid = text.MediaFilesOA.Guid;
					Assert.AreEqual(new Guid("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"), text.MediaFilesOA.MediaURIsOC.First().Guid,
						"Guid not maintained during import.");
					Assert.AreEqual(@"file:\\test.wav", text.MediaFilesOA.MediaURIsOC.First().MediaURI, "URI was not imported correctly.");
					using (var thirdStream = new MemoryStream(Encoding.ASCII.GetBytes(secondxml.Replace("test.wav", "retest.wav").ToCharArray())))
					{
						li.ImportInterlinear(new DummyProgressDlg(), thirdStream, 0, ref text);
						Assert.AreEqual(2, li.NumTimesDlgShown, "The user should have been prompted again to merge the text with the same Guid.");
						Assert.AreEqual(new Guid("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA"), text.Guid, "Guid not maintained during import.");
						Assert.AreEqual(1, Cache.LanguageProject.Texts.Count, "Duplicate text not merged with extant.");
						Assert.AreEqual(1, text.ContentsOA.ParagraphsOS.Count, "Paragraph from third import not merged with the extant.");
						Assert.AreEqual(1, text.ContentsOA[0].SegmentsOS.Count, "Segment from third import not merged with the extant.");
						VerifyMediaLink(text);
						Assert.AreEqual(mediaContainerGuid, text.MediaFilesOA.Guid, "Merging should not replace the media container.");
						Assert.AreEqual(new Guid("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"), text.MediaFilesOA.MediaURIsOC.First().Guid,
							"Guid not maintained during import.");
						Assert.AreEqual(@"file:\\retest.wav", text.MediaFilesOA.MediaURIsOC.First().MediaURI, "URI was not updated.");
					}
				}
			}
		}

		[Test]
		public void TestImportTwiceWithoutMerge()
		{
			const string importxml = "<document><interlinear-text guid=\"AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA\"><item type=\"title\" lang=\"en\">wordspace</item>" +
				"<item type=\"title-abbreviation\" lang=\"en\">ws</item><paragraphs><paragraph><phrases>" +
				"<phrase guid=\"BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB\" media-file=\"FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF\" begin-time-offset=\"1\" end-time-offset=\"2\">" +
				"<word guid=\"CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC\"><item type=\"txt\" lang=\"fr\">Word</item></word></phrase></phrases></paragraph></paragraphs>" +
				@"<media-files offset-type=""milliseconds""><media guid=""FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"" location=""file:\\test.wav""/></media-files></interlinear-text></document>";

			var li = new LLINomergeExtension(Cache, null, null);
			FDO.IText firstText = null;
			FDO.IText secondText = null;
			using (var firstStream = new MemoryStream(Encoding.ASCII.GetBytes(importxml.ToCharArray())))
			{
				li.ImportInterlinear(new DummyProgressDlg(), firstStream, 0, ref firstText);
				Assert.AreEqual(0, li.NumTimesDlgShown, "The user should not have been prompted to merge on the initial import.");
				Assert.AreEqual(new Guid("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA"), firstText.Guid, "Guid not maintained during import.");
				Assert.AreEqual(1, Cache.LanguageProject.Texts.Count, "Text not imported properly.");
				Assert.AreEqual(1, firstText.ContentsOA.ParagraphsOS.Count, "Text not imported properly.");
				Assert.AreEqual(1, firstText.ContentsOA[0].SegmentsOS.Count, "Text not imported properly.");
				//Assert.AreEqual("TODO: B", firstText.ContentsOA.ParagraphsOS.);
				VerifyMediaLink(firstText);
				var mediaContainerGuid = firstText.MediaFilesOA.Guid;
				Assert.AreEqual(new Guid("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"), firstText.MediaFilesOA.MediaURIsOC.First().Guid,
					"Guid not maintained during import.");
				Assert.AreEqual(@"file:\\test.wav", firstText.MediaFilesOA.MediaURIsOC.First().MediaURI, "URI was not imported correctly.");
				using (var secondStream = new MemoryStream(Encoding.ASCII.GetBytes(importxml.Replace("test.wav", "retest.wav").ToCharArray())))
				{
					li.ImportInterlinear(new DummyProgressDlg(), secondStream, 0, ref secondText);
					Assert.AreEqual(1, li.NumTimesDlgShown, "The user should have been prompted to merge the text with the same Guid.");
					Assert.AreEqual(2, Cache.LanguageProject.Texts.Count, "We imported twice and didn't merge; there should be two texts.");

					Assert.AreEqual(new Guid("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA"), firstText.Guid, "First text should remain unchanged.");
					Assert.AreEqual(1, firstText.ContentsOA.ParagraphsOS.Count, "First text should remain unchanged.");
					Assert.AreEqual(1, firstText.ContentsOA[0].SegmentsOS.Count, "First text should remain unchanged.");
					VerifyMediaLink(firstText);
					Assert.AreEqual(mediaContainerGuid, firstText.MediaFilesOA.Guid, "First text should remain unchanged.");
					Assert.AreEqual(new Guid("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"), firstText.MediaFilesOA.MediaURIsOC.First().Guid,
						"First text should remain unchanged.");
					Assert.AreEqual(@"file:\\test.wav", firstText.MediaFilesOA.MediaURIsOC.First().MediaURI, "First text should remain unchanged.");

					Assert.AreNotEqual(new Guid("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA"), secondText.Guid, "Second text should have a unique Guid.");
					Assert.AreEqual(1, secondText.ContentsOA.ParagraphsOS.Count, "Second text not imported properly.");
					Assert.AreEqual(1, secondText.ContentsOA[0].SegmentsOS.Count, "Second text not imported properly.");
					VerifyMediaLink(secondText);
					Assert.AreNotEqual(mediaContainerGuid, secondText.MediaFilesOA.Guid, "Second text's media container should have a unique Guid.");
					Assert.AreNotEqual(new Guid("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"), secondText.MediaFilesOA.MediaURIsOC.First().Guid,
						"Second text's media URI should have a unique Guid.");
					Assert.AreEqual(@"file:\\retest.wav", secondText.MediaFilesOA.MediaURIsOC.First().MediaURI, "URI was not imported correctly.");
				}
			}
		}

		/// <summary>LinguaLinksImport for testing the choice to merge, without actually showing the dialog during unit tests</summary>
		internal class LLIMergeExtension : LinguaLinksImport
		{
			/// <summary/>
			public int NumTimesDlgShown { get; private set; }

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
				NumTimesDlgShown = 0;
			}

			protected override DialogResult ShowPossibleMergeDialog(IThreadedProgress progress)
			{
				NumTimesDlgShown++;
				return DialogResult.Yes;
			}
		}

		/// <summary>LinguaLinksImport for testing the choice NOT to merge, without actually showing the dialog during unit tests</summary>
		internal class LLINomergeExtension : LinguaLinksImport
		{
			/// <summary/>
			public int NumTimesDlgShown { get; private set; }

			/// <summary/>
			public LLINomergeExtension(FdoCache cache, string tempDir, string rootDir) : base(cache, tempDir, rootDir)
			{
				NumTimesDlgShown = 0;
			}

			protected override DialogResult ShowPossibleMergeDialog(IThreadedProgress progress)
			{
				NumTimesDlgShown++;
				return DialogResult.No;
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
			using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())))
			{
				li.ImportInterlinear(new DummyProgressDlg(), stream, 0, ref text);

				Assert.AreEqual("Jimmy Dorante",
					(Cache.LanguageProject.PeopleOA.PossibilitiesOS[0] as ICmPerson).Name.get_String(Cache.DefaultVernWs).Text,
					"Speaker was not created during the import.");
			}
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
				() => {
				Cache.LanguageProject.PeopleOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
				//person not found create one and add it.
				newPerson = Cache.ServiceLocator.GetInstance<ICmPersonFactory>().Create();
				Cache.LanguageProject.PeopleOA.PossibilitiesOS.Add(newPerson);
				newPerson.Name.set_String(Cache.DefaultVernWs, "Jimmy Dorante");
			});
			Assert.NotNull(newPerson);

			LinguaLinksImport li = new LinguaLinksImport(Cache, null, null);
			FDO.IText text = null;
			using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())))
			{
				li.ImportInterlinear(new DummyProgressDlg(), stream, 0, ref text);

				//If the import sets the speaker in the segment to our Jimmy, and not a new Jimmy then all is well
				Assert.AreEqual(newPerson, text.ContentsOA[0].SegmentsOS[0].SpeakerRA, "Speaker not reused.");
			}
		}

		private static void VerifyMediaLink(FDO.IText imported)
		{
			var mediaFilesContainer = imported.MediaFilesOA;
			var para = imported.ContentsOA[0];
			Assert.NotNull(mediaFilesContainer, "Media Files not being imported.");
			Assert.AreEqual(1, mediaFilesContainer.MediaURIsOC.Count, "Media file not imported.");
			using (var enumerator = para.SegmentsOS.GetEnumerator())
			{
				enumerator.MoveNext();
				var seg = enumerator.Current;
				Assert.AreEqual("1", seg.BeginTimeOffset, "Begin offset not imported correctly");
				Assert.AreEqual("2", seg.EndTimeOffset, "End offset not imported correctly");
				Assert.AreEqual(seg.MediaURIRA, mediaFilesContainer.MediaURIsOC.First(), "Media not correctly linked to segment.");
			}
		}
	}
}
