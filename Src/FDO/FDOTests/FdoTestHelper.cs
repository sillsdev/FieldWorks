// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FdoTestHelper.cs
// Responsibility: FW Team

using System;
using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.Common.COMInterfaces;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;
using SIL.Utils;
using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Various helper functions for testing FDO code
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class FdoTestHelper
	{
		private static readonly char[] kPunctuationForms = new [] { '<', '>', '.', '!', '?',
			',', '\'', '\"', StringUtils.kChHardLB};// numbers wordforming LT-10746, '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates analyses for the words and punctuation forms of the specified segment. Words
		/// will have WfiWordforms and glosses created if they don't already exist.
		/// </summary>
		/// <param name="segment">The segment.</param>
		/// <param name="paraContents">The para contents.</param>
		/// <param name="ichBeginOffset">The beginning character offset.</param>
		/// <param name="ichLimOffset">The character offset limit.</param>
		/// ------------------------------------------------------------------------------------
		public static void CreateAnalyses(ISegment segment, ITsString paraContents,
			int ichBeginOffset, int ichLimOffset)
		{
			CreateAnalyses(segment, paraContents, ichBeginOffset, ichLimOffset, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates analyses for the words and punctuation forms of the specified segment. Words
		/// will have WfiWordforms created if they don't already exist.
		/// </summary>
		/// <param name="segment">The segment.</param>
		/// <param name="paraContents">The para contents.</param>
		/// <param name="ichBeginOffset">The beginning character offset.</param>
		/// <param name="ichLimOffset">The character offset limit.</param>
		/// <param name="fCreateGlosses">if set to <c>true</c> create glosses in addition to the
		/// WfiWordforms for each surface form.</param>
		/// ------------------------------------------------------------------------------------
		public static void CreateAnalyses(ISegment segment, ITsString paraContents,
			int ichBeginOffset, int ichLimOffset, bool fCreateGlosses)
		{
			FdoCache cache = segment.Cache;
			IFdoServiceLocator servloc = cache.ServiceLocator;
			if (SegmentBreaker.HasLabelText(paraContents, ichBeginOffset, ichLimOffset))
			{
				IPunctuationForm labelPunc = servloc.GetInstance<IPunctuationFormFactory>().Create();
				segment.AnalysesRS.Add(labelPunc);
				labelPunc.Form = paraContents.GetSubstring(ichBeginOffset, ichLimOffset);
			}
			else
			{
				ParseSegBaseline(segment, ichBeginOffset, ichLimOffset, (iForm, word, iAnalysis) =>
					{
						CreateAnalysisForWord(word, segment, cache.DefaultAnalWs, fCreateGlosses);
						return true;
					},
					(sPunc, iAnalysis) => CreatePuncForm(segment, cache.TsStrFactory.MakeString(sPunc, cache.DefaultVernWs)),
					(ichOrc, iAnalysis) => CreatePuncForm(segment, paraContents.Substring(segment.BeginOffset + ichOrc, 1)));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the requested punctuation form and adds it to the segment's analyses.
		/// </summary>
		/// <param name="segment">The segment.</param>
		/// <param name="form">The form.</param>
		/// ------------------------------------------------------------------------------------
		private static void CreatePuncForm(ISegment segment, ITsString form)
		{
			IPunctuationForm puncForm = segment.Services.GetInstance<IPunctuationFormFactory>().Create();
			segment.AnalysesRS.Add(puncForm);
			puncForm.Form = form;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates an analysis for a word.
		/// </summary>
		/// <param name="word">The word which will have an analysis added.</param>
		/// <param name="segment">The segment containing the word.</param>
		/// <param name="ws">The writing system used for the gloss.</param>
		/// <param name="fCreateGlosses">if set to <c>true</c> create a gloss in addition to the
		/// WfiWordform.</param>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "cache is a reference")]
		private static void CreateAnalysisForWord(string word, ISegment segment, int ws,
			bool fCreateGlosses)
		{
			FdoCache cache = segment.Cache;
			IWfiWordformFactory wfFactory = cache.ServiceLocator.GetInstance<IWfiWordformFactory>();
			IWfiWordformRepository wfRepo = cache.ServiceLocator.GetInstance<IWfiWordformRepository>();
			IWfiGlossFactory glossFactory = cache.ServiceLocator.GetInstance<IWfiGlossFactory>();
			IWfiAnalysisFactory wfiAnalysisFactory = cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>();

			ITsString tssForm = cache.TsStrFactory.MakeString(word, cache.DefaultVernWs);
			IWfiWordform form;
			IAnalysis analysis;
			if (wfRepo.TryGetObject(tssForm, out form))
				analysis = (fCreateGlosses) ? (IAnalysis)form.AnalysesOC.First().MeaningsOC.First() : form;
			else
			{
				analysis = form = wfFactory.Create(tssForm);
				IWfiAnalysis actualWfiAnalysis = wfiAnalysisFactory.Create();
				form.AnalysesOC.Add(actualWfiAnalysis);
				if (fCreateGlosses)
				{
					IWfiGloss gloss = glossFactory.Create();
					actualWfiAnalysis.MeaningsOC.Add(gloss);
					gloss.Form.set_String(ws, "G" + word + "g");
					analysis = gloss;
				}
			}
			segment.AnalysesRS.Add(analysis);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies the analysis for the given segment.
		/// </summary>
		/// <param name="seg">The segment.</param>
		/// <param name="iSeg">The index of the segment in the paragraph.</param>
		/// <param name="expectedFormsWithoutAnalyses">List of indices of wordforms which are
		/// not expected to have any analyses.</param>
		/// <param name="expectedWordforms">List of indices of analyses that are expected to be
		/// word forms, rather than glosses (these should correspond to whole words that did not
		/// survive the resegmentation)</param>
		/// ------------------------------------------------------------------------------------
		public static void VerifyAnalysis(ISegment seg, int iSeg,
			IEnumerable<int> expectedFormsWithoutAnalyses, IEnumerable<int> expectedWordforms)
		{
			string baseline = seg.BaselineText.Text;

			if (seg.IsLabel)
			{
				// We don't really care about analyses in label segments, but if we do have one, it better
				// be a punctuation form that exactly matches the segment's baseline.
				Assert.IsTrue(seg.AnalysesRS.Count == 0 ||
					(seg.AnalysesRS.Count == 1 && ((IPunctuationForm)seg.AnalysesRS[0]).Form.Text == baseline));
				return;
			}

			int cAnalysis = ParseSegBaseline(seg, seg.BeginOffset, seg.EndOffset,
				(iForm, word, iAnalysis) => VerifyWfi(seg, iForm, word, iAnalysis, expectedFormsWithoutAnalyses, expectedWordforms),
				(puncForm, iAnalysis) => VerifyPunctuationForm(seg, puncForm, iAnalysis),
				(ich, iAnalysis) =>
				{
					ITsString form = VerifyPunctuationForm(seg, StringUtils.kszObject, iAnalysis);
					FwObjDataTypes odt, odtExpected;
					TsRunInfo tri;
					ITsTextProps ttp;
					Guid guid = TsStringUtils.GetGuidFromRun(form, 0, out odt, out tri, out ttp, null);
					Assert.AreEqual(TsStringUtils.GetGuidFromProps(
						seg.BaselineText.get_PropertiesAt(ich), null, out odtExpected), guid);
					Assert.AreEqual(odtExpected, odt);
				});

			Assert.AreEqual(cAnalysis, seg.AnalysesRS.Count, "Segment " + iSeg + " (" +
				seg.BaselineText.Text + ") had too many analyses");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the ith analysis is the expected punctuation form.
		/// </summary>
		/// <param name="seg">The segment whose analyses are being verified.</param>
		/// <param name="puncForm">The expected punctuation form.</param>
		/// <param name="iAnalysis">The index of the analysis which we expect to be the
		/// punctuation form corresponding to the given character.</param>
		/// <returns>The TsString of the form (in case the caller wants to check more stuff)
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private static ITsString VerifyPunctuationForm(ISegment seg, string puncForm, int iAnalysis)
		{
			IPunctuationForm punc = seg.AnalysesRS[iAnalysis] as IPunctuationForm;
			Assert.IsNotNull(punc, "Expected analysis " + iAnalysis + " to be a punctuation form for: " + puncForm);
			ITsString form = punc.Form;
			Assert.AreEqual(puncForm, form.Text);
			return form;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks that the specified analysis of the given segment is a gloss for the given word.
		/// </summary>
		/// <param name="seg">The segment.</param>
		/// <param name="iForm">The index of the word (or punctuation) form.</param>
		/// <param name="word">The (vernacular) word.</param>
		/// <param name="iAnalysis">The index into the collection of analyses.</param>
		/// <param name="expectedFormsWithoutAnalyses">List of indices of wordforms which are
		/// not expected to have any analyses.</param>
		/// <param name="expectedWordforms">List of indices of analyses that are expected to be
		/// word forms, rather than glosses (these should correspond to whole words that did not
		/// survive the resegmentation)</param>
		/// ------------------------------------------------------------------------------------
		private static bool VerifyWfi(ISegment seg, int iForm, string word, int iAnalysis,
			IEnumerable<int> expectedFormsWithoutAnalyses, IEnumerable<int> expectedWordforms)
		{
			if (expectedFormsWithoutAnalyses.Contains(iForm))
			{
				Assert.AreNotEqual(word, seg.AnalysesRS[iAnalysis].GetForm(seg.Cache.DefaultVernWs).Text,
					"Did not expect to find an analysis for form " + iForm + " (" + word + ") at position " + iAnalysis +
					" in the collection of analyses for this segment.");
				return false;
			}
			if (expectedWordforms.Contains(iAnalysis))
			{
				IWfiWordform wordform = seg.AnalysesRS[iAnalysis] as IWfiWordform;
				Assert.IsNotNull(wordform, "Expected form " + iForm + " (" + word + ") to have an analysis, which is a wordform.");
				Assert.AreEqual(word, wordform.Form.VernacularDefaultWritingSystem.Text);
			}
			else
			{
				IWfiGloss gloss = seg.AnalysesRS[iAnalysis] as IWfiGloss;
				Assert.IsNotNull(gloss, "Expected form " + iForm + " (" + word + ") to have an analysis, which is a gloss.");
				Assert.AreEqual("G" + word + "g", gloss.Form.AnalysisDefaultWritingSystem.Text);
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parses the given segment, calling the correct handler for each word and punctuation
		/// token found.
		/// </summary>
		/// <param name="seg">The segment.</param>
		/// <param name="ichBeginOffset">The index of the character offset relative to the
		/// paragraph that owns the segment at which the segment begins or should be assumed to
		/// begin if it doesn't really.</param>
		/// <param name="ichLimOffset">The limit (see above).</param>
		/// <param name="handleWord">The delegate to handle each word in the segment.</param>
		/// <param name="handlePunc">The delegate to handle each group of contiguous punctuation
		/// characters.</param>
		/// <param name="handleORC">The delegate to handle each ORC.</param>
		/// <returns>
		/// The number of tokens (corresponding to analyses) handled
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private static int ParseSegBaseline(ISegment seg, int ichBeginOffset, int ichLimOffset,
			Func<int, string, int, bool> handleWord, Action<string, int> handlePunc,
			Action<int, int> handleORC)
		{
			string baseline = seg.Paragraph.Contents.Text.Substring(ichBeginOffset, ichLimOffset - ichBeginOffset);
			string word = string.Empty;
			string puncForm = string.Empty;
			int iAnalysis = 0, iForm = 0;

			for (int ich = 0; ich < baseline.Length; ich++)
			{
				char ch = baseline[ich];
				if (kPunctuationForms.Contains(ch))
				{
					if (word.Length > 0)
					{
						if (handleWord(iForm++, word, iAnalysis))
							iAnalysis++;
						word = string.Empty;
					}
					puncForm += ch;
				}
				else switch (ch)
				{
					case StringUtils.kChObject:
						// Validate ORC analysis.
						if (word.Length > 0)
						{
							if (handleWord(iForm++, word, iAnalysis))
								iAnalysis++;
							word = string.Empty;
						}
						else if (puncForm.Length > 0)
						{
							handlePunc(puncForm, iAnalysis++);
							puncForm = string.Empty;
							iForm++;
						}
						handleORC(ich, iAnalysis++);
						iForm++;
						break;
					case ' ':
						if (word.Length > 0)
						{
							if (handleWord(iForm++, word, iAnalysis))
								iAnalysis++;
							word = string.Empty;
						}
						else if (puncForm.Length > 0)
						{
							handlePunc(puncForm, iAnalysis++);
							puncForm = string.Empty;
							iForm++;
						}
						break;
					default:
						if (puncForm.Length > 0)
						{
							handlePunc(puncForm, iAnalysis++);
							puncForm = string.Empty;
							iForm++;
						}
						word += ch;
						break;
				}
			}
			if (word.Length > 0)
			{
				if (handleWord(iForm, word, iAnalysis))
					iAnalysis++;
			}
			else if (puncForm.Length > 0)
				handlePunc(puncForm, iAnalysis++);
			return iAnalysis;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the marker for a footnote exists in the back translation and refers to the
		/// footnote properly.
		/// </summary>
		/// <param name="footnote">given footnote whose marker we want to verify in the BT</param>
		/// <param name="para">vernacular paragraph which owns the back translation</param>
		/// <param name="ws">writing system of the back transltion</param>
		/// <param name="ich">Character position where ORC should be in the specified back
		/// translation</param>
		/// ------------------------------------------------------------------------------------
		public static void VerifyBtFootnote(IStFootnote footnote, IStTxtPara para, int ws, int ich)
		{
			ICmTranslation trans = para.GetBT();
			ITsString btTss = trans.Translation.get_String(ws);
			int iRun = btTss.get_RunAt(ich);

			Guid newFootnoteGuid = TsStringUtils.GetGuidFromRun(btTss, iRun, FwObjDataTypes.kodtNameGuidHot);
			Assert.AreEqual(footnote.Guid, newFootnoteGuid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure footnote exists and is referred to properly in the paragraph contents
		/// </summary>
		/// <param name="footnote"></param>
		/// <param name="para"></param>
		/// <param name="ich">Character position where ORC should be</param>
		/// ------------------------------------------------------------------------------------
		public static void VerifyFootnote(IStFootnote footnote, IStTxtPara para, int ich)
		{
			ITsString tss = para.Contents;
			int iRun = tss.get_RunAt(ich);
			Guid newFootnoteGuid = TsStringUtils.GetGuidFromRun(tss, iRun, FwObjDataTypes.kodtOwnNameGuidHot);
			Assert.AreEqual(footnote.Guid, newFootnoteGuid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the marker for the specified footnote exists in the free translation of
		/// the specified segment in the specified paragraph at the specified character position.
		/// </summary>
		/// <param name="footnote">The footnote</param>
		/// <param name="para">vernacular paragraph which owns the segment</param>
		/// <param name="iSeg">The index of the segment.</param>
		/// <param name="ws">the writing system</param>
		/// <param name="ich">Character position where ORC should be in the specified segment</param>
		/// ------------------------------------------------------------------------------------
		public static void VerifyFootnoteInSegmentFt(IStFootnote footnote, IStTxtPara para,
			int iSeg, int ws, int ich)
		{
			ISegment segment = para.SegmentsOS[iSeg];
			ITsString tss = segment.FreeTranslation.get_String(ws);
			int iRun = tss.get_RunAt(ich);
			Guid newFootnoteGuid = TsStringUtils.GetGuidFromRun(tss, iRun, FwObjDataTypes.kodtNameGuidHot);
			Assert.AreEqual(footnote.Guid, newFootnoteGuid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Asserts the properties of a run are set correctly to be a hyperlink for the given
		/// URL.
		/// </summary>
		/// <param name="props">The properties.</param>
		/// <param name="expectedWs">The expected writing system.</param>
		/// <param name="sUrl">The URL.</param>
		/// ------------------------------------------------------------------------------------
		public static void VerifyHyperlinkPropsAreCorrect(ITsTextProps props, int expectedWs,
			string sUrl)
		{
			Assert.AreEqual(1, props.IntPropCount);
			int nDummy;
			Assert.AreEqual(expectedWs,
				props.GetIntPropValues((int)FwTextPropType.ktptWs, out nDummy));
			Assert.AreEqual(2, props.StrPropCount);
			Assert.AreEqual(StyleServices.Hyperlink,
				props.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			string sObjData = props.GetStrPropValue((int)FwTextPropType.ktptObjData);
			Assert.AreEqual(Convert.ToChar((int)FwObjDataTypes.kodtExternalPathName), sObjData[0]);
			Assert.AreEqual(sUrl, sObjData.Substring(1));
		}

		/// <summary>
		/// Setup static FDO properties
		/// </summary>
		public static void SetupStaticFdoProperties()
		{
			ClientServerServices.SetCurrentToDb4OBackend(new DummyFdoUI(), FwDirectoryFinder.FdoDirectories);
			ScrMappingList.TeStylesPath = FwDirectoryFinder.TeStylesPath;
		}
	}
}
