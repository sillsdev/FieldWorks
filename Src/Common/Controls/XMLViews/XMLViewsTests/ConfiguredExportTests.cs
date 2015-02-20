using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.Utils;
using SIL.WritingSystems;

namespace XMLViewsTests
{
	public class ConfiguredExportTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		[Test]
		public void BeginCssClassIfNeeded_UsesSafeClasses()
		{
			TestBeginCssClassForFlowType("para");
			TestBeginCssClassForFlowType("span");
		}

		[Test]
		public void XHTMLExportGetDigraphMapsFirstCharactersFromICUSortRules()
		{
			WritingSystem ws = Cache.LangProject.DefaultVernacularWritingSystem;
			ws.DefaultCollation = new CollationDefinition("standard") {IcuRules = "&b < az << a < c <<< ch"};

			var exporter = new ConfiguredExport(null, null, 0);
			string output;
			using (var stream = new MemoryStream())
			{
				using (var writer = new StreamWriter(stream))
				{
					exporter.Initialize(Cache, null, writer, null, "xhtml", null, "dicBody");
					Dictionary<string, string> mapChars;
					Set<string> ignoreSet;
					var data = exporter.GetDigraphs(ws.Id, out mapChars, out ignoreSet);
					Assert.AreEqual(mapChars.Count, 2, "Too many characters found equivalents");
					Assert.AreEqual(mapChars["a"], "az");
					Assert.AreEqual(mapChars["ch"], "c");
				}
			}
		}

		[Test]
		public void XHTMLExportGetDigraphMapsFromICUSortRules_TertiaryIgnorableDoesNotCrash()
		{
			WritingSystem ws = Cache.LangProject.DefaultVernacularWritingSystem;
			ws.DefaultCollation = new CollationDefinition("standard") {IcuRules = "&[last tertiary ignorable] = \\"};

			var exporter = new ConfiguredExport(null, null, 0);
			string output;
			using(var stream = new MemoryStream())
			{
				using(var writer = new StreamWriter(stream))
				{
					exporter.Initialize(Cache, null, writer, null, "xhtml", null, "dicBody");
					Dictionary<string, string> mapChars = null;
					Set<string> ignoreSet = null;
					Set<string> data = null;
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
			WritingSystem ws = Cache.LangProject.DefaultVernacularWritingSystem;
			ws.DefaultCollation = new CollationDefinition("standard") {IcuRules = "&[last tertiary ignorable] = \\uA78C"};

			var exporter = new ConfiguredExport(null, null, 0);
			string output;
			using(var stream = new MemoryStream())
			{
				using(var writer = new StreamWriter(stream))
				{
					exporter.Initialize(Cache, null, writer, null, "xhtml", null, "dicBody");
					Dictionary<string, string> mapChars = null;
					Set<string> ignoreSet = null;
					Set<string> data = null;
					Assert.DoesNotThrow(() => data = exporter.GetDigraphs(ws.Id, out mapChars, out ignoreSet));
					Assert.AreEqual(mapChars.Count, 0, "Too many characters found equivalents");
					Assert.AreEqual(ignoreSet.Count, 1, "Ignorable character not parsed from rule");
					Assert.IsTrue(ignoreSet.Contains('\uA78C'.ToString(CultureInfo.InvariantCulture)));
				}
			}
		}

		[Test]
		public void XHTMLExportGetDigraphMapsFirstCharactersFromToolboxSortRules()
		{
			WritingSystem ws = Cache.LangProject.DefaultVernacularWritingSystem;
			ws.DefaultCollation = new SimpleCollationDefinition("standard") {SimpleRules = "b" + Environment.NewLine + "az a" + Environment.NewLine + "c ch"};

			var exporter = new ConfiguredExport(null, null, 0);
			string output;
			using (var stream = new MemoryStream())
			{
				using (var writer = new StreamWriter(stream))
				{
					exporter.Initialize(Cache, null, writer, null, "xhtml", null, "dicBody");
					Dictionary<string, string> mapChars;
					Set<string> ignoreSet;
					var data = exporter.GetDigraphs(ws.Id, out mapChars, out ignoreSet);
					Assert.AreEqual(mapChars.Count, 2, "Too many characters found equivalents");
					Assert.AreEqual(mapChars["a"], "az");
					Assert.AreEqual(mapChars["ch"], "c");
				}
			}
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
			WritingSystem ws = Cache.LangProject.DefaultVernacularWritingSystem;
			ws.DefaultCollation = new InheritedCollationDefinition("standard") {BaseIetfLanguageTag = "fr", BaseType = "standard"};

			var exporter = new ConfiguredExport(null, null, 0);
			string output;
			using (var stream = new MemoryStream())
			{
				using (var writer = new StreamWriter(stream))
				{
					exporter.Initialize(Cache, null, writer, null, "xhtml", null, "dicBody");
					Dictionary<string, string> mapChars;
					Set<string> ignoreSet;
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
					exporter.Initialize(Cache, null, writer, null, "xhtml", null, "dicBody");

					var frag = new XmlDocument();
					frag.LoadXml("<p css='some#style' flowType='" + flowType + "'/>");

					exporter.BeginCssClassIfNeeded(frag.DocumentElement);
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
