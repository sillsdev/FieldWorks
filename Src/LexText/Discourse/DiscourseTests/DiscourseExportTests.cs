// Copyright (c) 2015-2022 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.LCModel;
using System.IO;
using System.Linq;
using System.Xml;
using SIL.LCModel.Core.Text;
using XCore;
// ReSharper disable PossibleNullReferenceException - we trust that XML documents are well formed.

namespace SIL.FieldWorks.Discourse
{
	/// <summary>
	/// Tests the discourse export code
	/// </summary>
	[TestFixture]
	public class DiscourseExportTests : InMemoryDiscourseTestBase
	{
		private IDsConstChart m_chart;
		private TestCCLogic m_logic;
		private ConstChartBody m_chartBody;
		private ConstituentChart m_constChart;
		private ICmPossibility m_templateRoot;
		private List<ICmPossibility> m_allColumns;
		private PropertyTable m_propertyTable;

		protected override void CreateTestData()
		{
			base.CreateTestData();
			m_logic = new TestCCLogic(Cache, m_chart, m_stText); // m_chart is still null!
			m_helper.Logic = m_logic;
			m_logic.Ribbon = new MockRibbon(Cache, m_stText.Hvo);
			m_templateRoot = m_helper.MakeTemplate(out m_allColumns);
			// Note: do this AFTER creating the template, which may also create the DiscourseData object.
			m_chart = m_helper.SetupAChart();
			m_propertyTable = new PropertyTable(null);
			m_constChart = new ConstituentChart(Cache, m_propertyTable, m_logic);
			m_constChart.Init(null, null, null);
			m_chartBody = m_constChart.Body;
			m_chartBody.Cache = Cache; // don't know why constructor doesn't do this, but it doesn't.

			m_chartBody.SetRoot(m_chart, m_allColumns.ToArray());

			//Set up some cells.
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(6);

			// Make last analysis point to WfiWordform instead of WfiGloss
			var lastOccurrence = allParaOccurrences[5];
			var wordform = (lastOccurrence.Analysis as IWfiGloss).Wordform;
			lastOccurrence.Segment.AnalysesRS.Replace(1, 0, new List<ICmObject> { wordform });
			// This block makes the first row, puts WordGroups in cells 1 and 2, and list refs in cells 1 and 2
			var row0 = m_helper.MakeFirstRow();
			var movedItem = allParaOccurrences[1];
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var marker = m_helper.GetAMarker();
			var cellPart0_1b = m_helper.MakeChartMarker(row0, 1, marker);
			var cellPart0_2 = m_helper.MakeWordGroup(row0, 2, movedItem, movedItem);
			var marker2 = m_helper.GetAnotherMarker();
			var cellPart0_2b = m_helper.MakeChartMarker(row0, 2, marker2);
			var cellPart0_2c = m_helper.MakeChartMarker(row0, 2, marker);
			var cellPart0_3 = m_helper.MakeWordGroup(row0, 3, lastOccurrence, lastOccurrence);

			// Now another row, and cell 4 on the first has a ref to it. The new row has a WordGroup with two
			// wordforms in cell 1. The cell is two columns wide, being merged with the previous cell.
			var row1 = m_helper.MakeSecondRow();
			m_helper.MakeDependentClauseMarker(row0, 4, new[] { row1 }, ClauseTypes.Song);
			var cellPart1_1 = m_helper.MakeWordGroup(row1, 1, allParaOccurrences[2], allParaOccurrences[3]);
			cellPart1_1.MergesBefore = true;

			// Let's have some notes on row 0.
			row0.Notes = TsStringUtils.MakeString("This is a test note", Cache.DefaultAnalWs);

			// And some moved text in row 1
			var cellPart1_2 = m_helper.MakeWordGroup(row1, 2, allParaOccurrences[4], allParaOccurrences[4]);
			m_helper.MakeMovedTextMarker(row1, 3, cellPart1_2, true);

			// We need four rows to properly test the variations on endPara/endSent
			var row2 = m_helper.MakeRow(m_chart, "2");
			row2.EndSentence = true;
			var row3 = m_helper.MakeRow(m_chart, "3");
			row3.EndSentence = true;
			row3.EndParagraph = true;
			m_helper.MakeRow(m_chart, "4");
		}

