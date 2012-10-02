using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO.LangProj;
using System.Xml;
using SIL.Utils;

namespace SIL.FieldWorks.Discourse
{
	public class DiscourseTestHelper
	{
		Dictionary<int, int[]> m_annotations = new Dictionary<int, int[]>();
		internal Text m_text;
		internal StText m_stText;
		internal ITsStrFactory m_tsf;
		internal StTxtPara m_firstPara;
		FdoCache m_cache;
		TestCCLogic m_logic;
		CmPossibility m_template;
		List<int> m_allColumns;
		DsConstChart m_chart;

		public DiscourseTestHelper(FdoCache cache)
		{
			m_cache = cache;
			m_tsf = TsStrFactoryClass.Create();
			m_text = (Text)Cache.LangProject.TextsOC.Add(new Text());
			m_stText = new StText();
			m_text.ContentsOA = m_stText;
			m_firstPara = MakeParagraph();
		}

		internal DsConstChart Chart
		{
			get { return m_chart; }
			set { m_chart = value; }
		}

		public void Dispose()
		{
			m_stText = null;
			m_tsf = null;
			m_text = null;
			m_firstPara = null;
			m_cache = null;
		}

		internal TestCCLogic Logic
		{
			get { return m_logic; }
			set { m_logic = value; }
		}

		public CmPossibility MakeTemplate(out List<int> allCols)
		{
			// The exact organization of columns is not of great
			// importance for the current tests (still less the names), but we do want there
			// to be a hierarchy, since that is a common problem, and naming them conventionally
			// may make debugging easier. Currently this is the same set of columns as
			// m_logic.CreateDefaultColumns, but we make it explicit here so most of the test
			// code is unaffected by changes to the default.
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(
				"<template name=\"default\">"
				+ "<column name=\"prenuclear\">"
				+ "<column name=\"prenuc1\"/>"
				+ "<column name=\"prenuc2\"/>"
				+ "</column>"
				+ "<column name=\"nucleus\">"
				+ "<column name=\"Subject\"/>"
				+ "<column name=\"verb\"/>"
				+ "<column name=\"object\"/>"
				+ "</column>"
				+ "<column name=\"postnuc\"/>"
				+ "</template>");
			m_template = (CmPossibility)m_cache.LangProject.CreateChartTemplate(doc.DocumentElement);
			//m_template = m_logic.CreateTemplate(doc.DocumentElement);
			m_allColumns = m_logic.AllColumns(m_template);
			allCols = m_allColumns;
			return m_template;
		}

		FdoCache Cache
		{
			get { return m_cache; }
		}

		internal StTxtPara FirstPara
		{
			get { return m_firstPara; }
		}

		internal StTxtPara MakeParagraph()
		{
			StTxtPara para0 = new StTxtPara();
			m_stText.ParagraphsOS.Append(para0);
			int cPara = m_stText.ParagraphsOS.Count;
			para0.Contents.UnderlyingTsString = m_tsf.MakeString("this is paragraph " + cPara + " for our constituent chart database tests.",
				Cache.DefaultVernWs);
			return para0;
		}



