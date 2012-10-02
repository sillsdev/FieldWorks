using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;

using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.Discourse
{
	/// <summary>
	/// Tests for the Constituent chart.
	/// </summary>
	[TestFixture]
	public class ConstituentChartDatabaseTests : InDatabaseFdoTestBase
	{
		FDO.IText m_text1 = null;
		TestCCLogic m_ccl = null;
		ITsStrFactory m_tsf = TsStrFactoryClass.Create();
		ICmPossibility m_template;
		private DsConstChart m_chart;

		public ConstituentChartDatabaseTests()
		{
		}

		const int kflidBeginObject = (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject;
		const int kflidParagraphs = (int)StText.StTextTags.kflidParagraphs;

		[SetUp]
		public override void Initialize()
		{
			MakeTextAndCcl();
			m_chart = Cache.LangProject.DiscourseDataOA.ChartsOC.Add(new DsConstChart()) as DsConstChart;
			m_ccl.Chart = m_chart;
			//SetupTexts();
		}

		internal void MakeTextAndCcl()
		{
			using (new UndoRedoTaskHelper(Cache, "ConstituentChartDatabaseTests - MakeText()", "ConstituentChartDatabaseTests - MakeText()"))
			{
				m_text1 = Cache.LangProject.TextsOC.Add(new Text());
				m_text1.ContentsOA = new StText();
				m_ccl = new TestCCLogic(Cache, m_chart, m_text1.ContentsOAHvo);
				MakeTemplate();
			}
		}

		public void SetupTexts()
		{
			// First make a regular text.
			using (new UndoRedoTaskHelper(Cache, "ConstituentChartDatabaseTests - SetupTexts()", "ConstituentChartDatabaseTests - SetupTexts()"))
			{
				m_text1 = Cache.LangProject.TextsOC.Add(new Text());
				m_text1.ContentsOA = new StText();
				StTxtPara para0 = new StTxtPara();
				m_text1.ContentsOA.ParagraphsOS.Append(para0);
				//m_text1.ContentsOA.ParagraphsOS.Append(para1);
				//           1         2         3         4         5         6
				// 0123456789012345678901234567890123456789012345678901234567890123456789
				// this is the first paragraph for our constituent chart database tests.
				para0.Contents.UnderlyingTsString = m_tsf.MakeString("this is the first paragraph for our constituent chart database tests.",
					Cache.DefaultVernWs);
			}
		}

		internal void CallGetWficCellsBorderingChOrph(int hvoChOrph, out ChartLocation precCell, out ChartLocation follCell)
		{
			int iPara, offset;
			iPara = m_ccl.CallGetWficParaIndex(hvoChOrph);
			Assert.Greater(iPara, -1, "Can't get ChOrph paragraph index.");
			offset = m_ccl.CallGetBeginOffset(hvoChOrph);
			Assert.Greater(offset, -1, "Can't get ChOrph offset.");
			m_ccl.GetWficCellsBorderingChOrph(iPara, offset, out precCell, out follCell);
		}

		StTxtPara MakeParagraph()
		{
			using (new UndoRedoTaskHelper(Cache, "ConstituentChartDatabaseTests - MakeParagraph()", "ConstituentChartDatabaseTests - MakeParagraph()"))
			{
				StTxtPara para0 = new StTxtPara();
				m_text1.ContentsOA.ParagraphsOS.Append(para0);
				int cPara = m_text1.ContentsOA.ParagraphsOS.Count;
				para0.Contents.UnderlyingTsString = m_tsf.MakeString("this is paragraph " + cPara + " for our constituent chart database tests.",
					Cache.DefaultVernWs);
				return para0;
			}
		}

		ICmBaseAnnotation MakeWfic(StTxtPara para, ICmBaseAnnotation previous)
		{
			return MakeAnnotation(para, previous, CmAnnotationDefn.Twfic(Cache));
		}

		ICmBaseAnnotation MakeAnnotation(StTxtPara para, ICmBaseAnnotation previous, ICmAnnotationDefn type)
		{
			using (new UndoRedoTaskHelper(Cache, "ConstituentChartDatabaseTests - MakeAnnotation()", "ConstituentChartDatabaseTests - MakeAnnotation()"))
			{
				ICmBaseAnnotation result = (ICmBaseAnnotation)Cache.LangProject.AnnotationsOC.Add(new CmBaseAnnotation());
				result.BeginObjectRA = para;
				int prevOffset = 0;
				if (previous != null)
					prevOffset = previous.EndOffset;
				result.BeginOffset = prevOffset + 1;
				result.EndOffset = prevOffset + 2;
				result.AnnotationTypeRA = type;
				return result;
			}
		}

		List<int> MakeNWfics(StTxtPara para, ICmBaseAnnotation previous, int n)
		{
			ICmBaseAnnotation current = previous;
			List<int> result = new List<int>(n);
			for (int i = 0; i < n; i++)
			{
				current = MakeWfic(para, current);
				result.Add(current.Hvo);
			}
			return result;
		}

		ICmIndirectAnnotation MakeCca(int[] wfics, int icol)
		{
			ICmIndirectAnnotation cca = MakeIndirectAnnotation(wfics, CmAnnotationDefn.ConstituentChartAnnotation(Cache));
			cca.InstanceOfRAHvo = m_ccl.AllMyColumns[icol];
			return cca;
		}

		ICmIndirectAnnotation MakeCca(int[] wfics)
		{
			return MakeIndirectAnnotation(wfics, CmAnnotationDefn.ConstituentChartAnnotation(Cache));
		}
		ICmIndirectAnnotation MakeRow(int[] wfics)
		{
			return MakeIndirectAnnotation(wfics, CmAnnotationDefn.ConstituentChartRow(Cache));
		}

		ICmIndirectAnnotation MakeIndirectAnnotation(int[] wfics, ICmAnnotationDefn type)
		{
			using (new UndoRedoTaskHelper(Cache, "ConstituentChartDatabaseTests - MakeIndirectAnnotation()", "ConstituentChartDatabaseTests - MakeIndirectAnnotation()"))
			{
				ICmIndirectAnnotation result = (ICmIndirectAnnotation)Cache.LangProject.AnnotationsOC.Add(new CmIndirectAnnotation());
				result.AnnotationTypeRA = type;
				foreach (int hvo in wfics)
					result.AppliesToRS.Append(hvo);
				return result;
			}
		}

		/// <summary>
		/// N.B. If actually using columns in tests, you must add the following line after creating the chart.
		/// m_ccl.Chart.TemplateRA = m_template;
		/// </summary>
		/// <returns></returns>
		public ICmPossibility MakeTemplate()
		{
			// The exact organization of columns is not of great
			// importance for the current tests (still less the names), but we do want there
			// to be a hierarchy, since that is a common problem, and naming them conventionally
			// may make debugging easier.
			m_template = m_fdoCache.LangProject.GetDefaultChartTemplate();
			return m_template;
		}

		//bool SameList(List<int> first, List<int> second)
		//{
		//    if (first.Count != second.Count)
		//        return false;
		//}

		/// <summary>
		/// No unused items in empty text
		/// </summary>
		[Test]
		public void NextUnusedInEmptyText()
		{
			const int kmaxAnnotations = 20;
			int[] result = m_ccl.NextUnchartedInput(kmaxAnnotations);
			Assert.AreEqual(0, result.Length);
		}

		/// <summary>
		/// No unused items in text with content but no annotations
		/// </summary>
		[Test]
		public void NextUnusedInUnannotatedText()
		{
			const int kmaxAnnotations = 20;
			MakeParagraph();
			int[] result = m_ccl.NextUnchartedInput(kmaxAnnotations);
			Assert.AreEqual(0, result.Length);
		}

		/// <summary>
		/// Find only annotation in text with no discourse stuff
		/// </summary>
		[Test]
		public void NextUnusedInUnchartedOneAnnotatedWordText()
		{
			const int kmaxAnnotations = 20;
			StTxtPara para = MakeParagraph();
			ICmBaseAnnotation firstWord = MakeWfic(para, null);
			int[] result = m_ccl.NextUnchartedInput(kmaxAnnotations);
			Assert.AreEqual(new int[] { firstWord.Hvo }, result);
		}

		/// <summary>
		/// Find no annotations in text with one word already used.
		/// </summary>
		[Test]
		public void NextUnusedInFullyChartedOneAnnotatedWordText()
		{
			const int kmaxAnnotations = 20;
			StTxtPara para = MakeParagraph();
			ICmBaseAnnotation firstWord = MakeWfic(para, null);
			ICmIndirectAnnotation cca = MakeCca(new int[] { firstWord.Hvo });
			ICmIndirectAnnotation row = MakeRow(new int[] { cca.Hvo });
			m_chart.RowsRS.Append(row);
			int[] result = m_ccl.NextUnchartedInput(kmaxAnnotations);
			Assert.AreEqual(new int[] { }, result);
		}

		/// <summary>
		/// Find several annotations in text with no discourse stuff
		/// </summary>
		[Test]
		public void NextUnusedInUnchartedThreeWordText()
		{
			const int kmaxAnnotations = 20;
			StTxtPara para = MakeParagraph();
			int[] expected = MakeNWfics(para, null, 3).ToArray();
			int[] result = m_ccl.NextUnchartedInput(kmaxAnnotations);
			Assert.AreEqual(expected, result);
		}
		/// <summary>
		/// Find several annotations in partly annotated text
		/// </summary>
		[Test]
		public void NextUnusedInPartlyChartedText()
		{
			const int kmaxAnnotations = 20;
			StTxtPara para = MakeParagraph();
			List<int> wfics = MakeNWfics(para, null, 5);
			ICmIndirectAnnotation cca1 = MakeCca(new int[] { wfics[0], wfics[1] });
			ICmIndirectAnnotation cca2 = MakeCca(new int[] { wfics[2] });
			ICmIndirectAnnotation row = MakeRow(new int[] { cca1.Hvo, cca2.Hvo });
			m_chart.RowsRS.Append(row);
			int[] result = m_ccl.NextUnchartedInput(kmaxAnnotations);
			Assert.AreEqual(new int[] { wfics[3], wfics[4] }, result);
		}
		/// <summary>
		/// Find no uncharted annotations in fully charted text
		/// </summary>
		[Test]
		public void NextUnusedInFullyChartedText()
		{
			const int kmaxAnnotations = 20;
			StTxtPara para = MakeParagraph();
			List<int> wfics = MakeNWfics(para, null, 5);
			ICmIndirectAnnotation cca1 = MakeCca(new int[] { wfics[0], wfics[1] });
			ICmIndirectAnnotation cca2 = MakeCca(new int[] { wfics[2] });
			ICmIndirectAnnotation cca3 = MakeCca(new int[] { wfics[3], wfics[4] });
			ICmIndirectAnnotation row1 = MakeRow(new int[] { cca1.Hvo, cca2.Hvo });
			ICmIndirectAnnotation row2 = MakeRow(new int[] { cca3.Hvo });
			m_chart.RowsRS.Append(row1);
			m_chart.RowsRS.Append(row2);
			int[] result = m_ccl.NextUnchartedInput(kmaxAnnotations);
			Assert.AreEqual(new int[0], result);
		}
		/// <summary>
		/// Find several annotations in multi-para text with no discourse stuff. Also checks length limit.
		/// Also checks that (a) base annotations of the wrong type are not returned, and
		/// (b) indirect annotations that are not CCAs don't suppress results.
		/// </summary>
		[Test]
		public void NextUnusedInUnchartedThreeParaText()
		{
			const int kmaxAnnotations = 20;
			StTxtPara para1 = MakeParagraph();
			StTxtPara para2 = MakeParagraph();
			StTxtPara para3 = MakeParagraph();
			List<int> wfics1 = MakeNWfics(para1, null, 3);
			List<int> wfics2 = MakeNWfics(para2, null, 2);
			List<int> wfics3 = MakeNWfics(para3, null, 4);

			// Make another annotation on the text that is NOT a wfic.
			MakeAnnotation(para2, CmBaseAnnotation.CreateFromDBObject(Cache, wfics2[wfics2.Count - 1]),
				CmAnnotationDefn.TextSegment(Cache));

			// And make an indirect annotation on some of them that is NOT a CCA.
			MakeIndirectAnnotation(wfics2.ToArray(), CmAnnotationDefn.Punctuation(Cache));

			List<int> expected = new List<int>(9);
			expected.AddRange(wfics1);
			expected.AddRange(wfics2);
			expected.AddRange(wfics3);
			int[] result = m_ccl.NextUnchartedInput(kmaxAnnotations);
			Assert.AreEqual(expected.ToArray(), result);

			// OK, two things in this test :-)
			int[] result2 = m_ccl.NextUnchartedInput(7);
			expected.RemoveRange(7, 2);
			Assert.AreEqual(expected.ToArray(), result2, "length limit failed");
		}
		/// <summary>
		/// Find several annotations in multi-para text with some discourse stuff.
		/// </summary>
		[Test]
		public void NextUnusedInPartlyChartedThreeParaText()
		{
			const int kmaxAnnotations = 20;
			StTxtPara para1 = MakeParagraph();
			StTxtPara para2 = MakeParagraph();
			StTxtPara para3 = MakeParagraph();
			List<int> wfics1 = MakeNWfics(para1, null, 3);
			List<int> wfics2 = MakeNWfics(para2, null, 2);
			List<int> wfics3 = MakeNWfics(para3, null, 4);
			ICmIndirectAnnotation cca1 = MakeCca(new int[] { wfics1[0], wfics1[1] });
			ICmIndirectAnnotation cca2 = MakeCca(new int[] { wfics1[2]});
			ICmIndirectAnnotation cca3 = MakeCca(new int[] { wfics2[0] });
			ICmIndirectAnnotation row = MakeRow(new int[] { cca1.Hvo, cca2.Hvo, cca3.Hvo });
			m_chart.RowsRS.Append(row);

			List<int> expected = new List<int>(5);
			expected.Add(wfics2[1]);
			expected.AddRange(wfics3);
			int[] result = m_ccl.NextUnchartedInput(kmaxAnnotations);
			Assert.AreEqual(expected.ToArray(), result);
		}
		/// <summary>
		/// Find no uncharted annotations in multi-para text fully charted.
		/// </summary>
		[Test]
		public void NextUnusedInFullyChartedThreeParaText()
		{
			const int kmaxAnnotations = 20;
			StTxtPara para1 = MakeParagraph();
			StTxtPara para2 = MakeParagraph();
			StTxtPara para3 = MakeParagraph();
			List<int> wfics1 = MakeNWfics(para1, null, 3);
			List<int> wfics2 = MakeNWfics(para2, null, 2);
			List<int> wfics3 = MakeNWfics(para3, null, 4);
			// Make CCAs that cover all the wfics so it is fully annotated.
			// This test doesn't care exactly how they are broken up into CCAs as long as everything is charted.
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			ICmIndirectAnnotation cca1 = MakeCca(new int[] { wfics1[0], wfics1[1] });
			ICmIndirectAnnotation cca2 = MakeCca(new int[] { wfics1[2] });
			ICmIndirectAnnotation cca3 = MakeCca(new int[] { wfics2[0] });
			ICmIndirectAnnotation cca4 = MakeCca(new int[] { wfics2[1] });
			ICmIndirectAnnotation cca5 = MakeCca(new int[] { wfics3[0] });
			ICmIndirectAnnotation cca6 = MakeCca(new int[] { wfics3[1], wfics3[2], wfics3[3] });
			ICmIndirectAnnotation row1 = MakeRow(new int[] { cca1.Hvo, cca2.Hvo, cca3.Hvo });
			ICmIndirectAnnotation row2 = MakeRow(new int[] { cca4.Hvo });
			ICmIndirectAnnotation row3 = MakeRow(new int[] { cca5.Hvo, cca6.Hvo });
			m_chart.RowsRS.Append(row1);
			m_chart.RowsRS.Append(row2);
			m_chart.RowsRS.Append(row3);

			int[] result = m_ccl.NextUnchartedInput(kmaxAnnotations);
			Assert.AreEqual(new int[0], result);
		}

		/// <summary>
		/// Try to find RowAnn and CCA of a Wfic, but it isn't charted.
		/// </summary>
		[Test]
		public void FindChartLocOfWfic_NotCharted()
		{
			StTxtPara para1 = MakeParagraph();
			StTxtPara para2 = MakeParagraph();
			List<int> wfics1 = MakeNWfics(para1, null, 3);
			List<int> wfics2 = MakeNWfics(para2, null, 2);
			m_ccl.Chart = new DsConstChart();
			Cache.LangProject.DiscourseDataOA.ChartsOC.Add(m_ccl.Chart); // needed for CallMakeNewRow()

			// Chart all but the last wfic (we'll test it)
			int[] chartedCcas = new int[3];
			chartedCcas[0] = MakeCca(new int[] { wfics1[0], wfics1[1] }).Hvo;
			chartedCcas[1] = MakeCca(new int[] { wfics1[2] }).Hvo;
			chartedCcas[2] = MakeCca(new int[] { wfics2[0] }).Hvo;
			ICmIndirectAnnotation row = m_ccl.CallMakeNewRow();
			foreach (int hvoCca in chartedCcas)
			{
				row.AppliesToRS.Append(hvoCca);
			}
			// wfics2[1] isn't charted

			ChartLocation result = m_ccl.FindChartLocOfWfic(wfics2[1]);
			Assert.IsNull(result);
		}

		/// <summary>
		/// Find RowAnn and CCA of a Wfic that is in the chart.
		/// </summary>
		[Test]
		public void FindChartLocOfWfic_Charted()
		{
			StTxtPara para1 = MakeParagraph();
			StTxtPara para2 = MakeParagraph();
			List<int> wfics1 = MakeNWfics(para1, null, 3);
			List<int> wfics2 = MakeNWfics(para2, null, 2);
			m_ccl.Chart = new DsConstChart();
			Cache.LangProject.DiscourseDataOA.ChartsOC.Add(m_ccl.Chart); // needed for CallMakeNewRow()
			m_ccl.Chart.TemplateRA = m_template; // needed for tests that use MakeCca() with columns

			// Chart all but the last wfic (we'll test it)
			int[] otherCcas = new int[2]; // for completeness we'll make a row for these too
			int newCca = MakeCca(new int[] { wfics1[0], wfics1[1] }, 0).Hvo;
			otherCcas[0] = MakeCca(new int[] { wfics1[2] }).Hvo;
			otherCcas[1] = MakeCca(new int[] { wfics2[0] }).Hvo;
			ICmIndirectAnnotation row0 = m_ccl.CallMakeNewRow();
			row0.AppliesToRS.Append(newCca);
			ICmIndirectAnnotation row1 = m_ccl.CallMakeNewRow();
			row1.AppliesToRS.Append(otherCcas[0]); // These two lines not strictly necessary for the test...
			row1.AppliesToRS.Append(otherCcas[1]); //       ^^^

			ChartLocation result = m_ccl.FindChartLocOfWfic(wfics1[1]);
			Assert.IsTrue(result.IsSameLocation(new ChartLocation(0, row0)));
		}

		/// <summary>
		/// Test IsChartComplete property based on NextUnchartedInput(), if chart is complete.
		/// </summary>
		[Test]
		public void IsChartComplete_Yes()
		{
			StTxtPara para1 = MakeParagraph();
			StTxtPara para2 = MakeParagraph();
			StTxtPara para3 = MakeParagraph();
			List<int> wfics1 = MakeNWfics(para1, null, 3);
			List<int> wfics2 = MakeNWfics(para2, null, 2);
			List<int> wfics3 = MakeNWfics(para3, null, 4);
			// Make CCAs that cover all the wfics so it is fully annotated.
			// This test doesn't care exactly how they are broken up into CCAs as long as everything is charted.
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			ICmIndirectAnnotation cca0 = MakeCca(new int[] { wfics1[0], wfics1[1] });
			ICmIndirectAnnotation cca1 = MakeCca(new int[] { wfics1[2] });
			ICmIndirectAnnotation cca2 = MakeCca(new int[] { wfics2[0] });
			ICmIndirectAnnotation cca3 = MakeCca(new int[] { wfics2[1] });
			ICmIndirectAnnotation cca4 = MakeCca(new int[] { wfics3[0] });
			ICmIndirectAnnotation cca5 = MakeCca(new int[] { wfics3[1], wfics3[2], wfics3[3] });
			ICmIndirectAnnotation row = MakeRow(new int[] {cca0.Hvo, cca1.Hvo, cca2.Hvo, cca3.Hvo, cca4.Hvo, cca5.Hvo});
			m_chart.RowsRS.Append(row);

			Assert.IsTrue(m_ccl.IsChartComplete, "IsChartComplete() failed.");
		}

		/// <summary>
		/// Test IsChartComplete property based on NextUnchartedInput(), if chart is NOT complete.
		/// </summary>
		[Test]
		public void IsChartComplete_No()
		{
			StTxtPara para1 = MakeParagraph();
			StTxtPara para2 = MakeParagraph();
			StTxtPara para3 = MakeParagraph();
			List<int> wfics1 = MakeNWfics(para1, null, 3);
			List<int> wfics2 = MakeNWfics(para2, null, 2);
			List<int> wfics3 = MakeNWfics(para3, null, 4);
			// Make CCAs that cover all the wfics so it is fully annotated.
			// This test doesn't care exactly how they are broken up into CCAs as long as not everything is charted.
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			MakeCca(new int[] { wfics1[0], wfics1[1] });
			MakeCca(new int[] { wfics1[2] });
			MakeCca(new int[] { wfics2[0] });
			MakeCca(new int[] { wfics2[1] });
			MakeCca(new int[] { wfics3[0] });
			//MakeCca(new int[] { wfics3[1], wfics3[2], wfics3[3] });


			Assert.IsFalse(m_ccl.IsChartComplete, "IsChartComplete() failed.");
		}

		/// <summary>
		/// Tests IsChOrph() in the case where the first annotation in the ribbon
		/// is not a ChartOrphan.
		/// </summary>
		[Test]
		public void ChOrphFalse()
		{
			StTxtPara para1 = MakeParagraph();
			StTxtPara para2 = MakeParagraph();

			List<int> wfics1 = MakeNWfics(para1, null, 3);
			List<int> wfics2 = MakeNWfics(para2, null, 4); // not going to chart all of these...

			m_ccl.Chart = new DsConstChart();
			Cache.LangProject.DiscourseDataOA.ChartsOC.Add(m_ccl.Chart); // needed for CallMakeNewRow()
			// Make CCAs that cover most of the wfics.
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			int hvoCca1 = MakeCca(new int[] { wfics1[0], wfics1[1] }).Hvo;
			int hvoCca2 = MakeCca(new int[] { wfics1[2] }).Hvo;
			int hvoCca3 = MakeCca(new int[] { wfics2[0] }).Hvo;
			int hvoCca4 = MakeCca(new int[] { wfics2[1] }).Hvo; // Leaves 2 wfics uncharted (in the ribbon)

			//This test had better not care about columns... actually I fixed that, there are some now.
			ICmIndirectAnnotation row0 = m_ccl.CallMakeNewRow();
			row0.AppliesToRS.Append(hvoCca1);
			row0.AppliesToRS.Append(hvoCca2);
			ICmIndirectAnnotation row1 = m_ccl.CallMakeNewRow();
			row1.AppliesToRS.Append(hvoCca3);
			row1.AppliesToRS.Append(hvoCca4);

			int nextUnchartedHvo = m_ccl.NextUnchartedInput(1)[0];
			Assert.AreEqual(wfics2[2], nextUnchartedHvo);
			Assert.IsFalse(m_ccl.CallIsChOrph(nextUnchartedHvo)); // this one should be in the normal AnnotationList.
		}

		/// <summary>
		/// Tests IsChOrph() in the case where the ChartOrphan is from an earlier paragraph than the last
		/// charted Wfic.
		/// </summary>
		[Test]
		public void ChOrphFromOtherPara()
		{
			StTxtPara para1 = MakeParagraph();
			StTxtPara para2 = MakeParagraph();

			List<int> wfics1 = MakeNWfics(para1, null, 3);
			List<int> wfics2 = MakeNWfics(para2, null, 4); // not going to chart all of these...

			m_ccl.Chart = new DsConstChart();
			Cache.LangProject.DiscourseDataOA.ChartsOC.Add(m_ccl.Chart); // needed for CallMakeNewRow()
			// Make CCAs that cover most of the wfics.
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			int hvoCca1 = MakeCca(new int[] { wfics1[0] }).Hvo; // We left wfics1[1] uncharted, it's a paragraph ChOrph!
			int hvoCca2 = MakeCca(new int[] { wfics1[2] }).Hvo;
			int hvoCca3 = MakeCca(new int[] { wfics2[0] }).Hvo;
			int hvoCca4 = MakeCca(new int[] { wfics2[1] }).Hvo; // Leaves 2 wfics uncharted (in the ribbon)

			//This test had better not care about columns... actually I fixed that, there are some now.
			ICmIndirectAnnotation row0 = m_ccl.CallMakeNewRow();
			row0.AppliesToRS.Append(hvoCca1);
			row0.AppliesToRS.Append(hvoCca2);
			ICmIndirectAnnotation row1 = m_ccl.CallMakeNewRow();
			row1.AppliesToRS.Append(hvoCca3);
			row1.AppliesToRS.Append(hvoCca4);

			int nextUnchartedHvo = m_ccl.NextUnchartedInput(1)[0];
			Assert.AreEqual(wfics1[1], nextUnchartedHvo);
			Assert.IsTrue(m_ccl.CallIsChOrph(nextUnchartedHvo));
		}

		/// <summary>
		/// Tests IsChOrph() in the case where the ChartOrphan is from an earlier offset than the last
		/// charted Wfic.
		/// </summary>
		[Test]
		public void ChOrphFromOffset()
		{
			StTxtPara para1 = MakeParagraph();
			StTxtPara para2 = MakeParagraph();

			List<int> wfics1 = MakeNWfics(para1, null, 3);
			List<int> wfics2 = MakeNWfics(para2, null, 4); // not going to chart all of these...

			m_ccl.Chart = new DsConstChart();
			Cache.LangProject.DiscourseDataOA.ChartsOC.Add(m_ccl.Chart); // needed for CallMakeNewRow()
			// Make CCAs that cover most of the wfics.
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			int hvoCca1 = MakeCca(new int[] { wfics1[0], wfics1[1]}).Hvo;
			int hvoCca2 = MakeCca(new int[] { wfics1[2] }).Hvo;
			//int hvoCca3 = MakeCca(new int[] { wfics2[0] }).Hvo; // We left wfics2[0] uncharted, it's an offset ChOrph!
			int hvoCca4 = MakeCca(new int[] { wfics2[1], wfics2[2] }).Hvo; // Leaves 1 wfic uncharted (in the ribbon)

			//This test had better not care about columns... actually I fixed that, there are some now.
			ICmIndirectAnnotation row0 = m_ccl.CallMakeNewRow();
			row0.AppliesToRS.Append(hvoCca1);
			row0.AppliesToRS.Append(hvoCca2);
			ICmIndirectAnnotation row1 = m_ccl.CallMakeNewRow();
			//row1.AppliesToRS.Append(hvoCca3); // Leaves this cca uncreated and its wfic uncharted
			row1.AppliesToRS.Append(hvoCca4);

			int nextUnchartedHvo = m_ccl.NextUnchartedInput(1)[0];
			Assert.AreEqual(wfics2[0], nextUnchartedHvo);
			Assert.IsTrue(m_ccl.CallIsChOrph(nextUnchartedHvo));
		}

		/// <summary>
		/// Find the Preceding and Following wfic-containing chart cells relative to a previous paragraph ChOrph.
		/// </summary>
		[Test]
		public void FindPrecFollCellsForParaChOrph()
		{
			StTxtPara para1 = MakeParagraph();
			StTxtPara para2 = MakeParagraph();

			List<int> wfics1 = MakeNWfics(para1, null, 3);
			List<int> wfics2 = MakeNWfics(para2, null, 4); // not going to chart all of these...

			m_ccl.Chart = new DsConstChart();
			Cache.LangProject.DiscourseDataOA.ChartsOC.Add(m_ccl.Chart); // needed for CallMakeNewRow()
			m_ccl.Chart.TemplateRA = m_template; // Needed for calls to MakeCca that assign columns.

			// Make CCAs that cover most of the wfics.
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			//This test cares about columns... there are some now. Use a different version of MakeCca
			int cca0_1 = MakeCca(new int[] { wfics1[0] }, 1).Hvo; // We left wfics1[1] uncharted, it's a paragraph ChOrph!
			int cca0_3 = MakeCca(new int[] { wfics1[2] }, 3).Hvo;
			int cca1_2 = MakeCca(new int[] { wfics2[0] }, 2).Hvo;
			int cca1_3 = MakeCca(new int[] { wfics2[1] }, 3).Hvo; // Leaves 2 wfics uncharted (in the ribbon)

			ICmIndirectAnnotation row0 = m_ccl.CallMakeNewRow();
			row0.AppliesToRS.Append(cca0_1);
			row0.AppliesToRS.Append(cca0_3);
			ICmIndirectAnnotation row1 = m_ccl.CallMakeNewRow();
			row1.AppliesToRS.Append(cca1_2);
			row1.AppliesToRS.Append(cca1_3);

			ChartLocation result1, result2;
			ChartLocation precCell = m_ccl.MakeLocObj(1, row0);
			ChartLocation follCell = m_ccl.MakeLocObj(3, row0);

			CallGetWficCellsBorderingChOrph(wfics1[1],
				out result1, out result2);

			// Test results
			Assert.IsTrue(precCell.IsSameLocation(result1));
			Assert.IsTrue(follCell.IsSameLocation(result2));
		}

		/// <summary>
		/// Find the Preceding and Following wfic-containing chart cells relative to a previous offset ChOrph.
		/// </summary>
		[Test]
		public void FindPrecFollCellsForOffsetChOrph()
		{
			StTxtPara para1 = MakeParagraph();
			StTxtPara para2 = MakeParagraph();

			List<int> wfics1 = MakeNWfics(para1, null, 3);
			List<int> wfics2 = MakeNWfics(para2, null, 4); // not going to chart all of these...

			m_ccl.Chart = new DsConstChart();
			Cache.LangProject.DiscourseDataOA.ChartsOC.Add(m_ccl.Chart); // needed for CallMakeNewRow()
			m_ccl.Chart.TemplateRA = m_template; // Needed for calls to MakeCca that assign columns.

			// Make CCAs that cover most of the wfics.
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			//This test cares about columns... there are some now. Use a different version of MakeCca
			int cca0_1 = MakeCca(new int[] { wfics1[0], wfics1[1] }, 1).Hvo;
			int cca0_3 = MakeCca(new int[] { wfics1[2] }, 3).Hvo;
			//int cca1_2 = MakeCca(new int[] { wfics2[0] }, 2).Hvo; // We left wfics2[0] uncharted, it's an offset ChOrph!
			int cca1_3 = MakeCca(new int[] { wfics2[1], wfics2[2] }, 3).Hvo; // Leaves 1 wfic uncharted (in the ribbon)

			ICmIndirectAnnotation row0 = m_ccl.CallMakeNewRow();
			row0.AppliesToRS.Append(cca0_1);
			row0.AppliesToRS.Append(cca0_3);
			ICmIndirectAnnotation row1 = m_ccl.CallMakeNewRow();
			//row1.AppliesToRS.Append(cca1_2);
			row1.AppliesToRS.Append(cca1_3);

			ChartLocation result1, result2;
			ChartLocation precCell = m_ccl.MakeLocObj(3, row0);
			ChartLocation follCell = m_ccl.MakeLocObj(3, row1);

			CallGetWficCellsBorderingChOrph(wfics2[0], out result1, out result2);

			// Test results
			Assert.IsTrue(precCell.IsSameLocation(result1));
			Assert.IsTrue(follCell.IsSameLocation(result2));
		}

		/// <summary>
		/// Find the Preceding and Following wfic-containing chart cells relative to a previous offset ChOrph,
		/// for the case where the preceding/following cells are the same cell.
		/// </summary>
		[Test]
		public void FindPrecFollCellsForOffsetChOrph_SameCell()
		{
			StTxtPara para1 = MakeParagraph();
			StTxtPara para2 = MakeParagraph();

			List<int> wfics1 = MakeNWfics(para1, null, 3);
			List<int> wfics2 = MakeNWfics(para2, null, 5); // not going to chart all of these...

			m_ccl.Chart = new DsConstChart();
			Cache.LangProject.DiscourseDataOA.ChartsOC.Add(m_ccl.Chart); // needed for CallMakeNewRow()
			m_ccl.Chart.TemplateRA = m_template; // Needed for calls to MakeCca that assign columns.

			// Make CCAs that cover most of the wfics.
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			//This test cares about columns... there are some now. Use a different version of MakeCca
			int cca0_1 = MakeCca(new int[] { wfics1[0], wfics1[1] }, 1).Hvo;
			int cca0_3 = MakeCca(new int[] { wfics1[2] }, 3).Hvo;
			int cca1_2 = MakeCca(new int[] { wfics2[0], wfics2[2] }, 2).Hvo; // Left wfics2[1] uncharted, offset ChOrph!
			int cca1_3 = MakeCca(new int[] { wfics2[3] }, 3).Hvo; // Leaves 1 wfic uncharted (in the ribbon)

			ICmIndirectAnnotation row0 = m_ccl.CallMakeNewRow();
			row0.AppliesToRS.Append(cca0_1);
			row0.AppliesToRS.Append(cca0_3);
			ICmIndirectAnnotation row1 = m_ccl.CallMakeNewRow();
			row1.AppliesToRS.Append(cca1_2);
			row1.AppliesToRS.Append(cca1_3);

			ChartLocation result1, result2;
			ChartLocation precCell = m_ccl.MakeLocObj(2, row1);
			ChartLocation follCell = m_ccl.MakeLocObj(2, row1);

			CallGetWficCellsBorderingChOrph(wfics2[1],
				out result1, out result2);

			// Test results
			Assert.IsTrue(precCell.IsSameLocation(result1));
			Assert.IsTrue(follCell.IsSameLocation(result2));
		}

		/// <summary>
		/// Find the Preceding and Following wfic-containing chart cells relative to a previous offset ChOrph,
		/// for the case where the preceding/following cells are the same cell and in the first row. Re: part of [LT-8380]
		/// </summary>
		[Test]
		public void FindPrecFollCellsForOffsetChOrph_SameCellFirstRow()
		{
			StTxtPara para1 = MakeParagraph();
			StTxtPara para2 = MakeParagraph();

			List<int> wfics1 = MakeNWfics(para1, null, 5);
			List<int> wfics2 = MakeNWfics(para2, null, 3); // not going to chart all of these...

			m_ccl.Chart = new DsConstChart();
			Cache.LangProject.DiscourseDataOA.ChartsOC.Add(m_ccl.Chart); // needed for CallMakeNewRow()
			m_ccl.Chart.TemplateRA = m_template; // Needed for calls to MakeCca that assign columns.

			// Make CCAs that cover most of the wfics.
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			// This test cares about columns... there are some now. Use a different version of MakeCca
			int cca0_1 = MakeCca(new int[] { wfics1[0], wfics1[1] }, 1).Hvo;
			int cca0_2 = MakeCca(new int[] { wfics1[2], wfics1[4] }, 2).Hvo; // Left wfics1[3] uncharted, offset ChOrph!
			//int cca1_2 = MakeCca(new int[] { wfics2[0], wfics2[2] }, 2).Hvo; // Left wfics2[1] uncharted, offset ChOrph!
			//int cca1_3 = MakeCca(new int[] { wfics2[3] }, 3).Hvo; // Leaves 1 wfic uncharted (in the ribbon)

			ICmIndirectAnnotation row0 = m_ccl.CallMakeNewRow();
			row0.AppliesToRS.Append(cca0_1);
			row0.AppliesToRS.Append(cca0_2);
			//ICmIndirectAnnotation row1 = m_ccl.CallMakeNewRow();
			//row1.AppliesToRS.Append(cca1_2);
			//row1.AppliesToRS.Append(cca1_3);

			ChartLocation result1, result2;
			ChartLocation precCell = m_ccl.MakeLocObj(2, row0);
			ChartLocation follCell = m_ccl.MakeLocObj(2, row0);

			CallGetWficCellsBorderingChOrph(wfics1[3],
				out result1, out result2);

			// Test results
			Assert.IsTrue(precCell.IsSameLocation(result1));
			Assert.IsTrue(follCell.IsSameLocation(result2));
		}

		/// <summary>
		/// Find the Preceding and Following wfic-containing chart cells relative to a previous offset ChOrph,
		/// for the case where there's an empty row between the preceding and following cells.
		/// </summary>
		[Test]
		public void FindPrecFollCellsForOffsetChOrph_EmptyRow()
		{
			StTxtPara para1 = MakeParagraph();
			StTxtPara para2 = MakeParagraph();

			List<int> wfics1 = MakeNWfics(para1, null, 3);
			List<int> wfics2 = MakeNWfics(para2, null, 5); // not going to chart all of these...

			m_ccl.Chart = new DsConstChart();
			Cache.LangProject.DiscourseDataOA.ChartsOC.Add(m_ccl.Chart); // needed for CallMakeNewRow()
			m_ccl.Chart.TemplateRA = m_template; // Needed for calls to MakeCca that assign columns.

			// Make CCAs that cover most of the wfics.
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			//This test cares about columns... there are some now. Use a different version of MakeCca
			int cca0_1 = MakeCca(new int[] { wfics1[0], wfics1[1] }, 1).Hvo;
			int cca0_3 = MakeCca(new int[] { wfics1[2] }, 3).Hvo;
			int cca1_0 = MakeCca(new int[] { wfics2[0] }, 0).Hvo;
			int cca1_2 = MakeCca(new int[] { wfics2[1] }, 2).Hvo; // Left wfics2[2] uncharted, offset ChOrph!
			int cca3_1 = MakeCca(new int[] { wfics2[3] }, 1).Hvo; // Leaves 1 wfic uncharted (in the ribbon)

			ICmIndirectAnnotation row0 = m_ccl.CallMakeNewRow();
			row0.AppliesToRS.Append(cca0_1);
			row0.AppliesToRS.Append(cca0_3);
			ICmIndirectAnnotation row1 = m_ccl.CallMakeNewRow();
			row1.AppliesToRS.Append(cca1_0);
			row1.AppliesToRS.Append(cca1_2);
			ICmIndirectAnnotation row2 = m_ccl.CallMakeNewRow(); // blank row (or at least no Wfics)
			ICmIndirectAnnotation row3 = m_ccl.CallMakeNewRow();
			row3.AppliesToRS.Append(cca3_1);

			ChartLocation result1, result2;
			ChartLocation precCell = m_ccl.MakeLocObj(2, row1);
			ChartLocation follCell = m_ccl.MakeLocObj(1, row3);

			CallGetWficCellsBorderingChOrph(wfics2[2],
				out result1, out result2);

			// Test results
			Assert.IsTrue(precCell.IsSameLocation(result1));
			Assert.IsTrue(follCell.IsSameLocation(result2));
		}

		/// <summary>
		/// Find the Preceding and Following wfic-containing chart cells relative to a previous paragraph ChOrph,
		/// for the case where there are no wfics before the ChOrph (if no wfics after, it isn't a ChOrph!).
		/// </summary>
		[Test]
		public void FindPrecFollCellsForParaChOrph_NothingBefore()
		{
			StTxtPara para1 = MakeParagraph();
			StTxtPara para2 = MakeParagraph();

			List<int> wfics1 = MakeNWfics(para1, null, 3);
			List<int> wfics2 = MakeNWfics(para2, null, 5); // not going to chart all of these...

			m_ccl.Chart = new DsConstChart();
			Cache.LangProject.DiscourseDataOA.ChartsOC.Add(m_ccl.Chart); // needed for CallMakeNewRow()
			m_ccl.Chart.TemplateRA = m_template; // Needed for calls to MakeCca that assign columns.

			// Make CCAs that cover most of the wfics.
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			//This test cares about columns... there are some now. Use a different version of MakeCca
			int cca0_1 = MakeCca(new int[] { wfics1[1] }, 1).Hvo; // Left wfics1[0] uncharted, ChOrph w/no Wfic before
			int cca0_3 = MakeCca(new int[] { wfics1[2] }, 3).Hvo;
			int cca1_2 = MakeCca(new int[] { wfics2[0], wfics2[1], wfics2[2] }, 2).Hvo;
			int cca1_3 = MakeCca(new int[] { wfics2[3] }, 3).Hvo; // Leaves 1 wfic uncharted (in the ribbon)

			ICmIndirectAnnotation row0 = m_ccl.CallMakeNewRow();
			row0.AppliesToRS.Append(cca0_1);
			row0.AppliesToRS.Append(cca0_3);
			ICmIndirectAnnotation row1 = m_ccl.CallMakeNewRow();
			row1.AppliesToRS.Append(cca1_2);
			row1.AppliesToRS.Append(cca1_3);

			ChartLocation result1, result2;
			ChartLocation precCell = m_ccl.MakeLocObj(0, row0);
			ChartLocation follCell = m_ccl.MakeLocObj(1, row0);

			CallGetWficCellsBorderingChOrph(wfics1[0],
				out result1, out result2);

			// Test results
			Assert.IsTrue(precCell.IsSameLocation(result1));
			Assert.IsTrue(follCell.IsSameLocation(result2));
		}

		/// <summary>
		/// Test FindWhereToAddChOrph() in the case where there is a CCA and it has wfics from multiple paragraphs.
		/// In this case, there is an earlier paragraph as well as same paragraph wfics.
		/// </summary>
		[Test]
		public void FindWhereAddChOrph_InsertAfterEarlierPara()
		{
			StTxtPara para1 = MakeParagraph();
			StTxtPara para2 = MakeParagraph();

			List<int> wfics1 = MakeNWfics(para1, null, 3);
			List<int> wfics2 = MakeNWfics(para2, null, 5); // not going to chart all of these...

			m_ccl.Chart = new DsConstChart();
			Cache.LangProject.DiscourseDataOA.ChartsOC.Add(m_ccl.Chart); // needed for CallMakeNewRow()
			m_ccl.Chart.TemplateRA = m_template; // Needed for calls to MakeCca that assign columns.

			// Make CCAs that cover most of the wfics.
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			//This test cares about columns... there are some now. Use a different version of MakeCca
			int cca0_1 = MakeCca(new int[] { wfics1[0], wfics1[1] }, 1).Hvo;
			int cca0_2 = MakeCca(new int[] { wfics1[2], wfics2[0], wfics2[2] }, 2).Hvo; // wfics2[1] is ChOrph
			int cca1_3 = MakeCca(new int[] { wfics2[3] }, 3).Hvo; // Leaves 1 wfic uncharted (in the ribbon)

			ICmIndirectAnnotation row0 = m_ccl.CallMakeNewRow();
			row0.AppliesToRS.Append(cca0_1);
			row0.AppliesToRS.Append(cca0_2);
			ICmIndirectAnnotation row1 = m_ccl.CallMakeNewRow();
			row1.AppliesToRS.Append(cca1_3);
			ChartLocation testCell = m_ccl.MakeLocObj(2, row0);

			int whereToInsertActual;
			ICmIndirectAnnotation existingCcaActual;

			// SUT; icol of CCA in question = 2, iPara of ChOrph = 1
			ConstituentChartLogic.FindWhereToAddResult result = m_ccl.FindWhereToAddChOrph(testCell, 1,
				m_ccl.CallGetBeginOffset(wfics2[1]), out whereToInsertActual, out existingCcaActual);

			// Test results
			Assert.AreEqual(ConstituentChartLogic.FindWhereToAddResult.kInsertChOrphInCCA, result);
			Assert.AreEqual(2, whereToInsertActual);
			Assert.AreEqual(cca0_2, existingCcaActual.Hvo);
		}

		/// <summary>
		/// Test FindWhereToAddChOrph() in the case where there is a CCA and it has wfics from multiple paragraphs.
		/// In this case, there is a later paragraph after some same paragraph wfics.
		/// </summary>
		[Test]
		public void FindWhereAddChOrph_InsertBeforeLaterWithMultipleParas()
		{
			StTxtPara para1 = MakeParagraph();
			StTxtPara para2 = MakeParagraph();

			List<int> wfics1 = MakeNWfics(para1, null, 5);
			List<int> wfics2 = MakeNWfics(para2, null, 5); // not going to chart all of these...

			m_ccl.Chart = new DsConstChart();
			Cache.LangProject.DiscourseDataOA.ChartsOC.Add(m_ccl.Chart); // needed for CallMakeNewRow()
			m_ccl.Chart.TemplateRA = m_template; // Needed for calls to MakeCca that assign columns.

			// Make CCAs that cover most of the wfics.
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			//This test cares about columns... there are some now. Use a different version of MakeCca
			int cca0_1 = MakeCca(new int[] { wfics1[0] }, 1).Hvo;
			int cca1_0 = MakeCca(new int[] { wfics1[1], wfics1[4], wfics2[0] }, 0).Hvo; // wfics1[2&3] are ChOrphs
			int cca1_3 = MakeCca(new int[] { wfics2[1], wfics2[2] }, 3).Hvo; // Leaves 2 wfics uncharted (in the ribbon)

			ICmIndirectAnnotation row0 = m_ccl.CallMakeNewRow();
			row0.AppliesToRS.Append(cca0_1);
			//row0.AppliesToRS.Append(cca0_2);
			ICmIndirectAnnotation row1 = m_ccl.CallMakeNewRow();
			row1.AppliesToRS.Append(cca1_0);
			row1.AppliesToRS.Append(cca1_3);

			ChartLocation testCell = m_ccl.MakeLocObj(0, row1);

			int whereToInsertActual;
			ICmIndirectAnnotation existingCcaActual;

			// SUT; icol of CCA in question = 0, iPara of ChOrph = 0
			ConstituentChartLogic.FindWhereToAddResult result = m_ccl.FindWhereToAddChOrph(testCell, 0,
				m_ccl.CallGetBeginOffset(wfics1[2]), out whereToInsertActual, out existingCcaActual);

			// Test results
			Assert.AreEqual(ConstituentChartLogic.FindWhereToAddResult.kInsertChOrphInCCA, result);
			Assert.AreEqual(1, whereToInsertActual);
			Assert.AreEqual(cca1_0, existingCcaActual.Hvo);
		}

		/// <summary>
		/// Test FindWhereToAddChOrph() in the case where there is a CCA and it has wfics from multiple paragraphs.
		/// In this case, there are earlier and later paragraph wfics in the target CCA.
		/// </summary>
		[Test]
		public void FindWhereAddChOrph_InsertBetwnOtherParas()
		{
			StTxtPara para1 = MakeParagraph();
			StTxtPara para2 = MakeParagraph();
			StTxtPara para3 = MakeParagraph();

			List<int> wfics1 = MakeNWfics(para1, null, 3);
			List<int> wfics2 = MakeNWfics(para2, null, 2); // these will both be ChOrphs
			List<int> wfics3 = MakeNWfics(para3, null, 4); // not going to chart all of these...

			m_ccl.Chart = new DsConstChart();
			Cache.LangProject.DiscourseDataOA.ChartsOC.Add(m_ccl.Chart); // needed for CallMakeNewRow()
			m_ccl.Chart.TemplateRA = m_template; // Needed for calls to MakeCca that assign columns.

			// Make CCAs that cover most of the wfics.
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			//This test cares about columns... there are some now. Use a different version of MakeCca
			int cca0_0 = MakeCca(new int[] { wfics1[0], wfics1[1] }, 0).Hvo;
			int cca0_1 = MakeCca(new int[] { wfics1[2], wfics3[0] }, 1).Hvo; // wfics2[0&1] are ChOrphs
			int cca1_0 = MakeCca(new int[] { wfics3[1] }, 0).Hvo;
			int cca1_3 = MakeCca(new int[] { wfics3[2] }, 3).Hvo; // Leaves 1 wfic uncharted (in the ribbon)

			ICmIndirectAnnotation row0 = m_ccl.CallMakeNewRow();
			row0.AppliesToRS.Append(cca0_0);
			row0.AppliesToRS.Append(cca0_1);
			ICmIndirectAnnotation row1 = m_ccl.CallMakeNewRow();
			row1.AppliesToRS.Append(cca1_0);
			row1.AppliesToRS.Append(cca1_3);

			ChartLocation testCell = m_ccl.MakeLocObj(1, row0);

			int whereToInsertActual;
			ICmIndirectAnnotation existingCcaActual;

			// SUT; icol of CCA in question = 1, iPara of ChOrph = 1
			ConstituentChartLogic.FindWhereToAddResult result = m_ccl.FindWhereToAddChOrph(testCell, 1,
				m_ccl.CallGetBeginOffset(wfics2[0]), out whereToInsertActual, out existingCcaActual);

			// Test results
			Assert.AreEqual(ConstituentChartLogic.FindWhereToAddResult.kInsertChOrphInCCA, result);
			Assert.AreEqual(1, whereToInsertActual);
			Assert.AreEqual(cca0_1, existingCcaActual.Hvo);
		}

		/// <summary>
		/// Test FindWhereToAddChOrph() in the case where there is a CCA and it has wfics from an earlier paragraph.
		/// </summary>
		[Test]
		public void FindWhereAddChOrph_AppendByPara()
		{
			StTxtPara para1 = MakeParagraph();
			StTxtPara para2 = MakeParagraph();
			//StTxtPara para3 = MakeParagraph();

			List<int> wfics1 = MakeNWfics(para1, null, 3);
			List<int> wfics2 = MakeNWfics(para2, null, 3); // the first of these will  be the ChOrph
			//List<int> wfics3 = MakeNWfics(para3, null, 4); // not going to chart all of these...

			m_ccl.Chart = new DsConstChart();
			Cache.LangProject.DiscourseDataOA.ChartsOC.Add(m_ccl.Chart); // needed for CallMakeNewRow()
			m_ccl.Chart.TemplateRA = m_template; // Needed for calls to MakeCca that assign columns.

			// Make CCAs that cover most of the wfics.
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			//This test cares about columns... there are some now. Use a different version of MakeCca
			int cca0_0 = MakeCca(new int[] { wfics1[0] }, 1).Hvo;
			int cca0_2 = MakeCca(new int[] { wfics1[1], wfics1[2] }, 2).Hvo; // wfics2[0] is the ChOrph
			int cca1_1 = MakeCca(new int[] { wfics2[1] }, 0).Hvo; // Leaves 1 wfic uncharted (in the ribbon)
			//int cca1_3 = MakeCca(new int[] { wfics3[2] }, 3).Hvo;

			ICmIndirectAnnotation row0 = m_ccl.CallMakeNewRow();
			row0.AppliesToRS.Append(cca0_0);
			row0.AppliesToRS.Append(cca0_2);
			ICmIndirectAnnotation row1 = m_ccl.CallMakeNewRow();
			row1.AppliesToRS.Append(cca1_1);

			ChartLocation testCell = m_ccl.MakeLocObj(2, row0);

			int whereToInsertActual;
			ICmIndirectAnnotation existingCcaActual;

			// SUT; icol of CCA in question = 2, iPara of ChOrph = 1
			ConstituentChartLogic.FindWhereToAddResult result = m_ccl.FindWhereToAddChOrph(testCell, 1,
				m_ccl.CallGetBeginOffset(wfics2[0]), out whereToInsertActual, out existingCcaActual);

			// Test results
			Assert.AreEqual(ConstituentChartLogic.FindWhereToAddResult.kAppendToExisting, result);
			Assert.AreEqual(2, whereToInsertActual);
			Assert.AreEqual(cca0_2, existingCcaActual.Hvo);
		}

		/// <summary>
		/// Test FindWhereToAddChOrph() in the case where there is a CCA and it only has wfics from a later paragraph.
		/// </summary>
		[Test]
		public void FindWhereAddChOrph_InsertBeforeLaterPara()
		{
			StTxtPara para1 = MakeParagraph();
			StTxtPara para2 = MakeParagraph();

			List<int> wfics1 = MakeNWfics(para1, null, 5);
			List<int> wfics2 = MakeNWfics(para2, null, 5); // not going to chart all of these...

			m_ccl.Chart = new DsConstChart();
			Cache.LangProject.DiscourseDataOA.ChartsOC.Add(m_ccl.Chart); // needed for CallMakeNewRow()
			m_ccl.Chart.TemplateRA = m_template; // Needed for calls to MakeCca that assign columns.

			// Make CCAs that cover most of the wfics.
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			//This test cares about columns... there are some now. Use a different version of MakeCca
			int cca0_1 = MakeCca(new int[] { wfics1[0], wfics1[1] }, 1).Hvo;
			int cca0_2 = MakeCca(new int[] { wfics1[2], wfics1[3] }, 2).Hvo;
			int cca1_0 = MakeCca(new int[] { wfics2[0], wfics2[1] }, 0).Hvo; // wfics1[4] is the ChOrph
			int cca1_3 = MakeCca(new int[] { wfics2[2] }, 3).Hvo; // Leaves 2 wfics uncharted (in the ribbon)

			ICmIndirectAnnotation row0 = m_ccl.CallMakeNewRow();
			row0.AppliesToRS.Append(cca0_1);
			row0.AppliesToRS.Append(cca0_2);
			ICmIndirectAnnotation row1 = m_ccl.CallMakeNewRow();
			row1.AppliesToRS.Append(cca1_0);
			row1.AppliesToRS.Append(cca1_3);

			ChartLocation testCell = m_ccl.MakeLocObj(0, row1);

			int whereToInsertActual;
			ICmIndirectAnnotation existingCcaActual;

			// SUT; icol of CCA in question = 0, iPara of ChOrph = 0
			ConstituentChartLogic.FindWhereToAddResult result = m_ccl.FindWhereToAddChOrph(testCell, 0,
				m_ccl.CallGetBeginOffset(wfics1[4]), out whereToInsertActual, out existingCcaActual);

			// Test results
			Assert.AreEqual(ConstituentChartLogic.FindWhereToAddResult.kInsertChOrphInCCA, result);
			Assert.AreEqual(0, whereToInsertActual);
			Assert.AreEqual(cca1_0, existingCcaActual.Hvo);
		}

		/// <summary>
		/// Test FindWhereToAddChOrph() in the case where there is NO CCA in the specified cell.
		/// </summary>
		[Test]
		public void FindWhereAddChOrph_InsertNewCCA()
		{
			StTxtPara para1 = MakeParagraph();

			List<int> wfics1 = MakeNWfics(para1, null, 5);

			m_ccl.Chart = new DsConstChart();
			Cache.LangProject.DiscourseDataOA.ChartsOC.Add(m_ccl.Chart); // needed for CallMakeNewRow()
			m_ccl.Chart.TemplateRA = m_template; // Needed for calls to MakeCca that assign columns.

			// Make CCAs that cover most of the wfics.
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			//This test cares about columns... there are some now. Use a different version of MakeCca
			int cca0_1 = MakeCca(new int[] { wfics1[0], wfics1[1] }, 1).Hvo; // wfics1[2] will be ChOrph
			// This tests what happens if we try to put the ChOrph in column index 3.
			int cca0_4 = MakeCca(new int[] { wfics1[3], wfics1[4] }, 4).Hvo; // leaves nothing in Ribbon, except ChOrph

			ICmIndirectAnnotation row0 = m_ccl.CallMakeNewRow();
			row0.AppliesToRS.Append(cca0_1);
			row0.AppliesToRS.Append(cca0_4);

			ChartLocation testCell = m_ccl.MakeLocObj(3, row0);

			int whereToInsertActual;
			ICmIndirectAnnotation existingCcaActual;

			// SUT; icol of CCA in question = 3, iPara of ChOrph = 0
			ConstituentChartLogic.FindWhereToAddResult result = m_ccl.FindWhereToAddChOrph(testCell, 0,
				m_ccl.CallGetBeginOffset(wfics1[2]), out whereToInsertActual, out existingCcaActual);

			// Test results
			Assert.AreEqual(ConstituentChartLogic.FindWhereToAddResult.kInsertCcaInSameRow, result);
			Assert.AreEqual(1, whereToInsertActual); // index in Row.AppliesTo!
			Assert.IsNull(existingCcaActual);
		}

		/// <summary>
		/// Test FindWhereToAddChOrph() in the case where there are multiple(2) CCAs in the specified cell
		/// and they surround the text-logical ChOrph location. Should append to first CCA.
		/// Actually this is somewhat arbitrary. We could just as well insert it at the beginning of the second.
		/// </summary>
		[Test]
		public void FindWhereAddChOrph_MultiDataCCAs_SurroundLoc()
		{
			StTxtPara para1 = MakeParagraph();

			List<int> wfics1 = MakeNWfics(para1, null, 5);

			m_ccl.Chart = new DsConstChart();
			Cache.LangProject.DiscourseDataOA.ChartsOC.Add(m_ccl.Chart); // needed for CallMakeNewRow()
			m_ccl.Chart.TemplateRA = m_template; // Needed for calls to MakeCca that assign columns.

			// Make CCAs that cover most of the wfics.
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			//This test cares about columns... there are some now. Use a different version of MakeCca
			int cca0_1 = MakeCca(new int[] { wfics1[0], wfics1[1] }, 1).Hvo; // wfics1[2] will be ChOrph
			int cca0_1b = MakeCca(new int[] { wfics1[3] }, 1).Hvo;
			// This tests what happens if we try to put the ChOrph in column index 1.
			int cca0_4 = MakeCca(new int[] { wfics1[4] }, 4).Hvo; // leaves nothing in Ribbon, except ChOrph

			ICmIndirectAnnotation row0 = m_ccl.CallMakeNewRow();
			row0.AppliesToRS.Append(cca0_1);
			row0.AppliesToRS.Append(cca0_1b);
			row0.AppliesToRS.Append(cca0_4);

			ChartLocation testCell = m_ccl.MakeLocObj(1, row0);

			int whereToInsertActual;
			ICmIndirectAnnotation existingCcaActual;

			// SUT; icol of CCA in question = 1, iPara of ChOrph = 0
			ConstituentChartLogic.FindWhereToAddResult result = m_ccl.FindWhereToAddChOrph(testCell, 0,
				m_ccl.CallGetBeginOffset(wfics1[2]), out whereToInsertActual, out existingCcaActual);

			// Test results
			Assert.AreEqual(ConstituentChartLogic.FindWhereToAddResult.kAppendToExisting, result);
			Assert.AreEqual(cca0_1, existingCcaActual.Hvo);
		}

		/// <summary>
		/// Test FindWhereToAddChOrph() in the case where there are multiple(2) CCAs in the specified cell
		/// and they occur before the text-logical ChOrph location. Should append to second CCA.
		/// </summary>
		[Test]
		public void FindWhereAddChOrph_MultiDataCCAs_BeforeLoc()
		{
			StTxtPara para1 = MakeParagraph();

			List<int> wfics1 = MakeNWfics(para1, null, 5);

			m_ccl.Chart = new DsConstChart();
			Cache.LangProject.DiscourseDataOA.ChartsOC.Add(m_ccl.Chart); // needed for CallMakeNewRow()
			m_ccl.Chart.TemplateRA = m_template; // Needed for calls to MakeCca that assign columns.

			// Make CCAs that cover most of the wfics.
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			//This test cares about columns... there are some now. Use a different version of MakeCca
			int cca0_1 = MakeCca(new int[] { wfics1[0], wfics1[1] }, 1).Hvo;
			int cca0_1b = MakeCca(new int[] { wfics1[2] }, 1).Hvo; // wfics1[3] will be ChOrph
			// This tests what happens if we try to put the ChOrph in column index 1.
			int cca0_4 = MakeCca(new int[] { wfics1[4] }, 4).Hvo; // leaves nothing in Ribbon, except ChOrph

			ICmIndirectAnnotation row0 = m_ccl.CallMakeNewRow();
			row0.AppliesToRS.Append(cca0_1);
			row0.AppliesToRS.Append(cca0_1b);
			row0.AppliesToRS.Append(cca0_4);

			ChartLocation testCell = m_ccl.MakeLocObj(1, row0);

			int whereToInsertActual;
			ICmIndirectAnnotation existingCcaActual;

			// SUT; icol of CCA in question = 1, iPara of ChOrph = 0
			ConstituentChartLogic.FindWhereToAddResult result = m_ccl.FindWhereToAddChOrph(testCell, 0,
				m_ccl.CallGetBeginOffset(wfics1[3]), out whereToInsertActual, out existingCcaActual);

			// Test results
			Assert.AreEqual(ConstituentChartLogic.FindWhereToAddResult.kAppendToExisting, result);
			Assert.AreEqual(cca0_1b, existingCcaActual.Hvo);
			Assert.AreEqual(1, whereToInsertActual); // not used, but it should be this value anyway.
		}

		/// <summary>
		/// Test FindWhereToAddChOrph() in the case where there are multiple(2) CCAs in the specified cell
		/// and they occur after the text-logical ChOrph location. Should insert at beginning of first CCA.
		/// </summary>
		[Test]
		public void FindWhereAddChOrph_MultiDataCCAs_AfterLoc()
		{
			StTxtPara para1 = MakeParagraph();

			List<int> wfics1 = MakeNWfics(para1, null, 5);

			m_ccl.Chart = new DsConstChart();
			Cache.LangProject.DiscourseDataOA.ChartsOC.Add(m_ccl.Chart); // needed for CallMakeNewRow()
			m_ccl.Chart.TemplateRA = m_template; // Needed for calls to MakeCca that assign columns.

			// Make CCAs that cover most of the wfics.
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			//This test cares about columns... there are some now. Use a different version of MakeCca
			int cca0_1 = MakeCca(new int[] { wfics1[1] }, 1).Hvo; // wfics1[0] will be ChOrph
			int cca0_1b = MakeCca(new int[] { wfics1[2], wfics1[3] }, 1).Hvo;
			// This tests what happens if we try to put the ChOrph in column index 1.
			int cca0_4 = MakeCca(new int[] { wfics1[4] }, 4).Hvo; // leaves nothing in Ribbon, except ChOrph

			ICmIndirectAnnotation row0 = m_ccl.CallMakeNewRow();
			row0.AppliesToRS.Append(cca0_1);
			row0.AppliesToRS.Append(cca0_1b);
			row0.AppliesToRS.Append(cca0_4);

			ChartLocation testCell = m_ccl.MakeLocObj(1, row0);

			int whereToInsertActual;
			ICmIndirectAnnotation existingCcaActual;

			// SUT; icol of CCA in question = 1, iPara of ChOrph = 0
			ConstituentChartLogic.FindWhereToAddResult result = m_ccl.FindWhereToAddChOrph(testCell, 0,
				m_ccl.CallGetBeginOffset(wfics1[0]), out whereToInsertActual, out existingCcaActual);

			// Test results
			Assert.AreEqual(ConstituentChartLogic.FindWhereToAddResult.kInsertChOrphInCCA, result);
			Assert.AreEqual(cca0_1, existingCcaActual.Hvo);
			Assert.AreEqual(0, whereToInsertActual, "Should insert at beginning of CCA"); // insert at beginning
		}

		/// <summary>
		/// Test FindWhereToAddChOrph() in the case where there are multiple(2) CCAs in the specified cell
		/// and the second contains internally the text-logical ChOrph location.
		/// (Should insert into the second CCA.)
		/// </summary>
		[Test]
		public void FindWhereAddChOrph_MultiDataCCAs_Inside2nd()
		{
			StTxtPara para1 = MakeParagraph();

			List<int> wfics1 = MakeNWfics(para1, null, 5);

			m_ccl.Chart = new DsConstChart();
			Cache.LangProject.DiscourseDataOA.ChartsOC.Add(m_ccl.Chart); // needed for CallMakeNewRow()
			m_ccl.Chart.TemplateRA = m_template; // Needed for calls to MakeCca that assign columns.

			// Make CCAs that cover most of the wfics.
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			//This test cares about columns... there are some now. Use a different version of MakeCca
			int cca0_1 = MakeCca(new int[] { wfics1[0] }, 1).Hvo;
			int cca0_1b = MakeCca(new int[] { wfics1[1], wfics1[3] }, 1).Hvo; // wfics1[2] will be ChOrph
			// This tests what happens if we try to put the ChOrph in column index 1.
			int cca0_4 = MakeCca(new int[] { wfics1[4] }, 4).Hvo; // leaves nothing in Ribbon, except ChOrph

			ICmIndirectAnnotation row0 = m_ccl.CallMakeNewRow();
			row0.AppliesToRS.Append(cca0_1);
			row0.AppliesToRS.Append(cca0_1b);
			row0.AppliesToRS.Append(cca0_4);

			ChartLocation testCell = m_ccl.MakeLocObj(1, row0);

			int whereToInsertActual;
			ICmIndirectAnnotation existingCcaActual;

			// SUT; icol of CCA in question = 1, iPara of ChOrph = 0
			ConstituentChartLogic.FindWhereToAddResult result = m_ccl.FindWhereToAddChOrph(testCell, 0,
				m_ccl.CallGetBeginOffset(wfics1[2]), out whereToInsertActual, out existingCcaActual);

			// Test results
			Assert.AreEqual(ConstituentChartLogic.FindWhereToAddResult.kInsertChOrphInCCA, result);
			Assert.AreEqual(cca0_1b, existingCcaActual.Hvo);
			Assert.AreEqual(1, whereToInsertActual, "Should insert ChOrph between the 2 words.");
		}

		/// <summary>
		/// Test FindWhereToAddChOrph() in the case where there are multiple(2) CCAs in the specified cell
		/// and the first contains internally the text-logical ChOrph location.
		/// (Should insert into the first CCA.)
		/// </summary>
		[Test]
		public void FindWhereAddChOrph_MultiDataCCAs_Inside1st()
		{
			StTxtPara para1 = MakeParagraph();

			List<int> wfics1 = MakeNWfics(para1, null, 5);

			m_ccl.Chart = new DsConstChart();
			Cache.LangProject.DiscourseDataOA.ChartsOC.Add(m_ccl.Chart); // needed for CallMakeNewRow()
			m_ccl.Chart.TemplateRA = m_template; // Needed for calls to MakeCca that assign columns.

			// Make CCAs that cover most of the wfics.
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			//This test cares about columns... there are some now. Use a different version of MakeCca
			int cca0_1 = MakeCca(new int[] { wfics1[0], wfics1[2]}, 1).Hvo; // wfics1[1] will be ChOrph
			int cca0_1b = MakeCca(new int[] { wfics1[3] }, 1).Hvo;
			// This tests what happens if we try to put the ChOrph in column index 1.
			int cca0_4 = MakeCca(new int[] { wfics1[4] }, 4).Hvo; // leaves nothing in Ribbon, except ChOrph

			ICmIndirectAnnotation row0 = m_ccl.CallMakeNewRow();
			row0.AppliesToRS.Append(cca0_1);
			row0.AppliesToRS.Append(cca0_1b);
			row0.AppliesToRS.Append(cca0_4);

			ChartLocation testCell = m_ccl.MakeLocObj(1, row0);

			int whereToInsertActual;
			ICmIndirectAnnotation existingCcaActual;

			// SUT; icol of CCA in question = 1, iPara of ChOrph = 0
			ConstituentChartLogic.FindWhereToAddResult result = m_ccl.FindWhereToAddChOrph(testCell, 0,
				m_ccl.CallGetBeginOffset(wfics1[1]), out whereToInsertActual, out existingCcaActual);

			// Test results
			Assert.AreEqual(ConstituentChartLogic.FindWhereToAddResult.kInsertChOrphInCCA, result);
			Assert.AreEqual(cca0_1, existingCcaActual.Hvo);
			Assert.AreEqual(1, whereToInsertActual, "Should insert ChOrph between the 2 words.");
		}

		/// <summary>
		/// Find the maximum index of Ribbon words allowed to be selected due to a ChOrph, but there isn't a ChOrph!
		/// Test the default case.
		/// </summary>
		[Test]
		public void SetRibbonLimits_NoChOrph()
		{
			StTxtPara para1 = MakeParagraph();
			StTxtPara para2 = MakeParagraph();

			List<int> wfics1 = MakeNWfics(para1, null, 3);
			List<int> wfics2 = MakeNWfics(para2, null, 5); // not going to chart all of these...

			m_ccl.Ribbon = new MockRibbon(Cache, m_ccl.StTextHvo); // To set ribbon limits we have to have one!

			m_ccl.Chart = new DsConstChart();
			Cache.LangProject.DiscourseDataOA.ChartsOC.Add(m_ccl.Chart); // needed for CallMakeNewRow()
			m_ccl.Chart.TemplateRA = m_template; // Needed for calls to MakeCca that assign columns.

			// Make CCAs that cover most of the wfics.
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			//This test cares about columns... there are some now. Use a different version of MakeCca
			int cca0_1 = MakeCca(new int[] { wfics1[0], wfics1[1] }, 1).Hvo;
			int cca0_3 = MakeCca(new int[] { wfics1[2] }, 3).Hvo;
			int cca1_2 = MakeCca(new int[] { wfics2[0], wfics2[1] }, 2).Hvo;
			// Leaves 3 wfics uncharted (in the ribbon)

			ICmIndirectAnnotation row0 = m_ccl.CallMakeNewRow();
			row0.AppliesToRS.Append(cca0_1);
			row0.AppliesToRS.Append(cca0_3);
			ICmIndirectAnnotation row1 = m_ccl.CallMakeNewRow();
			row1.AppliesToRS.Append(cca1_2);

			// No ChOrph! SetRibbonLimits shouldn't get called, so we'll do the NextInputIsChOrph() test instead
			// and test the default Ribbon vars.
			//int icolFollActual = 1;
			//ICmIndirectAnnotation rowFollActual = row0;

			//// This shouldn't even get run normally if there's no ChOrph
			//m_ccl.SetRibbonLimits(icolFollActual, rowFollActual);

			// Test results
			Assert.IsFalse(m_ccl.NextInputIsChOrph(), "Next word in Ribbon should not be a Chorph.");
			Assert.AreEqual(-1, m_ccl.Ribbon.EndSelLimitIndex, "Default Ribbon selection limit.");
			Assert.AreEqual(0, m_ccl.Ribbon.SelLimAnn);
		}

		/// <summary>
		/// Find the maximum index of Ribbon words allowed to be selected due to a ChOrph,
		/// for the case where the ChOrph is the first word of the text.
		/// </summary>
		[Test]
		public void SetRibbonLimits_OneWord()
		{
			StTxtPara para1 = MakeParagraph();
			StTxtPara para2 = MakeParagraph();

			List<int> wfics1 = MakeNWfics(para1, null, 3);
			List<int> wfics2 = MakeNWfics(para2, null, 5); // not going to chart all of these...

			m_ccl.Ribbon = new MockRibbon(Cache, m_ccl.StTextHvo); // To set ribbon limits we have to have one!

			m_ccl.Chart = new DsConstChart();
			Cache.LangProject.DiscourseDataOA.ChartsOC.Add(m_ccl.Chart); // needed for CallMakeNewRow()
			m_ccl.Chart.TemplateRA = m_template; // Needed for calls to MakeCca that assign columns.

			// Make CCAs that cover most of the wfics.
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			//This test cares about columns... there are some now. Use a different version of MakeCca
			int cca0_1 = MakeCca(new int[] { wfics1[1] }, 1).Hvo; // Left out [0], first word is ChOrph
			int cca0_3 = MakeCca(new int[] { wfics1[2] }, 3).Hvo;
			int cca1_2 = MakeCca(new int[] { wfics2[0], wfics2[1] }, 2).Hvo;
			// Leaves 3 wfics uncharted (in the ribbon)

			ICmIndirectAnnotation row0 = m_ccl.CallMakeNewRow();
			row0.AppliesToRS.Append(cca0_1);
			row0.AppliesToRS.Append(cca0_3);
			ICmIndirectAnnotation row1 = m_ccl.CallMakeNewRow();
			row1.AppliesToRS.Append(cca1_2);

			// First word is a ChOrph! It is limited to row0, col 1.
			ChartLocation follCell = m_ccl.MakeLocObj(1, row0);

			// SUT
			m_ccl.SetRibbonLimits(follCell);

			// Test results
			Assert.AreEqual(0, m_ccl.Ribbon.EndSelLimitIndex, "Ribbon should only select first word");
			Assert.AreEqual(wfics1[0], m_ccl.Ribbon.SelLimAnn);
		}

		/// <summary>
		/// Find the maximum index of Ribbon words allowed to be selected due to a ChOrph,
		/// for the case where the ChOrph is two words long.
		/// </summary>
		[Test]
		public void SetRibbonLimits_TwoWords()
		{
			StTxtPara para1 = MakeParagraph();
			StTxtPara para2 = MakeParagraph();

			List<int> wfics1 = MakeNWfics(para1, null, 3);
			List<int> wfics2 = MakeNWfics(para2, null, 5); // not going to chart all of these...

			m_ccl.Ribbon = new MockRibbon(Cache, m_ccl.StTextHvo); // To set ribbon limits we have to have one!

			m_ccl.Chart = new DsConstChart();
			Cache.LangProject.DiscourseDataOA.ChartsOC.Add(m_ccl.Chart); // needed for CallMakeNewRow()
			m_ccl.Chart.TemplateRA = m_template; // Needed for calls to MakeCca that assign columns.

			// Make CCAs that cover most of the wfics.
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			//This test cares about columns... there are some now. Use a different version of MakeCca
			int cca0_1 = MakeCca(new int[] { wfics1[0] }, 1).Hvo;
			//int cca0_3 = MakeCca(new int[] { wfics1[1], wfics1[2] }, 3).Hvo;
			int cca1_2 = MakeCca(new int[] { wfics2[0], wfics2[1] }, 2).Hvo;
			// Leaves 3 wfics uncharted (in the ribbon)

			ICmIndirectAnnotation row0 = m_ccl.CallMakeNewRow();
			row0.AppliesToRS.Append(cca0_1);
			//row0.AppliesToRS.Append(cca0_3);
			ICmIndirectAnnotation row1 = m_ccl.CallMakeNewRow();
			row1.AppliesToRS.Append(cca1_2);

			// Two word ChOrph! It is limited to row1, col 2.
			ChartLocation follCell = m_ccl.MakeLocObj(2, row1);

			// SUT
			m_ccl.SetRibbonLimits(follCell);

			// Test results
			Assert.AreEqual(1, m_ccl.Ribbon.EndSelLimitIndex, "Ribbon should be able to select through 2nd word");
			Assert.AreEqual(wfics1[2], m_ccl.Ribbon.SelLimAnn);
		}

		/// <summary>
		/// Find the maximum index of Ribbon words allowed to be selected due to a ChOrph,
		/// for the case where there are two distinct ChOrphs.
		/// </summary>
		[Test]
		public void SetRibbonLimits_TwoChOrphs()
		{
			StTxtPara para1 = MakeParagraph();
			StTxtPara para2 = MakeParagraph();

			List<int> wfics1 = MakeNWfics(para1, null, 3);
			List<int> wfics2 = MakeNWfics(para2, null, 5); // not going to chart all of these...

			m_ccl.Ribbon = new MockRibbon(Cache, m_ccl.StTextHvo); // To set ribbon limits we have to have one!

			m_ccl.Chart = new DsConstChart();
			Cache.LangProject.DiscourseDataOA.ChartsOC.Add(m_ccl.Chart); // needed for CallMakeNewRow()
			m_ccl.Chart.TemplateRA = m_template; // Needed for calls to MakeCca that assign columns.

			// Make CCAs that cover most of the wfics.
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			//This test cares about columns... there are some now. Use a different version of MakeCca
			int cca0_1 = MakeCca(new int[] { wfics1[0] }, 1).Hvo; // Left out [1], second word is ChOrph
			int cca0_3 = MakeCca(new int[] { wfics1[2] }, 3).Hvo;
			// Also left out [0] & [1] from 2nd Para, this is 2nd ChOrph
			int cca1_3 = MakeCca(new int[] { wfics2[3] }, 3).Hvo; // Leaves 1 wfic uncharted (in the ribbon)

			ICmIndirectAnnotation row0 = m_ccl.CallMakeNewRow();
			row0.AppliesToRS.Append(cca0_1);
			row0.AppliesToRS.Append(cca0_3);
			ICmIndirectAnnotation row1 = m_ccl.CallMakeNewRow();
			row1.AppliesToRS.Append(cca1_3);

			// Two distinct ChOrphs! The first is limited to row0, col 1.
			ChartLocation follCell = m_ccl.MakeLocObj(1, row0);

			// SUT
			m_ccl.SetRibbonLimits(follCell);

			// Test results
			Assert.AreEqual(0, m_ccl.Ribbon.EndSelLimitIndex, "Ribbon should only select first word");
			Assert.AreEqual(wfics1[1], m_ccl.Ribbon.SelLimAnn);
		}
	}
}