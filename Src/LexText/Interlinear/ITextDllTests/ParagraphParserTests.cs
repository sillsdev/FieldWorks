using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.FDOTests;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.XWorks;
using SIL.Utils;

namespace SIL.FieldWorks.IText
{

	public class InterlinearTestBase : InDatabaseFdoTestBase
	{
		static private int s_kSegmentAnnType = -1;
		static protected int s_kTwficAnnType = -1;
		static private int s_kPunctuationAnnType = -1;

		protected XCore.Mediator m_mediator = null;

		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			CheckDisposed();
			base.FixtureSetup();

			s_kSegmentAnnType = CmAnnotationDefn.TextSegment(Cache).Hvo;
			s_kTwficAnnType = CmAnnotationDefn.Twfic(Cache).Hvo;
			s_kPunctuationAnnType = CmAnnotationDefn.Punctuation(Cache).Hvo;
		}

		protected override List<IVwVirtualHandler> InstallVirtuals(XmlNode virtuals)
		{
			return PropertyTableVirtualHandler.InstallVirtuals(virtuals, Cache, Mediator);
		}

		/// <summary>
		/// Set to true, if the Mediator is created by this object.
		/// That is the only one we can dispose, since the one provided by
		/// the window has to dispose it, since it created it.
		/// </summary>
		protected bool m_locallyOwnedMediator = false;
		protected virtual XCore.Mediator Mediator
		{
			get
			{
				if (m_mediator == null)
				{
					m_mediator = new XCore.Mediator();
					m_locallyOwnedMediator = true;
				}
				return m_mediator;
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (m_locallyOwnedMediator && m_mediator != null && !m_mediator.IsDisposed)
					m_mediator.Dispose();
			}

			// Calling Application.DoEvents here helps with FWC-16. It gives the SQLNCLI dll
			// time to clean up so that we can unload before we possibly load it again with the
			// next text fixture.
			Application.DoEvents();

			base.Dispose(disposing);

			m_mediator = null;
		}

		static internal int SegmentAnnotationType
		{
			get { return s_kSegmentAnnType; }
		}

		static internal int TwficAnnotationType
		{
			get { return s_kTwficAnnType; }
		}

		static internal int PunctuationAnnotationType
		{
			get { return s_kPunctuationAnnType; }
		}

		protected string ConfigurationFilePath(string fileRelativePath)
		{
			return Path.Combine(SIL.FieldWorks.Common.Utils.DirectoryFinder.FwSourceDirectory, fileRelativePath);
		}

		protected FDO.IText LoadTestText(string textsDefinitionRelativePath, int index, XmlDocument textsDefn)
		{
			TextBuilder tb;
			return LoadTestText(textsDefinitionRelativePath, index, textsDefn, out tb);
		}

		protected FDO.IText LoadTestText(string textsDefinitionRelativePath, int index, XmlDocument textsDefn, out TextBuilder tb)
		{
			string textsDefinitionsPath = ConfigurationFilePath(textsDefinitionRelativePath);
			textsDefn.Load(textsDefinitionsPath);
			XmlNode text1Defn = textsDefn.SelectSingleNode("/Texts6001/Text[" + index + "]");
			tb = new ParagraphParserTests.TextBuilder(Cache.LangProject.TextsOC);
			return tb.BuildText(text1Defn);
		}

		static internal List<XmlNode> NodeListToNodes(XmlNodeList nl)
		{
			List<XmlNode> nodes = new List<XmlNode>();
			foreach (XmlNode node in nl)
				nodes.Add(node);
			return nodes;
		}

		static internal void MoveSiblingNodes(XmlNode srcStartNode, XmlNode targetParentNode, XmlNode limChildNode)
		{
			XmlNode srcParentNode = srcStartNode.ParentNode;
			List<XmlNode> siblingNodes = NodeListToNodes(srcParentNode.ChildNodes);
			bool fStartFound = false;
			foreach (XmlNode siblingNode in siblingNodes)
			{
				if (!fStartFound && siblingNode != srcStartNode)
					continue;
				fStartFound = true;
				// break after we've added the limiting segment form node.
				XmlNode orphan = srcParentNode.RemoveChild(siblingNode);
				targetParentNode.AppendChild(orphan);
				if (siblingNode == limChildNode)
					break;
			}
		}

		/// <summary>
		/// This abstract class can be used to validate any paragraph structure against an existing StTxtPara structure.
		/// </summary>
		abstract internal class ParagraphValidator
		{
			internal ParagraphValidator()
			{
			}

			virtual internal void ValidateParagraphs(object expectedParaInfo, object actualParaInfo)
			{
				string paraContext = String.Format("Para({0})", GetParagraphContext(expectedParaInfo));
				ValidateParagraphOuterElements(expectedParaInfo, actualParaInfo, paraContext);
				ValidateParagraphSegments(expectedParaInfo, actualParaInfo, paraContext);
			}

			virtual protected void ValidateParagraphOuterElements(object expectedParaInfo, object actualParaInfo, string paraContext)
			{
				// base override.
				return;
			}

			virtual protected void ValidateParagraphSegments(object expectedParaInfo, object actualParaInfo, string paraContext)
			{
				ArrayList expectedSegments = GetExpectedSegments(expectedParaInfo);
				ArrayList actualSegments = GetActualSegments(actualParaInfo);
				// Validate Segments
				Assert.AreEqual(expectedSegments.Count, actualSegments.Count, String.Format("Expect the same Segment count in {0}.", paraContext));
				IEnumerator actualSegEnumerator = actualSegments.GetEnumerator();
				int iSegment = 0;
				foreach (object expectedSegment in expectedSegments)
				{
					string segmentContext = String.Format(paraContext + "/Segment({0})", iSegment);
					actualSegEnumerator.MoveNext();
					object actualSegment = actualSegEnumerator.Current;
					ValidateSegmentOuterElements(expectedSegment, actualSegment, segmentContext);
					ValidateSegmentSegForms(expectedSegment, actualSegment, segmentContext);
					iSegment++;
				}
			}

			virtual protected void ValidateSegmentOuterElements(object expectedSegment, object actualSegment, string segmentContext)
			{
				// base override
				return;
			}

			virtual protected void ValidateSegmentSegForms(object expectedSegment, object actualSegment, string segmentContext)
			{
				ArrayList expectedSegForms = GetExpectedSegmentForms(expectedSegment);
				ArrayList actualSegForms = GetActualSegmentForms(actualSegment);
				// Validate Segments
				Assert.AreEqual(expectedSegForms.Count, actualSegForms.Count, String.Format("Expect the same SegmentForm count in {0}.", segmentContext));
				IEnumerator actualSegFormEnumerator = actualSegForms.GetEnumerator();
				int iSegForm = 0;
				foreach (object expectedSegForm in expectedSegForms)
				{
					string segFormContext = String.Format(segmentContext + "/SegForm({0})", iSegForm);
					actualSegFormEnumerator.MoveNext();
					object actualSegForm = actualSegFormEnumerator.Current;
					ValidateSegFormOuterElements(expectedSegForm, actualSegForm, segFormContext);
					ValidateSegForms(expectedSegForm, actualSegForm, segFormContext);
					iSegForm++;
				}
			}

			virtual protected void ValidateSegFormOuterElements(object expectedSegForm, object actualSegForm, string segFormContext)
			{
				return;
			}
			virtual protected void ValidateSegForms(object expectedSegForm, object actualSegForm, string segFormContext)
			{
				return;
			}

			abstract protected string GetParagraphContext(object expectedParaInfo);
			abstract protected ArrayList GetExpectedSegments(object expectedParaInfo);
			abstract protected ArrayList GetActualSegments(object actualParaInfo);
			abstract protected ArrayList GetExpectedSegmentForms(object expectedSegment);
			abstract protected ArrayList GetActualSegmentForms(object actualSegment);
		}

		internal class FdoValidator : ParagraphValidator
		{
			protected FdoCache m_cache = null;
			protected IStTxtPara m_para = null;

			internal FdoValidator(IStTxtPara para)
			{
				m_para = para;
				m_cache = m_para.Cache;
			}

			internal void CompareCbas(int hvoCbaExpected, int hvoCbaActual, string context)
			{
				ICmBaseAnnotation expectedCba = CmBaseAnnotation.CreateFromDBObject(m_cache, hvoCbaExpected);
				ICmBaseAnnotation actualCba = CmBaseAnnotation.CreateFromDBObject(m_cache, hvoCbaActual);
				IStTxtPara para = expectedCba.BeginObjectRA as IStTxtPara;

				Assert.LessOrEqual(expectedCba.BeginOffset, para.Contents.Length,
					String.Format("expected cba offset {1} is beyond expected text '{0}' length", para.Contents.Text, expectedCba.BeginOffset));
				Assert.LessOrEqual(expectedCba.EndOffset, para.Contents.Length,
					String.Format("expected cba offset {1} is beyond expected text '{0}' length", para.Contents.Text, expectedCba.EndOffset));

				ITsString tssExpected = para.Contents.UnderlyingTsString.GetSubstring(expectedCba.BeginOffset, expectedCba.EndOffset);
				context += String.Format("[{0}]", tssExpected.Text);
				string msg = "{0} cba mismatch in {1}.";
				Assert.AreEqual(expectedCba.AnnotationTypeRAHvo, actualCba.AnnotationTypeRAHvo,
								   String.Format(msg, "AnnotationType", context));
				Assert.AreEqual(expectedCba.BeginObjectRAHvo, actualCba.BeginObjectRAHvo,
								   String.Format(msg, "BeginObject", context));
				Assert.AreEqual(expectedCba.EndObjectRAHvo, actualCba.EndObjectRAHvo,
								   String.Format(msg, "EndObject", context));
				Assert.AreEqual(expectedCba.BeginOffset, actualCba.BeginOffset,
								   String.Format(msg, "BeginOffset", context));
				Assert.AreEqual(expectedCba.EndOffset, actualCba.EndOffset,
								   String.Format(msg, "EndOffset", context));
				// see if we match the actual analysis of the text.
				if (expectedCba.InstanceOfRAHvo != -1)
				{
					CompareCbaInstanceOf(context, expectedCba, actualCba, msg);
				}
				else if (actualCba.AnnotationTypeRAHvo == TwficAnnotationType)
				{
					Assert.AreNotEqual(0, actualCba.InstanceOfRAHvo,
						String.Format(msg, "InstanceOf hvo", context));
					// see if the actual instanceOf is a wordform
					Assert.AreEqual((int)WfiWordform.kClassId, m_cache.GetClassOfObject(actualCba.InstanceOfRAHvo),
						   String.Format(msg, "InstanceOf class", context));
					// For Twfics, make sure the actual annotation's analysis corresponds to the wordform at the offsets in the text.
					Assert.AreNotEqual(0, actualCba.InstanceOfRAHvo,
						String.Format("Unexpected actual cba InstanceOf in {0}.", context));
					int hvoWordformActual = WfiWordform.GetWfiWordformFromInstanceOf(m_cache, actualCba.Hvo);
					IWfiWordform actualWf = WfiWordform.CreateFromDBObject(m_cache, hvoWordformActual);
					ITsString tssActual = actualWf.Form.BestVernacularAlternative;

					// Get the writing systems for each string.
					int nvar;
					int wsExpected = tssExpected.get_Properties(0).GetIntPropValues((int)FwTextPropType.ktptWs, out nvar);
					int wsActual = tssActual.get_Properties(0).GetIntPropValues((int)FwTextPropType.ktptWs, out nvar);
					Assert.AreEqual(wsExpected, wsActual,
						String.Format("Writing System for wordform TsString in text should match the actual cba wordform in {0}.", context));
					string expectedForm = tssExpected.Text;
					string actualForm = tssActual.Text;
					Assert.AreEqual(tssExpected.Text, tssActual.Text,
								   String.Format(msg, "underlying wordform for InstanceOf", context));
				}
			}

			protected virtual void CompareCbaInstanceOf(string context, ICmBaseAnnotation expectedCba, ICmBaseAnnotation actualCba, string msg)
			{
				// we expect these analyses to be identical.
				Assert.AreEqual(expectedCba.InstanceOfRAHvo, actualCba.InstanceOfRAHvo,
				   String.Format(msg, "InstanceOf", context));
			}

			protected override string GetParagraphContext(object expectedParaInfo)
			{
				throw new Exception("The method or operation is not implemented.");
			}

			protected override ArrayList GetExpectedSegments(object expectedParaInfo)
			{
				throw new Exception("The method or operation is not implemented.");
			}

			protected override ArrayList GetActualSegments(object actualParaInfo)
			{
				throw new Exception("The method or operation is not implemented.");
			}

			protected override ArrayList GetExpectedSegmentForms(object expectedSegment)
			{
				throw new Exception("The method or operation is not implemented.");
			}

			protected override ArrayList GetActualSegmentForms(object actualSegment)
			{
				throw new Exception("The method or operation is not implemented.");
			}
		}

		/// <summary>
		/// This validates the actual StTxtPara against an expected Xml paragraph structure that is based on the conceptual model.
		/// </summary>
		internal class ConceptualModelXmlParagraphValidator : FdoValidator
		{
			ParagraphBuilder m_pb = null;

			internal ConceptualModelXmlParagraphValidator(ParagraphBuilder pb) : base (pb.ActualParagraph)
			{
				m_pb = pb;
			}

			override protected string GetParagraphContext(object expectedParaInfo)
			{
				XmlNode expectedParaDefn = expectedParaInfo as XmlNode;
				StringBuilder sb = new StringBuilder();
				sb.Append("[");
				sb.Append(XmlUtils.GetIndexAmongSiblings(expectedParaDefn));
				sb.Append("] ");
				sb.Append(XmlUtils.GetManditoryAttributeValue(expectedParaInfo as XmlNode, "id"));
				return sb.ToString();
			}
			override protected ArrayList GetExpectedSegments(object expectedParaInfo)
			{
				return new ArrayList(ParagraphBuilder.SegmentNodes(expectedParaInfo as XmlNode).ToArray());
			}
			override protected ArrayList GetActualSegments(object actualParaInfo)
			{
				IStTxtPara para = actualParaInfo as IStTxtPara;
				Debug.Assert(para == m_para);
				return new ArrayList(para.Segments.ToArray());
			}
			override protected ArrayList GetExpectedSegmentForms(object expectedSegment)
			{
				return new ArrayList(ParagraphBuilder.SegmentFormNodes(expectedSegment as XmlNode).ToArray());
			}
			override protected ArrayList GetActualSegmentForms(object actualSegment)
			{
				return new ArrayList(m_para.SegmentForms((int)actualSegment).ToArray());
			}

			private void CompareCbas(object expectedSegment, object actualSegment, string context)
			{
				int hvoCbaExpected = ParagraphBuilder.GetCmBaseAnnotationId(expectedSegment as XmlNode);
				base.CompareCbas(hvoCbaExpected, (int)actualSegment, context);
			}
			protected override void ValidateSegmentOuterElements(object expectedSegment, object actualSegment, string segmentContext)
			{
				CompareCbas(expectedSegment, actualSegment, segmentContext);
			}

			protected override void ValidateSegFormOuterElements(object expectedSegForm, object actualSegForm, string segFormContext)
			{
				CompareCbas(expectedSegForm, actualSegForm, segFormContext);
			}

			internal void ValidateActualParagraphAgainstDefn()
			{
				ITsString paraContents = m_pb.GenerateParaContentFromAnnotations();
				Assert.AreEqual(paraContents.Text, m_pb.ActualParagraph.Contents.Text,
					"Expected edited text to be the same as text built from defn.");
				base.ValidateParagraphs(m_pb.ParagraphDefinition, m_pb.ActualParagraph);
			}
		}

		/// <summary>
		/// This class allows annotating twfics in a paragraph.
		/// </summary>
		internal class ParagraphAnnotator
		{
			protected IStTxtPara m_para = null;
			protected FdoCache m_cache = null;
			protected bool m_fNeedReparseParagraph = false;

			internal ParagraphAnnotator(IStTxtPara para)
			{
				m_para = para;
				m_cache = m_para.Cache;
			}

			/// <summary>
			/// If the annotations have changed (e.g. due to a merge), other annotations might also be affected by parsing the
			/// paragraph again.
			/// </summary>
			internal bool NeedReparseParagraph
			{
				get { return m_fNeedReparseParagraph; }
				set { m_fNeedReparseParagraph = value; }
			}

			internal void LoadParaDefaultAnalyses()
			{
				new ParaDataLoader(m_cache).LoadParaData(m_para.Hvo);
			}
			internal void ReparseParagraph()
			{
				ParagraphParser.ParseParagraph(m_para, true, true, false);
				NeedReparseParagraph = false;
			}

			/// <summary>
			/// create a variant and link it to the given leMain entry
			/// and confirm this analysis on the given (monomorphemic) cba.
			/// </summary>
			/// <param name="iSegment"></param>
			/// <param name="iSegForm"></param>
			/// <param name="leMain"></param>
			/// <param name="variantTypeRevAbbr"></param>
			/// <returns>hvo of the resulting LexEntryRef</returns>
			virtual internal ILexEntryRef SetVariantOf(int iSegment, int iSegForm, ILexEntry leMain, string variantTypeRevAbbr)
			{
				ILexEntryType variantType = null;
				foreach (ILexEntryType let in m_cache.LangProject.LexDbOA.VariantEntryTypesOA.ReallyReallyAllPossibilities)
				{
					if (let.ReverseAbbr.AnalysisDefaultWritingSystem == variantTypeRevAbbr)
					{
						variantType = let;
						break;
					}
				}

				// for now, just create the variant entry and the variant of target, treating the wordform as monomorphemic.
				int hvoCbaActual = GetSegmentForm(iSegment, iSegForm);
				ITsString tssVariantLexemeForm = GetBaselineText(hvoCbaActual);
				ILexEntryRef ler = leMain.CreateVariantEntryAndBackRef(variantType, tssVariantLexemeForm);
				ILexEntry variant = (ler as LexEntryRef).Owner as ILexEntry;
				ArrayList morphs = new ArrayList(1);
				morphs.Add(variant.LexemeFormOA);
				BreakIntoMorphs(iSegment, iSegForm, morphs);
				return ler;
			}

			virtual internal int SetAlternateCase(string wordform, int iOccurrenceInParagraph, StringCaseStatus targetState)
			{
				return 0; // override
			}

			virtual internal int SetAlternateCase(int iSegment, int iSegForm, StringCaseStatus targetState, out string alternateCaseForm)
			{
				// Get actual segment form.
				int hvoCbaActual = GetSegmentForm(iSegment, iSegForm);
				int hvoActualInstanceOf;
				IWfiWordform actualCbaWordform;
				GetRealWordformInfo(hvoCbaActual, out hvoActualInstanceOf, out actualCbaWordform);
				ITsString tssWordformBaseline = GetBaselineText(hvoCbaActual);
				// Add any relevant 'other case' forms.
				int nvar;
				int ws = tssWordformBaseline.get_Properties(0).GetIntPropValues((int)FwTextPropType.ktptWs, out nvar); ;
				string locale = m_cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(ws).IcuLocale;
				CaseFunctions cf = new CaseFunctions(locale);
				switch (targetState)
				{
					case StringCaseStatus.allLower:
						alternateCaseForm = cf.ToLower(actualCbaWordform.Form.GetAlternative(ws));
						break;
					default:
						throw new ArgumentException("target StringCaseStatus(" + targetState.ToString() + ") not yet supported.");
				}

				// Find or create the new wordform.
				IWfiWordform wfAlternateCase = WfiWordform.FindOrCreateWordform(m_cache, alternateCaseForm, ws);

				// Set the annotation to this wordform.
				int hvoCbaReal = SetInstanceOf(hvoCbaActual, wfAlternateCase.Hvo, true);

				if (!tssWordformBaseline.Equals(wfAlternateCase.Form.BestVernacularAlternative))
				{
					// Cache the real form.
					m_cache.VwCacheDaAccessor.CacheStringProp(hvoCbaReal, InterlinVc.TwficRealFormTag(m_cache), tssWordformBaseline);
				}
				return wfAlternateCase.Hvo;
			}

			virtual internal int SetDefaultWordGloss(string wordform, int iOccurrenceInParagraph)
			{
				return 0;	// override
			}

			virtual internal int SetDefaultWordGloss(int iSegment, int iSegForm, out string gloss)
			{
				return SetDefaultWordGloss(iSegment, iSegForm, null, out gloss);
			}


			virtual internal int SetDefaultWordGloss(int iSegment, int iSegForm, IWfiAnalysis actualWfiAnalysis, out string gloss)
			{
				gloss = "";
				// Get actual segment form.
				int hvoCbaActual = GetSegmentForm(iSegment, iSegForm);

				// Get the wordform for this segmentForm
				ITsString tssWordformBaseline = GetBaselineText(hvoCbaActual);

				// Find or create the current analysis of the actual annotation.
				if (actualWfiAnalysis == null)
				{
					actualWfiAnalysis = FindOrCreateWfiAnalysis(hvoCbaActual);
				}

				// Make a new gloss based upon the wordform and segmentForm path.
				IWfiGloss newGloss = new WfiGloss();
				// Add the gloss to the WfiAnalysis.
				actualWfiAnalysis.MeaningsOC.Add(newGloss);
				gloss = String.Format("{0}.{1}.{2}", iSegment, iSegForm, tssWordformBaseline.Text);
				newGloss.Form.SetAlternative(gloss, m_cache.DefaultAnalWs);
				int hvoGloss = newGloss.Hvo;

				// Set the new expected and actual analysis.
				SetInstanceOf(hvoCbaActual, hvoGloss, true);
				return hvoGloss;
			}

