// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.LexText.Controls;

namespace LexTextControlsTests
{
	public partial class LiftMergerTests
	{
		private static string[] sequenceLiftData = new[]
			{
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

		private static string[] sequenceLiftData2 = new[]
			{
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

		[Test]
		public void TestImportDoesNotDuplicateSequenceRelations()
		{
			//This test is for the issue documented in LT-13747
			SetWritingSystems("fr");

			CreateNeededStyles();

			var senseRepo = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();

			var sOrigFile = CreateInputFile(sequenceLiftData);
			var logFile = TryImport(sOrigFile, null, FlexLiftMerger.MergeStyle.MsKeepNew, 3);
			var coldSense = senseRepo.GetObject(new Guid("57f884c0-0df2-43bf-8ba7-c70b2a208cf1"));

			Assert.AreEqual(1, coldSense.LexSenseReferences.Count(), "Too many LexSenseReferences, import has issues.");
			Assert.AreEqual(2, coldSense.LexSenseReferences.First().TargetsRS.Count,
								 "Incorrect number of references, part relations not imported correctly.");

			var sNewFile = CreateInputFile(sequenceLiftData2);
			TryImport(sNewFile, null, FlexLiftMerger.MergeStyle.MsKeepOnlyNew, 3);
			const string coolerGuid = "42510a32-787c-4162-80b1-0f94ef2eb3bf";
			var coolerSense = senseRepo.GetObject(new Guid(coolerGuid));

			//There should be 1 LexSenseReference representing the new cool, cooler order.
			Assert.AreEqual(1, coldSense.LexSenseReferences.Count(), "Too many LexSenseReferences, the relation was not merged.");
			Assert.AreEqual(2, coldSense.LexSenseReferences.First().TargetsRS.Count,
								 "Incorrect number of references, part relations not imported correctly.");
			Assert.AreEqual(coolerGuid, coldSense.LexSenseReferences.First().TargetsRS[1].Guid.ToString(),
								 "Sequence incorrectly modified.");
			Assert.AreEqual(1, coolerSense.LexSenseReferences.Count(), "Incorrect number of references in the leg sense.");
			Assert.AreEqual(2, coolerSense.LexSenseReferences.First().TargetsRS.Count,
								 "Incorrect number of targets in the leg sense.");
		}

		private static string[] componentData = new[]
			{
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

		private static string[] componentData2 = new[]
			{
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

		[Test]
		public void TestImportRemovesItemFromComponentRelation()
		{
			//This test is for the issue documented in LT-13764
			SetWritingSystems("fr");

			CreateNeededStyles();

			var entryRepo = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();

			var sOrigFile = CreateInputFile(componentData);
			var logFile = TryImport(sOrigFile, null, FlexLiftMerger.MergeStyle.MsKeepNew, 3);
			var coldEntry = entryRepo.GetObject(new Guid("d76f4068-833e-40a8-b4d5-5f4ba785bf6e"));
			var ler = coldEntry.LexEntryReferences;
			Assert.AreEqual(3, coldEntry.LexEntryReferences.ElementAt(0).TargetsRS.Count,
								 "Incorrect number of component references.");

			var sNewFile = CreateInputFile(componentData2);
			logFile = TryImport(sNewFile, null, FlexLiftMerger.MergeStyle.MsKeepOnlyNew, 3);
			const string coolerGuid = "03237d6e-a327-436b-8ae3-b84eed3549fd";
			Assert.AreEqual(2, coldEntry.LexEntryReferences.ElementAt(0).TargetsRS.Count,
								 "Incorrect number of component references.");
			var coolerEntry = entryRepo.GetObject(new Guid(coolerGuid));
			Assert.AreEqual(0, coolerEntry.LexEntryReferences.Count());
		}


		private static readonly string[] s_ComponentTest = new[]
			{
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

		private static readonly string[] s_ComponentTest2 = new[]
			{
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

		[Test]
		public void TestImportWarnsOnNonSubsetCollectionMerge()
		{
			SetWritingSystems("fr");

			CreateNeededStyles();

			var entryRepository = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();

			var sOrigFile = CreateInputFile(s_ComponentTest);
			TryImport(sOrigFile, null, FlexLiftMerger.MergeStyle.MsKeepNew, 3);
			var sMergeFile = CreateInputFile(s_ComponentTest2);
			var logFile = TryImport(sMergeFile, null, FlexLiftMerger.MergeStyle.MsKeepNew, 4);
			var todoEntry = entryRepository.GetObject(new Guid("10af904a-7395-4a37-a195-44001127ae40"));
			Assert.AreEqual(1, todoEntry.LexEntryReferences.Count());
			Assert.AreEqual(3, todoEntry.LexEntryReferences.First().TargetsRS.Count);
			using(var stream = new StreamReader(logFile))
			{
				string data = stream.ReadToEnd();
				stream.Close();
				Assert.IsTrue(data.Contains("Combined Collections"), "Logfile does not show conflict for collection.");
			}
		}

		private static readonly string[] s_LT12948Test = new[]
			{
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
			Assert.AreEqual(1, todoEntry.ComplexFormEntryRefs.Count(),
								 "Too many ComplexFormEntryRefs? Then they were incorrectly split.");
			Assert.AreEqual(2, todoEntry.ComplexFormEntryRefs.First().ComponentLexemesRS.Count, "Wrong number of Components.");
			Assert.AreEqual(1, todoEntry.VariantEntryRefs.Count(), "Wrong number of VariantEntryRefs.");
		}

		private static readonly string[] mergeTestOld = new[]
			{
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

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// LIFT Import:  Test that two component lists which share an entry are considered the same
		/// list and merged rather than put in two different lists of components.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestMergeWithDiffComponentListKeepOld()
		{
			SetWritingSystems("fr");

			CreateNeededStyles();

			var entryRepository = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();

			var sOrigFile = CreateInputFile(mergeTestOld);
			var logFile = TryImport(sOrigFile, null, FlexLiftMerger.MergeStyle.MsKeepNew, 4);

			var sNewFile = CreateInputFile(s_LT12948Test2);
			TryImport(sNewFile, null, FlexLiftMerger.MergeStyle.MsKeepNew, 3);
			var todoEntry = entryRepository.GetObject(new Guid("10af904a-7395-4a37-a195-44001127ae40"));

			//Even though they do not have an order set (due to a now fixed export defect) the two relations in the 'todo' entry
			//should be collected in the same LexEntryRef
			Assert.AreEqual(1, todoEntry.ComplexFormEntryRefs.Count(), "Too many ComplexForms, they were incorrectly split.");
			Assert.AreEqual(1, todoEntry.VariantEntryRefs.Count(), "Wrong number of VariantEntryRefs.");
			Assert.AreEqual(1, todoEntry.VariantEntryRefs.First().ComponentLexemesRS.Count, "Incorrect number of Variants.");
			Assert.AreEqual(2, todoEntry.ComplexFormEntryRefs.First().ComponentLexemesRS.Count, "Incorrect number of components.");
		}

		private static readonly string[] s_LT12948Test3 = new[]
			{
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

		// This data represents a lift file with 3 entries of form 'arm', 'leg', and 'body' with a whole/part relationship between 'arm' and 'body'
		private static string[] treeLiftData = new[]
			{
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

		private static string[] treeLiftRange = new[]
			{
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

		// This lift data is a modified version of treeLiftData preserving 'arm', 'leg', and 'body' but adding 'leg' to the whole/part relation.
		private static string[] treeLiftData2 = new[]
			{
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

		[Test]
		public void TestImportDoesNotDuplicateTreeRelations()
		{
			SetWritingSystems("fr");

			CreateNeededStyles();

			var senseRepo = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();

			var sOrigFile = CreateInputFile(treeLiftData);
			var logFile = TryImport(sOrigFile, CreateInputRangesFile(treeLiftRange), FlexLiftMerger.MergeStyle.MsKeepNew, 4);
			var bodySense = senseRepo.GetObject(new Guid("52c632c2-98ad-4f97-b130-2a32992254e3"));

			Assert.AreEqual(1, bodySense.LexSenseReferences.Count(), "Too many LexSenseReferences, the parts were split.");
			Assert.AreEqual(2, bodySense.LexSenseReferences.First().TargetsRS.Count,
								 "Incorrect number of references, part relations not imported correctly.");

			var sNewFile = CreateInputFile(treeLiftData2);
			TryImport(sNewFile, CreateInputRangesFile(treeLiftRange), FlexLiftMerger.MergeStyle.MsKeepOnlyNew, 4);
			var legSense = senseRepo.GetObject(new Guid("62c632c2-98ad-4f97-b130-2a32992254e3"));
			var armSense = senseRepo.GetObject(new Guid("5ca96ad0-cb18-4ddc-be8e-3547fc87221f"));
			//There should be 1 LexSenseReference for the Whole/Part relationship and each involved sense should share it.
			Assert.AreEqual(1, bodySense.LexSenseReferences.Count(), "Too many LexSenseReferences, the parts were split.");
			Assert.AreEqual(3, bodySense.LexSenseReferences.First().TargetsRS.Count,
								 "Incorrect number of references, part relations not imported correctly.");
			Assert.AreEqual(1, legSense.LexSenseReferences.Count(), "Incorrect number of references in the leg sense.");
			Assert.AreEqual(3, legSense.LexSenseReferences.First().TargetsRS.Count,
								 "Incorrect number of targets in the leg sense.");
			Assert.AreEqual(bodySense.LexSenseReferences.First(), legSense.LexSenseReferences.First(), "LexReferences of Body and Leg should match.");
			Assert.AreEqual(armSense.LexSenseReferences.First(), legSense.LexSenseReferences.First(), "LexReferences of Arm and Leg should match.");
		}

		// This lift data contains 'a' 'b' and 'c' entries with 'a' being a whole of 2 parts 'b' and 'c' (whole/part relation)
		private static string[] treeLiftDataBase = new[]
			{
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
		private static string[] treeLiftDataReparented = new[]
			{
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

		[Test]
		public void TestImportDoesNotConfuseModifiedTreeRelations()
		{
			SetWritingSystems("fr");

			CreateNeededStyles();

			var senseRepo = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();

			var sOrigFile = CreateInputFile(treeLiftDataBase);
			var logFile = TryImport(sOrigFile, CreateInputRangesFile(treeLiftRange), FlexLiftMerger.MergeStyle.MsKeepNew, 3);
			var aSense = senseRepo.GetObject(new Guid("5ca96ad0-cb18-4ddc-be8e-3547fc87221f"));
			var bSense = senseRepo.GetObject(new Guid("52c632c2-98ad-4f97-b130-2a32992254e3"));
			var cSense = senseRepo.GetObject(new Guid("62c632c2-98ad-4f97-b130-2a32992254e3"));

			Assert.AreEqual(1, aSense.LexSenseReferences.Count(), "Too many LexSenseReferences, the parts were split.");
			Assert.AreEqual(3, aSense.LexSenseReferences.First().TargetsRS.Count,
								 "Incorrect number of references, part relations not imported correctly.");

			var sNewFile = CreateInputFile(treeLiftDataReparented);
			TryImport(sNewFile, CreateInputRangesFile(treeLiftRange), FlexLiftMerger.MergeStyle.MsKeepOnlyNew, 4);
			var dSense = senseRepo.GetObject(new Guid("3b3632c2-98ad-4f97-b130-2a32992254e3"));
			//There should be 1 LexSenseReference for the Whole/Part relationship and each involved sense should share it.
			Assert.AreEqual(1, aSense.LexSenseReferences.Count(), "Too many LexSenseReferences, the parts were split.");
			Assert.AreEqual(2, aSense.LexSenseReferences.First().TargetsRS.Count,
								 "Incorrect number of references, part relations not imported correctly.");
			Assert.AreEqual(1, cSense.LexSenseReferences.Count(), "Incorrect number of references in the c sense.");
			Assert.AreEqual(2, cSense.LexSenseReferences.First().TargetsRS.Count,
								 "Incorrect number of targets in the c senses reference.");
			Assert.AreEqual(cSense.LexSenseReferences.First(), dSense.LexSenseReferences.First(), "c and d should be in the same relation");
			Assert.AreEqual(1, dSense.LexSenseReferences.Count(), "dSense picked up a phantom reference.");
		}

		// Defines a lift file with two entries 'Bother' and 'me'.
		private static string[] origNoPair = new[]
			{
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
		// modified LIFT with same entries 'Bother' and 'me' related using relation type Twain and Twin, expects a lexical-relation range in a ranges file
		private static string[] newWithPair = new[]
			{
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
		//ranges file with defining an Antonym and a Twin relation
		private static string[] newWithPairRange = new[]
			{
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

		[Test]
		public void TestImportCustomPairReferenceTypeWorks()
		{
			SetWritingSystems("fr");

			CreateNeededStyles();

			var entryRepo = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var senseRepo = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();

			var sOrigFile = CreateInputFile(origNoPair);
			var logFile = TryImport(sOrigFile, null, FlexLiftMerger.MergeStyle.MsKeepOnlyNew, 2);
			var aSense = senseRepo.GetObject(new Guid("c2b4fe44-a3d9-4a42-a87c-8e174593fb30"));
			var bSense = senseRepo.GetObject(new Guid("de2fcb48-319a-48cf-bfea-0f25b9f38b31"));
			Assert.AreEqual(0, aSense.LexSenseReferences.Count(), "Incorrect number of component references.");
			Assert.AreEqual(0, bSense.LexSenseReferences.Count(), "Incorrect number of component references.");

			var sNewFile = CreateInputFile(newWithPair);
			logFile = TryImport(sNewFile, CreateInputRangesFile(newWithPairRange), FlexLiftMerger.MergeStyle.MsKeepOnlyNew, 2);
			Assert.AreEqual(1, aSense.LexSenseReferences.Count(), "Incorrect number of component references.");
			Assert.AreEqual(1, bSense.LexSenseReferences.Count(), "Incorrect number of component references.");
			Assert.That(aSense.LexSenseReferences.First().TargetsRS.Contains(bSense), "The Twin/Twain relationship failed to contain 'Bother' and 'me'");
			Assert.That(bSense.LexSenseReferences.First().TargetsRS.Contains(aSense), "The Twin/Twain relationship failed to contain 'Bother' and 'me'");
			Assert.AreEqual(aSense.LexSenseReferences.First(), bSense.LexSenseReferences.First(), "aSense and bSense should share the same LexSenseReference.");
			Assert.That(aSense.LexSenseReferences.First().TargetsRS[0].Equals(bSense), "Twin item should come before Twain");
			Assert.That(bSense.LexSenseReferences.First().TargetsRS[0].Equals(bSense), "Twin item should come before Twain");
		}

		//ranges file with defining an Antonym as a default relation (no guid) and a Twin relation
		private static string[] rangeWithOneCustomAndOneDefault = new[]
			{
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

		[Test]
		public void TestImportCustomRangesIgnoresNonCustomRanges()
		{
			SetWritingSystems("fr");

			CreateNeededStyles();

			var entryRepo = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var senseRepo = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			var typeRepo = Cache.ServiceLocator.GetInstance<ILexRefTypeRepository>();

			var sOrigFile = CreateInputFile(newWithPair);
			Assert.AreEqual(0, typeRepo.Count, "Too many types exist before import, bootstrapping has changed?");
			var logFile = TryImport(sOrigFile, CreateInputRangesFile(rangeWithOneCustomAndOneDefault), FlexLiftMerger.MergeStyle.MsKeepOnlyNew, 2);
			var aSense = senseRepo.GetObject(new Guid("c2b4fe44-a3d9-4a42-a87c-8e174593fb30"));
			var bSense = senseRepo.GetObject(new Guid("de2fcb48-319a-48cf-bfea-0f25b9f38b31"));
			Assert.AreEqual(1, aSense.LexSenseReferences.Count(), "Incorrect number of component references.");
			Assert.AreEqual(1, bSense.LexSenseReferences.Count(), "Incorrect number of component references.");
			Assert.AreEqual(1, typeRepo.Count, "Too many types created during import.");

		}

		// Defines a lift file with two entries 'Bother' and 'me'.
		private static string[] origNoRelation = new[]
			{
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
		// modified LIFT with same entries 'Bother' and 'me' related using relation type queue (alternatively named enqueue)
		private static string[] newWithRelation = new[]
			{
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

		//ranges file with defining a custom queue SenseSequence type, which is called enqueue in french
		private static string[] newWithRelationRange = new[]
			{
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

		[Test]
		public void TestImportCustomReferenceTypeWithMultipleWsWorks()
		{
			Cache.LangProject.AnalysisWss = "en fr";
			Cache.LangProject.CurAnalysisWss = "en";
			Cache.LangProject.VernWss = "sen arb";
			Cache.LangProject.AddToCurrentVernacularWritingSystems(Cache.WritingSystemFactory.get_Engine("sen") as IWritingSystem);
			Cache.LangProject.AddToCurrentVernacularWritingSystems(Cache.WritingSystemFactory.get_Engine("arb") as IWritingSystem);
			Cache.LangProject.CurVernWss = "sen";

			CreateNeededStyles();

			var entryRepo = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var senseRepo = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			var refTypeRepo = Cache.ServiceLocator.GetInstance<ILexRefTypeRepository>();

			var sOrigFile = CreateInputFile(origNoRelation);
			var logFile = TryImport(sOrigFile, null, FlexLiftMerger.MergeStyle.MsKeepOnlyNew, 2);
			var aSense = senseRepo.GetObject(new Guid("c2b4fe44-a3d9-4a42-a87c-8e174593fb30"));
			var bSense = senseRepo.GetObject(new Guid("de2fcb48-319a-48cf-bfea-0f25b9f38b31"));
			Assert.AreEqual(0, aSense.LexSenseReferences.Count(), "Incorrect number of component references.");
			Assert.AreEqual(0, bSense.LexSenseReferences.Count(), "Incorrect number of component references.");

			var sNewFile = CreateInputFile(newWithRelation);
			logFile = TryImport(sNewFile, CreateInputRangesFile(newWithRelationRange), FlexLiftMerger.MergeStyle.MsKeepOnlyNew, 2);
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

		// lift data with the entries a, b, and c where a and c are in a Synonym relationship and b is in none.
		private static string[] origAntReplaceSyn = new[]
			{
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
		// replacment lift data where b and c entries are in an Antonym relationship and a isn't in any.
		private static string[] nextAntReplaceSyn = new[]
			{
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

		[Test]
		public void TestReplaceSynonymWithAntonymWorks()
		{
			SetWritingSystems("fr");

			CreateNeededStyles();

			var entryRepo = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var senseRepo = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			var refTypeRepo = Cache.ServiceLocator.GetInstance<ILexRefTypeRepository>();

			var sOrigFile = CreateInputFile(origAntReplaceSyn);
			var logFile = TryImport(sOrigFile, null, FlexLiftMerger.MergeStyle.MsKeepOnlyNew, 3);
			var aSense = senseRepo.GetObject(new Guid("a2096aa3-6076-47c0-b243-e50d00afaeb5"));
			var bSense = senseRepo.GetObject(new Guid("70a6973b-787e-4ddc-942f-3a2b2d0c6863"));
			var cSense = senseRepo.GetObject(new Guid("91eb7dc2-4057-4e7c-88c3-a81536a38c3e"));
			Assert.AreEqual(1, aSense.LexSenseReferences.Count(), "Incorrect number of component references.");
			Assert.AreEqual(0, bSense.LexSenseReferences.Count(), "Incorrect number of component references.");
			Assert.AreEqual(1, cSense.LexSenseReferences.Count(), "Incorrect number of component references.");
			var synType = refTypeRepo.AllInstances().FirstOrDefault(refType => refType.Name.BestAnalysisAlternative.Text.Equals("Synonyms"));
			Assert.That(synType != null && synType.MembersOC.Contains(aSense.LexSenseReferences.First()), "Synonym incorrectly imported.");

			var sNewFile = CreateInputFile(nextAntReplaceSyn);
			logFile = TryImport(sNewFile, null, FlexLiftMerger.MergeStyle.MsKeepOnlyNew, 3);
			Assert.AreEqual(0, aSense.LexSenseReferences.Count(), "Incorrect number of component references.");
			Assert.AreEqual(1, bSense.LexSenseReferences.Count(), "Incorrect number of component references.");
			Assert.AreEqual(1, cSense.LexSenseReferences.Count(), "Incorrect number of component references.");
			var antType = refTypeRepo.AllInstances().FirstOrDefault(refType => refType.Name.BestAnalysisAlternative.Text.Equals("Antonym"));
			Assert.That(antType != null && antType.MembersOC.Contains(bSense.LexSenseReferences.First()), "Antonym incorrectly imported.");
		}

		private static string[] twoEntryWithVariantComplexFormLift = new[]
			{
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

		private static string[] twoEntryWithVariantRefRemovedLift = new[]
			{
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

		[Test]
		public void TestDeleteRelationRefOnVariantComplexFormWorks()
		{
			SetWritingSystems("fr");

			CreateNeededStyles();

			var entryRepo = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var senseRepo = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();

			var sOrigFile = CreateInputFile(twoEntryWithVariantComplexFormLift);
			var logFile = TryImport(sOrigFile, null, FlexLiftMerger.MergeStyle.MsKeepOnlyNew, 2);
			var eEntry = entryRepo.GetObject(new Guid("40a9574d-2d13-4d30-9eab-9a6d84bf29f8"));
			var aEntry = entryRepo.GetObject(new Guid("ac828ef4-9a18-4802-b095-11cca00947db"));
			Assert.AreEqual(1, eEntry.VariantEntryRefs.Count(), "No VariantEntryRefs found when expected, import of lift data during test setup failed.");
			Assert.AreEqual(1, aEntry.VariantFormEntries.Count(), "Variant form Entry not found when expected, import of lift data during test setup failed.");

			var sNewFile = CreateInputFile(twoEntryWithVariantRefRemovedLift);
			logFile = TryImport(sNewFile, null, FlexLiftMerger.MergeStyle.MsKeepOnlyNew, 2);
			// An empty VariantEntryRef can not be unambiguously identified, so a new empty one is
			// created. This results in stable lift (doesn't change on round trip), but the fwdata
			// will change on round trip without real changes. This is not what we prefer, but think
			// it is OK for now. Nov 2013
			Assert.AreEqual(1, eEntry.VariantEntryRefs.Count(), "VariantEntryRef should remain after lift import.");
			Assert.AreEqual(0, aEntry.VariantFormEntries.Count(), "VariantForm Entry was not deleted during lift import."); // The reference was removed so the Entries collection should be empty
		}

		private static string[] twoEntryWithVariantRemovedLift = new[]
			{
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

		[Test]
		public void TestDeleteVariantComplexFormWorks()
		{
			SetWritingSystems("fr");

			CreateNeededStyles();

			var entryRepo = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var senseRepo = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();

			var sOrigFile = CreateInputFile(twoEntryWithVariantComplexFormLift);
			var logFile = TryImport(sOrigFile, null, FlexLiftMerger.MergeStyle.MsKeepOnlyNew, 2);
			var eEntry = entryRepo.GetObject(new Guid("40a9574d-2d13-4d30-9eab-9a6d84bf29f8"));
			var aEntry = entryRepo.GetObject(new Guid("ac828ef4-9a18-4802-b095-11cca00947db"));
			Assert.AreEqual(1, eEntry.VariantEntryRefs.Count(), "No VariantEntryRefs found when expected, import of lift data during test setup failed.");
			Assert.AreEqual(1, aEntry.VariantFormEntries.Count(), "Variant form Entry not found when expected, import of lift data during test setup failed.");

			var sNewFile = CreateInputFile(twoEntryWithVariantRemovedLift);
			logFile = TryImport(sNewFile, null, FlexLiftMerger.MergeStyle.MsKeepOnlyNew, 2);
			Assert.AreEqual(0, eEntry.VariantEntryRefs.Count(), "VariantEntryRef was not deleted during lift import.");
			Assert.AreEqual(0, aEntry.VariantFormEntries.Count(), "VariantForm Entry was not deleted during lift import.");
		}

		private static string[] twoEntryWithVariantComplexFormAndNewItemLift = new[]
			{
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

		[Test]
		public void TestVariantComplexFormNotDeletedWhenUnTouchedWorks()
		{
			SetWritingSystems("fr");

			CreateNeededStyles();

			var entryRepo = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var senseRepo = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();

			var sOrigFile = CreateInputFile(twoEntryWithVariantComplexFormLift);
			var logFile = TryImport(sOrigFile, null, FlexLiftMerger.MergeStyle.MsKeepOnlyNew, 2);
			var eEntry = entryRepo.GetObject(new Guid("40a9574d-2d13-4d30-9eab-9a6d84bf29f8"));
			var aEntry = entryRepo.GetObject(new Guid("ac828ef4-9a18-4802-b095-11cca00947db"));
			Assert.AreEqual(1, eEntry.VariantEntryRefs.Count(), "No VariantEntryRefs found when expected, import of lift data during test setup failed.");
			Assert.AreEqual(1, aEntry.VariantFormEntries.Count(), "Variant form Entry not found when expected, import of lift data during test setup failed.");

			var sNewFile = CreateInputFile(twoEntryWithVariantComplexFormAndNewItemLift);
			logFile = TryImport(sNewFile, null, FlexLiftMerger.MergeStyle.MsKeepOnlyNew, 3);
			Assert.AreEqual(1, eEntry.VariantEntryRefs.Count(), "VariantEntryRef mistakenly deleted during lift import.");
			Assert.AreEqual(1, aEntry.VariantFormEntries.Count(), "VariantForm Entry was mistakenly deleted during lift import.");
		}

		private static string[] twoEntryWithDerivativeComplexFormLift = new[]
			{
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

		private static string[] twoEntryWithDerivativeComplexFormRemovedLift = new[]
			{
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

		[Test]
		public void TestDeleteDerivativeComplexFormWorks()
		{
			SetWritingSystems("fr");

			CreateNeededStyles();

			var entryRepo = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var senseRepo = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();

			var sOrigFile = CreateInputFile(twoEntryWithDerivativeComplexFormLift);
			var logFile = TryImport(sOrigFile, null, FlexLiftMerger.MergeStyle.MsKeepOnlyNew, 2);
			var eEntry = entryRepo.GetObject(new Guid("40a9574d-2d13-4d30-9eab-9a6d84bf29f8"));
			var aEntry = entryRepo.GetObject(new Guid("ac828ef4-9a18-4802-b095-11cca00947db"));
			Assert.AreEqual(1, eEntry.ComplexFormEntryRefs.Count(), "No ComplexFormEntryRefs found when expected, import of lift data during test setup failed.");
			Assert.AreEqual(1, aEntry.ComplexFormEntries.Count(), "No ComplexEntries found when expected, import of lift data during test setup failed.");

			var sNewFile = CreateInputFile(twoEntryWithDerivativeComplexFormRemovedLift);
			logFile = TryImport(sNewFile, null, FlexLiftMerger.MergeStyle.MsKeepOnlyNew, 2);
			Assert.AreEqual(0, eEntry.ComplexFormEntryRefs.Count(), "ComplexFormEntryRefs was not deleted during lift import.");
			Assert.AreEqual(0, aEntry.ComplexFormEntries.Count(), "ComplexFormEntry was not deleted during lift import.");
			Assert.AreEqual(1, eEntry.VariantEntryRefs.Count(), "An empty VariantEntryRef should have resulted from the import");
			Assert.AreEqual(0, aEntry.VariantFormEntries.Count(), "An empty VariantEntryRef should have resulted from the import");
		}

		[Test]
		public void TestImportLexRefType_NonAsciiCharactersDoNotCauseDuplication()
		{
			var unNormalizedOmega = '\u2126';
			var normalizedOmega = '\u03A9';
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
			var entryRepo = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var senseRepo = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			var refTypeRepo = Cache.ServiceLocator.GetInstance<ILexRefTypeRepository>();
			var refTypeFactory = Cache.ServiceLocator.GetInstance<ILexRefTypeFactory>();
			var testType = refTypeFactory.Create(new Guid("b7862f14-ea5e-11de-8d47-0013722f8dec"), null);
			testType.Name.set_String(wsEn, Cache.TsStrFactory.MakeString("Test" + normalizedOmega, wsEn));
			testType.Abbreviation.set_String(wsEn, Cache.TsStrFactory.MakeString("test", wsEn));
			Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Add(testType);
			var refTypeCountBeforeImport = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Count;
			var liftFile = CreateInputFile(liftWithSenseUsingNonAsciiRelation);
			var rangeFile = CreateInputRangesFile(liftRangeWithNonAsciiRelation);
			// SUT
			var logFile = TryImport(liftFile, rangeFile, FlexLiftMerger.MergeStyle.MsKeepOnlyNew, 1);
			Assert.AreEqual(refTypeCountBeforeImport, Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Count, "Relation duplicated on import");
		}
	}
}