		// Make some sort of wfics for the text of the specified paragraph. Assumes no double spaces!
		// Caches results and does not repeat on same para
		internal int[] MakeAnnotations(StTxtPara para)
		{
			int[] previous;
			if (m_annotations.TryGetValue(para.Hvo, out previous))
				return previous;
			string contents = para.Contents.Text;
			string[] words = contents.Split(new char[] { ' ', '.' });
			int ich = 0;
			List<int> results = new List<int>();
			ICmAnnotationDefn WficType = CmAnnotationDefn.Twfic(Cache);
			foreach (string word in words)
			{
				if (word == "")
				{
					ich++;
					continue;
				}
				WfiWordform wordform = new WfiWordform();
				Cache.LangProject.WordformInventoryOA.WordformsOC.Add(wordform);
				wordform.Form.SetAlternative(word, Cache.DefaultVernWs);
				// JohnT: This should ideally use CmBaseAnnotation.CreateUnownedCba. But most or all uses of this
				// method are memory-only tests, and that method requires a database.
				CmBaseAnnotation cba = new CmBaseAnnotation();
				Cache.LangProject.AnnotationsOC.Add(cba);
				cba.BeginOffset = ich;
				ich += word.Length;
				cba.EndOffset = ich;
				ich++; // past space or dot
				cba.BeginObjectRA = para;
				cba.AnnotationTypeRA = WficType;
				//cba.AnnotationTypeRA = ?? can we get CmAnnotationDefn.Twfic(Cache) with a non-database cache?
				WfiAnalysis analysis = new WfiAnalysis();
				wordform.AnalysesOC.Add(analysis);
				WfiGloss gloss = new WfiGloss();
				analysis.MeaningsOC.Add(gloss);
				gloss.Form.SetAlternative(word + "Gloss" + ich, Cache.DefaultAnalWs);
				cba.InstanceOfRA = gloss;
				results.Add(cba.Hvo);

			}
			int[] result = results.ToArray();
			m_annotations[para.Hvo] = result;
			return result;
		}

		/// <summary>
		/// Call MakeAnnotations, then cache all but the first nUsedAnnotations of them as
		/// the value of the annotationList. One param version assumes first paragraph.
		/// </summary>
		/// <param name="nUsedAnnotations">-1 is magic for "All used"</param>
		/// <returns></returns>
		internal int[] MakeAnnotationsUsedN(int nUsedAnnotations)
		{
			return MakeAnnotationsUsedN(nUsedAnnotations, m_firstPara);
		}

		/// <summary>
		/// Call MakeAnnotations, then cache all but the first nUsedAnnotations of them as
		/// the value of the annotationList. Might be any paragraph.
		/// </summary>
		/// <param name="nUsedAnnotations">-1 is magic for "All used"</param>
		/// <returns></returns>
		internal int[] MakeAnnotationsUsedN(int nUsedAnnotations, StTxtPara para)
		{
			int[] allParaWfics = MakeAnnotations(para);
			if (nUsedAnnotations < 0)
				nUsedAnnotations = allParaWfics.Length; // nUsedAnnotations = -1 is magic for "all used"
			int[] unusedAnnotations = SubArray(allParaWfics, nUsedAnnotations, allParaWfics.Length);
			int[] cachedAnnotations = Cache.GetVectorProperty(m_stText.Hvo, m_logic.Ribbon.AnnotationListId, false);
			if (cachedAnnotations.Length > 0)
			{
				int[] newUnusedAnn = new int[unusedAnnotations.Length + cachedAnnotations.Length];
				//newUnusedAnn = cachedAnnotations + unusedAnnotations; // this work? Nope.
				int i = 0; // need to keep i after its loop
				for (; i < cachedAnnotations.Length; i++)
					newUnusedAnn[i] = cachedAnnotations[i]; // copy in cachedAnnotations first

				for (int j = 0; j < unusedAnnotations.Length; j++)
					newUnusedAnn[i + j] = unusedAnnotations[j]; // copy in unusedAnnotations next

				Cache.VwCacheDaAccessor.CacheVecProp(m_stText.Hvo, m_logic.Ribbon.AnnotationListId,
					newUnusedAnn, newUnusedAnn.Length);
			}
			else
			{
				Cache.VwCacheDaAccessor.CacheVecProp(m_stText.Hvo, m_logic.Ribbon.AnnotationListId,
					unusedAnnotations, unusedAnnotations.Length);
			}
			return allParaWfics;
		}

		#region DefaultChartMarkers

