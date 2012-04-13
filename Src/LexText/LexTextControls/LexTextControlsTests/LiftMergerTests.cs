// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: LiftMergerTests1.cs
// Responsibility: mcconnel
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using NUnit.Framework;
using Palaso.Lift.Parsing;
using Palaso.WritingSystems;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.LexText.Controls;
using System.IO;
using SIL.FieldWorks.FDO.FDOTests;
using Palaso.Lift.Migration;
using SIL.Utils;
using System.Xml.Linq;
using System.Xml.XPath;

namespace LexTextControlsTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test the LIFT import functionality provided by the FlexLiftMerger class in conjunction
	/// with the Palaso.Lift library.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class LiftMergerTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private string MockProjectFolder { get; set; }
		private string MockLinkedFilesFolder { get; set; }
		private int m_audioWsCode;


		private Dictionary<String, int> m_customFieldEntryIds = new Dictionary<String, int>();
		private Dictionary<String, int> m_customFieldSenseIds = new Dictionary<String, int>();
		private Dictionary<String, int> m_customFieldAllomorphsIds = new Dictionary<String, int>();
		private Dictionary<String, int> m_customFieldExampleSentencesIds = new Dictionary<String, int>();

		public override void TestSetup()
		{
			base.TestSetup();
			Cache.LangProject.LexDbOA.ReferencesOA =
				Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().
					Create();
			var mockProjectName = "xxyyzProjectFolderForLIFTImport";
			MockProjectFolder = Path.Combine(Path.GetTempPath(), mockProjectName);
			var mockProjectPath = Path.Combine(MockProjectFolder, mockProjectName + ".fwdata");
			MockLinkedFilesFolder = Path.Combine(MockProjectFolder, DirectoryFinder.ksLinkedFilesDir);
			if (Directory.Exists(MockLinkedFilesFolder))
				Directory.Delete(MockLinkedFilesFolder, true);
			Directory.CreateDirectory(MockLinkedFilesFolder);
			Cache.LangProject.LinkedFilesRootDir = MockLinkedFilesFolder;

			var writingSystemManager = Cache.ServiceLocator.WritingSystemManager;
			var languageSubtag =
				Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.LanguageSubtag;
			//var voiceTag = RFC5646Tag.RFC5646TagForVoiceWritingSystem(languageSubtag.Name, "");
			var audioWs = writingSystemManager.Create(languageSubtag,
				LangTagUtils.GetScriptSubtag("Zxxx"), null, LangTagUtils.GetVariantSubtag("audio"));
			IWritingSystem existingAudioWs;
			if (writingSystemManager.TryGet(audioWs.Id, out existingAudioWs))
			{
				m_audioWsCode = existingAudioWs.Handle;
			}
			else
			{
				((WritingSystemDefinition) audioWs).IsVoice = true;
				// should already be so? Make sure.
				writingSystemManager.Set(audioWs); // gives it a handle
				m_audioWsCode = audioWs.Handle;
			}
		}

		private static string LiftFolder { get; set; }

		private static string CreateInputFile(IList<string> data)
		{
			LiftFolder = Path.Combine(Path.GetTempPath(), "xxyyTestLIFTImport");
			if (Directory.Exists(LiftFolder))
				Directory.Delete(LiftFolder, true);
			Directory.CreateDirectory(LiftFolder);
			var path = Path.Combine(LiftFolder, "LiftTest.lift");
			CreateLiftInputFile(path, data);
			return path;
		}

		private static string CreateInputRangesFile(IList<string> data)
		{
			LiftFolder = Path.Combine(Path.GetTempPath(), "xxyyTestLIFTImport");
			Assert.True(Directory.Exists(LiftFolder));
			var path = Path.Combine(LiftFolder, "LiftTest.lift-ranges");
			CreateLiftInputFile(path, data);
			return path;
		}

		private static void CreateLiftInputFile(string path, IList<string> data)
		{
			if (File.Exists(path))
				File.Delete(path);
			using (var wrtr = File.CreateText(path))
			{
				for (var i = 0; i < data.Count; ++i)
					wrtr.WriteLine(data[i]);
				wrtr.Close();
			}
		}

		private string TryImport(string sOrigFile, int expectedCount)
		{
			return TryImport(sOrigFile, null, FlexLiftMerger.MergeStyle.MsKeepBoth, expectedCount);
		}

		private string TryImportWithRanges(string sOrigFile, string sOrigRangesFile, int expectedCount)
		{
			return TryImport(sOrigFile, sOrigRangesFile, FlexLiftMerger.MergeStyle.MsKeepBoth, expectedCount);
		}

		private string TryImport(string sOrigFile, string sOrigRangesFile, FlexLiftMerger.MergeStyle mergeStyle, int expectedCount)
		{
			string logfile = null;
			try
			{
				IProgress progressDlg = new DummyProgressDlg();
				var fMigrationNeeded = Migrator.IsMigrationNeeded(sOrigFile);
				var sFilename = fMigrationNeeded
									? Migrator.MigrateToLatestVersion(sOrigFile)
									: sOrigFile;
				var flexImporter = new FlexLiftMerger(Cache, mergeStyle, true);
				var parser = new LiftParser<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>(flexImporter);
				flexImporter.LiftFile = sOrigFile;

				//The following are the calls to import the Ranges file and then the Data file.
				if (!String.IsNullOrEmpty(sOrigRangesFile))
					flexImporter.LoadLiftRanges(sOrigRangesFile);
				var cEntries = parser.ReadLiftFile(sFilename);

				Assert.AreEqual(expectedCount, cEntries);
				if (fMigrationNeeded)
					File.Delete(sFilename);
				flexImporter.ProcessPendingRelations(progressDlg);
				logfile = flexImporter.DisplayNewListItems(sOrigFile, cEntries);
			}
			catch (Exception error)
			{
				return null;
			}
			return logfile;
		}

		private static void CreateDummyFile(string folder, string filename)
		{
			CreateDummyFile(Path.Combine(Path.Combine(LiftFolder, folder), filename));
		}

		private static string CreateDummyFile(string path)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.Delete(path);
			using (var wrtr = File.CreateText(path))
			{
				wrtr.WriteLine("This is a dummy file used in testing LIFT import");
				wrtr.Close();
			}
			return path;
		}

		static private readonly string[] s_LiftData1 = new[]
		{
			"<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
			"<lift producer=\"SIL.FLEx 7.0.1.40602\" version=\"0.13\">",
			"<header>",
			"<ranges/>",
			"<fields/>",
			"</header>",
			"<entry dateCreated=\"2011-03-01T18:09:46Z\" dateModified=\"2011-03-01T18:30:07Z\" guid=\"ecfbe958-36a1-4b82-bb69-ca5210355400\" id=\"hombre_ecfbe958-36a1-4b82-bb69-ca5210355400\">",
			"<lexical-unit>",
			"<form lang=\"es\"><text>hombre</text></form>",
			"<form lang=\"fr-Zxxx-x-AUDIO\"><text>hombre634407358826681759.wav</text></form>",
			"<form lang=\"Fr-Tech 30Oct\"><text>form in bad WS</text></form>",
			"</lexical-unit>",
			"<trait name=\"morph-type\" value=\"root\"></trait>",
			"<pronunciation>",
			"<form lang=\"fr\"><text>ombre</text></form>",
			"<media href=\"Sleep Away.mp3\">",
			"</media>",
			"</pronunciation>",
			"<sense id=\"hombre_f63f1ccf-3d50-417e-8024-035d999d48bc\">",
			"<grammatical-info value=\"Noun\">",
			"</grammatical-info>",
			"<gloss lang=\"en\"><text>man</text></gloss>",
			"<definition>",
			"<form lang=\"en\"><text>male adult human <span href=\"file://others/SomeFile.txt\" class=\"Hyperlink\">link</span></text></form>",
			"<form lang=\"fr-Zxxx-x-AUDIO\"><text>male adult634407358826681760.wav</text></form>",
			"</definition>",
			"<illustration href=\"Desert.jpg\">",
			"<label>",
			"<form lang=\"fr\"><text>Desert</text></form>",
			"</label>",
			"</illustration>",
			"<illustration href=\"subfolder/MyPic.jpg\">",
			"<label>",
			"<form lang=\"fr\"><text>My picture</text></form>",
			"</label>",
			"</illustration>",
			"<trait name=\"semantic-domain-ddp4\" value=\"2.6.5.1 Man\"></trait>",
			"<trait name=\"semantic-domain-ddp4\" value=\"2.6.4.4 Adult\"></trait>",
			"</sense>",
			"</entry>",
			"<entry dateCreated=\"2011-03-01T18:13:41Z\" dateModified=\"2011-03-01T18:29:55Z\" guid=\"766aaee2-34b6-4e28-a883-5c2186125a2f\" id=\"mujer_766aaee2-34b6-4e28-a883-5c2186125a2f\">",
			"<lexical-unit>",
			"<form lang=\"es\"><text>mujer</text></form>",
			"</lexical-unit>",
			"<trait name=\"morph-type\" value=\"root\"></trait>",
			"<sense id=\"mujer_cf6680cc-faeb-4bd2-90ec-0be5dcdcc6af\">",
			"<grammatical-info value=\"Noun\">",
			"</grammatical-info>",
			"<gloss lang=\"en\"><text>woman</text></gloss>",
			"<definition>",
			"<form lang=\"en\"><text>female adult human</text></form>",
			"</definition>",
			"<trait name=\"semantic-domain-ddp4\" value=\"2.6.5.2 Woman\"></trait>",
			"<trait name=\"semantic-domain-ddp4\" value=\"2.6.4.4 Adult\"></trait>",
			"</sense>",
			"</entry>",
			"<entry dateCreated=\"2011-03-01T18:17:06Z\" dateModified=\"2011-03-01T18:29:29Z\" guid=\"1767c76d-e35f-495a-9203-6b31fd82ad72\" id=\"niño_1767c76d-e35f-495a-9203-6b31fd82ad72\">",
			"<lexical-unit>",
			"<form lang=\"es\"><text>niño</text></form>",
			"</lexical-unit>",
			"<trait name=\"morph-type\" value=\"stem\"></trait>",
			"<sense id=\"niño_04545fa2-e24c-446e-928c-2a13710359b3\">",
			"<grammatical-info value=\"Noun\">",
			"</grammatical-info>",
			"<gloss lang=\"en\"><text>boy</text></gloss>",
			"<definition>",
			"<form lang=\"en\"><text>male human child</text></form>",
			"</definition>",
			"<trait name=\"semantic-domain-ddp4\" value=\"2.6.4.2 Child\"></trait>",
			"<trait name=\"semantic-domain-ddp4\" value=\"2.6.5.1 Man\"></trait>",
			"</sense>",
			"</entry>",
			"<entry dateCreated=\"2011-03-01T18:17:54Z\" dateModified=\"2011-03-01T18:29:36Z\" guid=\"185c528d-aeb1-4e32-8aac-2420322020d2\" id=\"niña_185c528d-aeb1-4e32-8aac-2420322020d2\">",
			"<lexical-unit>",
			"<form lang=\"es\"><text>niña</text></form>",
			"</lexical-unit>",
			"<trait name=\"morph-type\" value=\"stem\"></trait>",
			"<sense id=\"niña_db9d3790-2f5c-4d99-b9fc-3b21b47fa505\">",
			"<grammatical-info value=\"Noun\">",
			"</grammatical-info>",
			"<gloss lang=\"en\"><text>girl</text></gloss>",
			"<definition>",
			"<form lang=\"en\"><text>female human child</text></form>",
			"</definition>",
			"<trait name=\"semantic-domain-ddp4\" value=\"2.6.4.2 Child\"></trait>",
			"<trait name=\"semantic-domain-ddp4\" value=\"2.6.5.2 Woman\"></trait>",
			"</sense>",
			"</entry>",
			"</lift>"
		};

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// First test of LIFT import: four simple entries.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestLiftImport1()
		{
			SetWritingSystems("es");

			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			Assert.AreEqual(0, repoEntry.Count);
			Assert.AreEqual(0, repoSense.Count);

			var sOrigFile = CreateInputFile(s_LiftData1);
			CreateDummyFile("pictures", "Desert.jpg");
			var myPicRelativePath = Path.Combine("subfolder","MyPic.jpg");
			CreateDummyFile("pictures", myPicRelativePath);
			CreateDummyFile("audio", "Sleep Away.mp3");
			CreateDummyFile("audio", "hombre634407358826681759.wav");
			CreateDummyFile("audio", "male adult634407358826681760.wav");
			CreateDummyFile("others", "SomeFile.txt");

			var logFile = TryImport(sOrigFile, 4);
			File.Delete(sOrigFile);
			Assert.IsNotNull(logFile);
			File.Delete(logFile);
			Assert.AreEqual(4, repoEntry.Count);
			Assert.AreEqual(4, repoSense.Count);

			ILexEntry entry;
			Assert.IsTrue(repoEntry.TryGetObject(new Guid("ecfbe958-36a1-4b82-bb69-ca5210355400"), out entry));
			Assert.AreEqual(1, entry.SensesOS.Count);
			var sense0 = entry.SensesOS[0];
			Assert.AreEqual(sense0.Guid, new Guid("f63f1ccf-3d50-417e-8024-035d999d48bc"));
			Assert.IsNotNull(entry.LexemeFormOA);
			Assert.IsNotNull(entry.LexemeFormOA.MorphTypeRA);
			Assert.AreEqual("root", entry.LexemeFormOA.MorphTypeRA.Name.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("hombre", entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual("hombre634407358826681759.wav", entry.LexemeFormOA.Form.get_String(m_audioWsCode).Text);
			Assert.IsNotNull(sense0.MorphoSyntaxAnalysisRA as IMoStemMsa);
			// ReSharper disable PossibleNullReferenceException
			Assert.IsNotNull((sense0.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA);
			Assert.AreEqual("Noun", (sense0.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA.Name.AnalysisDefaultWritingSystem.Text);
			// ReSharper restore PossibleNullReferenceException
			Assert.AreEqual("man", sense0.Gloss.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("male adult human link", sense0.Definition.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("male adult634407358826681760.wav", sense0.Definition.get_String(m_audioWsCode).Text);
			Assert.AreEqual(2, sense0.SemanticDomainsRC.Count);
			foreach (var sem in sense0.SemanticDomainsRC)
			{
				if (sem.Abbreviation.AnalysisDefaultWritingSystem.Text == "2.6.4.4 Adult")
				{
					Assert.AreEqual("2.6.4.4 Adult", sem.Name.AnalysisDefaultWritingSystem.Text);
				}
				else
				{
					Assert.AreEqual("2.6.5.1 Man", sem.Abbreviation.AnalysisDefaultWritingSystem.Text);
					Assert.AreEqual("2.6.5.1 Man", sem.Name.AnalysisDefaultWritingSystem.Text);
				}
			}

			Assert.That(sense0.PicturesOS.Count, Is.EqualTo(2));
			Assert.That(sense0.PicturesOS[0].PictureFileRA.InternalPath, Is.EqualTo(Path.Combine(DirectoryFinder.ksPicturesDir, "Desert.jpg")));
			Assert.That(sense0.PicturesOS[1].PictureFileRA.InternalPath, Is.EqualTo(Path.Combine(DirectoryFinder.ksPicturesDir, myPicRelativePath)));
			VerifyLinkedFileExists(DirectoryFinder.ksPicturesDir, "Desert.jpg");
			VerifyLinkedFileExists(DirectoryFinder.ksPicturesDir, myPicRelativePath);

			Assert.That(entry.PronunciationsOS.Count, Is.EqualTo(1));
			Assert.That(entry.PronunciationsOS[0].MediaFilesOS[0].MediaFileRA.InternalPath,
				Is.EqualTo(Path.Combine(DirectoryFinder.ksMediaDir, "Sleep Away.mp3")));
			VerifyLinkedFileExists(DirectoryFinder.ksMediaDir, "Sleep Away.mp3");
			VerifyLinkedFileExists(DirectoryFinder.ksMediaDir, "hombre634407358826681759.wav");
			VerifyLinkedFileExists(DirectoryFinder.ksMediaDir, "male adult634407358826681760.wav");
			VerifyLinkedFileExists(DirectoryFinder.ksOtherLinkedFilesDir, "SomeFile.txt");

			Assert.IsTrue(repoEntry.TryGetObject(new Guid("766aaee2-34b6-4e28-a883-5c2186125a2f"), out entry));
			Assert.AreEqual(1, entry.SensesOS.Count);
			sense0 = entry.SensesOS[0];
			Assert.AreEqual(sense0.Guid, new Guid("cf6680cc-faeb-4bd2-90ec-0be5dcdcc6af"));
			Assert.IsNotNull(entry.LexemeFormOA);
			Assert.IsNotNull(entry.LexemeFormOA.MorphTypeRA);
			Assert.AreEqual("root", entry.LexemeFormOA.MorphTypeRA.Name.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("mujer", entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text);
			Assert.IsNotNull(sense0.MorphoSyntaxAnalysisRA as IMoStemMsa);
			// ReSharper disable PossibleNullReferenceException
			Assert.IsNotNull((sense0.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA);
			Assert.AreEqual("Noun", (sense0.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA.Name.AnalysisDefaultWritingSystem.Text);
			// ReSharper restore PossibleNullReferenceException
			Assert.AreEqual("woman", sense0.Gloss.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("female adult human", sense0.Definition.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual(2, sense0.SemanticDomainsRC.Count);
			foreach (var sem in sense0.SemanticDomainsRC)
			{
				if (sem.Abbreviation.AnalysisDefaultWritingSystem.Text == "2.6.4.4 Adult")
				{
					Assert.AreEqual("2.6.4.4 Adult", sem.Name.AnalysisDefaultWritingSystem.Text);
				}
				else
				{
					Assert.AreEqual("2.6.5.2 Woman", sem.Abbreviation.AnalysisDefaultWritingSystem.Text);
					Assert.AreEqual("2.6.5.2 Woman", sem.Name.AnalysisDefaultWritingSystem.Text);
				}
			}

			Assert.IsTrue(repoEntry.TryGetObject(new Guid("1767c76d-e35f-495a-9203-6b31fd82ad72"), out entry));
			Assert.AreEqual(1, entry.SensesOS.Count);
			sense0 = entry.SensesOS[0];
			Assert.AreEqual(sense0.Guid, new Guid("04545fa2-e24c-446e-928c-2a13710359b3"));
			Assert.IsNotNull(entry.LexemeFormOA);
			Assert.IsNotNull(entry.LexemeFormOA.MorphTypeRA);
			Assert.AreEqual("stem", entry.LexemeFormOA.MorphTypeRA.Name.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("niño".Normalize(NormalizationForm.FormD), entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text);
			Assert.IsNotNull(sense0.MorphoSyntaxAnalysisRA as IMoStemMsa);
			// ReSharper disable PossibleNullReferenceException
			Assert.IsNotNull((sense0.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA);
			Assert.AreEqual("Noun", (sense0.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA.Name.AnalysisDefaultWritingSystem.Text);
			// ReSharper restore PossibleNullReferenceException
			Assert.AreEqual("boy", sense0.Gloss.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("male human child", sense0.Definition.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual(2, sense0.SemanticDomainsRC.Count);
			foreach (var sem in sense0.SemanticDomainsRC)
			{
				if (sem.Abbreviation.AnalysisDefaultWritingSystem.Text == "2.6.4.2 Child")
				{
					Assert.AreEqual("2.6.4.2 Child", sem.Name.AnalysisDefaultWritingSystem.Text);
				}
				else
				{
					Assert.AreEqual("2.6.5.1 Man", sem.Abbreviation.AnalysisDefaultWritingSystem.Text);
					Assert.AreEqual("2.6.5.1 Man", sem.Name.AnalysisDefaultWritingSystem.Text);
				}
			}

			Assert.IsTrue(repoEntry.TryGetObject(new Guid("185c528d-aeb1-4e32-8aac-2420322020d2"), out entry));
			Assert.AreEqual(1, entry.SensesOS.Count);
			sense0 = entry.SensesOS[0];
			Assert.AreEqual(sense0.Guid, new Guid("db9d3790-2f5c-4d99-b9fc-3b21b47fa505"));
			Assert.IsNotNull(entry.LexemeFormOA);
			Assert.IsNotNull(entry.LexemeFormOA.MorphTypeRA);
			Assert.AreEqual("stem", entry.LexemeFormOA.MorphTypeRA.Name.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("niña".Normalize(NormalizationForm.FormD), entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text);
			Assert.IsNotNull(sense0.MorphoSyntaxAnalysisRA as IMoStemMsa);
			// ReSharper disable PossibleNullReferenceException
			Assert.IsNotNull((sense0.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA);
			Assert.AreEqual("Noun", (sense0.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA.Name.AnalysisDefaultWritingSystem.Text);
			// ReSharper restore PossibleNullReferenceException
			Assert.AreEqual("girl", sense0.Gloss.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("female human child", sense0.Definition.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual(2, sense0.SemanticDomainsRC.Count);
			foreach (var sem in sense0.SemanticDomainsRC)
			{
				if (sem.Abbreviation.AnalysisDefaultWritingSystem.Text == "2.6.4.2 Child")
				{
					Assert.AreEqual("2.6.4.2 Child", sem.Name.AnalysisDefaultWritingSystem.Text);
				}
				else
				{
					Assert.AreEqual("2.6.5.2 Woman", sem.Abbreviation.AnalysisDefaultWritingSystem.Text);
					Assert.AreEqual("2.6.5.2 Woman", sem.Name.AnalysisDefaultWritingSystem.Text);
				}
			}
		}

		private void SetWritingSystems(string vern)
		{
			Cache.LangProject.VernWss = vern;
			Cache.LangProject.CurVernWss = vern;
			Cache.LangProject.AnalysisWss = "en";
			Cache.LangProject.CurAnalysisWss = "en";
		}

		void VerifyLinkedFileExists(string folder, string filename)
		{
			Assert.That(File.Exists(Path.Combine(Path.Combine(MockLinkedFilesFolder, folder), filename)), Is.True);
		}

		static private readonly string[] s_LiftData2 = new[]
		{
			"<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
			"<lift producer=\"SIL.FLEx 7.0.1.40602\" version=\"0.13\">",
			"<header>",
			"<ranges/>",
			"<fields/>",
			"</header>",
			"<entry dateCreated=\"2011-03-01T22:26:47Z\" dateModified=\"2011-03-01T22:41:41Z\" guid=\"69ccc807-f3d1-44cb-b79a-e8d416b0d7c1\" id=\"house_69ccc807-f3d1-44cb-b79a-e8d416b0d7c1\">",
			"<lexical-unit>",
			"<form lang=\"fr\"><text>house</text></form>",
			"</lexical-unit>",
			"<trait name=\"morph-type\" value=\"stem\"></trait>",
			"<variant>",
			"<form lang=\"fr\"><text>ouse</text></form>",
			"<trait name=\"environment\" value=\"/[C]_\"></trait>",
			"<trait name=\"morph-type\" value=\"stem\"></trait>",
			"</variant>",
			"<sense id=\"house_f722992a-cfdc-41ec-9c46-f927f02d68ef\">",
			"<grammatical-info value=\"Noun\">",
			"</grammatical-info>",
			"<gloss lang=\"en\"><text>house</text></gloss>",
			"</sense>",
			"</entry>",
			"<entry dateCreated=\"2011-03-01T22:27:13Z\" dateModified=\"2011-03-01T22:27:13Z\" guid=\"67940acb-9252-4941-bfb3-3ace4e1bda7a\" id=\"green_67940acb-9252-4941-bfb3-3ace4e1bda7a\">",
			"<lexical-unit>",
			"<form lang=\"fr\"><text>green</text></form>",
			"</lexical-unit>",
			"<trait name=\"morph-type\" value=\"stem\"></trait>",
			"<sense id=\"green_d3ed09c5-8757-41cb-849d-a24e6200caf4\">",
			"<grammatical-info value=\"Adjective\">",
			"</grammatical-info>",
			"<gloss lang=\"en\"><text>green</text></gloss>",
			"</sense>",
			"</entry>",
			"<entry dateCreated=\"2011-03-01T22:27:46Z\" dateModified=\"2011-03-01T22:28:00Z\" guid=\"67113a7f-e448-43e7-87cf-6d3a46ee10ec\" id=\"greenhouse_67113a7f-e448-43e7-87cf-6d3a46ee10ec\">",
			"<lexical-unit>",
			"<form lang=\"fr\"><text>greenhouse</text></form>",
			"</lexical-unit>",
			"<trait name=\"morph-type\" value=\"stem\"></trait>",
			"<relation type=\"_component-lexeme\" ref=\"green_67940acb-9252-4941-bfb3-3ace4e1bda7a\" order=\"0\">",
			"<trait name=\"complex-form-type\" value=\"Compound\"></trait>",
			"<trait name=\"is-primary\" value=\"true\"/>",
			"</relation>",
			"<relation type=\"_component-lexeme\" ref=\"house_69ccc807-f3d1-44cb-b79a-e8d416b0d7c1\" order=\"1\">",
			"<trait name=\"complex-form-type\" value=\"Compound\"></trait>",
			"<trait name=\"is-primary\" value=\"true\"/>",
			"</relation>",
			"<relation type=\"BaseForm\" ref=\"green_67940acb-9252-4941-bfb3-3ace4e1bda7a\">",
			"</relation>",
			"<sense id=\"greenhouse_cf2ac6f4-01d8-47ed-9b41-25b6e727097f\">",
			"<grammatical-info value=\"Noun\">",
			"</grammatical-info>",
			"</sense>",
			"</entry>",
			"<entry dateCreated=\"2011-03-01T22:28:56Z\" dateModified=\"2011-03-01T22:29:07Z\" guid=\"58f978d2-2cb2-4506-9a47-63c5454f0065\" id=\"hoose_58f978d2-2cb2-4506-9a47-63c5454f0065\">",
			"<lexical-unit>",
			"<form lang=\"fr\"><text>hoose</text></form>",
			"</lexical-unit>",
			"<trait name=\"morph-type\" value=\"stem\"></trait>",
			"<relation type=\"_component-lexeme\" ref=\"house_69ccc807-f3d1-44cb-b79a-e8d416b0d7c1\">",
			"<trait name=\"variant-type\" value=\"Dialectal Variant\"></trait>",
			"</relation>",
			"</entry>",
			"</lift>"
		};

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Second test of LIFT import: more complex and variant entries.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestLiftImport2()
		{
			Assert.AreEqual("en", Cache.LangProject.CurAnalysisWss);
			Assert.AreEqual("fr", Cache.LangProject.CurVernWss);
			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			Assert.AreEqual(0, repoEntry.Count);
			Assert.AreEqual(0, repoSense.Count);

			var sOrigFile = CreateInputFile(s_LiftData2);
			var logFile = TryImport(sOrigFile, 4);
			File.Delete(sOrigFile);
			Assert.IsNotNull(logFile);
			File.Delete(logFile);
			Assert.AreEqual(4, repoEntry.Count);
			Assert.AreEqual(3, repoSense.Count);

			ILexEntry entry;
			Assert.IsTrue(repoEntry.TryGetObject(new Guid("69ccc807-f3d1-44cb-b79a-e8d416b0d7c1"), out entry));
			Assert.AreEqual(1, entry.SensesOS.Count);
			var sense = entry.SensesOS[0];
			Assert.AreEqual(sense.Guid, new Guid("f722992a-cfdc-41ec-9c46-f927f02d68ef"));
			Assert.IsNotNull(entry.LexemeFormOA);
			Assert.IsNotNull(entry.LexemeFormOA.MorphTypeRA);
			Assert.AreEqual("stem", entry.LexemeFormOA.MorphTypeRA.Name.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("house", entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text);
			Assert.IsNotNull(sense.MorphoSyntaxAnalysisRA as IMoStemMsa);
			// ReSharper disable PossibleNullReferenceException
			Assert.IsNotNull((sense.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA);
			Assert.AreEqual("Noun", (sense.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA.Name.AnalysisDefaultWritingSystem.Text);
			// ReSharper restore PossibleNullReferenceException
			Assert.AreEqual("house", sense.Gloss.AnalysisDefaultWritingSystem.Text);
			Assert.IsNull(sense.Definition.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual(0, sense.SemanticDomainsRC.Count);
			Assert.AreEqual(1, entry.AlternateFormsOS.Count);
			var allo = entry.AlternateFormsOS[0] as IMoStemAllomorph;
			Assert.IsNotNull(allo);
			Assert.AreEqual("ouse", allo.Form.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual("stem", allo.MorphTypeRA.Name.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual(1, allo.PhoneEnvRC.Count);
			IPhEnvironment env = null;
			foreach (var x in allo.PhoneEnvRC)
				env = x;
			Assert.IsNotNull(env);
			Assert.AreEqual("/[C]_", env.StringRepresentation.Text);
			Assert.AreEqual(0, entry.EntryRefsOS.Count);

			Assert.IsTrue(repoEntry.TryGetObject(new Guid("67940acb-9252-4941-bfb3-3ace4e1bda7a"), out entry));
			Assert.AreEqual(1, entry.SensesOS.Count);
			sense = entry.SensesOS[0];
			Assert.AreEqual(sense.Guid, new Guid("d3ed09c5-8757-41cb-849d-a24e6200caf4"));
			Assert.IsNotNull(entry.LexemeFormOA);
			Assert.IsNotNull(entry.LexemeFormOA.MorphTypeRA);
			Assert.AreEqual("stem", entry.LexemeFormOA.MorphTypeRA.Name.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("green", entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text);
			Assert.IsNotNull(sense.MorphoSyntaxAnalysisRA as IMoStemMsa);
			// ReSharper disable PossibleNullReferenceException
			Assert.IsNotNull((sense.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA);
			Assert.AreEqual("Adjective", (sense.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA.Name.AnalysisDefaultWritingSystem.Text);
			// ReSharper restore PossibleNullReferenceException
			Assert.AreEqual("green", sense.Gloss.AnalysisDefaultWritingSystem.Text);
			Assert.IsNull(sense.Definition.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual(0, sense.SemanticDomainsRC.Count);
			Assert.AreEqual(0, entry.EntryRefsOS.Count);

			Assert.IsTrue(repoEntry.TryGetObject(new Guid("67113a7f-e448-43e7-87cf-6d3a46ee10ec"), out entry));
			Assert.AreEqual(1, entry.SensesOS.Count);
			sense = entry.SensesOS[0];
			Assert.AreEqual(sense.Guid, new Guid("cf2ac6f4-01d8-47ed-9b41-25b6e727097f"));
			Assert.IsNotNull(entry.LexemeFormOA);
			Assert.IsNotNull(entry.LexemeFormOA.MorphTypeRA);
			Assert.AreEqual("stem", entry.LexemeFormOA.MorphTypeRA.Name.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("greenhouse", entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text);
			Assert.IsNotNull(sense.MorphoSyntaxAnalysisRA as IMoStemMsa);
			// ReSharper disable PossibleNullReferenceException
			Assert.IsNotNull((sense.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA);
			Assert.AreEqual("Noun", (sense.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA.Name.AnalysisDefaultWritingSystem.Text);
			// ReSharper restore PossibleNullReferenceException
			Assert.IsNull(sense.Gloss.AnalysisDefaultWritingSystem.Text);
			Assert.IsNull(sense.Definition.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual(0, sense.SemanticDomainsRC.Count);
			Assert.AreEqual(2, entry.EntryRefsOS.Count);
			var lexref = entry.EntryRefsOS[0];
			Assert.AreEqual(LexEntryRefTags.krtComplexForm, lexref.RefType);
			Assert.AreEqual(1, lexref.ComplexEntryTypesRS.Count);
			var reftype = lexref.ComplexEntryTypesRS[0];
			Assert.AreEqual("Compound", reftype.Name.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual(0, lexref.VariantEntryTypesRS.Count);
			Assert.AreEqual(2, lexref.ComponentLexemesRS.Count);
			Assert.AreEqual(new Guid("67940acb-9252-4941-bfb3-3ace4e1bda7a"), lexref.ComponentLexemesRS[0].Guid);
			Assert.AreEqual(new Guid("69ccc807-f3d1-44cb-b79a-e8d416b0d7c1"), lexref.ComponentLexemesRS[1].Guid);
			Assert.AreEqual(2, lexref.PrimaryLexemesRS.Count);
			Assert.AreEqual(new Guid("67940acb-9252-4941-bfb3-3ace4e1bda7a"), lexref.PrimaryLexemesRS[0].Guid);
			Assert.AreEqual(new Guid("69ccc807-f3d1-44cb-b79a-e8d416b0d7c1"), lexref.PrimaryLexemesRS[1].Guid);

			lexref = entry.EntryRefsOS[1];
			Assert.AreEqual(LexEntryRefTags.krtComplexForm, lexref.RefType);
			Assert.AreEqual(1, lexref.ComplexEntryTypesRS.Count);
			reftype = lexref.ComplexEntryTypesRS[0];
			Assert.AreEqual("BaseForm", reftype.Name.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual(0, lexref.VariantEntryTypesRS.Count);
			Assert.AreEqual(1, lexref.ComponentLexemesRS.Count);
			Assert.AreEqual(new Guid("67940acb-9252-4941-bfb3-3ace4e1bda7a"), lexref.ComponentLexemesRS[0].Guid);
			Assert.AreEqual(1, lexref.PrimaryLexemesRS.Count);
			Assert.AreEqual(new Guid("67940acb-9252-4941-bfb3-3ace4e1bda7a"), lexref.PrimaryLexemesRS[0].Guid);

			Assert.IsTrue(repoEntry.TryGetObject(new Guid("58f978d2-2cb2-4506-9a47-63c5454f0065"), out entry));
			Assert.AreEqual(0, entry.SensesOS.Count);
			Assert.IsNotNull(entry.LexemeFormOA);
			Assert.IsNotNull(entry.LexemeFormOA.MorphTypeRA);
			Assert.AreEqual("stem", entry.LexemeFormOA.MorphTypeRA.Name.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("hoose", entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(1, entry.EntryRefsOS.Count);
			lexref = entry.EntryRefsOS[0];
			Assert.AreEqual(LexEntryRefTags.krtVariant, lexref.RefType);
			Assert.AreEqual(1, lexref.VariantEntryTypesRS.Count);
			reftype = lexref.VariantEntryTypesRS[0];
			Assert.AreEqual("Dialectal Variant", reftype.Name.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual(0, lexref.ComplexEntryTypesRS.Count);
			Assert.AreEqual(1, lexref.ComponentLexemesRS.Count);
			Assert.AreEqual(new Guid("69ccc807-f3d1-44cb-b79a-e8d416b0d7c1"), lexref.ComponentLexemesRS[0].Guid);
			Assert.AreEqual(0, lexref.PrimaryLexemesRS.Count);
		}

		static private readonly string[] s_LiftData3a = new[]
		{
			"<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
			"<lift producer=\"SIL.FLEx 7.0.1.40602\" version=\"0.13\">",
			"<header>",
			"<ranges/>",
			"<fields/>",
			"</header>",
			"<entry dateCreated=\"2011-03-01T22:26:47Z\" dateModified=\"2011-03-01T22:41:41Z\" guid=\"69ccc807-f3d1-44cb-b79a-e8d416b0d7c1\" id=\"house_69ccc807-f3d1-44cb-b79a-e8d416b0d7c1\">",
			"<lexical-unit>",
			"<form lang=\"fr\"><text>house</text></form>",
			"</lexical-unit>",
			"<trait name=\"morph-type\" value=\"stem\"></trait>",
			"<variant>",
			"<trait name=\"paradigm\" value=\"sing\"/>",
			"<form lang=\"fr\"><text>ouse</text></form>",
			"</variant>",
			"<sense id=\"house_f722992a-cfdc-41ec-9c46-f927f02d68ef\">",
			"<grammatical-info value=\"Noun\">",
			"</grammatical-info>",
			"<gloss lang=\"en\"><text>house</text></gloss>",
			"</sense>",
			"</entry>",
			"<entry dateCreated=\"2011-03-01T22:27:13Z\" dateModified=\"2011-03-01T22:27:13Z\" guid=\"67940acb-9252-4941-bfb3-3ace4e1bda7a\" id=\"green_67940acb-9252-4941-bfb3-3ace4e1bda7a\">",
			"<lexical-unit>",
			"<form lang=\"fr\"><text>green</text></form>",
			"</lexical-unit>",
			"<trait name=\"morph-type\" value=\"stem\"></trait>",
			"<sense id=\"green_d3ed09c5-8757-41cb-849d-a24e6200caf4\">",
			"<grammatical-info value=\"Adjective\">",
			"</grammatical-info>",
			"<gloss lang=\"en\"><text>green</text></gloss>",
			"</sense>",
			"</entry>"
		};

		private const string sLiftData3b =
			"<entry dateCreated=\"2011-03-01T22:27:46Z\" dateModified=\"{0}\" guid=\"67113a7f-e448-43e7-87cf-6d3a46ee10ec\" id=\"greenhouse_67113a7f-e448-43e7-87cf-6d3a46ee10ec\">";
		static private readonly string[] s_LiftData3c = new[]
		{
			"<lexical-unit>",
			"<form lang=\"fr\"><text>greenhouse</text></form>",
			"</lexical-unit>",
			"<trait name=\"morph-type\" value=\"stem\"></trait>",
			"<relation type=\"_component-lexeme\" ref=\"green_67940acb-9252-4941-bfb3-3ace4e1bda7a\" order=\"0\">",
			"</relation>",
			"<relation type=\"_component-lexeme\" ref=\"house_69ccc807-f3d1-44cb-b79a-e8d416b0d7c1\" order=\"1\">",
			"</relation>",
			"<sense id=\"greenhouse_cf2ac6f4-01d8-47ed-9b41-25b6e727097f\">",
			"<grammatical-info value=\"Noun\">",
			"</grammatical-info>",
			"</sense>",
			"</entry>",
			"<entry dateCreated=\"2011-03-01T22:28:56Z\" dateModified=\"2011-03-01T22:29:07Z\" guid=\"58f978d2-2cb2-4506-9a47-63c5454f0065\" id=\"hoose_58f978d2-2cb2-4506-9a47-63c5454f0065\">",
			"<lexical-unit>",
			"<form lang=\"fr\"><text>hoose</text></form>",
			"</lexical-unit>",
			"<trait name=\"morph-type\" value=\"stem\"></trait>",
			"<relation type=\"_component-lexeme\" ref=\"house_69ccc807-f3d1-44cb-b79a-e8d416b0d7c1\">",
			"</relation>",
			"</entry>",
			"</lift>"
		};

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Third test of LIFT import: minimally specified complex and variant entries.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestLiftImport3()
		{
			Assert.AreEqual("en", Cache.LangProject.CurAnalysisWss);
			Assert.AreEqual("fr", Cache.LangProject.CurVernWss);
			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			Assert.AreEqual(0, repoEntry.Count);
			Assert.AreEqual(0, repoSense.Count);

			var sOrigFile = CreateInputFile(GetLift3Strings("2011-03-01T22:28:00Z"));
			var logFile = TryImport(sOrigFile, 4);
			File.Delete(sOrigFile);
			Assert.IsNotNull(logFile);
			File.Delete(logFile);
			Assert.AreEqual(4, repoEntry.Count);
			Assert.AreEqual(3, repoSense.Count);

			ILexEntry entry;
			Assert.IsTrue(repoEntry.TryGetObject(new Guid("69ccc807-f3d1-44cb-b79a-e8d416b0d7c1"), out entry));
			Assert.AreEqual(1, entry.SensesOS.Count);
			var sense = entry.SensesOS[0];
			Assert.AreEqual(sense.Guid, new Guid("f722992a-cfdc-41ec-9c46-f927f02d68ef"));
			Assert.IsNotNull(entry.LexemeFormOA);
			Assert.IsNotNull(entry.LexemeFormOA.MorphTypeRA);
			Assert.AreEqual("stem", entry.LexemeFormOA.MorphTypeRA.Name.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("house", entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text);
			Assert.IsNotNull(sense.MorphoSyntaxAnalysisRA as IMoStemMsa);
			// ReSharper disable PossibleNullReferenceException
			Assert.IsNotNull((sense.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA);
			Assert.AreEqual("Noun", (sense.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA.Name.AnalysisDefaultWritingSystem.Text);
			// ReSharper restore PossibleNullReferenceException
			Assert.AreEqual("house", sense.Gloss.AnalysisDefaultWritingSystem.Text);
			Assert.IsNull(sense.Definition.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual(0, sense.SemanticDomainsRC.Count);
			Assert.AreEqual(1, entry.AlternateFormsOS.Count);
			var allo = entry.AlternateFormsOS[0] as IMoStemAllomorph;
			Assert.IsNotNull(allo);
			Assert.AreEqual("ouse", allo.Form.VernacularDefaultWritingSystem.Text);
			Assert.IsNull(allo.MorphTypeRA);
			Assert.AreEqual(0, allo.PhoneEnvRC.Count);
			Assert.AreEqual("<lift-residue><trait name=\"paradigm\" value=\"sing\" />" + Environment.NewLine + "</lift-residue>", allo.LiftResidue);
			Assert.AreEqual(0, entry.EntryRefsOS.Count);

			Assert.IsTrue(repoEntry.TryGetObject(new Guid("67940acb-9252-4941-bfb3-3ace4e1bda7a"), out entry));
			Assert.AreEqual(1, entry.SensesOS.Count);
			sense = entry.SensesOS[0];
			Assert.AreEqual(sense.Guid, new Guid("d3ed09c5-8757-41cb-849d-a24e6200caf4"));
			Assert.IsNotNull(entry.LexemeFormOA);
			Assert.IsNotNull(entry.LexemeFormOA.MorphTypeRA);
			Assert.AreEqual("stem", entry.LexemeFormOA.MorphTypeRA.Name.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("green", entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text);
			Assert.IsNotNull(sense.MorphoSyntaxAnalysisRA as IMoStemMsa);
			// ReSharper disable PossibleNullReferenceException
			Assert.IsNotNull((sense.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA);
			Assert.AreEqual("Adjective", (sense.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA.Name.AnalysisDefaultWritingSystem.Text);
			// ReSharper restore PossibleNullReferenceException
			Assert.AreEqual("green", sense.Gloss.AnalysisDefaultWritingSystem.Text);
			Assert.IsNull(sense.Definition.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual(0, sense.SemanticDomainsRC.Count);
			Assert.AreEqual(0, entry.EntryRefsOS.Count);

			Assert.IsTrue(repoEntry.TryGetObject(new Guid("67113a7f-e448-43e7-87cf-6d3a46ee10ec"), out entry));
			Assert.AreEqual(1, entry.SensesOS.Count);
			sense = entry.SensesOS[0];
			Assert.AreEqual(sense.Guid, new Guid("cf2ac6f4-01d8-47ed-9b41-25b6e727097f"));
			Assert.IsNotNull(entry.LexemeFormOA);
			Assert.IsNotNull(entry.LexemeFormOA.MorphTypeRA);
			Assert.AreEqual("stem", entry.LexemeFormOA.MorphTypeRA.Name.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("greenhouse", entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text);
			Assert.IsNotNull(sense.MorphoSyntaxAnalysisRA as IMoStemMsa);
			// ReSharper disable PossibleNullReferenceException
			Assert.IsNotNull((sense.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA);
			Assert.AreEqual("Noun", (sense.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA.Name.AnalysisDefaultWritingSystem.Text);
			// ReSharper restore PossibleNullReferenceException
			Assert.IsNull(sense.Gloss.AnalysisDefaultWritingSystem.Text);
			Assert.IsNull(sense.Definition.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual(0, sense.SemanticDomainsRC.Count);
			Assert.AreEqual(1, entry.EntryRefsOS.Count);
			var lexref = entry.EntryRefsOS[0];
			Assert.AreEqual(LexEntryRefTags.krtVariant, lexref.RefType);
			Assert.AreEqual(0, lexref.ComplexEntryTypesRS.Count);
			Assert.AreEqual(0, lexref.VariantEntryTypesRS.Count);
			Assert.AreEqual(2, lexref.ComponentLexemesRS.Count);
			Assert.AreEqual(new Guid("67940acb-9252-4941-bfb3-3ace4e1bda7a"), lexref.ComponentLexemesRS[0].Guid);
			Assert.AreEqual(new Guid("69ccc807-f3d1-44cb-b79a-e8d416b0d7c1"), lexref.ComponentLexemesRS[1].Guid);
			Assert.AreEqual(0, lexref.PrimaryLexemesRS.Count);

			Assert.IsTrue(repoEntry.TryGetObject(new Guid("58f978d2-2cb2-4506-9a47-63c5454f0065"), out entry));
			Assert.AreEqual(0, entry.SensesOS.Count);
			Assert.IsNotNull(entry.LexemeFormOA);
			Assert.IsNotNull(entry.LexemeFormOA.MorphTypeRA);
			Assert.AreEqual("stem", entry.LexemeFormOA.MorphTypeRA.Name.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("hoose", entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(1, entry.EntryRefsOS.Count);
			lexref = entry.EntryRefsOS[0];
			Assert.AreEqual(LexEntryRefTags.krtVariant, lexref.RefType);
			Assert.AreEqual(0, lexref.VariantEntryTypesRS.Count);
			Assert.AreEqual(0, lexref.ComplexEntryTypesRS.Count);
			Assert.AreEqual(1, lexref.ComponentLexemesRS.Count);
			Assert.AreEqual(new Guid("69ccc807-f3d1-44cb-b79a-e8d416b0d7c1"), lexref.ComponentLexemesRS[0].Guid);
			Assert.AreEqual(0, lexref.PrimaryLexemesRS.Count);
		}

		private string[] GetLift3Strings(string date)
		{
			var modString = string.Format(sLiftData3b, date);
			return s_LiftData3a.Concat(new[] {modString}).Concat(s_LiftData3c).ToArray();
		}

		[Test]
		public void LiftDataImportDoesNotDuplicateVariants()
		{
			Assert.AreEqual("en", Cache.LangProject.CurAnalysisWss);
			Assert.AreEqual("fr", Cache.LangProject.CurVernWss);
			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			Assert.AreEqual(0, repoEntry.Count);
			Assert.AreEqual(0, repoSense.Count);

			var sOrigFile = CreateInputFile(GetLift3Strings("2011-03-01T22:28:00Z"));
			var logFile = TryImport(sOrigFile, 4);
			Assert.IsNotNull(logFile);
			File.Delete(logFile);
			File.Delete(sOrigFile);

			ILexEntry entry;
			Assert.IsTrue(repoEntry.TryGetObject(new Guid("67113a7f-e448-43e7-87cf-6d3a46ee10ec"), out entry));
			Assert.AreEqual(1, entry.EntryRefsOS.Count);

			var temp = entry.EntryRefsOS[0];
			Assert.IsNotNull(logFile);
			File.Delete(logFile);
			// Importing twice should not create duplicates. Note that we use a slightly different date here
			sOrigFile = CreateInputFile(GetLift3Strings("2011-03-01T22:30:00Z"));
			logFile = TryImport(sOrigFile, null, FlexLiftMerger.MergeStyle.MsKeepNew, 4);
			Assert.IsNotNull(logFile);
			File.Delete(logFile);
			File.Delete(sOrigFile);

			Assert.AreEqual(4, repoEntry.Count);
			Assert.AreEqual(3, repoSense.Count);

			Assert.IsTrue(repoEntry.TryGetObject(new Guid("67113a7f-e448-43e7-87cf-6d3a46ee10ec"), out entry));
			Assert.AreEqual(1, entry.EntryRefsOS.Count);

		}

		static private readonly string[] s_LiftData4 = new[]
		{
			"<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
			"<lift producer=\"SIL.FLEx 7.0.1.40602\" version=\"0.13\">",
			"<header>",
			"<ranges/>",
			"<fields/>",
			"</header>",
			"<entry dateCreated=\"2011-03-01T18:09:46Z\" dateModified=\"2011-03-01T18:30:07Z\" guid=\"ecfbe958-36a1-4b82-bb69-ca5210355400\" id=\"hombre_ecfbe958-36a1-4b82-bb69-ca5210355400\">",
			"<lexical-unit>",
			"<form lang=\"es\"><text>hombre</text></form>",
			"</lexical-unit>",
			"<trait name=\"morph-type\" value=\"root\"></trait>",
			"<sense id=\"hombre_f63f1ccf-3d50-417e-8024-035d999d48bc\">",
			"<grammatical-info value=\"Noun\">",
			"</grammatical-info>",
			"<gloss lang=\"en\"><text>man</text></gloss>",
			"<definition>",
			"<form lang=\"en\"><text>male{0}adult{1}human{2}</text></form>",
			"</definition>",
			"<note type=\"encyclopedic\">",
			"<form lang=\"en\">",
			"<text>This is <span class=\"underline\">not</span> limited to spans, etc.</text>",
			"</form>",
			"</note>",
			"</sense>",
			"</entry>",
			"</lift>"
		};

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Fourth test of LIFT import: test importing multi-paragraph text with various CR/LF
		/// combinations.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		//[Ignore("Until I get it working.")]
		public void TestLiftImport4()
		{
			// Setup
// ReSharper disable InconsistentNaming
			const string LINE_SEPARATOR = "\u2028";
			var s_newLine = Environment.NewLine;
			var ccharsNL = s_newLine.Length;
			const string s_cr = "\r";
			const string s_lf = "\n";
// ReSharper restore InconsistentNaming

			SetWritingSystems("es");

			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			Assert.AreEqual(0, repoEntry.Count);
			Assert.AreEqual(0, repoSense.Count);

			// Put data in LIFT string
			const int idxModifiedLine = 16;
			// "<form lang=\"en\"><text>male{0}adult{1}human</text></form>",
			var fmtString = s_LiftData4[idxModifiedLine];
			s_LiftData4[idxModifiedLine] = String.Format(fmtString, s_newLine, s_cr, s_lf);

			var sOrigFile = CreateInputFile(s_LiftData4);
			var logFile = TryImport(sOrigFile, 1);
			File.Delete(sOrigFile);
			Assert.IsNotNull(logFile);
			File.Delete(logFile);
			Assert.AreEqual(1, repoEntry.Count);
			Assert.AreEqual(1, repoSense.Count);

			ILexEntry entry;
			Assert.IsTrue(repoEntry.TryGetObject(new Guid("ecfbe958-36a1-4b82-bb69-ca5210355400"), out entry));
			Assert.AreEqual(1, entry.SensesOS.Count);
			Assert.AreEqual(entry.SensesOS[0].Guid, new Guid("f63f1ccf-3d50-417e-8024-035d999d48bc"));
			Assert.IsNotNull(entry.LexemeFormOA);
			Assert.IsNotNull(entry.LexemeFormOA.MorphTypeRA);
			Assert.AreEqual("root", entry.LexemeFormOA.MorphTypeRA.Name.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("hombre", entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text);
			var actualDefn = entry.SensesOS[0].Definition.AnalysisDefaultWritingSystem.Text;
			var expectedXmlDefn = String.Format(fmtString, LINE_SEPARATOR, LINE_SEPARATOR, LINE_SEPARATOR);
			var doc = new XmlDocument();
			doc.LoadXml(expectedXmlDefn);
			var expectedDefn = doc.SelectSingleNode("form/text");
			Assert.IsNotNull(expectedDefn);
			Assert.AreEqual(expectedDefn.InnerText, actualDefn, "Mismatched definition.");
		}

		static private readonly string[] s_LiftData5 = new[]
		{
			"<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
			"<lift producer=\"SIL.FLEx 7.0.1.40602\" version=\"0.13\">",
			"<header>",
			"<ranges/>",
			"<fields>",
			"<field tag=\"cv-pattern\">",
			"<form lang=\"en\"><text>This records the syllable pattern for a LexPronunciation in FieldWorks.</text></form>",
			"</field>",
			"<field tag=\"tone\">",
			"<form lang=\"en\"><text>This records the tone information for a LexPronunciation in FieldWorks.</text></form>",
			"</field>",
			"<field tag=\"comment\">",
			"<form lang=\"en\"><text>This records a comment (note) in a LexEtymology in FieldWorks.</text></form>",
			"</field>",
			"<field tag=\"import-residue\">",
			"<form lang=\"en\"><text>This records residue left over from importing a standard format file into FieldWorks (or LinguaLinks).</text></form>",
			"</field>",
			"<field tag=\"literal-meaning\">",
			"<form lang=\"en\"><text>This field is used to store a literal meaning of the entry.  Typically, this field is necessary only for a compound or an idiom where the meaning of the whole is different from the sum of its parts.</text></form>",
			"</field>",
			"<field tag=\"summary-definition\">",
			"<form lang=\"en\"><text>A summary definition (located at the entry level in the Entry pane) is a general definition summarizing all the senses of a primary entry. It has no theoretical value; its use is solely pragmatic.</text></form>",
			"</field>",
			"<field tag=\"scientific-name\">",
			"<form lang=\"en\"><text>This field stores the scientific name pertinent to the current sense.</text></form>",
			"</field>",
			"<field tag=\"CustomFldEntry\">",
			"<form lang=\"en\"><text></text></form>",
			"<form lang=\"qaa-x-spec\"><text>Class= LexEntry; Type=String; WsSelector=kwsAnal</text></form>",
			"</field>",
				"<field tag=\"CustomFldEntryMulti\">",
				"<form lang=\"en\"><text></text></form>",
				"<form lang=\"qaa-x-spec\"><text>Class= LexEntry; Type=MultiUnicode; WsSelector=kwsAnalVerns</text></form>",
				"</field>",
			"<field tag=\"CustomFldSense\">",
			"<form lang=\"en\"><text></text></form>",
			"<form lang=\"qaa-x-spec\"><text>Class= LexSense; Type=String; WsSelector=kwsAnal</text></form>",
			"</field>",
			"<field tag=\"CustomFldAllomorf\">",
			"<form lang=\"en\"><text></text></form>",
			"<form lang=\"qaa-x-spec\"><text>Class= MoForm; Type=MultiUnicode; WsSelector=kwsAnalVerns</text></form>",
			"</field>",
				"<field tag=\"CustomFldAllomorphSingle\">",
				"<form lang=\"en\"><text></text></form>",
				"<form lang=\"qaa-x-spec\"><text>Class= MoForm; Type=String; WsSelector=kwsVern</text></form>",
				"</field>",
			"<field tag=\"CustomFldExample\">",
			"<form lang=\"en\"><text></text></form>",
			"<form lang=\"qaa-x-spec\"><text>Class= LexExampleSentence; Type=String; WsSelector=kwsVern</text></form>",
			"</field>",
			"</fields>",
			"</header>",
			"<entry dateCreated=\"2011-05-17T21:31:02Z\" dateModified=\"2011-05-17T21:31:02Z\" id=\"Baba_d6a97c93-0616-4793-89d3-7a90edadebd1\" guid=\"d6a97c93-0616-4793-89d3-7a90edadebd1\">",
				"<lexical-unit>",
				"<form lang=\"fr\"><text>Baba</text></form>",
				"</lexical-unit>",
				"<trait  name=\"morph-type\" value=\"stem\"/>",
				"<sense id=\"2cc3ebf6-658b-4e85-abb0-73832c33b2db\">",
				"<gloss lang=\"en\"><text>Father</text></gloss>",
				"</sense>",
			"</entry>",
			"<entry dateCreated=\"2011-05-17T21:31:17Z\" dateModified=\"2011-05-17T21:37:23Z\" id=\"Babababa_7e4e4484-d691-4ffa-8fb1-10cf4941ac14\" guid=\"7e4e4484-d691-4ffa-8fb1-10cf4941ac14\">",
				"<lexical-unit>",
				"<form lang=\"fr\"><text>Babababa</text></form>",
				"</lexical-unit>",
				"<trait  name=\"morph-type\" value=\"stem\"/>",
				"<variant>",
				"<trait  name=\"morph-type\" value=\"stem\"/>",
					"<field type=\"CustomFldAllomorf\">",
					"<form lang=\"fr\"><text>Allomorph multi French</text></form>",
					"<form lang=\"es\"><text>Allomorph multi Spanish</text></form>",
					"<form lang=\"en\"><text>Allomorph multi English</text></form>",
					"</field>",
						"<field type=\"CustomFldAllomorphSingle\">",
						"<form lang=\"fr\"><text>Allomorph single Vernacular</text></form>",
						"</field>",
				"</variant>",
					"<field type=\"CustomFldEntry\">",
					"<form lang=\"en\"><text>Entry custom field</text></form>",
					"</field>",
					"<field type=\"UndefinedCustom\">",
					"<form lang=\"en\"><text>Undefined custom field</text></form>",
					"</field>",

					"<field type=\"CustomFldEntryMulti\">",
					"<form lang=\"fr\"><text>Entry Multi Frn</text></form>",
					"<form lang=\"es\"><text>Entry Multi Spn</text></form>",
					"<form lang=\"en\"><text>Entry Multi Eng</text></form>",
					"</field>",
							"<sense id=\"29b7913f-0d28-4ee9-a57e-177f68a96654\">",
							"<grammatical-info value=\"Noun\">",
							"</grammatical-info>",
							"<gloss lang=\"en\"><text>Papi</text></gloss>",
							"<example>",
							"<field type=\"CustomFldExample\">",
							"<form lang=\"fr\"><text>example sentence custom field</text></form>",
							"</field>",
							"</example>",
							"<field type=\"CustomFldSense\">",
							"<form lang=\"en\"><text>Sense custom fiield in English</text></form>",
							"</field>",
							"</sense>",
			"</entry>",
			"</lift>"
		};

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// LIFT import: Test import of Custom Fields which contain strings
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestLiftImport5_CustomFieldsStringsAndMultiUnicode()
		{
			SetWritingSystems("fr");

			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			Assert.AreEqual(0, repoEntry.Count);
			Assert.AreEqual(0, repoSense.Count);
			// One custom field is defined in FW but not in the file
			var fdNew = new FieldDescription(Cache)
			{
				Type = CellarPropertyType.MultiUnicode,
				Class = LexEntryTags.kClassId,
				Name = "UndefinedCustom",
				Userlabel = "UndefinedCustom",
				HelpString = "some help",
				WsSelector = WritingSystemServices.kwsAnalVerns,
				DstCls = 0,
				ListRootId = Guid.Empty
			};
			fdNew.UpdateCustomField();

			var sOrigFile = CreateInputFile(s_LiftData5);

			var logFile = TryImport(sOrigFile, 2);
			File.Delete(sOrigFile);
			Assert.IsNotNull(logFile);
			File.Delete(logFile);
			Assert.AreEqual(2, repoEntry.Count);
			Assert.AreEqual(2, repoSense.Count);

			ILexEntry entry;
			Assert.IsTrue(repoEntry.TryGetObject(new Guid("7e4e4484-d691-4ffa-8fb1-10cf4941ac14"), out entry));
			Assert.AreEqual(1, entry.SensesOS.Count);
			var sense0 = entry.SensesOS[0];
			Assert.AreEqual(sense0.Guid, new Guid("29b7913f-0d28-4ee9-a57e-177f68a96654"));
			var customData = new CustomFieldData()
								{
									CustomFieldname = "CustomFldSense",
									CustomFieldType = CellarPropertyType.String,
									StringFieldText = "Sense custom fiield in English",
									StringFieldWs = "en"
								};
			m_customFieldSenseIds = GetCustomFlidsOfObject(sense0);
			VerifyCustomField(sense0, customData, m_customFieldSenseIds["CustomFldSense"]);

			//===================================================================================
			Assert.IsNotNull(entry.LexemeFormOA);
			Assert.IsNotNull(entry.LexemeFormOA.MorphTypeRA);
			Assert.AreEqual("stem", entry.LexemeFormOA.MorphTypeRA.Name.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("Babababa", entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text);
			Assert.IsNotNull(sense0.MorphoSyntaxAnalysisRA as IMoStemMsa);
			// ReSharper disable PossibleNullReferenceException
			Assert.IsNotNull((sense0.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA);
			Assert.AreEqual("Noun",
							(sense0.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA.Name.AnalysisDefaultWritingSystem.Text);
			// ReSharper restore PossibleNullReferenceException
			Assert.AreEqual("Papi", sense0.Gloss.AnalysisDefaultWritingSystem.Text);
			m_customFieldEntryIds = GetCustomFlidsOfObject(entry);
			customData = new CustomFieldData()
							{
								CustomFieldname = "UndefinedCustom",
								CustomFieldType = CellarPropertyType.MultiUnicode,
							};
			customData.MultiUnicodeStrings.Add("Undefined custom field");
			customData.MultiUnicodeWss.Add("en");
			VerifyCustomField(entry, customData, m_customFieldEntryIds["UndefinedCustom"]);

			customData = new CustomFieldData()
			{
				CustomFieldname = "CustomFldEntry",
				CustomFieldType = CellarPropertyType.String,
				StringFieldText = "Entry custom field",
				StringFieldWs = "en"
			};
			VerifyCustomField(entry, customData, m_customFieldEntryIds["CustomFldEntry"]);

			//"<field type=\"CustomFldEntryMulti\">",
			//        "<form lang=\"fr\"><text>Entry Multi Frn</text></form>",
			//        "<form lang=\"es\"><text>Entry Multi Spn</text></form>",
			//        "<form lang=\"en\"><text>Entry Multi Eng</text></form>",
			//        "</field>",
			customData = new CustomFieldData()
							{
								CustomFieldname = "CustomFldEntryMulti",
								CustomFieldType = CellarPropertyType.MultiUnicode,
							};
			customData.MultiUnicodeStrings.Add("Entry Multi Frn");
			customData.MultiUnicodeStrings.Add("Entry Multi Spn");
			customData.MultiUnicodeStrings.Add("Entry Multi Eng");
			customData.MultiUnicodeWss.Add("fr");
			customData.MultiUnicodeWss.Add("es");
			customData.MultiUnicodeWss.Add("en");

			VerifyCustomField(entry, customData, m_customFieldEntryIds["CustomFldEntryMulti"]);

			foreach (var example in sense0.ExamplesOS)
			{
				m_customFieldExampleSentencesIds = GetCustomFlidsOfObject(example);
				customData = new CustomFieldData()
								{
									CustomFieldname = "CustomFldExample",
									CustomFieldType = CellarPropertyType.String,
									StringFieldText = "example sentence custom field",
									StringFieldWs = "fr"
								};
				VerifyCustomField(example, customData, m_customFieldExampleSentencesIds["CustomFldExample"]);
			}

			//==================================Allomorph Custom Field Test===== MultiString
			var form = entry.AlternateFormsOS[0];

			m_customFieldAllomorphsIds = GetCustomFlidsOfObject(form);
			customData = new CustomFieldData()
							{
								CustomFieldname = "CustomFldAllomorf",
								CustomFieldType = CellarPropertyType.MultiUnicode,
							};
			customData.MultiUnicodeStrings.Add("Allomorph multi French");
			customData.MultiUnicodeStrings.Add("Allomorph multi Spanish");
			customData.MultiUnicodeStrings.Add("Allomorph multi English");
			customData.MultiUnicodeWss.Add("fr");
			customData.MultiUnicodeWss.Add("es");
			customData.MultiUnicodeWss.Add("en");
			VerifyCustomField(form, customData, m_customFieldAllomorphsIds["CustomFldAllomorf"]);
			//"<field type=\"CustomFldAllomorphSingle\">",
			//"<form lang=\"fr\"><text>Allomorph single Vernacular</text></form>",
			//"</field>",
			customData = new CustomFieldData()
							{
								CustomFieldname = "CustomFldAllomorphSingle",
								CustomFieldType = CellarPropertyType.String,
								StringFieldText = "Allomorph single Vernacular",
								StringFieldWs = "fr"
							};
			VerifyCustomField(form, customData, m_customFieldAllomorphsIds["CustomFldAllomorphSingle"]);
		}

		private static readonly string[] s_LiftData6 = new[]
		{
			"<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
			"<lift producer=\"SIL.FLEx 7.0.1.40602\" version=\"0.13\">",
			"<header>",
				"<ranges>",
				"<range id=\"semantic-domain-ddp4\" href=\"file://C:/Users/maclean.DALLAS/Documents/My FieldWorks/LIFT-CustomFlds New/LIFT-CustomFlds New.lift-ranges\"/>",
				"</ranges>",
			"<fields>",
			"<field tag=\"cv-pattern\">",
			"<form lang=\"en\"><text>This records the syllable pattern for a LexPronunciation in FieldWorks.</text></form>",
			"</field>",
			"<field tag=\"tone\">",
			"<form lang=\"en\"><text>This records the tone information for a LexPronunciation in FieldWorks.</text></form>",
			"</field>",
			"<field tag=\"comment\">",
			"<form lang=\"en\"><text>This records a comment (note) in a LexEtymology in FieldWorks.</text></form>",
			"</field>",
			"<field tag=\"import-residue\">",
			"<form lang=\"en\"><text>This records residue left over from importing a standard format file into FieldWorks (or LinguaLinks).</text></form>",
			"</field>",
			"<field tag=\"literal-meaning\">",
			"<form lang=\"en\"><text>This field is used to store a literal meaning of the entry.  Typically, this field is necessary only for a compound or an idiom where the meaning of the whole is different from the sum of its parts.</text></form>",
			"</field>",
			"<field tag=\"summary-definition\">",
			"<form lang=\"en\"><text>A summary definition (located at the entry level in the Entry pane) is a general definition summarizing all the senses of a primary entry. It has no theoretical value; its use is solely pragmatic.</text></form>",
			"</field>",
			"<field tag=\"scientific-name\">",
			"<form lang=\"en\"><text>This field stores the scientific name pertinent to the current sense.</text></form>",
			"</field>",
					"<field tag=\"CustomFldEntry Number\">",
					"<form lang=\"en\"><text>Number Custom Field Description</text></form>",
					"<form lang=\"qaa-x-spec\"><text>Class= LexEntry; Type=Integer</text></form>",
					"</field>",
					"<field tag=\"CustomFldEntry GenDate\">",
					"<form lang=\"en\"><text></text></form>",
					"<form lang=\"qaa-x-spec\"><text>Class= LexEntry; Type=GenDate</text></form>",
					"</field>",
					"<field tag=\"CustomFldEntry ListSingleItem\">",
					"<form lang=\"en\"><text></text></form>",
					"<form lang=\"qaa-x-spec\"><text>Class= LexEntry; Type=ReferenceAtomic; DstCls=CmPossibility</text></form>",
					"</field>",
					"<field tag=\"CustomFldEntry-ListMultiItems\">",
					"<form lang=\"en\"><text></text></form>",
					"<form lang=\"qaa-x-spec\"><text>Class= LexEntry; Type=ReferenceCollection; DstCls=CmPossibility</text></form>",
					"</field>",
					"<field tag=\"CustomFldEntry MultiString\">",
					"<form lang=\"en\"><text>This one Palaso handles as of May24/2011</text></form>",
					"<form lang=\"qaa-x-spec\"><text>Class= LexEntry; Type=MultiUnicode; WsSelector=kwsAnalVerns</text></form>",
					"</field>",
					"<field tag=\"CustomFldEntry String\">",
					"<form lang=\"en\"><text>Check this out</text></form>",
					"<form lang=\"qaa-x-spec\"><text>Class= LexEntry; Type=String; WsSelector=kwsVern</text></form>",
					"</field>",

					"<field tag=\"CustmFldSense Int\">",
					"<form lang=\"en\"><text>Sense Custom Field, Integer</text></form>",
					"<form lang=\"qaa-x-spec\"><text>Class= LexSense; Type=Integer</text></form>",
					"</field>",
					"<field tag=\"CustmFldAllomorph Int\">",
					"<form lang=\"en\"><text>Allomorph Custom Fld Integer</text></form>",
					"<form lang=\"qaa-x-spec\"><text>Class= MoForm; Type=Integer</text></form>",
					"</field>",
					"<field tag=\"CustmFldExample Int\">",
					"<form lang=\"en\"><text>Example Custom Fld Integer</text></form>",
					"<form lang=\"qaa-x-spec\"><text>Class= LexExampleSentence; Type=Integer</text></form>",
					"</field>",
			"</fields>",
			"</header>",
			"<entry dateCreated=\"2011-05-24T00:06:07Z\" dateModified=\"2011-05-24T00:18:02Z\" id=\"Baba_c78f68b9-79d0-4ce9-8b76-baa68a5c8444\" guid=\"c78f68b9-79d0-4ce9-8b76-baa68a5c8444\">",
			"<lexical-unit>",
			"<form lang=\"fr\"><text>Baba</text></form>",
			"</lexical-unit>",
				"<trait  name=\"morph-type\" value=\"stem\"/>",
					"<variant>",
					"<form lang=\"fr\"><text>BabaAlo</text></form>",
					"<trait  name=\"morph-type\" value=\"stem\"/>",
					"<trait name=\"CustmFldAllomorph Int\" value=\"175\"/>",
					"</variant>",
				"<trait name=\"CustomFldEntry Number\" value=\"13\"/>",
				"<trait name=\"CustomFldEntry GenDate\" value=\"201105232\"/>",

				"<field type=\"CustomFldEntry MultiString\">",
					"<form lang=\"fr\"><text>multiString Custom Field  french</text></form>",
					"<form lang=\"en\"><text>multiString Custom Field  english</text></form>",
				"</field>",

			"<sense id=\"9d6c600b-192a-4eec-980b-a605173ba5e3\">",
			"<grammatical-info value=\"NounPerson\">",
			"</grammatical-info>",
			"<gloss lang=\"en\"><text>Pops</text></gloss>",
				"<example>",
				"<form lang=\"fr\"><text>Example Sentence</text></form>",
				"<trait name=\"CustmFldExample Int\" value=\"24\"/>",
				"</example>",
				"<trait name=\"CustmFldSense Int\" value=\"1319\"/>",
			"</sense>",
			"</entry>",
			"</lift>"
		};

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// LIFT import: Test import of GenDate's and Numbers
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestLiftImport6_CustomFieldsNumberGenDate()
		{
			SetWritingSystems("fr");

			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			Assert.AreEqual(0, repoEntry.Count);
			Assert.AreEqual(0, repoSense.Count);

			var sOrigFile = CreateInputFile(s_LiftData6);

			var logFile = TryImport(sOrigFile, 1);
			File.Delete(sOrigFile);
			Assert.IsNotNull(logFile);
			File.Delete(logFile);
			Assert.AreEqual(1, repoEntry.Count);
			Assert.AreEqual(1, repoSense.Count);

			ILexEntry entry;
			Assert.IsTrue(repoEntry.TryGetObject(new Guid("c78f68b9-79d0-4ce9-8b76-baa68a5c8444"), out entry));
			Assert.AreEqual(1, entry.SensesOS.Count);
			var sense0 = entry.SensesOS[0];
			Assert.AreEqual(sense0.Guid, new Guid("9d6c600b-192a-4eec-980b-a605173ba5e3"));

			//===================================================================================
			Assert.IsNotNull(entry.LexemeFormOA);
			Assert.IsNotNull(entry.LexemeFormOA.MorphTypeRA);
			Assert.AreEqual("stem", entry.LexemeFormOA.MorphTypeRA.Name.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("Baba", entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text);
			Assert.IsNotNull(sense0.MorphoSyntaxAnalysisRA as IMoStemMsa);
			// ReSharper disable PossibleNullReferenceException
			Assert.IsNotNull((sense0.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA);
			Assert.AreEqual("NounPerson",
							(sense0.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA.Name.AnalysisDefaultWritingSystem.Text);
			// ReSharper restore PossibleNullReferenceException
			Assert.AreEqual("Pops", sense0.Gloss.AnalysisDefaultWritingSystem.Text);

			//===================================================================================
			VerifyCustomFieldsEntry(entry);
			//===================================================================================
			VerifyCustomFieldsSense(sense0);
			//===================================================================================
			var example = sense0.ExamplesOS[0];
			VerifyCustomFieldsExample(example);
			//==================================Allomorph Custom Field Test===== MultiString
			var form = entry.AlternateFormsOS[0];
			VerifyCustomFieldsAllomorph(form);
		}

		private void VerifyCustomFieldsEntry(ICmObject obj)
		{
			m_customFieldEntryIds = GetCustomFlidsOfObject(obj);

			var customData = new CustomFieldData()
				{
					CustomFieldname = "CustomFldEntry Number",
					CustomFieldType = CellarPropertyType.Integer,
					IntegerValue = 13
				};
			VerifyCustomField(obj, customData, m_customFieldEntryIds["CustomFldEntry Number"]);

			customData = new CustomFieldData()
			{
				CustomFieldname = "CustomFldEntry GenDate",
				CustomFieldType = CellarPropertyType.GenDate,
				GenDateLiftFormat = "201105232"
			};
			VerifyCustomField(obj, customData, m_customFieldEntryIds["CustomFldEntry GenDate"]);

			var mdc = Cache.MetaDataCacheAccessor as IFwMetaDataCacheManaged;
			var stringFlid = m_customFieldEntryIds["CustomFldEntry String"];
			var wsSpec = mdc.GetFieldWs(stringFlid);
			Assert.That(wsSpec, Is.EqualTo(WritingSystemServices.kwsVern));
		}

		private void VerifyCustomFieldsSense(ICmObject obj)
		{
			m_customFieldSenseIds = GetCustomFlidsOfObject(obj);
			var customData = new CustomFieldData()
			{
				CustomFieldname = "CustmFldSense Int",
				CustomFieldType = CellarPropertyType.Integer,
				IntegerValue = 1319
			};
			VerifyCustomField(obj, customData, m_customFieldSenseIds["CustmFldSense Int"]);
		}

		private void VerifyCustomFieldsAllomorph(ICmObject obj)
		{
			m_customFieldAllomorphsIds = GetCustomFlidsOfObject(obj);
			var customData = new CustomFieldData()
			{
				CustomFieldname = "CustmFldAllomorph Int",
				CustomFieldType = CellarPropertyType.Integer,
				IntegerValue = 175
			};
			VerifyCustomField(obj, customData, m_customFieldAllomorphsIds["CustmFldAllomorph Int"]);
		}

		private void VerifyCustomFieldsExample(ICmObject obj)
		{
			m_customFieldExampleSentencesIds = GetCustomFlidsOfObject(obj);
			var customData = new CustomFieldData()
			{
				CustomFieldname = "CustmFldExample Int",
				CustomFieldType = CellarPropertyType.Integer,
				IntegerValue = 24
			};
			VerifyCustomField(obj, customData, m_customFieldExampleSentencesIds["CustmFldExample Int"]);
		}

		private Dictionary<String, int> GetCustomFlidsOfObject(ICmObject obj)
		{
			var customFieldIds2 = new Dictionary<String, int>();
			var customFieldIds = new List<int>();
			var mdc = Cache.MetaDataCacheAccessor as IFwMetaDataCacheManaged;
			Assert.IsNotNull(mdc);
			foreach (var flid in mdc.GetFields(obj.ClassID, true, (int)CellarPropertyTypeFilter.All))
			{
				var fieldName = mdc.GetFieldName(flid);
				if (mdc.IsCustom(flid))
				{
					customFieldIds.Add(flid);
					customFieldIds2.Add(fieldName, flid);
				}

			}
			return customFieldIds2;
		}

		private void VerifyCustomField(ICmObject obj, CustomFieldData fieldData, int flid)
		{
			var mdc = Cache.MetaDataCacheAccessor as IFwMetaDataCacheManaged;
			Assert.IsNotNull(mdc);
			var sda = Cache.DomainDataByFlid as ISilDataAccessManaged;
			Assert.IsNotNull(sda);

			var fieldName = mdc.GetFieldName(flid);
			Assert.AreEqual(fieldData.CustomFieldname, fieldName);

			var type = (CellarPropertyType)mdc.GetFieldType(flid);
			Assert.AreEqual(fieldData.CustomFieldType, type);

			int ws;
			ITsString tssString;
			switch (fieldData.CustomFieldType)
			{
				case CellarPropertyType.MultiUnicode:
					//"<field type=\"CustomFldAllomorf\">",
					//    "<form lang=\"fr\"><text>Allomorph multi French</text></form>",
					//    "<form lang=\"es\"><text>Allomorph multi Spanish</text></form>",
					//    "<form lang=\"en\"><text>Allomorph multi English</text></form>",
					//</field>
					var tssMultiString = Cache.DomainDataByFlid.get_MultiStringProp(obj.Hvo, flid);
					Assert.IsNotNull(tssMultiString);
					//Assert.IsTrue(tssMultiString.StringCount >0);

						for (var i = 0; i < tssMultiString.StringCount; ++i)
						{
							tssString = tssMultiString.GetStringFromIndex(i, out ws);
							Assert.AreEqual(fieldData.MultiUnicodeStrings[i], tssString.Text);
							Assert.AreEqual(fieldData.MultiUnicodeWss[i], Cache.WritingSystemFactory.GetStrFromWs(ws));
						}
						Assert.That(tssMultiString.StringCount, Is.EqualTo(fieldData.MultiUnicodeStrings.Count));
					break;
				case CellarPropertyType.MultiBigString:
				case CellarPropertyType.MultiBigUnicode:
				case CellarPropertyType.MultiString:
					break;
				case CellarPropertyType.ReferenceAtomic:
					//"<trait name=\"CustomFld ListSingle\" value=\"Reptile\"/>",
					//"<trait name=\"CustomFld CmPossibilityCustomList\" value=\"list item 1\"/>",
					//"<trait name=\"CustomFld CustomList2\" value=\"cstm list item 2\"/>",
					//"<trait name=\"CustomFldEntry ListSingleItem\" value=\"graphology\"/>",
					var possibilityHvo = Cache.DomainDataByFlid.get_ObjectProp(obj.Hvo, flid);
					if (possibilityHvo != 0)
					{
						if (possibilityHvo == 0)
							return;
						var tss = GetPossibilityBestAlternative(possibilityHvo, Cache);
						Assert.AreEqual(fieldData.cmPossibilityNameRA, tss.ToString());
					}
					break;
				case CellarPropertyType.ReferenceCollection:
				case CellarPropertyType.ReferenceSequence:
					//"<trait name=\"CustomFld ListMulti\" value=\"Universe, creation\"/>",
					//"<trait name=\"CustomFld ListMulti\" value=\"Sun\"/>",
					var hvos = sda.VecProp(obj.Hvo, flid);
					int count = hvos.Length;
					Assert.AreEqual(fieldData.cmPossibilityNamesRS.Count, count);
					foreach (var hvo in hvos)
					{
						var tss = GetPossibilityBestAlternative(hvo, Cache);
						Assert.True(fieldData.cmPossibilityNamesRS.Contains(tss.ToString()));
					}
					break;
				case CellarPropertyType.String:
					//<field type=\"CustomField1\">
					//<form lang=\"en\">
					//    <text>CustomField1text.</text>
					//</form>
					//</field>
					tssString = Cache.DomainDataByFlid.get_StringProp(obj.Hvo, flid);
					Assert.AreEqual(fieldData.StringFieldText, tssString.Text);
					ws = tssString.get_WritingSystem(0);
					Assert.AreEqual(fieldData.StringFieldWs, Cache.WritingSystemFactory.GetStrFromWs(ws));
					break;
				case CellarPropertyType.GenDate:
					//"<trait name=\"CustomFldEntry GenDate\" value=\"201105232\"/>",
					var genDate = sda.get_GenDateProp(obj.Hvo, flid);
					VerifyGenDate(fieldData, genDate);
					break;
				case CellarPropertyType.Integer:
					//<trait name="CustomField2-LexSense Integer" value="5"></trait>
					var intVal = Cache.DomainDataByFlid.get_IntProp(obj.Hvo, flid);
					if (intVal != 0)
						Assert.AreEqual(fieldData.IntegerValue, intVal);
					break;
				default:
					break;
			}
		}

		private static void VerifyGenDate(CustomFieldData fieldData, GenDate genDate)
		{
			//"<trait name=\"CustomFldEntry GenDate\" value=\"201105232\"/>",
			//   '-'(BC and ''AD) 2011 05(May) 11(Day) 2(GenDate.PrecisionType (Before, Exact, Approximate, After)
			var sValue = fieldData.GenDateLiftFormat;
			Assert.IsNotNull(sValue);
			var liftGenDate = LiftExporter.GetGenDateFromInt(Convert.ToInt32(sValue));
			Assert.AreEqual(liftGenDate.Precision, genDate.Precision);
			Assert.AreEqual(liftGenDate.IsAD, genDate.IsAD);
			Assert.AreEqual(liftGenDate.Year, genDate.Year);
			Assert.AreEqual(liftGenDate.Month, genDate.Month);
			Assert.AreEqual(liftGenDate.Day, genDate.Day);
		}

		private static readonly string[] s_LiftRangeData7 = new[]
		{
			"<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
			"<!-- See http://code.google.com/p/lift-standard for more information on the format used here. -->",
			"<lift-ranges>",

			"<range id=\"semantic-domain-ddp4\">",

				"<range-element id=\"1 Universe, creation\" guid=\"63403699-07c1-43f3-a47c-069d6e4316e5\">",
				"<label>",
				"<form lang=\"en\"><text>Universe, creation</text></form>",
				"</label>",
				"<abbrev>",
				"<form lang=\"en\"><text>1</text></form>",
				"</abbrev>",
				"<description>",
				"<form lang=\"en\"><text>Use this domain for general words referring to the physical universe. Some languages may not have a single word for the universe and may have to use a phrase such as 'rain, soil, and things of the sky' or 'sky, land, and water' or a descriptive phrase such as 'everything you can see' or 'everything that exists'.</text></form>",
				"</description>",
				"</range-element>",

				"<range-element id=\"1.1 Sky\" guid=\"999581c4-1611-4acb-ae1b-5e6c1dfe6f0c\" parent=\"1 Universe, creation\">",
				"<label>",
				"<form lang=\"en\"><text>Sky</text></form>",
				"</label>",
				"<abbrev>",
				"<form lang=\"en\"><text>1.1</text></form>",
				"</abbrev>",
				"<description>",
				"<form lang=\"en\"><text>Use this domain for words related to the sky.</text></form>",
				"</description>",
				"</range-element>",

			"</range>",

			"<range id=\"morph-type\">",
				"<range-element id=\"particle\" guid=\"56db04bf-3d58-44cc-b292-4c8aa68538f4\">",
				"<label>",
				"<form lang=\"en\"><text>particle</text></form>",
				"</label>",
				"<abbrev>",
				"<form lang=\"en\"><text>part</text></form>",
				"</abbrev>",
				"<description>",
				"<form lang=\"en\"><text>A particle is a word that .</text></form>",
				"</description>",
				"</range-element>",

				"<range-element id=\"klingongtype\" guid=\"49343092-A48B-4c73-92B5-7603DF372D8B\">",
				"<label>",
				"<form lang=\"en\"><text>klingongtype</text></form>",
				"</label>",
				"<abbrev>",
				"<form lang=\"en\"><text>spok</text></form>",
				"</abbrev>",
				"<description>",
				"<form lang=\"en\"><text>Does this thing kling or clingy thingy.</text></form>",
				"</description>",
				"<trait name=\"leading-symbol\" value=\"-\"/>",
				"<trait name=\"trailing-symbol\" value=\"-\"/>",
				"</range-element>",

				"<range-element id=\"prefix\" guid=\"d7f713db-e8cf-11d3-9764-00c04f186933\">",
				"<label>",
				"<form lang=\"en\"><text>prefix</text></form>",
				"</label>",
				"<abbrev>",
				"<form lang=\"en\"><text>pfx</text></form>",
				"</abbrev>",
				"<description>",
				"<form lang=\"en\"><text>A prefix is an affix that is joined before a root or stem.</text></form>",
				"</description>",
				"<trait name=\"trailing-symbol\" value=\"-\"/>",
				"</range-element>",

			"</range>",

			"<!-- This is a Custom CmPossibilityList.  -->",
			"<range id=\"CustomCmPossibiltyList\" guid=\"5c99df69-9418-4d37-b9e8-f0b10c696675\">",
				"<range-element id=\"list item 1\" guid=\"66705e7a-d7db-47c6-964c-973d5830566c\">",
				"<label>",
				"<form lang=\"en\"><text>list item 1</text></form>",
				"</label>",
				"<description>",
				"<form lang=\"en\"><text>description of item 1</text></form>",
				"</description>",
				"</range-element>",

				"<range-element id=\"list item 2\" guid=\"8af65c9d-2e79-4d6a-8164-854aab89d068\">",
				"<label>",
				"<form lang=\"en\"><text>list item 2</text></form>",
				"</label>",
				"<abbrev>",
				"<form lang=\"en\"><text>itm2</text></form>",
				"</abbrev>",
				"<description>",
				"<form lang=\"en\"><text>Here is a description of item 2.</text></form>",
				"</description>",
				"</range-element>",
			"</range>",

			"<!-- This is a Custom CmPossibilityList.  -->",
			"<range id=\"CustomList Number2 \" guid=\"fddccf76-e722-4712-89a0-edd6439a860c\">",
				"<range-element id=\"cstm list item 1\" guid=\"aea3e48f-de0c-4315-8a35-f3b844070e94\">",
				"<label>",
				"<form lang=\"en\"><text>cstm list item 1</text></form>",
				"</label>",
				"<abbrev>",
				"<form lang=\"en\"><text>labr1</text></form>",
				"</abbrev>",
				"</range-element>",

				"<range-element id=\"cstm list item 2\" guid=\"164fc705-c8fd-46af-a3a8-5f0f62565d96\">",
				"<label>",
				"<form lang=\"en\"><text>cstm list item 2</text></form>",
				"</label>",
				"<description>",
				"<form lang=\"en\"><text>Desc list2 item2</text></form>",
				"</description>",
				"</range-element>",
			"</range>",

			//This list is added twice to the ranges file. This way we can test the situation where FieldWorks
			//is importing data from a lift-ranges file when the custom list already exists.
			//Here we have the situation where one range-element matches a possibility entry and the second
			//range element is a new entry.
			"<!-- This is a Custom CmPossibilityList.  -->",
			"<range id=\"CustomCmPossibiltyList\" guid=\"5c99df69-9418-4d37-b9e8-f0b10c696675\">",
				"<range-element id=\"list item 1\" guid=\"66705e7a-d7db-47c6-964c-973d5830566c\">",
				"<label>",
				"<form lang=\"en\"><text>list item 1</text></form>",
				"</label>",
				"<description>",
				"<form lang=\"en\"><text>description of item 1</text></form>",
				"</description>",
				"</range-element>",

				"<range-element id=\"list item 3\" guid=\"D7BFD944-AD73-4512-B5F2-35EC5DB3BFF3\">",
				"<label>",
				"<form lang=\"en\"><text>list item 3</text></form>",
				"</label>",
				"<abbrev>",
				"<form lang=\"en\"><text>itm3</text></form>",
				"</abbrev>",
				"<description>",
				"<form lang=\"en\"><text>Range is in twice</text></form>",
				"</description>",
				"</range-element>",
			"</range>",

			"</lift-ranges>"
		};

		private static readonly string[] s_LiftData7 = new[]
		{
			"<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
			"<lift producer=\"SIL.FLEx 7.0.1.40602\" version=\"0.13\">",
			"<header>",
				"<ranges>",
				"<range id=\"semantic-domain-ddp4\" href=\"file://C:/Users/maclean.DALLAS/Documents/My FieldWorks/LIFT-CustomFlds New/LIFT-CustomFlds New.lift-ranges\"/>",

					"<range id=\"CustomCmPossibiltyList\" href=\"file://C:/Users/maclean/Documents/My FieldWorks/LIFT Export customList/LIFT Export customList.lift-ranges\"/>",
					"<range id=\"CustomList Number2 \" href=\"file://C:/Users/maclean/Documents/My FieldWorks/LIFT Export customList/LIFT Export customList.lift-ranges\"/>",

			"</ranges>",
			"<fields>",
			"<field tag=\"cv-pattern\">",
			"<form lang=\"en\"><text>This records the syllable pattern for a LexPronunciation in FieldWorks.</text></form>",
			"</field>",
			"<field tag=\"tone\">",
			"<form lang=\"en\"><text>This records the tone information for a LexPronunciation in FieldWorks.</text></form>",
			"</field>",
			"<field tag=\"comment\">",
			"<form lang=\"en\"><text>This records a comment (note) in a LexEtymology in FieldWorks.</text></form>",
			"</field>",
			"<field tag=\"import-residue\">",
			"<form lang=\"en\"><text>This records residue left over from importing a standard format file into FieldWorks (or LinguaLinks).</text></form>",
			"</field>",
			"<field tag=\"literal-meaning\">",
			"<form lang=\"en\"><text>This field is used to store a literal meaning of the entry.  Typically, this field is necessary only for a compound or an idiom where the meaning of the whole is different from the sum of its parts.</text></form>",
			"</field>",
			"<field tag=\"summary-definition\">",
			"<form lang=\"en\"><text>A summary definition (located at the entry level in the Entry pane) is a general definition summarizing all the senses of a primary entry. It has no theoretical value; its use is solely pragmatic.</text></form>",
			"</field>",
			"<field tag=\"scientific-name\">",
			"<form lang=\"en\"><text>This field stores the scientific name pertinent to the current sense.</text></form>",
			"</field>",

				"<field tag=\"CustomFld ListSingle\">",
				"<form lang=\"en\"><text></text></form>",
				"<form lang=\"qaa-x-spec\"><text>Class=LexEntry; Type=ReferenceAtomic; DstCls=CmPossibility; range=semantic-domain-ddp4</text></form>",
				"</field>",
				"<field tag=\"CustomFld ListMulti\">",
				"<form lang=\"en\"><text></text></form>",
				"<form lang=\"qaa-x-spec\"><text>Class=LexEntry; Type=ReferenceCollection; DstCls=CmPossibility; range=semantic-domain-ddp4</text></form>",
				"</field>",

					"<field tag=\"CustomFld CmPossibilityCustomList\">",
					"<form lang=\"en\"><text></text></form>",
					"<form lang=\"qaa-x-spec\"><text>Class=LexEntry; Type=ReferenceAtomic; DstCls=CmPossibility; range=CustomCmPossibiltyList</text></form>",
					"</field>",
					"<field tag=\"CustomFld CustomList2\">",
					"<form lang=\"en\"><text>This is to ensure we import correctly.</text></form>",
					"<form lang=\"qaa-x-spec\"><text>Class=LexEntry; Type=ReferenceAtomic; DstCls=CmPossibility; range=CustomList Number2 </text></form>",
					"</field>",

			"</fields>",
			"</header>",
			"<entry dateCreated=\"2011-05-31T21:21:28Z\" dateModified=\"2011-06-06T20:03:42Z\" id=\"Baba_aef5e807-c841-4f35-9591-c8a998dc2465\" guid=\"aef5e807-c841-4f35-9591-c8a998dc2465\">",
			"<lexical-unit>",
			"<form lang=\"fr\"><text>Baba</text></form>",
			"</lexical-unit>",
					"<trait  name=\"morph-type\" value=\"stem\"/>",
					"<trait name=\"CustomFld ListSingle\" value=\"Reptile\"/>",
					"<trait name=\"CustomFld ListMulti\" value=\"Universe, creation\"/>",
					"<trait name=\"CustomFld ListMulti\" value=\"Sun\"/>",
						"<trait name=\"CustomFld CmPossibilityCustomList\" value=\"list item 1\"/>",
						"<trait name=\"CustomFld CustomList2\" value=\"cstm list item 2\"/>",
			"<sense id=\"5741255b-0563-49e0-8839-98bdb8c73f48\">",
			"<grammatical-info value=\"NounFamily\">",
			"</grammatical-info>",
			"<gloss lang=\"en\"><text>Papi</text></gloss>",
			"</sense>",
			"</entry>",

			"</lift>"
		};

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// LIFT Import:  test import of Custom Lists from the Ranges file.
		/// Also test the import of field definitions for custom fields
		/// which contain CmPossibility list data and verify that the data is correct too.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestLiftImport7_CustomLists_and_CustomFieldsWithListData()
		{
			SetWritingSystems("fr");

			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			Assert.AreEqual(0, repoEntry.Count);
			Assert.AreEqual(0, repoSense.Count);

			//Creat the LIFT data file
			var sOrigFile = CreateInputFile(s_LiftData7);
			//Create the LIFT ranges file
			var sOrigRangesFile = CreateInputRangesFile(s_LiftRangeData7);

			var logFile = TryImportWithRanges(sOrigFile, sOrigRangesFile, 1);
			File.Delete(sOrigFile);
			File.Delete(sOrigRangesFile);
			Assert.IsNotNull(logFile);
			File.Delete(logFile);
			Assert.AreEqual(1, repoEntry.Count);
			Assert.AreEqual(1, repoSense.Count);

			ILexEntry entry;
			Assert.IsTrue(repoEntry.TryGetObject(new Guid("aef5e807-c841-4f35-9591-c8a998dc2465"), out entry));
			Assert.AreEqual(1, entry.SensesOS.Count);
			var sense0 = entry.SensesOS[0];
			Assert.AreEqual(sense0.Guid, new Guid("5741255b-0563-49e0-8839-98bdb8c73f48"));

			//===================================================================================
			Assert.IsNotNull(entry.LexemeFormOA);
			Assert.IsNotNull(entry.LexemeFormOA.MorphTypeRA);
			Assert.AreEqual("stem", entry.LexemeFormOA.MorphTypeRA.Name.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("Baba", entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text);
			Assert.IsNotNull(sense0.MorphoSyntaxAnalysisRA as IMoStemMsa);
			// ReSharper disable PossibleNullReferenceException
			Assert.IsNotNull((sense0.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA);
			Assert.AreEqual("NounFamily",
							(sense0.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA.Name.AnalysisDefaultWritingSystem.Text);
			// ReSharper restore PossibleNullReferenceException
			Assert.AreEqual("Papi", sense0.Gloss.AnalysisDefaultWritingSystem.Text);

			VerifyCmPossibilityLists();

			VerifyCmPossibilityCustomFields(entry);

			VerifyCmPossibilityCustomFieldsData(entry);
		}

		private void VerifyCmPossibilityLists()
		{
			//Semantic Domain list is imported so the GUIDs should be the same for the data that was in the
			//ranges file
			var semanticDomainsList = Cache.LanguageProject.SemanticDomainListOA;
			var item = semanticDomainsList.FindOrCreatePossibility("Universe, creation", Cache.DefaultAnalWs);
			Assert.IsNotNull(item);
			Assert.AreEqual("63403699-07c1-43f3-a47c-069d6e4316e5", item.Guid.ToString());

			item = semanticDomainsList.FindOrCreatePossibility("Universe, creation" + StringUtils.kszObject + "Sky",
				Cache.DefaultAnalWs);
			Assert.IsNotNull(item);
			Assert.AreEqual("999581c4-1611-4acb-ae1b-5e6c1dfe6f0c", item.Guid.ToString());

			//FLEX does not allow users to add new morph-types.  However LIFT import will add new morph-types if
			//they are found in the LIFT ranges file.
			//Here we test that standard morph-types were not changed but a new was was added.
			var morphTylesList = Cache.LanguageProject.LexDbOA.MorphTypesOA;
			var morphType = morphTylesList.FindOrCreatePossibility("klingongtype", Cache.DefaultAnalWs);
			Assert.IsNotNull(morphType);
			Assert.AreEqual("49343092-A48B-4c73-92B5-7603DF372D8B".ToLowerInvariant(), morphType.Guid.ToString().ToLowerInvariant());
			Assert.AreEqual("Does this thing kling or clingy thingy.", morphType.Description.BestAnalysisVernacularAlternative.Text);
			Assert.AreEqual("spok", morphType.Abbreviation.BestAnalysisVernacularAlternative.Text);

			var repo = Cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();
			foreach (var list in repo.AllInstances())
			{
				if (list.OwningFlid == 0 &&
					list.Name.BestAnalysisVernacularAlternative.Text == "CustomCmPossibiltyList")
				{
					Assert.IsTrue(list.PossibilitiesOS.Count == 3);
					VerifyListItem(list.PossibilitiesOS[0], "list item 1", "66705e7a-d7db-47c6-964c-973d5830566c",
						"***", "description of item 1");
					VerifyListItem(list.PossibilitiesOS[1], "list item 2", "8af65c9d-2e79-4d6a-8164-854aab89d068",
						"itm2", "Here is a description of item 2.");
					VerifyListItem(list.PossibilitiesOS[2], "list item 3", "D7BFD944-AD73-4512-B5F2-35EC5DB3BFF3",
						"itm3", "Range is in twice");

				}
				else if (list.OwningFlid == 0 &&
					list.Name.BestAnalysisVernacularAlternative.Text == "CustomList Number2 ")
				{
					Assert.IsTrue(list.PossibilitiesOS.Count == 2);
					VerifyListItem(list.PossibilitiesOS[0], "cstm list item 1", "aea3e48f-de0c-4315-8a35-f3b844070e94",
								   "labr1", "***");
					VerifyListItem(list.PossibilitiesOS[1], "cstm list item 2", "164fc705-c8fd-46af-a3a8-5f0f62565d96",
								   "***", "Desc list2 item2");
				}
			}
		}

		private void VerifyListItem(ICmPossibility listItem, string itemName, string itemGuid, string itemAbbrev,
			string itemDesc)
		{
			Assert.AreEqual(itemName, listItem.Name.BestAnalysisVernacularAlternative.Text);
			Assert.AreEqual(itemGuid.ToLowerInvariant(), listItem.Guid.ToString().ToLowerInvariant());
			Assert.AreEqual(itemAbbrev, listItem.Abbreviation.BestAnalysisVernacularAlternative.Text);
			Assert.AreEqual(itemDesc, listItem.Description.BestAnalysisVernacularAlternative.Text);
		}

		//All custom CmPossibility lists names and Guids
		private Dictionary<string, Guid> m_customListNamesAndGuids = new Dictionary<string, Guid>();
		private IFwMetaDataCacheManaged m_mdc;

		private void VerifyCmPossibilityCustomFields(ILexEntry entry)
		{
			m_mdc = Cache.MetaDataCacheAccessor as IFwMetaDataCacheManaged;
			Assert.IsNotNull(m_mdc);
			var repo = Cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();

			//Store mapping between Possibility List names and their guids. This is used to verify that
			//the custom list has stored the correct guid for the list when imported.
			m_customListNamesAndGuids.Add(RangeNames.sSemanticDomainListOA, Cache.LanguageProject.SemanticDomainListOA.Guid);
			foreach (ICmPossibilityList list in repo.AllInstances())
			{
				if (list.OwningFlid == 0) //then it is a custom list
				{
					if (!m_customListNamesAndGuids.ContainsKey(list.Name.BestAnalysisVernacularAlternative.Text))
					{
						m_customListNamesAndGuids.Add(list.Name.BestAnalysisVernacularAlternative.Text, list.Guid);
					}
				}
			}

			//Verify each custom field
			foreach (var flid in m_mdc.GetFields(entry.ClassID, true, (int)CellarPropertyTypeFilter.All))
			{
				if (!m_mdc.IsCustom(flid))
					continue;
				var fieldName = m_mdc.GetFieldName(flid);

				if (fieldName == "CustomFld ListSingle")
				{
					VerifyCustomListToPossList(flid, CellarPropertyType.ReferenceAtomic, RangeNames.sSemanticDomainListOA);
				}
				else if (fieldName == "CustomFld ListMulti")
				{
					VerifyCustomListToPossList(flid, CellarPropertyType.ReferenceCollection, RangeNames.sSemanticDomainListOA);
				}
				else if (fieldName == "CustomFld CmPossibilityCustomList")
				{
					VerifyCustomListToPossList(flid, CellarPropertyType.ReferenceAtomic, "CustomCmPossibiltyList");
				}
				else if (fieldName == "CustomFld CustomList2")
				{
					VerifyCustomListToPossList(flid, CellarPropertyType.ReferenceAtomic, "CustomList Number2 ");
				}
			}
		}

		private void VerifyCustomListToPossList(int flid, CellarPropertyType type, string possListName)
		{
			var custFieldType = (CellarPropertyType)m_mdc.GetFieldType(flid);
			var custFieldListGuid = m_mdc.GetFieldListRoot(flid);
			Assert.AreEqual(type, custFieldType);
			Guid lstGuid = Guid.Empty;
			m_customListNamesAndGuids.TryGetValue(possListName, out lstGuid);
			Assert.AreEqual(custFieldListGuid, lstGuid);
		}

		private void VerifyCmPossibilityCustomFieldsData(ILexEntry entry)
		{
			//<trait name="CustomFld ListSingle" value="Reptile"/>
			m_customFieldEntryIds = GetCustomFlidsOfObject(entry);

			var customData = new CustomFieldData()
			{
				CustomFieldname = "CustomFld ListSingle",
				CustomFieldType = CellarPropertyType.ReferenceAtomic,
				cmPossibilityNameRA = "Reptile"
			};
			VerifyCustomField(entry, customData, m_customFieldEntryIds["CustomFld ListSingle"]);

			//<trait name="CustomFld ListMulti" value="Universe, creation"/>
			//<trait name="CustomFld ListMulti" value="Sun"/>
			customData = new CustomFieldData()
			{
				CustomFieldname = "CustomFld ListMulti",
				CustomFieldType = CellarPropertyType.ReferenceCollection
			};
			customData.cmPossibilityNamesRS.Add("Universe, creation");
			customData.cmPossibilityNamesRS.Add("Sun");
			VerifyCustomField(entry, customData, m_customFieldEntryIds["CustomFld ListMulti"]);

			//<trait name="CustomFld CmPossibilityCustomList" value="list item 1"/>
			customData = new CustomFieldData()
			{
				CustomFieldname = "CustomFld CmPossibilityCustomList",
				CustomFieldType = CellarPropertyType.ReferenceAtomic,
				cmPossibilityNameRA = "list item 1"
			};
			VerifyCustomField(entry, customData, m_customFieldEntryIds["CustomFld CmPossibilityCustomList"]);

			//<trait name="CustomFld CustomList2" value="cstm list item 2"/>
			customData = new CustomFieldData()
			{
				CustomFieldname = "CustomFld CustomList2",
				CustomFieldType = CellarPropertyType.ReferenceAtomic,
				cmPossibilityNameRA = "cstm list item 2"
			};
			VerifyCustomField(entry, customData, m_customFieldEntryIds["CustomFld CustomList2"]);
		}

		public static String GetPossibilityBestAlternative(int possibilityHvo, FdoCache cache)
		{
			ITsMultiString tsm =
				cache.DomainDataByFlid.get_MultiStringProp(possibilityHvo, CmPossibilityTags.kflidName);
			var str = BestAlternative(tsm as IMultiAccessorBase, cache.DefaultUserWs);
			return str;
		}
		private static string BestAlternative(IMultiAccessorBase multi, int wsDefault)
		{
			var tss = multi.BestAnalysisVernacularAlternative;
			if (tss.Text == "***")
				tss = multi.get_String(wsDefault);
			return XmlUtils.MakeSafeXmlAttribute(tss.Text);
		}

		private static readonly string[] s_LiftData8 = new[]
		{
			"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>",
			"<lift producer=\"SIL.FLEx 7.1.0.40722\" version=\"0.13\">",
			"  <header>",
			"    <fields>",
			"      <field tag=\"Long Text\">",
			"        <form lang=\"en\"><text>This field contains structured text.</text></form>",
			"        <form lang=\"qaa-x-spec\"><text>Class=LexEntry; Type=OwningAtomic; DstCls=StText</text></form>",
			"      </field>",
			"    </fields>",
			"  </header>",
			"  <entry dateCreated=\"2011-06-27T21:45:52Z\" dateModified=\"2011-06-29T14:57:28Z\" id=\"apa_494616cc-2f23-4877-a109-1a6c1db0887e\" guid=\"494616cc-2f23-4877-a109-1a6c1db0887e\">",
			"    <lexical-unit>",
			"      <form lang=\"fr\"><text>apa</text></form>",
			"    </lexical-unit>",
			"    <trait  name=\"morph-type\" value=\"stem\"/>",
			"    <field type=\"Long Text\">",
			"      <form lang=\"en\"><text><span class=\"Bulleted List\"><span lang=\"en\">This is a test of sorts.  This field can contain </span><span lang=\"en\" class=\"Emphasized Text\">multiple</span><span lang=\"en\"> paragraphs.</span></span>\u2029",
			"<span class=\"Bulleted List\"><span lang=\"en\">For example, this is the second paragraph already.</span></span>\u2029",
			"<span class=\"Normal\"><span lang=\"en\">This third paragraph is back in the normal (default) paragraph style, and some character </span><span lang=\"en\" class=\"Emphasized Text\">formatting</span><span lang=\"en\"> to produce </span><span lang=\"en\" class=\"Strong\">multiple</span><span lang=\"en\"> spans within the paragraph.</span></span></text></form>",
			"    </field>",
			"    <sense id=\"3e0ae703-db7f-4687-9cf5-481524095905\">",
			"      <grammatical-info value=\"Pronoun\">",
			"      </grammatical-info>",
			"      <gloss lang=\"en\"><text>this</text></gloss>",
			"    </sense>",
			"  </entry>",
			"  <entry dateCreated=\"2011-06-29T15:58:03Z\" dateModified=\"2011-06-29T15:58:03Z\" id=\"test_241cffca-3062-4b1c-8f9f-ab8ed07eb7bd\" guid=\"241cffca-3062-4b1c-8f9f-ab8ed07eb7bd\">",
			"    <lexical-unit>",
			"      <form lang=\"fr\"><text>test</text></form>",
			"    </lexical-unit>",
			"    <trait  name=\"morph-type\" value=\"stem\"/>",
			"    <field type=\"Long Text\">",
			"      <form lang=\"en\"><text><span lang=\"en\">This test paragraph does not have a style explicitly assigned.</span>\u2029",
			"<span lang=\"en\">This test paragraph has </span><span lang=\"en\" class=\"Strong\">some</span><span lang=\"en\"> </span><span lang=\"en\" class=\"Emphasized Text\">character</span><span lang=\"en\"> formatting.</span>\u2029",
			"<span class=\"Block Quote\"><span lang=\"en\">This paragraph has a paragraph style applied.</span></span></text></form>",
			"    </field>",
			"    <sense id=\"2759532a-26db-4850-9cba-b3684f0a3f5f\">",
			"      <grammatical-info value=\"Noun\">",
			"      </grammatical-info>",
			"      <gloss lang=\"en\"><text>test</text></gloss>",
			"    </sense>",
			"  </entry>",
			"</lift>"
		};

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// LIFT Import:  test import of custom fields representing StText data.
		/// Also test the import of field definitions for custom fields which contain StText data
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestLiftImport8CustomStText()
		{
			SetWritingSystems("fr");

			CreateNeededStyles();

			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			Assert.AreEqual(0, repoEntry.Count);
			Assert.AreEqual(0, repoSense.Count);

			var sOrigFile = CreateInputFile(s_LiftData8);

			var logFile = TryImport(sOrigFile, 2);
			File.Delete(sOrigFile);
			Assert.IsNotNull(logFile);
			File.Delete(logFile);

			var flidCustom = Cache.MetaDataCacheAccessor.GetFieldId("LexEntry", "Long Text", false);
			Assert.AreNotEqual(0, flidCustom, "The \"Long Text\" custom field should exist for LexEntry objects.");
			var type = Cache.MetaDataCacheAccessor.GetFieldType(flidCustom);
			Assert.AreEqual((int) CellarPropertyType.OwningAtomic, type, "The custom field should be an atomic owning field.");
			var destName = Cache.MetaDataCacheAccessor.GetDstClsName(flidCustom);
			Assert.AreEqual("StText", destName, "The custom field should own an StText object.");

			Assert.AreEqual(2, repoEntry.Count);
			Assert.AreEqual(2, repoSense.Count);

			VerifyFirstEntryStTextDataImportExact(repoEntry, 3, flidCustom);

			ILexEntry entry2;
			Assert.IsTrue(repoEntry.TryGetObject(new Guid("241cffca-3062-4b1c-8f9f-ab8ed07eb7bd"), out entry2));
			Assert.AreEqual(1, entry2.SensesOS.Count);
			var sense2 = entry2.SensesOS[0];
			Assert.AreEqual(sense2.Guid, new Guid("2759532a-26db-4850-9cba-b3684f0a3f5f"));

			var hvo = Cache.DomainDataByFlid.get_ObjectProp(entry2.Hvo, flidCustom);
			Assert.AreNotEqual(0, hvo, "The second entry has a value in the \"Long Text\" custom field.");
			var text = Cache.ServiceLocator.ObjectRepository.GetObject(hvo) as IStText;
			Assert.IsNotNull(text);
			Assert.AreEqual(3, text.ParagraphsOS.Count, "The first Long Text field should have three paragraphs.");

			Assert.IsNull(text.ParagraphsOS[0].StyleName);
			ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, wsEn);
			tisb.Append("This test paragraph does not have a style explicitly assigned.");
			var tss = tisb.GetString();
			tisb.Clear();
			tisb.ClearProps();
			var para = text.ParagraphsOS[0] as IStTxtPara;
			Assert.IsNotNull(para);
			Assert.AreEqual(tss.Text, para.Contents.Text);
			Assert.IsTrue(tss.Equals(para.Contents), "The first paragraph (second entry) contents should have all its formatting.");

			Assert.IsNull(text.ParagraphsOS[1].StyleName);
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, wsEn);
			tisb.Append("This test paragraph has ");
			tisb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Strong");
			tisb.Append("some");
			tisb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, null);
			tisb.Append(" ");
			tisb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Emphasized Text");
			tisb.Append("character");
			tisb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, null);
			tisb.Append(" formatting.");
			tss = tisb.GetString();
			tisb.Clear();
			tisb.ClearProps();
			para = text.ParagraphsOS[1] as IStTxtPara;
			Assert.IsNotNull(para);
			Assert.AreEqual(tss.Text, para.Contents.Text);
			Assert.IsTrue(tss.Equals(para.Contents), "The second paragraph (second entry) contents should have all its formatting.");

			Assert.AreEqual("Block Quote", text.ParagraphsOS[2].StyleName);
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, wsEn);
			tisb.Append("This paragraph has a paragraph style applied.");
			tss = tisb.GetString();
			tisb.Clear();
			tisb.ClearProps();
			para = text.ParagraphsOS[2] as IStTxtPara;
			Assert.IsNotNull(para);
			Assert.AreEqual(tss.Text, para.Contents.Text);
			Assert.IsTrue(tss.Equals(para.Contents), "The third paragraph (second entry) contents should have all its formatting.");
		}

		private void CreateNeededStyles()
		{
			var factory = Cache.ServiceLocator.GetInstance<IStStyleFactory>();
			factory.Create(Cache.LangProject.StylesOC, "Bulleted List", ContextValues.General, StructureValues.Undefined,
				FunctionValues.Prose, false, 0, true);
			factory.Create(Cache.LangProject.StylesOC, "Block Quote", ContextValues.General, StructureValues.Undefined,
				FunctionValues.Prose, false, 0, true);
			factory.Create(Cache.LangProject.StylesOC, "Normal", ContextValues.General, StructureValues.Undefined,
				FunctionValues.Prose, false, 0, true);
			factory.Create(Cache.LangProject.StylesOC, "Emphasized Text", ContextValues.General, StructureValues.Undefined,
				FunctionValues.Prose, true, 0, true);
			factory.Create(Cache.LangProject.StylesOC, "Strong", ContextValues.General, StructureValues.Undefined,
				FunctionValues.Prose, true, 0, true);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// LIFT Import:  test import of custom fields representing StText data with conflicts,
		/// using the "Keep Both" option.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestLiftImport9AMergingStTextKeepBoth()
		{
			SetWritingSystems("fr");

			CreateNeededStyles();
			var flidCustom = CreateFirstEntryWithConflictingData();

			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			Assert.AreEqual(1, repoEntry.Count);
			Assert.AreEqual(1, repoSense.Count);

			var sOrigFile = CreateInputFile(s_LiftData8);
			var logFile = TryImport(sOrigFile, null, FlexLiftMerger.MergeStyle.MsKeepBoth, 2);

		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// LIFT Import:  test import of custom fields representing StText data with conflicts,
		/// using the "Keep Old" option.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestLiftImport9BMergingStTextKeepOld()
		{
			SetWritingSystems("fr");

			CreateNeededStyles();
			var flidCustom = CreateFirstEntryWithConflictingData();

			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			Assert.AreEqual(1, repoEntry.Count);
			Assert.AreEqual(1, repoSense.Count);

			var sOrigFile = CreateInputFile(s_LiftData8);
			var logFile = TryImport(sOrigFile, null, FlexLiftMerger.MergeStyle.MsKeepOld, 2);

		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// LIFT Import:  test import of custom fields representing StText data with conflicts,
		/// using the "Keep New" option.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test, Ignore("This fails if another test runs first, but succeeds by itself!")]
		public void TestLiftImport9CMergingStTextKeepNew()
		{
			SetWritingSystems("fr");

			CreateNeededStyles();
			var flidCustom = CreateFirstEntryWithConflictingData();

			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			Assert.AreEqual(1, repoEntry.Count);
			Assert.AreEqual(1, repoSense.Count);

			var sOrigFile = CreateInputFile(s_LiftData8);
			var logFile = TryImport(sOrigFile, null, FlexLiftMerger.MergeStyle.MsKeepNew, 2);

			Assert.AreEqual(2, repoEntry.Count);
			Assert.AreEqual(2, repoSense.Count);

			VerifyFirstEntryStTextDataImportExact(repoEntry, 4, flidCustom);
			// Now check the fourth paragraph.
			ILexEntry entry1;
			Assert.IsTrue(repoEntry.TryGetObject(new Guid("494616cc-2f23-4877-a109-1a6c1db0887e"), out entry1));
			var hvo = Cache.DomainDataByFlid.get_ObjectProp(entry1.Hvo, flidCustom);
			Assert.AreNotEqual(0, hvo, "The first entry has a value in the \"Long Text\" custom field.");
			var text = Cache.ServiceLocator.ObjectRepository.GetObject(hvo) as IStText;
			Assert.IsNotNull(text);
			var para = text.ParagraphsOS[3] as IStTxtPara;
			Assert.IsNotNull(para);
			Assert.AreEqual("Numbered List", para.StyleName);
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			var tss = Cache.TsStrFactory.MakeString("This is the fourth paragraph.", wsEn);
			Assert.AreEqual(tss.Text, para.Contents.Text);
			Assert.IsTrue(tss.Equals(para.Contents), "The fourth paragraph contents should not have changed.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// LIFT Import:  test import of custom fields representing StText data with conflicts,
		/// using the "Keep Only New" option.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test, Ignore("This fails if another test runs first, but succeeds by itself!")]
		public void TestLiftImport9DMergingStTextKeepOnlyNew()
		{
			SetWritingSystems("fr");

			CreateNeededStyles();
			var flidCustom = CreateFirstEntryWithConflictingData();

			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			Assert.AreEqual(1, repoEntry.Count);
			Assert.AreEqual(1, repoSense.Count);

			var sOrigFile = CreateInputFile(s_LiftData8);
			var logFile = TryImport(sOrigFile, null, FlexLiftMerger.MergeStyle.MsKeepOnlyNew, 2);

			Assert.AreEqual(2, repoEntry.Count);
			Assert.AreEqual(2, repoSense.Count);

			VerifyFirstEntryStTextDataImportExact(repoEntry, 3, flidCustom);
		}

		private static readonly string[] s_ComponentTest = new[]
		{
			"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>",
			"<lift producer=\"SIL.FLEx 7.2.4.41003\" version=\"0.13\">",
			"  <header>",
			"    <fields>",
			"    </fields>",
			"  </header>",
			"  <entry dateCreated=\"2011-06-27T21:45:52Z\" dateModified=\"2011-06-29T14:57:28Z\" id=\"do_be4eb9fd-58fd-49fe-a8ef-e13a96646806\" guid=\"be4eb9fd-58fd-49fe-a8ef-e13a96646806\">",
			"    <lexical-unit>",
			"      <form lang=\"fr\"><text>do</text></form>",
			"    </lexical-unit>",
			"    <trait  name=\"morph-type\" value=\"stem\"/>",
			"<relation type=\"Compare\" ref=\"todo_10af904a-7395-4a37-a195-44001127ae40\"/>",
			"<relation type=\"Compare\" ref=\"to_10af904a-7395-4a37-a195-44001127ae40\"/>",
			"    <sense id=\"3e0ae703-db7f-4687-9cf5-481524095905\">",
			"    </sense>",
			"  </entry>",
			"  <entry dateCreated=\"2011-06-29T15:58:03Z\" dateModified=\"2011-06-29T15:58:03Z\" id=\"todo_10af904a-7395-4a37-a195-44001127ae40\" guid=\"10af904a-7395-4a37-a195-44001127ae40\">",
			"    <lexical-unit>",
			"      <form lang=\"fr\"><text>todo</text></form>",
			"    </lexical-unit>",
			"    <trait  name=\"morph-type\" value=\"stem\"/>",
			"<relation type=\"Compare\" ref=\"to_9bfa6fc4-0a2d-44ff-9c2c-91460ef3c856\"/>",
			"<relation type=\"Compare\" ref=\"do_be4eb9fd-58fd-49fe-a8ef-e13a96646806\"/>",
			"    <sense id=\"2759532a-26db-4850-9cba-b3684f0a3f5f\">",
			"    </sense>",
			"  </entry>",
			"  <entry dateCreated=\"2011-06-29T15:58:03Z\" dateModified=\"2011-06-29T15:58:03Z\" id=\"to_9bfa6fc4-0a2d-44ff-9c2c-91460ef3c856\" guid=\"9bfa6fc4-0a2d-44ff-9c2c-91460ef3c856\">",
			"    <lexical-unit>",
			"      <form lang=\"fr\"><text>todo</text></form>",
			"    </lexical-unit>",
			"    <trait  name=\"morph-type\" value=\"stem\"/>",
			"<relation type=\"Compare\" ref=\"todo_10af904a-7395-4a37-a195-44001127ae40\"/>",
			"<relation type=\"Compare\" ref=\"do_be4eb9fd-58fd-49fe-a8ef-e13a96646806\"/>",
			"    <sense id=\"2759532a-26db-4850-9cba-b3684f0a3f5f\">",
			"    </sense>",
			"  </entry>",
			"</lift>"
		};

		[Test]
		public void TestImportDoesNotSplitComponentCollection()
		{

			SetWritingSystems("fr");

			CreateNeededStyles();

			var entryRepository = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();

			var sOrigFile = CreateInputFile(s_ComponentTest);
			var logFile = TryImport(sOrigFile, null, FlexLiftMerger.MergeStyle.MsKeepNew, 3);
			var todoEntry = entryRepository.GetObject(new Guid("10af904a-7395-4a37-a195-44001127ae40"));
			Assert.AreEqual(1, todoEntry.LexEntryReferences.Count());
			Assert.AreEqual(3, todoEntry.LexEntryReferences.First().TargetsRS.Count);
		}

		private static readonly string[] s_LT12948Test = new[]
		{
			"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>",
			"<lift producer=\"SIL.FLEx 7.2.4.41003\" version=\"0.13\">",
			"  <header>",
			"    <fields>",
			"    </fields>",
			"  </header>",
			"  <entry dateCreated=\"2011-06-27T21:45:52Z\" dateModified=\"2011-06-29T14:57:28Z\" id=\"do_be4eb9fd-58fd-49fe-a8ef-e13a96646806\" guid=\"be4eb9fd-58fd-49fe-a8ef-e13a96646806\">",
			"    <lexical-unit>",
			"      <form lang=\"fr\"><text>do</text></form>",
			"    </lexical-unit>",
			"    <trait  name=\"morph-type\" value=\"stem\"/>",
			"    <sense id=\"3e0ae703-db7f-4687-9cf5-481524095905\">",
			"    </sense>",
			"  </entry>",
			"  <entry dateCreated=\"2011-06-29T15:58:03Z\" dateModified=\"2011-06-29T15:58:03Z\" id=\"todo_10af904a-7395-4a37-a195-44001127ae40\" guid=\"10af904a-7395-4a37-a195-44001127ae40\">",
			"    <lexical-unit>",
			"      <form lang=\"fr\"><text>todo</text></form>",
			"    </lexical-unit>",
			"    <trait  name=\"morph-type\" value=\"stem\"/>",
			"<relation type=\"_component-lexeme\" ref=\"to_9bfa6fc4-0a2d-44ff-9c2c-91460ef3c856\">",
			"<trait name=\"is-primary\" value=\"true\"/>",
			"<trait name=\"complex-form-type\" value=\"Compound\"/>",
			"</relation>",
			"<relation type=\"_component-lexeme\" ref=\"do_be4eb9fd-58fd-49fe-a8ef-e13a96646806\">",
			"<trait name=\"complex-form-type\" value=\"Compound\"/>",
			"<trait name=\"is-primary\" value=\"true\"/>",
			"</relation>",
			"    <sense id=\"2759532a-26db-4850-9cba-b3684f0a3f5f\">",
			"    </sense>",
			"  </entry>",
			"  <entry dateCreated=\"2011-06-29T15:58:03Z\" dateModified=\"2011-06-29T15:58:03Z\" id=\"to_9bfa6fc4-0a2d-44ff-9c2c-91460ef3c856\" guid=\"9bfa6fc4-0a2d-44ff-9c2c-91460ef3c856\">",
			"    <lexical-unit>",
			"      <form lang=\"fr\"><text>todo</text></form>",
			"    </lexical-unit>",
			"    <trait  name=\"morph-type\" value=\"stem\"/>",
			"    <sense id=\"2759532a-26db-4850-9cba-b3684f0a3f5f\">",
			"    </sense>",
			"  </entry>",
			"</lift>"
		};

		[Test]
		public void TestImportDoesNotSplitComplexForms_LT12948()
		{

			SetWritingSystems("fr");

			CreateNeededStyles();

			var entryRepository = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();

			var sOrigFile = CreateInputFile(s_LT12948Test);
			var logFile = TryImport(sOrigFile, null, FlexLiftMerger.MergeStyle.MsKeepNew, 3);
			var todoEntry = entryRepository.GetObject(new Guid("10af904a-7395-4a37-a195-44001127ae40"));
			//Even though they do not have an order set (due to a now fixed export defect) the two relations in the 'todo' entry
			//should be collected in the same LexEntryRef
			Assert.AreEqual(1, todoEntry.ComplexFormEntryRefs.Count());
			Assert.AreEqual(2, todoEntry.ComplexFormEntryRefs.First().ComponentLexemesRS.Count);
		}


		private static readonly string[] s_LT12948Test2 = new[]
		{
			"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>",
			"<lift producer=\"SIL.FLEx 7.2.4.41003\" version=\"0.13\">",
			"  <header>",
			"    <fields>",
			"    </fields>",
			"  </header>",
			"  <entry dateCreated=\"2011-06-27T21:45:52Z\" dateModified=\"2011-06-29T14:57:28Z\" id=\"do_be4eb9fd-58fd-49fe-a8ef-e13a96646806\" guid=\"be4eb9fd-58fd-49fe-a8ef-e13a96646806\">",
			"    <lexical-unit>",
			"      <form lang=\"fr\"><text>do</text></form>",
			"    </lexical-unit>",
			"    <trait  name=\"morph-type\" value=\"stem\"/>",
			"    <sense id=\"3e0ae703-db7f-4687-9cf5-481524095905\">",
			"    </sense>",
			"  </entry>",
			"  <entry dateCreated=\"2011-06-29T15:58:03Z\" dateModified=\"2011-06-29T15:58:03Z\" id=\"todo_10af904a-7395-4a37-a195-44001127ae40\" guid=\"10af904a-7395-4a37-a195-44001127ae40\">",
			"    <lexical-unit>",
			"      <form lang=\"fr\"><text>todo</text></form>",
			"    </lexical-unit>",
			"    <trait  name=\"morph-type\" value=\"stem\"/>",
			"<relation type=\"_component-lexeme\" ref=\"to_9bfa6fc4-0a2d-44ff-9c2c-91460ef3c856\" order=\"1\">",
			"<trait name=\"is-primary\" value=\"true\"/>",
			"<trait name=\"variant-type\" value=\"Dialectal Variant\"/>",
			"</relation>",
			"<relation type=\"_component-lexeme\" ref=\"to_9bfa6fc4-0a2d-44ff-9c2c-91460ef3c856\" order=\"1\">",
			"<trait name=\"is-primary\" value=\"true\"/>",
			"<trait name=\"complex-form-type\" value=\"Compound\"/>",
			"</relation>",
			"<relation type=\"_component-lexeme\" ref=\"do_be4eb9fd-58fd-49fe-a8ef-e13a96646806\" order=\"2\">",
			"<trait name=\"is-primary\" value=\"true\"/>",
			"<trait name=\"complex-form-type\" value=\"Compound\"/>",
			"</relation>",
			"    <sense id=\"2759532a-26db-4850-9cba-b3684f0a3f5f\">",
			"    </sense>",
			"  </entry>",
			"  <entry dateCreated=\"2011-06-29T15:58:03Z\" dateModified=\"2011-06-29T15:58:03Z\" id=\"to_9bfa6fc4-0a2d-44ff-9c2c-91460ef3c856\" guid=\"9bfa6fc4-0a2d-44ff-9c2c-91460ef3c856\">",
			"    <lexical-unit>",
			"      <form lang=\"fr\"><text>todo</text></form>",
			"    </lexical-unit>",
			"    <trait  name=\"morph-type\" value=\"stem\"/>",
			"    <sense id=\"2759532a-26db-4850-9cba-b3684f0a3f5f\">",
			"    </sense>",
			"  </entry>",
			"</lift>"
		};

		[Test]
		public void TestImportSplitsDifferingComplexFormsByType_LT12948()
		{
			SetWritingSystems("fr");

			CreateNeededStyles();

			var entryRepository = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();

			var sOrigFile = CreateInputFile(s_LT12948Test2);
			var logFile = TryImport(sOrigFile, null, FlexLiftMerger.MergeStyle.MsKeepNew, 3);
			var todoEntry = entryRepository.GetObject(new Guid("10af904a-7395-4a37-a195-44001127ae40"));
			//Even though they do not have an order set (due to a now fixed export defect) the two relations in the 'todo' entry
			//should be collected in the same LexEntryRef
			Assert.AreEqual(1, todoEntry.ComplexFormEntryRefs.Count());
			Assert.AreEqual(2, todoEntry.ComplexFormEntryRefs.First().ComponentLexemesRS.Count);
			Assert.AreEqual(1, todoEntry.VariantEntryRefs.Count());
		}

		private static readonly string[] s_LT12948Test3 = new[]
		{
			"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>",
			"<lift producer=\"SIL.FLEx 7.2.4.41003\" version=\"0.13\">",
			"  <header>",
			"    <fields>",
			"    </fields>",
			"  </header>",
			"<entry dateCreated='2012-04-11T19:45:34Z' dateModified='2012-04-11T19:49:42Z' id='house_7e4e4aed-0b2e-4e2b-9c84-4466b8e73ea4' guid='7e4e4aed-0b2e-4e2b-9c84-4466b8e73ea4'>",
			"<lexical-unit>",
			"<form lang='obo'>",
			"	<text>house</text>",
			"</form>",
			"	</lexical-unit>",
			"	<trait name='morph-type' value='stem' />",
			"	<sense id='a02f9304-1100-40cd-9433-6f9c70177e1e'>",
			"		<gloss lang='en'>",
			"			<text>house</text>",
			"		</gloss>",
			"		<relation type='Synonyms' ref='4bb72859-623b-4616-aa10-a6b0005a2f4b' />",
			"		<relation type='Synonyms' ref='1c62a5fa-fcc1-477e-bc1e-e69f7633c613' />",
			"	</sense>",
			"</entry>",
			"<entry dateCreated='2012-04-11T19:45:34Z' dateModified='2012-04-11T19:49:42Z' id='bob' guid='7e6e4aed-0b2e-4e2b-9c84-4466b8e73ea4'>",
			"<lexical-unit>",
			"<form lang='obo'>",
			"	<text>bob</text>",
			"</form>",
			"	</lexical-unit>",
			"	<trait name='morph-type' value='stem' />",
			"	<relation type='Synonyms' ref='builder' />",
			"	<sense id='bobsense'>",
			"	</sense>",
			"</entry>",
			"<entry dateCreated='2012-04-11T19:49:42Z' dateModified='2012-04-11T19:49:42Z' id='bungalo_885f3937-7761-406c-a46b-ef71e2f10334' guid='885f3937-7761-406c-a46b-ef71e2f10334'>",
			"	<lexical-unit>",
			"		<form lang='obo'>",
			"			<text>bungalo</text>",
			"		</form>",
			"	</lexical-unit>",
			"	<trait name='morph-type' value='stem' />",
			"	<sense id='4bb72859-623b-4616-aa10-a6b0005a2f4b'>",
			"		<gloss lang='en'>",
			"			<text>bungalo</text>",
			"		</gloss>",
			"		<relation type='Synonyms' ref='a02f9304-1100-40cd-9433-6f9c70177e1e' />",
			"		<relation type='Synonyms' ref='1c62a5fa-fcc1-477e-bc1e-e69f7633c613' />",
			"	</sense>",
			"</entry>",
			"<entry dateCreated='2012-04-11T19:45:34Z' dateModified='2012-04-11T19:49:42Z' id='builder' guid='7e5e4aed-0b2e-4e2b-9c84-4466b8e73ea4'>",
			"<lexical-unit>",
			"<form lang='obo'>",
			"	<text>builder</text>",
			"</form>",
			"	</lexical-unit>",
			"	<trait name='morph-type' value='stem' />",
			"	<relation type='Synonyms' ref='bob' />",
			"	<sense id='buildersense'>",
			"	</sense>",
			"</entry>",
			"<entry dateCreated='2012-04-11T19:49:57Z' dateModified='2012-04-11T19:49:57Z' id='castle_00c8535d-0be5-45c3-9d70-0a7840325fed' guid='00c8535d-0be5-45c3-9d70-0a7840325fed'>",
			"	<lexical-unit>",
			"		<form lang='obo'>",
			"			<text>castle</text>",
			"		</form>",
			"	</lexical-unit>",
			"	<trait name='morph-type' value='stem' />",
			"	<sense id='1c62a5fa-fcc1-477e-bc1e-e69f7633c613'>",
			"		<gloss lang='en'>",
			"			<text>castle</text>",
			"		</gloss>",
			"		<relation type='Synonyms' ref='a02f9304-1100-40cd-9433-6f9c70177e1e' />",
			"		<relation type='Synonyms' ref='4bb72859-623b-4616-aa10-a6b0005a2f4b' />",
			"	</sense>",
			"</entry>",
			"</lift>"
		};

		[Test]
		public void TestImportDoesNotSplitSynonyms_LT12948()
		{
			SetWritingSystems("fr");

			CreateNeededStyles();

			var entryRepository = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var senseRepository = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();

			var sOrigFile = CreateInputFile(s_LT12948Test3);
			var logFile = TryImport(sOrigFile, null, FlexLiftMerger.MergeStyle.MsKeepNew, 5);
			var bungaloSense = senseRepository.GetObject(new Guid("4bb72859-623b-4616-aa10-a6b0005a2f4b"));
			var bobEntry = entryRepository.GetObject(new Guid("7e6e4aed-0b2e-4e2b-9c84-4466b8e73ea4"));
			//Even though they do not have an order set (due to a now fixed export defect) the two relations in the 'todo' entry
			//should be collected in the same LexEntryRef
			Assert.AreEqual(1, bungaloSense.LexSenseReferences.Count());
			Assert.AreEqual(3, bungaloSense.LexSenseReferences.First().TargetsRS.Count);
			Assert.AreEqual(1, bobEntry.LexEntryReferences.Count());
			Assert.AreEqual(2, bobEntry.LexEntryReferences.First().TargetsRS.Count);
		}

		private void VerifyFirstEntryStTextDataImportExact(ILexEntryRepository repoEntry, int cpara, int flidCustom)
		{
			ILexEntry entry1;
			Assert.IsTrue(repoEntry.TryGetObject(new Guid("494616cc-2f23-4877-a109-1a6c1db0887e"), out entry1));
			Assert.AreEqual(1, entry1.SensesOS.Count);
			var sense1 = entry1.SensesOS[0];
			Assert.AreEqual(sense1.Guid, new Guid("3e0ae703-db7f-4687-9cf5-481524095905"));

			var hvo = Cache.DomainDataByFlid.get_ObjectProp(entry1.Hvo, flidCustom);
			Assert.AreNotEqual(0, hvo, "The first entry has a value in the \"Long Text\" custom field.");
			var text = Cache.ServiceLocator.ObjectRepository.GetObject(hvo) as IStText;
			Assert.IsNotNull(text);
			Assert.AreEqual(cpara, text.ParagraphsOS.Count,
				String.Format("The first Long Text field should have {0} paragraphs.", cpara));
			Assert.AreEqual("Bulleted List", text.ParagraphsOS[0].StyleName);
			ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, wsEn);
			tisb.Append("This is a test of sorts.  This field can contain ");
			tisb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Emphasized Text");
			tisb.Append("multiple");
			tisb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, null);
			tisb.Append(" paragraphs.");
			var tss = tisb.GetString();
			tisb.Clear();
			tisb.ClearProps();
			var para = text.ParagraphsOS[0] as IStTxtPara;
			Assert.IsNotNull(para);
			Assert.AreEqual(tss.Text, para.Contents.Text);
			Assert.IsTrue(tss.Equals(para.Contents), "The first paragraph contents should have all its formatting.");

			Assert.AreEqual("Bulleted List", text.ParagraphsOS[1].StyleName);
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, wsEn);
			tisb.Append("For example, this is the second paragraph already.");
			tss = tisb.GetString();
			tisb.Clear();
			tisb.ClearProps();
			para = text.ParagraphsOS[1] as IStTxtPara;
			Assert.IsNotNull(para);
			Assert.AreEqual(tss.Text, para.Contents.Text);
			Assert.IsTrue(tss.Equals(para.Contents), "The second paragraph contents should have all its formatting.");

			Assert.AreEqual("Normal", text.ParagraphsOS[2].StyleName);
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, wsEn);
			tisb.Append("This third paragraph is back in the normal (default) paragraph style, and some character ");
			tisb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Emphasized Text");
			tisb.Append("formatting");
			tisb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, null);
			tisb.Append(" to produce ");
			tisb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Strong");
			tisb.Append("multiple");
			tisb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, null);
			tisb.Append(" spans within the paragraph.");
			tss = tisb.GetString();
			tisb.Clear();
			tisb.ClearProps();
			para = text.ParagraphsOS[2] as IStTxtPara;
			Assert.IsNotNull(para);
			Assert.AreEqual(tss.Text, para.Contents.Text);
			Assert.IsTrue(tss.Equals(para.Contents), "The third paragraph contents should have all its formatting.");
		}

		private int CreateFirstEntryWithConflictingData()
		{
			var entry0 = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(
				new Guid("494616cc-2f23-4877-a109-1a6c1db0887e"), Cache.LangProject.LexDbOA);
			entry0.LexemeFormOA = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry0.LexemeFormOA.Form.set_String(Cache.DefaultVernWs, "afa");
			entry0.LexemeFormOA.MorphTypeRA =
				Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem);
			var msa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			entry0.MorphoSyntaxAnalysesOC.Add(msa);
			var sense0 = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create(
				new Guid("3e0ae703-db7f-4687-9cf5-481524095905"), entry0);
			sense0.Gloss.set_String(Cache.DefaultVernWs, "these");
			sense0.MorphoSyntaxAnalysisRA = msa;

			var mdc = Cache.MetaDataCacheAccessor as IFwMetaDataCacheManaged;
			Assert.IsNotNull(mdc);
			var flidCustom = mdc.AddCustomField("LexEntry", "Long Text", CellarPropertyType.OwningAtomic, StTextTags.kClassId);
			var hvoText = Cache.DomainDataByFlid.MakeNewObject(StTextTags.kClassId, entry0.Hvo, flidCustom, -2);
			var text = Cache.ServiceLocator.GetInstance<IStTextRepository>().GetObject(hvoText);

			var para1 = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
			text.ParagraphsOS.Add(para1);
			para1.StyleName = "Numbered List";
			para1.Contents = Cache.TsStrFactory.MakeString("This is the first paragraph.", Cache.DefaultAnalWs);
			var para2 = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
			text.ParagraphsOS.Add(para2);
			para2.StyleName = "Numbered List";
			para2.Contents = Cache.TsStrFactory.MakeString("This is the second paragraph.", Cache.DefaultAnalWs);
			var para3 = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
			text.ParagraphsOS.Add(para3);
			para3.StyleName = "Numbered List";
			para3.Contents = Cache.TsStrFactory.MakeString("This is the third paragraph.", Cache.DefaultAnalWs);
			var para4 = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
			text.ParagraphsOS.Add(para4);
			para4.StyleName = "Numbered List";
			para4.Contents = Cache.TsStrFactory.MakeString("This is the fourth paragraph.", Cache.DefaultAnalWs);

			return flidCustom;
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// LIFT Import:  test import of custom fields representing StText data with conflicts,
		/// using the "Keep New" option.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestLDMLMigration()
		{
			var projectFolder = Path.GetTempPath();
			var testLiftDataSource = Path.Combine(DirectoryFinder.FwSourceDirectory,
												  "LexText/LexTextControls/LexTextControlsTests/LDML-11723");
			var testLiftDataPath = Path.Combine(DirectoryFinder.FwSourceDirectory,
												"LexText/LexTextControls/LexTextControlsTests/LDML-11723-test");

			var sLiftDataFile = Path.Combine(testLiftDataPath, "LDML-11723.lift");
			var sLiftRangesFile = Path.Combine(testLiftDataPath, "LDML-11723.lift-ranges");
			var sWSfilesPath = Path.Combine(testLiftDataPath, "WritingSystems");
			var enLdml = Path.Combine(sWSfilesPath, "en.ldml");
			var sehLdml = Path.Combine(sWSfilesPath, "seh.ldml");
			var esLdml = Path.Combine(sWSfilesPath, "es.ldml");
			var xkalLdml = Path.Combine(sWSfilesPath, "x-kal.ldml");
			var qaaxkalLdml = Path.Combine(sWSfilesPath, "qaa-x-kal.ldml");
			var qaaIPAxkalLdml = Path.Combine(sWSfilesPath, "qaa-fonipa-x-kal.ldml");
			var qaaPhonemicxkalLdml = Path.Combine(sWSfilesPath, "qaa-fonipa-x-kal-emic.ldml");

			LdmlFileBackup.CopyDirectory(testLiftDataSource, testLiftDataPath);

			//Make all files writable
			// don't want to copy readonly property.
			File.SetAttributes(sLiftDataFile, FileAttributes.Normal);
			File.SetAttributes(sLiftRangesFile, FileAttributes.Normal);
			File.SetAttributes(enLdml, FileAttributes.Normal);
			File.SetAttributes(sehLdml, FileAttributes.Normal);
			File.SetAttributes(esLdml, FileAttributes.Normal);
			File.SetAttributes(xkalLdml, FileAttributes.Normal);
			File.SetAttributes(qaaIPAxkalLdml, FileAttributes.Normal);
			File.SetAttributes(qaaPhonemicxkalLdml, FileAttributes.Normal);

			var flexImporter = new FlexLiftMerger(Cache, FlexLiftMerger.MergeStyle.MsKeepBoth, true);
			var parser = new LiftParser<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>(flexImporter);

			//Mirgrate the LDML files and lang names
			flexImporter.LdmlFilesMigration(testLiftDataPath, sLiftDataFile, sLiftRangesFile);

			//Verify the migration worked
			// Verify the data file exists
			Assert.That(File.Exists(sLiftDataFile));
			// Verify the ranges file exists
			Assert.That(File.Exists(sLiftRangesFile));

			// Verify that en.ldml is unchanged.
			Assert.That(File.Exists(enLdml));
			// Verify that seh.ldml is unchanged.
			Assert.That(File.Exists(sehLdml));
			// Verify that es.ldml is unchanged.
			Assert.That(File.Exists(esLdml));
			// Verify that qaa-fonipa-x-kal.ldml is unchanged.
			Assert.That(File.Exists(qaaIPAxkalLdml));
			// Verify that qaa-fonipa-x-kal-emic.ldml is unchanged.
			Assert.That(File.Exists(qaaPhonemicxkalLdml));

			// Verify that x-kal.ldml no longer exists
			Assert.That(!File.Exists(xkalLdml));
			// Verify that x-kal.ldml is renamed to qaa-x-kal and content changed
			Assert.That(File.Exists(qaaxkalLdml));

			//Verify qaa-x-kal.ldml file has correct changes in it.
			VerifyKalabaLdmlFile(qaaxkalLdml);

			//Verify LDML 11723.lift file has correct changes in it.
			VerifyLiftDataFile(sLiftDataFile);

			//Verify LDML 11723.lift file has correct changes in it.
			VerifyLiftRangesFile(sLiftRangesFile);

			//Delete the files that were converted to the new lang names.
			LdmlFileBackup.DeleteDirectory(testLiftDataPath);
		}

		private void VerifyLiftRangesFile(string sLiftRangesFile)
		{
			var xdoc = new XmlDocument();
			xdoc.Load(sLiftRangesFile);
			var data = XElement.Parse(xdoc.InnerXml);
			var range = data.XPathSelectElement("//*[name()='range' and @id='domain-type']");
			var rangeElement = range.XPathSelectElement("//*[name()='range-element' and @id='Kalaba anatomy']");

			XAttribute attr;
			XElement span;
			XElement text;

			var label = rangeElement.XPathSelectElement("label");
			int i = 0;
			foreach (var form in label.XPathSelectElements("form"))
			{
				if (i == 0)
				{
					attr = form.Attribute("lang"); Assert.IsNotNull(attr); //en
					Assert.IsTrue(attr.Value.Equals("en"));
					text = form.XPathSelectElement("text");
					Assert.IsTrue(text.Value.Equals("anatomy"));
				}
				if (i == 1)
				{
					attr = form.Attribute("lang"); Assert.IsNotNull(attr); //en
					Assert.IsTrue(attr.Value.Equals("qaa-x-kal"));
					text = form.XPathSelectElement("text");
					Assert.IsTrue(text.Value.Equals("Kalaba anatomy"));
				}
				i++;
			}

			var abbrev = rangeElement.XPathSelectElement("abbrev");
			i = 0;
			foreach (var form in abbrev.XPathSelectElements("form"))
			{
				if (i == 0)
				{
					attr = form.Attribute("lang"); Assert.IsNotNull(attr); //en
					Assert.IsTrue(attr.Value.Equals("en"));
					text = form.XPathSelectElement("text");
					Assert.IsTrue(text.Value.Equals("Anat"));
				}
				if (i == 1)
				{
					attr = form.Attribute("lang"); Assert.IsNotNull(attr); //en
					Assert.IsTrue(attr.Value.Equals("qaa-x-kal"));
					text = form.XPathSelectElement("text");
					Assert.IsTrue(text.Value.Equals("Kalaba Anat"));
				}
				i++;
			}
			var description = rangeElement.XPathSelectElement("description");
			i = 0;
			foreach (var form in description.XPathSelectElements("form"))
			{
				if (i == 0)
				{
					attr = form.Attribute("lang"); Assert.IsNotNull(attr); //en
					Assert.IsTrue(attr.Value.Equals("qaa-x-kal"));
					text = form.XPathSelectElement("text");
					Assert.IsTrue(text.Value.Equals("Kalaba anatomy definition"));
				}
				i++;
			}
		}

		private void VerifyLiftDataFile(string sLiftDataFile)
		{
			var xdoc = new XmlDocument();
			xdoc.Load(sLiftDataFile);
			var data = XElement.Parse(xdoc.InnerXml);
			VerifyFirstLexEntry(data);
			VerifySecondLexEntry(data);
		}

		private void VerifyFirstLexEntry(XElement data)
		{
			var entry = data.XPathSelectElement("//*[name()='entry' and @guid='a9628929-4561-4afc-b097-88c9bb6df5e9']");
			var lexUnitForm = entry.XPathSelectElement("lexical-unit/form");
			var attr = lexUnitForm.Attribute("lang"); Assert.IsNotNull(attr); //lang
			Assert.IsTrue(attr.Value.Equals("qaa-x-kal"));

			var definition = entry.XPathSelectElement("sense/definition");
			XElement text;
			XElement span;
			int i = 0;
			foreach (var form in definition.XPathSelectElements("form"))
			{
				if (i == 0)
				{
					attr = form.Attribute("lang"); Assert.IsNotNull(attr); //en
					Assert.IsTrue(attr.Value.Equals("en"));
					span = form.XPathSelectElement("text/span");
					attr = span.Attribute("lang"); Assert.IsNotNull(attr); //qaa-x-kal
					Assert.IsTrue(attr.Value.Equals("qaa-x-kal"));
				}
				if (i == 1)
				{
					attr = form.Attribute("lang"); Assert.IsNotNull(attr); //qaa-x-kal
					Assert.IsTrue(attr.Value.Equals("qaa-x-kal"));
					span = form.XPathSelectElement("text/span");
					attr = span.Attribute("lang"); Assert.IsNotNull(attr); //en
					Assert.IsTrue(attr.Value.Equals("en"));
				}
				if (i == 2)
				{
					attr = form.Attribute("lang"); Assert.IsNotNull(attr); //es
					Assert.IsTrue(attr.Value.Equals("es"));
				}
				i++;
			}
		}

		private void VerifySecondLexEntry(XElement data)
		{
			var entry = data.XPathSelectElement("//*[name()='entry' and @guid='fa6a7bf7-1007-4e33-95b5-663335a12a98']");
			var lexUnitForm = entry.XPathSelectElement("lexical-unit");
			XAttribute attr;
			XElement text;
			XElement span;
			int i = 0;
			foreach (var form in lexUnitForm.XPathSelectElements("form"))
			{
				if (i == 0)
				{
					attr = form.Attribute("lang");
					Assert.IsNotNull(attr); //en
					Assert.IsTrue(attr.Value.Equals("qaa-x-kal"));
				}
				if (i == 1)
				{
					attr = form.Attribute("lang"); Assert.IsNotNull(attr); //qaa-x-kal
					Assert.IsTrue(attr.Value.Equals("qaa-fonipa-x-kal"));
				}
				i++;
			}

			XElement glossText;
			var sense = entry.XPathSelectElement("sense");
			i = 0;
			foreach (var gloss in sense.XPathSelectElements("gloss"))
			{
				if (i == 0)
				{
					attr = gloss.Attribute("lang"); Assert.IsNotNull(attr); //qaa-x-kal
					Assert.IsTrue(attr.Value.Equals("qaa-x-kal"));
					glossText = gloss.XPathSelectElement("text");Assert.IsNotNull(glossText);
					Assert.IsTrue(glossText.Value.Equals("KalabaGloss"));
				}
				if (i == 1)
				{
					attr = gloss.Attribute("lang"); Assert.IsNotNull(attr);
					Assert.IsTrue(attr.Value.Equals("en"));
					glossText = gloss.XPathSelectElement("text"); Assert.IsNotNull(glossText);
					Assert.IsTrue(glossText.Value.Equals("EnglishGLoss"));
				}
				if (i == 2)
				{
					attr = gloss.Attribute("lang"); Assert.IsNotNull(attr);
					Assert.IsTrue(attr.Value.Equals("es"));
					glossText = gloss.XPathSelectElement("text"); Assert.IsNotNull(glossText);
					Assert.IsTrue(glossText.Value.Equals("SpanishGloss"));
				}
				i++;
			}

			var definitionForm = entry.XPathSelectElement("sense/definition/form");
			attr = definitionForm.Attribute("lang"); Assert.IsNotNull(attr); //en
			Assert.IsTrue(attr.Value.Equals("qaa-x-kal"));
			var definitionText = entry.XPathSelectElement("sense/definition/form/text");
			i = 0;
			foreach (var spanInDefn in definitionText.XPathSelectElements("span"))
			{
				if (i == 0)
				{
					attr = spanInDefn.Attribute("lang"); Assert.IsNotNull(attr); //en
					Assert.IsTrue(attr.Value.Equals("qaa-fonipa-x-kal"));
					Assert.IsTrue(spanInDefn.Value.Equals("KalabaIPAspan"));
				}
				else if (i == 1)
				{
					attr = spanInDefn.Attribute("lang"); Assert.IsNotNull(attr); //en
					Assert.IsTrue(attr.Value.Equals("en"));
					Assert.IsTrue(spanInDefn.Value.Equals("EnglishSpan"));
				}
				else if (i == 2)
				{
					attr = spanInDefn.Attribute("lang"); Assert.IsNotNull(attr); //en
					Assert.IsTrue(attr.Value.Equals("es"));
					Assert.IsTrue(spanInDefn.Value.Equals("SpanishSpan"));
				}
				else if (i == 3)
				{
					attr = spanInDefn.Attribute("lang"); Assert.IsNotNull(attr); //en
					Assert.IsTrue(attr.Value.Equals("qaa-fonipa-x-kal-emic"));
					Assert.IsTrue(spanInDefn.Value.Equals("KalabaPhonemic"));
				}
				else if (i == 4)
				{
					attr = spanInDefn.Attribute("lang"); Assert.IsNotNull(attr); //en
					Assert.IsTrue(attr.Value.Equals("qaa-x-Lomwe"));
					Assert.IsTrue(spanInDefn.Value.Equals("Lomwe Span"));
				}
				else if (i == 5)
				{
					attr = spanInDefn.Attribute("lang"); Assert.IsNotNull(attr); //en
					Assert.IsTrue(attr.Value.Equals("qaa-x-AveryLon"));
					Assert.IsTrue(spanInDefn.Value.Equals("AveryLongWSName span"));
				}
				i++;
			}
		}

		private void VerifyKalabaLdmlFile(string qaaxkalLdml)
		{
			var xdoc = new XmlDocument();
			xdoc.Load(qaaxkalLdml);
			var data = XElement.Parse(xdoc.InnerXml);

			var language = data.XPathSelectElement("//*[name()='language']");
			var attr = language.Attribute("type");
			Assert.IsNotNull(attr, "The ldml file for Kalaba should have a language element with at type");
			Assert.IsTrue(attr.Value.Equals("qaa"), "Language type attribute should be 'qaa'.");

			var variant = data.XPathSelectElement("//*[name()='variant']");
			attr = variant.Attribute("type");
			Assert.IsNotNull(attr, "The ldml file for Kalaba should have a language element with at type");
			Assert.IsTrue(attr.Value.Equals("x-kal"), "Variante type attribute should be 'x-kal'.");
		}

		private static void PrepareStore(string path)
		{
			if (Directory.Exists(path))
			{
				foreach (string file in Directory.GetFiles(path))
					File.Delete(file);
			}
			else
			{
				Directory.CreateDirectory(path);
			}
		}
	}

	class CustomFieldData
	{
		/// <summary>
		///
		/// </summary>
		internal String CustomFieldname;

		/// <summary>
		///
		/// </summary>
		internal String StringFieldText;

		/// <summary>
		///
		/// </summary>
		internal String StringFieldWs;

		/// <summary>
		///
		/// </summary>
		internal CellarPropertyType CustomFieldType;

		/// <summary>
		///
		/// </summary>
		internal List<String> MultiUnicodeStrings = new List<string>();

		/// <summary>
		///
		/// </summary>
		internal List<String> MultiUnicodeWss	= new List<string>();

		/// <summary>
		///
		/// </summary>
		internal int IntegerValue;

		internal String GenDateLiftFormat;

		internal String cmPossibilityNameRA;

		internal List<String> cmPossibilityNamesRS = new List<String>();
	}
}