			/// <summary>
			/// Walk through the paragraph and create word glosses (formatted: wordform.segNum.segFormNum) and segment annotations.
			/// </summary>
			/// <returns>list of wordglosses for each form in the paragraph, including non-twfics (ie. hvo == 0).</returns>
			internal protected virtual List<int> SetupDefaultWordGlosses()
			{
				int kAnnTypeTwfic = CmAnnotationDefn.Twfic(m_cache).Hvo;
				int kAnnTypePunc = CmAnnotationDefn.Punctuation(m_cache).Hvo;

				int iseg = 0;
				List<int> wordGlosses = new List<int>();
				List<int> segsPara = m_para.Segments;
				foreach (int hvoSegPara in segsPara)
				{
					int isegform = 0;
					List<int> segFormsPara = m_para.SegmentForms(hvoSegPara);
					foreach (int hvoSegFormPara in segFormsPara)
					{
						int hvoWordGloss = 0;
						CmBaseAnnotation cba = new CmBaseAnnotation(m_cache, hvoSegFormPara);
						if (cba.AnnotationTypeRAHvo == kAnnTypeTwfic)
						{
							string twficGloss;
							hvoWordGloss = SetDefaultWordGloss(iseg, isegform, out twficGloss);
						}
						wordGlosses.Add(hvoWordGloss);
						isegform++;
					}
					// create freeform annotations for each of the segments
					// TODO: multiple writing systems.
					ITsString tssComment;
					SetDefaultFreeformAnnotation(iseg, LangProject.kguidAnnFreeTranslation, out tssComment);
					SetDefaultFreeformAnnotation(iseg, LangProject.kguidAnnLiteralTranslation, out tssComment);
					SetDefaultFreeformAnnotation(iseg, LangProject.kguidAnnNote.ToString(), out tssComment);

					iseg++;
				}
				return wordGlosses;
			}

			/// <summary>
			/// </summary>
			/// <param name="iSegment"></param>
			/// <param name="guidFreeformAnnotation"></param>
			/// <param name="comment"></param>
			/// <returns></returns>
			internal virtual int SetDefaultFreeformAnnotation(int iSegment, string guidFreeformAnnotation,
				out ITsString tssComment)
			{
				int segDefn = m_cache.GetIdFromGuid(guidFreeformAnnotation);
				BaseFreeformAdder ffAdder = new BaseFreeformAdder(m_cache);
				// add the indirect annotation
				int hvoSegmentActual = GetSegment(iSegment);
				ITsString tssSegment = GetBaselineText(hvoSegmentActual);
				ICmIndirectAnnotation indirectAnn = ffAdder.AddFreeformAnnotation(hvoSegmentActual, segDefn);
				string comment = String.Format("{0}.Type({1}).{2}", iSegment, segDefn, tssSegment.Text);
				indirectAnn.Comment.SetAlternative(comment, m_cache.DefaultAnalWs);
				tssComment = indirectAnn.Comment.AnalysisDefaultWritingSystem.UnderlyingTsString;
				return indirectAnn.Hvo;
			}

			/// <summary>
			/// Finds an existing wfiAnalysis related to the given cba.InstanceOf.
			/// If none exists, we'll create one.
			/// </summary>
			/// <param name="hvoCbaActual"></param>
			/// <returns></returns>
			private IWfiAnalysis FindOrCreateWfiAnalysis(int hvoCbaActual)
			{
				int hvoActualWfiAnalysis = WfiAnalysis.GetWfiAnalysisFromInstanceOf(m_cache, hvoCbaActual);
				IWfiAnalysis actualWfiAnalysis = null;
				if (hvoActualWfiAnalysis == 0)
				{
					actualWfiAnalysis = CreateWfiAnalysisForTwfic(hvoCbaActual);
					hvoActualWfiAnalysis = actualWfiAnalysis.Hvo;
				}
				else
				{
					actualWfiAnalysis = WfiAnalysis.CreateFromDBObject(m_cache, hvoActualWfiAnalysis);
				}
				return actualWfiAnalysis;
			}

			private IWfiAnalysis CreateWfiAnalysisForTwfic(int hvoCbaActual)
			{
				int hvoActualInstanceOf;
				IWfiWordform actualWordform;
				GetRealWordformInfo(hvoCbaActual, out hvoActualInstanceOf, out actualWordform);

				// Create a new WfiAnalysis for the wordform.
				IWfiAnalysis actualWfiAnalysis = new WfiAnalysis();
				actualWordform.AnalysesOC.Add(actualWfiAnalysis);
				return actualWfiAnalysis;
			}

			/// <summary>
			/// Creates a new analysis with the given MoForms.
			/// </summary>
			/// <param name="iSegment"></param>
			/// <param name="iSegForm"></param>
			/// <param name="moForms"></param>
			/// <returns>WfiAnalysis</returns>
			virtual internal IWfiAnalysis BreakIntoMorphs(int iSegment, int iSegForm, ArrayList moForms)
			{
				int hvoCbaActual = GetSegmentForm(iSegment, iSegForm);
				// Find or create the current analysis of the actual annotation.
				IWfiAnalysis actualWfiAnalysis = CreateWfiAnalysisForTwfic(hvoCbaActual);

				// Setup WfiMorphBundle(s)
				foreach (object morphForm in moForms)
				{
					IWfiMorphBundle wmb = actualWfiAnalysis.MorphBundlesOS.Append(new WfiMorphBundle());
					if (morphForm is string)
					{
						// just set the Form.
						wmb.Form.SetVernacularDefaultWritingSystem((string)morphForm);
					}
					else if (morphForm is IMoForm)
					{
						wmb.MorphRA = morphForm as IMoForm;
					}
					else
					{
						throw new ArgumentException("Unexpected class for morphForm.");
					}
				}

				SetInstanceOf(hvoCbaActual, actualWfiAnalysis.Hvo, true);
				return actualWfiAnalysis;
			}

			virtual internal IWfiMorphBundle SetMorphSense(int iSegment, int iSegForm, int iMorphBundle, ILexSense sense)
			{
				int hvoCbaActual = GetSegmentForm(iSegment, iSegForm);
				// Find or create the current analysis of the actual annotation.
				return SetMorphBundle(hvoCbaActual, iMorphBundle, sense);
			}

			protected IWfiMorphBundle SetMorphBundle(int hvoCba, int iMorphBundle, ILexSense sense)
			{
				int hvoWfiAnalysis = WfiAnalysis.GetWfiAnalysisFromInstanceOf(m_cache, hvoCba);
				IWfiAnalysis actualWfiAnalysis = WfiAnalysis.CreateFromDBObject(m_cache, hvoWfiAnalysis);
				actualWfiAnalysis.MorphBundlesOS[iMorphBundle].SenseRA = sense;
				return actualWfiAnalysis.MorphBundlesOS[iMorphBundle];
			}


			virtual internal protected int MergeAdjacentAnnotations(int iSegment, int iSegForm)
			{
				ICmBaseAnnotation cbaMerge = (m_para as StTxtPara).MergeAdjacentSegmentForms(GetSegmentForm(iSegment, iSegForm), new int[] { m_para.Hvo });
				// Recompute other segment forms that match this combination of phrases.
				NeedReparseParagraph = true;
				return cbaMerge.Hvo;
			}

			virtual internal protected List<int> BreakPhrase(int iSegment, int iSegForm)
			{
				ParagraphParser pp = new ParagraphParser(m_para);
				List<int> newCbas = pp.BreakPhraseAnnotation(GetSegmentForm(iSegment, iSegForm), new int[] { m_para.Hvo });
				// Recompute other segment forms that match this combination of phrases.
				NeedReparseParagraph = true;
				pp.Dispose();
				return newCbas;
			}

			private ITsString GetBaselineText(int hvoCbaActual)
			{
				ICmBaseAnnotation cbaActual = CmBaseAnnotation.CreateFromDBObject(m_cache, hvoCbaActual);
				ITsString tssWordformBaseline = m_para.Contents.UnderlyingTsString.GetSubstring(cbaActual.BeginOffset, cbaActual.EndOffset);
				return tssWordformBaseline;
			}

			private void GetRealWordformInfo(int hvoCbaActual, out int hvoActualInstanceOf, out IWfiWordform realWordform)
			{
				hvoActualInstanceOf = m_cache.GetObjProperty(hvoCbaActual, (int)CmAnnotation.CmAnnotationTags.kflidInstanceOf);
				int hvoActualWordform = WfiWordform.GetWfiWordformFromInstanceOf(m_cache, hvoCbaActual);
				realWordform = null;
				if (m_cache.IsDummyObject(hvoActualWordform))
				{
					// we need to convert this to a real wordform.
					realWordform = (IWfiWordform)CmObject.ConvertDummyToReal(m_cache, hvoActualWordform) as WfiWordform;
					hvoActualWordform = realWordform.Hvo;
				}
				else
				{
					realWordform = WfiWordform.CreateFromDBObject(m_cache, hvoActualWordform);
				}
			}

			internal int GetSegmentForm(int iSegment, int iSegForm)
			{
				int hvoCbaActual;
				List<int> segmentForms = m_para.SegmentForms(GetSegment(iSegment));
				Debug.Assert(iSegForm >= 0 && iSegForm < segmentForms.Count);
				hvoCbaActual = (int)segmentForms[iSegForm];
				return hvoCbaActual;
			}

			internal int GetSegment(int iSegment)
			{
				Debug.Assert(m_para != null);
				List<int> segments = m_para.Segments;
				Debug.Assert(iSegment >= 0 && iSegment < segments.Count);
				return segments[iSegment];
			}

			/// <summary>
			/// Caches object property for dummy owning objects, Sets the real property for real owning objects.
			/// </summary>
			/// <param name="hvoOwningObject"></param>
			/// <param name="tag"></param>
			/// <param name="hvoObject"></param>
			/// <param name="fMakeReal"></param>
			/// <returns></returns>
			internal int SetObjProp(int hvoOwningObject, int tag, int hvoObject, bool fMakeReal)
			{
				int hvoAnnotationOld = hvoOwningObject;
				Debug.Assert(hvoOwningObject != 0);
				if (m_cache.IsDummyObject(hvoOwningObject))
				{
					// set the InstanceOf in the cache.
					IVwCacheDa cda = m_cache.VwCacheDaAccessor;
					cda.CacheObjProp(hvoOwningObject, tag, hvoObject);
					if (fMakeReal)
					{
						// Convert the actual annotation to a real one.
						ICmObject realCba = CmObject.ConvertDummyToReal(m_cache, hvoOwningObject);
						hvoOwningObject = realCba.Hvo;
					}
				}
				else
				{
					ISilDataAccess sda = m_cache.MainCacheAccessor;
					sda.SetObjProp(hvoOwningObject, tag, hvoObject);
				}
				return hvoOwningObject;
			}

			internal int SetInstanceOf(int hvoAnnotation, int hvoInstanceOf, bool fMakeReal)
			{
				return SetObjProp(hvoAnnotation, (int)CmAnnotation.CmAnnotationTags.kflidInstanceOf, hvoInstanceOf, fMakeReal);
			}
		}

		internal class ParagraphAnnotatorForParagraphBuilder : ParagraphAnnotator
		{
			ParagraphBuilder m_pb = null;

			internal ParagraphAnnotatorForParagraphBuilder(ParagraphBuilder pb) : base(pb.ActualParagraph)
			{
				m_pb = pb;
			}

			override internal int SetAlternateCase(string wordform, int iOccurrenceInParagraph, StringCaseStatus targetState)
			{
				int iSegment = -1;
				int iSegForm = -1;
				m_pb.GetSegmentFormInfo(wordform, iOccurrenceInParagraph, out iSegment, out iSegForm);
				string alternateWordform;
				int hvoAlternateWordform = SetAlternateCase(iSegment, iSegForm, targetState, out alternateWordform);
				return hvoAlternateWordform;
			}

			override internal int SetDefaultWordGloss(string wordform, int iOccurrenceInParagraph)
			{
				int iSegment = -1;
				int iSegForm = -1;
				m_pb.GetSegmentFormInfo(wordform, iOccurrenceInParagraph, out iSegment, out iSegForm);
				string gloss;
				int hvoGloss = SetDefaultWordGloss(iSegment, iSegForm, out gloss);
				return hvoGloss;
			}

			override internal int SetAlternateCase(int iSegment, int iSegForm, StringCaseStatus targetState, out string alternateCaseForm)
			{
				int hvoWfAlternateCase = base.SetAlternateCase(iSegment, iSegForm, targetState, out alternateCaseForm);
				int hvoCbaExpected = GetExpectedSegmentForm(iSegment, iSegForm);
				SetInstanceOf(hvoCbaExpected, hvoWfAlternateCase, false);
				return hvoWfAlternateCase;
			}

