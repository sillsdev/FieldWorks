// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: StTextTests.cs
// Responsibility: FieldWorks Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.FDO.LangProj;


namespace SIL.FieldWorks.FDO.FDOTests
{

	#region StTextTests class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the StText class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class StTextTests: InMemoryFdoTestBase
	{
		private int m_wsArbitrary;
		private IStText m_stText;
		private IStTxtPara m_stTextPara;
		private IText m_text;
		private IDsChart m_chart;
		private List<int> m_hvoAnnot = new List<int>();
		private int[] m_cols;
		List<int> m_expectedCCAHvos = new List<int>();
		List<int> m_expectedCCRHvos = new List<int>();

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_stText = null;
			m_text = null;
			m_chart = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

#region TestUtils
		private void MakeFakeWfics()
		{
			// CreateTestData's text:
			// Here is a sentence I can chart.
			// 012345678901234567890123456789
			m_hvoAnnot.Add(MakeFakeWfic(0, 3)); //   0 Here
			m_hvoAnnot.Add(MakeFakeWfic(5, 6)); //   1 is
			m_hvoAnnot.Add(MakeFakeWfic(8, 8)); //   2 a
			m_hvoAnnot.Add(MakeFakeWfic(10, 17)); // 3 sentence
			m_hvoAnnot.Add(MakeFakeWfic(19, 19)); // 4 I
			m_hvoAnnot.Add(MakeFakeWfic(21, 23)); // 5 can
			m_hvoAnnot.Add(MakeFakeWfic(25, 29)); // 6 chart

		}

		private int MakeFakeWfic(int cbegin, int cend)
		{
			return m_inMemoryCache.AddWficToLangProj(cbegin, cend, m_stText, m_stTextPara).Hvo;
		}

		private void MakeAnnotDefns()
		{
			// Make the key annotationdefns.
			m_inMemoryCache.InitializeAnnotationDefs();
			m_inMemoryCache.InitializeAnnotationCategories();
			// Get the LangProj AnnotDefns list
			ICmPossibilityList defns = m_inMemoryCache.Cache.LangProject.AnnotationDefsOA;
			MakeAnnDefn(defns, LangProject.kguidAnnWordformInContext);
			MakeAnnDefn(defns, LangProject.kguidConstituentChartRow);
			MakeAnnDefn(defns, LangProject.kguidConstituentChartAnnotation);
		}

		private void MakeAnnDefn(ICmPossibilityList defns, string guid)
		{
			CmAnnotationDefn defn = new CmAnnotationDefn();
			defns.PossibilitiesOS.Append(defn);
			Cache.VwCacheDaAccessor.CacheGuidProp(defn.Hvo, (int)CmObjectFields.kflidCmObject_Guid,
				new Guid(guid));
		}

		private void MakeFakeTemplate()
		{
			m_cols = new int[6];
			ICmPossibility template;
			template = m_inMemoryCache.AddEmptyTemplateToDiscData("default",
				m_inMemoryCache.Cache.LangProject.DiscourseDataOA);
			// Add columns
			m_cols[0] = m_inMemoryCache.AddColumnToTemplate(template, "Prenuc");
			m_cols[1] = m_inMemoryCache.AddColumnToTemplate(template, "Subject");
			m_cols[2] = m_inMemoryCache.AddColumnToTemplate(template, "Verb");
			m_cols[3] = m_inMemoryCache.AddColumnToTemplate(template, "Object");
			m_cols[4] = m_inMemoryCache.AddColumnToTemplate(template, "Postnuc1");
			m_cols[5] = m_inMemoryCache.AddColumnToTemplate(template, "Postnuc2");
		}

		private void MakeBasicChart()
		{
			// Make CCAs
			m_expectedCCAHvos.Add(m_inMemoryCache.AddIndirAnnToLangProj(new int[] { m_hvoAnnot[0] }, m_cols[1], ""));
			m_expectedCCAHvos.Add(m_inMemoryCache.AddIndirAnnToLangProj(new int[] { m_hvoAnnot[1] }, m_cols[2], ""));
			m_expectedCCAHvos.Add(m_inMemoryCache.AddIndirAnnToLangProj(new int[] { m_hvoAnnot[2], m_hvoAnnot[3] }, m_cols[3], ""));
			m_expectedCCAHvos.Add(m_inMemoryCache.AddIndirAnnToLangProj(new int[] { m_hvoAnnot[4] }, m_cols[1], ""));
			m_expectedCCAHvos.Add(m_inMemoryCache.AddIndirAnnToLangProj(new int[] { m_hvoAnnot[5], m_hvoAnnot[6] }, m_cols[2], ""));

			// Make rows
			m_expectedCCRHvos.Add(m_inMemoryCache.AddIndirAnnToLangProj(
				new int[] { m_expectedCCAHvos[0], m_expectedCCAHvos[1], m_expectedCCAHvos[2] }, 0, "1a"));
			m_expectedCCRHvos.Add(m_inMemoryCache.AddIndirAnnToLangProj(
				new int[] { m_expectedCCAHvos[3], m_expectedCCAHvos[4] }, 0, "1b"));
			(m_chart as IDsConstChart).RowsRS.Append(m_expectedCCRHvos[0]);
			(m_chart as IDsConstChart).RowsRS.Append(m_expectedCCRHvos[1]);
		}

		/// <summary>
		/// Asserts that each int in 'expected' array is contained in 'actual' array.
		/// </summary>
		/// <param name="expected"></param>
		/// <param name="actual"></param>
		private void AssertArrayContainedInArray(int[] expected, int[] actual)
		{
			foreach (int expNum in expected)
				 Assert.Contains(expNum, actual);
		}

#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create test data for tests. Includes (an empty) constituent chart.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_inMemoryCache.InitializeWritingSystemEncodings();

			m_wsArbitrary = Cache.LanguageEncodings.Item(0).Hvo;
			m_text = m_inMemoryCache.AddInterlinearTextToLangProj("My Interlinear Text");
			m_stTextPara = m_inMemoryCache.AddParaToInterlinearTextContents(m_text, "Here is a sentence I can chart.");
			m_stText = m_text.ContentsOA;
			m_chart = m_inMemoryCache.AddChartToLangProj("My Discourse Chart", m_stText);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the basic operation of deleting a text by creating an empty chart and
		/// deleting it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteText_emptyChart()
		{
			Set<int> hvosToDelete = new Set<int>();

			using (NullProgressState state = new NullProgressState())
			{
				// SUT
				m_stText.DeleteObjectSideEffects(hvosToDelete, state);
			}
			int[] hvosActual = hvosToDelete.ToArray();
			Assert.AreEqual(3, hvosActual.Length, "Wrong number of hvos to delete.");
			Assert.Contains(m_chart.Hvo, hvosActual, "The (empty) chart should be deleted.");
			Assert.AreEqual(
				(int)SIL.FieldWorks.FDO.CmObject.SpecialHVOValues.kHvoUnderlyingObjectDeleted,
				m_stText.Hvo, "StText is already deleted.");
			Assert.Contains(m_text.Hvo, hvosActual, "The containing Text should be deleted.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the operation of deleting a chart by creating some wfics and
		/// deleting the text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteText_WficsButEmptyChart()
		{
			MakeFakeWfics(); // annotation hvos are stored in List m_hvoAnnot.
			Set<int> hvosToDelete = new Set<int>();

			using (NullProgressState state = new NullProgressState())
			{
				// SUT
				m_stText.DeleteObjectSideEffects(hvosToDelete, state);
			}
			int[] hvosActual = hvosToDelete.ToArray();
			Assert.AreEqual(3, hvosActual.Length, "Wrong number of hvos to delete.");
			Assert.Contains(m_chart.Hvo, hvosActual, "The (empty) chart should be deleted.");
			Assert.AreEqual(
				(int)SIL.FieldWorks.FDO.CmObject.SpecialHVOValues.kHvoUnderlyingObjectDeleted,
				m_stText.Hvo, "StText is already deleted.");
			Assert.Contains(m_text.Hvo, hvosActual, "The containing Text should be deleted.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the operation of deleting a chart by creating some wfics, creating a basic chart
		/// and then deleting the text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteText_BasicChart()
		{
			MakeFakeWfics(); // annotation hvos are stored in List m_hvoAnnot.
			MakeFakeTemplate(); // template hvos are stored in m_cols[].
			// CCA annotation hvos are stored in m_expectedCCAHvos, but should get deleted!
			// CCR annotation hvos are stored in m_expectedCCRHvos, but should get deleted!
			MakeBasicChart();
			Set<int> hvosToDelete = new Set<int>();

			using (NullProgressState state = new NullProgressState())
			{
				// SUT
				m_stText.DeleteObjectSideEffects(hvosToDelete, state);
			}
			int[] hvosActual = hvosToDelete.ToArray();
			// 10 = chartHvo, TextHvo, StTextHvo, 2XCCRHvo, 5XCCAHvo
			Assert.AreEqual(10, hvosActual.Length, "Wrong number of hvos to delete.");
			Assert.Contains(m_chart.Hvo, hvosActual, "The (empty) chart should be deleted.");
			Assert.AreEqual(
				(int)SIL.FieldWorks.FDO.CmObject.SpecialHVOValues.kHvoUnderlyingObjectDeleted,
				m_stText.Hvo, "StText is already deleted.");
			Assert.Contains(m_text.Hvo, hvosActual, "The containing Text should be deleted.");
			AssertArrayContainedInArray(m_expectedCCAHvos.ToArray(), hvosActual);
			AssertArrayContainedInArray(m_expectedCCRHvos.ToArray(), hvosActual);
		}
	}

	#endregion
}