		internal ICmPossibilityList MakeDefaultChartMarkers()
		{
			string xml =
				"<list>"
				+ " <item name=\"Group1\" abbr=\"G1\">"
					+ " <item name=\"Group1.1\" abbr=\"G1.1\">"
						+ " <item name=\"Item1\" abbr=\"I1\"/>"
					+ " </item>"
				+ " </item>"
				+ " <item name=\"Group2\" abbr=\"G2\">"
					+ " <item name=\"Item2\" abbr=\"I2\"/>"
					+ " <item name=\"Item3\" abbr=\"I3\"/>"
				+ " </item>"
				+ "</list>";
			return MakeChartMarkers(xml);
		}

		internal ICmPossibilityList MakeChartMarkers(string xml)
		{
			ICmPossibilityList result = m_cache.LangProject.DiscourseDataOA.ChartMarkersOA = new CmPossibilityList();
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(xml);
			MakeListXml(result, doc.DocumentElement);
			return result;
		}
		private void MakeListXml(ICmPossibilityList list, XmlElement root)
		{
			foreach (XmlNode item in root)
			{
				ICmPossibility poss = list.PossibilitiesOS.Append(new CmPossibility());
				InitItem(item, poss);

			}
		}

		private static void InitItem(XmlNode item, ICmPossibility poss)
		{
			poss.Name.AnalysisDefaultWritingSystem = XmlUtils.GetManditoryAttributeValue(item, "name");
			string abbr = XmlUtils.GetOptionalAttributeValue(item, "abbr");
			if (String.IsNullOrEmpty(abbr))
				abbr = poss.Name.AnalysisDefaultWritingSystem;
			poss.Abbreviation.AnalysisDefaultWritingSystem = abbr;
			foreach (XmlNode subItem in item.ChildNodes)
			{
				ICmPossibility poss2 = poss.SubPossibilitiesOS.Append(new CmPossibility());
				InitItem(subItem, poss2);
			}
		}

		#endregion // DefaultChartMarkers

		/// <summary>
		/// Make a typical first row (we don't care about the label).
		/// If we DO care about the label and there will be other rows, use MakeRow() or MakeRow1a().
		/// </summary>
		/// <returns></returns>
		internal CmIndirectAnnotation MakeFirstRow()
		{
			return MakeRow(m_chart, FirstRowLabel);
		}
		internal string FirstRowLabel
		{
			get { return "1"; }
		}

		/// <summary>
		/// Make a typical second row (we might care about the label).
		/// Must be called after MakeFirstRow.
		/// </summary>
		/// <returns></returns>
		internal CmIndirectAnnotation MakeSecondRow()
		{
			return MakeRow(m_chart, SecondRowLabel);
		}
		internal string SecondRowLabel
		{
			get { return "1b"; }
		}

		/// <summary>
		/// Make a typical first row where there will be others
		/// </summary>
		/// <returns></returns>
		internal CmIndirectAnnotation MakeRow1a()
		{
			return MakeRow(m_chart, FirstClauseRowLabel);
		}
		internal string FirstClauseRowLabel
		{
			get { return "1a"; }
		}

		internal CmIndirectAnnotation MakeRow(string lineNo)
		{
			return MakeRow(m_chart, lineNo);
		}

		internal CmIndirectAnnotation MakeRow(DsConstChart chart, string lineNo)
		{
			CmIndirectAnnotation row = MakeIndirectAnnotation();
			chart.RowsRS.Append(row);
			row.Comment.SetAnalysisDefaultWritingSystem(lineNo);
			row.AnnotationTypeRA = CmAnnotationDefn.ConstituentChartRow(Cache);
			return row;
		}
		// Make a column annotation for the specified column that groups the specified words
		// and append to the specified row.
		internal CmIndirectAnnotation MakeColumnAnnotation(int icol, int[] words, ICmIndirectAnnotation row)
		{
			CmIndirectAnnotation cca = MakeIndirectAnnotation();
			for (int i = 0; i < words.Length; i++)
				cca.AppliesToRS.Append(words[i]);
			InitializeCca(cca, icol, row);
			return cca;
		}

