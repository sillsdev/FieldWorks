// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.PlatformUtilities;
using SIL.TestUtilities;

namespace LanguageExplorerTests.Areas.TextsAndWords.Interlinear
{
	public class InterlinearExporterTestsForELAN : InterlinearExporterTestsBase
	{
		private string recGuid;

		public override void TestSetup()
		{
			base.TestSetup();
			m_choices = new InterlinLineChoices(Cache.LanguageProject, Cache.DefaultVernWs, Cache.DefaultAnalWs);
		}

		protected override void CreateTestData()
		{
			base.CreateTestData();

			Cache.LanguageProject.PeopleOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			//person not found create one and add it.
			var newPerson = Cache.ServiceLocator.GetInstance<ICmPersonFactory>().Create();
			Cache.LanguageProject.PeopleOA.PossibilitiesOS.Add(newPerson);
			newPerson.Name.set_String(Cache.DefaultVernWs, "Hiro Protaganist");

			m_text1.ContentsOA = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			m_text1.MediaFilesOA = Cache.ServiceLocator.GetInstance<ICmMediaContainerFactory>().Create();
			var recording = Cache.ServiceLocator.GetInstance<ICmMediaURIFactory>().Create();
			m_text1.MediaFilesOA.MediaURIsOC.Add(recording);
			recGuid = recording.Guid.ToString();
			IStTxtPara para = Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(m_text1.ContentsOA, 0, "special");
			para.Contents = TsStringUtils.MakeString("This is a text. It has two segments.", Cache.LanguageProject.DefaultVernacularWritingSystem.Handle);
			var seg = para.SegmentsOS[0];
			seg.BeginTimeOffset = "Timeslot 1";
			seg.EndTimeOffset = "Timeslot 2";
			seg.MediaURIRA = recording;
			seg.SpeakerRA = newPerson;
		}

		#region Overrides of InterlinearExporterTestsBase
		/// <inheritdoc />
		protected override IText CreateText()
		{
			return Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
		}
		#endregion

		[Test]
		public void ValidateMultipleTitlesAndAbbreviations()
		{
			m_text1.Name.set_String(Cache.WritingSystemFactory.GetWsFromStr("en"), "english");
			m_text1.Name.set_String(Cache.WritingSystemFactory.GetWsFromStr("fr"), "french");
			m_text1.Abbreviation.set_String(Cache.WritingSystemFactory.GetWsFromStr("en"), "english");
			m_text1.Abbreviation.set_String(Cache.WritingSystemFactory.GetWsFromStr("fr"), "french");
			var exportedDoc = ExportToXml();
			AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath("//interlinear-text/item[@type=\"title\"]", 2);
			AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath("//interlinear-text/item[@type=\"title-abbreviation\"]", 2);
		}

		[Test]
		public void ValidateMultipleSources()
		{
			m_text1.Source.set_String(Cache.WritingSystemFactory.GetWsFromStr("en"), "english");
			m_text1.Source.set_String(Cache.WritingSystemFactory.GetWsFromStr("fr"), "french");
			var exportedDoc = ExportToXml();
			AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath("//interlinear-text/item[@type=\"source\"]", 2);
		}

		[Test]
		public void ValidateMultipleComments()
		{
			m_text1.Description.set_String(Cache.WritingSystemFactory.GetWsFromStr("en"), "english");
			m_text1.Description.set_String(Cache.WritingSystemFactory.GetWsFromStr("fr"), "french");
			var exportedDoc = ExportToXml();
			AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath("//interlinear-text/item[@type=\"comment\"]", 2);
		}

		/// <summary>
		/// Create two paragraphs with two identical sentences. The first paragraph has real analyses, the second has only guesses.
		/// Validate that the guids for each paragraph, and each phrase and word annotation are unique.
		/// </summary>
		[Test]
		public void ValidateELANExport()
		{
			m_choices.Add(InterlinLineChoices.kflidWord);
			m_choices.Add(InterlinLineChoices.kflidMorphemes);
			m_choices.Add(InterlinLineChoices.kflidLexEntries);
			m_choices.Add(InterlinLineChoices.kflidLexGloss);
			m_choices.Add(InterlinLineChoices.kflidLexPos);

			// create two paragraphs with two identical sentences.
			var pa = new ParagraphAnnotator((IStTxtPara)m_text1.ContentsOA.ParagraphsOS[0]);
			pa.ReparseParagraph();
			var exportedDoc = ExportToXml("elan");

			//validate export xml against schema
			var settings = new XmlReaderSettings { ValidationType = ValidationType.Schema };

			var p = Path.Combine(FwDirectoryFinder.FlexFolder, Path.Combine("Export Templates", "Interlinear"));
			var file = Path.Combine(p, "FlexInterlinear.xsd");
			settings.Schemas.Add("", file);
			exportedDoc.Schemas.Add("", new Uri(file).AbsoluteUri);

			// The Mono implementation of XmlDocument.Validate is buggy (Xamarin Bug 8381).
			// But the other validation checks below are still worthwhile.
			if (!Platform.IsMono)
			{
				//validate export against schema.
				Assert.DoesNotThrow(() =>
				{
					exportedDoc.Validate(DontIgnore);
				});
			}

			//validate segment reference to MediaURI
			AssertThatXmlIn.Dom(exportedDoc).HasAtLeastOneMatchForXpath("//phrase[@media-file=\"" + recGuid + "\"]");
			AssertThatXmlIn.Dom(exportedDoc).HasAtLeastOneMatchForXpath("//media-files/media[@guid=\"" + recGuid + "\"]");
			AssertThatXmlIn.Dom(exportedDoc).HasAtLeastOneMatchForXpath("//phrase[@speaker=\"Hiro Protaganist\"]");

			//validate guid present on Interlin-text, phrase, word, and morpheme
			AssertThatXmlIn.Dom(exportedDoc).HasNoMatchForXpath("document/interlinear-text[not(@guid)]");
			AssertThatXmlIn.Dom(exportedDoc).HasNoMatchForXpath("//phrase[not(@guid)]");
			AssertThatXmlIn.Dom(exportedDoc).HasNoMatchForXpath("//word[not(@guid) and not(item[@type=\"punct\"])]");
			AssertThatXmlIn.Dom(exportedDoc).HasNoMatchForXpath("//morpheme[not(@guid)]");
		}

		private void DontIgnore(object sender, ValidationEventArgs e)
		{
			throw e.Exception;
		}
	}
}