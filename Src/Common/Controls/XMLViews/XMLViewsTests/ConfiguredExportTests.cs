using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using NUnit.Framework;
using Palaso.WritingSystems;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.Utils;

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
			var ws = Cache.LangProject.DefaultVernacularWritingSystem;
			ws.SortRules = "&b < az << a < c <<< ch";
			ws.SortUsing = WritingSystemDefinition.SortRulesType.CustomICU;

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
		public void XHTMLExportGetDigraphMapsFirstCharactersFromToolboxSortRules()
		{
			var ws = Cache.LangProject.DefaultVernacularWritingSystem;
			ws.SortRules = "b" + Environment.NewLine + "az a" + Environment.NewLine + "c ch";
			ws.SortUsing = WritingSystemDefinition.SortRulesType.CustomSimple;

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
