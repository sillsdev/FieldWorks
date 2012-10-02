// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: StTextAnnotationNavigatorTests.cs
// Responsibility: maclean
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class StTextAnnotationNavigatorTests : MemoryOnlyBackendProviderBasicTestBase
	{

		IText m_text;
		private IStText m_stText;
		private IStTxtPara m_para0, m_para1;
		//private IList<AnalysisTree> m_analysis_para0 = new List<AnalysisTree>();
		private List<AnalysisOccurrence> m_expectedOccurrences;
		private List<AnalysisOccurrence> m_expectedOccurrencesPara0;

		#region Test setup
		/// <summary>
		///
		/// </summary>
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				DoSetupFixture);
		}

		/// <summary>
		/// non-undoable task
		/// </summary>
		private void DoSetupFixture()
		{

			var textFactory = Cache.ServiceLocator.GetInstance<ITextFactory>();
			var stTextFactory = Cache.ServiceLocator.GetInstance<IStTextFactory>();
			m_text = textFactory.Create();
			Cache.LangProject.TextsOC.Add(m_text);
			m_stText = stTextFactory.Create();
			m_text.ContentsOA = m_stText;
			m_para0 = m_stText.AddNewTextPara(null);
			m_para0.Contents = TsStringUtils.MakeTss("Xxxhope xxxthis xxxwill xxxdo. xxxI xxxhope.", Cache.DefaultVernWs);
			m_para1 = m_stText.AddNewTextPara(null);
			m_para1.Contents = TsStringUtils.MakeTss("Xxxcertain xxxto xxxcatch xxxa xxxfrog. xxxCertainly xxxcan xxxon xxxLake xxxMonroe.", Cache.DefaultVernWs);

			using (ParagraphParser pp = new ParagraphParser(Cache))
			{
				foreach (IStTxtPara para in m_stText.ParagraphsOS)
				{
					if (para.ParseIsCurrent) continue;
					pp.Parse(para);
				}
			}

			m_expectedOccurrences = new List<AnalysisOccurrence>();
			foreach (IStTxtPara para in m_stText.ParagraphsOS)
			{
				foreach (var seg in para.SegmentsOS)
					for(int i = 0; i < seg.AnalysesRS.Count; i++)
						m_expectedOccurrences.Add(new AnalysisOccurrence(seg, i));
			}

			m_expectedOccurrencesPara0 = new List<AnalysisOccurrence>();
			foreach (var seg in m_para0.SegmentsOS)
				for (int i = 0; i < seg.AnalysesRS.Count; i++)
					m_expectedOccurrencesPara0.Add(new AnalysisOccurrence(seg, i));
		}

		#endregion

		#region Tests

		/// <summary>
		/// 0		1		2		3    4 5	6	   7
		/// Xxxhope xxxthis xxxwill xxxdo. xxxI xxxhope.
		/// 01234567890123456789012345678901234567890123
		///
		/// 8          9     10       11   12     13 14           15     16    17      18       19
		/// Xxxcertain xxxto xxxcatch xxxa xxxfrog.  xxxCertainly xxxcan xxxon xxxLake xxxMonroe.
		/// </summary>
		[Test]
		public void AdvanceThroughWordformInThePara_Using_GetNextWordformOrDefault()
		{
			//start before end of sentence
			var navigator = new SegmentServices.StTextAnnotationNavigator(m_expectedOccurrences[2]);
			var nextWordform = navigator.GetNextWordformOrDefault(null);
			Assert.AreEqual(m_expectedOccurrences[3], nextWordform);

			navigator = new SegmentServices.StTextAnnotationNavigator(m_expectedOccurrences[3]);
			nextWordform = navigator.GetNextWordformOrDefault(null);
			Assert.AreEqual(m_expectedOccurrences[5], nextWordform);

			navigator = new SegmentServices.StTextAnnotationNavigator(m_expectedOccurrences[6]);
			nextWordform = navigator.GetNextWordformOrDefault(null);
			Assert.AreEqual(m_expectedOccurrences[8], nextWordform);

			//position the navigator at the last occurrence of the stText.
			navigator = new SegmentServices.StTextAnnotationNavigator(m_expectedOccurrences[18]);
			//if there are no occurrences beyond this position in the stText then null should be returned
			nextWordform = navigator.GetNextWordformOrDefault(null);
			Assert.AreEqual(null, nextWordform);

			// JohnT: this is to test that it returns an arbitrary default passed in, if there are
			// no more. This dummy analysis occurrence is probably not in a valid state, so don't
			// use it for other things or be too surprised if improved validation detects a problem.
			var expected = new AnalysisOccurrence(m_para0.SegmentsOS[0], 18);
			nextWordform = navigator.GetNextWordformOrDefault(expected);
			Assert.AreEqual(expected, nextWordform);

			navigator = new SegmentServices.StTextAnnotationNavigator(m_expectedOccurrences[18]);
			//if there are no occurrences beyond this position in the stText then null should be returned
			nextWordform = navigator.GetNextWordformOrDefault(null);
			Assert.AreEqual(null, nextWordform);

			//========================================================================
			navigator = new SegmentServices.StTextAnnotationNavigator(m_expectedOccurrences[2]);
			nextWordform = navigator.GetNextWordformOrStartingWordform();
			Assert.AreEqual(m_expectedOccurrences[3], nextWordform);

			navigator = new SegmentServices.StTextAnnotationNavigator(m_expectedOccurrences[3]);
			nextWordform = navigator.GetNextWordformOrStartingWordform();
			Assert.AreEqual(m_expectedOccurrences[5], nextWordform);

			navigator = new SegmentServices.StTextAnnotationNavigator(m_expectedOccurrences[6]);
			nextWordform = navigator.GetNextWordformOrStartingWordform();
			Assert.AreEqual(m_expectedOccurrences[8], nextWordform);

			//position the navigator at the last occurrence of the stText.
			navigator = new SegmentServices.StTextAnnotationNavigator(m_expectedOccurrences[18]);
			//if there are no occurrences beyond this position in the stText then null should be returned
			nextWordform = navigator.GetNextWordformOrStartingWordform();
			Assert.AreEqual(m_expectedOccurrences[18], nextWordform);
		}


		/// <summary>
		/// 0		1		2		3    4 5	6	   7
		/// Xxxhope xxxthis xxxwill xxxdo. xxxI xxxhope.
		/// 01234567890123456789012345678901234567890123
		///
		/// 8          9     10       11   12     13 14           15     16    17      18       19
		/// Xxxcertain xxxto xxxcatch xxxa xxxfrog.  xxxCertainly xxxcan xxxon xxxLake xxxMonroe.
		/// </summary>
		[Test]
		public void AdvanceThroughAnalysisOccurrences_StartingAtEndOfPara()
		{
			var navigator = new SegmentServices.StTextAnnotationNavigator(m_expectedOccurrences[6]);
			int i = 6;
			foreach (var occurrence in navigator.GetAnalysisOccurrencesAdvancingIncludingStartingOccurrence())
			{
				Assert.AreEqual(m_expectedOccurrences[i], occurrence);
				i++;
			}
			//ensure the all paragraphs in the stText were processed.
			Assert.AreEqual(m_expectedOccurrences.Count, i);
		}

		/// <summary>
		/// 0		1		2		3    4 5	6	   7
		/// Xxxhope xxxthis xxxwill xxxdo. xxxI xxxhope.
		/// 01234567890123456789012345678901234567890123
		///
		/// 8          9     10       11   12     13 14           15     16    17      18       19
		/// Xxxcertain xxxto xxxcatch xxxa xxxfrog.  xxxCertainly xxxcan xxxon xxxLake xxxMonroe.
		/// </summary>
		[Test]
		public void AdvanceThroughEachAnalysisOccurrenceInThePara()
		{
			int i = 0;
			foreach (IStTxtPara para in m_stText.ParagraphsOS)
			{
				foreach (var occurrence in SegmentServices.StTextAnnotationNavigator.GetAnalysisOccurrencesAdvancingInPara(para))
				{
					Assert.AreEqual(m_expectedOccurrences[i], occurrence);
					i++;
				}
			}
		}

		/// <summary>
		/// 0		1		2		3    4 5	6	   7
		/// Xxxhope xxxthis xxxwill xxxdo. xxxI xxxhope.
		/// 01234567890123456789012345678901234567890123
		///
		/// 8          9     10       11   12     13 14           15     16    17      18       19
		/// Xxxcertain xxxto xxxcatch xxxa xxxfrog.  xxxCertainly xxxcan xxxon xxxLake xxxMonroe.
		/// </summary>
		[Test]
		public void AdvanceThroughEachAnalysisOccurrenceInTheStText()
		{
			var navigator = new SegmentServices.StTextAnnotationNavigator(m_stText);
			int i = 0;

			foreach (var occurrence in navigator.GetAnalysisOccurrencesAdvancingInStText())
			{
				Assert.AreEqual(m_expectedOccurrences[i], occurrence);
				i++;
			}
			//ensure the all paragraphs in the stText were processed.
			Assert.AreEqual(m_expectedOccurrences.Count, i);
		}

		/// <summary>
		/// 0		1		2		3    4 5	6	   7
		/// Xxxhope xxxthis xxxwill xxxdo. xxxI xxxhope.
		/// 01234567890123456789012345678901234567890123
		///
		/// 8          9     10       11   12     13 14           15     16    17      18       19
		/// Xxxcertain xxxto xxxcatch xxxa xxxfrog.  xxxCertainly xxxcan xxxon xxxLake xxxMonroe.
		/// </summary>
		[Test]
		public void AdvanceThroughEachWordformInTheStText()
		{
			var navigator = new SegmentServices.StTextAnnotationNavigator(m_stText);
			int i = 0;

			foreach (var occurrence in navigator.GetWordformsAdvancingInStText())
			{
				if (i == 4 || i == 7 || i == 13)
					i++;
				Assert.AreEqual(m_expectedOccurrences[i], occurrence);
				i++;
			}
			//ensure all occurrences of both paragraphs were processed.
			Assert.AreEqual(m_expectedOccurrences.Count - 1, i);
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void BackwardsThroughEachAnalysisOccurrenceInThePara()
		{
			int i = m_expectedOccurrencesPara0.Count - 1;

			foreach (var occurrence in SegmentServices.StTextAnnotationNavigator.GetAnalysisOccurrencesBackwardsInPara(m_para0))
			{
				Assert.AreEqual(m_expectedOccurrencesPara0[i], occurrence);
				i--;
			}
		}

		/// <summary>
		/// 0		1		2		3    4 5	6	   7
		/// Xxxhope xxxthis xxxwill xxxdo. xxxI xxxhope.
		/// 01234567890123456789012345678901234567890123
		///
		/// 8          9     10       11   12     13 14           15     16    17      18       19
		/// Xxxcertain xxxto xxxcatch xxxa xxxfrog.  xxxCertainly xxxcan xxxon xxxLake xxxMonroe.
		/// </summary>
		[Test]
		public void BackwardsThroughEachAnalysisOccurrenceInTheStText()
		{
			var navigator = new SegmentServices.StTextAnnotationNavigator(m_stText);
			int i = m_expectedOccurrences.Count - 1;

			foreach (var occurrence in navigator.GetAnalysisOccurrencesBackwardsInStText())
			{
				Assert.AreEqual(m_expectedOccurrences[i], occurrence);
				i--;
			}
			//ensure all paragraphs in stText were processed and not just the last one.
			Assert.AreEqual(-1, i);
		}

		/// <summary>
		/// 0		1		2		3    4 5	6	   7
		/// Xxxhope xxxthis xxxwill xxxdo. xxxI xxxhope.
		/// 01234567890123456789012345678901234567890123
		///
		/// 8          9     10       11   12     13 14           15     16    17      18       19
		/// Xxxcertain xxxto xxxcatch xxxa xxxfrog.  xxxCertainly xxxcan xxxon xxxLake xxxMonroe.
		/// </summary>
		[Test]
		public void BackwardsThroughEachWordformOccurrenceInTheStText()
		{
			var navigator = new SegmentServices.StTextAnnotationNavigator(m_stText);
			int i = m_expectedOccurrences.Count - 2;

			foreach (var occurrence in navigator.GetWordformOccurrencesBackwardsInStText())
			{
				if (i == 4 || i == 7 || i == 13)
					i--;
				Assert.AreEqual(m_expectedOccurrences[i], occurrence);
				i--;
			}
			//ensure all paragraphs in stText were processed and not just the last one.
			Assert.AreEqual(-1, i);
		}

		#endregion



	}
}