			override internal IWfiAnalysis BreakIntoMorphs(int iSegment, int iSegForm, ArrayList moForms)
			{
				IWfiAnalysis wfiAnalysis = base.BreakIntoMorphs(iSegment, iSegForm, moForms);
				int hvoCbaExpected = GetExpectedSegmentForm(iSegment, iSegForm);
				SetInstanceOf(hvoCbaExpected, wfiAnalysis.Hvo, false);
				return wfiAnalysis;
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="iSegForm">primary segment form index to merge to adjacent segform</param>
			/// <param name="iSegment"></param>
			/// <param name="secondaryPathsToJoinWords">list containing Segment/SegmentForm pairs in paragraph to also join.</param>
			/// <param name="secondaryPathsToBreakPhrases">list containint Segment/SegmentForm pairs to break into annotations.</param>
			/// <param name="fValidateStringValues">if true, we'll make sure that the resulting merges have the same string values.</param>
			/// <returns></returns>
			internal int MergeAdjacentAnnotations(int iSegmentPrimary, int iSegFormPrimary,
				IList<int[]> secondaryPathsToJoinWords, IList<int[]> secondaryPathsToBreakPhrases)
			{
				// first do the primary merge in the paragraph definition (the base class will do it to the paragraph definition).
				string formerMergedStringValue = m_pb.SegmentFormNode(iSegmentPrimary, iSegFormPrimary).InnerText;
				m_pb.MergeAdjacentAnnotations(iSegmentPrimary, iSegFormPrimary);
				string mainMergedStringValue = m_pb.SegmentFormNode(iSegmentPrimary, iSegFormPrimary).InnerText;

				// next do secondary merges which will result from the real analysis.
				MergeSecondaryPhrases(secondaryPathsToJoinWords, mainMergedStringValue);

				// now remove other secondary merges that are no longer valid, since we have destroyed the former one.
				BreakSecondaryPhrases(secondaryPathsToBreakPhrases, formerMergedStringValue);

				// physically change the paragraph (SegmentForms) to match the paragraph definition.
				// after a reparsing the paragraph, the secondary merges will also be reflected.
				return base.MergeAdjacentAnnotations(iSegmentPrimary, iSegFormPrimary);
			}

			private void MergeSecondaryPhrases(IList<int[]> secondaryPathsToJoinWords, string mainMergedStringValue)
			{
				foreach (int[] segmentFormPath in secondaryPathsToJoinWords)
				{
					Debug.Assert(segmentFormPath.Length == 2, "SegmentForm paths should have a segment index followed by a segment form index.");
					int iSecondarySegment = segmentFormPath[0];
					int iSecondarySegForm = segmentFormPath[1];
					m_pb.MergeAdjacentAnnotations(iSecondarySegment, iSecondarySegForm);
					// validate string values match main merge.
					string secondaryStringValue = m_pb.SegmentFormNode(iSecondarySegment, iSecondarySegForm).InnerText;
					Debug.Equals(mainMergedStringValue, secondaryStringValue);
				}
			}

			private void BreakSecondaryPhrases(IList<int[]> secondaryPathsToBreakPhrases, string formerMergedStringValue)
			{
				foreach (int[] segmentFormPath in secondaryPathsToBreakPhrases)
				{
					Debug.Assert(segmentFormPath.Length == 2, "SegmentForm paths should have a segment index followed by a segment form index.");
					int iSecondarySegment = segmentFormPath[0];
					int iSecondarySegForm = segmentFormPath[1];
					string secondaryStringValue = m_pb.SegmentFormNode(iSecondarySegment, iSecondarySegForm).InnerText;
					// validate the phrase we are destroying equals the previous merged phrase.
					Debug.Equals(formerMergedStringValue, secondaryStringValue);
					m_pb.BreakPhraseAnnotation(iSecondarySegment, iSecondarySegForm);
				}
			}

			internal List<int> BreakPhrase(int iSegmentPrimary, int iSegFormPrimary,
				IList<int[]> secondaryPathsToBreakPhrases, IList<int[]> secondaryPathsToJoinWords, string mainMergedStringValue)
			{
				// first do the primary break in the paragraph definition (the base class will actually do it the paragraph).
				string formerMergedStringValue = m_pb.SegmentFormNode(iSegmentPrimary, iSegFormPrimary).InnerText;
				m_pb.BreakPhraseAnnotation(iSegmentPrimary, iSegFormPrimary);

				// now remove other secondary merges that are no longer valid, since we have destroyed the former one.
				BreakSecondaryPhrases(secondaryPathsToBreakPhrases, formerMergedStringValue);

				// merge together any remaining secondary phrases.
				MergeSecondaryPhrases(secondaryPathsToJoinWords, mainMergedStringValue);

				// physically change the paragraph (SegmentForms) to match the paragraph definition.
				// after a reparsing the paragraph, the secondary breaks will also be reflected.
				return base.BreakPhrase(iSegmentPrimary, iSegFormPrimary);
			}

			/// <summary>
			/// </summary>
			/// <param name="text"></param>
			/// <param name="iParagraph"></param>
			/// <param name="wordform"></param>
			/// <param name="iWordformOccurrenceInParagraph"></param>
			/// <returns>hvo of new analysis.</returns>
			override internal int SetDefaultWordGloss(int iSegment, int iSegForm, out string gloss)
			{
				return SetDefaultWordGloss(iSegment, iSegForm, null, out gloss);
			}

			/// <summary>
			/// Copies out the cba info in the paragraph definition cba node into a new real cba.
			/// </summary>
			/// <param name="iSegment"></param>
			/// <param name="iSegForm"></param>
			/// <returns></returns>
			internal int ExportCbaNodeToReal(int iSegment, int iSegForm)
			{
				int hvoSegFormInParaDef = GetExpectedSegmentForm(iSegment, iSegForm);
				ICmBaseAnnotation cbaInParaDef = new CmBaseAnnotation(m_cache, hvoSegFormInParaDef);
				ICmBaseAnnotation exportedCba = CmBaseAnnotation.CreateRealAnnotation(m_cache, cbaInParaDef.AnnotationTypeRAHvo,
					cbaInParaDef.InstanceOfRAHvo, cbaInParaDef.BeginObjectRAHvo, cbaInParaDef.Flid,
					cbaInParaDef.BeginOffset, cbaInParaDef.EndOffset);
				return exportedCba.Hvo;
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="iSegment"></param>
			/// <param name="iSegForm"></param>
			/// <param name="hvoWfiAnalysis">wfi analysis to add gloss to.</param>
			/// <param name="gloss"></param>
			/// <returns></returns>
			override internal int SetDefaultWordGloss(int iSegment, int iSegForm, IWfiAnalysis wfiAnalysis, out string gloss)
			{
				gloss = "";
				// Set actual analysis.
				int hvoGloss = base.SetDefaultWordGloss(iSegment, iSegForm, wfiAnalysis, out gloss);

				int hvoCbaExpected = GetExpectedSegmentForm(iSegment, iSegForm);
				SetInstanceOf(hvoCbaExpected, hvoGloss, false);
				return hvoGloss;
			}

			private int GetExpectedSegmentForm(int iSegment, int iSegForm)
			{
				// Get the expected annotation.
				XmlNode cbaWordformNode = m_pb.SegmentFormNode(iSegment, iSegForm);
				int hvoCbaExpected = XmlUtils.GetMandatoryIntegerAttributeValue(cbaWordformNode, "id");
				Debug.Assert(hvoCbaExpected != 0);

				return hvoCbaExpected;
			}

			/// <summary>
			/// Walk through the paragraph and create word glosses, and segment annotations.
			/// </summary>
			/// <returns>list of wordglosses, including 0's for nontwfics</returns>
			internal protected override List<int> SetupDefaultWordGlosses()
			{
				List<int> wordGlosses = new List<int>();
				int iseg = 0;
				foreach (XmlNode segNode in m_pb.SegmentNodes())
				{
					int isegform = 0;
					foreach (XmlNode segFormNode in ParagraphBuilder.SegmentFormNodes(segNode))
					{
						int hvoWordGloss = 0;
						string annTypeGuid = ParagraphBuilder.GetAnnotationTypeGuid(segFormNode);
						if (annTypeGuid == LangProject.kguidAnnWordformInContext)
						{
							string twficGloss;
							hvoWordGloss = SetDefaultWordGloss(iseg, isegform, out twficGloss);
						}
						if (annTypeGuid == LangProject.kguidAnnPunctuationInContext)
						{
							ExportCbaNodeToReal(iseg, isegform);
						}
						wordGlosses.Add(hvoWordGloss);
						isegform++;
					}
					// create freeform annotations for each of the segments
					// TODO: multiple writing systems.
					ITsString tssComment;
					SetDefaultFreeformAnnotation(iseg, LangProject.kguidAnnFreeTranslation, out tssComment);
					SetDefaultFreeformAnnotation(iseg, LangProject.kguidAnnLiteralTranslation, out tssComment);
					SetDefaultFreeformAnnotation(iseg, LangProject.kguidAnnNote.ToString(), out tssComment);

					iseg++;
				}
				return wordGlosses;
			}

			/// <summary>
			///
			/// </summary>
			internal void ValidateAnnotations()
			{
				ValidateAnnotations(false);
			}

			/// <summary>
			/// Validate the annotation information stored in the xml configuration against
			/// the annotation information stored in memory from ParagraphParser.
			/// </summary>
			/// <param name="fSkipForceParse">skips parsing if the text has not changed.</param>
			internal void ValidateAnnotations(bool fSkipForceParse)
			{
				Debug.Assert(m_para != null);
				Debug.Assert(m_para == m_pb.ActualParagraph);
				if (fSkipForceParse)
				{
					// just sync the defn with expected valid ids.
					m_pb.ResyncExpectedAnnotationIds();
				}
				else if (NeedReparseParagraph)
				{
					ReparseParagraph();
				}
				if (m_pb.NeedToRebuildParagraphContentFromAnnotations)
				{
					m_pb.RebuildParagraphContentFromAnnotations();
				}
				ConceptualModelXmlParagraphValidator validator = new ConceptualModelXmlParagraphValidator(m_pb);
				validator.ValidateParagraphs(m_pb.ParagraphDefinition, m_pb.ActualParagraph);
			}
		}

		/// <summary>
		/// This class can be used to build the contents of a paragraph through an xml specification.
		/// Note: the xml specification will be modified to save the expected state of the paragraph.
		/// </summary>
		internal class ParagraphBuilder
		{
			FdoCache m_cache = null;
			FdoOwningSequence<IStPara> m_owner = null;
			IStTxtPara m_para = null;
			XmlNode m_paraDefn = null;
			bool m_fNeedToRebuildParagraphContentFromAnnotations = false;
			Dictionary<string, int> m_expectedWordformsAndOccurrences;
			//bool m_fNeedToRebuildParagraphContentFromStrings = false;

			internal ParagraphBuilder(FdoOwningSequence<IStPara> owner)
			{
				m_owner = owner;
				m_cache = owner.Cache;
			}

			internal ParagraphBuilder(FdoOwningSequence<IStPara> owner, XmlNode paraDefn)
				: this(owner)
			{
				m_paraDefn = paraDefn;
			}

			internal ParagraphBuilder(IStTxtPara para, XmlNode paraDefn)
			{
				m_para = para;
				m_cache = para.Cache;
				Debug.Assert(paraDefn != null &&
					XmlUtils.GetMandatoryIntegerAttributeValue(paraDefn, "id") == para.Hvo);
				m_paraDefn = paraDefn;
			}

			internal ParagraphBuilder(XmlNode textsDefn, FDO.IText text, int iPara)
			{
				IStTxtPara para;
				m_paraDefn = GetStTxtParaDefnNode(text, textsDefn, iPara, out para);
				m_para = para;
				m_cache = para.Cache;
			}

			internal XmlNode ParagraphDefinition
			{
				get { return m_paraDefn; }
				set { m_paraDefn = value; }
			}

			private XmlNode ParentStTextDefinition
			{
				get
				{
					if (m_paraDefn != null)
						return m_paraDefn.SelectSingleNode("ancestor::StText");
					else
						return null;
				}
			}

			internal XmlNode SnapshotOfStText()
			{
				return Snapshot(ParentStTextDefinition);
			}

			internal XmlNode SnapshotOfPara()
			{
				return Snapshot(ParagraphDefinition);
			}

			/// <summary>
			/// get a clone of a node (and its owning document)
			/// </summary>
			/// <param name="node"></param>
			/// <returns></returns>
			internal static XmlNode Snapshot(XmlNode node)
			{
				return XmlUtils.CloneNodeWithDocument(node);
			}

			internal IStTxtPara ActualParagraph
			{
				get { return m_para; }
			}

			static protected XmlNode GetStTxtParaDefnNode(FDO.IText text, XmlNode textsDefn, int iPara, out IStTxtPara para)
			{
				para = text.ContentsOA.ParagraphsOS[iPara] as IStTxtPara;
				Debug.Assert(para != null);
				if (textsDefn == null)
					return null;
				XmlNode paraNode = textsDefn.SelectSingleNode("//StTxtPara[@id='" + para.Hvo + "']");
				if (paraNode == null)
				{
					// see if we can find a new node that for which we haven't determined its hvo.
					paraNode = textsDefn.SelectSingleNode("//StTxtPara[" + (iPara + 1) + "]");
					// the paragraph shouldn't have an hvo yet.
					if (paraNode.Attributes["id"].Value == "")
					{
						// go ahead and set the hvo now.
						paraNode.Attributes["id"].Value = para.Hvo.ToString();
					}
					else
					{
						paraNode = null;
					}
				}
				return paraNode;
			}

			internal IStTxtPara BuildParagraphContent(XmlNode paraDefn)
			{
				if (paraDefn.Name != "StTxtPara")
					return null;
				// 1. Create a new paragraph.
				CreateNewParagraph(paraDefn);
				// Build Contents
				ITsString contents;
				XmlNodeList segments = m_paraDefn.SelectNodes("Segments16/CmBaseAnnotation");
				if (segments.Count > 0)
				{
					contents = RebuildParagraphContentFromAnnotations();
				}
				else
				{
					XmlNodeList runs = m_paraDefn.SelectNodes("Contents16/Str/Run");
					if (runs.Count > 0)
						contents = RebuildParagraphContentFromStrings();
				}
				return m_para;
			}

			internal ITsString RebuildParagraphContentFromStrings()
			{
				try
				{
					m_expectedWordformsAndOccurrences = null;
					//<Contents16> <Str> <Run ws="">
					XmlNodeList runs = m_paraDefn.SelectNodes("Contents16/Str/Run");
					if (runs == null)
						return null;

					StTxtParaBldr bldr = new StTxtParaBldr(m_cache);
					bldr.StringBuilder.Clear();

					foreach (XmlNode run in runs)
					{
						int ws = GetWsFromStringNode(run);
						// we need to restrict this to twfics.
						// UpdateExpectedWordformsAndOccurrences(run.InnerText, ws);
						bldr.AppendRun(run.InnerText, MakeTextProps(ws));
					}
					SetContents(bldr.StringBuilder.GetString(), false, false);
					return m_para.Contents.UnderlyingTsString;
				}
				finally
				{
					//m_fNeedToRebuildParagraphContentFromStrings = false;
				}
			}

			internal IStTxtPara CreateNewParagraph(XmlNode paraDefn)
			{
				int iInsertAt = XmlUtils.GetIndexAmongSiblings(paraDefn);
				m_para = this.CreateParagraph(iInsertAt);
				m_paraDefn = paraDefn;
				m_paraDefn.Attributes["id"].Value = m_para.Hvo.ToString();
				return m_para;
			}

			static public void CopyContents(IStTxtPara paraSrc, IStTxtPara paraTarget)
			{
				paraTarget.Contents.UnderlyingTsString = paraSrc.Contents.UnderlyingTsString;
			}

			/// <summary>
			/// (after setting paragraph contents, we'll reparse the text, creating dummy wordforms.)
			/// </summary>
			/// <returns></returns>
			internal ITsString RebuildParagraphContentFromAnnotations()
			{
				return RebuildParagraphContentFromAnnotations(false, false);
			}

			internal Dictionary<string, int> ExpectedWordformsAndOccurrences
			{
				get
				{
					if (m_expectedWordformsAndOccurrences == null)
						CollectExpectedWordformsAndOccurrences();
					return m_expectedWordformsAndOccurrences;
				}
			}

			private void CollectExpectedWordformsAndOccurrences()
			{
				GenerateParaContentFromAnnotations();
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="fCreateRealWordorms">if true, creates real wordforms rather than dummy ones during parse.</param>
			/// <returns></returns>
			internal ITsString RebuildParagraphContentFromAnnotations(bool fCreateRealWordorms, bool fResetConcordance)
			{
				try
				{
					ITsString content = GenerateParaContentFromAnnotations();
					if (content == null)
						return null;

					// Set the content of text for the paragraph.
					this.SetContents(content, fCreateRealWordorms, fResetConcordance);
					return m_para.Contents.UnderlyingTsString;
				}
				finally
				{
					m_fNeedToRebuildParagraphContentFromAnnotations = false;
				}
			}

			/// <summary>
			/// Sync the expected wfics ids with the database.
			/// </summary>
			internal void ResyncExpectedAnnotationIds()
			{
				GenerateParaContentFromAnnotations();
			}

			internal ITsString GenerateParaContentFromAnnotations()
			{
				m_expectedWordformsAndOccurrences = new Dictionary<string, int>();
				Debug.Assert(m_paraDefn != null);
				XmlNodeList segments = m_paraDefn.SelectNodes("Segments16/CmBaseAnnotation");
				if (segments == null)
					return null;

				int ichMinSeg = 0;
				int ichLimSeg = 0;
				// Create TsString for Paragraph Contents.
				ITsStrBldr contentsBldr = m_para.Contents.UnderlyingTsString.GetBldr();
				contentsBldr.Clear();
				foreach (XmlNode segment in segments)
				{
					// Build the segment by its SegmentForms annotations.
					XmlNodeList forms = segment.SelectNodes("SegmentForms37/CmBaseAnnotation");
					if (forms == null)
						break;
					XmlNode lastForm = forms[forms.Count - 1];
					string prevAnnType = null;
					foreach (XmlNode form in forms)
					{
						// Get the string form for the annotation.
						XmlNode strValue = form.SelectSingleNode("StringValue37");
						string strForm = strValue.InnerText;
						// Build the text based upon this annotation.
						// if we are given a ws for the writing system, otherwise use the default.
						int ws = GetWsFromStringNode(strValue);
						Debug.Assert(!String.IsNullOrEmpty(strForm), "Can't build paragraphs based on annotations without a StringValue.");
						string guid = GetAnnotationTypeGuid(form);

						// Build leading whitespace.
						int cchLeadingWhitespace = XmlUtils.GetOptionalIntegerValue(strValue, "leadingWhitespace", 0);
						string leadingWhitespace = GenerateWhitespace(cchLeadingWhitespace);
						if (prevAnnType != null && prevAnnType == guid)
						{
							// add standard form separator between common annotation types.
							leadingWhitespace += " ";
						}
						int ichMinForm = ichLimSeg + leadingWhitespace.Length;
						int ichLimForm = ichMinForm + strForm.Length;
						// if the 'id' is valid, then use it.
						int hvoAnnSegmentForm = 0;
						// Create a dummy annotation for the appropriate type.
						if (guid == LangProject.kguidAnnWordformInContext)
						{
							// Create twfic dummy annotation.
							// NOTE: we'll set InstanceOf to something meaningful elsewhere.
							// For now we'll set -1 to mean there is no analysis (i.e. wordform level).
							hvoAnnSegmentForm = this.CreateOrReuseNodeAnnotation(form, m_para.Hvo,
								ParagraphParserTests.TwficAnnotationType, ichMinForm, ichLimForm, -1);
							// update our expected occurences of this wordform.
							UpdateExpectedWordformsAndOccurrences(strForm, ws);
						}
						else if (guid == LangProject.kguidAnnPunctuationInContext)
						{
							hvoAnnSegmentForm = this.CreateOrReuseNodeAnnotation(form, m_para.Hvo,
								ParagraphParserTests.PunctuationAnnotationType, ichMinForm, ichLimForm, 0);
						}
						else
						{
							Debug.Fail("AnnotationType " + guid + " not supported in SegmentForms.");
						}

						// Calculate any trailing whitespace to be added to text.
						int extraTrailingWhitespace = XmlUtils.GetOptionalIntegerValue(strValue, "extraTrailingWhitespace", 0);
						string trailingWhitespace = GenerateWhitespace(extraTrailingWhitespace);

						// Make a TsTextProps specifying just the writing system.
						ITsTextProps props = MakeTextProps(ws);
						// Add the Form to the text.
						contentsBldr.Replace(contentsBldr.Length, contentsBldr.Length, leadingWhitespace + strForm + trailingWhitespace, props);
						// setup for the next form.
						prevAnnType = guid;
						ichLimSeg = ichLimForm + trailingWhitespace.Length;
						Debug.Assert(ichLimSeg == contentsBldr.Length, "The current length of the text we're building (" + contentsBldr.Length +
							") should match the current segment Lim (" + ichLimSeg + ").");
					}
					// Create the segment annotation.
					int hvoAnnSegment = this.CreateOrReuseNodeAnnotation(segment, m_para.Hvo,
							ParagraphParserTests.SegmentAnnotationType, ichMinSeg, ichLimSeg, 0);
					ichMinSeg = ichLimSeg;
				}

				return contentsBldr.GetString();
			}

			internal static string GetAnnotationTypeGuid(XmlNode segFormNode)
			{
				XmlNode annotationType = segFormNode.SelectSingleNode("AnnotationType34/Link");
				Debug.Assert(annotationType != null, "SegmentForm needs to specify an AnnotationType.");
				string guid = XmlUtils.GetManditoryAttributeValue(annotationType, "guid");
				return guid;
			}

			public List<int> CollectAnnotationIdsFromDefn()
			{
				List<int> annotationIds = new List<int>();
				foreach (XmlNode annotationNode in ParagraphDefinition.SelectNodes("descendant::CmBaseAnnotation"))
				{
					annotationIds.Add(GetCmBaseAnnotationId(annotationNode));
				}
				return annotationIds;
			}

			private static string GenerateWhitespace(int cchWhitespace)
			{
				string whitespace = "";
				// add extra whitespace to our text.
				for (int i = 0; i < cchWhitespace; i++)
					whitespace += " ";
				return whitespace;
			}

			private void UpdateExpectedWordformsAndOccurrences(string strForm, int ws)
			{
				if (strForm.Length == 0)
					return;
				string key = strForm + ws.ToString();
				if (m_expectedWordformsAndOccurrences.ContainsKey(key))
				{
					// note another occurrence
					m_expectedWordformsAndOccurrences[key] += 1;
				}
				else
				{
					// first occurrence
					m_expectedWordformsAndOccurrences[key] = 1;
				}
			}

			private static ITsTextProps MakeTextProps(int ws)
			{
				ITsPropsBldr propBldr = TsPropsBldrClass.Create();
				propBldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, ws);
				ITsTextProps props = propBldr.GetTextProps();
				return props;
			}

			private int GetWsFromStringNode(XmlNode strValue)
			{
				int ws = 0;
				string wsAbbr = XmlUtils.GetOptionalAttributeValue(strValue, "ws");
				if (wsAbbr != null)
				{
					ws = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(wsAbbr);
					Debug.Assert(ws != 0, "Don't recognize ws (" + wsAbbr + ") for StringValue");
					// add it to the vernacular writing system list, if it's not already there.
					if (!m_cache.LangProject.CurVernWssRS.Contains(ws))
						m_cache.LangProject.CurVernWssRS.Append(ws);
				}
				if (ws == 0)
					ws = m_cache.DefaultVernWs;
				return ws;
			}

			int CreateOrReuseNodeAnnotation(XmlNode annotationDefn, int hvoPara, int annotationType, int ichMin, int ichLim, int hvoInstanceOf)
			{
				int hvoAnnotation = XmlUtils.GetOptionalIntegerValue(annotationDefn, "id", 0);
				Debug.Assert(hvoAnnotation == 0 || m_cache.IsDummyObject(hvoAnnotation), "Expected Annotation Node to be using DummyObject");
				if (m_cache.IsValidObject(hvoAnnotation) && m_cache.IsDummyObject(hvoAnnotation))
				{
					// update its offsets.
					CmBaseAnnotation.SetCbaFields(m_cache, hvoAnnotation, ichMin, ichLim, m_para.Hvo, false);
				}
				else
				{
					hvoAnnotation = CmBaseAnnotation.CreateDummyAnnotation(m_cache, m_para.Hvo, annotationType, ichMin, ichLim, hvoInstanceOf);
				}
				Debug.Assert(hvoAnnotation != 0);
				annotationDefn.Attributes["id"].Value = hvoAnnotation.ToString();
				return hvoAnnotation;
			}

			public IStTxtPara AppendParagraph()
			{
				return CreateParagraph(m_owner.Count);
			}

			public IStTxtPara CreateParagraph(int iInsertNewParaAt)
			{
				Debug.Assert(m_owner != null);
				IStTxtPara para = new StTxtPara();
				m_owner.InsertAt(para, iInsertNewParaAt);
				return para;
			}

			void SetContents(ITsString tssContents, bool fCreateRealWordforms, bool fResetConcordance)
			{
				Debug.Assert(m_para != null);
				if (tssContents.Equals(m_para.Contents.UnderlyingTsString))
					return;
				m_para.Contents.UnderlyingTsString = tssContents;
				ParseParagraph(fResetConcordance, fCreateRealWordforms);
			}

			internal void ParseParagraph(bool fResetConcordance, bool fCreateRealWordforms)
			{
				// Reparse the paragraph to recompute annotations.
				ParseParagraph(fResetConcordance, fCreateRealWordforms, true);
			}

			internal void ParseParagraph(bool fResetConcordance, bool fCreateRealWordforms, bool fBuildConcordance)
			{
				ParagraphParser.ParseParagraph(m_para, fBuildConcordance, fResetConcordance, fCreateRealWordforms);
			}

			/// <summary>
			/// rebuilds segments and xfics for the paragraph, without adjusting cba offsets.
			/// </summary>
			internal void ReconstructSegmentsAndXfics()
			{
				// Note: disallowing the parser from readjusting the offsets
				// allows testing of subsequent undo and redo.
				ParagraphParser.ReconstructSegmentsAndXfics(m_para, true);
			}

			XmlNode SegmentNode(int iSegment)
			{
				XmlNodeList cbaSegmentNodes = SegmentNodeList();
				Debug.Assert(cbaSegmentNodes != null);
				Debug.Assert(iSegment < cbaSegmentNodes.Count);
				return cbaSegmentNodes[iSegment];
			}

			XmlNodeList SegmentFormNodeList(int iSegment)
			{
				XmlNode segmentNode = SegmentNode(iSegment);
				return SegmentFormNodeList(segmentNode);
			}

			static XmlNodeList SegmentFormNodeList(XmlNode segmentNode)
			{
				XmlNodeList cbaSegFormNodes = segmentNode.SelectNodes("SegmentForms37/CmBaseAnnotation");
				return cbaSegFormNodes;
			}

			internal XmlNode SegmentFormNode(int iSegment, int iSegForm)
			{
				XmlNodeList cbaSegFormNodes = SegmentFormNodeList(iSegment);
				Debug.Assert(cbaSegFormNodes != null);
				if (cbaSegFormNodes.Count == 0)
					return null;
				Debug.Assert(iSegForm < cbaSegFormNodes.Count);
				XmlNode cbaSegFormNode = cbaSegFormNodes[iSegForm];
				return cbaSegFormNode;
			}

			static internal List<XmlNode> SegmentNodes(XmlNode paraDefn)
			{
				return NodeListToNodes(SegmentNodeList(paraDefn));
			}

			internal List<XmlNode> SegmentNodes()
			{
				return NodeListToNodes(SegmentNodeList());
			}

			static internal List<XmlNode> SegmentFormNodes(XmlNode segment)
			{
				return NodeListToNodes(SegmentFormNodeList(segment));
			}

			internal XmlNode GetSegmentFormInfo(string wordform, int iOccurrenceInParagraph, out int iSegment, out int iSegForm)
			{
				XmlNodeList cbaSegFormNodes = m_paraDefn.SelectNodes("./Segments16/CmBaseAnnotation/SegmentForms37/CmBaseAnnotation[StringValue37='" + wordform + "']");
				Debug.Assert(cbaSegFormNodes != null);
				Debug.Assert(iOccurrenceInParagraph >= 0 && iOccurrenceInParagraph < cbaSegFormNodes.Count);
				XmlNode cbaSegForm = cbaSegFormNodes[iOccurrenceInParagraph];
				iSegForm = XmlViewsUtils.FindIndexOfMatchingNode(
					NodeListToNodes(cbaSegForm.ParentNode.SelectNodes("CmBaseAnnotation")), cbaSegForm);
				iSegment = XmlViewsUtils.FindIndexOfMatchingNode(
					NodeListToNodes(cbaSegForm.SelectNodes("../../../CmBaseAnnotation")), cbaSegForm.ParentNode.ParentNode);
				return cbaSegForm;
			}

			internal bool ReplaceTrailingWhitepace(int iSegment, int iSegForm, int newExtraWhitespaceCount)
			{
				XmlNode stringValueNode = StringValueNode(iSegment, iSegForm);
				ReplaceAttributeValue(stringValueNode, "extraTrailingWhitespace", newExtraWhitespaceCount.ToString());
				return NeedToRebuildParagraphContentFromAnnotations;
			}

			internal bool ReplaceLeadingWhitepace(int iSegment, int iSegForm, int newExtraWhitespaceCount)
			{
				XmlNode stringValueNode = StringValueNode(iSegment, iSegForm);
				ReplaceAttributeValue(stringValueNode, "leadingWhitespace", newExtraWhitespaceCount.ToString());
				return NeedToRebuildParagraphContentFromAnnotations;
			}

			internal string ReplaceAttributeValue(XmlNode node, string attrName, string newValue)
			{
				XmlAttribute attr = node.Attributes[attrName];
				if (attr == null)
				{
					attr = node.OwnerDocument.CreateAttribute(attrName);
					node.Attributes.Append(attr);
				}
				attr.Value = newValue;
				InvalidateParagraphInfo();
				return attr.Value;
			}

			private XmlNode StringValueNode(int iSegment, int iSegForm)
			{
				XmlNode segFormNode = SegmentFormNode(iSegment, iSegForm);
				XmlNode stringValueNode = segFormNode.SelectSingleNode("StringValue37");
				return stringValueNode;
			}

			private string StringValue(int iSegment, int iSegForm)
			{
				return StringValueNode(iSegment, iSegForm).InnerText;
			}

			internal bool ReplaceSegmentForm(string segmentForm, int iOccurrenceInParagraph, string newValue)
			{
				int iSegment = -1;
				int iSegForm = -1;
				GetSegmentFormInfo(segmentForm, iOccurrenceInParagraph, out iSegment, out iSegForm);
				return ReplaceSegmentForm(iSegment, iSegForm, newValue, 0);
			}

			/// <summary>
			/// Inserts (before iSegment, iSegForm) a new SegmentForm with format:
			///		<CmBaseAnnotation id="">
			///			<AnnotationType34>
			///				<Link ws="en" name="Wordform In Context" guid="eb92e50f-ba96-4d1d-b632-057b5c274132" />
			///			</AnnotationType34>
			///			<StringValue37>xxxpus</StringValue37>
			///		</CmBaseAnnotation>
			/// </summary>
			/// <returns></returns>
			internal bool InsertSegmentForm(int iSegment, int iSegForm, string annTypeGuid, string stringValue)
			{
				// assume this segment already has a segment form.

				XmlNode segmentForms = m_paraDefn.SelectSingleNode("Segments16/CmBaseAnnotation[" + (iSegment + 1) + "]/SegmentForms37");
				XmlElement newSegmentForm = segmentForms.OwnerDocument.CreateElement("CmBaseAnnotation");
				newSegmentForm.SetAttribute("id", "");
				XmlElement newAnnotationType = segmentForms.OwnerDocument.CreateElement("AnnotationType34");
				XmlElement newLink = segmentForms.OwnerDocument.CreateElement("Link");
				Debug.Assert(annTypeGuid == LangProject.kguidAnnWordformInContext ||
					annTypeGuid == LangProject.kguidAnnPunctuationInContext,
					String.Format("annTypeGuid {0} parameter should be either Wordform or Punctuation In Context.", annTypeGuid));
				newLink.SetAttribute("guid", annTypeGuid);
				newAnnotationType.AppendChild(newLink);
				newSegmentForm.AppendChild(newAnnotationType);
				XmlElement newStringValue = segmentForms.OwnerDocument.CreateElement("StringValue37");
				Debug.Assert(stringValue.Length != 0, "stringValue parameter should be nonzero.");
				newStringValue.InnerText = stringValue; // default ws.
				newSegmentForm.AppendChild(newStringValue);

				XmlNode segmentFormNode = SegmentFormNode(iSegment, iSegForm);
				if (segmentFormNode == null)
					segmentForms.AppendChild(newSegmentForm);
				else
					segmentForms.InsertBefore(newSegmentForm, segmentFormNode);

				// Invalidate real paragraph cache info.
				InvalidateParagraphInfo();
				return NeedToRebuildParagraphContentFromAnnotations;
			}

			internal XmlElement CreateSegmentNode()
			{
				XmlNode segments = m_paraDefn.SelectSingleNode(".//Segments16");
				XmlElement newSegment = segments.OwnerDocument.CreateElement("CmBaseAnnotation");
				newSegment.SetAttribute("id", "");
				segments.AppendChild(newSegment);
				return newSegment;
			}

			internal XmlElement CreateSegmentForms()
			{
				Debug.Assert(m_paraDefn.SelectNodes(".//Segments16/CmBaseAnnotation").Count == 1);
				Debug.Assert(m_paraDefn.SelectSingleNode(".//Segments16/CmBaseAnnotation/SegmentForms37") == null);
				XmlNode segment = m_paraDefn.SelectSingleNode(".//Segments16/CmBaseAnnotation");
				XmlElement newSegmentForms = segment.OwnerDocument.CreateElement("SegmentForms37");
				segment.AppendChild(newSegmentForms);
				return newSegmentForms;
			}

			internal bool InsertSegmentBreak(int iSegment, int iSegForm, string stringValue)
			{
				// First insert a punctuation segment form.
				InsertSegmentForm(iSegment, iSegForm, LangProject.kguidAnnPunctuationInContext, stringValue);

				// Next insert a new preceding segment
				XmlNode segmentBreakNode = SegmentFormNode(iSegment, iSegForm);
				XmlNode segmentForms = segmentBreakNode.ParentNode;
				XmlNode segments = segmentForms.ParentNode.ParentNode;
				XmlElement newSegment = segments.OwnerDocument.CreateElement("CmBaseAnnotation");
				newSegment.SetAttribute("id", "");
				XmlElement newSegmentForms = segments.OwnerDocument.CreateElement("SegmentForms37");
				newSegment.AppendChild(newSegmentForms);
				segments.InsertBefore(newSegment, segmentForms.ParentNode);

				// then move all children through the period to the new segment forms
				MoveSiblingNodes(segmentForms.FirstChild, newSegmentForms, segmentBreakNode);

				// Invalidate real paragraph cache info.
				InvalidateParagraphInfo();
				return NeedToRebuildParagraphContentFromAnnotations;
			}

			internal bool DeleteSegmentBreak(int iSegment, int iSegForm)
			{
				// get first segment
				XmlNode segmentBreakNode = SegmentFormNode(iSegment, iSegForm);
				XmlNode segmentForms = segmentBreakNode.ParentNode;
				XmlNode segments = segmentForms.ParentNode.ParentNode;

				Debug.Assert(iSegment < SegmentNodeList().Count - 1,
					String.Format("Can't merge segments in paragraph, since iSegment {0} is last segment.", iSegment));

				// first remove segment break node.
				segmentForms.RemoveChild(segmentBreakNode);

				// then move all segment forms from following segment to the new segment forms
				MoveSiblingNodes(SegmentFormNode(iSegment + 1, 0), segmentForms, null);

				// finally remove following segment
				RemoveSegment(iSegment + 1);

				// Invalidate real paragraph cache info.
				InvalidateParagraphInfo();
				return NeedToRebuildParagraphContentFromAnnotations;
			}

			internal bool ReplaceSegmentForm(int iSegment, int iSegForm, string newValue, int newWs)
			{
				XmlNode segFormNode = SegmentFormNode(iSegment, iSegForm);
				XmlNode stringValueNode = segFormNode.SelectSingleNode("StringValue37");
				if (newValue.Length > 0)
				{
					//if (stringValueNode.InnerText != newValue)
					//{
						// modify the current form.
						stringValueNode.InnerText = newValue;
						// invalidate the segment form id
						segFormNode.Attributes["id"].Value = "";	// we need to recreate the dummy object for this node.
					//}
					// modify the current ws
					if (newWs != 0 && newWs != GetWsFromStringNode(stringValueNode))
					{
						XmlAttribute xaWs = stringValueNode.Attributes["ws"];
						if (xaWs == null)
						{
							xaWs = stringValueNode.OwnerDocument.CreateAttribute("ws");
							stringValueNode.Attributes.Append(xaWs);
						}
						xaWs.Value = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(newWs);
						segFormNode.Attributes["id"].Value = "";	// we need to recreate the dummy object for this node.
					}

				}
				else
				{
					// delete the segment form.
					XmlNode segmentList = segFormNode.ParentNode;
					// Review: handle special cases
					// 1) removing final item in the list. Adjust trailing whitespace for the new final item.
					// 2) if removing item results in an empty list, remove the parent Segment,
					// if it no longer has any forms.
					Debug.Assert(segmentList.ChildNodes.Count != 1,
						"We need to enhance ParagraphBuilder to cleanup an empty SegmentForm list.");
					segmentList.RemoveChild(segFormNode);
				}
				// Invalidate real paragraph cache info.
				InvalidateParagraphInfo();
				return NeedToRebuildParagraphContentFromAnnotations;
			}

			internal bool RemoveSegment(int iSegment)
			{
				XmlNode segmentNode = SegmentNode(iSegment);
				XmlNode segmentsNode = segmentNode.SelectSingleNode("..");
				segmentsNode.RemoveChild(segmentNode);
				InvalidateParagraphInfo();
				return NeedToRebuildParagraphContentFromAnnotations;
			}

			internal bool RemoveSegmentForm(int iSegment, int iSegForm)
			{
				ReplaceSegmentForm(iSegment, iSegForm, "", 0);
				InvalidateParagraphInfo();
				return NeedToRebuildParagraphContentFromAnnotations;
			}

			virtual internal bool MergeAdjacentAnnotations(int iSegment, int iSegForm)
			{
				// change the paragraph definition spec.
				this.ReplaceSegmentForm(iSegment, iSegForm + 1, String.Format("{0} {1}",
					StringValue(iSegment, iSegForm), StringValue(iSegment, iSegForm + 1)), 0);
				this.RemoveSegmentForm(iSegment, iSegForm);

				// The surface text will not change, but we'll need to
				// reparse paragraph to identify matching phrases, and to sync with the ids.
				InvalidateParagraphInfo();
				return NeedToRebuildParagraphContentFromAnnotations;
			}

			virtual internal bool BreakPhraseAnnotation(int iSegment, int iSegForm)
			{
				string[] words = StringValue(iSegment, iSegForm).Split(new char[] { ' ' });
				// insert these in reverse order.
				Array.Reverse(words);
				foreach (string word in words)
				{
					this.InsertSegmentForm(iSegment, iSegForm + 1, LangProject.kguidAnnWordformInContext, word);
				}
				this.RemoveSegmentForm(iSegment, iSegForm);

				InvalidateParagraphInfo();
				return NeedToRebuildParagraphContentFromAnnotations;
			}

			/// <summary>
			/// If we change the xml definition for the paragraph, we need to rebuild the real paragraph content at some point.
			/// </summary>
			void InvalidateParagraphInfo()
			{
				m_fNeedToRebuildParagraphContentFromAnnotations = true;
			}

			internal bool NeedToRebuildParagraphContentFromAnnotations
			{
				get { return m_fNeedToRebuildParagraphContentFromAnnotations; }
			}

			static XmlNodeList SegmentNodeList(XmlNode paraDefn)
			{
				Debug.Assert(paraDefn != null);
				XmlNodeList cbaSegmentNodes = paraDefn.SelectNodes("Segments16/CmBaseAnnotation");
				return cbaSegmentNodes;
			}

			XmlNodeList SegmentNodeList()
			{
				return SegmentNodeList(m_paraDefn);
			}

			static internal int GetCmBaseAnnotationId(XmlNode cbaNode)
			{
				int hvoCba = XmlUtils.GetMandatoryIntegerAttributeValue(cbaNode, "id");
				return hvoCba;
			}
		}

		/// <summary>
		/// This can be used to build a Text based upon an xml specification.
		/// Note: the xml specification will be modified to save the expected state of the text.
		/// </summary>
		protected internal class TextBuilder
		{
			FdoCache m_cache = null;
			FdoOwningCollection<FDO.IText> m_owner = null;
			FDO.IText m_text = null;
			XmlNode m_textDefn = null;

			internal TextBuilder(TextBuilder tbToClone)
				: this(tbToClone.m_owner)
			{
				m_text = tbToClone.m_text;
				this.SelectedNode = ParagraphBuilder.Snapshot(tbToClone.SelectedNode);
			}

			internal TextBuilder(FdoOwningCollection<FDO.IText> owner)
			{
				m_owner = owner;
				m_cache = owner.Cache;
			}

			internal FDO.IText BuildText(XmlNode textDefn)
			{
				if (textDefn.Name != "Text")
					return null;
				m_textDefn = textDefn;
				// 1. Create the new text and give it an owner.
				m_text = this.CreateText();
				textDefn.Attributes["id"].Value = m_text.Hvo.ToString();

				XmlNode name = textDefn.SelectSingleNode("Name5/AUni");
				// 2. If we have a name, set it.
				if (name != null && name.InnerText.Length > 0)
				{
					string wsAbbr = XmlUtils.GetOptionalAttributeValue(name, "ws");
					int ws = 0;
					if (wsAbbr != null)
						ws = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(wsAbbr);
					if (ws == 0)
						ws = m_cache.DefaultVernWs;
					this.SetName(name.InnerText, ws);
				}

				// 3. Create a body for the text;
				XmlNode contents = textDefn.SelectSingleNode("Contents5054/StText");
				if (contents == null)
					return m_text;
				IStText body = this.CreateContents(m_text);

				// 4. Create each paragraph for the text.
				XmlNodeList paragraphs = contents.SelectNodes("Paragraphs14/StTxtPara");
				if (paragraphs != null)
				{
					foreach (XmlNode paraDef in paragraphs)
					{
						ParagraphBuilder pb = new ParagraphBuilder(m_text.ContentsOA.ParagraphsOS);
						IStTxtPara realPara = pb.BuildParagraphContent(paraDef);
					}
				}

				return m_text;
			}

			public List<int> GenerateAnnotationIdsFromDefn()
			{
				foreach (int hvoPara in m_text.ContentsOA.ParagraphsOS.HvoArray)
				{
					ParagraphBuilder pb = GetParagraphBuilder(hvoPara);
					pb.GenerateParaContentFromAnnotations();
				}
				return CollectAnnotationIdsFromDefn();
			}

			public List<int> CollectAnnotationIdsFromDefn()
			{
				List<int> annotationIds = new List<int>();
				foreach (int hvoPara in m_text.ContentsOA.ParagraphsOS.HvoArray)
				{
					ParagraphBuilder pb = GetParagraphBuilder(hvoPara);
					annotationIds.AddRange(pb.CollectAnnotationIdsFromDefn());
				}
				return annotationIds;
			}

			/// <summary>
			/// TODO: Finish implementing this.
			/// </summary>
			/// <returns></returns>
			public List<int> ExportRealSegmentAnnotationsFromDefn()
			{
				List<int> realSegments = new List<int>();
				foreach (int hvoPara in m_text.ContentsOA.ParagraphsOS.HvoArray)
				{
					ParagraphBuilder pb = GetParagraphBuilder(hvoPara);
					ParagraphAnnotatorForParagraphBuilder papb = new ParagraphAnnotatorForParagraphBuilder(pb);
					int iseg = 0;
					foreach (XmlNode segNode in pb.SegmentNodes())
					{
						int hvoSeg = papb.GetSegment(iseg);
						// export this into a real annotation by converting it to a real annotation.
					}
				}
				return realSegments;
			}

			/// <summary>
			/// Creates the text to be used by this TextBuilder
			/// </summary>
			/// <param name="fCreateContents"></param>
			/// <returns></returns>
			public FDO.IText CreateText(bool fCreateContents)
			{
				Debug.Assert(m_text == null);
				m_text = CreateText();
				if (fCreateContents)
					CreateContents(m_text);
				return m_text;
			}

			private FDO.IText CreateText()
			{
				Debug.Assert(m_owner != null && m_owner.Cache != null);
				FDO.IText newText = new Text();
				Debug.Assert(m_owner != null);
				m_owner.Add(newText);
				return newText;
			}


			IStText CreateContents(FDO.IText text)
			{
				Debug.Assert(text != null);
				IStText body1 = new StText();
				text.ContentsOA = body1;
				return body1;
			}

			public IStTxtPara AppendNewParagraph()
			{
				XmlNode lastParagraph = null;
				if (m_textDefn != null)
				{
					// Append insert a new paragraph node.
					XmlNode paragraphs = m_textDefn.SelectSingleNode("//Paragraphs14");
					lastParagraph = paragraphs.LastChild;
				}
				return InsertNewParagraphAfter(lastParagraph);
			}

			public IStTxtPara InsertNewParagraphAfter(XmlNode paragraphDefnToInsertAfter)
			{
				ParagraphBuilder pb = new ParagraphBuilder(m_text.ContentsOA.ParagraphsOS);
				IStTxtPara newPara = null;
				if (m_textDefn != null)
				{
					// Append insert a new paragraph node.
					XmlElement newParaDefn = InsertNewParagraphDefn(paragraphDefnToInsertAfter);
					newPara = pb.CreateNewParagraph(newParaDefn);
				}
				else
				{
					newPara = pb.AppendParagraph();
				}
				return newPara;
			}

			internal void DeleteText()
			{
				using (new UndoRedoTaskHelper(m_cache, "TextBuilder - Undo DeleteText",
				"TextBuilder - Redo DeleteText"))
				{
					m_text.DeleteUnderlyingObject();
				}
				HvoActualStText = 0;
			}

			internal void DeleteParagraphDefn(IStTxtPara para)
			{
				ParagraphBuilder pb = GetParagraphBuilder(para.Hvo);
				DeleteParagraphDefn(pb.ParagraphDefinition);
			}

			internal void DeleteParagraphDefn(XmlNode paraDefn)
			{
				if (paraDefn != null)
				{
					XmlNode paragraphs = paraDefn.ParentNode;
					paragraphs.RemoveChild(paraDefn);
				}
			}


			/// <summary>
			///
			/// </summary>
			/// <param name="para"></param>
			/// <param name="segNodeToBreakAt"></param>
			/// <returns>the XmlNode corresponding to the new paragraph defn</returns>
			internal XmlNode InsertParagraphBreak(IStTxtPara para, XmlNode startingNodeToMoveToNewPara)
			{
				// StTxtPara/Segments/CmBaseAnnotation[]
				ParagraphBuilder pb = GetParagraphBuilder(para.Hvo);
				// make a new paragraph node after the current one
				if (pb.ParagraphDefinition != null)
				{
					XmlNode newParaDefn = InsertNewParagraphDefn(pb.ParagraphDefinition);
					ParagraphBuilder pbNew = new ParagraphBuilder(m_text.ContentsOA.ParagraphsOS, newParaDefn);
					// if we are breaking at a segment form, create new segment and move the rest of the segment forms
					XmlNode startingSegNode = null;
					if (startingNodeToMoveToNewPara.ParentNode.Name == "SegmentForms37")
					{
						pbNew.CreateSegmentNode();
						XmlNode newSegForms = pbNew.CreateSegmentForms();
						MoveSiblingNodes(startingNodeToMoveToNewPara, newSegForms, null);

						// get the next segment if any.
						int iSeg = XmlUtils.GetIndexAmongSiblings(startingNodeToMoveToNewPara.ParentNode.ParentNode);
						List<XmlNode> segmentNodes = pb.SegmentNodes();
						startingSegNode = segmentNodes[iSeg].NextSibling;
					}
					else
					{
						Debug.Assert(startingNodeToMoveToNewPara.ParentNode.Name == "Segments16");
						startingSegNode = startingNodeToMoveToNewPara;
					}
					if (startingSegNode != null)
					{
						// move all the remaining segments beginning at startingSegNode into the new paragraph.
						MoveSiblingNodes(startingSegNode, pbNew.ParagraphDefinition.LastChild, null);
					}
					return pbNew.ParagraphDefinition;
				}
				return null;
			}

			private static XmlElement InsertNewParagraphDefn(XmlNode paraDefnToInsertAfter)
			{
				XmlNode paragraphs = paraDefnToInsertAfter.ParentNode;
				XmlElement newParaDefn = paragraphs.OwnerDocument.CreateElement("StTxtPara");
				newParaDefn.SetAttribute("id", "");
				paragraphs.InsertAfter(newParaDefn, paraDefnToInsertAfter);
				XmlElement newSegments = newParaDefn.OwnerDocument.CreateElement("Segments16");
				newParaDefn.AppendChild(newSegments);
				return newParaDefn;
			}

			internal void DeleteParagraphBreak(XmlNode paraNodeToDelBreak)
			{
				int iParaToDeleteBreak = XmlUtils.GetIndexAmongSiblings(paraNodeToDelBreak);
				XmlNodeList paragraphs = paraNodeToDelBreak.ParentNode.SelectNodes("StTxtPara");
				XmlNode nextParaNode = paragraphs[iParaToDeleteBreak + 1];
				// StTxtPara/Segments/CmBaseAnnotation[]
				MoveSiblingNodes(nextParaNode.FirstChild.FirstChild, paraNodeToDelBreak.FirstChild, null);
				DeleteParagraphDefn(nextParaNode);
			}
			internal void DeleteParagraphBreak(IStTxtPara para)
			{
				ParagraphBuilder pb1 = GetParagraphBuilder(para.Hvo);
				DeleteParagraphBreak(pb1.ParagraphDefinition);
			}

			internal int HvoActualStText
			{
				get
				{
					if (m_text != null)
						return m_text.ContentsOAHvo;
					else
						return 0;
				}

				set
				{
					if (value == 0)
					{
						m_text = null;
						m_textDefn = null;
					}
					else if (HvoActualStText != value)
					{
						StText stText = new StText(m_cache, value);
						m_text = stText.Owner as FDO.IText;
						m_textDefn = null;
					}
				}

			}

			internal XmlNode SelectNode(IStTxtPara para)
			{
				return SelectNode(para, -1);
			}

			internal XmlNode SelectNode(IStTxtPara para, int iSeg)
			{
				return SelectNode(para, iSeg, -1);
			}

			internal XmlNode SelectNode(IStTxtPara para, int iSeg, int iSegForm)
			{
				if (para == null)
				{
					SelectedNode = m_textDefn;
				}
				else
				{
					ParagraphBuilder pb = this.GetParagraphBuilder(para.Hvo);
					if (iSegForm >= 0)
						SelectedNode = pb.SegmentFormNode(iSeg, iSegForm);
					else if (iSeg >= 0)
						SelectedNode = pb.SegmentNodes()[iSeg];
					else
						SelectedNode = pb.ParagraphDefinition;
				}
				return SelectedNode;
			}

			/// <summary>
			/// The node we have selected as a "cursor" position for editing.
			/// </summary>
			XmlNode m_selectedNode = null;
			internal XmlNode SelectedNode
			{
				get
				{
					if (m_selectedNode != null && m_textDefn != null &&
						m_selectedNode != m_textDefn &&
						m_textDefn.OwnerDocument == m_selectedNode.OwnerDocument)
					{
						// make sure the selected node is owned by the current Text defn
						XmlNodeList decendants = m_textDefn.SelectNodes(String.Format("//{0}", m_selectedNode.LocalName));
						bool fFound = false;
						foreach (XmlNode node in decendants)
						{
							if (m_selectedNode == node)
							{
								fFound = true;
								break;
							}
						}
						if (!fFound)
							m_selectedNode = null;  // reset an invalidated node.
					}
					if (m_selectedNode == null || m_textDefn == null)
						m_selectedNode = m_textDefn;
					return m_selectedNode;
				}

				set
				{
					m_selectedNode = value;
					SetTextDefnFromSelectedNode(value);
				}
			}

			internal XmlNode SetTextDefnFromSelectedNode(XmlNode node)
			{
				// try to find an ancestor StText
				XmlNode stTextNode = null;
				if (node != null)
				{
					if (node.LocalName == "StText")
						stTextNode = node;
					else
						stTextNode = node.SelectSingleNode("ancestor::StText");
					if (stTextNode == null)
						stTextNode = node.SelectSingleNode("//StText");
					this.StTextDefinition = stTextNode;
				}
				return stTextNode;
			}

			internal Text ActualText
			{
				get
				{
					if (m_text == null)
						return null;
					return m_text as Text;
				}
			}

			internal IStText ActualStText
			{
				get
				{
					if (m_text == null || m_text.ContentsOAHvo == 0)
						return null;
					return m_text.ContentsOA;
				}
			}

			internal XmlNode TextDefinition
			{
				get { return m_textDefn; }
			}

			internal XmlNode StTextDefinition
			{
				get
				{
					if (m_textDefn == null)
						return null;
					return m_textDefn.SelectSingleNode("//StText");
				}
				set
				{
					if (value == null)
						m_textDefn = null;
					else
						m_textDefn = value.SelectSingleNode("ancestor::Text");
				}
			}

			/// <summary>
			/// get the paragraph builder for the given hvoPara
			/// </summary>
			/// <param name="hvoPara"></param>
			/// <returns></returns>
			internal ParagraphBuilder GetParagraphBuilder(int hvoPara)
			{
				List<int> hvoParas = new List<int>(m_text.ContentsOA.ParagraphsOS.HvoArray);
				int iPara = hvoParas.IndexOf(hvoPara);
				return new ParagraphBuilder(m_textDefn, m_text, iPara);
			}

			/// <summary>
			/// Note: paraDefn may not yet be mapped to a real paragraph Hvo.
			/// </summary>
			/// <param name="paraDefn"></param>
			/// <returns></returns>
			internal ParagraphBuilder GetParagraphBuilderForNotYetExistingParagraph(XmlNode paraDefn)
			{
				// Paragraphs14/StTxtPara/Segments/
				return new ParagraphBuilder(m_text.ContentsOA.ParagraphsOS, paraDefn);
			}


			void SetName(string name, int ws)
			{
				m_text.Name.SetAlternative(name, ws);
			}

		}

	}