		protected void UngroupColumnsInTemplate()
		{
			//Remove the column group level from the template
			var columnGroups = m_templateRoot.SubPossibilitiesOS.ToArray();
			for (var i = columnGroups.Length; i > 0; i--)
			{
				var group = columnGroups[i - 1];
				if (group.SubPossibilitiesOS.Any())
				{
					while (group.SubPossibilitiesOS.Any())
					{
						m_templateRoot.SubPossibilitiesOS.Insert(i, group.SubPossibilitiesOS.Last());
					}
					m_templateRoot.SubPossibilitiesOS.RemoveAt(i - 1);
				}
			}
			EndSetupTask();
		}

		protected void PlaceTemplateUnderStoryNode()
		{
			var defAnalWs = Cache.DefaultAnalWs;
			var columnGroups = m_templateRoot.SubPossibilitiesOS.ToArray();
			var story = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
			// Add the Story node to the template first, to set its cache.
			m_templateRoot.SubPossibilitiesOS.Add(story);
			story.SubPossibilitiesOS.Replace(0, 0, columnGroups);
			story.Name.set_String(defAnalWs, TsStringUtils.MakeString("Story", defAnalWs));
		}

		public override void TestTearDown()
		{
			m_chartBody.Dispose();
			m_constChart.Dispose();
			m_propertyTable.Dispose();
			base.TestTearDown();
		}

		// Verify some basics about a child node (and return it)
		private static XmlNode VerifyNode(string message, XmlNode parent, int index, string name, int childCount, int attrCount)
		{
			var child = parent.ChildNodes[index];
			Assert.IsNotNull(child, message);
			Assert.AreEqual(childCount, child.ChildNodes.Count, message);
			Assert.AreEqual(name, child.Name, message);
			Assert.AreEqual(attrCount, child.Attributes.Count, message + " attribute count");
			return child;
		}

		// Verify attribute presence (and value, if attval is not null)
		private static void AssertAttr(XmlNode node, string attname, string attval)
		{
			var attr = node.Attributes[attname];
			Assert.That(attr, Is.Not.Null, "Expected node " + node.Name + " to have attribute " + attname);
			if (attval != null)
				Assert.AreEqual(attval, attr.Value, "Expected attr " + attname + " of " + node.Name + " to have value " + attval);
		}

		#region tests

		[Test]
		public void Export([Values(true, false)] bool isNotesOnRight, [Values(true, false)] bool isRtl)
		{
			m_constChart.NotesColumnOnRight = isNotesOnRight;
			m_chartBody.IsRightToLeft = isRtl;

			using (var stream = new MemoryStream())
			using (var writer = new XmlTextWriter(stream, Encoding.UTF8))
			using (var vc = new ConstChartVc(m_chartBody) {LineChoices = m_chartBody.LineChoices})
			using (var exporter = new DiscourseExporter(Cache, writer, m_chart.Hvo, vc, Cache.DefaultAnalWs))
			{
				// SUT
				exporter.ExportDisplay();
				// Close makes the stream unusable; Flush the writer and reset the stream position to 0
				writer.Flush();
				stream.Position = 0;
				using (var reader = new StreamReader(stream, Encoding.UTF8))
				{
					var result = reader.ReadToEnd();
					var doc = new XmlDocument();
					doc.LoadXml(result);
					var docNode = doc.DocumentElement;
					Assert.AreEqual("document", docNode.Name);
					var chartNode = VerifyNode("chart", docNode, 0, "chart", 7, 0);
					VerifyColumnGroupTitleRow(chartNode, 2, isNotesOnRight, isRtl);
					VerifyColumnTitleRow(chartNode, 2, isNotesOnRight, isRtl);
					VerifyFirstDataRow(chartNode, 2, isNotesOnRight, isRtl);
					VerifySecondDataRow(chartNode, 2, isNotesOnRight, isRtl);
					var thirdRow = VerifyNode("row", chartNode, 4, "row", 8, 3);
					AssertAttr(thirdRow, "endSent", "true");
					var fourthRow = VerifyNode("row", chartNode, 5, "row", 8, 3);
					AssertAttr(fourthRow, "endPara", "true");

					var langNode = VerifyNode("languages", docNode, 1, "languages", 2, 0);
					var enNode = VerifyNode("english lang node", langNode, 0, "language", 0, 2);
					AssertAttr(enNode, "lang", "en");
					AssertAttr(enNode, "font", null);
					// don't verify exact font, may depend on installation.
				}
			}
		}

