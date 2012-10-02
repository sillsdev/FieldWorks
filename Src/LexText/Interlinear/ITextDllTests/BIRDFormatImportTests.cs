using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.IText
{
	class BIRDFormatImportTests : MemoryOnlyBackendProviderBasicTestBase
	{
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

		public override void TestTearDown()
		{
			base.TestTearDown();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => Cache.LanguageProject.TextsOC.Clear());
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

			LinguaLinksImport li = new LinguaLinksImport(Cache, null, null);
			FDO.IText text = null;
			li.ImportInterlinear(new DummyProgressDlg(), new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())), 0, ref text);
			var firstEntry = Cache.LanguageProject.TextsOC.GetEnumerator();
			firstEntry.MoveNext();
			var imported = firstEntry.Current;
			var para = imported.ContentsOA[0];
			var spaceOne = para.Contents.Text.Substring(2, 1); //should be: " "
			var spaceTwo = para.Contents.Text.Substring(5, 1); //should be: " "
			var spaceThree = para.Contents.Text.Substring(9, 1);
			var spaceFour = para.Contents.Text.Substring(13, 1);
			var spaceFive = para.Contents.Text.Substring(15, 1);
			//test to make sure no space was inserted before the comma, this is probably captured by the other assert
			Assert.IsTrue(para.Contents.Text.Split(new char[]{' '}).Length == 6); //capture correct number of spaces, and no double spaces
			//test to make sure spaces were inserted in each expected place
			CollectionAssert.AreEqual(new [] {spaceOne, spaceTwo, spaceThree, spaceFour, spaceFive},
									  new [] {" ", " ", " ", " ", " "});

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
	}
}