	/// <summary>
	/// </summary>
	[TestFixture]
	public class ParagraphParserTests: InterlinearTestBase
	{
		FDO.IText m_text1 = null;
		XmlDocument m_textsDefn = null;

		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			CheckDisposed();
			base.FixtureSetup();

			m_textsDefn = new XmlDocument();

			InstallVirtuals(@"Language Explorer\Configuration\Main.xml",
				new string[] { "SIL.FieldWorks.FDO.", "SIL.FieldWorks.IText." });
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Guarantees that the default vernacular writing system has the single quote and
		/// hyphen characters as word-forming (which used to be the default based on the old
		/// word-forming overrides XML file).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void SetupOldWordformingOverrides()
		{
			LgWritingSystem wsObj = new LgWritingSystem(m_fdoCache, m_fdoCache.DefaultVernWs);
			ValidCharacters validChars = ValidCharacters.Load(wsObj.ValidChars,
				wsObj.Name.UserDefaultWritingSystem, null);
			bool fChangedSomething = false;
			if (!validChars.IsWordForming('-'))
			{
				validChars.AddCharacter("-");
				validChars.MoveBetweenWordFormingAndOther(new List<string>(new string[] {"-"}), true);
				fChangedSomething = true;
			}
			if (!validChars.IsWordForming('\''))
			{
				validChars.AddCharacter("'");
				validChars.MoveBetweenWordFormingAndOther(new List<string>(new string[] { "'" }), true);
				fChangedSomething = true;
			}
			if (!fChangedSomething)
				return;
			wsObj.ValidChars = validChars.XmlString;
			IWritingSystem wseng = m_fdoCache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(wsObj.Hvo);
			wseng.ValidChars = wsObj.ValidChars;
		}