		[Test]
		public void Export_NoColumnGroups()
		{
			UngroupColumnsInTemplate();

			using (var stream = new MemoryStream())
			using (var writer = new XmlTextWriter(stream, Encoding.UTF8))
			using (var vc = new ConstChartVc(m_chartBody) {LineChoices = m_chartBody.LineChoices})
			using (var exporter = new DiscourseExporter(Cache, writer, m_chart.Hvo, vc, Cache.DefaultAnalWs))
			{
				// SUT
				exporter.ExportDisplay();
				// Close makes the stream unusable; Flush the writer and reset the stream position to 0
				writer.Flush();
				stream.Position = 0;
				using (var reader = new StreamReader(stream, Encoding.UTF8))
				{
					var result = reader.ReadToEnd();
					var doc = new XmlDocument();
					doc.LoadXml(result);
					var docNode = doc.DocumentElement;
					Assert.AreEqual("document", docNode.Name);
					var chartNode = VerifyNode("chart", docNode, 0, "chart", 6, 0);
					VerifyColumnTitleRow(chartNode, 1, true, false);
					VerifyFirstDataRow(chartNode, 1, true, false);
					VerifySecondDataRow(chartNode, 1, true, false);
					var thirdRow = VerifyNode("row", chartNode, 3, "row", 8, 3);
					AssertAttr(thirdRow, "endSent", "true");
					var fourthRow = VerifyNode("row", chartNode, 4, "row", 8, 3);
					AssertAttr(fourthRow, "endPara", "true");

					var langNode = VerifyNode("languages", docNode, 1, "languages", 2, 0);
					var enNode = VerifyNode("english lang node", langNode, 0, "language", 0, 2);
					AssertAttr(enNode, "lang", "en");
					AssertAttr(enNode, "font", null);
					// don't verify exact font; may depend on installation.
				}
			}
		}

		[Test]
		public void Export_UnderStoryNode()
		{
			PlaceTemplateUnderStoryNode();

			using (var stream = new MemoryStream())
			using (var writer = new XmlTextWriter(stream, Encoding.UTF8))
			using (var vc = new ConstChartVc(m_chartBody) {LineChoices = m_chartBody.LineChoices})
			using (var exporter = new DiscourseExporter(Cache, writer, m_chart.Hvo, vc, Cache.DefaultAnalWs))
			{
				// SUT
				exporter.ExportDisplay();
				// Close makes the stream unusable; Flush the writer and reset the stream position to 0
				writer.Flush();
				stream.Position = 0;
				using (var reader = new StreamReader(stream, Encoding.UTF8))
				{
					var result = reader.ReadToEnd();
					var doc = new XmlDocument();
					doc.LoadXml(result);
					var docNode = doc.DocumentElement;
					Assert.AreEqual("document", docNode.Name);
					var chartNode = VerifyNode("chart", docNode, 0, "chart", 8, 0);
					VerifyStoryHeaderRow(chartNode, 3);
					VerifyColumnGroupTitleRow(chartNode, 3, true, false);
					VerifyColumnTitleRow(chartNode, 3, true, false);
					VerifyFirstDataRow(chartNode, 3, true, false);
					VerifySecondDataRow(chartNode, 3, true, false);
					var thirdRow = VerifyNode("row", chartNode, 5, "row", 8, 3);
					AssertAttr(thirdRow, "endSent", "true");
					var fourthRow = VerifyNode("row", chartNode, 6, "row", 8, 3);
					AssertAttr(fourthRow, "endPara", "true");

					var langNode = VerifyNode("languages", docNode, 1, "languages", 2, 0);
					var enNode = VerifyNode("english lang node", langNode, 0, "language", 0, 2);
					AssertAttr(enNode, "lang", "en");
					AssertAttr(enNode, "font", null);
				}
			}
		}

