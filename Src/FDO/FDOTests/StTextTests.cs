// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2003' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: StTextTests.cs
// Responsibility: FieldWorks Team
// --------------------------------------------------------------------------------------------
using System;
using System.Reflection;
using NUnit.Framework;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.FDOTests
{
	#region StTextTests class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the StText class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class StTextTests: MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private IStText m_stText;
		private IStTxtPara m_stTextPara;
		private IText m_text;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create test data for tests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_text = AddInterlinearTextToLangProj("My Interlinear Text");
			m_stTextPara = AddParaToInterlinearTextContents(m_text, "Here is a sentence I can chart.");
			m_stText = m_text.ContentsOA;
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
			IDsChart chart = AddChartToLangProj("My Discourse Chart", m_stText);

			// SUT
			Cache.DomainDataByFlid.DeleteObj(m_text.Hvo);

			Assert.AreEqual((int)SpecialHVOValues.kHvoObjectDeleted, chart.Hvo, "The chart should be deleted.");
			Assert.AreEqual((int)SpecialHVOValues.kHvoObjectDeleted, m_stText.Hvo, "The contained StText should be deleted.");
			Assert.AreEqual((int)SpecialHVOValues.kHvoObjectDeleted, m_text.Hvo, "The containing Text should be deleted.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that attempting to delete a StText directly without deleting its owner fails
		/// unless the StText is in a collection (which I don't think it ever is in our model,
		/// except in the case of footnotes, which are a subclass of StText).
		/// deleting it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(InvalidOperationException))]
		public void DeleteStTextWithoutDeletingOwner()
		{
			try
			{
				Cache.DomainDataByFlid.DeleteObj(m_stText.Hvo);
			}
			catch (TargetInvocationException e)
			{
				throw e.InnerException;
			}
		}

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds an empty chart on the specified text.
		/// </summary>
		/// <param name="name">Chart name.</param>
		/// <param name="stText">Chart is BasedOn this text.</param>
		/// ------------------------------------------------------------------------------------
		private IDsConstChart AddChartToLangProj(string name, IStText stText)
		{
			IFdoServiceLocator servloc = Cache.ServiceLocator;
			IDsConstChart chart = servloc.GetInstance<IDsConstChartFactory>().Create();
			if (Cache.LangProject.DiscourseDataOA == null)
				Cache.LangProject.DiscourseDataOA = servloc.GetInstance<IDsDiscourseDataFactory>().Create();

			Cache.LangProject.DiscourseDataOA.ChartsOC.Add(chart);

			// Setup the new chart
			chart.Name.AnalysisDefaultWritingSystem = StringUtils.MakeTss(name, Cache.DefaultAnalWs);
			chart.BasedOnRA = stText;

			return chart; // This chart has no template or rows, so far!!
		}
		#endregion
	}
	#endregion
}