		/// ------------------------------------------------------------------------------------
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
		/// ------------------------------------------------------------------------------------
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
			m_textsDefn = null;
			m_text1 = null;

			base.Dispose(disposing);
		}

		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();
			SetupOldWordformingOverrides();

			m_text1 = LoadTestText(@"LexText\Interlinear\ITextDllTests\ParagraphParserTestTexts.xml", 1, m_textsDefn);
		}

		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			// UndoEverything before we clear our wordform table, so we can make sure
			// the real wordform list is what we want to start with the next time.
			base.Exit();

			// clear the wordform table.
			m_text1.Cache.LangProject.WordformInventoryOA.ResetAllWordformOccurrences();
			m_text1 = null;
		}

		/// <summary>
		/// Test the virtual handler for the reference of a CmBaseAnnotation
		/// Enhance: It would be nice to test references for more than one paragraph.
		/// </summary>
		[Test]
		public void AnnotationReference()
		{
			CheckDisposed();

			IVwVirtualHandler psvh = BaseFDOPropertyVirtualHandler.GetInstalledHandler(m_fdoCache, "StTxtPara", "Segments");
			IVwVirtualHandler arh = BaseFDOPropertyVirtualHandler.GetInstalledHandler(m_fdoCache, "CmBaseAnnotation", "Reference");
			//m_fdoCache.VwCacheDaAccessor.InstallVirtual(arh);
			//m_fdoCache.VwCacheDaAccessor.InstallVirtual(psvh);

			// Get the id of the 'My Green Mat' text.
			// (This test does depend on this text remaining in TestLangProject. Sorry.)
			string sql = "select obj from CmMajorObject_Name where txt like 'My Gr%'";
			int hvoText;
			DbOps.ReadOneIntFromCommand(m_fdoCache, sql, null, out hvoText);
			Debug.Assert(hvoText != 0, "We expect TestLangProj to have a text with a title starting with 'My Gr'");

			// Now get the ID of the first twfic on the only paragraph
			sql = string.Format("select top 1 cba.id from CmBaseAnnotation_ cba "
				+ "join CmObject para on cba.BeginObject = para.id "
				+ "join CmObject st on para.owner$ = st.id and st.owner$ = {0} "
				+ "order by cba.BeginOffset", hvoText);
			int hvoFirstAnnotation;
			DbOps.ReadOneIntFromCommand(m_fdoCache, sql, null, out hvoFirstAnnotation);

			ISilDataAccess sda = m_fdoCache.MainCacheAccessor;
			string sRef = sda.get_MultiStringAlt(hvoFirstAnnotation, arh.Tag, m_fdoCache.DefaultAnalWs).Text;
			Assert.AreEqual("My Green 1:1", sRef);

			// Now try for one in the second segment. Currently it runs from character 24-48.
			sql = string.Format("select top 1 cba.id from CmBaseAnnotation_ cba "
				+ "join CmObject para on cba.BeginObject = para.id "
				+ "join CmObject st on para.owner$ = st.id and st.owner$ = {0} and cba.BeginOffset > 30 "
				+ "order by cba.BeginOffset", hvoText);
			int hvoSeg2Annotation;
			DbOps.ReadOneIntFromCommand(m_fdoCache, sql, null, out hvoSeg2Annotation);
			sRef = sda.get_MultiStringAlt(hvoSeg2Annotation, arh.Tag, m_fdoCache.DefaultAnalWs).Text;
			Assert.AreEqual("My Green 1:2", sRef);

			int hvoPara = sda.get_ObjectProp(hvoFirstAnnotation,
				(int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject);
			StTxtPara para = StTxtPara.CreateFromDBObject(m_fdoCache, hvoPara) as StTxtPara;
			// try a fake annotation that spans a character within a word.
			int hvoAnnotation1 = CmBaseAnnotation.CreateDummyAnnotation(m_fdoCache, hvoPara, 0, 0, 1, 0);
			sRef = sda.get_MultiStringAlt(hvoAnnotation1, arh.Tag, m_fdoCache.DefaultAnalWs).Text;
			Assert.AreEqual("My Green 1:1", sRef);
			// try a fake annotation that spans the entire paragraph.
			int hvoAnnotation1to3 = CmBaseAnnotation.CreateDummyAnnotation(m_fdoCache, hvoPara, 0, 0, para.Contents.Length, 0);
			sRef = sda.get_MultiStringAlt(hvoAnnotation1to3, arh.Tag, m_fdoCache.DefaultAnalWs).Text;
			Assert.AreEqual("My Green 1:1-3", sRef);
			// try a fake annotation that spans the last two sentences.
			string substringSpanSegments = "la. hes";
			int indexSubstring = para.Contents.Text.IndexOf(substringSpanSegments);
			int hvoAnnotation2to3 = CmBaseAnnotation.CreateDummyAnnotation(m_fdoCache, hvoPara, 0,
				indexSubstring, indexSubstring + substringSpanSegments.Length, 0);
			sRef = sda.get_MultiStringAlt(hvoAnnotation2to3, arh.Tag, m_fdoCache.DefaultAnalWs).Text;
			Assert.AreEqual("My Green 1:2-3", sRef);

			IVwCacheDa cda = m_fdoCache.VwCacheDaAccessor;
			ITsString tssName = sda.get_MultiStringAlt(hvoText, (int)CmMajorObject.CmMajorObjectTags.kflidName,
				m_fdoCache.DefaultAnalWs);
			// Fake an empty text name and check.
			cda.CacheStringAlt(hvoText, (int)CmMajorObject.CmMajorObjectTags.kflidName,
				m_fdoCache.DefaultAnalWs, m_fdoCache.MakeAnalysisTss(""));
			arh.Load(hvoFirstAnnotation, arh.Tag, m_fdoCache.DefaultAnalWs, cda);
			sRef = sda.get_MultiStringAlt(hvoFirstAnnotation, arh.Tag, m_fdoCache.DefaultAnalWs).Text;
			Assert.AreEqual("1:1", sRef);

			// Try with a name less than 5 chars.
			cda.CacheStringAlt(hvoText, (int)CmMajorObject.CmMajorObjectTags.kflidName,
				m_fdoCache.DefaultAnalWs, m_fdoCache.MakeAnalysisTss("abc"));
			arh.Load(hvoFirstAnnotation, arh.Tag, m_fdoCache.DefaultAnalWs, cda);
			sRef = sda.get_MultiStringAlt(hvoFirstAnnotation, arh.Tag, m_fdoCache.DefaultAnalWs).Text;
			Assert.AreEqual("abc 1:1", sRef);

			// Restore name
			cda.CacheStringAlt(hvoText, (int)CmMajorObject.CmMajorObjectTags.kflidName,
				m_fdoCache.DefaultAnalWs, tssName);
		}

		enum Text1ParaIndex { EmptyParagraph = 0, SimpleSegmentPara, ComplexPunctuations, ComplexWordforms, MixedCases, MultipleWritingSystems,
			PhraseWordforms};
		/// <summary>
		/// The actual paragraphs are built from ParagraphParserTestTexts.xml. ParagraphContents is simply for auditing.
		/// </summary>
		string[] ParagraphContents = new string[] {
					// Paragraph 0 - empty paragraph
					"",
					// Paragraph 1 - simple segments
					"xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.",
					// Paragraph 2 - complex punctuation
					"xxxnihimbiligu  xxxyaloni, xxxkasani & xxxnihimbilibi... \"xxxnihimbilira (xxxpus) xxxkasani.\"! ! ! xxxpus",
					// Paragraph 3 - complex wordforms.
					"xxxnihimbili'gu xxxyaloni. xxxkasani1xxxpus xxxnihimbilibi 123. xxxnihimbilira xxxpus-kola xxxkasani.",
					// Paragraph 4 - mixed case.
					"Xxxpus xxxyalola xxxnihimbilira. Xxxnihimbilira xxxpus Xxxyalola. Xxxhesyla XXXNIHIMBILIRA.",
					// Paragraph 5 - multiple vernacular writing systems.
					"xxxpus xxes xxxnihimbilira. xxfr xxen xxxnihimbilira xxxpus xxde. xxkal xxkal xxxxhesyla xxxxhesyla.",
					// Paragraph 6 - PhraseWordforms.
					"xxxpus xxxyalola xxxnihimbilira. xxxpus xxxyalola xxxhesyla xxxnihimbilira. xxxpus xxxyalola xxxnihimbilira."
					};

		private Dictionary<string, int> BuildExpectedOccurrences()
		{
			ILgWritingSystemFactory wsf = Cache.LanguageWritingSystemFactoryAccessor;
			int wsXkal = wsf.GetWsFromStr("xkal");
			int wsEn = wsf.GetWsFromStr("en");
			int wsFr = wsf.GetWsFromStr("fr");
			int wsEs = wsf.GetWsFromStr("es");
			int wsDe = wsf.GetWsFromStr("de");

			Dictionary<string, int> expectedOccurrences = new Dictionary<string, int>();
			// paragraph 1 - simple
			expectedOccurrences["xxxpus" + wsXkal.ToString()] = 11;
			expectedOccurrences["xxxyalola" + wsXkal.ToString()] = 6;
			expectedOccurrences["xxxnihimbilira" + wsXkal.ToString()] = 11;
			// paragraph 2 - complex punctuation
			expectedOccurrences["xxxhesyla" + wsXkal.ToString()] = 2;
			expectedOccurrences["xxxnihimbiligu" + wsXkal.ToString()] = 1;
			expectedOccurrences["xxxyaloni" + wsXkal.ToString()] = 2;
			expectedOccurrences["xxxkasani" + wsXkal.ToString()] = 4;
			expectedOccurrences["xxxnihimbilibi" + wsXkal.ToString()] = 2;
			// paragraph 3 - complex wordform
			expectedOccurrences["xxxnihimbili'gu" + wsXkal.ToString()] = 1;
			expectedOccurrences["xxxpus-kola" + wsXkal.ToString()] = 1;
			// paragraph 4 - mixed case
			expectedOccurrences["Xxxpus" + wsXkal.ToString()] = 1;
			expectedOccurrences["Xxxnihimbilira" + wsXkal.ToString()] = 1;
			expectedOccurrences["Xxxyalola" + wsXkal.ToString()] = 1;
			expectedOccurrences["Xxxhesyla" + wsXkal.ToString()] = 1;
			expectedOccurrences["XXXNIHIMBILIRA" + wsXkal.ToString()] = 1;
			// paragraph 5 - multiple writing
			expectedOccurrences["xxes" + wsEs.ToString()] = 1;
			expectedOccurrences["xxfr" + wsFr.ToString()] = 1;
			expectedOccurrences["xxen" + wsEn.ToString()] = 1;
			expectedOccurrences["xxde" + wsDe.ToString()] = 1;
			expectedOccurrences["xxkal" + wsDe.ToString()] = 1;
			expectedOccurrences["xxkal" + wsXkal.ToString()] = 1;
			// xxxxhesyla: Kalaba (xkal)
			// xxxxhesyla: German (de)
			expectedOccurrences["xxxxhesyla" + wsXkal.ToString()] = 1;
			expectedOccurrences["xxxxhesyla" + wsDe.ToString()] = 1;
			return expectedOccurrences;
		}

		/// <summary>
		/// This checks that our TextBuilder and ParagraphBuilder build texts like we expect.
		/// </summary>
		[Test]
		public void BuildTextFromAnnotations()
		{
			CheckDisposed();

			Assert.IsNotNull(m_text1);
			Assert.AreEqual(m_text1.Name.VernacularDefaultWritingSystem, "Test Text1 for ParagraphParser");
			Assert.IsNotNull(m_text1.ContentsOA);
			Assert.AreEqual(Enum.GetValues(typeof(Text1ParaIndex)).Length, m_text1.ContentsOA.ParagraphsOS.Count);
			// Empty paragraph.
			Assert.AreEqual(ParagraphContents[(int)Text1ParaIndex.EmptyParagraph].Length,
				((IStTxtPara)m_text1.ContentsOA.ParagraphsOS[(int)Text1ParaIndex.EmptyParagraph]).Contents.Length);
			// Simple Segments.
			Assert.AreEqual(ParagraphContents[(int)Text1ParaIndex.SimpleSegmentPara],
				((IStTxtPara)m_text1.ContentsOA.ParagraphsOS[(int)Text1ParaIndex.SimpleSegmentPara]).Contents.Text);
			// Complex Punctuations
			Assert.AreEqual(ParagraphContents[(int)Text1ParaIndex.ComplexPunctuations],
				((IStTxtPara)m_text1.ContentsOA.ParagraphsOS[(int)Text1ParaIndex.ComplexPunctuations]).Contents.Text);
			// Complex Wordforms
			Assert.AreEqual(ParagraphContents[(int)Text1ParaIndex.ComplexWordforms],
				((IStTxtPara)m_text1.ContentsOA.ParagraphsOS[(int)Text1ParaIndex.ComplexWordforms]).Contents.Text);
			// Mixed Cases
			Assert.AreEqual(ParagraphContents[(int)Text1ParaIndex.MixedCases],
				((IStTxtPara)m_text1.ContentsOA.ParagraphsOS[(int)Text1ParaIndex.MixedCases]).Contents.Text);
			// Multiple Writing systems.
			Assert.AreEqual(ParagraphContents[(int)Text1ParaIndex.MultipleWritingSystems],
				((IStTxtPara)m_text1.ContentsOA.ParagraphsOS[(int)Text1ParaIndex.MultipleWritingSystems]).Contents.Text);
			// Phrase wordforms
			Assert.AreEqual(ParagraphContents[(int)Text1ParaIndex.PhraseWordforms],
				((IStTxtPara)m_text1.ContentsOA.ParagraphsOS[(int)Text1ParaIndex.PhraseWordforms]).Contents.Text);
		}

		/// <summary>
		/// Test converting dummy objects to real ones.
		/// </summary>
		[Test]
		public void ConvertDummyToReal()
		{
			CheckDisposed();
			(m_fdoCache.LangProject.WordformInventoryOA as WordformInventory).SuspendUpdatingConcordanceWordforms = true;

			//xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
			IStTxtPara para = (IStTxtPara)m_text1.ContentsOA.ParagraphsOS[(int)Text1ParaIndex.SimpleSegmentPara];
			ParagraphAnnotator ta = new ParagraphAnnotator(para);
			// 1. convert a twfic dummy wordform to real one.
			int hvoCba0_0 =  ta.GetSegmentForm(0,0) ;  // xxxpus
			ICmBaseAnnotation dummyCba0_0 = CmBaseAnnotation.CreateFromDBObject(Cache, hvoCba0_0);
			Assert.IsTrue(dummyCba0_0.IsDummyObject);
			int hvoDummyWordform0_0 = dummyCba0_0.InstanceOfRAHvo;
			IWfiWordform dummyWordform0_0 = WfiWordform.CreateFromDBObject(Cache, hvoDummyWordform0_0);
			Assert.IsTrue(dummyWordform0_0.IsDummyObject);
			// Lookup the form and see if we can find it.
			hvoDummyWordform0_0 = Cache.LangProject.WordformInventoryOA.GetWordformId(dummyWordform0_0.Form.GetAlternativeTss(Cache.DefaultVernWs));
			Assert.AreEqual(dummyWordform0_0.Hvo, hvoDummyWordform0_0);

			IWfiWordform realWordform0_0 = CmObject.ConvertDummyToReal(Cache, dummyWordform0_0.Hvo) as IWfiWordform;
			Assert.IsTrue(realWordform0_0.IsRealObject);
			Assert.AreEqual(dummyWordform0_0.Form.VernacularDefaultWritingSystem,
				realWordform0_0.Form.VernacularDefaultWritingSystem);
			// Make sure we can still lookup the form.
			int hvoRealWordform0_0 = Cache.LangProject.WordformInventoryOA.GetWordformId(realWordform0_0.Form.GetAlternativeTss(Cache.DefaultVernWs));
			Assert.AreEqual(realWordform0_0.Hvo, hvoRealWordform0_0);

			// Enhance: We could check virtual properties for wordform, e.g. wordform Occurrences.

			// 2. convert its twfic annotation to a real one.
			ICmBaseAnnotation realCba0_0 = CmObject.ConvertDummyToReal(Cache, hvoCba0_0) as ICmBaseAnnotation;
			FdoValidator validator = new FdoValidator(para);
			validator.CompareCbas(dummyCba0_0.Hvo, realCba0_0.Hvo,
				String.Format("Para{0}/Segment{1}/SegForm{2}", para.Hvo, 0, 0));

			// 3. convert a twfic annotation with a dummy wordform instanceOf to a real one,
			int hvoCba0_1 = ta.GetSegmentForm(0, 1); // xxxyalola
			ICmBaseAnnotation dummyCba0_1 = CmBaseAnnotation.CreateFromDBObject(Cache, hvoCba0_1);
			Assert.IsTrue(dummyCba0_1.IsDummyObject);
			Assert.IsTrue(Cache.IsDummyObject(dummyCba0_1.InstanceOfRAHvo));
			IWfiWordform dummyWordform0_1 = WfiWordform.CreateFromDBObject(Cache, dummyCba0_1.InstanceOfRAHvo) as IWfiWordform;
			ICmBaseAnnotation realCba0_1 = CmObject.ConvertDummyToReal(Cache, hvoCba0_1) as ICmBaseAnnotation;
			Assert.IsTrue(realCba0_1.IsRealObject);
			validator.CompareCbas(dummyCba0_1.Hvo, realCba0_1.Hvo,
				String.Format("Para{0}/Segment{1}/SegForm{2}", para.Hvo, 0, 1));
			// and confirm the wordform is now real as well.
			IWfiWordform realWordform0_1 = realCba0_1.InstanceOfRA as IWfiWordform;
			Assert.IsNotNull(realWordform0_1);
			Assert.IsTrue(realWordform0_1.IsRealObject);
			Assert.AreEqual(dummyWordform0_1.Form.VernacularDefaultWritingSystem,
				realWordform0_1.Form.VernacularDefaultWritingSystem);

			// 3. Try converting a dummy cba with an analysis to a real one,
			// and check to make sure we haven't altered the basic analysis
			// of another occurrence of the same wordform.
			int hvoCba1_0 = ta.GetSegmentForm(1, 0);  // xxxnihimbilira
			ICmBaseAnnotation dummyCba1_0 = CmBaseAnnotation.CreateFromDBObject(Cache, hvoCba1_0);
			Assert.IsTrue(dummyCba1_0.IsDummyObject);
			int hvoDummyWordform1_0 = dummyCba1_0.InstanceOfRAHvo;
			IWfiWordform dummyWordform1_0 = WfiWordform.CreateFromDBObject(Cache, hvoDummyWordform1_0);
			Assert.IsTrue(dummyWordform1_0.IsDummyObject);
			// this will establish a real wfiGloss analysis and convert cba to real one.
			string wordgloss;
			int hvoWordGloss = ta.SetDefaultWordGloss(1, 0, out wordgloss);
			hvoCba1_0 = ta.GetSegmentForm(1, 0);  // xxxnihimbilira
			Assert.IsFalse(Cache.IsDummyObject(hvoCba1_0));
			Assert.IsTrue(Cache.IsValidObject(hvoCba1_0));

			int hvoCba2_1 = ta.GetSegmentForm(2, 1);  // xxxnihimbilira (third instance)
			ICmBaseAnnotation dummyCba2_1 = CmBaseAnnotation.CreateFromDBObject(Cache, hvoCba2_1);
			Assert.IsTrue(dummyCba2_1.IsDummyObject);
			int hvoDummyWordform2_1 = dummyCba2_1.InstanceOfRAHvo;
			// the basic class of the InstanceOf should still be the same.
			Assert.AreEqual(WfiWordform.kClassId, Cache.GetClassOfObject(hvoDummyWordform2_1),
				"Cba2_1.InstanceOf should be WfiWordform.");
			// Instance of, should be real, however.
			Assert.IsFalse(Cache.IsDummyObject(hvoDummyWordform2_1), "Cba2_1.InstanceOf should not be dummy object.");
			// See if we can get the guess for this twfic.
			ta.LoadParaDefaultAnalyses();
			int tagTwficDefault = StTxtPara.TwficDefaultFlid(Cache);
			int hvoGuess2_1 = Cache.GetObjProperty(hvoCba2_1, tagTwficDefault);
			Assert.IsTrue(Cache.IsValidObject(hvoGuess2_1), "Cba2_1 should have a default analysis (guess).");
			// Convert it to a real, and then see if we still have the guess.
			ICmBaseAnnotation realCba2_1 = CmObject.ConvertDummyToReal(Cache, hvoCba2_1) as ICmBaseAnnotation;
			int hvoGuess2_1_real = Cache.GetObjProperty(realCba2_1.Hvo, tagTwficDefault);
			Assert.AreEqual(hvoGuess2_1, hvoGuess2_1_real, "Cba2_1 has unexpected twfic default after conversion.");
			(m_fdoCache.LangProject.WordformInventoryOA as WordformInventory).SuspendUpdatingConcordanceWordforms = false;
		}

		/// <summary>
		/// Test the annotations for empty paragraph.
		/// </summary>
		[Test]
		public void NoAnalyses_NoEdits_EmptyParagraph()
		{
			CheckDisposed();

			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.EmptyParagraph);
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			tapb.ValidateAnnotations();
		}

		/// <summary>
		/// Test the annotations for simple segment paragraph.
		/// </summary>
		[Test]
		public void NoAnalyses_NoEdits_SimpleSegmentParagraph()
		{
			CheckDisposed();

			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.SimpleSegmentPara);
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			tapb.ValidateAnnotations();
		}

		/// <summary>
		/// Test the annotations for complex punctuation paragraph.
		/// </summary>
		[Test]
		public void NoAnalyses_NoEdits_ComplexPunctuationParagraph()
		{
			CheckDisposed();

			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.ComplexPunctuations);
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			tapb.ValidateAnnotations();
		}

		/// <summary>
		/// Test the annotations for complex wordform paragraph.
		/// </summary>
		[Test]
		public void NoAnalyses_NoEdits_ComplexWordformsParagraph()
		{
			CheckDisposed();
			//SetupOldWordformingOverrides();

			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.ComplexWordforms);
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			tapb.ValidateAnnotations();
		}

		/// <summary>
		/// Test the annotations for mixed case wordform paragraph.
		/// </summary>
		[Test]
		public void NoAnalyses_NoEdits_MixedCaseWordformsParagraph()
		{
			CheckDisposed();

			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.MixedCases);
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			tapb.ValidateAnnotations();
		}

		/// <summary>
		/// Test the annotations for mixed case wordform paragraph.
		/// </summary>
		[Test]
		public void NoAnalyses_NoEdits_MultipleWritingSystemsParagraph()
		{
			CheckDisposed();

			// For now, just make sure we can build the text (and consequently parse it.)
			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.MultipleWritingSystems);
		}

		/// <summary>
		/// Test the annotations for mixed case wordform paragraph.
		/// </summary>
		[Test]
		public void NoAnalyses_NoEdits_MultipleWritingSystemsParagraph_LT5379()
		{
			CheckDisposed();

			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.MultipleWritingSystems);
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			tapb.ValidateAnnotations();
		}

		/// <summary>
		/// Change the writing system of a word in the text after parsing it the first time.
		/// </summary>
		[Test]
		public void NoAnalyses_SimpleEdits_MultipleWritingSystemsParagraph()
		{
			CheckDisposed();
			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.MultipleWritingSystems);
			pb.ParseParagraph(true, true);
			// xxxpus xxes xxxnihimbilira. xxfr xxen xxxnihimbilira xxxpus xxde. xxkal xxkal xxxxhesyla xxxxhesyla.
			//	xxkal: German (de)		-- occurrence 0
			//	xxkal: Kalaba (xkal)	-- occurrence 1
			Dictionary<string, int> expectedOccurrences = pb.ExpectedWordformsAndOccurrences;
			WordformInventory wfi = Cache.LangProject.WordformInventoryOA as WordformInventory;
			CheckExpectedWordformsAndOccurrences(wfi, expectedOccurrences);
			// replace the german occurrence of "xxkal" with a xkal version.
			int hvoSeg2 = pb.ActualParagraph.Segments[2];
			int hvoSegForm2_0 = pb.ActualParagraph.SegmentForms(hvoSeg2)[0];
			StTxtPara.TwficInfo ti2_0 = new StTxtPara.TwficInfo(Cache, hvoSegForm2_0);
			int wsDe = StringUtils.GetWsAtOffset(pb.ActualParagraph.Contents.UnderlyingTsString, ti2_0.Object.BeginOffset);
			int wsVernDef = Cache.DefaultVernWs;
			Assert.AreNotEqual(wsVernDef, wsDe, "did not expect to have a default vern ws.");
			pb.ReplaceSegmentForm(2, 0, "xxkal", wsVernDef);
			expectedOccurrences.Remove("xxkal" + wsDe.ToString());
			expectedOccurrences["xxkal" + wsVernDef.ToString()] += 1;
			pb.RebuildParagraphContentFromAnnotations(true, true);
			CheckExpectedWordformsAndOccurrences(wfi, expectedOccurrences);
		}

		/// <summary>
		/// Add a new ws alternative to an existing wordform.
		/// Then add another alternative that already has an existing hvo.
		/// </summary>
		[Ignore("Need to this when we get more serious about merging wordform information.")]
		[Test]
		public void SimpleAnalyses_SimpleEdits_MultipleWritingSystemsParagraph()
		{
			CheckDisposed();
		}

		[Test]
		public void Phrase_BreakIntoMorphs()
		{
			CheckDisposed();
			// Test word breaks on a standard wordform.
			string baseWord1 = "xxxpus";
			string baseWord1_morphs1 = "xxxpus";
			List<string> morphs = SandboxBase.MorphemeBreaker.BreakIntoMorphs(baseWord1_morphs1, baseWord1);
			Assert.AreEqual(1, morphs.Count,
				String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord1_morphs1, baseWord1));
			Assert.AreEqual("xxxpus", morphs[0]);

			string baseWord1_morphs2 = "xxxpu -s";
			morphs = SandboxBase.MorphemeBreaker.BreakIntoMorphs(baseWord1_morphs2, baseWord1);
			Assert.AreEqual(2, morphs.Count,
				String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord1_morphs2, baseWord1));
			Assert.AreEqual("xxxpu", morphs[0]);
			Assert.AreEqual("-s", morphs[1]);

			string baseWord1_morphs3 = "xxx pu -s";
			morphs = SandboxBase.MorphemeBreaker.BreakIntoMorphs(baseWord1_morphs3, baseWord1);
			Assert.AreEqual(3, morphs.Count,
				String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord1_morphs3, baseWord1));
			Assert.AreEqual("xxx", morphs[0]);
			Assert.AreEqual("pu", morphs[1]);
			Assert.AreEqual("-s", morphs[2]);

			// Test word breaks on a phrase wordform.
			string baseWord2 = "xxxpus xxxyalola";
			string baseWord2_morphs1 = "pus xxxyalola";
			morphs = SandboxBase.MorphemeBreaker.BreakIntoMorphs(baseWord2_morphs1, baseWord2);
			Assert.AreEqual(1, morphs.Count,
				String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord2_morphs1, baseWord2));
			Assert.AreEqual("pus xxxyalola", morphs[0]);

			string baseWord2_morphs2 = "xxxpus xxxyalo  -la";
			morphs = SandboxBase.MorphemeBreaker.BreakIntoMorphs(baseWord2_morphs2, baseWord2);
			Assert.AreEqual(2, morphs.Count,
				String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord2_morphs2, baseWord2));
			Assert.AreEqual("xxxpus xxxyalo", morphs[0]);
			Assert.AreEqual("-la", morphs[1]);

			string baseWord2_morphs3 = "xxxpus  xxxyalo  -la";
			morphs = SandboxBase.MorphemeBreaker.BreakIntoMorphs(baseWord2_morphs3, baseWord2);
			Assert.AreEqual(3, morphs.Count,
				String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord2_morphs3, baseWord2));
			Assert.AreEqual("xxxpus", morphs[0]);
			Assert.AreEqual("xxxyalo", morphs[1]);
			Assert.AreEqual("-la", morphs[2]);

			string baseWord3 = "xxxnihimbilira xxxpus xxxyalola";
			string baseWord3_morphs1 = "xxxnihimbilira xxxpus xxxyalola";
			morphs = SandboxBase.MorphemeBreaker.BreakIntoMorphs(baseWord3_morphs1, baseWord3);
			Assert.AreEqual(1, morphs.Count,
				String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord3_morphs1, baseWord3));
			Assert.AreEqual("xxxnihimbilira xxxpus xxxyalola", morphs[0]);

			string baseWord3_morphs2 = "xxxnihimbili  -ra  xxxpus xxxyalola";
			morphs = SandboxBase.MorphemeBreaker.BreakIntoMorphs(baseWord3_morphs2, baseWord3);
			Assert.AreEqual(3, morphs.Count,
				String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord3_morphs2, baseWord3));
			Assert.AreEqual("xxxnihimbili", morphs[0]);
			Assert.AreEqual("-ra", morphs[1]);
			Assert.AreEqual("xxxpus xxxyalola", morphs[2]);

			string baseWord4 = "xxxpus xxxyalola xxxnihimbilira";
			string baseWord4_morphs1 = "xxxpus xxxyalola xxxnihimbilira";
			morphs = SandboxBase.MorphemeBreaker.BreakIntoMorphs(baseWord4_morphs1, baseWord4);
			Assert.AreEqual(1, morphs.Count,
				String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord4_morphs1, baseWord4));
			Assert.AreEqual("xxxpus xxxyalola xxxnihimbilira", morphs[0]);

			string baseWord4_morphs2 = "xxxpus  xxxyalola xxxnihimbilira";
			morphs = SandboxBase.MorphemeBreaker.BreakIntoMorphs(baseWord4_morphs2, baseWord4);
			Assert.AreEqual(2, morphs.Count,
				String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord4_morphs2, baseWord4));
			Assert.AreEqual("xxxpus", morphs[0]);
			Assert.AreEqual("xxxyalola xxxnihimbilira", morphs[1]);

			string baseWord5 = "kicked the bucket";
			string baseWord5_morphs2 = "kick the bucket  -ed";
			morphs = SandboxBase.MorphemeBreaker.BreakIntoMorphs(baseWord5_morphs2, baseWord5);
			Assert.AreEqual(2, morphs.Count,
				String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord5_morphs2, baseWord5));
			Assert.AreEqual("kick the bucket", morphs[0]);
			Assert.AreEqual("-ed", morphs[1]);
		}

		[Test]
		public void Phrase_MakeAndBreak_UndoRedo()
		{
			CheckDisposed();
			// 1. Make Phrases
			IList<int[]> secondaryPathsToJoinWords = new List<int[]>();
			IList<int[]> secondaryPathsToBreakPhrases = new List<int[]>();
			WordformInventory wfi = m_fdoCache.LangProject.WordformInventoryOA as WordformInventory;

			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.PhraseWordforms);
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			XmlNode paraDef0 = pb.ParagraphDefinition.CloneNode(true);

			XmlNode paraDef_afterJoin1_2;
			using (new UndoRedoTaskHelper(Cache, "Phrase_MakeAndBreak_UndoRedo_Join", "Phrase_MakeAndBreak_UndoRedo_Join"))
			{
				// xxxpus xxxyalola xxxnihimbilira. xxxpus xxxyalola [xxxhesyla xxxnihimbilira]. xxxpus xxxyalola xxxnihimbilira
				tapb.MergeAdjacentAnnotations(1, 2, secondaryPathsToJoinWords, secondaryPathsToBreakPhrases);
				tapb.ValidateAnnotations();
				paraDef_afterJoin1_2 = pb.ParagraphDefinition.CloneNode(true) ;
			}
			UndoResult ures;
			Cache.Undo(out ures);
			WordformInventory.OnChangedWordformsOC();
			pb.ParagraphDefinition = paraDef0;
			pb.ParseParagraph(false, false, false);
			tapb.ValidateAnnotations();
			Cache.Redo(out ures);
			WordformInventory.OnChangedWordformsOC();
			pb.ParagraphDefinition = paraDef_afterJoin1_2;
			pb.ParseParagraph(false, false, false);
			tapb.ValidateAnnotations();
			paraDef_afterJoin1_2 = pb.ParagraphDefinition.CloneNode(true);
			XmlNode paraDef_afterBreak1_2;
			using (new UndoRedoTaskHelper(Cache, "Phrase_MakeAndBreak_UndoRedo_Break", "Phrase_MakeAndBreak_UndoRedo_Break"))
			{
				// xxxpus xxxyalola xxxnihimbilira. xxxpus xxxyalola /xxxhesyla/ /xxxnihimbilira/. xxxpus xxxyalola xxxnihimbilira
				secondaryPathsToJoinWords.Clear();
				secondaryPathsToBreakPhrases.Clear();
				tapb.BreakPhrase(1, 2, secondaryPathsToBreakPhrases, secondaryPathsToJoinWords, null);
				tapb.ValidateAnnotations();
				paraDef_afterBreak1_2 = pb.ParagraphDefinition.CloneNode(true);
			}
			Cache.Undo(out ures);
			WordformInventory.OnChangedWordformsOC();
			pb.ParagraphDefinition = paraDef_afterJoin1_2;
			pb.ParseParagraph(false, false, false);
			tapb.ValidateAnnotations();
			Cache.Redo(out ures);
			WordformInventory.OnChangedWordformsOC();
			pb.ParagraphDefinition = paraDef_afterBreak1_2;
			pb.ParseParagraph(false, false, false);
			tapb.ValidateAnnotations();
		}

		/// <summary>
		/// Test that making and breaking phrases does not delete wordforms occurring elsewhere in a text
		/// that has not been loaded as part of a concordance, because that can invalidate the display
		/// of those wordforms in the Interlinear tabs.
		/// </summary>
		[Test]
		public void LT7974_Phrase_MakeAndBreak()
		{
			CheckDisposed();
			// 1. Make Phrases
			IList<int[]> secondaryPathsToJoinWords = new List<int[]>();
			IList<int[]> secondaryPathsToBreakPhrases = new List<int[]>();

			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.PhraseWordforms);
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			// go ahead and parse the text generating real wordforms to be more like a real user situation (LT-7974)
			// and reset the concordance to simulate the situation where the user hasn't loaded ConcordanceWords yet.
			(Cache.LangProject.WordformInventoryOA as WordformInventory).ResetConcordanceWordformsAndOccurrences();
			bool fDidParse;
			(m_text1.ContentsOA as StText).LastParsedTimestamp = 0;
			ParagraphParser.ParseText(m_text1.ContentsOA, new NullProgressState(), out fDidParse);
			pb.ResyncExpectedAnnotationIds();

			// first do a basic phrase (without secondary phrases (guesses)), which should not delete "xxxpus" or "xxxyalola"
			// since they occur later in the text.
			// [xxxpus xxxyalola] xxxnihimbilira. xxxpus xxxyalola xxxhesyla xxxnihimbilira. xxxpus xxxyalola xxxnihimbilira
			int hvoCba1_0 = tapb.GetSegmentForm(1, 0);	// second xxxpus
			int hvoCba1_1 = tapb.GetSegmentForm(1, 1);	// second xxxyalola
			ICmBaseAnnotation cba1_0 = new CmBaseAnnotation(Cache, hvoCba1_0);
			ICmBaseAnnotation cba1_1 = new CmBaseAnnotation(Cache, hvoCba1_1);
			IWfiWordform wf1 = cba1_0.InstanceOfRA as WfiWordform;
			IWfiWordform wf2 = cba1_0.InstanceOfRA as WfiWordform;
			tapb.MergeAdjacentAnnotations(0, 0, secondaryPathsToJoinWords, secondaryPathsToBreakPhrases);
			// after the merge, make sure the wordforms for xxxpus and xxxyalola are still valid
			Assert.IsTrue(wf1.IsValidObject(), "expected xxxpus to still be valid");
			Assert.IsTrue(wf2.IsValidObject(), "expected xxxyalola to still be valid");
			tapb.ValidateAnnotations(true);

			// now break the phrase and make sure we delete this wordform, b/c it is not occurring elsewhere
			(Cache.LangProject.WordformInventoryOA as WordformInventory).ResetConcordanceWordformsAndOccurrences();
			pb.ResyncExpectedAnnotationIds();
			// \xxxpus\ \xxxyalola\ xxxnihimbilira. xxxpus xxxyalola xxxhesyla xxxnihimbilira. xxxpus xxxyalola xxxnihimbilira
			int hvoCba0_0 = tapb.GetSegmentForm(0, 0);	// xxxpus xxxyalola
			ICmBaseAnnotation cba0_0 = new CmBaseAnnotation(Cache, hvoCba0_0);
			IWfiWordform wf3 = cba0_0.InstanceOfRA as WfiWordform;
			secondaryPathsToJoinWords.Clear();
			secondaryPathsToBreakPhrases.Clear();
			tapb.BreakPhrase(0, 0, secondaryPathsToBreakPhrases, secondaryPathsToJoinWords, null);
			Assert.IsFalse(wf3.IsValidObject(), "expected 'xxxpus xxxyalola' to be deleted.");
			tapb.ValidateAnnotations(true);

			// rejoin the first phrase, parse the text, and break the first phrase
			// [xxxpus xxxyalola] xxxnihimbilira. {xxxpus xxxyalola} xxxhesyla xxxnihimbilira. {xxxpus xxxyalola} xxxnihimbilira
			secondaryPathsToJoinWords.Add(new int[2] { 1, 0 }); // {xxxpus xxxyalola} xxxhesyla
			secondaryPathsToJoinWords.Add(new int[2] { 2, 0 }); // {xxxpus xxxyalola} xxxnihimbilira
			tapb.MergeAdjacentAnnotations(0, 0, secondaryPathsToJoinWords, secondaryPathsToBreakPhrases);
			hvoCba0_0 = tapb.GetSegmentForm(0, 0);	// xxxpus xxxyalola
			cba0_0 = new CmBaseAnnotation(Cache, hvoCba0_0);
			wf1 = cba0_0.InstanceOfRA as WfiWordform;
			tapb.ReparseParagraph();
			// just break the new phrase (and the leave existing guesses)
			// \xxxpus\ \xxxyalola\ xxxnihimbilira. {xxxpus xxxyalola} xxxhesyla xxxnihimbilira. {xxxpus xxxyalola} xxxnihimbilira
			(Cache.LangProject.WordformInventoryOA as WordformInventory).ResetConcordanceWordformsAndOccurrences();
			secondaryPathsToJoinWords.Clear();
			tapb.BreakPhrase(0, 0, secondaryPathsToBreakPhrases, secondaryPathsToJoinWords, null);
			Assert.IsTrue(wf1.IsValidObject(), "expected 'xxxpus xxxyalola' to still be valid");
			tapb.ValidateAnnotations(true);
			// xxxpus xxxyalola xxxnihimbilira. \xxxpus\ \xxxyalola\ xxxhesyla xxxnihimbilira. {xxxpus xxxyalola} xxxnihimbilira
			tapb.BreakPhrase(1, 0, secondaryPathsToBreakPhrases, secondaryPathsToJoinWords, null);
			Assert.IsTrue(wf1.IsValidObject(), "expected 'xxxpus xxxyalola' to still be valid");
			tapb.ValidateAnnotations(true);
			// xxxpus xxxyalola xxxnihimbilira. xxxpus xxxyalola xxxhesyla xxxnihimbilira. \xxxpus\ \xxxyalola\ xxxnihimbilira
			tapb.BreakPhrase(2, 0, secondaryPathsToBreakPhrases, secondaryPathsToJoinWords, null);
			Assert.IsFalse(wf1.IsValidObject(), "expected 'xxxpus xxxyalola' to be deleted");
			tapb.ValidateAnnotations(true);

			// now do two joins, the second resulting in deletion of a wordform.
			// xxxpus xxxyalola xxxnihimbilira. xxxpus [xxxyalola xxxhesyla] xxxnihimbilira. xxxpus xxxyalola xxxnihimbilira
			// xxxpus xxxyalola xxxnihimbilira. xxxpus [xxxyalola xxxhesyla xxxnihimbilira]. xxxpus xxxyalola xxxnihimbilira
			(Cache.LangProject.WordformInventoryOA as WordformInventory).ResetConcordanceWordformsAndOccurrences();
			(m_text1.ContentsOA as StText).LastParsedTimestamp = 0;
			ParagraphParser.ParseText(m_text1.ContentsOA, new NullProgressState(), out fDidParse);
			pb.ResyncExpectedAnnotationIds();
			tapb.MergeAdjacentAnnotations(1, 1, secondaryPathsToJoinWords, secondaryPathsToBreakPhrases);
			hvoCba1_1 = tapb.GetSegmentForm(1, 1);	// xxxyalola xxxhesyla
			cba1_1 = new CmBaseAnnotation(Cache, hvoCba1_1);
			IWfiWordform wf4 = cba1_1.InstanceOfRA as WfiWordform;
			tapb.MergeAdjacentAnnotations(1, 1, secondaryPathsToJoinWords, secondaryPathsToBreakPhrases);
			// this merge should have deleted 'xxxyalola xxxhesyla'
			Assert.IsFalse(wf4.IsValidObject(), "expected 'xxxyalola xxxhesyla' to be deleted.");
			tapb.ValidateAnnotations(true);
		}

		/// <summary>
		/// Test the annotations for paragraph with phrase-wordform annotations.
		/// </summary>
		[Test]
		//[Ignore("FWC-16: this test causes NUnit to hang in fixture teardown - need more investigation")]
		public void Phrase_MakeAndBreak()
		{
			CheckDisposed();
			// 1. Make Phrases
			IList<int[]> secondaryPathsToJoinWords = new List<int[]>();
			IList<int[]> secondaryPathsToBreakPhrases = new List<int[]>();

			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.PhraseWordforms);
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);

			// first do a basic phrase (without secondary phrases (guesses)) and break it
			// [xxxpus xxxyalola] xxxnihimbilira. xxxpus xxxyalola xxxhesyla xxxnihimbilira. xxxpus xxxyalola xxxnihimbilira
			tapb.MergeAdjacentAnnotations(0, 0, secondaryPathsToJoinWords, secondaryPathsToBreakPhrases);
			tapb.ValidateAnnotations(true);

			// \xxxpus\ \xxxyalola\ xxxnihimbilira. xxxpus xxxyalola xxxhesyla xxxnihimbilira. xxxpus xxxyalola xxxnihimbilira
			secondaryPathsToJoinWords.Clear();
			secondaryPathsToBreakPhrases.Clear();
			tapb.BreakPhrase(0, 0, secondaryPathsToBreakPhrases, secondaryPathsToJoinWords, null);
			tapb.ValidateAnnotations(true);

			// make phrases with secondary phrases (guesses).
			// [xxxpus xxxyalola] xxxnihimbilira. {xxxpus xxxyalola} xxxhesyla xxxnihimbilira. {xxxpus xxxyalola} xxxnihimbilira
			secondaryPathsToJoinWords.Clear();
			secondaryPathsToBreakPhrases.Clear();
			secondaryPathsToJoinWords.Add(new int[2] { 1, 0 }); // {xxxpus xxxyalola} xxxhesyla
			secondaryPathsToJoinWords.Add(new int[2] { 2, 0 }); // {xxxpus xxxyalola} xxxnihimbilira
			tapb.MergeAdjacentAnnotations(0, 0, secondaryPathsToJoinWords, secondaryPathsToBreakPhrases);
			// then check that the last phrase has been properly annotated.
			tapb.ValidateAnnotations();

			// [xxxpus xxxyalola xxxnihimbilira]. xxxpus xxxyalola xxxhesyla xxxnihimbilira. {xxxpus xxxyalola xxxnihimbilira}.
			secondaryPathsToJoinWords.Clear();
			secondaryPathsToBreakPhrases.Clear();
			secondaryPathsToBreakPhrases.Add(new int[2] { 1, 0 }); // \xxxpus\ \xxxyalola\ xxxhesyla
			secondaryPathsToJoinWords.Add(new int[2] { 2, 0 }); // {{xxxpus xxxyalola} xxxnihimbilira}
			tapb.MergeAdjacentAnnotations(0, 0, secondaryPathsToJoinWords, secondaryPathsToBreakPhrases);
			tapb.ValidateAnnotations();

			// [xxxpus xxxyalola xxxnihimbilira]. [xxxpus xxxyalola] xxxhesyla xxxnihimbilira. {xxxpus xxxyalola xxxnihimbilira}
			secondaryPathsToJoinWords.Clear();
			secondaryPathsToBreakPhrases.Clear();
			tapb.MergeAdjacentAnnotations(1, 0, secondaryPathsToJoinWords, secondaryPathsToBreakPhrases);
			tapb.ValidateAnnotations();

			// 2. Break Phrases.
			// [xxxpus xxxyalola xxxnihimbilira]. \xxxpus\ \xxxyalola\ xxxhesyla xxxnihimbilira. {xxxpus xxxyalola xxxnihimbilira}
			secondaryPathsToJoinWords.Clear();
			secondaryPathsToBreakPhrases.Clear();
			tapb.BreakPhrase(1, 0, secondaryPathsToBreakPhrases, secondaryPathsToJoinWords, null);
			tapb.ValidateAnnotations();

			// [\xxxpus\ \xxxyalola\ \xxxnihimbilira\]. xxxpus xxxyalola xxxhesyla xxxnihimbilira. {\xxxpus\ \xxxyalola\ \xxxnihimbilira\}
			secondaryPathsToBreakPhrases.Clear();
			secondaryPathsToBreakPhrases.Add(new int[2] { 2, 0 }); // {\xxxpus\ \xxxyalola\ \xxxnihimbilira\}
			tapb.BreakPhrase(0, 0, secondaryPathsToBreakPhrases, secondaryPathsToJoinWords, null);
			tapb.ValidateAnnotations();

			// gloss the second occurrence of xxxyalola and check that we don't overwrite it with a secondary guess.
			secondaryPathsToJoinWords.Clear();
			secondaryPathsToBreakPhrases.Clear();
			// [xxxpus xxxyalola] xxxnihimbilira. xxxpus [xxxyalola] xxxhesyla xxxnihimbilira. {xxxpus xxxyalola} xxxnihimbilira
			tapb.SetDefaultWordGloss("xxxyalola", 1);
			secondaryPathsToJoinWords.Add(new int[2] { 2, 0 }); // {xxxpus xxxyalola} xxxnihimbilira
			tapb.MergeAdjacentAnnotations(0, 0, secondaryPathsToJoinWords, secondaryPathsToBreakPhrases);
			tapb.ValidateAnnotations();	// reparse so that we can 'confirm' "xxxpus xxxyalola" phrase.

			tapb.SetDefaultWordGloss("xxxpus xxxyalola", 0);	// 'confirm' this merge.
			tapb.ValidateAnnotations();

			// join the last occurrence of 'xxxpus xxxyalola xxxnihimbilira'
			// and check that we don't overwrite the first join of 'xxxpus xxxyalola' with a secondary guess.
			secondaryPathsToJoinWords.Clear();
			secondaryPathsToBreakPhrases.Clear();
			// [xxxpus xxxyalola] xxxnihimbilira. xxxpus [xxxyalola] xxxhesyla xxxnihimbilira. [xxxpus xxxyalola xxxnihimbilira]
			tapb.MergeAdjacentAnnotations(2, 0, secondaryPathsToJoinWords, secondaryPathsToBreakPhrases);
			tapb.ValidateAnnotations();	// reparse so that we can 'confirm' "xxxpus xxxyalola xxxnihimbilira" phrase.

			tapb.SetDefaultWordGloss("xxxpus xxxyalola xxxnihimbilira", 0);	// 'confirm' this merge.
			tapb.ValidateAnnotations();

			// make sure we can break our analysis.
			secondaryPathsToJoinWords.Clear();
			secondaryPathsToBreakPhrases.Clear();
			// [xxxpus xxxyalola] xxxnihimbilira. xxxpus [xxxyalola] xxxhesyla xxxnihimbilira. {\xxxpus\ \xxxyalola\} \xxxnihimbilira\
			secondaryPathsToJoinWords.Add(new int[2] { 2, 0 }); // {xxxpus xxxyalola} xxxnihimbilira
			tapb.BreakPhrase(2, 0, secondaryPathsToBreakPhrases, secondaryPathsToJoinWords, "xxxpus xxxyalola");
			tapb.ValidateAnnotations();
		}

		/// <summary>
		/// Test editing the text after making a phrase.
		/// </summary>
		[Test]
		public void Phrase_SimpleEdits_LT6244()
		{
			CheckDisposed();

			// 1. Make Phrases
			IList<int[]> secondaryPathsToJoinWords = new List<int[]>();
			IList<int[]> secondaryPathsToBreakPhrases = new List<int[]>();

			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.PhraseWordforms);
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);

			// first do a basic phrase (without secondary phrases (guesses))
			// xxxpus xxxyalola xxxnihimbilira. xxxpus xxxyalola [xxxhesyla xxxnihimbilira]. xxxpus xxxyalola xxxnihimbilira
			tapb.MergeAdjacentAnnotations(1, 2, secondaryPathsToJoinWords, secondaryPathsToBreakPhrases);
			tapb.ValidateAnnotations(true);

			// edit the second word in the phrase.
			pb.ReplaceSegmentForm("xxxhesyla xxxnihimbilira", 0, "xxxhesyla xxxra");
			pb.BreakPhraseAnnotation(1, 2);  // this edit should break the phrase back into words.
			tapb.ValidateAnnotations();
		}

		[Test]
		public void SparseSegmentAnalyses_FreeformAnnotations_LT7318()
		{
			// Make some analyses linked to the same LexSense.
			//<!--xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.-->
			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.SimpleSegmentPara);
			List<int> segments = (pb.ActualParagraph as StTxtPara).Segments;
			Assert.AreEqual(3, segments.Count, "Unexpected number of senses.");

			// Verify that each of these segments are dummy
			Assert.IsTrue(Cache.IsDummyObject(segments[0]), "Expected dummy segment.");
			Assert.IsTrue(Cache.IsDummyObject(segments[1]), "Expected dummy segment.");
			Assert.IsTrue(Cache.IsDummyObject(segments[2]), "Expected dummy segment.");

			// Load free form annotations for segments. shouldn't have any.
			Set<int> analWsIds = new Set<int>(Cache.LangProject.AnalysisWssRC.HvoArray);
			StTxtPara.LoadSegmentFreeformAnnotationData(Cache, new Set<int>(segments), analWsIds);
			int tagSegFF = StTxtPara.SegmentFreeformAnnotationsFlid(Cache);
			int[] segFFs = Cache.GetVectorProperty(segments[0], tagSegFF, true);
			Assert.AreEqual(0, segFFs.Length, "Segment 0 should not have any freeform annotations.");
			StTxtPara.LoadSegmentFreeformAnnotationData(Cache, pb.ActualParagraph.Hvo, analWsIds);
			segFFs = Cache.GetVectorProperty(segments[1], tagSegFF, true);
			Assert.AreEqual(0, segFFs.Length, "Segment 1 should not have any freeform annotations.");
			segFFs = Cache.GetVectorProperty(segments[2], tagSegFF, true);
			Assert.AreEqual(0, segFFs.Length, "Segment 2 should not have any freeform annotations.");

			// convert the third segment to a real, in preparation for adding the annotation.
			ICmBaseAnnotation cbaReal = CmBaseAnnotation.ConvertBaseAnnotationToReal(m_fdoCache, segments[2]);
			segments = (pb.ActualParagraph as StTxtPara).Segments;
			Assert.IsTrue(!Cache.IsDummyObject(segments[2]), "Expected real segment.");

			// Try adding some freeform translations to third segment.
			int segDefn_literalTranslation = Cache.GetIdFromGuid(LangProject.kguidAnnLiteralTranslation);
			int segDefn_freeTranslation = Cache.GetIdFromGuid(LangProject.kguidAnnFreeTranslation);
			BaseFreeformAdder ffAdder = new BaseFreeformAdder(Cache);
			ICmIndirectAnnotation freeTrans0 = ffAdder.AddFreeformAnnotation(segments[2], segDefn_freeTranslation);
			freeTrans0.Comment.SetAlternative("Segment2: Freeform translation.", Cache.DefaultAnalWs);
			ICmIndirectAnnotation literalTrans0 = ffAdder.AddFreeformAnnotation(segments[2], segDefn_literalTranslation);
			literalTrans0.Comment.SetAlternative("Segment2: Literal translation.", Cache.DefaultAnalWs);

			// see if we can load this into the cache.
			StTxtPara.LoadSegmentFreeformAnnotationData(Cache, new Set<int>(segments), analWsIds);
			segFFs = Cache.GetVectorProperty(segments[2], tagSegFF, true);
			Assert.AreEqual(2, segFFs.Length, "Segment 2 should have freeform annotations.");
			Assert.AreEqual(segFFs[0], freeTrans0.Hvo, "Segment 2 Freeform translation id.");
			Assert.AreEqual(segFFs[1], literalTrans0.Hvo, "Segment 2 Literal translation id.");
			// make sure the other segments don't have freeform annotations.
			segFFs = Cache.GetVectorProperty(segments[0], tagSegFF, true);
			Assert.AreEqual(0, segFFs.Length, "Segment 0 should not have any freeform annotations.");
			StTxtPara.LoadSegmentFreeformAnnotationData(Cache, pb.ActualParagraph.Hvo, analWsIds);
			segFFs = Cache.GetVectorProperty(segments[1], tagSegFF, true);
			Assert.AreEqual(0, segFFs.Length, "Segment 1 should not have any freeform annotations.");

			// reparse the paragraph and make sure nothing changed.
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			tapb.ReparseParagraph();
			segments = (pb.ActualParagraph as StTxtPara).Segments;

			StTxtPara.LoadSegmentFreeformAnnotationData(Cache, new Set<int>(segments), analWsIds);
			segFFs = Cache.GetVectorProperty(segments[2], tagSegFF, true);
			Assert.AreEqual(2, segFFs.Length, "Segment 2 should have freeform annotations.");
			Assert.AreEqual(segFFs[0], freeTrans0.Hvo, "Segment 2 Freeform translation id.");
			Assert.AreEqual(segFFs[1], literalTrans0.Hvo, "Segment 2 Literal translation id.");
			// make sure the other segments don't have freeform annotations.
			segFFs = Cache.GetVectorProperty(segments[0], tagSegFF, true);
			Assert.AreEqual(0, segFFs.Length, "Segment 0 should not have any freeform annotations.");
			StTxtPara.LoadSegmentFreeformAnnotationData(Cache, pb.ActualParagraph.Hvo, analWsIds);
			segFFs = Cache.GetVectorProperty(segments[1], tagSegFF, true);
			Assert.AreEqual(0, segFFs.Length, "Segment 1 should not have any freeform annotations.");
		}

		[Test]
		public void FindExampleSentences()
		{
			// Make some analyses linked to the same LexSense.
			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.SimpleSegmentPara);
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			// Create a new lexical entry and sense.
			int clsidForm;
			string formLexEntry = "xnihimbilira";
			ITsString tssLexEntryForm = StringUtils.MakeTss(formLexEntry, Cache.DefaultVernWs);
			ILexEntry xnihimbilira_Entry = LexEntry.CreateEntry(Cache,
				MoMorphType.FindMorphType(Cache, new MoMorphTypeCollection(Cache), ref formLexEntry, out clsidForm), tssLexEntryForm,
				"xnihimbilira.sense1", null);

			ILexSense xnihimbilira_Sense1 = xnihimbilira_Entry.SensesOS[0];
			ILexSense xnihimbilira_Sense2 = LexSense.CreateSense(xnihimbilira_Entry, null, "xnihimbilira.sense2");
			//<!--xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.-->
			ArrayList moForms = new ArrayList();
			moForms.Add("xx");
			moForms.Add(xnihimbilira_Entry.LexemeFormOA);
			// 1. Establish first analysis with Sense1
			IWfiAnalysis wfiAnalysis1 = tapb.BreakIntoMorphs(1, 0, moForms);
			tapb.SetMorphSense(1, 0, 1, xnihimbilira_Sense1);
			List<int> instancesInTwfics_Sense1 = (xnihimbilira_Sense1 as LexSense).InstancesInTwfics;
			List<int> instancesInTwfics_Sense2 = (xnihimbilira_Sense2 as LexSense).InstancesInTwfics;
			Assert.AreEqual(1, instancesInTwfics_Sense1.Count,
				String.Format("Unexpected number of instances of sense '{0}'", xnihimbilira_Sense1.Gloss.AnalysisDefaultWritingSystem));
			Assert.AreEqual(0, instancesInTwfics_Sense2.Count,
				String.Format("Unexpected number of instances of sense '{0}'", xnihimbilira_Sense2.Gloss.AnalysisDefaultWritingSystem));
			StTxtPara.TwficInfo infoCba1_0 = new StTxtPara.TwficInfo(Cache, instancesInTwfics_Sense1[0]);
			Assert.AreEqual(pb.ActualParagraph.Hvo, infoCba1_0.Object.BeginObjectRAHvo);
			Assert.AreEqual(1, infoCba1_0.SegmentIndex,
				String.Format("Unexpected index of segment '{0}'", infoCba1_0.SegmentHvo));

			WfiAnalysis waCba1_0 = infoCba1_0.Object.InstanceOfRA as WfiAnalysis;
			Assert.IsNotNull(waCba1_0,
				String.Format("Unexpected class({0}) of InstanceOf({1}) for cba0.", infoCba1_0.Object.InstanceOfRA.ClassID, infoCba1_0.Object.InstanceOfRAHvo));
			Assert.AreEqual(xnihimbilira_Sense1.Hvo, waCba1_0.MorphBundlesOS[1].SenseRAHvo);

			// get the segment information for the twfics.
			List<int> segments_Sense1 = StTxtPara.TwficSegments(Cache, instancesInTwfics_Sense1);
			Assert.AreEqual(1, segments_Sense1.Count, "Unexpected number of senses for twfics.");
			Assert.AreEqual(infoCba1_0.SegmentHvo, segments_Sense1[0], "Unexpected segment hvo.");

			// 2. Establish word gloss on the existing analysis.
			string wordGloss1;
			tapb.SetDefaultWordGloss(2, 1, wfiAnalysis1, out wordGloss1);
			instancesInTwfics_Sense1 = (xnihimbilira_Sense1 as LexSense).InstancesInTwfics;
			Assert.AreEqual(2, instancesInTwfics_Sense1.Count,
				String.Format("Unexpected number of instances of sense '{0}'", xnihimbilira_Sense1.Gloss.AnalysisDefaultWritingSystem));

			infoCba1_0 = new StTxtPara.TwficInfo(Cache, instancesInTwfics_Sense1[0]);
			Assert.AreEqual(pb.ActualParagraph.Hvo, infoCba1_0.Object.BeginObjectRAHvo);
			Assert.AreEqual(1, infoCba1_0.SegmentIndex,
				String.Format("Unexpected index of segment '{0}'", infoCba1_0.SegmentHvo));

			StTxtPara.TwficInfo infoCba2_1 = new StTxtPara.TwficInfo(Cache, instancesInTwfics_Sense1[1]);
			Assert.AreEqual(pb.ActualParagraph.Hvo, infoCba2_1.Object.BeginObjectRAHvo);
			Assert.AreEqual(2, infoCba2_1.SegmentIndex,
				String.Format("Unexpected index of segment '{0}'", infoCba2_1.SegmentHvo));

			waCba1_0 = infoCba1_0.Object.InstanceOfRA as WfiAnalysis;
			Assert.IsNotNull(waCba1_0,
				String.Format("Unexpected class({0}) of InstanceOf({1}) for cba0.", infoCba1_0.Object.InstanceOfRA.ClassID, infoCba1_0.Object.InstanceOfRAHvo));
			Assert.AreEqual(xnihimbilira_Sense1.Hvo, waCba1_0.MorphBundlesOS[1].SenseRAHvo);

			WfiGloss wgCba2_1 = infoCba2_1.Object.InstanceOfRA as WfiGloss;
			Assert.IsNotNull(wgCba2_1,
				String.Format("Unexpected class({0}) of InstanceOf({1}) for cba1.", infoCba2_1.Object.InstanceOfRA.ClassID, infoCba2_1.Object.InstanceOfRAHvo));
			WfiAnalysis waCba2_1 = WfiAnalysis.CreateFromDBObject(Cache, wgCba2_1.OwnerHVO) as WfiAnalysis;
			Assert.AreEqual(xnihimbilira_Sense1.Hvo, waCba2_1.MorphBundlesOS[1].SenseRAHvo);

			segments_Sense1 = StTxtPara.TwficSegments(Cache, instancesInTwfics_Sense1);
			Assert.AreEqual(2, segments_Sense1.Count, "Unexpected number of senses for twfics.");
			Assert.AreEqual(infoCba1_0.SegmentHvo, segments_Sense1[0], "Unexpected segment hvo.");
			Assert.AreEqual(infoCba2_1.SegmentHvo, segments_Sense1[1], "Unexpected segment hvo.");

			// 3. establish a new analysis with Sense1.
			IWfiAnalysis wfiAnalysis2 = tapb.BreakIntoMorphs(0, 2, moForms); // xxxnihimbilira (first occurrence)
			tapb.SetMorphSense(0, 2, 1, xnihimbilira_Sense1);
			instancesInTwfics_Sense1 = (xnihimbilira_Sense1 as LexSense).InstancesInTwfics;
			Assert.AreEqual(3, instancesInTwfics_Sense1.Count,
				String.Format("Unexpected number of instances of sense '{0}'", xnihimbilira_Sense1.Gloss.AnalysisDefaultWritingSystem));

			StTxtPara.TwficInfo infoCba0_2 = new StTxtPara.TwficInfo(Cache, instancesInTwfics_Sense1[0]);
			Assert.AreEqual(pb.ActualParagraph.Hvo, infoCba0_2.Object.BeginObjectRAHvo);
			Assert.AreEqual(0, infoCba0_2.SegmentIndex,
				String.Format("Unexpected index of segment '{0}'", infoCba0_2.SegmentHvo));

			WfiAnalysis waCba0_2 = infoCba0_2.Object.InstanceOfRA as WfiAnalysis;
			Assert.IsNotNull(waCba0_2,
				String.Format("Unexpected class({0}) of InstanceOf({1}) for cba0.", infoCba0_2.Object.InstanceOfRA.ClassID, infoCba0_2.Object.InstanceOfRAHvo));
			Assert.AreEqual(xnihimbilira_Sense1.Hvo, waCba0_2.MorphBundlesOS[1].SenseRAHvo);

			segments_Sense1 = StTxtPara.TwficSegments(Cache, instancesInTwfics_Sense1);
			Assert.AreEqual(3, segments_Sense1.Count, "Unexpected number of senses for twfics.");
			Assert.AreEqual(infoCba0_2.SegmentHvo, segments_Sense1[0], "Unexpected segment hvo.");
			Assert.AreEqual(infoCba1_0.SegmentHvo, segments_Sense1[1], "Unexpected segment hvo.");
			Assert.AreEqual(infoCba2_1.SegmentHvo, segments_Sense1[2], "Unexpected segment hvo.");

			// 4. change an existing sense to sense2.
			tapb.SetMorphSense(0, 2, 1, xnihimbilira_Sense2);
			instancesInTwfics_Sense1 = (xnihimbilira_Sense1 as LexSense).InstancesInTwfics;
			Assert.AreEqual(2, instancesInTwfics_Sense1.Count,
				String.Format("Unexpected number of instances of sense '{0}'", xnihimbilira_Sense1.Gloss.AnalysisDefaultWritingSystem));
			instancesInTwfics_Sense2 = (xnihimbilira_Sense2 as LexSense).InstancesInTwfics;
			Assert.AreEqual(1, instancesInTwfics_Sense2.Count,
				String.Format("Unexpected number of instances of sense '{0}'", xnihimbilira_Sense2.Gloss.AnalysisDefaultWritingSystem));

			infoCba0_2 = new StTxtPara.TwficInfo(Cache, instancesInTwfics_Sense2[0]);
			Assert.AreEqual(pb.ActualParagraph.Hvo, infoCba0_2.Object.BeginObjectRAHvo);
			Assert.AreEqual(0, infoCba0_2.SegmentIndex,
				String.Format("Unexpected index of segment '{0}'", infoCba0_2.SegmentHvo));

			waCba0_2 = infoCba0_2.Object.InstanceOfRA as WfiAnalysis;
			Assert.IsNotNull(waCba0_2,
				String.Format("Unexpected class({0}) of InstanceOf({1}) for cba0.", infoCba0_2.Object.InstanceOfRA.ClassID, infoCba0_2.Object.InstanceOfRAHvo));
			Assert.AreEqual(xnihimbilira_Sense2.Hvo, waCba0_2.MorphBundlesOS[1].SenseRAHvo);

			// do multiple occurrences of the same sense in the same segment.
			IWfiAnalysis wfiAnalysis3 = tapb.BreakIntoMorphs(0, 0, moForms); // break xxxpus into xx xnihimbilira (for fun).
			tapb.SetMorphSense(0, 0, 1, xnihimbilira_Sense2);
			instancesInTwfics_Sense2 = (xnihimbilira_Sense2 as LexSense).InstancesInTwfics;
			Assert.AreEqual(2, instancesInTwfics_Sense2.Count,
				String.Format("Unexpected number of instances of sense '{0}'", xnihimbilira_Sense2.Gloss.AnalysisDefaultWritingSystem));
			StTxtPara.TwficInfo infoCba0_0 = new StTxtPara.TwficInfo(Cache, instancesInTwfics_Sense2[0]);

			// reparse paragraph to convert all segments with real analyses to real ones.
			tapb.ReparseParagraph();
			infoCba0_0.ReloadInfo();
			infoCba0_2.ReloadInfo();
			infoCba1_0.ReloadInfo();
			infoCba2_1.ReloadInfo();

			List<int> segments_Sense2 = StTxtPara.TwficSegments(Cache, instancesInTwfics_Sense2);
			Assert.AreEqual(2, segments_Sense2.Count, "Unexpected number of senses for twfics.");
			Assert.AreEqual(infoCba0_0.SegmentHvo, segments_Sense2[0], "Unexpected segment hvo.");
			Assert.AreEqual(infoCba0_2.SegmentHvo, segments_Sense2[1], "Unexpected segment hvo.");

			// Load free form annotations for segments Sense 2.
			Set<int> analWsIds = new Set<int>(Cache.LangProject.AnalysisWssRC.HvoArray);
			StTxtPara.LoadSegmentFreeformAnnotationData(Cache, new Set<int>(segments_Sense2), analWsIds);
			int tagSegFF = StTxtPara.SegmentFreeformAnnotationsFlid(Cache);
			int[] segFFs = Cache.GetVectorProperty(infoCba0_0.SegmentHvo, tagSegFF, true);
			Assert.AreEqual(0, segFFs.Length, "Segment 0 should not have any freeform annotations.");
			StTxtPara.LoadSegmentFreeformAnnotationData(Cache, pb.ActualParagraph.Hvo, analWsIds);
			segFFs = Cache.GetVectorProperty(infoCba0_0.SegmentHvo, tagSegFF, true);
			Assert.AreEqual(0, segFFs.Length, "Segment 0 should not have any freeform annotations.");
			segFFs = Cache.GetVectorProperty(infoCba1_0.SegmentHvo, tagSegFF, true);
			Assert.AreEqual(0, segFFs.Length, "Segment 1 should not have any freeform annotations.");
			segFFs = Cache.GetVectorProperty(infoCba2_1.SegmentHvo, tagSegFF, true);
			Assert.AreEqual(0, segFFs.Length, "Segment 2 should not have any freeform annotations.");

			// Try adding some freeform translations
			int segDefn_literalTranslation = Cache.GetIdFromGuid(LangProject.kguidAnnLiteralTranslation);
			int segDefn_freeTranslation = Cache.GetIdFromGuid(LangProject.kguidAnnFreeTranslation);
			BaseFreeformAdder ffAdder = new BaseFreeformAdder(Cache);
			ICmIndirectAnnotation freeTrans0 = ffAdder.AddFreeformAnnotation(infoCba0_0.SegmentHvo, segDefn_freeTranslation);
			freeTrans0.Comment.SetAlternative("Segment0: Freeform translation.", Cache.DefaultAnalWs);
			ICmIndirectAnnotation literalTrans0 = ffAdder.AddFreeformAnnotation(infoCba0_0.SegmentHvo, segDefn_literalTranslation);
			literalTrans0.Comment.SetAlternative("Segment0: Literal translation.", Cache.DefaultAnalWs);

			// see if we can load this into the cache.
			StTxtPara.LoadSegmentFreeformAnnotationData(Cache, new Set<int>(segments_Sense2), analWsIds);
			segFFs = Cache.GetVectorProperty(infoCba0_0.SegmentHvo, tagSegFF, true);
			Assert.AreEqual(2, segFFs.Length, "Segment 0 should have freeform annotations.");
			Assert.AreEqual(segFFs[0], freeTrans0.Hvo, "Segment 0 Freeform translation id.");
			Assert.AreEqual(segFFs[1], literalTrans0.Hvo, "Segment 0 Literal translation id.");

			StTxtPara.LoadSegmentFreeformAnnotationData(Cache, pb.ActualParagraph.Hvo, analWsIds);
			segFFs = Cache.GetVectorProperty(infoCba0_0.SegmentHvo, tagSegFF, true);
			Assert.AreEqual(2, segFFs.Length, "Segment 0 should have freeform annotations.");
			Assert.AreEqual(segFFs[0], freeTrans0.Hvo, "Segment 0 Freeform translation id.");
			Assert.AreEqual(segFFs[1], literalTrans0.Hvo, "Segment 0 Literal translation id.");
			segFFs = Cache.GetVectorProperty(infoCba1_0.SegmentHvo, tagSegFF, true);
			Assert.AreEqual(0, segFFs.Length, "Segment 1 should not have any freeform annotations.");
			segFFs = Cache.GetVectorProperty(infoCba2_1.SegmentHvo, tagSegFF, true);
			Assert.AreEqual(0, segFFs.Length, "Segment 2 should not have any freeform annotations.");
		}

		/// <summary>
		/// Test editing simple segment paragraph with sparse analyses.
		/// </summary>
		[Test]
		public void SparseTwficAnalyses_SimpleEdits_SimpleSegmentParagraph_DuplicateWordforms()
		{
			CheckDisposed();

			// First set sparse analyses on wordforms that have multiple occurrences.
			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.SimpleSegmentPara);
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			tapb.SetDefaultWordGloss("xxxyalola", 0);		// gloss first occurrence.
			tapb.SetDefaultWordGloss("xxxpus", 1);			// gloss second occurrence.
			tapb.SetDefaultWordGloss("xxxnihimbilira", 2);	// gloss third occurrence.
			tapb.ValidateAnnotations();
			// Replace some occurrences of these wordforms from the text to validate the analysis does not show up on the wrong occurrence.
			// Remove the first occurrence of 'xxxnihimbilira'; the second occurrence should have the gloss.
			pb.ReplaceSegmentForm("xxxnihimbilira", 0, "");
			pb.RebuildParagraphContentFromAnnotations();
			tapb.ValidateAnnotations();
			// Remove first occurrence of 'xxxpus'; the next one should still have the gloss.
			pb.ReplaceSegmentForm("xxxpus", 0, "");
			pb.RebuildParagraphContentFromAnnotations();
			tapb.ValidateAnnotations();
			//SparseTwficAnalyses_SimpleEdits_SimpleSegmentParagraph_DuplicateWordforms_RemoveSegment_LT5376()
			//SparseTwficAnalyses_SimpleEdits_SimpleSegmentParagraph_DuplicateWordforms_AddWhitespace_LT5313()
		}

		[Test]
		[Ignore("See LT-5376 for details.")]
		public void SparseTwficAnalyses_SimpleEdits_SimpleSegmentParagraph_DuplicateWordforms_RemoveSegment_LT5376()
		{
			CheckDisposed();

			// First set sparse analyses on wordforms that have multiple occurrences.
			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.SimpleSegmentPara);
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			tapb.SetDefaultWordGloss("xxxyalola", 0);		// gloss first occurrence.
			tapb.ValidateAnnotations();
			// Remove first sentence containing 'xxxyalola'; its annotation should be removed.
			pb.RemoveSegment(0);
			pb.RebuildParagraphContentFromAnnotations();
			tapb.ValidateAnnotations();
		}

		[Test]
		[Ignore("See LT-5313 for details.")]
		public void SparseTwficAnalyses_SimpleEdits_SimpleSegmentParagraph_DuplicateWordforms_AddWhitespace_LT5313()
		{
			CheckDisposed();

			// First set sparse analyses on wordforms that have multiple occurrences.
			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.SimpleSegmentPara);
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			tapb.SetDefaultWordGloss("xxxyalola", 0);		// gloss first occurrence.
			tapb.SetDefaultWordGloss("xxxpus", 1);			// gloss second occurrence.
			tapb.SetDefaultWordGloss("xxxnihimbilira", 2);	// gloss third occurrence.
			tapb.ValidateAnnotations();
			// Append whitespace in the text, and see if the analyses still show up in the right place
			// (cf. LT-5313).
			pb.ReplaceTrailingWhitepace(0, 0, 1);
			pb.RebuildParagraphContentFromAnnotations();
			tapb.ValidateAnnotations();
		}

		[Test]
		public void SparseTwficAnalyses_NoEdits_MixedCaseWordformsParagraph()
		{
			CheckDisposed();

			// First set sparse analyses on wordforms that have multiple occurrences.
			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.MixedCases);
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			tapb.ValidateAnnotations();

			// set the corresponding annotations to lowercase wordforms.
			tapb.SetAlternateCase("Xxxpus", 0, StringCaseStatus.allLower);
			tapb.SetAlternateCase("Xxxnihimbilira", 0, StringCaseStatus.allLower);
			tapb.SetAlternateCase("Xxxhesyla", 0, StringCaseStatus.allLower);
			tapb.SetAlternateCase("XXXNIHIMBILIRA", 0, StringCaseStatus.allLower);
			tapb.ValidateAnnotations();
		}

		[Test]
		public void CheckValidGuessesAfterInsertNewWord_LT8467()
		{
			//NOTE: The new test paragraphs need to have all new words w/o duplicates so we can predict the guesses
			//xxxcrayzee xxxyouneek xxxsintents.

			// copy a text of first paragraph into a new paragraph to generate guesses.
			StTxtPara paraGlossed = m_text1.ContentsOA.ParagraphsOS.Append(new StTxtPara()) as StTxtPara;
			StTxtPara paraGuessed = m_text1.ContentsOA.ParagraphsOS.Append(new StTxtPara()) as StTxtPara;
			paraGlossed.Contents.UnderlyingTsString = StringUtils.MakeTss("xxxcrayzee xxxyouneek xxxsintents.", Cache.DefaultVernWs);
			paraGuessed.Contents.UnderlyingTsString = paraGlossed.Contents.UnderlyingTsString;

			// collect expected guesses from the glosses in the first paragraph.
			ParagraphAnnotator paGlossed = new ParagraphAnnotator(paraGlossed);
			List<int> expectedGuesses = paGlossed.SetupDefaultWordGlosses();

			// then verify we've created guesses for the new text.
			ParagraphAnnotator paGuessed = new ParagraphAnnotator(paraGuessed);
			bool fDidParse;
			ParagraphParser.ParseText(m_text1.ContentsOA, new NullProgressState(), out fDidParse);

			paGuessed.LoadParaDefaultAnalyses();
			List<int> expectedGuessesBeforeEdit = new List<int>(expectedGuesses);
			ValidateGuesses(expectedGuessesBeforeEdit, paraGuessed);

			// now edit the paraGuessed and expected Guesses.
			paraGuessed.Contents.UnderlyingTsString = StringUtils.MakeTss("xxxcrayzee xxxguessless xxxyouneek xxxsintents.", Cache.DefaultVernWs);
			List<int> expectedGuessesAfterEdit = new List<int>(expectedGuesses);
			// we don't expect a guess for the inserted word, so insert 0 after first twfic.
			expectedGuessesAfterEdit.Insert(1, 0);

			// Note: we need to use ParseText rather than ReparseParagraph, because it uses
			// code to Reuse dummy annotations.
			ParagraphParser.ParseText(m_text1.ContentsOA, new NullProgressState(), out fDidParse);
			paGuessed.LoadParaDefaultAnalyses();
			ValidateGuesses(expectedGuessesAfterEdit, paraGuessed);
		}


		private void ValidateGuesses(List<int> expectedGuesses, StTxtPara paraWithGuesses)
		{
			List<int> segsParaGuesses = paraWithGuesses.Segments;
			int iExpectedGuess = 0;
			foreach (int hvoSegParaGuesses in segsParaGuesses)
			{
				List<int> segFormsParaGuesses = paraWithGuesses.SegmentForms(hvoSegParaGuesses);
				Assert.AreEqual(expectedGuesses.Count, segFormsParaGuesses.Count);
				foreach (int hvoSegFormParaGuesses in segFormsParaGuesses)
				{
					int hvoGuessActual = 0;
					CmBaseAnnotation cba = new CmBaseAnnotation(Cache, hvoSegFormParaGuesses);
					if (cba.InstanceOfRAHvo != 0)
					{
						// should be a twfic so get it's guess.
						StTxtPara.TwficInfo cbaInfo = new StTxtPara.TwficInfo(Cache, cba.Hvo);
						hvoGuessActual = cbaInfo.GetGuess();
					}
					Assert.AreEqual(expectedGuesses[iExpectedGuess], hvoGuessActual, "Guess mismatch");
					iExpectedGuess++;
				}
			}
		}

		[Test]
		public void ConcordanceWordforms_Text1()
		{
			//SetupOldWordformingOverrides();
			CheckDisposed();
			WordformInventory wfi = m_fdoCache.LangProject.WordformInventoryOA as WordformInventory;
			LangProject lp = Cache.LangProject as LangProject;
			int kflidConcordanceTexts = LangProject.InterlinearTextsFlid(Cache);
			int kflidConcordanceWords = WordformInventory.ConcordanceWordformsFlid(Cache);
			int kflidWordformOccurrences = WfiWordform.OccurrencesFlid(Cache);

			// Test ConcordanceTexts (default is all texts).
			int[] expectedTexts = Cache.LangProject.TextsOC.HvoArray;
			int[] texts = m_fdoCache.GetVectorProperty(Cache.LangProject.Hvo, kflidConcordanceTexts, false);
			Assert.AreEqual(expectedTexts.Length, texts.Length, "Text count mismatch.");

			// Make sure all our texts are out of date (and need to be parsed)
			CheckIsUpToDate(texts, false);

			// Limit the Concordance text to Text1
			List<int> testTexts = new List<int>(new int[] { m_text1.ContentsOA.Hvo });
			lp.InterlinearTexts = testTexts;
			texts = m_fdoCache.GetVectorProperty(Cache.LangProject.Hvo, kflidConcordanceTexts, false);
			Assert.AreEqual(testTexts.Count, texts.Length, "Text count mismatch.");

			// Do the actual parsing.
			int[] words = m_fdoCache.GetVectorProperty(wfi.Hvo, kflidConcordanceWords, false);

			// make sure the text is now up to date.
			CheckIsUpToDate(texts, true);

			// make sure we have the same wordforms, and the expected number of occurrences.
			Dictionary<string, int> expectedOccurrences = BuildExpectedOccurrences();
			CheckExpectedWordformsAndOccurrences(wfi, expectedOccurrences);

			// Modify paragraph contents and make sure text is no longer up to date.
			//           1         2         3         4         5         6         7         8         9
			// 01234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901
			// Xxxpus xxxyalola xxxnihimbilira. Xxxnihimbilira xxxpus Xxxyalola. Xxxhesyla XXXNIHIMBILIRA.
			string para5Contents = ParagraphContents[(int)Text1ParaIndex.MixedCases];
			string newContents = para5Contents.Replace("Xxxnihimbilira", "Xxxpus");
			StTxtPara para5 = m_text1.ContentsOA.ParagraphsOS[(int)Text1ParaIndex.MixedCases] as StTxtPara;
			ITsStrBldr bldr = para5.Contents.UnderlyingTsString.GetBldr();
			bldr.Replace(33, 47, "Xxxpus", null);
			para5.Contents.UnderlyingTsString = bldr.GetString();
			CheckIsUpToDate(texts, false);
			// Reparse text.
			RebuildConcordance(wfi, kflidConcordanceWords);
			CheckIsUpToDate(texts, true);

			int wsVern = Cache.DefaultVernWs;
			expectedOccurrences.Remove("Xxxnihimbilira" + wsVern.ToString());
			expectedOccurrences["Xxxpus" + wsVern.ToString()] += 1;
			CheckExpectedWordformsAndOccurrences(wfi, expectedOccurrences);

			// Remove the last paragraph and check see that it is out of date.

		}

		private void CheckExpectedWordformsAndOccurrences(WordformInventory wfi, Dictionary<string, int> expectedOccurrences)
		{
			int kflidWordformOccurrences = WfiWordform.OccurrencesFlid(Cache);
			// Get the words parsed while generating our list.
			Set<int> wordsCollected = ParagraphParser.WordformsFromLastParseSession(m_fdoCache);

			List<string> expectedWordforms = new List<string>(expectedOccurrences.Keys);
			foreach (string key in expectedWordforms)
			{
				int ichWs = key.IndexOfAny(new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' });
				string form = key.Substring(0, ichWs);
				int ws = Convert.ToInt32(key.Substring(ichWs));
				int hvoWordform = wfi.GetWordformId(form, ws);
				Assert.AreNotEqual(0, hvoWordform, String.Format("Expected to find key {0} in ConcordanceWords.", key));
				Assert.Contains(hvoWordform, wordsCollected.ToArray(), "The wordforms collected in last parse session doesn't contain this id.");

				// see if we match the expected occurrences.
				int[] actualOccurrences = Cache.GetVectorProperty(hvoWordform, kflidWordformOccurrences, true);
				Assert.AreEqual(expectedOccurrences[key], actualOccurrences.Length,
					String.Format("Unexpected number of occurrences for wordform {0}", key));
			}
			Assert.AreEqual(expectedWordforms.Count, wordsCollected.Count, "Word count mismatch.");
		}



		private void RebuildConcordance(WordformInventory wfi, int kflidConcordanceWords)
		{
			IVwVirtualHandler vh = Cache.VwCacheDaAccessor.GetVirtualHandlerId(kflidConcordanceWords);
			vh.Load(wfi.Hvo, kflidConcordanceWords, 0, Cache.VwCacheDaAccessor);
			m_fdoCache.GetVectorProperty(wfi.Hvo, kflidConcordanceWords, false);
		}

		private void CheckIsUpToDate(int[] texts, bool fExpectUpToDate)
		{
			foreach (IStText text in new FdoObjectSet<IStText>(Cache, texts, false))
			{
				Assert.AreEqual(fExpectUpToDate, text.IsUpToDate(),
					String.Format("Unexpected value for text {0} parsing being up to date or not.",
						text.Hvo));
			}
		}
	}
}
