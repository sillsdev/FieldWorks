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
using System.Text;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.LexText.Controls;
using System.IO;
using SIL.FieldWorks.FDO.FDOTests;
using LiftIO.Migration;

namespace LexTextControlsTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test the LIFT import functionality provided by the FlexLiftMerger class in conjunction
	/// with the LiftIO library.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class LiftMergerTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		/// Doesn't actually do anything (but 'implements' the needed interface)
		/// </summary>
		internal class DummyProgressDlg : IProgress
		{
			#region IProgress Members
			public bool Canceled
			{
				get { return false; }
			}
			public Form Form
			{
				get { return null; }
			}
			public int Maximum { get; set; }
			public string Message { get; set; }
			public int Minimum { get; set; }
			public int Position { get; set; }
			public void Step(int amount)
			{
				Position += amount;
			}
			public int StepSize { get; set; }
			public string Title { get; set; }
			#endregion
		}

		[SetUp]
		public void SetupForTest()
		{
			base.CreateTestData();
			Cache.LangProject.LexDbOA.ReferencesOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
		}


		private static string CreateInputFile(IList<string> data)
		{
			var path = Path.GetTempFileName();
			File.Delete(path);
			path = Path.ChangeExtension(path, ".lift");
			using (var wrtr = File.CreateText(path))
			{
				for (var i = 0; i < data.Count; ++i)
					wrtr.WriteLine(data[i]);
				wrtr.Close();
			}
			return path;
		}


		private string TryImport(string sOrigFile, int expectedCount)
		{
			try
			{
				IProgress progressDlg = new DummyProgressDlg();
				var fMigrationNeeded = Migrator.IsMigrationNeeded(sOrigFile);
				var sFilename = fMigrationNeeded ? Migrator.MigrateToLatestVersion(sOrigFile) : sOrigFile;
				var flexImporter = new FlexLiftMerger(Cache, FlexLiftMerger.MergeStyle.MsKeepBoth, true);
				var parser = new LiftIO.Parsing.LiftParser<LiftObject, LiftEntry, LiftSense, LiftExample>(flexImporter);
				flexImporter.LiftFile = sOrigFile;
				var cEntries = parser.ReadLiftFile(sFilename);
				Assert.AreEqual(expectedCount, cEntries);
				if (fMigrationNeeded)
					File.Delete(sFilename);
				flexImporter.ProcessPendingRelations(progressDlg);
				return flexImporter.DisplayNewListItems(sOrigFile, cEntries);
			}
			catch (Exception error)
			{
				return null;
			}
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
			"</lexical-unit>",
			"<trait name=\"morph-type\" value=\"root\"></trait>",
			"<sense id=\"hombre_f63f1ccf-3d50-417e-8024-035d999d48bc\">",
			"<grammatical-info value=\"Noun\">",
			"</grammatical-info>",
			"<gloss lang=\"en\"><text>man</text></gloss>",
			"<definition>",
			"<form lang=\"en\"><text>male adult human</text></form>",
			"</definition>",
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
			Cache.LangProject.VernWss = "es";
			Cache.LangProject.CurVernWss = "es";
			Cache.LangProject.AnalysisWss = "en";
			Cache.LangProject.CurAnalysisWss = "en";

			var repoEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var repoSense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			Assert.AreEqual(0, repoEntry.Count);
			Assert.AreEqual(0, repoSense.Count);

			var sOrigFile = CreateInputFile(s_LiftData1);
			var logFile = TryImport(sOrigFile, 4);
			File.Delete(sOrigFile);
			Assert.IsNotNull(logFile);
			File.Delete(logFile);
			Assert.AreEqual(4, repoEntry.Count);
			Assert.AreEqual(4, repoSense.Count);

			ILexEntry entry;
			Assert.IsTrue(repoEntry.TryGetObject(new Guid("ecfbe958-36a1-4b82-bb69-ca5210355400"), out entry));
			Assert.AreEqual(1, entry.SensesOS.Count);
			Assert.AreEqual(entry.SensesOS[0].Guid, new Guid("f63f1ccf-3d50-417e-8024-035d999d48bc"));
			Assert.IsNotNull(entry.LexemeFormOA);
			Assert.IsNotNull(entry.LexemeFormOA.MorphTypeRA);
			Assert.AreEqual("root", entry.LexemeFormOA.MorphTypeRA.Name.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("hombre", entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text);
			Assert.IsNotNull(entry.SensesOS[0].MorphoSyntaxAnalysisRA as IMoStemMsa);
			// ReSharper disable PossibleNullReferenceException
			Assert.IsNotNull((entry.SensesOS[0].MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA);
			Assert.AreEqual("Noun", (entry.SensesOS[0].MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA.Name.AnalysisDefaultWritingSystem.Text);
			// ReSharper restore PossibleNullReferenceException
			Assert.AreEqual("man", entry.SensesOS[0].Gloss.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("male adult human", entry.SensesOS[0].Definition.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual(2, entry.SensesOS[0].SemanticDomainsRC.Count);
			foreach (var sem in entry.SensesOS[0].SemanticDomainsRC)
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

			Assert.IsTrue(repoEntry.TryGetObject(new Guid("766aaee2-34b6-4e28-a883-5c2186125a2f"), out entry));
			Assert.AreEqual(1, entry.SensesOS.Count);
			Assert.AreEqual(entry.SensesOS[0].Guid, new Guid("cf6680cc-faeb-4bd2-90ec-0be5dcdcc6af"));
			Assert.IsNotNull(entry.LexemeFormOA);
			Assert.IsNotNull(entry.LexemeFormOA.MorphTypeRA);
			Assert.AreEqual("root", entry.LexemeFormOA.MorphTypeRA.Name.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("mujer", entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text);
			Assert.IsNotNull(entry.SensesOS[0].MorphoSyntaxAnalysisRA as IMoStemMsa);
			// ReSharper disable PossibleNullReferenceException
			Assert.IsNotNull((entry.SensesOS[0].MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA);
			Assert.AreEqual("Noun", (entry.SensesOS[0].MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA.Name.AnalysisDefaultWritingSystem.Text);
			// ReSharper restore PossibleNullReferenceException
			Assert.AreEqual("woman", entry.SensesOS[0].Gloss.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("female adult human", entry.SensesOS[0].Definition.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual(2, entry.SensesOS[0].SemanticDomainsRC.Count);
			foreach (var sem in entry.SensesOS[0].SemanticDomainsRC)
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
			Assert.AreEqual(entry.SensesOS[0].Guid, new Guid("04545fa2-e24c-446e-928c-2a13710359b3"));
			Assert.IsNotNull(entry.LexemeFormOA);
			Assert.IsNotNull(entry.LexemeFormOA.MorphTypeRA);
			Assert.AreEqual("stem", entry.LexemeFormOA.MorphTypeRA.Name.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("niño".Normalize(NormalizationForm.FormD), entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text);
			Assert.IsNotNull(entry.SensesOS[0].MorphoSyntaxAnalysisRA as IMoStemMsa);
			// ReSharper disable PossibleNullReferenceException
			Assert.IsNotNull((entry.SensesOS[0].MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA);
			Assert.AreEqual("Noun", (entry.SensesOS[0].MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA.Name.AnalysisDefaultWritingSystem.Text);
			// ReSharper restore PossibleNullReferenceException
			Assert.AreEqual("boy", entry.SensesOS[0].Gloss.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("male human child", entry.SensesOS[0].Definition.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual(2, entry.SensesOS[0].SemanticDomainsRC.Count);
			foreach (var sem in entry.SensesOS[0].SemanticDomainsRC)
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
			Assert.AreEqual(entry.SensesOS[0].Guid, new Guid("db9d3790-2f5c-4d99-b9fc-3b21b47fa505"));
			Assert.IsNotNull(entry.LexemeFormOA);
			Assert.IsNotNull(entry.LexemeFormOA.MorphTypeRA);
			Assert.AreEqual("stem", entry.LexemeFormOA.MorphTypeRA.Name.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("niña".Normalize(NormalizationForm.FormD), entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text);
			Assert.IsNotNull(entry.SensesOS[0].MorphoSyntaxAnalysisRA as IMoStemMsa);
			// ReSharper disable PossibleNullReferenceException
			Assert.IsNotNull((entry.SensesOS[0].MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA);
			Assert.AreEqual("Noun", (entry.SensesOS[0].MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA.Name.AnalysisDefaultWritingSystem.Text);
			// ReSharper restore PossibleNullReferenceException
			Assert.AreEqual("girl", entry.SensesOS[0].Gloss.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("female human child", entry.SensesOS[0].Definition.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual(2, entry.SensesOS[0].SemanticDomainsRC.Count);
			foreach (var sem in entry.SensesOS[0].SemanticDomainsRC)
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
			"<trait name=\"is-primary\" value=\"true\"/>",
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
			Assert.AreEqual(1, entry.EntryRefsOS.Count);
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

		static private readonly string[] s_LiftData3 = new[]
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
			"</entry>",
			"<entry dateCreated=\"2011-03-01T22:27:46Z\" dateModified=\"2011-03-01T22:28:00Z\" guid=\"67113a7f-e448-43e7-87cf-6d3a46ee10ec\" id=\"greenhouse_67113a7f-e448-43e7-87cf-6d3a46ee10ec\">",
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

			var sOrigFile = CreateInputFile(s_LiftData3);
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
	}
}
