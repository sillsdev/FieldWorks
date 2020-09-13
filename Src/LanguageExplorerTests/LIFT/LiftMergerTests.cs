// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using LanguageExplorer.LIFT;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.IO;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using SIL.Lift.Migration;
using SIL.Lift.Parsing;
using SIL.TestUtilities;
using SIL.WritingSystems;
using SIL.Xml;

namespace LanguageExplorerTests.LIFT
{
	/// <summary>
	/// Test the LIFT import functionality provided by the FlexLiftMerger class in conjunction
	/// with the Palaso.Lift library.
	/// </summary>
	[TestFixture]
	public class LiftMergerTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private static readonly string[] _componentTest = {
				"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>",
				"<lift producer=\"SIL.FLEx 7.2.4.41003\" version=\"0.13\">",
				"  <header>",
				"    <fields>",
				"    </fields>",
				"  </header>",
				"  <entry dateCreated=\"2011-06-27T21:45:52Z\" dateModified=\"2011-06-29T14:57:28Z\" id=\"do_be4eb9fd-58fd-49fe-a8ef-e13a96646806\" guid=\"be4eb9fd-58fd-49fe-a8ef-e13a96646806\">"
				,
				"    <lexical-unit>",
				"      <form lang=\"fr\"><text>do</text></form>",
				"    </lexical-unit>",
				"    <trait  name=\"morph-type\" value=\"stem\"/>",
				"<relation type=\"Compare\" ref=\"todo_10af904a-7395-4a37-a195-44001127ae40\"/>",
				"<relation type=\"Compare\" ref=\"to_10af904a-7395-4a37-a195-44001127ae40\"/>",
				"    <sense id=\"3e0ae703-db7f-4687-9cf5-481524095905\">",
				"    </sense>",
				"  </entry>",
				"  <entry dateCreated=\"2011-06-29T15:58:03Z\" dateModified=\"2011-06-29T15:58:03Z\" id=\"todo_10af904a-7395-4a37-a195-44001127ae40\" guid=\"10af904a-7395-4a37-a195-44001127ae40\">"
				,
				"    <lexical-unit>",
				"      <form lang=\"fr\"><text>todo</text></form>",
				"    </lexical-unit>",
				"    <trait  name=\"morph-type\" value=\"stem\"/>",
				"<relation type=\"Compare\" ref=\"to_9bfa6fc4-0a2d-44ff-9c2c-91460ef3c856\"/>",
				"<relation type=\"Compare\" ref=\"do_be4eb9fd-58fd-49fe-a8ef-e13a96646806\"/>",
				"    <sense id=\"2759532a-26db-4850-9cba-b3684f0a3f5f\">",
				"    </sense>",
				"  </entry>",
				"  <entry dateCreated=\"2011-06-29T15:58:03Z\" dateModified=\"2011-06-29T15:58:03Z\" id=\"to_9bfa6fc4-0a2d-44ff-9c2c-91460ef3c856\" guid=\"9bfa6fc4-0a2d-44ff-9c2c-91460ef3c856\">"
				,
				"    <lexical-unit>",
				"      <form lang=\"fr\"><text>to</text></form>",
				"    </lexical-unit>",
				"    <trait  name=\"morph-type\" value=\"stem\"/>",
				"<relation type=\"Compare\" ref=\"todo_10af904a-7395-4a37-a195-44001127ae40\"/>",
				"<relation type=\"Compare\" ref=\"do_be4eb9fd-58fd-49fe-a8ef-e13a96646806\"/>",
				"    <sense id=\"2759532a-26db-4850-9cba-b3684f0a3f5f\">",
				"    </sense>",
				"  </entry>",
				"</lift>"
			};

		// KEEPERS, below this point.
		private static readonly string[] _LT12948Test2 = {
				"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>",
				"<lift producer=\"SIL.FLEx 7.2.4.41003\" version=\"0.13\">",
				"  <header>",
				"    <fields>",
				"    </fields>",
				"  </header>",
				"  <entry dateCreated=\"2011-06-27T21:45:52Z\" dateModified=\"2011-06-29T14:57:28Z\" id=\"do_be4eb9fd-58fd-49fe-a8ef-e13a96646806\" guid=\"be4eb9fd-58fd-49fe-a8ef-e13a96646806\">"
				,
				"    <lexical-unit>",
				"      <form lang=\"fr\"><text>do</text></form>",
				"    </lexical-unit>",
				"    <trait  name=\"morph-type\" value=\"stem\"/>",
				"    <sense id=\"3e0ae703-db7f-4687-9cf5-481524095905\">",
				"    </sense>",
				"  </entry>",
				"  <entry dateCreated=\"2011-06-29T15:58:03Z\" dateModified=\"2011-06-29T15:58:03Z\" id=\"todo_10af904a-7395-4a37-a195-44001127ae40\" guid=\"10af904a-7395-4a37-a195-44001127ae40\">"
				,
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
				"  <entry dateCreated=\"2011-06-29T15:58:03Z\" dateModified=\"2011-06-29T15:58:03Z\" id=\"to_9bfa6fc4-0a2d-44ff-9c2c-91460ef3c856\" guid=\"9bfa6fc4-0a2d-44ff-9c2c-91460ef3c856\">"
				,
				"    <lexical-unit>",
				"      <form lang=\"fr\"><text>todo</text></form>",
				"    </lexical-unit>",
				"    <trait  name=\"morph-type\" value=\"stem\"/>",
				"    <sense id=\"2759532a-26db-4850-9cba-b3684f0a3f5f\">",
				"    </sense>",
				"  </entry>",
				"</lift>"
			};

		private static readonly string[] _liftData8 = {
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

		private static readonly string[] _minimalLiftData = {
			"<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
			"<lift producer=\"SIL.FLEx 7.3.2.41302\" version=\"0.13\">",
			"<header>",
			"<fields/>",
			"</header>",
			"<entry dateCreated=\"2013-01-29T08:53:26Z\" dateModified=\"2013-01-29T08:10:28Z\" id=\"baba_aef5e807-c841-4f35-9591-c8a998dc2465\" guid=\"aef5e807-c841-4f35-9591-c8a998dc2465\">",
			"<lexical-unit>",
			"<form lang=\"fr\"><text>baba baba</text></form>",
			"</lexical-unit>",
			"<sense id=\"$guid2\" dateCreated=\"2013-01-29T08:55:26Z\" dateModified=\"2013-01-29T08:15:28Z\">",
			"<gloss lang=\"en\"><text>dad</text></gloss>",
			"</sense>",
			"</entry>",
			"</lift>"
		};

		private static readonly string[] _treeLiftRange = {
				"<?xml version='1.0' encoding='UTF-8'?>",
				"<lift-ranges>",
				"<range id='lexical-relation'>",
				"<range-element id='Part' guid='b764ce50-ea5e-11de-864f-0013722f8dec'>",
				"<label>",
				"<form lang='en'><text>Part</text></form>",
				"</label>",
				"<abbrev>",
				"<form lang='en'><text>pt</text></form>",
				"</abbrev>",
				"<description>",
				"<form lang='en'><text>A part/whole relation establishes a link between the sense for the whole (e.g., room), and senses for the parts (e.g., ceiling, wall, floor).</text></form>",
				"</description>",
				"<field type='reverse-label'>",
				"<form lang='en'><text>Whole</text></form>",
				"</field>",
				"<field type='reverse-abbrev'>",
				"<form lang='en'><text>wh</text></form>",
				"</field>",
				"<trait  name='referenceType' value='3'/>",
				"</range-element>",
				"</range>",
				"</lift-ranges>"
			};

		// modified LIFT with same entries 'Bother' and 'me' related using relation type Twain and Twin, expects a lexical-relation range in a ranges file
		private static readonly string[] _newWithPair = {
				"<?xml version='1.0' encoding='UTF-8' ?>",
				"<lift producer='SIL.FLEx 8.0.5.41540' version='0.13'>",
				"<header>",
				"<ranges>",
				"<range id='lexical-relation' href='???'/>",
				"</ranges>",
				"<fields/>",
				"</header>",
				"<entry dateCreated='2013-09-24T18:57:02Z' dateModified='2013-09-24T19:00:14Z' id='Bug_84338803-89e0-4d74-b175-fcbb2eb23ea5' guid='84338803-89e0-4d74-b175-fcbb2eb23ea5'>",
				"<lexical-unit>",
				"<form lang='fr'><text>Bother</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='c2b4fe44-a3d9-4a42-a87c-8e174593fb30'>",
				"<relation type='Twain' ref='de2fcb48-319a-48cf-bfea-0f25b9f38b31'/>",
				"</sense>",
				"</entry>",
				"<entry dateCreated='2013-09-24T18:57:11Z' dateModified='2013-09-24T19:00:14Z' id='me_1bbaccee-d640-4cae-9f80-0563225b93ca' guid='1bbaccee-d640-4cae-9f80-0563225b93ca'>",
				"<lexical-unit>",
				"<form lang='fr'><text>me</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='de2fcb48-319a-48cf-bfea-0f25b9f38b31'>",
				"<relation type='Twin' ref='c2b4fe44-a3d9-4a42-a87c-8e174593fb30'/>",
				"</sense>",
				"</entry>",
				"</lift>"
			};

		private static readonly string[] _twoEntryWithVariantComplexFormLift = {
				"<?xml version='1.0' encoding='utf-8'?>",
				"<lift producer='SIL.FLEx 8.0.4.41520' version='0.13'>",
				"	<header></header>",
				"	<entry dateCreated='2013-09-20T19:34:26Z' dateModified='2013-09-20T19:34:26Z' guid='40a9574d-2d13-4d30-9eab-9a6d84bf29f8' id='e_40a9574d-2d13-4d30-9eab-9a6d84bf29f8'>",
				"		<lexical-unit>",
				"			<form lang='fr'>",
				"				<text>e</text>",
				"			</form>",
				"		</lexical-unit>",
				"		<relation order='0' ref='a_ac828ef4-9a18-4802-b095-11cca00947db' type='_component-lexeme'>",
				"			<trait name='variant-type' value='' />",
				"		</relation>",
				"		<trait name='morph-type' value='stem' />",
				"	</entry>",
				"	<entry dateCreated='2013-09-20T16:25:23Z' dateModified='2013-09-20T19:01:59Z' guid='ac828ef4-9a18-4802-b095-11cca00947db' id='a_ac828ef4-9a18-4802-b095-11cca00947db'>",
				"		<lexical-unit>",
				"			<form lang='fr'>",
				"				<text>a</text>",
				"			</form>",
				"		</lexical-unit>",
				"		<sense id='91eb7dc2-4057-4e7c-88c3-a81536a38c3e' />",
				"		<trait name='morph-type' value='stem' />",
				"	</entry>",
				"</lift>"
			};

		//All custom CmPossibility lists names and Guids
		private Dictionary<string, Guid> m_customListNamesAndGuids = new Dictionary<string, Guid>();
		private IFwMetaDataCacheManaged m_mdc;
		private const string sLiftData3b = "<entry dateCreated=\"2011-03-01T22:27:46Z\" dateModified=\"{0}\" guid=\"67113a7f-e448-43e7-87cf-6d3a46ee10ec\" id=\"greenhouse_67113a7f-e448-43e7-87cf-6d3a46ee10ec\">";
		private string MockProjectFolder { get; set; }
		private string MockLinkedFilesFolder { get; set; }
		private int m_audioWsCode;
		private Dictionary<string, int> m_customFieldEntryIds = new Dictionary<string, int>();
		private Dictionary<string, int> m_customFieldSenseIds = new Dictionary<string, int>();
		private Dictionary<string, int> m_customFieldAllomorphsIds = new Dictionary<string, int>();
		private Dictionary<string, int> m_customFieldExampleSentencesIds = new Dictionary<string, int>();

		#region Overrides of LcmTestBase

		public override void TestSetup()
		{
			base.TestSetup();
			Cache.LangProject.LexDbOA.ReferencesOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			const string mockProjectName = "xxyyzProjectFolderForLIFTImport";
			MockProjectFolder = Path.Combine(Path.GetTempPath(), mockProjectName);
			MockLinkedFilesFolder = Path.Combine(MockProjectFolder, LcmFileHelper.ksLinkedFilesDir);
			if (Directory.Exists(MockLinkedFilesFolder))
			{
				Directory.Delete(MockLinkedFilesFolder, true);
			}
			Directory.CreateDirectory(MockLinkedFilesFolder);
			Cache.LangProject.LinkedFilesRootDir = MockLinkedFilesFolder;
			var writingSystemManager = Cache.ServiceLocator.WritingSystemManager;
			var languageSubtag = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Language;
			var audioWs = writingSystemManager.Create(languageSubtag, WellKnownSubtags.AudioScript, null, new VariantSubtag[] { WellKnownSubtags.AudioPrivateUse });
			if (writingSystemManager.TryGet(audioWs.LanguageTag, out var existingAudioWs))
			{
				m_audioWsCode = existingAudioWs.Handle;
			}
			else
			{
				audioWs.IsVoice = true;
				// should already be so? Make sure.
				writingSystemManager.Set(audioWs); // gives it a handle
				m_audioWsCode = audioWs.Handle;
			}
		}
		#endregion

		private static string LiftFolder { get; set; }

		private static readonly Random TestNameRandomizer = new Random((int)DateTime.Now.Ticks);

		private static string CreateInputFile(IList<string> data)
		{
			LiftFolder = Path.Combine(Path.GetTempPath(), "xxyyTestLIFTImport");
			if (Directory.Exists(LiftFolder))
			{
				Directory.Delete(LiftFolder, true);
			}
			Directory.CreateDirectory(LiftFolder);
			var path = Path.Combine(LiftFolder, $"LiftTest{TestNameRandomizer.Next(1000)}.lift");
			CreateLiftInputFile(path, data);
			return path;
		}

		private static string CreateInputRangesFile(IList<string> data)
		{
			LiftFolder = Path.Combine(Path.GetTempPath(), "xxyyTestLIFTImport");
			Assert.True(Directory.Exists(LiftFolder));
			var path = Path.Combine(LiftFolder, $"LiftTest{TestNameRandomizer.Next(1000)}.lift-ranges");
			CreateLiftInputFile(path, data);
			return path;
		}

		private static void CreateLiftInputFile(string path, IList<string> data)
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
			using (var wrtr = File.CreateText(path))
			{
				foreach (var text in data)
				{
					wrtr.WriteLine(text);
				}
				wrtr.Close();
			}
		}

		private string TryImport(string sOrigFile, int expectedCount)
		{
			return TryImport(sOrigFile, null, MergeStyle.MsKeepBoth, expectedCount);
		}

		private string TryImportWithRanges(string sOrigFile, string sOrigRangesFile, int expectedCount)
		{
			return TryImport(sOrigFile, sOrigRangesFile, MergeStyle.MsKeepBoth, expectedCount);
		}

		private string TryImport(string sOrigFile, string sOrigRangesFile, MergeStyle mergeStyle, int expectedCount, bool trustModificationTimes = true)
		{
			IProgress progressDlg = new DummyProgressDlg();
			var fMigrationNeeded = Migrator.IsMigrationNeeded(sOrigFile);
			var sFilename = fMigrationNeeded ? Migrator.MigrateToLatestVersion(sOrigFile) : sOrigFile;
			var flexImporter = new FlexLiftMerger(Cache, mergeStyle, trustModificationTimes);
			var parser = new LiftParser<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>(flexImporter);
			flexImporter.LiftFile = sOrigFile;

			//The following are the calls to import the Ranges file and then the Data file.
			if (!string.IsNullOrEmpty(sOrigRangesFile))
			{
				flexImporter.LoadLiftRanges(sOrigRangesFile);
			}
			var cEntries = parser.ReadLiftFile(sFilename);

			Assert.AreEqual(expectedCount, cEntries);
			if (fMigrationNeeded)
			{
				File.Delete(sFilename);
			}
			flexImporter.ProcessPendingRelations(progressDlg);
			var logfile = flexImporter.DisplayNewListItems(sOrigFile, cEntries);

			return logfile;
		}

		private static void CreateDummyFile(string folder, string filename)
		{
			CreateDummyFile(Path.Combine(Path.Combine(LiftFolder, folder), filename));
		}

