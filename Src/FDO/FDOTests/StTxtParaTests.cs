// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: StTxtParaTests.cs
// Responsibility: FW Team
// ---------------------------------------------------------------------------------------------
using NUnit.Framework;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class StTxtParaTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		#region Member variables
		private IStText m_stText;
		#endregion

		#region Setup/Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the test data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			IText text = AddInterlinearTextToLangProj("My Interlinear Text");
			m_stText = text.ContentsOA;
		}
		#endregion

		#region ReplaceTextRange method tests
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ReplaceTextRange when appending text from another paragraph.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ReplaceTextRange_Append()
		{
			IStTxtPara para1 = AddParaToMockedText(m_stText, "Monkey");
			AddRunToMockedPara(para1, "This is text.", null);
			AddSegmentTrans(para1, 0, "Hello");
			para1.ParseIsCurrent = true;

			IStTxtPara para2 = AddParaToMockedText(m_stText, "Monkey");
			AddRunToMockedPara(para2, "So what?", null);
			AddSegmentTrans(para2, 0, "there");
			para2.ParseIsCurrent = true;

			para1.ReplaceTextRange(para1.Contents.Length, para1.Contents.Length, para2, 0, para2.Contents.Length);

			VerifyPara(para1, "This is text.So what?");
			VerifyParaSegments(para1, "Hello", "there");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ReplaceTextRange when appending text from another paragraph when
		/// paragraph one ends with non-wordforming characters and paragraph two starts with
		/// non-wordforming characters. (TE-9287)
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ReplaceTextRange_Append_WithNWFC()
		{
			IStTxtPara para1 = AddParaToMockedText(m_stText, "Monkey");
			AddRunToMockedPara(para1, "'This is text.'", null);
			AddSegmentTrans(para1, 0, "Hello");
			para1.ParseIsCurrent = true;

			IStTxtPara para2 = AddParaToMockedText(m_stText, "Monkey");
			AddRunToMockedPara(para2, "'So what?'", null);
			AddSegmentTrans(para2, 0, "there");
			para2.ParseIsCurrent = true;

			para1.ReplaceTextRange(para1.Contents.Length, para1.Contents.Length, para2, 0, para2.Contents.Length);

			VerifyPara(para1, "'This is text.''So what?'");
			VerifyParaSegments(para1, "Hello", "there");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ReplaceTextRange when inserting text from another paragraph.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ReplaceTextRange_InsertAtBeginning()
		{
			IStTxtPara para1 = AddParaToMockedText(m_stText, "Monkey");
			AddRunToMockedPara(para1, "This is text.", null);
			AddSegmentTrans(para1, 0, "Hello");
			para1.ParseIsCurrent = true;

			IStTxtPara para2 = AddParaToMockedText(m_stText, "Monkey");
			AddRunToMockedPara(para2, "So what?", null);
			AddSegmentTrans(para2, 0, "there");
			para2.ParseIsCurrent = true;

			para1.ReplaceTextRange(0, 0, para2, 0, para2.Contents.Length);

			VerifyPara(para1, "So what?This is text.");
			VerifyParaSegments(para1, "there", "Hello");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ReplaceTextRange when inserting text from another paragraph when
		/// there are non-wordforming characters at the segment boundaries. (TE-9287)
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ReplaceTextRange_InsertAtBeginning_WithNWFC()
		{
			IStTxtPara para1 = AddParaToMockedText(m_stText, "Monkey");
			AddRunToMockedPara(para1, "'This is text.'", null);
			AddSegmentTrans(para1, 0, "Hello");
			para1.ParseIsCurrent = true;

			IStTxtPara para2 = AddParaToMockedText(m_stText, "Monkey");
			AddRunToMockedPara(para2, "'So what?'", null);
			AddSegmentTrans(para2, 0, "there");
			para2.ParseIsCurrent = true;

			para1.ReplaceTextRange(0, 0, para2, 0, para2.Contents.Length);

			VerifyPara(para1, "'So what?''This is text.'");
			VerifyParaSegments(para1, "there", "Hello");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ReplaceTextRange when replacing the end of the first paragraph with
		/// the contents of the second replacing the last full segment.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ReplaceTextRange_ReplaceRangeAtEnd_WholeSegment()
		{
			IStTxtPara para1 = AddParaToMockedText(m_stText, "Monkey");
			AddRunToMockedPara(para1, "This is text. This text might be gone.", null);
			AddSegmentTrans(para1, 0, "My");
			AddSegmentTrans(para1, 1, "text");
			para1.ParseIsCurrent = true;

			IStTxtPara para2 = AddParaToMockedText(m_stText, "Monkey");
			AddRunToMockedPara(para2, "So what?", null);
			AddSegmentTrans(para2, 0, "there");
			para2.ParseIsCurrent = true;

			const int ich = 14; // right before 'This'
			para1.ReplaceTextRange(ich, para1.Contents.Length, para2, 0, para2.Contents.Length);

			VerifyPara(para1, "This is text. So what?");
			VerifyParaSegments(para1, "My", "there");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ReplaceTextRange when replacing the end of the first paragraph with
		/// the contents of the second replacing part of the last segment.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ReplaceTextRange_ReplaceRangeAtEnd_PartialSegment()
		{
			IStTxtPara para1 = AddParaToMockedText(m_stText, "Monkey");
			AddRunToMockedPara(para1, "This is text. This text might be gone.", null);
			AddSegmentTrans(para1, 0, "My");
			AddSegmentTrans(para1, 1, "text");
			para1.ParseIsCurrent = true;

			IStTxtPara para2 = AddParaToMockedText(m_stText, "Monkey");
			AddRunToMockedPara(para2, "So what?", null);
			AddSegmentTrans(para2, 0, "there");
			para2.ParseIsCurrent = true;

			const int ich = 19; // right after the space following 'This'
			para1.ReplaceTextRange(ich, para1.Contents.Length, para2, 0, para2.Contents.Length);

			VerifyPara(para1, "This is text. This So what?");
			VerifyParaSegments(para1, "My", "text there");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ReplaceTextRange when replacing the end of the first paragraph with
		/// the contents of the second.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ReplaceTextRange_ReplaceRangeAtBeginning_WholeSegment()
		{
			IStTxtPara para1 = AddParaToMockedText(m_stText, "Monkey");
			AddRunToMockedPara(para1, "This is text. This text might be gone.", null);
			AddSegmentTrans(para1, 0, "My");
			AddSegmentTrans(para1, 1, "text");
			para1.ParseIsCurrent = true;

			IStTxtPara para2 = AddParaToMockedText(m_stText, "Monkey");
			AddRunToMockedPara(para2, "So what?", null);
			AddSegmentTrans(para2, 0, "there");
			para2.ParseIsCurrent = true;

			const int ich = 14; // right before the second 'This'
			para1.ReplaceTextRange(0, ich, para2, 0, para2.Contents.Length);

			VerifyPara(para1, "So what?This text might be gone.");
			VerifyParaSegments(para1, "there", "text");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ReplaceTextRange when replacing the end of the first paragraph with
		/// the contents of the second.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ReplaceTextRange_ReplaceRangeAtBeginning_PartialSegment()
		{
			IStTxtPara para1 = AddParaToMockedText(m_stText, "Monkey");
			AddRunToMockedPara(para1, "This is text. This text might be gone.", null);
			AddSegmentTrans(para1, 0, "My");
			AddSegmentTrans(para1, 1, "text");
			para1.ParseIsCurrent = true;

			IStTxtPara para2 = AddParaToMockedText(m_stText, "Monkey");
			AddRunToMockedPara(para2, "So what?", null);
			AddSegmentTrans(para2, 0, "there");
			para2.ParseIsCurrent = true;

			const int ich = 8; // right after the space following 'is'
			para1.ReplaceTextRange(0, ich, para2, 0, para2.Contents.Length);

			VerifyPara(para1, "So what?text. This text might be gone.");
			VerifyParaSegments(para1, "there", "My", "text");
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a free translation and analyses to the specified segment on the specified
		/// paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void AddSegmentTrans(IStTxtPara para, int iSeg, string transFT)
		{
			AddSegmentFt(para, iSeg, transFT, para.Cache.DefaultAnalWs);
			ISegment seg = para.SegmentsOS[iSeg];
			FdoTestHelper.CreateAnalyses(seg, para.Contents, seg.BeginOffset, seg.EndOffset, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies the state of the specified paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void VerifyPara(IStTxtPara para, string contents)
		{
			Assert.AreEqual(contents, para.Contents.Text);
			Assert.AreEqual(1, para.Contents.RunCount);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies the free translations for the segments of the specified paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void VerifyParaSegments(IStTxtPara para, params string[] segmentFTs)
		{
			Assert.AreEqual(segmentFTs.Length, para.SegmentsOS.Count);
			for (int i = 0; i < segmentFTs.Length; i++)
			{
				Assert.AreEqual(segmentFTs[i], para.SegmentsOS[i].FreeTranslation.AnalysisDefaultWritingSystem.Text,
					"Free translation for segment " + i + " is wrong");
				FdoTestHelper.VerifyAnalysis(para.SegmentsOS[i], i, new int[0], new int[0]);
			}
		}
		#endregion
	}
}