		internal ICmBaseAnnotation MakeMarkerAnnotation(int icol, ICmIndirectAnnotation row, ICmPossibility marker)
		{
			CmBaseAnnotation cba = MakeBaseAnnotation();
			cba.BeginObjectRA = marker;
			InitializeCca(cba, icol, row);
			return cba;
		}

		internal CmIndirectAnnotation MakeMovedTextAnnotation(int icol, CmIndirectAnnotation target,
			CmIndirectAnnotation rowAnn, string marker)
		{
			CmIndirectAnnotation cca = MakeIndirectAnnotation();
			if (target != null)
				cca.AppliesToRS.Append(target);
			InitializeCca(cca, icol, rowAnn);
			cca.Comment.AnalysisDefaultWritingSystem.Text = marker;
			return cca;
		}

		/// <summary>
		/// Get some arbitrary chart marker.
		/// </summary>
		/// <returns></returns>
		internal ICmPossibility GetAMarker()
		{
			return Cache.LangProject.DiscourseDataOA.ChartMarkersOA.PossibilitiesOS[1].SubPossibilitiesOS[0];
		}
		/// <summary>
		/// Get some arbitrary chart marker (different from GetAMarker()).
		/// </summary>
		/// <returns></returns>
		internal ICmPossibility GetAnotherMarker()
		{
			return Cache.LangProject.DiscourseDataOA.ChartMarkersOA.PossibilitiesOS[1].SubPossibilitiesOS[1];
		}

		private void InitializeCca(ICmAnnotation cca, int icol, ICmIndirectAnnotation row)
		{
			cca.InstanceOfRAHvo = m_allColumns[icol];
			row.AppliesToRS.Append(cca);
			cca.AnnotationTypeRA = CmAnnotationDefn.ConstituentChartAnnotation(Cache);
		}

		internal CmIndirectAnnotation MakeIndirectAnnotation()
		{
			return Cache.LangProject.AnnotationsOC.Add(new CmIndirectAnnotation()) as CmIndirectAnnotation;
		}

		internal CmBaseAnnotation MakeBaseAnnotation()
		{
			return Cache.LangProject.AnnotationsOC.Add(new CmBaseAnnotation()) as CmBaseAnnotation;
		}

		/// <summary>
		/// Copy a subset out of an int[] array.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="start"></param>
		/// <param name="count1"></param>
		/// <returns></returns>
		internal static int[] SubArray(int[] input, int start, int count1)
		{
			int count = Math.Min(count1, input.Length - start);
			int[] result = new int[count];
			for (int i = 0; i < count; i++)
				result[i] = input[start + i];
			return result;
		}