		#region bits and pieces
		/// <remarks>
		/// (Hasso) 2022.03: not sure the best place for this, but since this is the only test suite that tests <see cref="ConstChartVc.Display"/>,
		/// this is the easiest place to test displaying discrete chart parts.
		/// </remarks>
		[Test]
		public void DisplayChartBody()
		{
			using (var stream = new MemoryStream())
			using (var writer = new XmlTextWriter(stream, Encoding.UTF8))
			using (var vc = new ConstChartVc(m_chartBody) {LineChoices = m_chartBody.LineChoices})
			using (var exporter = new DiscourseExporter(Cache, writer, m_chart.Hvo, vc, Cache.DefaultAnalWs))
			{
				writer.WriteStartDocument();
				writer.WriteStartElement("document");
				writer.WriteStartElement("chart");
				// SUT
				vc.Display(exporter, m_chart.Hvo, ConstChartVc.kfragChart);
				writer.WriteEndElement();
				writer.WriteEndElement();
				writer.WriteEndDocument();
				// Close makes the stream unusable; Flush the writer and reset the stream position to 0
				writer.Flush();
				stream.Position = 0;
				using (var reader = new StreamReader(stream, Encoding.UTF8))
				{
					var result = reader.ReadToEnd();
					var doc = new XmlDocument();
					doc.LoadXml(result);
					var docNode = doc.DocumentElement;
					Assert.AreEqual("document", docNode.Name);
					var chartNode = VerifyNode("chart", docNode, 0, "chart", 5, 0);
					VerifyFirstDataRow(chartNode, 0, true, false);
					VerifySecondDataRow(chartNode, 0, true, false);
					var thirdRow = VerifyNode("row", chartNode, 2, "row", 8, 3);
					AssertAttr(thirdRow, "endSent", "true");
					var fourthRow = VerifyNode("row", chartNode, 3, "row", 8, 3);
					AssertAttr(fourthRow, "endPara", "true");
				}
			}
		}
		#endregion bits and pieces

		private static void VerifySecondDataRow(XmlNode chartNode, int titleRowCount, bool isNotesOnRight, bool isRtl)
		{
			//<row type="song" id="1b">
			var row = VerifyNode("second row", chartNode, titleRowCount + 1, "row", 7, 2);
			AssertAttr(row, "type", "song");
			AssertAttr(row, "id", "1b");
			var iCols = ComputeColumnIndices(6, isNotesOnRight, isRtl);
			//    <cell cols="1">
			//        <main>
			//            <rownum lang="en">1b</rownum>
			//        </main>
			//    </cell>
			AssertCellMainChild(row, iCols[0], 1, null, 1, "rownum", "1b", "en");
			//    <cell reversed="true" cols="2">
			//        <main>
			//            <lit noSpaceAfter="true" lang="en">[</lit>
			//            <word lang="fr">paragraph</word>
			//            <word lang="fr">one</word>
			//        </main>
			//        <glosses>
			//            <gloss>paragraphGloss18</gloss>
			//            <gloss>oneGloss22</gloss>
			//        </glosses>
			//    </cell>
			var cell1 = VerifyNode("open clause node in row 2", row, iCols[1], "cell", 2, 2);
			VerifyNode("main in 2/1", cell1, 0, "main", 3, 0);
			AssertAttr(cell1, "reversed", "true");
			AssertAttr(cell1, "cols", "2");
			// TODO (Hasso) 2022.03: for RTL, <lit/> should come after <word/>s instead (since this is actually the last cell).
			// TODO (cont): In real life, XLingPaper exports look wrong on both ends, even though the test somehow passes.
			AssertMainChild(cell1, 0, "lit", new[] { isRtl ? "noSpaceBefore" : "noSpaceAfter", "lang" }, new[] { "true", "en" },
				isRtl ? $"{ConstChartVc.RLM}]{ConstChartVc.RLM}" : "[");
			AssertMainChild(cell1, 1, "word", new[] { "lang" }, new[] { "fr" }, "paragraph");
			AssertMainChild(cell1, 2, "word", new[] { "lang" }, new[] { "fr" }, "one");
			var glosses1 = VerifyNode("glosses cell 1/2", cell1, 1, "glosses", 2, 0);
			var gloss1_1 = VerifyNode("second gloss in 1/2", glosses1, 1, "gloss", 1, 1);
			AssertAttr(gloss1_1, "lang", "en");
			Assert.AreEqual("oneGloss22", gloss1_1.InnerText);
			//    <cell cols="1">
			//        <main>
			//            <word moved="true" lang="fr">It</word>
			//        </main>
			//        <glosses>
			//            <gloss>ItGloss26</gloss>
			//        </glosses>
			//    </cell>
			var cell2 = VerifyNode("third node in row 2", row, iCols[2], "cell", 2, 1);
			VerifyNode("main in 2/2", cell2, 0, "main", 1, 0);
			AssertMainChild(cell2, 0, "word", new[] { "moved", "lang" }, new[] { "true", "fr" }, "It");
			var glosses2 = VerifyNode("glosses cell 2/2", cell2, 1, "glosses", 1, 0);
			var gloss2_0 = VerifyNode("gloss in 2/2", glosses2, 0, "gloss", 1, 1);
			AssertAttr(gloss2_0, "lang", "en");
			Assert.AreEqual("ItGloss26", gloss2_0.InnerText);
			//    <cell cols="1">
			//        <main>
			//            <moveMkr lang="en">Preposed</moveMkr>
			//            <lit noSpaceBefore="true" lang="en">]</lit>
			//        </main>
			//    </cell>
			var cell3 = VerifyNode("close clause node in row 2", row, iCols[3], "cell", 1, 1);
			var idxInCell3 = ComputeColumnIndices(2, true, isRtl);
			AssertMainChild(cell3, idxInCell3[0], "moveMkr", new[] { "lang" }, new[] { "en" }, "Preposed");
			AssertMainChild(cell3, idxInCell3[1], "lit", new[] { isRtl ? "noSpaceAfter" : "noSpaceBefore", "lang" }, new[] { "true", "en" },
				isRtl ? $"{ConstChartVc.RLM}[{ConstChartVc.RLM}" : "]");
			//    <cell cols="1">
			//        <main />
			//    </cell>
			//    <cell cols="1">
			//        <main />
			//    </cell>
			//    <cell cols="1">
			//        <main />
			//    </cell>
			//</row>
		}

