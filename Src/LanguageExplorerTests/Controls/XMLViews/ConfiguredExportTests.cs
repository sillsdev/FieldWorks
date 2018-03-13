// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using LanguageExplorer.Controls.XMLViews;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;
using SIL.WritingSystems;

namespace LanguageExplorerTests.Controls.XMLViews
{
	public class ConfiguredExportTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private IPublisher m_publisher;
		private ISubscriber m_subscriber;
		private IPropertyTable m_propertyTable;

		#region Overrides of LcmTestBase
		public override void FixtureSetup()
		{
			if (!Sldr.IsInitialized)
			{
				// initialize the SLDR
				Sldr.Initialize();
			}

			base.FixtureSetup();
		}

		public override void FixtureTeardown()
		{
			base.FixtureTeardown();

			if (Sldr.IsInitialized)
			{
				Sldr.Cleanup();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override to start an undoable UOW.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();

			m_propertyTable = TestSetupServices.SetupTestTriumvirate(out m_publisher, out m_subscriber);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override to end the undoable UOW, Undo everything, and 'commit',
		/// which will essentially clear out the Redo stack.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestTearDown()
		{
			m_propertyTable.Dispose();
			m_propertyTable = null;
			m_publisher = null;
			m_subscriber = null;

			base.TestTearDown();
		}
		#endregion

		[Test]
		public void BeginCssClassIfNeeded_UsesSafeClasses()
		{
			TestBeginCssClassForFlowType("para");
			TestBeginCssClassForFlowType("span");
		}

		[Test]
		public void XHTMLExportGetDigraphMapsFirstCharactersFromICUSortRules()
		{
			CoreWritingSystemDefinition ws = Cache.LangProject.DefaultVernacularWritingSystem;
			ws.DefaultCollation = new IcuRulesCollationDefinition("standard") {IcuRules = "&b < az << a < c <<< ch"};

			var exporter = new ConfiguredExport(null, null, 0);
			string output;
			using (var stream = new MemoryStream())
			{
				using (var writer = new StreamWriter(stream))
				{
					exporter.Initialize(Cache, m_propertyTable, writer, null, "xhtml", null, "dicBody");
					Dictionary<string, string> mapChars;
					ISet<string> ignoreSet;
					var data = exporter.GetDigraphs(ws.Id, out mapChars, out ignoreSet);
					Assert.AreEqual(mapChars.Count, 2, "Too many characters found equivalents");
					Assert.AreEqual(mapChars["a"], "az");
					Assert.AreEqual(mapChars["ch"], "c");
				}
			}
		}

#if RANDYTODO
		// TODO: Figure out why this is a new failure.
#endif
		[Test]
		[Ignore("RANDYTODO: Figure out why it fails, since the long awaited merge.")]
		public void XHTMLExportGetDigraphMapsFromICUSortRules_TestSecondaryTertiaryShouldNotGenerateHeader()
		{
			var ws = Cache.LangProject.DefaultVernacularWritingSystem;
			ws.DefaultCollation = new IcuRulesCollationDefinition("standard") { IcuRules = "&b << az / c <<< AZ / C" + Environment.NewLine + "&f << gz"};

			var exporter = new ConfiguredExport(null, null, 0);
			string output;
			using (var stream = new MemoryStream())
			{
				using (var writer = new StreamWriter(stream))
				{
					exporter.Initialize(Cache, m_propertyTable, writer, null, "xhtml", null, "dicBody");
					Dictionary<string, string> mapChars;
					ISet<string> ignoreSet;
					var data = exporter.GetDigraphs(ws.Id, out mapChars, out ignoreSet);
					Assert.AreEqual(data.Count, 0, "Header created for two wedges");
					Assert.AreEqual(mapChars.Count, 3, "Too many characters found equivalents");
					Assert.AreEqual(mapChars["az"], "b");
					Assert.AreEqual(mapChars["AZ"], "b");
					// Rules following the '/' rule should not be skipped LT-18309
					Assert.AreEqual(mapChars["gz"], "f");
				}
			}
		}

		[Test]
		public void XHTMLExportGetDigraphMapsFromICUSortRules_TertiaryIgnorableDoesNotCrash()
		{
			CoreWritingSystemDefinition ws = Cache.LangProject.DefaultVernacularWritingSystem;
			ws.DefaultCollation = new IcuRulesCollationDefinition("standard") {IcuRules = "&[last tertiary ignorable] = \\"};

			var exporter = new ConfiguredExport(null, null, 0);
			string output;
			using(var stream = new MemoryStream())
			{
				using(var writer = new StreamWriter(stream))
				{
					exporter.Initialize(Cache, m_propertyTable, writer, null, "xhtml", null, "dicBody");
					Dictionary<string, string> mapChars = null;
					ISet<string> ignoreSet = null;
					ISet<string> data = null;
					Assert.DoesNotThrow(() => data = exporter.GetDigraphs(ws.Id, out mapChars, out ignoreSet));
					// The second test catches the real world scenario, GetDigraphs is actually called many times, but the first time
					// is the only one that should trigger the algorithm, afterward the information is cached in the exporter.
					Assert.DoesNotThrow(() => data = exporter.GetDigraphs(ws.Id, out mapChars, out ignoreSet));
					Assert.AreEqual(mapChars.Count, 0, "Too many characters found equivalents");
					Assert.AreEqual(ignoreSet.Count, 1, "Ignorable character not parsed from rule");
				}
			}
		}

		[Test]
		public void XHTMLExportGetDigraphMapsFromICUSortRules_UnicodeTertiaryIgnorableWorks()
		{
			CoreWritingSystemDefinition ws = Cache.LangProject.DefaultVernacularWritingSystem;
			ws.DefaultCollation = new IcuRulesCollationDefinition("standard") {IcuRules = "&[last tertiary ignorable] = \\uA78C"};

			var exporter = new ConfiguredExport(null, null, 0);
			string output;
			using(var stream = new MemoryStream())
			{
				using(var writer = new StreamWriter(stream))
				{
					exporter.Initialize(Cache, m_propertyTable, writer, null, "xhtml", null, "dicBody");
					Dictionary<string, string> mapChars = null;
					ISet<string> ignoreSet = null;
					ISet<string> data = null;
					Assert.DoesNotThrow(() => data = exporter.GetDigraphs(ws.Id, out mapChars, out ignoreSet));
					Assert.AreEqual(mapChars.Count, 0, "Too many characters found equivalents");
					Assert.AreEqual(ignoreSet.Count, 1, "Ignorable character not parsed from rule");
					Assert.IsTrue(ignoreSet.Contains('\uA78C'.ToString(CultureInfo.InvariantCulture)));
				}
			}
		}

		[Test]
		public void XHTMLExportGetDigraphMapsFromICUSortRules_BeforeRuleSecondaryIgnored()
		{
			CoreWritingSystemDefinition ws = Cache.LangProject.DefaultVernacularWritingSystem;
			ws.DefaultCollation = new IcuRulesCollationDefinition("standard") {IcuRules = "& [before 2] a < aa <<< Aa <<< AA"};

			var exporter = new ConfiguredExport(null, null, 0);
			string output;
			using(var stream = new MemoryStream())
			{
				using(var writer = new StreamWriter(stream))
				{
					exporter.Initialize(Cache, m_propertyTable, writer, null, "xhtml", null, "dicBody");
					Dictionary<string, string> mapChars = null;
					ISet<string> ignoreSet = null;
					ISet<string> data = null;
					Assert.DoesNotThrow(() => data = exporter.GetDigraphs(ws.Id, out mapChars, out ignoreSet));
					Assert.AreEqual(data.Count, 0, "No characters should be generated by a before 2 rule");
					Assert.AreEqual(mapChars.Count, 0, "The rule should have been ignored, no characters ought to have been mapped");
					Assert.AreEqual(ignoreSet.Count, 0, "Ignorable character incorrectly parsed from rule");
				}
			}
		}

		[Test]
		public void XHTMLExportGetDigraphMapsFromICUSortRules_BeforeRuleCombinedWithNormalRuleWorks()
		{
			CoreWritingSystemDefinition ws = Cache.LangProject.DefaultVernacularWritingSystem;
			ws.DefaultCollation = new IcuRulesCollationDefinition("standard") {IcuRules = "& a < bb & [before 1] a < aa"};

			var exporter = new ConfiguredExport(null, null, 0);
			string output;
			using(var stream = new MemoryStream())
			{
				using(var writer = new StreamWriter(stream))
				{
					exporter.Initialize(Cache, m_propertyTable, writer, null, "xhtml", null, "dicBody");
					Dictionary<string, string> mapChars = null;
					ISet<string> ignoreSet = null;
					ISet<string> data = null;
					Assert.DoesNotThrow(() => data = exporter.GetDigraphs(ws.Id, out mapChars, out ignoreSet));
					Assert.AreEqual(data.Count, 2, "The [before 1] rule should have added one additional character");
				}
			}
		}

		[Test]
		public void XHTMLExportGetDigraphMapsFromICUSortRules_BeforeRulePrimaryGetsADigraph()
		{
			CoreWritingSystemDefinition ws = Cache.LangProject.DefaultVernacularWritingSystem;
			ws.DefaultCollation = new IcuRulesCollationDefinition("standard") {IcuRules = "& [before 1] a < aa <<< Aa <<< AA"};

			var exporter = new ConfiguredExport(null, null, 0);
			string output;
			using(var stream = new MemoryStream())
			{
				using(var writer = new StreamWriter(stream))
				{
					exporter.Initialize(Cache, m_propertyTable, writer, null, "xhtml", null, "dicBody");
					Dictionary<string, string> mapChars = null;
					ISet<string> ignoreSet = null;
					ISet<string> data = null;
					Assert.DoesNotThrow(() => data = exporter.GetDigraphs(ws.Id, out mapChars, out ignoreSet));
					Assert.AreEqual(data.Count, 1, "Wrong number of character mappings found");
					Assert.AreEqual(mapChars.Count, 2, "Wrong number of character mappings found");
					Assert.AreEqual(ignoreSet.Count, 0, "Ignorable character incorrectly parsed from rule");
				}
			}
		}

		[Test]
		public void XHTMLExportGetDigraphMapsFirstCharactersFromToolboxSortRules()
		{
			CoreWritingSystemDefinition ws = Cache.LangProject.DefaultVernacularWritingSystem;
			ws.DefaultCollation = new SimpleRulesCollationDefinition("standard") {SimpleRules = "b" + Environment.NewLine + "az a" + Environment.NewLine + "c ch"};

			var exporter = new ConfiguredExport(null, null, 0);
			string output;
			using (var stream = new MemoryStream())
			{
				using (var writer = new StreamWriter(stream))
				{
					exporter.Initialize(Cache, m_propertyTable, writer, null, "xhtml", null, "dicBody");
					Dictionary<string, string> mapChars;
					ISet<string> ignoreSet;
					var data = exporter.GetDigraphs(ws.Id, out mapChars, out ignoreSet);
					Assert.AreEqual(mapChars.Count, 2, "Too many characters found equivalents");
					Assert.AreEqual(mapChars["a"], "az");
					Assert.AreEqual(mapChars["ch"], "c");
				}
			}
		}

		[Test]
		public void XHTMLExportGetDigraphMapsFirstCharactersFromSortRulesWithNoMapping()
		{
			var ws = Cache.LangProject.DefaultVernacularWritingSystem;
			ws.DefaultCollation = new SimpleRulesCollationDefinition("standard") { SimpleRules = "b" + Environment.NewLine + "ñe ñ"};

			var exporter = new ConfiguredExport(null, null, 0);
			string output;
			using (var stream = new MemoryStream())
			{
				using (var writer = new StreamWriter(stream))
				{
					exporter.Initialize(Cache, m_propertyTable, writer, null, "xhtml", null, "dicBody");
					Dictionary<string, string> mapChars;
					ISet<string> ignoreSet;
					var data = exporter.GetDigraphs(ws.Id, out mapChars, out ignoreSet);
					Assert.AreEqual(data.Count, 2, "Two Digraphs should be returned");
					Assert.AreEqual(mapChars["ñ"], "ñe");
				}
			}
		}

		[Test]
		public void XHTMLExportGetLeadChar_SurrogatePairDoesNotCrash()
		{
			string data = null;
			CoreWritingSystemDefinition wsEn;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("ipo", out wsEn);
			Cache.ServiceLocator.WritingSystems.AddToCurrentVernacularWritingSystems(wsEn);
			string entryLetter = "\U00016F00\U00016F51\U00016F61\U00016F90";
			Dictionary<string, ISet<string>> wsDigraphMap = new Dictionary<string, ISet<string>>();
			Dictionary<string, Dictionary<string, string>> wsCharEquivalentMap = new Dictionary<string, Dictionary<string, string>>();
			Dictionary<string, ISet<string>> wsIgnorableCharMap = new Dictionary<string, ISet<string>>();
			Assert.DoesNotThrow(() => data = ConfiguredExport.GetLeadChar(entryLetter, "ipo", wsDigraphMap, wsCharEquivalentMap, wsIgnorableCharMap, Cache));
			Assert.AreEqual(data.Length, 2, "Surrogate pair should contains 2 characters");
		}

		/// <summary>
		/// Test verifies minimal behavior added for sort rules other than Toolbox and ICU
		/// (which currently does something minimal, enough to prevent crashes).
		/// This test currently just verifies that, indeed, we don't crash.
		/// It may be desirable to do something more for some or all of the other cases,
		/// in which case this test will probably need to change.
		/// </summary>
		[Test]
		public void XHTMLExportGetDigraphMapsFirstCharactersFromOtherSortRules()
		{
			CoreWritingSystemDefinition ws = Cache.LangProject.DefaultVernacularWritingSystem;
			ws.DefaultCollation = new SystemCollationDefinition {LanguageTag = "fr"};

			var exporter = new ConfiguredExport(null, null, 0);
			string output;
			using (var stream = new MemoryStream())
			{
				using (var writer = new StreamWriter(stream))
				{
					exporter.Initialize(Cache, m_propertyTable, writer, null, "xhtml", null, "dicBody");
					Dictionary<string, string> mapChars;
					ISet<string> ignoreSet;
					var data = exporter.GetDigraphs(ws.Id, out mapChars, out ignoreSet);
					Assert.AreEqual(mapChars.Count, 0, "No equivalents expected");
				}
			}
		}

		private void TestBeginCssClassForFlowType(string flowType)
		{
			var exporter = new ConfiguredExport(null, null, 0);
			string output;
			using (var stream = new MemoryStream())
			{
				using (var writer = new StreamWriter(stream))
				{
					exporter.Initialize(Cache, m_propertyTable, writer, null, "xhtml", null, "dicBody");

					var frag = XDocument.Parse("<p css='some#style' flowType='" + flowType + "'/>");

					exporter.BeginCssClassIfNeeded(frag.Root);
					writer.Flush();
					stream.Seek(0, SeekOrigin.Begin);
					using (var reader = new StreamReader(stream))
					{
						output = reader.ReadToEnd();
					}
				}
			}
			Assert.That(output, Is.StringContaining("class=\"someNUMBER_SIGNstyle\""));
		}
	}
}
