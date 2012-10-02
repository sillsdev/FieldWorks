// <copyright from='2010' to='2010' company='SIL International'>
// Copyright (c) 2010, SIL International. All Rights Reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Test the StringServices functions.
	/// </summary>
	[TestFixture]
	public class StringServicesTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		/// Tests correct operation of StringServices.MergeStyles.
		/// </summary>
		[Test]
		public void MergeStyles()
		{
			// Text with style red will be changed to yellow, green to blue, purple to no style at all.
			var specification = new Dictionary<string, string>();
			specification["red"] = "yellow";
			specification["blue"] = "green";
			specification["purple"] = null;

			var entry = MakeEntry("kick", "strike with foot");
			var parts1 = new[] { "lead", "red part", "middle", "old green part", "blue part", "purple part", "another red part", "a black part" };
			var styles1 = new[] { null, "red", null, "green", "blue", "purple", "red", "black" };
			var testString1 = MakeStyledString(Cache.DefaultVernWs, parts1, styles1);
			var parts2 = new[] { "red part", "middle", "purple part" };
			var styles2 = new[] { "red", null, "purple" };
			var testString2 = MakeStyledString(Cache.DefaultAnalWs, parts2, styles2);

			// Want some data in a multistring.
			entry.SensesOS[0].Definition.VernacularDefaultWritingSystem = testString1;
			// And in another alternative
			entry.SensesOS[0].Definition.AnalysisDefaultWritingSystem = testString2;

			// And in some simple string properties, and some paragraph styles
			var sttext = MakeText(new ITsString[] {testString1, testString2, testString1,
				Cache.TsStrFactory.MakeString("nothing", Cache.DefaultVernWs)},
				new string[] { "red", "blue", "purple", "black" });

			StringServices.ReplaceStyles(Cache, specification);

			//var styles1 = new[] { null, "red", null, "green", "blue", "purple", "red", "black" };
			var expectedStyles1 = new[] { null, "yellow", null, "green", "green", null, "yellow", "black" };
			var expectedStyles2 = new[] { "yellow", null, null };
			var expectedParaStyles = new[] { "yellow", "green", null, "black" };

			VerifyString(entry.SensesOS[0].Definition.VernacularDefaultWritingSystem, parts1, expectedStyles1);
			VerifyString(entry.SensesOS[0].Definition.AnalysisDefaultWritingSystem, parts2, expectedStyles2);
			VerifyString(((IStTxtPara)sttext.ParagraphsOS[0]).Contents, parts1, expectedStyles1);
			VerifyString(((IStTxtPara)sttext.ParagraphsOS[1]).Contents, parts2, expectedStyles2);
			VerifyString(((IStTxtPara)sttext.ParagraphsOS[2]).Contents, parts1, expectedStyles1);
			VerifyString(((IStTxtPara)sttext.ParagraphsOS[3]).Contents, new[] { "nothing" }, new string[] { null });
			for (int i = 0; i < 4; i++)
			{
				Assert.That(sttext.ParagraphsOS[i].StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle),
					Is.EqualTo(expectedParaStyles[i]));
			}
		}


		private void VerifyString(ITsString tss, string[] parts, string[] expectedStyles)
		{
			// The number of runs is not necessarily the same as the number of parts, because some runs may have merged.
			Assert.AreEqual(parts.Length, expectedStyles.Length);
			Assert.That(tss.Length, Is.EqualTo((from item in parts select item.Length).Sum()));
			int start = 0;
			for (int i = 0; i < parts.Length; i++)
			{
				int end = start + parts[i].Length;
				var sub = tss.GetSubstring(start, end);
				Assert.That(sub.RunCount, Is.EqualTo(1), " part " + i + " (" + parts[i] + ") has too many runs in string " + tss.Text );
				Assert.That(sub.Text, Is.EqualTo(parts[i]));
				Assert.That(sub.get_Properties(0).GetStrPropValue((int)FwTextPropType.ktptNamedStyle), Is.EqualTo(expectedStyles[i]),
					" part " + i + " (" + parts[i] + ") has the wrong style " + tss.Text);
				start = end;
			}
		}

		private ITsString MakeStyledString(int ws, string[] parts, string[] styles)
		{
			Assert.AreEqual(parts.Length, styles.Length);
			var bldr = Cache.TsStrFactory.MakeString("", ws).GetBldr();
			for (int i = 0; i < parts.Length; i++)
			{
				var content = parts[i];
				var style = styles[i];
				int start = bldr.Length;
				bldr.Replace(start, start, content, null);
				bldr.SetStrPropValue(start, bldr.Length, (int)FwTextPropType.ktptNamedStyle, style);
			}
			return bldr.GetString();
		}

		private ILexEntry MakeEntry(string lf, string gloss)
		{
			ILexEntry entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var form = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.LexemeFormOA = form;
			form.Form.VernacularDefaultWritingSystem =
				Cache.TsStrFactory.MakeString(lf, Cache.DefaultVernWs);
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			sense.Gloss.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString(gloss,
				Cache.DefaultAnalWs);
			return entry;
		}
		private IStText MakeText(ITsString[] paragraphs, string[] styles)
		{
			var text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
			Cache.LangProject.TextsOC.Add(text);
			IStText sttext = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			text.ContentsOA = sttext;
			var paraFactory = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>();
			for (int i = 0; i < paragraphs.Length; i++)
			{
				var para = paraFactory.Create();
				sttext.ParagraphsOS.Add(para);
				para.Contents = paragraphs[i];
				var bldr = TsPropsBldrClass.Create();
				bldr.SetStrPropValue((int) FwTextPropType.ktptNamedStyle, styles[i]);
				para.StyleRules = bldr.GetTextProps();
			}
			return sttext;
		}

		private IStStyle MakeStyle(string name)
		{
			var style = Cache.ServiceLocator.GetInstance<IStStyleFactory>().Create();
			Cache.LangProject.StylesOC.Add(style);
			style.Name = name;
			return style;
		}

		private IStTxtPara MakeSimpleText(string content)
		{
			var text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
			Cache.LangProject.TextsOC.Add(text);
			IStText sttext = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			text.ContentsOA = sttext;
			var paraFactory = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>();
			var para = paraFactory.Create();
			sttext.ParagraphsOS.Add(para);
			para.Contents = Cache.TsStrFactory.MakeString(content, Cache.DefaultVernWs);
			return para;
		}

		/// <summary>
		/// Test the function for getting the hyperlinks that start with a specified folder,
		/// and the function for changing them to another folder.
		/// </summary>
		[Test]
		public void GetHyperlinksInFolder()
		{
			// Test cases cover multistring, regular string, two matches in same string, and a link that doesn't match.
			var entry = MakeEntry("kick", "hit with foot");
			var link = FileUtils.ChangeWindowsPathIfLinux(@"c:\testlangproj\linkedFiles\other\Myfile.wav");
			var tss = Cache.TsStrFactory.MakeString("This here is a link", Cache.DefaultAnalWs);
			var bldr = tss.GetBldr();
			var linkStyle = MakeStyle("testStyle");
			StringServices.MarkTextInBldrAsHyperlink(bldr, 5, 9, link, linkStyle);
			entry.LiteralMeaning.AnalysisDefaultWritingSystem = bldr.GetString();

			var para = MakeSimpleText("OneTwoSix");
			bldr = para.Contents.GetBldr();
			StringServices.MarkTextInBldrAsHyperlink(bldr, 0, 3, link, linkStyle);
			StringServices.MarkTextInBldrAsHyperlink(bldr, 3, 6, "This is a link but not matching", linkStyle);
			StringServices.MarkTextInBldrAsHyperlink(bldr, 6, 9, link, linkStyle);
			para.Contents = bldr.GetString();

			var para2 = MakeSimpleText("ab" + link + "cd" + link + "ef");
			bldr = para2.Contents.GetBldr();
			StringServices.MarkTextInBldrAsHyperlink(bldr, 2, link.Length + 2, link, linkStyle);
			StringServices.MarkTextInBldrAsHyperlink(bldr, 4 + link.Length, 4 + 2 * link.Length, link, linkStyle);
			para2.Contents = bldr.GetString();

			var hyperlinkInfo = StringServices.GetHyperlinksInFolder(Cache, FileUtils.ChangeWindowsPathIfLinux(@"c:\testlangproj\linkedFiles"));
			Assert.That(hyperlinkInfo, Has.Count.EqualTo(5), "should find all four matching links but not the other one");
			var relPath = FileUtils.ChangeWindowsPathIfLinux(@"other\Myfile.wav");
			VerifyHyperlinkInfo(hyperlinkInfo, entry, LexEntryTags.kflidLiteralMeaning, Cache.DefaultAnalWs, 5, 9, relPath);
			VerifyHyperlinkInfo(hyperlinkInfo, para, StTxtParaTags.kflidContents, 0, 0, 3, relPath);
			VerifyHyperlinkInfo(hyperlinkInfo, para, StTxtParaTags.kflidContents, 0, 6, 9, relPath);
			VerifyHyperlinkInfo(hyperlinkInfo, para2, StTxtParaTags.kflidContents, 0, 2, link.Length + 2, relPath);
			VerifyHyperlinkInfo(hyperlinkInfo, para2, StTxtParaTags.kflidContents, 0, 4 + link.Length, 4 + 2 * link.Length, relPath);

			StringServices.FixHyperlinkFolder(hyperlinkInfo, FileUtils.ChangeWindowsPathIfLinux(@"c:\testlangproj\linkedFiles"), FileUtils.ChangeWindowsPathIfLinux(@"c:\testlangproj\externalLinks"));
			var newText = FileUtils.ChangeWindowsPathIfLinux(@"c:\testlangproj\externalLinks\other\Myfile.wav");
			VerifyObjData(entry.LiteralMeaning.AnalysisDefaultWritingSystem, 5, 9, FileUtils.ChangeWindowsPathIfLinux(@"c:\testlangproj\externalLinks\other\Myfile.wav"));
			VerifyObjData(para.Contents, 0, 3, newText);
			VerifyObjData(para.Contents, 6, 9, newText);
			Assert.That(para2.Contents.Text, Is.EqualTo("ab" + newText + "cd" + newText + "ef"));
			VerifyObjData(para2.Contents, 2, newText.Length + 2, newText);
			VerifyObjData(para2.Contents, 4 + newText.Length, 4 + 2 * newText.Length, newText);
		}

		/// <summary>
		/// If the link text is the same as the link, it should be updated as well as the link.
		/// </summary>
		[Test]
		public void FixHyperlinkFolder_linkTextSameAsLink_isUpdated()
		{
			string origPathSetInDatabase = FileUtils.ChangePathToPlatform("/origdir/file.txt");
			string lookupPath = FileUtils.ChangePathToPlatform("/origdir");
			string lookupResultToVerify = "file.txt";
			string pathToRebaseFrom = FileUtils.ChangePathToPlatform("/origdir");
			string pathToRebaseTo = FileUtils.ChangePathToPlatform("/newdir");
			string rebasedPathToVerify = FileUtils.ChangePathToPlatform("/newdir/file.txt");

			FixHyperlinkFolder_linkTextSameAsLink_helper(origPathSetInDatabase, lookupPath,
				lookupResultToVerify, pathToRebaseFrom, pathToRebaseTo, rebasedPathToVerify);
		}

		/// <summary>
		/// If a FieldWorks database was created on a Windows machine with Windows-style
		/// external link paths, and that database is opened on a Linux machine, then
		/// FieldWorks in Linux should find the Windows-style external link paths when
		/// looked up using Linux-style paths.
		/// The situation also needs to work in reverse when a Windows FieldWorks opens
		/// a database created by Linux FieldWorks.
		/// This test tests either of the two situations depending on which platform the test
		/// is being run on.
		/// </summary>
		[Test]
		public void GetHyperlinksInFolder_changeOfPlatform()
		{
			string origPathSetInDatabase = MiscUtils.IsUnix ?
				@"c:\testlangproj\linkedFiles\other\Myfile.wav" :
				"/testlangproj/linkedFiles/other/Myfile.wav";
			string newPlatformStyleLookupPath = MiscUtils.IsUnix ?
				"/testlangproj/linkedFiles" : @"C:\testlangproj\linkedFiles";
			string newPlatformStyleResultToVerify = MiscUtils.IsUnix ?
				"other/Myfile.wav" : @"other\Myfile.wav";

			GetHyperlinksInFolder_changeOfPlatform_helper(origPathSetInDatabase,
				newPlatformStyleLookupPath, newPlatformStyleResultToVerify);
		}

		/// <summary>
		/// Similar to the situation tested in GetHyperlinksInFolder_changeOfPlatform(),
		/// if a FieldWorks database was created on a Windows machine, then when using
		/// that database on a Linux machine, FieldWorks should find the Windows-style
		/// external link paths when looked up using Linux-style paths.
		/// For FixHyperlinkFolder, if a Windows machine created an external link of
		/// "C:\dir\file.txt", and a Linux machine opens the database and wants to
		/// rebase paths from "/dir" to "/NEWdir", then it should rebase the external link
		/// path "C:\dir\file.txt" to "/NEWdir/file.txt", without claiming that it can't
		/// find any external links pointing to anything in "/dir".
		/// The situation also needs to work in reverse when a Windows FieldWorks opens
		/// a database created by Linux FieldWorks.
		/// This test tests either of the two situations depending on which platform the
		/// test is being run on.
		/// </summary>
		[Test]
		public void FixHyperlinkFolder_changeOfPlatform()
		{
			// When running on Windows, call "foreign"="Linux" and "current"="Windows".
			// When running on Linux, call "foreign"="Windows" and "current"="Linux".

			string origPathSetInDatabase = MiscUtils.IsUnix ? @"c:\origdir\file.txt" : "/origdir/file.txt";
			string newPlatformStyleLookupPath = MiscUtils.IsUnix ? "/origdir" : @"C:\origdir";
			string newPlatformStyleResultToVerify = MiscUtils.IsUnix ? "file.txt" : "file.txt";
			string pathToRebaseFrom = MiscUtils.IsUnix ? "/origdir" : @"C:\origdir";
			string pathToRebaseTo = MiscUtils.IsUnix ? "/newdir" : @"C:\newdir";
			string rebasedPathToVerify = MiscUtils.IsUnix ? "/newdir/file.txt": @"C:\newdir\file.txt";

			// Add an external link into database and look it up
			var output = GetHyperlinksInFolder_changeOfPlatform_helper(origPathSetInDatabase,
				newPlatformStyleLookupPath, newPlatformStyleResultToVerify);
			var hyperlinkInfo = output.Item1;
			var entry = output.Item2;

			// Update link paths using currentOS-style paths, even though they are presently stored
			// as foreign-style paths.
			StringServices.FixHyperlinkFolder(hyperlinkInfo, pathToRebaseFrom, pathToRebaseTo);
			VerifyHyperlinkInfo(hyperlinkInfo, entry, LexEntryTags.kflidLiteralMeaning, Cache.DefaultAnalWs, 5, 9, newPlatformStyleResultToVerify);
			VerifyObjData(entry.LiteralMeaning.AnalysisDefaultWritingSystem, 5, 9, rebasedPathToVerify);
		}

		/// <summary>
		/// Helper method for GetHyperlinksInFolder_changeOfPlatform and
		/// FixHyperlinkFolder_changeOfPlatform.
		/// </summary>
		private Tuple<List<StringServices.LinkOccurrence>, ILexEntry>
			GetHyperlinksInFolder_changeOfPlatform_helper(string origPathSetInDatabase,
			string newPlatformStyleLookupPath, string newPlatformStyleResultToVerify)
		{
			// When running on Windows, call "foreign"="Linux" and "current"="Windows".
			// When running on Linux, call "foreign"="Windows" and "current"="Linux".

			var entry = MakeEntry("kick", "hit with foot");
			// Foreign-style path is set by a foreign-platform FieldWorks
			var link = origPathSetInDatabase;
			var tss = Cache.TsStrFactory.MakeString("This here is a link", Cache.DefaultAnalWs);
			var bldr = tss.GetBldr();
			var linkStyle = MakeStyle("testStyle");
			StringServices.MarkTextInBldrAsHyperlink(bldr, 5, 9, link, linkStyle);
			entry.LiteralMeaning.AnalysisDefaultWritingSystem = bldr.GetString();

			// Now current-platform FieldWorks opens and reads the database created by a
			// foreign-platform FieldWorks, and needs to respond to currentOS-style path lookups
			// that match foreign-style stored paths.
			var hyperlinkInfo = StringServices.GetHyperlinksInFolder(Cache, newPlatformStyleLookupPath);
			// There should be one hyperlink, and with the proper path.
			Assert.That(hyperlinkInfo, Has.Count.EqualTo(1),
				"Should have found hyperlink despite different platform style path being used during lookup");
			var relPath = newPlatformStyleResultToVerify;
			VerifyHyperlinkInfo(hyperlinkInfo, entry, LexEntryTags.kflidLiteralMeaning, Cache.DefaultAnalWs, 5, 9, relPath);
			return new Tuple<List<StringServices.LinkOccurrence>, ILexEntry>(hyperlinkInfo, entry);
		}

		/// <summary>
		/// If the link text is the same as the link, it should be updated as well as the link,
		/// and this should work when using the database on a platform other than the one
		/// on which the link was created.
		/// </summary>
		[Test]
		public void FixHyperlinkFolder_linkTextSameAsLinkDespiteChangeOfPlatform_linkTextIsUpdated()
		{
			// When running on Windows, call "foreign"="Linux" and "current"="Windows".
			// When running on Linux, call "foreign"="Windows" and "current"="Linux".

			// Foreign-style path is set by a foreign-platform FieldWorks
			string origPathSetInDatabase =
				MiscUtils.IsUnix ? @"C:\origdir\file.txt" : "/origdir/file.txt";
			// Now current-platform FieldWorks opens and reads the database created by a
			// foreign-platform FieldWorks, and needs to respond to currentOS-style path lookups
			// that match foreign-style stored paths.
			string newPlatformStyleLookupPath = MiscUtils.IsUnix ? "/origdir" : @"C:\origdir";
			string newPlatformStyleResultToVerify = MiscUtils.IsUnix ? "file.txt" : "file.txt";
			// Update link paths using currentOS-style paths, even though they are presently stored
			// as foreign-style paths.
			string pathToRebaseFrom = MiscUtils.IsUnix ? "/origdir" : @"C:\origdir";
			string pathToRebaseTo = MiscUtils.IsUnix ? "/newdir" : @"C:\newdir";
			string rebasedPathToVerify =
				MiscUtils.IsUnix ? "/newdir/file.txt": @"C:\newdir\file.txt";

			FixHyperlinkFolder_linkTextSameAsLink_helper(origPathSetInDatabase,
				newPlatformStyleLookupPath, newPlatformStyleResultToVerify, pathToRebaseFrom,
				pathToRebaseTo, rebasedPathToVerify);
		}

		/// <summary>
		/// Helper method for FixHyperlinkFolder_linkTextSameAsLink_isUpdated and
		/// FixHyperlinkFolder_linkTextSameAsLinkDespiteChangeOfPlatform_linkTextIsUpdated.
		/// </summary>
		private void FixHyperlinkFolder_linkTextSameAsLink_helper(string origPathSetInDatabase,
			string lookupPath, string lookupResultToVerify, string pathToRebaseFrom,
			string pathToRebaseTo, string rebasedPathToVerify)
		{
			var entry = MakeEntry("kick", "hit with foot");

			var link = origPathSetInDatabase;
			var paragraph = Cache.TsStrFactory.MakeString("abc" + link + "def", Cache.DefaultAnalWs);
			var linkBeginning = "abc".Length;
			var linkEnding = linkBeginning + link.Length;
			var bldr = paragraph.GetBldr();
			var linkStyle = MakeStyle("testStyle");
			StringServices.MarkTextInBldrAsHyperlink(bldr, linkBeginning, linkEnding, link, linkStyle);
			entry.LiteralMeaning.AnalysisDefaultWritingSystem = bldr.GetString();

			var hyperlinkInfo = StringServices.GetHyperlinksInFolder(Cache, lookupPath);
			// There should be one hyperlink, and with the proper path.
			Assert.That(hyperlinkInfo, Has.Count.EqualTo(1), "Mistake in unit test");
			var relPath = lookupResultToVerify;
			VerifyHyperlinkInfo(hyperlinkInfo, entry, LexEntryTags.kflidLiteralMeaning,
				Cache.DefaultAnalWs, linkBeginning, linkEnding, relPath);
			VerifyObjData(entry.LiteralMeaning.AnalysisDefaultWritingSystem, linkBeginning,
				linkEnding, link);

			// Update links
			StringServices.FixHyperlinkFolder(hyperlinkInfo, pathToRebaseFrom, pathToRebaseTo);
			VerifyHyperlinkInfo(hyperlinkInfo, entry, LexEntryTags.kflidLiteralMeaning,
				Cache.DefaultAnalWs, linkBeginning, linkEnding, lookupResultToVerify);

			// Check that link text was updated, not just its link

			VerifyObjData(entry.LiteralMeaning.AnalysisDefaultWritingSystem, linkBeginning,
				linkBeginning + rebasedPathToVerify.Length, rebasedPathToVerify);

			var linkInfo = hyperlinkInfo.First();
			var obj = linkInfo.Object;
			var domainData = obj.Cache.DomainDataByFlid;
			ITsString updatedParagraph;
			if (linkInfo.Ws == 0)
				updatedParagraph = domainData.get_StringProp(obj.Hvo, linkInfo.Flid);
			else
				updatedParagraph = domainData.get_MultiStringAlt(obj.Hvo, linkInfo.Flid, linkInfo.Ws);
			Assert.That(updatedParagraph.Text, Is.EqualTo("abc" + rebasedPathToVerify + "def"),
				"Link text was not updated properly");
		}

		private void VerifyHyperlinkInfo(List<StringServices.LinkOccurrence> hyperlinkInfo, ICmObject obj, int flid, int ws,
			int ichMin, int ichLim, string relPath)
		{
			foreach (var linkOccurrence in hyperlinkInfo)
			{
				if (linkOccurrence.Object == obj && linkOccurrence.Flid == flid && linkOccurrence.Ws == ws &&
					linkOccurrence.IchMin == ichMin && linkOccurrence.IchLim == ichLim && linkOccurrence.RelativePath == relPath)
					return;
			}
			Assert.Fail("Did not find the link with properties " + obj.ToString() + " flid: " + flid + " ws: " + ws
				+ " ichMin: " + ichMin + " ichLim: " + ichLim + " link: " + relPath);
		}

		private void VerifyObjData(ITsString tss, int ichMin, int ichLim, string newPath)
		{
			var objData = tss.get_StringPropertyAt(ichMin, (int)FwTextPropType.ktptObjData);
			Assert.That(objData, Is.Not.Null);
			Assert.That(objData.Length, Is.GreaterThan(1));
			Assert.That(objData[0], Is.EqualTo(Convert.ToChar((int)FwObjDataTypes.kodtExternalPathName)));
			Assert.That(objData.Substring(1), Is.EqualTo(newPath));
			int ichMinActual, ichLimActual;
			tss.GetBoundsOfRun(tss.get_RunAt(ichMin), out ichMinActual, out ichLimActual);
			Assert.That(ichLimActual, Is.EqualTo(ichLim));
		}
	}
}