		private static void VerifyFirstDataRow(XmlNode chartNode, int titleRowCount, bool isNotesOnRight, bool isRtl)
		{
			//<row type="normal" id="1">
			var row = VerifyNode("first row", chartNode, titleRowCount, "row", 8, 2);
			AssertAttr(row, "type", "normal");
			AssertAttr(row, "id", "1");
			var iCols = ComputeColumnIndices(7, isNotesOnRight, isRtl);
			//    <cell cols="1">
			//        <main>
			//            <rownum lang="en">1</rownum>
			//        </main>
			//    </cell>
			AssertCellMainChild(row, iCols[0], 1, null, 1, "rownum", "1", "en");
			//    <cell cols="1">
			//        <main />
			//    </cell>
			AssertCellMainChild(row, iCols[1], 1, null, 0, null, null, null);
			//    <cell cols="1">
			//        <main>
			//            <word lang="fr">this</word>
			//            <lit noSpaceAfter="true" lang="en">(</lit>
			//            <listRef lang="en">I2</listRef>
			//            <lit noSpaceBefore="true" lang="en">)</lit>
			//        </main>
			//        <glosses>
			//            <gloss lang="en">thisGloss5</gloss>
			//        </glosses>
			//    </cell>
			var cell2 = AssertCellMainChild(row, iCols[2], 1, new [] { "thisGloss5" }, 4, "word", "this", "fr");
			AssertMainChild(cell2, 1, "lit", new [] { "noSpaceAfter", "lang" }, new[] { "true", "en" }, "(");
			AssertMainChild(cell2, 2, "listRef", new [] { "lang" }, new[] { "en" }, "I2");
			AssertMainChild(cell2, 3, "lit", new [] { "noSpaceBefore", "lang" }, new [] { "true", "en" }, ")");
			//    <cell cols="1">
			//        <main>
			//            <word lang="fr">is</word>
			//            <lit noSpaceAfter="true" lang="en">(</lit>
			//            <listRef lang="en">I3</listRef>
			//            <lit noSpaceBefore="true" lang="en">)</lit>
			//            <lit noSpaceAfter="true" lang="en">(</lit>
			//            <listRef lang="en">I2</listRef>
			//            <lit noSpaceBefore="true" lang="en">)</lit>
			//        </main>
			//        <glosses>
			//            <gloss lang="en">isGloss8</gloss>
			//        </glosses>
			//    </cell>
			AssertCellMainChild(row, iCols[3], 1, new [] { "isGloss8" }, 7, "word", "is", "fr");
			//    <cell cols="1">
			//        <main>
			//            <word lang="fr">is</word>
			//        </main>
			//        <glosses>
			//            <gloss lang="en">***</gloss>
			//        </glosses>
			//    </cell>
			AssertCellMainChild(row, iCols[4], 1, new[] { "***" }, 1, "word", "is", "fr");
			//    <cell cols="1">
			//        <main>
			//            <lit noSpaceAfter="true" lang="en">[</lit>
			//            <clauseMkr target="1b" lang="en">1b</clauseMkr>
			//            <lit noSpaceBefore="true" lang="en">]</lit>
			//        </main>
			//    </cell>
			var cell4 = VerifyNode("fourth node in row 0", row, iCols[5], "cell", 1, 1);
			VerifyNode("main in 4/1", cell4, 0, "main", 3, 0);
			AssertMainChild(cell4, 0, "lit", new[] { "noSpaceAfter", "lang" }, new[] { "true", "en" }, "[");
			AssertMainChild(cell4, 1, "clauseMkr", new[] { "target", "lang" }, new[] { "1b", "en" }, "1b");
			AssertMainChild(cell4, 2, "lit", new[] { "noSpaceBefore", "lang" }, new[] { "true", "en" }, "]");
			//    <cell cols="1">
			//        <main />
			//    </cell>

			//    <cell cols="1">
			//        <main>
			//            <note lang="en">This is a test note</note>
			//        </main>
			//    </cell>
			AssertCellMainChild(row, isNotesOnRight ? 7 : 0, 1, null, 1, "note", "This is a test note", "en");
			//</row>
		}