		internal void VerifyFirstRow(int cAppliesTo)
		{
			VerifyRow(0, FirstRowLabel, cAppliesTo);
		}
		internal void VerifySecondRow(int cAppliesTo)
		{
			VerifyRow(1, SecondRowLabel, cAppliesTo);
		}
		/// <summary>
		/// Verify that the specified row of the chart exists and has the expected row-number comment and number of rows.
		/// Also that it is a CCR.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="rowNumber"></param>
		/// <param name="cAppliesTo"></param>
		internal void VerifyRow(int index, string rowNumber, int cAppliesTo)
		{
			int crows = m_chart.RowsRS.Count;
			Assert.IsTrue(index <= crows);
			CmIndirectAnnotation row = (CmIndirectAnnotation)m_chart.RowsRS[index];
			Assert.AreEqual(rowNumber, row.Comment.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual(cAppliesTo, row.AppliesToRS.Count);
			Assert.AreEqual(CmAnnotationDefn.ConstituentChartRow(Cache), row.AnnotationTypeRA);
		}

		/// <summary>
		/// Verify that there is a row with the specified index that has a cell with the specified
		/// index which belongs to the specified column (instanceOf) and appliesTo the specified words
		/// and has the specified marker (if any).
		/// </summary>
		/// <param name="rowIndex"></param>
		/// <param name="cellIndex"></param>
		/// <param name="hvoColumn"></param>
		/// <param name="words"></param>
		/// <param name="marker"></param>
		internal void VerifyCca(int rowIndex, int ccaIndex, int hvoColumn, int[] words, string marker)
		{
			ICmIndirectAnnotation cca = VerifyCcaBasic(rowIndex, ccaIndex, hvoColumn) as ICmIndirectAnnotation;
			Assert.IsNotNull(cca, "Cca should be indirect annotation");
			Assert.AreEqual(words, cca.AppliesToRS.HvoArray);
			string comment = cca.Comment.AnalysisDefaultWritingSystem.Text;
			if (comment == null)
				comment = "";
			Assert.AreEqual(marker, comment);
			Assert.AreEqual(CmAnnotationDefn.ConstituentChartAnnotation(Cache), cca.AnnotationTypeRA);
		}
		/// <summary>
		/// Verify that there is a row with the specified index that has a cell with the specified
		/// index which belongs to the specified column (instanceOf) and appliesTo the specified words
		/// and has the specified marker (if any).
		/// </summary>
		/// <param name="rowIndex"></param>
		/// <param name="cellIndex"></param>
		/// <param name="hvoColumn"></param>
		/// <param name="words"></param>
		/// <param name="marker"></param>
		internal void VerifyMarkerCca(int rowIndex, int ccaIndex, int hvoColumn, int hvoMarker)
		{
			ICmBaseAnnotation cca = VerifyCcaBasic(rowIndex, ccaIndex, hvoColumn) as ICmBaseAnnotation;
			Assert.IsNotNull(cca, "Cca should be base annotation");
			Assert.AreEqual(hvoMarker, cca.BeginObjectRAHvo);
		}

		private ICmAnnotation VerifyCcaBasic(int rowIndex, int ccaIndex, int hvoColumn)
		{
			int crows = m_chart.RowsRS.Count;
			Assert.IsTrue(rowIndex <= crows);
			CmIndirectAnnotation row = (CmIndirectAnnotation)m_chart.RowsRS[rowIndex];
			int cCells = row.AppliesToRS.Count;
			Assert.IsTrue(ccaIndex < cCells);
			ICmAnnotation cca = row.AppliesToRS[ccaIndex];
			Assert.AreEqual(hvoColumn, cca.InstanceOfRAHvo);
			Assert.AreEqual(CmAnnotationDefn.ConstituentChartAnnotation(Cache), cca.AnnotationTypeRA);
			return cca;
		}

		/// <summary>
		/// Make a dependent clause marker at the end of the specified row in the specified column
		/// for the specified clauses (rows) of the specified type. Caller supplies a marker.
		/// </summary>
		/// <param name="rowSrc"></param>
		/// <param name="colSrc"></param>
		/// <param name="depClauses"></param>
		/// <param name="depType"></param>
		/// <param name="marker"></param>
		/// <returns></returns>
		internal ICmIndirectAnnotation MakeDependentClauseMarker(ICmIndirectAnnotation rowSrc, int colSrc,
			int[] depClauses, string depType, string marker)
		{
			foreach (int rowDst in depClauses)
			{
				string insert = "";
				if (rowDst == depClauses[0])
					insert += " firstDep=\"true\"";
				if (rowDst == depClauses[depClauses.Length - 1])
					insert += " endDep=\"true\"";

				Cache.SetUnicodeProperty(rowDst, (int)CmAnnotation.CmAnnotationTags.kflidCompDetails,
					"<ccinfo " + depType + "=\"true\"" + insert + "/>");
			}
			ICmIndirectAnnotation result = MakeColumnAnnotation(colSrc, depClauses, rowSrc);
			result.Comment.SetAnalysisDefaultWritingSystem(marker);
			return result;
		}
	}
}
