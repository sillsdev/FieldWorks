using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.LangProj;
using System.IO;
using System.Xml;

namespace SIL.FieldWorks.Discourse
{
	/// <summary>
	/// Tests the discourse export code
	/// </summary>
	[TestFixture]
	public class DiscourseExportTests : InMemoryDiscourseTestBase
	{
		DsConstChart m_chart;
		CmPossibility m_template;
		TestCCLogic m_logic;
		ConstChartBody m_chartBody;
		ConstituentChart m_constChart;
		int[] m_firstParaWfics;
		List<int> m_allColumns;
		MockRibbon m_mockRibbon;

		public DiscourseExportTests()
		{
		}

		protected override void CreateTestData()
		{
			base.CreateTestData();
			m_firstParaWfics = m_helper.MakeAnnotations(m_firstPara);
			m_logic = new TestCCLogic(Cache, m_chart, m_stText.Hvo); // m_chart is still null!
			m_helper.Logic = m_logic;
			m_logic.Ribbon = m_mockRibbon = new MockRibbon(Cache, m_stText.Hvo);
			m_template = m_helper.MakeTemplate(out m_allColumns);
			// Note: do this AFTER creating the template, which may also create the DiscourseData object.
			m_chart = new DsConstChart();
			Cache.LangProject.DiscourseDataOA.ChartsOC.Add(m_chart);
			m_chart.TemplateRA = m_template;
			m_logic.Chart = m_chart;
			m_helper.MakeDefaultChartMarkers();
			m_helper.Chart = m_chart;

			m_constChart = new ConstituentChart(Cache, m_logic);
			m_constChart.Init(null, null);
			m_chartBody = m_constChart.Body;
			m_chartBody.Cache = Cache; // don't know why constructor doesn't do this, but it doesn't.
			m_chartBody.SetRoot(m_chart.Hvo, m_allColumns.ToArray());
		}

		// Verify some basics about a child node (and return it)
		XmlNode VerifyNode(string message, XmlNode parent, int index, string name, int childCount, int attrCount)
		{
			XmlNode child = parent.ChildNodes[index];
			Assert.IsNotNull(child, message);
			Assert.AreEqual(childCount, child.ChildNodes.Count, message);
			Assert.AreEqual(name, child.Name, message);
			Assert.AreEqual(attrCount, child.Attributes.Count, message + " attribute count");
			return child;
		}

		// Verify attribute presence (and value, if attval is not null)
		void AssertAttr(XmlNode node, string attname, string attval)
		{
			XmlAttribute attr = node.Attributes[attname];
			Assert.IsNotNull(attr, "Expected node " + node.Name + " to have attribute " + attname);
			if (attval != null)
				Assert.AreEqual(attval, attr.Value, "Expected attr " + attname + " of " + node.Name + " to have value " + attval);
		}

		#region tests

		[Test]
		public void Export()
		{
			using (MemoryStream stream = new MemoryStream())
			{
				//Set up some cells.
				int[] allParaWfics = m_helper.MakeAnnotationsUsedN(5);

				// This block makes the first row, puts CCAs in cells 1 and 2, and list refs in cells 1 and 2
				CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
				int[] movedItems = new int[] { allParaWfics[1] };
				CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
				ICmPossibility marker = m_helper.GetAMarker();
				ICmBaseAnnotation cca0_1b = m_helper.MakeMarkerAnnotation(1, row0, marker);
				CmIndirectAnnotation cca0_2 = m_helper.MakeColumnAnnotation(2, movedItems, row0);
				ICmPossibility marker2 = m_helper.GetAnotherMarker();
				ICmBaseAnnotation cca0_2b = m_helper.MakeMarkerAnnotation(2, row0, marker2);
				ICmBaseAnnotation cca0_2c = m_helper.MakeMarkerAnnotation(2, row0, marker);

				// Now another row, and cell 4 on the first has a ref to it. The new row has a CCA with two wfics in cell 1. The cell is
				// two columns wide, being merged with the previous cell.
				CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
				ICmIndirectAnnotation cca0_4 = m_helper.MakeDependentClauseMarker(row0, 4, new int[] { row1.Hvo }, "song", "2");
				CmIndirectAnnotation cca1_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[2], allParaWfics[3] }, row1);
				ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca1_1.Hvo, ConstituentChartLogic.mergeBeforeTag, true);

				// Let's have some notes on row 0.
				StText notesText = new StText();
				row0.TextOA = notesText;
				StTxtPara notesPara = new StTxtPara();
				notesText.ParagraphsOS.Append(notesPara);
				notesPara.Contents.UnderlyingTsString = Cache.MakeAnalysisTss("This is a test note");