		/// <summary>
		/// Assert that the root node "cell" has a child "main" at index zero, which has a child
		/// at "index" with the given name, list of attributes, and inner text.
		/// </summary>
		private static void AssertMainChild(XmlNode cell, int index, string name, string[] attrs, string[] vals, string inner)
		{
			var main = cell.ChildNodes[0];
			var child = VerifyNode("cell main child", main, index, name, 1, attrs.Length);
			for (var i = 0; i < attrs.Length; i++)
				AssertAttr(child, attrs[i], vals[i]);
			Assert.AreEqual(inner, child.InnerText);
		}

		/// <summary>
		/// Computes column indices for `count` columns. NB: the Notes column is not included in `count` or the result.
		/// </summary>
		private static List<int> ComputeColumnIndices(int count, bool isNotesOnRight, bool isRtl)
		{
			var notesOffset = isNotesOnRight ? 0 : 1;
			var iCols = new List<int>(count);
			for (var i = 0; i < count; i++)
			{
				iCols.Add(i + notesOffset);
			}
			if (isRtl)
			{
				iCols.Reverse();
			}
			return iCols;
		}

		private void VerifyColumnTitleRow(XmlNode chartNode, int titleRowCount, bool isNotesOnRight, bool isRtl)
		{
			//<row type="title2">
			//    <cell cols="1">
			//        <main />
			//    </cell>
			//    <cell cols="1">
			//        <main>
			//            <lit lang="en">prenuc1</lit>
			//        </main>
			//    </cell>
			//    ...prenuc2...
			//    <cell cols="1">
			//        <main>
			//            <lit lang="en">subject</lit>
			//        </main>
			//    </cell>
			//    ...verb...object...postnuc...(empty)
			//</row>
			var titleRow = VerifyNode($"title{titleRowCount}", chartNode, titleRowCount - 1, "row", 8, 1);
			AssertAttr(titleRow, "type", $"title{titleRowCount}");
			var iCols = ComputeColumnIndices(7, isNotesOnRight, isRtl);
			VerifyNode("minimal first cell", titleRow, iCols[0], "cell", 1, 1);
			AssertCellMainLit(titleRow, iCols[1], 1, "prenuc1", "en");
			AssertCellMainLit(titleRow, iCols[2], 1, "prenuc2", "en");
			AssertCellMainLit(titleRow, iCols[3], 1, "Subject", "en");
			AssertCellMainLit(titleRow, iCols[4], 1, "verb", "en");
			AssertCellMainLit(titleRow, iCols[5], 1, "object", "en");
			// If there is at least one level of column group headers above the column headers,
			// the postnuc header will be in the row above this one, since it is its own group.
			VerifyCellOrPlaceholder(titleRow, isNotesOnRight ? 6 : 0, 1, "postnuc", "en", titleRowCount == 0);
		}