		private static void CreateDummyFile(string path)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.Delete(path);
			using (var wrtr = File.CreateText(path))
			{
				wrtr.WriteLine("This is a dummy file used in testing LIFT import");
				wrtr.Close();
			}
		}

		/// <summary>
		/// First test of LIFT import: four simple entries.
		/// (This was also a convenient point to check handling of a lexical relation with
		/// an empty ref attr.)
		/// </summary>
		[Test]
		public void TestLiftImport1()
		{
			string[] liftData1 =
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
			"<relation type=\"_component-lexeme\" ref=\"\">",
			"<trait  name=\"complex-form-type\" value=\"Derivative\"/>",
			"</relation>",
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
			var messageCapture = new MessageCapture();
			MessageBoxUtils.SetMessageBoxAdapter(messageCapture);
			SetWritingSystems("es");

			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			Assert.AreEqual(0, repoEntry.Count);
			Assert.AreEqual(0, repoSense.Count);

			var sOrigFile = CreateInputFile(liftData1);
			CreateDummyFile("pictures", "Desert.jpg");
			var myPicRelativePath = Path.Combine("subfolder", "MyPic.jpg");
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

			Assert.That(messageCapture.Messages, Has.Count.EqualTo(0), "we should not message about an empty-string ref in <relation>");

			Assert.IsTrue(repoEntry.TryGetObject(new Guid("ecfbe958-36a1-4b82-bb69-ca5210355400"), out var entry));
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
			Assert.That(sense0.PicturesOS[0].PictureFileRA.InternalPath, Is.EqualTo(Path.Combine(LcmFileHelper.ksPicturesDir, "Desert.jpg")));
			Assert.That(sense0.PicturesOS[1].PictureFileRA.InternalPath, Is.EqualTo(Path.Combine(LcmFileHelper.ksPicturesDir, myPicRelativePath)));
			VerifyLinkedFileExists(LcmFileHelper.ksPicturesDir, "Desert.jpg");
			VerifyLinkedFileExists(LcmFileHelper.ksPicturesDir, myPicRelativePath);

			Assert.That(entry.PronunciationsOS.Count, Is.EqualTo(1));
			Assert.That(entry.PronunciationsOS[0].MediaFilesOS[0].MediaFileRA.InternalPath, Is.EqualTo(Path.Combine(LcmFileHelper.ksMediaDir, "Sleep Away.mp3")));
			VerifyLinkedFileExists(LcmFileHelper.ksMediaDir, "Sleep Away.mp3");
			VerifyLinkedFileExists(LcmFileHelper.ksMediaDir, "hombre634407358826681759.wav");
			VerifyLinkedFileExists(LcmFileHelper.ksMediaDir, "male adult634407358826681760.wav");
			VerifyLinkedFileExists(LcmFileHelper.ksOtherLinkedFilesDir, "SomeFile.txt");

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

		private void VerifyLinkedFileExists(string folder, string filename)
		{
			Assert.That(File.Exists(Path.Combine(Path.Combine(MockLinkedFilesFolder, folder), filename)), Is.True);
		}

		/// <summary>
		/// Second test of LIFT import: more complex and variant entries.
		/// </summary>
		[Test]
		public void TestLiftImport2()
		{
			string[] liftData2 = {
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
			Assert.AreEqual("en", Cache.LangProject.CurAnalysisWss);
			Assert.AreEqual("fr", Cache.LangProject.CurVernWss);
			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			Assert.AreEqual(0, repoEntry.Count);
			Assert.AreEqual(0, repoSense.Count);

			var sOrigFile = CreateInputFile(liftData2);
			var logFile = TryImport(sOrigFile, 4);
			File.Delete(sOrigFile);
			Assert.IsNotNull(logFile);
			File.Delete(logFile);
			Assert.AreEqual(4, repoEntry.Count);
			Assert.AreEqual(3, repoSense.Count);

			Assert.IsTrue(repoEntry.TryGetObject(new Guid("69ccc807-f3d1-44cb-b79a-e8d416b0d7c1"), out var entry));
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
			{
				env = x;
			}
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

		[Test]
		public void TestImportOutOfOrderRelation()
		{
			string[] outOfOrderRelation = {
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
				"<sense id=\"house_f722992a-cfdc-41ec-9c46-f927f02d68ef\">",
				"<relation type=\"Calendar\" ref=\"house_f722992a-cfdc-41ec-9c46-f927f02d68ef\" order=\"3\"/>",
				"<relation type=\"Calendar\" ref=\"2e827b5e-1558-48fd-b629-1518f1aabba3\" order=\"1\"/>",
				"<grammatical-info value=\"Noun\">",
				"</grammatical-info>",
				"<gloss lang=\"en\"><text>house</text></gloss>",
				"</sense>",
				"<sense id=\"2e827b5e-1558-48fd-b629-1518f1aabba3\">",
				"<relation type=\"Calendar\" ref=\"house_f722992a-cfdc-41ec-9c46-f927f02d68ef\" order=\"3\"/>",
				"<relation type=\"Calendar\" ref=\"2e827b5e-1558-48fd-b629-1518f1aabba3\" order=\"1\"/>",
				"<grammatical-info value=\"Noun\">",
				"</grammatical-info>",
				"<gloss lang=\"en\"><text>shack</text></gloss>",
				"</sense>",
				"</entry>",
				"</lift>"
			};
			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			var repoLrType = Cache.ServiceLocator.GetInstance<ILexRefTypeRepository>();
			Assert.AreEqual(0, repoEntry.Count);
			Assert.AreEqual(0, repoSense.Count);
			Assert.AreEqual(0, repoLrType.Count);

			var sOrigFile = CreateInputFile(outOfOrderRelation);
			var logFile = TryImport(sOrigFile, 1);
			File.Delete(sOrigFile);
			Assert.IsNotNull(logFile);
			File.Delete(logFile);
			Assert.AreEqual(1, repoEntry.Count);
			Assert.AreEqual(2, repoSense.Count);
			Assert.AreEqual(1, repoLrType.Count);
			var lexEntry = repoEntry.AllInstances().First();
			var sense1 = lexEntry.SensesOS[0];
			var lrType = repoLrType.AllInstances().First();
			var lexRefs = lrType.MembersOC;
			Assert.That(lexRefs, Has.Count.EqualTo(1));
			var targets = lexRefs.First().TargetsRS;
			Assert.That(targets, Has.Count.EqualTo(2));
			Assert.That(targets.First(), Is.EqualTo(lexEntry.SensesOS[1]), "Targets should be ordered according to Order attribute");
			Assert.That(targets.Skip(1).First(), Is.EqualTo(sense1), "Both senses should be present in targets");
		}

		/// <summary>
		/// Third test of LIFT import: minimally specified complex and variant entries.
		/// </summary>
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

			Assert.IsTrue(repoEntry.TryGetObject(new Guid("69ccc807-f3d1-44cb-b79a-e8d416b0d7c1"), out var entry));
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

		private static string[] GetLift3Strings(string date)
		{
			string[] liftData3A = {
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
			string[] liftData3C = {
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
			var modString = string.Format(sLiftData3b, date);
			return liftData3A.Concat(new[] { modString }).Concat(liftData3C).ToArray();
		}

		[Test]
		public void LiftDoesNotImportTabs()
		{
			string[] tabInput = {"<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
				"<lift producer=\"SIL.FLEx 7.0.1.40602\" version=\"0.13\">",
				"<header>",
				"<ranges/>",
				"<fields/>",
				"</header>",
				"<entry dateCreated=\"2011-03-01T18:09:46Z\" dateModified=\"2011-03-01T18:30:07Z\" guid=\"ecfbe958-36a1-4b82-bb69-ca5210355401\" id=\"hombre_ecfbe958-36a1-4b82-bb69-ca5210355400\">",
				"<lexical-unit>",
				"<form lang=\"fr\"><text>\thombre</text></form>",
				"</lexical-unit>",
				"<trait name=\"morph-type\" value=\"root\"></trait>",
				"<pronunciation>",
				"<form lang=\"fr\"><text>ombre\t1</text></form>",
				"<media href=\"\t\tSleep Away.mp3\">",
				"</media>",
				"</pronunciation>",
				"<sense id=\"hombre_f63f1ccf-3d50-417e-8024-035d999d48bc\">",
				"<grammatical-info value=\"Noun\">",
				"</grammatical-info>",
				"<gloss lang=\"en\"><text>\t\tman</text></gloss>",
				"<definition>",
				"<form lang=\"en\"><text>",
				"\tmale adult\thuman\t<span href=\"file://others/SomeFile.txt\" class=\"Hyperlink\">link</span></text></form>",
				"</definition>",
				"</sense>",
				"</entry>",
				"</lift>"
			};
			var sOrigFile = CreateInputFile(tabInput);
			var logFile = TryImport(sOrigFile, null, MergeStyle.MsKeepNew, 1);
			Assert.IsNotNull(logFile);
			File.Delete(logFile);
			File.Delete(sOrigFile);

			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			Assert.IsTrue(repoEntry.TryGetObject(new Guid("ecfbe958-36a1-4b82-bb69-ca5210355401"), out var entry));
			Assert.That(entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text, Is.EqualTo("hombre"));

			Assert.That(entry.SensesOS[0].Gloss.AnalysisDefaultWritingSystem.Text, Is.EqualTo("  man"));
			Assert.That(entry.SensesOS[0].Definition.AnalysisDefaultWritingSystem.Text, Is.EqualTo("\u2028 male adult human link"));
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

			Assert.IsTrue(repoEntry.TryGetObject(new Guid("67113a7f-e448-43e7-87cf-6d3a46ee10ec"), out var entry));
			Assert.AreEqual(1, entry.EntryRefsOS.Count);

			Assert.IsNotNull(logFile);
			File.Delete(logFile);
			// Importing twice should not create duplicates. Note that we use a slightly different date here
			sOrigFile = CreateInputFile(GetLift3Strings("2011-03-01T22:30:00Z"));
			logFile = TryImport(sOrigFile, null, MergeStyle.MsKeepNew, 4);
			Assert.IsNotNull(logFile);
			File.Delete(logFile);
			File.Delete(sOrigFile);

			Assert.AreEqual(4, repoEntry.Count);
			Assert.AreEqual(3, repoSense.Count);

			Assert.IsTrue(repoEntry.TryGetObject(new Guid("67113a7f-e448-43e7-87cf-6d3a46ee10ec"), out entry));
			Assert.AreEqual(1, entry.EntryRefsOS.Count);

		}

		/// <summary>
		/// Fourth test of LIFT import: test importing multi-paragraph text with various CR/LF
		/// combinations.
		/// (This was also a convenient point to test that we get a warning when creating
		/// a LER with a missing component.)
		/// </summary>
		[Test]
		public void TestLiftImport4()
		{
			string[] liftData4 = {
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
				"<relation type=\"_component-lexeme\" ref=\"nonsence_object_ID\">",
				"<trait  name=\"complex-form-type\" value=\"Derivative\"/>",
				"</relation>",
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
			// Setup
			var messageCapture = new MessageCapture();
			MessageBoxUtils.SetMessageBoxAdapter(messageCapture);
			// ReSharper disable InconsistentNaming
			const string LINE_SEPARATOR = "\u2028";
			var s_newLine = Environment.NewLine;
			const string s_cr = "\r";
			const string s_lf = "\n";
			// ReSharper restore InconsistentNaming

			SetWritingSystems("es");

			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			Assert.AreEqual(0, repoEntry.Count);
			Assert.AreEqual(0, repoSense.Count);

			// Put data in LIFT string
			const int idxModifiedLine = 19;
			// "<form lang=\"en\"><text>male{0}adult{1}human</text></form>",
			var fmtString = liftData4[idxModifiedLine];
			liftData4[idxModifiedLine] = string.Format(fmtString, s_newLine, s_cr, s_lf);

			var sOrigFile = CreateInputFile(liftData4);
			var logFile = TryImport(sOrigFile, 1);
			File.Delete(sOrigFile);
			Assert.IsNotNull(logFile);
			File.Delete(logFile);
			Assert.AreEqual(1, repoEntry.Count);
			Assert.AreEqual(1, repoSense.Count);

			Assert.That(messageCapture.Messages[0], Is.StringContaining("nonsence_object_ID"), "inability to link up bad ref should be reported in message box");

			Assert.IsTrue(repoEntry.TryGetObject(new Guid("ecfbe958-36a1-4b82-bb69-ca5210355400"), out var entry));
			Assert.AreEqual(1, entry.SensesOS.Count);
			Assert.AreEqual(entry.SensesOS[0].Guid, new Guid("f63f1ccf-3d50-417e-8024-035d999d48bc"));
			Assert.IsNotNull(entry.LexemeFormOA);
			Assert.IsNotNull(entry.LexemeFormOA.MorphTypeRA);
			Assert.AreEqual("root", entry.LexemeFormOA.MorphTypeRA.Name.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("hombre", entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text);
			var actualDefn = entry.SensesOS[0].Definition.AnalysisDefaultWritingSystem.Text;
			var expectedXmlDefn = string.Format(fmtString, LINE_SEPARATOR, LINE_SEPARATOR, LINE_SEPARATOR);
			var doc = new XmlDocument();
			doc.LoadXml(expectedXmlDefn);
			var expectedDefn = doc.SelectSingleNode("form/text");
			Assert.IsNotNull(expectedDefn);
			Assert.AreEqual(expectedDefn.InnerText, actualDefn, "Mismatched definition.");
		}

		/// <summary>
		/// LIFT import: Test import of Custom Fields which contain strings
		/// </summary>
		[Test]
		public void TestLiftImport5_CustomFieldsStringsAndMultiUnicode()
		{
			string[] liftData5 = {
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

			var sOrigFile = CreateInputFile(liftData5);

			var logFile = TryImport(sOrigFile, 2);
			File.Delete(sOrigFile);
			Assert.IsNotNull(logFile);
			File.Delete(logFile);
			Assert.AreEqual(2, repoEntry.Count);
			Assert.AreEqual(2, repoSense.Count);

			Assert.IsTrue(repoEntry.TryGetObject(new Guid("7e4e4484-d691-4ffa-8fb1-10cf4941ac14"), out var entry));
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

			Assert.IsNotNull(entry.LexemeFormOA);
			Assert.IsNotNull(entry.LexemeFormOA.MorphTypeRA);
			Assert.AreEqual("stem", entry.LexemeFormOA.MorphTypeRA.Name.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("Babababa", entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text);
			Assert.IsNotNull(sense0.MorphoSyntaxAnalysisRA as IMoStemMsa);
			// ReSharper disable PossibleNullReferenceException
			Assert.IsNotNull((sense0.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA);
			Assert.AreEqual("Noun", (sense0.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA.Name.AnalysisDefaultWritingSystem.Text);
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

			// Allomorph Custom Field Test: MultiString
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

		/// <summary>
		/// LIFT import: Test import of GenDate's and Numbers
		/// </summary>
		[Test]
		public void TestLiftImport6_CustomFieldsNumberGenDate()
		{
			string[] liftData6 = {
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
			SetWritingSystems("fr");

			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			Assert.AreEqual(0, repoEntry.Count);
			Assert.AreEqual(0, repoSense.Count);

			var sOrigFile = CreateInputFile(liftData6);

			var logFile = TryImport(sOrigFile, 1);
			File.Delete(sOrigFile);
			Assert.IsNotNull(logFile);
			File.Delete(logFile);
			Assert.AreEqual(1, repoEntry.Count);
			Assert.AreEqual(1, repoSense.Count);

			Assert.IsTrue(repoEntry.TryGetObject(new Guid("c78f68b9-79d0-4ce9-8b76-baa68a5c8444"), out var entry));
			Assert.AreEqual(1, entry.SensesOS.Count);
			var sense0 = entry.SensesOS[0];
			Assert.AreEqual(sense0.Guid, new Guid("9d6c600b-192a-4eec-980b-a605173ba5e3"));

			Assert.IsNotNull(entry.LexemeFormOA);
			Assert.IsNotNull(entry.LexemeFormOA.MorphTypeRA);
			Assert.AreEqual("stem", entry.LexemeFormOA.MorphTypeRA.Name.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("Baba", entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text);
			Assert.IsNotNull(sense0.MorphoSyntaxAnalysisRA as IMoStemMsa);
			// ReSharper disable PossibleNullReferenceException
			Assert.IsNotNull((sense0.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA);
			Assert.AreEqual("NounPerson", (sense0.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA.Name.AnalysisDefaultWritingSystem.Text);
			// ReSharper restore PossibleNullReferenceException
			Assert.AreEqual("Pops", sense0.Gloss.AnalysisDefaultWritingSystem.Text);

			VerifyCustomFieldsEntry(entry);
			VerifyCustomFieldsSense(sense0);
			var example = sense0.ExamplesOS[0];
			var customData = new CustomFieldData()
			{
				CustomFieldname = "CustmFldExample Int",
				CustomFieldType = CellarPropertyType.Integer,
				IntegerValue = 24
			};
			VerifyCustomFieldExample(example, customData);
			// Allomorph Custom Field Test===== : MultiString
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

		private void VerifyCustomFieldExample(ILexExampleSentence obj, CustomFieldData expectedData)
		{
			m_customFieldExampleSentencesIds = GetCustomFlidsOfObject(obj);
			VerifyCustomField(obj, expectedData, m_customFieldExampleSentencesIds[expectedData.CustomFieldname]);
		}

		private Dictionary<string, int> GetCustomFlidsOfObject(ICmObject obj)
		{
			var customFieldIds2 = new Dictionary<string, int>();
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
			switch (obj)
			{
				case ILexEntry lexEntry:
				{
					Assert.That(lexEntry.LiftResidue, Is.Not.StringContaining(fieldData.CustomFieldname));
					break;
				}
				case ILexSense lexSense:
				{
					Assert.That(lexSense.LiftResidue, Is.Not.StringContaining(fieldData.CustomFieldname));
					break;
				}
				case ILexExampleSentence exampleSentence:
				{
					Assert.That(exampleSentence.LiftResidue, Is.Not.StringContaining(fieldData.CustomFieldname));
					break;
				}
			}

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
						{
							return;
						}
						var tss = GetPossibilityBestAlternative(possibilityHvo, Cache);
						Assert.AreEqual(fieldData.cmPossibilityNameRA, tss.ToString());
					}
					break;
				case CellarPropertyType.ReferenceCollection:
				case CellarPropertyType.ReferenceSequence:
					//"<trait name=\"CustomFld ListMulti\" value=\"Universe, creation\"/>",
					//"<trait name=\"CustomFld ListMulti\" value=\"Sun\"/>",
					var hvos = sda.VecProp(obj.Hvo, flid);
					var count = hvos.Length;
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
					{
						Assert.AreEqual(fieldData.IntegerValue, intVal);
					}
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

		/// <summary>
		/// LIFT Import:  test import of Custom Lists from the Ranges file.
		/// Also test the import of field definitions for custom fields
		/// which contain CmPossibility list data and verify that the data is correct too.
		/// </summary>
		[Test]
		public void TestLiftImport7_CustomLists_and_CustomFieldsWithListData()
		{
			string[] liftData7 = {
			"<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
			"<lift producer=\"SIL.FLEx 7.0.1.40602\" version=\"0.13\">",
			"<header>",
			"<ranges>",
				"<range id=\"semantic-domain-ddp4\" href=\"file://C:/junk.lift-ranges\"/>",
				"<range id=\"CustomCmPossibiltyList\" href=\"file://C:/junk.lift-ranges\"/>",
				"<range id=\"CustomList Number2 \" href=\"file://C:/junk.lift-ranges\"/>",
				"<range id=\"status\" href=\"file://C:/junk.lift-ranges\"/>",
			"</ranges>",
			"<fields>",
			@"<field tag=""ExampleStatus"">",
			@"<form lang=""en""><text></text></form>",
			@"<form lang=""qaa-x-spec""><text>Class=LexExampleSentence; Type=ReferenceAtom; WsSelector=kwsAnal; DstCls=CmPossibility; range=status</text></form>",
			@"</field>",
			"<field tag=\"import-residue\">",
			"<form lang=\"en\"><text>This records residue left over from importing a standard format file into FieldWorks (or LinguaLinks).</text></form>",
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
			@"<example>",
			@"<form lang=""fr""><text>a complex example</text></form>",
			@"<trait name=""do-not-publish-in"" value=""School Dictionary""/>",
			@"<trait name=""ExampleStatus"" value=""Pending""/>",
			@"</example>",
			"</sense>",
			"</entry>",
			"</lift>"
			};
			string[] liftRangeData7 = {
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
			"<range id=\"status\" guid=\"aa99df69-9418-4d37-b9e8-f0b10c696675\">",
				"<range-element id=\"Pending\" guid=\"66705eee-d7db-47c6-964c-973d5830566c\">",
				"<label>",
				"<form lang=\"en\"><text>Pending</text></form>",
				"</label>",
				"<description>",
				"<form lang=\"en\"><text>Not done</text></form>",
				"</description>",
				"</range-element>",
			"</range>",
			"</lift-ranges>"
			};
			SetWritingSystems("fr");

			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			Cache.LangProject.StatusOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			Assert.AreEqual(0, repoEntry.Count);
			Assert.AreEqual(0, repoSense.Count);

			//Create the LIFT data file
			var sOrigFile = CreateInputFile(liftData7);
			//Create the LIFT ranges file
			var sOrigRangesFile = CreateInputRangesFile(liftRangeData7);

			var logFile = TryImportWithRanges(sOrigFile, sOrigRangesFile, 1);
			File.Delete(sOrigFile);
			File.Delete(sOrigRangesFile);
			Assert.IsNotNull(logFile);
			File.Delete(logFile);
			Assert.AreEqual(1, repoEntry.Count);
			Assert.AreEqual(1, repoSense.Count);

			Assert.IsTrue(repoEntry.TryGetObject(new Guid("aef5e807-c841-4f35-9591-c8a998dc2465"), out var entry));
			Assert.AreEqual(1, entry.SensesOS.Count);
			var sense0 = entry.SensesOS[0];
			Assert.AreEqual(sense0.Guid, new Guid("5741255b-0563-49e0-8839-98bdb8c73f48"));

			Assert.IsNotNull(entry.LexemeFormOA);
			Assert.IsNotNull(entry.LexemeFormOA.MorphTypeRA);
			Assert.AreEqual("stem", entry.LexemeFormOA.MorphTypeRA.Name.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("Baba", entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text);
			Assert.IsNotNull(sense0.MorphoSyntaxAnalysisRA as IMoStemMsa);
			// ReSharper disable PossibleNullReferenceException
			Assert.IsNotNull((sense0.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA);
			Assert.AreEqual("NounFamily", (sense0.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA.Name.AnalysisDefaultWritingSystem.Text);
			// ReSharper restore PossibleNullReferenceException
			Assert.AreEqual("Papi", sense0.Gloss.AnalysisDefaultWritingSystem.Text);

			// Verify example was imported
			Assert.AreEqual(1, sense0.ExamplesOS.Count, "Example not imported correctly.");

			VerifyCmPossibilityLists();
			VerifyCmPossibilityCustomFields(entry);
			VerifyCmPossibilityCustomFieldsData(entry);
			var customData = new CustomFieldData()
			{
				CustomFieldname = "ExampleStatus",
				CustomFieldType = CellarPropertyType.ReferenceAtomic,
				cmPossibilityNameRA = "Pending"
			};
			VerifyCustomFieldExample(sense0.ExamplesOS[0], customData);
		}

		/// <summary>
		/// LIFT Import:  test import of Custom Lists from the Ranges file.
		/// Also test the import of field definitions for custom fields
		/// which contain CmPossibility list data and verify that the data is correct too.
		/// </summary>
		[Test]
		public void TestLiftImport_InflectionFieldRangeDoesNotCauseError()
		{
			var inflectionLiftData = new[]
			{
				"<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
				"<lift producer=\"SIL.FLEx 7.3.2.41302\" version=\"0.13\">",
				"<header>",
				"<fields/>",
				"</header>",
				"<entry dateCreated=\"2013-01-29T08:53:26Z\" dateModified=\"2013-01-29T08:10:28Z\" id=\"baba_aef5e807-c841-4f35-9591-c8a998dc2465\" guid=\"aef5e807-c841-4f35-9591-c8a998dc2465\">",
				"<lexical-unit>",
				"<form lang=\"fr\"><text>baba baba</text></form>",
				"</lexical-unit>",
				"<sense id=\"$guid2\" dateCreated=\"2013-01-29T08:55:26Z\" dateModified=\"2013-01-29T08:15:28Z\">",
				"<gloss lang=\"en\"><text>dad</text></gloss>",
				"</sense>",
				"</entry>",
				"</lift>"
			};
			var inflectionLiftRangeData = new[]
			{
			"<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
			"<lift-ranges>",
			"<range id='inflection-feature'>",
			"<range-element guid=\"21fb4fd5-278c-4530-b1ac-2755ced5b278\" id=\"NounAgr\">",
			"<label>",
			"<form lang=\"en\"><text>noun agreement</text></form><form lang=\"fr\"><text>concordancia nominal</text></form>",
			"</label>",
			"<abbrev><form lang=\"en\"><text>NounAgr</text></form><form lang=\"fr\"><text>NounAgr</text></form></abbrev>",
			"<trait name=\"catalog-source-id\" value=\"cNounAgr\"/>",
			"<trait name=\"display-to-right\" value=\"False\" />",
			"<trait name=\"show-in-gloss\" value=\"False\" />",
			"<trait name=\"feature-definition-type\" value=\"complex\" />",
			"</range-element>",
			"</range>",
			"</lift-ranges>"
			};

			SetWritingSystems("fr");

			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			Assert.AreEqual(0, repoEntry.Count);
			Assert.AreEqual(0, repoSense.Count);

			//Create the LIFT data file
			var sOrigFile = CreateInputFile(inflectionLiftData);
			//Create the LIFT ranges file
			var sOrigRangesFile = CreateInputRangesFile(inflectionLiftRangeData);

			var logFile = TryImportWithRanges(sOrigFile, sOrigRangesFile, 1);
			File.Delete(sOrigFile);
			File.Delete(sOrigRangesFile);
			//Verify that no errors were encountered loading the inflection features range
			AssertThatXmlIn.File(logFile).HasNoMatchForXpath("//*[contains(., 'Error encountered processing ranges')]");
			File.Delete(logFile);
			Assert.AreEqual(1, repoEntry.Count);
			Assert.AreEqual(1, repoSense.Count);

			Assert.IsTrue(repoEntry.TryGetObject(new Guid("aef5e807-c841-4f35-9591-c8a998dc2465"), out _));
		}

		/// <summary>
		/// LT-15516: Blank reversal entries were multiplying on import. Blank entries should be removed during
		/// an import.
		/// </summary>
		[Test]
		public void TestLiftImport_BlankReversalsAreNotImported()
		{
			var liftDataWithEmptyReversal = new[]
			{
			@"<?xml version=""1.0"" encoding=""UTF-8"" ?>",
			@"<lift producer=""SIL.FLEx 8.0.10.41831"" version=""0.13"">",
			@"<entry id=""some entry_f543cf6b-5ce1-4bed-af4b-82760994890c"" guid=""f543cf6b-5ce1-4bed-af4b-82760994890c"">",
			@"<lexical-unit>",
			@"<form lang=""fr""><text>some entry</text></form>",
			@"</lexical-unit>",
			@"<trait  name=""morph-type"" value=""phrase""/>",
			@"<relation type=""_component-lexeme"" ref="""">",
			@"<trait name=""complex-form-type"" value=""""/>",
			@"</relation>",
			@"<sense id=""b4de1476-b432-46b6-97e3-c993ff0a2ff9"">",
			@"<gloss lang=""en""><text>has a blank reversal</text></gloss>",
			@"<reversal type=""en""></reversal>",
			@"</sense>",
			@"</entry>",
			@"</lift>"
			};
			SetWritingSystems("fr");

			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			Assert.AreEqual(0, repoEntry.Count);
			Assert.AreEqual(0, repoSense.Count);
			Assert.That(Cache.ServiceLocator.GetInstance<IReversalIndexEntryRepository>().Count, Is.EqualTo(0));

			//Create the LIFT data file
			var liftFileWithBlankReversal = CreateInputFile(liftDataWithEmptyReversal);

			var logFile = TryImport(liftFileWithBlankReversal, null, MergeStyle.MsKeepNew, 1);
			File.Delete(liftFileWithBlankReversal);
			//Verify that no errors were encountered loading the inflection features range
			AssertThatXmlIn.File(logFile).HasNoMatchForXpath("//*[contains(., 'Error encountered processing ranges')]");
			File.Delete(logFile);
			Assert.AreEqual(1, repoEntry.Count);
			Assert.AreEqual(1, repoSense.Count);
			Assert.IsTrue(repoSense.TryGetObject(new Guid("b4de1476-b432-46b6-97e3-c993ff0a2ff9"), out var sense));
			Assert.That(sense.ReferringReversalIndexEntries.Count, Is.EqualTo(0), "Empty reversal should not have been imported.");
			Assert.That(Cache.ServiceLocator.GetInstance<IReversalIndexEntryRepository>().Count, Is.EqualTo(0));
		}

		/// <summary>
		/// Blank reversal entries were multiplying on import. Blank entries should be removed during
		/// an import while still importing non-blank entries
		/// </summary>
		[Test]
		public void TestLiftImport_BlankReversalsAreSkippedButNonBlanksAreImported()
		{
			var liftDataWithOneEmptyAndOneNonEmptyReversal = new[]
			{
			@"<?xml version=""1.0"" encoding=""UTF-8"" ?>",
			@"<lift producer=""SIL.FLEx 8.0.10.41831"" version=""0.13"">",
			@"<entry id=""some entry_f543cf6b-5ce1-4bed-af4b-82760994890c"" guid=""f543cf6b-5ce1-4bed-af4b-82760994890c"">",
			@"<lexical-unit>",
			@"<form lang=""fr""><text>some entry</text></form>",
			@"</lexical-unit>",
			@"<trait  name=""morph-type"" value=""phrase""/>",
			@"<relation type=""_component-lexeme"" ref="""">",
			@"<trait name=""complex-form-type"" value=""""/>",
			@"</relation>",
			@"<sense id=""b4de1476-b432-46b6-97e3-c993ff0a2ff9"">",
			@"<gloss lang=""en""><text>has a blank reversal</text></gloss>",
			@"<reversal type=""en""></reversal>",
			@"<reversal type=""en""><form lang=""en""><text>Got one</text></form></reversal>",
			@"</sense>",
			@"</entry>",
			@"</lift>"
			};

			SetWritingSystems("fr");

			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			Assert.AreEqual(0, repoEntry.Count);
			Assert.AreEqual(0, repoSense.Count);

			//Create the LIFT data file
			var liftFileWithOneEmptyAndOneNonEmptyReversal = CreateInputFile(liftDataWithOneEmptyAndOneNonEmptyReversal);

			var logFile = TryImport(liftFileWithOneEmptyAndOneNonEmptyReversal, null, MergeStyle.MsKeepNew, 1);
			File.Delete(liftFileWithOneEmptyAndOneNonEmptyReversal);
			File.Delete(logFile);
			Assert.AreEqual(1, repoEntry.Count);
			Assert.AreEqual(1, repoSense.Count);
			Assert.IsTrue(repoSense.TryGetObject(new Guid("b4de1476-b432-46b6-97e3-c993ff0a2ff9"), out var sense));
			Assert.That(sense.ReferringReversalIndexEntries.Count, Is.EqualTo(1), "Empty reversal should not have been imported but non empty should.");
		}

		/// <summary>
		/// Test for Date only conflict mediation
		/// </summary>
		[Test]
		public void TestLiftImport_DateAndWesayIdAloneShouldNotChangeDate()
		{
			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();

			SetWritingSystems("fr");
			var english = Cache.WritingSystemFactory.GetWsFromStr("en");
			entry.LexemeFormOA = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.LexemeFormOA.Form.set_String(Cache.DefaultVernWs, "some entry");
			entry.SensesOS.Add(sense);
			sense.Gloss.set_String(english, TsStringUtils.MakeString("blank", english));
			var entryCreationMs = entry.DateCreated.Millisecond;
			var entryModifiedMs = entry.DateModified.Millisecond;
			var basicLiftEntry = new[]
			{
			@"<?xml version=""1.0"" encoding=""UTF-8"" ?>",
			@"<lift producer=""SIL.FLEx 8.0.10.41831"" version=""0.13"">",
			"<entry dateCreated=\"" + entry.DateCreated.ToUniversalTime().ToString("yyyy-MM-ddTHH':'mm':'ssZ") + "\" dateModified=\""
			 + entry.DateModified.ToUniversalTime().ToString("yyyy-MM-ddTHH':'mm':'ssZ") + "\" id=\"some entry_" +  entry.Guid + "\" guid=\"" + entry.Guid + "\">",
			@"<lexical-unit>",
			@"<form lang=""fr""><text>some entry</text></form>",
			@"</lexical-unit>",
			"<sense id=\"" + sense.Guid + "\">",
			@"<gloss lang=""en""><text>has a blank reversal</text></gloss>",
			@"</sense>",
			@"</entry>",
			@"</lift>"
			};
			Assert.AreEqual(1, repoEntry.Count);
			Assert.AreEqual(1, repoSense.Count);

			//Create the LIFT data file
			var testLiftFile = CreateInputFile(basicLiftEntry);

			var logFile = TryImport(testLiftFile, null, MergeStyle.MsKeepNew, 1, false);
			File.Delete(testLiftFile);
			File.Delete(logFile);
			Assert.AreEqual(1, repoEntry.Count);
			Assert.AreEqual(1, repoSense.Count);
			Assert.AreEqual(entry.DateCreated.Millisecond, entryCreationMs, "Creation Date lost milliseconds on a 'no-op' merge");
			Assert.AreEqual(entry.DateModified.Millisecond, entryModifiedMs, "Modification time lost milliseconds on a 'no-op' merge");
		}

		/// <summary>
		/// LT-15516: Blank reversal entries were multiplying on import. Blank entries should be removed during
		/// an import.
		/// </summary>
		[Test]
		public void TestLiftImport_PronunciationLanguageAddedToPronunciationAndVernacularLists()
		{
			var liftDataWithIpaPronunciation = new[]
			{
			@"<?xml version=""1.0"" encoding=""UTF-8"" ?>",
			@"<lift producer=""SIL.FLEx 8.0.10.41831"" version=""0.13"">",
			@"<entry id=""some entry_f543cf6b-5ce1-4bed-af4b-82760994890c"" guid=""f543cf6b-5ce1-4bed-af4b-82760994890c"">",
			@"<lexical-unit>",
			@"<form lang=""fr""><text>some entry</text></form>",
			@"</lexical-unit>",
			@"<pronunciation>",
			@"<form lang=""nbf-fonipa"">",
			@"<text>ʕɑ³³</text>",
			@"</form>",
			@"</pronunciation>",
			@"</entry>",
			@"</lift>"
			};
			SetWritingSystems("fr");

			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			Assert.AreEqual(0, repoEntry.Count);
			Assert.AreEqual(Cache.LangProject.CurrentPronunciationWritingSystems.Count, 0);
			Assert.AreEqual(Cache.LangProject.VernacularWritingSystems.Count, 1);

			//Create the LIFT data file
			var liftFileWithIpaPronunciation = CreateInputFile(liftDataWithIpaPronunciation);

			var logFile = TryImport(liftFileWithIpaPronunciation, null, MergeStyle.MsKeepNew, 1);
			File.Delete(liftFileWithIpaPronunciation);
			//Verify that the writing system was reported as added
			AssertThatXmlIn.File(logFile).HasSpecifiedNumberOfMatchesForXpath("//li[contains(., 'Naxi (International Phonetic Alphabet) (nbf-fonipa)')]", 1);
			File.Delete(logFile);
			Assert.AreEqual(1, repoEntry.Count);
			Assert.AreEqual(Cache.LangProject.CurrentPronunciationWritingSystems.Count, 1, "IPA from pronunciation was not added to pronunciation writing systems");
			Assert.AreEqual(Cache.LangProject.VernacularWritingSystems.Count, 2, "IPA from pronunciation was not added to vernacular writing systems");
		}

		private void VerifyCmPossibilityLists()
		{
			//Semantic Domain list is imported so the GUIDs should be the same for the data that was in the
			//ranges file
			var semanticDomainsList = Cache.LanguageProject.SemanticDomainListOA;
			var item = semanticDomainsList.FindOrCreatePossibility("Universe, creation", Cache.DefaultAnalWs);
			Assert.IsNotNull(item);
			Assert.AreEqual("63403699-07c1-43f3-a47c-069d6e4316e5", item.Guid.ToString());

			item = semanticDomainsList.FindOrCreatePossibility("Universe, creation" + StringUtils.kszObject + "Sky", Cache.DefaultAnalWs);
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
					VerifyListItem(list.PossibilitiesOS[0], "list item 1", "66705e7a-d7db-47c6-964c-973d5830566c", "***", "description of item 1");
					VerifyListItem(list.PossibilitiesOS[1], "list item 2", "8af65c9d-2e79-4d6a-8164-854aab89d068", "itm2", "Here is a description of item 2.");
					VerifyListItem(list.PossibilitiesOS[2], "list item 3", "D7BFD944-AD73-4512-B5F2-35EC5DB3BFF3", "itm3", "Range is in twice");

				}
				else if (list.OwningFlid == 0 && list.Name.BestAnalysisVernacularAlternative.Text == "CustomList Number2 ")
				{
					Assert.IsTrue(list.PossibilitiesOS.Count == 2);
					VerifyListItem(list.PossibilitiesOS[0], "cstm list item 1", "aea3e48f-de0c-4315-8a35-f3b844070e94", "labr1", "***");
					VerifyListItem(list.PossibilitiesOS[1], "cstm list item 2", "164fc705-c8fd-46af-a3a8-5f0f62565d96", "***", "Desc list2 item2");
				}
			}
		}

		private static void VerifyListItem(ICmPossibility listItem, string itemName, string itemGuid, string itemAbbrev, string itemDesc)
		{
			Assert.AreEqual(itemName, listItem.Name.BestAnalysisVernacularAlternative.Text);
			Assert.AreEqual(itemGuid.ToLowerInvariant(), listItem.Guid.ToString().ToLowerInvariant());
			Assert.AreEqual(itemAbbrev, listItem.Abbreviation.BestAnalysisVernacularAlternative.Text);
			Assert.AreEqual(itemDesc, listItem.Description.BestAnalysisVernacularAlternative.Text);
		}


		private void VerifyCmPossibilityCustomFields(ILexEntry entry)
		{
			m_mdc = Cache.MetaDataCacheAccessor as IFwMetaDataCacheManaged;
			var repo = Cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();

			//Store mapping between Possibility List names and their guids. This is used to verify that
			//the custom list has stored the correct guid for the list when imported.
			m_customListNamesAndGuids.Add(RangeNames.sSemanticDomainListOA, Cache.LanguageProject.SemanticDomainListOA.Guid);
			foreach (var list in repo.AllInstances())
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
				{
					continue;
				}
				var fieldName = m_mdc.GetFieldName(flid);

				switch (fieldName)
				{
					case "CustomFld ListSingle":
						VerifyCustomListToPossList(flid, CellarPropertyType.ReferenceAtomic, RangeNames.sSemanticDomainListOA);
						break;
					case "CustomFld ListMulti":
						VerifyCustomListToPossList(flid, CellarPropertyType.ReferenceCollection, RangeNames.sSemanticDomainListOA);
						break;
					case "CustomFld CmPossibilityCustomList":
						VerifyCustomListToPossList(flid, CellarPropertyType.ReferenceAtomic, "CustomCmPossibiltyList");
						break;
					case "CustomFld CustomList2":
						VerifyCustomListToPossList(flid, CellarPropertyType.ReferenceAtomic, "CustomList Number2 ");
						break;
				}
			}
		}

		private void VerifyCustomListToPossList(int flid, CellarPropertyType type, string possListName)
		{
			var custFieldType = (CellarPropertyType)m_mdc.GetFieldType(flid);
			var custFieldListGuid = m_mdc.GetFieldListRoot(flid);
			Assert.AreEqual(type, custFieldType);
			var lstGuid = Guid.Empty;
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
			customData = new CustomFieldData
			{
				CustomFieldname = "CustomFld CmPossibilityCustomList",
				CustomFieldType = CellarPropertyType.ReferenceAtomic,
				cmPossibilityNameRA = "list item 1"
			};
			VerifyCustomField(entry, customData, m_customFieldEntryIds["CustomFld CmPossibilityCustomList"]);

			//<trait name="CustomFld CustomList2" value="cstm list item 2"/>
			customData = new CustomFieldData
			{
				CustomFieldname = "CustomFld CustomList2",
				CustomFieldType = CellarPropertyType.ReferenceAtomic,
				cmPossibilityNameRA = "cstm list item 2"
			};
			VerifyCustomField(entry, customData, m_customFieldEntryIds["CustomFld CustomList2"]);
		}

		public static string GetPossibilityBestAlternative(int possibilityHvo, LcmCache cache)
		{
			var tsm = cache.DomainDataByFlid.get_MultiStringProp(possibilityHvo, CmPossibilityTags.kflidName);
			var str = BestAlternative(tsm as IMultiAccessorBase, cache.DefaultUserWs);
			return str;
		}
		private static string BestAlternative(IMultiAccessorBase multi, int wsDefault)
		{
			var tss = multi.BestAnalysisVernacularAlternative;
			if (tss.Text == "***")
			{
				tss = multi.get_String(wsDefault);
			}
			return XmlUtils.MakeSafeXmlAttribute(tss.Text);
		}

		/// <summary>
		/// LIFT Import:  test import of Location List from the Ranges file (and that we can link to one).
		/// </summary>
		[Test]
		public void TestLiftImportLocationList()
		{
			string[] liftDataLocations = {
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
					"</variant>",
			"<pronunciation>",
				"<form lang=\"qaa-fonipa-x-kal\"><text>pronunciation</text></form>",
				"<field type=\"cv-pattern\">",
				"<form lang=\"en\"><text>CVCV</text></form>",
				"</field>",
				"<field type=\"tone\">",
				"<form lang=\"en\"><text>HLH</text></form>",
				"</field>",
				"<trait name=\"location\" value=\"Village\"/>",
			"</pronunciation>",
			"<sense id=\"9d6c600b-192a-4eec-980b-a605173ba5e3\">",
			"<gloss lang=\"en\"><text>Pops</text></gloss>",
				"<example>",
				"<form lang=\"fr\"><text>Example Sentence</text></form>",
				"</example>",
			"</sense>",
			"</entry>",
			"</lift>"
			};
			string[] liftRangeDataLocations = {
				"<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
				"<!-- See http://code.google.com/p/lift-standard for more information on the format used here. -->",
				"<lift-ranges>",

				"<range id=\"location\">",
				"<range-element id=\"Village\" guid=\"63403699-07c1-43f3-a47c-069d6e4316e5\">",
				"<label>",
				"<form lang=\"en\"><text>village</text></form>",
				"</label>",
				"<abbrev>",
				"<form lang=\"en\"><text>VIL</text></form>",
				"</abbrev>",
				"<description>",
				"<form lang=\"en\"><text>Located 135 deg east, 3 deg south</text></form>",
				"</description>",
				"</range-element>",

				"<range-element id=\"river\">",
				"<label>",
				"<form lang=\"en\"><text>river</text></form>",
				"</label>",
				"<abbrev>",
				"<form lang=\"en\"><text>RIV</text></form>",
				"</abbrev>",
				"<description>",
				"<form lang=\"en\"><text>large river, somewhat silty</text></form>",
				"</description>",
				"</range-element>",

				"<range-element id=\"House\" guid=\"5893e40e-e7bb-4c44-ac06-b6cec06b8470\" parent=\"Village\">",
				"<label>",
				"<form lang=\"en\"><text>House</text></form>",
				"</label>",
				"</range-element>",
				"<range-element id=\"Square\" parent=\"Village\">",
				"<label>",
				"<form lang=\"en\"><text>Square</text></form>",
				"</label>",
				"</range-element>",
				"</range>",
				"</lift-ranges>"
			};
			SetWritingSystems("fr");

			// One should exist already, to show that we can detect duplicates.
			var originalRiver = Cache.ServiceLocator.GetInstance<ICmLocationFactory>().Create();
			Cache.LangProject.LocationsOA.PossibilitiesOS.Add(originalRiver);
			originalRiver.Name.SetAnalysisDefaultWritingSystem("river");
			originalRiver.Abbreviation.SetAnalysisDefaultWritingSystem("RIV");

			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			Assert.AreEqual(0, repoEntry.Count);
			Assert.AreEqual(0, repoSense.Count);

			//Create the LIFT data file
			var sOrigFile = CreateInputFile(liftDataLocations);
			//Create the LIFT ranges file
			var sOrigRangesFile = CreateInputRangesFile(liftRangeDataLocations);

			var logFile = TryImportWithRanges(sOrigFile, sOrigRangesFile, 1);
			File.Delete(sOrigFile);
			File.Delete(sOrigRangesFile);
			Assert.IsNotNull(logFile);
			File.Delete(logFile);
			Assert.AreEqual(1, repoEntry.Count);
			Assert.AreEqual(1, repoSense.Count);

			var locations = Cache.LangProject.LocationsOA;
			Assert.That(locations.PossibilitiesOS.Count, Is.EqualTo(2), "should have imported one locations and matched another");
			var village = locations.PossibilitiesOS[1];
			Assert.That(village.Name.AnalysisDefaultWritingSystem.Text, Is.EqualTo("village"));
			Assert.That(village.Guid, Is.EqualTo(new Guid("63403699-07c1-43f3-a47c-069d6e4316e5")));
			Assert.That(village.SubPossibilitiesOS.Count, Is.EqualTo(2));
			Assert.That(village.Abbreviation.AnalysisDefaultWritingSystem.Text, Is.EqualTo("VIL"));
			Assert.That(village.Description.AnalysisDefaultWritingSystem.Text, Is.EqualTo("Located 135 deg east, 3 deg south"));

			Assert.That(locations.PossibilitiesOS[0], Is.EqualTo(originalRiver));
			Assert.That(locations.PossibilitiesOS[0].Name.AnalysisDefaultWritingSystem.Text, Is.EqualTo("river"));

			var house = village.SubPossibilitiesOS[0];
			Assert.That(house.Name.AnalysisDefaultWritingSystem.Text, Is.EqualTo("House"));
			Assert.That(house.Guid, Is.EqualTo(new Guid("5893e40e-e7bb-4c44-ac06-b6cec06b8470")));

			Assert.That(village.SubPossibilitiesOS[1].Name.AnalysisDefaultWritingSystem.Text, Is.EqualTo("Square"));

			var entry = repoEntry.AllInstances().First();
			var pronunciation = entry.PronunciationsOS[0];
			var location = pronunciation.LocationRA;
			Assert.That(location, Is.EqualTo(village));
		}

		/// <summary>
		/// LIFT Import:  test import of custom fields representing StText data.
		/// Also test the import of field definitions for custom fields which contain StText data
		/// </summary>
		[Test]
		public void TestLiftImport8CustomStText()
		{
			SetWritingSystems("fr");

			CreateNeededStyles();

			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			Assert.AreEqual(0, repoEntry.Count);
			Assert.AreEqual(0, repoSense.Count);

			var sOrigFile = CreateInputFile(_liftData8);

			var logFile = TryImport(sOrigFile, 2);
			File.Delete(sOrigFile);
			Assert.IsNotNull(logFile);
			File.Delete(logFile);

			var flidCustom = Cache.MetaDataCacheAccessor.GetFieldId("LexEntry", "Long Text", false);
			Assert.AreNotEqual(0, flidCustom, "The \"Long Text\" custom field should exist for LexEntry objects.");
			var type = Cache.MetaDataCacheAccessor.GetFieldType(flidCustom);
			Assert.AreEqual((int)CellarPropertyType.OwningAtomic, type, "The custom field should be an atomic owning field.");
			var destName = Cache.MetaDataCacheAccessor.GetDstClsName(flidCustom);
			Assert.AreEqual("StText", destName, "The custom field should own an StText object.");

			Assert.AreEqual(2, repoEntry.Count);
			Assert.AreEqual(2, repoSense.Count);

			VerifyFirstEntryStTextDataImportExact(repoEntry, 3, flidCustom);

			Assert.IsTrue(repoEntry.TryGetObject(new Guid("241cffca-3062-4b1c-8f9f-ab8ed07eb7bd"), out var entry2));
			Assert.AreEqual(1, entry2.SensesOS.Count);
			var sense2 = entry2.SensesOS[0];
			Assert.AreEqual(sense2.Guid, new Guid("2759532a-26db-4850-9cba-b3684f0a3f5f"));

			var hvo = Cache.DomainDataByFlid.get_ObjectProp(entry2.Hvo, flidCustom);
			Assert.AreNotEqual(0, hvo, "The second entry has a value in the \"Long Text\" custom field.");
			var text = Cache.ServiceLocator.ObjectRepository.GetObject(hvo) as IStText;
			Assert.IsNotNull(text);
			Assert.AreEqual(3, text.ParagraphsOS.Count, "The first Long Text field should have three paragraphs.");

			Assert.IsNull(text.ParagraphsOS[0].StyleName);
			var tisb = TsStringUtils.MakeIncStrBldr();
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
			factory.Create(Cache.LangProject.StylesOC, "Bulleted List", ContextValues.General, StructureValues.Undefined, FunctionValues.Prose, false, 0, true);
			factory.Create(Cache.LangProject.StylesOC, "Block Quote", ContextValues.General, StructureValues.Undefined, FunctionValues.Prose, false, 0, true);
			factory.Create(Cache.LangProject.StylesOC, "Normal", ContextValues.General, StructureValues.Undefined, FunctionValues.Prose, false, 0, true);
			factory.Create(Cache.LangProject.StylesOC, "Emphasized Text", ContextValues.General, StructureValues.Undefined, FunctionValues.Prose, true, 0, true);
			factory.Create(Cache.LangProject.StylesOC, "Strong", ContextValues.General, StructureValues.Undefined, FunctionValues.Prose, true, 0, true);
		}

		/// <summary>
		/// LIFT Import:  test import of custom fields representing StText data with conflicts,
		/// using the "Keep Both" option.
		/// </summary>
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

			var sOrigFile = CreateInputFile(_liftData8);
			TryImport(sOrigFile, null, MergeStyle.MsKeepBoth, 2);

		}

		/// <summary>
		/// LIFT Import:  test import of custom fields representing StText data with conflicts,
		/// using the "Keep Old" option.
		/// </summary>
		[Test]
		public void TestLiftImport9BMergingStTextKeepOld()
		{
			SetWritingSystems("fr");

			CreateNeededStyles();
			CreateFirstEntryWithConflictingData();

			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			Assert.AreEqual(1, repoEntry.Count);
			Assert.AreEqual(1, repoSense.Count);

			var sOrigFile = CreateInputFile(_liftData8);
			TryImport(sOrigFile, null, MergeStyle.MsKeepOld, 2);

		}

		/// <summary>
		/// LIFT Import:  test import of custom fields representing StText data with conflicts,
		/// using the "Keep New" option.
		/// </summary>
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

			var sOrigFile = CreateInputFile(_liftData8);
			TryImport(sOrigFile, null, MergeStyle.MsKeepNew, 2);

			Assert.AreEqual(2, repoEntry.Count);
			Assert.AreEqual(2, repoSense.Count);

			VerifyFirstEntryStTextDataImportExact(repoEntry, 4, flidCustom);
			// Now check the fourth paragraph.
			Assert.IsTrue(repoEntry.TryGetObject(new Guid("494616cc-2f23-4877-a109-1a6c1db0887e"), out var entry1));
			var hvo = Cache.DomainDataByFlid.get_ObjectProp(entry1.Hvo, flidCustom);
			Assert.AreNotEqual(0, hvo, "The first entry has a value in the \"Long Text\" custom field.");
			var text = Cache.ServiceLocator.ObjectRepository.GetObject(hvo) as IStText;
			Assert.IsNotNull(text);
			var para = text.ParagraphsOS[3] as IStTxtPara;
			Assert.IsNotNull(para);
			Assert.AreEqual("Numbered List", para.StyleName);
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			var tss = TsStringUtils.MakeString("This is the fourth paragraph.", wsEn);
			Assert.AreEqual(tss.Text, para.Contents.Text);
			Assert.IsTrue(tss.Equals(para.Contents), "The fourth paragraph contents should not have changed.");
		}

		/// <summary>
		/// LIFT Import:  test import of custom fields representing StText data with conflicts,
		/// using the "Keep Only New" option.
		/// </summary>
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

			var sOrigFile = CreateInputFile(_liftData8);
			TryImport(sOrigFile, null, MergeStyle.MsKeepOnlyNew, 2);

			Assert.AreEqual(2, repoEntry.Count);
			Assert.AreEqual(2, repoSense.Count);

			VerifyFirstEntryStTextDataImportExact(repoEntry, 3, flidCustom);
		}

		/// <summary>
		/// LIFT Import:  test import of inflection feature with empty value. Lose it!
		/// </summary>
		[Test]
		public void TestLiftImportEmptyInflectionFeature()
		{
			string[] liftData9 = {
				"<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
				"<lift producer=\"SIL.FLEx 7.2.4.41019\" version=\"0.13\">",
				"<header>",
				"<ranges/>",
				"<fields/>",
				"</header>",
				"<entry dateCreated=\"2012-02-23T11:53:26Z\" dateModified=\"2012-08-16T08:10:28Z\" id=\"test\" guid=\"f8506500-d17c-4c1b-b05d-ea57f562cb1c\">",
				"<lexical-unit>",
				"<form lang=\"grh\"><text>test</text></form>",
				"<form lang=\"grh-fonipa\"><text>testipa</text></form>",
				"</lexical-unit>",
				"<trait name=\"morph-type\" value=\"stem\"/>",
				"<field type=\"Syllable shape\">",
				"<form lang=\"en\"><text>CV-CV</text></form>",
				"</field>",
				"<sense id=\"62fc5222-aa72-40bb-b3f1-24569bb94042\" dateCreated=\"2012-08-12T12:27:12Z\" dateModified=\"2012-08-12T12:27:12Z\">",
				"<grammatical-info value=\"Noun\">",
				"<trait name=\"inflection-feature\" value=\"{Infl}\"/>",
				"</grammatical-info>",
				"<gloss lang=\"en\"><text>black.plum</text></gloss>",
				"<gloss lang=\"fr\"><text>noir</text></gloss>",
				"<definition>",
				"<form lang=\"en\"><text>black plum tree</text></form>",
				"<form lang=\"fr\"><text>noir: noir est un manque de couleur</text></form>",
				"</definition>",
				"</sense>",
				"</entry>",
				"</lift>"
			};
			SetWritingSystems("fr");

			CreateNeededStyles();

			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			Assert.AreEqual(0, repoEntry.Count);
			Assert.AreEqual(0, repoSense.Count);

			var sOrigFile = CreateInputFile(liftData9);
			TryImport(sOrigFile, null, MergeStyle.MsKeepNew, 1);
			Assert.AreEqual(1, repoEntry.Count);
			Assert.AreEqual(1, repoSense.Count);
			var todoEntry = repoEntry.GetObject(new Guid("f8506500-d17c-4c1b-b05d-ea57f562cb1c"));
			Assert.AreEqual("Noun", todoEntry.SensesOS[0].MorphoSyntaxAnalysisRA.LongName, "MSA should NOT have any Inflection Feature stuff on it.");
		}

		private void VerifyFirstEntryStTextDataImportExact(ILexEntryRepository repoEntry, int cpara, int flidCustom)
		{
			Assert.IsTrue(repoEntry.TryGetObject(new Guid("494616cc-2f23-4877-a109-1a6c1db0887e"), out var entry1));
			Assert.AreEqual(1, entry1.SensesOS.Count);
			var sense1 = entry1.SensesOS[0];
			Assert.AreEqual(sense1.Guid, new Guid("3e0ae703-db7f-4687-9cf5-481524095905"));

			var hvo = Cache.DomainDataByFlid.get_ObjectProp(entry1.Hvo, flidCustom);
			Assert.AreNotEqual(0, hvo, "The first entry has a value in the \"Long Text\" custom field.");
			var text = Cache.ServiceLocator.ObjectRepository.GetObject(hvo) as IStText;
			Assert.IsNotNull(text);
			Assert.AreEqual(cpara, text.ParagraphsOS.Count, $"The first Long Text field should have {cpara} paragraphs.");
			Assert.AreEqual("Bulleted List", text.ParagraphsOS[0].StyleName);
			var tisb = TsStringUtils.MakeIncStrBldr();
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
			var entry0 = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(new Guid("494616cc-2f23-4877-a109-1a6c1db0887e"), Cache.LangProject.LexDbOA);
			entry0.LexemeFormOA = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry0.LexemeFormOA.Form.set_String(Cache.DefaultVernWs, "afa");
			entry0.LexemeFormOA.MorphTypeRA = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem);
			var msa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			entry0.MorphoSyntaxAnalysesOC.Add(msa);
			var sense0 = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create(new Guid("3e0ae703-db7f-4687-9cf5-481524095905"), entry0);
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
			para1.Contents = TsStringUtils.MakeString("This is the first paragraph.", Cache.DefaultAnalWs);
			var para2 = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
			text.ParagraphsOS.Add(para2);
			para2.StyleName = "Numbered List";
			para2.Contents = TsStringUtils.MakeString("This is the second paragraph.", Cache.DefaultAnalWs);
			var para3 = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
			text.ParagraphsOS.Add(para3);
			para3.StyleName = "Numbered List";
			para3.Contents = TsStringUtils.MakeString("This is the third paragraph.", Cache.DefaultAnalWs);
			var para4 = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
			text.ParagraphsOS.Add(para4);
			para4.StyleName = "Numbered List";
			para4.Contents = TsStringUtils.MakeString("This is the fourth paragraph.", Cache.DefaultAnalWs);

			return flidCustom;
		}

		/// <summary>
		/// LIFT Import:  test import of custom fields representing StText data with conflicts,
		/// using the "Keep New" option.
		/// </summary>
		[Test]
		public void TestLdmlMigration()
		{
			var testLiftDataSource = Path.Combine(FwDirectoryFinder.SourceDirectory, TestUtilities.LanguageExplorerTests, "LIFT", "LDML-11723");
			var testLiftDataPath = Path.Combine(FwDirectoryFinder.SourceDirectory, TestUtilities.LanguageExplorerTests, "LIFT", "LDML-11723-test");

			var sLiftDataFile = Path.Combine(testLiftDataPath, "LDML-11723.lift");
			var sLiftRangesFile = Path.Combine(testLiftDataPath, "LDML-11723.lift-ranges");
			var sWSfilesPath = Path.Combine(testLiftDataPath, "WritingSystems");
			var enLdml = Path.Combine(sWSfilesPath, "en.ldml");
			var sehLdml = Path.Combine(sWSfilesPath, "seh.ldml");
			var esLdml = Path.Combine(sWSfilesPath, "es.ldml");
			var xkalLdml = Path.Combine(sWSfilesPath, "x-kal.ldml");
			var qaaXkalLdml = Path.Combine(sWSfilesPath, "qaa-x-kal.ldml");
			var qaaIpaXkalLdml = Path.Combine(sWSfilesPath, "qaa-fonipa-x-kal.ldml");
			var qaaPhonemicxkalLdml = Path.Combine(sWSfilesPath, "qaa-fonipa-x-kal-emic.ldml");

			DirectoryHelper.Copy(testLiftDataSource, testLiftDataPath, true);

			//Make all files writable
			// don't want to copy readonly property.
			File.SetAttributes(sLiftDataFile, FileAttributes.Normal);
			File.SetAttributes(sLiftRangesFile, FileAttributes.Normal);
			File.SetAttributes(enLdml, FileAttributes.Normal);
			File.SetAttributes(sehLdml, FileAttributes.Normal);
			File.SetAttributes(esLdml, FileAttributes.Normal);
			File.SetAttributes(xkalLdml, FileAttributes.Normal);
			File.SetAttributes(qaaIpaXkalLdml, FileAttributes.Normal);
			File.SetAttributes(qaaPhonemicxkalLdml, FileAttributes.Normal);

			var flexImporter = new FlexLiftMerger(Cache, MergeStyle.MsKeepBoth, true);

			//Migrate the LDML files and lang names
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
			Assert.That(File.Exists(qaaIpaXkalLdml));
			// Verify that qaa-fonipa-x-kal-emic.ldml is unchanged.
			Assert.That(File.Exists(qaaPhonemicxkalLdml));

			// Verify that x-kal.ldml no longer exists
			Assert.That(!File.Exists(xkalLdml));
			// Verify that x-kal.ldml is renamed to qaa-x-kal and content changed
			Assert.That(File.Exists(qaaXkalLdml));

			//Verify qaa-x-kal.ldml file has correct changes in it.
			VerifyKalabaLdmlFile(qaaXkalLdml);

			//Verify LDML 11723.lift file has correct changes in it.
			VerifyLiftDataFile(sLiftDataFile);

			//Verify LDML 11723.lift file has correct changes in it.
			VerifyLiftRangesFile(sLiftRangesFile);

			//Delete the files that were converted to the new lang names.
			DeleteDirectory(new DirectoryInfo(testLiftDataPath));
		}

		/// <summary>
		/// Delete all files in a directory and all subfolders
		/// </summary>
		private static void DeleteDirectory(DirectoryInfo source)
		{
			foreach (var diSourceSubDir in source.GetDirectories())
			{
				DeleteDirectory(diSourceSubDir);
			}
			foreach (var fi in source.GetFiles())
			{
				fi.Delete();
			}
		}

		private static void VerifyLiftRangesFile(string sLiftRangesFile)
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
			var i = 0;
			foreach (var form in label.XPathSelectElements("form"))
			{
				switch (i)
				{
					case 0:
						attr = form.Attribute("lang"); Assert.IsNotNull(attr); //en
						Assert.IsTrue(attr.Value.Equals("en"));
						text = form.XPathSelectElement("text");
						Assert.IsTrue(text.Value.Equals("anatomy"));
						break;
					case 1:
						attr = form.Attribute("lang"); Assert.IsNotNull(attr); //en
						Assert.IsTrue(attr.Value.Equals("qaa-x-kal"));
						text = form.XPathSelectElement("text");
						Assert.IsTrue(text.Value.Equals("Kalaba anatomy"));
						break;
				}

				i++;
			}

			var abbrev = rangeElement.XPathSelectElement("abbrev");
			i = 0;
			foreach (var form in abbrev.XPathSelectElements("form"))
			{
				switch (i)
				{
					case 0:
						attr = form.Attribute("lang"); Assert.IsNotNull(attr); //en
						Assert.IsTrue(attr.Value.Equals("en"));
						text = form.XPathSelectElement("text");
						Assert.IsTrue(text.Value.Equals("Anat"));
						break;
					case 1:
						attr = form.Attribute("lang"); Assert.IsNotNull(attr); //en
						Assert.IsTrue(attr.Value.Equals("qaa-x-kal"));
						text = form.XPathSelectElement("text");
						Assert.IsTrue(text.Value.Equals("Kalaba Anat"));
						break;
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

		private static void VerifyLiftDataFile(string sLiftDataFile)
		{
			var xdoc = new XmlDocument();
			xdoc.Load(sLiftDataFile);
			var data = XElement.Parse(xdoc.InnerXml);
			VerifyFirstLexEntry(data);
			VerifySecondLexEntry(data);
		}

		private static void VerifyFirstLexEntry(XElement data)
		{
			var entry = data.XPathSelectElement("//*[name()='entry' and @guid='a9628929-4561-4afc-b097-88c9bb6df5e9']");
			var lexUnitForm = entry.XPathSelectElement("lexical-unit/form");
			var attr = lexUnitForm.Attribute("lang"); Assert.IsNotNull(attr); //lang
			Assert.IsTrue(attr.Value.Equals("qaa-x-kal"));

			var definition = entry.XPathSelectElement("sense/definition");
			var i = 0;
			foreach (var form in definition.XPathSelectElements("form"))
			{
				XElement span;
				switch (i)
				{
					case 0:
						attr = form.Attribute("lang"); Assert.IsNotNull(attr); //en
						Assert.IsTrue(attr.Value.Equals("en"));
						span = form.XPathSelectElement("text/span");
						attr = span.Attribute("lang"); Assert.IsNotNull(attr); //qaa-x-kal
						Assert.IsTrue(attr.Value.Equals("qaa-x-kal"));
						break;
					case 1:
						attr = form.Attribute("lang"); Assert.IsNotNull(attr); //qaa-x-kal
						Assert.IsTrue(attr.Value.Equals("qaa-x-kal"));
						span = form.XPathSelectElement("text/span");
						attr = span.Attribute("lang"); Assert.IsNotNull(attr); //en
						Assert.IsTrue(attr.Value.Equals("en"));
						break;
					case 2:
						attr = form.Attribute("lang"); Assert.IsNotNull(attr); //es
						Assert.IsTrue(attr.Value.Equals("es"));
						break;
				}

				i++;
			}
		}

		private static void VerifySecondLexEntry(XElement data)
		{
			var entry = data.XPathSelectElement("//*[name()='entry' and @guid='fa6a7bf7-1007-4e33-95b5-663335a12a98']");
			var lexUnitForm = entry.XPathSelectElement("lexical-unit");
			XAttribute attr;
			XElement text;
			XElement span;
			var i = 0;
			foreach (var form in lexUnitForm.XPathSelectElements("form"))
			{
				switch (i)
				{
					case 0:
						attr = form.Attribute("lang");
						Assert.IsNotNull(attr); //en
						Assert.IsTrue(attr.Value.Equals("qaa-x-kal"));
						break;
					case 1:
						attr = form.Attribute("lang"); Assert.IsNotNull(attr); //qaa-x-kal
						Assert.IsTrue(attr.Value.Equals("qaa-fonipa-x-kal"));
						break;
				}

				i++;
			}

			var sense = entry.XPathSelectElement("sense");
			i = 0;
			foreach (var gloss in sense.XPathSelectElements("gloss"))
			{
				XElement glossText;
				switch (i)
				{
					case 0:
						attr = gloss.Attribute("lang"); Assert.IsNotNull(attr); //qaa-x-kal
						Assert.IsTrue(attr.Value.Equals("qaa-x-kal"));
						glossText = gloss.XPathSelectElement("text"); Assert.IsNotNull(glossText);
						Assert.IsTrue(glossText.Value.Equals("KalabaGloss"));
						break;
					case 1:
						attr = gloss.Attribute("lang"); Assert.IsNotNull(attr);
						Assert.IsTrue(attr.Value.Equals("en"));
						glossText = gloss.XPathSelectElement("text"); Assert.IsNotNull(glossText);
						Assert.IsTrue(glossText.Value.Equals("EnglishGLoss"));
						break;
					case 2:
						attr = gloss.Attribute("lang"); Assert.IsNotNull(attr);
						Assert.IsTrue(attr.Value.Equals("es"));
						glossText = gloss.XPathSelectElement("text"); Assert.IsNotNull(glossText);
						Assert.IsTrue(glossText.Value.Equals("SpanishGloss"));
						break;
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
				switch (i)
				{
					case 0:
						attr = spanInDefn.Attribute("lang"); Assert.IsNotNull(attr); //en
						Assert.IsTrue(attr.Value.Equals("qaa-fonipa-x-kal"));
						Assert.IsTrue(spanInDefn.Value.Equals("KalabaIPAspan"));
						break;
					case 1:
						attr = spanInDefn.Attribute("lang"); Assert.IsNotNull(attr); //en
						Assert.IsTrue(attr.Value.Equals("en"));
						Assert.IsTrue(spanInDefn.Value.Equals("EnglishSpan"));
						break;
					case 2:
						attr = spanInDefn.Attribute("lang"); Assert.IsNotNull(attr); //en
						Assert.IsTrue(attr.Value.Equals("es"));
						Assert.IsTrue(spanInDefn.Value.Equals("SpanishSpan"));
						break;
					case 3:
						attr = spanInDefn.Attribute("lang"); Assert.IsNotNull(attr); //en
						Assert.IsTrue(attr.Value.Equals("qaa-fonipa-x-kal-emic"));
						Assert.IsTrue(spanInDefn.Value.Equals("KalabaPhonemic"));
						break;
					case 4:
						attr = spanInDefn.Attribute("lang"); Assert.IsNotNull(attr); //en
						Assert.IsTrue(attr.Value.Equals("qaa-x-Lomwe"));
						Assert.IsTrue(spanInDefn.Value.Equals("Lomwe Span"));
						break;
					case 5:
						attr = spanInDefn.Attribute("lang"); Assert.IsNotNull(attr); //en
						Assert.IsTrue(attr.Value.Equals("qaa-x-AveryLon"));
						Assert.IsTrue(spanInDefn.Value.Equals("AveryLongWSName span"));
						break;
				}

				i++;
			}
		}

		private static void VerifyKalabaLdmlFile(string qaaxkalLdml)
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

		/// <summary>
		/// LIFT Import:  test import of Publish In data in entries, senses and example sentences.
		/// Also test the import of the Publications list.
		/// </summary>
		[Test]
		public void TestLiftImportOfPublicationSettings()
		{
			string[] publicationTestData = {
				"<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
				"<lift producer=\"SIL.FLEx 7.3.2.41302\" version=\"0.13\">",
				"<header>",
				"<ranges>",
				"<range id=\"do-not-publish-in\" href=\"\"/>",
				"</ranges>",
				"<fields/>",
				"</header>",
				"<entry dateCreated=\"2013-01-29T08:53:26Z\" dateModified=\"2013-01-29T08:10:28Z\" id=\"baba_f8506500-d17c-4c1b-b05d-ea57f562cb1c\" guid=\"f8506500-d17c-4c1b-b05d-ea57f562cb1c\">",
				"<lexical-unit>",
				"<form lang=\"fr\"><text>baba</text></form>",
				"</lexical-unit>",
				"<trait name=\"morph-type\" value=\"stem\"/>",
				"<trait name=\"do-not-publish-in\" value=\"Main Dictionary\"/>",
				"<sense id=\"62fc5222-aa72-40bb-b3f1-24569bb94042\" dateCreated=\"2013-01-29T08:55:26Z\" dateModified=\"2013-01-29T08:15:28Z\">",
				"<grammatical-info value=\"Noun\">",
				"</grammatical-info>",
				"<gloss lang=\"en\"><text>dad</text></gloss>",
				"<gloss lang=\"fr\"><text>papi</text></gloss>",
				"<definition>",
				"<form lang=\"en\"><text>male parent</text></form>",
				"<form lang=\"fr\"><text>parent masculin</text></form>",
				"</definition>",
				"<trait name=\"do-not-publish-in\" value=\"Pocket\"/>",
				"<example>",
				"<form lang=\"en\"><text>Example Sentence</text></form>",
				"<trait name=\"do-not-publish-in\" value=\"Main Dictionary\"/>",
				"<trait name=\"do-not-publish-in\" value=\"Pocket\"/>",
				"</example>",
				"</sense>",
				"</entry>",
				"</lift>"
			};
			string[] publicationLiftRangeData = {
				"<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
				"<!-- See http://code.google.com/p/lift-standard for more information on the format used here. -->",
				"<lift-ranges>",

				"<range id=\"morph-type\">",
				"<range-element id=\"stem\" guid=\"d7f713e8-e8cf-11d3-9764-00c04f186933\">",
				"<label>",
				"<form lang=\"en\"><text>stem</text></form>",
				"</label>",
				"<abbrev>",
				"<form lang=\"en\"><text>u stem</text></form>",
				"</abbrev>",
				"<description>",
				"<form lang=\"en\"><text>A stem is...</text></form>",
				"</description>",
				"</range-element>",
				"</range>",

				"<range id=\"do-not-publish-in\" guid=\"fc769efe-efd8-4e44-981e-9a07e343bb64\">",

				"<range-element id=\"Main Dictionary\" guid=\"70c0a758-5901-4884-b992-94ca31087607\">",
				"<label>",
				"<form lang=\"en\"><text>Main Dictionary</text></form>",
				"</label>",
				"<abbrev>",
				"<form lang=\"en\"><text>Main</text></form>",
				"<form lang=\"fr\"><text>Principal</text></form>",
				"</abbrev>",
				"</range-element>",
				"<range-element id=\"Pocket\" guid=\"9f699508-3773-4889-87ee-ca0dbd9e3736\">",
				"<label>",
				"<form lang=\"en\"><text>Pocket</text></form>",
				"<form lang=\"fr\"><text>Poche</text></form>",
				"</label>",
				"</range-element>",

				"</range>",

				"</lift-ranges>"
			};
			SetWritingSystems("fr");

			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			Assert.AreEqual(0, repoEntry.Count);
			Assert.AreEqual(0, repoSense.Count);

			// This one should always be there (and the merging one has a different guid!)
			var originalMainDictPubGuid = Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS[0].Guid;
			var importedPocketPubGuid = new Guid("9f699508-3773-4889-87ee-ca0dbd9e3736");

			//Create the LIFT data file
			var sOrigFile = CreateInputFile(publicationTestData);
			//Create the LIFT ranges file
			var sOrigRangesFile = CreateInputRangesFile(publicationLiftRangeData);

			// SUT
			var logFile = TryImportWithRanges(sOrigFile, sOrigRangesFile, 1);
			File.Delete(sOrigFile);
			File.Delete(sOrigRangesFile);

			// Verification
			Assert.IsNotNull(logFile);
			File.Delete(logFile);
			Assert.AreEqual(1, repoEntry.Count);
			Assert.AreEqual(1, repoSense.Count);

			Assert.IsTrue(repoEntry.TryGetObject(new Guid("f8506500-d17c-4c1b-b05d-ea57f562cb1c"), out var entry));
			Assert.AreEqual(1, entry.SensesOS.Count);
			var sense0 = entry.SensesOS[0];
			Assert.AreEqual(sense0.Guid, new Guid("62fc5222-aa72-40bb-b3f1-24569bb94042"));
			Assert.AreEqual(1, sense0.ExamplesOS.Count);
			var example0 = sense0.ExamplesOS[0];

			Assert.IsNotNull(entry.LexemeFormOA);
			Assert.IsNotNull(entry.LexemeFormOA.MorphTypeRA);
			Assert.AreEqual("stem", entry.LexemeFormOA.MorphTypeRA.Name.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("baba", entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text);

			// Verify specific Publication stuff
			Assert.AreEqual(1, entry.DoNotPublishInRC.Count, "Entry has wrong number of Publication settings");
			var mainDictPub = entry.DoNotPublishInRC.First();
			Assert.AreEqual("Main Dictionary", mainDictPub.Name.AnalysisDefaultWritingSystem.Text, "Entry has wrong Publish In setting");
			Assert.AreEqual(originalMainDictPubGuid, mainDictPub.Guid, "Entry has Main Dictionary, but not the one we started out with (different Guid)!");
			Assert.AreEqual(1, sense0.DoNotPublishInRC.Count, "Sense has wrong number of Publication settings");
			var sensePub = sense0.DoNotPublishInRC.First();
			Assert.AreEqual("Pocket", sensePub.Name.AnalysisDefaultWritingSystem.Text, "Sense has wrong Publish In setting");
			Assert.AreEqual(importedPocketPubGuid, sensePub.Guid, "Sense Publish In setting has wrong guid");
			Assert.AreEqual(2, example0.DoNotPublishInRC.Count, "Example has wrong number of Publication settings");
			var examplePublications = example0.DoNotPublishInRC.Select(pub => pub.Name.AnalysisDefaultWritingSystem.Text).ToList();
			Assert.IsTrue(examplePublications.Contains("Main Dictionary"));
			Assert.IsTrue(examplePublications.Contains("Pocket"));
			Assert.That(example0.LiftResidue, Is.Not.StringContaining("do-not-publish-in"));
		}

		/// <summary>
		/// Prove that a custom list with custom list items imports correctly.
		/// </summary>
		[Test]
		public void TestLiftImportOfCustomList()
		{
			const string customListGuid = "cd19e27d-e404-4bf1-9be4-baba071a431f";
			const string customListItemGuid = "821105b6-02a0-4ef2-8ea8-225e120142f7";
			var customListLiftData = new[]
			{
				"<?xml version='1.0' encoding='UTF-8' ?>",
				"<lift producer='SIL.FLEx 8.3.6.42804' version='0.13'>",
				"<header>",
				"<ranges>",
				"<range id='CustomList' href='file://E:/WorkFiles/Lifty/Lifty.lift-ranges'/>",
				"</ranges>",
				"<fields/>",
				"</header>",
				"</lift>"
			};
			var customListRanges = new[]
			{
				"<?xml version='1.0' encoding='UTF-8'?>",
				"<lift-ranges>}",
				"<range id='CustomList' guid='" + customListGuid + "'>",
				"<range-element id='CustomItemOne' guid='" + customListItemGuid + "'>",
				"<label>",
				"<form lang='en'><text>CustomItemOne</text></form>",
				"</label>",
				"<abbrev>",
				"<form lang='en'><text>cio</text></form>",
				"</abbrev>",
				"<description>",
				"<form lang='en'><text>descriptivo</text></form>",
				"</description>",
				"</range-element>",
				"</range>",
				"</lift-ranges>"
			};
			SetWritingSystems("fr");

			//Create the LIFT data file
			var sOrigFile = CreateInputFile(customListLiftData);
			//Create the LIFT ranges file
			var sOrigRangesFile = CreateInputRangesFile(customListRanges);

			// prove that our setup is good and we are importing these objects
			Assert.Throws<KeyNotFoundException>(() => Cache.ServiceLocator.ObjectRepository.GetObject(new Guid(customListGuid)));
			Assert.Throws<KeyNotFoundException>(() => Cache.ServiceLocator.ObjectRepository.GetObject(new Guid(customListItemGuid)));

			// SUT
			var logFile = TryImportWithRanges(sOrigFile, sOrigRangesFile, 0);
			File.Delete(sOrigFile);
			File.Delete(sOrigRangesFile);

			// Verification
			Assert.IsNotNull(logFile);
			File.Delete(logFile);

			var customList = Cache.ServiceLocator.ObjectRepository.GetObject(new Guid(customListGuid)) as ICmPossibilityList;
			Assert.NotNull(customList);
			var customListItem = Cache.ServiceLocator.ObjectRepository.GetObject(new Guid(customListItemGuid));
			Assert.NotNull(customListItem);
			Assert.IsTrue(customListItem is ICmCustomItem);
		}

		/// <summary>
		/// LIFT Import:  test importing data where a phrase has a variant that is also a phrase,
		/// but where there is existing data claiming the allomorph is an affix phrase.
		/// To produce the crash that led to this test, the problem variant must also have
		/// a trait that produces residue and comes before the trait that changes the morph type.
		/// (LT-14372)
		/// </summary>
		[Test]
		public void TestLiftImportChangingAffixToStem()
		{
			string[] badMorphTypeTestData = {
				"<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
				"<lift producer=\"SIL.FLEx 7.3.2.41302\" version=\"0.13\">",
				"<header>",
				"<ranges>",
				"</ranges>",
				"<fields/>",
				"</header>",
				"<entry dateCreated=\"2013-01-29T08:53:26Z\" dateModified=\"2013-01-29T08:10:28Z\" id=\"baba_$guid1\" guid=\"$guid1\">",
				"<lexical-unit>",
				"<form lang=\"fr\"><text>baba baba</text></form>",
				"</lexical-unit>",
				"<trait name=\"morph-type\" value=\"phrase\"/>",
				"<variant>",
				"<form lang=\"fr\"><text>baba baba</text></form>",
				"<trait name=\"nonsence\" value=\"look for this\" />",
				"<trait name=\"morph-type\" value=\"phrase\" />",
				"</variant>",
				"<sense id=\"$guid2\" dateCreated=\"2013-01-29T08:55:26Z\" dateModified=\"2013-01-29T08:15:28Z\">",
				"<gloss lang=\"en\"><text>dad</text></gloss>",
				"</sense>",
				"</entry>",
				"</lift>"
			};
			SetWritingSystems("fr");

			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			Assert.AreEqual(0, repoEntry.Count);
			Assert.AreEqual(0, repoSense.Count);

			// The entry should already be present.
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			sense.Gloss.SetAnalysisDefaultWritingSystem("dad");
			var lf = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.LexemeFormOA = lf;
			lf.Form.SetVernacularDefaultWritingSystem("baba baba");
			var allo = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
			entry.AlternateFormsOS.Add(allo);
			allo.Form.SetVernacularDefaultWritingSystem("baba baba");
			var phrase = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphPhrase);
			lf.MorphTypeRA = phrase;
			allo.MorphTypeRA = phrase;

			//Create the LIFT data file
			badMorphTypeTestData[7] = badMorphTypeTestData[7].Replace("$guid1", entry.Guid.ToString());
			badMorphTypeTestData[16] = badMorphTypeTestData[16].Replace("$guid2", sense.Guid.ToString());
			var sOrigFile = CreateInputFile(badMorphTypeTestData);

			// SUT
			var logFile = TryImport(sOrigFile, null, MergeStyle.MsKeepOnlyNew, 1);
			File.Delete(sOrigFile);

			// Verification
			Assert.IsNotNull(logFile);
			File.Delete(logFile);
			Assert.AreEqual(1, repoEntry.Count);
			Assert.AreEqual(1, repoSense.Count);

			Assert.That(entry.AlternateFormsOS, Has.Count.EqualTo(1), "should still have exactly one allomorph");
			Assert.That(entry.AlternateFormsOS.First(), Is.InstanceOf(typeof(IMoStemAllomorph)), "affix should be changed to stem");
			Assert.That(entry.AlternateFormsOS.First().LiftResidue, Is.StringContaining("look for this"));
		}

		/// <summary>
		/// Test LIFT merger for problems merging pronunciations.
		/// To produce the problem that led to this test, an entry with one or formless pronunciation
		/// gets merged with a LIFT file that has the same entry with other pronunciations. (LT-14725)
		/// </summary>
		[Test]
		public void TestLiftMergeOfPronunciations()
		{
			string[] liftPronunciations = {
				"<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
				"<lift producer=\"SIL.FLEx 8.0.3.41457\" version=\"0.13\">",
				"<header>",
				"<ranges/>",
				"<fields/>",
				"</header>",
				"<entry dateCreated=\"2013-07-02T20:01:04Z\" dateModified=\"2013-07-02T20:05:43Z\" id=\"test_503d3478-3545-4213-9f6b-1f087464e140\" guid=\"503d3478-3545-4213-9f6b-1f087464e140\">",
				"<lexical-unit>",
				"<form lang=\"fr\"><text>test</text></form>",
				"</lexical-unit>",
				"<trait name=\"morph-type\" value=\"stem\"/>",
				"<pronunciation>",
				"<form lang=\"fr\"><text>pronunciation</text></form>",
				"</pronunciation>",
				"<pronunciation>",
				"<form lang=\"es\"><text>pronunciation</text></form>",
				"</pronunciation>",
				"</entry>",
				"<entry dateCreated=\"2013-07-02T20:01:04Z\" dateModified=\"2013-07-02T20:05:43Z\" id=\"test_8d735e34-c555-4390-a0af-21a12e1dd6ff\" guid=\"8d735e34-c555-4390-a0af-21a12e1dd6ff\">",
				"<lexical-unit>",
				"<form lang=\"fr\"><text>testb</text></form>",
				"</lexical-unit>",
				"<trait name=\"morph-type\" value=\"stem\"/>",
				"<pronunciation>",
				"<form lang=\"es\"><text>pronunciation</text></form>",
				"</pronunciation>",
				"</entry>",
				"</lift>"
			};
			SetWritingSystems("fr es");

			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			Assert.AreEqual(0, repoEntry.Count);
			Assert.AreEqual(0, repoSense.Count);

			// The entries should already be present.
			var entry1 = CreateSimpleStemEntry("503d3478-3545-4213-9f6b-1f087464e140", "test");
			AddPronunciation(entry1, "pronunciation", Cache.DefaultVernWs); // add 'fr' pronunciation
			AddPronunciation(entry1, "", -1); // add blank pronunciation, no form
			var entry2 = CreateSimpleStemEntry("8d735e34-c555-4390-a0af-21a12e1dd6ff", "testb");
			AddPronunciation(entry2, "pronunciation", Cache.DefaultVernWs); // add 'fr' pronunciation

			var sOrigFile = CreateInputFile(liftPronunciations);

			// Try to merge in two LIFT file entries that match our two existing entries
			TryImport(sOrigFile, null, MergeStyle.MsKeepBoth, 2);
			File.Delete(sOrigFile);

			// Verification
			Assert.AreEqual(2, repoEntry.Count, "Created some unnecessary entries.");
			Assert.AreEqual(0, repoSense.Count, "Created some unnecessary senses.");
			var repoPronunciation = Cache.ServiceLocator.GetInstance<ILexPronunciationRepository>();
			Assert.AreEqual(5, repoPronunciation.Count, "Wrong number of remaining LexPronunciation objects");
		}

		[Test]
		public void LiftImport_UnknownExampleTraitCreatesResidue()
		{
			var liftDataWithExampleWithUnnkownTrait = new[]
			{
			"<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
			"<lift producer=\"SIL.FLEx 7.0.1.40602\" version=\"0.13\">",
			"<header>",
			"<ranges/>",
			"<fields/>",
			"</header>",
			"<entry dateCreated=\"2011-03-01T18:09:46Z\" dateModified=\"2011-03-01T18:30:07Z\" guid=\"ecfbe958-36a1-4b82-bb69-ca5210355400\" id=\"hombre_ecfbe958-36a1-4b82-bb69-ca5210355400\">",
			"<sense id=\"hombre_f63f1ccf-3d50-417e-8024-035d999d48bc\">",
			"<example>",
			"<form lang=\"en\"><text>Example Sentence</text></form>",
			"<trait name=\"totallyunknowntrait\" value=\"Who are you?\"/>",
			"</example>",
			"</sense>",
			"</entry>",
			"</lift>"
			};
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			Assert.AreEqual(0, repoSense.Count);
			var file = CreateInputFile(liftDataWithExampleWithUnnkownTrait);
			// SUT
			TryImport(file, null, MergeStyle.MsKeepBoth, 1);
			Assert.AreEqual(1, repoSense.Count);
			var sense = repoSense.AllInstances().First();
			Assert.AreEqual(1, sense.ExamplesOS.Count);
			var example = sense.ExamplesOS[0];
			// Important assertion
			Assert.That(example.LiftResidue, Is.StringContaining("totallyunknowntrait"));
		}

		[Test]
		public void LiftImport_ExampleCustomFieldUpdatedDuringMerge()
		{
			var rangesWithStatusList = new[]
			{
				@"<?xml version=""1.0"" encoding=""UTF-8"" ?>",
				@"<lift-ranges>",
				@"<range id=""status"">",
				@"<range-element id=""Confirmed"" guid=""bd80cd3e-ea5e-11de-9871-0013722f8dec"">",
				@"<label>",
				@"<form lang=""en""><text>Confirmed</text></form>",
				@"</label>",
				@"<abbrev>",
				@"<form lang=""en""><text>Conf</text></form>",
				@"</abbrev>",
				@"</range-element>",
				@"<range-element id=""Pending"" guid=""bd964254-ea5e-11de-8cdf-0013722f8dec"">",
				@"<label>",
				@"<form lang=""en""><text>Pending</text></form>",
				@"</label>",
				@"<abbrev>",
				@"<form lang=""en""><text>Pend</text></form>",
				@"</abbrev>",
				@"</range-element>",
				@"</range>",
				@"</lift-ranges>"
			};
			var liftDataWithExampleWithPendingStatus = new[]
			{
				@"<?xml version=""1.0"" encoding=""UTF-8"" ?>",
				@"<lift producer=""SIL.FLEx 8.0.10.41829"" version=""0.13"">",
				@"<header>",
				@"<ranges>",
				@"<range id=""status"" href=""file://C:/Users/zook/Desktop/test/test.lift-ranges""/>",
				@"</ranges>",
				@"<fields>",
				@"<field tag=""EntryStatus"">",
				@"<form lang=""en""><text></text></form>",
				@"<form lang=""qaa-x-spec""><text>Class=LexEntry; Type=ReferenceAtom; WsSelector=kwsAnal; DstCls=CmPossibility; range=status</text></form>",
				@"</field>",
				@"<field tag=""CustomExampleStatus"">",
				@"<form lang=""en""><text></text></form>",
				@"<form lang=""qaa-x-spec""><text>Class=LexExampleSentence; Type=ReferenceAtom; WsSelector=kwsAnal; DstCls=CmPossibility; range=status</text></form>",
				@"</field>",
				@"</fields>",
				@"</header>",
				@"<entry dateCreated=""2013-07-14T21:32:58Z"" dateModified=""2013-07-14T21:46:21Z"" id=""tester_edae30f5-49f0-4025-97ce-3a2022bf7fa3"" guid=""edae30f5-49f0-4025-97ce-3a2022bf7fa3"">",
				@"<lexical-unit>",
				@"<form lang=""fr""><text>tester</text></form>",
				@"</lexical-unit>",
				@"<trait  name=""morph-type"" value=""stem""/>",
				@"<trait name=""EntryStatus"" value=""Pending""/>",
				@"<sense id=""c1811b5c-aec1-42f7-87c2-6bbb4b76ff60"">",
				@"<example source=""A reference"">",
				@"<form lang=""fr""><text>An example sentence</text></form>",
				@"<translation type=""Free translation"">",
				@"<form lang=""en""><text>A translation</text></form>",
				@"</translation>",
				@"<note type=""reference"">",
				@"<form lang=""en""><text>A reference</text></form>",
				@"</note>",
				@"<trait name=""CustomExampleStatus"" value=""Pending""/>",
				@"</example>",
				@"</sense>",
				@"</entry>",
				@"</lift>"
			};
			var liftDataWithExampleWithConfirmedStatus = new[]
			{
				@"<?xml version=""1.0"" encoding=""UTF-8"" ?>",
				@"<lift producer=""SIL.FLEx 8.0.10.41829"" version=""0.13"">",
				@"<header>",
				@"<ranges>",
				@"<range id=""status"" href=""file://C:/Users/zook/Desktop/test/test.lift-ranges""/>",
				@"</ranges>",
				@"<fields>",
				@"<field tag=""EntryStatus"">",
				@"<form lang=""en""><text></text></form>",
				@"<form lang=""qaa-x-spec""><text>Class=LexEntry; Type=ReferenceAtom; WsSelector=kwsAnal; DstCls=CmPossibility; range=status</text></form>",
				@"</field>",
				@"<field tag=""CustomExampleStatus"">",
				@"<form lang=""en""><text></text></form>",
				@"<form lang=""qaa-x-spec""><text>Class=LexExampleSentence; Type=ReferenceAtom; WsSelector=kwsAnal; DstCls=CmPossibility; range=status</text></form>",
				@"</field>",
				@"</fields>",
				@"</header>",
				@"<entry dateCreated=""2014-07-14T21:32:58Z"" dateModified=""2014-07-14T21:46:21Z"" id=""tester_edae30f5-49f0-4025-97ce-3a2022bf7fa3"" guid=""edae30f5-49f0-4025-97ce-3a2022bf7fa3"">",
				@"<lexical-unit>",
				@"<form lang=""fr""><text>tester</text></form>",
				@"</lexical-unit>",
				@"<trait  name=""morph-type"" value=""stem""/>",
				@"<trait name=""EntryStatus"" value=""Confirmed""/>",
				@"<sense id=""c1811b5c-aec1-42f7-87c2-6bbb4b76ff60"">",
				@"<example source=""A reference"">",
				@"<form lang=""fr""><text>An example sentence</text></form>",
				@"<translation type=""Free translation"">",
				@"<form lang=""en""><text>A translation</text></form>",
				@"</translation>",
				@"<note type=""reference"">",
				@"<form lang=""en""><text>A reference</text></form>",
				@"</note>",
				@"<trait name=""CustomExampleStatus"" value=""Confirmed""/>",
				@"</example>",
				@"</sense>",
				@"</entry>",
				@"</lift>"
			};
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			var statusList = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().CreateUnowned("status", wsEn);
			var confirmed = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create(new Guid("bd80cd3e-ea5e-11de-9871-0013722f8dec"), statusList);
			confirmed.Name.set_String(wsEn, TsStringUtils.MakeString("Confirmed", wsEn));
			var pending = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create(new Guid("bd964254-ea5e-11de-8cdf-0013722f8dec"), statusList);
			pending.Name.set_String(wsEn, TsStringUtils.MakeString("Pending", wsEn));
			var entryNew = new FieldDescription(Cache)
			{
				Type = CellarPropertyType.ReferenceAtomic,
				Class = LexEntryTags.kClassId,
				Name = "EntryStatus",
				ListRootId = statusList.Guid
			};
			var exampleNew = new FieldDescription(Cache)
			{
				Type = CellarPropertyType.ReferenceAtomic,
				Class = LexExampleSentenceTags.kClassId,
				Name = "CustomExampleStatus",
				ListRootId = statusList.Guid
			};
			entryNew.UpdateCustomField();
			exampleNew.UpdateCustomField();
			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			Assert.AreEqual(0, repoEntry.Count);
			var rangeFile = CreateInputFile(rangesWithStatusList);
			var pendingLiftFile = CreateInputFile(liftDataWithExampleWithPendingStatus);
			// Verify basic import of custom field data matching existing custom list and items
			TryImport(pendingLiftFile, rangeFile, MergeStyle.MsKeepBoth, 1);
			Assert.AreEqual(1, repoEntry.Count);
			Assert.AreEqual(1, repoSense.Count);
			var entry = repoEntry.AllInstances().First();
			var sense = repoSense.AllInstances().First();
			Assert.AreEqual(1, sense.ExamplesOS.Count);
			var example = sense.ExamplesOS[0];
			var entryCustomData = new CustomFieldData()
			{
				CustomFieldname = "EntryStatus",
				CustomFieldType = CellarPropertyType.ReferenceAtom,
				cmPossibilityNameRA = "Pending"
			};
			var exampleCustomData = new CustomFieldData()
			{
				CustomFieldname = "CustomExampleStatus",
				CustomFieldType = CellarPropertyType.ReferenceAtom,
				cmPossibilityNameRA = "Pending"
			};
			VerifyCustomField(entry, entryCustomData, entryNew.Id);
			VerifyCustomField(example, exampleCustomData, exampleNew.Id);
			// SUT - Verify merging of changes to custom field data
			var confirmedLiftFile = CreateInputFile(liftDataWithExampleWithConfirmedStatus);
			TryImport(confirmedLiftFile, rangeFile, MergeStyle.MsKeepBoth, 1);
			entry = repoEntry.AllInstances().First();
			sense = repoSense.AllInstances().First();
			Assert.AreEqual(1, sense.ExamplesOS.Count);
			example = sense.ExamplesOS[0];
			entryCustomData.cmPossibilityNameRA = "Confirmed";
			exampleCustomData.cmPossibilityNameRA = "Confirmed";
			Assert.AreEqual(1, repoEntry.Count);
			Assert.AreEqual(1, repoSense.Count);
			VerifyCustomField(entry, entryCustomData, entryNew.Id);
			VerifyCustomField(example, exampleCustomData, exampleNew.Id);
		}

		private ILexEntry CreateSimpleStemEntry(string entryGuid, string form)
		{
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(new Guid(entryGuid), Cache.LangProject.LexDbOA);
			var lf = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.LexemeFormOA = lf;
			lf.Form.SetVernacularDefaultWritingSystem(form);
			var stem = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem);
			lf.MorphTypeRA = stem;
			return entry;
		}

		private void AddPronunciation(ILexEntry entry, string pronunciation, int ws)
		{
			var lexPronunciation = Cache.ServiceLocator.GetInstance<ILexPronunciationFactory>().Create();
			entry.PronunciationsOS.Add(lexPronunciation);
			if (ws > 0)
			{
				lexPronunciation.Form.set_String(ws, TsStringUtils.MakeString(pronunciation, ws));
			}
		}

		[Test]
		public void ImportRangeWithNoId_DoesNotDuplicate_ButDoesLoadData()
		{
			SetWritingSystems("fr");
			var lrtFactory = Cache.ServiceLocator.GetInstance<ILexRefTypeFactory>();
			var lrt = lrtFactory.Create();
			Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Add(lrt);
			var noNameRangeData = new[]
			{
			"<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
			"<lift-ranges>",
			"<range id='lexical-relation'>",
			$@"<range-element id=""***"" guid=""{lrt.Guid.ToString()}"">",
			"<trait  name='referenceType' value=''/>",
			"<label>",
			"<form lang='en'><text>Antonym</text></form>",
			"<form lang='de'><text>AntonymG</text></form>",
			"</label>",
			"<description>",
			"<form lang='en'><text>Opposite</text></form>",
			"<form lang='de'><text>OppositeG</text></form>",
			"</description>",
			"<abbrev>",
			"<form lang='en'><text>Ant</text></form>",
			"<form lang='de'><text>AntG</text></form>",
			"</abbrev>",
			"</range-element>",
			"</range>",
			"</lift-ranges>"
			};

			Assert.That(Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS, Has.Count.EqualTo(1), "Should start out with just the one LRT");

			//Create the LIFT data file
			var sOrigFile = CreateInputFile(_minimalLiftData);
			//Create the LIFT ranges file
			var sOrigRangesFile = CreateInputRangesFile(noNameRangeData);

			var logFile = TryImport(sOrigFile, sOrigRangesFile, MergeStyle.MsKeepNew, 1);
			File.Delete(sOrigFile);
			File.Delete(sOrigRangesFile);
			Assert.That(Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS, Has.Count.EqualTo(1), "Should have merged import with LexRefType from input");
			Assert.That(lrt.Name.AnalysisDefaultWritingSystem.Text, Is.EqualTo("Antonym"));
			var de = Cache.WritingSystemFactory.GetWsFromStr("de");
			Assert.That(lrt.Name.get_String(de).Text, Is.EqualTo("AntonymG"));
			Assert.That(lrt.Description.AnalysisDefaultWritingSystem.Text, Is.EqualTo("Opposite"));
			Assert.That(lrt.Description.get_String(de).Text, Is.EqualTo("OppositeG"));
			Assert.That(lrt.Abbreviation.AnalysisDefaultWritingSystem.Text, Is.EqualTo("Ant"));
			Assert.That(lrt.Abbreviation.get_String(de).Text, Is.EqualTo("AntG"));
		}

		[Test]
		public void ImportRangeWithNoId_DoesNotDuplicateGuids_AnthroCode()
		{
			SetWritingSystems("fr");
			var antFactory = Cache.ServiceLocator.GetInstance<ICmAnthroItemFactory>();
			var antItem = antFactory.Create();
			Cache.LangProject.AnthroListOA.PossibilitiesOS.Add(antItem);
			var noNameRangeData1 = new[]
			{
			"<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
			"<lift-ranges>",
			"<range id='anthro-code'>",
			$@"<range-element id=""***"" guid=""{antItem.Guid.ToString()}"">",
			"<trait  name='referenceType' value=''/>",
			"<description>",
			"<form lang='en'><text>Test for Duplicate Anthropology Guid</text></form>",
			"</description>",
			"</range-element>",
			"</range>",
			"</lift-ranges>"
			};

			Assert.That(Cache.LangProject.AnthroListOA.PossibilitiesOS, Has.Count.EqualTo(1), "Should start out with just the one ANT");

			//Create the LIFT data file
			var sOrigFile = CreateInputFile(_minimalLiftData);
			//Create the LIFT ranges file
			var sOrigRangesFile = CreateInputRangesFile(noNameRangeData1);

			TryImport(sOrigFile, sOrigRangesFile, MergeStyle.MsKeepNew, 1);
			File.Delete(sOrigFile);
			File.Delete(sOrigRangesFile);
			Assert.That(Cache.LangProject.AnthroListOA.PossibilitiesOS, Has.Count.EqualTo(1), "Should have merged import with ICmAnthroItem from input");
		}

		[Test]
		public void ImportRangeWithExistingObject_DoesNotDuplicate_UnifiesData()
		{
			SetWritingSystems("fr");
			var lrtFactory = Cache.ServiceLocator.GetInstance<ILexRefTypeFactory>();
			var lrt = lrtFactory.Create();
			Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Add(lrt);
			// The Rubbish values should be ignored because we will set values on those alternatives before merging.
			// The other values should be imported.
			var moreCompleteRangeData = new[]
			{
			"<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
			"<lift-ranges>",
			"<range id='lexical-relation'>",
			$@"<range-element id=""***"" guid=""{lrt.Guid.ToString()}"">",
			"<trait  name='referenceType' value=''/>",
			"<label>",
			"<form lang='en'><text>AntonymRubbish</text></form>",
			"<form lang='de'><text>AntonymG</text></form>",
			"</label>",
			"<description>",
			"<form lang='en'><text>OppositeRubbish</text></form>",
			"<form lang='de'><text>OppositeG</text></form>",
			"</description>",
			"<abbrev>",
			"<form lang='en'><text>Ant</text></form>",
			"<form lang='de'><text>AntGRubbish</text></form>",
			"</abbrev>",
			"</range-element>",
			"</range>",
			"</lift-ranges>"
			};
			Assert.That(Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS, Has.Count.EqualTo(1), "Should start out with just the one LRT");
			var en = Cache.WritingSystemFactory.GetWsFromStr("en");
			var de = Cache.WritingSystemFactory.GetWsFromStr("de");
			lrt.Name.AnalysisDefaultWritingSystem = TsStringUtils.MakeString("Antonym", en);
			lrt.Description.AnalysisDefaultWritingSystem = TsStringUtils.MakeString("Opposite", en);
			lrt.Description.set_String(de, TsStringUtils.MakeString("OppositeG", de));
			lrt.Abbreviation.set_String(de, TsStringUtils.MakeString("AntG", de));

			//Create the LIFT data file
			var sOrigFile = CreateInputFile(_minimalLiftData);
			//Create the LIFT ranges file
			var sOrigRangesFile = CreateInputRangesFile(moreCompleteRangeData);

			var logFile = TryImport(sOrigFile, sOrigRangesFile, MergeStyle.MsKeepNew, 1);
			File.Delete(sOrigFile);
			File.Delete(sOrigRangesFile);
			Assert.That(Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS, Has.Count.EqualTo(1), "Should have merged import with LexRefType from input");
			Assert.That(lrt.Name.AnalysisDefaultWritingSystem.Text, Is.EqualTo("Antonym"));
			Assert.That(lrt.Name.get_String(de).Text, Is.EqualTo("AntonymG"));
			Assert.That(lrt.Description.AnalysisDefaultWritingSystem.Text, Is.EqualTo("Opposite"));
			Assert.That(lrt.Description.get_String(de).Text, Is.EqualTo("OppositeG"));
			Assert.That(lrt.Abbreviation.AnalysisDefaultWritingSystem.Text, Is.EqualTo("Ant"));
			Assert.That(lrt.Abbreviation.get_String(de).Text, Is.EqualTo("AntG"));

			var log = File.ReadAllText(logFile, Encoding.UTF8);
			Assert.That(log, Is.StringContaining("AntonymRubbish"));
			Assert.That(log, Is.StringContaining("had a conflicting value"));
			Assert.That(log, Is.StringContaining("Description"));
			Assert.That(log, Is.Not.StringContaining("OppositeG"), "should not report conflict when values are equal");
		}
		[Test]
		public void TestImportDoesNotDuplicateSequenceRelations()
		{
			string[] sequenceLiftData = {
				"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>",
				"<lift producer=\"SIL.FLEx 7.2.4.41003\" version=\"0.13\">",
				"  <header>",
				"    <fields>",
				"    </fields>",
				"  </header>",
				"<entry dateCreated='2012-11-05T20:39:08Z' dateModified='2012-11-05T20:40:14Z' id='cold_97b8a20d-9989-430d-8a20-2f95592d60cb' guid='97b8a20d-9989-430d-8a20-2f95592d60cb'>"
				,
				"<lexical-unit>",
				"<form lang='en'><text>cold</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='57f884c0-0df2-43bf-8ba7-c70b2a208cf1'>",
				"<gloss lang='en'><text>cold</text></gloss>",
				"<relation type='Calendar' ref='57f884c0-0df2-43bf-8ba7-c70b2a208cf1' order='1'/>",
				"<relation type='Calendar' ref='136a83c8-dcde-4499-b645-0103b7c5763e' order='2'/>",
				"</sense>",
				"</entry>",
				"<entry dateCreated='2012-11-05T20:40:14Z' dateModified='2012-11-05T20:40:14Z' id='cool_ce707f68-2e25-4073-837f-9b4deb9e5b36' guid='ce707f68-2e25-4073-837f-9b4deb9e5b36'>"
				,
				"<lexical-unit>",
				"<form lang='en'><text>cool</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='136a83c8-dcde-4499-b645-0103b7c5763e'>",
				"<gloss lang='en'><text>cool</text></gloss>",
				"<relation type='Calendar' ref='57f884c0-0df2-43bf-8ba7-c70b2a208cf1' order='1'/>",
				"<relation type='Calendar' ref='136a83c8-dcde-4499-b645-0103b7c5763e' order='2'/>",
				"</sense>",
				"</entry>",
				"<entry dateCreated='2012-11-05T20:44:27Z' dateModified='2012-11-05T20:44:27Z' id='cooler_e7a5b85a-2ea5-44e3-8f57-9bc2759803ca' guid='e7a5b85a-2ea5-44e3-8f57-9bc2759803ca'>"
				,
				"<lexical-unit>",
				"<form lang='en'><text>cooler</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='42510a32-787c-4162-80b1-0f94ef2eb3bf'>",
				"<gloss lang='en'><text>cooler</text></gloss>",
				"</sense>",
				"</entry>",
				"</lift>"
			};
			string[] sequenceLiftData2 = {
				"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>",
				"<lift producer=\"SIL.FLEx 7.2.4.41003\" version=\"0.13\">",
				"  <header>",
				"    <fields>",
				"    </fields>",
				"  </header>",
				"<entry dateCreated='2012-11-05T20:39:08Z' dateModified='2012-11-05T20:50:14Z' id='cold_97b8a20d-9989-430d-8a20-2f95592d60cb' guid='97b8a20d-9989-430d-8a20-2f95592d60cb'>"
				,
				"<lexical-unit>",
				"<form lang='en'><text>cold</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='57f884c0-0df2-43bf-8ba7-c70b2a208cf1'>",
				"<gloss lang='en'><text>cold</text></gloss>",
				"<relation type='Calendar' ref='57f884c0-0df2-43bf-8ba7-c70b2a208cf1' order='1'/>",
				"<relation type='Calendar' ref='42510a32-787c-4162-80b1-0f94ef2eb3bf' order='2'/>",
				"</sense>",
				"</entry>",
				"<entry dateCreated='2012-11-05T20:40:14Z' dateModified='2012-11-05T20:50:14Z' id='cool_ce707f68-2e25-4073-837f-9b4deb9e5b36' guid='ce707f68-2e25-4073-837f-9b4deb9e5b36'>"
				,
				"<lexical-unit>",
				"<form lang='en'><text>cool</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='136a83c8-dcde-4499-b645-0103b7c5763e'>",
				"<gloss lang='en'><text>cool</text></gloss>",
				"<relation type='Calendar' ref='57f884c0-0df2-43bf-8ba7-c70b2a208cf1' order='1'/>",
				"<relation type='Calendar' ref='42510a32-787c-4162-80b1-0f94ef2eb3bf' order='2'/>",
				"</sense>",
				"</entry>",
				"<entry dateCreated='2012-11-05T20:44:27Z' dateModified='2012-11-05T20:54:27Z' id='cooler_e7a5b85a-2ea5-44e3-8f57-9bc2759803ca' guid='e7a5b85a-2ea5-44e3-8f57-9bc2759803ca'>"
				,
				"<lexical-unit>",
				"<form lang='en'><text>cooler</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='42510a32-787c-4162-80b1-0f94ef2eb3bf'>",
				"<gloss lang='en'><text>cooler</text></gloss>",
				"<relation type='Calendar' ref='57f884c0-0df2-43bf-8ba7-c70b2a208cf1' order='1'/>",
				"<relation type='Calendar' ref='42510a32-787c-4162-80b1-0f94ef2eb3bf' order='2'/>",
				"</sense>",
				"</entry>",
				"</lift>"
			};
			//This test is for the issue documented in LT-13747
			SetWritingSystems("fr");

			CreateNeededStyles();

			var senseRepo = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();

			var sOrigFile = CreateInputFile(sequenceLiftData);
			var logFile = TryImport(sOrigFile, null, MergeStyle.MsKeepNew, 3);
			var coldSense = senseRepo.GetObject(new Guid("57f884c0-0df2-43bf-8ba7-c70b2a208cf1"));

			Assert.AreEqual(1, coldSense.LexSenseReferences.Count(), "Too many LexSenseReferences, import has issues.");
			Assert.AreEqual(2, coldSense.LexSenseReferences.First().TargetsRS.Count, "Incorrect number of references, part relations not imported correctly.");

			var sNewFile = CreateInputFile(sequenceLiftData2);
			TryImport(sNewFile, null, MergeStyle.MsKeepOnlyNew, 3);
			const string coolerGuid = "42510a32-787c-4162-80b1-0f94ef2eb3bf";
			var coolerSense = senseRepo.GetObject(new Guid(coolerGuid));

			//There should be 1 LexSenseReference representing the new cool, cooler order.
			Assert.AreEqual(1, coldSense.LexSenseReferences.Count(), "Too many LexSenseReferences, the relation was not merged.");
			Assert.AreEqual(2, coldSense.LexSenseReferences.First().TargetsRS.Count, "Incorrect number of references, part relations not imported correctly.");
			Assert.AreEqual(coolerGuid, coldSense.LexSenseReferences.First().TargetsRS[1].Guid.ToString(), "Sequence incorrectly modified.");
			Assert.AreEqual(1, coolerSense.LexSenseReferences.Count(), "Incorrect number of references in the leg sense.");
			Assert.AreEqual(2, coolerSense.LexSenseReferences.First().TargetsRS.Count, "Incorrect number of targets in the leg sense.");
		}

		[Test]
		public void TestImportRemovesItemFromComponentRelation()
		{
			string[] componentData = {
				"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>",
				"<lift producer=\"SIL.FLEx 7.2.4.41003\" version=\"0.13\">",
				"<header>",
				"<fields></fields>",
				"</header>",
				"<entry dateCreated='2012-11-07T20:40:33Z' dateModified='2012-11-07T20:41:06Z' id='cold_d76f4068-833e-40a8-b4d5-5f4ba785bf6e' guid='d76f4068-833e-40a8-b4d5-5f4ba785bf6e'>"
				,
				"<lexical-unit>",
				"<form lang='en'> <text>cold</text>",
				"</form>",
				"</lexical-unit>",
				"<trait name='morph-type' value='stem' />",
				"<relation type='Compare' ref='cool_d5222b80-e3f2-4016-a17b-3ae13718e70d' />",
				"<relation type='Compare' ref='cooler_03237d6e-a327-436b-8ae3-b84eed3549fd' />",
				"<sense id='e6d3dd67-27b2-4c2b-91ae-da05c740cbd7'></sense>",
				"</entry>",
				"<entry dateCreated='2012-11-07T20:41:06Z' dateModified='2012-11-07T20:41:06Z' id='cool_d5222b80-e3f2-4016-a17b-3ae13718e70d' guid='d5222b80-e3f2-4016-a17b-3ae13718e70d'>"
				,
				"<lexical-unit>",
				"<form lang='en'><text>cool</text></form>",
				"</lexical-unit>",
				"<trait name='morph-type' value='stem' />",
				"<relation type='Compare' ref='cold_d76f4068-833e-40a8-b4d5-5f4ba785bf6e' />",
				"<relation type='Compare' ref='cooler_03237d6e-a327-436b-8ae3-b84eed3549fd' />",
				"<sense id='5e9a79ee-68a4-48e2-81fc-30d9f6b11eb3'></sense>",
				"</entry>",
				"<entry dateCreated='2012-11-07T20:41:19Z' dateModified='2012-11-07T20:51:56Z' id='cooler_03237d6e-a327-436b-8ae3-b84eed3549fd' guid='03237d6e-a327-436b-8ae3-b84eed3549fd'>"
				,
				"<lexical-unit>",
				"<form lang='en'><text>cooler</text></form>",
				"</lexical-unit>",
				"<trait name='morph-type' value='stem' />",
				"<relation type='Compare' ref='cool_d5222b80-e3f2-4016-a17b-3ae13718e70d' />",
				"<relation type='Compare' ref='cold_d76f4068-833e-40a8-b4d5-5f4ba785bf6e' />",
				"<sense id='83decc9c-89d2-460f-842e-f69a84dc9dd4'></sense>",
				"</entry>",
				"</lift>"
			};
			string[] componentData2 = {
				"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>",
				"<lift producer=\"SIL.FLEx 7.2.4.41003\" version=\"0.13\">",
				"<header>",
				"<fields></fields>",
				"</header>",
				"<entry dateCreated='2012-11-07T20:40:33Z' dateModified='2012-11-07T20:45:06Z' id='cold_d76f4068-833e-40a8-b4d5-5f4ba785bf6e' guid='d76f4068-833e-40a8-b4d5-5f4ba785bf6e'>"
				,
				"<lexical-unit>",
				"<form lang='en'> <text>cold</text>",
				"</form>",
				"</lexical-unit>",
				"<trait name='morph-type' value='stem' />",
				"<relation type='Compare' ref='cool_d5222b80-e3f2-4016-a17b-3ae13718e70d' />",
				"<sense id='e6d3dd67-27b2-4c2b-91ae-da05c740cbd7'></sense>",
				"</entry>",
				"<entry dateCreated='2012-11-07T20:41:06Z' dateModified='2012-11-07T20:45:06Z' id='cool_d5222b80-e3f2-4016-a17b-3ae13718e70d' guid='d5222b80-e3f2-4016-a17b-3ae13718e70d'>"
				,
				"<lexical-unit>",
				"<form lang='en'><text>cool</text></form>",
				"</lexical-unit>",
				"<trait name='morph-type' value='stem' />",
				"<relation type='Compare' ref='cold_d76f4068-833e-40a8-b4d5-5f4ba785bf6e' />",
				"<sense id='5e9a79ee-68a4-48e2-81fc-30d9f6b11eb3'></sense>",
				"</entry>",
				"<entry dateCreated='2012-11-07T20:41:19Z' dateModified='2012-11-07T20:55:56Z' id='cooler_03237d6e-a327-436b-8ae3-b84eed3549fd' guid='03237d6e-a327-436b-8ae3-b84eed3549fd'>"
				,
				"<lexical-unit>",
				"<form lang='en'><text>cooler</text></form>",
				"</lexical-unit>",
				"<trait name='morph-type' value='stem' />",
				"<sense id='83decc9c-89d2-460f-842e-f69a84dc9dd4'></sense>",
				"</entry>",
				"</lift>"
			};
			//This test is for the issue documented in LT-13764
			SetWritingSystems("fr");

			CreateNeededStyles();

			var entryRepo = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();

			var sOrigFile = CreateInputFile(componentData);
			TryImport(sOrigFile, null, MergeStyle.MsKeepNew, 3);
			var coldEntry = entryRepo.GetObject(new Guid("d76f4068-833e-40a8-b4d5-5f4ba785bf6e"));
			var ler = coldEntry.LexEntryReferences;
			Assert.AreEqual(3, coldEntry.LexEntryReferences.ElementAt(0).TargetsRS.Count, "Incorrect number of component references.");

			var sNewFile = CreateInputFile(componentData2);
			TryImport(sNewFile, null, MergeStyle.MsKeepOnlyNew, 3);
			const string coolerGuid = "03237d6e-a327-436b-8ae3-b84eed3549fd";
			Assert.AreEqual(2, coldEntry.LexEntryReferences.ElementAt(0).TargetsRS.Count, "Incorrect number of component references.");
			var coolerEntry = entryRepo.GetObject(new Guid(coolerGuid));
			Assert.AreEqual(0, coolerEntry.LexEntryReferences.Count());
		}


		[Test]
		public void TestImportDoesNotSplitComponentCollection()
		{
			SetWritingSystems("fr");

			CreateNeededStyles();

			var entryRepository = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();

			var sOrigFile = CreateInputFile(_componentTest);
			TryImport(sOrigFile, null, MergeStyle.MsKeepNew, 3);
			var todoEntry = entryRepository.GetObject(new Guid("10af904a-7395-4a37-a195-44001127ae40"));
			Assert.AreEqual(1, todoEntry.LexEntryReferences.Count());
			Assert.AreEqual(3, todoEntry.LexEntryReferences.First().TargetsRS.Count);
		}

		[Test]
		public void TestImportWarnsOnNonSubsetCollectionMerge()
		{
			string[] componentTest2 = {
				"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>",
				"<lift producer=\"SIL.FLEx 7.2.4.41003\" version=\"0.13\">",
				"  <header>",
				"    <fields>",
				"    </fields>",
				"  </header>",
				"  <entry dateCreated=\"2011-06-27T21:45:52Z\" dateModified=\"2011-06-29T14:57:28Z\" id=\"do_be4eb9fd-58fd-49fe-a8ef-e13a96646806\" guid=\"be4eb9fd-58fd-49fe-a8ef-e13a96646806\">"
				,
				"    <lexical-unit>",
				"      <form lang=\"fr\"><text>do</text></form>",
				"    </lexical-unit>",
				"    <trait  name=\"morph-type\" value=\"stem\"/>",
				"    <sense id=\"3e0ae703-db7f-4687-9cf5-481524095905\">",
				"    </sense>",
				"  </entry>",
				"  <entry dateCreated=\"2011-06-29T15:58:03Z\" dateModified=\"2011-06-29T15:58:03Z\" id=\"todo_10af904a-7395-4a37-a195-44001127ae40\" guid=\"10af904a-7395-4a37-a195-44001127ae40\">"
				,
				"    <lexical-unit>",
				"      <form lang=\"fr\"><text>todo</text></form>",
				"    </lexical-unit>",
				"    <trait  name=\"morph-type\" value=\"stem\"/>",
				"<relation type=\"Compare\" ref=\"to_9bfa6fc4-0a2d-44ff-9c2c-91460ef3c856\"/>",
				"<relation type=\"Compare\" ref=\"zoo_6bfa6fc4-0a2d-44ff-9c2c-91460ef3c856\"/>",
				"    <sense id=\"2759532a-26db-4850-9cba-b3684f0a3f5f\">",
				"    </sense>",
				"  </entry>",
				"  <entry dateCreated=\"2011-06-29T15:58:03Z\" dateModified=\"2011-06-29T15:58:03Z\" id=\"to_9bfa6fc4-0a2d-44ff-9c2c-91460ef3c856\" guid=\"9bfa6fc4-0a2d-44ff-9c2c-91460ef3c856\">"
				,
				"    <lexical-unit>",
				"      <form lang=\"fr\"><text>to</text></form>",
				"    </lexical-unit>",
				"    <trait  name=\"morph-type\" value=\"stem\"/>",
				"<relation type=\"Compare\" ref=\"todo_10af904a-7395-4a37-a195-44001127ae40\"/>",
				"<relation type=\"Compare\" ref=\"zoo_6bfa6fc4-0a2d-44ff-9c2c-91460ef3c856\"/>",
				"    <sense id=\"2759532a-26db-4850-9cba-b3684f0a3f5f\">",
				"    </sense>",
				"  </entry>",
				"  <entry dateCreated=\"2011-06-29T15:58:03Z\" dateModified=\"2011-06-29T15:58:03Z\" id=\"zoo_6bfa6fc4-0a2d-44ff-9c2c-91460ef3c856\" guid=\"6bfa6fc4-0a2d-44ff-9c2c-91460ef3c856\">"
				,
				"    <lexical-unit>",
				"      <form lang=\"fr\"><text>zoo</text></form>",
				"    </lexical-unit>",
				"    <trait  name=\"morph-type\" value=\"stem\"/>",
				"<relation type=\"Compare\" ref=\"todo_10af904a-7395-4a37-a195-44001127ae40\"/>",
				"<relation type=\"Compare\" ref=\"to_9bfa6fc4-0a2d-44ff-9c2c-91460ef3c856\"/>",
				"    <sense id=\"2759532a-26db-4850-9cba-b3684f0a3f5f\">",
				"    </sense>",
				"  </entry>",
				"</lift>"
			};
			SetWritingSystems("fr");

			CreateNeededStyles();

			var entryRepository = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();

			var sOrigFile = CreateInputFile(_componentTest);
			TryImport(sOrigFile, null, MergeStyle.MsKeepNew, 3);
			var sMergeFile = CreateInputFile(componentTest2);
			var logFile = TryImport(sMergeFile, null, MergeStyle.MsKeepNew, 4);
			var todoEntry = entryRepository.GetObject(new Guid("10af904a-7395-4a37-a195-44001127ae40"));
			Assert.AreEqual(1, todoEntry.LexEntryReferences.Count());
			Assert.AreEqual(3, todoEntry.LexEntryReferences.First().TargetsRS.Count);
			using (var stream = new StreamReader(logFile))
			{
				var data = stream.ReadToEnd();
				stream.Close();
				Assert.IsTrue(data.Contains("Combined Collections"), "Logfile does not show conflict for collection.");
			}
		}

		[Test]
		public void TestImportDoesNotSplitComplexForms_LT12948()
		{
			string[] lt12948Test = {
				"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>",
				"<lift producer=\"SIL.FLEx 7.2.4.41003\" version=\"0.13\">",
				"  <header>",
				"    <fields>",
				"    </fields>",
				"  </header>",
				"  <entry dateCreated=\"2011-06-27T21:45:52Z\" dateModified=\"2011-06-29T14:57:28Z\" id=\"do_be4eb9fd-58fd-49fe-a8ef-e13a96646806\" guid=\"be4eb9fd-58fd-49fe-a8ef-e13a96646806\">"
				,
				"    <lexical-unit>",
				"      <form lang=\"fr\"><text>do</text></form>",
				"    </lexical-unit>",
				"    <trait  name=\"morph-type\" value=\"stem\"/>",
				"    <sense id=\"3e0ae703-db7f-4687-9cf5-481524095905\">",
				"    </sense>",
				"  </entry>",
				"  <entry dateCreated=\"2011-06-29T15:58:03Z\" dateModified=\"2011-06-29T15:58:03Z\" id=\"todo_10af904a-7395-4a37-a195-44001127ae40\" guid=\"10af904a-7395-4a37-a195-44001127ae40\">"
				,
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
				"  <entry dateCreated=\"2011-06-29T15:58:03Z\" dateModified=\"2011-06-29T15:58:03Z\" id=\"to_9bfa6fc4-0a2d-44ff-9c2c-91460ef3c856\" guid=\"9bfa6fc4-0a2d-44ff-9c2c-91460ef3c856\">"
				,
				"    <lexical-unit>",
				"      <form lang=\"fr\"><text>todo</text></form>",
				"    </lexical-unit>",
				"    <trait  name=\"morph-type\" value=\"stem\"/>",
				"    <sense id=\"2759532a-26db-4850-9cba-b3684f0a3f5f\">",
				"    </sense>",
				"  </entry>",
				"</lift>"
			};
			SetWritingSystems("fr");

			CreateNeededStyles();

			var entryRepository = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();

			var sOrigFile = CreateInputFile(lt12948Test);
			TryImport(sOrigFile, null, MergeStyle.MsKeepNew, 3);
			var todoEntry = entryRepository.GetObject(new Guid("10af904a-7395-4a37-a195-44001127ae40"));
			//Even though they do not have an order set (due to a now fixed export defect) the two relations in the 'todo' entry
			//should be collected in the same LexEntryRef
			Assert.AreEqual(1, todoEntry.ComplexFormEntryRefs.Count());
			Assert.AreEqual(2, todoEntry.ComplexFormEntryRefs.First().ComponentLexemesRS.Count);
		}


		[Test]
		public void TestImportSplitsDifferingComplexFormsByType_LT12948()
		{
			SetWritingSystems("fr");

			CreateNeededStyles();

			var entryRepository = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();

			var sOrigFile = CreateInputFile(_LT12948Test2);
			TryImport(sOrigFile, null, MergeStyle.MsKeepNew, 3);
			var todoEntry = entryRepository.GetObject(new Guid("10af904a-7395-4a37-a195-44001127ae40"));
			//Even though they do not have an order set (due to a now fixed export defect) the two relations in the 'todo' entry
			//should be collected in the same LexEntryRef
			Assert.AreEqual(1, todoEntry.ComplexFormEntryRefs.Count(), "Too many ComplexFormEntryRefs? Then they were incorrectly split.");
			Assert.AreEqual(2, todoEntry.ComplexFormEntryRefs.First().ComponentLexemesRS.Count, "Wrong number of Components.");
			Assert.AreEqual(1, todoEntry.VariantEntryRefs.Count(), "Wrong number of VariantEntryRefs.");
		}

		/// <summary>
		/// LIFT Import:  Test that two component lists which share an entry are considered the same
		/// list and merged rather than put in two different lists of components.
		/// </summary>
		[Test]
		public void TestMergeWithDiffComponentListKeepOld()
		{
			string[] mergeTestOld = {
				"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>",
				"<lift producer=\"SIL.FLEx 7.2.4.41003\" version=\"0.13\">",
				"  <header>",
				"    <fields>",
				"    </fields>",
				"  </header>",
				"  <entry dateCreated=\"2011-06-27T21:45:52Z\" dateModified=\"2011-06-29T14:57:28Z\" id=\"do_be4eb9fd-58fd-49fe-a8ef-e13a96646806\" guid=\"be4eb9fd-58fd-49fe-a8ef-e13a96646806\">"
				,
				"    <lexical-unit>",
				"      <form lang=\"fr\"><text>do</text></form>",
				"    </lexical-unit>",
				"    <trait  name=\"morph-type\" value=\"stem\"/>",
				"    <sense id=\"3e0ae703-db7f-4687-9cf5-481524095905\">",
				"    </sense>",
				"  </entry>",
				"  <entry dateCreated=\"2011-06-29T15:58:03Z\" dateModified=\"2011-06-29T15:56:03Z\" id=\"todo_10af904a-7395-4a37-a195-44001127ae40\" guid=\"10af904a-7395-4a37-a195-44001127ae40\">"
				,
				"    <lexical-unit>",
				"      <form lang=\"fr\"><text>todo</text></form>",
				"    </lexical-unit>",
				"    <trait  name=\"morph-type\" value=\"stem\"/>",
				"<relation type=\"_component-lexeme\" ref=\"to_9bfa6fc4-0a2d-44ff-9c2c-91460ef3c856\" order=\"1\">",
				"<trait name=\"is-primary\" value=\"true\"/>",
				"<trait name=\"variant-type\" value=\"Dialectal Variant\"/>",
				"</relation>",
				"<relation type=\"_component-lexeme\" ref=\"do_be4eb9fd-58fd-49fe-a8ef-e13a96646806\" order=\"1\">",
				"<trait name=\"is-primary\" value=\"true\"/>",
				"<trait name=\"complex-form-type\" value=\"Compound\"/>",
				"</relation>",
				"<relation type=\"_component-lexeme\" ref=\"zoo_6bfa6fc4-0a2d-44ff-9c2c-91460ef3c856\" order=\"2\">",
				"<trait name=\"is-primary\" value=\"true\"/>",
				"<trait name=\"complex-form-type\" value=\"Compound\"/>",
				"</relation>",
				"    <sense id=\"2759532a-26db-4850-9cba-b3684f0a3f5f\">",
				"    </sense>",
				"  </entry>",
				"  <entry dateCreated=\"2011-06-29T15:58:03Z\" dateModified=\"2011-06-29T15:58:03Z\" id=\"to_9bfa6fc4-0a2d-44ff-9c2c-91460ef3c856\" guid=\"9bfa6fc4-0a2d-44ff-9c2c-91460ef3c856\">"
				,
				"    <lexical-unit>",
				"      <form lang=\"fr\"><text>todo</text></form>",
				"    </lexical-unit>",
				"    <trait  name=\"morph-type\" value=\"stem\"/>",
				"    <sense id=\"2759532a-26db-4850-9cba-b3684f0a3f5f\">",
				"    </sense>",
				"  </entry>",
				"  <entry dateCreated=\"2011-06-29T15:58:03Z\" dateModified=\"2011-06-29T15:58:03Z\" id=\"zoo_6bfa6fc4-0a2d-44ff-9c2c-91460ef3c856\" guid=\"6bfa6fc4-0a2d-44ff-9c2c-91460ef3c856\">"
				,
				"    <lexical-unit>",
				"      <form lang=\"fr\"><text>todo</text></form>",
				"    </lexical-unit>",
				"    <trait  name=\"morph-type\" value=\"stem\"/>",
				"    <sense id=\"2759532a-26db-4850-9cba-b3684f0a3f5f\">",
				"    </sense>",
				"  </entry>",
				"</lift>"
			};
			SetWritingSystems("fr");

			CreateNeededStyles();

			var entryRepository = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();

			var sOrigFile = CreateInputFile(mergeTestOld);
			TryImport(sOrigFile, null, MergeStyle.MsKeepNew, 4);

			var sNewFile = CreateInputFile(_LT12948Test2);
			TryImport(sNewFile, null, MergeStyle.MsKeepNew, 3);
			var todoEntry = entryRepository.GetObject(new Guid("10af904a-7395-4a37-a195-44001127ae40"));

			//Even though they do not have an order set (due to a now fixed export defect) the two relations in the 'todo' entry
			//should be collected in the same LexEntryRef
			Assert.AreEqual(1, todoEntry.ComplexFormEntryRefs.Count(), "Too many ComplexForms, they were incorrectly split.");
			Assert.AreEqual(1, todoEntry.VariantEntryRefs.Count(), "Wrong number of VariantEntryRefs.");
			Assert.AreEqual(1, todoEntry.VariantEntryRefs.First().ComponentLexemesRS.Count, "Incorrect number of Variants.");
			Assert.AreEqual(2, todoEntry.ComplexFormEntryRefs.First().ComponentLexemesRS.Count, "Incorrect number of components.");
		}

		[Test]
		public void TestImportDoesNotSplitSynonyms_LT12948()
		{
			string[] lt12948Test3 = {
				"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>",
				"<lift producer=\"SIL.FLEx 7.2.4.41003\" version=\"0.13\">",
				"  <header>",
				"    <fields>",
				"    </fields>",
				"  </header>",
				"<entry dateCreated='2012-04-11T19:45:34Z' dateModified='2012-04-11T19:49:42Z' id='house_7e4e4aed-0b2e-4e2b-9c84-4466b8e73ea4' guid='7e4e4aed-0b2e-4e2b-9c84-4466b8e73ea4'>"
				,
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
				"<entry dateCreated='2012-04-11T19:45:34Z' dateModified='2012-04-11T19:49:42Z' id='bob' guid='7e6e4aed-0b2e-4e2b-9c84-4466b8e73ea4'>"
				,
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
				"<entry dateCreated='2012-04-11T19:49:42Z' dateModified='2012-04-11T19:49:42Z' id='bungalo_885f3937-7761-406c-a46b-ef71e2f10334' guid='885f3937-7761-406c-a46b-ef71e2f10334'>"
				,
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
				"<entry dateCreated='2012-04-11T19:45:34Z' dateModified='2012-04-11T19:49:42Z' id='builder' guid='7e5e4aed-0b2e-4e2b-9c84-4466b8e73ea4'>"
				,
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
				"<entry dateCreated='2012-04-11T19:49:57Z' dateModified='2012-04-11T19:49:57Z' id='castle_00c8535d-0be5-45c3-9d70-0a7840325fed' guid='00c8535d-0be5-45c3-9d70-0a7840325fed'>"
				,
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
			SetWritingSystems("fr");

			CreateNeededStyles();

			var entryRepository = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var senseRepository = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();

			var sOrigFile = CreateInputFile(lt12948Test3);
			TryImport(sOrigFile, null, MergeStyle.MsKeepNew, 5);
			var bungaloSense = senseRepository.GetObject(new Guid("4bb72859-623b-4616-aa10-a6b0005a2f4b"));
			var bobEntry = entryRepository.GetObject(new Guid("7e6e4aed-0b2e-4e2b-9c84-4466b8e73ea4"));
			//Even though they do not have an order set (due to a now fixed export defect) the two relations in the 'todo' entry
			//should be collected in the same LexEntryRef
			Assert.AreEqual(1, bungaloSense.LexSenseReferences.Count());
			Assert.AreEqual(3, bungaloSense.LexSenseReferences.First().TargetsRS.Count);
			Assert.AreEqual(1, bobEntry.LexEntryReferences.Count());
			Assert.AreEqual(2, bobEntry.LexEntryReferences.First().TargetsRS.Count);
		}

		[Test]
		public void TestImportDoesNotDuplicateTreeRelations()
		{
			// This data represents a lift file with 3 entries of form 'arm', 'leg', and 'body' with a whole/part relationship between 'arm' and 'body'
			string[] treeLiftData = {
				"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>",
				"<lift producer=\"SIL.FLEx 7.2.4.41003\" version=\"0.13\">",
				"  <header>",
				"    <fields>",
				"    </fields>",
				"    <ranges>",
				"<range id='lexical-relation' href='???'/>",
				"    </ranges>",
				"  </header>",
				"<entry dateCreated='2012-04-19T17:47:25Z' dateModified='2012-04-19T18:15:31Z' id='arm_835b9236-c6a4-48fe-b66e-673e40ff040d' guid='835b9236-c6a4-48fe-b66e-673e40ff040d'>",
				"<lexical-unit>",
				"<form lang='en'><text>arm</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='5ca96ad0-cb18-4ddc-be8e-3547fc87221f'>",
				"<gloss lang='en'><text>arm</text></gloss>",
				"<relation type='Whole' ref='52c632c2-98ad-4f97-b130-2a32992254e3'/>",
				"</sense>",
				"</entry>",
				"<entry dateCreated='2012-04-19T17:49:04Z' dateModified='2012-04-19T18:15:44Z' id='body_a79278be-d698-4def-b104-c4303615683f' guid='a79278be-d698-4def-b104-c4303615683f'>",
				"<lexical-unit>",
				"<form lang='en'><text>body</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='52c632c2-98ad-4f97-b130-2a32992254e3'>",
				"<gloss lang='en'><text>body</text></gloss>",
				"<relation type='Part' ref='5ca96ad0-cb18-4ddc-be8e-3547fc87221f'/>",
				"</sense>",
				"</entry>",
				"<entry dateCreated='2012-04-19T17:49:04Z' dateModified='2012-04-19T18:15:44Z' id='leg_b79278be-d698-4def-b104-c4303615683f' guid='b79278be-d698-4def-b104-c4303615683f'>",
				"<lexical-unit>",
				"<form lang='en'><text>leg</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='62c632c2-98ad-4f97-b130-2a32992254e3'>",
				"<gloss lang='en'><text>leg</text></gloss>",
				"</sense>",
				"</entry>",
				"<entry dateCreated='2012-04-19T17:49:04Z' dateModified='2012-04-19T18:15:44Z' id='hand_c79278be-d698-4def-b104-c4303615683f' guid='c79278be-d698-4def-b104-c4303615683f'>",
				"<lexical-unit>",
				"<form lang='en'><text>hand</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='72c632c2-98ad-4f97-b130-2a32992254e3'>",
				"<gloss lang='en'><text>leg</text></gloss>",
				"</sense>",
				"</entry>",
				"</lift>"
			};
			// This lift data is a modified version of treeLiftData preserving 'arm', 'leg', and 'body' but adding 'leg' to the whole/part relation.
			string[] treeLiftData2 = {
				"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>",
				"<lift producer=\"SIL.FLEx 7.2.4.41003\" version=\"0.13\">",
				"  <header>",
				"    <fields>",
				"    </fields>",
				"    <ranges>",
				"<range id='lexical-relation' href='???'/>",
				"    </ranges>",
				"  </header>",
				"<entry dateCreated='2012-04-19T17:47:25Z' dateModified='2012-04-19T18:15:32Z' id='arm_835b9236-c6a4-48fe-b66e-673e40ff040d' guid='835b9236-c6a4-48fe-b66e-673e40ff040d'>",
				"<lexical-unit>",
				"<form lang='en'><text>arm</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='5ca96ad0-cb18-4ddc-be8e-3547fc87221f'>",
				"<gloss lang='en'><text>arm</text></gloss>",
				"<relation type='Whole' ref='52c632c2-98ad-4f97-b130-2a32992254e3'/>",
				"<relation type='Part' ref='72c632c2-98ad-4f97-b130-2a32992254e3'/>",
				"</sense>",
				"</entry>",
				"<entry dateCreated='2012-04-19T17:49:04Z' dateModified='2012-04-19T18:15:45Z' id='body_a79278be-d698-4def-b104-c4303615683f' guid='a79278be-d698-4def-b104-c4303615683f'>",
				"<lexical-unit>",
				"<form lang='en'><text>body</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='52c632c2-98ad-4f97-b130-2a32992254e3'>",
				"<gloss lang='en'><text>body</text></gloss>",
				"<relation type='Part' ref='5ca96ad0-cb18-4ddc-be8e-3547fc87221f'/>",
				"<relation type='Part' ref='62c632c2-98ad-4f97-b130-2a32992254e3'/>",
				"</sense>",
				"</entry>",
				"<entry dateCreated='2012-04-19T17:49:04Z' dateModified='2012-04-19T18:16:44Z' id='leg_b79278be-d698-4def-b104-c4303615683f' guid='b79278be-d698-4def-b104-c4303615683f'>",
				"<lexical-unit>",
				"<form lang='en'><text>leg</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='62c632c2-98ad-4f97-b130-2a32992254e3'>",
				"<gloss lang='en'><text>leg</text></gloss>",
				"<relation type='Whole' ref='52c632c2-98ad-4f97-b130-2a32992254e3'/>",
				"</sense>",
				"</entry>",
				"<entry dateCreated='2012-04-19T17:49:04Z' dateModified='2012-04-19T18:15:45Z' id='hand_c79278be-d698-4def-b104-c4303615683f' guid='c79278be-d698-4def-b104-c4303615683f'>",
				"<lexical-unit>",
				"<form lang='en'><text>hand</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='72c632c2-98ad-4f97-b130-2a32992254e3'>",
				"<gloss lang='en'><text>hand</text></gloss>",
				"<relation type='Whole' ref='5ca96ad0-cb18-4ddc-be8e-3547fc87221f'/>",
				"</sense>",
				"</entry>",
				"</lift>"
			};
			SetWritingSystems("fr");

			CreateNeededStyles();

			var senseRepo = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();

			var sOrigFile = CreateInputFile(treeLiftData);
			TryImport(sOrigFile, CreateInputRangesFile(_treeLiftRange), MergeStyle.MsKeepNew, 4);
			var bodySense = senseRepo.GetObject(new Guid("52c632c2-98ad-4f97-b130-2a32992254e3"));

			Assert.AreEqual(1, bodySense.LexSenseReferences.Count(), "Too many LexSenseReferences, the parts were split.");
			Assert.AreEqual(2, bodySense.LexSenseReferences.First().TargetsRS.Count, "Incorrect number of references, part relations not imported correctly.");

			var sNewFile = CreateInputFile(treeLiftData2);
			TryImport(sNewFile, CreateInputRangesFile(_treeLiftRange), MergeStyle.MsKeepOnlyNew, 4);
			var legSense = senseRepo.GetObject(new Guid("62c632c2-98ad-4f97-b130-2a32992254e3"));
			var armSense = senseRepo.GetObject(new Guid("5ca96ad0-cb18-4ddc-be8e-3547fc87221f"));
			//There should be 1 LexSenseReference for the Whole/Part relationship and each involved sense should share it.
			Assert.AreEqual(1, bodySense.LexSenseReferences.Count(), "Too many LexSenseReferences, the parts were split.");
			Assert.AreEqual(3, bodySense.LexSenseReferences.First().TargetsRS.Count, "Incorrect number of references, part relations not imported correctly.");
			Assert.AreEqual(1, legSense.LexSenseReferences.Count(), "Incorrect number of references in the leg sense.");
			Assert.AreEqual(3, legSense.LexSenseReferences.First().TargetsRS.Count, "Incorrect number of targets in the leg sense.");
			// body and leg both have only one LexReference
			Assert.AreEqual(bodySense.LexSenseReferences.First(), legSense.LexSenseReferences.First(), "LexReferences of Body and Leg should match.");
			// arm has two LexReferences and leg has one LexReference
			CollectionAssert.Contains(armSense.LexSenseReferences, legSense.LexSenseReferences.First(), "Arm LexReferences should include the single Leg LexReference");
		}

		[Test]
		public void TestImportDoesNotConfuseModifiedTreeRelations()
		{
			// This lift data contains 'a' 'b' and 'c' entries with 'a' being a whole of 2 parts 'b' and 'c' (whole/part relation)
			string[] treeLiftDataBase = {
				"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>",
				"<lift producer=\"SIL.FLEx 7.2.4.41003\" version=\"0.13\">",
				"  <header>",
				"    <fields>",
				"    </fields>",
				"  </header>",
				"<entry dateCreated='2012-04-19T17:47:25Z' dateModified='2012-04-19T18:15:32Z' id='arm_835b9236-c6a4-48fe-b66e-673e40ff040d' guid='835b9236-c6a4-48fe-b66e-673e40ff040d'>",
				"<lexical-unit>",
				"<form lang='en'><text>a</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='5ca96ad0-cb18-4ddc-be8e-3547fc87221f'>",
				"<gloss lang='en'><text>a</text></gloss>",
				"<relation type='Part' ref='52c632c2-98ad-4f97-b130-2a32992254e3'/>",
				"<relation type='Part' ref='62c632c2-98ad-4f97-b130-2a32992254e3'/>",
				"</sense>",
				"</entry>",
				"<entry dateCreated='2012-04-19T17:49:04Z' dateModified='2012-04-19T18:15:45Z' id='body_a79278be-d698-4def-b104-c4303615683f' guid='a79278be-d698-4def-b104-c4303615683f'>",
				"<lexical-unit>",
				"<form lang='en'><text>b</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='52c632c2-98ad-4f97-b130-2a32992254e3'>",
				"<gloss lang='en'><text>b</text></gloss>",
				"<relation type='Whole' ref='5ca96ad0-cb18-4ddc-be8e-3547fc87221f'/>",
				"</sense>",
				"</entry>",
				"<entry dateCreated='2012-04-19T17:49:04Z' dateModified='2012-04-19T18:16:44Z' id='leg_b79278be-d698-4def-b104-c4303615683f' guid='b79278be-d698-4def-b104-c4303615683f'>",
				"<lexical-unit>",
				"<form lang='en'><text>c</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='62c632c2-98ad-4f97-b130-2a32992254e3'>",
				"<gloss lang='en'><text>c</text></gloss>",
				"<relation type='Whole' ref='5ca96ad0-cb18-4ddc-be8e-3547fc87221f'/>",
				"</sense>",
				"</entry>",
				"</lift>"
			};
			// This lift data modifies treeLiftDataBase adding a 'd' entry and changing 'c' to have 'd' as a parent while 'b' still has 'a'
			string[] treeLiftDataReparented = {
				"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>",
				"<lift producer=\"SIL.FLEx 7.2.4.41003\" version=\"0.13\">",
				"  <header>",
				"    <fields>",
				"    </fields>",
				"  </header>",
				"<entry dateCreated='2012-04-19T17:47:25Z' dateModified='2013-04-19T18:15:32Z' id='a_835b9236-c6a4-48fe-b66e-673e40ff040d' guid='835b9236-c6a4-48fe-b66e-673e40ff040d'>",
				"<lexical-unit>",
				"<form lang='en'><text>a</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='5ca96ad0-cb18-4ddc-be8e-3547fc87221f'>",
				"<gloss lang='en'><text>a</text></gloss>",
				"<relation type='Part' ref='52c632c2-98ad-4f97-b130-2a32992254e3'/>",
				"</sense>",
				"</entry>",
				"<entry dateCreated='2012-04-19T17:49:04Z' dateModified='2013-04-19T18:15:45Z' id='b_a79278be-d698-4def-b104-c4303615683f' guid='a79278be-d698-4def-b104-c4303615683f'>",
				"<lexical-unit>",
				"<form lang='en'><text>b</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='52c632c2-98ad-4f97-b130-2a32992254e3'>",
				"<gloss lang='en'><text>b</text></gloss>",
				"<relation type='Whole' ref='5ca96ad0-cb18-4ddc-be8e-3547fc87221f'/>",
				"</sense>",
				"</entry>",
				"<entry dateCreated='2012-04-19T17:49:04Z' dateModified='2013-04-19T18:16:44Z' id='c_b79278be-d698-4def-b104-c4303615683f' guid='b79278be-d698-4def-b104-c4303615683f'>",
				"<lexical-unit>",
				"<form lang='en'><text>c</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='62c632c2-98ad-4f97-b130-2a32992254e3'>",
				"<gloss lang='en'><text>c</text></gloss>",
				"<relation type='Whole' ref='3b3632c2-98ad-4f97-b130-2a32992254e3'/>",
				"</sense>",
				"</entry>",
				"<entry dateCreated='2013-04-19T17:49:04Z' dateModified='2013-04-19T18:16:44Z' id='d_a87278be-d698-4def-b104-c4303615683f' guid='a87278be-d698-4def-b104-c4303615683f'>",
				"<lexical-unit>",
				"<form lang='en'><text>d</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='3b3632c2-98ad-4f97-b130-2a32992254e3'>",
				"<gloss lang='en'><text>d</text></gloss>",
				"<relation type='Part' ref='62c632c2-98ad-4f97-b130-2a32992254e3'/>",
				"</sense>",
				"</entry>",
				"</lift>"
			};
			SetWritingSystems("fr");

			CreateNeededStyles();

			var senseRepo = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();

			var sOrigFile = CreateInputFile(treeLiftDataBase);
			TryImport(sOrigFile, CreateInputRangesFile(_treeLiftRange), MergeStyle.MsKeepNew, 3);
			var aSense = senseRepo.GetObject(new Guid("5ca96ad0-cb18-4ddc-be8e-3547fc87221f"));
			var bSense = senseRepo.GetObject(new Guid("52c632c2-98ad-4f97-b130-2a32992254e3"));
			var cSense = senseRepo.GetObject(new Guid("62c632c2-98ad-4f97-b130-2a32992254e3"));

			Assert.AreEqual(1, aSense.LexSenseReferences.Count(), "Too many LexSenseReferences, the parts were split.");
			Assert.AreEqual(3, aSense.LexSenseReferences.First().TargetsRS.Count, "Incorrect number of references, part relations not imported correctly.");

			var sNewFile = CreateInputFile(treeLiftDataReparented);
			TryImport(sNewFile, CreateInputRangesFile(_treeLiftRange), MergeStyle.MsKeepOnlyNew, 4);
			var dSense = senseRepo.GetObject(new Guid("3b3632c2-98ad-4f97-b130-2a32992254e3"));
			//There should be 1 LexSenseReference for the Whole/Part relationship and each involved sense should share it.
			Assert.AreEqual(1, aSense.LexSenseReferences.Count(), "Too many LexSenseReferences, the parts were split.");
			Assert.AreEqual(2, aSense.LexSenseReferences.First().TargetsRS.Count, "Incorrect number of references, part relations not imported correctly.");
			Assert.AreEqual(1, cSense.LexSenseReferences.Count(), "Incorrect number of references in the c sense.");
			Assert.AreEqual(2, cSense.LexSenseReferences.First().TargetsRS.Count, "Incorrect number of targets in the c senses reference.");
			Assert.AreEqual(cSense.LexSenseReferences.First(), dSense.LexSenseReferences.First(), "c and d should be in the same relation");
			Assert.AreEqual(1, dSense.LexSenseReferences.Count(), "dSense picked up a phantom reference.");
		}

		[Test]
		public void TestImportCustomPairReferenceTypeWorks()
		{
			//ranges file with defining an Antonym and a Twin relation
			string[] newWithPairRange = {
				"<?xml version='1.0' encoding='UTF-8'?>",
				"<lift-ranges>",
				"<range id='lexical-relation'>",
				"<range-element id='Antonym' guid='b7862f14-ea5e-11de-8d47-0013722f8dec'>",
				"<label>",
				"<form lang='en'><text>Antonym</text></form>",
				"</label>",
				"<abbrev>",
				"<form lang='en'><text>ant</text></form>",
				"</abbrev>",
				"<description>",
				"<form lang='en'><text>Use this type for antonym relationships (e.g., fast, slow).</text></form>",
				"</description>",
				"<trait name='referenceType' value='1'/>",
				"</range-element>",
				"<range-element id='Twin' guid='1ec1f6c7-da89-4a4a-adc5-1f0df5b0c1f5'>",
				"<label>",
				"<form lang='en'><text>Twin</text></form>",
				"</label>",
				"<abbrev>",
				"<form lang='en'><text>twn</text></form>",
				"</abbrev>",
				"<field type='reverse-label'>",
				"<form lang='en'><text>Twain</text></form>",
				"</field>",
				"<field type='reverse-abbrev'>",
				"<form lang='en'><text>tin</text></form>",
				"</field>",
				"<trait name='referenceType' value='2'/>",
				"</range-element>",
				"</range>",
				"</lift-ranges>"
			};
			// Defines a lift file with two entries 'Bother' and 'me'.
			string[] origNoPair = {
				"<?xml version='1.0' encoding='UTF-8' ?>",
				"<lift producer='SIL.FLEx 8.0.5.41540' version='0.13'>",
				"<header>",
				"<ranges/>",
				"<fields/>",
				"</header>",
				"<entry dateCreated='2013-09-24T18:57:02Z' dateModified='2013-09-24T19:01:10Z' id='Bug_84338803-89e0-4d74-b175-fcbb2eb23ea5' guid='84338803-89e0-4d74-b175-fcbb2eb23ea5'>",
				"<lexical-unit>",
				"<form lang='fr'><text>Bother</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='c2b4fe44-a3d9-4a42-a87c-8e174593fb30'>",
				"</sense>",
				"</entry>",
				"<entry dateCreated='2013-09-24T18:57:11Z' dateModified='2013-09-24T19:01:10Z' id='me_1bbaccee-d640-4cae-9f80-0563225b93ca' guid='1bbaccee-d640-4cae-9f80-0563225b93ca'>",
				"<lexical-unit>",
				"<form lang='fr'><text>me</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='de2fcb48-319a-48cf-bfea-0f25b9f38b31'>",
				"</sense>",
				"</entry>",
				"</lift>"
			};
			SetWritingSystems("fr");

			CreateNeededStyles();

			var entryRepo = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var senseRepo = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();

			var sOrigFile = CreateInputFile(origNoPair);
			TryImport(sOrigFile, null, MergeStyle.MsKeepOnlyNew, 2);
			var aSense = senseRepo.GetObject(new Guid("c2b4fe44-a3d9-4a42-a87c-8e174593fb30"));
			var bSense = senseRepo.GetObject(new Guid("de2fcb48-319a-48cf-bfea-0f25b9f38b31"));
			Assert.AreEqual(0, aSense.LexSenseReferences.Count(), "Incorrect number of component references.");
			Assert.AreEqual(0, bSense.LexSenseReferences.Count(), "Incorrect number of component references.");

			var sNewFile = CreateInputFile(_newWithPair);
			TryImport(sNewFile, CreateInputRangesFile(newWithPairRange), MergeStyle.MsKeepOnlyNew, 2);
			Assert.AreEqual(1, aSense.LexSenseReferences.Count(), "Incorrect number of component references.");
			Assert.AreEqual(1, bSense.LexSenseReferences.Count(), "Incorrect number of component references.");
			Assert.That(aSense.LexSenseReferences.First().TargetsRS.Contains(bSense), "The Twin/Twain relationship failed to contain 'Bother' and 'me'");
			Assert.That(bSense.LexSenseReferences.First().TargetsRS.Contains(aSense), "The Twin/Twain relationship failed to contain 'Bother' and 'me'");
			Assert.AreEqual(aSense.LexSenseReferences.First(), bSense.LexSenseReferences.First(), "aSense and bSense should share the same LexSenseReference.");
			Assert.That(aSense.LexSenseReferences.First().TargetsRS[0].Equals(bSense), "Twin item should come before Twain");
			Assert.That(bSense.LexSenseReferences.First().TargetsRS[0].Equals(bSense), "Twin item should come before Twain");
		}

		[Test]
		public void TestImportCustomRangesIgnoresNonCustomRanges()
		{
			//ranges file with defining an Antonym as a default relation (no guid) and a Twin relation
			string[] rangeWithOneCustomAndOneDefault = {
				"<?xml version='1.0' encoding='UTF-8'?>",
				"<lift-ranges>",
				"<range id='lexical-relation'>",
				"<range-element id='Antonym'>",
				"<label>",
				"<form lang='en'><text>Antonym</text></form>",
				"</label>",
				"<abbrev>",
				"<form lang='en'><text>ant</text></form>",
				"</abbrev>",
				"<description>",
				"<form lang='en'><text>Use this type for antonym relationships (e.g., fast, slow).</text></form>",
				"</description>",
				"<trait name='referenceType' value='1'/>",
				"</range-element>",
				"<range-element id='Twin' guid='1ec1f6c7-da89-4a4a-adc5-1f0df5b0c1f5'>",
				"<label>",
				"<form lang='en'><text>Twin</text></form>",
				"</label>",
				"<abbrev>",
				"<form lang='en'><text>twn</text></form>",
				"</abbrev>",
				"<field type='reverse-label'>",
				"<form lang='en'><text>Twain</text></form>",
				"</field>",
				"<field type='reverse-abbrev'>",
				"<form lang='en'><text>tin</text></form>",
				"</field>",
				"<trait name='referenceType' value='2'/>",
				"</range-element>",
				"</range>",
				"</lift-ranges>"
			};
			SetWritingSystems("fr");

			CreateNeededStyles();

			var entryRepo = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var senseRepo = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			var typeRepo = Cache.ServiceLocator.GetInstance<ILexRefTypeRepository>();

			var sOrigFile = CreateInputFile(_newWithPair);
			Assert.AreEqual(0, typeRepo.Count, "Too many types exist before import, bootstrapping has changed?");
			TryImport(sOrigFile, CreateInputRangesFile(rangeWithOneCustomAndOneDefault), MergeStyle.MsKeepOnlyNew, 2);
			var aSense = senseRepo.GetObject(new Guid("c2b4fe44-a3d9-4a42-a87c-8e174593fb30"));
			var bSense = senseRepo.GetObject(new Guid("de2fcb48-319a-48cf-bfea-0f25b9f38b31"));
			Assert.AreEqual(1, aSense.LexSenseReferences.Count(), "Incorrect number of component references.");
			Assert.AreEqual(1, bSense.LexSenseReferences.Count(), "Incorrect number of component references.");
			Assert.AreEqual(1, typeRepo.Count, "Too many types created during import.");
		}

		[Test]
		public void TestImportCustomReferenceTypeWithMultipleWsWorks()
		{
			//ranges file with defining a custom queue SenseSequence type, which is called enqueue in french
			string[] newWithRelationRange = {
				"<?xml version='1.0' encoding='UTF-8'?>",
				"<lift-ranges>",
				"<range id='lexical-relation'>",
				"<range-element id='Queue' guid='b7862f14-ea5e-11de-8d47-0013722f8dec'>",
				"<label>",
				"<form lang='en'><text>queue</text></form>",
				"<form lang='fr'><text>enqueue</text></form>",
				"</label>",
				"<abbrev>",
				"<form lang='en'><text>q</text></form>",
				"<form lang='fr'><text>eq</text></form>",
				"</abbrev>",
				"<description>",
				"<form lang='en'><text>Get in line.</text></form>",
				"<form lang='fr'><text>Git le line.</text></form>",
				"</description>",
				"<trait name='referenceType' value='" + (int)LexRefTypeTags.MappingTypes.kmtSenseSequence + "'/>",
				"</range-element>",
				"</range>",
				"</lift-ranges>"
			};
			// modified LIFT with same entries 'Bother' and 'me' related using relation type queue (alternatively named enqueue)
			string[] newWithRelation = {
				"<?xml version='1.0' encoding='UTF-8' ?>",
				"<lift producer='SIL.FLEx 8.0.5.41540' version='0.13'>",
				"<header>",
				"<ranges>",
				"<range id='lexical-relation' href='???'/>",
				"</ranges>",
				"<fields/>",
				"</header>",
				"<entry dateCreated='2013-09-24T18:57:02Z' dateModified='2013-09-24T19:00:14Z' id='Bug_84338803-89e0-4d74-b175-fcbb2eb23ea5' guid='84338803-89e0-4d74-b175-fcbb2eb23ea5'>",
				"<lexical-unit>",
				"<form lang='fr'><text>Bother</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='c2b4fe44-a3d9-4a42-a87c-8e174593fb30'>",
				"<relation type='queue' ref='de2fcb48-319a-48cf-bfea-0f25b9f38b31' order='1'/>",
				"<relation type='queue' ref='c2b4fe44-a3d9-4a42-a87c-8e174593fb30' order='2'/>",
				"</sense>",
				"</entry>",
				"<entry dateCreated='2013-09-24T18:57:11Z' dateModified='2013-09-24T19:00:14Z' id='me_1bbaccee-d640-4cae-9f80-0563225b93ca' guid='1bbaccee-d640-4cae-9f80-0563225b93ca'>",
				"<lexical-unit>",
				"<form lang='fr'><text>me</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='de2fcb48-319a-48cf-bfea-0f25b9f38b31'>",
				"<relation type='enqueue' ref='de2fcb48-319a-48cf-bfea-0f25b9f38b31' order='1'/>",
				"<relation type='enqueue' ref='c2b4fe44-a3d9-4a42-a87c-8e174593fb30' order='2'/>",
				"</sense>",
				"</entry>",
				"</lift>"
			};
			// Defines a lift file with two entries 'Bother' and 'me'.
			string[] origNoRelation = {
				"<?xml version='1.0' encoding='UTF-8' ?>",
				"<lift producer='SIL.FLEx 8.0.5.41540' version='0.13'>",
				"<header>",
				"<ranges/>",
				"<fields/>",
				"</header>",
				"<entry dateCreated='2013-09-24T18:57:02Z' dateModified='2013-09-24T19:01:10Z' id='Bug_84338803-89e0-4d74-b175-fcbb2eb23ea5' guid='84338803-89e0-4d74-b175-fcbb2eb23ea5'>",
				"<lexical-unit>",
				"<form lang='fr'><text>Bother</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='c2b4fe44-a3d9-4a42-a87c-8e174593fb30'>",
				"</sense>",
				"</entry>",
				"<entry dateCreated='2013-09-24T18:57:11Z' dateModified='2013-09-24T19:01:10Z' id='me_1bbaccee-d640-4cae-9f80-0563225b93ca' guid='1bbaccee-d640-4cae-9f80-0563225b93ca'>",
				"<lexical-unit>",
				"<form lang='fr'><text>me</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='de2fcb48-319a-48cf-bfea-0f25b9f38b31'>",
				"</sense>",
				"</entry>",
				"</lift>"
			};
			Cache.LangProject.AnalysisWss = "en fr";
			Cache.LangProject.CurAnalysisWss = "en";
			Cache.LangProject.VernWss = "sen arb";
			Cache.LangProject.AddToCurrentVernacularWritingSystems(Cache.WritingSystemFactory.get_Engine("sen") as CoreWritingSystemDefinition);
			Cache.LangProject.AddToCurrentVernacularWritingSystems(Cache.WritingSystemFactory.get_Engine("arb") as CoreWritingSystemDefinition);
			Cache.LangProject.CurVernWss = "sen";

			CreateNeededStyles();

			Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var senseRepo = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			var refTypeRepo = Cache.ServiceLocator.GetInstance<ILexRefTypeRepository>();

			var sOrigFile = CreateInputFile(origNoRelation);
			TryImport(sOrigFile, null, MergeStyle.MsKeepOnlyNew, 2);
			var aSense = senseRepo.GetObject(new Guid("c2b4fe44-a3d9-4a42-a87c-8e174593fb30"));
			var bSense = senseRepo.GetObject(new Guid("de2fcb48-319a-48cf-bfea-0f25b9f38b31"));
			Assert.AreEqual(0, aSense.LexSenseReferences.Count(), "Incorrect number of component references.");
			Assert.AreEqual(0, bSense.LexSenseReferences.Count(), "Incorrect number of component references.");

			var sNewFile = CreateInputFile(newWithRelation);
			TryImport(sNewFile, CreateInputRangesFile(newWithRelationRange), MergeStyle.MsKeepOnlyNew, 2);
			Assert.AreEqual(1, aSense.LexSenseReferences.Count(), "Incorrect number of component references.");
			Assert.AreEqual(1, bSense.LexSenseReferences.Count(), "Incorrect number of component references.");
			var queueType = refTypeRepo.AllInstances().FirstOrDefault(refType => refType.Name.BestAnalysisAlternative.Text.Equals("queue"));
			Assert.That(queueType != null && queueType.MembersOC.Contains(bSense.LexSenseReferences.First()), "Queue incorrectly imported.");
			Assert.That(queueType.MappingType == (int)LexRefTypeTags.MappingTypes.kmtSenseSequence, "Queue imported with wrong type.");
			Assert.That(queueType.Description.StringCount == 2, "One writing system didn't import");
			Assert.That(queueType.Name.StringCount == 2, "One writing system didn't import");
			Assert.That(queueType.Abbreviation.StringCount == 2, "One writing system didn't import");
			Assert.That(queueType.Name.get_String(Cache.WritingSystemFactory.GetWsFromStr("fr")).Text.Equals("enqueue"));
			Assert.That(queueType.Abbreviation.get_String(Cache.WritingSystemFactory.GetWsFromStr("fr")).Text.Equals("eq"));
			Assert.That(queueType.Description.get_String(Cache.WritingSystemFactory.GetWsFromStr("en")).Text.Equals("Get in line."));
		}

		[Test]
		public void TestReplaceSynonymWithAntonymWorks()
		{
			string[] nextAntReplaceSyn = {
				"<?xml version='1.0' encoding='utf-8'?>",
				"<lift producer='SIL.FLEx 8.0.4.41523' version='0.13'>",
				" <header>",
				"  <ranges/>",
				"  <fields/>",
				" </header>",
				" <entry  dateCreated='2013-09-20T16:25:38Z'  dateModified='2013-09-20T16:55:53Z'  guid='497ee5d1-7459-48cc-b142-48605c77be80'  id='c_497ee5d1-7459-48cc-b142-48605c77be80'>",
				"  <lexical-unit>",
				"   <form    lang='fr'>",
				"    <text>c</text>",
				"   </form>",
				"  </lexical-unit>",
				"  <sense   id='a2096aa3-6076-47c0-b243-e50d00afaeb5' />",
				"  <trait   name='morph-type'   value='stem' />",
				" </entry>",
				" <entry  dateCreated='2013-09-20T16:25:28Z'  dateModified='2013-09-20T16:56:32Z'  guid='5709b6df-ab08-4f62-bd2e-4d3685000c68'  id='b_5709b6df-ab08-4f62-bd2e-4d3685000c68'>",
				"  <lexical-unit>",
				"   <form    lang='fr'>",
				"    <text>b</text>",
				"   </form>",
				"  </lexical-unit>",
				"  <sense   id='70a6973b-787e-4ddc-942f-3a2b2d0c6863'>",
				"   <relation    ref='91eb7dc2-4057-4e7c-88c3-a81536a38c3e'    type='Antonym' />",
				"  </sense>",
				"  <trait   name='morph-type'   value='stem' />",
				" </entry>",
				" <entry  dateCreated='2013-09-20T16:25:23Z'  dateModified='2013-09-20T16:56:32Z'  guid='ac828ef4-9a18-4802-b095-11cca00947db'  id='a_ac828ef4-9a18-4802-b095-11cca00947db'>",
				"  <lexical-unit>",
				"   <form    lang='fr'>",
				"    <text>a</text>",
				"   </form>",
				"  </lexical-unit>",
				"  <sense   id='91eb7dc2-4057-4e7c-88c3-a81536a38c3e'>",
				"   <relation    ref='70a6973b-787e-4ddc-942f-3a2b2d0c6863'    type='Antonym' />",
				"  </sense>",
				"  <trait   name='morph-type'   value='stem' />",
				" </entry>",
				"</lift>"
			};
			// lift data with the entries a, b, and c where a and c are in a Synonym relationship and b is in none.
			string[] origAntReplaceSyn = {
				"<?xml version='1.0' encoding='UTF-8' ?>",
				"<!-- See http://code.google.com/p/lift-standard for more information on the format used here. -->",
				"<lift producer='SIL.FLEx 8.0.5.41540' version='0.13'>",
				"<header>",
				"<ranges/>",
				"<fields/>",
				"</header>",
				"<entry dateCreated='2013-09-20T16:25:38Z' dateModified='2013-09-20T16:26:04Z' id='c_497ee5d1-7459-48cc-b142-48605c77be80' guid='497ee5d1-7459-48cc-b142-48605c77be80'>"
				,
				"<lexical-unit>",
				"<form lang='fr'><text>c</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='a2096aa3-6076-47c0-b243-e50d00afaeb5'>",
				"<relation type='Synonyms' ref='91eb7dc2-4057-4e7c-88c3-a81536a38c3e'/>",
				"</sense>",
				"</entry>",
				"<entry dateCreated='2013-09-20T16:25:28Z' dateModified='2013-09-20T16:34:26Z' id='b_5709b6df-ab08-4f62-bd2e-4d3685000c68' guid='5709b6df-ab08-4f62-bd2e-4d3685000c68'>"
				,
				"<lexical-unit>",
				"<form lang='fr'><text>b</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='70a6973b-787e-4ddc-942f-3a2b2d0c6863'>",
				"</sense>",
				"</entry>",
				"<entry dateCreated='2013-09-20T16:25:23Z' dateModified='2013-09-20T16:25:56Z' id='a_ac828ef4-9a18-4802-b095-11cca00947db' guid='ac828ef4-9a18-4802-b095-11cca00947db'>"
				,
				"<lexical-unit>",
				"<form lang='fr'><text>a</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='91eb7dc2-4057-4e7c-88c3-a81536a38c3e'>",
				"<relation type='Synonyms' ref='a2096aa3-6076-47c0-b243-e50d00afaeb5'/>",
				"</sense>",
				"</entry>",
				"</lift>"
			};
			SetWritingSystems("fr");

			CreateNeededStyles();

			var senseRepo = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			var refTypeRepo = Cache.ServiceLocator.GetInstance<ILexRefTypeRepository>();

			var sOrigFile = CreateInputFile(origAntReplaceSyn);
			TryImport(sOrigFile, null, MergeStyle.MsKeepOnlyNew, 3);
			var aSense = senseRepo.GetObject(new Guid("a2096aa3-6076-47c0-b243-e50d00afaeb5"));
			var bSense = senseRepo.GetObject(new Guid("70a6973b-787e-4ddc-942f-3a2b2d0c6863"));
			var cSense = senseRepo.GetObject(new Guid("91eb7dc2-4057-4e7c-88c3-a81536a38c3e"));
			Assert.AreEqual(1, aSense.LexSenseReferences.Count(), "Incorrect number of component references.");
			Assert.AreEqual(0, bSense.LexSenseReferences.Count(), "Incorrect number of component references.");
			Assert.AreEqual(1, cSense.LexSenseReferences.Count(), "Incorrect number of component references.");
			var synType = refTypeRepo.AllInstances().FirstOrDefault(refType => refType.Name.BestAnalysisAlternative.Text.Equals("Synonyms"));
			Assert.That(synType != null && synType.MembersOC.Contains(aSense.LexSenseReferences.First()), "Synonym incorrectly imported.");

			var sNewFile = CreateInputFile(nextAntReplaceSyn);
			TryImport(sNewFile, null, MergeStyle.MsKeepOnlyNew, 3);
			Assert.AreEqual(0, aSense.LexSenseReferences.Count(), "Incorrect number of component references.");
			Assert.AreEqual(1, bSense.LexSenseReferences.Count(), "Incorrect number of component references.");
			Assert.AreEqual(1, cSense.LexSenseReferences.Count(), "Incorrect number of component references.");
			var antType = refTypeRepo.AllInstances().FirstOrDefault(refType => refType.Name.BestAnalysisAlternative.Text.Equals("Antonym"));
			Assert.That(antType != null && antType.MembersOC.Contains(bSense.LexSenseReferences.First()), "Antonym incorrectly imported.");
		}

		[Test]
		public void TestDeleteRelationRefOnVariantComplexFormWorks()
		{
			string[] twoEntryWithVariantRefRemovedLift = {
				"<?xml version='1.0' encoding='utf-8'?>",
				"<lift producer='SIL.FLEx 8.0.4.41520' version='0.13'>",
				"	<header></header>",
				"	<entry dateCreated='2013-09-20T19:34:26Z' dateModified='2013-09-20T19:35:26Z' guid='40a9574d-2d13-4d30-9eab-9a6d84bf29f8' id='e_40a9574d-2d13-4d30-9eab-9a6d84bf29f8'>",
				"		<lexical-unit>",
				"			<form lang='fr'>",
				"				<text>e</text>",
				"			</form>",
				"		</lexical-unit>",
				"		<relation order='0' ref='' type='_component-lexeme'>",
				"			<trait name='variant-type' value='' />",
				"		</relation>",
				"		<trait name='morph-type' value='stem' />",
				"	</entry>",
				"	<entry dateCreated='2013-09-20T16:25:23Z' dateModified='2013-09-20T19:35:26Z' guid='ac828ef4-9a18-4802-b095-11cca00947db' id='a_ac828ef4-9a18-4802-b095-11cca00947db'>",
				"		<lexical-unit>",
				"			<form lang='fr'>",
				"				<text>a</text>",
				"			</form>",
				"		</lexical-unit>",
				"		<sense id='91eb7dc2-4057-4e7c-88c3-a81536a38c3e' />",
				"		<trait name='morph-type' value='stem' />",
				"	</entry>",
				"</lift>"
			};
			SetWritingSystems("fr");

			CreateNeededStyles();

			var entryRepo = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();

			var sOrigFile = CreateInputFile(_twoEntryWithVariantComplexFormLift);
			TryImport(sOrigFile, null, MergeStyle.MsKeepOnlyNew, 2);
			var eEntry = entryRepo.GetObject(new Guid("40a9574d-2d13-4d30-9eab-9a6d84bf29f8"));
			var aEntry = entryRepo.GetObject(new Guid("ac828ef4-9a18-4802-b095-11cca00947db"));
			Assert.AreEqual(1, eEntry.VariantEntryRefs.Count(), "No VariantEntryRefs found when expected, import of lift data during test setup failed.");
			Assert.AreEqual(1, aEntry.VariantFormEntries.Count(), "Variant form Entry not found when expected, import of lift data during test setup failed.");

			var sNewFile = CreateInputFile(twoEntryWithVariantRefRemovedLift);
			TryImport(sNewFile, null, MergeStyle.MsKeepOnlyNew, 2);
			// An empty VariantEntryRef can not be unambiguously identified, so a new empty one is
			// created. This results in stable lift (doesn't change on round trip), but the fwdata
			// will change on round trip without real changes. This is not what we prefer, but think
			// it is OK for now. Nov 2013
			Assert.AreEqual(1, eEntry.VariantEntryRefs.Count(), "VariantEntryRef should remain after lift import.");
			Assert.AreEqual(0, aEntry.VariantFormEntries.Count(), "VariantForm Entry was not deleted during lift import."); // The reference was removed so the Entries collection should be empty
		}

		[Test]
		public void TestDeleteVariantComplexFormWorks()
		{
			string[] twoEntryWithVariantRemovedLift = {
				"<?xml version='1.0' encoding='utf-8'?>",
				"<lift producer='SIL.FLEx 8.0.4.41520' version='0.13'>",
				"	<header></header>",
				"	<entry dateCreated='2013-09-20T19:34:26Z' dateModified='2013-09-20T19:35:26Z' guid='40a9574d-2d13-4d30-9eab-9a6d84bf29f8' id='e_40a9574d-2d13-4d30-9eab-9a6d84bf29f8'>",
				"		<lexical-unit>",
				"			<form lang='fr'>",
				"				<text>e</text>",
				"			</form>",
				"		</lexical-unit>",
				"		<trait name='morph-type' value='stem' />",
				"	</entry>",
				"	<entry dateCreated='2013-09-20T16:25:23Z' dateModified='2013-09-20T19:35:26Z' guid='ac828ef4-9a18-4802-b095-11cca00947db' id='a_ac828ef4-9a18-4802-b095-11cca00947db'>",
				"		<lexical-unit>",
				"			<form lang='fr'>",
				"				<text>a</text>",
				"			</form>",
				"		</lexical-unit>",
				"		<sense id='91eb7dc2-4057-4e7c-88c3-a81536a38c3e' />",
				"		<trait name='morph-type' value='stem' />",
				"	</entry>",
				"</lift>"
			};
			SetWritingSystems("fr");

			CreateNeededStyles();

			var entryRepo = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();

			var sOrigFile = CreateInputFile(_twoEntryWithVariantComplexFormLift);
			TryImport(sOrigFile, null, MergeStyle.MsKeepOnlyNew, 2);
			var eEntry = entryRepo.GetObject(new Guid("40a9574d-2d13-4d30-9eab-9a6d84bf29f8"));
			var aEntry = entryRepo.GetObject(new Guid("ac828ef4-9a18-4802-b095-11cca00947db"));
			Assert.AreEqual(1, eEntry.VariantEntryRefs.Count(), "No VariantEntryRefs found when expected, import of lift data during test setup failed.");
			Assert.AreEqual(1, aEntry.VariantFormEntries.Count(), "Variant form Entry not found when expected, import of lift data during test setup failed.");

			var sNewFile = CreateInputFile(twoEntryWithVariantRemovedLift);
			TryImport(sNewFile, null, MergeStyle.MsKeepOnlyNew, 2);
			Assert.AreEqual(0, eEntry.VariantEntryRefs.Count(), "VariantEntryRef was not deleted during lift import.");
			Assert.AreEqual(0, aEntry.VariantFormEntries.Count(), "VariantForm Entry was not deleted during lift import.");
		}

		[Test]
		public void TestVariantComplexFormNotDeletedWhenUnTouchedWorks()
		{
			string[] twoEntryWithVariantComplexFormAndNewItemLift = {
				"<?xml version='1.0' encoding='utf-8'?>",
				"<lift producer='SIL.FLEx 8.0.4.41520' version='0.13'>",
				"	<header></header>",
				"	<entry dateCreated='2013-09-20T19:34:26Z' dateModified='2013-09-21T19:34:26Z' guid='40a9574d-2d13-4d30-9eab-9a6d84bf29f8' id='e_40a9574d-2d13-4d30-9eab-9a6d84bf29f8'>",
				"		<lexical-unit>",
				"			<form lang='fr'>",
				"				<text>e</text>",
				"			</form>",
				"		</lexical-unit>",
				"		<relation order='0' ref='a_ac828ef4-9a18-4802-b095-11cca00947db' type='_component-lexeme'>",
				"			<trait name='variant-type' value='' />",
				"		</relation>",
				"		<trait name='morph-type' value='stem' />",
				"	</entry>",
				"	<entry dateCreated='2013-09-20T16:25:23Z' dateModified='2013-09-21T19:01:59Z' guid='ac828ef4-9a18-4802-b095-11cca00947db' id='a_ac828ef4-9a18-4802-b095-11cca00947db'>",
				"		<lexical-unit>",
				"			<form lang='fr'>",
				"				<text>a</text>",
				"			</form>",
				"		</lexical-unit>",
				"		<sense id='91eb7dc2-4057-4e7c-88c3-a81536a38c3e' />",
				"		<trait name='morph-type' value='stem' />",
				"	</entry>",
				"	<entry dateCreated='2013-09-21T16:25:23Z' dateModified='2013-09-21T19:01:59Z' guid='ad928ef4-9a18-4802-b095-11cca00947db' id='a_ad928ef4-9a18-4802-b095-11cca00947db'>",
				"		<lexical-unit>",
				"			<form lang='fr'>",
				"				<text>new</text>",
				"			</form>",
				"		</lexical-unit>",
				"		<trait name='morph-type' value='stem' />",
				"	</entry>",
				"</lift>"
			};
			SetWritingSystems("fr");

			CreateNeededStyles();

			var entryRepo = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();

			var sOrigFile = CreateInputFile(_twoEntryWithVariantComplexFormLift);
			TryImport(sOrigFile, null, MergeStyle.MsKeepOnlyNew, 2);
			var eEntry = entryRepo.GetObject(new Guid("40a9574d-2d13-4d30-9eab-9a6d84bf29f8"));
			var aEntry = entryRepo.GetObject(new Guid("ac828ef4-9a18-4802-b095-11cca00947db"));
			Assert.AreEqual(1, eEntry.VariantEntryRefs.Count(), "No VariantEntryRefs found when expected, import of lift data during test setup failed.");
			Assert.AreEqual(1, aEntry.VariantFormEntries.Count(), "Variant form Entry not found when expected, import of lift data during test setup failed.");

			var sNewFile = CreateInputFile(twoEntryWithVariantComplexFormAndNewItemLift);
			TryImport(sNewFile, null, MergeStyle.MsKeepOnlyNew, 3);
			Assert.AreEqual(1, eEntry.VariantEntryRefs.Count(), "VariantEntryRef mistakenly deleted during lift import.");
			Assert.AreEqual(1, aEntry.VariantFormEntries.Count(), "VariantForm Entry was mistakenly deleted during lift import.");
		}

		[Test]
		public void TestDeleteDerivativeComplexFormWorks()
		{
			string[] twoEntryWithDerivativeComplexFormRemovedLift = {
				"<?xml version='1.0' encoding='utf-8'?>",
				"<lift producer='SIL.FLEx 8.0.4.41520' version='0.13'>",
				"	<header></header>",
				"	<entry dateCreated='2013-09-20T19:34:26Z' dateModified='2013-09-20T19:35:26Z' guid='40a9574d-2d13-4d30-9eab-9a6d84bf29f8' id='e_40a9574d-2d13-4d30-9eab-9a6d84bf29f8'>",
				"		<lexical-unit>",
				"			<form lang='fr'>",
				"				<text>e</text>",
				"			</form>",
				"		</lexical-unit>",
				"		<relation order='0' ref='' type='_component-lexeme'>",
				"			<trait name='variant-type' value='' />",
				"		</relation>",
				"		<trait name='morph-type' value='stem' />",
				"	</entry>",
				"	<entry dateCreated='2013-09-20T16:25:23Z' dateModified='2013-09-20T19:35:26Z' guid='ac828ef4-9a18-4802-b095-11cca00947db' id='a_ac828ef4-9a18-4802-b095-11cca00947db'>",
				"		<lexical-unit>",
				"			<form lang='fr'>",
				"				<text>a</text>",
				"			</form>",
				"		</lexical-unit>",
				"		<sense id='91eb7dc2-4057-4e7c-88c3-a81536a38c3e' />",
				"		<trait name='morph-type' value='stem' />",
				"	</entry>",
				"</lift>"
			};
			string[] twoEntryWithDerivativeComplexFormLift = {
				"<?xml version='1.0' encoding='utf-8'?>",
				"<lift producer='SIL.FLEx 8.0.4.41520' version='0.13'>",
				"	<header></header>",
				"	<entry dateCreated='2013-09-20T19:34:26Z' dateModified='2013-09-20T19:34:26Z' guid='40a9574d-2d13-4d30-9eab-9a6d84bf29f8' id='e_40a9574d-2d13-4d30-9eab-9a6d84bf29f8'>",
				"		<lexical-unit>",
				"			<form lang='fr'>",
				"				<text>e</text>",
				"			</form>",
				"		</lexical-unit>",
				"		<relation order='0' ref='a_ac828ef4-9a18-4802-b095-11cca00947db' type='_component-lexeme'>",
				"			<trait name='is-primary' value='true' />",
				"			<trait name='complex-form-type' value='Derivative' />",
				"		</relation>",
				"		<trait name='morph-type' value='stem' />",
				"	</entry>",
				"	<entry dateCreated='2013-09-20T16:25:23Z' dateModified='2013-09-20T19:01:59Z' guid='ac828ef4-9a18-4802-b095-11cca00947db' id='a_ac828ef4-9a18-4802-b095-11cca00947db'>",
				"		<lexical-unit>",
				"			<form lang='fr'>",
				"				<text>a</text>",
				"			</form>",
				"		</lexical-unit>",
				"		<sense id='91eb7dc2-4057-4e7c-88c3-a81536a38c3e' />",
				"		<trait name='morph-type' value='stem' />",
				"	</entry>",
				"</lift>"
			};
			SetWritingSystems("fr");

			CreateNeededStyles();

			var entryRepo = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();

			var sOrigFile = CreateInputFile(twoEntryWithDerivativeComplexFormLift);
			TryImport(sOrigFile, null, MergeStyle.MsKeepOnlyNew, 2);
			var eEntry = entryRepo.GetObject(new Guid("40a9574d-2d13-4d30-9eab-9a6d84bf29f8"));
			var aEntry = entryRepo.GetObject(new Guid("ac828ef4-9a18-4802-b095-11cca00947db"));
			Assert.AreEqual(1, Enumerable.Count(eEntry.ComplexFormEntryRefs), "No ComplexFormEntryRefs found when expected, import of lift data during test setup failed.");
			Assert.AreEqual(1, Enumerable.Count(aEntry.ComplexFormEntries), "No ComplexEntries found when expected, import of lift data during test setup failed.");

			var sNewFile = CreateInputFile(twoEntryWithDerivativeComplexFormRemovedLift);
			TryImport(sNewFile, null, MergeStyle.MsKeepOnlyNew, 2);
			Assert.AreEqual(0, eEntry.ComplexFormEntryRefs.Count(), "ComplexFormEntryRefs was not deleted during lift import.");
			Assert.AreEqual(0, aEntry.ComplexFormEntries.Count(), "ComplexFormEntry was not deleted during lift import.");
			Assert.AreEqual(1, eEntry.VariantEntryRefs.Count(), "An empty VariantEntryRef should have resulted from the import");
			Assert.AreEqual(0, aEntry.VariantFormEntries.Count(), "An empty VariantEntryRef should have resulted from the import");
		}

		[Test]
		public void TestImportLexRefType_NonAsciiCharactersDoNotCauseDuplication()
		{
			const char unNormalizedOmega = '\u2126';
			const char normalizedOmega = '\u03A9';
			//ranges file with defining a custom EntryCollection type using a non-normalized unicode character in the name
			var liftRangeWithNonAsciiRelation = new[]
			{
				"<?xml version='1.0' encoding='UTF-8'?>",
				"<lift-ranges>",
				"<range id='lexical-relation'>",
				"<range-element id='Test" + unNormalizedOmega + "' guid='b7862f14-ea5e-11de-8d47-0013722f8dec'>",
				"<label>",
				"<form lang='en'><text>One</text></form>",
				"<form lang='fr'><text>deux</text></form>",
				"</label>",
				"<abbrev>",
				"<form lang='en'><text>o.</text></form>",
				"<form lang='fr'><text>d.</text></form>",
				"</abbrev>",
				"<trait name='referenceType' value='" + (int)LexRefTypeTags.MappingTypes.kmtEntryCollection + "'/>",
				"</range-element>",
				"</range>",
				"</lift-ranges>"
			};
			var liftWithSenseUsingNonAsciiRelation = new[]
			{
				"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>",
				"<lift producer=\"SIL.FLEx 8.1.0\" version=\"0.13\">",
				"  <header>",
				"    <fields>",
				"    </fields>",
				"  </header>",
				"<entry id='cold_97b8a20d-9989-430d-8a20-2f95592d60cb' guid='97b8a20d-9989-430d-8a20-2f95592d60cb'>",
				"<lexical-unit>",
				"<form lang='en'><text>cold</text></form>",
				"</lexical-unit>",
				"<trait  name='morph-type' value='stem'/>",
				"<sense id='57f884c0-0df2-43bf-8ba7-c70b2a208cf1'>",
				"<gloss lang='en'><text>cold</text></gloss>",
				"<relation type='Test" + unNormalizedOmega + "' ref='97b8a20d-9989-430d-8a20-2f95592d60cb' order='1'/>",
				"</sense>",
				"</entry>",
				"</lift>"
			};

			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			var refTypeFactory = Cache.ServiceLocator.GetInstance<ILexRefTypeFactory>();
			var testType = refTypeFactory.Create(new Guid("b7862f14-ea5e-11de-8d47-0013722f8dec"), null);
			testType.Name.set_String(wsEn, TsStringUtils.MakeString("Test" + normalizedOmega, wsEn));
			testType.Abbreviation.set_String(wsEn, TsStringUtils.MakeString("test", wsEn));
			Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Add(testType);
			var refTypeCountBeforeImport = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Count;
			var liftFile = CreateInputFile(liftWithSenseUsingNonAsciiRelation);
			var rangeFile = CreateInputRangesFile(liftRangeWithNonAsciiRelation);
			// SUT
			TryImport(liftFile, rangeFile, MergeStyle.MsKeepOnlyNew, 1);
			Assert.AreEqual(refTypeCountBeforeImport, Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Count, "Relation duplicated on import");
		}

		private sealed class MessageCapture : IMessageBox
		{
			public List<string> Messages = new List<string>();
			public DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
			{
				Messages.Add(text);
				return DialogResult.OK;
			}

			public DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon,
				MessageBoxDefaultButton defaultButton, MessageBoxOptions options, string helpFilePath, HelpNavigator navigator, object param)
			{
				Messages.Add(text);
				return DialogResult.OK;
			}
		}

		private sealed class CustomFieldData
		{
			/// <summary />
			internal string CustomFieldname;

			/// <summary />
			internal string StringFieldText;

			/// <summary />
			internal string StringFieldWs;

			/// <summary />
			internal CellarPropertyType CustomFieldType;

			/// <summary />
			internal List<string> MultiUnicodeStrings = new List<string>();

			/// <summary />
			internal List<string> MultiUnicodeWss = new List<string>();

			/// <summary />
			internal int IntegerValue;

			internal string GenDateLiftFormat;

			internal string cmPossibilityNameRA;

			internal List<string> cmPossibilityNamesRS = new List<string>();
		}
	}
}