				// And some moved text in row 1
				CmIndirectAnnotation cca1_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[4] }, row1);
				ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca1_2.Hvo,
					ConstituentChartLogic.MovedTextFeatureName, true);
				CmIndirectAnnotation cca2_3 = m_helper.MakeMovedTextAnnotation(3, cca1_2, row1, "Preposed");

				// We need four rows to properly test the variations on endPara/endSent
				CmIndirectAnnotation row2 = m_helper.MakeRow(m_chart, "2");
				ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, row2.Hvo, ConstituentChartLogic.EndSentFeatureName, true);
				CmIndirectAnnotation row3 = m_helper.MakeRow(m_chart, "3");
				ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, row3.Hvo, ConstituentChartLogic.EndParaFeatureName, true);
				ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, row3.Hvo, ConstituentChartLogic.EndSentFeatureName, true);
				CmIndirectAnnotation row4 = m_helper.MakeRow(m_chart, "4");


				XmlWriter writer = new XmlTextWriter(stream, Encoding.UTF8);
				ConstChartVc vc = new ConstChartVc(m_chartBody);
				vc.LineChoices = m_chartBody.LineChoices;
				DiscourseExporter exporter = new DiscourseExporter(m_inMemoryCache.Cache, writer, m_chart.Hvo,
					vc, m_inMemoryCache.Cache.DefaultAnalWs);
				writer.WriteStartDocument();
				writer.WriteStartElement("document");
				exporter.ExportDisplay();
				writer.WriteEndElement();
				writer.WriteEndDocument();
				writer.Flush(); // Close makes it unuseable
				stream.Position = 0;
				StreamReader reader = new StreamReader(stream, Encoding.UTF8);
				string result = reader.ReadToEnd();
				XmlDocument doc = new XmlDocument();
				doc.LoadXml(result);
				XmlNode docNode = doc.DocumentElement;
				Assert.AreEqual("document", docNode.Name);
				XmlNode chartNode = VerifyNode("chart", docNode, 0, "chart", 7, 0);
				VerifyTitleRow(chartNode);
				VerifyTitle2Row(chartNode);
				VerifyFirstDataRow(chartNode);
				VerifySecondDataRow(chartNode);
				XmlNode thirdRow = VerifyNode("row", chartNode, 4, "row", 8, 3);
				AssertAttr(thirdRow, "endSent", "true");
				XmlNode fourthRow = VerifyNode("row", chartNode, 5, "row", 8, 3);
				AssertAttr(fourthRow, "endPara", "true");


				XmlNode langNode = VerifyNode("languages", docNode, 1, "languages", 2, 0);
				XmlNode enNode = VerifyNode("english lang node", langNode, 0, "language", 0, 2);
				AssertAttr(enNode, "lang", "en");
				AssertAttr(enNode, "font", null); // don't verify exact font, may depend on installation.

			}

		}

		private void VerifySecondDataRow(XmlNode chartNode)
		{
			//<row type="song" id="1b">
			XmlNode row = VerifyNode("second row", chartNode, 3, "row", 7, 2);
			AssertAttr(row, "type", "song");
			AssertAttr(row, "id", "1b");
			//    <cell cols="1">
			//        <main>
			//            <rownum lang="en">1b</rownum>
			//        </main>
			//    </cell>
			AssertCellMainChild(row, 0, 1, null, 1, "rownum", "1b", "en");
			//    <cell reversed="true" cols="2">
			//        <main>
			//            <lit noSpaceAfter="true" lang="en">[</lit>
			//            <word lang="fr">paragraph</word>
			//            <word lang="fr">1</word>
			//        </main>
			//        <glosses>
			//            <gloss>paragraphGloss18</gloss>
			//            <gloss>1Gloss20</gloss>
			//        </glosses>
			//    </cell>
			XmlNode cell1 = VerifyNode("second node in row 2", row, 1, "cell", 2, 2);
			VerifyNode("main in 2/1", cell1, 0, "main", 3, 0);
			AssertAttr(cell1, "reversed", "true");
			AssertAttr(cell1, "cols", "2");
			AssertMainChild(cell1, 0, "lit", new string[] { "noSpaceAfter", "lang" }, new string[] { "true", "en" }, "[");
			AssertMainChild(cell1, 1, "word", new string[] { "lang" }, new string[] { "fr" }, "paragraph");
			AssertMainChild(cell1, 2, "word", new string[] { "lang" }, new string[] { "fr" }, "1");
			XmlNode glosses1 = VerifyNode("glosses cell 1/2", cell1, 1, "glosses", 2, 0);
			XmlNode gloss1_1 = VerifyNode("second gloss in 1/2", glosses1, 1, "gloss", 1, 1);
			AssertAttr(gloss1_1, "lang", "en");
			Assert.AreEqual("1Gloss20", gloss1_1.InnerText);
			//    <cell cols="1">
			//        <main>
			//            <word moved="true" lang="fr">for</word>
			//        </main>
			//        <glosses>
			//            <gloss>forGloss24</gloss>
			//        </glosses>
			//    </cell>
			XmlNode cell2 = VerifyNode("third node in row 2", row, 2, "cell", 2, 1);
			VerifyNode("main in 2/2", cell2, 0, "main", 1, 0);
			AssertMainChild(cell2, 0, "word", new string[] { "moved", "lang" }, new string[] { "true", "fr" }, "for");
			XmlNode glosses2 = VerifyNode("glosses cell 2/2", cell2, 1, "glosses", 1, 0);
			XmlNode gloss2_0 = VerifyNode("gloss in 2/2", glosses2, 0, "gloss", 1, 1);
			AssertAttr(gloss2_0, "lang", "en");
			Assert.AreEqual("forGloss24", gloss2_0.InnerText);
			//    <cell cols="1">
			//        <main>
			//            <moveMkr lang="en">Preposed</moveMkr>
			//            <lit noSpaceBefore="true" lang="en">]</lit>
			//        </main>
			//    </cell>
			XmlNode cell3 = AssertCellMainChild(row, 3, 1, null, 2, "moveMkr", "Preposed", "en");
			AssertMainChild(cell3, 1, "lit", new string[] { "noSpaceBefore", "lang" }, new string[] { "true", "en" }, "]");
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

		private void VerifyFirstDataRow(XmlNode chartNode)
		{
			//<row type="normal" id="1">
			XmlNode row = VerifyNode("first row", chartNode, 2, "row", 8, 2);
			AssertAttr(row, "type", "normal");
			AssertAttr(row, "id", "1");
			//    <cell cols="1">
			//        <main>
			//            <rownum lang="en">1</rownum>
			//        </main>
			//    </cell>
			AssertCellMainChild(row, 0, 1, null, 1, "rownum", "1", "en");
			//    <cell cols="1">
			//        <main />
			//    </cell>
			AssertCellMainChild(row, 1, 1, null, 0, null, null, null);
			//    <cell cols="1">
			//        <main>
			//            <word lang="fr">this</word>
			//            <lit noSpaceAfter="true" lang="en">(</lit>
			//            <listRef lang="en">I2</listRef>
			//            <lit noSpaceBefore="true" lang="en">)</lit>
			//        </main>
			//        <glosses>
			//            <gloss>thisGloss5</gloss>
			//        </glosses>
			//    </cell>
			XmlNode cell2 = AssertCellMainChild(row, 2, 1, new string[] { "thisGloss5" }, 4, "word", "this", "fr");
			AssertMainChild(cell2, 1, "lit", new string[] { "noSpaceAfter", "lang" }, new string[] { "true", "en" }, "(");
			AssertMainChild(cell2, 2, "listRef", new string[] { "lang" }, new string[] { "en" }, "I2");
			AssertMainChild(cell2, 3, "lit", new string[] { "noSpaceBefore", "lang" }, new string[] { "true", "en" }, ")");
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
			//            <gloss>isGloss8</gloss>
			//        </glosses>
			//    </cell>
			XmlNode cell3 = AssertCellMainChild(row, 3, 1, new string[] { "isGloss8" }, 7, "word", "is", "fr");
			//    <cell cols="1">
			//        <main />
			//    </cell>

			//    <cell cols="1">
			//        <main>
			//            <lit noSpaceAfter="true" lang="en">[</lit>
			//            <clauseMkr target="1b" lang="en">1b</clauseMkr>
			//            <lit noSpaceBefore="true" lang="en">]</lit>
			//        </main>
			//    </cell>
			XmlNode cell4 = VerifyNode("fourth node in row 0", row, 5, "cell", 1, 1);
			XmlNode main4 = VerifyNode("main in 4/1", cell4, 0, "main", 3, 0);
			AssertMainChild(cell4, 0, "lit", new string[] { "noSpaceAfter", "lang" }, new string[] { "true", "en" }, "[");
			AssertMainChild(cell4, 1, "clauseMkr", new string[] { "target", "lang" }, new string[] { "1b", "en" }, "1b");
			AssertMainChild(cell4, 2, "lit", new string[] { "noSpaceBefore", "lang" }, new string[] { "true", "en" }, "]");
			//    <cell cols="1">
			//        <main />
			//    </cell>

			//    <cell cols="1">
			//        <main>
			//            <note lang="en">This is a test note</note>
			//        </main>
			//    </cell>
			AssertCellMainChild(row, 7, 1, null, 1, "note", "This is a test note", "en");
			//</row>
		}

		/// <summary>
		/// Assert that the root node "cell" has a child "main" at index zero, which has a child
		/// at "index" with the given name, list of attributes, and inner text.
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="index"></param>
		/// <param name="name"></param>
		/// <param name="attrs"></param>
		/// <param name="vals"></param>
		/// <param name="inner"></param>
		private void AssertMainChild(XmlNode cell, int index, string name, string[] attrs, string[] vals, string inner)
		{
			XmlNode main = cell.ChildNodes[0];
			XmlNode child = VerifyNode("cell main child", main, index, name, 1, attrs.Length);
			for (int i = 0; i < attrs.Length; i++)
				AssertAttr(child, attrs[i], vals[i]);
			Assert.AreEqual(inner, child.InnerText);
		}

		private void VerifyTitle2Row(XmlNode chartNode)
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
			XmlNode titleRow = VerifyNode("title2", chartNode, 1, "row", 8, 1);
			AssertAttr(titleRow, "type", "title2");
			VerifyNode("minimal first cell", titleRow, 0, "cell", 1, 1);
			AssertCellMainLit(titleRow, 1, 1, "prenuc1", "en");
			AssertCellMainLit(titleRow, 3, 1, "Subject", "en");
		}

		// Assert that parent has a "cell" child at index index, occupying ccols columns,
		// which has a child called "main" with one child called "lit" with innerText inner
		// in language lang
		private void AssertCellMainLit(XmlNode parent, int index, int ccols, string inner, string lang)
		// Assert that parent has a "cell" child at index index, occupying ccols columns,
		// which has a child called "main" with one child called "lit" with innerText inner
		// in language lang
		{
			AssertCellMainChild(parent, index, ccols, null, 1, "lit", inner, lang);
		}

		// Assert that parent has a 'cell' child at position index with a 'cols' attribute indicating ccols columns.
		// Furthermore, the cell has a child 'main', and if cglosses is >0 a second child 'glosses' with the specified number of children.
		// Also, the 'main' child has cchild children, the first of which has the indicated name, inner text, and lang
		// attribute.
		private XmlNode AssertCellMainChild(XmlNode parent, int index, int ccols, string[] glosses, int cchild, string firstChildName,
			string inner, string lang)
		{
			XmlNode cell = VerifyNode("cell in title", parent, index, "cell", (glosses != null ? 2 : 1), 1);
			AssertAttr(cell, "cols", ccols.ToString());
			XmlNode main = VerifyNode("main in cell", cell, 0, "main", cchild, 0);
			if (cchild != 0)
			{
				XmlNode innerNode = VerifyNode("first child in main in cell", main, 0, firstChildName, 1, 1); // text counts as one child
				Assert.AreEqual(inner, innerNode.InnerText);
				AssertAttr(innerNode, "lang", lang);
			}
			if (glosses != null && glosses.Length != 0)
			{
				XmlNode glossesNode = VerifyNode("glosses in cell", cell, 1, "glosses", glosses.Length, 0);
				for (int i = 0; i < glosses.Length; i++)
				{
					XmlNode item = VerifyNode("gloss in glosses", glossesNode, i, "gloss", 1, 1);
					AssertAttr(item, "lang", "en");
					Assert.AreEqual(glosses[i], item.InnerText);
				}
			}
			return cell;
		}

		private void VerifyTitleRow(XmlNode chartNode)
		{
			//<row type="title1">
			//    <cell cols="1">
			//        <main>
			//            <lit lang="en">#</lit>
			//        </main>
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

			XmlNode titleRow = VerifyNode("title1", chartNode, 0, "row", 5, 1);
			AssertAttr(titleRow, "type", "title1");
			//XmlNode cell1 = VerifyNode("title1cell1", titleRow, 0, "cell", 1, 1);
			//Assert.AreEqual("#", cell1.InnerText);
			AssertCellMainLit(titleRow, 0, 1, "#", "en");
			AssertCellMainLit(titleRow, 1, 2, "prenuclear", "en");
			AssertCellMainLit(titleRow, 4, 1, "Notes", "en");
		}
		#endregion
	}
}