		private void VerifyColumnGroupTitleRow(XmlNode chartNode, int titleRowCount, bool isNotesOnRight, bool isRtl)
		{
			//<row type="title1">
			//    <cell cols="1">
			//        <main/>
			//    </cell>
			//    <cell cols="2">
			//        <main>
			//            <lit lang="en">prenuclear</lit>
			//        </main>
			//    </cell>
			// ...nucleus/3...
			//    <cell cols="1">
			//        <main>
			//            <lit lang="en">Notes</lit>
			//        </main>
			//    </cell>
			//</row>

			var rowTitle = $"title{titleRowCount - 1}";
			var titleRow = VerifyNode(rowTitle, chartNode, titleRowCount - 2, "row", 5, 1);
			AssertAttr(titleRow, "type", rowTitle);
			var iCols = ComputeColumnIndices(4, isNotesOnRight, isRtl);
			VerifyNode("num col first header", titleRow, iCols[0], "cell", 1, 1);
			AssertCellMainLit(titleRow, iCols[1], 2, "prenuclear", "en");
			AssertCellMainLit(titleRow, iCols[2], 3, "nucleus", "en");
			AssertCellMainLit(titleRow, iCols[3], 1, "postnuc", "en");
			// If there are two rows of headers, this is the first, so it contains the Notes header
			VerifyCellOrPlaceholder(titleRow, isNotesOnRight ? 4 : 0, 1, "Notes", "en", titleRowCount == 2);
		}

		private void VerifyStoryHeaderRow(XmlNode chartNode, int titleRowCount)
		{
			//<row type="title1">
			//	<cell cols="1">
			//		<main />
			//	</cell>
			//	<cell cols="6">
			//		<main>
			//			<lit lang="en">Story</lit>
			//		</main>
			//	</cell>
			//	<cell cols="1">
			//		<main>
			//			<lit lang="en">Notes</lit>
			//		</main>
			//	</cell>
			//</row>

			var rowTitle = $"title{titleRowCount - 2}";
			var titleRow = VerifyNode(rowTitle, chartNode, titleRowCount - 3, "row", 3, 1);
			AssertAttr(titleRow, "type", rowTitle);
			VerifyNode("num col story-level", titleRow, 0, "cell", 1, 1);
			AssertCellMainLit(titleRow, 1, 6, "Story", "en");
			// If there are three rows of headers, this is the first, so it contains the Notes header
			VerifyCellOrPlaceholder(titleRow, 2, 1, "Notes", "en", titleRowCount == 3);
		}

		/// <summary>
		/// Verifies a header cell that may be in the first header row, or may be a placeholder for a column whose header is in a row above
		/// </summary>
		private void VerifyCellOrPlaceholder(XmlNode parent, int index, int cCols, string inner, string lang, bool hasContent)
		{
			if (hasContent)
			{
				AssertCellMainLit(parent, index, cCols, inner, lang);
			}
			else
			{
				VerifyNode($"{inner} placeholder", parent, index, "cell", 1, 1);
			}
		}

		/// <summary>
		/// Assert that parent has a "cell" child at index index, occupying ccols columns,
		/// which has a child called "main" with one child called "lit" with innerText inner in language lang
		/// </summary>
		private void AssertCellMainLit(XmlNode parent, int index, int ccols, string inner, string lang)
		{
			AssertCellMainChild(parent, index, ccols, null, 1, "lit", inner, lang);
		}

		// Assert that parent has a 'cell' child at position index with a 'cols' attribute indicating ccols columns.
		// Furthermore, the cell has a child 'main', and if cglosses is >0 a second child 'glosses' with the specified number of children.
		// Also, the 'main' child has cchild children, the first of which has the indicated name, inner text, and lang
		// attribute.
		private static XmlNode AssertCellMainChild(XmlNode parent, int index, int ccols, string[] glosses, int cchild, string firstChildName,
			string inner, string lang)
		{
			var cell = VerifyNode("cell in title", parent, index, "cell", (glosses != null ? 2 : 1), 1);
			AssertAttr(cell, "cols", ccols.ToString());
			var main = VerifyNode("main in cell", cell, 0, "main", cchild, 0);
			if (cchild != 0)
			{
				var innerNode = VerifyNode("first child in main in cell", main, 0, firstChildName, 1, 1); // text counts as one child
				Assert.AreEqual(inner, innerNode.InnerText);
				AssertAttr(innerNode, "lang", lang);
			}
			if (glosses != null && glosses.Length != 0)
			{
				var glossesNode = VerifyNode("glosses in cell", cell, 1, "glosses", glosses.Length, 0);
				for (var i = 0; i < glosses.Length; i++)
				{
					var item = VerifyNode("gloss in glosses", glossesNode, i, "gloss", 1, 1);
					AssertAttr(item, "lang", "en");
					Assert.AreEqual(glosses[i], item.InnerText);
				}
			}
			return cell;
		}
		#endregion
	}
}
